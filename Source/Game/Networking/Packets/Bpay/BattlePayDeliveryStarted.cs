// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
