// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using System;
using System.Collections.Generic;
using System.Numerics;

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
        VersionMismatch,
        ReadFromFileFailed,
        DisabledInConfig
    }

    public class VMapManager : Singleton<VMapManager>
    {
        VMapManager() { }

        public static string VMapPath = Global.WorldMgr.GetDataPath() + "/vmaps/";

        public void Initialize(MultiMap<uint, uint> mapData)
        {
            foreach (var pair in mapData)
                iParentMapData[pair.Value] = pair.Key;
        }

        public LoadResult LoadMap(uint mapId, int x, int y)
        {
            if (!IsMapLoadingEnabled())
                return LoadResult.DisabledInConfig;

            var instanceTree = iInstanceMapTrees.LookupByKey(mapId);
            if (instanceTree == null)
            {
                string filename = VMapPath + GetMapFileName(mapId);
                StaticMapTree newTree = new(mapId);
                LoadResult treeInitResult = newTree.InitMap(filename);
                if (treeInitResult != LoadResult.Success)
                    return treeInitResult;

                iInstanceMapTrees.Add(mapId, newTree);

                instanceTree = newTree;
            }

            return instanceTree.LoadMapTile(x, y, this);
        }

        public void UnloadMap(uint mapId, int x, int y)
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
            if (!IsLineOfSightCalcEnabled() || Global.DisableMgr.IsVMAPDisabledFor(mapId, (byte)DisableFlags.VmapLOS))
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
            if (IsLineOfSightCalcEnabled() && !Global.DisableMgr.IsVMAPDisabledFor(mapId, (byte)DisableFlags.VmapLOS))
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
            if (IsHeightCalcEnabled() && !Global.DisableMgr.IsVMAPDisabledFor(mapId, (byte)DisableFlags.VmapHeight))
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
            if (!Global.DisableMgr.IsVMAPDisabledFor(mapId, (byte)DisableFlags.VmapAreaFlag))
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

        public bool GetLiquidLevel(uint mapId, float x, float y, float z, uint reqLiquidType, ref float level, ref float floor, ref uint type, ref uint mogpFlags)
        {
            if (!Global.DisableMgr.IsVMAPDisabledFor(mapId, (byte)DisableFlags.VmapLiquidStatus))
            {
                var instanceTree = iInstanceMapTrees.LookupByKey(mapId);
                if (instanceTree != null)
                {
                    LocationInfo info = new();
                    Vector3 pos = ConvertPositionToInternalRep(x, y, z);
                    if (instanceTree.GetLocationInfo(pos, info))
                    {
                        floor = info.ground_Z;
                        Cypher.Assert(floor < float.MaxValue);
                        type = info.hitModel.GetLiquidType();  // entry from LiquidType.dbc
                        mogpFlags = info.hitModel.GetMogpFlags();
                        if (reqLiquidType != 0 && !Convert.ToBoolean(Global.DB2Mgr.GetLiquidFlags(type) & reqLiquidType))
                            return false;
                        if (info.hitInstance.GetLiquidLevel(pos, info, ref level))
                            return true;
                    }
                }
            }
            return false;
        }

        public AreaAndLiquidData GetAreaAndLiquidData(uint mapId, float x, float y, float z, uint reqLiquidType)
        {
            var data = new AreaAndLiquidData();

            if (Global.DisableMgr.IsVMAPDisabledFor(mapId, (byte)DisableFlags.VmapLiquidStatus))
            {
                data.floorZ = z;
                int adtId, rootId, groupId;
                uint flags;
                if (GetAreaInfo(mapId, x, y, ref data.floorZ, out flags, out adtId, out rootId, out groupId))
                    data.areaInfo = new(adtId, rootId, groupId, flags);
                return data;
            }
            var instanceTree = iInstanceMapTrees.LookupByKey(mapId);
            if (instanceTree != null)
            {
                LocationInfo info = new();
                Vector3 pos = ConvertPositionToInternalRep(x, y, z);
                if (instanceTree.GetLocationInfo(pos, info))
                {
                    data.floorZ = info.ground_Z;
                    uint liquidType = info.hitModel.GetLiquidType();
                    float liquidLevel = 0;
                    if (reqLiquidType == 0 || Convert.ToBoolean(Global.DB2Mgr.GetLiquidFlags(liquidType) & reqLiquidType))
                        if (info.hitInstance.GetLiquidLevel(pos, info, ref liquidLevel))
                            data.liquidInfo = new(liquidType, liquidLevel);

                    if (!Global.DisableMgr.IsVMAPDisabledFor(mapId, (byte)DisableFlags.VmapLiquidStatus))
                        data.areaInfo = new(info.hitInstance.adtId, info.rootId, (int)info.hitModel.GetWmoID(), info.hitModel.GetMogpFlags());
                }
            }

            return data;
        }

        public WorldModel AcquireModelInstance(string filename, uint flags = 0)
        {
            lock (LoadedModelFilesLock)
            {
                filename = filename.TrimEnd('\0');
                var model = iLoadedModelFiles.LookupByKey(filename);
                if (model == null)
                {
                    model = new ManagedModel();
                    if (!model.GetModel().ReadFile(VMapPath + filename))
                    {
                        Log.outError(LogFilter.Server, "VMapManager: could not load '{0}'", filename);
                        return null;
                    }

                    Log.outDebug(LogFilter.Maps, "VMapManager: loading file '{0}'", filename);
                    model.GetModel().Flags = flags;

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

        public LoadResult ExistsMap(uint mapId, int x, int y)
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
            Vector3 pos = new();
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

        Dictionary<string, ManagedModel> iLoadedModelFiles = new();
        Dictionary<uint, StaticMapTree> iInstanceMapTrees = new();
        Dictionary<uint, uint> iParentMapData = new();
        bool _enableLineOfSightCalc;
        bool _enableHeightCalc;

        object LoadedModelFilesLock = new();
    }

    public class ManagedModel
    {
        public ManagedModel()
        {
            iModel = new();
            iRefCount = 0;
        }

        public void SetModel(WorldModel model) { iModel = model; }
        public WorldModel GetModel() { return iModel; }
        public void IncRefCount() { ++iRefCount; }
        public int DecRefCount() { return --iRefCount; }

        WorldModel iModel;
        int iRefCount;
    }

    public class AreaAndLiquidData
    {
        public struct AreaInfo
        {
            public int AdtId;
            public int RootId;
            public int GroupId;
            public uint MogpFlags;

            public AreaInfo(int adtId, int rootId, int groupId, uint flags)
            {
                AdtId = adtId;
                RootId = rootId;
                GroupId = groupId;
                MogpFlags = flags;
            }
        }
        public struct LiquidInfo
        {
            public uint LiquidType;
            public float Level;

            public LiquidInfo(uint type, float level)
            {
                LiquidType = type;
                Level = level;
            }
        }

        public float floorZ = MapConst.VMAPInvalidHeightValue;
        public AreaInfo? areaInfo;
        public LiquidInfo? liquidInfo;
    }
}
