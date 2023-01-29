// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved._hp
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Checks
{
    internal class NearestHostileUnitInAttackDistanceCheck : ICheck<Unit>
    {
        private readonly bool _force;

        private readonly Creature _me;
        private float _range;

        public NearestHostileUnitInAttackDistanceCheck(Creature creature, float dist = 0)
        {
            _me = creature;
            _range = dist == 0 ? 9999 : dist;
            _force = dist != 0;
        }

        public bool Invoke(Unit u)
        {
            if (!_me.IsWithinDist(u, _range))
                return false;

            if (!_me.CanSeeOrDetect(u))
                return false;

            if (_force)
            {
                if (!_me.IsValidAttackTarget(u))
                    return false;
            }
            else if (!_me.CanStartAttack(u, false))
            {
                return false;
            }

            _range = _me.GetDistance(u); // use found unit range as new range limit for next check

            return true;
        }
    }
}