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