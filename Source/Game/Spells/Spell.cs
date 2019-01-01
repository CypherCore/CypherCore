/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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
using Game.Network.Packets;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Game.AI;

namespace Game.Spells
{
    public partial class Spell : IDisposable
    {
        public Spell(Unit caster, SpellInfo info, TriggerCastFlags triggerFlags, ObjectGuid originalCasterGUID = default(ObjectGuid), bool skipcheck = false)
        {
            m_spellInfo = info;
            m_caster = (info.HasAttribute(SpellAttr6.CastByCharmer) && caster.GetCharmerOrOwner() != null ? caster.GetCharmerOrOwner() : caster);
            m_spellValue = new SpellValue(caster.GetMap().GetDifficultyID(), m_spellInfo, caster);
            m_preGeneratedPath = new PathGenerator(m_caster);
            m_castItemLevel = -1;
            _effects = info.GetEffectsForDifficulty(caster.GetMap().GetDifficultyID());
            m_castId = ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, m_caster.GetMapId(), m_spellInfo.Id, m_caster.GetMap().GenerateLowGuid(HighGuid.Cast));
            m_SpellVisual = caster.GetCastSpellXSpellVisualId(m_spellInfo);

            m_customError = SpellCustomErrors.None;
            m_skipCheck = skipcheck;
            m_fromClient = false;
            m_needComboPoints = m_spellInfo.NeedsComboPoints();

            // Get data for type of attack
            m_attackType = info.GetAttackType();

            m_spellSchoolMask = m_spellInfo.GetSchoolMask();           // Can be override for some spell (wand shoot for example)

            if (m_attackType == WeaponAttackType.RangedAttack)
            {
                if ((m_caster.getClassMask() & (uint)Class.ClassMaskWandUsers) != 0 && m_caster.IsTypeId(TypeId.Player))
                {
                    Item pItem = m_caster.ToPlayer().GetWeaponForAttack(WeaponAttackType.RangedAttack);
                    if (pItem != null)
                        m_spellSchoolMask = (SpellSchoolMask)(1 << (int)pItem.GetTemplate().GetDamageType());
                }
            }

            if (!originalCasterGUID.IsEmpty())
                m_originalCasterGUID = originalCasterGUID;
            else
                m_originalCasterGUID = m_caster.GetGUID();

            if (m_originalCasterGUID == m_caster.GetGUID())
                m_originalCaster = m_caster;
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
            m_canReflect = m_spellInfo.DmgClass == SpellDmgClass.Magic && !m_spellInfo.HasAttribute(SpellAttr0.Ability)
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
                if ((target.ToUnit() && !Convert.ToBoolean(neededTargets & (SpellCastTargetFlags.UnitMask | SpellCastTargetFlags.CorpseMask)))
                    || (target.IsTypeId(TypeId.GameObject) && !Convert.ToBoolean(neededTargets & SpellCastTargetFlags.GameobjectMask))
                    || (target.IsTypeId(TypeId.Corpse) && !Convert.ToBoolean(neededTargets & SpellCastTargetFlags.CorpseMask)))
                    m_targets.RemoveObjectTarget();
            }
            else
            {
                // try to select correct unit target if not provided by client or by serverside cast
                if (Convert.ToBoolean(neededTargets & SpellCastTargetFlags.UnitMask))
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
                    else if ((m_caster.IsTypeId(TypeId.Unit)) && Convert.ToBoolean(neededTargets & (SpellCastTargetFlags.UnitEnemy | SpellCastTargetFlags.Unit)))
                        unit = m_caster.GetVictim();

                    // didn't find anything - let's use self as target
                    if (unit == null && Convert.ToBoolean(neededTargets & (SpellCastTargetFlags.UnitRaid | SpellCastTargetFlags.UnitParty | SpellCastTargetFlags.UnitAlly)))
                        unit = m_caster;

                    m_targets.SetUnitTarget(unit);
                }
            }

            // check if spell needs dst target
            if (Convert.ToBoolean(neededTargets & SpellCastTargetFlags.DestLocation))
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

            if (Convert.ToBoolean(neededTargets & SpellCastTargetFlags.SourceLocation))
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
                    Unit redirect;
                    switch (m_spellInfo.DmgClass)
                    {
                        case SpellDmgClass.Magic:
                            redirect = m_caster.GetMagicHitRedirectTarget(target, m_spellInfo);
                            break;
                        case SpellDmgClass.Melee:
                        case SpellDmgClass.Ranged:
                            redirect = m_caster.GetMeleeHitRedirectTarget(target, m_spellInfo);
                            break;
                        default:
                            redirect = null;
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
            foreach (SpellEffectInfo effect in GetEffects())
            {
                if (effect == null)
                    continue;

                // not call for empty effect.
                // Also some spells use not used effect targets for store targets for dummy effect in triggered spells
                if (!effect.IsEffect())
                    continue;

                // set expected type of implicit targets to be sent to client
                SpellCastTargetFlags implicitTargetMask = SpellInfo.GetTargetFlagMask(effect.TargetA.GetObjectType()) | SpellInfo.GetTargetFlagMask(effect.TargetB.GetObjectType());
                if (Convert.ToBoolean(implicitTargetMask & SpellCastTargetFlags.Unit))
                    m_targets.SetTargetFlag(SpellCastTargetFlags.Unit);
                if (Convert.ToBoolean(implicitTargetMask & (SpellCastTargetFlags.Gameobject | SpellCastTargetFlags.GameobjectItem)))
                    m_targets.SetTargetFlag(SpellCastTargetFlags.Gameobject);

                SelectEffectImplicitTargets(effect.EffectIndex, effect.TargetA, processedAreaEffectsMask);
                SelectEffectImplicitTargets(effect.EffectIndex, effect.TargetB, processedAreaEffectsMask);

                // Select targets of effect based on effect type
                // those are used when no valid target could be added for spell effect based on spell target type
                // some spell effects use explicit target as a default target added to target map (like SPELL_EFFECT_LEARN_SPELL)
                // some spell effects add target to target map only when target type specified (like SPELL_EFFECT_WEAPON)
                // some spell effects don't add anything to target map (confirmed with sniffs) (like SPELL_EFFECT_DESTROY_ALL_TOTEMS)
                SelectEffectTypeImplicitTargets(effect.EffectIndex);

                if (m_targets.HasDst())
                    AddDestTarget(m_targets.GetDst(), effect.EffectIndex);

                if (m_spellInfo.IsChanneled())
                {
                    uint mask = (1u << (int)effect.EffectIndex);
                    foreach (var ihit in m_UniqueTargetInfo)
                    {
                        if (Convert.ToBoolean(ihit.effectMask & mask))
                        {
                            m_channelTargetEffectMask |= mask;
                            break;
                        }
                    }
                }
            }

            if (m_targets.HasDst())
            {
                if (m_targets.HasTraj())
                {
                    float speed = m_targets.GetSpeedXY();
                    if (speed > 0.0f)
                        m_delayMoment = (ulong)Math.Floor(m_targets.GetDist2d() / speed * 1000.0f);
                }
                else if (m_spellInfo.Speed > 0.0f)
                {
                    float dist = m_caster.GetDistance(m_targets.GetDstPos());
                    if (!m_spellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
                        m_delayMoment = (ulong)Math.Floor(dist / m_spellInfo.Speed * 1000.0f);
                    else
                        m_delayMoment = (ulong)(m_spellInfo.Speed * 1000.0f);
                }
            }
        }

        void SelectEffectImplicitTargets(uint effIndex, SpellImplicitTargetInfo targetType, uint processedEffectMask)
        {
            if (targetType.GetTarget() == 0)
                return;

            uint effectMask = (uint)(1 << (int)effIndex);
            // set the same target list for all effects
            // some spells appear to need this, however this requires more research
            switch (targetType.GetSelectionCategory())
            {
                case SpellTargetSelectionCategories.Nearby:
                case SpellTargetSelectionCategories.Cone:
                case SpellTargetSelectionCategories.Area:
                    // targets for effect already selected
                    if (Convert.ToBoolean(effectMask & processedEffectMask))
                        return;
                    SpellEffectInfo _effect = GetEffect(effIndex);
                    if (_effect != null)
                    {
                        // choose which targets we can select at once
                        foreach (SpellEffectInfo effect in GetEffects())
                        {
                            if (effect == null || effect.EffectIndex <= effIndex)
                                continue;

                            if (effect.IsEffect() &&
                                _effect.TargetA.GetTarget() == effect.TargetA.GetTarget() &&
                                _effect.TargetB.GetTarget() == effect.TargetB.GetTarget() &&
                                _effect.ImplicitTargetConditions == effect.ImplicitTargetConditions &&
                                _effect.CalcRadius(m_caster) == effect.CalcRadius(m_caster) &&
                                CheckScriptEffectImplicitTargets(effIndex, effect.EffectIndex))
                            {
                                effectMask |= (uint)(1 << (int)effect.EffectIndex);
                            }
                        }
                    }
                    processedEffectMask |= effectMask;
                    break;
                default:
                    break;
            }

            switch (targetType.GetSelectionCategory())
            {
                case SpellTargetSelectionCategories.Channel:
                    SelectImplicitChannelTargets(effIndex, targetType);
                    break;
                case SpellTargetSelectionCategories.Nearby:
                    SelectImplicitNearbyTargets(effIndex, targetType, effectMask);
                    break;
                case SpellTargetSelectionCategories.Cone:
                    SelectImplicitConeTargets(effIndex, targetType, effectMask);
                    break;
                case SpellTargetSelectionCategories.Area:
                    SelectImplicitAreaTargets(effIndex, targetType, effectMask);
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
                                    SelectImplicitCasterDestTargets(effIndex, targetType);
                                    break;
                                case SpellTargetReferenceTypes.Target:
                                    SelectImplicitTargetDestTargets(effIndex, targetType);
                                    break;
                                case SpellTargetReferenceTypes.Dest:
                                    SelectImplicitDestDestTargets(effIndex, targetType);
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
                                    SelectImplicitCasterObjectTargets(effIndex, targetType);
                                    break;
                                case SpellTargetReferenceTypes.Target:
                                    SelectImplicitTargetObjectTargets(effIndex, targetType);
                                    break;
                                default:
                                    Cypher.Assert(false, "Spell.SelectEffectImplicitTargets: received not implemented select target reference type for TARGET_TYPE_OBJECT");
                                    break;
                            }
                            break;
                    }
                    break;
                case SpellTargetSelectionCategories.Nyi:
                    Log.outDebug(LogFilter.Spells, "SPELL: target type {0}, found in spellID {1}, effect {2} is not implemented yet!", m_spellInfo.Id, effIndex, targetType.GetTarget());
                    break;
                default:
                    Cypher.Assert(false, "Spell.SelectEffectImplicitTargets: received not implemented select target category");
                    break;
            }
        }

        void SelectImplicitChannelTargets(uint effIndex, SpellImplicitTargetInfo targetType)
        {
            if (targetType.GetReferenceType() != SpellTargetReferenceTypes.Caster)
            {
                Cypher.Assert(false, "Spell.SelectImplicitChannelTargets: received not implemented target reference type");
                return;
            }

            Spell channeledSpell = m_originalCaster.GetCurrentSpell(CurrentSpellTypes.Channeled);
            if (channeledSpell == null)
            {
                Log.outDebug(LogFilter.Spells, "Spell.SelectImplicitChannelTargets: cannot find channel spell for spell ID {0}, effect {1}", m_spellInfo.Id, effIndex);
                return;

            }

            switch (targetType.GetTarget())
            {
                case Targets.UnitChannelTarget:
                    {
                        foreach (ObjectGuid channelTarget in m_originalCaster.GetChannelObjects())
                        {
                            WorldObject target = Global.ObjAccessor.GetUnit(m_caster, channelTarget);
                            CallScriptObjectTargetSelectHandlers(ref target, effIndex, targetType);
                            // unit target may be no longer avalible - teleported out of map for example
                            Unit unitTarget = target ? target.ToUnit() : null;
                            if (unitTarget)
                                AddUnitTarget(unitTarget, 1u << (int)effIndex);
                            else
                                Log.outDebug(LogFilter.Spells, "SPELL: cannot find channel spell target for spell ID {0}, effect {1}", m_spellInfo.Id, effIndex);
                        }
                        break;
                    }
                case Targets.DestChannelTarget:
                    {
                        if (channeledSpell.m_targets.HasDst())
                            m_targets.SetDst(channeledSpell.m_targets);
                        else
                        {
                            var channelObjects = m_originalCaster.GetChannelObjects();
                            WorldObject target = channelObjects.Count > 0 ? Global.ObjAccessor.GetWorldObject(m_caster, channelObjects[0]) : null;
                            if (target != null)
                            {
                                CallScriptObjectTargetSelectHandlers(ref target, effIndex, targetType);
                                if (target)
                                {
                                    SpellDestination dest = new SpellDestination(target);
                                    CallScriptDestinationTargetSelectHandlers(ref dest, effIndex, targetType);
                                    m_targets.SetDst(dest);
                                }
                            }
                            else
                                Log.outDebug(LogFilter.Spells, "SPELL: cannot find channel spell destination for spell ID {0}, effect {1}", m_spellInfo.Id, effIndex);
                        }
                        break;
                    }
                case Targets.DestChannelCaster:
                    {
                        SpellDestination dest = new SpellDestination(channeledSpell.GetCaster());
                        CallScriptDestinationTargetSelectHandlers(ref dest, effIndex, targetType);
                        m_targets.SetDst(dest);
                        break;
                    }
                default:
                    Cypher.Assert(false, "Spell.SelectImplicitChannelTargets: received not implemented target type");
                    break;
            }
        }

        void SelectImplicitNearbyTargets(uint effIndex, SpellImplicitTargetInfo targetType, uint effMask)
        {
            if (targetType.GetReferenceType() != SpellTargetReferenceTypes.Caster)
            {
                Cypher.Assert(false, "Spell.SelectImplicitNearbyTargets: received not implemented target reference type");
                return;
            }

            SpellEffectInfo effect = GetEffect(effIndex);
            if (effect == null)
                return;

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
                    range = m_spellInfo.GetMaxRange(m_spellInfo.IsPositive(), m_caster, this);
                    break;
                default:
                    Cypher.Assert(false, "Spell.SelectImplicitNearbyTargets: received not implemented selection check type");
                    break;
            }

            List<Condition> condList = effect.ImplicitTargetConditions;

            // handle emergency case - try to use other provided targets if no conditions provided
            if (targetType.GetCheckType() == SpellTargetCheckTypes.Entry && (condList == null || condList.Empty()))
            {
                Log.outDebug(LogFilter.Spells, "Spell.SelectImplicitNearbyTargets: no conditions entry for target with TARGET_CHECK_ENTRY of spell ID {0}, effect {1} - selecting default targets", m_spellInfo.Id, effIndex);
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
                                finish(false);
                            }
                            return;
                        }
                        break;
                    case SpellTargetObjectTypes.Dest:
                        if (m_spellInfo.RequiresSpellFocus != 0)
                        {
                            if (focusObject != null)
                            {
                                SpellDestination dest = new SpellDestination(focusObject);
                                CallScriptDestinationTargetSelectHandlers(ref dest, effIndex, targetType);
                                m_targets.SetDst(dest);
                            }
                            else
                            {
                                SendCastResult(SpellCastResult.BadImplicitTargets);
                                finish(false);
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
                Log.outDebug(LogFilter.Spells, "Spell.SelectImplicitNearbyTargets: cannot find nearby target for spell ID {0}, effect {1}", m_spellInfo.Id, effIndex);
                SendCastResult(SpellCastResult.BadImplicitTargets);
                finish(false);
                return;
            }

            CallScriptObjectTargetSelectHandlers(ref target, effIndex, targetType);
            if (!target)
            {
                Log.outDebug(LogFilter.Spells, $"Spell.SelectImplicitNearbyTargets: OnObjectTargetSelect script hook for spell Id {m_spellInfo.Id} set NULL target, effect {effIndex}");
                SendCastResult(SpellCastResult.BadImplicitTargets);
                finish(false);
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
                        finish(false);
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
                        finish(false);
                        return;
                    }
                    break;
                case SpellTargetObjectTypes.Dest:
                    SpellDestination dest = new SpellDestination(target);
                    CallScriptDestinationTargetSelectHandlers(ref dest, effIndex, targetType);
                    m_targets.SetDst(dest);
                    break;
                default:
                    Cypher.Assert(false, "Spell.SelectImplicitNearbyTargets: received not implemented target object type");
                    break;
            }

            SelectImplicitChainTargets(effIndex, targetType, target, effMask);
        }

        void SelectImplicitConeTargets(uint effIndex, SpellImplicitTargetInfo targetType, uint effMask)
        {
            if (targetType.GetReferenceType() != SpellTargetReferenceTypes.Caster)
            {
                Cypher.Assert(false, "Spell.SelectImplicitConeTargets: received not implemented target reference type");
                return;
            }
            List<WorldObject> targets = new List<WorldObject>();
            SpellTargetObjectTypes objectType = targetType.GetObjectType();
            SpellTargetCheckTypes selectionType = targetType.GetCheckType();
            SpellEffectInfo effect = GetEffect(effIndex);
            if (effect == null)
                return;

            var condList = effect.ImplicitTargetConditions;
            float radius = effect.CalcRadius(m_caster) * m_spellValue.RadiusMod;

            GridMapTypeMask containerTypeMask = GetSearcherTypeMask(objectType, condList);
            if (containerTypeMask != 0)
            {
                var spellCone = new WorldObjectSpellConeTargetCheck(MathFunctions.DegToRad(m_spellInfo.ConeAngle), radius, m_caster, m_spellInfo, selectionType, condList);
                var searcher = new WorldObjectListSearcher(m_caster, targets, spellCone, containerTypeMask);
                SearchTargets(searcher, containerTypeMask, m_caster, m_caster.GetPosition(), radius);

                CallScriptObjectAreaTargetSelectHandlers(targets, effIndex, targetType);

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

        void SelectImplicitAreaTargets(uint effIndex, SpellImplicitTargetInfo targetType, uint effMask)
        {
            Unit referer = null;
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
                            if (Convert.ToBoolean(target.effectMask & (1 << (int)effIndex)))
                            {
                                referer = Global.ObjAccessor.GetUnit(m_caster, target.targetGUID);
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

            Position center = null;
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
            List<WorldObject> targets = new List<WorldObject>();
            SpellEffectInfo effect = GetEffect(effIndex);
            if (effect == null)
                return;
            float radius = effect.CalcRadius(m_caster) * m_spellValue.RadiusMod;
            SearchAreaTargets(targets, radius, center, referer, targetType.GetObjectType(), targetType.GetCheckType(), effect.ImplicitTargetConditions);

            CallScriptObjectAreaTargetSelectHandlers(targets, effIndex, targetType);

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

        void SelectImplicitCasterDestTargets(uint effIndex, SpellImplicitTargetInfo targetType)
        {
            SpellDestination dest = new SpellDestination(m_caster);

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
                    SpellTargetPosition st = Global.SpellMgr.GetSpellTargetPosition(m_spellInfo.Id, effIndex);
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
                        m_caster.GetClosePoint(out x, out y, out z, SharedConst.DefaultWorldObjectSize, dis, angle);

                        float ground = m_caster.GetMap().GetHeight(m_caster.GetPhaseShift(), x, y, z, true, 50.0f);
                        float liquidLevel = MapConst.VMAPInvalidHeightValue;
                        LiquidData liquidData;
                        if (m_caster.GetMap().getLiquidStatus(m_caster.GetPhaseShift(), x, y, z, MapConst.MapAllLiquidTypes, out liquidData) != 0)
                            liquidLevel = liquidData.level;

                        if (liquidLevel <= ground) // When there is no liquid Map.GetWaterOrGroundLevel returns ground level
                        {
                            SendCastResult(SpellCastResult.NotHere);
                            SendChannelUpdate(0);
                            finish(false);
                            return;
                        }

                        if (ground + 0.75 > liquidLevel)
                        {
                            SendCastResult(SpellCastResult.TooShallow);
                            SendChannelUpdate(0);
                            finish(false);
                            return;
                        }

                        dest = new SpellDestination(x, y, liquidLevel, m_caster.GetOrientation());
                        break;
                    }
                default:
                    SpellEffectInfo effect = GetEffect(effIndex);
                    if (effect != null)
                    {
                        float dist = effect.CalcRadius(m_caster);
                        float angl = targetType.CalcDirectionAngle();
                        float objSize = m_caster.GetObjectSize();

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
                                    if (!effect.HasRadius() && !effect.HasMaxRadius())
                                        dist = DefaultTotemDistance;
                                    break;
                                }
                            default:
                                break;
                        }

                        if (dist < objSize)
                            dist = objSize;

                        Position pos = dest.Position;
                        m_caster.MovePositionToFirstCollision(ref pos, dist, angl);

                        dest.Relocate(pos);
                    }
                    break;
            }
            CallScriptDestinationTargetSelectHandlers(ref dest, effIndex, targetType);
            m_targets.SetDst(dest);
        }

        void SelectImplicitTargetDestTargets(uint effIndex, SpellImplicitTargetInfo targetType)
        {
            WorldObject target = m_targets.GetObjectTarget();

            SpellDestination dest = new SpellDestination(target);

            switch (targetType.GetTarget())
            {
                case Targets.DestEnemy:
                case Targets.DestAny:
                    break;
                default:
                    SpellEffectInfo effect = GetEffect(effIndex);
                    if (effect != null)
                    {
                        float angle = targetType.CalcDirectionAngle();
                        float objSize = target.GetObjectSize();
                        float dist = effect.CalcRadius(m_caster);
                        if (dist < objSize)
                            dist = objSize;
                        else if (targetType.GetTarget() == Targets.DestRandom)
                            dist = objSize + (dist - objSize) * (float)RandomHelper.NextDouble();

                        Position pos = dest.Position;
                        target.MovePositionToFirstCollision(ref pos, dist, angle);

                        dest.Relocate(pos);
                    }
                    break;
            }

            CallScriptDestinationTargetSelectHandlers(ref dest, effIndex, targetType);
            m_targets.SetDst(dest);
        }

        void SelectImplicitDestDestTargets(uint effIndex, SpellImplicitTargetInfo targetType)
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
                case Targets.DestTraj:
                    SelectImplicitTrajTargets(effIndex);
                    return;
                default:
                    SpellEffectInfo effect = GetEffect(effIndex);
                    if (effect != null)
                    {
                        float angle = targetType.CalcDirectionAngle();
                        float dist = effect.CalcRadius(m_caster);
                        if (targetType.GetTarget() == Targets.DestRandom)
                            dist *= (float)RandomHelper.NextDouble();

                        Position pos = m_targets.GetDstPos();
                        m_caster.MovePositionToFirstCollision(ref pos, dist, angle);

                        dest.Relocate(pos);
                    }
                    break;
            }

            CallScriptDestinationTargetSelectHandlers(ref dest, effIndex, targetType);
            m_targets.ModDst(dest);
        }

        void SelectImplicitCasterObjectTargets(uint effIndex, SpellImplicitTargetInfo targetType)
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
                    target = m_caster.GetGuardianPet();
                    break;
                case Targets.UnitSummoner:
                    if (m_caster.IsSummon())
                        target = m_caster.ToTempSummon().GetSummoner();
                    break;
                case Targets.UnitVehicle:
                    target = m_caster.GetVehicleBase();
                    break;
                case Targets.UnitPassenger0:
                case Targets.UnitPassenger1:
                case Targets.UnitPassenger2:
                case Targets.UnitPassenger3:
                case Targets.UnitPassenger4:
                case Targets.UnitPassenger5:
                case Targets.UnitPassenger6:
                case Targets.UnitPassenger7:
                    if (m_caster.IsTypeId(TypeId.Unit) && m_caster.ToCreature().IsVehicle())
                        target = m_caster.GetVehicleKit().GetPassenger((sbyte)(targetType.GetTarget() - Targets.UnitPassenger0));
                    break;
                case Targets.UnitOwnCritter:
                    target = ObjectAccessor.GetCreatureOrPetOrVehicle(m_caster, m_caster.GetCritterGUID());
                    break;
                default:
                    break;
            }

            CallScriptObjectTargetSelectHandlers(ref target, effIndex, targetType);

            if (target != null && target.ToUnit())
                AddUnitTarget(target.ToUnit(), (uint)(1 << (int)effIndex), checkIfValid);
        }

        void SelectImplicitTargetObjectTargets(uint effIndex, SpellImplicitTargetInfo targetType)
        {
            WorldObject target = m_targets.GetObjectTarget();

            CallScriptObjectTargetSelectHandlers(ref target, effIndex, targetType);

            Item item = m_targets.GetItemTarget();
            if (target != null)
            {
                if (target.ToUnit())
                    AddUnitTarget(target.ToUnit(), (uint)(1 << (int)effIndex), true, false);
                else if (target.IsTypeId(TypeId.GameObject))
                    AddGOTarget(target.ToGameObject(), (uint)(1 << (int)effIndex));

                SelectImplicitChainTargets(effIndex, targetType, target, (uint)(1 << (int)effIndex));
            }
            // Script hook can remove object target and we would wrongly land here
            else if (item != null)
                AddItemTarget(item, (uint)(1 << (int)effIndex));
        }

        void SelectImplicitChainTargets(uint effIndex, SpellImplicitTargetInfo targetType, WorldObject target, uint effMask)
        {
            SpellEffectInfo effect = GetEffect(effIndex);
            if (effect == null)
                return;

            int maxTargets = effect.ChainTargets;
            Player modOwner = m_caster.GetSpellModOwner();
            if (modOwner)
                modOwner.ApplySpellMod(m_spellInfo.Id, SpellModOp.JumpTargets, ref maxTargets, this);

            if (maxTargets > 1)
            {
                // mark damage multipliers as used
                foreach (SpellEffectInfo eff in GetEffects())
                    if (eff != null && Convert.ToBoolean(effMask & (1 << (int)eff.EffectIndex)))
                        m_damageMultipliers[eff.EffectIndex] = 1.0f;
                m_applyMultiplierMask |= (byte)effMask;

                List<WorldObject> targets = new List<WorldObject>();
                SearchChainTargets(targets, (uint)maxTargets - 1, target, targetType.GetObjectType(), targetType.GetCheckType(), effect.ImplicitTargetConditions, targetType.GetTarget() == Targets.UnitChainhealAlly);

                // Chain primary target is added earlier
                CallScriptObjectAreaTargetSelectHandlers(targets, effIndex, targetType);

                foreach (var obj in targets)
                {
                    Unit unitTarget = obj.ToUnit();
                    if (unitTarget)
                        AddUnitTarget(unitTarget, effMask, false);
                }
            }
        }

        float tangent(float x)
        {
            x = (float)Math.Tan(x);
            if (x < 100000.0f && x > -100000.0f) return x;
            if (x >= 100000.0f) return 100000.0f;
            if (x <= 100000.0f) return -100000.0f;
            return 0.0f;
        }

        void SelectImplicitTrajTargets(uint effIndex)
        {
            if (!m_targets.HasTraj())
                return;

            float dist2d = m_targets.GetDist2d();
            if (dist2d == 0)
                return;

            float srcToDestDelta = m_targets.GetDstPos().posZ - m_targets.GetSrcPos().posZ;

            List<WorldObject> targets = new List<WorldObject>();
            var spellTraj = new WorldObjectSpellTrajTargetCheck(dist2d, m_targets.GetSrcPos(), m_caster, m_spellInfo);
            var searcher = new WorldObjectListSearcher(m_caster, targets, spellTraj);
            SearchTargets(searcher, GridMapTypeMask.All, m_caster, m_targets.GetSrcPos(), dist2d);
            if (targets.Empty())
                return;

            targets.Sort(new ObjectDistanceOrderPred(m_caster));

            float b = tangent(m_targets.GetPitch());
            float a = (srcToDestDelta - dist2d * b) / (dist2d * dist2d);
            if (a > -0.0001f)
                a = 0;
            Log.outError(LogFilter.Spells, "Spell.SelectTrajTargets: a {0} b {1}", a, b);

            float bestDist = m_spellInfo.GetMaxRange(false);

            foreach (var obj in targets)
            {
                if (!m_caster.HasInLine(obj, 5.0f))
                    continue;

                if (m_spellInfo.CheckTarget(m_caster, obj, true) != SpellCastResult.SpellCastOk)
                    continue;

                Unit unitTarget = obj.ToUnit();
                if (unitTarget)
                {
                    if (m_caster == obj || m_caster.IsOnVehicle(unitTarget) || unitTarget.GetVehicle())
                        continue;

                    Creature creatureTarget = unitTarget.ToCreature();
                    if (creatureTarget)
                    {
                        if (!creatureTarget.GetCreatureTemplate().TypeFlags.HasAnyFlag(CreatureTypeFlags.CanCollideWithMissiles))
                            continue;
                    }
                }

                float size = Math.Max(obj.GetObjectSize() * 0.7f, 1.0f); // 1/sqrt(3)
                // @todo all calculation should be based on src instead of m_caster
                float objDist2d = (float)(m_targets.GetSrcPos().GetExactDist2d(obj) * Math.Cos(m_targets.GetSrcPos().GetRelativeAngle(obj)));
                float dz = obj.GetPositionZ() - m_targets.GetSrcPos().posZ;

                Log.outError(LogFilter.Spells, "Spell.SelectTrajTargets: check {0}, dist between {1} {2}, height between {3} {4}.", obj.GetEntry(), objDist2d - size, objDist2d + size, dz - size, dz + size);

                float dist = objDist2d - size;
                float height = dist * (a * dist + b);
                Log.outError(LogFilter.Spells, "Spell.SelectTrajTargets: dist {0}, height {1}.", dist, height);
                if (dist < bestDist && height < dz + size && height > dz - size)
                {
                    bestDist = dist > 0 ? dist : 0;
                    break;
                }

                if (a == 0)
                {
                    height = dz - size;
                    dist = height / b;

                    if (dist > bestDist)
                        continue;
                    if (dist < objDist2d + size && dist > objDist2d - size)
                    {
                        bestDist = dist;
                        break;
                    }

                    height = dz + size;
                    dist = height / b;

                    if (dist > bestDist)
                        continue;
                    if (dist < objDist2d + size && dist > objDist2d - size)
                    {
                        bestDist = dist;
                        break;
                    }

                    continue;
                }

                height = dz - size;
                float sqrt1 = b * b + 4 * a * height;
                if (sqrt1 > 0)
                {
                    sqrt1 = (float)Math.Sqrt(sqrt1);
                    dist = (sqrt1 - b) / (2 * a);

                    if (dist > bestDist)
                        continue;
                    if (dist < objDist2d + size && dist > objDist2d - size)
                    {
                        bestDist = dist;
                        break;
                    }
                }

                height = dz + size;
                float sqrt2 = b * b + 4 * a * height;
                if (sqrt2 > 0)
                {
                    sqrt2 = (float)Math.Sqrt(sqrt2);
                    dist = (sqrt2 - b) / (2 * a);

                    if (dist > bestDist)
                        continue;
                    if (dist < objDist2d + size && dist > objDist2d - size)
                    {
                        bestDist = dist;
                        break;
                    }

                    dist = (-sqrt2 - b) / (2 * a);

                    if (dist > bestDist)
                        continue;
                    if (dist < objDist2d + size && dist > objDist2d - size)
                    {
                        bestDist = dist;
                        break;
                    }
                }

                if (sqrt1 > 0)
                {
                    dist = (-sqrt1 - b) / (2 * a);

                    if (dist > bestDist)
                        continue;
                    if (dist < objDist2d + size && dist > objDist2d - size)
                    {
                        bestDist = dist;
                        break;
                    }
                }
            }

            if (m_targets.GetSrcPos().GetExactDist2d(m_targets.GetDstPos()) > bestDist)
            {
                float x = (float)(m_targets.GetSrcPos().posX + Math.Cos(m_caster.GetOrientation()) * bestDist);
                float y = (float)(m_targets.GetSrcPos().posY + Math.Sin(m_caster.GetOrientation()) * bestDist);
                float z = m_targets.GetSrcPos().posZ + bestDist * (a * bestDist + b);
                var obj = targets[0];
                if (obj != null)
                {
                    float distSq = obj.GetExactDistSq(x, y, z);
                    float sizeSq = obj.GetObjectSize();
                    sizeSq *= sizeSq;
                    if (distSq > sizeSq)
                    {
                        float factor = 1 - (float)Math.Sqrt(sizeSq / distSq);
                        x += factor * (obj.GetPositionX() - x);
                        y += factor * (obj.GetPositionY() - y);
                        z += factor * (obj.GetPositionZ() - z);

                        distSq = obj.GetExactDistSq(x, y, z);
                    }
                }

                Position trajDst = new Position();
                trajDst.Relocate(x, y, z, m_caster.GetOrientation());
                SpellDestination dest = m_targets.GetDst();
                dest.Relocate(trajDst);

                CallScriptDestinationTargetSelectHandlers(ref dest, effIndex, new SpellImplicitTargetInfo(Targets.DestTraj));
                m_targets.ModDst(dest);
            }

            Vehicle veh = m_caster.GetVehicleKit();
            if (veh != null)
                veh.SetLastShootPos(m_targets.GetDstPos());
        }

        void SelectEffectTypeImplicitTargets(uint effIndex)
        {
            // special case for SPELL_EFFECT_SUMMON_RAF_FRIEND and SPELL_EFFECT_SUMMON_PLAYER
            // @todo this is a workaround - target shouldn't be stored in target map for those spells
            SpellEffectInfo effect = GetEffect(effIndex);
            if (effect == null)
                return;
            switch (effect.Effect)
            {
                case SpellEffectName.SummonRafFriend:
                case SpellEffectName.SummonPlayer:
                    if (m_caster.IsTypeId(TypeId.Player) && !m_caster.GetTarget().IsEmpty())
                    {
                        WorldObject rafTarget = Global.ObjAccessor.FindPlayer(m_caster.GetTarget());

                        CallScriptObjectTargetSelectHandlers(ref rafTarget, effIndex, new SpellImplicitTargetInfo());

                        if (rafTarget != null && rafTarget.IsTypeId(TypeId.Player))
                            AddUnitTarget(rafTarget.ToUnit(), (uint)(1 << (int)effIndex), false);
                    }
                    return;
                default:
                    break;
            }

            // select spell implicit targets based on effect type
            if (effect.GetImplicitTargetType() == 0)
                return;

            SpellCastTargetFlags targetMask = effect.GetMissingTargetMask();

            if (targetMask == 0)
                return;

            WorldObject target = null;

            switch (effect.GetImplicitTargetType())
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
                            AddItemTarget(itemTarget, (uint)(1 << (int)effIndex));
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

            CallScriptObjectTargetSelectHandlers(ref target, effIndex, new SpellImplicitTargetInfo());

            if (target != null)
            {
                if (target.ToUnit())
                    AddUnitTarget(target.ToUnit(), (uint)(1 << (int)effIndex), false);
                else if (target.IsTypeId(TypeId.GameObject))
                    AddGOTarget(target.ToGameObject(), (uint)(1 << (int)effIndex));
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

        void SearchTargets(Notifier notifier, GridMapTypeMask containerMask, Unit referer, Position pos, float radius)
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
                Cell cell = new Cell(p);
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
            var check = new WorldObjectSpellNearbyTargetCheck(range, m_caster, m_spellInfo, selectionType, condList);
            var searcher = new WorldObjectLastSearcher(m_caster, check, containerTypeMask);
            SearchTargets(searcher, containerTypeMask, m_caster, m_caster.GetPosition(), range);
            return searcher.GetTarget();
        }

        void SearchAreaTargets(List<WorldObject> targets, float range, Position position, Unit referer, SpellTargetObjectTypes objectType, SpellTargetCheckTypes selectionType, List<Condition> condList)
        {
            var containerTypeMask = GetSearcherTypeMask(objectType, condList);
            if (containerTypeMask == 0)
                return;
            var check = new WorldObjectSpellAreaTargetCheck(range, position, m_caster, referer, m_spellInfo, selectionType, condList);
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
                modOwner.ApplySpellMod(m_spellInfo.Id, SpellModOp.JumpDistance, ref jumpRadius, this);

            // chain lightning/heal spells and similar - allow to jump at larger distance and go out of los
            bool isBouncingFar = (m_spellInfo.HasAttribute(SpellAttr4.AreaTargetChain)
                || m_spellInfo.DmgClass == SpellDmgClass.None
                || m_spellInfo.DmgClass == SpellDmgClass.Magic);

            // max dist which spell can reach
            float searchRadius = jumpRadius;
            if (isBouncingFar)
                searchRadius *= chainTargets;

            List<WorldObject> tempTargets = new List<WorldObject>();
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
                            if ((deficit > maxHPDeficit || found == null) && target.IsWithinDist(unitTarget, jumpRadius) && target.IsWithinLOSInMap(unitTarget, ModelIgnoreFlags.M2))
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
                            if ((!isBouncingFar || target.IsWithinDist(obj, jumpRadius)) && target.IsWithinLOSInMap(obj, ModelIgnoreFlags.M2))
                                found = obj;
                        }
                        else if (target.GetDistanceOrder(obj, found) && target.IsWithinLOSInMap(obj, ModelIgnoreFlags.M2))
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

        void prepareDataForTriggerSystem()
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
                    // For other spells trigger procflags are set in Spell.DoAllEffectOnTarget
                    // Because spell positivity is dependant on target
            }

            // Hunter trap spells - activation proc for Lock and Load, Entrapment and Misdirection
            if (m_spellInfo.SpellFamilyName == SpellFamilyNames.Hunter && (m_spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x18u) ||     // Freezing and Frost Trap, Freezing Arrow
                m_spellInfo.Id == 57879 || // Snake Trap - done this way to avoid double proc
                m_spellInfo.SpellFamilyFlags[2].HasAnyFlag(0x00024000u))) // Explosive and Immolation Trap
            {
                m_procAttacker |= ProcFlags.DoneTrapActivation;

                // also fill up other flags (DoAllEffectOnTarget only fills up flag if both are not set)
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
            uint validEffectMask = 0;
            foreach (SpellEffectInfo effect in GetEffects())
                if (effect != null && (effectMask & (1 << (int)effect.EffectIndex)) != 0 && CheckEffectTarget(target, effect, losPosition))
                    validEffectMask |= 1u << (int)effect.EffectIndex;

            effectMask &= validEffectMask;

            // no effects left
            if (effectMask == 0)
                return;

            if (checkIfValid)
                if (m_spellInfo.CheckTarget(m_caster, target, Implicit || m_caster.GetEntry() == SharedConst.WorldTrigger) != SpellCastResult.SpellCastOk) // skip stealth checks for GO casts
                    return;

            // Check for effect immune skip if immuned
            foreach (SpellEffectInfo effect in GetEffects())
                if (effect != null && target.IsImmunedToSpellEffect(m_spellInfo, effect.EffectIndex, m_caster))
                    effectMask &= ~(uint)(1 << (int)effect.EffectIndex);

            ObjectGuid targetGUID = target.GetGUID();

            // Lookup target in already in list
            foreach (var ihit in m_UniqueTargetInfo)
            {
                if (targetGUID == ihit.targetGUID)             // Found in list
                {
                    ihit.effectMask |= effectMask;             // Immune effects removed from mask
                    return;
                }
            }

            // This is new target calculate data for him

            // Get spell hit result on target
            TargetInfo targetInfo = new TargetInfo();
            targetInfo.targetGUID = targetGUID;                         // Store target GUID
            targetInfo.effectMask = effectMask;                         // Store all effects not immune
            targetInfo.processed = false;                              // Effects not apply on target
            targetInfo.alive = target.IsAlive();
            targetInfo.damage = 0;
            targetInfo.crit = false;

            // Calculate hit result
            if (m_originalCaster != null)
            {
                targetInfo.missCondition = m_originalCaster.SpellHitResult(target, m_spellInfo, m_canReflect && !(m_spellInfo.IsPositive() && m_caster.IsFriendlyTo(target)));
                if (m_skipCheck && targetInfo.missCondition != SpellMissInfo.Immune)
                    targetInfo.missCondition = SpellMissInfo.None;
            }
            else
                targetInfo.missCondition = SpellMissInfo.Evade;

            // Spell have speed - need calculate incoming time
            // Incoming time is zero for self casts. At least I think so.
            if (m_spellInfo.Speed > 0.0f && m_caster != target)
            {
                // calculate spell incoming interval
                // @todo this is a hack
                float dist = m_caster.GetDistance(target.GetPositionX(), target.GetPositionY(), target.GetPositionZ());

                if (dist < 5.0f)
                    dist = 5.0f;

                if (!(m_spellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation)))
                    targetInfo.timeDelay = (ulong)Math.Floor((dist / m_spellInfo.Speed) * 1000.0f);
                else
                    targetInfo.timeDelay = (ulong)(m_spellInfo.Speed * 1000.0f);
            }
            else
                targetInfo.timeDelay = 0L;

            // If target reflect spell back to caster
            if (targetInfo.missCondition == SpellMissInfo.Reflect)
            {
                // Calculate reflected spell result on caster
                targetInfo.reflectResult = m_caster.SpellHitResult(m_caster, m_spellInfo, false);

                // Proc spell reflect aura when missile hits the original target
                target.m_Events.AddEvent(new ProcReflectDelayed(target, m_originalCasterGUID), target.m_Events.CalculateTime(targetInfo.timeDelay));

                // Increase time interval for reflected spells by 1.5
                targetInfo.timeDelay += targetInfo.timeDelay >> 1;
            }
            else
                targetInfo.reflectResult = SpellMissInfo.None;

            // Calculate minimum incoming time
            if (targetInfo.timeDelay != 0 && (m_delayMoment == 0 || m_delayMoment > targetInfo.timeDelay))
                m_delayMoment = targetInfo.timeDelay;

            // Add target to list
            m_UniqueTargetInfo.Add(targetInfo);
        }

        void AddGOTarget(GameObject go, uint effectMask)
        {
            uint validEffectMask = 0;
            foreach (SpellEffectInfo effect in GetEffects())
                if (effect != null && (effectMask & (1 << (int)effect.EffectIndex)) != 0 && CheckEffectTarget(go, effect))
                    validEffectMask |= (uint)(1 << (int)effect.EffectIndex);

            effectMask &= validEffectMask;

            // no effects left
            if (effectMask == 0)
                return;

            ObjectGuid targetGUID = go.GetGUID();

            // Lookup target in already in list
            foreach (var ihit in m_UniqueGOTargetInfo)
            {
                if (targetGUID == ihit.targetGUID)                 // Found in list
                {
                    ihit.effectMask |= effectMask;                 // Add only effect mask
                    return;
                }
            }

            // This is new target calculate data for him
            GOTargetInfo target = new GOTargetInfo();
            target.targetGUID = targetGUID;
            target.effectMask = effectMask;
            target.processed = false;                              // Effects not apply on target

            // Spell have speed - need calculate incoming time
            if (m_spellInfo.Speed > 0.0f)
            {
                // calculate spell incoming interval
                float dist = m_caster.GetDistance(go.GetPositionX(), go.GetPositionY(), go.GetPositionZ());
                if (dist < 5.0f)
                    dist = 5.0f;

                if (!m_spellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
                    target.timeDelay = (ulong)Math.Floor(dist / m_spellInfo.Speed * 1000.0f);
                else
                    target.timeDelay = (ulong)(m_spellInfo.Speed * 1000.0f);

                if (m_delayMoment == 0 || m_delayMoment > target.timeDelay)
                    m_delayMoment = target.timeDelay;
            }
            else
                target.timeDelay = 0L;

            // Add target to list
            m_UniqueGOTargetInfo.Add(target);
        }

        void AddItemTarget(Item item, uint effectMask)
        {
            uint validEffectMask = 0;
            foreach (SpellEffectInfo effect in GetEffects())
                if (effect != null && (effectMask & (1 << (int)effect.EffectIndex)) != 0 && CheckEffectTarget(item, effect))
                    validEffectMask |= 1u << (int)effect.EffectIndex;

            effectMask &= validEffectMask;

            // no effects left
            if (effectMask == 0)
                return;

            // Lookup target in already in list
            foreach (var ihit in m_UniqueItemInfo)
            {
                if (item == ihit.item)                            // Found in list
                {
                    ihit.effectMask |= effectMask;                 // Add only effect mask
                    return;
                }
            }

            // This is new target add data

            ItemTargetInfo target = new ItemTargetInfo();
            target.item = item;
            target.effectMask = effectMask;

            m_UniqueItemInfo.Add(target);
        }

        void AddDestTarget(SpellDestination dest, uint effIndex)
        {
            m_destTargets[effIndex] = dest;
        }

        void DoAllEffectOnTarget(TargetInfo target)
        {
            if (target == null || target.processed)
                return;

            target.processed = true;                               // Target checked in apply effects procedure

            // Get mask of effects for target
            uint mask = target.effectMask;

            Unit unit = m_caster.GetGUID() == target.targetGUID ? m_caster : Global.ObjAccessor.GetUnit(m_caster, target.targetGUID);
            if (!unit && target.targetGUID.IsPlayer()) // only players may be targeted across maps
            {
                uint farMask = 0;
                // create far target mask
                foreach (SpellEffectInfo effect in GetEffects())
                {
                    if (effect != null && effect.IsFarUnitTargetEffect())
                        if (Convert.ToBoolean((1 << (int)effect.EffectIndex) & mask))
                            farMask |= (1u << (int)effect.EffectIndex);
                }

                if (farMask == 0)
                    return;

                // find unit in world
                unit = Global.ObjAccessor.FindPlayer(target.targetGUID);
                if (unit == null)
                    return;

                // do far effects on the unit
                // can't use default call because of threading, do stuff as fast as possible
                foreach (SpellEffectInfo effect in GetEffects())
                    if (effect != null && Convert.ToBoolean(farMask & (1 << (int)effect.EffectIndex)))
                        HandleEffects(unit, null, null, effect.EffectIndex, SpellEffectHandleMode.HitTarget);
                return;
            }

            if (!unit)
                return;

            if (unit.IsAlive() != target.alive)
                return;

            if (getState() == SpellState.Delayed && !m_spellInfo.IsPositive() && (Time.GetMSTime() - target.timeDelay) <= unit.m_lastSanctuaryTime)
                return;                                             // No missinfo in that case

            // Get original caster (if exist) and calculate damage/healing from him data
            Unit caster = m_originalCaster ?? m_caster;

            // Skip if m_originalCaster not avaiable
            if (caster == null)
                return;

            SpellMissInfo missInfo = target.missCondition;

            // Need init unitTarget by default unit (can changed in code on reflect)
            // Or on missInfo != SPELL_MISS_NONE unitTarget undefined (but need in trigger subsystem)
            unitTarget = unit;
            targetMissInfo = missInfo;

            // Reset damage/healing counter
            m_damage = target.damage;
            m_healing = -target.damage;

            // Fill base trigger info
            ProcFlags procAttacker = m_procAttacker;
            ProcFlags procVictim = m_procVictim;
            ProcFlagsHit hitMask = ProcFlagsHit.None;

            m_spellAura = null;

            //Spells with this flag cannot trigger if effect is cast on self
            bool canEffectTrigger = !m_spellInfo.HasAttribute(SpellAttr3.CantTriggerProc) && unitTarget.CanProc() && (CanExecuteTriggersOnHit(mask) || missInfo == SpellMissInfo.Immune || missInfo == SpellMissInfo.Immune2);

            Unit spellHitTarget = null;

            if (missInfo == SpellMissInfo.None)                          // In case spell hit target, do all effect on that target
                spellHitTarget = unit;
            else if (missInfo == SpellMissInfo.Reflect)                // In case spell reflect from target, do all effect on caster (if hit)
            {
                if (target.reflectResult == SpellMissInfo.None)       // If reflected spell hit caster . do all effect on him
                {
                    spellHitTarget = m_caster;
                    if (m_caster.IsTypeId(TypeId.Unit))
                        m_caster.ToCreature().LowerPlayerDamageReq((uint)target.damage);
                }
            }

            PrepareScriptHitHandlers();
            CallScriptBeforeHitHandlers(missInfo);

            bool enablePvP = false; // need to check PvP state before spell effects, but act on it afterwards

            if (spellHitTarget != null)
            {
                // if target is flagged for pvp also flag caster if a player
                if (unit.IsPvP() && m_caster.IsTypeId(TypeId.Player))
                    enablePvP = true; // Decide on PvP flagging now, but act on it later.
                SpellMissInfo missInfo2 = DoSpellHitOnUnit(spellHitTarget, mask);
                if (missInfo2 != SpellMissInfo.None)
                {
                    if (missInfo2 != SpellMissInfo.Miss)
                        m_caster.SendSpellMiss(unit, m_spellInfo.Id, missInfo2);
                    m_damage = 0;
                    spellHitTarget = null;
                }
            }

            // Do not take combo points on dodge and miss
            if (missInfo != SpellMissInfo.None && m_needComboPoints && m_targets.GetUnitTargetGUID() == target.targetGUID)
                m_needComboPoints = false;

            // Trigger info was not filled in spell.preparedatafortriggersystem - we do it now
            if (canEffectTrigger && procAttacker == 0 && procVictim == 0)
            {
                bool positive = true;
                if (m_damage > 0)
                    positive = false;
                else if (m_healing == 0)
                {
                    for (byte i = 0; i < SpellConst.MaxEffects; ++i)
                    {
                        if (!Convert.ToBoolean(target.effectMask & (1 << i)))
                            continue;

                        if (!m_spellInfo.IsPositiveEffect(i))
                        {
                            positive = false;
                            break;
                        }
                    }
                }

                switch (m_spellInfo.DmgClass)
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
            CallScriptOnHitHandlers();

            // All calculated do it!
            // Do healing and triggers
            if (m_healing > 0)
            {
                bool crit = target.crit;
                uint addhealth = (uint)m_healing;
                if (crit)
                {
                    hitMask |= ProcFlagsHit.Critical;
                    addhealth = (uint)caster.SpellCriticalHealingBonus(m_spellInfo, (int)addhealth, null);
                }
                else
                    hitMask |= ProcFlagsHit.Normal;

                HealInfo healInfo = new HealInfo(caster, unitTarget, addhealth, m_spellInfo, m_spellInfo.GetSchoolMask());
                caster.HealBySpell(healInfo, crit);
                unitTarget.getHostileRefManager().threatAssist(caster, healInfo.GetEffectiveHeal() * 0.5f, m_spellInfo);
                m_healing = (int)healInfo.GetEffectiveHeal();

                // Do triggers for unit
                if (canEffectTrigger)
                    caster.ProcSkillsAndAuras(unitTarget, procAttacker, procVictim, ProcFlagsSpellType.Heal, ProcFlagsSpellPhase.Hit, hitMask, this, null, healInfo);
            }
            // Do damage and triggers
            else if (m_damage > 0)
            {
                // Fill base damage struct (unitTarget - is real spell target)
                SpellNonMeleeDamage damageInfo = new SpellNonMeleeDamage(caster, unitTarget, m_spellInfo.Id, m_SpellVisual, m_spellSchoolMask, m_castId);

                // Check damage immunity
                if (unitTarget.IsImmunedToDamage(m_spellInfo))
                {
                    hitMask = ProcFlagsHit.Immune;
                    m_damage = 0;
                    // no packet found in sniffs
                }
                else
                {
                    // Add bonuses and fill damageInfo struct
                    caster.CalculateSpellDamageTaken(damageInfo, m_damage, m_spellInfo, m_attackType, target.crit);
                    caster.DealDamageMods(damageInfo.target, ref damageInfo.damage, ref damageInfo.absorb);

                    hitMask |= Unit.createProcHitMask(damageInfo, missInfo);
                    procVictim |= ProcFlags.TakenDamage;

                    m_damage = (int)damageInfo.damage;

                    caster.DealSpellDamage(damageInfo, true);

                    // Send log damage message to client
                    caster.SendSpellNonMeleeDamageLog(damageInfo);
                }

                // Do triggers for unit
                if (canEffectTrigger)
                {
                    DamageInfo spellDamageInfo = new DamageInfo(damageInfo, DamageEffectType.SpellDirect, m_attackType, hitMask);
                    caster.ProcSkillsAndAuras(unitTarget, procAttacker, procVictim, ProcFlagsSpellType.Damage, ProcFlagsSpellPhase.Hit, hitMask, this, spellDamageInfo, null);

                    if (caster.IsPlayer() && !m_spellInfo.HasAttribute(SpellAttr0.StopAttackTarget) &&
                        (m_spellInfo.DmgClass == SpellDmgClass.Melee || m_spellInfo.DmgClass == SpellDmgClass.Ranged))
                        caster.ToPlayer().CastItemCombatSpell(spellDamageInfo);
                }
            }
            // Passive spell hits/misses or active spells only misses (only triggers)
            else
            {
                // Fill base damage struct (unitTarget - is real spell target)
                SpellNonMeleeDamage damageInfo = new SpellNonMeleeDamage(caster, unitTarget, m_spellInfo.Id, m_SpellVisual, m_spellSchoolMask);
                hitMask |= Unit.createProcHitMask(damageInfo, missInfo);
                // Do triggers for unit
                if (canEffectTrigger)
                {
                    DamageInfo spellNoDamageInfo = new DamageInfo(damageInfo, DamageEffectType.NoDamage, m_attackType, hitMask);
                    caster.ProcSkillsAndAuras(unitTarget, procAttacker, procVictim, ProcFlagsSpellType.NoDmgHeal, ProcFlagsSpellPhase.Hit, hitMask, this, spellNoDamageInfo, null);

                    if (caster.IsPlayer() && !m_spellInfo.HasAttribute(SpellAttr0.StopAttackTarget) &&
                        (m_spellInfo.DmgClass == SpellDmgClass.Melee || m_spellInfo.DmgClass == SpellDmgClass.Ranged))
                        caster.ToPlayer().CastItemCombatSpell(spellNoDamageInfo);
                }

                // Failed Pickpocket, reveal rogue
                if (missInfo == SpellMissInfo.Resist && m_spellInfo.HasAttribute(SpellCustomAttributes.PickPocket) && unitTarget.IsTypeId(TypeId.Unit))
                {
                    m_caster.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Talk);
                    if (unitTarget.ToCreature().IsAIEnabled)
                        unitTarget.ToCreature().GetAI().AttackStart(m_caster);
                }
            }

            // set hitmask for finish procs
            m_hitMask |= hitMask;

            // spellHitTarget can be null if spell is missed in DoSpellHitOnUnit
            if (missInfo != SpellMissInfo.Evade && spellHitTarget && !m_caster.IsFriendlyTo(unit) && (!m_spellInfo.IsPositive() || m_spellInfo.HasEffect(SpellEffectName.Dispel)))
            {
                m_caster.CombatStart(unit, m_spellInfo.HasInitialAggro());

                if (!unit.IsStandState())
                    unit.SetStandState(UnitStandStateType.Stand);
            }

            // Check for SPELL_ATTR7_INTERRUPT_ONLY_NONPLAYER
            if (missInfo == SpellMissInfo.None && m_spellInfo.HasAttribute(SpellAttr7.InterruptOnlyNonplayer) && !unit.IsTypeId(TypeId.Player))
                caster.CastSpell(unit, 32747, true);

            if (spellHitTarget != null)
            {
                //AI functions
                if (spellHitTarget.IsTypeId(TypeId.Unit))
                    if (spellHitTarget.ToCreature().IsAIEnabled)
                        spellHitTarget.ToCreature().GetAI().SpellHit(m_caster, m_spellInfo);

                if (m_caster.IsTypeId(TypeId.Unit) && m_caster.ToCreature().IsAIEnabled)
                    m_caster.ToCreature().GetAI().SpellHitTarget(spellHitTarget, m_spellInfo);

                // Needs to be called after dealing damage/healing to not remove breaking on damage auras
                DoTriggersOnSpellHit(spellHitTarget, mask);

                // if target is fallged for pvp also flag caster if a player
                if (enablePvP)
                    m_caster.ToPlayer().UpdatePvP(true);

                CallScriptAfterHitHandlers();
            }
        }

        SpellMissInfo DoSpellHitOnUnit(Unit unit, uint effectMask)
        {
            if (unit == null || effectMask == 0)
                return SpellMissInfo.Evade;

            // For delayed spells immunity may be applied between missile launch and hit - check immunity for that case
            if (m_spellInfo.Speed != 0.0f && unit.IsImmunedToSpell(m_spellInfo, m_caster))
                return SpellMissInfo.Immune;

            // disable effects to which unit is immune
            SpellMissInfo returnVal = SpellMissInfo.Immune;
            foreach (SpellEffectInfo effect in GetEffects())
                if (effect != null && Convert.ToBoolean(effectMask & (1 << (int)effect.EffectIndex)))
                    if (unit.IsImmunedToSpellEffect(m_spellInfo, effect.EffectIndex, m_caster))
                        effectMask &= (uint)~(1 << (int)effect.EffectIndex);

            if (effectMask == 0)
                return returnVal;

            Player player = unit.ToPlayer();
            if (player != null)
            {
                player.StartCriteriaTimer(CriteriaTimedTypes.SpellTarget, m_spellInfo.Id);
                player.UpdateCriteria(CriteriaTypes.BeSpellTarget, m_spellInfo.Id, 0, 0, m_caster);
                player.UpdateCriteria(CriteriaTypes.BeSpellTarget2, m_spellInfo.Id);
            }

            player = m_caster.ToPlayer();
            if (player != null)
            {
                player.StartCriteriaTimer(CriteriaTimedTypes.SpellCaster, m_spellInfo.Id);
                player.UpdateCriteria(CriteriaTypes.CastSpell2, m_spellInfo.Id, 0, 0, unit);
            }

            if (m_caster != unit)
            {
                // Recheck  UNIT_FLAG_NON_ATTACKABLE for delayed spells
                if (m_spellInfo.Speed > 0.0f && unit.HasFlag(UnitFields.Flags, UnitFlags.NonAttackable) && unit.GetCharmerOrOwnerGUID() != m_caster.GetGUID())
                    return SpellMissInfo.Evade;

                if (m_caster._IsValidAttackTarget(unit, m_spellInfo))
                    unit.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Hitbyspell);
                else if (m_caster.IsFriendlyTo(unit))
                {
                    // for delayed spells ignore negative spells (after duel end) for friendly targets
                    // @todo this cause soul transfer bugged
                    // 63881 - Malady of the Mind jump spell (Yogg-Saron)
                    if (m_spellInfo.Speed > 0.0f && unit.IsTypeId(TypeId.Player) && !m_spellInfo.IsPositive() && m_spellInfo.Id != 63881)
                        return SpellMissInfo.Evade;

                    // assisting case, healing and resurrection
                    if (unit.HasUnitState(UnitState.AttackPlayer))
                    {
                        m_caster.SetContestedPvP();
                        if (m_caster.IsTypeId(TypeId.Player))
                            m_caster.ToPlayer().UpdatePvP(true);
                    }
                    if (unit.IsInCombat() && m_spellInfo.HasInitialAggro())
                    {
                        m_caster.SetInCombatState(unit.GetCombatTimer() > 0, unit);
                        unit.getHostileRefManager().threatAssist(m_caster, 0.0f);
                    }
                }
            }

            uint aura_effmask = 0;
            foreach (SpellEffectInfo effect in GetEffects())
                if (effect != null && (Convert.ToBoolean(effectMask & (1 << (int)effect.EffectIndex)) && effect.IsUnitOwnedAuraEffect()))
                    aura_effmask |= (1u << (int)effect.EffectIndex);

            // Get Data Needed for Diminishing Returns, some effects may have multiple auras, so this must be done on spell hit, not aura add
            DiminishingGroup diminishGroup = m_spellInfo.GetDiminishingReturnsGroupForSpell();

            DiminishingLevels diminishLevel = DiminishingLevels.Level1;
            if (diminishGroup != 0 && aura_effmask != 0)
            {
                diminishLevel = unit.GetDiminishing(diminishGroup);
                DiminishingReturnsType type = m_spellInfo.GetDiminishingReturnsGroupType();
                // Increase Diminishing on unit, current informations for actually casts will use values above
                if ((type == DiminishingReturnsType.Player && (unit.GetCharmerOrOwnerPlayerOrPlayerItself()
                    || (unit.IsCreature() && unit.ToCreature().GetCreatureTemplate().FlagsExtra.HasAnyFlag(CreatureFlagsExtra.AllDiminish))))
                    || type == DiminishingReturnsType.All)
                    unit.IncrDiminishing(m_spellInfo);
            }

            if (aura_effmask != 0)
            {
                if (m_originalCaster != null)
                {
                    int[] basePoints = new int[SpellConst.MaxEffects];
                    foreach (SpellEffectInfo auraSpellEffect in GetEffects())
                        if (auraSpellEffect != null)
                            basePoints[auraSpellEffect.EffectIndex] =  (m_spellValue.CustomBasePointsMask & (1 << (int)auraSpellEffect.EffectIndex)) != 0 ?
                                m_spellValue.EffectBasePoints[auraSpellEffect.EffectIndex] : auraSpellEffect.CalcBaseValue(m_originalCaster, unit, m_castItemLevel);

                    bool refresh = false;
                    bool resetPeriodicTimer = !_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.DontResetPeriodicTimer);
                    m_spellAura = Aura.TryRefreshStackOrCreate(m_spellInfo, m_castId, (byte)effectMask, unit,
                        m_originalCaster, out refresh, basePoints, m_CastItem, ObjectGuid.Empty, resetPeriodicTimer, ObjectGuid.Empty, m_castItemLevel);
                    if (m_spellAura != null)
                    {
                        // Set aura stack amount to desired value
                        if (m_spellValue.AuraStackAmount > 1)
                        {
                            if (!refresh)
                                m_spellAura.SetStackAmount(m_spellValue.AuraStackAmount);
                            else
                                m_spellAura.ModStackAmount(m_spellValue.AuraStackAmount);
                        }

                        // Now Reduce spell duration using data received at spell hit
                        int duration = m_spellAura.GetMaxDuration();
                        float diminishMod = unit.ApplyDiminishingToDuration(m_spellInfo, ref duration, m_originalCaster, diminishLevel);

                        // unit is immune to aura if it was diminished to 0 duration
                        if (diminishMod == 0.0f)
                        {
                            m_spellAura.Remove();
                            bool found = false;
                            foreach (SpellEffectInfo effect in GetEffects())
                                if (effect != null && (Convert.ToBoolean(effectMask & (1 << (int)effect.EffectIndex)) && effect.Effect != SpellEffectName.ApplyAura))
                                    found = true;
                            if (!found)
                                return SpellMissInfo.Immune;
                        }
                        else
                        {
                            ((UnitAura)m_spellAura).SetDiminishGroup(diminishGroup);

                            bool positive = m_spellAura.GetSpellInfo().IsPositive();
                            AuraApplication aurApp = m_spellAura.GetApplicationOfTarget(m_originalCaster.GetGUID());
                            if (aurApp != null)
                                positive = aurApp.IsPositive();

                            duration = m_originalCaster.ModSpellDuration(m_spellInfo, unit, duration, positive, effectMask);

                            if (duration > 0)
                            {
                                // Haste modifies duration of channeled spells
                                if (m_spellInfo.IsChanneled())
                                    m_originalCaster.ModSpellDurationTime(m_spellInfo, ref duration, this);
                                else if (m_spellInfo.HasAttribute(SpellAttr5.HasteAffectDuration))
                                {
                                    int origDuration = duration;
                                    duration = 0;
                                    foreach (SpellEffectInfo effect in GetEffects())
                                    {
                                        if (effect != null)
                                        {
                                            AuraEffect eff = m_spellAura.GetEffect(effect.EffectIndex);
                                            if (eff != null)
                                            {
                                                int period = eff.GetPeriod();
                                                if (period != 0)  // period is hastened by UNIT_MOD_CAST_SPEED
                                                    duration = Math.Max(Math.Max(origDuration / period, 1) * period, duration);
                                            }
                                        }
                                    }

                                    // if there is no periodic effect
                                    if (duration == 0)
                                        duration = (int)(origDuration * m_originalCaster.GetFloatValue(UnitFields.ModCastSpeed));
                                }
                            }

                            if (duration != m_spellAura.GetMaxDuration())
                            {
                                m_spellAura.SetMaxDuration(duration);
                                m_spellAura.SetDuration(duration);
                            }
                            m_spellAura._RegisterForTargets();
                        }
                    }
                }
            }

            foreach (SpellEffectInfo effect in GetEffects())
                if (effect != null && Convert.ToBoolean(effectMask & (1 << (int)effect.EffectIndex)))
                    HandleEffects(unit, null, null, effect.EffectIndex, SpellEffectHandleMode.HitTarget);

            return SpellMissInfo.None;
        }

        void DoTriggersOnSpellHit(Unit unit, uint effMask)
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
                        m_caster.CastSpell(unit, hit.triggeredSpell, TriggerCastFlags.FullMask);
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
                        unit.RemoveAurasDueToSpell((uint)-(id));
                    else
                        unit.CastSpell(unit, (uint)id, true, null, null, m_caster.GetGUID());
                }
            }
        }

        void DoAllEffectOnTarget(GOTargetInfo target)
        {
            if (target.processed)                                  // Check target
                return;

            target.processed = true;                               // Target checked in apply effects procedure

            uint effectMask = target.effectMask;
            if (effectMask == 0)
                return;

            GameObject go = m_caster.GetMap().GetGameObject(target.targetGUID);
            if (go == null)
                return;

            PrepareScriptHitHandlers();
            CallScriptBeforeHitHandlers(SpellMissInfo.None);

            foreach (SpellEffectInfo effect in GetEffects())
                if (effect != null && Convert.ToBoolean(effectMask & (1 << (int)effect.EffectIndex)))
                    HandleEffects(null, null, go, effect.EffectIndex, SpellEffectHandleMode.HitTarget);

            CallScriptOnHitHandlers();
            CallScriptAfterHitHandlers();
        }

        void DoAllEffectOnTarget(ItemTargetInfo target)
        {
            uint effectMask = target.effectMask;
            if (target.item == null || effectMask == 0)
                return;

            PrepareScriptHitHandlers();
            CallScriptBeforeHitHandlers(SpellMissInfo.None);

            foreach (SpellEffectInfo effect in GetEffects())
                if (effect != null && Convert.ToBoolean(effectMask & (1 << (int)effect.EffectIndex)))
                    HandleEffects(null, target.item, null, effect.EffectIndex, SpellEffectHandleMode.HitTarget);

            CallScriptOnHitHandlers();

            CallScriptAfterHitHandlers();
        }

        bool UpdateChanneledTargetList()
        {
            // Not need check return true
            if (m_channelTargetEffectMask == 0)
                return true;

            uint channelTargetEffectMask = m_channelTargetEffectMask;
            uint channelAuraMask = 0;
            foreach (SpellEffectInfo effect in GetEffects())
                if (effect != null && effect.Effect == SpellEffectName.ApplyAura)
                    channelAuraMask |= (1u << (int)effect.EffectIndex);

            channelAuraMask &= channelTargetEffectMask;

            float range = 0;
            if (channelAuraMask != 0)
            {
                range = m_spellInfo.GetMaxRange(m_spellInfo.IsPositive());
                Player modOwner = m_caster.GetSpellModOwner();
                if (modOwner != null)
                    modOwner.ApplySpellMod(m_spellInfo.Id, SpellModOp.Range, ref range, this);

                // add little tolerance level
                range += Math.Min(3.0f, range * 0.1f); // 10% but no more than 3.0f
            }

            foreach (var ihit in m_UniqueTargetInfo)
            {
                if (ihit.missCondition == SpellMissInfo.None && Convert.ToBoolean(channelTargetEffectMask & ihit.effectMask))
                {
                    Unit unit = m_caster.GetGUID() == ihit.targetGUID ? m_caster : Global.ObjAccessor.GetUnit(m_caster, ihit.targetGUID);

                    if (unit == null)
                        continue;

                    if (IsValidDeadOrAliveTarget(unit))
                    {
                        if (Convert.ToBoolean(channelAuraMask & ihit.effectMask))
                        {
                            AuraApplication aurApp = unit.GetAuraApplication(m_spellInfo.Id, m_originalCasterGUID);
                            if (aurApp != null)
                            {
                                if (m_caster != unit && !m_caster.IsWithinDistInMap(unit, range))
                                {
                                    ihit.effectMask &= ~aurApp.GetEffectMask();
                                    unit.RemoveAura(aurApp);
                                    continue;
                                }
                            }
                            else // aura is dispelled
                                continue;
                        }

                        channelTargetEffectMask &= (byte)~ihit.effectMask;   // remove from need alive mask effect that have alive target
                    }
                }
            }

            // is all effects from m_needAliveTargetMask have alive targets
            return channelTargetEffectMask == 0;
        }

        public void prepare(SpellCastTargets targets, AuraEffect triggeredByAura = null)
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
                    finish(false);
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
            SpellEvent Event = new SpellEvent(this);
            m_caster.m_Events.AddEvent(Event, m_caster.m_Events.CalculateTime(1));

            //Prevent casting at cast another spell (ServerSide check)
            if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreCastInProgress) && m_caster.IsNonMeleeSpellCast(false, true, true) && !m_castId.IsEmpty())
            {
                SendCastResult(SpellCastResult.SpellInProgress);
                finish(false);
                return;
            }

            if (Global.DisableMgr.IsDisabledFor(DisableType.Spell, m_spellInfo.Id, m_caster))
            {
                SendCastResult(SpellCastResult.SpellUnavailable);
                finish(false);
                return;
            }
            LoadScripts();

            if (m_caster.IsTypeId(TypeId.Player))
                m_caster.ToPlayer().SetSpellModTakingSpell(this, true);
            // Fill cost data (not use power for item casts
            if (!m_CastItem)
                m_powerCost = m_spellInfo.CalcPowerCost(m_caster, m_spellSchoolMask);
            if (m_caster.IsTypeId(TypeId.Player))
                m_caster.ToPlayer().SetSpellModTakingSpell(this, false);

            // Set combo point requirement
            if (Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreComboPoints) || m_CastItem != null || m_caster.m_playerMovingMe == null)
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

                // cleanup after mod system
                // triggered spell pointer can be not removed in some cases
                if (m_caster.IsTypeId(TypeId.Player))
                    m_caster.ToPlayer().SetSpellModTakingSpell(this, false);

                if (param1 != 0 || param2 != 0)
                    SendCastResult(result, param1, param2);
                else
                    SendCastResult(result);

                finish(false);
                return;
            }

            // Prepare data for triggers
            prepareDataForTriggerSystem();

            if (m_caster.IsTypeId(TypeId.Player))
            {
                if (!m_caster.ToPlayer().GetCommandStatus(PlayerCommandStates.Casttime))
                {
                    m_caster.ToPlayer().SetSpellModTakingSpell(this, true);
                    m_casttime = m_spellInfo.CalcCastTime(m_caster.getLevel(), this);
                    m_caster.ToPlayer().SetSpellModTakingSpell(this, false);
                }
                else
                    m_casttime = 0;
            }
            else
                m_casttime = m_spellInfo.CalcCastTime(m_caster.getLevel(), this);

            if (m_caster.IsTypeId(TypeId.Unit) && !m_caster.HasFlag(UnitFields.Flags, UnitFlags.PlayerControlled)) // _UNIT actually means creature. for some reason.
            {
                if (!(m_spellInfo.IsNextMeleeSwingSpell() || IsAutoRepeat() || _triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreSetFacing)))
                {
                    if (m_targets.GetObjectTarget() && m_caster != m_targets.GetObjectTarget())
                        m_caster.ToCreature().FocusTarget(this, m_targets.GetObjectTarget());
                    else if (m_spellInfo.HasAttribute(SpellAttr5.DontTurnDuringCast))
                        m_caster.ToCreature().FocusTarget(this, null);
                }
            }

            // don't allow channeled spells / spells with cast time to be casted while moving
            // exception are only channeled spells that have no casttime and SPELL_ATTR5_CAN_CHANNEL_WHEN_MOVING
            // (even if they are interrupted on moving, spells with almost immediate effect get to have their effect processed before movement interrupter kicks in)
            // don't cancel spells which are affected by a SPELL_AURA_CAST_WHILE_WALKING effect
            if (((m_spellInfo.IsChanneled() || m_casttime != 0) && m_caster.IsTypeId(TypeId.Player) && !(m_caster.IsCharmed() && m_caster.GetCharmerGUID().IsCreature()) && m_caster.isMoving() &&
                m_spellInfo.InterruptFlags.HasAnyFlag(SpellInterruptFlags.Movement) && !m_caster.HasAuraTypeWithAffectMask(AuraType.CastWhileWalking, m_spellInfo)))
            {
                // 1. Has casttime, 2. Or doesn't have flag to allow movement during channel
                if (m_casttime != 0 || !m_spellInfo.IsMoveAllowedChannel())
                {
                    SendCastResult(SpellCastResult.Moving);
                    finish(false);
                    return;
                }
            }

            // set timer base at cast time
            ReSetTimer();

            Log.outDebug(LogFilter.Spells, "Spell.prepare: spell id {0} source {1} caster {2} customCastFlags {3} mask {4}", m_spellInfo.Id, m_caster.GetEntry(), m_originalCaster != null ? (int)m_originalCaster.GetEntry() : -1, _triggeredCastFlags, m_targets.GetTargetMask());

            //Containers for channeled spells have to be set
            // @todoApply this to all casted spells if needed
            // Why check duration? 29350: channelled triggers channelled
            if (_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.CastDirectly) && (!m_spellInfo.IsChanneled() || m_spellInfo.GetMaxDuration() == 0))
                cast(true);
            else
            {
                // stealth must be removed at cast starting (at show channel bar)
                // skip triggered spell (item equip spell casting and other not explicit character casts/item uses)
                if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreAuraInterruptFlags) && m_spellInfo.IsBreakingStealth())
                {
                    m_caster.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Cast);
                    foreach (SpellEffectInfo effect in GetEffects())
                    {
                        if (effect != null && effect.GetUsedTargetObjectType() == SpellTargetObjectTypes.Unit)
                        {
                            m_caster.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.SpellAttack);
                            break;
                        }
                    }
                }

                m_caster.SetCurrentCastSpell(this);

                SendSpellStart();

                if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreGCD))
                    TriggerGlobalCooldown();

                //item: first cast may destroy item and second cast causes crash
                // commented out !m_spellInfo->StartRecoveryTime, it forces instant spells with global cooldown to be processed in spell::update
                // as a result a spell that passed CheckCast and should be processed instantly may suffer from this delayed process
                // the easiest bug to observe is LoS check in AddUnitTarget, even if spell passed the CheckCast LoS check the situation can change in spell::update
                // because target could be relocated in the meantime, making the spell fly to the air (no targets can be registered, so no effects processed, nothing in combat log)
                if (m_casttime == 0 && /*m_spellInfo.StartRecoveryTime == 0 && */ m_castItemGUID.IsEmpty() && GetCurrentContainer() == CurrentSpellTypes.Generic)
                    cast(true);
            }
        }

        public void cancel()
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
                        if (ihit.missCondition == SpellMissInfo.None)
                        {
                            Unit unit = m_caster.GetGUID() == ihit.targetGUID ? m_caster : Global.ObjAccessor.GetUnit(m_caster, ihit.targetGUID);
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

            m_caster.RemoveDynObject(m_spellInfo.Id);
            if (m_spellInfo.IsChanneled()) // if not channeled then the object for the current cast wasn't summoned yet
                m_caster.RemoveGameObject(m_spellInfo.Id, true);

            //set state back so finish will be processed
            m_spellState = oldState;

            finish(false);
        }

        public void cast(bool skipCheck = false)
        {
            if (!UpdatePointers())
            {
                // cancel the spell if UpdatePointers() returned false, something wrong happened there
                cancel();
                return;
            }

            // cancel at lost explicit target during cast
            if (!m_targets.GetObjectTargetGUID().IsEmpty() && m_targets.GetObjectTarget() == null)
            {
                cancel();
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
                    Pet playerPet = playerCaster.GetPet();
                    if (playerPet != null)
                        if (playerPet.IsAlive() && playerPet.isControlled() && Convert.ToBoolean(m_targets.GetTargetMask() & SpellCastTargetFlags.Unit))
                            playerPet.GetAI().OwnerAttacked(m_targets.GetUnitTarget());
                }
            }

            SetExecutedCurrently(true);

            if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreSetFacing))
                if (m_caster.IsTypeId(TypeId.Unit) && m_targets.GetObjectTarget() != null && m_caster != m_targets.GetObjectTarget())
                    m_caster.SetInFront(m_targets.GetObjectTarget());

            // Should this be done for original caster?
            if (m_caster.IsTypeId(TypeId.Player))
            {
                // Set spell which will drop charges for triggered cast spells
                // if not successfully casted, will be remove in finish(false)
                m_caster.ToPlayer().SetSpellModTakingSpell(this, true);
            }

            CallScriptBeforeCastHandlers();

            // skip check if done already (for instant cast spells for example)
            if (!skipCheck)
            {
                uint param1 = 0, param2 = 0;
                SpellCastResult castResult = CheckCast(false, ref param1, ref param2);
                if (castResult != SpellCastResult.SpellCastOk)
                {
                    SendCastResult(castResult, param1, param2);
                    SendInterrupted(0);

                    // cleanup after mod system
                    // triggered spell pointer can be not removed in some cases
                    if (m_caster.IsTypeId(TypeId.Player))
                        m_caster.ToPlayer().SetSpellModTakingSpell(this, false);

                    finish(false);
                    SetExecutedCurrently(false);
                    return;
                }

                // additional check after cast bar completes (must not be in CheckCast)
                // if trade not complete then remember it in trade data
                if (Convert.ToBoolean(m_targets.GetTargetMask() & SpellCastTargetFlags.TradeItem))
                {
                    if (m_caster.IsTypeId(TypeId.Player))
                    {
                        TradeData my_trade = m_caster.ToPlayer().GetTradeData();
                        if (my_trade != null)
                        {
                            if (!my_trade.IsInAcceptProcess())
                            {
                                // Spell will be casted at completing the trade. Silently ignore at this place
                                my_trade.SetSpell(m_spellInfo.Id, m_CastItem);
                                SendCastResult(SpellCastResult.DontReport);
                                SendInterrupted(0);

                                // cleanup after mod system
                                m_caster.ToPlayer().SetSpellModTakingSpell(this, false);

                                finish(false);
                                SetExecutedCurrently(false);
                                return;
                            }
                        }
                    }
                }
            }

            // if the spell allows the creature to turn while casting, then adjust server-side orientation to face the target now
            // client-side orientation is handled by the client itself, as the cast target is targeted due to Creature::FocusTarget
            if (m_caster.IsTypeId(TypeId.Unit) && !m_caster.HasFlag(UnitFields.Flags, UnitFlags.PlayerControlled))
            {
                if (!m_spellInfo.HasAttribute(SpellAttr5.DontTurnDuringCast))
                {
                    WorldObject objTarget = m_targets.GetObjectTarget();
                    if (objTarget != null)
                        m_caster.SetInFront(objTarget);
                }
            }

            SelectSpellTargets();

            // Spell may be finished after target map check
            if (m_spellState == SpellState.Finished)
            {
                SendInterrupted(0);

                // cleanup after mod system
                // triggered spell pointer can be not removed in some cases
                if (m_caster.IsTypeId(TypeId.Player))
                    m_caster.ToPlayer().SetSpellModTakingSpell(this, false);

                finish(false);
                SetExecutedCurrently(false);
                return;
            }

            if (m_spellInfo.HasAttribute(SpellAttr1.DismissPet))
            {
                Creature pet = ObjectAccessor.GetCreature(m_caster, m_caster.GetPetGUID());
                if (pet)
                    pet.DespawnOrUnsummon();
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
                    player.StartCriteriaTimer(CriteriaTimedTypes.Item, m_CastItem.GetEntry());
                    player.UpdateCriteria(CriteriaTypes.UseItem, m_CastItem.GetEntry());
                }

                player.UpdateCriteria(CriteriaTypes.CastSpell, m_spellInfo.Id);
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
            SendSpellCooldown();

            PrepareScriptHitHandlers();

            HandleLaunchPhase();

            // we must send smsg_spell_go packet before m_castItem delete in TakeCastItem()...
            SendSpellGo();

            // Okay, everything is prepared. Now we need to distinguish between immediate and evented delayed spells
            if ((m_spellInfo.Speed > 0.0f && !m_spellInfo.IsChanneled()) || m_spellInfo.HasAttribute(SpellAttr4.Unk4))
            {
                // Remove used for cast item if need (it can be already NULL after TakeReagents call
                // in case delayed spell remove item at cast delay start
                TakeCastItem();

                // Okay, maps created, now prepare flags
                m_immediateHandled = false;
                m_spellState = SpellState.Delayed;
                SetDelayStart(0);

                if (m_caster.HasUnitState(UnitState.Casting) && !m_caster.IsNonMeleeSpellCast(false, false, true))
                    m_caster.ClearUnitState(UnitState.Casting);
            }
            else
            {
                // Immediate spell, no big deal
                handle_immediate();
            }

            CallScriptAfterCastHandlers();

            var spell_triggered = Global.SpellMgr.GetSpellLinked((int)m_spellInfo.Id);
            if (spell_triggered != null)
            {
                foreach (var spellId in spell_triggered)
                {
                    if (spellId < 0)
                        m_caster.RemoveAurasDueToSpell((uint)-spellId);
                    else
                        m_caster.CastSpell(m_targets.GetUnitTarget() ?? m_caster, (uint)spellId, true);
                }
            }

            if (m_caster.IsTypeId(TypeId.Player))
            {
                m_caster.ToPlayer().SetSpellModTakingSpell(this, false);

                //Clear spell cooldowns after every spell is cast if .cheat cooldown is enabled.
                if (m_caster.ToPlayer().GetCommandStatus(PlayerCommandStates.Cooldown))
                {
                    m_caster.GetSpellHistory().ResetCooldown(m_spellInfo.Id, true);
                    m_caster.GetSpellHistory().RestoreCharge(m_spellInfo.ChargeCategoryId);
                }
            }

            SetExecutedCurrently(false);

            Creature creatureCaster = m_caster.ToCreature();
            if (creatureCaster)
                creatureCaster.ReleaseFocus(this);

            if (!m_originalCaster)
                return;

            // Handle procs on cast
            ProcFlags procAttacker = m_procAttacker;
            if (procAttacker == 0)
            {
                if (m_spellInfo.DmgClass == SpellDmgClass.Magic)
                    procAttacker = m_spellInfo.IsPositive() ? ProcFlags.DoneSpellMagicDmgClassPos : ProcFlags.DoneSpellMagicDmgClassNeg;
                else
                    procAttacker = m_spellInfo.IsPositive() ? ProcFlags.DoneSpellNoneDmgClassPos : ProcFlags.DoneSpellNoneDmgClassNeg;
            }

            ProcFlagsHit hitMask = m_hitMask;
            if (!hitMask.HasAnyFlag(ProcFlagsHit.Critical))
                hitMask |= ProcFlagsHit.Normal;

            m_originalCaster.ProcSkillsAndAuras(null, procAttacker, ProcFlags.None, ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.Cast, hitMask, this, null, null);

            // Call CreatureAI hook OnSuccessfulSpellCast
            Creature caster = m_originalCaster.ToCreature();
            if (caster)
                if (caster.IsAIEnabled)
                    caster.GetAI().OnSuccessfulSpellCast(GetSpellInfo());
        }

        void handle_immediate()
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
                        modOwner.ApplySpellMod(m_spellInfo.Id, SpellModOp.Duration, ref duration);
                    // Apply haste mods
                    m_caster.ModSpellDurationTime(m_spellInfo, ref duration, this);

                    m_spellState = SpellState.Casting;
                    m_caster.AddInterruptMask(m_spellInfo.ChannelInterruptFlags);
                    SendChannelStart((uint)duration);
                }
                else if (duration == -1)
                {
                    m_spellState = SpellState.Casting;
                    m_caster.AddInterruptMask(m_spellInfo.ChannelInterruptFlags);
                    SendChannelStart((uint)duration);
                }
            }

            PrepareTargetProcessing();

            // process immediate effects (items, ground, etc.) also initialize some variables
            _handle_immediate_phase();

            // consider spell hit for some spells without target, so they may proc on finish phase correctly
            if (m_UniqueTargetInfo.Empty())
                m_hitMask = ProcFlagsHit.Normal;
            else
            {
                foreach (var ihit in m_UniqueTargetInfo)
                    DoAllEffectOnTarget(ihit);
            }

            foreach (var ihit in m_UniqueGOTargetInfo)
                DoAllEffectOnTarget(ihit);

            FinishTargetProcessing();

            // spell is finished, perform some last features of the spell here
            _handle_finish_phase();

            // Remove used for cast item if need (it can be already NULL after TakeReagents call
            TakeCastItem();

            if (m_spellState != SpellState.Casting)
                finish(true);                                       // successfully finish spell cast (not last in case autorepeat or channel spell)
        }

        public ulong handle_delayed(ulong t_offset)
        {
            if (!UpdatePointers())
            {
                // finish the spell if UpdatePointers() returned false, something wrong happened there
                finish(false);
                return 0;
            }

            if (m_caster.IsTypeId(TypeId.Player))
                m_caster.ToPlayer().SetSpellModTakingSpell(this, true);

            ulong next_time = 0;

            PrepareTargetProcessing();

            if (!m_immediateHandled)
            {
                _handle_immediate_phase();
                m_immediateHandled = true;
            }

            bool single_missile = m_targets.HasDst();

            // now recheck units targeting correctness (need before any effects apply to prevent adding immunity at first effect not allow apply second spell effect and similar cases)
            foreach (var ihit in m_UniqueTargetInfo)
            {
                if (!ihit.processed)
                {
                    if (single_missile || ihit.timeDelay <= t_offset)
                    {
                        ihit.timeDelay = t_offset;
                        DoAllEffectOnTarget(ihit);
                    }
                    else if (next_time == 0 || ihit.timeDelay < next_time)
                        next_time = ihit.timeDelay;
                }
            }

            // now recheck gameobject targeting correctness
            foreach (var ighit in m_UniqueGOTargetInfo)
            {
                if (!ighit.processed)
                {
                    if (single_missile || ighit.timeDelay <= t_offset)
                        DoAllEffectOnTarget(ighit);
                    else if (next_time == 0 || ighit.timeDelay < next_time)
                        next_time = ighit.timeDelay;
                }
            }

            FinishTargetProcessing();

            if (m_caster.IsTypeId(TypeId.Player))
                m_caster.ToPlayer().SetSpellModTakingSpell(this, false);

            // All targets passed - need finish phase
            if (next_time == 0)
            {
                // spell is finished, perform some last features of the spell here
                _handle_finish_phase();

                finish(true);                                       // successfully finish spell cast

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
            m_spellAura = null;

            // handle some immediate features of the spell here
            HandleThreatSpells();

            PrepareScriptHitHandlers();

            // handle effects with SPELL_EFFECT_HANDLE_HIT mode
            foreach (SpellEffectInfo effect in GetEffects())
            {
                // don't do anything for empty effect
                if (effect == null || !effect.IsEffect())
                    continue;

                // call effect handlers to handle destination hit
                HandleEffects(null, null, null, effect.EffectIndex, SpellEffectHandleMode.Hit);
            }

            // process items
            foreach (var ihit in m_UniqueItemInfo)
                DoAllEffectOnTarget(ihit);
        }

        void _handle_finish_phase()
        {
            if (m_caster.m_playerMovingMe != null)
            {
                // Take for real after all targets are processed
                if (m_needComboPoints)
                    m_caster.m_playerMovingMe.ClearComboPoints();

                // Real add combo points from effects
                if (m_comboPointGain != 0)
                    m_caster.m_playerMovingMe.GainSpellComboPoints(m_comboPointGain);
            }

            if (m_caster.m_extraAttacks != 0 && m_spellInfo.HasEffect(SpellEffectName.AddExtraAttacks))
            {
                Unit victim = Global.ObjAccessor.GetUnit(m_caster, m_targets.GetOrigUnitTargetGUID());
                if (victim)
                    m_caster.HandleProcExtraAttackFor(victim);
                else
                    m_caster.m_extraAttacks = 0;
            }

            // Handle procs on finish
            if (!m_originalCaster)
                return;

            ProcFlags procAttacker = m_procAttacker;
            if (procAttacker == 0)
            {
                if (m_spellInfo.DmgClass == SpellDmgClass.Magic)
                    procAttacker = m_spellInfo.IsPositive() ? ProcFlags.DoneSpellMagicDmgClassPos : ProcFlags.DoneSpellMagicDmgClassNeg;
                else
                    procAttacker = m_spellInfo.IsPositive() ? ProcFlags.DoneSpellNoneDmgClassPos : ProcFlags.DoneSpellNoneDmgClassNeg;
            }

            m_originalCaster.ProcSkillsAndAuras(null, procAttacker, ProcFlags.None, ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.Finish, m_hitMask, this, null, null);
        }

        void SendSpellCooldown()
        {
            if (m_CastItem)
                m_caster.GetSpellHistory().HandleCooldowns(m_spellInfo, m_CastItem, this);
            else
                m_caster.GetSpellHistory().HandleCooldowns(m_spellInfo, m_castItemEntry, this);
        }

        public void update(uint difftime)
        {
            if (!UpdatePointers())
            {
                // cancel the spell if UpdatePointers() returned false, something wrong happened there
                cancel();
                return;
            }

            if (!m_targets.GetUnitTargetGUID().IsEmpty() && m_targets.GetUnitTarget() == null)
            {
                Log.outDebug(LogFilter.Spells, "Spell {0} is cancelled due to removal of target.", m_spellInfo.Id);
                cancel();
                return;
            }

            // check if the player caster has moved before the spell finished
            // with the exception of spells affected with SPELL_AURA_CAST_WHILE_WALKING effect
            SpellEffectInfo effect = GetEffect(0);
            if ((m_caster.IsTypeId(TypeId.Player) && m_timer != 0) &&
                m_caster.isMoving() && m_spellInfo.InterruptFlags.HasAnyFlag(SpellInterruptFlags.Movement) &&
                ((effect != null && effect.Effect != SpellEffectName.Stuck) || !m_caster.HasUnitMovementFlag(MovementFlag.FallingFar)) &&
                !m_caster.HasAuraTypeWithAffectMask(AuraType.CastWhileWalking, m_spellInfo))
            {
                // don't cancel for melee, autorepeat, triggered and instant spells
                if (!m_spellInfo.IsNextMeleeSwingSpell() && !IsAutoRepeat() && !IsTriggered() && !(IsChannelActive() && m_spellInfo.IsMoveAllowedChannel()))
                {
                    // if charmed by creature, trust the AI not to cheat and allow the cast to proceed
                    // @todo this is a hack, "creature" movesplines don't differentiate turning/moving right now
                    // however, checking what type of movement the spline is for every single spline would be really expensive
                    if (!m_caster.GetCharmerGUID().IsCreature())
                        cancel();
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
                            cast(m_casttime == 0);
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
                                    Unit unit = m_caster.GetGUID() == target.targetGUID ? m_caster : Global.ObjAccessor.GetUnit(m_caster, target.targetGUID);
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
                            finish();
                        }
                        break;
                    }
                default:
                    break;
            }
        }

        public void finish(bool ok = true)
        {
            if (!m_caster)
                return;

            if (m_spellState == SpellState.Finished)
                return;
            m_spellState = SpellState.Finished;

            if (m_spellInfo.IsChanneled())
                m_caster.UpdateInterruptMask();

            if (m_caster.HasUnitState(UnitState.Casting) && !m_caster.IsNonMeleeSpellCast(false, false, true))
                m_caster.ClearUnitState(UnitState.Casting);

            // Unsummon summon as possessed creatures on spell cancel
            if (m_spellInfo.IsChanneled() && m_caster.IsTypeId(TypeId.Player))
            {
                Unit charm = m_caster.GetCharm();
                if (charm != null)
                    if (charm.IsTypeId(TypeId.Unit) && charm.ToCreature().HasUnitTypeMask(UnitTypeMask.Puppet)
                        && charm.GetUInt32Value(UnitFields.CreatedBySpell) == m_spellInfo.Id)
                        ((Puppet)charm).UnSummon();
            }

            if (m_caster.IsTypeId(TypeId.Unit))
                m_caster.ToCreature().ReleaseFocus(this);

            if (!ok)
                return;

            if (m_caster.IsTypeId(TypeId.Unit) && m_caster.ToCreature().IsSummon())
            {
                // Unsummon statue
                uint spell = m_caster.GetUInt32Value(UnitFields.CreatedBySpell);
                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spell);
                if (spellInfo != null && spellInfo.IconFileDataId == 134230)
                {
                    Log.outDebug(LogFilter.Spells, "Statue {0} is unsummoned in spell {1} finish", m_caster.GetGUID().ToString(), m_spellInfo.Id);
                    m_caster.setDeathState(DeathState.JustDied);
                    return;
                }
            }

            if (IsAutoActionResetSpell())
            {
                bool found = false;
                var vIgnoreReset = m_caster.GetAuraEffectsByType(AuraType.IgnoreMeleeReset);
                foreach (var i in vIgnoreReset)
                {
                    if (i.IsAffectingSpell(m_spellInfo))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found && !m_spellInfo.HasAttribute(SpellAttr2.NotResetAutoActions))
                {
                    m_caster.resetAttackTimer(WeaponAttackType.BaseAttack);
                    if (m_caster.haveOffhandWeapon())
                        m_caster.resetAttackTimer(WeaponAttackType.OffAttack);
                    m_caster.resetAttackTimer(WeaponAttackType.RangedAttack);
                }
            }

            // potions disabled by client, send event "not in combat" if need
            if (m_caster.IsTypeId(TypeId.Player))
            {
                if (m_triggeredByAuraSpell == null)
                    m_caster.ToPlayer().UpdatePotionCooldown(this);
            }

            // Stop Attack for some spells
            if (m_spellInfo.HasAttribute(SpellAttr0.StopAttackTarget))
                m_caster.AttackStop();
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
                            foreach (SpellEffectInfo effect in spellInfo.GetEffectsForDifficulty(caster.GetMap().GetDifficultyID()))
                                if (effect.ItemType != 0)
                                    item = effect.ItemType;
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

            CastFailed castFailed = new CastFailed();
            castFailed.SpellXSpellVisualID = (int)m_SpellVisual;
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

            PetCastFailed petCastFailed = new PetCastFailed();
            FillSpellCastFailedArgs(petCastFailed, m_castId, m_spellInfo, result, SpellCustomErrors.None, param1, param2, owner.ToPlayer());
            owner.ToPlayer().SendPacket(petCastFailed);
        }

        public static void SendCastResult(Player caster, SpellInfo spellInfo, uint spellVisual, ObjectGuid cast_count, SpellCastResult result, SpellCustomErrors customError = SpellCustomErrors.None, uint? param1 = null, uint? param2 = null)
        {
            if (result == SpellCastResult.SpellCastOk)
                return;

            CastFailed packet = new CastFailed();
            packet.SpellXSpellVisualID = (int)spellVisual;
            FillSpellCastFailedArgs(packet, cast_count, spellInfo, result, customError, param1, param2, caster);
            caster.SendPacket(packet);
        }

        void SendSpellStart()
        {
            if (!IsNeedSendToClient())
                return;

            SpellCastFlags castFlags = SpellCastFlags.HasTrajectory;
            uint schoolImmunityMask = m_caster.GetSchoolImmunityMask();
            uint mechanicImmunityMask = m_caster.GetMechanicImmunityMask();
            if (schoolImmunityMask != 0 || mechanicImmunityMask != 0)
                castFlags |= SpellCastFlags.Immunity;

            if (((IsTriggered() && !m_spellInfo.IsAutoRepeatRangedSpell()) || m_triggeredByAuraSpell != null) && !m_fromClient)
                castFlags |= SpellCastFlags.Pending;

            if (m_spellInfo.HasAttribute(SpellAttr0.ReqAmmo) || m_spellInfo.HasAttribute(SpellCustomAttributes.NeedsAmmoData))
                castFlags |= SpellCastFlags.Projectile;

            if ((m_caster.IsTypeId(TypeId.Player) || (m_caster.IsTypeId(TypeId.Unit) && m_caster.IsPet())) && m_powerCost.Any(cost => cost.Power != PowerType.Health))
                castFlags |= SpellCastFlags.PowerLeftSelf;

            if (m_powerCost.Any(cost => cost.Power == PowerType.Runes))
                castFlags |= SpellCastFlags.NoGCD; // not needed, but Blizzard sends it

            SpellStart packet = new SpellStart();
            SpellCastData castData = packet.Cast;

            if (m_CastItem)
                castData.CasterGUID = m_CastItem.GetGUID();
            else
                castData.CasterGUID = m_caster.GetGUID();

            castData.CasterUnit = m_caster.GetGUID();
            castData.CastID = m_castId;
            castData.OriginalCastID = m_originalCastId;
            castData.SpellID = (int)m_spellInfo.Id;
            castData.SpellXSpellVisualID = m_SpellVisual;
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
                    powerData.Cost = m_caster.GetPower(cost.Power);
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

            if ((m_caster.IsTypeId(TypeId.Player) || (m_caster.IsTypeId(TypeId.Unit) && m_caster.IsPet())) && m_powerCost.Any(cost => cost.Power != PowerType.Health))
                castFlags |= SpellCastFlags.PowerLeftSelf; // should only be sent to self, but the current messaging doesn't make that possible

            if ((m_caster.IsTypeId(TypeId.Player)) && (m_caster.GetClass() == Class.Deathknight) &&
                m_powerCost.Any(cost => cost.Power == PowerType.Runes) && !_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnorePowerAndReagentCost))
            {
                castFlags |= SpellCastFlags.NoGCD;                   // same as in SMSG_SPELL_START
                castFlags |= SpellCastFlags.RuneList;                    // rune cooldowns list
            }

            if (m_spellInfo.HasEffect(SpellEffectName.ActivateRune))
                castFlags |= SpellCastFlags.RuneList;                    // rune cooldowns list

            if (m_targets.HasTraj())
                castFlags |= SpellCastFlags.AdjustMissile;

            if (m_spellInfo.StartRecoveryTime == 0)
                castFlags |= SpellCastFlags.NoGCD;

            SpellGo packet = new SpellGo();
            SpellCastData castData = packet.Cast;

            if (m_CastItem != null)
                castData.CasterGUID = m_CastItem.GetGUID();
            else
                castData.CasterGUID = m_caster.GetGUID();

            castData.CasterUnit = m_caster.GetGUID();
            castData.CastID = m_castId;
            castData.OriginalCastID = m_originalCastId;
            castData.SpellID = (int)m_spellInfo.Id;
            castData.SpellXSpellVisualID = m_SpellVisual;
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
                    powerData.Cost = m_caster.GetPower(cost.Power);
                    castData.RemainingPower.Add(powerData);
                }
            }

            if (Convert.ToBoolean(castFlags & SpellCastFlags.RuneList))                   // rune cooldowns list
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

            if (Convert.ToBoolean(castFlags & SpellCastFlags.AdjustMissile))
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
                if (targetInfo.effectMask == 0)                  // No effect apply - all immuned add state
                    // possibly SPELL_MISS_IMMUNE2 for this??
                    targetInfo.missCondition = SpellMissInfo.Immune2;

                if (targetInfo.missCondition == SpellMissInfo.None) // hits
                {
                    data.HitTargets.Add(targetInfo.targetGUID);

                    m_channelTargetEffectMask |= targetInfo.effectMask;
                }
                else // misses
                {
                    data.MissTargets.Add(targetInfo.targetGUID);

                    SpellMissStatus missStatus = new SpellMissStatus();
                    missStatus.Reason = (byte)targetInfo.missCondition;
                    if (targetInfo.missCondition == SpellMissInfo.Reflect)
                        missStatus.ReflectStatus = (byte)targetInfo.reflectResult;

                    data.MissStatus.Add(missStatus);
                }
            }

            foreach (GOTargetInfo targetInfo in m_UniqueGOTargetInfo)
                data.HitTargets.Add(targetInfo.targetGUID); // Always hits

            // Reset m_needAliveTargetMask for non channeled spell
            if (!m_spellInfo.IsChanneled())
                m_channelTargetEffectMask = 0;
        }

        void UpdateSpellCastDataAmmo(SpellAmmo ammo)
        {
            InventoryType ammoInventoryType = 0;
            uint ammoDisplayID = 0;

            if (m_caster.IsTypeId(TypeId.Player))
            {
                Item pItem = m_caster.ToPlayer().GetWeaponForAttack(WeaponAttackType.RangedAttack);
                if (pItem)
                {
                    ammoInventoryType = pItem.GetTemplate().GetInventoryType();
                    if (ammoInventoryType == InventoryType.Thrown)
                        ammoDisplayID = pItem.GetDisplayId(m_caster.ToPlayer());
                    else if (m_caster.HasAura(46699))      // Requires No Ammo
                    {
                        ammoDisplayID = 5996;                   // normal arrow
                        ammoInventoryType = InventoryType.Ammo;
                    }
                }
            }
            else
            {
                for (byte i = 0; i < 3; ++i)
                {
                    uint itemId = m_caster.GetVirtualItemId(i);
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
                                        ammoDisplayID = Global.DB2Mgr.GetItemDisplayId(itemId, m_caster.GetVirtualItemAppearanceMod(i));
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

            ammo.DisplayID = (int)ammoDisplayID;
            ammo.InventoryType = (sbyte)ammoInventoryType;
        }

        void SendSpellExecuteLog()
        {
            SpellExecuteLog spellExecuteLog = new SpellExecuteLog();

            spellExecuteLog.Caster = m_caster.GetGUID();
            spellExecuteLog.SpellID = m_spellInfo.Id;

            foreach (SpellEffectInfo effect in GetEffects())
            {
                if (effect == null)
                    continue;

                if (_powerDrainTargets[effect.EffectIndex].Empty() && _extraAttacksTargets[effect.EffectIndex].Empty() &&
                    _durabilityDamageTargets[effect.EffectIndex].Empty() && _genericVictimTargets[effect.EffectIndex].Empty() &&
                    _tradeSkillTargets[effect.EffectIndex].Empty() && _feedPetTargets[effect.EffectIndex].Empty())
                    continue;

                SpellExecuteLog.SpellLogEffect spellLogEffect = new SpellExecuteLog.SpellLogEffect();
                spellLogEffect.Effect = (int)effect.Effect;
                spellLogEffect.PowerDrainTargets = _powerDrainTargets[effect.EffectIndex];
                spellLogEffect.ExtraAttacksTargets = _extraAttacksTargets[effect.EffectIndex];
                spellLogEffect.DurabilityDamageTargets = _durabilityDamageTargets[effect.EffectIndex];
                spellLogEffect.GenericVictimTargets = _genericVictimTargets[effect.EffectIndex];
                spellLogEffect.TradeSkillTargets = _tradeSkillTargets[effect.EffectIndex];
                spellLogEffect.FeedPetTargets = _feedPetTargets[effect.EffectIndex];
                spellExecuteLog.Effects.Add(spellLogEffect);
            }

            if (!spellExecuteLog.Effects.Empty())
                m_caster.SendCombatLogMessage(spellExecuteLog);

            CleanupExecuteLogList();
        }

        void CleanupExecuteLogList()
        {
            _powerDrainTargets.Clear();
            _extraAttacksTargets.Clear();
            _durabilityDamageTargets.Clear();
            _genericVictimTargets.Clear();
            _tradeSkillTargets.Clear();
            _feedPetTargets.Clear();
        }

        void ExecuteLogEffectTakeTargetPower(uint effIndex, Unit target, PowerType powerType, uint points, float amplitude)
        {
            SpellLogEffectPowerDrainParams spellLogEffectPowerDrainParams;

            spellLogEffectPowerDrainParams.Victim = target.GetGUID();
            spellLogEffectPowerDrainParams.Points = points;
            spellLogEffectPowerDrainParams.PowerType = (uint)powerType;
            spellLogEffectPowerDrainParams.Amplitude = amplitude;

            _powerDrainTargets.Add(effIndex, spellLogEffectPowerDrainParams);
        }

        void ExecuteLogEffectExtraAttacks(uint effIndex, Unit victim, uint numAttacks)
        {
            SpellLogEffectExtraAttacksParams spellLogEffectExtraAttacksParams;
            spellLogEffectExtraAttacksParams.Victim = victim.GetGUID();
            spellLogEffectExtraAttacksParams.NumAttacks = numAttacks;

            _extraAttacksTargets.Add(effIndex, spellLogEffectExtraAttacksParams);
        }

        void ExecuteLogEffectInterruptCast(uint effIndex, Unit victim, uint spellId)
        {
            SpellInterruptLog data = new SpellInterruptLog();
            data.Caster = m_caster.GetGUID();
            data.Victim = victim.GetGUID();
            data.InterruptedSpellID = m_spellInfo.Id;
            data.SpellID = spellId;

            m_caster.SendMessageToSet(data, true);
        }

        void ExecuteLogEffectDurabilityDamage(uint effIndex, Unit victim, int itemId, int amount)
        {
            SpellLogEffectDurabilityDamageParams spellLogEffectDurabilityDamageParams;
            spellLogEffectDurabilityDamageParams.Victim = victim.GetGUID();
            spellLogEffectDurabilityDamageParams.ItemID = itemId;
            spellLogEffectDurabilityDamageParams.Amount = amount;

            _durabilityDamageTargets.Add(effIndex, spellLogEffectDurabilityDamageParams);
        }

        void ExecuteLogEffectOpenLock(uint effIndex, WorldObject obj)
        {
            SpellLogEffectGenericVictimParams spellLogEffectGenericVictimParams;
            spellLogEffectGenericVictimParams.Victim = obj.GetGUID();

            _genericVictimTargets.Add(effIndex, spellLogEffectGenericVictimParams);
        }

        void ExecuteLogEffectCreateItem(uint effIndex, uint entry)
        {
            SpellLogEffectTradeSkillItemParams spellLogEffectTradeSkillItemParams;
            spellLogEffectTradeSkillItemParams.ItemID = (int)entry;

            _tradeSkillTargets.Add(effIndex, spellLogEffectTradeSkillItemParams);
        }

        void ExecuteLogEffectDestroyItem(uint effIndex, uint entry)
        {
            SpellLogEffectFeedPetParams spellLogEffectFeedPetParams;
            spellLogEffectFeedPetParams.ItemID = (int)entry;

            _feedPetTargets.Add(effIndex, spellLogEffectFeedPetParams);
        }

        void ExecuteLogEffectSummonObject(uint effIndex, WorldObject obj)
        {
            SpellLogEffectGenericVictimParams spellLogEffectGenericVictimParams;
            spellLogEffectGenericVictimParams.Victim = obj.GetGUID();

            _genericVictimTargets.Add(effIndex, spellLogEffectGenericVictimParams);
        }

        void ExecuteLogEffectUnsummonObject(uint effIndex, WorldObject obj)
        {
            SpellLogEffectGenericVictimParams spellLogEffectGenericVictimParams;
            spellLogEffectGenericVictimParams.Victim = obj.GetGUID();

            _genericVictimTargets.Add(effIndex, spellLogEffectGenericVictimParams);
        }

        void ExecuteLogEffectResurrect(uint effIndex, Unit target)
        {
            SpellLogEffectGenericVictimParams spellLogEffectGenericVictimParams;
            spellLogEffectGenericVictimParams.Victim = target.GetGUID();

            _genericVictimTargets.Add(effIndex, spellLogEffectGenericVictimParams);
        }

        void SendInterrupted(byte result)
        {
            SpellFailure failurePacket = new SpellFailure();
            failurePacket.CasterUnit = m_caster.GetGUID();
            failurePacket.CastID = m_castId;
            failurePacket.SpellID = m_spellInfo.Id;
            failurePacket.SpellXSpellVisualID = m_SpellVisual;
            failurePacket.Reason = result;
            m_caster.SendMessageToSet(failurePacket, true);

            SpellFailedOther failedPacket = new SpellFailedOther();
            failedPacket.CasterUnit = m_caster.GetGUID();
            failedPacket.CastID = m_castId;
            failedPacket.SpellID = m_spellInfo.Id;
            failedPacket.Reason = result;
            m_caster.SendMessageToSet(failedPacket, true);
        }

        public void SendChannelUpdate(uint time)
        {
            if (time == 0)
            {
                m_caster.ClearDynamicValue(UnitDynamicFields.ChannelObjects);
                m_caster.SetChannelSpellId(0);
                m_caster.SetChannelSpellXSpellVisualId(0);
            }

            SpellChannelUpdate spellChannelUpdate = new SpellChannelUpdate();
            spellChannelUpdate.CasterGUID = m_caster.GetGUID();
            spellChannelUpdate.TimeRemaining = (int)time;
            m_caster.SendMessageToSet(spellChannelUpdate, true);
        }

        void SendChannelStart(uint duration)
        {
            SpellChannelStart spellChannelStart = new SpellChannelStart();
            spellChannelStart.CasterGUID = m_caster.GetGUID();
            spellChannelStart.SpellID = (int)m_spellInfo.Id;
            spellChannelStart.SpellXSpellVisualID = (int)m_SpellVisual;
            spellChannelStart.ChannelDuration = duration;

            uint schoolImmunityMask = m_caster.GetSchoolImmunityMask();
            uint mechanicImmunityMask = m_caster.GetMechanicImmunityMask();

            if (schoolImmunityMask != 0 || mechanicImmunityMask != 0)
            {
                spellChannelStart.InterruptImmunities.HasValue = true;
                spellChannelStart.InterruptImmunities.Value.SchoolImmunities = (int)schoolImmunityMask;
                spellChannelStart.InterruptImmunities.Value.Immunities = (int)mechanicImmunityMask;
            }
            m_caster.SendMessageToSet(spellChannelStart, true);

            m_timer = (int)duration;
            foreach (TargetInfo target in m_UniqueTargetInfo)
                m_caster.AddChannelObject(target.targetGUID);

            foreach (GOTargetInfo target in m_UniqueGOTargetInfo)
                m_caster.AddChannelObject(target.targetGUID);

            m_caster.SetChannelSpellId(m_spellInfo.Id);
            m_caster.SetChannelSpellXSpellVisualId(m_SpellVisual);
        }

        void SendResurrectRequest(Player target)
        {
            // get ressurector name for creature resurrections, otherwise packet will be not accepted
            // for player resurrections the name is looked up by guid
            string sentName = (m_caster.IsTypeId(TypeId.Player) ? "" : m_caster.GetName(target.GetSession().GetSessionDbLocaleIndex()));

            ResurrectRequest resurrectRequest = new ResurrectRequest();
            resurrectRequest.ResurrectOffererGUID = m_caster.GetGUID();
            resurrectRequest.ResurrectOffererVirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();

            Pet pet = target.GetPet();
            if (pet)
            {
                CharmInfo charmInfo = pet.GetCharmInfo();
                if (charmInfo != null)
                    resurrectRequest.PetNumber = charmInfo.GetPetNumber();
            }

            resurrectRequest.SpellID = m_spellInfo.Id;

            //packet.ReadBit("UseTimer"); // @todo: 6.x Has to be implemented
            resurrectRequest.Sickness = !m_caster.IsTypeId(TypeId.Player); // "you'll be afflicted with resurrection sickness"

            resurrectRequest.Name = sentName;

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

            for (int i = 0; i < proto.Effects.Count && i < 5; ++i)
            {
                if (proto.Effects[i].SpellID != 0)
                {
                    // item has limited charges
                    if (proto.Effects[i].Charges != 0)
                    {
                        if (proto.Effects[i].Charges < 0)
                            expendable = true;

                        int charges = m_CastItem.GetSpellCharges(i);

                        // item has charges left
                        if (charges != 0)
                        {
                            if (charges > 0)
                                --charges;
                            else
                                ++charges;

                            if (proto.GetMaxStackSize() == 1)
                                m_CastItem.SetSpellCharges(i, charges);
                            m_CastItem.SetState(ItemUpdateState.Changed, m_caster.ToPlayer());
                        }

                        // all charges used
                        withoutCharges = (charges == 0);
                    }
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
            if (m_CastItem != null || m_triggeredByAuraSpell != null)
                return;

            //Don't take power if the spell is cast while .cheat power is enabled.
            if (m_caster.IsTypeId(TypeId.Player))
            {
                if (m_caster.ToPlayer().GetCommandStatus(PlayerCommandStates.Power))
                    return;
            }

            foreach (SpellPowerCost cost in m_powerCost)
            {
                bool hit = true;
                if (m_caster.IsTypeId(TypeId.Player))
                {
                    if (cost.Power == PowerType.Rage || cost.Power == PowerType.Energy || cost.Power == PowerType.Runes)
                    {
                        ObjectGuid targetGUID = m_targets.GetUnitTargetGUID();
                        if (!targetGUID.IsEmpty())
                        {
                            foreach (var ihit in m_UniqueTargetInfo)
                            {
                                if (ihit.targetGUID == targetGUID)
                                {
                                    if (ihit.missCondition != SpellMissInfo.None)
                                    {
                                        hit = false;
                                        //lower spell cost on fail (by talent aura)
                                        Player modOwner = m_caster.ToPlayer().GetSpellModOwner();
                                        if (modOwner)
                                            modOwner.ApplySpellMod(m_spellInfo.Id, SpellModOp.SpellCostRefundOnFail, ref cost.Amount);
                                    }
                                    break;
                                }
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
                    m_caster.ModifyHealth(-cost.Amount);
                    continue;
                }

                if (cost.Power >= PowerType.Max)
                {
                    Log.outError(LogFilter.Spells, "Spell.TakePower: Unknown power type '{0}'", cost.Power);
                    continue;
                }

                if (hit)
                    m_caster.ModifyPower(cost.Power, -cost.Amount);
                else
                    m_caster.ModifyPower(cost.Power, -RandomHelper.IRand(0, cost.Amount / 4));
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
            if (!m_caster.IsTypeId(TypeId.Player) || m_caster.GetClass() != Class.Deathknight)
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

            ItemTemplate castItemTemplate = m_CastItem?.GetTemplate();

            // do not take reagents for these item casts
            if (castItemTemplate != null && Convert.ToBoolean(castItemTemplate.GetFlags() & ItemFlags.NoReagentCost))
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
                if (castItemTemplate != null && castItemTemplate.GetId() == itemid)
                {
                    for (int s = 0; s < castItemTemplate.Effects.Count && s < 5; ++s)
                    {
                        // CastItem will be used up and does not count as reagent
                        int charges = m_CastItem.GetSpellCharges(s);
                        if (castItemTemplate.Effects[s].Charges < 0 && Math.Abs(charges) < 2)
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
            if (m_UniqueTargetInfo.Empty())
                return;

            if (!m_spellInfo.HasInitialAggro())
                return;

            float threat = 0.0f;
            SpellThreatEntry threatEntry = Global.SpellMgr.GetSpellThreatEntry(m_spellInfo.Id);
            if (threatEntry != null)
            {
                if (threatEntry.apPctMod != 0.0f)
                    threat += threatEntry.apPctMod * m_caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack);

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
                if (ihit.missCondition != SpellMissInfo.None)
                    threatToAdd = 0.0f;

                Unit target = Global.ObjAccessor.GetUnit(m_caster, ihit.targetGUID);
                if (target == null)
                    continue;

                // positive spells distribute threat among all units that are in combat with target, like healing
                if (m_spellInfo.IsPositive())
                    target.getHostileRefManager().threatAssist(m_caster, threatToAdd, m_spellInfo);
                // for negative spells threat gets distributed among affected targets
                else
                {
                    if (!target.CanHaveThreatList())
                        continue;

                    target.AddThreat(m_caster, threatToAdd, m_spellInfo.GetSchoolMask(), m_spellInfo);
                }
            }
            Log.outDebug(LogFilter.Spells, "Spell {0}, added an additional {1} threat for {2} {3} target(s)", m_spellInfo.Id, threat, m_spellInfo.IsPositive() ? "assisting" : "harming", m_UniqueTargetInfo.Count);
        }

        void HandleEffects(Unit pUnitTarget, Item pItemTarget, GameObject pGOTarget, uint i, SpellEffectHandleMode mode)
        {
            effectHandleMode = mode;
            unitTarget = pUnitTarget;
            itemTarget = pItemTarget;
            gameObjTarget = pGOTarget;
            destTarget = m_destTargets[i].Position;

            effectInfo = GetEffect(i);
            if (effectInfo == null)
            {
                Log.outError(LogFilter.Spells, "Spell: {0} HandleEffects at EffectIndex: {1} missing effect", m_spellInfo.Id, i);
                return;
            }
            SpellEffectName eff = effectInfo.Effect;

            Log.outDebug(LogFilter.Spells, "Spell: {0} Effect : {1}", m_spellInfo.Id, eff);

            damage = CalculateDamage(i, unitTarget, out _variance);

            bool preventDefault = CallScriptEffectHandlers(i, mode);

            if (!preventDefault && eff < SpellEffectName.TotalSpellEffects)
                Global.SpellMgr.GetSpellEffectHandler(eff).Invoke(this, i);
        }

        public SpellCastResult CheckCast(bool strict)
        {
            uint param1 = 0, param2 = 0;
            return CheckCast(strict, ref param1, ref param2);
        }

        public SpellCastResult CheckCast(bool strict, ref uint param1, ref uint param2)
        {
            SpellCastResult castResult = SpellCastResult.SpellCastOk;
            // check death state
            if (!m_caster.IsAlive() && !m_spellInfo.IsPassive() && !(m_spellInfo.HasAttribute(SpellAttr0.CastableWhileDead) || (IsTriggered() && m_triggeredByAuraSpell == null)))
                return SpellCastResult.CasterDead;

            // check cooldowns to prevent cheating
            if (!m_spellInfo.IsPassive())
            {
                if (m_caster.IsTypeId(TypeId.Player))
                {
                    //can cast triggered (by aura only?) spells while have this flag
                    if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreCasterAurastate) && m_caster.ToPlayer().HasFlag(PlayerFields.Flags, PlayerFlags.AllowOnlyAbility))
                        return SpellCastResult.SpellInProgress;

                    // check if we are using a potion in combat for the 2nd+ time. Cooldown is added only after caster gets out of combat
                    if (m_caster.ToPlayer().GetLastPotionId() != 0 && m_CastItem && (m_CastItem.IsPotion() || m_spellInfo.IsCooldownStartedOnEvent()))
                        return SpellCastResult.NotReady;
                }

                if (!m_caster.GetSpellHistory().IsReady(m_spellInfo, m_castItemEntry, IsIgnoringCooldowns()))
                {
                    if (m_triggeredByAuraSpell != null)
                        return SpellCastResult.DontReport;
                    else
                        return SpellCastResult.NotReady;
                }
            }

            if (m_spellInfo.HasAttribute(SpellAttr7.IsCheatSpell) && !m_caster.HasFlag(UnitFields.Flags2, UnitFlags2.AllowCheatSpells))
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

            if (m_caster.IsTypeId(TypeId.Player) && Global.VMapMgr.isLineOfSightCalcEnabled())
            {
                if (m_spellInfo.HasAttribute(SpellAttr0.OutdoorsOnly) &&
                        !m_caster.GetMap().IsOutdoors(m_caster.GetPhaseShift(), m_caster.posX, m_caster.posY, m_caster.posZ))
                    return SpellCastResult.OnlyOutdoors;

                if (m_spellInfo.HasAttribute(SpellAttr0.IndoorsOnly) &&
                        m_caster.GetMap().IsOutdoors(m_caster.GetPhaseShift(), m_caster.posX, m_caster.posY, m_caster.posZ))
                    return SpellCastResult.OnlyIndoors;
            }

            // only check at first call, Stealth auras are already removed at second call
            // for now, ignore triggered spells
            if (strict && !Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreShapeshift))
            {
                bool checkForm = true;
                // Ignore form req aura
                var ignore = m_caster.GetAuraEffectsByType(AuraType.ModIgnoreShapeshift);
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
                    SpellCastResult shapeError = m_spellInfo.CheckShapeshift(m_caster.GetShapeshiftForm());
                    if (shapeError != SpellCastResult.SpellCastOk)
                        return shapeError;

                    if (m_spellInfo.HasAttribute(SpellAttr0.OnlyStealthed) && !m_caster.HasStealthAura())
                        return SpellCastResult.OnlyStealthed;
                }
            }

            if (m_caster.HasAuraTypeWithMiscvalue(AuraType.BlockSpellFamily, (int)m_spellInfo.SpellFamilyName))
                return SpellCastResult.SpellUnavailable;

            bool reqCombat = true;
            var stateAuras = m_caster.GetAuraEffectsByType(AuraType.AbilityIgnoreAurastate);
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
            if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreCasterAurastate))
            {
                if (m_spellInfo.CasterAuraState != 0 && !m_caster.HasAuraState(m_spellInfo.CasterAuraState, m_spellInfo, m_caster))
                    return SpellCastResult.CasterAurastate;
                if (m_spellInfo.CasterAuraStateNot != 0 && m_caster.HasAuraState(m_spellInfo.CasterAuraStateNot, m_spellInfo, m_caster))
                    return SpellCastResult.CasterAurastate;

                // Note: spell 62473 requres casterAuraSpell = triggering spell
                if (m_spellInfo.CasterAuraSpell != 0 && !m_caster.HasAura(m_spellInfo.CasterAuraSpell))
                    return SpellCastResult.CasterAurastate;
                if (m_spellInfo.ExcludeCasterAuraSpell != 0 && m_caster.HasAura(m_spellInfo.ExcludeCasterAuraSpell))
                    return SpellCastResult.CasterAurastate;

                if (reqCombat && m_caster.IsInCombat() && !m_spellInfo.CanBeUsedInCombat())
                    return SpellCastResult.AffectingCombat;
            }

            // cancel autorepeat spells if cast start when moving
            // (not wand currently autorepeat cast delayed to moving stop anyway in spell update code)
            // Do not cancel spells which are affected by a SPELL_AURA_CAST_WHILE_WALKING effect
            if (m_caster.IsTypeId(TypeId.Player) && m_caster.ToPlayer().isMoving() && (!m_caster.IsCharmed() || !m_caster.GetCharmerGUID().IsCreature()) && !m_caster.HasAuraTypeWithAffectMask(AuraType.CastWhileWalking, m_spellInfo))
            {
                // skip stuck spell to allow use it in falling case and apply spell limitations at movement
                SpellEffectInfo effect = GetEffect(0);
                if ((!m_caster.HasUnitMovementFlag(MovementFlag.FallingFar) || (effect != null && effect.Effect != SpellEffectName.Stuck)) &&
                    (IsAutoRepeat() || m_spellInfo.HasAuraInterruptFlag(SpellAuraInterruptFlags.NotSeated)))
                    return SpellCastResult.Moving;
            }

            // Check vehicle flags
            if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreCasterMountedOrOnVehicle))
            {
                SpellCastResult vehicleCheck = m_spellInfo.CheckVehicle(m_caster);
                if (vehicleCheck != SpellCastResult.SpellCastOk)
                    return vehicleCheck;
            }

            // check spell cast conditions from database
            {
                ConditionSourceInfo condInfo = new ConditionSourceInfo(m_caster, m_targets.GetObjectTarget());
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
                Unit caster = m_caster;
                if (m_originalCaster && m_caster.GetEntry() != SharedConst.WorldTrigger) // Do a simplified check for gameobject casts
                    caster = m_originalCaster;

                castResult = m_spellInfo.CheckExplicitTarget(caster, m_targets.GetObjectTarget(), m_targets.GetItemTarget());
                if (castResult != SpellCastResult.SpellCastOk)
                    return castResult;
            }

            Unit unitTarget = m_targets.GetUnitTarget();
            if (unitTarget != null)
            {
                castResult = m_spellInfo.CheckTarget(m_caster, unitTarget, m_caster.GetEntry() == SharedConst.WorldTrigger); // skip stealth checks for GO casts
                if (castResult != SpellCastResult.SpellCastOk)
                    return castResult;

                // If it's not a melee spell, check if vision is obscured by SPELL_AURA_INTERFERE_TARGETTING
                if (m_spellInfo.DmgClass != SpellDmgClass.Melee)
                {
                    foreach (var auraEffect in m_caster.GetAuraEffectsByType(AuraType.InterfereTargetting))
                        if (!m_caster.IsFriendlyTo(auraEffect.GetCaster()) && !unitTarget.HasAura(auraEffect.GetId(), auraEffect.GetCasterGUID()))
                            return SpellCastResult.VisionObscured;

                    foreach (var auraEffect in unitTarget.GetAuraEffectsByType(AuraType.InterfereTargetting))
                        if (!m_caster.IsFriendlyTo(auraEffect.GetCaster()) && (!unitTarget.HasAura(auraEffect.GetId(), auraEffect.GetCasterGUID()) || !m_caster.HasAura(auraEffect.GetId(), auraEffect.GetCasterGUID())))
                            return SpellCastResult.VisionObscured;
                }

                if (unitTarget != m_caster)
                {
                    // Must be behind the target
                    if (m_spellInfo.HasAttribute(SpellCustomAttributes.ReqCasterBehindTarget) && unitTarget.HasInArc(MathFunctions.PI, m_caster))
                        return SpellCastResult.NotBehind;

                    // Target must be facing you
                    if (m_spellInfo.HasAttribute(SpellCustomAttributes.ReqTargetFacingCaster) && !unitTarget.HasInArc(MathFunctions.PI, m_caster))
                        return SpellCastResult.NotInfront;

                    if (m_caster.GetEntry() != SharedConst.WorldTrigger) // Ignore LOS for gameobjects casts (wrongly casted by a trigger)
                    {
                        WorldObject losTarget = m_caster;
                        if (IsTriggered() && m_triggeredByAuraSpell != null)
                        {
                            DynamicObject dynObj = m_caster.GetDynObject(m_triggeredByAuraSpell.Id);
                            if (dynObj)
                                losTarget = dynObj;
                        }

                        if (!m_spellInfo.HasAttribute(SpellAttr2.CanTargetNotInLos) && !Global.DisableMgr.IsDisabledFor(DisableType.Spell, m_spellInfo.Id, null, DisableFlags.SpellLOS)
                            && !unitTarget.IsWithinLOSInMap(losTarget, ModelIgnoreFlags.M2))
                            return SpellCastResult.LineOfSight;
                    }
                }
            }

            // Check for line of sight for spells with dest
            if (m_targets.HasDst())
            {
                float x, y, z;
                m_targets.GetDstPos().GetPosition(out x, out y, out z);

                if (!m_spellInfo.HasAttribute(SpellAttr2.CanTargetNotInLos) && !Global.DisableMgr.IsDisabledFor(DisableType.Spell, m_spellInfo.Id, null, DisableFlags.SpellLOS)
                    && !m_caster.IsWithinLOS(x, y, z, ModelIgnoreFlags.M2))
                    return SpellCastResult.LineOfSight;
            }

            // check pet presence
            foreach (SpellEffectInfo effect in GetEffects())
            {
                if (effect != null && effect.TargetA.GetTarget() == Targets.UnitPet)
                {
                    if (m_caster.GetGuardianPet() == null)
                    {
                        if (m_triggeredByAuraSpell != null)              // not report pet not existence for triggered spells
                            return SpellCastResult.DontReport;
                        else
                            return SpellCastResult.NoPet;
                    }
                    break;
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
            if (m_caster.IsTypeId(TypeId.Unit) || !m_caster.ToPlayer().IsGameMaster())
            {
                uint zone, area;
                m_caster.GetZoneAndAreaId(out zone, out area);

                SpellCastResult locRes = m_spellInfo.CheckLocation(m_caster.GetMapId(), zone, area, m_caster.ToPlayer());
                if (locRes != SpellCastResult.SpellCastOk)
                    return locRes;
            }

            // not let players cast spells at mount (and let do it to creatures)
            if (m_caster.IsMounted() && m_caster.IsTypeId(TypeId.Player) && !Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreCasterMountedOrOnVehicle) &&
                !m_spellInfo.IsPassive() && !m_spellInfo.HasAttribute(SpellAttr0.CastableWhileMounted))
            {
                if (m_caster.IsInFlight())
                    return SpellCastResult.NotOnTaxi;
                else
                    return SpellCastResult.NotMounted;
            }

            // check spell focus object
            if (m_spellInfo.RequiresSpellFocus != 0)
            {
                focusObject = SearchSpellFocus();
                if (!focusObject)
                    return SpellCastResult.RequiresSpellFocus;
            }

            castResult = SpellCastResult.SpellCastOk;

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

            foreach (SpellEffectInfo effect in GetEffects())
            {
                if (effect == null)
                    continue;

                // for effects of spells that have only one target
                switch (effect.Effect)
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
                                if (target == null || !target.IsFriendlyTo(m_caster) || target.getAttackers().Empty())
                                    return SpellCastResult.BadTargets;

                            }
                            break;
                        }
                    case SpellEffectName.LearnSpell:
                        {
                            if (!m_caster.IsTypeId(TypeId.Player))
                                return SpellCastResult.BadTargets;

                            if (effect.TargetA.GetTarget() != Targets.UnitPet)
                                break;

                            Pet pet = m_caster.ToPlayer().GetPet();

                            if (pet == null)
                                return SpellCastResult.NoPet;

                            SpellInfo learn_spellproto = Global.SpellMgr.GetSpellInfo(effect.TriggerSpell);

                            if (learn_spellproto == null)
                                return SpellCastResult.NotKnown;

                            if (m_spellInfo.SpellLevel > pet.getLevel())
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

                                SpellInfo learn_spellproto = Global.SpellMgr.GetSpellInfo(effect.TriggerSpell);
                                if (learn_spellproto == null)
                                    return SpellCastResult.NotKnown;

                                if (m_spellInfo.SpellLevel > pet.getLevel())
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

                            uint glyphId = (uint)effect.MiscValue;
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
                                    if (caster.GetUInt32Value(PlayerFields.CurrentSpecId) == 0)
                                        return SpellCastResult.GlyphNoSpec;

                                    if (!glyphRequiredSpecs.Contains(caster.GetUInt32Value(PlayerFields.CurrentSpecId)))
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

                            if (pet.GetCurrentFoodBenefitLevel(foodItem.GetTemplate().GetBaseItemLevel()) == 0)
                                return SpellCastResult.FoodLowlevel;

                            if (m_caster.IsInCombat() || pet.IsInCombat())
                                return SpellCastResult.AffectingCombat;

                            break;
                        }
                    case SpellEffectName.PowerBurn:
                    case SpellEffectName.PowerDrain:
                        {
                            // Can be area effect, Check only for players and not check if target - caster (spell can have multiply drain/burn effects)
                            if (m_caster.IsTypeId(TypeId.Player))
                            {
                                Unit target = m_targets.GetUnitTarget();
                                if (target != null)
                                    if (target != m_caster && unitTarget.GetPowerType() != (PowerType)effect.MiscValue)
                                        return SpellCastResult.BadTargets;
                            }
                            break;
                        }
                    case SpellEffectName.Charge:
                        {
                            if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreCasterAuras) && m_caster.HasUnitState(UnitState.Root))
                                return SpellCastResult.Rooted;

                            if (GetSpellInfo().NeedsExplicitUnitTarget())
                            {
                                Unit target = m_targets.GetUnitTarget();
                                if (target == null)
                                    return SpellCastResult.DontReport;

                                // first we must check to see if the target is in LoS. A path can usually be built but LoS matters for charge spells
                                if (!target.IsWithinLOSInMap(m_caster)) //Do full LoS/Path check. Don't exclude m2
                                    return SpellCastResult.LineOfSight;

                                float objSize = target.GetObjectSize();
                                float range = m_spellInfo.GetMaxRange(true, m_caster, this) * 1.5f + objSize; // can't be overly strict

                                m_preGeneratedPath.SetPathLengthLimit(range);
                                //first try with raycast, if it fails fall back to normal path
                                float targetObjectSize = Math.Min(target.GetObjectSize(), 4.0f);
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

                                m_preGeneratedPath.ReducePathLenghtByDist(objSize); //move back
                            }
                            break;
                        }
                    case SpellEffectName.Skinning:
                        {
                            if (!m_caster.IsTypeId(TypeId.Player) || m_targets.GetUnitTarget() == null || !m_targets.GetUnitTarget().IsTypeId(TypeId.Unit))
                                return SpellCastResult.BadTargets;

                            if (!Convert.ToBoolean(m_targets.GetUnitTarget().GetUInt32Value(UnitFields.Flags) & (uint)UnitFlags.Skinnable))
                                return SpellCastResult.TargetUnskinnable;

                            Creature creature = m_targets.GetUnitTarget().ToCreature();
                            if (!creature.IsCritter() && !creature.loot.isLooted())
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
                            if (effect.TargetA.GetTarget() != Targets.GameobjectTarget &&
                                effect.TargetA.GetTarget() != Targets.GameobjectItemTarget)
                                break;

                            if (!m_caster.IsTypeId(TypeId.Player)  // only players can open locks, gather etc.
                                                                   // we need a go target in case of TARGET_GAMEOBJECT_TARGET
                                || (effect.TargetA.GetTarget() == Targets.GameobjectTarget && m_targets.GetGOTarget() == null))
                                return SpellCastResult.BadTargets;

                            Item pTempItem = null;
                            if (Convert.ToBoolean(m_targets.GetTargetMask() & SpellCastTargetFlags.TradeItem))
                            {
                                TradeData pTrade = m_caster.ToPlayer().GetTradeData();
                                if (pTrade != null)
                                    pTempItem = pTrade.GetTraderData().GetItem((TradeSlots)m_targets.GetItemTargetGUID().GetLowValue());
                            }
                            else if (Convert.ToBoolean(m_targets.GetTargetMask() & SpellCastTargetFlags.Item))
                                pTempItem = m_caster.ToPlayer().GetItemByGuid(m_targets.GetItemTargetGUID());

                            // we need a go target, or an openable item target in case of TARGET_GAMEOBJECT_ITEM_TARGET
                            if (effect.TargetA.GetTarget() == Targets.GameobjectItemTarget &&
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
                            SpellCastResult res = CanOpenLock(effect.EffectIndex, lockId, ref skillId, ref reqSkillValue, ref skillValue);
                            if (res != SpellCastResult.SpellCastOk)
                                return res;
                            break;
                        }
                    case SpellEffectName.ResurrectPet:
                        {
                            Creature pet = m_caster.GetGuardianPet();

                            if (pet != null && pet.IsAlive())
                                return SpellCastResult.AlreadyHaveSummon;

                            break;
                        }
                    // This is generic summon effect
                    case SpellEffectName.Summon:
                        {
                            var SummonProperties = CliDB.SummonPropertiesStorage.LookupByKey(effect.MiscValueB);
                            if (SummonProperties == null)
                                break;

                            switch (SummonProperties.Control)
                            {
                                case SummonCategory.Pet:
                                    if (!m_spellInfo.HasAttribute(SpellAttr1.DismissPet) && !m_caster.GetPetGUID().IsEmpty())
                                        return SpellCastResult.AlreadyHaveSummon;
                                    break;
                                case SummonCategory.Puppet:
                                    if (!m_caster.GetCharmGUID().IsEmpty())
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
                            if (!m_caster.GetPetGUID().IsEmpty())                  //let warlock do a replacement summon
                            {
                                if (m_caster.IsTypeId(TypeId.Player))
                                {
                                    if (strict)                         //starting cast, trigger pet stun (cast by pet so it doesn't attack player)
                                    {
                                        Pet pet = m_caster.ToPlayer().GetPet();
                                        if (pet != null)
                                            pet.CastSpell(pet, 32752, true, null, null, pet.GetGUID());
                                    }
                                }
                                else if (!m_spellInfo.HasAttribute(SpellAttr1.DismissPet))
                                    return SpellCastResult.AlreadyHaveSummon;
                            }

                            if (!m_caster.GetCharmGUID().IsEmpty())
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
                            if (m_targets.GetUnitTarget() == m_caster)
                                return SpellCastResult.BadTargets;
                            break;
                        }
                    case SpellEffectName.LeapBack:
                        {
                            if (m_caster.HasUnitState(UnitState.Root))
                            {
                                if (m_caster.IsTypeId(TypeId.Player))
                                    return SpellCastResult.Rooted;
                                else
                                    return SpellCastResult.DontReport;
                            }
                            break;
                        }
                    case SpellEffectName.TalentSpecSelect:
                        {
                            ChrSpecializationRecord spec = CliDB.ChrSpecializationStorage.LookupByKey(m_misc.SpecializationId);
                            Player playerCaster = m_caster.ToPlayer();
                            if (!playerCaster)
                                return SpellCastResult.TargetNotPlayer;

                            if (spec == null || (spec.ClassID != (uint)m_caster.GetClass() && !spec.IsPetSpecialization()))
                                return SpellCastResult.NoSpec;

                            if (spec.IsPetSpecialization())
                            {
                                Pet pet = player.GetPet();
                                if (!pet || pet.getPetType() != PetType.Hunter || pet.GetCharmInfo() == null)
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
                            if (!m_caster.IsTypeId(TypeId.Player))
                                return SpellCastResult.BadTargets;

                            TalentRecord talent = CliDB.TalentStorage.LookupByKey(m_misc.TalentId);
                            if (talent == null)
                                return SpellCastResult.DontReport;
                            if (m_caster.GetSpellHistory().HasCooldown(talent.SpellID))
                            {
                                param1 = talent.SpellID;
                                return SpellCastResult.CantUntalent;
                            }
                            break;
                        }
                    case SpellEffectName.GiveArtifactPower:
                    case SpellEffectName.GiveArtifactPowerNoBonus:
                        {
                            if (!m_caster.IsTypeId(TypeId.Player))
                                return SpellCastResult.BadTargets;

                            Aura artifactAura = m_caster.GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);
                            if (artifactAura == null)
                                return SpellCastResult.NoArtifactEquipped;
                            Item artifact = m_caster.ToPlayer().GetItemByGuid(artifactAura.GetCastItemGUID());
                            if (artifact == null)
                                return SpellCastResult.NoArtifactEquipped;
                            if (effect.Effect == SpellEffectName.GiveArtifactPower)
                            {
                                ArtifactRecord artifactEntry = CliDB.ArtifactStorage.LookupByKey(artifact.GetTemplate().GetArtifactID());
                                if (artifactEntry == null || artifactEntry.ArtifactCategoryID != effect.MiscValue)
                                    return SpellCastResult.WrongArtifactEquipped;
                            }
                            break;
                        }
                    default:
                        break;
                }
            }

            foreach (SpellEffectInfo effect in GetEffects())
            {
                if (effect == null)
                    continue;

                switch (effect.ApplyAuraName)
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
                            if (!m_caster.GetCharmerGUID().IsEmpty())
                                return SpellCastResult.Charmed;

                            if (effect.ApplyAuraName == AuraType.ModCharm
                                || effect.ApplyAuraName == AuraType.ModPossess)
                            {
                                if (!m_spellInfo.HasAttribute(SpellAttr1.DismissPet) && !m_caster.GetPetGUID().IsEmpty())
                                    return SpellCastResult.AlreadyHaveSummon;

                                if (!m_caster.GetCharmGUID().IsEmpty())
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

                                int damage = CalculateDamage(effect.EffectIndex, target);
                                if (damage != 0 && target.GetLevelForTarget(m_caster) > damage)
                                    return SpellCastResult.Highlevel;
                            }

                            break;
                        }
                    case AuraType.Mounted:
                        {
                            if (m_caster.IsInWater() && m_spellInfo.HasAura(m_caster.GetMap().GetDifficultyID(), AuraType.ModIncreaseMountedFlightSpeed))
                                return SpellCastResult.OnlyAbovewater;

                            // Ignore map check if spell have AreaId. AreaId already checked and this prevent special mount spells
                            bool allowMount = !m_caster.GetMap().IsDungeon() || m_caster.GetMap().IsBattlegroundOrArena();
                            InstanceTemplate it = Global.ObjectMgr.GetInstanceTemplate(m_caster.GetMapId());
                            if (it != null)
                                allowMount = it.AllowMount;
                            if (m_caster.IsTypeId(TypeId.Player) && !allowMount && m_spellInfo.RequiredAreasID == 0)
                                return SpellCastResult.NoMountsAllowed;

                            if (m_caster.IsInDisallowedMountForm())
                                return SpellCastResult.NotShapeshift;

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
                                    if (area.Flags[0].HasAnyFlag(AreaFlags.NoFlyZone) || (Bf != null && !Bf.CanFlyIn()))
                                        return SpellCastResult.NotHere;
                            }
                            break;
                        }
                    case AuraType.PeriodicManaLeech:
                        {
                            if (effect.IsTargetingArea())
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
            if (m_caster.HasUnitState(UnitState.Casting) && !Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreCastInProgress))              //prevent spellcast interruption by another spellcast
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
                if (m_caster.GetCharmInfo() != null && m_caster.GetSpellHistory().HasGlobalCooldown(m_spellInfo))
                    return SpellCastResult.NotReady;

            return CheckCast(true);
        }

        SpellCastResult CheckCasterAuras(ref uint param1)
        {
            // spells totally immuned to caster auras (wsg flag drop, give marks etc)
            if (m_spellInfo.HasAttribute(SpellAttr6.IgnoreCasterAuras))
                return SpellCastResult.SpellCastOk;

            bool usableWhileStunned = m_spellInfo.HasAttribute(SpellAttr5.UsableWhileStunned);
            bool usableWhileFeared = m_spellInfo.HasAttribute(SpellAttr5.UsableWhileFeared);
            bool usableWhileConfused = m_spellInfo.HasAttribute(SpellAttr5.UsableWhileConfused);

            // Check whether the cast should be prevented by any state you might have.
            SpellCastResult result = SpellCastResult.SpellCastOk;
            // Get unit state
            UnitFlags unitflag = (UnitFlags)m_caster.GetUInt32Value(UnitFields.Flags);

            // this check should only be done when player does cast directly
            // (ie not when it's called from a script) Breaks for example PlayerAI when charmed
            /*if (!m_caster.GetCharmerGUID().IsEmpty())
            {
                Unit charmer = m_caster.GetCharmer();
                if (charmer)
                    if (charmer.GetUnitBeingMoved() != m_caster && !CheckSpellCancelsCharm(ref param1))
                        result = SpellCastResult.Charmed;
            }*/

            if (unitflag.HasAnyFlag(UnitFlags.Stunned))
            {
                // spell is usable while stunned, check if caster has allowed stun auras, another stun types must prevent cast spell
                if (usableWhileStunned)
                {
                    uint allowedStunMask = 1 << (int)Mechanics.Stun | 1 << (int)Mechanics.Sleep;

                    bool foundNotStun = false;
                    var stunAuras = m_caster.GetAuraEffectsByType(AuraType.ModStun);
                    foreach (AuraEffect stunEff in stunAuras)
                    {
                        uint stunMechanicMask = stunEff.GetSpellInfo().GetAllEffectsMechanicMask();
                        if (stunMechanicMask != 0 && !Convert.ToBoolean(stunMechanicMask & allowedStunMask))
                        {
                            foundNotStun = true;

                            // fill up aura mechanic info to send client proper error message
                            param1 = (uint)stunEff.GetSpellInfo().GetEffect(stunEff.GetEffIndex()).Mechanic;
                            if (param1 == 0)
                                param1 = (uint)stunEff.GetSpellInfo().Mechanic;

                            break;
                        }
                    }

                    if (foundNotStun)
                        result = SpellCastResult.Stunned;
                }
                // Not usable while stunned, however spell might provide some immunity that allows to cast it anyway
                else if (!CheckSpellCancelsStun(ref param1))
                    result = SpellCastResult.Stunned;
            }
            else if (unitflag.HasAnyFlag(UnitFlags.Silenced) && m_spellInfo.PreventionType.HasAnyFlag(SpellPreventionType.Silence) && !CheckSpellCancelsSilence(ref param1))
                result = SpellCastResult.Silenced;
            else if (unitflag.HasAnyFlag(UnitFlags.Pacified) && m_spellInfo.PreventionType.HasAnyFlag(SpellPreventionType.Pacify) && !CheckSpellCancelsPacify(ref param1))
                result = SpellCastResult.Pacified;
            else if (unitflag.HasAnyFlag(UnitFlags.Fleeing) && !usableWhileFeared && !CheckSpellCancelsFear(ref param1))
                result = SpellCastResult.Fleeing;
            else if (unitflag.HasAnyFlag(UnitFlags.Confused) && !usableWhileConfused && !CheckSpellCancelsConfuse(ref param1))
                result = SpellCastResult.Confused;
            else if (m_caster.HasFlag(UnitFields.Flags2, UnitFlags2.NoActions) && m_spellInfo.PreventionType.HasAnyFlag(SpellPreventionType.NoActions))
                result = SpellCastResult.NoActions;

            // Attr must make flag drop spell totally immune from all effects
            if (result != SpellCastResult.SpellCastOk)
                return (param1 != 0) ? SpellCastResult.PreventedByMechanic : result;
            return SpellCastResult.SpellCastOk;
        }

        bool CheckSpellCancelsAuraEffect(AuraType auraType, ref uint param1)
        {
            // Checking auras is needed now, because you are prevented by some state but the spell grants immunity.
            var auraEffects = m_caster.GetAuraEffectsByType(auraType);
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
                 CheckSpellCancelsAuraEffect(AuraType.Strangulate, ref param1);
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
            foreach (SpellEffectInfo effect in GetEffects())
            {
                if (effect == null)
                    continue;

                if (!effect.IsAura())
                    continue;

                AuraType auraType = effect.ApplyAuraName;
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
                            if (Math.Abs(effect.BasePoints) <= Math.Abs(eff.GetAmount()))
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
                    if (ihit.targetGUID == targetguid)
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
                        && !m_caster.IsWithinBoundaryRadius(target)))
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

        Tuple<float, float> GetMinMaxRange(bool strict)
        {
            float rangeMod = 0.0f;
            float minRange = 0.0f;
            float maxRange = 0.0f;

            if (strict && m_spellInfo.IsNextMeleeSwingSpell())
            {
                maxRange = 100.0f;
                return Tuple.Create(minRange, maxRange);
            }

            if (m_spellInfo.RangeEntry != null)
            {
                Unit target = m_targets.GetUnitTarget();
                if (m_spellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Melee))
                {
                    rangeMod = m_caster.GetMeleeRange(target ? target : m_caster); // when the target is not a unit, take the caster's combat reach as the target's combat reach.
                }
                else
                {
                    float meleeRange = 0.0f;
                    if (m_spellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Ranged))
                        meleeRange = m_caster.GetMeleeRange(target ? target : m_caster); // when the target is not a unit, take the caster's combat reach as the target's combat reach.

                    minRange = m_caster.GetSpellMinRangeForTarget(target, m_spellInfo) + meleeRange;
                    maxRange = m_caster.GetSpellMaxRangeForTarget(target, m_spellInfo);

                    if (target || m_targets.GetCorpseTarget())
                    {
                        rangeMod = m_caster.GetCombatReach() + (target ? target.GetCombatReach() : m_caster.GetCombatReach());

                        if (minRange > 0.0f && !m_spellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Ranged))
                            minRange += rangeMod;
                    }
                }

                if (target && m_caster.isMoving() && target.isMoving() && !m_caster.IsWalking() && !target.IsWalking() &&
                    (m_spellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Melee) || target.IsTypeId(TypeId.Player)))
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
                modOwner.ApplySpellMod(m_spellInfo.Id, SpellModOp.Range, ref maxRange, this);

            maxRange += rangeMod;

            return Tuple.Create(minRange, maxRange);
        }

        SpellCastResult CheckPower()
        {
            // item cast not used power
            if (m_CastItem != null)
                return SpellCastResult.SpellCastOk;

            foreach (SpellPowerCost cost in m_powerCost)
            {
                // health as power used - need check health amount
                if (cost.Power == PowerType.Health)
                {
                    if (m_caster.GetHealth() <= (uint)cost.Amount)
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
                if (m_caster.GetPower(cost.Power) < cost.Amount)
                    return SpellCastResult.NoPower;
            }

            return SpellCastResult.SpellCastOk;
        }

        SpellCastResult CheckItems(ref uint param1, ref uint param2)
        {
            Player player = m_caster.ToPlayer();
            if (!player)
                return SpellCastResult.SpellCastOk;

            if (m_spellInfo.HasAttribute(SpellAttr2.IgnoreItemCheck))
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

                for (int i = 0; i < proto.Effects.Count && i < 5; ++i)
                    if (proto.Effects[i].Charges > 0)
                        if (m_CastItem.GetSpellCharges(i) == 0)
                            return SpellCastResult.NoChargesRemain;

                // consumable cast item checks
                if (proto.GetClass() == ItemClass.Consumable && m_targets.GetUnitTarget() != null)
                {
                    // such items should only fail if there is no suitable effect at all - see Rejuvenation Potions for example
                    SpellCastResult failReason = SpellCastResult.SpellCastOk;
                    foreach (SpellEffectInfo effect in GetEffects())
                    {
                        // skip check, pet not required like checks, and for TARGET_UNIT_PET m_targets.GetUnitTarget() is not the real target but the caster
                        if (effect == null || effect.TargetA.GetTarget() == Targets.UnitPet)
                            continue;

                        if (effect.Effect == SpellEffectName.Heal)
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
                        if (effect.Effect == SpellEffectName.Energize)
                        {
                            if (effect.MiscValue < 0 || effect.MiscValue >= (int)PowerType.Max)
                            {
                                failReason = SpellCastResult.AlreadyAtFullPower;
                                continue;
                            }

                            PowerType power = (PowerType)effect.MiscValue;
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
                        if (targetItem.GetOwnerGUID() != m_caster.GetGUID())
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
                            for (int s = 0; s < proto.Effects.Count && s < 5; ++s)
                            {
                                // CastItem will be used up and does not count as reagent
                                int charges = m_CastItem.GetSpellCharges(s);
                                if (proto.Effects[s].Charges < 0 && Math.Abs(charges) < 2)
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
                    } else
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
            foreach (SpellEffectInfo effect in GetEffects())
            {
                if (effect == null)
                    continue;

                switch (effect.Effect)
                {
                    case SpellEffectName.CreateItem:
                    case SpellEffectName.CreateLoot:
                        {
                            if (!IsTriggered() && effect.ItemType != 0)
                            {
                                List<ItemPosCount> dest = new List<ItemPosCount>();
                                InventoryResult msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, effect.ItemType, 1);
                                if (msg != InventoryResult.Ok)
                                {
                                    ItemTemplate pProto = Global.ObjectMgr.GetItemTemplate(effect.ItemType);
                                    // @todo Needs review
                                    if (pProto != null && pProto.GetItemLimitCategory() == 0)
                                    {
                                        player.SendEquipError(msg, null, null, effect.ItemType);
                                        return SpellCastResult.DontReport;
                                    }
                                    else
                                    {
                                        SpellEffectInfo efi;
                                        if (!(m_spellInfo.SpellFamilyName == SpellFamilyNames.Mage && m_spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x40000000u)))
                                            return SpellCastResult.TooManyOfItem;
                                        else if (!player.HasItemCount(effect.ItemType))
                                            return SpellCastResult.TooManyOfItem;
                                        else if ((efi = GetEffect(1)) != null)
                                            player.CastSpell(m_caster, (uint)efi.CalcValue(), false);        // move this to anywhere
                                        return SpellCastResult.DontReport;
                                    }
                                }
                            }
                            break;
                        }
                    case SpellEffectName.EnchantItem:
                        if (effect.ItemType != 0 && m_targets.GetItemTarget() != null && m_targets.GetItemTarget().IsVellum())
                        {
                            // cannot enchant vellum for other player
                            if (m_targets.GetItemTarget().GetOwner() != m_caster)
                                return SpellCastResult.NotTradeable;
                            // do not allow to enchant vellum from scroll made by vellum-prevent exploit
                            if (m_CastItem != null && Convert.ToBoolean(m_CastItem.GetTemplate().GetFlags() & ItemFlags.NoReagentCost))
                                return SpellCastResult.TotemCategory;
                            List<ItemPosCount> dest = new List<ItemPosCount>();
                            InventoryResult msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, effect.ItemType, 1);
                            if (msg != InventoryResult.Ok)
                            {
                                player.SendEquipError(msg, null, null, effect.ItemType);
                                return SpellCastResult.DontReport;
                            }
                        }
                        goto case SpellEffectName.EnchantItemPrismatic;
                    case SpellEffectName.EnchantItemPrismatic:
                        {
                            Item targetItem = m_targets.GetItemTarget();
                            if (targetItem == null)
                                return SpellCastResult.ItemNotFound;

                            if (targetItem.GetTemplate().GetBaseItemLevel() < m_spellInfo.BaseLevel)
                                return SpellCastResult.Lowlevel;

                            bool isItemUsable = false;
                            ItemTemplate proto = targetItem.GetTemplate();
                            for (byte e = 0; e < proto.Effects.Count; ++e)
                            {
                                if (proto.Effects[e].SpellID != 0 && proto.Effects[e].TriggerType == ItemSpelltriggerType.OnUse)
                                {
                                    isItemUsable = true;
                                    break;
                                }
                            }

                            var enchantEntry = CliDB.SpellItemEnchantmentStorage.LookupByKey(effect.MiscValue);
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
                            if (targetItem.GetOwner() != m_caster)
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
                            if (item.GetOwner() != m_caster)
                            {
                                int enchant_id = effect.MiscValue;
                                var pEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);
                                if (pEnchant == null)
                                    return SpellCastResult.Error;
                                if (pEnchant.Flags.HasAnyFlag(EnchantmentSlotMask.CanSouldBound))
                                    return SpellCastResult.NotTradeable;
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
                            if (item.GetOwnerGUID() != m_caster.GetGUID())
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
                            if (item.GetOwnerGUID() != m_caster.GetGUID())
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
                            if (item.GetOwnerGUID() != m_caster.GetGUID())
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
                    case SpellEffectName.WeaponDamageNoschool:
                        {
                            if (!m_caster.IsTypeId(TypeId.Player))
                                return SpellCastResult.TargetNotPlayer;

                            if (m_attackType != WeaponAttackType.RangedAttack)
                                break;

                            Item pItem = m_caster.ToPlayer().GetWeaponForAttack(m_attackType);
                            if (pItem == null || pItem.IsBroken())
                                return SpellCastResult.EquippedItem;
                            break;
                        }
                    case SpellEffectName.RechargeItem:
                        {
                            uint itemId = effect.ItemType;

                            ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(itemId);
                            if (proto == null)
                                return SpellCastResult.ItemAtMaxCharges;

                            Item item = player.GetItemByEntry(itemId);
                            if (item != null)
                            {
                                for (int x = 0; x < proto.Effects.Count && x < 5; ++x)
                                    if (proto.Effects[x].Charges != 0 && item.GetSpellCharges(x) == proto.Effects[x].Charges)
                                        return SpellCastResult.ItemAtMaxCharges;
                            }
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
                    Item item = m_caster.ToPlayer().GetWeaponForAttack(attackType);

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
            if (m_caster == null)
                return;

            if (isDelayableNoMore())                                 // Spells may only be delayed twice
                return;

            //check pushback reduce
            int delaytime = 500;                                  // spellcasting delay is normally 500ms
            int delayReduce = 100;                                // must be initialized to 100 for percent modifiers
            m_caster.ToPlayer().ApplySpellMod(m_spellInfo.Id, SpellModOp.NotLoseCastingTime, ref delayReduce, this);
            delayReduce += m_caster.GetTotalAuraModifier(AuraType.ReducePushback) - 100;
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

            Log.outDebug(LogFilter.Spells, "Spell {0} partially interrupted for ({1}) ms at damage", m_spellInfo.Id, delaytime);

            SpellDelayed spellDelayed = new SpellDelayed();
            spellDelayed.Caster = m_caster.GetGUID();
            spellDelayed.ActualDelay = delaytime;

            m_caster.SendMessageToSet(spellDelayed, true);
        }

        public void DelayedChannel()
        {
            if (m_caster == null || !m_caster.IsTypeId(TypeId.Player) || getState() != SpellState.Casting)
                return;

            if (isDelayableNoMore())                                    // Spells may only be delayed twice
                return;

            //check pushback reduce
            int delaytime = MathFunctions.CalculatePct(m_spellInfo.GetDuration(), 25); // channeling delay is normally 25% of its time per hit
            int delayReduce = 100;                                    // must be initialized to 100 for percent modifiers
            m_caster.ToPlayer().ApplySpellMod(m_spellInfo.Id, SpellModOp.NotLoseCastingTime, ref delayReduce, this);
            delayReduce += m_caster.GetTotalAuraModifier(AuraType.ReducePushback) - 100;
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

            Log.outDebug(LogFilter.Spells, "Spell {0} partially interrupted for {1} ms, new duration: {2} ms", m_spellInfo.Id, delaytime, m_timer);

            foreach (var ihit in m_UniqueTargetInfo)
                if (ihit.missCondition == SpellMissInfo.None)
                {
                    Unit unit = (m_caster.GetGUID() == ihit.targetGUID) ? m_caster : Global.ObjAccessor.GetUnit(m_caster, ihit.targetGUID);
                    if (unit != null)
                        unit.DelayOwnedAuras(m_spellInfo.Id, m_originalCasterGUID, delaytime);
                }

            // partially interrupt persistent area auras
            DynamicObject dynObj = m_caster.GetDynObject(m_spellInfo.Id);
            if (dynObj != null)
                dynObj.Delay(delaytime);

            SendChannelUpdate((uint)m_timer);
        }

        bool UpdatePointers()
        {
            if (m_originalCasterGUID == m_caster.GetGUID())
                m_originalCaster = m_caster;
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
            foreach (SpellEffectInfo effect in GetEffects())
            {
                if (effect == null)
                    continue;

                SpellDestination dest = m_destTargets[effect.EffectIndex];
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

        bool CheckEffectTarget(Unit target, SpellEffectInfo effect, Position losPosition)
        {
            if (!effect.IsEffect())
                return false;

            switch (effect.ApplyAuraName)
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
                    int damage = CalculateDamage(effect.EffectIndex, target);
                    if (damage != 0)
                        if (target.GetLevelForTarget(m_caster) > damage)
                            return false;
                    break;
                default:
                    break;
            }

            // check for ignore LOS on the effect itself
            if (m_spellInfo.HasAttribute(SpellAttr2.CanTargetNotInLos) || Global.DisableMgr.IsDisabledFor(DisableType.Spell, m_spellInfo.Id, null, DisableFlags.SpellLOS))
                return true;

            // if spell is triggered, need to check for LOS disable on the aura triggering it and inherit that behaviour
            if (IsTriggered() && m_triggeredByAuraSpell != null && (m_triggeredByAuraSpell.HasAttribute(SpellAttr2.CanTargetNotInLos) || Global.DisableMgr.IsDisabledFor(DisableType.Spell, m_triggeredByAuraSpell.Id, null, DisableFlags.SpellLOS)))
                return true;

            // @todo shit below shouldn't be here, but it's temporary
            //Check targets for LOS visibility
            if (losPosition != null)
                return target.IsWithinLOS(losPosition.GetPositionX(), losPosition.GetPositionY(), losPosition.GetPositionZ(), ModelIgnoreFlags.M2);
            else
            {
                // Get GO cast coordinates if original caster . GO
                WorldObject caster = null;
                if (m_originalCasterGUID.IsGameObject())
                    caster = m_caster.GetMap().GetGameObject(m_originalCasterGUID);
                if (!caster)
                    caster = m_caster;
                if (target != m_caster && !target.IsWithinLOSInMap(caster, ModelIgnoreFlags.M2))
                    return false;
            }

            return true;
        }

        bool CheckEffectTarget(GameObject target, SpellEffectInfo effect)
        {
            if (!effect.IsEffect())
                return false;

            switch (effect.Effect)
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

        bool CheckEffectTarget(Item target, SpellEffectInfo effect)
        {
            if (!effect.IsEffect())
                return false;

            return true;
        }

        bool IsAutoActionResetSpell()
        {
            // @todo changed SPELL_INTERRUPT_FLAG_AUTOATTACK . SPELL_INTERRUPT_FLAG_INTERRUPT to fix compile - is this check correct at all?
            if (IsTriggered() || !m_spellInfo.InterruptFlags.HasAnyFlag(SpellInterruptFlags.Interrupt))
                return false;

            if (m_casttime == 0 && m_spellInfo.HasAttribute(SpellAttr6.NotResetSwingIfInstant))
                return false;

            return true;
        }

        bool IsNeedSendToClient()
        {
            return m_SpellVisual != 0 || m_spellInfo.IsChanneled() ||
                m_spellInfo.HasAttribute(SpellAttr8.AuraSendAmount) || m_spellInfo.Speed > 0.0f || (m_triggeredByAuraSpell == null && !IsTriggered());
        }

        bool HaveTargetsForEffect(byte effect)
        {
            foreach (var targetInfo in m_UniqueTargetInfo)
                if (Convert.ToBoolean(targetInfo.effectMask & (1 << effect)))
                    return true;

            foreach (var targetInfo in m_UniqueGOTargetInfo)
                if (Convert.ToBoolean(targetInfo.effectMask & (1 << effect)))
                    return true;

            foreach (var targetInfo in m_UniqueItemInfo)
                if (Convert.ToBoolean(targetInfo.effectMask & (1 << effect)))
                    return true;

            return false;
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
            foreach (SpellEffectInfo effect in GetEffects())
            {
                // don't do anything for empty effect
                if (effect == null || !effect.IsEffect())
                    continue;

                HandleEffects(null, null, null, effect.EffectIndex, SpellEffectHandleMode.Launch);
            }

            float[] multiplier = new float[SpellConst.MaxEffects];
            foreach (SpellEffectInfo effect in GetEffects())
                if (effect != null && Convert.ToBoolean(m_applyMultiplierMask & (1 << (int)effect.EffectIndex)))
                    multiplier[effect.EffectIndex] = effect.CalcDamageMultiplier(m_originalCaster, this);

            PrepareTargetProcessing();

            foreach (var ihit in m_UniqueTargetInfo)
            {
                TargetInfo target = ihit;

                uint mask = target.effectMask;
                if (mask == 0)
                    continue;

                DoAllEffectOnLaunchTarget(target, multiplier);
            }

            FinishTargetProcessing();
        }

        void DoAllEffectOnLaunchTarget(TargetInfo targetInfo, float[] multiplier)
        {
            Unit unit = null;
            // In case spell hit target, do all effect on that target
            if (targetInfo.missCondition == SpellMissInfo.None)
                unit = m_caster.GetGUID() == targetInfo.targetGUID ? m_caster : Global.ObjAccessor.GetUnit(m_caster, targetInfo.targetGUID);
            // In case spell reflect from target, do all effect on caster (if hit)
            else if (targetInfo.missCondition == SpellMissInfo.Reflect && targetInfo.reflectResult == SpellMissInfo.None)
                unit = m_caster;
            if (unit == null)
                return;

            foreach (SpellEffectInfo effect in GetEffects())
            {
                if (effect != null && Convert.ToBoolean(targetInfo.effectMask & (1 << (int)effect.EffectIndex)))
                {
                    m_damage = 0;
                    m_healing = 0;

                    HandleEffects(unit, null, null, effect.EffectIndex, SpellEffectHandleMode.LaunchTarget);

                    if (m_damage > 0)
                    {
                        if (effect.IsTargetingArea() || effect.IsAreaAuraEffect() || effect.IsEffect(SpellEffectName.PersistentAreaAura))
                        {
                            m_damage = (int)(m_damage * unit.GetTotalAuraMultiplierByMiscMask(AuraType.ModAoeDamageAvoidance, (uint)m_spellInfo.SchoolMask));
                            if (!m_caster.IsTypeId(TypeId.Player))
                                m_damage = (int)(m_damage * unit.GetTotalAuraMultiplierByMiscMask(AuraType.ModCreatureAoeDamageAvoidance, (uint)m_spellInfo.SchoolMask));

                            if (m_caster.IsTypeId(TypeId.Player))
                            {
                                int targetAmount = m_UniqueTargetInfo.Count;
                                if (targetAmount > 20)
                                    m_damage = m_damage * 20 / targetAmount;
                            }
                        }
                    }

                    if (Convert.ToBoolean(m_applyMultiplierMask & (1 << (int)effect.EffectIndex)))
                    {
                        m_damage = (int)(m_damage * m_damageMultipliers[effect.EffectIndex]);
                        m_damageMultipliers[effect.EffectIndex] *= multiplier[effect.EffectIndex];
                    }
                    targetInfo.damage += m_damage;
                }
            }

            Player modOwner = m_caster.GetSpellModOwner();
            if (modOwner)
                modOwner.SetSpellModTakingSpell(this, true);

            targetInfo.crit = m_caster.IsSpellCrit(unit, m_spellInfo, m_spellSchoolMask, m_attackType);

            modOwner = m_caster.GetSpellModOwner();
            if (modOwner)
                modOwner.SetSpellModTakingSpell(this, false);
        }

        SpellCastResult CanOpenLock(uint effIndex, uint lockId, ref SkillType skillId, ref int reqSkillValue, ref int skillValue)
        {
            if (lockId == 0)                                             // possible case for GO and maybe for items.
                return SpellCastResult.SpellCastOk;

            // Get LockInfo
            var lockInfo = CliDB.LockStorage.LookupByKey(lockId);

            if (lockInfo == null)
                return SpellCastResult.BadTargets;

            SpellEffectInfo effect = GetEffect(effIndex);
            if (effect == null)
                return SpellCastResult.BadTargets; // no idea about correct error

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
                                if (!m_CastItem && m_caster.IsTypeId(TypeId.Player))
                                    skillValue = m_caster.ToPlayer().GetSkillValue(skillId);
                                else if (lockInfo.Index[j] == (uint)LockType.Lockpicking)
                                    skillValue = (int)m_caster.getLevel() * 5;

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
                    m_spellValue.AuraStackAmount = (byte)value;
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

        void PrepareScriptHitHandlers()
        {
            foreach (var script in m_loadedScripts)
                script._InitHit();
        }

        bool CallScriptEffectHandlers(uint effIndex, SpellEffectHandleMode mode)
        {
            // execute script effect handler hooks and check if effects was prevented
            bool preventDefault = false;

            foreach (var script in m_loadedScripts)
            {
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

        void CallScriptBeforeHitHandlers(SpellMissInfo missInfo)
        {
            foreach (var script in m_loadedScripts)
            {
                script._PrepareScriptCall(SpellScriptHookType.BeforeHit);

                foreach (var hook in script.BeforeHit)
                    hook.Call(missInfo);

                script._FinishScriptCall();
            }
        }

        void CallScriptOnHitHandlers()
        {
            foreach (var script in m_loadedScripts)
            {
                script._PrepareScriptCall(SpellScriptHookType.OnHit);

                foreach (var hook in script.OnHit)
                    hook.Call();

                script._FinishScriptCall();
            }
        }

        void CallScriptAfterHitHandlers()
        {
            foreach (var script in m_loadedScripts)
            {
                script._PrepareScriptCall(SpellScriptHookType.AfterHit);

                foreach (var hook in script.AfterHit)
                    hook.Call();

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

        bool CanExecuteTriggersOnHit(uint effMask, SpellInfo triggeredByAura = null)
        {
            bool only_on_caster = (triggeredByAura != null && triggeredByAura.HasAttribute(SpellAttr4.ProcOnlyOnCaster));
            // If triggeredByAura has SPELL_ATTR4_PROC_ONLY_ON_CASTER then it can only proc on a casted spell with TARGET_UNIT_CASTER
            foreach (SpellEffectInfo effect in GetEffects())
            {
                if (effect != null && (Convert.ToBoolean(effMask & (1 << (int)effect.EffectIndex)) && (!only_on_caster || (effect.TargetA.GetTarget() == Targets.UnitCaster))))
                    return true;
            }
            return false;
        }

        void PrepareTriggersExecutedOnHit()
        {
            // handle SPELL_AURA_ADD_TARGET_TRIGGER auras:
            // save auras which were present on spell caster on cast, to prevent triggered auras from affecting caster
            // and to correctly calculate proc chance when combopoints are present
            var targetTriggers = m_caster.GetAuraEffectsByType(AuraType.AddTargetTrigger);
            foreach (var aurEff in targetTriggers)
            {
                if (!aurEff.IsAffectingSpell(m_spellInfo))
                    continue;

                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(aurEff.GetSpellEffectInfo().TriggerSpell);
                if (spellInfo != null)
                {
                    // calculate the chance using spell base amount, because aura amount is not updated on combo-points change
                    // this possibly needs fixing
                    int auraBaseAmount = aurEff.GetBaseAmount();
                    // proc chance is stored in effect amount
                    int chance = m_caster.CalculateSpellDamage(null, aurEff.GetSpellInfo(), aurEff.GetEffIndex(), auraBaseAmount);
                    chance *= aurEff.GetBase().GetStackAmount();

                    // build trigger and add to the list
                    m_hitTriggerSpells.Add(new HitTriggerSpell(spellInfo, aurEff.GetSpellInfo(), chance));
                }
            }
        }

        bool HasGlobalCooldown()
        {
            // Only players or controlled units have global cooldown
            if (!m_caster.IsTypeId(TypeId.Player) && m_caster.GetCharmInfo() == null)
                return false;

            return m_caster.GetSpellHistory().HasGlobalCooldown(m_spellInfo);
        }

        void TriggerGlobalCooldown()
        {
            int gcd = (int)m_spellInfo.StartRecoveryTime;
            if (gcd == 0 || m_spellInfo.StartRecoveryCategory == 0)
                return;

            // Only players or controlled units have global cooldown
            if (!m_caster.IsTypeId(TypeId.Player) && m_caster.GetCharmInfo() == null)
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
                    modOwner.ApplySpellMod(m_spellInfo.Id, SpellModOp.GlobalCooldown, ref gcd, this);

                bool isMeleeOrRangedSpell = m_spellInfo.DmgClass == SpellDmgClass.Melee || m_spellInfo.DmgClass == SpellDmgClass.Ranged ||
                    m_spellInfo.HasAttribute(SpellAttr0.ReqAmmo) || m_spellInfo.HasAttribute(SpellAttr0.Ability);

                // Apply haste rating
                if (gcd > 750 && ((m_spellInfo.StartRecoveryCategory == 133 && !isMeleeOrRangedSpell) || m_caster.HasAuraTypeWithAffectMask(AuraType.ModGlobalCooldownByHaste, m_spellInfo)))
                {
                    gcd = (int)(gcd * m_caster.GetFloatValue(UnitFields.ModCastHaste));
                    MathFunctions.RoundToInterval(ref gcd, 750, 1500);
                }

                if (gcd > 750 && m_caster.HasAuraTypeWithAffectMask(AuraType.ModGlobalCooldownByHasteRegen, m_spellInfo))
                {
                    gcd = (int)(gcd * m_caster.GetFloatValue(UnitFields.ModHasteRegen));
                    MathFunctions.RoundToInterval(ref gcd, 750, 1500);
                }
            }

            m_caster.GetSpellHistory().AddGlobalCooldown(m_spellInfo, (uint)gcd);
        }

        void CancelGlobalCooldown()
        {
            if (m_spellInfo.StartRecoveryTime == 0)
                return;

            // Cancel global cooldown when interrupting current cast
            if (m_caster.GetCurrentSpell(CurrentSpellTypes.Generic) != this)
                return;

            // Only players or controlled units have global cooldown
            if (!m_caster.IsTypeId(TypeId.Player) && m_caster.GetCharmInfo() == null)
                return;

            m_caster.GetSpellHistory().CancelGlobalCooldown(m_spellInfo);
        }

        List<SpellScript> m_loadedScripts = new List<SpellScript>();

        int CalculateDamage(uint i, Unit target)
        {
            int? basePoint = null;
            if ((m_spellValue.CustomBasePointsMask & (1 << (int)i)) != 0)
                basePoint = m_spellValue.EffectBasePoints[i];

            return m_caster.CalculateSpellDamage(target, m_spellInfo, i, basePoint, m_castItemLevel);
        }
        int CalculateDamage(uint i, Unit target, out float variance)
        {
            int? basePoint = null;
            if ((m_spellValue.CustomBasePointsMask & (1 << (int)i)) != 0)
                basePoint = m_spellValue.EffectBasePoints[i];

            return m_caster.CalculateSpellDamage(target, m_spellInfo, i, out variance, basePoint, m_castItemLevel);
        }
        public SpellState getState()
        {
            return m_spellState;
        }
        public void setState(SpellState state)
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
        public bool IsProcDisabled() { return _triggeredCastFlags.HasAnyFlag(TriggerCastFlags.DisallowProcEvents); }
        public bool IsChannelActive() { return m_caster.GetChannelSpellId() != 0; }

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

        public Unit GetCaster()
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

        bool isDelayableNoMore()
        {
            if (m_delayAtDamageCount >= 2)
                return true;

            m_delayAtDamageCount++;
            return false;
        }

        bool DontReport()
        {
            return Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.DontReportCastError);
        }

        public SpellInfo GetTriggeredByAuraSpell() { return m_triggeredByAuraSpell; }

        SpellEffectInfo[] GetEffects() { return _effects; }
        public SpellEffectInfo GetEffect(uint index)
        {
            if (index >= _effects.Length)
                return null;

            return _effects[index];
        }

        bool HasEffect(SpellEffectName effect)
        {
            foreach (SpellEffectInfo eff in GetEffects())
            {
                if (eff != null && eff.IsEffect(effect))
                    return true;
            }
            return false;
        }

        public static implicit operator bool(Spell spell)
        {
            return spell != null;
        }

        #region Fields
        MultiMap<uint, SpellLogEffectPowerDrainParams> _powerDrainTargets = new MultiMap<uint, SpellLogEffectPowerDrainParams>();
        MultiMap<uint, SpellLogEffectExtraAttacksParams> _extraAttacksTargets = new MultiMap<uint, SpellLogEffectExtraAttacksParams>();
        MultiMap<uint, SpellLogEffectDurabilityDamageParams> _durabilityDamageTargets = new MultiMap<uint, SpellLogEffectDurabilityDamageParams>();
        MultiMap<uint, SpellLogEffectGenericVictimParams> _genericVictimTargets = new MultiMap<uint, SpellLogEffectGenericVictimParams>();
        MultiMap<uint, SpellLogEffectTradeSkillItemParams> _tradeSkillTargets = new MultiMap<uint, SpellLogEffectTradeSkillItemParams>();
        MultiMap<uint, SpellLogEffectFeedPetParams> _feedPetTargets = new MultiMap<uint, SpellLogEffectFeedPetParams>();
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
        public uint m_SpellVisual;
        public SpellCastTargets m_targets;
        public sbyte m_comboPointGain;
        public SpellCustomErrors m_customError;

        public List<Aura> m_appliedMods;

        Unit m_caster;
        public SpellValue m_spellValue;
        ObjectGuid m_originalCasterGUID;
        Unit m_originalCaster;
        public Spell m_selfContainer;

        //Spell data
        SpellSchoolMask m_spellSchoolMask;                  // Spell school (can be overwrite for some spells (wand shoot for example)
        WeaponAttackType m_attackType;                      // For weapon based attack

        List<SpellPowerCost> m_powerCost = new List<SpellPowerCost>();
        int m_casttime;                                   // Calculated spell cast time initialized only in Spell.prepare
        bool m_canReflect;                                  // can reflect this spell?
        bool m_autoRepeat;
        byte m_runesState;
        byte m_delayAtDamageCount;

        // Delayed spells system
        ulong m_delayStart;                                // time of spell delay start, filled by event handler, zero = just started
        ulong m_delayMoment;                               // moment of next delay call, used internally
        bool m_immediateHandled;                            // were immediate actions handled? (used by delayed spells only)

        // These vars are used in both delayed spell system and modified immediate spell system
        bool m_referencedFromCurrentSpell;
        bool m_executedCurrently;
        bool m_needComboPoints;
        uint m_applyMultiplierMask;
        float[] m_damageMultipliers = new float[SpellConst.MaxEffects];

        // Current targets, to be used in SpellEffects (MUST BE USED ONLY IN SPELL EFFECTS)
        public Unit unitTarget;
        public Item itemTarget;
        public GameObject gameObjTarget;
        public WorldLocation destTarget;
        public int damage;
        SpellMissInfo targetMissInfo;
        float _variance;
        SpellEffectHandleMode effectHandleMode;
        public SpellEffectInfo effectInfo;
        // used in effects handlers
        public Aura m_spellAura;

        // -------------------------------------------
        GameObject focusObject;

        // Damage and healing in effects need just calculate
        public int m_damage;           // Damge   in effects count here
        public int m_healing;          // Healing in effects count here

        // ******************************************
        // Spell trigger system
        // ******************************************
        ProcFlags m_procAttacker;                // Attacker trigger flags
        ProcFlags m_procVictim;                  // Victim   trigger flags
        ProcFlagsHit m_hitMask;

        // *****************************************
        // Spell target subsystem
        // *****************************************
        // Targets store structures and data
        List<TargetInfo> m_UniqueTargetInfo = new List<TargetInfo>();
        uint m_channelTargetEffectMask;                        // Mask req. alive targets

        List<GOTargetInfo> m_UniqueGOTargetInfo = new List<GOTargetInfo>();

        List<ItemTargetInfo> m_UniqueItemInfo = new List<ItemTargetInfo>();

        SpellDestination[] m_destTargets = new SpellDestination[SpellConst.MaxEffects];

        List<HitTriggerSpell> m_hitTriggerSpells = new List<HitTriggerSpell>();

        SpellState m_spellState;
        int m_timer;

        TriggerCastFlags _triggeredCastFlags;

        // if need this can be replaced by Aura copy
        // we can't store original aura link to prevent access to deleted auras
        // and in same time need aura data and after aura deleting.
        public SpellInfo m_triggeredByAuraSpell;

        SpellEffectInfo[] _effects = new SpellEffectInfo[SpellConst.MaxEffects];

        bool m_skipCheck;
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

    public class TargetInfo
    {
        public ObjectGuid targetGUID;
        public ulong timeDelay;
        public int damage;

        public SpellMissInfo missCondition;
        public SpellMissInfo reflectResult;

        public uint effectMask;
        public bool processed;
        public bool alive;
        public bool crit;
    }

    public class GOTargetInfo
    {
        public ObjectGuid targetGUID;
        public ulong timeDelay;
        public uint effectMask;
        public bool processed;
    }

    public class ItemTargetInfo
    {
        public Item item;
        public uint effectMask;
    }

    public class SpellValue
    {
        public SpellValue(Difficulty difficulty, SpellInfo proto, Unit caster)
        {
            var effects = proto.GetEffectsForDifficulty(difficulty);
            Cypher.Assert(effects.Length <= SpellConst.MaxEffects);
            foreach (SpellEffectInfo effect in effects)
                if (effect != null)
                    EffectBasePoints[effect.EffectIndex] = effect.CalcBaseValue(caster, null, -1);

            CustomBasePointsMask = 0;
            MaxAffectedTargets = proto.MaxAffectedTargets;
            RadiusMod = 1.0f;
            AuraStackAmount = 1;
        }

        public int[] EffectBasePoints = new int[SpellConst.MaxEffects];
        public uint CustomBasePointsMask;
        public uint MaxAffectedTargets;
        public float RadiusMod;
        public byte AuraStackAmount;
    }

    // Spell modifier (used for modify other spells)
    public class SpellModifier
    {
        public SpellModifier(Aura _ownerAura)
        {
            op = SpellModOp.Damage;
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
        public WorldObjectSpellTargetCheck(Unit caster, Unit referer, SpellInfo spellInfo,
            SpellTargetCheckTypes selectionType, List<Condition> condList)
        {
            _caster = caster;
            _referer = referer;
            _spellInfo = spellInfo;
            _targetSelectionType = selectionType;
            _condList = condList;

            if (condList != null)
                _condSrcInfo = new ConditionSourceInfo(null, caster);
            else
                _condSrcInfo = null;
        }

        public virtual bool Invoke(WorldObject obj)
        {
            if (_spellInfo.CheckTarget(_caster, obj, true) != SpellCastResult.SpellCastOk)
                return false;
            Unit unitTarget = obj.ToUnit();
            Corpse corpseTarget = obj.ToCorpse();
            if (corpseTarget != null)
            {
                // use ofter for party/assistance checks
                Player owner = Global.ObjAccessor.FindPlayer(corpseTarget.GetOwnerGUID());
                if (owner != null)
                    unitTarget = owner;
                else
                    return false;
            }
            if (unitTarget != null)
            {
                switch (_targetSelectionType)
                {
                    case SpellTargetCheckTypes.Enemy:
                        if (unitTarget.IsTotem())
                            return false;
                        if (!_caster._IsValidAttackTarget(unitTarget, _spellInfo))
                            return false;
                        break;
                    case SpellTargetCheckTypes.Ally:
                        if (unitTarget.IsTotem())
                            return false;
                        if (!_caster._IsValidAssistTarget(unitTarget, _spellInfo))
                            return false;
                        break;
                    case SpellTargetCheckTypes.Party:
                        if (unitTarget.IsTotem())
                            return false;
                        if (!_caster._IsValidAssistTarget(unitTarget, _spellInfo))
                            return false;
                        if (!_referer.IsInPartyWith(unitTarget))
                            return false;
                        break;
                    case SpellTargetCheckTypes.RaidClass:
                    case SpellTargetCheckTypes.Raid:
                        if (_referer.GetClass() != unitTarget.GetClass())
                            return false;
                        if (unitTarget.IsTotem())
                            return false;
                        if (!_caster._IsValidAssistTarget(unitTarget, _spellInfo))
                            return false;
                        if (!_referer.IsInRaidWith(unitTarget))
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

        public Unit _caster { get; set; }
        Unit _referer;
        public SpellInfo _spellInfo { get; set; }
        SpellTargetCheckTypes _targetSelectionType;
        ConditionSourceInfo _condSrcInfo;
        List<Condition> _condList;
    }

    public class WorldObjectSpellNearbyTargetCheck : WorldObjectSpellTargetCheck
    {
        float _range;
        Position _position;
        public WorldObjectSpellNearbyTargetCheck(float range, Unit caster, SpellInfo spellInfo,
            SpellTargetCheckTypes selectionType, List<Condition> condList)
            : base(caster, caster, spellInfo, selectionType, condList)
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
        public WorldObjectSpellAreaTargetCheck(float range, Position position, Unit caster,
            Unit referer, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList)
            : base(caster, referer, spellInfo, selectionType, condList)
        {
            _range = range;
            _position = position;

        }

        public override bool Invoke(WorldObject target)
        {
            if (!target.IsWithinDist3d(_position, _range) && !(target.IsTypeId(TypeId.GameObject) && target.ToGameObject().IsInRange(_position.posX, _position.posY, _position.posZ, _range)))
                return false;
            return base.Invoke(target);
        }
    }

    public class WorldObjectSpellConeTargetCheck : WorldObjectSpellAreaTargetCheck
    {
        public WorldObjectSpellConeTargetCheck(float coneAngle, float range, Unit caster, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList)
            : base(range, caster.GetPosition(), caster, caster, spellInfo, selectionType, condList)
        {
            _coneAngle = coneAngle;
        }

        public override bool Invoke(WorldObject target)
        {
            if (_spellInfo.HasAttribute(SpellCustomAttributes.ConeBack))
            {
                if (!_caster.isInBack(target, _coneAngle))
                    return false;
            }
            else if (_spellInfo.HasAttribute(SpellCustomAttributes.ConeLine))
            {
                if (!_caster.HasInLine(target, _caster.GetObjectSize() + target.GetObjectSize()))
                    return false;
            }
            else
            {
                if (!_caster.IsWithinBoundaryRadius(target.ToUnit()))
                    // ConeAngle > 0 -> select targets in front
                    // ConeAngle < 0 -> select targets in back
                    if (_caster.HasInArc(_coneAngle, target) != MathFunctions.fuzzyGe(_coneAngle, 0.0f))
                        return false;
            }
            return base.Invoke(target);
        }

        float _coneAngle;
    }

    public class WorldObjectSpellTrajTargetCheck : WorldObjectSpellAreaTargetCheck
    {
        public WorldObjectSpellTrajTargetCheck(float range, Position position, Unit caster, SpellInfo spellInfo)
            : base(range, position, caster, caster, spellInfo, SpellTargetCheckTypes.Default, null) { }

        public override bool Invoke(WorldObject target)
        {
            // return all targets on missile trajectory (0 - size of a missile)
            if (!_caster.HasInLine(target, target.GetObjectSize()))
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
            if (m_Spell.getState() != SpellState.Finished)
                m_Spell.update(p_time);

            // check spell state to process
            switch (m_Spell.getState())
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
                            ulong n_offset = m_Spell.handle_delayed(t_offset);
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
                            if (!m_Spell.m_targets.HasDst())
                            {
                                ulong n_offset = m_Spell.handle_delayed(0);
                                Cypher.Assert(n_offset == m_Spell.GetDelayMoment());
                            }
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
            if (m_Spell.getState() != SpellState.Finished)
                m_Spell.cancel();
        }
        public override bool IsDeletable()
        {
            return m_Spell.IsDeletable();
        }

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

            caster.ProcSkillsAndAuras(_victim, typeMaskActor, typeMaskActionTarget, spellTypeMask, spellPhaseMask, hitMask, null, null, null);
            return true;
        }

        Unit _victim;
        ObjectGuid _casterGuid;
    }
}
