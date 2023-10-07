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
    struct SpellIds
    {
        public const uint EnergizingFlame = 400006;
        public const uint GlideKnockback = 358736;
        public const uint Hover = 358267;
        public const uint LivingFlame = 361469;
        public const uint LivingFlameDamage = 361500;
        public const uint LivingFlameHeal = 361509;
        public const uint PermeatingChillTalent = 370897;
        public const uint PyreDamage = 357212;
        public const uint SoarRacial = 369536;

        public const uint LabelEvokerBlue = 1465;
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

            caster.GetSpellHistory().StartCooldown(SpellMgr.GetSpellInfo(SpellIds.Hover, GetCastDifficulty()), 0, null, false, TimeSpan.FromMilliseconds(250));
            caster.GetSpellHistory().StartCooldown(SpellMgr.GetSpellInfo(SpellIds.SoarRacial, GetCastDifficulty()), 0, null, false, TimeSpan.FromMilliseconds(250));
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
            if (caster.IsFriendlyTo(hitUnit))
                caster.CastSpell(hitUnit, SpellIds.LivingFlameHeal, true);
            else
                caster.CastSpell(hitUnit, SpellIds.LivingFlameDamage, true);
        }

        void HandleLaunchTarget(uint effIndex)
        {
            Unit caster = GetCaster();
            if (caster.IsFriendlyTo(GetHitUnit()))
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

            if (spellInfo.HasLabel(SpellIds.LabelEvokerBlue))
                return false;

            if (!procInfo.GetActor().HasAura(SpellIds.PermeatingChillTalent))
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
}