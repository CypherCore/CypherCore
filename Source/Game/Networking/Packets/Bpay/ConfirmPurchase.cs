using Framework.Constants;

namespace Game.Networking.Packets.Bpay
{
    public class ConfirmPurchase : ServerPacket
    {
        public ConfirmPurchase() : base(ServerOpcodes.BattlePayConfirmPurchase)
        {
        }

        public override void Write()
        {
            _worldPacket.Write(PurchaseID);
            _worldPacket.Write(ServerToken);
        }

        public ulong PurchaseID { get; set; } = 0;
        public uint ServerToken { get; set; } = 0;
    }
}
