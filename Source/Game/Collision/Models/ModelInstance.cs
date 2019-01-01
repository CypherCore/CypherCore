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

using Framework.Constants;
using Framework.GameMath;
using System;
using System.IO;

namespace Game.Collision
{
    public enum ModelFlags
    {
        M2 = 1,
        HasBound = 1 << 1,
        ParentSpawn = 1 << 2
    }

    public class ModelSpawn
    {
        public ModelSpawn() { }
        public ModelSpawn(ModelSpawn spawn)
        {
            flags = spawn.flags;
            adtId = spawn.adtId;
            ID = spawn.ID;
            iPos = spawn.iPos;
            iRot = spawn.iRot;
            iScale = spawn.iScale;
            iBound = spawn.iBound;
            name = spawn.name;
        }

        public static bool readFromFile(BinaryReader reader, out ModelSpawn spawn)
        {
            spawn = new ModelSpawn();

            spawn.flags = reader.ReadUInt32();
            spawn.adtId = reader.ReadUInt16();
            spawn.ID = reader.ReadUInt32();
            spawn.iPos = reader.Read<Vector3>();
            spawn.iRot = reader.Read<Vector3>();
            spawn.iScale = reader.ReadSingle();

            bool has_bound = Convert.ToBoolean(spawn.flags & (uint)ModelFlags.HasBound);
            if (has_bound) // only WMOs have bound in MPQ, only available after computation
            {
                Vector3 bLow = reader.Read<Vector3>();
                Vector3 bHigh = reader.Read<Vector3>();
                spawn.iBound = new AxisAlignedBox(bLow, bHigh);
            }

            uint nameLen = reader.ReadUInt32();
            spawn.name = reader.ReadString((int)nameLen);
            return true;
        }

        public uint flags;
        public ushort adtId;
        public uint ID;
        public Vector3 iPos;
        public Vector3 iRot;
        public float iScale;
        public AxisAlignedBox iBound;
        public string name;
    }

    public class ModelInstance : ModelSpawn
    {
        public ModelInstance()
        {
            iInvScale = 0.0f;
            iModel = null;
        }
        public ModelInstance(ModelSpawn spawn, WorldModel model)
            : base(spawn)
        {
            iModel = model;

            iInvRot = Matrix3.fromEulerAnglesZYX(MathFunctions.PI * iRot.Y / 180.0f, MathFunctions.PI * iRot.X / 180.0f, MathFunctions.PI * iRot.Z / 180.0f).inverse();
            iInvScale = 1.0f / iScale;
        }

        public bool intersectRay(Ray pRay, ref float pMaxDist, bool pStopAtFirstHit, ModelIgnoreFlags ignoreFlags)
        {
            if (iModel == null)
                return false;

            float time = pRay.intersectionTime(iBound);
            if (float.IsInfinity(time))
                return false;

            // child bounds are defined in object space:
            Vector3 p = iInvRot * (pRay.Origin - iPos) * iInvScale;
            Ray modRay = new Ray(p, iInvRot * pRay.Direction);
            float distance = pMaxDist * iInvScale;
            bool hit = iModel.IntersectRay(modRay, ref distance, pStopAtFirstHit, ignoreFlags);
            if (hit)
            {
                distance *= iScale;
                pMaxDist = distance;
            }
            return hit;
        }

        public void intersectPoint(Vector3 p, AreaInfo info)
        {
            if (iModel == null)
                return;

            // M2 files don't contain area info, only WMO files
            if (Convert.ToBoolean(flags & (uint)ModelFlags.M2))
                return;
            if (!iBound.contains(p))
                return;
            // child bounds are defined in object space:
            Vector3 pModel = iInvRot * (p - iPos) * iInvScale;
            Vector3 zDirModel = iInvRot * new Vector3(0.0f, 0.0f, -1.0f);
            float zDist;
            if (iModel.IntersectPoint(pModel, zDirModel, out zDist, info))
            {
                Vector3 modelGround = pModel + zDist * zDirModel;
                // Transform back to world space. Note that:
                // Mat * vec == vec * Mat.transpose()
                // and for rotation matrices: Mat.inverse() == Mat.transpose()
                float world_Z = ((modelGround * iInvRot) * iScale + iPos).Z;
                if (info.ground_Z < world_Z)
                {
                    info.ground_Z = world_Z;
                    info.adtId = adtId;
                }
            }
        }

        public bool GetLiquidLevel(Vector3 p, LocationInfo info, ref float liqHeight)
        {
            // child bounds are defined in object space:
            Vector3 pModel = iInvRot * (p - iPos) * iInvScale;
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

        public bool GetLocationInfo(Vector3 p, LocationInfo info)
        {
            if (iModel == null)
                return false;

            // M2 files don't contain area info, only WMO files
            if (Convert.ToBoolean(flags & (uint)ModelFlags.M2))
                return false;
            if (!iBound.contains(p))
                return false;
            // child bounds are defined in object space:
            Vector3 pModel = iInvRot * (p - iPos) * iInvScale;
            Vector3 zDirModel = iInvRot * new Vector3(0.0f, 0.0f, -1.0f);
            float zDist;
            if (iModel.GetLocationInfo(pModel, zDirModel, out zDist, info))
            {
                Vector3 modelGround = pModel + zDist * zDirModel;
                // Transform back to world space. Note that:
                // Mat * vec == vec * Mat.transpose()
                // and for rotation matrices: Mat.inverse() == Mat.transpose()
                float world_Z = ((modelGround * iInvRot) * iScale + iPos).Z;
                if (info.ground_Z < world_Z) // hm...could it be handled automatically with zDist at intersection?
                {
                    info.ground_Z = world_Z;
                    info.hitInstance = this;
                    return true;
                }
            }
            return false;
        }

        public void setUnloaded() { iModel = null; }

        Matrix3 iInvRot;
        float iInvScale;
        WorldModel iModel;
    }
}
