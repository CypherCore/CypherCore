// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
