// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.GameMath;
using System.Numerics;

namespace Game.Collision
{
    public class DynamicMapTree
    {
        public DynamicMapTree()
        {
            impl = new DynTreeImpl();
        }

        public void Insert(GameObjectModel mdl)
        {
            impl.Insert(mdl);
        }

        public void Remove(GameObjectModel mdl)
        {
            impl.Remove(mdl);
        }

        public bool Contains(GameObjectModel mdl)
        {
            return impl.Contains(mdl);
        }

        public void Balance()
        {
            impl.Balance();
        }

        public void Update(uint diff)
        {
            impl.Update(diff);
        }

        public bool GetIntersectionTime(Ray ray, Vector3 endPos, PhaseShift phaseShift, ref float maxDist)
        {
            float distance = maxDist;
            DynamicTreeIntersectionCallback callback = new(phaseShift);
            impl.IntersectRay(ray, callback, ref distance, endPos);
            if (callback.DidHit())
                maxDist = distance;
            return callback.DidHit();
        }

        public bool GetObjectHitPos(Vector3 startPos, Vector3 endPos, ref Vector3 resultHitPos, float modifyDist, PhaseShift phaseShift)
        {
            bool result;
            float maxDist = (endPos - startPos).Length();
            // valid map coords should *never ever* produce float overflow, but this would produce NaNs too
            Cypher.Assert(maxDist < float.MaxValue);
            // prevent NaN values which can cause BIH intersection to enter infinite loop
            if (maxDist < 1e-10f)
            {
                resultHitPos = endPos;
                return false;
            }
            Vector3 dir = (endPos - startPos) / maxDist;              // direction with length of 1
            Ray ray = new(startPos, dir);
            float dist = maxDist;
            if (GetIntersectionTime(ray, endPos, phaseShift, ref dist))
            {
                resultHitPos = startPos + dir * dist;
                if (modifyDist < 0)
                {
                    if ((resultHitPos - startPos).Length() > -modifyDist)
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

        public bool IsInLineOfSight(Vector3 startPos, Vector3 endPos, PhaseShift phaseShift)
        {
            float maxDist = (endPos - startPos).Length();

            if (!MathFunctions.fuzzyGt(maxDist, 0))
                return true;

            Ray r = new(startPos, (endPos - startPos) / maxDist);
            DynamicTreeIntersectionCallback callback = new(phaseShift);
            impl.IntersectRay(r, callback, ref maxDist, endPos);

            return !callback.DidHit();
        }

        public float GetHeight(float x, float y, float z, float maxSearchDist, PhaseShift phaseShift)
        {
            Vector3 v = new(x, y, z);
            Ray r = new(v, new Vector3(0, 0, -1));
            DynamicTreeIntersectionCallback callback = new(phaseShift);
            impl.IntersectZAllignedRay(r, callback, ref maxSearchDist);

            if (callback.DidHit())
                return v.Z - maxSearchDist;
            else
                return float.NegativeInfinity;
        }

        public bool GetAreaInfo(float x, float y, ref float z, PhaseShift phaseShift, out uint flags, out int adtId, out int rootId, out int groupId)
        {
            flags = 0;
            adtId = 0;
            rootId = 0;
            groupId = 0;

            Vector3 v = new(x, y, z + 0.5f);
            DynamicTreeAreaInfoCallback intersectionCallBack = new(phaseShift);
            impl.IntersectPoint(v, intersectionCallBack);
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

        public AreaAndLiquidData GetAreaAndLiquidData(float x, float y, float z, PhaseShift phaseShift, byte reqLiquidType)
        {
            AreaAndLiquidData data = new();

            Vector3 v = new(x, y, z + 0.5f);
            DynamicTreeLocationInfoCallback intersectionCallBack = new(phaseShift);
            impl.IntersectPoint(v, intersectionCallBack);
            if (intersectionCallBack.GetLocationInfo().hitModel != null)
            {
                data.floorZ = intersectionCallBack.GetLocationInfo().ground_Z;
                uint liquidType = intersectionCallBack.GetLocationInfo().hitModel.GetLiquidType();
                float liquidLevel = 0;
                if (reqLiquidType == 0 || (Global.DB2Mgr.GetLiquidFlags(liquidType) & reqLiquidType) != 0)
                    if (intersectionCallBack.GetHitModel().GetLiquidLevel(v, intersectionCallBack.GetLocationInfo(), ref liquidLevel))
                        data.liquidInfo = new AreaAndLiquidData.LiquidInfo(liquidType, liquidLevel);

                data.areaInfo = new(intersectionCallBack.GetHitModel().GetNameSetId(), intersectionCallBack.GetLocationInfo().rootId,
                    (int)intersectionCallBack.GetLocationInfo().hitModel.GetWmoID(), intersectionCallBack.GetLocationInfo().hitModel.GetMogpFlags());
            }

            return data;
        }
        
        DynTreeImpl impl;
    }

    public class DynTreeImpl : RegularGrid2D<GameObjectModel, BIHWrap<GameObjectModel>>
    {
        public DynTreeImpl()
        {
            rebalance_timer = new TimeTracker(200);
            unbalanced_times = 0;
        }

        public override void Insert(GameObjectModel mdl)
        {
            base.Insert(mdl);
            ++unbalanced_times;
        }

        public override void Remove(GameObjectModel mdl)
        {
            base.Remove(mdl);
            ++unbalanced_times;
        }

        public override void Balance()
        {
            base.Balance();
            unbalanced_times = 0;
        }

        public void Update(uint difftime)
        {
            if (Empty())
                return;

            rebalance_timer.Update(difftime);
            if (rebalance_timer.Passed())
            {
                rebalance_timer.Reset(200);
                if (unbalanced_times > 0)
                    Balance();
            }
        }

        TimeTracker rebalance_timer;
        int unbalanced_times;
    }
}
