// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Groups;

namespace Game.Networking.Packets
{
    public class ChatMessage : ClientPacket
    {
        public Language Language = Language.Universal;

        public string Text;

        public ChatMessage(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Language = (Language)_worldPacket.ReadInt32();
            uint len = _worldPacket.ReadBits<uint>(11);
            Text = _worldPacket.ReadString(len);
        }
    }

    public class ChatMessageWhisper : ClientPacket
    {
        public Language Language = Language.Universal;
        public string Target;
        public string Text;

        public ChatMessageWhisper(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Language = (Language)_worldPacket.ReadInt32();
            uint targetLen = _worldPacket.ReadBits<uint>(9);
            uint textLen = _worldPacket.ReadBits<uint>(11);
            Target = _worldPacket.ReadString(targetLen);
            Text = _worldPacket.ReadString(textLen);
        }
    }

    public class ChatMessageChannel : ClientPacket
    {
        public ObjectGuid ChannelGUID;

        public Language Language = Language.Universal;
        public string Target;
        public string Text;

        public ChatMessageChannel(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Language = (Language)_worldPacket.ReadInt32();
            ChannelGUID = _worldPacket.ReadPackedGuid();
            uint targetLen = _worldPacket.ReadBits<uint>(9);
            uint textLen = _worldPacket.ReadBits<uint>(11);
            Target = _worldPacket.ReadString(targetLen);
            Text = _worldPacket.ReadString(textLen);
        }
    }

    public class ChatAddonMessage : ClientPacket
    {
        public ChatAddonMessageParams Params = new();

        public ChatAddonMessage(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Params.Read(_worldPacket);
        }
    }

    internal class ChatAddonMessageTargeted : ClientPacket
    {
        public ObjectGuid? ChannelGUID; // not optional in the packet. Optional for api reasons
        public ChatAddonMessageParams Params = new();

        public string Target;

        public ChatAddonMessageTargeted(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            uint targetLen = _worldPacket.ReadBits<uint>(9);
            Params.Read(_worldPacket);
            ChannelGUID = _worldPacket.ReadPackedGuid();
            Target = _worldPacket.ReadString(targetLen);
        }
    }

    public class ChatMessageDND : ClientPacket
    {
        public string Text;

        public ChatMessageDND(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            uint len = _worldPacket.ReadBits<uint>(11);
            Text = _worldPacket.ReadString(len);
        }
    }

    public class ChatMessageAFK : ClientPacket
    {
        public string Text;

        public ChatMessageAFK(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            uint len = _worldPacket.ReadBits<uint>(11);
            Text = _worldPacket.ReadString(len);
        }
    }

    public class ChatMessageEmote : ClientPacket
    {
        public string Text;

        public ChatMessageEmote(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            uint len = _worldPacket.ReadBits<uint>(11);
            Text = _worldPacket.ReadString(len);
        }
    }

    public class ChatPkt : ServerPacket
    {
        public ChatFlags _ChatFlags;
        public Language _Language = Language.Universal;
        public uint AchievementID;
        public string Channel = "";
        public ObjectGuid? ChannelGUID;
        public string ChatText = "";
        public float DisplayTime;
        public bool FakeSenderName;
        public bool HideChatLog;
        public ObjectGuid PartyGUID;
        public string Prefix = "";
        public ObjectGuid SenderAccountGUID;
        public ObjectGuid SenderGUID;
        public ObjectGuid SenderGuildGUID;
        public string SenderName = "";
        public uint SenderVirtualAddress;

        public ChatMsg SlashCmd;
        public ObjectGuid TargetGUID;
        public string TargetName = "";
        public uint TargetVirtualAddress;
        public uint? Unused_801;

        public ChatPkt() : base(ServerOpcodes.Chat)
        {
        }

        public void Initialize(ChatMsg chatType, Language language, WorldObject sender, WorldObject receiver, string message, uint achievementId = 0, string channelName = "", Locale locale = Locale.enUS, string addonPrefix = "")
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

        public void SetReceiver(WorldObject receiver, Locale locale)
        {
            TargetGUID = receiver.GetGUID();

            Creature creatureReceiver = receiver.ToCreature();

            if (creatureReceiver)
                TargetName = creatureReceiver.GetName(locale);
        }

        public override void Write()
        {
            _worldPacket.WriteUInt8((byte)SlashCmd);
            _worldPacket.WriteUInt32((uint)_Language);
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
            _worldPacket.WriteBits((byte)_ChatFlags, 14);
            _worldPacket.WriteBit(HideChatLog);
            _worldPacket.WriteBit(FakeSenderName);
            _worldPacket.WriteBit(Unused_801.HasValue);
            _worldPacket.WriteBit(ChannelGUID.HasValue);
            _worldPacket.FlushBits();

            _worldPacket.WriteString(SenderName);
            _worldPacket.WriteString(TargetName);
            _worldPacket.WriteString(Prefix);
            _worldPacket.WriteString(Channel);
            _worldPacket.WriteString(ChatText);

            if (Unused_801.HasValue)
                _worldPacket.WriteUInt32(Unused_801.Value);

            if (ChannelGUID.HasValue)
                _worldPacket.WritePackedGuid(ChannelGUID.Value);
        }

        private void SetSender(WorldObject sender, Locale locale)
        {
            SenderGUID = sender.GetGUID();

            Creature creatureSender = sender.ToCreature();

            if (creatureSender)
                SenderName = creatureSender.GetName(locale);

            Player playerSender = sender.ToPlayer();

            if (playerSender)
            {
                SenderAccountGUID = playerSender.Session.GetAccountGUID();
                _ChatFlags = playerSender.GetChatFlags();

                SenderGuildGUID = ObjectGuid.Create(HighGuid.Guild, playerSender.GetGuildId());

                Group group = playerSender.GetGroup();

                if (group)
                    PartyGUID = group.GetGUID();
            }
        }
    }

    public class EmoteMessage : ServerPacket
    {
        public uint EmoteID;

        public ObjectGuid Guid;
        public int SequenceVariation;
        public List<uint> SpellVisualKitIDs = new();

        public EmoteMessage() : base(ServerOpcodes.Emote, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
            _worldPacket.WriteUInt32(EmoteID);
            _worldPacket.WriteInt32(SpellVisualKitIDs.Count);
            _worldPacket.WriteInt32(SequenceVariation);

            foreach (var id in SpellVisualKitIDs)
                _worldPacket.WriteUInt32(id);
        }
    }

    public class CTextEmote : ClientPacket
    {
        public int EmoteID;
        public int SequenceVariation;
        public int SoundIndex;
        public uint[] SpellVisualKitIDs;

        public ObjectGuid Target;

        public CTextEmote(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Target = _worldPacket.ReadPackedGuid();
            EmoteID = _worldPacket.ReadInt32();
            SoundIndex = _worldPacket.ReadInt32();

            SpellVisualKitIDs = new uint[_worldPacket.ReadUInt32()];
            SequenceVariation = _worldPacket.ReadInt32();

            for (var i = 0; i < SpellVisualKitIDs.Length; ++i)
                SpellVisualKitIDs[i] = _worldPacket.ReadUInt32();
        }
    }

    public class STextEmote : ServerPacket
    {
        public int EmoteID;
        public int SoundIndex = -1;
        public ObjectGuid SourceAccountGUID;

        public ObjectGuid SourceGUID;
        public ObjectGuid TargetGUID;

        public STextEmote() : base(ServerOpcodes.TextEmote, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(SourceGUID);
            _worldPacket.WritePackedGuid(SourceAccountGUID);
            _worldPacket.WriteInt32(EmoteID);
            _worldPacket.WriteInt32(SoundIndex);
            _worldPacket.WritePackedGuid(TargetGUID);
        }
    }

    public class PrintNotification : ServerPacket
    {
        public string NotifyText;

        public PrintNotification(string notifyText) : base(ServerOpcodes.PrintNotification)
        {
            NotifyText = notifyText;
        }

        public override void Write()
        {
            _worldPacket.WriteBits(NotifyText.GetByteCount(), 12);
            _worldPacket.WriteString(NotifyText);
        }
    }

    public class EmoteClient : ClientPacket
    {
        public EmoteClient(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
        }
    }

    internal class ChatPlayerNotfound : ServerPacket
    {
        private readonly string Name;

        public ChatPlayerNotfound(string name) : base(ServerOpcodes.ChatPlayerNotfound)
        {
            Name = name;
        }

        public override void Write()
        {
            _worldPacket.WriteBits(Name.GetByteCount(), 9);
            _worldPacket.WriteString(Name);
        }
    }

    internal class ChatPlayerAmbiguous : ServerPacket
    {
        private readonly string Name;

        public ChatPlayerAmbiguous(string name) : base(ServerOpcodes.ChatPlayerAmbiguous)
        {
            Name = name;
        }

        public override void Write()
        {
            _worldPacket.WriteBits(Name.GetByteCount(), 9);
            _worldPacket.WriteString(Name);
        }
    }

    internal class ChatRestricted : ServerPacket
    {
        private readonly ChatRestrictionType Reason;

        public ChatRestricted(ChatRestrictionType reason) : base(ServerOpcodes.ChatRestricted)
        {
            Reason = reason;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt8((byte)Reason);
        }
    }

    internal class ChatServerMessage : ServerPacket
    {
        public int MessageID;
        public string StringParam = "";

        public ChatServerMessage() : base(ServerOpcodes.ChatServerMessage)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteInt32(MessageID);

            _worldPacket.WriteBits(StringParam.GetByteCount(), 11);
            _worldPacket.WriteString(StringParam);
        }
    }

    internal class ChatRegisterAddonPrefixes : ClientPacket
    {
        public string[] Prefixes = new string[64];

        public ChatRegisterAddonPrefixes(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            int count = _worldPacket.ReadInt32();

            for (int i = 0; i < count && i < 64; ++i)
                Prefixes[i] = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(5));
        }
    }

    internal class ChatUnregisterAllAddonPrefixes : ClientPacket
    {
        public ChatUnregisterAllAddonPrefixes(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
        }
    }

    internal class DefenseMessage : ServerPacket
    {
        public string MessageText = "";

        public uint ZoneID;

        public DefenseMessage() : base(ServerOpcodes.DefenseMessage)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(ZoneID);
            _worldPacket.WriteBits(MessageText.GetByteCount(), 12);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(MessageText);
        }
    }

    internal class ChatReportIgnored : ClientPacket
    {
        public ObjectGuid IgnoredGUID;
        public byte Reason;

        public ChatReportIgnored(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            IgnoredGUID = _worldPacket.ReadPackedGuid();
            Reason = _worldPacket.ReadUInt8();
        }
    }

    public class ChatAddonMessageParams
    {
        public bool IsLogged;

        public string Prefix;
        public string Text;
        public ChatMsg Type = ChatMsg.Party;

        public void Read(WorldPacket data)
        {
            uint prefixLen = data.ReadBits<uint>(5);
            uint textLen = data.ReadBits<uint>(8);
            IsLogged = data.HasBit();
            Type = (ChatMsg)data.ReadInt32();
            Prefix = data.ReadString(prefixLen);
            Text = data.ReadString(textLen);
        }
    }
}