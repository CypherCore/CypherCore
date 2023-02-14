// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

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
            _worldPacket.WriteUInt32(Duration);
            _worldPacket.WriteUInt32(SpellID);
            _worldPacket.WriteFloat(TimeMod);
            _worldPacket.WriteBit(CannotDismiss);
            _worldPacket.FlushBits();
        }

        public ObjectGuid Totem;
        public uint SpellID;
        public uint Duration;
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
