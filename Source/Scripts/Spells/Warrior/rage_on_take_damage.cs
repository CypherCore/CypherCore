// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IPlayer;

namespace Scripts.Spells.Warrior
{
	[Script]
	public class rage_on_take_damage : ScriptObjectAutoAddDBBound, IPlayerOnTakeDamage
	{
		public Class PlayerClass => Class.Warrior;

		public rage_on_take_damage() : base("rage_on_take_damage")
		{
		}

		public void OnPlayerTakeDamage(Player player, double amount, SpellSchoolMask schoolMask)
		{
			var rage = player.GetPower(PowerType.Rage);
			var mod  = 30;
			player.SetPower(PowerType.Rage, rage + mod);
		}
	}
}