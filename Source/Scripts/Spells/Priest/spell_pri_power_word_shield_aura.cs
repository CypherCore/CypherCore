using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 17 - Power Word: Shield Aura
internal class spell_pri_power_word_shield_aura : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.BodyAndSoul,
		                         PriestSpells.BodyAndSoulSpeed,
		                         PriestSpells.StrengthOfSoul,
		                         PriestSpells.StrengthOfSoulEffect,
		                         PriestSpells.RenewedHope,
		                         PriestSpells.RenewedHopeEffect,
		                         PriestSpells.VoidShield,
		                         PriestSpells.VoidShieldEffect,
		                         PriestSpells.Atonement,
		                         PriestSpells.Trinity,
		                         PriestSpells.AtonementTriggered,
		                         PriestSpells.AtonementTriggeredPowerTrinity,
		                         PriestSpells.ShieldDisciplinePassive,
		                         PriestSpells.ShieldDisciplineEnergize,
		                         PriestSpells.Rapture,
		                         PriestSpells.MasteryGrace);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleOnApply, 0, AuraType.SchoolAbsorb, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectAfterApply));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleOnRemove, 0, AuraType.SchoolAbsorb, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void CalculateAmount(AuraEffect auraEffect, ref int amount, ref bool canBeRecalculated)
	{
		canBeRecalculated = false;

		Unit caster = GetCaster();

		if (caster != null)
		{
			float amountF = caster.SpellBaseDamageBonusDone(GetSpellInfo().GetSchoolMask()) * 1.65f;

			Player player = caster.ToPlayer();

			if (player != null)
			{
				MathFunctions.AddPct(ref amountF, player.GetRatingBonusValue(CombatRating.VersatilityDamageDone));

				AuraEffect mastery = caster.GetAuraEffect(PriestSpells.MasteryGrace, 0);

				if (mastery != null)
					if (GetUnitOwner().HasAura(PriestSpells.AtonementTriggered) ||
					    GetUnitOwner().HasAura(PriestSpells.AtonementTriggeredPowerTrinity))
						MathFunctions.AddPct(ref amountF, mastery.GetAmount());
			}

			AuraEffect rapture = caster.GetAuraEffect(PriestSpells.Rapture, 1);

			if (rapture != null)
				MathFunctions.AddPct(ref amountF, rapture.GetAmount());

			amount = (int)amountF;
		}
	}

	private void HandleOnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		Unit caster = GetCaster();
		Unit target = GetTarget();

		if (!caster)
			return;

		if (caster.HasAura(PriestSpells.BodyAndSoul))
			caster.CastSpell(target, PriestSpells.BodyAndSoulSpeed, true);

		if (caster.HasAura(PriestSpells.StrengthOfSoul))
			caster.CastSpell(target, PriestSpells.StrengthOfSoulEffect, true);

		if (caster.HasAura(PriestSpells.RenewedHope))
			caster.CastSpell(target, PriestSpells.RenewedHopeEffect, true);

		if (caster.HasAura(PriestSpells.VoidShield) &&
		    caster == target)
			caster.CastSpell(target, PriestSpells.VoidShieldEffect, true);

		if (caster.HasAura(PriestSpells.Atonement))
			caster.CastSpell(target, caster.HasAura(PriestSpells.Trinity) ? PriestSpells.AtonementTriggeredPowerTrinity : PriestSpells.AtonementTriggered, true);
	}

	private void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		GetTarget().RemoveAura(PriestSpells.StrengthOfSoulEffect);
		Unit caster = GetCaster();

		if (caster)
			if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.EnemySpell &&
			    caster.HasAura(PriestSpells.ShieldDisciplinePassive))
				caster.CastSpell(caster, PriestSpells.ShieldDisciplineEnergize, true);
	}
}