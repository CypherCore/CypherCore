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
        public ByteBuffer Data { get; set; } = new ByteBuffer();
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

        public BattlenetRpcErrorCode BnetStatus { get; set; } = BattlenetRpcErrorCode.Ok;
        public MethodCall Method;
        public ByteBuffer Data { get; set; } = new ByteBuffer();
    }

    class SetSessionState : ServerPacket
    {
        public SetSessionState() : base(ServerOpcodes.BattlenetSetSessionState) { }

        public override void Write()
        {
            _worldPacket.WriteBits(State, 2);
            _worldPacket.FlushBits();
        }

        public byte State { get; set; }
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

        public uint Token { get; set; }
        public bool Allow { get; set; } = true;
        public ByteBuffer Ticket { get; set; }
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

        public MethodCall Method { get; set; }
        public byte[] Data { get; set; }
    }

    class RequestRealmListTicket : ClientPacket
    {
        public RequestRealmListTicket(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Token = _worldPacket.ReadUInt32();
            Secret.AddRange(_worldPacket.ReadBytes((uint)Secret.Capacity));
        }

        public uint Token { get; set; }
        public Array<byte> Secret { get; set; } = new Array<byte>(32);
    }

    struct MethodCall
    {
        public uint GetServiceHash() => (uint)(Type >> 32);
        public uint GetMethodId() => (uint)(Type & 0xFFFFFFFF);

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

        public ulong Type { get; set; }
        public ulong ObjectId { get; set; }
        public uint Token { get; set; }
    }
}
