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
            Log.outDebug(LogFilter.Maps, "StaticMapTree.InitMap() : initializing StaticMapTree '{0}'", fname);
            if (!File.Exists(fname))
                return LoadResult.FileNotFound;

            using (BinaryReader reader = new(new FileStream(fname, FileMode.Open, FileAccess.Read)))
            {
                var magic = reader.ReadStringFromChars(8);
                if (magic != MapConst.VMapMagic)
                    return LoadResult.VersionMismatch;

                var node = reader.ReadStringFromChars(4);
                if (node != "NODE")
                    return LoadResult.ReadFromFileFailed;

                if (!iTree.ReadFromFile(reader))
                    return LoadResult.ReadFromFileFailed;

                iNTreeValues = iTree.PrimCount();
                iTreeValues = new ModelInstance[iNTreeValues];

                if (reader.ReadStringFromChars(4) != "SIDX")
                    return LoadResult.ReadFromFileFailed;

                uint spawnIndicesSize = reader.ReadUInt32();
                for (uint i = 0; i < spawnIndicesSize; ++i)
                {
                    uint spawnId = reader.ReadUInt32();
                    iSpawnIndices[spawnId] = i;
                }
            }

            return LoadResult.Success;
        }

        public void UnloadMap(VMapManager vm)
        {
            foreach (var id in iLoadedSpawns)
            {
                for (uint refCount = 0; refCount < id.Key; ++refCount)
                    vm.ReleaseModelInstance(iTreeValues[id.Key].name);

                iTreeValues[id.Key].SetUnloaded();
            }
            iLoadedSpawns.Clear();
            iLoadedTiles.Clear();
        }

        public LoadResult LoadMapTile(int tileX, int tileY, VMapManager vm)
        {
            if (iTreeValues == null)
            {
                Log.outError(LogFilter.Server, "StaticMapTree.LoadMapTile() : tree has not been initialized [{0}, {1}]", tileX, tileY);
                return LoadResult.ReadFromFileFailed;
            }

            LoadResult result = LoadResult.FileNotFound;

            TileFileOpenResult fileResult = OpenMapTileFile(VMapManager.VMapPath, iMapID, tileX, tileY, vm);
            if (fileResult.File != null)
            {
                result = LoadResult.Success;
                using BinaryReader reader = new(fileResult.File);
                if (reader.ReadStringFromChars(8) != MapConst.VMapMagic)
                    result = LoadResult.VersionMismatch;

                if (result == LoadResult.Success)
                {
                    uint numSpawns = reader.ReadUInt32();
                    for (uint i = 0; i < numSpawns && result == LoadResult.Success; ++i)
                    {
                        // read model spawns
                        if (ModelSpawn.ReadFromFile(reader, out ModelSpawn spawn))
                        {
                            // acquire model instance
                            WorldModel model = vm.AcquireModelInstance(spawn.name, spawn.flags);
                            if (model == null)
                                Log.outError(LogFilter.Server, "StaticMapTree.LoadMapTile() : could not acquire WorldModel [{0}, {1}]", tileX, tileY);

                            // update tree
                            if (iSpawnIndices.ContainsKey(spawn.Id))
                            {
                                uint referencedVal = iSpawnIndices[spawn.Id];
                                if (!iLoadedSpawns.ContainsKey(referencedVal))
                                {
                                    if (referencedVal >= iNTreeValues)
                                    {
                                        Log.outError(LogFilter.Maps, "StaticMapTree.LoadMapTile() : invalid tree element ({0}/{1}) referenced in tile {2}", referencedVal, iNTreeValues, fileResult.Name);
                                        continue;
                                    }

                                    iTreeValues[referencedVal] = new ModelInstance(spawn, model);
                                    iLoadedSpawns[referencedVal] = 1;
                                }
                                else
                                    ++iLoadedSpawns[referencedVal];
                            }
                            else if (iMapID == fileResult.UsedMapId)
                            {
                                // unknown parent spawn might appear in because it overlaps multiple tiles
                                // in case the original tile is swapped but its neighbour is now (adding this spawn)
                                // we want to not mark it as loading error and just skip that model
                                Log.outError(LogFilter.Maps, $"StaticMapTree.LoadMapTile() : invalid tree element (spawn {spawn.Id}) referenced in tile fileResult.Name{fileResult.Name} by map {iMapID}");
                                result = LoadResult.ReadFromFileFailed;
                            }
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
                if (fileResult.File != null)
                {
                    using BinaryReader reader = new(fileResult.File);
                    bool result = true;
                    if (reader.ReadStringFromChars(8) != MapConst.VMapMagic)
                        result = false;

                    uint numSpawns = reader.ReadUInt32();
                    for (uint i = 0; i < numSpawns && result; ++i)
                    {
                        // read model spawns
                        ModelSpawn spawn;
                        result = ModelSpawn.ReadFromFile(reader, out spawn);
                        if (result)
                        {
                            // release model instance
                            vm.ReleaseModelInstance(spawn.name);

                            // update tree
                            if (iSpawnIndices.ContainsKey(spawn.Id))
                            {
                                uint referencedNode = iSpawnIndices[spawn.Id];
                                if (!iLoadedSpawns.ContainsKey(referencedNode))
                                    Log.outError(LogFilter.Server, "StaticMapTree.UnloadMapTile() : trying to unload non-referenced model '{0}' (ID:{1})", spawn.name, spawn.Id);
                                else if (--iLoadedSpawns[referencedNode] == 0)
                                {
                                    iTreeValues[referencedNode].SetUnloaded();
                                    iLoadedSpawns.Remove(referencedNode);
                                }
                            }
                            else if (iMapID == fileResult.UsedMapId) // logic documented in StaticMapTree::LoadMapTile
                                result = false;
                        }
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
            result.Name = vmapPath + GetTileFileName(mapID, tileX, tileY);

            if (File.Exists(result.Name))
            {
                result.UsedMapId = mapID;
                result.File = new FileStream(result.Name, FileMode.Open, FileAccess.Read);
                return result;
            }

            int parentMapId = vm.GetParentMapId(mapID);
            while (parentMapId != -1)
            {
                result.Name = vmapPath + GetTileFileName((uint)parentMapId, tileX, tileY);
                if (File.Exists(result.Name))
                {
                    result.File = new FileStream(result.Name, FileMode.Open, FileAccess.Read);
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

            FileStream stream = OpenMapTileFile(vmapPath, mapID, tileX, tileY, vm).File;
            if (stream == null)
                return LoadResult.FileNotFound;

            using (BinaryReader reader = new(stream))
            {
                if (reader.ReadStringFromChars(8) != MapConst.VMapMagic)
                    return LoadResult.VersionMismatch;
            }

            return LoadResult.Success;
        }

        public static string GetTileFileName(uint mapID, int tileX, int tileY)
        {
            return $"{mapID:D4}_{tileY:D2}_{tileX:D2}.vmtile";
        }

        public bool GetAreaInfo(ref Vector3 pos, out uint flags, out int adtId, out int rootId, out int groupId)
        {
            flags = 0;
            adtId = 0;
            rootId = 0;
            groupId = 0;

            AreaInfoCallback intersectionCallBack = new(iTreeValues);
            iTree.IntersectPoint(pos, intersectionCallBack);
            if (intersectionCallBack.aInfo.result)
            {
                flags = intersectionCallBack.aInfo.flags;
                adtId = intersectionCallBack.aInfo.adtId;
                rootId = intersectionCallBack.aInfo.rootId;
                groupId = intersectionCallBack.aInfo.groupId;
                pos.Z = intersectionCallBack.aInfo.ground_Z;
                return true;
            }
            return false;
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
        ModelInstance[] iTreeValues;
        uint iNTreeValues;
        Dictionary<uint, uint> iSpawnIndices = new();

        Dictionary<uint, bool> iLoadedTiles = new();
        Dictionary<uint, uint> iLoadedSpawns = new();
    }

    class TileFileOpenResult
    {
        public string Name;
        public FileStream File;
        public uint UsedMapId;
    }
}
