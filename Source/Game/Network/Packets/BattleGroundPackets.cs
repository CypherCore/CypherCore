/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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
            _worldPacket.WriteUInt32(CurrentSeason);
            _worldPacket.WriteUInt32(PreviousSeason);
        }

        public uint PreviousSeason { get; set; } = 0;
        public uint CurrentSeason { get; set; } = 0;
    }

    public class AreaSpiritHealerQuery : ClientPacket
    {
        public AreaSpiritHealerQuery(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            HealerGuid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid HealerGuid { get; set; }
    }

    public class AreaSpiritHealerQueue : ClientPacket
    {
        public AreaSpiritHealerQueue(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            HealerGuid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid HealerGuid { get; set; }
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

    public class PVPLogData : ServerPacket
    {
        public PVPLogData() : base(ServerOpcodes.PvpLogData, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteBit(Ratings.HasValue);
            _worldPacket.WriteBit(Winner.HasValue);
            _worldPacket.WriteUInt32(Players.Count);
            foreach (var id in PlayerCount)
                _worldPacket.WriteInt8(id);

            if (Ratings.HasValue)
                Ratings.Value.Write(_worldPacket);

            if (Winner.HasValue)
                _worldPacket.WriteUInt8(Winner.Value);

            foreach (PlayerData player in Players)
                player.Write(_worldPacket);
        }

        public Optional<byte> Winner { get; set; }
        public List<PlayerData> Players { get; set; } = new List<PlayerData>();
        public Optional<RatingData> Ratings;
        public sbyte[] PlayerCount { get; set; } = new sbyte[2];

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

            public uint[] Prematch { get; set; } = new uint[2];
            public uint[] Postmatch { get; set; } = new uint[2];
            public uint[] PrematchMMR { get; set; } = new uint[2];
        }

        public struct HonorData
        {
            public void Write(WorldPacket data)
            {
                data.WriteUInt32(HonorKills);
                data.WriteUInt32(Deaths);
                data.WriteUInt32(ContributionPoints);
            }

            public uint HonorKills { get; set; }
            public uint Deaths { get; set; }
            public uint ContributionPoints { get; set; }
        }

        public class PlayerData
        {
            public void Write(WorldPacket data)
            {
                data.WritePackedGuid(PlayerGUID);
                data.WriteUInt32(Kills);
                data.WriteUInt32(DamageDone);
                data.WriteUInt32(HealingDone);
                data.WriteUInt32(Stats.Count);
                data.WriteInt32(PrimaryTalentTree);
                data.WriteInt32(PrimaryTalentTreeNameIndex);
                data.WriteInt32(PlayerRace);
                if (!Stats.Empty())
                    Stats.ForEach(id => data.WriteUInt32(id));

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

            public ObjectGuid PlayerGUID { get; set; }
            public uint Kills { get; set; }
            public byte Faction { get; set; }
            public bool IsInWorld { get; set; }
            public Optional<HonorData> Honor;
            public uint DamageDone { get; set; }
            public uint HealingDone { get; set; }
            public Optional<uint> PreMatchRating { get; set; }
            public Optional<int> RatingChange { get; set; }
            public Optional<uint> PreMatchMMR { get; set; }
            public Optional<int> MmrChange { get; set; }
            public List<uint> Stats { get; set; } = new List<uint>();
            public int PrimaryTalentTree { get; set; }
            public int PrimaryTalentTreeNameIndex;  // controls which name field from ChrSpecialization.dbc will be sent to lua
            public Race PlayerRace { get; set; }
            public uint Prestige { get; set; }
        }
    }

    public class BattlefieldStatusNone : ServerPacket
    {
        public BattlefieldStatusNone() : base(ServerOpcodes.BattlefieldStatusNone) { }

        public override void Write()
        {
            Ticket.Write(_worldPacket);
        }

        public RideTicket Ticket { get; set; } = new RideTicket();
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

        public uint Timeout { get; set; }
        public uint Mapid { get; set; }
        public BattlefieldStatusHeader Hdr;
        public byte Role { get; set; }
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

        public BattlefieldStatusHeader Hdr;
        public uint ShutdownTimer { get; set; }
        public byte ArenaFaction { get; set; }
        public bool LeftEarly { get; set; }
        public uint StartTimer { get; set; }
        public uint Mapid { get; set; }
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

        public uint AverageWaitTime { get; set; }
        public BattlefieldStatusHeader Hdr;
        public bool AsGroup { get; set; }
        public bool SuspendedQueue { get; set; }
        public bool EligibleForMatchmaking { get; set; }
        public uint WaitTime { get; set; }
    }

    public class BattlefieldStatusFailed : ServerPacket
    {
        public BattlefieldStatusFailed() : base(ServerOpcodes.BattlefieldStatusFailed) { }

        public override void Write()
        {
            Ticket.Write(_worldPacket);
            _worldPacket.WriteUInt64(QueueID);
            _worldPacket.WriteUInt32(Reason);
            _worldPacket.WritePackedGuid(ClientID);
        }

        public ulong QueueID { get; set; }
        public ObjectGuid ClientID { get; set; }
        public int Reason { get; set; }
        public RideTicket Ticket { get; set; } = new RideTicket();
    }

    class BattlemasterJoin : ClientPacket
    {
        public BattlemasterJoin(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            QueueID = _worldPacket.ReadUInt64();
            Roles = _worldPacket.ReadUInt8();
            BlacklistMap[0] = _worldPacket.ReadInt32();
            BlacklistMap[1] = _worldPacket.ReadInt32();
            JoinAsGroup = _worldPacket.HasBit();
        }

        public bool JoinAsGroup { get; set; } = false;
        public byte Roles { get; set; } = 0;
        public ulong QueueID { get; set; } = 0;
        public int[] BlacklistMap { get; set; } = new int[2];
    }

    class BattlemasterJoinArena : ClientPacket
    {
        public BattlemasterJoinArena(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            TeamSizeIndex = _worldPacket.ReadUInt8();
            Roles = _worldPacket.ReadUInt8();
        }

        public byte TeamSizeIndex { get; set; }
        public byte Roles { get; set; }
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

        public RideTicket Ticket { get; set; } = new RideTicket();
        public bool AcceptedInvite { get; set; }
    }

    class BattlefieldListRequest : ClientPacket
    {
        public BattlefieldListRequest(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ListID = _worldPacket.ReadInt32();
        }

        public int ListID { get; set; }
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
            _worldPacket.WriteUInt32(Battlefields.Count);
            if (!Battlefields.Empty())
                Battlefields.ForEach(field => _worldPacket.WriteInt32(field));

            _worldPacket.WriteBit(PvpAnywhere);
            _worldPacket.WriteBit(HasRandomWinToday);
            _worldPacket.FlushBits();
        }

        public ObjectGuid BattlemasterGuid { get; set; }
        public int BattlemasterListID { get; set; }
        public byte MinLevel { get; set; }
        public byte MaxLevel { get; set; }
        public List<int> Battlefields { get; set; } = new List<int>();    // Players cannot join a specific Battleground instance anymore - this is always empty
        public bool PvpAnywhere { get; set; }
        public bool HasRandomWinToday { get; set; }
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

        public bool WargameArenas { get; set; }
        public bool RatedArenas { get; set; }
        public bool WargameBattlegrounds { get; set; }
        public bool ArenaSkirmish { get; set; }
        public bool PugBattlegrounds { get; set; }
        public bool RatedBattlegrounds { get; set; }
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

        public ObjectGuid Offender { get; set; }
    }

    class ReportPvPPlayerAFKResult : ServerPacket
    {
        public ReportPvPPlayerAFKResult() : base(ServerOpcodes.ReportPvpPlayerAfkResult, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Offender);
            _worldPacket.WriteUInt8(Result);
            _worldPacket.WriteUInt8(NumBlackMarksOnOffender);
            _worldPacket.WriteUInt8(NumPlayersIHaveReported);
        }

        public ObjectGuid Offender { get; set; }
        public byte NumPlayersIHaveReported { get; set; } = 0;
        public byte NumBlackMarksOnOffender { get; set; } = 0;
        public ResultCode Result { get; set; } = ResultCode.GenericFailure;

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
            _worldPacket .WriteUInt32(FlagCarriers.Count);
            FlagCarriers.ForEach(pos => pos.Write(_worldPacket));
        }

        public List<BattlegroundPlayerPosition> FlagCarriers { get; set; } = new List<BattlegroundPlayerPosition>();
    }

    class BattlegroundPlayerJoined : ServerPacket
    {
        public BattlegroundPlayerJoined() : base(ServerOpcodes.BattlegroundPlayerJoined, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
        }

        public ObjectGuid Guid { get; set; }
    }

    class BattlegroundPlayerLeft : ServerPacket
    {
        public BattlegroundPlayerLeft() : base(ServerOpcodes.BattlegroundPlayerLeft, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
        }

        public ObjectGuid Guid { get; set; }
    }

    class DestroyArenaUnit : ServerPacket
    {
        public DestroyArenaUnit() : base(ServerOpcodes.DestroyArenaUnit) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
        }

        public ObjectGuid Guid { get; set; }
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

        public uint RatedRewardPointsThisWeek { get; set; }
        public uint ArenaRewardPointsThisWeek { get; set; }
        public uint RatedMaxRewardPointsThisWeek { get; set; }
        public uint ArenaRewardPoints { get; set; }
        public uint RandomRewardPointsThisWeek { get; set; }
        public uint ArenaMaxRewardPointsThisWeek { get; set; }
        public uint RatedRewardPoints { get; set; }
        public uint MaxRewardPointsThisWeek { get; set; }
        public uint RewardPointsThisWeek { get; set; }
        public uint RandomMaxRewardPointsThisWeek { get; set; }
    }

    class RequestRatedBattlefieldInfo : ClientPacket
    {
        public RequestRatedBattlefieldInfo(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class ArenaError : ServerPacket
    {
        public ArenaError() : base(ServerOpcodes.ArenaError) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(ErrorType);
            if (ErrorType == ArenaErrorType.NoTeam)
                _worldPacket.WriteUInt8(TeamSize);
        }

        public ArenaErrorType ErrorType { get; set; }
        public byte TeamSize { get; set; }
    }

    //Structs
    public struct BattlefieldStatusHeader
    {
        public void Write(WorldPacket data)
        {
            Ticket.Write(data);
            data.WriteUInt64(QueueID);
            data.WriteUInt8(RangeMin);
            data.WriteUInt8(RangeMax);
            data.WriteUInt8(TeamSize);
            data.WriteUInt32(InstanceID);
            data.WriteBit(RegisteredMatch);
            data.WriteBit(TournamentRules);
            data.FlushBits();
        }

        public RideTicket Ticket { get; set; }
        public ulong QueueID { get; set; }
        public byte RangeMin { get; set; }
        public byte RangeMax { get; set; }
        public byte TeamSize { get; set; }
        public uint InstanceID { get; set; }
        public bool RegisteredMatch { get; set; }
        public bool TournamentRules { get; set; }
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

        public ObjectGuid Guid { get; set; }
        public Vector2 Pos;
        public sbyte IconID { get; set; }
        public sbyte ArenaSlot { get; set; }
    }
}
