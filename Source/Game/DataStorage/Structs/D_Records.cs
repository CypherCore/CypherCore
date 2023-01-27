// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.DataStorage
{
	public sealed class DestructibleModelDataRecord
	{
		public byte DoNotHighlight;
		public byte EjectDirection;
		public byte HealEffect;
		public ushort HealEffectSpeed;
		public uint Id;
		public byte State0AmbientDoodadSet;
		public sbyte State0ImpactEffectDoodadSet;
		public byte State0NameSet;
		public uint State0Wmo;
		public byte State1AmbientDoodadSet;
		public sbyte State1DestructionDoodadSet;
		public sbyte State1ImpactEffectDoodadSet;
		public byte State1NameSet;
		public uint State1Wmo;
		public byte State2AmbientDoodadSet;
		public sbyte State2DestructionDoodadSet;
		public sbyte State2ImpactEffectDoodadSet;
		public byte State2NameSet;
		public uint State2Wmo;
		public byte State3AmbientDoodadSet;
		public byte State3InitDoodadSet;
		public byte State3NameSet;
		public uint State3Wmo;
	}

	public sealed class DifficultyRecord
	{
		public byte FallbackDifficultyID;
		public DifficultyFlags Flags;
		public ushort GroupSizeDmgCurveID;
		public ushort GroupSizeHealthCurveID;
		public ushort GroupSizeSpellPointsCurveID;
		public uint Id;
		public MapTypes InstanceType;
		public byte ItemContext;
		public byte MaxPlayers;
		public byte MinPlayers;
		public string Name;
		public sbyte OldEnumValue;
		public byte OrderIndex;
		public byte ToggleDifficultyID;
	}

	public sealed class DungeonEncounterRecord
	{
		public sbyte Bit;
		public int CompleteWorldStateID;
		public int DifficultyID;
		public int Faction;
		public int Flags;
		public uint Id;
		public short MapID;
		public LocalizedString Name;
		public int OrderIndex;
		public int SpellIconFileID;
	}

	public sealed class DurabilityCostsRecord
	{
		public ushort[] ArmorSubClassCost = new ushort[8];
		public uint Id;
		public ushort[] WeaponSubClassCost = new ushort[21];
	}

	public sealed class DurabilityQualityRecord
	{
		public float Data;
		public uint Id;
	}
}