// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Collision;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Game.MMaps
{
    public class TerrainBuilder(string inputDirectory, bool skipLiquid)
    {
        const int V9_SIZE = 129;
        const int V9_SIZE_SQ = V9_SIZE * V9_SIZE;
        const int V8_SIZE = 128;
        const int V8_SIZE_SQ = V8_SIZE * V8_SIZE;
        const float GRID_SIZE = 533.3333f;
        const float GRID_PART_SIZE = GRID_SIZE / V8_SIZE;

        // see contrib/extractor/system.cpp, CONF_use_minHeight
        const float INVALID_MAP_LIQ_HEIGHT = -2000.0f;
        const float INVALID_MAP_LIQ_HEIGHT_MAX = 5000.0f;

        void GetLoopVars(Spot portion, ref int loopStart, ref int loopEnd, ref int loopInc)
        {
            switch (portion)
            {
                case Spot.Entire:
                    loopStart = 0;
                    loopEnd = V8_SIZE_SQ;
                    loopInc = 1;
                    break;
                case Spot.Top:
                    loopStart = 0;
                    loopEnd = V8_SIZE;
                    loopInc = 1;
                    break;
                case Spot.Left:
                    loopStart = 0;
                    loopEnd = V8_SIZE_SQ - V8_SIZE + 1;
                    loopInc = V8_SIZE;
                    break;
                case Spot.Right:
                    loopStart = V8_SIZE - 1;
                    loopEnd = V8_SIZE_SQ;
                    loopInc = V8_SIZE;
                    break;
                case Spot.Bottom:
                    loopStart = V8_SIZE_SQ - V8_SIZE;
                    loopEnd = V8_SIZE_SQ;
                    loopInc = 1;
                    break;
            }
        }

        /**************************************************************************/
        public void LoadMap(uint mapID, uint tileX, uint tileY, MeshData meshData, VMapManager vmapManager)
        {
            if (LoadMap(mapID, tileX, tileY, meshData, vmapManager, Spot.Entire))
            {
                LoadMap(mapID, tileX + 1, tileY, meshData, vmapManager, Spot.Left);
                LoadMap(mapID, tileX - 1, tileY, meshData, vmapManager, Spot.Right);
                LoadMap(mapID, tileX, tileY + 1, meshData, vmapManager, Spot.Top);
                LoadMap(mapID, tileX, tileY - 1, meshData, vmapManager, Spot.Bottom);
            }
        }

        /**************************************************************************/
        bool LoadMap(uint mapID, uint tileX, uint tileY, MeshData meshData, VMapManager vmapManager, Spot portion)
        {
            string mapFileName = $"{inputDirectory}/maps/{mapID:D4}_{tileY:D2}_{tileX:D2}.map";
            if (!File.Exists(mapFileName))
            {
                int parentMapId = vmapManager.GetParentMapId(mapID);
                while (File.Exists(mapFileName) && parentMapId != -1)
                {
                    mapFileName = $"{inputDirectory}/maps/{parentMapId:04}_{tileX:02}_{tileY:02}.map";
                    parentMapId = vmapManager.GetParentMapId((uint)parentMapId);
                }
            }

            if (!File.Exists(mapFileName))
                return false;

            using (BinaryReader reader = new(File.Open(mapFileName, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                map_fileheader fheader = reader.Read<map_fileheader>();
                if (fheader.versionMagic != MapConst.MapVersionMagic)
                {
                    Console.WriteLine($"{mapFileName} is the wrong version, please extract new .map files");
                    return false;
                }

                reader.BaseStream.Seek(fheader.heightMapOffset, SeekOrigin.Begin);

                bool haveTerrain = false;
                bool haveLiquid = false;

                map_heightHeader hheader = reader.Read<map_heightHeader>();
                if (hheader.heightMagic == 1413957709)
                {
                    haveTerrain = !Convert.ToBoolean(hheader.flags & (uint)HeightHeaderFlags.NoHeight);
                    haveLiquid = fheader.liquidMapOffset != 0 && !skipLiquid;
                }

                // no data in this map file
                if (!haveTerrain && !haveLiquid)
                    return false;

                // data used later
                byte[][][] holes = new byte[16][][];
                for (var i = 0; i < 16; ++i)
                {
                    holes[i] = new byte[16][];
                    for (var x = 0; x < 16; ++x)
                        holes[i][x] = new byte[8];
                }

                ushort[][] liquid_entry = new ushort[16][];
                LiquidHeaderTypeFlags[][] liquid_flags = new LiquidHeaderTypeFlags[16][];
                for (var i = 0; i < 16; ++i)
                {
                    liquid_entry[i] = new ushort[16];
                    liquid_flags[i] = new LiquidHeaderTypeFlags[16];
                }

                List<int> ltriangles = new();
                List<int> ttriangles = new();

                // terrain data
                if (haveTerrain)
                {
                    float heightMultiplier;
                    float[] V9 = new float[V9_SIZE_SQ];
                    float[] V8 = new float[V8_SIZE_SQ];
                    int expected = V9_SIZE_SQ + V8_SIZE_SQ;

                    if (Convert.ToBoolean(hheader.flags & (uint)HeightHeaderFlags.HeightAsInt8))
                    {
                        byte[] v9 = new byte[V9_SIZE_SQ];
                        byte[] v8 = new byte[V8_SIZE_SQ];
                        int count = 0;
                        count += reader.Read(v9, 0, V9_SIZE_SQ);
                        count += reader.Read(v8, 0, V8_SIZE_SQ);
                        if (count != expected)
                            Console.WriteLine($"TerrainBuilder.loadMap: Failed to read some data expected {expected}, read {count}");

                        heightMultiplier = (hheader.gridMaxHeight - hheader.gridHeight) / 255;

                        for (int i = 0; i < V9_SIZE_SQ; ++i)
                            V9[i] = (float)v9[i] * heightMultiplier + hheader.gridHeight;

                        for (int i = 0; i < V8_SIZE_SQ; ++i)
                            V8[i] = (float)v8[i] * heightMultiplier + hheader.gridHeight;
                    }
                    else if (Convert.ToBoolean(hheader.flags & (uint)HeightHeaderFlags.HeightAsInt16))
                    {
                        ushort[] v9 = new ushort[V9_SIZE_SQ];
                        ushort[] v8 = new ushort[V8_SIZE_SQ];

                        for (var i = 0; i < V9_SIZE_SQ; ++i)
                            v9[i] = reader.ReadUInt16();

                        for (var i = 0; i < V8_SIZE_SQ; ++i)
                            v8[i] = reader.ReadUInt16();

                        heightMultiplier = (hheader.gridMaxHeight - hheader.gridHeight) / 65535;

                        for (int i = 0; i < V9_SIZE_SQ; ++i)
                            V9[i] = (float)v9[i] * heightMultiplier + hheader.gridHeight;

                        for (int i = 0; i < V8_SIZE_SQ; ++i)
                            V8[i] = (float)v8[i] * heightMultiplier + hheader.gridHeight;
                    }
                    else
                    {
                        for (var i = 0; i < V9_SIZE_SQ; ++i)
                            V9[i] = reader.ReadSingle();

                        for (var i = 0; i < V8_SIZE_SQ; ++i)
                            V8[i] = reader.ReadSingle();
                    }

                    // hole data
                    if (fheader.holesSize != 0)
                    {
                        reader.BaseStream.Seek(fheader.holesOffset, SeekOrigin.Begin);

                        int readCount = 0;
                        for (var i = 0; i < 16; ++i)
                        {
                            for (var x = 0; x < 16; ++x)
                            {
                                for (var c = 0; c < 8; ++c)
                                {
                                    if (readCount == fheader.holesSize)
                                        break;

                                    holes[i][x][c] = reader.ReadByte();
                                }
                            }
                        }
                    }

                    int count1 = meshData.solidVerts.Count / 3;
                    float xoffset = ((float)tileX - 32) * MapConst.SizeofGrids;
                    float yoffset = ((float)tileY - 32) * MapConst.SizeofGrids;

                    float[] coord = new float[3];

                    for (int i = 0; i < V9_SIZE_SQ; ++i)
                    {
                        GetHeightCoord(i, GridNumber.V9, xoffset, yoffset, ref coord, ref V9);
                        meshData.solidVerts.Add(coord[0]);
                        meshData.solidVerts.Add(coord[2]);
                        meshData.solidVerts.Add(coord[1]);
                    }

                    for (int i = 0; i < V8_SIZE_SQ; ++i)
                    {
                        GetHeightCoord(i, GridNumber.V8, xoffset, yoffset, ref coord, ref V8);
                        meshData.solidVerts.Add(coord[0]);
                        meshData.solidVerts.Add(coord[2]);
                        meshData.solidVerts.Add(coord[1]);
                    }

                    int[] indices = { 0, 0, 0 };
                    int loopStart = 0, loopEnd = 0, loopInc = 0;
                    GetLoopVars(portion, ref loopStart, ref loopEnd, ref loopInc);
                    for (int i = loopStart; i < loopEnd; i += loopInc)
                    {
                        for (Spot j = Spot.Top; j <= Spot.Bottom; j += 1)
                        {
                            GetHeightTriangle(i, j, indices);
                            ttriangles.Add(indices[2] + count1);
                            ttriangles.Add(indices[1] + count1);
                            ttriangles.Add(indices[0] + count1);
                        }
                    }
                }

                // liquid data
                if (haveLiquid)
                {
                    reader.BaseStream.Seek(fheader.liquidMapOffset, SeekOrigin.Begin);
                    MapLiquidHeader lheader = reader.Read<MapLiquidHeader>();

                    float[] liquid_map = null;
                    if (!lheader.flags.HasFlag(LiquidHeaderFlags.NoType))
                    {
                        for (var i = 0; i < 16; ++i)
                            for (var x = 0; x < 16; ++x)
                                liquid_entry[i][x] = reader.ReadUInt16();

                        for (var i = 0; i < 16; ++i)
                            for (var x = 0; x < 16; ++x)
                                liquid_flags[i][x] = (LiquidHeaderTypeFlags)reader.ReadByte();
                    }
                    else
                    {
                        for (var i = 0; i < 16; ++i)
                        {
                            for (var x = 0; x < 16; ++x)
                            {
                                liquid_entry[i][x] = lheader.liquidType;
                                liquid_flags[i][x] = (LiquidHeaderTypeFlags)lheader.liquidFlags;
                            }
                        }
                    }

                    if (!lheader.flags.HasFlag(LiquidHeaderFlags.NoHeight))
                    {
                        int toRead = lheader.width * lheader.height;
                        liquid_map = new float[toRead];
                        for (var i = 0; i < toRead; ++i)
                            liquid_map[i] = reader.ReadSingle();
                    }

                    int count = meshData.liquidVerts.Count / 3;
                    float xoffset = (tileX - 32) * MapConst.SizeofGrids;
                    float yoffset = (tileY - 32) * MapConst.SizeofGrids;

                    float[] coord = new float[3];
                    int row, col;

                    // generate coordinates
                    if (!lheader.flags.HasFlag(LiquidHeaderFlags.NoHeight))
                    {
                        int j = 0;
                        for (int i = 0; i < V9_SIZE_SQ; ++i)
                        {
                            row = i / V9_SIZE;
                            col = i % V9_SIZE;

                            if (row < lheader.offsetY || row >= lheader.offsetY + lheader.height ||
                                col < lheader.offsetX || col >= lheader.offsetX + lheader.width)
                            {
                                // dummy vert using invalid height
                                meshData.liquidVerts.Add((xoffset + col * GRID_PART_SIZE) * -1);
                                meshData.liquidVerts.Add(INVALID_MAP_LIQ_HEIGHT);
                                meshData.liquidVerts.Add((yoffset + row * GRID_PART_SIZE) * -1);
                                continue;
                            }

                            GetLiquidCoord(i, j, xoffset, yoffset, ref coord, ref liquid_map);
                            meshData.liquidVerts.Add(coord[0]);
                            meshData.liquidVerts.Add(coord[2]);
                            meshData.liquidVerts.Add(coord[1]);
                            j++;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < V9_SIZE_SQ; ++i)
                        {
                            row = i / V9_SIZE;
                            col = i % V9_SIZE;
                            meshData.liquidVerts.Add((xoffset + col * GRID_PART_SIZE) * -1);
                            meshData.liquidVerts.Add(lheader.liquidLevel);
                            meshData.liquidVerts.Add((yoffset + row * GRID_PART_SIZE) * -1);
                        }
                    }


                    int[] indices = { 0, 0, 0 };
                    int loopStart = 0, loopEnd = 0, loopInc = 0, triInc = Spot.Bottom - Spot.Top;
                    GetLoopVars(portion, ref loopStart, ref loopEnd, ref loopInc);

                    // generate triangles
                    for (int i = loopStart; i < loopEnd; i += loopInc)
                    {
                        for (Spot j = Spot.Top; j <= Spot.Bottom; j += triInc)
                        {
                            GetHeightTriangle(i, j, indices, true);
                            ltriangles.Add(indices[2] + count);
                            ltriangles.Add(indices[1] + count);
                            ltriangles.Add(indices[0] + count);
                        }
                    }
                }

                // now that we have gathered the data, we can figure out which parts to keep:
                // liquid above ground, ground above liquid
                int loopStart1 = 0, loopEnd1 = 0, loopInc1 = 0, tTriCount = 4;
                bool useTerrain, useLiquid;

                float[] lverts = meshData.liquidVerts.ToArray();
                int[] ltris = ltriangles.ToArray();
                int currentLtrisIndex = 0;

                float[] tverts = meshData.solidVerts.ToArray();
                int[] ttris = ttriangles.ToArray();
                int currentTtrisIndex = 0;

                if ((ltriangles.Count + ttriangles.Count) == 0)
                    return false;

                // make a copy of liquid vertices
                // used to pad right-bottom frame due to lost vertex data at extraction
                float[] lverts_copy = null;
                if (meshData.liquidVerts.Count != 0)
                {
                    lverts_copy = new float[meshData.liquidVerts.Count];
                    Array.Copy(lverts, lverts_copy, meshData.liquidVerts.Count);
                }

                GetLoopVars(portion, ref loopStart1, ref loopEnd1, ref loopInc1);
                for (int i = loopStart1; i < loopEnd1; i += loopInc1)
                {
                    for (int j = 0; j < 2; ++j)
                    {
                        // default is true, will change to false if needed
                        useTerrain = true;
                        useLiquid = true;
                        byte navLiquidType = 0;

                        // if there is no liquid, don't use liquid
                        if (meshData.liquidVerts.Count == 0 || ltriangles.Count == 0)
                            useLiquid = false;
                        else
                        {
                            LiquidHeaderTypeFlags liquidType = GetLiquidType(i, liquid_flags);
                            if (liquidType.HasFlag(LiquidHeaderTypeFlags.DarkWater))
                            {
                                // players should not be here, so logically neither should creatures
                                useTerrain = false;
                                useLiquid = false;
                            }
                            else if (liquidType.HasAnyFlag(LiquidHeaderTypeFlags.Water | LiquidHeaderTypeFlags.Ocean))
                                navLiquidType = (byte)NavArea.Water;
                            else if (liquidType.HasAnyFlag(LiquidHeaderTypeFlags.Magma | LiquidHeaderTypeFlags.Slime))
                                navLiquidType = (byte)NavArea.MagmaSlime;
                            else
                                useLiquid = false;
                        }

                        // if there is no terrain, don't use terrain
                        if (ttriangles.Count == 0)
                            useTerrain = false;

                        // while extracting ADT data we are losing right-bottom vertices
                        // this code adds fair approximation of lost data
                        if (useLiquid)
                        {
                            float quadHeight = 0;
                            uint validCount = 0;
                            for (uint idx = 0; idx < 3; idx++)
                            {
                                float h = lverts_copy[ltris[currentLtrisIndex + idx] * 3 + 1];
                                if (h != INVALID_MAP_LIQ_HEIGHT && h < INVALID_MAP_LIQ_HEIGHT_MAX)
                                {
                                    quadHeight += h;
                                    validCount++;
                                }
                            }

                            // update vertex height data
                            if (validCount > 0 && validCount < 3)
                            {
                                quadHeight /= validCount;
                                for (uint idx = 0; idx < 3; idx++)
                                {
                                    float h = lverts[ltris[currentLtrisIndex + idx] * 3 + 1];
                                    if (h == INVALID_MAP_LIQ_HEIGHT || h > INVALID_MAP_LIQ_HEIGHT_MAX)
                                        lverts[ltris[currentLtrisIndex + idx] * 3 + 1] = quadHeight;
                                }
                            }

                            // no valid vertexes - don't use this poly at all
                            if (validCount == 0)
                                useLiquid = false;
                        }

                        // if there is a hole here, don't use the terrain
                        if (useTerrain && fheader.holesSize != 0)
                            useTerrain = !IsHole(i, holes);

                        // we use only one terrain kind per quad - pick higher one
                        if (useTerrain && useLiquid)
                        {
                            float minLLevel = INVALID_MAP_LIQ_HEIGHT_MAX;
                            float maxLLevel = INVALID_MAP_LIQ_HEIGHT;
                            for (uint x = 0; x < 3; x++)
                            {
                                float h = lverts[ltris[currentLtrisIndex + x] * 3 + 1];
                                if (minLLevel > h)
                                    minLLevel = h;

                                if (maxLLevel < h)
                                    maxLLevel = h;
                            }

                            float maxTLevel = INVALID_MAP_LIQ_HEIGHT;
                            float minTLevel = INVALID_MAP_LIQ_HEIGHT_MAX;
                            for (uint x = 0; x < 6; x++)
                            {
                                float h = tverts[ttris[currentTtrisIndex + x] * 3 + 1];
                                if (maxTLevel < h)
                                    maxTLevel = h;

                                if (minTLevel > h)
                                    minTLevel = h;
                            }

                            // terrain under the liquid?
                            if (minLLevel > maxTLevel)
                                useTerrain = false;

                            //liquid under the terrain?
                            if (minTLevel > maxLLevel)
                                useLiquid = false;
                        }

                        // store the result
                        if (useLiquid)
                        {
                            meshData.liquidType.Add(navLiquidType);
                            for (int k = 0; k < 3; ++k)
                                meshData.liquidTris.Add(ltris[currentLtrisIndex + k]);
                        }

                        if (useTerrain)
                            for (int k = 0; k < 3 * tTriCount / 2; ++k)
                                meshData.solidTris.Add(ttris[currentTtrisIndex + k]);

                        currentLtrisIndex += 3;
                        //ltris = ltris.Skip(3).ToArray();
                        currentTtrisIndex += 3 * tTriCount / 2;
                        //ttris = ttris.Skip(3 * tTriCount / 2).ToArray();
                    }
                }
            }
            return meshData.solidTris.Count != 0 || meshData.liquidTris.Count != 0;
        }

        /**************************************************************************/
        void GetHeightCoord(int index, GridNumber grid, float xOffset, float yOffset, ref float[] coord, ref float[] v)
        {
            // wow coords: x, y, height
            // coord is mirroed about the horizontal axes
            switch (grid)
            {
                case GridNumber.V9:
                    coord[0] = (xOffset + index % (V9_SIZE) * GRID_PART_SIZE) * -1.0f;
                    coord[1] = (yOffset + (int)(index / (V9_SIZE)) * GRID_PART_SIZE) * -1.0f;
                    coord[2] = v[index];
                    break;
                case GridNumber.V8:
                    coord[0] = (xOffset + index % (V8_SIZE) * GRID_PART_SIZE + GRID_PART_SIZE / 2.0f) * -1.0f;
                    coord[1] = (yOffset + (int)(index / (V8_SIZE)) * GRID_PART_SIZE + GRID_PART_SIZE / 2.0f) * -1.0f;
                    coord[2] = v[index];
                    break;
            }
        }

        /**************************************************************************/
        void GetHeightTriangle(int square, Spot triangle, int[] indices, bool liquid = false)
        {
            int rowOffset = square / V8_SIZE;
            if (!liquid)
                switch (triangle)
                {
                    case Spot.Top:
                        indices[0] = square + rowOffset;                  //           0-----1 .... 128
                        indices[1] = square + 1 + rowOffset;                //           |\ T /|
                        indices[2] = V9_SIZE_SQ + square;               //           | \ / |
                        break;                                          //           |L 0 R| .. 127
                    case Spot.Left:                                          //           | / \ |
                        indices[0] = square + rowOffset;                  //           |/ B \|
                        indices[1] = V9_SIZE_SQ + square;               //          129---130 ... 386
                        indices[2] = square + V9_SIZE + rowOffset;          //           |\   /|
                        break;                                          //           | \ / |
                    case Spot.Right:                                         //           | 128 | .. 255
                        indices[0] = square + 1 + rowOffset;                //           | / \ |
                        indices[1] = square + V9_SIZE + 1 + rowOffset;        //           |/   \|
                        indices[2] = V9_SIZE_SQ + square;               //          258---259 ... 515
                        break;
                    case Spot.Bottom:
                        indices[0] = V9_SIZE_SQ + square;
                        indices[1] = square + V9_SIZE + 1 + rowOffset;
                        indices[2] = square + V9_SIZE + rowOffset;
                        break;
                    default: break;
                }
            else
                switch (triangle)
                {                                                           //           0-----1 .... 128
                    case Spot.Top:                                               //           |\    |
                        indices[0] = square + rowOffset;                      //           | \ T |
                        indices[1] = square + 1 + rowOffset;                    //           |  \  |
                        indices[2] = square + V9_SIZE + 1 + rowOffset;            //           | B \ |
                        break;                                              //           |    \|
                    case Spot.Bottom:                                            //          129---130 ... 386
                        indices[0] = square + rowOffset;                      //           |\    |
                        indices[1] = square + V9_SIZE + 1 + rowOffset;            //           | \   |
                        indices[2] = square + V9_SIZE + rowOffset;              //           |  \  |
                        break;                                              //           |   \ |
                    default: break;                                         //           |    \|
                }                                                           //          258---259 ... 515

        }

        /**************************************************************************/
        void GetLiquidCoord(int index, int index2, float xOffset, float yOffset, ref float[] coord, ref float[] v)
        {
            // wow coords: x, y, height
            // coord is mirroed about the horizontal axes
            coord[0] = (xOffset + index % V9_SIZE * GRID_PART_SIZE) * -1.0f;
            coord[1] = (yOffset + (int)(index / V9_SIZE) * GRID_PART_SIZE) * -1.0f;
            coord[2] = v[index2];
        }

        /**************************************************************************/
        bool IsHole(int square, byte[][][] holes)
        {
            int row = square / 128;
            int col = square % 128;
            int cellRow = row / 8;     // 8 squares per cell
            int cellCol = col / 8;
            int holeRow = row % 8;
            int holeCol = (square - (row * 128 + cellCol * 8));

            return (holes[cellRow][cellCol][holeRow] & (1 << holeCol)) != 0;
        }

        /**************************************************************************/
        LiquidHeaderTypeFlags GetLiquidType(int square, LiquidHeaderTypeFlags[][] liquid_type)
        {
            int row = square / 128;
            int col = square % 128;
            int cellRow = row / 8;     // 8 squares per cell
            int cellCol = col / 8;

            return liquid_type[cellRow][cellCol];
        }

        public bool LoadVMap(uint mapID, uint tileX, uint tileY, MeshData meshData, VMapManager vmapManager)
        {
            LoadResult result = vmapManager.LoadMap($"{inputDirectory} / vmaps", mapID, tileX, tileY);
            if (result != LoadResult.Success)
                return false;

            Span<ModelInstance> models = vmapManager.GetModelsOnMap(mapID);
            if (models.IsEmpty)
            {
                vmapManager.UnloadMap(mapID, tileX, tileY);
                return false;
            }

            bool retval = false;

            foreach (ModelInstance instance in models)
            {
                // model instances exist in tree even though there are instances of that model in this tile
                WorldModel worldModel = instance.GetWorldModel();
                if (worldModel == null)
                    continue;

                // now we have a model to add to the meshdata
                retval = true;

                Vector3 position = instance.iPos;
                position.X -= 32 * GRID_SIZE;
                position.Y -= 32 * GRID_SIZE;
                LoadVMapModel(worldModel, position, instance.GetInvRot(), instance.iScale, meshData, vmapManager);
            }

            vmapManager.UnloadMap(mapID, tileX, tileY);

            return retval;
        }

        public void LoadVMapModel(WorldModel worldModel, Vector3 position, Matrix4x4 rotation, float scale, MeshData meshData, VMapManager vmapManager)
        {
            List<GroupModel> groupModels = worldModel.GetGroupModels();

            // all M2s need to have triangle indices reversed
            bool isM2 = worldModel.IsM2();

            // transform data
            foreach (var it in groupModels)
            {
                // first handle collision mesh
                int offset = meshData.solidVerts.Count / 3;
                transformVertices(it.GetVertices(), meshData.solidVerts, scale, rotation, position);
                copyIndices(it.GetTriangles(), meshData.solidTris, offset, isM2);

                // now handle liquid data
                WmoLiquid liquid = it.GetLiquid();
                if (liquid != null && liquid.GetFlagsStorage() != null)
                {
                    liquid.GetPosInfo(out uint tilesX, out uint tilesY, out Vector3 corner);
                    uint vertsX = tilesX + 1;
                    uint vertsY = tilesY + 1;
                    byte[] flags = liquid.GetFlagsStorage();
                    float[] data = liquid.GetHeightStorage();
                    NavArea type = NavArea.Empty;

                    // convert liquid type to NavTerrain
                    LiquidHeaderTypeFlags liquidFlags = (LiquidHeaderTypeFlags)Global.DB2Mgr.GetLiquidFlags(liquid.GetLiquidType());
                    if (liquidFlags.HasFlag(LiquidHeaderTypeFlags.Water | LiquidHeaderTypeFlags.Ocean))
                        type = NavArea.Water;
                    else if (liquidFlags.HasFlag(LiquidHeaderTypeFlags.Magma | LiquidHeaderTypeFlags.Slime))
                        type = NavArea.MagmaSlime;

                    // indexing is weird...
                    // after a lot of trial and error, this is what works:
                    // vertex = y*vertsX+x
                    // tile   = x*tilesY+y
                    // flag   = y*tilesY+x

                    int liqOffset = meshData.liquidVerts.Count / 3;
                    meshData.liquidVerts.Resize((uint)(meshData.liquidVerts.Count() + vertsX * vertsY * 3));
                    float[] liquidVerts = meshData.liquidVerts.ToArray();
                    for (uint x = 0; x < vertsX; ++x)
                    {
                        for (uint y = 0; y < vertsY; ++y)
                        {
                            Vector3 vert = new Vector3(corner.X + x * GRID_PART_SIZE, corner.Y + y * GRID_PART_SIZE, data[y * vertsX + x]);
                            vert = rotation.Multiply(vert) * scale + position;
                            vert.X *= -1.0f;
                            vert.Y *= -1.0f;
                            liquidVerts[(liqOffset + x * vertsY + y) * 3 + 0] = vert.Y;
                            liquidVerts[(liqOffset + x * vertsY + y) * 3 + 1] = vert.Z;
                            liquidVerts[(liqOffset + x * vertsY + y) * 3 + 2] = vert.X;
                        }
                    }

                    int liquidSquares = 0;
                    for (uint x = 0; x < tilesX; ++x)
                    {
                        for (uint y = 0; y < tilesY; ++y)
                        {
                            if ((flags[x + y * tilesX] & 0x0f) != 0x0f)
                            {
                                uint square = x * tilesY + y;
                                int idx1 = (int)(square + x);
                                int idx2 = (int)(square + 1 + x);
                                int idx3 = (int)(square + tilesY + 1 + 1 + x);
                                int idx4 = (int)(square + tilesY + 1 + x);

                                int liquidTriOffset = meshData.liquidTris.Count;
                                meshData.liquidTris.Resize((uint)(liquidTriOffset + 6));

                                // top triangle
                                meshData.liquidTris[liquidTriOffset + 0] = idx2 + liqOffset;
                                meshData.liquidTris[liquidTriOffset + 1] = idx1 + liqOffset;
                                meshData.liquidTris[liquidTriOffset + 2] = idx3 + liqOffset;

                                // bottom triangle
                                meshData.liquidTris[liquidTriOffset + 3] = idx3 + liqOffset;
                                meshData.liquidTris[liquidTriOffset + 4] = idx1 + liqOffset;
                                meshData.liquidTris[liquidTriOffset + 5] = idx4 + liqOffset;

                                ++liquidSquares;
                            }
                        }
                    }

                    meshData.liquidType.Resize((uint)(meshData.liquidType.Count + liquidSquares * 2), (byte)type);
                }
            }
        }

        /**************************************************************************/
        void transformVertices(List<Vector3> source, List<float> dest, float scale, Matrix4x4 rotation, Vector3 position)
        {
            int offset = dest.Count;
            for (var i = 0; i < source.Count; ++i)
            {
                // apply tranform, then mirror along the horizontal axes
                Vector3 v = (rotation.Multiply(source[i]) * scale + position);

                v.X *= -1.0f;
                v.Y *= -1.0f;
                dest[offset + i * 3 + 0] = v.Y;
                dest[offset + i * 3 + 1] = v.Z;
                dest[offset + i * 3 + 2] = v.X;
            }
        }

        /**************************************************************************/
        void copyVertices(Vector3[] source, List<float> dest)
        {
            foreach (var it in source)
            {
                dest.Add(it.Y);
                dest.Add(it.Z);
                dest.Add(it.X);
            }
        }

        /**************************************************************************/
        void copyIndices(List<MeshTriangle> source, List<int> dest, int offset, bool flip)
        {
            if (flip)
            {
                foreach (var it in source)
                {
                    dest.Add((int)(it.idx2 + offset));
                    dest.Add((int)(it.idx1 + offset));
                    dest.Add((int)(it.idx0 + offset));
                }
            }
            else
            {
                foreach (var it in source)
                {
                    dest.Add((int)(it.idx0 + offset));
                    dest.Add((int)(it.idx1 + offset));
                    dest.Add((int)(it.idx2 + offset));
                }
            }
        }

        public static void copyIndices(List<int> source, List<int> dest, int offset)
        {
            int[] src = source.ToArray();

            for (int i = 0; i < source.Count; ++i)
            {
                var g = src[i] + offset;
                dest.Add(src[i] + offset);

            }


        }

        /**************************************************************************/
        void copyIndices(int[] source, List<int> dest, int offset)
        {
            for (int i = 0; i < source.Length; ++i)
                dest.Add(source[i] + offset);
        }

        /**************************************************************************/
        public static void cleanVertices(List<float> verts, List<int> tris)
        {
            Dictionary<int, int> vertMap = new();

            List<float> cleanVerts = new();
            int index, count = 0;
            // collect all the vertex indices from triangle
            for (int i = 0; i < tris.Count; ++i)
            {
                if (vertMap.ContainsKey(tris[i]))
                    continue;

                index = tris[i];
                vertMap.Add(tris[i], count);

                cleanVerts.Add(verts[index * 3]);
                cleanVerts.Add(verts[index * 3 + 1]);
                cleanVerts.Add(verts[index * 3 + 2]);
                count++;
            }

            verts.Clear();
            verts.AddRange(cleanVerts);
            cleanVerts.Clear();

            // update triangles to use new indices
            for (int i = 0; i < tris.Count; ++i)
            {
                if (!vertMap.ContainsKey(tris[i]))
                    continue;

                tris[i] = vertMap[tris[i]];
            }

            vertMap.Clear();
        }

        /**************************************************************************/
        public void loadOffMeshConnections(uint mapID, uint tileX, uint tileY, MeshData meshData, List<OffMeshData> offMeshConnections)
        {
            foreach (OffMeshData offMeshConnection in offMeshConnections)
            {
                if (mapID != offMeshConnection.MapId || tileX != offMeshConnection.TileX || tileY != offMeshConnection.TileY)
                    continue;

                meshData.offMeshConnections.Add(offMeshConnection.From[1]);
                meshData.offMeshConnections.Add(offMeshConnection.From[2]);
                meshData.offMeshConnections.Add(offMeshConnection.From[0]);

                meshData.offMeshConnections.Add(offMeshConnection.To[1]);
                meshData.offMeshConnections.Add(offMeshConnection.To[2]);
                meshData.offMeshConnections.Add(offMeshConnection.To[0]);

                meshData.offMeshConnectionDirs.Add((byte)(offMeshConnection.ConnectionFlags & OffMeshConnectionFlag.BiDirectional));
                meshData.offMeshConnectionRads.Add(offMeshConnection.Radius);    // agent size equivalent
                                                                                 // can be used same way as polygon flags
                meshData.offMeshConnectionsAreas.Add(offMeshConnection.AreaId);
                meshData.offMeshConnectionsFlags.Add((ushort)offMeshConnection.Flags);
            }
        }

        public bool UsesLiquids() { return !skipLiquid; }
    }

    public struct map_fileheader
    {
        public uint mapMagic;
        public uint versionMagic;
        public uint buildMagic;
        public uint areaMapOffset;
        public uint areaMapSize;
        public uint heightMapOffset;
        public uint heightMapSize;
        public uint liquidMapOffset;
        public uint liquidMapSize;
        public uint holesOffset;
        public uint holesSize;
    }

    public struct map_heightHeader
    {
        public uint heightMagic;
        public uint flags;
        public float gridHeight;
        public float gridMaxHeight;
    }

    public class MeshData
    {
        public List<float> solidVerts = new();
        public List<int> solidTris = new();

        public List<float> liquidVerts = new();
        public List<int> liquidTris = new();
        public List<byte> liquidType = new();

        // offmesh connection data
        public List<float> offMeshConnections = new();   // [p0y,p0z,p0x,p1y,p1z,p1x] - per connection
        public List<float> offMeshConnectionRads = new();
        public List<byte> offMeshConnectionDirs = new();
        public List<byte> offMeshConnectionsAreas = new();
        public List<ushort> offMeshConnectionsFlags = new();
    }
}
