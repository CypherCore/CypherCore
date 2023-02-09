using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(344865)]
public class spell_dh_fel_rush_specless : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var caster = GetCaster();

		caster.CastSpell(DemonHunterSpells.SPELL_DH_FEL_RUSH, true);
	}
}