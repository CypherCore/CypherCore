// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Dos
{
    public class CallOfHelpCreatureInRangeDo : IDoWork<Creature>
    {
        private readonly Unit _enemy;

        private readonly Unit _funit;
        private readonly float _range;

        public CallOfHelpCreatureInRangeDo(Unit funit, Unit enemy, float range)
        {
            _funit = funit;
            _enemy = enemy;
            _range = range;
        }

        public void Invoke(Creature u)
        {
            if (u == _funit)
                return;

            if (!u.CanAssistTo(_funit, _enemy, false))
                return;

            // too far
            // Don't use combat reach distance, range must be an absolute value, otherwise the chain aggro range will be too big
            if (!u.IsWithinDist(_funit, _range, true, false, false))
                return;

            // only if see assisted creature's enemy
            if (!u.IsWithinLOSInMap(_enemy))
                return;

            u.EngageWithTarget(_enemy);
        }
    }
}