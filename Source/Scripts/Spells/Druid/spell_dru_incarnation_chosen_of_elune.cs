using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(102560)]
public class spell_dru_incarnation_chosen_of_elune : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		Player player = GetCaster().ToPlayer();
		if (player != null)
		{
			if (!player.HasAura(ShapeshiftFormSpells.SPELL_DRUID_MOONKIN_FORM))
			{
				player.CastSpell(player, ShapeshiftFormSpells.SPELL_DRUID_MOONKIN_FORM, true);
			}
		}
	}
}