using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.AI;


// Very simple Target selector, will just skip main Target
// NOTE: When passing to UnitAI.SelectTarget remember to use 0 as position for random selection
//       because tank will not be in the temporary list

// Simple selector for units using mana
internal class FarthestTargetSelector : ICheck<Unit>
{
    private readonly float _dist;
    private readonly bool _inLos;
    private readonly Unit _me;
    private readonly bool _playerOnly;

    public FarthestTargetSelector(Unit unit, float dist, bool playerOnly, bool inLos)
    {
        _me = unit;
        _dist = dist;
        _playerOnly = playerOnly;
        _inLos = inLos;
    }

    public bool Invoke(Unit target)
    {
        if (_me == null ||
            target == null)
            return false;

        if (_playerOnly && target.GetTypeId() != TypeId.Player)
            return false;

        if (_dist > 0.0f &&
            !_me.IsWithinCombatRange(target, _dist))
            return false;

        if (_inLos && !_me.IsWithinLOSInMap(target))
            return false;

        return true;
    }
}