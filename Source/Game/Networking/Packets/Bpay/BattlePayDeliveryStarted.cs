using Framework.Constants;

namespace Game.Networking.Packets.Bpay
{
    public class BattlePayDeliveryStarted : ServerPacket
    {
        public BattlePayDeliveryStarted() : base(ServerOpcodes.BattlePayDeliveryStarted)
        {
        }

        public override void Write()
        {
            _worldPacket.Write(DistributionID);
        }

        public ulong DistributionID { get; set; } = 0;
    }


}
