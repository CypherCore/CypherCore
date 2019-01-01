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
    public class QueryGuildInfo : ClientPacket
    {
        public QueryGuildInfo(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            GuildGuid = _worldPacket.ReadPackedGuid();
            PlayerGuid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid GuildGuid;
        public ObjectGuid PlayerGuid;
    }

    public class QueryGuildInfoResponse : ServerPacket
    {
        public QueryGuildInfoResponse() : base(ServerOpcodes.QueryGuildInfoResponse) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(GuildGUID);
            _worldPacket.WriteBit(HasGuildInfo);
            _worldPacket.FlushBits();

            if (HasGuildInfo)
            {
                _worldPacket.WritePackedGuid(Info.GuildGuid);
                _worldPacket.WriteUInt32(Info.VirtualRealmAddress);
                _worldPacket.WriteUInt32(Info.Ranks.Count);
                _worldPacket.WriteUInt32(Info.EmblemStyle);
                _worldPacket.WriteUInt32(Info.EmblemColor);
                _worldPacket.WriteUInt32(Info.BorderStyle);
                _worldPacket.WriteUInt32(Info.BorderColor);
                _worldPacket.WriteUInt32(Info.BackgroundColor);
                _worldPacket.WriteBits(Info.GuildName.GetByteCount(), 7);
                _worldPacket.FlushBits();

                foreach (var rank in Info.Ranks)
                {
                    _worldPacket.WriteUInt32(rank.RankID);
                    _worldPacket.WriteUInt32(rank.RankOrder);

                    _worldPacket.WriteBits(rank.RankName.GetByteCount(), 7);
                    _worldPacket.WriteString(rank.RankName);
                }

                _worldPacket.WriteString(Info.GuildName);
            }

        }

        public ObjectGuid GuildGUID;
        public GuildInfo Info = new GuildInfo();
        public bool HasGuildInfo;

        public class GuildInfo
        {
            public ObjectGuid GuildGuid;

            public uint VirtualRealmAddress; // a special identifier made from the Index, BattleGroup and Region.

            public uint EmblemStyle;
            public uint EmblemColor;
            public uint BorderStyle;
            public uint BorderColor;
            public uint BackgroundColor;
            public List<RankInfo> Ranks = new List<RankInfo>();
            public string GuildName = "";

            public struct RankInfo
            {
                public RankInfo(uint id, uint order, string name)
                {
                    RankID = id;
                    RankOrder = order;
                    RankName = name;
                }

                public uint RankID;
                public uint RankOrder;
                public string RankName;
            }
        }
    }

    public class GuildGetRoster : ClientPacket
    {
        public GuildGetRoster(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class GuildRoster : ServerPacket
    {
        public GuildRoster() : base(ServerOpcodes.GuildRoster)
        {
            MemberData = new List<GuildRosterMemberData>();
        }

        public override void Write()
        {
            _worldPacket.WriteInt32(NumAccounts);
            _worldPacket.WritePackedTime(CreateDate);
            _worldPacket.WriteInt32(GuildFlags);
            _worldPacket.WriteUInt32(MemberData.Count);
            _worldPacket.WriteBits(WelcomeText.GetByteCount(), 10);
            _worldPacket.WriteBits(InfoText.GetByteCount(), 10);
            _worldPacket.FlushBits();

            MemberData.ForEach(p => p.Write(_worldPacket));

            _worldPacket.WriteString(WelcomeText);
            _worldPacket.WriteString(InfoText);
        }

        public List<GuildRosterMemberData> MemberData;
        public string WelcomeText;
        public string InfoText;
        public uint CreateDate;
        public int NumAccounts;
        public int GuildFlags;
    }

    public class GuildRosterUpdate : ServerPacket
    {
        public GuildRosterUpdate() : base(ServerOpcodes.GuildRosterUpdate)
        {
            MemberData = new List<GuildRosterMemberData>();
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(MemberData.Count);

            MemberData.ForEach(p => p.Write(_worldPacket));
        }

        public List<GuildRosterMemberData> MemberData;
    }

    public class GuildUpdateMotdText : ClientPacket
    {
        public GuildUpdateMotdText(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint textLen = _worldPacket.ReadBits<uint>(10);
            MotdText = _worldPacket.ReadString(textLen);
        }

        public string MotdText;
    }

    public class GuildCommandResult : ServerPacket
    {
        public GuildCommandResult() : base(ServerOpcodes.GuildCommandResult) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Result);
            _worldPacket.WriteInt32(Command);

            _worldPacket.WriteBits(Name.GetByteCount(), 8);
            _worldPacket.WriteString(Name);
        }

        public string Name;
        public GuildCommandError Result;
        public GuildCommandType Command;
    }

    public class AcceptGuildInvite : ClientPacket
    {
        public AcceptGuildInvite(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class GuildDeclineInvitation : ClientPacket
    {
        public GuildDeclineInvitation(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class DeclineGuildInvites : ClientPacket
    {
        public DeclineGuildInvites(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Allow = _worldPacket.HasBit();
        }

        public bool Allow;
    }

    public class GuildInviteByName : ClientPacket
    {
        public GuildInviteByName(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint nameLen = _worldPacket.ReadBits<uint>(9);
            Name = _worldPacket.ReadString(nameLen);
        }

        public string Name;
    }

    public class GuildInvite : ServerPacket
    {
        public GuildInvite() : base(ServerOpcodes.GuildInvite) { }

        public override void Write()
        {
            _worldPacket.WriteBits(InviterName.GetByteCount(), 6);
            _worldPacket.WriteBits(GuildName.GetByteCount(), 7);
            _worldPacket.WriteBits(OldGuildName.GetByteCount(), 7);

            _worldPacket.WriteUInt32(InviterVirtualRealmAddress);
            _worldPacket.WriteUInt32(GuildVirtualRealmAddress);
            _worldPacket.WritePackedGuid(GuildGUID);
            _worldPacket.WriteUInt32(OldGuildVirtualRealmAddress);
            _worldPacket.WritePackedGuid(OldGuildGUID);
            _worldPacket.WriteUInt32(EmblemStyle);
            _worldPacket.WriteUInt32(EmblemColor);
            _worldPacket.WriteUInt32(BorderStyle);
            _worldPacket.WriteUInt32(BorderColor);
            _worldPacket.WriteUInt32(Background);
            _worldPacket.WriteInt32(AchievementPoints);

            _worldPacket.WriteString(InviterName);
            _worldPacket.WriteString(GuildName);
            _worldPacket.WriteString(OldGuildName);
        }

        public ObjectGuid GuildGUID;
        public ObjectGuid OldGuildGUID;
        public int AchievementPoints;
        public uint EmblemColor;
        public uint EmblemStyle;
        public uint BorderStyle;
        public uint BorderColor;
        public uint Background;
        public uint GuildVirtualRealmAddress;
        public uint OldGuildVirtualRealmAddress;
        public uint InviterVirtualRealmAddress;
        public string InviterName;
        public string GuildName;
        public string OldGuildName;
    }

    public class GuildEventPresenceChange : ServerPacket
    {
        public GuildEventPresenceChange() : base(ServerOpcodes.GuildEventPresenceChange) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
            _worldPacket.WriteUInt32(VirtualRealmAddress);

            _worldPacket.WriteBits(Name.GetByteCount(), 6);
            _worldPacket.WriteBit(LoggedOn);
            _worldPacket.WriteBit(Mobile);

            _worldPacket.WriteString(Name);
        }

        public ObjectGuid Guid;
        public uint VirtualRealmAddress;
        public string Name;
        public bool Mobile;
        public bool LoggedOn;
    }

    public class GuildEventMotd : ServerPacket
    {
        public GuildEventMotd() : base(ServerOpcodes.GuildEventMotd) { }

        public override void Write()
        {
            _worldPacket.WriteBits(MotdText.GetByteCount(), 10);
            _worldPacket.WriteString(MotdText);
        }

        public string MotdText;
    }

    public class GuildEventPlayerJoined : ServerPacket
    {
        public GuildEventPlayerJoined() : base(ServerOpcodes.GuildEventPlayerJoined) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
            _worldPacket.WriteUInt32(VirtualRealmAddress);

            _worldPacket.WriteBits(Name.GetByteCount(), 6);
            _worldPacket.WriteString(Name);
        }

        public ObjectGuid Guid;
        public string Name;
        public uint VirtualRealmAddress;
    }

    public class GuildEventRankChanged : ServerPacket
    {
        public GuildEventRankChanged() : base(ServerOpcodes.GuildEventRankChanged) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(RankID);
        }

        public uint RankID;
    }

    public class GuildEventRanksUpdated : ServerPacket
    {
        public GuildEventRanksUpdated() : base(ServerOpcodes.GuildEventRanksUpdated) { }

        public override void Write() { }
    }

    public class GuildEventBankMoneyChanged : ServerPacket
    {
        public GuildEventBankMoneyChanged() : base(ServerOpcodes.GuildEventBankMoneyChanged) { }

        public override void Write()
        {
            _worldPacket.WriteUInt64(Money);
        }

        public ulong Money;
    }

    public class GuildEventDisbanded : ServerPacket
    {
        public GuildEventDisbanded() : base(ServerOpcodes.GuildEventDisbanded) { }

        public override void Write() { }
    }

    public class GuildEventLogQuery : ClientPacket
    {
        public GuildEventLogQuery(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class GuildEventLogQueryResults : ServerPacket
    {
        public GuildEventLogQueryResults() : base(ServerOpcodes.GuildEventLogQueryResults)
        {
            Entry = new List<GuildEventEntry>();
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Entry.Count);

            foreach (GuildEventEntry entry in Entry)
            {
                _worldPacket.WritePackedGuid(entry.PlayerGUID);
                _worldPacket.WritePackedGuid(entry.OtherGUID);
                _worldPacket.WriteUInt8(entry.TransactionType);
                _worldPacket.WriteUInt8(entry.RankID);
                _worldPacket.WriteUInt32(entry.TransactionDate);
            }
        }

        public List<GuildEventEntry> Entry;
    }

    public class GuildEventPlayerLeft : ServerPacket
    {
        public GuildEventPlayerLeft() : base(ServerOpcodes.GuildEventPlayerLeft) { }

        public override void Write()
        {
            _worldPacket.WriteBit(Removed);
            _worldPacket.WriteBits(LeaverName.GetByteCount(), 6);
            _worldPacket.FlushBits();

            if (Removed)
            {
                _worldPacket.WriteBits(RemoverName.GetByteCount(), 6);
                _worldPacket.WritePackedGuid(RemoverGUID);
                _worldPacket.WriteUInt32(RemoverVirtualRealmAddress);
                _worldPacket.WriteString(RemoverName);
            }

            _worldPacket.WritePackedGuid(LeaverGUID);
            _worldPacket.WriteUInt32(LeaverVirtualRealmAddress);
            _worldPacket.WriteString(LeaverName);
        }

        public ObjectGuid LeaverGUID;
        public string LeaverName;
        public uint LeaverVirtualRealmAddress;
        public ObjectGuid RemoverGUID;
        public string RemoverName;
        public uint RemoverVirtualRealmAddress;
        public bool Removed;
    }

    public class GuildEventNewLeader : ServerPacket
    {
        public GuildEventNewLeader() : base(ServerOpcodes.GuildEventNewLeader) { }

        public override void Write()
        {
            _worldPacket.WriteBit(SelfPromoted);
            _worldPacket.WriteBits(OldLeaderName.GetByteCount(), 6);
            _worldPacket.WriteBits(NewLeaderName.GetByteCount(), 6);

            _worldPacket.WritePackedGuid(OldLeaderGUID);
            _worldPacket.WriteUInt32(OldLeaderVirtualRealmAddress);
            _worldPacket.WritePackedGuid(NewLeaderGUID);
            _worldPacket.WriteUInt32(NewLeaderVirtualRealmAddress);

            _worldPacket.WriteString(OldLeaderName);
            _worldPacket.WriteString(NewLeaderName);
        }

        public ObjectGuid NewLeaderGUID;
        public string NewLeaderName;
        public uint NewLeaderVirtualRealmAddress;
        public ObjectGuid OldLeaderGUID;
        public string OldLeaderName = "";
        public uint OldLeaderVirtualRealmAddress;
        public bool SelfPromoted;
    }

    public class GuildEventTabAdded : ServerPacket
    {
        public GuildEventTabAdded() : base(ServerOpcodes.GuildEventTabAdded) { }

        public override void Write() { }
    }

    public class GuildEventTabModified : ServerPacket
    {
        public GuildEventTabModified() : base(ServerOpcodes.GuildEventTabModified) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Tab);

            _worldPacket.WriteBits(Name.GetByteCount(), 7);
            _worldPacket.WriteBits(Icon.GetByteCount(), 9);
            _worldPacket.FlushBits();

            _worldPacket.WriteString(Name);
            _worldPacket.WriteString(Icon);
        }

        public string Icon;
        public string Name;
        public int Tab;
    }

    public class GuildEventTabTextChanged : ServerPacket
    {
        public GuildEventTabTextChanged() : base(ServerOpcodes.GuildEventTabTextChanged) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Tab);
        }

        public int Tab;
    }

    public class GuildEventBankContentsChanged : ServerPacket
    {
        public GuildEventBankContentsChanged() : base(ServerOpcodes.GuildEventBankContentsChanged) { }

        public override void Write() { }
    }

    public class GuildPermissionsQuery : ClientPacket
    {
        public GuildPermissionsQuery(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class GuildPermissionsQueryResults : ServerPacket
    {
        public GuildPermissionsQueryResults() : base(ServerOpcodes.GuildPermissionsQueryResults)
        {
            Tab = new List<GuildRankTabPermissions>();
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(RankID);
            _worldPacket.WriteInt32(WithdrawGoldLimit);
            _worldPacket.WriteInt32(Flags);
            _worldPacket.WriteInt32(NumTabs);
            _worldPacket.WriteUInt32(Tab.Count);

            foreach (GuildRankTabPermissions tab in Tab)
            {
                _worldPacket.WriteInt32(tab.Flags);
                _worldPacket.WriteInt32(tab.WithdrawItemLimit);
            }
        }

        public int NumTabs;
        public int WithdrawGoldLimit;
        public int Flags;
        public uint RankID;
        public List<GuildRankTabPermissions> Tab;

        public struct GuildRankTabPermissions
        {
            public int Flags;
            public int WithdrawItemLimit;
        }
    }

    public class GuildSetRankPermissions : ClientPacket
    {
        public GuildSetRankPermissions(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            RankID = _worldPacket.ReadInt32();
            RankOrder = _worldPacket.ReadInt32();
            Flags = _worldPacket.ReadUInt32();
            OldFlags = _worldPacket.ReadUInt32();
            WithdrawGoldLimit = _worldPacket.ReadUInt32();

            for (byte i = 0; i < GuildConst.MaxBankTabs; i++)
            {
                TabFlags[i] = _worldPacket.ReadUInt32();
                TabWithdrawItemLimit[i] = _worldPacket.ReadUInt32();
            }

            _worldPacket.ResetBitPos();
            uint rankNameLen = _worldPacket.ReadBits<uint>(7);

            RankName = _worldPacket.ReadString(rankNameLen);
        }

        public int RankID;
        public int RankOrder;
        public uint WithdrawGoldLimit;
        public uint Flags;
        public uint OldFlags;
        public uint[] TabFlags = new uint[GuildConst.MaxBankTabs];
        public uint[] TabWithdrawItemLimit = new uint[GuildConst.MaxBankTabs];
        public string RankName;
    }

    public class GuildAddRank : ClientPacket
    {
        public GuildAddRank(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint nameLen = _worldPacket.ReadBits<uint>(7);
            _worldPacket.ResetBitPos();

            RankOrder = _worldPacket.ReadInt32();
            Name = _worldPacket.ReadString(nameLen);
        }

        public string Name;
        public int RankOrder;
    }

    public class GuildAssignMemberRank : ClientPacket
    {
        public GuildAssignMemberRank(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Member = _worldPacket.ReadPackedGuid();
            RankOrder = _worldPacket.ReadInt32();
        }

        public ObjectGuid Member;
        public int RankOrder;
    }

    public class GuildDeleteRank : ClientPacket
    {
        public GuildDeleteRank(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            RankOrder = _worldPacket.ReadInt32();
        }

        public int RankOrder;
    }

    public class GuildGetRanks : ClientPacket
    {
        public GuildGetRanks(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            GuildGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid GuildGUID;
    }

    public class GuildRanks : ServerPacket
    {
        public GuildRanks() : base(ServerOpcodes.GuildRanks)
        {
            Ranks = new List<GuildRankData>();
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Ranks.Count);

            Ranks.ForEach(p => p.Write(_worldPacket));
        }

        public List<GuildRankData> Ranks;
    }

    public class GuildSendRankChange : ServerPacket
    {
        public GuildSendRankChange() : base(ServerOpcodes.GuildSendRankChange) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Officer);
            _worldPacket.WritePackedGuid(Other);
            _worldPacket.WriteUInt32(RankID);

            _worldPacket.WriteBit(Promote);
            _worldPacket.FlushBits();
        }

        public ObjectGuid Other;
        public ObjectGuid Officer;
        public bool Promote;
        public uint RankID;
    }

    public class GuildShiftRank : ClientPacket
    {
        public GuildShiftRank(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            RankOrder = _worldPacket.ReadInt32();
            ShiftUp = _worldPacket.HasBit();
        }

        public bool ShiftUp;
        public int RankOrder;
    }

    public class GuildUpdateInfoText : ClientPacket
    {
        public GuildUpdateInfoText(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint textLen = _worldPacket.ReadBits<uint>(11);
            InfoText = _worldPacket.ReadString(textLen);
        }

        public string InfoText;
    }

    public class GuildSetMemberNote : ClientPacket
    {
        public GuildSetMemberNote(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            NoteeGUID = _worldPacket.ReadPackedGuid();

            uint noteLen = _worldPacket.ReadBits<uint>(8);
            IsPublic = _worldPacket.HasBit();

            Note = _worldPacket.ReadString(noteLen);
        }

        public ObjectGuid NoteeGUID;
        public bool IsPublic;          // 0 == Officer, 1 == Public
        public string Note;
    }

    public class GuildMemberUpdateNote : ServerPacket
    {
        public GuildMemberUpdateNote() : base(ServerOpcodes.GuildMemberUpdateNote) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Member);

            _worldPacket.WriteBits(Note.GetByteCount(), 8);
            _worldPacket.WriteBit(IsPublic);
            _worldPacket.FlushBits();

            _worldPacket.WriteString(Note);
        }

        public ObjectGuid Member;
        public bool IsPublic;          // 0 == Officer, 1 == Public
        public string Note;
    }

    public class GuildMemberDailyReset : ServerPacket
    {
        public GuildMemberDailyReset() : base(ServerOpcodes.GuildMemberDailyReset) { }

        public override void Write() { }
    }

    public class GuildDelete : ClientPacket
    {
        public GuildDelete(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class GuildDemoteMember : ClientPacket
    {
        public GuildDemoteMember(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Demotee = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Demotee;
    }

    public class GuildPromoteMember : ClientPacket
    {
        public GuildPromoteMember(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Promotee = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Promotee;
    }

    public class GuildOfficerRemoveMember : ClientPacket
    {
        public GuildOfficerRemoveMember(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Removee = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Removee;
    }

    public class GuildLeave : ClientPacket
    {
        public GuildLeave(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class GuildChangeNameRequest : ClientPacket
    {
        public GuildChangeNameRequest(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint nameLen = _worldPacket.ReadBits<uint>(7);
            NewName = _worldPacket.ReadString(nameLen);
        }

        public string NewName;
    }

    public class GuildFlaggedForRename : ServerPacket
    {
        public GuildFlaggedForRename() : base(ServerOpcodes.GuildFlaggedForRename) { }

        public override void Write()
        {
            _worldPacket.WriteBit(FlagSet);
        }

        public bool FlagSet;
    }

    public class RequestGuildPartyState : ClientPacket
    {
        public RequestGuildPartyState(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            GuildGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid GuildGUID;
    }

    public class GuildPartyState : ServerPacket
    {
        public GuildPartyState() : base(ServerOpcodes.GuildPartyState, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteBit(InGuildParty);
            _worldPacket.FlushBits();

            _worldPacket.WriteInt32(NumMembers);
            _worldPacket.WriteInt32(NumRequired);
            _worldPacket.WriteFloat(GuildXPEarnedMult);
        }

        public float GuildXPEarnedMult = 0.0f;
        public int NumMembers;
        public int NumRequired;
        public bool InGuildParty;
    }

    public class RequestGuildRewardsList : ClientPacket
    {
        public RequestGuildRewardsList(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            CurrentVersion = _worldPacket.ReadUInt32();
        }

        public uint CurrentVersion;
    }

    public class GuildRewardList : ServerPacket
    {
        public GuildRewardList() : base(ServerOpcodes.GuildRewardList)
        {
            RewardItems = new List<GuildRewardItem>();
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Version);
            _worldPacket.WriteUInt32(RewardItems.Count);

            foreach (var item in RewardItems)
                item.Write(_worldPacket);
        }

        public List<GuildRewardItem> RewardItems;
        public uint Version;
    }

    public class GuildBankActivate : ClientPacket
    {
        public GuildBankActivate(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Banker = _worldPacket.ReadPackedGuid();
            FullUpdate = _worldPacket.HasBit();
        }

        public ObjectGuid Banker;
        public bool FullUpdate;
    }

    public class GuildBankBuyTab : ClientPacket
    {
        public GuildBankBuyTab(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Banker = _worldPacket.ReadPackedGuid();
            BankTab = _worldPacket.ReadUInt8();
        }

        public ObjectGuid Banker;
        public byte BankTab;
    }

    public class GuildBankUpdateTab : ClientPacket
    {
        public GuildBankUpdateTab(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Banker = _worldPacket.ReadPackedGuid();
            BankTab = _worldPacket.ReadUInt8();

            _worldPacket.ResetBitPos();
            uint nameLen = _worldPacket.ReadBits<uint>(7);
            uint iconLen = _worldPacket.ReadBits<uint>(9);

            Name = _worldPacket.ReadString(nameLen);
            Icon = _worldPacket.ReadString(iconLen);
        }

        public ObjectGuid Banker;
        public byte BankTab;
        public string Name;
        public string Icon;
    }

    public class GuildBankDepositMoney : ClientPacket
    {
        public GuildBankDepositMoney(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Banker = _worldPacket.ReadPackedGuid();
            Money = _worldPacket.ReadUInt64();
        }

        public ObjectGuid Banker;
        public ulong Money;
    }

    public class GuildBankQueryTab : ClientPacket
    {
        public GuildBankQueryTab(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Banker = _worldPacket.ReadPackedGuid();
            Tab = _worldPacket.ReadUInt8();

            FullUpdate = _worldPacket.HasBit();
        }

        public ObjectGuid Banker;
        public byte Tab;
        public bool FullUpdate;
    }

    public class GuildBankRemainingWithdrawMoneyQuery : ClientPacket
    {
        public GuildBankRemainingWithdrawMoneyQuery(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class GuildBankRemainingWithdrawMoney : ServerPacket
    {
        public GuildBankRemainingWithdrawMoney() : base(ServerOpcodes.GuildBankRemainingWithdrawMoney) { }

        public override void Write()
        {
            _worldPacket.WriteInt64(RemainingWithdrawMoney);
        }

        public long RemainingWithdrawMoney;
    }

    public class GuildBankWithdrawMoney : ClientPacket
    {
        public GuildBankWithdrawMoney(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Banker = _worldPacket.ReadPackedGuid();
            Money = _worldPacket.ReadUInt64();
        }

        public ObjectGuid Banker;
        public ulong Money;
    }

    public class GuildBankQueryResults : ServerPacket
    {
        public GuildBankQueryResults() : base(ServerOpcodes.GuildBankQueryResults)
        {
            ItemInfo = new List<GuildBankItemInfo>();
            TabInfo = new List<GuildBankTabInfo>();
        }

        public override void Write()
        {
            _worldPacket.WriteUInt64(Money);
            _worldPacket.WriteInt32(Tab);
            _worldPacket.WriteInt32(WithdrawalsRemaining);
            _worldPacket.WriteUInt32(TabInfo.Count);
            _worldPacket.WriteUInt32(ItemInfo.Count);
            _worldPacket.WriteBit(FullUpdate);
            _worldPacket.FlushBits();

            foreach (GuildBankTabInfo tab in TabInfo)
            {
                _worldPacket.WriteUInt32(tab.TabIndex);
                _worldPacket.WriteBits(tab.Name.GetByteCount(), 7);
                _worldPacket.WriteBits(tab.Icon.GetByteCount(), 9);

                _worldPacket.WriteString(tab.Name);
                _worldPacket.WriteString(tab.Icon);
            }

            foreach (GuildBankItemInfo item in ItemInfo)
            {
                _worldPacket.WriteInt32(item.Slot);
                _worldPacket.WriteInt32(item.Count);
                _worldPacket.WriteInt32(item.EnchantmentID);
                _worldPacket.WriteInt32(item.Charges);
                _worldPacket.WriteInt32(item.OnUseEnchantmentID);
                _worldPacket.WriteInt32(item.Flags);

                item.Item.Write(_worldPacket);

                _worldPacket.WriteBits(item.SocketEnchant.Count, 2);
                _worldPacket.WriteBit(item.Locked);
                _worldPacket.FlushBits();

                foreach (ItemGemData socketEnchant in item.SocketEnchant)
                    socketEnchant.Write(_worldPacket);
            }
        }

        public List<GuildBankItemInfo> ItemInfo;
        public List<GuildBankTabInfo> TabInfo;
        public int WithdrawalsRemaining;
        public int Tab;
        public ulong Money;
        public bool FullUpdate;
    }

    public class GuildBankSwapItems : ClientPacket
    {
        public GuildBankSwapItems(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Banker = _worldPacket.ReadPackedGuid();
            BankTab = _worldPacket.ReadUInt8();
            BankSlot = _worldPacket.ReadUInt8();
            ItemID = _worldPacket.ReadUInt32();
            BankTab1 = _worldPacket.ReadUInt8();
            BankSlot1 = _worldPacket.ReadUInt8();
            ItemID1 = _worldPacket.ReadUInt32();
            BankItemCount = _worldPacket.ReadInt32();
            ContainerSlot = _worldPacket.ReadUInt8();
            ContainerItemSlot = _worldPacket.ReadUInt8();
            ToSlot = _worldPacket.ReadUInt8();
            StackCount = _worldPacket.ReadInt32();

            _worldPacket.ResetBitPos();
            BankOnly = _worldPacket.HasBit();
            AutoStore = _worldPacket.HasBit();
        }

        public ObjectGuid Banker;
        public int StackCount;
        public int BankItemCount;
        public uint ItemID;
        public uint ItemID1;
        public byte ToSlot;
        public byte BankSlot;
        public byte BankSlot1;
        public byte BankTab;
        public byte BankTab1;
        public byte ContainerSlot;
        public byte ContainerItemSlot;
        public bool AutoStore;
        public bool BankOnly;
    }

    public class GuildBankLogQuery : ClientPacket
    {
        public GuildBankLogQuery(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Tab = _worldPacket.ReadInt32();
        }

        public int Tab;
    }

    public class GuildBankLogQueryResults : ServerPacket
    {
        public GuildBankLogQueryResults() : base(ServerOpcodes.GuildBankLogQueryResults)
        {
            Entry = new List<GuildBankLogEntry>();
        }

        public override void Write()
        {
            _worldPacket.WriteInt32(Tab);
            _worldPacket.WriteUInt32(Entry.Count);
            _worldPacket.WriteBit(WeeklyBonusMoney.HasValue);
            _worldPacket.FlushBits();

            foreach (GuildBankLogEntry logEntry in Entry)
            {
                _worldPacket.WritePackedGuid(logEntry.PlayerGUID);
                _worldPacket.WriteUInt32(logEntry.TimeOffset);
                _worldPacket.WriteInt8(logEntry.EntryType);

                _worldPacket.WriteBit(logEntry.Money.HasValue);
                _worldPacket.WriteBit(logEntry.ItemID.HasValue);
                _worldPacket.WriteBit(logEntry.Count.HasValue);
                _worldPacket.WriteBit(logEntry.OtherTab.HasValue);
                _worldPacket.FlushBits();

                if (logEntry.Money.HasValue)
                    _worldPacket.WriteUInt64(logEntry.Money.Value);

                if (logEntry.ItemID.HasValue)
                    _worldPacket.WriteInt32(logEntry.ItemID.Value);

                if (logEntry.Count.HasValue)
                    _worldPacket.WriteInt32(logEntry.Count.Value);

                if (logEntry.OtherTab.HasValue)
                    _worldPacket.WriteInt8(logEntry.OtherTab.Value);
            }

            if (WeeklyBonusMoney.HasValue)
                _worldPacket.WriteUInt64(WeeklyBonusMoney.Value);
        }

        public int Tab;
        public List<GuildBankLogEntry> Entry;
        public Optional<ulong> WeeklyBonusMoney;
    }

    public class GuildBankTextQuery : ClientPacket
    {
        public GuildBankTextQuery(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Tab = _worldPacket.ReadInt32();
        }

        public int Tab;
    }

    public class GuildBankTextQueryResult : ServerPacket
    {
        public GuildBankTextQueryResult() : base(ServerOpcodes.GuildBankTextQueryResult) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Tab);

            _worldPacket.WriteBits(Text.GetByteCount(), 14);
            _worldPacket.WriteString(Text);
        }

        public int Tab;
        public string Text;
    }

    public class GuildBankSetTabText : ClientPacket
    {
        public GuildBankSetTabText(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Tab = _worldPacket.ReadInt32();
            TabText = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(14));
        }

        public int Tab;
        public string TabText;
    }

    public class GuildQueryNews : ClientPacket
    {
        public GuildQueryNews(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            GuildGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid GuildGUID;
    }

    public class GuildNewsPkt : ServerPacket
    {
        public GuildNewsPkt() : base(ServerOpcodes.GuildNews)
        {
            NewsEvents = new List<GuildNewsEvent>();
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(NewsEvents.Count);
            foreach (var newsEvent in NewsEvents)
                newsEvent.Write(_worldPacket);
        }

        public List<GuildNewsEvent> NewsEvents;
    }

    public class GuildNewsUpdateSticky : ClientPacket
    {
        public GuildNewsUpdateSticky(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            GuildGUID = _worldPacket.ReadPackedGuid();
            NewsID = _worldPacket.ReadInt32();

            Sticky = _worldPacket.HasBit();
        }

        public int NewsID;
        public ObjectGuid GuildGUID;
        public bool Sticky;
    }

    class GuildReplaceGuildMaster : ClientPacket
    {
        public GuildReplaceGuildMaster(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class GuildSetGuildMaster : ClientPacket
    {
        public GuildSetGuildMaster(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint nameLen = _worldPacket.ReadBits<uint>(9);
            NewMasterName = _worldPacket.ReadString(nameLen);
        }

        public string NewMasterName;
    }

    public class GuildChallengeUpdateRequest : ClientPacket
    {
        public GuildChallengeUpdateRequest(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class GuildChallengeUpdate : ServerPacket
    {
        public GuildChallengeUpdate() : base(ServerOpcodes.GuildChallengeUpdate) { }

        public override void Write()
        {
            for (int i = 0; i < GuildConst.ChallengesTypes; ++i)
                _worldPacket.WriteInt32(CurrentCount[i]);

            for (int i = 0; i < GuildConst.ChallengesTypes; ++i)
                _worldPacket.WriteInt32(MaxCount[i]);

            for (int i = 0; i < GuildConst.ChallengesTypes; ++i)
                _worldPacket.WriteInt32(MaxLevelGold[i]);

            for (int i = 0; i < GuildConst.ChallengesTypes; ++i)
                _worldPacket.WriteInt32(Gold[i]);
        }

        public int[] CurrentCount = new int[GuildConst.ChallengesTypes];
        public int[] MaxCount = new int[GuildConst.ChallengesTypes];
        public int[] Gold = new int[GuildConst.ChallengesTypes];
        public int[] MaxLevelGold = new int[GuildConst.ChallengesTypes];
    }

    public class SaveGuildEmblem : ClientPacket
    {
        public SaveGuildEmblem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Vendor = _worldPacket.ReadPackedGuid();
            EStyle = _worldPacket.ReadUInt32();
            EColor = _worldPacket.ReadUInt32();
            BStyle = _worldPacket.ReadUInt32();
            BColor = _worldPacket.ReadUInt32();
            Bg = _worldPacket.ReadUInt32();
        }

        public ObjectGuid Vendor;
        public uint BStyle;
        public uint EStyle;
        public uint BColor;
        public uint EColor;
        public uint Bg;
    }

    public class PlayerSaveGuildEmblem : ServerPacket
    {
        public PlayerSaveGuildEmblem() : base(ServerOpcodes.PlayerSaveGuildEmblem) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Error);
        }

        public GuildEmblemError Error;
    }

    class GuildSetAchievementTracking : ClientPacket
    {
        public GuildSetAchievementTracking(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint count = _worldPacket.ReadUInt32();

            for (uint i = 0; i < count; ++i)
                AchievementIDs.Add(_worldPacket.ReadUInt32());
        }

        public List<uint> AchievementIDs = new List<uint>();
    }

    class GuildNameChanged : ServerPacket
    {
        public GuildNameChanged() : base(ServerOpcodes.GuildNameChanged) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(GuildGUID);
            _worldPacket.WriteBits(GuildName.GetByteCount(), 7);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(GuildName);
        }

        public ObjectGuid GuildGUID;
        public string GuildName;
    }

    //Structs
    public struct GuildRosterProfessionData
    {
        public void Write(WorldPacket data)
        {
            data.WriteInt32(DbID);
            data.WriteInt32(Rank);
            data.WriteInt32(Step);
        }

        public int DbID;
        public int Rank;
        public int Step;
    }

    public class GuildRosterMemberData
    {
        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(Guid);
            data.WriteInt32(RankID);
            data.WriteInt32(AreaID);
            data.WriteInt32(PersonalAchievementPoints);
            data.WriteInt32(GuildReputation);
            data.WriteFloat(LastSave);

            for (byte i = 0; i < 2; i++)
                Profession[i].Write(data);

            data.WriteUInt32(VirtualRealmAddress);
            data.WriteUInt8(Status);
            data.WriteUInt8(Level);
            data.WriteUInt8(ClassID);
            data.WriteUInt8(Gender);

            data.WriteBits(Name.GetByteCount(), 6);
            data.WriteBits(Note.GetByteCount(), 8);
            data.WriteBits(OfficerNote.GetByteCount(), 8);
            data.WriteBit(Authenticated);
            data.WriteBit(SorEligible);

            data.WriteString(Name);
            data.WriteString(Note);
            data.WriteString(OfficerNote);
        }

        public ObjectGuid Guid;
        public long WeeklyXP;
        public long TotalXP;
        public int RankID;
        public int AreaID;
        public int PersonalAchievementPoints;
        public int GuildReputation;
        public int GuildRepToCap;
        public float LastSave;
        public string Name;
        public uint VirtualRealmAddress;
        public string Note;
        public string OfficerNote;
        public byte Status;
        public byte Level;
        public byte ClassID;
        public byte Gender;
        public bool Authenticated;
        public bool SorEligible;
        public GuildRosterProfessionData[] Profession = new GuildRosterProfessionData[2];
    }

    public class GuildEventEntry
    {
        public ObjectGuid PlayerGUID;
        public ObjectGuid OtherGUID;
        public byte TransactionType;
        public byte RankID;
        public uint TransactionDate;
    }

    public class GuildRankData
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(RankID);
            data.WriteUInt32(RankOrder);
            data.WriteUInt32(Flags);
            data.WriteUInt32(WithdrawGoldLimit);

            for (byte i = 0; i < GuildConst.MaxBankTabs; i++)
            {
                data.WriteUInt32(TabFlags[i]);
                data.WriteUInt32(TabWithdrawItemLimit[i]);
            }

            data.WriteBits(RankName.GetByteCount(), 7);
            data.WriteString(RankName);
        }

        public uint RankID;
        public uint RankOrder;
        public uint Flags;
        public uint WithdrawGoldLimit;
        public string RankName;
        public uint[] TabFlags = new uint[GuildConst.MaxBankTabs];
        public uint[] TabWithdrawItemLimit = new uint[GuildConst.MaxBankTabs];
    }

    public class GuildRewardItem
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(ItemID);
            data.WriteUInt32(Unk4);
            data.WriteUInt32(AchievementsRequired.Count);
            data.WriteUInt64(RaceMask);
            data.WriteUInt32(MinGuildLevel);
            data.WriteUInt32(MinGuildRep);
            data.WriteUInt64(Cost);

            foreach (var achievementId in AchievementsRequired)
                data.WriteUInt32(achievementId);
        }

        public uint ItemID;
        public uint Unk4;
        public List<uint> AchievementsRequired = new List<uint>();
        public ulong RaceMask;
        public int MinGuildLevel;
        public int MinGuildRep;
        public ulong Cost;
    }

    public class GuildBankItemInfo
    {
        public ItemInstance Item;
        public int Slot;
        public int Count;
        public int EnchantmentID;
        public int Charges;
        public int OnUseEnchantmentID;
        public int Flags;
        public bool Locked;
        public List<ItemGemData> SocketEnchant = new List<ItemGemData>();
    }

    public struct GuildBankTabInfo
    {
        public int TabIndex;
        public string Name;
        public string Icon;
    }

    public class GuildBankLogEntry
    {
        public ObjectGuid PlayerGUID;
        public uint TimeOffset;
        public sbyte EntryType;
        public Optional<ulong> Money;
        public Optional<int> ItemID;
        public Optional<int> Count;
        public Optional<sbyte> OtherTab;
    }

    public class GuildNewsEvent
    {
        public void Write(WorldPacket data)
        {
            data.WriteInt32(Id);
            data.WritePackedTime(CompletedDate);
            data.WriteInt32(Type);
            data.WriteInt32(Flags);

            for (byte i = 0; i < 2; i++)
                data.WriteInt32(Data[i]);

            data.WritePackedGuid(MemberGuid);
            data.WriteUInt32(MemberList.Count);

            foreach (ObjectGuid memberGuid in MemberList)
                data.WritePackedGuid(memberGuid);

            data.WriteBit(Item.HasValue);
            data.FlushBits();

            if (Item.HasValue)
                Item.Value.Write(data);
        }

        public int Id;
        public uint CompletedDate;
        public int Type;
        public int Flags;
        public int[] Data = new int[2];
        public ObjectGuid MemberGuid;
        public List<ObjectGuid> MemberList = new List<ObjectGuid>();
        public Optional<ItemInstance> Item;
    }
}
