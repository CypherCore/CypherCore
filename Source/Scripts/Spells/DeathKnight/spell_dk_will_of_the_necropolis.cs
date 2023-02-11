﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(206967)]
public class spell_dk_will_of_the_necropolis : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (Global.SpellMgr.GetSpellInfo(DeathKnightSpells.SPELL_DK_WILL_OF_THE_NECROPOLIS, Difficulty.None) != null)
			return false;

		return true;
	}

	private void CalculateAmount(AuraEffect UnnamedParameter, ref int p_Amount, ref bool UnnamedParameter2)
	{
		p_Amount = -1;
	}

	private void Absorb(AuraEffect UnnamedParameter, DamageInfo dmgInfo, ref uint absorbAmount)
	{
		absorbAmount = 0;

		if (GetTarget().GetHealthPct() < GetEffect(2).GetBaseAmount())
			absorbAmount = MathFunctions.CalculatePct(dmgInfo.GetDamage(), GetEffect(1).GetBaseAmount());
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
		AuraEffects.Add(new AuraEffectAbsorbHandler(Absorb, 0));
	}
}