// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior
{
    //280772 - Siegebreaker
    [SpellScript(280772)]
    public class spell_warr_siegebreaker : SpellScript, IOnHit
    {
        public void OnHit()
        {
            Unit caster = GetCaster();
            caster.CastSpell(null, WarriorSpells.SIEGEBREAKER_BUFF, true);
        }
    }
}