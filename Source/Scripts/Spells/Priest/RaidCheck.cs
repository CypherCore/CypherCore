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