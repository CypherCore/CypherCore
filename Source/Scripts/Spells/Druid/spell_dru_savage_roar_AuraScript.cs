using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	[Script]
	internal class spell_dru_savage_roar_AuraScript : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(DruidSpellIds.SavageRoar);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(AfterApply, 1, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
			AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
		}

		private void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
		{
			var target = GetTarget();
			target.CastSpell(target, DruidSpellIds.SavageRoar, new CastSpellExtraArgs(aurEff).SetOriginalCaster(GetCasterGUID()));
		}

		private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
		{
			GetTarget().RemoveAurasDueToSpell(DruidSpellIds.SavageRoar);
		}
	}
}