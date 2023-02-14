// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warrior
{
	// Enrage - 184361
	[SpellScript(184361)]
	public class spell_warr_enrage : AuraScript, IAuraCheckProc
	{
		public bool CheckProc(ProcEventInfo eventInfo)
		{
			if (eventInfo.GetSpellInfo().Id == WarriorSpells.BLOODTHIRST_DAMAGE && (eventInfo.GetHitMask() & ProcFlagsHit.Critical) != 0)
				return true;

			return false;
		}
	}
}