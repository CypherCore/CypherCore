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
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Framework.Dynamic;
using Game.Network;

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

    public class AreaTriggerScaleInfo
    {
        public AreaTriggerScaleInfo()
        {
            OverrideScale = new OverrideScaleStruct[SharedConst.MaxAreatriggerScale];
            ExtraScale = new ExtraScaleStruct[SharedConst.MaxAreatriggerScale];

            ExtraScale[5].AsFloat = 1.0000001f;
            ExtraScale[6].AsInt32 = 1;
        }

        public OverrideScaleStruct[] OverrideScale;
        public ExtraScaleStruct[] ExtraScale;

        [StructLayout(LayoutKind.Explicit)]
        public struct OverrideScaleStruct
        {
            [FieldOffset(0)]
            public int AsInt32;

            [FieldOffset(0)]
            public float AsFloat;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct ExtraScaleStruct
        {
            [FieldOffset(0)]
            public int AsInt32;

            [FieldOffset(0)]
            public float AsFloat;
        }
    }

    public struct AreaTriggerCircularMovementInfo
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

    public class AreaTriggerTemplate : AreaTriggerData
    {
        public unsafe void InitMaxSearchRadius()
        {
            switch (TriggerType)
            {
                case AreaTriggerTypes.Sphere:
                {
                    MaxSearchRadius = Math.Max(SphereDatas.Radius, SphereDatas.RadiusTarget);
                    break;
                }
                case AreaTriggerTypes.Box:
                {
                    fixed (float* ptr = BoxDatas.Extents)
                    {
                        MaxSearchRadius = (float) Math.Sqrt(ptr[0] * ptr[0] / 4 + ptr[1] * ptr[1] / 4);
                    }

                    break;
                }
                case AreaTriggerTypes.Polygon:
                {
                    if (PolygonDatas.Height <= 0.0f)
                        PolygonDatas.Height = 1.0f;

                    foreach (Vector2 vertice in PolygonVertices)
                    {
                        float pointDist = vertice.GetLength();

                        if (pointDist > MaxSearchRadius)
                            MaxSearchRadius = pointDist;
                    }

                    break;
                }
                case AreaTriggerTypes.Cylinder:
                {
                    MaxSearchRadius = CylinderDatas.Radius;
                    break;
                }
                default:
                    break;
            }
        }

        public bool HasFlag(AreaTriggerFlags flag) { return Flags.HasAnyFlag(flag); }

        public bool IsSphere() { return TriggerType == AreaTriggerTypes.Sphere; }
        public bool IsBox() { return TriggerType == AreaTriggerTypes.Box; }
        public bool IsPolygon() { return TriggerType == AreaTriggerTypes.Polygon; }
        public bool IsCylinder() { return TriggerType == AreaTriggerTypes.Cylinder; }

        public uint Id;
        public AreaTriggerTypes TriggerType;
        public AreaTriggerFlags Flags;
        public uint ScriptId;
        public float MaxSearchRadius;

        public List<Vector2> PolygonVertices = new List<Vector2>();
        public List<Vector2> PolygonVerticesTarget = new List<Vector2>();
        public List<AreaTriggerAction> Actions = new List<AreaTriggerAction>();
    }

    public class AreaTriggerMiscTemplate
    {
        public bool HasSplines() { return SplinePoints.Count >= 2; }

        public uint MiscId;
        public uint AreaTriggerEntry;

        public uint MoveCurveId;
        public uint ScaleCurveId;
        public uint MorphCurveId;
        public uint FacingCurveId;

        public uint AnimId;
        public uint AnimKitId;

        public uint DecalPropertiesId;

        public uint TimeToTarget;
        public uint TimeToTargetScale;

        public AreaTriggerScaleInfo ScaleInfo = new AreaTriggerScaleInfo();
        public AreaTriggerCircularMovementInfo CircularMovementInfo;

        public AreaTriggerTemplate Template;
        public List<Vector3> SplinePoints = new List<Vector3>();
    }

    public struct AreaTriggerAction
    {
        public uint Param;
        public AreaTriggerActionTypes ActionType;
        public AreaTriggerActionUserTypes TargetType;
    }
}
