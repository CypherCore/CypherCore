// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Druid;

[SpellScript(159286)]
public class spell_dru_primal_fury : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		var _spellCanProc = (eventInfo.GetSpellInfo().Id == DruidSpells.SHRED || eventInfo.GetSpellInfo().Id == DruidSpells.RAKE || eventInfo.GetSpellInfo().Id == DruidSpells.SWIPE_CAT || eventInfo.GetSpellInfo().Id == DruidSpells.MOONFIRE_CAT);

		if ((eventInfo.GetHitMask() & ProcFlagsHit.Critical) != 0 && _spellCanProc)
			return true;

		return false;
	}
}