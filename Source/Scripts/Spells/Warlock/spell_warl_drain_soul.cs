// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;
using Game.Spells.Auras.EffectHandlers;

namespace Scripts.Spells.Warlock
{
    [Script] // 198590 - Drain Soul
    internal class spell_warl_drain_soul : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.DRAIN_SOUL_ENERGIZE);
        }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Death)
                return;

            Unit caster = GetCaster();

            caster?.CastSpell(caster, SpellIds.DRAIN_SOUL_ENERGIZE, true);
        }
    }
}