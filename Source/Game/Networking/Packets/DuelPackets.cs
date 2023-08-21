// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using System;

namespace Game.Networking.Packets
{
    public class CanDuel : ClientPacket
    {
        public CanDuel(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            TargetGUID = _worldPacket.ReadPackedGuid();
            ToTheDeath = _worldPacket.HasBit();
        }

        public ObjectGuid TargetGUID;
        public bool ToTheDeath;
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
            _worldPacket.WriteBit(ToTheDeath);
            _worldPacket.FlushBits();
        }

        public ObjectGuid ArbiterGUID;
        public ObjectGuid RequestedByGUID;
        public ObjectGuid RequestedByWowAccount;
        public bool ToTheDeath;
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
