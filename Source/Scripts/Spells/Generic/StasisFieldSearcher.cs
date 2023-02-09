using System.Collections.Generic;
using Game.Entities;

namespace Scripts.Spells.Generic;

internal class StasisFieldSearcher : ICheck<Unit>
{
	private readonly float _distance;
	private readonly Unit _unit;

	public StasisFieldSearcher(Unit obj, float distance)
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