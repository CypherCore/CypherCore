// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Networking;

namespace Game.DataStorage
{
    public struct HotfixId
    {
        public int PushID;
        public uint UniqueID;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(PushID);
            data.WriteUInt32(UniqueID);
        }

        public void Read(WorldPacket data)
        {
            PushID = data.ReadInt32();
            UniqueID = data.ReadUInt32();
        }
    }
}