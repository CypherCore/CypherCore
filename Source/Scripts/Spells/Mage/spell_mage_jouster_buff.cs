using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[SpellScript(195391)]
public class spell_mage_jouster_buff : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private void CalculateAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
	{
		Unit caster = GetCaster();
		if (caster == null)
		{
			return;
		}

		AuraEffect jousterRank = caster.GetAuraEffect(MageSpells.SPELL_MAGE_JOUSTER, 0);
		if (jousterRank != null)
		{
			amount = jousterRank.GetAmount();
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.ModDamagePercentTaken));
	}
}