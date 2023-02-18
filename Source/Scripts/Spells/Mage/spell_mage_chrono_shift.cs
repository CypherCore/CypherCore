// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Mage;

[SpellScript(235711)]
public class spell_mage_chrono_shift : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		var _spellCanProc = (eventInfo.GetSpellInfo().Id == MageSpells.ARCANE_BARRAGE || eventInfo.GetSpellInfo().Id == MageSpells.ARCANE_BARRAGE_TRIGGERED);

		if (_spellCanProc)
			return true;

		return false;
	}
}