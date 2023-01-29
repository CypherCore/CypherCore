// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Numerics;

namespace Game.Collision
{
    public class LocationInfoCallback : WorkerCallback
    {
        private readonly LocationInfo _locInfo;

        private readonly ModelInstance[] _prims;
        public bool Result { get; set; }

        public LocationInfoCallback(ModelInstance[] val, LocationInfo info)
        {
            _prims = val;
            _locInfo = info;
            Result = false;
        }

        public override void Invoke(Vector3 point, uint entry)
        {
            if (_prims[entry] != null &&
                _prims[entry].GetLocationInfo(point, _locInfo))
                Result = true;
        }
    }
}