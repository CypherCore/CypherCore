// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Rogue;

[SpellScript(35551)]
public class spell_rogue_combat_potency_AuraScript : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		var offHand        = (eventInfo.GetDamageInfo().GetAttackType() == WeaponAttackType.OffAttack && RandomHelper.randChance(20));
		var mainRollChance = 20.0f * GetCaster().GetAttackTimer(WeaponAttackType.BaseAttack) / 1.4f / 600.0f;
		var mainHand       = (eventInfo.GetDamageInfo().GetAttackType() == WeaponAttackType.BaseAttack && RandomHelper.randChance(mainRollChance));

		return offHand || mainHand;
	}
}