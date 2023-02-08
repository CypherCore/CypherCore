using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(200389)]
public class spell_dru_cultivation : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private void CalculateAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
	{
		if (!GetCaster())
		{
			return;
		}

		amount = (int)MathFunctions.CalculatePct(GetCaster().SpellBaseHealingBonusDone(SpellSchoolMask.Nature), 60);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.PeriodicHeal));
	}
}