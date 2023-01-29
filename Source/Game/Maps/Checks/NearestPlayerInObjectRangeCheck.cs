// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved._hp
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Checks
{
    internal class NearestPlayerInObjectRangeCheck : ICheck<Player>
    {
        private readonly WorldObject _obj;
        private float _range;

        public NearestPlayerInObjectRangeCheck(WorldObject obj, float range)
        {
            _obj = obj;
            _range = range;
        }

        public bool Invoke(Player pl)
        {
            if (pl.IsAlive() &&
                _obj.IsWithinDist(pl, _range))
            {
                _range = _obj.GetDistance(pl);

                return true;
            }

            return false;
        }
    }
}