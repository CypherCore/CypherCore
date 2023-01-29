// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.GameMath;

namespace Game.Collision
{
    public class DynamicTreeIntersectionCallback : WorkerCallback
    {
        private readonly PhaseShift _phaseShift;
        private bool _didHit;

        public DynamicTreeIntersectionCallback(PhaseShift phaseShift)
        {
            _didHit = false;
            _phaseShift = phaseShift;
        }

        public override bool Invoke(Ray r, IModel obj, ref float distance)
        {
            _didHit = obj.IntersectRay(r, ref distance, true, _phaseShift, ModelIgnoreFlags.Nothing);

            return _didHit;
        }

        public bool DidHit()
        {
            return _didHit;
        }
    }
}