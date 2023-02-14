// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Networking.Packets.Bpay
{
    public class PurchaseListResponse : ServerPacket
    {
        public PurchaseListResponse() : base(ServerOpcodes.BattlePayGetPurchaseListResponse)
        {
        }

        public override void Write()
        {
            _worldPacket.Write(Result);
            _worldPacket.WriteUInt32((uint)Purchase.Count);

            foreach (var purchaseData in Purchase)
                purchaseData.Write(_worldPacket);
        }

        public uint Result { get; set; } = 0;
        public List<BpayPurchase> Purchase { get; set; } = new List<BpayPurchase>();
    }
}
