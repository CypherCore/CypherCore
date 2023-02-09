using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 26400 - Arcane Shroud
internal class spell_item_arcane_shroud : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.ModThreat));
	}

	private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
	{
		var diff = (int)GetUnitOwner().GetLevel() - 60;

		if (diff > 0)
			amount += 2 * diff;
	}
}