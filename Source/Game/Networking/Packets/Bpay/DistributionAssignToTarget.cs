using Game.Entities;

namespace Game.Networking.Packets.Bpay
{
    public class DistributionAssignToTarget : ClientPacket
    {

        public DistributionAssignToTarget(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            ProductID = _worldPacket.ReadUInt32();
            DistributionID = _worldPacket.ReadUInt64();
            TargetCharacter = _worldPacket.ReadPackedGuid();
            SpecializationID = _worldPacket.ReadUInt16();
            ChoiceID = _worldPacket.ReadUInt16();
        }

        public ObjectGuid TargetCharacter { get; set; } = new ObjectGuid();
        public ulong DistributionID { get; set; } = 0;
        public uint ProductID { get; set; } = 0;
        public ushort SpecializationID { get; set; } = 0;
        public ushort ChoiceID { get; set; } = 0;
    }

}
