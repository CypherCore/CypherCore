// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved._hp
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps.Checks
{
    public class AnyFriendlyUnitInObjectRangeCheck : ICheck<Unit>
    {
        private readonly Unit _funit;
        private readonly bool _incOwnRadius;
        private readonly bool _incTargetRadius;

        private readonly WorldObject _obj;
        private readonly bool _playerOnly;
        private readonly float _range;

        public AnyFriendlyUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range, bool playerOnly = false, bool incOwnRadius = true, bool incTargetRadius = true)
        {
            _obj = obj;
            _funit = funit;
            _range = range;
            _playerOnly = playerOnly;
            _incOwnRadius = incOwnRadius;
            _incTargetRadius = incTargetRadius;
        }

        public bool Invoke(Unit u)
        {
            if (!u.IsAlive())
                return false;

            float searchRadius = _range;

            if (_incOwnRadius)
                searchRadius += _obj.GetCombatReach();

            if (_incTargetRadius)
                searchRadius += u.GetCombatReach();

            if (!u.IsInMap(_obj) ||
                !u.InSamePhase(_obj) ||
                !u.IsWithinDoubleVerticalCylinder(_obj, searchRadius, searchRadius))
                return false;

            if (!_funit.IsFriendlyTo(u))
                return false;

            return !_playerOnly || u.GetTypeId() == TypeId.Player;
        }
    }
}