// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.m_Events.WinterVeil
{
    internal struct SpellIds
    {
        //Mistletoe
        public const uint CreateMistletoe = 26206;
        public const uint CreateHolly = 26207;
        public const uint CreateSnowflakes = 45036;

        //Winter Wondervolt
        public const uint Px238WinterWondervoltTransform1 = 26157;
        public const uint Px238WinterWondervoltTransform2 = 26272;
        public const uint Px238WinterWondervoltTransform3 = 26273;
        public const uint Px238WinterWondervoltTransform4 = 26274;

        //Reindeertransformation
        public const uint FlyingReindeer310 = 44827;
        public const uint FlyingReindeer280 = 44825;
        public const uint FlyingReindeer60 = 44824;
        public const uint Reindeer100 = 25859;
        public const uint Reindeer60 = 25858;
    }

    [Script] // 26218 - Mistletoe
    internal class spell_winter_veil_mistletoe : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.CreateMistletoe, SpellIds.CreateHolly, SpellIds.CreateSnowflakes);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(int effIndex)
        {
            Player target = GetHitPlayer();

            if (target)
            {
                uint spellId = RandomHelper.RAND(SpellIds.CreateHolly, SpellIds.CreateMistletoe, SpellIds.CreateSnowflakes);
                GetCaster().CastSpell(target, spellId, true);
            }
        }
    }

    [Script] // 26275 - PX-238 Winter Wondervolt TRAP
    internal class spell_winter_veil_px_238_winter_wondervolt : SpellScript, IHasSpellEffects
    {
        private static readonly uint[] spells =
        {
            SpellIds.Px238WinterWondervoltTransform1, SpellIds.Px238WinterWondervoltTransform2, SpellIds.Px238WinterWondervoltTransform3, SpellIds.Px238WinterWondervoltTransform4
        };

        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Px238WinterWondervoltTransform1,
                                     SpellIds.Px238WinterWondervoltTransform2,
                                     SpellIds.Px238WinterWondervoltTransform3,
                                     SpellIds.Px238WinterWondervoltTransform4);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(int effIndex)
        {
            PreventHitDefaultEffect(effIndex);

            Unit target = GetHitUnit();

            if (target)
            {
                for (byte i = 0; i < 4; ++i)
                    if (target.HasAura(spells[i]))
                        return;

                target.CastSpell(target, spells[RandomHelper.URand(0, 3)], true);
            }
        }
    }

    [Script]
    internal class spell_item_reindeer_transformation : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.FlyingReindeer310, SpellIds.FlyingReindeer280, SpellIds.FlyingReindeer60, SpellIds.Reindeer100, SpellIds.Reindeer60);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(int effIndex)
        {
            Unit caster = GetCaster();

            if (caster.HasAuraType(AuraType.Mounted))
            {
                double flyspeed = caster.GetSpeedRate(UnitMoveType.Flight);
                double speed = caster.GetSpeedRate(UnitMoveType.Run);

                caster.RemoveAurasByType(AuraType.Mounted);
                //5 different spells used depending on mounted speed and if Mount can fly or not

                if (flyspeed >= 4.1f)
                    // Flying Reindeer
                    caster.CastSpell(caster, SpellIds.FlyingReindeer310, true); //310% flying Reindeer
                else if (flyspeed >= 3.8f)
                    // Flying Reindeer
                    caster.CastSpell(caster, SpellIds.FlyingReindeer280, true); //280% flying Reindeer
                else if (flyspeed >= 1.6f)
                    // Flying Reindeer
                    caster.CastSpell(caster, SpellIds.FlyingReindeer60, true); //60% flying Reindeer
                else if (speed >= 2.0f)
                    // Reindeer
                    caster.CastSpell(caster, SpellIds.Reindeer100, true); //100% ground Reindeer
                else
                    // Reindeer
                    caster.CastSpell(caster, SpellIds.Reindeer60, true); //60% ground Reindeer
            }
        }
    }
}