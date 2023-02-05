// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    [SpellScript(686)] // 686 - Shadow Bolt
    internal class spell_warl_shadow_bolt : SpellScript, ISpellAfterCast
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(WarlockSpells.SHADOW_BOLT_SHOULSHARD);
        }

        public void AfterCast()
        {
            GetCaster().CastSpell(GetCaster(), WarlockSpells.SHADOW_BOLT_SHOULSHARD, true);
        }
    }
}