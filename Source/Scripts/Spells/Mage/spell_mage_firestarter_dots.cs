// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 321712 - Pyroblast
internal class spell_mage_firestarter_dots : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.Firestarter);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcCritChanceHandler(CalcCritChance, SpellConst.EffectAll, AuraType.PeriodicDamage));
	}

	private void CalcCritChance(AuraEffect aurEff, Unit victim, ref double critChance)
	{
		var aurEff0 = GetCaster().GetAuraEffect(MageSpells.Firestarter, 0);

		if (aurEff0 != null)
			if (victim.GetHealthPct() >= aurEff0.GetAmount())
				critChance = 100.0f;
	}
}