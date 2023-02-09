using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[SpellScript(199754)]
public class spell_rog_riposte_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();


	private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo procInfo)
	{
		PreventDefaultAction();

		Unit caster = GetCaster();
		if (caster == null)
		{
			return;
		}

		Unit target = procInfo.GetActionTarget();
		if (target == null)
		{
			return;
		}
		caster.CastSpell(target, RogueSpells.SPELL_ROGUE_RIPOSTE_DAMAGE, true);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 1, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}
}