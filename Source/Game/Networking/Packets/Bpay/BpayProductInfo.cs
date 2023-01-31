using System.Collections.Generic;

namespace Game.Networking.Packets.Bpay
{
    public class BpayProductInfo
    {
        public uint Entry { get; set; }
        public uint ProductId { get; set; }
        public ulong NormalPriceFixedPoint { get; set; }
        public ulong CurrentPriceFixedPoint { get; set; }
        public List<uint> ProductIds { get; set; } = new List<uint>();
        public uint Unk1 { get; set; }
        public uint Unk2 { get; set; }
        public List<uint> UnkInts { get; set; } = new List<uint>();
        public uint Unk3 { get; set; }
        public uint ChoiceType { get; set; }
        public BpayDisplayInfo Display { get; set; }

        public void Write(WorldPacket _worldPacket)
        {
            _worldPacket.Write(ProductId);
            _worldPacket.Write(NormalPriceFixedPoint);
            _worldPacket.Write(CurrentPriceFixedPoint);
            _worldPacket.Write((uint)ProductIds.Count);
            _worldPacket.Write(Unk1);
            _worldPacket.Write(Unk2);
            _worldPacket.Write((uint)UnkInts.Count);
            _worldPacket.Write(Unk3);

            foreach (var id in ProductIds)
                _worldPacket.Write(id);

            foreach (var id in UnkInts)
                _worldPacket.Write(id);

            _worldPacket.WriteBits(ChoiceType, 7);

            bool wrote = _worldPacket.WriteBit(Display.has_value());
            _worldPacket.FlushBits();

            if (wrote)
                Display.Write(_worldPacket);
        }
    }

}
