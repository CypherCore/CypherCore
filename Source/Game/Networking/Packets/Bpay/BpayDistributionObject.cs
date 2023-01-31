using Game.Entities;

namespace Game.Networking.Packets.Bpay
{
    public class BpayDistributionObject
    {
        public BpayProduct Product { get; set; }
        public ObjectGuid TargetPlayer { get; set; } = new ObjectGuid();
        public ulong DistributionID { get; set; } = 0;
        public ulong PurchaseID { get; set; } = 0;
        public uint Status { get; set; } = 0;
        public uint ProductID { get; set; } = 0;
        public uint TargetVirtualRealm { get; set; } = 0;
        public uint TargetNativeRealm { get; set; } = 0;
        public bool Revoked { get; set; } = false;

        public void Write(WorldPacket _worldPacket)
        {
            _worldPacket.Write(DistributionID);

            _worldPacket.Write(Status);
            _worldPacket.Write(ProductID);

            _worldPacket.Write(TargetPlayer);
            _worldPacket.Write(TargetVirtualRealm);
            _worldPacket.Write(TargetNativeRealm);

            _worldPacket.Write(PurchaseID);
            _worldPacket.WriteBit(Product.has_value());
            _worldPacket.WriteBit(Revoked);
            _worldPacket.FlushBits();

            if (Product.has_value())
                Product.Write(_worldPacket);

        }
    }


}
