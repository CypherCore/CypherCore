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

using Framework.Constants;
using Framework.GameMath;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Game.Collision
{
    public enum VMAPLoadResult
    {
        Error,
        OK,
        Ignored
    }

    public class VMapManager : Singleton<VMapManager>
    {
        VMapManager() { }

        public static string VMapPath = Global.WorldMgr.GetDataPath() + "/vmaps/";

        public VMAPLoadResult loadMap(uint mapId, uint x, uint y)
        {
            var result = VMAPLoadResult.Ignored;
            if (_loadMap(mapId, x, y))
                result = VMAPLoadResult.OK;
            else
                result = VMAPLoadResult.Error;

            return result;
        }

        bool _loadMap(uint mapId, uint tileX, uint tileY)
        {
            var instanceTree = iInstanceMapTrees.LookupByKey(mapId);
            if (instanceTree == null)
            {
                string filename = string.Format("{0}{1:D4}.vmtree", VMapPath, mapId);
                StaticMapTree newTree = new StaticMapTree(mapId);
                if (!newTree.InitMap(filename, this))
                    return false;

                iInstanceMapTrees.Add(mapId, newTree);

                instanceTree = newTree;
            }

            return instanceTree.LoadMapTile(tileX, tileY, this);
        }

        public WorldModel acquireModelInstance(string filename)
        {
            var model = iLoadedModelFiles.LookupByKey(filename);
            if (model == null)
            {
                WorldModel worldmodel = new WorldModel();
                if (!worldmodel.readFile(VMapPath + filename + ".vmo"))
                {
                    Log.outError(LogFilter.Server, "VMapManager: could not load '{0}.vmo'", filename);
                    return null;
                }
                Log.outDebug(LogFilter.Maps, "VMapManager: loading file '{0}'", filename);
                iLoadedModelFiles.Add(filename, new ManagedModel());
                model = iLoadedModelFiles.LookupByKey(filename);
                model.setModel(worldmodel);
            }
            model.incRefCount();
            return model.getModel();
        }

        public void releaseModelInstance(string filename)
        {
            var model = iLoadedModelFiles.LookupByKey(filename);
            if (model == null)
            {
                Log.outError(LogFilter.Server, "VMapManager: trying to unload non-loaded file '{0}'", filename);
                return;
            }
            if (model.decRefCount() == 0)
            {
                Log.outDebug(LogFilter.Maps, "VMapManager: unloading file '{0}'", filename);
                iLoadedModelFiles.Remove(filename);
            }
        }

        public bool existsMap(uint mapId, uint x, uint y)
        {
            return StaticMapTree.CanLoadMap(VMapPath, mapId, x, y);
        }

        public static string getMapFileName(uint mapId)
        {
            return string.Format("{0:D4}.vmtree", mapId);
        }

        public bool GetLiquidLevel(uint mapId, float x, float y, float z, byte reqLiquidType, ref float level, ref float floor, ref uint type)
        {
            if (!Global.DisableMgr.IsDisabledFor(DisableType.VMAP, mapId, null, DisableFlags.VmapLiquidStatus))
            {
                var instanceTree = iInstanceMapTrees.LookupByKey(mapId);
                if (instanceTree != null)
                {
                    LocationInfo info = new LocationInfo();
                    Vector3 pos = convertPositionToInternalRep(x, y, z);
                    if (instanceTree.GetLocationInfo(pos, info))
                    {
                        floor = info.ground_Z;
                        Contract.Assert(floor < float.MaxValue);
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

        public float getHeight(uint mapId, float x, float y, float z, float maxSearchDist)
        {
            if (isHeightCalcEnabled() && !Global.DisableMgr.IsDisabledFor(DisableType.VMAP, mapId, null, DisableFlags.VmapHeight))
            {
                var instanceTree = iInstanceMapTrees.LookupByKey(mapId);
                if (instanceTree != null)
                {
                    Vector3 pos = convertPositionToInternalRep(x, y, z);
                    float height = instanceTree.getHeight(pos, maxSearchDist);
                    if (float.IsInfinity(height))
                        height = MapConst.VMAPInvalidHeightValue; // No height

                    return height;
                }
            }

            return MapConst.VMAPInvalidHeightValue;
        }

        public bool getAreaInfo(uint mapId, float x, float y, ref float z, out uint flags, out int adtId, out int rootId, out int groupId)
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
                    Vector3 pos = convertPositionToInternalRep(x, y, z);
                    bool result = instanceTree.getAreaInfo(ref pos, out flags, out adtId, out rootId, out groupId);
                    // z is not touched by convertPositionToInternalRep(), so just copy
                    z = pos.Z;
                    return result;
                }
            }

            return false;
        }

        Vector3 convertPositionToInternalRep(float x, float y, float z)
        {
            Vector3 pos = new Vector3();
            float mid = 0.5f * 64.0f * 533.33333333f;
            pos.X = mid - x;
            pos.Y = mid - y;
            pos.Z = z;

            return pos;
        }

        public void setEnableLineOfSightCalc(bool pVal) { _enableLineOfSightCalc = pVal; }
        public void setEnableHeightCalc(bool pVal) { _enableHeightCalc = pVal; }

        public bool isLineOfSightCalcEnabled() { return _enableLineOfSightCalc; }
        public bool isHeightCalcEnabled() { return _enableHeightCalc; }
        public bool isMapLoadingEnabled() { return _enableLineOfSightCalc || _enableHeightCalc; }

        public bool getObjectHitPos(uint mapId, float x1, float y1, float z1, float x2, float y2, float z2, out float rx, out float ry, out float rz, float modifyDist)
        {
            if (isLineOfSightCalcEnabled() && !Global.DisableMgr.IsDisabledFor(DisableType.VMAP, mapId, null, DisableFlags.VmapLOS))
            {
                var instanceTree = iInstanceMapTrees.LookupByKey(mapId);
                if (instanceTree != null)
                {
                    Vector3 resultPos;
                    Vector3 pos1 = convertPositionToInternalRep(x1, y1, z1);
                    Vector3 pos2 = convertPositionToInternalRep(x2, y2, z2);
                    bool result = instanceTree.getObjectHitPos(pos1, pos2, out resultPos, modifyDist);
                    resultPos = convertPositionToInternalRep(resultPos.X, resultPos.Y, resultPos.Z);
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

        public bool isInLineOfSight(uint mapId, float x1, float y1, float z1, float x2, float y2, float z2)
        {
            if (!isLineOfSightCalcEnabled() || Global.DisableMgr.IsDisabledFor(DisableType.VMAP, mapId, null, DisableFlags.VmapLOS))
                return true;

            var instanceTree = iInstanceMapTrees.LookupByKey(mapId);
            if (instanceTree != null)
            {
                Vector3 pos1 = convertPositionToInternalRep(x1, y1, z1);
                Vector3 pos2 = convertPositionToInternalRep(x2, y2, z2);
                if (pos1 != pos2)
                {
                    return instanceTree.isInLineOfSight(pos1, pos2);
                }
            }

            return true;
        }

        public void unloadMap(uint mapId)
        {
            var instanceTree = iInstanceMapTrees.LookupByKey(mapId);
            if (instanceTree != null)
            {
                instanceTree.UnloadMap(this);
                if (instanceTree.numLoadedTiles() == 0)
                {
                    iInstanceMapTrees.Remove(mapId);
                }
            }
        }

        public void unloadMap(uint mapId, uint x, uint y)
        {
            var instanceTree = iInstanceMapTrees.LookupByKey(mapId);
            if (instanceTree != null)
            {
                instanceTree.UnloadMapTile(x, y, this);
                if (instanceTree.numLoadedTiles() == 0)
                {
                    iInstanceMapTrees.Remove(mapId);
                }
            }
        }

        Dictionary<string, ManagedModel> iLoadedModelFiles = new Dictionary<string, ManagedModel>();
        Dictionary<uint, StaticMapTree> iInstanceMapTrees = new Dictionary<uint, StaticMapTree>();
        bool _enableLineOfSightCalc;
        bool _enableHeightCalc;
    }

    public class ManagedModel
    {
        public ManagedModel()
        {
            iModel = null;
            iRefCount = 0;
        }

        public void setModel(WorldModel model) { iModel = model; }
        public WorldModel getModel() { return iModel; }
        public void incRefCount() { ++iRefCount; }
        public int decRefCount() { return --iRefCount; }

        WorldModel iModel;
        int iRefCount;
    }
}
