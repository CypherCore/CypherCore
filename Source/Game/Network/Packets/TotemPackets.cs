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
