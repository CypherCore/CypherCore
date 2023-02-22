// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 11366 - Pyroblast
internal class spell_mage_firestarter : SpellScript, ISpellCalcCritChance
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.Firestarter);
	}

	public void CalcCritChance(Unit victim, ref double critChance)
	{
		var aurEff = GetCaster().GetAuraEffect(MageSpells.Firestarter, 0);

		if (aurEff != null)
			if (victim.GetHealthPct() >= aurEff.GetAmount())
				critChance = 100.0f;
	}
}