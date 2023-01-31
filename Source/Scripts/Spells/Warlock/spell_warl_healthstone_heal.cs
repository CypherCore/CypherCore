// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
    [Script] // 6262 - Healthstone
    internal class spell_warl_healthstone_heal : SpellScript, IOnHit
    {
        public void OnHit()
        {
            int heal = (int)MathFunctions.CalculatePct(GetCaster().GetCreateHealth(), GetHitHeal());
            SetHitHeal(heal);
        }
    }
}