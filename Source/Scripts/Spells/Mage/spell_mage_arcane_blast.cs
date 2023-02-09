using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[SpellScript(30451)]
public class spell_mage_arcane_blast : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var caster = GetCaster();

		if (caster != null)
		{
			var threes = caster.GetAura(MageSpells.SPELL_MAGE_RULE_OF_THREES_BUFF);

			if (threes != null)
				threes.Remove();
		}
	}
}