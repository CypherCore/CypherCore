// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps.Checks
{
    // Find the nearest Fishing hole and return true only if source object is in range of hole
    internal class NearestGameObjectFishingHole : ICheck<GameObject>
    {
        private readonly WorldObject _obj;
        private float _range;

        public NearestGameObjectFishingHole(WorldObject obj, float range)
        {
            _obj = obj;
            _range = range;
        }

        public bool Invoke(GameObject go)
        {
            if (go.GetGoInfo().type == GameObjectTypes.FishingHole &&
                go.IsSpawned() &&
                _obj.IsWithinDist(go, _range) &&
                _obj.IsWithinDist(go, go.GetGoInfo().FishingHole.radius))
            {
                _range = _obj.GetDistance(go);

                return true;
            }

            return false;
        }
    }
}