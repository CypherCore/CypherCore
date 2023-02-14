// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;

namespace Scripts.Spells.Priest;

public class RaidCheck : ICheck<WorldObject>
{
	public RaidCheck(Unit caster)
	{
		this._caster = caster;
	}

	public bool Invoke(WorldObject obj)
	{
		var target = obj.ToUnit();

		if (target != null)
			return !_caster.IsInRaidWith(target);

		return true;
	}

	private readonly Unit _caster;
}