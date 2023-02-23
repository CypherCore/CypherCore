// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	[Script] // 1079 - Rip
	internal class spell_dru_rip : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Load()
		{
			var caster = GetCaster();

			return caster != null && caster.IsTypeId(TypeId.Player);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.PeriodicDamage));
		}

		private void CalculateAmount(AuraEffect aurEff, ref double amount, ref bool canBeRecalculated)
		{
			canBeRecalculated = false;

			var caster = GetCaster();

			if (caster != null)
			{
				// 0.01 * $AP * cp
				var cp = (byte)caster.ToPlayer().GetComboPoints();

				// Idol of Feral Shadows. Can't be handled as SpellMod due its dependency from CPs
				var idol = caster.GetAuraEffect(DruidSpellIds.IdolOfFeralShadows, 0);

				if (idol != null)
					amount += cp * idol.GetAmount();
				// Idol of Worship. Can't be handled as SpellMod due its dependency from CPs
				else if ((idol = caster.GetAuraEffect(DruidSpellIds.IdolOfWorship, 0)) != null)
					amount += cp * idol.GetAmount();

				amount += (int)MathFunctions.CalculatePct(caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack), cp);
			}
		}
	}
}