using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(212817)]
public class spell_dh_artifact_fiery_demise : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		var caster = GetCaster();
		var target = eventInfo.GetActionTarget();

		if (caster == null || target == null || !caster.IsValidAttackTarget(target))
			return;

		caster.CastSpell(target, ShatteredSoulsSpells.SPELL_DH_FIERY_DEMISE_DEBUFF, aurEff.GetAmount());
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}
}