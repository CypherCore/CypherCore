// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Items;

[Script] // 64415 Val'anyr Hammer of Ancient Kings - Equip Effect
internal class spell_item_valanyr_hammer_of_ancient_kings : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		return eventInfo.GetHealInfo() != null && eventInfo.GetHealInfo().GetEffectiveHeal() > 0;
	}
}