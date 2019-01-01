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
using System.Collections.Generic;
using System.IO;

namespace Game.Collision
{
    public class LocationInfo
    {
        public LocationInfo()
        {
            ground_Z = float.NegativeInfinity;
        }

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

        public bool InitMap(string fname)
        {
            Log.outDebug(LogFilter.Maps, "StaticMapTree.InitMap() : initializing StaticMapTree '{0}'", fname);
            bool success = false;
            if (!File.Exists(fname))
                return false;

            using (BinaryReader reader = new BinaryReader(new FileStream(fname, FileMode.Open, FileAccess.Read)))
            {
                var magic = reader.ReadStringFromChars(8);
                var node = reader.ReadStringFromChars(4);

                if (magic == MapConst.VMapMagic && node == "NODE" && iTree.readFromFile(reader))
                {
                    iNTreeValues = iTree.primCount();
                    iTreeValues = new ModelInstance[iNTreeValues];
                    success = true;
                }

                if (success)
                {
                    success = reader.ReadStringFromChars(4) == "SIDX";
                    if (success)
                    {
                        uint spawnIndicesSize = reader.ReadUInt32();
                        for (uint i = 0; i < spawnIndicesSize; ++i)
                        {
                            uint spawnId = reader.ReadUInt32();
                            uint spawnIndex = reader.ReadUInt32();
                            iSpawnIndices[spawnId] = spawnIndex;
                        }
                    }
                }
            }
            return success;
        }

        public void UnloadMap(VMapManager vm)
        {
            foreach (var id in iLoadedSpawns)
            {
                iTreeValues[id.Key].setUnloaded();
                for (uint refCount = 0; refCount < id.Key; ++refCount)
                    vm.releaseModelInstance(iTreeValues[id.Key].name);
            }
            iLoadedSpawns.Clear();
            iLoadedTiles.Clear();
        }

        public bool LoadMapTile(uint tileX, uint tileY, VMapManager vm)
        {
            if (iTreeValues == null)
            {
                Log.outError(LogFilter.Server, "StaticMapTree.LoadMapTile() : tree has not been initialized [{0}, {1}]", tileX, tileY);
                return false;
            }
            bool result = true;

            FileStream stream = OpenMapTileFile(VMapManager.VMapPath, iMapID, tileX, tileY, vm);
            if (stream == null)
            {
                iLoadedTiles[packTileID(tileX, tileY)] = false;
            }
            else
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    if (reader.ReadStringFromChars(8) != MapConst.VMapMagic)
                        return false;

                    uint numSpawns = reader.ReadUInt32();

                    for (uint i = 0; i < numSpawns && result; ++i)
                    {
                        // read model spawns
                        ModelSpawn spawn;
                        result = ModelSpawn.readFromFile(reader, out spawn);
                        if (result)
                        {
                            // acquire model instance
                            WorldModel model = vm.acquireModelInstance(spawn.name, spawn.flags);
                            if (model == null)
                                Log.outError(LogFilter.Server, "StaticMapTree.LoadMapTile() : could not acquire WorldModel [{0}, {1}]", tileX, tileY);

                            // update tree
                            if (iSpawnIndices.ContainsKey(spawn.ID))
                            {
                                uint referencedVal = iSpawnIndices[spawn.ID];
                                if (!iLoadedSpawns.ContainsKey(referencedVal))
                                {
                                    if (referencedVal >= iNTreeValues)
                                    {
                                        Log.outError(LogFilter.Maps, "StaticMapTree.LoadMapTile() : invalid tree element ({0}/{1}) referenced in tile {2}", referencedVal, iNTreeValues, stream.Name);
                                        continue;
                                    }

                                    iTreeValues[referencedVal] = new ModelInstance(spawn, model);
                                    iLoadedSpawns[referencedVal] = 1;
                                }
                                else
                                    ++iLoadedSpawns[referencedVal];
                            }
                            else
                                result = false;

                        }
                    }
                }
                iLoadedTiles[packTileID(tileX, tileY)] = true;
            }
            return result;
        }

        public void UnloadMapTile(uint tileX, uint tileY, VMapManager vm)
        {
            uint tileID = packTileID(tileX, tileY);
            var tile = iLoadedTiles.LookupByKey(tileID);
            if (!iLoadedTiles.ContainsKey(tileID))
            {
                Log.outError(LogFilter.Server, "StaticMapTree.UnloadMapTile() : trying to unload non-loaded tile - Map:{0} X:{1} Y:{2}", iMapID, tileX, tileY);
                return;
            }
            if (tile) // file associated with tile
            {
                FileStream stream = OpenMapTileFile(VMapManager.VMapPath, iMapID, tileX, tileY, vm);
                if (stream != null)
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        bool result = true;
                        if (reader.ReadStringFromChars(8) != MapConst.VMapMagic)
                            result = false;

                        uint numSpawns = reader.ReadUInt32();
                        for (uint i = 0; i < numSpawns && result; ++i)
                        {
                            // read model spawns
                            ModelSpawn spawn;
                            result = ModelSpawn.readFromFile(reader, out spawn);
                            if (result)
                            {
                                // release model instance
                                vm.releaseModelInstance(spawn.name);

                                // update tree
                                if (!iSpawnIndices.ContainsKey(spawn.ID))
                                    result = false;
                                else
                                {
                                    uint referencedNode = iSpawnIndices[spawn.ID];
                                    if (!iLoadedSpawns.ContainsKey(referencedNode))
                                        Log.outError(LogFilter.Server, "StaticMapTree.UnloadMapTile() : trying to unload non-referenced model '{0}' (ID:{1})", spawn.name, spawn.ID);
                                    else if (--iLoadedSpawns[referencedNode] == 0)
                                    {
                                        iTreeValues[referencedNode].setUnloaded();
                                        iLoadedSpawns.Remove(referencedNode);
                                    }
                                }

                            }
                        }
                    }
                }
            }
            iLoadedTiles.Remove(tileID);
        }
        
        static uint packTileID(uint tileX, uint tileY) { return tileX << 16 | tileY; }
        static void unpackTileID(uint ID, ref uint tileX, ref uint tileY) { tileX = ID >> 16; tileY = ID & 0xFF; }

        static FileStream OpenMapTileFile(string vmapPath, uint mapID, uint tileX, uint tileY, VMapManager vm)
        {
            string tilefile = vmapPath + getTileFileName(mapID, tileX, tileY);
            if (!File.Exists(tilefile))
            {
                int parentMapId = vm.getParentMapId(mapID);
                if (parentMapId != -1)
                    tilefile = vmapPath + getTileFileName((uint)parentMapId, tileX, tileY);
            }

            if (!File.Exists(tilefile))
                return null;

            return new FileStream(tilefile, FileMode.Open, FileAccess.Read);
        }

        public static LoadResult CanLoadMap(string vmapPath, uint mapID, uint tileX, uint tileY, VMapManager vm)
        {
            string fullname = vmapPath + VMapManager.getMapFileName(mapID);
            if (!File.Exists(fullname))
                return LoadResult.FileNotFound;

            using (BinaryReader reader = new BinaryReader(new FileStream(fullname, FileMode.Open, FileAccess.Read)))
            {
                if (reader.ReadStringFromChars(8) != MapConst.VMapMagic)
                    return LoadResult.VersionMismatch;
            }

            FileStream stream = OpenMapTileFile(vmapPath, mapID, tileX, tileY, vm);
            if (stream == null)
                return LoadResult.FileNotFound;

            using (BinaryReader reader = new BinaryReader(stream))
            {
                if (reader.ReadStringFromChars(8) != MapConst.VMapMagic)
                    return LoadResult.VersionMismatch;
            }

            return LoadResult.Success;
        }

        public static string getTileFileName(uint mapID, uint tileX, uint tileY)
        {
            return $"{mapID:D4}_{tileY:D2}_{tileX:D2}.vmtile";
        }

        public bool getAreaInfo(ref Vector3 pos, out uint flags, out int adtId, out int rootId, out int groupId)
        {
            flags = 0;
            adtId = 0;
            rootId = 0;
            groupId = 0;

            AreaInfoCallback intersectionCallBack = new AreaInfoCallback(iTreeValues);
            iTree.intersectPoint(pos, intersectionCallBack);
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
            LocationInfoCallback intersectionCallBack = new LocationInfoCallback(iTreeValues, info);
            iTree.intersectPoint(pos, intersectionCallBack);
            return intersectionCallBack.result;
        }

        public float getHeight(Vector3 pPos, float maxSearchDist)
        {
            float height = float.PositiveInfinity;
            Vector3 dir = new Vector3(0, 0, -1);
            Ray ray = new Ray(pPos, dir);   // direction with length of 1
            float maxDist = maxSearchDist;
            if (getIntersectionTime(ray, ref maxDist, false, ModelIgnoreFlags.Nothing))
                height = pPos.Z - maxDist;

            return height;
        }
        bool getIntersectionTime(Ray pRay, ref float pMaxDist, bool pStopAtFirstHit, ModelIgnoreFlags ignoreFlags)
        {
            float distance = pMaxDist;
            MapRayCallback intersectionCallBack = new MapRayCallback(iTreeValues, ignoreFlags);
            iTree.intersectRay(pRay, intersectionCallBack, ref distance, pStopAtFirstHit);
            if (intersectionCallBack.didHit())
                pMaxDist = distance;
            return intersectionCallBack.didHit();
        }

        public bool getObjectHitPos(Vector3 pPos1, Vector3 pPos2, out Vector3 pResultHitPos, float pModifyDist)
        {
            bool result = false;
            float maxDist = (pPos2 - pPos1).magnitude();
            // valid map coords should *never ever* produce float overflow, but this would produce NaNs too
            Cypher.Assert(maxDist < float.MaxValue);
            // prevent NaN values which can cause BIH intersection to enter infinite loop
            if (maxDist < 1e-10f)
            {
                pResultHitPos = pPos2;
                return false;
            }
            Vector3 dir = (pPos2 - pPos1) / maxDist;              // direction with length of 1
            Ray ray = new Ray(pPos1, dir);
            float dist = maxDist;
            if (getIntersectionTime(ray, ref dist, false, ModelIgnoreFlags.Nothing))
            {
                pResultHitPos = pPos1 + dir * dist;
                if (pModifyDist < 0)
                {
                    if ((pResultHitPos - pPos1).magnitude() > -pModifyDist)
                    {
                        pResultHitPos = pResultHitPos + dir * pModifyDist;
                    }
                    else
                    {
                        pResultHitPos = pPos1;
                    }
                }
                else
                {
                    pResultHitPos = pResultHitPos + dir * pModifyDist;
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

        public bool isInLineOfSight(Vector3 pos1, Vector3 pos2, ModelIgnoreFlags ignoreFlags)
        {
            float maxDist = (pos2 - pos1).magnitude();
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
            Ray ray = new Ray(pos1, (pos2 - pos1) / maxDist);
            if (getIntersectionTime(ray, ref maxDist, true, ignoreFlags))
                return false;

            return true;
        }

        public int numLoadedTiles() { return iLoadedTiles.Count; }

        uint iMapID;
        BIH iTree = new BIH();
        ModelInstance[] iTreeValues;
        uint iNTreeValues;
        Dictionary<uint, uint> iSpawnIndices = new Dictionary<uint, uint>();

        Dictionary<uint, bool> iLoadedTiles = new Dictionary<uint, bool>();
        Dictionary<uint, uint> iLoadedSpawns = new Dictionary<uint, uint>();
    }
}
