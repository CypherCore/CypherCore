// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(116849)]
public class spell_monk_life_cocoon : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	private void CalcAbsorb(AuraEffect UnnamedParameter, ref double amount, ref bool canBeRecalculated)
	{
		if (!GetCaster())
			return;

		var caster = GetCaster();

		//Formula:  [(((Spell power * 11) + 0)) * (1 + Versatility)]
		//Simplified to : [(Spellpower * 11)]
		//Versatility will be taken into account at a later date.
		amount            += caster.SpellBaseDamageBonusDone(GetSpellInfo().GetSchoolMask()) * 11;
		canBeRecalculated =  false;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAbsorb, 0, AuraType.SchoolAbsorb));
	}
}