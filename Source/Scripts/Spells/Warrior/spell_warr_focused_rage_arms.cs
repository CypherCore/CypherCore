// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warrior
{
	//207982 - Focused Rage
	[SpellScript(207982)]
	public class spell_warr_focused_rage_arms : AuraScript, IAuraCheckProc
	{
		public bool CheckProc(ProcEventInfo eventInfo)
		{
			return eventInfo.GetSpellInfo().Id == WarriorSpells.MORTAL_STRIKE;
		}
	}
}