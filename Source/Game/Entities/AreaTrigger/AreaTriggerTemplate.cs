// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Maps;
using OneOf;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Game.Entities
{
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

    interface IShapeInfo
    {
        float GetMaxSearchRadius();
    }

    public class AreaTriggerShapeInfo
    {
        public OneOf<Sphere, Box, Polygon, Cylinder, Disk, BoundedPlane> Data;

        public bool IsSphere() { return Data.IsT0; }
        public bool IsBox() { return Data.IsT1; }
        public bool IsPolygon() { return Data.IsT2; }
        public bool IsCylinder() { return Data.IsT3; }
        public bool IsDisk() { return Data.IsT4; }
        public bool IsBoundedPlane() { return Data.IsT5; }

        public float GetMaxSearchRadius() { return ((IShapeInfo)Data.Value).GetMaxSearchRadius(); }

        public struct Sphere : IShapeInfo
        {
            public float Radius;
            public float RadiusTarget;

            public Sphere() { }
            public Sphere(float[] raw)
            {
                Radius = raw[0];
                RadiusTarget = raw[1];
            }

            public float GetMaxSearchRadius()
            {
                return Math.Max(Radius, RadiusTarget);
            }
        }

        public struct Box : IShapeInfo
        {
            public Vector3 Extents;
            public Vector3 ExtentsTarget;

            public Box() { }
            public Box(float[] raw)
            {
                Extents = new(raw[0], raw[1], raw[2]);
                ExtentsTarget = new(raw[3], raw[4], raw[5]);
            }

            public float GetMaxSearchRadius()
            {
                return MathF.Sqrt(MathF.Max(
                    Extents.X * Extents.X + Extents.Y * Extents.Y,
                    ExtentsTarget.X * ExtentsTarget.X + ExtentsTarget.Y * ExtentsTarget.Y));
            }
        }

        public struct Polygon : IShapeInfo
        {
            public List<Vector2> PolygonVertices = new();
            public List<Vector2> PolygonVerticesTarget = new();
            public float Height;
            public float HeightTarget;

            public Polygon() { }
            public Polygon(float[] raw)
            {
                Height = raw[0];
                HeightTarget = raw[1];
            }

            public float GetMaxSearchRadius()
            {
                Position center = new(0.0f, 0.0f);
                float maxSearchRadius = 0.0f;

                foreach (var vertex in PolygonVertices)
                    maxSearchRadius = Math.Max(maxSearchRadius, center.GetExactDist2d(vertex.X, vertex.Y));

                foreach (var vertex in PolygonVerticesTarget)
                    maxSearchRadius = Math.Max(maxSearchRadius, center.GetExactDist2d(vertex.X, vertex.Y));

                return maxSearchRadius;
            }
        }

        public struct Cylinder : IShapeInfo
        {
            public float Radius;
            public float RadiusTarget;
            public float Height;
            public float HeightTarget;
            public float LocationZOffset;
            public float LocationZOffsetTarget;

            public Cylinder() { }
            public Cylinder(float[] raw)
            {
                Radius = raw[0];
                RadiusTarget = raw[1];
                Height = raw[2];
                HeightTarget = raw[3];
                LocationZOffset = raw[4];
                LocationZOffsetTarget = raw[5];
            }

            public float GetMaxSearchRadius()
            {
                return Math.Max(Radius, RadiusTarget);
            }
        }

        public struct Disk : IShapeInfo
        {
            public float InnerRadius;
            public float InnerRadiusTarget;
            public float OuterRadius;
            public float OuterRadiusTarget;
            public float Height;
            public float HeightTarget;
            public float LocationZOffset;
            public float LocationZOffsetTarget;

            public Disk() { }
            public Disk(float[] raw)
            {
                InnerRadius = raw[0];
                InnerRadiusTarget = raw[1];
                OuterRadius = raw[2];
                OuterRadiusTarget = raw[3];
                Height = raw[4];
                HeightTarget = raw[5];
                LocationZOffset = raw[6];
                LocationZOffsetTarget = raw[7];
            }

            public float GetMaxSearchRadius()
            {
                return Math.Max(OuterRadius, OuterRadiusTarget);
            }
        }

        public struct BoundedPlane : IShapeInfo
        {
            public Vector2 Extents;
            public Vector2 ExtentsTarget;

            public BoundedPlane() { }
            public BoundedPlane(float[] raw)
            {
                Extents = new(raw[0], raw[1]);
                ExtentsTarget = new(raw[2], raw[3]);
            }

            public float GetMaxSearchRadius()
            {
                return MathF.Sqrt(Math.Max(
                    Extents.X * Extents.X / 4 + Extents.Y * Extents.Y / 4,
                    ExtentsTarget.X * ExtentsTarget.X / 4 + ExtentsTarget.Y * ExtentsTarget.Y / 4));
            }
        }
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
        public bool SpeedIsTime;
        public OneOf<EmptyStruct, List<Vector3>, AreaTriggerOrbitInfo> Movement;

        public uint ScriptId;

        public AreaTriggerCreateProperties()
        {
            Id = new(0, false);
        }
    }

    public struct EmptyStruct { }

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
