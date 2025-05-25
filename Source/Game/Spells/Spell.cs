// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.Combat;
using Game.Conditions;
using Game.DataStorage;
using Game.Entities;
using Game.Loots;
using Game.Maps;
using Game.Miscellaneous;
using Game.Movement;
using Game.Networking.Packets;
using Game.Scripting;
using Game.Scripting.v2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Game.Spells
{
    public partial class Spell : IDisposable
    {
        public Spell(WorldObject caster, SpellInfo info, TriggerCastFlags triggerFlags, ObjectGuid originalCasterGUID = default, ObjectGuid originalCastId = default)
        {
            m_spellInfo = info;
            m_caster = (info.HasAttribute(SpellAttr6.OriginateFromController) && caster.GetCharmerOrOwner() != null ? caster.GetCharmerOrOwner() : caster);
            m_spellValue = new SpellValue(m_spellInfo, caster);

            // Get data for type of attack
            m_attackType = info.GetAttackType();

            m_spellSchoolMask = m_spellInfo.GetSchoolMask();           // Can be override for some spell (wand shoot for example)

            Player playerCaster = m_caster.ToPlayer();
            if (playerCaster != null)
            {
                // wand case
                if (m_attackType == WeaponAttackType.RangedAttack)
                {
                    if ((playerCaster.GetClassMask() & (uint)Class.ClassMaskWandUsers) != 0)
                    {
                        Item pItem = playerCaster.GetWeaponForAttack(WeaponAttackType.RangedAttack);
                        if (pItem != null)
                            m_spellSchoolMask = (SpellSchoolMask)(1 << (int)pItem.GetTemplate().GetDamageType());
                    }
                }
            }

            Player modOwner = caster.GetSpellModOwner();
            if (modOwner != null)
                modOwner.ApplySpellMod(info, SpellModOp.Doses, ref m_spellValue.AuraStackAmount, this);

            if (!originalCasterGUID.IsEmpty())
                m_originalCasterGUID = originalCasterGUID;
            else
                m_originalCasterGUID = m_caster.GetGUID();

            if (m_originalCasterGUID == m_caster.GetGUID())
                m_originalCaster = m_caster.ToUnit();
            else
            {
                m_originalCaster = Global.ObjAccessor.GetUnit(m_caster, m_originalCasterGUID);
                if (m_originalCaster != null && !m_originalCaster.IsInWorld)
                    m_originalCaster = null;
            }

            _triggeredCastFlags = triggerFlags;

            if (info.HasAttribute(SpellAttr2.DoNotReportSpellFailure))
                _triggeredCastFlags |= TriggerCastFlags.DontReportCastError;

            if (m_spellInfo.HasAttribute(SpellAttr4.AllowCastWhileCasting))
                _triggeredCastFlags |= TriggerCastFlags.IgnoreCastInProgress;

            m_castItemLevel = -1;

            if (IsIgnoringCooldowns())
                m_castFlagsEx |= SpellCastFlagsEx.IgnoreCooldown;

            m_castId = ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, m_caster.GetMapId(), m_spellInfo.Id, m_caster.GetMap().GenerateLowGuid(HighGuid.Cast));
            m_originalCastId = originalCastId;
            m_SpellVisual.SpellXSpellVisualID = caster.GetCastSpellXSpellVisualId(m_spellInfo);
            m_procChainLength = caster.IsUnit() ? caster.ToUnit().GetProcChainLength() : 0;

            //Auto Shot & Shoot (wand)
            m_autoRepeat = m_spellInfo.IsAutoRepeatRangedSpell();

            if (m_spellInfo.IsEmpowerSpell())
                m_empower = new EmpowerData();

            // Determine if spell can be reflected back to the caster
            // Patch 1.2 notes: Spell Reflection no longer reflects abilities
            m_canReflect = caster.IsUnit() && ((m_spellInfo.DmgClass == SpellDmgClass.Magic && !m_spellInfo.HasAttribute(SpellAttr0.IsAbility)) || m_spellInfo.HasAttribute(SpellAttr7.AllowSpellReflection))
                && !m_spellInfo.HasAttribute(SpellAttr1.NoReflection) && !m_spellInfo.HasAttribute(SpellAttr0.NoImmunities)
                && !m_spellInfo.IsPassive();

            for (var i = 0; i < SpellConst.MaxEffects; ++i)
                m_destTargets[i] = new SpellDestination(m_caster);
        }

        public virtual void Dispose()
        {
            // unload scripts
            for (var i = 0; i < m_loadedScripts.Count; ++i)
                m_loadedScripts[i]._Unload();

            if (m_referencedFromCurrentSpell && m_selfContainer != null && m_selfContainer == this)
            {
                // Clean the reference to avoid later crash.
                // If this error is repeating, we may have to add an ASSERT to better track down how we get into this case.
                Log.outError(LogFilter.Spells, "SPELL: deleting spell for spell ID {0}. However, spell still referenced.", m_spellInfo.Id);
                m_selfContainer = null;
            }

            if (m_caster != null && m_caster.GetTypeId() == TypeId.Player)
                Cypher.Assert(m_caster.ToPlayer().m_spellModTakingSpell != this);
        }

        void InitExplicitTargets(SpellCastTargets targets)
        {
            m_targets = targets;

            // this function tries to correct spell explicit targets for spell
            // client doesn't send explicit targets correctly sometimes - we need to fix such spells serverside
            // this also makes sure that we correctly send explicit targets to client (removes redundant data)
            SpellCastTargetFlags neededTargets = m_spellInfo.GetExplicitTargetMask();

            WorldObject target = m_targets.GetObjectTarget();
            if (target != null)
            {
                // check if object target is valid with needed target flags
                // for unit case allow corpse target mask because player with not released corpse is a unit target
                if ((target.ToUnit() != null && !neededTargets.HasAnyFlag(SpellCastTargetFlags.UnitMask | SpellCastTargetFlags.CorpseMask))
                    || (target.IsTypeId(TypeId.GameObject) && !neededTargets.HasFlag(SpellCastTargetFlags.GameobjectMask))
                    || (target.IsTypeId(TypeId.Corpse) && !neededTargets.HasFlag(SpellCastTargetFlags.CorpseMask)))
                    m_targets.RemoveObjectTarget();
            }
            else
            {
                // try to select correct unit target if not provided by client or by serverside cast
                if (neededTargets.HasAnyFlag(SpellCastTargetFlags.UnitMask))
                {
                    Unit unit = null;
                    // try to use player selection as a target
                    Player playerCaster = m_caster.ToPlayer();
                    if (playerCaster != null)
                    {
                        // selection has to be found and to be valid target for the spell
                        Unit selectedUnit = Global.ObjAccessor.GetUnit(m_caster, playerCaster.GetTarget());
                        if (selectedUnit != null)
                            if (m_spellInfo.CheckExplicitTarget(m_caster, selectedUnit) == SpellCastResult.SpellCastOk)
                                unit = selectedUnit;
                    }
                    // try to use attacked unit as a target
                    else if (m_caster.IsTypeId(TypeId.Unit) && neededTargets.HasAnyFlag(SpellCastTargetFlags.UnitEnemy | SpellCastTargetFlags.Unit))
                        unit = m_caster.ToUnit().GetVictim();

                    // didn't find anything - let's use self as target
                    if (unit == null && neededTargets.HasAnyFlag(SpellCastTargetFlags.UnitRaid | SpellCastTargetFlags.UnitParty | SpellCastTargetFlags.UnitAlly))
                        unit = m_caster.ToUnit();

                    m_targets.SetUnitTarget(unit);
                }
            }

            // check if spell needs dst target
            if (neededTargets.HasFlag(SpellCastTargetFlags.DestLocation))
            {
                // and target isn't set
                if (!m_targets.HasDst())
                {
                    // try to use unit target if provided
                    WorldObject targett = targets.GetObjectTarget();
                    if (targett != null)
                        m_targets.SetDst(targett);
                    // or use self if not available
                    else
                        m_targets.SetDst(m_caster);
                }
            }
            else
                m_targets.RemoveDst();

            if (neededTargets.HasFlag(SpellCastTargetFlags.SourceLocation))
            {
                if (!targets.HasSrc())
                    m_targets.SetSrc(m_caster);
            }
            else
                m_targets.RemoveSrc();
        }

        void SelectExplicitTargets()
        {
            // here go all explicit target changes made to explicit targets after spell prepare phase is finished
            Unit target = m_targets.GetUnitTarget();
            if (target != null)
            {
                // check for explicit target redirection, for Grounding Totem for example
                if (m_spellInfo.GetExplicitTargetMask().HasAnyFlag(SpellCastTargetFlags.UnitEnemy)
                    || (m_spellInfo.GetExplicitTargetMask().HasAnyFlag(SpellCastTargetFlags.Unit) && !m_caster.IsFriendlyTo(target)))
                {
                    Unit redirect = null;
                    switch (m_spellInfo.DmgClass)
                    {
                        case SpellDmgClass.Magic:
                            redirect = m_caster.GetMagicHitRedirectTarget(target, m_spellInfo);
                            break;
                        case SpellDmgClass.Melee:
                        case SpellDmgClass.Ranged:
                            // should gameobjects cast damagetype melee/ranged spells this needs to be changed
                            redirect = m_caster.ToUnit().GetMeleeHitRedirectTarget(target, m_spellInfo);
                            break;
                        default:
                            break;
                    }
                    if (redirect != null && (redirect != target))
                        m_targets.SetUnitTarget(redirect);
                }
            }
        }

        public void SelectSpellTargets()
        {
            // select targets for cast phase
            SelectExplicitTargets();

            uint processedAreaEffectsMask = 0;
            foreach (var spellEffectInfo in m_spellInfo.GetEffects())
            {
                // not call for empty effect.
                // Also some spells use not used effect targets for store targets for dummy effect in triggered spells
                if (!spellEffectInfo.IsEffect())
                    continue;

                // set expected type of implicit targets to be sent to client
                SpellCastTargetFlags implicitTargetMask = SpellInfo.GetTargetFlagMask(spellEffectInfo.TargetA.GetObjectType()) | SpellInfo.GetTargetFlagMask(spellEffectInfo.TargetB.GetObjectType());
                if (Convert.ToBoolean(implicitTargetMask & SpellCastTargetFlags.Unit))
                    m_targets.SetTargetFlag(SpellCastTargetFlags.Unit);
                if (Convert.ToBoolean(implicitTargetMask & (SpellCastTargetFlags.Gameobject | SpellCastTargetFlags.GameobjectItem)))
                    m_targets.SetTargetFlag(SpellCastTargetFlags.Gameobject);

                SelectEffectImplicitTargets(spellEffectInfo, spellEffectInfo.TargetA, SpellTargetIndex.TargetA, ref processedAreaEffectsMask);
                SelectEffectImplicitTargets(spellEffectInfo, spellEffectInfo.TargetB, SpellTargetIndex.TargetB, ref processedAreaEffectsMask);

                // Select targets of effect based on effect type
                // those are used when no valid target could be added for spell effect based on spell target type
                // some spell effects use explicit target as a default target added to target map (like SPELL_EFFECT_LEARN_SPELL)
                // some spell effects add target to target map only when target type specified (like SPELL_EFFECT_WEAPON)
                // some spell effects don't add anything to target map (confirmed with sniffs) (like SPELL_EFFECT_DESTROY_ALL_TOTEMS)
                SelectEffectTypeImplicitTargets(spellEffectInfo);

                if (m_targets.HasDst())
                    AddDestTarget(m_targets.GetDst(), spellEffectInfo.EffectIndex);

                if (spellEffectInfo.TargetA.GetObjectType() == SpellTargetObjectTypes.Unit
                    || spellEffectInfo.TargetA.GetObjectType() == SpellTargetObjectTypes.UnitAndDest
                    || spellEffectInfo.TargetB.GetObjectType() == SpellTargetObjectTypes.Unit
                    || spellEffectInfo.TargetB.GetObjectType() == SpellTargetObjectTypes.UnitAndDest)
                {
                    if (m_spellInfo.HasAttribute(SpellAttr1.RequireAllTargets))
                    {
                        bool noTargetFound = !m_UniqueTargetInfo.Any(target => (target.EffectMask & 1 << (int)spellEffectInfo.EffectIndex) != 0);

                        if (noTargetFound)
                        {
                            SendCastResult(SpellCastResult.BadImplicitTargets);
                            Finish(SpellCastResult.BadImplicitTargets);
                            return;
                        }
                    }
                    if (m_spellInfo.HasAttribute(SpellAttr2.FailOnAllTargetsImmune))
                    {
                        bool anyNonImmuneTargetFound = m_UniqueTargetInfo.Any(target => (target.EffectMask & 1 << (int)spellEffectInfo.EffectIndex) != 0 && target.MissCondition != SpellMissInfo.Immune && target.MissCondition != SpellMissInfo.Immune2);

                        if (!anyNonImmuneTargetFound)
                        {
                            SendCastResult(SpellCastResult.Immune);
                            Finish(SpellCastResult.Immune);
                            return;
                        }
                    }
                }

                if (m_spellInfo.IsChanneled())
                {
                    // maybe do this for all spells?
                    if (focusObject == null && m_UniqueTargetInfo.Empty() && m_UniqueGOTargetInfo.Empty() && m_UniqueItemInfo.Empty() && !m_targets.HasDst())
                    {
                        SendCastResult(SpellCastResult.BadImplicitTargets);
                        Finish(SpellCastResult.BadImplicitTargets);
                        return;
                    }

                    uint mask = (1u << (int)spellEffectInfo.EffectIndex);
                    foreach (var ihit in m_UniqueTargetInfo)
                    {
                        if (Convert.ToBoolean(ihit.EffectMask & mask))
                        {
                            m_channelTargetEffectMask |= mask;
                            break;
                        }
                    }
                }
            }

            if (m_targets.HasDst())
            {
                if (m_spellInfo.HasAttribute(SpellAttr8.RequiresLocationToBeOnLiquidSurface))
                {
                    ZLiquidStatus status = m_caster.GetMap().GetLiquidStatus(m_caster.GetPhaseShift(), m_targets.GetDstPos().GetPositionX(), m_targets.GetDstPos().GetPositionY(), m_targets.GetDstPos().GetPositionZ());
                    if (!status.HasAnyFlag(ZLiquidStatus.WaterWalk | ZLiquidStatus.InWater))
                    {
                        SendCastResult(SpellCastResult.NoLiquid);
                        Finish(SpellCastResult.NoLiquid);
                        return;
                    }
                }
            }

            ulong dstDelay = CalculateDelayMomentForDst(m_spellInfo.LaunchDelay);
            if (dstDelay != 0)
                m_delayMoment = dstDelay;
        }

        ulong CalculateDelayMomentForDst(float launchDelay)
        {
            if (m_targets.HasDst())
            {
                if (m_targets.HasTraj())
                {
                    float speed = m_targets.GetSpeedXY();
                    if (speed > 0.0f)
                        return (ulong)(Math.Floor((m_targets.GetDist2d() / speed + launchDelay) * 1000.0f));
                }
                else if (m_spellInfo.HasAttribute(SpellAttr9.MissileSpeedIsDelayInSec))
                    return (ulong)(Math.Floor((m_spellInfo.Speed + launchDelay) * 1000.0f));
                else if (m_spellInfo.Speed > 0.0f)
                {
                    // We should not subtract caster size from dist calculation (fixes execution time desync with animation on client, eg. Malleable Goo cast by PP)
                    float dist = m_caster.GetExactDist(m_targets.GetDstPos());
                    return (ulong)(Math.Floor((dist / m_spellInfo.Speed + launchDelay) * 1000.0f));
                }

                return (ulong)Math.Floor(launchDelay * 1000.0f);
            }

            return 0;
        }

        public void RecalculateDelayMomentForDst()
        {
            UpdateDelayMomentForDst(CalculateDelayMomentForDst(0.0f));
        }

        void UpdateDelayMomentForDst(ulong hitDelay)
        {
            m_delayMoment = hitDelay;

            if (GetDelayStart() != 0)
                m_caster.m_Events.ModifyEventTime(_spellEvent, TimeSpan.FromMilliseconds(GetDelayStart() + m_delayMoment));
        }

        void UpdateDelayMomentForUnitTarget(Unit unit, ulong hitDelay)
        {
            var itr = m_UniqueTargetInfo.Find(targetInfo => targetInfo.TargetGUID == unit.GetGUID());

            ulong oldDelay = itr.TimeDelay;
            itr.TimeDelay = hitDelay;

            if (hitDelay != 0 && (m_delayMoment == 0 || m_delayMoment > hitDelay))
                m_delayMoment = hitDelay;
            else if (m_delayMoment != 0 && oldDelay < hitDelay)
            {
                // if new hit delay is greater than old delay for this target we must check all other spell targets to see if m_delayMoment can be increased
                var minDelay = m_UniqueTargetInfo.Min(targetInfo => targetInfo.TimeDelay);

                m_delayMoment = minDelay;
            }

            if (GetDelayStart() != 0)
                m_caster.m_Events.ModifyEventTime(_spellEvent, TimeSpan.FromMilliseconds(GetDelayStart() + m_delayMoment));
        }

        void SelectEffectImplicitTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, SpellTargetIndex targetIndex, ref uint processedEffectMask)
        {
            if (targetType.GetTarget() == 0)
                return;

            uint effectMask = (1u << (int)spellEffectInfo.EffectIndex);
            // set the same target list for all effects
            // some spells appear to need this, however this requires more research
            switch (targetType.GetSelectionCategory())
            {
                case SpellTargetSelectionCategories.Nearby:
                case SpellTargetSelectionCategories.Cone:
                case SpellTargetSelectionCategories.Area:
                case SpellTargetSelectionCategories.Line:
                {
                    // targets for effect already selected
                    if (Convert.ToBoolean(effectMask & processedEffectMask))
                        return;

                    var effects = GetSpellInfo().GetEffects();
                    // choose which targets we can select at once
                    for (int j = (int)spellEffectInfo.EffectIndex + 1; j < effects.Count; ++j)
                    {
                        if (effects[j].IsEffect() &&
                            spellEffectInfo.TargetA.GetTarget() == effects[j].TargetA.GetTarget() &&
                            spellEffectInfo.TargetB.GetTarget() == effects[j].TargetB.GetTarget() &&
                            spellEffectInfo.ImplicitTargetConditions == effects[j].ImplicitTargetConditions &&
                            spellEffectInfo.CalcRadius(m_caster, SpellTargetIndex.TargetA) == effects[j].CalcRadius(m_caster, SpellTargetIndex.TargetA) &&
                            spellEffectInfo.CalcRadius(m_caster, SpellTargetIndex.TargetB) == effects[j].CalcRadius(m_caster, SpellTargetIndex.TargetB) &&
                            spellEffectInfo.EffectAttributes.HasFlag(SpellEffectAttributes.PlayersOnly) == effects[j].EffectAttributes.HasFlag(SpellEffectAttributes.PlayersOnly) &&
                            CheckScriptEffectImplicitTargets(spellEffectInfo.EffectIndex, (uint)j))
                        {
                            effectMask |= 1u << j;
                        }
                    }
                    processedEffectMask |= effectMask;
                    break;
                }
                default:
                    break;
            }

            switch (targetType.GetSelectionCategory())
            {
                case SpellTargetSelectionCategories.Channel:
                    SelectImplicitChannelTargets(spellEffectInfo, targetType);
                    break;
                case SpellTargetSelectionCategories.Nearby:
                    SelectImplicitNearbyTargets(spellEffectInfo, targetType, targetIndex, effectMask);
                    break;
                case SpellTargetSelectionCategories.Cone:
                    SelectImplicitConeTargets(spellEffectInfo, targetType, targetIndex, effectMask);
                    break;
                case SpellTargetSelectionCategories.Area:
                    SelectImplicitAreaTargets(spellEffectInfo, targetType, targetIndex, effectMask);
                    break;
                case SpellTargetSelectionCategories.Traj:
                    // just in case there is no dest, explanation in SelectImplicitDestDestTargets
                    CheckDst();

                    SelectImplicitTrajTargets(spellEffectInfo, targetType);
                    break;
                case SpellTargetSelectionCategories.Line:
                    SelectImplicitLineTargets(spellEffectInfo, targetType, targetIndex, effectMask);
                    break;
                case SpellTargetSelectionCategories.Default:
                    switch (targetType.GetObjectType())
                    {
                        case SpellTargetObjectTypes.Src:
                            switch (targetType.GetReferenceType())
                            {
                                case SpellTargetReferenceTypes.Caster:
                                    m_targets.SetSrc(m_caster);
                                    break;
                                default:
                                    Cypher.Assert(false, "Spell.SelectEffectImplicitTargets: received not implemented select target reference type for TARGET_TYPE_OBJECT_SRC");
                                    break;
                            }
                            break;
                        case SpellTargetObjectTypes.Dest:
                            switch (targetType.GetReferenceType())
                            {
                                case SpellTargetReferenceTypes.Caster:
                                    SelectImplicitCasterDestTargets(spellEffectInfo, targetType, targetIndex);
                                    break;
                                case SpellTargetReferenceTypes.Target:
                                    SelectImplicitTargetDestTargets(spellEffectInfo, targetType, targetIndex);
                                    break;
                                case SpellTargetReferenceTypes.Dest:
                                    SelectImplicitDestDestTargets(spellEffectInfo, targetType, targetIndex);
                                    break;
                                default:
                                    Cypher.Assert(false, "Spell.SelectEffectImplicitTargets: received not implemented select target reference type for TARGET_TYPE_OBJECT_DEST");
                                    break;
                            }
                            break;
                        default:
                            switch (targetType.GetReferenceType())
                            {
                                case SpellTargetReferenceTypes.Caster:
                                    SelectImplicitCasterObjectTargets(spellEffectInfo, targetType);
                                    break;
                                case SpellTargetReferenceTypes.Target:
                                    SelectImplicitTargetObjectTargets(spellEffectInfo, targetType);
                                    break;
                                default:
                                    Cypher.Assert(false, "Spell.SelectEffectImplicitTargets: received not implemented select target reference type for TARGET_TYPE_OBJECT");
                                    break;
                            }
                            break;
                    }
                    break;
                case SpellTargetSelectionCategories.Nyi:
                    Log.outDebug(LogFilter.Spells, "SPELL: target type {0}, found in spellID {1}, effect {2} is not implemented yet!", m_spellInfo.Id, spellEffectInfo.EffectIndex, targetType.GetTarget());
                    break;
                default:
                    Cypher.Assert(false, "Spell.SelectEffectImplicitTargets: received not implemented select target category");
                    break;
            }
        }

        void SelectImplicitChannelTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
        {
            if (targetType.GetReferenceType() != SpellTargetReferenceTypes.Caster)
            {
                Cypher.Assert(false, "Spell.SelectImplicitChannelTargets: received not implemented target reference type");
                return;
            }

            Spell channeledSpell = m_originalCaster.GetCurrentSpell(CurrentSpellTypes.Channeled);
            if (channeledSpell == null)
            {
                Log.outDebug(LogFilter.Spells, "Spell.SelectImplicitChannelTargets: cannot find channel spell for spell ID {0}, effect {1}", m_spellInfo.Id, spellEffectInfo.EffectIndex);
                return;
            }

            switch (targetType.GetTarget())
            {
                case Targets.UnitChannelTarget:
                {
                    foreach (ObjectGuid channelTarget in m_originalCaster.m_unitData.ChannelObjects)
                    {
                        WorldObject target = Global.ObjAccessor.GetUnit(m_caster, channelTarget);
                        CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, targetType);
                        // unit target may be no longer avalible - teleported out of map for example
                        Unit unitTarget = target?.ToUnit();
                        if (unitTarget != null)
                            AddUnitTarget(unitTarget, 1u << (int)spellEffectInfo.EffectIndex);
                        else
                            Log.outDebug(LogFilter.Spells, "SPELL: cannot find channel spell target for spell ID {0}, effect {1}", m_spellInfo.Id, spellEffectInfo.EffectIndex);
                    }
                    break;
                }
                case Targets.DestChannelTarget:
                {
                    if (channeledSpell.m_targets.HasDst())
                        m_targets.SetDst(channeledSpell.m_targets);
                    else
                    {
                        List<ObjectGuid> channelObjects = m_originalCaster.m_unitData.ChannelObjects;
                        WorldObject target = !channelObjects.Empty() ? Global.ObjAccessor.GetWorldObject(m_caster, channelObjects[0]) : null;
                        if (target != null)
                        {
                            CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, targetType);
                            if (target != null)
                            {
                                SpellDestination dest = new(target);
                                if (m_spellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
                                    dest.Position.SetOrientation(spellEffectInfo.PositionFacing);

                                CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
                                m_targets.SetDst(dest);
                            }
                        }
                        else
                            Log.outDebug(LogFilter.Spells, "SPELL: cannot find channel spell destination for spell ID {0}, effect {1}", m_spellInfo.Id, spellEffectInfo.EffectIndex);
                    }
                    break;
                }
                case Targets.DestChannelCaster:
                {
                    SpellDestination dest = new(channeledSpell.GetCaster());
                    if (m_spellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
                        dest.Position.SetOrientation(spellEffectInfo.PositionFacing);

                    CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
                    m_targets.SetDst(dest);
                    break;
                }
                default:
                    Cypher.Assert(false, "Spell.SelectImplicitChannelTargets: received not implemented target type");
                    break;
            }
        }

        void SelectImplicitNearbyTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, SpellTargetIndex targetIndex, uint effMask)
        {
            if (targetType.GetReferenceType() != SpellTargetReferenceTypes.Caster)
            {
                Cypher.Assert(false, "Spell.SelectImplicitNearbyTargets: received not implemented target reference type");
                return;
            }

            float range = 0.0f;
            switch (targetType.GetCheckType())
            {
                case SpellTargetCheckTypes.Enemy:
                    range = m_spellInfo.GetMaxRange(false, m_caster, this);
                    break;
                case SpellTargetCheckTypes.Ally:
                case SpellTargetCheckTypes.Party:
                case SpellTargetCheckTypes.Raid:
                case SpellTargetCheckTypes.RaidClass:
                    range = m_spellInfo.GetMaxRange(true, m_caster, this);
                    break;
                case SpellTargetCheckTypes.Entry:
                case SpellTargetCheckTypes.Default:
                    range = m_spellInfo.GetMaxRange(IsPositive(), m_caster, this);
                    break;
                default:
                    Cypher.Assert(false, "Spell.SelectImplicitNearbyTargets: received not implemented selection check type");
                    break;
            }

            List<Condition> condList = spellEffectInfo.ImplicitTargetConditions;

            // handle emergency case - try to use other provided targets if no conditions provided
            if (targetType.GetCheckType() == SpellTargetCheckTypes.Entry && (condList == null || condList.Empty()))
            {
                Log.outDebug(LogFilter.Spells, "Spell.SelectImplicitNearbyTargets: no conditions entry for target with TARGET_CHECK_ENTRY of spell ID {0}, effect {1} - selecting default targets", m_spellInfo.Id, spellEffectInfo.EffectIndex);
                switch (targetType.GetObjectType())
                {
                    case SpellTargetObjectTypes.Gobj:
                        if (m_spellInfo.RequiresSpellFocus != 0)
                        {
                            if (focusObject != null)
                                AddGOTarget(focusObject, effMask);
                            else
                            {
                                SendCastResult(SpellCastResult.BadImplicitTargets);
                                Finish(SpellCastResult.BadImplicitTargets);
                            }
                            return;
                        }
                        break;
                    case SpellTargetObjectTypes.Dest:
                        if (m_spellInfo.RequiresSpellFocus != 0)
                        {
                            if (focusObject != null)
                            {
                                SpellDestination dest = new(focusObject);
                                if (m_spellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
                                    dest.Position.SetOrientation(spellEffectInfo.PositionFacing);

                                CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
                                m_targets.SetDst(dest);
                            }
                            else
                            {
                                SendCastResult(SpellCastResult.BadImplicitTargets);
                                Finish(SpellCastResult.BadImplicitTargets);
                            }
                            return;
                        }
                        if (targetType.GetTarget() == Targets.DestNearbyEntryOrDB)
                        {
                            SpellTargetPosition st = Global.SpellMgr.GetSpellTargetPosition(m_spellInfo.Id, spellEffectInfo.EffectIndex);
                            if (st != null)
                            {
                                SpellDestination dest = new(m_caster);
                                if (st.target_mapId == m_caster.GetMapId() && m_caster.IsInDist(st.target_X, st.target_Y, st.target_Z, range))
                                    dest = new SpellDestination(st.target_X, st.target_Y, st.target_Z, st.target_Orientation);
                                else
                                {
                                    float randomRadius1 = spellEffectInfo.CalcRadius(m_caster, targetIndex);
                                    if (randomRadius1 > 0.0f)
                                        MovePosition(dest.Position, m_caster, randomRadius1, targetType.CalcDirectionAngle());
                                }

                                CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
                                m_targets.SetDst(dest);
                                return;
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            WorldObject target = SearchNearbyTarget(spellEffectInfo, range, targetType.GetObjectType(), targetType.GetCheckType(), condList);
            float randomRadius = 0.0f;
            switch (targetType.GetTarget())
            {
                case Targets.DestNearbyEntryOrDB:
                    // if we are here then there was no db target
                    if (target == null)
                    {
                        target = m_caster;
                        // radius is only meant to be randomized when using caster fallback
                        randomRadius = spellEffectInfo.CalcRadius(m_caster, targetIndex);
                    }
                    break;
                default:
                    break;
            }

            if (target == null)
            {
                Log.outDebug(LogFilter.Spells, "Spell.SelectImplicitNearbyTargets: cannot find nearby target for spell ID {0}, effect {1}", m_spellInfo.Id, spellEffectInfo.EffectIndex);
                SendCastResult(SpellCastResult.BadImplicitTargets);
                Finish(SpellCastResult.BadImplicitTargets);
                return;
            }

            CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, targetType);
            if (target == null)
            {
                Log.outDebug(LogFilter.Spells, $"Spell.SelectImplicitNearbyTargets: OnObjectTargetSelect script hook for spell Id {m_spellInfo.Id} set NULL target, effect {spellEffectInfo.EffectIndex}");
                SendCastResult(SpellCastResult.BadImplicitTargets);
                Finish(SpellCastResult.BadImplicitTargets);
                return;
            }

            switch (targetType.GetObjectType())
            {
                case SpellTargetObjectTypes.Unit:
                    Unit unitTarget = target.ToUnit();
                    if (unitTarget != null)
                        AddUnitTarget(unitTarget, effMask, true, false);
                    else
                    {
                        Log.outDebug(LogFilter.Spells, $"Spell.SelectImplicitNearbyTargets: OnObjectTargetSelect script hook for spell Id {m_spellInfo.Id} set object of wrong type, expected unit, got {target.GetGUID().GetHigh()}, effect {effMask}");
                        SendCastResult(SpellCastResult.BadImplicitTargets);
                        Finish(SpellCastResult.BadImplicitTargets);
                        return;
                    }
                    break;
                case SpellTargetObjectTypes.Gobj:
                    GameObject gobjTarget = target.ToGameObject();
                    if (gobjTarget != null)
                        AddGOTarget(gobjTarget, effMask);
                    else
                    {
                        Log.outDebug(LogFilter.Spells, $"Spell.SelectImplicitNearbyTargets: OnObjectTargetSelect script hook for spell Id {m_spellInfo.Id} set object of wrong type, expected gameobject, got {target.GetGUID().GetHigh()}, effect {effMask}");
                        SendCastResult(SpellCastResult.BadImplicitTargets);
                        Finish(SpellCastResult.BadImplicitTargets);
                        return;
                    }
                    break;
                case SpellTargetObjectTypes.Corpse:
                    Corpse corpseTarget = target.ToCorpse();
                    if (corpseTarget != null)
                        AddCorpseTarget(corpseTarget, effMask);
                    else
                    {
                        Log.outDebug(LogFilter.Spells, $"Spell::SelectImplicitNearbyTargets: OnObjectTargetSelect script hook for spell Id {m_spellInfo.Id} set object of wrong type, expected corpse, got {target.GetGUID().GetTypeId()}, effect {effMask}");
                        SendCastResult(SpellCastResult.BadImplicitTargets);
                        Finish(SpellCastResult.BadImplicitTargets);
                        return;
                    }
                    break;
                case SpellTargetObjectTypes.Dest:
                    SpellDestination dest = new(target);
                    if (randomRadius > 0.0f)
                        MovePosition(dest.Position, target, randomRadius, targetType.CalcDirectionAngle());

                    if (m_spellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
                        dest.Position.SetOrientation(spellEffectInfo.PositionFacing);

                    CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
                    m_targets.SetDst(dest);
                    break;
                default:
                    Cypher.Assert(false, "Spell.SelectImplicitNearbyTargets: received not implemented target object type");
                    break;
            }

            SelectImplicitChainTargets(spellEffectInfo, targetType, target, effMask);
        }

        void SelectImplicitConeTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, SpellTargetIndex targetIndex, uint effMask)
        {
            Position coneSrc = new(m_caster);
            float coneAngle = m_spellInfo.ConeAngle;
            switch (targetType.GetReferenceType())
            {
                case SpellTargetReferenceTypes.Caster:
                    break;
                case SpellTargetReferenceTypes.Dest:
                    if (m_caster.GetExactDist2d(m_targets.GetDstPos()) > 0.1f)
                        coneSrc.SetOrientation(m_caster.GetAbsoluteAngle(m_targets.GetDstPos()));
                    break;
                default:
                    break;
            }

            switch (targetType.GetTarget())
            {
                case Targets.UnitCone180DegEnemy:
                    if (coneAngle == 0.0f)
                        coneAngle = 180.0f;
                    break;
                default:
                    break;
            }

            List<WorldObject> targets = new();
            SpellTargetObjectTypes objectType = targetType.GetObjectType();
            SpellTargetCheckTypes selectionType = targetType.GetCheckType();

            var condList = spellEffectInfo.ImplicitTargetConditions;
            float radius = spellEffectInfo.CalcRadius(m_caster, targetIndex) * m_spellValue.RadiusMod;

            GridMapTypeMask containerTypeMask = GetSearcherTypeMask(m_spellInfo, spellEffectInfo, objectType, condList);
            if (containerTypeMask != 0)
            {
                float extraSearchRadius = radius > 0.0f ? SharedConst.ExtraCellSearchRadius : 0.0f;
                var spellCone = new WorldObjectSpellConeTargetCheck(coneSrc, MathFunctions.DegToRad(coneAngle), m_spellInfo.Width != 0 ? m_spellInfo.Width : m_caster.GetCombatReach(), radius, m_caster, m_spellInfo, selectionType, condList, objectType);
                var searcher = new WorldObjectListSearcher(m_caster, targets, spellCone, containerTypeMask);
                SearchTargets(searcher, containerTypeMask, m_caster, m_caster.GetPosition(), radius + extraSearchRadius);

                CallScriptObjectAreaTargetSelectHandlers(targets, spellEffectInfo.EffectIndex, targetType);

                if (!targets.Empty())
                {
                    // Other special target selection goes here
                    uint maxTargets = m_spellValue.MaxAffectedTargets;
                    if (maxTargets != 0)
                        targets.RandomResize(maxTargets);

                    foreach (var obj in targets)
                    {
                        if (obj.IsUnit())
                            AddUnitTarget(obj.ToUnit(), effMask, false);
                        else if (obj.IsGameObject())
                            AddGOTarget(obj.ToGameObject(), effMask);
                        else if (obj.IsCorpse())
                            AddCorpseTarget(obj.ToCorpse(), effMask);
                    }
                }
            }
        }

        void SelectImplicitAreaTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, SpellTargetIndex targetIndex, uint effMask)
        {
            WorldObject referer;
            switch (targetType.GetReferenceType())
            {
                case SpellTargetReferenceTypes.Src:
                case SpellTargetReferenceTypes.Dest:
                case SpellTargetReferenceTypes.Caster:
                    referer = m_caster;
                    break;
                case SpellTargetReferenceTypes.Target:
                    referer = m_targets.GetUnitTarget();
                    break;
                case SpellTargetReferenceTypes.Last:
                {
                    referer = m_caster;

                    // find last added target for this effect
                    foreach (var target in m_UniqueTargetInfo)
                    {
                        if (Convert.ToBoolean(target.EffectMask & (1 << (int)spellEffectInfo.EffectIndex)))
                        {
                            referer = Global.ObjAccessor.GetUnit(m_caster, target.TargetGUID);
                            break;
                        }
                    }
                    break;
                }
                default:
                    Cypher.Assert(false, "Spell.SelectImplicitAreaTargets: received not implemented target reference type");
                    return;
            }
            if (referer == null)
                return;

            Position center;
            switch (targetType.GetReferenceType())
            {
                case SpellTargetReferenceTypes.Src:
                    center = m_targets.GetSrcPos();
                    break;
                case SpellTargetReferenceTypes.Dest:
                    center = m_targets.GetDstPos();
                    break;
                case SpellTargetReferenceTypes.Caster:
                case SpellTargetReferenceTypes.Target:
                case SpellTargetReferenceTypes.Last:
                    center = referer.GetPosition();
                    break;
                default:
                    Cypher.Assert(false, "Spell.SelectImplicitAreaTargets: received not implemented target reference type");
                    return;
            }

            float radius = spellEffectInfo.CalcRadius(m_caster, targetIndex) * m_spellValue.RadiusMod;
            List<WorldObject> targets = new();
            switch (targetType.GetTarget())
            {
                case Targets.UnitCasterAndPassengers:
                {
                    targets.Add(m_caster);
                    Unit unit = m_caster.ToUnit();
                    if (unit != null)
                    {
                        Vehicle vehicleKit = unit.GetVehicleKit();
                        if (vehicleKit != null)
                            for (sbyte seat = 0; seat < SharedConst.MaxVehicleSeats; ++seat)
                            {
                                Unit passenger = vehicleKit.GetPassenger(seat);
                                if (passenger != null)
                                    targets.Add(passenger);
                            }
                    }
                    break;
                }
                case Targets.UnitTargetAllyOrRaid:
                {
                    Unit targetedUnit = m_targets.GetUnitTarget();
                    if (targetedUnit != null)
                    {
                        if (!m_caster.IsUnit() || !m_caster.ToUnit().IsInRaidWith(targetedUnit))
                            targets.Add(m_targets.GetUnitTarget());
                        else
                            SearchAreaTargets(targets, spellEffectInfo, radius, targetedUnit, referer, targetType.GetObjectType(), targetType.GetCheckType(), spellEffectInfo.ImplicitTargetConditions, WorldObjectSpellAreaTargetSearchReason.Area);
                    }
                    break;
                }
                case Targets.UnitCasterAndSummons:
                {
                    targets.Add(m_caster);
                    SearchAreaTargets(targets, spellEffectInfo, radius, center, referer, targetType.GetObjectType(), targetType.GetCheckType(), spellEffectInfo.ImplicitTargetConditions, WorldObjectSpellAreaTargetSearchReason.Area);
                    break;
                }
                case Targets.UnitAreaThreatList:
                {
                    Unit unit = m_caster.ToUnit();
                    if (unit != null)
                    {
                        foreach (ThreatReference threatRef in unit.GetThreatManager().GetSortedThreatList())
                        {
                            Unit threateningUnit = threatRef.GetVictim();
                            if (threateningUnit != null)
                                targets.Add(threateningUnit);
                        }
                    }
                    break;
                }
                case Targets.UnitAreaTapList:
                {
                    Creature creature = m_caster.ToCreature();
                    if (creature != null)
                    {
                        foreach (ObjectGuid tapperGuid in creature.GetTapList())
                        {
                            Player tapper = Global.ObjAccessor.GetPlayer(m_caster, tapperGuid);
                            if (tapper != null)
                                targets.Add(tapper);
                        }
                    }
                    break;
                }
                default:
                    SearchAreaTargets(targets, spellEffectInfo, radius, center, referer, targetType.GetObjectType(), targetType.GetCheckType(), spellEffectInfo.ImplicitTargetConditions, WorldObjectSpellAreaTargetSearchReason.Area);
                    break;
            }

            if (targetType.GetObjectType() == SpellTargetObjectTypes.UnitAndDest)
            {
                SpellDestination dest = new(referer);
                if (m_spellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
                    dest.Position.SetOrientation(spellEffectInfo.PositionFacing);

                CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);

                m_targets.ModDst(dest);
            }

            CallScriptObjectAreaTargetSelectHandlers(targets, spellEffectInfo.EffectIndex, targetType);

            if (targetType.GetTarget() == Targets.UnitSrcAreaFurthestEnemy)
                targets.Sort(new ObjectDistanceOrderPred(referer, false));

            if (!targets.Empty())
            {
                // Other special target selection goes here
                uint maxTargets = m_spellValue.MaxAffectedTargets;
                if (maxTargets != 0)
                {
                    if (targetType.GetTarget() != Targets.UnitSrcAreaFurthestEnemy)
                        targets.RandomResize(maxTargets);
                    else if (targets.Count > maxTargets)
                        targets.Resize(maxTargets);
                }

                foreach (var obj in targets)
                {
                    if (obj.IsUnit())
                        AddUnitTarget(obj.ToUnit(), effMask, false, true, center);
                    else if (obj.IsGameObject())
                        AddGOTarget(obj.ToGameObject(), effMask);
                    else if (obj.IsCorpse())
                        AddCorpseTarget(obj.ToCorpse(), effMask);
                }
            }
        }

        void SelectImplicitCasterDestTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, SpellTargetIndex targetIndex)
        {
            SpellDestination dest = new(m_caster);

            switch (targetType.GetTarget())
            {
                case Targets.DestCaster:
                    break;
                case Targets.DestHome:
                    Player playerCaster = m_caster.ToPlayer();
                    if (playerCaster != null)
                        dest = new SpellDestination(playerCaster.GetHomebind());
                    break;
                case Targets.DestDb:
                    SpellTargetPosition st = Global.SpellMgr.GetSpellTargetPosition(m_spellInfo.Id, spellEffectInfo.EffectIndex);
                    if (st != null)
                    {
                        // @todo fix this check
                        if (m_spellInfo.HasEffect(SpellEffectName.TeleportUnits) || m_spellInfo.HasEffect(SpellEffectName.TeleportWithSpellVisualKitLoadingScreen) || m_spellInfo.HasEffect(SpellEffectName.Bind))
                            dest = new SpellDestination(st.target_X, st.target_Y, st.target_Z, st.target_Orientation, st.target_mapId);
                        else if (st.target_mapId == m_caster.GetMapId())
                            dest = new SpellDestination(st.target_X, st.target_Y, st.target_Z, st.target_Orientation);
                    }
                    else
                    {
                        Log.outDebug(LogFilter.Spells, "SPELL: unknown target coordinates for spell ID {0}", m_spellInfo.Id);
                        WorldObject target = m_targets.GetObjectTarget();
                        if (target != null)
                            dest = new SpellDestination(target);
                    }
                    break;
                case Targets.DestCasterFishing:
                {
                    float minDist = m_spellInfo.GetMinRange(true);
                    float maxDist = m_spellInfo.GetMaxRange(true);
                    float dis = RandomHelper.NextSingle() * (maxDist - minDist) + minDist;
                    float x, y, z;
                    float angle = RandomHelper.NextSingle() * (MathF.PI * 35.0f / 180.0f) - (MathF.PI * 17.5f / 180.0f);
                    m_caster.GetClosePoint(out x, out y, out z, SharedConst.DefaultPlayerBoundingRadius, dis, angle);

                    float ground = m_caster.GetMapHeight(x, y, z);
                    float liquidLevel = MapConst.VMAPInvalidHeightValue;
                    LiquidData liquidData;
                    if (m_caster.GetMap().GetLiquidStatus(m_caster.GetPhaseShift(), x, y, z, out liquidData, null, m_caster.GetCollisionHeight()) != 0)
                        liquidLevel = liquidData.level;

                    if (liquidLevel <= ground) // When there is no liquid Map.GetWaterOrGroundLevel returns ground level
                    {
                        SendCastResult(SpellCastResult.NotHere);
                        SendChannelUpdate(0, SpellCastResult.NotHere);
                        Finish(SpellCastResult.NotHere);
                        return;
                    }

                    if (ground + 0.75 > liquidLevel)
                    {
                        SendCastResult(SpellCastResult.TooShallow);
                        SendChannelUpdate(0, SpellCastResult.TooShallow);
                        Finish(SpellCastResult.TooShallow);
                        return;
                    }

                    dest = new SpellDestination(x, y, liquidLevel, m_caster.GetOrientation());
                    break;
                }
                case Targets.DestCasterFrontLeap:
                case Targets.DestCasterMovementDirection:
                {
                    Unit unitCaster = m_caster.ToUnit();
                    if (unitCaster == null)
                        break;

                    float dist = spellEffectInfo.CalcRadius(unitCaster, targetIndex);
                    float angle = targetType.CalcDirectionAngle();
                    if (targetType.GetTarget() == Targets.DestCasterMovementDirection)
                    {
                        switch (m_caster.m_movementInfo.GetMovementFlags() & (MovementFlag.Forward | MovementFlag.Backward | MovementFlag.StrafeLeft | MovementFlag.StrafeRight))
                        {
                            case MovementFlag.None:
                            case MovementFlag.Forward:
                            case MovementFlag.Forward | MovementFlag.Backward:
                            case MovementFlag.StrafeLeft | MovementFlag.StrafeRight:
                            case MovementFlag.Forward | MovementFlag.StrafeLeft | MovementFlag.StrafeRight:
                            case MovementFlag.Forward | MovementFlag.Backward | MovementFlag.StrafeLeft | MovementFlag.StrafeRight:
                                angle = 0.0f;
                                break;
                            case MovementFlag.Backward:
                            case MovementFlag.Backward | MovementFlag.StrafeLeft | MovementFlag.StrafeRight:
                                angle = MathF.PI;
                                break;
                            case MovementFlag.StrafeLeft:
                            case MovementFlag.Forward | MovementFlag.Backward | MovementFlag.StrafeLeft:
                                angle = (MathF.PI / 2);
                                break;
                            case MovementFlag.Forward | MovementFlag.StrafeLeft:
                                angle = (MathF.PI / 4);
                                break;
                            case MovementFlag.Backward | MovementFlag.StrafeLeft:
                                angle = (3 * MathF.PI / 4);
                                break;
                            case MovementFlag.StrafeRight:
                            case MovementFlag.Forward | MovementFlag.Backward | MovementFlag.StrafeRight:
                                angle = (-MathF.PI / 2);
                                break;
                            case MovementFlag.Forward | MovementFlag.StrafeRight:
                                angle = (-MathF.PI / 4);
                                break;
                            case MovementFlag.Backward | MovementFlag.StrafeRight:
                                angle = (-3 * MathF.PI / 4);
                                break;
                            default:
                                angle = 0.0f;
                                break;
                        }
                    }

                    Position pos = new(dest.Position);

                    MovePosition(pos, unitCaster, dist, angle);
                    dest.Relocate(pos);
                    break;
                }
                case Targets.DestCasterGround:
                case Targets.DestCasterGround2:
                    dest.Position.posZ = m_caster.GetMapWaterOrGroundLevel(dest.Position.GetPositionX(), dest.Position.GetPositionY(), dest.Position.GetPositionZ());
                    break;
                case Targets.DestSummoner:
                {
                    Unit unitCaster = m_caster.ToUnit();
                    if (unitCaster != null)
                    {
                        TempSummon casterSummon = unitCaster.ToTempSummon();
                        if (casterSummon != null)
                        {
                            WorldObject summoner = casterSummon.GetSummoner();
                            if (summoner != null)
                                dest = new SpellDestination(summoner);
                        }
                    }
                    break;
                }
                default:
                {
                    float dist = spellEffectInfo.CalcRadius(m_caster, targetIndex);
                    float angle = targetType.CalcDirectionAngle();
                    float objSize = m_caster.GetCombatReach();

                    switch (targetType.GetTarget())
                    {
                        case Targets.DestCasterSummon:
                            dist = SharedConst.PetFollowDist;
                            break;
                        case Targets.DestCasterRandom:
                            if (dist > objSize)
                                dist = objSize + (dist - objSize);
                            break;
                        case Targets.DestCasterFrontLeft:
                        case Targets.DestCasterBackLeft:
                        case Targets.DestCasterFrontRight:
                        case Targets.DestCasterBackRight:
                        {
                            float DefaultTotemDistance = 3.0f;
                            if (!spellEffectInfo.HasRadius(targetIndex))
                                dist = DefaultTotemDistance;
                            break;
                        }
                        default:
                            break;
                    }

                    if (dist < objSize)
                        dist = objSize;

                    Position pos = new(dest.Position);
                    MovePosition(pos, m_caster, dist, angle);

                    dest.Relocate(pos);
                    break;
                }
            }

            if (m_spellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
                dest.Position.SetOrientation(spellEffectInfo.PositionFacing);

            CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
            m_targets.SetDst(dest);
        }

        void SelectImplicitTargetDestTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, SpellTargetIndex targetIndex)
        {
            WorldObject target = m_targets.GetObjectTarget();

            SpellDestination dest = new(target);

            switch (targetType.GetTarget())
            {
                case Targets.DestTargetEnemy:
                case Targets.DestAny:
                case Targets.DestTargetAlly:
                    break;
                default:
                {
                    float angle = targetType.CalcDirectionAngle();
                    float dist = spellEffectInfo.CalcRadius(null, targetIndex);

                    Position pos = new(dest.Position);
                    MovePosition(pos, target, dist, angle);

                    dest.Relocate(pos);
                }
                break;
            }

            if (m_spellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
                dest.Position.SetOrientation(spellEffectInfo.PositionFacing);

            CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
            m_targets.SetDst(dest);
        }

        void SelectImplicitDestDestTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, SpellTargetIndex targetIndex)
        {
            // set destination to caster if no dest provided
            // can only happen if previous destination target could not be set for some reason
            // (not found nearby target, or channel target for example
            // maybe we should abort the spell in such case?
            CheckDst();

            SpellDestination dest = m_targets.GetDst();

            switch (targetType.GetTarget())
            {
                case Targets.DestDynobjEnemy:
                case Targets.DestDynobjAlly:
                case Targets.DestDynobjNone:
                case Targets.DestDest:
                    break;
                case Targets.DestDestGround:
                    dest.Position.posZ = m_caster.GetMapHeight(dest.Position.GetPositionX(), dest.Position.GetPositionY(), dest.Position.GetPositionZ());
                    break;
                case Targets.DestDestTargetTowardsCaster:
                {
                    float dist = spellEffectInfo.CalcRadius(m_caster, targetIndex);
                    Position pos = dest.Position;
                    float angle = pos.GetAbsoluteAngle(m_caster) - m_caster.GetOrientation();

                    MovePosition(pos, m_caster, dist, angle);
                    pos.SetOrientation(m_caster.GetAbsoluteAngle(dest.Position));

                    dest.Relocate(pos);
                    break;
                }
                default:
                {
                    float angle = targetType.CalcDirectionAngle();
                    float dist = spellEffectInfo.CalcRadius(m_caster, targetIndex);

                    Position pos = new(m_targets.GetDstPos());
                    MovePosition(pos, m_caster, dist, angle);

                    dest.Relocate(pos);
                }
                break;
            }

            if (m_spellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
                dest.Position.SetOrientation(spellEffectInfo.PositionFacing);

            CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
            m_targets.ModDst(dest);
        }

        void SelectImplicitCasterObjectTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
        {
            WorldObject target = null;
            bool checkIfValid = true;

            switch (targetType.GetTarget())
            {
                case Targets.UnitCaster:
                    target = m_caster;
                    checkIfValid = false;
                    break;
                case Targets.UnitMaster:
                    target = m_caster.GetCharmerOrOwner();
                    break;
                case Targets.UnitPet:
                {
                    Unit unitCaster = m_caster.ToUnit();
                    if (unitCaster != null)
                        target = unitCaster.GetGuardianPet();
                    break;
                }
                case Targets.UnitSummoner:
                {
                    Unit unitCaster = m_caster.ToUnit();
                    if (unitCaster != null)
                        if (unitCaster.IsSummon())
                            target = unitCaster.ToTempSummon().GetSummonerUnit();
                    break;
                }
                case Targets.UnitVehicle:
                {
                    Unit unitCaster = m_caster.ToUnit();
                    if (unitCaster != null)
                        target = unitCaster.GetVehicleBase();
                    break;
                }
                case Targets.UnitPassenger0:
                case Targets.UnitPassenger1:
                case Targets.UnitPassenger2:
                case Targets.UnitPassenger3:
                case Targets.UnitPassenger4:
                case Targets.UnitPassenger5:
                case Targets.UnitPassenger6:
                case Targets.UnitPassenger7:
                    Creature vehicleBase = m_caster.ToCreature();
                    if (vehicleBase != null && vehicleBase.IsVehicle())
                        target = vehicleBase.GetVehicleKit().GetPassenger((sbyte)(targetType.GetTarget() - Targets.UnitPassenger0));
                    break;
                case Targets.UnitTargetTapList:
                    Creature creatureCaster = m_caster.ToCreature();
                    if (creatureCaster != null && !creatureCaster.GetTapList().Empty())
                        target = Global.ObjAccessor.GetWorldObject(creatureCaster, creatureCaster.GetTapList().SelectRandom());
                    break;
                case Targets.UnitOwnCritter:
                {
                    Unit unitCaster = m_caster.ToUnit();
                    if (unitCaster != null)
                        target = ObjectAccessor.GetCreatureOrPetOrVehicle(m_caster, unitCaster.GetCritterGUID());
                    break;
                }
                default:
                    break;
            }

            CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, targetType);

            if (target != null)
            {
                if (target.IsUnit())
                    AddUnitTarget(target.ToUnit(), 1u << (int)spellEffectInfo.EffectIndex, checkIfValid);
                else if (target.IsGameObject())
                    AddGOTarget(target.ToGameObject(), 1u << (int)spellEffectInfo.EffectIndex);
                else if (target.IsCorpse())
                    AddCorpseTarget(target.ToCorpse(), 1u << (int)spellEffectInfo.EffectIndex);
            }
        }

        void SelectImplicitTargetObjectTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
        {
            WorldObject target = m_targets.GetObjectTarget();

            CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, targetType);

            Item item = m_targets.GetItemTarget();
            if (target != null)
            {
                if (target.IsUnit())
                    AddUnitTarget(target.ToUnit(), 1u << (int)spellEffectInfo.EffectIndex, true, false);
                else if (target.IsGameObject())
                    AddGOTarget(target.ToGameObject(), 1u << (int)spellEffectInfo.EffectIndex);
                else if (target.IsCorpse())
                    AddCorpseTarget(target.ToCorpse(), 1u << (int)spellEffectInfo.EffectIndex);

                SelectImplicitChainTargets(spellEffectInfo, targetType, target, 1u << (int)spellEffectInfo.EffectIndex);
            }
            // Script hook can remove object target and we would wrongly land here
            else if (item != null)
                AddItemTarget(item, 1u << (int)spellEffectInfo.EffectIndex);
        }

        void SelectImplicitChainTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, WorldObject target, uint effMask)
        {
            int maxTargets = spellEffectInfo.ChainTargets;
            Player modOwner = m_caster.GetSpellModOwner();
            if (modOwner != null)
                modOwner.ApplySpellMod(m_spellInfo, SpellModOp.ChainTargets, ref maxTargets, this);

            if (maxTargets > 1)
            {
                // mark damage multipliers as used
                for (int k = (int)spellEffectInfo.EffectIndex; k < m_spellInfo.GetEffects().Count; ++k)
                    if (Convert.ToBoolean(effMask & (1 << (int)k)))
                        m_damageMultipliers[spellEffectInfo.EffectIndex] = 1.0f;

                m_applyMultiplierMask |= effMask;

                List<WorldObject> targets = new();
                SearchChainTargets(targets, (uint)maxTargets - 1, target, targetType.GetObjectType(), targetType.GetCheckType(), spellEffectInfo, targetType.GetTarget() == Targets.UnitChainhealAlly);

                // Chain primary target is added earlier
                CallScriptObjectAreaTargetSelectHandlers(targets, spellEffectInfo.EffectIndex, targetType);

                Position losPosition = m_spellInfo.HasAttribute(SpellAttr2.ChainFromCaster) ? m_caster : target;

                foreach (var obj in targets)
                {
                    Unit unitTarget = obj.ToUnit();
                    if (unitTarget != null)
                        AddUnitTarget(unitTarget, effMask, false, true, losPosition);

                    if (!m_spellInfo.HasAttribute(SpellAttr2.ChainFromCaster) && !spellEffectInfo.EffectAttributes.HasFlag(SpellEffectAttributes.ChainFromInitialTarget))
                        losPosition = obj;
                }
            }
        }

        float Tangent(float x)
        {
            x = (float)Math.Tan(x);
            if (x < 100000.0f && x > -100000.0f) return x;
            if (x >= 100000.0f) return 100000.0f;
            if (x <= 100000.0f) return -100000.0f;
            return 0.0f;
        }

        void SelectImplicitTrajTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
        {
            if (!m_targets.HasTraj())
                return;

            float dist2d = m_targets.GetDist2d();
            if (dist2d == 0)
                return;

            Position srcPos = m_targets.GetSrcPos();
            srcPos.SetOrientation(m_caster.GetOrientation());
            float srcToDestDelta = m_targets.GetDstPos().posZ - srcPos.posZ;

            List<WorldObject> targets = new();
            var spellTraj = new WorldObjectSpellTrajTargetCheck(dist2d, srcPos, m_caster, m_spellInfo, targetType.GetCheckType(), spellEffectInfo.ImplicitTargetConditions, SpellTargetObjectTypes.None);
            var searcher = new WorldObjectListSearcher(m_caster, targets, spellTraj);
            SearchTargets(searcher, GridMapTypeMask.All, m_caster, srcPos, dist2d);
            if (targets.Empty())
                return;

            targets.Sort(new ObjectDistanceOrderPred(m_caster));

            float b = Tangent(m_targets.GetPitch());
            float a = (srcToDestDelta - dist2d * b) / (dist2d * dist2d);
            if (a > -0.0001f)
                a = 0f;

            // We should check if triggered spell has greater range (which is true in many cases, and initial spell has too short max range)
            // limit max range to 300 yards, sometimes triggered spells can have 50000yds
            float bestDist = m_spellInfo.GetMaxRange(false);
            SpellInfo triggerSpellInfo = Global.SpellMgr.GetSpellInfo(spellEffectInfo.TriggerSpell, GetCastDifficulty());
            if (triggerSpellInfo != null)
                bestDist = Math.Min(Math.Max(bestDist, triggerSpellInfo.GetMaxRange(false)), Math.Min(dist2d, 300.0f));

            // GameObjects don't cast traj
            Unit unitCaster = m_caster.ToUnit();
            foreach (var obj in targets)
            {
                if (m_spellInfo.CheckTarget(unitCaster, obj, true) != SpellCastResult.SpellCastOk)
                    continue;

                Unit unitTarget = obj.ToUnit();
                if (unitTarget != null)
                {
                    if (unitCaster == obj || unitCaster.IsOnVehicle(unitTarget) || unitTarget.GetVehicle() != null)
                        continue;

                    Creature creatureTarget = unitTarget.ToCreature();
                    if (creatureTarget != null)
                    {
                        if (creatureTarget.GetCreatureDifficulty().TypeFlags.HasFlag(CreatureTypeFlags.CollideWithMissiles))
                            continue;
                    }
                }

                float size = Math.Max(obj.GetCombatReach(), 1.0f);
                float objDist2d = srcPos.GetExactDist2d(obj);
                float dz = obj.GetPositionZ() - srcPos.posZ;

                float horizontalDistToTraj = (float)Math.Abs(objDist2d * Math.Sin(srcPos.GetRelativeAngle(obj)));
                float sizeFactor = (float)Math.Cos((horizontalDistToTraj / size) * (Math.PI / 2.0f));
                float distToHitPoint = (float)Math.Max(objDist2d * Math.Cos(srcPos.GetRelativeAngle(obj)) - size * sizeFactor, 0.0f);
                float height = distToHitPoint * (a * distToHitPoint + b);

                if (Math.Abs(dz - height) > size + b / 2.0f + SpellConst.TrajectoryMissileSize)
                    continue;

                if (distToHitPoint < bestDist)
                {
                    bestDist = distToHitPoint;
                    break;

                }
            }

            if (dist2d > bestDist)
            {
                float x = (float)(m_targets.GetSrcPos().posX + Math.Cos(unitCaster.GetOrientation()) * bestDist);
                float y = (float)(m_targets.GetSrcPos().posY + Math.Sin(unitCaster.GetOrientation()) * bestDist);
                float z = m_targets.GetSrcPos().posZ + bestDist * (a * bestDist + b);

                SpellDestination dest = new(x, y, z, unitCaster.GetOrientation());
                if (m_spellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
                    dest.Position.SetOrientation(spellEffectInfo.PositionFacing);

                CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
                m_targets.ModDst(dest);
            }
        }

        void SelectImplicitLineTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, SpellTargetIndex targetIndex, uint effMask)
        {
            List<WorldObject> targets = new();
            SpellTargetObjectTypes objectType = targetType.GetObjectType();
            SpellTargetCheckTypes selectionType = targetType.GetCheckType();

            Position dst;
            switch (targetType.GetReferenceType())
            {
                case SpellTargetReferenceTypes.Src:
                    dst = m_targets.GetSrcPos();
                    break;
                case SpellTargetReferenceTypes.Dest:
                    dst = m_targets.GetDstPos();
                    break;
                case SpellTargetReferenceTypes.Caster:
                    dst = m_caster;
                    break;
                case SpellTargetReferenceTypes.Target:
                    dst = m_targets.GetUnitTarget();
                    break;
                default:
                    Cypher.Assert(false, "Spell.SelectImplicitLineTargets: received not implemented target reference type");
                    return;
            }

            var condList = spellEffectInfo.ImplicitTargetConditions;
            float radius = spellEffectInfo.CalcRadius(m_caster, targetIndex) * m_spellValue.RadiusMod;

            GridMapTypeMask containerTypeMask = GetSearcherTypeMask(m_spellInfo, spellEffectInfo, objectType, condList);
            if (containerTypeMask != 0)
            {
                WorldObjectSpellLineTargetCheck check = new(m_caster, dst, m_spellInfo.Width != 0 ? m_spellInfo.Width : m_caster.GetCombatReach(), radius, m_caster, m_spellInfo, selectionType, condList, objectType);
                WorldObjectListSearcher searcher = new(m_caster, targets, check, containerTypeMask);
                SearchTargets(searcher, containerTypeMask, m_caster, m_caster, radius);

                CallScriptObjectAreaTargetSelectHandlers(targets, spellEffectInfo.EffectIndex, targetType);

                if (!targets.Empty())
                {
                    // Other special target selection goes here
                    uint maxTargets = m_spellValue.MaxAffectedTargets;
                    if (maxTargets != 0)
                    {
                        if (maxTargets < targets.Count)
                        {
                            targets.Sort(new ObjectDistanceOrderPred(m_caster));
                            targets.Resize(maxTargets);
                        }
                    }

                    foreach (var obj in targets)
                    {
                        if (obj.IsUnit())
                            AddUnitTarget(obj.ToUnit(), effMask, false);
                        else if (obj.IsGameObject())
                            AddGOTarget(obj.ToGameObject(), effMask);
                        else if (obj.IsCorpse())
                            AddCorpseTarget(obj.ToCorpse(), effMask);
                    }
                }
            }
        }

        void SelectEffectTypeImplicitTargets(SpellEffectInfo spellEffectInfo)
        {
            // special case for SPELL_EFFECT_SUMMON_RAF_FRIEND and SPELL_EFFECT_SUMMON_PLAYER, queue them on map for later execution
            switch (spellEffectInfo.Effect)
            {
                case SpellEffectName.SummonRafFriend:
                case SpellEffectName.SummonPlayer:
                    if (m_caster.IsTypeId(TypeId.Player) && !m_caster.ToPlayer().GetTarget().IsEmpty())
                    {
                        WorldObject rafTarget = Global.ObjAccessor.FindPlayer(m_caster.ToPlayer().GetTarget());

                        CallScriptObjectTargetSelectHandlers(ref rafTarget, spellEffectInfo.EffectIndex, new SpellImplicitTargetInfo());

                        // scripts may modify the target - recheck
                        if (rafTarget != null && rafTarget.IsPlayer())
                        {
                            // target is not stored in target map for those spells
                            // since we're completely skipping AddUnitTarget logic, we need to check immunity manually
                            // eg. aura 21546 makes target immune to summons
                            Player player = rafTarget.ToPlayer();
                            if (player.IsImmunedToSpellEffect(m_spellInfo, spellEffectInfo, null))
                                return;

                            var spell = this;
                            var targetGuid = rafTarget.GetGUID();
                            rafTarget.GetMap().AddFarSpellCallback(map =>
                            {
                                Player player = Global.ObjAccessor.GetPlayer(map, targetGuid);
                                if (player == null)
                                    return;

                                // check immunity again in case it changed during update
                                if (player.IsImmunedToSpellEffect(spell.GetSpellInfo(), spellEffectInfo, null))
                                    return;

                                spell.HandleEffects(player, null, null, null, spellEffectInfo, SpellEffectHandleMode.HitTarget);
                            });
                        }
                    }
                    return;
                default:
                    break;
            }

            // select spell implicit targets based on effect type
            if (spellEffectInfo.GetImplicitTargetType() == 0)
                return;

            SpellCastTargetFlags targetMask = spellEffectInfo.GetMissingTargetMask();

            if (targetMask == 0)
                return;

            WorldObject target = null;

            switch (spellEffectInfo.GetImplicitTargetType())
            {
                // add explicit object target or self to the target map
                case SpellEffectImplicitTargetTypes.Explicit:
                    // player which not released his spirit is Unit, but target flag for it is TARGET_FLAG_CORPSE_MASK
                    if (Convert.ToBoolean(targetMask & (SpellCastTargetFlags.UnitMask | SpellCastTargetFlags.CorpseMask)))
                    {
                        Unit unitTarget = m_targets.GetUnitTarget();
                        if (unitTarget != null)
                            target = unitTarget;
                        else if (Convert.ToBoolean(targetMask & SpellCastTargetFlags.CorpseMask))
                        {
                            Corpse corpseTarget = m_targets.GetCorpseTarget();
                            if (corpseTarget != null)
                                target = corpseTarget;
                        }
                        else //if (targetMask & TARGET_FLAG_UNIT_MASK)
                            target = m_caster;
                    }
                    if (Convert.ToBoolean(targetMask & SpellCastTargetFlags.ItemMask))
                    {
                        Item itemTarget = m_targets.GetItemTarget();
                        if (itemTarget != null)
                            AddItemTarget(itemTarget, (uint)(1 << (int)spellEffectInfo.EffectIndex));
                        return;
                    }
                    if (Convert.ToBoolean(targetMask & SpellCastTargetFlags.GameobjectMask))
                        target = m_targets.GetGOTarget();
                    break;
                // add self to the target map
                case SpellEffectImplicitTargetTypes.Caster:
                    if (Convert.ToBoolean(targetMask & SpellCastTargetFlags.UnitMask))
                        target = m_caster;
                    break;
                default:
                    break;
            }

            CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, new SpellImplicitTargetInfo());

            if (target != null)
            {
                if (target.IsUnit())
                    AddUnitTarget(target.ToUnit(), 1u << (int)spellEffectInfo.EffectIndex, false);
                else if (target.IsGameObject())
                    AddGOTarget(target.ToGameObject(), 1u << (int)spellEffectInfo.EffectIndex);
                else if (target.IsCorpse())
                    AddCorpseTarget(target.ToCorpse(), 1u << (int)spellEffectInfo.EffectIndex);
            }
        }

        public static GridMapTypeMask GetSearcherTypeMask(SpellInfo spellInfo, SpellEffectInfo spellEffectInfo, SpellTargetObjectTypes objType, List<Condition> condList)
        {
            // this function selects which containers need to be searched for spell target
            GridMapTypeMask retMask = GridMapTypeMask.All;

            // filter searchers based on searched object type
            switch (objType)
            {
                case SpellTargetObjectTypes.Unit:
                case SpellTargetObjectTypes.UnitAndDest:
                    retMask &= GridMapTypeMask.Player | GridMapTypeMask.Creature;
                    break;
                case SpellTargetObjectTypes.Corpse:
                case SpellTargetObjectTypes.CorpseEnemy:
                case SpellTargetObjectTypes.CorpseAlly:
                    retMask &= GridMapTypeMask.Player | GridMapTypeMask.Corpse | GridMapTypeMask.Creature;
                    break;
                case SpellTargetObjectTypes.Gobj:
                case SpellTargetObjectTypes.GobjItem:
                    retMask &= GridMapTypeMask.GameObject;
                    break;
                default:
                    break;
            }

            if (spellInfo.HasAttribute(SpellAttr3.OnlyOnPlayer) || spellEffectInfo.EffectAttributes.HasFlag(SpellEffectAttributes.PlayersOnly))
                retMask &= GridMapTypeMask.Corpse | GridMapTypeMask.Player;
            if (spellInfo.HasAttribute(SpellAttr3.OnlyOnGhosts))
                retMask &= GridMapTypeMask.Player;
            if (spellInfo.HasAttribute(SpellAttr5.NotOnPlayer))
                retMask &= ~GridMapTypeMask.Player;

            if (condList != null)
                retMask &= Global.ConditionMgr.GetSearcherTypeMaskForConditionList(condList);
            return retMask;
        }

        public static void SearchTargets(Notifier notifier, GridMapTypeMask containerMask, WorldObject referer, Position pos, float radius)
        {
            if (containerMask == 0)
                return;

            bool searchInGrid = containerMask.HasAnyFlag(GridMapTypeMask.Creature | GridMapTypeMask.GameObject);
            bool searchInWorld = containerMask.HasAnyFlag(GridMapTypeMask.Creature | GridMapTypeMask.Player | GridMapTypeMask.Corpse);
            if (searchInGrid || searchInWorld)
            {
                float x = pos.GetPositionX();
                float y = pos.GetPositionY();

                CellCoord p = GridDefines.ComputeCellCoord(x, y);
                Cell cell = new(p);
                cell.SetNoCreate();

                Map map = referer.GetMap();

                if (searchInWorld)
                    Cell.VisitWorldObjects(x, y, map, notifier, radius);

                if (searchInGrid)
                    Cell.VisitGridObjects(x, y, map, notifier, radius);
            }
        }

        WorldObject SearchNearbyTarget(SpellEffectInfo spellEffectInfo, float range, SpellTargetObjectTypes objectType, SpellTargetCheckTypes selectionType, List<Condition> condList)
        {
            GridMapTypeMask containerTypeMask = GetSearcherTypeMask(m_spellInfo, spellEffectInfo, objectType, condList);
            if (containerTypeMask == 0)
                return null;

            var check = new WorldObjectSpellNearbyTargetCheck(range, m_caster, m_spellInfo, selectionType, condList, objectType);
            var searcher = new WorldObjectLastSearcher(m_caster, check, containerTypeMask);
            SearchTargets(searcher, containerTypeMask, m_caster, m_caster.GetPosition(), range);
            return searcher.GetTarget();
        }

        void SearchAreaTargets(List<WorldObject> targets, SpellEffectInfo spellEffectInfo, float range, Position position, WorldObject referer,
            SpellTargetObjectTypes objectType, SpellTargetCheckTypes selectionType, List<Condition> condList, WorldObjectSpellAreaTargetSearchReason searchReason)
        {
            var containerTypeMask = GetSearcherTypeMask(m_spellInfo, spellEffectInfo, objectType, condList);
            if (containerTypeMask == 0)
                return;

            float extraSearchRadius = range > 0.0f ? SharedConst.ExtraCellSearchRadius : 0.0f;
            var check = new WorldObjectSpellAreaTargetCheck(range, position, m_caster, referer, m_spellInfo, selectionType, condList, objectType, searchReason);
            var searcher = new WorldObjectListSearcher(m_caster, targets, check, containerTypeMask);
            SearchTargets(searcher, containerTypeMask, m_caster, position, range + extraSearchRadius);
        }

        void SearchChainTargets(List<WorldObject> targets, uint chainTargets, WorldObject target, SpellTargetObjectTypes objectType, SpellTargetCheckTypes selectType, SpellEffectInfo spellEffectInfo, bool isChainHeal)
        {
            // max dist for jump target selection
            float jumpRadius = 0.0f;
            switch (m_spellInfo.DmgClass)
            {
                case SpellDmgClass.Ranged:
                    // 7.5y for multi shot
                    jumpRadius = 7.5f;
                    break;
                case SpellDmgClass.Melee:
                    // 5y for swipe, cleave and similar
                    jumpRadius = 5.0f;
                    break;
                case SpellDmgClass.None:
                case SpellDmgClass.Magic:
                    // 12.5y for chain heal spell since 3.2 patch
                    if (isChainHeal)
                        jumpRadius = 12.5f;
                    // 10y as default for magic chain spells
                    else
                        jumpRadius = 10.0f;
                    break;
            }

            Player modOwner = m_caster.GetSpellModOwner();
            if (modOwner != null)
                modOwner.ApplySpellMod(m_spellInfo, SpellModOp.ChainJumpDistance, ref jumpRadius, this);

            float searchRadius;
            if (m_spellInfo.HasAttribute(SpellAttr2.ChainFromCaster))
                searchRadius = GetMinMaxRange(false).maxRange;
            else if (spellEffectInfo.EffectAttributes.HasFlag(SpellEffectAttributes.ChainFromInitialTarget))
                searchRadius = jumpRadius;
            else
                searchRadius = jumpRadius * chainTargets;

            WorldObject chainSource = m_spellInfo.HasAttribute(SpellAttr2.ChainFromCaster) ? m_caster : target;
            List<WorldObject> tempTargets = new();
            SearchAreaTargets(tempTargets, spellEffectInfo, searchRadius, chainSource, m_caster, objectType, selectType, spellEffectInfo.ImplicitTargetConditions, WorldObjectSpellAreaTargetSearchReason.Chain);
            tempTargets.Remove(target);

            // remove targets which are always invalid for chain spells
            // for some spells allow only chain targets in front of caster (swipe for example)
            if (m_spellInfo.HasAttribute(SpellAttr5.MeleeChainTargeting))
                tempTargets.RemoveAll(obj => !m_caster.HasInArc(MathF.PI, obj));

            while (chainTargets != 0)
            {
                // try to get unit for next chain jump
                WorldObject foundObj = null;
                // get unit with highest hp deficit in dist
                if (isChainHeal)
                {
                    uint maxHPDeficit = 0;
                    foreach (var obj in tempTargets)
                    {
                        Unit unitTarget = obj.ToUnit();
                        if (unitTarget != null)
                        {
                            uint deficit = (uint)(unitTarget.GetMaxHealth() - unitTarget.GetHealth());
                            if ((deficit > maxHPDeficit || foundObj == null) && chainSource.IsWithinDist(unitTarget, jumpRadius) && IsWithinLOS(chainSource, unitTarget, false, ModelIgnoreFlags.M2))
                            {
                                foundObj = obj;
                                maxHPDeficit = deficit;
                            }
                        }
                    }
                }
                // get closest object
                else
                {
                    foreach (var obj in tempTargets)
                    {
                        bool isBestDistanceMatch = foundObj != null ? chainSource.GetDistanceOrder(obj, foundObj) : chainSource.IsWithinDist(obj, jumpRadius);
                        if (!isBestDistanceMatch)
                            continue;

                        if (!IsWithinLOS(chainSource, obj, false, ModelIgnoreFlags.M2))
                            continue;

                        if (spellEffectInfo.EffectAttributes.HasFlag(SpellEffectAttributes.EnforceLineOfSightToChainTargets) && !IsWithinLOS(m_caster, obj, false, ModelIgnoreFlags.M2))
                            continue;

                        foundObj = obj;
                    }
                }
                // not found any valid target - chain ends
                if (foundObj == null)
                    break;

                if (!m_spellInfo.HasAttribute(SpellAttr2.ChainFromCaster) && !spellEffectInfo.EffectAttributes.HasFlag(SpellEffectAttributes.ChainFromInitialTarget))
                    chainSource = foundObj;

                targets.Add(foundObj);
                tempTargets.Remove(foundObj);
                --chainTargets;
            }
        }

        GameObject SearchSpellFocus()
        {
            var check = new GameObjectFocusCheck(m_caster, m_spellInfo.RequiresSpellFocus);
            var searcher = new GameObjectSearcher(m_caster, check);
            SearchTargets(searcher, GridMapTypeMask.GameObject, m_caster, m_caster, m_caster.GetVisibilityRange());
            return searcher.GetTarget();
        }

        void PrepareDataForTriggerSystem()
        {
            //==========================================================================================
            // Now fill data for trigger system, need know:
            // Create base triggers flags for Attacker and Victim (m_procAttacker, m_procVictim and m_hitMask)
            //==========================================================================================

            m_procVictim = m_procAttacker = new ProcFlagsInit();
            // Get data for type of attack and fill base info for trigger
            switch (m_spellInfo.DmgClass)
            {
                case SpellDmgClass.Melee:
                    m_procAttacker = new ProcFlagsInit(ProcFlags.DealMeleeAbility);
                    if (m_attackType == WeaponAttackType.OffAttack)
                        m_procAttacker.Or(ProcFlags.OffHandWeaponSwing);
                    else
                        m_procAttacker.Or(ProcFlags.MainHandWeaponSwing);
                    m_procVictim = new ProcFlagsInit(ProcFlags.TakeMeleeAbility);
                    break;
                case SpellDmgClass.Ranged:
                    // Auto attack
                    if (m_spellInfo.HasAttribute(SpellAttr2.AutoRepeat))
                    {
                        m_procAttacker = new ProcFlagsInit(ProcFlags.DealRangedAttack);
                        m_procVictim = new ProcFlagsInit(ProcFlags.TakeRangedAttack);
                    }
                    else // Ranged spell attack
                    {
                        m_procAttacker = new ProcFlagsInit(ProcFlags.DealRangedAbility);
                        m_procVictim = new ProcFlagsInit(ProcFlags.TakeRangedAbility);
                    }
                    break;
                default:
                    if (m_spellInfo.EquippedItemClass == ItemClass.Weapon &&
                        Convert.ToBoolean(m_spellInfo.EquippedItemSubClassMask & (1 << (int)ItemSubClassWeapon.Wand))
                        && m_spellInfo.HasAttribute(SpellAttr2.AutoRepeat)) // Wands auto attack
                    {
                        m_procAttacker = new ProcFlagsInit(ProcFlags.DealRangedAttack);
                        m_procVictim = new ProcFlagsInit(ProcFlags.TakeRangedAttack);
                    }
                    break;
                    // For other spells trigger procflags are set in Spell::TargetInfo::DoDamageAndTriggers
                    // Because spell positivity is dependant on target
            }
        }

        public void CleanupTargetList()
        {
            m_UniqueTargetInfo.Clear();
            m_UniqueGOTargetInfo.Clear();
            m_UniqueItemInfo.Clear();
            m_delayMoment = 0;
        }

        void AddUnitTarget(Unit target, uint effectMask, bool checkIfValid = true, bool Implicit = true, Position losPosition = null)
        {
            foreach (var spellEffectInfo in m_spellInfo.GetEffects())
                if (!spellEffectInfo.IsEffect() || !CheckEffectTarget(target, spellEffectInfo, losPosition))
                    effectMask &= ~(1u << (int)spellEffectInfo.EffectIndex);

            // no effects left
            if (effectMask == 0)
                return;

            if (checkIfValid)
                if (m_spellInfo.CheckTarget(m_caster, target, Implicit) != SpellCastResult.SpellCastOk) // skip stealth checks for AOE
                    return;

            // Check for effect immune skip if immuned
            foreach (var spellEffectInfo in m_spellInfo.GetEffects())
                if (target.IsImmunedToSpellEffect(m_spellInfo, spellEffectInfo, m_caster))
                    effectMask &= ~(1u << (int)spellEffectInfo.EffectIndex);

            ObjectGuid targetGUID = target.GetGUID();

            // Lookup target in already in list
            var index = m_UniqueTargetInfo.FindIndex(target => target.TargetGUID == targetGUID);
            if (index != -1) // Found in list
            {
                // Immune effects removed from mask
                m_UniqueTargetInfo[index].EffectMask |= effectMask;
                return;
            }

            // This is new target calculate data for him

            // Get spell hit result on target
            TargetInfo targetInfo = new();
            targetInfo.TargetGUID = targetGUID;                         // Store target GUID
            targetInfo.EffectMask = effectMask;                         // Store all effects not immune
            targetInfo.IsAlive = target.IsAlive();

            // Calculate hit result
            WorldObject caster = m_originalCaster != null ? m_originalCaster : m_caster;
            targetInfo.MissCondition = caster.SpellHitResult(target, m_spellInfo, m_canReflect && !(IsPositive() && m_caster.IsFriendlyTo(target)));

            // Spell have speed - need calculate incoming time
            // Incoming time is zero for self casts. At least I think so.
            if (m_caster != target)
            {
                float hitDelay = m_spellInfo.LaunchDelay;
                WorldObject missileSource = m_caster;
                if (m_spellInfo.HasAttribute(SpellAttr4.BouncyChainMissiles))
                {
                    var previousTargetInfo = m_UniqueTargetInfo.FindLast(target => (target.EffectMask & effectMask) != 0);
                    if (previousTargetInfo != null)
                    {
                        hitDelay = 0.0f; // this is not the first target in chain, LaunchDelay was already included

                        WorldObject previousTarget = Global.ObjAccessor.GetWorldObject(m_caster, previousTargetInfo.TargetGUID);
                        if (previousTarget != null)
                            missileSource = previousTarget;

                        targetInfo.TimeDelay += previousTargetInfo.TimeDelay;
                    }
                }

                if (m_spellInfo.HasAttribute(SpellAttr9.MissileSpeedIsDelayInSec))
                    hitDelay += m_spellInfo.Speed;
                else if (m_spellInfo.Speed > 0.0f)
                {
                    // calculate spell incoming interval
                    /// @todo this is a hack
                    float dist = Math.Max(missileSource.GetDistance(target.GetPositionX(), target.GetPositionY(), target.GetPositionZ()), 5.0f);
                    hitDelay += dist / m_spellInfo.Speed;
                }

                targetInfo.TimeDelay += (ulong)Math.Floor(hitDelay * 1000.0f);
            }
            else
                targetInfo.TimeDelay = 0L;

            // If target reflect spell back to caster
            if (targetInfo.MissCondition == SpellMissInfo.Reflect)
            {
                // Calculate reflected spell result on caster (shouldn't be able to reflect gameobject spells)
                Unit unitCaster = m_caster.ToUnit();
                targetInfo.ReflectResult = unitCaster.SpellHitResult(unitCaster, m_spellInfo, false); // can't reflect twice

                // Proc spell reflect aura when missile hits the original target
                target.m_Events.AddEvent(new ProcReflectDelayed(target, m_originalCasterGUID), target.m_Events.CalculateTime(TimeSpan.FromMilliseconds(targetInfo.TimeDelay)));

                // Increase time interval for reflected spells by 1.5
                targetInfo.TimeDelay += targetInfo.TimeDelay >> 1;
            }
            else
                targetInfo.ReflectResult = SpellMissInfo.None;

            // Calculate minimum incoming time
            if (targetInfo.TimeDelay != 0 && (m_delayMoment == 0 || m_delayMoment > targetInfo.TimeDelay))
                m_delayMoment = targetInfo.TimeDelay;

            // Add target to list
            m_UniqueTargetInfo.Add(targetInfo);
        }

        void AddGOTarget(GameObject go, uint effectMask)
        {
            foreach (var spellEffectInfo in m_spellInfo.GetEffects())
                if (!spellEffectInfo.IsEffect() || !CheckEffectTarget(go, spellEffectInfo))
                    effectMask &= ~(1u << (int)spellEffectInfo.EffectIndex);

            // no effects left
            if (effectMask == 0)
                return;

            ObjectGuid targetGUID = go.GetGUID();

            // Lookup target in already in list
            var index = m_UniqueGOTargetInfo.FindIndex(target => target.TargetGUID == targetGUID);
            if (index != -1) // Found in list
            {
                // Add only effect mask
                m_UniqueGOTargetInfo[index].EffectMask |= effectMask;
                return;
            }

            // This is new target calculate data for him
            GOTargetInfo target = new();
            target.TargetGUID = targetGUID;
            target.EffectMask = effectMask;

            // Spell have speed - need calculate incoming time
            if (m_caster != go)
            {
                float hitDelay = m_spellInfo.LaunchDelay;
                if (m_spellInfo.HasAttribute(SpellAttr9.MissileSpeedIsDelayInSec))
                    hitDelay += m_spellInfo.Speed;
                else if (m_spellInfo.Speed > 0.0f)
                {
                    // calculate spell incoming interval
                    float dist = Math.Max(m_caster.GetDistance(go.GetPositionX(), go.GetPositionY(), go.GetPositionZ()), 5.0f);
                    hitDelay += dist / m_spellInfo.Speed;
                }

                target.TimeDelay = (ulong)Math.Floor(hitDelay * 1000.0f);
            }
            else
                target.TimeDelay = 0UL;

            // Calculate minimum incoming time
            if (target.TimeDelay != 0 && (m_delayMoment == 0 || m_delayMoment > target.TimeDelay))
                m_delayMoment = target.TimeDelay;

            // Add target to list
            m_UniqueGOTargetInfo.Add(target);
        }

        void AddItemTarget(Item item, uint effectMask)
        {
            foreach (var spellEffectInfo in m_spellInfo.GetEffects())
                if (!spellEffectInfo.IsEffect() || !CheckEffectTarget(item, spellEffectInfo))
                    effectMask &= ~(1u << (int)spellEffectInfo.EffectIndex);

            // no effects left
            if (effectMask == 0)
                return;

            // Lookup target in already in list
            var index = m_UniqueItemInfo.FindIndex(target => target.TargetItem == item);
            if (index != -1) // Found in list
            {
                // Add only effect mask
                m_UniqueItemInfo[index].EffectMask |= effectMask;
                return;
            }

            // This is new target add data

            ItemTargetInfo target = new();
            target.TargetItem = item;
            target.EffectMask = effectMask;

            m_UniqueItemInfo.Add(target);
        }

        void AddCorpseTarget(Corpse corpse, uint effectMask)
        {
            foreach (SpellEffectInfo spellEffectInfo in m_spellInfo.GetEffects())
                if (!spellEffectInfo.IsEffect())
                    effectMask &= ~(1u << (int)spellEffectInfo.EffectIndex);

            // no effects left
            if (effectMask == 0)
                return;

            ObjectGuid targetGUID = corpse.GetGUID();

            // Lookup target in already in list
            var corpseTargetInfo = m_UniqueCorpseTargetInfo.Find(target => { return target.TargetGUID == targetGUID; });
            if (corpseTargetInfo != null) // Found in list
            {
                // Add only effect mask
                corpseTargetInfo.EffectMask |= effectMask;
                return;
            }

            // This is new target calculate data for him
            CorpseTargetInfo target = new();
            target.TargetGUID = targetGUID;
            target.EffectMask = effectMask;

            // Spell have speed - need calculate incoming time
            if (m_caster != corpse)
            {
                float hitDelay = m_spellInfo.LaunchDelay;
                if (m_spellInfo.HasAttribute(SpellAttr9.MissileSpeedIsDelayInSec))
                    hitDelay += m_spellInfo.Speed;
                else if (m_spellInfo.Speed > 0.0f)
                {
                    // calculate spell incoming interval
                    float dist = Math.Max(m_caster.GetDistance(corpse.GetPositionX(), corpse.GetPositionY(), corpse.GetPositionZ()), 5.0f);
                    hitDelay += dist / m_spellInfo.Speed;
                }

                target.TimeDelay = (ulong)Math.Floor(hitDelay * 1000.0f);
            }
            else
                target.TimeDelay = 0;

            // Calculate minimum incoming time
            if (target.TimeDelay != 0 && (m_delayMoment == 0 || m_delayMoment > target.TimeDelay))
                m_delayMoment = target.TimeDelay;

            // Add target to list
            m_UniqueCorpseTargetInfo.Add(target);
        }

        void AddDestTarget(SpellDestination dest, uint effIndex)
        {
            m_destTargets[effIndex] = dest;
        }

        int GetUnitTargetIndexForEffect(ObjectGuid target, uint effect)
        {
            int index = 0;
            foreach (TargetInfo uniqueTargetInfo in m_UniqueTargetInfo)
            {
                if (uniqueTargetInfo.MissCondition == SpellMissInfo.None && (uniqueTargetInfo.EffectMask & (1 << (int)effect)) != 0)
                {
                    if (uniqueTargetInfo.TargetGUID == target)
                        break;

                    ++index;
                }
            }

            return index;
        }

        public long GetUnitTargetCountForEffect(uint effect)
        {
            return m_UniqueTargetInfo.Count(targetInfo => targetInfo.MissCondition == SpellMissInfo.None && (targetInfo.EffectMask & (1 << (int)effect)) != 0);
        }

        public long GetGameObjectTargetCountForEffect(uint effect)
        {
            return m_UniqueGOTargetInfo.Count(targetInfo => (targetInfo.EffectMask & (1 << (int)effect)) != 0);
        }

        public long GetItemTargetCountForEffect(uint effect)
        {
            return m_UniqueItemInfo.Count(targetInfo => (targetInfo.EffectMask & (1 << (int)effect)) != 0);
        }

        public long GetCorpseTargetCountForEffect(uint effect)
        {
            return m_UniqueCorpseTargetInfo.Count(targetInfo => (targetInfo.EffectMask & (1u << (int)effect)) != 0);
        }

        public SpellMissInfo PreprocessSpellHit(Unit unit, TargetInfo hitInfo)
        {
            if (unit == null)
                return SpellMissInfo.Evade;

            // Target may have begun evading between launch and hit phases - re-check now
            Creature creatureTarget = unit.ToCreature();
            if (creatureTarget != null && creatureTarget.IsEvadingAttacks())
                return SpellMissInfo.Evade;

            // For delayed spells immunity may be applied between missile launch and hit - check immunity for that case
            if (m_spellInfo.HasHitDelay() && unit.IsImmunedToSpell(m_spellInfo, m_caster))
                return SpellMissInfo.Immune;

            CallScriptBeforeHitHandlers(hitInfo.MissCondition);

            Player player = unit.ToPlayer();
            if (player != null)
            {
                player.FailCriteria(CriteriaFailEvent.BeSpellTarget, m_spellInfo.Id);
                player.StartCriteria(CriteriaStartEvent.BeSpellTarget, m_spellInfo.Id);
                player.UpdateCriteria(CriteriaType.BeSpellTarget, m_spellInfo.Id, 0, 0, m_caster);
            }

            Player casterPlayer = m_caster.ToPlayer();
            if (casterPlayer != null)
                casterPlayer.UpdateCriteria(CriteriaType.LandTargetedSpellOnTarget, m_spellInfo.Id, 0, 0, unit);

            if (m_caster != unit)
            {
                // Recheck  UNIT_FLAG_NON_ATTACKABLE for delayed spells
                if (m_spellInfo.HasHitDelay() && unit.HasUnitFlag(UnitFlags.NonAttackable) && unit.GetCharmerOrOwnerGUID() != m_caster.GetGUID())
                    return SpellMissInfo.Evade;

                if (m_caster.IsValidAttackTarget(unit, m_spellInfo))
                    unit.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.HostileActionReceived);
                else if (m_caster.IsFriendlyTo(unit))
                {
                    // for delayed spells ignore negative spells (after duel end) for friendly targets
                    if (m_spellInfo.HasHitDelay() && unit.IsPlayer() && !IsPositive() && !m_caster.IsValidAssistTarget(unit, m_spellInfo))
                        return SpellMissInfo.Evade;

                    // assisting case, healing and resurrection
                    if (unit.HasUnitState(UnitState.AttackPlayer))
                    {
                        Player playerOwner = m_caster.GetCharmerOrOwnerPlayerOrPlayerItself();
                        if (playerOwner != null)
                        {
                            playerOwner.SetContestedPvP();
                            playerOwner.UpdatePvP(true);
                        }
                    }

                    if (m_originalCaster != null && unit.IsInCombat() && m_spellInfo.HasInitialAggro())
                    {
                        if (m_originalCaster.HasUnitFlag(UnitFlags.PlayerControlled))               // only do explicit combat forwarding for PvP enabled units
                            m_originalCaster.GetCombatManager().InheritCombatStatesFrom(unit);    // for creature v creature combat, the threat forward does it for us
                        unit.GetThreatManager().ForwardThreatForAssistingMe(m_originalCaster, 0.0f, null, true);
                    }
                }
            }

            // original caster for auras
            WorldObject origCaster = m_caster;
            if (m_originalCaster != null)
                origCaster = m_originalCaster;

            // check immunity due to diminishing returns
            if (Aura.BuildEffectMaskForOwner(m_spellInfo, SpellConst.MaxEffectMask, unit) != 0)
            {
                foreach (var spellEffectInfo in m_spellInfo.GetEffects())
                {
                    hitInfo.AuraBasePoints[spellEffectInfo.EffectIndex] = (m_spellValue.CustomBasePointsMask & (1 << (int)spellEffectInfo.EffectIndex)) != 0 ?
                    m_spellValue.EffectBasePoints[spellEffectInfo.EffectIndex] :
                    spellEffectInfo.CalcBaseValue(m_originalCaster, unit, m_castItemEntry, m_castItemLevel);
                }

                // Get Data Needed for Diminishing Returns, some effects may have multiple auras, so this must be done on spell hit, not aura add
                hitInfo.DRGroup = m_spellInfo.GetDiminishingReturnsGroupForSpell();

                DiminishingLevels diminishLevel = DiminishingLevels.Level1;
                if (hitInfo.DRGroup != 0)
                {
                    diminishLevel = unit.GetDiminishing(hitInfo.DRGroup);
                    DiminishingReturnsType type = m_spellInfo.GetDiminishingReturnsGroupType();
                    // Increase Diminishing on unit, current informations for actually casts will use values above
                    if (type == DiminishingReturnsType.All || (type == DiminishingReturnsType.Player && unit.IsAffectedByDiminishingReturns()))
                        unit.IncrDiminishing(m_spellInfo);
                }

                // Now Reduce spell duration using data received at spell hit
                // check whatever effects we're going to apply, diminishing returns only apply to negative aura effects
                hitInfo.Positive = true;
                if (origCaster == unit || !origCaster.IsFriendlyTo(unit))
                {
                    foreach (var spellEffectInfo in m_spellInfo.GetEffects())
                    {
                        // mod duration only for effects applying aura!
                        if ((hitInfo.EffectMask & (1 << (int)spellEffectInfo.EffectIndex)) != 0 &&
                            spellEffectInfo.IsUnitOwnedAuraEffect() && !m_spellInfo.IsPositiveEffect(spellEffectInfo.EffectIndex))
                        {
                            hitInfo.Positive = false;
                            break;
                        }
                    }
                }

                hitInfo.AuraDuration = Aura.CalcMaxDuration(m_spellInfo, origCaster, m_powerCost);

                // unit is immune to aura if it was diminished to 0 duration
                if (!hitInfo.Positive && !unit.ApplyDiminishingToDuration(m_spellInfo, ref hitInfo.AuraDuration, origCaster, diminishLevel))
                    if (m_spellInfo.GetEffects().All(effInfo => !effInfo.IsEffect() || effInfo.IsEffect(SpellEffectName.ApplyAura)))
                        return SpellMissInfo.Immune;
            }

            return SpellMissInfo.None;
        }

        public void DoSpellEffectHit(Unit unit, SpellEffectInfo spellEffectInfo, TargetInfo hitInfo)
        {
            uint aura_effmask = Aura.BuildEffectMaskForOwner(m_spellInfo, 1u << (int)spellEffectInfo.EffectIndex, unit);
            if (aura_effmask != 0)
            {
                WorldObject caster = m_caster;
                if (m_originalCaster != null)
                    caster = m_originalCaster;

                if (caster != null)
                {
                    // delayed spells with multiple targets need to create a new aura object, otherwise we'll access a deleted aura
                    if (hitInfo.HitAura == null)
                    {
                        bool resetPeriodicTimer = (m_spellInfo.StackAmount < 2) && !_triggeredCastFlags.HasFlag(TriggerCastFlags.DontResetPeriodicTimer);
                        uint allAuraEffectMask = Aura.BuildEffectMaskForOwner(m_spellInfo, SpellConst.MaxEffectMask, unit);

                        AuraCreateInfo createInfo = new(m_castId, m_spellInfo, GetCastDifficulty(), allAuraEffectMask, unit);
                        createInfo.SetCasterGUID(caster.GetGUID());
                        createInfo.SetBaseAmount(hitInfo.AuraBasePoints);
                        createInfo.SetCastItem(m_castItemGUID, m_castItemEntry, m_castItemLevel);
                        createInfo.SetPeriodicReset(resetPeriodicTimer);
                        createInfo.SetOwnerEffectMask(aura_effmask);

                        Aura aura = Aura.TryRefreshStackOrCreate(createInfo, false);
                        if (aura != null)
                        {
                            hitInfo.HitAura = aura.ToUnitAura();

                            // Set aura stack amount to desired value
                            if (m_spellValue.AuraStackAmount > 1)
                            {
                                if (!createInfo.IsRefresh)
                                    hitInfo.HitAura.SetStackAmount((byte)m_spellValue.AuraStackAmount);
                                else
                                    hitInfo.HitAura.ModStackAmount(m_spellValue.AuraStackAmount);
                            }

                            hitInfo.HitAura.SetDiminishGroup(hitInfo.DRGroup);

                            if (!m_spellValue.Duration.HasValue)
                            {
                                hitInfo.AuraDuration = caster.ModSpellDuration(m_spellInfo, unit, hitInfo.AuraDuration, hitInfo.Positive, hitInfo.HitAura.GetEffectMask());

                                if (hitInfo.AuraDuration > 0)
                                {
                                    hitInfo.AuraDuration *= (int)m_spellValue.DurationMul;

                                    // Haste modifies duration of channeled spells
                                    if (m_spellInfo.IsChanneled())
                                        caster.ModSpellDurationTime(m_spellInfo, ref hitInfo.AuraDuration, this);
                                    else if (m_spellInfo.HasAttribute(SpellAttr8.HasteAffectsDuration))
                                    {
                                        int origDuration = hitInfo.AuraDuration;
                                        hitInfo.AuraDuration = 0;
                                        foreach (AuraEffect auraEff in hitInfo.HitAura.GetAuraEffects())
                                        {
                                            if (auraEff != null)
                                            {
                                                int period = auraEff.GetPeriod();
                                                if (period != 0)  // period is hastened by UNIT_MOD_CAST_SPEED
                                                    hitInfo.AuraDuration = Math.Max(Math.Max(origDuration / period, 1) * period, hitInfo.AuraDuration);
                                            }
                                        }

                                        // if there is no periodic effect
                                        if (hitInfo.AuraDuration == 0)
                                            hitInfo.AuraDuration = (int)(origDuration * m_originalCaster.m_unitData.ModCastingSpeed);
                                    }
                                }
                            }
                            else
                                hitInfo.AuraDuration = m_spellValue.Duration.Value;

                            if (hitInfo.AuraDuration != hitInfo.HitAura.GetMaxDuration())
                            {
                                hitInfo.HitAura.SetMaxDuration(hitInfo.AuraDuration);
                                hitInfo.HitAura.SetDuration(hitInfo.AuraDuration);
                            }

                            if (createInfo.IsRefresh)
                                hitInfo.HitAura.AddStaticApplication(unit, aura_effmask);
                        }
                    }
                    else
                        hitInfo.HitAura.AddStaticApplication(unit, aura_effmask);
                }
            }

            spellAura = hitInfo.HitAura;
            HandleEffects(unit, null, null, null, spellEffectInfo, SpellEffectHandleMode.HitTarget);
            spellAura = null;
        }

        public void DoTriggersOnSpellHit(Unit unit)
        {
            // handle SPELL_AURA_ADD_TARGET_TRIGGER auras
            // this is executed after spell proc spells on target hit
            // spells are triggered for each hit spell target
            // info confirmed with retail sniffs of permafrost and shadow weaving
            if (!m_hitTriggerSpells.Empty())
            {
                int _duration = 0;
                foreach (var hit in m_hitTriggerSpells)
                {
                    if (CanExecuteTriggersOnHit(unit, hit.triggeredByAura) && RandomHelper.randChance(hit.chance))
                    {
                        m_caster.CastSpell(unit, hit.triggeredSpell.Id, new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                            .SetTriggeringSpell(this)
                            .SetCastDifficulty(hit.triggeredSpell.Difficulty));
                        Log.outDebug(LogFilter.Spells, "Spell {0} triggered spell {1} by SPELL_AURA_ADD_TARGET_TRIGGER aura", m_spellInfo.Id, hit.triggeredSpell.Id);

                        // SPELL_AURA_ADD_TARGET_TRIGGER auras shouldn't trigger auras without duration
                        // set duration of current aura to the triggered spell
                        if (hit.triggeredSpell.GetDuration() == -1)
                        {
                            Aura triggeredAur = unit.GetAura(hit.triggeredSpell.Id, m_caster.GetGUID());
                            if (triggeredAur != null)
                            {
                                // get duration from aura-only once
                                if (_duration == 0)
                                {
                                    Aura aur = unit.GetAura(m_spellInfo.Id, m_caster.GetGUID());
                                    _duration = aur != null ? aur.GetDuration() : -1;
                                }
                                triggeredAur.SetDuration(_duration);
                            }
                        }
                    }
                }
            }

            // trigger linked auras remove/apply
            // @todo remove/cleanup this, as this table is not documented and people are doing stupid things with it
            var spellTriggered = Global.SpellMgr.GetSpellLinked(SpellLinkedType.Hit, m_spellInfo.Id);
            if (spellTriggered != null)
            {
                foreach (var id in spellTriggered)
                {
                    if (id < 0)
                        unit.RemoveAurasDueToSpell((uint)-id);
                    else
                        unit.CastSpell(unit, (uint)id, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCaster(m_caster.GetGUID()).SetTriggeringSpell(this));
                }
            }
        }

        bool UpdateChanneledTargetList()
        {
            // Not need check return true
            if (m_channelTargetEffectMask == 0)
                return true;

            uint channelTargetEffectMask = m_channelTargetEffectMask;
            uint channelAuraMask = 0;
            foreach (var spellEffectInfo in m_spellInfo.GetEffects())
                if (spellEffectInfo.IsEffect(SpellEffectName.ApplyAura))
                    channelAuraMask |= 1u << (int)spellEffectInfo.EffectIndex;

            channelAuraMask &= channelTargetEffectMask;

            float range = 0;
            if (channelAuraMask != 0)
            {
                range = m_spellInfo.GetMaxRange(IsPositive());
                Player modOwner = m_caster.GetSpellModOwner();
                if (modOwner != null)
                    modOwner.ApplySpellMod(m_spellInfo, SpellModOp.Range, ref range, this);

                // add little tolerance level
                range += Math.Min(3.0f, range * 0.1f); // 10% but no more than 3.0f
            }

            foreach (var targetInfo in m_UniqueTargetInfo)
            {
                if (targetInfo.MissCondition == SpellMissInfo.None && Convert.ToBoolean(channelTargetEffectMask & targetInfo.EffectMask))
                {
                    Unit unit = m_caster.GetGUID() == targetInfo.TargetGUID ? m_caster.ToUnit() : Global.ObjAccessor.GetUnit(m_caster, targetInfo.TargetGUID);
                    if (unit == null)
                    {
                        Unit unitCaster = m_caster.ToUnit();
                        if (unitCaster != null)
                            unitCaster.RemoveChannelObject(targetInfo.TargetGUID);
                        continue;
                    }

                    if (IsValidDeadOrAliveTarget(unit))
                    {
                        if (Convert.ToBoolean(channelAuraMask & targetInfo.EffectMask))
                        {
                            AuraApplication aurApp = unit.GetAuraApplication(m_spellInfo.Id, m_originalCasterGUID);
                            if (aurApp != null)
                            {
                                if (m_caster != unit && !m_caster.IsWithinDistInMap(unit, range))
                                {
                                    targetInfo.EffectMask &= ~aurApp.GetEffectMask();
                                    unit.RemoveAura(aurApp);
                                    Unit unitCaster = m_caster.ToUnit();
                                    if (unitCaster != null)
                                        unitCaster.RemoveChannelObject(targetInfo.TargetGUID);
                                    continue;
                                }
                            }
                            else // aura is dispelled
                            {
                                Unit unitCaster = m_caster.ToUnit();
                                if (unitCaster != null)
                                    unitCaster.RemoveChannelObject(targetInfo.TargetGUID);
                                continue;
                            }
                        }

                        channelTargetEffectMask &= ~targetInfo.EffectMask;   // remove from need alive mask effect that have alive target
                    }
                }
            }

            // is all effects from m_needAliveTargetMask have alive targets
            return channelTargetEffectMask == 0;
        }

        public SpellCastResult Prepare(SpellCastTargets targets, AuraEffect triggeredByAura = null)
        {
            if (m_CastItem != null)
            {
                m_castItemGUID = m_CastItem.GetGUID();
                m_castItemEntry = m_CastItem.GetEntry();

                Player owner = m_CastItem.GetOwner();
                if (owner != null)
                    m_castItemLevel = (int)m_CastItem.GetItemLevel(owner);
                else if (m_CastItem.GetOwnerGUID() == m_caster.GetGUID())
                    m_castItemLevel = (int)m_CastItem.GetItemLevel(m_caster.ToPlayer());
                else
                {
                    SendCastResult(SpellCastResult.EquippedItem);
                    Finish(SpellCastResult.EquippedItem);
                    return SpellCastResult.EquippedItem;
                }
            }

            InitExplicitTargets(targets);

            m_spellState = SpellState.Preparing;

            if (triggeredByAura != null)
            {
                m_triggeredByAuraSpell = triggeredByAura.GetSpellInfo();
                m_castItemLevel = triggeredByAura.GetBase().GetCastItemLevel();
            }

            // create and add update event for this spell
            _spellEvent = new SpellEvent(this);
            m_caster.m_Events.AddEvent(_spellEvent, m_caster.m_Events.CalculateTime(TimeSpan.FromMilliseconds(1)));

            // check disables
            if (Global.DisableMgr.IsDisabledFor(DisableType.Spell, m_spellInfo.Id, m_caster))
            {
                SendCastResult(SpellCastResult.SpellUnavailable);
                Finish(SpellCastResult.SpellUnavailable);
                return SpellCastResult.SpellUnavailable;
            }

            // Prevent casting at cast another spell (ServerSide check)
            if (!_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreCastInProgress) && m_caster.ToUnit() != null && m_caster.ToUnit().IsNonMeleeSpellCast(false, true, true, m_spellInfo.Id == 75))
            {
                SendCastResult(SpellCastResult.SpellInProgress);
                Finish(SpellCastResult.SpellInProgress);
                return SpellCastResult.SpellInProgress;
            }

            LoadScripts();

            // Fill cost data (not use power for item casts
            if (m_CastItem == null)
                m_powerCost = m_spellInfo.CalcPowerCost(m_caster, m_spellSchoolMask, this);

            int param1 = 0, param2 = 0;
            SpellCastResult result = CheckCast(true, ref param1, ref param2);
            // target is checked in too many locations and with different results to handle each of them
            // handle just the general SPELL_FAILED_BAD_TARGETS result which is the default result for most DBC target checks
            if (Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreTargetCheck) && result == SpellCastResult.BadTargets)
                result = SpellCastResult.SpellCastOk;
            if (result != SpellCastResult.SpellCastOk)
            {
                // Periodic auras should be interrupted when aura triggers a spell which can't be cast
                // for example bladestorm aura should be removed on disarm as of patch 3.3.5
                // channeled periodic spells should be affected by this (arcane missiles, penance, etc)
                // a possible alternative sollution for those would be validating aura target on unit state change
                if (triggeredByAura != null && triggeredByAura.IsPeriodic() && !triggeredByAura.GetBase().IsPassive())
                {
                    SendChannelUpdate(0, result);
                    triggeredByAura.GetBase().SetDuration(0);
                }

                if (param1 != 0 || param2 != 0)
                    SendCastResult(result, param1, param2);
                else
                    SendCastResult(result);

                // queue autorepeat spells for future repeating
                if (GetCurrentContainer() == CurrentSpellTypes.AutoRepeat && m_caster.IsUnit())
                    m_caster.ToUnit().SetCurrentCastSpell(this);

                Finish(result);
                return result;
            }

            // Prepare data for triggers
            PrepareDataForTriggerSystem();

            if (!_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreCastTime))
                m_casttime = m_spellInfo.CalcCastTime(this);
            m_casttime = CallScriptCalcCastTimeHandlers(m_casttime);

            SpellCastResult movementResult = SpellCastResult.SpellCastOk;
            if (m_caster.IsUnit() && m_caster.ToUnit().IsMoving())
                movementResult = CheckMovement();

            // Creatures focus their target when possible
            if (m_casttime != 0 && m_caster.IsCreature() && !m_spellInfo.IsNextMeleeSwingSpell() && !IsAutoRepeat() && !m_caster.ToUnit().HasUnitFlag(UnitFlags.Possessed))
            {
                // Channeled spells and some triggered spells do not focus a cast target. They face their target later on via channel object guid and via spell attribute or not at all
                bool focusTarget = !m_spellInfo.IsChanneled() && !_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreSetFacing);
                if (focusTarget && m_targets.GetObjectTarget() != null && m_caster != m_targets.GetObjectTarget())
                    m_caster.ToCreature().SetSpellFocus(this, m_targets.GetObjectTarget());
                else
                    m_caster.ToCreature().SetSpellFocus(this, null);
            }

            if (movementResult != SpellCastResult.SpellCastOk)
            {
                if (m_caster.ToUnit().IsControlledByPlayer() || !CanStopMovementForSpellCasting(m_caster.ToUnit().GetMotionMaster().GetCurrentMovementGeneratorType()))
                {
                    SendCastResult(movementResult);
                    Finish(movementResult);
                    return movementResult;
                }
                else
                {
                    // Creatures (not controlled) give priority to spell casting over movement.
                    // We assume that the casting is always valid and the current movement
                    // is stopped immediately (because spells are updated before movement, so next Unit::Update would cancel the spell before stopping movement)
                    // and future attempts are stopped by by Unit::IsMovementPreventedByCasting in movement generators to prevent casting interruption.
                    m_caster.ToUnit().StopMoving();
                }
            }

            CallScriptOnPrecastHandler();

            // set timer base at cast time
            ReSetTimer();

            Log.outDebug(LogFilter.Spells, "Spell.prepare: spell id {0} source {1} caster {2} customCastFlags {3} mask {4}", m_spellInfo.Id, m_caster.GetEntry(), m_originalCaster != null ? (int)m_originalCaster.GetEntry() : -1, _triggeredCastFlags, m_targets.GetTargetMask());

            if (m_spellInfo.HasAttribute(SpellAttr12.StartCooldownOnCastStart))
                SendSpellCooldown();

            if (m_spellInfo.HasAttribute(SpellAttr7.ResetSwingTimerAtSpellStart) && IsAutoActionResetSpell())
                ResetCombatTimers();

            //Containers for channeled spells have to be set
            // @todoApply this to all casted spells if needed
            // Why check duration? 29350: channelled triggers channelled
            if (_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.CastDirectly) && (!m_spellInfo.IsChanneled() || m_spellInfo.GetMaxDuration() == 0))
                Cast(true);
            else
            {
                // commented out !m_spellInfo.StartRecoveryTime, it forces instant spells with global cooldown to be processed in spell::update
                // as a result a spell that passed CheckCast and should be processed instantly may suffer from this delayed process
                // the easiest bug to observe is LoS check in AddUnitTarget, even if spell passed the CheckCast LoS check the situation can change in spell::update
                // because target could be relocated in the meantime, making the spell fly to the air (no targets can be registered, so no effects processed, nothing in combat log)
                bool willCastDirectly = m_casttime == 0 && /*!m_spellInfo.StartRecoveryTime && */ GetCurrentContainer() == CurrentSpellTypes.Generic;

                Unit unitCaster = m_caster.ToUnit();
                if (unitCaster != null)
                {
                    // stealth must be removed at cast starting (at show channel bar)
                    // skip triggered spell (item equip spell casting and other not explicit character casts/item uses)
                    if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreCastInProgress) && !m_spellInfo.HasAttribute(SpellAttr2.NotAnAction))
                        unitCaster.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Action, m_spellInfo);

                    // Do not register as current spell when requested to ignore cast in progress
                    // We don't want to interrupt that other spell with cast time
                    if (!willCastDirectly || !_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreCastInProgress))
                        unitCaster.SetCurrentCastSpell(this);
                }

                SendSpellStart();

                if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreGCD))
                    TriggerGlobalCooldown();

                // Call CreatureAI hook OnSpellStart
                Creature caster = m_caster.ToCreature();
                if (caster != null)
                    if (caster.IsAIEnabled())
                        caster.GetAI().OnSpellStart(GetSpellInfo());

                if (willCastDirectly)
                    Cast(true);
            }

            return SpellCastResult.SpellCastOk;
        }

        public void Cancel()
        {
            if (m_spellState == SpellState.Finished)
                return;

            SpellState oldState = m_spellState;
            m_spellState = SpellState.Finished;

            m_autoRepeat = false;
            switch (oldState)
            {
                case SpellState.Preparing:
                    CancelGlobalCooldown();
                    goto case SpellState.Delayed;
                case SpellState.Delayed:
                    SendInterrupted(0);
                    SendCastResult(SpellCastResult.Interrupted);
                    break;

                case SpellState.Casting:
                    foreach (var ihit in m_UniqueTargetInfo)
                    {
                        if (ihit.MissCondition == SpellMissInfo.None)
                        {
                            Unit unit = m_caster.GetGUID() == ihit.TargetGUID ? m_caster.ToUnit() : Global.ObjAccessor.GetUnit(m_caster, ihit.TargetGUID);
                            if (unit != null)
                                unit.RemoveOwnedAura(m_spellInfo.Id, m_originalCasterGUID, 0, AuraRemoveMode.Cancel);
                        }
                    }

                    SendChannelUpdate(0, SpellCastResult.Interrupted);
                    SendInterrupted(0);
                    SendCastResult(SpellCastResult.Interrupted);

                    m_appliedMods.Clear();
                    break;

                default:
                    break;
            }

            SetReferencedFromCurrent(false);
            if (m_selfContainer != null && m_selfContainer == this)
                m_selfContainer = null;

            // originalcaster handles gameobjects/dynobjects for gob caster
            if (m_originalCaster != null)
            {
                m_originalCaster.RemoveDynObject(m_spellInfo.Id);
                if (m_spellInfo.IsChanneled()) // if not channeled then the object for the current cast wasn't summoned yet
                    m_originalCaster.RemoveGameObject(m_spellInfo.Id, true);
            }

            //set state back so finish will be processed
            m_spellState = oldState;

            Finish(SpellCastResult.Interrupted);
        }

        public void Cast(bool skipCheck = false)
        {
            Player modOwner = m_caster.GetSpellModOwner();
            Spell lastSpellMod = null;
            if (modOwner != null)
            {
                lastSpellMod = modOwner.m_spellModTakingSpell;
                if (lastSpellMod != null)
                    modOwner.SetSpellModTakingSpell(lastSpellMod, false);
            }

            _cast(skipCheck);

            if (lastSpellMod != null)
                modOwner.SetSpellModTakingSpell(lastSpellMod, true);
        }

        void _cast(bool skipCheck = false)
        {
            if (!UpdatePointers())
            {
                // cancel the spell if UpdatePointers() returned false, something wrong happened there
                Cancel();
                return;
            }

            // cancel at lost explicit target during cast
            if (!m_targets.GetObjectTargetGUID().IsEmpty() && m_targets.GetObjectTarget() == null)
            {
                Cancel();
                return;
            }

            Player playerCaster = m_caster.ToPlayer();
            if (playerCaster != null)
            {
                // now that we've done the basic check, now run the scripts
                // should be done before the spell is actually executed
                Global.ScriptMgr.OnPlayerSpellCast(playerCaster, this, skipCheck);

                // As of 3.0.2 pets begin attacking their owner's target immediately
                // Let any pets know we've attacked something. Check DmgClass for harmful spells only
                // This prevents spells such as Hunter's Mark from triggering pet attack
                if (m_spellInfo.DmgClass != SpellDmgClass.None)
                {
                    Unit target = m_targets.GetUnitTarget();
                    if (target != null)
                    {
                        foreach (Unit controlled in playerCaster.m_Controlled)
                        {
                            Creature cControlled = controlled.ToCreature();
                            if (cControlled != null)
                            {
                                CreatureAI controlledAI = cControlled.GetAI();
                                if (controlledAI != null)
                                    controlledAI.OwnerAttacked(target);
                            }
                        }
                    }
                }
            }

            SetExecutedCurrently(true);

            // Should this be done for original caster?
            Player modOwner = m_caster.GetSpellModOwner();
            if (modOwner != null)
            {
                // Set spell which will drop charges for triggered cast spells
                // if not successfully casted, will be remove in finish(false)
                modOwner.SetSpellModTakingSpell(this, true);
            }

            CallScriptBeforeCastHandlers();

            // skip check if done already (for instant cast spells for example)
            if (!skipCheck)
            {
                void cleanupSpell(SpellCastResult result, int? param1 = null, int? param2 = null)
                {
                    SendCastResult(result, param1, param2);
                    SendInterrupted(0);

                    if (modOwner != null)
                        modOwner.SetSpellModTakingSpell(this, false);

                    Finish(result);
                    SetExecutedCurrently(false);
                }

                int param1 = 0, param2 = 0;
                SpellCastResult castResult = CheckCast(false, ref param1, ref param2);
                if (castResult != SpellCastResult.SpellCastOk)
                {
                    cleanupSpell(castResult, param1, param2);
                    return;
                }

                // additional check after cast bar completes (must not be in CheckCast)
                // if trade not complete then remember it in trade data
                if (Convert.ToBoolean(m_targets.GetTargetMask() & SpellCastTargetFlags.TradeItem))
                {
                    if (modOwner != null)
                    {
                        TradeData my_trade = modOwner.GetTradeData();
                        if (my_trade != null)
                        {
                            if (!my_trade.IsInAcceptProcess())
                            {
                                // Spell will be casted at completing the trade. Silently ignore at this place
                                my_trade.SetSpell(m_spellInfo.Id, m_CastItem);
                                cleanupSpell(SpellCastResult.DontReport);
                                return;
                            }
                        }
                    }
                }

                // check diminishing returns (again, only after finish cast bar, tested on retail)
                Unit target = m_targets.GetUnitTarget();
                if (target != null)
                {
                    uint aura_effmask = 0;
                    foreach (var spellEffectInfo in m_spellInfo.GetEffects())
                        if (spellEffectInfo.IsUnitOwnedAuraEffect())
                            aura_effmask |= 1u << (int)spellEffectInfo.EffectIndex;

                    if (aura_effmask != 0)
                    {
                        if (m_spellInfo.GetDiminishingReturnsGroupForSpell() != 0)
                        {
                            DiminishingReturnsType type = m_spellInfo.GetDiminishingReturnsGroupType();
                            if (type == DiminishingReturnsType.All || (type == DiminishingReturnsType.Player && target.IsAffectedByDiminishingReturns()))
                            {
                                Unit caster1 = m_originalCaster != null ? m_originalCaster : m_caster.ToUnit();
                                if (caster1 != null)
                                {
                                    if (target.HasStrongerAuraWithDR(m_spellInfo, caster1))
                                    {
                                        cleanupSpell(SpellCastResult.AuraBounced);
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // The spell focusing is making sure that we have a valid cast target guid when we need it so only check for a guid value here.
            Creature creatureCaster = m_caster.ToCreature();
            if (creatureCaster != null)
            {
                if (creatureCaster.GetTarget().IsEmpty() && !creatureCaster.HasUnitFlag(UnitFlags.Possessed))
                {
                    WorldObject target = Global.ObjAccessor.GetUnit(creatureCaster, creatureCaster.GetTarget());
                    if (target != null)
                        creatureCaster.SetInFront(target);
                }
            }

            SelectSpellTargets();

            // Spell may be finished after target map check
            if (m_spellState == SpellState.Finished)
            {
                SendInterrupted(0);

                if (m_caster.IsTypeId(TypeId.Player))
                    m_caster.ToPlayer().SetSpellModTakingSpell(this, false);

                Finish(SpellCastResult.Interrupted);
                SetExecutedCurrently(false);
                return;
            }

            Unit unitCaster = m_caster.ToUnit();
            if (unitCaster != null)
            {
                if (m_spellInfo.HasAttribute(SpellAttr1.DismissPetFirst))
                {
                    Creature pet = ObjectAccessor.GetCreature(m_caster, unitCaster.GetPetGUID());
                    if (pet != null)
                        pet.DespawnOrUnsummon();
                }
            }

            PrepareTriggersExecutedOnHit();

            CallScriptOnCastHandlers();

            // traded items have trade slot instead of guid in m_itemTargetGUID
            // set to real guid to be sent later to the client
            m_targets.UpdateTradeSlotItem();

            Player player = m_caster.ToPlayer();
            if (player != null)
            {
                if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreCastItem) && m_CastItem != null)
                {
                    player.StartCriteria(CriteriaStartEvent.UseItem, m_CastItem.GetEntry());
                    player.UpdateCriteria(CriteriaType.UseItem, m_CastItem.GetEntry());
                }

                player.FailCriteria(CriteriaFailEvent.CastSpell, m_spellInfo.Id);
                player.StartCriteria(CriteriaStartEvent.CastSpell, m_spellInfo.Id);
                player.UpdateCriteria(CriteriaType.CastSpell, m_spellInfo.Id);
            }

            // Spells that don't create items can have this attribute - handle here
            if (m_CastItem != null && m_spellInfo.HasAttribute(SpellAttr9.ItemCastGrantsSkillGain))
            {
                Player playerCaster1 = m_caster.ToPlayer();
                if (playerCaster1 != null)
                    playerCaster1.UpdateCraftSkill(m_spellInfo);
            }

            Item targetItem = m_targets.GetItemTarget();
            // Powers have to be taken before SendSpellGo
            if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnorePowerCost))
                TakePower();

            if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreReagentCost))
                TakeReagents();                                         // we must remove reagents before HandleEffects to allow place crafted item in same slot            
            else if (targetItem != null)
            {
                // Not own traded item (in trader trade slot) req. reagents including triggered spell case
                if (targetItem.GetOwnerGUID() != m_caster.GetGUID())
                    TakeReagents();
            }

            // CAST SPELL
            if (!m_spellInfo.HasAttribute(SpellAttr12.StartCooldownOnCastStart))
                SendSpellCooldown();

            if (m_spellInfo.LaunchDelay == 0)
            {
                HandleLaunchPhase();
                m_launchHandled = true;
            }

            if (!m_spellInfo.HasAttribute(SpellAttr7.ResetSwingTimerAtSpellStart) && IsAutoActionResetSpell())
                ResetCombatTimers();

            // we must send smsg_spell_go packet before m_castItem delete in TakeCastItem()...
            SendSpellGo();

            if (!m_spellInfo.IsChanneled())
                if (creatureCaster != null)
                    creatureCaster.ReleaseSpellFocus(this);

            // Okay, everything is prepared. Now we need to distinguish between immediate and evented delayed spells
            if (m_spellInfo.HasHitDelay() && !m_spellInfo.IsChanneled())
            {
                // Remove used for cast item if need (it can be already NULL after TakeReagents call
                // in case delayed spell remove item at cast delay start
                TakeCastItem();

                // Okay, maps created, now prepare flags
                m_immediateHandled = false;
                m_spellState = SpellState.Delayed;
                SetDelayStart(0);

                unitCaster = m_caster.ToUnit();
                if (unitCaster != null)
                    if (unitCaster.HasUnitState(UnitState.Casting) && !unitCaster.IsNonMeleeSpellCast(false, false, true))
                        unitCaster.ClearUnitState(UnitState.Casting);
            }
            else
            {
                // Immediate spell, no big deal
                HandleImmediate();
            }

            if (m_scriptResult != null && !m_scriptWaitsForSpellHit)
                m_scriptResult.SetResult(SpellCastResult.SpellCastOk);

            CallScriptAfterCastHandlers();

            var spell_triggered = Global.SpellMgr.GetSpellLinked(SpellLinkedType.Cast, m_spellInfo.Id);
            if (spell_triggered != null)
            {
                foreach (var spellId in spell_triggered)
                {
                    if (spellId < 0)
                    {
                        unitCaster = m_caster.ToUnit();
                        if (unitCaster != null)
                            unitCaster.RemoveAurasDueToSpell((uint)-spellId);
                    }
                    else
                        m_caster.CastSpell(m_targets.GetUnitTarget() ?? m_caster, (uint)spellId, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetTriggeringSpell(this));
                }
            }

            if (modOwner != null)
            {
                modOwner.SetSpellModTakingSpell(this, false);

                //Clear spell cooldowns after every spell is cast if .cheat cooldown is enabled.
                if (m_originalCaster != null && modOwner.GetCommandStatus(PlayerCommandStates.Cooldown))
                {
                    m_originalCaster.GetSpellHistory().ResetCooldown(m_spellInfo.Id, true);
                    m_originalCaster.GetSpellHistory().RestoreCharge(m_spellInfo.ChargeCategoryId);
                }
            }

            SetExecutedCurrently(false);

            if (m_originalCaster == null)
                return;

            // Handle procs on cast
            ProcFlagsInit procAttacker = m_procAttacker;
            if (!procAttacker)
            {
                if (m_spellInfo.HasAttribute(SpellAttr3.TreatAsPeriodic))
                {
                    if (IsPositive())
                        procAttacker.Or(ProcFlags.DealHelpfulPeriodic);
                    else
                        procAttacker.Or(ProcFlags.DealHarmfulPeriodic);
                }
                else if (m_spellInfo.HasAttribute(SpellAttr0.IsAbility))
                {
                    if (IsPositive())
                        procAttacker.Or(ProcFlags.DealHelpfulAbility);
                    else
                        procAttacker.Or(ProcFlags.DealHarmfulSpell);
                }
                else
                {
                    if (IsPositive())
                        procAttacker.Or(ProcFlags.DealHelpfulSpell);
                    else
                        procAttacker.Or(ProcFlags.DealHarmfulSpell);
                }
            }

            procAttacker.Or(ProcFlags2.CastSuccessful);

            ProcFlagsHit hitMask = m_hitMask;
            if (!hitMask.HasAnyFlag(ProcFlagsHit.Critical))
                hitMask |= ProcFlagsHit.Normal;

            if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreCastInProgress) && !m_spellInfo.HasAttribute(SpellAttr2.NotAnAction))
                m_originalCaster.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.ActionDelayed, m_spellInfo);

            Unit.ProcSkillsAndAuras(m_originalCaster, null, procAttacker, new ProcFlagsInit(ProcFlags.None), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.Cast, hitMask, this, null, null);

            // Call CreatureAI hook OnSpellCast
            Creature caster = m_originalCaster.ToCreature();
            if (caster != null)
                if (caster.IsAIEnabled())
                    caster.GetAI().OnSpellCast(GetSpellInfo());
        }

        void DoProcessTargetContainer<T>(List<T> targetContainer) where T : TargetInfoBase
        {
            foreach (TargetInfoBase target in targetContainer)
                target.PreprocessTarget(this);

            foreach (var spellEffectInfo in m_spellInfo.GetEffects())
            {
                foreach (TargetInfoBase target in targetContainer)
                    if ((target.EffectMask & (1 << (int)spellEffectInfo.EffectIndex)) != 0)
                        target.DoTargetSpellHit(this, spellEffectInfo);
            }

            foreach (TargetInfoBase target in targetContainer)
                target.DoDamageAndTriggers(this);
        }

        void HandleImmediate()
        {
            // start channeling if applicable
            if (m_spellInfo.IsChanneled())
            {
                int duration = m_spellInfo.GetDuration();
                if (duration > 0 || m_spellValue.Duration > 0)
                {
                    if (!m_spellValue.Duration.HasValue)
                    {
                        int originalDuration = duration;

                        // First mod_duration then haste - see Missile Barrage
                        // Apply duration mod
                        Player modOwner = m_caster.GetSpellModOwner();
                        if (modOwner != null)
                            modOwner.ApplySpellMod(m_spellInfo, SpellModOp.Duration, ref duration);

                        duration = (int)(duration * m_spellValue.DurationMul);

                        // Apply haste mods
                        m_caster.ModSpellDurationTime(m_spellInfo, ref duration, this);

                        if (IsEmpowerSpell())
                        {
                            float ratio = (float)duration / (float)originalDuration;
                            TimeSpan totalExceptLastStage = TimeSpan.Zero;
                            for (int i = 0; i < m_spellInfo.EmpowerStageThresholds.Count - 1; ++i)
                            {
                                m_empower.StageDurations[i] = TimeSpan.FromMilliseconds((long)(m_spellInfo.EmpowerStageThresholds[i].TotalMilliseconds * ratio));
                                totalExceptLastStage += m_empower.StageDurations[i];
                            }

                            m_empower.StageDurations[^1] = TimeSpan.FromMilliseconds(duration) - totalExceptLastStage;

                            Player playerCaster = m_caster.ToPlayer();
                            if (playerCaster != null)
                                m_empower.MinHoldTime = TimeSpan.FromMilliseconds((long)(m_empower.StageDurations[0].TotalMilliseconds * playerCaster.GetEmpowerMinHoldStagePercent()));
                            else
                                m_empower.MinHoldTime = m_empower.StageDurations[0];

                            duration += (int)SpellConst.EmpowerHoldTimeAtMax;
                        }
                    }
                    else
                        duration = m_spellValue.Duration.Value;

                    m_channeledDuration = duration;
                    SendChannelStart((uint)duration);
                }
                else if (duration == -1)
                    SendChannelStart(unchecked((uint)duration));

                if (duration != 0)
                {
                    m_spellState = SpellState.Casting;
                    // GameObjects shouldn't cast channeled spells
                    m_caster.ToUnit()?.AddInterruptMask(m_spellInfo.ChannelInterruptFlags, m_spellInfo.ChannelInterruptFlags2);
                }
            }

            PrepareTargetProcessing();

            // process immediate effects (items, ground, etc.) also initialize some variables
            _handle_immediate_phase();

            // consider spell hit for some spells without target, so they may proc on finish phase correctly
            if (m_UniqueTargetInfo.Empty())
            {
                m_hitMask = ProcFlagsHit.Normal;
                m_procSpellType = ProcFlagsSpellType.NoDmgHeal;
            }
            else
                DoProcessTargetContainer(m_UniqueTargetInfo);

            DoProcessTargetContainer(m_UniqueGOTargetInfo);

            DoProcessTargetContainer(m_UniqueCorpseTargetInfo);

            FinishTargetProcessing();

            // spell is finished, perform some last features of the spell here
            _handle_finish_phase();

            // Remove used for cast item if need (it can be already NULL after TakeReagents call
            TakeCastItem();

            if (m_spellState != SpellState.Casting)
                Finish();                                       // successfully finish spell cast (not last in case autorepeat or channel spell)
        }

        public ulong HandleDelayed(ulong offset)
        {
            if (!UpdatePointers())
            {
                // finish the spell if UpdatePointers() returned false, something wrong happened there
                Finish(SpellCastResult.NoValidTargets);
                return 0;
            }

            // when spell has a single missile we hit all targets (except caster) at the same time
            bool single_missile = m_targets.HasDst();
            bool ignoreTargetInfoTimeDelay = single_missile;
            ulong next_time = 0;

            if (!m_launchHandled)
            {
                ulong launchMoment = (ulong)Math.Floor(m_spellInfo.LaunchDelay * 1000.0f);
                if (launchMoment > offset)
                    return launchMoment;

                HandleLaunchPhase();
                m_launchHandled = true;
            }

            if (m_delayMoment > offset)
            {
                ignoreTargetInfoTimeDelay = false;
                next_time = m_delayMoment;
            }

            Player modOwner = m_caster.GetSpellModOwner();
            if (modOwner != null)
                modOwner.SetSpellModTakingSpell(this, true);

            PrepareTargetProcessing();

            if (!m_immediateHandled && m_delayMoment <= offset)
            {
                _handle_immediate_phase();
                m_immediateHandled = true;
            }

            // now recheck units targeting correctness (need before any effects apply to prevent adding immunity at first effect not allow apply second spell effect and similar cases)
            {
                List<TargetInfo> delayedTargets = new();
                m_UniqueTargetInfo.RemoveAll(target =>
                {
                    if (ignoreTargetInfoTimeDelay || target.TimeDelay <= offset)
                    {
                        target.TimeDelay = offset;
                        delayedTargets.Add(target);
                        return true;
                    }
                    else if (!single_missile && (next_time == 0 || target.TimeDelay < next_time))
                        next_time = target.TimeDelay;
                    return false;
                });

                DoProcessTargetContainer(delayedTargets);
            }

            // now recheck gameobject targeting correctness
            {
                List<GOTargetInfo> delayedGOTargets = new();
                m_UniqueGOTargetInfo.RemoveAll(goTarget =>
                {
                    if (ignoreTargetInfoTimeDelay || goTarget.TimeDelay <= offset)
                    {
                        goTarget.TimeDelay = offset;
                        delayedGOTargets.Add(goTarget);
                        return true;
                    }
                    else if (!single_missile && (next_time == 0 || goTarget.TimeDelay < next_time))
                        next_time = goTarget.TimeDelay;
                    return false;
                });

                DoProcessTargetContainer(delayedGOTargets);
            }

            FinishTargetProcessing();

            if (modOwner != null)
                modOwner.SetSpellModTakingSpell(this, false);

            // All targets passed - need finish phase
            if (next_time == 0)
            {
                // spell is finished, perform some last features of the spell here
                _handle_finish_phase();

                Finish();                                       // successfully finish spell cast

                // return zero, spell is finished now
                return 0;
            }
            else
            {
                // spell is unfinished, return next execution time
                return next_time;
            }
        }

        void _handle_immediate_phase()
        {
            // handle some immediate features of the spell here
            HandleThreatSpells();

            // handle effects with SPELL_EFFECT_HANDLE_HIT mode
            foreach (var spellEffectInfo in m_spellInfo.GetEffects())
            {
                // don't do anything for empty effect
                if (!spellEffectInfo.IsEffect())
                    continue;

                // call effect handlers to handle destination hit
                HandleEffects(null, null, null, null, spellEffectInfo, SpellEffectHandleMode.Hit);
            }

            // process items
            DoProcessTargetContainer(m_UniqueItemInfo);
        }

        void _handle_finish_phase()
        {
            Unit unitCaster = m_caster.ToUnit();
            if (unitCaster != null)
                if (m_spellInfo.HasEffect(SpellEffectName.AddExtraAttacks))
                    unitCaster.SetLastExtraAttackSpell(m_spellInfo.Id);

            // Handle procs on finish
            if (m_originalCaster == null)
                return;

            ProcFlagsInit procAttacker = m_procAttacker;
            if (!procAttacker)
            {
                if (m_spellInfo.HasAttribute(SpellAttr3.TreatAsPeriodic))
                {
                    if (IsPositive())
                        procAttacker.Or(ProcFlags.DealHelpfulPeriodic);
                    else
                        procAttacker.Or(ProcFlags.DealHarmfulPeriodic);
                }
                else if (m_spellInfo.HasAttribute(SpellAttr0.IsAbility))
                {
                    if (IsPositive())
                        procAttacker.Or(ProcFlags.DealHelpfulAbility);
                    else
                        procAttacker.Or(ProcFlags.DealHarmfulAbility);
                }
                else
                {
                    if (IsPositive())
                        procAttacker.Or(ProcFlags.DealHelpfulSpell);
                    else
                        procAttacker.Or(ProcFlags.DealHarmfulSpell);
                }
            }

            Unit.ProcSkillsAndAuras(m_originalCaster, null, procAttacker, new ProcFlagsInit(ProcFlags.None), m_procSpellType, ProcFlagsSpellPhase.Finish, m_hitMask, this, null, null);
        }

        void SendSpellCooldown()
        {
            if (!m_caster.IsUnit())
                return;

            if (m_CastItem != null)
                m_caster.ToUnit().GetSpellHistory().HandleCooldowns(m_spellInfo, m_CastItem, this);
            else
                m_caster.ToUnit().GetSpellHistory().HandleCooldowns(m_spellInfo, m_castItemEntry, this);

            if (IsAutoRepeat())
                m_caster.ToUnit().ResetAttackTimer(WeaponAttackType.RangedAttack);
        }

        public void Update(uint difftime)
        {
            if (!UpdatePointers())
            {
                // cancel the spell if UpdatePointers() returned false, something wrong happened there
                Cancel();
                return;
            }

            if (!m_targets.GetUnitTargetGUID().IsEmpty() && m_targets.GetUnitTarget() == null)
            {
                Log.outDebug(LogFilter.Spells, "Spell {0} is cancelled due to removal of target.", m_spellInfo.Id);
                Cancel();
                return;
            }

            // check if the unit caster has moved before the spell finished
            if (m_timer != 0 && m_caster.IsUnit() && m_caster.ToUnit().IsMoving() && CheckMovement() != SpellCastResult.SpellCastOk)
                Cancel();

            switch (m_spellState)
            {
                case SpellState.Preparing:
                {
                    if (m_timer > 0)
                    {
                        if (difftime >= m_timer)
                            m_timer = 0;
                        else
                            m_timer -= (int)difftime;
                    }

                    if (m_timer == 0 && !m_spellInfo.IsNextMeleeSwingSpell())
                        // don't CheckCast for instant spells - done in spell.prepare, skip duplicate checks, needed for range checks for example
                        Cast(m_casttime == 0);
                    break;
                }
                case SpellState.Casting:
                {
                    if (m_timer != 0)
                    {
                        // check if there are alive targets left
                        if (!UpdateChanneledTargetList())
                        {
                            Log.outDebug(LogFilter.Spells, "Channeled spell {0} is removed due to lack of targets", m_spellInfo.Id);
                            m_timer = 0;

                            // Also remove applied auras
                            foreach (TargetInfo target in m_UniqueTargetInfo)
                            {
                                Unit unit = m_caster.GetGUID() == target.TargetGUID ? m_caster.ToUnit() : Global.ObjAccessor.GetUnit(m_caster, target.TargetGUID);
                                if (unit != null)
                                    unit.RemoveOwnedAura(m_spellInfo.Id, m_originalCasterGUID, 0, AuraRemoveMode.Cancel);
                            }
                        }

                        if (m_timer > 0)
                        {
                            if (difftime >= m_timer)
                                m_timer = 0;
                            else
                                m_timer -= (int)difftime;
                        }
                    }

                    if (IsEmpowerSpell())
                    {
                        int completedStages = new Func<int>(() =>
                        {
                            TimeSpan passed = TimeSpan.FromMilliseconds(m_channeledDuration - m_timer);
                            for (int i = 0; i < m_empower.StageDurations.Count; ++i)
                            {
                                passed -= m_empower.StageDurations[i];
                                if (passed < TimeSpan.Zero)
                                    return i;
                            }

                            return m_empower.StageDurations.Count;
                        })();

                        if (completedStages != m_empower.CompletedStages)
                        {
                            SpellEmpowerSetStage empowerSetStage = new();
                            empowerSetStage.CastID = m_castId;
                            empowerSetStage.CasterGUID = m_caster.GetGUID();
                            empowerSetStage.Stage = m_empower.CompletedStages;
                            m_caster.SendMessageToSet(empowerSetStage, true);

                            m_empower.CompletedStages = completedStages;
                            m_caster.ToUnit().SetSpellEmpowerStage((sbyte)completedStages);

                            CallScriptEmpowerStageCompletedHandlers(completedStages);
                            m_caster.ToUnit().RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags2.ReleaseEmpower, m_spellInfo);
                        }

                        if (CanReleaseEmpowerSpell())
                        {
                            m_empower.IsReleased = true;
                            m_timer = 0;
                            CallScriptEmpowerCompletedHandlers(m_empower.CompletedStages);
                        }
                    }

                    if (m_timer == 0)
                    {
                        SendChannelUpdate(0, SpellCastResult.SpellCastOk);
                        Finish();

                        // We call the hook here instead of in Spell::finish because we only want to call it for completed channeling. Everything else is handled by interrupts
                        Creature creatureCaster = m_caster.ToCreature();
                        if (creatureCaster != null)
                            if (creatureCaster.IsAIEnabled())
                                creatureCaster.GetAI().OnChannelFinished(m_spellInfo);
                    }
                    break;
                }
                default:
                    break;
            }
        }

        public void Finish(SpellCastResult result = SpellCastResult.SpellCastOk)
        {
            if (m_spellState == SpellState.Finished)
                return;

            m_spellState = SpellState.Finished;

            if (m_scriptResult != null && (m_scriptWaitsForSpellHit || result != SpellCastResult.SpellCastOk))
                m_scriptResult.SetResult(result);

            if (m_caster == null)
                return;

            Unit unitCaster = m_caster.ToUnit();
            if (unitCaster == null)
                return;

            // successful cast of the initial autorepeat spell is moved to idle state so that it is not deleted as long as autorepeat is active
            if (IsAutoRepeat() && unitCaster.GetCurrentSpell(CurrentSpellTypes.AutoRepeat) == this)
                m_spellState = SpellState.Idle;

            if (m_spellInfo.IsChanneled())
                unitCaster.UpdateInterruptMask();

            if (unitCaster.HasUnitState(UnitState.Casting) && !unitCaster.IsNonMeleeSpellCast(false, false, true))
                unitCaster.ClearUnitState(UnitState.Casting);

            // Unsummon summon as possessed creatures on spell cancel
            if (m_spellInfo.IsChanneled() && unitCaster.IsTypeId(TypeId.Player))
            {
                Unit charm = unitCaster.GetCharmed();
                if (charm != null)
                    if (charm.IsTypeId(TypeId.Unit) && charm.ToCreature().HasUnitTypeMask(UnitTypeMask.Puppet)
                        && charm.m_unitData.CreatedBySpell == m_spellInfo.Id)
                        ((Puppet)charm).UnSummon();
            }

            Creature creatureCaster = unitCaster.ToCreature();
            if (creatureCaster != null)
                creatureCaster.ReleaseSpellFocus(this);

            Unit.ProcSkillsAndAuras(unitCaster, null, new ProcFlagsInit(ProcFlags.CastEnded), new ProcFlagsInit(), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, this, null, null);

            if (IsEmpowerSpell())
            {
                // Empower spells trigger gcd at the end of cast instead of at start
                SpellInfo gcd = Global.SpellMgr.GetSpellInfo(SpellConst.EmpowerHardcodedGCD, Difficulty.None);
                if (gcd != null)
                    unitCaster.GetSpellHistory().AddGlobalCooldown(gcd, TimeSpan.FromMilliseconds(gcd.StartRecoveryTime));
            }

            if (result != SpellCastResult.SpellCastOk)
            {
                // on failure (or manual cancel) send TraitConfigCommitFailed to revert talent UI saved config selection
                if (m_caster.IsPlayer() && m_spellInfo.HasEffect(SpellEffectName.ChangeActiveCombatTraitConfig))
                    if (m_customArg is TraitConfig)
                        m_caster.ToPlayer().SendPacket(new TraitConfigCommitFailed((m_customArg as TraitConfig).ID));

                if (IsEmpowerSpell())
                {
                    unitCaster.GetSpellHistory().ResetCooldown(m_spellInfo.Id, true);
                    RefundPower();
                }

                return;
            }

            if (unitCaster.IsTypeId(TypeId.Unit) && unitCaster.ToCreature().IsSummon())
            {
                // Unsummon statue
                uint spell = unitCaster.m_unitData.CreatedBySpell;
                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spell, GetCastDifficulty());
                if (spellInfo != null && spellInfo.IconFileDataId == 134230)
                {
                    Log.outDebug(LogFilter.Spells, "Statue {0} is unsummoned in spell {1} finish", unitCaster.GetGUID().ToString(), m_spellInfo.Id);
                    // Avoid infinite loops with setDeathState(JUST_DIED) being called over and over
                    // It might make sense to do this check in Unit::setDeathState() and all overloaded functions
                    if (unitCaster.GetDeathState() != DeathState.JustDied)
                        unitCaster.SetDeathState(DeathState.JustDied);
                    return;
                }
            }

            // potions disabled by client, send event "not in combat" if need
            if (unitCaster.IsTypeId(TypeId.Player))
            {
                if (m_triggeredByAuraSpell == null)
                    unitCaster.ToPlayer().UpdatePotionCooldown(this);
            }

            // Stop Attack for some spells
            if (m_spellInfo.HasAttribute(SpellAttr0.CancelsAutoAttackCombat))
                unitCaster.AttackStop();
        }

        static void FillSpellCastFailedArgs<T>(T packet, ObjectGuid castId, SpellInfo spellInfo, SpellCastResult result, SpellCustomErrors customError, int? param1, int? param2, Player caster) where T : CastFailedBase
        {
            packet.CastID = castId;
            packet.SpellID = (int)spellInfo.Id;
            packet.Reason = result;

            switch (result)
            {
                case SpellCastResult.NotReady:
                    if (param1.HasValue)
                        packet.FailedArg1 = (int)param1;
                    else
                        packet.FailedArg1 = 0;// unknown (value 1 update cooldowns on client flag)
                    break;
                case SpellCastResult.RequiresSpellFocus:
                    if (param1.HasValue)
                        packet.FailedArg1 = (int)param1;
                    else
                        packet.FailedArg1 = (int)spellInfo.RequiresSpellFocus;  // SpellFocusObject.dbc id
                    break;
                case SpellCastResult.RequiresArea:                    // AreaTable.dbc id
                    if (param1.HasValue)
                        packet.FailedArg1 = (int)param1;
                    else
                    {
                        // hardcode areas limitation case
                        switch (spellInfo.Id)
                        {
                            case 41617:                                 // Cenarion Mana Salve
                            case 41619:                                 // Cenarion Healing Salve
                                packet.FailedArg1 = 3905;
                                break;
                            case 41618:                                 // Bottled Nethergon Energy
                            case 41620:                                 // Bottled Nethergon Vapor
                                packet.FailedArg1 = 3842;
                                break;
                            case 45373:                                 // Bloodberry Elixir
                                packet.FailedArg1 = 4075;
                                break;
                            default:                                    // default case (don't must be)
                                packet.FailedArg1 = 0;
                                break;
                        }
                    }
                    break;
                case SpellCastResult.Totems:
                    if (param1.HasValue)
                    {
                        packet.FailedArg1 = (int)param1;
                        if (param2.HasValue)
                            packet.FailedArg2 = (int)param2;
                    }
                    else
                    {
                        if (spellInfo.Totem[0] != 0)
                            packet.FailedArg1 = (int)spellInfo.Totem[0];
                        if (spellInfo.Totem[1] != 0)
                            packet.FailedArg2 = (int)spellInfo.Totem[1];
                    }
                    break;
                case SpellCastResult.TotemCategory:
                    if (param1.HasValue)
                    {
                        packet.FailedArg1 = (int)param1;
                        if (param2.HasValue)
                            packet.FailedArg2 = (int)param2;
                    }
                    else
                    {
                        if (spellInfo.TotemCategory[0] != 0)
                            packet.FailedArg1 = (int)spellInfo.TotemCategory[0];
                        if (spellInfo.TotemCategory[1] != 0)
                            packet.FailedArg2 = (int)spellInfo.TotemCategory[1];
                    }
                    break;
                case SpellCastResult.EquippedItemClass:
                case SpellCastResult.EquippedItemClassMainhand:
                case SpellCastResult.EquippedItemClassOffhand:
                    if (param1.HasValue && param2.HasValue)
                    {
                        packet.FailedArg1 = (int)param1;
                        packet.FailedArg2 = (int)param2;
                    }
                    else
                    {
                        packet.FailedArg1 = (int)spellInfo.EquippedItemClass;
                        packet.FailedArg2 = spellInfo.EquippedItemSubClassMask;
                    }
                    break;
                case SpellCastResult.TooManyOfItem:
                {
                    if (param1.HasValue)
                        packet.FailedArg1 = (int)param1;
                    else
                    {
                        uint item = 0;
                        foreach (var spellEffectInfo in spellInfo.GetEffects())
                            if (spellEffectInfo.ItemType != 0)
                                item = spellEffectInfo.ItemType;

                        ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(item);
                        if (proto != null && proto.GetItemLimitCategory() != 0)
                            packet.FailedArg1 = (int)proto.GetItemLimitCategory();
                    }
                    break;
                }
                case SpellCastResult.PreventedByMechanic:
                    if (param1.HasValue)
                        packet.FailedArg1 = (int)param1;
                    else
                        packet.FailedArg1 = (int)spellInfo.GetAllEffectsMechanicMask();  // SpellMechanic.dbc id
                    break;
                case SpellCastResult.NeedExoticAmmo:
                    if (param1.HasValue)
                        packet.FailedArg1 = (int)param1;
                    else
                        packet.FailedArg1 = spellInfo.EquippedItemSubClassMask; // seems correct...
                    break;
                case SpellCastResult.NeedMoreItems:
                    if (param1.HasValue && param2.HasValue)
                    {
                        packet.FailedArg1 = (int)param1;
                        packet.FailedArg2 = (int)param2;
                    }
                    else
                    {
                        packet.FailedArg1 = 0;                              // Item id
                        packet.FailedArg2 = 0;                              // Item count?
                    }
                    break;
                case SpellCastResult.MinSkill:
                    if (param1.HasValue && param2.HasValue)
                    {
                        packet.FailedArg1 = (int)param1;
                        packet.FailedArg2 = (int)param2;
                    }
                    else
                    {
                        packet.FailedArg1 = 0;                              // SkillLine.dbc id
                        packet.FailedArg2 = 0;                              // required skill value
                    }
                    break;
                case SpellCastResult.FishingTooLow:
                    if (param1.HasValue)
                        packet.FailedArg1 = (int)param1;
                    else
                        packet.FailedArg1 = 0;                              // required fishing skill
                    break;
                case SpellCastResult.CustomError:
                    packet.FailedArg1 = (int)customError;
                    break;
                case SpellCastResult.Silenced:
                    if (param1.HasValue)
                        packet.FailedArg1 = (int)param1;
                    else
                        packet.FailedArg1 = 0;                              // Unknown
                    break;
                case SpellCastResult.Reagents:
                {
                    if (param1.HasValue)
                        packet.FailedArg1 = (int)param1;
                    else
                    {
                        for (uint i = 0; i < SpellConst.MaxReagents; i++)
                        {
                            if (spellInfo.Reagent[i] <= 0)
                                continue;

                            uint itemid = (uint)spellInfo.Reagent[i];
                            uint itemcount = spellInfo.ReagentCount[i];

                            if (caster != null && !caster.HasItemCount(itemid, itemcount))
                            {
                                packet.FailedArg1 = (int)itemid;  // first missing item
                                break;
                            }
                        }
                    }

                    if (param2.HasValue)
                        packet.FailedArg2 = (int)param2;
                    else if (!param1.HasValue)
                    {
                        foreach (var reagentsCurrency in spellInfo.ReagentsCurrency)
                        {
                            if (caster != null && !caster.HasCurrency(reagentsCurrency.CurrencyTypesID, reagentsCurrency.CurrencyCount))
                            {
                                packet.FailedArg1 = -1;
                                packet.FailedArg2 = reagentsCurrency.CurrencyTypesID;
                                break;
                            }
                        }
                    }
                    break;
                }
                case SpellCastResult.CantUntalent:
                {
                    Cypher.Assert(param1.HasValue);
                    packet.FailedArg1 = (int)param1;
                    break;
                }
                // TODO: SPELL_FAILED_NOT_STANDING
                default:
                    break;
            }
        }

        public void SendCastResult(SpellCastResult result, int? param1 = null, int? param2 = null)
        {
            if (result == SpellCastResult.SpellCastOk)
                return;

            Player receiver = m_caster.ToPlayer();
            if (m_spellInfo.HasAttribute(SpellAttr7.ReportSpellFailureToUnitTarget))
            {
                Player target = m_targets.GetUnitTarget()?.ToPlayer();
                if (target != null)
                    receiver = target;
            }

            if (receiver == null)
                return;

            if (receiver.IsLoading())  // don't send cast results at loading time
                return;

            if (_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.DontReportCastError))
                result = SpellCastResult.DontReport;

            CastFailed castFailed = new();
            castFailed.Visual = m_SpellVisual;
            FillSpellCastFailedArgs(castFailed, m_castId, m_spellInfo, result, m_customError, param1, param2, m_caster.ToPlayer());
            receiver.SendPacket(castFailed);
        }

        public void SendPetCastResult(SpellCastResult result, int? param1 = null, int? param2 = null)
        {
            if (result == SpellCastResult.SpellCastOk)
                return;

            Unit owner = m_caster.GetCharmerOrOwner();
            if (owner == null || !owner.IsTypeId(TypeId.Player))
                return;

            if (_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.DontReportCastError))
                result = SpellCastResult.DontReport;

            PetCastFailed petCastFailed = new();
            FillSpellCastFailedArgs(petCastFailed, m_castId, m_spellInfo, result, SpellCustomErrors.None, param1, param2, owner.ToPlayer());
            owner.ToPlayer().SendPacket(petCastFailed);
        }

        public static void SendCastResult(Player caster, SpellInfo spellInfo, SpellCastVisual spellVisual, ObjectGuid castCount, SpellCastResult result, SpellCustomErrors customError = SpellCustomErrors.None, int? param1 = null, int? param2 = null)
        {
            if (result == SpellCastResult.SpellCastOk)
                return;

            CastFailed packet = new();
            packet.Visual = spellVisual;
            FillSpellCastFailedArgs(packet, castCount, spellInfo, result, customError, param1, param2, caster);
            caster.SendPacket(packet);
        }

        void SendMountResult(MountResult result)
        {
            if (result == MountResult.Ok)
                return;

            if (!m_caster.IsPlayer())
                return;

            Player caster = m_caster.ToPlayer();
            if (caster.IsLoading())  // don't send mount results at loading time
                return;

            MountResultPacket packet = new();
            packet.Result = (uint)result;
            caster.SendPacket(packet);
        }

        void SendSpellStart()
        {
            if (!IsNeedSendToClient())
                return;

            SpellCastFlags castFlags = SpellCastFlags.HasTrajectory;
            uint schoolImmunityMask = 0;
            ulong mechanicImmunityMask = 0;
            Unit unitCaster = m_caster.ToUnit();
            if (unitCaster != null)
            {
                schoolImmunityMask = m_timer != 0 ? unitCaster.GetSchoolImmunityMask() : 0;
                mechanicImmunityMask = m_timer != 0 ? m_spellInfo.GetMechanicImmunityMask(unitCaster) : 0;
            }

            if (schoolImmunityMask != 0 || mechanicImmunityMask != 0)
                castFlags |= SpellCastFlags.Immunity;

            if ((IsTriggered() && !m_spellInfo.IsAutoRepeatRangedSpell()) || m_triggeredByAuraSpell != null)
                castFlags |= SpellCastFlags.Pending;

            if (m_spellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) || m_spellInfo.HasAttribute(SpellAttr10.UsesRangedSlotCosmeticOnly) || m_spellInfo.HasAttribute(SpellCustomAttributes.NeedsAmmoData))
                castFlags |= SpellCastFlags.Projectile;

            if ((m_caster.IsTypeId(TypeId.Player) || (m_caster.IsTypeId(TypeId.Unit) && m_caster.ToCreature().IsPet())) && m_powerCost.Any(cost => cost.Power != PowerType.Health))
                castFlags |= SpellCastFlags.PowerLeftSelf;

            if (HasPowerTypeCost(PowerType.Runes))
                castFlags |= SpellCastFlags.NoGCD; // not needed, but Blizzard sends it

            if (m_spellInfo.HasAttribute(SpellAttr8.HealPrediction) && m_casttime != 0 && m_caster.IsUnit())
                castFlags |= SpellCastFlags.HealPrediction;

            SpellStart packet = new();
            SpellCastData castData = packet.Cast;

            if (m_CastItem != null)
                castData.CasterGUID = m_CastItem.GetGUID();
            else
                castData.CasterGUID = m_caster.GetGUID();

            castData.CasterUnit = m_caster.GetGUID();
            castData.CastID = m_castId;
            castData.OriginalCastID = m_originalCastId;
            castData.SpellID = (int)m_spellInfo.Id;
            castData.Visual = m_SpellVisual;
            castData.CastFlags = castFlags;
            castData.CastFlagsEx = m_castFlagsEx;
            castData.CastTime = (uint)m_casttime;

            m_targets.Write(castData.Target);

            if (castFlags.HasAnyFlag(SpellCastFlags.PowerLeftSelf))
            {
                foreach (SpellPowerCost cost in m_powerCost)
                {
                    SpellPowerData powerData;
                    powerData.Type = cost.Power;
                    powerData.Cost = m_caster.ToUnit().GetPower(cost.Power);
                    castData.RemainingPower.Add(powerData);
                }
            }

            if (castFlags.HasAnyFlag(SpellCastFlags.RuneList)) // rune cooldowns list
            {
                castData.RemainingRunes = new();

                RuneData runeData = castData.RemainingRunes;
                //TODO: There is a crash caused by a spell with CAST_FLAG_RUNE_LIST casted by a creature
                //The creature is the mover of a player, so HandleCastSpellOpcode uses it as the caster

                Player player = m_caster.ToPlayer();
                if (player != null)
                {
                    runeData.Start = m_runesState; // runes state before
                    runeData.Count = player.GetRunesState(); // runes state after
                    for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
                    {
                        // float casts ensure the division is performed on floats as we need float result
                        float baseCd = player.GetRuneBaseCooldown();
                        runeData.Cooldowns.Add((byte)((baseCd - player.GetRuneCooldown(i)) / baseCd * 255)); // rune cooldown passed
                    }
                }
                else
                {
                    runeData.Start = 0;
                    runeData.Count = 0;
                    for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
                        runeData.Cooldowns.Add(0);
                }
            }

            if (castFlags.HasFlag(SpellCastFlags.Projectile))
                castData.AmmoDisplayID = (int)GetSpellCastDataAmmo();

            if (castFlags.HasFlag(SpellCastFlags.Immunity))
            {
                castData.Immunities.School = schoolImmunityMask;
                castData.Immunities.Value = (uint)mechanicImmunityMask;
            }

            UpdateSpellHealPrediction(castData.Predict, false);

            m_caster.SendMessageToSet(packet, true);
        }

        void SendSpellGo()
        {
            // not send invisible spell casting
            if (!IsNeedSendToClient())
                return;

            Log.outDebug(LogFilter.Spells, "Sending SMSG_SPELL_GO id={0}", m_spellInfo.Id);

            SpellCastFlags castFlags = SpellCastFlags.Unk9;

            // triggered spells with spell visual != 0
            if ((IsTriggered() && !m_spellInfo.IsAutoRepeatRangedSpell()) || m_triggeredByAuraSpell != null)
                castFlags |= SpellCastFlags.Pending;

            if (m_spellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) || m_spellInfo.HasAttribute(SpellAttr10.UsesRangedSlotCosmeticOnly) || m_spellInfo.HasAttribute(SpellCustomAttributes.NeedsAmmoData))
                castFlags |= SpellCastFlags.Projectile;                        // arrows/bullets visual

            if ((m_caster.IsPlayer() || (m_caster.IsTypeId(TypeId.Unit) && m_caster.ToCreature().IsPet())) && m_powerCost.Any(cost => cost.Power != PowerType.Health))
                castFlags |= SpellCastFlags.PowerLeftSelf;

            if (m_caster.IsPlayer() && m_caster.ToPlayer().GetClass() == Class.Deathknight &&
                HasPowerTypeCost(PowerType.Runes) && !_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnorePowerCost))
            {
                castFlags |= SpellCastFlags.NoGCD;                   // same as in SMSG_SPELL_START
                castFlags |= SpellCastFlags.RuneList;                    // rune cooldowns list
            }

            if (m_targets.HasTraj())
                castFlags |= SpellCastFlags.AdjustMissile;

            if (m_spellInfo.StartRecoveryTime == 0)
                castFlags |= SpellCastFlags.NoGCD;

            SpellGo packet = new();
            SpellCastData castData = packet.Cast;

            if (m_CastItem != null)
                castData.CasterGUID = m_CastItem.GetGUID();
            else
                castData.CasterGUID = m_caster.GetGUID();

            castData.CasterUnit = m_caster.GetGUID();
            castData.CastID = m_castId;
            castData.OriginalCastID = m_originalCastId;
            castData.SpellID = (int)m_spellInfo.Id;
            castData.Visual = m_SpellVisual;
            castData.CastFlags = castFlags;
            castData.CastFlagsEx = m_castFlagsEx;
            castData.CastTime = Time.GetMSTime();

            castData.HitTargets = new List<ObjectGuid>();
            UpdateSpellCastDataTargets(castData);

            m_targets.Write(castData.Target);

            if (Convert.ToBoolean(castFlags & SpellCastFlags.PowerLeftSelf))
            {
                castData.RemainingPower = new List<SpellPowerData>();
                foreach (SpellPowerCost cost in m_powerCost)
                {
                    SpellPowerData powerData;
                    powerData.Type = cost.Power;
                    powerData.Cost = m_caster.ToUnit().GetPower(cost.Power);
                    castData.RemainingPower.Add(powerData);
                }
            }

            if (Convert.ToBoolean(castFlags & SpellCastFlags.RuneList))                   // rune cooldowns list
            {
                castData.RemainingRunes = new();
                RuneData runeData = castData.RemainingRunes;

                Player player = m_caster.ToPlayer();
                runeData.Start = m_runesState; // runes state before
                runeData.Count = player.GetRunesState(); // runes state after
                for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
                {
                    // float casts ensure the division is performed on floats as we need float result
                    float baseCd = (float)player.GetRuneBaseCooldown();
                    runeData.Cooldowns.Add((byte)((baseCd - (float)player.GetRuneCooldown(i)) / baseCd * 255)); // rune cooldown passed
                }
            }

            if (castFlags.HasFlag(SpellCastFlags.AdjustMissile))
            {
                castData.MissileTrajectory.TravelTime = (uint)m_delayMoment;
                castData.MissileTrajectory.Pitch = m_targets.GetPitch();
            }

            packet.LogData.Initialize(this);

            m_caster.SendCombatLogMessage(packet);
        }

        // Writes miss and hit targets for a SMSG_SPELL_GO packet
        void UpdateSpellCastDataTargets(SpellCastData data)
        {
            // This function also fill data for channeled spells:
            // m_needAliveTargetMask req for stop channelig if one target die
            foreach (var targetInfo in m_UniqueTargetInfo)
            {
                if (targetInfo.EffectMask == 0)                  // No effect apply - all immuned add state
                                                                 // possibly SPELL_MISS_IMMUNE2 for this??
                    targetInfo.MissCondition = SpellMissInfo.Immune2;

                if (targetInfo.MissCondition == SpellMissInfo.None || (targetInfo.MissCondition == SpellMissInfo.Block && !m_spellInfo.HasAttribute(SpellAttr3.CompletelyBlocked))) // Add only hits and partial blocked
                {
                    data.HitTargets.Add(targetInfo.TargetGUID);
                    data.HitStatus.Add(new SpellHitStatus(SpellMissInfo.None));

                    m_channelTargetEffectMask |= targetInfo.EffectMask;
                }
                else // misses
                {
                    data.MissTargets.Add(targetInfo.TargetGUID);

                    data.MissStatus.Add(new SpellMissStatus(targetInfo.MissCondition, targetInfo.ReflectResult));
                }
            }

            foreach (GOTargetInfo targetInfo in m_UniqueGOTargetInfo)
                data.HitTargets.Add(targetInfo.TargetGUID); // Always hits

            foreach (CorpseTargetInfo targetInfo in m_UniqueCorpseTargetInfo)
                data.HitTargets.Add(targetInfo.TargetGUID); // Always hits

            // Reset m_needAliveTargetMask for non channeled spell
            if (!m_spellInfo.IsChanneled())
                m_channelTargetEffectMask = 0;
        }

        uint GetSpellCastDataAmmo()
        {
            InventoryType ammoInventoryType = 0;
            uint ammoDisplayID = 0;

            Player playerCaster = m_caster.ToPlayer();
            if (playerCaster != null)
            {
                Item pItem = playerCaster.GetWeaponForAttack(WeaponAttackType.RangedAttack);
                if (pItem != null)
                {
                    ammoInventoryType = pItem.GetTemplate().GetInventoryType();
                    if (ammoInventoryType == InventoryType.Thrown)
                        ammoDisplayID = pItem.GetDisplayId(playerCaster);
                    else if (playerCaster.HasAura(46699))      // Requires No Ammo
                    {
                        ammoDisplayID = 5996;                   // normal arrow
                        ammoInventoryType = InventoryType.Ammo;
                    }
                }
            }
            else
            {
                Unit unitCaster = m_caster.ToUnit();
                if (unitCaster != null)
                {
                    uint nonRangedAmmoDisplayID = 0;
                    InventoryType nonRangedAmmoInventoryType = 0;
                    for (byte i = (int)WeaponAttackType.BaseAttack; i < (int)WeaponAttackType.Max; ++i)
                    {
                        uint itemId = unitCaster.GetVirtualItemId(i);
                        if (itemId != 0)
                        {
                            ItemRecord itemEntry = CliDB.ItemStorage.LookupByKey(itemId);
                            if (itemEntry != null)
                            {
                                if (itemEntry.ClassID == ItemClass.Weapon)
                                {
                                    switch ((ItemSubClassWeapon)itemEntry.SubclassID)
                                    {
                                        case ItemSubClassWeapon.Thrown:
                                            ammoDisplayID = Global.DB2Mgr.GetItemDisplayId(itemId, unitCaster.GetVirtualItemAppearanceMod(i));
                                            ammoInventoryType = (InventoryType)itemEntry.inventoryType;
                                            break;
                                        case ItemSubClassWeapon.Bow:
                                        case ItemSubClassWeapon.Crossbow:
                                            ammoDisplayID = 5996;       // is this need fixing?
                                            ammoInventoryType = InventoryType.Ammo;
                                            break;
                                        case ItemSubClassWeapon.Gun:
                                            ammoDisplayID = 5998;       // is this need fixing?
                                            ammoInventoryType = InventoryType.Ammo;
                                            break;
                                        default:
                                            nonRangedAmmoDisplayID = Global.DB2Mgr.GetItemDisplayId(itemId, unitCaster.GetVirtualItemAppearanceMod(i));
                                            nonRangedAmmoInventoryType = itemEntry.inventoryType;
                                            break;
                                    }

                                    if (ammoDisplayID != 0)
                                        break;
                                }
                            }
                        }
                    }

                    if (ammoDisplayID == 0 && ammoInventoryType == 0)
                    {
                        ammoDisplayID = nonRangedAmmoDisplayID;
                        ammoInventoryType = nonRangedAmmoInventoryType;
                    }
                }
            }

            return ammoDisplayID;
        }

        static (int, SpellHealPredictionType) CalcPredictedHealing(SpellInfo spellInfo, Unit unitCaster, Unit target, uint castItemEntry, int castItemLevel, Spell spell, bool withPeriodic)
        {
            int points = 0;
            SpellHealPredictionType type = SpellHealPredictionType.Target;
            foreach (SpellEffectInfo spellEffectInfo in spellInfo.GetEffects())
            {
                switch (spellEffectInfo.Effect)
                {
                    case SpellEffectName.Heal:
                    case SpellEffectName.HealPct:
                        points += unitCaster.SpellHealingBonusDone(target, spellInfo, spellEffectInfo.CalcValue(unitCaster, null, target, castItemEntry, castItemLevel), DamageEffectType.Direct, spellEffectInfo, 1, spell);

                        if (target != unitCaster && (spellEffectInfo.TargetA.GetTarget() == Targets.UnitCaster || spellEffectInfo.TargetB.GetTarget() == Targets.UnitCaster))
                            type = SpellHealPredictionType.TargetAndCaster;    // Binding Heal-like spells
                        else if (spellEffectInfo.TargetA.GetCheckType() == SpellTargetCheckTypes.Party || spellEffectInfo.TargetB.GetCheckType() == SpellTargetCheckTypes.Party)
                            type = SpellHealPredictionType.TargetParty;         // Prayer of Healing (old party-wide targeting)
                        break;
                    default:
                        break;
                }

                if (withPeriodic)
                {
                    switch (spellEffectInfo.ApplyAuraName)
                    {
                        case AuraType.PeriodicHeal:
                        case AuraType.ObsModHealth:
                            points += (int)(unitCaster.SpellHealingBonusDone(target, spellInfo, spellEffectInfo.CalcValue(unitCaster, null, target, castItemEntry, castItemLevel), DamageEffectType.Direct, spellEffectInfo, 1, spell) * spellInfo.GetMaxTicks());
                            break;
                        case AuraType.PeriodicTriggerSpell:
                            SpellInfo triggered = Global.SpellMgr.GetSpellInfo(spellEffectInfo.TriggerSpell, spellInfo.Difficulty);
                            if (triggered != null)
                                points += CalcPredictedHealing(triggered, unitCaster, target, castItemEntry, castItemLevel, null, withPeriodic).Item1;
                            break;
                        default:
                            break;
                    }
                }
            }

            return (points, type);
        }

        bool CanStopMovementForSpellCasting(MovementGeneratorType type)
        {
            // MovementGenerators that don't check Unit::IsMovementPreventedByCasting
            switch (type)
            {
                case MovementGeneratorType.Home:
                case MovementGeneratorType.Flight:
                case MovementGeneratorType.Effect:    // knockbacks, jumps, falling, land/takeoff transitions
                    return false;
                default:
                    break;
            }
            return true;
        }

        void UpdateSpellHealPrediction(SpellHealPrediction healPrediction, bool withPeriodic)
        {
            healPrediction.BeaconGUID = ObjectGuid.Empty;
            healPrediction.Points = 0;
            healPrediction.Type = SpellHealPredictionType.Target;

            Unit unitCaster = m_caster.ToUnit();
            Unit target = m_targets.GetUnitTarget();
            if (target != null)
            {
                var (points, type) = CalcPredictedHealing(m_spellInfo, unitCaster, target, m_castItemEntry, m_castItemLevel, this, withPeriodic);
                healPrediction.Points = (uint)points;
                healPrediction.Type = type;
            }

            uint beaconSpellId = 53651;

            if (healPrediction.Type == SpellHealPredictionType.Target && unitCaster.HasAura(beaconSpellId, unitCaster.GetGUID()))
            {
                var beacon = unitCaster.GetSingleCastAuras().Find(aura => aura.GetSpellInfo().GetEffects().Count > 1 && aura.GetSpellInfo().GetEffect(1).TriggerSpell == beaconSpellId);
                if (beacon != null)
                {
                    healPrediction.BeaconGUID = beacon.GetOwner().GetGUID();
                    healPrediction.Type = SpellHealPredictionType.TargetAndBeacon;
                }
            }
        }

        void SendSpellExecuteLog()
        {
            if (_executeLogEffects.Empty())
                return;

            SpellExecuteLog spellExecuteLog = new();

            spellExecuteLog.Caster = m_caster.GetGUID();
            spellExecuteLog.SpellID = m_spellInfo.Id;
            spellExecuteLog.Effects = _executeLogEffects.Values.ToList();
            spellExecuteLog.LogData.Initialize(this);

            m_caster.SendCombatLogMessage(spellExecuteLog);

            _executeLogEffects.Clear();
        }

        public SpellLogEffect GetExecuteLogEffect(SpellEffectName effect)
        {
            var spellLogEffect = _executeLogEffects.LookupByKey(effect);
            if (spellLogEffect != null)
                return spellLogEffect;

            SpellLogEffect executeLogEffect = new();
            executeLogEffect.Effect = (int)effect;
            _executeLogEffects.Add(effect, executeLogEffect);
            return executeLogEffect;
        }

        void ExecuteLogEffectTakeTargetPower(SpellEffectName effect, Unit target, PowerType powerType, uint points, float amplitude)
        {
            SpellLogEffectPowerDrainParams spellLogEffectPowerDrainParams;

            spellLogEffectPowerDrainParams.Victim = target.GetGUID();
            spellLogEffectPowerDrainParams.Points = points;
            spellLogEffectPowerDrainParams.PowerType = powerType;
            spellLogEffectPowerDrainParams.Amplitude = amplitude;

            GetExecuteLogEffect(effect).PowerDrainTargets.Add(spellLogEffectPowerDrainParams);
        }

        void ExecuteLogEffectExtraAttacks(SpellEffectName effect, Unit victim, uint numAttacks)
        {
            SpellLogEffectExtraAttacksParams spellLogEffectExtraAttacksParams;
            spellLogEffectExtraAttacksParams.Victim = victim.GetGUID();
            spellLogEffectExtraAttacksParams.NumAttacks = numAttacks;

            GetExecuteLogEffect(effect).ExtraAttacksTargets.Add(spellLogEffectExtraAttacksParams);
        }

        void SendSpellInterruptLog(Unit victim, uint spellId)
        {
            SpellInterruptLog data = new();
            data.Caster = m_caster.GetGUID();
            data.Victim = victim.GetGUID();
            data.InterruptedSpellID = m_spellInfo.Id;
            data.SpellID = spellId;

            m_caster.SendMessageToSet(data, true);
        }

        void ExecuteLogEffectDurabilityDamage(SpellEffectName effect, Unit victim, int itemId, int amount)
        {
            SpellLogEffectDurabilityDamageParams spellLogEffectDurabilityDamageParams;
            spellLogEffectDurabilityDamageParams.Victim = victim.GetGUID();
            spellLogEffectDurabilityDamageParams.ItemID = itemId;
            spellLogEffectDurabilityDamageParams.Amount = amount;

            GetExecuteLogEffect(effect).DurabilityDamageTargets.Add(spellLogEffectDurabilityDamageParams);
        }

        void ExecuteLogEffectOpenLock(SpellEffectName effect, WorldObject obj)
        {
            SpellLogEffectGenericVictimParams spellLogEffectGenericVictimParams;
            spellLogEffectGenericVictimParams.Victim = obj.GetGUID();

            GetExecuteLogEffect(effect).GenericVictimTargets.Add(spellLogEffectGenericVictimParams);
        }

        void ExecuteLogEffectCreateItem(SpellEffectName effect, uint entry)
        {
            SpellLogEffectTradeSkillItemParams spellLogEffectTradeSkillItemParams;
            spellLogEffectTradeSkillItemParams.ItemID = (int)entry;

            GetExecuteLogEffect(effect).TradeSkillTargets.Add(spellLogEffectTradeSkillItemParams);
        }

        void ExecuteLogEffectDestroyItem(SpellEffectName effect, uint entry)
        {
            SpellLogEffectFeedPetParams spellLogEffectFeedPetParams;
            spellLogEffectFeedPetParams.ItemID = (int)entry;

            GetExecuteLogEffect(effect).FeedPetTargets.Add(spellLogEffectFeedPetParams);
        }

        void ExecuteLogEffectSummonObject(SpellEffectName effect, WorldObject obj)
        {
            SpellLogEffectGenericVictimParams spellLogEffectGenericVictimParams;
            spellLogEffectGenericVictimParams.Victim = obj.GetGUID();

            GetExecuteLogEffect(effect).GenericVictimTargets.Add(spellLogEffectGenericVictimParams);
        }

        void ExecuteLogEffectUnsummonObject(SpellEffectName effect, WorldObject obj)
        {
            SpellLogEffectGenericVictimParams spellLogEffectGenericVictimParams;
            spellLogEffectGenericVictimParams.Victim = obj.GetGUID();

            GetExecuteLogEffect(effect).GenericVictimTargets.Add(spellLogEffectGenericVictimParams);
        }

        void ExecuteLogEffectResurrect(SpellEffectName effect, Unit target)
        {
            SpellLogEffectGenericVictimParams spellLogEffectGenericVictimParams;
            spellLogEffectGenericVictimParams.Victim = target.GetGUID();

            GetExecuteLogEffect(effect).GenericVictimTargets.Add(spellLogEffectGenericVictimParams);
        }

        void SendInterrupted(byte result)
        {
            SpellFailure failurePacket = new();
            failurePacket.CasterUnit = m_caster.GetGUID();
            failurePacket.CastID = m_castId;
            failurePacket.SpellID = m_spellInfo.Id;
            failurePacket.Visual = m_SpellVisual;
            failurePacket.Reason = result;
            m_caster.SendMessageToSet(failurePacket, true);

            SpellFailedOther failedPacket = new();
            failedPacket.CasterUnit = m_caster.GetGUID();
            failedPacket.CastID = m_castId;
            failedPacket.SpellID = m_spellInfo.Id;
            failedPacket.Visual = m_SpellVisual;
            failedPacket.Reason = result;
            m_caster.SendMessageToSet(failedPacket, true);
        }

        public void SendChannelUpdate(uint time, SpellCastResult? result = null)
        {
            // GameObjects don't channel
            Unit unitCaster = m_caster.ToUnit();
            if (unitCaster == null)
                return;

            if (time == 0)
            {
                unitCaster.ClearChannelObjects();
                unitCaster.SetChannelSpellId(0);
                unitCaster.SetChannelVisual(new SpellCastVisualField());
                unitCaster.SetSpellEmpowerStage(-1);
            }

            if (IsEmpowerSpell())
            {
                SpellEmpowerUpdate spellEmpowerUpdate = new();
                spellEmpowerUpdate.CastID = m_castId;
                spellEmpowerUpdate.CasterGUID = unitCaster.GetGUID();
                spellEmpowerUpdate.TimeRemaining = TimeSpan.FromMilliseconds(time);
                if (time > 0)
                    spellEmpowerUpdate.StageDurations.AddRange(m_empower.StageDurations);
                else if (result.HasValue && result != SpellCastResult.SpellCastOk)
                    spellEmpowerUpdate.Status = 1;
                else
                    spellEmpowerUpdate.Status = 4;

                unitCaster.SendMessageToSet(spellEmpowerUpdate, true);
            }
            else
            {
                SpellChannelUpdate spellChannelUpdate = new();
                spellChannelUpdate.CasterGUID = unitCaster.GetGUID();
                spellChannelUpdate.TimeRemaining = (int)time;
                unitCaster.SendMessageToSet(spellChannelUpdate, true);
            }
        }

        void SendChannelStart(uint duration)
        {
            // GameObjects don't channel
            Unit unitCaster = m_caster.ToUnit();
            if (unitCaster == null)
                return;

            m_timer = (int)duration;

            if (!m_UniqueTargetInfo.Empty() || !m_UniqueGOTargetInfo.Empty())
            {
                uint channelAuraMask = 0;
                uint explicitTargetEffectMask = 0xFFFFFFFF;
                // if there is an explicit target, only add channel objects from effects that also hit ut
                if (!m_targets.GetUnitTargetGUID().IsEmpty())
                {
                    var explicitTarget = m_UniqueTargetInfo.Find(target => target.TargetGUID == m_targets.GetUnitTargetGUID());
                    if (explicitTarget != null)
                        explicitTargetEffectMask = explicitTarget.EffectMask;
                }

                foreach (var spellEffectInfo in m_spellInfo.GetEffects())
                    if (spellEffectInfo.Effect == SpellEffectName.ApplyAura && (explicitTargetEffectMask & (1u << (int)spellEffectInfo.EffectIndex)) != 0)
                        channelAuraMask |= 1u << (int)spellEffectInfo.EffectIndex;

                foreach (TargetInfo target in m_UniqueTargetInfo)
                {
                    if ((target.EffectMask & channelAuraMask) == 0)
                        continue;

                    SpellAttr1 requiredAttribute = target.TargetGUID != unitCaster.GetGUID() ? SpellAttr1.IsChannelled : SpellAttr1.IsSelfChannelled;
                    if (!m_spellInfo.HasAttribute(requiredAttribute))
                        continue;

                    unitCaster.AddChannelObject(target.TargetGUID);
                }

                foreach (GOTargetInfo target in m_UniqueGOTargetInfo)
                    if ((target.EffectMask & channelAuraMask) != 0)
                        unitCaster.AddChannelObject(target.TargetGUID);
            }
            else if (m_spellInfo.HasAttribute(SpellAttr1.IsSelfChannelled))
                unitCaster.AddChannelObject(unitCaster.GetGUID());

            Creature creatureCaster = unitCaster.ToCreature();
            if (creatureCaster != null)
                if (unitCaster.m_unitData.ChannelObjects.Size() == 1 && unitCaster.m_unitData.ChannelObjects[0].IsUnit())
                    if (creatureCaster.HasSpellFocus(this))
                        creatureCaster.SetSpellFocus(this, Global.ObjAccessor.GetWorldObject(creatureCaster, unitCaster.m_unitData.ChannelObjects[0]));

            unitCaster.SetChannelSpellId(m_spellInfo.Id);
            unitCaster.SetChannelVisual(m_SpellVisual);

            void setImmunitiesAndHealPrediction(ref SpellChannelStartInterruptImmunities? interruptImmunities, ref SpellTargetedHealPrediction? healPrediction)
            {
                uint schoolImmunityMask = unitCaster.GetSchoolImmunityMask();
                ulong mechanicImmunityMask = unitCaster.GetMechanicImmunityMask();

                if (schoolImmunityMask != 0 || mechanicImmunityMask != 0)
                {
                    SpellChannelStartInterruptImmunities immunities = new();
                    immunities.SchoolImmunities = (int)schoolImmunityMask;
                    immunities.Immunities = (int)mechanicImmunityMask;

                    interruptImmunities = immunities;
                }

                if (m_spellInfo.HasAttribute(SpellAttr8.HealPrediction) && m_caster.IsUnit())
                {

                    SpellTargetedHealPrediction prediction = new();
                    if (unitCaster.m_unitData.ChannelObjects.Size() == 1 && unitCaster.m_unitData.ChannelObjects[0].IsUnit())
                        prediction.TargetGUID = unitCaster.m_unitData.ChannelObjects[0];

                    UpdateSpellHealPrediction(prediction.Predict, true);

                    healPrediction = prediction;
                }
            };

            if (IsEmpowerSpell())
            {
                unitCaster.SetSpellEmpowerStage(0);

                SpellEmpowerStart spellEmpowerStart = new();
                spellEmpowerStart.CastID = m_castId;
                spellEmpowerStart.CasterGUID = unitCaster.GetGUID();
                spellEmpowerStart.SpellID = (int)m_spellInfo.Id;
                spellEmpowerStart.Visual = m_SpellVisual;
                spellEmpowerStart.EmpowerDuration = new TimeSpan(m_empower.StageDurations.Sum(r => r.Ticks));
                spellEmpowerStart.MinHoldTime = m_empower.MinHoldTime;
                spellEmpowerStart.HoldAtMaxTime = TimeSpan.FromMilliseconds(SpellConst.EmpowerHoldTimeAtMax);
                spellEmpowerStart.Targets.AddRange(unitCaster.m_unitData.ChannelObjects._values);
                spellEmpowerStart.StageDurations.AddRange(m_empower.StageDurations);
                setImmunitiesAndHealPrediction(ref spellEmpowerStart.InterruptImmunities, ref spellEmpowerStart.HealPrediction);

                unitCaster.SendMessageToSet(spellEmpowerStart, true);
            }
            else
            {
                SpellChannelStart spellChannelStart = new();
                spellChannelStart.CasterGUID = unitCaster.GetGUID();
                spellChannelStart.SpellID = (int)m_spellInfo.Id;
                spellChannelStart.Visual = m_SpellVisual;
                spellChannelStart.ChannelDuration = duration;
                setImmunitiesAndHealPrediction(ref spellChannelStart.InterruptImmunities, ref spellChannelStart.HealPrediction);

                unitCaster.SendMessageToSet(spellChannelStart, true);
            }
        }

        void SendResurrectRequest(Player target)
        {
            // get resurrector name for creature resurrections, otherwise packet will be not accepted
            // for player resurrections the name is looked up by guid
            string sentName = "";
            if (!m_caster.IsPlayer())
                sentName = m_caster.GetName(target.GetSession().GetSessionDbLocaleIndex());

            ResurrectRequest resurrectRequest = new();
            resurrectRequest.ResurrectOffererGUID = m_caster.GetGUID();
            Player playerCaster = m_caster.ToPlayer();
            if (playerCaster != null)
                resurrectRequest.ResurrectOffererVirtualRealmAddress = playerCaster.m_playerData.VirtualPlayerRealm;
            else
                resurrectRequest.ResurrectOffererVirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();

            resurrectRequest.Name = sentName;
            resurrectRequest.Sickness = m_caster.IsUnit() && !m_caster.IsTypeId(TypeId.Player); // "you'll be afflicted with resurrection sickness"
            resurrectRequest.UseTimer = !m_spellInfo.HasAttribute(SpellAttr3.NoResTimer);

            Pet pet = target.GetPet();
            if (pet != null)
            {
                CharmInfo charmInfo = pet.GetCharmInfo();
                if (charmInfo != null)
                    resurrectRequest.PetNumber = charmInfo.GetPetNumber();
            }

            resurrectRequest.SpellID = m_spellInfo.Id;

            target.SendPacket(resurrectRequest);
        }

        void TakeCastItem()
        {
            if (m_CastItem == null || !m_caster.IsTypeId(TypeId.Player))
                return;

            // not remove cast item at triggered spell (equipping, weapon damage, etc)
            if (Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreCastItem))
                return;

            ItemTemplate proto = m_CastItem.GetTemplate();
            if (proto == null)
            {
                // This code is to avoid a crash
                // I'm not sure, if this is really an error, but I guess every item needs a prototype
                Log.outError(LogFilter.Spells, "Cast item has no item prototype {0}", m_CastItem.GetGUID().ToString());
                return;
            }

            bool expendable = false;
            bool withoutCharges = false;

            foreach (ItemEffectRecord itemEffect in m_CastItem.GetEffects())
            {
                if (itemEffect.LegacySlotIndex >= m_CastItem.m_itemData.SpellCharges.GetSize())
                    continue;

                // item has limited charges
                if (itemEffect.Charges != 0)
                {
                    if (itemEffect.Charges < 0)
                        expendable = true;

                    int charges = m_CastItem.GetSpellCharges(itemEffect.LegacySlotIndex);

                    // item has charges left for this slot
                    if (charges != 0 && itemEffect.SpellID == m_spellInfo.Id)
                    {
                        if (charges > 0)
                            --charges;
                        else
                            ++charges;

                        if (proto.GetMaxStackSize() == 1)
                            m_CastItem.SetSpellCharges(itemEffect.LegacySlotIndex, charges);
                        m_CastItem.SetState(ItemUpdateState.Changed, m_caster.ToPlayer());
                    }

                    // all charges used
                    withoutCharges = (charges == 0);
                }
            }

            if (expendable && withoutCharges)
            {
                uint count = 1;
                m_caster.ToPlayer().DestroyItemCount(m_CastItem, ref count, true);

                // prevent crash at access to deleted m_targets.GetItemTarget
                if (m_CastItem == m_targets.GetItemTarget())
                    m_targets.SetItemTarget(null);

                m_CastItem = null;
                m_castItemGUID.Clear();
                m_castItemEntry = 0;
            }
        }

        void TakePower()
        {
            // GameObjects don't use power
            Unit unitCaster = m_caster.ToUnit();
            if (unitCaster == null)
                return;

            if (m_CastItem != null || m_triggeredByAuraSpell != null)
                return;

            //Don't take power if the spell is cast while .cheat power is enabled.
            if (unitCaster.IsTypeId(TypeId.Player))
            {
                if (unitCaster.ToPlayer().GetCommandStatus(PlayerCommandStates.Power))
                    return;
            }

            bool hit = true;
            if (unitCaster.IsPlayer())
            {
                if (m_spellInfo.HasAttribute(SpellAttr1.DiscountPowerOnMiss))
                {
                    ObjectGuid targetGUID = m_targets.GetUnitTargetGUID();
                    if (!targetGUID.IsEmpty())
                        hit = m_UniqueTargetInfo.Any(targetInfo => targetInfo.TargetGUID == targetGUID && targetInfo.MissCondition == SpellMissInfo.None);
                }
            }

            foreach (SpellPowerCost cost in m_powerCost)
            {
                if (!hit)
                {
                    //lower spell cost on fail (by talent aura)
                    Player modOwner = unitCaster.GetSpellModOwner();
                    if (modOwner != null)
                        modOwner.ApplySpellMod(m_spellInfo, SpellModOp.PowerCostOnMiss, ref cost.Amount);
                }

                if (cost.Power == PowerType.Runes)
                {
                    TakeRunePower(hit);
                    continue;
                }

                if (cost.Amount == 0)
                    continue;

                // health as power used
                if (cost.Power == PowerType.Health)
                {
                    unitCaster.ModifyHealth(-cost.Amount);
                    continue;
                }

                unitCaster.ModifyPower(cost.Power, -cost.Amount);
            }
        }

        void RefundPower()
        {
            // GameObjects don't use power
            Unit unitCaster = m_caster.ToUnit();
            if (unitCaster == null)
                return;

            if (m_CastItem != null || m_triggeredByAuraSpell != null)
                return;

            //Don't take power if the spell is cast while .cheat power is enabled.
            if (unitCaster.IsPlayer())
            {
                if (unitCaster.ToPlayer().GetCommandStatus(PlayerCommandStates.Power))
                    return;
            }

            foreach (SpellPowerCost cost in m_powerCost)
            {
                if (cost.Power == PowerType.Runes)
                {
                    RefundRunePower();
                    continue;
                }

                if (cost.Amount == 0)
                    continue;

                // health as power used
                if (cost.Power == PowerType.Health)
                {
                    unitCaster.ModifyHealth(cost.Amount);
                    continue;
                }

                unitCaster.ModifyPower(cost.Power, cost.Amount);
            }
        }

        SpellCastResult CheckRuneCost()
        {
            int runeCost = m_powerCost.Sum(cost => cost.Power == PowerType.Runes ? cost.Amount : 0);
            if (runeCost == 0)
                return SpellCastResult.SpellCastOk;

            Player player = m_caster.ToPlayer();
            if (player == null)
                return SpellCastResult.SpellCastOk;

            if (player.GetClass() != Class.Deathknight)
                return SpellCastResult.SpellCastOk;

            int readyRunes = 0;
            for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
                if (player.GetRuneCooldown(i) == 0)
                    ++readyRunes;

            if (readyRunes < runeCost)
                return SpellCastResult.NoPower;                       // not sure if result code is correct

            return SpellCastResult.SpellCastOk;
        }

        void TakeRunePower(bool didHit)
        {
            if (!m_caster.IsTypeId(TypeId.Player) || m_caster.ToPlayer().GetClass() != Class.Deathknight)
                return;

            Player player = m_caster.ToPlayer();
            m_runesState = player.GetRunesState();                 // store previous state

            int runeCost = m_powerCost.Sum(cost => cost.Power == PowerType.Runes ? cost.Amount : 0);
            for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
            {
                if (player.GetRuneCooldown(i) == 0 && runeCost > 0)
                {
                    player.SetRuneCooldown(i, didHit ? player.GetRuneBaseCooldown() : RuneCooldowns.Miss);
                    --runeCost;
                }
            }
        }

        void RefundRunePower()
        {
            if (!m_caster.IsPlayer() || m_caster.ToPlayer().GetClass() != Class.Deathknight)
                return;

            Player player = m_caster.ToPlayer();

            // restore old rune state
            for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
                if ((m_runesState & (1 << i)) != 0)
                    player.SetRuneCooldown(i, 0);
        }

        void TakeReagents()
        {
            if (!m_caster.IsTypeId(TypeId.Player))
                return;

            // do not take reagents for these item casts
            if (m_CastItem != null && m_CastItem.GetTemplate().HasFlag(ItemFlags.NoReagentCost))
                return;

            Player p_caster = m_caster.ToPlayer();
            if (p_caster.CanNoReagentCast(m_spellInfo))
                return;

            for (int x = 0; x < SpellConst.MaxReagents; ++x)
            {
                if (m_spellInfo.Reagent[x] <= 0)
                    continue;

                uint itemid = (uint)m_spellInfo.Reagent[x];
                uint itemcount = m_spellInfo.ReagentCount[x];

                // if CastItem is also spell reagent
                if (m_CastItem != null && m_CastItem.GetEntry() == itemid)
                {
                    foreach (ItemEffectRecord itemEffect in m_CastItem.GetEffects())
                    {
                        if (itemEffect.LegacySlotIndex >= m_CastItem.m_itemData.SpellCharges.GetSize())
                            continue;

                        // CastItem will be used up and does not count as reagent
                        int charges = m_CastItem.GetSpellCharges(itemEffect.LegacySlotIndex);
                        if (itemEffect.Charges < 0 && Math.Abs(charges) < 2)
                        {
                            ++itemcount;
                            break;
                        }
                    }

                    m_CastItem = null;
                    m_castItemGUID.Clear();
                    m_castItemEntry = 0;
                }

                // if GetItemTarget is also spell reagent
                if (m_targets.GetItemTargetEntry() == itemid)
                    m_targets.SetItemTarget(null);

                p_caster.DestroyItemCount(itemid, itemcount, true);
            }

            foreach (var reagentsCurrency in m_spellInfo.ReagentsCurrency)
                p_caster.RemoveCurrency(reagentsCurrency.CurrencyTypesID, -reagentsCurrency.CurrencyCount, CurrencyDestroyReason.Spell);
        }

        void HandleThreatSpells()
        {
            // wild GameObject spells don't cause threat
            Unit unitCaster = (m_originalCaster != null ? m_originalCaster : m_caster.ToUnit());
            if (unitCaster == null)
                return;

            if (m_UniqueTargetInfo.Empty())
                return;

            if (!m_spellInfo.HasInitialAggro())
                return;

            float threat = 0.0f;
            SpellThreatEntry threatEntry = Global.SpellMgr.GetSpellThreatEntry(m_spellInfo.Id);
            if (threatEntry != null)
            {
                if (threatEntry.apPctMod != 0.0f)
                    threat += threatEntry.apPctMod * unitCaster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack);

                threat += threatEntry.flatMod;
            }
            else if (!m_spellInfo.HasAttribute(SpellCustomAttributes.NoInitialThreat))
                threat += m_spellInfo.SpellLevel;

            // past this point only multiplicative effects occur
            if (threat == 0.0f)
                return;

            // since 2.0.1 threat from positive effects also is distributed among all targets, so the overall caused threat is at most the defined bonus
            threat /= m_UniqueTargetInfo.Count;

            foreach (var ihit in m_UniqueTargetInfo)
            {
                float threatToAdd = threat;
                if (ihit.MissCondition != SpellMissInfo.None)
                    threatToAdd = 0.0f;

                Unit target = Global.ObjAccessor.GetUnit(unitCaster, ihit.TargetGUID);
                if (target == null)
                    continue;

                // positive spells distribute threat among all units that are in combat with target, like healing
                if (IsPositive())
                    target.GetThreatManager().ForwardThreatForAssistingMe(unitCaster, threatToAdd, m_spellInfo);
                // for negative spells threat gets distributed among affected targets
                else
                {
                    if (!target.CanHaveThreatList())
                        continue;

                    target.GetThreatManager().AddThreat(unitCaster, threatToAdd, m_spellInfo, true);
                }
            }
            Log.outDebug(LogFilter.Spells, "Spell {0}, added an additional {1} threat for {2} {3} target(s)", m_spellInfo.Id, threat, IsPositive() ? "assisting" : "harming", m_UniqueTargetInfo.Count);
        }

        public void HandleEffects(Unit pUnitTarget, Item pItemTarget, GameObject pGoTarget, Corpse pCorpseTarget, SpellEffectInfo spellEffectInfo, SpellEffectHandleMode mode)
        {
            effectHandleMode = mode;
            unitTarget = pUnitTarget;
            itemTarget = pItemTarget;
            gameObjTarget = pGoTarget;
            corpseTarget = pCorpseTarget;
            destTarget = m_destTargets[spellEffectInfo.EffectIndex].Position;
            effectInfo = spellEffectInfo;

            damage = CalculateDamage(spellEffectInfo, unitTarget, out variance);

            bool preventDefault = CallScriptEffectHandlers(spellEffectInfo.EffectIndex, mode);

            if (!preventDefault)
                Global.SpellMgr.GetSpellEffectHandler(spellEffectInfo.Effect).Invoke(this);
        }

        public static Spell ExtractSpellFromEvent(BasicEvent basicEvent)
        {
            SpellEvent spellEvent = (SpellEvent)basicEvent;
            if (spellEvent != null)
                return spellEvent.GetSpell();

            return null;
        }

        public SpellCastResult CheckCast(bool strict)
        {
            int param1 = 0, param2 = 0;
            return CheckCast(strict, ref param1, ref param2);
        }

        public SpellCastResult CheckCast(bool strict, ref int param1, ref int param2)
        {
            SpellCastResult castResult;

            // check death state
            if (m_caster.ToUnit() != null && !m_caster.ToUnit().IsAlive() && !m_spellInfo.IsPassive() && !(m_spellInfo.HasAttribute(SpellAttr0.AllowCastWhileDead) || (IsTriggered() && m_triggeredByAuraSpell == null)))
                return SpellCastResult.CasterDead;

            // Prevent cheating in case the player has an immunity effect and tries to interact with a non-allowed gameobject. The error message is handled by the client so we don't report anything here
            if (m_caster.IsPlayer() && m_targets.GetGOTarget() != null)
            {
                if (m_targets.GetGOTarget().GetGoInfo().GetNoDamageImmune() != 0 && m_caster.ToUnit().HasUnitFlag(UnitFlags.Immune))
                    return SpellCastResult.DontReport;
            }

            // check cooldowns to prevent cheating
            if (!m_spellInfo.IsPassive())
            {
                Player playerCaster = m_caster.ToPlayer();
                if (playerCaster != null)
                {
                    //can cast triggered (by aura only?) spells while have this flag
                    if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreCasterAurastate))
                    {
                        // These two auras check SpellFamilyName defined by db2 class data instead of current spell SpellFamilyName
                        if (playerCaster.HasAuraType(AuraType.DisableCastingExceptAbilities)
                            && !m_spellInfo.HasAttribute(SpellAttr0.UsesRangedSlot)
                            && !m_spellInfo.HasEffect(SpellEffectName.Attack)
                            && !m_spellInfo.HasAttribute(SpellAttr12.IgnoreCastingDisabled)
                            && !playerCaster.HasAuraTypeWithFamilyFlags(AuraType.DisableCastingExceptAbilities, CliDB.ChrClassesStorage.LookupByKey(playerCaster.GetClass()).SpellClassSet, m_spellInfo.SpellFamilyFlags))
                            return SpellCastResult.CantDoThatRightNow;

                        if (playerCaster.HasAuraType(AuraType.DisableAttackingExceptAbilities))
                        {
                            if (!playerCaster.HasAuraTypeWithFamilyFlags(AuraType.DisableAttackingExceptAbilities, CliDB.ChrClassesStorage.LookupByKey(playerCaster.GetClass()).SpellClassSet, m_spellInfo.SpellFamilyFlags))
                            {
                                if (m_spellInfo.HasAttribute(SpellAttr0.UsesRangedSlot)
                                    || m_spellInfo.IsNextMeleeSwingSpell()
                                    || m_spellInfo.HasAttribute(SpellAttr1.InitiatesCombatEnablesAutoAttack)
                                    || m_spellInfo.HasAttribute(SpellAttr2.InitiateCombatPostCastEnablesAutoAttack)
                                    || m_spellInfo.HasEffect(SpellEffectName.Attack)
                                    || m_spellInfo.HasEffect(SpellEffectName.NormalizedWeaponDmg)
                                    || m_spellInfo.HasEffect(SpellEffectName.WeaponDamageNoSchool)
                                    || m_spellInfo.HasEffect(SpellEffectName.WeaponPercentDamage)
                                    || m_spellInfo.HasEffect(SpellEffectName.WeaponDamage))
                                    return SpellCastResult.CantDoThatRightNow;
                            }
                        }
                    }

                    // check if we are using a potion in combat for the 2nd+ time. Cooldown is added only after caster gets out of combat
                    if (!IsIgnoringCooldowns() && playerCaster.GetLastPotionId() != 0 && m_CastItem != null && (m_CastItem.IsPotion() || m_spellInfo.IsCooldownStartedOnEvent()))
                        return SpellCastResult.NotReady;
                }

                if (!IsIgnoringCooldowns() && m_caster.ToUnit() != null && (!m_spellInfo.HasAttribute(SpellAttr12.StartCooldownOnCastStart) || strict))
                {
                    if (!m_caster.ToUnit().GetSpellHistory().IsReady(m_spellInfo, m_castItemEntry))
                    {
                        if (m_triggeredByAuraSpell != null || m_spellInfo.IsCooldownStartedOnEvent())
                            return SpellCastResult.DontReport;
                        else
                            return SpellCastResult.NotReady;
                    }

                    if ((IsAutoRepeat() || m_spellInfo.CategoryId == 76) && !m_caster.ToUnit().IsAttackReady(WeaponAttackType.RangedAttack))
                        return SpellCastResult.DontReport;
                }
            }

            if (m_spellInfo.HasAttribute(SpellAttr7.DebugSpell) && m_caster.IsUnit() && !m_caster.ToUnit().HasUnitFlag2(UnitFlags2.AllowCheatSpells))
            {
                m_customError = SpellCustomErrors.GmOnly;
                return SpellCastResult.CustomError;
            }

            if (m_spellInfo.HasAttribute(SpellAttr8.OnlyPlayersCanCastThisSpell) && !m_caster.IsPlayer())
                return SpellCastResult.CasterAurastate;

            // Check global cooldown
            if (strict && !Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreGCD) && HasGlobalCooldown())
                return !m_spellInfo.HasAttribute(SpellAttr0.CooldownOnEvent) ? SpellCastResult.NotReady : SpellCastResult.DontReport;

            // only triggered spells can be processed an ended Battleground
            if (!IsTriggered() && m_caster.IsTypeId(TypeId.Player))
            {
                Battleground bg = m_caster.ToPlayer().GetBattleground();
                if (bg != null)
                    if (bg.GetStatus() == BattlegroundStatus.WaitLeave)
                        return SpellCastResult.DontReport;
            }

            if (m_caster.IsTypeId(TypeId.Player) && Global.VMapMgr.IsLineOfSightCalcEnabled())
            {
                if (m_spellInfo.HasAttribute(SpellAttr0.OnlyOutdoors) && !m_caster.IsOutdoors())
                    return SpellCastResult.OnlyOutdoors;

                if (m_spellInfo.HasAttribute(SpellAttr0.OnlyIndoors) && m_caster.IsOutdoors())
                    return SpellCastResult.OnlyIndoors;
            }

            Unit unitCaster = m_caster.ToUnit();
            if (unitCaster != null)
            {
                if (m_spellInfo.HasAttribute(SpellAttr5.NotAvailableWhileCharmed) && unitCaster.IsCharmed())
                    return SpellCastResult.Charmed;

                // only check at first call, Stealth auras are already removed at second call
                // for now, ignore triggered spells
                if (strict && !_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreShapeshift))
                {
                    bool checkForm = true;
                    // Ignore form req aura
                    var ignore = unitCaster.GetAuraEffectsByType(AuraType.ModIgnoreShapeshift);
                    foreach (var aurEff in ignore)
                    {
                        if (!aurEff.IsAffectingSpell(m_spellInfo))
                            continue;

                        checkForm = false;
                        break;
                    }

                    if (checkForm)
                    {
                        // Cannot be used in this stance/form
                        SpellCastResult shapeError = m_spellInfo.CheckShapeshift(unitCaster.GetShapeshiftForm());
                        if (shapeError != SpellCastResult.SpellCastOk)
                            return shapeError;

                        if (m_spellInfo.HasAttribute(SpellAttr0.OnlyStealthed) && !unitCaster.HasStealthAura())
                            return SpellCastResult.OnlyStealthed;
                    }
                }

                // caster state requirements
                // not for triggered spells (needed by execute)
                if (!_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreCasterAurastate))
                {
                    if (m_spellInfo.CasterAuraState != 0 && !unitCaster.HasAuraState(m_spellInfo.CasterAuraState, m_spellInfo, unitCaster))
                        return SpellCastResult.CasterAurastate;
                    if (m_spellInfo.ExcludeCasterAuraState != 0 && unitCaster.HasAuraState(m_spellInfo.ExcludeCasterAuraState, m_spellInfo, unitCaster))
                        return SpellCastResult.CasterAurastate;

                    // Note: spell 62473 requres casterAuraSpell = triggering spell
                    if (m_spellInfo.CasterAuraSpell != 0 && !unitCaster.HasAura(m_spellInfo.CasterAuraSpell))
                        return SpellCastResult.CasterAurastate;
                    if (m_spellInfo.ExcludeCasterAuraSpell != 0 && unitCaster.HasAura(m_spellInfo.ExcludeCasterAuraSpell))
                        return SpellCastResult.CasterAurastate;

                    if (m_spellInfo.CasterAuraType != 0 && !unitCaster.HasAuraType(m_spellInfo.CasterAuraType))
                        return SpellCastResult.CasterAurastate;
                    if (m_spellInfo.ExcludeCasterAuraType != 0 && unitCaster.HasAuraType(m_spellInfo.ExcludeCasterAuraType))
                        return SpellCastResult.CasterAurastate;

                    if (unitCaster.IsInCombat() && !m_spellInfo.CanBeUsedInCombat(unitCaster))
                        return SpellCastResult.AffectingCombat;

                    if (m_spellInfo.HasAttribute(SpellAttr9.OnlyWhenIllegallyMounted))
                    {
                        bool hasInvalidMountAura = unitCaster.GetAuraEffectsByType(AuraType.Mounted).Any(mountEffect =>
                        {
                            uint mountType = (uint)mountEffect.GetSpellEffectInfo().MiscValueB;
                            var mountEntry = Global.DB2Mgr.GetMount(mountEffect.GetId());
                            if (mountEntry != null)
                                mountType = mountEntry.MountTypeID;

                            var mountCapability = unitCaster.GetMountCapability(mountType);
                            return mountCapability == null || mountCapability.Id != mountEffect.GetAmount();
                        });

                        if (!hasInvalidMountAura)
                            return SpellCastResult.OnlyMounted;
                    }
                }

                // Check vehicle flags
                if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreCasterMountedOrOnVehicle))
                {
                    SpellCastResult vehicleCheck = m_spellInfo.CheckVehicle(unitCaster);
                    if (vehicleCheck != SpellCastResult.SpellCastOk)
                        return vehicleCheck;
                }
            }

            // check spell cast conditions from database
            {
                ConditionSourceInfo condInfo = new(m_caster, m_targets.GetObjectTarget());
                if (!Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.Spell, m_spellInfo.Id, condInfo))
                {
                    // mLastFailedCondition can be NULL if there was an error processing the condition in Condition.Meets (i.e. wrong data for ConditionTarget or others)
                    if (condInfo.mLastFailedCondition != null && condInfo.mLastFailedCondition.ErrorType != 0)
                    {
                        if (condInfo.mLastFailedCondition.ErrorType == (uint)SpellCastResult.CustomError)
                            m_customError = (SpellCustomErrors)condInfo.mLastFailedCondition.ErrorTextId;
                        return (SpellCastResult)condInfo.mLastFailedCondition.ErrorType;
                    }

                    if (condInfo.mLastFailedCondition == null || condInfo.mLastFailedCondition.ConditionTarget == 0)
                        return SpellCastResult.CasterAurastate;
                    return SpellCastResult.BadTargets;
                }
            }

            // Don't check explicit target for passive spells (workaround) (check should be skipped only for learn case)
            // those spells may have incorrect target entries or not filled at all (for example 15332)
            // such spells when learned are not targeting anyone using targeting system, they should apply directly to caster instead
            // also, such casts shouldn't be sent to client
            if (!(m_spellInfo.IsPassive() && (m_targets.GetUnitTarget() == null || m_targets.GetUnitTarget() == m_caster)))
            {
                // Check explicit target for m_originalCaster - todo: get rid of such workarounds
                WorldObject caster = m_caster;
                // in case of gameobjects like traps, we need the gameobject itself to check target validity
                // otherwise, if originalCaster is far away and cannot detect the target, the trap would not hit the target
                if (m_originalCaster != null && !caster.IsGameObject())
                    caster = m_originalCaster;

                castResult = m_spellInfo.CheckExplicitTarget(caster, m_targets.GetObjectTarget(), m_targets.GetItemTarget());
                if (castResult != SpellCastResult.SpellCastOk)
                    return castResult;
            }

            Unit unitTarget = m_targets.GetUnitTarget();
            if (unitTarget != null)
            {
                castResult = m_spellInfo.CheckTarget(m_caster, unitTarget, m_caster.IsGameObject()); // skip stealth checks for GO casts
                if (castResult != SpellCastResult.SpellCastOk)
                    return castResult;

                // If it's not a melee spell, check if vision is obscured by SPELL_AURA_INTERFERE_TARGETTING
                if (m_spellInfo.DmgClass != SpellDmgClass.Melee)
                {
                    Unit unitCaster1 = m_caster.ToUnit();
                    if (unitCaster1 != null)
                    {
                        foreach (var auraEffect in unitCaster1.GetAuraEffectsByType(AuraType.InterfereTargetting))
                            if (!unitCaster1.IsFriendlyTo(auraEffect.GetCaster()) && !unitTarget.HasAura(auraEffect.GetId(), auraEffect.GetCasterGUID()))
                                return SpellCastResult.VisionObscured;

                        foreach (var auraEffect in unitTarget.GetAuraEffectsByType(AuraType.InterfereTargetting))
                            if (!unitCaster1.IsFriendlyTo(auraEffect.GetCaster()) && (!unitTarget.HasAura(auraEffect.GetId(), auraEffect.GetCasterGUID()) || !unitCaster1.HasAura(auraEffect.GetId(), auraEffect.GetCasterGUID())))
                                return SpellCastResult.VisionObscured;
                    }
                }

                if (unitTarget != m_caster)
                {
                    // Must be behind the target
                    if (m_spellInfo.HasAttribute(SpellCustomAttributes.ReqCasterBehindTarget) && unitTarget.HasInArc(MathFunctions.PI, m_caster))
                        return SpellCastResult.NotBehind;

                    // Target must be facing you
                    if (m_spellInfo.HasAttribute(SpellCustomAttributes.ReqTargetFacingCaster) && !unitTarget.HasInArc(MathFunctions.PI, m_caster))
                        return SpellCastResult.NotInfront;

                    // Ignore LOS for gameobjects casts
                    if (!m_caster.IsGameObject())
                    {
                        WorldObject losTarget = m_caster;
                        if (IsTriggered() && m_triggeredByAuraSpell != null)
                        {
                            DynamicObject dynObj = m_caster.ToUnit().GetDynObject(m_triggeredByAuraSpell.Id);
                            if (dynObj != null)
                                losTarget = dynObj;
                        }

                        if (!IsWithinLOS(losTarget, unitTarget, true, ModelIgnoreFlags.M2))
                            return SpellCastResult.LineOfSight;
                    }
                }
            }

            // Check for line of sight for spells with dest
            if (m_targets.HasDst())
                if (!IsWithinLOS(m_caster, m_targets.GetDstPos(), ModelIgnoreFlags.M2))
                    return SpellCastResult.LineOfSight;

            // check pet presence
            if (unitCaster != null)
            {
                if (m_spellInfo.HasAttribute(SpellAttr2.NoActivePets))
                    if (!unitCaster.GetPetGUID().IsEmpty())
                        return SpellCastResult.AlreadyHavePet;

                foreach (var spellEffectInfo in m_spellInfo.GetEffects())
                {
                    if (spellEffectInfo.TargetA.GetTarget() == Targets.UnitPet)
                    {
                        if (unitCaster.GetGuardianPet() == null)
                        {
                            if (m_triggeredByAuraSpell != null)              // not report pet not existence for triggered spells
                                return SpellCastResult.DontReport;
                            else
                                return SpellCastResult.NoPet;
                        }
                        break;
                    }
                }
            }

            // Spell casted only on Battleground
            if (m_spellInfo.HasAttribute(SpellAttr3.OnlyBattlegrounds))
                if (!m_caster.GetMap().IsBattleground())
                    return SpellCastResult.OnlyBattlegrounds;

            // do not allow spells to be cast in arenas or rated Battlegrounds
            Player player = m_caster.ToPlayer();
            if (player != null)
                if (player.InArena()/* || player.InRatedBattleground() NYI*/)
                {
                    castResult = CheckArenaAndRatedBattlegroundCastRules();
                    if (castResult != SpellCastResult.SpellCastOk)
                        return castResult;
                }

            // zone check
            if (!m_caster.IsPlayer() || !m_caster.ToPlayer().IsGameMaster())
            {
                uint zone, area;
                m_caster.GetZoneAndAreaId(out zone, out area);

                SpellCastResult locRes = m_spellInfo.CheckLocation(m_caster.GetMapId(), zone, area, m_caster.ToPlayer());
                if (locRes != SpellCastResult.SpellCastOk)
                    return locRes;
            }

            // not let players cast spells at mount (and let do it to creatures)
            if (!_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreCasterMountedOrOnVehicle))
            {
                if (m_caster.IsPlayer() && m_caster.ToPlayer().IsMounted() && !m_spellInfo.IsPassive() && !m_spellInfo.HasAttribute(SpellAttr0.AllowWhileMounted))
                {
                    if (m_caster.ToPlayer().IsInFlight())
                        return SpellCastResult.NotOnTaxi;
                    else
                        return SpellCastResult.NotMounted;
                }
            }

            // check spell focus object
            if (m_spellInfo.RequiresSpellFocus != 0)
            {
                if (!m_caster.IsUnit() || !m_caster.ToUnit().HasAuraTypeWithMiscvalue(AuraType.ProvideSpellFocus, (int)m_spellInfo.RequiresSpellFocus))
                {
                    focusObject = SearchSpellFocus();
                    if (focusObject == null)
                        return SpellCastResult.RequiresSpellFocus;
                }
            }

            // always (except passive spells) check items (focus object can be required for any type casts)
            if (!m_spellInfo.IsPassive())
            {
                castResult = CheckItems(ref param1, ref param2);
                if (castResult != SpellCastResult.SpellCastOk)
                    return castResult;
            }

            // Triggered spells also have range check
            // @todo determine if there is some flag to enable/disable the check
            castResult = CheckRange(strict);
            if (castResult != SpellCastResult.SpellCastOk)
                return castResult;

            if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnorePowerCost))
            {
                castResult = CheckPower();
                if (castResult != SpellCastResult.SpellCastOk)
                    return castResult;
            }

            if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreCasterAuras))
            {
                castResult = CheckCasterAuras(ref param1);
                if (castResult != SpellCastResult.SpellCastOk)
                    return castResult;
            }

            // script hook
            castResult = CallScriptCheckCastHandlers();
            if (castResult != SpellCastResult.SpellCastOk)
                return castResult;

            uint approximateAuraEffectMask = 0;
            uint nonAuraEffectMask = 0;
            foreach (var spellEffectInfo in m_spellInfo.GetEffects())
            {
                // for effects of spells that have only one target
                switch (spellEffectInfo.Effect)
                {
                    case SpellEffectName.Dummy:
                    {
                        if (m_spellInfo.Id == 19938)          // Awaken Peon
                        {
                            Unit unit = m_targets.GetUnitTarget();
                            if (unit == null || !unit.HasAura(17743))
                                return SpellCastResult.BadTargets;
                        }
                        else if (m_spellInfo.Id == 31789)          // Righteous Defense
                        {
                            if (!m_caster.IsTypeId(TypeId.Player))
                                return SpellCastResult.DontReport;

                            Unit target = m_targets.GetUnitTarget();
                            if (target == null || !target.IsFriendlyTo(m_caster) || target.GetAttackers().Empty())
                                return SpellCastResult.BadTargets;

                        }
                        break;
                    }
                    case SpellEffectName.LearnSpell:
                    {
                        if (spellEffectInfo.TargetA.GetTarget() != Targets.UnitPet)
                            break;

                        Pet pet = m_caster.ToPlayer().GetPet();
                        if (pet == null)
                            return SpellCastResult.NoPet;

                        SpellInfo learn_spellproto = Global.SpellMgr.GetSpellInfo(spellEffectInfo.TriggerSpell, Difficulty.None);

                        if (learn_spellproto == null)
                            return SpellCastResult.NotKnown;

                        if (m_spellInfo.SpellLevel > pet.GetLevel())
                            return SpellCastResult.Lowlevel;

                        break;
                    }
                    case SpellEffectName.UnlockGuildVaultTab:
                    {
                        if (!m_caster.IsTypeId(TypeId.Player))
                            return SpellCastResult.BadTargets;
                        var guild = m_caster.ToPlayer().GetGuild();
                        if (guild != null)
                            if (guild.GetLeaderGUID() != m_caster.ToPlayer().GetGUID())
                                return SpellCastResult.CantDoThatRightNow;
                        break;
                    }
                    case SpellEffectName.LearnPetSpell:
                    {
                        // check target only for unit target case
                        Unit target = m_targets.GetUnitTarget();
                        if (target != null)
                        {
                            if (!m_caster.IsTypeId(TypeId.Player))
                                return SpellCastResult.BadTargets;

                            Pet pet = target.ToPet();
                            if (pet == null || pet.GetOwner() != m_caster)
                                return SpellCastResult.BadTargets;

                            SpellInfo learn_spellproto = Global.SpellMgr.GetSpellInfo(spellEffectInfo.TriggerSpell, Difficulty.None);
                            if (learn_spellproto == null)
                                return SpellCastResult.NotKnown;

                            if (m_spellInfo.SpellLevel > pet.GetLevel())
                                return SpellCastResult.Lowlevel;
                        }
                        break;
                    }
                    case SpellEffectName.ApplyGlyph:
                    {
                        if (!m_caster.IsTypeId(TypeId.Player))
                            return SpellCastResult.GlyphNoSpec;

                        Player caster = m_caster.ToPlayer();
                        if (!caster.HasSpell(m_misc.SpellId))
                            return SpellCastResult.NotKnown;

                        uint glyphId = (uint)spellEffectInfo.MiscValue;
                        if (glyphId != 0)
                        {
                            GlyphPropertiesRecord glyphProperties = CliDB.GlyphPropertiesStorage.LookupByKey(glyphId);
                            if (glyphProperties == null)
                                return SpellCastResult.InvalidGlyph;

                            List<uint> glyphBindableSpells = Global.DB2Mgr.GetGlyphBindableSpells(glyphId);
                            if (glyphBindableSpells.Empty())
                                return SpellCastResult.InvalidGlyph;

                            if (!glyphBindableSpells.Contains(m_misc.SpellId))
                                return SpellCastResult.InvalidGlyph;

                            List<ChrSpecialization> glyphRequiredSpecs = Global.DB2Mgr.GetGlyphRequiredSpecs(glyphId);
                            if (!glyphRequiredSpecs.Empty())
                            {
                                if (caster.GetPrimarySpecialization() == ChrSpecialization.None)
                                    return SpellCastResult.GlyphNoSpec;

                                if (!glyphRequiredSpecs.Contains(caster.GetPrimarySpecialization()))
                                    return SpellCastResult.GlyphInvalidSpec;
                            }

                            uint replacedGlyph = 0;
                            foreach (uint activeGlyphId in caster.GetGlyphs(caster.GetActiveTalentGroup()))
                            {
                                List<uint> activeGlyphBindableSpells = Global.DB2Mgr.GetGlyphBindableSpells(activeGlyphId);
                                if (!activeGlyphBindableSpells.Empty())
                                {
                                    if (activeGlyphBindableSpells.Contains(m_misc.SpellId))
                                    {
                                        replacedGlyph = activeGlyphId;
                                        break;
                                    }
                                }
                            }

                            foreach (uint activeGlyphId in caster.GetGlyphs(caster.GetActiveTalentGroup()))
                            {
                                if (activeGlyphId == replacedGlyph)
                                    continue;

                                if (activeGlyphId == glyphId)
                                    return SpellCastResult.UniqueGlyph;

                                if (CliDB.GlyphPropertiesStorage.LookupByKey(activeGlyphId).GlyphExclusiveCategoryID == glyphProperties.GlyphExclusiveCategoryID)
                                    return SpellCastResult.GlyphExclusiveCategory;
                            }
                        }
                        break;
                    }
                    case SpellEffectName.FeedPet:
                    {
                        if (!m_caster.IsTypeId(TypeId.Player))
                            return SpellCastResult.BadTargets;

                        Item foodItem = m_targets.GetItemTarget();
                        if (foodItem == null)
                            return SpellCastResult.BadTargets;

                        Pet pet = m_caster.ToPlayer().GetPet();
                        if (pet == null)
                            return SpellCastResult.NoPet;

                        if (!pet.HaveInDiet(foodItem.GetTemplate()))
                            return SpellCastResult.WrongPetFood;

                        if (foodItem.GetTemplate().GetBaseItemLevel() + 30 <= pet.GetLevel())
                            return SpellCastResult.FoodLowlevel;

                        if (m_caster.ToPlayer().IsInCombat() || pet.IsInCombat())
                            return SpellCastResult.AffectingCombat;

                        break;
                    }
                    case SpellEffectName.Charge:
                    {
                        if (unitCaster == null)
                            return SpellCastResult.BadTargets;

                        if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreCasterAuras) && unitCaster.HasUnitState(UnitState.Root))
                            return SpellCastResult.Rooted;

                        if (GetSpellInfo().NeedsExplicitUnitTarget())
                        {
                            Unit target = m_targets.GetUnitTarget();
                            if (target == null)
                                return SpellCastResult.DontReport;

                            // first we must check to see if the target is in LoS. A path can usually be built but LoS matters for charge spells
                            if (!IsWithinLOS(unitCaster, target, true, ModelIgnoreFlags.Nothing)) //Do full LoS/Path check. Don't exclude m2
                                return SpellCastResult.LineOfSight;

                            float objSize = target.GetCombatReach();
                            float range = m_spellInfo.GetMaxRange(true, unitCaster, this) * 1.5f + objSize; // can't be overly strict

                            m_preGeneratedPath = new(unitCaster);
                            m_preGeneratedPath.SetPathLengthLimit(range);

                            // first try with raycast, if it fails fall back to normal path
                            bool result = m_preGeneratedPath.CalculatePath(target.GetPositionX(), target.GetPositionY(), target.GetPositionZ(), false);
                            if (m_preGeneratedPath.GetPathType().HasAnyFlag(PathType.Short))
                                return SpellCastResult.NoPath;
                            else if (!result || m_preGeneratedPath.GetPathType().HasAnyFlag(PathType.NoPath | PathType.Incomplete))
                                return SpellCastResult.NoPath;
                            else if (m_preGeneratedPath.IsInvalidDestinationZ(target)) // Check position z, if not in a straight line
                                return SpellCastResult.NoPath;

                            m_preGeneratedPath.ShortenPathUntilDist(target, objSize); //move back
                        }
                        break;
                    }
                    case SpellEffectName.Skinning:
                    {
                        if (!m_caster.IsTypeId(TypeId.Player) || m_targets.GetUnitTarget() == null || !m_targets.GetUnitTarget().IsTypeId(TypeId.Unit))
                            return SpellCastResult.BadTargets;

                        if (!m_targets.GetUnitTarget().HasUnitFlag(UnitFlags.Skinnable))
                            return SpellCastResult.TargetUnskinnable;

                        Creature creature = m_targets.GetUnitTarget().ToCreature();
                        Loot loot = creature.GetLootForPlayer(m_caster.ToPlayer());
                        if (loot != null && (!loot.IsLooted() || loot.loot_type == LootType.Skinning))
                            return SpellCastResult.TargetNotLooted;

                        break;
                    }
                    case SpellEffectName.OpenLock:
                    {
                        if (spellEffectInfo.TargetA.GetTarget() != Targets.GameobjectTarget &&
                            spellEffectInfo.TargetA.GetTarget() != Targets.GameobjectItemTarget)
                            break;

                        if (!m_caster.IsTypeId(TypeId.Player)  // only players can open locks, gather etc.
                                                               // we need a go target in case of TARGET_GAMEOBJECT_TARGET
                            || (spellEffectInfo.TargetA.GetTarget() == Targets.GameobjectTarget && m_targets.GetGOTarget() == null))
                            return SpellCastResult.BadTargets;

                        Item pTempItem = null;
                        if (Convert.ToBoolean(m_targets.GetTargetMask() & SpellCastTargetFlags.TradeItem))
                        {
                            TradeData pTrade = m_caster.ToPlayer().GetTradeData();
                            if (pTrade != null)
                                pTempItem = pTrade.GetTraderData().GetItem(TradeSlots.NonTraded);
                        }
                        else if (Convert.ToBoolean(m_targets.GetTargetMask() & SpellCastTargetFlags.Item))
                            pTempItem = m_caster.ToPlayer().GetItemByGuid(m_targets.GetItemTargetGUID());

                        // we need a go target, or an openable item target in case of TARGET_GAMEOBJECT_ITEM_TARGET
                        if (spellEffectInfo.TargetA.GetTarget() == Targets.GameobjectItemTarget &&
                            m_targets.GetGOTarget() == null && (pTempItem == null || pTempItem.GetTemplate().GetLockID() == 0 || !pTempItem.IsLocked()))
                            return SpellCastResult.BadTargets;

                        if (m_spellInfo.Id != 1842 || (m_targets.GetGOTarget() != null &&
                            m_targets.GetGOTarget().GetGoInfo().type != GameObjectTypes.Trap))
                            if (m_caster.ToPlayer().InBattleground() && // In Battlegroundplayers can use only flags and banners
                                !m_caster.ToPlayer().CanUseBattlegroundObject(m_targets.GetGOTarget()))
                                return SpellCastResult.TryAgain;

                        // get the lock entry
                        uint lockId = 0;
                        GameObject go = m_targets.GetGOTarget();
                        Item itm = m_targets.GetItemTarget();
                        if (go != null)
                        {
                            lockId = go.GetGoInfo().GetLockId();
                            if (lockId == 0)
                                return SpellCastResult.BadTargets;

                            if (go.GetGoInfo().GetNotInCombat() != 0 && m_caster.ToUnit().IsInCombat())
                                return SpellCastResult.AffectingCombat;
                        }
                        else if (itm != null)
                            lockId = itm.GetTemplate().GetLockID();

                        SkillType skillId = SkillType.None;
                        int reqSkillValue = 0;
                        int skillValue = 0;

                        // check lock compatibility
                        SpellCastResult res = CanOpenLock(spellEffectInfo, lockId, ref skillId, ref reqSkillValue, ref skillValue);
                        if (res != SpellCastResult.SpellCastOk)
                            return res;
                        break;
                    }
                    case SpellEffectName.ResurrectPet:
                    {
                        Player playerCaster = m_caster.ToPlayer();
                        if (playerCaster == null || playerCaster.GetPetStable() == null)
                            return SpellCastResult.BadTargets;

                        Pet pet = playerCaster.GetPet();
                        if (pet != null && pet.IsAlive())
                            return SpellCastResult.AlreadyHaveSummon;

                        PetStable petStable = playerCaster.GetPetStable();
                        var deadPetInfo = petStable.ActivePets.FirstOrDefault(petInfo => petInfo?.Health == 0);

                        if (deadPetInfo == null)
                            return SpellCastResult.BadTargets;

                        break;
                    }
                    // This is generic summon effect
                    case SpellEffectName.Summon:
                    {
                        if (unitCaster == null)
                            break;

                        var SummonProperties = CliDB.SummonPropertiesStorage.LookupByKey(spellEffectInfo.MiscValueB);
                        if (SummonProperties == null)
                            break;

                        switch (SummonProperties.Control)
                        {
                            case SummonCategory.Pet:
                                if (!m_spellInfo.HasAttribute(SpellAttr1.DismissPetFirst) && !unitCaster.GetPetGUID().IsEmpty())
                                    return SpellCastResult.AlreadyHaveSummon;
                                goto case SummonCategory.Puppet;
                            case SummonCategory.Puppet:
                                if (!unitCaster.GetCharmedGUID().IsEmpty())
                                    return SpellCastResult.AlreadyHaveCharm;
                                break;
                        }
                        break;
                    }
                    case SpellEffectName.CreateTamedPet:
                    {
                        if (m_targets.GetUnitTarget() != null)
                        {
                            if (!m_targets.GetUnitTarget().IsTypeId(TypeId.Player))
                                return SpellCastResult.BadTargets;
                            if (!m_spellInfo.HasAttribute(SpellAttr1.DismissPetFirst) && !m_targets.GetUnitTarget().GetPetGUID().IsEmpty())
                                return SpellCastResult.AlreadyHaveSummon;
                        }
                        break;
                    }
                    case SpellEffectName.SummonPet:
                    {
                        if (unitCaster == null)
                            return SpellCastResult.BadTargets;

                        if (!unitCaster.GetPetGUID().IsEmpty())                  //let warlock do a replacement summon
                        {
                            if (unitCaster.IsTypeId(TypeId.Player))
                            {
                                if (strict)                         //starting cast, trigger pet stun (cast by pet so it doesn't attack player)
                                {
                                    Pet pet = unitCaster.ToPlayer().GetPet();
                                    if (pet != null)
                                    {
                                        pet.CastSpell(pet, SharedConst.SpellPetSummoningDisorientation, new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                                            .SetOriginalCaster(pet.GetGUID())
                                            .SetTriggeringSpell(this));
                                    }
                                }
                            }
                            else if (!m_spellInfo.HasAttribute(SpellAttr1.DismissPetFirst))
                                return SpellCastResult.AlreadyHaveSummon;
                        }

                        if (!unitCaster.GetCharmedGUID().IsEmpty())
                            return SpellCastResult.AlreadyHaveCharm;

                        Player playerCaster = unitCaster.ToPlayer();
                        if (playerCaster != null && playerCaster.GetPetStable() != null)
                        {
                            PetSaveMode? petSlot = null;
                            if (spellEffectInfo.MiscValue == 0)
                            {
                                petSlot = (PetSaveMode)spellEffectInfo.CalcValue();

                                // No pet can be summoned if any pet is dead
                                foreach (var activePet in playerCaster.GetPetStable().ActivePets)
                                {
                                    if (activePet?.Health == 0)
                                    {
                                        playerCaster.SendTameFailure(PetTameResult.Dead);
                                        return SpellCastResult.DontReport;
                                    }
                                }
                            }

                            var info = Pet.GetLoadPetInfo(playerCaster.GetPetStable(), (uint)spellEffectInfo.MiscValue, 0, petSlot);
                            if (info.Item1 != null)
                            {
                                if (info.Item1.Type == PetType.Hunter)
                                {
                                    CreatureTemplate creatureInfo = Global.ObjectMgr.GetCreatureTemplate(info.Item1.CreatureId);
                                    CreatureDifficulty creatureDifficulty = creatureInfo.GetDifficulty(Difficulty.None);
                                    if (creatureInfo == null || !creatureInfo.IsTameable(playerCaster.CanTameExoticPets(), creatureDifficulty))
                                    {
                                        // if problem in exotic pet
                                        if (creatureInfo != null && creatureInfo.IsTameable(true, creatureDifficulty))
                                            playerCaster.SendTameFailure(PetTameResult.CantControlExotic);
                                        else
                                            playerCaster.SendTameFailure(PetTameResult.NoPetAvailable);

                                        return SpellCastResult.DontReport;
                                    }
                                }
                            }
                            else if (spellEffectInfo.MiscValue == 0) // when miscvalue is present it is allowed to create new pets
                            {
                                playerCaster.SendTameFailure(PetTameResult.NoPetAvailable);
                                return SpellCastResult.DontReport;
                            }
                        }
                        break;
                    }
                    case SpellEffectName.DismissPet:
                    {
                        Player playerCaster = m_caster.ToPlayer();
                        if (playerCaster == null)
                            return SpellCastResult.BadTargets;

                        Pet pet = playerCaster.GetPet();
                        if (pet == null)
                            return SpellCastResult.NoPet;

                        if (!pet.IsAlive())
                            return SpellCastResult.TargetsDead;

                        break;
                    }
                    case SpellEffectName.SummonPlayer:
                    {
                        if (!m_caster.IsTypeId(TypeId.Player))
                            return SpellCastResult.BadTargets;

                        if (m_caster.ToPlayer().GetTarget().IsEmpty())
                            return SpellCastResult.BadTargets;

                        Player target = Global.ObjAccessor.FindPlayer(m_caster.ToPlayer().GetTarget());
                        if (target == null || m_caster.ToPlayer() == target || (!target.IsInSameRaidWith(m_caster.ToPlayer()) && m_spellInfo.Id != 48955)) // refer-a-friend spell
                            return SpellCastResult.BadTargets;

                        if (target.HasSummonPending())
                            return SpellCastResult.SummonPending;

                        // check if our map is dungeon
                        InstanceMap map = m_caster.GetMap().ToInstanceMap();
                        if (map != null)
                        {
                            uint mapId = map.GetId();
                            Difficulty difficulty = map.GetDifficultyID();
                            InstanceLock mapLock = map.GetInstanceLock();
                            if (mapLock != null)
                                if (Global.InstanceLockMgr.CanJoinInstanceLock(target.GetGUID(), new MapDb2Entries(mapId, difficulty), mapLock) != TransferAbortReason.None)
                                    return SpellCastResult.TargetLockedToRaidInstance;

                            if (!target.Satisfy(Global.ObjectMgr.GetAccessRequirement(mapId, difficulty), mapId))
                                return SpellCastResult.BadTargets;
                        }
                        break;
                    }
                    // RETURN HERE
                    case SpellEffectName.SummonRafFriend:
                    {
                        if (!m_caster.IsTypeId(TypeId.Player))
                            return SpellCastResult.BadTargets;

                        Player playerCaster = m_caster.ToPlayer();
                        //
                        if (playerCaster.GetTarget().IsEmpty())
                            return SpellCastResult.BadTargets;

                        Player target = Global.ObjAccessor.FindPlayer(playerCaster.GetTarget());
                        if (target == null ||
                            !(target.GetSession().GetRecruiterId() == playerCaster.GetSession().GetAccountId() || target.GetSession().GetAccountId() == playerCaster.GetSession().GetRecruiterId()))
                            return SpellCastResult.BadTargets;

                        break;
                    }
                    case SpellEffectName.Leap:
                    case SpellEffectName.TeleportUnitsFaceCaster:
                    {
                        //Do not allow to cast it before BG starts.
                        if (m_caster.IsTypeId(TypeId.Player))
                        {
                            Battleground bg = m_caster.ToPlayer().GetBattleground();
                            if (bg != null)
                                if (bg.GetStatus() != BattlegroundStatus.InProgress)
                                    return SpellCastResult.TryAgain;
                        }
                        break;
                    }
                    case SpellEffectName.StealBeneficialBuff:
                    {
                        if (m_targets.GetUnitTarget() == null || m_targets.GetUnitTarget() == m_caster)
                            return SpellCastResult.BadTargets;

                        break;
                    }
                    case SpellEffectName.LeapBack:
                    {
                        if (unitCaster == null)
                            return SpellCastResult.BadTargets;

                        if (unitCaster.HasUnitState(UnitState.Root))
                        {
                            if (unitCaster.IsTypeId(TypeId.Player))
                                return SpellCastResult.Rooted;
                            else
                                return SpellCastResult.DontReport;
                        }
                        break;
                    }
                    case SpellEffectName.Jump:
                    case SpellEffectName.JumpDest:
                    {
                        if (unitCaster == null)
                            return SpellCastResult.BadTargets;

                        if (unitCaster.HasUnitState(UnitState.Root))
                            return SpellCastResult.Rooted;
                        break;
                    }
                    case SpellEffectName.TalentSpecSelect:
                    {
                        ChrSpecializationRecord spec = CliDB.ChrSpecializationStorage.LookupByKey(m_misc.SpecializationId);
                        Player playerCaster = m_caster.ToPlayer();
                        if (playerCaster == null)
                            return SpellCastResult.TargetNotPlayer;

                        if (spec == null || (spec.ClassID != (uint)player.GetClass() && !spec.IsPetSpecialization()))
                            return SpellCastResult.NoSpec;

                        if (spec.IsPetSpecialization())
                        {
                            Pet pet = player.GetPet();
                            if (pet == null || pet.GetPetType() != PetType.Hunter || pet.GetCharmInfo() == null)
                                return SpellCastResult.NoPet;
                        }

                        // can't change during already started arena/Battleground
                        Battleground bg = player.GetBattleground();
                        if (bg != null)
                            if (bg.GetStatus() == BattlegroundStatus.InProgress)
                                return SpellCastResult.NotInBattleground;
                        break;
                    }
                    case SpellEffectName.RemoveTalent:
                    {
                        Player playerCaster = m_caster.ToPlayer();
                        if (playerCaster == null)
                            return SpellCastResult.BadTargets;

                        TalentRecord talent = CliDB.TalentStorage.LookupByKey(m_misc.TalentId);
                        if (talent == null)
                            return SpellCastResult.DontReport;

                        if (playerCaster.GetSpellHistory().HasCooldown(talent.SpellID))
                        {
                            param1 = (int)talent.SpellID;
                            return SpellCastResult.CantUntalent;
                        }
                        break;
                    }
                    case SpellEffectName.CreateHeirloomItem:
                    {
                        if (!m_caster.IsPlayer())
                            return SpellCastResult.BadTargets;

                        if (!m_caster.ToPlayer().GetSession().GetCollectionMgr().HasHeirloom(m_misc.ItemId))
                            return SpellCastResult.BadTargets;

                        break;
                    }
                    case SpellEffectName.GiveArtifactPower:
                    case SpellEffectName.GiveArtifactPowerNoBonus:
                    {
                        Player playerCaster = m_caster.ToPlayer();
                        if (playerCaster == null)
                            return SpellCastResult.BadTargets;

                        Aura artifactAura = playerCaster.GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);
                        if (artifactAura == null)
                            return SpellCastResult.NoArtifactEquipped;

                        Item artifact = playerCaster.ToPlayer().GetItemByGuid(artifactAura.GetCastItemGUID());
                        if (artifact == null)
                            return SpellCastResult.NoArtifactEquipped;

                        if (spellEffectInfo.Effect == SpellEffectName.GiveArtifactPower)
                        {
                            ArtifactRecord artifactEntry = CliDB.ArtifactStorage.LookupByKey(artifact.GetTemplate().GetArtifactID());
                            if (artifactEntry == null || artifactEntry.ArtifactCategoryID != spellEffectInfo.MiscValue)
                                return SpellCastResult.WrongArtifactEquipped;
                        }
                        break;
                    }
                    case SpellEffectName.ChangeBattlepetQuality:
                    case SpellEffectName.GrantBattlepetLevel:
                    case SpellEffectName.GrantBattlepetExperience:
                    {
                        Player playerCaster = m_caster.ToPlayer();
                        if (playerCaster == null || m_targets.GetUnitTarget() == null || !m_targets.GetUnitTarget().IsCreature())
                            return SpellCastResult.BadTargets;

                        var battlePetMgr = playerCaster.GetSession().GetBattlePetMgr();
                        if (!battlePetMgr.HasJournalLock())
                            return SpellCastResult.CantDoThatRightNow;

                        Creature creature = m_targets.GetUnitTarget().ToCreature();
                        if (creature != null)
                        {
                            if (playerCaster.GetSummonedBattlePetGUID().IsEmpty() || creature.GetBattlePetCompanionGUID().IsEmpty())
                                return SpellCastResult.NoPet;

                            if (playerCaster.GetSummonedBattlePetGUID() != creature.GetBattlePetCompanionGUID())
                                return SpellCastResult.BadTargets;

                            var battlePet = battlePetMgr.GetPet(creature.GetBattlePetCompanionGUID());
                            if (battlePet != null)
                            {
                                var battlePetSpecies = CliDB.BattlePetSpeciesStorage.LookupByKey(battlePet.PacketInfo.Species);
                                if (battlePetSpecies != null)
                                {
                                    uint battlePetType = (uint)spellEffectInfo.MiscValue;
                                    if (battlePetType != 0)
                                        if ((battlePetType & (1 << battlePetSpecies.PetTypeEnum)) == 0)
                                            return SpellCastResult.WrongBattlePetType;

                                    if (spellEffectInfo.Effect == SpellEffectName.ChangeBattlepetQuality)
                                    {
                                        var qualityRecord = CliDB.BattlePetBreedQualityStorage.Values.FirstOrDefault(a1 => a1.MaxQualityRoll < spellEffectInfo.CalcBaseValue(m_caster, creature, m_castItemEntry, m_castItemLevel));

                                        BattlePetBreedQuality quality = BattlePetBreedQuality.Poor;
                                        if (qualityRecord != null)
                                            quality = (BattlePetBreedQuality)qualityRecord.QualityEnum;

                                        if (battlePet.PacketInfo.Quality >= (byte)quality)
                                            return SpellCastResult.CantUpgradeBattlePet;

                                    }

                                    if (spellEffectInfo.Effect == SpellEffectName.GrantBattlepetLevel || spellEffectInfo.Effect == SpellEffectName.GrantBattlepetExperience)
                                        if (battlePet.PacketInfo.Level >= SharedConst.MaxBattlePetLevel)
                                            return SpellCastResult.GrantPetLevelFail;

                                    if (battlePetSpecies.HasFlag(BattlePetSpeciesFlags.CantBattle))
                                        return SpellCastResult.BadTargets;
                                }
                            }
                        }
                        break;
                    }
                    default:
                        break;
                }

                if (spellEffectInfo.IsAura())
                    approximateAuraEffectMask |= 1u << (int)spellEffectInfo.EffectIndex;
                else if (spellEffectInfo.IsEffect())
                    nonAuraEffectMask |= 1u << (int)spellEffectInfo.EffectIndex;
            }

            foreach (var spellEffectInfo in m_spellInfo.GetEffects())
            {
                switch (spellEffectInfo.ApplyAuraName)
                {
                    case AuraType.ModPossessPet:
                    {
                        if (!m_caster.IsTypeId(TypeId.Player))
                            return SpellCastResult.NoPet;

                        Pet pet = m_caster.ToPlayer().GetPet();
                        if (pet == null)
                            return SpellCastResult.NoPet;

                        if (!pet.GetCharmerGUID().IsEmpty())
                            return SpellCastResult.AlreadyHaveCharm;
                        break;
                    }
                    case AuraType.ModPossess:
                    case AuraType.ModCharm:
                    case AuraType.AoeCharm:
                    {
                        Unit unitCaster1 = (m_originalCaster != null ? m_originalCaster : m_caster.ToUnit());
                        if (unitCaster1 == null)
                            return SpellCastResult.BadTargets;

                        if (!unitCaster1.GetCharmerGUID().IsEmpty())
                            return SpellCastResult.AlreadyHaveCharm;

                        if (spellEffectInfo.ApplyAuraName == AuraType.ModCharm || spellEffectInfo.ApplyAuraName == AuraType.ModPossess)
                        {
                            if (!m_spellInfo.HasAttribute(SpellAttr1.DismissPetFirst) && !unitCaster1.GetPetGUID().IsEmpty())
                                return SpellCastResult.AlreadyHaveSummon;

                            if (!unitCaster1.GetCharmedGUID().IsEmpty())
                                return SpellCastResult.AlreadyHaveCharm;
                        }

                        Unit target = m_targets.GetUnitTarget();
                        if (target != null)
                        {
                            if (target.IsTypeId(TypeId.Unit) && target.ToCreature().IsVehicle())
                                return SpellCastResult.BadImplicitTargets;

                            if (target.IsMounted())
                                return SpellCastResult.CantBeCharmed;

                            if (!target.GetCharmerGUID().IsEmpty())
                                return SpellCastResult.Charmed;

                            if (target.GetOwner() != null && target.GetOwner().IsTypeId(TypeId.Player))
                                return SpellCastResult.TargetIsPlayerControlled;

                            int damage = CalculateDamage(spellEffectInfo, target);
                            if (damage != 0 && target.GetLevelForTarget(m_caster) > damage)
                                return SpellCastResult.Highlevel;
                        }

                        break;
                    }
                    case AuraType.Mounted:
                    {
                        if (unitCaster == null)
                            return SpellCastResult.BadTargets;

                        if (unitCaster.IsInWater() && m_spellInfo.HasAura(AuraType.ModIncreaseMountedFlightSpeed))
                            return SpellCastResult.OnlyAbovewater;

                        if (unitCaster.IsInDisallowedMountForm())
                        {
                            SendMountResult(MountResult.Shapeshifted); // mount result gets sent before the cast result
                            return SpellCastResult.DontReport;
                        }

                        break;
                    }
                    case AuraType.RangedAttackPowerAttackerBonus:
                    {
                        if (m_targets.GetUnitTarget() == null)
                            return SpellCastResult.BadImplicitTargets;

                        // can be casted at non-friendly unit or own pet/charm
                        if (m_caster.IsFriendlyTo(m_targets.GetUnitTarget()))
                            return SpellCastResult.TargetFriendly;

                        break;
                    }
                    case AuraType.Fly:
                    case AuraType.ModIncreaseFlightSpeed:
                    {
                        // not allow cast fly spells if not have req. skills  (all spells is self target)
                        // allow always ghost flight spells
                        if (m_originalCaster != null && m_originalCaster.IsTypeId(TypeId.Player) && m_originalCaster.IsAlive())
                        {
                            BattleField battleField = Global.BattleFieldMgr.GetBattlefieldToZoneId(m_originalCaster.GetMap(), m_originalCaster.GetZoneId());
                            if (battleField != null && !battleField.CanFlyIn())
                                return SpellCastResult.NotHere;
                        }
                        break;
                    }
                    case AuraType.PeriodicManaLeech:
                    {
                        if (spellEffectInfo.IsTargetingArea())
                            break;

                        if (m_targets.GetUnitTarget() == null)
                            return SpellCastResult.BadImplicitTargets;

                        if (!m_caster.IsTypeId(TypeId.Player) || m_CastItem != null)
                            break;

                        if (m_targets.GetUnitTarget().GetPowerType() != PowerType.Mana)
                            return SpellCastResult.BadTargets;

                        break;
                    }
                    default:
                        break;
                }

                // check if target already has the same type, but more powerful aura
                if (!m_spellInfo.HasAttribute(SpellAttr4.AuraNeverBounces)
                    && (nonAuraEffectMask == 0 || m_spellInfo.HasAttribute(SpellAttr4.AuraBounceFailsSpell))
                    && (approximateAuraEffectMask & (1 << (int)spellEffectInfo.EffectIndex)) != 0
                    && !m_spellInfo.IsTargetingArea())
                {
                    Unit target = m_targets.GetUnitTarget();
                    if (target != null)
                        if (!target.IsHighestExclusiveAuraEffect(m_spellInfo, spellEffectInfo.ApplyAuraName, spellEffectInfo.CalcValue(m_caster, m_spellValue.EffectBasePoints[spellEffectInfo.EffectIndex], null, m_castItemEntry, m_castItemLevel), approximateAuraEffectMask, false))
                            return SpellCastResult.AuraBounced;
                }
            }

            // check trade slot case (last, for allow catch any another cast problems)
            if (Convert.ToBoolean(m_targets.GetTargetMask() & SpellCastTargetFlags.TradeItem))
            {
                if (m_CastItem != null)
                    return SpellCastResult.ItemEnchantTradeWindow;

                if (m_spellInfo.HasAttribute(SpellAttr2.EnchantOwnItemOnly))
                    return SpellCastResult.ItemEnchantTradeWindow;

                if (!m_caster.IsTypeId(TypeId.Player))
                    return SpellCastResult.NotTrading;

                TradeData my_trade = m_caster.ToPlayer().GetTradeData();

                if (my_trade == null)
                    return SpellCastResult.NotTrading;

                TradeSlots slot = (TradeSlots)m_targets.GetItemTargetGUID().GetLowValue();
                if (slot != TradeSlots.NonTraded)
                    return SpellCastResult.BadTargets;

                if (!IsTriggered())
                    if (my_trade.GetSpell() != 0)
                        return SpellCastResult.ItemAlreadyEnchanted;
            }

            // all ok
            return SpellCastResult.SpellCastOk;
        }

        public SpellCastResult CheckPetCast(Unit target)
        {
            Unit unitCaster = m_caster.ToUnit();
            if (unitCaster != null && unitCaster.HasUnitState(UnitState.Casting) && !_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreCastInProgress))              //prevent spellcast interruption by another spellcast
                return SpellCastResult.SpellInProgress;

            // dead owner (pets still alive when owners ressed?)
            Unit owner = m_caster.GetCharmerOrOwner();
            if (owner != null)
                if (!owner.IsAlive())
                    return SpellCastResult.CasterDead;

            if (target == null && m_targets.GetUnitTarget() != null)
                target = m_targets.GetUnitTarget();

            if (m_spellInfo.NeedsExplicitUnitTarget())
            {
                if (target == null)
                    return SpellCastResult.BadImplicitTargets;
                m_targets.SetUnitTarget(target);
            }

            // cooldown
            Creature creatureCaster = m_caster.ToCreature();
            if (creatureCaster != null)
                if (creatureCaster.GetSpellHistory().HasCooldown(m_spellInfo.Id))
                    return SpellCastResult.NotReady;

            // Check if spell is affected by GCD
            if (m_spellInfo.StartRecoveryCategory > 0)
                if (unitCaster.GetCharmInfo() != null && unitCaster.GetSpellHistory().HasGlobalCooldown(m_spellInfo))
                    return SpellCastResult.NotReady;

            return CheckCast(true);
        }

        SpellCastResult CheckCasterAuras(ref int param1)
        {
            Unit unitCaster = (m_originalCaster != null ? m_originalCaster : m_caster.ToUnit());
            if (unitCaster == null)
                return SpellCastResult.SpellCastOk;

            // these attributes only show the spell as usable on the client when it has related aura applied
            // still they need to be checked against certain mechanics

            // SPELL_ATTR5_USABLE_WHILE_STUNNED by default only MECHANIC_STUN (ie no sleep, knockout, freeze, etc.)
            bool usableWhileStunned = m_spellInfo.HasAttribute(SpellAttr5.AllowWhileStunned);

            // SPELL_ATTR5_USABLE_WHILE_FEARED by default only fear (ie no horror)
            bool usableWhileFeared = m_spellInfo.HasAttribute(SpellAttr5.AllowWhileFleeing);

            // SPELL_ATTR5_USABLE_WHILE_CONFUSED by default only disorient (ie no polymorph)
            bool usableWhileConfused = m_spellInfo.HasAttribute(SpellAttr5.AllowWhileConfused);

            // Check whether the cast should be prevented by any state you might have.
            SpellCastResult result = SpellCastResult.SpellCastOk;
            // Get unit state
            UnitFlags unitflag = (UnitFlags)(uint)unitCaster.m_unitData.Flags;

            // this check should only be done when player does cast directly
            // (ie not when it's called from a script) Breaks for example PlayerAI when charmed
            /*if (!unitCaster.GetCharmerGUID().IsEmpty())
            {
                Unit charmer = unitCaster.GetCharmer();
                if (charmer != null)
                    if (charmer.GetUnitBeingMoved() != unitCaster && !CheckSpellCancelsCharm(ref param1))
                        result = SpellCastResult.Charmed;
            }*/

            // spell has attribute usable while having a cc state, check if caster has allowed mechanic auras, another mechanic types must prevent cast spell
            SpellCastResult mechanicCheck(AuraType auraType, ref int _param1)
            {
                bool foundNotMechanic = false;
                var auras = unitCaster.GetAuraEffectsByType(auraType);
                foreach (AuraEffect aurEff in auras)
                {
                    ulong mechanicMask = aurEff.GetSpellInfo().GetAllEffectsMechanicMask();
                    if (mechanicMask != 0 && !Convert.ToBoolean(mechanicMask & GetSpellInfo().GetAllowedMechanicMask()))
                    {
                        foundNotMechanic = true;

                        // fill up aura mechanic info to send client proper error message
                        _param1 = (int)aurEff.GetSpellEffectInfo().Mechanic;
                        if (_param1 == 0)
                            _param1 = (int)aurEff.GetSpellInfo().Mechanic;

                        break;
                    }
                }

                if (foundNotMechanic)
                {
                    switch (auraType)
                    {
                        case AuraType.ModStun:
                        case AuraType.ModStunDisableGravity:
                            return SpellCastResult.Stunned;
                        case AuraType.ModFear:
                            return SpellCastResult.Fleeing;
                        case AuraType.ModConfuse:
                            return SpellCastResult.Confused;
                        default:
                            //ABORT();
                            return SpellCastResult.NotKnown;
                    }
                }

                return SpellCastResult.SpellCastOk;
            }

            if (unitflag.HasAnyFlag(UnitFlags.Stunned))
            {
                if (usableWhileStunned)
                {
                    SpellCastResult mechanicResult = mechanicCheck(AuraType.ModStun, ref param1);
                    if (mechanicResult != SpellCastResult.SpellCastOk)
                        result = mechanicResult;
                }
                else if (!CheckSpellCancelsStun(ref param1))
                    result = SpellCastResult.Stunned;
            }
            else if (unitCaster.IsSilenced(m_spellSchoolMask) && (m_spellInfo.PreventionType & SpellPreventionType.Silence) != 0 && !CheckSpellCancelsSilence(ref param1))
                result = SpellCastResult.Silenced;
            else if (unitflag.HasAnyFlag(UnitFlags.Pacified) && m_spellInfo.PreventionType.HasAnyFlag(SpellPreventionType.Pacify) && !CheckSpellCancelsPacify(ref param1))
                result = SpellCastResult.Pacified;
            else if (unitflag.HasAnyFlag(UnitFlags.Fleeing))
            {
                if (usableWhileFeared)
                {
                    SpellCastResult mechanicResult = mechanicCheck(AuraType.ModFear, ref param1);
                    if (mechanicResult != SpellCastResult.SpellCastOk)
                        result = mechanicResult;
                    else
                    {
                        mechanicResult = mechanicCheck(AuraType.ModStunDisableGravity, ref param1);
                        if (mechanicResult != SpellCastResult.SpellCastOk)
                            result = mechanicResult;
                    }
                }
                else if (!CheckSpellCancelsFear(ref param1))
                    result = SpellCastResult.Fleeing;
            }
            else if (unitflag.HasAnyFlag(UnitFlags.Confused))
            {
                if (usableWhileConfused)
                {
                    SpellCastResult mechanicResult = mechanicCheck(AuraType.ModConfuse, ref param1);
                    if (mechanicResult != SpellCastResult.SpellCastOk)
                        result = mechanicResult;
                }
                else if (!CheckSpellCancelsConfuse(ref param1))
                    result = SpellCastResult.Confused;
            }
            else if (unitCaster.HasUnitFlag2(UnitFlags2.NoActions) && m_spellInfo.PreventionType.HasAnyFlag(SpellPreventionType.NoActions) && !CheckSpellCancelsNoActions(ref param1))
                result = SpellCastResult.NoActions;

            // Attr must make flag drop spell totally immune from all effects
            if (result != SpellCastResult.SpellCastOk)
                return (param1 != 0) ? SpellCastResult.PreventedByMechanic : result;

            return SpellCastResult.SpellCastOk;
        }

        bool CheckSpellCancelsAuraEffect(AuraType auraType, ref int param1)
        {
            Unit unitCaster = (m_originalCaster != null ? m_originalCaster : m_caster.ToUnit());
            if (unitCaster == null)
                return false;

            // Checking auras is needed now, because you are prevented by some state but the spell grants immunity.
            var auraEffects = unitCaster.GetAuraEffectsByType(auraType);
            if (auraEffects.Empty())
                return true;

            foreach (AuraEffect aurEff in auraEffects)
            {
                if (m_spellInfo.SpellCancelsAuraEffect(aurEff))
                    continue;

                param1 = (int)aurEff.GetSpellEffectInfo().Mechanic;
                if (param1 == 0)
                    param1 = (int)aurEff.GetSpellInfo().Mechanic;

                return false;
            }

            return true;
        }

        bool CheckSpellCancelsCharm(ref int param1)
        {
            return CheckSpellCancelsAuraEffect(AuraType.ModCharm, ref param1) ||
                CheckSpellCancelsAuraEffect(AuraType.AoeCharm, ref param1) ||
                CheckSpellCancelsAuraEffect(AuraType.ModPossess, ref param1);
        }

        bool CheckSpellCancelsStun(ref int param1)
        {
            return CheckSpellCancelsAuraEffect(AuraType.ModStun, ref param1) &&
                 CheckSpellCancelsAuraEffect(AuraType.ModStunDisableGravity, ref param1);
        }

        bool CheckSpellCancelsSilence(ref int param1)
        {
            return CheckSpellCancelsAuraEffect(AuraType.ModSilence, ref param1) ||
                CheckSpellCancelsAuraEffect(AuraType.ModPacifySilence, ref param1);
        }

        bool CheckSpellCancelsPacify(ref int param1)
        {
            return CheckSpellCancelsAuraEffect(AuraType.ModPacify, ref param1) ||
                CheckSpellCancelsAuraEffect(AuraType.ModPacifySilence, ref param1);
        }

        bool CheckSpellCancelsFear(ref int param1)
        {
            return CheckSpellCancelsAuraEffect(AuraType.ModFear, ref param1);
        }

        bool CheckSpellCancelsConfuse(ref int param1)
        {
            return CheckSpellCancelsAuraEffect(AuraType.ModConfuse, ref param1);
        }

        bool CheckSpellCancelsNoActions(ref int param1)
        {
            return CheckSpellCancelsAuraEffect(AuraType.ModNoActions, ref param1);
        }

        SpellCastResult CheckArenaAndRatedBattlegroundCastRules()
        {
            bool isRatedBattleground = false; // NYI
            bool isArena = !isRatedBattleground;

            // check USABLE attributes
            // USABLE takes precedence over NOT_USABLE
            if (isRatedBattleground && m_spellInfo.HasAttribute(SpellAttr9.IgnoreDefaultRatedBattlegroundRestrictions))
                return SpellCastResult.SpellCastOk;

            if (isArena && m_spellInfo.HasAttribute(SpellAttr4.IgnoreDefaultArenaRestrictions))
                return SpellCastResult.SpellCastOk;

            // check NOT_USABLE attributes
            if (m_spellInfo.HasAttribute(SpellAttr4.NotInArenaOrRatedBattleground))
                return isArena ? SpellCastResult.NotInArena : SpellCastResult.NotInBattleground;

            if (isArena && m_spellInfo.HasAttribute(SpellAttr9.NotInArena))
                return SpellCastResult.NotInArena;

            // check cooldowns
            uint spellCooldown = m_spellInfo.GetRecoveryTime();
            if (isArena && spellCooldown > 10 * Time.Minute * Time.InMilliseconds) // not sure if still needed
                return SpellCastResult.NotInArena;

            if (isRatedBattleground && spellCooldown > 15 * Time.Minute * Time.InMilliseconds)
                return SpellCastResult.NotInBattleground;

            return SpellCastResult.SpellCastOk;
        }

        public bool CanAutoCast(Unit target)
        {
            if (target == null)
                return (CheckPetCast(target) == SpellCastResult.SpellCastOk);

            ObjectGuid targetguid = target.GetGUID();

            // check if target already has the same or a more powerful aura
            foreach (var spellEffectInfo in m_spellInfo.GetEffects())
            {
                if (!spellEffectInfo.IsAura())
                    continue;

                AuraType auraType = spellEffectInfo.ApplyAuraName;
                var auras = target.GetAuraEffectsByType(auraType);
                foreach (var eff in auras)
                {
                    if (GetSpellInfo().Id == eff.GetSpellInfo().Id)
                        return false;

                    switch (Global.SpellMgr.CheckSpellGroupStackRules(GetSpellInfo(), eff.GetSpellInfo()))
                    {
                        case SpellGroupStackRule.Exclusive:
                            return false;
                        case SpellGroupStackRule.ExclusiveFromSameCaster:
                            if (GetCaster() == eff.GetCaster())
                                return false;
                            break;
                        case SpellGroupStackRule.ExclusiveSameEffect: // this one has further checks, but i don't think they're necessary for autocast logic
                        case SpellGroupStackRule.ExclusiveHighest:
                            if (Math.Abs(spellEffectInfo.CalcBaseValue(m_caster, target, 0, -1)) <= Math.Abs(eff.GetAmount()))
                                return false;
                            break;
                        case SpellGroupStackRule.Default:
                        default:
                            break;
                    }
                }
            }

            SpellCastResult result = CheckPetCast(target);

            if (result == SpellCastResult.SpellCastOk || result == SpellCastResult.UnitNotInfront)
            {
                // do not check targets for ground-targeted spells (we target them on top of the intended target anyway)
                if (GetSpellInfo().ExplicitTargetMask.HasAnyFlag((uint)SpellCastTargetFlags.DestLocation))
                    return true;
                SelectSpellTargets();
                //check if among target units, our WANTED target is as well (.only self cast spells return false)
                foreach (var ihit in m_UniqueTargetInfo)
                    if (ihit.TargetGUID == targetguid)
                        return true;
            }
            // either the cast failed or the intended target wouldn't be hit
            return false;
        }

        SpellCastResult CheckRange(bool strict)
        {
            // Don't check for instant cast spells
            if (!strict && m_casttime == 0)
                return SpellCastResult.SpellCastOk;

            (float minRange, float maxRange) = GetMinMaxRange(strict);

            // dont check max_range to strictly after cast
            if (m_spellInfo.RangeEntry != null && m_spellInfo.RangeEntry.HasFlag(SpellRangeFlag.Melee) && !strict)
                maxRange += Math.Min(3.0f, maxRange * 0.1f); // 10% but no more than 3.0f

            // get square values for sqr distance checks
            minRange *= minRange;
            maxRange *= maxRange;

            Unit target = m_targets.GetUnitTarget();
            if (target != null && target != m_caster)
            {
                if (m_caster.GetExactDistSq(target) > maxRange)
                    return SpellCastResult.OutOfRange;

                if (minRange > 0.0f && m_caster.GetExactDistSq(target) < minRange)
                    return SpellCastResult.OutOfRange;

                if (m_caster.IsTypeId(TypeId.Player) &&
                    ((m_spellInfo.FacingCasterFlags.HasAnyFlag(1u) && !m_caster.HasInArc((float)Math.PI, target))
                        && !m_caster.ToPlayer().IsWithinBoundaryRadius(target)))
                    return SpellCastResult.UnitNotInfront;
            }

            GameObject goTarget = m_targets.GetGOTarget();
            if (goTarget != null)
            {
                if (!goTarget.IsAtInteractDistance(m_caster.ToPlayer(), m_spellInfo))
                    return SpellCastResult.OutOfRange;
            }

            if (m_targets.HasDst() && !m_targets.HasTraj())
            {
                if (m_caster.GetExactDistSq(m_targets.GetDstPos()) > maxRange)
                    return SpellCastResult.OutOfRange;
                if (minRange > 0.0f && m_caster.GetExactDistSq(m_targets.GetDstPos()) < minRange)
                    return SpellCastResult.OutOfRange;
            }

            return SpellCastResult.SpellCastOk;
        }

        public static bool CanIncreaseRangeByMovement(Unit unit)
        {
            // forward running only
            return unit.HasUnitMovementFlag(MovementFlag.Forward | MovementFlag.StrafeLeft | MovementFlag.StrafeRight | MovementFlag.Falling) && !unit.IsWalking();
        }

        (float minRange, float maxRange) GetMinMaxRange(bool strict)
        {
            float rangeMod = 0.0f;
            float minRange = 0.0f;
            float maxRange = 0.0f;

            if (strict && m_spellInfo.IsNextMeleeSwingSpell())
                return (0.0f, 100.0f);

            Unit unitCaster = m_caster.ToUnit();
            if (m_spellInfo.RangeEntry != null)
            {
                Unit target = m_targets.GetUnitTarget();
                if (m_spellInfo.RangeEntry.HasFlag(SpellRangeFlag.Melee))
                {
                    // when the target is not a unit, take the caster's combat reach as the target's combat reach.
                    if (unitCaster != null)
                        rangeMod = unitCaster.GetMeleeRange(target != null ? target : unitCaster);
                }
                else
                {
                    float meleeRange = 0.0f;
                    if (m_spellInfo.RangeEntry.HasFlag(SpellRangeFlag.Ranged))
                    {
                        // when the target is not a unit, take the caster's combat reach as the target's combat reach.
                        if (unitCaster != null)
                            meleeRange = unitCaster.GetMeleeRange(target != null ? target : unitCaster);
                    }

                    minRange = m_caster.GetSpellMinRangeForTarget(target, m_spellInfo) + meleeRange;
                    maxRange = m_caster.GetSpellMaxRangeForTarget(target, m_spellInfo);

                    if (target != null || m_targets.GetCorpseTarget() != null)
                    {
                        rangeMod = m_caster.GetCombatReach() + (target != null ? target.GetCombatReach() : m_caster.GetCombatReach());

                        if (minRange > 0.0f && !m_spellInfo.RangeEntry.HasFlag(SpellRangeFlag.Ranged))
                            minRange += rangeMod;
                    }
                }

                if (target != null && unitCaster != null && CanIncreaseRangeByMovement(target) && CanIncreaseRangeByMovement(unitCaster) &&
                    (m_spellInfo.RangeEntry.HasFlag(SpellRangeFlag.Melee) || target.IsPlayer()))
                    rangeMod += 8.0f / 3.0f;
            }

            if (m_spellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) && m_caster.IsTypeId(TypeId.Player))
            {
                Item ranged = m_caster.ToPlayer().GetWeaponForAttack(WeaponAttackType.RangedAttack, true);
                if (ranged != null)
                    maxRange *= ranged.GetTemplate().GetRangedModRange() * 0.01f;
            }

            Player modOwner = m_caster.GetSpellModOwner();
            if (modOwner != null)
                modOwner.ApplySpellMod(m_spellInfo, SpellModOp.Range, ref maxRange, this);

            maxRange += rangeMod;

            return (minRange, maxRange);
        }

        SpellCastResult CheckPower()
        {
            Unit unitCaster = m_caster.ToUnit();
            if (unitCaster == null)
                return SpellCastResult.SpellCastOk;

            // item cast not used power
            if (m_CastItem != null)
                return SpellCastResult.SpellCastOk;

            foreach (SpellPowerCost cost in m_powerCost)
            {
                // health as power used - need check health amount
                if (cost.Power == PowerType.Health)
                {
                    if (unitCaster.GetHealth() <= (ulong)cost.Amount)
                        return SpellCastResult.CasterAurastate;
                    continue;
                }

                // Check valid power type
                if (cost.Power >= PowerType.Max)
                {
                    Log.outError(LogFilter.Spells, "Spell.CheckPower: Unknown power type '{0}'", cost.Power);
                    return SpellCastResult.Unknown;
                }

                //check rune cost only if a spell has PowerType == POWER_RUNES
                if (cost.Power == PowerType.Runes)
                {
                    SpellCastResult failReason = CheckRuneCost();
                    if (failReason != SpellCastResult.SpellCastOk)
                        return failReason;
                }

                // Check power amount
                if (unitCaster.GetPower(cost.Power) < cost.Amount)
                    return SpellCastResult.NoPower;
            }

            return SpellCastResult.SpellCastOk;
        }

        SpellCastResult CheckItems(ref int param1, ref int param2)
        {
            Player player = m_caster.ToPlayer();
            if (player == null)
                return SpellCastResult.SpellCastOk;

            if (m_CastItem == null)
            {
                if (!m_castItemGUID.IsEmpty())
                    return SpellCastResult.ItemNotReady;
            }
            else
            {
                uint itemid = m_CastItem.GetEntry();
                if (!player.HasItemCount(itemid))
                    return SpellCastResult.ItemNotReady;

                ItemTemplate proto = m_CastItem.GetTemplate();
                if (proto == null)
                    return SpellCastResult.ItemNotReady;

                foreach (ItemEffectRecord itemEffect in m_CastItem.GetEffects())
                    if (itemEffect.LegacySlotIndex < m_CastItem.m_itemData.SpellCharges.GetSize() && itemEffect.Charges != 0)
                        if (m_CastItem.GetSpellCharges(itemEffect.LegacySlotIndex) == 0)
                            return SpellCastResult.NoChargesRemain;

                // consumable cast item checks
                if (proto.GetClass() == ItemClass.Consumable && m_targets.GetUnitTarget() != null)
                {
                    // such items should only fail if there is no suitable effect at all - see Rejuvenation Potions for example
                    SpellCastResult failReason = SpellCastResult.SpellCastOk;
                    foreach (var spellEffectInfo in m_spellInfo.GetEffects())
                    {
                        // skip check, pet not required like checks, and for TARGET_UNIT_PET m_targets.GetUnitTarget() is not the real target but the caster
                        if (spellEffectInfo.TargetA.GetTarget() == Targets.UnitPet)
                            continue;

                        if (spellEffectInfo.Effect == SpellEffectName.Heal)
                        {
                            if (m_targets.GetUnitTarget().IsFullHealth())
                            {
                                failReason = SpellCastResult.AlreadyAtFullHealth;
                                continue;
                            }
                            else
                            {
                                failReason = SpellCastResult.SpellCastOk;
                                break;
                            }
                        }

                        // Mana Potion, Rage Potion, Thistle Tea(Rogue), ...
                        if (spellEffectInfo.Effect == SpellEffectName.Energize)
                        {
                            if (spellEffectInfo.MiscValue < 0 || spellEffectInfo.MiscValue >= (int)PowerType.Max)
                            {
                                failReason = SpellCastResult.AlreadyAtFullPower;
                                continue;
                            }

                            PowerType power = (PowerType)spellEffectInfo.MiscValue;
                            if (m_targets.GetUnitTarget().GetPower(power) == m_targets.GetUnitTarget().GetMaxPower(power))
                            {
                                failReason = SpellCastResult.AlreadyAtFullPower;
                                continue;
                            }
                            else
                            {
                                failReason = SpellCastResult.SpellCastOk;
                                break;
                            }
                        }
                    }
                    if (failReason != SpellCastResult.SpellCastOk)
                        return failReason;
                }
            }

            // check target item
            if (!m_targets.GetItemTargetGUID().IsEmpty())
            {
                Item item = m_targets.GetItemTarget();
                if (item == null)
                    return SpellCastResult.ItemGone;

                if (!item.IsFitToSpellRequirements(m_spellInfo))
                    return SpellCastResult.EquippedItemClass;
            }
            // if not item target then required item must be equipped
            else
            {
                if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreEquippedItemRequirement))
                    if (m_caster.IsTypeId(TypeId.Player) && !m_caster.ToPlayer().HasItemFitToSpellRequirements(m_spellInfo))
                        return SpellCastResult.EquippedItemClass;
            }

            // do not take reagents for these item casts
            if (!(m_CastItem != null && m_CastItem.GetTemplate().HasFlag(ItemFlags.NoReagentCost)))
            {
                bool checkReagents = !Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreReagentCost) && !player.CanNoReagentCast(m_spellInfo);
                // Not own traded item (in trader trade slot) requires reagents even if triggered spell
                if (!checkReagents)
                {
                    Item targetItem = m_targets.GetItemTarget();
                    if (targetItem != null)
                        if (targetItem.GetOwnerGUID() != player.GetGUID())
                            checkReagents = true;
                }

                // check reagents (ignore triggered spells with reagents processed by original spell) and special reagent ignore case.
                if (checkReagents)
                {
                    for (byte i = 0; i < SpellConst.MaxReagents; i++)
                    {
                        if (m_spellInfo.Reagent[i] <= 0)
                            continue;

                        uint itemid = (uint)m_spellInfo.Reagent[i];
                        uint itemcount = m_spellInfo.ReagentCount[i];

                        // if CastItem is also spell reagent
                        if (m_CastItem != null && m_CastItem.GetEntry() == itemid)
                        {
                            ItemTemplate proto = m_CastItem.GetTemplate();
                            if (proto == null)
                                return SpellCastResult.ItemNotReady;

                            foreach (ItemEffectRecord itemEffect in m_CastItem.GetEffects())
                            {
                                if (itemEffect.LegacySlotIndex >= m_CastItem.m_itemData.SpellCharges.GetSize())
                                    continue;

                                // CastItem will be used up and does not count as reagent
                                int charges = m_CastItem.GetSpellCharges(itemEffect.LegacySlotIndex);
                                if (itemEffect.Charges < 0 && Math.Abs(charges) < 2)
                                {
                                    ++itemcount;
                                    break;
                                }
                            }
                        }
                        if (!player.HasItemCount(itemid, itemcount))
                        {
                            param1 = (int)itemid;
                            return SpellCastResult.Reagents;
                        }
                    }

                    foreach (var reagentsCurrency in m_spellInfo.ReagentsCurrency)
                    {
                        if (!player.HasCurrency(reagentsCurrency.CurrencyTypesID, reagentsCurrency.CurrencyCount))
                        {
                            param1 = -1;
                            param2 = reagentsCurrency.CurrencyTypesID;

                            return SpellCastResult.Reagents;
                        }
                    }
                }

                if (!m_spellInfo.HasAttribute(SpellAttr9.IgnoreTotemRequirementsForCasting))
                {
                    // check totem-item requirements (items presence in inventory)
                    foreach (uint totem in m_spellInfo.Totem)
                    {
                        if (totem != 0 && player.HasItemCount(totem))
                        {
                            param1 = (int)totem;
                            return SpellCastResult.Totems;
                        }
                    }
                    // Check items for TotemCategory (items presence in inventory)
                    foreach (uint totemCategory in m_spellInfo.TotemCategory)
                    {
                        if (totemCategory != 0 && player.HasItemTotemCategory(totemCategory))
                        {
                            param1 = (int)totemCategory;
                            return SpellCastResult.TotemCategory;
                        }
                    }
                }
            }

            // special checks for spell effects
            foreach (var spellEffectInfo in m_spellInfo.GetEffects())
            {
                switch (spellEffectInfo.Effect)
                {
                    case SpellEffectName.CreateItem:
                    case SpellEffectName.CreateLoot:
                    {
                        // m_targets.GetUnitTarget() means explicit cast, otherwise we dont check for possible equip error
                        Unit target = m_targets.GetUnitTarget() ?? player;
                        if (target.IsPlayer() && !IsTriggered())
                        {

                            // SPELL_EFFECT_CREATE_ITEM_2 differs from SPELL_EFFECT_CREATE_ITEM in that it picks the random item to create from a pool of potential items,
                            // so we need to make sure there is at least one free space in the player's inventory
                            if (spellEffectInfo.Effect == SpellEffectName.CreateLoot)
                            {
                                if (target.ToPlayer().GetFreeInventorySlotCount(ItemSearchLocation.Inventory) == 0)
                                {
                                    player.SendEquipError(InventoryResult.InvFull, null, null, spellEffectInfo.ItemType);
                                    return SpellCastResult.DontReport;
                                }
                            }

                            if (spellEffectInfo.ItemType != 0)
                            {
                                ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(spellEffectInfo.ItemType);
                                if (itemTemplate == null)
                                    return SpellCastResult.ItemNotFound;

                                uint createCount = (uint)Math.Clamp(spellEffectInfo.CalcValue(), 1u, itemTemplate.GetMaxStackSize());

                                List<ItemPosCount> dest = new();
                                InventoryResult msg = target.ToPlayer().CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, spellEffectInfo.ItemType, createCount);
                                if (msg != InventoryResult.Ok)
                                {
                                    /// @todo Needs review
                                    if (itemTemplate.GetItemLimitCategory() == 0)
                                    {
                                        player.SendEquipError(msg, null, null, spellEffectInfo.ItemType);
                                        return SpellCastResult.DontReport;
                                    }
                                    else
                                    {
                                        // Conjure Food/Water/Refreshment spells
                                        if (m_spellInfo.SpellFamilyName != SpellFamilyNames.Mage || (!m_spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x40000000u)))
                                            return SpellCastResult.TooManyOfItem;
                                        else if (!target.ToPlayer().HasItemCount(spellEffectInfo.ItemType))
                                        {
                                            player.SendEquipError(msg, null, null, spellEffectInfo.ItemType);
                                            return SpellCastResult.DontReport;
                                        }
                                        else if (m_spellInfo.GetEffects().Count > 1)
                                            player.CastSpell(player, (uint)m_spellInfo.GetEffect(1).CalcValue(), new CastSpellExtraArgs()
                                                .SetTriggeringSpell(this));        // move this to anywhere
                                        return SpellCastResult.DontReport;
                                    }
                                }
                            }
                        }
                        break;
                    }
                    case SpellEffectName.EnchantItem:
                        if (spellEffectInfo.ItemType != 0 && m_targets.GetItemTarget() != null && m_targets.GetItemTarget().IsVellum())
                        {
                            // cannot enchant vellum for other player
                            if (m_targets.GetItemTarget().GetOwner() != player)
                                return SpellCastResult.NotTradeable;
                            // do not allow to enchant vellum from scroll made by vellum-prevent exploit
                            if (m_CastItem != null && m_CastItem.GetTemplate().HasFlag(ItemFlags.NoReagentCost))
                                return SpellCastResult.TotemCategory;
                            List<ItemPosCount> dest = new();
                            InventoryResult msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, spellEffectInfo.ItemType, 1);
                            if (msg != InventoryResult.Ok)
                            {
                                player.SendEquipError(msg, null, null, spellEffectInfo.ItemType);
                                return SpellCastResult.DontReport;
                            }
                        }
                        goto case SpellEffectName.EnchantItemPrismatic;
                    case SpellEffectName.EnchantItemPrismatic:
                    {
                        Item targetItem = m_targets.GetItemTarget();
                        if (targetItem == null)
                            return SpellCastResult.ItemNotFound;

                        // Apply item level restriction
                        if (!m_spellInfo.HasAttribute(SpellAttr2.AllowLowLevelBuff))
                        {
                            uint requiredLevel = (uint)targetItem.GetRequiredLevel();
                            if (requiredLevel == 0)
                                requiredLevel = targetItem.GetItemLevel(targetItem.GetOwner());

                            if (requiredLevel < m_spellInfo.BaseLevel)
                                return SpellCastResult.Lowlevel;
                        }
                        if ((m_CastItem != null || effectInfo.IsEffect(SpellEffectName.EnchantItemPrismatic))
                            && m_spellInfo.MaxLevel > 0 && targetItem.GetItemLevel(targetItem.GetOwner()) > m_spellInfo.MaxLevel)
                            return SpellCastResult.Highlevel;

                        bool isItemUsable = false;
                        foreach (ItemEffectRecord itemEffect in targetItem.GetEffects())
                        {
                            if (itemEffect.SpellID != 0 && itemEffect.TriggerType == ItemSpelltriggerType.OnUse)
                            {
                                isItemUsable = true;
                                break;
                            }
                        }

                        var enchantEntry = CliDB.SpellItemEnchantmentStorage.LookupByKey(spellEffectInfo.MiscValue);
                        // do not allow adding usable enchantments to items that have use effect already
                        if (enchantEntry != null)
                        {
                            for (var s = 0; s < ItemConst.MaxItemEnchantmentEffects; ++s)
                            {
                                switch (enchantEntry.Effect[s])
                                {
                                    case ItemEnchantmentType.UseSpell:
                                        if (isItemUsable)
                                            return SpellCastResult.OnUseEnchant;
                                        break;
                                    case ItemEnchantmentType.PrismaticSocket:
                                    {
                                        uint numSockets = 0;
                                        for (uint socket = 0; socket < ItemConst.MaxGemSockets; ++socket)
                                            if (targetItem.GetSocketColor(socket) != 0)
                                                ++numSockets;

                                        if (numSockets == ItemConst.MaxGemSockets || targetItem.GetEnchantmentId(EnchantmentSlot.Prismatic) != 0)
                                            return SpellCastResult.MaxSockets;
                                        break;
                                    }
                                }
                            }
                        }

                        // Not allow enchant in trade slot for some enchant type
                        if (targetItem.GetOwner() != player)
                        {
                            if (enchantEntry == null)
                                return SpellCastResult.Error;
                            if (enchantEntry.HasFlag(SpellItemEnchantmentFlags.Soulbound))
                                return SpellCastResult.NotTradeable;
                        }
                        break;
                    }
                    case SpellEffectName.EnchantItemTemporary:
                    {
                        Item item = m_targets.GetItemTarget();
                        if (item == null)
                            return SpellCastResult.ItemNotFound;
                        // Not allow enchant in trade slot for some enchant type
                        if (item.GetOwner() != player)
                        {
                            int enchant_id = spellEffectInfo.MiscValue;
                            var enchantEntry = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);
                            if (enchantEntry == null)
                                return SpellCastResult.Error;
                            if (enchantEntry.HasFlag(SpellItemEnchantmentFlags.Soulbound))
                                return SpellCastResult.NotTradeable;
                        }

                        // Apply item level restriction
                        if (!m_spellInfo.HasAttribute(SpellAttr2.AllowLowLevelBuff))
                        {
                            uint requiredLevel = (uint)item.GetRequiredLevel();
                            if (requiredLevel == 0)
                                requiredLevel = item.GetItemLevel(item.GetOwner());

                            if (requiredLevel < m_spellInfo.BaseLevel)
                                return SpellCastResult.Lowlevel;
                        }
                        if (m_CastItem != null && m_spellInfo.MaxLevel > 0 && item.GetItemLevel(item.GetOwner()) > m_spellInfo.MaxLevel)
                            return SpellCastResult.Highlevel;
                        break;
                    }
                    case SpellEffectName.EnchantHeldItem:
                        // check item existence in effect code (not output errors at offhand hold item effect to main hand for example
                        break;
                    case SpellEffectName.Disenchant:
                    {
                        Item item = m_targets.GetItemTarget();
                        if (item == null)
                            return SpellCastResult.CantBeSalvaged;

                        // prevent disenchanting in trade slot
                        if (item.GetOwnerGUID() != player.GetGUID())
                            return SpellCastResult.CantBeSalvaged;

                        ItemTemplate itemProto = item.GetTemplate();
                        if (itemProto == null)
                            return SpellCastResult.CantBeSalvaged;

                        ushort? disenchantSkillRequired = item.GetDisenchantSkillRequired();
                        if (!disenchantSkillRequired.HasValue)
                            return SpellCastResult.CantBeSalvaged;
                        if (disenchantSkillRequired > player.GetSkillValue(SkillType.Enchanting))
                            return SpellCastResult.CantBeSalvagedSkill;
                        break;
                    }
                    case SpellEffectName.Prospecting:
                    {
                        Item item = m_targets.GetItemTarget();
                        if (item == null)
                            return SpellCastResult.CantBeProspected;
                        //ensure item is a prospectable ore
                        if (!item.GetTemplate().HasFlag(ItemFlags.IsProspectable))
                            return SpellCastResult.CantBeProspected;
                        //prevent prospecting in trade slot
                        if (item.GetOwnerGUID() != player.GetGUID())
                            return SpellCastResult.CantBeProspected;
                        //Check for enough skill in jewelcrafting
                        uint item_prospectingskilllevel = item.GetTemplate().GetRequiredSkillRank();
                        if (item_prospectingskilllevel > player.GetSkillValue(SkillType.Jewelcrafting))
                            return SpellCastResult.LowCastlevel;
                        //make sure the player has the required ores in inventory
                        if (item.GetCount() < 5)
                        {
                            param1 = (int)item.GetEntry();
                            param2 = 5;
                            return SpellCastResult.NeedMoreItems;
                        }

                        if (!LootStorage.Prospecting.HaveLootFor(m_targets.GetItemTargetEntry()))
                            return SpellCastResult.CantBeProspected;

                        break;
                    }
                    case SpellEffectName.Milling:
                    {
                        Item item = m_targets.GetItemTarget();
                        if (item == null)
                            return SpellCastResult.CantBeMilled;
                        //ensure item is a millable herb
                        if (!item.GetTemplate().HasFlag(ItemFlags.IsMillable))
                            return SpellCastResult.CantBeMilled;
                        //prevent milling in trade slot
                        if (item.GetOwnerGUID() != player.GetGUID())
                            return SpellCastResult.CantBeMilled;
                        //Check for enough skill in inscription
                        uint item_millingskilllevel = item.GetTemplate().GetRequiredSkillRank();
                        if (item_millingskilllevel > player.GetSkillValue(SkillType.Inscription))
                            return SpellCastResult.LowCastlevel;
                        //make sure the player has the required herbs in inventory
                        if (item.GetCount() < 5)
                        {
                            param1 = (int)item.GetEntry();
                            param2 = 5;
                            return SpellCastResult.NeedMoreItems;
                        }

                        if (!LootStorage.Milling.HaveLootFor(m_targets.GetItemTargetEntry()))
                            return SpellCastResult.CantBeMilled;

                        break;
                    }
                    case SpellEffectName.WeaponDamage:
                    case SpellEffectName.WeaponDamageNoSchool:
                    {
                        if (m_attackType != WeaponAttackType.RangedAttack)
                            break;

                        Item item = player.GetWeaponForAttack(m_attackType);
                        if (item == null || item.IsBroken())
                            return SpellCastResult.EquippedItem;

                        switch ((ItemSubClassWeapon)item.GetTemplate().GetSubClass())
                        {
                            case ItemSubClassWeapon.Thrown:
                            {
                                uint ammo = item.GetEntry();
                                if (!player.HasItemCount(ammo))
                                    return SpellCastResult.NoAmmo;
                                break;
                            }
                            case ItemSubClassWeapon.Gun:
                            case ItemSubClassWeapon.Bow:
                            case ItemSubClassWeapon.Crossbow:
                            case ItemSubClassWeapon.Wand:
                                break;
                            default:
                                break;
                        }
                        break;
                    }
                    case SpellEffectName.RechargeItem:
                    {
                        uint itemId = spellEffectInfo.ItemType;

                        ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(itemId);
                        if (proto == null)
                            return SpellCastResult.ItemAtMaxCharges;

                        Item item = player.GetItemByEntry(itemId);
                        if (item != null)
                        {
                            foreach (ItemEffectRecord itemEffect in item.GetEffects())
                                if (itemEffect.LegacySlotIndex <= item.m_itemData.SpellCharges.GetSize() && itemEffect.Charges != 0 && item.GetSpellCharges(itemEffect.LegacySlotIndex) == itemEffect.Charges)
                                    return SpellCastResult.ItemAtMaxCharges;
                        }
                        break;
                    }
                    case SpellEffectName.RespecAzeriteEmpoweredItem:
                    {
                        Item item = m_targets.GetItemTarget();
                        if (item == null)
                            return SpellCastResult.AzeriteEmpoweredOnly;

                        if (item.GetOwnerGUID() != m_caster.GetGUID())
                            return SpellCastResult.DontReport;

                        AzeriteEmpoweredItem azeriteEmpoweredItem = item.ToAzeriteEmpoweredItem();
                        if (azeriteEmpoweredItem == null)
                            return SpellCastResult.AzeriteEmpoweredOnly;

                        bool hasSelections = false;
                        for (int tier = 0; tier < SharedConst.MaxAzeriteEmpoweredTier; ++tier)
                        {
                            if (azeriteEmpoweredItem.GetSelectedAzeritePower(tier) != 0)
                            {
                                hasSelections = true;
                                break;
                            }
                        }

                        if (!hasSelections)
                            return SpellCastResult.AzeriteEmpoweredNoChoicesToUndo;

                        if (!m_caster.ToPlayer().HasEnoughMoney(azeriteEmpoweredItem.GetRespecCost()))
                            return SpellCastResult.DontReport;

                        break;
                    }
                    default:
                        break;
                }
            }

            // check weapon presence in slots for main/offhand weapons
            if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreEquippedItemRequirement) && m_spellInfo.EquippedItemClass >= 0)
            {
                var weaponCheck = new Func<WeaponAttackType, SpellCastResult>(attackType =>
                {
                    Item item = player.ToPlayer().GetWeaponForAttack(attackType);

                    // skip spell if no weapon in slot or broken
                    if (item == null || item.IsBroken())
                        return SpellCastResult.EquippedItemClass;

                    // skip spell if weapon not fit to triggered spell
                    if (!item.IsFitToSpellRequirements(m_spellInfo))
                        return SpellCastResult.EquippedItemClass;

                    return SpellCastResult.SpellCastOk;
                });

                // main hand weapon required
                if (m_spellInfo.HasAttribute(SpellAttr3.RequiresMainHandWeapon))
                {
                    SpellCastResult mainHandResult = weaponCheck(WeaponAttackType.BaseAttack);
                    if (mainHandResult != SpellCastResult.SpellCastOk)
                        return mainHandResult;
                }

                // offhand hand weapon required
                if (m_spellInfo.HasAttribute(SpellAttr3.RequiresOffHandWeapon))
                {
                    SpellCastResult offHandResult = weaponCheck(WeaponAttackType.OffAttack);
                    if (offHandResult != SpellCastResult.SpellCastOk)
                        return offHandResult;
                }
            }

            return SpellCastResult.SpellCastOk;
        }

        public void Delayed() // only called in DealDamage()
        {
            Unit unitCaster = m_caster.ToUnit();
            if (unitCaster == null)
                return;

            if (IsDelayableNoMore())                                 // Spells may only be delayed twice
                return;

            //check pushback reduce
            int delaytime = 500;                                  // spellcasting delay is normally 500ms
            int delayReduce = 100;                                // must be initialized to 100 for percent modifiers

            Player player = unitCaster.GetSpellModOwner();
            if (player != null)
                player.ApplySpellMod(m_spellInfo, SpellModOp.ResistPushback, ref delayReduce, this);

            delayReduce += unitCaster.GetTotalAuraModifier(AuraType.ReducePushback) - 100;
            if (delayReduce >= 100)
                return;

            MathFunctions.AddPct(ref delaytime, -delayReduce);

            if (m_timer + delaytime > m_casttime)
            {
                delaytime = m_casttime - m_timer;
                m_timer = m_casttime;
            }
            else
                m_timer += delaytime;

            SpellDelayed spellDelayed = new();
            spellDelayed.Caster = unitCaster.GetGUID();
            spellDelayed.ActualDelay = delaytime;

            unitCaster.SendMessageToSet(spellDelayed, true);
        }

        public void DelayedChannel()
        {
            Unit unitCaster = m_caster.ToUnit();
            if (unitCaster == null)
                return;

            if (m_spellState != SpellState.Casting)
                return;

            if (IsDelayableNoMore())                                    // Spells may only be delayed twice
                return;

            //check pushback reduce
            // should be affected by modifiers, not take the dbc duration.
            int duration = ((m_channeledDuration > 0) ? m_channeledDuration : m_spellInfo.GetDuration());

            int delaytime = MathFunctions.CalculatePct(duration, 25); // channeling delay is normally 25% of its time per hit
            int delayReduce = 100;                                    // must be initialized to 100 for percent modifiers

            Player player = unitCaster.GetSpellModOwner();
            if (player != null)
                player.ApplySpellMod(m_spellInfo, SpellModOp.ResistPushback, ref delayReduce, this);

            delayReduce += unitCaster.GetTotalAuraModifier(AuraType.ReducePushback) - 100;
            if (delayReduce >= 100)
                return;

            MathFunctions.AddPct(ref delaytime, -delayReduce);

            if (m_timer <= delaytime)
            {
                delaytime = m_timer;
                m_timer = 0;
            }
            else
                m_timer -= delaytime;

            foreach (var ihit in m_UniqueTargetInfo)
            {
                if (ihit.MissCondition == SpellMissInfo.None)
                {
                    Unit unit = (unitCaster.GetGUID() == ihit.TargetGUID) ? unitCaster : Global.ObjAccessor.GetUnit(unitCaster, ihit.TargetGUID);
                    if (unit != null)
                        unit.DelayOwnedAuras(m_spellInfo.Id, m_originalCasterGUID, delaytime);
                }
            }

            // partially interrupt persistent area auras
            DynamicObject dynObj = unitCaster.GetDynObject(m_spellInfo.Id);
            if (dynObj != null)
                dynObj.Delay(delaytime);

            SendChannelUpdate((uint)m_timer);
        }

        public bool HasPowerTypeCost(PowerType power)
        {
            return GetPowerTypeCostAmount(power).HasValue;
        }

        public int? GetPowerTypeCostAmount(PowerType power)
        {
            var powerCost = m_powerCost.Find(cost => cost.Power == power);
            if (powerCost == null)
                return null;

            return powerCost.Amount;
        }

        bool UpdatePointers()
        {
            if (m_originalCasterGUID == m_caster.GetGUID())
                m_originalCaster = m_caster.ToUnit();
            else
            {
                m_originalCaster = Global.ObjAccessor.GetUnit(m_caster, m_originalCasterGUID);
                if (m_originalCaster != null && !m_originalCaster.IsInWorld)
                    m_originalCaster = null;
            }

            if (!m_castItemGUID.IsEmpty() && m_caster.IsTypeId(TypeId.Player))
            {
                m_CastItem = m_caster.ToPlayer().GetItemByGuid(m_castItemGUID);
                m_castItemLevel = -1;
                // cast item not found, somehow the item is no longer where we expected
                if (m_CastItem == null)
                    return false;

                // check if the item is really the same, in case it has been wrapped for example
                if (m_castItemEntry != m_CastItem.GetEntry())
                    return false;

                m_castItemLevel = (int)m_CastItem.GetItemLevel(m_caster.ToPlayer());
            }

            m_targets.Update(m_caster);

            // further actions done only for dest targets
            if (!m_targets.HasDst())
                return true;

            // cache last transport
            WorldObject transport = null;

            // update effect destinations (in case of moved transport dest target)
            foreach (var spellEffectInfo in m_spellInfo.GetEffects())
            {
                SpellDestination dest = m_destTargets[spellEffectInfo.EffectIndex];
                if (dest.TransportGUID.IsEmpty())
                    continue;

                if (transport == null || transport.GetGUID() != dest.TransportGUID)
                    transport = Global.ObjAccessor.GetWorldObject(m_caster, dest.TransportGUID);

                if (transport != null)
                {
                    dest.Position.Relocate(transport.GetPosition());
                    dest.Position.RelocateOffset(dest.TransportOffset);
                }
            }

            return true;
        }

        public CurrentSpellTypes GetCurrentContainer()
        {
            if (m_spellInfo.IsNextMeleeSwingSpell())
                return CurrentSpellTypes.Melee;
            else if (IsAutoRepeat())
                return CurrentSpellTypes.AutoRepeat;
            else if (m_spellInfo.IsChanneled())
                return CurrentSpellTypes.Channeled;

            return CurrentSpellTypes.Generic;
        }

        public Difficulty GetCastDifficulty()
        {
            return m_caster.GetMap().GetDifficultyID();
        }

        bool CheckEffectTarget(Unit target, SpellEffectInfo spellEffectInfo, Position losPosition)
        {
            if (spellEffectInfo == null || !spellEffectInfo.IsEffect())
                return false;

            switch (spellEffectInfo.ApplyAuraName)
            {
                case AuraType.ModPossess:
                case AuraType.ModCharm:
                case AuraType.ModPossessPet:
                case AuraType.AoeCharm:
                    if (target.GetVehicleKit() != null && target.GetVehicleKit().IsControllableVehicle())
                        return false;
                    if (target.IsMounted())
                        return false;
                    if (!target.GetCharmerGUID().IsEmpty())
                        return false;
                    int damage = CalculateDamage(spellEffectInfo, target);
                    if (damage != 0)
                        if (target.GetLevelForTarget(m_caster) > damage)
                            return false;
                    break;
                default:
                    break;
            }

            // check for ignore LOS on the effect itself
            if (m_spellInfo.HasAttribute(SpellAttr2.IgnoreLineOfSight) || Global.DisableMgr.IsDisabledFor(DisableType.Spell, m_spellInfo.Id, null, DisableFlags.SpellLOS))
                return true;

            // check if gameobject ignores LOS
            GameObject gobCaster = m_caster.ToGameObject();
            if (gobCaster != null)
                if (gobCaster.GetGoInfo().GetRequireLOS() == 0)
                    return true;

            // if spell is triggered, need to check for LOS disable on the aura triggering it and inherit that behaviour
            if (!m_spellInfo.HasAttribute(SpellAttr5.AlwaysLineOfSight) && IsTriggered() && m_triggeredByAuraSpell != null && (m_triggeredByAuraSpell.HasAttribute(SpellAttr2.IgnoreLineOfSight) || Global.DisableMgr.IsDisabledFor(DisableType.Spell, m_triggeredByAuraSpell.Id, null, DisableFlags.SpellLOS)))
                return true;

            // @todo shit below shouldn't be here, but it's temporary
            //Check targets for LOS visibility
            switch (spellEffectInfo.Effect)
            {
                case SpellEffectName.SkinPlayerCorpse:
                {
                    if (m_targets.GetCorpseTargetGUID().IsEmpty())
                    {
                        if (IsWithinLOS(m_caster, target, true, ModelIgnoreFlags.M2) && target.HasUnitFlag(UnitFlags.Skinnable))
                            return true;

                        return false;
                    }

                    Corpse corpse = ObjectAccessor.GetCorpse(m_caster, m_targets.GetCorpseTargetGUID());
                    if (corpse == null)
                        return false;

                    if (target.GetGUID() != corpse.GetOwnerGUID())
                        return false;

                    if (!corpse.HasCorpseDynamicFlag(CorpseDynFlags.Lootable))
                        return false;

                    if (!IsWithinLOS(m_caster, corpse, true, ModelIgnoreFlags.M2))
                        return false;

                    break;
                }
                default:
                {
                    if (losPosition == null || m_spellInfo.HasAttribute(SpellAttr5.AlwaysAoeLineOfSight) || spellEffectInfo.EffectAttributes.HasFlag(SpellEffectAttributes.AlwaysAoeLineOfSight))
                    {
                        // Get GO cast coordinates if original caster . GO
                        WorldObject caster = null;
                        if (m_originalCasterGUID.IsGameObject())
                            caster = m_caster.GetMap().GetGameObject(m_originalCasterGUID);
                        if (caster == null)
                            caster = m_caster;

                        if (target != m_caster && !IsWithinLOS(caster, target, true, ModelIgnoreFlags.M2))
                            return false;
                    }

                    if (losPosition != null)
                        if (!target.IsWithinLOS(losPosition.GetPositionX(), losPosition.GetPositionY(), losPosition.GetPositionZ(), LineOfSightChecks.All, ModelIgnoreFlags.M2))
                            return false;

                    break;
                }
            }

            return true;
        }

        bool CheckEffectTarget(GameObject target, SpellEffectInfo spellEffectInfo)
        {
            if (spellEffectInfo == null || !spellEffectInfo.IsEffect())
                return false;

            switch (spellEffectInfo.Effect)
            {
                case SpellEffectName.GameObjectDamage:
                case SpellEffectName.GameobjectRepair:
                case SpellEffectName.GameobjectSetDestructionState:
                    if (target.GetGoType() != GameObjectTypes.DestructibleBuilding)
                        return false;
                    break;
                default:
                    break;
            }

            return true;
        }

        bool CheckEffectTarget(Item target, SpellEffectInfo spellEffectInfo)
        {
            if (spellEffectInfo == null || !spellEffectInfo.IsEffect())
                return false;

            return true;
        }

        bool IsAutoActionResetSpell()
        {
            if (IsTriggered() || m_spellInfo.HasAttribute(SpellAttr2.DoNotResetCombatTimers))
                return false;

            if (m_casttime == 0 && m_spellInfo.HasAttribute(SpellAttr6.DoesntResetSwingTimerIfInstant))
                return false;

            return true;
        }

        public bool IsPositive()
        {
            return m_spellInfo.IsPositive() && (m_triggeredByAuraSpell == null || m_triggeredByAuraSpell.IsPositive());
        }

        public bool IsEmpowerSpell() { return m_empower != null; }

        public void SetEmpowerReleasedByClient(bool release)
        {
            m_empower.IsReleasedByClient = release;
        }

        public bool CanReleaseEmpowerSpell()
        {
            if (m_empower.IsReleased)
                return false;

            if (!m_empower.IsReleasedByClient && m_timer != 0)
                return false;

            TimeSpan passedTime = TimeSpan.FromMilliseconds(m_channeledDuration - m_timer);
            return passedTime >= m_empower.MinHoldTime;
        }

        bool IsNeedSendToClient()
        {
            return m_SpellVisual.SpellXSpellVisualID != 0 || m_SpellVisual.ScriptVisualID != 0 || m_spellInfo.IsChanneled() ||
                (m_spellInfo.HasAttribute(SpellAttr8.AuraPointsOnClient)) || m_spellInfo.HasHitDelay() || (m_triggeredByAuraSpell == null && !IsTriggered()) ||
                m_spellInfo.HasAttribute(SpellAttr7.AlwaysCastLog);
        }

        public Unit GetUnitCasterForEffectHandlers()
        {
            return m_originalCaster != null ? m_originalCaster : m_caster.ToUnit();
        }

        bool IsValidDeadOrAliveTarget(Unit target)
        {
            if (target.IsAlive())
                return !m_spellInfo.IsRequiringDeadTarget();
            if (m_spellInfo.IsAllowingDeadTarget())
                return true;
            return false;
        }

        void HandleLaunchPhase()
        {
            // handle effects with SPELL_EFFECT_HANDLE_LAUNCH mode
            foreach (var spellEffectInfo in m_spellInfo.GetEffects())
            {
                // don't do anything for empty effect
                if (!spellEffectInfo.IsEffect())
                    continue;

                HandleEffects(null, null, null, null, spellEffectInfo, SpellEffectHandleMode.Launch);
            }

            PrepareTargetProcessing();

            foreach (TargetInfo target in m_UniqueTargetInfo)
                PreprocessSpellLaunch(target);

            foreach (var spellEffectInfo in m_spellInfo.GetEffects())
            {
                float multiplier = 1.0f;
                if ((m_applyMultiplierMask & (1 << (int)spellEffectInfo.EffectIndex)) != 0)
                    multiplier = spellEffectInfo.CalcDamageMultiplier(m_originalCaster, this);

                foreach (TargetInfo target in m_UniqueTargetInfo)
                {
                    uint mask = target.EffectMask;
                    if ((mask & (1 << (int)spellEffectInfo.EffectIndex)) == 0)
                        continue;

                    DoEffectOnLaunchTarget(target, multiplier, spellEffectInfo);
                }
            }

            FinishTargetProcessing();
        }

        void PreprocessSpellLaunch(TargetInfo targetInfo)
        {
            Unit targetUnit = m_caster.GetGUID() == targetInfo.TargetGUID ? m_caster.ToUnit() : Global.ObjAccessor.GetUnit(m_caster, targetInfo.TargetGUID);
            if (targetUnit == null)
                return;

            // This will only cause combat - the target will engage once the projectile hits (in Spell::TargetInfo::PreprocessTarget)
            if (m_originalCaster != null && targetInfo.MissCondition != SpellMissInfo.Evade && !m_originalCaster.IsFriendlyTo(targetUnit) && (!m_spellInfo.IsPositive() || m_spellInfo.HasEffect(SpellEffectName.Dispel)) && (m_spellInfo.HasInitialAggro() || targetUnit.IsEngaged()))
                m_originalCaster.SetInCombatWith(targetUnit, true);

            Unit unit = null;
            // In case spell hit target, do all effect on that target
            if (targetInfo.MissCondition == SpellMissInfo.None)
                unit = targetUnit;
            // In case spell reflect from target, do all effect on caster (if hit)
            else if (targetInfo.MissCondition == SpellMissInfo.Reflect && targetInfo.ReflectResult == SpellMissInfo.None)
                unit = m_caster.ToUnit();
            if (unit == null)
                return;

            float critChance = m_spellValue.CriticalChance;
            if (m_originalCaster != null)
            {
                if (critChance == 0)
                    critChance = m_originalCaster.SpellCritChanceDone(this, null, m_spellSchoolMask, m_attackType);
                critChance = unit.SpellCritChanceTaken(m_originalCaster, this, null, m_spellSchoolMask, critChance, m_attackType);
            }

            targetInfo.IsCrit = RandomHelper.randChance(critChance);
        }

        void DoEffectOnLaunchTarget(TargetInfo targetInfo, float multiplier, SpellEffectInfo spellEffectInfo)
        {
            Unit unit = null;
            // In case spell hit target, do all effect on that target
            if (targetInfo.MissCondition == SpellMissInfo.None || (targetInfo.MissCondition == SpellMissInfo.Block && !m_spellInfo.HasAttribute(SpellAttr3.CompletelyBlocked)))
                unit = m_caster.GetGUID() == targetInfo.TargetGUID ? m_caster.ToUnit() : Global.ObjAccessor.GetUnit(m_caster, targetInfo.TargetGUID);
            // In case spell reflect from target, do all effect on caster (if hit)
            else if (targetInfo.MissCondition == SpellMissInfo.Reflect && targetInfo.ReflectResult == SpellMissInfo.None)
                unit = m_caster.ToUnit();

            if (unit == null)
                return;

            m_damage = 0;
            m_healing = 0;

            HandleEffects(unit, null, null, null, spellEffectInfo, SpellEffectHandleMode.LaunchTarget);

            if (m_originalCaster != null && m_damage > 0)
            {
                bool isAoeTarget = spellEffectInfo.IsTargetingArea() || spellEffectInfo.IsAreaAuraEffect() || spellEffectInfo.IsEffect(SpellEffectName.PersistentAreaAura);
                if (isAoeTarget || m_spellInfo.HasAttribute(SpellAttr5.TreatAsAreaEffect) || m_spellInfo.HasAttribute(SpellAttr7.TreatAsNpcAoe))
                {
                    m_damage = unit.CalculateAOEAvoidance(m_damage, (uint)m_spellInfo.SchoolMask, !m_originalCaster.IsControlledByPlayer() || m_spellInfo.HasAttribute(SpellAttr7.TreatAsNpcAoe));

                    if (m_originalCaster.IsPlayer())
                    {
                        long targetCount = !isAoeTarget && m_spellValue.ParentSpellTargetCount.HasValue ? m_spellValue.ParentSpellTargetCount.Value : GetUnitTargetCountForEffect(spellEffectInfo.EffectIndex);
                        int targetIndex = !isAoeTarget && m_spellValue.ParentSpellTargetIndex.HasValue ? m_spellValue.ParentSpellTargetIndex.Value : GetUnitTargetIndexForEffect(targetInfo.TargetGUID, spellEffectInfo.EffectIndex);

                        // sqrt target cap damage calculation
                        if (m_spellInfo.SqrtDamageAndHealingDiminishing.MaxTargets != 0
                            && targetCount > m_spellInfo.SqrtDamageAndHealingDiminishing.MaxTargets
                            && targetIndex >= m_spellInfo.SqrtDamageAndHealingDiminishing.NumNonDiminishedTargets)
                            m_damage = (int)(m_damage * Math.Sqrt((float)m_spellInfo.SqrtDamageAndHealingDiminishing.MaxTargets / Math.Min(SpellConst.AoeDamageTargetCap, targetCount)));
                        if (targetCount > SpellConst.AoeDamageTargetCap)
                            m_damage = (int)(m_damage * SpellConst.AoeDamageTargetCap / targetCount);
                    }
                }
            }

            if (m_originalCaster != null && m_healing > 0)
            {
                bool isAoeTarget = spellEffectInfo.IsTargetingArea() || spellEffectInfo.IsAreaAuraEffect() || spellEffectInfo.IsEffect(SpellEffectName.PersistentAreaAura);
                if (isAoeTarget || m_spellInfo.HasAttribute(SpellAttr5.TreatAsAreaEffect))
                {
                    if (m_originalCaster.IsPlayer())
                    {
                        long targetCount = !isAoeTarget && m_spellValue.ParentSpellTargetCount.HasValue ? m_spellValue.ParentSpellTargetCount.Value : GetUnitTargetCountForEffect(spellEffectInfo.EffectIndex);
                        int targetIndex = !isAoeTarget && m_spellValue.ParentSpellTargetIndex.HasValue ? m_spellValue.ParentSpellTargetIndex.Value : GetUnitTargetIndexForEffect(targetInfo.TargetGUID, spellEffectInfo.EffectIndex);

                        // sqrt target cap healing calculation
                        if (m_spellInfo.SqrtDamageAndHealingDiminishing.MaxTargets != 0
                            && targetCount > m_spellInfo.SqrtDamageAndHealingDiminishing.MaxTargets
                            && targetIndex >= m_spellInfo.SqrtDamageAndHealingDiminishing.NumNonDiminishedTargets)
                            m_healing = (int)(m_healing * Math.Sqrt((float)m_spellInfo.SqrtDamageAndHealingDiminishing.MaxTargets / Math.Min(SpellConst.AoeDamageTargetCap, targetCount)));
                    }

                }
            }

            if ((m_applyMultiplierMask & (1 << (int)spellEffectInfo.EffectIndex)) != 0)
            {
                m_damage = (int)(m_damage * m_damageMultipliers[spellEffectInfo.EffectIndex]);
                m_healing = (int)(m_healing * m_damageMultipliers[spellEffectInfo.EffectIndex]);

                m_damageMultipliers[spellEffectInfo.EffectIndex] *= multiplier;
            }

            targetInfo.Damage += m_damage;
            targetInfo.Healing += m_healing;
        }

        void ResetCombatTimers()
        {
            Unit unitCaster = m_caster.ToUnit();
            if (unitCaster == null)
                return;

            unitCaster.ResetAttackTimer(WeaponAttackType.BaseAttack);
            if (unitCaster.HaveOffhandWeapon())
                unitCaster.ResetAttackTimer(WeaponAttackType.OffAttack);
            unitCaster.ResetAttackTimer(WeaponAttackType.RangedAttack);
        }

        SpellCastResult CanOpenLock(SpellEffectInfo effect, uint lockId, ref SkillType skillId, ref int reqSkillValue, ref int skillValue)
        {
            if (lockId == 0)                                             // possible case for GO and maybe for items.
                return SpellCastResult.SpellCastOk;

            Unit unitCaster = m_caster.ToUnit();
            if (unitCaster == null)
                return SpellCastResult.BadTargets;

            // Get LockInfo
            var lockInfo = CliDB.LockStorage.LookupByKey(lockId);

            if (lockInfo == null)
                return SpellCastResult.BadTargets;

            bool reqKey = false;                                    // some locks not have reqs

            for (int j = 0; j < SharedConst.MaxLockCase; ++j)
            {
                switch ((LockKeyType)lockInfo.LockType[j])
                {
                    // check key item (many fit cases can be)
                    case LockKeyType.Item:
                        if (lockInfo.Index[j] != 0 && m_CastItem != null && m_CastItem.GetEntry() == lockInfo.Index[j])
                            return SpellCastResult.SpellCastOk;
                        reqKey = true;
                        break;
                    // check key skill (only single first fit case can be)
                    case LockKeyType.Skill:
                    {
                        reqKey = true;

                        // wrong locktype, skip
                        if (effect.MiscValue != lockInfo.Index[j])
                            continue;

                        skillId = SharedConst.SkillByLockType((LockType)lockInfo.Index[j]);

                        if (skillId != SkillType.None || lockInfo.Index[j] == (uint)LockType.Lockpicking)
                        {
                            reqSkillValue = lockInfo.Skill[j];

                            // castitem check: rogue using skeleton keys. the skill values should not be added in this case.
                            skillValue = 0;
                            if (m_CastItem == null && unitCaster.IsTypeId(TypeId.Player))
                                skillValue = unitCaster.ToPlayer().GetSkillValue(skillId);
                            else if (lockInfo.Index[j] == (uint)LockType.Lockpicking)
                                skillValue = (int)unitCaster.GetLevel() * 5;

                            // skill bonus provided by casting spell (mostly item spells)
                            // add the effect base points modifier from the spell cast (cheat lock / skeleton key etc.)
                            if (effect.TargetA.GetTarget() == Targets.GameobjectItemTarget || effect.TargetB.GetTarget() == Targets.GameobjectItemTarget)
                                skillValue += effect.CalcValue();

                            if (skillValue < reqSkillValue)
                                return SpellCastResult.LowCastlevel;
                        }

                        return SpellCastResult.SpellCastOk;
                    }
                    case LockKeyType.Spell:
                        if (m_spellInfo.Id == lockInfo.Index[j])
                            return SpellCastResult.SpellCastOk;
                        reqKey = true;
                        break;
                }
            }

            if (reqKey)
                return SpellCastResult.BadTargets;

            return SpellCastResult.SpellCastOk;
        }

        public void SetSpellValue(SpellValueMod mod, int value)
        {
            if (mod < SpellValueMod.IntEnd)
            {
                m_spellValue.EffectBasePoints[(int)mod] = value;
                m_spellValue.CustomBasePointsMask |= 1u << (int)mod;
                return;
            }

            switch (mod)
            {
                case SpellValueMod.MaxTargets:
                    m_spellValue.MaxAffectedTargets = (uint)value;
                    break;
                case SpellValueMod.AuraStack:
                    m_spellValue.AuraStackAmount = value;
                    break;
                case SpellValueMod.Duration:
                    m_spellValue.Duration = value;
                    break;
                case SpellValueMod.ParentSpellTargetCount:
                    m_spellValue.ParentSpellTargetCount = value;
                    break;
                case SpellValueMod.ParentSpellTargetIndex:
                    m_spellValue.ParentSpellTargetIndex = value;
                    break;
            }
        }

        public void SetSpellValue(SpellValueModFloat mod, float value)
        {
            switch (mod)
            {
                case SpellValueModFloat.RadiusMod:
                    m_spellValue.RadiusMod = value;
                    break;
                case SpellValueModFloat.CritChance:
                    m_spellValue.CriticalChance = value;
                    break;
                case SpellValueModFloat.DurationPct:
                    m_spellValue.DurationMul = value / 100.0f;
                    break;
                default:
                    break;
            }
        }

        void PrepareTargetProcessing()
        {
        }

        void FinishTargetProcessing()
        {
            SendSpellExecuteLog();
        }

        void LoadScripts()
        {
            m_loadedScripts = Global.ScriptMgr.CreateSpellScripts(m_spellInfo.Id, this);

            foreach (var script in m_loadedScripts)
            {
                Log.outDebug(LogFilter.Spells, "Spell.LoadScripts: Script `{0}` for spell `{1}` is loaded now", script.GetScriptName(), m_spellInfo.Id);
                script.Register();
            }
        }

        void CallScriptOnPrecastHandler()
        {
            foreach (var script in m_loadedScripts)
            {
                script._PrepareScriptCall(SpellScriptHookType.OnPrecast);
                script.OnPrecast();
                script._FinishScriptCall();
            }
        }

        void CallScriptBeforeCastHandlers()
        {
            foreach (var script in m_loadedScripts)
            {
                script._PrepareScriptCall(SpellScriptHookType.BeforeCast);

                foreach (var hook in script.BeforeCast)
                    hook.Call();

                script._FinishScriptCall();
            }
        }

        void CallScriptOnCastHandlers()
        {
            foreach (var script in m_loadedScripts)
            {
                script._PrepareScriptCall(SpellScriptHookType.OnCast);

                foreach (var hook in script.OnCast)
                    hook.Call();

                script._FinishScriptCall();
            }
        }

        void CallScriptAfterCastHandlers()
        {
            foreach (var script in m_loadedScripts)
            {
                script._PrepareScriptCall(SpellScriptHookType.AfterCast);

                foreach (var hook in script.AfterCast)
                    hook.Call();

                script._FinishScriptCall();
            }
        }

        SpellCastResult CallScriptCheckCastHandlers()
        {
            SpellCastResult retVal = SpellCastResult.SpellCastOk;
            foreach (var script in m_loadedScripts)
            {
                script._PrepareScriptCall(SpellScriptHookType.CheckCast);

                foreach (var hook in script.OnCheckCast)
                {
                    SpellCastResult tempResult = hook.Call();
                    if (tempResult != SpellCastResult.SpellCastOk)
                        retVal = tempResult;
                }

                script._FinishScriptCall();
            }
            return retVal;
        }

        int CallScriptCalcCastTimeHandlers(int castTime)
        {
            foreach (var script in m_loadedScripts)
            {
                script._PrepareScriptCall(SpellScriptHookType.CalcCastTime);
                castTime = script.CalcCastTime(castTime);
                script._FinishScriptCall();
            }
            return castTime;
        }

        bool CallScriptEffectHandlers(uint effIndex, SpellEffectHandleMode mode)
        {
            // execute script effect handler hooks and check if effects was prevented
            bool preventDefault = false;

            foreach (var script in m_loadedScripts)
            {
                script._InitHit();

                SpellScriptHookType hookType;
                List<SpellScript.EffectHandler> effList;
                switch (mode)
                {
                    case SpellEffectHandleMode.Launch:
                        effList = script.OnEffectLaunch;
                        hookType = SpellScriptHookType.Launch;
                        break;
                    case SpellEffectHandleMode.LaunchTarget:
                        effList = script.OnEffectLaunchTarget;
                        hookType = SpellScriptHookType.LaunchTarget;
                        break;
                    case SpellEffectHandleMode.Hit:
                        effList = script.OnEffectHit;
                        hookType = SpellScriptHookType.EffectHit;
                        break;
                    case SpellEffectHandleMode.HitTarget:
                        effList = script.OnEffectHitTarget;
                        hookType = SpellScriptHookType.EffectHitTarget;
                        break;
                    default:
                        Cypher.Assert(false);
                        return false;
                }
                script._PrepareScriptCall(hookType);
                foreach (var eff in effList)
                {
                    // effect execution can be prevented
                    if (!script._IsEffectPrevented(effIndex) && eff.IsEffectAffected(m_spellInfo, effIndex))
                        eff.Call(effIndex);
                }

                if (!preventDefault)
                    preventDefault = script._IsDefaultEffectPrevented(effIndex);

                script._FinishScriptCall();
            }
            return preventDefault;
        }

        void CallScriptSuccessfulDispel(uint effIndex)
        {
            foreach (var script in m_loadedScripts)
            {
                script._PrepareScriptCall(SpellScriptHookType.EffectSuccessfulDispel);

                foreach (var hook in script.OnEffectSuccessfulDispel)
                    hook.Call(effIndex);

                script._FinishScriptCall();
            }
        }

        public void CallScriptBeforeHitHandlers(SpellMissInfo missInfo)
        {
            foreach (var script in m_loadedScripts)
            {
                script._InitHit();
                script._PrepareScriptCall(SpellScriptHookType.BeforeHit);

                foreach (var hook in script.BeforeHit)
                    hook.Call(missInfo);

                script._FinishScriptCall();
            }
        }

        public void CallScriptOnHitHandlers()
        {
            foreach (var script in m_loadedScripts)
            {
                script._PrepareScriptCall(SpellScriptHookType.Hit);

                foreach (var hook in script.OnHit)
                    hook.Call();

                script._FinishScriptCall();
            }
        }

        public void CallScriptAfterHitHandlers()
        {
            foreach (var script in m_loadedScripts)
            {
                script._PrepareScriptCall(SpellScriptHookType.AfterHit);

                foreach (var hook in script.AfterHit)
                    hook.Call();

                script._FinishScriptCall();
            }
        }

        public void CallScriptCalcCritChanceHandlers(Unit victim, ref float critChance)
        {
            foreach (var loadedScript in m_loadedScripts)
            {
                loadedScript._PrepareScriptCall(SpellScriptHookType.CalcCritChance);
                foreach (var hook in loadedScript.OnCalcCritChance)
                    hook.Call(victim, ref critChance);

                loadedScript._FinishScriptCall();
            }
        }

        public void CallScriptCalcDamageHandlers(SpellEffectInfo spellEffectInfo, Unit victim, ref int damage, ref int flatMod, ref float pctMod)
        {
            foreach (SpellScript script in m_loadedScripts)
            {
                script._PrepareScriptCall(SpellScriptHookType.CalcDamage);
                foreach (var calcDamage in script.CalcDamage)
                    calcDamage.Call(spellEffectInfo, victim, ref damage, ref flatMod, ref pctMod);

                script._FinishScriptCall();
            }
        }

        public void CallScriptCalcHealingHandlers(SpellEffectInfo spellEffectInfo, Unit victim, ref int healing, ref int flatMod, ref float pctMod)
        {
            foreach (SpellScript script in m_loadedScripts)
            {
                script._PrepareScriptCall(SpellScriptHookType.CalcHealing);
                foreach (var calcHealing in script.CalcHealing)
                    calcHealing.Call(spellEffectInfo, victim, ref healing, ref flatMod, ref pctMod);

                script._FinishScriptCall();
            }
        }

        void CallScriptObjectAreaTargetSelectHandlers(List<WorldObject> targets, uint effIndex, SpellImplicitTargetInfo targetType)
        {
            foreach (var script in m_loadedScripts)
            {
                script._PrepareScriptCall(SpellScriptHookType.ObjectAreaTargetSelect);

                foreach (var hook in script.OnObjectAreaTargetSelect)
                    if (hook.IsEffectAffected(m_spellInfo, effIndex) && targetType.GetTarget() == hook.GetTarget())
                        hook.Call(targets);

                script._FinishScriptCall();
            }
        }

        void CallScriptObjectTargetSelectHandlers(ref WorldObject target, uint effIndex, SpellImplicitTargetInfo targetType)
        {
            foreach (var script in m_loadedScripts)
            {
                script._PrepareScriptCall(SpellScriptHookType.ObjectTargetSelect);

                foreach (var hook in script.OnObjectTargetSelect)
                    if (hook.IsEffectAffected(m_spellInfo, effIndex) && targetType.GetTarget() == hook.GetTarget())
                        hook.Call(ref target);

                script._FinishScriptCall();
            }
        }

        void CallScriptDestinationTargetSelectHandlers(ref SpellDestination target, uint effIndex, SpellImplicitTargetInfo targetType)
        {
            foreach (var script in m_loadedScripts)
            {
                script._PrepareScriptCall(SpellScriptHookType.DestinationTargetSelect);

                foreach (var hook in script.OnDestinationTargetSelect)
                    if (hook.IsEffectAffected(m_spellInfo, effIndex) && targetType.GetTarget() == hook.GetTarget())
                        hook.Call(ref target);

                script._FinishScriptCall();
            }
        }

        public void CallScriptOnResistAbsorbCalculateHandlers(DamageInfo damageInfo, ref uint resistAmount, ref int absorbAmount)
        {
            foreach (var script in m_loadedScripts)
            {
                script._PrepareScriptCall(SpellScriptHookType.OnResistAbsorbCalculation);

                foreach (var hook in script.OnCalculateResistAbsorb)
                    hook.Call(damageInfo, ref resistAmount, ref absorbAmount);

                script._FinishScriptCall();
            }
        }

        bool IsWithinLOS(WorldObject source, WorldObject target, bool targetAsSourceLocation, ModelIgnoreFlags ignoreFlags)
        {
            if (m_spellInfo.HasAttribute(SpellAttr2.IgnoreLineOfSight))
                return true;

            if (Global.DisableMgr.IsDisabledFor(DisableType.Spell, m_spellInfo.Id, null, DisableFlags.SpellLOS))
                return true;

            if (target.IsCreature() && target.ToCreature().CanIgnoreLineOfSightWhenCastingOnMe())
                return true;

            WorldObject src = targetAsSourceLocation ? target : source;
            WorldObject dst = targetAsSourceLocation ? source : target;
            return src.IsWithinLOSInMap(dst, LineOfSightChecks.All, ignoreFlags);
        }

        bool IsWithinLOS(WorldObject source, Position target, ModelIgnoreFlags ignoreFlags)
        {
            if (m_spellInfo.HasAttribute(SpellAttr2.IgnoreLineOfSight))
                return true;

            if (Global.DisableMgr.IsDisabledFor(DisableType.Spell, m_spellInfo.Id, null, DisableFlags.SpellLOS))
                return true;

            return source.IsWithinLOS(target.GetPositionX(), target.GetPositionY(), target.GetPositionZ(), LineOfSightChecks.All, ignoreFlags);
        }

        void MovePosition(Position pos, WorldObject from, float dist, float angle)
        {
            if (m_spellInfo.HasAttribute(SpellAttr9.ForceDestLocation))
                from.MovePosition(pos, dist, angle);
            else
                from.MovePositionToFirstCollision(pos, dist, angle);
        }

        void CallScriptEmpowerStageCompletedHandlers(int completedStagesCount)
        {
            foreach (SpellScript script in m_loadedScripts)
            {
                script._PrepareScriptCall(SpellScriptHookType.EmpowerStageCompleted);
                foreach (var empowerStageCompleted in script.OnEmpowerStageCompleted)
                    empowerStageCompleted.Call(completedStagesCount);

                script._FinishScriptCall();
            }
        }

        void CallScriptEmpowerCompletedHandlers(int completedStagesCount)
        {
            foreach (SpellScript script in m_loadedScripts)
            {
                script._PrepareScriptCall(SpellScriptHookType.EmpowerCompleted);
                foreach (var empowerStageCompleted in script.OnEmpowerCompleted)
                    empowerStageCompleted.Call(completedStagesCount);

                script._FinishScriptCall();
            }
        }

        bool CheckScriptEffectImplicitTargets(uint effIndex, uint effIndexToCheck)
        {
            bool allEffectTargetScriptsAreShared<T>(List<T> hooks, SpellInfo spellInfo, uint effIndex, uint effIndexToCheck) where T : SpellScript.EffectHook, SpellScript.ITargetFunction
            {
                foreach (var hook in hooks)
                {
                    if (!hook.IsEffectAffected(spellInfo, effIndex))
                        continue;

                    bool otherEffectHasSameTargetFunction = hooks.Any(other => other.IsEffectAffected(spellInfo, effIndexToCheck) && hook.HasSameTargetFunctionAs(other));
                    if (!otherEffectHasSameTargetFunction)
                        return false;
                }

                return true;
            };

            foreach (var script in m_loadedScripts)
            {
                if (!allEffectTargetScriptsAreShared(script.OnObjectTargetSelect, m_spellInfo, effIndex, effIndexToCheck))
                    return false;

                if (!allEffectTargetScriptsAreShared(script.OnObjectTargetSelect, m_spellInfo, effIndexToCheck, effIndex))
                    return false;

                if (!allEffectTargetScriptsAreShared(script.OnObjectAreaTargetSelect, m_spellInfo, effIndex, effIndexToCheck))
                    return false;

                if (!allEffectTargetScriptsAreShared(script.OnObjectAreaTargetSelect, m_spellInfo, effIndexToCheck, effIndex))
                    return false;
            }
            return true;
        }

        public bool CanExecuteTriggersOnHit(Unit unit, SpellInfo triggeredByAura = null)
        {
            bool onlyOnTarget = triggeredByAura != null && triggeredByAura.HasAttribute(SpellAttr4.ClassTriggerOnlyOnTarget);
            if (!onlyOnTarget)
                return true;

            // If triggeredByAura has SPELL_ATTR4_CLASS_TRIGGER_ONLY_ON_TARGET then it can only proc on either noncaster units...
            if (unit != m_caster)
                return true;

            // ... or caster if it is the only target
            if (m_UniqueTargetInfo.Count == 1)
                return true;

            return false;
        }

        void PrepareTriggersExecutedOnHit()
        {
            Unit unitCaster = m_caster.ToUnit();
            if (unitCaster == null)
                return;

            // handle SPELL_AURA_ADD_TARGET_TRIGGER auras:
            // save auras which were present on spell caster on cast, to prevent triggered auras from affecting caster
            // and to correctly calculate proc chance when combopoints are present
            var targetTriggers = unitCaster.GetAuraEffectsByType(AuraType.AddTargetTrigger);
            foreach (var aurEff in targetTriggers)
            {
                if (!aurEff.IsAffectingSpell(m_spellInfo))
                    continue;

                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(aurEff.GetSpellEffectInfo().TriggerSpell, GetCastDifficulty());
                if (spellInfo != null)
                {
                    // calculate the chance using spell base amount, because aura amount is not updated on combo-points change
                    // this possibly needs fixing
                    int auraBaseAmount = aurEff.GetBaseAmount();
                    // proc chance is stored in effect amount
                    int chance = unitCaster.CalculateSpellDamage(null, aurEff.GetSpellEffectInfo(), auraBaseAmount);
                    chance *= aurEff.GetBase().GetStackAmount();

                    // build trigger and add to the list
                    m_hitTriggerSpells.Add(new HitTriggerSpell(spellInfo, aurEff.GetSpellInfo(), chance));
                }
            }
        }

        bool CanHaveGlobalCooldown(WorldObject caster)
        {
            // Only players or controlled units have global cooldown
            if (!caster.IsPlayer() && (!caster.IsCreature() || caster.ToCreature().GetCharmInfo() == null))
                return false;

            return true;
        }

        bool HasGlobalCooldown()
        {
            if (!CanHaveGlobalCooldown(m_caster))
                return false;

            return m_caster.ToUnit().GetSpellHistory().HasGlobalCooldown(m_spellInfo);
        }

        void TriggerGlobalCooldown()
        {
            if (!CanHaveGlobalCooldown(m_caster))
                return;

            TimeSpan gcd = TimeSpan.FromMilliseconds(m_spellInfo.StartRecoveryTime);
            if (gcd == TimeSpan.Zero || m_spellInfo.StartRecoveryCategory == 0)
                return;

            if (m_caster.IsTypeId(TypeId.Player))
                if (m_caster.ToPlayer().GetCommandStatus(PlayerCommandStates.Cooldown))
                    return;

            TimeSpan MinGCD = TimeSpan.FromMilliseconds(750);
            TimeSpan MaxGCD = TimeSpan.FromMilliseconds(1500);

            // Global cooldown can't leave range 1..1.5 secs
            // There are some spells (mostly not casted directly by player) that have < 1 sec and > 1.5 sec global cooldowns
            // but as tests show are not affected by any spell mods.
            if (gcd >= MinGCD && gcd <= MaxGCD)
            {
                // gcd modifier auras are applied only to own spells and only players have such mods
                Player modOwner = m_caster.GetSpellModOwner();
                if (modOwner != null)
                {
                    int intGcd = (int)gcd.TotalMilliseconds;
                    modOwner.ApplySpellMod(m_spellInfo, SpellModOp.StartCooldown, ref intGcd, this);
                    gcd = TimeSpan.FromMilliseconds(intGcd);
                }

                bool isMeleeOrRangedSpell = m_spellInfo.DmgClass == SpellDmgClass.Melee || m_spellInfo.DmgClass == SpellDmgClass.Ranged ||
                    m_spellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) || m_spellInfo.HasAttribute(SpellAttr0.IsAbility);

                // Apply haste rating
                if (gcd > MinGCD && (m_spellInfo.StartRecoveryCategory == 133 && !isMeleeOrRangedSpell))
                {
                    gcd = TimeSpan.FromMilliseconds(gcd.TotalMilliseconds * m_caster.ToUnit().m_unitData.ModSpellHaste);
                    int intGcd = (int)gcd.TotalMilliseconds;
                    MathFunctions.RoundToInterval(ref intGcd, 750, 1500);
                    gcd = TimeSpan.FromMilliseconds(intGcd);
                }

                if (gcd > MinGCD && m_caster.ToUnit().HasAuraTypeWithAffectMask(AuraType.ModGlobalCooldownByHasteRegen, m_spellInfo))
                {
                    gcd = TimeSpan.FromMilliseconds(gcd.TotalMilliseconds * m_caster.ToUnit().m_unitData.ModHasteRegen);
                    int intGcd = (int)gcd.TotalMilliseconds;
                    MathFunctions.RoundToInterval(ref intGcd, 750, 1500);
                    gcd = TimeSpan.FromMilliseconds(intGcd);
                }
            }

            m_caster.ToUnit().GetSpellHistory().AddGlobalCooldown(m_spellInfo, gcd);
        }

        void CancelGlobalCooldown()
        {
            if (!CanHaveGlobalCooldown(m_caster))
                return;

            if (m_spellInfo.StartRecoveryTime == 0)
                return;

            // Cancel global cooldown when interrupting current cast
            if (m_caster.ToUnit().GetCurrentSpell(CurrentSpellTypes.Generic) != this)
                return;

            m_caster.ToUnit().GetSpellHistory().CancelGlobalCooldown(m_spellInfo);
        }

        public string GetDebugInfo()
        {
            return $"Id: {GetSpellInfo().Id} Name: '{GetSpellInfo().SpellName[Global.WorldMgr.GetDefaultDbcLocale()]}' OriginalCaster: {m_originalCasterGUID} State: {GetState()}";
        }

        List<SpellScript> m_loadedScripts = new();

        public SpellCastResult CheckMovement()
        {
            if (IsTriggered())
                return SpellCastResult.SpellCastOk;

            Unit unitCaster = m_caster.ToUnit();
            if (unitCaster != null)
            {
                if (!unitCaster.CanCastSpellWhileMoving(m_spellInfo))
                {
                    if (GetState() == SpellState.Preparing)
                    {
                        if (m_casttime > 0 && m_spellInfo.InterruptFlags.HasFlag(SpellInterruptFlags.Movement))
                            return SpellCastResult.Moving;
                    }
                    else if (GetState() == SpellState.Casting && !m_spellInfo.IsMoveAllowedChannel())
                        return SpellCastResult.Moving;
                }
            }

            return SpellCastResult.SpellCastOk;
        }

        int CalculateDamage(SpellEffectInfo spellEffectInfo, Unit target)
        {
            return CalculateDamage(spellEffectInfo, target, out _);
        }

        int CalculateDamage(SpellEffectInfo spellEffectInfo, Unit target, out float variance)
        {
            bool needRecalculateBasePoints = (m_spellValue.CustomBasePointsMask & (1 << (int)spellEffectInfo.EffectIndex)) == 0;
            return m_caster.CalculateSpellDamage(out variance, target, spellEffectInfo, needRecalculateBasePoints ? null : m_spellValue.EffectBasePoints[spellEffectInfo.EffectIndex], m_castItemEntry, m_castItemLevel);
        }

        public SpellState GetState()
        {
            return m_spellState;
        }

        public void SetState(SpellState state)
        {
            m_spellState = state;
        }

        void CheckSrc()
        {
            if (!m_targets.HasSrc()) m_targets.SetSrc(m_caster);
        }

        void CheckDst()
        {
            if (!m_targets.HasDst()) m_targets.SetDst(m_caster);
        }

        public int GetCastTime()
        {
            return m_casttime;
        }

        public int GetRemainingCastTime() { return m_timer; }

        bool IsAutoRepeat()
        {
            return m_autoRepeat;
        }

        void SetAutoRepeat(bool rep)
        {
            m_autoRepeat = rep;
        }

        void ReSetTimer()
        {
            m_timer = m_casttime > 0 ? m_casttime : 0;
        }

        public bool IsTriggered() { return (!m_fromClient && _triggeredCastFlags.HasAnyFlag(TriggerCastFlags.FullMask)) || !m_originalCastId.IsEmpty(); }
        public bool IsTriggeredByAura(SpellInfo auraSpellInfo) { return (auraSpellInfo == m_triggeredByAuraSpell); }
        public bool IsIgnoringCooldowns() { return _triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreSpellAndCategoryCD); }
        public bool IsFocusDisabled() { return _triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreSetFacing) || (m_spellInfo.IsChanneled() && !m_spellInfo.HasAttribute(SpellAttr1.TrackTargetInChannel)); }
        public bool IsProcDisabled() { return _triggeredCastFlags.HasAnyFlag(TriggerCastFlags.DisallowProcEvents); }
        public bool IsChannelActive() { return m_caster.IsUnit() && m_caster.ToUnit().GetChannelSpellId() != 0; }

        public int GetProcChainLength() { return m_procChainLength; }

        public bool IsDeletable()
        {
            return !m_referencedFromCurrentSpell && !m_executedCurrently;
        }
        public void SetReferencedFromCurrent(bool yes)
        {
            m_referencedFromCurrentSpell = yes;
        }
        public bool IsInterruptable()
        {
            return !m_executedCurrently;
        }
        void SetExecutedCurrently(bool yes)
        {
            m_executedCurrently = yes;
        }
        public ulong GetDelayStart()
        {
            return m_delayStart;
        }
        public void SetDelayStart(ulong m_time)
        {
            m_delayStart = m_time;
        }
        public ulong GetDelayMoment()
        {
            return m_delayMoment;
        }

        public WorldObject GetCaster() { return m_caster; }

        public ObjectGuid GetOriginalCasterGUID() { return m_originalCasterGUID; }

        public Unit GetOriginalCaster() { return m_originalCaster; }

        public SpellInfo GetSpellInfo() { return m_spellInfo; }

        public List<SpellPowerCost> GetPowerCost() { return m_powerCost; }

        bool IsDelayableNoMore()
        {
            if (m_delayAtDamageCount >= 2)
                return true;

            ++m_delayAtDamageCount;
            return false;
        }

        bool DontReport()
        {
            return Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.DontReportCastError);
        }

        public SpellInfo GetTriggeredByAuraSpell() { return m_triggeredByAuraSpell; }

        public int GetTimer() { return m_timer; }

        #region Fields
        Dictionary<SpellEffectName, SpellLogEffect> _executeLogEffects = new();
        PathGenerator m_preGeneratedPath;

        public SpellInfo m_spellInfo;
        public Item m_CastItem;
        public ObjectGuid m_castItemGUID;
        public uint m_castItemEntry;
        public int m_castItemLevel;
        public ObjectGuid m_castId;
        public ObjectGuid m_originalCastId;
        public bool m_fromClient;
        public SpellCastFlagsEx m_castFlagsEx;
        public SpellMisc m_misc;
        public object m_customArg;
        public SpellCastVisual m_SpellVisual;
        public SpellCastTargets m_targets = new();
        public SpellCustomErrors m_customError;

        public List<Aura> m_appliedMods = new();

        public ActionResultSetter<SpellCastResult> m_scriptResult;
        public bool m_scriptWaitsForSpellHit;

        WorldObject m_caster;
        public SpellValue m_spellValue;
        ObjectGuid m_originalCasterGUID;
        Unit m_originalCaster;
        public Spell m_selfContainer;

        //Spell data
        internal SpellSchoolMask m_spellSchoolMask;                  // Spell school (can be overwrite for some spells (wand shoot for example)
        internal WeaponAttackType m_attackType;                      // For weapon based attack

        List<SpellPowerCost> m_powerCost = new();
        int m_casttime;                                   // Calculated spell cast time initialized only in Spell.prepare
        int m_channeledDuration;                          // Calculated channeled spell duration in order to calculate correct pushback.
        bool m_canReflect;                                  // can reflect this spell?
        bool m_autoRepeat;
        byte m_runesState;
        byte m_delayAtDamageCount;

        EmpowerData m_empower;

        // Delayed spells system
        ulong m_delayStart;                                // time of spell delay start, filled by event handler, zero = just started
        ulong m_delayMoment;                               // moment of next delay call, used internally
        bool m_launchHandled;                               // were launch actions handled
        bool m_immediateHandled;                            // were immediate actions handled? (used by delayed spells only)

        // These vars are used in both delayed spell system and modified immediate spell system
        bool m_referencedFromCurrentSpell;
        bool m_executedCurrently;
        uint m_applyMultiplierMask;
        float[] m_damageMultipliers = new float[SpellConst.MaxEffects];

        // Current targets, to be used in SpellEffects (MUST BE USED ONLY IN SPELL EFFECTS)
        public Unit unitTarget;
        public Item itemTarget;
        public GameObject gameObjTarget;
        public Corpse corpseTarget;
        public WorldLocation destTarget;
        public int damage;
        public SpellMissInfo targetMissInfo;
        public float variance;
        SpellEffectHandleMode effectHandleMode;
        public SpellEffectInfo effectInfo;
        // used in effects handlers
        internal UnitAura spellAura;
        internal DynObjAura dynObjAura;

        // -------------------------------------------
        GameObject focusObject;

        // Damage and healing in effects need just calculate
        public int m_damage;           // Damge   in effects count here
        public int m_healing;          // Healing in effects count here

        // ******************************************
        // Spell trigger system
        // ******************************************
        internal ProcFlagsInit m_procAttacker;                // Attacker trigger flags
        internal ProcFlagsInit m_procVictim;                  // Victim   trigger flags
        internal ProcFlagsHit m_hitMask;
        internal ProcFlagsSpellType m_procSpellType;   // for finish procs

        // *****************************************
        // Spell target subsystem
        // *****************************************
        // Targets store structures and data
        public List<TargetInfo> m_UniqueTargetInfo = new();
        uint m_channelTargetEffectMask;                        // Mask req. alive targets

        List<GOTargetInfo> m_UniqueGOTargetInfo = new();
        List<ItemTargetInfo> m_UniqueItemInfo = new();
        List<CorpseTargetInfo> m_UniqueCorpseTargetInfo = new();

        SpellDestination[] m_destTargets = new SpellDestination[SpellConst.MaxEffects];

        List<HitTriggerSpell> m_hitTriggerSpells = new();

        SpellState m_spellState;
        int m_timer;

        SpellEvent _spellEvent;
        TriggerCastFlags _triggeredCastFlags;

        // if need this can be replaced by Aura copy
        // we can't store original aura link to prevent access to deleted auras
        // and in same time need aura data and after aura deleting.
        public SpellInfo m_triggeredByAuraSpell;
        int m_procChainLength;
        #endregion
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct SpellMisc
    {
        // Alternate names for this value 
        [FieldOffset(0)]
        public uint TalentId;

        [FieldOffset(0)]
        public uint SpellId;

        [FieldOffset(0)]
        public uint SpecializationId;

        // SPELL_EFFECT_SET_FOLLOWER_QUALITY
        // SPELL_EFFECT_INCREASE_FOLLOWER_ITEM_LEVEL
        // SPELL_EFFECT_INCREASE_FOLLOWER_EXPERIENCE
        // SPELL_EFFECT_RANDOMIZE_FOLLOWER_ABILITIES
        // SPELL_EFFECT_LEARN_FOLLOWER_ABILITY
        [FieldOffset(0)]
        public uint FollowerId;

        [FieldOffset(4)]
        public uint FollowerAbilityId;   // only SPELL_EFFECT_LEARN_FOLLOWER_ABILITY

        // SPELL_EFFECT_FINISH_GARRISON_MISSION
        [FieldOffset(0)]
        public uint GarrMissionId;

        // SPELL_EFFECT_UPGRADE_HEIRLOOM
        [FieldOffset(0)]
        public uint ItemId;

        [FieldOffset(0)]
        public uint Data0;

        [FieldOffset(4)]
        public uint Data1;

        public uint[] GetRawData()
        {
            return new uint[] { Data0, Data1 };
        }
    }

    public struct HitTriggerSpell
    {
        public HitTriggerSpell(SpellInfo spellInfo, SpellInfo auraSpellInfo, int procChance)
        {
            triggeredSpell = spellInfo;
            triggeredByAura = auraSpellInfo;
            chance = procChance;
        }

        public SpellInfo triggeredSpell;
        public SpellInfo triggeredByAura;
        // ubyte triggeredByEffIdx          This might be needed at a later stage - No need known for now
        public int chance;
    }

    public enum SpellEffectHandleMode
    {
        Launch,
        LaunchTarget,
        Hit,
        HitTarget
    }

    public class SkillStatusData
    {
        public SkillStatusData(uint _pos, SkillState state)
        {
            Pos = (byte)_pos;
            State = state;
        }
        public byte Pos;
        public SkillState State;
    }

    public class SpellChainNode
    {
        public SpellInfo prev;
        public SpellInfo next;
        public SpellInfo first;
        public SpellInfo last;
        public byte rank;
    }

    public class SpellLearnSkillNode
    {
        public SkillType skill;
        public ushort step;
        public ushort value;                                           // 0  - max skill value for player level
        public ushort maxvalue;                                        // 0  - max skill value for player level
    }

    public class SpellLearnSpellNode
    {
        public uint SourceSpell;
        public uint Spell;
        public uint OverridesSpell;
        public bool Active;         // show in spellbook or not
        public bool AutoLearned;    // This marks the spell as automatically learned from another source that - will only be used for unlearning
    }

    public class SpellDestination
    {
        public SpellDestination()
        {
            Position = new WorldLocation();
            TransportGUID = ObjectGuid.Empty;
            TransportOffset = new Position();
        }

        public SpellDestination(float x, float y, float z, float orientation = 0.0f, uint mapId = 0xFFFFFFFF) : this()
        {
            Position.Relocate(x, y, z, orientation);
            TransportGUID = ObjectGuid.Empty;
            Position.SetMapId(mapId);
        }

        public SpellDestination(Position pos) : this()
        {
            Position.Relocate(pos);
            TransportGUID = ObjectGuid.Empty;
        }

        public SpellDestination(WorldLocation loc) : this()
        {
            Position.WorldRelocate(loc);
            TransportGUID.Clear();
            TransportOffset.Relocate(0, 0, 0, 0);
        }

        public SpellDestination(WorldObject wObj) : this()
        {
            TransportGUID = wObj.GetTransGUID();
            TransportOffset.Relocate(wObj.GetTransOffsetX(), wObj.GetTransOffsetY(), wObj.GetTransOffsetZ(), wObj.GetTransOffsetO());
            Position.Relocate(wObj.GetPosition());
        }

        public void Relocate(Position pos)
        {
            if (!TransportGUID.IsEmpty())
            {
                Position offset;
                Position.GetPositionOffsetTo(pos, out offset);
                TransportOffset.RelocateOffset(offset);
            }
            Position.Relocate(pos);
        }

        public void RelocateOffset(Position offset)
        {
            if (!TransportGUID.IsEmpty())
                TransportOffset.RelocateOffset(offset);

            Position.RelocateOffset(offset);
        }

        public WorldLocation Position;
        public ObjectGuid TransportGUID;
        public Position TransportOffset;
    }

    public class TargetInfoBase
    {
        public uint EffectMask;

        public virtual void PreprocessTarget(Spell spell) { }
        public virtual void DoTargetSpellHit(Spell spell, SpellEffectInfo spellEffectInfo) { }
        public virtual void DoDamageAndTriggers(Spell spell) { }
    }

    public class TargetInfo : TargetInfoBase
    {
        public ObjectGuid TargetGUID;
        public ulong TimeDelay;
        public int Damage;
        public int Healing;

        public SpellMissInfo MissCondition;
        public SpellMissInfo ReflectResult;

        public bool IsAlive;
        public bool IsCrit;

        // info set at PreprocessTarget, used by DoTargetSpellHit
        public DiminishingGroup DRGroup;
        public int AuraDuration;
        public int[] AuraBasePoints = new int[SpellConst.MaxEffects];
        public bool Positive = true;
        public UnitAura HitAura;
        public ProcFlagsHit ProcHitMask;

        Unit _spellHitTarget; // changed for example by reflect
        bool _enablePVP;         // need to enable PVP at DoDamageAndTriggers?

        public override void PreprocessTarget(Spell spell)
        {
            Unit unit = spell.GetCaster().GetGUID() == TargetGUID ? spell.GetCaster().ToUnit() : Global.ObjAccessor.GetUnit(spell.GetCaster(), TargetGUID);
            if (unit == null)
                return;

            // Need init unitTarget by default unit (can changed in code on reflect)
            spell.unitTarget = unit;

            // Reset damage/healing counter
            spell.m_damage = Damage;
            spell.m_healing = Healing;

            _spellHitTarget = null;
            if (MissCondition == SpellMissInfo.None || (MissCondition == SpellMissInfo.Block && !spell.GetSpellInfo().HasAttribute(SpellAttr3.CompletelyBlocked)))
                _spellHitTarget = unit;
            else if (MissCondition == SpellMissInfo.Reflect && ReflectResult == SpellMissInfo.None)
                _spellHitTarget = spell.GetCaster().ToUnit();

            if (spell.GetOriginalCaster() != null && MissCondition != SpellMissInfo.Evade && !spell.GetOriginalCaster().IsFriendlyTo(unit) && (!spell.m_spellInfo.IsPositive() || spell.m_spellInfo.HasEffect(SpellEffectName.Dispel)) && (spell.m_spellInfo.HasInitialAggro() || unit.IsEngaged()))
                unit.SetInCombatWith(spell.GetOriginalCaster());

            // if target is flagged for pvp also flag caster if a player
            // but respect current pvp rules (buffing/healing npcs flagged for pvp only flags you if they are in combat)
            _enablePVP = (MissCondition == SpellMissInfo.None || spell.m_spellInfo.HasAttribute(SpellAttr3.PvpEnabling))
                && unit.IsPvP() && (unit.IsInCombat() || unit.IsCharmedOwnedByPlayerOrPlayer()) && spell.GetCaster().IsPlayer(); // need to check PvP state before spell effects, but act on it afterwards

            if (_spellHitTarget != null)
            {
                SpellMissInfo missInfo = spell.PreprocessSpellHit(_spellHitTarget, this);
                if (missInfo != SpellMissInfo.None)
                {
                    if (missInfo != SpellMissInfo.Miss)
                        spell.GetCaster().SendSpellMiss(unit, spell.m_spellInfo.Id, missInfo);
                    spell.m_damage = 0;
                    spell.m_healing = 0;
                    _spellHitTarget = null;
                }
            }

            spell.CallScriptOnHitHandlers();

            // scripts can modify damage/healing for current target, save them
            Damage = spell.m_damage;
            Healing = spell.m_healing;
        }

        public override void DoTargetSpellHit(Spell spell, SpellEffectInfo spellEffectInfo)
        {
            Unit unit = spell.GetCaster().GetGUID() == TargetGUID ? spell.GetCaster().ToUnit() : Global.ObjAccessor.GetUnit(spell.GetCaster(), TargetGUID);
            if (unit == null)
                return;

            // Need init unitTarget by default unit (can changed in code on reflect)
            // Or on missInfo != SPELL_MISS_NONE unitTarget undefined (but need in trigger subsystem)
            spell.unitTarget = unit;
            spell.targetMissInfo = MissCondition;

            // Reset damage/healing counter
            spell.m_damage = Damage;
            spell.m_healing = Healing;

            if (unit.IsAlive() != IsAlive && !spell.m_spellInfo.HasAttribute(SpellAttr9.ForceCorpseTarget))
                return;

            if (!spell.m_spellInfo.HasAttribute(SpellAttr8.IgnoreSanctuary) && spell.GetState() == SpellState.Delayed && !spell.IsPositive() && (GameTime.GetGameTimeMS() - TimeDelay) <= unit.LastSanctuaryTime)
                return;                                             // No missinfo in that case

            if (_spellHitTarget != null)
                spell.DoSpellEffectHit(_spellHitTarget, spellEffectInfo, this);

            // scripts can modify damage/healing for current target, save them
            Damage = spell.m_damage;
            Healing = spell.m_healing;
        }

        public override void DoDamageAndTriggers(Spell spell)
        {
            Unit unit = spell.GetCaster().GetGUID() == TargetGUID ? spell.GetCaster().ToUnit() : Global.ObjAccessor.GetUnit(spell.GetCaster(), TargetGUID);
            if (unit == null)
                return;

            // other targets executed before this one changed pointer
            spell.unitTarget = unit;
            if (_spellHitTarget != null)
                spell.unitTarget = _spellHitTarget;

            // Reset damage/healing counter
            spell.m_damage = Damage;
            spell.m_healing = Healing;

            // Get original caster (if exist) and calculate damage/healing from him data
            // Skip if m_originalCaster not available
            Unit caster = spell.GetOriginalCaster() != null ? spell.GetOriginalCaster() : spell.GetCaster().ToUnit();
            if (caster != null)
            {
                // Fill base trigger info
                ProcFlagsInit procAttacker = spell.m_procAttacker;
                ProcFlagsInit procVictim = spell.m_procVictim;
                ProcFlagsSpellType procSpellType = ProcFlagsSpellType.None;

                // Spells with this flag cannot trigger if effect is cast on self
                bool canEffectTrigger = spell.unitTarget.CanProc();

                // Trigger info was not filled in Spell::prepareDataForTriggerSystem - we do it now
                if (canEffectTrigger && !procAttacker && !procVictim)
                {
                    bool positive = true;
                    if (spell.m_damage > 0)
                        positive = false;
                    else if (spell.m_healing == 0)
                    {
                        for (uint i = 0; i < spell.m_spellInfo.GetEffects().Count; ++i)
                        {
                            // in case of immunity, check all effects to choose correct procFlags, as none has technically hit
                            if (EffectMask != 0 && (EffectMask & (1 << (int)i)) == 0)
                                continue;

                            if (!spell.m_spellInfo.IsPositiveEffect(i))
                            {
                                positive = false;
                                break;
                            }
                        }
                    }

                    switch (spell.m_spellInfo.DmgClass)
                    {
                        case SpellDmgClass.None:
                        case SpellDmgClass.Magic:
                            if (spell.m_spellInfo.HasAttribute(SpellAttr3.TreatAsPeriodic))
                            {
                                if (positive)
                                {
                                    procAttacker.Or(ProcFlags.DealHelpfulPeriodic);
                                    procVictim.Or(ProcFlags.TakeHelpfulPeriodic);
                                }
                                else
                                {
                                    procAttacker.Or(ProcFlags.DealHarmfulPeriodic);
                                    procVictim.Or(ProcFlags.TakeHarmfulPeriodic);
                                }
                            }
                            else if (spell.m_spellInfo.HasAttribute(SpellAttr0.IsAbility))
                            {
                                if (positive)
                                {
                                    procAttacker.Or(ProcFlags.DealHelpfulAbility);
                                    procVictim.Or(ProcFlags.TakeHelpfulAbility);
                                }
                                else
                                {
                                    procAttacker.Or(ProcFlags.DealHarmfulAbility);
                                    procVictim.Or(ProcFlags.TakeHarmfulAbility);
                                }
                            }
                            else
                            {
                                if (positive)
                                {
                                    procAttacker.Or(ProcFlags.DealHelpfulSpell);
                                    procVictim.Or(ProcFlags.TakeHelpfulSpell);
                                }
                                else
                                {
                                    procAttacker.Or(ProcFlags.DealHarmfulSpell);
                                    procVictim.Or(ProcFlags.TakeHarmfulSpell);
                                }
                            }
                            break;
                    }
                }

                // All calculated do it!
                // Do healing
                bool hasHealing = false;
                DamageInfo spellDamageInfo = null;
                HealInfo healInfo = null;
                if (spell.m_healing > 0)
                {
                    hasHealing = true;
                    int addhealth = spell.m_healing;
                    if (IsCrit)
                    {
                        ProcHitMask |= ProcFlagsHit.Critical;
                        addhealth = Unit.SpellCriticalHealingBonus(caster, spell.m_spellInfo, addhealth, null);
                    }
                    else
                        ProcHitMask |= ProcFlagsHit.Normal;

                    healInfo = new(caster, spell.unitTarget, (uint)addhealth, spell.m_spellInfo, spell.m_spellInfo.GetSchoolMask());
                    caster.HealBySpell(healInfo, IsCrit);
                    spell.unitTarget.GetThreatManager().ForwardThreatForAssistingMe(caster, healInfo.GetEffectiveHeal() * 0.5f, spell.m_spellInfo);
                    spell.m_healing = (int)healInfo.GetEffectiveHeal();

                    procSpellType |= ProcFlagsSpellType.Heal;
                }

                // Do damage
                bool hasDamage = false;
                if (spell.m_damage > 0)
                {
                    hasDamage = true;
                    // Fill base damage struct (unitTarget - is real spell target)
                    SpellNonMeleeDamage damageInfo = new(caster, spell.unitTarget, spell.m_spellInfo, spell.m_SpellVisual, spell.m_spellSchoolMask, spell.m_castId);
                    // Check damage immunity
                    if (spell.unitTarget.IsImmunedToDamage(caster, spell.m_spellInfo))
                    {
                        ProcHitMask = ProcFlagsHit.Immune;
                        spell.m_damage = 0;

                        // no packet found in sniffs
                    }
                    else
                    {
                        caster.SetLastDamagedTargetGuid(spell.unitTarget.GetGUID());

                        // Add bonuses and fill damageInfo struct
                        caster.CalculateSpellDamageTaken(damageInfo, spell.m_damage, spell.m_spellInfo, spell.m_attackType, IsCrit, MissCondition == SpellMissInfo.Block, spell);
                        Unit.DealDamageMods(damageInfo.attacker, damageInfo.target, ref damageInfo.damage, ref damageInfo.absorb);

                        ProcHitMask |= Unit.CreateProcHitMask(damageInfo, MissCondition);
                        procVictim.Or(ProcFlags.TakeAnyDamage);

                        spell.m_damage = (int)damageInfo.damage;

                        // sparring
                        if (damageInfo.target != damageInfo.attacker)
                        {
                            Creature victimCreature = damageInfo.target.ToCreature();
                            if (victimCreature != null)
                                damageInfo.damage = victimCreature.CalculateDamageForSparring(damageInfo.attacker, damageInfo.damage);
                        }

                        caster.DealSpellDamage(damageInfo, true);

                        // Send log damage message to client
                        caster.SendSpellNonMeleeDamageLog(damageInfo);
                    }

                    // Do triggers for unit
                    if (canEffectTrigger)
                    {
                        spellDamageInfo = new(damageInfo, DamageEffectType.SpellDirect, spell.m_attackType, ProcHitMask);
                        procSpellType |= ProcFlagsSpellType.Damage;
                    }
                }

                // Passive spell hits/misses or active spells only misses (only triggers)
                if (!hasHealing && !hasDamage)
                {
                    // Fill base damage struct (unitTarget - is real spell target)
                    SpellNonMeleeDamage damageInfo = new(caster, spell.unitTarget, spell.m_spellInfo, spell.m_SpellVisual, spell.m_spellSchoolMask);
                    ProcHitMask |= Unit.CreateProcHitMask(damageInfo, MissCondition);
                    // Do triggers for unit
                    if (canEffectTrigger)
                    {
                        spellDamageInfo = new(damageInfo, DamageEffectType.NoDamage, spell.m_attackType, ProcHitMask);
                        procSpellType |= ProcFlagsSpellType.NoDmgHeal;
                    }

                    // Failed Pickpocket, reveal rogue
                    if (MissCondition == SpellMissInfo.Resist && spell.m_spellInfo.HasAttribute(SpellCustomAttributes.PickPocket) && spell.unitTarget.IsCreature())
                    {
                        Unit unitCaster = spell.GetCaster().ToUnit();
                        unitCaster.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Interacting);
                        spell.unitTarget.ToCreature().EngageWithTarget(unitCaster);
                    }
                }

                // Do triggers for unit
                if (canEffectTrigger)
                {
                    Unit.ProcSkillsAndAuras(caster, spell.unitTarget, procAttacker, procVictim, procSpellType, ProcFlagsSpellPhase.Hit, ProcHitMask, spell, spellDamageInfo, healInfo);

                    // item spells (spell hit of non-damage spell may also activate items, for example seal of corruption hidden hit)
                    if (caster.IsPlayer() && procSpellType.HasAnyFlag(ProcFlagsSpellType.Damage | ProcFlagsSpellType.NoDmgHeal))
                    {
                        if (spell.m_spellInfo.DmgClass == SpellDmgClass.Melee || spell.m_spellInfo.DmgClass == SpellDmgClass.Ranged)
                            if (!spell.m_spellInfo.HasAttribute(SpellAttr0.CancelsAutoAttackCombat) && !spell.m_spellInfo.HasAttribute(SpellAttr4.SuppressWeaponProcs))
                                caster.ToPlayer().CastItemCombatSpell(spellDamageInfo);
                    }
                }

                // set hitmask for finish procs
                spell.m_hitMask |= ProcHitMask;
                spell.m_procSpellType |= procSpellType;

                // _spellHitTarget can be null if spell is missed in DoSpellHitOnUnit
                if (MissCondition != SpellMissInfo.Evade && _spellHitTarget != null && !spell.GetCaster().IsFriendlyTo(unit) && (!spell.IsPositive() || spell.m_spellInfo.HasEffect(SpellEffectName.Dispel)))
                {
                    Unit unitCaster = spell.GetCaster().ToUnit();
                    if (unitCaster != null)
                    {
                        unitCaster.AtTargetAttacked(unit, spell.m_spellInfo.HasInitialAggro());
                        if (spell.m_spellInfo.HasAttribute(SpellAttr6.TapsImmediately))
                        {
                            Creature targetCreature = unit.ToCreature();
                            if (targetCreature != null)
                                if (unitCaster.IsPlayer())
                                    targetCreature.SetTappedBy(unitCaster);
                        }
                    }

                    if (!spell.m_spellInfo.HasAttribute(SpellAttr3.DoNotTriggerTargetStand) && !unit.IsStandState())
                        unit.SetStandState(UnitStandStateType.Stand);
                }

                // Check for SPELL_ATTR7_INTERRUPT_ONLY_NONPLAYER
                if (MissCondition == SpellMissInfo.None && spell.m_spellInfo.HasAttribute(SpellAttr7.CanCauseInterrupt) && !unit.IsPlayer())
                    caster.CastSpell(unit, 32747, new CastSpellExtraArgs(spell));
            }

            if (_spellHitTarget != null)
            {
                //AI functions
                Creature cHitTarget = _spellHitTarget.ToCreature();
                if (cHitTarget != null)
                {
                    CreatureAI hitTargetAI = cHitTarget.GetAI();
                    if (hitTargetAI != null)
                        hitTargetAI.SpellHit(spell.GetCaster(), spell.m_spellInfo);
                }

                if (spell.GetCaster().IsCreature() && spell.GetCaster().ToCreature().IsAIEnabled())
                    spell.GetCaster().ToCreature().GetAI().SpellHitTarget(_spellHitTarget, spell.m_spellInfo);
                else if (spell.GetCaster().IsGameObject() && spell.GetCaster().ToGameObject().GetAI() != null)
                    spell.GetCaster().ToGameObject().GetAI().SpellHitTarget(_spellHitTarget, spell.m_spellInfo);

                if (HitAura != null)
                {
                    AuraApplication aurApp = HitAura.GetApplicationOfTarget(_spellHitTarget.GetGUID());
                    if (aurApp != null)
                    {
                        // only apply unapplied effects (for reapply case)
                        uint effMask = EffectMask & aurApp.GetEffectsToApply();
                        for (uint i = 0; i < spell.m_spellInfo.GetEffects().Count; ++i)
                            if ((effMask & (1 << (int)i)) != 0 && aurApp.HasEffect(i))
                                effMask &= ~(1u << (int)i);

                        if (effMask != 0)
                            _spellHitTarget._ApplyAura(aurApp, effMask);

                        if (aurApp.IsNeedClientUpdate() && aurApp.GetRemoveMode() == AuraRemoveMode.None)
                        {
                            aurApp.ClientUpdate(false);
                            _spellHitTarget.RemoveVisibleAuraUpdate(aurApp);
                        }
                    }
                }

                // Needs to be called after dealing damage/healing to not remove breaking on damage auras
                spell.DoTriggersOnSpellHit(_spellHitTarget);
            }

            if (_enablePVP)
                spell.GetCaster().ToPlayer().UpdatePvP(true);

            spell.spellAura = HitAura;
            spell.CallScriptAfterHitHandlers();
            spell.spellAura = null;
        }
    }

    public class GOTargetInfo : TargetInfoBase
    {
        public ObjectGuid TargetGUID;
        public ulong TimeDelay;

        public override void DoTargetSpellHit(Spell spell, SpellEffectInfo spellEffectInfo)
        {
            GameObject go = spell.GetCaster().GetGUID() == TargetGUID ? spell.GetCaster().ToGameObject() : ObjectAccessor.GetGameObject(spell.GetCaster(), TargetGUID);
            if (go == null)
                return;

            spell.CallScriptBeforeHitHandlers(SpellMissInfo.None);

            spell.HandleEffects(null, null, go, null, spellEffectInfo, SpellEffectHandleMode.HitTarget);

            //AI functions
            if (go.GetAI() != null)
                go.GetAI().SpellHit(spell.GetCaster(), spell.m_spellInfo);

            if (spell.GetCaster().IsCreature() && spell.GetCaster().ToCreature().IsAIEnabled())
                spell.GetCaster().ToCreature().GetAI().SpellHitTarget(go, spell.m_spellInfo);
            else if (spell.GetCaster().IsGameObject() && spell.GetCaster().ToGameObject().GetAI() != null)
                spell.GetCaster().ToGameObject().GetAI().SpellHitTarget(go, spell.m_spellInfo);

            spell.CallScriptOnHitHandlers();
            spell.CallScriptAfterHitHandlers();
        }
    }

    public class ItemTargetInfo : TargetInfoBase
    {
        public Item TargetItem;

        public override void DoTargetSpellHit(Spell spell, SpellEffectInfo spellEffectInfo)
        {
            spell.CallScriptBeforeHitHandlers(SpellMissInfo.None);

            spell.HandleEffects(null, TargetItem, null, null, spellEffectInfo, SpellEffectHandleMode.HitTarget);

            spell.CallScriptOnHitHandlers();
            spell.CallScriptAfterHitHandlers();
        }
    }

    public class CorpseTargetInfo : TargetInfoBase
    {
        public ObjectGuid TargetGUID;
        public ulong TimeDelay;

        public override void DoTargetSpellHit(Spell spell, SpellEffectInfo spellEffectInfo)
        {
            Corpse corpse = ObjectAccessor.GetCorpse(spell.GetCaster(), TargetGUID);
            if (corpse == null)
                return;

            spell.CallScriptBeforeHitHandlers(SpellMissInfo.None);

            spell.HandleEffects(null, null, null, corpse, spellEffectInfo, SpellEffectHandleMode.HitTarget);

            spell.CallScriptOnHitHandlers();
            spell.CallScriptAfterHitHandlers();
        }
    }

    public class SpellValue
    {
        public SpellValue(SpellInfo proto, WorldObject caster)
        {
            foreach (var spellEffectInfo in proto.GetEffects())
                EffectBasePoints[spellEffectInfo.EffectIndex] = spellEffectInfo.CalcBaseValue(caster, null, 0, -1);

            CustomBasePointsMask = 0;
            MaxAffectedTargets = proto.MaxAffectedTargets;
            RadiusMod = 1.0f;
            AuraStackAmount = 1;
            CriticalChance = 0.0f;
            DurationMul = 1;
        }

        public int[] EffectBasePoints = new int[SpellConst.MaxEffects];
        public uint CustomBasePointsMask;
        public uint MaxAffectedTargets;
        public float RadiusMod;
        public int AuraStackAmount;
        public float DurationMul;
        public float CriticalChance;
        public int? Duration;
        public int? ParentSpellTargetCount;
        public int? ParentSpellTargetIndex;
    }

    // Spell modifier (used for modify other spells)
    public class SpellModifier
    {
        public SpellModifier(Aura _ownerAura)
        {
            op = SpellModOp.HealingAndDamage;
            type = SpellModType.Flat;
            spellId = 0;
            ownerAura = _ownerAura;
        }

        public SpellModOp op { get; set; }
        public SpellModType type { get; set; }
        public uint spellId { get; set; }
        public Aura ownerAura { get; set; }
    }

    public class SpellModifierByClassMask : SpellModifier
    {
        public SpellModifierByClassMask(Aura _ownerAura) : base(_ownerAura)
        {
            value = 0;
            mask = new FlagArray128();
        }

        public int value;
        public FlagArray128 mask;
    }

    public class SpellFlatModifierByLabel : SpellModifier
    {
        public SpellFlatModByLabel value = new();

        public SpellFlatModifierByLabel(Aura _ownerAura) : base(_ownerAura) { }
    }

    class SpellPctModifierByLabel : SpellModifier
    {
        public SpellPctModByLabel value = new();

        public SpellPctModifierByLabel(Aura _ownerAura) : base(_ownerAura) { }
    }

    public class WorldObjectSpellTargetCheck : ICheck<WorldObject>
    {
        internal WorldObject _caster;
        WorldObject _referer;
        internal SpellInfo _spellInfo;
        SpellTargetCheckTypes _targetSelectionType;
        ConditionSourceInfo _condSrcInfo;
        List<Condition> _condList;
        SpellTargetObjectTypes _objectType;

        public WorldObjectSpellTargetCheck(WorldObject caster, WorldObject referer, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList, SpellTargetObjectTypes objectType)
        {
            _caster = caster;
            _referer = referer;
            _spellInfo = spellInfo;
            _targetSelectionType = selectionType;
            _condList = condList;
            _objectType = objectType;

            if (condList != null)
                _condSrcInfo = new ConditionSourceInfo(null, caster);
        }

        public virtual bool Invoke(WorldObject target)
        {
            if (_spellInfo.CheckTarget(_caster, target, true) != SpellCastResult.SpellCastOk)
                return false;

            Unit unitTarget = target.ToUnit();
            Corpse corpseTarget = target.ToCorpse();
            if (corpseTarget != null)
            {
                // use owner for party/assistance checks
                Player owner = Global.ObjAccessor.FindPlayer(corpseTarget.GetOwnerGUID());
                if (owner != null)
                    unitTarget = owner;
                else
                    return false;
            }

            Unit refUnit = _referer.ToUnit();
            if (unitTarget != null)
            {
                // do only faction checks here
                switch (_targetSelectionType)
                {
                    case SpellTargetCheckTypes.Enemy:
                        if (unitTarget.IsTotem())
                            return false;
                        // TODO: restore IsValidAttackTarget for corpses using corpse owner (faction, etc)
                        if (!target.IsCorpse() && !_caster.IsValidAttackTarget(unitTarget, _spellInfo))
                            return false;
                        break;
                    case SpellTargetCheckTypes.Ally:
                        if (unitTarget.IsTotem())
                            return false;
                        // TODO: restore IsValidAttackTarget for corpses using corpse owner (faction, etc)
                        if (!target.IsCorpse() && !_caster.IsValidAssistTarget(unitTarget, _spellInfo))
                            return false;
                        break;
                    case SpellTargetCheckTypes.Party:
                        if (refUnit == null)
                            return false;
                        if (unitTarget.IsTotem())
                            return false;
                        // TODO: restore IsValidAttackTarget for corpses using corpse owner (faction, etc)
                        if (!target.IsCorpse() && !_caster.IsValidAssistTarget(unitTarget, _spellInfo))
                            return false;
                        if (!refUnit.IsInPartyWith(unitTarget))
                            return false;
                        break;
                    case SpellTargetCheckTypes.RaidClass:
                        if (refUnit == null)
                            return false;
                        if (refUnit.GetClass() != unitTarget.GetClass())
                            return false;
                        goto case SpellTargetCheckTypes.Raid;
                    case SpellTargetCheckTypes.Raid:
                        if (refUnit == null)
                            return false;
                        if (unitTarget.IsTotem())
                            return false;
                        // TODO: restore IsValidAttackTarget for corpses using corpse owner (faction, etc)
                        if (!target.IsCorpse() && !_caster.IsValidAssistTarget(unitTarget, _spellInfo))
                            return false;
                        if (!refUnit.IsInRaidWith(unitTarget))
                            return false;
                        break;
                    case SpellTargetCheckTypes.Summoned:
                        if (!unitTarget.IsSummon())
                            return false;
                        if (unitTarget.ToTempSummon().GetSummonerGUID() != _caster.GetGUID())
                            return false;
                        break;
                    default:
                        break;
                }

                switch (_objectType)
                {
                    case SpellTargetObjectTypes.Corpse:
                    case SpellTargetObjectTypes.CorpseAlly:
                    case SpellTargetObjectTypes.CorpseEnemy:
                        if (unitTarget.IsAlive())
                            return false;
                        break;
                    default:
                        break;
                }
            }
            if (_condSrcInfo == null)
                return true;

            _condSrcInfo.mConditionTargets[0] = target;
            return Global.ConditionMgr.IsObjectMeetToConditions(_condSrcInfo, _condList);
        }
    }

    public class WorldObjectSpellNearbyTargetCheck : WorldObjectSpellTargetCheck
    {
        float _range;
        Position _position;

        public WorldObjectSpellNearbyTargetCheck(float range, WorldObject caster, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList, SpellTargetObjectTypes objectType)
            : base(caster, caster, spellInfo, selectionType, condList, objectType)
        {
            _range = range;
            _position = caster.GetPosition();
        }

        public override bool Invoke(WorldObject target)
        {
            float dist = target.GetDistance(_position);
            if (dist < _range && base.Invoke(target))
            {
                _range = dist;
                return true;
            }
            return false;
        }
    }

    public class WorldObjectSpellAreaTargetCheck : WorldObjectSpellTargetCheck
    {
        float _range;
        Position _position;
        WorldObjectSpellAreaTargetSearchReason _searchReason;

        public WorldObjectSpellAreaTargetCheck(float range, Position position, WorldObject caster, WorldObject referer, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList, SpellTargetObjectTypes objectType,
            WorldObjectSpellAreaTargetSearchReason searchReason = WorldObjectSpellAreaTargetSearchReason.Area)
            : base(caster, referer, spellInfo, selectionType, condList, objectType)
        {
            _range = range;
            _position = position;
            _searchReason = searchReason;
        }

        public override bool Invoke(WorldObject target)
        {
            GameObject gameObjectTarget = target.ToGameObject();
            if (gameObjectTarget != null)
            {
                // isInRange including the dimension of the GO
                bool isInRange = gameObjectTarget.IsInRange(_position.GetPositionX(), _position.GetPositionY(), _position.GetPositionZ(), _range);
                if (!isInRange)
                    return false;
            }
            else
            {
                bool isInsideCylinder = target.IsWithinDist2d(_position, _range) && Math.Abs(target.GetPositionZ() - _position.GetPositionZ()) <= _range;
                if (!isInsideCylinder)
                    return false;

                Unit unitTarget = target.ToUnit();
                if (unitTarget != null)
                {
                    switch (_searchReason)
                    {
                        case WorldObjectSpellAreaTargetSearchReason.Area:
                            if (!_spellInfo.HasAttribute(SpellAttr8.CanHitAoeUntargetable) && unitTarget.GetSpellOtherImmunityMask().HasFlag(SpellOtherImmunity.AoETarget))
                                return false;
                            break;
                        case WorldObjectSpellAreaTargetSearchReason.Chain:
                            if (unitTarget.GetSpellOtherImmunityMask().HasFlag(SpellOtherImmunity.ChainTarget))
                                return false;
                            break;
                        default:
                            break;
                    }
                }
            }

            return base.Invoke(target);
        }
    }

    public class WorldObjectSpellConeTargetCheck : WorldObjectSpellAreaTargetCheck
    {
        Position _coneSrc;
        float _coneAngle;
        float _lineWidth;

        public WorldObjectSpellConeTargetCheck(Position coneSrc, float coneAngle, float lineWidth, float range, WorldObject caster, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList, SpellTargetObjectTypes objectType)
            : base(range, caster.GetPosition(), caster, caster, spellInfo, selectionType, condList, objectType)
        {
            _coneSrc = coneSrc;
            _coneAngle = coneAngle;
            _lineWidth = lineWidth;
        }

        public override bool Invoke(WorldObject target)
        {
            if (_spellInfo.HasAttribute(SpellCustomAttributes.ConeBack))
            {
                if (_coneSrc.HasInArc(-Math.Abs(_coneAngle), target))
                    return false;
            }
            else if (_spellInfo.HasAttribute(SpellCustomAttributes.ConeLine))
            {
                if (!_coneSrc.HasInLine(target, target.GetCombatReach(), _lineWidth))
                    return false;
            }
            else
            {
                if (!_caster.IsUnit() || !_caster.ToUnit().IsWithinBoundaryRadius(target.ToUnit()))
                    // ConeAngle > 0 . select targets in front
                    // ConeAngle < 0 . select targets in back
                    if (_coneSrc.HasInArc(_coneAngle, target) != MathFunctions.fuzzyGe(_coneAngle, 0.0f))
                        return false;
            }
            return base.Invoke(target);
        }
    }

    public class WorldObjectSpellTrajTargetCheck : WorldObjectSpellTargetCheck
    {
        float _range;
        Position _position;

        public WorldObjectSpellTrajTargetCheck(float range, Position position, WorldObject caster, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList, SpellTargetObjectTypes objectType)
            : base(caster, caster, spellInfo, selectionType, condList, objectType)
        {
            _range = range;
            _position = position;
        }

        public override bool Invoke(WorldObject target)
        {
            // return all targets on missile trajectory (0 - size of a missile)
            if (!_caster.HasInLine(target, target.GetCombatReach(), SpellConst.TrajectoryMissileSize))
                return false;

            if (target.GetExactDist2d(_position) > _range)
                return false;

            return base.Invoke(target);
        }
    }

    public class WorldObjectSpellLineTargetCheck : WorldObjectSpellAreaTargetCheck
    {
        Position _position;
        float _lineWidth;

        public WorldObjectSpellLineTargetCheck(Position srcPosition, Position dstPosition, float lineWidth, float range, WorldObject caster, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList, SpellTargetObjectTypes objectType)
            : base(range, caster, caster, caster, spellInfo, selectionType, condList, objectType)
        {
            _position = srcPosition;
            _lineWidth = lineWidth;

            if (dstPosition != null && srcPosition != dstPosition)
                _position.SetOrientation(srcPosition.GetAbsoluteAngle(dstPosition));
        }

        public override bool Invoke(WorldObject target)
        {
            if (!_position.HasInLine(target, target.GetCombatReach(), _lineWidth))
                return false;

            return base.Invoke(target);
        }
    }

    public class SpellEvent : BasicEvent
    {
        public SpellEvent(Spell spell)
        {
            m_Spell = spell;
        }

        public override bool Execute(ulong e_time, uint p_time)
        {
            // update spell if it is not finished
            if (m_Spell.GetState() != SpellState.Finished)
                m_Spell.Update(p_time);

            // check spell state to process
            switch (m_Spell.GetState())
            {
                case SpellState.Finished:
                {
                    // spell was finished, check deletable state
                    if (m_Spell.IsDeletable())
                    {
                        // check, if we do have unfinished triggered spells
                        return true;                                // spell is deletable, finish event
                    }
                    // event will be re-added automatically at the end of routine)
                    break;
                }
                case SpellState.Delayed:
                {
                    // first, check, if we have just started
                    if (m_Spell.GetDelayStart() != 0)
                    {
                        // run the spell handler and think about what we can do next
                        ulong t_offset = e_time - m_Spell.GetDelayStart();
                        ulong n_offset = m_Spell.HandleDelayed(t_offset);
                        if (n_offset != 0)
                        {
                            // re-add us to the queue
                            m_Spell.GetCaster().m_Events.AddEvent(this, TimeSpan.FromMilliseconds(m_Spell.GetDelayStart() + n_offset), false);
                            return false;                       // event not complete
                        }
                        // event complete
                        // finish update event will be re-added automatically at the end of routine)
                    }
                    else
                    {
                        // delaying had just started, record the moment
                        m_Spell.SetDelayStart(e_time);
                        // handle effects on caster if the spell has travel time but also affects the caster in some way
                        ulong n_offset = m_Spell.HandleDelayed(0);
                        if (m_Spell.m_spellInfo.LaunchDelay != 0)
                            Cypher.Assert(n_offset == (ulong)Math.Floor(m_Spell.m_spellInfo.LaunchDelay * 1000.0f));
                        else
                            Cypher.Assert(n_offset == m_Spell.GetDelayMoment(), $"{n_offset} == {m_Spell.GetDelayMoment()}");

                        // re-plan the event for the delay moment
                        m_Spell.GetCaster().m_Events.AddEvent(this, TimeSpan.FromMilliseconds(e_time + n_offset), false);
                        return false;                               // event not complete
                    }
                    break;
                }
                default:
                {
                    // all other states
                    // event will be re-added automatically at the end of routine)
                    break;
                }
            }

            // spell processing not complete, plan event on the next update interval
            m_Spell.GetCaster().m_Events.AddEvent(this, TimeSpan.FromMilliseconds(e_time + 1), false);
            return false;                                           // event not complete
        }

        public override void Abort(ulong e_time)
        {
            // oops, the spell we try to do is aborted
            if (m_Spell.GetState() != SpellState.Finished)
                m_Spell.Cancel();
        }

        public override bool IsDeletable()
        {
            return m_Spell.IsDeletable();
        }

        public Spell GetSpell() { return m_Spell; }

        public string GetDebugInfo() { return m_Spell.GetDebugInfo(); }

        Spell m_Spell;
    }

    class ProcReflectDelayed : BasicEvent
    {
        public ProcReflectDelayed(Unit owner, ObjectGuid casterGuid)
        {
            _victim = owner;
            _casterGuid = casterGuid;
        }

        public override bool Execute(ulong e_time, uint p_time)
        {
            Unit caster = Global.ObjAccessor.GetUnit(_victim, _casterGuid);
            if (caster == null)
                return true;

            ProcFlags typeMaskActor = ProcFlags.None;
            ProcFlags typeMaskActionTarget = ProcFlags.TakeHarmfulSpell | ProcFlags.TakeHarmfulAbility;
            ProcFlagsSpellType spellTypeMask = ProcFlagsSpellType.Damage | ProcFlagsSpellType.NoDmgHeal;
            ProcFlagsSpellPhase spellPhaseMask = ProcFlagsSpellPhase.None;
            ProcFlagsHit hitMask = ProcFlagsHit.Reflect;

            Unit.ProcSkillsAndAuras(caster, _victim, new ProcFlagsInit(typeMaskActor), new ProcFlagsInit(typeMaskActionTarget), spellTypeMask, spellPhaseMask, hitMask, null, null, null);
            return true;
        }

        Unit _victim;
        ObjectGuid _casterGuid;
    }

    public class CastSpellTargetArg
    {
        public SpellCastTargets Targets;

        public CastSpellTargetArg() { Targets = new(); }

        public CastSpellTargetArg(WorldObject target)
        {
            if (target != null)
            {
                Unit unitTarget = target.ToUnit();
                if (unitTarget != null)
                {
                    Targets = new();
                    Targets.SetUnitTarget(unitTarget);
                }
                else
                {
                    GameObject goTarget = target.ToGameObject();
                    if (goTarget != null)
                    {
                        Targets = new();
                        Targets.SetGOTarget(goTarget);
                    }
                    // error when targeting anything other than units and gameobjects
                }
            }
            else
                Targets = new(); // nullptr is allowed
        }

        public CastSpellTargetArg(Item itemTarget)
        {
            Targets = new();
            Targets.SetItemTarget(itemTarget);
        }

        public CastSpellTargetArg(Position dest)
        {
            Targets = new();
            Targets.SetDst(dest);
        }

        public CastSpellTargetArg(SpellDestination dest)
        {
            Targets = new();
            Targets.SetDst(dest);
        }

        public CastSpellTargetArg(SpellCastTargets targets)
        {
            Targets = new();
            Targets = targets;
        }

        public static implicit operator CastSpellTargetArg(WorldObject target)
        {
            return new CastSpellTargetArg(target);
        }

        public static implicit operator CastSpellTargetArg(Item itemTarget)
        {
            return new CastSpellTargetArg(itemTarget);
        }

        public static implicit operator CastSpellTargetArg(Position dest)
        {
            return new CastSpellTargetArg(dest);
        }

        public static implicit operator CastSpellTargetArg(SpellDestination dest)
        {
            return new CastSpellTargetArg(dest);
        }

        public static implicit operator CastSpellTargetArg(SpellCastTargets targets)
        {
            return new CastSpellTargetArg(targets);
        }
    }

    public class CastSpellExtraArgsInit
    {
        public TriggerCastFlags TriggerFlags;
        public Difficulty CastDifficulty;
        public Item CastItem;
        public Spell TriggeringSpell;
        public AuraEffect TriggeringAura;
        public ObjectGuid OriginalCaster;
        public ObjectGuid OriginalCastId;
        public int? OriginalCastItemLevel;
        public SpellValueOverride Value;

        public List<SpellValueOverride> SpellValueOverrides = new();
        public object CustomArg;
        public ActionResultSetter<SpellCastResult> ScriptResult;
        public bool ScriptWaitsForSpellHit;

        public struct SpellValueOverride
        {
            public SpellValueOverride(SpellValueMod mod, int val)
            {
                Type = (int)mod;
                IntValue = val;
            }
            public SpellValueOverride(SpellValueModFloat mod, float val)
            {
                Type = (int)mod;
                FloatValue = val;
            }

            public int Type;
            public float FloatValue;
            public int IntValue;
        }
    }

    public class CastSpellExtraArgs : CastSpellExtraArgsInit
    {
        public CastSpellExtraArgs() { }

        public CastSpellExtraArgs(bool triggered)
        {
            TriggerFlags = triggered ? TriggerCastFlags.FullMask : TriggerCastFlags.None;
        }

        public CastSpellExtraArgs(TriggerCastFlags trigger)
        {
            TriggerFlags = trigger;
        }

        public CastSpellExtraArgs(Item item)
        {
            TriggerFlags = TriggerCastFlags.FullMask;
            CastItem = item;
        }

        public CastSpellExtraArgs(Spell triggeringSpell)
        {
            TriggerFlags = TriggerCastFlags.FullMask;
            SetTriggeringSpell(triggeringSpell);
        }

        public CastSpellExtraArgs(AuraEffect eff)
        {
            TriggerFlags = TriggerCastFlags.FullMask;
            SetTriggeringAura(eff);
        }

        public CastSpellExtraArgs(Difficulty castDifficulty)
        {
            CastDifficulty = castDifficulty;
        }

        public CastSpellExtraArgs(SpellValueMod mod, int val)
        {
            SpellValueOverrides.Add(new SpellValueOverride(mod, val));
        }

        public CastSpellExtraArgs(SpellValueModFloat mod, float val) 
        { 
            SpellValueOverrides.Add(new SpellValueOverride(mod, val));
        }

        public CastSpellExtraArgs SetTriggerFlags(TriggerCastFlags flag)
        {
            TriggerFlags = flag;
            return this;
        }

        public CastSpellExtraArgs SetCastItem(Item item)
        {
            CastItem = item;
            return this;
        }

        public CastSpellExtraArgs SetTriggeringSpell(Spell triggeringSpell)
        {
            TriggeringSpell = triggeringSpell;
            if (triggeringSpell != null)
            {
                if (!OriginalCastItemLevel.HasValue)
                    OriginalCastItemLevel = triggeringSpell.m_castItemLevel;
                if (OriginalCastId.IsEmpty())
                    OriginalCastId = triggeringSpell.m_castId;
            }
            return this;
        }

        public CastSpellExtraArgs SetTriggeringAura(AuraEffect triggeringAura)
        {
            TriggeringAura = triggeringAura;
            return this;
        }

        public CastSpellExtraArgs SetOriginalCaster(ObjectGuid guid)
        {
            OriginalCaster = guid;
            return this;
        }

        public CastSpellExtraArgs SetCastDifficulty(Difficulty castDifficulty)
        {
            CastDifficulty = castDifficulty;
            return this;
        }

        public CastSpellExtraArgs SetOriginalCastId(ObjectGuid castId)
        {
            OriginalCastId = castId;
            return this;
        }

        public CastSpellExtraArgs AddSpellMod(SpellValueMod mod, int val)
        {
            SpellValueOverrides.Add(new SpellValueOverride(mod, val));
            return this;
        }

        public CastSpellExtraArgs AddSpellMod(SpellValueModFloat mod, float val)
        {
            SpellValueOverrides.Add(new SpellValueOverride(mod, val));
            return this;
        }

        public CastSpellExtraArgs SetCustomArg(object customArg)
        {
            CustomArg = customArg;
            return this;
        }

        public CastSpellExtraArgs SetScriptResult(ActionResultSetter<SpellCastResult> scriptResult)
        {
            ScriptResult = scriptResult;
            return this;
        }
        public CastSpellExtraArgs SetScriptWaitsForSpellHit(bool scriptWaitsForSpellHit)
        {
            ScriptWaitsForSpellHit = scriptWaitsForSpellHit;
            return this;
        }

        public static implicit operator CastSpellExtraArgs(bool triggered) => new CastSpellExtraArgs(triggered);

        public static implicit operator CastSpellExtraArgs(TriggerCastFlags trigger) => new CastSpellExtraArgs(trigger);

        public static implicit operator CastSpellExtraArgs(Item item) => new CastSpellExtraArgs(item);

        public static implicit operator CastSpellExtraArgs(Spell triggeringSpell) => new CastSpellExtraArgs(triggeringSpell);

        public static implicit operator CastSpellExtraArgs(AuraEffect eff) => new CastSpellExtraArgs(eff);

        public static implicit operator CastSpellExtraArgs(Difficulty castDifficulty) => new CastSpellExtraArgs(castDifficulty);
    }

    public class SpellLogEffect
    {
        public int Effect;

        public List<SpellLogEffectPowerDrainParams> PowerDrainTargets = new();
        public List<SpellLogEffectExtraAttacksParams> ExtraAttacksTargets = new();
        public List<SpellLogEffectDurabilityDamageParams> DurabilityDamageTargets = new();
        public List<SpellLogEffectGenericVictimParams> GenericVictimTargets = new();
        public List<SpellLogEffectTradeSkillItemParams> TradeSkillTargets = new();
        public List<SpellLogEffectFeedPetParams> FeedPetTargets = new();
    }

    public class ProcFlagsInit : FlagsArray<int>
    {
        public ProcFlagsInit(ProcFlags procFlags = 0, ProcFlags2 procFlags2 = 0) : base(2)
        {
            _storage[0] = (int)procFlags;
            _storage[1] = (int)procFlags2;
        }

        public ProcFlagsInit(params int[] flags) : base(flags) { }

        public ProcFlagsInit Or(ProcFlags procFlags)
        {
            _storage[0] |= (int)procFlags;
            return this;
        }

        public ProcFlagsInit Or(ProcFlags2 procFlags2)
        {
            _storage[1] |= (int)procFlags2;
            return this;
        }

        public bool HasFlag(ProcFlags procFlags)
        {
            return (_storage[0] & (int)procFlags) != 0;
        }

        public bool HasFlag(ProcFlags2 procFlags)
        {
            return (_storage[1] & (int)procFlags) != 0;
        }
    }

    public class EmpowerData
    {
        public TimeSpan MinHoldTime;
        public List<TimeSpan> StageDurations = new();
        public int CompletedStages = 0;
        public bool IsReleasedByClient;
        public bool IsReleased;
    }
}