using Framework.Constants;

namespace Game.Collision
{
    public struct MapFileHeader
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

    public struct MapAreaHeader
    {
        public uint fourcc;
        public AreaHeaderFlags flags;
        public ushort gridArea;
    }

    public struct MapHeightHeader
    {
        public uint fourcc;
        public HeightHeaderFlags flags;
        public float gridHeight;
        public float gridMaxHeight;
    }

    public struct MapLiquidHeader
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
        public LiquidHeaderTypeFlags type_flags;
        public uint entry;
        public float level;
        public float depth_level;
    }

    public class OffMeshData
    {
        public uint MapId;
        public uint TileX;
        public uint TileY;
        public float[] From = new float[3];
        public float[] To = new float[3];
        public float Radius;
        public OffMeshConnectionFlag ConnectionFlags;
        public byte AreaId;
        public NavTerrainFlag Flags;
    }

    public struct MmapTileHeader
    {
        public uint mmapMagic;
        public uint dtVersion;
        public uint mmapVersion;
        public uint size;
        public byte usesLiquids;
    }

    public enum MMapLoadResult
    {
        Success,
        AlreadyLoaded,
        FileNotFound,
        VersionMismatch,
        ReadFromFileFailed,
        LibraryError
    }
}
