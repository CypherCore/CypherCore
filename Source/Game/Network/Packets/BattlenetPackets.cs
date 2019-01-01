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
using Framework.IO;
using System.Collections.Generic;

namespace Game.Network.Packets
{
    class Notification : ServerPacket
    {
        public Notification() : base(ServerOpcodes.BattlenetNotification) { }

        public override void Write()
        {
            Method.Write(_worldPacket);
            _worldPacket.WriteUInt32(Data.GetSize());
            _worldPacket.WriteBytes(Data);
        }

        public MethodCall Method;
        public ByteBuffer Data = new ByteBuffer();
    }

    class Response : ServerPacket
    {
        public Response() : base(ServerOpcodes.BattlenetResponse) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(BnetStatus);
            Method.Write(_worldPacket);
            _worldPacket.WriteUInt32(Data.GetSize());
            _worldPacket.WriteBytes(Data);
        }

        public BattlenetRpcErrorCode BnetStatus = BattlenetRpcErrorCode.Ok;
        public MethodCall Method;
        public ByteBuffer Data = new ByteBuffer();
    }

    class SetSessionState : ServerPacket
    {
        public SetSessionState() : base(ServerOpcodes.BattlenetSetSessionState) { }

        public override void Write()
        {
            _worldPacket.WriteBits(State, 2);
            _worldPacket.FlushBits();
        }

        public byte State;
    }

    class RealmListTicket : ServerPacket
    {
        public RealmListTicket() : base(ServerOpcodes.BattlenetRealmListTicket) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Token);
            _worldPacket.WriteBit(Allow);
            _worldPacket.WriteUInt32(Ticket.GetSize());
            _worldPacket.WriteBytes(Ticket);
        }

        public uint Token;
        public bool Allow = true;
        public ByteBuffer Ticket;
    }

    class BattlenetRequest : ClientPacket
    {
        public BattlenetRequest(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Method.Read(_worldPacket);
            uint protoSize = _worldPacket.ReadUInt32();

            Data = _worldPacket.ReadBytes(protoSize);
        }

        public MethodCall Method;
        public byte[] Data;
    }

    class RequestRealmListTicket : ClientPacket
    {
        public RequestRealmListTicket(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Token = _worldPacket.ReadUInt32();
            for (var i = 0; i < Secret.GetLimit(); ++i)
                Secret[i] = _worldPacket.ReadUInt8();
        }

        public uint Token;
        public Array<byte> Secret = new Array<byte>(32);
    }

    struct MethodCall
    {
        public uint GetServiceHash() { return (uint)(Type >> 32); }
        public uint GetMethodId() { return (uint)(Type & 0xFFFFFFFF); }

        public void Read(ByteBuffer data)
        {
            Type = data.ReadUInt64();
            ObjectId = data.ReadUInt64();
            Token = data.ReadUInt32();
        }

        public void Write(ByteBuffer data)
        {
            data.WriteUInt64(Type);
            data.WriteUInt64(ObjectId);
            data.WriteUInt32(Token);
        }

        public ulong Type;
        public ulong ObjectId;
        public uint Token;
    }
}
