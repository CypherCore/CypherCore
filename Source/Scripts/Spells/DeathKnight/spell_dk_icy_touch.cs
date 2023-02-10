using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(49184)]
public class spell_dk_icy_touch : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var caster = GetCaster();
		var target = GetHitUnit();

		if (target != null)
		{
			if (caster.HasAura(152281))
				caster.CastSpell(target, 155159, true);
			else
				caster.CastSpell(target, DeathKnightSpells.SPELL_DK_FROST_FEVER, true);
		}
	}
}