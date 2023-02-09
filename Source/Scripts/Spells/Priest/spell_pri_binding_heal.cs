using System;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(32546)]
public class spell_pri_binding_heal : SpellScript, ISpellAfterCast
{
	public void AfterCast()
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		if (caster.GetSpellHistory().HasCooldown(PriestSpells.SPELL_PRIEST_HOLY_WORD_SANCTIFY))
			caster.GetSpellHistory().ModifyCooldown(PriestSpells.SPELL_PRIEST_HOLY_WORD_SANCTIFY, TimeSpan.FromSeconds(-3 * Time.InMilliseconds));

		if (caster.GetSpellHistory().HasCooldown(PriestSpells.SPELL_PRIEST_HOLY_WORD_SERENITY))
			caster.GetSpellHistory().ModifyCooldown(PriestSpells.SPELL_PRIEST_HOLY_WORD_SERENITY, TimeSpan.FromSeconds(-3 * Time.InMilliseconds));
	}
}