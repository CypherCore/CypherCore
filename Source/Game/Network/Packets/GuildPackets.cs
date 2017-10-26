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
    public class QueryGuildInfo : ClientPacket
    {
        public QueryGuildInfo(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            GuildGuid = _worldPacket.ReadPackedGuid();
            PlayerGuid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid GuildGuid { get; set; }
        public ObjectGuid PlayerGuid { get; set; }
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
                _worldPacket.WriteBits(Info.GuildName.Length, 7);
                _worldPacket.FlushBits();

                foreach (var rank in Info.Ranks)
                {
                    _worldPacket.WriteUInt32(rank.RankID);
                    _worldPacket.WriteUInt32(rank.RankOrder);

                    _worldPacket.WriteBits(rank.RankName.Length, 7);
                    _worldPacket.WriteString(rank.RankName);
                }

                _worldPacket.WriteString(Info.GuildName);
            }

        }

        public ObjectGuid GuildGUID { get; set; }
        public GuildInfo Info { get; set; } = new GuildInfo();
        public bool HasGuildInfo { get; set; }

        public class GuildInfo
        {
            public ObjectGuid GuildGuid { get; set; }

            public uint VirtualRealmAddress; // a special identifier made from the Index, BattleGroup and Region.

            public uint EmblemStyle { get; set; }
            public uint EmblemColor { get; set; }
            public uint BorderStyle { get; set; }
            public uint BorderColor { get; set; }
            public uint BackgroundColor { get; set; }
            public List<RankInfo> Ranks { get; set; } = new List<RankInfo>();
            public string GuildName { get; set; } = "";

            public struct RankInfo
            {
                public RankInfo(uint id, uint order, string name)
                {
                    RankID = id;
                    RankOrder = order;
                    RankName = name;
                }

                public uint RankID { get; set; }
                public uint RankOrder { get; set; }
                public string RankName { get; set; }
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
            _worldPacket.WriteBits(WelcomeText.Length, 10);
            _worldPacket.WriteBits(InfoText.Length, 10);
            _worldPacket.FlushBits();

            MemberData.ForEach(p => p.Write(_worldPacket));

            _worldPacket.WriteString(WelcomeText);
            _worldPacket.WriteString(InfoText);
        }

        public List<GuildRosterMemberData> MemberData { get; set; }
        public string WelcomeText { get; set; }
        public string InfoText { get; set; }
        public uint CreateDate { get; set; }
        public int NumAccounts { get; set; }
        public int GuildFlags { get; set; }
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

        public List<GuildRosterMemberData> MemberData { get; set; }
    }

    public class GuildUpdateMotdText : ClientPacket
    {
        public GuildUpdateMotdText(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint textLen = _worldPacket.ReadBits<uint>(10);
            MotdText = _worldPacket.ReadString(textLen);
        }

        public string MotdText { get; set; }
    }

    public class GuildCommandResult : ServerPacket
    {
        public GuildCommandResult() : base(ServerOpcodes.GuildCommandResult) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Result);
            _worldPacket.WriteInt32(Command);

            _worldPacket.WriteBits(Name.Length, 8);
            _worldPacket.WriteString(Name);
        }

        public string Name { get; set; }
        public GuildCommandError Result { get; set; }
        public GuildCommandType Command { get; set; }
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

        public bool Allow { get; set; }
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
            _worldPacket.WriteBits(InviterName.Length, 6);
            _worldPacket.WriteBits(GuildName.Length, 7);
            _worldPacket.WriteBits(OldGuildName.Length, 7);

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

        public ObjectGuid GuildGUID { get; set; }
        public ObjectGuid OldGuildGUID { get; set; }
        public int AchievementPoints { get; set; }
        public uint EmblemColor { get; set; }
        public uint EmblemStyle { get; set; }
        public uint BorderStyle { get; set; }
        public uint BorderColor { get; set; }
        public uint Background { get; set; }
        public uint GuildVirtualRealmAddress { get; set; }
        public uint OldGuildVirtualRealmAddress { get; set; }
        public uint InviterVirtualRealmAddress { get; set; }
        public string InviterName { get; set; }
        public string GuildName { get; set; }
        public string OldGuildName { get; set; }
    }

    public class GuildEventPresenceChange : ServerPacket
    {
        public GuildEventPresenceChange() : base(ServerOpcodes.GuildEventPresenceChange) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
            _worldPacket.WriteUInt32(VirtualRealmAddress);

            _worldPacket.WriteBits(Name.Length, 6);
            _worldPacket.WriteBit(LoggedOn);
            _worldPacket.WriteBit(Mobile);

            _worldPacket.WriteString(Name);
        }

        public ObjectGuid Guid { get; set; }
        public uint VirtualRealmAddress { get; set; }
        public string Name { get; set; }
        public bool Mobile { get; set; }
        public bool LoggedOn { get; set; }
    }

    public class GuildEventMotd : ServerPacket
    {
        public GuildEventMotd() : base(ServerOpcodes.GuildEventMotd) { }

        public override void Write()
        {
            _worldPacket.WriteBits(MotdText.Length, 10);
            _worldPacket.WriteString(MotdText);
        }

        public string MotdText { get; set; }
    }

    public class GuildEventPlayerJoined : ServerPacket
    {
        public GuildEventPlayerJoined() : base(ServerOpcodes.GuildEventPlayerJoined) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
            _worldPacket.WriteUInt32(VirtualRealmAddress);

            _worldPacket.WriteBits(Name.Length, 6);
            _worldPacket.WriteString(Name);
        }

        public ObjectGuid Guid { get; set; }
        public string Name { get; set; }
        public uint VirtualRealmAddress { get; set; }
    }

    public class GuildEventRankChanged : ServerPacket
    {
        public GuildEventRankChanged() : base(ServerOpcodes.GuildEventRankChanged) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(RankID);
        }

        public uint RankID { get; set; }
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

        public ulong Money { get; set; }
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

        public List<GuildEventEntry> Entry { get; set; }
    }

    public class GuildEventPlayerLeft : ServerPacket
    {
        public GuildEventPlayerLeft() : base(ServerOpcodes.GuildEventPlayerLeft) { }

        public override void Write()
        {
            _worldPacket.WriteBit(Removed);
            _worldPacket.WriteBits(LeaverName.Length, 6);
            _worldPacket.FlushBits();

            if (Removed)
            {
                _worldPacket.WriteBits(RemoverName.Length, 6);
                _worldPacket.WritePackedGuid(RemoverGUID);
                _worldPacket.WriteUInt32(RemoverVirtualRealmAddress);
                _worldPacket.WriteString(RemoverName);
            }

            _worldPacket.WritePackedGuid(LeaverGUID);
            _worldPacket.WriteUInt32(LeaverVirtualRealmAddress);
            _worldPacket.WriteString(LeaverName);
        }

        public ObjectGuid LeaverGUID { get; set; }
        public string LeaverName { get; set; }
        public uint LeaverVirtualRealmAddress { get; set; }
        public ObjectGuid RemoverGUID { get; set; }
        public string RemoverName { get; set; }
        public uint RemoverVirtualRealmAddress { get; set; }
        public bool Removed { get; set; }
    }

    public class GuildEventNewLeader : ServerPacket
    {
        public GuildEventNewLeader() : base(ServerOpcodes.GuildEventNewLeader) { }

        public override void Write()
        {
            _worldPacket.WriteBit(SelfPromoted);
            _worldPacket.WriteBits(NewLeaderName.Length, 6);
            _worldPacket.WriteBits(OldLeaderName.Length, 6);

            _worldPacket.WritePackedGuid(OldLeaderGUID);
            _worldPacket.WriteUInt32(OldLeaderVirtualRealmAddress);
            _worldPacket.WritePackedGuid(NewLeaderGUID);
            _worldPacket.WriteUInt32(NewLeaderVirtualRealmAddress);

            _worldPacket.WriteString(NewLeaderName);
            _worldPacket.WriteString(OldLeaderName);
        }

        public ObjectGuid NewLeaderGUID { get; set; }
        public string NewLeaderName { get; set; }
        public uint NewLeaderVirtualRealmAddress { get; set; }
        public ObjectGuid OldLeaderGUID { get; set; }
        public string OldLeaderName { get; set; } = "";
        public uint OldLeaderVirtualRealmAddress { get; set; }
        public bool SelfPromoted { get; set; }
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

            _worldPacket.WriteBits(Name.Length, 7);
            _worldPacket.WriteBits(Icon.Length, 9);
            _worldPacket.FlushBits();

            _worldPacket.WriteString(Name);
            _worldPacket.WriteString(Icon);
        }

        public string Icon { get; set; }
        public string Name { get; set; }
        public int Tab { get; set; }
    }

    public class GuildEventTabTextChanged : ServerPacket
    {
        public GuildEventTabTextChanged() : base(ServerOpcodes.GuildEventTabTextChanged) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Tab);
        }

        public int Tab { get; set; }
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

        public int NumTabs { get; set; }
        public int WithdrawGoldLimit { get; set; }
        public int Flags { get; set; }
        public uint RankID { get; set; }
        public List<GuildRankTabPermissions> Tab { get; set; }

        public struct GuildRankTabPermissions
        {
            public int Flags { get; set; }
            public int WithdrawItemLimit { get; set; }
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
            WithdrawGoldLimit = _worldPacket.ReadInt32();

            for (byte i = 0; i < GuildConst.MaxBankTabs; i++)
            {
                TabFlags[i] = _worldPacket.ReadInt32();
                TabWithdrawItemLimit[i] = _worldPacket.ReadInt32();
            }

            _worldPacket.ResetBitPos();
            uint rankNameLen = _worldPacket.ReadBits<uint>(7);

            RankName = _worldPacket.ReadString(rankNameLen);
        }

        public int RankID { get; set; }
        public int RankOrder { get; set; }
        public int WithdrawGoldLimit { get; set; }
        public uint Flags { get; set; }
        public uint OldFlags { get; set; }
        public int[] TabFlags { get; set; } = new int[GuildConst.MaxBankTabs];
        public int[] TabWithdrawItemLimit { get; set; } = new int[GuildConst.MaxBankTabs];
        public string RankName { get; set; }
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

        public string Name { get; set; }
        public int RankOrder { get; set; }
    }

    public class GuildAssignMemberRank : ClientPacket
    {
        public GuildAssignMemberRank(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Member = _worldPacket.ReadPackedGuid();
            RankOrder = _worldPacket.ReadInt32();
        }

        public ObjectGuid Member { get; set; }
        public int RankOrder { get; set; }
    }

    public class GuildDeleteRank : ClientPacket
    {
        public GuildDeleteRank(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            RankOrder = _worldPacket.ReadInt32();
        }

        public int RankOrder { get; set; }
    }

    public class GuildGetRanks : ClientPacket
    {
        public GuildGetRanks(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            GuildGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid GuildGUID { get; set; }
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

        public List<GuildRankData> Ranks { get; set; }
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

        public ObjectGuid Other { get; set; }
        public ObjectGuid Officer { get; set; }
        public bool Promote { get; set; }
        public uint RankID { get; set; }
    }

    public class GuildShiftRank : ClientPacket
    {
        public GuildShiftRank(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            RankOrder = _worldPacket.ReadInt32();
            ShiftUp = _worldPacket.HasBit();
        }

        public bool ShiftUp { get; set; }
        public int RankOrder { get; set; }
    }

    public class GuildUpdateInfoText : ClientPacket
    {
        public GuildUpdateInfoText(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint textLen = _worldPacket.ReadBits<uint>(11);
            InfoText = _worldPacket.ReadString(textLen);
        }

        public string InfoText { get; set; }
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

        public ObjectGuid NoteeGUID { get; set; }
        public bool IsPublic { get; set; }          ///< 0 == Officer, 1 == Public
        public string Note { get; set; }
    }

    public class GuildMemberUpdateNote : ServerPacket
    {
        public GuildMemberUpdateNote() : base(ServerOpcodes.GuildMemberUpdateNote) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Member);

            _worldPacket.WriteBits(Note.Length, 8);
            _worldPacket.WriteBit(IsPublic);
            _worldPacket.FlushBits();

            _worldPacket.WriteString(Note);
        }

        public ObjectGuid Member { get; set; }
        public bool IsPublic;          ///< 0 == Officer, 1 == Public
        public string Note { get; set; }
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

        public ObjectGuid Demotee { get; set; }
    }

    public class GuildPromoteMember : ClientPacket
    {
        public GuildPromoteMember(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Promotee = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Promotee { get; set; }
    }

    public class GuildOfficerRemoveMember : ClientPacket
    {
        public GuildOfficerRemoveMember(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Removee = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Removee { get; set; }
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

        public string NewName { get; set; }
    }

    public class GuildFlaggedForRename : ServerPacket
    {
        public GuildFlaggedForRename() : base(ServerOpcodes.GuildFlaggedForRename) { }

        public override void Write()
        {
            _worldPacket.WriteBit(FlagSet);
        }

        public bool FlagSet { get; set; }
    }

    public class RequestGuildPartyState : ClientPacket
    {
        public RequestGuildPartyState(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            GuildGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid GuildGUID { get; set; }
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

        public float GuildXPEarnedMult { get; set; } = 0.0f;
        public int NumMembers { get; set; }
        public int NumRequired { get; set; }
        public bool InGuildParty { get; set; }
    }

    public class RequestGuildRewardsList : ClientPacket
    {
        public RequestGuildRewardsList(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            CurrentVersion = _worldPacket.ReadUInt32();
        }

        public uint CurrentVersion { get; set; }
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

        public List<GuildRewardItem> RewardItems { get; set; }
        public uint Version { get; set; }
    }

    public class GuildBankActivate : ClientPacket
    {
        public GuildBankActivate(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Banker = _worldPacket.ReadPackedGuid();
            FullUpdate = _worldPacket.HasBit();
        }

        public ObjectGuid Banker { get; set; }
        public bool FullUpdate { get; set; }
    }

    public class GuildBankBuyTab : ClientPacket
    {
        public GuildBankBuyTab(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Banker = _worldPacket.ReadPackedGuid();
            BankTab = _worldPacket.ReadUInt8();
        }

        public ObjectGuid Banker { get; set; }
        public byte BankTab { get; set; }
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

        public ObjectGuid Banker { get; set; }
        public byte BankTab { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
    }

    public class GuildBankDepositMoney : ClientPacket
    {
        public GuildBankDepositMoney(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Banker = _worldPacket.ReadPackedGuid();
            Money = _worldPacket.ReadUInt64();
        }

        public ObjectGuid Banker { get; set; }
        public ulong Money { get; set; }
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

        public ObjectGuid Banker { get; set; }
        public byte Tab { get; set; }
        public bool FullUpdate { get; set; }
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
            _worldPacket .WriteInt64( RemainingWithdrawMoney);
        }

        public long RemainingWithdrawMoney { get; set; }
    }

    public class GuildBankWithdrawMoney : ClientPacket
    {
        public GuildBankWithdrawMoney(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Banker = _worldPacket.ReadPackedGuid();
            Money = _worldPacket.ReadUInt64();
        }

        public ObjectGuid Banker { get; set; }
        public ulong Money { get; set; }
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
                _worldPacket.WriteBits(tab.Name.Length, 7);
                _worldPacket.WriteBits(tab.Icon.Length, 9);;

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

        public List<GuildBankItemInfo> ItemInfo { get; set; }
        public List<GuildBankTabInfo> TabInfo { get; set; }
        public int WithdrawalsRemaining { get; set; }
        public int Tab { get; set; }
        public ulong Money { get; set; }
        public bool FullUpdate { get; set; }
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

        public ObjectGuid Banker { get; set; }
        public int StackCount { get; set; }
        public int BankItemCount { get; set; }
        public uint ItemID { get; set; }
        public uint ItemID1 { get; set; }
        public byte ToSlot { get; set; }
        public byte BankSlot { get; set; }
        public byte BankSlot1 { get; set; }
        public byte BankTab { get; set; }
        public byte BankTab1 { get; set; }
        public byte ContainerSlot { get; set; }
        public byte ContainerItemSlot { get; set; }
        public bool AutoStore { get; set; }
        public bool BankOnly { get; set; }
    }

    public class GuildBankLogQuery : ClientPacket
    {
        public GuildBankLogQuery(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Tab = _worldPacket.ReadInt32();
        }

        public int Tab { get; set; }
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

        public int Tab { get; set; }
        public List<GuildBankLogEntry> Entry { get; set; }
        public Optional<ulong> WeeklyBonusMoney { get; set; }
    }

    public class GuildBankTextQuery : ClientPacket
    {
        public GuildBankTextQuery(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Tab = _worldPacket.ReadInt32();
        }

        public int Tab { get; set; }
    }

    public class GuildBankTextQueryResult : ServerPacket
    {
        public GuildBankTextQueryResult() : base(ServerOpcodes.GuildBankTextQueryResult) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Tab);

            _worldPacket.WriteBits(Text.Length, 14);
            _worldPacket.WriteString(Text);
        }

        public int Tab { get; set; }
        public string Text { get; set; }
    }

    public class GuildBankSetTabText : ClientPacket
    {
        public GuildBankSetTabText(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Tab = _worldPacket.ReadInt32();
            TabText = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(14));
        }

        public int Tab { get; set; }
        public string TabText { get; set; }
    }

    public class GuildQueryNews : ClientPacket
    {
        public GuildQueryNews(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            GuildGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid GuildGUID { get; set; }
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

        public List<GuildNewsEvent> NewsEvents { get; set; }
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

        public int NewsID { get; set; }
        public ObjectGuid GuildGUID { get; set; }
        public bool Sticky { get; set; }
    }

    public class GuildSetGuildMaster : ClientPacket
    {
        public GuildSetGuildMaster(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint nameLen = _worldPacket.ReadBits<uint>(9);
            NewMasterName = _worldPacket.ReadString(nameLen);
        }

        public string NewMasterName { get; set; }
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

        public int[] CurrentCount { get; set; } = new int[GuildConst.ChallengesTypes];
        public int[] MaxCount { get; set; } = new int[GuildConst.ChallengesTypes];
        public int[] Gold { get; set; } = new int[GuildConst.ChallengesTypes];
        public int[] MaxLevelGold { get; set; } = new int[GuildConst.ChallengesTypes];
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

        public ObjectGuid Vendor { get; set; }
        public uint BStyle { get; set; }
        public uint EStyle { get; set; }
        public uint BColor { get; set; }
        public uint EColor { get; set; }
        public uint Bg { get; set; }
    }

    public class PlayerSaveGuildEmblem : ServerPacket
    {
        public PlayerSaveGuildEmblem() : base(ServerOpcodes.PlayerSaveGuildEmblem) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Error);
        }

        public GuildEmblemError Error { get; set; }
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

        public List<uint> AchievementIDs { get; set; } = new List<uint>();
    }

    class GuildNameChanged : ServerPacket
    {
        public GuildNameChanged() : base(ServerOpcodes.GuildNameChanged) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(GuildGUID);
            _worldPacket.WriteBits(GuildName.Length, 7);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(GuildName);
        }

        public ObjectGuid GuildGUID { get; set; }
        public string GuildName { get; set; }
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

        public int DbID { get; set; }
        public int Rank { get; set; }
        public int Step { get; set; }
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

            data.WriteBits(Name.Length, 6);
            data.WriteBits(Note.Length, 8);
            data.WriteBits(OfficerNote.Length, 8);
            data.WriteBit(Authenticated);
            data.WriteBit(SorEligible);

            data.WriteString(Name);
            data.WriteString(Note);
            data.WriteString(OfficerNote);
        }

        public ObjectGuid Guid { get; set; }
        public long WeeklyXP { get; set; }
        public long TotalXP { get; set; }
        public int RankID { get; set; }
        public int AreaID { get; set; }
        public int PersonalAchievementPoints { get; set; }
        public int GuildReputation { get; set; }
        public int GuildRepToCap { get; set; }
        public float LastSave { get; set; }
        public string Name { get; set; }
        public uint VirtualRealmAddress { get; set; }
        public string Note { get; set; }
        public string OfficerNote { get; set; }
        public byte Status { get; set; }
        public byte Level { get; set; }
        public byte ClassID { get; set; }
        public byte Gender { get; set; }
        public bool Authenticated { get; set; }
        public bool SorEligible { get; set; }
        public GuildRosterProfessionData[] Profession { get; set; } = new GuildRosterProfessionData[2];
    }

    public struct GuildEventEntry
    {
        public ObjectGuid PlayerGUID { get; set; }
        public ObjectGuid OtherGUID { get; set; }
        public byte TransactionType { get; set; }
        public byte RankID { get; set; }
        public uint TransactionDate { get; set; }
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

            data.WriteBits(RankName.Length, 7);
            data.WriteString(RankName);
        }

        public uint RankID { get; set; }
        public uint RankOrder { get; set; }
        public uint Flags { get; set; }
        public uint WithdrawGoldLimit { get; set; }
        public string RankName { get; set; }
        public uint[] TabFlags { get; set; } = new uint[GuildConst.MaxBankTabs];
        public uint[] TabWithdrawItemLimit { get; set; } = new uint[GuildConst.MaxBankTabs];
    }

    public class GuildRewardItem
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(ItemID);
            data.WriteUInt32(Unk4);
            data.WriteUInt32(AchievementsRequired.Count);
            data.WriteUInt32(RaceMask);
            data.WriteUInt32(MinGuildLevel);
            data.WriteUInt32(MinGuildRep);
            data.WriteUInt64(Cost);

            foreach (var achievementId in AchievementsRequired)
                data.WriteUInt32(achievementId);
        }

        public uint ItemID { get; set; }
        public uint Unk4 { get; set; }
        public List<uint> AchievementsRequired { get; set; } = new List<uint>();
        public uint RaceMask { get; set; }
        public int MinGuildLevel { get; set; }
        public int MinGuildRep { get; set; }
        public ulong Cost { get; set; }
    }

    public class GuildBankItemInfo
    {
        public ItemInstance Item { get; set; }
        public int Slot { get; set; }
        public int Count { get; set; }
        public int EnchantmentID { get; set; }
        public int Charges { get; set; }
        public int OnUseEnchantmentID { get; set; }
        public int Flags { get; set; }
        public bool Locked { get; set; }
        public List<ItemGemData> SocketEnchant { get; set; } = new List<ItemGemData>();
    }

    public struct GuildBankTabInfo
    {
        public int TabIndex { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
    }

    public struct GuildBankLogEntry
    {
        public ObjectGuid PlayerGUID { get; set; }
        public uint TimeOffset { get; set; }
        public sbyte EntryType { get; set; }
        public Optional<ulong> Money { get; set; }
        public Optional<int> ItemID { get; set; }
        public Optional<int> Count { get; set; }
        public Optional<sbyte> OtherTab { get; set; }
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

        public int Id { get; set; }
        public uint CompletedDate { get; set; }
        public int Type { get; set; }
        public int Flags { get; set; }
        public int[] Data { get; set; } = new int[2];
        public ObjectGuid MemberGuid { get; set; }
        public List<ObjectGuid> MemberList { get; set; } = new List<ObjectGuid>();
        public Optional<ItemInstance> Item { get; set; }
    }
}
