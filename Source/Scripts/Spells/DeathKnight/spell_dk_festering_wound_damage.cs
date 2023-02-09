using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(194311)]
public class spell_dk_festering_wound_damage : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		if (GetCaster().HasAura(DeathKnightSpells.SPELL_DK_PESTILENT_PUSTULES) && RandomHelper.randChance(10))
			GetCaster().CastSpell(null, DeathKnightSpells.SPELL_DK_RUNIC_CORRUPTION_MOD_RUNES, true);
	}
}