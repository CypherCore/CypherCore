// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved._hp
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Checks
{
    public class AnyPlayerInObjectRangeCheck : ICheck<Player>
    {
        private readonly WorldObject _obj;
        private readonly float _range;
        private readonly bool _reqAlive;

        public AnyPlayerInObjectRangeCheck(WorldObject obj, float range, bool reqAlive = true)
        {
            _obj = obj;
            _range = range;
            _reqAlive = reqAlive;
        }

        public bool Invoke(Player pl)
        {
            if (_reqAlive && !pl.IsAlive())
                return false;

            if (!_obj.IsWithinDist(pl, _range))
                return false;

            return true;
        }
    }
}