namespace Game.Networking.Packets.Bpay
{
    public sealed class GetProductList : ClientPacket
    {
        public GetProductList(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
        }
    }
}
