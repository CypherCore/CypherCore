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

namespace Game.DataStorage
{
    public sealed class BankBagSlotPricesRecord
    {
        public uint Id;
        public uint Cost;
    }

    public sealed class BannedAddOnsRecord
    {
        public uint Id;
        public string Name;
        public string Version;
        public byte[] Flags = new byte[4];
    }

    public sealed class BarberShopStyleRecord
    {
        public LocalizedString DisplayName;
        public LocalizedString Description;
        public float CostModifier;
        public byte Type;
        public byte Race;
        public byte Sex;
        public byte Data;
        public uint Id;
    }

    public sealed class BattlePetBreedQualityRecord
    {
        public uint Id;
        public float Modifier;
        public byte Quality;
    }

    public sealed class BattlePetBreedStateRecord
    {
        public uint Id;
        public short Value;
        public byte BreedID;
        public byte State;
    }

    public sealed class BattlePetSpeciesRecord
    {
        public uint CreatureID;
        public uint IconFileID;
        public uint SummonSpellID;
        public LocalizedString SourceText;
        public LocalizedString Description;
        public ushort Flags;
        public byte PetType;
        public sbyte Source;
        public uint Id;
        public uint CardModelSceneID;
        public uint LoadoutModelSceneID;
    }

    public sealed class BattlePetSpeciesStateRecord
    {
        public uint Id;
        public int Value;
        public ushort SpeciesID;
        public byte State;
    }

    public sealed class BattlemasterListRecord
    {
        public uint Id;
        public LocalizedString Name;
        public uint IconFileDataID;
        public LocalizedString GameType;
        public LocalizedString ShortDescription;
        public LocalizedString LongDescription;
        public short[] MapId = new short[16];
        public ushort HolidayWorldState;
        public ushort PlayerConditionId;
        public byte InstanceType;
        public byte GroupsAllowed;
        public byte MaxGroupSize;
        public byte MinLevel;
        public byte MaxLevel;
        public byte RatedPlayers;
        public byte MinPlayers;
        public byte MaxPlayers;
        public byte Flags;
    }

    public sealed class BroadcastTextRecord
    {
        public uint Id;
        public LocalizedString MaleText;
        public LocalizedString FemaleText;
        public ushort[] EmoteID = new ushort[3];
        public ushort[] EmoteDelay = new ushort[3];
        public ushort UnkEmoteID;
        public byte Language;
        public byte Type;
        public uint[] SoundID = new uint[2];
        public uint PlayerConditionID;
    }
}
