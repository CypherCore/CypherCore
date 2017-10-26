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
using System.Collections.Generic;

namespace Game.Network.Packets
{
    public class ChannelListResponse : ServerPacket
    {
        public ChannelListResponse() : base(ServerOpcodes.ChannelList)
        {
            Members = new List<ChannelPlayer>();
        }

        public override void Write()
        {
            _worldPacket.WriteBit(Display);
            _worldPacket.WriteBits(Channel.Length, 7);
            _worldPacket.WriteUInt32(ChannelFlags);
            _worldPacket.WriteUInt32(Members.Count);
            _worldPacket.WriteString(Channel);

            foreach (ChannelPlayer player in Members)
            {
                _worldPacket.WritePackedGuid(player.Guid);
                _worldPacket.WriteUInt32(player.VirtualRealmAddress);
                _worldPacket.WriteUInt8(player.Flags);
            }
        }

        public List<ChannelPlayer> Members { get; set; }
        public string Channel; // Channel Name
        public ChannelFlags ChannelFlags { get; set; }
        public bool Display { get; set; }

        public struct ChannelPlayer
        {
            public ChannelPlayer(ObjectGuid guid, uint realm, ChannelMemberFlags flags)
            {
                Guid = guid;
                VirtualRealmAddress = realm;
                Flags = flags;
            }

            public ObjectGuid Guid; // Player Guid
            public uint VirtualRealmAddress { get; set; }
            public ChannelMemberFlags Flags { get; set; }
        }
    }

    public class ChannelNotify : ServerPacket
    {
        public ChannelNotify() : base(ServerOpcodes.ChannelNotify) { }

        public override void Write()
        {
            _worldPacket.WriteBits(Type, 6);
            _worldPacket.WriteBits(Channel.Length, 7);
            _worldPacket.WriteBits(Sender.Length, 6);

            _worldPacket.WritePackedGuid(SenderGuid);
            _worldPacket.WritePackedGuid(SenderAccountID);
            _worldPacket.WriteUInt32(SenderVirtualRealm);
            _worldPacket.WritePackedGuid(TargetGuid);
            _worldPacket.WriteUInt32(TargetVirtualRealm);
            _worldPacket.WriteInt32(ChatChannelID);

            if (Type == ChatNotify.ModeChangeNotice)
            {
                _worldPacket.WriteUInt8(OldFlags);
                _worldPacket.WriteUInt8(NewFlags);
            }

            _worldPacket.WriteString(Channel);
            _worldPacket.WriteString(Sender);
        }

        public string Sender { get; set; } = "";
        public ObjectGuid SenderGuid { get; set; }
        public ObjectGuid SenderAccountID { get; set; }
        public ChatNotify Type { get; set; }
        public ChannelMemberFlags OldFlags { get; set; }
        public ChannelMemberFlags NewFlags { get; set; }
        public string Channel { get; set; }
        public uint SenderVirtualRealm { get; set; }
        public ObjectGuid TargetGuid { get; set; }
        public uint TargetVirtualRealm { get; set; }
        public int ChatChannelID { get; set; }
    }

    public class ChannelNotifyJoined : ServerPacket
    {
        public ChannelNotifyJoined() : base(ServerOpcodes.ChannelNotifyJoined) { }

        public override void Write()
        {
            _worldPacket.WriteBits(Channel.Length, 7);
            _worldPacket.WriteBits(ChannelWelcomeMsg.Length, 10);
            _worldPacket.WriteUInt32(ChannelFlags);
            _worldPacket.WriteInt32(ChatChannelID);
            _worldPacket.WriteUInt64(InstanceID);
            _worldPacket.WriteString(Channel);
            _worldPacket.WriteString(ChannelWelcomeMsg);
        }

        public string ChannelWelcomeMsg { get; set; } = "";
        public int ChatChannelID { get; set; }
        public int InstanceID { get; set; }
        public ChannelFlags ChannelFlags { get; set; }
        public string Channel { get; set; } = "";
    }

    public class ChannelNotifyLeft : ServerPacket
    {
        public ChannelNotifyLeft() : base(ServerOpcodes.ChannelNotifyLeft) { }

        public override void Write()
        {
            _worldPacket.WriteBits(Channel.Length, 7);
            _worldPacket.WriteBit(Suspended);
            _worldPacket.WriteUInt32(ChatChannelID);
            _worldPacket.WriteString(Channel);
        }

        public string Channel { get; set; }
        public uint ChatChannelID { get; set; }
        public bool Suspended { get; set; }
    }

    class UserlistAdd : ServerPacket
    {
        public UserlistAdd() : base(ServerOpcodes.UserlistAdd) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(AddedUserGUID);
            _worldPacket.WriteUInt8(UserFlags);
            _worldPacket.WriteUInt32(ChannelFlags);
            _worldPacket.WriteUInt32(ChannelID);

            _worldPacket.WriteBits(ChannelName.Length, 7);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(ChannelName);
        }

        public ObjectGuid AddedUserGUID { get; set; }
        public ChannelFlags ChannelFlags { get; set; }
        public ChannelMemberFlags UserFlags { get; set; }
        public uint ChannelID { get; set; }
        public string ChannelName { get; set; }
    }

    class UserlistRemove : ServerPacket
    {
        public UserlistRemove() : base(ServerOpcodes.UserlistRemove) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(RemovedUserGUID);
            _worldPacket.WriteUInt32(ChannelFlags);
            _worldPacket.WriteUInt32(ChannelID);

            _worldPacket.WriteBits(ChannelName.Length, 7);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(ChannelName);
        }

        public ObjectGuid RemovedUserGUID { get; set; }
        public ChannelFlags ChannelFlags { get; set; }
        public uint ChannelID { get; set; }
        public string ChannelName { get; set; }
    }

    class UserlistUpdate : ServerPacket
    {
        public UserlistUpdate() : base(ServerOpcodes.UserlistUpdate) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(UpdatedUserGUID);
            _worldPacket.WriteUInt8(UserFlags);
            _worldPacket.WriteUInt32(ChannelFlags);
            _worldPacket.WriteUInt32(ChannelID);

            _worldPacket.WriteBits(ChannelName.Length, 7);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(ChannelName);
        }

        public ObjectGuid UpdatedUserGUID { get; set; }
        public ChannelFlags ChannelFlags { get; set; }
        public ChannelMemberFlags UserFlags { get; set; }
        public uint ChannelID { get; set; }
        public string ChannelName { get; set; }
    }

    class ChannelCommand : ClientPacket
    {
        public ChannelCommand(WorldPacket packet) : base(packet)
        {
            switch (GetOpcode())
            {
                case ClientOpcodes.ChatChannelAnnouncements:
                case ClientOpcodes.ChatChannelDeclineInvite:
                case ClientOpcodes.ChatChannelDisplayList:
                case ClientOpcodes.ChatChannelList:
                case ClientOpcodes.ChatChannelModerate:
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

        public string ChannelName { get; set; }
    }

    class ChannelPlayerCommand : ClientPacket
    {
        public ChannelPlayerCommand(WorldPacket packet) : base(packet)
        {
            switch (GetOpcode())
            {
                case ClientOpcodes.ChatChannelBan:
                case ClientOpcodes.ChatChannelInvite:
                case ClientOpcodes.ChatChannelKick:
                case ClientOpcodes.ChatChannelModerator:
                case ClientOpcodes.ChatChannelMute:
                case ClientOpcodes.ChatChannelSetOwner:
                case ClientOpcodes.ChatChannelSilenceAll:
                case ClientOpcodes.ChatChannelUnban:
                case ClientOpcodes.ChatChannelUnmoderator:
                case ClientOpcodes.ChatChannelUnmute:
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

        public string ChannelName { get; set; }
        public string Name;
    }

    class ChannelPassword : ClientPacket
    {
        public ChannelPassword(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint channelNameLength = _worldPacket.ReadBits<uint>(7);
            uint passwordLength = _worldPacket.ReadBits<uint>(7);
            ChannelName = _worldPacket.ReadString(channelNameLength);
            Password = _worldPacket.ReadString(passwordLength);
        }

        public string ChannelName { get; set; }
        public string Password { get; set; }
    }

    public class JoinChannel : ClientPacket
    {
        public JoinChannel(WorldPacket packet) : base(packet) { }

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

        public string Password { get; set; }
        public string ChannelName { get; set; }
        public bool CreateVoiceSession { get; set; }
        public int ChatChannelId { get; set; }
        public bool Internal { get; set; }
    }

    public class LeaveChannel : ClientPacket
    {
        public LeaveChannel(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ZoneChannelID = _worldPacket.ReadInt32();
            ChannelName = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(7));
        }

        public int ZoneChannelID { get; set; }
        public string ChannelName { get; set; }
    }
}
