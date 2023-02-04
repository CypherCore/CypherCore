// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
namespace Scripts.Spells.Warrior
{
    [Script] // 167105 - Colossus Smash 7.1.5
    internal class spell_warr_colossus_smash_SpellScript : SpellScript, IOnHit
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.COLOSSUS_SMASH_EFFECT);
        }

        public void OnHit()
        {
            Unit target = GetHitUnit();

            if (target)
                GetCaster().CastSpell(target, SpellIds.COLOSSUS_SMASH_EFFECT, true);
        }
    }
}