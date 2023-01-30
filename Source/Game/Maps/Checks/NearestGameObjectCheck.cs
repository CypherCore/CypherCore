// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Checks
{
    internal class NearestGameObjectCheck : ICheck<GameObject>
    {
        private readonly WorldObject _obj;
        private float _range;

        public NearestGameObjectCheck(WorldObject obj)
        {
            _obj = obj;
            _range = 999;
        }

        public bool Invoke(GameObject go)
        {
            if (_obj.IsWithinDist(go, _range))
            {
                _range = _obj.GetDistance(go); // use found GO range as new range limit for next check

                return true;
            }

            return false;
        }
    }
}