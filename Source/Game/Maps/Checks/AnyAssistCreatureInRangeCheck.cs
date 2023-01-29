// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved._hp
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Checks
{
    internal class AnyAssistCreatureInRangeCheck : ICheck<Creature>
    {
        private readonly Unit _enemy;

        private readonly Unit _funit;
        private readonly float _range;

        public AnyAssistCreatureInRangeCheck(Unit funit, Unit enemy, float range)
        {
            _funit = funit;
            _enemy = enemy;
            _range = range;
        }

        public bool Invoke(Creature u)
        {
            if (u == _funit)
                return false;

            if (!u.CanAssistTo(_funit, _enemy))
                return false;

            // too far
            // Don't use combat reach distance, range must be an absolute value, otherwise the chain aggro range will be too big
            if (!_funit.IsWithinDist(u, _range, true, false, false))
                return false;

            // only if see assisted creature
            if (!_funit.IsWithinLOSInMap(u))
                return false;

            return true;
        }
    }
}