// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved._hp
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps.Checks
{
    // Success at unit in range, range update for next check (this can be use with CreatureLastSearcher to find nearest creature)
    internal class NearestCreatureEntryWithLiveStateInObjectRangeCheck : ICheck<Creature>
    {
        private readonly bool _alive;
        private readonly uint _entry;

        private readonly WorldObject _obj;
        private float _range;

        public NearestCreatureEntryWithLiveStateInObjectRangeCheck(WorldObject obj, uint entry, bool alive, float range)
        {
            _obj = obj;
            _entry = entry;
            _alive = alive;
            _range = range;
        }

        public bool Invoke(Creature u)
        {
            if (u.GetDeathState() != DeathState.Dead &&
                u.GetEntry() == _entry &&
                u.IsAlive() == _alive &&
                u.GetGUID() != _obj.GetGUID() &&
                _obj.IsWithinDist(u, _range) &&
                u.CheckPrivateObjectOwnerVisibility(_obj))
            {
                _range = _obj.GetDistance(u); // use found unit range as new range limit for next check

                return true;
            }

            return false;
        }
    }
}