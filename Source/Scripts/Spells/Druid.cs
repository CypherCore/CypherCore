// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.DataStorage;
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
        public const uint BearForm = 5487;
        public const uint BlessingOfCenarius = 40452;
        public const uint BlessingOfElune = 40446;
        public const uint BlessingOfRemulos = 40445;
        public const uint BlessingOfTheClaw = 28750;
        public const uint BloodFrenzyAura = 203962;
        public const uint BloodFrenzyRageGain = 203961;
        public const uint BramblesDamageAura = 213709;
        public const uint BramblesPassive = 203953;
        public const uint BramblesRelect = 203958;
        public const uint BristlingFurGainRage = 204031;
        public const uint CatForm = 768;
        public const uint EarthwardenAura = 203975;
        public const uint EclipseDummy = 79577;
        public const uint EclipseLunarAura = 48518;
        public const uint EclipseLunarSpellCnt = 326055;
        public const uint EclipseOoc = 329910;
        public const uint EclipseSolarAura = 48517;
        public const uint EclipseSolarSpellCnt = 326053;
        public const uint Exhilarate = 28742;
        public const uint FormAquaticPassive = 276012;
        public const uint FormAquatic = 1066;
        public const uint FormFlight = 33943;
        public const uint FormStag = 165961;
        public const uint FormSwiftFlight = 40120;
        public const uint FormsTrinketBear = 37340;
        public const uint FormsTrinketCat = 37341;
        public const uint FormsTrinketMoonkin = 37343;
        public const uint FormsTrinketNone = 37344;
        public const uint FormsTrinketTree = 37342;
        public const uint GalacticGuardianAura = 213708;
        public const uint GlyphOfStars = 114301;
        public const uint GlyphOfStarsVisual = 114302;
        public const uint GoreProc = 93622;
        public const uint IdolOfFeralShadows = 34241;
        public const uint IdolOfWorship = 60774;
        public const uint IncarnationKingOfTheJungle = 102543;
        public const uint Innervate = 29166;
        public const uint InnervateRank2 = 326228;
        public const uint Infusion = 37238;
        public const uint Languish = 71023;
        public const uint LifebloomFinalHeal = 33778;
        public const uint LunarInspirationOverride = 155627;
        public const uint Mangle = 33917;
        public const uint MoonfireDamage = 164812;
        public const uint Prowl = 5215;
        public const uint RejuvenationT10Proc = 70691;
        public const uint RestorationT102PBonus = 70658;
        public const uint SavageRoar = 62071;
        public const uint SkullBashCharge = 221514;
        public const uint SkullBashInterrupt = 93985;
        public const uint SunfireDamage = 164815;
        public const uint SurvivalInstincts = 50322;
        public const uint TravelForm = 783;
        public const uint ThrashBear = 77758;
        public const uint ThrashBearAura = 192090;
        public const uint ThrashCat = 106830;
    }

    [Script] // 22812 - Barkskin
    class spell_dru_barkskin : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BramblesPassive);
        }

        void HandlePeriodic(AuraEffect aurEff)
        {
            Unit target = GetTarget();
            if (target.HasAura(SpellIds.BramblesPassive))
                target.CastSpell(target, SpellIds.BramblesDamageAura, true);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandlePeriodic, 2, AuraType.PeriodicDummy));
        }
    }

    [Script] // 77758 - Berserk
    class spell_dru_berserk : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BearForm);
        }

        void HandleOnCast()
        {
            // Change into cat form
            if (GetCaster().GetShapeshiftForm() != ShapeShiftForm.BearForm)
                GetCaster().CastSpell(GetCaster(), SpellIds.BearForm, true);
        }

        public override void Register()
        {
            BeforeCast.Add(new CastHandler(HandleOnCast));
        }
    }

    [Script] // 203953 - Brambles - SPELL_DRUID_BRAMBLES_PASSIVE
    class spell_dru_brambles : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BramblesRelect, SpellIds.BramblesDamageAura);
        }

        void HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            // Prevent Removal
            PreventDefaultAction();
        }

        void HandleAfterAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            // reflect back damage to the attacker
            Unit target = GetTarget();
            Unit attacker = dmgInfo.GetAttacker();
            if (attacker != null)
                target.CastSpell(attacker, SpellIds.BramblesRelect, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)absorbAmount));
        }

        public override void Register()
        {
            OnEffectAbsorb.Add(new EffectAbsorbHandler(HandleAbsorb, 0));
            AfterEffectAbsorb.Add(new EffectAbsorbHandler(HandleAfterAbsorb, 0));
        }
    }

    [Script] // 155835 - Bristling Fur
    class spell_dru_bristling_fur : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BristlingFurGainRage);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            // BristlingFurRage = 100 * Damage / MaxHealth.
            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo != null)
            {
                Unit target = GetTarget();
                uint rage = (uint)(target.GetMaxPower(PowerType.Rage) * (float)damageInfo.GetDamage() / (float)target.GetMaxHealth());
                if (rage > 0)
                    target.CastSpell(target, SpellIds.BristlingFurGainRage, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)rage));
            }
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 768 - CatForm - SPELL_DRUID_CAT_FORM
    class spell_dru_cat_form : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Prowl);
        }

        void HandleAfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveOwnedAura(SpellIds.Prowl);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(HandleAfterRemove, 0, AuraType.ModShapeshift, AuraEffectHandleModes.Real));
        }
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

    class spell_dru_eclipse_common
    {
        public static void SetSpellCount(Unit unitOwner, uint spellId, uint amount)
        {
            Aura aura = unitOwner.GetAura(spellId);
            if (aura == null)
                unitOwner.CastSpell(unitOwner, spellId, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.AuraStack, (int)amount));
            else
                aura.SetStackAmount((byte)amount);
        }
    }

    [Script] // 48517 Eclipse (Solar) + 48518 Eclipse (Lunar)
    class spell_dru_eclipse_aura : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EclipseLunarSpellCnt, SpellIds.EclipseSolarSpellCnt, SpellIds.EclipseDummy);
        }

        void HandleRemoved(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            AuraEffect auraEffDummy = GetTarget().GetAuraEffect(SpellIds.EclipseDummy, 0);
            if (auraEffDummy == null)
                return;

            uint spellId = GetSpellInfo().Id == SpellIds.EclipseSolarAura ? SpellIds.EclipseLunarSpellCnt : SpellIds.EclipseSolarSpellCnt;
            spell_dru_eclipse_common.SetSpellCount(GetTarget(), spellId, (uint)auraEffDummy.GetAmount());
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(HandleRemoved, 0, AuraType.AddPctModifier, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 79577 - Eclipse - SPELL_DRUID_ECLIPSE_DUMMY
    class spell_dru_eclipse_dummy : AuraScript
    {
        class InitializeEclipseCountersEvent : BasicEvent
        {
            Unit _owner;
            uint _count;

            public InitializeEclipseCountersEvent(Unit owner, uint count)
            {
                _owner = owner;
                _count = count;
            }

            public override bool Execute(ulong e_time, uint p_time)
            {
                spell_dru_eclipse_common.SetSpellCount(_owner, SpellIds.EclipseSolarSpellCnt, _count);
                spell_dru_eclipse_common.SetSpellCount(_owner, SpellIds.EclipseLunarSpellCnt, _count);
                return true;
            }
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EclipseSolarSpellCnt, SpellIds.EclipseLunarSpellCnt, SpellIds.EclipseSolarAura, SpellIds.EclipseLunarAura);
        }

        void HandleProc(ProcEventInfo eventInfo)
        {
            SpellInfo spellInfo = eventInfo.GetSpellInfo();
            if (spellInfo != null)
            {
                if (spellInfo.SpellFamilyFlags & new FlagArray128(0x4, 0x0, 0x0, 0x0)) // Starfire
                    OnSpellCast(SpellIds.EclipseSolarSpellCnt, SpellIds.EclipseLunarSpellCnt, SpellIds.EclipseSolarAura);
                else if (spellInfo.SpellFamilyFlags & new FlagArray128(0x1, 0x0, 0x0, 0x0)) // Wrath
                    OnSpellCast(SpellIds.EclipseLunarSpellCnt, SpellIds.EclipseSolarSpellCnt, SpellIds.EclipseLunarAura);
            }
        }

        void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // counters are applied with a delay
            GetTarget().m_Events.AddEventAtOffset(new InitializeEclipseCountersEvent(GetTarget(), (uint)aurEff.GetAmount()), TimeSpan.FromSeconds(1));
        }

        void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAura(SpellIds.EclipseSolarSpellCnt);
            GetTarget().RemoveAura(SpellIds.EclipseLunarSpellCnt);
        }

        void OnOwnerOutOfCombat(bool isNowInCombat)
        {
            if (!isNowInCombat)
                GetTarget().CastSpell(GetTarget(), SpellIds.EclipseOoc, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(HandleApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            OnProc.Add(new AuraProcHandler(HandleProc));
            OnEnterLeaveCombat.Add(new EnterLeaveCombatHandler(OnOwnerOutOfCombat));
        }

        void OnSpellCast(uint cntSpellId, uint otherCntSpellId, uint eclipseAuraSpellId)
        {
            Unit target = GetTarget();
            Aura aura = target.GetAura(cntSpellId);
            if (aura != null)
            {
                uint remaining = aura.GetStackAmount();
                if (remaining == 0)
                    return;

                if (remaining > 1)
                    aura.SetStackAmount((byte)(remaining - 1));
                else
                {
                    // cast eclipse
                    target.CastSpell(target, eclipseAuraSpellId, new CastSpellExtraArgs(TriggerCastFlags.FullMask));

                    // Remove stacks from other one as well
                    // reset remaining power on other spellId
                    target.RemoveAura(cntSpellId);
                    target.RemoveAura(otherCntSpellId);
                }
            }
        }
    }

    [Script] // 329910 - Eclipse out of combat - SPELL_DRUID_ECLIPSE_OOC
    class spell_dru_eclipse_ooc : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EclipseDummy, SpellIds.EclipseSolarSpellCnt, SpellIds.EclipseLunarSpellCnt);
        }

        void Tick(AuraEffect aurEff)
        {
            Unit owner = GetTarget();
            AuraEffect auraEffDummy = owner.GetAuraEffect(SpellIds.EclipseDummy, 0);
            if (auraEffDummy == null)
                return;

            if (!owner.IsInCombat() && (!owner.HasAura(SpellIds.EclipseSolarSpellCnt) || !owner.HasAura(SpellIds.EclipseLunarSpellCnt)))
            {
                // Restore 2 stacks to each spell when out of combat
                spell_dru_eclipse_common.SetSpellCount(owner, SpellIds.EclipseSolarSpellCnt, (uint)auraEffDummy.GetAmount());
                spell_dru_eclipse_common.SetSpellCount(owner, SpellIds.EclipseLunarSpellCnt, (uint)auraEffDummy.GetAmount());
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(Tick, 0, AuraType.PeriodicDummy));
        }
    }

    [Script] // 203974 - Earthwarden
    class spell_dru_earthwarden : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ThrashCat, SpellIds.ThrashBear, SpellIds.EarthwardenAura);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Unit target = GetTarget();
            target.CastSpell(target, SpellIds.EarthwardenAura, true);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 22568 - Ferocious Bite
    class spell_dru_ferocious_bite : SpellScript
    {
        float _damageMultiplier = 0.0f;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.IncarnationKingOfTheJungle)
            && Global.SpellMgr.GetSpellInfo(SpellIds.IncarnationKingOfTheJungle, Difficulty.None).GetEffects().Count > 1;
        }

        void HandleHitTargetBurn(uint effIndex)
        {
            int newValue = (int)((float)GetEffectValue() * _damageMultiplier);
            SetEffectValue(newValue);
        }

        void HandleHitTargetDmg(uint effIndex)
        {
            int newValue = (int)((float)GetHitDamage() * (1.0f + _damageMultiplier));
            SetHitDamage(newValue);
        }

        void HandleLaunchTarget(uint effIndex)
        {
            Unit caster = GetCaster();

            int maxExtraConsumedPower = GetEffectValue();

            AuraEffect auraEffect = caster.GetAuraEffect(SpellIds.IncarnationKingOfTheJungle, 1);
            if (auraEffect != null)
            {
                float multiplier = 1.0f + (float)auraEffect.GetAmount() / 100.0f;
                maxExtraConsumedPower = (int)((float)maxExtraConsumedPower * multiplier);
                SetEffectValue(maxExtraConsumedPower);
            }

            _damageMultiplier = Math.Min(caster.GetPower(PowerType.Energy), maxExtraConsumedPower) / maxExtraConsumedPower;
        }

        public override void Register()
        {
            OnEffectLaunchTarget.Add(new EffectHandler(HandleLaunchTarget, 1, SpellEffectName.PowerBurn));
            OnEffectHitTarget.Add(new EffectHandler(HandleHitTargetBurn, 1, SpellEffectName.PowerBurn));
            OnEffectHitTarget.Add(new EffectHandler(HandleHitTargetDmg, 0, SpellEffectName.SchoolDamage));
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
            uint triggerspell;

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

            target.CastSpell(target, triggerspell, new CastSpellExtraArgs(aurEff));
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 203964 - Galactic Guardian
    class spell_dru_galactic_guardian : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GalacticGuardianAura);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo != null)
            {
                Unit target = GetTarget();

                // free automatic moonfire on target
                target.CastSpell(damageInfo.GetVictim(), SpellIds.MoonfireDamage, true);

                // Cast aura
                target.CastSpell(damageInfo.GetVictim(), SpellIds.GalacticGuardianAura, true);
            }
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 24858 - Moonkin Form
    class spell_dru_glyph_of_stars : AuraScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.GlyphOfStars, SpellIds.GlyphOfStarsVisual);
        }

        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            if (target.HasAura(SpellIds.GlyphOfStars))
                target.CastSpell(target, SpellIds.GlyphOfStarsVisual, true);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(SpellIds.GlyphOfStarsVisual);
        }

        public override void Register()
        {
            OnEffectApply.Add(new EffectApplyHandler(OnApply, 1, AuraType.ModShapeshift, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new EffectApplyHandler(OnRemove, 1, AuraType.ModShapeshift, AuraEffectHandleModes.Real));
        }
    }
    
    [Script] // 210706 - Gore
    class spell_dru_gore : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GoreProc, SpellIds.Mangle);
        }

        bool CheckEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return RandomHelper.randChance(aurEff.GetAmount());
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            Unit owner = GetTarget();
            owner.CastSpell(owner, SpellIds.GoreProc);
            owner.GetSpellHistory().ResetCooldown(SpellIds.Mangle, true);
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new CheckEffectProcHandler(CheckEffectProc, 0, AuraType.Dummy));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 99 - Incapacitating Roar
    class spell_dru_incapacitating_roar : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BearForm);
        }

        void HandleOnCast()
        {
            // Change into cat form
            if (GetCaster().GetShapeshiftForm() != ShapeShiftForm.BearForm)
                GetCaster().CastSpell(GetCaster(), SpellIds.BearForm, true);
        }

        public override void Register()
        {
            BeforeCast.Add(new CastHandler(HandleOnCast));
        }
    }

    [Script] // 29166 - Innervate
    class spell_dru_innervate : SpellScript
    {
        SpellCastResult CheckCast()
        {
            Player target = GetExplTargetUnit()?.ToPlayer();
            if (target == null)
                return SpellCastResult.BadTargets;

            ChrSpecializationRecord spec = CliDB.ChrSpecializationStorage.LookupByKey(target.GetPrimarySpecialization());
            if (spec == null || spec.Role != 1)
                return SpellCastResult.BadTargets;

            return SpellCastResult.SpellCastOk;
        }

        void HandleRank2()
        {
            Unit caster = GetCaster();
            if (caster != GetHitUnit())
            {
                AuraEffect innervateR2 = caster.GetAuraEffect(SpellIds.InnervateRank2, 0);
                if (innervateR2 != null)
                caster.CastSpell(caster, SpellIds.Innervate,
                    new CastSpellExtraArgs(TriggerCastFlags.IgnoreSpellAndCategoryCD | TriggerCastFlags.IgnoreCastInProgress)
                    .SetTriggeringSpell(GetSpell())
                    .AddSpellMod(SpellValueMod.BasePoint0, -innervateR2.GetAmount()));
            }
        }

        public override void Register()
        {
            OnCheckCast.Add(new CheckCastHandler(CheckCast));
            OnHit.Add(new HitHandler(HandleRank2));
        }
    }

    [Script] // 40442 - Druid Tier 6 Trinket
    class spell_dru_item_t6_trinket : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BlessingOfRemulos, SpellIds.BlessingOfElune, SpellIds.BlessingOfCenarius);
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
                spellId = SpellIds.BlessingOfRemulos;
                chance = 25;
            }
            // Rejuvenation
            else if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00000010u))
            {
                spellId = SpellIds.BlessingOfElune;
                chance = 25;
            }
            // Mangle (Bear) and Mangle (Cat)
            else if (spellInfo.SpellFamilyFlags[1].HasAnyFlag(0x00000440u))
            {
                spellId = SpellIds.BlessingOfCenarius;
                chance = 40;
            }
            else
                return;

            if (RandomHelper.randChance(chance))
                eventInfo.GetActor().CastSpell((Unit)null, spellId, new CastSpellExtraArgs(aurEff));
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 33763 - Lifebloom
    class spell_dru_lifebloom : AuraScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.LifebloomFinalHeal);
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // Final heal only on duration end
            if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Expire || GetTargetApplication().GetRemoveMode() == AuraRemoveMode.EnemySpell)
                GetCaster().CastSpell(GetUnitOwner(), SpellIds.LifebloomFinalHeal, true);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 155580 - Lunar Inspiration
    class spell_dru_lunar_inspiration : AuraScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.LunarInspirationOverride);
        }

        void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().CastSpell(GetTarget(), SpellIds.LunarInspirationOverride, true);
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(SpellIds.LunarInspirationOverride);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(AfterApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }
    
    [Script] //  8921 - Moonfire
    class spell_dru_moonfire : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.MoonfireDamage);
        }

        void HandleOnHit(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.MoonfireDamage, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.Dummy));
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
                target.CastSpell(null, SpellIds.BalanceT10BonusProc, true);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 5215 - Prowl
    class spell_dru_prowl : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CatForm);
        }

        void HandleOnCast()
        {
            // Change into cat form
            if (GetCaster().GetShapeshiftForm() != ShapeShiftForm.CatForm)
                GetCaster().CastSpell(GetCaster(), SpellIds.CatForm, true);
        }

        public override void Register()
        {
            BeforeCast.Add(new CastHandler(HandleOnCast));
        }
    }

    [Script] // 1079 - Rip
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
                byte cp = (byte)caster.ToPlayer().GetComboPoints();

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

    [Script] // 52610 - Savage Roar
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
            target.CastSpell(target, SpellIds.SavageRoar, new CastSpellExtraArgs(aurEff).SetOriginalCaster(GetCasterGUID()));
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

    [Script] // 106839 - Skull Bash
    class spell_dru_skull_bash : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SkullBashCharge, SpellIds.SkullBashInterrupt);
        }

        void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.SkullBashCharge, true);
            GetCaster().CastSpell(GetHitUnit(), SpellIds.SkullBashInterrupt, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 106898 - Stampeding Roar
    class spell_dru_stampeding_roar : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BearForm);
        }

        void HandleOnCast()
        {
            // Change into cat form
            if (GetCaster().GetShapeshiftForm() != ShapeShiftForm.BearForm)
                GetCaster().CastSpell(GetCaster(), SpellIds.BearForm, true);
        }

        public override void Register()
        {
            BeforeCast.Add(new CastHandler(HandleOnCast));
        }
    }

    [Script] // 50286 - Starfall (Dummy)
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
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaEnemy));
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

    [Script] // 61336 - Survival Instincts
    class spell_dru_survival_instincts_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.SurvivalInstincts);
        }

        void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().CastSpell(GetTarget(), SpellIds.SurvivalInstincts, true);
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(SpellIds.SurvivalInstincts);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(AfterApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 40121 - Swift Flight Form (Passive)
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
            eventInfo.GetActor().CastSpell(eventInfo.GetProcTarget(), SpellIds.BlessingOfTheClaw, new CastSpellExtraArgs(aurEff));
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
            var spellPowerCostList = spell.GetPowerCost();
            var spellPowerCost = spellPowerCostList.First(cost => cost.Power == PowerType.Mana);
            if (spellPowerCost == null)
                return;

            int amount = MathFunctions.CalculatePct(spellPowerCost.Amount, aurEff.GetAmount());
            CastSpellExtraArgs args = new (aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, amount);
            caster.CastSpell((Unit)null, SpellIds.Exhilarate, args);
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
            eventInfo.GetActor().CastSpell((Unit)null, SpellIds.Infusion, new CastSpellExtraArgs(aurEff));
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

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.Languish, GetCastDifficulty());
            int amount = (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount());
            amount /= (int)spellInfo.GetMaxTicks();

            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, amount);
            caster.CastSpell(target, SpellIds.Languish, args);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 70691 - Item T10 Restoration 4P Bonus
    class spell_dru_t10_restoration_4p_bonus : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        void FilterTargets(List<WorldObject> targets)
        {
            if (!GetCaster().ToPlayer().GetGroup())
            {
                targets.Clear();
                targets.Add(GetCaster());
            }
            else
            {
                targets.Remove(GetExplTargetUnit());
                List<Unit> tempTargets = new();
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
            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)eventInfo.GetHealInfo().GetHeal());
            eventInfo.GetActor().CastSpell((Unit)null, SpellIds.RejuvenationT10Proc, args);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 77758 - Thrash
    class spell_dru_thrash : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ThrashBearAura);
        }

        void HandleOnHitTarget(uint effIndex)
        {
            Unit hitUnit = GetHitUnit();
            if (hitUnit != null)
            {
                Unit caster = GetCaster();

                caster.CastSpell(hitUnit, SpellIds.ThrashBearAura, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleOnHitTarget, 0, SpellEffectName.SchoolDamage));
        }
    }

    [Script] // 192090 - Thrash (Aura) - SPELL_DRUID_THRASH_BEAR_AURA
    class spell_dru_thrash_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BloodFrenzyAura, SpellIds.BloodFrenzyRageGain);
        }

        void HandlePeriodic(AuraEffect aurEff)
        {
            Unit caster = GetCaster();
            if (caster != null)
                if (caster.HasAura(SpellIds.BloodFrenzyAura))
                    caster.CastSpell(caster, SpellIds.BloodFrenzyRageGain, true);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDamage));
        }
    }

    // 1066 - Aquatic Form
    // 33943 - Flight Form
    // 40120 - Swift Flight Form
    [Script] // 165961 - Stag Form
    class spell_dru_travel_form_AuraScript : AuraScript
    {
        uint triggeredSpellId;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FormStag, SpellIds.FormAquaticPassive, SpellIds.FormAquatic, SpellIds.FormFlight, SpellIds.FormSwiftFlight);
        }

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // If it stays 0, it removes Travel Form dummy in AfterRemove.
            triggeredSpellId = 0;

            // We should only handle aura interrupts.
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Interrupt)
                return;

            // Check what form is appropriate
            triggeredSpellId = GetFormSpellId(GetTarget().ToPlayer(), GetCastDifficulty(), true);

            // If chosen form is current aura, just don't remove it.
            if (triggeredSpellId == m_scriptSpellId)
                PreventDefaultAction();
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (triggeredSpellId == m_scriptSpellId)
                return;

            Player player = GetTarget().ToPlayer();

            if (triggeredSpellId != 0) // Apply new form
                player.CastSpell(player, triggeredSpellId, new CastSpellExtraArgs(aurEff));
            else // If not set, simply remove Travel Form dummy
                player.RemoveAura(SpellIds.TravelForm);
        }

        public override void Register()
        {
            OnEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.ModShapeshift, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.ModShapeshift, AuraEffectHandleModes.Real));
        }

        public static uint GetFormSpellId(Player player, Difficulty difficulty, bool requiresOutdoor)
        {
            // Check what form is appropriate
            if (player.HasSpell(SpellIds.FormAquaticPassive) && player.IsInWater()) // Aquatic form
                return SpellIds.FormAquatic;

            if (!player.IsInCombat() && player.GetSkillValue(SkillType.Riding) >= 225 && CheckLocationForForm(player, difficulty, requiresOutdoor, SpellIds.FormFlight) == SpellCastResult.SpellCastOk) // Flight form
                return player.GetSkillValue(SkillType.Riding) >= 300 ? SpellIds.FormSwiftFlight : SpellIds.FormFlight;

            if (!player.IsInWater() && CheckLocationForForm(player, difficulty, requiresOutdoor, SpellIds.FormStag) == SpellCastResult.SpellCastOk) // Stag form
                return SpellIds.FormStag;

            return 0;
        }

        static SpellCastResult CheckLocationForForm(Player targetPlayer, Difficulty difficulty, bool requireOutdoors, uint spell_id)
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spell_id, difficulty);

            if (requireOutdoors && !targetPlayer.IsOutdoors())
                return SpellCastResult.OnlyOutdoors;

            return spellInfo.CheckLocation(targetPlayer.GetMapId(), targetPlayer.GetZoneId(), targetPlayer.GetAreaId(), targetPlayer);
        }
    }

    [Script] // 783 - Travel Form (dummy)
    class spell_dru_travel_form_dummy : SpellScript
    {
        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(SpellIds.FormAquaticPassive, SpellIds.FormAquatic, SpellIds.FormStag);
        }

        SpellCastResult CheckCast()
        {
            Player player = GetCaster().ToPlayer();
            if (!player)
                return SpellCastResult.CustomError;

            uint spellId = (player.HasSpell(SpellIds.FormAquaticPassive) && player.IsInWater()) ? SpellIds.FormAquatic : SpellIds.FormStag;

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId, GetCastDifficulty());
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
            return GetCaster().IsTypeId(TypeId.Player);
        }

        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Player player = GetTarget().ToPlayer();

            // Outdoor check already passed - Travel Form (dummy) has SPELL_ATTR0_OUTDOORS_ONLY attribute.
            uint triggeredSpellId = spell_dru_travel_form_AuraScript.GetFormSpellId(player, GetCastDifficulty(), false);

            player.CastSpell(player, triggeredSpellId, new CastSpellExtraArgs(aurEff));
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
    }

    [Script] // 252216 - Tiger Dash
    class spell_dru_tiger_dash : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CatForm);
        }

        void HandleOnCast()
        {
            // Change into cat form
            if (GetCaster().GetShapeshiftForm() != ShapeShiftForm.CatForm)
                GetCaster().CastSpell(GetCaster(), SpellIds.CatForm, true);
        }

        public override void Register()
        {
            BeforeCast.Add(new CastHandler(HandleOnCast));
        }
    }

    [Script] // 252216 - Tiger Dash (Aura)
    class spell_dru_tiger_dash_AuraScript : AuraScript
    {
        void HandlePeriodic(AuraEffect aurEff)
        {
            AuraEffect effRunSpeed = GetEffect(0);
            if (effRunSpeed != null)
            {
                int reduction = aurEff.GetAmount();
                effRunSpeed.ChangeAmount(effRunSpeed.GetAmount() - reduction);
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandlePeriodic, 1, AuraType.PeriodicDummy));
        }
    }
    
    [Script] // 48438 - Wild Growth
    class spell_dru_wild_growth : SpellScript
    {
        List<WorldObject> _targets;

        public override bool Validate(SpellInfo spellInfo)
        {
            if (spellInfo.GetEffects().Count <= 2 || spellInfo.GetEffect(2).IsEffect() || spellInfo.GetEffect(2).CalcValue() <= 0)
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

            int maxTargets = GetEffectInfo(2).CalcValue(GetCaster());

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
    }

    [Script]
    class spell_dru_wild_growth_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RestorationT102PBonus);
        }

        void HandleTickUpdate(AuraEffect aurEff)
        {
            Unit caster = GetCaster();
            if (!caster)
                return;

            // calculate from base damage, not from aurEff.GetAmount() (already modified)
            float damage = caster.CalculateSpellDamage(GetUnitOwner(), aurEff.GetSpellEffectInfo());

            // Wild Growth = first tick gains a 6% bonus, reduced by 2% each tick
            float reduction = 2.0f;
            AuraEffect bonus = caster.GetAuraEffect(SpellIds.RestorationT102PBonus, 0);
            if (bonus != null)
                reduction -= MathFunctions.CalculatePct(reduction, bonus.GetAmount());
            reduction *= (aurEff.GetTickNumber() - 1);

            MathFunctions.AddPct(ref damage, 6.0f - reduction);
            aurEff.SetAmount((int)damage);
        }

        public override void Register()
        {
            OnEffectUpdatePeriodic.Add(new EffectUpdatePeriodicHandler(HandleTickUpdate, 0, AuraType.PeriodicHeal));
        }
    }
}