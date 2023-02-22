// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;

namespace Scripts.Spells.Generic;

internal class StasisFieldSearcher : ICheck<Unit>
{
	private readonly double _distance;
	private readonly Unit _unit;

	public StasisFieldSearcher(Unit obj, double distance)
	{
		_unit     = obj;
		_distance = distance;
	}

	public bool Invoke(Unit u)
	{
		if (_unit.GetDistance2d(u) < _distance &&
		    (u.GetEntry() == CreatureIds.ApexisFlayer || u.GetEntry() == CreatureIds.ShardHideBoar || u.GetEntry() == CreatureIds.AetherRay || u.GetEntry() == CreatureIds.DaggertailLizard) &&
		    !u.HasAura(GenericSpellIds.StasisField))
			return true;

		return false;
	}
}