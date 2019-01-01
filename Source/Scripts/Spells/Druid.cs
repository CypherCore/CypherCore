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
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Spells.Druid
{
    struct SpellIds
    {
        public const uint BalanceT10Bonus = 70718;
        public const uint BalanceT10BonusProc = 70721;
        public const uint BlessingOfTheClaw = 28750;
        public const uint BlessingOfTheRemulos = 40445;
        public const uint BlessingOfTheElune = 40446;
        public const uint BlessingOfTheCenarius = 40452;
        public const uint Exhilarate = 28742;
        public const uint FeralChargeBear = 16979;
        public const uint FeralChargeCat = 49376;
        public const uint FormAquatic = 1066;
        public const uint FormFlight = 33943;
        public const uint FormStag = 165961;
        public const uint FormSwiftFlight = 40120;
        public const uint FormsTrinketBear = 37340;
        public const uint FormsTrinketCat = 37341;
        public const uint FormsTrinketMoonkin = 37343;
        public const uint FormsTrinketNone = 37344;
        public const uint FormsTrinketTree = 37342;
        public const uint IdolOfFeralShadows = 34241;
        public const uint IdolOfWorship = 60774;
        public const uint Infusion = 37238;
        public const uint Languish = 71023;
        public const uint LifebloomEnergize = 64372;
        public const uint LifebloomFinalHeal = 33778;
        public const uint LivingSeedHeal = 48503;
        public const uint LivingSeedProc = 48504;
        public const uint MoonfireDamage = 164812;
        public const uint RejuvenationT10Proc = 70691;
        public const uint SavageRoar = 62071;
        public const uint StampedeBearRank1 = 81016;
        public const uint StampedeCatRank1 = 81021;
        public const uint StampedeCatState = 109881;
        public const uint SunfireDamage = 164815;
        public const uint SurvivalInstincts = 50322;
        public const uint TravelForm = 783;
    }

    [Script] // 1850 - Dash
    public class spell_dru_dash : AuraScript
    {
        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            // do not set speed if not in cat form
            if (GetUnitOwner().GetShapeshiftForm() != ShapeShiftForm.CatForm)
                amount = 0;
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.ModIncreaseSpeed));
        }
    }

    [Script] // 33943 - Flight Form
    class spell_dru_flight_form : SpellScript
    {
        SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();
            if (caster.IsInDisallowedMountForm())
                return SpellCastResult.NotShapeshift;

            return SpellCastResult.SpellCastOk;
        }

        public override void Register()
        {
            OnCheckCast.Add(new CheckCastHandler(CheckCast));
        }
    }

    [Script] // 37336 - Druid Forms Trinket
    class spell_dru_forms_trinket : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FormsTrinketBear, SpellIds.FormsTrinketCat, SpellIds.FormsTrinketMoonkin, SpellIds.FormsTrinketNone, SpellIds.FormsTrinketTree);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            Unit target = eventInfo.GetActor();

            switch (target.GetShapeshiftForm())
            {
                case ShapeShiftForm.BearForm:
                case ShapeShiftForm.DireBearForm:
                case ShapeShiftForm.CatForm:
                case ShapeShiftForm.MoonkinForm:
                case ShapeShiftForm.None:
                case ShapeShiftForm.TreeOfLife:
                    return true;
                default:
                    break;
            }

            return false;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Unit target = eventInfo.GetActor();
            uint triggerspell = 0;

            switch (target.GetShapeshiftForm())
            {
                case ShapeShiftForm.BearForm:
                case ShapeShiftForm.DireBearForm:
                    triggerspell = SpellIds.FormsTrinketBear;
                    break;
                case ShapeShiftForm.CatForm:
                    triggerspell = SpellIds.FormsTrinketCat;
                    break;
                case ShapeShiftForm.MoonkinForm:
                    triggerspell = SpellIds.FormsTrinketMoonkin;
                    break;
                case ShapeShiftForm.None:
                    triggerspell = SpellIds.FormsTrinketNone;
                    break;
                case ShapeShiftForm.TreeOfLife:
                    triggerspell = SpellIds.FormsTrinketTree;
                    break;
                default:
                    return;
            }

            target.CastSpell(target, triggerspell, true, null, aurEff);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    // 34246 - Idol of the Emerald Queen
    [Script] // 60779 - Idol of Lush Moss
    class spell_dru_idol_lifebloom : AuraScript
    {
        void HandleEffectCalcSpellMod(AuraEffect aurEff, ref SpellModifier spellMod)
        {
            if (spellMod == null)
            {
                spellMod = new SpellModifier(GetAura());
                spellMod.op = SpellModOp.Dot;
                spellMod.type = SpellModType.Flat;
                spellMod.spellId = GetId();
                spellMod.mask = GetSpellInfo().GetEffect(aurEff.GetEffIndex()).SpellClassMask;
            }
            spellMod.value = aurEff.GetAmount() / 7;
        }

        public override void Register()
        {
            DoEffectCalcSpellMod.Add(new EffectCalcSpellModHandler(HandleEffectCalcSpellMod, 0, AuraType.Dummy));
        }
    }

    // 29166 - Innervate
    [Script]
    class spell_dru_innervate : AuraScript
    {
        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit caster = GetCaster();
            if (caster)
                amount = MathFunctions.CalculatePct(caster.GetCreatePowers(PowerType.Mana), amount) / aurEff.GetTotalTicks();
            else
                amount = 0;
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.PeriodicEnergize));
        }
    }

    // 33763 - Lifebloom
    [Script]
    class spell_dru_lifebloom : AuraScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.LifebloomFinalHeal, SpellIds.LifebloomEnergize);
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // Final heal only on duration end
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
                return;

            // final heal
            uint stack = GetStackAmount();
            uint healAmount = (uint)aurEff.GetAmount();
            Unit caster = GetCaster();
            if (caster != null)
            {
                healAmount = caster.SpellHealingBonusDone(GetTarget(), GetSpellInfo(), healAmount, DamageEffectType.Heal, aurEff.GetSpellEffectInfo(), stack);
                healAmount = GetTarget().SpellHealingBonusTaken(caster, GetSpellInfo(), healAmount, DamageEffectType.Heal, aurEff.GetSpellEffectInfo(), stack);

                GetTarget().CastCustomSpell(GetTarget(), SpellIds.LifebloomFinalHeal, (int)healAmount, 0, 0, true, null, aurEff, GetCasterGUID());

                // restore mana
                var costs = GetSpellInfo().CalcPowerCost(caster, GetSpellInfo().GetSchoolMask());
                var m = costs.Find(cost => cost.Power == PowerType.Mana);
                if (m != null)
                {
                    int returnMana = m.Amount * (int)stack / 2;
                    caster.CastCustomSpell(caster, SpellIds.LifebloomEnergize, returnMana, 0, 0, true, null, aurEff, GetCasterGUID());
                }
                return;
            }

            GetTarget().CastCustomSpell(GetTarget(), SpellIds.LifebloomFinalHeal, (int)healAmount, 0, 0, true, null, aurEff, GetCasterGUID());
        }

        void HandleDispel(DispelInfo dispelInfo)
        {
            Unit target = GetUnitOwner();
            if (target != null)
            {
                AuraEffect aurEff = GetEffect(1);
                if (aurEff != null)
                {
                    // final heal
                    uint healAmount = (uint)aurEff.GetAmount();
                    Unit caster = GetCaster();
                    if (caster != null)
                    {
                        healAmount = caster.SpellHealingBonusDone(target, GetSpellInfo(), healAmount, DamageEffectType.Heal, aurEff.GetSpellEffectInfo(), dispelInfo.GetRemovedCharges());
                        healAmount = target.SpellHealingBonusTaken(caster, GetSpellInfo(), healAmount, DamageEffectType.Heal, aurEff.GetSpellEffectInfo(), dispelInfo.GetRemovedCharges());
                        target.CastCustomSpell(target, SpellIds.LifebloomFinalHeal, (int)healAmount, 0, 0, true, null, null, GetCasterGUID());

                        // restore mana
                        var costs = GetSpellInfo().CalcPowerCost(caster, GetSpellInfo().GetSchoolMask());
                        var m = costs.Find(cost => cost.Power == PowerType.Mana);
                        if (m != null)
                        {
                            int returnMana = m.Amount * dispelInfo.GetRemovedCharges() / 2;
                            caster.CastCustomSpell(caster, SpellIds.LifebloomEnergize, returnMana, 0, 0, true, null, null, GetCasterGUID());
                        }
                        return;
                    }

                    target.CastCustomSpell(target, SpellIds.LifebloomFinalHeal, (int)healAmount, 0, 0, true, null, null, GetCasterGUID());
                }
            }
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(AfterRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
            AfterDispel.Add(new AuraDispelHandler(HandleDispel));
        }
    }

    // 48496 - Living Seed
    [Script]
    class spell_dru_living_seed : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LivingSeedProc);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            int amount = (int)MathFunctions.CalculatePct(eventInfo.GetHealInfo().GetHeal(), aurEff.GetAmount());
            GetTarget().CastCustomSpell(SpellIds.LivingSeedProc, SpellValueMod.BasePoint0, amount, eventInfo.GetProcTarget(), true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    // 48504 - Living Seed (Proc)
    [Script]
    class spell_dru_living_seed_proc : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LivingSeedHeal);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastCustomSpell(SpellIds.LivingSeedHeal, SpellValueMod.BasePoint0, aurEff.GetAmount(), GetTarget(), true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] //  8921 - Moonfire
    class spell_dru_moonfire : SpellScript
    {
        void HandleOnHit(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.MoonfireDamage, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.Dummy));
        }
    }

    // 16972 - Predatory Strikes
    [Script]
    class spell_dru_predatory_strikes : AuraScript
    {
        void UpdateAmount(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Player target = GetTarget().ToPlayer();
            if (target != null)
                target.UpdateAttackPowerAndDamage();
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(UpdateAmount, SpellConst.EffectAll, AuraType.Dummy, AuraEffectHandleModes.ChangeAmountMask));
            AfterEffectRemove.Add(new EffectApplyHandler(UpdateAmount, SpellConst.EffectAll, AuraType.Dummy, AuraEffectHandleModes.ChangeAmountMask));
        }
    }

    // 1079 - Rip
    [Script]
    class spell_dru_rip : AuraScript
    {
        public override bool Load()
        {
            Unit caster = GetCaster();
            return caster != null && caster.IsTypeId(TypeId.Player);
        }

        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            canBeRecalculated = false;

            Unit caster = GetCaster();
            if (caster != null)
            {
                // 0.01 * $AP * cp
                byte cp = caster.ToPlayer().GetComboPoints();

                // Idol of Feral Shadows. Can't be handled as SpellMod due its dependency from CPs
                AuraEffect idol = caster.GetAuraEffect(SpellIds.IdolOfFeralShadows, 0);
                if (idol != null)
                    amount += cp * idol.GetAmount();
                // Idol of Worship. Can't be handled as SpellMod due its dependency from CPs
                else if ((idol = caster.GetAuraEffect(SpellIds.IdolOfWorship, 0)) != null)
                    amount += cp * idol.GetAmount();

                amount += (int)MathFunctions.CalculatePct(caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack), cp);
            }
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.PeriodicDamage));
        }
    }

    [Script] // 16864 - Omen of Clarity
    class spell_dru_omen_of_clarity : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BalanceT10Bonus, SpellIds.BalanceT10BonusProc);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Unit target = GetTarget();
            if (target.HasAura(SpellIds.BalanceT10Bonus))
                target.CastSpell((Unit)null, SpellIds.BalanceT10BonusProc, true, null);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    // 52610 - Savage Roar
    [Script]
    class spell_dru_savage_roar : SpellScript
    {
        SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();
            if (caster.GetShapeshiftForm() != ShapeShiftForm.CatForm)
                return SpellCastResult.OnlyShapeshift;

            return SpellCastResult.SpellCastOk;
        }

        public override void Register()
        {
            OnCheckCast.Add(new CheckCastHandler(CheckCast));
        }
    }

    [Script]
    class spell_dru_savage_roar_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SavageRoar);
        }

        void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.CastSpell(target, SpellIds.SavageRoar, true, null, aurEff, GetCasterGUID());
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(SpellIds.SavageRoar);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(AfterApply, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new EffectApplyHandler(AfterRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 78892 - Stampede
    class spell_dru_stampede : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.StampedeBearRank1, SpellIds.StampedeCatRank1, SpellIds.StampedeCatState, SpellIds.FeralChargeCat, SpellIds.FeralChargeBear);
        }

        void HandleEffectCatProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            if (GetTarget().GetShapeshiftForm() != ShapeShiftForm.CatForm || eventInfo.GetDamageInfo().GetSpellInfo().Id != SpellIds.FeralChargeCat)
                return;

            GetTarget().CastSpell(GetTarget(), Global.SpellMgr.GetSpellWithRank(SpellIds.StampedeCatRank1, GetSpellInfo().GetRank()), true, null, aurEff);
            GetTarget().CastSpell(GetTarget(), SpellIds.StampedeCatState, true, null, aurEff);
        }

        void HandleEffectBearProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            if (GetTarget().GetShapeshiftForm() != ShapeShiftForm.BearForm || eventInfo.GetDamageInfo().GetSpellInfo().Id != SpellIds.FeralChargeBear)
                return;

            GetTarget().CastSpell(GetTarget(), Global.SpellMgr.GetSpellWithRank(SpellIds.StampedeBearRank1, GetSpellInfo().GetRank()), true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleEffectCatProc, 0, AuraType.Dummy));
            OnEffectProc.Add(new EffectProcHandler(HandleEffectBearProc, 1, AuraType.Dummy));
        }
    }

    // 50286 - Starfall (Dummy)
    [Script]
    class spell_dru_starfall_dummy : SpellScript
    {
        void FilterTargets(List<WorldObject> targets)
        {
            targets.Resize(2);
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            // Shapeshifting into an animal form or mounting cancels the effect
            if (caster.GetCreatureType() == CreatureType.Beast || caster.IsMounted())
            {
                SpellInfo spellInfo = GetTriggeringSpell();
                if (spellInfo != null)
                    caster.RemoveAurasDueToSpell(spellInfo.Id);
                return;
            }

            // Any effect which causes you to lose control of your character will supress the starfall effect.
            if (caster.HasUnitState(UnitState.Controlled))
                return;

            caster.CastSpell(GetHitUnit(), (uint)GetEffectValue(), true);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] //  93402 - Sunfire
    class spell_dru_sunfire : SpellScript
    {
        void HandleOnHit(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.SunfireDamage, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.Dummy));
        }
    }

    // 61336 - Survival Instincts
    [Script]
    class spell_dru_survival_instincts : SpellScript
    {
        SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();
            if (!caster.IsInFeralForm())
                return SpellCastResult.OnlyShapeshift;

            return SpellCastResult.SpellCastOk;
        }

        public override void Register()
        {
            OnCheckCast.Add(new CheckCastHandler(CheckCast));
        }
    }

    [Script]
    class spell_dru_survival_instincts_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.SurvivalInstincts);
        }

        void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            int bp0 = (int)target.CountPctFromMaxHealth(aurEff.GetAmount());
            target.CastCustomSpell(target, SpellIds.SurvivalInstincts, bp0, 0, 0, true);
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(SpellIds.SurvivalInstincts);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(AfterApply, 0, AuraType.Dummy, AuraEffectHandleModes.ChangeAmountMask));
            AfterEffectRemove.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.ChangeAmountMask));
        }
    }

    // 40121 - Swift Flight Form (Passive)
    [Script]
    class spell_dru_swift_flight_passive : AuraScript
    {
        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Player caster = GetCaster().ToPlayer();
            if (caster != null)
                if (caster.GetSkillValue(SkillType.Riding) >= 375)
                    amount = 310;
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 1, AuraType.ModIncreaseVehicleFlightSpeed));
        }
    }

    [Script] // 28744 - Regrowth
    class spell_dru_t3_6p_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BlessingOfTheClaw);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            eventInfo.GetActor().CastSpell(eventInfo.GetProcTarget(), SpellIds.BlessingOfTheClaw, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.OverrideClassScripts));
        }
    }

    [Script] // 28719 - Healing Touch
    class spell_dru_t3_8p_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Exhilarate);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Spell spell = eventInfo.GetProcSpell();
            if (spell == null)
                return;

            Unit caster = eventInfo.GetActor();
            var costs = spell.GetPowerCost();
            var m = costs.First(cost => cost.Power == PowerType.Mana);
            if (m == null)
                return;

            int amount = MathFunctions.CalculatePct(m.Amount, aurEff.GetAmount());
            caster.CastCustomSpell(SpellIds.Exhilarate, SpellValueMod.BasePoint0, amount, null, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    // 37288 - Mana Restore
    [Script] // 37295 - Mana Restore
    class spell_dru_t4_2p_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Infusion);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            eventInfo.GetActor().CastSpell((Unit)null, SpellIds.Infusion, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 783 - Travel Form (dummy)
    class spell_dru_travel_form_dummy_SpellScript : SpellScript
    {
        SpellCastResult CheckCast()
        {
            Player player = GetCaster().ToPlayer();
            if (!player)
                return SpellCastResult.CustomError;

            if (player.GetSkillValue(SkillType.Riding) < 75)
                return SpellCastResult.ApprenticeRidingRequirement;

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(player.IsInWater() ? SpellIds.FormAquatic : SpellIds.FormStag);
            return spellInfo.CheckLocation(player.GetMapId(), player.GetZoneId(), player.GetAreaId(), player);
        }

        public override void Register()
        {
            OnCheckCast.Add(new CheckCastHandler(CheckCast));
        }
    }

    [Script]
    class spell_dru_travel_form_dummy_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FormStag, SpellIds.FormAquatic, SpellIds.FormFlight, SpellIds.FormSwiftFlight);
        }

        public override bool Load()
        {
            return GetCaster().GetTypeId() == TypeId.Player;
        }

        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            uint triggeredSpellId;

            Player player = GetTarget().ToPlayer();
            if (player.IsInWater()) // Aquatic form
                triggeredSpellId = SpellIds.FormAquatic;
            else if (player.GetSkillValue(SkillType.Riding) >= 225 && CheckLocationForForm(SpellIds.FormFlight) == SpellCastResult.SpellCastOk) // Flight form
                triggeredSpellId = player.getLevel() >= 71 ? SpellIds.FormSwiftFlight : SpellIds.FormFlight;
            else // Stag form (riding skill already checked in CheckCast)
                triggeredSpellId = SpellIds.FormStag;

            player.AddAura(triggeredSpellId, player);
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // No need to check remove mode, it's safe for auras to remove each other in AfterRemove hook.
            GetTarget().RemoveAura(SpellIds.FormStag);
            GetTarget().RemoveAura(SpellIds.FormAquatic);
            GetTarget().RemoveAura(SpellIds.FormFlight);
            GetTarget().RemoveAura(SpellIds.FormSwiftFlight);
        }

        public override void Register()
        {
            OnEffectApply.Add(new EffectApplyHandler(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }

        // Outdoor check already passed - Travel Form (dummy) has SPELL_ATTR0_OUTDOORS_ONLY attribute.
        SpellCastResult CheckLocationForForm(uint spell)
        {
            Player player = GetTarget().ToPlayer();
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spell);
            return spellInfo.CheckLocation(player.GetMapId(), player.GetZoneId(), player.GetAreaId(), player);
        }
    }

    // 1066 - Aquatic Form
    // 33943 - Flight Form
    // 40120 - Swift Flight Form
    [Script]  // 165961 - Stag Form
    class spell_dru_travel_form_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FormStag, SpellIds.FormAquatic, SpellIds.FormFlight, SpellIds.FormSwiftFlight);
        }

        public override bool Load()
        {
            return GetCaster().GetTypeId() == TypeId.Player;
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // If it stays 0, it removes Travel Form dummy in AfterRemove.
            triggeredSpellId = 0;

            // We should only handle aura interrupts.
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Interrupt)
                return;

            // Check what form is appropriate
            Player player = GetTarget().ToPlayer();
            if (player.IsInWater()) // Aquatic form
                triggeredSpellId = SpellIds.FormAquatic;
            else if (player.GetSkillValue(SkillType.Riding) >= 225 && CheckLocationForForm(SpellIds.FormFlight) == SpellCastResult.SpellCastOk) // Flight form
                triggeredSpellId = player.GetSkillValue(SkillType.Riding) >= 300 ? SpellIds.FormSwiftFlight : SpellIds.FormFlight;
            else if (CheckLocationForForm(SpellIds.FormStag) == SpellCastResult.SpellCastOk) // Stag form
                triggeredSpellId = SpellIds.FormStag;

            // If chosen form is current aura, just don't remove it.
            if (triggeredSpellId == m_scriptSpellId)
                PreventDefaultAction();
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Player player = GetTarget().ToPlayer();

            if (triggeredSpellId != 0) // Apply new form
                player.AddAura(triggeredSpellId, player);
            else // If not set, simply remove Travel Form dummy
                player.RemoveAura(SpellIds.TravelForm);
        }

        public override void Register()
        {
            OnEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.ModShapeshift, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.ModShapeshift, AuraEffectHandleModes.Real));
        }

        SpellCastResult CheckLocationForForm(uint spell_id)
        {
            Player player = GetTarget().ToPlayer();
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spell_id);

            if (!player.GetMap().IsOutdoors(player.GetPhaseShift(), player.GetPositionX(), player.GetPositionY(), player.GetPositionZ()))
                return SpellCastResult.OnlyOutdoors;

            return spellInfo.CheckLocation(player.GetMapId(), player.GetZoneId(), player.GetAreaId(), player);
        }

        uint triggeredSpellId;
    }

    [Script] // 40442 - Druid Tier 6 Trinket
    class spell_dru_item_t6_trinket : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BlessingOfTheRemulos, SpellIds.BlessingOfTheElune, SpellIds.BlessingOfTheCenarius);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            SpellInfo spellInfo = eventInfo.GetSpellInfo();
            if (spellInfo == null)
                return;

            uint spellId;
            int chance;

            // Starfire
            if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00000004u))
            {
                spellId = SpellIds.BlessingOfTheRemulos;
                chance = 25;
            }
            // Rejuvenation
            else if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00000010u))
            {
                spellId = SpellIds.BlessingOfTheElune;
                chance = 25;
            }
            // Mangle (Bear) and Mangle (Cat)
            else if (spellInfo.SpellFamilyFlags[1].HasAnyFlag(0x00000440u))
            {
                spellId = SpellIds.BlessingOfTheCenarius;
                chance = 40;
            }
            else
                return;

            if (RandomHelper.randChance(chance))
                eventInfo.GetActor().CastSpell((Unit)null, spellId, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 70723 - Item - Druid T10 Balance 4P Bonus
    class spell_dru_t10_balance_4p_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Languish);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo == null || damageInfo.GetDamage() == 0)
                return;

            Unit caster = eventInfo.GetActor();
            Unit target = eventInfo.GetProcTarget();

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.Languish);
            int amount = (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount());
            amount /= (int)spellInfo.GetMaxTicks(Difficulty.None);
            // Add remaining ticks to damage done
            amount += (int)target.GetRemainingPeriodicAmount(caster.GetGUID(), SpellIds.Languish, AuraType.PeriodicDamage);

            caster.CastCustomSpell(SpellIds.Languish, SpellValueMod.BasePoint0, amount, target, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    // 70691 - Item T10 Restoration 4P Bonus
    [Script]
    class spell_dru_t10_restoration_4p_bonus : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        void FilterTargets(List<WorldObject> targets)
        {
            if (GetCaster().ToPlayer().GetGroup() == null)
            {
                targets.Clear();
                targets.Add(GetCaster());
            }
            else
            {
                targets.Remove(GetExplTargetUnit());
                List<Unit> tempTargets = new List<Unit>();
                foreach (var obj in targets)
                    if (obj.IsTypeId(TypeId.Player) && GetCaster().IsInRaidWith(obj.ToUnit()))
                        tempTargets.Add(obj.ToUnit());

                if (tempTargets.Empty())
                {
                    targets.Clear();
                    FinishCast(SpellCastResult.DontReport);
                    return;
                }

                Unit target = tempTargets.SelectRandom();
                targets.Clear();
                targets.Add(target);
            }
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaAlly));
        }
    }

    [Script] // 70664 - Druid T10 Restoration 4P Bonus (Rejuvenation)
    class spell_dru_t10_restoration_4p_bonus_dummy : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RejuvenationT10Proc);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            SpellInfo spellInfo = eventInfo.GetSpellInfo();
            if (spellInfo == null || spellInfo.Id == SpellIds.RejuvenationT10Proc)
                return false;

            HealInfo healInfo = eventInfo.GetHealInfo();
            if (healInfo == null || healInfo.GetHeal() == 0)
                return false;

            Player caster = eventInfo.GetActor().ToPlayer();
            if (!caster)
                return false;

            return caster.GetGroup() || caster != eventInfo.GetProcTarget();
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            int amount = (int)eventInfo.GetHealInfo().GetHeal();
            eventInfo.GetActor().CastCustomSpell(SpellIds.RejuvenationT10Proc, SpellValueMod.BasePoint0, amount, null, true, null, aurEff);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 48438 - Wild Growth
    class spell_dru_wild_growth : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            SpellEffectInfo effect2 = spellInfo.GetEffect(2);
            if (effect2 == null || effect2.IsEffect() || effect2.CalcValue() <= 0)
                return false;
            return true;
        }

        void FilterTargets(List<WorldObject> targets)
        {
            targets.RemoveAll(obj =>
            {
                Unit target = obj.ToUnit();
                if (target)
                    return !GetCaster().IsInRaidWith(target);

                return true;
            });

            int maxTargets = GetSpellInfo().GetEffect(2).CalcValue(GetCaster());

            if (targets.Count > maxTargets)
            {
                targets.Sort(new HealthPctOrderPred());
                targets.RemoveRange(maxTargets, targets.Count - maxTargets);
            }

            _targets = targets;
        }

        void SetTargets(List<WorldObject> targets)
        {
            targets.Clear();
            targets.AddRange(_targets);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaAlly));
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(SetTargets, 1, Targets.UnitDestAreaAlly));
        }

        List<WorldObject> _targets;
    }
}
