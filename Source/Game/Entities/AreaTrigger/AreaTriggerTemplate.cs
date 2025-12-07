// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Maps;
using Game.Networking;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Game.Entities
{
    [StructLayout(LayoutKind.Explicit)]
    public unsafe class AreaTriggerData
    {
        [FieldOffset(0)]
        public defaultdatas DefaultDatas;

        [FieldOffset(0)]
        public spheredatas SphereDatas;

        [FieldOffset(0)]
        public boxdatas BoxDatas;

        [FieldOffset(0)]
        public polygondatas PolygonDatas;

        [FieldOffset(0)]
        public cylinderdatas CylinderDatas;

        [FieldOffset(0)]
        public diskDatas DiskDatas;

        [FieldOffset(0)]
        public boundedPlaneDatas BoundedPlaneDatas;

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

    public struct AreaTriggerId
    {
        public uint Id;
        public bool IsCustom;

        public AreaTriggerId(uint id, bool isCustom)
        {
            Id = id;
            IsCustom = isCustom;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ IsCustom.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this == (AreaTriggerId)obj;
        }

        public static bool operator ==(AreaTriggerId left, AreaTriggerId right)
        {
            return left.Id == right.Id && left.IsCustom == right.IsCustom;
        }
        public static bool operator !=(AreaTriggerId left, AreaTriggerId right)
        {
            return !(left == right);
        }
    }

    public class AreaTriggerShapeInfo : AreaTriggerData
    {
        public AreaTriggerShapeType TriggerType;
        public List<Vector2> PolygonVertices;
        public List<Vector2> PolygonVerticesTarget;

        public AreaTriggerShapeInfo()
        {
            TriggerType = AreaTriggerShapeType.Max;
            PolygonVertices = new();
            PolygonVerticesTarget = new();
        }

        public unsafe float GetMaxSearchRadius()
        {
            switch (TriggerType)
            {
                case AreaTriggerShapeType.Sphere:
                    return Math.Max(SphereDatas.Radius, SphereDatas.RadiusTarget);
                case AreaTriggerShapeType.Box:
                    return MathF.Sqrt(Math.Max(BoxDatas.Extents[0] * BoxDatas.Extents[0] + BoxDatas.Extents[1] * BoxDatas.Extents[1],
                        BoxDatas.ExtentsTarget[0] * BoxDatas.ExtentsTarget[0] + BoxDatas.ExtentsTarget[1] * BoxDatas.ExtentsTarget[1]));
                case AreaTriggerShapeType.Polygon:
                {
                    Position center = new(0.0f, 0.0f);
                    float maxSearchRadius = 0.0f;

                    foreach (var vertex in PolygonVertices)
                        maxSearchRadius = Math.Max(maxSearchRadius, center.GetExactDist2d(vertex.X, vertex.Y));

                    foreach (var vertex in PolygonVerticesTarget)
                        maxSearchRadius = Math.Max(maxSearchRadius, center.GetExactDist2d(vertex.X, vertex.Y));

                    return maxSearchRadius;
                }
                case AreaTriggerShapeType.Cylinder:
                    return Math.Max(CylinderDatas.Radius, CylinderDatas.RadiusTarget);
                case AreaTriggerShapeType.Disk:
                    return Math.Max(DiskDatas.OuterRadius, DiskDatas.OuterRadiusTarget);
                case AreaTriggerShapeType.BoundedPlane:
                    return MathF.Sqrt(Math.Max(BoundedPlaneDatas.Extents[0] * BoundedPlaneDatas.Extents[0] / 4 + BoundedPlaneDatas.Extents[1] * BoundedPlaneDatas.Extents[1] / 4,
                        BoundedPlaneDatas.ExtentsTarget[0] * BoundedPlaneDatas.ExtentsTarget[0] / 4 + BoundedPlaneDatas.ExtentsTarget[1] * BoundedPlaneDatas.ExtentsTarget[1] / 4));
            }

            return 0.0f;
        }

        public bool IsSphere() { return TriggerType == AreaTriggerShapeType.Sphere; }
        public bool IsBox() { return TriggerType == AreaTriggerShapeType.Box; }
        public bool IsPolygon() { return TriggerType == AreaTriggerShapeType.Polygon; }
        public bool IsCylinder() { return TriggerType == AreaTriggerShapeType.Cylinder; }
        public bool IsDisk() { return TriggerType == AreaTriggerShapeType.Disk; }
        public bool IsBoundedPlane() { return TriggerType == AreaTriggerShapeType.BoundedPlane; }
    }

    public class AreaTriggerOrbitInfo
    {
        public ObjectGuid? PathTarget;
        public Vector3? Center;
        public bool CounterClockwise;
        public bool CanLoop;
        public int ExtraTimeForBlending;
        public float Radius;
        public float BlendFromRadius;
        public float InitialAngle;
        public float ZOffset;
    }

    public class AreaTriggerTemplate
    {
        public AreaTriggerId Id;
        public AreaTriggerFlag Flags;
        public uint ActionSetId;
        public AreaTriggerActionSetFlag ActionSetFlags;
        public List<AreaTriggerAction> Actions = new();
    }

    public class AreaTriggerCreateProperties
    {
        public AreaTriggerId Id;
        public AreaTriggerTemplate Template;
        public AreaTriggerCreatePropertiesFlag Flags;

        public uint MoveCurveId;
        public uint ScaleCurveId;
        public uint MorphCurveId;
        public uint FacingCurveId;

        public int AnimId;
        public uint AnimKitId;

        public uint DecalPropertiesId;

        public uint? SpellForVisuals;

        public uint TimeToTargetScale;

        public AreaTriggerShapeInfo Shape = new();

        public float Speed = 1.0f;
        public List<Vector3> SplinePoints = new();
        public AreaTriggerOrbitInfo OrbitInfo;

        public uint ScriptId;

        public AreaTriggerCreateProperties()
        {
            Id = new(0, false);
        }
    }

    public class AreaTriggerSpawn : SpawnData
    {
        public new AreaTriggerId Id;

        public AreaTriggerSpawn() : base(SpawnObjectType.AreaTrigger) { }
    }

    public struct AreaTriggerAction
    {
        public uint Param;
        public AreaTriggerActionTypes ActionType;
        public AreaTriggerActionUserTypes TargetType;
    }
}
