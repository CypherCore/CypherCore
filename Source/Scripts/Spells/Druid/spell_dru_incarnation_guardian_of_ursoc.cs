// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(102558)]
public class spell_dru_incarnation_guardian_of_ursoc : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var player = GetCaster().ToPlayer();

		if (player != null)
			if (!player.HasAura(ShapeshiftFormSpells.BEAR_FORM))
				player.CastSpell(player, ShapeshiftFormSpells.BEAR_FORM, true);
	}
}