// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Numerics;

namespace Game.Collision
{
    public class DynamicTreeAreaInfoCallback : WorkerCallback
    {
        private readonly AreaInfo _areaInfo;

        private readonly PhaseShift _phaseShift;

        public DynamicTreeAreaInfoCallback(PhaseShift phaseShift)
        {
            _phaseShift = phaseShift;
            _areaInfo = new AreaInfo();
        }

        public override void Invoke(Vector3 p, GameObjectModel obj)
        {
            obj.IntersectPoint(p, _areaInfo, _phaseShift);
        }

        public AreaInfo GetAreaInfo()
        {
            return _areaInfo;
        }
    }
}