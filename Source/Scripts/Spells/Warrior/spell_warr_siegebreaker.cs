// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior
{
	//280772 - Siegebreaker
	[SpellScript(280772)]
	public class spell_warr_siegebreaker : SpellScript, ISpellOnHit
	{
		public void OnHit()
		{
			var caster = GetCaster();
			caster.CastSpell(null, WarriorSpells.SIEGEBREAKER_BUFF, true);
		}
	}
}