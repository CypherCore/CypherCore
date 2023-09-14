// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using System;

namespace Game.Networking.Packets
{
    class TotemDestroyed : ClientPacket
    {
        public TotemDestroyed(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Slot = _worldPacket.ReadUInt8();
            TotemGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid TotemGUID;
        public byte Slot;
    }

    class TotemCreated : ServerPacket
    {
        public TotemCreated() : base(ServerOpcodes.TotemCreated) { }

        public override void Write()
        {
            _worldPacket.WriteUInt8(Slot);
            _worldPacket.WritePackedGuid(Totem);
            _worldPacket.WriteUInt32((uint)Duration.TotalMilliseconds);
            _worldPacket.WriteUInt32(SpellID);
            _worldPacket.WriteFloat(TimeMod);
            _worldPacket.WriteBit(CannotDismiss);
            _worldPacket.FlushBits();
        }

        public ObjectGuid Totem;
        public uint SpellID;
        public TimeSpan Duration;
        public byte Slot;
        public float TimeMod = 1.0f;
        public bool CannotDismiss;
    }

    class TotemMoved : ServerPacket
    {
        public TotemMoved() : base(ServerOpcodes.TotemMoved) { }

        public override void Write()
        {
            _worldPacket.WriteUInt8(Slot);
            _worldPacket.WriteUInt8(NewSlot);
            _worldPacket.WritePackedGuid(Totem);
        }

        public ObjectGuid Totem;
        public byte Slot;
        public byte NewSlot;
    }
}
