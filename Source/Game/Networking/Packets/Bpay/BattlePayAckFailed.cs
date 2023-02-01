﻿using Framework.Constants;

namespace Game.Networking.Packets.Bpay
{
    public class BattlePayAckFailed : ServerPacket
    {
        public BattlePayAckFailed() : base(ServerOpcodes.BattlePayAckFailed)
        {
        }

        public override void Write()
        {
            _worldPacket.Write(PurchaseID);
            _worldPacket.Write(PurchaseResult);
            _worldPacket.Write(ClientToken);
        }

        public ulong PurchaseID { get; set; } = 0;
        public uint ClientToken { get; set; } = 0;
        public uint PurchaseResult { get; set; } = 0;
    }
}