// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Networking;

namespace Game.DataStorage
{
    public class HotfixRecord
    {
        public enum Status
        {
            NotSet = 0,
            Valid = 1,
            RecordRemoved = 2,
            Invalid = 3,
            NotPublic = 4
        }

        public Status HotfixStatus = Status.Invalid;
        public HotfixId ID;
        public int RecordID;
        public uint TableHash;

        public void Write(WorldPacket data)
        {
            ID.Write(data);
            data.WriteUInt32(TableHash);
            data.WriteInt32(RecordID);
        }

        public void Read(WorldPacket data)
        {
            ID.Read(data);
            TableHash = data.ReadUInt32();
            RecordID = data.ReadInt32();
        }
    }
}