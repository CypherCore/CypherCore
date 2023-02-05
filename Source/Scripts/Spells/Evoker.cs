// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.Spells.Evoker
{
    internal struct SpellIds
    {
        public const uint EnergizingFlame = 400006;
        public const uint GlideKnockback = 358736;
        public const uint Hover = 358267;
        public const uint LivingFlame = 361469;
        public const uint LivingFlameDamage = 361500;
        public const uint LivingFlameHeal = 361509;
        public const uint SoarRacial = 369536;
    }

    [Script] // 362969 - Azure Strike (blue)
    class spell_evo_azure_strike : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new List<ISpellEffect>();
        void FilterTargets(List<WorldObject> targets)
        {
            targets.Remove(GetExplTargetUnit());
            targets.RandomResize((uint)GetEffectInfo(0).CalcValue(GetCaster()) - 1);
            targets.Add(GetExplTargetUnit());
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaEnemy));
        }
    }

    [Script] // 358733 - Glide (Racial)
    internal class spell_evo_glide : SpellScript, ISpellCheckCastHander, ISpellOnCast
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GlideKnockback, SpellIds.Hover, SpellIds.SoarRacial);
        }

        public SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();

            if (!caster.IsFalling())
                return SpellCastResult.NotOnGround;

            return SpellCastResult.SpellCastOk;
        }

        public void OnCast()
        {
            Player caster = GetCaster().ToPlayer();

            if (caster == null)
                return;

            caster.CastSpell(caster, SpellIds.GlideKnockback, true);

            caster.GetSpellHistory().StartCooldown(Global.SpellMgr.GetSpellInfo(SpellIds.Hover, GetCastDifficulty()), 0, null, false, TimeSpan.FromMilliseconds(250));
            caster.GetSpellHistory().StartCooldown(Global.SpellMgr.GetSpellInfo(SpellIds.SoarRacial, GetCastDifficulty()), 0, null, false, TimeSpan.FromMilliseconds(250));
        }
    }

    [Script] // 361469 - Living Flame (Red)
    class spell_evo_living_flame : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new List<ISpellEffect>();

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
            SpellEffects.Add(new EffectHandler(HandleHitTarget, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
            SpellEffects.Add(new EffectHandler(HandleLaunchTarget, 0, SpellEffectName.Dummy, SpellScriptHookType.LaunchTarget));
        }
    }

}
