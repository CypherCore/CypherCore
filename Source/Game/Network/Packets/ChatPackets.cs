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
using Game.Entities;
using Game.Groups;

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

        public string Text { get; set; }
        public Language Language { get; set; } = Language.Universal;
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

        public Language Language { get; set; } = Language.Universal;
        public string Text { get; set; }
        public string Target { get; set; }
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

        public Language Language { get; set; } = Language.Universal;
        public string Text { get; set; }
        public string Target { get; set; }
    }

    public class ChatAddonMessage : ClientPacket
    {
        public ChatAddonMessage(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint prefixLen = _worldPacket.ReadBits<uint>(5);
            uint textLen = _worldPacket.ReadBits<uint>(9);
            Prefix = _worldPacket.ReadString(prefixLen);
            Text = _worldPacket.ReadString(textLen);
        }

        public string Prefix { get; set; }
        public string Text { get; set; }
    }

    public class ChatAddonMessageWhisper : ClientPacket
    {
        public ChatAddonMessageWhisper(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint targetLen = _worldPacket.ReadBits<uint>(9);
            uint prefixLen = _worldPacket.ReadBits<uint>(5);
            uint textLen = _worldPacket.ReadBits<uint>(9);
            Target = _worldPacket.ReadString(targetLen);
            Prefix = _worldPacket.ReadString(prefixLen);
            Text = _worldPacket.ReadString(textLen);
        }

        public string Prefix { get; set; }
        public string Target { get; set; }
        public string Text { get; set; }
    }

    class ChatAddonMessageChannel : ClientPacket
    {
        public ChatAddonMessageChannel(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint targetLen = _worldPacket.ReadBits<uint>(9);
            uint prefixLen = _worldPacket.ReadBits<uint>(5);
            uint textLen = _worldPacket.ReadBits<uint>(9);
            Target = _worldPacket.ReadString(targetLen);
            Prefix = _worldPacket.ReadString(prefixLen);
            Text = _worldPacket.ReadString(textLen);
        }

        public string Text { get; set; }
        public string Target { get; set; }
        public string Prefix { get; set; }
    }

    public class ChatMessageDND : ClientPacket
    {
        public ChatMessageDND(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint len = _worldPacket.ReadBits<uint>(9);
            Text = _worldPacket.ReadString(len);
        }

        public string Text { get; set; }
    }

    public class ChatMessageAFK : ClientPacket
    {
        public ChatMessageAFK(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint len = _worldPacket.ReadBits<uint>(9);
            Text = _worldPacket.ReadString(len);
        }

        public string Text { get; set; }
    }

    public class ChatMessageEmote : ClientPacket
    {
        public ChatMessageEmote(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint len = _worldPacket.ReadBits<uint>(9);
            Text = _worldPacket.ReadString(len);
        }

        public string Text { get; set; }
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
            _worldPacket.WriteInt8(_Language);
            _worldPacket.WritePackedGuid(SenderGUID);
            _worldPacket.WritePackedGuid(SenderGuildGUID);
            _worldPacket.WritePackedGuid(SenderAccountGUID);
            _worldPacket.WritePackedGuid(TargetGUID);
            _worldPacket.WriteUInt32(TargetVirtualAddress);
            _worldPacket.WriteUInt32(SenderVirtualAddress);
            _worldPacket.WritePackedGuid(PartyGUID);
            _worldPacket.WriteUInt32(AchievementID);
            _worldPacket.WriteFloat(DisplayTime);
            _worldPacket.WriteBits(SenderName.Length, 11);
            _worldPacket.WriteBits(TargetName.Length, 11);
            _worldPacket.WriteBits(Prefix.Length, 5);
            _worldPacket.WriteBits(Channel.Length, 7);
            _worldPacket.WriteBits(ChatText.Length, 12);
            _worldPacket.WriteBits((uint)_ChatFlags, 11);
            _worldPacket.WriteBit(HideChatLog);
            _worldPacket.WriteBit(FakeSenderName);

            _worldPacket.WriteString(SenderName);
            _worldPacket.WriteString(TargetName);
            _worldPacket.WriteString(Prefix);
            _worldPacket.WriteString(Channel);
            _worldPacket.WriteString(ChatText);
        }

        public ChatMsg SlashCmd { get; set; } = 0;
        public Language _Language { get; set; } = Language.Universal;
        public ObjectGuid SenderGUID { get; set; }
        public ObjectGuid SenderGuildGUID { get; set; }
        public ObjectGuid SenderAccountGUID { get; set; }
        public ObjectGuid TargetGUID { get; set; }
        public ObjectGuid PartyGUID { get; set; }
        public uint SenderVirtualAddress { get; set; }
        public uint TargetVirtualAddress { get; set; }
        public string SenderName { get; set; } = "";
        public string TargetName { get; set; } = "";
        public string Prefix { get; set; } = "";
        public string Channel { get; set; } = "";
        public string ChatText { get; set; } = "";
        public uint AchievementID { get; set; } = 0;
        public ChatFlags _ChatFlags { get; set; } = 0;
        public float DisplayTime { get; set; } = 0.0f;
        public bool HideChatLog { get; set; } = false;
        public bool FakeSenderName { get; set; } = false;
    }

    public class EmoteMessage : ServerPacket
    {
        public EmoteMessage() : base(ServerOpcodes.Emote, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
            _worldPacket.WriteUInt32(EmoteID);
        }

        public ObjectGuid Guid { get; set; }
        public int EmoteID { get; set; }
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

        public ObjectGuid Target { get; set; }
        public int EmoteID { get; set; }
        public int SoundIndex { get; set; }
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

        public ObjectGuid SourceGUID { get; set; }
        public ObjectGuid SourceAccountGUID { get; set; }
        public ObjectGuid TargetGUID { get; set; }
        public int SoundIndex { get; set; } = -1;
        public int EmoteID { get; set; }
    }

    public class PrintNotification : ServerPacket
    {
        public PrintNotification(string notifyText) : base(ServerOpcodes.PrintNotification)
        {
            NotifyText = notifyText;
        }

        public override void Write()
        {
            _worldPacket.WriteBits(NotifyText.Length, 12);
            _worldPacket.WriteString(NotifyText);
        }

        public string NotifyText { get; set; }
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
            _worldPacket.WriteBits(Name.Length, 9);
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
            _worldPacket.WriteBits(Name.Length, 9);
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

            _worldPacket.WriteBits(StringParam.Length, 11);
            _worldPacket.WriteString(StringParam);
        }

        public int MessageID { get; set; }
        public string StringParam { get; set; } = "";
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

        public string[] Prefixes { get; set; } = new string[64];
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
            _worldPacket .WriteUInt32(ZoneID);
            _worldPacket.WriteBits(MessageText.Length, 12);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(MessageText);
        }

        public uint ZoneID { get; set; }
        public string MessageText { get; set; } = "";
    }

    class ChatReportIgnored : ClientPacket
    {
        public ChatReportIgnored(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            IgnoredGUID = _worldPacket.ReadPackedGuid();
            Reason = _worldPacket.ReadUInt8();
        }

        public ObjectGuid IgnoredGUID { get; set; }
        public byte Reason { get; set; }
    }
}
