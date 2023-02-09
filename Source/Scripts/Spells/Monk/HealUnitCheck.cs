using System.Collections.Generic;
using Game.Entities;

namespace Scripts.Spells.Monk;

public class HealUnitCheck : ICheck<WorldObject>
{
	public HealUnitCheck(Unit source)
	{
		this.m_source = source;
	}

	public bool Invoke(WorldObject @object)
	{
		var unit = @object.ToUnit();

		if (unit == null)
			return true;

		if (m_source.IsFriendlyTo(unit))
			return false;

		return true;
	}

	private readonly Unit m_source;
}