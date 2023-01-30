// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved._hp
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Checks
{
    internal class AnyPlayerInPositionRangeCheck : ICheck<Player>
    {
        private readonly Position _pos;
        private readonly float _range;
        private readonly bool _reqAlive;

        public AnyPlayerInPositionRangeCheck(Position pos, float range, bool reqAlive = true)
        {
            _pos = pos;
            _range = range;
            _reqAlive = reqAlive;
        }

        public bool Invoke(Player u)
        {
            if (_reqAlive && !u.IsAlive())
                return false;

            if (!u.IsWithinDist3d(_pos, _range))
                return false;

            return true;
        }
    }
}