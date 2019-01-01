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
using System;

namespace Game.DataStorage
{
    public sealed class PhaseRecord
    {
        public uint Id;
        public PhaseEntryFlags Flags;
    }

    public sealed class PhaseXPhaseGroupRecord
    {
        public uint Id;
        public ushort PhaseId;
        public uint PhaseGroupID;
    }

    public sealed class PlayerConditionRecord
    {
        public ulong RaceMask;
        public string FailureDescription;
        public uint Id;
        public ushort MinLevel;
        public ushort MaxLevel;
        public int ClassMask;
        public uint SkillLogic;
        public byte LanguageID;
        public byte MinLanguage;
        public int MaxLanguage;
        public ushort MaxFactionID;
        public byte MaxReputation;
        public uint ReputationLogic;
        public sbyte CurrentPvpFaction;
        public byte PvpMedal;
        public uint PrevQuestLogic;
        public uint CurrQuestLogic;
        public uint CurrentCompletedQuestLogic;
        public uint SpellLogic;
        public uint ItemLogic;
        public byte ItemFlags;
        public uint AuraSpellLogic;
        public ushort WorldStateExpressionID;
        public byte WeatherID;
        public byte PartyStatus;
        public byte LifetimeMaxPVPRank;
        public uint AchievementLogic;
        public sbyte Gender;
        public sbyte NativeGender;
        public uint AreaLogic;
        public uint LfgLogic;
        public uint CurrencyLogic;
        public ushort QuestKillID;
        public uint QuestKillLogic;
        public sbyte MinExpansionLevel;
        public sbyte MaxExpansionLevel;
        public int MinAvgItemLevel;
        public int MaxAvgItemLevel;
        public ushort MinAvgEquippedItemLevel;
        public ushort MaxAvgEquippedItemLevel;
        public byte PhaseUseFlags;
        public ushort PhaseID;
        public uint PhaseGroupID;
        public byte Flags;
        public sbyte ChrSpecializationIndex;
        public sbyte ChrSpecializationRole;
        public uint ModifierTreeID;
        public sbyte PowerType;
        public byte PowerTypeComp;
        public byte PowerTypeValue;
        public int WeaponSubclassMask;
        public byte MaxGuildLevel;
        public byte MinGuildLevel;
        public sbyte MaxExpansionTier;
        public sbyte MinExpansionTier;
        public byte MinPVPRank;
        public byte MaxPVPRank;
        public ushort[] SkillID = new ushort[4];
        public ushort[] MinSkill = new ushort[4];
        public ushort[] MaxSkill = new ushort[4];
        public uint[] MinFactionID = new uint[3];
        public byte[] MinReputation = new byte[3];
        public ushort[] PrevQuestID = new ushort[4];
        public ushort[] CurrQuestID = new ushort[4];
        public ushort[] CurrentCompletedQuestID = new ushort[4];
        public uint[] SpellID = new uint[4];
        public uint[] ItemID = new uint[4];
        public uint[] ItemCount = new uint[4];
        public ushort[] Explored = new ushort[2];
        public uint[] Time = new uint[2];
        public uint[] AuraSpellID = new uint[4];
        public byte[] AuraStacks = new byte[4];
        public ushort[] Achievement = new ushort[4];
        public ushort[] AreaID = new ushort[4];
        public byte[] LfgStatus = new byte[4];
        public byte[] LfgCompare = new byte[4];
        public uint[] LfgValue = new uint[4];
        public uint[] CurrencyID = new uint[4];
        public uint[] CurrencyCount = new uint[4];
        public uint[] QuestKillMonster = new uint[6];
        public int[] MovementFlags = new int[2];
    }

    public sealed class PowerDisplayRecord
    {
        public uint Id;
        public string GlobalStringBaseTag;
        public byte ActualType;
        public byte Red;
        public byte Green;
        public byte Blue;
    }

    public sealed class PowerTypeRecord
    {
        public uint Id;
        public string NameGlobalStringTag;
        public string CostGlobalStringTag;
        public PowerType PowerTypeEnum;
        public sbyte MinPower;
        public short MaxBasePower;
        public sbyte CenterPower;
        public sbyte DefaultPower;
        public sbyte DisplayModifier;
        public short RegenInterruptTimeMS;
        public float RegenPeace;
        public float RegenCombat;
        public short Flags;
    }

    public sealed class PrestigeLevelInfoRecord
    {
        public uint Id;
        public string Name;
        public int PrestigeLevel;
        public int BadgeTextureFileDataID;
        public PrestigeLevelInfoFlags Flags;
        public int AwardedAchievementID;

        public bool IsDisabled() { return (Flags & PrestigeLevelInfoFlags.Disabled) != 0; }
    }

    public sealed class PvpDifficultyRecord
    {
        public uint Id;
        public byte RangeIndex;
        public byte MinLevel;
        public byte MaxLevel;
        public uint MapID;

        // helpers
        public BattlegroundBracketId GetBracketId()
        {
            return (BattlegroundBracketId)RangeIndex;
        }
    }

    public sealed class PvpItemRecord
    {
        public uint Id;
        public uint ItemID;
        public byte ItemLevelDelta;
    }

    public sealed class PvpTalentRecord
    {
        public string Description;
        public uint Id;
        public int SpecID;
        public uint SpellID;
        public uint OverridesSpellID;
        public int Flags;
        public int ActionBarSpellID;
        public int PvpTalentCategoryID;
        public int LevelRequired;
    }

    public sealed class PvpTalentCategoryRecord
    {
        public uint Id;
        public byte TalentSlotMask;
    }

    public sealed class PvpTalentSlotUnlockRecord
    {
        public uint Id;
        public sbyte Slot;
        public uint LevelRequired;
        public uint DeathKnightLevelRequired;
        public uint DemonHunterLevelRequired;
    }
}
