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

using System.Numerics;

namespace Game.DataStorage
{
    public sealed class JournalEncounterRecord
    {
        public uint Id;
        public LocalizedString Name;
        public LocalizedString Description;
        public Vector2 Map;
        public ushort JournalInstanceID;
        public uint OrderIndex;
        public ushort FirstSectionID;
        public ushort UiMapID;
        public uint MapDisplayConditionID;
        public int Flags;
        public sbyte DifficultyMask;
    }

    public sealed class JournalEncounterSectionRecord
    {
        public uint Id;
        public LocalizedString Title;
        public LocalizedString BodyText;
        public ushort JournalEncounterID;
        public byte OrderIndex;
        public ushort ParentSectionID;
        public ushort FirstChildSectionID;
        public ushort NextSiblingSectionID;
        public byte Type;
        public uint IconCreatureDisplayInfoID;
        public int UiModelSceneID;
        public int SpellID;
        public int IconFileDataID;
        public int Flags;
        public int IconFlags;
        public sbyte DifficultyMask;
    }

    public sealed class JournalInstanceRecord
    {        
        public LocalizedString Name;
        public LocalizedString Description;
        public uint Id;
        public ushort MapID;
        public int BackgroundFileDataID;
        public int ButtonFileDataID;
        public int ButtonSmallFileDataID;
        public int LoreFileDataID;
        public byte OrderIndex;
        public int Flags;
        public ushort AreaID;
    }

    public sealed class JournalTierRecord
    {
        public uint Id;
        public LocalizedString Name;
    }
}
