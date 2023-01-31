using Framework.Constants;

namespace Game.Networking.Packets.Bpay
{
    public class DisplayPromotion : ServerPacket
    {
        public DisplayPromotion(uint ID) : base(ServerOpcodes.DisplayPromotion)
        {
            PromotionID = ID;
        }


        /*void WorldPackets::BattlePay::PurchaseDetailsResponse::Read()
        {
            _worldPacket >> UnkByte;
        }*/

        /*/
        void WorldPackets::BattlePay::PurchaseUnkResponse::Read()
        {
            auto keyLen = _worldPacket.ReadBits(6);
            auto key2Len = _worldPacket.ReadBits(7);
            Key = _worldPacket.ReadString(keyLen);
            Key2 = _worldPacket.ReadString(key2Len);
        }*/

        public override void Write()
        {
            _worldPacket.Write(PromotionID);
        }

        public uint PromotionID { get; set; } = 0;
    }
}
