using System.Collections.Generic;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(265202)]
public class spell_pri_holy_word_salvation : SpellScript, ISpellAfterCast
{
	public void AfterCast()
	{
		Unit caster = GetCaster();
		if (caster == null)
		{
			return;
		}
		var        eff          = GetEffectInfo(1);
		List<Unit> friendlyList = caster.GetPlayerListInGrid(40);
		foreach (var friendPlayers in friendlyList)
		{
			if (friendPlayers.IsFriendlyTo(caster))
			{
				caster.CastSpell(friendPlayers, PriestSpells.SPELL_PRIEST_RENEW, true);

				var prayer = friendPlayers.GetAura(PriestSpells.PrayerOfMendingAura);

				if (prayer != null)
				{
					prayer.ModStackAmount(eff.BasePoints);
				}
			}
		}
	}
}