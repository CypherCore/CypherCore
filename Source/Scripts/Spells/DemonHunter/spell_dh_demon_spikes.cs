using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(203720)]
public class spell_dh_demon_spikes : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var caster = GetCaster();
		caster.CastSpell(203819, true);
	}
}