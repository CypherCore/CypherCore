// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(2050)]
public class spell_pri_holy_word_serenity : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var player = GetCaster().ToPlayer();

		if (player != null)
			player.GetSpellHistory().ModifyCooldown(PriestSpells.HOLY_WORLD_SALVATION, TimeSpan.FromSeconds(-30000));
	}
}