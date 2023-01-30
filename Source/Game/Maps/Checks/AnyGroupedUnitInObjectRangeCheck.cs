// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved._hp
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Checks
{
    public class AnyGroupedUnitInObjectRangeCheck : ICheck<Unit>
    {
        private readonly bool _playerOnly;
        private readonly bool _raid;
        private readonly float _range;
        private readonly Unit _refUnit;

        private readonly WorldObject _source;
        private readonly bool _incOwnRadius;
        private readonly bool _incTargetRadius;

        public AnyGroupedUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range, bool raid, bool playerOnly = false, bool incOwnRadius = true, bool incTargetRadius = true)
        {
            _source = obj;
            _refUnit = funit;
            _range = range;
            _raid = raid;
            _playerOnly = playerOnly;
            _incOwnRadius = incOwnRadius;
            _incTargetRadius = incTargetRadius;
        }

        public bool Invoke(Unit u)
        {
            if (_playerOnly && !u.IsPlayer())
                return false;

            if (_raid)
            {
                if (!_refUnit.IsInRaidWith(u))
                    return false;
            }
            else if (!_refUnit.IsInPartyWith(u))
            {
                return false;
            }

            if (_refUnit.IsHostileTo(u))
                return false;

            if (!u.IsAlive())
                return false;

            float searchRadius = _range;

            if (_incOwnRadius)
                searchRadius += _source.GetCombatReach();

            if (_incTargetRadius)
                searchRadius += u.GetCombatReach();

            return u.IsInMap(_source) && u.InSamePhase(_source) && u.IsWithinDoubleVerticalCylinder(_source, searchRadius, searchRadius);
        }
    }
}