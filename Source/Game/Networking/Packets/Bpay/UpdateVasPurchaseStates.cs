namespace Game.Networking.Packets.Bpay
{
    public sealed class UpdateVasPurchaseStates : ClientPacket
    {
        public UpdateVasPurchaseStates(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
        }
    }
}
