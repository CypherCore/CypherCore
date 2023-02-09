using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[Script] // 1943 - Rupture
internal class spell_rog_rupture_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(RogueSpells.VenomousWounds);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.PeriodicDummy));
		AuraEffects.Add(new AuraEffectApplyHandler(OnEffectRemoved, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}

	private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
	{
		Unit caster = GetCaster();

		if (caster != null)
		{
			canBeRecalculated = false;

			float[] attackpowerPerCombo =
			{
				0.0f, 0.015f, // 1 point:  ${($m1 + $b1*1 + 0.015 * $AP) * 4} Damage over 8 secs
				0.024f,       // 2 points: ${($m1 + $b1*2 + 0.024 * $AP) * 5} Damage over 10 secs
				0.03f,        // 3 points: ${($m1 + $b1*3 + 0.03 * $AP) * 6} Damage over 12 secs
				0.03428571f,  // 4 points: ${($m1 + $b1*4 + 0.03428571 * $AP) * 7} Damage over 14 secs
				0.0375f       // 5 points: ${($m1 + $b1*5 + 0.0375 * $AP) * 8} Damage over 16 secs
			};

			uint cp = caster.GetComboPoints();

			if (cp > 5)
				cp = 5;

			amount += (int)(caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * attackpowerPerCombo[cp]);
		}
	}

	private void OnEffectRemoved(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Death)
			return;

		Aura aura   = GetAura();
		Unit caster = aura.GetCaster();

		if (!caster)
			return;

		Aura auraVenomousWounds = caster.GetAura(RogueSpells.VenomousWounds);

		if (auraVenomousWounds == null)
			return;

		// Venomous Wounds: if unit dies while being affected by rupture, regain energy based on remaining duration
		SpellPowerCost cost = GetSpellInfo().CalcPowerCost(PowerType.Energy, false, caster, GetSpellInfo().GetSchoolMask(), null);

		if (cost == null)
			return;

		float pct         = (float)aura.GetDuration() / (float)aura.GetMaxDuration();
		int   extraAmount = (int)((float)cost.Amount * pct);
		caster.ModifyPower(PowerType.Energy, extraAmount);
	}
}