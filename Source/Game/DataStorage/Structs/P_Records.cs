﻿/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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
        public ushort Flags;
    }

    public sealed class PhaseXPhaseGroupRecord
    {
        public uint Id;
        public ushort PhaseId;
        public ushort PhaseGroupID;
    }

    public sealed class PlayerConditionRecord
    {
        public uint Id;
        public uint RaceMask;
        public uint[] Time = new uint[2];
        public uint[] AuraSpellID = new uint[4];
        public LocalizedString FailureDescription;
        public ushort[] SkillID = new ushort[4];
        public short[] MinSkill = new short[4];
        public short[] MaxSkill = new short[4];
        public ushort[] PrevQuestID = new ushort[4];
        public ushort[] CurrQuestID = new ushort[4];
        public ushort[] CurrentCompletedQuestID = new ushort[4];
        public ushort[] Explored = new ushort[2];
        public ushort[] Achievement = new ushort[4];
        public ushort[] AreaID = new ushort[4];
        public byte Flags;
        public byte[] MinReputation = new byte[3];
        public byte[] AuraCount = new byte[4];
        public byte[] LfgStatus = new byte[4];
        public byte[] LfgCompare = new byte[4];
        public byte[] CurrencyCount = new byte[4];
        public int ClassMask;
        public uint[] MinFactionID = new uint[3];
        public uint[] SpellID = new uint[4];
        public uint[] ItemID = new uint[4];
        public uint[] ItemCount = new uint[4];
        public uint[] LfgValue = new uint[4];
        public uint[] CurrencyID = new uint[4];
        public uint[] QuestKillMonster = new uint[6];
        public int[] MovementFlags = new int[2];
        public ushort MinLevel;
        public ushort MaxLevel;
        public sbyte Gender;
        public sbyte NativeGender;
        public uint SkillLogic;
        public byte LanguageID;
        public byte MinLanguage;
        public uint MaxLanguage;
        public ushort MaxFactionID;
        public byte MaxReputation;
        public uint ReputationLogic;
        public byte Unknown1;
        public byte MinPVPRank;
        public byte MaxPVPRank;
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
        public uint LfgLogic;
        public uint AreaLogic;
        public uint CurrencyLogic;
        public ushort QuestKillID;
        public uint QuestKillLogic;
        public sbyte MinExpansionLevel;
        public sbyte MaxExpansionLevel;
        public sbyte MinExpansionTier;
        public sbyte MaxExpansionTier;
        public byte MinGuildLevel;
        public byte MaxGuildLevel;
        public byte PhaseUseFlags;
        public ushort PhaseID;
        public uint PhaseGroupID;
        public uint MinAvgItemLevel;
        public uint MaxAvgItemLevel;
        public ushort MinAvgEquippedItemLevel;
        public ushort MaxAvgEquippedItemLevel;
        public sbyte ChrSpecializationIndex;
        public sbyte ChrSpecializationRole;
        public sbyte PowerType;
        public sbyte PowerTypeComp;
        public sbyte PowerTypeValue;
        public uint ModifierTreeID;
        public uint MainHandItemSubclassMask;
    }

    public sealed class PowerDisplayRecord
    {
        public uint Id;
        public uint GlobalStringBaseTag;
        public byte PowerType;
        public byte Red;
        public byte Green;
        public byte Blue;
    }

    public sealed class PowerTypeRecord
    {
        public uint Id;
        public string PowerTypeToken;
        public string PowerCostToken;
        public float RegenerationPeace;
        public float RegenerationCombat;
        public short MaxPower;
        public ushort RegenerationDelay;
        public ushort Flags;
        public PowerType PowerTypeEnum;
        public sbyte RegenerationMin;
        public sbyte RegenerationCenter;
        public sbyte RegenerationMax;
        public byte UIModifier;
    }

    public sealed class PrestigeLevelInfoRecord
    {
        public uint ID;
        public uint IconID;
        public LocalizedString PrestigeText;
        public byte PrestigeLevel;
        public PrestigeLevelInfoFlags Flags;

        public bool IsDisabled() { return Flags.HasAnyFlag(PrestigeLevelInfoFlags.Disabled); }
    }

    public sealed class PVPDifficultyRecord
    {
        public uint Id;
        public ushort MapID;
        public byte BracketID;
        public byte MinLevel;
        public byte MaxLevel;

        // helpers
        public BattlegroundBracketId GetBracketId() { return (BattlegroundBracketId)BracketID; }
    }

    public sealed class PvpRewardRecord
    {
        public uint Id;
        public uint HonorLevel;
        public uint Prestige;
        public uint RewardPackID;
    }
}
