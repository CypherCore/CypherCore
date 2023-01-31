namespace Game.Networking.Packets.Bpay
{
    public sealed class GetPurchaseListQuery : ClientPacket
    {
        public GetPurchaseListQuery(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
        }
    }
}
