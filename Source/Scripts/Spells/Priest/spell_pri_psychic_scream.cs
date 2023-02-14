// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Priest;

[SpellScript(8122)]
public class spell_pri_psychic_scream : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		var target = eventInfo.GetActionTarget();

		if (target == null)
			return false;

		var dmg  = eventInfo.GetDamageInfo();
		var fear = GetAura();

		if (fear != null && dmg != null && dmg.GetDamage() > 0)
			fear.SetDuration(0);

		return true;
	}
}