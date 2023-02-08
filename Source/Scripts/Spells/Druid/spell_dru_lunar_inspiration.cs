// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
    [Script] // 155580 - Lunar Inspiration
    internal class spell_dru_lunar_inspiration : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(DruidSpellIds.LunarInspirationOverride);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectApplyHandler(AfterApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
            AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }

        private void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().CastSpell(GetTarget(), DruidSpellIds.LunarInspirationOverride, true);
        }

        private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(DruidSpellIds.LunarInspirationOverride);
        }
    }
}