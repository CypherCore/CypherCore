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
    [Script] // Heroic Leap (triggered by Heroic Leap (6544)) - 178368
    internal class spell_warr_heroic_leap_jump : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GLYPH_OF_HEROIC_LEAP,
                                     SpellIds.GLYPH_OF_HEROIC_LEAP_BUFF,
                                     SpellIds.IMPROVED_HEROIC_LEAP,
                                     SpellIds.TAUNT);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(AfterJump, 1, SpellEffectName.JumpDest, SpellScriptHookType.EffectHit));
        }

        private void AfterJump(uint effIndex)
        {
            if (GetCaster().HasAura(SpellIds.GLYPH_OF_HEROIC_LEAP))
                GetCaster().CastSpell(GetCaster(), SpellIds.GLYPH_OF_HEROIC_LEAP_BUFF, true);

            if (GetCaster().HasAura(SpellIds.IMPROVED_HEROIC_LEAP))
                GetCaster().GetSpellHistory().ResetCooldown(SpellIds.TAUNT, true);
        }
    }
}