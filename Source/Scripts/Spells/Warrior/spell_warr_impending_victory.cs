// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
namespace Scripts.Spells.Warrior
{
    [Script] // 202168 - Impending Victory
    internal class spell_warr_impending_victory : SpellScript, IAfterCast
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.IMPENDING_VICTORY_HEAL);
        }

        public void AfterCast()
        {
            Unit caster = GetCaster();
            caster.CastSpell(caster, SpellIds.IMPENDING_VICTORY_HEAL, true);
            caster.RemoveAurasDueToSpell(SpellIds.VICTORIOUS);
        }
    }
}