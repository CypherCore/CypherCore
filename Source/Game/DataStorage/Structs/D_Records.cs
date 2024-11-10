// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.DataStorage
{
    public sealed class DestructibleModelDataRecord
    {
        public uint Id;
        public sbyte State0ImpactEffectDoodadSet;
        public byte State0AmbientDoodadSet;
        public uint State1Wmo;
        public sbyte State1DestructionDoodadSet;
        public sbyte State1ImpactEffectDoodadSet;
        public byte State1AmbientDoodadSet;
        public uint State2Wmo;
        public sbyte State2DestructionDoodadSet;
        public sbyte State2ImpactEffectDoodadSet;
        public byte State2AmbientDoodadSet;
        public uint State3Wmo;
        public byte State3InitDoodadSet;
        public byte State3AmbientDoodadSet;
        public byte EjectDirection;
        public byte DoNotHighlight;
        public uint State0Wmo;
        public byte HealEffect;
        public ushort HealEffectSpeed;
        public byte State0NameSet;
        public byte State1NameSet;
        public byte State2NameSet;
        public byte State3NameSet;
    }

    public sealed class DifficultyRecord
    {
        public uint Id;
        public string Name;
        public MapTypes InstanceType;
        public byte OrderIndex;
        public sbyte OldEnumValue;
        public byte FallbackDifficultyID;
        public byte MinPlayers;
        public byte MaxPlayers;
        public ushort Flags;
        public byte ItemContext;
        public byte ToggleDifficultyID;
        public uint GroupSizeHealthCurveID;
        public uint GroupSizeDmgCurveID;
        public uint GroupSizeSpellPointsCurveID;

        public bool HasFlag(DifficultyFlags difficultyFlags) { return (Flags & (ushort)difficultyFlags) != 0; }
    }

    public sealed class DungeonEncounterRecord
    {
        public LocalizedString Name;
        public uint Id;
        public ushort MapID;
        public int DifficultyID;
        public int OrderIndex;
        public int CompleteWorldStateID;
        public sbyte Bit;
        public int Flags;
        public int SpellIconFileID;
        public int Faction;
    }

    public sealed class DurabilityCostsRecord
    {
        public uint Id;
        public ushort[] WeaponSubClassCost = new ushort[21];
        public ushort[] ArmorSubClassCost = new ushort[8];
    }

    public sealed class DurabilityQualityRecord
    {
        public uint Id;
        public float Data;
    }
}
