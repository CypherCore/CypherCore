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
using Game.Entities;
using Game.Groups;
using System;
using Framework.Dynamic;

namespace Game.Network.Packets
{
    public class ChatMessage : ClientPacket
    {
        public ChatMessage(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Language = (Language)_worldPacket.ReadInt32();
            uint len = _worldPacket.ReadBits<uint>(9);
            Text = _worldPacket.ReadString(len);
        }

        public string Text;
        public Language Language = Language.Universal;
    }

    public class ChatMessageWhisper : ClientPacket
    {
        public ChatMessageWhisper(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Language = (Language)_worldPacket.ReadInt32();
            uint targetLen = _worldPacket.ReadBits<uint>(9);
            uint textLen = _worldPacket.ReadBits<uint>(9);
            Target = _worldPacket.ReadString(targetLen);
            Text = _worldPacket.ReadString(textLen);
        }

        public Language Language = Language.Universal;
        public string Text;
        public string Target;
    }

    public class ChatMessageChannel : ClientPacket
    {
        public ChatMessageChannel(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Language = (Language)_worldPacket.ReadInt32();
            uint targetLen = _worldPacket.ReadBits<uint>(9);
            uint textLen = _worldPacket.ReadBits<uint>(9);
            Target = _worldPacket.ReadString(targetLen);
            Text = _worldPacket.ReadString(textLen);
        }

        public Language Language = Language.Universal;
        public string Text;
        public string Target;
    }

    public class ChatAddonMessage : ClientPacket
    {
        public ChatAddonMessage(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Params.Read(_worldPacket);
        }

        public ChatAddonMessageParams Params = new ChatAddonMessageParams();
    }

    class ChatAddonMessageTargeted : ClientPacket
    {
        public ChatAddonMessageTargeted(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint targetLen = _worldPacket.ReadBits<uint>(9);
            Params.Read(_worldPacket);
            Target = _worldPacket.ReadString(targetLen);
        }

        public string Target;
        public ChatAddonMessageParams Params = new ChatAddonMessageParams();
    }

    public class ChatMessageDND : ClientPacket
    {
        public ChatMessageDND(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint len = _worldPacket.ReadBits<uint>(9);
            Text = _worldPacket.ReadString(len);
        }

        public string Text;
    }

    public class ChatMessageAFK : ClientPacket
    {
        public ChatMessageAFK(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint len = _worldPacket.ReadBits<uint>(9);
            Text = _worldPacket.ReadString(len);
        }

        public string Text;
    }

    public class ChatMessageEmote : ClientPacket
    {
        public ChatMessageEmote(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint len = _worldPacket.ReadBits<uint>(9);
            Text = _worldPacket.ReadString(len);
        }

        public string Text;
    }

    public class ChatPkt : ServerPacket
    {
        public ChatPkt() : base(ServerOpcodes.Chat) { }

        public void Initialize(ChatMsg chatType, Language language, WorldObject sender, WorldObject receiver, string message, uint achievementId = 0, string channelName = "", LocaleConstant locale = LocaleConstant.enUS, string addonPrefix = "")
        {
            // Clear everything because same packet can be used multiple times
            Clear();

            SenderGUID.Clear();
            SenderAccountGUID.Clear();
            SenderGuildGUID.Clear();
            PartyGUID.Clear();
            TargetGUID.Clear();
            SenderName = "";
            TargetName = "";
            _ChatFlags = ChatFlags.None;

            SlashCmd = chatType;
            _Language = language;

            if (sender)
                SetSender(sender, locale);

            if (receiver)
                SetReceiver(receiver, locale);

            SenderVirtualAddress = Global.WorldMgr.GetVirtualRealmAddress();
            TargetVirtualAddress = Global.WorldMgr.GetVirtualRealmAddress();
            AchievementID = achievementId;
            Channel = channelName;
            Prefix = addonPrefix;
            ChatText = message;
        }

        void SetSender(WorldObject sender, LocaleConstant locale)
        {
            SenderGUID = sender.GetGUID();

            Creature creatureSender = sender.ToCreature();
            if (creatureSender)
                SenderName = creatureSender.GetName(locale);

            Player playerSender = sender.ToPlayer();
            if (playerSender)
            {
                SenderAccountGUID = playerSender.GetSession().GetAccountGUID();
                _ChatFlags = playerSender.GetChatFlags();

                SenderGuildGUID = ObjectGuid.Create(HighGuid.Guild, playerSender.GetGuildId());

                Group group = playerSender.GetGroup();
                if (group)
                    PartyGUID = group.GetGUID();
            }
        }

        public void SetReceiver(WorldObject receiver, LocaleConstant locale)
        {
            TargetGUID = receiver.GetGUID();

            Creature creatureReceiver = receiver.ToCreature();
            if (creatureReceiver)
                TargetName = creatureReceiver.GetName(locale);
        }

        public override void Write()
        {
            _worldPacket.WriteUInt8(SlashCmd);
            _worldPacket.WriteUInt32(_Language);
            _worldPacket.WritePackedGuid(SenderGUID);
            _worldPacket.WritePackedGuid(SenderGuildGUID);
            _worldPacket.WritePackedGuid(SenderAccountGUID);
            _worldPacket.WritePackedGuid(TargetGUID);
            _worldPacket.WriteUInt32(TargetVirtualAddress);
            _worldPacket.WriteUInt32(SenderVirtualAddress);
            _worldPacket.WritePackedGuid(PartyGUID);
            _worldPacket.WriteUInt32(AchievementID);
            _worldPacket.WriteFloat(DisplayTime);
            _worldPacket.WriteBits(SenderName.GetByteCount(), 11);
            _worldPacket.WriteBits(TargetName.GetByteCount(), 11);
            _worldPacket.WriteBits(Prefix.GetByteCount(), 5);
            _worldPacket.WriteBits(Channel.GetByteCount(), 7);
            _worldPacket.WriteBits(ChatText.GetByteCount(), 12);
            _worldPacket.WriteBits((byte)_ChatFlags, 11);
            _worldPacket.WriteBit(HideChatLog);
            _worldPacket.WriteBit(FakeSenderName);
            _worldPacket.WriteBit(Unused_801.HasValue);
            _worldPacket.FlushBits();

            _worldPacket.WriteString(SenderName);
            _worldPacket.WriteString(TargetName);
            _worldPacket.WriteString(Prefix);
            _worldPacket.WriteString(Channel);
            _worldPacket.WriteString(ChatText);

            if (Unused_801.HasValue)
                _worldPacket.WriteUInt32(Unused_801.Value);
        }

        public ChatMsg SlashCmd = 0;
        public Language _Language = Language.Universal;
        public ObjectGuid SenderGUID;
        public ObjectGuid SenderGuildGUID;
        public ObjectGuid SenderAccountGUID;
        public ObjectGuid TargetGUID;
        public ObjectGuid PartyGUID;
        public uint SenderVirtualAddress;
        public uint TargetVirtualAddress;
        public string SenderName = "";
        public string TargetName = "";
        public string Prefix = "";
        public string Channel = "";
        public string ChatText = "";
        public uint AchievementID;
        public ChatFlags _ChatFlags = 0;
        public float DisplayTime = 0.0f;
        public Optional<uint> Unused_801;
        public bool HideChatLog = false;
        public bool FakeSenderName = false;
    }

    public class EmoteMessage : ServerPacket
    {
        public EmoteMessage() : base(ServerOpcodes.Emote, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
            _worldPacket.WriteUInt32(EmoteID);
        }

        public ObjectGuid Guid;
        public int EmoteID;
    }

    public class CTextEmote : ClientPacket
    {
        public CTextEmote(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Target = _worldPacket.ReadPackedGuid();
            EmoteID = _worldPacket.ReadInt32();
            SoundIndex = _worldPacket.ReadInt32();
        }

        public ObjectGuid Target;
        public int EmoteID;
        public int SoundIndex;
    }

    public class STextEmote : ServerPacket
    {
        public STextEmote() : base(ServerOpcodes.TextEmote, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(SourceGUID);
            _worldPacket.WritePackedGuid(SourceAccountGUID);
            _worldPacket.WriteUInt32(EmoteID);
            _worldPacket.WriteInt32(SoundIndex);
            _worldPacket.WritePackedGuid(TargetGUID);
        }

        public ObjectGuid SourceGUID;
        public ObjectGuid SourceAccountGUID;
        public ObjectGuid TargetGUID;
        public int SoundIndex = -1;
        public int EmoteID;
    }

    public class PrintNotification : ServerPacket
    {
        public PrintNotification(string notifyText) : base(ServerOpcodes.PrintNotification)
        {
            NotifyText = notifyText;
        }

        public override void Write()
        {
            _worldPacket.WriteBits(NotifyText.GetByteCount(), 12);
            _worldPacket.WriteString(NotifyText);
        }

        public string NotifyText;
    }

    public class EmoteClient : ClientPacket
    {
        public EmoteClient(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class ChatPlayerNotfound : ServerPacket
    {
        public ChatPlayerNotfound(string name) : base(ServerOpcodes.ChatPlayerNotfound)
        {
            Name = name;
        }

        public override void Write()
        {
            _worldPacket.WriteBits(Name.GetByteCount(), 9);
            _worldPacket.WriteString(Name);
        }

        string Name;
    }

    class ChatPlayerAmbiguous : ServerPacket
    {
        public ChatPlayerAmbiguous(string name) : base(ServerOpcodes.ChatPlayerAmbiguous)
        {
            Name = name;
        }

        public override void Write()
        {
            _worldPacket.WriteBits(Name.GetByteCount(), 9);
            _worldPacket.WriteString(Name);
        }

        string Name;
    }

    class ChatRestricted : ServerPacket
    {
        public ChatRestricted(ChatRestrictionType reason) : base(ServerOpcodes.ChatRestricted)
        {
            Reason = reason;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt8(Reason);
        }

        ChatRestrictionType Reason;
    }

    class ChatServerMessage : ServerPacket
    {
        public ChatServerMessage() : base(ServerOpcodes.ChatServerMessage) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(MessageID);

            _worldPacket.WriteBits(StringParam.GetByteCount(), 11);
            _worldPacket.WriteString(StringParam);
        }

        public int MessageID;
        public string StringParam = "";
    }

    class ChatRegisterAddonPrefixes : ClientPacket
    {
        public ChatRegisterAddonPrefixes(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            int count = _worldPacket.ReadInt32();

            for (int i = 0; i < count && i < 64; ++i)
                Prefixes[i] = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(5));
        }

        public string[] Prefixes = new string[64];
    }

    class ChatUnregisterAllAddonPrefixes : ClientPacket
    {
        public ChatUnregisterAllAddonPrefixes(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class DefenseMessage : ServerPacket
    {
        public DefenseMessage() : base(ServerOpcodes.DefenseMessage) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(ZoneID);
            _worldPacket.WriteBits(MessageText.GetByteCount(), 12);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(MessageText);
        }

        public uint ZoneID;
        public string MessageText = "";
    }

    class ChatReportIgnored : ClientPacket
    {
        public ChatReportIgnored(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            IgnoredGUID = _worldPacket.ReadPackedGuid();
            Reason = _worldPacket.ReadUInt8();
        }

        public ObjectGuid IgnoredGUID;
        public byte Reason;
    }

    public class ChatAddonMessageParams
    {
        public void Read(WorldPacket data)
        {
            uint prefixLen = data.ReadBits<uint>(5);
            uint textLen = data.ReadBits<uint>(8);
            IsLogged = data.HasBit();
            Type = (ChatMsg)data.ReadInt32();
            Prefix = data.ReadString(prefixLen);
            Text = data.ReadString(textLen);
        }

        public string Prefix;
        public string Text;
        public ChatMsg Type = ChatMsg.Party;
        public bool IsLogged;
    }
}
