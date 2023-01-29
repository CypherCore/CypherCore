// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved._hp
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Checks
{
    // Success at unit in range, range update for next check (this can be use with UnitLastSearcher to find nearest unit)
    public class NearestAttackableUnitInObjectRangeCheck : ICheck<Unit>
    {
        private readonly Unit _funit;

        private readonly WorldObject _obj;
        private float _range;

        public NearestAttackableUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range)
        {
            _obj = obj;
            _funit = funit;
            _range = range;
        }

        public bool Invoke(Unit u)
        {
            if (u.IsTargetableForAttack() &&
                _obj.IsWithinDist(u, _range) &&
                (_funit.IsInCombatWith(u) || _funit.IsHostileTo(u)) &&
                _obj.CanSeeOrDetect(u))
            {
                _range = _obj.GetDistance(u); // use found unit range as new range limit for next check

                return true;
            }

            return false;
        }
    }
}