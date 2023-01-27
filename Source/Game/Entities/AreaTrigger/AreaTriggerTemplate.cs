// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Framework.Constants;
using Game.Maps;
using Game.Networking;

namespace Game.Entities
{
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
		[FieldOffset(0)] public RawData Raw;

		[FieldOffset(0)] public StructuredData Structured;

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
			Id           = id;
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
					return Math.Max(CylinderDatas.Radius, CylinderDatas.RadiusTarget);
				case AreaTriggerTypes.Disk:
					return Math.Max(DiskDatas.OuterRadius, DiskDatas.OuterRadiusTarget);
				case AreaTriggerTypes.BoundedPlane:
					return MathF.Sqrt(BoundedPlaneDatas.Extents[0] * BoundedPlaneDatas.Extents[0] / 4 + BoundedPlaneDatas.Extents[1] * BoundedPlaneDatas.Extents[1] / 4);
			}

			return 0.0f;
		}

		public bool IsSphere()
		{
			return TriggerType == AreaTriggerTypes.Sphere;
		}

		public bool IsBox()
		{
			return TriggerType == AreaTriggerTypes.Box;
		}

		public bool IsPolygon()
		{
			return TriggerType == AreaTriggerTypes.Polygon;
		}

		public bool IsCylinder()
		{
			return TriggerType == AreaTriggerTypes.Cylinder;
		}

		public bool IsDisk()
		{
			return TriggerType == AreaTriggerTypes.Disk;
		}

		public bool IsBoudedPlane()
		{
			return TriggerType == AreaTriggerTypes.BoundedPlane;
		}
	}

	public class AreaTriggerOrbitInfo
	{
		public float BlendFromRadius;
		public bool CanLoop;
		public Vector3? Center;
		public bool CounterClockwise;
		public int ElapsedTimeForMovement;
		public float InitialAngle;

		public ObjectGuid? PathTarget;
		public float Radius;
		public uint StartDelay;
		public uint TimeToTarget;
		public float ZOffset;

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
	}

	public class AreaTriggerTemplate
	{
		public List<AreaTriggerAction> Actions = new();
		public AreaTriggerFlags Flags;
		public AreaTriggerId Id;

		public bool HasFlag(AreaTriggerFlags flag)
		{
			return Flags.HasAnyFlag(flag);
		}
	}

	public unsafe class AreaTriggerCreateProperties
	{
		public int AnimId;
		public uint AnimKitId;

		public uint DecalPropertiesId;
		public AreaTriggerScaleInfo ExtraScale = new();
		public uint FacingCurveId;

		public uint Id;
		public uint MorphCurveId;

		public uint MoveCurveId;
		public AreaTriggerOrbitInfo OrbitInfo;

		public AreaTriggerScaleInfo OverrideScale = new();
		public List<Vector2> PolygonVertices = new();
		public List<Vector2> PolygonVerticesTarget = new();
		public uint ScaleCurveId;

		public uint ScriptId;

		public AreaTriggerShapeInfo Shape = new();
		public List<Vector3> SplinePoints = new();
		public AreaTriggerTemplate Template;

		public uint TimeToTarget;
		public uint TimeToTargetScale;

		public AreaTriggerCreateProperties()
		{
			// legacy code from before it was known what each curve field does
			ExtraScale.Raw.Data[5] = 1065353217;
			// also OverrideActive does nothing on ExtraScale
			ExtraScale.Structured.OverrideActive = 1;
		}

		public bool HasSplines()
		{
			return SplinePoints.Count >= 2;
		}

		public float GetMaxSearchRadius()
		{
			if (Shape.TriggerType == AreaTriggerTypes.Polygon)
			{
				Position center          = new(0.0f, 0.0f);
				float    maxSearchRadius = 0.0f;

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
	}

	public class AreaTriggerSpawn : SpawnData
	{
		public AreaTriggerShapeInfo Shape = new();
		public AreaTriggerId TriggerId;

		public AreaTriggerSpawn() : base(SpawnObjectType.AreaTrigger)
		{
		}
	}

	public struct AreaTriggerAction
	{
		public uint Param;
		public AreaTriggerActionTypes ActionType;
		public AreaTriggerActionUserTypes TargetType;
	}
}