﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(206977)]
public class spell_dk_blood_mirror : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private void CalcAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
	{
		amount = -1;
	}


	private void HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
	{
		absorbAmount = dmgInfo.GetDamage() * ((uint)aurEff.GetBaseAmount() / 100);
		var caster = GetCaster();
		var target = GetTarget();

		if (caster != null && target != null)
			caster.CastSpell(target, DeathKnightSpells.SPELL_DK_BLOOD_MIRROR_DAMAGE, (int)absorbAmount, true);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAmount, 1, AuraType.SchoolAbsorb));
		AuraEffects.Add(new AuraEffectAbsorbHandler(HandleAbsorb, 1));
	}
}