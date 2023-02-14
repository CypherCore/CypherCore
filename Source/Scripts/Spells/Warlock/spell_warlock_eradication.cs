// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warlock
{
	// 196412 - Eradication
	[SpellScript(196412)]
	public class spell_warlock_eradication : AuraScript, IAuraCheckProc
	{
		public bool CheckProc(ProcEventInfo eventInfo)
		{
			return eventInfo.GetSpellInfo().Id == WarlockSpells.CHAOS_BOLT;
		}
	}
}