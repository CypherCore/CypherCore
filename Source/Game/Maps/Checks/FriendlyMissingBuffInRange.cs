// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved._hp
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Checks
{
    public class FriendlyMissingBuffInRange : ICheck<Creature>
    {
        private readonly Unit _obj;
        private readonly float _range;
        private readonly uint _spell;

        public FriendlyMissingBuffInRange(Unit obj, float range, uint spellid)
        {
            _obj = obj;
            _range = range;
            _spell = spellid;
        }

        public bool Invoke(Creature u)
        {
            if (u.IsAlive() &&
                u.IsInCombat() &&
                !_obj.IsHostileTo(u) &&
                _obj.IsWithinDist(u, _range) &&
                !u.HasAura(_spell))
                return true;

            return false;
        }
    }
}