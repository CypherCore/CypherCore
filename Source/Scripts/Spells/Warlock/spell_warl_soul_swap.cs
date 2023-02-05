// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    [SpellScript(86121)] // 86121 - Soul Swap
    internal class spell_warl_soul_swap : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(WarlockSpells.GLYPH_OF_SOUL_SWAP, WarlockSpells.SOUL_SWAP_CD_MARKER, WarlockSpells.SOUL_SWAP_OVERRIDE);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleHit(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), WarlockSpells.SOUL_SWAP_OVERRIDE, true);
            GetHitUnit().CastSpell(GetCaster(), WarlockSpells.SOUL_SWAP_OVERRIDE, true);
        }
    }
}