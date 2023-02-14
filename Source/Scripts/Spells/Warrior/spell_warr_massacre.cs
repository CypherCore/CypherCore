// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warrior
{
	//206315
	[SpellScript(206315)]
	public class spell_warr_massacre : AuraScript, IAuraCheckProc
	{
		public bool CheckProc(ProcEventInfo procInfo)
		{
			if (procInfo.GetSpellInfo().Id == WarriorSpells.EXECUTE)
				if ((procInfo.GetHitMask() & ProcFlagsHit.Critical) != 0)
					return true;

			return false;
		}
	}
}