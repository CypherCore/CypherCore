// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.DeathKnight;

[Script] // 70656 - Advantage (T10 4P Melee Bonus)
internal class spell_dk_advantage_t10_4p : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		var caster = eventInfo.GetActor();

		if (caster)
		{
			var player = caster.ToPlayer();

			if (!player ||
			    caster.GetClass() != Class.Deathknight)
				return false;

			for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
				if (player.GetRuneCooldown(i) == 0)
					return false;

			return true;
		}

		return false;
	}
}