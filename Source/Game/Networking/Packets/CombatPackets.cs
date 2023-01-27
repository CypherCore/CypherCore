// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets
{
	public class AttackSwing : ClientPacket
	{
		public ObjectGuid Victim;

		public AttackSwing(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			Victim = _worldPacket.ReadPackedGuid();
		}
	}

	public class AttackSwingError : ServerPacket
	{
		private AttackSwingErr Reason;

		public AttackSwingError(AttackSwingErr reason = AttackSwingErr.CantAttack) : base(ServerOpcodes.AttackSwingError)
		{
			Reason = reason;
		}

		public override void Write()
		{
			_worldPacket.WriteBits((uint)Reason, 3);
			_worldPacket.FlushBits();
		}
	}

	public class AttackStop : ClientPacket
	{
		public AttackStop(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
		}
	}

	public class AttackStart : ServerPacket
	{
		public ObjectGuid Attacker;
		public ObjectGuid Victim;

		public AttackStart() : base(ServerOpcodes.AttackStart, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(Attacker);
			_worldPacket.WritePackedGuid(Victim);
		}
	}

	public class SAttackStop : ServerPacket
	{
		public ObjectGuid Attacker;
		public bool NowDead;
		public ObjectGuid Victim;

		public SAttackStop(Unit attacker, Unit victim) : base(ServerOpcodes.AttackStop, ConnectionType.Instance)
		{
			Attacker = attacker.GetGUID();

			if (victim)
			{
				Victim  = victim.GetGUID();
				NowDead = !victim.IsAlive(); // using isAlive instead of isDead to catch JUST_DIED death states as well
			}
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(Attacker);
			_worldPacket.WritePackedGuid(Victim);
			_worldPacket.WriteBit(NowDead);
			_worldPacket.FlushBits();
		}
	}

	public class ThreatUpdate : ServerPacket
	{
		public List<ThreatInfo> ThreatList = new();

		public ObjectGuid UnitGUID;

		public ThreatUpdate() : base(ServerOpcodes.ThreatUpdate, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(UnitGUID);
			_worldPacket.WriteInt32(ThreatList.Count);

			foreach (ThreatInfo threatInfo in ThreatList)
			{
				_worldPacket.WritePackedGuid(threatInfo.UnitGUID);
				_worldPacket.WriteInt64(threatInfo.Threat);
			}
		}
	}

	public class HighestThreatUpdate : ServerPacket
	{
		public ObjectGuid HighestThreatGUID;
		public List<ThreatInfo> ThreatList = new();

		public ObjectGuid UnitGUID;

		public HighestThreatUpdate() : base(ServerOpcodes.HighestThreatUpdate, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(UnitGUID);
			_worldPacket.WritePackedGuid(HighestThreatGUID);
			_worldPacket.WriteInt32(ThreatList.Count);

			foreach (ThreatInfo threatInfo in ThreatList)
			{
				_worldPacket.WritePackedGuid(threatInfo.UnitGUID);
				_worldPacket.WriteInt64(threatInfo.Threat);
			}
		}
	}

	public class ThreatRemove : ServerPacket
	{
		public ObjectGuid AboutGUID; // Unit to remove threat from (e.g. player, pet, guardian)
		public ObjectGuid UnitGUID;  // Unit being attacked (e.g. creature, boss)

		public ThreatRemove() : base(ServerOpcodes.ThreatRemove, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(UnitGUID);
			_worldPacket.WritePackedGuid(AboutGUID);
		}
	}

	public class AIReaction : ServerPacket
	{
		public AiReaction Reaction;

		public ObjectGuid UnitGUID;

		public AIReaction() : base(ServerOpcodes.AiReaction, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(UnitGUID);
			_worldPacket.WriteUInt32((uint)Reaction);
		}
	}

	public class CancelCombat : ServerPacket
	{
		public CancelCombat() : base(ServerOpcodes.CancelCombat)
		{
		}

		public override void Write()
		{
		}
	}

	public class PowerUpdate : ServerPacket
	{
		public ObjectGuid Guid;
		public List<PowerUpdatePower> Powers;

		public PowerUpdate() : base(ServerOpcodes.PowerUpdate)
		{
			Powers = new List<PowerUpdatePower>();
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(Guid);
			_worldPacket.WriteInt32(Powers.Count);

			foreach (var power in Powers)
			{
				_worldPacket.WriteInt32(power.Power);
				_worldPacket.WriteUInt8(power.PowerType);
			}
		}
	}

	public class SetSheathed : ClientPacket
	{
		public bool Animate = true;

		public int CurrentSheathState;

		public SetSheathed(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			CurrentSheathState = _worldPacket.ReadInt32();
			Animate            = _worldPacket.HasBit();
		}
	}

	public class CancelAutoRepeat : ServerPacket
	{
		public ObjectGuid Guid;

		public CancelAutoRepeat() : base(ServerOpcodes.CancelAutoRepeat)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(Guid);
		}
	}

	public class HealthUpdate : ServerPacket
	{
		public ObjectGuid Guid;
		public long Health;

		public HealthUpdate() : base(ServerOpcodes.HealthUpdate)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(Guid);
			_worldPacket.WriteInt64(Health);
		}
	}

	public class ThreatClear : ServerPacket
	{
		public ObjectGuid UnitGUID;

		public ThreatClear() : base(ServerOpcodes.ThreatClear)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(UnitGUID);
		}
	}

	internal class PvPCredit : ServerPacket
	{
		public int Honor;

		public int OriginalHonor;
		public uint Rank;
		public ObjectGuid Target;

		public PvPCredit() : base(ServerOpcodes.PvpCredit)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteInt32(OriginalHonor);
			_worldPacket.WriteInt32(Honor);
			_worldPacket.WritePackedGuid(Target);
			_worldPacket.WriteUInt32(Rank);
		}
	}

	internal class BreakTarget : ServerPacket
	{
		public ObjectGuid UnitGUID;

		public BreakTarget() : base(ServerOpcodes.BreakTarget)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(UnitGUID);
		}
	}

	//Structs
	public struct ThreatInfo
	{
		public ObjectGuid UnitGUID;
		public long Threat;
	}

	public struct PowerUpdatePower
	{
		public PowerUpdatePower(int power, byte powerType)
		{
			Power     = power;
			PowerType = powerType;
		}

		public int Power;
		public byte PowerType;
	}
}