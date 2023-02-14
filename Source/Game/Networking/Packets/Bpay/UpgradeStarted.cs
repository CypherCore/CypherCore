// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
