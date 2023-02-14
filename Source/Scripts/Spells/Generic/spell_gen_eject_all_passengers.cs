// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_eject_all_passengers : SpellScript, ISpellAfterHit
{
	public void AfterHit()
	{
		var vehicle = GetHitUnit().GetVehicleKit();

		if (vehicle)
			vehicle.RemoveAllPassengers();
	}
}