// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
    [Script] // 33763 - Lifebloom
    internal class spell_dru_lifebloom : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(DruidSpellIds.LifebloomFinalHeal);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // Final heal only on duration end
            if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Expire ||
                GetTargetApplication().GetRemoveMode() == AuraRemoveMode.EnemySpell)
                GetCaster().CastSpell(GetUnitOwner(), DruidSpellIds.LifebloomFinalHeal, true);
        }
    }
}