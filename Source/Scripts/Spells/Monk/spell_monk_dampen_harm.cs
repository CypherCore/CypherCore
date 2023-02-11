﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(122278)]
public class spell_monk_dampen_harm : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();
	private int healthPct;

	public override bool Load()
	{
		healthPct = GetSpellInfo().GetEffect(0).CalcValue(GetCaster());

		return GetUnitOwner().ToPlayer();
	}

	private void CalculateAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
	{
		amount = -1;
	}

	private void Absorb(AuraEffect auraEffect, DamageInfo dmgInfo, ref uint absorbAmount)
	{
		var target = GetTarget();
		var health = target.CountPctFromMaxHealth(healthPct);

		if (dmgInfo.GetDamage() < health)
			return;

		absorbAmount = (uint)(dmgInfo.GetDamage() * (GetSpellInfo().GetEffect(0).CalcValue(GetCaster()) / 100));
		auraEffect.GetBase().DropCharge();
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
		AuraEffects.Add(new AuraEffectAbsorbHandler(Absorb, 0));
	}
}