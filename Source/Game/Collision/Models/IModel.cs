/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.GameMath;
using System.Collections.Generic;
using Framework.Constants;

namespace Game.Collision
{
    public class IModel
    {
        public virtual Vector3 getPosition() { return default(Vector3); }
        public virtual AxisAlignedBox getBounds() { return default(AxisAlignedBox); }

        public virtual bool IntersectRay(Ray ray, ref float maxDist, bool stopAtFirstHit, PhaseShift phaseShift, ModelIgnoreFlags ignoreFlags) { return false; }
        public virtual bool IntersectRay(Ray ray, ref float distance, bool stopAtFirstHit, ModelIgnoreFlags ignoreFlags) { return false; }
        public virtual bool IntersectRay(Ray ray, ref float distance, bool stopAtFirstHit) { return false; }
        public virtual void IntersectPoint(Vector3 point, AreaInfo info, PhaseShift phaseShift) { }
    }
}
