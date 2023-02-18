// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(102359)]
public class spell_dru_mass_entanglement : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var targetList = new List<Unit>();
		GetCaster().GetAttackableUnitListInRange(targetList, 15.0f);

		if (targetList.Count != 0)
			foreach (var targets in targetList)
				GetCaster().AddAura(DruidSpells.MASS_ENTANGLEMENT, targets);
	}
}