/*
 * Copyright (C) 2012-2018 CypherCore <http://github.com/CypherCore>
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
        public ushort State0Wmo;
        public ushort State1Wmo;
        public ushort State2Wmo;
        public ushort State3Wmo;
        public ushort HealEffectSpeed;
        public byte State0ImpactEffectDoodadSet;
        public byte State0AmbientDoodadSet;
        public byte State0NameSet;
        public byte State1DestructionDoodadSet;
        public byte State1ImpactEffectDoodadSet;
        public byte State1AmbientDoodadSet;
        public byte State1NameSet;
        public byte State2DestructionDoodadSet;
        public byte State2ImpactEffectDoodadSet;
        public byte State2AmbientDoodadSet;
        public byte State2NameSet;
        public byte State3InitDoodadSet;
        public byte State3AmbientDoodadSet;
        public byte State3NameSet;
        public byte EjectDirection;
        public byte DoNotHighlight;
        public byte HealEffect;
    }

    public sealed class DifficultyRecord
    {
        public uint Id;
        public string Name;
        public ushort GroupSizeHealthCurveID;
        public ushort GroupSizeDmgCurveID;
        public ushort GroupSizeSpellPointsCurveID;
        public byte FallbackDifficultyID;
        public MapTypes InstanceType;
        public byte MinPlayers;
        public byte MaxPlayers;
        public sbyte OldEnumValue;
        public DifficultyFlags Flags;
        public byte ToggleDifficultyID;
        public byte ItemContext;
        public byte OrderIndex;
    }

    public sealed class DungeonEncounterRecord
    {
        public LocalizedString Name;
        public uint CreatureDisplayID;
        public ushort MapID;
        public byte DifficultyID;
        public byte Bit;
        public byte Flags;
        public uint Id;
        public uint OrderIndex;
        public uint SpellIconFileID;
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
