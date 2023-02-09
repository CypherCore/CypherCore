using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(344862)]
public class spell_dh_chaos_strike_specless : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var caster = GetCaster();

		caster.CastSpell(DemonHunterSpells.SPELL_DH_CHAOS_STRIKE, true);
	}
}