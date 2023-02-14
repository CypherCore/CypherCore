// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_gift_of_the_harvester : SpellScript, ISpellCheckCast
{
	public SpellCastResult CheckCast()
	{
		List<TempSummon> ghouls = new();
		GetCaster().GetAllMinionsByEntry(ghouls, CreatureIds.Ghoul);

		if (ghouls.Count >= CreatureIds.MaxGhouls)
		{
			SetCustomCastResultMessage(SpellCustomErrors.TooManyGhouls);

			return SpellCastResult.CustomError;
		}

		return SpellCastResult.SpellCastOk;
	}
}