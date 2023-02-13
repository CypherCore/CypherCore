﻿using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	[Script] // 34428 - Victory Rush
	internal class spell_warr_victory_rush : SpellScript, ISpellAfterCast
	{
		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(WarriorSpells.VICTORIOUS, WarriorSpells.VICTORY_RUSH_HEAL);
		}

		public void AfterCast()
		{
			var caster = GetCaster();

			caster.CastSpell(caster, WarriorSpells.VICTORY_RUSH_HEAL, true);
			caster.RemoveAurasDueToSpell(WarriorSpells.VICTORIOUS);
		}
	}
}