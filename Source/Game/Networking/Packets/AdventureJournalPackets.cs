// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Networking.Packets
{
    internal class AdventureJournalOpenQuest : ClientPacket
    {
        public uint AdventureJournalID;

        public AdventureJournalOpenQuest(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            AdventureJournalID = _worldPacket.ReadUInt32();
        }
    }

    internal class AdventureJournalUpdateSuggestions : ClientPacket
    {
        public bool OnLevelUp;

        public AdventureJournalUpdateSuggestions(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            OnLevelUp = _worldPacket.HasBit();
        }
    }

    internal class AdventureJournalDataResponse : ServerPacket
    {
        public List<AdventureJournalEntry> AdventureJournalDatas = new();

        public bool OnLevelUp;

        public AdventureJournalDataResponse() : base(ServerOpcodes.AdventureJournalDataResponse)
        {
        }

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
    }

    internal struct AdventureJournalEntry
    {
        public int AdventureJournalID;
        public int Priority;
    }
}