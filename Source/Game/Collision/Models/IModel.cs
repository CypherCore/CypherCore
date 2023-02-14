// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
