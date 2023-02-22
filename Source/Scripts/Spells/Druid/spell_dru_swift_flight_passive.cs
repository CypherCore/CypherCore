// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	[Script] // 40121 - Swift Flight Form (Passive)
	internal class spell_dru_swift_flight_passive : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Load()
		{
			return GetCaster().IsTypeId(TypeId.Player);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 1, AuraType.ModIncreaseVehicleFlightSpeed));
		}

		private void CalculateAmount(AuraEffect aurEff, ref double amount, ref bool canBeRecalculated)
		{
			var caster = GetCaster().ToPlayer();

			if (caster != null)
				if (caster.GetSkillValue(SkillType.Riding) >= 375)
					amount = 310;
		}
	}
}