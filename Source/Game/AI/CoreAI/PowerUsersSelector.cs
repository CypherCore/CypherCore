using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.AI;

internal class PowerUsersSelector : ICheck<Unit>
{
	private readonly float _dist;
	private readonly Unit _me;
	private readonly bool _playerOnly;
	private readonly PowerType _power;

	public PowerUsersSelector(Unit unit, PowerType power, float dist, bool playerOnly)
	{
		_me         = unit;
		_power      = power;
		_dist       = dist;
		_playerOnly = playerOnly;
	}

	public bool Invoke(Unit target)
	{
		if (_me == null ||
		    target == null)
			return false;

		if (target.GetPowerType() != _power)
			return false;

		if (_playerOnly && target.GetTypeId() != TypeId.Player)
			return false;

		if (_dist > 0.0f &&
		    !_me.IsWithinCombatRange(target, _dist))
			return false;

		if (_dist < 0.0f &&
		    _me.IsWithinCombatRange(target, -_dist))
			return false;

		return true;
	}
}