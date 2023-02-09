using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(186270)]
public class spell_hun_raptor_strike : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var caster = GetCaster();
		var target = GetHitUnit();

		if (caster == null || target == null)
			return;

		if (caster.HasSpell(HunterSpells.SPELL_HUNTER_SERPENT_STING))
			caster.CastSpell(target, HunterSpells.SPELL_HUNTER_SERPENT_STING_DAMAGE, true);
	}
}