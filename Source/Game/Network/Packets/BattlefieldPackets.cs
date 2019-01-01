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

namespace Game.Network.Packets
{
    class BFMgrEntryInvite : ServerPacket
    {
        public BFMgrEntryInvite() : base(ServerOpcodes.None) { }

        public override void Write()
        {
            _worldPacket.WriteUInt64(QueueID);
            _worldPacket.WriteUInt32(AreaID);
            _worldPacket.WriteUInt32(ExpireTime);
        }

        public ulong QueueID;
        public int AreaID;
        public long ExpireTime;
    }

    class BFMgrEntryInviteResponse : ClientPacket
    {
        public BFMgrEntryInviteResponse(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            QueueID = _worldPacket.ReadUInt64();
            AcceptedInvite = _worldPacket.HasBit();
        }

        public ulong QueueID;
        public bool AcceptedInvite;
    }

    class BFMgrQueueInvite : ServerPacket
    {
        public BFMgrQueueInvite() : base(ServerOpcodes.None) { }

        public override void Write()
        {
            _worldPacket.WriteUInt64(QueueID);
            _worldPacket.WriteInt8(BattleState);
            _worldPacket.WriteUInt32(Timeout);
            _worldPacket.WriteInt32(MinLevel);
            _worldPacket.WriteInt32(MaxLevel);
            _worldPacket.WriteInt32(MapID);
            _worldPacket.WriteUInt32(InstanceID);
            _worldPacket.WriteBit(Index);
            _worldPacket.FlushBits();
        }

        public ulong QueueID;
        public BattlefieldState BattleState;
        public uint Timeout = uint.MaxValue;    // unused in client
        public int MinLevel;                                     // unused in client
        public int MaxLevel;                                     // unused in client
        public int MapID;                                        // unused in client
        public uint InstanceID;                                  // unused in client
        public sbyte Index;                                         // unused in client
    }

    class BFMgrQueueInviteResponse : ClientPacket
    {
        public BFMgrQueueInviteResponse(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            QueueID = _worldPacket.ReadUInt64();
            AcceptedInvite = _worldPacket.HasBit();
        }

        public ulong QueueID;
        public bool AcceptedInvite;
    }

    class BFMgrQueueRequestResponse : ServerPacket
    {
        public BFMgrQueueRequestResponse() : base(ServerOpcodes.None) { }

        public override void Write()
        {
            _worldPacket.WriteUInt64(QueueID);
            _worldPacket.WriteInt32(AreaID);
            _worldPacket.WriteInt8(Result);
            _worldPacket.WritePackedGuid(FailedPlayerGUID);
            _worldPacket.WriteInt8(BattleState);
            _worldPacket.WriteBit(LoggingIn);
            _worldPacket.FlushBits();
        }

        public ulong QueueID;
        public int AreaID;
        public sbyte Result;
        public ObjectGuid FailedPlayerGUID;
        public BattlefieldState BattleState;
        public bool LoggingIn;
    }

    class BFMgrQueueExitRequest : ClientPacket
    {
        public BFMgrQueueExitRequest(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            QueueID = _worldPacket.ReadUInt64();
        }

        public ulong QueueID;
    }

    class BFMgrEntering : ServerPacket
    {
        public BFMgrEntering() : base(ServerOpcodes.None, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteBit(ClearedAFK);
            _worldPacket.WriteBit(Relocated);
            _worldPacket.WriteBit(OnOffense);
            _worldPacket.WriteUInt64(QueueID);
        }

        public bool ClearedAFK;
        public bool Relocated;
        public bool OnOffense;
        public ulong QueueID;
    }

    class BFMgrEjected : ServerPacket
    {
        public BFMgrEjected() : base(ServerOpcodes.None, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt64(QueueID);
            _worldPacket.WriteInt8(Reason);
            _worldPacket.WriteInt8(BattleState);
            _worldPacket.WriteBit(Relocated);
            _worldPacket.FlushBits();
        }

        public ulong QueueID;
        public BFLeaveReason Reason;
        public BattlefieldState BattleState;
        public bool Relocated;
    }

    public class AreaSpiritHealerTime : ServerPacket
    {
        public AreaSpiritHealerTime() : base(ServerOpcodes.AreaSpiritHealerTime) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(HealerGuid);
            _worldPacket.WriteUInt32(TimeLeft);
        }

        public ObjectGuid HealerGuid;
        public uint TimeLeft;
    }
}
