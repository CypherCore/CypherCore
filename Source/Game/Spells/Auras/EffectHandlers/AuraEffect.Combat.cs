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
        /********************************/
        /***          FIGHT           ***/
        /********************************/
        [AuraEffectHandler(AuraType.ModParryPercent)]
        private void HandleAuraModParryPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            target.ToPlayer().UpdateParryPercentage();
        }

        [AuraEffectHandler(AuraType.ModDodgePercent)]
        private void HandleAuraModDodgePercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            target.ToPlayer().UpdateDodgePercentage();
        }

        [AuraEffectHandler(AuraType.ModBlockPercent)]
        private void HandleAuraModBlockPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            target.ToPlayer().UpdateBlockPercentage();
        }

        [AuraEffectHandler(AuraType.InterruptRegen)]
        private void HandleAuraModRegenInterrupt(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsPlayer())
                return;

            target.ToPlayer().UpdateManaRegen();
        }

        [AuraEffectHandler(AuraType.ModWeaponCritPercent)]
        private void HandleAuraModWeaponCritPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Player target = aurApp.GetTarget().ToPlayer();

            if (!target)
                return;

            target.UpdateAllWeaponDependentCritAuras();
        }

        [AuraEffectHandler(AuraType.ModSpellHitChance)]
        private void HandleModSpellHitChance(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if (target.IsTypeId(TypeId.Player))
                target.ToPlayer().UpdateSpellHitChances();
            else
                target.ModSpellHitChance += apply ? GetAmount() : -GetAmount();
        }

        [AuraEffectHandler(AuraType.ModSpellCritChance)]
        private void HandleModSpellCritChance(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if (target.IsTypeId(TypeId.Player))
                target.ToPlayer().UpdateSpellCritChance();
            else
                target.BaseSpellCritChance += apply ? GetAmount() : -GetAmount();
        }

        [AuraEffectHandler(AuraType.ModCritPct)]
        private void HandleAuraModCritPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
            {
                target.BaseSpellCritChance += apply ? GetAmount() : -GetAmount();

                return;
            }

            target.ToPlayer().UpdateAllWeaponDependentCritAuras();

            // included in Player.UpdateSpellCritChance calculation
            target.ToPlayer().UpdateSpellCritChance();
        }

        /********************************/
        /***         ATTACK SPEED     ***/
        /********************************/
        [AuraEffectHandler(AuraType.HasteSpells)]
        [AuraEffectHandler(AuraType.ModCastingSpeedNotStack)]
        private void HandleModCastingSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            // Do not apply such Auras in normal way
            if (GetAmount() >= 1000)
            {
                if (apply)
                {
                    target.SetInstantCast(true);
                }
                else
                {
                    // only SPELL_AURA_MOD_CASTING_SPEED_NOT_STACK can have this high amount
                    // it's some rare case that you have 2 Auras like that, but just in case ;)

                    bool remove = true;
                    var castingSpeedNotStack = target.GetAuraEffectsByType(AuraType.ModCastingSpeedNotStack);

                    foreach (AuraEffect aurEff in castingSpeedNotStack)
                        if (aurEff != this &&
                            aurEff.GetAmount() >= 1000)
                        {
                            remove = false;

                            break;
                        }

                    if (remove)
                        target.SetInstantCast(false);
                }

                return;
            }

            int spellGroupVal = target.GetHighestExclusiveSameEffectSpellGroupValue(this, GetAuraType());

            if (Math.Abs(spellGroupVal) >= Math.Abs(GetAmount()))
                return;

            if (spellGroupVal != 0)
                target.ApplyCastTimePercentMod(spellGroupVal, !apply);

            target.ApplyCastTimePercentMod(GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModMeleeRangedHaste)]
        [AuraEffectHandler(AuraType.ModMeleeRangedHaste2)]
        private void HandleModMeleeRangedSpeedPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            //! ToDo: Haste Auras with the same handler _CAN'T_ stack together
            Unit target = aurApp.GetTarget();

            target.ApplyAttackTimePercentMod(WeaponAttackType.BaseAttack, GetAmount(), apply);
            target.ApplyAttackTimePercentMod(WeaponAttackType.OffAttack, GetAmount(), apply);
            target.ApplyAttackTimePercentMod(WeaponAttackType.RangedAttack, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.MeleeSlow)]
        [AuraEffectHandler(AuraType.ModSpeedSlowAll)]
        private void HandleModCombatSpeedPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();
            int spellGroupVal = target.GetHighestExclusiveSameEffectSpellGroupValue(this, AuraType.MeleeSlow);

            if (Math.Abs(spellGroupVal) >= Math.Abs(GetAmount()))
                return;

            if (spellGroupVal != 0)
            {
                target.ApplyCastTimePercentMod(spellGroupVal, !apply);
                target.ApplyAttackTimePercentMod(WeaponAttackType.BaseAttack, spellGroupVal, !apply);
                target.ApplyAttackTimePercentMod(WeaponAttackType.OffAttack, spellGroupVal, !apply);
                target.ApplyAttackTimePercentMod(WeaponAttackType.RangedAttack, spellGroupVal, !apply);
            }

            target.ApplyCastTimePercentMod(GetAmount(), apply);
            target.ApplyAttackTimePercentMod(WeaponAttackType.BaseAttack, GetAmount(), apply);
            target.ApplyAttackTimePercentMod(WeaponAttackType.OffAttack, GetAmount(), apply);
            target.ApplyAttackTimePercentMod(WeaponAttackType.RangedAttack, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModAttackspeed)]
        private void HandleModAttackSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            target.ApplyAttackTimePercentMod(WeaponAttackType.BaseAttack, GetAmount(), apply);
            target.UpdateDamagePhysical(WeaponAttackType.BaseAttack);
        }

        [AuraEffectHandler(AuraType.ModMeleeHaste)]
        [AuraEffectHandler(AuraType.ModMeleeHaste2)]
        [AuraEffectHandler(AuraType.ModMeleeHaste3)]
        private void HandleModMeleeSpeedPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            //! ToDo: Haste Auras with the same handler _CAN'T_ stack together
            Unit target = aurApp.GetTarget();
            int spellGroupVal = target.GetHighestExclusiveSameEffectSpellGroupValue(this, AuraType.ModMeleeHaste);

            if (Math.Abs(spellGroupVal) >= Math.Abs(GetAmount()))
                return;

            if (spellGroupVal != 0)
            {
                target.ApplyAttackTimePercentMod(WeaponAttackType.BaseAttack, spellGroupVal, !apply);
                target.ApplyAttackTimePercentMod(WeaponAttackType.OffAttack, spellGroupVal, !apply);
            }

            target.ApplyAttackTimePercentMod(WeaponAttackType.BaseAttack, GetAmount(), apply);
            target.ApplyAttackTimePercentMod(WeaponAttackType.OffAttack, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModRangedHaste)]
        private void HandleAuraModRangedHaste(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            //! ToDo: Haste Auras with the same handler _CAN'T_ stack together
            Unit target = aurApp.GetTarget();

            target.ApplyAttackTimePercentMod(WeaponAttackType.RangedAttack, GetAmount(), apply);
        }

        /********************************/
        /***       COMBAT RATING      ***/
        /********************************/
        [AuraEffectHandler(AuraType.ModRating)]
        private void HandleModRating(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            for (int rating = 0; rating < (int)CombatRating.Max; ++rating)
                if (Convert.ToBoolean(GetMiscValue() & 1 << rating))
                    target.ToPlayer().ApplyRatingMod((CombatRating)rating, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModRatingPct)]
        private void HandleModRatingPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            // Just recalculate ratings
            for (int rating = 0; rating < (int)CombatRating.Max; ++rating)
                if (Convert.ToBoolean(GetMiscValue() & 1 << rating))
                    target.ToPlayer().UpdateRating((CombatRating)rating);
        }

        /********************************/
        /***        ATTACK POWER      ***/
        /********************************/
        [AuraEffectHandler(AuraType.ModAttackPower)]
        private void HandleAuraModAttackPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            target.HandleStatFlatModifier(UnitMods.AttackPower, UnitModifierFlatType.Total, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModRangedAttackPower)]
        private void HandleAuraModRangedAttackPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if ((target.GetClassMask() & (uint)Class.ClassMaskWandUsers) != 0)
                return;

            target.HandleStatFlatModifier(UnitMods.AttackPowerRanged, UnitModifierFlatType.Total, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModAttackPowerPct)]
        private void HandleAuraModAttackPowerPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            //UNIT_FIELD_ATTACK_POWER_MULTIPLIER = Multiplier - 1
            if (apply)
            {
                target.ApplyStatPctModifier(UnitMods.AttackPower, UnitModifierPctType.Total, GetAmount());
            }
            else
            {
                float amount = target.GetTotalAuraMultiplier(AuraType.ModAttackPowerPct);
                target.SetStatPctModifier(UnitMods.AttackPower, UnitModifierPctType.Total, amount);
            }
        }

        [AuraEffectHandler(AuraType.ModRangedAttackPowerPct)]
        private void HandleAuraModRangedAttackPowerPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if ((target.GetClassMask() & (uint)Class.ClassMaskWandUsers) != 0)
                return;

            //UNIT_FIELD_RANGED_ATTACK_POWER_MULTIPLIER = Multiplier - 1
            if (apply)
            {
                target.ApplyStatPctModifier(UnitMods.AttackPowerRanged, UnitModifierPctType.Total, GetAmount());
            }
            else
            {
                float amount = target.GetTotalAuraMultiplier(AuraType.ModRangedAttackPowerPct);
                target.SetStatPctModifier(UnitMods.AttackPowerRanged, UnitModifierPctType.Total, amount);
            }
        }

        /********************************/
        /***        DAMAGE BONUS      ***/
        /********************************/
        [AuraEffectHandler(AuraType.ModDamageDone)]
        private void HandleModDamageDone(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if ((GetMiscValue() & (int)SpellSchoolMask.Normal) != 0)
                target.UpdateAllDamageDoneMods();

            // Magic Damage modifiers implemented in Unit::SpellBaseDamageBonusDone
            // This information for client side use only
            Player playerTarget = target.ToPlayer();

            if (playerTarget != null)
            {
                for (int i = 0; i < (int)SpellSchools.Max; ++i)
                    if (Convert.ToBoolean(GetMiscValue() & 1 << i))
                    {
                        if (GetAmount() >= 0)
                            playerTarget.ApplyModDamageDonePos((SpellSchools)i, GetAmount(), apply);
                        else
                            playerTarget.ApplyModDamageDoneNeg((SpellSchools)i, GetAmount(), apply);
                    }

                Guardian pet = playerTarget.GetGuardianPet();

                if (pet)
                    pet.UpdateAttackPowerAndDamage();
            }
        }

        [AuraEffectHandler(AuraType.ModDamagePercentDone)]
        private void HandleModDamagePercentDone(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            // also handles spell group stacks
            if (Convert.ToBoolean(GetMiscValue() & (int)SpellSchoolMask.Normal))
                target.UpdateAllDamagePctDoneMods();

            Player thisPlayer = target.ToPlayer();

            if (thisPlayer != null)
                for (var i = SpellSchools.Normal; i < SpellSchools.Max; ++i)
                    if (Convert.ToBoolean(GetMiscValue() & 1 << (int)i))
                    {
                        // only aura Type modifying PLAYER_FIELD_MOD_DAMAGE_DONE_PCT
                        float amount = thisPlayer.GetTotalAuraMultiplierByMiscMask(AuraType.ModDamagePercentDone, 1u << (int)i);
                        thisPlayer.SetModDamageDonePercent(i, amount);
                    }
        }

        [AuraEffectHandler(AuraType.ModOffhandDamagePct)]
        private void HandleModOffhandDamagePercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            // also handles spell group stacks
            target.UpdateDamagePctDoneMods(WeaponAttackType.OffAttack);
        }

        private void HandleShieldBlockValue(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Player player = aurApp.GetTarget().ToPlayer();

            player?.HandleBaseModFlatValue(BaseModGroup.ShieldBlockValue, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModShieldBlockvaluePct)]
        private void HandleShieldBlockValuePercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Player target = aurApp.GetTarget().ToPlayer();

            if (!target)
                return;

            if (apply)
            {
                target.ApplyBaseModPctValue(BaseModGroup.ShieldBlockValue, GetAmount());
            }
            else
            {
                float amount = target.GetTotalAuraMultiplier(AuraType.ModShieldBlockvaluePct);
                target.SetBaseModPctValue(BaseModGroup.ShieldBlockValue, amount);
            }
        }

        /********************************/
        /***        POWER COST        ***/
        /********************************/
        [AuraEffectHandler(AuraType.ModPowerCostSchool)]
        private void HandleModPowerCost(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            // handled in SpellInfo::CalcPowerCost, this is only for client UI
            if ((GetMiscValueB() & 1 << (int)PowerType.Mana) == 0)
                return;

            Unit target = aurApp.GetTarget();

            for (int i = 0; i < (int)SpellSchools.Max; ++i)
                if (Convert.ToBoolean(GetMiscValue() & 1 << i))
                    target.ApplyModManaCostModifier((SpellSchools)i, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ArenaPreparation)]
        private void HandleArenaPreparation(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                target.SetUnitFlag(UnitFlags.Preparation);
            }
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;

                target.RemoveUnitFlag(UnitFlags.Preparation);
            }

            target.ModifyAuraState(AuraStateType.ArenaPreparation, apply);
        }

        [AuraEffectHandler(AuraType.NoReagentUse)]
        private void HandleNoReagentUseAura(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            FlagArray128 mask = new();
            var noReagent = target.GetAuraEffectsByType(AuraType.NoReagentUse);

            foreach (var eff in noReagent)
            {
                SpellEffectInfo effect = eff.GetSpellEffectInfo();

                if (effect != null)
                    mask |= effect.SpellClassMask;
            }

            target.ToPlayer().SetNoRegentCostMask(mask);
        }

    }
}