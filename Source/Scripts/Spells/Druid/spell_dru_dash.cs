﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	[Script] // 1850 - Dash
	public class spell_dru_dash : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.ModIncreaseSpeed));
		}

		private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
		{
			// do not set speed if not in cat form
			if (GetUnitOwner().GetShapeshiftForm() != ShapeShiftForm.CatForm)
				amount = 0;
		}
	}
}