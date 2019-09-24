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

namespace Game.Collision
{
    public enum VMAPLoadResult
    {
        Error,
        OK,
        Ignored
    }

    public enum LoadResult
    {
        Success,
        FileNotFound,
        VersionMismatch
    }

    public class VMapManager : Singleton<VMapManager>
    {
        VMapManager() { }

        public static string VMapPath = Global.WorldMgr.GetDataPath() + "/vmaps/";

        public void Initialize(MultiMap<uint, uint> mapData)
        {
            iChildMapData = mapData;
            foreach (var pair in mapData)
                iParentMapData[pair.Value] = pair.Key;
        }

        public VMAPLoadResult LoadMap(uint mapId, uint x, uint y)
        {
            var result = VMAPLoadResult.Ignored;
            if (IsMapLoadingEnabled())
            {
                if (LoadSingleMap(mapId, x, y))
                {
                    result = VMAPLoadResult.OK;
                    var childMaps = iChildMapData.LookupByKey(mapId);
                    foreach (uint childMapId in childMaps)
                        if (!LoadSingleMap(childMapId, x, y))
                            result = VMAPLoadResult.Error;
                }
                else
                    result = VMAPLoadResult.Error;
            }

            return result;
        }

        bool LoadSingleMap(uint mapId, uint tileX, uint tileY)
        {
            var instanceTree = iInstanceMapTrees.LookupByKey(mapId);
            if (instanceTree == null)
            {
                string filename = VMapPath + GetMapFileName(mapId);
                StaticMapTree newTree = new StaticMapTree(mapId);
                if (!newTree.InitMap(filename))
                    return false;

                iInstanceMapTrees.Add(mapId, newTree);

                instanceTree = newTree;
            }

            return instanceTree.LoadMapTile(tileX, tileY, this);
        }

        public void UnloadMap(uint mapId, uint x, uint y)
        {
            var childMaps = iChildMapData.LookupByKey(mapId);
            foreach (uint childMapId in childMaps)
                UnloadSingleMap(childMapId, x, y);

            UnloadSingleMap(mapId, x, y);
        }

        void UnloadSingleMap(uint mapId, uint x, uint y)
        {
            var instanceTree = iInstanceMapTrees.LookupByKey(mapId);
            if (instanceTree != null)
            {
                instanceTree.UnloadMapTile(x, y, this);
                if (instanceTree.NumLoadedTiles() == 0)
                {
                    iInstanceMapTrees.Remove(mapId);
                }
            }
        }

        public void UnloadMap(uint mapId)
        {
            var childMaps = iChildMapData.LookupByKey(mapId);
            foreach (uint childMapId in childMaps)
                UnloadSingleMap(childMapId);

            UnloadSingleMap(mapId);
        }

        void UnloadSingleMap(uint mapId)
        {
            var instanceTree = iInstanceMapTrees.LookupByKey(mapId);
            if (instanceTree != null)
            {
                instanceTree.UnloadMap(this);
                if (instanceTree.NumLoadedTiles() == 0)
                {
                    iInstanceMapTrees.Remove(mapId);
                }
            }
        }

        public bool IsInLineOfSight(uint mapId, float x1, float y1, float z1, float x2, float y2, float z2, ModelIgnoreFlags ignoreFlags)
        {
            if (!IsLineOfSightCalcEnabled() || Global.DisableMgr.IsDisabledFor(DisableType.VMAP, mapId, null, DisableFlags.VmapLOS))
                return true;

            var instanceTree = iInstanceMapTrees.LookupByKey(mapId);
            if (instanceTree != null)
            {
                Vector3 pos1 = ConvertPositionToInternalRep(x1, y1, z1);
                Vector3 pos2 = ConvertPositionToInternalRep(x2, y2, z2);
                if (pos1 != pos2)
                    return instanceTree.IsInLineOfSight(pos1, pos2, ignoreFlags);
            }

            return true;
        }

        public bool GetObjectHitPos(uint mapId, float x1, float y1, float z1, float x2, float y2, float z2, out float rx, out float ry, out float rz, float modifyDist)
        {
            if (IsLineOfSightCalcEnabled() && !Global.DisableMgr.IsDisabledFor(DisableType.VMAP, mapId, null, DisableFlags.VmapLOS))
            {
                var instanceTree = iInstanceMapTrees.LookupByKey(mapId);
                if (instanceTree != null)
                {
                    Vector3 resultPos;
                    Vector3 pos1 = ConvertPositionToInternalRep(x1, y1, z1);
                    Vector3 pos2 = ConvertPositionToInternalRep(x2, y2, z2);
                    bool result = instanceTree.GetObjectHitPos(pos1, pos2, out resultPos, modifyDist);
                    resultPos = ConvertPositionToInternalRep(resultPos.X, resultPos.Y, resultPos.Z);
                    rx = resultPos.X;
                    ry = resultPos.Y;
                    rz = resultPos.Z;
                    return result;
                }
            }

            rx = x2;
            ry = y2;
            rz = z2;

            return false;
        }

        public float GetHeight(uint mapId, float x, float y, float z, float maxSearchDist)
        {
            if (IsHeightCalcEnabled() && !Global.DisableMgr.IsDisabledFor(DisableType.VMAP, mapId, null, DisableFlags.VmapHeight))
            {
                var instanceTree = iInstanceMapTrees.LookupByKey(mapId);
                if (instanceTree != null)
                {
                    Vector3 pos = ConvertPositionToInternalRep(x, y, z);
                    float height = instanceTree.GetHeight(pos, maxSearchDist);
                    if (float.IsInfinity(height))
                        height = MapConst.VMAPInvalidHeightValue; // No height

                    return height;
                }
            }

            return MapConst.VMAPInvalidHeightValue;
        }

        public bool GetAreaInfo(uint mapId, float x, float y, ref float z, out uint flags, out int adtId, out int rootId, out int groupId)
        {
            flags = 0;
            adtId = 0;
            rootId = 0;
            groupId = 0;
            if (!Global.DisableMgr.IsDisabledFor(DisableType.VMAP, mapId, null, DisableFlags.VmapAreaFlag))
            {
                var instanceTree = iInstanceMapTrees.LookupByKey(mapId);
                if (instanceTree != null)
                {
                    Vector3 pos = ConvertPositionToInternalRep(x, y, z);
                    bool result = instanceTree.GetAreaInfo(ref pos, out flags, out adtId, out rootId, out groupId);
                    // z is not touched by convertPositionToInternalRep(), so just copy
                    z = pos.Z;
                    return result;
                }
            }

            return false;
        }

        public bool GetLiquidLevel(uint mapId, float x, float y, float z, uint reqLiquidType, ref float level, ref float floor, ref uint type)
        {
            if (!Global.DisableMgr.IsDisabledFor(DisableType.VMAP, mapId, null, DisableFlags.VmapLiquidStatus))
            {
                var instanceTree = iInstanceMapTrees.LookupByKey(mapId);
                if (instanceTree != null)
                {
                    LocationInfo info = new LocationInfo();
                    Vector3 pos = ConvertPositionToInternalRep(x, y, z);
                    if (instanceTree.GetLocationInfo(pos, info))
                    {
                        floor = info.ground_Z;
                        Cypher.Assert(floor < float.MaxValue);
                        type = info.hitModel.GetLiquidType();  // entry from LiquidType.dbc
                        if (reqLiquidType != 0 && !Convert.ToBoolean(Global.DB2Mgr.GetLiquidFlags(type) & reqLiquidType))
                            return false;
                        if (info.hitInstance.GetLiquidLevel(pos, info, ref level))
                            return true;
                    }
                }
            }
            return false;
        }

        public WorldModel AcquireModelInstance(string filename, uint flags = 0)
        {
            lock (LoadedModelFilesLock)
            {
                filename = filename.TrimEnd('\0');
                var model = iLoadedModelFiles.LookupByKey(filename);
                if (model == null)
                {
                    WorldModel worldmodel = new WorldModel();
                    if (!worldmodel.ReadFile(VMapPath + filename))
                    {
                        Log.outError(LogFilter.Server, "VMapManager: could not load '{0}'", filename);
                        return null;
                    }

                    Log.outDebug(LogFilter.Maps, "VMapManager: loading file '{0}'", filename);

                    worldmodel.Flags = flags;

                    model = new ManagedModel();
                    model.SetModel(worldmodel);

                    iLoadedModelFiles.Add(filename, model);
                }
                model.IncRefCount();
                return model.GetModel();
            }
        }

        public void ReleaseModelInstance(string filename)
        {
            lock (LoadedModelFilesLock)
            {
                filename = filename.TrimEnd('\0');
                var model = iLoadedModelFiles.LookupByKey(filename);
                if (model == null)
                {
                    Log.outError(LogFilter.Server, "VMapManager: trying to unload non-loaded file '{0}'", filename);
                    return;
                }
                if (model.DecRefCount() == 0)
                {
                    Log.outDebug(LogFilter.Maps, "VMapManager: unloading file '{0}'", filename);
                    iLoadedModelFiles.Remove(filename);
                }
            }
        }

        public LoadResult ExistsMap(uint mapId, uint x, uint y)
        {
            return StaticMapTree.CanLoadMap(VMapPath, mapId, x, y, this);
        }

        public int GetParentMapId(uint mapId)
        {
            if (iParentMapData.ContainsKey(mapId))
                return (int)iParentMapData[mapId];

            return -1;
        }

        Vector3 ConvertPositionToInternalRep(float x, float y, float z)
        {
            Vector3 pos = new Vector3();
            float mid = 0.5f * 64.0f * 533.33333333f;
            pos.X = mid - x;
            pos.Y = mid - y;
            pos.Z = z;

            return pos;
        }

        public static string GetMapFileName(uint mapId)
        {
            return $"{mapId:D4}.vmtree";
        }

        public void SetEnableLineOfSightCalc(bool pVal) { _enableLineOfSightCalc = pVal; }
        public void SetEnableHeightCalc(bool pVal) { _enableHeightCalc = pVal; }

        public bool IsLineOfSightCalcEnabled() { return _enableLineOfSightCalc; }
        public bool IsHeightCalcEnabled() { return _enableHeightCalc; }
        public bool IsMapLoadingEnabled() { return _enableLineOfSightCalc || _enableHeightCalc; }

        Dictionary<string, ManagedModel> iLoadedModelFiles = new Dictionary<string, ManagedModel>();
        Dictionary<uint, StaticMapTree> iInstanceMapTrees = new Dictionary<uint, StaticMapTree>();
        MultiMap<uint, uint> iChildMapData = new MultiMap<uint, uint>();
        Dictionary<uint, uint> iParentMapData = new Dictionary<uint, uint>();
        bool _enableLineOfSightCalc;
        bool _enableHeightCalc;

        object LoadedModelFilesLock = new object();
    }

    public class ManagedModel
    {
        public ManagedModel()
        {
            iModel = null;
            iRefCount = 0;
        }

        public void SetModel(WorldModel model) { iModel = model; }
        public WorldModel GetModel() { return iModel; }
        public void IncRefCount() { ++iRefCount; }
        public int DecRefCount() { return --iRefCount; }

        WorldModel iModel;
        int iRefCount;
    }
}
