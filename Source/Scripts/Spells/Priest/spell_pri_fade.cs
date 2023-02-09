using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(586)]
public class spell_pri_fade : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var caster = GetCaster();

		if (caster.HasAura(159628)) // Glyph of Mass dispel
			caster.CastSpell(caster, 159630, true);
	}
}