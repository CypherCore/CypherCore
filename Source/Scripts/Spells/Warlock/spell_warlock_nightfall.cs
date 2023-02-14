// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warlock
{
	// 108558 - Nightfall
	[SpellScript(108558)]
	public class spell_warlock_nightfall : AuraScript, IAuraOnProc, IAuraCheckProc
	{
		public void OnProc(ProcEventInfo UnnamedParameter)
		{
			GetCaster().CastSpell(GetCaster(), WarlockSpells.NIGHTFALL_BUFF, true);
		}

		public bool CheckProc(ProcEventInfo eventInfo)
		{
			return (eventInfo).GetSpellInfo().Id == WarlockSpells.CORRUPTION_DOT;
		}
	}
}