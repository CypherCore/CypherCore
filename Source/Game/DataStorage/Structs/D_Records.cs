/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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

namespace Game.DataStorage
{
    public sealed class DestructibleModelDataRecord
    {
        public uint Id;
        public sbyte State0ImpactEffectDoodadSet;
        public byte State0AmbientDoodadSet;
        public ushort State1Wmo;
        public sbyte State1DestructionDoodadSet;
        public sbyte State1ImpactEffectDoodadSet;
        public byte State1AmbientDoodadSet;
        public ushort State2Wmo;
        public sbyte State2DestructionDoodadSet;
        public sbyte State2ImpactEffectDoodadSet;
        public byte State2AmbientDoodadSet;
        public ushort State3Wmo;
        public byte State3InitDoodadSet;
        public byte State3AmbientDoodadSet;
        public byte EjectDirection;
        public byte DoNotHighlight;
        public ushort State0Wmo;
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
        public DifficultyFlags Flags;
        public byte ItemContext;
        public byte ToggleDifficultyID;
        public ushort GroupSizeHealthCurveID;
        public ushort GroupSizeDmgCurveID;
        public ushort GroupSizeSpellPointsCurveID;
    }

    public sealed class DungeonEncounterRecord
    {
        public LocalizedString Name;
        public uint Id;
        public short MapID;
        public sbyte DifficultyID;
        public int OrderIndex;
        public sbyte Bit;
        public int CreatureDisplayID;
        public byte Flags;
        public int SpellIconFileID;
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
