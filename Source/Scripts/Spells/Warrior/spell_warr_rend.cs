// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	// 94009 - Rend
	[SpellScript(94009)]
	public class spell_warr_rend : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

		private void CalculateAmount(AuraEffect aurEff, ref double amount, ref bool canBeRecalculated)
		{
			var caster = GetCaster();

			if (caster != null)
			{
				canBeRecalculated = false;

				// $0.25 * (($MWB + $mwb) / 2 + $AP / 14 * $MWS) bonus per tick
				var ap     = caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack);
				var mws    = caster.GetAttackTimer(WeaponAttackType.BaseAttack);
				var mwbMin = caster.GetWeaponDamageRange(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage);
				var mwbMax = caster.GetWeaponDamageRange(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage);
				var mwb    = ((mwbMin + mwbMax) / 2 + ap * mws / 14000) * 0.266f;
				amount += (int)caster.ApplyEffectModifiers(GetSpellInfo(), aurEff.GetEffIndex(), mwb);
			}
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 1, AuraType.PeriodicDamage));
		}
	}
}