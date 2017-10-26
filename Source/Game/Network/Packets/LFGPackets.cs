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

        public bool QueueAsGroup { get; set; }
        bool Unknown;       // Always false in 7.2.5
        public byte PartyIndex { get; set; }
        public LfgRoles Roles { get; set; }
        public List<uint> Slots { get; set; } = new List<uint>();
    }

    class DFLeave : ClientPacket
    {
        public DFLeave(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Ticket.Read(_worldPacket);
        }

        public RideTicket Ticket { get; set; } = new RideTicket();
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

        public RideTicket Ticket { get; set; } = new RideTicket();
        public ulong InstanceID { get; set; }
        public uint ProposalID { get; set; }
        public bool Accepted { get; set; }
    }

    class DFSetRoles : ClientPacket
    {
        public DFSetRoles(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            RolesDesired = (LfgRoles)_worldPacket.ReadUInt32();
            PartyIndex = _worldPacket.ReadUInt8();
        }

        public LfgRoles RolesDesired { get; set; }
        public byte PartyIndex { get; set; }
    }

    class DFBootPlayerVote : ClientPacket
    {
        public DFBootPlayerVote(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Vote = _worldPacket.HasBit();
        }

        public bool Vote { get; set; }
    }

    class DFTeleport : ClientPacket
    {
        public DFTeleport(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            TeleportOut = _worldPacket.HasBit();
        }

        public bool TeleportOut { get; set; }
    }

    class DFGetSystemInfo : ClientPacket
    {
        public DFGetSystemInfo(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Player = _worldPacket.HasBit();
            PartyIndex = _worldPacket.ReadUInt8();
        }

        public byte PartyIndex { get; set; }
        public bool Player { get; set; }
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

        public LFGBlackList BlackList { get; set; } = new LFGBlackList();
        public List<LfgPlayerDungeonInfo> Dungeons { get; set; } = new List<LfgPlayerDungeonInfo>();
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

        public List<LFGBlackList> Player { get; set; } = new List<LFGBlackList>();
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

        public RideTicket Ticket { get; set; } = new RideTicket();
        public byte SubType { get; set; }
        public byte Reason { get; set; }
        public List<uint> Slots { get; set; } = new List<uint>();
        public uint RequestedRoles { get; set; }
        public List<ObjectGuid> SuspendedPlayers { get; set; } = new List<ObjectGuid>();
        public bool NotifyUI { get; set; }
        public bool IsParty { get; set; }
        public bool Joined { get; set; }
        public bool LfgJoined { get; set; }
        public bool Queued { get; set; }
        public bool Unused { get; set; }
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

        public ObjectGuid Player { get; set; }
        public LfgRoles RoleMask { get; set; }
        public bool Accepted { get; set; }
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

        public byte PartyIndex { get; set; }
        public byte RoleCheckStatus { get; set; }
        public List<uint> JoinSlots { get; set; } = new List<uint>();
        public ulong BgQueueID { get; set; }
        public int GroupFinderActivityID { get; set; } = 0;
        public List<LFGRoleCheckUpdateMember> Members { get; set; } = new List<LFGRoleCheckUpdateMember>();
        public bool IsBeginning { get; set; }
        public bool IsRequeue { get; set; }
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

        public RideTicket Ticket { get; set; }
        public byte Result { get; set; }
        public byte ResultDetail { get; set; }
        public List<LFGJoinBlackList> BlackList { get; set; } = new List<LFGJoinBlackList>();
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

        public RideTicket Ticket { get; set; }
        public uint Slot { get; set; }
        public uint AvgWaitTimeMe { get; set; }
        public uint AvgWaitTime { get; set; }
        public uint[] AvgWaitTimeByRole { get; set; } = new uint[3];
        public byte[] LastNeeded { get; set; } = new byte[3];
        public uint QueuedTime { get; set; }
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

        public uint QueuedSlot { get; set; }
        public uint ActualSlot { get; set; }
        public uint RewardMoney { get; set; }
        public uint AddedXP { get; set; }
        public List<LFGPlayerRewards> Rewards { get; set; } = new List<LFGPlayerRewards>();
    }

    class LfgBootPlayer : ServerPacket
    {
        public LfgBootPlayer() : base(ServerOpcodes.LfgBootPlayer, ConnectionType.Instance) { }

        public override void Write()
        {
            Info.Write(_worldPacket);
        }

        public LfgBootInfo Info { get; set; } = new LfgBootInfo();
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
            _worldPacket.WriteUInt8(Unused);
            _worldPacket.WriteBit(ValidCompletedMask);
            _worldPacket.WriteBit(ProposalSilent);
            _worldPacket.WriteBit(IsRequeue);
            _worldPacket.FlushBits();

            foreach (var player in Players)
                player.Write(_worldPacket);
        }

        public RideTicket Ticket { get; set; }
        public ulong InstanceID { get; set; }
        public uint ProposalID { get; set; }
        public uint Slot { get; set; }
        public byte State { get; set; }
        public uint CompletedMask { get; set; }
        public byte Unused { get; set; }
        public bool ValidCompletedMask { get; set; }
        public bool ProposalSilent { get; set; }
        public bool IsRequeue { get; set; }
        public List<LFGProposalUpdatePlayer> Players { get; set; } = new List<LFGProposalUpdatePlayer>();
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

        public uint Slot { get; set; }
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

        public LfgTeleportResult Reason { get; set; }
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

        public Optional<ObjectGuid> PlayerGuid { get; set; }
        public List<LFGBlackListSlot> Slot { get; set; } = new List<LFGBlackListSlot>();
    }

    public struct LfgPlayerQuestRewardItem
    {
        public LfgPlayerQuestRewardItem(uint itemId, uint quantity)
        {
            ItemID = itemId;
            Quantity = quantity;
        }

        public uint ItemID { get; set; }
        public uint Quantity { get; set; }
    }

    public struct LfgPlayerQuestRewardCurrency
    {
        public LfgPlayerQuestRewardCurrency(uint currencyId, uint quantity)
        {
            CurrencyID = currencyId;
            Quantity = quantity;
        }

        public uint CurrencyID { get; set; }
        public uint Quantity { get; set; }
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

        public uint Mask { get; set; }
        public uint RewardMoney { get; set; }
        public uint RewardXP { get; set; }
        public List<LfgPlayerQuestRewardItem> Item { get; set; } = new List<LfgPlayerQuestRewardItem>();
        public List<LfgPlayerQuestRewardCurrency> Currency { get; set; } = new List<LfgPlayerQuestRewardCurrency>();
        public List<LfgPlayerQuestRewardCurrency> BonusCurrency { get; set; } = new List<LfgPlayerQuestRewardCurrency>();
        public Optional<int> RewardSpellID;                              // Only used by SMSG_LFG_PLAYER_INFO
        public Optional<int> Unused1 { get; set; }
        public Optional<ulong> Unused2 { get; set; }
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

        public uint Slot { get; set; }
        public int CompletionQuantity { get; set; }
        public int CompletionLimit { get; set; }
        public int CompletionCurrencyID { get; set; }
        public int SpecificQuantity { get; set; }
        public int SpecificLimit { get; set; }
        public int OverallQuantity { get; set; }
        public int OverallLimit { get; set; }
        public int PurseWeeklyQuantity { get; set; }
        public int PurseWeeklyLimit { get; set; }
        public int PurseQuantity { get; set; }
        public int PurseLimit { get; set; }
        public int Quantity { get; set; }
        public uint CompletedMask { get; set; }
        public uint EncounterMask { get; set; }
        public bool FirstReward { get; set; }
        public bool ShortageEligible { get; set; }
        public LfgPlayerQuestReward Rewards { get; set; } = new LfgPlayerQuestReward();
        public List<LfgPlayerQuestReward> ShortageReward { get; set; } = new List<LfgPlayerQuestReward>();
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

        public ObjectGuid Guid { get; set; }
        public uint RolesDesired { get; set; }
        public byte Level { get; set; }
        public bool RoleCheckComplete { get; set; }
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

        public ObjectGuid PlayerGuid { get; set; }
        public List<LFGJoinBlackListSlot> Slots { get; set; } = new List<LFGJoinBlackListSlot>();
    }

    public struct LFGPlayerRewards
    {
        public LFGPlayerRewards(uint rewardItem, uint rewardItemQuantity, int bonusCurrency, bool isCurrency)
        {
            RewardItem = rewardItem;
            RewardItemQuantity = rewardItemQuantity;
            BonusCurrency = bonusCurrency;
            IsCurrency = isCurrency;
        }

        public void Write(WorldPacket data)
        {
            data.WriteInt32(RewardItem);
            data.WriteUInt32(RewardItemQuantity);
            data.WriteInt32(BonusCurrency);
            data.WriteBit(IsCurrency);
            data.FlushBits();
        }

        public uint RewardItem { get; set; }
        public uint RewardItemQuantity { get; set; }
        public int BonusCurrency { get; set; }
        public bool IsCurrency { get; set; }
    }

    public class LfgBootInfo
    {
        public void Write(WorldPacket data)
        {
            data.WriteBit(VoteInProgress);
            data.WriteBit(VotePassed);
            data.WriteBit(MyVoteCompleted);
            data.WriteBit(MyVote);
            data.WriteBits(Reason.Length, 8);
            data.WritePackedGuid(Target);
            data.WriteUInt32(TotalVotes);
            data.WriteUInt32(BootVotes);
            data.WriteInt32(TimeLeft);
            data.WriteUInt32(VotesNeeded);
            data.WriteString(Reason);
        }

        public bool VoteInProgress { get; set; }
        public bool VotePassed { get; set; }
        public bool MyVoteCompleted { get; set; }
        public bool MyVote { get; set; }
        public ObjectGuid Target { get; set; }
        public uint TotalVotes { get; set; }
        public uint BootVotes { get; set; }
        public uint TimeLeft { get; set; }
        public uint VotesNeeded { get; set; }
        public string Reason { get; set; } = "";
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

        public uint Roles { get; set; }
        public bool Me { get; set; }
        public bool SameParty { get; set; }
        public bool MyParty { get; set; }
        public bool Responded { get; set; }
        public bool Accepted { get; set; }
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

        public ObjectGuid RequesterGuid { get; set; }
        public uint Id { get; set; }
        public RideType Type { get; set; }
        public int Time { get; set; }
    }

    public enum RideType
    {
        None = 0,
        Battlegrounds = 1,
        Lfg = 2
    }
}
