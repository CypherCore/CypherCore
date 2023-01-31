namespace Game.Networking.Packets.Bpay
{
    public sealed class BattlePayRequestPriceInfo : ClientPacket
    {
        public BattlePayRequestPriceInfo(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
        }

        public byte UnkByte { get; set; } = 0;
    }
}
