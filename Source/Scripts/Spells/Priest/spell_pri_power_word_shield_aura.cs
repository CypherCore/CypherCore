// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(17)] // 17 - Power Word: Shield Aura
internal class spell_pri_power_word_shield_aura : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.BODY_AND_SOUL,
		                         PriestSpells.BODY_AND_SOUL_SPEED,
		                         PriestSpells.STRENGTH_OF_SOUL,
		                         PriestSpells.STRENGTH_OF_SOUL_EFFECT,
		                         PriestSpells.RENEWED_HOPE,
		                         PriestSpells.RENEWED_HOPE_EFFECT,
		                         PriestSpells.VOID_SHIELD,
		                         PriestSpells.VOID_SHIELD_EFFECT,
		                         PriestSpells.ATONEMENT,
		                         PriestSpells.TRINITY,
		                         PriestSpells.ATONEMENT_TRIGGERED,
		                         PriestSpells.ATONEMENT_TRIGGERED_POWER_TRINITY,
		                         PriestSpells.SHIELD_DISCIPLINE_PASSIVE,
		                         PriestSpells.SHIELD_DISCIPLINE_ENERGIZE,
		                         PriestSpells.RAPTURE,
		                         PriestSpells.MASTERY_GRACE);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleOnApply, 0, AuraType.SchoolAbsorb, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectAfterApply));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleOnRemove, 0, AuraType.SchoolAbsorb, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void CalculateAmount(AuraEffect auraEffect, ref double amount, ref bool canBeRecalculated)
	{
		canBeRecalculated = false;

		var caster = GetCaster();

		if (caster != null)
		{
			var amountF = caster.SpellBaseDamageBonusDone(GetSpellInfo().GetSchoolMask()) * 1.65f;

			var player = caster.ToPlayer();

			if (player != null)
			{
				MathFunctions.AddPct(ref amountF, player.GetRatingBonusValue(CombatRating.VersatilityDamageDone));

				var mastery = caster.GetAuraEffect(PriestSpells.MASTERY_GRACE, 0);

				if (mastery != null)
					if (GetUnitOwner().HasAura(PriestSpells.ATONEMENT_TRIGGERED) ||
					    GetUnitOwner().HasAura(PriestSpells.ATONEMENT_TRIGGERED_POWER_TRINITY))
						MathFunctions.AddPct(ref amountF, mastery.GetAmount());
			}

			var rapture = caster.GetAuraEffect(PriestSpells.RAPTURE, 1);

			if (rapture != null)
				MathFunctions.AddPct(ref amountF, rapture.GetAmount());

			amount = amountF;
		}
	}

	private void HandleOnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var caster = GetCaster();
		var target = GetTarget();

		if (!caster)
			return;

		if (caster.HasAura(PriestSpells.BODY_AND_SOUL))
			caster.CastSpell(target, PriestSpells.BODY_AND_SOUL_SPEED, true);

		if (caster.HasAura(PriestSpells.STRENGTH_OF_SOUL))
			caster.CastSpell(target, PriestSpells.STRENGTH_OF_SOUL_EFFECT, true);

		if (caster.HasAura(PriestSpells.RENEWED_HOPE))
			caster.CastSpell(target, PriestSpells.RENEWED_HOPE_EFFECT, true);

		if (caster.HasAura(PriestSpells.VOID_SHIELD) &&
		    caster == target)
			caster.CastSpell(target, PriestSpells.VOID_SHIELD_EFFECT, true);

		if (caster.HasAura(PriestSpells.ATONEMENT))
			caster.CastSpell(target, caster.HasAura(PriestSpells.TRINITY) ? PriestSpells.ATONEMENT_TRIGGERED_POWER_TRINITY : PriestSpells.ATONEMENT_TRIGGERED, true);
	}

	private void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		GetTarget().RemoveAura(PriestSpells.STRENGTH_OF_SOUL_EFFECT);
		var caster = GetCaster();

		if (caster)
			if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.EnemySpell &&
			    caster.HasAura(PriestSpells.SHIELD_DISCIPLINE_PASSIVE))
				caster.CastSpell(caster, PriestSpells.SHIELD_DISCIPLINE_ENERGIZE, true);
	}
}