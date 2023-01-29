// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved._hp
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Checks
{
    public class FriendlyBelowHpPctEntryInRange : ICheck<Unit>
    {
        private readonly uint _entry;
        private readonly bool _excludeSelf;

        private readonly Unit _obj;
        private readonly byte _pct;
        private readonly float _range;

        public FriendlyBelowHpPctEntryInRange(Unit obj, uint entry, float range, byte pct, bool excludeSelf)
        {
            _obj = obj;
            _entry = entry;
            _range = range;
            _pct = pct;
            _excludeSelf = excludeSelf;
        }

        public bool Invoke(Unit u)
        {
            if (_excludeSelf && _obj.GetGUID() == u.GetGUID())
                return false;

            if (u.GetEntry() == _entry &&
                u.IsAlive() &&
                u.IsInCombat() &&
                !_obj.IsHostileTo(u) &&
                _obj.IsWithinDist(u, _range) &&
                u.HealthBelowPct(_pct))
                return true;

            return false;
        }
    }
}