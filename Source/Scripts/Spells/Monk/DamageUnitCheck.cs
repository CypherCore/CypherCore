using System.Collections.Generic;
using Game.Entities;

namespace Scripts.Spells.Monk;

public class DamageUnitCheck : ICheck<WorldObject>
{
	public DamageUnitCheck(Unit source, float range)
	{
		this.m_source = source;
		this.m_range  = range;
	}
	public bool Invoke(WorldObject @object)
	{
		Unit unit = @object.ToUnit();
		if (unit == null)
		{
			return true;
		}

		if (m_source.IsValidAttackTarget(unit) && unit.IsTargetableForAttack() && m_source.IsWithinDistInMap(unit, m_range))
		{
			m_range = m_source.GetDistance(unit);
			return false;
		}

		return true;
	}
	private readonly Unit m_source;
	private float m_range;
}