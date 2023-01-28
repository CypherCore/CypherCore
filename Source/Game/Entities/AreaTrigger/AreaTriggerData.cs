using System.Runtime.InteropServices;
using Framework.Constants;

namespace Game.Entities;

[StructLayout(LayoutKind.Explicit)]
public unsafe class AreaTriggerData
{
    [FieldOffset(0)] public boundedPlaneDatas BoundedPlaneDatas;

    [FieldOffset(0)] public boxdatas BoxDatas;

    [FieldOffset(0)] public cylinderdatas CylinderDatas;

    [FieldOffset(0)] public defaultdatas DefaultDatas;

    [FieldOffset(0)] public diskDatas DiskDatas;

    [FieldOffset(0)] public polygondatas PolygonDatas;

    [FieldOffset(0)] public spheredatas SphereDatas;

    public struct defaultdatas
    {
        public fixed float Data[SharedConst.MaxAreatriggerEntityData];
    }

    // AREATRIGGER_TYPE_SPHERE
    public struct spheredatas
    {
        public float Radius;
        public float RadiusTarget;
    }

    // AREATRIGGER_TYPE_BOX
    public struct boxdatas
    {
        public fixed float Extents[3];
        public fixed float ExtentsTarget[3];
    }

    // AREATRIGGER_TYPE_POLYGON
    public struct polygondatas
    {
        public float Height;
        public float HeightTarget;
    }

    // AREATRIGGER_TYPE_CYLINDER
    public struct cylinderdatas
    {
        public float Radius;
        public float RadiusTarget;
        public float Height;
        public float HeightTarget;
        public float LocationZOffset;
        public float LocationZOffsetTarget;
    }

    // AREATRIGGER_TYPE_DISK
    public struct diskDatas
    {
        public float InnerRadius;
        public float InnerRadiusTarget;
        public float OuterRadius;
        public float OuterRadiusTarget;
        public float Height;
        public float HeightTarget;
        public float LocationZOffset;
        public float LocationZOffsetTarget;
    }

    // AREATRIGGER_TYPE_BOUNDED_PLANE
    public struct boundedPlaneDatas
    {
        public fixed float Extents[2];
        public fixed float ExtentsTarget[2];
    }
}