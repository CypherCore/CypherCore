// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;
namespace Scripts.Spells.Warrior
{
    //197690
    [SpellScript(197690)]
    public class spell_defensive_state : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new List<IAuraEffectHandler>();

        private void OnApply(AuraEffect aura, AuraEffectHandleModes auraMode)
        {
            Unit caster = GetCaster();

            if (caster != null)
            {
                AuraEffect defensiveState = caster?.GetAura(197690)?.GetEffect(0);

                if (defensiveState != null)
                    defensiveState.GetAmount();
            }
        }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(OnApply, 0, AuraType.ModDamagePercentTaken, AuraEffectHandleModes.Real));
        }
    }
}