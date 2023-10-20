// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System.Collections.Generic;

namespace Scripts.Events
{
    [Script] // 26218 - Mistletoe
    class spell_winter_veil_mistletoe : SpellScript
    {
        const uint SpellCreateMistletoe = 26206;
        const uint SpellCreateHolly = 26207;
        const uint SpellCreateSnowflakes = 45036;

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellCreateMistletoe, SpellCreateHolly, SpellCreateSnowflakes);
        }

        void HandleScript(uint effIndex)
        {
            Player target = GetHitPlayer();
            if (target != null)
            {
                uint spellId = RandomHelper.RAND(SpellCreateHolly, SpellCreateMistletoe, SpellCreateSnowflakes);
                GetCaster().CastSpell(target, spellId, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 26275 - Px-238 Winter Wondervolt Trap
    class spell_winter_veil_px_238_winter_wondervolt : SpellScript
    {
        uint[] WonderboltTransformSpells = { 26157, 26272, 26273, 26274 };

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(WonderboltTransformSpells);
        }

        void HandleScript(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);

            Unit target = GetHitUnit();
            if (target != null)
            {
                foreach (uint spell in WonderboltTransformSpells)
                    if (target.HasAura(spell))
                        return;

                target.CastSpell(target, WonderboltTransformSpells.SelectRandom(), true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 25860 - Reindeer Transformation
    class spell_winter_veil_reindeer_transformation : SpellScript
    {
        const uint SpellFlyingReindeer310 = 44827;
        const uint SpellFlyingReindeer280 = 44825;
        const uint SpellFlyingReindeer60 = 44824;
        const uint SpellReindeer100 = 25859;
        const uint SpellReindeer60 = 25858;

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellFlyingReindeer310, SpellFlyingReindeer280, SpellFlyingReindeer60, SpellReindeer100, SpellReindeer60);
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            if (caster.HasAuraType(AuraType.Mounted))
            {
                float flyspeed = caster.GetSpeedRate(UnitMoveType.Flight);
                float speed = caster.GetSpeedRate(UnitMoveType.Run);

                caster.RemoveAurasByType(AuraType.Mounted);
                //5 different spells used depending on mounted speed and if mount can fly or not

                if (flyspeed >= 4.1f)
                    // Flying Reindeer
                    caster.CastSpell(caster, SpellFlyingReindeer310, true); //310% flying Reindeer
                else if (flyspeed >= 3.8f)
                    // Flying Reindeer
                    caster.CastSpell(caster, SpellFlyingReindeer280, true); //280% flying Reindeer
                else if (flyspeed >= 1.6f)
                    // Flying Reindeer
                    caster.CastSpell(caster, SpellFlyingReindeer60, true); //60% flying Reindeer
                else if (speed >= 2.0f)
                    // Reindeer
                    caster.CastSpell(caster, SpellReindeer100, true); //100% ground Reindeer
                else
                    // Reindeer
                    caster.CastSpell(caster, SpellReindeer60, true); //60% ground Reindeer
            }
        }

        public override void Register()
        {
            OnEffectHit.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }
}