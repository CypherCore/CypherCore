// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Dynamic;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Maps.Checks;
using Game.Maps.Notifiers;
using Game.Networking.Packets;
using Game.Scripting.Interfaces.IUnit;

namespace Game.Spells.Auras.EffectHandlers
{
    public partial class AuraEffect
    {

        /************************/
        /***      FIGHT       ***/
        /************************/
        [AuraEffectHandler(AuraType.FeignDeath)]
        private void HandleFeignDeath(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                List<Unit> targets = new();
                var u_check = new AnyUnfriendlyUnitInObjectRangeCheck(target, target, target.GetMap().GetVisibilityRange());
                var searcher = new UnitListSearcher(target, targets, u_check);

                Cell.VisitAllObjects(target, searcher, target.GetMap().GetVisibilityRange());

                foreach (var unit in targets)
                {
                    if (!unit.HasUnitState(UnitState.Casting))
                        continue;

                    for (var i = CurrentSpellTypes.Generic; i < CurrentSpellTypes.Max; i++)
                        if (unit.GetCurrentSpell(i) != null &&
                            unit.GetCurrentSpell(i)._targets.GetUnitTargetGUID() == target.GetGUID())
                            unit.InterruptSpell(i, false);
                }

                foreach (var pair in target.GetThreatManager().GetThreatenedByMeList())
                    pair.Value.ScaleThreat(0.0f);

                if (target.GetMap().IsDungeon()) // feign death does not remove combat in dungeons
                {
                    target.AttackStop();
                    Player targetPlayer = target.ToPlayer();

                    targetPlayer?.SendAttackSwingCancelAttack();
                }
                else
                {
                    target.CombatStop(false, false);
                }

                // prevent interrupt message
                if (GetCasterGUID() == target.GetGUID() &&
                    target.GetCurrentSpell(CurrentSpellTypes.Generic) != null)
                    target.FinishSpell(CurrentSpellTypes.Generic, false);

                target.InterruptNonMeleeSpells(true);

                // stop handling the effect if it was removed by linked event
                if (aurApp.HasRemoveMode())
                    return;

                target.SetUnitFlag(UnitFlags.PreventEmotesFromChatText);
                target.SetUnitFlag2(UnitFlags2.FeignDeath);
                target.SetUnitFlag3(UnitFlags3.FakeDead);
                target.AddUnitState(UnitState.Died);

                Creature creature = target.ToCreature();

                creature?.SetReactState(ReactStates.Passive);
            }
            else
            {
                target.RemoveUnitFlag(UnitFlags.PreventEmotesFromChatText);
                target.RemoveUnitFlag2(UnitFlags2.FeignDeath);
                target.RemoveUnitFlag3(UnitFlags3.FakeDead);
                target.ClearUnitState(UnitState.Died);

                Creature creature = target.ToCreature();

                creature?.InitializeReactState();
            }
        }

        [AuraEffectHandler(AuraType.ModUnattackable)]
        private void HandleModUnattackable(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
            if (!apply &&
                target.HasAuraType(AuraType.ModUnattackable))
                return;

            if (apply)
                target.SetUnitFlag(UnitFlags.NonAttackable2);
            else
                target.RemoveUnitFlag(UnitFlags.NonAttackable2);

            // call functions which may have additional effects after changing State of unit
            if (apply && mode.HasAnyFlag(AuraEffectHandleModes.Real))
            {
                if (target.GetMap().IsDungeon())
                {
                    target.AttackStop();
                    Player targetPlayer = target.ToPlayer();

                    targetPlayer?.SendAttackSwingCancelAttack();
                }
                else
                {
                    target.CombatStop();
                }
            }
        }

        [AuraEffectHandler(AuraType.ModDisarm)]
        private void HandleAuraModDisarm(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            //Prevent handling aura twice
            AuraType type = GetAuraType();

            if (apply ? target.GetAuraEffectsByType(type).Count > 1 : target.HasAuraType(type))
                return;

            Action<Unit> flagChangeFunc = null;
            byte slot;
            WeaponAttackType attType;

            switch (type)
            {
                case AuraType.ModDisarm:
                    if (apply)
                        flagChangeFunc = unit => { unit.SetUnitFlag(UnitFlags.Disarmed); };
                    else
                        flagChangeFunc = unit => { unit.RemoveUnitFlag(UnitFlags.Disarmed); };

                    slot = EquipmentSlot.MainHand;
                    attType = WeaponAttackType.BaseAttack;

                    break;
                case AuraType.ModDisarmOffhand:
                    if (apply)
                        flagChangeFunc = unit => { unit.SetUnitFlag2(UnitFlags2.DisarmOffhand); };
                    else
                        flagChangeFunc = unit => { unit.RemoveUnitFlag2(UnitFlags2.DisarmOffhand); };

                    slot = EquipmentSlot.OffHand;
                    attType = WeaponAttackType.OffAttack;

                    break;
                case AuraType.ModDisarmRanged:
                    if (apply)
                        flagChangeFunc = unit => { unit.SetUnitFlag2(UnitFlags2.DisarmRanged); };
                    else
                        flagChangeFunc = unit => { unit.RemoveUnitFlag2(UnitFlags2.DisarmRanged); };

                    slot = EquipmentSlot.MainHand;
                    attType = WeaponAttackType.RangedAttack;

                    break;
                default:
                    return;
            }

            // set/remove flag before weapon bonuses so it's properly reflected in CanUseAttackType
            flagChangeFunc?.Invoke(target);

            // Handle Damage modification, shapeshifted druids are not affected
            if (target.IsTypeId(TypeId.Player) &&
                !target.IsInFeralForm())
            {
                Player player = target.ToPlayer();

                Item item = player.GetItemByPos(InventorySlots.Bag0, slot);

                if (item != null)
                {
                    WeaponAttackType attackType = Player.GetAttackBySlot(slot, item.GetTemplate().GetInventoryType());

                    player.ApplyItemDependentAuras(item, !apply);

                    if (attackType < WeaponAttackType.Max)
                    {
                        player._ApplyWeaponDamage(slot, item, !apply);

                        if (!apply) // apply case already handled on Item dependent aura removal (if any)
                            player.UpdateWeaponDependentAuras(attackType);
                    }
                }
            }

            if (target.IsTypeId(TypeId.Unit) &&
                target.ToCreature().GetCurrentEquipmentId() != 0)
                target.UpdateDamagePhysical(attType);
        }

        [AuraEffectHandler(AuraType.ModSilence)]
        private void HandleAuraModSilence(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                target.SetUnitFlag(UnitFlags.Silenced);

                // call functions which may have additional effects after changing State of unit
                // Stop cast only spells vs PreventionType & SPELL_PREVENTION_TYPE_SILENCE
                for (var i = CurrentSpellTypes.Melee; i < CurrentSpellTypes.Max; ++i)
                {
                    Spell spell = target.GetCurrentSpell(i);

                    if (spell != null)
                        if (spell._spellInfo.PreventionType.HasAnyFlag(SpellPreventionType.Silence))
                            // Stop spells on prepare or casting State
                            target.InterruptSpell(i, false);
                }
            }
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(AuraType.ModSilence) ||
                    target.HasAuraType(AuraType.ModPacifySilence))
                    return;

                target.RemoveUnitFlag(UnitFlags.Silenced);
            }
        }

        [AuraEffectHandler(AuraType.ModPacify)]
        private void HandleAuraModPacify(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                target.SetUnitFlag(UnitFlags.Pacified);
            }
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(AuraType.ModPacify) ||
                    target.HasAuraType(AuraType.ModPacifySilence))
                    return;

                target.RemoveUnitFlag(UnitFlags.Pacified);
            }
        }

        [AuraEffectHandler(AuraType.ModPacifySilence)]
        private void HandleAuraModPacifyAndSilence(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            // Vengeance of the Blue Flight (@todo REMOVE THIS!)
            // @workaround
            if (_spellInfo.Id == 45839)
            {
                if (apply)
                    target.SetUnitFlag(UnitFlags.NonAttackable);
                else
                    target.RemoveUnitFlag(UnitFlags.NonAttackable);
            }

            if (!apply)
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(AuraType.ModPacifySilence))
                    return;

            HandleAuraModPacify(aurApp, mode, apply);
            HandleAuraModSilence(aurApp, mode, apply);
        }

        [AuraEffectHandler(AuraType.ModNoActions)]
        private void HandleAuraModNoActions(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                target.SetUnitFlag2(UnitFlags2.NoActions);

                // call functions which may have additional effects after changing State of unit
                // Stop cast only spells vs PreventionType & SPELL_PREVENTION_TYPE_SILENCE
                for (var i = CurrentSpellTypes.Melee; i < CurrentSpellTypes.Max; ++i)
                {
                    Spell spell = target.GetCurrentSpell(i);

                    if (spell)
                        if (spell._spellInfo.PreventionType.HasAnyFlag(SpellPreventionType.NoActions))
                            // Stop spells on prepare or casting State
                            target.InterruptSpell(i, false);
                }
            }
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(AuraType.ModNoActions))
                    return;

                target.RemoveUnitFlag2(UnitFlags2.NoActions);
            }
        }

        /****************************/
        /***      TRACKING        ***/
        /****************************/
        [AuraEffectHandler(AuraType.TrackCreatures)]
        private void HandleAuraTrackCreatures(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Player target = aurApp.GetTarget().ToPlayer();

            if (target == null)
                return;

            if (apply)
                target.SetTrackCreatureFlag(1u << GetMiscValue() - 1);
            else
                target.RemoveTrackCreatureFlag(1u << GetMiscValue() - 1);
        }

        [AuraEffectHandler(AuraType.TrackStealthed)]
        private void HandleAuraTrackStealthed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Player target = aurApp.GetTarget().ToPlayer();

            if (target == null)
                return;

            if (!apply)
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;

            if (apply)
                target.SetPlayerLocalFlag(PlayerLocalFlags.TrackStealthed);
            else
                target.RemovePlayerLocalFlag(PlayerLocalFlags.TrackStealthed);
        }

        [AuraEffectHandler(AuraType.ModStalked)]
        private void HandleAuraModStalked(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            // used by spells: Hunter's Mark, Mind Vision, Syndicate Tracker (MURP) DND
            if (apply)
            {
                target.SetDynamicFlag(UnitDynFlags.TrackUnit);
            }
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (!target.HasAuraType(GetAuraType()))
                    target.RemoveDynamicFlag(UnitDynFlags.TrackUnit);
            }

            // call functions which may have additional effects after changing State of unit
            if (target.IsInWorld)
                target.UpdateObjectVisibility();
        }

        [AuraEffectHandler(AuraType.Untrackable)]
        private void HandleAuraUntrackable(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Player target = aurApp.GetTarget().ToPlayer();

            if (target == null)
                return;

            if (apply)
            {
                target.SetVisFlag(UnitVisFlags.Untrackable);
            }
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;

                target.RemoveVisFlag(UnitVisFlags.Untrackable);
            }
        }

    }
}