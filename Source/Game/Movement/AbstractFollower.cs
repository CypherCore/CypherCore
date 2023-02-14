// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Movement
{
    public class AbstractFollower
    {
        Unit _target;

        public AbstractFollower(Unit target = null) { SetTarget(target); }

        public void SetTarget(Unit unit)
        {
            if (unit == _target)
                return;

            if (_target)
                _target.FollowerRemoved(this);

            _target = unit;

            if (_target)
                _target.FollowerAdded(this);
        }

        public Unit GetTarget() { return _target; }
    }
}
