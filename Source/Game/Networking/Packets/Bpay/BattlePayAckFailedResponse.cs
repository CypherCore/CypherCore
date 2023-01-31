namespace Game.Networking.Packets.Bpay
{
    public class BattlePayAckFailedResponse : ClientPacket
    {

        public BattlePayAckFailedResponse(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            ServerToken = _worldPacket.ReadUInt32();
        }

        public uint ServerToken { get; set; } = 0;
    }

}
