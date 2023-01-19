// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Game.Networking.Packets
{
    public class GMTicketGetSystemStatus : ClientPacket
    {
        public GMTicketGetSystemStatus(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class GMTicketSystemStatusPkt : ServerPacket
    {
        public GMTicketSystemStatusPkt() : base(ServerOpcodes.GmTicketSystemStatus) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Status);
        }

        public int Status;
    }

    public class GMTicketGetCaseStatus : ClientPacket
    {
        public GMTicketGetCaseStatus(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class GMTicketCaseStatus : ServerPacket
    {
        public GMTicketCaseStatus() : base(ServerOpcodes.GmTicketCaseStatus) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Cases.Count);

            foreach (var c in Cases)
            {
                _worldPacket.WriteInt32(c.CaseID);
                _worldPacket.WriteInt64(c.CaseOpened);
                _worldPacket.WriteInt32(c.CaseStatus);
                _worldPacket.WriteUInt16(c.CfgRealmID);
                _worldPacket.WriteUInt64(c.CharacterID);
                _worldPacket.WriteInt32(c.WaitTimeOverrideMinutes);

                _worldPacket.WriteBits(c.Url.GetByteCount(), 11);
                _worldPacket.WriteBits(c.WaitTimeOverrideMessage.GetByteCount(), 10);

                _worldPacket.WriteString(c.Url);
                _worldPacket.WriteString(c.WaitTimeOverrideMessage);
            }
        }

        public List<GMTicketCase> Cases = new();

        public struct GMTicketCase
        {
            public int CaseID;
            public long CaseOpened;
            public int CaseStatus;
            public ushort CfgRealmID;
            public ulong CharacterID;
            public int WaitTimeOverrideMinutes;
            public string Url;
            public string WaitTimeOverrideMessage;
        }
    }

    public class GMTicketAcknowledgeSurvey : ClientPacket
    {
        public GMTicketAcknowledgeSurvey(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            CaseID = _worldPacket.ReadInt32();
        }

        int CaseID;
    }

    public class SubmitUserFeedback : ClientPacket
    {
        public SupportTicketHeader Header;
        public string Note;
        public bool IsSuggestion;

        public SubmitUserFeedback(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Header.Read(_worldPacket);
            uint noteLength = _worldPacket.ReadBits<uint>(24);
            IsSuggestion = _worldPacket.HasBit();

            if (noteLength != 0)
                Note = _worldPacket.ReadString(noteLength - 1);
        }
    }

    public class SupportTicketSubmitComplaint : ClientPacket
    {
        public SupportTicketSubmitComplaint(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Header.Read(_worldPacket);
            TargetCharacterGUID = _worldPacket.ReadPackedGuid();
            ReportType = _worldPacket.ReadInt32();
            MajorCategory = _worldPacket.ReadInt32();
            MinorCategoryFlags = _worldPacket.ReadInt32();
            ChatLog.Read(_worldPacket);

            uint noteLength = _worldPacket.ReadBits<uint>(10);
            bool hasMailInfo = _worldPacket.HasBit();
            bool hasCalendarInfo = _worldPacket.HasBit();
            bool hasPetInfo = _worldPacket.HasBit();
            bool hasGuildInfo = _worldPacket.HasBit();
            bool hasLFGListSearchResult = _worldPacket.HasBit();
            bool hasLFGListApplicant = _worldPacket.HasBit();
            bool hasClubMessage = _worldPacket.HasBit();
            bool hasClubFinderResult = _worldPacket.HasBit();
            bool hasUnk910 = _worldPacket.HasBit();

            _worldPacket.ResetBitPos();

            if (hasClubMessage)
            {
                SupportTicketCommunityMessage communityMessage = new();
                communityMessage.IsPlayerUsingVoice = _worldPacket.HasBit();
                CommunityMessage = communityMessage;
                _worldPacket.ResetBitPos();
            }

            HorusChatLog.Read(_worldPacket);

            Note = _worldPacket.ReadString(noteLength);

            if (hasMailInfo)
            {
                MailInfo = new();
                MailInfo.Value.Read(_worldPacket);
            }

            if (hasCalendarInfo)
            {
                CalenderInfo = new();
                CalenderInfo.Value.Read(_worldPacket);
            }

            if (hasPetInfo)
            {
                PetInfo = new();
                PetInfo.Value.Read(_worldPacket);
            }

            if (hasGuildInfo)
            {
                GuildInfo = new();
                GuildInfo.Value.Read(_worldPacket);
            }

            if (hasLFGListSearchResult)
            {
                LFGListSearchResult = new();
                LFGListSearchResult.Value.Read(_worldPacket);
            }

            if (hasLFGListApplicant)
            {
                LFGListApplicant = new();
                LFGListApplicant.Value.Read(_worldPacket);
            }

            if (hasClubFinderResult)
            {
                ClubFinderResult = new();
                ClubFinderResult.Value.Read(_worldPacket);
            }

            if (hasUnk910)
            {
                Unused910 = new();
                Unused910.Value.Read(_worldPacket);
            }
        }

        public SupportTicketHeader Header;
        public SupportTicketChatLog ChatLog;
        public ObjectGuid TargetCharacterGUID;
        public int ReportType;
        public int MajorCategory;
        public int MinorCategoryFlags;
        public string Note;
        public SupportTicketHorusChatLog HorusChatLog;
        public SupportTicketMailInfo? MailInfo;
        public SupportTicketCalendarEventInfo? CalenderInfo;
        public SupportTicketPetInfo? PetInfo;
        public SupportTicketGuildInfo? GuildInfo;
        public SupportTicketLFGListSearchResult? LFGListSearchResult;
        public SupportTicketLFGListApplicant? LFGListApplicant;
        public SupportTicketCommunityMessage? CommunityMessage;
        public SupportTicketClubFinderResult? ClubFinderResult;
        public SupportTicketUnused910? Unused910;

        public struct SupportTicketChatLine
        {
            public long Timestamp;
            public string Text;

            public SupportTicketChatLine(WorldPacket data)
            {
                Timestamp = data.ReadInt64();
                Text = data.ReadString(data.ReadBits<uint>(12));
            }

            public SupportTicketChatLine(long timestamp, string text)
            {
                Timestamp = timestamp;
                Text = text;
            }

            public void Read(WorldPacket data)
            {
                Timestamp = data.ReadUInt32();
                Text = data.ReadString(data.ReadBits<uint>(12));
            }
        }

        public class SupportTicketChatLog
        {
            public void Read(WorldPacket data)
            {
                uint linesCount = data.ReadUInt32();
                bool hasReportLineIndex = data.HasBit();

                data.ResetBitPos();

                for (uint i = 0; i < linesCount; i++)
                    Lines.Add(new SupportTicketChatLine(data));

                if (hasReportLineIndex)
                    ReportLineIndex = data.ReadUInt32();
            }

            public List<SupportTicketChatLine> Lines = new();
            public uint? ReportLineIndex;
        }

        public struct SupportTicketHorusChatLine
        {
            public void Read(WorldPacket data)
            {
                Timestamp = data.ReadInt64();
                AuthorGUID = data.ReadPackedGuid();

                bool hasClubID = data.HasBit();
                bool hasChannelGUID = data.HasBit();
                bool hasRealmAddress = data.HasBit();
                bool hasSlashCmd = data.HasBit();
                uint textLength = data.ReadBits<uint>(12);

                if (hasClubID)
                    ClubID = data.ReadUInt64();

                if (hasChannelGUID)
                    ChannelGUID = data.ReadPackedGuid();

                if (hasRealmAddress)
                {
                    SenderRealm senderRealm = new();
                    senderRealm.VirtualRealmAddress = data.ReadUInt32();
                    senderRealm.field_4 = data.ReadUInt16();
                    senderRealm.field_6 = data.ReadUInt8();
                    RealmAddress = senderRealm;
                }

                if (hasSlashCmd)
                    SlashCmd = data.ReadInt32();

                Text = data.ReadString(textLength);
            }

            public struct SenderRealm
            {
                public uint VirtualRealmAddress;
                public ushort field_4;
                public byte field_6;
            }

            public long Timestamp;
            public ObjectGuid AuthorGUID;
            public ulong? ClubID;
            public ObjectGuid? ChannelGUID;
            public SenderRealm? RealmAddress;
            public int? SlashCmd;
            public string Text;
        }

        public class SupportTicketHorusChatLog
        {
            public List<SupportTicketHorusChatLine> Lines = new();

            public void Read(WorldPacket data)
            {
                uint linesCount = data.ReadUInt32();
                data.ResetBitPos();

                for (uint i = 0; i < linesCount; i++)
                {
                    var chatLine = new SupportTicketHorusChatLine();
                    chatLine.Read(data);
                    Lines.Add(chatLine);
                }
            }
        }

        public struct SupportTicketMailInfo
        {
            public void Read(WorldPacket data)
            {
                MailID = data.ReadInt32();
                uint bodyLength = data.ReadBits<uint>(13);
                uint subjectLength = data.ReadBits<uint>(9);

                MailBody = data.ReadString(bodyLength);
                MailSubject = data.ReadString(subjectLength);
            }

            public int MailID;
            public string MailSubject;
            public string MailBody;
        }

        public struct SupportTicketCalendarEventInfo
        {
            public void Read(WorldPacket data)
            {
                EventID = data.ReadUInt64();
                InviteID = data.ReadUInt64();

                EventTitle = data.ReadString(data.ReadBits<byte>(8));
            }

            public ulong EventID;
            public ulong InviteID;
            public string EventTitle;
        }

        public struct SupportTicketPetInfo
        {
            public void Read(WorldPacket data)
            {
                PetID = data.ReadPackedGuid();

                PetName = data.ReadString(data.ReadBits<byte>(8));
            }

            public ObjectGuid PetID;
            public string PetName;
        }

        public struct SupportTicketGuildInfo
        {
            public void Read(WorldPacket data)
            {
                byte nameLength = data.ReadBits<byte>(8);
                GuildID = data.ReadPackedGuid();

                GuildName = data.ReadString(nameLength);
            }

            public ObjectGuid GuildID;
            public string GuildName;
        }

        public struct SupportTicketLFGListSearchResult
        {
            public void Read(WorldPacket data)
            {
                RideTicket = new RideTicket();
                RideTicket.Read(data);

                GroupFinderActivityID = data.ReadUInt32();
                LastTitleAuthorGuid = data.ReadPackedGuid();
                LastDescriptionAuthorGuid = data.ReadPackedGuid();
                LastVoiceChatAuthorGuid = data.ReadPackedGuid();
                ListingCreatorGuid = data.ReadPackedGuid();
                Unknown735 = data.ReadPackedGuid();

                byte titleLength = data.ReadBits<byte>(10);
                byte descriptionLength = data.ReadBits<byte>(11);
                byte voiceChatLength = data.ReadBits<byte>(8);

                Title = data.ReadString(titleLength);
                Description = data.ReadString(descriptionLength);
                VoiceChat = data.ReadString(voiceChatLength);
            }

            public RideTicket RideTicket;
            public uint GroupFinderActivityID;
            public ObjectGuid LastTitleAuthorGuid;
            public ObjectGuid LastDescriptionAuthorGuid;
            public ObjectGuid LastVoiceChatAuthorGuid;
            public ObjectGuid ListingCreatorGuid;
            public ObjectGuid Unknown735;
            public string Title;
            public string Description;
            public string VoiceChat;
        }

        public struct SupportTicketLFGListApplicant
        {
            public void Read(WorldPacket data)
            {
                RideTicket = new RideTicket();
                RideTicket.Read(data);

                Comment = data.ReadString(data.ReadBits<uint>(9));
            }

            public RideTicket RideTicket;
            public string Comment;
        }

        public struct SupportTicketCommunityMessage
        {
            public bool IsPlayerUsingVoice;
        }

        public struct SupportTicketClubFinderResult
        {
            public ulong ClubFinderPostingID;
            public ulong ClubID;
            public ObjectGuid ClubFinderGUID;
            public string ClubName;

            public void Read(WorldPacket data)
            {
                ClubFinderPostingID = data.ReadUInt64();
                ClubID = data.ReadUInt64();
                ClubFinderGUID = data.ReadPackedGuid();
                ClubName = data.ReadString(data.ReadBits<uint>(12));
            }
        }

        public struct SupportTicketUnused910
        {
            public string field_0;
            public ObjectGuid field_104;

            public void Read(WorldPacket data)
            {
                uint field_0Length = data.ReadBits<uint>(7);
                field_104 = data.ReadPackedGuid();
                field_0 = data.ReadString(field_0Length);
            }
        }
    }

    class Complaint : ClientPacket
    {
        public Complaint(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ComplaintType = (SupportSpamType)_worldPacket.ReadUInt8();
            Offender.Read(_worldPacket);

            switch (ComplaintType)
            {
                case SupportSpamType.Mail:
                    MailID = _worldPacket.ReadUInt32();
                    break;
                case SupportSpamType.Chat:
                    Chat.Read(_worldPacket);
                    break;
                case SupportSpamType.Calendar:
                    EventGuid = _worldPacket.ReadUInt64();
                    InviteGuid = _worldPacket.ReadUInt64();
                    break;
            }
        }

        public SupportSpamType ComplaintType;
        ComplaintOffender Offender;
        uint MailID;
        ComplaintChat Chat;

        ulong EventGuid;
        ulong InviteGuid;

        struct ComplaintOffender
        {
            public void Read(WorldPacket data)
            {
                PlayerGuid = data.ReadPackedGuid();
                RealmAddress = data.ReadUInt32();
                TimeSinceOffence = data.ReadUInt32();
            }

            public ObjectGuid PlayerGuid;
            public uint RealmAddress;
            public uint TimeSinceOffence;
        }

        struct ComplaintChat
        {
            public void Read(WorldPacket data)
            {
                Command = data.ReadUInt32();
                ChannelID = data.ReadUInt32();
                MessageLog = data.ReadString(data.ReadBits<uint>(12));
            }

            public uint Command;
            public uint ChannelID;
            public string MessageLog;
        }
    }

    public class ComplaintResult : ServerPacket
    {
        public ComplaintResult() : base(ServerOpcodes.ComplaintResult) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32((uint)ComplaintType);
            _worldPacket.WriteUInt8(Result);
        }

        public SupportSpamType ComplaintType;
        public byte Result;
    }

    class BugReport : ClientPacket
    {
        public BugReport(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Type = _worldPacket.ReadBit();
            uint diagLen = _worldPacket.ReadBits<uint>(12);
            uint textLen = _worldPacket.ReadBits<uint>(10);
            DiagInfo = _worldPacket.ReadString(diagLen);
            Text = _worldPacket.ReadString(textLen);
        }

        public uint Type;
        public string Text;
        public string DiagInfo;
    }

    //Structs
    public struct SupportTicketHeader
    {
        public void Read(WorldPacket packet)
        {
            MapID = packet.ReadUInt32();
            Position = packet.ReadVector3();
            Facing = packet.ReadFloat();
            Program = packet.ReadInt32();
        }

        public uint MapID;
        public Vector3 Position;
        public float Facing;
        public int Program;
    }
}
