// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warrior
{
	// Taste for Blood - 206333
	[SpellScript(206333)]
	public class spell_warr_taste_for_blood : AuraScript, IAuraCheckProc
	{
		public bool CheckProc(ProcEventInfo eventInfo)
		{
			if ((eventInfo.GetHitMask() & ProcFlagsHit.Critical) != 0 && eventInfo.GetSpellInfo().Id == WarriorSpells.BLOODTHIRST_DAMAGE)
			{
				GetAura().SetDuration(0);

				return true;
			}

			return false;
		}
	}
}