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

        public ulong QueueID { get; set; }
        public int AreaID { get; set; }
        public long ExpireTime { get; set; }
    }

    class BFMgrEntryInviteResponse : ClientPacket
    {
        public BFMgrEntryInviteResponse(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            QueueID = _worldPacket.ReadUInt64();
            AcceptedInvite = _worldPacket.HasBit();
        }

        public ulong QueueID { get; set; }
        public bool AcceptedInvite { get; set; }
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

        public ulong QueueID { get; set; }
        public BattlefieldState BattleState { get; set; }
        public uint Timeout { get; set; } = uint.MaxValue;    // unused in client
        public int MinLevel { get; set; }                     // unused in client
        public int MaxLevel { get; set; }                     // unused in client
        public int MapID { get; set; }                        // unused in client
        public uint InstanceID { get; set; }                  // unused in client
        public sbyte Index { get; set; }                      // unused in client
    }

    class BFMgrQueueInviteResponse : ClientPacket
    {
        public BFMgrQueueInviteResponse(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            QueueID = _worldPacket.ReadUInt64();
            AcceptedInvite = _worldPacket.HasBit();
        }

        public ulong QueueID { get; set; }
        public bool AcceptedInvite { get; set; }
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

        public ulong QueueID { get; set; }
        public int AreaID { get; set; }
        public sbyte Result { get; set; }
        public ObjectGuid FailedPlayerGUID { get; set; }
        public BattlefieldState BattleState { get; set; }
        public bool LoggingIn { get; set; }
    }

    class BFMgrQueueExitRequest : ClientPacket
    {
        public BFMgrQueueExitRequest(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            QueueID = _worldPacket.ReadUInt64();
        }

        public ulong QueueID { get; set; }
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

        public bool ClearedAFK { get; set; }
        public bool Relocated { get; set; }
        public bool OnOffense { get; set; }
        public ulong QueueID { get; set; }
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

        public ulong QueueID { get; set; }
        public BFLeaveReason Reason { get; set; }
        public BattlefieldState BattleState { get; set; }
        public bool Relocated { get; set; }
    }

    public class AreaSpiritHealerTime : ServerPacket
    {
        public AreaSpiritHealerTime() : base(ServerOpcodes.AreaSpiritHealerTime) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(HealerGuid);
            _worldPacket.WriteUInt32(TimeLeft);
        }

        public ObjectGuid HealerGuid { get; set; }
        public uint TimeLeft { get; set; }
    }
}
