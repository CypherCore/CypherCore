// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved._hp
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Checks
{
    public class AnyUnitInObjectRangeCheck : ICheck<Unit>
    {
        private readonly bool _check3D;

        private readonly WorldObject _obj;
        private readonly float _range;

        public AnyUnitInObjectRangeCheck(WorldObject obj, float range, bool check3D = true)
        {
            _obj = obj;
            _range = range;
            _check3D = check3D;
        }

        public bool Invoke(Unit u)
        {
            if (u.IsAlive() &&
                _obj.IsWithinDist(u, _range, _check3D))
                return true;

            return false;
        }
    }
}