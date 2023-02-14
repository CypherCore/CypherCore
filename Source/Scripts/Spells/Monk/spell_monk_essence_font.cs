// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(191837)]
public class spell_monk_essence_font : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var caster = GetCaster();

		if (caster != null)
		{
			caster.AddAura(MonkSpells.SPELL_MONK_ESSENCE_FONT_PERIODIC_HEAL, null);
			var  u_li        = new List<Unit>();
			byte targetLimit = 6;
			u_li.RandomResize(targetLimit);
			caster.GetFriendlyUnitListInRange(u_li, 30.0f, false);

			foreach (var targets in u_li)
				caster.AddAura(MonkSpells.SPELL_MONK_ESSENCE_FONT_PERIODIC_HEAL, targets);
		}
	}
}