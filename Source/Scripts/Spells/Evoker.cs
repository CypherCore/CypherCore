// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using static Global;

namespace Scripts.Spells.Evoker
{
    enum SpellIds
    {
        BlastFurnace = 375510,
        BlessingOfTheBronzeDk = 381732,
        BlessingOfTheBronzeDh = 381741,
        BlessingOfTheBronzeDruid = 381746,
        BlessingOfTheBronzeEvoker = 381748,
        BlessingOfTheBronzeHunter = 381749,
        BlessingOfTheBronzeMage = 381750,
        BlessingOfTheBronzeMonk = 381751,
        BlessingOfTheBronzePaladin = 381752,
        BlessingOfTheBronzePriest = 381753,
        BlessingOfTheBronzeRogue = 381754,
        BlessingOfTheBronzeShaman = 381756,
        BlessingOfTheBronzeWarlock = 381757,
        BlessingOfTheBronzeWarrior = 381758,
        EnergizingFlame = 400006,
        FireBreathDamage = 357209,
        GlideKnockback = 358736,
        Hover = 358267,
        LivingFlame = 361469,
        LivingFlameDamage = 361500,
        LivingFlameHeal = 361509,
        PermeatingChillTalent = 370897,
        PyreDamage = 357212,
        ScouringFlame = 378438,
        SoarRacial = 369536,

        LabelEvokerBlue = 1465
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

                return (SpellIds)GetSpellInfo().Id switch
                {
                    SpellIds.BlessingOfTheBronzeDk => unitTarget.GetClass() != Class.Deathknight,
                    SpellIds.BlessingOfTheBronzeDh => unitTarget.GetClass() != Class.DemonHunter,
                    SpellIds.BlessingOfTheBronzeDruid => unitTarget.GetClass() != Class.Druid,
                    SpellIds.BlessingOfTheBronzeEvoker => unitTarget.GetClass() != Class.Evoker,
                    SpellIds.BlessingOfTheBronzeHunter => unitTarget.GetClass() != Class.Hunter,
                    SpellIds.BlessingOfTheBronzeMage => unitTarget.GetClass() != Class.Mage,
                    SpellIds.BlessingOfTheBronzeMonk => unitTarget.GetClass() != Class.Monk,
                    SpellIds.BlessingOfTheBronzePaladin => unitTarget.GetClass() != Class.Paladin,
                    SpellIds.BlessingOfTheBronzePriest => unitTarget.GetClass() != Class.Priest,
                    SpellIds.BlessingOfTheBronzeRogue => unitTarget.GetClass() != Class.Rogue,
                    SpellIds.BlessingOfTheBronzeShaman => unitTarget.GetClass() != Class.Shaman,
                    SpellIds.BlessingOfTheBronzeWarlock => unitTarget.GetClass() != Class.Warlock,
                    SpellIds.BlessingOfTheBronzeWarrior => unitTarget.GetClass() != Class.Warrior,
                    _ => true
                };
            });
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(RemoveInvalidTargets, SpellConst.EffectAll, Targets.UnitCasterAreaRaid));
        }
    }


    [Script] // 370455 - Charged Blast
    class spell_evo_charged_blast : AuraScript
    {
        bool CheckProc(ProcEventInfo procInfo)
        {
            return procInfo.GetSpellInfo() != null && procInfo.GetSpellInfo().HasLabel((uint)SpellIds.LabelEvokerBlue);
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
        }
    }

    // 357208 Fire Breath (Red)
    [Script] // 382266 Fire Breath (Red)
    class spell_evo_fire_breath : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo((uint)SpellIds.FireBreathDamage, (uint)SpellIds.BlastFurnace);
        }

        void OnComplete(int completedStageCount)
        {
            int dotTicks = 10 - (completedStageCount - 1) * 3;
            AuraEffect blastFurnace = GetCaster().GetAuraEffect((uint)SpellIds.BlastFurnace, 0);
            if (blastFurnace != null)
                dotTicks += blastFurnace.GetAmount() / 2;

            GetCaster().CastSpell(GetCaster(), (uint)SpellIds.FireBreathDamage, new CastSpellExtraArgs()
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

        void AddBonusUpfrontDamage(Unit victim, ref int damage, ref int flatMod, ref float pctMod)
        {
            int empowerLevel = (int)GetSpell().m_customArg;
            if (empowerLevel == 0)
                return;

            // damage is done after aura is applied, grab periodic amount
            AuraEffect fireBreath = victim.GetAuraEffect(GetSpellInfo().Id, 1, GetCaster().GetGUID());
            if (fireBreath != null)
                flatMod += (int)(fireBreath.GetEstimatedAmount().GetValueOrDefault(fireBreath.GetAmount()) * (empowerLevel - 1) * 3);
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

    [Script] // 358733 - Glide (Racial)
    class spell_evo_glide : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo((uint)SpellIds.GlideKnockback, (uint)SpellIds.Hover, (uint)SpellIds.SoarRacial);
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

            caster.CastSpell(caster, (uint)SpellIds.GlideKnockback, true);

            caster.GetSpellHistory().StartCooldown(SpellMgr.GetSpellInfo((uint)SpellIds.Hover, GetCastDifficulty()), 0, null, false, TimeSpan.FromMilliseconds(250));
            caster.GetSpellHistory().StartCooldown(SpellMgr.GetSpellInfo((uint)SpellIds.SoarRacial, GetCastDifficulty()), 0, null, false, TimeSpan.FromMilliseconds(250));
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
            return ValidateSpellInfo((uint)SpellIds.LivingFlameDamage, (uint)SpellIds.LivingFlameHeal, (uint)SpellIds.EnergizingFlame);
        }

        void HandleHitTarget(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit hitUnit = GetHitUnit();
            if (caster.IsFriendlyTo(hitUnit))
                caster.CastSpell(hitUnit, (uint)SpellIds.LivingFlameHeal, true);
            else
                caster.CastSpell(hitUnit, (uint)SpellIds.LivingFlameDamage, true);
        }

        void HandleLaunchTarget(uint effIndex)
        {
            Unit caster = GetCaster();
            if (caster.IsFriendlyTo(GetHitUnit()))
                return;

            AuraEffect auraEffect = caster.GetAuraEffect((uint)SpellIds.EnergizingFlame, 0);
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

    [Script] // 381773 - Permeating Chill
    class spell_evo_permeating_chill : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo((uint)SpellIds.PermeatingChillTalent);
        }

        bool CheckProc(ProcEventInfo procInfo)
        {
            SpellInfo spellInfo = procInfo.GetSpellInfo();
            if (spellInfo == null)
                return false;

            if (spellInfo.HasLabel((uint)SpellIds.LabelEvokerBlue))
                return false;

            if (!procInfo.GetActor().HasAura((uint)SpellIds.PermeatingChillTalent))
                if (spellInfo.IsAffected(SpellFamilyNames.Evoker, new FlagArray128(0x40, 0, 0, 0))) // disintegrate
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
            return ValidateSpellInfo((uint)SpellIds.PyreDamage);
        }

        void HandleDamage(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit().GetPosition(), (uint)SpellIds.PyreDamage, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDamage, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 357209 Fire Breath (Red)
    class spell_evo_scouring_flame : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo((uint)SpellIds.ScouringFlame);
        }

        void HandleScouringFlame(List<WorldObject> targets)
        {
            if (!GetCaster().HasAura((uint)SpellIds.ScouringFlame))
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
}