// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(115939)]
public class spell_hun_beast_cleave : SpellScript, ISpellAfterCast
{
	public void AfterCast()
	{
		var player = GetCaster().ToPlayer();

		if (player != null)
			if (player.HasAura(HunterSpells.BEAST_CLEAVE_AURA))
			{
				var pet = player.GetPet();

				if (pet != null)
					player.CastSpell(pet, HunterSpells.BEAST_CLEAVE_PROC, true);
			}
	}
}