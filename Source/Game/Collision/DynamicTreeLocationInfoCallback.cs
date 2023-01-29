// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Numerics;

namespace Game.Collision
{
    public class DynamicTreeLocationInfoCallback : WorkerCallback
    {
        private readonly LocationInfo _locationInfo = new();

        private readonly PhaseShift _phaseShift;
        private GameObjectModel _hitModel = new();

        public DynamicTreeLocationInfoCallback(PhaseShift phaseShift)
        {
            _phaseShift = phaseShift;
        }

        public override void Invoke(Vector3 p, GameObjectModel obj)
        {
            if (obj.GetLocationInfo(p, _locationInfo, _phaseShift))
                _hitModel = obj;
        }

        public LocationInfo GetLocationInfo()
        {
            return _locationInfo;
        }

        public GameObjectModel GetHitModel()
        {
            return _hitModel;
        }
    }
}