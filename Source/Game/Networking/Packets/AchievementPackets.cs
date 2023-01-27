// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets
{
	public class AllAchievementData : ServerPacket
	{
		public AllAchievements Data = new();

		public AllAchievementData() : base(ServerOpcodes.AllAchievementData, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			Data.Write(_worldPacket);
		}
	}

	internal class AllAccountCriteria : ServerPacket
	{
		public List<CriteriaProgressPkt> Progress = new();

		public AllAccountCriteria() : base(ServerOpcodes.AllAccountCriteria, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteInt32(Progress.Count);

			foreach (var progress in Progress)
				progress.Write(_worldPacket);
		}
	}

	public class RespondInspectAchievements : ServerPacket
	{
		public AllAchievements Data = new();

		public ObjectGuid Player;

		public RespondInspectAchievements() : base(ServerOpcodes.RespondInspectAchievements, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(Player);
			Data.Write(_worldPacket);
		}
	}

	public class CriteriaUpdate : ServerPacket
	{
		public long CreationTime;

		public uint CriteriaID;
		public long CurrentTime;
		public uint ElapsedTime;
		public uint Flags;
		public ObjectGuid PlayerGUID;
		public ulong Quantity;
		public ulong? RafAcceptanceID;

		public CriteriaUpdate() : base(ServerOpcodes.CriteriaUpdate, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(CriteriaID);
			_worldPacket.WriteUInt64(Quantity);
			_worldPacket.WritePackedGuid(PlayerGUID);
			_worldPacket.WriteUInt32(Flags);
			_worldPacket.WritePackedTime(CurrentTime);
			_worldPacket.WriteUInt32(ElapsedTime);
			_worldPacket.WriteInt64(CreationTime);
			_worldPacket.WriteBit(RafAcceptanceID.HasValue);
			_worldPacket.FlushBits();

			if (RafAcceptanceID.HasValue)
				_worldPacket.WriteUInt64(RafAcceptanceID.Value);
		}
	}

	internal class AccountCriteriaUpdate : ServerPacket
	{
		public CriteriaProgressPkt Progress;

		public AccountCriteriaUpdate() : base(ServerOpcodes.AccountCriteriaUpdate)
		{
		}

		public override void Write()
		{
			Progress.Write(_worldPacket);
		}
	}

	public class CriteriaDeleted : ServerPacket
	{
		public uint CriteriaID;

		public CriteriaDeleted() : base(ServerOpcodes.CriteriaDeleted, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(CriteriaID);
		}
	}

	public class AchievementDeleted : ServerPacket
	{
		public uint AchievementID;
		public uint Immunities; // this is just garbage, not used by client

		public AchievementDeleted() : base(ServerOpcodes.AchievementDeleted, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(AchievementID);
			_worldPacket.WriteUInt32(Immunities);
		}
	}

	public class AchievementEarned : ServerPacket
	{
		public uint AchievementID;

		public ObjectGuid Earner;
		public uint EarnerNativeRealm;
		public uint EarnerVirtualRealm;
		public bool Initial;
		public ObjectGuid Sender;
		public long Time;

		public AchievementEarned() : base(ServerOpcodes.AchievementEarned, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(Sender);
			_worldPacket.WritePackedGuid(Earner);
			_worldPacket.WriteUInt32(AchievementID);
			_worldPacket.WritePackedTime(Time);
			_worldPacket.WriteUInt32(EarnerNativeRealm);
			_worldPacket.WriteUInt32(EarnerVirtualRealm);
			_worldPacket.WriteBit(Initial);
			_worldPacket.FlushBits();
		}
	}

	public class BroadcastAchievement : ServerPacket
	{
		public uint AchievementID;
		public bool GuildAchievement;
		public string Name = "";

		public ObjectGuid PlayerGUID;

		public BroadcastAchievement() : base(ServerOpcodes.BroadcastAchievement)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteBits(Name.GetByteCount(), 7);
			_worldPacket.WriteBit(GuildAchievement);
			_worldPacket.WritePackedGuid(PlayerGUID);
			_worldPacket.WriteUInt32(AchievementID);
			_worldPacket.WriteString(Name);
		}
	}

	public class GuildCriteriaUpdate : ServerPacket
	{
		public List<GuildCriteriaProgress> Progress = new();

		public GuildCriteriaUpdate() : base(ServerOpcodes.GuildCriteriaUpdate)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteInt32(Progress.Count);

			foreach (GuildCriteriaProgress progress in Progress)
			{
				_worldPacket.WriteUInt32(progress.CriteriaID);
				_worldPacket.WriteInt64(progress.DateCreated);
				_worldPacket.WriteInt64(progress.DateStarted);
				_worldPacket.WritePackedTime(progress.DateUpdated);
				_worldPacket.WriteUInt32(0); // this is a hack. this is a packed time written as int64 (progress.DateUpdated)
				_worldPacket.WriteUInt64(progress.Quantity);
				_worldPacket.WritePackedGuid(progress.PlayerGUID);
				_worldPacket.WriteInt32(progress.Flags);
			}
		}
	}

	public class GuildCriteriaDeleted : ServerPacket
	{
		public uint CriteriaID;

		public ObjectGuid GuildGUID;

		public GuildCriteriaDeleted() : base(ServerOpcodes.GuildCriteriaDeleted)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(GuildGUID);
			_worldPacket.WriteUInt32(CriteriaID);
		}
	}

	public class GuildSetFocusedAchievement : ClientPacket
	{
		public uint AchievementID;

		public GuildSetFocusedAchievement(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			AchievementID = _worldPacket.ReadUInt32();
		}
	}

	public class GuildAchievementDeleted : ServerPacket
	{
		public uint AchievementID;

		public ObjectGuid GuildGUID;
		public long TimeDeleted;

		public GuildAchievementDeleted() : base(ServerOpcodes.GuildAchievementDeleted)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(GuildGUID);
			_worldPacket.WriteUInt32(AchievementID);
			_worldPacket.WritePackedTime(TimeDeleted);
		}
	}

	public class GuildAchievementEarned : ServerPacket
	{
		public uint AchievementID;
		public ObjectGuid GuildGUID;
		public long TimeEarned;

		public GuildAchievementEarned() : base(ServerOpcodes.GuildAchievementEarned)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(GuildGUID);
			_worldPacket.WriteUInt32(AchievementID);
			_worldPacket.WritePackedTime(TimeEarned);
		}
	}

	public class AllGuildAchievements : ServerPacket
	{
		public List<EarnedAchievement> Earned = new();

		public AllGuildAchievements() : base(ServerOpcodes.AllGuildAchievements)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteInt32(Earned.Count);

			foreach (EarnedAchievement earned in Earned)
				earned.Write(_worldPacket);
		}
	}

	internal class GuildGetAchievementMembers : ClientPacket
	{
		public uint AchievementID;
		public ObjectGuid GuildGUID;

		public ObjectGuid PlayerGUID;

		public GuildGetAchievementMembers(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			PlayerGUID    = _worldPacket.ReadPackedGuid();
			GuildGUID     = _worldPacket.ReadPackedGuid();
			AchievementID = _worldPacket.ReadUInt32();
		}
	}

	internal class GuildAchievementMembers : ServerPacket
	{
		public uint AchievementID;

		public ObjectGuid GuildGUID;
		public List<ObjectGuid> Member = new();

		public GuildAchievementMembers() : base(ServerOpcodes.GuildAchievementMembers)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(GuildGUID);
			_worldPacket.WriteUInt32(AchievementID);
			_worldPacket.WriteInt32(Member.Count);

			foreach (ObjectGuid guid in Member)
				_worldPacket.WritePackedGuid(guid);
		}
	}

	//Structs
	public struct EarnedAchievement
	{
		public void Write(WorldPacket data)
		{
			data.WriteUInt32(Id);
			data.WritePackedTime(Date);
			data.WritePackedGuid(Owner);
			data.WriteUInt32(VirtualRealmAddress);
			data.WriteUInt32(NativeRealmAddress);
		}

		public uint Id;
		public long Date;
		public ObjectGuid Owner;
		public uint VirtualRealmAddress;
		public uint NativeRealmAddress;
	}

	public struct CriteriaProgressPkt
	{
		public void Write(WorldPacket data)
		{
			data.WriteUInt32(Id);
			data.WriteUInt64(Quantity);
			data.WritePackedGuid(Player);
			data.WritePackedTime(Date);
			data.WriteUInt32(TimeFromStart);
			data.WriteUInt32(TimeFromCreate);
			data.WriteBits(Flags, 4);
			data.WriteBit(RafAcceptanceID.HasValue);
			data.FlushBits();

			if (RafAcceptanceID.HasValue)
				data.WriteUInt64(RafAcceptanceID.Value);
		}

		public uint Id;
		public ulong Quantity;
		public ObjectGuid Player;
		public uint Flags;
		public long Date;
		public uint TimeFromStart;
		public uint TimeFromCreate;
		public ulong? RafAcceptanceID;
	}

	public struct GuildCriteriaProgress
	{
		public uint CriteriaID;
		public long DateCreated;
		public long DateStarted;
		public long DateUpdated;
		public ulong Quantity;
		public ObjectGuid PlayerGUID;
		public int Flags;
	}

	public class AllAchievements
	{
		public List<EarnedAchievement> Earned = new();
		public List<CriteriaProgressPkt> Progress = new();

		public void Write(WorldPacket data)
		{
			data.WriteInt32(Earned.Count);
			data.WriteInt32(Progress.Count);

			foreach (EarnedAchievement earned in Earned)
				earned.Write(data);

			foreach (CriteriaProgressPkt progress in Progress)
				progress.Write(data);
		}
	}
}