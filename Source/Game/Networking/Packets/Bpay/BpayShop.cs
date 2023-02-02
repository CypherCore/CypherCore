using System.Collections.Generic;

namespace Game.Networking.Packets.Bpay
{
    public class BpayShop
    {
        public uint Entry { get; set; }
        public uint EntryId { get; set; }
        public uint GroupID { get; set; }
        public uint ProductID { get; set; }
        public uint Ordering { get; set; }
        public uint VasServiceType { get; set; }
        public byte StoreDeliveryType { get; set; }
        public BpayDisplayInfo Display { get; set; }

        public void Write(WorldPacket _worldPacket)
        {
            _worldPacket.Write(EntryId);
            _worldPacket.Write(GroupID);
            _worldPacket.Write(ProductID);
            _worldPacket.Write(Ordering);
            _worldPacket.Write(VasServiceType);
            _worldPacket.Write(StoreDeliveryType);
            _worldPacket.FlushBits();
            _worldPacket.WriteBit(Display.has_value());

            if (Display.has_value())
            {
                _worldPacket.FlushBits();
                Display.Write(_worldPacket);
            }
        }
    }
}
