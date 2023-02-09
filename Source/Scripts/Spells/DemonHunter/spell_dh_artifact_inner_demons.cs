using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(201471)]
public class spell_dh_artifact_inner_demons : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();


	private void OnProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
	{
		var caster = GetCaster();
		var target = eventInfo.GetActionTarget();

		if (caster == null || target == null)
			return;

		caster.VariableStorage.Set("Spells.InnerDemonsTarget", target.GetGUID());
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}
}