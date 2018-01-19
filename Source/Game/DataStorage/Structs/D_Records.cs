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
        public ushort StateDamagedDisplayID;
        public ushort StateDestroyedDisplayID;
        public ushort StateRebuildingDisplayID;
        public ushort StateSmokeDisplayID;
        public ushort HealEffectSpeed;
        public byte StateDamagedImpactEffectDoodadSet;
        public byte StateDamagedAmbientDoodadSet;
        public byte StateDamagedNameSet;
        public byte StateDestroyedDestructionDoodadSet;
        public byte StateDestroyedImpactEffectDoodadSet;
        public byte StateDestroyedAmbientDoodadSet;
        public byte StateDestroyedNameSet;
        public byte StateRebuildingDestructionDoodadSet;
        public byte StateRebuildingImpactEffectDoodadSet;
        public byte StateRebuildingAmbientDoodadSet;
        public byte StateRebuildingNameSet;
        public byte StateSmokeInitDoodadSet;
        public byte StateSmokeAmbientDoodadSet;
        public byte StateSmokeNameSet;
        public byte EjectDirection;
        public byte DoNotHighlight;
        public byte HealEffect;
    }

    public sealed class DifficultyRecord
    {
        public uint Id;
        public LocalizedString Name;
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
        public byte ItemBonusTreeModID;
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
        public uint TextureFileDataID;
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
        public float QualityMod;
    }
}
