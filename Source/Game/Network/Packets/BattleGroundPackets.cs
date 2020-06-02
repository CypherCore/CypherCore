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
using Game.Entities;
using System;
using System.Collections.Generic;

namespace Game.Network.Packets
{
    public class PVPSeason : ServerPacket
    {
        public PVPSeason() : base(ServerOpcodes.PvpSeason) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(MythicPlusSeasonID);
            _worldPacket.WriteInt32(CurrentSeason);
            _worldPacket.WriteInt32(PreviousSeason);
            _worldPacket.WriteInt32(PvpSeasonID);
        }

        public int MythicPlusSeasonID;
        public int PreviousSeason;
        public int CurrentSeason;
        public int PvpSeasonID;
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

    public class PVPLogDataMessage : ServerPacket
    {
        public PVPLogDataMessage() : base(ServerOpcodes.PvpLogData, ConnectionType.Instance) { }

        public override void Write()
        {
            Data.Write(_worldPacket);
        }

        public PVPLogData Data;
    }

    public class BattlefieldStatusNone : ServerPacket
    {
        public BattlefieldStatusNone() : base(ServerOpcodes.BattlefieldStatusNone) { }

        public override void Write()
        {
            Ticket.Write(_worldPacket);
        }

        public RideTicket Ticket = new RideTicket();
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
        public BattlefieldStatusHeader Hdr = new BattlefieldStatusHeader();
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
            _worldPacket.WriteBit(ArenaFaction);
            _worldPacket.WriteBit(LeftEarly);
            _worldPacket.FlushBits();
        }

        public BattlefieldStatusHeader Hdr = new BattlefieldStatusHeader();
        public uint ShutdownTimer;
        public byte ArenaFaction;
        public bool LeftEarly;
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
            _worldPacket.WriteBit(AsGroup);
            _worldPacket.WriteBit(EligibleForMatchmaking);
            _worldPacket.WriteBit(SuspendedQueue);
            _worldPacket.FlushBits();
        }

        public uint AverageWaitTime;
        public BattlefieldStatusHeader Hdr = new BattlefieldStatusHeader();
        public bool AsGroup;
        public bool SuspendedQueue;
        public bool EligibleForMatchmaking;
        public uint WaitTime;
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
        public RideTicket Ticket = new RideTicket();
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

        public Array<ulong> QueueIDs = new Array<ulong>(1);
        public byte Roles;
        public int[] BlacklistMap = new int[2];
    }

    class BattlemasterJoinArena : ClientPacket
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

        public RideTicket Ticket = new RideTicket();
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
        public List<int> Battlefields = new List<int>();    // Players cannot join a specific Battleground instance anymore - this is always empty
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
            _worldPacket.FlushBits();
        }

        public bool WargameArenas;
        public bool RatedArenas;
        public bool WargameBattlegrounds;
        public bool ArenaSkirmish;
        public bool PugBattlegrounds;
        public bool RatedBattlegrounds;
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

        public List<BattlegroundPlayerPosition> FlagCarriers = new List<BattlegroundPlayerPosition>();
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

    class RequestRatedBattlefieldInfo : ClientPacket
    {
        public RequestRatedBattlefieldInfo(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class PVPMatchInit : ServerPacket
    {
        public PVPMatchInit() : base(ServerOpcodes.PvpMatchInit, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(MapID);
            _worldPacket.WriteUInt8((byte)State);
            _worldPacket.WriteInt32((int)StartTime);
            _worldPacket.WriteInt32(Duration);
            _worldPacket.WriteUInt8(ArenaFaction);
            _worldPacket.WriteUInt32(BattlemasterListID);
            _worldPacket.WriteBit(Registered);
            _worldPacket.WriteBit(AffectsRating);
            _worldPacket.FlushBits();
        }

        public enum MatchState
        {
            InProgress = 1,
            Complete = 3,
            Inactive = 4
        }

        public uint MapID;
        public MatchState State = MatchState.Inactive;
        public long StartTime;
        public int Duration;
        public byte ArenaFaction;
        public uint BattlemasterListID;
        public bool Registered;
        public bool AffectsRating;
    }

    class PVPMatchEnd : ServerPacket
    {
        public PVPMatchEnd() : base(ServerOpcodes.PvpMatchEnd, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt8(Winner);
            _worldPacket.WriteInt32(Duration);
            _worldPacket.WriteBit(LogData.HasValue);
            _worldPacket.FlushBits();

            if (LogData.HasValue)
                LogData.Value.Write(_worldPacket);
        }

        public byte Winner;
        public int Duration;
        public Optional<PVPLogData> LogData;
    }

    //Structs
    public class PVPLogData
    {
        public List<PVPMatchPlayerStatistics> Statistics = new List<PVPMatchPlayerStatistics>();
        public Optional<RatingData> Ratings;
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
                data.WriteUInt32(DamageDone);
                data.WriteUInt32(HealingDone);
                data.WriteInt32(Stats.Count);
                data.WriteInt32(PrimaryTalentTree);
                data.WriteInt32(Sex);
                data.WriteUInt32((uint)PlayerRace);
                data.WriteInt32(PlayerClass);
                data.WriteInt32(CreatureID);
                data.WriteInt32(HonorLevel);

                foreach (var pvpStat in Stats)
                    pvpStat.Write(data);

                data.WriteBit(Faction);
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

            public ObjectGuid PlayerGUID;
            public uint Kills;
            public byte Faction;
            public bool IsInWorld;
            public Optional<HonorData> Honor;
            public uint DamageDone;
            public uint HealingDone;
            public Optional<uint> PreMatchRating;
            public Optional<int> RatingChange;
            public Optional<uint> PreMatchMMR;
            public Optional<int> MmrChange;
            public List<PVPMatchPlayerPVPStat> Stats = new List<PVPMatchPlayerPVPStat>();
            public int PrimaryTalentTree;
            public int Sex;
            public Race PlayerRace;
            public int PlayerClass;
            public int CreatureID;
            public int HonorLevel;
        }

        public void Write(WorldPacket data)
        {
            data.WriteBit(Ratings.HasValue);
            data.WriteInt32(Statistics.Count);
            foreach (var count in PlayerCount)
                data.WriteInt8(count);

            if (Ratings.HasValue)
                Ratings.Value.Write(data);

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
        public List<ulong> QueueID = new List<ulong>();
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
}
