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
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private void CalculateAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
	{
		Aura frenzied = GetCaster().GetAura(22842);
		if (frenzied != null)
		{
			frenzied.GetMaxDuration();
		}
		ulong healAmount    = MathFunctions.CalculatePct(GetCaster().GetDamageOverLastSeconds(5), 50);
		ulong minHealAmount = MathFunctions.CalculatePct(GetCaster().GetMaxHealth(), 5);
		healAmount = Math.Max(healAmount, minHealAmount);
		amount     = (int)healAmount;
	}

	public override void Register()
	{
		AuraEffects.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.ObsModHealth));
	}
}