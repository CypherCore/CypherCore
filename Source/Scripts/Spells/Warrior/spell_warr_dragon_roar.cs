// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
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