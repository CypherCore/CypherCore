// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Generic;

[Script] // 36659 - Tail Sting
internal class spell_gen_decay_over_time_tail_sting_AuraScript : AuraScript, IAuraOnProc, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		return eventInfo.GetSpellInfo() == GetSpellInfo();
	}

	public void OnProc(ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		ModStackAmount(-1);
	}
}