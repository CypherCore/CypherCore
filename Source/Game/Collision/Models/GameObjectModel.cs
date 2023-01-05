/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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

using Framework.Constants;
using Framework.GameMath;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace Game.Collision
{
    public class StaticModelList
    {
        public static Dictionary<uint, GameobjectModelData> models = new();
    }

    public abstract class GameObjectModelOwnerBase
    {
        public abstract bool IsSpawned();
        public abstract uint GetDisplayId();
        public abstract byte GetNameSetId();
        public abstract bool IsInPhase(PhaseShift phaseShift);
        public abstract Vector3 GetPosition();
        public abstract Quaternion GetRotation();
        public abstract float GetScale();
    }

    public class GameObjectModel : IModel
    {
        bool Initialize(GameObjectModelOwnerBase modelOwner)
        {
            var modelData = StaticModelList.models.LookupByKey(modelOwner.GetDisplayId());
            if (modelData == null)
                return false;

            AxisAlignedBox mdl_box = new(modelData.bound);
            // ignore models with no bounds
            if (mdl_box == AxisAlignedBox.Zero())
            {
                Log.outError(LogFilter.Server, "GameObject model {0} has zero bounds, loading skipped", modelData.name);
                return false;
            }

            iModel = Global.VMapMgr.AcquireModelInstance(modelData.name);

            if (iModel == null)
                return false;

            iPos = modelOwner.GetPosition();
            iScale = modelOwner.GetScale();
            iInvScale = 1.0f / iScale;

            Matrix4x4 iRotation = modelOwner.GetRotation().ToMatrix();
            iRotation.Inverse(out iInvRot);
            // transform bounding box:
            mdl_box = new AxisAlignedBox(mdl_box.Lo * iScale, mdl_box.Hi * iScale);
            AxisAlignedBox rotated_bounds = new();
            for (int i = 0; i < 8; ++i)
                rotated_bounds.merge(iRotation.Multiply(mdl_box.corner(i)));

            iBound = rotated_bounds + iPos;
            owner = modelOwner;
            isWmo = modelData.isWmo;
            return true;
        }

        public static GameObjectModel Create(GameObjectModelOwnerBase modelOwner)
        {
            GameObjectModel mdl = new();
            if (!mdl.Initialize(modelOwner))
                return null;

            return mdl;
        }

        public override bool IntersectRay(Ray ray, ref float maxDist, bool stopAtFirstHit, PhaseShift phaseShift, ModelIgnoreFlags ignoreFlags)
        {
            if (!IsCollisionEnabled() || !owner.IsSpawned())
                return false;

            if (!owner.IsInPhase(phaseShift))
                return false;

            float time = ray.intersectionTime(iBound);
            if (time == float.PositiveInfinity)
                return false;

            // child bounds are defined in object space:
            Vector3 p = iInvRot.Multiply(ray.Origin - iPos) * iInvScale;
            Ray modRay = new Ray(p, iInvRot.Multiply(ray.Direction));
            float distance = maxDist * iInvScale;
            bool hit = iModel.IntersectRay(modRay, ref distance, stopAtFirstHit, ignoreFlags);
            if (hit)
            {
                distance *= iScale;
                maxDist = distance;
            }
            return hit;
        }

        public override void IntersectPoint(Vector3 point, AreaInfo info, PhaseShift phaseShift)
        {
            if (!IsCollisionEnabled() || !owner.IsSpawned() || !IsMapObject())
                return;

            if (!owner.IsInPhase(phaseShift))
                return;

            if (!iBound.contains(point))
                return;

            // child bounds are defined in object space:
            Vector3 pModel = iInvRot.Multiply(point - iPos) * iInvScale;
            Vector3 zDirModel = iInvRot.Multiply(new Vector3(0.0f, 0.0f, -1.0f));
            float zDist;
            if (iModel.IntersectPoint(pModel, zDirModel, out zDist, info))
            {
                Vector3 modelGround = pModel + zDist * zDirModel;
                float world_Z = (iInvRot.Multiply(modelGround) * iScale + iPos).Z;
                if (info.ground_Z < world_Z)
                {
                    info.ground_Z = world_Z;
                    info.adtId = owner.GetNameSetId();
                }
            }
        }

        public bool GetLocationInfo(Vector3 point, LocationInfo info, PhaseShift phaseShift)
        {
            if (!IsCollisionEnabled() || !owner.IsSpawned() || !IsMapObject())
                return false;

            if (!owner.IsInPhase(phaseShift))
                return false;

            if (!iBound.contains(point))
                return false;

            // child bounds are defined in object space:
            Vector3 pModel = iInvRot.Multiply(point - iPos) * iInvScale;
            Vector3 zDirModel = iInvRot.Multiply(new Vector3(0.0f, 0.0f, -1.0f));
            float zDist;

            GroupLocationInfo groupInfo = new();
            if (iModel.GetLocationInfo(pModel, zDirModel, out zDist, groupInfo))
            {
                Vector3 modelGround = pModel + zDist * zDirModel;
                float world_Z = (iInvRot.Multiply(modelGround) * iScale + iPos).Z;
                if (info.ground_Z < world_Z)
                {
                    info.ground_Z = world_Z;
                    return true;
                }
            }

            return false;
        }

        public bool GetLiquidLevel(Vector3 point, LocationInfo info, ref float liqHeight)
        {
            // child bounds are defined in object space:
            Vector3 pModel = iInvRot.Multiply(point - iPos) * iInvScale;
            //Vector3 zDirModel = iInvRot * Vector3(0.f, 0.f, -1.f);
            float zDist;
            if (info.hitModel.GetLiquidLevel(pModel, out zDist))
            {
                // calculate world height (zDist in model coords):
                // assume WMO not tilted (wouldn't make much sense anyway)
                liqHeight = zDist * iScale + iPos.Z;
                return true;
            }
            return false;
        }
        
        public bool UpdatePosition()
        {
            if (iModel == null)
                return false;

            var it = StaticModelList.models.LookupByKey(owner.GetDisplayId());
            if (it == null)
                return false;

            AxisAlignedBox mdl_box = new(it.bound);
            // ignore models with no bounds
            if (mdl_box == AxisAlignedBox.Zero())
            {
                Log.outError(LogFilter.Server, "GameObject model {0} has zero bounds, loading skipped", it.name);
                return false;
            }

            iPos = owner.GetPosition();

            Matrix4x4 iRotation = owner.GetRotation().ToMatrix();
            iRotation.Inverse(out iInvRot);
            // transform bounding box:
            mdl_box = new AxisAlignedBox(mdl_box.Lo * iScale, mdl_box.Hi * iScale);
            AxisAlignedBox rotated_bounds = new();
            for (int i = 0; i < 8; ++i)
                rotated_bounds.merge(iRotation.Multiply(mdl_box.corner(i)));

            iBound = rotated_bounds + iPos;

            return true;
        }

        public override Vector3 GetPosition() { return iPos; }
        public override AxisAlignedBox GetBounds() { return iBound; }

        public void EnableCollision(bool enable) { _collisionEnabled = enable; }
        bool IsCollisionEnabled() { return _collisionEnabled; }
        public bool IsMapObject() { return isWmo; }
        public byte GetNameSetId() { return owner.GetNameSetId(); }
        
        public static bool LoadGameObjectModelList()
        {
            uint oldMSTime = Time.GetMSTime();
            var filename = Global.WorldMgr.GetDataPath() + "/vmaps/GameObjectModels.dtree";
            if (!File.Exists(filename))
            {
                Log.outWarn(LogFilter.Server, "Unable to open '{0}' file.", filename);
                return false;
            }
            try
            {
                using BinaryReader reader = new(new FileStream(filename, FileMode.Open, FileAccess.Read));
                string magic = reader.ReadStringFromChars(8);
                if (magic != MapConst.VMapMagic)
                {
                    Log.outError(LogFilter.Misc, $"File '{filename}' has wrong header, expected {MapConst.VMapMagic}.");
                    return false;
                }

                long length = reader.BaseStream.Length;
                while (true)
                {
                    if (reader.BaseStream.Position >= length)
                        break;

                    uint displayId = reader.ReadUInt32();
                    bool isWmo = reader.ReadBoolean();
                    int name_length = reader.ReadInt32();
                    string name = reader.ReadString(name_length);
                    Vector3 v1 = reader.Read<Vector3>();
                    Vector3 v2 = reader.Read<Vector3>();

                    StaticModelList.models.Add(displayId, new GameobjectModelData(name, v1, v2, isWmo));
                }
            }
            catch (EndOfStreamException ex)
            {
                Log.outException(ex);
            }

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} GameObject models in {1} ms", StaticModelList.models.Count, Time.GetMSTimeDiffToNow(oldMSTime));
            return true;
        }

        bool _collisionEnabled;
        AxisAlignedBox iBound;
        Matrix4x4 iInvRot;
        Vector3 iPos;
        float iInvScale;
        float iScale;
        WorldModel iModel;
        GameObjectModelOwnerBase owner;
        bool isWmo;
    }
    public class GameobjectModelData
    {
        public GameobjectModelData(string name_, Vector3 lowBound, Vector3 highBound, bool isWmo_)
        {
            bound = new AxisAlignedBox(lowBound, highBound);
            name = name_;
            isWmo = isWmo_;
        }

        public AxisAlignedBox bound;
        public string name;
        public bool isWmo;
    }
}
    
