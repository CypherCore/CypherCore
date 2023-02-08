using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(263346)]
public class spell_pri_dark_void : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		Unit caster = GetCaster();
		Unit target = GetHitUnit();
		if (caster == null || target == null)
		{
			return;
		}

		caster.CastSpell(target, PriestSpells.SPELL_PRIEST_SHADOW_WORD_PAIN, true);
	}
}