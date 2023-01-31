using Framework.Constants;

namespace Game.Networking.Packets.Bpay
{
    public class DistributionUpdate : ServerPacket
    {
        public DistributionUpdate() : base(ServerOpcodes.BattlePayDistributionUpdate)
        {
        }

        public override void Write()
        {
            DistributionObject.Write(_worldPacket);
        }

        public BpayDistributionObject DistributionObject { get; set; } = new BpayDistributionObject();
    }
}
