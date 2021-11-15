/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

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
