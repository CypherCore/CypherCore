// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using System.Collections.Generic;

namespace Game.Networking.Packets
{
    class AdventureJournalOpenQuest : ClientPacket
    {
        public AdventureJournalOpenQuest(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            AdventureJournalID = _worldPacket.ReadUInt32();
        }

        public uint AdventureJournalID;
    }

    class AdventureJournalUpdateSuggestions : ClientPacket
    {
        public AdventureJournalUpdateSuggestions(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            OnLevelUp = _worldPacket.HasBit();
        }

        public bool OnLevelUp;
    }

    class AdventureJournalDataResponse : ServerPacket
    {
        public AdventureJournalDataResponse() : base(ServerOpcodes.AdventureJournalDataResponse) { }

        public override void Write()
        {
            _worldPacket.WriteBit(OnLevelUp);
            _worldPacket.FlushBits();
            _worldPacket.WriteInt32(AdventureJournalDatas.Count);
            foreach (var adventureJournal in AdventureJournalDatas)
            {
                _worldPacket.WriteInt32(adventureJournal.AdventureJournalID);
                _worldPacket.WriteInt32(adventureJournal.Priority);
            }
        }

        public bool OnLevelUp;
        public List<AdventureJournalEntry> AdventureJournalDatas = new();
    }

    struct AdventureJournalEntry
    {
        public int AdventureJournalID;
        public int Priority;
    }
}
