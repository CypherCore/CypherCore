// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps.Checks
{
    // Success at unit in range, range update for next check (this can be use with GameobjectLastSearcher to find nearest GO with a certain Type)
    internal class NearestGameObjectTypeInObjectRangeCheck : ICheck<GameObject>
    {
        private readonly WorldObject _obj;
        private readonly GameObjectTypes _type;
        private float _range;

        public NearestGameObjectTypeInObjectRangeCheck(WorldObject obj, GameObjectTypes type, float range)
        {
            _obj = obj;
            _type = type;
            _range = range;
        }

        public bool Invoke(GameObject go)
        {
            if (go.GetGoType() == _type &&
                _obj.IsWithinDist(go, _range))
            {
                _range = _obj.GetDistance(go); // use found GO range as new range limit for next check

                return true;
            }

            return false;
        }
    }
}