using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_replenishment_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Load()
	{
		return GetUnitOwner().GetPower(PowerType.Mana) != 0;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.PeriodicEnergize));
	}

	private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
	{
		switch (GetSpellInfo().Id)
		{
			case GenericSpellIds.Replenishment:
				amount = (int)(GetUnitOwner().GetMaxPower(PowerType.Mana) * 0.002f);

				break;
			case GenericSpellIds.InfiniteReplenishment:
				amount = (int)(GetUnitOwner().GetMaxPower(PowerType.Mana) * 0.0025f);

				break;
			default:
				break;
		}
	}
}