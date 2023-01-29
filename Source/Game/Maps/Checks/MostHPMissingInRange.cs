// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved._hp
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Checks
{
    public class MostHPMissingInRange<T> : ICheck<T> where T : Unit
    {
        private readonly Unit _obj;
        private readonly float _range;
        private ulong _hp;

        public MostHPMissingInRange(Unit obj, float range, uint hp)
        {
            _obj = obj;
            _range = range;
            _hp = hp;
        }

        public bool Invoke(T u)
        {
            if (u.IsAlive() &&
                u.IsInCombat() &&
                !_obj.IsHostileTo(u) &&
                _obj.IsWithinDist(u, _range) &&
                u.GetMaxHealth() - u.GetHealth() > _hp)
            {
                _hp = (uint)(u.GetMaxHealth() - u.GetHealth());

                return true;
            }

            return false;
        }
    }
}