using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(69369)]
public class spell_dru_predatory_swiftness_aura : SpellScript, ISpellAfterHit
{
	public void AfterHit()
	{
		var player = GetCaster().ToPlayer();

		if (player != null)
			if (player.HasAura(PredatorySwiftnessSpells.SPELL_DRUID_PREDATORY_SWIFTNESS_AURA))
				player.RemoveAurasDueToSpell(PredatorySwiftnessSpells.SPELL_DRUID_PREDATORY_SWIFTNESS_AURA);
	}
}