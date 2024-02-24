// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Networking.Packets
{
    class CheckIsAdventureMapPoiValid : ClientPacket
    {
        public uint AdventureMapPoiID;

        public CheckIsAdventureMapPoiValid(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            AdventureMapPoiID = _worldPacket.ReadUInt32();
        }
    }

    class PlayerIsAdventureMapPoiValid : ServerPacket
    {
        public uint AdventureMapPoiID;
        public bool IsVisible;

        public PlayerIsAdventureMapPoiValid() : base(ServerOpcodes.PlayerIsAdventureMapPoiValid, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(AdventureMapPoiID);
            _worldPacket.WriteBit(IsVisible);
            _worldPacket.FlushBits();
        }
    }

    class AdventureMapStartQuest : ClientPacket
    {
        public uint QuestID;

        public AdventureMapStartQuest(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            QuestID = _worldPacket.ReadUInt32();
        }
    }
}
