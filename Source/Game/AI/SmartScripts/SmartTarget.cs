// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Framework.Constants;

namespace Game.AI
{
    [StructLayout(LayoutKind.Explicit)]
	public struct SmartTarget
	{
		[FieldOffset(0)] public SmartTargets type;

		[FieldOffset(4)] public float x;

		[FieldOffset(8)] public float y;

		[FieldOffset(12)] public float z;

		[FieldOffset(16)] public float o;

		[FieldOffset(20)] public HostilRandom hostilRandom;

		[FieldOffset(20)] public Farthest farthest;

		[FieldOffset(20)] public UnitRange unitRange;

		[FieldOffset(20)] public UnitGUID unitGUID;

		[FieldOffset(20)] public UnitDistance unitDistance;

		[FieldOffset(20)] public PlayerDistance playerDistance;

		[FieldOffset(20)] public PlayerRange playerRange;

		[FieldOffset(20)] public Stored stored;

		[FieldOffset(20)] public GoRange goRange;

		[FieldOffset(20)] public GoGUID goGUID;

		[FieldOffset(20)] public GoDistance goDistance;

		[FieldOffset(20)] public UnitClosest unitClosest;

		[FieldOffset(20)] public GoClosest goClosest;

		[FieldOffset(20)] public ClosestAttackable closestAttackable;

		[FieldOffset(20)] public ClosestFriendly closestFriendly;

		[FieldOffset(20)] public Owner owner;

		[FieldOffset(20)] public Vehicle vehicle;

		[FieldOffset(20)] public ThreatList threatList;

		[FieldOffset(20)] public Raw raw;

		#region Structs

		public struct HostilRandom
		{
			public uint maxDist;
			public uint playerOnly;
			public uint powerType;
		}

		public struct Farthest
		{
			public uint maxDist;
			public uint playerOnly;
			public uint isInLos;
		}

		public struct UnitRange
		{
			public uint creature;
			public uint minDist;
			public uint maxDist;
			public uint maxSize;
		}

		public struct UnitGUID
		{
			public uint dbGuid;
			public uint entry;
		}

		public struct UnitDistance
		{
			public uint creature;
			public uint dist;
			public uint maxSize;
		}

		public struct PlayerDistance
		{
			public uint dist;
		}

		public struct PlayerRange
		{
			public uint minDist;
			public uint maxDist;
		}

		public struct Stored
		{
			public uint id;
		}

		public struct GoRange
		{
			public uint entry;
			public uint minDist;
			public uint maxDist;
			public uint maxSize;
		}

		public struct GoGUID
		{
			public uint dbGuid;
			public uint entry;
		}

		public struct GoDistance
		{
			public uint entry;
			public uint dist;
			public uint maxSize;
		}

		public struct UnitClosest
		{
			public uint entry;
			public uint dist;
			public uint dead;
		}

		public struct GoClosest
		{
			public uint entry;
			public uint dist;
		}

		public struct ClosestAttackable
		{
			public uint maxDist;
			public uint playerOnly;
		}

		public struct ClosestFriendly
		{
			public uint maxDist;
			public uint playerOnly;
		}

		public struct Owner
		{
			public uint useCharmerOrOwner;
		}

		public struct Vehicle
		{
			public uint seatMask;
		}

		public struct ThreatList
		{
			public uint maxDist;
		}

		public struct Raw
		{
			public uint param1;
			public uint param2;
			public uint param3;
			public uint param4;
		}

		#endregion
	}
}