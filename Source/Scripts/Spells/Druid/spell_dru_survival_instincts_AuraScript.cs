// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
    [Script] // 61336 - Survival Instincts
    internal class spell_dru_survival_instincts_AuraScript : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(DruidSpellIds.SurvivalInstincts);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(AfterApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
            AuraEffects.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().CastSpell(GetTarget(), DruidSpellIds.SurvivalInstincts, true);
        }

        private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(DruidSpellIds.SurvivalInstincts);
        }
    }
}