// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Game.Networking.Packets
{
    public class SeasonInfo : ServerPacket
    {
        public SeasonInfo() : base(ServerOpcodes.SeasonInfo) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(MythicPlusDisplaySeasonID);
            _worldPacket.WriteInt32(MythicPlusMilestoneSeasonID);
            _worldPacket.WriteInt32(CurrentArenaSeason);
            _worldPacket.WriteInt32(PreviousArenaSeason);
            _worldPacket.WriteInt32(ConquestWeeklyProgressCurrencyID);
            _worldPacket.WriteInt32(PvpSeasonID);
            _worldPacket.WriteInt32(Unknown1027_1);
            _worldPacket.WriteBit(WeeklyRewardChestsEnabled);
            _worldPacket.WriteBit(CurrentArenaSeasonUsesTeams);
            _worldPacket.WriteBit(PreviousArenaSeasonUsesTeams);
            _worldPacket.FlushBits();
        }

        public int MythicPlusDisplaySeasonID;
        public int MythicPlusMilestoneSeasonID;
        public int PreviousArenaSeason;
        public int CurrentArenaSeason;
        public int PvpSeasonID;
        public int ConquestWeeklyProgressCurrencyID;
        public int Unknown1027_1;
        public bool WeeklyRewardChestsEnabled;
        public bool CurrentArenaSeasonUsesTeams;
        public bool PreviousArenaSeasonUsesTeams;
    }

    public class AreaSpiritHealerQuery : ClientPacket
    {
        public AreaSpiritHealerQuery(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            HealerGuid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid HealerGuid;
    }

    public class AreaSpiritHealerTime : ServerPacket
    {
        public AreaSpiritHealerTime() : base(ServerOpcodes.AreaSpiritHealerTime) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(HealerGuid);
            _worldPacket.WriteUInt32(TimeLeft);
        }

        public ObjectGuid HealerGuid;
        public uint TimeLeft;
    }

    public class AreaSpiritHealerQueue : ClientPacket
    {
        public AreaSpiritHealerQueue(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            HealerGuid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid HealerGuid;
    }

    public class HearthAndResurrect : ClientPacket
    {
        public HearthAndResurrect(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class PVPLogDataRequest : ClientPacket
    {
        public PVPLogDataRequest(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class PVPMatchStatisticsMessage : ServerPacket
    {
        public PVPMatchStatisticsMessage() : base(ServerOpcodes.PvpMatchStatistics, ConnectionType.Instance) { }

        public override void Write()
        {
            Data.Write(_worldPacket);
        }

        public PVPMatchStatistics Data;
    }

    public class BattlefieldStatusNone : ServerPacket
    {
        public BattlefieldStatusNone() : base(ServerOpcodes.BattlefieldStatusNone) { }

        public override void Write()
        {
            Ticket.Write(_worldPacket);
        }

        public RideTicket Ticket = new();
    }

    public class BattlefieldStatusNeedConfirmation : ServerPacket
    {
        public BattlefieldStatusNeedConfirmation() : base(ServerOpcodes.BattlefieldStatusNeedConfirmation) { }

        public override void Write()
        {
            Hdr.Write(_worldPacket);
            _worldPacket.WriteUInt32(Mapid);
            _worldPacket.WriteUInt32(Timeout);
            _worldPacket.WriteUInt8(Role);
        }

        public uint Timeout;
        public uint Mapid;
        public BattlefieldStatusHeader Hdr = new();
        public byte Role;
    }

    public class BattlefieldStatusActive : ServerPacket
    {
        public BattlefieldStatusActive() : base(ServerOpcodes.BattlefieldStatusActive) { }

        public override void Write()
        {
            Hdr.Write(_worldPacket);
            _worldPacket.WriteUInt32(Mapid);
            _worldPacket.WriteUInt32(ShutdownTimer);
            _worldPacket.WriteUInt32(StartTimer);
            _worldPacket.WriteInt8(ArenaFaction);
            _worldPacket.WriteBit(LeftEarly);
            _worldPacket.WriteBit(Brawl);
            _worldPacket.FlushBits();
        }

        public BattlefieldStatusHeader Hdr = new();
        public uint ShutdownTimer;
        public sbyte ArenaFaction;
        public bool LeftEarly;
        public bool Brawl;
        public uint StartTimer;
        public uint Mapid;
    }

    public class BattlefieldStatusQueued : ServerPacket
    {
        public BattlefieldStatusQueued() : base(ServerOpcodes.BattlefieldStatusQueued) { }

        public override void Write()
        {
            Hdr.Write(_worldPacket);
            _worldPacket.WriteUInt32(AverageWaitTime);
            _worldPacket.WriteUInt32(WaitTime);
            _worldPacket.WriteInt32(SpecSelected);
            _worldPacket.WriteBit(AsGroup);
            _worldPacket.WriteBit(EligibleForMatchmaking);
            _worldPacket.WriteBit(SuspendedQueue);
            _worldPacket.FlushBits();
        }

        public uint AverageWaitTime;
        public BattlefieldStatusHeader Hdr = new();
        public bool AsGroup;
        public bool SuspendedQueue;
        public bool EligibleForMatchmaking;
        public uint WaitTime;
        public int SpecSelected;
    }

    public class BattlefieldStatusFailed : ServerPacket
    {
        public BattlefieldStatusFailed() : base(ServerOpcodes.BattlefieldStatusFailed) { }

        public override void Write()
        {
            Ticket.Write(_worldPacket);
            _worldPacket.WriteUInt64(QueueID);
            _worldPacket.WriteInt32(Reason);
            _worldPacket.WritePackedGuid(ClientID);
        }

        public ulong QueueID;
        public ObjectGuid ClientID;
        public int Reason;
        public RideTicket Ticket = new();
    }

    class BattlemasterJoin : ClientPacket
    {
        public BattlemasterJoin(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            var queueCount = _worldPacket.ReadUInt32();
            Roles = _worldPacket.ReadUInt8();
            BlacklistMap[0] = _worldPacket.ReadInt32();
            BlacklistMap[1] = _worldPacket.ReadInt32();

            for (var i = 0; i < queueCount; ++i)
                QueueIDs[i] = _worldPacket.ReadUInt64();
        }

        public Array<ulong> QueueIDs = new(1);
        public byte Roles;
        public int[] BlacklistMap = new int[2];
    }

    public class BattlemasterJoinArena : ClientPacket
    {
        public BattlemasterJoinArena(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            TeamSizeIndex = _worldPacket.ReadUInt8();
            Roles = _worldPacket.ReadUInt8();
        }

        public byte TeamSizeIndex;
        public byte Roles;
    }

    class BattlefieldLeave : ClientPacket
    {
        public BattlefieldLeave(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class BattlefieldPort : ClientPacket
    {
        public BattlefieldPort(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Ticket.Read(_worldPacket);
            AcceptedInvite = _worldPacket.HasBit();
        }

        public RideTicket Ticket = new();
        public bool AcceptedInvite;
    }

    class BattlefieldListRequest : ClientPacket
    {
        public BattlefieldListRequest(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ListID = _worldPacket.ReadInt32();
        }

        public int ListID;
    }

    class BattlefieldList : ServerPacket
    {
        public BattlefieldList() : base(ServerOpcodes.BattlefieldList) { }

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

        public ObjectGuid BattlemasterGuid;
        public int BattlemasterListID;
        public byte MinLevel;
        public byte MaxLevel;
        public List<int> Battlefields = new();    // Players cannot join a specific Battleground instance anymore - this is always empty
        public bool PvpAnywhere;
        public bool HasRandomWinToday;
    }

    class GetPVPOptionsEnabled : ClientPacket
    {
        public GetPVPOptionsEnabled(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class PVPOptionsEnabled : ServerPacket
    {
        public PVPOptionsEnabled() : base(ServerOpcodes.PvpOptionsEnabled) { }

        public override void Write()
        {
            _worldPacket.WriteBit(RatedBattlegrounds);
            _worldPacket.WriteBit(PugBattlegrounds);
            _worldPacket.WriteBit(WargameBattlegrounds);
            _worldPacket.WriteBit(WargameArenas);
            _worldPacket.WriteBit(RatedArenas);
            _worldPacket.WriteBit(ArenaSkirmish);
            _worldPacket.WriteBit(SoloShuffle);
            _worldPacket.WriteBit(RatedSoloShuffle);
            _worldPacket.WriteBit(BattlegroundBlitz);
            _worldPacket.WriteBit(RatedBattlegroundBlitz);
            _worldPacket.FlushBits();
        }

        public bool RatedBattlegrounds;
        public bool PugBattlegrounds;
        public bool WargameBattlegrounds;
        public bool WargameArenas;
        public bool RatedArenas;
        public bool ArenaSkirmish;
        public bool SoloShuffle;
        public bool RatedSoloShuffle;
        public bool BattlegroundBlitz;
        public bool RatedBattlegroundBlitz; // solo rbg
    }

    class RequestBattlefieldStatus : ClientPacket
    {
        public RequestBattlefieldStatus(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class ReportPvPPlayerAFK : ClientPacket
    {
        public ReportPvPPlayerAFK(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Offender = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Offender;
    }

    class ReportPvPPlayerAFKResult : ServerPacket
    {
        public ReportPvPPlayerAFKResult() : base(ServerOpcodes.ReportPvpPlayerAfkResult, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Offender);
            _worldPacket.WriteUInt8((byte)Result);
            _worldPacket.WriteUInt8(NumBlackMarksOnOffender);
            _worldPacket.WriteUInt8(NumPlayersIHaveReported);
        }

        public ObjectGuid Offender;
        public byte NumPlayersIHaveReported = 0;
        public byte NumBlackMarksOnOffender = 0;
        public ResultCode Result = ResultCode.GenericFailure;

        public enum ResultCode
        {
            Success = 0,
            GenericFailure = 1, // there are more error codes but they are impossible to receive without modifying the client
            AFKSystemEnabled = 5,
            AFKSystemDisabled = 6
        }
    }

    class BattlegroundPlayerPositions : ServerPacket
    {
        public BattlegroundPlayerPositions() : base(ServerOpcodes.BattlegroundPlayerPositions, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(FlagCarriers.Count);
            foreach (var pos in FlagCarriers)
                pos.Write(_worldPacket);
        }

        public List<BattlegroundPlayerPosition> FlagCarriers = new();
    }

    class BattlegroundPlayerJoined : ServerPacket
    {
        public BattlegroundPlayerJoined() : base(ServerOpcodes.BattlegroundPlayerJoined, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
        }

        public ObjectGuid Guid;
    }

    class BattlegroundPlayerLeft : ServerPacket
    {
        public BattlegroundPlayerLeft() : base(ServerOpcodes.BattlegroundPlayerLeft, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
        }

        public ObjectGuid Guid;
    }

    class DestroyArenaUnit : ServerPacket
    {
        public DestroyArenaUnit() : base(ServerOpcodes.DestroyArenaUnit) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
        }

        public ObjectGuid Guid;
    }

    class RequestPVPRewards : ClientPacket
    {
        public RequestPVPRewards(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class RequestPVPRewardsResponse : ServerPacket
    {
        public RequestPVPRewardsResponse() : base(ServerOpcodes.RequestPvpRewardsResponse) { }

        public override void Write()
        {
            throw new NotImplementedException();
        }

        public uint RatedRewardPointsThisWeek;
        public uint ArenaRewardPointsThisWeek;
        public uint RatedMaxRewardPointsThisWeek;
        public uint ArenaRewardPoints;
        public uint RandomRewardPointsThisWeek;
        public uint ArenaMaxRewardPointsThisWeek;
        public uint RatedRewardPoints;
        public uint MaxRewardPointsThisWeek;
        public uint RewardPointsThisWeek;
        public uint RandomMaxRewardPointsThisWeek;
    }

    class RequestRatedPvpInfo : ClientPacket
    {
        public RequestRatedPvpInfo(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class RatedPvpInfo : ServerPacket
    {
        public RatedPvpInfo() : base(ServerOpcodes.RatedPvpInfo) { }

        public override void Write()
        {
            foreach (BracketInfo bracket in Bracket)
                bracket.Write(_worldPacket);
        }

        BracketInfo[] Bracket = new BracketInfo[9];
    }

    class PVPMatchInitialize : ServerPacket
    {
        public PVPMatchInitialize() : base(ServerOpcodes.PvpMatchInitialize, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(MapID);
            _worldPacket.WriteUInt8((byte)State);
            _worldPacket.WriteInt64(StartTime);
            _worldPacket.WriteInt64(Duration);
            _worldPacket.WriteUInt8(ArenaFaction);
            _worldPacket.WriteUInt32(BattlemasterListID);
            _worldPacket.WriteBit(Registered);
            _worldPacket.WriteBit(AffectsRating);
            _worldPacket.WriteBit(DeserterPenalty != null);
            _worldPacket.FlushBits();

            if (DeserterPenalty != null)
                DeserterPenalty.Write(_worldPacket);
        }

        public uint MapID;
        public PVPMatchState State = PVPMatchState.Inactive;
        public long StartTime;
        public long Duration;
        public RatedMatchDeserterPenalty DeserterPenalty;
        public byte ArenaFaction;
        public uint BattlemasterListID;
        public bool Registered;
        public bool AffectsRating;
    }

    class PVPMatchSetState : ServerPacket
    {
        public PVPMatchSetState(PVPMatchState state) : base(ServerOpcodes.PvpMatchSetState)
        {
            State = state;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt8((byte)State);
        }

        PVPMatchState State;
    }

    class PVPMatchComplete : ServerPacket
    {
        public PVPMatchComplete() : base(ServerOpcodes.PvpMatchComplete, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Winner);
            _worldPacket.WriteInt64(Duration);
            _worldPacket.WriteBit(LogData != null);
            _worldPacket.WriteBits(SoloShuffleStatus, 2);
            _worldPacket.FlushBits();

            if (LogData != null)
                LogData.Write(_worldPacket);
        }

        public int Winner;
        public long Duration;
        public PVPMatchStatistics LogData;
        public uint SoloShuffleStatus;
    }

    class UpdateCapturePoint : ServerPacket
    {
        public BattlegroundCapturePointInfo CapturePointInfo;

        public UpdateCapturePoint() : base(ServerOpcodes.UpdateCapturePoint) { }

        public override void Write()
        {
            CapturePointInfo.Write(_worldPacket);
        }
    }

    class CapturePointRemoved : ServerPacket
    {
        public ObjectGuid CapturePointGUID;

        public CapturePointRemoved() : base(ServerOpcodes.CapturePointRemoved) { }
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
    struct BracketInfo
    {
        public int PersonalRating;
        public int Ranking;
        public int SeasonPlayed;
        public int SeasonWon;
        public int SeasonFactionPlayed;
        public int SeasonFactionWon;
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
        public int SeasonPvpTier;
        public int BestWeeklyPvpTier;
        public byte BestSeasonPvpTierEnum;
        public bool Disqualified;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(PersonalRating);
            data.WriteInt32(Ranking);
            data.WriteInt32(SeasonPlayed);
            data.WriteInt32(SeasonWon);
            data.WriteInt32(SeasonFactionPlayed);
            data.WriteInt32(SeasonFactionWon);
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
            data.WriteInt32(SeasonPvpTier);
            data.WriteInt32(BestWeeklyPvpTier);
            data.WriteUInt8(BestSeasonPvpTierEnum);
            data.WriteBit(Disqualified);
            data.FlushBits();
        }
    }

    class RatedMatchDeserterPenalty
    {
        public int PersonalRatingChange;
        public int QueuePenaltySpellID;
        public int QueuePenaltyDuration;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(PersonalRatingChange);
            data.WriteInt32(QueuePenaltySpellID);
            data.WriteInt32(QueuePenaltyDuration);
        }
    }

    public class PVPMatchStatistics
    {
        public List<PVPMatchPlayerStatistics> Statistics = new();
        public RatingData Ratings;
        public sbyte[] PlayerCount = new sbyte[2];

        public class RatingData
        {
            public void Write(WorldPacket data)
            {
                foreach (var id in Prematch)
                    data.WriteUInt32(id);

                foreach (var id in Postmatch)
                    data.WriteUInt32(id);

                foreach (var id in PrematchMMR)
                    data.WriteUInt32(id);
            }

            public uint[] Prematch = new uint[2];
            public uint[] Postmatch = new uint[2];
            public uint[] PrematchMMR = new uint[2];
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
                PvpStatID = pvpStatID;
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
            public void Write(WorldPacket data)
            {
                data.WritePackedGuid(PlayerGUID);
                data.WriteUInt32(Kills);
                data.WriteInt32(Faction);
                data.WriteUInt32(DamageDone);
                data.WriteUInt32(HealingDone);
                data.WriteInt32(Stats.Count);
                data.WriteInt32(PrimaryTalentTree);
                data.WriteInt8(Sex);
                data.WriteInt8(PlayerRace);
                data.WriteInt8(PlayerClass);
                data.WriteInt32(CreatureID);
                data.WriteInt32(HonorLevel);
                data.WriteInt32(Role);

                foreach (var pvpStat in Stats)
                    pvpStat.Write(data);

                data.WriteBit(IsInWorld);
                data.WriteBit(Honor.HasValue);
                data.WriteBit(PreMatchRating.HasValue);
                data.WriteBit(RatingChange.HasValue);
                data.WriteBit(PreMatchMMR.HasValue);
                data.WriteBit(MmrChange.HasValue);
                data.WriteBit(PostMatchMMR.HasValue);
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

                if (PostMatchMMR.HasValue)
                    data.WriteUInt32(PostMatchMMR.Value);
            }

            public ObjectGuid PlayerGUID;
            public uint Kills;
            public int Faction;
            public bool IsInWorld;
            public HonorData? Honor;
            public uint DamageDone;
            public uint HealingDone;
            public uint? PreMatchRating;
            public int? RatingChange;
            public uint? PreMatchMMR;
            public int? MmrChange;
            public uint? PostMatchMMR;
            public List<PVPMatchPlayerPVPStat> Stats = new();
            public int PrimaryTalentTree;
            public sbyte Sex;
            public sbyte PlayerRace;
            public sbyte PlayerClass;
            public int CreatureID;
            public int HonorLevel;
            public int Role;
        }

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
    }

    public class BattlefieldStatusHeader
    {
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

        public RideTicket Ticket;
        public List<ulong> QueueID = new();
        public byte RangeMin;
        public byte RangeMax;
        public byte TeamSize;
        public uint InstanceID;
        public bool RegisteredMatch;
        public bool TournamentRules;
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

    class BattlegroundCapturePointInfo
    {
        public ObjectGuid Guid;
        public Vector2 Pos;
        public BattlegroundCapturePointState State = BattlegroundCapturePointState.Neutral;
        public long CaptureTime;
        public TimeSpan CaptureTotalDuration;

        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(Guid);
            data.WriteVector2(Pos);
            data.WriteInt8((sbyte)State);

            if (State == BattlegroundCapturePointState.ContestedHorde || State == BattlegroundCapturePointState.ContestedAlliance)
            {
                data.WriteInt64(CaptureTime);
                data.WriteUInt32((uint)CaptureTotalDuration.TotalMilliseconds);
            }
        }
    }
}
