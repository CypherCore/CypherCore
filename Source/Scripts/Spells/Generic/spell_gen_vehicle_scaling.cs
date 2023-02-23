// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_vehicle_scaling : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Load()
	{
		return GetCaster() && GetCaster().IsTypeId(TypeId.Player);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.ModHealingPct));
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 1, AuraType.ModDamagePercentDone));
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 2, AuraType.ModIncreaseHealthPercent));
	}

	private void CalculateAmount(AuraEffect aurEff, ref double amount, ref bool canBeRecalculated)
	{
		var    caster = GetCaster();
		double  factor;
		ushort baseItemLevel;

		// @todo Reserach coeffs for different vehicles
		switch (GetId())
		{
			case GenericSpellIds.GearScaling:
				factor        = 1.0f;
				baseItemLevel = 205;

				break;
			default:
				factor        = 1.0f;
				baseItemLevel = 170;

				break;
		}

		var avgILvl = caster.ToPlayer().GetAverageItemLevel();

		if (avgILvl < baseItemLevel)
			return; // @todo Research possibility of scaling down

		amount = (int)((avgILvl - baseItemLevel) * factor);
	}
}