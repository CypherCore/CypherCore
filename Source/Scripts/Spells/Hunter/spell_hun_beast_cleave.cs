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
			if (player.HasAura(HunterSpells.SPELL_HUNTER_BEAST_CLEAVE_AURA))
			{
				var pet = player.GetPet();

				if (pet != null)
					player.CastSpell(pet, HunterSpells.SPELL_HUNTER_BEAST_CLEAVE_PROC, true);
			}
	}
}