using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	[Script] // 768 - CatForm - SPELL_DRUID_CAT_FORM
	internal class spell_dru_cat_form : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(DruidSpellIds.Prowl);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(HandleAfterRemove, 0, AuraType.ModShapeshift, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
		}

		private void HandleAfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
		{
			GetTarget().RemoveOwnedAura(DruidSpellIds.Prowl);
		}
	}
}