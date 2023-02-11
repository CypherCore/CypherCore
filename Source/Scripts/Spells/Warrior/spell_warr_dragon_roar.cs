﻿using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior
{
	// 118000
	[SpellScript(118000)]
	public class spell_warr_dragon_roar : SpellScript, ISpellOnHit
	{
		public void OnHit()
		{
			var _player = GetCaster().ToPlayer();

			if (_player != null)
			{
				var target = GetHitUnit();

				if (target != null)
					_player.CastSpell(target, WarriorSpells.DRAGON_ROAR_KNOCK_BACK, true);
			}
		}
	}
}