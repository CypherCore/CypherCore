// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the Gnu General Public License. See License file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Spells.DeathKnight;

struct SpellIds
{
    public const uint AntiMagicBarrier = 205727;
    public const uint ArmyFleshBeastTransform = 127533;
    public const uint ArmyGeistTransform = 127534;
    public const uint ArmyNorthrendSkeletonTransform = 127528;
    public const uint ArmySkeletonTransform = 127527;
    public const uint ArmySpikedGhoulTransform = 127525;
    public const uint ArmySuperZombieTransform = 127526;
    public const uint BlindingSleetSlow = 317898;
    public const uint Blood = 137008;
    public const uint BlooddrinkerDebuff = 458687;
    public const uint BloodPlague = 55078;
    public const uint BloodShieldAbsorb = 77535;
    public const uint BloodShieldMastery = 77513;
    public const uint BoneShield = 195181;
    public const uint BreathOfSindragosa = 152279;
    public const uint BrittleDebuff = 374557;
    public const uint CleavingStrikes = 316916;
    public const uint CorpseExplosionTriggered = 43999;
    public const uint CrimsonScourgeBuff = 81141;
    public const uint DarkSimulacrumBuff = 77616;
    public const uint DarkSimulacrumSpellpowerBuff = 94984;
    public const uint DeathAndDecay = 43265;
    public const uint DeathAndDecayDamage = 52212;
    public const uint DeathAndDecayIncreaseTargets = 188290;
    public const uint DeathCoilDamage = 47632;
    public const uint DeathGripDummy = 243912;
    public const uint DeathGripJump = 49575;
    public const uint DeathGripTaunt = 51399;
    public const uint DeathStrikeEnabler = 89832; // Server Side
    public const uint DeathStrikeHeal = 45470;
    public const uint DeathStrikeOffhand = 66188;
    public const uint FesteringWound = 194310;
    public const uint Frost = 137006;
    public const uint FrostFever = 55095;
    public const uint FrostScythe = 207230;
    public const uint FrostShield = 207203;
    public const uint GlyphOfFoulMenagerie = 58642;
    public const uint GlyphOfTheGeist = 58640;
    public const uint GlyphOfTheSkeleton = 146652;
    public const uint GorefiendsGrasp = 108199;
    public const uint HeartbreakerEnergize = 210738;
    public const uint HeartbreakerTalent = 221536;
    public const uint IcePrisonRoot = 454787;
    public const uint IcePrisonTalent = 454786;
    public const uint KillingMachineProc = 51124;
    public const uint MarkOfBloodHeal = 206945;
    public const uint NecrosisEffect = 216974;
    public const uint Obliteration = 281238;
    public const uint ObliterationRuneEnergize = 281327;
    public const uint PillarOfFrost = 51271;
    public const uint RaiseDeadSummon = 52150;
    public const uint ReaperOfSoulsProc = 469172;
    public const uint RecentlyUsedDeathStrike = 180612;
    public const uint RunicCorruption = 51460;
    public const uint RunicPowerEnergize = 49088;
    public const uint RunicReturn = 61258;
    public const uint SanguineGroundTalent = 391458;
    public const uint SanguineGround = 391459;
    public const uint SludgeBelcher = 207313;
    public const uint SludgeBelcherSummon = 212027;
    public const uint SmotheringOffense = 435005;
    public const uint SoulReaper = 343294;
    public const uint SoulReaperDamage = 343295;
    public const uint SubduingGraspDebuff = 454824;
    public const uint SubduingGraspTalent = 454822;
    public const uint Unholy = 137007;
    public const uint UnholyVigor = 196263;
    public const uint DhVoraciousLeech = 274009;
    public const uint DhVoraciousTalent = 273953;
}

[Script] // 70656 - Advantage (T10 4P Melee Bonus)
class spell_dk_advantage_t10_4p : AuraScript
{
    bool CheckProc(ProcEventInfo eventInfo)
    {
        Unit caster = eventInfo.GetActor();
        if (caster != null)
        {
            Player player = caster.ToPlayer();
            if (player == null || caster.GetClass() != Class.DeathKnight)
                return false;

            for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
                if (player.GetRuneCooldown(i) == 0)
                    return false;

            return true;
        }

        return false;
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
    }
}

[Script] // 48707 - Anti-Magic Shell
class spell_dk_anti_magic_shell : AuraScript
{
    int absorbPct;
    int maxHealth;
    uint absorbedAmount;

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.RunicPowerEnergize)
            && ValidateSpellEffect((spellInfo.Id, 1), (SpellIds.AntiMagicBarrier, 2));
    }

    public override bool Load()
    {
        absorbPct = GetEffectInfo(1).CalcValue(GetCaster());
        maxHealth = (int)GetCaster().GetMaxHealth();
        absorbedAmount = 0;
        return true;
    }

    void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
    {
        amount = MathFunctions.CalculatePct(maxHealth, absorbPct);
        AuraEffect antiMagicBarrier = GetCaster().GetAuraEffect(SpellIds.AntiMagicBarrier, 2);
        if (antiMagicBarrier != null)

            MathFunctions.AddPct(ref amount, antiMagicBarrier.GetAmount());
        Player player = GetUnitOwner().ToPlayer();
        if (player != null)
            MathFunctions.AddPct(ref amount, player.GetRatingBonusValue(CombatRating.VersatilityDamageDone) + player.GetTotalAuraModifier(AuraType.ModVersatility));
    }

    void Trigger(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
    {
        absorbedAmount += absorbAmount;

        CastSpellExtraArgs args = new(aurEff);
        args.AddSpellMod(SpellValueMod.BasePoint0, (int)MathFunctions.CalculatePct(absorbAmount, 2 * absorbAmount * 100 / maxHealth));
        GetTarget().CastSpell(GetTarget(), SpellIds.RunicPowerEnergize, args);
    }

    public override void Register()
    {
        DoEffectCalcAmount.Add(new(CalculateAmount, 0, AuraType.SchoolAbsorb));
        AfterEffectAbsorb.Add(new(Trigger, 0));
    }
}

// 195182 - Marrowrend
// 195292 - Death's Caress
[Script("spell_dk_marrowrend_apply_bone_shield", 2)]
[Script("spell_dk_deaths_caress_apply_bone_shield", 2)]
class spell_dk_apply_bone_shield : SpellScript
{
    uint _effIndex;

    public spell_dk_apply_bone_shield(uint effIndex)
    {
        _effIndex = effIndex;
    }

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.BoneShield)
            && ValidateSpellEffect((spellInfo.Id, _effIndex))
            && spellInfo.GetEffect(_effIndex).CalcBaseValue(null, null, 0, 0) <= (int)Global.SpellMgr.GetSpellInfo(SpellIds.BoneShield, Difficulty.None).StackAmount;
    }

    void HandleHitTarget(uint effIndex)
    {
        Unit caster = GetCaster();
        for (int i = 0; i < GetEffectValue(); ++i)
            caster.CastSpell(caster, SpellIds.BoneShield, new CastSpellExtraArgs()
                .SetTriggerFlags(TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError)
                .SetTriggeringSpell(GetSpell()));
    }

    public override void Register()
    {
        OnEffectHit.Add(new(HandleHitTarget, _effIndex, SpellEffectName.Dummy));
    }
}

// 127517 - Army Transform
[Script] // 6.x, does this belong here or in spell_generic? where do we cast this? sniffs say this is only cast when caster has glyph of foul menagerie.
class spell_dk_army_transform : SpellScript
{
    static uint[] ArmyTransforms =
    {
        SpellIds.ArmyFleshBeastTransform,
        SpellIds.ArmyGeistTransform,
        SpellIds.ArmyNorthrendSkeletonTransform,
        SpellIds.ArmySkeletonTransform,
        SpellIds.ArmySpikedGhoulTransform,
        SpellIds.ArmySuperZombieTransform,
    };

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.GlyphOfFoulMenagerie);
    }

    public override bool Load()
    {
        return GetCaster().IsGuardian();
    }

    SpellCastResult CheckCast()
    {
        Unit owner = GetCaster().GetOwner();
        if (owner != null)
            if (owner.HasAura(SpellIds.GlyphOfFoulMenagerie))
                return SpellCastResult.SpellCastOk;

        return SpellCastResult.SpellUnavailable;
    }

    void HandleDummy(uint effIndex)
    {
        GetCaster().CastSpell(GetCaster(), ArmyTransforms.SelectRandom(), true);
    }

    public override void Register()
    {
        OnCheckCast.Add(new(CheckCast));
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 207167 - Blinding Sleet
class spell_dk_blinding_sleet : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.BlindingSleetSlow);
    }

    void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Expire)
            GetTarget().CastSpell(GetTarget(), SpellIds.BlindingSleetSlow, true);
    }

    public override void Register()
    {
        AfterEffectRemove.Add(new(HandleOnRemove, 0, AuraType.ModConfuse, AuraEffectHandleModes.Real));
    }
}

[Script] // 206931 - Blooddrinker
class spell_dk_blooddrinker : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.BlooddrinkerDebuff);
    }

    void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
            return;

        Unit caster = GetCaster();
        if (caster != null)
            caster.CastSpell(GetTarget(), SpellIds.BlooddrinkerDebuff, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringAura = aurEff
            });
    }

    public override void Register()
    {
        AfterEffectRemove.Add(new(AfterRemove, 0, AuraType.PeriodicLeech, AuraEffectHandleModes.Real));
    }
}

[Script] // 50842 - Blood Boil
class spell_dk_blood_boil : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.BloodPlague);
    }

    void HandleEffect()
    {
        GetCaster().CastSpell(GetHitUnit(), SpellIds.BloodPlague, true);
    }

    public override void Register()
    {
        OnHit.Add(new(HandleEffect));
    }
}

[Script] // 374504 - Brittle
class spell_dk_brittle : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.BrittleDebuff);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        GetTarget().CastSpell(eventInfo.GetActionTarget(), SpellIds.BrittleDebuff, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError
        });
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 81136 - Crimson Scourge
class spell_dk_crimson_scourge : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.BloodPlague, SpellIds.CrimsonScourgeBuff, SpellIds.DeathAndDecay);
    }

    bool CheckProc(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        return procInfo.GetProcTarget().HasAura(SpellIds.BloodPlague, procInfo.GetActor().GetGUID());
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        Unit actor = eventInfo.GetActor();
        actor.GetSpellHistory().RestoreCharge(Global.SpellMgr.GetSpellInfo(SpellIds.DeathAndDecay, Difficulty.None).ChargeCategoryId);
        actor.CastSpell(actor, SpellIds.CrimsonScourgeBuff, TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.Dummy));
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

// 49028 - Dancing Rune Weapon
[Script] // 7.1.5
class spell_dk_dancing_rune_weapon : AuraScript
{
    const uint NpcDkDancingRuneWeapon = 27893;

    public override bool Validate(SpellInfo spellInfo)
    {
        if (Global.ObjectMgr.GetCreatureTemplate(NpcDkDancingRuneWeapon) == null)
            return false;
        return true;
    }

    // This is a port of the old switch hack in Unit.cpp, it's not correct
    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        Unit caster = GetCaster();
        if (caster == null)
            return;

        Unit drw = null;
        foreach (Unit controlled in caster.m_Controlled)
        {
            if (controlled.GetEntry() == NpcDkDancingRuneWeapon)
            {
                drw = controlled;
                break;
            }
        }

        if (drw == null || drw.GetVictim() == null)
            return;

        SpellInfo spellInfo = eventInfo.GetSpellInfo();
        if (spellInfo == null)
            return;

        DamageInfo damageInfo = eventInfo.GetDamageInfo();
        if (damageInfo == null || damageInfo.GetDamage() == 0)
            return;

        int amount = (int)(damageInfo.GetDamage()) / 2;
        SpellNonMeleeDamage log = new(drw, drw.GetVictim(), spellInfo, new(spellInfo.GetSpellXSpellVisualId(drw), 0), spellInfo.GetSchoolMask());
        log.damage = (uint)amount;
        Unit.DealDamage(drw, drw.GetVictim(), (uint)amount, null, DamageEffectType.Direct, spellInfo.GetSchoolMask(), spellInfo, true);
        drw.SendSpellNonMeleeDamageLog(log);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 1, AuraType.Dummy));
    }
}

[Script] // 77606 - Dark Simulacrum
class spell_dk_dark_simulacrum : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.DarkSimulacrumBuff, SpellIds.DarkSimulacrumSpellpowerBuff);
    }

    bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        Spell procSpell = eventInfo.GetProcSpell();
        if (procSpell == null)
            return false;

        if (!GetTarget().IsPlayer())
            return procSpell.GetSpellInfo().HasAttribute(SpellAttr9.AllowDarkSimulacrum);

        if (!procSpell.HasPowerTypeCost(PowerType.Mana))
            return false;

        // filter out spells not castable by mind controlled players (teleports, summons, item creations (healthstones))
        if (procSpell.GetSpellInfo().HasAttribute(SpellAttr1.NoAutocastAi))
            return false;

        return true;
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        Unit caster = GetCaster();
        if (caster == null)
            return;

        caster.CastSpell(caster, SpellIds.DarkSimulacrumBuff, new CastSpellExtraArgs()
            .SetTriggerFlags(TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError)
            .SetTriggeringSpell(eventInfo.GetProcSpell())
            .AddSpellMod(SpellValueMod.BasePoint0, (int)eventInfo.GetSpellInfo().Id));

        caster.CastSpell(caster, SpellIds.DarkSimulacrumSpellpowerBuff, new CastSpellExtraArgs()
            .SetTriggerFlags(TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError)
            .SetTriggeringSpell(eventInfo.GetProcSpell())
            .AddSpellMod(SpellValueMod.BasePoint0, GetTarget().SpellBaseDamageBonusDone(SpellSchoolMask.Magic))
            .AddSpellMod(SpellValueMod.BasePoint1, (int)GetTarget().SpellBaseHealingBonusDone(SpellSchoolMask.Magic)));
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.Dummy));
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 77616 - Dark Simulacrum
class spell_dk_dark_simulacrum_buff : AuraScript
{
    bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        return aurEff.GetAmount() == eventInfo.GetSpellInfo().Id;
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.OverrideActionbarSpellsTriggered));
    }
}

[Script] // 43265 - Death and Decay (Aura)
class spell_dk_death_and_decay : AuraScript
{
    void HandleDummyTick(AuraEffect aurEff)
    {
        Unit caster = GetCaster();
        if (caster != null)
            caster.CastSpell(GetTarget(), SpellIds.DeathAndDecayDamage, aurEff);
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(HandleDummyTick, 2, AuraType.PeriodicDummy));
    }
}

[Script] // 47541 - Death Coil
class spell_dk_death_coil : SpellScript
{
    public override bool Validate(SpellInfo spell)
    {
        return ValidateSpellInfo(SpellIds.DeathCoilDamage, SpellIds.Unholy, SpellIds.UnholyVigor);
    }

    void HandleDummy(uint effIndex)
    {
        Unit caster = GetCaster();
        caster.CastSpell(GetHitUnit(), SpellIds.DeathCoilDamage, true);
        AuraEffect unholyAura = caster.GetAuraEffect(SpellIds.Unholy, 6);
        if (unholyAura != null) // can be any effect, just here to send SpellCastResult.DontReport on failure
            caster.CastSpell(caster, SpellIds.UnholyVigor, unholyAura);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 52751 - Death Gate
class spell_dk_death_gate : SpellScript
{
    SpellCastResult CheckClass()
    {
        if (GetCaster().GetClass() != Class.DeathKnight)
        {
            SetCustomCastResultMessage(SpellCustomErrors.MustBeDeathKnight);
            return SpellCastResult.CustomError;
        }

        return SpellCastResult.SpellCastOk;
    }

    void HandleScript(uint effIndex)
    {
        PreventHitDefaultEffect(effIndex);
        Unit target = GetHitUnit();
        if (target != null)
            target.CastSpell(target, (uint)GetEffectValue(), false);
    }

    public override void Register()
    {
        OnCheckCast.Add(new(CheckClass));
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script] // 49576 - Death Grip Initial
class spell_dk_death_grip_initial : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.DeathGripDummy, SpellIds.DeathGripJump, SpellIds.Blood, SpellIds.DeathGripTaunt);
    }

    SpellCastResult CheckCast()
    {
        Unit caster = GetCaster();
        // Death Grip should not be castable while jumping/falling
        if (caster.HasUnitState(UnitState.Jumping) || caster.HasUnitMovementFlag(MovementFlag.Falling))
            return SpellCastResult.Moving;

        return SpellCastResult.SpellCastOk;
    }

    void HandleDummy(uint effIndex)
    {
        GetCaster().CastSpell(GetHitUnit(), SpellIds.DeathGripDummy, true);
        GetHitUnit().CastSpell(GetCaster(), SpellIds.DeathGripJump, true);
        if (GetCaster().HasAura(SpellIds.Blood))
            GetCaster().CastSpell(GetHitUnit(), SpellIds.DeathGripTaunt, true);
    }


    public override void Register()
    {
        OnCheckCast.Add(new(CheckCast));
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.ScriptEffect));
    }
}

[Script] // 48743 - Death Pact
class spell_dk_death_pact : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((spellInfo.Id, 2));
    }

    void HandleCalcAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
    {
        Unit caster = GetCaster();
        if (caster != null)
            amount = (int)caster.CountPctFromMaxHealth(GetEffectInfo(2).CalcValue(caster));
    }

    public override void Register()
    {
        DoEffectCalcAmount.Add(new(HandleCalcAmount, 1, AuraType.SchoolHealAbsorb));
    }
}

[Script] // 49998 - Death Strike
class spell_dk_death_strike : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.DeathStrikeEnabler, SpellIds.DeathStrikeHeal, SpellIds.BloodShieldMastery, SpellIds.BloodShieldAbsorb, SpellIds.Frost, SpellIds.DeathStrikeOffhand)
            && ValidateSpellEffect((spellInfo.Id, 2));
    }

    void HandleDummy(uint effIndex)
    {
        Unit caster = GetCaster();
        AuraEffect enabler = caster.GetAuraEffect(SpellIds.DeathStrikeEnabler, 0, GetCaster().GetGUID());
        if (enabler != null)
        {
            // Heals you for 25% of all damage taken in the last 5 sec,
            int heal = MathFunctions.CalculatePct(enabler.CalculateAmount(GetCaster()), GetEffectInfo(1).CalcValue(GetCaster()));
            // minimum 7.0% of maximum health.
            int pctOfMaxHealth = MathFunctions.CalculatePct(GetEffectInfo(2).CalcValue(GetCaster()), caster.GetMaxHealth());
            heal = Math.Max(heal, pctOfMaxHealth);

            caster.CastSpell(caster, SpellIds.DeathStrikeHeal, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, heal));

            AuraEffect aurEff = caster.GetAuraEffect(SpellIds.BloodShieldMastery, 0);
            if (aurEff != null)
                caster.CastSpell(caster, SpellIds.BloodShieldAbsorb, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, MathFunctions.CalculatePct(heal, aurEff.GetAmount())));

            if (caster.HasAura(SpellIds.Frost))
                caster.CastSpell(GetHitUnit(), SpellIds.DeathStrikeOffhand, true);
        }
    }

    void TriggerRecentlyUsedDeathStrike()
    {
        GetCaster().CastSpell(GetCaster(), SpellIds.RecentlyUsedDeathStrike, true);
    }

    public override void Register()
    {
        OnEffectLaunch.Add(new(HandleDummy, 1, SpellEffectName.Dummy));
        AfterCast.Add(new(TriggerRecentlyUsedDeathStrike));
    }
}

[Script] // 89832 - Death Strike Enabler - SpellDeathStrikeEnabler
class spell_dk_death_strike_enabler : AuraScript
{
    // Amount of seconds we calculate damage over
    static byte LastSeconds = 5;

    uint[] _damagePerSecond = new uint[LastSeconds];

    bool CheckProc(ProcEventInfo eventInfo)
    {
        return eventInfo.GetDamageInfo() != null;
    }

    void Update(AuraEffect aurEff)
    {
        // Move backwards all datas by one from [23][0][0][0][0] . [0][23][0][0][0]
        _damagePerSecond = Enumerable.Range(1, _damagePerSecond.Length).Select(i => _damagePerSecond[i % _damagePerSecond.Length]).ToArray();
        _damagePerSecond[0] = 0;
    }

    void HandleCalcAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
    {
        canBeRecalculated = true;
        amount = (int)_damagePerSecond.Aggregate(0u, (a, b) => a += b);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        _damagePerSecond[0] += eventInfo.GetDamageInfo().GetDamage();
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
        OnEffectProc.Add(new(HandleProc, 0, AuraType.PeriodicDummy));
        DoEffectCalcAmount.Add(new(HandleCalcAmount, 0, AuraType.PeriodicDummy));
        OnEffectUpdatePeriodic.Add(new(Update, 0, AuraType.PeriodicDummy));
    }
}

[Script] // 85948 - Festering Strike
class spell_dk_festering_strike : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FesteringWound);
    }

    void HandleScriptEffect(uint effIndex)
    {
        GetCaster().CastSpell(GetHitUnit(), SpellIds.FesteringWound, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.AuraStack, GetEffectValue()));
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScriptEffect, 1, SpellEffectName.Dummy));
    }
}

[Script] // 195621 - Frost Fever
class spell_dk_frost_fever_proc : AuraScript
{
    bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        return RandomHelper.randChance(aurEff.GetAmount());
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.ProcTriggerSpell));
    }
}

[Script] // 47496 - Explode, Ghoul spell for Corpse Explosion
class spell_dk_ghoul_explode : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.CorpseExplosionTriggered) && ValidateSpellEffect((spellInfo.Id, 2));
    }

    void HandleDamage(uint effIndex)
    {
        SetHitDamage((int)GetCaster().CountPctFromMaxHealth(GetEffectInfo(2).CalcValue(GetCaster())));
    }

    void Suicide(uint effIndex)
    {
        Unit unitTarget = GetHitUnit();
        if (unitTarget != null)
        {
            // Corpse Explosion (Suicide)
            unitTarget.CastSpell(unitTarget, SpellIds.CorpseExplosionTriggered, true);
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDamage, 0, SpellEffectName.SchoolDamage));
        OnEffectHitTarget.Add(new(Suicide, 1, SpellEffectName.SchoolDamage));
    }
}

[Script] // 69961 - Glyph of Scourge Strike
class spell_dk_glyph_of_scourge_strike_script : SpellScript
{
    void HandleScriptEffect(uint effIndex)
    {
        Unit caster = GetCaster();
        Unit target = GetHitUnit();

        var mPeriodic = target.GetAuraEffectsByType(AuraType.PeriodicDamage);
        foreach (var aurEff in mPeriodic)
        {
            SpellInfo spellInfo = aurEff.GetSpellInfo();
            // search our Blood Plague and Frost Fever on target
            if (spellInfo.SpellFamilyName == SpellFamilyNames.Deathknight && (spellInfo.SpellFamilyFlags[2] & 0x2) != 0 && aurEff.GetCasterGUID() == caster.GetGUID())
            {
                int countMin = aurEff.GetBase().GetMaxDuration();
                int countMax = spellInfo.GetMaxDuration();

                // this Glyph
                countMax += 9000;

                if (countMin < countMax)
                {
                    aurEff.GetBase().SetDuration(aurEff.GetBase().GetDuration() + 3000);
                    aurEff.GetBase().SetMaxDuration(countMin + 3000);
                }
            }
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
    }
}

[Script] // Called by 206930 - Heart Strike
class spell_dk_heartbreaker : SpellScript
{
    public override bool Validate(SpellInfo spell)
    {
        return ValidateSpellInfo(SpellIds.HeartbreakerTalent, SpellIds.HeartbreakerEnergize);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.HeartbreakerTalent);
    }


    void HandleEnergize(uint effIndex)
    {
        GetCaster().CastSpell(GetCaster(), SpellIds.HeartbreakerEnergize, new CastSpellExtraArgs()
            .SetTriggeringSpell(GetSpell())
            .SetTriggerFlags(TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError));
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleEnergize, 0, SpellEffectName.Dummy));
    }
}

[Script] // 49184 - Howling Blast
class spell_dk_howling_blast : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FrostFever);
    }

    void HandleFrostFever(uint effIndex)
    {
        GetCaster().CastSpell(GetHitUnit(), SpellIds.FrostFever);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleFrostFever, 0, SpellEffectName.SchoolDamage));
    }
}

// Called by 45524 - Chains of Ice
[Script] // 454786 - Ice Prison
class spell_dk_ice_prison : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.IcePrisonTalent, SpellIds.IcePrisonRoot);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.IcePrisonTalent);
    }

    void HandleOnHit()
    {
        GetCaster().CastSpell(GetHitUnit(), SpellIds.IcePrisonRoot, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    public override void Register()
    {
        OnHit.Add(new(HandleOnHit));
    }
}

[Script] // 194878 - Icy Talons
class spell_dk_icy_talons : AuraScript
{
    bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        Spell procSpell = eventInfo.GetProcSpell();
        if (procSpell != null)
            return procSpell.GetPowerTypeCostAmount(PowerType.RunicPower) > 0;

        return false;
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.ProcTriggerSpellWithValue));
    }
}

[Script] // 194879 - Icy Talons
class spell_dk_icy_talons_buff : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.SmotheringOffense);
    }

    void HandleSmotheringOffense(ref WorldObject target)
    {
        if (!GetCaster().HasAura(SpellIds.SmotheringOffense))
            target = null;
    }

    public override void Register()
    {
        OnObjectTargetSelect.Add(new(HandleSmotheringOffense, 1, Targets.UnitCaster));
    }
}

[Script] // 374277 - Improved Death Strike
class spell_dk_improved_death_strike : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.Blood)
            && ValidateSpellEffect((spellInfo.Id, 4));
    }

    void CalcHealIncrease(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
    {
        if (GetUnitOwner().HasAura(SpellIds.Blood))
            amount = GetEffectInfo(3).CalcValue(GetCaster());
    }

    void CalcPowerCostReduction(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
    {
        if (GetUnitOwner().HasAura(SpellIds.Blood))
            amount = GetEffectInfo(4).CalcValue(GetCaster());
    }

    public override void Register()
    {
        DoEffectCalcAmount.Add(new(CalcHealIncrease, 0, AuraType.AddPctModifier));
        DoEffectCalcAmount.Add(new(CalcHealIncrease, 1, AuraType.AddPctModifier));
        DoEffectCalcAmount.Add(new(CalcPowerCostReduction, 2, AuraType.AddFlatModifier));
    }
}

[Script] // 206940 - Mark of Blood
class spell_dk_mark_of_blood : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.MarkOfBloodHeal);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        Unit caster = GetCaster();
        if (caster != null)
            caster.CastSpell(eventInfo.GetProcTarget(), SpellIds.MarkOfBloodHeal, true);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 207346 - Necrosis
class spell_dk_necrosis : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.NecrosisEffect);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        GetTarget().CastSpell(eventInfo.GetProcTarget(), SpellIds.NecrosisEffect, true);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 207256 - Obliteration
class spell_dk_obliteration : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.Obliteration, SpellIds.ObliterationRuneEnergize, SpellIds.KillingMachineProc)
            && ValidateSpellEffect((SpellIds.Obliteration, 1));
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        Unit target = GetTarget();
        target.CastSpell(target, SpellIds.KillingMachineProc, aurEff);

        AuraEffect oblitaration = target.GetAuraEffect(SpellIds.Obliteration, 1);
        if (oblitaration != null)
            if (RandomHelper.randChance(oblitaration.GetAmount()))
                target.CastSpell(target, SpellIds.ObliterationRuneEnergize, aurEff);
    }

    public override void Register()
    {
        AfterEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 207200 - Permafrost
class spell_dk_permafrost : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FrostShield);
    }

    void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        CastSpellExtraArgs args = new(aurEff);
        args.AddSpellMod(SpellValueMod.BasePoint0, (int)MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), aurEff.GetAmount()));
        GetTarget().CastSpell(GetTarget(), SpellIds.FrostShield, args);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.Dummy));
    }
}

// 121916 - Glyph of the Geist (Unholy)
[Script] // 6.x, does this belong here or in spell_generic? apply this in creature_template_addon? sniffs say this is always cast on raise dead.
class spell_dk_pet_geist_transform : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.GlyphOfTheGeist);
    }

    public override bool Load()
    {
        return GetCaster().IsPet();
    }

    SpellCastResult CheckCast()
    {
        Unit owner = GetCaster().GetOwner();
        if (owner != null)
            if (owner.HasAura(SpellIds.GlyphOfTheGeist))
                return SpellCastResult.SpellCastOk;

        return SpellCastResult.SpellUnavailable;
    }

    public override void Register()
    {
        OnCheckCast.Add(new(CheckCast));
    }
}

// 147157 Glyph of the Skeleton (Unholy)
[Script] // 6.x, does this belong here or in spell_generic? apply this in creature_template_addon? sniffs say this is always cast on raise dead.
class spell_dk_pet_skeleton_transform : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.GlyphOfTheSkeleton);
    }

    SpellCastResult CheckCast()
    {
        Unit owner = GetCaster().GetOwner();
        if (owner != null)
            if (owner.HasAura(SpellIds.GlyphOfTheSkeleton))
                return SpellCastResult.SpellCastOk;

        return SpellCastResult.SpellUnavailable;
    }

    public override void Register()
    {
        OnCheckCast.Add(new(CheckCast));
    }
}

// 61257 - Runic Power Back on Snare/Root
[Script] // 7.1.5
class spell_dk_pvp_4p_bonus : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.RunicReturn);
    }

    bool CheckProc(ProcEventInfo eventInfo)
    {
        SpellInfo spellInfo = eventInfo.GetSpellInfo();
        if (spellInfo == null)
            return false;

        return (spellInfo.GetAllEffectsMechanicMask() & ((1 << (int)Mechanics.Root) | (1 << (int)Mechanics.Snare))) != 0;
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        eventInfo.GetActionTarget().CastSpell(null, SpellIds.RunicReturn, true);
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 46584 - Raise Dead
class spell_dk_raise_dead : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.RaiseDeadSummon);
    }

    void HandleDummy(uint effIndex)
    {
        uint spellId = SpellIds.RaiseDeadSummon;
        GetCaster().CastSpell(null, spellId, true);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 440002 - Reaper of Souls (attached to 343294 - Soul Reaper)
class spell_dk_reaper_of_souls : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ReaperOfSoulsProc);
    }

    bool IsAffectedByReaperOfSouls()
    {
        Aura reaperOfSouls = GetCaster().GetAura(SpellIds.ReaperOfSoulsProc);
        if (reaperOfSouls != null)
            return GetSpell().m_appliedMods.Contains(reaperOfSouls);
        return false;
    }

    void HandleDefault(ref WorldObject target)
    {
        if (IsAffectedByReaperOfSouls())
            target = null;
    }

    void HandleReaperOfSouls(uint effIndex)
    {
        if (!IsAffectedByReaperOfSouls())
            PreventHitDefaultEffect(effIndex);
    }

    public override void Register()
    {
        OnObjectTargetSelect.Add(new(HandleDefault, 1, Targets.UnitTargetEnemy));
        OnEffectLaunch.Add(new(HandleReaperOfSouls, 3, SpellEffectName.TriggerSpell));
    }
}

[Script] // 59057 - Rime
class spell_dk_rime : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((spellInfo.Id, 1)) && ValidateSpellInfo(SpellIds.FrostScythe);
    }

    bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        float chance = (float)GetSpellInfo().GetEffect(1).CalcValue(GetTarget());
        if (eventInfo.GetSpellInfo().Id == SpellIds.FrostScythe)
            chance /= 2.0f;

        return RandomHelper.randChance(chance);
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.ProcTriggerSpell));
    }
}

// 343294 - Soul Reaper
// 469180 - Soul Reaper
[Script("spell_dk_soul_reaper", 1, 2)]
[Script("spell_dk_soul_reaper_reaper_of_souls", 0, null)]
class spell_dk_soul_reaper : AuraScript
{
    public spell_dk_soul_reaper(uint auraEffectIndex, uint? healthLimitEffectIndex)
    {
        _auraEffectIndex = (byte)auraEffectIndex;
        _healthLimitEffectIndex = healthLimitEffectIndex;
    }

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.SoulReaper, SpellIds.SoulReaperDamage, SpellIds.RunicCorruption);
    }

    void HandleOnTick(AuraEffect aurEff)
    {
        Unit target = GetTarget();
        Unit caster = GetCaster();
        if (caster == null)
            return;

        if (!_healthLimitEffectIndex.HasValue || target.GetHealthPct() < (float)GetEffectInfo(_healthLimitEffectIndex.Value).CalcValue(caster))
        {
            caster.CastSpell(target, SpellIds.SoulReaperDamage, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringAura = aurEff
            });
        }
    }

    void RemoveEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Death)
            return;

        Player caster = GetCaster()?.ToPlayer();
        if (caster == null)
            return;

        if (caster.IsHonorOrXPTarget(GetTarget()))
            caster.CastSpell(caster, SpellIds.RunicCorruption, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringAura = aurEff
            });
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(HandleOnTick, _auraEffectIndex, AuraType.PeriodicDummy));
        AfterEffectRemove.Add(new(RemoveEffect, _auraEffectIndex, AuraType.PeriodicDummy, AuraEffectHandleModes.Real));
    }


    byte _auraEffectIndex;
    uint? _healthLimitEffectIndex;
}

// Called by 383312 Abomination Limb and 49576 - Death Grip
[Script] // 454822 - Subduing Grasp
class spell_dk_subduing_grasp : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.SubduingGraspTalent, SpellIds.SubduingGraspDebuff);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.SubduingGraspTalent);
    }

    void HandleSubduingGrasp(uint effIndex)
    {
        GetCaster().CastSpell(GetHitUnit(), SpellIds.SubduingGraspDebuff, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    public override void Register()
    {
        if (m_scriptSpellId == SpellIds.GorefiendsGrasp)
            OnEffectHitTarget.Add(new(HandleSubduingGrasp, 1, SpellEffectName.ScriptEffect));
        else
            OnEffectHitTarget.Add(new(HandleSubduingGrasp, 0, SpellEffectName.ScriptEffect));
    }
}

[Script] // 242057 - Rune Empowered
class spell_dk_t20_2p_rune_empowered : AuraScript
{
    int _runicPowerSpent = 0;

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.PillarOfFrost, SpellIds.BreathOfSindragosa);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        Spell procSpell = procInfo.GetProcSpell();
        if (procSpell == null)
            return;

        Aura pillarOfFrost = GetTarget().GetAura(SpellIds.PillarOfFrost);
        if (pillarOfFrost == null)
            return;

        _runicPowerSpent += procSpell.GetPowerTypeCostAmount(PowerType.RunicPower).GetValueOrDefault(0);
        // Breath of Sindragosa special case
        SpellInfo breathOfSindragosa = Global.SpellMgr.GetSpellInfo(SpellIds.BreathOfSindragosa, Difficulty.None);
        if (procSpell.IsTriggeredByAura(breathOfSindragosa))
        {
            var powerRecord = breathOfSindragosa.PowerCosts.ToList().Find(power => power.PowerType == PowerType.RunicPower && power.PowerPctPerSecond > 0.0f);
            if (powerRecord != null)
                _runicPowerSpent += MathFunctions.CalculatePct(GetTarget().GetMaxPower(PowerType.RunicPower), powerRecord.PowerPctPerSecond);
        }

        if (_runicPowerSpent >= 600)
        {
            pillarOfFrost.SetDuration(pillarOfFrost.GetDuration() + 1000);
            _runicPowerSpent -= 600;
        }
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 55233 - Vampiric Blood
class spell_dk_vampiric_blood : AuraScript
{
    void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
    {
        amount = (int)GetUnitOwner().CountPctFromMaxHealth(amount);
    }

    public override void Register()
    {
        DoEffectCalcAmount.Add(new(CalculateAmount, 1, AuraType.ModIncreaseHealth2));
    }
}

[Script] // 273953 - Voracious (attached to 49998 - Death Strike)
class spell_dk_voracious : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.DhVoraciousTalent, SpellIds.DhVoraciousLeech);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.DhVoraciousTalent);
    }

    void HandleHit(uint effIndex)
    {
        Unit caster = GetCaster();
        caster.CastSpell(caster, SpellIds.DhVoraciousLeech, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleHit, 0, SpellEffectName.SchoolDamage));
    }
}

[Script] // 43265 - Death and Decay
class at_dk_death_and_decay(AreaTrigger areaTrigger) : AreaTriggerAI(areaTrigger)
{
    public override void OnUnitEnter(Unit unit)
    {
        if (unit.GetGUID() != at.GetCasterGUID())
            return;

        if (unit.HasAura(SpellIds.CleavingStrikes))
            unit.CastSpell(unit, SpellIds.DeathAndDecayIncreaseTargets, TriggerCastFlags.DontReportCastError);

        if (unit.HasAura(SpellIds.SanguineGroundTalent))
            unit.CastSpell(unit, SpellIds.SanguineGround);
    }

    public override void OnUnitExit(Unit unit, AreaTriggerExitReason reason)
    {
        if (unit.GetGUID() != at.GetCasterGUID())
            return;

        Aura deathAndDecay = unit.GetAura(SpellIds.DeathAndDecayIncreaseTargets);
        if (deathAndDecay != null)
        {
            AuraEffect cleavingStrikes = unit.GetAuraEffect(SpellIds.CleavingStrikes, 3);
            if (cleavingStrikes != null)

                deathAndDecay.SetDuration(cleavingStrikes.GetAmount());
        }

        unit.RemoveAurasDueToSpell(SpellIds.SanguineGround);
    }
}