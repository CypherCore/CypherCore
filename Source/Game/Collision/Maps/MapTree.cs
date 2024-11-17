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
    public class GroupLocationInfo
    {
        public GroupModel hitModel;
        public int rootId = -1;
    }

    public class LocationInfo
    {
        public LocationInfo()
        {
            ground_Z = float.NegativeInfinity;
        }

        public int rootId;
        public ModelInstance hitInstance;
        public GroupModel hitModel;
        public float ground_Z;
    }

    public class AreaInfo
    {
        public AreaInfo()
        {
            ground_Z = float.NegativeInfinity;
        }

        public bool result;
        public float ground_Z;
        public uint flags;
        public int adtId;
        public int rootId;
        public int groupId;
    }

    public class StaticMapTree
    {
        public StaticMapTree(uint mapId)
        {
            iMapID = mapId;
        }

        public LoadResult InitMap(string fname)
        {
            Log.outDebug(LogFilter.Maps, $"StaticMapTree.InitMap() : initializing StaticMapTree '{fname}'");
            if (!File.Exists(fname))
                return LoadResult.FileNotFound;

            using (BinaryReader reader = new(new FileStream(fname, FileMode.Open, FileAccess.Read)))
            {
                var magic = reader.ReadStringFromChars(8);
                if (magic != MapConst.VMapMagic)
                    return LoadResult.VersionMismatch;

                var node = reader.ReadStringFromChars(4);
                if (node != "NODE" || !iTree.ReadFromFile(reader))
                    return LoadResult.ReadFromFileFailed;
            }

            iTreeValues = new ModelInstance[iTree.PrimCount()];

            return LoadResult.Success;
        }

        public void UnloadMap()
        {
            iTreeValues.Clear();
            iLoadedTiles.Clear();
        }

        public LoadResult LoadMapTile(int tileX, int tileY, VMapManager vm)
        {
            if (iTreeValues.Empty())
            {
                Log.outError(LogFilter.Server, "StaticMapTree.LoadMapTile() : tree has not been initialized [{0}, {1}]", tileX, tileY);
                return LoadResult.ReadFromFileFailed;
            }

            LoadResult result = LoadResult.FileNotFound;

            TileFileOpenResult fileResult = OpenMapTileFile(VMapManager.VMapPath, iMapID, tileX, tileY, vm);
            if (fileResult.TileFile != null && fileResult.SpawnIndicesFile != null)
            {
                result = LoadResult.Success;
                using BinaryReader reader = new(fileResult.TileFile);
                using BinaryReader spawnIndicesReader = new(fileResult.SpawnIndicesFile);
                if (reader.ReadStringFromChars(8) != MapConst.VMapMagic)
                    result = LoadResult.VersionMismatch;
                if (spawnIndicesReader.ReadStringFromChars(8) != MapConst.VMapMagic)
                    result = LoadResult.VersionMismatch;

                if (result == LoadResult.Success)
                {
                    uint numSpawns = reader.ReadUInt32();
                    uint numSpawnIndices = spawnIndicesReader.ReadUInt32();

                    if (numSpawns != numSpawnIndices)
                        result = LoadResult.ReadFromFileFailed;

                    for (uint i = 0; i < numSpawns && result == LoadResult.Success; ++i)
                    {
                        // read model spawns
                        if (ModelSpawn.ReadFromFile(reader, out ModelSpawn spawn))
                        {
                            // acquire model instance
                            WorldModel model = vm.AcquireModelInstance(spawn.name);
                            if (model == null)
                                Log.outError(LogFilter.Server, "StaticMapTree.LoadMapTile() : could not acquire WorldModel [{0}, {1}]", tileX, tileY);

                            // update tree
                            int referencedVal = spawnIndicesReader.ReadInt32();
                            if (referencedVal >= iTreeValues.Length)
                            {
                                Log.outError(LogFilter.Maps, $"StaticMapTree::LoadMapTile() : invalid tree element ({referencedVal}/{iTreeValues.Length}) referenced in tile {fileResult.Name}");
                                result = LoadResult.ReadFromFileFailed;
                                continue;
                            }

                            if (iTreeValues[referencedVal]?.GetWorldModel() == null)
                                iTreeValues[referencedVal] = new ModelInstance(spawn, model);

                            iTreeValues[referencedVal].AddTileReference();
                        }
                        else
                        {
                            Log.outError(LogFilter.Maps, $"StaticMapTree.LoadMapTile() : cannot read model from file (spawn index {i}) referenced in tile {fileResult.Name} by map {iMapID}");
                            result = LoadResult.ReadFromFileFailed;
                        }
                    }
                }
                iLoadedTiles[PackTileID(tileX, tileY)] = true;
            }
            else
            {
                iLoadedTiles[PackTileID(tileX, tileY)] = false;
            }

            return result;
        }

        public void UnloadMapTile(int tileX, int tileY, VMapManager vm)
        {
            uint tileID = PackTileID(tileX, tileY);
            if (!iLoadedTiles.ContainsKey(tileID))
            {
                Log.outError(LogFilter.Server, "StaticMapTree.UnloadMapTile() : trying to unload non-loaded tile - Map:{0} X:{1} Y:{2}", iMapID, tileX, tileY);
                return;
            }
            if (iLoadedTiles[tileID]) // file associated with tile
            {
                TileFileOpenResult fileResult = OpenMapTileFile(VMapManager.VMapPath, iMapID, tileX, tileY, vm);
                if (fileResult.TileFile != null)
                {
                    using BinaryReader reader = new(fileResult.TileFile);
                    using BinaryReader spawnIndicesReader = new(fileResult.SpawnIndicesFile);
                    bool result = true;
                    if (reader.ReadStringFromChars(8) != MapConst.VMapMagic)
                        result = false;

                    uint numSpawns = reader.ReadUInt32();
                    uint numSpawnIndices = spawnIndicesReader.ReadUInt32();
                    if (numSpawns != numSpawnIndices)
                        result = false;

                    for (uint i = 0; i < numSpawns && result; ++i)
                    {
                        // read model spawns
                        if (!ModelSpawn.ReadFromFile(reader, out ModelSpawn spawn))
                            break;

                        // update tree
                        int referencedNode = spawnIndicesReader.ReadInt32();
                        if (referencedNode >= iTreeValues.Length)
                        {
                            Log.outError(LogFilter.Maps, $"StaticMapTree::LoadMapTile() : invalid tree element ({referencedNode}/{iTreeValues.Length}) referenced in tile {fileResult.Name}");
                            result = false;
                            continue;
                        }

                        if (iTreeValues[referencedNode].GetWorldModel() == null)
                            Log.outError(LogFilter.Misc, $"StaticMapTree::UnloadMapTile() : trying to unload non-referenced model '{spawn.name}' (ID:{spawn.Id})");
                        else if (iTreeValues[referencedNode].RemoveTileReference() == 0)
                            iTreeValues[referencedNode].SetUnloaded();
                    }
                }
            }
            iLoadedTiles.Remove(tileID);
        }

        static uint PackTileID(int tileX, int tileY) { return (uint)(tileX << 16 | tileY); }
        static void UnpackTileID(int ID, ref int tileX, ref int tileY) { tileX = ID >> 16; tileY = ID & 0xFF; }

        static TileFileOpenResult OpenMapTileFile(string vmapPath, uint mapID, int tileX, int tileY, VMapManager vm)
        {
            TileFileOpenResult result = new();
            result.Name = vmapPath + GetTileFileName(mapID, tileX, tileY, "vmtile");

            if (File.Exists(result.Name))
            {
                result.UsedMapId = mapID;
                result.TileFile = new FileStream(result.Name, FileMode.Open, FileAccess.Read);
                if (File.Exists(vmapPath + GetTileFileName(mapID, tileX, tileY, "vmtileidx")))
                    result.SpawnIndicesFile = new FileStream(vmapPath + GetTileFileName(mapID, tileX, tileY, "vmtileidx"), FileMode.Open, FileAccess.Read);
                return result;
            }

            int parentMapId = vm.GetParentMapId(mapID);
            while (parentMapId != -1)
            {
                result.Name = vmapPath + GetTileFileName((uint)parentMapId, tileX, tileY, "vmtile");
                if (File.Exists(result.Name))
                {
                    result.TileFile = new FileStream(result.Name, FileMode.Open, FileAccess.Read);
                    result.UsedMapId = (uint)parentMapId;
                    return result;
                }

                parentMapId = vm.GetParentMapId((uint)parentMapId);
            }

            return result;
        }

        public static LoadResult CanLoadMap(string vmapPath, uint mapID, int tileX, int tileY, VMapManager vm)
        {
            string fullname = vmapPath + VMapManager.GetMapFileName(mapID);
            if (!File.Exists(fullname))
                return LoadResult.FileNotFound;

            using (BinaryReader reader = new(new FileStream(fullname, FileMode.Open, FileAccess.Read)))
            {
                if (reader.ReadStringFromChars(8) != MapConst.VMapMagic)
                    return LoadResult.VersionMismatch;
            }

            FileStream stream = OpenMapTileFile(vmapPath, mapID, tileX, tileY, vm).TileFile;
            if (stream == null)
                return LoadResult.FileNotFound;

            using (BinaryReader reader = new(stream))
            {
                if (reader.ReadStringFromChars(8) != MapConst.VMapMagic)
                    return LoadResult.VersionMismatch;
            }

            return LoadResult.Success;
        }

        public static string GetTileFileName(uint mapID, int tileX, int tileY, string extension)
        {
            return $"{mapID:D4}/{mapID:D4}_{tileY:D2}_{tileX:D2}.{extension}";
        }

        public bool GetLocationInfo(Vector3 pos, LocationInfo info)
        {
            LocationInfoCallback intersectionCallBack = new(iTreeValues, info);
            iTree.IntersectPoint(pos, intersectionCallBack);
            return intersectionCallBack.result;
        }

        public float GetHeight(Vector3 pPos, float maxSearchDist)
        {
            float height = float.PositiveInfinity;
            Vector3 dir = new(0, 0, -1);
            Ray ray = new(pPos, dir);   // direction with length of 1
            float maxDist = maxSearchDist;
            if (GetIntersectionTime(ray, ref maxDist, false, ModelIgnoreFlags.Nothing))
                height = pPos.Z - maxDist;

            return height;
        }
        bool GetIntersectionTime(Ray pRay, ref float pMaxDist, bool pStopAtFirstHit, ModelIgnoreFlags ignoreFlags)
        {
            float distance = pMaxDist;
            MapRayCallback intersectionCallBack = new(iTreeValues, ignoreFlags);
            iTree.IntersectRay(pRay, intersectionCallBack, ref distance, pStopAtFirstHit);
            if (intersectionCallBack.DidHit())
                pMaxDist = distance;
            return intersectionCallBack.DidHit();
        }

        public bool GetObjectHitPos(Vector3 pPos1, Vector3 pPos2, out Vector3 pResultHitPos, float pModifyDist)
        {
            bool result;
            float maxDist = (pPos2 - pPos1).Length();
            // valid map coords should *never ever* produce float overflow, but this would produce NaNs too
            Cypher.Assert(maxDist < float.MaxValue);
            // prevent NaN values which can cause BIH intersection to enter infinite loop
            if (maxDist < 1e-10f)
            {
                pResultHitPos = pPos2;
                return false;
            }
            Vector3 dir = (pPos2 - pPos1) / maxDist;              // direction with length of 1
            Ray ray = new(pPos1, dir);
            float dist = maxDist;
            if (GetIntersectionTime(ray, ref dist, false, ModelIgnoreFlags.Nothing))
            {
                pResultHitPos = pPos1 + dir * dist;
                if (pModifyDist < 0)
                {
                    if ((pResultHitPos - pPos1).Length() > -pModifyDist)
                    {
                        pResultHitPos += dir * pModifyDist;
                    }
                    else
                    {
                        pResultHitPos = pPos1;
                    }
                }
                else
                {
                    pResultHitPos += dir * pModifyDist;
                }
                result = true;
            }
            else
            {
                pResultHitPos = pPos2;
                result = false;
            }
            return result;
        }

        public bool IsInLineOfSight(Vector3 pos1, Vector3 pos2, ModelIgnoreFlags ignoreFlags)
        {
            float maxDist = (pos2 - pos1).Length();
            // return false if distance is over max float, in case of cheater teleporting to the end of the universe
            if (maxDist == float.MaxValue ||
                maxDist == float.PositiveInfinity)
                return false;

            // valid map coords should *never ever* produce float overflow, but this would produce NaNs too
            Cypher.Assert(maxDist < float.MaxValue);
            // prevent NaN values which can cause BIH intersection to enter infinite loop
            if (maxDist < 1e-10f)
                return true;
            // direction with length of 1
            Ray ray = new(pos1, (pos2 - pos1) / maxDist);
            if (GetIntersectionTime(ray, ref maxDist, true, ignoreFlags))
                return false;

            return true;
        }

        public int NumLoadedTiles() { return iLoadedTiles.Count; }

        uint iMapID;
        BIH iTree = new();
        ModelInstance[] iTreeValues; // the tree entries

        Dictionary<uint, bool> iLoadedTiles = new();
    }

    class TileFileOpenResult
    {
        public string Name;
        public FileStream TileFile;
        public FileStream SpawnIndicesFile;
        public uint UsedMapId;
    }
}
