using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(207311)]
public class spell_dk_clawing_shadows : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		Unit caster = GetCaster();
		Unit target = caster.ToPlayer().GetSelectedUnit();

		if (caster == null || target == null)
		{
			return;
		}

		caster.CastSpell(target, DeathKnightSpells.SPELL_DK_FESTERING_WOUND_DAMAGE, true);
	}
}