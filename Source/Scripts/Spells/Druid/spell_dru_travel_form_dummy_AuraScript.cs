using System.Collections.Generic;
using Framework.Constants;
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
			AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
			AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
		}

		private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
		{
			var player = GetTarget().ToPlayer();

			// Outdoor check already passed - Travel Form (dummy) has SPELL_ATTR0_OUTDOORS_ONLY attribute.
			var triggeredSpellId = spell_dru_travel_form_AuraScript.GetFormSpellId(player, GetCastDifficulty(), false);

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