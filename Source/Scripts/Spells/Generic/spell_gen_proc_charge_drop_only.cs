// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_proc_charge_drop_only : AuraScript, IAuraOnProc
{
	public void OnProc(ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
	}
}