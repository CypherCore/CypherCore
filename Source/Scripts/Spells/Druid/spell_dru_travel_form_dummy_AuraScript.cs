// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
    [Script]
    internal class spell_dru_travel_form_dummy_AuraScript : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(DruidSpellIds.FormStag, DruidSpellIds.FormAquatic, DruidSpellIds.FormFlight, DruidSpellIds.FormSwiftFlight);
        }

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
            AuraEffects.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Player player = GetTarget().ToPlayer();

            // Outdoor check already passed - Travel Form (dummy) has SPELL_ATTR0_OUTDOORS_ONLY attribute.
            uint triggeredSpellId = spell_dru_travel_form_AuraScript.GetFormSpellId(player, GetCastDifficulty(), false);

            player.CastSpell(player, triggeredSpellId, new CastSpellExtraArgs(aurEff));
        }

        private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // No need to check remove mode, it's safe for Auras to remove each other in AfterRemove hook.
            GetTarget().RemoveAura(DruidSpellIds.FormStag);
            GetTarget().RemoveAura(DruidSpellIds.FormAquatic);
            GetTarget().RemoveAura(DruidSpellIds.FormFlight);
            GetTarget().RemoveAura(DruidSpellIds.FormSwiftFlight);
        }
    }
}