// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Framework.Constants;
using Framework.GameMath;
using Game.Collision;
using Game.DataStorage;

namespace Game.Maps
{
    public class GridMap
    {
        public GridMap()
        {
            // Height level _data
            _gridHeight = MapConst.InvalidHeight;
            _gridGetHeight = GetHeightFromFlat;

            // Liquid _data
            _liquidLevel = MapConst.InvalidHeight;
        }

        public LoadResult LoadData(string filename)
        {
            // Unload old _data if exist
            UnloadData();

            // Not return error if file not found
            if (!File.Exists(filename))
                return LoadResult.FileNotFound;

            using BinaryReader reader = new(new FileStream(filename, FileMode.Open, FileAccess.Read));
            MapFileHeader header = reader.Read<MapFileHeader>();

            if (header.MapMagic != MapConst.MapMagic ||
                (header.VersionMagic != MapConst.MapVersionMagic && header.VersionMagic != MapConst.MapVersionMagic2)) // Hack for some different extractors using v2.0 header
            {
                Log.outError(LogFilter.Maps, $"Map file '{filename}' is from an incompatible map version. Please recreate using the mapextractor.");

                return LoadResult.ReadFromFileFailed;
            }

            if (header.AreaMapOffset != 0 &&
                !LoadAreaData(reader, header.AreaMapOffset))
            {
                Log.outError(LogFilter.Maps, "Error loading map area _data");

                return LoadResult.ReadFromFileFailed;
            }

            if (header.HeightMapOffset != 0 &&
                !LoadHeightData(reader, header.HeightMapOffset))
            {
                Log.outError(LogFilter.Maps, "Error loading map height _data");

                return LoadResult.ReadFromFileFailed;
            }

            if (header.LiquidMapOffset != 0 &&
                !LoadLiquidData(reader, header.LiquidMapOffset))
            {
                Log.outError(LogFilter.Maps, "Error loading map liquids _data");

                return LoadResult.ReadFromFileFailed;
            }

            if (header.HolesSize != 0 &&
                !LoadHolesData(reader, header.HolesOffset))
            {
                Log.outError(LogFilter.Maps, "Error loading map holes _data");

                return LoadResult.ReadFromFileFailed;
            }

            return LoadResult.Success;
        }

        public void UnloadData()
        {
            AreaMap = null;
            V9 = null;
            V8 = null;
            _liquidEntry = null;
            _liquidFlags = null;
            _liquidMap = null;
            _gridGetHeight = GetHeightFromFlat;
        }

        public ushort GetArea(float x, float y)
        {
            if (AreaMap == null)
                return _gridArea;

            x = 16 * (32 - x / MapConst.SizeofGrids);
            y = 16 * (32 - y / MapConst.SizeofGrids);
            int lx = (int)x & 15;
            int ly = (int)y & 15;

            return AreaMap[lx * 16 + ly];
        }

        public float GetMinHeight(float x, float y)
        {
            if (_minHeightPlanes == null)
                return -500.0f;

            GridCoord gridCoord = GridDefines.ComputeGridCoordSimple(x, y);

            int doubleGridX = (int)(Math.Floor(-(x - MapConst.MapHalfSize) / MapConst.CenterGridOffset));
            int doubleGridY = (int)(Math.Floor(-(y - MapConst.MapHalfSize) / MapConst.CenterGridOffset));

            float gx = x - ((int)gridCoord.X_coord - MapConst.CenterGridId + 1) * MapConst.SizeofGrids;
            float gy = y - ((int)gridCoord.Y_coord - MapConst.CenterGridId + 1) * MapConst.SizeofGrids;

            uint quarterIndex;

            if (Convert.ToBoolean(doubleGridY & 1))
            {
                if (Convert.ToBoolean(doubleGridX & 1))
                    quarterIndex = 4 + (gx <= gy ? 1 : 0u);
                else
                    quarterIndex = (2 + ((-MapConst.SizeofGrids - gx) > gy ? 1u : 0));
            }
            else if (Convert.ToBoolean(doubleGridX & 1))
            {
                quarterIndex = 6 + ((-MapConst.SizeofGrids - gx) <= gy ? 1u : 0);
            }
            else
            {
                quarterIndex = gx > gy ? 1u : 0;
            }

            Ray ray = new(new Vector3(gx, gy, 0.0f), Vector3.UnitZ);

            return ray.intersection(_minHeightPlanes[quarterIndex]).Z;
        }

        public float GetLiquidLevel(float x, float y)
        {
            if (_liquidMap == null)
                return _liquidLevel;

            x = MapConst.MapResolution * (32 - x / MapConst.SizeofGrids);
            y = MapConst.MapResolution * (32 - y / MapConst.SizeofGrids);

            int cx_int = ((int)x & (MapConst.MapResolution - 1)) - _liquidOffY;
            int cy_int = ((int)y & (MapConst.MapResolution - 1)) - _liquidOffX;

            if (cx_int < 0 ||
                cx_int >= _liquidHeight)
                return MapConst.InvalidHeight;

            if (cy_int < 0 ||
                cy_int >= _liquidWidth)
                return MapConst.InvalidHeight;

            return _liquidMap[cx_int * _liquidWidth + cy_int];
        }

        // Get water State on map
        public ZLiquidStatus GetLiquidStatus(float x, float y, float z, LiquidHeaderTypeFlags? reqLiquidType, LiquidData data, float collisionHeight)
        {
            // Check water Type (if no water return)
            if (_liquidGlobalFlags == LiquidHeaderTypeFlags.NoWater &&
                _liquidFlags == null)
                return ZLiquidStatus.NoWater;

            // Get cell
            float cx = MapConst.MapResolution * (32 - x / MapConst.SizeofGrids);
            float cy = MapConst.MapResolution * (32 - y / MapConst.SizeofGrids);

            int x_int = (int)cx & (MapConst.MapResolution - 1);
            int y_int = (int)cy & (MapConst.MapResolution - 1);

            // Check water Type in cell
            int idx = (x_int >> 3) * 16 + (y_int >> 3);
            LiquidHeaderTypeFlags type = _liquidFlags != null ? (LiquidHeaderTypeFlags)_liquidFlags[idx] : _liquidGlobalFlags;
            uint entry = _liquidEntry != null ? _liquidEntry[idx] : _liquidGlobalEntry;
            LiquidTypeRecord liquidEntry = CliDB.LiquidTypeStorage.LookupByKey(entry);

            if (liquidEntry != null)
            {
                type &= LiquidHeaderTypeFlags.DarkWater;
                uint liqTypeIdx = liquidEntry.SoundBank;

                if (entry < 21)
                {
                    var area = CliDB.AreaTableStorage.LookupByKey(GetArea(x, y));

                    if (area != null)
                    {
                        uint overrideLiquid = area.LiquidTypeID[liquidEntry.SoundBank];

                        if (overrideLiquid == 0 &&
                            area.ParentAreaID == 0)
                        {
                            area = CliDB.AreaTableStorage.LookupByKey(area.ParentAreaID);

                            if (area != null)
                                overrideLiquid = area.LiquidTypeID[liquidEntry.SoundBank];
                        }

                        var liq = CliDB.LiquidTypeStorage.LookupByKey(overrideLiquid);

                        if (liq != null)
                        {
                            entry = overrideLiquid;
                            liqTypeIdx = liq.SoundBank;
                        }
                    }
                }

                type |= (LiquidHeaderTypeFlags)(1 << (int)liqTypeIdx);
            }

            if (type == LiquidHeaderTypeFlags.NoWater)
                return ZLiquidStatus.NoWater;

            // Check req liquid Type mask
            if (reqLiquidType.HasValue &&
                (reqLiquidType & type) == LiquidHeaderTypeFlags.NoWater)
                return ZLiquidStatus.NoWater;

            // Check water level:
            // Check water height map
            int lx_int = x_int - _liquidOffY;
            int ly_int = y_int - _liquidOffX;

            if (lx_int < 0 ||
                lx_int >= _liquidHeight)
                return ZLiquidStatus.NoWater;

            if (ly_int < 0 ||
                ly_int >= _liquidWidth)
                return ZLiquidStatus.NoWater;

            // Get water level
            float liquid_level = _liquidMap != null ? _liquidMap[lx_int * _liquidWidth + ly_int] : _liquidLevel;
            // Get ground level (sub 0.2 for fix some errors)
            float ground_level = GetHeight(x, y);

            // Check water level and ground level
            if (liquid_level < ground_level ||
                z < ground_level)
                return ZLiquidStatus.NoWater;

            // All ok in water . store _data
            if (data != null)
            {
                data.Entry = entry;
                data.Type_flags = type;
                data.Level = liquid_level;
                data.Depth_level = ground_level;
            }

            // For speed check as int values
            float delta = liquid_level - z;

            if (delta > collisionHeight) // Under water
                return ZLiquidStatus.UnderWater;

            if (delta > 0.0f) // In water
                return ZLiquidStatus.InWater;

            if (delta > -0.1f) // Walk on water
                return ZLiquidStatus.WaterWalk;

            // Above water
            return ZLiquidStatus.AboveWater;
        }

        public float GetHeight(float x, float y)
        {
            return _gridGetHeight(x, y);
        }

        private bool LoadAreaData(BinaryReader reader, uint offset)
        {
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            MapAreaHeader areaHeader = reader.Read<MapAreaHeader>();

            if (areaHeader.Fourcc != MapConst.MapAreaMagic)
                return false;

            _gridArea = areaHeader.GridArea;

            if (!areaHeader.Flags.HasAnyFlag(AreaHeaderFlags.NoArea))
                AreaMap = reader.ReadArray<ushort>(16 * 16);

            return true;
        }

        private bool LoadHeightData(BinaryReader reader, uint offset)
        {
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            MapHeightHeader mapHeader = reader.Read<MapHeightHeader>();

            if (mapHeader.Fourcc != MapConst.MapHeightMagic)
                return false;

            _gridHeight = mapHeader.GridHeight;
            _flags = (uint)mapHeader.Flags;

            if (!mapHeader.Flags.HasAnyFlag(HeightHeaderFlags.NoHeight))
            {
                if (mapHeader.Flags.HasAnyFlag(HeightHeaderFlags.HeightAsInt16))
                {
                    Uint16_V9 = reader.ReadArray<ushort>(129 * 129);
                    Uint16_V8 = reader.ReadArray<ushort>(128 * 128);

                    _gridIntHeightMultiplier = (mapHeader.GridMaxHeight - mapHeader.GridHeight) / 65535;
                    _gridGetHeight = GetHeightFromUint16;
                }
                else if (mapHeader.Flags.HasAnyFlag(HeightHeaderFlags.HeightAsInt8))
                {
                    Ubyte_V9 = reader.ReadBytes(129 * 129);
                    Ubyte_V8 = reader.ReadBytes(128 * 128);
                    _gridIntHeightMultiplier = (mapHeader.GridMaxHeight - mapHeader.GridHeight) / 255;
                    _gridGetHeight = GetHeightFromUint8;
                }
                else
                {
                    V9 = reader.ReadArray<float>(129 * 129);
                    V8 = reader.ReadArray<float>(128 * 128);

                    _gridGetHeight = GetHeightFromFloat;
                }
            }
            else
            {
                _gridGetHeight = GetHeightFromFlat;
            }

            if (mapHeader.Flags.HasAnyFlag(HeightHeaderFlags.HasFlightBounds))
            {
                short[] maxHeights = reader.ReadArray<short>(3 * 3);
                short[] minHeights = reader.ReadArray<short>(3 * 3);

                uint[][] indices =
                {
                    new uint[]
                    {
                        3, 0, 4
                    },
                    new uint[]
                    {
                        0, 1, 4
                    },
                    new uint[]
                    {
                        1, 2, 4
                    },
                    new uint[]
                    {
                        2, 5, 4
                    },
                    new uint[]
                    {
                        5, 8, 4
                    },
                    new uint[]
                    {
                        8, 7, 4
                    },
                    new uint[]
                    {
                        7, 6, 4
                    },
                    new uint[]
                    {
                        6, 3, 4
                    }
                };

                float[][] boundGridCoords =
                {
                    new[]
                    {
                        0.0f, 0.0f
                    },
                    new[]
                    {
                        0.0f, -266.66666f
                    },
                    new[]
                    {
                        0.0f, -533.33331f
                    },
                    new[]
                    {
                        -266.66666f, 0.0f
                    },
                    new[]
                    {
                        -266.66666f, -266.66666f
                    },
                    new[]
                    {
                        -266.66666f, -533.33331f
                    },
                    new[]
                    {
                        -533.33331f, 0.0f
                    },
                    new[]
                    {
                        -533.33331f, -266.66666f
                    },
                    new[]
                    {
                        -533.33331f, -533.33331f
                    }
                };

                _minHeightPlanes = new Plane[8];

                for (uint quarterIndex = 0; quarterIndex < 8; ++quarterIndex)
                    _minHeightPlanes[quarterIndex] = Plane.CreateFromVertices(new Vector3(boundGridCoords[indices[quarterIndex][0]][0], boundGridCoords[indices[quarterIndex][0]][1], minHeights[indices[quarterIndex][0]]),
                                                                              new Vector3(boundGridCoords[indices[quarterIndex][1]][0], boundGridCoords[indices[quarterIndex][1]][1], minHeights[indices[quarterIndex][1]]),
                                                                              new Vector3(boundGridCoords[indices[quarterIndex][2]][0], boundGridCoords[indices[quarterIndex][2]][1], minHeights[indices[quarterIndex][2]]));
            }

            return true;
        }

        private bool LoadLiquidData(BinaryReader reader, uint offset)
        {
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            MapLiquidHeader liquidHeader = reader.Read<MapLiquidHeader>();

            if (liquidHeader.Fourcc != MapConst.MapLiquidMagic)
                return false;

            _liquidGlobalEntry = liquidHeader.LiquidType;
            _liquidGlobalFlags = (LiquidHeaderTypeFlags)liquidHeader.LiquidFlags;
            _liquidOffX = liquidHeader.OffsetX;
            _liquidOffY = liquidHeader.OffsetY;
            _liquidWidth = liquidHeader.Width;
            _liquidHeight = liquidHeader.Height;
            _liquidLevel = liquidHeader.LiquidLevel;

            if (!liquidHeader.Flags.HasAnyFlag(LiquidHeaderFlags.NoType))
            {
                _liquidEntry = reader.ReadArray<ushort>(16 * 16);
                _liquidFlags = reader.ReadBytes(16 * 16);
            }

            if (!liquidHeader.Flags.HasAnyFlag(LiquidHeaderFlags.NoHeight))
                _liquidMap = reader.ReadArray<float>((uint)(_liquidWidth * _liquidHeight));

            return true;
        }

        private bool LoadHolesData(BinaryReader reader, uint offset)
        {
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);

            _holes = reader.ReadArray<byte>(16 * 16 * 8);

            return true;
        }

        private float GetHeightFromFlat(float x, float y)
        {
            return _gridHeight;
        }

        private float GetHeightFromFloat(float x, float y)
        {
            if (Uint16_V8 == null ||
                Uint16_V9 == null)
                return _gridHeight;

            x = MapConst.MapResolution * (32 - x / MapConst.SizeofGrids);
            y = MapConst.MapResolution * (32 - y / MapConst.SizeofGrids);

            int x_int = (int)x;
            int y_int = (int)y;
            x -= x_int;
            y -= y_int;
            x_int &= (MapConst.MapResolution - 1);
            y_int &= (MapConst.MapResolution - 1);

            if (IsHole(x_int, y_int))
                return MapConst.InvalidHeight;

            float a, b, c;

            if (x + y < 1)
            {
                if (x > y)
                {
                    // 1 triangle (h1, h2, h5 points)
                    float h1 = V9[(x_int) * 129 + y_int];
                    float h2 = V9[(x_int + 1) * 129 + y_int];
                    float h5 = 2 * V8[x_int * 128 + y_int];
                    a = h2 - h1;
                    b = h5 - h1 - h2;
                    c = h1;
                }
                else
                {
                    // 2 triangle (h1, h3, h5 points)
                    float h1 = V9[x_int * 129 + y_int];
                    float h3 = V9[x_int * 129 + y_int + 1];
                    float h5 = 2 * V8[x_int * 128 + y_int];
                    a = h5 - h1 - h3;
                    b = h3 - h1;
                    c = h1;
                }
            }
            else
            {
                if (x > y)
                {
                    // 3 triangle (h2, h4, h5 points)
                    float h2 = V9[(x_int + 1) * 129 + y_int];
                    float h4 = V9[(x_int + 1) * 129 + y_int + 1];
                    float h5 = 2 * V8[x_int * 128 + y_int];
                    a = h2 + h4 - h5;
                    b = h4 - h2;
                    c = h5 - h4;
                }
                else
                {
                    // 4 triangle (h3, h4, h5 points)
                    float h3 = V9[(x_int) * 129 + y_int + 1];
                    float h4 = V9[(x_int + 1) * 129 + y_int + 1];
                    float h5 = 2 * V8[x_int * 128 + y_int];
                    a = h4 - h3;
                    b = h3 + h4 - h5;
                    c = h5 - h4;
                }
            }

            // Calculate height
            return a * x + b * y + c;
        }

        private float GetHeightFromUint8(float x, float y)
        {
            if (Ubyte_V8 == null ||
                Ubyte_V9 == null)
                return _gridHeight;

            x = MapConst.MapResolution * (32 - x / MapConst.SizeofGrids);
            y = MapConst.MapResolution * (32 - y / MapConst.SizeofGrids);

            int x_int = (int)x;
            int y_int = (int)y;
            x -= x_int;
            y -= y_int;
            x_int &= (MapConst.MapResolution - 1);
            y_int &= (MapConst.MapResolution - 1);

            if (IsHole(x_int, y_int))
                return MapConst.InvalidHeight;

            int a, b, c;

            unsafe
            {
                fixed (byte* V9 = Ubyte_V9)
                {
                    byte* V9_h1_ptr = &V9[x_int * 128 + x_int + y_int];

                    if (x + y < 1)
                    {
                        if (x > y)
                        {
                            // 1 triangle (h1, h2, h5 points)
                            int h1 = V9_h1_ptr[0];
                            int h2 = V9_h1_ptr[129];
                            int h5 = 2 * Ubyte_V8[x_int * 128 + y_int];
                            a = h2 - h1;
                            b = h5 - h1 - h2;
                            c = h1;
                        }
                        else
                        {
                            // 2 triangle (h1, h3, h5 points)
                            int h1 = V9_h1_ptr[0];
                            int h3 = V9_h1_ptr[1];
                            int h5 = 2 * Ubyte_V8[x_int * 128 + y_int];
                            a = h5 - h1 - h3;
                            b = h3 - h1;
                            c = h1;
                        }
                    }
                    else
                    {
                        if (x > y)
                        {
                            // 3 triangle (h2, h4, h5 points)
                            int h2 = V9_h1_ptr[129];
                            int h4 = V9_h1_ptr[130];
                            int h5 = 2 * Ubyte_V8[x_int * 128 + y_int];
                            a = h2 + h4 - h5;
                            b = h4 - h2;
                            c = h5 - h4;
                        }
                        else
                        {
                            // 4 triangle (h3, h4, h5 points)
                            int h3 = V9_h1_ptr[1];
                            int h4 = V9_h1_ptr[130];
                            int h5 = 2 * Ubyte_V8[x_int * 128 + y_int];
                            a = h4 - h3;
                            b = h3 + h4 - h5;
                            c = h5 - h4;
                        }
                    }

                    // Calculate height
                    return ((a * x) + (b * y) + c) * _gridIntHeightMultiplier + _gridHeight;
                }
            }
        }

        private float GetHeightFromUint16(float x, float y)
        {
            if (Uint16_V8 == null ||
                Uint16_V9 == null)
                return _gridHeight;

            x = MapConst.MapResolution * (MapConst.CenterGridId - x / MapConst.SizeofGrids);
            y = MapConst.MapResolution * (MapConst.CenterGridId - y / MapConst.SizeofGrids);

            int x_int = (int)x;
            int y_int = (int)y;
            x -= x_int;
            y -= y_int;
            x_int &= (MapConst.MapResolution - 1);
            y_int &= (MapConst.MapResolution - 1);

            if (IsHole(x_int, y_int))
                return MapConst.InvalidHeight;

            int a, b, c;

            unsafe
            {
                fixed (ushort* V9 = Uint16_V9)
                {
                    ushort* V9_h1_ptr = &V9[x_int * 128 + x_int + y_int];

                    if (x + y < 1)
                    {
                        if (x > y)
                        {
                            // 1 triangle (h1, h2, h5 points)
                            int h1 = V9_h1_ptr[0];
                            int h2 = V9_h1_ptr[129];
                            int h5 = 2 * Uint16_V8[x_int * 128 + y_int];
                            a = h2 - h1;
                            b = h5 - h1 - h2;
                            c = h1;
                        }
                        else
                        {
                            // 2 triangle (h1, h3, h5 points)
                            int h1 = V9_h1_ptr[0];
                            int h3 = V9_h1_ptr[1];
                            int h5 = 2 * Uint16_V8[x_int * 128 + y_int];
                            a = h5 - h1 - h3;
                            b = h3 - h1;
                            c = h1;
                        }
                    }
                    else
                    {
                        if (x > y)
                        {
                            // 3 triangle (h2, h4, h5 points)
                            int h2 = V9_h1_ptr[129];
                            int h4 = V9_h1_ptr[130];
                            int h5 = 2 * Uint16_V8[x_int * 128 + y_int];
                            a = h2 + h4 - h5;
                            b = h4 - h2;
                            c = h5 - h4;
                        }
                        else
                        {
                            // 4 triangle (h3, h4, h5 points)
                            int h3 = V9_h1_ptr[1];
                            int h4 = V9_h1_ptr[130];
                            int h5 = 2 * Uint16_V8[x_int * 128 + y_int];
                            a = h4 - h3;
                            b = h3 + h4 - h5;
                            c = h5 - h4;
                        }
                    }

                    // Calculate height
                    return ((a * x) + (b * y) + c) * _gridIntHeightMultiplier + _gridHeight;
                }
            }
        }

        private bool IsHole(int row, int col)
        {
            if (_holes == null)
                return false;

            int cellRow = row / 8; // 8 squares per cell
            int cellCol = col / 8;
            int holeRow = row % 8;
            int holeCol = col % 8;

            return (_holes[cellRow * 16 * 8 + cellCol * 8 + holeRow] & (1 << holeCol)) != 0;
        }

        #region Fields

        private delegate float GetHeightDel(float x, float y);

        private GetHeightDel _gridGetHeight;
        private uint _flags;

        public float[] V9 { get; set; }
        public ushort[] Uint16_V9 { get; set; }
        public byte[] Ubyte_V9 { get; set; }

        public float[] V8 { get; set; }
        public ushort[] Uint16_V8 { get; set; }
        public byte[] Ubyte_V8 { get; set; }
        private Plane[] _minHeightPlanes;
        private float _gridHeight;
        private float _gridIntHeightMultiplier;

        //Area _data
        public ushort[] AreaMap { get; set; }

        //Liquid Map
        private float _liquidLevel;
        private ushort[] _liquidEntry;
        private byte[] _liquidFlags;
        private float[] _liquidMap;
        private ushort _gridArea;
        private ushort _liquidGlobalEntry;
        private LiquidHeaderTypeFlags _liquidGlobalFlags;
        private byte _liquidOffX;
        private byte _liquidOffY;
        private byte _liquidWidth;
        private byte _liquidHeight;

        private byte[] _holes;

        #endregion
    }
}