// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Cryptography;
using Framework.Database;
using Game.Collision;
using Game.MMaps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using static Detour;

namespace Game.Maps
{
    public class DynamicTileBuilder(Map map, dtNavMesh navMesh) : TileBuilder(Global.WorldMgr.GetDataPath(), Global.WorldMgr.GetDataPath(), null, null, false, false, false, null)
    {
        Map m_map = map;
        public dtNavMesh m_navMesh = navMesh;

        List<TileId> m_tilesToRebuild = [];

        TimeTracker m_rebuildCheckTimer = new TimeTracker(TimeSpan.FromSeconds(1));

        AsyncCallbackProcessor<TileBuildRequest> m_tiles = new();

        public void AddTile(uint terrainMapId, uint tileX, uint tileY)
        {
            TileId id = new TileId() { TerrainMapId = terrainMapId, X = tileX, Y = tileY };
            if (!m_tilesToRebuild.Contains(id))
                m_tilesToRebuild.Add(id);
        }

        public void Update(TimeSpan diff)
        {
            m_rebuildCheckTimer.Update(diff);
            if (m_rebuildCheckTimer.Passed())
            {
                foreach (TileId tileId in m_tilesToRebuild)
                    m_tiles.AddCallback(new TileBuildRequest() { Id = tileId, Result = BuildTile(tileId.TerrainMapId, tileId.X, tileId.Y), NavMesh = m_navMesh });

                m_tilesToRebuild.Clear();
                m_rebuildCheckTimer.Reset(TimeSpan.FromSeconds(1));
            }

            m_tiles.ProcessReadyCallbacks();
        }

        struct GameObjectModelWorkData(WorldModel worldModel, Vector3 position, float scale, Quaternion rotation, GameObjectModel gameObject)
        {
            public WorldModel WorldModel = worldModel;
            public Vector3 Position = position;
            public Quaternion Rotation = rotation;
            public float Scale = scale;
            public GameObjectModel GameObject = gameObject;
        }

        AsyncTileResult BuildTile(uint terrainMapId, uint tileX, uint tileY)
        {
            List<GameObjectModelWorkData> modelSpawns = [];
            foreach (GameObjectModel gameObjectModel in m_map.GetGameObjectModelsInGrid(tileX, tileY))
            {
                if (!gameObjectModel.IsMapObject() || !gameObjectModel.IsIncludedInNavMesh())
                    continue;

                WorldModel worldModel = gameObjectModel.GetWorldModel();
                if (worldModel == null)
                    continue;

                modelSpawns.Add(new GameObjectModelWorkData(worldModel, gameObjectModel.GetPosition(), gameObjectModel.GetScale(), gameObjectModel.GetRotation(), gameObjectModel));
            }

            TileCacheKey cacheKey = new() { TerrainMapId = terrainMapId, X = tileX, Y = tileY, CachedHash = 0, Objects = new List<TileCacheKeyObject>(modelSpawns.Count) };
            for (var i = 0; i < modelSpawns.Count; ++i)
            {
                GameObjectModel gameObjectModel = modelSpawns[i].GameObject;
                TileCacheKeyObject obj = cacheKey.Objects[i];
                obj.DisplayId = gameObjectModel.GetDisplayId();
                obj.Scale = (short)(gameObjectModel.GetScale() * 1024.0f);
                var pos = gameObjectModel.GetPosition();
                obj.Position = [(short)pos.X, (short)pos.Y, (short)pos.Z];
                obj.Rotation = gameObjectModel.GetPackedRotation();
            }

            // Ensure spawn order is stable after adding/removing gameobjects from the map for hash calculation
            cacheKey.Objects.Sort();

            cacheKey.CachedHash = TileCacheKey.Compute(cacheKey);

            TileCache tileCache = TileCache.Instance();
            lock (tileCache.TilesMutex)
            {

                TileCache.Tile tile = new();
                var isNew = tileCache.Tiles.TryAdd(cacheKey, tile);
                if (isNew)
                    tile = tileCache.Tiles[cacheKey];

                tile.LastAccessed = GameTime.Now();
                if (!isNew)
                    return tile.Data;

                tile.Data = new AsyncTileResult();
                var result = tile.Data;
                var hash = cacheKey.CachedHash;
                var selfRef = this;
                tileCache.StartTask(() =>
                {
                    //var isReadyGuard = make_unique_ptr_with_deleter<SetAsyncCallbackReady>(result);

                    DynamicTileBuilder self = selfRef;
                    if (self == null)
                        return;

                    // get navmesh params
                    dtNavMeshParams meshParams;
                    List<OffMeshData> offMeshConnections = new();
                    if (Global.MMapMgr.parseNavMeshParamsFile(Global.WorldMgr.GetDataPath(), terrainMapId, out meshParams, offMeshConnections) != MMapLoadResult.Success)
                        return;

                    VMapManager vmapManager = CreateVMapManager(terrainMapId);

                    MeshData meshData = new();

                    // get heightmap data
                    self.m_terrainBuilder.LoadMap(terrainMapId, tileX, tileY, meshData, vmapManager);

                    // get model data
                    self.m_terrainBuilder.LoadVMap(terrainMapId, tileX, tileY, meshData, vmapManager);

                    foreach (GameObjectModelWorkData gameObjectModel in modelSpawns)
                    {
                        Vector3 position = gameObjectModel.Position;
                        position.X = -position.X;
                        position.Y = -position.Y;

                        Matrix4x4 invRotation = (new Quaternion(0, 0, 1, 0) * gameObjectModel.Rotation).ToRotationMatrix().Inverse();

                        self.m_terrainBuilder.LoadVMapModel(gameObjectModel.WorldModel, position, invRotation, gameObjectModel.Scale, meshData, vmapManager);
                    }

                    // if there is no data, give up now
                    if (meshData.solidVerts.Empty() && meshData.liquidVerts.Empty())
                        return;

                    // remove unused vertices
                    TerrainBuilder.cleanVertices(meshData.solidVerts, meshData.solidTris);
                    TerrainBuilder.cleanVertices(meshData.liquidVerts, meshData.liquidTris);

                    // gather all mesh data for final data check, and bounds calculation
                    List<float> allVerts = [.. meshData.liquidVerts, .. meshData.solidVerts];

                    // get bounds of current tile
                    float[] bmin = new float[3];
                    float[] bmax = new float[3];
                    getTileBounds(tileX, tileY, allVerts.ToArray(), allVerts.Count / 3, bmin, bmax);

                    self.m_terrainBuilder.loadOffMeshConnections(terrainMapId, tileX, tileY, meshData, offMeshConnections);

                    // build navmesh tile
                    string debugSuffix = $"_{hash:016X}";

                    result.Result = self.buildMoveMapTile(terrainMapId, tileX, tileY, meshData, bmin, bmax, meshParams);
                    if (self.m_debugOutput && result.Result.data != null)
                        self.saveMoveMapTileToFile(terrainMapId, tileX, tileY, null, result.Result, debugSuffix);
                });

                return tile.Data;
            }
        }
    }

    public struct TileId
    {
        public uint TerrainMapId;
        public uint X;
        public uint Y;
    }
    public class AsyncTileResult
    {
        public TileBuilder.TileResult Result;
        public volatile bool IsReady;
    }

    class TileCacheKeyObject
    {
        public uint DisplayId;
        public short Scale;
        public short[] Position = new short[3];
        public long Rotation;
    }

    class TileCacheKey
    {
        public uint TerrainMapId;
        public uint X;
        public uint Y;
        public ulong CachedHash; // computing the hash is expensive - store it
        public List<TileCacheKeyObject> Objects = [];
        
        public static ulong Compute(TileCacheKey key)
        {            
            HashFNV1a_64 hash = new();
            hash.ComputeHash(key.TerrainMapId);
            hash.ComputeHash(key.X);
            hash.ComputeHash(key.Y);
            foreach (TileCacheKeyObject obj in key.Objects)
            {
                hash.ComputeHash(obj.DisplayId);
                hash.ComputeHash(obj.Scale);
                hash.ComputeHash(obj.Position);
                hash.ComputeHash(obj.Rotation);
            }
            return hash.Value;
        }
    }


    class TileCache
    {
        public Mutex TilesMutex = new();
        public Dictionary<TileCacheKey, Tile> Tiles = new();

        Timer _cacheCleanupTimer;
        static TileCache tc = new();
        public static TileCache Instance() { return tc; }

        public TileCache()
        {
            _cacheCleanupTimer = new(OnCacheCleanupTimerTick, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));

            // init timer
            OnCacheCleanupTimerTick();
        }


        ~TileCache()
        {
            _cacheCleanupTimer.Dispose();
        }

        public Task<bool> StartTask(Action task)
        {

            return Task<bool>.Run(task) as Task<bool>;
        }

        void OnCacheCleanupTimerTick(object error = null)
        {
            if (error != null)
                return;

            DateTime now = GameTime.Now();
            RemoveOldCacheEntries(now - TimeSpan.FromMinutes(30));

            OnCacheCleanupTimerTick();
        }

        void RemoveOldCacheEntries(DateTime oldestPreservedEntryTimestamp)
        {
            lock (TilesMutex)
                Tiles.Remove(Tiles.First(kv => kv.Value.LastAccessed < oldestPreservedEntryTimestamp).Key);
        }

        public class Tile
        {
            public AsyncTileResult Data;
            public DateTime LastAccessed;
        }
    }


    public struct TileBuildRequest : IAsyncCallback
    {
        public TileId Id;
        public AsyncTileResult Result;
        public dtNavMesh NavMesh;

        public bool InvokeAsyncCallbackIfReady()
        {
            if (Result == null)
                return true;    // expired, mark as complete and do nothing

            if (!Result.IsReady)
                return false;

            TileBuilder.TileResult tileResult = Result.Result;
            if (tileResult.data != null)
            {
                dtMeshHeader header = tileResult.data.header;
                ulong tileRef = NavMesh.getTileRefAt(header.x, header.y, 0);
                if (tileRef != 0)
                {
                    Log.outInfo(LogFilter.Maps, $"[Map {Id.TerrainMapId:04}] [{Id.Y:02},{Id.X:02}]: Swapping new tile");

                    NavMesh.removeTile(tileRef, out _);

                    ulong result = 0;
                    NavMesh.addTile(tileResult.data, 1, tileRef, ref result);
                }
            }

            return true;
        }
    }
}
