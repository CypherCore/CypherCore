using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(102558)]
public class spell_dru_incarnation_guardian_of_ursoc : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		Player player = GetCaster().ToPlayer();
		if (player != null)
		{
			if (!player.HasAura(ShapeshiftFormSpells.SPELL_DRUID_BEAR_FORM))
			{
				player.CastSpell(player, ShapeshiftFormSpells.SPELL_DRUID_BEAR_FORM, true);
			}
		}
	}
}