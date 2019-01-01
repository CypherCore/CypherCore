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
using System;

namespace Game.Network.Packets
{
    public class CanDuel : ClientPacket
    {
        public CanDuel(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            TargetGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid TargetGUID;
    }

    public class CanDuelResult : ServerPacket
    {
        public CanDuelResult() : base(ServerOpcodes.CanDuelResult) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(TargetGUID);
            _worldPacket.WriteBit(Result);
            _worldPacket.FlushBits();
        }

        public ObjectGuid TargetGUID;
        public bool Result;
    }

    public class DuelComplete : ServerPacket
    {
        public DuelComplete() : base(ServerOpcodes.DuelComplete, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteBit(Started);
            _worldPacket.FlushBits();
        }

        public bool Started;
    }

    public class DuelCountdown : ServerPacket
    {
        public DuelCountdown(uint countdown) : base(ServerOpcodes.DuelCountdown)
        {
            Countdown = countdown;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Countdown);
        }

        uint Countdown;
    }

    public class DuelInBounds : ServerPacket
    {
        public DuelInBounds() : base(ServerOpcodes.DuelInBounds, ConnectionType.Instance) { }

        public override void Write() { }
    }

    public class DuelOutOfBounds : ServerPacket
    {
        public DuelOutOfBounds() : base(ServerOpcodes.DuelOutOfBounds, ConnectionType.Instance) { }

        public override void Write() { }
    }

    public class DuelRequested : ServerPacket
    {
        public DuelRequested() : base(ServerOpcodes.DuelRequested, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ArbiterGUID);
            _worldPacket.WritePackedGuid(RequestedByGUID);
            _worldPacket.WritePackedGuid(RequestedByWowAccount);
        }

        public ObjectGuid ArbiterGUID;
        public ObjectGuid RequestedByGUID;
        public ObjectGuid RequestedByWowAccount;
    }

    public class DuelResponse : ClientPacket
    {
        public DuelResponse(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ArbiterGUID = _worldPacket.ReadPackedGuid();
            Accepted = _worldPacket.HasBit();
            Forfeited = _worldPacket.HasBit();
        }

        public ObjectGuid ArbiterGUID;
        public bool Accepted;
        public bool Forfeited;
    }

    public class DuelWinner : ServerPacket
    {
        public DuelWinner() : base(ServerOpcodes.DuelWinner, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteBits(BeatenName.GetByteCount(), 6);
            _worldPacket.WriteBits(WinnerName.GetByteCount(), 6);
            _worldPacket.WriteBit(Fled);
            _worldPacket.WriteUInt32(BeatenVirtualRealmAddress);
            _worldPacket.WriteUInt32(WinnerVirtualRealmAddress);
            _worldPacket.WriteString(BeatenName);
            _worldPacket.WriteString(WinnerName);
        }

        public string BeatenName;
        public string WinnerName;
        public uint BeatenVirtualRealmAddress;
        public uint WinnerVirtualRealmAddress;
        public bool Fled;
    }
}
