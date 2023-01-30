// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.IO;

namespace Game.Networking.Packets
{
    internal class Notification : ServerPacket
    {
        public ByteBuffer Data = new();

        public MethodCall Method;

        public Notification() : base(ServerOpcodes.BattlenetNotification)
        {
        }

        public override void Write()
        {
            Method.Write(_worldPacket);
            _worldPacket.WriteUInt32(Data.GetSize());
            _worldPacket.WriteBytes(Data);
        }
    }

    internal class Response : ServerPacket
    {
        public BattlenetRpcErrorCode BnetStatus = BattlenetRpcErrorCode.Ok;
        public ByteBuffer Data = new();
        public MethodCall Method;

        public Response() : base(ServerOpcodes.BattlenetResponse)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32((uint)BnetStatus);
            Method.Write(_worldPacket);
            _worldPacket.WriteUInt32(Data.GetSize());
            _worldPacket.WriteBytes(Data);
        }
    }

    internal class ConnectionStatus : ServerPacket
    {
        public byte State;
        public bool SuppressNotification;

        public ConnectionStatus() : base(ServerOpcodes.BattleNetConnectionStatus)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteBits(State, 2);
            _worldPacket.WriteBit(SuppressNotification);
            _worldPacket.FlushBits();
        }
    }

    internal class ChangeRealmTicketResponse : ServerPacket
    {
        public bool Allow = true;
        public ByteBuffer Ticket;

        public uint Token;

        public ChangeRealmTicketResponse() : base(ServerOpcodes.ChangeRealmTicketResponse)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Token);
            _worldPacket.WriteBit(Allow);
            _worldPacket.WriteUInt32(Ticket.GetSize());
            _worldPacket.WriteBytes(Ticket);
        }
    }

    internal class BattlenetRequest : ClientPacket
    {
        public byte[] Data;

        public MethodCall Method;

        public BattlenetRequest(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Method.Read(_worldPacket);
            uint protoSize = _worldPacket.ReadUInt32();

            Data = _worldPacket.ReadBytes(protoSize);
        }
    }

    internal class ChangeRealmTicket : ClientPacket
    {
        public Array<byte> Secret = new(32);

        public uint Token;

        public ChangeRealmTicket(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Token = _worldPacket.ReadUInt32();

            for (var i = 0; i < Secret.GetLimit(); ++i)
                Secret[i] = _worldPacket.ReadUInt8();
        }
    }

    public struct MethodCall
    {
        public uint GetServiceHash()
        {
            return (uint)(Type >> 32);
        }

        public uint GetMethodId()
        {
            return (uint)(Type & 0xFFFFFFFF);
        }

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