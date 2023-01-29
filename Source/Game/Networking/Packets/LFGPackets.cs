// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets
{
    internal class DFJoin : ClientPacket
    {
        public byte PartyIndex;

        public bool QueueAsGroup;
        public LfgRoles Roles;
        public List<uint> Slots = new();
        private bool Unknown; // Always false in 7.2.5

        public DFJoin(WorldPacket packet) : base(packet)
        {
        }

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
    }

    internal class DFLeave : ClientPacket
    {
        public RideTicket Ticket = new();

        public DFLeave(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Ticket.Read(_worldPacket);
        }
    }

    internal class DFProposalResponse : ClientPacket
    {
        public bool Accepted;
        public ulong InstanceID;
        public uint ProposalID;

        public RideTicket Ticket = new();

        public DFProposalResponse(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Ticket.Read(_worldPacket);
            InstanceID = _worldPacket.ReadUInt64();
            ProposalID = _worldPacket.ReadUInt32();
            Accepted = _worldPacket.HasBit();
        }
    }

    internal class DFSetRoles : ClientPacket
    {
        public byte PartyIndex;

        public LfgRoles RolesDesired;

        public DFSetRoles(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            RolesDesired = (LfgRoles)_worldPacket.ReadUInt32();
            PartyIndex = _worldPacket.ReadUInt8();
        }
    }

    internal class DFBootPlayerVote : ClientPacket
    {
        public bool Vote;

        public DFBootPlayerVote(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Vote = _worldPacket.HasBit();
        }
    }

    internal class DFTeleport : ClientPacket
    {
        public bool TeleportOut;

        public DFTeleport(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            TeleportOut = _worldPacket.HasBit();
        }
    }

    internal class DFGetSystemInfo : ClientPacket
    {
        public byte PartyIndex;
        public bool Player;

        public DFGetSystemInfo(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Player = _worldPacket.HasBit();
            PartyIndex = _worldPacket.ReadUInt8();
        }
    }

    internal class DFGetJoinStatus : ClientPacket
    {
        public DFGetJoinStatus(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
        }
    }

    internal class LfgPlayerInfo : ServerPacket
    {
        public LFGBlackList BlackList = new();
        public List<LfgPlayerDungeonInfo> Dungeons = new();

        public LfgPlayerInfo() : base(ServerOpcodes.LfgPlayerInfo, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteInt32(Dungeons.Count);
            BlackList.Write(_worldPacket);

            foreach (var dungeonInfo in Dungeons)
                dungeonInfo.Write(_worldPacket);
        }
    }

    internal class LfgPartyInfo : ServerPacket
    {
        public List<LFGBlackList> Player = new();

        public LfgPartyInfo() : base(ServerOpcodes.LfgPartyInfo, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteInt32(Player.Count);

            foreach (var blackList in Player)
                blackList.Write(_worldPacket);
        }
    }

    internal class LFGUpdateStatus : ServerPacket
    {
        public bool IsParty;
        public bool Joined;
        public bool LfgJoined;
        public bool NotifyUI;
        public bool Queued;
        public uint QueueMapID;
        public byte Reason;
        public uint RequestedRoles;
        public List<uint> Slots = new();
        public byte SubType;
        public List<ObjectGuid> SuspendedPlayers = new();

        public RideTicket Ticket = new();
        public bool Unused;

        public LFGUpdateStatus() : base(ServerOpcodes.LfgUpdateStatus)
        {
        }

        public override void Write()
        {
            Ticket.Write(_worldPacket);

            _worldPacket.WriteUInt8(SubType);
            _worldPacket.WriteUInt8(Reason);
            _worldPacket.WriteInt32(Slots.Count);
            _worldPacket.WriteUInt32(RequestedRoles);
            _worldPacket.WriteInt32(SuspendedPlayers.Count);
            _worldPacket.WriteUInt32(QueueMapID);

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
    }

    internal class RoleChosen : ServerPacket
    {
        public bool Accepted;

        public ObjectGuid Player;
        public LfgRoles RoleMask;

        public RoleChosen() : base(ServerOpcodes.RoleChosen)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Player);
            _worldPacket.WriteUInt32((uint)RoleMask);
            _worldPacket.WriteBit(Accepted);
            _worldPacket.FlushBits();
        }
    }

    internal class LFGRoleCheckUpdate : ServerPacket
    {
        public List<ulong> BgQueueIDs = new();
        public int GroupFinderActivityID = 0;
        public bool IsBeginning;
        public bool IsRequeue;
        public List<uint> JoinSlots = new();
        public List<LFGRoleCheckUpdateMember> Members = new();

        public byte PartyIndex;
        public byte RoleCheckStatus;

        public LFGRoleCheckUpdate() : base(ServerOpcodes.LfgRoleCheckUpdate)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt8(PartyIndex);
            _worldPacket.WriteUInt8(RoleCheckStatus);
            _worldPacket.WriteInt32(JoinSlots.Count);
            _worldPacket.WriteInt32(BgQueueIDs.Count);
            _worldPacket.WriteInt32(GroupFinderActivityID);
            _worldPacket.WriteInt32(Members.Count);

            foreach (var slot in JoinSlots)
                _worldPacket.WriteUInt32(slot);

            foreach (ulong bgQueueID in BgQueueIDs)
                _worldPacket.WriteUInt64(bgQueueID);

            _worldPacket.WriteBit(IsBeginning);
            _worldPacket.WriteBit(IsRequeue);
            _worldPacket.FlushBits();

            foreach (var member in Members)
                member.Write(_worldPacket);
        }
    }

    internal class LFGJoinResult : ServerPacket
    {
        public List<LFGBlackListPkt> BlackList = new();
        public List<string> BlackListNames = new();
        public byte Result;
        public byte ResultDetail;

        public RideTicket Ticket = new();

        public LFGJoinResult() : base(ServerOpcodes.LfgJoinResult)
        {
        }

        public override void Write()
        {
            Ticket.Write(_worldPacket);

            _worldPacket.WriteUInt8(Result);
            _worldPacket.WriteUInt8(ResultDetail);
            _worldPacket.WriteInt32(BlackList.Count);
            _worldPacket.WriteInt32(BlackListNames.Count);

            foreach (LFGBlackListPkt blackList in BlackList)
                blackList.Write(_worldPacket);

            foreach (string str in BlackListNames)
                _worldPacket.WriteBits(str.GetByteCount() + 1, 24);

            foreach (string str in BlackListNames)
                if (!str.IsEmpty())
                    _worldPacket.WriteCString(str);
        }
    }

    internal class LFGQueueStatus : ServerPacket
    {
        public uint AvgWaitTime;
        public uint[] AvgWaitTimeByRole = new uint[3];
        public uint AvgWaitTimeMe;
        public byte[] LastNeeded = new byte[3];
        public uint QueuedTime;
        public uint Slot;

        public RideTicket Ticket;

        public LFGQueueStatus() : base(ServerOpcodes.LfgQueueStatus)
        {
        }

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
    }

    internal class LFGPlayerReward : ServerPacket
    {
        public uint ActualSlot;
        public uint AddedXP;

        public uint QueuedSlot;
        public uint RewardMoney;
        public List<LFGPlayerRewards> Rewards = new();

        public LFGPlayerReward() : base(ServerOpcodes.LfgPlayerReward)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(QueuedSlot);
            _worldPacket.WriteUInt32(ActualSlot);
            _worldPacket.WriteUInt32(RewardMoney);
            _worldPacket.WriteUInt32(AddedXP);
            _worldPacket.WriteInt32(Rewards.Count);

            foreach (var reward in Rewards)
                reward.Write(_worldPacket);
        }
    }

    internal class LfgBootPlayer : ServerPacket
    {
        public LfgBootInfo Info = new();

        public LfgBootPlayer() : base(ServerOpcodes.LfgBootPlayer, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            Info.Write(_worldPacket);
        }
    }

    internal class LFGProposalUpdate : ServerPacket
    {
        public uint CompletedMask;
        public uint EncounterMask;
        public ulong InstanceID;
        public bool IsRequeue;
        public List<LFGProposalUpdatePlayer> Players = new();
        public uint ProposalID;
        public bool ProposalSilent;
        public uint Slot;
        public byte State;

        public RideTicket Ticket;
        public byte Unused;
        public bool ValidCompletedMask;

        public LFGProposalUpdate() : base(ServerOpcodes.LfgProposalUpdate)
        {
        }

        public override void Write()
        {
            Ticket.Write(_worldPacket);

            _worldPacket.WriteUInt64(InstanceID);
            _worldPacket.WriteUInt32(ProposalID);
            _worldPacket.WriteUInt32(Slot);
            _worldPacket.WriteUInt8(State);
            _worldPacket.WriteUInt32(CompletedMask);
            _worldPacket.WriteUInt32(EncounterMask);
            _worldPacket.WriteInt32(Players.Count);
            _worldPacket.WriteUInt8(Unused);
            _worldPacket.WriteBit(ValidCompletedMask);
            _worldPacket.WriteBit(ProposalSilent);
            _worldPacket.WriteBit(IsRequeue);
            _worldPacket.FlushBits();

            foreach (var player in Players)
                player.Write(_worldPacket);
        }
    }

    internal class LfgDisabled : ServerPacket
    {
        public LfgDisabled() : base(ServerOpcodes.LfgDisabled, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
        }
    }

    internal class LfgOfferContinue : ServerPacket
    {
        public uint Slot;

        public LfgOfferContinue(uint slot) : base(ServerOpcodes.LfgOfferContinue, ConnectionType.Instance)
        {
            Slot = slot;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Slot);
        }
    }

    internal class LfgTeleportDenied : ServerPacket
    {
        public LfgTeleportResult Reason;

        public LfgTeleportDenied(LfgTeleportResult reason) : base(ServerOpcodes.LfgTeleportDenied, ConnectionType.Instance)
        {
            Reason = reason;
        }

        public override void Write()
        {
            _worldPacket.WriteBits(Reason, 4);
            _worldPacket.FlushBits();
        }
    }

    //Structs
    public class LFGBlackListSlot
    {
        public uint Reason;
        public uint Slot;
        public uint SoftLock;
        public int SubReason1;
        public int SubReason2;

        public LFGBlackListSlot(uint slot, uint reason, int subReason1, int subReason2, uint softLock)
        {
            Slot = slot;
            Reason = reason;
            SubReason1 = subReason1;
            SubReason2 = subReason2;
            SoftLock = softLock;
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(Slot);
            data.WriteUInt32(Reason);
            data.WriteInt32(SubReason1);
            data.WriteInt32(SubReason2);
            data.WriteUInt32(SoftLock);
        }
    }

    public class LFGBlackList
    {
        public ObjectGuid? PlayerGuid;
        public List<LFGBlackListSlot> Slot = new();

        public void Write(WorldPacket data)
        {
            data.WriteBit(PlayerGuid.HasValue);
            data.WriteInt32(Slot.Count);

            if (PlayerGuid.HasValue)
                data.WritePackedGuid(PlayerGuid.Value);

            foreach (LFGBlackListSlot slot in Slot)
                slot.Write(data);
        }
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
        public List<LfgPlayerQuestRewardCurrency> BonusCurrency = new();
        public List<LfgPlayerQuestRewardCurrency> Currency = new();
        public int? Honor; // Only used by SMSG_REQUEST_PVP_REWARDS_RESPONSE
        public List<LfgPlayerQuestRewardItem> Item = new();

        public uint Mask;
        public uint RewardMoney;
        public int? RewardSpellID; // Only used by SMSG_LFG_PLAYER_INFO
        public uint RewardXP;
        public int? Unused1;
        public ulong? Unused2;

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(Mask);
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
    }

    public class LfgPlayerDungeonInfo
    {
        public uint CompletedMask;
        public int CompletionCurrencyID;
        public int CompletionLimit;
        public int CompletionQuantity;
        public uint EncounterMask;
        public bool FirstReward;
        public int OverallLimit;
        public int OverallQuantity;
        public int PurseLimit;
        public int PurseQuantity;
        public int PurseWeeklyLimit;
        public int PurseWeeklyQuantity;
        public int Quantity;
        public LfgPlayerQuestReward Rewards = new();
        public bool ShortageEligible;
        public List<LfgPlayerQuestReward> ShortageReward = new();

        public uint Slot;
        public int SpecificLimit;
        public int SpecificQuantity;

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(Slot);
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
            data.WriteUInt32(CompletedMask);
            data.WriteUInt32(EncounterMask);
            data.WriteInt32(ShortageReward.Count);
            data.WriteBit(FirstReward);
            data.WriteBit(ShortageEligible);
            data.FlushBits();

            Rewards.Write(data);

            foreach (var shortageReward in ShortageReward)
                shortageReward.Write(data);
        }
    }

    public class LFGRoleCheckUpdateMember
    {
        public ObjectGuid Guid;
        public byte Level;
        public bool RoleCheckComplete;
        public uint RolesDesired;

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
    }

    public class LFGBlackListPkt
    {
        public ObjectGuid? PlayerGuid;
        public List<LFGBlackListSlot> Slot = new();

        public void Write(WorldPacket data)
        {
            data.WriteBit(PlayerGuid.HasValue);
            data.WriteInt32(Slot.Count);

            if (PlayerGuid.HasValue)
                data.WritePackedGuid(PlayerGuid.Value);

            foreach (LFGBlackListSlot slot in Slot)
                slot.Write(data);
        }
    }

    public struct LFGPlayerRewards
    {
        public LFGPlayerRewards(uint id, uint quantity, int bonusQuantity, bool isCurrency)
        {
            Quantity = quantity;
            BonusQuantity = bonusQuantity;
            RewardItem = null;
            RewardCurrency = null;

            if (!isCurrency)
            {
                RewardItem = new ItemInstance();
                RewardItem.ItemID = id;
            }
            else
            {
                RewardCurrency = id;
            }
        }

        public void Write(WorldPacket data)
        {
            data.WriteBit(RewardItem != null);
            data.WriteBit(RewardCurrency.HasValue);

            RewardItem?.Write(data);

            data.WriteUInt32(Quantity);
            data.WriteInt32(BonusQuantity);

            if (RewardCurrency.HasValue)
                data.WriteUInt32(RewardCurrency.Value);
        }

        public ItemInstance RewardItem;
        public uint? RewardCurrency;
        public uint Quantity;
        public int BonusQuantity;
    }

    public class LfgBootInfo
    {
        public uint BootVotes;
        public bool MyVote;
        public bool MyVoteCompleted;
        public string Reason = "";
        public ObjectGuid Target;
        public uint TimeLeft;
        public uint TotalVotes;

        public bool VoteInProgress;
        public bool VotePassed;
        public uint VotesNeeded;

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
            data.WriteUInt32(TimeLeft);
            data.WriteUInt32(VotesNeeded);
            data.WriteString(Reason);
        }
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
        public uint Id;

        public ObjectGuid RequesterGuid;
        public long Time;
        public RideType Type;
        public bool Unknown925;

        public void Read(WorldPacket data)
        {
            RequesterGuid = data.ReadPackedGuid();
            Id = data.ReadUInt32();
            Type = (RideType)data.ReadUInt32();
            Time = data.ReadInt64();
            Unknown925 = data.HasBit();
            data.ResetBitPos();
        }

        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(RequesterGuid);
            data.WriteUInt32(Id);
            data.WriteUInt32((uint)Type);
            data.WriteInt64(Time);
            data.WriteBit(Unknown925);
            data.FlushBits();
        }
    }

    public enum RideType
    {
        None = 0,
        Battlegrounds = 1,
        Lfg = 2
    }
}