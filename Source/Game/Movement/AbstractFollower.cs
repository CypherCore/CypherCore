// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
