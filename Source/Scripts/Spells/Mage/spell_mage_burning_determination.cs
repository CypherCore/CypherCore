// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Mage;

[Script] // 198063 - Burning Determination
internal class spell_mage_burning_determination : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		var spellInfo = eventInfo.GetSpellInfo();

		if (spellInfo != null)
			if (spellInfo.GetAllEffectsMechanicMask().HasAnyFlag(((1u << (int)Mechanics.Interrupt) | (1 << (int)Mechanics.Silence))))
				return true;

		return false;
	}
}