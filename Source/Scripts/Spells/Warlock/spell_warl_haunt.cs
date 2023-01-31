// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using Game.Spells.Auras.EffectHandlers;

namespace Scripts.Spells.Warlock
{
    [Script] // 48181 - Haunt
    internal class spell_warl_haunt : SpellScript, IAfterHit
    {
        public void AfterHit()
        {
            Aura aura = GetHitAura();

            if (aura != null)
            {
                AuraEffect aurEff = aura.GetEffect(1);

                aurEff?.SetAmount(MathFunctions.CalculatePct(GetHitDamage(), aurEff.GetAmount()));
            }
        }
    }
}