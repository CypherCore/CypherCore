// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(209426)]
public class spell_dh_darkness_absorb : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();


	private void CalculateAmount(AuraEffect UnnamedParameter, ref double amount, ref bool UnnamedParameter2)
	{
		amount = -1;
	}

	private void OnAbsorb(AuraEffect UnnamedParameter, DamageInfo dmgInfo, ref double absorbAmount)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		var chance = GetSpellInfo().GetEffect(1).BasePoints + caster.GetAuraEffectAmount(ShatteredSoulsSpells.COVER_OF_DARKNESS, 0);

		if (RandomHelper.randChance(chance))
			absorbAmount = dmgInfo.GetDamage();
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectAbsorbHandler(OnAbsorb, 0));
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
	}
}