using System.Collections.Generic;
using Game.Entities;

namespace Game.Networking.Packets.Bpay
{
    public class VasPurchase
    {
        public List<uint> ItemIDs { get; set; } = new List<uint>();
        public ObjectGuid PlayerGuid { get; set; } = new ObjectGuid();
        public ulong UnkLong { get; set; } = 0;
        public uint UnkInt { get; set; } = 0;
        public uint UnkInt2 { get; set; } = 0;

        public void Write(WorldPacket _worldPacket)
        {
            _worldPacket.Write(PlayerGuid);
            _worldPacket.Write(UnkInt);
            _worldPacket.Write(UnkInt2);
            _worldPacket.Write(UnkLong);
            _worldPacket.WriteBits(ItemIDs.Count, 2);
            _worldPacket.FlushBits();

            foreach (var itemId in ItemIDs)
                _worldPacket.Write(itemId);
        }
    }
}
