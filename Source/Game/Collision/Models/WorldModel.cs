// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.GameMath;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace Game.Collision
{
    public struct MeshTriangle
    {
        public uint idx0;
        public uint idx1;
        public uint idx2;

        public MeshTriangle(uint na, uint nb, uint nc)
        {
            idx0 = na;
            idx1 = nb;
            idx2 = nc;
        }
    }

    public class WmoLiquid
    {
        uint iTilesX;
        uint iTilesY;
        Vector3 iCorner;
        uint iType;
        float[] iHeight;
        byte[] iFlags;

        public WmoLiquid() { }

        public WmoLiquid(uint width, uint height, Vector3 corner, uint type)
        {
            iTilesX = width;
            iTilesY = height;
            iCorner = corner;
            iType = type;

            if (width != 0 && height != 0)
            {
                iHeight = new float[(width + 1) * (height + 1)];
                iFlags = new byte[width * height];
            }
            else
            {
                iHeight = new float[1];
                iFlags = null;
            }
        }

        public WmoLiquid(WmoLiquid other)
        {
            if (this == other)
                return;

            iTilesX = other.iTilesX;
            iTilesY = other.iTilesY;
            iCorner = other.iCorner;
            iType = other.iType;
            if (other.iHeight != null)
            {
                iHeight = new float[(iTilesX + 1) * (iTilesY + 1)];
                Buffer.BlockCopy(other.iHeight, 0, iHeight, 0, (int)((iTilesX + 1) * (iTilesY + 1)));
            }
            else
                iHeight = null;
            if (other.iFlags != null)
            {
                iFlags = new byte[iTilesX * iTilesY];
                Buffer.BlockCopy(other.iFlags, 0, iFlags, 0, (int)(iTilesX * iTilesY));
            }
            else
                iFlags = null;
        }

        public bool GetLiquidHeight(Vector3 pos, out float liqHeight)
        {
            // simple case
            if (iFlags == null)
            {
                liqHeight = iHeight[0];
                return true;
            }

            liqHeight = 0f;
            float tx_f = (pos.X - iCorner.X) / MapConst.LiquidTileSize;
            uint tx = (uint)tx_f;
            if (tx_f < 0.0f || tx >= iTilesX)
                return false;

            float ty_f = (pos.Y - iCorner.Y) / MapConst.LiquidTileSize;
            uint ty = (uint)ty_f;
            if (ty_f < 0.0f || ty >= iTilesY)
                return false;

            // check if tile shall be used for liquid level
            // checking for 0x08 *might* be enough, but disabled tiles always are 0x?F:
            if ((iFlags[tx + ty * iTilesX] & 0x0F) == 0x0F)
                return false;

            // (dx, dy) coordinates inside tile, in [0, 1]^2
            float dx = tx_f - tx;
            float dy = ty_f - ty;

            uint rowOffset = iTilesX + 1;
            if (dx > dy) // case (a)
            {
                float sx = iHeight[tx + 1 + ty * rowOffset] - iHeight[tx + ty * rowOffset];
                float sy = iHeight[tx + 1 + (ty + 1) * rowOffset] - iHeight[tx + 1 + ty * rowOffset];
                liqHeight = iHeight[tx + ty * rowOffset] + dx * sx + dy * sy;
            }
            else // case (b)
            {
                float sx = iHeight[tx + 1 + (ty + 1) * rowOffset] - iHeight[tx + (ty + 1) * rowOffset];
                float sy = iHeight[tx + (ty + 1) * rowOffset] - iHeight[tx + ty * rowOffset];
                liqHeight = iHeight[tx + ty * rowOffset] + dx * sx + dy * sy;
            }
            return true;
        }

        public static WmoLiquid ReadFromFile(BinaryReader reader)
        {
            WmoLiquid liquid = new();

            liquid.iTilesX = reader.ReadUInt32();
            liquid.iTilesY = reader.ReadUInt32();
            liquid.iCorner = reader.Read<Vector3>();
            liquid.iType = reader.ReadUInt32();

            if (liquid.iTilesX != 0 && liquid.iTilesY != 0)
            {
                uint size = (liquid.iTilesX + 1) * (liquid.iTilesY + 1);
                liquid.iHeight = reader.ReadArray<float>(size);

                size = liquid.iTilesX * liquid.iTilesY;
                liquid.iFlags = reader.ReadArray<byte>(size);
            }
            else
            {
                liquid.iHeight = new float[1];
                liquid.iHeight[0] = reader.ReadSingle();
            }

            return liquid;
        }

        public uint GetLiquidType() { return iType; }
    }

    public class GroupModel : IModel
    {
        AxisAlignedBox iBound;
        uint iMogpFlags;
        uint iGroupWMOID;
        List<Vector3> vertices = new();
        List<MeshTriangle> triangles = new();
        BIH meshTree = new();
        WmoLiquid iLiquid;

        public GroupModel()
        {
            iLiquid = null;
        }

        public GroupModel(GroupModel other)
        {
            iBound = other.iBound;
            iMogpFlags = other.iMogpFlags;
            iGroupWMOID = other.iGroupWMOID;
            vertices = other.vertices;
            triangles = other.triangles;
            meshTree = other.meshTree;
            iLiquid = null;

            if (other.iLiquid != null)
                iLiquid = new WmoLiquid(other.iLiquid);
        }

        public GroupModel(uint mogpFlags, uint groupWMOID, AxisAlignedBox bound)
        {
            iBound = bound;
            iMogpFlags = mogpFlags;
            iGroupWMOID = groupWMOID;
            iLiquid = null;
        }

        public bool ReadFromFile(BinaryReader reader)
        {
            triangles.Clear();
            vertices.Clear();
            iLiquid = null;

            var lo = reader.Read<Vector3>();
            var hi = reader.Read<Vector3>();
            iBound = new AxisAlignedBox(lo, hi);
            iMogpFlags = reader.ReadUInt32();
            iGroupWMOID = reader.ReadUInt32();

            // read vertices
            if (reader.ReadStringFromChars(4) != "VERT")
                return false;

            uint chunkSize = reader.ReadUInt32();
            uint count = reader.ReadUInt32();
            if (count == 0)
                return false;

            for (var i = 0; i < count; ++i)
                vertices.Add(reader.Read<Vector3>());

            // read triangle mesh
            if (reader.ReadStringFromChars(4) != "TRIM")
                return false;

            chunkSize = reader.ReadUInt32();
            count = reader.ReadUInt32();

            for (var i = 0; i < count; ++i)
                triangles.Add(reader.Read<MeshTriangle>());

            // read mesh BIH
            if (reader.ReadStringFromChars(4) != "MBIH")
                return false;

            meshTree.ReadFromFile(reader);

            // write liquid data
            if (reader.ReadStringFromChars(4) != "LIQU")
                return false;

            chunkSize = reader.ReadUInt32();
            if (chunkSize > 0)
                iLiquid = WmoLiquid.ReadFromFile(reader);

            return true;
        }

        public override bool IntersectRay(Ray ray, ref float distance, bool stopAtFirstHit)
        {
            if (triangles.Empty())
                return false;

            GModelRayCallback callback = new(triangles, vertices);
            meshTree.IntersectRay(ray, callback, ref distance, stopAtFirstHit);
            return callback.hit;
        }

        public bool IsInsideObject(Vector3 pos, Vector3 down, out float z_dist)
        {
            z_dist = 0f;
            if (triangles.Empty() || !iBound.contains(pos))
                return false;

            Vector3 rPos = pos - 0.1f * down;
            float dist = float.PositiveInfinity;
            Ray ray = new(rPos, down);
            bool hit = IntersectRay(ray, ref dist, false);
            if (hit)
                z_dist = dist - 0.1f;
            return hit;
        }

        public bool GetLiquidLevel(Vector3 pos, out float liqHeight)
        {
            liqHeight = 0f;
            if (iLiquid != null)
                return iLiquid.GetLiquidHeight(pos, out liqHeight);
            return false;
        }

        public uint GetLiquidType()
        {
            if (iLiquid != null)
                return iLiquid.GetLiquidType();
            return 0;
        }

        public override AxisAlignedBox GetBounds() { return iBound; }

        public uint GetMogpFlags() { return iMogpFlags; }

        public uint GetWmoID() { return iGroupWMOID; }
    }

    public class WorldModel : IModel
    {
        public uint Flags;

        uint RootWMOID;
        List<GroupModel> groupModels = new();
        BIH groupTree = new();

        public override bool IntersectRay(Ray ray, ref float distance, bool stopAtFirstHit, ModelIgnoreFlags ignoreFlags)
        {
            // If the caller asked us to ignore certain objects we should check flags
            if ((ignoreFlags & ModelIgnoreFlags.M2) != ModelIgnoreFlags.Nothing)
            {
                // M2 models are not taken into account for LoS calculation if caller requested their ignoring.
                if ((Flags & (uint)ModelFlags.M2) != 0)
                    return false;
            }

            // small M2 workaround, maybe better make separate class with virtual intersection funcs
            // in any case, there's no need to use a bound tree if we only have one submodel
            if (groupModels.Count == 1)
                return groupModels[0].IntersectRay(ray, ref distance, stopAtFirstHit);

            WModelRayCallBack isc = new(groupModels);
            groupTree.IntersectRay(ray, isc, ref distance, stopAtFirstHit);
            return isc.hit;
        }

        public bool IntersectPoint(Vector3 p, Vector3 down, out float dist, AreaInfo info)
        {
            dist = 0f;
            if (groupModels.Empty())
                return false;

            WModelAreaCallback callback = new(groupModels, down);
            groupTree.IntersectPoint(p, callback);
            if (callback.hit != null)
            {
                info.rootId = (int)RootWMOID;
                info.groupId = (int)callback.hit.GetWmoID();
                info.flags = callback.hit.GetMogpFlags();
                info.result = true;
                dist = callback.zDist;
                return true;
            }
            return false;
        }

        public bool GetLocationInfo(Vector3 p, Vector3 down, out float dist, GroupLocationInfo info)
        {
            dist = 0f;
            if (groupModels.Empty())
                return false;

            WModelAreaCallback callback = new(groupModels, down);
            groupTree.IntersectPoint(p, callback);
            if (callback.hit != null)
            {
                info.rootId = (int)RootWMOID;
                info.hitModel = callback.hit;
                dist = callback.zDist;
                return true;
            }
            return false;
        }

        public bool ReadFile(string filename)
        {
            if (!File.Exists(filename))
            {
                filename += ".vmo";
                if (!File.Exists(filename))
                    return false;
            }

            using BinaryReader reader = new(new FileStream(filename, FileMode.Open, FileAccess.Read));
            if (reader.ReadStringFromChars(8) != MapConst.VMapMagic)
                return false;

            if (reader.ReadStringFromChars(4) != "WMOD")
                return false;

            reader.ReadUInt32(); //chunkSize notused
            RootWMOID = reader.ReadUInt32();

            // read group models
            if (reader.ReadStringFromChars(4) != "GMOD")
                return false;

            uint count = reader.ReadUInt32();
            for (var i = 0; i < count; ++i)
            {
                GroupModel group = new();
                group.ReadFromFile(reader);
                groupModels.Add(group);
            }

            // read group BIH
            if (reader.ReadStringFromChars(4) != "GBIH")
                return false;

            return groupTree.ReadFromFile(reader);
        }
    }
}
