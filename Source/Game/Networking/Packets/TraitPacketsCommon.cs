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
using Game.Entities;
using System.Collections.Generic;
using System;

namespace Game.Networking.Packets
{
    public struct TraitEntry
    {
        public int TraitNodeID;
        public int TraitNodeEntryID;
        public int Rank;
        public int GrantedRanks;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(TraitNodeID);
            data.WriteInt32(TraitNodeEntryID);
            data.WriteInt32(Rank);
            data.WriteInt32(GrantedRanks);
        }
    }

    public class TraitConfig
    {
        public int ID;
        public TraitConfigType Type;
        public int ChrSpecializationID = 0;
        public TraitCombatConfigFlags CombatConfigFlags;
        public int LocalIdentifier;  // Local to specialization
        public int SkillLineID;
        public int TraitSystemID;
        public List<TraitEntry> Entries = new();
        public string Name = "";

        public void Write(WorldPacket data)
        {
            data.WriteInt32(ID);
            data.WriteInt32((int)Type);
            data.WriteInt32(Entries.Count);
            switch (Type)
            {
                case TraitConfigType.Combat:
                    data.WriteInt32(ChrSpecializationID);
                    data.WriteInt32((int)CombatConfigFlags);
                    data.WriteInt32(LocalIdentifier);
                    break;
                case TraitConfigType.Profession:
                    data.WriteInt32(SkillLineID);
                    break;
                case TraitConfigType.Generic:
                    data.WriteInt32(TraitSystemID);
                    break;
                default:
                    break;
            }

            foreach (TraitEntry traitEntry in Entries)
                traitEntry.Write(data);

            data.WriteBits(Name.GetByteCount(), 9);
            data.FlushBits();

            data.WriteString(Name);
        }
    }
}
