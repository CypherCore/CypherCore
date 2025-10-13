// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the Gnu General Public License. See License file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.Spells.Rogue;

struct SpellIds
{
    public const uint AcrobaticStrikesProc = 455144;
    public const uint AdrenalineRush = 13750;
    public const uint AirborneIrritant = 200733;
    public const uint AmplifyingPoison = 381664;
    public const uint AmplifyingPoisonDebuff = 383414;
    public const uint AtrophicPoison = 381637;
    public const uint AtrophicPoisonDebuff = 392388;
    public const uint BetweenTheEyes = 199804;
    public const uint BlackjackTalent = 379005;
    public const uint Blackjack = 394119;
    public const uint BladeFlurry = 13877;
    public const uint BladeFlurryExtraAttack = 22482;
    public const uint BlindArea = 427773;
    public const uint Broadside = 193356;
    public const uint BuriedTreasure = 199600;
    public const uint CheatDeathDummy = 31231;
    public const uint CheatedDeath = 45181;
    public const uint CheatingDeath = 45182;
    public const uint CloakedInShadowsTalent = 382515;
    public const uint CloakedInShadowsAbsorb = 386165;
    public const uint CripplingPoison = 3408;
    public const uint CripplingPoisonDebuff = 3409;
    public const uint DeadlyPoison = 2823;
    public const uint DeadlyPoisonDebuff = 2818;
    public const uint DeadlyPoisonInstantDamage = 113780;
    public const uint GrandMelee = 193358;
    public const uint GrapplingHook = 195457;
    public const uint ImprovedGarroteAfterStealth = 392401;
    public const uint ImprovedGarroteStealth = 392403;
    public const uint ImprovedGarroteTalent = 381632;
    public const uint ImprovedShiv = 319032;
    public const uint InstantPoison = 315584;
    public const uint InstantPoisonDamage = 315585;
    public const uint KillingSpree = 51690;
    public const uint KillingSpreeTeleport = 57840;
    public const uint KillingSpreeWeaponDmg = 57841;
    public const uint KillingSpreeDmgBuff = 61851;
    public const uint MarkedForDeath = 137619;
    public const uint MainGauche = 86392;
    public const uint NightTerrors = 277953;
    public const uint NumbingPoison = 5761;
    public const uint NumbingPoisonDebuff = 5760;
    public const uint PremeditationPassive = 343160;
    public const uint PremeditationAura = 343173;
    public const uint PremeditationEnergize = 343170;
    public const uint PreyOnTheWeakTalent = 131511;
    public const uint PreyOnTheWeak = 255909;
    public const uint RuthlessPrecision = 193357;
    public const uint Sanctuary = 98877;
    public const uint SkullAndCrossbones = 199603;
    public const uint ShadowDance = 185313;
    public const uint ShadowFocus = 108209;
    public const uint ShadowFocusEffect = 112942;
    public const uint ShadowsGrasp = 206760;
    public const uint ShivNatureDamage = 319504;
    public const uint ShotInTheDarkTalent = 257505;
    public const uint ShotInTheDarkBuff = 257506;
    public const uint ShurikenStormDamage = 197835;
    public const uint ShurikenStormEnergize = 212743;
    public const uint SliceAndDice = 315496;
    public const uint Sprint = 2983;
    public const uint SoothingDarknessTalent = 393970;
    public const uint SoothingDarknessHeal = 393971;
    public const uint Stealth = 1784;
    public const uint StealthStealthAura = 158185;
    public const uint StealthShapeshiftAura = 158188;
    public const uint SymbolsOfDeathCritAura = 227151;
    public const uint SymbolsOfDeathRANK2 = 328077;
    public const uint TrueBearing = 193359;
    public const uint TurnTheTablesBuff = 198027;
    public const uint Vanish = 1856;
    public const uint VanishAura = 11327;
    public const uint TricksOfTheTrade = 57934;
    public const uint TricksOfTheTradeProc = 59628;
    public const uint HonorAmongThievesEnergize = 51699;
    public const uint T5_2PSetBonus = 37169;
    public const uint VenomousWounds = 79134;
    public const uint WoundPoison = 8679;
    public const uint WoundPoisonDebuff = 8680;

    public static (uint, uint)[] PoisonAuraToDebuff =
    {
        (WoundPoison, WoundPoisonDebuff),
        (DeadlyPoison, DeadlyPoisonDebuff),
        (AmplifyingPoison, AmplifyingPoisonDebuff),
        (CripplingPoison, CripplingPoisonDebuff),
        (NumbingPoison, NumbingPoisonDebuff),
        (InstantPoison, InstantPoisonDamage),
        (AtrophicPoison, AtrophicPoisonDebuff),
    };
}

[Script] // 455143 - Acrobatic Strikes
class spell_rog_acrobatic_strikes : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.AcrobaticStrikesProc);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        GetTarget().CastSpell(GetTarget(), SpellIds.AcrobaticStrikesProc, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringAura = aurEff
        });
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // Called by 2094 - Blind
class spell_rog_airborne_irritant : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.AirborneIrritant, SpellIds.BlindArea);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.AirborneIrritant);
    }

    void HandleHit(uint effIndex)
    {
        GetCaster().CastSpell(GetHitUnit(), SpellIds.BlindArea, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    public override void Register()
    {
        OnEffectHit.Add(new(HandleHit, 0, SpellEffectName.ApplyAura));
    }
}

[Script] // 427773 - Blind
class spell_rog_airborne_irritant_target_selection : SpellScript
{
    void FilterTargets(List<WorldObject> targets)
    {
        targets.Remove(GetExplTargetWorldObject());
    }

    public override void Register()
    {
        OnObjectAreaTargetSelect.Add(new(FilterTargets, SpellConst.EffectAll, Targets.UnitDestAreaEnemy));
    }
}

[Script] // 53 - Backstab
class spell_rog_backstab : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((spellInfo.Id, 3));
    }

    void HandleHitDamage(uint effIndex)
    {
        Unit hitUnit = GetHitUnit();
        if (hitUnit == null)
            return;

        Unit caster = GetCaster();
        if (hitUnit.IsInBack(caster))
        {
            float currDamage = (float)GetHitDamage();
            float newDamage = MathFunctions.AddPct(ref currDamage, (float)GetEffectInfo(3).CalcValue(caster));
            SetHitDamage((int)newDamage);
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleHitDamage, 1, SpellEffectName.SchoolDamage));
    }
}

// 379005 - Blackjack
[Script] // Called by Sap - 6770 and Blind - 2094
class spell_rog_blackjack : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.BlackjackTalent, SpellIds.Blackjack);
    }

    void EffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit caster = GetCaster();
        if (caster != null)
            if (caster.HasAura(SpellIds.BlackjackTalent))
                caster.CastSpell(GetTarget(), SpellIds.Blackjack, true);
    }

    public override void Register()
    {
        AfterEffectRemove.Add(new(EffectRemove, 0, AuraType.Any, AuraEffectHandleModes.Real));
    }
}

[Script] // 13877, 33735, (check 51211, 65956) - Blade Flurry
class spell_rog_blade_flurry : AuraScript
{
    Unit _procTarget;

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.BladeFlurryExtraAttack);
    }

    bool CheckProc(ProcEventInfo eventInfo)
    {
        _procTarget = GetTarget().SelectNearbyTarget(eventInfo.GetProcTarget());
        return _procTarget != null && eventInfo.GetDamageInfo() != null;
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        DamageInfo damageInfo = eventInfo.GetDamageInfo();
        if (damageInfo != null)
        {
            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)damageInfo.GetDamage());
            GetTarget().CastSpell(_procTarget, SpellIds.BladeFlurryExtraAttack, args);
        }
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
        if (m_scriptSpellId == SpellIds.BladeFlurry)
            OnEffectProc.Add(new(HandleProc, 0, AuraType.ModPowerRegenPercent));
        else
            OnEffectProc.Add(new(HandleProc, 0, AuraType.ModMeleeHaste));
    }
}

[Script] // 31230 - Cheat Death
class spell_rog_cheat_death : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.CheatDeathDummy, SpellIds.CheatedDeath, SpellIds.CheatingDeath)
            && ValidateSpellEffect((spellInfo.Id, 1));
    }

    void HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
    {
        Unit target = GetTarget();
        if (target.HasAura(SpellIds.CheatedDeath))
        {
            absorbAmount = 0;
            return;
        }

        PreventDefaultAction();

        target.CastSpell(target, SpellIds.CheatDeathDummy, TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
        target.CastSpell(target, SpellIds.CheatedDeath, TriggerCastFlags.DontReportCastError);
        target.CastSpell(target, SpellIds.CheatingDeath, TriggerCastFlags.DontReportCastError);

        target.SetHealth(target.CountPctFromMaxHealth(GetEffectInfo(1).CalcValue(target)));
    }

    public override void Register()
    {
        OnEffectAbsorb.Add(new(HandleAbsorb, 0));
    }
}

[Script] // 382515 - Cloaked in Shadows (attached to 1856 - Vanish)
class spell_rog_cloaked_in_shadows : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.CloakedInShadowsAbsorb)
            && ValidateSpellEffect((SpellIds.CloakedInShadowsTalent, 0));
    }

    public override bool Load()
    {
        return GetCaster().HasAuraEffect(SpellIds.CloakedInShadowsTalent, 0);
    }

    void HandleCloakedInShadows()
    {
        Unit caster = GetCaster();

        AuraEffect cloakedInShadows = caster.GetAuraEffect(SpellIds.CloakedInShadowsTalent, 0);
        if (cloakedInShadows == null)
            return;

        int amount = (int)caster.CountPctFromMaxHealth(cloakedInShadows.GetAmount());

        caster.CastSpell(caster, SpellIds.CloakedInShadowsAbsorb, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell(),
            SpellValueOverrides = { new(SpellValueMod.BasePoint0, amount) }
        });
    }

    public override void Register()
    {
        AfterCast.Add(new(HandleCloakedInShadows));
    }
}

[Script] // 2818 - Deadly Poison
class spell_rog_deadly_poison : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.DeadlyPoisonInstantDamage);
    }

    void HandleInstantDamage(uint effIndex)
    {
        Unit caster = GetCaster();
        Unit target = GetHitUnit();
        if (target.HasAura(GetSpellInfo().Id, caster.GetGUID()))
        {
            caster.CastSpell(target, SpellIds.DeadlyPoisonInstantDamage, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError
            });
        }
    }

    public override void Register()
    {
        OnEffectLaunchTarget.Add(new(HandleInstantDamage, 0, SpellEffectName.ApplyAura));
    }
}

[Script] // 185314 - Deepening Shadows
class spell_rog_deepening_shadows : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ShadowDance);
    }

    bool CheckProc(AuraEffect aurEff, ProcEventInfo procEvent)
    {
        Spell procSpell = procEvent.GetProcSpell();
        if (procSpell != null)
            return procSpell.GetPowerTypeCostAmount(PowerType.ComboPoints) > 0;

        return false;
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        TimeSpan amount = -TimeSpan.FromSeconds(aurEff.GetAmount()) * procInfo.GetProcSpell().GetPowerTypeCostAmount(PowerType.ComboPoints).Value;
        GetTarget().GetSpellHistory().ModifyChargeRecoveryTime(Global.SpellMgr.GetSpellInfo(SpellIds.ShadowDance, GetCastDifficulty()).ChargeCategoryId, amount / 10);
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.Dummy));
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 32645 - Envenom
class spell_rog_envenom : SpellScript
{
    void CalculateDamage(SpellEffectInfo spellEffectInfo, Unit victim, ref int damage, ref int flatMod, ref float pctMod)
    {
        pctMod *= GetSpell().GetPowerTypeCostAmount(PowerType.ComboPoints).GetValueOrDefault(0);

        AuraEffect t5 = GetCaster().GetAuraEffect(SpellIds.T5_2PSetBonus, 0);
        if (t5 != null)
            flatMod += t5.GetAmount();
    }

    public override void Register()
    {
        CalcDamage.Add(new(CalculateDamage));
    }
}

[Script] // 196819 - Eviscerate
class spell_rog_eviscerate : SpellScript
{
    void CalculateDamage(SpellEffectInfo spellEffectInfo, Unit victim, ref int damage, ref int flatMod, ref float pctMod)
    {
        pctMod *= GetSpell().GetPowerTypeCostAmount(PowerType.ComboPoints).GetValueOrDefault(0);

        AuraEffect t5 = GetCaster().GetAuraEffect(SpellIds.T5_2PSetBonus, 0);
        if (t5 != null)
            flatMod += t5.GetAmount();
    }

    public override void Register()
    {
        CalcDamage.Add(new(CalculateDamage));
    }
}

[Script] // 193358 - Grand Melee
class spell_rog_grand_melee : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.SliceAndDice);
    }

    bool HandleCheckProc(ProcEventInfo eventInfo)
    {
        Spell procSpell = eventInfo.GetProcSpell();
        return procSpell != null && procSpell.HasPowerTypeCost(PowerType.ComboPoints);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        Spell procSpell = procInfo.GetProcSpell();
        int amount = aurEff.GetAmount() * procSpell.GetPowerTypeCostAmount(PowerType.ComboPoints).Value * 1000;

        Unit target = GetTarget();
        if (target != null)
        {
            Aura aura = target.GetAura(SpellIds.SliceAndDice);
            if (aura != null)
                aura.SetDuration(aura.GetDuration() + amount);
            else
            {
                CastSpellExtraArgs args = new();
                args.TriggerFlags = TriggerCastFlags.FullMask;
                args.AddSpellMod(SpellValueMod.Duration, amount);
                target.CastSpell(target, SpellIds.SliceAndDice, args);
            }
        }
    }

    public override void Register()
    {
        DoCheckProc.Add(new(HandleCheckProc));
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

// 198031 - Honor Among Thieves
[Script] /// 7.1.5
class spell_rog_honor_among_thieves : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.HonorAmongThievesEnergize);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        Unit target = GetTarget();
        target.CastSpell(target, SpellIds.HonorAmongThievesEnergize, aurEff);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // Called by 1784 - Stealth
class spell_rog_improved_garrote : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ImprovedGarroteAfterStealth, SpellIds.ImprovedGarroteStealth, SpellIds.ImprovedGarroteTalent);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.ImprovedGarroteTalent);
    }

    void HandleBuff(uint spellToCast, uint auraToRemove)
    {
        Unit target = GetTarget();

        target.RemoveAurasDueToSpell(auraToRemove);
        target.CastSpell(target, spellToCast, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError
        });

    }

    void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        HandleBuff(SpellIds.ImprovedGarroteStealth, SpellIds.ImprovedGarroteAfterStealth);
    }

    void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        HandleBuff(SpellIds.ImprovedGarroteAfterStealth, SpellIds.ImprovedGarroteStealth);
    }

    public override void Register()
    {
        AfterEffectApply.Add(new(HandleEffectApply, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
        AfterEffectRemove.Add(new(HandleEffectRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

[Script] // 703 - Garrote
class spell_rog_improved_garrote_damage : AuraScript
{
    float _pctMod = 1.0f;

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ImprovedGarroteAfterStealth, SpellIds.ImprovedGarroteStealth, SpellIds.ImprovedGarroteTalent);
    }

    void CalculateBonus(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
    {
        _pctMod = 1.0f;
        Unit caster = GetCaster();
        if (caster == null)
            return;

        AuraEffect improvedGarroteStealth = caster.GetAuraEffect(SpellIds.ImprovedGarroteAfterStealth, 1);
        if (improvedGarroteStealth != null)
            MathFunctions.AddPct(ref _pctMod, improvedGarroteStealth.GetAmount());
        else
        {
            AuraEffect improvedGarroteAfterStealth = caster.GetAuraEffect(SpellIds.ImprovedGarroteStealth, 1);
            if (improvedGarroteAfterStealth != null)
                MathFunctions.AddPct(ref _pctMod, improvedGarroteAfterStealth.GetAmount());
        }
    }

    void CalculateDamage(AuraEffect aurEff, Unit victim, ref int damage, ref int flatMod, ref float pctMod)
    {
        pctMod *= _pctMod;
    }

    public override void Register()
    {
        DoEffectCalcAmount.Add(new(CalculateBonus, 0, AuraType.PeriodicDamage));
        DoEffectCalcDamageAndHealing.Add(new(CalculateDamage, 0, AuraType.PeriodicDamage));
    }
}

[Script] // 5938 - Shiv
class spell_rog_improved_shiv : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ShivNatureDamage);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.ImprovedShiv);
    }

    void HandleDummy(uint effIndex)
    {
        GetCaster().CastSpell(GetHitUnit(), SpellIds.ShivNatureDamage, new CastSpellExtraArgs()
            .SetTriggerFlags(TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError)
            .SetTriggeringSpell(GetSpell()));
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.SchoolDamage));
    }
}

[Script] // 51690 - Killing Spree
class spell_rog_killing_spree_aura : AuraScript
{
    List<ObjectGuid> _targets = new();

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.KillingSpreeTeleport, SpellIds.KillingSpreeWeaponDmg, SpellIds.KillingSpreeDmgBuff);
    }

    void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        GetTarget().CastSpell(GetTarget(), SpellIds.KillingSpreeDmgBuff, true);
    }

    void HandleEffectPeriodic(AuraEffect aurEff)
    {
        while (!_targets.Empty())
        {
            ObjectGuid guid = _targets.SelectRandom();
            Unit target = Global.ObjAccessor.GetUnit(GetTarget(), guid);
            if (target != null)
            {
                GetTarget().CastSpell(target, SpellIds.KillingSpreeTeleport, true);
                GetTarget().CastSpell(target, SpellIds.KillingSpreeWeaponDmg, true);
                break;
            }
            else
                _targets.Remove(guid);
        }
    }

    void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        GetTarget().RemoveAurasDueToSpell(SpellIds.KillingSpreeDmgBuff);
    }

    public override void Register()
    {
        AfterEffectApply.Add(new(HandleApply, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real));
        OnEffectPeriodic.Add(new(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
        AfterEffectRemove.Add(new(HandleRemove, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real));
    }

    public
        void AddTarget(Unit target)
    {
        _targets.Add(target.GetGUID());
    }
}

[Script]
class spell_rog_killing_spree : SpellScript
{
    void FilterTargets(List<WorldObject> targets)
    {
        if (targets.Empty() || GetCaster().GetVehicleBase() != null)
            FinishCast(SpellCastResult.OutOfRange);
    }

    void HandleDummy(uint effIndex)
    {
        Aura aura = GetCaster().GetAura(SpellIds.KillingSpree);
        if (aura != null)
        {
            spell_rog_killing_spree_aura script = aura.GetScript<spell_rog_killing_spree_aura>();
            if (script != null)
                script.AddTarget(GetHitUnit());
        }
    }

    public override void Register()
    {
        OnObjectAreaTargetSelect.Add(new(FilterTargets, 1, Targets.UnitDestAreaEnemy));
        OnEffectHitTarget.Add(new(HandleDummy, 1, SpellEffectName.Dummy));
    }
}

[Script] // 385627 - Kingsbane
class spell_rog_kingsbane : AuraScript
{
    bool CheckProc(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        return procInfo.GetActionTarget().HasAura(GetId(), GetCasterGUID());
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckProc, 4, AuraType.ProcTriggerSpell)); ;
    }
}

[Script] // 76806 - Mastery: Main Gauche
class spell_rog_mastery_main_gauche : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.MainGauche);
    }

    bool HandleCheckProc(ProcEventInfo eventInfo)
    {
        return eventInfo.GetDamageInfo() != null && eventInfo.GetDamageInfo().GetVictim() != null;
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        Unit target = GetTarget();
        if (target != null)
            target.CastSpell(procInfo.GetDamageInfo().GetVictim(), SpellIds.MainGauche, aurEff);
    }

    public override void Register()
    {
        DoCheckProc.Add(new(HandleCheckProc));
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 277953 - Night Terrors (attached to 197835 - Shuriken Storm)
class spell_rog_night_terrors : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.NightTerrors, SpellIds.ShadowsGrasp);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.NightTerrors);
    }

    void HandleEnergize(uint effIndex)
    {
        GetCaster().CastSpell(GetHitUnit(), SpellIds.ShadowsGrasp, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleEnergize, 0, SpellEffectName.SchoolDamage));
    }
}

[Script]
class spell_rog_pickpocket : SpellScript
{
    SpellCastResult CheckCast()
    {
        if (GetExplTargetUnit() == null || !GetCaster().IsValidAttackTarget(GetExplTargetUnit(), GetSpellInfo()))
            return SpellCastResult.BadTargets;

        return SpellCastResult.SpellCastOk;
    }

    public override void Register()
    {
        OnCheckCast.Add(new(CheckCast));
    }
}

[Script] // 185565 - Poisoned Knife
class spell_rog_poisoned_knife : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo
        (
            SpellIds.WoundPoison,
            SpellIds.WoundPoisonDebuff,
            SpellIds.DeadlyPoison,
            SpellIds.DeadlyPoisonDebuff,
            SpellIds.AmplifyingPoison,
            SpellIds.AmplifyingPoisonDebuff,
            SpellIds.CripplingPoison,
            SpellIds.CripplingPoisonDebuff,
            SpellIds.NumbingPoison,
            SpellIds.NumbingPoisonDebuff,
            SpellIds.InstantPoison,
            SpellIds.InstantPoisonDamage,
            SpellIds.AtrophicPoison,
            SpellIds.AtrophicPoisonDebuff
       );
    }

    void HandleHit(uint effIndex)
    {
        Unit caster = GetCaster();
        foreach (var (poisonAura, debuffSpellId) in SpellIds.PoisonAuraToDebuff)
        {
            if (caster.HasAura(poisonAura))
            {
                caster.CastSpell(GetHitUnit(), debuffSpellId, new CastSpellExtraArgs()
                {
                    TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                    TriggeringSpell = GetSpell()
                });
            }
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleHit, 0, SpellEffectName.SchoolDamage));
    }
}

[Script] // Called by 1784 - Stealth
class spell_rog_premeditation : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.PremeditationPassive, SpellIds.PremeditationAura);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.PremeditationPassive);
    }

    void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        GetCaster().CastSpell(GetCaster(), SpellIds.PremeditationAura, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringAura = aurEff
        });
    }

    public override void Register()
    {
        AfterEffectApply.Add(new(HandleEffectApply, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

[Script] // 343173 - Premeditation (proc)
class spell_rog_premeditation_proc : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.PremeditationEnergize);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        GetCaster().CastSpell(GetCaster(), SpellIds.PremeditationEnergize, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringAura = aurEff
        });
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

// 131511 - Prey on the Weak
[Script] // Called by Cheap Shot - 1833 and Kidney Shot - 408
class spell_rog_prey_on_the_weak : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.PreyOnTheWeakTalent, SpellIds.PreyOnTheWeak);
    }

    void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit caster = GetCaster();
        if (caster != null)
            if (caster.HasAura(SpellIds.PreyOnTheWeakTalent))
                caster.CastSpell(GetTarget(), SpellIds.PreyOnTheWeak, true);
    }

    public override void Register()
    {
        AfterEffectApply.Add(new(OnApply, 0, AuraType.ModStun, AuraEffectHandleModes.Real));
    }
}

[Script] // 79096 - Restless Blades
class spell_rog_restless_blades : AuraScript
{
    static uint[] Spells = [SpellIds.AdrenalineRush, SpellIds.BetweenTheEyes, SpellIds.Sprint, SpellIds.GrapplingHook, SpellIds.Vanish, SpellIds.KillingSpree, SpellIds.MarkedForDeath];

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(Spells);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        var spentCp = procInfo.GetProcSpell()?.GetPowerTypeCostAmount(PowerType.ComboPoints);
        if (spentCp.HasValue)
        {
            int cdExtra = -(int)((float)(aurEff.GetAmount() * spentCp.Value) * 0.1f);

            SpellHistory history = GetTarget().GetSpellHistory();
            foreach (uint spellId in Spells)
                history.ModifyCooldown(spellId, TimeSpan.FromSeconds(cdExtra), true);
        }
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 315508 - Roll the Bones
class spell_rog_roll_the_bones : SpellScript
{
    static uint[] Spells = [SpellIds.SkullAndCrossbones, SpellIds.GrandMelee, SpellIds.RuthlessPrecision, SpellIds.TrueBearing, SpellIds.BuriedTreasure, SpellIds.Broadside];

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(Spells);
    }

    void HandleDummy(uint effIndex)
    {
        int currentDuration = 0;
        foreach (uint spellId in Spells)
        {
            Aura aura = GetCaster().GetAura(spellId);
            if (aura != null)
            {
                currentDuration = aura.GetDuration();
                GetCaster().RemoveAura(aura);
            }
        }

        List<uint> possibleBuffs = new(Spells);
        possibleBuffs.RandomShuffle();

        // https://www.icy-veins.com/wow/outlaw-rogue-pve-dps-rotation-cooldowns-abilities
        // 1 Roll the Bones buff  : 100.0 % chance;
        // 2 Roll the Bones buffs : 19 % chance;
        // 5 Roll the Bones buffs : 1 % chance.
        int chance = RandomHelper.IRand(1, 100);
        int numBuffs = 1;
        if (chance <= 1)
            numBuffs = 5;
        else if (chance <= 20)
            numBuffs = 2;

        for (int i = 0; i < numBuffs; ++i)
        {
            uint spellId = possibleBuffs[i];
            CastSpellExtraArgs args = new();
            args.TriggerFlags = TriggerCastFlags.FullMask;
            args.AddSpellMod(SpellValueMod.Duration, GetSpellInfo().GetDuration() + currentDuration);
            GetCaster().CastSpell(GetCaster(), spellId, args);
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.ApplyAura));
    }
}

[Script] // 1943 - Rupture
class spell_rog_rupture : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.VenomousWounds);
    }

    void OnEffectRemoved(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Death)
            return;

        Aura aura = GetAura();
        Unit caster = aura.GetCaster();
        if (caster == null)
            return;

        Aura auraVenomousWounds = caster.GetAura(SpellIds.VenomousWounds);
        if (auraVenomousWounds == null)
            return;

        // Venomous Wounds: if unit dies while being affected by rupture, regain energy based on remaining duration
        var cost = GetSpellInfo().CalcPowerCost(PowerType.Energy, false, caster, GetSpellInfo().GetSchoolMask(), null);
        if (cost == null)
            return;

        float pct = (float)aura.GetDuration() / (float)aura.GetMaxDuration();
        int extraAmount = (int)((float)cost.Amount * pct);
        caster.ModifyPower(PowerType.Energy, extraAmount);
    }

    public override void Register()
    {
        OnEffectRemove.Add(new(OnEffectRemoved, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.Real));
    }
}

[Script] // 14161 - Ruthlessness
class spell_rog_ruthlessness : AuraScript
{
    void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        Unit target = GetTarget();

        var cost = procInfo.GetProcSpell()?.GetPowerTypeCostAmount(PowerType.ComboPoints);
        if (cost.HasValue)
            if (RandomHelper.randChance(aurEff.GetSpellEffectInfo().PointsPerResource * cost.Value))
                target.ModifyPower(PowerType.ComboPoints, 1);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 185438 - Shadowstrike
class spell_rog_shadowstrike : SpellScript
{
    bool _hasPremeditationAura;

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.PremeditationAura, SpellIds.SliceAndDice, SpellIds.PremeditationPassive)
            && ValidateSpellEffect((SpellIds.PremeditationPassive, 0));
    }

    SpellCastResult HandleCheckCast()
    {
        // Because the premeditation aura is removed when we're out of stealth,
        // when we reach HandleEnergize the aura won't be there, even if it was when player launched the spell
        _hasPremeditationAura = GetCaster().HasAura(SpellIds.PremeditationAura);
        return SpellCastResult.Success;
    }

    void HandleEnergize(uint effIndex)
    {
        Unit caster = GetCaster();
        if (_hasPremeditationAura)
        {
            if (caster.HasAura(SpellIds.SliceAndDice))
            {
                Aura premeditationPassive = caster.GetAura(SpellIds.PremeditationPassive);
                if (premeditationPassive != null)
                {
                    AuraEffect auraEff = premeditationPassive.GetEffect(1);
                    if (auraEff != null)
                        SetHitDamage(GetHitDamage() + auraEff.GetAmount());
                }
            }

            // Grant 10 seconds of slice and dice
            int duration = Global.SpellMgr.GetSpellInfo(SpellIds.PremeditationPassive, Difficulty.None).GetEffect(0).CalcValue(GetCaster());

            CastSpellExtraArgs args = new();
            args.TriggerFlags = TriggerCastFlags.FullMask;
            args.AddSpellMod(SpellValueMod.Duration, duration * Time.InMilliseconds);
            caster.CastSpell(caster, SpellIds.SliceAndDice, args);
        }
    }

    public override void Register()
    {
        OnCheckCast.Add(new(HandleCheckCast));
        OnEffectHitTarget.Add(new(HandleEnergize, 1, SpellEffectName.Energize));
    }
}

[Script] // Called by 1784 - Stealth and 185313 - Shadow Dance
class spell_rog_shadow_focus : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ShadowFocus, SpellIds.ShadowFocusEffect);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.ShadowFocus);
    }

    void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        GetTarget().CastSpell(GetTarget(), SpellIds.ShadowFocusEffect, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringAura = aurEff
        });
    }

    void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        GetTarget().RemoveAurasDueToSpell(SpellIds.ShadowFocusEffect);
    }

    public override void Register()
    {
        AfterEffectApply.Add(new(HandleEffectApply, 1, AuraType.Any, AuraEffectHandleModes.Real));
        AfterEffectRemove.Add(new(HandleEffectRemove, 1, AuraType.Any, AuraEffectHandleModes.Real));
    }
}

[Script] // 257505 - Shot in the Dark (attached to 1784 - Stealth and 185313 - Shadow Dance)
class spell_rog_shot_in_the_dark : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ShotInTheDarkTalent, SpellIds.ShotInTheDarkBuff);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.ShotInTheDarkTalent);
    }

    void HandleAfterCast()
    {
        Unit caster = GetCaster();
        caster.CastSpell(caster, SpellIds.ShotInTheDarkBuff, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    public override void Register()
    {
        AfterCast.Add(new(HandleAfterCast));
    }
}

[Script] // 257506 - Shot in the Dark (attached to 185422 - Shadow Dance and 158185 - Stealth)
class spell_rog_shot_in_the_dark_buff : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ShotInTheDarkBuff);
    }

    void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        GetTarget().RemoveAurasDueToSpell(SpellIds.ShotInTheDarkBuff);
    }

    public override void Register()
    {
        AfterEffectRemove.Add(new(HandleEffectRemove, 0, AuraType.Any, AuraEffectHandleModes.Real));
    }
}

[Script] // 197835 - Shuriken Storm
class spell_rog_shuriken_storm : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ShurikenStormEnergize);
    }

    void HandleEnergize(uint effIndex)
    {
        GetCaster().CastSpell(GetCaster(), SpellIds.ShurikenStormEnergize, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell(),
            SpellValueOverrides = { new(SpellValueMod.BasePoint0, (int)GetUnitTargetCountForEffect(effIndex)) }
        });
    }

    public override void Register()
    {
        OnEffectHit.Add(new(HandleEnergize, 0, SpellEffectName.SchoolDamage));
    }
}

[Script] // 277925 - Shuriken Tornado
class spell_rog_shuriken_tornado : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ShurikenStormDamage);
    }

    void HandlePeriodicEffect(AuraEffect aurEff)
    {
        GetTarget().CastSpell(null, SpellIds.ShurikenStormDamage, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnorePowerCost | TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringAura = aurEff
        });
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(HandlePeriodicEffect, 0, AuraType.PeriodicTriggerSpell));
    }
}

[Script] // 193315 - Sinister Strike
class spell_rog_sinister_strike : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.T5_2PSetBonus);
    }

    void HandleDummy(uint effIndex)
    {
        int damagePerCombo = GetHitDamage();
        AuraEffect t5 = GetCaster().GetAuraEffect(SpellIds.T5_2PSetBonus, 0);
        if (t5 != null)
            damagePerCombo += t5.GetAmount();

        int finalDamage = damagePerCombo;
        var comboPointCost = GetSpell().GetPowerTypeCostAmount(PowerType.ComboPoints);
        if (comboPointCost.HasValue)
            finalDamage *= comboPointCost.Value;

        SetHitDamage(finalDamage);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 2, SpellEffectName.Dummy));
    }
}

[Script] // Called by 1856 - Vanish
class spell_rog_soothing_darkness : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.SoothingDarknessTalent, SpellIds.SoothingDarknessHeal);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.SoothingDarknessTalent);
    }

    void Heal()
    {
        GetCaster().CastSpell(GetCaster(), SpellIds.SoothingDarknessHeal, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    public override void Register()
    {
        AfterCast.Add(new(Heal));
    }
}

[Script] // 1784 - Stealth
class spell_rog_stealth : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.Sanctuary, SpellIds.StealthStealthAura, SpellIds.StealthShapeshiftAura);
    }

    void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();

        target.CastSpell(target, SpellIds.Sanctuary, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringAura = aurEff
        });
        target.CastSpell(target, SpellIds.StealthStealthAura, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringAura = aurEff
        });
        target.CastSpell(target, SpellIds.StealthShapeshiftAura, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringAura = aurEff
        });
    }

    void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();

        target.RemoveAurasDueToSpell(SpellIds.StealthStealthAura);
        target.RemoveAurasDueToSpell(SpellIds.StealthShapeshiftAura);
    }

    public override void Register()
    {
        AfterEffectApply.Add(new(HandleEffectApply, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
        AfterEffectRemove.Add(new(HandleEffectRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

[Script] // 212283 - Symbols of Death
class spell_rog_symbols_of_death : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.SymbolsOfDeathRANK2, SpellIds.SymbolsOfDeathCritAura);
    }

    void HandleEffectHitTarget(uint effIndex)
    {
        if (GetCaster().HasAura(SpellIds.SymbolsOfDeathRANK2))
            GetCaster().CastSpell(GetCaster(), SpellIds.SymbolsOfDeathCritAura, true);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleEffectHitTarget, 0, SpellEffectName.ApplyAura));
    }
}

[Script] // 57934 - Tricks of the Trade
class spell_rog_tricks_of_the_trade_aura : AuraScript
{
    ObjectGuid _redirectTarget;

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.TricksOfTheTradeProc);
    }

    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Default || !GetTarget().HasAura(SpellIds.TricksOfTheTradeProc))
            GetTarget().GetThreatManager().UnregisterRedirectThreat(SpellIds.TricksOfTheTrade);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        Unit rogue = GetTarget();
        if (Global.ObjAccessor.GetUnit(rogue, _redirectTarget) != null)
            rogue.CastSpell(rogue, SpellIds.TricksOfTheTradeProc, aurEff);
        Remove(AuraRemoveMode.Default);
    }

    public override void Register()
    {
        AfterEffectRemove.Add(new(OnRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
        OnEffectProc.Add(new(HandleProc, 1, AuraType.Dummy));
    }

    public void SetRedirectTarget(ObjectGuid guid) { _redirectTarget = guid; }
}

[Script] // 57934 - Tricks of the Trade
class spell_rog_tricks_of_the_trade : SpellScript
{
    void DoAfterHit()
    {
        Aura aura = GetHitAura();
        if (aura != null)
        {
            spell_rog_tricks_of_the_trade_aura script = aura.GetScript<spell_rog_tricks_of_the_trade_aura>();
            if (script != null)
            {
                Unit explTarget = GetExplTargetUnit();
                if (explTarget != null)
                    script.SetRedirectTarget(explTarget.GetGUID());
                else
                    script.SetRedirectTarget(ObjectGuid.Empty);
            }
        }
    }

    public override void Register()
    {
        AfterHit.Add(new(DoAfterHit));
    }
}

[Script] // 59628 - Tricks of the Trade (Proc)
class spell_rog_tricks_of_the_trade_proc : AuraScript
{
    void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        GetTarget().GetThreatManager().UnregisterRedirectThreat(SpellIds.TricksOfTheTrade);
    }

    public override void Register()
    {
        AfterEffectRemove.Add(new(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

[Script] // 198020 - Turn the Tables (PvP Talent)
class spell_rog_turn_the_tables : AuraScript
{
    bool CheckForStun(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        return eventInfo.GetProcSpell() != null && eventInfo.GetProcSpell().GetSpellInfo().HasAura(AuraType.ModStun);
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckForStun, 0, AuraType.ProcTriggerSpell));
    }
}

[Script] // 198023 - Turn the Tables (periodic)
class spell_rog_turn_the_tables_periodic_check : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.TurnTheTablesBuff);
    }

    void CheckForStun(AuraEffect aurEff)
    {
        Unit target = GetTarget();
        if (!target.HasAuraType(AuraType.ModStun))
        {
            target.CastSpell(target, SpellIds.TurnTheTablesBuff, aurEff);
            PreventDefaultAction();
            Remove();
        }
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(CheckForStun, 0, AuraType.PeriodicDummy));
    }
}

[Script] // 1856 - Vanish - SpellIds.Vanish
class spell_rog_vanish : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.VanishAura, SpellIds.StealthShapeshiftAura);
    }

    void OnLaunchTarget(uint effIndex)
    {
        PreventHitDefaultEffect(effIndex);

        Unit target = GetHitUnit();

        target.RemoveAurasByType(AuraType.ModStalked);
        if (!target.IsPlayer())
            return;

        if (target.HasAura(SpellIds.VanishAura))
            return;

        target.CastSpell(target, SpellIds.VanishAura, TriggerCastFlags.FullMask);
        target.CastSpell(target, SpellIds.StealthShapeshiftAura, TriggerCastFlags.FullMask);
    }

    public override void Register()
    {
        OnEffectLaunchTarget.Add(new(OnLaunchTarget, 1, SpellEffectName.TriggerSpell));
    }
}

[Script] // 11327 - Vanish
class spell_rog_vanish_aura : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.Stealth);
    }

    void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        GetTarget().CastSpell(GetTarget(), SpellIds.Stealth, TriggerCastFlags.FullMask);
    }

    public override void Register()
    {
        AfterEffectRemove.Add(new(HandleEffectRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

[Script] // 79134 - Venomous Wounds
class spell_rog_venomous_wounds : AuraScript
{
    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        int extraEnergy = aurEff.GetAmount();
        GetTarget().ModifyPower(PowerType.Energy, extraEnergy);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 1, AuraType.Dummy));
    }
}
