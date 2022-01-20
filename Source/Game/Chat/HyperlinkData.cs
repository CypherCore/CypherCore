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

using Game.DataStorage;
using Game.Entities;
using Game.Spells;
using System.Collections.Generic;

namespace Game.Chat
{
    struct AchievementLinkData
    {
        public AchievementRecord Achievement;
        public ObjectGuid CharacterId;
        public bool IsFinished;
        public ushort Year;
        public byte Month;
        public byte Day;
        public uint[] Criteria = new uint[4];
    }

    struct ArtifactPowerLinkData
    {
        public ArtifactPowerRankRecord ArtifactPower;
        public byte PurchasedRank;
        public byte CurrentRankWithBonus;
    }

    struct AzeriteEssenceLinkData
    {
        public AzeriteEssenceRecord Essence;
        public byte Rank;
    }

    struct BattlePetLinkData
    {
        public BattlePetSpeciesRecord Species;
        public byte Level;
        public byte Quality;
        public uint MaxHealth;
        public uint Power;
        public uint Speed;
        public ObjectGuid PetGuid;
        public uint DisplayId;
    }

    struct CurrencyLinkData
    {
        public CurrencyTypesRecord Currency;
        public int Quantity;

        public CurrencyContainerRecord Container;
    }

    struct GarrisonFollowerLinkData
    {
        public GarrFollowerRecord Follower;
        public uint Quality;
        public uint Level;
        public uint ItemLevel;
        public uint[] Abilities = new uint[4];
        public uint[] Traits = new uint[4];
        public uint Specialization;
    }

    struct GarrisonMissionLinkData
    {
        public GarrMissionRecord Mission;
        public ulong DbID;
    }

    struct InstanceLockLinkData
    {
        public ObjectGuid Owner;
        public MapRecord Map;
        public uint Difficulty;
        public uint CompletedEncountersMask;
    }

    struct ItemLinkData
    {
        public ItemTemplate Item;
        public uint EnchantId;
        public uint[] GemItemId = new uint[3];
        public byte RenderLevel;
        public uint RenderSpecialization;
        public byte Context;
        public List<int> ItemBonusListIDs;

        public struct Modifier
        {
            public uint Type;
            public int Value;
        }

        public List<Modifier> Modifiers;
        public List<int>[] GemItemBonusListIDs = new List<int>[3];
        public ObjectGuid Creator;
        public uint UseEnchantId;

        public uint Quality;
        public ItemNameDescriptionRecord Suffix;
    }

    struct JournalLinkData
    {
        public enum Types : byte
        {
            Instance = 0,
            Encounter = 1,
            EncounterSection = 2,
            Tier = 3
        }

        public byte Type;
        public LocalizedString ExpectedText;
        public uint Difficulty;
    }

    struct KeystoneLinkData
    {
        public uint ItemId;
        public MapChallengeModeRecord Map;
        public uint Level;
        public uint[] Affix = new uint[4];
    }

    struct QuestLinkData
    {
        public Quest Quest;
        public uint ContentTuningId;
    }

    struct SpellLinkData
    {
        public SpellInfo Spell;
        public GlyphPropertiesRecord Glyph;
    }

    struct TradeskillLinkData
    {
        public ObjectGuid Owner;
        public SpellInfo Spell;
        public SkillLineRecord Skill;
    }

    struct WorldMapLinkData
    {
        public UiMapRecord UiMap;
        public uint X;
        public uint Y;
        public uint? Z;
    }
}
