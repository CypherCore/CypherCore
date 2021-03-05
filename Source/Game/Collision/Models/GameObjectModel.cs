﻿/*
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

using Framework.GameMath;
using System;
using System.Collections.Generic;
using System.IO;
using Framework.Constants;

namespace Game.Collision
{
    public class StaticModelList
    {
        public static Dictionary<uint, GameobjectModelData> models = new Dictionary<uint, GameobjectModelData>();
    }

    public abstract class GameObjectModelOwnerBase
    {
        public abstract bool IsSpawned();
        public abstract uint GetDisplayId();
        public abstract byte GetNameSetId();
        public abstract bool IsInPhase(PhaseShift phaseShift);
        public abstract Vector3 GetPosition();
        public abstract float GetOrientation();
        public abstract float GetScale();
    }

    public class GameObjectModel : IModel
    {
        bool Initialize(GameObjectModelOwnerBase modelOwner)
        {
            var modelData = StaticModelList.models.LookupByKey(modelOwner.GetDisplayId());
            if (modelData == null)
                return false;

            var mdl_box = new AxisAlignedBox(modelData.bound);
            // ignore models with no bounds
            if (mdl_box == AxisAlignedBox.Zero())
            {
                Log.outError(LogFilter.Server, "GameObject model {0} has zero bounds, loading skipped", modelData.name);
                return false;
            }

            iModel = Global.VMapMgr.AcquireModelInstance(modelData.name);

            if (iModel == null)
                return false;

            name = modelData.name;
            iPos = modelOwner.GetPosition();
            iScale = modelOwner.GetScale();
            iInvScale = 1.0f / iScale;

            var iRotation = Matrix3.fromEulerAnglesZYX(modelOwner.GetOrientation(), 0, 0);
            iInvRot = iRotation.inverse();
            // transform bounding box:
            mdl_box = new AxisAlignedBox(mdl_box.Lo * iScale, mdl_box.Hi * iScale);
            var rotated_bounds = new AxisAlignedBox();
            for (var i = 0; i < 8; ++i)
                rotated_bounds.merge(iRotation * mdl_box.corner(i));

            iBound = rotated_bounds + iPos;
            owner = modelOwner;
            isWmo = modelData.isWmo;
            return true;
        }

        public static GameObjectModel Create(GameObjectModelOwnerBase modelOwner)
        {
            var mdl = new GameObjectModel();
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

            var time = ray.intersectionTime(iBound);
            if (time == float.PositiveInfinity)
                return false;

            // child bounds are defined in object space:
            var p = iInvRot * (ray.Origin - iPos) * iInvScale;
            var modRay = new Ray(p, iInvRot * ray.Direction);
            var distance = maxDist * iInvScale;
            var hit = iModel.IntersectRay(modRay, ref distance, stopAtFirstHit, ignoreFlags);
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
            var pModel = iInvRot * (point - iPos) * iInvScale;
            var zDirModel = iInvRot * new Vector3(0.0f, 0.0f, -1.0f);
            float zDist;
            if (iModel.IntersectPoint(pModel, zDirModel, out zDist, info))
            {
                var modelGround = pModel + zDist * zDirModel;
                var world_Z = ((modelGround * iInvRot) * iScale + iPos).Z;
                if (info.ground_Z < world_Z)
                {
                    info.ground_Z = world_Z;
                    info.adtId = owner.GetNameSetId();
                }
            }
        }

        public bool UpdatePosition()
        {
            if (iModel == null)
                return false;

            var it = StaticModelList.models.LookupByKey(owner.GetDisplayId());
            if (it == null)
                return false;

            var mdl_box = new AxisAlignedBox(it.bound);
            // ignore models with no bounds
            if (mdl_box == AxisAlignedBox.Zero())
            {
                Log.outError(LogFilter.Server, "GameObject model {0} has zero bounds, loading skipped", it.name);
                return false;
            }

            iPos = owner.GetPosition();

            var iRotation = Matrix3.fromEulerAnglesZYX(owner.GetOrientation(), 0, 0);
            iInvRot = iRotation.inverse();
            // transform bounding box:
            mdl_box = new AxisAlignedBox(mdl_box.Lo * iScale, mdl_box.Hi * iScale);
            var rotated_bounds = new AxisAlignedBox();
            for (var i = 0; i < 8; ++i)
                rotated_bounds.merge(iRotation * mdl_box.corner(i));

            iBound = rotated_bounds + iPos;

            return true;
        }

        public override Vector3 GetPosition() { return iPos; }
        public override AxisAlignedBox GetBounds() { return iBound; }

        public void EnableCollision(bool enable) { _collisionEnabled = enable; }
        bool IsCollisionEnabled() { return _collisionEnabled; }
        public bool IsMapObject() { return isWmo; }

        public static void LoadGameObjectModelList()
        {
            var oldMSTime = Time.GetMSTime();
            var filename = Global.WorldMgr.GetDataPath() + "/vmaps/GameObjectModels.dtree";
            if (!File.Exists(filename))
            {
                Log.outWarn(LogFilter.Server, "Unable to open '{0}' file.", filename);
                return;
            }
            try
            {
                using (var reader = new BinaryReader(new FileStream(filename, FileMode.Open, FileAccess.Read)))
                {
                    var magic = reader.ReadStringFromChars(8);
                    if (magic != MapConst.VMapMagic)
                    {
                        Log.outError(LogFilter.Misc, $"File '{filename}' has wrong header, expected {MapConst.VMapMagic}.");
                        return;
                    }

                    var length = reader.BaseStream.Length;
                    while (true)
                    {
                        if (reader.BaseStream.Position >= length)
                            break;

                        var displayId = reader.ReadUInt32();
                        var isWmo = reader.ReadBoolean();
                        var name_length = reader.ReadInt32();
                        var name = reader.ReadString(name_length);
                        var v1 = reader.Read<Vector3>();
                        var v2 = reader.Read<Vector3>();

                        StaticModelList.models.Add(displayId, new GameobjectModelData(name, v1, v2, isWmo));
                    }
                }
            }
            catch (EndOfStreamException ex)
            {
                Log.outException(ex);
            }

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} GameObject models in {1} ms", StaticModelList.models.Count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        string name;
        bool _collisionEnabled;
        AxisAlignedBox iBound;
        Matrix3 iInvRot;
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
    
