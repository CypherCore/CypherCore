// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved._hp
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps.Checks
{
    public class NearestHostileUnitCheck : ICheck<Unit>
    {
        private readonly bool _playerOnly;

        private readonly Creature _me;
        private float _range;

        public NearestHostileUnitCheck(Creature creature, float dist = 0, bool playerOnly = false)
        {
            _me = creature;
            _playerOnly = playerOnly;

            _range = dist == 0 ? 9999 : dist;
        }

        public bool Invoke(Unit u)
        {
            if (!_me.IsWithinDist(u, _range))
                return false;

            if (!_me.IsValidAttackTarget(u))
                return false;

            if (_playerOnly && !u.IsTypeId(TypeId.Player))
                return false;

            _range = _me.GetDistance(u); // use found unit range as new range limit for next check

            return true;
        }
    }
}