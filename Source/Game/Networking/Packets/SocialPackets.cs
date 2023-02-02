// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using System;
using System.Collections.Generic;

namespace Game.Networking.Packets
{
    public class SendContactList : ClientPacket
    {
        public SendContactList(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Flags = (SocialFlag)_worldPacket.ReadUInt32();
        }

        public SocialFlag Flags;
    }

    public class ContactList : ServerPacket
    {
        public ContactList() : base(ServerOpcodes.ContactList)
        {
            Contacts = new List<ContactInfo>();
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32((uint)Flags);
            _worldPacket.WriteBits(Contacts.Count, 8);
            _worldPacket.FlushBits();

            foreach (ContactInfo contact in Contacts)
                contact.Write(_worldPacket);
        }

        public List<ContactInfo> Contacts;
        public SocialFlag Flags;
    }

    public class FriendStatusPkt : ServerPacket
    {
        public FriendStatusPkt() : base(ServerOpcodes.FriendStatus) { }

        public void Initialize(ObjectGuid guid, FriendsResult result, FriendInfo friendInfo)
        {
            VirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
            Notes = friendInfo.Note;
            ClassID = friendInfo.Class;
            Status = friendInfo.Status;
            Guid = guid;
            WowAccountGuid = friendInfo.WowAccountGuid;
            Level = friendInfo.Level;
            AreaID = friendInfo.Area;
            FriendResult = result;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt8((byte)FriendResult);
            _worldPacket.WritePackedGuid(Guid);
            _worldPacket.WritePackedGuid(WowAccountGuid);
            _worldPacket.WriteUInt32(VirtualRealmAddress);
            _worldPacket.WriteUInt8((byte)Status);
            _worldPacket.WriteUInt32(AreaID);
            _worldPacket.WriteUInt32(Level);
            _worldPacket.WriteUInt32((uint)ClassID);
            _worldPacket.WriteBits(Notes.GetByteCount(), 10);
            _worldPacket.WriteBit(Mobile);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(Notes);
        }

        public uint VirtualRealmAddress;
        public string Notes;
        public Class ClassID = Class.None;
        public FriendStatus Status;
        public ObjectGuid Guid;
        public ObjectGuid WowAccountGuid;
        public uint Level;
        public uint AreaID;
        public FriendsResult FriendResult;
        public bool Mobile;
    }

    public class AddFriend : ClientPacket
    {
        public AddFriend(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint nameLength = _worldPacket.ReadBits<uint>(9);
            uint noteslength = _worldPacket.ReadBits<uint>(10);
            Name = _worldPacket.ReadString(nameLength);
            Notes = _worldPacket.ReadString(noteslength);
        }

        public string Notes;
        public string Name;
    }

    public class DelFriend : ClientPacket
    {
        public DelFriend(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Player.Read(_worldPacket);
        }

        public QualifiedGUID Player;
    }

    public class SetContactNotes : ClientPacket
    {
        public SetContactNotes(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Player.Read(_worldPacket);
            Notes = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(10));
        }

        public QualifiedGUID Player;
        public string Notes;
    }

    public class AddIgnore : ClientPacket
    {
        public AddIgnore(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint nameLength = _worldPacket.ReadBits<uint>(9);
            AccountGUID = _worldPacket.ReadPackedGuid();
            Name = _worldPacket.ReadString(nameLength);
        }

        public string Name;
        public ObjectGuid AccountGUID;
    }

    public class DelIgnore : ClientPacket
    {
        public DelIgnore(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Player.Read(_worldPacket);
        }

        public QualifiedGUID Player;
    }

    class SocialContractRequest : ClientPacket
    {
        public SocialContractRequest(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class SocialContractRequestResponse : ServerPacket
    {
        public bool ShowSocialContract;

        public SocialContractRequestResponse() : base(ServerOpcodes.SocialContractRequestResponse) { }

        public override void Write()
        {
            _worldPacket.WriteBit(ShowSocialContract);
            _worldPacket.FlushBits();
        }
    }
    
    //Structs
    public class ContactInfo
    {
        public ContactInfo(ObjectGuid guid, FriendInfo friendInfo)
        {
            Guid = guid;
            WowAccountGuid = friendInfo.WowAccountGuid;
            VirtualRealmAddr = Global.WorldMgr.GetVirtualRealmAddress();
            NativeRealmAddr = Global.WorldMgr.GetVirtualRealmAddress();
            TypeFlags = friendInfo.Flags;
            Notes = friendInfo.Note;
            Status = friendInfo.Status;
            AreaID = friendInfo.Area;
            Level = friendInfo.Level;
            ClassID = friendInfo.Class;
        }

        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(Guid);
            data.WritePackedGuid(WowAccountGuid);
            data.WriteUInt32(VirtualRealmAddr);
            data.WriteUInt32(NativeRealmAddr);
            data.WriteUInt32((uint)TypeFlags);
            data.WriteUInt8((byte)Status);
            data.WriteUInt32(AreaID);
            data.WriteUInt32(Level);
            data.WriteUInt32((uint)ClassID);
            data.WriteBits(Notes.GetByteCount(), 10);
            data.WriteBit(Mobile);
            data.FlushBits();
            data.WriteString(Notes);
        }

        ObjectGuid Guid;
        ObjectGuid WowAccountGuid;
        uint VirtualRealmAddr;
        uint NativeRealmAddr;
        SocialFlag TypeFlags;
        string Notes;
        FriendStatus Status;
        uint AreaID;
        uint Level;
        Class ClassID;
        bool Mobile;
    }

    public struct QualifiedGUID
    {
        public void Read(WorldPacket data)
        {
            VirtualRealmAddress = data.ReadUInt32();
            Guid = data.ReadPackedGuid();
        }

        public ObjectGuid Guid;
        public uint VirtualRealmAddress;
    }
}
