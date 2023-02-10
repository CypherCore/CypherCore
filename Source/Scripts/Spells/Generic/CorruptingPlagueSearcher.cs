using System.Collections.Generic;
using Game.Entities;

namespace Scripts.Spells.Generic;

internal class CorruptingPlagueSearcher : ICheck<Unit>
{
	private readonly float _distance;

	private readonly Unit _unit;

	public CorruptingPlagueSearcher(Unit obj, float distance)
	{
		_unit     = obj;
		_distance = distance;
	}

	public bool Invoke(Unit u)
	{
		if (_unit.GetDistance2d(u) < _distance &&
		    (u.GetEntry() == CreatureIds.ApexisFlayer || u.GetEntry() == CreatureIds.ShardHideBoar || u.GetEntry() == CreatureIds.AetherRay) &&
		    !u.HasAura(GenericSpellIds.CorruptingPlague))
			return true;

		return false;
	}
}