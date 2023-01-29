// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;

namespace Game.Collision
{

    public class VMapManager : Singleton<VMapManager>
    {
        public static string VMapPath = Global.WorldMgr.GetDataPath() + "/vmaps/";
        private readonly Dictionary<uint, StaticMapTree> _iInstanceMapTrees = new();

        private readonly Dictionary<string, ManagedModel> _iLoadedModelFiles = new();
        private readonly Dictionary<uint, uint> _iParentMapData = new();

        private readonly object _loadedModelFilesLock = new();
        private bool _enableHeightCalc;
        private bool _enableLineOfSightCalc;

        private VMapManager()
        {
        }

        public void Initialize(MultiMap<uint, uint> mapData)
        {
            foreach (var pair in mapData)
                _iParentMapData[pair.Value] = pair.Key;
        }

        public LoadResult LoadMap(uint mapId, int x, int y)
        {
            if (!IsMapLoadingEnabled())
                return LoadResult.DisabledInConfig;

            var instanceTree = _iInstanceMapTrees.LookupByKey(mapId);

            if (instanceTree == null)
            {
                string filename = VMapPath + GetMapFileName(mapId);
                StaticMapTree newTree = new(mapId);
                LoadResult treeInitResult = newTree.InitMap(filename);

                if (treeInitResult != LoadResult.Success)
                    return treeInitResult;

                _iInstanceMapTrees.Add(mapId, newTree);

                instanceTree = newTree;
            }

            return instanceTree.LoadMapTile(x, y, this);
        }

        public void UnloadMap(uint mapId, int x, int y)
        {
            var instanceTree = _iInstanceMapTrees.LookupByKey(mapId);

            if (instanceTree != null)
            {
                instanceTree.UnloadMapTile(x, y, this);

                if (instanceTree.NumLoadedTiles() == 0)
                    _iInstanceMapTrees.Remove(mapId);
            }
        }

        public void UnloadMap(uint mapId)
        {
            var instanceTree = _iInstanceMapTrees.LookupByKey(mapId);

            if (instanceTree != null)
            {
                instanceTree.UnloadMap(this);

                if (instanceTree.NumLoadedTiles() == 0)
                    _iInstanceMapTrees.Remove(mapId);
            }
        }

        public bool IsInLineOfSight(uint mapId, float x1, float y1, float z1, float x2, float y2, float z2, ModelIgnoreFlags ignoreFlags)
        {
            if (!IsLineOfSightCalcEnabled() ||
                Global.DisableMgr.IsVMAPDisabledFor(mapId, (byte)DisableFlags.VmapLOS))
                return true;

            var instanceTree = _iInstanceMapTrees.LookupByKey(mapId);

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
            if (IsLineOfSightCalcEnabled() &&
                !Global.DisableMgr.IsVMAPDisabledFor(mapId, (byte)DisableFlags.VmapLOS))
            {
                var instanceTree = _iInstanceMapTrees.LookupByKey(mapId);

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
            if (IsHeightCalcEnabled() &&
                !Global.DisableMgr.IsVMAPDisabledFor(mapId, (byte)DisableFlags.VmapHeight))
            {
                var instanceTree = _iInstanceMapTrees.LookupByKey(mapId);

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
                var instanceTree = _iInstanceMapTrees.LookupByKey(mapId);

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
                var instanceTree = _iInstanceMapTrees.LookupByKey(mapId);

                if (instanceTree != null)
                {
                    LocationInfo info = new();
                    Vector3 pos = ConvertPositionToInternalRep(x, y, z);

                    if (instanceTree.GetLocationInfo(pos, info))
                    {
                        floor = info.Ground_Z;
                        Cypher.Assert(floor < float.MaxValue);
                        type = info.HitModel.GetLiquidType(); // entry from LiquidType.dbc
                        mogpFlags = info.HitModel.GetMogpFlags();

                        if (reqLiquidType != 0 &&
                            !Convert.ToBoolean(Global.DB2Mgr.GetLiquidFlags(type) & reqLiquidType))
                            return false;

                        if (info.HitInstance.GetLiquidLevel(pos, info, ref level))
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
                data.FloorZ = z;
                int adtId, rootId, groupId;
                uint flags;

                if (GetAreaInfo(mapId, x, y, ref data.FloorZ, out flags, out adtId, out rootId, out groupId))
                    data.AreaInfoData = new AreaAndLiquidData.AreaInfo(adtId, rootId, groupId, flags);

                return data;
            }

            var instanceTree = _iInstanceMapTrees.LookupByKey(mapId);

            if (instanceTree != null)
            {
                LocationInfo info = new();
                Vector3 pos = ConvertPositionToInternalRep(x, y, z);

                if (instanceTree.GetLocationInfo(pos, info))
                {
                    data.FloorZ = info.Ground_Z;
                    uint liquidType = info.HitModel.GetLiquidType();
                    float liquidLevel = 0;

                    if (reqLiquidType == 0 ||
                        Convert.ToBoolean(Global.DB2Mgr.GetLiquidFlags(liquidType) & reqLiquidType))
                        if (info.HitInstance.GetLiquidLevel(pos, info, ref liquidLevel))
                            data.LiquidInfoData = new AreaAndLiquidData.LiquidInfo(liquidType, liquidLevel);

                    if (!Global.DisableMgr.IsVMAPDisabledFor(mapId, (byte)DisableFlags.VmapLiquidStatus))
                        data.AreaInfoData = new AreaAndLiquidData.AreaInfo(info.HitInstance.adtId, info.RootId, (int)info.HitModel.GetWmoID(), info.HitModel.GetMogpFlags());
                }
            }

            return data;
        }

        public WorldModel AcquireModelInstance(string filename, uint flags = 0)
        {
            lock (_loadedModelFilesLock)
            {
                filename = filename.TrimEnd('\0');
                var model = _iLoadedModelFiles.LookupByKey(filename);

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

                    _iLoadedModelFiles.Add(filename, model);
                }

                model.IncRefCount();

                return model.GetModel();
            }
        }

        public void ReleaseModelInstance(string filename)
        {
            lock (_loadedModelFilesLock)
            {
                filename = filename.TrimEnd('\0');
                var model = _iLoadedModelFiles.LookupByKey(filename);

                if (model == null)
                {
                    Log.outError(LogFilter.Server, "VMapManager: trying to unload non-loaded file '{0}'", filename);

                    return;
                }

                if (model.DecRefCount() == 0)
                {
                    Log.outDebug(LogFilter.Maps, "VMapManager: unloading file '{0}'", filename);
                    _iLoadedModelFiles.Remove(filename);
                }
            }
        }

        public LoadResult ExistsMap(uint mapId, int x, int y)
        {
            return StaticMapTree.CanLoadMap(VMapPath, mapId, x, y, this);
        }

        public int GetParentMapId(uint mapId)
        {
            if (_iParentMapData.ContainsKey(mapId))
                return (int)_iParentMapData[mapId];

            return -1;
        }

        public static string GetMapFileName(uint mapId)
        {
            return $"{mapId:D4}.vmtree";
        }

        public void SetEnableLineOfSightCalc(bool pVal)
        {
            _enableLineOfSightCalc = pVal;
        }

        public void SetEnableHeightCalc(bool pVal)
        {
            _enableHeightCalc = pVal;
        }

        public bool IsLineOfSightCalcEnabled()
        {
            return _enableLineOfSightCalc;
        }

        public bool IsHeightCalcEnabled()
        {
            return _enableHeightCalc;
        }

        public bool IsMapLoadingEnabled()
        {
            return _enableLineOfSightCalc || _enableHeightCalc;
        }

        private Vector3 ConvertPositionToInternalRep(float x, float y, float z)
        {
            Vector3 pos = new();
            float mid = 0.5f * 64.0f * 533.33333333f;
            pos.X = mid - x;
            pos.Y = mid - y;
            pos.Z = z;

            return pos;
        }
    }
}