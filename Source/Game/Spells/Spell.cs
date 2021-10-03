/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Framework.Dynamic;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.Conditions;
using Game.DataStorage;
using Game.Entities;
using Game.Loots;
using Game.Maps;
using Game.Movement;
using Game.Networking.Packets;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Game.Spells
{
    public partial class Spell : IDisposable
    {
        public Spell(WorldObject caster, SpellInfo info, TriggerCastFlags triggerFlags, ObjectGuid originalCasterGUID = default)
        {
            m_spellInfo = info;
            m_caster = (info.HasAttribute(SpellAttr6.CastByCharmer) && caster.GetCharmerOrOwner() != null ? caster.GetCharmerOrOwner() : caster);
            m_spellValue = new SpellValue(m_spellInfo, caster);
            m_castItemLevel = -1;
            m_castId = ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, m_caster.GetMapId(), m_spellInfo.Id, m_caster.GetMap().GenerateLowGuid(HighGuid.Cast));
            m_SpellVisual.SpellXSpellVisualID = caster.GetCastSpellXSpellVisualId(m_spellInfo);

            m_customError = SpellCustomErrors.None;
            m_fromClient = false;
            m_needComboPoints = m_spellInfo.NeedsComboPoints();

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

            m_spellState = SpellState.None;
            _triggeredCastFlags = triggerFlags;
            if (m_spellInfo.HasAttribute(SpellAttr4.CanCastWhileCasting))
                _triggeredCastFlags = _triggeredCastFlags | TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.CastDirectly;

            effectHandleMode = SpellEffectHandleMode.Launch;

            //Auto Shot & Shoot (wand)
            m_autoRepeat = m_spellInfo.IsAutoRepeatRangedSpell();

            // Determine if spell can be reflected back to the caster
            // Patch 1.2 notes: Spell Reflection no longer reflects abilities
            m_canReflect = caster.IsUnit() && m_spellInfo.DmgClass == SpellDmgClass.Magic && !m_spellInfo.HasAttribute(SpellAttr0.Ability)
                && !m_spellInfo.HasAttribute(SpellAttr1.CantBeReflected) && !m_spellInfo.HasAttribute(SpellAttr0.UnaffectedByInvulnerability)
                && !m_spellInfo.IsPassive();

            CleanupTargetList();

            for (var i = 0; i < SpellConst.MaxEffects; ++i)
                m_destTargets[i] = new SpellDestination(m_caster);

            //not sure needed.
            m_targets = new SpellCastTargets();
            m_appliedMods = new List<Aura>();
        }

        public virtual void Dispose()
        {
            // unload scripts
            for (var i = 0; i < m_loadedScripts.Count; ++i)
                m_loadedScripts[i]._Unload();

            if (m_referencedFromCurrentSpell && m_selfContainer && m_selfContainer == this)
            {
                // Clean the reference to avoid later crash.
                // If this error is repeating, we may have to add an ASSERT to better track down how we get into this case.
                Log.outError(LogFilter.Spells, "SPELL: deleting spell for spell ID {0}. However, spell still referenced.", m_spellInfo.Id);
                m_selfContainer = null;
            }

            if (m_caster && m_caster.GetTypeId() == TypeId.Player)
                Cypher.Assert(m_caster.ToPlayer().m_spellModTakingSpell != this);
        }

        void InitExplicitTargets(SpellCastTargets targets)
        {
            m_targets = targets;
            m_targets.SetOrigUnitTarget(m_targets.GetUnitTarget());
            // this function tries to correct spell explicit targets for spell
            // client doesn't send explicit targets correctly sometimes - we need to fix such spells serverside
            // this also makes sure that we correctly send explicit targets to client (removes redundant data)
            SpellCastTargetFlags neededTargets = m_spellInfo.GetExplicitTargetMask();

            WorldObject target = m_targets.GetObjectTarget();
            if (target != null)
            {
                // check if object target is valid with needed target flags
                // for unit case allow corpse target mask because player with not released corpse is a unit target
                if ((target.ToUnit() && !neededTargets.HasAnyFlag(SpellCastTargetFlags.UnitMask | SpellCastTargetFlags.CorpseMask))
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

                SelectEffectImplicitTargets(spellEffectInfo, spellEffectInfo.TargetA, ref processedAreaEffectsMask);
                SelectEffectImplicitTargets(spellEffectInfo, spellEffectInfo.TargetB, ref processedAreaEffectsMask);

                // Select targets of effect based on effect type
                // those are used when no valid target could be added for spell effect based on spell target type
                // some spell effects use explicit target as a default target added to target map (like SPELL_EFFECT_LEARN_SPELL)
                // some spell effects add target to target map only when target type specified (like SPELL_EFFECT_WEAPON)
                // some spell effects don't add anything to target map (confirmed with sniffs) (like SPELL_EFFECT_DESTROY_ALL_TOTEMS)
                SelectEffectTypeImplicitTargets(spellEffectInfo);

                if (m_targets.HasDst())
                    AddDestTarget(m_targets.GetDst(), spellEffectInfo.EffectIndex);

                if (m_spellInfo.IsChanneled())
                {
                    // maybe do this for all spells?
                    if (focusObject == null && m_UniqueTargetInfo.Empty() && m_UniqueGOTargetInfo.Empty() && m_UniqueItemInfo.Empty() && !m_targets.HasDst())
                    {
                        SendCastResult(SpellCastResult.BadImplicitTargets);
                        Finish(false);
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
                else if (m_spellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
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
            m_delayMoment = CalculateDelayMomentForDst(0.0f);
            m_caster.m_Events.ModifyEventTime(_spellEvent, GetDelayStart() + m_delayMoment);
        }

        void SelectEffectImplicitTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, ref uint processedEffectMask)
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
                            spellEffectInfo.CalcRadius(m_caster) == effects[j].CalcRadius(m_caster) &&
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
                    SelectImplicitNearbyTargets(spellEffectInfo, targetType, effectMask);
                    break;
                case SpellTargetSelectionCategories.Cone:
                    SelectImplicitConeTargets(spellEffectInfo, targetType, effectMask);
                    break;
                case SpellTargetSelectionCategories.Area:
                    SelectImplicitAreaTargets(spellEffectInfo, targetType, effectMask);
                    break;
                case SpellTargetSelectionCategories.Traj:
                    // just in case there is no dest, explanation in SelectImplicitDestDestTargets
                    CheckDst();

                    SelectImplicitTrajTargets(spellEffectInfo, targetType);
                    break;
                case SpellTargetSelectionCategories.Line:
                    SelectImplicitLineTargets(spellEffectInfo, targetType, effectMask);
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
                                    SelectImplicitCasterDestTargets(spellEffectInfo, targetType);
                                    break;
                                case SpellTargetReferenceTypes.Target:
                                    SelectImplicitTargetDestTargets(spellEffectInfo, targetType);
                                    break;
                                case SpellTargetReferenceTypes.Dest:
                                    SelectImplicitDestDestTargets(spellEffectInfo, targetType);
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
                        Unit unitTarget = target ? target.ToUnit() : null;
                        if (unitTarget)
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
                            if (target)
                            {
                                SpellDestination dest = new(target);
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
                    CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
                    m_targets.SetDst(dest);
                    break;
                }
                default:
                    Cypher.Assert(false, "Spell.SelectImplicitChannelTargets: received not implemented target type");
                    break;
            }
        }

        void SelectImplicitNearbyTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, uint effMask)
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
                                Finish(false);
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
                                CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
                                m_targets.SetDst(dest);
                            }
                            else
                            {
                                SendCastResult(SpellCastResult.BadImplicitTargets);
                                Finish(false);
                            }
                            return;
                        }
                        break;
                    default:
                        break;
                }
            }

            WorldObject target = SearchNearbyTarget(range, targetType.GetObjectType(), targetType.GetCheckType(), condList);
            if (target == null)
            {
                Log.outDebug(LogFilter.Spells, "Spell.SelectImplicitNearbyTargets: cannot find nearby target for spell ID {0}, effect {1}", m_spellInfo.Id, spellEffectInfo.EffectIndex);
                SendCastResult(SpellCastResult.BadImplicitTargets);
                Finish(false);
                return;
            }

            CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, targetType);
            if (!target)
            {
                Log.outDebug(LogFilter.Spells, $"Spell.SelectImplicitNearbyTargets: OnObjectTargetSelect script hook for spell Id {m_spellInfo.Id} set NULL target, effect {spellEffectInfo.EffectIndex}");
                SendCastResult(SpellCastResult.BadImplicitTargets);
                Finish(false);
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
                        Finish(false);
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
                        Finish(false);
                        return;
                    }
                    break;
                case SpellTargetObjectTypes.Dest:
                    SpellDestination dest = new(target);
                    CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
                    m_targets.SetDst(dest);
                    break;
                default:
                    Cypher.Assert(false, "Spell.SelectImplicitNearbyTargets: received not implemented target object type");
                    break;
            }

            SelectImplicitChainTargets(spellEffectInfo, targetType, target, effMask);
        }

        void SelectImplicitConeTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, uint effMask)
        {
            if (targetType.GetReferenceType() != SpellTargetReferenceTypes.Caster)
            {
                Cypher.Assert(false, "Spell.SelectImplicitConeTargets: received not implemented target reference type");
                return;
            }
            List<WorldObject> targets = new();
            SpellTargetObjectTypes objectType = targetType.GetObjectType();
            SpellTargetCheckTypes selectionType = targetType.GetCheckType();

            var condList = spellEffectInfo.ImplicitTargetConditions;
            float radius = spellEffectInfo.CalcRadius(m_caster) * m_spellValue.RadiusMod;

            GridMapTypeMask containerTypeMask = GetSearcherTypeMask(objectType, condList);
            if (containerTypeMask != 0)
            {
                var spellCone = new WorldObjectSpellConeTargetCheck(MathFunctions.DegToRad(m_spellInfo.ConeAngle), m_spellInfo.Width != 0 ? m_spellInfo.Width : m_caster.GetCombatReach(), radius, m_caster, m_spellInfo, selectionType, condList, objectType);
                var searcher = new WorldObjectListSearcher(m_caster, targets, spellCone, containerTypeMask);
                SearchTargets(searcher, containerTypeMask, m_caster, m_caster.GetPosition(), radius);

                CallScriptObjectAreaTargetSelectHandlers(targets, spellEffectInfo.EffectIndex, targetType);

                if (!targets.Empty())
                {
                    // Other special target selection goes here
                    uint maxTargets = m_spellValue.MaxAffectedTargets;
                    if (maxTargets != 0)
                        targets.RandomResize(maxTargets);

                    foreach (var obj in targets)
                    {
                        Unit unitTarget = obj.ToUnit();
                        GameObject gObjTarget = obj.ToGameObject();
                        if (unitTarget)
                            AddUnitTarget(unitTarget, effMask, false);
                        else if (gObjTarget)
                            AddGOTarget(gObjTarget, effMask);
                    }
                }
            }
        }

        void SelectImplicitAreaTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, uint effMask)
        {
            WorldObject referer = null;
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
            List<WorldObject> targets = new();

            switch (targetType.GetTarget())
            {
                case Targets.UnitTargetAllyOrRaid:
                    Unit targetedUnit = m_targets.GetUnitTarget();
                    if (targetedUnit != null)
                    {
                        if (!m_caster.IsUnit() || !m_caster.ToUnit().IsInRaidWith(targetedUnit))
                        {
                            targets.Add(m_targets.GetUnitTarget());

                            CallScriptObjectAreaTargetSelectHandlers(targets, spellEffectInfo.EffectIndex, targetType);

                            if (!targets.Empty())
                            {
                                // Other special target selection goes here
                                uint maxTargets = m_spellValue.MaxAffectedTargets;
                                if (maxTargets != 0)
                                    targets.RandomResize(maxTargets);

                                foreach (WorldObject target in targets)
                                {
                                    Unit unit = target.ToUnit();
                                    if (unit != null)
                                        AddUnitTarget(unit, effMask, false, true, center);
                                    else
                                    {
                                        GameObject gObjTarget = target.ToGameObject();
                                        if (gObjTarget != null)
                                            AddGOTarget(gObjTarget, effMask);
                                    }
                                }
                            }

                            return;
                        }

                        center = targetedUnit;
                    }
                    break;
                case Targets.UnitCasterAndSummons:
                    targets.Add(m_caster);
                    break;
                default:
                    break;
            }

            float radius = spellEffectInfo.CalcRadius(m_caster) * m_spellValue.RadiusMod;

            SearchAreaTargets(targets, radius, center, referer, targetType.GetObjectType(), targetType.GetCheckType(), spellEffectInfo.ImplicitTargetConditions);

            CallScriptObjectAreaTargetSelectHandlers(targets, spellEffectInfo.EffectIndex, targetType);

            if (!targets.Empty())
            {
                // Other special target selection goes here
                uint maxTargets = m_spellValue.MaxAffectedTargets;
                if (maxTargets != 0)
                    targets.RandomResize(maxTargets);

                foreach (var obj in targets)
                {
                    Unit unitTarget = obj.ToUnit();
                    GameObject gObjTarget = obj.ToGameObject();
                    if (unitTarget)
                        AddUnitTarget(unitTarget, effMask, false, true, center);
                    else if (gObjTarget)
                        AddGOTarget(gObjTarget, effMask);
                }
            }
        }

        void SelectImplicitCasterDestTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
        {
            SpellDestination dest = new(m_caster);

            switch (targetType.GetTarget())
            {
                case Targets.DestCaster:
                    break;
                case Targets.DestHome:
                    Player playerCaster = m_caster.ToPlayer();
                    if (playerCaster != null)
                        dest = new SpellDestination(playerCaster.GetHomebind().posX, playerCaster.GetHomebind().posY, playerCaster.GetHomebind().posZ, playerCaster.GetOrientation(), playerCaster.GetHomebind().GetMapId());
                    break;
                case Targets.DestDb:
                    SpellTargetPosition st = Global.SpellMgr.GetSpellTargetPosition(m_spellInfo.Id, spellEffectInfo.EffectIndex);
                    if (st != null)
                    {
                        // @todo fix this check
                        if (m_spellInfo.HasEffect(SpellEffectName.TeleportUnits) || m_spellInfo.HasEffect(SpellEffectName.Bind))
                            dest = new SpellDestination(st.target_X, st.target_Y, st.target_Z, st.target_Orientation, st.target_mapId);
                        else if (st.target_mapId == m_caster.GetMapId())
                            dest = new SpellDestination(st.target_X, st.target_Y, st.target_Z, st.target_Orientation);
                    }
                    else
                    {
                        Log.outDebug(LogFilter.Spells, "SPELL: unknown target coordinates for spell ID {0}", m_spellInfo.Id);
                        WorldObject target = m_targets.GetObjectTarget();
                        if (target)
                            dest = new SpellDestination(target);
                    }
                    break;
                case Targets.DestCasterFishing:
                {
                    float minDist = m_spellInfo.GetMinRange(true);
                    float maxDist = m_spellInfo.GetMaxRange(true);
                    float dis = (float)RandomHelper.NextDouble() * (maxDist - minDist) + minDist;
                    float x, y, z;
                    float angle = (float)RandomHelper.NextDouble() * (MathFunctions.PI * 35.0f / 180.0f) - (float)(Math.PI * 17.5f / 180.0f);
                    m_caster.GetClosePoint(out x, out y, out z, SharedConst.DefaultPlayerBoundingRadius, dis, angle);

                    float ground = m_caster.GetMapHeight(x, y, z);
                    float liquidLevel = MapConst.VMAPInvalidHeightValue;
                    LiquidData liquidData;
                    if (m_caster.GetMap().GetLiquidStatus(m_caster.GetPhaseShift(), x, y, z, LiquidHeaderTypeFlags.AllLiquids, out liquidData, m_caster.GetCollisionHeight()) != 0)
                        liquidLevel = liquidData.level;

                    if (liquidLevel <= ground) // When there is no liquid Map.GetWaterOrGroundLevel returns ground level
                    {
                        SendCastResult(SpellCastResult.NotHere);
                        SendChannelUpdate(0);
                        Finish(false);
                        return;
                    }

                    if (ground + 0.75 > liquidLevel)
                    {
                        SendCastResult(SpellCastResult.TooShallow);
                        SendChannelUpdate(0);
                        Finish(false);
                        return;
                    }

                    dest = new SpellDestination(x, y, liquidLevel, m_caster.GetOrientation());
                    break;
                }
                case Targets.DestCasterFrontLeap:
                {
                    Unit unitCaster = m_caster.ToUnit();
                    if (unitCaster == null)
                        break;

                    float dist = spellEffectInfo.CalcRadius(unitCaster);
                    float angle = targetType.CalcDirectionAngle();

                    Position pos = new(dest.Position);

                    unitCaster.MovePositionToFirstCollision(pos, dist, angle);
                    // Generate path to that point.
                    if (m_preGeneratedPath == null)
                        m_preGeneratedPath = new(unitCaster);

                    m_preGeneratedPath.SetPathLengthLimit(dist);

                    // Should we use straightline here ? What do we do when we don't have a full path ?
                    bool pathResult = m_preGeneratedPath.CalculatePath(pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), false, true);
                    if (pathResult && m_preGeneratedPath.GetPathType().HasAnyFlag(PathType.Normal | PathType.Shortcut))
                    {
                        pos.posX = m_preGeneratedPath.GetActualEndPosition().X;
                        pos.posY = m_preGeneratedPath.GetActualEndPosition().Y;
                        pos.posZ = m_preGeneratedPath.GetActualEndPosition().Z;
                    }

                    dest.Relocate(pos);
                    break;
                }
                case Targets.DestCasterGround:
                    m_caster.UpdateAllowedPositionZ(dest.Position.GetPositionX(), dest.Position.GetPositionY(), ref dest.Position.posZ);
                    break;
                case Targets.DestSummoner:
                {
                    Unit unitCaster = m_caster.ToUnit();
                    if (unitCaster != null)
                    {
                        TempSummon casterSummon = unitCaster.ToTempSummon();
                        if (casterSummon != null)
                        {
                            Unit summoner = casterSummon.GetSummoner();
                            if (summoner != null)
                                dest = new SpellDestination(summoner);
                        }
                    }
                    break;
                }
                default:
                {
                    float dist = spellEffectInfo.CalcRadius(m_caster);
                    float angl = targetType.CalcDirectionAngle();
                    float objSize = m_caster.GetCombatReach();

                    switch (targetType.GetTarget())
                    {
                        case Targets.DestCasterSummon:
                            dist = SharedConst.PetFollowDist;
                            break;
                        case Targets.DestCasterRandom:
                            if (dist > objSize)
                                dist = objSize + (dist - objSize) * (float)RandomHelper.NextDouble();
                            break;
                        case Targets.DestCasterFrontLeft:
                        case Targets.DestCasterBackLeft:
                        case Targets.DestCasterFrontRight:
                        case Targets.DestCasterBackRight:
                        {
                            float DefaultTotemDistance = 3.0f;
                            if (!spellEffectInfo.HasRadius() && !spellEffectInfo.HasMaxRadius())
                                dist = DefaultTotemDistance;
                            break;
                        }
                        default:
                            break;
                    }

                    if (dist < objSize)
                        dist = objSize;

                    Position pos = new(dest.Position);
                    m_caster.MovePositionToFirstCollision(pos, dist, angl);

                    dest.Relocate(pos);
                    break;
                }
            }
            CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
            m_targets.SetDst(dest);
        }

        void SelectImplicitTargetDestTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
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
                    float dist = spellEffectInfo.CalcRadius(null);
                    if (targetType.GetTarget() == Targets.DestRandom)
                        dist *= (float)RandomHelper.NextDouble();

                    Position pos = new(dest.Position);
                    target.MovePositionToFirstCollision(pos, dist, angle);

                    dest.Relocate(pos);
                }
                break;
            }

            CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
            m_targets.SetDst(dest);
        }

        void SelectImplicitDestDestTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
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
                    return;
                default:
                {
                    float angle = targetType.CalcDirectionAngle();
                    float dist = spellEffectInfo.CalcRadius(m_caster);
                    if (targetType.GetTarget() == Targets.DestRandom)
                        dist *= (float)RandomHelper.NextDouble();

                    Position pos = new(m_targets.GetDstPos());
                    m_caster.MovePositionToFirstCollision(pos, dist, angle);

                    dest.Relocate(pos);
                }
                break;
            }

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
                            target = unitCaster.ToTempSummon().GetSummoner();
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

            if (target)
            {
                Unit unit = target.ToUnit();
                if (unit != null)
                    AddUnitTarget(unit, 1u << (int)spellEffectInfo.EffectIndex, checkIfValid);
                else
                {
                    GameObject go = target.ToGameObject();
                    if (go != null)
                        AddGOTarget(go, 1u << (int)spellEffectInfo.EffectIndex);
                }
            }
        }

        void SelectImplicitTargetObjectTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
        {
            WorldObject target = m_targets.GetObjectTarget();

            CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, targetType);

            Item item = m_targets.GetItemTarget();
            if (target != null)
            {
                if (target.ToUnit())
                    AddUnitTarget(target.ToUnit(), 1u << (int)spellEffectInfo.EffectIndex, true, false);
                else if (target.IsTypeId(TypeId.GameObject))
                    AddGOTarget(target.ToGameObject(), 1u << (int)spellEffectInfo.EffectIndex);

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
            if (modOwner)
                modOwner.ApplySpellMod(m_spellInfo, SpellModOp.ChainTargets, ref maxTargets, this);

            if (maxTargets > 1)
            {
                // mark damage multipliers as used
                for (int k = (int)spellEffectInfo.EffectIndex; k < m_spellInfo.GetEffects().Count; ++k)
                    if (Convert.ToBoolean(effMask & (1 << (int)k)))
                        m_damageMultipliers[spellEffectInfo.EffectIndex] = 1.0f;

                m_applyMultiplierMask |= effMask;

                List<WorldObject> targets = new();
                SearchChainTargets(targets, (uint)maxTargets - 1, target, targetType.GetObjectType(), targetType.GetCheckType(), spellEffectInfo.ImplicitTargetConditions, targetType.GetTarget() == Targets.UnitChainhealAlly);

                // Chain primary target is added earlier
                CallScriptObjectAreaTargetSelectHandlers(targets, spellEffectInfo.EffectIndex, targetType);

                foreach (var obj in targets)
                {
                    Unit unitTarget = obj.ToUnit();
                    if (unitTarget)
                        AddUnitTarget(unitTarget, effMask, false);
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
                if (unitTarget)
                {
                    if (unitCaster == obj || unitCaster.IsOnVehicle(unitTarget) || unitTarget.GetVehicle())
                        continue;

                    Creature creatureTarget = unitTarget.ToCreature();
                    if (creatureTarget)
                    {
                        if (!creatureTarget.GetCreatureTemplate().TypeFlags.HasAnyFlag(CreatureTypeFlags.CanCollideWithMissiles))
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
                CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
                m_targets.ModDst(dest);
            }
        }

        void SelectImplicitLineTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, uint effMask)
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
            float radius = spellEffectInfo.CalcRadius(m_caster) * m_spellValue.RadiusMod;

            GridMapTypeMask containerTypeMask = GetSearcherTypeMask(objectType, condList);
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
                        Unit unit = obj.ToUnit();
                        if (unit != null)
                            AddUnitTarget(unit, effMask, false);
                        else
                        {
                            GameObject gObjTarget = obj.ToGameObject();
                            if (gObjTarget != null)
                                AddGOTarget(gObjTarget, effMask);
                        }
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

                            rafTarget.GetMap().AddFarSpellCallback(map =>
                            {
                                Player player = Global.ObjAccessor.GetPlayer(map, rafTarget.GetGUID());
                                if (player == null)
                                    return;

                                // check immunity again in case it changed during update
                                if (player.IsImmunedToSpellEffect(GetSpellInfo(), spellEffectInfo, null))
                                    return;

                                HandleEffects(player, null, null, spellEffectInfo, SpellEffectHandleMode.HitTarget);
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
                            {
                                // @todo this is a workaround - corpses should be added to spell target map too, but we can't do that so we add owner instead
                                Player owner = Global.ObjAccessor.FindPlayer(corpseTarget.GetOwnerGUID());
                                if (owner != null)
                                    target = owner;
                            }
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
                if (target.ToUnit())
                    AddUnitTarget(target.ToUnit(), 1u << (int)spellEffectInfo.EffectIndex, false);
                else if (target.IsTypeId(TypeId.GameObject))
                    AddGOTarget(target.ToGameObject(), 1u << (int)spellEffectInfo.EffectIndex);
            }
        }

        public GridMapTypeMask GetSearcherTypeMask(SpellTargetObjectTypes objType, List<Condition> condList)
        {
            // this function selects which containers need to be searched for spell target
            GridMapTypeMask retMask = GridMapTypeMask.All;

            // filter searchers based on searched object type
            switch (objType)
            {
                case SpellTargetObjectTypes.Unit:
                case SpellTargetObjectTypes.UnitAndDest:
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
            if (!m_spellInfo.HasAttribute(SpellAttr2.CanTargetDead))
                retMask &= ~GridMapTypeMask.Corpse;
            if (m_spellInfo.HasAttribute(SpellAttr3.OnlyTargetPlayers))
                retMask &= GridMapTypeMask.Corpse | GridMapTypeMask.Player;
            if (m_spellInfo.HasAttribute(SpellAttr3.OnlyTargetGhosts))
                retMask &= GridMapTypeMask.Player;

            if (condList != null)
                retMask &= Global.ConditionMgr.GetSearcherTypeMaskForConditionList(condList);
            return retMask;
        }

        void SearchTargets(Notifier notifier, GridMapTypeMask containerMask, WorldObject referer, Position pos, float radius)
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

        WorldObject SearchNearbyTarget(float range, SpellTargetObjectTypes objectType, SpellTargetCheckTypes selectionType, List<Condition> condList)
        {
            GridMapTypeMask containerTypeMask = GetSearcherTypeMask(objectType, condList);
            if (containerTypeMask == 0)
                return null;
            var check = new WorldObjectSpellNearbyTargetCheck(range, m_caster, m_spellInfo, selectionType, condList, objectType);
            var searcher = new WorldObjectLastSearcher(m_caster, check, containerTypeMask);
            SearchTargets(searcher, containerTypeMask, m_caster, m_caster.GetPosition(), range);
            return searcher.GetTarget();
        }

        void SearchAreaTargets(List<WorldObject> targets, float range, Position position, WorldObject referer, SpellTargetObjectTypes objectType, SpellTargetCheckTypes selectionType, List<Condition> condList)
        {
            var containerTypeMask = GetSearcherTypeMask(objectType, condList);
            if (containerTypeMask == 0)
                return;
            var check = new WorldObjectSpellAreaTargetCheck(range, position, m_caster, referer, m_spellInfo, selectionType, condList, objectType);
            var searcher = new WorldObjectListSearcher(m_caster, targets, check, containerTypeMask);
            SearchTargets(searcher, containerTypeMask, m_caster, position, range);
        }

        void SearchChainTargets(List<WorldObject> targets, uint chainTargets, WorldObject target, SpellTargetObjectTypes objectType, SpellTargetCheckTypes selectType, List<Condition> condList, bool isChainHeal)
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
            if (modOwner)
                modOwner.ApplySpellMod(m_spellInfo, SpellModOp.ChainJumpDistance, ref jumpRadius, this);

            // chain lightning/heal spells and similar - allow to jump at larger distance and go out of los
            bool isBouncingFar = (m_spellInfo.HasAttribute(SpellAttr4.AreaTargetChain)
                || m_spellInfo.DmgClass == SpellDmgClass.None
                || m_spellInfo.DmgClass == SpellDmgClass.Magic);

            // max dist which spell can reach
            float searchRadius = jumpRadius;
            if (isBouncingFar)
                searchRadius *= chainTargets;

            List<WorldObject> tempTargets = new();
            SearchAreaTargets(tempTargets, searchRadius, target.GetPosition(), m_caster, objectType, selectType, condList);
            tempTargets.Remove(target);

            // remove targets which are always invalid for chain spells
            // for some spells allow only chain targets in front of caster (swipe for example)
            if (!isBouncingFar)
            {
                for (var i = 0; i < tempTargets.Count; ++i)
                {
                    var obj = tempTargets[i];
                    if (!m_caster.HasInArc(MathFunctions.PI, obj))
                        tempTargets.Remove(obj);
                }
            }

            while (chainTargets != 0)
            {
                // try to get unit for next chain jump
                WorldObject found = null;
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
                            if ((deficit > maxHPDeficit || found == null) && target.IsWithinDist(unitTarget, jumpRadius) && target.IsWithinLOSInMap(unitTarget, LineOfSightChecks.All, ModelIgnoreFlags.M2))
                            {
                                found = obj;
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
                        if (found == null)
                        {
                            if ((!isBouncingFar || target.IsWithinDist(obj, jumpRadius)) && target.IsWithinLOSInMap(obj, LineOfSightChecks.All, ModelIgnoreFlags.M2))
                                found = obj;
                        }
                        else if (target.GetDistanceOrder(obj, found) && target.IsWithinLOSInMap(obj, LineOfSightChecks.All, ModelIgnoreFlags.M2))
                            found = obj;
                    }
                }
                // not found any valid target - chain ends
                if (found == null)
                    break;
                target = found;
                tempTargets.Remove(found);
                targets.Add(target);
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

            m_procVictim = m_procAttacker = 0;
            // Get data for type of attack and fill base info for trigger
            switch (m_spellInfo.DmgClass)
            {
                case SpellDmgClass.Melee:
                    m_procAttacker = ProcFlags.DoneSpellMeleeDmgClass;
                    if (m_attackType == WeaponAttackType.OffAttack)
                        m_procAttacker |= ProcFlags.DoneOffHandAttack;
                    else
                        m_procAttacker |= ProcFlags.DoneMainHandAttack;
                    m_procVictim = ProcFlags.TakenSpellMeleeDmgClass;
                    break;
                case SpellDmgClass.Ranged:
                    // Auto attack
                    if (m_spellInfo.HasAttribute(SpellAttr2.AutorepeatFlag))
                    {
                        m_procAttacker = ProcFlags.DoneRangedAutoAttack;
                        m_procVictim = ProcFlags.TakenRangedAutoAttack;
                    }
                    else // Ranged spell attack
                    {
                        m_procAttacker = ProcFlags.DoneSpellRangedDmgClass;
                        m_procVictim = ProcFlags.TakenSpellRangedDmgClass;
                    }
                    break;
                default:
                    if (m_spellInfo.EquippedItemClass == ItemClass.Weapon &&
                        Convert.ToBoolean(m_spellInfo.EquippedItemSubClassMask & (1 << (int)ItemSubClassWeapon.Wand))
                        && m_spellInfo.HasAttribute(SpellAttr2.AutorepeatFlag)) // Wands auto attack
                    {
                        m_procAttacker = ProcFlags.DoneRangedAutoAttack;
                        m_procVictim = ProcFlags.TakenRangedAutoAttack;
                    }
                    break;
                    // For other spells trigger procflags are set in Spell::TargetInfo::DoDamageAndTriggers
                    // Because spell positivity is dependant on target
            }

            // Hunter trap spells - activation proc for Lock and Load, Entrapment and Misdirection
            if (m_spellInfo.SpellFamilyName == SpellFamilyNames.Hunter && (m_spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x18u) ||     // Freezing and Frost Trap, Freezing Arrow
                m_spellInfo.Id == 57879 || // Snake Trap - done this way to avoid double proc
                m_spellInfo.SpellFamilyFlags[2].HasAnyFlag(0x00024000u))) // Explosive and Immolation Trap
            {
                m_procAttacker |= ProcFlags.DoneTrapActivation;

                // also fill up other flags (TargetInfo::DoDamageAndTriggers only fills up flag if both are not set)
                m_procAttacker |= ProcFlags.DoneSpellMagicDmgClassNeg;
                m_procVictim |= ProcFlags.TakenSpellMagicDmgClassNeg;
            }

            // Hellfire Effect - trigger as DOT
            if (m_spellInfo.SpellFamilyName == SpellFamilyNames.Warlock && m_spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00000040u))
            {
                m_procAttacker = ProcFlags.DonePeriodic;
                m_procVictim = ProcFlags.TakenPeriodic;
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
            WorldObject caster = m_originalCaster ? m_originalCaster : m_caster;
            targetInfo.MissCondition = caster.SpellHitResult(target, m_spellInfo, m_canReflect && !(IsPositive() && m_caster.IsFriendlyTo(target)));

            // Spell have speed - need calculate incoming time
            // Incoming time is zero for self casts. At least I think so.
            if (m_caster != target)
            {
                float hitDelay = m_spellInfo.LaunchDelay;
                if (m_spellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
                    hitDelay += m_spellInfo.Speed;
                else if (m_spellInfo.Speed > 0.0f)
                {
                    // calculate spell incoming interval
                    /// @todo this is a hack
                    float dist = Math.Max(m_caster.GetDistance(target.GetPositionX(), target.GetPositionY(), target.GetPositionZ()), 5.0f);
                    hitDelay += dist / m_spellInfo.Speed;
                }

                targetInfo.TimeDelay = (ulong)Math.Floor(hitDelay * 1000.0f);
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
                target.m_Events.AddEvent(new ProcReflectDelayed(target, m_originalCasterGUID), target.m_Events.CalculateTime(targetInfo.TimeDelay));

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
                if (m_spellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
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

        void AddDestTarget(SpellDestination dest, uint effIndex)
        {
            m_destTargets[effIndex] = dest;
        }

        public long GetUnitTargetCountForEffect(uint effect)
        {
            return m_UniqueTargetInfo.Count(targetInfo => targetInfo.MissCondition == SpellMissInfo.Miss && (targetInfo.EffectMask & (1 << (int)effect)) != 0);
        }

        public long GetGameObjectTargetCountForEffect(uint effect)
        {
            return m_UniqueGOTargetInfo.Count(targetInfo => (targetInfo.EffectMask & (1 << (int)effect)) != 0);
        }

        public long GetItemTargetCountForEffect(uint effect)
        {
            return m_UniqueItemInfo.Count(targetInfo => (targetInfo.EffectMask & (1 << (int)effect)) != 0);
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
                player.StartCriteriaTimer(CriteriaStartEvent.BeSpellTarget, m_spellInfo.Id);
                player.UpdateCriteria(CriteriaType.BeSpellTarget, m_spellInfo.Id, 0, 0, m_caster);
                player.UpdateCriteria(CriteriaType.GainAura, m_spellInfo.Id);
            }

            Player casterPlayer = m_caster.ToPlayer();
            if (casterPlayer)
            {
                casterPlayer.StartCriteriaTimer(CriteriaStartEvent.CastSpell, m_spellInfo.Id);
                casterPlayer.UpdateCriteria(CriteriaType.LandTargetedSpellOnTarget, m_spellInfo.Id, 0, 0, unit);
            }

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

                    if (m_originalCaster && unit.IsInCombat() && m_spellInfo.HasInitialAggro())
                    {
                        if (m_originalCaster.HasUnitFlag(UnitFlags.PlayerControlled))               // only do explicit combat forwarding for PvP enabled units
                            m_originalCaster.GetCombatManager().InheritCombatStatesFrom(unit);    // for creature v creature combat, the threat forward does it for us
                        unit.GetThreatManager().ForwardThreatForAssistingMe(m_originalCaster, 0.0f, null, true);
                    }
                }
            }

            // original caster for auras
            WorldObject origCaster = m_caster;
            if (m_originalCaster)
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

                hitInfo.AuraDuration = Aura.CalcMaxDuration(m_spellInfo, origCaster);

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
                if (m_originalCaster)
                    caster = m_originalCaster;

                if (caster != null)
                {
                    // delayed spells with multiple targets need to create a new aura object, otherwise we'll access a deleted aura
                    if (m_spellInfo.HasHitDelay() && !m_spellInfo.IsChanneled())
                    {
                        spellAura = null;
                        Aura aura = unit.GetAura(m_spellInfo.Id, caster.GetGUID(), m_CastItem ? m_CastItem.GetGUID() : ObjectGuid.Empty, aura_effmask);
                        if (aura != null)
                            spellAura = aura.ToUnitAura();
                    }

                    if (spellAura == null)
                    {
                        bool resetPeriodicTimer = !_triggeredCastFlags.HasFlag(TriggerCastFlags.DontResetPeriodicTimer);
                        uint allAuraEffectMask = Aura.BuildEffectMaskForOwner(m_spellInfo, SpellConst.MaxEffectMask, unit);

                        AuraCreateInfo createInfo = new(m_castId, m_spellInfo, GetCastDifficulty(), allAuraEffectMask, unit);
                        createInfo.SetCasterGUID(caster.GetGUID());
                        createInfo.SetBaseAmount(hitInfo.AuraBasePoints);
                        createInfo.SetCastItem(m_castItemGUID, m_castItemEntry, m_castItemLevel);
                        createInfo.SetPeriodicReset(resetPeriodicTimer);
                        createInfo.SetOwnerEffectMask(aura_effmask);

                        Aura aura = Aura.TryRefreshStackOrCreate(createInfo);
                        if (aura != null)
                        {
                            spellAura = aura.ToUnitAura();

                            // Set aura stack amount to desired value
                            if (m_spellValue.AuraStackAmount > 1)
                            {
                                if (!createInfo.IsRefresh)
                                    spellAura.SetStackAmount((byte)m_spellValue.AuraStackAmount);
                                else
                                    spellAura.ModStackAmount(m_spellValue.AuraStackAmount);
                            }


                            spellAura.SetDiminishGroup(hitInfo.DRGroup);

                            hitInfo.AuraDuration = caster.ModSpellDuration(m_spellInfo, unit, hitInfo.AuraDuration, hitInfo.Positive, spellAura.GetEffectMask());

                            if (hitInfo.AuraDuration > 0)
                            {
                                hitInfo.AuraDuration *= (int)m_spellValue.DurationMul;

                                // Haste modifies duration of channeled spells
                                if (m_spellInfo.IsChanneled())
                                    caster.ModSpellDurationTime(m_spellInfo, ref hitInfo.AuraDuration, this);
                                else if (m_spellInfo.HasAttribute(SpellAttr5.HasteAffectDuration))
                                {
                                    int origDuration = hitInfo.AuraDuration;
                                    hitInfo.AuraDuration = 0;
                                    foreach (AuraEffect auraEff in spellAura.GetAuraEffects())
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

                            if (hitInfo.AuraDuration != spellAura.GetMaxDuration())
                            {
                                spellAura.SetMaxDuration(hitInfo.AuraDuration);
                                spellAura.SetDuration(hitInfo.AuraDuration);
                            }
                        }
                    }
                    else
                        spellAura.AddStaticApplication(unit, aura_effmask);

                    hitInfo.HitAura = spellAura;
                }
            }

            HandleEffects(unit, null, null, spellEffectInfo, SpellEffectHandleMode.HitTarget);
        }

        public void DoTriggersOnSpellHit(Unit unit, uint effMask)
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
                    if (CanExecuteTriggersOnHit(effMask, hit.triggeredByAura) && RandomHelper.randChance(hit.chance))
                    {
                        m_caster.CastSpell(unit, hit.triggeredSpell.Id, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetCastDifficulty(hit.triggeredSpell.Difficulty));
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
            var spellTriggered = Global.SpellMgr.GetSpellLinked((int)m_spellInfo.Id + (int)SpellLinkedType.Hit);
            if (spellTriggered != null)
            {
                foreach (var id in spellTriggered)
                {
                    if (id < 0)
                        unit.RemoveAurasDueToSpell((uint)-id);
                    else
                        unit.CastSpell(unit, (uint)id, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCaster(m_caster.GetGUID()));
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

        public void Prepare(SpellCastTargets targets, AuraEffect triggeredByAura = null)
        {
            if (m_CastItem != null)
            {
                m_castItemGUID = m_CastItem.GetGUID();
                m_castItemEntry = m_CastItem.GetEntry();

                Player owner = m_CastItem.GetOwner();
                if (owner)
                    m_castItemLevel = (int)m_CastItem.GetItemLevel(owner);
                else if (m_CastItem.GetOwnerGUID() == m_caster.GetGUID())
                    m_castItemLevel = (int)m_CastItem.GetItemLevel(m_caster.ToPlayer());
                else
                {
                    SendCastResult(SpellCastResult.EquippedItem);
                    Finish(false);
                    return;
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
            m_caster.m_Events.AddEvent(_spellEvent, m_caster.m_Events.CalculateTime(1));

            // check disables
            if (Global.DisableMgr.IsDisabledFor(DisableType.Spell, m_spellInfo.Id, m_caster))
            {
                SendCastResult(SpellCastResult.SpellUnavailable);
                Finish(false);
                return;
            }

            // Prevent casting at cast another spell (ServerSide check)
            if (!_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreCastInProgress) && m_caster.ToUnit() != null && m_caster.ToUnit().IsNonMeleeSpellCast(false, true, true, m_spellInfo.Id == 75) && !m_castId.IsEmpty())
            {
                SendCastResult(SpellCastResult.SpellInProgress);
                Finish(false);
                return;
            }

            LoadScripts();

            // Fill cost data (not use power for item casts
            if (m_CastItem == null)
                m_powerCost = m_spellInfo.CalcPowerCost(m_caster, m_spellSchoolMask, this);

            // Set combo point requirement
            if (Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreComboPoints) || m_CastItem != null)
                m_needComboPoints = false;

            uint param1 = 0, param2 = 0;
            SpellCastResult result = CheckCast(true, ref param1, ref param2);
            // target is checked in too many locations and with different results to handle each of them
            // handle just the general SPELL_FAILED_BAD_TARGETS result which is the default result for most DBC target checks
            if (Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreTargetCheck) && result == SpellCastResult.BadTargets)
                result = SpellCastResult.SpellCastOk;
            if (result != SpellCastResult.SpellCastOk && !IsAutoRepeat())          //always cast autorepeat dummy for triggering
            {
                // Periodic auras should be interrupted when aura triggers a spell which can't be cast
                // for example bladestorm aura should be removed on disarm as of patch 3.3.5
                // channeled periodic spells should be affected by this (arcane missiles, penance, etc)
                // a possible alternative sollution for those would be validating aura target on unit state change
                if (triggeredByAura != null && triggeredByAura.IsPeriodic() && !triggeredByAura.GetBase().IsPassive())
                {
                    SendChannelUpdate(0);
                    triggeredByAura.GetBase().SetDuration(0);
                }

                if (param1 != 0 || param2 != 0)
                    SendCastResult(result, param1, param2);
                else
                    SendCastResult(result);

                Finish(false);
                return;
            }

            // Prepare data for triggers
            PrepareDataForTriggerSystem();

            if (m_caster.IsTypeId(TypeId.Player))
            {
                if (!m_caster.ToPlayer().GetCommandStatus(PlayerCommandStates.Casttime))
                {
                    // calculate cast time (calculated after first CheckCast check to prevent charge counting for first CheckCast fail)
                    m_casttime = m_spellInfo.CalcCastTime(this);
                }
                else
                    m_casttime = 0;
            }
            else
                m_casttime = m_spellInfo.CalcCastTime(this);

            // don't allow channeled spells / spells with cast time to be casted while moving
            // exception are only channeled spells that have no casttime and SPELL_ATTR5_CAN_CHANNEL_WHEN_MOVING
            // (even if they are interrupted on moving, spells with almost immediate effect get to have their effect processed before movement interrupter kicks in)
            // don't cancel spells which are affected by a SPELL_AURA_CAST_WHILE_WALKING effect
            if (((m_spellInfo.IsChanneled() || m_casttime != 0) && m_caster.IsPlayer() && !(m_caster.ToPlayer().IsCharmed() && m_caster.ToPlayer().GetCharmerGUID().IsCreature()) && m_caster.ToPlayer().IsMoving() &&
                m_spellInfo.InterruptFlags.HasFlag(SpellInterruptFlags.Movement)) && !m_caster.ToPlayer().HasAuraTypeWithAffectMask(AuraType.CastWhileWalking, m_spellInfo))
            {
                // 1. Has casttime, 2. Or doesn't have flag to allow movement during channel
                if (m_casttime != 0 || !m_spellInfo.IsMoveAllowedChannel())
                {
                    SendCastResult(SpellCastResult.Moving);
                    Finish(false);
                    return;
                }
            }

            // focus if not controlled creature
            if (m_caster.GetTypeId() == TypeId.Unit && !m_caster.ToUnit().HasUnitFlag(UnitFlags.Possessed))
            {
                if (!(m_spellInfo.IsNextMeleeSwingSpell() || IsAutoRepeat()))
                {
                    if (m_targets.GetObjectTarget() && m_caster != m_targets.GetObjectTarget())
                        m_caster.ToCreature().FocusTarget(this, m_targets.GetObjectTarget());
                    else if (m_spellInfo.HasAttribute(SpellAttr5.DontTurnDuringCast))
                        m_caster.ToCreature().FocusTarget(this, null);
                }
            }

            // set timer base at cast time
            ReSetTimer();

            Log.outDebug(LogFilter.Spells, "Spell.prepare: spell id {0} source {1} caster {2} customCastFlags {3} mask {4}", m_spellInfo.Id, m_caster.GetEntry(), m_originalCaster != null ? (int)m_originalCaster.GetEntry() : -1, _triggeredCastFlags, m_targets.GetTargetMask());

            if (m_spellInfo.HasAttribute(SpellAttr12.StartCooldownOnCastStart))
                SendSpellCooldown();

            //Containers for channeled spells have to be set
            // @todoApply this to all casted spells if needed
            // Why check duration? 29350: channelled triggers channelled
            if (_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.CastDirectly) && (!m_spellInfo.IsChanneled() || m_spellInfo.GetMaxDuration() == 0))
                Cast(true);
            else
            {
                Unit unitCaster = m_caster.ToUnit();
                if (unitCaster != null)
                {
                    // stealth must be removed at cast starting (at show channel bar)
                    // skip triggered spell (item equip spell casting and other not explicit character casts/item uses)
                    if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreAuraInterruptFlags) && m_spellInfo.IsBreakingStealth() && !m_spellInfo.HasAttribute(SpellAttr2.IgnoreActionAuraInterruptFlags))
                        unitCaster.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Action);

                    unitCaster.SetCurrentCastSpell(this);
                }

                SendSpellStart();

                if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreGCD))
                    TriggerGlobalCooldown();

                // commented out !m_spellInfo.StartRecoveryTime, it forces instant spells with global cooldown to be processed in spell::update
                // as a result a spell that passed CheckCast and should be processed instantly may suffer from this delayed process
                // the easiest bug to observe is LoS check in AddUnitTarget, even if spell passed the CheckCast LoS check the situation can change in spell::update
                // because target could be relocated in the meantime, making the spell fly to the air (no targets can be registered, so no effects processed, nothing in combat log)
                if (m_casttime == 0 && /*m_spellInfo.StartRecoveryTime == 0 && */ GetCurrentContainer() == CurrentSpellTypes.Generic)
                    Cast(true);
            }
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

                    SendChannelUpdate(0);
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

            Finish(false);
        }

        public void Cast(bool skipCheck = false)
        {
            Player modOwner = m_caster.GetSpellModOwner();
            Spell lastSpellMod = null;
            if (modOwner)
            {
                lastSpellMod = modOwner.m_spellModTakingSpell;
                if (lastSpellMod)
                    modOwner.SetSpellModTakingSpell(lastSpellMod, false);
            }

            _cast(skipCheck);

            if (lastSpellMod)
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
                                if (cControlled.IsAIEnabled)
                                    cControlled.GetAI().OwnerAttacked(target);
                        }
                    }
                }
            }

            SetExecutedCurrently(true);

            if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreSetFacing))
                if (m_caster.IsTypeId(TypeId.Unit) && m_targets.GetObjectTarget() != null && m_caster != m_targets.GetObjectTarget())
                    m_caster.ToCreature().SetInFront(m_targets.GetObjectTarget());

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
                void cleanupSpell(SpellCastResult result, uint? param1 = null, uint? param2 = null)
                {
                    SendCastResult(result, param1, param2);
                    SendInterrupted(0);

                    if (modOwner)
                        modOwner.SetSpellModTakingSpell(this, false);

                    Finish(false);
                    SetExecutedCurrently(false);
                }


                uint param1 = 0, param2 = 0;
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
                    if (modOwner)
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
                                Unit caster1 = m_originalCaster ? m_originalCaster : m_caster.ToUnit();
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

            // if the spell allows the creature to turn while casting, then adjust server-side orientation to face the target now
            // client-side orientation is handled by the client itself, as the cast target is targeted due to Creature::FocusTarget
            if (m_caster.IsTypeId(TypeId.Unit) && !m_caster.ToUnit().HasUnitFlag(UnitFlags.Possessed))
            {
                if (!m_spellInfo.HasAttribute(SpellAttr5.DontTurnDuringCast))
                {
                    WorldObject objTarget = m_targets.GetObjectTarget();
                    if (objTarget != null)
                        m_caster.ToCreature().SetInFront(objTarget);
                }
            }

            SelectSpellTargets();

            // Spell may be finished after target map check
            if (m_spellState == SpellState.Finished)
            {
                SendInterrupted(0);

                if (m_caster.IsTypeId(TypeId.Player))
                    m_caster.ToPlayer().SetSpellModTakingSpell(this, false);

                Finish(false);
                SetExecutedCurrently(false);
                return;
            }

            Unit unitCaster = m_caster.ToUnit();
            if (unitCaster != null)
            {
                if (m_spellInfo.HasAttribute(SpellAttr1.DismissPet))
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
                    player.StartCriteriaTimer(CriteriaStartEvent.UseItem, m_CastItem.GetEntry());
                    player.UpdateCriteria(CriteriaType.UseItem, m_CastItem.GetEntry());
                }

                player.UpdateCriteria(CriteriaType.CastSpell, m_spellInfo.Id);
            }

            Item targetItem = m_targets.GetItemTarget();
            if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnorePowerAndReagentCost))
            {
                // Powers have to be taken before SendSpellGo
                TakePower();
                TakeReagents();                                         // we must remove reagents before HandleEffects to allow place crafted item in same slot
            }
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

            // we must send smsg_spell_go packet before m_castItem delete in TakeCastItem()...
            SendSpellGo();

            if (!m_spellInfo.IsChanneled())
            {
                Creature creatureCaster = m_caster.ToCreature();
                if (creatureCaster != null)
                    creatureCaster.ReleaseFocus(this);
            }

            // Okay, everything is prepared. Now we need to distinguish between immediate and evented delayed spells
            if ((m_spellInfo.HasHitDelay() && !m_spellInfo.IsChanneled()) || m_spellInfo.HasAttribute(SpellAttr4.Unk4))
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

            CallScriptAfterCastHandlers();

            var spell_triggered = Global.SpellMgr.GetSpellLinked((int)m_spellInfo.Id);
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
                        m_caster.CastSpell(m_targets.GetUnitTarget() ?? m_caster, (uint)spellId, new CastSpellExtraArgs(true));
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

            if (!m_originalCaster)
                return;

            // Handle procs on cast
            ProcFlags procAttacker = m_procAttacker;
            if (procAttacker == 0)
            {
                if (m_spellInfo.DmgClass == SpellDmgClass.Magic)
                    procAttacker = IsPositive() ? ProcFlags.DoneSpellMagicDmgClassPos : ProcFlags.DoneSpellMagicDmgClassNeg;
                else
                    procAttacker = IsPositive() ? ProcFlags.DoneSpellNoneDmgClassPos : ProcFlags.DoneSpellNoneDmgClassNeg;
            }

            ProcFlagsHit hitMask = m_hitMask;
            if (!hitMask.HasAnyFlag(ProcFlagsHit.Critical))
                hitMask |= ProcFlagsHit.Normal;

            if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreAuraInterruptFlags) && !m_spellInfo.HasAttribute(SpellAttr2.IgnoreActionAuraInterruptFlags))
                m_originalCaster.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.ActionDelayed);

            Unit.ProcSkillsAndAuras(m_originalCaster, null, procAttacker, ProcFlags.None, ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.Cast, hitMask, this, null, null);

            // Call CreatureAI hook OnSuccessfulSpellCast
            Creature caster = m_originalCaster.ToCreature();
            if (caster)
                if (caster.IsAIEnabled)
                    caster.GetAI().OnSuccessfulSpellCast(GetSpellInfo());
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
                if (duration > 0)
                {
                    // First mod_duration then haste - see Missile Barrage
                    // Apply duration mod
                    Player modOwner = m_caster.GetSpellModOwner();
                    if (modOwner != null)
                        modOwner.ApplySpellMod(m_spellInfo, SpellModOp.Duration, ref duration);

                    duration = (int)(duration * m_spellValue.DurationMul);

                    // Apply haste mods
                    m_caster.ModSpellDurationTime(m_spellInfo, ref duration, this);

                    SendChannelStart((uint)duration);
                }
                else if (duration == -1)
                    SendChannelStart((uint)duration);

                if (duration != 0)
                {
                    m_spellState = SpellState.Casting;
                    // GameObjects shouldn't cast channeled spells
                    m_caster.ToUnit().AddInterruptMask(m_spellInfo.ChannelInterruptFlags, m_spellInfo.ChannelInterruptFlags2);
                }
            }

            PrepareTargetProcessing();

            // process immediate effects (items, ground, etc.) also initialize some variables
            _handle_immediate_phase();

            // consider spell hit for some spells without target, so they may proc on finish phase correctly
            if (m_UniqueTargetInfo.Empty())
                m_hitMask = ProcFlagsHit.Normal;
            else
                DoProcessTargetContainer(m_UniqueTargetInfo);

            DoProcessTargetContainer(m_UniqueGOTargetInfo);

            FinishTargetProcessing();

            // spell is finished, perform some last features of the spell here
            _handle_finish_phase();

            // Remove used for cast item if need (it can be already NULL after TakeReagents call
            TakeCastItem();

            if (m_spellState != SpellState.Casting)
                Finish(true);                                       // successfully finish spell cast (not last in case autorepeat or channel spell)
        }

        public ulong HandleDelayed(ulong offset)
        {
            if (!UpdatePointers())
            {
                // finish the spell if UpdatePointers() returned false, something wrong happened there
                Finish(false);
                return 0;
            }

            bool single_missile = m_targets.HasDst();
            ulong next_time = 0;

            if (!m_launchHandled)
            {
                ulong launchMoment = (ulong)Math.Floor(m_spellInfo.LaunchDelay * 1000.0f);
                if (launchMoment > offset)
                    return launchMoment;

                HandleLaunchPhase();
                m_launchHandled = true;
                if (m_delayMoment > offset)
                {
                    if (single_missile)
                        return m_delayMoment;

                    next_time = m_delayMoment;
                    if ((m_UniqueTargetInfo.Count > 2 || (m_UniqueTargetInfo.Count == 1 && m_UniqueTargetInfo[0].TargetGUID == m_caster.GetGUID())) || !m_UniqueGOTargetInfo.Empty())
                    {
                        offset = 0; // if LaunchDelay was present then the only target that has timeDelay = 0 is m_caster - and that is the only target we want to process now
                    }
                }
            }

            if (single_missile && offset == 0)
                return m_delayMoment;

            Player modOwner = m_caster.GetSpellModOwner();
            if (modOwner != null)
                modOwner.SetSpellModTakingSpell(this, true);

            PrepareTargetProcessing();

            if (!m_immediateHandled && offset != 0)
            {
                _handle_immediate_phase();
                m_immediateHandled = true;
            }

            // now recheck units targeting correctness (need before any effects apply to prevent adding immunity at first effect not allow apply second spell effect and similar cases)
            {
                List<TargetInfo> delayedTargets = new();
                m_UniqueTargetInfo.RemoveAll(target =>
                {
                    if (single_missile || target.TimeDelay <= offset)
                    {
                        target.TimeDelay = offset;
                        delayedTargets.Add(target);
                        return true;
                    }
                    else if (next_time == 0 || target.TimeDelay < next_time)
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
                    if (single_missile || goTarget.TimeDelay <= offset)
                    {
                        goTarget.TimeDelay = offset;
                        delayedGOTargets.Add(goTarget);
                        return true;
                    }
                    else if (next_time == 0 || goTarget.TimeDelay < next_time)
                        next_time = goTarget.TimeDelay;
                    return false;
                });

                DoProcessTargetContainer(delayedGOTargets);
            }

            FinishTargetProcessing();

            if (modOwner)
                modOwner.SetSpellModTakingSpell(this, false);

            // All targets passed - need finish phase
            if (next_time == 0)
            {
                // spell is finished, perform some last features of the spell here
                _handle_finish_phase();

                Finish(true);                                       // successfully finish spell cast

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
                HandleEffects(null, null, null, spellEffectInfo, SpellEffectHandleMode.Hit);
            }

            // process items
            DoProcessTargetContainer(m_UniqueItemInfo);
        }

        void _handle_finish_phase()
        {
            Unit unitCaster = m_caster.ToUnit();
            if (unitCaster != null)
            {
                // Take for real after all targets are processed
                if (m_needComboPoints)
                    unitCaster.ClearComboPoints();

                // Real add combo points from effects
                if (m_comboPointGain != 0)
                    unitCaster.AddComboPoints(m_comboPointGain);

                if (unitCaster.ExtraAttacks != 0 && m_spellInfo.HasEffect(SpellEffectName.AddExtraAttacks))
                {
                    Unit victim = Global.ObjAccessor.GetUnit(unitCaster, m_targets.GetOrigUnitTargetGUID());
                    if (victim)
                        unitCaster.HandleProcExtraAttackFor(victim);
                    else
                        unitCaster.ExtraAttacks = 0;
                }
            }

            // Handle procs on finish
            if (!m_originalCaster)
                return;

            ProcFlags procAttacker = m_procAttacker;
            if (procAttacker == 0)
            {
                if (m_spellInfo.DmgClass == SpellDmgClass.Magic)
                    procAttacker = IsPositive() ? ProcFlags.DoneSpellMagicDmgClassPos : ProcFlags.DoneSpellMagicDmgClassNeg;
                else
                    procAttacker = IsPositive() ? ProcFlags.DoneSpellNoneDmgClassPos : ProcFlags.DoneSpellNoneDmgClassNeg;
            }

            Unit.ProcSkillsAndAuras(m_originalCaster, null, procAttacker, ProcFlags.None, ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.Finish, m_hitMask, this, null, null);
        }

        void SendSpellCooldown()
        {
            if (!m_caster.IsUnit())
                return;

            if (m_CastItem)
                m_caster.ToUnit().GetSpellHistory().HandleCooldowns(m_spellInfo, m_CastItem, this);
            else
                m_caster.ToUnit().GetSpellHistory().HandleCooldowns(m_spellInfo, m_castItemEntry, this);
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

            // check if the player caster has moved before the spell finished
            // with the exception of spells affected with SPELL_AURA_CAST_WHILE_WALKING effect
            if ((m_caster.IsTypeId(TypeId.Player) && m_timer != 0) &&
                m_caster.ToPlayer().IsMoving() && (m_spellInfo.InterruptFlags.HasFlag(SpellInterruptFlags.Movement)) &&
                (m_spellInfo.HasEffect(SpellEffectName.Stuck) || !m_caster.ToPlayer().HasUnitMovementFlag(MovementFlag.FallingFar)) &&
                !m_caster.ToPlayer().HasAuraTypeWithAffectMask(AuraType.CastWhileWalking, m_spellInfo))
            {
                // don't cancel for melee, autorepeat, triggered and instant spells
                if (!m_spellInfo.IsNextMeleeSwingSpell() && !IsAutoRepeat() && !IsTriggered() && !(IsChannelActive() && m_spellInfo.IsMoveAllowedChannel()))
                {
                    // if charmed by creature, trust the AI not to cheat and allow the cast to proceed
                    // @todo this is a hack, "creature" movesplines don't differentiate turning/moving right now
                    // however, checking what type of movement the spline is for every single spline would be really expensive
                    if (!m_caster.ToPlayer().GetCharmerGUID().IsCreature())
                        Cancel();
                }
            }

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

                    if (m_timer == 0 && !m_spellInfo.IsNextMeleeSwingSpell() && !IsAutoRepeat())
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
                                if (unit)
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

                    if (m_timer == 0)
                    {
                        SendChannelUpdate(0);
                        Finish();
                    }
                    break;
                }
                default:
                    break;
            }
        }

        public void Finish(bool ok = true)
        {
            if (m_spellState == SpellState.Finished)
                return;

            m_spellState = SpellState.Finished;

            if (!m_caster)
                return;

            Unit unitCaster = m_caster.ToUnit();
            if (unitCaster != null)
                return;

            if (m_spellInfo.IsChanneled())
                unitCaster.UpdateInterruptMask();

            if (unitCaster.HasUnitState(UnitState.Casting) && !unitCaster.IsNonMeleeSpellCast(false, false, true))
                unitCaster.ClearUnitState(UnitState.Casting);

            // Unsummon summon as possessed creatures on spell cancel
            if (m_spellInfo.IsChanneled() && unitCaster.IsTypeId(TypeId.Player))
            {
                Unit charm = unitCaster.GetCharm();
                if (charm != null)
                    if (charm.IsTypeId(TypeId.Unit) && charm.ToCreature().HasUnitTypeMask(UnitTypeMask.Puppet)
                        && charm.m_unitData.CreatedBySpell == m_spellInfo.Id)
                        ((Puppet)charm).UnSummon();
            }

            Creature creatureCaster = unitCaster.ToCreature();
            if (creatureCaster != null)
                creatureCaster.ReleaseFocus(this);

            if (!ok)
                return;

            if (unitCaster.IsTypeId(TypeId.Unit) && unitCaster.ToCreature().IsSummon())
            {
                // Unsummon statue
                uint spell = unitCaster.m_unitData.CreatedBySpell;
                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spell, GetCastDifficulty());
                if (spellInfo != null && spellInfo.IconFileDataId == 134230)
                {
                    Log.outDebug(LogFilter.Spells, "Statue {0} is unsummoned in spell {1} finish", unitCaster.GetGUID().ToString(), m_spellInfo.Id);
                    unitCaster.SetDeathState(DeathState.JustDied);
                    return;
                }
            }

            if (IsAutoActionResetSpell())
            {
                if (!m_spellInfo.HasAttribute(SpellAttr2.NotResetAutoActions))
                {
                    unitCaster.ResetAttackTimer(WeaponAttackType.BaseAttack);
                    if (unitCaster.HaveOffhandWeapon())
                        unitCaster.ResetAttackTimer(WeaponAttackType.OffAttack);
                    unitCaster.ResetAttackTimer(WeaponAttackType.RangedAttack);
                }
            }

            // potions disabled by client, send event "not in combat" if need
            if (unitCaster.IsTypeId(TypeId.Player))
            {
                if (m_triggeredByAuraSpell == null)
                    unitCaster.ToPlayer().UpdatePotionCooldown(this);
            }

            // Stop Attack for some spells
            if (m_spellInfo.HasAttribute(SpellAttr0.StopAttackTarget))
                unitCaster.AttackStop();
        }

        static void FillSpellCastFailedArgs<T>(T packet, ObjectGuid castId, SpellInfo spellInfo, SpellCastResult result, SpellCustomErrors customError, uint? param1, uint? param2, Player caster) where T : CastFailedBase
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
                        uint missingItem = 0;
                        for (uint i = 0; i < SpellConst.MaxReagents; i++)
                        {
                            if (spellInfo.Reagent[i] <= 0)
                                continue;

                            uint itemid = (uint)spellInfo.Reagent[i];
                            uint itemcount = spellInfo.ReagentCount[i];

                            if (!caster.HasItemCount(itemid, itemcount))
                            {
                                missingItem = itemid;
                                break;
                            }
                        }

                        packet.FailedArg1 = (int)missingItem;  // first missing item
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

        public void SendCastResult(SpellCastResult result, uint? param1 = null, uint? param2 = null)
        {
            if (result == SpellCastResult.SpellCastOk)
                return;

            if (!m_caster.IsTypeId(TypeId.Player))
                return;

            if (m_caster.ToPlayer().IsLoading())  // don't send cast results at loading time
                return;

            if (_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.DontReportCastError))
                result = SpellCastResult.DontReport;

            CastFailed castFailed = new();
            castFailed.Visual = m_SpellVisual;
            FillSpellCastFailedArgs(castFailed, m_castId, m_spellInfo, result, m_customError, param1, param2, m_caster.ToPlayer());
            m_caster.ToPlayer().SendPacket(castFailed);
        }

        public void SendPetCastResult(SpellCastResult result, uint? param1 = null, uint? param2 = null)
        {
            if (result == SpellCastResult.SpellCastOk)
                return;

            Unit owner = m_caster.GetCharmerOrOwner();
            if (!owner || !owner.IsTypeId(TypeId.Player))
                return;

            if (_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.DontReportCastError))
                result = SpellCastResult.DontReport;

            PetCastFailed petCastFailed = new();
            FillSpellCastFailedArgs(petCastFailed, m_castId, m_spellInfo, result, SpellCustomErrors.None, param1, param2, owner.ToPlayer());
            owner.ToPlayer().SendPacket(petCastFailed);
        }

        public static void SendCastResult(Player caster, SpellInfo spellInfo, SpellCastVisual spellVisual, ObjectGuid castCount, SpellCastResult result, SpellCustomErrors customError = SpellCustomErrors.None, uint? param1 = null, uint? param2 = null)
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
            uint mechanicImmunityMask = 0;
            Unit unitCaster = m_caster.ToUnit();
            if (unitCaster != null)
            {
                schoolImmunityMask = unitCaster.GetSchoolImmunityMask();
                mechanicImmunityMask = unitCaster.GetMechanicImmunityMask();
            }

            if (schoolImmunityMask != 0 || mechanicImmunityMask != 0)
                castFlags |= SpellCastFlags.Immunity;

            if (((IsTriggered() && !m_spellInfo.IsAutoRepeatRangedSpell()) || m_triggeredByAuraSpell != null) && !m_fromClient)
                castFlags |= SpellCastFlags.Pending;

            if (m_spellInfo.HasAttribute(SpellAttr0.ReqAmmo) || m_spellInfo.HasAttribute(SpellCustomAttributes.NeedsAmmoData))
                castFlags |= SpellCastFlags.Projectile;

            if ((m_caster.IsTypeId(TypeId.Player) || (m_caster.IsTypeId(TypeId.Unit) && m_caster.ToCreature().IsPet())) && m_powerCost.Any(cost => cost.Power != PowerType.Health))
                castFlags |= SpellCastFlags.PowerLeftSelf;

            if (HasPowerTypeCost(PowerType.Runes))
                castFlags |= SpellCastFlags.NoGCD; // not needed, but Blizzard sends it

            SpellStart packet = new();
            SpellCastData castData = packet.Cast;

            if (m_CastItem)
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
                castData.RemainingRunes.HasValue = true;

                RuneData runeData = castData.RemainingRunes.Value;
                //TODO: There is a crash caused by a spell with CAST_FLAG_RUNE_LIST casted by a creature
                //The creature is the mover of a player, so HandleCastSpellOpcode uses it as the caster

                Player player = m_caster.ToPlayer();
                if (player)
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

            UpdateSpellCastDataAmmo(castData.Ammo);

            if (castFlags.HasAnyFlag(SpellCastFlags.Immunity))
            {
                castData.Immunities.School = schoolImmunityMask;
                castData.Immunities.Value = mechanicImmunityMask;
            }

            /** @todo implement heal prediction packet data
            if (castFlags & CAST_FLAG_HEAL_PREDICTION)
            {
                castData.Predict.BeconGUID = ??
                castData.Predict.Points = 0;
                castData.Predict.Type = 0;
            }**/

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
            if (((IsTriggered() && !m_spellInfo.IsAutoRepeatRangedSpell()) || m_triggeredByAuraSpell != null) && !m_fromClient)
                castFlags |= SpellCastFlags.Pending;

            if (m_spellInfo.HasAttribute(SpellAttr0.ReqAmmo) || m_spellInfo.HasAttribute(SpellCustomAttributes.NeedsAmmoData))
                castFlags |= SpellCastFlags.Projectile;                        // arrows/bullets visual

            if ((m_caster.IsTypeId(TypeId.Player) || (m_caster.IsTypeId(TypeId.Unit) && m_caster.ToCreature().IsPet())) && m_powerCost.Any(cost => cost.Power != PowerType.Health))
                castFlags |= SpellCastFlags.PowerLeftSelf;

            if (m_caster.IsTypeId(TypeId.Player) && m_caster.ToPlayer().GetClass() == Class.Deathknight &&
                HasPowerTypeCost(PowerType.Runes) && !_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnorePowerAndReagentCost))
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
                castData.RemainingRunes.HasValue = true;
                RuneData runeData = castData.RemainingRunes.Value;

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

                if (targetInfo.MissCondition == SpellMissInfo.None) // hits
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

            // Reset m_needAliveTargetMask for non channeled spell
            if (!m_spellInfo.IsChanneled())
                m_channelTargetEffectMask = 0;
        }

        void UpdateSpellCastDataAmmo(SpellAmmo ammo)
        {
            InventoryType ammoInventoryType = 0;
            uint ammoDisplayID = 0;

            Player playerCaster = m_caster.ToPlayer();
            if (playerCaster != null)
            {
                Item pItem = playerCaster.GetWeaponForAttack(WeaponAttackType.RangedAttack);
                if (pItem)
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
                                    }

                                    if (ammoDisplayID != 0)
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            ammo.DisplayID = (int)ammoDisplayID;
            ammo.InventoryType = (sbyte)ammoInventoryType;
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
        }

        SpellLogEffect GetExecuteLogEffect(SpellEffectName effect)
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
            spellLogEffectPowerDrainParams.PowerType = (uint)powerType;
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

        public void SendChannelUpdate(uint time)
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
            }

            SpellChannelUpdate spellChannelUpdate = new();
            spellChannelUpdate.CasterGUID = unitCaster.GetGUID();
            spellChannelUpdate.TimeRemaining = (int)time;
            unitCaster.SendMessageToSet(spellChannelUpdate, true);
        }

        void SendChannelStart(uint duration)
        {
            // GameObjects don't channel
            Unit unitCaster = m_caster.ToUnit();
            if (unitCaster == null)
                return;

            SpellChannelStart spellChannelStart = new();
            spellChannelStart.CasterGUID = unitCaster.GetGUID();
            spellChannelStart.SpellID = (int)m_spellInfo.Id;
            spellChannelStart.Visual = m_SpellVisual;
            spellChannelStart.ChannelDuration = duration;

            uint schoolImmunityMask = unitCaster.GetSchoolImmunityMask();
            uint mechanicImmunityMask = unitCaster.GetMechanicImmunityMask();

            if (schoolImmunityMask != 0 || mechanicImmunityMask != 0)
            {
                spellChannelStart.InterruptImmunities.HasValue = true;
                spellChannelStart.InterruptImmunities.Value.SchoolImmunities = (int)schoolImmunityMask;
                spellChannelStart.InterruptImmunities.Value.Immunities = (int)mechanicImmunityMask;
            }
            unitCaster.SendMessageToSet(spellChannelStart, true);

            m_timer = (int)duration;

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
                if ((target.EffectMask & channelAuraMask) != 0)
                    unitCaster.AddChannelObject(target.TargetGUID);

                if (m_UniqueTargetInfo.Count == 1 && m_UniqueGOTargetInfo.Empty())
                {
                    if (target.TargetGUID != unitCaster.GetGUID())
                    {
                        Creature creatureCaster = unitCaster.ToCreature();
                        if (creatureCaster != null)
                            if (!creatureCaster.IsFocusing(this))
                                creatureCaster.FocusTarget(this, Global.ObjAccessor.GetWorldObject(creatureCaster, target.TargetGUID));
                    }
                }
            }

            foreach (GOTargetInfo target in m_UniqueGOTargetInfo)
                if ((target.EffectMask & channelAuraMask) != 0)
                    unitCaster.AddChannelObject(target.TargetGUID);

            unitCaster.SetChannelSpellId(m_spellInfo.Id);
            unitCaster.SetChannelVisual(m_SpellVisual);
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
            resurrectRequest.ResurrectOffererVirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
            resurrectRequest.Name = sentName;
            resurrectRequest.Sickness = m_caster.IsUnit() && !m_caster.IsTypeId(TypeId.Player); // "you'll be afflicted with resurrection sickness"
            resurrectRequest.UseTimer = !m_spellInfo.HasAttribute(SpellAttr3.IgnoreResurrectionTimer);

            Pet pet = target.GetPet();
            if (pet)
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

                    // item has charges left
                    if (charges != 0)
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
            if (!unitCaster)
                return;

            if (m_CastItem != null || m_triggeredByAuraSpell != null)
                return;

            //Don't take power if the spell is cast while .cheat power is enabled.
            if (unitCaster.IsTypeId(TypeId.Player))
            {
                if (unitCaster.ToPlayer().GetCommandStatus(PlayerCommandStates.Power))
                    return;
            }

            foreach (SpellPowerCost cost in m_powerCost)
            {
                bool hit = true;
                if (unitCaster.IsTypeId(TypeId.Player))
                {
                    if (cost.Power == PowerType.Rage || cost.Power == PowerType.Energy || cost.Power == PowerType.Runes)
                    {
                        ObjectGuid targetGUID = m_targets.GetUnitTargetGUID();
                        if (!targetGUID.IsEmpty())
                        {
                            var ihit = m_UniqueTargetInfo.FirstOrDefault(targetInfo => targetInfo.TargetGUID == targetGUID && targetInfo.MissCondition != SpellMissInfo.None);
                            if (ihit != null)
                            {
                                hit = false;
                                //lower spell cost on fail (by talent aura)
                                Player modOwner = unitCaster.GetSpellModOwner();
                                if (modOwner != null)
                                    modOwner.ApplySpellMod(m_spellInfo, SpellModOp.PowerCostOnMiss, ref cost.Amount);
                            }
                        }
                    }
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

                if (cost.Power >= PowerType.Max)
                {
                    Log.outError(LogFilter.Spells, "Spell.TakePower: Unknown power type '{0}'", cost.Power);
                    continue;
                }

                unitCaster.ModifyPower(cost.Power, -cost.Amount);
            }
        }

        SpellCastResult CheckRuneCost()
        {
            int runeCost = m_powerCost.Sum(cost => cost.Power == PowerType.Runes ? cost.Amount : 0);
            if (runeCost == 0)
                return SpellCastResult.SpellCastOk;

            Player player = m_caster.ToPlayer();
            if (!player)
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

        void TakeReagents()
        {
            if (!m_caster.IsTypeId(TypeId.Player))
                return;

            // do not take reagents for these item casts
            if (m_CastItem != null && m_CastItem.GetTemplate().GetFlags().HasAnyFlag(ItemFlags.NoReagentCost))
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
        }

        void HandleThreatSpells()
        {
            // wild GameObject spells don't cause threat
            Unit unitCaster = (m_originalCaster ? m_originalCaster : m_caster.ToUnit());
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

        public void HandleEffects(Unit pUnitTarget, Item pItemTarget, GameObject pGOTarget, SpellEffectInfo spellEffectInfo, SpellEffectHandleMode mode)
        {
            effectHandleMode = mode;
            unitTarget = pUnitTarget;
            itemTarget = pItemTarget;
            gameObjTarget = pGOTarget;
            destTarget = m_destTargets[spellEffectInfo.EffectIndex].Position;
            effectInfo = spellEffectInfo;
            unitCaster = m_originalCaster ? m_originalCaster : m_caster.ToUnit();

            damage = CalculateDamage(spellEffectInfo, unitTarget, out _variance);

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
            uint param1 = 0, param2 = 0;
            return CheckCast(strict, ref param1, ref param2);
        }

        public SpellCastResult CheckCast(bool strict, ref uint param1, ref uint param2)
        {
            SpellCastResult castResult;

            // check death state
            if (m_caster.ToUnit() && !m_caster.ToUnit().IsAlive() && !m_spellInfo.IsPassive() && !(m_spellInfo.HasAttribute(SpellAttr0.CastableWhileDead) || (IsTriggered() && m_triggeredByAuraSpell == null)))
                return SpellCastResult.CasterDead;

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
                            && !m_spellInfo.HasAttribute(SpellAttr0.ReqAmmo)
                            && !m_spellInfo.HasEffect(SpellEffectName.Attack)
                            && !m_spellInfo.HasAttribute(SpellAttr12.IgnoreCastingDisabled)
                            && !playerCaster.HasAuraTypeWithFamilyFlags(AuraType.DisableCastingExceptAbilities, CliDB.ChrClassesStorage.LookupByKey(playerCaster.GetClass()).SpellClassSet, m_spellInfo.SpellFamilyFlags))
                            return SpellCastResult.CantDoThatRightNow;

                        if (playerCaster.HasAuraType(AuraType.DisableAttackingExceptAbilities))
                        {
                            if (!playerCaster.HasAuraTypeWithFamilyFlags(AuraType.DisableAttackingExceptAbilities, CliDB.ChrClassesStorage.LookupByKey(playerCaster.GetClass()).SpellClassSet, m_spellInfo.SpellFamilyFlags))
                            {
                                if (m_spellInfo.HasAttribute(SpellAttr0.ReqAmmo)
                                    || m_spellInfo.IsNextMeleeSwingSpell()
                                    || m_spellInfo.HasAttribute(SpellAttr1.MeleeCombatStart)
                                    || m_spellInfo.HasAttribute(SpellAttr2.Unk20)
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
                    if (!IsIgnoringCooldowns() && playerCaster.GetLastPotionId() != 0 && m_CastItem && (m_CastItem.IsPotion() || m_spellInfo.IsCooldownStartedOnEvent()))
                        return SpellCastResult.NotReady;
                }

                if (m_caster.IsUnit() && !m_caster.ToUnit().GetSpellHistory().IsReady(m_spellInfo, m_castItemEntry, IsIgnoringCooldowns()))
                {
                    if (m_triggeredByAuraSpell != null)
                        return SpellCastResult.DontReport;
                    else
                        return SpellCastResult.NotReady;
                }
            }

            if (m_spellInfo.HasAttribute(SpellAttr7.IsCheatSpell) && m_caster.IsUnit() && !m_caster.ToUnit().HasUnitFlag2(UnitFlags2.AllowCheatSpells))
            {
                m_customError = SpellCustomErrors.GmOnly;
                return SpellCastResult.CustomError;
            }

            // Check global cooldown
            if (strict && !Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreGCD) && HasGlobalCooldown())
                return !m_spellInfo.HasAttribute(SpellAttr0.DisabledWhileActive) ? SpellCastResult.NotReady : SpellCastResult.DontReport;

            // only triggered spells can be processed an ended Battleground
            if (!IsTriggered() && m_caster.IsTypeId(TypeId.Player))
            {
                Battleground bg = m_caster.ToPlayer().GetBattleground();
                if (bg)
                    if (bg.GetStatus() == BattlegroundStatus.WaitLeave)
                        return SpellCastResult.DontReport;
            }

            if (m_caster.IsTypeId(TypeId.Player) && Global.VMapMgr.IsLineOfSightCalcEnabled())
            {
                if (m_spellInfo.HasAttribute(SpellAttr0.OutdoorsOnly) && !m_caster.IsOutdoors())
                    return SpellCastResult.OnlyOutdoors;

                if (m_spellInfo.HasAttribute(SpellAttr0.IndoorsOnly) && m_caster.IsOutdoors())
                    return SpellCastResult.OnlyIndoors;
            }

            Unit unitCaster = m_caster.ToUnit();
            if (unitCaster != null)
            {
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

                bool reqCombat = true;
                var stateAuras = unitCaster.GetAuraEffectsByType(AuraType.AbilityIgnoreAurastate);
                foreach (var aura in stateAuras)
                {
                    if (aura.IsAffectingSpell(m_spellInfo))
                    {
                        m_needComboPoints = false;
                        if (aura.GetMiscValue() == 1)
                        {
                            reqCombat = false;
                            break;
                        }
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

                    if (reqCombat && unitCaster.IsInCombat() && !m_spellInfo.CanBeUsedInCombat())
                        return SpellCastResult.AffectingCombat;
                }

                // cancel autorepeat spells if cast start when moving
                // (not wand currently autorepeat cast delayed to moving stop anyway in spell update code)
                // Do not cancel spells which are affected by a SPELL_AURA_CAST_WHILE_WALKING effect
                if (unitCaster.IsPlayer() && unitCaster.ToPlayer().IsMoving() && (!unitCaster.IsCharmed() || !unitCaster.GetCharmerGUID().IsCreature()) && !unitCaster.HasAuraTypeWithAffectMask(AuraType.CastWhileWalking, m_spellInfo))
                {
                    // skip stuck spell to allow use it in falling case and apply spell limitations at movement
                    if ((!unitCaster.HasUnitMovementFlag(MovementFlag.FallingFar) || !m_spellInfo.HasEffect(SpellEffectName.Stuck)) &&
                        (IsAutoRepeat() || m_spellInfo.HasAuraInterruptFlag(SpellAuraInterruptFlags.Standing)))
                        return SpellCastResult.Moving;
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
                if (m_originalCaster != null)
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
                            if (dynObj)
                                losTarget = dynObj;
                        }

                        if (!m_spellInfo.HasAttribute(SpellAttr2.CanTargetNotInLos) && !m_spellInfo.HasAttribute(SpellAttr5.AlwaysAoeLineOfSight) && !Global.DisableMgr.IsDisabledFor(DisableType.Spell, m_spellInfo.Id, null, (byte)DisableFlags.SpellLOS)
                            && !unitTarget.IsWithinLOSInMap(losTarget, LineOfSightChecks.All, ModelIgnoreFlags.M2))
                            return SpellCastResult.LineOfSight;
                    }
                }
            }

            // Check for line of sight for spells with dest
            if (m_targets.HasDst())
            {
                float x, y, z;
                m_targets.GetDstPos().GetPosition(out x, out y, out z);

                if (!m_spellInfo.HasAttribute(SpellAttr2.CanTargetNotInLos) && !m_spellInfo.HasAttribute(SpellAttr5.AlwaysAoeLineOfSight) && !Global.DisableMgr.IsDisabledFor(DisableType.Spell, m_spellInfo.Id, null, (byte)DisableFlags.SpellLOS)
                    && !m_caster.IsWithinLOS(x, y, z, LineOfSightChecks.All, ModelIgnoreFlags.M2))
                    return SpellCastResult.LineOfSight;
            }

            // check pet presence
            if (unitCaster != null)
            {
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
            if (m_spellInfo.HasAttribute(SpellAttr3.Battleground) && m_caster.IsTypeId(TypeId.Player))
                if (!m_caster.ToPlayer().InBattleground())
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
                if (m_caster.IsPlayer() && m_caster.ToPlayer().IsMounted() && !m_spellInfo.IsPassive() && !m_spellInfo.HasAttribute(SpellAttr0.CastableWhileMounted))
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
                    if (!focusObject)
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

            if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnorePowerAndReagentCost))
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

                            List<uint> glyphRequiredSpecs = Global.DB2Mgr.GetGlyphRequiredSpecs(glyphId);
                            if (!glyphRequiredSpecs.Empty())
                            {
                                if (caster.GetPrimarySpecialization() == 0)
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
                        if (!foodItem)
                            return SpellCastResult.BadTargets;

                        Pet pet = m_caster.ToPlayer().GetPet();
                        if (!pet)
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
                            if (!target.IsWithinLOSInMap(unitCaster)) //Do full LoS/Path check. Don't exclude m2
                                return SpellCastResult.LineOfSight;

                            float objSize = target.GetCombatReach();
                            float range = m_spellInfo.GetMaxRange(true, unitCaster, this) * 1.5f + objSize; // can't be overly strict

                            m_preGeneratedPath = new(unitCaster);
                            m_preGeneratedPath.SetPathLengthLimit(range);
                            //first try with raycast, if it fails fall back to normal path
                            float targetObjectSize = Math.Min(target.GetCombatReach(), 4.0f);
                            bool result = m_preGeneratedPath.CalculatePath(target.GetPositionX(), target.GetPositionY(), target.GetPositionZ() + targetObjectSize, false, true);
                            if (m_preGeneratedPath.GetPathType().HasAnyFlag(PathType.Short))
                                return SpellCastResult.OutOfRange;
                            else if (!result || m_preGeneratedPath.GetPathType().HasAnyFlag(PathType.NoPath | PathType.Incomplete))
                            {
                                result = m_preGeneratedPath.CalculatePath(target.GetPositionX(), target.GetPositionY(), target.GetPositionZ() + targetObjectSize, false, false);
                                if (m_preGeneratedPath.GetPathType().HasAnyFlag(PathType.Short))
                                    return SpellCastResult.OutOfRange;
                                else if (!result || m_preGeneratedPath.GetPathType().HasAnyFlag(PathType.NoPath | PathType.Incomplete))
                                    return SpellCastResult.NoPath;
                                else if (m_preGeneratedPath.IsInvalidDestinationZ(target)) // Check position z, if not in a straight line
                                    return SpellCastResult.NoPath;
                            }
                            else if (m_preGeneratedPath.IsInvalidDestinationZ(target)) // Check position z, if in a straight line
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
                        if (!creature.IsCritter() && !creature.loot.IsLooted())
                            return SpellCastResult.TargetNotLooted;

                        SkillType skill = creature.GetCreatureTemplate().GetRequiredLootSkill();

                        ushort skillValue = m_caster.ToPlayer().GetSkillValue(skill);
                        uint TargetLevel = m_targets.GetUnitTarget().GetLevelForTarget(m_caster);
                        int ReqValue = (int)(skillValue < 100 ? (TargetLevel - 10) * 10 : TargetLevel * 5);
                        if (ReqValue > skillValue)
                            return SpellCastResult.LowCastlevel;

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
                        if (unitCaster == null)
                            return SpellCastResult.BadTargets;

                        Creature pet = unitCaster.GetGuardianPet();
                        if (pet != null && pet.IsAlive())
                            return SpellCastResult.AlreadyHaveSummon;

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
                                if (!m_spellInfo.HasAttribute(SpellAttr1.DismissPet) && !unitCaster.GetPetGUID().IsEmpty())
                                    return SpellCastResult.AlreadyHaveSummon;
                                goto case SummonCategory.Puppet;
                            case SummonCategory.Puppet:
                                if (!unitCaster.GetCharmGUID().IsEmpty())
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
                            if (!m_spellInfo.HasAttribute(SpellAttr1.DismissPet) && !m_targets.GetUnitTarget().GetPetGUID().IsEmpty())
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
                                        pet.CastSpell(pet, 32752, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCaster(pet.GetGUID()));
                                }
                            }
                            else if (!m_spellInfo.HasAttribute(SpellAttr1.DismissPet))
                                return SpellCastResult.AlreadyHaveSummon;
                        }

                        if (!unitCaster.GetCharmGUID().IsEmpty())
                            return SpellCastResult.AlreadyHaveCharm;
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
                        MapRecord map = CliDB.MapStorage.LookupByKey(m_caster.GetMapId());
                        if (map.IsDungeon())
                        {
                            uint mapId = m_caster.GetMap().GetId();
                            Difficulty difficulty = m_caster.GetMap().GetDifficultyID();
                            if (map.IsRaid())
                            {
                                InstanceBind targetBind = target.GetBoundInstance(mapId, difficulty);
                                if (targetBind != null)
                                {
                                    InstanceBind casterBind = m_caster.ToPlayer().GetBoundInstance(mapId, difficulty);
                                    if (casterBind != null)
                                        if (targetBind.perm && targetBind.save != casterBind.save)
                                            return SpellCastResult.TargetLockedToRaidInstance;
                                }
                            }
                            InstanceTemplate instance = Global.ObjectMgr.GetInstanceTemplate(mapId);
                            if (instance == null)
                                return SpellCastResult.TargetNotInInstance;
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
                            if (bg)
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
                        if (!playerCaster)
                            return SpellCastResult.TargetNotPlayer;

                        if (spec == null || (spec.ClassID != (uint)player.GetClass() && !spec.IsPetSpecialization()))
                            return SpellCastResult.NoSpec;

                        if (spec.IsPetSpecialization())
                        {
                            Pet pet = player.GetPet();
                            if (!pet || pet.GetPetType() != PetType.Hunter || pet.GetCharmInfo() == null)
                                return SpellCastResult.NoPet;
                        }

                        // can't change during already started arena/Battleground
                        Battleground bg = player.GetBattleground();
                        if (bg)
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
                            param1 = talent.SpellID;
                            return SpellCastResult.CantUntalent;
                        }
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
                    default:
                        break;
                }
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
                            return SpellCastResult.Charmed;
                        break;
                    }
                    case AuraType.ModPossess:
                    case AuraType.ModCharm:
                    case AuraType.AoeCharm:
                    {
                        Unit unitCaster1 = (m_originalCaster ? m_originalCaster : m_caster.ToUnit());
                        if (unitCaster1 == null)
                            return SpellCastResult.BadTargets;

                        if (!unitCaster1.GetCharmerGUID().IsEmpty())
                            return SpellCastResult.Charmed;

                        if (spellEffectInfo.ApplyAuraName == AuraType.ModCharm || spellEffectInfo.ApplyAuraName == AuraType.ModPossess)
                        {
                            if (!m_spellInfo.HasAttribute(SpellAttr1.DismissPet) && !unitCaster1.GetPetGUID().IsEmpty())
                                return SpellCastResult.AlreadyHaveSummon;

                            if (!unitCaster1.GetCharmGUID().IsEmpty())
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

                        // Ignore map check if spell have AreaId. AreaId already checked and this prevent special mount spells
                        bool allowMount = !unitCaster.GetMap().IsDungeon() || unitCaster.GetMap().IsBattlegroundOrArena();
                        InstanceTemplate it = Global.ObjectMgr.GetInstanceTemplate(unitCaster.GetMapId());
                        if (it != null)
                            allowMount = it.AllowMount;

                        if (unitCaster.IsTypeId(TypeId.Player) && !allowMount && m_spellInfo.RequiredAreasID == 0)
                            return SpellCastResult.NoMountsAllowed;

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
                            BattleField Bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(m_originalCaster.GetZoneId());
                            var area = CliDB.AreaTableStorage.LookupByKey(m_originalCaster.GetAreaId());
                            if (area != null)
                                if (area.Flags.HasFlag(AreaFlags.NoFlyZone) || (Bf != null && !Bf.CanFlyIn()))
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
            }

            // check trade slot case (last, for allow catch any another cast problems)
            if (Convert.ToBoolean(m_targets.GetTargetMask() & SpellCastTargetFlags.TradeItem))
            {
                if (m_CastItem != null)
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

            // check if caster has at least 1 combo point for spells that require combo points
            if (m_needComboPoints)
            {
                Player plrCaster = m_caster.ToPlayer();
                if (plrCaster != null)
                    if (plrCaster.GetComboPoints() == 0)
                        return SpellCastResult.NoComboPoints;
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
            if (creatureCaster)
                if (creatureCaster.GetSpellHistory().HasCooldown(m_spellInfo.Id))
                    return SpellCastResult.NotReady;

            // Check if spell is affected by GCD
            if (m_spellInfo.StartRecoveryCategory > 0)
                if (unitCaster.GetCharmInfo() != null && unitCaster.GetSpellHistory().HasGlobalCooldown(m_spellInfo))
                    return SpellCastResult.NotReady;

            return CheckCast(true);
        }

        SpellCastResult CheckCasterAuras(ref uint param1)
        {
            Unit unitCaster = (m_originalCaster ? m_originalCaster : m_caster.ToUnit());
            if (unitCaster == null)
                return SpellCastResult.SpellCastOk;

            // spells totally immuned to caster auras (wsg flag drop, give marks etc)
            if (m_spellInfo.HasAttribute(SpellAttr6.IgnoreCasterAuras))
                return SpellCastResult.SpellCastOk;

            // these attributes only show the spell as usable on the client when it has related aura applied
            // still they need to be checked against certain mechanics

            // SPELL_ATTR5_USABLE_WHILE_STUNNED by default only MECHANIC_STUN (ie no sleep, knockout, freeze, etc.)
            bool usableWhileStunned = m_spellInfo.HasAttribute(SpellAttr5.UsableWhileStunned);

            // SPELL_ATTR5_USABLE_WHILE_FEARED by default only fear (ie no horror)
            bool usableWhileFeared = m_spellInfo.HasAttribute(SpellAttr5.UsableWhileFeared);

            // SPELL_ATTR5_USABLE_WHILE_CONFUSED by default only disorient (ie no polymorph)
            bool usableWhileConfused = m_spellInfo.HasAttribute(SpellAttr5.UsableWhileConfused);

            // Check whether the cast should be prevented by any state you might have.
            SpellCastResult result = SpellCastResult.SpellCastOk;
            // Get unit state
            UnitFlags unitflag = (UnitFlags)(uint)unitCaster.m_unitData.Flags;

            // this check should only be done when player does cast directly
            // (ie not when it's called from a script) Breaks for example PlayerAI when charmed
            /*if (!unitCaster.GetCharmerGUID().IsEmpty())
            {
                Unit charmer = unitCaster.GetCharmer();
                if (charmer)
                    if (charmer.GetUnitBeingMoved() != unitCaster && !CheckSpellCancelsCharm(ref param1))
                        result = SpellCastResult.Charmed;
            }*/

            // spell has attribute usable while having a cc state, check if caster has allowed mechanic auras, another mechanic types must prevent cast spell
            SpellCastResult mechanicCheck(AuraType auraType, ref uint _param1)
            {
                bool foundNotMechanic = false;
                var auras = unitCaster.GetAuraEffectsByType(auraType);
                foreach (AuraEffect aurEff in auras)
                {
                    uint mechanicMask = aurEff.GetSpellInfo().GetAllEffectsMechanicMask();
                    if (mechanicMask != 0 && !Convert.ToBoolean(mechanicMask & GetSpellInfo().GetAllowedMechanicMask()))
                    {
                        foundNotMechanic = true;

                        // fill up aura mechanic info to send client proper error message
                        _param1 = (uint)aurEff.GetSpellEffectInfo().Mechanic;
                        if (_param1 == 0)
                            _param1 = (uint)aurEff.GetSpellInfo().Mechanic;

                        break;
                    }
                }

                if (foundNotMechanic)
                {
                    switch (auraType)
                    {
                        case AuraType.ModStun:
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
            else if (unitflag.HasAnyFlag(UnitFlags.Silenced) && m_spellInfo.PreventionType.HasAnyFlag(SpellPreventionType.Silence) && !CheckSpellCancelsSilence(ref param1))
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

        bool CheckSpellCancelsAuraEffect(AuraType auraType, ref uint param1)
        {
            Unit unitCaster = (m_originalCaster ? m_originalCaster : m_caster.ToUnit());
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

                param1 = (uint)aurEff.GetSpellEffectInfo().Mechanic;
                if (param1 == 0)
                    param1 = (uint)aurEff.GetSpellInfo().Mechanic;

                return false;
            }

            return true;
        }

        bool CheckSpellCancelsCharm(ref uint param1)
        {
            return CheckSpellCancelsAuraEffect(AuraType.ModCharm, ref param1) ||
                CheckSpellCancelsAuraEffect(AuraType.AoeCharm, ref param1) ||
                CheckSpellCancelsAuraEffect(AuraType.ModPossess, ref param1);
        }

        bool CheckSpellCancelsStun(ref uint param1)
        {
            return CheckSpellCancelsAuraEffect(AuraType.ModStun, ref param1) &&
                 CheckSpellCancelsAuraEffect(AuraType.ModStunDisableGravity, ref param1);
        }

        bool CheckSpellCancelsSilence(ref uint param1)
        {
            return CheckSpellCancelsAuraEffect(AuraType.ModSilence, ref param1) ||
                CheckSpellCancelsAuraEffect(AuraType.ModPacifySilence, ref param1);
        }

        bool CheckSpellCancelsPacify(ref uint param1)
        {
            return CheckSpellCancelsAuraEffect(AuraType.ModPacify, ref param1) ||
                CheckSpellCancelsAuraEffect(AuraType.ModPacifySilence, ref param1);
        }

        bool CheckSpellCancelsFear(ref uint param1)
        {
            return CheckSpellCancelsAuraEffect(AuraType.ModFear, ref param1);
        }

        bool CheckSpellCancelsConfuse(ref uint param1)
        {
            return CheckSpellCancelsAuraEffect(AuraType.ModConfuse, ref param1);
        }

        bool CheckSpellCancelsNoActions(ref uint param1)
        {
            return CheckSpellCancelsAuraEffect(AuraType.ModNoActions, ref param1);
        }

        SpellCastResult CheckArenaAndRatedBattlegroundCastRules()
        {
            bool isRatedBattleground = false; // NYI
            bool isArena = !isRatedBattleground;

            // check USABLE attributes
            // USABLE takes precedence over NOT_USABLE
            if (isRatedBattleground && m_spellInfo.HasAttribute(SpellAttr9.UsableInRatedBattlegrounds))
                return SpellCastResult.SpellCastOk;

            if (isArena && m_spellInfo.HasAttribute(SpellAttr4.UsableInArena))
                return SpellCastResult.SpellCastOk;

            // check NOT_USABLE attributes
            if (m_spellInfo.HasAttribute(SpellAttr4.NotUsableInArenaOrRatedBg))
                return isArena ? SpellCastResult.NotInArena : SpellCastResult.NotInBattleground;

            if (isArena && m_spellInfo.HasAttribute(SpellAttr9.NotUsableInArena))
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
            if (!target)
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
                            if (Math.Abs(spellEffectInfo.BasePoints) <= Math.Abs(eff.GetAmount()))
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
            if (m_spellInfo.RangeEntry != null && m_spellInfo.RangeEntry.Flags != SpellRangeFlag.Melee && !strict)
                maxRange += Math.Min(3.0f, maxRange * 0.1f); // 10% but no more than 3.0f

            // get square values for sqr distance checks
            minRange *= minRange;
            maxRange *= maxRange;

            Unit target = m_targets.GetUnitTarget();
            if (target && target != m_caster)
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

            if (m_targets.HasDst() && !m_targets.HasTraj())
            {
                if (m_caster.GetExactDistSq(m_targets.GetDstPos()) > maxRange)
                    return SpellCastResult.OutOfRange;
                if (minRange > 0.0f && m_caster.GetExactDistSq(m_targets.GetDstPos()) < minRange)
                    return SpellCastResult.OutOfRange;
            }

            return SpellCastResult.SpellCastOk;
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
                if (m_spellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Melee))
                {
                    // when the target is not a unit, take the caster's combat reach as the target's combat reach.
                    if (unitCaster)
                        rangeMod = unitCaster.GetMeleeRange(target ? target : unitCaster);
                }
                else
                {
                    float meleeRange = 0.0f;
                    if (m_spellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Ranged))
                    {
                        // when the target is not a unit, take the caster's combat reach as the target's combat reach.
                        if (unitCaster != null)
                            meleeRange = unitCaster.GetMeleeRange(target ? target : unitCaster);
                    }

                    minRange = m_caster.GetSpellMinRangeForTarget(target, m_spellInfo) + meleeRange;
                    maxRange = m_caster.GetSpellMaxRangeForTarget(target, m_spellInfo);

                    if (target || m_targets.GetCorpseTarget())
                    {
                        rangeMod = m_caster.GetCombatReach() + (target ? target.GetCombatReach() : m_caster.GetCombatReach());

                        if (minRange > 0.0f && !m_spellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Ranged))
                            minRange += rangeMod;
                    }
                }

                if (target != null && unitCaster != null && unitCaster.IsMoving() && target.IsMoving() && !unitCaster.IsWalking() && !target.IsWalking() &&
                    (m_spellInfo.RangeEntry.Flags.HasFlag(SpellRangeFlag.Melee) || target.IsPlayer()))
                    rangeMod += 8.0f / 3.0f;
            }

            if (m_spellInfo.HasAttribute(SpellAttr0.ReqAmmo) && m_caster.IsTypeId(TypeId.Player))
            {
                Item ranged = m_caster.ToPlayer().GetWeaponForAttack(WeaponAttackType.RangedAttack, true);
                if (ranged)
                    maxRange *= ranged.GetTemplate().GetRangedModRange() * 0.01f;
            }

            Player modOwner = m_caster.GetSpellModOwner();
            if (modOwner)
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

        SpellCastResult CheckItems(ref uint param1, ref uint param2)
        {
            Player player = m_caster.ToPlayer();
            if (!player)
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
            if (!(m_CastItem != null && Convert.ToBoolean(m_CastItem.GetTemplate().GetFlags() & ItemFlags.NoReagentCost)))
            {
                bool checkReagents = !Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnorePowerAndReagentCost) && !player.CanNoReagentCast(m_spellInfo);
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
                            param1 = itemid;
                            return SpellCastResult.Reagents;
                        }
                    }
                }

                // check totem-item requirements (items presence in inventory)
                uint totems = 2;
                for (int i = 0; i < 2; ++i)
                {
                    if (m_spellInfo.Totem[i] != 0)
                    {
                        if (player.HasItemCount(m_spellInfo.Totem[i]))
                        {
                            totems -= 1;
                            continue;
                        }
                    }
                    else
                        totems -= 1;
                }
                if (totems != 0)
                    return SpellCastResult.Totems;

                // Check items for TotemCategory (items presence in inventory)
                uint totemCategory = 2;
                for (byte i = 0; i < 2; ++i)
                {
                    if (m_spellInfo.TotemCategory[i] != 0)
                    {
                        if (player.HasItemTotemCategory(m_spellInfo.TotemCategory[i]))
                        {
                            totemCategory -= 1;
                            continue;
                        }
                    }
                    else
                        totemCategory -= 1;
                }

                if (totemCategory != 0)
                    return SpellCastResult.TotemCategory;
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
                        Unit target = m_targets.GetUnitTarget() ? m_targets.GetUnitTarget() : player;
                        if (target.IsPlayer() && !IsTriggered() && spellEffectInfo.ItemType != 0)
                        {
                            List<ItemPosCount> dest = new();
                            InventoryResult msg = target.ToPlayer().CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, spellEffectInfo.ItemType, 1);
                            if (msg != InventoryResult.Ok)
                            {
                                ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(spellEffectInfo.ItemType);
                                // @todo Needs review
                                if (itemTemplate != null && itemTemplate.GetItemLimitCategory() == 0)
                                {
                                    player.SendEquipError(msg, null, null, spellEffectInfo.ItemType);
                                    return SpellCastResult.DontReport;
                                }
                                else
                                {
                                    // Conjure Food/Water/Refreshment spells
                                    if (!(m_spellInfo.SpellFamilyName == SpellFamilyNames.Mage && m_spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x40000000u)))
                                        return SpellCastResult.TooManyOfItem;
                                    else if (!target.ToPlayer().HasItemCount(spellEffectInfo.ItemType))
                                    {
                                        player.SendEquipError(msg, null, null, spellEffectInfo.ItemType);
                                        return SpellCastResult.DontReport;
                                    }
                                    else if (m_spellInfo.GetEffects().Count > 1)
                                        player.CastSpell(m_caster, (uint)m_spellInfo.GetEffect(1).CalcValue(), new CastSpellExtraArgs(false));        // move this to anywhere
                                    return SpellCastResult.DontReport;
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
                            if (m_CastItem != null && Convert.ToBoolean(m_CastItem.GetTemplate().GetFlags() & ItemFlags.NoReagentCost))
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

                        // required level has to be checked also! Exploit fix
                        if (targetItem.GetItemLevel(targetItem.GetOwner()) < m_spellInfo.BaseLevel || (targetItem.GetRequiredLevel() != 0 && targetItem.GetRequiredLevel() < m_spellInfo.BaseLevel))
                            return SpellCastResult.Lowlevel;

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
                            if (enchantEntry.Flags.HasAnyFlag(EnchantmentSlotMask.CanSouldBound))
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
                            var pEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);
                            if (pEnchant == null)
                                return SpellCastResult.Error;
                            if (pEnchant.Flags.HasAnyFlag(EnchantmentSlotMask.CanSouldBound))
                                return SpellCastResult.NotTradeable;
                        }

                        // Apply item level restriction if the enchanting spell has max level restrition set
                        if (m_CastItem != null && m_spellInfo.MaxLevel > 0)
                        {
                            if (item.GetTemplate().GetBaseItemLevel() < m_CastItem.GetTemplate().GetBaseRequiredLevel())
                                return SpellCastResult.Lowlevel;
                            if (item.GetTemplate().GetBaseItemLevel() > m_spellInfo.MaxLevel)
                                return SpellCastResult.Highlevel;
                        }
                        break;
                    }
                    case SpellEffectName.EnchantHeldItem:
                        // check item existence in effect code (not output errors at offhand hold item effect to main hand for example
                        break;
                    case SpellEffectName.Disenchant:
                    {
                        Item item = m_targets.GetItemTarget();
                        if (!item)
                            return SpellCastResult.CantBeDisenchanted;

                        // prevent disenchanting in trade slot
                        if (item.GetOwnerGUID() != player.GetGUID())
                            return SpellCastResult.CantBeDisenchanted;

                        ItemTemplate itemProto = item.GetTemplate();
                        if (itemProto == null)
                            return SpellCastResult.CantBeDisenchanted;

                        ItemDisenchantLootRecord itemDisenchantLoot = item.GetDisenchantLoot(m_caster.ToPlayer());
                        if (itemDisenchantLoot == null)
                            return SpellCastResult.CantBeDisenchanted;
                        if (itemDisenchantLoot.SkillRequired > player.GetSkillValue(SkillType.Enchanting))
                            return SpellCastResult.LowCastlevel;
                        break;
                    }
                    case SpellEffectName.Prospecting:
                    {
                        Item item = m_targets.GetItemTarget();
                        if (!item)
                            return SpellCastResult.CantBeProspected;
                        //ensure item is a prospectable ore
                        if (!Convert.ToBoolean(item.GetTemplate().GetFlags() & ItemFlags.IsProspectable))
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
                            param1 = item.GetEntry();
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
                        if (!item)
                            return SpellCastResult.CantBeMilled;
                        //ensure item is a millable herb
                        if (!(item.GetTemplate().GetFlags().HasAnyFlag(ItemFlags.IsMillable)))
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
                            param1 = item.GetEntry();
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
                        for (int tier = 0; tier < azeriteEmpoweredItem.GetMaxAzeritePowerTier(); ++tier)
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
                    if (!item || item.IsBroken())
                        return SpellCastResult.EquippedItemClass;

                    // skip spell if weapon not fit to triggered spell
                    if (!item.IsFitToSpellRequirements(m_spellInfo))
                        return SpellCastResult.EquippedItemClass;

                    return SpellCastResult.SpellCastOk;
                });

                // main hand weapon required
                if (m_spellInfo.HasAttribute(SpellAttr3.MainHand))
                {
                    SpellCastResult mainHandResult = weaponCheck(WeaponAttackType.BaseAttack);
                    if (mainHandResult != SpellCastResult.SpellCastOk)
                        return mainHandResult;
                }

                // offhand hand weapon required
                if (m_spellInfo.HasAttribute(SpellAttr3.ReqOffhand))
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
            int delaytime = MathFunctions.CalculatePct(m_spellInfo.GetDuration(), 25); // channeling delay is normally 25% of its time per hit
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
            return m_powerCost.Any(cost => cost.Power == power);
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
                if (!m_CastItem)
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
                    if (target.IsTypeId(TypeId.Unit) && target.IsVehicle())
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
            if (m_spellInfo.HasAttribute(SpellAttr2.CanTargetNotInLos) || Global.DisableMgr.IsDisabledFor(DisableType.Spell, m_spellInfo.Id, null, (byte)DisableFlags.SpellLOS))
                return true;

            // check if gameobject ignores LOS
            GameObject gobCaster = m_caster.ToGameObject();
            if (gobCaster != null)
                if (gobCaster.GetGoInfo().GetRequireLOS() == 0)
                    return true;

            // if spell is triggered, need to check for LOS disable on the aura triggering it and inherit that behaviour
            if (IsTriggered() && m_triggeredByAuraSpell != null && (m_triggeredByAuraSpell.HasAttribute(SpellAttr2.CanTargetNotInLos) || Global.DisableMgr.IsDisabledFor(DisableType.Spell, m_triggeredByAuraSpell.Id, null, (byte)DisableFlags.SpellLOS)))
                return true;

            // @todo shit below shouldn't be here, but it's temporary
            //Check targets for LOS visibility
            switch (spellEffectInfo.Effect)
            {
                case SpellEffectName.SkinPlayerCorpse:
                {
                    if (m_targets.GetCorpseTargetGUID().IsEmpty())
                    {
                        if (target.IsWithinLOSInMap(m_caster, LineOfSightChecks.All, ModelIgnoreFlags.M2) && target.HasUnitFlag(UnitFlags.Skinnable))
                            return true;

                        return false;
                    }

                    Corpse corpse = ObjectAccessor.GetCorpse(m_caster, m_targets.GetCorpseTargetGUID());
                    if (!corpse)
                        return false;

                    if (target.GetGUID() != corpse.GetOwnerGUID())
                        return false;

                    if (!corpse.HasDynamicFlag(UnitDynFlags.Lootable))
                        return false;

                    if (!corpse.IsWithinLOSInMap(m_caster, LineOfSightChecks.All, ModelIgnoreFlags.M2))
                        return false;

                    break;
                }
                default:
                {
                    if (losPosition != null)
                        return target.IsWithinLOS(losPosition.GetPositionX(), losPosition.GetPositionY(), losPosition.GetPositionZ(), LineOfSightChecks.All, ModelIgnoreFlags.M2);
                    else
                    {
                        // Get GO cast coordinates if original caster . GO
                        WorldObject caster = null;
                        if (m_originalCasterGUID.IsGameObject())
                            caster = m_caster.GetMap().GetGameObject(m_originalCasterGUID);
                        if (!caster)
                            caster = m_caster;
                        if (target != m_caster && !target.IsWithinLOSInMap(caster, LineOfSightChecks.All, ModelIgnoreFlags.M2))
                            return false;
                    }

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
            if (IsTriggered())
                return false;

            if (m_casttime == 0 && m_spellInfo.HasAttribute(SpellAttr6.NotResetSwingIfInstant))
                return false;

            return true;
        }

        public bool IsPositive()
        {
            return m_spellInfo.IsPositive() && (m_triggeredByAuraSpell == null || m_triggeredByAuraSpell.IsPositive());
        }

        bool IsNeedSendToClient()
        {
            return m_SpellVisual.SpellXSpellVisualID != 0 || m_SpellVisual.ScriptVisualID != 0 || m_spellInfo.IsChanneled() ||
                m_spellInfo.HasAttribute(SpellAttr8.AuraSendAmount) || m_spellInfo.HasHitDelay() || (m_triggeredByAuraSpell == null && !IsTriggered());
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

                HandleEffects(null, null, null, spellEffectInfo, SpellEffectHandleMode.Launch);
            }

            PrepareTargetProcessing();

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

        void DoEffectOnLaunchTarget(TargetInfo targetInfo, float multiplier, SpellEffectInfo spellEffectInfo)
        {
            Unit unit = null;
            // In case spell hit target, do all effect on that target
            if (targetInfo.MissCondition == SpellMissInfo.None)
                unit = m_caster.GetGUID() == targetInfo.TargetGUID ? m_caster.ToUnit() : Global.ObjAccessor.GetUnit(m_caster, targetInfo.TargetGUID);
            // In case spell reflect from target, do all effect on caster (if hit)
            else if (targetInfo.MissCondition == SpellMissInfo.Reflect && targetInfo.ReflectResult == SpellMissInfo.None)
                unit = m_caster.ToUnit();
            if (unit == null)
                return;

            // This will only cause combat - the target will engage once the projectile hits (in DoAllEffectOnTarget)
            if (m_originalCaster != null && targetInfo.MissCondition != SpellMissInfo.Evade && !m_originalCaster.IsFriendlyTo(unit) && (!m_spellInfo.IsPositive() || m_spellInfo.HasEffect(SpellEffectName.Dispel)) && (m_spellInfo.HasInitialAggro() || unit.IsEngaged()))
                m_originalCaster.SetInCombatWith(unit);

            m_damage = 0;
            m_healing = 0;

            HandleEffects(unit, null, null, spellEffectInfo, SpellEffectHandleMode.LaunchTarget);

            if (m_originalCaster != null && m_damage > 0)
            {
                if (spellEffectInfo.IsTargetingArea() || spellEffectInfo.IsAreaAuraEffect() || spellEffectInfo.IsEffect(SpellEffectName.PersistentAreaAura))
                {
                    m_damage = unit.CalculateAOEAvoidance(m_damage, (uint)m_spellInfo.SchoolMask, m_originalCaster.GetGUID());

                    if (m_originalCaster.IsPlayer())
                    {
                        // cap damage of player AOE
                        long targetAmount = GetUnitTargetCountForEffect(spellEffectInfo.EffectIndex);
                        if (targetAmount > 20)
                            m_damage = (int)(m_damage * 20 / targetAmount);
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

            float critChance = m_spellValue.CriticalChance;
            if (m_originalCaster != null)
            {
                if (critChance == 0)
                    critChance = m_originalCaster.SpellCritChanceDone(this, null, m_spellSchoolMask, m_attackType);
                critChance = unit.SpellCritChanceTaken(m_originalCaster, this, null, m_spellSchoolMask, critChance, m_attackType);
            }

            targetInfo.IsCrit = RandomHelper.randChance(critChance);
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
                        if (lockInfo.Index[j] != 0 && m_CastItem && m_CastItem.GetEntry() == lockInfo.Index[j])
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
                            if (!m_CastItem && unitCaster.IsTypeId(TypeId.Player))
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
                }
            }

            if (reqKey)
                return SpellCastResult.BadTargets;

            return SpellCastResult.SpellCastOk;
        }

        public void SetSpellValue(SpellValueMod mod, int value)
        {
            if (mod < SpellValueMod.End)
            {
                m_spellValue.EffectBasePoints[(int)mod] = value;
                m_spellValue.CustomBasePointsMask |= 1u << (int)mod;
                return;
            }

            switch (mod)
            {
                case SpellValueMod.RadiusMod:
                    m_spellValue.RadiusMod = (float)value / 10000;
                    break;
                case SpellValueMod.MaxTargets:
                    m_spellValue.MaxAffectedTargets = (uint)value;
                    break;
                case SpellValueMod.AuraStack:
                    m_spellValue.AuraStackAmount = value;
                    break;
                case SpellValueMod.CritChance:
                    m_spellValue.CriticalChance = value / 100.0f; // @todo ugly /100 remove when basepoints are double
                    break;
                case SpellValueMod.DurationPct:
                    m_spellValue.DurationMul = (float)value / 100.0f;
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
                Log.outDebug(LogFilter.Spells, "Spell.LoadScripts: Script `{0}` for spell `{1}` is loaded now", script._GetScriptName(), m_spellInfo.Id);
                script.Register();
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

        bool CheckScriptEffectImplicitTargets(uint effIndex, uint effIndexToCheck)
        {
            // Skip if there are not any script
            if (m_loadedScripts.Empty())
                return true;

            foreach (var script in m_loadedScripts)
            {
                foreach (var hook in script.OnObjectTargetSelect)
                    if ((hook.IsEffectAffected(m_spellInfo, effIndex) && !hook.IsEffectAffected(m_spellInfo, effIndexToCheck)) ||
                        (!hook.IsEffectAffected(m_spellInfo, effIndex) && hook.IsEffectAffected(m_spellInfo, effIndexToCheck)))
                        return false;

                foreach (var hook in script.OnObjectAreaTargetSelect)
                    if ((hook.IsEffectAffected(m_spellInfo, effIndex) && !hook.IsEffectAffected(m_spellInfo, effIndexToCheck)) ||
                        (!hook.IsEffectAffected(m_spellInfo, effIndex) && hook.IsEffectAffected(m_spellInfo, effIndexToCheck)))
                        return false;
            }
            return true;
        }

        public bool CanExecuteTriggersOnHit(uint effMask, SpellInfo triggeredByAura = null)
        {
            bool only_on_caster = (triggeredByAura != null && triggeredByAura.HasAttribute(SpellAttr4.ProcOnlyOnCaster));
            // If triggeredByAura has SPELL_ATTR4_PROC_ONLY_ON_CASTER then it can only proc on a casted spell with TARGET_UNIT_CASTER
            foreach (var spellEffectInfo in m_spellInfo.GetEffects())
            {
                if ((effMask & (1 << (int)spellEffectInfo.EffectIndex)) != 0 && (!only_on_caster || (spellEffectInfo.TargetA.GetTarget() == Targets.UnitCaster)))
                    return true;
            }
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

            int gcd = (int)m_spellInfo.StartRecoveryTime;
            if (gcd == 0 || m_spellInfo.StartRecoveryCategory == 0)
                return;

            if (m_caster.IsTypeId(TypeId.Player))
                if (m_caster.ToPlayer().GetCommandStatus(PlayerCommandStates.Cooldown))
                    return;

            // Global cooldown can't leave range 1..1.5 secs
            // There are some spells (mostly not casted directly by player) that have < 1 sec and > 1.5 sec global cooldowns
            // but as tests show are not affected by any spell mods.
            if (m_spellInfo.StartRecoveryTime >= 750 && m_spellInfo.StartRecoveryTime <= 1500)
            {
                // gcd modifier auras are applied only to own spells and only players have such mods
                Player modOwner = m_caster.GetSpellModOwner();
                if (modOwner)
                    modOwner.ApplySpellMod(m_spellInfo, SpellModOp.StartCooldown, ref gcd, this);

                bool isMeleeOrRangedSpell = m_spellInfo.DmgClass == SpellDmgClass.Melee || m_spellInfo.DmgClass == SpellDmgClass.Ranged ||
                    m_spellInfo.HasAttribute(SpellAttr0.ReqAmmo) || m_spellInfo.HasAttribute(SpellAttr0.Ability);

                // Apply haste rating
                if (gcd > 750 && (m_spellInfo.StartRecoveryCategory == 133 && !isMeleeOrRangedSpell))
                {
                    gcd = (int)(gcd * m_caster.ToUnit().m_unitData.ModSpellHaste);
                    MathFunctions.RoundToInterval(ref gcd, 750, 1500);
                }

                if (gcd > 750 && m_caster.ToUnit().HasAuraTypeWithAffectMask(AuraType.ModGlobalCooldownByHasteRegen, m_spellInfo))
                {
                    gcd = (int)(gcd * m_caster.ToUnit().m_unitData.ModHasteRegen);
                    MathFunctions.RoundToInterval(ref gcd, 750, 1500);
                }
            }

            m_caster.ToUnit().GetSpellHistory().AddGlobalCooldown(m_spellInfo, (uint)gcd);
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

        List<SpellScript> m_loadedScripts = new();

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

        public bool IsTriggered() { return _triggeredCastFlags.HasAnyFlag(TriggerCastFlags.FullMask); }
        public bool IsTriggeredByAura(SpellInfo auraSpellInfo) { return (auraSpellInfo == m_triggeredByAuraSpell); }
        public bool IsIgnoringCooldowns() { return _triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreSpellAndCategoryCD); }
        public bool IsFocusDisabled() { return _triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreSetFacing) || (m_spellInfo.IsChanneled() && !m_spellInfo.HasAttribute(SpellAttr1.ChannelTrackTarget)); }
        public bool IsProcDisabled() { return _triggeredCastFlags.HasAnyFlag(TriggerCastFlags.DisallowProcEvents); }
        public bool IsChannelActive() { return m_caster.IsUnit() && m_caster.ToUnit().GetChannelSpellId() != 0; }

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

        public WorldObject GetCaster()
        {
            return m_caster;
        }
        public Unit GetOriginalCaster()
        {
            return m_originalCaster;
        }
        public SpellInfo GetSpellInfo()
        {
            return m_spellInfo;
        }
        public List<SpellPowerCost> GetPowerCost()
        {
            return m_powerCost;
        }

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

        public static implicit operator bool(Spell spell)
        {
            return spell != null;
        }

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
        public Networking.Packets.SpellCastVisual m_SpellVisual;
        public SpellCastTargets m_targets;
        public sbyte m_comboPointGain;
        public SpellCustomErrors m_customError;

        public List<Aura> m_appliedMods;

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
        bool m_canReflect;                                  // can reflect this spell?
        bool m_autoRepeat;
        byte m_runesState;
        byte m_delayAtDamageCount;

        // Delayed spells system
        ulong m_delayStart;                                // time of spell delay start, filled by event handler, zero = just started
        ulong m_delayMoment;                               // moment of next delay call, used internally
        bool m_launchHandled;                               // were launch actions handled
        bool m_immediateHandled;                            // were immediate actions handled? (used by delayed spells only)

        // These vars are used in both delayed spell system and modified immediate spell system
        bool m_referencedFromCurrentSpell;
        bool m_executedCurrently;
        internal bool m_needComboPoints;
        uint m_applyMultiplierMask;
        float[] m_damageMultipliers = new float[SpellConst.MaxEffects];

        // Current targets, to be used in SpellEffects (MUST BE USED ONLY IN SPELL EFFECTS)
        public Unit unitTarget;
        public Item itemTarget;
        public GameObject gameObjTarget;
        public WorldLocation destTarget;
        public int damage;
        public SpellMissInfo targetMissInfo;
        float _variance;
        SpellEffectHandleMode effectHandleMode;
        public SpellEffectInfo effectInfo;
        // used in effects handlers
        Unit unitCaster;
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
        internal ProcFlags m_procAttacker;                // Attacker trigger flags
        internal ProcFlags m_procVictim;                  // Victim   trigger flags
        internal ProcFlagsHit m_hitMask;

        // *****************************************
        // Spell target subsystem
        // *****************************************
        // Targets store structures and data
        List<TargetInfo> m_UniqueTargetInfo = new();
        uint m_channelTargetEffectMask;                        // Mask req. alive targets

        List<GOTargetInfo> m_UniqueGOTargetInfo = new();

        List<ItemTargetInfo> m_UniqueItemInfo = new();

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
            if (MissCondition == SpellMissInfo.None)
                _spellHitTarget = unit;
            else if (MissCondition == SpellMissInfo.Reflect && ReflectResult == SpellMissInfo.None)
                _spellHitTarget = spell.GetCaster().ToUnit();

            _enablePVP = false; // need to check PvP state before spell effects, but act on it afterwards
            if (_spellHitTarget)
            {
                // if target is flagged for pvp also flag caster if a player
                if (unit.IsPvP() && spell.GetCaster().IsPlayer())
                    _enablePVP = true; // Decide on PvP flagging now, but act on it later.

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

            if (unit.IsAlive() != IsAlive)
                return;

            if (spell.GetState() == SpellState.Delayed && !spell.IsPositive() && (GameTime.GetGameTimeMS() - TimeDelay) <= unit.LastSanctuaryTime)
                return;                                             // No missinfo in that case

            if (_spellHitTarget)
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
            if (_spellHitTarget)
                spell.unitTarget = _spellHitTarget;

            // Reset damage/healing counter
            spell.m_damage = Damage;
            spell.m_healing = Healing;

            // Get original caster (if exist) and calculate damage/healing from him data
            // Skip if m_originalCaster not available
            Unit caster = spell.GetOriginalCaster() ? spell.GetOriginalCaster() : spell.GetCaster().ToUnit();
            if (caster == null)
                return;

            // Fill base trigger info
            ProcFlags procAttacker = spell.m_procAttacker;
            ProcFlags procVictim = spell.m_procVictim;
            ProcFlagsSpellType procSpellType = ProcFlagsSpellType.None;
            ProcFlagsHit hitMask = ProcFlagsHit.None;

            // Spells with this flag cannot trigger if effect is cast on self
            bool canEffectTrigger = !spell.m_spellInfo.HasAttribute(SpellAttr3.CantTriggerProc) && spell.unitTarget.CanProc() &&
                (spell.CanExecuteTriggersOnHit(EffectMask) || MissCondition == SpellMissInfo.Immune || MissCondition == SpellMissInfo.Immune2);

            // Trigger info was not filled in Spell::prepareDataForTriggerSystem - we do it now
            if (canEffectTrigger && procAttacker == 0 && procVictim == 0)
            {
                bool positive = true;
                if (spell.m_damage > 0)
                    positive = false;
                else if (spell.m_healing == 0)
                {
                    for (uint i = 0; i < SpellConst.MaxEffects; ++i)
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
                    case SpellDmgClass.Magic:
                        if (positive)
                        {
                            procAttacker |= ProcFlags.DoneSpellMagicDmgClassPos;
                            procVictim |= ProcFlags.TakenSpellMagicDmgClassPos;
                        }
                        else
                        {
                            procAttacker |= ProcFlags.DoneSpellMagicDmgClassNeg;
                            procVictim |= ProcFlags.TakenSpellMagicDmgClassNeg;
                        }
                        break;
                    case SpellDmgClass.None:
                        if (positive)
                        {
                            procAttacker |= ProcFlags.DoneSpellNoneDmgClassPos;
                            procVictim |= ProcFlags.TakenSpellNoneDmgClassPos;
                        }
                        else
                        {
                            procAttacker |= ProcFlags.DoneSpellNoneDmgClassNeg;
                            procVictim |= ProcFlags.TakenSpellNoneDmgClassNeg;
                        }
                        break;
                }
            }

            // All calculated do it!
            // Do healing
            DamageInfo spellDamageInfo = null;
            HealInfo healInfo = null;
            if (spell.m_healing > 0)
            {
                int addhealth = spell.m_healing;
                if (IsCrit)
                {
                    hitMask |= ProcFlagsHit.Critical;
                    addhealth = Unit.SpellCriticalHealingBonus(caster, spell.m_spellInfo, addhealth, null);
                }
                else
                    hitMask |= ProcFlagsHit.Normal;

                healInfo = new(caster, spell.unitTarget, (uint)addhealth, spell.m_spellInfo, spell.m_spellInfo.GetSchoolMask());
                caster.HealBySpell(healInfo, IsCrit);
                spell.unitTarget.GetThreatManager().ForwardThreatForAssistingMe(caster, healInfo.GetEffectiveHeal() * 0.5f, spell.m_spellInfo);
                spell.m_healing = (int)healInfo.GetEffectiveHeal();

                procSpellType |= ProcFlagsSpellType.Heal;
            }

            // Do damage
            if (spell.m_damage > 0)
            {
                // Fill base damage struct (unitTarget - is real spell target)
                SpellNonMeleeDamage damageInfo = new(caster, spell.unitTarget, spell.m_spellInfo, spell.m_SpellVisual, spell.m_spellSchoolMask, spell.m_castId);
                // Check damage immunity
                if (spell.unitTarget.IsImmunedToDamage(spell.m_spellInfo))
                {
                    hitMask = ProcFlagsHit.Immune;
                    spell.m_damage = 0;

                    // no packet found in sniffs
                }
                else
                {
                    // Add bonuses and fill damageInfo struct
                    caster.CalculateSpellDamageTaken(damageInfo, spell.m_damage, spell.m_spellInfo, spell.m_attackType, IsCrit);
                    Unit.DealDamageMods(damageInfo.attacker, damageInfo.target, ref damageInfo.damage, ref damageInfo.absorb);

                    hitMask |= Unit.CreateProcHitMask(damageInfo, MissCondition);
                    procVictim |= ProcFlags.TakenDamage;

                    spell.m_damage = (int)damageInfo.damage;

                    caster.DealSpellDamage(damageInfo, true);

                    // Send log damage message to client
                    caster.SendSpellNonMeleeDamageLog(damageInfo);
                }

                // Do triggers for unit
                if (canEffectTrigger)
                {
                    spellDamageInfo = new(damageInfo, DamageEffectType.SpellDirect, spell.m_attackType, hitMask);
                    procSpellType |= ProcFlagsSpellType.Damage;

                    if (caster.IsPlayer() && !spell.m_spellInfo.HasAttribute(SpellAttr0.StopAttackTarget) && !spell.m_spellInfo.HasAttribute(SpellAttr4.SuppressWeaponProcs) &&
                        (spell.m_spellInfo.DmgClass == SpellDmgClass.Melee || spell.m_spellInfo.DmgClass == SpellDmgClass.Ranged))
                        caster.ToPlayer().CastItemCombatSpell(spellDamageInfo);
                }
            }

            // Passive spell hits/misses or active spells only misses (only triggers)
            if (spell.m_damage <= 0 && spell.m_healing <= 0)
            {
                // Fill base damage struct (unitTarget - is real spell target)
                SpellNonMeleeDamage damageInfo = new(caster, spell.unitTarget, spell.m_spellInfo, spell.m_SpellVisual, spell.m_spellSchoolMask);
                hitMask |= Unit.CreateProcHitMask(damageInfo, MissCondition);
                // Do triggers for unit
                if (canEffectTrigger)
                {
                    spellDamageInfo = new(damageInfo, DamageEffectType.NoDamage, spell.m_attackType, hitMask);
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
                Unit.ProcSkillsAndAuras(caster, spell.unitTarget, procAttacker, procVictim, procSpellType, ProcFlagsSpellPhase.Hit, hitMask, spell, spellDamageInfo, healInfo);

                // item spells (spell hit of non-damage spell may also activate items, for example seal of corruption hidden hit)
                if (caster.IsPlayer() && procSpellType.HasAnyFlag(ProcFlagsSpellType.Damage | ProcFlagsSpellType.NoDmgHeal))
                {
                    if (spell.m_spellInfo.DmgClass == SpellDmgClass.Melee || spell.m_spellInfo.DmgClass == SpellDmgClass.Ranged)
                        if (!spell.m_spellInfo.HasAttribute(SpellAttr0.StopAttackTarget) && !spell.m_spellInfo.HasAttribute(SpellAttr4.SuppressWeaponProcs))
                            caster.ToPlayer().CastItemCombatSpell(spellDamageInfo);
                }
            }

            // set hitmask for finish procs
            spell.m_hitMask |= hitMask;

            // Do not take combo points on dodge and miss
            if (MissCondition != SpellMissInfo.None && spell.m_needComboPoints && spell.m_targets.GetUnitTargetGUID() == TargetGUID)
                spell.m_needComboPoints = false;

            // _spellHitTarget can be null if spell is missed in DoSpellHitOnUnit
            if (MissCondition != SpellMissInfo.Evade && _spellHitTarget && !spell.GetCaster().IsFriendlyTo(unit) && (!spell.IsPositive() || spell.m_spellInfo.HasEffect(SpellEffectName.Dispel)))
            {
                Unit unitCaster = spell.GetCaster().ToUnit();
                if (unitCaster != null)
                    unitCaster.AttackedTarget(unit, spell.m_spellInfo.HasInitialAggro());

                if (!unit.IsStandState())
                    unit.SetStandState(UnitStandStateType.Stand);
            }

            // Check for SPELL_ATTR7_INTERRUPT_ONLY_NONPLAYER
            if (MissCondition == SpellMissInfo.None && spell.m_spellInfo.HasAttribute(SpellAttr7.InterruptOnlyNonplayer) && !unit.IsPlayer())
                caster.CastSpell(unit, 32747, true);

            if (_spellHitTarget)
            {
                //AI functions
                if (_spellHitTarget.IsCreature())
                {
                    if (_spellHitTarget.ToCreature().IsAIEnabled)
                    {
                        if (spell.GetCaster().IsGameObject())
                            _spellHitTarget.ToCreature().GetAI().SpellHit(spell.GetCaster().ToGameObject(), spell.m_spellInfo);
                        else
                            _spellHitTarget.ToCreature().GetAI().SpellHit(spell.GetCaster().ToUnit(), spell.m_spellInfo);
                    }
                }

                if (spell.GetCaster().IsCreature() && spell.GetCaster().ToCreature().IsAIEnabled)
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
                    }
                }

                // Needs to be called after dealing damage/healing to not remove breaking on damage auras
                spell.DoTriggersOnSpellHit(_spellHitTarget, EffectMask);

                if (_enablePVP)
                    spell.GetCaster().ToPlayer().UpdatePvP(true);
            }

            spell.CallScriptAfterHitHandlers();
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

            spell.HandleEffects(null, null, go, spellEffectInfo, SpellEffectHandleMode.HitTarget);

            //AI functions
            if (go.GetAI() != null)
            {
                if (spell.GetCaster().IsGameObject())
                    go.GetAI().SpellHit(spell.GetCaster().ToGameObject(), spell.m_spellInfo);
                else
                    go.GetAI().SpellHit(spell.GetCaster().ToUnit(), spell.m_spellInfo);
            }

            if (spell.GetCaster().IsCreature() && spell.GetCaster().ToCreature().IsAIEnabled)
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

            spell.HandleEffects(null, TargetItem, null, spellEffectInfo, SpellEffectHandleMode.HitTarget);

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
    }

    // Spell modifier (used for modify other spells)
    public class SpellModifier
    {
        public SpellModifier(Aura _ownerAura)
        {
            op = SpellModOp.HealingAndDamage;
            type = SpellModType.Flat;
            value = 0;
            mask = new FlagArray128();
            spellId = 0;
            ownerAura = _ownerAura;
        }

        public SpellModOp op { get; set; }
        public SpellModType type { get; set; }
        public int value { get; set; }
        public FlagArray128 mask { get; set; }
        public uint spellId { get; set; }
        public Aura ownerAura { get; set; }
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

        public virtual bool Invoke(WorldObject obj)
        {
            if (_spellInfo.CheckTarget(_caster, obj, true) != SpellCastResult.SpellCastOk)
                return false;

            Unit unitTarget = obj.ToUnit();
            Corpse corpseTarget = obj.ToCorpse();
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
                        if (!_caster.IsValidAttackTarget(unitTarget, _spellInfo))
                            return false;
                        break;
                    case SpellTargetCheckTypes.Ally:
                        if (unitTarget.IsTotem())
                            return false;
                        if (!_caster.IsValidAssistTarget(unitTarget, _spellInfo))
                            return false;
                        break;
                    case SpellTargetCheckTypes.Party:
                        if (refUnit == null)
                            return false;
                        if (unitTarget.IsTotem())
                            return false;
                        if (!_caster.IsValidAssistTarget(unitTarget, _spellInfo))
                            return false;
                        if (!refUnit.IsInPartyWith(unitTarget))
                            return false;
                        break;
                    case SpellTargetCheckTypes.RaidClass:
                        if (!refUnit)
                            return false;
                        if (refUnit.GetClass() != unitTarget.GetClass())
                            return false;
                        goto case SpellTargetCheckTypes.Raid;
                    case SpellTargetCheckTypes.Raid:
                        if (refUnit == null)
                            return false;
                        if (unitTarget.IsTotem())
                            return false;
                        if (!_caster.IsValidAssistTarget(unitTarget, _spellInfo))
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
                    case SpellTargetCheckTypes.Threat:
                        if (!_referer.IsUnit() || _referer.ToUnit().GetThreatManager().GetThreat(unitTarget, true) <= 0.0f)
                            return false;
                        break;
                    case SpellTargetCheckTypes.Tap:
                        if (_referer.GetTypeId() != TypeId.Unit || unitTarget.GetTypeId() != TypeId.Player)
                            return false;
                        if (!_referer.ToCreature().IsTappedBy(unitTarget.ToPlayer()))
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

            _condSrcInfo.mConditionTargets[0] = obj;
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

        public WorldObjectSpellAreaTargetCheck(float range, Position position, WorldObject caster, WorldObject referer, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList, SpellTargetObjectTypes objectType)
            : base(caster, referer, spellInfo, selectionType, condList, objectType)
        {
            _range = range;
            _position = position;

        }

        public override bool Invoke(WorldObject target)
        {
            if (target.ToGameObject())
            {
                // isInRange including the dimension of the GO
                bool isInRange = target.ToGameObject().IsInRange(_position.GetPositionX(), _position.GetPositionY(), _position.GetPositionZ(), _range);
                if (!isInRange)
                    return false;
            }
            else
            {
                bool isInsideCylinder = target.IsWithinDist2d(_position, _range) && Math.Abs(target.GetPositionZ() - _position.GetPositionZ()) <= _range;
                if (!isInsideCylinder)
                    return false;
            }

            return base.Invoke(target);
        }
    }

    public class WorldObjectSpellConeTargetCheck : WorldObjectSpellAreaTargetCheck
    {
        float _coneAngle;
        float _lineWidth;

        public WorldObjectSpellConeTargetCheck(float coneAngle, float lineWidth, float range, WorldObject caster, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList, SpellTargetObjectTypes objectType)
            : base(range, caster.GetPosition(), caster, caster, spellInfo, selectionType, condList, objectType)
        {
            _coneAngle = coneAngle;
            _lineWidth = lineWidth;
        }

        public override bool Invoke(WorldObject target)
        {
            if (_spellInfo.HasAttribute(SpellCustomAttributes.ConeBack))
            {
                if (!_caster.IsInBack(target, _coneAngle))
                    return false;
            }
            else if (_spellInfo.HasAttribute(SpellCustomAttributes.ConeLine))
            {
                if (!_caster.HasInLine(target, target.GetCombatReach(), _lineWidth))
                    return false;
            }
            else
            {
                if (!_caster.IsUnit() || !_caster.ToUnit().IsWithinBoundaryRadius(target.ToUnit()))
                    // ConeAngle > 0 . select targets in front
                    // ConeAngle < 0 . select targets in back
                    if (_caster.HasInArc(_coneAngle, target) != MathFunctions.fuzzyGe(_coneAngle, 0.0f))
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
                            m_Spell.GetCaster().m_Events.AddEvent(this, m_Spell.GetDelayStart() + n_offset, false);
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
                            Cypher.Assert(n_offset == m_Spell.GetDelayMoment());

                        // re-plan the event for the delay moment
                        m_Spell.GetCaster().m_Events.AddEvent(this, e_time + m_Spell.GetDelayMoment(), false);
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
            m_Spell.GetCaster().m_Events.AddEvent(this, e_time + 1, false);
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
            if (!caster)
                return true;

            ProcFlags typeMaskActor = ProcFlags.None;
            ProcFlags typeMaskActionTarget = ProcFlags.TakenSpellMagicDmgClassNeg | ProcFlags.TakenSpellNoneDmgClassNeg;
            ProcFlagsSpellType spellTypeMask = ProcFlagsSpellType.Damage | ProcFlagsSpellType.NoDmgHeal;
            ProcFlagsSpellPhase spellPhaseMask = ProcFlagsSpellPhase.None;
            ProcFlagsHit hitMask = ProcFlagsHit.Reflect;

            Unit.ProcSkillsAndAuras(caster, _victim, typeMaskActor, typeMaskActionTarget, spellTypeMask, spellPhaseMask, hitMask, null, null, null);
            return true;
        }

        Unit _victim;
        ObjectGuid _casterGuid;
    }

    public class CastSpellExtraArgs
    {
        public TriggerCastFlags TriggerFlags;
        public Item CastItem;
        public AuraEffect TriggeringAura;
        public ObjectGuid OriginalCaster = ObjectGuid.Empty;
        public Difficulty CastDifficulty;
        public Dictionary<SpellValueMod, int> SpellValueOverrides = new();

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

        public CastSpellExtraArgs(AuraEffect eff)
        {
            TriggerFlags = TriggerCastFlags.FullMask;
            TriggeringAura = eff;
        }

        public CastSpellExtraArgs(Difficulty castDifficulty)
        {
            CastDifficulty = castDifficulty;
        }

        public CastSpellExtraArgs(SpellValueMod mod, int val)
        {
            SpellValueOverrides.Add(mod, val);
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
        public CastSpellExtraArgs AddSpellMod(SpellValueMod mod, int val)
        {
            SpellValueOverrides.Add(mod, val);
            return this;
        }
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
}