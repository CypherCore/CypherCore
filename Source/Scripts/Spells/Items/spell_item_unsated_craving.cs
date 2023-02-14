// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Items;

[Script] // 71169 - Shadow's Fate (Shadowmourne questline)
internal class spell_item_unsated_craving : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo procInfo)
	{
		var caster = procInfo.GetActor();

		if (!caster ||
		    caster.GetTypeId() != TypeId.Player)
			return false;

		var target = procInfo.GetActionTarget();

		if (!target ||
		    target.GetTypeId() != TypeId.Unit ||
		    target.IsCritter() ||
		    (target.GetEntry() != CreatureIds.Sindragosa && target.IsSummon()))
			return false;

		return true;
	}
}