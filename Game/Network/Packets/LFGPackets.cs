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
using Game.Entities;
using System.Collections.Generic;

namespace Game.Network.Packets
{
    class LfgDisabled : ServerPacket
    {
        public LfgDisabled() : base(ServerOpcodes.LfgDisabled, ConnectionType.Instance) { }

        public override void Write() { }
    }

    class LfgOfferContinue : ServerPacket
    {
        public LfgOfferContinue() : base(ServerOpcodes.LfgOfferContinue, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Slot);
        }

        public uint Slot;
    }

    class LfgTeleportDenied : ServerPacket
    {
        public LfgTeleportDenied() : base(ServerOpcodes.LfgTeleportDenied, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Reason);
        }

        public LfgTeleportResult Reason;
    }

    class LfgBootPlayer : ServerPacket
    {
        public LfgBootPlayer() : base(ServerOpcodes.LfgBootPlayer, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteBit(Info.VoteInProgress);
            _worldPacket.WriteBit(Info.VotePassed);
            _worldPacket.WriteBit(Info.MyVoteCompleted);
            _worldPacket.WriteBit(Info.MyVote);
            _worldPacket.WriteBits(Info.Reason.Length, 8);
            _worldPacket.FlushBits();

            _worldPacket.WritePackedGuid(Info.Target);
            _worldPacket.WriteUInt32(Info.TotalVotes);
            _worldPacket.WriteUInt32(Info.BootVotes);
            _worldPacket.WriteUInt32(Info.TimeLeft);
            _worldPacket.WriteUInt32(Info.VotesNeeded);
            _worldPacket.WriteString(Info.Reason);
        }

        public LfgBootInfo Info = new LfgBootInfo();
    }

    class RoleChosen : ServerPacket
    {
        public RoleChosen() : base(ServerOpcodes.RoleChosen) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Player);
            _worldPacket.WriteUInt32(RoleMask);
            _worldPacket.WriteBit(Accepted);
            _worldPacket.FlushBits();
        }

        public bool Accepted;
        public LfgRoles RoleMask;
        public ObjectGuid Player;
    }

    class DFGetSystemInfo : ClientPacket
    {
        public DFGetSystemInfo(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Player = _worldPacket.HasBit();
            PartyIndex = _worldPacket.ReadUInt8();
        }

        public bool Player;
        public byte PartyIndex;
    }

    class DFGetJoinStatus : ClientPacket
    {
        public DFGetJoinStatus(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class DFJoin : ClientPacket
    {
        public DFJoin(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            QueueAsGroup = _worldPacket.HasBit();
            var commentLength = _worldPacket.ReadBits<uint>(8);

            PartyIndex = _worldPacket.ReadUInt8();
            Roles = (LfgRoles)_worldPacket.ReadUInt32();
            var slotsCount = _worldPacket.ReadInt32();

            for (var i = 0; i < 3; ++i) // Needs
                Needs[i] = _worldPacket.ReadUInt32();

            Comment = _worldPacket.ReadString(commentLength);

            for (var i = 0; i < slotsCount; ++i) // Slots
                Slots.Add(_worldPacket.ReadUInt32());
        }

        public bool QueueAsGroup;
        public LfgRoles Roles;
        public byte PartyIndex;
        public string Comment = "";
        public List<uint> Slots = new List<uint>();
        public uint[] Needs = new uint[3];
    }

    class DFLeave : ClientPacket
    {
        public DFLeave(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Ticket.Read(_worldPacket);
        }

        public RideTicket Ticket;
    }

    class LFGJoinResult : ServerPacket
    {
        public LFGJoinResult() : base(ServerOpcodes.LfgJoinResult) { }

        public override void Write()
        {
            Ticket.Write(_worldPacket);

            _worldPacket.WriteUInt8(Result);
            _worldPacket.WriteUInt8(ResultDetail);

            _worldPacket.WriteUInt32(BlackList.Count);
            foreach (var blackList in BlackList)
            {
                _worldPacket.WritePackedGuid(blackList.PlayerGuid.Value);

                _worldPacket.WriteUInt32(blackList.BlackListSlots.Count);
                foreach (var blackListSlot in blackList.BlackListSlots)
                {
                    _worldPacket.WriteUInt32(blackListSlot.Slot);
                    _worldPacket.WriteUInt32(blackListSlot.Reason);
                    _worldPacket.WriteInt32(blackListSlot.SubReason1);
                    _worldPacket.WriteInt32(blackListSlot.SubReason2);
                }
            }
        }

        public byte Result;
        public List<LFGBlackList> BlackList = new List<LFGBlackList>();
        public byte ResultDetail;
        public RideTicket Ticket;
    }

    class LFGUpdateStatus : ServerPacket
    {
        public LFGUpdateStatus() : base(ServerOpcodes.LfgUpdateStatus) { }

        public override void Write()
        {
            Ticket.Write(_worldPacket);

            _worldPacket.WriteUInt8(SubType);
            _worldPacket.WriteUInt8(Reason);

            for (int i = 0; i < 3; i++)
                _worldPacket.WriteUInt8(Needs[i]);

            _worldPacket.WriteUInt32(Slots.Count);
            _worldPacket.WriteUInt32(RequestedRoles);
            _worldPacket.WriteUInt32(SuspendedPlayers.Count);

            foreach (var slot in Slots)
                _worldPacket.WriteUInt32(slot);

            foreach (var player in SuspendedPlayers)
                _worldPacket.WritePackedGuid(player);

            _worldPacket.WriteBit(IsParty);
            _worldPacket.WriteBit(NotifyUI);
            _worldPacket.WriteBit(Joined);
            _worldPacket.WriteBit(LfgJoined);
            _worldPacket.WriteBit(Queued);

            _worldPacket.WriteBits(Comment.Length, 8);
            _worldPacket.WriteString(Comment);
        }

        public RideTicket Ticket;
        public byte SubType;
        public byte Reason;
        public byte[] Needs = new byte[3];
        public uint RequestedRoles;

        public List<uint> Slots = new List<uint>();
        public List<ObjectGuid> SuspendedPlayers = new List<ObjectGuid>();

        public bool IsParty;
        public bool NotifyUI;
        public bool Joined;
        public bool LfgJoined;
        public bool Queued;
        public string Comment = "";
    }

    class DFProposalResponse : ClientPacket
    {
        public DFProposalResponse(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Ticket.Read(_worldPacket);
            InstanceID = _worldPacket.ReadUInt64();
            ProposalID = _worldPacket.ReadUInt32();
            Accepted = _worldPacket.HasBit();
        }

        public RideTicket Ticket;
        public ulong InstanceID;
        public uint ProposalID;
        public bool Accepted;
    }

    class LFGProposalUpdate : ServerPacket
    {
        public LFGProposalUpdate() : base(ServerOpcodes.LfgProposalUpdate) { }

        public override void Write()
        {
            Ticket.Write(_worldPacket);

            _worldPacket.WriteUInt64(InstanceID);
            _worldPacket.WriteUInt32(ProposalID);
            _worldPacket.WriteUInt32(Slot);
            _worldPacket.WriteUInt8(State);
            _worldPacket.WriteUInt32(CompletedMask);

            _worldPacket.WriteUInt32(Players.Count);
            foreach (var player in Players)
            {
                _worldPacket.WriteUInt32(player.Roles);

                _worldPacket.WriteBit(player.Me);
                _worldPacket.WriteBit(player.SameParty);
                _worldPacket.WriteBit(player.MyParty);
                _worldPacket.WriteBit(player.Responded);
                _worldPacket.WriteBit(player.Accepted);
                _worldPacket.FlushBits();
            }

            _worldPacket.WriteBit(ValidCompletedMask);
            _worldPacket.WriteBit(ProposalSilent);
            _worldPacket.FlushBits();
        }

        public RideTicket Ticket;
        public ulong InstanceID;
        public uint ProposalID;
        public uint Slot;
        public byte State;
        public uint CompletedMask;
        public List<LFGProposalUpdatePlayer> Players = new List<LFGProposalUpdatePlayer>();
        public bool ValidCompletedMask;
        public bool ProposalSilent;

        public struct LFGProposalUpdatePlayer
        {
            public uint Roles;
            public bool Me;
            public bool SameParty;
            public bool MyParty;
            public bool Responded;
            public bool Accepted;
        }
    }

    class LFGQueueStatus : ServerPacket
    {
        public LFGQueueStatus() : base(ServerOpcodes.LfgQueueStatus) { }

        public override void Write()
        {
            Ticket.Write(_worldPacket);

            _worldPacket.WriteUInt32(Slot);
            _worldPacket.WriteUInt32(AvgWaitTime);
            _worldPacket.WriteUInt32(QueuedTime);

            for (int i = 0; i < 3; i++)
            {
                _worldPacket.WriteUInt32(AvgWaitTimeByRole[i]);
                _worldPacket.WriteUInt8(LastNeeded[i]);
            }

            _worldPacket.WriteUInt32(AvgWaitTimeMe);
        }

        public RideTicket Ticket;
        public uint Slot;
        public uint AvgWaitTime;
        public uint QueuedTime;
        public byte[] LastNeeded = new byte[3];
        public uint[] AvgWaitTimeByRole = new uint[3];
        public uint AvgWaitTimeMe;
    }

    class LfgPlayerInfo : ServerPacket
    {
        public LfgPlayerInfo() : base(ServerOpcodes.LfgPlayerInfo) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Dungeons.Count);

            _worldPacket.WriteBit(BlackList.PlayerGuid.HasValue);
            _worldPacket.WriteUInt32(BlackList.BlackListSlots.Count);

            if (BlackList.PlayerGuid.HasValue)
                _worldPacket.WritePackedGuid(BlackList.PlayerGuid.Value);

            foreach (var blackListSlot in BlackList.BlackListSlots)
            {
                _worldPacket.WriteUInt32(blackListSlot.Slot);
                _worldPacket.WriteUInt32(blackListSlot.Reason);
                _worldPacket.WriteInt32(blackListSlot.SubReason1);
                _worldPacket.WriteInt32(blackListSlot.SubReason2);
            }

            foreach (var dungeonInfo in Dungeons)
            {
                dungeonInfo.Write(_worldPacket);
            }
        }

        public LFGBlackList BlackList;
        public List<LfgPlayerDungeonInfo> Dungeons = new List<LfgPlayerDungeonInfo>();
    }

    class LfgPartyInfo : ServerPacket
    {
        public LfgPartyInfo() : base(ServerOpcodes.LfgPartyInfo) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Players.Count);
            foreach (var blackList in Players)
            {
                _worldPacket.WriteBit(blackList.PlayerGuid.HasValue);
                _worldPacket.WriteUInt32(blackList.BlackListSlots.Count);

                if (blackList.PlayerGuid.HasValue)
                    _worldPacket.WritePackedGuid(blackList.PlayerGuid.Value);

                foreach (var blackListSlot in blackList.BlackListSlots)
                {
                    _worldPacket.WriteUInt32(blackListSlot.Slot);
                    _worldPacket.WriteUInt32(blackListSlot.Reason);
                    _worldPacket.WriteInt32(blackListSlot.SubReason1);
                    _worldPacket.WriteInt32(blackListSlot.SubReason2);
                }
            }
        }

        public List<LFGBlackList> Players = new List<LFGBlackList>();
    }

    class LFGPlayerReward : ServerPacket
    {
        public LFGPlayerReward() : base(ServerOpcodes.LfgPlayerReward) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(ActualSlot); // unconfirmed order
            _worldPacket.WriteUInt32(QueuedSlot); // unconfirmed order
            _worldPacket.WriteUInt32(RewardMoney);
            _worldPacket.WriteUInt32(AddedXP);

            _worldPacket.WriteUInt32(Rewards.Count);
            foreach (var reward in Rewards)
            {
                _worldPacket.WriteUInt32(reward.RewardItem);
                _worldPacket.WriteUInt32(reward.RewardItemQuantity);
                _worldPacket.WriteUInt32(reward.BonusCurrency);
                _worldPacket.WriteBit(reward.IsCurrency);
                _worldPacket.FlushBits();
            }
        }

        public uint ActualSlot;
        public uint QueuedSlot;
        public uint RewardMoney;
        public uint AddedXP;
        public List<LFGPlayerRewards> Rewards = new List<LFGPlayerRewards>();

        public struct LFGPlayerRewards
        {
            public uint RewardItem;
            public uint RewardItemQuantity;
            public int BonusCurrency;
            public bool IsCurrency;
        }
    }

    class LFGRoleCheckUpdate : ServerPacket
    {
        public LFGRoleCheckUpdate() : base(ServerOpcodes.LfgRoleCheckUpdate) { }

        public override void Write()
        {
            _worldPacket.WriteUInt8(PartyIndex);
            _worldPacket.WriteUInt8(RoleCheckStatus);
            _worldPacket.WriteUInt32(JoinSlots.Count);
            _worldPacket.WriteUInt64(BgQueueID);
            _worldPacket.WriteUInt32(0);// ActivityID);//Not in jam
            _worldPacket.WriteUInt32(Members.Count);

            foreach (var slot in JoinSlots)
                _worldPacket.WriteUInt32(slot);

            foreach (var member in Members)
            {
                _worldPacket.WritePackedGuid(member.Guid);
                _worldPacket.WriteUInt32(member.RolesDesired);
                _worldPacket.WriteUInt8(member.Level);
                _worldPacket.WriteBit(member.RoleCheckComplete);
            }

            _worldPacket.WriteBit(IsBeginning);
            _worldPacket.WriteBit(true);// ShowRoleCheck);//Not in jam
        }

        public byte PartyIndex;
        public byte RoleCheckStatus;
        public ulong BgQueueID;
        public List<uint> JoinSlots = new List<uint>();
        public List<LFGRoleCheckUpdateMember> Members = new List<LFGRoleCheckUpdateMember>();
        public bool IsBeginning;

        public class LFGRoleCheckUpdateMember
        {
            public ObjectGuid Guid;
            public bool RoleCheckComplete;
            public uint RolesDesired;
            public byte Level;
        }
    }

    class DFSetComment : ClientPacket
    {
        public DFSetComment(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Ticket.Read(_worldPacket);
            Comment = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(9));
        }

        public RideTicket Ticket;
        public string Comment;
    }

    class DFSetRoles : ClientPacket
    {
        public DFSetRoles(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            RolesDesired = (LfgRoles)_worldPacket.ReadUInt32();
            PartyIndex = _worldPacket.ReadUInt8();
        }

        public LfgRoles RolesDesired;
        public byte PartyIndex;
    }

    class DFBootPlayerVote : ClientPacket
    {
        public DFBootPlayerVote(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Vote = _worldPacket.HasBit();
        }

        public bool Vote;
    }

    class DFTeleport : ClientPacket
    {
        public DFTeleport(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            TeleportOut = _worldPacket.HasBit();
        }

        public bool TeleportOut;
    }

    //Structs
    public class LfgBootInfo
    {
        public bool VoteInProgress;
        public bool VotePassed;
        public bool MyVoteCompleted;
        public bool MyVote;
        public ObjectGuid Target;
        public uint TotalVotes;
        public uint BootVotes;
        public uint TimeLeft;
        public uint VotesNeeded;
        public string Reason;
    }

    public struct RideTicket
    {
        public RideTicket(ObjectGuid requesterGuid, uint id, int type, uint time)
        {
            RequesterGuid = requesterGuid;
            Id = id;
            Type = type;
            Time = time;
        }

        public void Read(WorldPacket data)
        {
            RequesterGuid = data.ReadPackedGuid();
            Id = data.ReadUInt32();
            Type = data.ReadInt32();
            Time = data.ReadUInt32();
        }

        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(RequesterGuid);
            data.WriteUInt32(Id);
            data.WriteInt32(Type);
            data.WriteUInt32(Time);
        }

        public ObjectGuid RequesterGuid;
        public uint Id;
        public int Type;
        public uint Time;
    }

    public class LFGBlackList
    {
        public LFGBlackList(Dictionary<uint, Game.DungeonFinding.LfgLockInfoData> lockInfoData)
        {
            foreach (var lockInfo in lockInfoData)
            {
                var blackListSlot = new LFGBlackList.LFGBlackListSlot();
                blackListSlot.Slot = lockInfo.Key; // Dungeon entry (id + type)
                blackListSlot.Reason = (uint)lockInfo.Value.lockStatus;
                blackListSlot.SubReason1 = lockInfo.Value.requiredItemLevel;
                blackListSlot.SubReason2 = (int)lockInfo.Value.currentItemLevel;

                BlackListSlots.Add(blackListSlot);
            }
        }

        public Optional<ObjectGuid> PlayerGuid;
        public List<LFGBlackListSlot> BlackListSlots = new List<LFGBlackListSlot>();

        public class LFGBlackListSlot
        {
            public uint Slot { get; set; }
            public uint Reason { get; set; }
            public int SubReason1 { get; set; }
            public int SubReason2 { get; set; }
        }
    }

    public class LfgPlayerDungeonInfo
    {
        public void Write(WorldPacket data)
        {
            data.WriteInt32(Slot);
            data.WriteInt32(CompletionQuantity);
            data.WriteInt32(CompletionLimit);
            data.WriteInt32(CompletionCurrencyID);
            data.WriteInt32(SpecificQuantity);
            data.WriteInt32(SpecificLimit);
            data.WriteInt32(OverallQuantity);
            data.WriteInt32(OverallLimit);
            data.WriteInt32(PurseWeeklyQuantity);
            data.WriteInt32(PurseWeeklyLimit);
            data.WriteInt32(PurseQuantity);
            data.WriteInt32(PurseLimit);
            data.WriteInt32(Quantity);
            data.WriteInt32(CompletedMask);

            data.WriteInt32(ShortageReward.Count);

            Rewards.Write(data);

            // ShortageReward
            foreach (var shortage in ShortageReward)
                shortage.Write(data);

            data.WriteBit(FirstReward);
            data.WriteBit(ShortageEligible);
            data.FlushBits();
        }

        public uint Slot;
        public bool FirstReward;
        public int CompletionQuantity;
        public int CompletionLimit;
        public int CompletionCurrencyID;
        public int SpecificQuantity;
        public int SpecificLimit;
        public int OverallQuantity;
        public int OverallLimit;
        public int PurseWeeklyQuantity;
        public int PurseWeeklyLimit;
        public int PurseQuantity;
        public int PurseLimit;
        public int Quantity;
        public uint CompletedMask;
        public bool ShortageEligible;
        public List<LfgPlayerQuestReward> ShortageReward = new List<LfgPlayerQuestReward>();
        public LfgPlayerQuestReward Rewards = new LfgPlayerQuestReward();
    }

    public class LfgPlayerQuestReward
    {
        public void Write(WorldPacket data)
        {
            data.WriteInt32(Mask);
            data.WriteUInt32(RewardMoney);
            data.WriteUInt32(RewardXP);

            data.WriteInt32(Items.Count);
            data.WriteInt32(Currency.Count);
            data.WriteInt32(BonusCurrency.Count);

            // Item
            foreach (var item in Items)
            {
                data.WriteUInt32(item.ItemID);
                data.WriteUInt32(item.Quantity);
            }

            // Currency
            foreach (var currency in Currency)
            {
                data.WriteUInt32(currency.CurrencyID);
                data.WriteUInt32(currency.Quantity);
            }

            // BonusCurrency
            foreach (var currency in BonusCurrency)
            {
                data.WriteUInt32(currency.CurrencyID);
                data.WriteUInt32(currency.Quantity);
            }

            var bit30 = data.WriteBit(false);
            if (bit30)
                data.WriteUInt32(0);

            data.FlushBits();
        }

        public uint Mask;
        public uint RewardMoney;
        public uint RewardXP;
        public List<LfgPlayerQuestRewardItem> Items = new List<LfgPlayerQuestRewardItem>();
        public List<LfgPlayerQuestRewardCurrency> Currency = new List<LfgPlayerQuestRewardCurrency>();
        public List<LfgPlayerQuestRewardCurrency> BonusCurrency = new List<LfgPlayerQuestRewardCurrency>();

        public struct LfgPlayerQuestRewardItem
        {
            public uint ItemID;
            public uint Quantity;
        }

        public struct LfgPlayerQuestRewardCurrency
        {
            public uint CurrencyID;
            public uint Quantity;
        }
    }
}
