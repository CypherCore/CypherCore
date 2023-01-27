// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets
{
	public class SeasonInfo : ServerPacket
	{
		public int ConquestWeeklyProgressCurrencyID;
		public int CurrentArenaSeason;

		public int MythicPlusDisplaySeasonID;
		public int MythicPlusMilestoneSeasonID;
		public int PreviousArenaSeason;
		public int PvpSeasonID;
		public bool WeeklyRewardChestsEnabled;

		public SeasonInfo() : base(ServerOpcodes.SeasonInfo)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteInt32(MythicPlusDisplaySeasonID);
			_worldPacket.WriteInt32(MythicPlusMilestoneSeasonID);
			_worldPacket.WriteInt32(CurrentArenaSeason);
			_worldPacket.WriteInt32(PreviousArenaSeason);
			_worldPacket.WriteInt32(ConquestWeeklyProgressCurrencyID);
			_worldPacket.WriteInt32(PvpSeasonID);
			_worldPacket.WriteBit(WeeklyRewardChestsEnabled);
			_worldPacket.FlushBits();
		}
	}

	public class AreaSpiritHealerQuery : ClientPacket
	{
		public ObjectGuid HealerGuid;

		public AreaSpiritHealerQuery(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			HealerGuid = _worldPacket.ReadPackedGuid();
		}
	}

	public class AreaSpiritHealerTime : ServerPacket
	{
		public ObjectGuid HealerGuid;
		public uint TimeLeft;

		public AreaSpiritHealerTime() : base(ServerOpcodes.AreaSpiritHealerTime)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(HealerGuid);
			_worldPacket.WriteUInt32(TimeLeft);
		}
	}

	public class AreaSpiritHealerQueue : ClientPacket
	{
		public ObjectGuid HealerGuid;

		public AreaSpiritHealerQueue(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			HealerGuid = _worldPacket.ReadPackedGuid();
		}
	}

	public class HearthAndResurrect : ClientPacket
	{
		public HearthAndResurrect(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
		}
	}

	internal class PVPLogDataRequest : ClientPacket
	{
		public PVPLogDataRequest(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
		}
	}

	public class PVPMatchStatisticsMessage : ServerPacket
	{
		public PVPMatchStatistics Data;

		public PVPMatchStatisticsMessage() : base(ServerOpcodes.PvpMatchStatistics, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			Data.Write(_worldPacket);
		}
	}

	public class BattlefieldStatusNone : ServerPacket
	{
		public RideTicket Ticket = new();

		public BattlefieldStatusNone() : base(ServerOpcodes.BattlefieldStatusNone)
		{
		}

		public override void Write()
		{
			Ticket.Write(_worldPacket);
		}
	}

	public class BattlefieldStatusNeedConfirmation : ServerPacket
	{
		public BattlefieldStatusHeader Hdr = new();
		public uint Mapid;
		public byte Role;

		public uint Timeout;

		public BattlefieldStatusNeedConfirmation() : base(ServerOpcodes.BattlefieldStatusNeedConfirmation)
		{
		}

		public override void Write()
		{
			Hdr.Write(_worldPacket);
			_worldPacket.WriteUInt32(Mapid);
			_worldPacket.WriteUInt32(Timeout);
			_worldPacket.WriteUInt8(Role);
		}
	}

	public class BattlefieldStatusActive : ServerPacket
	{
		public byte ArenaFaction;

		public BattlefieldStatusHeader Hdr = new();
		public bool LeftEarly;
		public uint Mapid;
		public uint ShutdownTimer;
		public uint StartTimer;

		public BattlefieldStatusActive() : base(ServerOpcodes.BattlefieldStatusActive)
		{
		}

		public override void Write()
		{
			Hdr.Write(_worldPacket);
			_worldPacket.WriteUInt32(Mapid);
			_worldPacket.WriteUInt32(ShutdownTimer);
			_worldPacket.WriteUInt32(StartTimer);
			_worldPacket.WriteBit(ArenaFaction != 0);
			_worldPacket.WriteBit(LeftEarly);
			_worldPacket.FlushBits();
		}
	}

	public class BattlefieldStatusQueued : ServerPacket
	{
		public bool AsGroup;

		public uint AverageWaitTime;
		public bool EligibleForMatchmaking;
		public BattlefieldStatusHeader Hdr = new();
		public bool SuspendedQueue;
		public int Unused920;
		public uint WaitTime;

		public BattlefieldStatusQueued() : base(ServerOpcodes.BattlefieldStatusQueued)
		{
		}

		public override void Write()
		{
			Hdr.Write(_worldPacket);
			_worldPacket.WriteUInt32(AverageWaitTime);
			_worldPacket.WriteUInt32(WaitTime);
			_worldPacket.WriteInt32(Unused920);
			_worldPacket.WriteBit(AsGroup);
			_worldPacket.WriteBit(EligibleForMatchmaking);
			_worldPacket.WriteBit(SuspendedQueue);
			_worldPacket.FlushBits();
		}
	}

	public class BattlefieldStatusFailed : ServerPacket
	{
		public ObjectGuid ClientID;

		public ulong QueueID;
		public int Reason;
		public RideTicket Ticket = new();

		public BattlefieldStatusFailed() : base(ServerOpcodes.BattlefieldStatusFailed)
		{
		}

		public override void Write()
		{
			Ticket.Write(_worldPacket);
			_worldPacket.WriteUInt64(QueueID);
			_worldPacket.WriteInt32(Reason);
			_worldPacket.WritePackedGuid(ClientID);
		}
	}

	internal class BattlemasterJoin : ClientPacket
	{
		public int[] BlacklistMap = new int[2];

		public Array<ulong> QueueIDs = new(1);
		public byte Roles;

		public BattlemasterJoin(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			var queueCount = _worldPacket.ReadUInt32();
			Roles           = _worldPacket.ReadUInt8();
			BlacklistMap[0] = _worldPacket.ReadInt32();
			BlacklistMap[1] = _worldPacket.ReadInt32();

			for (var i = 0; i < queueCount; ++i)
				QueueIDs[i] = _worldPacket.ReadUInt64();
		}
	}

	internal class BattlemasterJoinArena : ClientPacket
	{
		public byte Roles;

		public byte TeamSizeIndex;

		public BattlemasterJoinArena(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			TeamSizeIndex = _worldPacket.ReadUInt8();
			Roles         = _worldPacket.ReadUInt8();
		}
	}

	internal class BattlefieldLeave : ClientPacket
	{
		public BattlefieldLeave(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
		}
	}

	internal class BattlefieldPort : ClientPacket
	{
		public bool AcceptedInvite;

		public RideTicket Ticket = new();

		public BattlefieldPort(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			Ticket.Read(_worldPacket);
			AcceptedInvite = _worldPacket.HasBit();
		}
	}

	internal class BattlefieldListRequest : ClientPacket
	{
		public int ListID;

		public BattlefieldListRequest(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			ListID = _worldPacket.ReadInt32();
		}
	}

	internal class BattlefieldList : ServerPacket
	{
		public List<int> Battlefields = new(); // Players cannot join a specific Battleground instance anymore - this is always empty

		public ObjectGuid BattlemasterGuid;
		public int BattlemasterListID;
		public bool HasRandomWinToday;
		public byte MaxLevel;
		public byte MinLevel;
		public bool PvpAnywhere;

		public BattlefieldList() : base(ServerOpcodes.BattlefieldList)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(BattlemasterGuid);
			_worldPacket.WriteInt32(BattlemasterListID);
			_worldPacket.WriteUInt8(MinLevel);
			_worldPacket.WriteUInt8(MaxLevel);
			_worldPacket.WriteInt32(Battlefields.Count);

			foreach (var field in Battlefields)
				_worldPacket.WriteInt32(field);

			_worldPacket.WriteBit(PvpAnywhere);
			_worldPacket.WriteBit(HasRandomWinToday);
			_worldPacket.FlushBits();
		}
	}

	internal class GetPVPOptionsEnabled : ClientPacket
	{
		public GetPVPOptionsEnabled(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
		}
	}

	internal class PVPOptionsEnabled : ServerPacket
	{
		public bool ArenaSkirmish;
		public bool PugBattlegrounds;
		public bool RatedArenas;
		public bool RatedBattlegrounds;

		public bool WargameArenas;
		public bool WargameBattlegrounds;

		public PVPOptionsEnabled() : base(ServerOpcodes.PvpOptionsEnabled)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteBit(RatedBattlegrounds);
			_worldPacket.WriteBit(PugBattlegrounds);
			_worldPacket.WriteBit(WargameBattlegrounds);
			_worldPacket.WriteBit(WargameArenas);
			_worldPacket.WriteBit(RatedArenas);
			_worldPacket.WriteBit(ArenaSkirmish);
			_worldPacket.FlushBits();
		}
	}

	internal class RequestBattlefieldStatus : ClientPacket
	{
		public RequestBattlefieldStatus(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
		}
	}

	internal class ReportPvPPlayerAFK : ClientPacket
	{
		public ObjectGuid Offender;

		public ReportPvPPlayerAFK(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			Offender = _worldPacket.ReadPackedGuid();
		}
	}

	internal class ReportPvPPlayerAFKResult : ServerPacket
	{
		public enum ResultCode
		{
			Success = 0,
			GenericFailure = 1, // there are more error codes but they are impossible to receive without modifying the client
			AFKSystemEnabled = 5,
			AFKSystemDisabled = 6
		}

		public byte NumBlackMarksOnOffender = 0;
		public byte NumPlayersIHaveReported = 0;

		public ObjectGuid Offender;
		public ResultCode Result = ResultCode.GenericFailure;

		public ReportPvPPlayerAFKResult() : base(ServerOpcodes.ReportPvpPlayerAfkResult, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(Offender);
			_worldPacket.WriteUInt8((byte)Result);
			_worldPacket.WriteUInt8(NumBlackMarksOnOffender);
			_worldPacket.WriteUInt8(NumPlayersIHaveReported);
		}
	}

	internal class BattlegroundPlayerPositions : ServerPacket
	{
		public List<BattlegroundPlayerPosition> FlagCarriers = new();

		public BattlegroundPlayerPositions() : base(ServerOpcodes.BattlegroundPlayerPositions, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteInt32(FlagCarriers.Count);

			foreach (var pos in FlagCarriers)
				pos.Write(_worldPacket);
		}
	}

	internal class BattlegroundPlayerJoined : ServerPacket
	{
		public ObjectGuid Guid;

		public BattlegroundPlayerJoined() : base(ServerOpcodes.BattlegroundPlayerJoined, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(Guid);
		}
	}

	internal class BattlegroundPlayerLeft : ServerPacket
	{
		public ObjectGuid Guid;

		public BattlegroundPlayerLeft() : base(ServerOpcodes.BattlegroundPlayerLeft, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(Guid);
		}
	}

	internal class DestroyArenaUnit : ServerPacket
	{
		public ObjectGuid Guid;

		public DestroyArenaUnit() : base(ServerOpcodes.DestroyArenaUnit)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(Guid);
		}
	}

	internal class RequestPVPRewards : ClientPacket
	{
		public RequestPVPRewards(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
		}
	}

	public class RequestPVPRewardsResponse : ServerPacket
	{
		public uint ArenaMaxRewardPointsThisWeek;
		public uint ArenaRewardPoints;
		public uint ArenaRewardPointsThisWeek;
		public uint MaxRewardPointsThisWeek;
		public uint RandomMaxRewardPointsThisWeek;
		public uint RandomRewardPointsThisWeek;
		public uint RatedMaxRewardPointsThisWeek;
		public uint RatedRewardPoints;

		public uint RatedRewardPointsThisWeek;
		public uint RewardPointsThisWeek;

		public RequestPVPRewardsResponse() : base(ServerOpcodes.RequestPvpRewardsResponse)
		{
		}

		public override void Write()
		{
			throw new NotImplementedException();
		}
	}

	internal class RequestRatedPvpInfo : ClientPacket
	{
		public RequestRatedPvpInfo(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
		}
	}

	internal class RatedPvpInfo : ServerPacket
	{
		private BracketInfo[] Bracket = new BracketInfo[6];

		public RatedPvpInfo() : base(ServerOpcodes.RatedPvpInfo)
		{
		}

		public override void Write()
		{
			foreach (BracketInfo bracket in Bracket)
				bracket.Write(_worldPacket);
		}
	}

	internal class PVPMatchInitialize : ServerPacket
	{
		public enum MatchState
		{
			InProgress = 1,
			Complete = 3,
			Inactive = 4
		}

		public bool AffectsRating;
		public byte ArenaFaction;
		public uint BattlemasterListID;
		public int Duration;

		public uint MapID;
		public bool Registered;
		public long StartTime;
		public MatchState State = MatchState.Inactive;

		public PVPMatchInitialize() : base(ServerOpcodes.PvpMatchInitialize, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(MapID);
			_worldPacket.WriteUInt8((byte)State);
			_worldPacket.WriteInt64(StartTime);
			_worldPacket.WriteInt32(Duration);
			_worldPacket.WriteUInt8(ArenaFaction);
			_worldPacket.WriteUInt32(BattlemasterListID);
			_worldPacket.WriteBit(Registered);
			_worldPacket.WriteBit(AffectsRating);
			_worldPacket.FlushBits();
		}
	}

	internal class PVPMatchComplete : ServerPacket
	{
		public int Duration;
		public PVPMatchStatistics LogData;
		public uint SoloShuffleStatus;

		public byte Winner;

		public PVPMatchComplete() : base(ServerOpcodes.PvpMatchComplete, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt8(Winner);
			_worldPacket.WriteInt32(Duration);
			_worldPacket.WriteBit(LogData != null);
			_worldPacket.WriteBits(SoloShuffleStatus, 2);
			_worldPacket.FlushBits();

			if (LogData != null)
				LogData.Write(_worldPacket);
		}
	}

	internal class UpdateCapturePoint : ServerPacket
	{
		public BattlegroundCapturePointInfo CapturePointInfo;

		public UpdateCapturePoint() : base(ServerOpcodes.UpdateCapturePoint)
		{
		}

		public override void Write()
		{
			CapturePointInfo.Write(_worldPacket);
		}
	}

	internal class CapturePointRemoved : ServerPacket
	{
		public ObjectGuid CapturePointGUID;

		public CapturePointRemoved() : base(ServerOpcodes.CapturePointRemoved)
		{
		}

		public CapturePointRemoved(ObjectGuid capturePointGUID) : base(ServerOpcodes.CapturePointRemoved)
		{
			CapturePointGUID = capturePointGUID;
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(CapturePointGUID);
		}
	}

	//Structs
	internal struct BracketInfo
	{
		public int PersonalRating;
		public int Ranking;
		public int SeasonPlayed;
		public int SeasonWon;
		public int Unused1;
		public int Unused2;
		public int WeeklyPlayed;
		public int WeeklyWon;
		public int RoundsSeasonPlayed;
		public int RoundsSeasonWon;
		public int RoundsWeeklyPlayed;
		public int RoundsWeeklyWon;
		public int BestWeeklyRating;
		public int LastWeeksBestRating;
		public int BestSeasonRating;
		public int PvpTierID;
		public int Unused3;
		public int Unused4;
		public int Rank;
		public bool Disqualified;

		public void Write(WorldPacket data)
		{
			data.WriteInt32(PersonalRating);
			data.WriteInt32(Ranking);
			data.WriteInt32(SeasonPlayed);
			data.WriteInt32(SeasonWon);
			data.WriteInt32(Unused1);
			data.WriteInt32(Unused2);
			data.WriteInt32(WeeklyPlayed);
			data.WriteInt32(WeeklyWon);
			data.WriteInt32(RoundsSeasonPlayed);
			data.WriteInt32(RoundsSeasonWon);
			data.WriteInt32(RoundsWeeklyPlayed);
			data.WriteInt32(RoundsWeeklyWon);
			data.WriteInt32(BestWeeklyRating);
			data.WriteInt32(LastWeeksBestRating);
			data.WriteInt32(BestSeasonRating);
			data.WriteInt32(PvpTierID);
			data.WriteInt32(Unused3);
			data.WriteInt32(Unused4);
			data.WriteInt32(Rank);
			data.WriteBit(Disqualified);
			data.FlushBits();
		}
	}

	public class PVPMatchStatistics
	{
		public sbyte[] PlayerCount = new sbyte[2];
		public RatingData Ratings;
		public List<PVPMatchPlayerStatistics> Statistics = new();

		public void Write(WorldPacket data)
		{
			data.WriteBit(Ratings != null);
			data.WriteInt32(Statistics.Count);

			foreach (var count in PlayerCount)
				data.WriteInt8(count);

			if (Ratings != null)
				Ratings.Write(data);

			foreach (var player in Statistics)
				player.Write(data);
		}

		public class RatingData
		{
			public uint[] Postmatch = new uint[2];

			public uint[] Prematch = new uint[2];
			public uint[] PrematchMMR = new uint[2];

			public void Write(WorldPacket data)
			{
				foreach (var id in Prematch)
					data.WriteUInt32(id);

				foreach (var id in Postmatch)
					data.WriteUInt32(id);

				foreach (var id in PrematchMMR)
					data.WriteUInt32(id);
			}
		}

		public struct HonorData
		{
			public void Write(WorldPacket data)
			{
				data.WriteUInt32(HonorKills);
				data.WriteUInt32(Deaths);
				data.WriteUInt32(ContributionPoints);
			}

			public uint HonorKills;
			public uint Deaths;
			public uint ContributionPoints;
		}

		public struct PVPMatchPlayerPVPStat
		{
			public int PvpStatID;
			public uint PvpStatValue;

			public PVPMatchPlayerPVPStat(int pvpStatID, uint pvpStatValue)
			{
				PvpStatID    = pvpStatID;
				PvpStatValue = pvpStatValue;
			}

			public void Write(WorldPacket data)
			{
				data.WriteInt32(PvpStatID);
				data.WriteUInt32(PvpStatValue);
			}
		}

		public class PVPMatchPlayerStatistics
		{
			public int CreatureID;
			public uint DamageDone;
			public byte Faction;
			public uint HealingDone;
			public HonorData? Honor;
			public int HonorLevel;
			public bool IsInWorld;
			public uint Kills;
			public int? MmrChange;
			public int PlayerClass;

			public ObjectGuid PlayerGUID;
			public Race PlayerRace;
			public uint? PreMatchMMR;
			public uint? PreMatchRating;
			public int PrimaryTalentTree;
			public int? RatingChange;
			public int Role;
			public int Sex;
			public List<PVPMatchPlayerPVPStat> Stats = new();

			public void Write(WorldPacket data)
			{
				data.WritePackedGuid(PlayerGUID);
				data.WriteUInt32(Kills);
				data.WriteUInt32(DamageDone);
				data.WriteUInt32(HealingDone);
				data.WriteInt32(Stats.Count);
				data.WriteInt32(PrimaryTalentTree);
				data.WriteInt32(Sex);
				data.WriteUInt32((uint)PlayerRace);
				data.WriteInt32(PlayerClass);
				data.WriteInt32(CreatureID);
				data.WriteInt32(HonorLevel);
				data.WriteInt32(Role);

				foreach (var pvpStat in Stats)
					pvpStat.Write(data);

				data.WriteBit(Faction != 0);
				data.WriteBit(IsInWorld);
				data.WriteBit(Honor.HasValue);
				data.WriteBit(PreMatchRating.HasValue);
				data.WriteBit(RatingChange.HasValue);
				data.WriteBit(PreMatchMMR.HasValue);
				data.WriteBit(MmrChange.HasValue);
				data.FlushBits();

				if (Honor.HasValue)
					Honor.Value.Write(data);

				if (PreMatchRating.HasValue)
					data.WriteUInt32(PreMatchRating.Value);

				if (RatingChange.HasValue)
					data.WriteInt32(RatingChange.Value);

				if (PreMatchMMR.HasValue)
					data.WriteUInt32(PreMatchMMR.Value);

				if (MmrChange.HasValue)
					data.WriteInt32(MmrChange.Value);
			}
		}
	}

	public class BattlefieldStatusHeader
	{
		public uint InstanceID;
		public List<ulong> QueueID = new();
		public byte RangeMax;
		public byte RangeMin;
		public bool RegisteredMatch;
		public byte TeamSize;

		public RideTicket Ticket;
		public bool TournamentRules;

		public void Write(WorldPacket data)
		{
			Ticket.Write(data);
			data.WriteInt32(QueueID.Count);
			data.WriteUInt8(RangeMin);
			data.WriteUInt8(RangeMax);
			data.WriteUInt8(TeamSize);
			data.WriteUInt32(InstanceID);

			foreach (ulong queueID in QueueID)
				data.WriteUInt64(queueID);

			data.WriteBit(RegisteredMatch);
			data.WriteBit(TournamentRules);
			data.FlushBits();
		}
	}

	public struct BattlegroundPlayerPosition
	{
		public void Write(WorldPacket data)
		{
			data.WritePackedGuid(Guid);
			data.WriteVector2(Pos);
			data.WriteInt8(IconID);
			data.WriteInt8(ArenaSlot);
		}

		public ObjectGuid Guid;
		public Vector2 Pos;
		public sbyte IconID;
		public sbyte ArenaSlot;
	}

	internal class BattlegroundCapturePointInfo
	{
		public long CaptureTime;
		public TimeSpan CaptureTotalDuration;
		public ObjectGuid Guid;
		public Vector2 Pos;
		public BattlegroundCapturePointState State = BattlegroundCapturePointState.Neutral;

		public void Write(WorldPacket data)
		{
			data.WritePackedGuid(Guid);
			data.WriteVector2(Pos);
			data.WriteInt8((sbyte)State);

			if (State == BattlegroundCapturePointState.ContestedHorde ||
			    State == BattlegroundCapturePointState.ContestedAlliance)
			{
				data.WriteInt64(CaptureTime);
				data.WriteUInt32((uint)CaptureTotalDuration.TotalMilliseconds);
			}
		}
	}
}