// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved._hp
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Checks
{
    public class AnyUnfriendlyUnitInObjectRangeCheck : ICheck<Unit>
    {
        private readonly Unit _funit;

        private readonly WorldObject _obj;
        private readonly float _range;

        public AnyUnfriendlyUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range)
        {
            _obj = obj;
            _funit = funit;
            _range = range;
        }

        public bool Invoke(Unit u)
        {
            if (u.IsAlive() &&
                _obj.IsWithinDist(u, _range) &&
                !_funit.IsFriendlyTo(u))
                return true;
            else
                return false;
        }
    }
}