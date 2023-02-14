// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Game.Networking.Packets.Bpay
{
    public class BpayProduct
    {
        public uint Entry { get; set; }
        public uint ProductId { get; set; }
        public byte Type { get; set; }
        public uint Flags { get; set; } // ItemEntry
        public uint Unk1 { get; set; } // ItemCount
        public uint DisplayId { get; set; } // MountSpellID
        public uint ItemId { get; set; } // BattlePetCreatureID
        public uint Unk4 { get; set; }
        public uint Unk5 { get; set; }
        public uint Unk6 { get; set; }
        public uint Unk7 { get; set; } // TransmogSetID
        public uint Unk8 { get; set; }
        public uint Unk9 { get; set; }
        public string UnkString { get; set; } = "";
        public bool UnkBit { get; set; }
        public uint UnkBits { get; set; }
        public List<BpayProductItem> Items { get; set; } = new List<BpayProductItem>();
        public BpayDisplayInfo Display { get; set; }
        public string Name { get; set; }

        public void Write(WorldPacket _worldPacket)
        {
            _worldPacket.Write(ProductId);
            _worldPacket.Write(Type);
            _worldPacket.Write(Flags);
            _worldPacket.Write(Unk1);
            _worldPacket.Write(DisplayId);
            _worldPacket.Write(ItemId);
            _worldPacket.Write(Unk4);
            _worldPacket.Write(Unk5);
            _worldPacket.Write(Unk6);
            _worldPacket.Write(Unk7);
            _worldPacket.Write(Unk8);
            _worldPacket.Write(Unk9);
            _worldPacket.WriteBits(UnkString.Length, 8);
            _worldPacket.WriteBit(UnkBit);
            _worldPacket.WriteBit(UnkBits.has_value());
            _worldPacket.WriteBits(Items.Count, 7);
            _worldPacket.WriteBit(Display.has_value());

            if (UnkBits.has_value())
                _worldPacket.WriteBits(UnkBits, 4);

            _worldPacket.FlushBits();

            foreach (var item in Items)
                item.Write(_worldPacket);

            _worldPacket.WriteString(UnkString);

            if (Display.has_value())
                Display.Write(_worldPacket);

        }
    }


}
