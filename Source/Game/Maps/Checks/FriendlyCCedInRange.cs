// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved._hp
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps.Checks
{
    public class FriendlyCCedInRange : ICheck<Creature>
    {
        private readonly Unit _obj;
        private readonly float _range;

        public FriendlyCCedInRange(Unit obj, float range)
        {
            _obj = obj;
            _range = range;
        }

        public bool Invoke(Creature u)
        {
            if (u.IsAlive() &&
                u.IsInCombat() &&
                !_obj.IsHostileTo(u) &&
                _obj.IsWithinDist(u, _range) &&
                (u.IsFeared() || u.IsCharmed() || u.HasRootAura() || u.HasUnitState(UnitState.Stunned) || u.HasUnitState(UnitState.Confused)))
                return true;

            return false;
        }
    }
}