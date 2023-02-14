// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Networking.Packets.Bpay
{
    public class DistributionListResponse : ServerPacket
    {
        public DistributionListResponse() : base(ServerOpcodes.BattlePayGetDistributionListResponse)
        {
        }

        public override void Write()
        {
            _worldPacket.Write(Result);
            _worldPacket.WriteBits((uint)DistributionObject.Count, 11);

            foreach (var objectData in DistributionObject)
                objectData.Write(_worldPacket);
        }

        public uint Result { get; set; } = 0;
        public List<BpayDistributionObject> DistributionObject { get; set; } = new List<BpayDistributionObject>();
    }
}
