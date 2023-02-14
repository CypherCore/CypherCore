// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Linq;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Items;

[Script] // 29601 - Enlightenment (Pendant of the Violet Eye)
internal class spell_item_pendant_of_the_violet_eye : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		var spell = eventInfo.GetProcSpell();

		if (spell != null)
		{
			var costs = spell.GetPowerCost();
			var m     = costs.FirstOrDefault(cost => cost.Power == PowerType.Mana && cost.Amount > 0);

			if (m != null)
				return true;
		}

		return false;
	}
}