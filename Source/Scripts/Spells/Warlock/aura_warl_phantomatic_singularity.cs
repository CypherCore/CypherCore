// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    // 205179
    [SpellScript(205179)]
    public class aura_warl_phantomatic_singularity : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public void OnTick(AuraEffect UnnamedParameter)
        {
            Unit caster = GetCaster();

            if (GetCaster())
                caster.CastSpell(GetTarget().GetPosition(), WarlockSpells.PHANTOMATIC_SINGULARITY_DAMAGE, true);
        }

        public override void Register()
        {
            Effects.Add(new EffectPeriodicHandler(OnTick, 0, AuraType.PeriodicLeech));
        }
    }
}