// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(102560)]
public class spell_dru_incarnation_chosen_of_elune : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var player = GetCaster().ToPlayer();

		if (player != null)
			if (!player.HasAura(ShapeshiftFormSpells.MOONKIN_FORM))
				player.CastSpell(player, ShapeshiftFormSpells.MOONKIN_FORM, true);
	}
}