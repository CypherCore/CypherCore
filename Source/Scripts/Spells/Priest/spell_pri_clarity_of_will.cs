using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(152118)]
public class spell_pri_clarity_of_will : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool UnnamedParameter)
	{
		Unit caster = aurEff.GetCaster();
		if (caster != null)
		{
			Player player = caster.ToPlayer();
			if (player != null)
			{
				var absorbamount = 9.0f * player.SpellBaseHealingBonusDone(GetSpellInfo().GetSchoolMask());
				amount += (int)absorbamount;
			}
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
	}
}