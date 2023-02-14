// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Game.DataStorage
{
    public sealed class JournalEncounterRecord
    {
        public LocalizedString Name;
        public LocalizedString Description;
        public Vector2 Map;
        public uint Id;
        public ushort JournalInstanceID;
        public ushort DungeonEncounterID;
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
        public uint Id;
        public LocalizedString Name;
        public LocalizedString Description;
        public ushort MapID;
        public int BackgroundFileDataID;
        public int ButtonFileDataID;
        public int ButtonSmallFileDataID;
        public int LoreFileDataID;
        public int Flags;
        public ushort AreaID;
    }

    public sealed class JournalTierRecord
    {
        public uint Id;
        public LocalizedString Name;
        public int PlayerConditionID;
    }
}
