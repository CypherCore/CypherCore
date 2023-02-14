// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
