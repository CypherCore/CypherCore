using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[SpellScript(194386)]
public class aura_hun_volley : AuraScript, IHasAuraEffects, IAuraCheckProc
{
	public List<IAuraEffectHandler> AuraEffects => new();

	public bool CheckProc(ProcEventInfo UnnamedParameter)
	{
		return GetTarget().GetPower(PowerType.Focus) >= GetSpellInfo().GetEffect(0).BasePoints;
	}

	private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
	{
		GetTarget().ModifyPower(PowerType.Focus, -GetSpellInfo().GetEffect(0).BasePoints);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}
}