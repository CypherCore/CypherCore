// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Entities
{
    public class MovementInfo
	{
		public AdvanFlying? AdvFlying;
		private MovementFlag Flags { get; set; }
        private MovementFlag2 Flags2 { get; set; }
        private MovementFlags3 Flags3 { get; set; }
        public MovementInertia? Inertia;
		public JumpInfo Jump;
		public TransportInfo Transport;

		public MovementInfo()
		{
			Guid   = ObjectGuid.Empty;
			Flags  = MovementFlag.None;
			Flags2 = MovementFlag2.None;
			Time   = 0;
			Pitch  = 0.0f;

			Pos = new Position();
			Transport.Reset();
			Jump.Reset();
		}

		public ObjectGuid Guid { get; set; }
		public Position Pos { get; set; }
		public uint Time { get; set; }
		public float Pitch { get; set; }
		public float stepUpStartElevation { get; set; }

		public MovementFlag GetMovementFlags()
		{
			return Flags;
		}

		public void SetMovementFlags(MovementFlag f)
		{
			Flags = f;
		}

		public void AddMovementFlag(MovementFlag f)
		{
			Flags |= f;
		}

		public void RemoveMovementFlag(MovementFlag f)
		{
			Flags &= ~f;
		}

		public bool HasMovementFlag(MovementFlag f)
		{
			return (Flags & f) != 0;
		}

		public MovementFlag2 GetMovementFlags2()
		{
			return Flags2;
		}

		public void SetMovementFlags2(MovementFlag2 f)
		{
			Flags2 = f;
		}

		public void AddMovementFlag2(MovementFlag2 f)
		{
			Flags2 |= f;
		}

		public void RemoveMovementFlag2(MovementFlag2 f)
		{
			Flags2 &= ~f;
		}

		public bool HasMovementFlag2(MovementFlag2 f)
		{
			return (Flags2 & f) != 0;
		}

		public MovementFlags3 GetExtraMovementFlags2()
		{
			return Flags3;
		}

		public void SetExtraMovementFlags2(MovementFlags3 flag)
		{
			Flags3 = flag;
		}

		public void AddExtraMovementFlag2(MovementFlags3 flag)
		{
			Flags3 |= flag;
		}

		public void RemoveExtraMovementFlag2(MovementFlags3 flag)
		{
			Flags3 &= ~flag;
		}

		public bool HasExtraMovementFlag2(MovementFlags3 flag)
		{
			return (Flags3 & flag) != 0;
		}

		public void SetFallTime(uint time)
		{
			Jump.FallTime = time;
		}

		public void ResetTransport()
		{
			Transport.Reset();
		}

		public void ResetJump()
		{
			Jump.Reset();
		}

		public struct TransportInfo
		{
			public void Reset()
			{
				Guid      = ObjectGuid.Empty;
				Pos       = new Position();
				Seat      = -1;
				Time      = 0;
				PrevTime  = 0;
				VehicleId = 0;
			}

			public ObjectGuid Guid;
			public Position Pos;
			public sbyte Seat;
			public uint Time;
			public uint PrevTime;
			public uint VehicleId;
		}

		public struct MovementInertia
		{
			public int Id;
			public Position Force;
			public uint Lifetime;
		}

		public struct JumpInfo
		{
			public void Reset()
			{
				FallTime = 0;
				Zspeed   = SinAngle = CosAngle = XYspeed = 0.0f;
			}

			public uint FallTime;
			public float Zspeed;
			public float SinAngle;
			public float CosAngle;
			public float XYspeed;
		}

		// advflying
		public struct AdvanFlying
		{
			public float ForwardVelocity;
			public float UpVelocity;
		}
	}
}