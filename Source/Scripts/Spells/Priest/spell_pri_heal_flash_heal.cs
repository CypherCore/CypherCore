using System;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(2060)]
public class spell_pri_heal_flash_heal : SpellScript, ISpellAfterCast
{
	public void AfterCast()
	{
		Player caster = GetCaster().ToPlayer();
		if (!caster.ToPlayer())
		{
			return;
		}

		if (caster.GetPrimarySpecialization() == TalentSpecialization.PriestHoly)
		{
			if (caster.GetSpellHistory().HasCooldown(PriestSpells.SPELL_PRIEST_HOLY_WORD_SERENITY))
			{
				caster.GetSpellHistory().ModifyCooldown(PriestSpells.SPELL_PRIEST_HOLY_WORD_SERENITY, TimeSpan.FromSeconds(-6 * Time.InMilliseconds));
			}
		}
	}
}