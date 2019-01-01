/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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
using System;
using System.Collections.Generic;

namespace Game.Network.Packets
{
    class DFJoin : ClientPacket
    {
        public DFJoin(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            QueueAsGroup = _worldPacket.HasBit();
            Unknown = _worldPacket.HasBit();
            PartyIndex = _worldPacket.ReadUInt8();
            Roles = (LfgRoles)_worldPacket.ReadUInt32();

            var slotsCount = _worldPacket.ReadInt32();
            for (var i = 0; i < slotsCount; ++i) // Slots
                Slots.Add(_worldPacket.ReadUInt32());
        }

        public bool QueueAsGroup;
        bool Unknown;       // Always false in 7.2.5
        public byte PartyIndex;
        public LfgRoles Roles;
        public List<uint> Slots = new List<uint>();
    }

    class DFLeave : ClientPacket
    {
        public DFLeave(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Ticket.Read(_worldPacket);
        }

        public RideTicket Ticket = new RideTicket();
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

        public RideTicket Ticket = new RideTicket();
        public ulong InstanceID;
        public uint ProposalID;
        public bool Accepted;
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

    class DFGetSystemInfo : ClientPacket
    {
        public DFGetSystemInfo(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Player = _worldPacket.HasBit();
            PartyIndex = _worldPacket.ReadUInt8();
        }

        public byte PartyIndex;
        public bool Player;
    }

    class DFGetJoinStatus : ClientPacket
    {
        public DFGetJoinStatus(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class LfgPlayerInfo : ServerPacket
    {
        public LfgPlayerInfo() : base(ServerOpcodes.LfgPlayerInfo, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Dungeons.Count);
            BlackList.Write(_worldPacket);

            foreach (var dungeonInfo in Dungeons)
                dungeonInfo.Write(_worldPacket);
        }

        public LFGBlackList BlackList = new LFGBlackList();
        public List<LfgPlayerDungeonInfo> Dungeons = new List<LfgPlayerDungeonInfo>();
    }

    class LfgPartyInfo : ServerPacket
    {
        public LfgPartyInfo() : base(ServerOpcodes.LfgPartyInfo, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Player.Count);
            foreach (var blackList in Player)
                blackList.Write(_worldPacket);
        }

        public List<LFGBlackList> Player = new List<LFGBlackList>();
    }

    class LFGUpdateStatus : ServerPacket
    {
        public LFGUpdateStatus() : base(ServerOpcodes.LfgUpdateStatus) { }

        public override void Write()
        {
            Ticket.Write(_worldPacket);

            _worldPacket.WriteUInt8(SubType);
            _worldPacket.WriteUInt8(Reason);
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
            _worldPacket.WriteBit(Unused);
            _worldPacket.FlushBits();
        }

        public RideTicket Ticket = new RideTicket();
        public byte SubType;
        public byte Reason;
        public List<uint> Slots = new List<uint>();
        public uint RequestedRoles;
        public List<ObjectGuid> SuspendedPlayers = new List<ObjectGuid>();
        public bool NotifyUI;
        public bool IsParty;
        public bool Joined;
        public bool LfgJoined;
        public bool Queued;
        public bool Unused;
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

        public ObjectGuid Player;
        public LfgRoles RoleMask;
        public bool Accepted;
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
            _worldPacket.WriteUInt32(GroupFinderActivityID);
            _worldPacket.WriteUInt32(Members.Count);

            foreach (var slot in JoinSlots)
                _worldPacket.WriteUInt32(slot);

            _worldPacket.WriteBit(IsBeginning);
            _worldPacket.WriteBit(IsRequeue);
            _worldPacket.FlushBits();

            foreach (var member in Members)
                member.Write(_worldPacket);
        }

        public byte PartyIndex;
        public byte RoleCheckStatus;
        public List<uint> JoinSlots = new List<uint>();
        public ulong BgQueueID;
        public int GroupFinderActivityID = 0;
        public List<LFGRoleCheckUpdateMember> Members = new List<LFGRoleCheckUpdateMember>();
        public bool IsBeginning;
        public bool IsRequeue;
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

            foreach (LFGJoinBlackList blackList in BlackList)
                blackList.Write(_worldPacket);
        }

        public RideTicket Ticket = new RideTicket();
        public byte Result;
        public byte ResultDetail;
        public List<LFGJoinBlackList> BlackList = new List<LFGJoinBlackList>();
    }

    class LFGQueueStatus : ServerPacket
    {
        public LFGQueueStatus() : base(ServerOpcodes.LfgQueueStatus) { }

        public override void Write()
        {
            Ticket.Write(_worldPacket);

            _worldPacket.WriteUInt32(Slot);
            _worldPacket.WriteUInt32(AvgWaitTimeMe);
            _worldPacket.WriteUInt32(AvgWaitTime);

            for (int i = 0; i < 3; i++)
            {
                _worldPacket.WriteUInt32(AvgWaitTimeByRole[i]);
                _worldPacket.WriteUInt8(LastNeeded[i]);
            }

            _worldPacket.WriteUInt32(QueuedTime);
        }

        public RideTicket Ticket;
        public uint Slot;
        public uint AvgWaitTimeMe;
        public uint AvgWaitTime;
        public uint[] AvgWaitTimeByRole = new uint[3];
        public byte[] LastNeeded = new byte[3];
        public uint QueuedTime;
    }

    class LFGPlayerReward : ServerPacket
    {
        public LFGPlayerReward() : base(ServerOpcodes.LfgPlayerReward) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(QueuedSlot);
            _worldPacket.WriteUInt32(ActualSlot);
            _worldPacket.WriteUInt32(RewardMoney);
            _worldPacket.WriteUInt32(AddedXP);
            _worldPacket.WriteUInt32(Rewards.Count);

            foreach (var reward in Rewards)
                reward.Write(_worldPacket);
        }

        public uint QueuedSlot;
        public uint ActualSlot;
        public uint RewardMoney;
        public uint AddedXP;
        public List<LFGPlayerRewards> Rewards = new List<LFGPlayerRewards>();
    }

    class LfgBootPlayer : ServerPacket
    {
        public LfgBootPlayer() : base(ServerOpcodes.LfgBootPlayer, ConnectionType.Instance) { }

        public override void Write()
        {
            Info.Write(_worldPacket);
        }

        public LfgBootInfo Info = new LfgBootInfo();
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
            _worldPacket.WriteUInt32(EncounterMask);
            _worldPacket.WriteUInt32(Players.Count);
            _worldPacket.WriteUInt8(Unused);
            _worldPacket.WriteBit(ValidCompletedMask);
            _worldPacket.WriteBit(ProposalSilent);
            _worldPacket.WriteBit(IsRequeue);
            _worldPacket.FlushBits();

            foreach (var player in Players)
                player.Write(_worldPacket);
        }

        public RideTicket Ticket;
        public ulong InstanceID;
        public uint ProposalID;
        public uint Slot;
        public byte State;
        public uint CompletedMask;
        public uint EncounterMask;
        public byte Unused;
        public bool ValidCompletedMask;
        public bool ProposalSilent;
        public bool IsRequeue;
        public List<LFGProposalUpdatePlayer> Players = new List<LFGProposalUpdatePlayer>();
    }

    class LfgDisabled : ServerPacket
    {
        public LfgDisabled() : base(ServerOpcodes.LfgDisabled, ConnectionType.Instance) { }

        public override void Write() { }
    }

    class LfgOfferContinue : ServerPacket
    {
        public LfgOfferContinue(uint slot) : base(ServerOpcodes.LfgOfferContinue, ConnectionType.Instance)
        {
            Slot = slot;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Slot);
        }

        public uint Slot;
    }

    class LfgTeleportDenied : ServerPacket
    {
        public LfgTeleportDenied(LfgTeleportResult reason) : base(ServerOpcodes.LfgTeleportDenied, ConnectionType.Instance)
        {
            Reason = reason;
        }

        public override void Write()
        {
            _worldPacket.WriteBits(Reason, 4);
            _worldPacket.FlushBits();
        }

        public LfgTeleportResult Reason;
    }

    //Structs
    public class LFGBlackListSlot
    {
        public LFGBlackListSlot(uint slot, uint reason, int subReason1, int subReason2)
        {
            Slot = slot;
            Reason = reason;
            SubReason1 = subReason1;
            SubReason2 = subReason2;
        }

        public uint Slot { get; set; }
        public uint Reason { get; set; }
        public int SubReason1 { get; set; }
        public int SubReason2 { get; set; }
    }

    public class LFGBlackList
    {
        public void Write(WorldPacket data)
        {
            data.WriteBit(PlayerGuid.HasValue);
            data.WriteUInt32(Slot.Count);
            if (PlayerGuid.HasValue)
                data.WritePackedGuid(PlayerGuid.Value);

            foreach (LFGBlackListSlot lfgBlackListSlot in Slot)
            {
                data.WriteUInt32(lfgBlackListSlot.Slot);
                data.WriteUInt32(lfgBlackListSlot.Reason);
                data.WriteInt32(lfgBlackListSlot.SubReason1);
                data.WriteInt32(lfgBlackListSlot.SubReason2);
            }
        }

        public Optional<ObjectGuid> PlayerGuid;
        public List<LFGBlackListSlot> Slot = new List<LFGBlackListSlot>();
    }

    public struct LfgPlayerQuestRewardItem
    {
        public LfgPlayerQuestRewardItem(uint itemId, uint quantity)
        {
            ItemID = itemId;
            Quantity = quantity;
        }

        public uint ItemID;
        public uint Quantity;
    }

    public struct LfgPlayerQuestRewardCurrency
    {
        public LfgPlayerQuestRewardCurrency(uint currencyId, uint quantity)
        {
            CurrencyID = currencyId;
            Quantity = quantity;
        }

        public uint CurrencyID;
        public uint Quantity;
    }

    public class LfgPlayerQuestReward
    {
        public void Write(WorldPacket data)
        {
            data.WriteInt32(Mask);
            data.WriteUInt32(RewardMoney);
            data.WriteUInt32(RewardXP);
            data.WriteInt32(Item.Count);
            data.WriteInt32(Currency.Count);
            data.WriteInt32(BonusCurrency.Count);

            // Item
            foreach (var item in Item)
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
            foreach (var bonusCurrency in BonusCurrency)
            {
                data.WriteUInt32(bonusCurrency.CurrencyID);
                data.WriteUInt32(bonusCurrency.Quantity);
            }

            data.WriteBit(RewardSpellID.HasValue);
            data.WriteBit(Unused1.HasValue);
            data.WriteBit(Unused2.HasValue);
            data.WriteBit(Honor.HasValue);
            data.FlushBits();

            if (RewardSpellID.HasValue)
                data.WriteInt32(RewardSpellID.Value);

            if (Unused1.HasValue)
                data.WriteInt32(Unused1.Value);

            if (Unused2.HasValue)
                data.WriteUInt64(Unused2.Value);

            if (Honor.HasValue)
                data.WriteInt32(Honor.Value);
        }

        public uint Mask;
        public uint RewardMoney;
        public uint RewardXP;
        public List<LfgPlayerQuestRewardItem> Item = new List<LfgPlayerQuestRewardItem>();
        public List<LfgPlayerQuestRewardCurrency> Currency = new List<LfgPlayerQuestRewardCurrency>();
        public List<LfgPlayerQuestRewardCurrency> BonusCurrency = new List<LfgPlayerQuestRewardCurrency>();
        public Optional<int> RewardSpellID;                              // Only used by SMSG_LFG_PLAYER_INFO
        public Optional<int> Unused1;
        public Optional<ulong> Unused2;
        public Optional<int> Honor;                                      // Only used by SMSG_REQUEST_PVP_REWARDS_RESPONSE
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
            data.WriteUInt32(EncounterMask);
            data.WriteInt32(ShortageReward.Count);
            data.WriteBit(FirstReward);
            data.WriteBit(ShortageEligible);
            data.FlushBits();

            Rewards.Write(data);
            foreach (var shortageReward in ShortageReward)
                shortageReward.Write(data);
        }

        public uint Slot;
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
        public uint EncounterMask;
        public bool FirstReward;
        public bool ShortageEligible;
        public LfgPlayerQuestReward Rewards = new LfgPlayerQuestReward();
        public List<LfgPlayerQuestReward> ShortageReward = new List<LfgPlayerQuestReward>();
    }

    public class LFGRoleCheckUpdateMember
    {
        public LFGRoleCheckUpdateMember(ObjectGuid guid, uint rolesDesired, byte level, bool roleCheckComplete)
        {
            Guid = guid;
            RolesDesired = rolesDesired;
            Level = level;
            RoleCheckComplete = roleCheckComplete;
        }

        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(Guid);
            data.WriteUInt32(RolesDesired);
            data.WriteUInt8(Level);
            data.WriteBit(RoleCheckComplete);
            data.FlushBits();
        }

        public ObjectGuid Guid;
        public uint RolesDesired;
        public byte Level;
        public bool RoleCheckComplete;
    }

    public struct LFGJoinBlackListSlot
    {
        public LFGJoinBlackListSlot(int slot, int reason, int subReason1, int subReason2)
        {
            Slot = slot;
            Reason = reason;
            SubReason1 = subReason1;
            SubReason2 = subReason2;
        }

        public int Slot { get; set; }
        public int Reason { get; set; }
        public int SubReason1 { get; set; }
        public int SubReason2 { get; set; }
    }

    public class LFGJoinBlackList
    {
        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(PlayerGuid);
            data.WriteUInt32(Slots.Count);

            foreach (LFGJoinBlackListSlot lfgBlackListSlot in Slots)
            {
                data.WriteInt32(lfgBlackListSlot.Slot);
                data.WriteInt32(lfgBlackListSlot.Reason);
                data.WriteInt32(lfgBlackListSlot.SubReason1);
                data.WriteInt32(lfgBlackListSlot.SubReason2);
            }
        }

        public ObjectGuid PlayerGuid;
        public List<LFGJoinBlackListSlot> Slots = new List<LFGJoinBlackListSlot>();
    }

    public struct LFGPlayerRewards
    {
        public LFGPlayerRewards(uint id, uint quantity, int bonusQuantity, bool isCurrency)
        {
            Quantity = quantity;
            BonusQuantity = bonusQuantity;
            RewardItem = new Optional<ItemInstance>();
            RewardCurrency = new Optional<uint>();

            if (!isCurrency)
            {
                RewardItem.HasValue = true;
                RewardItem.Value.ItemID = id;
            }
            else
            {
                RewardCurrency.Set(id);
            }
        }

        public void Write(WorldPacket data)
        {
            data.WriteBit(RewardItem.HasValue);
            data.WriteBit(RewardCurrency.HasValue);
            if (RewardItem.HasValue)
                RewardItem.Value.Write(data);
            data.WriteUInt32(Quantity);
            data.WriteInt32(BonusQuantity);
            if (RewardCurrency.HasValue)
                data.WriteInt32(RewardCurrency.Value);
        }

        public Optional<ItemInstance> RewardItem;
        public Optional<uint> RewardCurrency;
        public uint Quantity;
        public int BonusQuantity;
    }

    public class LfgBootInfo
    {
        public void Write(WorldPacket data)
        {
            data.WriteBit(VoteInProgress);
            data.WriteBit(VotePassed);
            data.WriteBit(MyVoteCompleted);
            data.WriteBit(MyVote);
            data.WriteBits(Reason.GetByteCount(), 8);
            data.WritePackedGuid(Target);
            data.WriteUInt32(TotalVotes);
            data.WriteUInt32(BootVotes);
            data.WriteInt32(TimeLeft);
            data.WriteUInt32(VotesNeeded);
            data.WriteString(Reason);
        }

        public bool VoteInProgress;
        public bool VotePassed;
        public bool MyVoteCompleted;
        public bool MyVote;
        public ObjectGuid Target;
        public uint TotalVotes;
        public uint BootVotes;
        public uint TimeLeft;
        public uint VotesNeeded;
        public string Reason = "";
    }

    public struct LFGProposalUpdatePlayer
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(Roles);
            data.WriteBit(Me);
            data.WriteBit(SameParty);
            data.WriteBit(MyParty);
            data.WriteBit(Responded);
            data.WriteBit(Accepted);
            data.FlushBits();
        }

        public uint Roles;
        public bool Me;
        public bool SameParty;
        public bool MyParty;
        public bool Responded;
        public bool Accepted;
    }

    public class RideTicket
    {
        public void Read(WorldPacket data)
        {
            RequesterGuid = data.ReadPackedGuid();
            Id = data.ReadUInt32();
            Type = (RideType)data.ReadUInt32();
            Time = data.ReadInt32();
        }

        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(RequesterGuid);
            data.WriteUInt32(Id);
            data.WriteUInt32(Type);
            data.WriteInt32(Time);
        }

        public ObjectGuid RequesterGuid;
        public uint Id;
        public RideType Type;
        public int Time;
    }

    public enum RideType
    {
        None = 0,
        Battlegrounds = 1,
        Lfg = 2
    }
}
