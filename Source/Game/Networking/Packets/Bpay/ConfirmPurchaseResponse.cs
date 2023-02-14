// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets.Bpay
{
    public class ConfirmPurchaseResponse : ClientPacket
    {

        public ConfirmPurchaseResponse(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            ConfirmPurchase = _worldPacket.ReadBool();
            ServerToken = _worldPacket.ReadUInt32();
            ClientCurrentPriceFixedPoint = _worldPacket.ReadUInt64();
        }

        public ulong ClientCurrentPriceFixedPoint { get; set; } = 0;
        public uint ServerToken { get; set; } = 0;
        public bool ConfirmPurchase { get; set; } = false;
    }
}
