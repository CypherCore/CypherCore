// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Networking.Packets
{
    internal class AdventureMapStartQuest : ClientPacket
    {
        public uint QuestID;

        public AdventureMapStartQuest(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            QuestID = _worldPacket.ReadUInt32();
        }
    }
}