using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// 211720 - Thal'kiel's Discord
	[SpellScript(211720)]
	public class spell_warlock_artifact_thalkiels_discord : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
		{
			PreventDefaultAction();
			var caster = GetCaster();
			var target = eventInfo.GetActionTarget();

			if (caster == null || target == null)
				return;

			if (!caster.IsValidAttackTarget(target))
				return;

			caster.CastSpell(target, aurEff.GetSpellEffectInfo().TriggerSpell, true);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
		}
	}
}