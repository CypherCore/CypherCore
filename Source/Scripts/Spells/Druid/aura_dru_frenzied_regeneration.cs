// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(22842)]
public class aura_dru_frenzied_regeneration : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	private void CalculateAmount(AuraEffect UnnamedParameter, ref double amount, ref bool UnnamedParameter2)
	{
		var frenzied = GetCaster().GetAura(22842);

		if (frenzied != null)
			frenzied.GetMaxDuration();

		var healAmount = MathFunctions.CalculatePct(GetCaster().GetDamageOverLastSeconds(5), 50);
		var minHealAmount = MathFunctions.CalculatePct(GetCaster().GetMaxHealth(), 5);
		healAmount = Math.Max(healAmount, minHealAmount);
		amount = healAmount;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.ObsModHealth));
	}
}