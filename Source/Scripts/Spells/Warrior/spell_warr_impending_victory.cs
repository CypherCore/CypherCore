// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	[Script] // 202168 - Impending Victory
	internal class spell_warr_impending_victory : SpellScript, ISpellAfterCast
	{
		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(WarriorSpells.IMPENDING_VICTORY_HEAL);
		}

		public void AfterCast()
		{
			var caster = GetCaster();
			caster.CastSpell(caster, WarriorSpells.IMPENDING_VICTORY_HEAL, true);
			caster.RemoveAurasDueToSpell(WarriorSpells.VICTORIOUS);
		}
	}
}