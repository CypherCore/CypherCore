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
			player.GetSpellHistory().ModifyCooldown(PriestSpells.SPELL_PRIEST_HOLY_WORLD_SALVATION, TimeSpan.FromSeconds(-30000));
	}
}