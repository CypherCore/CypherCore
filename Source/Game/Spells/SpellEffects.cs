// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
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
using Game.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Game.Spells
{
    public partial class Spell
    {
        [SpellEffectHandler(SpellEffectName.None)]
        [SpellEffectHandler(SpellEffectName.Portal)]
        [SpellEffectHandler(SpellEffectName.BindSight)]
        [SpellEffectHandler(SpellEffectName.CallPet)]
        [SpellEffectHandler(SpellEffectName.PortalTeleport)]
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
        [SpellEffectHandler(SpellEffectName.Effect122)]
        [SpellEffectHandler(SpellEffectName.Effect175)]
        [SpellEffectHandler(SpellEffectName.Effect178)]
        void EffectUnused() { }

        void EffectResurrectNew()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (corpseTarget == null && unitTarget == null)
                return;

            Player player = null;

            if (corpseTarget != null)
                player = Global.ObjAccessor.FindPlayer(corpseTarget.GetOwnerGUID());
            else if (unitTarget != null)
                player = unitTarget.ToPlayer();

            if (player == null || player.IsAlive() || !player.IsInWorld)
                return;

            if (player.IsResurrectRequested())       // already have one active request
                return;

            int health = damage;
            int mana = effectInfo.MiscValue;
            ExecuteLogEffectResurrect(effectInfo.Effect, player);
            player.SetResurrectRequestData(m_caster, (uint)health, (uint)mana, 0);
            SendResurrectRequest(player);
        }

        [SpellEffectHandler(SpellEffectName.Instakill)]
        void EffectInstaKill()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsAlive())
                return;

            if (unitTarget.IsTypeId(TypeId.Player))
                if (unitTarget.ToPlayer().GetCommandStatus(PlayerCommandStates.God))
                    return;

            if (m_caster == unitTarget)                              // prevent interrupt message
                Finish();

            SpellInstakillLog data = new();
            data.Target = unitTarget.GetGUID();
            data.Caster = m_caster.GetGUID();
            data.SpellID = m_spellInfo.Id;
            m_caster.SendMessageToSet(data, true);

            Unit.Kill(GetUnitCasterForEffectHandlers(), unitTarget, false);
        }

        [SpellEffectHandler(SpellEffectName.EnvironmentalDamage)]
        void EffectEnvironmentalDMG()
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
                Unit unitCaster = GetUnitCasterForEffectHandlers();
                DamageInfo damageInfo = new(unitCaster, unitTarget, (uint)damage, m_spellInfo, m_spellInfo.GetSchoolMask(), DamageEffectType.SpellDirect, WeaponAttackType.BaseAttack);
                Unit.CalcAbsorbResist(damageInfo);

                SpellNonMeleeDamage log = new(unitCaster, unitTarget, m_spellInfo, m_SpellVisual, m_spellInfo.GetSchoolMask(), m_castId);
                log.damage = damageInfo.GetDamage();
                log.originalDamage = (uint)damage;
                log.absorb = damageInfo.GetAbsorb();
                log.resist = damageInfo.GetResist();

                if (unitCaster != null)
                    unitCaster.SendSpellNonMeleeDamageLog(log);
            }
        }

        [SpellEffectHandler(SpellEffectName.SchoolDamage)]
        void EffectSchoolDmg()
        {
            if (effectHandleMode != SpellEffectHandleMode.LaunchTarget)
                return;

            if (unitTarget != null && unitTarget.IsAlive())
            {
                bool apply_direct_bonus = true;

                // Meteor like spells (divided damage to targets)
                if (m_spellInfo.HasAttribute(SpellCustomAttributes.ShareDamage))
                {
                    long count = GetUnitTargetCountForEffect(effectInfo.EffectIndex);

                    // divide to all targets
                    if (count != 0)
                        damage /= (int)count;
                }

                Unit unitCaster = GetUnitCasterForEffectHandlers();
                if (unitCaster != null && apply_direct_bonus)
                {
                    int bonus = unitCaster.SpellDamageBonusDone(unitTarget, m_spellInfo, damage, DamageEffectType.SpellDirect, effectInfo, 1, this);
                    damage = (int)(bonus + (bonus * variance));
                    damage = unitTarget.SpellDamageBonusTaken(unitCaster, m_spellInfo, damage, DamageEffectType.SpellDirect);
                }

                m_damage += damage;
            }
        }

        [SpellEffectHandler(SpellEffectName.Dummy)]
        void EffectDummy()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null && gameObjTarget == null && itemTarget == null && corpseTarget == null)
                return;

            // pet auras
            if (m_caster.GetTypeId() == TypeId.Player)
            {
                PetAura petSpell = Global.SpellMgr.GetPetAura(m_spellInfo.Id, (byte)effectInfo.EffectIndex);
                if (petSpell != null)
                {
                    m_caster.ToPlayer().AddPetAura(petSpell);
                    return;
                }
            }

            // normal DB scripted effect
            Log.outDebug(LogFilter.Spells, "Spell ScriptStart spellid {0} in EffectDummy({1})", m_spellInfo.Id, effectInfo.EffectIndex);
            m_caster.GetMap().ScriptsStart(ScriptsType.Spell, (uint)((int)m_spellInfo.Id | (int)(effectInfo.EffectIndex << 24)), m_caster, unitTarget);
        }

        [SpellEffectHandler(SpellEffectName.TriggerSpell)]
        [SpellEffectHandler(SpellEffectName.TriggerSpellWithValue)]
        void EffectTriggerSpell()
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
                    // Demonic Empowerment -- succubus
                    case 54437:
                    {
                        unitTarget.RemoveMovementImpairingAuras(true);
                        unitTarget.RemoveAurasByType(AuraType.ModStalked);
                        unitTarget.RemoveAurasByType(AuraType.ModStun);

                        // Cast Lesser Invisibility
                        unitTarget.CastSpell(unitTarget, 7870, new CastSpellExtraArgs(this));
                        return;
                    }
                    // Brittle Armor - (need add max stack of 24575 Brittle Armor)
                    case 29284:
                    {
                        // Brittle Armor
                        SpellInfo spell = Global.SpellMgr.GetSpellInfo(24575, GetCastDifficulty());
                        if (spell == null)
                            return;

                        for (uint j = 0; j < spell.StackAmount; ++j)
                            m_caster.CastSpell(unitTarget, spell.Id, new CastSpellExtraArgs(this));
                        return;
                    }
                    // Mercurial Shield - (need add max stack of 26464 Mercurial Shield)
                    case 29286:
                    {
                        // Mercurial Shield
                        SpellInfo spell = Global.SpellMgr.GetSpellInfo(26464, GetCastDifficulty());
                        if (spell == null)
                            return;

                        for (uint j = 0; j < spell.StackAmount; ++j)
                            m_caster.CastSpell(unitTarget, spell.Id, new CastSpellExtraArgs(this));
                        return;
                    }
                }
            }

            if (triggered_spell_id == 0)
            {
                Log.outWarn(LogFilter.Spells, $"Spell::EffectTriggerSpell: Spell {m_spellInfo.Id} [EffectIndex: {effectInfo.EffectIndex}] does not have triggered spell.");
                return;
            }

            // normal case
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(triggered_spell_id, GetCastDifficulty());
            if (spellInfo == null)
            {
                Log.outDebug(LogFilter.Spells, "Spell.EffectTriggerSpell spell {0} tried to trigger unknown spell {1}", m_spellInfo.Id, triggered_spell_id);
                return;
            }

            SpellCastTargets targets = new();
            int? targetCount = null;
            int? targetIndex = null;
            if (effectHandleMode == SpellEffectHandleMode.LaunchTarget)
            {
                if (!spellInfo.NeedsToBeTriggeredByCaster(m_spellInfo))
                    return;

                targets.SetUnitTarget(unitTarget);
                targetCount = (int)GetUnitTargetCountForEffect(effectInfo.EffectIndex);
                targetIndex = GetUnitTargetIndexForEffect(unitTarget.GetGUID(), effectInfo.EffectIndex);
            }
            else //if (effectHandleMode == SpellEffectHandleMode.Launch)
            {
                if (spellInfo.NeedsToBeTriggeredByCaster(m_spellInfo) && effectInfo.GetProvidedTargetMask().HasAnyFlag(SpellCastTargetFlags.UnitMask))
                    return;

                if (spellInfo.GetExplicitTargetMask().HasAnyFlag(SpellCastTargetFlags.DestLocation) && m_targets.HasDst())
                    targets.SetDst(m_targets);

                Unit target = m_targets.GetUnitTarget();
                if (target != null)
                    targets.SetUnitTarget(target);
                else
                {
                    Unit unit = m_caster.ToUnit();
                    if (unit != null)
                        targets.SetUnitTarget(unit);
                    else
                    {
                        GameObject go = m_caster.ToGameObject();
                        if (go != null)
                            targets.SetGOTarget(go);
                    }
                }
            }

            TimeSpan delay = TimeSpan.Zero;
            if (effectInfo.Effect == SpellEffectName.TriggerSpell)
                delay = TimeSpan.FromMilliseconds(effectInfo.MiscValue);

            var caster = m_caster;
            var originalCaster = m_originalCasterGUID;
            var castItemGuid = m_castItemGUID;
            var originalCastId = m_castId;
            var triggerSpell = effectInfo.TriggerSpell;
            var effect = effectInfo.Effect;
            var value = damage;
            var itemLevel = m_castItemLevel;

            m_caster.m_Events.AddEventAtOffset(() =>
            {
                targets.Update(caster); // refresh pointers stored in targets

                // original caster guid only for GO cast
                CastSpellExtraArgs args = new(TriggerCastFlags.FullMask & ~(TriggerCastFlags.IgnorePowerCost | TriggerCastFlags.IgnoreReagentCost));
                args.SetOriginalCaster(originalCaster);
                args.OriginalCastId = originalCastId;
                args.OriginalCastItemLevel = itemLevel;
                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(triggerSpell, caster.GetMap().GetDifficultyID());
                if (!castItemGuid.IsEmpty() && spellInfo.HasAttribute(SpellAttr2.RetainItemCast))
                {
                    Player triggeringAuraCaster = caster?.ToPlayer();
                    if (triggeringAuraCaster != null)
                        args.CastItem = triggeringAuraCaster.GetItemByGuid(castItemGuid);
                }

                // set basepoints for trigger with value effect
                if (effect == SpellEffectName.TriggerSpellWithValue)
                    for (int i = 0; i < spellInfo.GetEffects().Count; ++i)
                        args.AddSpellMod(SpellValueMod.BasePoint0 + i, value);

                if (targetCount.HasValue)
                    args.AddSpellMod(SpellValueMod.ParentSpellTargetCount, targetCount.Value);

                if (targetIndex.HasValue)
                    args.AddSpellMod(SpellValueMod.ParentSpellTargetIndex, targetIndex.Value);

                caster.CastSpell(targets, triggerSpell, args);
            }, delay);
        }

        [SpellEffectHandler(SpellEffectName.TriggerMissile)]
        [SpellEffectHandler(SpellEffectName.TriggerMissileSpellWithValue)]
        void EffectTriggerMissileSpell()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget
                && effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            uint triggered_spell_id = effectInfo.TriggerSpell;
            if (triggered_spell_id == 0)
            {
                Log.outWarn(LogFilter.Spells, $"Spell::EffectTriggerMissileSpell: Spell {m_spellInfo.Id} [EffectIndex: {effectInfo.EffectIndex}] does not have triggered spell.");
                return;
            }

            // normal case
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(triggered_spell_id, GetCastDifficulty());
            if (spellInfo == null)
            {
                Log.outDebug(LogFilter.Spells, "Spell.EffectTriggerMissileSpell spell {0} tried to trigger unknown spell {1}", m_spellInfo.Id, triggered_spell_id);
                return;
            }

            SpellCastTargets targets = new();
            int? targetCount = null;
            int? targetIndex = null;
            if (effectHandleMode == SpellEffectHandleMode.HitTarget)
            {
                if (!spellInfo.NeedsToBeTriggeredByCaster(m_spellInfo))
                    return;

                targets.SetUnitTarget(unitTarget);
                targetCount = (int)GetUnitTargetCountForEffect(effectInfo.EffectIndex);
                targetIndex = GetUnitTargetIndexForEffect(unitTarget.GetGUID(), effectInfo.EffectIndex);
            }
            else //if (effectHandleMode == SpellEffectHandleMode.Hit)
            {
                if (spellInfo.NeedsToBeTriggeredByCaster(m_spellInfo) && effectInfo.GetProvidedTargetMask().HasAnyFlag(SpellCastTargetFlags.UnitMask))
                    return;

                if (spellInfo.GetExplicitTargetMask().HasAnyFlag(SpellCastTargetFlags.DestLocation))
                    targets.SetDst(m_targets);

                Unit unit = m_caster.ToUnit();
                if (unit != null)
                    targets.SetUnitTarget(unit);
                else
                {
                    GameObject go = m_caster.ToGameObject();
                    if (go != null)
                        targets.SetGOTarget(go);
                }
            }

            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask & ~(TriggerCastFlags.IgnorePowerCost | TriggerCastFlags.IgnoreReagentCost));
            args.SetOriginalCaster(m_originalCasterGUID);
            args.SetTriggeringSpell(this);
            args.SetCustomArg(m_customArg);
            // set basepoints for trigger with value effect
            if (effectInfo.Effect == SpellEffectName.TriggerMissileSpellWithValue)
                for (int i = 0; i < spellInfo.GetEffects().Count; ++i)
                    args.AddSpellMod(SpellValueMod.BasePoint0 + i, damage);

            if (targetCount.HasValue)
                args.AddSpellMod(SpellValueMod.ParentSpellTargetCount, targetCount.Value);

            if (targetIndex.HasValue)
                args.AddSpellMod(SpellValueMod.ParentSpellTargetIndex, targetIndex.Value);

            // original caster guid only for GO cast
            m_caster.CastSpell(targets, spellInfo.Id, args);
        }

        [SpellEffectHandler(SpellEffectName.ForceCast)]
        [SpellEffectHandler(SpellEffectName.ForceCastWithValue)]
        [SpellEffectHandler(SpellEffectName.ForceCast2)]
        void EffectForceCast()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            uint triggered_spell_id = effectInfo.TriggerSpell;
            if (triggered_spell_id == 0)
            {
                Log.outWarn(LogFilter.Spells, $"Spell::EffectForceCast: Spell {m_spellInfo.Id} [EffectIndex: {effectInfo.EffectIndex}] does not have triggered spell.");
                return;
            }

            // normal case
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(triggered_spell_id, GetCastDifficulty());
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
                    {
                        CastSpellExtraArgs args1 = new(TriggerCastFlags.FullMask);
                        args1.SetOriginalCaster(m_originalCasterGUID);
                        args1.SetTriggeringSpell(this);
                        args1.AddSpellMod(SpellValueMod.BasePoint0, damage);
                        unitTarget.CastSpell(unitTarget, spellInfo.Id, args1);
                        return;
                    }
                }
            }

            switch (spellInfo.Id)
            {
                case 72298: // Malleable Goo Summon
                    unitTarget.CastSpell(unitTarget, spellInfo.Id, new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                        .SetOriginalCaster(m_originalCasterGUID)
                        .SetTriggeringSpell(this));
                    return;
            }

            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask & ~(TriggerCastFlags.IgnorePowerCost | TriggerCastFlags.IgnoreReagentCost));
            args.SetTriggeringSpell(this);
            // set basepoints for trigger with value effect
            if (effectInfo.Effect == SpellEffectName.ForceCastWithValue)
                for (int i = 0; i < spellInfo.GetEffects().Count; ++i)
                    args.AddSpellMod(SpellValueMod.BasePoint0 + i, damage);

            unitTarget.CastSpell(m_caster, spellInfo.Id, args);
        }

        [SpellEffectHandler(SpellEffectName.TriggerSpell2)]
        void EffectTriggerRitualOfSummoning()
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            uint triggered_spell_id = effectInfo.TriggerSpell;
            if (triggered_spell_id == 0)
            {
                Log.outWarn(LogFilter.Spells, $"Spell::EffectTriggerRitualOfSummoning: Spell {m_spellInfo.Id} [EffectIndex: {effectInfo.EffectIndex}] does not have triggered spell.");
                return;
            }

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(triggered_spell_id, GetCastDifficulty());
            if (spellInfo == null)
            {
                Log.outError(LogFilter.Spells, $"EffectTriggerRitualOfSummoning of spell {m_spellInfo.Id}: triggering unknown spell id {triggered_spell_id}");
                return;
            }

            Finish();

            m_caster.CastSpell((Unit)null, spellInfo.Id, new CastSpellExtraArgs().SetTriggeringSpell(this));
        }

        void CalculateJumpSpeeds(SpellEffectInfo effInfo, float dist, out float speedXY, out float speedZ)
        {
            Unit unitCaster = GetUnitCasterForEffectHandlers();

            float multiplier = effInfo.Amplitude;
            if (multiplier <= 0.0f)
                multiplier = 1.0f;

            float minHeight = effInfo.MiscValue != 0 ? effInfo.MiscValue / 10.0f : 0.5f; // Lower bound is blizzlike
            float maxHeight = effInfo.MiscValueB != 0 ? effInfo.MiscValueB / 10.0f : 1000.0f; // Upper bound is unknown

            unitCaster.GetMotionMaster().CalculateJumpSpeeds(dist, UnitMoveType.Run, multiplier, minHeight, maxHeight, out speedXY, out speedZ);
        }

        [SpellEffectHandler(SpellEffectName.Jump)]
        void EffectJump()
        {
            if (effectHandleMode != SpellEffectHandleMode.LaunchTarget)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster == null)
                return;

            if (unitCaster.IsInFlight())
                return;

            if (unitTarget == null)
                return;

            float speedXY, speedZ;
            CalculateJumpSpeeds(effectInfo, unitCaster.GetExactDist2d(unitTarget), out speedXY, out speedZ);

            object facing = null;
            Unit target = m_targets.GetUnitTarget();
            if (target != null && m_spellInfo.HasAttribute(SpellAttr9.FaceUnitTargetUponCompletionOfJumpCharge))
                facing = target;

            JumpArrivalCastArgs arrivalCast = new();
            arrivalCast.SpellId = effectInfo.TriggerSpell;
            arrivalCast.Target = unitTarget.GetGUID();
            unitCaster.GetMotionMaster().MoveJump(unitTarget, speedXY, speedZ, EventId.Jump, facing, m_spellInfo.HasAttribute(SpellAttr9.JumpchargeNoFacingControl), arrivalCast);
        }

        [SpellEffectHandler(SpellEffectName.JumpDest)]
        void EffectJumpDest()
        {
            if (effectHandleMode != SpellEffectHandleMode.Launch)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster == null)
                return;

            if (unitCaster.IsInFlight())
                return;

            if (!m_targets.HasDst())
                return;

            float speedXY, speedZ;
            CalculateJumpSpeeds(effectInfo, unitCaster.GetExactDist2d(destTarget), out speedXY, out speedZ);

            object facing;
            Unit target = m_targets.GetUnitTarget();
            if (target != null && m_spellInfo.HasAttribute(SpellAttr9.FaceUnitTargetUponCompletionOfJumpCharge))
                facing = target;
            else
                facing = destTarget.GetOrientation();

            JumpArrivalCastArgs arrivalCast = new();
            arrivalCast.SpellId = effectInfo.TriggerSpell;
            unitCaster.GetMotionMaster().MoveJump(destTarget, speedXY, speedZ, EventId.Jump, facing, m_spellInfo.HasAttribute(SpellAttr9.JumpchargeNoFacingControl), arrivalCast);
        }

        TeleportToOptions GetTeleportOptions(WorldObject caster, Unit unitTarget, SpellDestination targetDest)
        {
            TeleportToOptions options = TeleportToOptions.None;
            if (caster == unitTarget)
                options |= TeleportToOptions.Spell;

            if (targetDest.Position.GetMapId() == unitTarget.GetMapId())
            {
                options |= TeleportToOptions.NotLeaveCombat | TeleportToOptions.NotUnSummonPet;

                if (unitTarget.GetTransGUID() == targetDest.TransportGUID)
                    options |= TeleportToOptions.NotLeaveTransport;
            }

            return options;
        }

        [SpellEffectHandler(SpellEffectName.TeleportUnits)]
        void EffectTeleportUnits()
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
            WorldLocation targetDest = new(destTarget);
            if (targetDest.GetMapId() == 0xFFFFFFFF)
                targetDest.SetMapId(unitTarget.GetMapId());

            if (targetDest.GetOrientation() == 0 && m_targets.GetUnitTarget() != null)
                targetDest.SetOrientation(m_targets.GetUnitTarget().GetOrientation());

            Player player = unitTarget.ToPlayer();
            if (player != null)
            {
                // Custom loading screen
                uint customLoadingScreenId = (uint)effectInfo.MiscValue;
                if (customLoadingScreenId != 0)
                    if (targetDest.GetMapId() != unitTarget.GetMapId() || !unitTarget.IsInDist2d(targetDest, PlayerConst.TeleportMinLoadScreenDistance))
                        player.SendPacket(new CustomLoadScreen(m_spellInfo.Id, customLoadingScreenId));

                TeleportToOptions options = GetTeleportOptions(m_caster, unitTarget, m_destTargets[effectInfo.EffectIndex]);

                player.TeleportTo(targetDest, options, null, m_spellInfo.Id);
            }
            else if (targetDest.GetMapId() == unitTarget.GetMapId())
                unitTarget.NearTeleportTo(targetDest, unitTarget == m_caster);
            else
                Log.outError(LogFilter.Spells, "Spell.EffectTeleportUnits - spellId {0} attempted to teleport creature to a different map.", m_spellInfo.Id);
        }

        [SpellEffectHandler(SpellEffectName.TeleportWithSpellVisualKitLoadingScreen)]
        void EffectTeleportUnitsWithVisualLoadingScreen()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            // If not exist data for dest location - return
            if (!m_targets.HasDst())
            {
                Log.outError(LogFilter.Spells, $"Spell::EffectTeleportUnitsWithVisualLoadingScreen - does not have a destination for spellId {m_spellInfo.Id}.");
                return;
            }

            // Init dest coordinates
            WorldLocation targetDest = new(destTarget);
            if (targetDest.GetMapId() == 0xFFFFFFFF)
                targetDest.SetMapId(unitTarget.GetMapId());

            if (targetDest.GetOrientation() == 0 && m_targets.GetUnitTarget() != null)
                targetDest.SetOrientation(m_targets.GetUnitTarget().GetOrientation());

            if (effectInfo.MiscValueB != 0)
            {
                Player playerTarget = unitTarget.ToPlayer();
                if (playerTarget != null)
                    playerTarget.SendPacket(new SpellVisualLoadScreen(effectInfo.MiscValueB, effectInfo.MiscValue));
            }

            TeleportToOptions options = GetTeleportOptions(m_caster, unitTarget, m_destTargets[effectInfo.EffectIndex]);
            unitTarget.m_Events.AddEventAtOffset(new DelayedSpellTeleportEvent(unitTarget, targetDest, options, m_spellInfo.Id), TimeSpan.FromMilliseconds(effectInfo.MiscValue));
        }

        [SpellEffectHandler(SpellEffectName.ApplyAura)]
        [SpellEffectHandler(SpellEffectName.ApplyAuraOnPet)]
        void EffectApplyAura()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (spellAura == null || unitTarget == null)
                return;

            // register target/effect on aura
            AuraApplication aurApp = spellAura.GetApplicationOfTarget(unitTarget.GetGUID());
            if (aurApp == null)
                aurApp = unitTarget._CreateAuraApplication(spellAura, 1u << (int)effectInfo.EffectIndex);
            else
                aurApp.AddEffectToApplyEffectMask(effectInfo.EffectIndex);
        }

        [SpellEffectHandler(SpellEffectName.UnlearnSpecialization)]
        void EffectUnlearnSpecialization()
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
        void EffectPowerDrain()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (effectInfo.MiscValue < 0 || effectInfo.MiscValue >= (byte)PowerType.Max)
                return;

            PowerType powerType = (PowerType)effectInfo.MiscValue;

            if (unitTarget == null || !unitTarget.IsAlive() || unitTarget.GetPowerType() != powerType || damage < 0)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            // add spell damage bonus
            if (unitCaster != null)
            {
                int bonus = unitCaster.SpellDamageBonusDone(unitTarget, m_spellInfo, damage, DamageEffectType.SpellDirect, effectInfo, 1, this);
                damage = (int)(bonus + (bonus * variance));
                damage = unitTarget.SpellDamageBonusTaken(unitCaster, m_spellInfo, damage, DamageEffectType.SpellDirect);
            }

            int newDamage = -(unitTarget.ModifyPower(powerType, -damage));

            // Don't restore from self drain
            float gainMultiplier = 0.0f;
            if (unitCaster != null && unitCaster != unitTarget)
            {
                gainMultiplier = effectInfo.CalcValueMultiplier(unitCaster, this);
                int gain = (int)(newDamage * gainMultiplier);

                unitCaster.EnergizeBySpell(unitCaster, m_spellInfo, gain, powerType);
            }
            ExecuteLogEffectTakeTargetPower(effectInfo.Effect, unitTarget, powerType, (uint)newDamage, gainMultiplier);
        }

        [SpellEffectHandler(SpellEffectName.SendEvent)]
        void EffectSendEvent()
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
                else if (corpseTarget != null)
                    target = corpseTarget;
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

            GameEvents.Trigger((uint)effectInfo.MiscValue, m_caster, target);
        }

        [SpellEffectHandler(SpellEffectName.PowerBurn)]
        void EffectPowerBurn()
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
            float dmgMultiplier = effectInfo.CalcValueMultiplier(GetUnitCasterForEffectHandlers(), this);

            // add log data before multiplication (need power amount, not damage)
            ExecuteLogEffectTakeTargetPower(effectInfo.Effect, unitTarget, powerType, (uint)newDamage, 0.0f);

            newDamage = (int)(newDamage * dmgMultiplier);

            m_damage += newDamage;
        }

        [SpellEffectHandler(SpellEffectName.Heal)]
        void EffectHeal()
        {
            if (effectHandleMode != SpellEffectHandleMode.LaunchTarget)
                return;

            if (unitTarget == null || !unitTarget.IsAlive() || damage < 0)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            // Skip if m_originalCaster not available
            if (unitCaster == null)
                return;

            int addhealth = damage;

            // Vessel of the Naaru (Vial of the Sunwell trinket)
            ///@todo: move this to scripts
            if (m_spellInfo.Id == 45064)
            {
                // Amount of heal - depends from stacked Holy Energy
                int damageAmount = 0;
                AuraEffect aurEff = unitCaster.GetAuraEffect(45062, 0);
                if (aurEff != null)
                {
                    damageAmount += aurEff.GetAmount();
                    unitCaster.RemoveAurasDueToSpell(45062);
                }

                addhealth += damageAmount;
            }
            // Death Pact - return pct of max health to caster
            else if (m_spellInfo.SpellFamilyName == SpellFamilyNames.Deathknight && m_spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00080000u))
                addhealth = unitCaster.SpellHealingBonusDone(unitTarget, m_spellInfo, (int)unitCaster.CountPctFromMaxHealth(damage), DamageEffectType.Heal, effectInfo, 1, this);
            else
            {
                int bonus = unitCaster.SpellHealingBonusDone(unitTarget, m_spellInfo, addhealth, DamageEffectType.Heal, effectInfo, 1, this);
                addhealth = (int)(bonus + (bonus * variance));
            }

            addhealth = unitTarget.SpellHealingBonusTaken(unitCaster, m_spellInfo, addhealth, DamageEffectType.Heal);

            // Remove Grievious bite if fully healed
            if (unitTarget.HasAura(48920) && ((uint)(unitTarget.GetHealth() + (ulong)addhealth) >= unitTarget.GetMaxHealth()))
                unitTarget.RemoveAura(48920);

            m_healing += addhealth;
        }

        [SpellEffectHandler(SpellEffectName.HealPct)]
        void EffectHealPct()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsAlive() || damage < 0)
                return;

            int heal = (int)unitTarget.CountPctFromMaxHealth(damage);
            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster != null)
            {
                heal = unitCaster.SpellHealingBonusDone(unitTarget, m_spellInfo, heal, DamageEffectType.Heal, effectInfo, 1, this);
                heal = unitTarget.SpellHealingBonusTaken(unitCaster, m_spellInfo, heal, DamageEffectType.Heal);
            }

            m_healing += heal;
        }

        [SpellEffectHandler(SpellEffectName.HealMechanical)]
        void EffectHealMechanical()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsAlive() || damage < 0)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            int heal = damage;
            if (unitCaster != null)
                heal = unitCaster.SpellHealingBonusDone(unitTarget, m_spellInfo, heal, DamageEffectType.Heal, effectInfo, 1, this);

            heal += (int)(heal * variance);
            if (unitCaster != null)
                heal = unitTarget.SpellHealingBonusTaken(unitCaster, m_spellInfo, heal, DamageEffectType.Heal);

            m_healing += heal;
        }

        [SpellEffectHandler(SpellEffectName.HealthLeech)]
        void EffectHealthLeech()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsAlive() || damage < 0)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            uint bonus = 0;
            if (unitCaster != null)
                bonus = (uint)unitCaster.SpellDamageBonusDone(unitTarget, m_spellInfo, damage, DamageEffectType.SpellDirect, effectInfo, 1, this);

            damage = (int)(bonus + (uint)(bonus * variance));

            if (unitCaster != null)
                damage = unitTarget.SpellDamageBonusTaken(unitCaster, m_spellInfo, damage, DamageEffectType.SpellDirect);

            Log.outDebug(LogFilter.Spells, "HealthLeech :{0}", damage);

            float healMultiplier = effectInfo.CalcValueMultiplier(unitCaster, this);

            m_damage += damage;

            DamageInfo damageInfo = new(unitCaster, unitTarget, (uint)damage, m_spellInfo, m_spellInfo.GetSchoolMask(), DamageEffectType.Direct, WeaponAttackType.BaseAttack);
            Unit.CalcAbsorbResist(damageInfo);
            uint absorb = damageInfo.GetAbsorb();
            damage -= (int)absorb;

            // get max possible damage, don't count overkill for heal
            uint healthGain = (uint)(-unitTarget.GetHealthGain(-damage) * healMultiplier);

            if (unitCaster != null && unitCaster.IsAlive())
            {
                healthGain = (uint)unitCaster.SpellHealingBonusDone(unitCaster, m_spellInfo, (int)healthGain, DamageEffectType.Heal, effectInfo, 1, this);
                healthGain = (uint)unitCaster.SpellHealingBonusTaken(unitCaster, m_spellInfo, (int)healthGain, DamageEffectType.Heal);

                HealInfo healInfo = new(unitCaster, unitCaster, healthGain, m_spellInfo, m_spellSchoolMask);
                unitCaster.HealBySpell(healInfo);
            }
        }

        public void DoCreateItem(uint itemId, ItemContext context = 0, List<uint> bonusListIds = null)
        {
            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            Player player = unitTarget.ToPlayer();

            uint newitemid = itemId;
            ItemTemplate pProto = Global.ObjectMgr.GetItemTemplate(newitemid);
            if (pProto == null)
            {
                player.SendEquipError(InventoryResult.ItemNotFound);
                return;
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
            uint perfectItemType = itemId;
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
            List<ItemPosCount> dest = new();
            uint no_space;
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
                Item pItem = player.StoreNewItem(dest, newitemid, true, ItemEnchantmentManager.GenerateItemRandomBonusListId(newitemid), null, context, bonusListIds);
                if (pItem != null)
                {
                    // set the "Crafted by ..." property of the item
                    if (pItem.GetTemplate().HasSignature())
                        pItem.SetCreator(player.GetGUID());

                    // send info to the client
                    player.SendNewItem(pItem, num_to_add, true, true);

                    if (pItem.GetQuality() > ItemQuality.Epic || (pItem.GetQuality() == ItemQuality.Epic && pItem.GetItemLevel(player) >= GuildConst.MinNewsItemLevel))
                    {
                        Guild guild = player.GetGuild();
                        if (guild != null)
                            guild.AddGuildNews(GuildNews.ItemCrafted, player.GetGUID(), 0, pProto.GetId());
                    }
                }

                // we succeeded in creating at least one item, so a levelup is possible
                if (m_CastItem == null)
                    player.UpdateCraftSkill(m_spellInfo);
            }
        }

        [SpellEffectHandler(SpellEffectName.CreateItem)]
        void EffectCreateItem()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            DoCreateItem(effectInfo.ItemType, m_spellInfo.HasAttribute(SpellAttr0.IsTradeskill) ? ItemContext.TradeSkill : ItemContext.None);
            ExecuteLogEffectCreateItem(effectInfo.Effect, effectInfo.ItemType);
        }

        [SpellEffectHandler(SpellEffectName.CreateLoot)]
        void EffectCreateItem2()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            Player player = unitTarget.ToPlayer();

            ItemContext context = m_spellInfo.HasAttribute(SpellAttr0.IsTradeskill) ? ItemContext.TradeSkill : ItemContext.None;

            // Pick a random item from spell_loot_template
            if (m_spellInfo.IsLootCrafting())
            {
                player.AutoStoreLoot(m_spellInfo.Id, LootStorage.Spell, context, false, true);
                if (m_CastItem == null)
                    player.UpdateCraftSkill(m_spellInfo);
            }
            else // If there's no random loot entries for this spell, pick the item associated with this spell
            {
                uint itemId = effectInfo.ItemType;
                if (itemId != 0)
                    DoCreateItem(itemId, context);
            }

            // @todo ExecuteLogEffectCreateItem(i, GetEffect(i].ItemType);
        }

        [SpellEffectHandler(SpellEffectName.CreateRandomItem)]
        void EffectCreateRandomItem()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;
            Player player = unitTarget.ToPlayer();

            // create some random items
            player.AutoStoreLoot(m_spellInfo.Id, LootStorage.Spell, m_spellInfo.HasAttribute(SpellAttr0.IsTradeskill) ? ItemContext.TradeSkill : ItemContext.None);
            // @todo ExecuteLogEffectCreateItem(i, GetEffect(i].ItemType);
        }

        [SpellEffectHandler(SpellEffectName.PersistentAreaAura)]
        void EffectPersistentAA()
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster == null)
                return;

            // Caster not in world, might be spell triggered from aura removal
            if (!unitCaster.IsInWorld)
                return;

            // only handle at last effect
            for (uint i = effectInfo.EffectIndex + 1; i < m_spellInfo.GetEffects().Count; ++i)
                if (m_spellInfo.GetEffect(i).IsEffect(SpellEffectName.PersistentAreaAura))
                    return;

            Cypher.Assert(dynObjAura == null);

            float radius = 0.0f;
            for (uint i = 0; i <= effectInfo.EffectIndex; ++i)
            {
                SpellEffectInfo spellEffectInfo = m_spellInfo.GetEffect(i);
                if (spellEffectInfo.IsEffect(SpellEffectName.PersistentAreaAura))
                    radius = Math.Max(radius, spellEffectInfo.CalcRadius(unitCaster));
            }

            DynamicObject dynObj = new(false);
            if (!dynObj.CreateDynamicObject(unitCaster.GetMap().GenerateLowGuid(HighGuid.DynamicObject), unitCaster, m_spellInfo, destTarget, radius, DynamicObjectType.AreaSpell, m_SpellVisual))
            {
                dynObj.Dispose();
                return;
            }

            AuraCreateInfo createInfo = new(m_castId, m_spellInfo, GetCastDifficulty(), SpellConst.MaxEffectMask, dynObj);
            createInfo.SetCaster(unitCaster);
            createInfo.SetBaseAmount(m_spellValue.EffectBasePoints);
            createInfo.SetCastItem(m_castItemGUID, m_castItemEntry, m_castItemLevel);

            Aura aura = Aura.TryCreate(createInfo);
            if (aura != null)
            {
                dynObjAura = aura.ToDynObjAura();
                dynObjAura._RegisterForTargets();
            }
            else
                return;

            Cypher.Assert(dynObjAura.GetDynobjOwner() != null);
            for (uint i = 0; i < m_spellInfo.GetEffects().Count; ++i)
                if (m_spellInfo.GetEffect(i).IsEffect(SpellEffectName.PersistentAreaAura))
                    dynObjAura._ApplyEffectForTargets(i);

            if (m_spellInfo.IsChanneled())
                unitCaster.AddChannelObject(dynObj.GetGUID());
        }

        [SpellEffectHandler(SpellEffectName.Energize)]
        void EffectEnergize()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster == null || unitTarget == null)
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
                    damage -= 10 * (int)Math.Max(0, Math.Min(30, unitCaster.GetLevel() - 60));
                    break;
                case 24532:                                         // Burst of Energy
                                                                    // Instantly increases your energy by ${60-4*$max(0,$min(15,$PL-60))}.
                    damage -= 4 * (int)Math.Max(0, Math.Min(15, unitCaster.GetLevel() - 60));
                    break;
                case 67490:                                         // Runic Mana Injector (mana gain increased by 25% for engineers - 3.2.0 patch change)
                {
                    Player player = unitCaster.ToPlayer();
                    if (player != null)
                        if (player.HasSkill(SkillType.Engineering))
                            MathFunctions.AddPct(ref damage, 25);
                    break;
                }
                default:
                    break;
            }

            unitCaster.EnergizeBySpell(unitTarget, m_spellInfo, damage, power);
        }

        [SpellEffectHandler(SpellEffectName.EnergizePct)]
        void EffectEnergizePct()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster == null || unitTarget == null)
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
            unitCaster.EnergizeBySpell(unitTarget, m_spellInfo, gain, power);
        }

        [SpellEffectHandler(SpellEffectName.OpenLock)]
        void EffectOpenLock()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!m_caster.IsTypeId(TypeId.Player))
            {
                Log.outDebug(LogFilter.Spells, "WORLD: Open Lock - No Player Caster!");
                return;
            }

            Player player = m_caster.ToPlayer();

            uint lockId;
            ObjectGuid guid;

            // Get lockId
            if (gameObjTarget != null)
            {
                GameObjectTemplate goInfo = gameObjTarget.GetGoInfo();

                if (goInfo.GetNoDamageImmune() != 0 && player.HasUnitFlag(UnitFlags.Immune))
                    return;

                if (m_spellInfo.Id == 1842 && gameObjTarget.GetGoInfo().type == GameObjectTypes.Trap && gameObjTarget.GetOwner() != null)
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

            SpellCastResult res = CanOpenLock(effectInfo, lockId, ref skillId, ref reqSkillValue, ref skillValue);
            if (res != SpellCastResult.SpellCastOk)
            {
                SendCastResult(res);
                return;
            }

            if (gameObjTarget != null)
                gameObjTarget.Use(player, true);
            else if (itemTarget != null)
            {
                itemTarget.SetItemFlag(ItemFieldFlags.Unlocked);
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
                        if (!gameObjTarget.IsInSkillupList(player.GetGUID()))
                        {
                            player.UpdateGatherSkill((uint)skillId, pureSkillValue, (uint)reqSkillValue, 1, gameObjTarget);
                            gameObjTarget.AddToSkillupList(player.GetGUID());
                        }
                    }
                    else if (itemTarget != null)
                    {
                        // Do one skill-up
                        player.UpdateGatherSkill(skillId, pureSkillValue, (uint)reqSkillValue);
                    }
                }
            }
            ExecuteLogEffectOpenLock(effectInfo.Effect, gameObjTarget != null ? gameObjTarget : (WorldObject)itemTarget);
        }

        [SpellEffectHandler(SpellEffectName.SummonChangeItem)]
        void EffectSummonChangeItem()
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

            Item pNewItem = Item.CreateItem(newitemid, 1, m_CastItem.GetContext(), player);
            if (pNewItem == null)
                return;

            for (var j = EnchantmentSlot.Perm; j <= EnchantmentSlot.Temp; ++j)
                if (m_CastItem.GetEnchantmentId(j) != 0)
                    pNewItem.SetEnchantment(j, m_CastItem.GetEnchantmentId(j), m_CastItem.GetEnchantmentDuration(j), (uint)m_CastItem.GetEnchantmentCharges(j));

            if (m_CastItem.m_itemData.Durability < m_CastItem.m_itemData.MaxDurability)
            {
                double lossPercent = 1 - m_CastItem.m_itemData.Durability / m_CastItem.m_itemData.MaxDurability;
                player.DurabilityLoss(pNewItem, lossPercent);
            }

            if (player.IsInventoryPos(pos))
            {
                List<ItemPosCount> dest = new();
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
                List<ItemPosCount> dest = new();
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
        void EffectProficiency()
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
        void EffectSummonType()
        {
            if (effectHandleMode != SpellEffectHandleMode.Launch)
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

            WorldObject caster = m_caster;
            if (m_originalCaster != null)
                caster = m_originalCaster;

            ObjectGuid privateObjectOwner = caster.GetGUID();
            if (!properties.HasFlag(SummonPropertiesFlags.OnlyVisibleToSummoner | SummonPropertiesFlags.OnlyVisibleToSummonerGroup))
                privateObjectOwner = ObjectGuid.Empty;

            if (caster.IsPrivateObject())
                privateObjectOwner = caster.GetPrivateObjectOwner();

            if (properties.HasFlag(SummonPropertiesFlags.OnlyVisibleToSummonerGroup))
                if (caster.IsPlayer() && m_originalCaster.ToPlayer().GetGroup() != null)
                    privateObjectOwner = caster.ToPlayer().GetGroup().GetGUID();

            TimeSpan duration = TimeSpan.FromMilliseconds(m_spellInfo.CalcDuration(caster));

            Unit unitCaster = GetUnitCasterForEffectHandlers();

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
                    if (properties.HasFlag(SummonPropertiesFlags.JoinSummonerSpawnGroup))
                    {
                        SummonGuardian(effectInfo, entry, properties, numSummons, privateObjectOwner);
                        break;
                    }
                    switch (properties.Title)
                    {
                        case SummonTitle.Pet:
                        case SummonTitle.Guardian:
                        case SummonTitle.Runeblade:
                        case SummonTitle.Minion:
                            SummonGuardian(effectInfo, entry, properties, numSummons, privateObjectOwner);
                            break;
                        // Summons a vehicle, but doesn't force anyone to enter it (see SUMMON_CATEGORY_VEHICLE)
                        case SummonTitle.Vehicle:
                        case SummonTitle.Mount:
                        {
                            if (unitCaster == null)
                                return;

                            summon = unitCaster.GetMap().SummonCreature(entry, destTarget, properties, duration, unitCaster, m_spellInfo.Id);
                            break;
                        }
                        case SummonTitle.LightWell:
                        case SummonTitle.Totem:
                        {
                            if (unitCaster == null)
                                return;

                            summon = unitCaster.GetMap().SummonCreature(entry, destTarget, properties, duration, unitCaster, m_spellInfo.Id, 0, privateObjectOwner);
                            if (summon == null || !summon.IsTotem())
                                return;

                            if (damage != 0)                                            // if not spell info, DB values used
                            {
                                summon.SetMaxHealth((uint)damage);
                                summon.SetHealth((uint)damage);
                            }
                            break;
                        }
                        case SummonTitle.Companion:
                        {
                            if (unitCaster == null)
                                return;

                            summon = unitCaster.GetMap().SummonCreature(entry, destTarget, properties, duration, unitCaster, m_spellInfo.Id, 0, privateObjectOwner);
                            if (summon == null || !summon.HasUnitTypeMask(UnitTypeMask.Minion))
                                return;

                            summon.SetImmuneToAll(true);

                            break;
                        }
                        default:
                        {
                            float radius = effectInfo.CalcRadius();

                            TempSummonType summonType = TempSummonType.TimedDespawn;
                            if (duration == TimeSpan.Zero)
                                summonType = TempSummonType.DeadDespawn;
                            else if (duration == TimeSpan.FromMilliseconds(-1))
                                summonType = TempSummonType.ManualDespawn;
                            else if (properties.HasFlag(SummonPropertiesFlags.UseDemonTimeout))
                                summonType = TempSummonType.TimedDespawnOutOfCombat;

                            for (uint count = 0; count < numSummons; ++count)
                            {
                                Position pos;
                                if (count == 0)
                                    pos = destTarget.GetPosition();
                                else
                                    // randomize position for multiple summons
                                    pos = caster.GetRandomPoint(destTarget, radius);

                                summon = caster.GetMap().SummonCreature(entry, pos, properties, duration, unitCaster, m_spellInfo.Id, 0, privateObjectOwner);
                                if (summon == null)
                                    continue;

                                summon.SetTempSummonType(summonType);
                                if (properties.Control == SummonCategory.Ally)
                                    summon.SetOwnerGUID(caster.GetGUID());
                                else if (properties.Control == SummonCategory.Wild && caster.IsPlayer()) // there might be more conditions involved
                                    summon.SetDemonCreatorGUID(caster.GetGUID());

                                ExecuteLogEffectSummonObject(effectInfo.Effect, summon);
                            }
                            return;
                        }
                    }//switch
                    break;
                case SummonCategory.Pet:
                    SummonGuardian(effectInfo, entry, properties, numSummons, privateObjectOwner);
                    break;
                case SummonCategory.Puppet:
                {
                    if (unitCaster == null)
                        return;

                    summon = unitCaster.GetMap().SummonCreature(entry, destTarget, properties, duration, unitCaster, m_spellInfo.Id, 0, privateObjectOwner);
                    break;
                }
                case SummonCategory.PossessedVehicle:
                case SummonCategory.Vehicle:
                {
                    if (unitCaster == null)
                        return;

                    // Summoning spells (usually triggered by npc_spellclick) that spawn a vehicle and that cause the clicker
                    // to cast a ride vehicle spell on the summoned unit.
                    summon = unitCaster.GetMap().SummonCreature(entry, destTarget, properties, duration, unitCaster, m_spellInfo.Id);
                    if (summon == null || !summon.IsVehicle())
                        return;

                    // The spell that this effect will trigger. It has SPELL_AURA_CONTROL_VEHICLE
                    uint spellId = SharedConst.VehicleSpellRideHardcoded;
                    int basePoints = effectInfo.CalcValue();
                    if (basePoints > SharedConst.MaxVehicleSeats)
                    {
                        SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo((uint)basePoints, GetCastDifficulty());
                        if (spellInfo != null && spellInfo.HasAura(AuraType.ControlVehicle))
                            spellId = spellInfo.Id;
                    }

                    CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                    args.SetTriggeringSpell(this);

                    // if we have small value, it indicates seat position
                    if (basePoints > 0 && basePoints < SharedConst.MaxVehicleSeats)
                        args.AddSpellMod(SpellValueMod.BasePoint0, basePoints);

                    unitCaster.CastSpell(summon, spellId, args);
                    break;
                }
            }

            if (summon != null)
            {
                summon.SetCreatorGUID(caster.GetGUID());
                ExecuteLogEffectSummonObject(effectInfo.Effect, summon);
            }
        }

        [SpellEffectHandler(SpellEffectName.LearnSpell)]
        void EffectLearnSpell()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            if (!unitTarget.IsTypeId(TypeId.Player))
            {
                if (unitTarget.IsPet())
                    EffectLearnPetSpell();
                return;
            }

            Player player = unitTarget.ToPlayer();

            if (m_CastItem != null && effectInfo.TriggerSpell == 0)
            {
                foreach (var itemEffect in m_CastItem.GetEffects())
                {
                    if (itemEffect.TriggerType != ItemSpelltriggerType.OnLearn)
                        continue;

                    bool dependent = false;

                    var speciesEntry = BattlePetMgr.GetBattlePetSpeciesBySpell((uint)itemEffect.SpellID);
                    if (speciesEntry != null)
                    {
                        player.GetSession().GetBattlePetMgr().AddPet(speciesEntry.Id, BattlePetMgr.SelectPetDisplay(speciesEntry), BattlePetMgr.RollPetBreed(speciesEntry.Id), BattlePetMgr.GetDefaultPetQuality(speciesEntry.Id));
                        // If the spell summons a battle pet, we fake that it has been learned and the battle pet is added
                        // marking as dependent prevents saving the spell to database (intended)
                        dependent = true;
                    }

                    player.LearnSpell((uint)itemEffect.SpellID, dependent);
                }
            }

            if (effectInfo.TriggerSpell != 0)
            {
                player.LearnSpell(effectInfo.TriggerSpell, false);
                Log.outDebug(LogFilter.Spells, $"Spell: {player.GetGUID()} has learned spell {effectInfo.TriggerSpell} from {m_caster.GetGUID()}");
            }
        }

        [SpellEffectHandler(SpellEffectName.Dispel)]
        void EffectDispel()
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
            List<DispelableAura> successList = new();

            DispelFailed dispelFailed = new();
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
                    var successAura = successList.Find(dispelAura =>
                    {
                        if (dispelAura.GetAura().GetId() == dispelableAura.GetAura().GetId() && dispelAura.GetAura().GetCaster() == dispelableAura.GetAura().GetCaster())
                            return true;

                        return false;
                    });

                    byte dispelledCharges = 1;
                    if (dispelableAura.GetAura().GetSpellInfo().HasAttribute(SpellAttr1.DispelAllStacks))
                        dispelledCharges = dispelableAura.GetDispelCharges();

                    if (successAura == null)
                        successList.Add(new DispelableAura(dispelableAura.GetAura(), 0, dispelledCharges));
                    else
                        successAura.IncrementCharges();

                    if (!dispelableAura.DecrementCharge(dispelledCharges))
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

            SpellDispellLog spellDispellLog = new();
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

                unitTarget.RemoveAurasDueToSpellByDispel(dispelableAura.GetAura().GetId(), m_spellInfo.Id, dispelableAura.GetAura().GetCasterGUID(), m_caster, dispelableAura.GetDispelCharges());

                spellDispellLog.DispellData.Add(dispellData);
            }
            m_caster.SendMessageToSet(spellDispellLog, true);

            CallScriptSuccessfulDispel(effectInfo.EffectIndex);

            m_UniqueTargetInfo.Find(p => p.TargetGUID == unitTarget.GetGUID()).ProcHitMask |= ProcFlagsHit.Dispel;
        }

        [SpellEffectHandler(SpellEffectName.DualWield)]
        void EffectDualWield()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            unitTarget.SetCanDualWield(true);
            if (unitTarget.IsTypeId(TypeId.Unit))
                unitTarget.ToCreature().UpdateDamagePhysical(WeaponAttackType.OffAttack);
        }

        [SpellEffectHandler(SpellEffectName.Distract)]
        void EffectDistract()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            // Check for possible target
            if (unitTarget == null || unitTarget.IsEngaged())
                return;

            // target must be OK to do this
            if (unitTarget.HasUnitState(UnitState.Confused | UnitState.Stunned | UnitState.Fleeing))
                return;

            unitTarget.GetMotionMaster().MoveDistract((uint)(damage * Time.InMilliseconds), unitTarget.GetAbsoluteAngle(destTarget));
        }

        [SpellEffectHandler(SpellEffectName.Pickpocket)]
        void EffectPickPocket()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player player = m_caster.ToPlayer();
            if (player == null)
                return;

            Creature creature = unitTarget?.ToCreature();
            if (creature == null)
                return;

            if (creature.CanGeneratePickPocketLoot())
            {
                creature.StartPickPocketRefillTimer();

                creature._loot = new Loot(creature.GetMap(), creature.GetGUID(), LootType.Pickpocketing, null);
                uint lootid = creature.GetCreatureDifficulty().PickPocketLootID;
                if (lootid != 0)
                    creature._loot.FillLoot(lootid, LootStorage.Pickpocketing, player, true);

                // Generate extra money for pick pocket loot
                uint a = RandomHelper.URand(0, creature.GetLevel() / 2);
                uint b = RandomHelper.URand(0, player.GetLevel() / 2);
                creature._loot.gold = (uint)(10 * (a + b) * WorldConfig.GetFloatValue(WorldCfg.RateDropMoney));
            }
            else if (creature._loot != null)
            {
                if (creature._loot.loot_type == LootType.Pickpocketing && creature._loot.IsLooted())
                    player.SendLootError(creature._loot.GetGUID(), creature.GetGUID(), LootError.AlreadPickPocketed);

                return;
            }

            player.SendLoot(creature._loot);
        }

        [SpellEffectHandler(SpellEffectName.AddFarsight)]
        void EffectAddFarsight()
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Player player = m_caster.ToPlayer();
            if (player == null)
                return;

            float radius = effectInfo.CalcRadius();
            int duration = m_spellInfo.CalcDuration(m_caster);
            // Caster not in world, might be spell triggered from aura removal
            if (!player.IsInWorld)
                return;

            DynamicObject dynObj = new(true);
            if (!dynObj.CreateDynamicObject(player.GetMap().GenerateLowGuid(HighGuid.DynamicObject), player, m_spellInfo, destTarget, radius, DynamicObjectType.FarsightFocus, m_SpellVisual))
                return;

            dynObj.SetDuration(duration);
            dynObj.SetCasterViewpoint();
        }

        [SpellEffectHandler(SpellEffectName.UntrainTalents)]
        void EffectUntrainTalents()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || m_caster.IsTypeId(TypeId.Player))
                return;

            ObjectGuid guid = m_caster.GetGUID();
            if (!guid.IsEmpty()) // the trainer is the caster
                unitTarget.ToPlayer().SendRespecWipeConfirm(guid, unitTarget.ToPlayer().GetNextResetTalentsCost(), SpecResetType.Talents);
        }

        [SpellEffectHandler(SpellEffectName.TeleportUnitsFaceCaster)]
        void EffectTeleUnitsFaceCaster()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            if (unitTarget.IsInFlight())
                return;

            if (m_targets.HasDst())
                unitTarget.NearTeleportTo(destTarget.GetPositionX(), destTarget.GetPositionY(), destTarget.GetPositionZ(), destTarget.GetAbsoluteAngle(m_caster), unitTarget == m_caster);
        }

        [SpellEffectHandler(SpellEffectName.SkillStep)]
        void EffectLearnSkill()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player playerTarget = unitTarget?.ToPlayer();
            if (playerTarget == null)
                return;

            if (damage < 1)
                return;

            uint skillid = (uint)effectInfo.MiscValue;

            SkillRaceClassInfoRecord rcEntry = Global.DB2Mgr.GetSkillRaceClassInfo(skillid, playerTarget.GetRace(), playerTarget.GetClass());
            if (rcEntry == null)
                return;

            SkillTiersEntry tier = Global.ObjectMgr.GetSkillTier(rcEntry.SkillTierID);
            if (tier == null)
                return;

            ushort skillval = Math.Max((ushort)1, playerTarget.GetPureSkillValue(skillid));
            ushort maxSkillVal = (ushort)tier.GetValueForTierIndex(damage - 1);

            if (rcEntry.HasFlag(SkillRaceClassInfoFlags.AlwaysMaxValue))
                skillval = maxSkillVal;

            playerTarget.SetSkill(skillid, (uint)damage, skillval, maxSkillVal);
        }

        [SpellEffectHandler(SpellEffectName.PlayMovie)]
        void EffectPlayMovie()
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
        void EffectTradeSkill()
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
        void EffectEnchantItemPerm()
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
                damage = 1;
                DoCreateItem(effectInfo.ItemType, m_spellInfo.HasAttribute(SpellAttr0.IsTradeskill) ? ItemContext.TradeSkill : ItemContext.None);
                itemTarget = null;
                m_targets.SetItemTarget(null);
            }
            else
            {
                // do not increase skill if vellum used
                if (m_CastItem == null)
                    player.UpdateCraftSkill(m_spellInfo);

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
        void EffectEnchantItemPrismatic()
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
        void EffectEnchantItemTmp()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (itemTarget == null)
                return;

            Player player = m_caster.ToPlayer();
            if (player == null)
                return;

            uint enchant_id = (uint)effectInfo.MiscValue;
            if (enchant_id == 0)
            {
                Log.outError(LogFilter.Spells, "Spell {0} Effect {1} (SPELL_EFFECT_ENCHANT_ITEM_TEMPORARY) have 0 as enchanting id", m_spellInfo.Id, effectInfo.EffectIndex);
                return;
            }

            SpellItemEnchantmentRecord pEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);
            if (pEnchant == null)
            {
                Log.outError(LogFilter.Spells, "Spell {0} Effect {1} (SPELL_EFFECT_ENCHANT_ITEM_TEMPORARY) have not existed enchanting id {2}", m_spellInfo.Id, effectInfo.EffectIndex, enchant_id);
                return;
            }

            // select enchantment duration
            uint duration = (uint)pEnchant.Duration;

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
        void EffectTameCreature()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster == null || !unitCaster.GetPetGUID().IsEmpty())
                return;

            if (unitTarget == null)
                return;

            if (!unitTarget.IsTypeId(TypeId.Unit))
                return;

            Creature creatureTarget = unitTarget.ToCreature();

            if (creatureTarget.IsPet())
                return;

            if (unitCaster.GetClass() != Class.Hunter)
                return;

            // cast finish successfully
            Finish();

            Pet pet = unitCaster.CreateTamedPetFrom(creatureTarget, m_spellInfo.Id);
            if (pet == null)                                               // in very specific state like near world end/etc.
                return;

            // "kill" original creature
            creatureTarget.DespawnOrUnsummon();

            uint level = (creatureTarget.GetLevelForTarget(m_caster) < (m_caster.GetLevelForTarget(creatureTarget) - 5)) ? (m_caster.GetLevelForTarget(creatureTarget) - 5) : creatureTarget.GetLevelForTarget(m_caster);

            // prepare visual effect for levelup
            pet.SetLevel(level - 1);

            // add to world
            pet.GetMap().AddToMap(pet.ToCreature());

            // visual effect for levelup
            pet.SetLevel(level);

            // caster have pet now
            unitCaster.SetMinion(pet, true);

            if (m_caster.IsTypeId(TypeId.Player))
            {
                pet.SavePetToDB(PetSaveMode.AsCurrent);
                unitCaster.ToPlayer().PetSpellInitialize();
            }
        }

        [SpellEffectHandler(SpellEffectName.SummonPet)]
        void EffectSummonPet()
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Player owner = null;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster != null)
            {
                owner = unitCaster.ToPlayer();
                if (owner == null && unitCaster.IsTotem())
                    owner = unitCaster.GetCharmerOrOwnerPlayerOrPlayerItself();
            }

            uint petentry = (uint)effectInfo.MiscValue;

            if (owner == null)
            {
                SummonPropertiesRecord properties = CliDB.SummonPropertiesStorage.LookupByKey(67);
                if (properties != null)
                    SummonGuardian(effectInfo, petentry, properties, 1, ObjectGuid.Empty);
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
                    owner.GetClosePoint(out px, out py, out pz, OldSummon.GetCombatReach());

                    OldSummon.NearTeleportTo(px, py, pz, OldSummon.GetOrientation());

                    if (owner.IsTypeId(TypeId.Player) && OldSummon.IsControlled())
                        owner.ToPlayer().PetSpellInitialize();

                    return;
                }

                if (owner.IsTypeId(TypeId.Player))
                    owner.ToPlayer().RemovePet(OldSummon, PetSaveMode.NotInSlot, false);
                else
                    return;
            }

            PetSaveMode? petSlot = null;
            if (petentry == 0)
                petSlot = (PetSaveMode)damage;

            float x, y, z;
            owner.GetClosePoint(out x, out y, out z, owner.GetCombatReach());
            Pet pet = owner.SummonPet(petentry, petSlot, x, y, z, owner.Orientation, 0, out bool isNew);
            if (pet == null)
                return;

            if (isNew)
            {
                if (m_caster.IsCreature())
                {
                    if (m_caster.ToCreature().IsTotem())
                        pet.SetReactState(ReactStates.Aggressive);
                    else
                        pet.SetReactState(ReactStates.Defensive);
                }

                pet.SetCreatedBySpell(m_spellInfo.Id);

                // generate new name for summon pet
                string new_name = Global.ObjectMgr.GeneratePetName(petentry);
                if (!string.IsNullOrEmpty(new_name))
                    pet.SetName(new_name);
            }

            ExecuteLogEffectSummonObject(effectInfo.Effect, pet);
        }

        [SpellEffectHandler(SpellEffectName.LearnPetSpell)]
        void EffectLearnPetSpell()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            if (unitTarget.ToPlayer() != null)
            {
                EffectLearnSpell();
                return;
            }
            Pet pet = unitTarget.ToPet();
            if (pet == null)
                return;

            SpellInfo learn_spellproto = Global.SpellMgr.GetSpellInfo(effectInfo.TriggerSpell, Difficulty.None);
            if (learn_spellproto == null)
                return;

            pet.LearnSpell(learn_spellproto.Id);
            pet.SavePetToDB(PetSaveMode.AsCurrent);
            pet.GetOwner().PetSpellInitialize();
        }

        [SpellEffectHandler(SpellEffectName.AttackMe)]
        void EffectTaunt()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster == null)
                return;

            // this effect use before aura Taunt apply for prevent taunt already attacking target
            // for spell as marked "non effective at already attacking target"
            if (unitTarget == null || unitTarget.IsTotem())
            {
                SendCastResult(SpellCastResult.DontReport);
                return;
            }

            // Hand of Reckoning can hit some entities that can't have a threat list (including players' pets)
            if (m_spellInfo.Id == 62124)
                if (!unitTarget.IsPlayer() && unitTarget.GetTarget() != unitCaster.GetGUID())
                    unitCaster.CastSpell(unitTarget, 67485, true);

            if (!unitTarget.CanHaveThreatList())
            {
                SendCastResult(SpellCastResult.DontReport);
                return;
            }

            ThreatManager mgr = unitTarget.GetThreatManager();
            if (mgr.GetCurrentVictim() == unitCaster)
            {
                SendCastResult(SpellCastResult.DontReport);
                return;
            }

            if (!mgr.IsThreatListEmpty())
                // Set threat equal to highest threat currently on target
                mgr.MatchUnitThreatToHighestThreat(unitCaster);
        }

        [SpellEffectHandler(SpellEffectName.WeaponDamageNoSchool)]
        [SpellEffectHandler(SpellEffectName.WeaponPercentDamage)]
        [SpellEffectHandler(SpellEffectName.WeaponDamage)]
        [SpellEffectHandler(SpellEffectName.NormalizedWeaponDmg)]
        void EffectWeaponDmg()
        {
            if (effectHandleMode != SpellEffectHandleMode.LaunchTarget)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster == null)
                return;

            if (unitTarget == null || !unitTarget.IsAlive())
                return;

            // multiple weapon dmg effect workaround
            // execute only the last weapon damage
            // and handle all effects at once
            for (var j = effectInfo.EffectIndex + 1; j < m_spellInfo.GetEffects().Count; ++j)
            {
                switch (m_spellInfo.GetEffect(j).Effect)
                {
                    case SpellEffectName.WeaponDamage:
                    case SpellEffectName.WeaponDamageNoSchool:
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
                case SpellFamilyNames.Shaman:
                {
                    // Skyshatter Harness item set bonus
                    // Stormstrike
                    AuraEffect aurEff = unitCaster.IsScriptOverriden(m_spellInfo, 5634);
                    if (aurEff != null)
                        unitCaster.CastSpell((WorldObject)null, 38430, new CastSpellExtraArgs(aurEff));
                    break;
                }
            }

            bool normalized = false;
            float weaponDamagePercentMod = 1.0f;
            foreach (var spellEffectInfo in m_spellInfo.GetEffects())
            {
                switch (spellEffectInfo.Effect)
                {
                    case SpellEffectName.WeaponDamage:
                    case SpellEffectName.WeaponDamageNoSchool:
                        fixed_bonus += CalculateDamage(spellEffectInfo, unitTarget);
                        break;
                    case SpellEffectName.NormalizedWeaponDmg:
                        fixed_bonus += CalculateDamage(spellEffectInfo, unitTarget);
                        normalized = true;
                        break;
                    case SpellEffectName.WeaponPercentDamage:
                        MathFunctions.ApplyPct(ref weaponDamagePercentMod, CalculateDamage(spellEffectInfo, unitTarget));
                        break;
                    default:
                        break;                                      // not weapon damage effect, just skip
                }
            }

            // if (addPctMods) { percent mods are added in Unit::CalculateDamage } else { percent mods are added in Unit::MeleeDamageBonusDone }
            // this distinction is neccessary to properly inform the client about his autoattack damage values from Script_UnitDamage
            bool addPctMods = !m_spellInfo.HasAttribute(SpellAttr6.IgnoreCasterDamageModifiers) && m_spellSchoolMask.HasAnyFlag(SpellSchoolMask.Normal);
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

                float weapon_total_pct = unitCaster.GetPctModifierValue(unitMod, UnitModifierPctType.Total);
                if (fixed_bonus != 0)
                    fixed_bonus = (int)(fixed_bonus * weapon_total_pct);
                if (spell_bonus != 0)
                    spell_bonus = (int)(spell_bonus * weapon_total_pct);
            }

            uint weaponDamage = unitCaster.CalculateDamage(m_attackType, normalized, addPctMods);
            Mechanics mechanic = Mechanics.None;

            // Sequence is important
            foreach (var spellEffectInfo in m_spellInfo.GetEffects())
            {
                // We assume that a spell have at most one fixed_bonus
                // and at most one weaponDamagePercentMod
                switch (spellEffectInfo.Effect)
                {
                    case SpellEffectName.WeaponDamage:
                    case SpellEffectName.WeaponDamageNoSchool:
                    case SpellEffectName.NormalizedWeaponDmg:
                        weaponDamage += (uint)fixed_bonus;
                        break;
                    case SpellEffectName.WeaponPercentDamage:
                        weaponDamage = (uint)(weaponDamage * weaponDamagePercentMod);
                        break;
                    default:
                        continue;                                      // not weapon damage effect, just skip
                }

                if (spellEffectInfo.Mechanic != Mechanics.None && mechanic == Mechanics.None)
                    mechanic = spellEffectInfo.Mechanic;
            }

            weaponDamage += (uint)spell_bonus;
            weaponDamage = (uint)(weaponDamage * totalDamagePercentMod);

            // prevent negative damage
            weaponDamage = Math.Max(weaponDamage, 0);

            // Add melee damage bonuses (also check for negative)
            weaponDamage = (uint)unitCaster.MeleeDamageBonusDone(unitTarget, (int)weaponDamage, m_attackType, DamageEffectType.SpellDirect, m_spellInfo, effectInfo, mechanic, m_spellSchoolMask, this);
            m_damage += unitTarget.MeleeDamageBonusTaken(unitCaster, (int)weaponDamage, m_attackType, DamageEffectType.SpellDirect, m_spellInfo);
        }

        [SpellEffectHandler(SpellEffectName.Threat)]
        void EffectThreat()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster == null || !unitCaster.IsAlive())
                return;

            if (unitTarget == null)
                return;

            if (!unitTarget.CanHaveThreatList())
                return;

            unitTarget.GetThreatManager().AddThreat(unitCaster, damage, m_spellInfo, true);
        }

        [SpellEffectHandler(SpellEffectName.HealMaxHealth)]
        void EffectHealMaxHealth()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster == null)
                return;

            if (unitTarget == null || !unitTarget.IsAlive())
                return;

            int addhealth;

            // damage == 0 - heal for caster max health
            if (damage == 0)
                addhealth = (int)unitCaster.GetMaxHealth();
            else
                addhealth = (int)(unitTarget.GetMaxHealth() - unitTarget.GetHealth());

            m_healing += addhealth;
        }

        [SpellEffectHandler(SpellEffectName.InterruptCast)]
        void EffectInterruptCast()
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
                    if ((spell.GetState() == SpellState.Casting
                        || (spell.GetState() == SpellState.Preparing && spell.GetCastTime() > 0.0f))
                        && curSpellInfo.CanBeInterrupted(m_caster, unitTarget))
                    {
                        int duration = m_spellInfo.GetDuration();
                        duration = unitTarget.ModSpellDuration(m_spellInfo, unitTarget, duration, false, 1u << (int)effectInfo.EffectIndex);
                        unitTarget.GetSpellHistory().LockSpellSchool(curSpellInfo.GetSchoolMask(), TimeSpan.FromMilliseconds(duration));
                        m_UniqueTargetInfo.Find(p => p.TargetGUID == unitTarget.GetGUID()).ProcHitMask |= ProcFlagsHit.Interrupt;
                        SendSpellInterruptLog(unitTarget, curSpellInfo.Id);
                        unitTarget.InterruptSpell(i, false);
                    }
                }
            }
        }

        [SpellEffectHandler(SpellEffectName.SummonObjectWild)]
        void EffectSummonObjectWild()
        {
            if (effectHandleMode != SpellEffectHandleMode.Launch)
                return;

            WorldObject target = focusObject;
            if (target == null)
                target = m_caster;

            float x, y, z, o;
            if (m_targets.HasDst())
                destTarget.GetPosition(out x, out y, out z, out o);
            else
            {
                m_caster.GetClosePoint(out x, out y, out z, SharedConst.DefaultPlayerBoundingRadius);
                o = target.GetOrientation();
            }

            Map map = target.GetMap();

            Position pos = new(x, y, z, o);
            Quaternion rotation = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(o, 0.0f, 0.0f));
            GameObject go = GameObject.CreateGameObject((uint)effectInfo.MiscValue, map, pos, rotation, 255, GameObjectState.Ready);
            if (go == null)
                return;

            PhasingHandler.InheritPhaseShift(go, m_caster);

            int duration = m_spellInfo.CalcDuration(m_caster);

            go.SetRespawnTime(duration > 0 ? duration / Time.InMilliseconds : 0);
            go.SetSpellId(m_spellInfo.Id);

            ExecuteLogEffectSummonObject(effectInfo.Effect, go);

            // Wild object not have owner and check clickable by players
            map.AddToMap(go);

            GameObject linkedTrap = go.GetLinkedTrap();
            if (linkedTrap != null)
            {
                PhasingHandler.InheritPhaseShift(linkedTrap, m_caster);
                linkedTrap.SetRespawnTime(duration > 0 ? duration / Time.InMilliseconds : 0);
                linkedTrap.SetSpellId(m_spellInfo.Id);

                ExecuteLogEffectSummonObject(effectInfo.Effect, linkedTrap);
            }
        }

        [SpellEffectHandler(SpellEffectName.ScriptEffect)]
        void EffectScriptEffect()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            // @todo we must implement hunter pet summon at login there (spell 6962)
            /// @todo: move this to scripts
            switch (m_spellInfo.SpellFamilyName)
            {
                case SpellFamilyNames.Generic:
                {
                    switch (m_spellInfo.Id)
                    {
                        case 45204: // Clone Me!
                            m_caster.CastSpell(unitTarget, (uint)damage, new CastSpellExtraArgs(true));
                            break;
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
                            m_caster.CastSpell(unitTarget, 22682, new CastSpellExtraArgs(this));
                            return;
                        }
                        // Mug Transformation
                        case 41931:
                        {
                            if (!m_caster.IsTypeId(TypeId.Player))
                                return;

                            byte bag = 19;
                            byte slot = 0;
                            Item item;

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
                                m_caster.CastSpell(m_caster, 42518, new CastSpellExtraArgs(this));
                                return;
                            }
                            break;
                        }
                        // Brutallus - Burn
                        case 45141:
                        case 45151:
                        {
                            //Workaround for Range ... should be global for every ScriptEffect
                            float radius = effectInfo.CalcRadius(null, SpellTargetIndex.TargetB);
                            if (unitTarget != null && unitTarget.IsTypeId(TypeId.Player) && unitTarget.GetDistance(m_caster) >= radius && !unitTarget.HasAura(46394) && unitTarget != m_caster)
                                unitTarget.CastSpell(unitTarget, 46394, new CastSpellExtraArgs(this));

                            break;
                        }
                        // Emblazon Runeblade
                        case 51770:
                        {
                            if (m_originalCaster == null)
                                return;

                            m_originalCaster.CastSpell(m_originalCaster, (uint)damage, new CastSpellExtraArgs(false));
                            break;
                        }
                        // Summon Ghouls On Scarlet Crusade
                        case 51904:
                        {
                            if (!m_targets.HasDst())
                                return;

                            float radius = effectInfo.CalcRadius();
                            for (byte i = 0; i < 15; ++i)
                                m_caster.CastSpell(m_caster.GetRandomPoint(destTarget, radius), 54522, new CastSpellExtraArgs(this));
                            break;
                        }
                        case 52173: // Coyote Spirit Despawn
                        case 60243: // Blood Parrot Despawn
                            if (unitTarget.IsTypeId(TypeId.Unit) && unitTarget.ToCreature().IsSummon())
                                unitTarget.ToTempSummon().UnSummon();
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
                        case 58941:                                 // Rock Shards
                            if (unitTarget != null && m_originalCaster != null)
                            {
                                for (uint i = 0; i < 3; ++i)
                                {
                                    m_originalCaster.CastSpell(unitTarget, 58689, new CastSpellExtraArgs(true));
                                    m_originalCaster.CastSpell(unitTarget, 58692, new CastSpellExtraArgs(true));
                                }
                                if (m_originalCaster.GetMap().GetDifficultyID() == Difficulty.None)
                                {
                                    m_originalCaster.CastSpell(unitTarget, 58695, new CastSpellExtraArgs(true));
                                    m_originalCaster.CastSpell(unitTarget, 58696, new CastSpellExtraArgs(true));
                                }
                                else
                                {
                                    m_originalCaster.CastSpell(unitTarget, 60883, new CastSpellExtraArgs(true));
                                    m_originalCaster.CastSpell(unitTarget, 60884, new CastSpellExtraArgs(true));
                                }
                            }
                            return;
                        case 62482: // Grab Crate
                        {
                            if (unitCaster == null)
                                return;

                            if (unitTarget != null)
                            {
                                Unit seat = unitCaster.GetVehicleBase();
                                if (seat != null)
                                {
                                    Unit parent = seat.GetVehicleBase();
                                    if (parent != null)
                                    {
                                        // @todo a hack, range = 11, should after some time cast, otherwise too far
                                        unitCaster.CastSpell(parent, 62496, new CastSpellExtraArgs(this));
                                        unitTarget.CastSpell(parent, (uint)damage, new CastSpellExtraArgs().SetTriggeringSpell(this)); // DIFFICULTY_NONE, so effect always valid
                                    }
                                }
                            }
                            return;
                        }
                    }
                    break;
                }
            }

            // normal DB scripted effect
            Log.outDebug(LogFilter.Spells, "Spell ScriptStart spellid {0} in EffectScriptEffect({1})", m_spellInfo.Id, effectInfo.EffectIndex);
            m_caster.GetMap().ScriptsStart(ScriptsType.Spell, (uint)((int)m_spellInfo.Id | (int)(effectInfo.EffectIndex << 24)), m_caster, unitTarget);
        }

        [SpellEffectHandler(SpellEffectName.Sanctuary)]
        [SpellEffectHandler(SpellEffectName.Sanctuary2)]
        void EffectSanctuary()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            var isAffectedBySanctuary = bool (Unit attacker) =>
            {
                Creature attackerCreature = attacker.ToCreature();
                return attackerCreature == null || !attackerCreature.IsIgnoringSanctuarySpellEffect();
            };

            if (unitTarget.IsPlayer() && !unitTarget.GetMap().IsDungeon())
            {
                // stop all pve combat for players outside dungeons, suppress pvp combat
                unitTarget.CombatStop(false, false, isAffectedBySanctuary);
            }
            else
            {
                // in dungeons (or for nonplayers), reset this unit on all enemies' threat lists
                foreach (var (_, refe) in unitTarget.GetThreatManager().GetThreatenedByMeList())
                    if (isAffectedBySanctuary(refe.GetOwner()))
                        refe.ScaleThreat(0.0f);
            }

            // makes spells cast before this time fizzle
            unitTarget.LastSanctuaryTime = GameTime.GetGameTimeMS();
        }

        [SpellEffectHandler(SpellEffectName.Duel)]
        void EffectDuel()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !m_caster.IsTypeId(TypeId.Player) || !unitTarget.IsTypeId(TypeId.Player))
                return;
            Player caster = m_caster.ToPlayer();
            Player target = unitTarget.ToPlayer();

            // caster or target already have requested duel
            if (caster.duel != null || target.duel != null || target.GetSocial() == null || target.GetSocial().HasIgnore(caster.GetGUID(), caster.GetSession().GetAccountGUID()))
                return;

            // Players can only fight a duel in zones with this flag
            AreaTableRecord casterAreaEntry = CliDB.AreaTableStorage.LookupByKey(caster.GetAreaId());
            if (casterAreaEntry != null && !casterAreaEntry.HasFlag(AreaFlags.AllowDueling))
            {
                SendCastResult(SpellCastResult.NoDueling);            // Dueling isn't allowed here
                return;
            }

            AreaTableRecord targetAreaEntry = CliDB.AreaTableStorage.LookupByKey(target.GetAreaId());
            if (targetAreaEntry != null && !targetAreaEntry.HasFlag(AreaFlags.AllowDueling))
            {
                SendCastResult(SpellCastResult.NoDueling);            // Dueling isn't allowed here
                return;
            }

            //CREATE DUEL FLAG OBJECT
            Map map = caster.GetMap();
            Position pos = new()
            {
                posX = caster.GetPositionX() + (unitTarget.GetPositionX() - caster.GetPositionX()) / 2,
                posY = caster.GetPositionY() + (unitTarget.GetPositionY() - caster.GetPositionY()) / 2,
                posZ = caster.GetPositionZ(),
                Orientation = caster.GetOrientation()
            };
            Quaternion rotation = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(pos.GetOrientation(), 0.0f, 0.0f));

            GameObject go = GameObject.CreateGameObject((uint)effectInfo.MiscValue, map, pos, rotation, 0, GameObjectState.Ready);
            if (go == null)
                return;

            PhasingHandler.InheritPhaseShift(go, caster);

            go.SetFaction(caster.GetFaction());
            go.SetLevel(caster.GetLevel() + 1);
            int duration = m_spellInfo.CalcDuration(caster);
            go.SetRespawnTime(duration > 0 ? duration / Time.InMilliseconds : 0);
            go.SetSpellId(m_spellInfo.Id);

            ExecuteLogEffectSummonObject(effectInfo.Effect, go);

            caster.AddGameObject(go);
            map.AddToMap(go);
            //END

            // Send request
            DuelRequested packet = new();
            packet.ArbiterGUID = go.GetGUID();
            packet.RequestedByGUID = caster.GetGUID();
            packet.RequestedByWowAccount = caster.GetSession().GetAccountGUID();

            caster.SendPacket(packet);
            target.SendPacket(packet);

            // create duel-info
            bool isMounted = (GetSpellInfo().Id == 62875);
            caster.duel = new(target, caster, isMounted);
            target.duel = new(caster, caster, isMounted);

            caster.SetDuelArbiter(go.GetGUID());
            target.SetDuelArbiter(go.GetGUID());

            Global.ScriptMgr.OnPlayerDuelRequest(target, caster);
        }

        [SpellEffectHandler(SpellEffectName.Stuck)]
        void EffectStuck()
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
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(8690, GetCastDifficulty());
            if (spellInfo == null)
                return;

            Spell spell = new(player, spellInfo, TriggerCastFlags.FullMask);
            spell.SendSpellCooldown();
        }

        [SpellEffectHandler(SpellEffectName.SummonPlayer)]
        void EffectSummonPlayer()
        {
            // workaround - this effect should not use target map
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster == null)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            unitTarget.ToPlayer().SendSummonRequestFrom(unitCaster);
        }

        [SpellEffectHandler(SpellEffectName.ActivateObject)]
        void EffectActivateObject()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (gameObjTarget == null)
                return;

            gameObjTarget.ActivateObject((GameObjectActions)effectInfo.MiscValue, effectInfo.MiscValueB, m_caster, m_spellInfo.Id, (int)effectInfo.EffectIndex);
        }

        [SpellEffectHandler(SpellEffectName.ApplyGlyph)]
        void EffectApplyGlyph()
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

            player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags2.ChangeGlyph);

            GlyphPropertiesRecord glyphProperties = CliDB.GlyphPropertiesStorage.LookupByKey(glyphId);
            if (glyphProperties != null)
                player.CastSpell(player, glyphProperties.SpellID, new CastSpellExtraArgs(this));

            ActiveGlyphs activeGlyphs = new();
            activeGlyphs.Glyphs.Add(new GlyphBinding(m_misc.SpellId, (ushort)glyphId));
            activeGlyphs.IsFullUpdate = false;
            player.SendPacket(activeGlyphs);
        }

        [SpellEffectHandler(SpellEffectName.EnchantHeldItem)]
        void EffectEnchantHeldItem()
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
        void EffectDisEnchant()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player caster = m_caster.ToPlayer();
            if (caster != null)
            {
                if (m_CastItem == null)
                    caster.UpdateCraftSkill(m_spellInfo);
                itemTarget.loot = new Loot(caster.GetMap(), itemTarget.GetGUID(), LootType.Disenchanting, null);
                itemTarget.loot.FillLoot(itemTarget.GetDisenchantLootId().GetValueOrDefault(), LootStorage.Disenchant, caster, true);
                caster.SendLoot(itemTarget.loot);
            }

            // item will be removed at disenchanting end
        }

        [SpellEffectHandler(SpellEffectName.Inebriate)]
        void EffectInebriate()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsPlayer())
                return;

            Player player = unitTarget.ToPlayer();

            byte currentDrunkValue = player.GetDrunkValue();
            byte drunkValue = (byte)Math.Clamp(damage + currentDrunkValue, 0, 100);
            if (currentDrunkValue == 100 && currentDrunkValue == drunkValue)
                if (RandomHelper.randChance(25.0f))
                    player.CastSpell(player, 67468, new CastSpellExtraArgs().SetTriggeringSpell(this));    // Drunken Vomit

            player.SetDrunkValue(drunkValue, m_CastItem != null ? m_CastItem.GetEntry() : 0);
        }

        [SpellEffectHandler(SpellEffectName.FeedPet)]
        void EffectFeedPet()
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

            ExecuteLogEffectDestroyItem(effectInfo.Effect, foodItem.GetEntry());

            int pct;
            int levelDiff = (int)pet.GetLevel() - (int)foodItem.GetTemplate().GetBaseItemLevel();
            if (levelDiff >= 30)
                return;
            else if (levelDiff >= 20)
                pct = (int)12.5; // we can't pass double so keeping the cast here for future references
            else if (levelDiff >= 10)
                pct = 25;
            else
                pct = 50;

            uint count = 1;
            player.DestroyItemCount(foodItem, ref count, true);
            // @todo fix crash when a spell has two effects, both pointed at the same item target

            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.SetTriggeringSpell(this);
            args.AddSpellMod(SpellValueMod.BasePoint0, pct);
            m_caster.CastSpell(pet, effectInfo.TriggerSpell, args);
        }

        [SpellEffectHandler(SpellEffectName.DismissPet)]
        void EffectDismissPet()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsPet())
                return;

            Pet pet = unitTarget.ToPet();

            ExecuteLogEffectUnsummonObject(effectInfo.Effect, pet);
            pet.Remove(PetSaveMode.NotInSlot);
        }

        [SpellEffectHandler(SpellEffectName.SummonObjectSlot1)]
        void EffectSummonObject()
        {
            if (effectHandleMode != SpellEffectHandleMode.Launch)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster == null)
                return;

            byte slot = (byte)(effectInfo.Effect - SpellEffectName.SummonObjectSlot1);
            ObjectGuid guid = unitCaster.m_ObjectSlot[slot];
            if (!guid.IsEmpty())
            {
                GameObject obj = unitCaster.GetMap().GetGameObject(guid);
                if (obj != null)
                {
                    // Recast case - null spell id to make auras not be removed on object remove from world
                    if (m_spellInfo.Id == obj.GetSpellId())
                        obj.SetSpellId(0);
                    unitCaster.RemoveGameObject(obj, true);
                }
                unitCaster.m_ObjectSlot[slot].Clear();
            }

            float x, y, z, o;
            // If dest location if present
            if (m_targets.HasDst())
                destTarget.GetPosition(out x, out y, out z, out o);
            // Summon in random point all other units if location present
            else
            {
                unitCaster.GetClosePoint(out x, out y, out z, SharedConst.DefaultPlayerBoundingRadius);
                o = unitCaster.GetOrientation();
            }

            Map map = m_caster.GetMap();
            Position pos = new(x, y, z, o);
            Quaternion rotation = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(o, 0.0f, 0.0f));
            GameObject go = GameObject.CreateGameObject((uint)effectInfo.MiscValue, map, pos, rotation, 255, GameObjectState.Ready);
            if (go == null)
                return;

            PhasingHandler.InheritPhaseShift(go, m_caster);

            go.SetFaction(unitCaster.GetFaction());
            go.SetLevel(unitCaster.GetLevel());
            int duration = m_spellInfo.CalcDuration(m_caster);
            go.SetRespawnTime(duration > 0 ? duration / Time.InMilliseconds : 0);
            go.SetSpellId(m_spellInfo.Id);
            unitCaster.AddGameObject(go);

            ExecuteLogEffectSummonObject(effectInfo.Effect, go);

            map.AddToMap(go);

            unitCaster.m_ObjectSlot[slot] = go.GetGUID();
        }

        [SpellEffectHandler(SpellEffectName.Resurrect)]
        void EffectResurrect()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (corpseTarget == null && unitTarget == null)
                return;

            Player player = null;

            if (corpseTarget != null)
                player = Global.ObjAccessor.FindPlayer(corpseTarget.GetOwnerGUID());
            else if (unitTarget != null)
                player = unitTarget.ToPlayer();

            if (player == null || player.IsAlive() || !player.IsInWorld)
                return;

            if (player.IsResurrectRequested())       // already have one active request
                return;

            uint health = (uint)player.CountPctFromMaxHealth(damage);
            uint mana = (uint)MathFunctions.CalculatePct(player.GetMaxPower(PowerType.Mana), damage);

            ExecuteLogEffectResurrect(effectInfo.Effect, player);

            player.SetResurrectRequestData(m_caster, health, mana, 0);
            SendResurrectRequest(player);
        }

        [SpellEffectHandler(SpellEffectName.AddExtraAttacks)]
        void EffectAddExtraAttacks()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsAlive())
                return;

            unitTarget.AddExtraAttacks((uint)damage);

            ExecuteLogEffectExtraAttacks(effectInfo.Effect, unitTarget, (uint)damage);
        }

        [SpellEffectHandler(SpellEffectName.Parry)]
        void EffectParry()
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (m_caster.IsTypeId(TypeId.Player))
                m_caster.ToPlayer().SetCanParry(true);
        }

        [SpellEffectHandler(SpellEffectName.Block)]
        void EffectBlock()
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (m_caster.IsTypeId(TypeId.Player))
                m_caster.ToPlayer().SetCanBlock(true);
        }

        [SpellEffectHandler(SpellEffectName.Leap)]
        void EffectLeap()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || unitTarget.IsInFlight())
                return;

            if (!m_targets.HasDst())
                return;

            Position pos = destTarget.GetPosition();
            unitTarget.NearTeleportTo(pos.posX, pos.posY, pos.posZ, pos.Orientation, unitTarget == m_caster);
        }

        [SpellEffectHandler(SpellEffectName.Reputation)]
        [SpellEffectHandler(SpellEffectName.Reputation2)]
        void EffectReputation()
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
        void EffectQuestComplete()
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

                QuestStatus questStatus = player.GetQuestStatus(questId);
                if (questStatus == QuestStatus.Rewarded)
                    return;

                if (quest.HasFlag(QuestFlags.CompletionEvent) || quest.HasFlag(QuestFlags.CompletionAreaTrigger))
                {
                    if (questStatus == QuestStatus.Incomplete)
                        player.AreaExploredOrEventHappens(questId);
                }
                else if (quest.HasFlag(QuestFlags.TrackingEvent)) // Check if the quest is used as a serverside flag
                    player.CompleteQuest(questId);
            }
        }

        [SpellEffectHandler(SpellEffectName.ForceDeselect)]
        void EffectForceDeselect()
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster == null)
                return;

            float dist = m_caster.GetVisibilityRange();

            // clear focus
            PacketSenderOwning<BreakTarget> breakTarget = new();
            breakTarget.Data.UnitGUID = m_caster.GetGUID();
            breakTarget.Data.Write();

            var notifierBreak = new MessageDistDelivererToHostile<PacketSenderOwning<BreakTarget>>(unitCaster, breakTarget, dist);
            Cell.VisitWorldObjects(m_caster, notifierBreak, dist);

            // and selection
            PacketSenderOwning<ClearTarget> clearTarget = new();
            clearTarget.Data.Guid = m_caster.GetGUID();
            clearTarget.Data.Write();
            var notifierClear = new MessageDistDelivererToHostile<PacketSenderOwning<ClearTarget>>(unitCaster, clearTarget, dist);
            Cell.VisitWorldObjects(m_caster, notifierClear, dist);

            // we should also force pets to remove us from current target
            List<Unit> attackerSet = new();
            foreach (var unit in unitCaster.GetAttackers())
                if (unit.GetTypeId() == TypeId.Unit && !unit.CanHaveThreatList())
                    attackerSet.Add(unit);

            foreach (var unit in attackerSet)
                unit.AttackStop();
        }

        [SpellEffectHandler(SpellEffectName.SelfResurrect)]
        void EffectSelfResurrect()
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Player player = m_caster.ToPlayer();
            if (player == null || !player.IsInWorld || player.IsAlive())
                return;

            uint health;
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
                health = (uint)player.CountPctFromMaxHealth(damage);
                if (player.GetMaxPower(PowerType.Mana) > 0)
                    mana = MathFunctions.CalculatePct(player.GetMaxPower(PowerType.Mana), damage);
            }

            player.ResurrectPlayer(0.0f);

            player.SetHealth(health);
            player.SetPower(PowerType.Mana, mana);
            player.SetPower(PowerType.Rage, 0);
            player.SetFullPower(PowerType.Energy);
            player.SetPower(PowerType.Focus, 0);

            player.SpawnCorpseBones();
        }

        [SpellEffectHandler(SpellEffectName.Skinning)]
        void EffectSkinning()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!unitTarget.IsTypeId(TypeId.Unit))
                return;

            Player player = m_caster.ToPlayer();
            if (player == null)
                return;

            Creature creature = unitTarget.ToCreature();
            int targetLevel = (int)creature.GetLevelForTarget(m_caster);

            SkillType skill = creature.GetCreatureDifficulty().GetRequiredLootSkill();

            creature.SetUnitFlag3(UnitFlags3.AlreadySkinned);
            creature.SetDynamicFlag(UnitDynFlags.Lootable);
            Loot loot = new(creature.GetMap(), creature.GetGUID(), LootType.Skinning, null);
            creature.m_personalLoot[player.GetGUID()] = loot;
            loot.FillLoot(creature.GetCreatureDifficulty().SkinLootID, LootStorage.Skinning, player, true);
            player.SendLoot(loot);

            if (!Global.SpellMgr.IsPartOfSkillLine(skill, m_spellInfo.Id))
                return;

            // Skill gain for skinning
            // This formula is still used (10.0.5.48526)
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

                ContentTuningRecord contentTuning = CliDB.ContentTuningStorage.LookupByKey(creature.GetContentTuning());
                if (contentTuning == null)
                    return;

                uint skinningSkill = player.GetProfessionSkillForExp(skill, contentTuning.ExpansionID);
                if (skinningSkill == 0)
                    return;

                uint pureSkillValue = player.GetPureSkillValue(skinningSkill);
                if (pureSkillValue != 0)
                {
                    // Double chances for elites
                    player.UpdateGatherSkill(skinningSkill, pureSkillValue, (uint)reqValue, creature.IsElite() ? 2 : 1u);
                }
            }
        }

        [SpellEffectHandler(SpellEffectName.Charge)]
        void EffectCharge()
        {
            if (unitTarget == null)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster == null)
                return;

            if (effectHandleMode == SpellEffectHandleMode.LaunchTarget)
            {
                // charge changes fall time
                if (unitCaster.IsPlayer())
                    unitCaster.ToPlayer().SetFallInformation(0, m_caster.GetPositionZ());

                float speed = MathFunctions.fuzzyGt(m_spellInfo.Speed, 0.0f) ? m_spellInfo.Speed : MotionMaster.SPEED_CHARGE;
                SpellEffectExtraData spellEffectExtraData = null;
                if (effectInfo.MiscValueB != 0)
                {
                    spellEffectExtraData = new SpellEffectExtraData();
                    spellEffectExtraData.Target = unitTarget.GetGUID();
                    spellEffectExtraData.SpellVisualId = (uint)effectInfo.MiscValueB;
                }
                // Spell is not using explicit target - no generated path
                if (m_preGeneratedPath == null)
                {
                    Position pos = unitTarget.GetFirstCollisionPosition(unitTarget.GetCombatReach(), unitTarget.GetRelativeAngle(m_caster.GetPosition()));
                    m_preGeneratedPath = new PathGenerator(unitCaster);
                    m_preGeneratedPath.CalculatePath(pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), false);
                }

                if (MathFunctions.fuzzyGt(m_spellInfo.Speed, 0.0f) && m_spellInfo.HasAttribute(SpellAttr9.MissileSpeedIsDelayInSec))
                    speed = m_preGeneratedPath.GetPathLength() / speed;

                unitCaster.GetMotionMaster().MoveCharge(m_preGeneratedPath, speed, unitTarget, spellEffectExtraData);

                // abuse implementation detail of MoveCharge accepting PathGenerator argument (instantly started spline)
                UpdateDelayMomentForUnitTarget(unitTarget, (uint)unitCaster.MoveSpline.Duration());
            }

            if (effectHandleMode == SpellEffectHandleMode.HitTarget)
            {
                // not all charge effects used in negative spells
                if (m_spellInfo.HasAttribute(SpellAttr7.AttackOnChargeToUnit))
                    unitCaster.Attack(unitTarget, true);

                if (effectInfo.TriggerSpell != 0)
                    m_caster.CastSpell(unitTarget, effectInfo.TriggerSpell, new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                        .SetOriginalCaster(m_originalCasterGUID)
                        .SetTriggeringSpell(this));
            }
        }

        [SpellEffectHandler(SpellEffectName.ChargeDest)]
        void EffectChargeDest()
        {
            if (destTarget == null)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster == null)
                return;

            if (effectHandleMode == SpellEffectHandleMode.Launch)
            {
                Position pos = destTarget.GetPosition();
                if (!unitCaster.IsWithinLOS(pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ()))
                {
                    float angle = unitCaster.GetRelativeAngle(pos.posX, pos.posY);
                    float dist = unitCaster.GetDistance(pos);
                    pos = unitCaster.GetFirstCollisionPosition(dist, angle);
                }

                PathGenerator path = new(unitCaster);
                path.CalculatePath(pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), false);

                float speed = MathFunctions.fuzzyGt(m_spellInfo.Speed, 0.0f) ? m_spellInfo.Speed : MotionMaster.SPEED_CHARGE;

                if (MathFunctions.fuzzyGt(m_spellInfo.Speed, 0.0f) && m_spellInfo.HasAttribute(SpellAttr9.MissileSpeedIsDelayInSec))
                    speed = path.GetPathLength() / speed;

                unitCaster.GetMotionMaster().MoveCharge(path, speed);

                // abuse implementation detail of MoveCharge accepting PathGenerator argument (instantly started spline)
                UpdateDelayMomentForDst((uint)unitCaster.MoveSpline.Duration());
            }
            else if (effectHandleMode == SpellEffectHandleMode.Hit)
            {
                if (effectInfo.TriggerSpell != 0)
                    m_caster.CastSpell(destTarget, effectInfo.TriggerSpell, new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                        .SetOriginalCaster(m_originalCasterGUID)
                        .SetTriggeringSpell(this));
            }
        }

        [SpellEffectHandler(SpellEffectName.KnockBack)]
        [SpellEffectHandler(SpellEffectName.KnockBackDest)]
        void EffectKnockBack()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            if (m_caster.GetAffectingPlayer() != null)
            {
                Creature creatureTarget = unitTarget.ToCreature();
                if (creatureTarget != null)
                    if (creatureTarget.IsWorldBoss() || creatureTarget.IsDungeonBoss())
                        return;
            }

            // Spells with SPELL_EFFECT_KNOCK_BACK (like Thunderstorm) can't knockback target if target has ROOT/STUN
            if (unitTarget.HasUnitState(UnitState.Root | UnitState.Stunned))
                return;

            float ratio = 0.1f;
            float speedxy = effectInfo.MiscValue * ratio;
            float speedz = damage * ratio;
            if (speedxy < 0.01f && speedz < 0.01f)
                return;

            Position origin;
            if (effectInfo.Effect == SpellEffectName.KnockBackDest)
            {
                if (m_targets.HasDst())
                    origin = new(destTarget.GetPosition());
                else
                    return;
            }
            else //if (effectInfo.Effect == SPELL_EFFECT_KNOCK_BACK)
                origin = new(m_caster.GetPosition());

            unitTarget.KnockbackFrom(origin, speedxy, speedz);

            Unit.ProcSkillsAndAuras(GetUnitCasterForEffectHandlers(), unitTarget, new ProcFlagsInit(ProcFlags.None), new ProcFlagsInit(ProcFlags.None, ProcFlags2.Knockback), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.Hit, ProcFlagsHit.None, null, null, null);
        }

        [SpellEffectHandler(SpellEffectName.LeapBack)]
        void EffectLeapBack()
        {
            if (effectHandleMode != SpellEffectHandleMode.LaunchTarget)
                return;

            if (unitTarget == null)
                return;

            float speedxy = effectInfo.MiscValue / 10.0f;
            float speedz = damage / 10.0f;
            // Disengage
            unitTarget.JumpTo(speedxy, speedz, effectInfo.PositionFacing);

            // changes fall time
            if (m_caster.GetTypeId() == TypeId.Player)
                m_caster.ToPlayer().SetFallInformation(0, m_caster.GetPositionZ());
        }

        [SpellEffectHandler(SpellEffectName.ClearQuest)]
        void EffectQuestClear()
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
            player.DespawnPersonalSummonsForQuest(quest_id);

            Global.ScriptMgr.OnQuestStatusChange(player, quest_id);
            Global.ScriptMgr.OnQuestStatusChange(player, quest, oldStatus, QuestStatus.None);
        }

        [SpellEffectHandler(SpellEffectName.SendTaxi)]
        void EffectSendTaxi()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            unitTarget.ToPlayer().ActivateTaxiPathTo((uint)effectInfo.MiscValue, m_spellInfo.Id);
        }

        [SpellEffectHandler(SpellEffectName.PullTowards)]
        void EffectPullTowards()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            Position pos = m_caster.GetFirstCollisionPosition(m_caster.GetCombatReach(), m_caster.GetRelativeAngle(unitTarget));

            // This is a blizzlike mistake: this should be 2D distance according to projectile motion formulas, but Blizzard erroneously used 3D distance.
            float distXY = unitTarget.GetExactDist(pos);

            // Avoid division by 0
            if (distXY < 0.001)
                return;

            float distZ = pos.GetPositionZ() - unitTarget.GetPositionZ();
            float speedXY = effectInfo.MiscValue != 0 ? effectInfo.MiscValue / 10.0f : 30.0f;
            float speedZ = (float)((2 * speedXY * speedXY * distZ + MotionMaster.gravity * distXY * distXY) / (2 * speedXY * distXY));

            if (!float.IsFinite(speedZ))
            {
                Log.outError(LogFilter.Spells, $"Spell {m_spellInfo.Id} with SPELL_EFFECT_PULL_TOWARDS called with invalid speedZ. {GetDebugInfo()}");
                return;
            }

            unitTarget.JumpTo(speedXY, speedZ, 0.0f, pos);
        }

        [SpellEffectHandler(SpellEffectName.PullTowardsDest)]
        void EffectPullTowardsDest()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            if (!m_targets.HasDst())
            {
                Log.outError(LogFilter.Spells, $"Spell {m_spellInfo.Id} with SPELL_EFFECT_PULL_TOWARDS_DEST has no dest target");
                return;
            }

            Position pos = m_targets.GetDstPos();
            // This is a blizzlike mistake: this should be 2D distance according to projectile motion formulas, but Blizzard erroneously used 3D distance
            float distXY = unitTarget.GetExactDist(pos);

            // Avoid division by 0
            if (distXY < 0.001)
                return;

            float distZ = pos.GetPositionZ() - unitTarget.GetPositionZ();

            float speedXY = effectInfo.MiscValue != 0 ? effectInfo.MiscValue / 10.0f : 30.0f;
            float speedZ = (float)((2 * speedXY * speedXY * distZ + MotionMaster.gravity * distXY * distXY) / (2 * speedXY * distXY));

            if (!float.IsFinite(speedZ))
            {
                Log.outError(LogFilter.Spells, $"Spell {m_spellInfo.Id} with SPELL_EFFECT_PULL_TOWARDS_DEST called with invalid speedZ. {GetDebugInfo()}");
                return;
            }

            unitTarget.JumpTo(speedXY, speedZ, 0.0f, pos);
        }

        [SpellEffectHandler(SpellEffectName.ChangeRaidMarker)]
        void EffectChangeRaidMarker()
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Player player = m_caster.ToPlayer();
            if (player == null || !m_targets.HasDst())
                return;

            Group group = player.GetGroup();
            if (group == null || (group.IsRaidGroup() && !group.IsLeader(player.GetGUID()) && !group.IsAssistant(player.GetGUID())))
                return;

            float x, y, z;
            destTarget.GetPosition(out x, out y, out z);

            group.AddRaidMarker((byte)damage, player.GetMapId(), x, y, z);
        }

        [SpellEffectHandler(SpellEffectName.DispelMechanic)]
        void EffectDispelMechanic()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            int mechanic = effectInfo.MiscValue;

            List<KeyValuePair<uint, ObjectGuid>> dispel_list = new();

            var auras = unitTarget.GetOwnedAuras();
            foreach (var pair in auras)
            {
                Aura aura = pair.Value;
                if (aura.GetApplicationOfTarget(unitTarget.GetGUID()) == null)
                    continue;

                if (RandomHelper.randChance(aura.CalcDispelChance(unitTarget, !unitTarget.IsFriendlyTo(m_caster))))
                    if ((aura.GetSpellInfo().GetAllEffectsMechanicMask() & (1ul << mechanic)) != 0)
                        dispel_list.Add(new KeyValuePair<uint, ObjectGuid>(aura.GetId(), aura.GetCasterGUID()));
            }

            if (dispel_list.Empty())
                return;

            while (!dispel_list.Empty())
            {
                unitTarget.RemoveAura(dispel_list[0].Key, dispel_list[0].Value, 0, AuraRemoveMode.EnemySpell);
                dispel_list.RemoveAt(0);
            }

            m_UniqueTargetInfo.Find(p => p.TargetGUID == unitTarget.GetGUID()).ProcHitMask |= ProcFlagsHit.Dispel;
        }

        [SpellEffectHandler(SpellEffectName.ResurrectPet)]
        void EffectResurrectPet()
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (damage < 0)
                return;

            Player player = m_caster.ToPlayer();
            if (player == null)
                return;

            // Maybe player dismissed dead pet or pet despawned?
            bool hadPet = true;

            if (player.GetPet() == null)
            {
                PetStable petStable = player.GetPetStable();
                var deadPetIndex = Array.FindIndex(petStable.ActivePets, petInfo => petInfo?.Health == 0);

                PetSaveMode slot = (PetSaveMode)deadPetIndex;

                player.SummonPet(0, slot, 0f, 0f, 0f, 0f, 0);
                hadPet = false;
            }

            // TODO: Better to fail Hunter's "Revive Pet" at cast instead of here when casting ends
            Pet pet = player.GetPet(); // Attempt to get current pet
            if (pet == null || pet.IsAlive())
                return;

            // If player did have a pet before reviving, teleport it
            if (hadPet)
            {
                // Reposition the pet's corpse before reviving so as not to grab aggro
                // We can use a different, more accurate version of GetClosePoint() since we have a pet
                // Will be used later to reposition the pet if we have one
                player.GetClosePoint(out float x, out float y, out float z, pet.GetCombatReach(), SharedConst.PetFollowDist, pet.GetFollowAngle());
                pet.NearTeleportTo(x, y, z, player.GetOrientation());
                pet.Relocate(x, y, z, player.GetOrientation()); // This is needed so SaveStayPosition() will get the proper coords.
            }

            pet.ReplaceAllDynamicFlags(UnitDynFlags.None);
            pet.RemoveUnitFlag(UnitFlags.Skinnable);
            pet.SetDeathState(DeathState.Alive);
            pet.ClearUnitState(UnitState.AllErasable);
            pet.SetHealth(pet.CountPctFromMaxHealth(damage));

            // Reset things for when the AI to takes over
            CharmInfo ci = pet.GetCharmInfo();
            if (ci != null)
            {
                // In case the pet was at stay, we don't want it running back
                ci.SaveStayPosition();
                ci.SetIsAtStay(ci.HasCommandState(CommandStates.Stay));

                ci.SetIsFollowing(false);
                ci.SetIsCommandAttack(false);
                ci.SetIsCommandFollow(false);
                ci.SetIsReturning(false);
            }

            pet.SavePetToDB(PetSaveMode.AsCurrent);
        }

        [SpellEffectHandler(SpellEffectName.DestroyAllTotems)]
        void EffectDestroyAllTotems()
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster == null)
                return;

            int mana = 0;
            for (byte slot = (int)SummonSlot.Totem; slot < SharedConst.MaxTotemSlot; ++slot)
            {
                if (unitCaster.m_SummonSlot[slot].IsEmpty())
                    continue;

                Creature totem = unitCaster.GetMap().GetCreature(unitCaster.m_SummonSlot[slot]);
                if (totem != null && totem.IsTotem())
                {
                    uint spell_id = totem.m_unitData.CreatedBySpell;
                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spell_id, GetCastDifficulty());
                    if (spellInfo != null)
                    {
                        var costs = spellInfo.CalcPowerCost(unitCaster, spellInfo.GetSchoolMask());
                        var m = costs.Find(cost => cost.Power == PowerType.Mana);
                        if (m != null)
                            mana += m.Amount;
                    }
                    totem.ToTotem().UnSummon();
                }
            }
            MathFunctions.ApplyPct(ref mana, damage);
            if (mana != 0)
            {
                CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                args.SetTriggeringSpell(this);
                args.AddSpellMod(SpellValueMod.BasePoint0, mana);
                unitCaster.CastSpell(m_caster, 39104, args);
            }
        }

        [SpellEffectHandler(SpellEffectName.DurabilityDamage)]
        void EffectDurabilityDamage()
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
                ExecuteLogEffectDurabilityDamage(effectInfo.Effect, unitTarget, -1, -1);
                return;
            }

            // invalid slot value
            if (slot >= InventorySlots.BagEnd)
                return;

            Item item = unitTarget.ToPlayer().GetItemByPos(InventorySlots.Bag0, (byte)slot);
            if (item != null)
            {
                unitTarget.ToPlayer().DurabilityPointsLoss(item, damage);
                ExecuteLogEffectDurabilityDamage(effectInfo.Effect, unitTarget, (int)item.GetEntry(), slot);
            }
        }

        [SpellEffectHandler(SpellEffectName.DurabilityDamagePct)]
        void EffectDurabilityDamagePCT()
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
        void EffectModifyThreatPercent()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster == null || unitTarget == null)
                return;

            unitTarget.GetThreatManager().ModifyThreatByPercent(unitCaster, damage);
        }

        [SpellEffectHandler(SpellEffectName.TransDoor)]
        void EffectTransmitted()
        {
            if (effectHandleMode != SpellEffectHandleMode.Launch)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster == null)
                return;

            uint name_id = (uint)effectInfo.MiscValue;

            var overrideSummonedGameObjects = unitCaster.GetAuraEffectsByType(AuraType.OverrideSummonedObject);
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

            float fx, fy, fz, fo;

            if (m_targets.HasDst())
                destTarget.GetPosition(out fx, out fy, out fz, out fo);
            //FIXME: this can be better check for most objects but still hack
            else if (effectInfo.HasRadius(SpellTargetIndex.TargetA) && m_spellInfo.Speed == 0)
            {
                float dis = effectInfo.CalcRadius(unitCaster);
                unitCaster.GetClosePoint(out fx, out fy, out fz, SharedConst.DefaultPlayerBoundingRadius, dis);
                fo = unitCaster.GetOrientation();
            }
            else
            {
                //GO is always friendly to it's creator, get range for friends
                float min_dis = m_spellInfo.GetMinRange(true);
                float max_dis = m_spellInfo.GetMaxRange(true);
                float dis = RandomHelper.NextSingle() * (max_dis - min_dis) + min_dis;

                unitCaster.GetClosePoint(out fx, out fy, out fz, SharedConst.DefaultPlayerBoundingRadius, dis);
                fo = unitCaster.GetOrientation();
            }

            Map cMap = unitCaster.GetMap();
            // if gameobject is summoning object, it should be spawned right on caster's position
            if (goinfo.type == GameObjectTypes.Ritual)
                unitCaster.GetPosition(out fx, out fy, out fz, out fo);

            Position pos = new(fx, fy, fz, fo);
            Quaternion rotation = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(fo, 0.0f, 0.0f));

            GameObject go = GameObject.CreateGameObject(name_id, cMap, pos, rotation, 255, GameObjectState.Ready);
            if (go == null)
                return;

            PhasingHandler.InheritPhaseShift(go, m_caster);

            int duration = m_spellInfo.CalcDuration(m_caster);

            switch (goinfo.type)
            {
                case GameObjectTypes.FishingNode:
                {
                    go.SetFaction(unitCaster.GetFaction());
                    ObjectGuid bobberGuid = go.GetGUID();
                    // client requires fishing bobber guid in channel object slot 0 to be usable
                    unitCaster.SetChannelObject(0, bobberGuid);
                    unitCaster.AddGameObject(go);              // will removed at spell cancel

                    // end time of range when possible catch fish (FISHING_BOBBER_READY_TIME..GetDuration(m_spellInfo))
                    // start time == fish-FISHING_BOBBER_READY_TIME (0..GetDuration(m_spellInfo)-FISHING_BOBBER_READY_TIME)
                    int lastSec = 0;
                    switch (RandomHelper.IRand(0, 2))
                    {
                        case 0: lastSec = 3; break;
                        case 1: lastSec = 7; break;
                        case 2: lastSec = 13; break;
                    }

                    // Duration of the fishing bobber can't be higher than the Fishing channeling duration
                    duration = Math.Min(duration, duration - lastSec * Time.InMilliseconds + 5 * Time.InMilliseconds);
                    break;
                }
                case GameObjectTypes.Ritual:
                {
                    if (unitCaster.IsPlayer())
                    {
                        go.AddUniqueUse(unitCaster.ToPlayer());
                        unitCaster.AddGameObject(go);      // will be removed at spell cancel
                    }
                    break;
                }
                case GameObjectTypes.DuelArbiter: // 52991
                    unitCaster.AddGameObject(go);
                    break;
                case GameObjectTypes.FishingHole:
                case GameObjectTypes.Chest:
                default:
                    break;
            }

            go.SetRespawnTime(duration > 0 ? duration / Time.InMilliseconds : 0);
            go.SetOwnerGUID(unitCaster.GetGUID());
            go.SetSpellId(m_spellInfo.Id);

            ExecuteLogEffectSummonObject(effectInfo.Effect, go);

            Log.outDebug(LogFilter.Spells, "AddObject at SpellEfects.cpp EffectTransmitted");

            cMap.AddToMap(go);
            GameObject linkedTrap = go.GetLinkedTrap();
            if (linkedTrap != null)
            {
                PhasingHandler.InheritPhaseShift(linkedTrap, m_caster);
                linkedTrap.SetRespawnTime(duration > 0 ? duration / Time.InMilliseconds : 0);
                linkedTrap.SetSpellId(m_spellInfo.Id);
                linkedTrap.SetOwnerGUID(unitCaster.GetGUID());

                ExecuteLogEffectSummonObject(effectInfo.Effect, linkedTrap);
            }
        }

        [SpellEffectHandler(SpellEffectName.Prospecting)]
        void EffectProspecting()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player player = m_caster.ToPlayer();
            if (player == null)
                return;

            if (itemTarget == null || !itemTarget.GetTemplate().HasFlag(ItemFlags.IsProspectable))
                return;

            if (itemTarget.GetCount() < 5)
                return;

            if (WorldConfig.GetBoolValue(WorldCfg.SkillProspecting))
            {
                uint SkillValue = player.GetPureSkillValue(SkillType.Jewelcrafting);
                uint reqSkillValue = itemTarget.GetTemplate().GetRequiredSkillRank();
                player.UpdateGatherSkill(SkillType.Jewelcrafting, SkillValue, reqSkillValue);
            }

            itemTarget.loot = new Loot(player.GetMap(), itemTarget.GetGUID(), LootType.Prospecting, null);
            itemTarget.loot.FillLoot(itemTarget.GetEntry(), LootStorage.Prospecting, player, true);
            player.SendLoot(itemTarget.loot);
        }

        [SpellEffectHandler(SpellEffectName.Milling)]
        void EffectMilling()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player player = m_caster.ToPlayer();
            if (player == null)
                return;

            if (itemTarget == null || !itemTarget.GetTemplate().HasFlag(ItemFlags.IsMillable))
                return;

            if (itemTarget.GetCount() < 5)
                return;

            if (WorldConfig.GetBoolValue(WorldCfg.SkillMilling))
            {
                uint SkillValue = player.GetPureSkillValue(SkillType.Inscription);
                uint reqSkillValue = itemTarget.GetTemplate().GetRequiredSkillRank();
                player.UpdateGatherSkill(SkillType.Inscription, SkillValue, reqSkillValue);
            }

            itemTarget.loot = new Loot(player.GetMap(), itemTarget.GetGUID(), LootType.Milling, null);
            itemTarget.loot.FillLoot(itemTarget.GetEntry(), LootStorage.Milling, player, true);
            player.SendLoot(itemTarget.loot);
        }

        [SpellEffectHandler(SpellEffectName.Skill)]
        void EffectSkill()
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Player playerTarget = GetUnitCasterForEffectHandlers()?.ToPlayer();
            if (playerTarget == null)
                return;

            if (damage < 1)
                return;

            uint skillid = (uint)effectInfo.MiscValue;
            if (playerTarget.GetSkillStep(skillid) >= damage)
                return;

            var rcEntry = Global.DB2Mgr.GetSkillRaceClassInfo(skillid, playerTarget.GetRace(), playerTarget.GetClass());
            if (rcEntry == null)
                return;

            var tier = Global.ObjectMgr.GetSkillTier(rcEntry.SkillTierID);
            if (tier == null)
                return;

            ushort skillval = Math.Max((ushort)1, playerTarget.GetPureSkillValue(skillid));
            ushort maxSkillVal = (ushort)tier.GetValueForTierIndex(damage - 1);

            if (rcEntry.HasFlag(SkillRaceClassInfoFlags.AlwaysMaxValue))
                skillval = maxSkillVal;

            playerTarget.SetSkill(skillid, (uint)damage, skillval, maxSkillVal);
        }

        void EffectSpiritHeal()
        {
            Unit caster = GetCaster().ToUnit();
            if (effectHandleMode == SpellEffectHandleMode.Hit)
                caster.CastSpell(null, BattlegroundConst.SpellResurrectionVisual, true);

            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player playerTarget = unitTarget.ToPlayer();
            if (playerTarget != null)
            {
                if (!playerTarget.IsInWorld)
                    return;

                // skip if player does not want to live
                if (!playerTarget.CanAcceptAreaSpiritHealFrom(caster))
                    return;

                playerTarget.ResurrectPlayer(1.0f);
                playerTarget.CastSpell(playerTarget, BattlegroundConst.SpellPetSummoned, true);
                playerTarget.CastSpell(playerTarget, BattlegroundConst.SpellSpiritHealMana, true);
                playerTarget.SpawnCorpseBones(false);
            }
        }

        // remove insignia spell effect
        [SpellEffectHandler(SpellEffectName.SkinPlayerCorpse)]
        void EffectSkinPlayerCorpse()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Log.outDebug(LogFilter.Spells, "Effect: SkinPlayerCorpse");

            Player player = m_caster.ToPlayer();
            Player target = null;
            if (unitTarget != null)
                target = unitTarget.ToPlayer();
            else if (corpseTarget != null)
                target = Global.ObjAccessor.FindPlayer(corpseTarget.GetOwnerGUID());

            if (player == null || target == null || target.IsAlive())
                return;

            target.RemovedInsignia(player);
        }

        [SpellEffectHandler(SpellEffectName.StealBeneficialBuff)]
        void EffectStealBeneficialBuff()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Log.outDebug(LogFilter.Spells, "Effect: StealBeneficialBuff");

            if (unitTarget == null || unitTarget == m_caster)                 // can't steal from self
                return;

            List<DispelableAura> stealList = new();

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
                    if (!aurApp.IsPositive() || aura.IsPassive() || aura.GetSpellInfo().HasAttribute(SpellAttr4.CannotBeStolen))
                        continue;

                    // 2.4.3 Patch Notes: "Dispel effects will no longer attempt to remove effects that have 100% dispel resistance."
                    int chance = aura.CalcDispelChance(unitTarget, !unitTarget.IsFriendlyTo(m_caster));
                    if (chance == 0)
                        continue;

                    // The charges / stack amounts don't count towards the total number of auras that can be dispelled.
                    // Ie: A dispel on a target with 5 stacks of Winters Chill and a Polymorph has 1 / (1 + 1) . 50% chance to dispell
                    // Polymorph instead of 1 / (5 + 1) . 16%.
                    bool dispelCharges = aura.GetSpellInfo().HasAttribute(SpellAttr7.DispelRemovesCharges);
                    byte charges = dispelCharges ? aura.GetCharges() : aura.GetStackAmount();
                    if (charges > 0)
                        stealList.Add(new DispelableAura(aura, chance, charges));
                }
            }

            if (stealList.Empty())
                return;

            int remaining = stealList.Count;

            // Ok if exist some buffs for dispel try dispel it
            List<Tuple<uint, ObjectGuid, int>> successList = new();

            DispelFailed dispelFailed = new();
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
                    byte stolenCharges = 1;
                    if (dispelableAura.GetAura().GetSpellInfo().HasAttribute(SpellAttr1.DispelAllStacks))
                        stolenCharges = dispelableAura.GetDispelCharges();

                    successList.Add(Tuple.Create(dispelableAura.GetAura().GetId(), dispelableAura.GetAura().GetCasterGUID(), (int)stolenCharges));
                    if (!dispelableAura.DecrementCharge(stolenCharges))
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

            SpellDispellLog spellDispellLog = new();
            spellDispellLog.IsBreak = false; // TODO: use me
            spellDispellLog.IsSteal = true;

            spellDispellLog.TargetGUID = unitTarget.GetGUID();
            spellDispellLog.CasterGUID = m_caster.GetGUID();
            spellDispellLog.DispelledBySpellID = m_spellInfo.Id;

            foreach (var (spellId, auraCaster, stolenCharges) in successList)
            {
                var dispellData = new SpellDispellData();
                dispellData.SpellID = spellId;
                dispellData.Harmful = false;      // TODO: use me

                unitTarget.RemoveAurasDueToSpellBySteal(spellId, auraCaster, m_caster, stolenCharges);

                spellDispellLog.DispellData.Add(dispellData);
            }
            m_caster.SendMessageToSet(spellDispellLog, true);

            m_UniqueTargetInfo.Find(p => p.TargetGUID == unitTarget.GetGUID()).ProcHitMask |= ProcFlagsHit.Dispel;
        }

        [SpellEffectHandler(SpellEffectName.KillCredit)]
        void EffectKillCreditPersonal()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            unitTarget.ToPlayer().KilledMonsterCredit((uint)effectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.KillCredit2)]
        void EffectKillCredit()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            int creatureEntry = effectInfo.MiscValue;
            if (creatureEntry != 0)
                unitTarget.ToPlayer().RewardPlayerAndGroupAtEvent((uint)creatureEntry, unitTarget);
        }

        [SpellEffectHandler(SpellEffectName.KillCreditLabel1)]
        [SpellEffectHandler(SpellEffectName.KillCreditLabel2)]
        void EffectKillCreditLabel()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player playerTarget = unitTarget?.ToPlayer();
            if (playerTarget == null)
                return;

            playerTarget.UpdateQuestObjectiveProgress(QuestObjectiveType.KillWithLabel, effectInfo.MiscValue, Math.Max(1, effectInfo.MiscValueB));
        }

        [SpellEffectHandler(SpellEffectName.QuestFail)]
        void EffectQuestFail()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            unitTarget.ToPlayer().FailQuest((uint)effectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.QuestStart)]
        void EffectQuestStart()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            Player player = unitTarget.ToPlayer();
            if (player == null)
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

        [SpellEffectHandler(SpellEffectName.CreateTamedPet)]
        void EffectCreateTamedPet()
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
            unitTarget.GetClosePoint(out px, out py, out pz, pet.GetCombatReach(), SharedConst.PetFollowDist, pet.GetFollowAngle());
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
        void EffectDiscoverTaxi()
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
        void EffectTitanGrip()
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (m_caster.IsTypeId(TypeId.Player))
                m_caster.ToPlayer().SetCanTitanGrip(true, (uint)effectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.RedirectThreat)]
        void EffectRedirectThreat()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster == null)
                return;

            if (unitTarget != null)
                unitCaster.GetThreatManager().RegisterRedirectThreat(m_spellInfo.Id, unitTarget.GetGUID(), (uint)damage);
        }

        [SpellEffectHandler(SpellEffectName.GameObjectDamage)]
        void EffectGameObjectDamage()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (gameObjTarget == null)
                return;

            FactionTemplateRecord casterFaction = m_caster.GetFactionTemplateEntry();
            FactionTemplateRecord targetFaction = CliDB.FactionTemplateStorage.LookupByKey(gameObjTarget.GetFaction());
            // Do not allow to damage GO's of friendly factions (ie: Wintergrasp Walls/Ulduar Storm Beacons)
            if (targetFaction == null || (casterFaction != null && !casterFaction.IsFriendlyTo(targetFaction)))
                gameObjTarget.ModifyHealth(-damage, m_caster, GetSpellInfo().Id);
        }

        [SpellEffectHandler(SpellEffectName.GameobjectRepair)]
        void EffectGameObjectRepair()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (gameObjTarget == null)
                return;

            gameObjTarget.ModifyHealth(damage, m_caster);
        }

        [SpellEffectHandler(SpellEffectName.GameobjectSetDestructionState)]
        void EffectGameObjectSetDestructionState()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (gameObjTarget == null)
                return;

            gameObjTarget.SetDestructibleState((GameObjectDestructibleState)effectInfo.MiscValue, m_caster, true);
        }

        void SummonGuardian(SpellEffectInfo effect, uint entry, SummonPropertiesRecord properties, uint numGuardians, ObjectGuid privateObjectOwner)
        {
            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster == null)
                return;

            if (unitCaster.IsTotem())
                unitCaster = unitCaster.ToTotem().GetOwner();

            // in another case summon new
            float radius = 5.0f;
            TimeSpan duration = TimeSpan.FromMilliseconds(m_spellInfo.CalcDuration(m_originalCaster));

            //TempSummonType summonType = (duration == 0) ? TempSummonType.DeadDespawn : TempSummonType.TimedDespawn;
            Map map = unitCaster.GetMap();

            for (uint count = 0; count < numGuardians; ++count)
            {
                Position pos;
                if (count == 0)
                    pos = destTarget.GetPosition();
                else
                    // randomize position for multiple summons
                    pos = unitCaster.GetRandomPoint(destTarget, radius);

                TempSummon summon = map.SummonCreature(entry, pos, properties, duration, unitCaster, m_spellInfo.Id, 0, privateObjectOwner);
                if (summon == null)
                    return;

                if (summon.IsGuardian())
                {
                    // level of pet summoned using engineering item based at engineering skill level
                    if (m_CastItem != null && unitCaster.IsPlayer())
                    {
                        ItemTemplate proto = m_CastItem.GetTemplate();
                        if (proto != null)
                        {
                            if (proto.GetRequiredSkill() == (uint)SkillType.Engineering)
                            {
                                ushort skill202 = unitCaster.ToPlayer().GetSkillValue(SkillType.Engineering);
                                if (skill202 != 0)
                                    ((Guardian)summon).InitStatsForLevel((uint)(skill202 / 5));
                            }
                        }
                    }
                }

                if (summon.HasUnitTypeMask(UnitTypeMask.Minion) && m_targets.HasDst())
                    ((Minion)summon).SetFollowAngle(unitCaster.GetAbsoluteAngle(summon.GetPosition()));

                if (summon.GetEntry() == 27893)
                {
                    VisibleItem weapon = m_caster.ToPlayer().m_playerData.VisibleItems[EquipmentSlot.MainHand];
                    if (weapon.ItemID != 0)
                    {
                        summon.SetDisplayId(11686);
                        summon.SetVirtualItem(0, weapon.ItemID, weapon.ItemAppearanceModID, weapon.ItemVisual);
                    }
                    else
                        summon.SetDisplayId(1126);
                }

                ExecuteLogEffectSummonObject(effect.Effect, summon);
            }
        }

        [SpellEffectHandler(SpellEffectName.AllowRenamePet)]
        void EffectRenamePet()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Unit) ||
                !unitTarget.IsPet() || unitTarget.ToPet().GetPetType() != PetType.Hunter)
                return;

            unitTarget.SetPetFlag(UnitPetFlags.CanBeRenamed);
        }

        [SpellEffectHandler(SpellEffectName.PlayMusic)]
        void EffectPlayMusic()
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
        void EffectActivateSpec()
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
        void EffectPlaySound()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            Player player = unitTarget.ToPlayer();
            if (player == null)
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
        [SpellEffectHandler(SpellEffectName.RemoveAura2)]
        void EffectRemoveAura()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;
            // there may be need of specifying casterguid of removed auras
            unitTarget.RemoveAurasDueToSpell(effectInfo.TriggerSpell);
        }

        [SpellEffectHandler(SpellEffectName.DamageFromMaxHealthPCT)]
        void EffectDamageFromMaxHealthPCT()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            m_damage += (int)unitTarget.CountPctFromMaxHealth(damage);
        }

        [SpellEffectHandler(SpellEffectName.GiveCurrency)]
        void EffectGiveCurrency()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            if (!CliDB.CurrencyTypesStorage.ContainsKey(effectInfo.MiscValue))
                return;

            unitTarget.ToPlayer().ModifyCurrency((uint)effectInfo.MiscValue, damage, CurrencyGainSource.Spell, CurrencyDestroyReason.Spell);
        }

        [SpellEffectHandler(SpellEffectName.CastButton)]
        void EffectCastButtons()
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Player player = m_caster.ToPlayer();
            if (player == null)
                return;

            int button_id = effectInfo.MiscValue + 132;
            int n_buttons = effectInfo.MiscValueB;

            for (; n_buttons != 0; --n_buttons, ++button_id)
            {
                ActionButton ab = player.GetActionButton((byte)button_id);
                if (ab == null || ab.GetButtonType() != ActionButtonType.Spell)
                    continue;

                //! Action button data is unverified when it's set so it can be "hacked"
                //! to contain invalid spells, so filter here.
                uint spell_id = (uint)ab.GetAction();
                if (spell_id == 0)
                    continue;

                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spell_id, GetCastDifficulty());
                if (spellInfo == null)
                    continue;

                if (!player.HasSpell(spell_id) || player.GetSpellHistory().HasCooldown(spell_id))
                    continue;

                if (!spellInfo.HasAttribute(SpellAttr7.CanBeMultiCast))
                    continue;

                CastSpellExtraArgs args = new(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.IgnoreCastTime | TriggerCastFlags.CastDirectly | TriggerCastFlags.DontReportCastError);
                args.OriginalCastId = m_castId;
                args.CastDifficulty = GetCastDifficulty();
                m_caster.CastSpell(m_caster, spellInfo.Id, args);
            }
        }

        [SpellEffectHandler(SpellEffectName.RechargeItem)]
        void EffectRechargeItem()
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
                foreach (ItemEffectRecord itemEffect in item.GetEffects())
                    if (itemEffect.LegacySlotIndex <= item.m_itemData.SpellCharges.GetSize())
                        item.SetSpellCharges(itemEffect.LegacySlotIndex, itemEffect.Charges);

                item.SetState(ItemUpdateState.Changed, player);
            }
        }

        [SpellEffectHandler(SpellEffectName.Bind)]
        void EffectBind()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            Player player = unitTarget.ToPlayer();

            WorldLocation homeLoc = new();
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

            Log.outDebug(LogFilter.Spells, $"EffectBind: New homebind: {homeLoc}, AreaId: {areaId}");

            // zone update
            player.SendPlayerBound(m_caster.GetGUID(), areaId);
        }

        [SpellEffectHandler(SpellEffectName.TeleportToReturnPoint)]
        void EffectTeleportToReturnPoint()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player player = unitTarget.ToPlayer();
            if (player != null)
            {
                WorldLocation dest = player.GetStoredAuraTeleportLocation((uint)effectInfo.MiscValue);
                if (dest != null)
                    player.TeleportTo(dest, unitTarget == m_caster ? TeleportToOptions.Spell | TeleportToOptions.NotLeaveCombat : 0);
            }
        }

        [SpellEffectHandler(SpellEffectName.IncreseCurrencyCap)]
        void EffectIncreaseCurrencyCap()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (damage <= 0)
                return;

            unitTarget.ToPlayer()?.IncreaseCurrencyCap((uint)effectInfo.MiscValue, (uint)damage);
        }

        [SpellEffectHandler(SpellEffectName.SummonRafFriend)]
        void EffectSummonRaFFriend()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!m_caster.IsTypeId(TypeId.Player) || unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            m_caster.CastSpell(unitTarget, effectInfo.TriggerSpell, new CastSpellExtraArgs(this));
        }

        [SpellEffectHandler(SpellEffectName.UnlockGuildVaultTab)]
        void EffectUnlockGuildVaultTab()
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            // Safety checks done in Spell.CheckCast
            Player caster = m_caster.ToPlayer();
            Guild guild = caster.GetGuild();
            if (guild != null)
                guild.HandleBuyBankTab(caster.GetSession(), (byte)(damage - 1)); // Bank tabs start at zero internally
        }

        [SpellEffectHandler(SpellEffectName.SummonPersonalGameobject)]
        void EffectSummonPersonalGameObject()
        {
            if (effectHandleMode != SpellEffectHandleMode.Launch)
                return;

            uint goId = (uint)effectInfo.MiscValue;
            if (goId == 0)
                return;

            float x, y, z, o;
            if (m_targets.HasDst())
                destTarget.GetPosition(out x, out y, out z, out o);
            else
            {
                m_caster.GetClosePoint(out x, out y, out z, SharedConst.DefaultPlayerBoundingRadius);
                o = m_caster.GetOrientation();
            }

            Map map = m_caster.GetMap();
            Position pos = new(x, y, z, o);
            Quaternion rot = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(o, 0.0f, 0.0f));
            GameObject go = GameObject.CreateGameObject(goId, map, pos, rot, 255, GameObjectState.Ready);

            if (go == null)
            {
                Log.outWarn(LogFilter.Spells, $"SpellEffect Failed to summon personal gameobject. SpellId {m_spellInfo.Id}, effect {effectInfo.EffectIndex}");
                return;
            }

            PhasingHandler.InheritPhaseShift(go, m_caster);

            int duration = m_spellInfo.CalcDuration(m_caster);

            go.SetRespawnTime(duration > 0 ? duration / Time.InMilliseconds : 0);
            go.SetSpellId(m_spellInfo.Id);
            go.SetPrivateObjectOwner(m_caster.GetGUID());

            ExecuteLogEffectSummonObject(effectInfo.Effect, go);

            map.AddToMap(go);

            GameObject linkedTrap = go.GetLinkedTrap();
            if (linkedTrap != null)
            {
                PhasingHandler.InheritPhaseShift(linkedTrap, m_caster);

                linkedTrap.SetRespawnTime(duration > 0 ? duration / Time.InMilliseconds : 0);
                linkedTrap.SetSpellId(m_spellInfo.Id);

                ExecuteLogEffectSummonObject(effectInfo.Effect, linkedTrap);
            }
        }

        [SpellEffectHandler(SpellEffectName.ResurrectWithAura)]
        void EffectResurrectWithAura()
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
            if (Global.SpellMgr.HasSpellInfo(effectInfo.TriggerSpell, Difficulty.None))
                resurrectAura = effectInfo.TriggerSpell;

            if (resurrectAura != 0 && target.HasAura(resurrectAura))
                return;

            ExecuteLogEffectResurrect(effectInfo.Effect, target);
            target.SetResurrectRequestData(m_caster, health, mana, resurrectAura);
            SendResurrectRequest(target);
        }

        [SpellEffectHandler(SpellEffectName.CreateAreaTrigger)]
        void EffectCreateAreaTrigger()
        {
            if (effectHandleMode != SpellEffectHandleMode.Launch)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster == null || !m_targets.HasDst())
                return;

            AreaTriggerId createPropertiesId = new((uint)effectInfo.MiscValue, false);
            int duration = GetSpellInfo().CalcDuration(GetCaster());
            AreaTrigger.CreateAreaTrigger(createPropertiesId, destTarget.GetPosition(), duration, unitCaster, null, m_SpellVisual, GetSpellInfo(), this);
        }

        [SpellEffectHandler(SpellEffectName.RemoveTalent)]
        void EffectRemoveTalent()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            TalentRecord talent = CliDB.TalentStorage.LookupByKey(m_misc.TalentId);
            if (talent == null)
                return;

            Player player = unitTarget != null ? unitTarget.ToPlayer() : null;
            if (player == null)
                return;

            player.RemoveTalent(talent);
            player.SendTalentsInfoData();
        }

        [SpellEffectHandler(SpellEffectName.DestroyItem)]
        void EffectDestroyItem()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            Player player = unitTarget.ToPlayer();
            Item item = player.GetItemByEntry(effectInfo.ItemType);
            if (item != null)
                player.DestroyItem(item.GetBagSlot(), item.GetSlot(), true);
        }

        [SpellEffectHandler(SpellEffectName.LearnGarrisonBuilding)]
        void EffectLearnGarrisonBuilding()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            Garrison garrison = unitTarget.ToPlayer().GetGarrison();
            if (garrison != null)
                garrison.LearnBlueprint((uint)effectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.RemoveAuraBySApellLabel)]
        void EffectRemoveAuraBySpellLabel()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            unitTarget.RemoveAppliedAuras(aurApp => aurApp.GetBase().GetSpellInfo().HasLabel((uint)effectInfo.MiscValue));
        }

        [SpellEffectHandler(SpellEffectName.CreateGarrison)]
        void EffectCreateGarrison()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            unitTarget.ToPlayer().CreateGarrison((uint)effectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.CreateConversation)]
        void EffectCreateConversation()
        {
            if (effectHandleMode != SpellEffectHandleMode.Launch)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster == null || !m_targets.HasDst())
                return;

            Conversation.CreateConversation((uint)effectInfo.MiscValue, unitCaster, destTarget.GetPosition(), ObjectGuid.Empty, GetSpellInfo());
        }

        [SpellEffectHandler(SpellEffectName.CancelConversation)]
        void EffectCancelConversation()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null)
                return;

            List<WorldObject> objs = new();
            ObjectEntryAndPrivateOwnerIfExistsCheck check = new(unitTarget.GetGUID(), (uint)effectInfo.MiscValue);
            WorldObjectListSearcher checker = new(unitTarget, objs, check, GridMapTypeMask.Conversation);
            Cell.VisitGridObjects(unitTarget, checker, 100.0f);

            foreach (WorldObject obj in objs)
            {
                Conversation convo = obj.ToConversation();
                if (convo != null)
                    convo.Remove();
            }
        }

        [SpellEffectHandler(SpellEffectName.AddGarrisonFollower)]
        void EffectAddGarrisonFollower()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            Garrison garrison = unitTarget.ToPlayer().GetGarrison();
            if (garrison != null)
                garrison.AddFollower((uint)effectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.CreateHeirloomItem)]
        void EffectCreateHeirloomItem()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player player = m_caster.ToPlayer();
            if (player == null)
                return;

            CollectionMgr collectionMgr = player.GetSession().GetCollectionMgr();
            if (collectionMgr == null)
                return;

            List<uint> bonusList = new();
            bonusList.Add(collectionMgr.GetHeirloomBonus(m_misc.Data0));

            DoCreateItem(m_misc.Data0, ItemContext.None, bonusList);
            ExecuteLogEffectCreateItem(effectInfo.Effect, m_misc.Data0);
        }

        [SpellEffectHandler(SpellEffectName.ActivateGarrisonBuilding)]
        void EffectActivateGarrisonBuilding()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            Garrison garrison = unitTarget.ToPlayer().GetGarrison();
            if (garrison != null)
                garrison.ActivateBuilding((uint)effectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.GrantBattlepetLevel)]
        void EffectGrantBattlePetLevel()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player playerCaster = m_caster.ToPlayer();
            if (playerCaster == null)
                return;

            if (unitTarget == null || !unitTarget.IsCreature())
                return;

            playerCaster.GetSession().GetBattlePetMgr().GrantBattlePetLevel(unitTarget.GetBattlePetCompanionGUID(), (ushort)damage);
        }

        [SpellEffectHandler(SpellEffectName.GiveExperience)]
        void EffectGiveExperience()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player playerTarget = unitTarget?.ToPlayer();
            if (playerTarget == null)
                return;

            uint xp = Quest.XPValue(playerTarget, (uint)effectInfo.MiscValue, (uint)effectInfo.MiscValueB);
            playerTarget.GiveXP(xp, null);
        }

        [SpellEffectHandler(SpellEffectName.GiveRestedEcperienceBonus)]
        void EffectGiveRestedExperience()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player playerTarget = unitTarget?.ToPlayer();
            if (playerTarget == null)
                return;

            // effect value is number of resting hours
            playerTarget.GetRestMgr().AddRestBonus(RestTypes.XP, damage * Time.Hour * playerTarget.GetRestMgr().CalcExtraPerSec(RestTypes.XP, 0.125f));
        }

        [SpellEffectHandler(SpellEffectName.HealBattlepetPct)]
        void EffectHealBattlePetPct()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            BattlePetMgr battlePetMgr = unitTarget.ToPlayer().GetSession().GetBattlePetMgr();
            if (battlePetMgr != null)
                battlePetMgr.HealBattlePetsPct((byte)damage);
        }

        [SpellEffectHandler(SpellEffectName.EnableBattlePets)]
        void EffectEnableBattlePets()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsPlayer())
                return;

            Player player = unitTarget.ToPlayer();
            player.SetPlayerFlag(PlayerFlags.PetBattlesUnlocked);
            player.GetSession().GetBattlePetMgr().UnlockSlot(BattlePetSlots.Slot0);
        }

        [SpellEffectHandler(SpellEffectName.ChangeBattlepetQuality)]
        void EffectChangeBattlePetQuality()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player playerCaster = m_caster.ToPlayer();
            if (playerCaster == null)
                return;

            if (unitTarget == null || !unitTarget.IsCreature())
                return;

            var qualityRecord = CliDB.BattlePetBreedQualityStorage.Values.FirstOrDefault(a1 => a1.MaxQualityRoll < damage);

            BattlePetBreedQuality quality = BattlePetBreedQuality.Poor;
            if (qualityRecord != null)
                quality = (BattlePetBreedQuality)qualityRecord.QualityEnum;

            playerCaster.GetSession().GetBattlePetMgr().ChangeBattlePetQuality(unitTarget.GetBattlePetCompanionGUID(), quality);
        }

        [SpellEffectHandler(SpellEffectName.LaunchQuestChoice)]
        void EffectLaunchQuestChoice()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsPlayer())
                return;

            unitTarget.ToPlayer().SendPlayerChoice(GetCaster().GetGUID(), effectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.UncageBattlepet)]
        void EffectUncageBattlePet()
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (m_CastItem == null || m_caster == null || !m_caster.IsTypeId(TypeId.Player))
                return;

            uint speciesId = m_CastItem.GetModifier(ItemModifier.BattlePetSpeciesId);
            ushort breed = (ushort)(m_CastItem.GetModifier(ItemModifier.BattlePetBreedData) & 0xFFFFFF);
            BattlePetBreedQuality quality = (BattlePetBreedQuality)((m_CastItem.GetModifier(ItemModifier.BattlePetBreedData) >> 24) & 0xFF);
            ushort level = (ushort)m_CastItem.GetModifier(ItemModifier.BattlePetLevel);
            uint displayId = m_CastItem.GetModifier(ItemModifier.BattlePetDisplayId);

            BattlePetSpeciesRecord speciesEntry = CliDB.BattlePetSpeciesStorage.LookupByKey(speciesId);
            if (speciesEntry == null)
                return;

            Player player = m_caster.ToPlayer();
            BattlePetMgr battlePetMgr = player.GetSession().GetBattlePetMgr();
            if (battlePetMgr == null)
                return;

            if (battlePetMgr.GetMaxPetLevel() < level)
            {
                battlePetMgr.SendError(BattlePetError.TooHighLevelToUncage, speciesEntry.CreatureID);
                SendCastResult(SpellCastResult.CantAddBattlePet);
                return;
            }

            if (battlePetMgr.HasMaxPetCount(speciesEntry, player.GetGUID()))
            {
                battlePetMgr.SendError(BattlePetError.CantHaveMorePetsOfThatType, speciesEntry.CreatureID);
                SendCastResult(SpellCastResult.CantAddBattlePet);
                return;
            }

            battlePetMgr.AddPet(speciesId, displayId, breed, quality, level);

            player.SendPlaySpellVisual(player, SharedConst.SpellVisualUncagePet, 0, 0, 0.0f, false);

            player.DestroyItem(m_CastItem.GetBagSlot(), m_CastItem.GetSlot(), true);
            m_CastItem = null;
        }

        [SpellEffectHandler(SpellEffectName.UpgradeHeirloom)]
        void EffectUpgradeHeirloom()
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Player player = m_caster.ToPlayer();
            if (player != null)
            {
                CollectionMgr collectionMgr = player.GetSession().GetCollectionMgr();
                if (collectionMgr != null)
                    collectionMgr.UpgradeHeirloom(m_misc.Data0, m_castItemEntry);
            }
        }

        [SpellEffectHandler(SpellEffectName.ApplyEnchantIllusion)]
        void EffectApplyEnchantIllusion()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (itemTarget == null)
                return;

            Player player = m_caster.ToPlayer();
            if (player == null || player.GetGUID() != itemTarget.GetOwnerGUID())
                return;

            itemTarget.SetState(ItemUpdateState.Changed, player);
            itemTarget.SetModifier(ItemModifier.EnchantIllusionAllSpecs, (uint)effectInfo.MiscValue);
            if (itemTarget.IsEquipped())
                player.SetVisibleItemSlot(itemTarget.GetSlot(), itemTarget);

            player.RemoveTradeableItem(itemTarget);
            itemTarget.ClearSoulboundTradeable(player);
        }

        [SpellEffectHandler(SpellEffectName.UpdatePlayerPhase)]
        void EffectUpdatePlayerPhase()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            PhasingHandler.OnConditionChange(unitTarget);
        }

        [SpellEffectHandler(SpellEffectName.UpdateZoneAurasPhases)]
        void EffectUpdateZoneAurasAndPhases()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsTypeId(TypeId.Player))
                return;

            unitTarget.ToPlayer().UpdateAreaDependentAuras(unitTarget.GetAreaId());
        }

        [SpellEffectHandler(SpellEffectName.GiveArtifactPower)]
        void EffectGiveArtifactPower()
        {
            if (effectHandleMode != SpellEffectHandleMode.LaunchTarget)
                return;

            Player playerCaster = m_caster.ToPlayer();
            if (playerCaster == null)
                return;

            Aura artifactAura = playerCaster.GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);
            if (artifactAura != null)
            {
                Item artifact = playerCaster.GetItemByGuid(artifactAura.GetCastItemGUID());
                if (artifact != null)
                    artifact.GiveArtifactXp((ulong)damage, m_CastItem, (ArtifactCategory)effectInfo.MiscValue);
            }
        }

        [SpellEffectHandler(SpellEffectName.GiveArtifactPowerNoBonus)]
        void EffectGiveArtifactPowerNoBonus()
        {
            if (effectHandleMode != SpellEffectHandleMode.LaunchTarget)
                return;

            if (unitTarget == null || !m_caster.IsTypeId(TypeId.Player))
                return;

            Aura artifactAura = unitTarget.GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);
            if (artifactAura != null)
            {
                Item artifact = unitTarget.ToPlayer().GetItemByGuid(artifactAura.GetCastItemGUID());
                if (artifact != null)
                    artifact.GiveArtifactXp((ulong)damage, m_CastItem, 0);
            }
        }

        [SpellEffectHandler(SpellEffectName.PlaySceneScriptPackage)]
        void EffectPlaySceneScriptPackage()
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (!m_caster.IsTypeId(TypeId.Player))
                return;

            m_caster.ToPlayer().GetSceneMgr().PlaySceneByPackageId((uint)effectInfo.MiscValue, SceneFlags.PlayerNonInteractablePhased, destTarget);
        }

        bool IsUnitTargetSceneObjectAura(Spell spell, TargetInfo target)
        {
            if (target.TargetGUID != spell.GetCaster().GetGUID())
                return false;

            foreach (SpellEffectInfo spellEffectInfo in spell.GetSpellInfo().GetEffects())
                if ((target.EffectMask & (1 << (int)spellEffectInfo.EffectIndex)) != 0 && spellEffectInfo.IsUnitOwnedAuraEffect())
                    return true;

            return false;
        }

        [SpellEffectHandler(SpellEffectName.CreateSceneObject)]
        void EffectCreateSceneObject()
        {
            if (effectHandleMode != SpellEffectHandleMode.Launch)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster == null || !m_targets.HasDst())
                return;

            SceneObject sceneObject = SceneObject.CreateSceneObject((uint)effectInfo.MiscValue, unitCaster, destTarget.GetPosition(), ObjectGuid.Empty);
            if (sceneObject != null)
            {
                bool hasAuraTargetingCaster = m_UniqueTargetInfo.Any(target => IsUnitTargetSceneObjectAura(this, target));
                if (hasAuraTargetingCaster)
                    sceneObject.SetCreatedBySpellCast(m_castId);
            }
        }

        [SpellEffectHandler(SpellEffectName.CreatePersonalSceneObject)]
        void EffectCreatePrivateSceneObject()
        {
            if (effectHandleMode != SpellEffectHandleMode.Launch)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster == null || !m_targets.HasDst())
                return;

            SceneObject sceneObject = SceneObject.CreateSceneObject((uint)effectInfo.MiscValue, unitCaster, destTarget.GetPosition(), unitCaster.GetGUID());
            if (sceneObject != null)
            {
                bool hasAuraTargetingCaster = m_UniqueTargetInfo.Any(target => IsUnitTargetSceneObjectAura(this, target));
                if (hasAuraTargetingCaster)
                    sceneObject.SetCreatedBySpellCast(m_castId);
            }
        }

        [SpellEffectHandler(SpellEffectName.PlayScene)]
        void EffectPlayScene()
        {
            if (effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (m_caster.GetTypeId() != TypeId.Player)
                return;

            m_caster.ToPlayer().GetSceneMgr().PlayScene((uint)effectInfo.MiscValue, destTarget);
        }

        [SpellEffectHandler(SpellEffectName.GiveHonor)]
        void EffectGiveHonor()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || unitTarget.GetTypeId() != TypeId.Player)
                return;

            PvPCredit packet = new();
            packet.Honor = damage;
            packet.OriginalHonor = damage;

            Player playerTarget = unitTarget.ToPlayer();
            playerTarget.AddHonorXP((uint)damage);
            playerTarget.SendPacket(packet);
        }

        [SpellEffectHandler(SpellEffectName.JumpCharge)]
        void EffectJumpCharge()
        {
            if (effectHandleMode != SpellEffectHandleMode.Launch)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster == null)
                return;

            if (unitCaster.IsInFlight())
                return;

            JumpChargeParams jumpParams = Global.ObjectMgr.GetJumpChargeParams(effectInfo.MiscValue);
            if (jumpParams == null)
                return;

            float speed = jumpParams.Speed;
            if (jumpParams.TreatSpeedAsMoveTimeSeconds)
                speed = unitCaster.GetExactDist(destTarget) / jumpParams.Speed;

            object facing = null;
            Unit target = m_targets.GetUnitTarget();
            if (target != null && m_spellInfo.HasAttribute(SpellAttr9.FaceUnitTargetUponCompletionOfJumpCharge))
                facing = target;

            JumpArrivalCastArgs arrivalCast = null;
            if (effectInfo.TriggerSpell != 0)
            {
                arrivalCast = new();
                arrivalCast.SpellId = effectInfo.TriggerSpell;
            }

            SpellEffectExtraData effectExtra = null;
            if (jumpParams.SpellVisualId.HasValue || jumpParams.ProgressCurveId.HasValue || jumpParams.ParabolicCurveId.HasValue)
            {
                effectExtra = new();
                if (jumpParams.SpellVisualId.HasValue)
                    effectExtra.SpellVisualId = jumpParams.SpellVisualId.Value;

                if (jumpParams.ProgressCurveId.HasValue)
                    effectExtra.ProgressCurveId = jumpParams.ProgressCurveId.Value;

                if (jumpParams.ParabolicCurveId.HasValue)
                    effectExtra.ParabolicCurveId = jumpParams.ParabolicCurveId.Value;
            }

            unitCaster.GetMotionMaster().MoveJumpWithGravity(destTarget, speed, jumpParams.JumpGravity, EventId.Jump, facing, m_spellInfo.HasAttribute(SpellAttr9.JumpchargeNoFacingControl), arrivalCast, effectExtra);
        }

        [SpellEffectHandler(SpellEffectName.LearnTransmogSet)]
        void EffectLearnTransmogSet()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (unitTarget == null || !unitTarget.IsPlayer())
                return;

            unitTarget.ToPlayer().GetSession().GetCollectionMgr().AddTransmogSet((uint)effectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.LearnAzeriteEssencePower)]
        void EffectLearnAzeriteEssencePower()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player playerTarget = unitTarget != null ? unitTarget.ToPlayer() : null;
            if (playerTarget == null)
                return;

            Item heartOfAzeroth = playerTarget.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);
            if (heartOfAzeroth == null)
                return;

            AzeriteItem azeriteItem = heartOfAzeroth.ToAzeriteItem();
            if (azeriteItem == null)
                return;

            // remove old rank and apply new one
            if (azeriteItem.IsEquipped())
            {
                SelectedAzeriteEssences selectedEssences = azeriteItem.GetSelectedAzeriteEssences();
                if (selectedEssences != null)
                {
                    for (int slot = 0; slot < SharedConst.MaxAzeriteEssenceSlot; ++slot)
                    {
                        if (selectedEssences.AzeriteEssenceID[slot] == effectInfo.MiscValue)
                        {
                            bool major = (AzeriteItemMilestoneType)Global.DB2Mgr.GetAzeriteItemMilestonePower(slot).Type == AzeriteItemMilestoneType.MajorEssence;
                            playerTarget.ApplyAzeriteEssence(azeriteItem, (uint)effectInfo.MiscValue, SharedConst.MaxAzeriteEssenceRank, major, false);
                            playerTarget.ApplyAzeriteEssence(azeriteItem, (uint)effectInfo.MiscValue, (uint)effectInfo.MiscValueB, major, false);
                            break;
                        }
                    }
                }
            }

            azeriteItem.SetEssenceRank((uint)effectInfo.MiscValue, (uint)effectInfo.MiscValueB);
            azeriteItem.SetState(ItemUpdateState.Changed, playerTarget);
        }

        [SpellEffectHandler(SpellEffectName.CreatePrivateConversation)]
        void EffectCreatePrivateConversation()
        {
            if (effectHandleMode != SpellEffectHandleMode.LaunchTarget)
                return;

            if (unitTarget.GetTypeId() != TypeId.Player)
                return;

            Conversation.CreateConversation((uint)effectInfo.MiscValue, unitTarget, destTarget.GetPosition(), unitTarget.GetGUID(), GetSpellInfo());
        }

        [SpellEffectHandler(SpellEffectName.ApplyMountEquipment)]
        void EffectApplyMountEquipment()
        {
            if (effectHandleMode != SpellEffectHandleMode.LaunchTarget)
                return;

            Player playerTarget = unitTarget.ToPlayer();
            if (playerTarget == null)
                return;

            foreach (var (_, mountEquipment) in CliDB.MountEquipmentStorage)
            {
                if (mountEquipment.LearnedBySpell == effectInfo.TriggerSpell)
                {
                    playerTarget.LearnSpell(mountEquipment.LearnedBySpell, false, 0, true);
                    var mountAuras = playerTarget.GetAuraEffectsByType(AuraType.Mounted);
                    if (!mountAuras.Empty())
                    {
                        var mountEntry = Global.DB2Mgr.GetMount(mountAuras.FirstOrDefault().GetId());
                        if (mountEntry != null && !mountEntry.HasFlag(MountFlags.MountEquipmentEffectsSuppressed))
                            playerTarget.CastSpell(playerTarget, (uint)mountEquipment.BuffSpell, true);
                    }
                }
                else
                {
                    playerTarget.RemoveOwnedAura((uint)mountEquipment.BuffSpell);
                    playerTarget.RemoveSpell(mountEquipment.LearnedBySpell, false, false, true);
                }
            }

            ApplyMountEquipmentResult applyMountEquipmentResult = new();
            applyMountEquipmentResult.ItemGUID = m_castItemGUID;
            applyMountEquipmentResult.ItemID = m_castItemEntry;
            applyMountEquipmentResult.Result = ApplyMountEquipmentResult.ApplyResult.Success;
            playerTarget.SendPacket(applyMountEquipmentResult);
        }

        [SpellEffectHandler(SpellEffectName.SendChatMessage)]
        void EffectSendChatMessage()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            if (unitCaster == null)
                return;

            uint broadcastTextId = (uint)effectInfo.MiscValue;
            if (!CliDB.BroadcastTextStorage.ContainsKey(broadcastTextId))
                return;

            ChatMsg chatType = (ChatMsg)effectInfo.MiscValueB;
            unitCaster.Talk(broadcastTextId, chatType, Global.CreatureTextMgr.GetRangeForChatType(chatType), unitTarget);
        }

        [SpellEffectHandler(SpellEffectName.GrantBattlepetExperience)]
        void EffectGrantBattlePetExperience()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player playerCaster = m_caster.ToPlayer();
            if (playerCaster == null)
                return;

            if (unitTarget == null || !unitTarget.IsCreature())
                return;

            playerCaster.GetSession().GetBattlePetMgr().GrantBattlePetExperience(unitTarget.GetBattlePetCompanionGUID(), (ushort)damage, BattlePetXpSource.SpellEffect);
        }

        [SpellEffectHandler(SpellEffectName.LearnTransmogIllusion)]
        void EffectLearnTransmogIllusion()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player player = unitTarget?.ToPlayer();
            if (player == null)
                return;

            uint illusionId = (uint)effectInfo.MiscValue;
            if (!CliDB.TransmogIllusionStorage.ContainsKey(illusionId))
                return;

            player.GetSession().GetCollectionMgr().AddTransmogIllusion(illusionId);
        }

        [SpellEffectHandler(SpellEffectName.ModifyAuraStacks)]
        void EffectModifyAuraStacks()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Aura targetAura = unitTarget.GetAura(effectInfo.TriggerSpell);
            if (targetAura == null)
                return;

            switch (effectInfo.MiscValue)
            {
                case 0:
                    targetAura.ModStackAmount(damage);
                    break;
                case 1:
                    targetAura.SetStackAmount((byte)damage);
                    break;
                default:
                    break;
            }
        }

        [SpellEffectHandler(SpellEffectName.ModifyCooldown)]
        void EffectModifyCooldown()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            unitTarget.GetSpellHistory().ModifyCooldown(effectInfo.TriggerSpell, TimeSpan.FromMilliseconds(damage));
        }

        [SpellEffectHandler(SpellEffectName.ModifyCooldowns)]
        void EffectModifyCooldowns()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            unitTarget.GetSpellHistory().ModifyCoooldowns(itr =>
            {
                SpellInfo spellOnCooldown = Global.SpellMgr.GetSpellInfo(itr.SpellId, Difficulty.None);
                if ((int)spellOnCooldown.SpellFamilyName != effectInfo.MiscValue)
                    return false;

                int bitIndex = effectInfo.MiscValueB - 1;
                if (bitIndex < 0 || bitIndex >= sizeof(uint) * 8)
                    return false;

                FlagArray128 reqFlag = new();
                reqFlag[bitIndex / 32] = 1u << (bitIndex % 32);
                return (spellOnCooldown.SpellFamilyFlags & reqFlag);
            }, TimeSpan.FromMilliseconds(damage));
        }

        [SpellEffectHandler(SpellEffectName.ModifyCooldownsByCategory)]
        void EffectModifyCooldownsByCategory()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            unitTarget.GetSpellHistory().ModifyCoooldowns(itr => Global.SpellMgr.GetSpellInfo(itr.SpellId, Difficulty.None).CategoryId == effectInfo.MiscValue, TimeSpan.FromMilliseconds(damage));
        }

        [SpellEffectHandler(SpellEffectName.ModifyCharges)]
        void EffectModifySpellCharges()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            for (int i = 0; i < damage; ++i)
                unitTarget.GetSpellHistory().RestoreCharge((uint)effectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.CreateTraitTreeConfig)]
        void EffectCreateTraitTreeConfig()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player target = unitTarget?.ToPlayer();
            if (target == null)
                return;

            if (target.IsLoading() && target.m_activePlayerData.TraitConfigs.Empty())
                return; // traits not loaded yet

            TraitConfigPacket newConfig = new();
            newConfig.Type = TraitMgr.GetConfigTypeForTree(effectInfo.MiscValue);
            if (newConfig.Type != TraitConfigType.Generic)
                return;

            newConfig.TraitSystemID = CliDB.TraitTreeStorage.LookupByKey(effectInfo.MiscValue).TraitSystemID;
            int existingConfigForSystem = target.m_activePlayerData.TraitConfigs.FindIndexIf(config =>
            {
                return config.Type == (int)TraitConfigType.Generic
                    && config.TraitSystemID == newConfig.TraitSystemID;
            });

            if (existingConfigForSystem < 0)
                target.CreateTraitConfig(newConfig);
        }

        [SpellEffectHandler(SpellEffectName.ChangeActiveCombatTraitConfig)]
        void EffectChangeActiveCombatTraitConfig()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player target = unitTarget?.ToPlayer();
            if (target == null)
                return;

            if (m_customArg is not TraitConfigPacket)
                return;

            target.UpdateTraitConfig(m_customArg as TraitConfigPacket, damage, false);
        }

        [SpellEffectHandler(SpellEffectName.TeleportGraveyard)]
        void EffectTeleportGraveyard()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            unitTarget.ToPlayer()?.RepopAtGraveyard();
        }

        [SpellEffectHandler(SpellEffectName.UpdateInteractions)]
        void EffectUpdateInteractions()
        {
            if (effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player target = unitTarget?.ToPlayer();
            if (target == null)
                return;

            target.UpdateVisibleObjectInteractions(true, false, true, true);
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

        public bool DecrementCharge(byte charges)
        {
            if (_charges == 0)
                return false;

            _charges -= charges;
            return _charges > 0;
        }

        Aura _aura;
        int _chance;
        byte _charges;
    }

    class DelayedSpellTeleportEvent : BasicEvent
    {
        Unit _target;
        WorldLocation _targetDest;
        TeleportToOptions _options;
        uint _spellId;

        public DelayedSpellTeleportEvent(Unit target, WorldLocation targetDest, TeleportToOptions options, uint spellId)
        {
            _target = target;
            _targetDest = targetDest;
            _options = options;
            _spellId = spellId;
        }

        public override bool Execute(ulong e_time, uint p_time)
        {

            Player player = _target.ToPlayer();
            if (player != null)
                player.TeleportTo(_targetDest, _options);
            else if (_targetDest.GetMapId() == _target.GetMapId())
                _target.NearTeleportTo(_targetDest, (_options & TeleportToOptions.Spell) != TeleportToOptions.None);
            else
                Log.outError(LogFilter.Spells, $"Spell::EffectTeleportUnitsWithVisualLoadingScreen - spellId {_spellId} attempted to teleport creature to a different map.");

            return true;
        }
    }
}