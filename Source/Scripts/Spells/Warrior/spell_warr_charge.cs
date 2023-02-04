// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
namespace Scripts.Spells.Warrior
{
    [SpellScript(100)] // 100 - Charge
    internal class spell_warr_charge : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CHARGE_EFFECT, SpellIds.CHARGE_EFFECT_BLAZING_TRAIL);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            uint spellId = SpellIds.CHARGE_EFFECT;

            if (GetCaster().HasAura(SpellIds.GLYPH_OF_THE_BLAZING_TRAIL))
                spellId = SpellIds.CHARGE_EFFECT_BLAZING_TRAIL;

            GetCaster().CastSpell(GetHitUnit(), spellId, true);
        }
    }
}