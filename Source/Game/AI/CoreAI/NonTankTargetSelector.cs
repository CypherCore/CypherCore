using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.AI;

public class NonTankTargetSelector : ICheck<Unit>
{
    private readonly bool _playerOnly;
    private readonly Unit _source;

    public NonTankTargetSelector(Unit source, bool playerOnly = true)
    {
        _source = source;
        _playerOnly = playerOnly;
    }

    public bool Invoke(Unit target)
    {
        if (target == null)
            return false;

        if (_playerOnly && !target.IsTypeId(TypeId.Player))
            return false;

        Unit currentVictim = _source.GetThreatManager().GetCurrentVictim();

        if (currentVictim != null)
            return target != currentVictim;

        return target != _source.GetVictim();
    }
}