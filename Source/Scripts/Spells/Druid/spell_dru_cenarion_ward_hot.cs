using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(102352)]
public class spell_dru_cenarion_ward_hot : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();


	private void CalculateAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
	{
		if (!GetCaster())
		{
			return;
		}

		amount = (int)MathFunctions.CalculatePct(GetCaster().SpellBaseHealingBonusDone(SpellSchoolMask.Nature), 220) / 4;
	}

	public override void Register()
	{
		AuraEffects.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.PeriodicHeal));
	}
}