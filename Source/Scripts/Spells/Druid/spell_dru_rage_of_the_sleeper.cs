﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(200851)]
public class spell_dru_rage_of_the_sleeper : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();


	private void CalculateAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
	{
		amount = -1;
	}

	private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster != null)
		{
			var target = caster.GetVictim();

			if (target != null)
				caster.CastSpell(target, 219432, true);
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 1, AuraType.SchoolAbsorb));
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 1, AuraType.SchoolAbsorb, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}
}