// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Monk;

[SpellScript(195630)]
public class spell_monk_elusive_brawler_stacks : AuraScript
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.GetHitMask().HasFlag(ProcFlagsHit.Dodge))
			return false;

		var elusiveBrawler = GetCaster().GetAura(MonkSpells.SPELL_MONK_ELUSIVE_BRAWLER, GetCaster().GetGUID());

		if (elusiveBrawler != null)
			elusiveBrawler.SetDuration(0);

		return true;
	}
}