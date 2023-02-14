// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Networking.Packets.Bpay
{
    public class PurchaseUpdate : ServerPacket
    {
        public PurchaseUpdate() : base(ServerOpcodes.BattlePayPurchaseUpdate)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32((uint)Purchase.Count);

            foreach (var purchaseData in Purchase)
                purchaseData.Write(_worldPacket);
        }

        public List<BpayPurchase> Purchase { get; set; } = new List<BpayPurchase>();
    }
}
