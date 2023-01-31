using System.Collections.Generic;
using Framework.Constants;

namespace Game.Networking.Packets.Bpay
{
    public class DeliveryEnded : ServerPacket
    {
        public DeliveryEnded() : base(ServerOpcodes.BattlePayDeliveryEnded)
        {
        }

        public override void Write()
        {
            _worldPacket.Write(DistributionID);

            _worldPacket.Write(Item.Count);

            foreach (var itemData in Item)
                itemData.Write(_worldPacket);
        }

        public List<ItemInstance> Item { get; set; } = new List<ItemInstance>();
        public ulong DistributionID { get; set; } = 0;
    }
}
