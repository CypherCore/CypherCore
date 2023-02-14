// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets.Bpay
{
    public class BattlePayAckFailedResponse : ClientPacket
    {

        public BattlePayAckFailedResponse(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            ServerToken = _worldPacket.ReadUInt32();
        }

        public uint ServerToken { get; set; } = 0;
    }

}
