// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets
{
    public class ChannelListResponse : ServerPacket
    {
        public string Channel; // Channel Name
        public ChannelFlags ChannelFlags;
        public bool Display;

        public List<ChannelPlayer> Members;

        public ChannelListResponse() : base(ServerOpcodes.ChannelList)
        {
            Members = new List<ChannelPlayer>();
        }

        public override void Write()
        {
            _worldPacket.WriteBit(Display);
            _worldPacket.WriteBits(Channel.GetByteCount(), 7);
            _worldPacket.WriteUInt32((uint)ChannelFlags);
            _worldPacket.WriteInt32(Members.Count);
            _worldPacket.WriteString(Channel);

            foreach (ChannelPlayer player in Members)
            {
                _worldPacket.WritePackedGuid(player.Guid);
                _worldPacket.WriteUInt32(player.VirtualRealmAddress);
                _worldPacket.WriteUInt8((byte)player.Flags);
            }
        }

        public struct ChannelPlayer
        {
            public ChannelPlayer(ObjectGuid guid, uint realm, ChannelMemberFlags flags)
            {
                Guid = guid;
                VirtualRealmAddress = realm;
                Flags = flags;
            }

            public ObjectGuid Guid; // Player Guid
            public uint VirtualRealmAddress;
            public ChannelMemberFlags Flags;
        }
    }

    public class ChannelNotify : ServerPacket
    {
        public string Channel;
        public int ChatChannelID;
        public ChannelMemberFlags NewFlags;
        public ChannelMemberFlags OldFlags;

        public string Sender = "";
        public ObjectGuid SenderAccountID;
        public ObjectGuid SenderGuid;
        public uint SenderVirtualRealm;
        public ObjectGuid TargetGuid;
        public uint TargetVirtualRealm;
        public ChatNotify Type;

        public ChannelNotify() : base(ServerOpcodes.ChannelNotify)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteBits(Type, 6);
            _worldPacket.WriteBits(Channel.GetByteCount(), 7);
            _worldPacket.WriteBits(Sender.GetByteCount(), 6);

            _worldPacket.WritePackedGuid(SenderGuid);
            _worldPacket.WritePackedGuid(SenderAccountID);
            _worldPacket.WriteUInt32(SenderVirtualRealm);
            _worldPacket.WritePackedGuid(TargetGuid);
            _worldPacket.WriteUInt32(TargetVirtualRealm);
            _worldPacket.WriteInt32(ChatChannelID);

            if (Type == ChatNotify.ModeChangeNotice)
            {
                _worldPacket.WriteUInt8((byte)OldFlags);
                _worldPacket.WriteUInt8((byte)NewFlags);
            }

            _worldPacket.WriteString(Channel);
            _worldPacket.WriteString(Sender);
        }
    }

    public class ChannelNotifyJoined : ServerPacket
    {
        public string Channel = "";
        public ChannelFlags ChannelFlags;
        public ObjectGuid ChannelGUID;

        public string ChannelWelcomeMsg = "";
        public int ChatChannelID;
        public ulong InstanceID;

        public ChannelNotifyJoined() : base(ServerOpcodes.ChannelNotifyJoined)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteBits(Channel.GetByteCount(), 7);
            _worldPacket.WriteBits(ChannelWelcomeMsg.GetByteCount(), 11);
            _worldPacket.WriteUInt32((uint)ChannelFlags);
            _worldPacket.WriteInt32(ChatChannelID);
            _worldPacket.WriteUInt64(InstanceID);
            _worldPacket.WritePackedGuid(ChannelGUID);
            _worldPacket.WriteString(Channel);
            _worldPacket.WriteString(ChannelWelcomeMsg);
        }
    }

    public class ChannelNotifyLeft : ServerPacket
    {
        public string Channel;
        public uint ChatChannelID;
        public bool Suspended;

        public ChannelNotifyLeft() : base(ServerOpcodes.ChannelNotifyLeft)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteBits(Channel.GetByteCount(), 7);
            _worldPacket.WriteBit(Suspended);
            _worldPacket.WriteUInt32(ChatChannelID);
            _worldPacket.WriteString(Channel);
        }
    }

    internal class UserlistAdd : ServerPacket
    {
        public ObjectGuid AddedUserGUID;
        public ChannelFlags ChannelFlags;
        public uint ChannelID;
        public string ChannelName;
        public ChannelMemberFlags UserFlags;

        public UserlistAdd() : base(ServerOpcodes.UserlistAdd)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(AddedUserGUID);
            _worldPacket.WriteUInt8((byte)UserFlags);
            _worldPacket.WriteUInt32((uint)ChannelFlags);
            _worldPacket.WriteUInt32(ChannelID);

            _worldPacket.WriteBits(ChannelName.GetByteCount(), 7);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(ChannelName);
        }
    }

    internal class UserlistRemove : ServerPacket
    {
        public ChannelFlags ChannelFlags;
        public uint ChannelID;
        public string ChannelName;

        public ObjectGuid RemovedUserGUID;

        public UserlistRemove() : base(ServerOpcodes.UserlistRemove)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(RemovedUserGUID);
            _worldPacket.WriteUInt32((uint)ChannelFlags);
            _worldPacket.WriteUInt32(ChannelID);

            _worldPacket.WriteBits(ChannelName.GetByteCount(), 7);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(ChannelName);
        }
    }

    internal class UserlistUpdate : ServerPacket
    {
        public ChannelFlags ChannelFlags;
        public uint ChannelID;
        public string ChannelName;

        public ObjectGuid UpdatedUserGUID;
        public ChannelMemberFlags UserFlags;

        public UserlistUpdate() : base(ServerOpcodes.UserlistUpdate)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(UpdatedUserGUID);
            _worldPacket.WriteUInt8((byte)UserFlags);
            _worldPacket.WriteUInt32((uint)ChannelFlags);
            _worldPacket.WriteUInt32(ChannelID);

            _worldPacket.WriteBits(ChannelName.GetByteCount(), 7);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(ChannelName);
        }
    }

    internal class ChannelCommand : ClientPacket
    {
        public string ChannelName;

        public ChannelCommand(WorldPacket packet) : base(packet)
        {
            switch (GetOpcode())
            {
                case ClientOpcodes.ChatChannelAnnouncements:
                case ClientOpcodes.ChatChannelDeclineInvite:
                case ClientOpcodes.ChatChannelDisplayList:
                case ClientOpcodes.ChatChannelList:
                case ClientOpcodes.ChatChannelOwner:
                    break;
                default:
                    //ABORT();
                    break;
            }
        }

        public override void Read()
        {
            ChannelName = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(7));
        }
    }

    internal class ChannelPlayerCommand : ClientPacket
    {
        public string ChannelName;
        public string Name;

        public ChannelPlayerCommand(WorldPacket packet) : base(packet)
        {
            switch (GetOpcode())
            {
                case ClientOpcodes.ChatChannelBan:
                case ClientOpcodes.ChatChannelInvite:
                case ClientOpcodes.ChatChannelKick:
                case ClientOpcodes.ChatChannelModerator:
                case ClientOpcodes.ChatChannelSetOwner:
                case ClientOpcodes.ChatChannelSilenceAll:
                case ClientOpcodes.ChatChannelUnban:
                case ClientOpcodes.ChatChannelUnmoderator:
                case ClientOpcodes.ChatChannelUnsilenceAll:
                    break;
                default:
                    //ABORT();
                    break;
            }
        }

        public override void Read()
        {
            uint channelNameLength = _worldPacket.ReadBits<uint>(7);
            uint nameLength = _worldPacket.ReadBits<uint>(9);
            ChannelName = _worldPacket.ReadString(channelNameLength);
            Name = _worldPacket.ReadString(nameLength);
        }
    }

    internal class ChannelPassword : ClientPacket
    {
        public string ChannelName;
        public string Password;

        public ChannelPassword(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            uint channelNameLength = _worldPacket.ReadBits<uint>(7);
            uint passwordLength = _worldPacket.ReadBits<uint>(7);
            ChannelName = _worldPacket.ReadString(channelNameLength);
            Password = _worldPacket.ReadString(passwordLength);
        }
    }

    public class JoinChannel : ClientPacket
    {
        public string ChannelName;
        public int ChatChannelId;
        public bool CreateVoiceSession;
        public bool Internal;

        public string Password;

        public JoinChannel(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            ChatChannelId = _worldPacket.ReadInt32();
            CreateVoiceSession = _worldPacket.HasBit();
            Internal = _worldPacket.HasBit();
            uint channelLength = _worldPacket.ReadBits<uint>(7);
            uint passwordLength = _worldPacket.ReadBits<uint>(7);
            ChannelName = _worldPacket.ReadString(channelLength);
            Password = _worldPacket.ReadString(passwordLength);
        }
    }

    public class LeaveChannel : ClientPacket
    {
        public string ChannelName;

        public int ZoneChannelID;

        public LeaveChannel(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            ZoneChannelID = _worldPacket.ReadInt32();
            ChannelName = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(7));
        }
    }
}