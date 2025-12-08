// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the Gnu General Public License. See License file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.DataStorage;
using Game.Entities;
using Game.Movement;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Spells.DemonHunter;

struct SpellIds
{
    public const uint AreatriggerDhShatteredSoulsHavoc = 8352;
    public const uint AreatriggerDhShatteredSoulsHavocDemon = 11231;
    public const uint AreatriggerDhShatteredSoulsVengeance = 11266;
    public const uint AreatriggerDhShatteredSoulsVengeanceDemon = 10693;
    public const uint AreatriggerDhSoulFragmentHavoc = 12929;
    public const uint AreatriggerDhSoulFragmentVengeance = 10665;

    public const uint AbyssalStrike = 207550;
    public const uint Annihilation = 201427;
    public const uint AnnihilationMh = 227518;
    public const uint AnnihilationOh = 201428;
    public const uint ArmyUntoOneself = 442714;
    public const uint AwakenTheDemonWithinCd = 207128;
    public const uint BladeWard = 442715;
    public const uint Blur = 212800;
    public const uint BlurTrigger = 198589;
    public const uint BurningAlive = 207739;
    public const uint BurningAliveTargetSelector = 207760;
    public const uint CalcifiedSpikesTalent = 389720;
    public const uint CalcifiedSpikesModDamage = 391171;
    public const uint ChaosNova = 179057;
    public const uint ChaosStrike = 162794;
    public const uint ChaosStrikeEnergize = 193840;
    public const uint ChaosStrikeMh = 222031;
    public const uint ChaosStrikeOh = 199547;
    public const uint ChaosTheoryTalent = 389687;
    public const uint ChaosTheoryCrit = 390195;
    public const uint ChaoticTransformation = 388112;
    public const uint CharredWarbladesHeal = 213011;
    public const uint CollectiveAnguish = 390152;
    public const uint CollectiveAnguishEyeBeam = 391057;
    public const uint CollectiveAnguishEyeBeamDamage = 391058;
    public const uint CollectiveAnguishFelDevastation = 393831;
    public const uint ConsumeSoulHavoc = 228542;
    public const uint ConsumeSoulHavocDemon = 228556;
    public const uint ConsumeSoulHavocShattered = 228540;
    public const uint ConsumeSoulHeal = 203794;
    public const uint ConsumeSoulVengeance = 208014;
    public const uint ConsumeSoulVengeanceDemon = 210050;
    public const uint ConsumeSoulVengeanceShattered = 210047;
    public const uint CycleOfHatredTalent = 258887;
    public const uint CycleOfHatredCooldownReduction = 1214887;
    public const uint CycleOfHatredRemoveStacks = 1214890;
    public const uint DarkglareBoon = 389708;
    public const uint DarkglareBoonEnergize = 391345;
    public const uint DarknessAbsorb = 209426;
    public const uint DeflectingSpikes = 321028;
    public const uint DemonBladesDmg = 203796;
    public const uint DemonSpikes = 203819;
    public const uint DemonSpikesTrigger = 203720;
    public const uint Demonic = 213410;
    public const uint DemonicOrigins = 235893;
    public const uint DemonicOriginsBuff = 235894;
    public const uint DemonicTrampleDmg = 208645;
    public const uint DemonicTrampleStun = 213491;
    public const uint DemonsBite = 162243;
    public const uint EssenceBreakDebuff = 320338;
    public const uint EyeBeam = 198013;
    public const uint EyeBeamDamage = 198030;
    public const uint EyeOfLeotherasDmg = 206650;
    public const uint FeastOfSouls = 207697;
    public const uint FeastOfSoulsPeriodicHeal = 207693;
    public const uint FeedTheDemon = 218612;
    public const uint FelBarrage = 211053;
    public const uint FelBarrageDmg = 211052;
    public const uint FelBarrageProc = 222703;
    public const uint FelDevastation = 212084;
    public const uint FelDevastationDmg = 212105;
    public const uint FelDevastationHeal = 212106;
    public const uint FelFlameFortificationTalent = 389705;
    public const uint FelFlameFortificationModDamage = 393009;
    public const uint FelRush = 195072;
    public const uint FelRushDmg = 192611;
    public const uint FelRushGround = 197922;
    public const uint FelRushWaterAir = 197923;
    public const uint Felblade = 232893;
    public const uint FelbladeCharge = 213241;
    public const uint FelbladeCooldownResetProcHavoc = 236167;
    public const uint FelbladeCooldownResetProcVengeance = 203557;
    public const uint FelbladeCooldownResetProcVisual = 204497;
    public const uint FelbladeDamage = 213243;
    public const uint FieryBrand = 204021;
    public const uint FieryBrandRank2 = 320962;
    public const uint FieryBrandDebuffRank1 = 207744;
    public const uint FieryBrandDebuffRank2 = 207771;
    public const uint FirstBlood = 206416;
    public const uint FlameCrash = 227322;
    public const uint Frailty = 224509;
    public const uint FuriousGaze = 343311;
    public const uint FuriousGazeBuff = 343312;
    public const uint FuriousThrows = 393029;
    public const uint GlaiveTempest = 342857;
    public const uint Glide = 131347;
    public const uint GlideDuration = 197154;
    public const uint GlideKnockback = 196353;
    public const uint HavocMastery = 185164;
    public const uint IllidansGrasp = 205630;
    public const uint IllidansGraspDamage = 208618;
    public const uint IllidansGraspJumpDest = 208175;
    public const uint ImmolationAura = 258920;
    public const uint InnerDemonBuff = 390145;
    public const uint InnerDemonDamage = 390137;
    public const uint InnerDemonTalent = 389693;
    public const uint InfernalStrikeCast = 189110;
    public const uint InfernalStrikeImpactDamage = 189112;
    public const uint InfernalStrikeJump = 189111;
    public const uint JaggedSpikes = 205627;
    public const uint JaggedSpikesDmg = 208790;
    public const uint JaggedSpikesProc = 208796;
    public const uint ManaRiftDmgPowerBurn = 235904;
    public const uint Metamorphosis = 191428;
    public const uint MetamorphosisDummy = 191427;
    public const uint MetamorphosisImpactDamage = 200166;
    public const uint MetamorphosisReset = 320645;
    public const uint MetamorphosisTransform = 162264;
    public const uint MetamorphosisVengeanceTransform = 187827;
    public const uint Momentum = 208628;
    public const uint MonsterRisingAgility = 452550;
    public const uint NemesisAberrations = 208607;
    public const uint NemesisBeasts = 208608;
    public const uint NemesisCritters = 208609;
    public const uint NemesisDemons = 208608;
    public const uint NemesisDragonkin = 208610;
    public const uint NemesisElementals = 208611;
    public const uint NemesisGiants = 208612;
    public const uint NemesisHumanoids = 208605;
    public const uint NemesisMechanicals = 208613;
    public const uint NemesisUndead = 208614;
    public const uint RainFromAbove = 206803;
    public const uint RainOfChaos = 205628;
    public const uint RainOfChaosImpact = 232538;
    public const uint RazorSpikes = 210003;
    public const uint RestlessHunterTalent = 390142;
    public const uint RestlessHunterBuff = 390212;
    public const uint Sever = 235964;
    public const uint ShatterSoul = 209980;
    public const uint ShatterSoul1 = 209981;
    public const uint ShatterSoul2 = 210038;
    public const uint ShatteredSoul = 226258;
    public const uint ShatteredSoulLesserSoulFragment1 = 228533;
    public const uint ShatteredSoulLesserSoulFragment2 = 237867;
    public const uint Shear = 203782;
    public const uint SigilOfChainsAreaSelector = 204834;
    public const uint SigilOfChainsGrip = 208674;
    public const uint SigilOfChainsJump = 208674;
    public const uint SigilOfChainsSlow = 204843;
    public const uint SigilOfChainsSnare = 204843;
    public const uint SigilOfChainsTargetSelect = 204834;
    public const uint SigilOfChainsVisual = 208673;
    public const uint SigilOfFlame = 204596;
    public const uint SigilOfFlameAoe = 204598;
    public const uint SigilOfFlameFlameCrash = 228973;
    public const uint SigilOfFlameVisual = 208710;
    public const uint SigilOfMisery = 207685;
    public const uint SigilOfMiseryAoe = 207685;
    public const uint SigilOfSilence = 204490;
    public const uint SigilOfSilenceAoe = 204490;
    public const uint SoulBarrier = 227225;
    public const uint SoulCleave = 228477;
    public const uint SoulCleaveDmg = 228478;
    public const uint SoulFragmentCounter = 203981;
    public const uint SoulFurnaceDamageBuff = 391172;
    public const uint SoulRending = 204909;
    public const uint SpiritBombDamage = 218677;
    public const uint SpiritBombHeal = 227255;
    public const uint SpiritBombVisual = 218678;
    public const uint StudentOfSufferingTalent = 452412;
    public const uint StudentOfSufferingAura = 453239;
    public const uint TacticalRetreatEnergize = 389890;
    public const uint TacticalRetreatTalent = 389688;
    public const uint ThrowGlaive = 185123;
    public const uint UncontainedFel = 209261;
    public const uint VengeanceDemonHunter = 212613;
    public const uint VengefulBonds = 320635;
    public const uint VengefulRetreat = 198813;
    public const uint VengefulRetreatTrigger = 198793;

    public const uint CategoryEyeBeam = 1582;
    public const uint CategoryBladeDance = 1640;
}

[Script] // Called by 232893 - Felblade
class spell_dh_army_unto_oneself : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ArmyUntoOneself, SpellIds.BladeWard);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.ArmyUntoOneself);
    }

    void ApplyBladeWard()
    {
        GetCaster().CastSpell(GetCaster(), SpellIds.BladeWard, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    public override void Register()
    {
        AfterCast.Add(new(ApplyBladeWard));
    }
}

[Script] // Called by 203819 - Demon Spikes
class spell_dh_calcified_spikes : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.CalcifiedSpikesTalent, SpellIds.CalcifiedSpikesModDamage);
    }

    public override bool Load()
    {
        return GetUnitOwner().HasAura(SpellIds.CalcifiedSpikesTalent);
    }

    void HandleAfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();
        target.CastSpell(target, SpellIds.CalcifiedSpikesModDamage, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringAura = aurEff
        });
    }

    public override void Register()
    {
        AfterEffectRemove.Add(new(HandleAfterRemove, 1, AuraType.ModArmorPctFromStat, AuraEffectHandleModes.RealOrReapplyMask));
    }
}

[Script] // 391171 - Calcified Spikes
class spell_dh_calcified_spikes_periodic : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((spellInfo.Id, 1));
    }

    void HandlePeriodic(AuraEffect aurEff)
    {
        AuraEffect damagePctTaken = GetEffect(0);
        if (damagePctTaken != null)
            damagePctTaken.ChangeAmount(damagePctTaken.GetAmount() + 1);
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(HandlePeriodic, 1, AuraType.PeriodicTriggerSpell));
    }
}

[Script] // 197125 - Chaos Strike
class spell_dh_chaos_strike : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ChaosStrikeEnergize);
    }

    void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        GetTarget().CastSpell(GetTarget(), SpellIds.ChaosStrikeEnergize, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = eventInfo.GetProcSpell()
        });
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.ProcTriggerSpell));
    }
}

[Script] // 344862 - Chaos Strike
class spell_dh_chaos_strike_initial : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ChaosStrike);
    }

    void HandleHit(uint effIndex)
    {
        GetCaster().CastSpell(GetHitUnit(), SpellIds.ChaosStrike, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnorePowerCost | TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleHit, 0, SpellEffectName.Dummy));
    }
}

[Script] // Called by 188499 - Blade Dance and 210152 - Death Sweep
class spell_dh_chaos_theory : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        if (!ValidateSpellInfo(SpellIds.ChaosTheoryCrit)
            || !ValidateSpellEffect((SpellIds.ChaosTheoryTalent, 1)))
            return false;

        SpellInfo chaosTheory = Global.SpellMgr.GetSpellInfo(SpellIds.ChaosTheoryTalent, Difficulty.None);
        return chaosTheory.GetEffect(0).CalcValue() < chaosTheory.GetEffect(1).CalcValue();
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.ChaosTheoryTalent);
    }

    void ChaosTheory()
    {
        Unit caster = GetCaster();
        Aura chaosTheory = caster.GetAura(SpellIds.ChaosTheoryTalent);
        if (chaosTheory == null)
            return;

        AuraEffect min = chaosTheory.GetEffect(0);
        AuraEffect max = chaosTheory.GetEffect(1);
        if (min == null || max == null)
            return;

        int critChance = RandomHelper.IRand(min.GetAmount(), max.GetAmount());
        caster.CastSpell(caster, SpellIds.ChaosTheoryCrit, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            SpellValueOverrides = { new(SpellValueMod.BasePoint0, critChance) }
        });
    }

    public override void Register()
    {
        AfterCast.Add(new(ChaosTheory));
    }
}

[Script] // 390195 - Chaos Theory
class spell_dh_chaos_theory_drop_charge : AuraScript
{
    void Prepare(ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        // delayed charge drop - this aura must be removed after Chaos Strike does damage and after it procs power refund
        GetAura().DropChargeDelayed(500);
    }

    public override void Register()
    {
        DoPrepareProc.Add(new(Prepare));
    }
}

[Script] // Called by 191427 - Metamorphosis
class spell_dh_chaotic_transformation : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ChaoticTransformation)
            && CliDB.SpellCategoryStorage.ContainsKey(SpellIds.CategoryEyeBeam)
            && CliDB.SpellCategoryStorage.ContainsKey(SpellIds.CategoryBladeDance);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.ChaoticTransformation);
    }

    void HandleCooldown()
    {
        GetCaster().GetSpellHistory().ResetCooldowns(cooldown =>
        {
            uint category = Global.SpellMgr.GetSpellInfo(cooldown.SpellId, Difficulty.None).CategoryId;
            return category == SpellIds.CategoryEyeBeam || category == SpellIds.CategoryBladeDance;
        }, true);
    }

    public override void Register()
    {
        AfterCast.Add(new(HandleCooldown));
    }
}

[Script] // 213010 - Charred Warblades
class spell_dh_charred_warblades : AuraScript
{
    uint _healAmount = 0;

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.CharredWarbladesHeal);
    }

    bool CheckProc(ProcEventInfo eventInfo)
    {
        return eventInfo.GetDamageInfo() != null && (eventInfo.GetDamageInfo().GetSchoolMask() & SpellSchoolMask.Fire) != 0;
    }

    void HandleAfterProc(ProcEventInfo eventInfo)
    {
        _healAmount += MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), GetEffect(0).GetAmount());
    }

    void HandleDummyTick(AuraEffect aurEff)
    {
        if (_healAmount == 0)
            return;

        GetTarget().CastSpell(GetTarget(), SpellIds.CharredWarbladesHeal,
            new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError)
            .SetTriggeringAura(aurEff)
            .AddSpellMod(SpellValueMod.BasePoint0, (int)_healAmount));

        _healAmount = 0;
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
        AfterProc.Add(new(HandleAfterProc));
        OnEffectPeriodic.Add(new(HandleDummyTick, 0, AuraType.PeriodicDummy));
    }
}

[Script] // Called by 212084 - Fel Devastation and 198013 - Eye Beam
class spell_dh_collective_anguish : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.CollectiveAnguish, SpellIds.FelDevastation, SpellIds.CollectiveAnguishEyeBeam, SpellIds.CollectiveAnguishFelDevastation);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.CollectiveAnguish);
    }

    void HandleEyeBeam()
    {
        GetCaster().CastSpell(GetCaster(), SpellIds.CollectiveAnguishEyeBeam, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    void HandleFelDevastation()
    {
        GetCaster().CastSpell(GetCaster(), SpellIds.CollectiveAnguishFelDevastation, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    public override void Register()
    {
        if (m_scriptSpellId == SpellIds.FelDevastation)
            AfterCast.Add(new(HandleEyeBeam));
        else
            AfterCast.Add(new(HandleFelDevastation));
    }
}

[Script] // 391057 - Eye Beam
class spell_dh_collective_anguish_eye_beam : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.CollectiveAnguishEyeBeamDamage);
    }

    void HandleEffectPeriodic(AuraEffect aurEff)
    {
        Unit caster = GetCaster();
        if (caster != null)
        {
            caster.CastSpell(null, SpellIds.CollectiveAnguishEyeBeamDamage, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringAura = aurEff
            });
        }
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(HandleEffectPeriodic, 0, AuraType.PeriodicTriggerSpell));
    }
}

[Script] // 320413 - Critical Chaos
class spell_dh_critical_chaos : AuraScript
{
    void CalcAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
    {
        AuraEffect amountHolder = GetEffect(1);
        if (amountHolder != null)
        {
            float critChanceDone = GetUnitOwner().GetUnitCriticalChanceDone(WeaponAttackType.BaseAttack);
            amount = (int)MathFunctions.CalculatePct(critChanceDone, amountHolder.GetAmount());
        }
    }

    void UpdatePeriodic(AuraEffect aurEff)
    {
        AuraEffect bonus = GetEffect(0);
        if (bonus != null)
            bonus.RecalculateAmount(aurEff);
    }

    public override void Register()
    {
        DoEffectCalcAmount.Add(new(CalcAmount, 0, AuraType.AddFlatModifier));
        OnEffectPeriodic.Add(new(UpdatePeriodic, 1, AuraType.PeriodicDummy));
    }
}

[Script] // Called by 198013 - Eye Beam
class spell_dh_cycle_of_hatred : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.CycleOfHatredTalent, SpellIds.CycleOfHatredCooldownReduction, SpellIds.CycleOfHatredRemoveStacks);
    }

    public override bool Load()
    {
        return GetCaster().HasAuraEffect(SpellIds.CycleOfHatredTalent, 0);
    }

    void HandleCycleOfHatred()
    {
        Unit caster = GetCaster();

        // First calculate cooldown then add another stack
        uint cycleOfHatredStack = caster.GetAuraCount(SpellIds.CycleOfHatredCooldownReduction);
        AuraEffect cycleOfHatred = caster.GetAuraEffect(SpellIds.CycleOfHatredTalent, 0);
        caster.GetSpellHistory().ModifyCooldown(GetSpellInfo(), -TimeSpan.FromSeconds(cycleOfHatred.GetAmount() * cycleOfHatredStack));

        CastSpellExtraArgs args = new();
        args.SetTriggerFlags(TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
        args.SetTriggeringSpell(GetSpell());

        caster.CastSpell(caster, SpellIds.CycleOfHatredCooldownReduction, args);
        caster.CastSpell(caster, SpellIds.CycleOfHatredRemoveStacks, args);
    }

    public override void Register()
    {
        AfterCast.Add(new(HandleCycleOfHatred));
    }
}

[Script] // 1214890 - Cycle of Hatred
class spell_dh_cycle_of_hatred_remove_stacks : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.CycleOfHatredCooldownReduction);
    }

    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Aura aura = GetTarget().GetAura(SpellIds.CycleOfHatredCooldownReduction);
        if (aura != null)
            aura.SetStackAmount(1);
    }

    public override void Register()
    {
        AfterEffectRemove.Add(new(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

[Script] // 258887 - Cycle of Hatred
class spell_dh_cycle_of_hatred_talent : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.CycleOfHatredCooldownReduction);
    }

    void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();
        target.CastSpell(target, SpellIds.CycleOfHatredCooldownReduction, TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
    }

    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        GetTarget().RemoveAurasDueToSpell(SpellIds.CycleOfHatredCooldownReduction);
    }

    public override void Register()
    {
        AfterEffectApply.Add(new(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        AfterEffectRemove.Add(new(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

[Script] // Called by 212084 - Fel Devastation
class spell_dh_darkglare_boon : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        if (!ValidateSpellInfo(SpellIds.DarkglareBoonEnergize, SpellIds.FelDevastation)
            || !ValidateSpellEffect((SpellIds.DarkglareBoon, 3)))
            return false;

        SpellInfo darkglareBoon = Global.SpellMgr.GetSpellInfo(SpellIds.DarkglareBoon, Difficulty.None);
        return darkglareBoon.GetEffect(0).CalcValue() < darkglareBoon.GetEffect(1).CalcValue()
            && darkglareBoon.GetEffect(2).CalcValue() < darkglareBoon.GetEffect(3).CalcValue();
    }

    public override bool Load()
    {
        return GetUnitOwner().HasAura(SpellIds.DarkglareBoon);
    }

    void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        // Tooltip mentions "fully channelled" being a requirement but ingame it always reduces cooldown and energizes, even when manually cancelled
        //if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
        //    return;

        Unit target = GetTarget();
        Aura darkglareBoon = target.GetAura(SpellIds.DarkglareBoon);

        uint unused = 0;
        TimeSpan cooldown = TimeSpan.Zero;
        TimeSpan categoryCooldown = TimeSpan.Zero;
        SpellHistory.GetCooldownDurations(GetSpellInfo(), 0, ref cooldown, ref unused, ref categoryCooldown);
        int reductionPct = RandomHelper.IRand(darkglareBoon.GetEffect(0).GetAmount(), darkglareBoon.GetEffect(1).GetAmount());
        TimeSpan cooldownReduction = TimeSpan.FromSeconds(MathFunctions.CalculatePct(MathF.Max((float)cooldown.TotalMilliseconds, (float)categoryCooldown.TotalMilliseconds), reductionPct));

        int energizeValue = RandomHelper.IRand(darkglareBoon.GetEffect(2).GetAmount(), darkglareBoon.GetEffect(3).GetAmount());

        target.GetSpellHistory().ModifyCooldown(SpellIds.FelDevastation, -cooldownReduction);

        target.CastSpell(target, SpellIds.DarkglareBoonEnergize, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            SpellValueOverrides = { new(SpellValueMod.BasePoint0, energizeValue) }
        });
    }

    public override void Register()
    {
        OnEffectRemove.Add(new(HandleEffectRemove, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
    }
}

[Script] // 209426 - Darkness
class spell_dh_darkness : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((spellInfo.Id, 1));
    }

    void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
    {
        // Set absorbtion amount to unlimited
        amount = -1;
    }

    void Absorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
    {
        AuraEffect chanceEffect = GetEffect(1);
        if (chanceEffect != null)
            if (RandomHelper.randChance(chanceEffect.GetAmount()))
                absorbAmount = dmgInfo.GetDamage();
    }

    public override void Register()
    {
        DoEffectCalcAmount.Add(new(CalculateAmount, 0, AuraType.SchoolAbsorb));
        OnEffectAbsorb.Add(new(Absorb, 0));
    }
}

// 196718 - Darkness
[Script] // Id: 6615
class areatrigger_dh_darkness : AreaTriggerAI
{
    SpellInfo _absorbAuraInfo;

    public areatrigger_dh_darkness(AreaTrigger areaTrigger) : base(areaTrigger)
    {
        _absorbAuraInfo = Global.SpellMgr.GetSpellInfo(SpellIds.DarknessAbsorb, Difficulty.None);
    }

    public override void OnUnitEnter(Unit unit)
    {
        Unit caster = at.GetCaster();
        if (caster == null || caster.IsValidAssistTarget(unit, _absorbAuraInfo))
            return;

        caster.CastSpell(unit, SpellIds.DarknessAbsorb, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            SpellValueOverrides = { new(SpellValueMod.Duration, at.GetDuration()) }
        });
    }

    public override void OnUnitExit(Unit unit, AreaTriggerExitReason reason)
    {
        unit.RemoveAura(SpellIds.DarknessAbsorb, at.GetCasterGUID());
    }
}

[Script] // 203819 - Demon Spikes
class spell_dh_deflecting_spikes : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.DeflectingSpikes)
            && ValidateSpellEffect((spellInfo.Id, 0))
            && spellInfo.GetEffect(0).IsAura(AuraType.ModParryPercent);
    }

    void HandleParryChance(ref WorldObject target)
    {
        if (!GetCaster().HasAura(SpellIds.DeflectingSpikes))
            target = null;
    }

    public override void Register()
    {
        OnObjectTargetSelect.Add(new(HandleParryChance, 0, Targets.UnitCaster));
    }
}

// 213410 - Demonic (attached to 212084 - Fel Devastation and 198013 - Eye Beam)
[Script("spell_dh_demonic_havoc", SpellIds.MetamorphosisTransform)]
[Script("spell_dh_demonic_vengeance", SpellIds.MetamorphosisVengeanceTransform)]
class spell_dh_demonic(uint transformSpellId) : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(transformSpellId)
            && ValidateSpellEffect((SpellIds.Demonic, 0))
            && Global.SpellMgr.GetSpellInfo(SpellIds.Demonic, Difficulty.None).GetEffect(0).IsAura();
    }

    public override bool Load()
    {
        return GetCaster().HasAuraEffect(SpellIds.Demonic, 0);
    }

    void TriggerMetamorphosis()
    {
        Unit caster = GetCaster();
        AuraEffect demonic = caster.GetAuraEffect(SpellIds.Demonic, 0);
        if (demonic == null)
            return;

        int duration = demonic.GetAmount() + GetSpell().GetChannelDuration();
        Aura aura = caster.GetAura(transformSpellId);
        if (aura != null)
        {
            aura.SetMaxDuration(aura.GetDuration() + duration);
            aura.SetDuration(aura.GetMaxDuration());
            return;
        }

        SpellCastTargets targets = new();
        targets.SetUnitTarget(caster);

        Spell spell = new Spell(caster, Global.SpellMgr.GetSpellInfo(transformSpellId, Difficulty.None),
            TriggerCastFlags.IgnoreSpellAndCategoryCD | TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            ObjectGuid.Empty, GetSpell().m_castId);
        spell.m_SpellVisual.SpellXSpellVisualID = 0;
        spell.m_SpellVisual.ScriptVisualID = 0;
        spell.SetSpellValue(new(SpellValueMod.Duration, duration));
        spell.Prepare(targets);
    }


    public override void Register()
    {
        AfterCast.Add(new(TriggerMetamorphosis));
    }
}

[Script] // 203720 - Demon Spikes
class spell_dh_demon_spikes : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.DemonSpikes);
    }

    void HandleArmor(uint effIndex)
    {
        GetCaster().CastSpell(GetCaster(), SpellIds.DemonSpikes, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleArmor, 0, SpellEffectName.Dummy));
    }
}

[Script] // 258860 - Essence Break
class spell_dh_essence_break : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.EssenceBreakDebuff);
    }

    void HandleDebuff(uint effIndex)
    {
        Unit caster = GetCaster();

        var targets = new CastSpellTargetArg(GetHitUnit());
        // debuff application is slightly delayed on official servers (after animation fully finishes playing)
        caster.m_Events.AddEventAtOffset(() =>
        {
            if (targets.Targets == null)
                return;

            targets.Targets.Update(caster);

            caster.CastSpell(targets, SpellIds.EssenceBreakDebuff, TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
        }, TimeSpan.FromSeconds(300));
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDebuff, 0, SpellEffectName.SchoolDamage));
    }
}

[Script] // 198013 - Eye Beam
class spell_dh_eye_beam : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.EyeBeamDamage);
    }

    void HandleEffectPeriodic(AuraEffect aurEff)
    {
        Unit caster = GetCaster();
        if (caster != null)
            caster.CastSpell(null, SpellIds.EyeBeamDamage, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringAura = aurEff
            });
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(HandleEffectPeriodic, 0, AuraType.PeriodicTriggerSpell));
    }
}

[Script] // Called by 228477 - Soul Cleave
class spell_dh_feast_of_souls : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FeastOfSouls, SpellIds.FeastOfSoulsPeriodicHeal);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.FeastOfSouls);
    }

    void HandleHeal()
    {
        GetCaster().CastSpell(GetCaster(), SpellIds.FeastOfSoulsPeriodicHeal, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    public override void Register()
    {
        AfterCast.Add(new(HandleHeal));
    }
}

[Script] // 212084 - Fel Devastation
class spell_dh_fel_devastation : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FelDevastationHeal);
    }

    void HandlePeriodicEffect(AuraEffect aurEff)
    {
        Unit caster = GetCaster();
        if (caster != null)
            caster.CastSpell(caster, SpellIds.FelDevastationHeal, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringAura = aurEff
            });
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(HandlePeriodicEffect, 0, AuraType.PeriodicTriggerSpell));
    }
}

[Script] // Called by 258920 - Immolation Aura
class spell_dh_fel_flame_fortification : AuraScript
{
    public override bool Validate(SpellInfo spell)
    {
        return ValidateSpellInfo(SpellIds.FelFlameFortificationTalent, SpellIds.FelFlameFortificationModDamage);
    }

    public override bool Load()
    {
        return GetUnitOwner().HasAura(SpellIds.FelFlameFortificationTalent);
    }

    void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();
        target.CastSpell(target, SpellIds.FelFlameFortificationModDamage, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringAura = aurEff,
            OriginalCastId = aurEff.GetBase().GetCastId()
        });
    }

    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        GetTarget().RemoveAurasDueToSpell(SpellIds.FelFlameFortificationModDamage);
    }

    public override void Register()
    {
        AfterEffectApply.Add(new(OnApply, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
        AfterEffectRemove.Add(new(OnRemove, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
    }
}

[Script] // 232893 - Felblade
class spell_dh_felblade : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FelbladeCharge);
    }

    void HandleCharge(uint effIndex)
    {
        uint spellToCast = GetCaster().IsWithinMeleeRange(GetHitUnit()) ? SpellIds.FelbladeDamage : SpellIds.FelbladeCharge;
        GetCaster().CastSpell(GetHitUnit(), spellToCast, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleCharge, 0, SpellEffectName.Dummy));
    }
}

[Script] // 213241 - Felblade Charge
class spell_dh_felblade_charge : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FelbladeDamage);
    }

    void HandleDamage(uint effIndex)
    {
        GetCaster().CastSpell(GetHitUnit(), SpellIds.FelbladeDamage, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDamage, 0, SpellEffectName.Charge));
    }
}

// 203557 - Felblade (Vengeance cooldow reset proc aura)
[Script] // 236167 - Felblade (Havoc cooldow reset proc aura)
class spell_dh_felblade_cooldown_reset_proc : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.Felblade);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        GetTarget().GetSpellHistory().ResetCooldown(SpellIds.Felblade, true);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.ProcTriggerSpell));
    }
}

[Script] // 204021 - Fiery Brand
class spell_dh_fiery_brand : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FieryBrandDebuffRank1, SpellIds.FieryBrandDebuffRank2, SpellIds.FieryBrandRank2);
    }

    void HandleDamage(uint effIndex)
    {
        GetCaster().CastSpell(GetHitUnit(), GetCaster().HasAura(SpellIds.FieryBrandRank2) ? SpellIds.FieryBrandDebuffRank2 : SpellIds.FieryBrandDebuffRank1,
            new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringSpell = GetSpell()
            });
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDamage, 1, SpellEffectName.SchoolDamage));
    }
}

[Script] // 206416 - First Blood
class spell_dh_first_blood : AuraScript
{
    ObjectGuid _firstTargetGuid;

    public ObjectGuid GetFirstTarget() { return _firstTargetGuid; }

    public void SetFirstTarget(ObjectGuid targetGuid) { _firstTargetGuid = targetGuid; }

    public override void Register()
    {
    }
}

[Script] // Called by 198013 - Eye Beam
class spell_dh_furious_gaze : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FuriousGaze, SpellIds.FuriousGazeBuff);
    }

    public override bool Load()
    {
        return GetUnitOwner().HasAura(SpellIds.FuriousGaze);
    }

    void HandleAfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
            return;

        Unit target = GetTarget();
        target.CastSpell(target, SpellIds.FuriousGazeBuff, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringAura = aurEff
        });
    }

    public override void Register()
    {
        AfterEffectRemove.Add(new(HandleAfterRemove, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
    }
}

// 342817 - Glaive Tempest
[Script] // Id - 21832
class at_dh_glaive_tempest(AreaTrigger areaTrigger) : AreaTriggerAI(areaTrigger)
{
    TaskScheduler _scheduler = new();

    public override void OnCreate(Spell creatingSpell)
    {
        _scheduler.Schedule(TimeSpan.Zero, task =>
            {
                TimeSpan period = TimeSpan.FromSeconds(500); // TimeSpan.FromSeconds(500), affected by haste
                Unit caster = at.GetCaster();
                if (caster != null)
                {
                    period *= caster.m_unitData.ModHaste;
                    caster.CastSpell(at.GetPosition(), SpellIds.GlaiveTempest, TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
                    caster.CastSpell(at.GetPosition(), SpellIds.GlaiveTempest, TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
                }
                task.Repeat(period);
            });
    }

    public override void OnUpdate(uint diff)
    {
        _scheduler.Update(diff);
    }
}

[Script] // Called by 162264 - Metamorphosis
class spell_dh_inner_demon : AuraScript
{
    public override bool Validate(SpellInfo spell)
    {
        return ValidateSpellInfo(SpellIds.InnerDemonTalent, SpellIds.InnerDemonBuff);
    }

    public override bool Load()
    {
        return GetUnitOwner().HasAura(SpellIds.InnerDemonTalent); // This spell has a proc, but is just a copypaste from spell 390145 (also don't have a TimeSpan.FromSeconds(5) cooldown)
    }

    void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();
        target.CastSpell(target, SpellIds.InnerDemonBuff, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
        });
    }

    public override void Register()
    {
        AfterEffectApply.Add(new(OnApply, 0, AuraType.Transform, AuraEffectHandleModes.RealOrReapplyMask));
    }
}

// 390139 - Inner Demon
[Script] // Id - 26749
class at_dh_inner_demon(AreaTrigger areaTrigger) : AreaTriggerAI(areaTrigger)
{
    public override void OnInitialize()
    {
        SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(at.GetSpellId(), Difficulty.None);
        if (spellInfo == null)
            return;

        Unit caster = at.GetCaster();
        if (caster == null)
            return;

        Position destPos = at.GetFirstCollisionPosition(spellInfo.GetEffect(0).CalcValue(caster) + at.GetMaxSearchRadius(), at.GetRelativeAngle(caster));
        PathGenerator path = new(at);

        path.CalculatePath(destPos.GetPositionX(), destPos.GetPositionY(), destPos.GetPositionZ(), false);

        at.InitSplines(path.GetPath());
    }

    public override void OnRemove()
    {
        Unit caster = at.GetCaster();
        if (caster != null)
            caster.CastSpell(caster.GetPosition(), SpellIds.InnerDemonDamage, TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
    }
}

[Script] // 388118 - Know Your Enemy
class spell_dh_know_your_enemy : AuraScript
{
    void CalcAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
    {
        AuraEffect amountHolder = GetEffect(1);
        if (amountHolder != null)
        {
            float critChanceDone = GetUnitOwner().GetUnitCriticalChanceDone(WeaponAttackType.BaseAttack);
            amount = (int)MathFunctions.CalculatePct(critChanceDone, amountHolder.GetAmount());
        }
    }

    void UpdatePeriodic(AuraEffect aurEff)
    {
        AuraEffect bonus = GetEffect(0);
        if (bonus != null)
            bonus.RecalculateAmount(aurEff);
    }

    public override void Register()
    {
        DoEffectCalcAmount.Add(new(CalcAmount, 0, AuraType.ModCritDamageBonus));
        OnEffectPeriodic.Add(new(UpdatePeriodic, 1, AuraType.PeriodicDummy));
    }
}

[Script] // 209258 - Last Resort
class spell_dh_last_resort : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.UncontainedFel, SpellIds.MetamorphosisVengeanceTransform)
            && ValidateSpellEffect((spellInfo.Id, 1));
    }

    void HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
    {
        Unit target = GetTarget();
        if (target.HasAura(SpellIds.UncontainedFel))
        {
            absorbAmount = 0;
            return;
        }

        PreventDefaultAction();

        CastSpellExtraArgs castArgs = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError | TriggerCastFlags.IgnoreSpellAndCategoryCD;

        target.CastSpell(target, SpellIds.MetamorphosisVengeanceTransform, castArgs);
        target.CastSpell(target, SpellIds.UncontainedFel, castArgs);

        target.SetHealth(target.CountPctFromMaxHealth(GetEffectInfo(1).CalcValue(target)));
    }

    public override void Register()
    {
        OnEffectAbsorb.Add(new(HandleAbsorb, 0));
    }
}

[Script] // 452414 - Monster Rising
class spell_dh_monster_rising : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.MonsterRisingAgility, SpellIds.MetamorphosisTransform, SpellIds.MetamorphosisVengeanceTransform);
    }

    void HandlePeriodic(AuraEffect aurEff)
    {
        Unit target = GetTarget();
        AuraApplication statBuff = target.GetAuraApplication(SpellIds.MonsterRisingAgility);

        if (target.HasAura(SpellIds.MetamorphosisTransform) || target.HasAura(SpellIds.MetamorphosisVengeanceTransform))
        {
            if (statBuff != null)
                target.RemoveAura(statBuff);
        }
        else if (statBuff == null)
        {
            target.CastSpell(target, SpellIds.MonsterRisingAgility, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringAura = aurEff
            });
        }
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(HandlePeriodic, 0, AuraType.PeriodicDummy));
    }
}

// 188499 - Blade Dance
[Script] // 210152 - Death Sweep
class spell_dh_blade_dance : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FirstBlood);
    }

    void DecideFirstTarget(List<WorldObject> targetList)
    {
        if (targetList.Empty())
            return;

        Aura aura = GetCaster().GetAura(SpellIds.FirstBlood);
        if (aura == null)
            return;

        ObjectGuid firstTargetGuid = ObjectGuid.Empty;
        ObjectGuid selectedTarget = GetCaster().GetTarget();

        // Prefer the selected target if he is one of the enemies
        if (targetList.Count > 1 && !selectedTarget.IsEmpty())
        {
            var it = targetList.Find(obj => obj.GetGUID() == selectedTarget);
            if (it != null)
                firstTargetGuid = it.GetGUID();
        }

        if (firstTargetGuid.IsEmpty())
            firstTargetGuid = targetList.FirstOrDefault().GetGUID();

        spell_dh_first_blood script = aura.GetScript<spell_dh_first_blood>();
        if (script != null)
            script.SetFirstTarget(firstTargetGuid);
    }


    public override void Register()
    {
        OnObjectAreaTargetSelect.Add(new(DecideFirstTarget, 0, Targets.UnitSrcAreaEnemy));
    }
}

// 199552 - Blade Dance
// 200685 - Blade Dance
// 210153 - Death Sweep
[Script] // 210155 - Death Sweep
class spell_dh_blade_dance_damage : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FirstBlood);
    }

    void HandleHitTarget()
    {
        int damage = GetHitDamage();

        AuraEffect aurEff = GetCaster().GetAuraEffect(SpellIds.FirstBlood, 0);
        if (aurEff != null)
        {
            spell_dh_first_blood script = aurEff.GetBase().GetScript<spell_dh_first_blood>();
            if (script != null && GetHitUnit().GetGUID() == script.GetFirstTarget())
                MathFunctions.AddPct(ref damage, aurEff.GetAmount());
        }

        SetHitDamage(damage);
    }

    public override void Register()
    {
        OnHit.Add(new(HandleHitTarget));
    }
}

[Script] // 131347 - Glide
class spell_dh_glide : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.GlideKnockback, SpellIds.GlideDuration, SpellIds.VengefulRetreatTrigger, SpellIds.FelRush);
    }

    SpellCastResult CheckCast()
    {
        Unit caster = GetCaster();
        if (caster.IsMounted() || caster.GetVehicleBase() != null)
            return SpellCastResult.DontReport;

        if (!caster.IsFalling())
            return SpellCastResult.NotOnGround;

        return SpellCastResult.SpellCastOk;
    }

    void HandleCast()
    {
        Player caster = GetCaster().ToPlayer();
        if (caster == null)
            return;

        caster.CastSpell(caster, SpellIds.GlideKnockback, true);
        caster.CastSpell(caster, SpellIds.GlideDuration, true);

        caster.GetSpellHistory().StartCooldown(Global.SpellMgr.GetSpellInfo(SpellIds.VengefulRetreatTrigger, GetCastDifficulty()), 0, null, false, TimeSpan.FromSeconds(250));
        caster.GetSpellHistory().StartCooldown(Global.SpellMgr.GetSpellInfo(SpellIds.FelRush, GetCastDifficulty()), 0, null, false, TimeSpan.FromSeconds(250));
    }

    public override void Register()
    {
        OnCheckCast.Add(new(CheckCast));
        BeforeCast.Add(new(HandleCast));
    }
}

[Script] // 131347 - Glide
class spell_dh_glide_AuraScript : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.GlideDuration);
    }

    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        GetTarget().RemoveAura(SpellIds.GlideDuration);
    }

    public override void Register()
    {
        AfterEffectRemove.Add(new(OnRemove, 0, AuraType.FeatherFall, AuraEffectHandleModes.Real));
    }
}

[Script] // 197154 - Glide
class spell_dh_glide_timer : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.Glide);
    }

    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        GetTarget().RemoveAura(SpellIds.Glide);
    }

    public override void Register()
    {
        AfterEffectRemove.Add(new(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

[Script] // Called by 162264 - Metamorphosis
class spell_dh_restless_hunter : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.RestlessHunterTalent, SpellIds.RestlessHunterBuff, SpellIds.FelRush)
            && CliDB.SpellCategoryStorage.HasRecord(Global.SpellMgr.GetSpellInfo(SpellIds.FelRush, Difficulty.None).ChargeCategoryId);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.RestlessHunterTalent);
    }


    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();

        target.CastSpell(target, SpellIds.RestlessHunterBuff, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringAura = aurEff
        });

        target.GetSpellHistory().RestoreCharge(Global.SpellMgr.GetSpellInfo(SpellIds.FelRush, GetCastDifficulty()).ChargeCategoryId);
    }

    public override void Register()
    {
        AfterEffectRemove.Add(new(OnRemove, 0, AuraType.Transform, AuraEffectHandleModes.RealOrReapplyMask));
    }
}

[Script] // 388116 - Shattered Destiny
class spell_dh_shattered_destiny : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.MetamorphosisTransform)
            && ValidateSpellEffect((spellInfo.Id, 1))
            && spellInfo.GetEffect(0).IsAura()
            && spellInfo.GetEffect(1).IsAura();
    }

    bool CheckFurySpent(ProcEventInfo eventInfo)
    {
        Spell procSpell = eventInfo.GetProcSpell();
        if (procSpell == null)
            return false;

        if (!eventInfo.GetActor().HasAura(SpellIds.MetamorphosisTransform))
            return false;

        _furySpent += procSpell.GetPowerTypeCostAmount(PowerType.Fury).GetValueOrDefault(0);
        return _furySpent >= GetEffect(1).GetAmount();
    }

    void HandleProc(ProcEventInfo eventInfo)
    {
        Aura metamorphosis = GetTarget().GetAura(SpellIds.MetamorphosisTransform);
        if (metamorphosis == null)
            return;

        int requiredFuryAmount = GetEffect(1).GetAmount();
        metamorphosis.SetDuration(metamorphosis.GetDuration() + _furySpent / requiredFuryAmount * GetEffect(0).GetAmount());
        _furySpent %= requiredFuryAmount;
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckFurySpent));
        OnProc.Add(new(HandleProc));
    }


    int _furySpent = 0;
}

[Script] // 391166 - Soul Furnace
class spell_dh_soul_furnace : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.SoulFurnaceDamageBuff);
    }

    void CalculateSpellMod(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        if (GetStackAmount() == GetAura().CalcMaxStackAmount())
        {
            GetTarget().CastSpell(GetTarget(), SpellIds.SoulFurnaceDamageBuff, true);
            Remove();
        }
    }

    public override void Register()
    {
        AfterEffectApply.Add(new(CalculateSpellMod, 0, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask));
    }
}

[Script] // 339424 - Soul Furnace
class spell_dh_soul_furnace_conduit : AuraScript
{
    void CalculateSpellMod(AuraEffect aurEff, ref SpellModifier spellMod)
    {
        if (aurEff.GetAmount() == 10)
        {
            if (spellMod == null)
            {
                spellMod = new SpellModifierByClassMask(GetAura());
                spellMod.op = SpellModOp.HealingAndDamage;
                spellMod.type = SpellModType.Pct;
                spellMod.spellId = GetId();
                ((SpellModifierByClassMask)spellMod).mask = new FlagArray128(0x80000000);
                ((SpellModifierByClassMask)spellMod).value = GetEffect(1).GetAmount() + 1;
            }
        }
    }

    public override void Register()
    {
        DoEffectCalcSpellMod.Add(new(CalculateSpellMod, 0, AuraType.Dummy));
    }
}

// 202138 - Sigil of Chains
// 204596 - Sigil of Flame
// 207684 - Sigil of Misery
// 202137 - Sigil of Silence
//template<uint TriggerSpellId, uint TriggerSpellId2 = 0 >
[Script("areatrigger_dh_sigil_of_chains", SpellIds.SigilOfChainsTargetSelect, SpellIds.SigilOfChainsVisual)]
[Script("areatrigger_dh_sigil_of_flame", SpellIds.SigilOfFlameAoe, SpellIds.SigilOfFlameVisual)]
[Script("areatrigger_dh_sigil_of_silence", SpellIds.SigilOfSilenceAoe)]
[Script("areatrigger_dh_sigil_of_misery", SpellIds.SigilOfMiseryAoe)]
class areatrigger_dh_generic_sigil(AreaTrigger areaTrigger, uint triggerSpellId, uint triggerSpellId2 = 0): AreaTriggerAI(areaTrigger)
{
    public override void OnRemove()
    {
        Unit caster = at.GetCaster();
        if (caster != null)
        {
            caster.CastSpell(at.GetPosition(), triggerSpellId);
            if (triggerSpellId2 != 0)
                caster.CastSpell(at.GetPosition(), triggerSpellId2);
        }
    }
}

[Script] // 208673 - Sigil of Chains
class spell_dh_sigil_of_chains : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.SigilOfChainsSlow, SpellIds.SigilOfChainsGrip);
    }

    void HandleEffectHitTarget(uint effIndex)
    {
        WorldLocation loc = GetExplTargetDest();
        if (loc != null)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.SigilOfChainsSlow, true);
            GetHitUnit().CastSpell(loc.GetPosition(), SpellIds.SigilOfChainsGrip, true);
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleEffectHitTarget, 0, SpellEffectName.Dummy));
    }
}

[Script] // Called by 204598 - Sigil of Flame
class spell_dh_student_of_suffering : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.StudentOfSufferingTalent, SpellIds.StudentOfSufferingAura);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.StudentOfSufferingTalent);
    }

    void HandleStudentOfSuffering()
    {
        GetCaster().CastSpell(GetCaster(), SpellIds.StudentOfSufferingAura, TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
    }

    public override void Register()
    {
        AfterCast.Add(new(HandleStudentOfSuffering));
    }
}

[Script] // Called by 198793 - Vengeful Retreat
class spell_dh_tactical_retreat : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.TacticalRetreatTalent, SpellIds.TacticalRetreatEnergize);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.TacticalRetreatTalent);
    }

    void Energize()
    {
        GetCaster().CastSpell(GetCaster(), SpellIds.TacticalRetreatEnergize, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    public override void Register()
    {
        AfterCast.Add(new(Energize));
    }
}

[Script] // 444931 - Unhindered Assault
class spell_dh_unhindered_assault : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.Felblade);
    }

    void HandleOnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        GetTarget().GetSpellHistory().ResetCooldown(SpellIds.Felblade, true);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleOnProc, 0, AuraType.Dummy));
    }
}

[Script] // 198813 - Vengeful Retreat
class spell_dh_vengeful_retreat_damage : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.VengefulBonds);
    }

    void HandleVengefulBonds(List<WorldObject> targets)
    {
        if (!GetCaster().HasAura(SpellIds.VengefulBonds))
            targets.Clear();
    }

    public override void Register()
    {
        OnObjectAreaTargetSelect.Add(new(HandleVengefulBonds, 0, Targets.UnitSrcAreaEnemy));
    }
}

[Script] // 452409 - Violent Transformation
class spell_dh_violent_transformation : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.SigilOfFlame, SpellIds.VengeanceDemonHunter, SpellIds.FelDevastation, SpellIds.ImmolationAura);
    }

    void HandleOnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        Unit target = GetTarget();
        target.GetSpellHistory().RestoreCharge(Global.SpellMgr.GetSpellInfo(SpellIds.SigilOfFlame, GetCastDifficulty()).ChargeCategoryId);

        if (target.HasAura(SpellIds.VengeanceDemonHunter))
            target.GetSpellHistory().ResetCooldown(SpellIds.FelDevastation, true);
        else
            target.GetSpellHistory().RestoreCharge(Global.SpellMgr.GetSpellInfo(SpellIds.ImmolationAura, GetCastDifficulty()).ChargeCategoryId);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleOnProc, 0, AuraType.Dummy));
    }
}