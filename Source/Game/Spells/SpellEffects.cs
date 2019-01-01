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
using Framework.GameMath;
using Game.BattleGrounds;
using Game.BattlePets;
using Game.Combat;
using Game.DataStorage;
using Game.Entities;
using Game.Garrisons;
using Game.Groups;
using Game.Guilds;
using Game.Loots;
using Game.Maps;
using Game.Movement;
using Game.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Spells
{
    public partial class Spell
    {
        [SpellEffectHandler(SpellEffectName.Null)]
        [SpellEffectHandler(SpellEffectName.Portal)]
        [SpellEffectHandler(SpellEffectName.BindSight)]
        [SpellEffectHandler(SpellEffectName.CallPet)]
        [SpellEffectHandler(SpellEffectName.Effect171)]
        [SpellEffectHandler(SpellEffectName.Effect177)]
        [SpellEffectHandler(SpellEffectName.PortalTeleport)]
        [SpellEffectHandler(SpellEffectName.RitualBase)]
        [SpellEffectHandler(SpellEffectName.RitualActivatePortal)]
        [SpellEffectHandler(SpellEffectName.Dodge)]
        [SpellEffectHandler(SpellEffectName.Evade)]
        [SpellEffectHandler(SpellEffectName.Weapon)]
        [SpellEffectHandler(SpellEffectName.Defense)]
        [SpellEffectHandler(SpellEffectName.SpellDefense)]
        [SpellEffectHandler(SpellEffectName.Language)]
        [SpellEffectHandler(SpellEffectName.Spawn)]
        [SpellEffectHandler(SpellEffectName.Stealth)]
        [SpellEffectHandler(SpellEffectName.Detect)]
        [SpellEffectHandler(SpellEffectName.ForceCriticalHit)]
        [SpellEffectHandler(SpellEffectName.Attack)]
        [SpellEffectHandler(SpellEffectName.ThreatAll)]
        [SpellEffectHandler(SpellEffectName.Effect112)]
        [SpellEffectHandler(SpellEffectName.TeleportGraveyard)]
        [SpellEffectHandler(SpellEffectName.Effect122)]
        [SpellEffectHandler(SpellEffectName.Effect175)]
        [SpellEffectHandler(SpellEffectName.Effect178)]
        void EffectUnused(uint effIndex) { }

        void EffectResurrectNew(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || unitTarget.IsAlive())
                return;

            if (!unitTarget.IsTypeId(TypeId.Player))
                return;

            if (!unitTarget.IsInWorld)
                return;

            Player target = unitTarget.ToPlayer();

            if (target.IsResurrectRequested())       // already have one active request
                return;

            int health = damage;
            int mana = effectInfo.MiscValue;
            ExecuteLogEffectResurrect(effIndex, target);
            target.SetResurrectRequestData(m_caster, (uint)health, (uint)mana, 0);
            SendResurrectRequest(target);
        }

        [SpellEffectHandler(SpellEffectName.Instakill)]
        void EffectInstaKill(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsAlive())
                return;

            if (unitTarget.IsTypeId(TypeId.Player))
                if (unitTarget.ToPlayer().GetCommandStatus(PlayerCommandStates.God))
                    return;

            if (m_caster == unitTarget)                              // prevent interrupt message
                finish();

            SpellInstakillLog data = new SpellInstakillLog();
            data.Target = unitTarget.GetGUID();
            data.Caster = m_caster.GetGUID();
            data.SpellID = m_spellInfo.Id;
            m_caster.SendMessageToSet(data, true);

            m_caster.DealDamage(unitTarget, (uint)unitTarget.GetHealth(), null, DamageEffectType.NoDamage, SpellSchoolMask.Normal, null, false);
        }

        void EffectEnvironmentalDMG(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsAlive())
                return;

            // CalcAbsorbResist already in Player::EnvironmentalDamage
            if (unitTarget.IsTypeId(TypeId.Player))
                unitTarget.ToPlayer().EnvironmentalDamage(EnviromentalDamage.Fire, (uint)damage);
            else
            {
                DamageInfo damageInfo = new DamageInfo(m_caster, unitTarget, (uint)damage, m_spellInfo, m_spellInfo.GetSchoolMask(), DamageEffectType.SpellDirect, WeaponAttackType.BaseAttack);
                m_caster.CalcAbsorbResist(damageInfo);

                SpellNonMeleeDamage log = new SpellNonMeleeDamage(m_caster, unitTarget, m_spellInfo.Id, m_SpellVisual, m_spellInfo.GetSchoolMask(), m_castId);
                log.damage = damageInfo.GetDamage();
                log.originalDamage = (uint)damage;
                log.absorb = damageInfo.GetAbsorb();
                log.resist = damageInfo.GetResist();

                m_caster.SendSpellNonMeleeDamageLog(log);
            }
        }

        [SpellEffectHandler(SpellEffectName.SchoolDamage)]
        void EffectSchoolDmg(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.LaunchTarget)
                return;

            if (unitTarget != null && unitTarget.IsAlive())
            {
                bool apply_direct_bonus = true;

                // Meteor like spells (divided damage to targets)
                if (m_spellInfo.HasAttribute(SpellCustomAttributes.ShareDamage))
                {
                    int count = m_UniqueTargetInfo.Count(targetInfo => Convert.ToBoolean(targetInfo.effectMask & (1 << (int)effIndex)));

                    // divide to all targets
                    if (count != 0)
                        damage /= count;
                }

                switch (m_spellInfo.SpellFamilyName)
                {
                    case SpellFamilyNames.Generic:
                        {
                            switch (m_spellInfo.Id)                     // better way to check unknown
                            {
                                // Consumption
                                case 28865:
                                    damage = m_caster.GetMap().GetDifficultyID() == Difficulty.None ? 2750 : 4250;
                                    break;
                                // percent from health with min
                                case 25599:                             // Thundercrash
                                    {
                                        damage = (int)unitTarget.GetHealth() / 2;
                                        if (damage < 200)
                                            damage = 200;
                                        break;
                                    }
                                // arcane charge. must only affect demons (also undead?)
                                case 45072:
                                    {
                                        if (unitTarget.GetCreatureType() != CreatureType.Demon
                                            && unitTarget.GetCreatureType() != CreatureType.Undead)
                                            return;
                                        break;
                                    }
                                // Gargoyle Strike
                                case 51963:
                                    {
                                        // about +4 base spell dmg per level
                                        damage = (int)(m_caster.getLevel() - 60) * 4 + 60;
                                        break;
                                    }
                            }
                            break;
                        }
                    case SpellFamilyNames.Warrior:
                        {
                            // Victory Rush
                            if (m_spellInfo.Id == 34428)
                                MathFunctions.ApplyPct(ref damage, m_caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack));
                            // Shockwave
                            else if (m_spellInfo.Id == 46968)
                            {
                                int pct = m_caster.CalculateSpellDamage(unitTarget, m_spellInfo, 2);
                                if (pct > 0)
                                    damage += (int)MathFunctions.CalculatePct(m_caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack), pct);
                                break;
                            }
                            break;
                        }
                    case SpellFamilyNames.Warlock:
                        {
                            break;
                        }
                    case SpellFamilyNames.Priest:
                        {
                            break;
                        }
                    case SpellFamilyNames.Druid:
                        {
                            // Ferocious Bite
                            if (m_caster.IsTypeId(TypeId.Player) && m_spellInfo.SpellFamilyFlags[3].HasAnyFlag(0x1000u))
                            {
                                // converts each extra point of energy ( up to 25 energy ) into additional damage
                                int energy = -(m_caster.ModifyPower(PowerType.Energy, -25));
                                // 25 energy = 100% more damage
                                MathFunctions.AddPct(ref damage, energy * 4);
                            }
                            break;
                        }
                    case SpellFamilyNames.Deathknight:
                        {
                            // Blood Boil - bonus for diseased targets
                            if (m_spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00040000u))
                            {
                                if (unitTarget.GetAuraEffect(AuraType.PeriodicDamage, SpellFamilyNames.Deathknight, new FlagArray128(0, 0, 0x00000002), m_caster.GetGUID()) != null)
                                {
                                    damage += m_damage / 2;
                                    damage += (int)(m_caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 0.035f);
                                }
                            }
                            break;
                        }
                }

                if (m_originalCaster != null && apply_direct_bonus)
                {
                    uint bonus = m_originalCaster.SpellDamageBonusDone(unitTarget, m_spellInfo, (uint)damage, DamageEffectType.SpellDirect, effectInfo);
                    damage = (int)(bonus + (bonus * _variance));
                    damage = (int)unitTarget.SpellDamageBonusTaken(m_originalCaster, m_spellInfo, (uint)damage, DamageEffectType.SpellDirect, effectInfo);
                }

                m_damage += damage;
            }
        }

        [SpellEffectHandler(SpellEffectName.Dummy)]
        void EffectDummy(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!unitTarget && !gameObjTarget && !itemTarget)
                return;

            // selection by spell family
            switch (m_spellInfo.SpellFamilyName)
            {
                case SpellFamilyNames.Paladin:
                    switch (m_spellInfo.Id)
                    {
                        case 31789:                                 // Righteous Defense (step 1)
                            {
                                // Clear targets for eff 1
                                foreach (var hit in m_UniqueTargetInfo)
                                    hit.effectMask &= ~Convert.ToUInt32(1 << 1);

                                // not empty (checked), copy
                                var attackers = unitTarget.getAttackers();

                                // remove invalid attackers
                                foreach (var att in attackers)
                                    if (!att.IsValidAttackTarget(m_caster))
                                        attackers.Remove(att);

                                // selected from list 3
                                int maxTargets = Math.Min(3, attackers.Count);
                                for (uint i = 0; i < maxTargets; ++i)
                                {
                                    Unit attacker = attackers.SelectRandom();
                                    AddUnitTarget(attacker, 1 << 1);
                                    attackers.Remove(attacker);
                                }

                                // now let next effect cast spell at each target.
                                return;
                            }
                    }
                    break;
            }

            // pet auras
            PetAura petSpell = Global.SpellMgr.GetPetAura(m_spellInfo.Id, (byte)effIndex);
            if (petSpell != null)
            {
                m_caster.AddPetAura(petSpell);
                return;
            }

            // normal DB scripted effect
            Log.outDebug(LogFilter.Spells, "Spell ScriptStart spellid {0} in EffectDummy({1})", m_spellInfo.Id, effIndex);
            m_caster.GetMap().ScriptsStart(ScriptsType.Spell, (uint)((int)m_spellInfo.Id | (int)(effIndex << 24)), m_caster, unitTarget);

            // Script based implementation. Must be used only for not good for implementation in core spell effects
            // So called only for not proccessed cases
            if (gameObjTarget)
                Global.ScriptMgr.OnDummyEffect(m_caster, m_spellInfo.Id, effIndex, gameObjTarget);
            else if (unitTarget && unitTarget.IsTypeId(TypeId.Unit))
                Global.ScriptMgr.OnDummyEffect(m_caster, m_spellInfo.Id, effIndex, unitTarget.ToCreature());
            else if (itemTarget)
                Global.ScriptMgr.OnDummyEffect(m_caster, m_spellInfo.Id, effIndex, itemTarget);
        }

        [SpellEffectHandler(SpellEffectName.TriggerSpell)]
        [SpellEffectHandler(SpellEffectName.TriggerSpellWithValue)]
        void EffectTriggerSpell(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.LaunchTarget
                && effectHandleMode != SpellEffectHandleMode.Launch)
                return;

            uint triggered_spell_id = effectInfo.TriggerSpell;

            // @todo move those to spell scripts
            if (effectInfo.Effect == SpellEffectName.TriggerSpell && effectHandleMode == SpellEffectHandleMode.LaunchTarget)
            {
                // special cases
                switch (triggered_spell_id)
                {
                    // Vanish (not exist)
                    case 18461:
                        {
                            unitTarget.RemoveMovementImpairingAuras();
                            unitTarget.RemoveAurasByType(AuraType.ModStalked);
                            return;
                        }
                    // Demonic Empowerment -- succubus
                    case 54437:
                        {
                            unitTarget.RemoveMovementImpairingAuras();
                            unitTarget.RemoveAurasByType(AuraType.ModStalked);
                            unitTarget.RemoveAurasByType(AuraType.ModStun);

                            // Cast Lesser Invisibility
                            unitTarget.CastSpell(unitTarget, 7870, true);
                            return;
                        }
                    // Brittle Armor - (need add max stack of 24575 Brittle Armor)
                    case 29284:
                        {
                            // Brittle Armor
                            SpellInfo spell = Global.SpellMgr.GetSpellInfo(24575);
                            if (spell == null)
                                return;

                            for (uint j = 0; j < spell.StackAmount; ++j)
                                m_caster.CastSpell(unitTarget, spell.Id, true);
                            return;
                        }
                    // Mercurial Shield - (need add max stack of 26464 Mercurial Shield)
                    case 29286:
                        {
                            // Mercurial Shield
                            SpellInfo spell = Global.SpellMgr.GetSpellInfo(26464);
                            if (spell == null)
                                return;

                            for (uint j = 0; j < spell.StackAmount; ++j)
                                m_caster.CastSpell(unitTarget, spell.Id, true);
                            return;
                        }
                    // Cloak of Shadows
                    case 35729:
                        {
                            uint dispelMask = SpellInfo.GetDispelMask(DispelType.ALL);
                            foreach (var iter in unitTarget.GetAppliedAuras())
                            {
                                // remove all harmful spells on you...
                                SpellInfo spell = iter.Value.GetBase().GetSpellInfo();
                                if ((spell.DmgClass == SpellDmgClass.Magic // only affect magic spells
                                    || (Convert.ToBoolean(spell.GetDispelMask() & dispelMask))
                                    // ignore positive and passive auras
                                    && !iter.Value.IsPositive() && !iter.Value.GetBase().IsPassive()))
                                {
                                    m_caster.RemoveAura(iter);
                                }
                            }
                            return;
                        }
                }
            }

            // normal case
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(triggered_spell_id);
            if (spellInfo == null)
            {
                Log.outDebug(LogFilter.Spells, "Spell.EffectTriggerSpell spell {0} tried to trigger unknown spell {1}", m_spellInfo.Id, triggered_spell_id);
                return;
            }

            SpellCastTargets targets = new SpellCastTargets();
            if (effectHandleMode == SpellEffectHandleMode.LaunchTarget)
            {
                if (!spellInfo.NeedsToBeTriggeredByCaster(m_spellInfo, m_caster.GetMap().GetDifficultyID()))
                    return;
                targets.SetUnitTarget(unitTarget);
            }
            else //if (effectHandleMode == SpellEffectHandleMode.Launch)
            {
                if (spellInfo.NeedsToBeTriggeredByCaster(m_spellInfo, m_caster.GetMap().GetDifficultyID()) && effectInfo.GetProvidedTargetMask().HasAnyFlag(SpellCastTargetFlags.UnitMask))
                    return;

                if (spellInfo.GetExplicitTargetMask().HasAnyFlag(SpellCastTargetFlags.DestLocation))
                    targets.SetDst(m_targets);

                targets.SetUnitTarget(m_caster);
            }

            Dictionary<SpellValueMod, int> values = new Dictionary<SpellValueMod, int>();
            // set basepoints for trigger with value effect
            if (effectInfo.Effect == SpellEffectName.TriggerSpellWithValue)
            {
                values.Add(SpellValueMod.BasePoint0, damage);
                values.Add(SpellValueMod.BasePoint1, damage);
                values.Add(SpellValueMod.BasePoint2, damage);
            }

            // original caster guid only for GO cast
            m_caster.CastSpell(targets, spellInfo, values, TriggerCastFlags.FullMask, null, null, m_originalCasterGUID);
        }

        [SpellEffectHandler(SpellEffectName.TriggerMissile)]
        [SpellEffectHandler(SpellEffectName.TriggerMissileSpellWithValue)]
        void EffectTriggerMissileSpell(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget
                && effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            uint triggered_spell_id = effectInfo.TriggerSpell;

            // normal case
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(triggered_spell_id);
            if (spellInfo == null)
            {
                Log.outDebug(LogFilter.Spells, "Spell.EffectTriggerMissileSpell spell {0} tried to trigger unknown spell {1}", m_spellInfo.Id, triggered_spell_id);
                return;
            }

            SpellCastTargets targets = new SpellCastTargets();
            if (effectHandleMode == SpellEffectHandleMode.HitTarget)
            {
                if (!spellInfo.NeedsToBeTriggeredByCaster(m_spellInfo, m_caster.GetMap().GetDifficultyID()))
                    return;
                targets.SetUnitTarget(unitTarget);
            }
            else //if (effectHandleMode == SpellEffectHandleMode.Hit)
            {
                if (spellInfo.NeedsToBeTriggeredByCaster(m_spellInfo, m_caster.GetMap().GetDifficultyID()) && effectInfo.GetProvidedTargetMask().HasAnyFlag(SpellCastTargetFlags.UnitMask))
                    return;

                if (spellInfo.GetExplicitTargetMask().HasAnyFlag(SpellCastTargetFlags.DestLocation))
                    targets.SetDst(m_targets);

                targets.SetUnitTarget(m_caster);
            }

            Dictionary<SpellValueMod, int> values = new Dictionary<SpellValueMod, int>();
            // set basepoints for trigger with value effect
            if (effectInfo.Effect == SpellEffectName.TriggerMissileSpellWithValue)
            {
                // maybe need to set value only when basepoints == 0?
                values.Add(SpellValueMod.BasePoint0, damage);
                values.Add(SpellValueMod.BasePoint1, damage);
                values.Add(SpellValueMod.BasePoint2, damage);
            }

            // original caster guid only for GO cast
            m_caster.CastSpell(targets, spellInfo, values, TriggerCastFlags.FullMask, null, null, m_originalCasterGUID);
        }

        [SpellEffectHandler(SpellEffectName.ForceCast)]
        [SpellEffectHandler(SpellEffectName.ForceCastWithValue)]
        [SpellEffectHandler(SpellEffectName.ForceCast2)]
        void EffectForceCast(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            uint triggered_spell_id = effectInfo.TriggerSpell;

            // normal case
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(triggered_spell_id);
            if (spellInfo == null)
            {
                Log.outError(LogFilter.Spells, "Spell.EffectForceCast of spell {0}: triggering unknown spell id {1}", m_spellInfo.Id, triggered_spell_id);
                return;
            }

            if (effectInfo.Effect == SpellEffectName.ForceCast && damage != 0)
            {
                switch (m_spellInfo.Id)
                {
                    case 52588: // Skeletal Gryphon Escape
                    case 48598: // Ride Flamebringer Cue
                        unitTarget.RemoveAura((uint)damage);
                        break;
                    case 52463: // Hide In Mine Car
                    case 52349: // Overtake
                        unitTarget.CastCustomSpell(unitTarget, spellInfo.Id, damage, 0, 0, true, null, null, m_originalCasterGUID);
                        return;
                }
            }

            Dictionary<SpellValueMod, int> values = new Dictionary<SpellValueMod, int>();
            // set basepoints for trigger with value effect
            if (effectInfo.Effect == SpellEffectName.ForceCastWithValue)
            {
                // maybe need to set value only when basepoints == 0?
                values.Add(SpellValueMod.BasePoint0, damage);
                values.Add(SpellValueMod.BasePoint1, damage);
                values.Add(SpellValueMod.BasePoint2, damage);
            }

            SpellCastTargets targets = new SpellCastTargets();
            targets.SetUnitTarget(m_caster);

            unitTarget.CastSpell(targets, spellInfo, values, TriggerCastFlags.FullMask);
        }

        [SpellEffectHandler(SpellEffectName.TriggerSpell2)]
        void EffectTriggerRitualOfSummoning(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            uint triggered_spell_id = effectInfo.TriggerSpell;
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(triggered_spell_id);
            if (spellInfo == null)
            {
                Log.outError(LogFilter.Spells, "EffectTriggerRitualOfSummoning of spell {0}: triggering unknown spell id {1}", m_spellInfo.Id, triggered_spell_id);
                return;
            }

            finish();

            m_caster.CastSpell(null, spellInfo, false);
        }

        [SpellEffectHandler(SpellEffectName.Jump)]
        void EffectJump(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.LaunchTarget)
                return;

            if (m_caster.IsInFlight())
                return;

            if (unitTarget == null)
                return;

            float x, y, z;
            unitTarget.GetContactPoint(m_caster, out x, out y, out z, SharedConst.ContactDistance);

            float speedXY, speedZ;
            CalculateJumpSpeeds(effectInfo, m_caster.GetExactDist2d(x, y), out speedXY, out speedZ);
            JumpArrivalCastArgs arrivalCast = new JumpArrivalCastArgs();
            arrivalCast.SpellId = effectInfo.TriggerSpell;
            arrivalCast.Target = unitTarget.GetGUID();
            m_caster.GetMotionMaster().MoveJump(x, y, z, 0.0f, speedXY, speedZ, EventId.Jump, false, arrivalCast);
        }

        [SpellEffectHandler(SpellEffectName.JumpDest)]
        void EffectJumpDest(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Launch)
                return;

            if (m_caster.IsInFlight())
                return;

            if (!m_targets.HasDst())
                return;

            float speedXY, speedZ;
            CalculateJumpSpeeds(effectInfo, m_caster.GetExactDist2d(destTarget), out speedXY, out speedZ);
            JumpArrivalCastArgs arrivalCast = new JumpArrivalCastArgs();
            arrivalCast.SpellId = effectInfo.TriggerSpell;
            m_caster.GetMotionMaster().MoveJump(destTarget, speedXY, speedZ, EventId.Jump, !m_targets.GetObjectTargetGUID().IsEmpty(), arrivalCast);
        }

        void CalculateJumpSpeeds(SpellEffectInfo effInfo, float dist, out float speedXY, out float speedZ)
        {
            if (effInfo.MiscValue != 0)
                speedZ = (float)effInfo.MiscValue / 10;
            else if (effInfo.MiscValueB != 0)
                speedZ = (float)effInfo.MiscValueB / 10;
            else
                speedZ = 10.0f;

            speedXY = dist * 10.0f / speedZ;
        }

        [SpellEffectHandler(SpellEffectName.TeleportUnits)]
        void EffectTeleportUnits(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || unitTarget.IsInFlight())
                return;

            // If not exist data for dest location - return
            if (!m_targets.HasDst())
            {
                Log.outError(LogFilter.Spells, "Spell.EffectTeleportUnits - does not have a destination for spellId {0}.", m_spellInfo.Id);
                return;
            }

            // Init dest coordinates
            uint mapid = destTarget.GetMapId();
            if (mapid == 0xFFFFFFFF)
                mapid = unitTarget.GetMapId();
            float x, y, z, orientation;
            destTarget.GetPosition(out x, out y, out z, out orientation);
            if (orientation == 0 && m_targets.GetUnitTarget() != null)
                orientation = m_targets.GetUnitTarget().GetOrientation();
            Log.outDebug(LogFilter.Spells, "Spell.EffectTeleportUnits - teleport unit to {0} {1} {2} {3} {4}\n", mapid, x, y, z, orientation);

            Player player = unitTarget.ToPlayer();
            if (player)
            {
                // Custom loading screen
                uint customLoadingScreenId = (uint)effectInfo.MiscValue;
                if (customLoadingScreenId != 0)
                    player.SendPacket(new CustomLoadScreen(m_spellInfo.Id, customLoadingScreenId));

                player.TeleportTo(mapid, x, y, z, orientation, unitTarget == m_caster ? TeleportToOptions.Spell | TeleportToOptions.NotLeaveCombat : 0);
            }
            else if (mapid == unitTarget.GetMapId())
                unitTarget.NearTeleportTo(x, y, z, orientation, unitTarget == m_caster);
            else
            {
                Log.outError(LogFilter.Spells, "Spell.EffectTeleportUnits - spellId {0} attempted to teleport creature to a different map.", m_spellInfo.Id);
                return;
            }

            // post effects for TARGET_DEST_DB
            switch (m_spellInfo.Id)
            {
                // Dimensional Ripper - Everlook
                case 23442:
                    {
                        int r = RandomHelper.IRand(0, 119);
                        if (r >= 70)                                  // 7/12 success
                        {
                            if (r < 100)                              // 4/12 evil twin
                                m_caster.CastSpell(m_caster, 23445, true);
                            else                                        // 1/12 fire
                                m_caster.CastSpell(m_caster, 23449, true);
                        }
                        return;
                    }
                // Ultrasafe Transporter: Toshley's Station
                case 36941:
                    {
                        if (RandomHelper.randChance(50))                        // 50% success
                        {
                            int rand_eff = RandomHelper.IRand(1, 7);
                            switch (rand_eff)
                            {
                                case 1:
                                    // soul split - evil
                                    m_caster.CastSpell(m_caster, 36900, true);
                                    break;
                                case 2:
                                    // soul split - good
                                    m_caster.CastSpell(m_caster, 36901, true);
                                    break;
                                case 3:
                                    // Increase the size
                                    m_caster.CastSpell(m_caster, 36895, true);
                                    break;
                                case 4:
                                    // Decrease the size
                                    m_caster.CastSpell(m_caster, 36893, true);
                                    break;
                                case 5:
                                    // Transform
                                    {
                                        if (m_caster.ToPlayer().GetTeam() == Team.Alliance)
                                            m_caster.CastSpell(m_caster, 36897, true);
                                        else
                                            m_caster.CastSpell(m_caster, 36899, true);
                                        break;
                                    }
                                case 6:
                                    // chicken
                                    m_caster.CastSpell(m_caster, 36940, true);
                                    break;
                                case 7:
                                    // evil twin
                                    m_caster.CastSpell(m_caster, 23445, true);
                                    break;
                            }
                        }
                        return;
                    }
                // Dimensional Ripper - Area 52
                case 36890:
                    {
                        if (RandomHelper.randChance(50))                        // 50% success
                        {
                            int rand_eff = RandomHelper.IRand(1, 4);
                            switch (rand_eff)
                            {
                                case 1:
                                    // soul split - evil
                                    m_caster.CastSpell(m_caster, 36900, true);
                                    break;
                                case 2:
                                    // soul split - good
                                    m_caster.CastSpell(m_caster, 36901, true);
                                    break;
                                case 3:
                                    // Increase the size
                                    m_caster.CastSpell(m_caster, 36895, true);
                                    break;
                                case 4:
                                    // Transform
                                    {
                                        if (m_caster.ToPlayer().GetTeam() == Team.Alliance)
                                            m_caster.CastSpell(m_caster, 36897, true);
                                        else
                                            m_caster.CastSpell(m_caster, 36899, true);
                                        break;
                                    }
                            }
                        }
                        return;
                    }
            }
        }

        [SpellEffectHandler(SpellEffectName.ApplyAura)]
        void EffectApplyAura(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (m_spellAura == null || unitTarget == null)
                return;

            Cypher.Assert(unitTarget == m_spellAura.GetOwner());
            m_spellAura._ApplyEffectForTargets(effIndex);
        }

        [SpellEffectHandler(SpellEffectName.ApplyAreaAuraEnemy)]
        [SpellEffectHandler(SpellEffectName.ApplyAreaAuraFriend)]
        [SpellEffectHandler(SpellEffectName.ApplyAreaAuraOwner)]
        [SpellEffectHandler(SpellEffectName.ApplyAreaAuraParty)]
        [SpellEffectHandler(SpellEffectName.ApplyAreaAuraPet)]
        [SpellEffectHandler(SpellEffectName.ApplyAreaAuraRaid)]
        void EffectApplyAreaAura(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (m_spellAura == null || unitTarget == null)
                return;
            Cypher.Assert(unitTarget == m_spellAura.GetOwner());
            m_spellAura._ApplyEffectForTargets(effIndex);
        }

        [SpellEffectHandler(SpellEffectName.UnlearnSpecialization)]
        void EffectUnlearnSpecialization(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            Player player = unitTarget.ToPlayer();
            uint spellToUnlearn = effectInfo.TriggerSpell;

            player.RemoveSpell(spellToUnlearn);

            Log.outDebug(LogFilter.Spells, "Spell: Player {0} has unlearned spell {1} from NpcGUID: {2}", player.GetGUID().ToString(), spellToUnlearn, m_caster.GetGUID().ToString());
        }

        [SpellEffectHandler(SpellEffectName.PowerDrain)]
        void EffectPowerDrain(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (effectInfo.MiscValue < 0 || effectInfo.MiscValue >= (byte)PowerType.Max)
                return;

            PowerType powerType = (PowerType)effectInfo.MiscValue;

            if (unitTarget == null || !unitTarget.IsAlive() || unitTarget.GetPowerType() != powerType || damage < 0)
                return;

            // add spell damage bonus
            uint bonus = m_caster.SpellDamageBonusDone(unitTarget, m_spellInfo, (uint)damage, DamageEffectType.SpellDirect, effectInfo);
            damage = (int)(bonus + (bonus * _variance));
            damage = (int)unitTarget.SpellDamageBonusTaken(m_caster, m_spellInfo, (uint)damage, DamageEffectType.SpellDirect, effectInfo);

            int newDamage = -(unitTarget.ModifyPower(powerType, -damage));

            float gainMultiplier = 0.0f;

            // Don't restore from self drain
            if (m_caster != unitTarget)
            {
                gainMultiplier = effectInfo.CalcValueMultiplier(m_originalCaster, this);

                int gain = (int)(newDamage * gainMultiplier);

                m_caster.EnergizeBySpell(m_caster, m_spellInfo.Id, gain, powerType);
            }
            ExecuteLogEffectTakeTargetPower(effIndex, unitTarget, powerType, (uint)newDamage, gainMultiplier);
        }

        [SpellEffectHandler(SpellEffectName.SendEvent)]
        void EffectSendEvent(uint effIndex)
        {
            // we do not handle a flag dropping or clicking on flag in Battlegroundby sendevent system
            if (effectHandleMode != SpellEffectHandleMode.HitTarget
                && effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            WorldObject target = null;

            // call events for object target if present
            if (effectHandleMode == SpellEffectHandleMode.HitTarget)
            {
                if (unitTarget != null)
                    target = unitTarget;
                else if (gameObjTarget != null)
                    target = gameObjTarget;
            }
            else // if (effectHandleMode == SpellEffectHandleMode.Hit)
            {
                // let's prevent executing effect handler twice in case when spell effect is capable of targeting an object
                // this check was requested by scripters, but it has some downsides:
                // now it's impossible to script (using sEventScripts) a cast which misses all targets
                // or to have an ability to script the moment spell hits dest (in a case when there are object targets present)
                if (effectInfo.GetProvidedTargetMask().HasAnyFlag(SpellCastTargetFlags.UnitMask | SpellCastTargetFlags.GameobjectMask))
                    return;
                // some spells have no target entries in dbc and they use focus target
                if (focusObject != null)
                    target = focusObject;
                // @todo there should be a possibility to pass dest target to event script
            }

            Log.outDebug(LogFilter.Spells, "Spell ScriptStart {0} for spellid {1} in EffectSendEvent ", effectInfo.MiscValue, m_spellInfo.Id);

            ZoneScript zoneScript = m_caster.GetZoneScript();
            InstanceScript instanceScript = m_caster.GetInstanceScript();
            if (zoneScript != null)
                zoneScript.ProcessEvent(target, (uint)effectInfo.MiscValue);
            else if (instanceScript != null)    // needed in case Player is the caster
                instanceScript.ProcessEvent(target, (uint)effectInfo.MiscValue);

            m_caster.GetMap().ScriptsStart(ScriptsType.Event, (uint)effectInfo.MiscValue, m_caster, target);
        }

        [SpellEffectHandler(SpellEffectName.PowerBurn)]
        void EffectPowerBurn(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (effectInfo.MiscValue < 0 || effectInfo.MiscValue >= (int)PowerType.Max)
                return;

            PowerType powerType = (PowerType)effectInfo.MiscValue;

            if (unitTarget == null || !unitTarget.IsAlive() || unitTarget.GetPowerType() != powerType || damage < 0)
                return;

            int newDamage = -(unitTarget.ModifyPower(powerType, -damage));

            // NO - Not a typo - EffectPowerBurn uses effect value multiplier - not effect damage multiplier
            float dmgMultiplier = effectInfo.CalcValueMultiplier(m_originalCaster, this);

            // add log data before multiplication (need power amount, not damage)
            ExecuteLogEffectTakeTargetPower(effIndex, unitTarget, powerType, (uint)newDamage, 0.0f);

            newDamage = (int)(newDamage * dmgMultiplier);

            m_damage += newDamage;
        }

        [SpellEffectHandler(SpellEffectName.Heal)]
        void EffectHeal(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.LaunchTarget)
                return;

            if (unitTarget != null && unitTarget.IsAlive() && damage >= 0)
            {
                // Try to get original caster
                Unit caster = !m_originalCasterGUID.IsEmpty() ? m_originalCaster : m_caster;

                // Skip if m_originalCaster not available
                if (caster == null)
                    return;

                int addhealth = damage;

                // Vessel of the Naaru (Vial of the Sunwell trinket)
                if (m_spellInfo.Id == 45064)
                {
                    // Amount of heal - depends from stacked Holy Energy
                    int damageAmount = 0;
                    AuraEffect aurEff = m_caster.GetAuraEffect(45062, 0);
                    if (aurEff != null)
                    {
                        damageAmount += aurEff.GetAmount();
                        m_caster.RemoveAurasDueToSpell(45062);
                    }

                    addhealth += damageAmount;
                }
                // Runic Healing Injector (heal increased by 25% for engineers - 3.2.0 patch change)
                else if (m_spellInfo.Id == 67489)
                {
                    Player player = m_caster.ToPlayer();
                    if (player != null)
                        if (player.HasSkill(SkillType.Engineering))
                            MathFunctions.AddPct(ref addhealth, 25);
                }
                // Death Pact - return pct of max health to caster
                else if (m_spellInfo.SpellFamilyName == SpellFamilyNames.Deathknight && m_spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00080000u))
                    addhealth = (int)caster.SpellHealingBonusDone(unitTarget, m_spellInfo, (uint)caster.CountPctFromMaxHealth(damage), DamageEffectType.Heal, effectInfo);
                else
                {
                    addhealth = (int)caster.SpellHealingBonusDone(unitTarget, m_spellInfo, (uint)addhealth, DamageEffectType.Heal, effectInfo);
                    uint bonus = caster.SpellHealingBonusDone(unitTarget, m_spellInfo, (uint)addhealth, DamageEffectType.Heal, effectInfo);
                    damage = (int)(bonus + (bonus * _variance));
                }

                addhealth = (int)unitTarget.SpellHealingBonusTaken(caster, m_spellInfo, (uint)addhealth, DamageEffectType.Heal, effectInfo);

                // Remove Grievious bite if fully healed
                if (unitTarget.HasAura(48920) && ((uint)(unitTarget.GetHealth() + (ulong)addhealth) >= unitTarget.GetMaxHealth()))
                    unitTarget.RemoveAura(48920);

                m_damage -= addhealth;
            }
        }

        [SpellEffectHandler(SpellEffectName.HealPct)]
        void EffectHealPct(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsAlive() || damage < 0)
                return;

            // Skip if m_originalCaster not available
            if (m_originalCaster == null)
                return;

            uint heal = m_originalCaster.SpellHealingBonusDone(unitTarget, m_spellInfo, (uint)unitTarget.CountPctFromMaxHealth(damage), DamageEffectType.Heal, effectInfo);
            heal = unitTarget.SpellHealingBonusTaken(m_originalCaster, m_spellInfo, heal, DamageEffectType.Heal, effectInfo);

            m_healing += (int)heal;
        }

        [SpellEffectHandler(SpellEffectName.HealMechanical)]
        void EffectHealMechanical(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsAlive() || damage < 0)
                return;

            // Skip if m_originalCaster not available
            if (m_originalCaster == null)
                return;

            uint heal = m_originalCaster.SpellHealingBonusDone(unitTarget, m_spellInfo, (uint)damage, DamageEffectType.Heal, effectInfo);
            heal += (uint)(heal * _variance);

            m_healing += (int)unitTarget.SpellHealingBonusTaken(m_originalCaster, m_spellInfo, heal, DamageEffectType.Heal, effectInfo);
        }

        [SpellEffectHandler(SpellEffectName.HealthLeech)]
        void EffectHealthLeech(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsAlive() || damage < 0)
                return;

            uint bonus = m_caster.SpellDamageBonusDone(unitTarget, m_spellInfo, (uint)damage, DamageEffectType.SpellDirect, effectInfo);
            damage = (int)(bonus + (bonus * _variance));
            damage = (int)unitTarget.SpellDamageBonusTaken(m_caster, m_spellInfo, (uint)damage, DamageEffectType.SpellDirect, effectInfo);

            Log.outDebug(LogFilter.Spells, "HealthLeech :{0}", damage);

            float healMultiplier = effectInfo.CalcValueMultiplier(m_originalCaster, this);

            m_damage += damage;
            // get max possible damage, don't count overkill for heal
            uint healthGain = (uint)(-unitTarget.GetHealthGain(-damage) * healMultiplier);

            if (m_caster.IsAlive())
            {
                healthGain = m_caster.SpellHealingBonusDone(m_caster, m_spellInfo, healthGain, DamageEffectType.Heal, effectInfo);
                healthGain = m_caster.SpellHealingBonusTaken(m_caster, m_spellInfo, healthGain, DamageEffectType.Heal, effectInfo);

                HealInfo healInfo = new HealInfo(m_caster, m_caster, healthGain, m_spellInfo, m_spellSchoolMask);
                m_caster.HealBySpell(healInfo);
            }
        }

        public void DoCreateItem(uint i, uint itemtype, byte context = 0, List<uint> bonusListIds = null)
        {
            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            Player player = unitTarget.ToPlayer();

            uint newitemid = itemtype;
            ItemTemplate pProto = Global.ObjectMgr.GetItemTemplate(newitemid);
            if (pProto == null)
            {
                player.SendEquipError(InventoryResult.ItemNotFound);
                return;
            }

            // bg reward have some special in code work
            BattlegroundTypeId bgType = 0;
            switch ((BattlegroundMarks)m_spellInfo.Id)
            {
                case BattlegroundMarks.SpellAvMarkWinner:
                case BattlegroundMarks.SpellAvMarkLoser:
                    bgType = BattlegroundTypeId.AV;
                    break;
                case BattlegroundMarks.SpellWsMarkWinner:
                case BattlegroundMarks.SpellWsMarkLoser:
                    bgType = BattlegroundTypeId.WS;
                    break;
                case BattlegroundMarks.SpellAbMarkWinner:
                case BattlegroundMarks.SpellAbMarkLoser:
                    bgType = BattlegroundTypeId.AB;
                    break;
                default:
                    break;
            }

            uint num_to_add = (uint)damage;

            if (num_to_add < 1)
                num_to_add = 1;
            if (num_to_add > pProto.GetMaxStackSize())
                num_to_add = pProto.GetMaxStackSize();

            // this is bad, should be done using spell_loot_template (and conditions)

            // the chance of getting a perfect result
            float perfectCreateChance = 0.0f;
            // the resulting perfect item if successful
            uint perfectItemType = itemtype;
            // get perfection capability and chance
            if (SkillPerfectItems.CanCreatePerfectItem(player, m_spellInfo.Id, ref perfectCreateChance, ref perfectItemType))
                if (RandomHelper.randChance(perfectCreateChance)) // if the roll succeeds...
                    newitemid = perfectItemType;        // the perfect item replaces the regular one

            // init items_count to 1, since 1 item will be created regardless of specialization
            int items_count = 1;
            // the chance to create additional items
            float additionalCreateChance = 0.0f;
            // the maximum number of created additional items
            byte additionalMaxNum = 0;
            // get the chance and maximum number for creating extra items
            if (SkillExtraItems.CanCreateExtraItems(player, m_spellInfo.Id, ref additionalCreateChance, ref additionalMaxNum))
            {
                // roll with this chance till we roll not to create or we create the max num
                while (RandomHelper.randChance(additionalCreateChance) && items_count <= additionalMaxNum)
                    ++items_count;
            }

            // really will be created more items
            num_to_add *= (uint)items_count;

            // can the player store the new item?
            List<ItemPosCount> dest = new List<ItemPosCount>();
            uint no_space = 0;
            InventoryResult msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, newitemid, num_to_add, out no_space);
            if (msg != InventoryResult.Ok)
            {
                // convert to possible store amount
                if (msg == InventoryResult.InvFull || msg == InventoryResult.ItemMaxCount)
                    num_to_add -= no_space;
                else
                {
                    // if not created by another reason from full inventory or unique items amount limitation
                    player.SendEquipError(msg, null, null, newitemid);
                    return;
                }
            }

            if (num_to_add != 0)
            {
                // create the new item and store it
                Item pItem = player.StoreNewItem(dest, newitemid, true, ItemEnchantment.GenerateItemRandomPropertyId(newitemid), null, context, bonusListIds);

                // was it successful? return error if not
                if (pItem == null)
                {
                    player.SendEquipError(InventoryResult.ItemNotFound);
                    return;
                }

                // set the "Crafted by ..." property of the item
                if (pItem.GetTemplate().GetClass() != ItemClass.Consumable && pItem.GetTemplate().GetClass() != ItemClass.Quest && newitemid != 6265 && newitemid != 6948)
                    pItem.SetGuidValue(ItemFields.Creator, player.GetGUID());

                // send info to the client
                player.SendNewItem(pItem, num_to_add, true, bgType == 0);

                if (pItem.GetQuality() > ItemQuality.Epic || (pItem.GetQuality() == ItemQuality.Epic && pItem.GetItemLevel(player) >= GuildConst.MinNewsItemLevel))
                {
                    Guild guild = player.GetGuild();
                    if (guild != null)
                        guild.AddGuildNews(GuildNews.ItemCrafted, player.GetGUID(), 0, pProto.GetId());
                }

                // we succeeded in creating at least one item, so a levelup is possible
                if (bgType == 0)
                    player.UpdateCraftSkill(m_spellInfo.Id);
            }
        }

        [SpellEffectHandler(SpellEffectName.CreateItem)]
        void EffectCreateItem(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            DoCreateItem(effIndex, effectInfo.ItemType);
            ExecuteLogEffectCreateItem(effIndex, effectInfo.ItemType);
        }

        [SpellEffectHandler(SpellEffectName.CreateLoot)]
        void EffectCreateItem2(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            Player player = unitTarget.ToPlayer();

            uint item_id = effectInfo.ItemType;

            if (item_id != 0)
                DoCreateItem(effIndex, item_id);

            // special case: fake item replaced by generate using spell_loot_template
            if (m_spellInfo.IsLootCrafting())
            {
                if (item_id != 0)
                {
                    if (!player.HasItemCount(item_id))
                        return;

                    // remove reagent
                    uint count = 1;
                    player.DestroyItemCount(item_id, count, true);

                    // create some random items
                    player.AutoStoreLoot(m_spellInfo.Id, LootStorage.Spell);
                }
                else
                    player.AutoStoreLoot(m_spellInfo.Id, LootStorage.Spell);    // create some random items

                player.UpdateCraftSkill(m_spellInfo.Id);
            }
            // @todo ExecuteLogEffectCreateItem(i, GetEffect(i].ItemType);
        }

        [SpellEffectHandler(SpellEffectName.CreateRandomItem)]
        void EffectCreateRandomItem(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;
            Player player = unitTarget.ToPlayer();

            // create some random items
            player.AutoStoreLoot(m_spellInfo.Id, LootStorage.Spell);
            // @todo ExecuteLogEffectCreateItem(i, GetEffect(i].ItemType);
        }

        [SpellEffectHandler(SpellEffectName.PersistentAreaAura)]
        void EffectPersistentAA(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (m_spellAura == null)
            {
                Unit caster = m_caster.GetEntry() == SharedConst.WorldTrigger ? m_originalCaster : m_caster;
                float radius = effectInfo.CalcRadius(caster);

                // Caster not in world, might be spell triggered from aura removal
                if (!caster.IsInWorld)
                    return;
                DynamicObject dynObj = new DynamicObject(false);
                if (!dynObj.CreateDynamicObject(caster.GetMap().GenerateLowGuid(HighGuid.DynamicObject), caster, m_spellInfo, destTarget, radius, DynamicObjectType.AreaSpell, m_SpellVisual))
                    return;

                Aura aura = Aura.TryCreate(m_spellInfo, m_castId, SpellConst.MaxEffectMask, dynObj, caster, m_spellValue.EffectBasePoints, null, ObjectGuid.Empty, ObjectGuid.Empty, m_castItemLevel);
                if (aura != null)
                {
                    m_spellAura = aura;
                    m_spellAura._RegisterForTargets();
                }
                else
                    return;
            }

            Cypher.Assert(m_spellAura.GetDynobjOwner());
            m_spellAura._ApplyEffectForTargets(effIndex);
        }

        [SpellEffectHandler(SpellEffectName.Energize)]
        void EffectEnergize(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            if (!unitTarget.IsAlive())
                return;

            if (effectInfo.MiscValue < 0 || effectInfo.MiscValue >= (byte)PowerType.Max)
                return;

            PowerType power = (PowerType)effectInfo.MiscValue;
            if (unitTarget.GetMaxPower(power) == 0)
                return;

            // Some level depends spells
            switch (m_spellInfo.Id)
            {
                case 24571:                                         // Blood Fury
                                                                    // Instantly increases your rage by ${(300-10*$max(0,$PL-60))/10}.
                    damage -= 10 * (int)Math.Max(0, Math.Min(30, m_caster.getLevel() - 60));
                    break;
                case 24532:                                         // Burst of Energy
                                                                    // Instantly increases your energy by ${60-4*$max(0,$min(15,$PL-60))}.
                    damage -= 4 * (int)Math.Max(0, Math.Min(15, m_caster.getLevel() - 60));
                    break;
                case 67490:                                         // Runic Mana Injector (mana gain increased by 25% for engineers - 3.2.0 patch change)
                    {
                        Player player = m_caster.ToPlayer();
                        if (player != null)
                            if (player.HasSkill(SkillType.Engineering))
                                MathFunctions.AddPct(ref damage, 25);
                        break;
                    }
                default:
                    break;
            }

            m_caster.EnergizeBySpell(unitTarget, m_spellInfo.Id, damage, power);

            // Mad Alchemist's Potion
            if (m_spellInfo.Id == 45051)
            {
                // find elixirs on target
                bool guardianFound = false;
                bool battleFound = false;
                foreach (var app in unitTarget.GetAppliedAuras())
                {
                    uint spell_id = app.Value.GetBase().GetId();
                    if (!guardianFound)
                        if (Global.SpellMgr.IsSpellMemberOfSpellGroup(spell_id, SpellGroup.ElixirGuardian))
                            guardianFound = true;
                    if (!battleFound)
                        if (Global.SpellMgr.IsSpellMemberOfSpellGroup(spell_id, SpellGroup.ElixirBattle))
                            battleFound = true;
                    if (battleFound && guardianFound)
                        break;
                }

                // get all available elixirs by mask and spell level
                List<int> avalibleElixirs = new List<int>();
                if (!guardianFound)
                    Global.SpellMgr.GetSetOfSpellsInSpellGroup(SpellGroup.ElixirGuardian, out avalibleElixirs);
                if (!battleFound)
                    Global.SpellMgr.GetSetOfSpellsInSpellGroup(SpellGroup.ElixirBattle, out avalibleElixirs);
                foreach (int spellId in avalibleElixirs)
                {
                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo((uint)spellId);
                    if (spellInfo.SpellLevel < m_spellInfo.SpellLevel || spellInfo.SpellLevel > unitTarget.getLevel())
                        avalibleElixirs.Remove(spellId);
                    else if (Global.SpellMgr.IsSpellMemberOfSpellGroup((uint)spellId, SpellGroup.ElixirShattrath))
                        avalibleElixirs.Remove(spellId);
                    else if (Global.SpellMgr.IsSpellMemberOfSpellGroup((uint)spellId, SpellGroup.ElixirUnstable))
                        avalibleElixirs.Remove(spellId);
                }

                if (!avalibleElixirs.Empty())
                {
                    // cast random elixir on target
                    m_caster.CastSpell(unitTarget, (uint)avalibleElixirs.SelectRandom(), true, m_CastItem);
                }
            }
        }

        [SpellEffectHandler(SpellEffectName.EnergizePct)]
        void EffectEnergizePct(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;
            if (!unitTarget.IsAlive())
                return;

            if (effectInfo.MiscValue < 0 || effectInfo.MiscValue >= (byte)PowerType.Max)
                return;

            PowerType power = (PowerType)effectInfo.MiscValue;
            uint maxPower = (uint)unitTarget.GetMaxPower(power);
            if (maxPower == 0)
                return;

            int gain = (int)MathFunctions.CalculatePct(maxPower, damage);
            m_caster.EnergizeBySpell(unitTarget, m_spellInfo.Id, gain, power);
        }

        void SendLoot(ObjectGuid guid, LootType loottype)
        {
            Player player = m_caster.ToPlayer();
            if (player == null)
                return;

            if (gameObjTarget != null)
            {
                // Players shouldn't be able to loot gameobjects that are currently despawned
                if (!gameObjTarget.isSpawned() && !player.IsGameMaster())
                {
                    Log.outError(LogFilter.Spells, "Possible hacking attempt: Player {0} [{1}] tried to loot a gameobject [{2}] which is on respawn time without being in GM mode!",
                                    player.GetName(), player.GetGUID().ToString(), gameObjTarget.GetGUID().ToString());
                    return;
                }
                // special case, already has GossipHello inside so return and avoid calling twice
                if (gameObjTarget.GetGoType() == GameObjectTypes.Goober)
                {
                    gameObjTarget.Use(m_caster);
                    return;
                }

                if (Global.ScriptMgr.OnGossipHello(player, gameObjTarget))
                    return;

                if (gameObjTarget.GetAI().GossipHello(player, true))
                    return;

                switch (gameObjTarget.GetGoType())
                {
                    case GameObjectTypes.Door:
                    case GameObjectTypes.Button:
                        gameObjTarget.UseDoorOrButton(0, false, player);
                        return;

                    case GameObjectTypes.QuestGiver:
                        player.PrepareGossipMenu(gameObjTarget, gameObjTarget.GetGoInfo().QuestGiver.gossipID, true);
                        player.SendPreparedGossip(gameObjTarget);
                        return;

                    case GameObjectTypes.SpellFocus:
                        // triggering linked GO
                        uint trapEntry = gameObjTarget.GetGoInfo().SpellFocus.linkedTrap;
                        if (trapEntry != 0)
                            gameObjTarget.TriggeringLinkedGameObject(trapEntry, m_caster);
                        return;
                    case GameObjectTypes.Chest:
                        // @todo possible must be moved to loot release (in different from linked triggering)
                        if (gameObjTarget.GetGoInfo().Chest.triggeredEvent != 0)
                        {
                            Log.outDebug(LogFilter.Spells, "Chest ScriptStart id {0} for GO {1}", gameObjTarget.GetGoInfo().Chest.triggeredEvent, gameObjTarget.GetSpawnId());
                            player.GetMap().ScriptsStart(ScriptsType.Event, gameObjTarget.GetGoInfo().Chest.triggeredEvent, player, gameObjTarget);
                        }

                        // triggering linked GO
                        uint _trapEntry = gameObjTarget.GetGoInfo().Chest.linkedTrap;
                        if (_trapEntry != 0)
                            gameObjTarget.TriggeringLinkedGameObject(_trapEntry, m_caster);
                        break;
                    // Don't return, let loots been taken
                    default:
                        break;
                }
            }

            // Send loot
            player.SendLoot(guid, loottype);
        }

        [SpellEffectHandler(SpellEffectName.OpenLock)]
        void EffectOpenLock(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!m_caster.IsTypeId(TypeId.Player))
            {
                Log.outDebug(LogFilter.Spells, "WORLD: Open Lock - No Player Caster!");
                return;
            }

            Player player = m_caster.ToPlayer();

            uint lockId = 0;
            ObjectGuid guid = ObjectGuid.Empty;

            // Get lockId
            if (gameObjTarget != null)
            {
                GameObjectTemplate goInfo = gameObjTarget.GetGoInfo();
                // Arathi Basin banner opening. // @todo Verify correctness of this check
                if ((goInfo.type == GameObjectTypes.Button && goInfo.Button.noDamageImmune != 0) ||
                    (goInfo.type == GameObjectTypes.Goober && goInfo.Goober.requireLOS != 0))
                {
                    //CanUseBattlegroundObject() already called in CheckCast()
                    // in Battlegroundcheck
                    Battleground bg = player.GetBattleground();
                    if (bg)
                    {
                        bg.EventPlayerClickedOnFlag(player, gameObjTarget);
                        return;
                    }
                }
                else if (goInfo.type == GameObjectTypes.FlagStand)
                {
                    //CanUseBattlegroundObject() already called in CheckCast()
                    // in Battlegroundcheck
                    Battleground bg = player.GetBattleground();
                    if (bg)
                    {
                        if (bg.GetTypeID(true) == BattlegroundTypeId.EY)
                            bg.EventPlayerClickedOnFlag(player, gameObjTarget);
                        return;
                    }
                }
                else if (m_spellInfo.Id == 1842 && gameObjTarget.GetGoInfo().type == GameObjectTypes.Trap && gameObjTarget.GetOwner() != null)
                {
                    gameObjTarget.SetLootState(LootState.JustDeactivated);
                    return;
                }
                // @todo Add script for spell 41920 - Filling, becouse server it freze when use this spell
                // handle outdoor pvp object opening, return true if go was registered for handling
                // these objects must have been spawned by outdoorpvp!
                else if (gameObjTarget.GetGoInfo().type == GameObjectTypes.Goober && Global.OutdoorPvPMgr.HandleOpenGo(player, gameObjTarget))
                    return;
                lockId = goInfo.GetLockId();
                guid = gameObjTarget.GetGUID();
            }
            else if (itemTarget != null)
            {
                lockId = itemTarget.GetTemplate().GetLockID();
                guid = itemTarget.GetGUID();
            }
            else
            {
                Log.outDebug(LogFilter.Spells, "WORLD: Open Lock - No GameObject/Item Target!");
                return;
            }

            SkillType skillId = SkillType.None;
            int reqSkillValue = 0;
            int skillValue = 0;

            SpellCastResult res = CanOpenLock(effIndex, lockId, ref skillId, ref reqSkillValue, ref skillValue);
            if (res != SpellCastResult.SpellCastOk)
            {
                SendCastResult(res);
                return;
            }

            if (gameObjTarget != null)
                SendLoot(guid, LootType.Skinning);
            else if (itemTarget != null)
            {
                itemTarget.SetFlag(ItemFields.Flags, ItemFieldFlags.Unlocked);
                itemTarget.SetState(ItemUpdateState.Changed, itemTarget.GetOwner());
            }

            // not allow use skill grow at item base open
            if (m_CastItem == null && skillId != SkillType.None)
            {
                // update skill if really known
                uint pureSkillValue = player.GetPureSkillValue(skillId);
                if (pureSkillValue != 0)
                {
                    if (gameObjTarget != null)
                    {
                        // Allow one skill-up until respawned
                        if (!gameObjTarget.IsInSkillupList(player.GetGUID()) &&
                            player.UpdateGatherSkill(skillId, pureSkillValue, (uint)reqSkillValue))
                            gameObjTarget.AddToSkillupList(player.GetGUID());
                    }
                    else if (itemTarget != null)
                    {
                        // Do one skill-up
                        player.UpdateGatherSkill(skillId, pureSkillValue, (uint)reqSkillValue);
                    }
                }
            }
            ExecuteLogEffectOpenLock(effIndex, gameObjTarget != null ? gameObjTarget : (WorldObject)itemTarget);
        }

        [SpellEffectHandler(SpellEffectName.SummonChangeItem)]
        void EffectSummonChangeItem(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (!m_caster.IsTypeId(TypeId.Player))
                return;

            Player player = m_caster.ToPlayer();

            // applied only to using item
            if (m_CastItem == null)
                return;

            // ... only to item in own inventory/bank/equip_slot
            if (m_CastItem.GetOwnerGUID() != player.GetGUID())
                return;

            uint newitemid = effectInfo.ItemType;
            if (newitemid == 0)
                return;

            ushort pos = m_CastItem.GetPos();

            Item pNewItem = Item.CreateItem(newitemid, 1, player);
            if (pNewItem == null)
                return;

            for (var j = EnchantmentSlot.Perm; j <= EnchantmentSlot.Temp; ++j)
                if (m_CastItem.GetEnchantmentId(j) != 0)
                    pNewItem.SetEnchantment(j, m_CastItem.GetEnchantmentId(j), m_CastItem.GetEnchantmentDuration(j), m_CastItem.GetEnchantmentCharges(j));

            if (m_CastItem.GetUInt32Value(ItemFields.Durability) < m_CastItem.GetUInt32Value(ItemFields.MaxDurability))
            {
                double lossPercent = 1 - m_CastItem.GetUInt32Value(ItemFields.Durability) / (double)m_CastItem.GetUInt32Value(ItemFields.MaxDurability);
                player.DurabilityLoss(pNewItem, lossPercent);
            }

            if (player.IsInventoryPos(pos))
            {
                List<ItemPosCount> dest = new List<ItemPosCount>();
                InventoryResult msg = player.CanStoreItem(m_CastItem.GetBagSlot(), m_CastItem.GetSlot(), dest, pNewItem, true);
                if (msg == InventoryResult.Ok)
                {
                    player.DestroyItem(m_CastItem.GetBagSlot(), m_CastItem.GetSlot(), true);

                    // prevent crash at access and unexpected charges counting with item update queue corrupt
                    if (m_CastItem == m_targets.GetItemTarget())
                        m_targets.SetItemTarget(null);

                    m_CastItem = null;
                    m_castItemGUID.Clear();
                    m_castItemEntry = 0;
                    m_castItemLevel = -1;

                    player.StoreItem(dest, pNewItem, true);
                    player.SendNewItem(pNewItem, 1, true, false);
                    player.ItemAddedQuestCheck(newitemid, 1);
                    return;
                }
            }
            else if (Player.IsBankPos(pos))
            {
                List<ItemPosCount> dest = new List<ItemPosCount>();
                InventoryResult msg = player.CanBankItem(m_CastItem.GetBagSlot(), m_CastItem.GetSlot(), dest, pNewItem, true);
                if (msg == InventoryResult.Ok)
                {
                    player.DestroyItem(m_CastItem.GetBagSlot(), m_CastItem.GetSlot(), true);

                    // prevent crash at access and unexpected charges counting with item update queue corrupt
                    if (m_CastItem == m_targets.GetItemTarget())
                        m_targets.SetItemTarget(null);

                    m_CastItem = null;
                    m_castItemGUID.Clear();
                    m_castItemEntry = 0;
                    m_castItemLevel = -1;

                    player.BankItem(dest, pNewItem, true);
                    return;
                }
            }
            else if (Player.IsEquipmentPos(pos))
            {
                ushort dest;

                player.DestroyItem(m_CastItem.GetBagSlot(), m_CastItem.GetSlot(), true);

                InventoryResult msg = player.CanEquipItem(m_CastItem.GetSlot(), out dest, pNewItem, true);

                if (msg == InventoryResult.Ok || msg == InventoryResult.ClientLockedOut)
                {
                    if (msg == InventoryResult.ClientLockedOut)
                        dest = EquipmentSlot.MainHand;

                    // prevent crash at access and unexpected charges counting with item update queue corrupt
                    if (m_CastItem == m_targets.GetItemTarget())
                        m_targets.SetItemTarget(null);

                    m_CastItem = null;
                    m_castItemGUID.Clear();
                    m_castItemEntry = 0;
                    m_castItemLevel = -1;

                    player.EquipItem(dest, pNewItem, true);
                    player.AutoUnequipOffhandIfNeed();
                    player.SendNewItem(pNewItem, 1, true, false);
                    player.ItemAddedQuestCheck(newitemid, 1);
                    return;
                }
            }
        }

        [SpellEffectHandler(SpellEffectName.Proficiency)]
        void EffectProficiency(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (!m_caster.IsTypeId(TypeId.Player))
                return;
            Player p_target = m_caster.ToPlayer();

            uint subClassMask = (uint)m_spellInfo.EquippedItemSubClassMask;
            if (m_spellInfo.EquippedItemClass == ItemClass.Weapon && !Convert.ToBoolean(p_target.GetWeaponProficiency() & subClassMask))
            {
                p_target.AddWeaponProficiency(subClassMask);
                p_target.SendProficiency(ItemClass.Weapon, p_target.GetWeaponProficiency());
            }
            if (m_spellInfo.EquippedItemClass == ItemClass.Armor && !Convert.ToBoolean(p_target.GetArmorProficiency() & subClassMask))
            {
                p_target.AddArmorProficiency(subClassMask);
                p_target.SendProficiency(ItemClass.Armor, p_target.GetArmorProficiency());
            }
        }

        [SpellEffectHandler(SpellEffectName.Summon)]
        void EffectSummonType(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            uint entry = (uint)effectInfo.MiscValue;
            if (entry == 0)
                return;

            SummonPropertiesRecord properties = CliDB.SummonPropertiesStorage.LookupByKey(effectInfo.MiscValueB);
            if (properties == null)
            {
                Log.outError(LogFilter.Spells, "EffectSummonType: Unhandled summon type {0}", effectInfo.MiscValueB);
                return;
            }

            if (m_originalCaster == null)
                return;

            bool personalSpawn = (properties.Flags & SummonPropFlags.PersonalSpawn) != 0;

            int duration = m_spellInfo.CalcDuration(m_originalCaster);

            TempSummon summon = null;

            // determine how many units should be summoned
            uint numSummons;

            // some spells need to summon many units, for those spells number of summons is stored in effect value
            // however so far noone found a generic check to find all of those (there's no related data in summonproperties.dbc
            // and in spell attributes, possibly we need to add a table for those)
            // so here's a list of MiscValueB values, which is currently most generic check
            switch (effectInfo.MiscValueB)
            {
                case 64:
                case 61:
                case 1101:
                case 66:
                case 648:
                case 2301:
                case 1061:
                case 1261:
                case 629:
                case 181:
                case 715:
                case 1562:
                case 833:
                case 1161:
                case 713:
                    numSummons = (uint)(damage > 0 ? damage : 1);
                    break;
                default:
                    numSummons = 1;
                    break;
            }

            switch (properties.Control)
            {
                case SummonCategory.Wild:
                case SummonCategory.Ally:
                case SummonCategory.Unk:
                    if (Convert.ToBoolean(properties.Flags & SummonPropFlags.Unk10))
                    {
                        SummonGuardian(effIndex, entry, properties, numSummons);
                        break;
                    }
                    switch (properties.Title)
                    {
                        case SummonType.Pet:
                        case SummonType.Guardian:
                        case SummonType.Guardian2:
                        case SummonType.Minion:
                            SummonGuardian(effIndex, entry, properties, numSummons);
                            break;
                        // Summons a vehicle, but doesn't force anyone to enter it (see SUMMON_CATEGORY_VEHICLE)
                        case SummonType.Vehicle:
                        case SummonType.Vehicle2:
                            summon = m_caster.GetMap().SummonCreature(entry, destTarget, properties, (uint)duration, m_originalCaster, m_spellInfo.Id);
                            break;
                        case SummonType.LightWell:
                        case SummonType.Totem:
                            {
                                summon = m_caster.GetMap().SummonCreature(entry, destTarget, properties, (uint)duration, m_originalCaster, m_spellInfo.Id, 0, personalSpawn);
                                if (summon == null || !summon.IsTotem())
                                    return;

                                if (damage != 0)                                            // if not spell info, DB values used
                                {
                                    summon.SetMaxHealth((uint)damage);
                                    summon.SetHealth((uint)damage);
                                }
                                break;
                            }
                        case SummonType.Minipet:
                            {
                                summon = m_caster.GetMap().SummonCreature(entry, destTarget, properties, (uint)duration, m_originalCaster, m_spellInfo.Id, 0, personalSpawn);
                                if (summon == null || !summon.HasUnitTypeMask(UnitTypeMask.Minion))
                                    return;

                                summon.SelectLevel();       // some summoned creaters have different from 1 DB data for level/hp
                                summon.SetUInt64Value(UnitFields.NpcFlags, (ulong)summon.GetCreatureTemplate().Npcflag);

                                summon.SetFlag(UnitFields.Flags, UnitFlags.ImmuneToPc | UnitFlags.ImmuneToNpc);

                                summon.GetAI().EnterEvadeMode();
                                break;
                            }
                        default:
                            {
                                float radius = effectInfo.CalcRadius();

                                TempSummonType summonType = (duration == 0) ? TempSummonType.DeadDespawn : TempSummonType.TimedDespawn;

                                for (uint count = 0; count < numSummons; ++count)
                                {
                                    Position pos = new Position();
                                    if (count == 0)
                                        pos = destTarget;
                                    else
                                        // randomize position for multiple summons
                                        m_caster.GetRandomPoint(destTarget, radius, out pos);

                                    summon = m_originalCaster.SummonCreature(entry, pos, summonType, (uint)duration, 0, personalSpawn);
                                    if (summon == null)
                                        continue;

                                    if (properties.Control == SummonCategory.Ally)
                                    {
                                        summon.SetOwnerGUID(m_originalCaster.GetGUID());
                                        summon.SetFaction(m_originalCaster.getFaction());
                                        summon.SetUInt32Value(UnitFields.CreatedBySpell, m_spellInfo.Id);
                                    }

                                    ExecuteLogEffectSummonObject(effIndex, summon);
                                }
                                return;
                            }
                    }//switch
                    break;
                case SummonCategory.Pet:
                    SummonGuardian(effIndex, entry, properties, numSummons);
                    break;
                case SummonCategory.Puppet:
                    summon = m_caster.GetMap().SummonCreature(entry, destTarget, properties, (uint)duration, m_originalCaster, m_spellInfo.Id, 0, personalSpawn);
                    break;
                case SummonCategory.Vehicle:
                    // Summoning spells (usually triggered by npc_spellclick) that spawn a vehicle and that cause the clicker
                    // to cast a ride vehicle spell on the summoned unit.
                    summon = m_originalCaster.GetMap().SummonCreature(entry, destTarget, properties, (uint)duration, m_caster, m_spellInfo.Id);
                    if (summon == null || !summon.IsVehicle())
                        return;

                    // The spell that this effect will trigger. It has SPELL_AURA_CONTROL_VEHICLE
                    uint spellId = SharedConst.VehicleSpellRideHardcoded;
                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo((uint)effectInfo.CalcValue());
                    if (spellInfo != null && spellInfo.HasAura(m_originalCaster.GetMap().GetDifficultyID(), AuraType.ControlVehicle))
                        spellId = spellInfo.Id;

                    // Hard coded enter vehicle spell
                    m_originalCaster.CastSpell(summon, spellId, true);

                    uint faction = properties.Faction;
                    if (faction == 0)
                        faction = m_originalCaster.getFaction();

                    summon.SetFaction(faction);
                    break;
            }

            if (summon != null)
            {
                summon.SetCreatorGUID(m_originalCaster.GetGUID());
                ExecuteLogEffectSummonObject(effIndex, summon);
            }
        }

        [SpellEffectHandler(SpellEffectName.LearnSpell)]
        void EffectLearnSpell(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            if (!unitTarget.IsTypeId(TypeId.Player))
            {
                if (unitTarget.IsPet())
                    EffectLearnPetSpell(effIndex);
                return;
            }

            Player player = unitTarget.ToPlayer();

            uint spellToLearn = (m_spellInfo.Id == 483 || m_spellInfo.Id == 55884) ? (uint)damage : effectInfo.TriggerSpell;
            player.LearnSpell(spellToLearn, false);

            Log.outDebug(LogFilter.Spells, "Spell: Player {0} has learned spell {1} from NpcGUID={2}", player.GetGUID().ToString(), spellToLearn, m_caster.GetGUID().ToString());
        }

        [SpellEffectHandler(SpellEffectName.Dispel)]
        void EffectDispel(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            // Create dispel mask by dispel type
            uint dispel_type = (uint)effectInfo.MiscValue;
            uint dispelMask = SpellInfo.GetDispelMask((DispelType)dispel_type);

            List<DispelableAura> dispelList = unitTarget.GetDispellableAuraList(m_caster, dispelMask, targetMissInfo == SpellMissInfo.Reflect);
            if (dispelList.Empty())
                return;

            int remaining = dispelList.Count;

            // Ok if exist some buffs for dispel try dispel it
            List<DispelableAura> successList = new List<DispelableAura>();

            DispelFailed dispelFailed = new DispelFailed();
            dispelFailed.CasterGUID = m_caster.GetGUID();
            dispelFailed.VictimGUID = unitTarget.GetGUID();
            dispelFailed.SpellID = m_spellInfo.Id;

            // dispel N = damage buffs (or while exist buffs for dispel)
            for (int count = 0; count < damage && remaining > 0;)
            {
                // Random select buff for dispel
                var dispelableAura = dispelList[RandomHelper.IRand(0, remaining - 1)];

                if (dispelableAura.RollDispel())
                {
                    var successItr = successList.Find(dispelAura =>
                    {
                        if (dispelAura.GetAura().GetId() == dispelableAura.GetAura().GetId())
                            return true;

                        return false;
                    });

                    if (successItr == null)
                        successList.Add(new DispelableAura(dispelableAura.GetAura(), 0, 1));
                    else
                        successItr.IncrementCharges();

                    if (!dispelableAura.DecrementCharge())
                    {
                        --remaining;
                        dispelList[remaining] = dispelableAura;
                    }
                }
                else
                {
                    dispelFailed.FailedSpells.Add(dispelableAura.GetAura().GetId());
                }
                ++count;
            }

            if (!dispelFailed.FailedSpells.Empty())
                m_caster.SendMessageToSet(dispelFailed, true);

            if (successList.Empty())
                return;

            SpellDispellLog spellDispellLog = new SpellDispellLog();
            spellDispellLog.IsBreak = false; // TODO: use me
            spellDispellLog.IsSteal = false;

            spellDispellLog.TargetGUID = unitTarget.GetGUID();
            spellDispellLog.CasterGUID = m_caster.GetGUID();
            spellDispellLog.DispelledBySpellID = m_spellInfo.Id;

            foreach (var dispelableAura in successList)
            {
                var dispellData = new SpellDispellData();
                dispellData.SpellID = dispelableAura.GetAura().GetId();
                dispellData.Harmful = false;      // TODO: use me
                //dispellData.Rolled = none; // TODO: use me
                //dispellData.Needed = none; // TODO: use me

                unitTarget.RemoveAurasDueToSpellByDispel(dispelableAura.GetAura().GetId(), m_spellInfo.Id, dispelableAura.GetAura().GetCasterGUID(), m_caster, dispelableAura.GetDispelCharges());

                spellDispellLog.DispellData.Add(dispellData);
            }
            m_caster.SendMessageToSet(spellDispellLog, true);

            CallScriptSuccessfulDispel(effIndex);
        }

        [SpellEffectHandler(SpellEffectName.DualWield)]
        void EffectDualWield(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            unitTarget.SetCanDualWield(true);
            if (unitTarget.IsTypeId(TypeId.Unit))
                unitTarget.ToCreature().UpdateDamagePhysical(WeaponAttackType.OffAttack);
        }

        [SpellEffectHandler(SpellEffectName.Pull)]
        void EffectPull(uint effIndex)
        {
            // @todo create a proper pull towards distract spell center for distract
            EffectUnused(effIndex);
        }

        [SpellEffectHandler(SpellEffectName.Distract)]
        void EffectDistract(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            // Check for possible target
            if (unitTarget == null || unitTarget.IsInCombat())
                return;

            // target must be OK to do this
            if (unitTarget.HasUnitState(UnitState.Confused | UnitState.Stunned | UnitState.Fleeing))
                return;

            unitTarget.SetFacingTo(unitTarget.GetAngle(destTarget));
            unitTarget.ClearUnitState(UnitState.Moving);

            if (unitTarget.IsTypeId(TypeId.Unit))
                unitTarget.GetMotionMaster().MoveDistract((uint)(damage * Time.InMilliseconds));
        }

        [SpellEffectHandler(SpellEffectName.Pickpocket)]
        void EffectPickPocket(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!m_caster.IsTypeId(TypeId.Player))
                return;

            // victim must be creature and attackable
            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Unit) || m_caster.IsFriendlyTo(unitTarget))
                return;

            // victim have to be alive and humanoid or undead
            if (unitTarget.IsAlive() && (unitTarget.GetCreatureTypeMask() & (uint)CreatureType.MaskHumanoidOrUndead) != 0)
                m_caster.ToPlayer().SendLoot(unitTarget.GetGUID(), LootType.Pickpocketing);
        }

        [SpellEffectHandler(SpellEffectName.AddFarsight)]
        void EffectAddFarsight(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (!m_caster.IsTypeId(TypeId.Player))
                return;

            float radius = effectInfo.CalcRadius();
            int duration = m_spellInfo.CalcDuration(m_caster);
            // Caster not in world, might be spell triggered from aura removal
            if (!m_caster.IsInWorld)
                return;

            DynamicObject dynObj = new DynamicObject(true);
            if (!dynObj.CreateDynamicObject(m_caster.GetMap().GenerateLowGuid(HighGuid.DynamicObject), m_caster, m_spellInfo, destTarget, radius, DynamicObjectType.FarsightFocus, m_SpellVisual))
                return;

            dynObj.SetDuration(duration);
            dynObj.SetCasterViewpoint();
        }

        [SpellEffectHandler(SpellEffectName.UntrainTalents)]
        void EffectUntrainTalents(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || m_caster.IsTypeId(TypeId.Player))
                return;

            ObjectGuid guid = m_caster.GetGUID();
            if (!guid.IsEmpty()) // the trainer is the caster
                unitTarget.ToPlayer().SendRespecWipeConfirm(guid, unitTarget.ToPlayer().GetNextResetTalentsCost());
        }

        [SpellEffectHandler(SpellEffectName.TeleportUnitsFaceCaster)]
        void EffectTeleUnitsFaceCaster(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            if (unitTarget.IsInFlight())
                return;

            float dis = effectInfo.CalcRadius(m_caster);

            float fx, fy, fz;
            m_caster.GetClosePoint(out fx, out fy, out fz, unitTarget.GetObjectSize(), dis);

            unitTarget.NearTeleportTo(fx, fy, fz, -m_caster.GetOrientation(), unitTarget == m_caster);
        }

        [SpellEffectHandler(SpellEffectName.SkillStep)]
        void EffectLearnSkill(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!unitTarget.IsTypeId(TypeId.Player))
                return;

            if (damage < 1)
                return;

            uint skillid = (uint)effectInfo.MiscValue;
            SkillRaceClassInfoRecord rcEntry = Global.DB2Mgr.GetSkillRaceClassInfo(skillid, unitTarget.GetRace(), unitTarget.GetClass());
            if (rcEntry == null)
                return;

            SkillTiersEntry tier = Global.ObjectMgr.GetSkillTier(rcEntry.SkillTierID);
            if (tier == null)
                return;
            ushort skillval = unitTarget.ToPlayer().GetPureSkillValue((SkillType)skillid);
            unitTarget.ToPlayer().SetSkill(skillid, (uint)damage, Math.Max(skillval, (ushort)1), tier.Value[damage - 1]);
        }

        [SpellEffectHandler(SpellEffectName.PlayMovie)]
        void EffectPlayMovie(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!unitTarget.IsTypeId(TypeId.Player))
                return;

            uint movieId = (uint)effectInfo.MiscValue;
            if (!CliDB.MovieStorage.ContainsKey(movieId))
                return;

            unitTarget.ToPlayer().SendMovieStart(movieId);
        }

        [SpellEffectHandler(SpellEffectName.TradeSkill)]
        void EffectTradeSkill(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (!m_caster.IsTypeId(TypeId.Player))
                return;
            // uint skillid =  GetEffect(i].MiscValue;
            // ushort skillmax = unitTarget.ToPlayer().(skillid);
            // m_caster.ToPlayer().SetSkill(skillid, skillval?skillval:1, skillmax+75);
        }

        [SpellEffectHandler(SpellEffectName.EnchantItem)]
        void EffectEnchantItemPerm(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (itemTarget == null)
                return;

            Player player = m_caster.ToPlayer();
            if (player == null)
                return;

            // Handle vellums
            if (itemTarget.IsVellum())
            {
                // destroy one vellum from stack
                uint count = 1;
                player.DestroyItemCount(itemTarget, ref count, true);
                unitTarget = player;
                // and add a scroll
                DoCreateItem(effIndex, effectInfo.ItemType);
                itemTarget = null;
                m_targets.SetItemTarget(null);
            }
            else
            {
                // do not increase skill if vellum used
                if (!(m_CastItem && m_CastItem.GetTemplate().GetFlags().HasAnyFlag(ItemFlags.NoReagentCost)))
                    player.UpdateCraftSkill(m_spellInfo.Id);

                uint enchant_id = (uint)effectInfo.MiscValue;
                if (enchant_id == 0)
                    return;

                SpellItemEnchantmentRecord pEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);
                if (pEnchant == null)
                    return;

                // item can be in trade slot and have owner diff. from caster
                Player item_owner = itemTarget.GetOwner();
                if (item_owner == null)
                    return;

                if (item_owner != player && player.GetSession().HasPermission(RBACPermissions.LogGmTrade))
                {
                    Log.outCommand(player.GetSession().GetAccountId(), "GM {0} (Account: {1}) enchanting(perm): {2} (Entry: {3}) for player: {4} (Account: {5})",
                        player.GetName(), player.GetSession().GetAccountId(), itemTarget.GetTemplate().GetName(), itemTarget.GetEntry(), item_owner.GetName(), item_owner.GetSession().GetAccountId());
                }

                // remove old enchanting before applying new if equipped
                item_owner.ApplyEnchantment(itemTarget, EnchantmentSlot.Perm, false);

                itemTarget.SetEnchantment(EnchantmentSlot.Perm, enchant_id, 0, 0, m_caster.GetGUID());

                // add new enchanting if equipped
                item_owner.ApplyEnchantment(itemTarget, EnchantmentSlot.Perm, true);

                item_owner.RemoveTradeableItem(itemTarget);
                itemTarget.ClearSoulboundTradeable(item_owner);
            }
        }

        [SpellEffectHandler(SpellEffectName.EnchantItemPrismatic)]
        void EffectEnchantItemPrismatic(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (itemTarget == null)
                return;

            Player player = m_caster.ToPlayer();
            if (player == null)
                return;

            uint enchantId = (uint)effectInfo.MiscValue;
            if (enchantId == 0)
                return;

            SpellItemEnchantmentRecord enchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchantId);
            if (enchant == null)
                return;

            // support only enchantings with add socket in this slot
            {
                bool add_socket = false;
                for (byte i = 0; i < ItemConst.MaxItemEnchantmentEffects; ++i)
                {
                    if (enchant.Effect[i] == ItemEnchantmentType.PrismaticSocket)
                    {
                        add_socket = true;
                        break;
                    }
                }
                if (!add_socket)
                {
                    Log.outError(LogFilter.Spells, "Spell.EffectEnchantItemPrismatic: attempt apply enchant spell {0} with SPELL_EFFECT_ENCHANT_ITEM_PRISMATIC ({1}) but without ITEM_ENCHANTMENT_TYPE_PRISMATIC_SOCKET ({2}), not suppoted yet.",
                        m_spellInfo.Id, SpellEffectName.EnchantItemPrismatic, ItemEnchantmentType.PrismaticSocket);
                    return;
                }
            }

            // item can be in trade slot and have owner diff. from caster
            Player item_owner = itemTarget.GetOwner();
            if (item_owner == null)
                return;

            if (item_owner != player && player.GetSession().HasPermission(RBACPermissions.LogGmTrade))
            {
                Log.outCommand(player.GetSession().GetAccountId(), "GM {0} (Account: {1}) enchanting(perm): {2} (Entry: {3}) for player: {4} (Account: {5})",
                    player.GetName(), player.GetSession().GetAccountId(), itemTarget.GetTemplate().GetName(), itemTarget.GetEntry(), item_owner.GetName(), item_owner.GetSession().GetAccountId());
            }

            // remove old enchanting before applying new if equipped
            item_owner.ApplyEnchantment(itemTarget, EnchantmentSlot.Prismatic, false);

            itemTarget.SetEnchantment(EnchantmentSlot.Prismatic, enchantId, 0, 0, m_caster.GetGUID());

            // add new enchanting if equipped
            item_owner.ApplyEnchantment(itemTarget, EnchantmentSlot.Prismatic, true);

            item_owner.RemoveTradeableItem(itemTarget);
            itemTarget.ClearSoulboundTradeable(item_owner);
        }

        [SpellEffectHandler(SpellEffectName.EnchantItemTemporary)]
        void EffectEnchantItemTmp(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player player = m_caster.ToPlayer();
            if (player == null)
                return;

            if (itemTarget == null)
                return;

            uint enchant_id = (uint)effectInfo.MiscValue;

            if (enchant_id == 0)
            {
                Log.outError(LogFilter.Spells, "Spell {0} Effect {1} (SPELL_EFFECT_ENCHANT_ITEM_TEMPORARY) have 0 as enchanting id", m_spellInfo.Id, effIndex);
                return;
            }

            SpellItemEnchantmentRecord pEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);
            if (pEnchant == null)
            {
                Log.outError(LogFilter.Spells, "Spell {0} Effect {1} (SPELL_EFFECT_ENCHANT_ITEM_TEMPORARY) have not existed enchanting id {2}", m_spellInfo.Id, effIndex, enchant_id);
                return;
            }

            // select enchantment duration
            uint duration;

            // rogue family enchantments exception by duration
            if (m_spellInfo.Id == 38615)
                duration = 1800;                                    // 30 mins
            // other rogue family enchantments always 1 hour (some have spell damage=0, but some have wrong data in EffBasePoints)
            else if (m_spellInfo.SpellFamilyName == SpellFamilyNames.Rogue)
                duration = 3600;                                    // 1 hour
            // shaman family enchantments
            else if (m_spellInfo.SpellFamilyName == SpellFamilyNames.Shaman)
                duration = 3600;                                    // 30 mins
            // other cases with this SpellVisual already selected
            else if (m_spellInfo.GetSpellVisual() == 215)
                duration = 1800;                                    // 30 mins
            // some fishing pole bonuses except Glow Worm which lasts full hour
            else if (m_spellInfo.GetSpellVisual() == 563 && m_spellInfo.Id != 64401)
                duration = 600;                                     // 10 mins
            else if (m_spellInfo.Id == 29702)
                duration = 300;                                     // 5 mins
            else if (m_spellInfo.Id == 37360)
                duration = 300;                                     // 5 mins
            // default case
            else
                duration = 3600;                                    // 1 hour

            // item can be in trade slot and have owner diff. from caster
            Player item_owner = itemTarget.GetOwner();
            if (item_owner == null)
                return;

            if (item_owner != player && player.GetSession().HasPermission(RBACPermissions.LogGmTrade))
            {
                Log.outCommand(player.GetSession().GetAccountId(), "GM {0} (Account: {1}) enchanting(temp): {2} (Entry: {3}) for player: {4} (Account: {5})",
                    player.GetName(), player.GetSession().GetAccountId(), itemTarget.GetTemplate().GetName(), itemTarget.GetEntry(), item_owner.GetName(), item_owner.GetSession().GetAccountId());
            }

            // remove old enchanting before applying new if equipped
            item_owner.ApplyEnchantment(itemTarget, EnchantmentSlot.Temp, false);

            itemTarget.SetEnchantment(EnchantmentSlot.Temp, enchant_id, duration * 1000, 0, m_caster.GetGUID());

            // add new enchanting if equipped
            item_owner.ApplyEnchantment(itemTarget, EnchantmentSlot.Temp, true);
        }

        [SpellEffectHandler(SpellEffectName.Tamecreature)]
        void EffectTameCreature(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!m_caster.GetPetGUID().IsEmpty())
                return;

            if (unitTarget == null)
                return;

            if (!unitTarget.IsTypeId(TypeId.Unit))
                return;

            Creature creatureTarget = unitTarget.ToCreature();

            if (creatureTarget.IsPet())
                return;

            if (m_caster.GetClass() != Class.Hunter)
                return;

            // cast finish successfully
            finish();

            Pet pet = m_caster.CreateTamedPetFrom(creatureTarget, m_spellInfo.Id);
            if (pet == null)                                               // in very specific state like near world end/etc.
                return;

            // "kill" original creature
            creatureTarget.DespawnOrUnsummon();

            uint level = (creatureTarget.GetLevelForTarget(m_caster) < (m_caster.GetLevelForTarget(creatureTarget) - 5)) ? (m_caster.GetLevelForTarget(creatureTarget) - 5) : creatureTarget.GetLevelForTarget(m_caster);

            // prepare visual effect for levelup
            pet.SetUInt32Value(UnitFields.Level, level - 1);

            // add to world
            pet.GetMap().AddToMap(pet.ToCreature());

            // visual effect for levelup
            pet.SetUInt32Value(UnitFields.Level, level);

            // caster have pet now
            m_caster.SetMinion(pet, true);

            if (m_caster.IsTypeId(TypeId.Player))
            {
                pet.SavePetToDB(PetSaveMode.AsCurrent);
                m_caster.ToPlayer().PetSpellInitialize();
            }
        }

        [SpellEffectHandler(SpellEffectName.SummonPet)]
        void EffectSummonPet(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Player owner = null;
            if (m_originalCaster != null)
            {
                owner = m_originalCaster.ToPlayer();
                if (owner == null && m_originalCaster.IsTotem())
                    owner = m_originalCaster.GetCharmerOrOwnerPlayerOrPlayerItself();
            }

            uint petentry = (uint)effectInfo.MiscValue;

            if (owner == null)
            {
                SummonPropertiesRecord properties = CliDB.SummonPropertiesStorage.LookupByKey(67);
                if (properties != null)
                    SummonGuardian(effIndex, petentry, properties, 1);
                return;
            }

            Pet OldSummon = owner.GetPet();

            // if pet requested type already exist
            if (OldSummon != null)
            {
                if (petentry == 0 || OldSummon.GetEntry() == petentry)
                {
                    // pet in corpse state can't be summoned
                    if (OldSummon.IsDead())
                        return;

                    Cypher.Assert(OldSummon.GetMap() == owner.GetMap());

                    float px, py, pz;
                    owner.GetClosePoint(out px, out py, out pz, OldSummon.GetObjectSize());

                    OldSummon.NearTeleportTo(px, py, pz, OldSummon.GetOrientation());

                    if (owner.IsTypeId(TypeId.Player) && OldSummon.isControlled())
                        owner.ToPlayer().PetSpellInitialize();

                    return;
                }

                if (owner.IsTypeId(TypeId.Player))
                    owner.ToPlayer().RemovePet(OldSummon, (OldSummon.getPetType() == PetType.Hunter ? PetSaveMode.AsDeleted : PetSaveMode.NotInSlot), false);
                else
                    return;
            }

            float x, y, z;
            owner.GetClosePoint(out x, out y, out z, owner.GetObjectSize());
            Pet pet = owner.SummonPet(petentry, x, y, z, owner.Orientation, PetType.Summon, 0);
            if (!pet)
                return;

            if (m_caster.IsTypeId(TypeId.Unit))
            {
                if (m_caster.IsTotem())
                    pet.SetReactState(ReactStates.Aggressive);
                else
                    pet.SetReactState(ReactStates.Defensive);
            }

            pet.SetUInt32Value(UnitFields.CreatedBySpell, m_spellInfo.Id);

            // generate new name for summon pet
            string new_name = Global.ObjectMgr.GeneratePetName(petentry);
            if (!string.IsNullOrEmpty(new_name))
                pet.SetName(new_name);

            ExecuteLogEffectSummonObject(effIndex, pet);
        }

        [SpellEffectHandler(SpellEffectName.LearnPetSpell)]
        void EffectLearnPetSpell(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            if (unitTarget.ToPlayer() != null)
            {
                EffectLearnSpell(effIndex);
                return;
            }
            Pet pet = unitTarget.ToPet();
            if (pet == null)
                return;

            SpellInfo learn_spellproto = Global.SpellMgr.GetSpellInfo(effectInfo.TriggerSpell);
            if (learn_spellproto == null)
                return;

            pet.learnSpell(learn_spellproto.Id);
            pet.SavePetToDB(PetSaveMode.AsCurrent);
            pet.GetOwner().PetSpellInitialize();
        }

        [SpellEffectHandler(SpellEffectName.AttackMe)]
        void EffectTaunt(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            // this effect use before aura Taunt apply for prevent taunt already attacking target
            // for spell as marked "non effective at already attacking target"
            if (unitTarget == null || !unitTarget.CanHaveThreatList()
                || unitTarget.GetVictim() == m_caster)
            {
                SendCastResult(SpellCastResult.DontReport);
                return;
            }

            if (!unitTarget.GetThreatManager().getOnlineContainer().empty())
            {
                // Also use this effect to set the taunter's threat to the taunted creature's highest value
                float myThreat = unitTarget.GetThreatManager().getThreat(m_caster);
                float topThreat = unitTarget.GetThreatManager().getOnlineContainer().getMostHated().getThreat();
                if (topThreat > myThreat)
                    unitTarget.GetThreatManager().addThreat(m_caster, topThreat - myThreat);

                //Set aggro victim to caster
                HostileReference forcedVictim = unitTarget.GetThreatManager().getOnlineContainer().getReferenceByTarget(m_caster);
                if (forcedVictim != null)
                    unitTarget.GetThreatManager().setCurrentVictim(forcedVictim);
            }

            if (unitTarget.ToCreature().IsAIEnabled && !unitTarget.ToCreature().HasReactState(ReactStates.Passive))
                unitTarget.ToCreature().GetAI().AttackStart(m_caster);
        }

        [SpellEffectHandler(SpellEffectName.WeaponDamageNoschool)]
        [SpellEffectHandler(SpellEffectName.WeaponPercentDamage)]
        [SpellEffectHandler(SpellEffectName.WeaponDamage)]
        [SpellEffectHandler(SpellEffectName.NormalizedWeaponDmg)]
        void EffectWeaponDmg(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.LaunchTarget)
                return;

            if (unitTarget == null || !unitTarget.IsAlive())
                return;

            // multiple weapon dmg effect workaround
            // execute only the last weapon damage
            // and handle all effects at once
            for (var i = effIndex + 1; i < SpellConst.MaxEffects; ++i)
            {
                var effect = GetEffect(i);
                if (effect == null)
                    continue;

                switch (effect.Effect)
                {
                    case SpellEffectName.WeaponDamage:
                    case SpellEffectName.WeaponDamageNoschool:
                    case SpellEffectName.NormalizedWeaponDmg:
                    case SpellEffectName.WeaponPercentDamage:
                        return;     // we must calculate only at last weapon effect
                }
            }

            // some spell specific modifiers
            float totalDamagePercentMod = 1.0f;                    // applied to final bonus+weapon damage
            int fixed_bonus = 0;
            int spell_bonus = 0;                                  // bonus specific for spell

            switch (m_spellInfo.SpellFamilyName)
            {
                case SpellFamilyNames.Warrior:
                    {
                        // Devastate (player ones)
                        if (m_spellInfo.SpellFamilyFlags[1].HasAnyFlag(0x40u))
                        {
                            // Player can apply only 58567 Sunder Armor effect.
                            bool needCast = !unitTarget.HasAura(58567, m_caster.GetGUID());
                            if (needCast)
                                m_caster.CastSpell(unitTarget, 58567, true);

                            Aura aur = unitTarget.GetAura(58567, m_caster.GetGUID());
                            if (aur != null)
                            {
                                int num = (needCast ? 0 : 1);
                                if (num != 0)
                                    aur.ModStackAmount(num);
                                fixed_bonus += (aur.GetStackAmount() - 1) * CalculateDamage(2, unitTarget);
                            }
                        }
                        break;
                    }
                case SpellFamilyNames.Rogue:
                    {
                        // Hemorrhage
                        if (m_spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x2000000u))
                        {
                            if (m_caster.IsTypeId(TypeId.Player))
                                m_caster.ToPlayer().AddComboPoints(1, this);
                            // 50% more damage with daggers
                            if (m_caster.IsTypeId(TypeId.Player))
                            {
                                Item item = m_caster.ToPlayer().GetWeaponForAttack(m_attackType, true);
                                if (item != null)
                                    if (item.GetTemplate().GetSubClass() == (uint)ItemSubClassWeapon.Dagger)
                                        totalDamagePercentMod *= 1.5f;
                            }
                        }
                        break;
                    }
                case SpellFamilyNames.Shaman:
                    {
                        // Skyshatter Harness item set bonus
                        // Stormstrike
                        AuraEffect aurEff = m_caster.IsScriptOverriden(m_spellInfo, 5634);
                        if (aurEff != null)
                            m_caster.CastSpell(m_caster, 38430, true, null, aurEff);
                        break;
                    }
                case SpellFamilyNames.Druid:
                    {
                        // Mangle (Cat): CP
                        if (m_spellInfo.SpellFamilyFlags[1].HasAnyFlag(0x400u))
                        {
                            if (m_caster.IsTypeId(TypeId.Player))
                                m_caster.ToPlayer().AddComboPoints(1, this);
                        }
                        break;
                    }
                case SpellFamilyNames.Hunter:
                    {
                        // Kill Shot - bonus damage from Ranged Attack Power
                        if (m_spellInfo.SpellFamilyFlags[1].HasAnyFlag(0x800000u))
                            spell_bonus += (int)(0.45f * m_caster.GetTotalAttackPowerValue(WeaponAttackType.RangedAttack));
                        break;
                    }
                case SpellFamilyNames.Deathknight:
                    {
                        // Blood Strike
                        if (m_spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x400000u))
                        {
                            SpellEffectInfo effect = GetEffect(2);
                            if (effect != null)
                            {
                                float bonusPct = effect.CalcValue(m_caster) * unitTarget.GetDiseasesByCaster(m_caster.GetGUID()) / 2.0f;
                                // Death Knight T8 Melee 4P Bonus
                                AuraEffect aurEff = m_caster.GetAuraEffect(64736, 0);
                                if (aurEff != null)
                                    MathFunctions.AddPct(ref bonusPct, aurEff.GetAmount());
                                MathFunctions.AddPct(ref totalDamagePercentMod, bonusPct);
                            }
                            break;
                        }
                        break;
                    }
            }

            bool normalized = false;
            float weaponDamagePercentMod = 1.0f;
            foreach (SpellEffectInfo effect in GetEffects())
            {
                if (effect == null)
                    continue;

                switch (effect.Effect)
                {
                    case SpellEffectName.WeaponDamage:
                    case SpellEffectName.WeaponDamageNoschool:
                        fixed_bonus += CalculateDamage(effect.EffectIndex, unitTarget);
                        break;
                    case SpellEffectName.NormalizedWeaponDmg:
                        fixed_bonus += CalculateDamage(effect.EffectIndex, unitTarget);
                        normalized = true;
                        break;
                    case SpellEffectName.WeaponPercentDamage:
                        MathFunctions.ApplyPct(ref weaponDamagePercentMod, CalculateDamage(effect.EffectIndex, unitTarget));
                        break;
                    default:
                        break;                                      // not weapon damage effect, just skip
                }
            }

            // if (addPctMods) { percent mods are added in Unit::CalculateDamage } else { percent mods are added in Unit::MeleeDamageBonusDone }
            // this distinction is neccessary to properly inform the client about his autoattack damage values from Script_UnitDamage
            bool addPctMods = !m_spellInfo.HasAttribute(SpellAttr6.NoDonePctDamageMods) && m_spellSchoolMask.HasAnyFlag(SpellSchoolMask.Normal);
            if (addPctMods)
            {
                UnitMods unitMod;
                switch (m_attackType)
                {
                    default:
                    case WeaponAttackType.BaseAttack:
                        unitMod = UnitMods.DamageMainHand;
                        break;
                    case WeaponAttackType.OffAttack:
                        unitMod = UnitMods.DamageOffHand;
                        break;
                    case WeaponAttackType.RangedAttack:
                        unitMod = UnitMods.DamageRanged;
                        break;
                }

                float weapon_total_pct = m_caster.GetModifierValue(unitMod, UnitModifierType.TotalPCT);
                if (fixed_bonus != 0)
                    fixed_bonus = (int)(fixed_bonus * weapon_total_pct);
                if (spell_bonus != 0)
                    spell_bonus = (int)(spell_bonus * weapon_total_pct);
            }

            uint weaponDamage = m_caster.CalculateDamage(m_attackType, normalized, addPctMods);

            // Sequence is important
            foreach (SpellEffectInfo effect in GetEffects())
            {
                if (effect == null)
                    continue;

                // We assume that a spell have at most one fixed_bonus
                // and at most one weaponDamagePercentMod
                switch (effect.Effect)
                {
                    case SpellEffectName.WeaponDamage:
                    case SpellEffectName.WeaponDamageNoschool:
                    case SpellEffectName.NormalizedWeaponDmg:
                        weaponDamage += (uint)fixed_bonus;
                        break;
                    case SpellEffectName.WeaponPercentDamage:
                        weaponDamage = (uint)(weaponDamage * weaponDamagePercentMod);
                        break;
                    default:
                        break;                                      // not weapon damage effect, just skip
                }
            }

            weaponDamage += (uint)spell_bonus;
            weaponDamage = (uint)(weaponDamage * totalDamagePercentMod);

            // prevent negative damage
            uint eff_damage = Math.Max(weaponDamage, 0);

            // Add melee damage bonuses (also check for negative)
            uint damage = m_caster.MeleeDamageBonusDone(unitTarget, eff_damage, m_attackType, m_spellInfo);

            m_damage += (int)unitTarget.MeleeDamageBonusTaken(m_caster, damage, m_attackType, m_spellInfo);
        }

        [SpellEffectHandler(SpellEffectName.Threat)]
        void EffectThreat(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsAlive() || !m_caster.IsAlive())
                return;

            if (!unitTarget.CanHaveThreatList())
                return;

            unitTarget.AddThreat(m_caster, damage);
        }

        [SpellEffectHandler(SpellEffectName.HealMaxHealth)]
        void EffectHealMaxHealth(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsAlive())
                return;

            int addhealth = 0;

            // damage == 0 - heal for caster max health
            if (damage == 0)
                addhealth = (int)m_caster.GetMaxHealth();
            else
                addhealth = (int)(unitTarget.GetMaxHealth() - unitTarget.GetHealth());

            m_healing += addhealth;
        }

        [SpellEffectHandler(SpellEffectName.InterruptCast)]
        void EffectInterruptCast(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.LaunchTarget)
                return;

            if (unitTarget == null || !unitTarget.IsAlive())
                return;

            // @todo not all spells that used this effect apply cooldown at school spells
            // also exist case: apply cooldown to interrupted cast only and to all spells
            // there is no CURRENT_AUTOREPEAT_SPELL spells that can be interrupted
            for (var i = CurrentSpellTypes.Generic; i < CurrentSpellTypes.AutoRepeat; ++i)
            {
                Spell spell = unitTarget.GetCurrentSpell(i);
                if (spell != null)
                {
                    SpellInfo curSpellInfo = spell.m_spellInfo;
                    // check if we can interrupt spell
                    if ((spell.getState() == SpellState.Casting
                        || (spell.getState() == SpellState.Preparing && spell.GetCastTime() > 0.0f))
                        && (curSpellInfo.PreventionType.HasAnyFlag(SpellPreventionType.Silence))
                        && ((i == CurrentSpellTypes.Generic && curSpellInfo.InterruptFlags.HasAnyFlag(SpellInterruptFlags.Interrupt))
                        || (i == CurrentSpellTypes.Channeled && curSpellInfo.HasChannelInterruptFlag(SpellChannelInterruptFlags.Interrupt))))
                    {
                        if (m_originalCaster != null)
                        {
                            int duration = m_spellInfo.GetDuration();
                            unitTarget.GetSpellHistory().LockSpellSchool(curSpellInfo.GetSchoolMask(), (uint)unitTarget.ModSpellDuration(m_spellInfo, unitTarget, duration, false, (uint)(1 << (int)effIndex)));
                            m_originalCaster.ProcSkillsAndAuras(unitTarget, ProcFlags.DoneSpellMagicDmgClassNeg, ProcFlags.TakenSpellMagicDmgClassNeg, ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.Hit, ProcFlagsHit.Interrupt, null, null, null);
                        }
                        ExecuteLogEffectInterruptCast(effIndex, unitTarget, curSpellInfo.Id);
                        unitTarget.InterruptSpell(i, false);
                    }
                }
            }
        }

        [SpellEffectHandler(SpellEffectName.SummonObjectWild)]
        void EffectSummonObjectWild(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            WorldObject target = focusObject;
            if (target == null)
                target = m_caster;

            float x, y, z;
            if (m_targets.HasDst())
                destTarget.GetPosition(out x, out y, out z);
            else
                m_caster.GetClosePoint(out x, out y, out z, SharedConst.DefaultWorldObjectSize);

            Map map = target.GetMap();

            Position pos = new Position(x, y, z, target.GetOrientation());
            Quaternion rotation = Quaternion.fromEulerAnglesZYX(target.GetOrientation(), 0.0f, 0.0f);
            GameObject go = GameObject.CreateGameObject((uint)effectInfo.MiscValue, map, pos, rotation, 255, GameObjectState.Ready);
            if (!go)
                return;

            PhasingHandler.InheritPhaseShift(go, m_caster);

            int duration = m_spellInfo.CalcDuration(m_caster);

            go.SetRespawnTime(duration > 0 ? duration / Time.InMilliseconds : 0);
            go.SetSpellId(m_spellInfo.Id);

            ExecuteLogEffectSummonObject(effIndex, go);

            // Wild object not have owner and check clickable by players
            map.AddToMap(go);

            if (go.GetGoType() == GameObjectTypes.FlagDrop)
            {
                Player player = m_caster.ToPlayer();
                if (player != null)
                {
                    Battleground bg = player.GetBattleground();
                    if (bg)
                        bg.SetDroppedFlagGUID(go.GetGUID(), (player.GetTeam() == Team.Alliance ? TeamId.Horde : TeamId.Alliance));
                }
            }

            GameObject linkedTrap = go.GetLinkedTrap();
            if (linkedTrap)
            {
                PhasingHandler.InheritPhaseShift(linkedTrap, m_caster);
                linkedTrap.SetRespawnTime(duration > 0 ? duration / Time.InMilliseconds : 0);
                linkedTrap.SetSpellId(m_spellInfo.Id);

                ExecuteLogEffectSummonObject(effIndex, linkedTrap);
            }
        }

        [SpellEffectHandler(SpellEffectName.ScriptEffect)]
        void EffectScriptEffect(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            // @todo we must implement hunter pet summon at login there (spell 6962)

            switch (m_spellInfo.SpellFamilyName)
            {
                case SpellFamilyNames.Generic:
                    {
                        switch (m_spellInfo.Id)
                        {
                            case 45204: // Clone Me!
                                m_caster.CastSpell(unitTarget, (uint)damage, true);
                                break;
                            case 55693:                                 // Remove Collapsing Cave Aura
                                if (unitTarget == null)
                                    return;
                                unitTarget.RemoveAurasDueToSpell((uint)effectInfo.CalcValue());
                                break;
                            // Bending Shinbone
                            case 8856:
                                {
                                    if (itemTarget == null && !m_caster.IsTypeId(TypeId.Player))
                                        return;

                                    uint spell_id = RandomHelper.Rand32(20) != 0 ? 8854u : 8855u;

                                    m_caster.CastSpell(m_caster, spell_id, true, null);
                                    return;
                                }
                            // Brittle Armor - need remove one 24575 Brittle Armor aura
                            case 24590:
                                unitTarget.RemoveAuraFromStack(24575);
                                return;
                            // Mercurial Shield - need remove one 26464 Mercurial Shield aura
                            case 26465:
                                unitTarget.RemoveAuraFromStack(26464);
                                return;
                            // Shadow Flame (All script effects, not just end ones to prevent player from dodging the last triggered spell)
                            case 22539:
                            case 22972:
                            case 22975:
                            case 22976:
                            case 22977:
                            case 22978:
                            case 22979:
                            case 22980:
                            case 22981:
                            case 22982:
                            case 22983:
                            case 22984:
                            case 22985:
                                {
                                    if (unitTarget == null || !unitTarget.IsAlive())
                                        return;

                                    // Onyxia Scale Cloak
                                    if (unitTarget.HasAura(22683))
                                        return;

                                    // Shadow Flame
                                    m_caster.CastSpell(unitTarget, 22682, true);
                                    return;
                                }
                            // Mirren's Drinking Hat
                            case 29830:
                                {
                                    uint item = 0;
                                    switch (RandomHelper.IRand(1, 6))
                                    {
                                        case 1:
                                        case 2:
                                        case 3:
                                            item = 23584;
                                            break;            // Loch Modan Lager
                                        case 4:
                                        case 5:
                                            item = 23585;
                                            break;            // Stouthammer Lite
                                        case 6:
                                            item = 23586;
                                            break;            // Aerie Peak Pale Ale
                                    }
                                    if (item != 0)
                                        DoCreateItem(effIndex, item);
                                    break;
                                }
                            case 20589: // Escape artist
                            case 30918: // Improved Sprint
                                {
                                    // Removes snares and roots.
                                    unitTarget.RemoveMovementImpairingAuras();
                                    break;
                                }
                            // Plant Warmaul Ogre Banner
                            case 32307:
                                Player caster = m_caster.ToPlayer();
                                if (caster != null)
                                {
                                    caster.RewardPlayerAndGroupAtEvent(18388, unitTarget);
                                    Creature target = unitTarget.ToCreature();
                                    if (target != null)
                                    {
                                        target.setDeathState(DeathState.Corpse);
                                        target.RemoveCorpse();
                                    }
                                }
                                break;
                            // Mug Transformation
                            case 41931:
                                {
                                    if (!m_caster.IsTypeId(TypeId.Player))
                                        return;

                                    byte bag = 19;
                                    byte slot = 0;
                                    Item item = null;

                                    while (bag != 0) // 256 = 0 due to var type
                                    {
                                        item = m_caster.ToPlayer().GetItemByPos(bag, slot);
                                        if (item != null && item.GetEntry() == 38587)
                                            break;

                                        ++slot;
                                        if (slot == 39)
                                        {
                                            slot = 0;
                                            ++bag;
                                        }
                                    }
                                    if (bag != 0)
                                    {
                                        if (m_caster.ToPlayer().GetItemByPos(bag, slot).GetCount() == 1) m_caster.ToPlayer().RemoveItem(bag, slot, true);
                                        else m_caster.ToPlayer().GetItemByPos(bag, slot).SetCount(m_caster.ToPlayer().GetItemByPos(bag, slot).GetCount() - 1);
                                        // Spell 42518 (Braufest - Gratisprobe des Braufest herstellen)
                                        m_caster.CastSpell(m_caster, 42518, true);
                                        return;
                                    }
                                    break;
                                }
                            // Brutallus - Burn
                            case 45141:
                            case 45151:
                                {
                                    //Workaround for Range ... should be global for every ScriptEffect
                                    float radius = effectInfo.CalcRadius();
                                    if (unitTarget != null && unitTarget.IsTypeId(TypeId.Player) && unitTarget.GetDistance(m_caster) >= radius && !unitTarget.HasAura(46394) && unitTarget != m_caster)
                                        unitTarget.CastSpell(unitTarget, 46394, true);

                                    break;
                                }
                            // Goblin Weather Machine
                            case 46203:
                                {
                                    if (unitTarget == null)
                                        return;

                                    uint spellId = 0;
                                    switch (RandomHelper.IRand(1, 4))
                                    {
                                        case 0: spellId = 46740; break;
                                        case 1: spellId = 46739; break;
                                        case 2: spellId = 46738; break;
                                        case 3: spellId = 46736; break;
                                    }
                                    unitTarget.CastSpell(unitTarget, spellId, true);
                                    break;
                                }
                            // 5, 000 Gold
                            case 46642:
                                {
                                    if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                                        return;

                                    unitTarget.ToPlayer().ModifyMoney(5000 * MoneyConstants.Gold);

                                    break;
                                }
                            // Death Knight Initiate Visual
                            case 51519:
                                {
                                    if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Unit))
                                        return;

                                    uint iTmpSpellId = 0;
                                    switch (unitTarget.GetDisplayId())
                                    {
                                        case 25369: iTmpSpellId = 51552; break; // bloodelf female
                                        case 25373: iTmpSpellId = 51551; break; // bloodelf male
                                        case 25363: iTmpSpellId = 51542; break; // draenei female
                                        case 25357: iTmpSpellId = 51541; break; // draenei male
                                        case 25361: iTmpSpellId = 51537; break; // dwarf female
                                        case 25356: iTmpSpellId = 51538; break; // dwarf male
                                        case 25372: iTmpSpellId = 51550; break; // forsaken female
                                        case 25367: iTmpSpellId = 51549; break; // forsaken male
                                        case 25362: iTmpSpellId = 51540; break; // gnome female
                                        case 25359: iTmpSpellId = 51539; break; // gnome male
                                        case 25355: iTmpSpellId = 51534; break; // human female
                                        case 25354: iTmpSpellId = 51520; break; // human male
                                        case 25360: iTmpSpellId = 51536; break; // nightelf female
                                        case 25358: iTmpSpellId = 51535; break; // nightelf male
                                        case 25368: iTmpSpellId = 51544; break; // orc female
                                        case 25364: iTmpSpellId = 51543; break; // orc male
                                        case 25371: iTmpSpellId = 51548; break; // tauren female
                                        case 25366: iTmpSpellId = 51547; break; // tauren male
                                        case 25370: iTmpSpellId = 51545; break; // troll female
                                        case 25365: iTmpSpellId = 51546; break; // troll male
                                        default: return;
                                    }

                                    unitTarget.CastSpell(unitTarget, iTmpSpellId, true);
                                    Creature npc = unitTarget.ToCreature();
                                    npc.LoadEquipment();
                                    return;
                                }
                            // Emblazon Runeblade
                            case 51770:
                                {
                                    if (m_originalCaster == null)
                                        return;

                                    m_originalCaster.CastSpell(m_originalCaster, (uint)damage, false);
                                    break;
                                }
                            // Deathbolt from Thalgran Blightbringer
                            // reflected by Freya's Ward
                            // Retribution by Sevenfold Retribution
                            case 51854:
                                {
                                    if (unitTarget == null)
                                        return;
                                    if (unitTarget.HasAura(51845))
                                        unitTarget.CastSpell(m_caster, 51856, true);
                                    else
                                        m_caster.CastSpell(unitTarget, 51855, true);
                                    break;
                                }
                            // Summon Ghouls On Scarlet Crusade
                            case 51904:
                                {
                                    if (!m_targets.HasDst())
                                        return;

                                    float x, y, z;
                                    float radius = effectInfo.CalcRadius();
                                    for (byte i = 0; i < 15; ++i)
                                    {
                                        m_caster.GetRandomPoint(destTarget, radius, out x, out y, out z);
                                        m_caster.CastSpell(x, y, z, 54522, true);
                                    }
                                    break;
                                }
                            case 52173: // Coyote Spirit Despawn
                            case 60243: // Blood Parrot Despawn
                                if (unitTarget.IsTypeId(TypeId.Unit) && unitTarget.ToCreature().IsSummon())
                                    unitTarget.ToTempSummon().UnSummon();
                                return;
                            case 52479: // Gift of the Harvester
                                if (unitTarget != null && m_originalCaster != null)
                                    m_originalCaster.CastSpell(unitTarget, Convert.ToBoolean(RandomHelper.IRand(0, 1)) ? (uint)damage : 52505, true);
                                return;
                            case 53110: // Devour Humanoid
                                if (unitTarget != null)
                                    unitTarget.CastSpell(m_caster, (uint)damage, true);
                                return;
                            case 57347: // Retrieving (Wintergrasp RP-GG pickup spell)
                                {
                                    if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Unit) || !m_caster.IsTypeId(TypeId.Player))
                                        return;

                                    unitTarget.ToCreature().DespawnOrUnsummon();

                                    return;
                                }
                            case 57349: // Drop RP-GG (Wintergrasp RP-GG at death drop spell)
                                {
                                    if (!m_caster.IsTypeId(TypeId.Player))
                                        return;

                                    // Delete item from inventory at death
                                    m_caster.ToPlayer().DestroyItemCount((uint)damage, 5, true);

                                    return;
                                }
                            case 58418:                                 // Portal to Orgrimmar
                            case 58420:                                 // Portal to Stormwind
                                {
                                    if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player) || effIndex != 0)
                                        return;

                                    uint spellID = (uint)GetEffect(0).CalcValue();
                                    uint questID = (uint)GetEffect(1).CalcValue();

                                    if (unitTarget.ToPlayer().GetQuestStatus(questID) == QuestStatus.Complete)
                                        unitTarget.CastSpell(unitTarget, spellID, true);

                                    return;
                                }
                            case 58941:                                 // Rock Shards
                                if (unitTarget != null && m_originalCaster != null)
                                {
                                    for (uint i = 0; i < 3; ++i)
                                    {
                                        m_originalCaster.CastSpell(unitTarget, 58689, true);
                                        m_originalCaster.CastSpell(unitTarget, 58692, true);
                                    }
                                    if (m_originalCaster.GetMap().GetDifficultyID() == Difficulty.None)
                                    {
                                        m_originalCaster.CastSpell(unitTarget, 58695, true);
                                        m_originalCaster.CastSpell(unitTarget, 58696, true);
                                    }
                                    else
                                    {
                                        m_originalCaster.CastSpell(unitTarget, 60883, true);
                                        m_originalCaster.CastSpell(unitTarget, 60884, true);
                                    }
                                }
                                return;
                            case 58983: // Big Blizzard Bear
                                {
                                    if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                                        return;

                                    // Prevent stacking of mounts and client crashes upon dismounting
                                    unitTarget.RemoveAurasByType(AuraType.Mounted);

                                    // Triggered spell id dependent on riding skilll
                                    ushort skillval = unitTarget.ToPlayer().GetSkillValue(SkillType.Riding);
                                    if (skillval != 0)
                                    {
                                        if (skillval >= 150)
                                            unitTarget.CastSpell(unitTarget, 58999, true);
                                        else
                                            unitTarget.CastSpell(unitTarget, 58997, true);
                                    }
                                    return;
                                }
                            case 59317:                                 // Teleporting
                                {

                                    if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                                        return;

                                    // return from top
                                    if (unitTarget.ToPlayer().GetAreaId() == 4637)
                                        unitTarget.CastSpell(unitTarget, 59316, true);
                                    // teleport atop
                                    else
                                        unitTarget.CastSpell(unitTarget, 59314, true);

                                    return;
                                }
                            case 62482: // Grab Crate
                                {
                                    if (unitTarget != null)
                                    {
                                        Unit seat = m_caster.GetVehicleBase();
                                        if (seat != null)
                                        {
                                            Unit parent = seat.GetVehicleBase();
                                            if (parent != null)
                                            {
                                                // @todo a hack, range = 11, should after some time cast, otherwise too far
                                                m_caster.CastSpell(parent, 62496, true);
                                                unitTarget.CastSpell(parent, (uint)GetEffect(0).CalcValue());
                                            }
                                        }
                                    }
                                    return;
                                }
                            case 60123: // Lightwell
                                {
                                    if (!m_caster.IsTypeId(TypeId.Unit) || !m_caster.ToCreature().IsSummon())
                                        return;

                                    uint spell_heal;

                                    switch (m_caster.GetEntry())
                                    {
                                        case 31897: spell_heal = 7001; break;
                                        case 31896: spell_heal = 27873; break;
                                        case 31895: spell_heal = 27874; break;
                                        case 31894: spell_heal = 28276; break;
                                        case 31893: spell_heal = 48084; break;
                                        case 31883: spell_heal = 48085; break;
                                        default:
                                            Log.outError(LogFilter.Spells, "Unknown Lightwell spell caster {0}", m_caster.GetEntry());
                                            return;
                                    }

                                    // proc a spellcast
                                    Aura chargesAura = m_caster.GetAura(59907);
                                    if (chargesAura != null)
                                    {
                                        m_caster.CastSpell(unitTarget, spell_heal, true, null, null, m_caster.ToTempSummon().GetSummonerGUID());
                                        if (chargesAura.ModCharges(-1))
                                            m_caster.ToTempSummon().UnSummon();
                                    }

                                    return;
                                }
                            // Stoneclaw Totem
                            case 55328: // Rank 1
                            case 55329: // Rank 2
                            case 55330: // Rank 3
                            case 55332: // Rank 4
                            case 55333: // Rank 5
                            case 55335: // Rank 6
                            case 55278: // Rank 7
                            case 58589: // Rank 8
                            case 58590: // Rank 9
                            case 58591: // Rank 10
                                {
                                    int basepoints0 = damage;
                                    // Cast Absorb on totems
                                    for (byte slot = (int)SummonSlot.Totem; slot < SharedConst.MaxTotemSlot; ++slot)
                                    {
                                        if (unitTarget.m_SummonSlot[slot].IsEmpty())
                                            continue;

                                        Creature totem = unitTarget.GetMap().GetCreature(unitTarget.m_SummonSlot[slot]);
                                        if (totem != null && totem.IsTotem())
                                        {
                                            m_caster.CastCustomSpell(totem, 55277, basepoints0, 0, 0, true);
                                        }
                                    }
                                    break;
                                }
                            case 45668:                                 // Ultra-Advanced Proto-Typical Shortening Blaster
                                {
                                    if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Unit))
                                        return;

                                    if (RandomHelper.randChance(50))                  // chance unknown, using 50
                                        return;

                                    uint[] spellPlayer = new uint[5]
                                    {
                                        45674,                            // Bigger!
                                        45675,                            // Shrunk
                                        45678,                            // Yellow
                                        45682,                            // Ghost
                                        45684                             // Polymorph
                                    };

                                    uint[] spellTarget = new uint[5]
                                    {
                                        45673,                            // Bigger!
                                        45672,                            // Shrunk
                                        45677,                            // Yellow
                                        45681,                            // Ghost
                                        45683                             // Polymorph
                                    };

                                    m_caster.CastSpell(m_caster, spellPlayer[RandomHelper.IRand(0, 4)], true);
                                    unitTarget.CastSpell(unitTarget, spellTarget[RandomHelper.IRand(0, 4)], true);
                                    break;
                                }
                        }
                        break;
                    }
                case SpellFamilyNames.Paladin:
                    {
                        // Judgement (seal trigger)
                        if (m_spellInfo.GetCategory() == (int)SpellCategories.Judgement)
                        {
                            if (unitTarget == null || !unitTarget.IsAlive())
                                return;
                            uint spellId1 = 0;
                            uint spellId2 = 0;

                            // Judgement self add switch
                            switch (m_spellInfo.Id)
                            {
                                case 53407: spellId1 = 20184; break;    // Judgement of Justice
                                case 20271:                             // Judgement of Light
                                case 57774: spellId1 = 20185; break;    // Judgement of Light
                                case 53408: spellId1 = 20186; break;    // Judgement of Wisdom
                                default:
                                    Log.outError(LogFilter.Spells, "Unsupported Judgement (seal trigger) spell (Id: {0}) in Spell.EffectScriptEffect", m_spellInfo.Id);
                                    return;
                            }
                            // all seals have aura dummy in 2 effect
                            foreach (var app in m_caster.GetAppliedAuras())
                            {
                                Aura aura = app.Value.GetBase();
                                if (aura.GetSpellInfo().GetSpellSpecific() == SpellSpecificType.Seal)
                                {
                                    AuraEffect aureff = aura.GetEffect(2);
                                    if (aureff != null)
                                        if (aureff.GetAuraType() == AuraType.Dummy)
                                        {
                                            if (Global.SpellMgr.GetSpellInfo((uint)aureff.GetAmount()) != null)
                                                spellId2 = (uint)aureff.GetAmount();
                                            break;
                                        }
                                    if (spellId2 == 0)
                                    {
                                        switch (app.Key)
                                        {
                                            // Seal of light, Seal of wisdom, Seal of justice
                                            case 20165:
                                            case 20166:
                                            case 20164:
                                                spellId2 = 54158;
                                                break;
                                        }
                                    }
                                    break;
                                }
                            }
                            if (spellId1 != 0)
                                m_caster.CastSpell(unitTarget, spellId1, true);
                            if (spellId2 != 0)
                                m_caster.CastSpell(unitTarget, spellId2, true);
                            return;
                        }
                        break;
                    }
            }

            // normal DB scripted effect
            Log.outDebug(LogFilter.Spells, "Spell ScriptStart spellid {0} in EffectScriptEffect({1})", m_spellInfo.Id, effIndex);
            m_caster.GetMap().ScriptsStart(ScriptsType.Spell, (uint)((int)m_spellInfo.Id | (int)(effIndex << 24)), m_caster, unitTarget);
        }

        [SpellEffectHandler(SpellEffectName.Sanctuary)]
        [SpellEffectHandler(SpellEffectName.Sanctuary2)]
        void EffectSanctuary(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            unitTarget.getHostileRefManager().UpdateVisibility();

            var attackers = unitTarget.getAttackers();
            foreach (var unit in attackers)
            {
                if (!unit.CanSeeOrDetect(unitTarget))
                    unit.AttackStop();
            }

            unitTarget.m_lastSanctuaryTime = Time.GetMSTime();

            // Vanish allows to remove all threat and cast regular stealth so other spells can be used
            if (m_caster.IsTypeId(TypeId.Player)
                && m_spellInfo.SpellFamilyName == SpellFamilyNames.Rogue
                && m_spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00000800u))
            {
                m_caster.ToPlayer().RemoveAurasByType(AuraType.ModRoot);
                m_caster.ToPlayer().RemoveAurasByType(AuraType.ModRoot2);
                // Overkill
                if (m_caster.ToPlayer().HasSpell(58426))
                    m_caster.CastSpell(m_caster, 58427, true);
            }
        }

        [SpellEffectHandler(SpellEffectName.AddComboPoints)]
        void EffectAddComboPoints(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            if (m_caster.m_playerMovingMe == null)
                return;

            if (damage <= 0)
                return;

            m_caster.m_playerMovingMe.AddComboPoints((sbyte)damage, this);
        }

        [SpellEffectHandler(SpellEffectName.Duel)]
        void EffectDuel(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !m_caster.IsTypeId(TypeId.Player) || !unitTarget.IsTypeId(TypeId.Player))
                return;
            Player caster = m_caster.ToPlayer();
            Player target = unitTarget.ToPlayer();

            // caster or target already have requested duel
            if (caster.duel != null || target.duel != null || target.GetSocial() == null || target.GetSocial().HasIgnore(caster.GetGUID()))
                return;

            // Players can only fight a duel in zones with this flag
            AreaTableRecord casterAreaEntry = CliDB.AreaTableStorage.LookupByKey(caster.GetAreaId());
            if (casterAreaEntry != null && !casterAreaEntry.Flags[0].HasAnyFlag(AreaFlags.AllowDuels))
            {
                SendCastResult(SpellCastResult.NoDueling);            // Dueling isn't allowed here
                return;
            }

            AreaTableRecord targetAreaEntry = CliDB.AreaTableStorage.LookupByKey(target.GetAreaId());
            if (targetAreaEntry != null && !targetAreaEntry.Flags[0].HasAnyFlag(AreaFlags.AllowDuels))
            {
                SendCastResult(SpellCastResult.NoDueling);            // Dueling isn't allowed here
                return;
            }

            //CREATE DUEL FLAG OBJECT
            Map map = m_caster.GetMap();
            Position pos = new Position()
            {
                posX = m_caster.GetPositionX() + (unitTarget.GetPositionX() - m_caster.GetPositionX()) / 2,
                posY = m_caster.GetPositionY() + (unitTarget.GetPositionY() - m_caster.GetPositionY()) / 2,
                posZ = m_caster.GetPositionZ(),
                Orientation = m_caster.GetOrientation()
            };
            Quaternion rotation = Quaternion.fromEulerAnglesZYX(pos.GetOrientation(), 0.0f, 0.0f);

            GameObject go = GameObject.CreateGameObject((uint)effectInfo.MiscValue, map, pos, rotation, 0, GameObjectState.Ready);
            if (!go)
                return;

            PhasingHandler.InheritPhaseShift(go, m_caster);

            go.SetUInt32Value(GameObjectFields.Faction, m_caster.getFaction());
            go.SetUInt32Value(GameObjectFields.Level, m_caster.getLevel() + 1);
            int duration = m_spellInfo.CalcDuration(m_caster);
            go.SetRespawnTime(duration > 0 ? duration / Time.InMilliseconds : 0);
            go.SetSpellId(m_spellInfo.Id);

            ExecuteLogEffectSummonObject(effIndex, go);

            m_caster.AddGameObject(go);
            map.AddToMap(go);
            //END

            // Send request
            DuelRequested packet = new DuelRequested();
            packet.ArbiterGUID = go.GetGUID();
            packet.RequestedByGUID = caster.GetGUID();
            packet.RequestedByWowAccount = caster.GetSession().GetAccountGUID();

            caster.SendPacket(packet);
            target.SendPacket(packet);

            // create duel-info
            DuelInfo duel = new DuelInfo();
            duel.initiator = caster;
            duel.opponent = target;
            duel.startTime = 0;
            duel.startTimer = 0;
            duel.isMounted = (GetSpellInfo().Id == 62875); // Mounted Duel
            caster.duel = duel;

            DuelInfo duel2 = new DuelInfo();
            duel2.initiator = caster;
            duel2.opponent = caster;
            duel2.startTime = 0;
            duel2.startTimer = 0;
            duel2.isMounted = (GetSpellInfo().Id == 62875); // Mounted Duel
            target.duel = duel2;

            caster.SetGuidValue(PlayerFields.DuelArbiter, go.GetGUID());
            target.SetGuidValue(PlayerFields.DuelArbiter, go.GetGUID());

            Global.ScriptMgr.OnPlayerDuelRequest(target, caster);
        }

        [SpellEffectHandler(SpellEffectName.Stuck)]
        void EffectStuck(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (!WorldConfig.GetBoolValue(WorldCfg.CastUnstuck))
                return;

            Player player = m_caster.ToPlayer();
            if (player == null)
                return;

            Log.outDebug(LogFilter.Spells, "Spell Effect: Stuck");
            Log.outInfo(LogFilter.Spells, "Player {0} (guid {1}) used auto-unstuck future at map {2} ({3}, {4}, {5})", player.GetName(), player.GetGUID().ToString(), player.GetMapId(), player.GetPositionX(), player.GetPositionY(), player.GetPositionZ());

            if (player.IsInFlight())
                return;

            // if player is dead without death timer is teleported to graveyard, otherwise not apply the effect
            if (player.IsDead())
            {
                if (player.GetDeathTimer() == 0)
                    player.RepopAtGraveyard();

                return;
            }

            // the player dies if hearthstone is in cooldown, else the player is teleported to home
            if (player.GetSpellHistory().HasCooldown(8690))
            {
                player.KillSelf();
                return;
            }

            player.TeleportTo(player.GetHomebind(), TeleportToOptions.Spell);

            // Stuck spell trigger Hearthstone cooldown
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(8690);
            if (spellInfo == null)
                return;

            Spell spell = new Spell(player, spellInfo, TriggerCastFlags.FullMask);
            spell.SendSpellCooldown();
        }

        [SpellEffectHandler(SpellEffectName.SummonPlayer)]
        void EffectSummonPlayer(uint effIndex)
        {
            // workaround - this effect should not use target map
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            unitTarget.ToPlayer().SendSummonRequestFrom(m_caster);
        }

        [SpellEffectHandler(SpellEffectName.ActivateObject)]
        void EffectActivateObject(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (gameObjTarget == null)
                return;

            ScriptInfo activateCommand = new ScriptInfo();
            activateCommand.command = ScriptCommands.ActivateObject;

            // int unk = effectInfo.MiscValue; // This is set for EffectActivateObject spells; needs research

            gameObjTarget.GetMap().ScriptCommandStart(activateCommand, 0, m_caster, gameObjTarget);
        }

        [SpellEffectHandler(SpellEffectName.ApplyGlyph)]
        void EffectApplyGlyph(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Player player = m_caster.ToPlayer();
            if (player == null)
                return;

            List<uint> glyphs = player.GetGlyphs(player.GetActiveTalentGroup());
            int replacedGlyph = glyphs.Count;
            for (int i = 0; i < glyphs.Count; ++i)
            {
                List<uint> activeGlyphBindableSpells = Global.DB2Mgr.GetGlyphBindableSpells(glyphs[i]);
                if (activeGlyphBindableSpells.Contains(m_misc.SpellId))
                {
                    replacedGlyph = i;
                    player.RemoveAurasDueToSpell(CliDB.GlyphPropertiesStorage.LookupByKey(glyphs[i]).SpellID);
                    break;
                }
            }

            uint glyphId = (uint)effectInfo.MiscValue;
            if (replacedGlyph < glyphs.Count)
            {
                if (glyphId != 0)
                    glyphs[replacedGlyph] = glyphId;
                else
                    glyphs.RemoveAt(replacedGlyph);
            }
            else if (glyphId != 0)
                glyphs.Add(glyphId);

            GlyphPropertiesRecord glyphProperties = CliDB.GlyphPropertiesStorage.LookupByKey(glyphId);
            if (glyphProperties != null)
                player.CastSpell(player, glyphProperties.SpellID, true);

            ActiveGlyphs activeGlyphs = new ActiveGlyphs();
            activeGlyphs.Glyphs.Add(new GlyphBinding(m_misc.SpellId, (ushort)glyphId));
            activeGlyphs.IsFullUpdate = false;
            player.SendPacket(activeGlyphs);
        }

        [SpellEffectHandler(SpellEffectName.EnchantHeldItem)]
        void EffectEnchantHeldItem(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            // this is only item spell effect applied to main-hand weapon of target player (players in area)
            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            Player item_owner = unitTarget.ToPlayer();
            Item item = item_owner.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);

            if (item == null)
                return;

            // must be equipped
            if (!item.IsEquipped())
                return;

            if (effectInfo.MiscValue != 0)
            {
                uint enchant_id = (uint)effectInfo.MiscValue;
                int duration = m_spellInfo.GetDuration();          //Try duration index first ..
                if (duration == 0)
                    duration = damage;//+1;            //Base points after ..
                if (duration == 0)
                    duration = 10 * Time.InMilliseconds;                                  //10 seconds for enchants which don't have listed duration

                if (m_spellInfo.Id == 14792) // Venomhide Poison
                    duration = 5 * Time.Minute * Time.InMilliseconds;

                SpellItemEnchantmentRecord pEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);
                if (pEnchant == null)
                    return;

                // Always go to temp enchantment slot
                EnchantmentSlot slot = EnchantmentSlot.Temp;

                // Enchantment will not be applied if a different one already exists
                if (item.GetEnchantmentId(slot) != 0 && item.GetEnchantmentId(slot) != enchant_id)
                    return;

                // Apply the temporary enchantment
                item.SetEnchantment(slot, enchant_id, (uint)duration, 0, m_caster.GetGUID());
                item_owner.ApplyEnchantment(item, slot, true);
            }
        }

        [SpellEffectHandler(SpellEffectName.Disenchant)]
        void EffectDisEnchant(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player caster = m_caster.ToPlayer();
            if (caster != null)
            {
                caster.UpdateCraftSkill(m_spellInfo.Id);
                caster.SendLoot(itemTarget.GetGUID(), LootType.Disenchanting);
            }

            // item will be removed at disenchanting end
        }

        [SpellEffectHandler(SpellEffectName.Inebriate)]
        void EffectInebriate(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            Player player = unitTarget.ToPlayer();
            byte currentDrunk = player.GetDrunkValue();
            int drunkMod = damage;
            if (currentDrunk + drunkMod > 100)
            {
                currentDrunk = 100;
                if (RandomHelper.randChance() < 25.0f)
                    player.CastSpell(player, 67468, false);    // Drunken Vomit
            }
            else
                currentDrunk += (byte)drunkMod;

            player.SetDrunkValue(currentDrunk, m_CastItem != null ? m_CastItem.GetEntry() : 0);
        }

        [SpellEffectHandler(SpellEffectName.FeedPet)]
        void EffectFeedPet(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player player = m_caster.ToPlayer();
            if (player == null)
                return;

            Item foodItem = itemTarget;
            if (foodItem == null)
                return;

            Pet pet = player.GetPet();
            if (pet == null)
                return;

            if (!pet.IsAlive())
                return;

            int benefit = (int)pet.GetCurrentFoodBenefitLevel(foodItem.GetTemplate().GetBaseItemLevel());
            if (benefit <= 0)
                return;

            ExecuteLogEffectDestroyItem(effIndex, foodItem.GetEntry());

            uint count = 1;
            player.DestroyItemCount(foodItem, ref count, true);
            // @todo fix crash when a spell has two effects, both pointed at the same item target

            m_caster.CastCustomSpell(pet, effectInfo.TriggerSpell, benefit, 0, 0, true);
        }

        [SpellEffectHandler(SpellEffectName.DismissPet)]
        void EffectDismissPet(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsPet())
                return;

            Pet pet = unitTarget.ToPet();

            ExecuteLogEffectUnsummonObject(effIndex, pet);
            pet.GetOwner().RemovePet(pet, PetSaveMode.NotInSlot);
        }

        [SpellEffectHandler(SpellEffectName.SummonObjectSlot1)]
        void EffectSummonObject(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            byte slot = (byte)(effectInfo.Effect - SpellEffectName.SummonObjectSlot1);
            ObjectGuid guid = m_caster.m_ObjectSlot[slot];
            if (!guid.IsEmpty())
            {
                GameObject obj = m_caster.GetMap().GetGameObject(guid);
                if (obj != null)
                {
                    // Recast case - null spell id to make auras not be removed on object remove from world
                    if (m_spellInfo.Id == obj.GetSpellId())
                        obj.SetSpellId(0);
                    m_caster.RemoveGameObject(obj, true);
                }
                m_caster.m_ObjectSlot[slot].Clear();
            }

            float x, y, z;
            // If dest location if present
            if (m_targets.HasDst())
                destTarget.GetPosition(out x, out y, out z);
            // Summon in random point all other units if location present
            else
                m_caster.GetClosePoint(out x, out y, out z, SharedConst.DefaultWorldObjectSize);

            Map map = m_caster.GetMap();
            Position pos = new Position(x, y, z, m_caster.GetOrientation());
            Quaternion rotation = Quaternion.fromEulerAnglesZYX(m_caster.GetOrientation(), 0.0f, 0.0f);
            GameObject go = GameObject.CreateGameObject((uint)effectInfo.MiscValue, map, pos, rotation, 255, GameObjectState.Ready);
            if (!go)
                return;

            PhasingHandler.InheritPhaseShift(go, m_caster);

            int duration = m_spellInfo.CalcDuration(m_caster);
            go.SetRespawnTime(duration > 0 ? duration / Time.InMilliseconds : 0);
            go.SetSpellId(m_spellInfo.Id);
            m_caster.AddGameObject(go);

            ExecuteLogEffectSummonObject(effIndex, go);

            map.AddToMap(go);

            m_caster.m_ObjectSlot[slot] = go.GetGUID();
        }

        [SpellEffectHandler(SpellEffectName.Resurrect)]
        void EffectResurrect(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            if (unitTarget.IsAlive() || !unitTarget.IsInWorld)
                return;

            Player target = unitTarget.ToPlayer();

            if (target.IsResurrectRequested())       // already have one active request
                return;

            uint health = (uint)target.CountPctFromMaxHealth(damage);
            uint mana = (uint)MathFunctions.CalculatePct(target.GetMaxPower(PowerType.Mana), damage);

            ExecuteLogEffectResurrect(effIndex, target);

            target.SetResurrectRequestData(m_caster, health, mana, 0);
            SendResurrectRequest(target);
        }

        [SpellEffectHandler(SpellEffectName.AddExtraAttacks)]
        void EffectAddExtraAttacks(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!unitTarget || !unitTarget.IsAlive())
                return;

            if (unitTarget.m_extraAttacks != 0)
                return;

            unitTarget.m_extraAttacks = (uint)damage;

            ExecuteLogEffectExtraAttacks(effIndex, unitTarget, (uint)damage);
        }

        [SpellEffectHandler(SpellEffectName.Parry)]
        void EffectParry(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (m_caster.IsTypeId(TypeId.Player))
                m_caster.ToPlayer().SetCanParry(true);
        }

        [SpellEffectHandler(SpellEffectName.Block)]
        void EffectBlock(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (m_caster.IsTypeId(TypeId.Player))
                m_caster.ToPlayer().SetCanBlock(true);
        }

        [SpellEffectHandler(SpellEffectName.Leap)]
        void EffectLeap(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || unitTarget.IsInFlight())
                return;

            if (!m_targets.HasDst())
                return;

            Position pos = destTarget.GetPosition();
            pos = unitTarget.GetFirstCollisionPosition(unitTarget.GetDistance(pos.posX, pos.posY, pos.posZ + 2.0f), 0.0f);
            unitTarget.NearTeleportTo(pos.posX, pos.posY, pos.posZ, pos.Orientation, unitTarget == m_caster);
        }

        [SpellEffectHandler(SpellEffectName.Reputation)]
        void EffectReputation(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            Player player = unitTarget.ToPlayer();

            int repChange = damage;

            int factionId = effectInfo.MiscValue;

            FactionRecord factionEntry = CliDB.FactionStorage.LookupByKey(factionId);
            if (factionEntry == null)
                return;

            repChange = player.CalculateReputationGain(ReputationSource.Spell, 0, repChange, factionId);

            player.GetReputationMgr().ModifyReputation(factionEntry, repChange);
        }

        [SpellEffectHandler(SpellEffectName.QuestComplete)]
        void EffectQuestComplete(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;
            Player player = unitTarget.ToPlayer();

            uint questId = (uint)effectInfo.MiscValue;
            if (questId != 0)
            {
                Quest quest = Global.ObjectMgr.GetQuestTemplate(questId);
                if (quest == null)
                    return;

                ushort logSlot = player.FindQuestSlot(questId);
                if (logSlot < SharedConst.MaxQuestLogSize)
                    player.AreaExploredOrEventHappens(questId);
                else if (quest.HasFlag(QuestFlags.Tracking))  // Check if the quest is used as a serverside flag.
                    player.SetRewardedQuest(questId);          // If so, set status to rewarded without broadcasting it to client.
            }
        }

        [SpellEffectHandler(SpellEffectName.ForceDeselect)]
        void EffectForceDeselect(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            ClearTarget clearTarget = new ClearTarget();
            clearTarget.Guid = m_caster.GetGUID();
            m_caster.SendMessageToSet(clearTarget, true);
        }

        [SpellEffectHandler(SpellEffectName.SelfResurrect)]
        void EffectSelfResurrect(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (m_caster == null || m_caster.IsAlive())
                return;
            if (!m_caster.IsTypeId(TypeId.Player))
                return;
            if (!m_caster.IsInWorld)
                return;

            uint health = 0;
            int mana = 0;

            // flat case
            if (damage < 0)
            {
                health = (uint)-damage;
                mana = effectInfo.MiscValue;
            }
            // percent case
            else
            {
                health = (uint)m_caster.CountPctFromMaxHealth(damage);
                if (m_caster.GetMaxPower(PowerType.Mana) > 0)
                    mana = MathFunctions.CalculatePct(m_caster.GetMaxPower(PowerType.Mana), damage);
            }

            Player player = m_caster.ToPlayer();
            player.ResurrectPlayer(0.0f);

            player.SetHealth(health);
            player.SetPower(PowerType.Mana, mana);
            player.SetPower(PowerType.Rage, 0);
            player.SetFullPower(PowerType.Energy);
            player.SetPower(PowerType.Focus, 0);

            player.SpawnCorpseBones();
        }

        [SpellEffectHandler(SpellEffectName.Skinning)]
        void EffectSkinning(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!unitTarget.IsTypeId(TypeId.Unit))
                return;
            if (!m_caster.IsTypeId(TypeId.Player))
                return;

            Creature creature = unitTarget.ToCreature();
            int targetLevel = (int)creature.GetLevelForTarget(m_caster);

            SkillType skill = creature.GetCreatureTemplate().GetRequiredLootSkill();

            m_caster.ToPlayer().SendLoot(creature.GetGUID(), LootType.Skinning);
            creature.RemoveFlag(UnitFields.Flags, UnitFlags.Skinnable);
            creature.SetFlag(ObjectFields.DynamicFlags, UnitDynFlags.Lootable);

            if (skill == SkillType.Skinning)
            {
                int reqValue;
                if (targetLevel <= 10)
                    reqValue = 1;
                else if (targetLevel < 20)
                    reqValue = (targetLevel - 10) * 10;
                else if (targetLevel <= 73)
                    reqValue = targetLevel * 5;
                else if (targetLevel < 80)
                    reqValue = targetLevel * 10 - 365;
                else if (targetLevel <= 84)
                    reqValue = targetLevel * 5 + 35;
                else if (targetLevel <= 87)
                    reqValue = targetLevel * 15 - 805;
                else if (targetLevel <= 92)
                    reqValue = (targetLevel - 62) * 20;
                else if (targetLevel <= 104)
                    reqValue = targetLevel * 5 + 175;
                else if (targetLevel <= 107)
                    reqValue = targetLevel * 15 - 905;
                else if (targetLevel <= 112)
                    reqValue = (targetLevel - 72) * 20;
                else if (targetLevel <= 122)
                    reqValue = (targetLevel - 32) * 10;
                else
                    reqValue = 900;

                // TODO: Specialize skillid for each expansion
                // new db field?
                // tied to one of existing expansion fields in creature_template?

                // Double chances for elites
                m_caster.ToPlayer().UpdateGatherSkill(skill, (uint)damage, (uint)reqValue, (uint)(creature.isElite() ? 2 : 1));
            }
        }

        [SpellEffectHandler(SpellEffectName.Charge)]
        void EffectCharge(uint effIndex)
        {
            if (unitTarget == null)
                return;

            if (effectHandleMode == SpellEffectHandleMode.LaunchTarget)
            {
                float speed = MathFunctions.fuzzyGt(m_spellInfo.Speed, 0.0f) ? m_spellInfo.Speed : MotionMaster.SPEED_CHARGE;
                SpellEffectExtraData spellEffectExtraData = null;
                if (effectInfo.MiscValueB != 0)
                {
                    spellEffectExtraData = new SpellEffectExtraData();
                    spellEffectExtraData.Target = unitTarget.GetGUID();
                    spellEffectExtraData.SpellVisualId = (uint)effectInfo.MiscValueB;
                }
                // Spell is not using explicit target - no generated path
                if (m_preGeneratedPath.GetPathType() == PathType.Blank)
                {
                    Position pos = unitTarget.GetFirstCollisionPosition(unitTarget.GetObjectSize(), unitTarget.GetRelativeAngle(m_caster.GetPosition()));
                    m_caster.GetMotionMaster().MoveCharge(pos.posX, pos.posY, pos.posZ, speed, EventId.Charge, false, unitTarget, spellEffectExtraData);
                }
                else
                    m_caster.GetMotionMaster().MoveCharge(m_preGeneratedPath, speed, unitTarget, spellEffectExtraData);
            }

            if (effectHandleMode == SpellEffectHandleMode.HitTarget)
            {
                // not all charge effects used in negative spells
                if (!m_spellInfo.IsPositive() && m_caster.IsTypeId(TypeId.Player))
                    m_caster.Attack(unitTarget, true);

                if (effectInfo.TriggerSpell != 0)
                    m_caster.CastSpell(unitTarget, effectInfo.TriggerSpell, true, null, null, m_originalCasterGUID);
            }
        }

        [SpellEffectHandler(SpellEffectName.ChargeDest)]
        void EffectChargeDest(uint effIndex)
        {
            if (destTarget == null)
                return;

            if (effectHandleMode == SpellEffectHandleMode.Launch)
            {
                Position pos = destTarget.GetPosition();
                float angle = m_caster.GetRelativeAngle(pos.posX, pos.posY);
                float dist = m_caster.GetDistance(pos);
                pos = m_caster.GetFirstCollisionPosition(dist, angle);

                m_caster.GetMotionMaster().MoveCharge(pos.posX, pos.posY, pos.posZ);
            }
            else if (effectHandleMode == SpellEffectHandleMode.Hit)
            {
                if (effectInfo.TriggerSpell != 0)
                    m_caster.CastSpell(destTarget.GetPositionX(), destTarget.GetPositionY(), destTarget.GetPositionZ(), effectInfo.TriggerSpell, true, null, null, m_originalCasterGUID);
            }
        }

        [SpellEffectHandler(SpellEffectName.KnockBack)]
        [SpellEffectHandler(SpellEffectName.KnockBackDest)]
        void EffectKnockBack(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!unitTarget)
                return;

            Creature creatureTarget = unitTarget.ToCreature();
            if (creatureTarget)
                if (creatureTarget.isWorldBoss() || creatureTarget.IsDungeonBoss())
                    return;

            // Spells with SPELL_EFFECT_KNOCK_BACK (like Thunderstorm) can't knockback target if target has ROOT/STUN
            if (unitTarget.HasUnitState(UnitState.Root | UnitState.Stunned))
                return;

            // Instantly interrupt non melee spells being casted
            if (unitTarget.IsNonMeleeSpellCast(true))
                unitTarget.InterruptNonMeleeSpells(true);

            float ratio = 0.1f;
            float speedxy = effectInfo.MiscValue * ratio;
            float speedz = damage * ratio;
            if (speedxy < 0.1f && speedz < 0.1f)
                return;

            float x, y;
            if (effectInfo.Effect == SpellEffectName.KnockBackDest)
            {
                if (m_targets.HasDst())
                    destTarget.GetPosition(out x, out y);
                else
                    return;
            }
            else //if (GetEffect(i].Effect == SPELL_EFFECT_KNOCK_BACK)
            {
                m_caster.GetPosition(out x, out y);
            }

            unitTarget.KnockbackFrom(x, y, speedxy, speedz);
        }

        [SpellEffectHandler(SpellEffectName.LeapBack)]
        void EffectLeapBack(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.LaunchTarget)
                return;

            if (unitTarget == null)
                return;

            float speedxy = effectInfo.MiscValue / 10.0f;
            float speedz = damage / 10.0f;
            // Disengage
            unitTarget.JumpTo(speedxy, speedz, m_spellInfo.IconFileDataId != 132572);
        }

        [SpellEffectHandler(SpellEffectName.ClearQuest)]
        void EffectQuestClear(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;
            Player player = unitTarget.ToPlayer();

            uint quest_id = (uint)effectInfo.MiscValue;

            Quest quest = Global.ObjectMgr.GetQuestTemplate(quest_id);

            if (quest == null)
                return;

            QuestStatus oldStatus = player.GetQuestStatus(quest_id);

            // Player has never done this quest
            if (oldStatus == QuestStatus.None)
                return;

            // remove all quest entries for 'entry' from quest log
            for (byte slot = 0; slot < SharedConst.MaxQuestLogSize; ++slot)
            {
                uint logQuest = player.GetQuestSlotQuestId(slot);
                if (logQuest == quest_id)
                {
                    player.SetQuestSlot(slot, 0);

                    // we ignore unequippable quest items in this case, it's still be equipped
                    player.TakeQuestSourceItem(logQuest, false);

                    if (quest.HasFlag(QuestFlags.Pvp))
                    {
                        player.pvpInfo.IsHostile = player.pvpInfo.IsInHostileArea || player.HasPvPForcingQuest();
                        player.UpdatePvPState();
                    }
                }
            }

            player.RemoveActiveQuest(quest_id, false);
            player.RemoveRewardedQuest(quest_id);

            Global.ScriptMgr.OnQuestStatusChange(player, quest_id);
            Global.ScriptMgr.OnQuestStatusChange(player, quest, oldStatus, QuestStatus.None);
        }

        [SpellEffectHandler(SpellEffectName.SendTaxi)]
        void EffectSendTaxi(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            unitTarget.ToPlayer().ActivateTaxiPathTo((uint)effectInfo.MiscValue, m_spellInfo.Id);
        }

        [SpellEffectHandler(SpellEffectName.PullTowards)]
        [SpellEffectHandler(SpellEffectName.PullTowardsDest)]
        void EffectPullTowards(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!unitTarget)
                return;

            Position pos = new Position();
            if (effectInfo.Effect == SpellEffectName.PullTowardsDest)
            {
                if (m_targets.HasDst())
                    pos.Relocate(destTarget);
                else
                    return;
            }
            else
            {
                pos.Relocate(m_caster.GetPosition());
            }

            float speedXY = effectInfo.MiscValue * 0.1f;
            float speedZ = (float)(unitTarget.GetDistance(pos) / speedXY * 0.5f * MotionMaster.gravity);

            unitTarget.GetMotionMaster().MoveJump(pos, speedXY, speedZ);
        }

        [SpellEffectHandler(SpellEffectName.ChangeRaidMarker)]
        void EffectChangeRaidMarker(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Player player = m_caster.ToPlayer();
            if (!player || !m_targets.HasDst())
                return;

            Group group = player.GetGroup();
            if (!group || (group.isRaidGroup() && !group.IsLeader(player.GetGUID()) && !group.IsAssistant(player.GetGUID())))
                return;

            float x, y, z;
            destTarget.GetPosition(out x, out y, out z);

            group.AddRaidMarker((byte)damage, player.GetMapId(), x, y, z);
        }

        [SpellEffectHandler(SpellEffectName.DispelMechanic)]
        void EffectDispelMechanic(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            int mechanic = effectInfo.MiscValue;

            List<KeyValuePair<uint, ObjectGuid>> dispel_list = new List<KeyValuePair<uint, ObjectGuid>>();

            var auras = unitTarget.GetOwnedAuras();
            foreach (var pair in auras)
            {
                Aura aura = pair.Value;
                if (aura.GetApplicationOfTarget(unitTarget.GetGUID()) == null)
                    continue;

                if (RandomHelper.randChance(aura.CalcDispelChance(unitTarget, !unitTarget.IsFriendlyTo(m_caster))))
                    if (Convert.ToBoolean(aura.GetSpellInfo().GetAllEffectsMechanicMask() & (1 << mechanic)))
                        dispel_list.Add(new KeyValuePair<uint, ObjectGuid>(aura.GetId(), aura.GetCasterGUID()));
            }

            while (!dispel_list.Empty())
            {
                unitTarget.RemoveAura(dispel_list[0].Key, dispel_list[0].Value, 0, AuraRemoveMode.EnemySpell);
                dispel_list.RemoveAt(0);
            }
        }

        [SpellEffectHandler(SpellEffectName.ResurrectPet)]
        void EffectSummonDeadPet(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Player player = m_caster.ToPlayer();
            if (player == null)
                return;

            Pet pet = player.GetPet();
            if (pet != null && pet.IsAlive())
                return;

            if (damage < 0)
                return;

            float x, y, z;
            player.GetPosition(out x, out y, out z);
            if (pet == null)
            {
                player.SummonPet(0, x, y, z, player.Orientation, PetType.Summon, 0);
                pet = player.GetPet();
            }
            if (pet == null)
                return;

            player.GetMap().CreatureRelocation(pet, x, y, z, player.GetOrientation());

            pet.SetUInt32Value(ObjectFields.DynamicFlags, (uint)UnitDynFlags.HideModel);
            pet.RemoveFlag(UnitFields.Flags, UnitFlags.Skinnable);
            pet.setDeathState(DeathState.Alive);
            pet.ClearUnitState(UnitState.AllState);
            pet.SetHealth(pet.CountPctFromMaxHealth(damage));

            pet.InitializeAI();
            player.PetSpellInitialize();
            pet.SavePetToDB(PetSaveMode.AsCurrent);
        }

        [SpellEffectHandler(SpellEffectName.DestroyAllTotems)]
        void EffectDestroyAllTotems(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            int mana = 0;
            for (byte slot = (int)SummonSlot.Totem; slot < SharedConst.MaxTotemSlot; ++slot)
            {
                if (m_caster.m_SummonSlot[slot].IsEmpty())
                    continue;

                Creature totem = m_caster.GetMap().GetCreature(m_caster.m_SummonSlot[slot]);
                if (totem != null && totem.IsTotem())
                {
                    uint spell_id = totem.GetUInt32Value(UnitFields.CreatedBySpell);
                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spell_id);
                    if (spellInfo != null)
                    {
                        var costs = spellInfo.CalcPowerCost(m_caster, spellInfo.GetSchoolMask());
                        var m = costs.Find(cost => cost.Power == PowerType.Mana);
                        if (m != null)
                            mana += m.Amount;
                    }
                    totem.ToTotem().UnSummon();
                }
            }
            MathFunctions.ApplyPct(ref mana, damage);
            if (mana != 0)
                m_caster.CastCustomSpell(m_caster, 39104, mana, 0, 0, true);
        }

        [SpellEffectHandler(SpellEffectName.DurabilityDamage)]
        void EffectDurabilityDamage(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            int slot = effectInfo.MiscValue;

            // -1 means all player equipped items and -2 all items
            if (slot < 0)
            {
                unitTarget.ToPlayer().DurabilityPointsLossAll(damage, (slot < -1));
                ExecuteLogEffectDurabilityDamage(effIndex, unitTarget, -1, -1);
                return;
            }

            // invalid slot value
            if (slot >= InventorySlots.BagEnd)
                return;

            Item item = unitTarget.ToPlayer().GetItemByPos(InventorySlots.Bag0, (byte)slot);
            if (item != null)
            {
                unitTarget.ToPlayer().DurabilityPointsLoss(item, damage);
                ExecuteLogEffectDurabilityDamage(effIndex, unitTarget, (int)item.GetEntry(), slot);
            }
        }

        [SpellEffectHandler(SpellEffectName.DurabilityDamagePct)]
        void EffectDurabilityDamagePCT(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            int slot = effectInfo.MiscValue;

            // FIXME: some spells effects have value -1/-2
            // Possibly its mean -1 all player equipped items and -2 all items
            if (slot < 0)
            {
                unitTarget.ToPlayer().DurabilityLossAll(damage / 100.0f, (slot < -1));
                return;
            }

            // invalid slot value
            if (slot >= InventorySlots.BagEnd)
                return;

            if (damage <= 0)
                return;

            Item item = unitTarget.ToPlayer().GetItemByPos(InventorySlots.Bag0, (byte)slot);
            if (item != null)
                unitTarget.ToPlayer().DurabilityLoss(item, damage / 100.0f);
        }

        [SpellEffectHandler(SpellEffectName.ModifyThreatPercent)]
        void EffectModifyThreatPercent(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            unitTarget.GetThreatManager().modifyThreatPercent(m_caster, damage);
        }

        [SpellEffectHandler(SpellEffectName.TransDoor)]
        void EffectTransmitted(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            uint name_id = (uint)effectInfo.MiscValue;

            var overrideSummonedGameObjects = m_caster.GetAuraEffectsByType(AuraType.OverrideSummonedObject);
            foreach (AuraEffect aurEff in overrideSummonedGameObjects)
            {
                if (aurEff.GetMiscValue() == name_id)
                {
                    name_id = (uint)aurEff.GetMiscValueB();
                    break;
                }
            }

            GameObjectTemplate goinfo = Global.ObjectMgr.GetGameObjectTemplate(name_id);
            if (goinfo == null)
            {
                Log.outError(LogFilter.Sql, "Gameobject (Entry: {0}) not exist and not created at spell (ID: {1}) cast", name_id, m_spellInfo.Id);
                return;
            }

            float fx, fy, fz;

            if (m_targets.HasDst())
                destTarget.GetPosition(out fx, out fy, out fz);
            //FIXME: this can be better check for most objects but still hack
            else if (effectInfo.HasRadius() && m_spellInfo.Speed == 0)
            {
                float dis = effectInfo.CalcRadius(m_originalCaster);
                m_caster.GetClosePoint(out fx, out fy, out fz, SharedConst.DefaultWorldObjectSize, dis);
            }
            else
            {
                //GO is always friendly to it's creator, get range for friends
                float min_dis = m_spellInfo.GetMinRange(true);
                float max_dis = m_spellInfo.GetMaxRange(true);
                float dis = (float)RandomHelper.NextDouble() * (max_dis - min_dis) + min_dis;

                m_caster.GetClosePoint(out fx, out fy, out fz, SharedConst.DefaultWorldObjectSize, dis);
            }

            Map cMap = m_caster.GetMap();
            // if gameobject is summoning object, it should be spawned right on caster's position
            if (goinfo.type == GameObjectTypes.Ritual)
                m_caster.GetPosition(out fx, out fy, out fz);

            Position pos = new Position(fx, fy, fz, m_caster.GetOrientation());
            Quaternion rotation = Quaternion.fromEulerAnglesZYX(m_caster.GetOrientation(), 0.0f, 0.0f);
            GameObject go = GameObject.CreateGameObject(name_id, cMap, pos, rotation, 255, GameObjectState.Ready);
            if (!go)
                return;

            PhasingHandler.InheritPhaseShift(go, m_caster);

            int duration = m_spellInfo.CalcDuration(m_caster);

            switch (goinfo.type)
            {
                case GameObjectTypes.FishingNode:
                    {
                        go.SetFaction(m_caster.getFaction());
                        ObjectGuid bobberGuid = go.GetGUID();
                        // client requires fishing bobber guid in channel object slot 0 to be usable
                        m_caster.SetDynamicStructuredValue(UnitDynamicFields.ChannelObjects, 0, bobberGuid);
                        m_caster.AddGameObject(go);              // will removed at spell cancel

                        // end time of range when possible catch fish (FISHING_BOBBER_READY_TIME..GetDuration(m_spellInfo))
                        // start time == fish-FISHING_BOBBER_READY_TIME (0..GetDuration(m_spellInfo)-FISHING_BOBBER_READY_TIME)
                        int lastSec = 0;
                        switch (RandomHelper.IRand(0, 3))
                        {
                            case 0: lastSec = 3; break;
                            case 1: lastSec = 7; break;
                            case 2: lastSec = 13; break;
                            case 3: lastSec = 17; break;
                        }

                        duration = duration - lastSec * Time.InMilliseconds + 5 * Time.InMilliseconds;
                        break;
                    }
                case GameObjectTypes.Ritual:
                    {
                        if (m_caster.IsTypeId(TypeId.Player))
                        {
                            go.AddUniqueUse(m_caster.ToPlayer());
                            m_caster.AddGameObject(go);      // will be removed at spell cancel
                        }
                        break;
                    }
                case GameObjectTypes.DuelArbiter: // 52991
                    m_caster.AddGameObject(go);
                    break;
                case GameObjectTypes.FishingHole:
                case GameObjectTypes.Chest:
                default:
                    break;
            }

            go.SetRespawnTime(duration > 0 ? duration / Time.InMilliseconds : 0);
            go.SetOwnerGUID(m_caster.GetGUID());
            go.SetSpellId(m_spellInfo.Id);

            ExecuteLogEffectSummonObject(effIndex, go);

            Log.outDebug(LogFilter.Spells, "AddObject at SpellEfects.cpp EffectTransmitted");

            cMap.AddToMap(go);
            GameObject linkedTrap = go.GetLinkedTrap();
            if (linkedTrap != null)
            {
                PhasingHandler.InheritPhaseShift(linkedTrap, m_caster);
                linkedTrap.SetRespawnTime(duration > 0 ? duration / Time.InMilliseconds : 0);
                linkedTrap.SetSpellId(m_spellInfo.Id);
                linkedTrap.SetOwnerGUID(m_caster.GetGUID());

                ExecuteLogEffectSummonObject(effIndex, linkedTrap);
            }
        }

        [SpellEffectHandler(SpellEffectName.Prospecting)]
        void EffectProspecting(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player player = m_caster.ToPlayer();
            if (player == null)
                return;

            if (itemTarget == null || !itemTarget.GetTemplate().GetFlags().HasAnyFlag(ItemFlags.IsProspectable))
                return;

            if (itemTarget.GetCount() < 5)
                return;

            if (WorldConfig.GetBoolValue(WorldCfg.SkillProspecting))
            {
                uint SkillValue = player.GetPureSkillValue(SkillType.Jewelcrafting);
                uint reqSkillValue = itemTarget.GetTemplate().GetRequiredSkillRank();
                player.UpdateGatherSkill(SkillType.Jewelcrafting, SkillValue, reqSkillValue);
            }

            player.SendLoot(itemTarget.GetGUID(), LootType.Prospecting);
        }

        [SpellEffectHandler(SpellEffectName.Milling)]
        void EffectMilling(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player player = m_caster.ToPlayer();
            if (player == null)
                return;

            if (itemTarget == null || !itemTarget.GetTemplate().GetFlags().HasAnyFlag(ItemFlags.IsMillable))
                return;

            if (itemTarget.GetCount() < 5)
                return;

            if (WorldConfig.GetBoolValue(WorldCfg.SkillMilling))
            {
                uint SkillValue = player.GetPureSkillValue(SkillType.Inscription);
                uint reqSkillValue = itemTarget.GetTemplate().GetRequiredSkillRank();
                player.UpdateGatherSkill(SkillType.Inscription, SkillValue, reqSkillValue);
            }

            player.SendLoot(itemTarget.GetGUID(), LootType.Milling);
        }

        [SpellEffectHandler(SpellEffectName.Skill)]
        void EffectSkill(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Log.outDebug(LogFilter.Spells, "WORLD: SkillEFFECT");
        }

        /* There is currently no need for this effect. We handle it in Battleground.cpp
           If we would handle the resurrection here, the spiritguide would instantly disappear as the
           player revives, and so we wouldn't see the spirit heal visual effect on the npc.
           This is why we use a half sec delay between the visual effect and the resurrection itself */
        void EffectSpiritHeal(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;
        }

        // remove insignia spell effect
        [SpellEffectHandler(SpellEffectName.SkinPlayerCorpse)]
        void EffectSkinPlayerCorpse(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Log.outDebug(LogFilter.Spells, "Effect: SkinPlayerCorpse");

            Player player = m_caster.ToPlayer();
            Player target = unitTarget.ToPlayer();
            if (player == null || target == null || target.IsAlive())
                return;

            target.RemovedInsignia(player);
        }

        [SpellEffectHandler(SpellEffectName.StealBeneficialBuff)]
        void EffectStealBeneficialBuff(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Log.outDebug(LogFilter.Spells, "Effect: StealBeneficialBuff");

            if (unitTarget == null || unitTarget == m_caster)                 // can't steal from self
                return;

            List<DispelableAura> stealList = new List<DispelableAura>();

            // Create dispel mask by dispel type
            uint dispelMask = SpellInfo.GetDispelMask((DispelType)effectInfo.MiscValue);
            var auras = unitTarget.GetOwnedAuras();
            foreach (var map in auras)
            {
                Aura aura = map.Value;
                AuraApplication aurApp = aura.GetApplicationOfTarget(unitTarget.GetGUID());
                if (aurApp == null)
                    continue;

                if (Convert.ToBoolean(aura.GetSpellInfo().GetDispelMask() & dispelMask))
                {
                    // Need check for passive? this
                    if (!aurApp.IsPositive() || aura.IsPassive() || aura.GetSpellInfo().HasAttribute(SpellAttr4.NotStealable))
                        continue;

                    // 2.4.3 Patch Notes: "Dispel effects will no longer attempt to remove effects that have 100% dispel resistance."
                    int chance = aura.CalcDispelChance(unitTarget, !unitTarget.IsFriendlyTo(m_caster));
                    if (chance == 0)
                        continue;

                    // The charges / stack amounts don't count towards the total number of auras that can be dispelled.
                    // Ie: A dispel on a target with 5 stacks of Winters Chill and a Polymorph has 1 / (1 + 1) -> 50% chance to dispell
                    // Polymorph instead of 1 / (5 + 1) -> 16%.
                    bool dispelCharges = aura.GetSpellInfo().HasAttribute(SpellAttr7.DispelCharges);
                    byte charges = dispelCharges ? aura.GetCharges() : aura.GetStackAmount();
                    if (charges > 0)
                        stealList.Add(new DispelableAura(aura, chance, charges));
                }
            }

            if (stealList.Empty())
                return;

            int remaining = stealList.Count;

            // Ok if exist some buffs for dispel try dispel it
            List<Tuple<uint, ObjectGuid>> successList = new List<Tuple<uint, ObjectGuid>>();

            DispelFailed dispelFailed = new DispelFailed();
            dispelFailed.CasterGUID = m_caster.GetGUID();
            dispelFailed.VictimGUID = unitTarget.GetGUID();
            dispelFailed.SpellID = m_spellInfo.Id;

            // dispel N = damage buffs (or while exist buffs for dispel)
            for (int count = 0; count < damage && remaining > 0;)
            {
                // Random select buff for dispel
                var dispelableAura = stealList[RandomHelper.IRand(0, remaining - 1)];

                if (dispelableAura.RollDispel())
                {
                    successList.Add(Tuple.Create(dispelableAura.GetAura().GetId(), dispelableAura.GetAura().GetCasterGUID()));
                    if (!dispelableAura.DecrementCharge())
                    {
                        --remaining;
                        stealList[remaining] = dispelableAura;
                    }
                }
                else
                {
                    dispelFailed.FailedSpells.Add(dispelableAura.GetAura().GetId());
                }
                ++count;
            }

            if (!dispelFailed.FailedSpells.Empty())
                m_caster.SendMessageToSet(dispelFailed, true);

            if (successList.Empty())
                return;

            SpellDispellLog spellDispellLog = new SpellDispellLog();
            spellDispellLog.IsBreak = false; // TODO: use me
            spellDispellLog.IsSteal = true;

            spellDispellLog.TargetGUID = unitTarget.GetGUID();
            spellDispellLog.CasterGUID = m_caster.GetGUID();
            spellDispellLog.DispelledBySpellID = m_spellInfo.Id;

            foreach (var dispell in successList)
            {
                var dispellData = new SpellDispellData();
                dispellData.SpellID = dispell.Item1;
                dispellData.Harmful = false;      // TODO: use me
                //dispellData.Rolled = none;        // TODO: use me
                //dispellData.Needed = none;        // TODO: use me

                unitTarget.RemoveAurasDueToSpellBySteal(dispell.Item1, dispell.Item2, m_caster);

                spellDispellLog.DispellData.Add(dispellData);
            }
            m_caster.SendMessageToSet(spellDispellLog, true);
        }

        [SpellEffectHandler(SpellEffectName.KillCredit)]
        void EffectKillCreditPersonal(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            unitTarget.ToPlayer().KilledMonsterCredit((uint)effectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.KillCredit2)]
        void EffectKillCredit(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            int creatureEntry = effectInfo.MiscValue;
            if (creatureEntry != 0)
                unitTarget.ToPlayer().RewardPlayerAndGroupAtEvent((uint)creatureEntry, unitTarget);
        }

        [SpellEffectHandler(SpellEffectName.QuestFail)]
        void EffectQuestFail(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            unitTarget.ToPlayer().FailQuest((uint)effectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.QuestStart)]
        void EffectQuestStart(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!unitTarget)
                return;

            Player player = unitTarget.ToPlayer();
            if (!player)
                return;

            Quest quest = Global.ObjectMgr.GetQuestTemplate((uint)effectInfo.MiscValue);
            if (quest != null)
            {
                if (!player.CanTakeQuest(quest, false))
                    return;

                if (quest.IsAutoAccept() && player.CanAddQuest(quest, false))
                {
                    player.AddQuestAndCheckCompletion(quest, null);
                    player.PlayerTalkClass.SendQuestGiverQuestDetails(quest, player.GetGUID(), true, true);
                }
                else
                    player.PlayerTalkClass.SendQuestGiverQuestDetails(quest, player.GetGUID(), true, false);
            }
        }

        [SpellEffectHandler(SpellEffectName.ActivateRune)]
        void EffectActivateRune(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Launch)
                return;

            if (!m_caster.IsTypeId(TypeId.Player))
                return;

            Player player = m_caster.ToPlayer();

            if (player.GetClass() != Class.Deathknight)
                return;

            // needed later
            m_runesState = m_caster.ToPlayer().GetRunesState();

            uint count = (uint)damage;
            if (count == 0)
                count = 1;

            // first restore fully depleted runes
            for (byte j = 0; j < player.GetMaxPower(PowerType.Runes) && count > 0; ++j)
            {
                if (player.GetRuneCooldown(j) == player.GetRuneBaseCooldown())
                {
                    player.SetRuneCooldown(j, 0);
                    --count;
                }
            }

            // then the rest if we still got something left
            for (byte j = 0; j < player.GetMaxPower(PowerType.Runes) && count > 0; ++j)
            {
                player.SetRuneCooldown(j, 0);
                --count;
            }
        }

        [SpellEffectHandler(SpellEffectName.CreateTamedPet)]
        void EffectCreateTamedPet(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player) || !unitTarget.GetPetGUID().IsEmpty() || unitTarget.GetClass() != Class.Hunter)
                return;

            uint creatureEntry = (uint)effectInfo.MiscValue;
            Pet pet = unitTarget.CreateTamedPetFrom(creatureEntry, m_spellInfo.Id);
            if (pet == null)
                return;

            // relocate
            float px, py, pz;
            unitTarget.GetClosePoint(out px, out py, out pz, pet.GetObjectSize(), SharedConst.PetFollowDist, pet.GetFollowAngle());
            pet.Relocate(px, py, pz, unitTarget.GetOrientation());

            // add to world
            pet.GetMap().AddToMap(pet.ToCreature());

            // unitTarget has pet now
            unitTarget.SetMinion(pet, true);

            if (unitTarget.IsTypeId(TypeId.Player))
            {
                pet.SavePetToDB(PetSaveMode.AsCurrent);
                unitTarget.ToPlayer().PetSpellInitialize();
            }
        }

        [SpellEffectHandler(SpellEffectName.DiscoverTaxi)]
        void EffectDiscoverTaxi(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;
            uint nodeid = (uint)effectInfo.MiscValue;
            if (CliDB.TaxiNodesStorage.ContainsKey(nodeid))
                unitTarget.ToPlayer().GetSession().SendDiscoverNewTaxiNode(nodeid);
        }

        [SpellEffectHandler(SpellEffectName.TitanGrip)]
        void EffectTitanGrip(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (m_caster.IsTypeId(TypeId.Player))
                m_caster.ToPlayer().SetCanTitanGrip(true, (uint)effectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.RedirectThreat)]
        void EffectRedirectThreat(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget != null)
                m_caster.SetRedirectThreat(unitTarget.GetGUID(), (uint)damage);
        }

        [SpellEffectHandler(SpellEffectName.GameObjectDamage)]
        void EffectGameObjectDamage(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (gameObjTarget == null)
                return;

            Unit caster = m_originalCaster;
            if (caster == null)
                return;

            FactionTemplateRecord casterFaction = caster.GetFactionTemplateEntry();
            FactionTemplateRecord targetFaction = CliDB.FactionTemplateStorage.LookupByKey(gameObjTarget.GetUInt32Value(GameObjectFields.Faction));
            // Do not allow to damage GO's of friendly factions (ie: Wintergrasp Walls/Ulduar Storm Beacons)
            if (targetFaction == null || (casterFaction != null && targetFaction != null && !casterFaction.IsFriendlyTo(targetFaction)))
                gameObjTarget.ModifyHealth(-damage, caster, GetSpellInfo().Id);
        }

        [SpellEffectHandler(SpellEffectName.GameobjectRepair)]
        void EffectGameObjectRepair(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (gameObjTarget == null)
                return;

            gameObjTarget.ModifyHealth(damage, m_caster);
        }

        [SpellEffectHandler(SpellEffectName.GameobjectSetDestructionState)]
        void EffectGameObjectSetDestructionState(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (gameObjTarget == null || m_originalCaster == null)
                return;

            Player player = m_originalCaster.GetCharmerOrOwnerPlayerOrPlayerItself();
            gameObjTarget.SetDestructibleState((GameObjectDestructibleState)effectInfo.MiscValue, player, true);
        }

        void SummonGuardian(uint i, uint entry, SummonPropertiesRecord properties, uint numGuardians)
        {
            Unit caster = m_originalCaster;
            if (caster == null)
                return;

            if (caster.IsTotem())
                caster = caster.ToTotem().GetOwner();

            // in another case summon new
            uint level = caster.getLevel();

            // level of pet summoned using engineering item based at engineering skill level
            if (m_CastItem != null && caster.IsTypeId(TypeId.Player))
            {
                ItemTemplate proto = m_CastItem.GetTemplate();
                if (proto != null)
                    if (proto.GetRequiredSkill() == (uint)SkillType.Engineering)
                    {
                        ushort skill202 = caster.ToPlayer().GetSkillValue(SkillType.Engineering);
                        if (skill202 != 0)
                            level = (uint)(skill202 / 5);
                    }
            }

            float radius = 5.0f;
            int duration = m_spellInfo.CalcDuration(m_originalCaster);

            //TempSummonType summonType = (duration == 0) ? TempSummonType.DeadDespawn : TempSummonType.TimedDespawn;
            Map map = caster.GetMap();

            for (uint count = 0; count < numGuardians; ++count)
            {
                Position pos = new Position();
                if (count == 0)
                    pos = destTarget;
                else
                    // randomize position for multiple summons
                    m_caster.GetRandomPoint(destTarget, radius, out pos);

                TempSummon summon = map.SummonCreature(entry, pos, properties, (uint)duration, caster, m_spellInfo.Id);
                if (summon == null)
                    return;
                if (summon.HasUnitTypeMask(UnitTypeMask.Guardian))
                    ((Guardian)summon).InitStatsForLevel(level);

                if (properties != null && properties.Control == SummonCategory.Ally)
                    summon.SetFaction(caster.getFaction());

                if (summon.HasUnitTypeMask(UnitTypeMask.Minion) && m_targets.HasDst())
                    ((Minion)summon).SetFollowAngle(m_caster.GetAngle(summon.GetPosition()));

                if (summon.GetEntry() == 27893)
                {
                    uint weapon = m_caster.GetUInt32Value(PlayerFields.VisibleItem + (EquipmentSlot.MainHand * 2));
                    if (weapon != 0)
                    {
                        summon.SetDisplayId(11686);
                        summon.SetVirtualItem(0, weapon);
                    }
                    else
                        summon.SetDisplayId(1126);
                }

                summon.GetAI().EnterEvadeMode();

                ExecuteLogEffectSummonObject(i, summon);
            }
        }

        [SpellEffectHandler(SpellEffectName.AllowRenamePet)]
        void EffectRenamePet(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Unit) ||
                !unitTarget.IsPet() || unitTarget.ToPet().getPetType() != PetType.Hunter)
                return;

            unitTarget.SetByteFlag(UnitFields.Bytes2, UnitBytes2Offsets.PetFlags, UnitPetFlags.CanBeRenamed);
        }

        [SpellEffectHandler(SpellEffectName.PlayMusic)]
        void EffectPlayMusic(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            uint soundid = (uint)effectInfo.MiscValue;

            if (!CliDB.SoundKitStorage.ContainsKey(soundid))
            {
                Log.outError(LogFilter.Spells, "EffectPlayMusic: Sound (Id: {0}) not exist in spell {1}.", soundid, m_spellInfo.Id);
                return;
            }

            unitTarget.ToPlayer().SendPacket(new PlayMusic(soundid));
        }

        [SpellEffectHandler(SpellEffectName.TalentSpecSelect)]
        void EffectActivateSpec(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            Player player = unitTarget.ToPlayer();
            uint specID = m_misc.SpecializationId;
            ChrSpecializationRecord spec = CliDB.ChrSpecializationStorage.LookupByKey(specID);

            // Safety checks done in Spell::CheckCast
            if (!spec.IsPetSpecialization())
                player.ActivateTalentGroup(spec);
            else
                player.GetPet().SetSpecialization(specID);
        }

        [SpellEffectHandler(SpellEffectName.PlaySound)]
        void EffectPlaySound(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!unitTarget)
                return;

            Player player = unitTarget.ToPlayer();
            if (!player)
                return;

            switch (m_spellInfo.Id)
            {
                case 91604: // Restricted Flight Area
                    player.GetSession().SendNotification(CypherStrings.ZoneNoflyzone);
                    break;
                default:
                    break;
            }

            uint soundId = (uint)effectInfo.MiscValue;

            if (!CliDB.SoundKitStorage.ContainsKey(soundId))
            {
                Log.outError(LogFilter.Spells, "EffectPlaySound: Sound (Id: {0}) not exist in spell {1}.", soundId, m_spellInfo.Id);
                return;
            }

            player.PlayDirectSound(soundId, player);
        }

        [SpellEffectHandler(SpellEffectName.RemoveAura)]
        void EffectRemoveAura(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;
            // there may be need of specifying casterguid of removed auras
            unitTarget.RemoveAurasDueToSpell(effectInfo.TriggerSpell);
        }

        [SpellEffectHandler(SpellEffectName.DamageFromMaxHealthPCT)]
        void EffectDamageFromMaxHealthPCT(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            m_damage += (int)unitTarget.CountPctFromMaxHealth(damage);
        }

        [SpellEffectHandler(SpellEffectName.GiveCurrency)]
        void EffectGiveCurrency(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            if (!CliDB.CurrencyTypesStorage.ContainsKey(effectInfo.MiscValue))
                return;

            unitTarget.ToPlayer().ModifyCurrency((CurrencyTypes)effectInfo.MiscValue, damage);
        }

        [SpellEffectHandler(SpellEffectName.CastButton)]
        void EffectCastButtons(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (!m_caster.IsTypeId(TypeId.Player))
                return;

            Player p_caster = m_caster.ToPlayer();
            int button_id = effectInfo.MiscValue + 132;
            int n_buttons = effectInfo.MiscValueB;

            for (; n_buttons != 0; --n_buttons, ++button_id)
            {
                ActionButton ab = p_caster.GetActionButton((byte)button_id);
                if (ab == null || ab.GetButtonType() != ActionButtonType.Spell)
                    continue;

                //! Action button data is unverified when it's set so it can be "hacked"
                //! to contain invalid spells, so filter here.
                uint spell_id = ab.GetAction();
                if (spell_id == 0)
                    continue;

                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spell_id);
                if (spellInfo == null)
                    continue;

                if (!p_caster.HasSpell(spell_id) || p_caster.GetSpellHistory().HasCooldown(spell_id))
                    continue;

                if (!spellInfo.HasAttribute(SpellAttr9.SummonPlayerTotem))
                    continue;

                TriggerCastFlags triggerFlags = (TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.CastDirectly | TriggerCastFlags.DontReportCastError);
                m_caster.CastSpell(m_caster, spell_id, triggerFlags);
            }
        }

        [SpellEffectHandler(SpellEffectName.RechargeItem)]
        void EffectRechargeItem(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            Player player = unitTarget.ToPlayer();
            if (player == null)
                return;

            Item item = player.GetItemByEntry(effectInfo.ItemType);
            if (item != null)
            {
                ItemTemplate proto = item.GetTemplate();
                for (int x = 0; x < proto.Effects.Count && x < 5; ++x)
                    item.SetSpellCharges(x, proto.Effects[x].Charges);

                item.SetState(ItemUpdateState.Changed, player);
            }
        }

        [SpellEffectHandler(SpellEffectName.Bind)]
        void EffectBind(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            Player player = unitTarget.ToPlayer();

            WorldLocation homeLoc = new WorldLocation();
            uint areaId = player.GetAreaId();

            if (effectInfo.MiscValue != 0)
                areaId = (uint)effectInfo.MiscValue;

            if (m_targets.HasDst())
                homeLoc.WorldRelocate(destTarget);
            else
            {
                homeLoc.Relocate(player.GetPosition());
                homeLoc.SetMapId(player.GetMapId());
            }

            player.SetHomebind(homeLoc, areaId);
            player.SendBindPointUpdate();

            Log.outDebug(LogFilter.Spells, "EffectBind: New homebind MapId: {0}, AreaId: {1}, {2}, ", homeLoc.GetMapId(), areaId, homeLoc);

            // zone update
            PlayerBound packet = new PlayerBound(m_caster.GetGUID(), areaId);
            player.SendPacket(packet);
        }

        [SpellEffectHandler(SpellEffectName.SummonRafFriend)]
        void EffectSummonRaFFriend(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!m_caster.IsTypeId(TypeId.Player) || unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            m_caster.CastSpell(unitTarget, effectInfo.TriggerSpell, true);
        }

        [SpellEffectHandler(SpellEffectName.UnlockGuildVaultTab)]
        void EffectUnlockGuildVaultTab(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            // Safety checks done in Spell.CheckCast
            Player caster = m_caster.ToPlayer();
            Guild guild = caster.GetGuild();
            if (guild != null)
                guild.HandleBuyBankTab(caster.GetSession(), (byte)(damage - 1)); // Bank tabs start at zero internally
        }

        [SpellEffectHandler(SpellEffectName.ResurrectWithAura)]
        void EffectResurrectWithAura(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsInWorld)
                return;

            Player target = unitTarget.ToPlayer();
            if (target == null)
                return;

            if (unitTarget.IsAlive())
                return;

            if (target.IsResurrectRequested())       // already have one active request
                return;

            uint health = (uint)target.CountPctFromMaxHealth(damage);
            uint mana = (uint)MathFunctions.CalculatePct(target.GetMaxPower(PowerType.Mana), damage);
            uint resurrectAura = 0;
            if (Global.SpellMgr.GetSpellInfo(effectInfo.TriggerSpell) != null)
                resurrectAura = effectInfo.TriggerSpell;

            if (resurrectAura != 0 && target.HasAura(resurrectAura))
                return;

            ExecuteLogEffectResurrect(effIndex, target);
            target.SetResurrectRequestData(m_caster, health, mana, resurrectAura);
            SendResurrectRequest(target);
        }

        [SpellEffectHandler(SpellEffectName.CreateAreaTrigger)]
        void EffectCreateAreaTrigger(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (!m_targets.HasDst())
                return;

            int duration = GetSpellInfo().CalcDuration(GetCaster());
            AreaTrigger.CreateAreaTrigger((uint)effectInfo.MiscValue, GetCaster(), null, GetSpellInfo(), destTarget.GetPosition(), duration, m_SpellVisual, m_castId);
        }

        [SpellEffectHandler(SpellEffectName.RemoveTalent)]
        void EffectRemoveTalent(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            TalentRecord talent = CliDB.TalentStorage.LookupByKey(m_misc.TalentId);
            if (talent == null)
                return;

            Player player = unitTarget ? unitTarget.ToPlayer() : null;
            if (player == null)
                return;

            player.RemoveTalent(talent);
            player.SendTalentsInfoData();
        }

        [SpellEffectHandler(SpellEffectName.DestroyItem)]
        void EffectDestroyItem(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!unitTarget || !unitTarget.IsTypeId(TypeId.Player))
                return;

            Player player = unitTarget.ToPlayer();
            Item item = player.GetItemByEntry(effectInfo.ItemType);
            if (item)
                player.DestroyItem(item.GetBagSlot(), item.GetSlot(), true);
        }

        [SpellEffectHandler(SpellEffectName.LearnGarrisonBuilding)]
        void EffectLearnGarrisonBuilding(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!unitTarget || !unitTarget.IsTypeId(TypeId.Player))
                return;

            Garrison garrison = unitTarget.ToPlayer().GetGarrison();
            if (garrison != null)
                garrison.LearnBlueprint((uint)effectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.CreateGarrison)]
        void EffectCreateGarrison(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!unitTarget || !unitTarget.IsTypeId(TypeId.Player))
                return;

            unitTarget.ToPlayer().CreateGarrison((uint)effectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.CreateConversation)]
        void EffectCreateConversation(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (!m_targets.HasDst())
                return;

            Conversation.CreateConversation((uint)effectInfo.MiscValue, GetCaster(), destTarget.GetPosition(), new List<ObjectGuid>() { GetCaster().GetGUID() }, GetSpellInfo());
        }

        [SpellEffectHandler(SpellEffectName.AddGarrisonFollower)]
        void EffectAddGarrisonFollower(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!unitTarget || !unitTarget.IsTypeId(TypeId.Player))
                return;

            Garrison garrison = unitTarget.ToPlayer().GetGarrison();
            if (garrison != null)
                garrison.AddFollower((uint)effectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.CreateHeirloomItem)]
        void EffectCreateHeirloomItem(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player player = m_caster.ToPlayer();
            if (!player)
                return;

            CollectionMgr collectionMgr = player.GetSession().GetCollectionMgr();
            if (collectionMgr == null)
                return;

            List<uint> bonusList = new List<uint>();
            bonusList.Add(collectionMgr.GetHeirloomBonus(m_misc.Data0));

            DoCreateItem(effIndex, m_misc.Data0, 0, bonusList);
            ExecuteLogEffectCreateItem(effIndex, m_misc.Data0);
        }

        [SpellEffectHandler(SpellEffectName.ActivateGarrisonBuilding)]
        void EffectActivateGarrisonBuilding(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!unitTarget || !unitTarget.IsTypeId(TypeId.Player))
                return;

            Garrison garrison = unitTarget.ToPlayer().GetGarrison();
            if (garrison != null)
                garrison.ActivateBuilding((uint)effectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.HealBattlepetPct)]
        void EffectHealBattlePetPct(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!unitTarget || !unitTarget.IsTypeId(TypeId.Player))
                return;

            BattlePetMgr battlePetMgr = unitTarget.ToPlayer().GetSession().GetBattlePetMgr();
            if (battlePetMgr != null)
                battlePetMgr.HealBattlePetsPct((byte)damage);
        }

        [SpellEffectHandler(SpellEffectName.EnableBattlePets)]
        void EffectEnableBattlePets(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!unitTarget || !unitTarget.IsTypeId(TypeId.Player))
                return;

            Player plr = unitTarget.ToPlayer();
            plr.SetFlag(PlayerFields.Flags, PlayerFlags.PetBattlesUnlocked);
            plr.GetSession().GetBattlePetMgr().UnlockSlot(0);
        }

        [SpellEffectHandler(SpellEffectName.LaunchQuestChoice)]
        void EffectLaunchQuestChoice(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!unitTarget || !unitTarget.IsPlayer())
                return;

            unitTarget.ToPlayer().SendPlayerChoice(GetCaster().GetGUID(), effectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.UncageBattlepet)]
        void EffectUncageBattlePet(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (!m_CastItem || !m_caster || !m_caster.IsTypeId(TypeId.Player))
                return;

            Player plr = m_caster.ToPlayer();

            // are we allowed to learn battle pets without it?
            /*if (plr.HasFlag(PLAYER_FLAGS, PLAYER_FLAGS_PET_BATTLES_UNLOCKED))
                return; // send some error*/

            uint speciesId = m_CastItem.GetModifier(ItemModifier.BattlePetSpeciesId);
            ushort breed = (ushort)(m_CastItem.GetModifier(ItemModifier.BattlePetBreedData) & 0xFFFFFF);
            byte quality = (byte)((m_CastItem.GetModifier(ItemModifier.BattlePetBreedData) >> 24) & 0xFF);
            ushort level = (ushort)m_CastItem.GetModifier(ItemModifier.BattlePetLevel);
            uint creatureId = m_CastItem.GetModifier(ItemModifier.BattlePetDisplayId);

            BattlePetSpeciesRecord speciesEntry = CliDB.BattlePetSpeciesStorage.LookupByKey(speciesId);
            if (speciesEntry == null)
                return;

            BattlePetMgr battlePetMgr = plr.GetSession().GetBattlePetMgr();
            if (battlePetMgr == null)
                return;

            // TODO: This means if you put your highest lvl pet into cage, you won't be able to uncage it again which is probably wrong.
            // We will need to store maxLearnedLevel somewhere to avoid this behaviour.
            if (battlePetMgr.GetMaxPetLevel() < level)
            {
                battlePetMgr.SendError(BattlePetError.TooHighLevelToUncage, creatureId); // or speciesEntry.CreatureID
                SendCastResult(SpellCastResult.CantAddBattlePet);
                return;
            }

            if (battlePetMgr.GetPetCount(speciesId) >= SharedConst.MaxBattlePetsPerSpecies)
            {
                battlePetMgr.SendError(BattlePetError.CantHaveMorePetsOfThatType, creatureId); // or speciesEntry.CreatureID
                SendCastResult(SpellCastResult.CantAddBattlePet);
                return;
            }

            battlePetMgr.AddPet(speciesId, creatureId, breed, quality, level);

            if (!plr.HasSpell(speciesEntry.SummonSpellID))
                plr.LearnSpell(speciesEntry.SummonSpellID, false);

            plr.DestroyItem(m_CastItem.GetBagSlot(), m_CastItem.GetSlot(), true);
            m_CastItem = null;
        }

        [SpellEffectHandler(SpellEffectName.UpgradeHeirloom)]
        void EffectUpgradeHeirloom(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Player player = m_caster.ToPlayer();
            if (player)
            {
                CollectionMgr collectionMgr = player.GetSession().GetCollectionMgr();
                if (collectionMgr != null)
                    collectionMgr.UpgradeHeirloom(m_misc.Data0, m_castItemEntry);
            }
        }

        [SpellEffectHandler(SpellEffectName.ApplyEnchantIllusion)]
        void EffectApplyEnchantIllusion(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!itemTarget)
                return;

            Player player = m_caster.ToPlayer();
            if (!player || player.GetGUID() != itemTarget.GetOwnerGUID())
                return;

            itemTarget.SetState(ItemUpdateState.Changed, player);
            itemTarget.SetModifier(ItemModifier.EnchantIllusionAllSpecs, (uint)effectInfo.MiscValue);
            if (itemTarget.IsEquipped())
                player.SetUInt16Value(PlayerFields.VisibleItem + 1 + (itemTarget.GetSlot() * 2), 1, itemTarget.GetVisibleItemVisual(player));

            player.RemoveTradeableItem(itemTarget);
            itemTarget.ClearSoulboundTradeable(player);
        }

        [SpellEffectHandler(SpellEffectName.UpdatePlayerPhase)]
        void EffectUpdatePlayerPhase(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!unitTarget || !unitTarget.IsTypeId(TypeId.Player))
                return;

            PhasingHandler.OnConditionChange(unitTarget);
        }

        [SpellEffectHandler(SpellEffectName.UpdateZoneAurasPhases)]
        void EffectUpdateZoneAurasAndPhases(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!unitTarget || !unitTarget.IsTypeId(TypeId.Player))
                return;

            unitTarget.ToPlayer().UpdateAreaDependentAuras(unitTarget.GetAreaId());
        }

        [SpellEffectHandler(SpellEffectName.GiveArtifactPower)]
        void EffectGiveArtifactPower(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.LaunchTarget)
                return;

            if (!m_caster.IsTypeId(TypeId.Player))
                return;

            Aura artifactAura = m_caster.GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);
            if (artifactAura != null)
            {
                Item artifact = m_caster.ToPlayer().GetItemByGuid(artifactAura.GetCastItemGUID());
                if (artifact)
                    artifact.GiveArtifactXp((ulong)damage, m_CastItem, (ArtifactCategory)effectInfo.MiscValue);
            }
        }

        [SpellEffectHandler(SpellEffectName.GiveArtifactPowerNoBonus)]
        void EffectGiveArtifactPowerNoBonus(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.LaunchTarget)
                return;

            if (!unitTarget || !m_caster.IsTypeId(TypeId.Player))
                return;

            Aura artifactAura = unitTarget.GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);
            if (artifactAura != null)
            {
                Item artifact = unitTarget.ToPlayer().GetItemByGuid(artifactAura.GetCastItemGUID());
                if (artifact)
                    artifact.GiveArtifactXp((ulong)damage, m_CastItem, 0);
            }
        }

        [SpellEffectHandler(SpellEffectName.PlayScene)]
        void EffectPlayScene(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (m_caster.GetTypeId() != TypeId.Player)
                return;

            m_caster.ToPlayer().GetSceneMgr().PlayScene((uint)effectInfo.MiscValue, destTarget);
        }

        [SpellEffectHandler(SpellEffectName.GiveHonor)]
        void EffectGiveHonor(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!unitTarget || unitTarget.GetTypeId() != TypeId.Player)
                return;

            PvPCredit packet = new PvPCredit();
            packet.Honor = damage;
            packet.OriginalHonor = damage;

            Player playerTarget = unitTarget.ToPlayer();
            playerTarget.AddHonorXP((uint)damage);
            playerTarget.SendPacket(packet);
        }

        [SpellEffectHandler(SpellEffectName.LearnTransmogSet)]
        void EffectLearnTransmogSet(uint effIndex)
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!unitTarget || !unitTarget.IsPlayer())
                return;

            unitTarget.ToPlayer().GetSession().GetCollectionMgr().AddTransmogSet((uint)effectInfo.MiscValue);
        }
    }

    public class DispelableAura
    {
        public DispelableAura(Aura aura, int dispelChance, byte dispelCharges)
        {
            _aura = aura;
            _chance = dispelChance;
            _charges = dispelCharges;
        }

        public bool RollDispel()
        {
            return RandomHelper.randChance(_chance);
        }

        public Aura GetAura()
        {
            return _aura;
        }

        public byte GetDispelCharges()
        {
            return _charges;
        }

        public void IncrementCharges()
        {
            ++_charges;
        }

        public bool DecrementCharge()
        {
            if (_charges == 0)
                return false;

            --_charges;
            return _charges > 0;
        }

        Aura _aura;
        int _chance;
        byte _charges;
    }
}
