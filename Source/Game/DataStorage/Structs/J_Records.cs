// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Numerics;

namespace Game.DataStorage
{
	public sealed class JournalEncounterRecord
	{
		public LocalizedString Description;
		public sbyte DifficultyMask;
		public ushort DungeonEncounterID;
		public ushort FirstSectionID;
		public int Flags;
		public uint Id;
		public ushort JournalInstanceID;
		public Vector2 Map;
		public uint MapDisplayConditionID;
		public LocalizedString Name;
		public uint OrderIndex;
		public ushort UiMapID;
	}

	public sealed class JournalEncounterSectionRecord
	{
		public LocalizedString BodyText;
		public sbyte DifficultyMask;
		public ushort FirstChildSectionID;
		public int Flags;
		public uint IconCreatureDisplayInfoID;
		public int IconFileDataID;
		public int IconFlags;
		public uint Id;
		public ushort JournalEncounterID;
		public ushort NextSiblingSectionID;
		public byte OrderIndex;
		public ushort ParentSectionID;
		public int SpellID;
		public LocalizedString Title;
		public byte Type;
		public int UiModelSceneID;
	}

	public sealed class JournalInstanceRecord
	{
		public ushort AreaID;
		public int BackgroundFileDataID;
		public int ButtonFileDataID;
		public int ButtonSmallFileDataID;
		public LocalizedString Description;
		public int Flags;
		public uint Id;
		public int LoreFileDataID;
		public ushort MapID;
		public LocalizedString Name;
	}

	public sealed class JournalTierRecord
	{
		public uint Id;
		public LocalizedString Name;
		public int PlayerConditionID;
	}
}