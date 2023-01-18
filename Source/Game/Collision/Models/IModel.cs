// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.GameMath;
using System.Numerics;

namespace Game.Collision
{
    public class IModel
    {
        public virtual Vector3 GetPosition() { return default; }
        public virtual AxisAlignedBox GetBounds() { return default; }

        public virtual bool IntersectRay(Ray ray, ref float maxDist, bool stopAtFirstHit, PhaseShift phaseShift, ModelIgnoreFlags ignoreFlags) { return false; }
        public virtual bool IntersectRay(Ray ray, ref float distance, bool stopAtFirstHit, ModelIgnoreFlags ignoreFlags) { return false; }
        public virtual bool IntersectRay(Ray ray, ref float distance, bool stopAtFirstHit) { return false; }
        public virtual void IntersectPoint(Vector3 point, AreaInfo info, PhaseShift phaseShift) { }
    }
}
