using System.Runtime.InteropServices;
using Framework.Constants;

namespace Game.Entities;

/// <summary>
/// Scale array definition
/// 0 - Time offset from creation for starting of scaling
/// 1+2,3+4 are values for curve points Vector2[2]
//  5 is packed curve information (has_no_data & 1) | ((interpolation_mode & 0x7) << 1) | ((first_point_offset & 0x7FFFFF) << 4) | ((point_count & 0x1F) << 27)
/// 6 bool is_override, only valid for AREATRIGGER_OVERRIDE_SCALE_CURVE, if true then use _data from AREATRIGGER_OVERRIDE_SCALE_CURVE instead of ScaleCurveId from CreateObject
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public class AreaTriggerScaleInfo
{
    [StructLayout(LayoutKind.Explicit)]
    public struct StructuredData
    {
        [FieldOffset(0)] public uint StartTimeOffset;

        [FieldOffset(4)] public float X;

        [FieldOffset(8)] public float Y;

        [FieldOffset(12)] public float Z;

        [FieldOffset(16)] public float W;

        [FieldOffset(20)] public uint CurveParameters;

        [FieldOffset(24)] public uint OverrideActive;

        public struct curveparameters
        {
            public uint Raw;

            public uint NoData => Raw & 1;
            public uint InterpolationMode => (Raw & 0x7) << 1;
            public uint FirstPointOffset => (Raw & 0x7FFFFF) << 4;
            public uint PointCount => (Raw & 0x1F) << 27;
        }
    }

    public unsafe struct RawData
    {
        public fixed uint Data[SharedConst.MaxAreatriggerScale];
    }

    [FieldOffset(0)] public RawData Raw;

    [FieldOffset(0)] public StructuredData Structured;
}