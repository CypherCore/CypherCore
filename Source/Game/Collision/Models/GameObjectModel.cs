/*
 * Copyright (C) 2012-2018 CypherCore <http://github.com/CypherCore>
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

namespace Game.Collision
{
    public class StaticModelList
    {
        public static Dictionary<uint, GameobjectModelData> models = new Dictionary<uint, GameobjectModelData>();
    }

    public class GameObjectModelOwnerBase
    {
        public virtual bool IsSpawned() { return false; }
        public virtual uint GetDisplayId() { return 0; }
        public virtual bool IsInPhase(List<uint> phases) { return false; }
        public virtual Vector3 GetPosition() { return Vector3.Zero; }
        public virtual float GetOrientation() { return 0.0f; }
        public virtual float GetScale() { return 1.0f; }
        public virtual void DebugVisualizeCorner(Vector3 corner) { }
    }

    public class GameObjectModel : IModel
    {
        bool initialize(GameObjectModelOwnerBase modelOwner)
        {
            var it = StaticModelList.models.LookupByKey(modelOwner.GetDisplayId());
            if (it == null)
                return false;

            AxisAlignedBox mdl_box = new AxisAlignedBox(it.bound);
            // ignore models with no bounds
            if (mdl_box == AxisAlignedBox.Zero())
            {
                Log.outError(LogFilter.Server, "GameObject model {0} has zero bounds, loading skipped", it.name);
                return false;
            }

            iModel = Global.VMapMgr.acquireModelInstance(it.name);

            if (iModel == null)
                return false;

            name = it.name;
            iPos = modelOwner.GetPosition();
            iScale = modelOwner.GetScale();
            iInvScale = 1.0f / iScale;

            Matrix3 iRotation = Matrix3.fromEulerAnglesZYX(modelOwner.GetOrientation(), 0, 0);
            iInvRot = iRotation.inverse();
            // transform bounding box:
            mdl_box = new AxisAlignedBox(mdl_box.Lo * iScale, mdl_box.Hi * iScale);
            AxisAlignedBox rotated_bounds = new AxisAlignedBox();
            for (int i = 0; i < 8; ++i)
                rotated_bounds.merge(iRotation * mdl_box.corner(i));

            iBound = rotated_bounds + iPos;
            owner = modelOwner;
            return true;
        }

        public static GameObjectModel Create(GameObjectModelOwnerBase modelOwner)
        {
            GameObjectModel mdl = new GameObjectModel();
            if (!mdl.initialize(modelOwner))
                return null;

            return mdl;
        }

        public bool intersectRay(Ray ray, ref float maxDist, bool stopAtFirstHit, List<uint> phases)
        {
            if (!isCollisionEnabled() || !owner.IsSpawned())
                return false;

            if (!owner.IsInPhase(phases))
                return false;

            float time = ray.intersectionTime(iBound);
            if (time == float.PositiveInfinity)
                return false;

            // child bounds are defined in object space:
            Vector3 p = iInvRot * (ray.Origin - iPos) * iInvScale;
            Ray modRay = new Ray(p, iInvRot * ray.Direction);
            float distance = maxDist * iInvScale;
            bool hit = iModel.IntersectRay(modRay, ref distance, stopAtFirstHit);
            if (hit)
            {
                distance *= iScale;
                maxDist = distance;
            }
            return hit;
        }

        public bool UpdatePosition()
        {
            if (iModel == null)
                return false;

            var it = StaticModelList.models.LookupByKey(owner.GetDisplayId());
            if (it == null)
                return false;

            AxisAlignedBox mdl_box = new AxisAlignedBox(it.bound);
            // ignore models with no bounds
            if (mdl_box == AxisAlignedBox.Zero())
            {
                Log.outError(LogFilter.Server, "GameObject model {0} has zero bounds, loading skipped", it.name);
                return false;
            }

            iPos = owner.GetPosition();

            Matrix3 iRotation = Matrix3.fromEulerAnglesZYX(owner.GetOrientation(), 0, 0);
            iInvRot = iRotation.inverse();
            // transform bounding box:
            mdl_box = new AxisAlignedBox(mdl_box.Lo * iScale, mdl_box.Hi * iScale);
            AxisAlignedBox rotated_bounds = new AxisAlignedBox();
            for (int i = 0; i < 8; ++i)
                rotated_bounds.merge(iRotation * mdl_box.corner(i));

            iBound = rotated_bounds + iPos;

            return true;
        }

        public override Vector3 getPosition() { return iPos; }
        public override AxisAlignedBox getBounds() { return iBound; }

        public void enableCollision(bool enable) { _collisionEnabled = enable; }
        bool isCollisionEnabled() { return _collisionEnabled; }

        public static void LoadGameObjectModelList()
        {
            uint oldMSTime = Time.GetMSTime();
            var filename = Global.WorldMgr.GetDataPath() + "/vmaps/GameObjectModels.dtree";
            if (!File.Exists(filename))
            {
                Log.outWarn(LogFilter.Server, "Unable to open '{0}' file.", filename);
                return;
            }
            try
            {
                using (BinaryReader reader = new BinaryReader(new FileStream(filename, FileMode.Open, FileAccess.Read)))
                {
                    uint name_length, displayId;
                    string name;

                    long length = reader.BaseStream.Length;
                    while (true)
                    {
                        if (reader.BaseStream.Position >= length)
                            break;

                        Vector3 v1, v2;
                        displayId = reader.ReadUInt32();
                        name_length = reader.ReadUInt32();
                        name = reader.ReadString((int)name_length);
                        v1 = reader.Read<Vector3>();
                        v2 = reader.Read<Vector3>();

                        StaticModelList.models.Add(displayId, new GameobjectModelData(name, new AxisAlignedBox(v1, v2)));
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
    }
    public class GameobjectModelData
    {
        public GameobjectModelData(string name_, AxisAlignedBox box)
        {
            bound = box;
            name = name_;
        }

        public AxisAlignedBox bound;
        public string name;
    }
}
    
