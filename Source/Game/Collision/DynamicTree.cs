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

namespace Game.Collision
{
    public class DynamicMapTree
    {
        public DynamicMapTree()
        {
            impl = new DynTreeImpl();
        }

        public void insert(GameObjectModel mdl)
        {
            impl.insert(mdl);
        }

        public void remove(GameObjectModel mdl)
        {
            impl.remove(mdl);
        }

        public bool contains(GameObjectModel mdl)
        {
            return impl.contains(mdl);
        }

        public void balance()
        {
            impl.balance();
        }

        public void update(uint diff)
        {
            impl.update(diff);
        }

        public bool getIntersectionTime(Ray ray, Vector3 endPos, PhaseShift phaseShift, float maxDist)
        {
            float distance = maxDist;
            DynamicTreeIntersectionCallback callback = new DynamicTreeIntersectionCallback(phaseShift);
            impl.intersectRay(ray, callback, ref distance, endPos);
            if (callback.didHit())
                maxDist = distance;
            return callback.didHit();
        }

        public bool getObjectHitPos(Vector3 startPos, Vector3 endPos, ref Vector3 resultHitPos, float modifyDist, PhaseShift phaseShift)
        {
            bool result = false;
            float maxDist = (endPos - startPos).magnitude();
            // valid map coords should *never ever* produce float overflow, but this would produce NaNs too
            Cypher.Assert(maxDist < float.MaxValue);
            // prevent NaN values which can cause BIH intersection to enter infinite loop
            if (maxDist < 1e-10f)
            {
                resultHitPos = endPos;
                return false;
            }
            Vector3 dir = (endPos - startPos) / maxDist;              // direction with length of 1
            Ray ray = new Ray(startPos, dir);
            float dist = maxDist;
            if (getIntersectionTime(ray, endPos, phaseShift, dist))
            {
                resultHitPos = startPos + dir * dist;
                if (modifyDist < 0)
                {
                    if ((resultHitPos - startPos).magnitude() > -modifyDist)
                        resultHitPos += dir * modifyDist;
                    else
                        resultHitPos = startPos;
                }
                else
                    resultHitPos += dir * modifyDist;

                result = true;
            }
            else
            {
                resultHitPos = endPos;
                result = false;
            }
            return result;
        }

        public bool isInLineOfSight(Vector3 startPos, Vector3 endPos, PhaseShift phaseShift)
        {
            float maxDist = (endPos - startPos).magnitude();

            if (!MathFunctions.fuzzyGt(maxDist, 0))
                return true;

            Ray r = new Ray(startPos, (endPos - startPos) / maxDist);
            DynamicTreeIntersectionCallback callback = new DynamicTreeIntersectionCallback(phaseShift);
            impl.intersectRay(r, callback, ref maxDist, endPos);

            return !callback.didHit();
        }

        public float getHeight(float x, float y, float z, float maxSearchDist, PhaseShift phaseShift)
        {
            Vector3 v = new Vector3(x, y, z + 0.5f);
            Ray r = new Ray(v, new Vector3(0, 0, -1));
            DynamicTreeIntersectionCallback callback = new DynamicTreeIntersectionCallback(phaseShift);
            impl.intersectZAllignedRay(r, callback, ref maxSearchDist);

            if (callback.didHit())
                return v.Z - maxSearchDist;
            else
                return float.NegativeInfinity;
        }

        public bool getAreaInfo(float x, float y, ref float z, PhaseShift phaseShift, out uint flags, out int adtId, out int rootId, out int groupId)
        {
            flags = 0;
            adtId = 0;
            rootId = 0;
            groupId = 0;

            Vector3 v = new Vector3(x, y, z + 0.5f);
            DynamicTreeAreaInfoCallback intersectionCallBack = new DynamicTreeAreaInfoCallback(phaseShift);
            impl.intersectPoint(v, intersectionCallBack);
            if (intersectionCallBack.GetAreaInfo().result)
            {
                flags = intersectionCallBack.GetAreaInfo().flags;
                adtId = intersectionCallBack.GetAreaInfo().adtId;
                rootId = intersectionCallBack.GetAreaInfo().rootId;
                groupId = intersectionCallBack.GetAreaInfo().groupId;
                z = intersectionCallBack.GetAreaInfo().ground_Z;
                return true;
            }
            return false;
        }

        DynTreeImpl impl;
    }

    public class DynTreeImpl : RegularGrid2D<GameObjectModel, BIHWrap<GameObjectModel>>
    {
        public DynTreeImpl()
        {
            rebalance_timer = new TimeTrackerSmall(200);
            unbalanced_times = 0;
        }

        public override void insert(GameObjectModel mdl)
        {
            base.insert(mdl);
            ++unbalanced_times;
        }

        public override void remove(GameObjectModel mdl)
        {
            base.remove(mdl);
            ++unbalanced_times;
        }

        public override void balance()
        {
            base.balance();
            unbalanced_times = 0;
        }

        public void update(uint difftime)
        {
            if (empty())
                return;

            rebalance_timer.Update((int)difftime);
            if (rebalance_timer.Passed())
            {
                rebalance_timer.Reset(200);
                if (unbalanced_times > 0)
                    balance();
            }
        }

        TimeTrackerSmall rebalance_timer;
        int unbalanced_times;
    }
}
