using Game.Entities;

namespace Game.Networking.Packets.Bpay
{
    public class StartPurchase : ClientPacket
    {

        public StartPurchase(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            ClientToken = _worldPacket.ReadUInt32();
            ProductID = _worldPacket.ReadUInt32();
            TargetCharacter = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid TargetCharacter { get; set; } = new ObjectGuid();
        public uint ClientToken { get; set; } = 0;
        public uint ProductID { get; set; } = 0;
    }
}
