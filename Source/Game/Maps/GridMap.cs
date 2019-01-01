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
using Game.DataStorage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Game.Maps
{
    public class GridMap
    {
        public GridMap()
        {
            // Height level data
            _gridHeight = MapConst.InvalidHeight;
            _gridGetHeight = getHeightFromFlat;

            // Liquid data
            _liquidLevel = MapConst.InvalidHeight;
        }

        public bool loadData(string filename)
        {
            unloadData();
            if (!File.Exists(filename))
                return true;

            _fileExists = true;
            using (BinaryReader reader = new BinaryReader(new FileStream(filename, FileMode.Open, FileAccess.Read)))
            {
                mapFileHeader header = reader.Read<mapFileHeader>();
                if (header.mapMagic != MapConst.MapMagic || header.versionMagic != MapConst.MapVersionMagic)
                {
                    Log.outError(LogFilter.Maps, $"Map file '{filename}' is from an incompatible map version. Please recreate using the mapextractor.");
                    return false;
                }

                if (header.areaMapOffset != 0 && !LoadAreaData(reader, header.areaMapOffset))
                {
                    Log.outError(LogFilter.Maps, "Error loading map area data");
                    return false;
                }

                if (header.heightMapOffset != 0 && !LoadHeightData(reader, header.heightMapOffset))
                {
                    Log.outError(LogFilter.Maps, "Error loading map height data");
                    return false;
                }

                if (header.liquidMapOffset != 0 && !LoadLiquidData(reader, header.liquidMapOffset))
                {
                    Log.outError(LogFilter.Maps, "Error loading map liquids data");
                    return false;
                }

                return true;
            }
        }

        public void unloadData()
        {
            _areaMap = null;
            m_V9 = null;
            m_V8 = null;
            _liquidEntry = null;
            _liquidFlags = null;
            _liquidMap = null;
            _gridGetHeight = getHeightFromFlat;
            _fileExists = false;
        }

        bool LoadAreaData(BinaryReader reader, uint offset)
        {
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            map_AreaHeader areaHeader = reader.Read<map_AreaHeader>();
            if (areaHeader.fourcc != MapConst.MapAreaMagic)
                return false;

            _gridArea = areaHeader.gridArea;

            if (!areaHeader.flags.HasAnyFlag(AreaHeaderFlags.NoArea))
                _areaMap = reader.ReadArray<ushort>(16 * 16);

            return true;
        }

        bool LoadHeightData(BinaryReader reader, uint offset)
        {
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            map_HeightHeader mapHeader = reader.Read<map_HeightHeader>();

            if (mapHeader.fourcc != MapConst.MapHeightMagic)
                return false;

            _gridHeight = mapHeader.gridHeight;
            _flags = (uint)mapHeader.flags;

            if (!mapHeader.flags.HasAnyFlag(HeightHeaderFlags.NoHeight))
            {
                if (mapHeader.flags.HasAnyFlag(HeightHeaderFlags.HeightAsInt16))
                {
                    m_uint16_V9 = reader.ReadArray<ushort>(129 * 129);
                    m_uint16_V8 = reader.ReadArray<ushort>(128 * 128);

                    _gridIntHeightMultiplier = (mapHeader.gridMaxHeight - mapHeader.gridHeight) / 65535;
                    _gridGetHeight = getHeightFromUint16;
                }
                else if (mapHeader.flags.HasAnyFlag(HeightHeaderFlags.HeightAsInt8))
                {
                    m_ubyte_V9 = reader.ReadBytes(129 * 129);
                    m_ubyte_V8 = reader.ReadBytes(128 * 128);
                    _gridIntHeightMultiplier = (mapHeader.gridMaxHeight - mapHeader.gridHeight) / 255;
                    _gridGetHeight = getHeightFromUint8;
                }
                else
                {
                    m_V9 = reader.ReadArray<float>(129 * 129);
                    m_V8 = reader.ReadArray<float>(128 * 128);

                    _gridGetHeight = getHeightFromFloat;
                }
            }
            else
                _gridGetHeight = getHeightFromFlat;

            if (mapHeader.flags.HasAnyFlag(HeightHeaderFlags.HeightHasFlightBounds))
            {
                short[] maxHeights = reader.ReadArray<short>(3 * 3);
                short[] minHeights = reader.ReadArray<short>(3 * 3);

                uint[][] indices =
                {
                    new uint[] { 3, 0, 4 },
                    new uint[] { 0, 1, 4 },
                    new uint[] { 1, 2, 4 },
                    new uint[] { 2, 5, 4 },
                    new uint[] { 5, 8, 4 },
                    new uint[] { 8, 7, 4 },
                    new uint[] { 7, 6, 4 },
                    new uint[] { 6, 3, 4 }
                };

                float[][] boundGridCoords =
                {
                    new [] { 0.0f, 0.0f },
                    new [] { 0.0f, -266.66666f },
                    new [] { 0.0f, -533.33331f },
                    new [] { -266.66666f, 0.0f },
                    new [] { -266.66666f, -266.66666f },
                    new [] { -266.66666f, -533.33331f },
                    new [] { -533.33331f, 0.0f },
                    new [] { -533.33331f, -266.66666f },
                    new [] { -533.33331f, -533.33331f }
                };

                _minHeightPlanes = new Plane[8];
                for (uint quarterIndex = 0; quarterIndex < 8; ++quarterIndex)
                    _minHeightPlanes[quarterIndex] = new Plane(
                        new Vector3(boundGridCoords[indices[quarterIndex][0]][0], boundGridCoords[indices[quarterIndex][0]][1], minHeights[indices[quarterIndex][0]]),
                        new Vector3(boundGridCoords[indices[quarterIndex][1]][0], boundGridCoords[indices[quarterIndex][1]][1], minHeights[indices[quarterIndex][1]]),
                        new Vector3(boundGridCoords[indices[quarterIndex][2]][0], boundGridCoords[indices[quarterIndex][2]][1], minHeights[indices[quarterIndex][2]])
                    );
            }

            return true;
        }

        bool LoadLiquidData(BinaryReader reader, uint offset)
        {
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            map_LiquidHeader liquidHeader = reader.Read<map_LiquidHeader>();

            if (liquidHeader.fourcc != MapConst.MapLiquidMagic)
                return false;

            _liquidGlobalEntry = liquidHeader.liquidType;
            _liquidGlobalFlags = liquidHeader.liquidFlags;
            _liquidOffX = liquidHeader.offsetX;
            _liquidOffY = liquidHeader.offsetY;
            _liquidWidth = liquidHeader.width;
            _liquidHeight = liquidHeader.height;
            _liquidLevel = liquidHeader.liquidLevel;

            if (!liquidHeader.flags.HasAnyFlag(LiquidHeaderFlags.LiquidNoType))
            {
                _liquidEntry = reader.ReadArray<ushort>(16 * 16);
                _liquidFlags = reader.ReadBytes(16 * 16);
            }

            if (!liquidHeader.flags.HasAnyFlag(LiquidHeaderFlags.LiquidNoHeight))
                _liquidMap = reader.ReadArray<float>((uint)(_liquidWidth * _liquidHeight));

            return true;
        }

        public ushort getArea(float x, float y)
        {
            if (_areaMap == null)
                return _gridArea;

            x = 16 * (32 - x / MapConst.SizeofGrids);
            y = 16 * (32 - y / MapConst.SizeofGrids);
            int lx = (int)x & 15;
            int ly = (int)y & 15;
            return _areaMap[lx * 16 + ly];
        }

        float getHeightFromFlat(float x, float y)
        {
            return _gridHeight;
        }

        float getHeightFromFloat(float x, float y)
        {
            if (m_uint16_V8 == null || m_uint16_V9 == null)
                return _gridHeight;

            x = MapConst.MapResolution * (32 - x / MapConst.SizeofGrids);
            y = MapConst.MapResolution * (32 - y / MapConst.SizeofGrids);

            int x_int = (int)x;
            int y_int = (int)y;
            x -= x_int;
            y -= y_int;
            x_int &= (MapConst.MapResolution - 1);
            y_int &= (MapConst.MapResolution - 1);

            float a, b, c;
            if (x + y < 1)
            {
                if (x > y)
                {
                    // 1 triangle (h1, h2, h5 points)
                    float h1 = m_V9[(x_int) * 129 + y_int];
                    float h2 = m_V9[(x_int + 1) * 129 + y_int];
                    float h5 = 2 * m_V8[x_int * 128 + y_int];
                    a = h2 - h1;
                    b = h5 - h1 - h2;
                    c = h1;
                }
                else
                {
                    // 2 triangle (h1, h3, h5 points)
                    float h1 = m_V9[x_int * 129 + y_int];
                    float h3 = m_V9[x_int * 129 + y_int + 1];
                    float h5 = 2 * m_V8[x_int * 128 + y_int];
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
                    float h2 = m_V9[(x_int + 1) * 129 + y_int];
                    float h4 = m_V9[(x_int + 1) * 129 + y_int + 1];
                    float h5 = 2 * m_V8[x_int * 128 + y_int];
                    a = h2 + h4 - h5;
                    b = h4 - h2;
                    c = h5 - h4;
                }
                else
                {
                    // 4 triangle (h3, h4, h5 points)
                    float h3 = m_V9[(x_int) * 129 + y_int + 1];
                    float h4 = m_V9[(x_int + 1) * 129 + y_int + 1];
                    float h5 = 2 * m_V8[x_int * 128 + y_int];
                    a = h4 - h3;
                    b = h3 + h4 - h5;
                    c = h5 - h4;
                }
            }
            // Calculate height
            return a * x + b * y + c;
        }

        float getHeightFromUint8(float x, float y)
        {
            if (m_ubyte_V8 == null || m_ubyte_V9 == null)
                return _gridHeight;

            x = MapConst.MapResolution * (32 - x / MapConst.SizeofGrids);
            y = MapConst.MapResolution * (32 - y / MapConst.SizeofGrids);

            int x_int = (int)x;
            int y_int = (int)y;
            x -= x_int;
            y -= y_int;
            x_int &= (MapConst.MapResolution - 1);
            y_int &= (MapConst.MapResolution - 1);
            int a, b, c;

            unsafe
            {
                fixed (byte* V9 = m_ubyte_V9)
                {
                    byte* V9_h1_ptr = &V9[x_int * 128 + x_int + y_int];
                    if (x + y < 1)
                    {
                        if (x > y)
                        {
                            // 1 triangle (h1, h2, h5 points)
                            int h1 = V9_h1_ptr[0];
                            int h2 = V9_h1_ptr[129];
                            int h5 = 2 * m_ubyte_V8[x_int * 128 + y_int];
                            a = h2 - h1;
                            b = h5 - h1 - h2;
                            c = h1;
                        }
                        else
                        {
                            // 2 triangle (h1, h3, h5 points)
                            int h1 = V9_h1_ptr[0];
                            int h3 = V9_h1_ptr[1];
                            int h5 = 2 * m_ubyte_V8[x_int * 128 + y_int];
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
                            int h5 = 2 * m_ubyte_V8[x_int * 128 + y_int];
                            a = h2 + h4 - h5;
                            b = h4 - h2;
                            c = h5 - h4;
                        }
                        else
                        {
                            // 4 triangle (h3, h4, h5 points)
                            int h3 = V9_h1_ptr[1];
                            int h4 = V9_h1_ptr[130];
                            int h5 = 2 * m_ubyte_V8[x_int * 128 + y_int];
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

        float getHeightFromUint16(float x, float y)
        {
            if (m_uint16_V8 == null || m_uint16_V9 == null)
                return _gridHeight;

            x = MapConst.MapResolution * (32 - x / MapConst.SizeofGrids);
            y = MapConst.MapResolution * (32 - y / MapConst.SizeofGrids);

            int x_int = (int)x;
            int y_int = (int)y;
            x -= x_int;
            y -= y_int;
            x_int &= (MapConst.MapResolution - 1);
            y_int &= (MapConst.MapResolution - 1);
            int a, b, c;
            unsafe
            {
                fixed (ushort* V9 = m_uint16_V9)
                {
                    ushort* V9_h1_ptr = &V9[x_int * 128 + x_int + y_int];
                    if (x + y < 1)
                    {
                        if (x > y)
                        {
                            // 1 triangle (h1, h2, h5 points)
                            int h1 = V9_h1_ptr[0];
                            int h2 = V9_h1_ptr[129];
                            int h5 = 2 * m_uint16_V8[x_int * 128 + y_int];
                            a = h2 - h1;
                            b = h5 - h1 - h2;
                            c = h1;
                        }
                        else
                        {
                            // 2 triangle (h1, h3, h5 points)
                            int h1 = V9_h1_ptr[0];
                            int h3 = V9_h1_ptr[1];
                            int h5 = 2 * m_uint16_V8[x_int * 128 + y_int];
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
                            int h5 = 2 * m_uint16_V8[x_int * 128 + y_int];
                            a = h2 + h4 - h5;
                            b = h4 - h2;
                            c = h5 - h4;
                        }
                        else
                        {
                            // 4 triangle (h3, h4, h5 points)
                            int h3 = V9_h1_ptr[1];
                            int h4 = V9_h1_ptr[130];
                            int h5 = 2 * m_uint16_V8[x_int * 128 + y_int];
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

        public float getMinHeight(float x, float y)
        {
            if (_minHeightPlanes == null)
                return -500.0f;

            GridCoord gridCoord = GridDefines.ComputeGridCoord(x, y);

            int doubleGridX = (int)(Math.Floor(-(x - MapConst.MapHalfSize) / MapConst.CenterGridOffset));
            int doubleGridY = (int)(Math.Floor(-(y - MapConst.MapHalfSize) / MapConst.CenterGridOffset));

            float gx = x - ((int)gridCoord.x_coord - MapConst.CenterGridId + 1) * MapConst.SizeofGrids;
            float gy = y - ((int)gridCoord.y_coord - MapConst.CenterGridId + 1) * MapConst.SizeofGrids;

            uint quarterIndex = 0;
            if (Convert.ToBoolean(doubleGridY & 1))
            {
                if (Convert.ToBoolean(doubleGridX & 1))
                    quarterIndex = 4 + (gx <= gy ? 1 : 0u);
                else
                    quarterIndex = (2 + ((-MapConst.SizeofGrids - gx) > gy ? 1u : 0));
            }
            else if (Convert.ToBoolean(doubleGridX & 1))
                quarterIndex = 6 + ((-MapConst.SizeofGrids - gx) <= gy ? 1u : 0);
            else
                quarterIndex = gx > gy ? 1u : 0;


            Ray ray = new Ray(new Vector3(gx, gy, 0.0f), Vector3.ZAxis);
            return ray.intersection(_minHeightPlanes[quarterIndex]).Z;
        }

        public float getLiquidLevel(float x, float y)
        {
            if (_liquidMap == null)
                return _liquidLevel;

            x = MapConst.MapResolution * (32 - x / MapConst.SizeofGrids);
            y = MapConst.MapResolution * (32 - y / MapConst.SizeofGrids);

            int cx_int = ((int)x & (MapConst.MapResolution - 1)) - _liquidOffY;
            int cy_int = ((int)y & (MapConst.MapResolution - 1)) - _liquidOffX;

            if (cx_int < 0 || cx_int >= _liquidHeight)
                return MapConst.InvalidHeight;
            if (cy_int < 0 || cy_int >= _liquidWidth)
                return MapConst.InvalidHeight;

            return _liquidMap[cx_int * _liquidWidth + cy_int];
        }

        // Why does this return LIQUID data?
        public byte getTerrainType(float x, float y)
        {
            if (_liquidFlags == null)
                return 0;

            x = 16 * (32 - x / MapConst.SizeofGrids);
            y = 16 * (32 - y / MapConst.SizeofGrids);
            int lx = (int)x & 15;
            int ly = (int)y & 15;
            return _liquidFlags[lx * 16 + ly];
        }

        // Get water state on map
        public ZLiquidStatus getLiquidStatus(float x, float y, float z, uint ReqLiquidType, LiquidData data)
        {
            // Check water type (if no water return)
            if (_liquidGlobalFlags == 0 && _liquidFlags == null)
                return ZLiquidStatus.NoWater;

            // Get cell
            float cx = MapConst.MapResolution * (32 - x / MapConst.SizeofGrids);
            float cy = MapConst.MapResolution * (32 - y / MapConst.SizeofGrids);

            int x_int = (int)cx & (MapConst.MapResolution - 1);
            int y_int = (int)cy & (MapConst.MapResolution - 1);

            // Check water type in cell
            int idx = (x_int >> 3) * 16 + (y_int >> 3);
            byte type = _liquidFlags != null ? _liquidFlags[idx] : _liquidGlobalFlags;
            uint entry = _liquidEntry != null ? _liquidEntry[idx] : _liquidGlobalEntry;
            LiquidTypeRecord liquidEntry = CliDB.LiquidTypeStorage.LookupByKey(entry);
            if (liquidEntry != null)
            {
                type &= (byte)MapConst.MapLiquidTypeDarkWater;
                uint liqTypeIdx = liquidEntry.SoundBank;
                if (entry < 21)
                {
                    var area = CliDB.AreaTableStorage.LookupByKey(getArea(x, y));
                    if (area != null)
                    {
                        uint overrideLiquid = area.LiquidTypeID[liquidEntry.SoundBank];
                        if (overrideLiquid == 0 && area.ParentAreaID == 0)
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
                type |= (byte)(1 << (int)liqTypeIdx);
            }

            if (type == 0)
                return ZLiquidStatus.NoWater;

            // Check req liquid type mask
            if (ReqLiquidType != 0 && !Convert.ToBoolean(ReqLiquidType & type))
                return ZLiquidStatus.NoWater;

            // Check water level:
            // Check water height map
            int lx_int = x_int - _liquidOffY;
            int ly_int = y_int - _liquidOffX;
            if (lx_int < 0 || lx_int >= _liquidHeight)
                return ZLiquidStatus.NoWater;
            if (ly_int < 0 || ly_int >= _liquidWidth)
                return ZLiquidStatus.NoWater;

            // Get water level
            float liquid_level = _liquidMap != null ? _liquidMap[lx_int * _liquidWidth + ly_int] : _liquidLevel;
            // Get ground level (sub 0.2 for fix some errors)
            float ground_level = getHeight(x, y);

            // Check water level and ground level
            if (liquid_level < ground_level || z < ground_level - 2)
                return ZLiquidStatus.NoWater;

            // All ok in water . store data
            if (data != null)
            {
                data.entry = entry;
                data.type_flags = type;
                data.level = liquid_level;
                data.depth_level = ground_level;
            }

            // For speed check as int values
            float delta = liquid_level - z;

            if (delta > 2.0f)                   // Under water
                return ZLiquidStatus.UnderWater;
            if (delta > 0.0f)                   // In water
                return ZLiquidStatus.InWater;
            if (delta > -0.1f)                   // Walk on water
                return ZLiquidStatus.WaterWalk;
            // Above water
            return ZLiquidStatus.AboveWater;
        }

        public float getHeight(float x, float y) { return _gridGetHeight(x, y); }

        public bool fileExists() { return _fileExists; }

        #region Fields
        delegate float GetHeight(float x, float y);

        GetHeight _gridGetHeight;
        uint _flags;

        public float[] m_V9;
        public ushort[] m_uint16_V9;
        public byte[] m_ubyte_V9;

        public float[] m_V8;
        public ushort[] m_uint16_V8;
        public byte[] m_ubyte_V8;
        Plane[] _minHeightPlanes;
        float _gridHeight;
        float _gridIntHeightMultiplier;

        //Area data
        public ushort[] _areaMap;

        //Liquid Map
        float _liquidLevel;
        ushort[] _liquidEntry;
        byte[] _liquidFlags;
        float[] _liquidMap;
        ushort _gridArea;
        ushort _liquidGlobalEntry;
        byte _liquidGlobalFlags;
        byte _liquidOffX;
        byte _liquidOffY;
        byte _liquidWidth;
        byte _liquidHeight;
        bool _fileExists;
        #endregion
    }

    public struct mapFileHeader
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

    public struct map_AreaHeader
    {
        public uint fourcc;
        public AreaHeaderFlags flags;
        public ushort gridArea;
    }

    public struct map_HeightHeader
    {
        public uint fourcc;
        public HeightHeaderFlags flags;
        public float gridHeight;
        public float gridMaxHeight;
    }

    public struct map_LiquidHeader
    {
        public uint fourcc;
        public LiquidHeaderFlags flags;
        public byte liquidFlags;
        public ushort liquidType;
        public byte offsetX;
        public byte offsetY;
        public byte width;
        public byte height;
        public float liquidLevel;
    }

    public class LiquidData
    {
        public uint type_flags;
        public uint entry;
        public float level;
        public float depth_level;
    }

    [Flags]
    public enum AreaHeaderFlags : ushort
    {
        NoArea = 0x0001
    }

    [Flags]
    public enum HeightHeaderFlags
    {
        NoHeight = 0x0001,
        HeightAsInt16 = 0x0002,
        HeightAsInt8 = 0x0004,
        HeightHasFlightBounds = 0x0008
    }

    [Flags]
    public enum LiquidHeaderFlags : byte
    {
        LiquidNoType = 0x0001,
        LiquidNoHeight = 0x0002
    }
}
