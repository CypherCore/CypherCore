// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Monk;

[SpellScript(116095)]
public class aura_monk_disable : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		var damageInfo = eventInfo.GetDamageInfo();

		if (damageInfo != null)
			if ((damageInfo.GetAttackType() == WeaponAttackType.BaseAttack || damageInfo.GetAttackType() == WeaponAttackType.OffAttack) && damageInfo.GetAttacker() == GetCaster())
			{
				GetAura().RefreshDuration();

				return true;
			}

		return false;
	}
}