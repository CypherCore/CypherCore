// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the Gnu General Public License. See License file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.Spells.Evoker;

struct SpellIds
{
    public const uint AzureEssenceBurst = 375721;
    public const uint BlastFurnace = 375510;
    public const uint BlessingOfTheBronzeDk = 381732;
    public const uint BlessingOfTheBronzeDh = 381741;
    public const uint BlessingOfTheBronzeDruid = 381746;
    public const uint BlessingOfTheBronzeEvoker = 381748;
    public const uint BlessingOfTheBronzeHunter = 381749;
    public const uint BlessingOfTheBronzeMage = 381750;
    public const uint BlessingOfTheBronzeMonk = 381751;
    public const uint BlessingOfTheBronzePaladin = 381752;
    public const uint BlessingOfTheBronzePriest = 381753;
    public const uint BlessingOfTheBronzeRogue = 381754;
    public const uint BlessingOfTheBronzeShaman = 381756;
    public const uint BlessingOfTheBronzeWarlock = 381757;
    public const uint BlessingOfTheBronzeWarrior = 381758;
    public const uint Burnout = 375802;
    public const uint CallOfYseraTalent = 373834;
    public const uint CallOfYsera = 373835;
    public const uint Causality = 375777;
    public const uint Disintegrate = 356995;
    public const uint EmeraldBlossomHeal = 355916;
    public const uint EnergizingFlame = 400006;
    public const uint EssenceBurst = 359618;
    public const uint FirestormDamage = 369374;
    public const uint EternitySurge = 359073;
    public const uint FireBreath = 357208;
    public const uint FireBreathDamage = 357209;
    public const uint GlideKnockback = 358736;
    public const uint Hover = 358267;
    public const uint LivingFlame = 361469;
    public const uint LivingFlameDamage = 361500;
    public const uint LivingFlameHeal = 361509;
    public const uint PanaceaHeal = 387763;
    public const uint PanaceaTalent = 387761;
    public const uint PermeatingChillTalent = 370897;
    public const uint PyreDamage = 357212;
    public const uint RubyEmbers = 365937;
    public const uint RubyEssenceBurst = 376872;
    public const uint ScouringFlame = 378438;
    public const uint Snapfire = 370818;
    public const uint SoarRacial = 369536;
    public const uint VerdantEmbraceHeal = 361195;
    public const uint VerdantEmbraceJump = 373514;

    public const uint LabelEvokerBlue = 1465;

    public const uint VisualKitEvokerVerdantEmbraceJump = 152557;

    public static uint[] CausalityAffectedEmpowerSpells = [EternitySurge, FireBreath];
}

[Script] // 362969 - Azure Strike (blue)
class spell_evo_azure_strike : SpellScript
{
    void FilterTargets(List<WorldObject> targets)
    {
        targets.Remove(GetExplTargetUnit());
        targets.RandomResize((uint)GetEffectInfo(0).CalcValue(GetCaster()) - 1);
        targets.Add(GetExplTargetUnit());
    }

    public override void Register()
    {
        OnObjectAreaTargetSelect.Add(new(FilterTargets, 1, Targets.UnitDestAreaEnemy));
    }
}

// 381732 - Blessing of the Bronze (Bronze)
// 381741 - Blessing of the Bronze (Bronze)
// 381746 - Blessing of the Bronze (Bronze)
// 381748 - Blessing of the Bronze (Bronze)
// 381749 - Blessing of the Bronze (Bronze)
// 381750 - Blessing of the Bronze (Bronze)
// 381751 - Blessing of the Bronze (Bronze)
// 381752 - Blessing of the Bronze (Bronze)
// 381753 - Blessing of the Bronze (Bronze)
// 381754 - Blessing of the Bronze (Bronze)
// 381756 - Blessing of the Bronze (Bronze)
// 381757 - Blessing of the Bronze (Bronze)
[Script] // 381758 - Blessing of the Bronze (Bronze)
class spell_evo_blessing_of_the_bronze : SpellScript
{
    void RemoveInvalidTargets(List<WorldObject> targets)
    {
        targets.RemoveAll(target =>
        {
            Unit unitTarget = target.ToUnit();
            if (unitTarget == null)
                return true;

            switch (GetSpellInfo().Id)
            {
                case SpellIds.BlessingOfTheBronzeDk: return unitTarget.GetClass() != Class.DeathKnight;
                case SpellIds.BlessingOfTheBronzeDh: return unitTarget.GetClass() != Class.DemonHunter;
                case SpellIds.BlessingOfTheBronzeDruid: return unitTarget.GetClass() != Class.Druid;
                case SpellIds.BlessingOfTheBronzeEvoker: return unitTarget.GetClass() != Class.Evoker;
                case SpellIds.BlessingOfTheBronzeHunter: return unitTarget.GetClass() != Class.Hunter;
                case SpellIds.BlessingOfTheBronzeMage: return unitTarget.GetClass() != Class.Mage;
                case SpellIds.BlessingOfTheBronzeMonk: return unitTarget.GetClass() != Class.Monk;
                case SpellIds.BlessingOfTheBronzePaladin: return unitTarget.GetClass() != Class.Paladin;
                case SpellIds.BlessingOfTheBronzePriest: return unitTarget.GetClass() != Class.Priest;
                case SpellIds.BlessingOfTheBronzeRogue: return unitTarget.GetClass() != Class.Rogue;
                case SpellIds.BlessingOfTheBronzeShaman: return unitTarget.GetClass() != Class.Shaman;
                case SpellIds.BlessingOfTheBronzeWarlock: return unitTarget.GetClass() != Class.Warlock;
                case SpellIds.BlessingOfTheBronzeWarrior: return unitTarget.GetClass() != Class.Warrior;
                default:
                    break;
            }
            return true;
        });
    }

    public override void Register()
    {
        OnObjectAreaTargetSelect.Add(new(RemoveInvalidTargets, SpellConst.EffectAll, Targets.UnitCasterAreaRaid));
    }
}

[Script] // 375801 - Burnout
class spell_evo_burnout : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.Burnout);
    }

    bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        return RandomHelper.randChance(aurEff.GetAmount());
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        eventInfo.GetActor().CastSpell(eventInfo.GetActor(), SpellIds.Burnout, TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.Dummy));
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 373834 - Call of Ysera (attached to 361195 - Verdant Embrace (Green))
class spell_evo_call_of_ysera : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.CallOfYseraTalent, SpellIds.CallOfYsera);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.CallOfYseraTalent);
    }

    void HandleCallOfYsera()
    {
        GetCaster().CastSpell(GetCaster(), SpellIds.CallOfYsera, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    public override void Register()
    {
        AfterCast.Add(new(HandleCallOfYsera));
    }
}

[Script] // Called by 356995 - Disintegrate (Blue)
class spell_evo_causality_disintegrate : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((SpellIds.Causality, 1));
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.Causality);
    }

    void OnTick(AuraEffect aurEff)
    {
        AuraEffect causality = GetCaster().GetAuraEffect(SpellIds.Causality, 0);
        if (causality != null)
        {
            foreach (uint spell in SpellIds.CausalityAffectedEmpowerSpells)
                GetCaster().GetSpellHistory().ModifyCooldown(spell, TimeSpan.FromSeconds(causality.GetAmount()));
        }
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(OnTick, 0, AuraType.PeriodicDamage));
    }
}

[Script] // Called by 357212 - Pyre (Red)
class spell_evo_causality_pyre : SpellScript
{
    static long TargetLimit = 5;

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((SpellIds.Causality, 1));
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.Causality);
    }

    void HandleCooldown()
    {
        AuraEffect causality = GetCaster().GetAuraEffect(SpellIds.Causality, 1);
        if (causality == null)
            return;

        TimeSpan cooldownReduction = TimeSpan.FromSeconds(Math.Min(GetUnitTargetCountForEffect(0), TargetLimit) * causality.GetAmount());
        foreach (uint spell in SpellIds.CausalityAffectedEmpowerSpells)
            GetCaster().GetSpellHistory().ModifyCooldown(spell, cooldownReduction);
    }

    public override void Register()
    {
        AfterCast.Add(new(HandleCooldown));
    }
}

[Script] // 370455 - Charged Blast
class spell_evo_charged_blast : AuraScript
{
    bool CheckProc(ProcEventInfo procInfo)
    {
        return procInfo.GetSpellInfo() != null && procInfo.GetSpellInfo().HasLabel(SpellIds.LabelEvokerBlue);
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
    }
}

// 355913 - Emerald Blossom (Green)
[Script] // Id - 23318
class at_evo_emerald_blossom(AreaTrigger areaTrigger) : AreaTriggerAI(areaTrigger)
{
    public override void OnRemove()
    {
        Unit caster = at.GetCaster();
        if (caster != null)
            caster.CastSpell(at.GetPosition(), SpellIds.EmeraldBlossomHeal, TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
    }
}

[Script] // 355916 - Emerald Blossom (Green)
class spell_evo_emerald_blossom_heal : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((spellInfo.Id, 1));
    }

    void FilterTargets(List<WorldObject> targets)
    {
        uint maxTargets = (uint)GetSpellInfo().GetEffect(1).CalcValue(GetCaster());
        SelectRandomInjuredTargets(targets, maxTargets, true);
    }

    public override void Register()
    {
        OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.UnitDestAreaAlly));
    }
}

// Called by 362969 - Azure Strike
// Called by 361469 - Living Flame (Red)
[Script("spell_evo_azure_essence_burst", SpellIds.AzureEssenceBurst)]
[Script("spell_evo_ruby_essence_burst", SpellIds.RubyEssenceBurst)]
class spell_evo_essence_burst_trigger(uint talentAuraId) : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(talentAuraId, SpellIds.EssenceBurst);
    }

    public override bool Load()
    {
        AuraEffect aurEff = GetCaster().GetAuraEffect(talentAuraId, 0);
        return aurEff != null && RandomHelper.randChance(aurEff.GetAmount());
    }

    void HandleEssenceBurst()
    {
        GetCaster().CastSpell(GetCaster(), SpellIds.EssenceBurst, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    public override void Register()
    {
        AfterCast.Add(new(HandleEssenceBurst));
    }
}

// 357208 Fire Breath (Red)
[Script] // 382266 Fire Breath (Red)
class spell_evo_fire_breath : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FireBreathDamage, SpellIds.BlastFurnace);
    }

    void OnComplete(int completedStageCount)
    {
        int dotTicks = 10 - (completedStageCount - 1) * 3;
        AuraEffect blastFurnace = GetCaster().GetAuraEffect(SpellIds.BlastFurnace, 0);
        if (blastFurnace != null)
            dotTicks += blastFurnace.GetAmount() / 2;

        GetCaster().CastSpell(GetCaster(), SpellIds.FireBreathDamage, new CastSpellExtraArgs()
            .SetTriggeringSpell(GetSpell())
            .SetTriggerFlags(TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError)
            .AddSpellMod(SpellValueModFloat.DurationPct, 100 * dotTicks)
            .SetCustomArg(completedStageCount));
    }

    public override void Register()
    {
        OnEmpowerCompleted.Add(new(OnComplete));
    }
}

[Script] // 357209 Fire Breath (Red)
class spell_evo_fire_breath_damage : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((spellInfo.Id, 2))
            && spellInfo.GetEffect(2).IsAura(AuraType.ModSilence); // validate we are removing the correct effect
    }

    void AddBonusUpfrontDamage(SpellEffectInfo spellEffectInfo, Unit victim, ref int damage, ref int flatMod, ref float pctMod)
    {
        var empowerLevel = (int)GetSpell().m_customArg;
        if (empowerLevel == 0)
            return;

        // damage is done after aura is applied, grab periodic amount
        AuraEffect fireBreath = victim.GetAuraEffect(GetSpellInfo().Id, 1, GetCaster().GetGUID());
        if (fireBreath != null)
            flatMod += (int)fireBreath.GetEstimatedAmount().GetValueOrDefault(fireBreath.GetAmount()) * (empowerLevel - 1) * 3;
    }

    void RemoveUnusedEffect(List<WorldObject> targets)
    {
        targets.Clear();
    }

    public override void Register()
    {
        CalcDamage.Add(new(AddBonusUpfrontDamage));
        OnObjectAreaTargetSelect.Add(new(RemoveUnusedEffect, 2, Targets.UnitConeCasterToDestEnemy));
    }
}

[Script] // 369372 - Firestorm (Red)
class at_evo_firestorm(AreaTrigger areaTrigger) : AreaTriggerAI(areaTrigger)
{
    TaskScheduler _scheduler = new();
    object _damageSpellCustomArg;

    public class extra_create_data
    {
        public float SnapshotDamageMultipliers = 1.0f;
    }

    public static extra_create_data GetOrCreateExtraData(Spell firestorm)
    {
        if (firestorm.m_customArg.GetType() != typeof(extra_create_data))
            firestorm.m_customArg = new extra_create_data();

        return (extra_create_data)firestorm.m_customArg;
    }

    public override void OnCreate(Spell creatingSpell)
    {
        _damageSpellCustomArg = creatingSpell.m_customArg;

        _scheduler.Schedule(TimeSpan.FromSeconds(0), task =>
        {
            TimeSpan period = TimeSpan.FromSeconds(2); // TimeSpan.FromSeconds(2), affected by haste
            Unit caster = at.GetCaster();
            if (caster != null)
            {
                period *= caster.m_unitData.ModCastingSpeed;
                caster.CastSpell(at.GetPosition(), SpellIds.FirestormDamage, new CastSpellExtraArgs()
                {
                    TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                    CustomArg = _damageSpellCustomArg
                });
            }

            task.Repeat(period);
        });
    }

    public override void OnUpdate(uint diff)
    {
        _scheduler.Update(diff);
    }
}

[Script] // 358733 - Glide (Racial)
class spell_evo_glide : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.GlideKnockback, SpellIds.Hover, SpellIds.SoarRacial);
    }

    SpellCastResult CheckCast()
    {
        Unit caster = GetCaster();

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

        caster.GetSpellHistory().StartCooldown(Global.SpellMgr.GetSpellInfo(SpellIds.Hover, GetCastDifficulty()), 0, null, false, TimeSpan.FromSeconds(250));
        caster.GetSpellHistory().StartCooldown(Global.SpellMgr.GetSpellInfo(SpellIds.SoarRacial, GetCastDifficulty()), 0, null, false, TimeSpan.FromSeconds(250));
    }

    public override void Register()
    {
        OnCheckCast.Add(new(CheckCast));
        OnCast.Add(new(HandleCast));
    }
}

[Script] // 361469 - Living Flame (Red)
class spell_evo_living_flame : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.LivingFlameDamage, SpellIds.LivingFlameHeal, SpellIds.EnergizingFlame);
    }

    void HandleHitTarget(uint effIndex)
    {
        Unit caster = GetCaster();
        Unit hitUnit = GetHitUnit();
        if (caster.IsValidAssistTarget(hitUnit))
            caster.CastSpell(hitUnit, SpellIds.LivingFlameHeal, true);
        else
            caster.CastSpell(hitUnit, SpellIds.LivingFlameDamage, true);
    }

    void HandleLaunchTarget(uint effIndex)
    {
        Unit caster = GetCaster();
        if (caster.IsValidAssistTarget(GetHitUnit()))
            return;

        AuraEffect auraEffect = caster.GetAuraEffect(SpellIds.EnergizingFlame, 0);
        if (auraEffect != null)
        {
            int manaCost = GetSpell().GetPowerTypeCostAmount(PowerType.Mana).GetValueOrDefault(0);
            if (manaCost != 0)
                GetCaster().ModifyPower(PowerType.Mana, MathFunctions.CalculatePct(manaCost, auraEffect.GetAmount()));
        }
    }


    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleHitTarget, 0, SpellEffectName.Dummy));
        OnEffectLaunchTarget.Add(new(HandleLaunchTarget, 0, SpellEffectName.Dummy));
    }
}

[Script] // 387761 Panacea (Green) (attached to 355913 - Emerald Blossom (Green) and 360995 - Verdant Embrace (Green))
class spell_evo_panacea : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.PanaceaTalent, SpellIds.PanaceaHeal);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.PanaceaTalent);
    }

    void HandlePanacea()
    {
        GetCaster().CastSpell(GetCaster(), SpellIds.PanaceaHeal, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    public override void Register()
    {
        AfterCast.Add(new(HandlePanacea));
    }
}

[Script] // 381773 - Permeating Chill
class spell_evo_permeating_chill : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.PermeatingChillTalent);
    }

    bool CheckProc(ProcEventInfo procInfo)
    {
        SpellInfo spellInfo = procInfo.GetSpellInfo();
        if (spellInfo == null)
            return false;

        if (!spellInfo.HasLabel(SpellIds.LabelEvokerBlue))
            return false;

        if (!procInfo.GetActor().HasAura(SpellIds.PermeatingChillTalent))
            if (!spellInfo.IsAffected(SpellFamilyNames.Evoker, new FlagArray128(0x40, 0, 0, 0))) // disintegrate
                return false;

        return true;
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
    }
}

[Script] // 393568 - Pyre
class spell_evo_pyre : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.PyreDamage);
    }

    void HandleDamage(uint effIndex)
    {
        GetCaster().CastSpell(GetHitUnit().GetPosition(), SpellIds.PyreDamage, true);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDamage, 0, SpellEffectName.Dummy));
    }
}

// 361500 Living Flame (Red)
[Script] // 361509 Living Flame (Red)
class spell_evo_ruby_embers : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.RubyEmbers)
            && ValidateSpellEffect((spellInfo.Id, 1))
            && spellInfo.GetEffect(1).IsEffect(SpellEffectName.ApplyAura)
            && spellInfo.GetEffect(1).ApplyAuraPeriod != 0;
    }

    public override bool Load()
    {
        return !GetCaster().HasAura(SpellIds.RubyEmbers);
    }

    void PreventPeriodic(ref WorldObject target)
    {
        target = null;
    }

    public override void Register()
    {
        OnObjectTargetSelect.Add(new(PreventPeriodic, 1, m_scriptSpellId == SpellIds.LivingFlameDamage ? Targets.UnitTargetEnemy : Targets.UnitTargetAlly));
    }
}

[Script] // 357209 Fire Breath (Red)
class spell_evo_scouring_flame : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ScouringFlame);
    }

    void HandleScouringFlame(List<WorldObject> targets)
    {
        if (!GetCaster().HasAura(SpellIds.ScouringFlame))
            targets.Clear();
    }

    void CalcDispelCount(uint effIndex)
    {
        int empowerLevel = (int)GetSpell().m_customArg;
        if (empowerLevel != 0)
            SetEffectValue(empowerLevel);
    }

    public override void Register()
    {
        OnObjectAreaTargetSelect.Add(new(HandleScouringFlame, 3, Targets.UnitConeCasterToDestEnemy));
        OnEffectHitTarget.Add(new(CalcDispelCount, 3, SpellEffectName.Dispel));
    }
}

[Script] // Called by 368847 - Firestorm (Red)
class spell_evo_snapfire : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((SpellIds.Snapfire, 1));
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.Snapfire);
    }

    public override void OnPrecast()
    {
        AuraEffect snapfire = GetCaster().GetAuraEffect(SpellIds.Snapfire, 1);
        if (snapfire != null)
            if (GetSpell().m_appliedMods.Contains(snapfire.GetBase()))
                MathFunctions.AddPct(ref at_evo_firestorm.GetOrCreateExtraData(GetSpell()).SnapshotDamageMultipliers, snapfire.GetAmount());
    }

    public override void Register() { }
}

[Script] // Called by 369374 - Firestorm (Red)
class spell_evo_snapfire_bonus_damage : SpellScript
{
    void CalculateDamageBonus(SpellEffectInfo spellEffectInfo, Unit victim, ref int damage, ref int flatMod, ref float pctMod)
    {
        if (GetSpell().m_customArg is at_evo_firestorm.extra_create_data bonus)
            pctMod *= bonus.SnapshotDamageMultipliers;
    }

    public override void Register()
    {
        CalcDamage.Add(new(CalculateDamageBonus));
    }
}

[Script] // 360995 - Verdant Embrace (Green)
class spell_evo_verdant_embrace : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.VerdantEmbraceHeal, SpellIds.VerdantEmbraceJump)
            && CliDB.SpellVisualKitStorage.HasRecord(SpellIds.VisualKitEvokerVerdantEmbraceJump);
    }

    void HandleLaunchTarget(uint effIndex)
    {
        Unit caster = GetCaster();
        Unit target = GetHitUnit();
        CastSpellExtraArgs args = new();
        args.SetTriggerFlags(TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
        args.SetTriggeringSpell(GetSpell());

        if (target != caster)
        {
            caster.CastSpell(target, SpellIds.VerdantEmbraceJump, args);
            caster.SendPlaySpellVisualKit(SpellIds.VisualKitEvokerVerdantEmbraceJump, 0, 0);
        }
        else
            caster.CastSpell(caster, SpellIds.VerdantEmbraceHeal, args);
    }

    public override void Register()
    {
        OnEffectLaunchTarget.Add(new(HandleLaunchTarget, 0, SpellEffectName.Dummy));
    }
}

[Script] // 396557 - Verdant Embrace
class spell_evo_verdant_embrace_trigger_heal : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.VerdantEmbraceHeal);
    }

    void HandleHitTarget(uint effIndex)
    {
        GetHitUnit().CastSpell(GetExplTargetUnit(), SpellIds.VerdantEmbraceHeal, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleHitTarget, 0, SpellEffectName.Dummy));
    }
}
