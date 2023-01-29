// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.GameMath;

namespace Game.Collision
{
    public class MapRayCallback : WorkerCallback
    {
        private readonly ModelIgnoreFlags _flags;

        private readonly ModelInstance[] _prims;
        private bool _hit;

        public MapRayCallback(ModelInstance[] val, ModelIgnoreFlags ignoreFlags)
        {
            _prims = val;
            _hit = false;
            _flags = ignoreFlags;
        }

        public override bool Invoke(Ray ray, uint entry, ref float distance, bool pStopAtFirstHit = true)
        {
            if (_prims[entry] == null)
                return false;

            bool result = _prims[entry].IntersectRay(ray, ref distance, pStopAtFirstHit, _flags);

            if (result)
                _hit = true;

            return result;
        }

        public bool DidHit()
        {
            return _hit;
        }
    }
}