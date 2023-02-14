// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warrior
{
	// Improved Whirlwind - 12950

	public class spell_warr_meat_cleaver : AuraScript, IAuraCheckProc
	{
		public bool CheckProc(ProcEventInfo UnnamedParameter)
		{
			return false;
		}
	}
}