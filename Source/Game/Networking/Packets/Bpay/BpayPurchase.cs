namespace Game.Networking.Packets.Bpay
{
    public class BpayPurchase
    {
        public ulong PurchaseID { get; set; } = 0;
        public ulong UnkLong { get; set; } = 0;
        public ulong UnkLong2 { get; set; } = 0;
        public uint Status { get; set; } = 0;
        public uint ResultCode { get; set; } = 0;
        public uint ProductID { get; set; } = 0;
        public uint UnkInt { get; set; } = 0;
        public string WalletName { get; set; } = "";

        public void Write(WorldPacket _worldPacket)
        {
            _worldPacket.Write(PurchaseID);
            _worldPacket.Write(UnkLong);
            _worldPacket.Write(UnkLong2);
            _worldPacket.Write(Status);
            _worldPacket.Write(ResultCode);
            _worldPacket.Write(ProductID);
            _worldPacket.Write(UnkInt);
            _worldPacket.WriteBits(WalletName.Length, 8);
            _worldPacket.WriteString(WalletName);
        }
    }

}
