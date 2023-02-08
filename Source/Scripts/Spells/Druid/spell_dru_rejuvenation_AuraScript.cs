using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(774)]
public class spell_dru_rejuvenation_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();



	//Posible Fixed


	// bool Validate(SpellInfo const* spellInfo) override
	// {
	//     return ValidateSpellInfo(
	//         {
	//             SPELL_DRUID_CULTIVATION,
	//             SPELL_DRUID_CULTIVATION_HOT,
	//             SPELL_DRUID_ABUNDANCE,
	//             SPELL_DRUID_ABUNDANCE_BUFF,
	//         });
	// }
	//
	// void AfterRemove(AuraEffect const* aurEff, AuraEffectHandleModes mode)
	// {
	//     if (Unit* caster = GetCaster())
	//         if (caster->HasAura(SPELL_DRUID_ABUNDANCE))
	//             if (Aura* abundanceBuff = caster->GetAura(SPELL_DRUID_ABUNDANCE_BUFF))
	//                 abundanceBuff->ModStackAmount(-1);
	// }
	//
	// void OnPeriodic(AuraEffect const* aurEff)
	// {
	//     if (Unit* target = GetTarget())
	//         if (GetCaster()->HasAura(SPELL_DRUID_CULTIVATION) && !target->HasAura(SPELL_DRUID_CULTIVATION_HOT) && target->HealthBelowPct(Global.SpellMgr->GetSpellInfo//(SPELL_DRUID_CULTIVATION)->GetEffect(0).BasePoints))
	//             GetCaster()->CastSpell(target, SPELL_DRUID_CULTIVATION_HOT, true);
	// }
	//
	// void CalculateAmount(AuraEffect const* aurEff, int32& amount, bool& canBeRecalculated)
	// {
	//     if (!GetCaster())
	//         return;
	//
	//     amount = MathFunctions.CalculatePct(GetCaster()->SpellBaseHealingBonusDone(SpellSchoolMask.Nature), 60);
	// }

	//Posible Fixed

	private struct Spells
	{
		public static uint GlyphofRejuvenation = 17076;
		public static uint GlyphofRejuvenationEffect = 96206;
	}
	private void HandleCalculateAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
	{
		Unit l_Caster = GetCaster();
		if (l_Caster != null)
		{
			///If soul of the forest is activated we increase the heal by 100%
			if (l_Caster.HasAura(SoulOfTheForestSpells.SPELL_DRUID_SOUL_OF_THE_FOREST_RESTO) && !l_Caster.HasAura(DruidSpells.SPELL_DRUID_REJUVENATION))
			{
				amount *= 2;
				l_Caster.RemoveAura(SoulOfTheForestSpells.SPELL_DRUID_SOUL_OF_THE_FOREST_RESTO);
			}
		}
	}

	private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		Unit caster = GetCaster();

		if (caster == null)
		{
			return;
		}

		AuraEffect GlyphOfRejuvenation = caster.GetAuraEffect(Spells.GlyphofRejuvenation, 0);
		if (GlyphOfRejuvenation != null)
		{
			GlyphOfRejuvenation.SetAmount(GlyphOfRejuvenation.GetAmount() + 1);
			if (GlyphOfRejuvenation.GetAmount() >= 3)
			{
				caster.CastSpell(caster, Spells.GlyphofRejuvenationEffect, true);
			}
		}
	}

	private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		Unit caster = GetCaster();

		if (caster == null)
		{
			return;
		}

		AuraEffect l_GlyphOfRejuvenation = caster.GetAuraEffect(Spells.GlyphofRejuvenation, 0);
		if (l_GlyphOfRejuvenation != null)
		{
			l_GlyphOfRejuvenation.SetAmount(l_GlyphOfRejuvenation.GetAmount() - 1);
			if (l_GlyphOfRejuvenation.GetAmount() < 3)
			{
				caster.RemoveAura(Spells.GlyphofRejuvenationEffect);
			}
		}
	}

	public override void Register()
	{
		// Posible Fixed
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.Real));
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
		AuraEffects.Add(new AuraEffectCalcAmountHandler(HandleCalculateAmount, 0, AuraType.PeriodicHeal));

		//  OnEffectPeriodic += AuraEffectPeriodicFn(spell_dru_rejuvenation::OnPeriodic, 0, AuraType.PeriodicHeal);
		//  DoEffectCalcAmount += AuraEffectCalcAmountFn(spell_dru_rejuvenation::CalculateAmount, 0, AuraType.PeriodicHeal);
		//  AfterEffectRemove += AuraEffectRemoveFn(spell_dru_rejuvenation::AfterRemove, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.Real);
	}
}