﻿/*
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
                _worldPacket.WriteInt32(c.CaseOpened);
                _worldPacket.WriteInt32(c.CaseStatus);
                _worldPacket.WriteInt16(c.CfgRealmID);
                _worldPacket.WriteInt64(c.CharacterID);
                _worldPacket.WriteInt32(c.WaitTimeOverrideMinutes);

                _worldPacket.WriteBits(c.Url.GetByteCount(), 11);
                _worldPacket.WriteBits(c.WaitTimeOverrideMessage.GetByteCount(), 10);

                _worldPacket.WriteString(c.Url);
                _worldPacket.WriteString(c.WaitTimeOverrideMessage);
            }
        }

        public List<GMTicketCase> Cases = new List<GMTicketCase>();

        public struct GMTicketCase
        {
            public int CaseID;
            public int CaseOpened;
            public int CaseStatus;
            public short CfgRealmID;
            public long CharacterID;
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

        private int CaseID;
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
            var noteLength = _worldPacket.ReadBits<uint>(24);
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
            ChatLog.Read(_worldPacket);
            ComplaintType = _worldPacket.ReadBits<byte>(5);

            var noteLength = _worldPacket.ReadBits<uint>(10);
            var hasMailInfo = _worldPacket.HasBit();
            var hasCalendarInfo = _worldPacket.HasBit();
            var hasPetInfo = _worldPacket.HasBit();
            var hasGuildInfo = _worldPacket.HasBit();
            var hasLFGListSearchResult = _worldPacket.HasBit();
            var hasLFGListApplicant = _worldPacket.HasBit();
            var hasClubMessage = _worldPacket.HasBit();
            var hasClubFinderResult = _worldPacket.HasBit();

            _worldPacket.ResetBitPos();

            if (hasClubMessage)
            {
                CommunityMessage.HasValue = true;
                CommunityMessage.Value.IsPlayerUsingVoice = _worldPacket.HasBit();
                _worldPacket.ResetBitPos();
            }

            HorusChatLog.Read(_worldPacket);

            Note = _worldPacket.ReadString(noteLength);

            if (hasMailInfo)
            {
                MailInfo.HasValue = true;
                MailInfo.Value.Read(_worldPacket);
            }

            if (hasCalendarInfo)
            {
                CalenderInfo.HasValue = true;
                CalenderInfo.Value.Read(_worldPacket);
            }

            if (hasPetInfo)
            {
                PetInfo.HasValue = true;
                PetInfo.Value.Read(_worldPacket);
            }

            if (hasGuildInfo)
            {
                GuildInfo.HasValue = true;
                GuildInfo.Value.Read(_worldPacket);
            }

            if (hasLFGListSearchResult)
            {
                LFGListSearchResult.HasValue = true;
                LFGListSearchResult.Value.Read(_worldPacket);
            }

            if (hasLFGListApplicant)
            {
                LFGListApplicant.HasValue = true;
                LFGListApplicant.Value.Read(_worldPacket);
            }

            if (hasClubFinderResult)
            {
                ClubFinderResult.HasValue = true;
                ClubFinderResult.Value.Read(_worldPacket);
            }
        }

        public SupportTicketHeader Header;
        public SupportTicketChatLog ChatLog;
        public ObjectGuid TargetCharacterGUID;
        public byte ComplaintType;
        public string Note;
        public SupportTicketHorusChatLog HorusChatLog;
        public Optional<SupportTicketMailInfo> MailInfo;
        public Optional<SupportTicketCalendarEventInfo> CalenderInfo;
        public Optional<SupportTicketPetInfo> PetInfo;
        public Optional<SupportTicketGuildInfo> GuildInfo;
        public Optional<SupportTicketLFGListSearchResult> LFGListSearchResult;
        public Optional<SupportTicketLFGListApplicant> LFGListApplicant;
        public Optional<SupportTicketCommunityMessage> CommunityMessage;
        public Optional<SupportTicketClubFinderResult> ClubFinderResult;

        public struct SupportTicketChatLine
        {
            public uint Timestamp;
            public string Text;

            public SupportTicketChatLine(WorldPacket data)
            {
                Timestamp = data.ReadUInt32();
                Text = data.ReadString(data.ReadBits<uint>(12));
            }

            public SupportTicketChatLine(uint timestamp, string text)
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
                var linesCount = data.ReadUInt32();
                ReportLineIndex.HasValue = data.HasBit();
                data.ResetBitPos();

                for (uint i = 0; i < linesCount; i++)
                    Lines.Add(new SupportTicketChatLine(data));

                if (ReportLineIndex.HasValue)
                    ReportLineIndex.Value = data.ReadUInt32();
            }

            public List<SupportTicketChatLine> Lines = new List<SupportTicketChatLine>();
            public Optional<uint> ReportLineIndex;
        }

        public struct SupportTicketHorusChatLine
        {
            public void Read(WorldPacket data)
            {
                Timestamp = data.ReadInt32();
                AuthorGUID = data.ReadPackedGuid();

                var hasClubID = data.HasBit();
                var hasChannelGUID = data.HasBit();
                var hasRealmAddress = data.HasBit();
                var hasSlashCmd = data.HasBit();
                var textLength = data.ReadBits<uint>(12);

                if (hasClubID)
                {
                    ClubID.HasValue = true;
                    ClubID.Value = data.ReadUInt64();
                }

                if (hasChannelGUID)
                {
                    ChannelGUID.HasValue = true;
                    ChannelGUID.Value = data.ReadPackedGuid();
                }

                if (hasRealmAddress)
                {
                    RealmAddress.HasValue = true;
                    RealmAddress.Value.VirtualRealmAddress = data.ReadUInt32();
                    RealmAddress.Value.field_4 = data.ReadUInt16();
                    RealmAddress.Value.field_6 = data.ReadUInt8();
                }

                if (hasSlashCmd)
                {
                    SlashCmd.HasValue = true;
                    SlashCmd.Value = data.ReadInt32();
                }

                Text = data.ReadString(textLength);
            }

            public struct SenderRealm
            {
                public uint VirtualRealmAddress;
                public ushort field_4;
                public byte field_6;
            }

            public int Timestamp;
            public ObjectGuid AuthorGUID;
            public Optional<ulong> ClubID;
            public Optional<ObjectGuid> ChannelGUID;
            public Optional<SenderRealm> RealmAddress;
            public Optional<int> SlashCmd;
            public string Text;
        }

        public class SupportTicketHorusChatLog
        {
            public List<SupportTicketHorusChatLine> Lines = new List<SupportTicketHorusChatLine>();

            public void Read(WorldPacket data)
            {
                var linesCount = data.ReadUInt32();
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
                var bodyLength = data.ReadBits<uint>(13);
                var subjectLength = data.ReadBits<uint>(9);

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
                var nameLength = data.ReadBits<byte>(8);
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

                var titleLength = data.ReadBits<byte>(8);
                var descriptionLength = data.ReadBits<byte>(11);
                var voiceChatLength = data.ReadBits<byte>(8);

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
    }

    internal class Complaint : ClientPacket
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
        private ComplaintOffender Offender;
        private uint MailID;
        private ComplaintChat Chat;

        private ulong EventGuid;
        private ulong InviteGuid;

        private struct ComplaintOffender
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

        private struct ComplaintChat
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

    internal class BugReport : ClientPacket
    {
        public BugReport(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Type = _worldPacket.ReadBit();
            var diagLen = _worldPacket.ReadBits<uint>(12);
            var textLen = _worldPacket.ReadBits<uint>(10);
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
        }

        public uint MapID;
        public Vector3 Position;
        public float Facing;
    }
}
