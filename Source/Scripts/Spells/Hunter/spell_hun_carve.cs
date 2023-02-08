using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(187708)]
public class spell_hun_carve : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		Unit caster = GetCaster();
		Unit target = GetHitUnit();
		if (caster == null || target == null)
		{
			return;
		}

		if (caster.HasSpell(HunterSpells.SPELL_HUNTER_SERPENT_STING))
		{
			caster.CastSpell(target, HunterSpells.SPELL_HUNTER_SERPENT_STING_DAMAGE, true);
		}
	}
}