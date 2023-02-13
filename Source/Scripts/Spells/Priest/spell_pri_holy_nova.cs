﻿using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(132157)]
public class spell_pri_holy_nova : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var caster = GetCaster();
		var target = GetHitUnit();

		if (caster == null || target == null)
			return;

		if (target != null)
			if (RandomHelper.randChance(20))
				caster.GetSpellHistory().ResetCooldown(PriestSpells.SPELL_PRIEST_HOLY_FIRE, true);
	}
}