using System.Collections.Generic;

namespace Game.Networking.Packets.Bpay
{
    public class BpayProductItem
    {
        public uint Entry { get; set; }
        public uint ID { get; set; }
        public byte UnkByte { get; set; }
        public uint ItemID { get; set; }
        public uint Quantity { get; set; }
        public uint UnkInt1 { get; set; }
        public uint UnkInt2 { get; set; }
        public bool IsPet { get; set; }
        public uint PetResult { get; set; }
        public BpayDisplayInfo Display { get; set; }

        public void Write(WorldPacket _worldPacket)
        {
            _worldPacket.Write(ID);
            _worldPacket.Write(UnkByte);
            _worldPacket.Write(ItemID);
            _worldPacket.Write(Quantity);
            _worldPacket.Write(UnkInt1);
            _worldPacket.Write(UnkInt2);
            _worldPacket.WriteBit(IsPet);
            _worldPacket.WriteBit(PetResult.has_value());
            _worldPacket.WriteBit(Display.has_value());

            if (PetResult.has_value())
                _worldPacket.WriteBits(PetResult, 4);

            _worldPacket.FlushBits();

            if (Display.has_value())
                Display.Write(_worldPacket);
        }
    }


}
