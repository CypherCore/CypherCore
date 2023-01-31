using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets.Bpay
{
    public class UpgradeStarted : ServerPacket
    {
        public UpgradeStarted() : base(ServerOpcodes.CharacterUpgradeStarted)
        {
        }

        public override void Write()
        {
            _worldPacket.Write(CharacterGUID);
        }

        public ObjectGuid CharacterGUID { get; set; } = new ObjectGuid();
    }
}
