// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved._hp
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Checks
{
    internal class MostHPPercentMissingInRange : ICheck<Unit>
    {
        private readonly float _maxHpPct;
        private readonly float _minHpPct;
        private readonly Unit _obj;
        private readonly float _range;
        private float _hpPct;

        public MostHPPercentMissingInRange(Unit obj, float range, uint minHpPct, uint maxHpPct)
        {
            _obj = obj;
            _range = range;
            _minHpPct = minHpPct;
            _maxHpPct = maxHpPct;
            _hpPct = 101.0f;
        }

        public bool Invoke(Unit u)
        {
            if (u.IsAlive() &&
                u.IsInCombat() &&
                !_obj.IsHostileTo(u) &&
                _obj.IsWithinDist(u, _range) &&
                _minHpPct <= u.GetHealthPct() &&
                u.GetHealthPct() <= _maxHpPct &&
                u.GetHealthPct() < _hpPct)
            {
                _hpPct = u.GetHealthPct();

                return true;
            }

            return false;
        }
    }
}