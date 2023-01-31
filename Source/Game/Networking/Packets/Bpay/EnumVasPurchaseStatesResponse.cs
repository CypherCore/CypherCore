using Framework.Constants;

namespace Game.Networking.Packets.Bpay
{
    public class EnumVasPurchaseStatesResponse : ServerPacket
    {
        public EnumVasPurchaseStatesResponse() : base(ServerOpcodes.EnumVasPurchaseStatesResponse)
        {
        }


        /*WorldPacket const* WorldPackets::BattlePay::BattlePayVasPurchaseStarted::Write()
        {
            _worldPacket << UnkInt;
            _worldPacket << VasPurchase;

            return &_worldPacket;
        }*/

        /*WorldPacket const* WorldPackets::BattlePay::CharacterClassTrialCreate::Write()
        {
            _worldPacket << Result;
            return &_worldPacket;
        }*/

        /*WorldPacket const* WorldPackets::BattlePay::BattlePayCharacterUpgradeQueued::Write()
        {
            _worldPacket << Character;
            _worldPacket << static_cast<uint32>(EquipmentItems.size());
            for (auto const& item : EquipmentItems)
                _worldPacket << item;

            return &_worldPacket;
        }*/

        /*void WorldPackets::BattlePay::BattlePayTrialBoostCharacter::Read()
        {
            _worldPacket >> Character;
            _worldPacket >> SpecializationID;
        }*/

        /*WorldPacket const* WorldPackets::BattlePay::BattlePayVasPurchaseList::Write()
        {
            _worldPacket.WriteBits(VasPurchase.size(), 6);
            _worldPacket.FlushBits();
            for (auto const& itr : VasPurchase)
                _worldPacket << itr;

            return &_worldPacket;
        }*/

        public override void Write()
        {
            _worldPacket.WriteBits(Result, 2);
        }

        public byte Result { get; set; } = 0;
    }
}
