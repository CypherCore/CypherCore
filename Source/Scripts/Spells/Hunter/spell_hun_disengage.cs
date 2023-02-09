using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(781)]
public class spell_hun_disengage : SpellScript, ISpellAfterCast
{
	public void AfterCast()
	{
		var player = GetCaster().ToPlayer();

		if (player != null)
		{
			var spec = player.GetPrimarySpecialization();

			if (player.HasSpell(HunterSpells.SPELL_HUNTER_POSTHAST))
				if (spec == TalentSpecialization.HunterMarksman || spec == TalentSpecialization.HunterBeastMastery)
				{
					player.RemoveMovementImpairingAuras(false);
					player.CastSpell(player, HunterSpells.SPELL_HUNTER_POSTHAST_SPEED, true);
				}
		}
	}
}