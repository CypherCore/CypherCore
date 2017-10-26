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
    class TotemDestroyed : ClientPacket
    {
        public TotemDestroyed(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Slot = _worldPacket.ReadUInt8();
            TotemGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid TotemGUID { get; set; }
        public byte Slot { get; set; }
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

        public ObjectGuid Totem { get; set; }
        public uint SpellID { get; set; }
        public uint Duration { get; set; }
        public byte Slot { get; set; }
        public float TimeMod { get; set; } = 1.0f;
        public bool CannotDismiss { get; set; }
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

        public ObjectGuid Totem { get; set; }
        public byte Slot { get; set; }
        public byte NewSlot { get; set; }
    }
}
