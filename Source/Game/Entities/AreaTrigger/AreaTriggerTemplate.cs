/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using Framework.Dynamic;
using Framework.GameMath;
using Game.Networking;
using System;
using System.Collections.Generic;
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
    }

    /// <summary>
    /// Scale array definition
    /// 0 - time offset from creation for starting of scaling
    /// 1+2,3+4 are values for curve points Vector2[2]
    //  5 is packed curve information (has_no_data & 1) | ((interpolation_mode & 0x7) << 1) | ((first_point_offset & 0x7FFFFF) << 4) | ((point_count & 0x1F) << 27)
    /// 6 bool is_override, only valid for AREATRIGGER_OVERRIDE_SCALE_CURVE, if true then use data from AREATRIGGER_OVERRIDE_SCALE_CURVE instead of ScaleCurveId from CreateObject
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public class AreaTriggerScaleInfo
    {
        [FieldOffset(0)]
        public StructuredData Structured;

        [FieldOffset(0)]
        public RawData Raw;

        [StructLayout(LayoutKind.Explicit)]
        public struct StructuredData
        {
            [FieldOffset(0)]
            public uint StartTimeOffset;

            [FieldOffset(4)]
            public float X;

            [FieldOffset(8)]
            public float Y;

            [FieldOffset(12)]
            public float Z;

            [FieldOffset(16)]
            public float W;

            [FieldOffset(20)]
            public uint CurveParameters;

            [FieldOffset(24)]
            public uint OverrideActive;

            public struct curveparameters
            {
                public uint Raw;

                public uint NoData { get { return Raw & 1; } }
                public uint InterpolationMode { get { return (Raw & 0x7) << 1; } }
                public uint FirstPointOffset { get { return (Raw & 0x7FFFFF) << 4; } }
                public uint PointCount { get { return (Raw & 0x1F) << 27; } }
            }
        }

        public unsafe struct RawData
        {
            public fixed uint Data[SharedConst.MaxAreatriggerScale];
        }
    }

    public struct AreaTriggerMovementScriptInfo
    {
        public uint SpellScriptID;
        public Vector3 Center;

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(SpellScriptID);
            data.WriteVector3(Center);
        }
    }

    public struct AreaTriggerId
    {
        public uint Id;
        public bool IsServerSide;

        public AreaTriggerId(uint id, bool isServerSide)
        {
            Id = id;
            IsServerSide = isServerSide;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ IsServerSide.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this == (AreaTriggerId)obj;
        }

        public static bool operator ==(AreaTriggerId left, AreaTriggerId right)
        {
            return left.Id == right.Id && left.IsServerSide == right.IsServerSide;
        }
        public static bool operator !=(AreaTriggerId left, AreaTriggerId right)
        {
            return !(left == right);
        }
    }

    public class AreaTriggerShapeInfo : AreaTriggerData
    {  
        public AreaTriggerTypes TriggerType;

        public AreaTriggerShapeInfo()
        {
            TriggerType = AreaTriggerTypes.Max;
        }

        public unsafe float GetMaxSearchRadius()
        {
            switch (TriggerType)
            {
                case AreaTriggerTypes.Sphere:
                    return Math.Max(SphereDatas.Radius, SphereDatas.RadiusTarget);
                case AreaTriggerTypes.Box:
                    return MathF.Sqrt(BoxDatas.Extents[0] * BoxDatas.Extents[0] / 4 + BoxDatas.Extents[1] * BoxDatas.Extents[1] / 4);
                case AreaTriggerTypes.Cylinder:
                    return CylinderDatas.Radius;
            }

            return 0.0f;
        }

        public bool IsSphere() { return TriggerType == AreaTriggerTypes.Sphere; }
        public bool IsBox() { return TriggerType == AreaTriggerTypes.Box; }
        public bool IsPolygon() { return TriggerType == AreaTriggerTypes.Polygon; }
        public bool IsCylinder() { return TriggerType == AreaTriggerTypes.Cylinder; }
    }

    public struct AreaTriggerOrbitInfo
    {
        public void Write(WorldPacket data)
        {
            data.WriteBit(PathTarget.HasValue);
            data.WriteBit(Center.HasValue);
            data.WriteBit(CounterClockwise);
            data.WriteBit(CanLoop);

            data.WriteUInt32(TimeToTarget);
            data.WriteInt32(ElapsedTimeForMovement);
            data.WriteUInt32(StartDelay);
            data.WriteFloat(Radius);
            data.WriteFloat(BlendFromRadius);
            data.WriteFloat(InitialAngle);
            data.WriteFloat(ZOffset);

            if (PathTarget.HasValue)
                data.WritePackedGuid(PathTarget.Value);

            if (Center.HasValue)
                data.WriteVector3(Center.Value);
        }

        public Optional<ObjectGuid> PathTarget;
        public Optional<Vector3> Center;
        public bool CounterClockwise;
        public bool CanLoop;
        public uint TimeToTarget;
        public int ElapsedTimeForMovement;
        public uint StartDelay;
        public float Radius;
        public float BlendFromRadius;
        public float InitialAngle;
        public float ZOffset;
    }

    public class AreaTriggerTemplate
    {
        public AreaTriggerId Id;
        public AreaTriggerFlags Flags;
        public uint ScriptId;

        public List<AreaTriggerAction> Actions = new();

        public bool HasFlag(AreaTriggerFlags flag) { return Flags.HasAnyFlag(flag); }
    }

    public unsafe class AreaTriggerCreateProperties
    {
        public AreaTriggerCreateProperties()
        {
            // legacy code from before it was known what each curve field does
            ExtraScale.Raw.Data[5] = 1065353217;
            // also OverrideActive does nothing on ExtraScale
            ExtraScale.Structured.OverrideActive = 1;
        }

        public bool HasSplines() { return SplinePoints.Count >= 2; }

        public float GetMaxSearchRadius()
        {
            if (Shape.TriggerType == AreaTriggerTypes.Polygon)
            {
                Position center = new(0.0f, 0.0f);
                float maxSearchRadius = 0.0f;

                foreach (var vertice in PolygonVertices)
                {
                    float pointDist = center.GetExactDist2d(vertice.X, vertice.Y);

                    if (pointDist > maxSearchRadius)
                        maxSearchRadius = pointDist;
                }

                return maxSearchRadius;
            }

            return Shape.GetMaxSearchRadius();
        }
        
        public uint Id;
        public AreaTriggerTemplate Template;

        public uint MoveCurveId;
        public uint ScaleCurveId;
        public uint MorphCurveId;
        public uint FacingCurveId;

        public uint AnimId;
        public uint AnimKitId;

        public uint DecalPropertiesId;

        public uint TimeToTarget;
        public uint TimeToTargetScale;

        public AreaTriggerScaleInfo OverrideScale = new();
        public AreaTriggerScaleInfo ExtraScale = new();

        public AreaTriggerShapeInfo Shape;
        public List<Vector2> PolygonVertices = new();
        public List<Vector2> PolygonVerticesTarget = new();
        public List<Vector3> SplinePoints = new();
        public Optional<AreaTriggerOrbitInfo> OrbitInfo;
    }

    public class AreaTriggerSpawn
    {
        public ulong SpawnId;
        public AreaTriggerId Id;
        public WorldLocation Location;
        public uint PhaseId;
        public uint PhaseGroup;
        public byte PhaseUseFlags;

        public AreaTriggerShapeInfo Shape;
    }

    public struct AreaTriggerAction
    {
        public uint Param;
        public AreaTriggerActionTypes ActionType;
        public AreaTriggerActionUserTypes TargetType;
    }
}
