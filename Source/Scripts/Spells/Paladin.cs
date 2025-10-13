// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the Gnu General Public License. See License file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.DataStorage;
using Game.Entities;
using Game.Miscellaneous;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Spells.Paladin;

struct SpellIds
{
    public const uint AJustRewardHeal = 469413;
    public const uint ArdentDefenderHeal = 66235;
    public const uint ArtOfWarTriggered = 231843;
    public const uint AvengersShield = 31935;
    public const uint AvengingWrath = 31884;
    public const uint BeaconOfLight = 53563;
    public const uint BeaconOfLightHeal = 53652;
    public const uint BladeOfJustice = 184575;
    public const uint BladeOfVengeance = 403826;
    public const uint BlessingOfFreedom = 1044;
    public const uint BlindingLightEffect = 105421;
    public const uint ConcentractionAura = 19746;
    public const uint ConsecratedGroundPassive = 204054;
    public const uint ConsecratedGroundSlow = 204242;
    public const uint Consecration = 26573;
    public const uint ConsecrationDamage = 81297;
    public const uint ConsecrationProtectionAura = 188370;
    public const uint CrusadingStrikesEnergize = 406834;
    public const uint DivineAuxiliaryEnergize = 408386;
    public const uint DivineAuxiliaryTalent = 406158;
    public const uint DivinePurposeTriggered = 223819;
    public const uint DivineSteedHuman = 221883;
    public const uint DivineSteedDwarf = 276111;
    public const uint DivineSteedDraenei = 221887;
    public const uint DivineSteedDarkIronDwarf = 276112;
    public const uint DivineSteedBloodelf = 221886;
    public const uint DivineSteedTauren = 221885;
    public const uint DivineSteedZandalariTroll = 294133;
    public const uint DivineSteedLfDraenei = 363608;
    public const uint DivineStormDamage = 224239;
    public const uint EnduringLight = 40471;
    public const uint EnduringJudgement = 40472;
    public const uint ExecutionSentenceDamage = 387113;
    public const uint ExecutionSentence11_Seconds = 406919;
    public const uint ExecutionSentence8_Seconds = 386579;
    public const uint ExecutionersWill = 406940;
    public const uint EyeForAnEyeTriggered = 205202;
    public const uint FinalStand = 204077;
    public const uint FinalStandEffect = 204079;
    public const uint FinalVerdict = 383329;
    public const uint Forbearance = 25771;
    public const uint GuardianOfAncientKings = 86659;
    public const uint HammerOfJustice = 853;
    public const uint HammerOfTheRighteousAoe = 88263;
    public const uint HandOfSacrifice = 6940;
    public const uint HolyMending = 64891;
    public const uint HolyPowerArmor = 28790;
    public const uint HolyPowerAttackPower = 28791;
    public const uint HolyPowerSpellPower = 28793;
    public const uint HolyPowerMP5 = 28795;
    public const uint HolyPrismAreaBeamVisual = 121551;
    public const uint HolyPrismTargetAlly = 114871;
    public const uint HolyPrismTargetEnemy = 114852;
    public const uint HolyPrismTargetBeamVisual = 114862;
    public const uint HolyShock = 20473;
    public const uint HolyShockDamage = 25912;
    public const uint HolyShockHealing = 25914;
    public const uint HolyLight = 82326;
    public const uint InfusionOfLightEnergize = 356717;
    public const uint ImmuneShieldMarker = 61988; // Serverside
    public const uint ItemHealingTrance = 37706;
    public const uint JudgmentGainHolyPower = 220637;
    public const uint JudgmentHolyR3 = 231644;
    public const uint JudgmentHolyR3_Debuff = 214222;
    public const uint JudgmentProtRetR3 = 315867;
    public const uint LightHammerCosmetic = 122257;
    public const uint LightHammerDamage = 114919;
    public const uint LightHammerHealing = 119952;
    public const uint LightHammerPeriodic = 114918;
    public const uint RighteousDefenseTaunt = 31790;
    public const uint RighteousVerdictAura = 267611;
    public const uint SealOfRighteousness = 25742;
    public const uint ShieldOfTheRighteousArmor = 132403;
    public const uint ShieldOfVengeanceDamage = 184689;
    public const uint TemplarVerdictDamage = 224266;
    public const uint T30_2PHeartfireDamage = 408399;
    public const uint T30_2PHeartfireHeal = 408400;
    public const uint WakeOfAshesStun = 255941;
    public const uint ZealAura = 269571;

    //CovenantSpells
    public const uint AshenHallow = 316958;
    public const uint AshenHallowDamage = 317221;
    public const uint AshenHallowHeal = 317223;
    public const uint AshenHallowAllowHammer = 330382;

    public const uint VisualKitDivineStorm = 73892;
    public const uint VisualSpellHolyShockDamage = 83731;
    public const uint VisualSpellHolyShockDamageCrit = 83881;
    public const uint VisualSpellHolyShockHeal = 83732;
    public const uint VisualSpellHolyShockHealCrit = 83880;

    public const uint LabelPaladinT30_2PHeartfire = 2598;
}

[Script] // 469411 - A Just Reward
class spell_pal_a_just_reward : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.AJustRewardHeal);
    }

    void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        eventInfo.GetActor().CastSpell(eventInfo.GetActionTarget(), SpellIds.AJustRewardHeal, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringAura = aurEff
        });
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.Dummy));
    }
}

[Script] // 31850 - Ardent Defender
class spell_pal_ardent_defender : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ArdentDefenderHeal)
            && ValidateSpellEffect((spellInfo.Id, 1));
    }

    void HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
    {
        PreventDefaultAction();

        int targetHealthPercent = GetEffectInfo(1).CalcValue(GetTarget());
        ulong targetHealth = GetTarget().CountPctFromMaxHealth(targetHealthPercent);
        if (GetTarget().HealthBelowPct(targetHealthPercent))
        {
            // we are currently below desired health
            // absorb everything and heal up
            GetTarget().CastSpell(GetTarget(), SpellIds.ArdentDefenderHeal,
                new CastSpellExtraArgs(aurEff)
                .AddSpellMod(SpellValueMod.BasePoint0, (int)(targetHealth - GetTarget().GetHealth())));
        }
        else
        {
            // we are currently above desired health
            // just absorb enough to reach that percentage
            absorbAmount = (uint)(dmgInfo.GetDamage() - (GetTarget().GetHealth() - targetHealth));
        }

        Remove();
    }

    public override void Register()
    {
        OnEffectAbsorb.Add(new(HandleAbsorb, 2));
    }
}

[Script] // 267344 - Art of War
class spell_pal_art_of_war : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ArtOfWarTriggered, SpellIds.BladeOfJustice);
    }

    bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        return RandomHelper.randChance(aurEff.GetAmount());
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        GetTarget().GetSpellHistory().ResetCooldown(SpellIds.BladeOfJustice, true);
        GetTarget().CastSpell(GetTarget(), SpellIds.ArtOfWarTriggered, TriggerCastFlags.IgnoreCastInProgress);
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.Dummy));
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 19042 - Ashen Hallow
class areatrigger_pal_ashen_hallow(AreaTrigger areaTrigger) : AreaTriggerAI(areaTrigger)
{
    TimeSpan _refreshTimer;
    TimeSpan _period;

    void RefreshPeriod()
    {
        Unit caster = at.GetCaster();
        if (caster != null)
        {
            AuraEffect ashen = caster.GetAuraEffect(SpellIds.AshenHallow, 1);
            if (ashen != null)
                _period = TimeSpan.FromSeconds(ashen.GetPeriod());
        }
    }

    public override void OnCreate(Spell creatingSpell)
    {
        RefreshPeriod();
        _refreshTimer = _period;
    }

    public override void OnUpdate(uint diff)
    {
        _refreshTimer -= TimeSpan.FromSeconds(diff);

        while (_refreshTimer <= TimeSpan.Zero)
        {
            Unit caster = at.GetCaster();
            if (caster != null)
            {
                caster.CastSpell(at.GetPosition(), SpellIds.AshenHallowHeal);
                caster.CastSpell(at.GetPosition(), SpellIds.AshenHallowDamage);
            }

            RefreshPeriod();

            _refreshTimer += _period;
        }
    }

    public override void OnUnitEnter(Unit unit)
    {
        if (unit.GetGUID() == at.GetCasterGUID())
            unit.CastSpell(unit, SpellIds.AshenHallowAllowHammer, true);
    }

    public override void OnUnitExit(Unit unit)
    {
        if (unit.GetGUID() == at.GetCasterGUID())
            unit.RemoveAura(SpellIds.AshenHallowAllowHammer);
    }
}

[Script] // 248033 - Awakening
class spell_pal_awakening : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.AvengingWrath)
            && ValidateSpellEffect((spellInfo.Id, 1));
    }

    bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        return RandomHelper.randChance(aurEff.GetAmount());
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        TimeSpan extraDuration = TimeSpan.FromSeconds(0);
        AuraEffect durationEffect = GetEffect(1);
        if (durationEffect != null)
            extraDuration = TimeSpan.FromSeconds(durationEffect.GetAmount());

        Aura avengingWrath = GetTarget().GetAura(SpellIds.AvengingWrath);
        if (avengingWrath != null)
        {
            avengingWrath.SetDuration((int)(avengingWrath.GetDuration() + extraDuration.TotalMilliseconds));
            avengingWrath.SetMaxDuration((int)(avengingWrath.GetMaxDuration() + extraDuration.TotalMilliseconds));
        }
        else
            GetTarget().CastSpell(GetTarget(), SpellIds.AvengingWrath,
                new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.IgnoreSpellAndCategoryCD)
                    .SetTriggeringSpell(eventInfo.GetProcSpell())
                    .AddSpellMod(SpellValueMod.Duration, (int)extraDuration.TotalMilliseconds));
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.Dummy));
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // Called by 184575 - Blade of Justice
class spell_pal_blade_of_vengeance : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.BladeOfVengeance)
            && ValidateSpellEffect((spellInfo.Id, 2))
            && spellInfo.GetEffect(2).IsEffect(SpellEffectName.TriggerSpell);
    }

    public override bool Load()
    {
        return !GetCaster().HasAura(SpellIds.BladeOfVengeance);
    }

    void PreventProc(ref WorldObject target)
    {
        target = null;
    }

    public override void Register()
    {
        OnObjectTargetSelect.Add(new(PreventProc, 2, Targets.UnitTargetEnemy));
    }
}

[Script] // 404358 - Blade of Justice
class spell_pal_blade_of_vengeance_aoe_target_selector : SpellScript
{
    void RemoveExplicitTarget(List<WorldObject> targets)
    {
        targets.Remove(GetExplTargetWorldObject());
    }

    public override void Register()
    {
        OnObjectAreaTargetSelect.Add(new(RemoveExplicitTarget, 0, Targets.UnitDestAreaEnemy));
    }
}

// 1022 - Blessing of Protection
[Script] // 204018 - Blessing of Spellwarding
class spell_pal_blessing_of_protection : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.Forbearance, SpellIds.ImmuneShieldMarker) && spellInfo.ExcludeTargetAuraSpell == SpellIds.ImmuneShieldMarker;
    }

    void TriggerForbearance()
    {
        GetCaster().CastSpell(GetHitUnit(), SpellIds.Forbearance, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    public override void Register()
    {
        AfterHit.Add(new(TriggerForbearance));
    }
}

[Script] // 115750 - Blinding Light
class spell_pal_blinding_light : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.BlindingLightEffect);
    }

    void HandleDummy(uint effIndex)
    {
        GetCaster().CastSpell(GetHitUnit(), SpellIds.BlindingLightEffect, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 26573 - Consecration
class spell_pal_consecration : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo
        (SpellIds.ConsecrationDamage,
            // validate for areatrigger_pal_consecration
            SpellIds.ConsecrationProtectionAura,
            SpellIds.ConsecratedGroundPassive,
            SpellIds.ConsecratedGroundSlow
       );
    }

    void HandleEffectPeriodic(AuraEffect aurEff)
    {
        AreaTrigger at = GetTarget().GetAreaTrigger(SpellIds.Consecration);
        if (at != null)
            GetTarget().CastSpell(at.GetPosition(), SpellIds.ConsecrationDamage);
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
    }
}

// 26573 - Consecration
[Script] //  9228 - AreaTriggerId
class areatrigger_pal_consecration(AreaTrigger areaTrigger) : AreaTriggerAI(areaTrigger)
{
    public override void OnUnitEnter(Unit unit)
    {
        Unit caster = at.GetCaster();
        if (caster != null)
        {
            // 243597 is also being cast as protection, but CreateObject is not sent, either serverside areatrigger for this aura or unused - also no visual is seen
            if (unit == caster && caster.IsPlayer() && caster.ToPlayer().GetPrimarySpecialization() == ChrSpecialization.PaladinProtection)
                caster.CastSpell(caster, SpellIds.ConsecrationProtectionAura);

            if (caster.IsValidAttackTarget(unit))
                if (caster.HasAura(SpellIds.ConsecratedGroundPassive))
                    caster.CastSpell(unit, SpellIds.ConsecratedGroundSlow);
        }
    }

    public override void OnUnitExit(Unit unit)
    {
        if (at.GetCasterGUID() == unit.GetGUID())
            unit.RemoveAurasDueToSpell(SpellIds.ConsecrationProtectionAura, at.GetCasterGUID());

        unit.RemoveAurasDueToSpell(SpellIds.ConsecratedGroundSlow, at.GetCasterGUID());
    }
}

[Script] // 196926 - Crusader Might
class spell_pal_crusader_might : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.HolyShock);
    }

    void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        GetTarget().GetSpellHistory().ModifyCooldown(SpellIds.HolyShock, TimeSpan.FromSeconds(aurEff.GetAmount()));
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.Dummy));
    }
}

[Script] // 406833 - Crusading Strikes
class spell_pal_crusading_strikes : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.CrusadingStrikesEnergize);
    }

    void HandleEffectProc(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        if (GetStackAmount() == 2)
        {
            GetTarget().CastSpell(GetTarget(), SpellIds.CrusadingStrikesEnergize, aurEff);

            // this spell has weird proc order dependency set up in db2 data so we do removal manually
            Remove();
        }
    }

    public override void Register()
    {
        AfterEffectApply.Add(new(HandleEffectProc, 0, AuraType.ProcTriggerSpell, AuraEffectHandleModes.RealOrReapplyMask));
    }
}

[Script] // 406158 - Divine Auxiliary (attached to 343721 - Final Reckoning and 343527 - Execution Sentence)
class spell_pal_divine_auxiliary : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.DivineAuxiliaryEnergize, SpellIds.DivineAuxiliaryTalent);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.DivineAuxiliaryTalent);
    }

    void HandleEnergize()
    {
        Unit caster = GetCaster();
        caster.CastSpell(caster, SpellIds.DivineAuxiliaryEnergize, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    public override void Register()
    {
        AfterCast.Add(new(HandleEnergize));
    }
}

[Script] // 223817 - Divine Purpose
class spell_pal_divine_purpose : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.DivinePurposeTriggered);
    }

    bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        Spell procSpell = eventInfo.GetProcSpell();
        if (procSpell == null)
            return false;

        if (!procSpell.HasPowerTypeCost(PowerType.HolyPower))
            return false;

        return RandomHelper.randChance(aurEff.GetAmount());
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        eventInfo.GetActor().CastSpell(eventInfo.GetActor(), SpellIds.DivinePurposeTriggered,
            new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress).SetTriggeringSpell(eventInfo.GetProcSpell()));
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.Dummy));
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 642 - Divine Shield
class spell_pal_divine_shield : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FinalStand, SpellIds.FinalStandEffect, SpellIds.Forbearance, SpellIds.ImmuneShieldMarker)
            && spellInfo.ExcludeCasterAuraSpell == SpellIds.ImmuneShieldMarker;
    }

    void HandleFinalStand()
    {
        if (GetCaster().HasAura(SpellIds.FinalStand))
            GetCaster().CastSpell(null, SpellIds.FinalStandEffect, true);
    }

    void TriggerForbearance()
    {
        GetCaster().CastSpell(GetHitUnit(), SpellIds.Forbearance, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    public override void Register()
    {
        AfterCast.Add(new(HandleFinalStand));
        AfterHit.Add(new(TriggerForbearance));
    }
}

[Script] // 190784 - Divine Steed
class spell_pal_divine_steed : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.DivineSteedHuman, SpellIds.DivineSteedDwarf, SpellIds.DivineSteedDraenei, SpellIds.DivineSteedDarkIronDwarf, SpellIds.DivineSteedBloodelf,
            SpellIds.DivineSteedTauren, SpellIds.DivineSteedZandalariTroll, SpellIds.DivineSteedLfDraenei);
    }

    void HandleOnCast()
    {
        Unit caster = GetCaster();

        uint spellId = SpellIds.DivineSteedHuman;
        switch (caster.GetRace())
        {
            case Race.Human:
                spellId = SpellIds.DivineSteedHuman;
                break;
            case Race.Dwarf:
                spellId = SpellIds.DivineSteedDwarf;
                break;
            case Race.Draenei:
                spellId = SpellIds.DivineSteedDraenei;
                break;
            case Race.LightforgedDraenei:
                spellId = SpellIds.DivineSteedLfDraenei;
                break;
            case Race.DarkIronDwarf:
                spellId = SpellIds.DivineSteedDarkIronDwarf;
                break;
            case Race.BloodElf:
                spellId = SpellIds.DivineSteedBloodelf;
                break;
            case Race.Tauren:
                spellId = SpellIds.DivineSteedTauren;
                break;
            case Race.ZandalariTroll:
                spellId = SpellIds.DivineSteedZandalariTroll;
                break;
            default:
                break;
        }

        caster.CastSpell(caster, spellId, true);
    }

    public override void Register()
    {
        OnCast.Add(new(HandleOnCast));
    }
}

[Script] // 53385 - Divine Storm
class spell_pal_divine_storm : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return CliDB.SpellVisualKitStorage.HasRecord(SpellIds.VisualKitDivineStorm);
    }

    void HandleOnCast()
    {
        GetCaster().SendPlaySpellVisualKit(SpellIds.VisualKitDivineStorm, 0, 0);
    }

    public override void Register()
    {
        OnCast.Add(new(HandleOnCast));
    }
}

[Script] // 343527 - Execution Sentence
class spell_pal_execution_sentence : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ExecutionSentenceDamage, SpellIds.ExecutionersWill, SpellIds.ExecutionSentence11_Seconds, SpellIds.ExecutionSentence8_Seconds);
    }

    void HandleVisual(uint effIndex)
    {
        uint visualSpellId = GetCaster().HasAura(SpellIds.ExecutionersWill)
            ? SpellIds.ExecutionSentence11_Seconds
            : SpellIds.ExecutionSentence8_Seconds;
        GetCaster().CastSpell(GetHitUnit(), visualSpellId,
            new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringSpell = GetSpell()
            });
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleVisual, 0, SpellEffectName.ApplyAura));
    }
}

[Script]
class spell_pal_execution_sentence_aura : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((spellInfo.Id, 1))
            && spellInfo.GetEffect(1).IsAura();
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        DamageInfo damageInfo = eventInfo.GetDamageInfo();
        if (damageInfo != null)
            aurEff.ChangeAmount((int)(aurEff.GetAmount() + MathFunctions.CalculatePct(damageInfo.GetDamage(), GetEffect(1).GetAmount())));
    }

    void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        int amount = aurEff.GetAmount();
        if (amount == 0 || GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
            return;

        Unit caster = GetCaster();
        if (caster != null)
            caster.CastSpell(GetTarget(), SpellIds.ExecutionSentenceDamage, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringAura = aurEff,
                SpellValueOverrides = { new(SpellValueMod.BasePoint0, amount) }
            });
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        AfterEffectRemove.Add(new(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

[Script] // 205191 - Eye for an Eye
class spell_pal_eye_for_an_eye : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.EyeForAnEyeTriggered);
    }

    void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        GetTarget().CastSpell(eventInfo.GetActor(), SpellIds.EyeForAnEyeTriggered, true);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.Dummy));
    }
}

[Script] // 383328 - Final Verdict
class spell_pal_final_verdict : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FinalVerdict);
    }

    void HandleDummy(uint effIndex)
    {
        if (!RandomHelper.randChance(GetEffectValue()))
            return;

        Unit caster = GetCaster();
        caster.CastSpell(caster, SpellIds.FinalVerdict, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 1, SpellEffectName.Dummy));
    }
}

[Script] // 234299 - Fist of Justice
class spell_pal_fist_of_justice : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.HammerOfJustice);
    }

    bool CheckEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        Spell procSpell = eventInfo.GetProcSpell();
        if (procSpell != null)
            return procSpell.HasPowerTypeCost(PowerType.HolyPower);

        return false;
    }

    void HandleEffectProc(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        int value = aurEff.GetAmount() / 10;

        GetTarget().GetSpellHistory().ModifyCooldown(SpellIds.HammerOfJustice, TimeSpan.FromSeconds(-value));
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckEffectProc, 0, AuraType.Dummy));
        OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.Dummy));
    }
}

[Script] // -85043 - Grand Crusader
class spell_pal_grand_crusader : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.AvengersShield);
    }

    bool CheckProc(ProcEventInfo eventInfo)
    {
        return GetTarget().IsPlayer();
    }

    void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        GetTarget().GetSpellHistory().ResetCooldown(SpellIds.AvengersShield, true);
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
        OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.ProcTriggerSpell));
    }
}

[Script] // 54968 - Glyph of Holy Light
class spell_pal_glyph_of_holy_light : SpellScript
{
    void FilterTargets(List<WorldObject> targets)
    {
        uint maxTargets = GetSpellInfo().MaxAffectedTargets;

        if (targets.Count > maxTargets)
        {
            targets.Sort(new HealthPctOrderPred());
            targets.Resize(maxTargets);
        }
    }

    public override void Register()
    {
        OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.UnitDestAreaAlly));
    }
}

[Script] // 53595 - Hammer of the Righteous
class spell_pal_hammer_of_the_righteous : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ConsecrationProtectionAura, SpellIds.HammerOfTheRighteousAoe);
    }

    void HandleAoEHit(uint effIndex)
    {
        if (GetCaster().HasAura(SpellIds.ConsecrationProtectionAura))
            GetCaster().CastSpell(GetHitUnit(), SpellIds.HammerOfTheRighteousAoe);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleAoEHit, 0, SpellEffectName.SchoolDamage));
    }
}

[Script] // 6940 - Hand of Sacrifice
class spell_pal_hand_of_sacrifice : AuraScript
{
    int remainingAmount;

    public override bool Load()
    {
        Unit caster = GetCaster();
        if (caster != null)
        {
            remainingAmount = (int)caster.GetMaxHealth();
            return true;
        }
        return false;
    }

    void Split(AuraEffect aurEff, DamageInfo dmgInfo, uint splitAmount)
    {
        remainingAmount -= (int)splitAmount;

        if (remainingAmount <= 0)
        {
            GetTarget().RemoveAura(SpellIds.HandOfSacrifice);
        }
    }

    public override void Register()
    {
        OnEffectSplit.Add(new(Split, 0));
    }
}

[Script] // 54149 - Infusion of Light
class spell_pal_infusion_of_light : AuraScript
{
    static FlagArray128 HolyLightSpellClassMask = new(0, 0, 0x400);

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.InfusionOfLightEnergize);
    }

    bool CheckFlashOfLightProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        return eventInfo.GetProcSpell() != null && eventInfo.GetProcSpell().m_appliedMods.Find(p => p == GetAura()) != null;
    }

    bool CheckHolyLightProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        return eventInfo.GetSpellInfo() != null && eventInfo.GetSpellInfo().IsAffected(SpellFamilyNames.Paladin, HolyLightSpellClassMask);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        eventInfo.GetActor().CastSpell(eventInfo.GetActor(), SpellIds.InfusionOfLightEnergize,
           new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetTriggeringSpell(eventInfo.GetProcSpell()));
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckFlashOfLightProc, 0, AuraType.AddPctModifier));
        DoCheckEffectProc.Add(new(CheckFlashOfLightProc, 2, AuraType.AddFlatModifier));

        DoCheckEffectProc.Add(new(CheckHolyLightProc, 1, AuraType.Dummy));
        OnEffectProc.Add(new(HandleProc, 1, AuraType.Dummy));
    }
}

[Script] // 327193 - Moment of Glory
class spell_pal_moment_of_glory : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.AvengersShield);
    }

    void HandleOnHit()
    {
        GetCaster().GetSpellHistory().ResetCooldown(SpellIds.AvengersShield);
    }

    public override void Register()
    {
        OnHit.Add(new(HandleOnHit));
    }
}

[Script] // 20271/275779/275773 - Judgement (Retribution/Protection/Holy)
class spell_pal_judgment : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.JudgmentProtRetR3, SpellIds.JudgmentGainHolyPower, SpellIds.JudgmentHolyR3, SpellIds.JudgmentHolyR3_Debuff);
    }

    void HandleOnHit()
    {
        Unit caster = GetCaster();

        if (caster.HasSpell(SpellIds.JudgmentProtRetR3))
            caster.CastSpell(caster, SpellIds.JudgmentGainHolyPower, GetSpell());

        if (caster.HasSpell(SpellIds.JudgmentHolyR3))
            caster.CastSpell(GetHitUnit(), SpellIds.JudgmentHolyR3_Debuff, GetSpell());
    }

    public override void Register()
    {
        OnHit.Add(new(HandleOnHit));
    }
}

[Script] // 215661 - Justicar's Vengeance
class spell_pal_justicars_vengeance : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((spellInfo.Id, 1));
    }

    void HandleDamage(SpellEffectInfo spellEffectInfo, Unit victim, ref int damage, ref int flatMod, ref float pctMod)
    {
        if (victim.HasUnitState(UnitState.Stunned))
            MathFunctions.AddPct(ref pctMod, GetEffectInfo(1).CalcValue(GetCaster()));
    }

    public override void Register()
    {
        CalcDamage.Add(new(HandleDamage));
    }
}

[Script] // 114165 - Holy Prism
class spell_pal_holy_prism : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.HolyPrismTargetAlly, SpellIds.HolyPrismTargetEnemy, SpellIds.HolyPrismTargetBeamVisual);
    }

    void HandleDummy(uint effIndex)
    {
        if (GetCaster().IsFriendlyTo(GetHitUnit()))
            GetCaster().CastSpell(GetHitUnit(), SpellIds.HolyPrismTargetAlly, true);
        else
            GetCaster().CastSpell(GetHitUnit(), SpellIds.HolyPrismTargetEnemy, true);

        GetCaster().CastSpell(GetHitUnit(), SpellIds.HolyPrismTargetBeamVisual, true);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

// 114852 - Holy Prism (Damage)
[Script] // 114871 - Holy Prism (Heal)
class spell_pal_holy_prism_selector : SpellScript
{
    List<WorldObject> _sharedTargets = new();
    ObjectGuid _targetGuid;

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.HolyPrismTargetAlly, SpellIds.HolyPrismAreaBeamVisual);
    }

    void SaveTargetGuid(uint effIndex)
    {
        _targetGuid = GetHitUnit().GetGUID();
    }

    void FilterTargets(List<WorldObject> targets)
    {
        byte maxTargets = 5;

        if (targets.Count > maxTargets)
        {
            if (GetSpellInfo().Id == SpellIds.HolyPrismTargetAlly)
            {
                targets.Sort(new HealthPctOrderPred());
                targets.Resize(maxTargets);
            }
            else
                targets.RandomResize(maxTargets);
        }

        _sharedTargets = targets;
    }

    void ShareTargets(List<WorldObject> targets)
    {
        targets.Clear();
        targets.AddRange(_sharedTargets);
    }

    void HandleScript(uint effIndex)
    {
        Unit initialTarget = Global.ObjAccessor.GetUnit(GetCaster(), _targetGuid);
        if (initialTarget != null)
            initialTarget.CastSpell(GetHitUnit(), SpellIds.HolyPrismAreaBeamVisual, true);
    }

    public override void Register()
    {
        if (m_scriptSpellId == SpellIds.HolyPrismTargetEnemy)
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 1, Targets.UnitDestAreaAlly));
        else if (m_scriptSpellId == SpellIds.HolyPrismTargetAlly)
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 1, Targets.UnitDestAreaEnemy));

        OnObjectAreaTargetSelect.Add(new(ShareTargets, 2, Targets.UnitDestAreaEntry));

        OnEffectHitTarget.Add(new(SaveTargetGuid, 0, SpellEffectName.Any));
        OnEffectHitTarget.Add(new(HandleScript, 2, SpellEffectName.ScriptEffect));
    }
}

[Script] // 20473 - Holy Shock
class spell_pal_holy_shock : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.HolyShock, SpellIds.HolyShockHealing, SpellIds.HolyShockDamage);
    }

    SpellCastResult CheckCast()
    {
        Unit caster = GetCaster();

        Unit target = GetExplTargetUnit();
        if (target != null)
        {
            if (!caster.IsFriendlyTo(target))
            {
                if (!caster.IsValidAttackTarget(target))
                    return SpellCastResult.BadTargets;

                if (!caster.IsInFront(target))
                    return SpellCastResult.UnitNotInfront;
            }
        }
        else
            return SpellCastResult.BadTargets;

        return SpellCastResult.SpellCastOk;
    }

    void HandleDummy(uint effIndex)
    {
        Unit caster = GetCaster();

        Unit unitTarget = GetHitUnit();
        if (unitTarget != null)
        {
            if (caster.IsFriendlyTo(unitTarget))
                caster.CastSpell(unitTarget, SpellIds.HolyShockHealing, GetSpell());
            else
                caster.CastSpell(unitTarget, SpellIds.HolyShockDamage, GetSpell());
        }
    }

    public override void Register()
    {
        OnCheckCast.Add(new(CheckCast));
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 25912 - Holy Shock
class spell_pal_holy_shock_damage_visual : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return CliDB.SpellVisualStorage.HasRecord(SpellIds.VisualSpellHolyShockDamage)
            && CliDB.SpellVisualStorage.HasRecord(SpellIds.VisualSpellHolyShockDamageCrit);
    }

    void PlayVisual()
    {
        GetCaster().SendPlaySpellVisual(GetHitUnit(), IsHitCrit() ? SpellIds.VisualSpellHolyShockDamageCrit : SpellIds.VisualSpellHolyShockDamage, 0, 0, 0.0f, false);
    }

    public override void Register()
    {
        AfterHit.Add(new(PlayVisual));
    }
}

[Script] // 25914 - Holy Shock
class spell_pal_holy_shock_heal_visual : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return CliDB.SpellVisualStorage.HasRecord(SpellIds.VisualSpellHolyShockHeal)
            && CliDB.SpellVisualStorage.HasRecord(SpellIds.VisualSpellHolyShockHealCrit);
    }

    void PlayVisual()
    {
        GetCaster().SendPlaySpellVisual(GetHitUnit(), IsHitCrit() ? SpellIds.VisualSpellHolyShockHealCrit : SpellIds.VisualSpellHolyShockHeal, 0, 0, 0.0f, false);
    }

    public override void Register()
    {
        AfterHit.Add(new(PlayVisual));
    }
}

[Script] // 37705 - Healing Discount
class spell_pal_item_healing_discount : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ItemHealingTrance);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        GetTarget().CastSpell(GetTarget(), SpellIds.ItemHealingTrance, aurEff);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.ProcTriggerSpell));
    }
}

[Script] // 40470 - Paladin Tier 6 Trinket
class spell_pal_item_t6_trinket : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.EnduringLight, SpellIds.EnduringJudgement);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        SpellInfo spellInfo = eventInfo.GetSpellInfo();
        if (spellInfo == null)
            return;

        uint spellId;
        int chance;

        // Holy Light & Flash of Light
        if ((spellInfo.SpellFamilyFlags[0] & 0xC0000000) != 0)
        {
            spellId = SpellIds.EnduringLight;
            chance = 15;
        }
        // Judgements
        else if ((spellInfo.SpellFamilyFlags[0] & 0x00800000) != 0)
        {
            spellId = SpellIds.EnduringJudgement;
            chance = 50;
        }
        else
            return;

        if (RandomHelper.randChance(chance))
            eventInfo.GetActor().CastSpell(eventInfo.GetProcTarget(), spellId, aurEff);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

// 633 - Lay on Hands
[Script] // 471195 - Lay on Hands (from 387791 - Empyreal Ward)
class spell_pal_lay_on_hands : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.Forbearance)
            && spellInfo.ExcludeTargetAuraSpell == SpellIds.ImmuneShieldMarker;
    }

    void TriggerForbearance()
    {
        GetCaster().CastSpell(GetHitUnit(), SpellIds.Forbearance, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    public override void Register()
    {
        AfterHit.Add(new(TriggerForbearance));
    }
}

[Script] // 53651 - Light's Beacon - Beacon of Light
class spell_pal_light_s_beacon : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.BeaconOfLight, SpellIds.BeaconOfLightHeal);
    }

    bool CheckProc(ProcEventInfo eventInfo)
    {
        if (eventInfo.GetActionTarget() == null)
            return false;
        if (eventInfo.GetActionTarget().HasAura(SpellIds.BeaconOfLight, eventInfo.GetActor().GetGUID()))
            return false;
        return true;
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        HealInfo healInfo = eventInfo.GetHealInfo();
        if (healInfo == null || healInfo.GetHeal() == 0)
            return;

        uint heal = MathFunctions.CalculatePct(healInfo.GetHeal(), aurEff.GetAmount());

        var auras = GetCaster().GetSingleCastAuras();
        foreach (var aura in auras)
        {
            if (aura.GetId() == SpellIds.BeaconOfLight)
            {
                List<AuraApplication> applications = aura.GetApplicationList();
                if (!applications.Empty())
                {
                    CastSpellExtraArgs args = new(aurEff);
                    args.AddSpellMod(SpellValueMod.BasePoint0, (int)heal);
                    eventInfo.GetActor().CastSpell(applications.First().GetTarget(), SpellIds.BeaconOfLightHeal, args);
                }
                return;
            }
        }
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 122773 - Light's Hammer
class spell_pal_light_hammer_init_summon : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.LightHammerCosmetic, SpellIds.LightHammerPeriodic);
    }

    void InitSummon()
    {
        foreach (var summonedObject in GetSpell().GetExecuteLogEffect(SpellEffectName.Summon).GenericVictimTargets)
        {
            Unit hammer = Global.ObjAccessor.GetUnit(GetCaster(), summonedObject.Victim);
            if (hammer != null)
            {
                hammer.CastSpell(hammer, SpellIds.LightHammerCosmetic,
                   new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress).SetTriggeringSpell(GetSpell()));
                hammer.CastSpell(hammer, SpellIds.LightHammerPeriodic,
                   new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress).SetTriggeringSpell(GetSpell()));
            }
        }
    }

    public override void Register()
    {
        AfterCast.Add(new(InitSummon));
    }
}

[Script] // 114918 - Light's Hammer (Periodic)
class spell_pal_light_hammer_periodic : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.LightHammerHealing, SpellIds.LightHammerDamage);
    }

    void HandleEffectPeriodic(AuraEffect aurEff)
    {
        Unit lightHammer = GetTarget();
        Unit originalCaster = lightHammer.GetOwner();
        if (originalCaster != null)
        {
            originalCaster.CastSpell(lightHammer.GetPosition(), SpellIds.LightHammerDamage, TriggerCastFlags.IgnoreCastInProgress);
            originalCaster.CastSpell(lightHammer.GetPosition(), SpellIds.LightHammerHealing, TriggerCastFlags.IgnoreCastInProgress);
        }
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
    }
}

[Script] // 204074 - Righteous Protector
class spell_pal_righteous_protector : AuraScript
{
    SpellPowerCost _baseHolyPowerCost;

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.AvengingWrath, SpellIds.GuardianOfAncientKings);
    }

    bool CheckEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        SpellInfo procSpell = eventInfo.GetSpellInfo();
        if (procSpell != null)
            _baseHolyPowerCost = procSpell.CalcPowerCost(PowerType.HolyPower, false, eventInfo.GetActor(), eventInfo.GetSchoolMask());
        else
            _baseHolyPowerCost = null;

        return _baseHolyPowerCost != null;
    }

    void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        int value = aurEff.GetAmount() * 100 * _baseHolyPowerCost.Amount;

        GetTarget().GetSpellHistory().ModifyCooldown(SpellIds.AvengingWrath, TimeSpan.FromSeconds(-value));
        GetTarget().GetSpellHistory().ModifyCooldown(SpellIds.GuardianOfAncientKings, TimeSpan.FromSeconds(-value));
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckEffectProc, 0, AuraType.Dummy));
        OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.Dummy));
    }
}

[Script] // 267610 - Righteous Verdict
class spell_pal_righteous_verdict : AuraScript
{
    public override bool Validate(SpellInfo spellEntry)
    {
        return ValidateSpellInfo(SpellIds.RighteousVerdictAura);
    }

    void HandleEffectProc(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        procInfo.GetActor().CastSpell(procInfo.GetActor(), SpellIds.RighteousVerdictAura, true);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.Dummy));
    }
}

[Script] // 85804 - Selfless Healer
class spell_pal_selfless_healer : AuraScript
{
    bool CheckEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        Spell procSpell = eventInfo.GetProcSpell();
        if (procSpell != null)
            return procSpell.HasPowerTypeCost(PowerType.HolyPower);

        return false;
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckEffectProc, 0, AuraType.ProcTriggerSpell));
    }
}

[Script] // 53600 - Shield of the Righteous
class spell_pal_shield_of_the_righteous : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ShieldOfTheRighteousArmor);
    }

    void HandleArmor()
    {
        GetCaster().CastSpell(GetCaster(), SpellIds.ShieldOfTheRighteousArmor, true);
    }

    public override void Register()
    {
        AfterCast.Add(new(HandleArmor));
    }
}

[Script] // 184662 - Shield of Vengeance
class spell_pal_shield_of_vengeance : AuraScript
{
    int _initialAmount = 0;

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ShieldOfVengeanceDamage) && ValidateSpellEffect((spellInfo.Id, 1));
    }

    void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
    {
        amount = (int)MathFunctions.CalculatePct(GetUnitOwner().GetMaxHealth(), GetEffectInfo(1).CalcValue());
        Player player = GetUnitOwner().ToPlayer();
        if (player != null)
            MathFunctions.AddPct(ref amount, player.GetRatingBonusValue(CombatRating.VersatilityDamageDone) + player.GetTotalAuraModifier(AuraType.ModVersatility));

        _initialAmount = amount;
    }

    void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        GetTarget().CastSpell(GetTarget(), SpellIds.ShieldOfVengeanceDamage,
           new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, _initialAmount - aurEff.GetAmount()));
    }

    public override void Register()
    {
        DoEffectCalcAmount.Add(new(CalculateAmount, 0, AuraType.SchoolAbsorb));
        OnEffectRemove.Add(new(HandleRemove, 0, AuraType.SchoolAbsorb, AuraEffectHandleModes.Real));
    }
}

[Script] // 469304 - Steed of Liberty
class spell_pal_steed_of_liberty : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.BlessingOfFreedom);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        Unit target = GetTarget();
        target.CastSpell(target, SpellIds.BlessingOfFreedom, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringAura = aurEff,
            SpellValueOverrides = { new(SpellValueMod.Duration, aurEff.GetAmount()) }
        });
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 85256 - Templar's Verdict
class spell_pal_templar_s_verdict : SpellScript
{
    public override bool Validate(SpellInfo spellEntry)
    {
        return ValidateSpellInfo(SpellIds.TemplarVerdictDamage);
    }

    void HandleHitTarget(uint effIndex)
    {
        GetCaster().CastSpell(GetHitUnit(), SpellIds.TemplarVerdictDamage, true);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleHitTarget, 0, SpellEffectName.Dummy));
    }
}

[Script] // 28789 - Holy Power
class spell_pal_t3_6p_bonus : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.HolyPowerArmor, SpellIds.HolyPowerAttackPower, SpellIds.HolyPowerSpellPower, SpellIds.HolyPowerMP5);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        uint spellId;
        Unit caster = eventInfo.GetActor();
        Unit target = eventInfo.GetProcTarget();

        switch (target.GetClass())
        {
            case Class.Paladin:
            case Class.Priest:
            case Class.Shaman:
            case Class.Druid:
                spellId = SpellIds.HolyPowerMP5;
                break;
            case Class.Mage:
            case Class.Warlock:
                spellId = SpellIds.HolyPowerSpellPower;
                break;
            case Class.Hunter:
            case Class.Rogue:
                spellId = SpellIds.HolyPowerAttackPower;
                break;
            case Class.Warrior:
                spellId = SpellIds.HolyPowerArmor;
                break;
            default:
                return;
        }

        caster.CastSpell(target, spellId, aurEff);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 64890 - Item - Paladin T8 Holy 2P Bonus
class spell_pal_t8_2p_bonus : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.HolyMending);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        HealInfo healInfo = eventInfo.GetHealInfo();
        if (healInfo == null || healInfo.GetHeal() == 0)
            return;

        Unit caster = eventInfo.GetActor();
        Unit target = eventInfo.GetProcTarget();

        SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.HolyMending, GetCastDifficulty());
        int amount = MathFunctions.CalculatePct((int)(healInfo.GetHeal()), aurEff.GetAmount());

        Cypher.Assert(spellInfo.GetMaxTicks() > 0);
        amount /= (int)spellInfo.GetMaxTicks();

        CastSpellExtraArgs args = new(aurEff);
        args.AddSpellMod(SpellValueMod.BasePoint0, amount);
        caster.CastSpell(target, SpellIds.HolyMending, args);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 405547 - Paladin Protection 10.1 Class Set 2pc
class spell_pal_t30_2p_protection_bonus : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.T30_2PHeartfireDamage);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        PreventDefaultAction();

        Unit caster = procInfo.GetActor();
        uint ticks = Global.SpellMgr.GetSpellInfo(SpellIds.T30_2PHeartfireDamage, Difficulty.None).GetMaxTicks();
        uint damage = MathFunctions.CalculatePct(procInfo.GetDamageInfo().GetOriginalDamage(), aurEff.GetAmount()) / ticks;

        caster.CastSpell(procInfo.GetActionTarget(), SpellIds.T30_2PHeartfireDamage, new CastSpellExtraArgs(aurEff)
            .SetTriggeringSpell(procInfo.GetProcSpell())
            .AddSpellMod(SpellValueMod.BasePoint0, (int)damage));
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 1, AuraType.Dummy));
    }
}

[Script] // 408461 - Heartfire
class spell_pal_t30_2p_protection_bonus_heal : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.T30_2PHeartfireHeal);
    }

    bool CheckProc(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        return procInfo.GetDamageInfo() != null && procInfo.GetSpellInfo() != null && procInfo.GetSpellInfo().HasLabel(SpellIds.LabelPaladinT30_2PHeartfire);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        GetTarget().CastSpell(GetTarget(), SpellIds.T30_2PHeartfireHeal, new CastSpellExtraArgs(aurEff)
            .SetTriggeringSpell(procInfo.GetProcSpell())
            .AddSpellMod(SpellValueMod.BasePoint0, (int)procInfo.GetDamageInfo().GetOriginalDamage()));
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.Dummy));
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 255937 - Wake of Ashes
class spell_pal_wake_of_ashes : SpellScript
{
    public override bool Validate(SpellInfo spellEntry)
    {
        return ValidateSpellInfo(SpellIds.WakeOfAshesStun);
    }

    void HandleHitTarget(uint effIndex)
    {
        Unit target = GetHitUnit();

        if (target.GetCreatureType() == CreatureType.Demon || target.GetCreatureType() == CreatureType.Undead)
        {
            GetCaster().CastSpell(target, SpellIds.WakeOfAshesStun, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringSpell = GetSpell()
            });
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleHitTarget, 0, SpellEffectName.SchoolDamage));
    }
}

[Script] // 269569 - Zeal
class spell_pal_zeal : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ZealAura);
    }

    void HandleEffectProc(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        Unit target = GetTarget();
        target.CastSpell(target, SpellIds.ZealAura, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.AuraStack, aurEff.GetAmount()));

        PreventDefaultAction();
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.ProcTriggerSpell));
    }
}

