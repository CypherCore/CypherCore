using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	// War Machine 215556
	[SpellScript(215556)]
	public class aura_warr_war_machine : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			var caster = GetCaster();

			if (caster != null)
				caster.CastSpell(caster, WarriorSpells.WAR_MACHINE_AURA, true);
		}

		private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			var caster = GetCaster();

			if (caster != null)
				caster.RemoveAurasDueToSpell(WarriorSpells.WAR_MACHINE_AURA);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.ProcTriggerSpell, AuraEffectHandleModes.Real));
			AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.ProcTriggerSpell, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
		}
	}
}