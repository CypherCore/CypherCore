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

using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using Game.Network.Packets;
using System;
using System.Collections.Generic;

namespace Game
{
    public class Quest
    {
        public Quest(SQLFields fields)
        {
            Id = fields.Read<uint>(0);
            Type = (QuestType)fields.Read<byte>(1);
            Level = fields.Read<int>(2);
            MaxScalingLevel = fields.Read<int>(3);
            PackageID = fields.Read<uint>(4);
            MinLevel = fields.Read<int>(5);
            QuestSortID = fields.Read<short>(6);
            QuestInfoID = fields.Read<ushort>(7);
            SuggestedPlayers = fields.Read<uint>(8);
            NextQuestInChain = fields.Read<uint>(9);
            RewardXPDifficulty = fields.Read<uint>(10);
            RewardXPMultiplier = fields.Read<float>(11);
            RewardMoney = fields.Read<int>(12);
            RewardMoneyDifficulty = fields.Read<uint>(13);
            RewardMoneyMultiplier = fields.Read<float>(14);
            RewardBonusMoney = fields.Read<uint>(15);

            for (int i = 0; i < SharedConst.QuestRewardDisplaySpellCount; ++i)
                RewardDisplaySpell[i] = fields.Read<uint>(16 + i);

            RewardSpell = fields.Read<uint>(19);
            RewardHonor = fields.Read<uint>(20);
            RewardKillHonor = fields.Read<uint>(21);
            SourceItemId = fields.Read<uint>(22);
            RewardArtifactXPDifficulty = fields.Read<uint>(23);
            RewardArtifactXPMultiplier = fields.Read<float>(24);
            RewardArtifactCategoryID = fields.Read<uint>(25);
            Flags = (QuestFlags)fields.Read<uint>(26);
            FlagsEx = (QuestFlagsEx)fields.Read<uint>(27);

            for (int i = 0; i < SharedConst.QuestItemDropCount; ++i)
            {
                RewardItemId[i] = fields.Read<uint>(28 + i * 4);
                RewardItemCount[i] = fields.Read<uint>(29 + i * 4);
                ItemDrop[i] = fields.Read<uint>(30 + i * 4);
                ItemDropQuantity[i] = fields.Read<uint>(31 + i * 4);

                if (RewardItemId[i] != 0)
                    ++_rewItemsCount;
            }

            for (int i = 0; i < SharedConst.QuestRewardChoicesCount; ++i)
            {
                RewardChoiceItemId[i] = fields.Read<uint>(44 + i * 3);
                RewardChoiceItemCount[i] = fields.Read<uint>(45 + i * 3);
                RewardChoiceItemDisplayId[i] = fields.Read<uint>(46 + i * 3);

                if (RewardChoiceItemId[i] != 0)
                    ++_rewChoiceItemsCount;
            }

            POIContinent = fields.Read<uint>(62);
            POIx = fields.Read<float>(63);
            POIy = fields.Read<float>(64);
            POIPriority = fields.Read<uint>(65);

            RewardTitleId = fields.Read<uint>(66);
            RewardArenaPoints = fields.Read<int>(67);
            RewardSkillId = fields.Read<uint>(68);
            RewardSkillPoints = fields.Read<uint>(69);

            QuestGiverPortrait = fields.Read<uint>(70);
            QuestTurnInPortrait = fields.Read<uint>(71);

            for (int i = 0; i < SharedConst.QuestRewardReputationsCount; ++i)
            {
                RewardFactionId[i] = fields.Read<uint>(72 + i * 4);
                RewardFactionValue[i] = fields.Read<int>(73 + i * 4);
                RewardFactionOverride[i] = fields.Read<int>(74 + i * 4);
                RewardFactionCapIn[i] = fields.Read<uint>(75 + i * 4);
            }

            RewardReputationMask = fields.Read<uint>(92);

            for (int i = 0; i < SharedConst.QuestRewardCurrencyCount; ++i)
            {
                RewardCurrencyId[i] = fields.Read<uint>(93 + i * 2);
                RewardCurrencyCount[i] = fields.Read<uint>(94 + i * 2);

                if (RewardCurrencyId[i] != 0)
                    ++_rewCurrencyCount;
            }

            SoundAccept = fields.Read<uint>(101);
            SoundTurnIn = fields.Read<uint>(102);
            AreaGroupID = fields.Read<uint>(103);
            LimitTime = fields.Read<uint>(104);
            AllowableRaces = fields.Read<long>(105);
            QuestRewardID = fields.Read<uint>(106);
            Expansion = fields.Read<int>(107);

            LogTitle = fields.Read<string>(108);
            LogDescription = fields.Read<string>(109);
            QuestDescription = fields.Read<string>(110);
            AreaDescription = fields.Read<string>(111);
            PortraitGiverText = fields.Read<string>(112);
            PortraitGiverName = fields.Read<string>(113);
            PortraitTurnInText = fields.Read<string>(114);
            PortraitTurnInName = fields.Read<string>(115);
            QuestCompletionLog = fields.Read<string>(116);
        }

        public void LoadQuestDetails(SQLFields fields)
        {
            for (int i = 0; i < SharedConst.QuestEmoteCount; ++i)
            {
                ushort emoteId = fields.Read<ushort>(1 + i);
                if (!CliDB.EmotesStorage.ContainsKey(emoteId))
                {
                    Log.outError(LogFilter.Sql, "Table `quest_details` has non-existing Emote{0} ({1}) set for quest {2}. Skipped.", 1 + i, emoteId, fields.Read<uint>(0));
                    continue;
                }
                DetailsEmote[i] = emoteId;
            }

            for (int i = 0; i < SharedConst.QuestEmoteCount; ++i)
                DetailsEmoteDelay[i] = fields.Read<uint>(5 + i);
        }

        public void LoadQuestRequestItems(SQLFields fields)
        {
            EmoteOnComplete = fields.Read<ushort>(1);
            EmoteOnIncomplete = fields.Read<ushort>(2);

            if (!CliDB.EmotesStorage.ContainsKey(EmoteOnComplete))
                Log.outError(LogFilter.Sql, "Table `quest_request_items` has non-existing EmoteOnComplete ({0}) set for quest {1}.", EmoteOnComplete, fields.Read<uint>(0));

            if (!CliDB.EmotesStorage.ContainsKey(EmoteOnIncomplete))
                Log.outError(LogFilter.Sql, "Table `quest_request_items` has non-existing EmoteOnIncomplete ({0}) set for quest {1}.", EmoteOnIncomplete, fields.Read<uint>(0));

            EmoteOnCompleteDelay = fields.Read<uint>(3);
            EmoteOnIncompleteDelay = fields.Read<uint>(4);
            RequestItemsText = fields.Read<string>(5);
        }

        public void LoadQuestOfferReward(SQLFields fields)
        {
            for (int i = 0; i < SharedConst.QuestEmoteCount; ++i)
            {
                ushort emoteId = fields.Read<ushort>(1 + i);
                if (!CliDB.EmotesStorage.ContainsKey(emoteId))
                {
                    Log.outError(LogFilter.Sql, "Table `quest_offer_reward` has non-existing Emote{0} ({1}) set for quest {2}. Skipped.", 1 + i, emoteId, fields.Read<uint>(0));
                    continue;
                }
                OfferRewardEmote[i] = emoteId;
            }

            for (int i = 0; i < SharedConst.QuestEmoteCount; ++i)
                OfferRewardEmoteDelay[i] = fields.Read<uint>(5 + i);

            OfferRewardText = fields.Read<string>(9);
        }

        public void LoadQuestTemplateAddon(SQLFields fields)
        {
            MaxLevel = fields.Read<byte>(1);
            AllowableClasses = fields.Read<uint>(2);
            SourceSpellID = fields.Read<uint>(3);
            PrevQuestId = fields.Read<int>(4);
            NextQuestId = fields.Read<int>(5);
            ExclusiveGroup = fields.Read<int>(6);
            RewardMailTemplateId = fields.Read<uint>(7);
            RewardMailDelay = fields.Read<uint>(8);
            RequiredSkillId = fields.Read<ushort>(9);
            RequiredSkillPoints = fields.Read<ushort>(10);
            RequiredMinRepFaction = fields.Read<ushort>(11);
            RequiredMaxRepFaction = fields.Read<ushort>(12);
            RequiredMinRepValue = fields.Read<int>(13);
            RequiredMaxRepValue = fields.Read<int>(14);
            SourceItemIdCount = fields.Read<byte>(15);
            RewardMailSenderEntry = fields.Read<uint>(16);
            SpecialFlags = (QuestSpecialFlags)fields.Read<byte>(17);
            ScriptId = Global.ObjectMgr.GetScriptId(fields.Read<string>(18));

            if (SpecialFlags.HasAnyFlag(QuestSpecialFlags.AutoAccept))
                Flags |= QuestFlags.AutoAccept;
        }

        public void LoadQuestObjective(SQLFields fields)
        {
            QuestObjective obj = new QuestObjective();
            obj.ID = fields.Read<uint>(0);
            obj.QuestID = fields.Read<uint>(1);
            obj.Type = (QuestObjectiveType)fields.Read<byte>(2);
            obj.StorageIndex = fields.Read<sbyte>(3);
            obj.ObjectID = fields.Read<int>(4);
            obj.Amount = fields.Read<int>(5);
            obj.Flags = (QuestObjectiveFlags)fields.Read<uint>(6);
            obj.Flags2 = fields.Read<uint>(7);
            obj.ProgressBarWeight = fields.Read<float>(8);
            obj.Description = fields.Read<string>(9);

            Objectives.Add(obj);
        }

        public void LoadQuestObjectiveVisualEffect(SQLFields fields)
        {
            uint objID = fields.Read<uint>(1);

            foreach (QuestObjective obj in Objectives)
            {
                if (obj.ID == objID)
                {
                    byte effectIndex = fields.Read<byte>(3);
                    if (obj.VisualEffects == null)
                        obj.VisualEffects = new int[effectIndex + 1];

                    if (effectIndex >= obj.VisualEffects.Length)
                        Array.Resize(ref obj.VisualEffects, effectIndex + 1);

                    obj.VisualEffects[effectIndex] = fields.Read<int>(4);
                    break;
                }
            }
        }

        public uint XPValue(uint playerLevel)
        {
            if (playerLevel != 0)
            {
                uint questLevel = (Level == -1 ? playerLevel : (uint)Level);
                QuestXPRecord questXp = CliDB.QuestXPStorage.LookupByKey(questLevel);
                if (questXp == null || RewardXPDifficulty >= 10)
                    return 0;

                float multiplier = 1.0f;
                if (questLevel != playerLevel)
                    multiplier = CliDB.XpGameTable.GetRow(Math.Min(playerLevel, questLevel)).Divisor / CliDB.XpGameTable.GetRow(playerLevel).Divisor;

                int diffFactor = (int)(2 * (questLevel - playerLevel) + 20);
                if (diffFactor < 1)
                    diffFactor = 1;
                else if (diffFactor > 10)
                    diffFactor = 10;

                uint xp = (uint)(diffFactor * questXp.Difficulty[RewardXPDifficulty] * RewardXPMultiplier / 10 * multiplier);
                if (xp <= 100)
                    xp = 5 * ((xp + 2) / 5);
                else if (xp <= 500)
                    xp = 10 * ((xp + 5) / 10);
                else if (xp <= 1000)
                    xp = 25 * ((xp + 12) / 25);
                else
                    xp = 50 * ((xp + 25) / 50);

                return xp;
            }

            return 0;
        }

        public uint MoneyValue(uint playerLevel)
        {
            uint level = Level == -1 ? playerLevel : (uint)Level;

            QuestMoneyRewardRecord money = CliDB.QuestMoneyRewardStorage.LookupByKey(level);
            if (money != null)
                return (uint)(money.Difficulty[RewardMoneyDifficulty] * RewardMoneyMultiplier);
            else
                return 0;
        }

        public void BuildQuestRewards(QuestRewards rewards, Player player)
        {
            rewards.ChoiceItemCount = GetRewChoiceItemsCount();
            rewards.ItemCount = GetRewItemsCount();
            rewards.Money = player.GetQuestMoneyReward(this);
            rewards.XP = player.GetQuestXPReward(this);
            rewards.ArtifactCategoryID = RewardArtifactCategoryID;
            rewards.Title = RewardTitleId;
            rewards.FactionFlags = RewardReputationMask;
            for (int i = 0; i < SharedConst.QuestRewardDisplaySpellCount; ++i)
                rewards.SpellCompletionDisplayID[i] = (int)RewardDisplaySpell[i];

            rewards.SpellCompletionID = RewardSpell;
            rewards.SkillLineID = RewardSkillId;
            rewards.NumSkillUps = RewardSkillPoints;
            rewards.RewardID = QuestRewardID;

            for (int i = 0; i < SharedConst.QuestRewardChoicesCount; ++i)
            {
                rewards.ChoiceItems[i].ItemID = RewardChoiceItemId[i];
                rewards.ChoiceItems[i].Quantity = RewardChoiceItemCount[i];
            }

            for (int i = 0; i < SharedConst.QuestRewardItemCount; ++i)
            {
                rewards.ItemID[i] = RewardItemId[i];
                rewards.ItemQty[i] = RewardItemCount[i];
            }

            for (int i = 0; i < SharedConst.QuestRewardReputationsCount; ++i)
            {
                rewards.FactionID[i] = RewardFactionId[i];
                rewards.FactionOverride[i] = RewardFactionOverride[i];
                rewards.FactionValue[i] = RewardFactionValue[i];
                rewards.FactionCapIn[i] = (int)RewardFactionCapIn[i];
            }

            for (int i = 0; i < SharedConst.QuestRewardCurrencyCount; ++i)
            {
                rewards.CurrencyID[i] = RewardCurrencyId[i];
                rewards.CurrencyQty[i] = RewardCurrencyCount[i];
            }
        }

        public uint GetRewMoneyMaxLevel()
        {
            // If Quest has flag to not give money on max level, it's 0
            if (HasFlag(QuestFlags.NoMoneyFromXp))
                return 0;

            // Else, return the rewarded copper sum modified by the rate
            return (uint)(RewardBonusMoney * WorldConfig.GetFloatValue(WorldCfg.RateMoneyMaxLevelQuest));
        }

        public bool IsAutoAccept()
        {
            return !WorldConfig.GetBoolValue(WorldCfg.QuestIgnoreAutoAccept) && HasFlag(QuestFlags.AutoAccept);
        }

        public bool IsAutoComplete()
        {
            return !WorldConfig.GetBoolValue(WorldCfg.QuestIgnoreAutoComplete) && Type == QuestType.AutoComplete;
        }

        public bool IsRaidQuest(Difficulty difficulty)
        {
            switch ((QuestInfos)QuestInfoID)
            {
                case QuestInfos.Raid:
                    return true;
                case QuestInfos.Raid10:
                    return difficulty == Difficulty.Raid10N || difficulty == Difficulty.Raid10HC;
                case QuestInfos.Raid25:
                    return difficulty == Difficulty.Raid25N || difficulty == Difficulty.Raid25HC;
                default:
                    break;
            }

            if (Flags.HasAnyFlag(QuestFlags.Raid))
                return true;

            return false;
        }

        public bool IsAllowedInRaid(Difficulty difficulty)
        {
            if (IsRaidQuest(difficulty))
                return true;

            return WorldConfig.GetBoolValue(WorldCfg.QuestIgnoreRaid);
        }

        public uint CalculateHonorGain(uint level)
        {
            uint honor = 0;
            return honor;
        }

        public bool CanIncreaseRewardedQuestCounters()
        {
            // Dungeon Finder/Daily/Repeatable (if not weekly, monthly or seasonal) quests are never considered rewarded serverside.
            // This affects counters and client requests for completed quests.
            return (!IsDFQuest() && !IsDaily() && (!IsRepeatable() || IsWeekly() || IsMonthly() || IsSeasonal()));
        }

        public bool HasFlag(QuestFlags flag) { return (Flags & flag) != 0; }
        public void SetFlag(QuestFlags flag) { Flags |= flag; }

        public bool HasSpecialFlag(QuestSpecialFlags flag) { return (SpecialFlags & flag) != 0; }
        public void SetSpecialFlag(QuestSpecialFlags flag) { SpecialFlags |= flag; }

        public bool HasFlagEx(QuestFlagsEx flag) { return (FlagsEx & flag) != 0; }

        // table data accessors:
        public bool IsRepeatable() { return SpecialFlags.HasAnyFlag(QuestSpecialFlags.Repeatable); }
        public bool IsDaily() { return Flags.HasAnyFlag(QuestFlags.Daily); }
        public bool IsWeekly() { return Flags.HasAnyFlag(QuestFlags.Weekly); }
        public bool IsMonthly() { return SpecialFlags.HasAnyFlag(QuestSpecialFlags.Monthly); }
        public bool IsSeasonal()
        {
            return (QuestSortID == -(int)QuestSort.Seasonal || QuestSortID == -(int)QuestSort.Special || QuestSortID == -(int)QuestSort.LunarFestival
                || QuestSortID == -(int)QuestSort.Midsummer || QuestSortID == -(int)QuestSort.Brewfest || QuestSortID == -(int)QuestSort.LoveIsInTheAir
                || QuestSortID == -(int)QuestSort.Noblegarden) && !IsRepeatable();
        }
        public bool IsDailyOrWeekly() { return Flags.HasAnyFlag(QuestFlags.Daily | QuestFlags.Weekly); }
        public bool IsDFQuest() { return SpecialFlags.HasAnyFlag(QuestSpecialFlags.DfQuest); }

        public uint GetRewChoiceItemsCount() { return _rewChoiceItemsCount; }
        public uint GetRewItemsCount() { return _rewItemsCount; }
        public uint GetRewCurrencyCount() { return _rewCurrencyCount; }

        #region Fields
        public uint Id;
        public QuestType Type;
        public int Level;
        public int MaxScalingLevel;
        public uint PackageID;
        public int MinLevel;
        public int QuestSortID;
        public uint QuestInfoID;
        public uint SuggestedPlayers;
        public uint NextQuestInChain { get; set; }
        public uint RewardXPDifficulty;
        public float RewardXPMultiplier;
        public int RewardMoney;
        public uint RewardMoneyDifficulty;
        public float RewardMoneyMultiplier;
        public uint RewardBonusMoney;
        public uint[] RewardDisplaySpell = new uint[SharedConst.QuestRewardDisplaySpellCount];
        public uint RewardSpell { get; set; }
        public uint RewardHonor;
        public uint RewardKillHonor;
        public uint RewardArtifactXPDifficulty;
        public float RewardArtifactXPMultiplier;
        public uint RewardArtifactCategoryID;
        public uint SourceItemId { get; set; }
        public QuestFlags Flags { get; set; }
        public QuestFlagsEx FlagsEx;
        public uint[] RewardItemId = new uint[SharedConst.QuestRewardItemCount];
        public uint[] RewardItemCount = new uint[SharedConst.QuestRewardItemCount];
        public uint[] ItemDrop = new uint[SharedConst.QuestItemDropCount];
        public uint[] ItemDropQuantity = new uint[SharedConst.QuestItemDropCount];
        public uint[] RewardChoiceItemId = new uint[SharedConst.QuestRewardChoicesCount];
        public uint[] RewardChoiceItemCount = new uint[SharedConst.QuestRewardChoicesCount];
        public uint[] RewardChoiceItemDisplayId = new uint[SharedConst.QuestRewardChoicesCount];
        public uint POIContinent;
        public float POIx;
        public float POIy;
        public uint POIPriority;
        public uint RewardTitleId { get; set; }
        public int RewardArenaPoints;
        public uint RewardSkillId;
        public uint RewardSkillPoints;
        public uint QuestGiverPortrait;
        public uint QuestTurnInPortrait;
        public uint[] RewardFactionId = new uint[SharedConst.QuestRewardReputationsCount];
        public int[] RewardFactionValue = new int[SharedConst.QuestRewardReputationsCount];
        public int[] RewardFactionOverride = new int[SharedConst.QuestRewardReputationsCount];
        public uint[] RewardFactionCapIn = new uint[SharedConst.QuestRewardReputationsCount];
        public uint RewardReputationMask;
        public uint[] RewardCurrencyId = new uint[SharedConst.QuestRewardCurrencyCount];
        public uint[] RewardCurrencyCount = new uint[SharedConst.QuestRewardCurrencyCount];
        public uint SoundAccept { get; set; }
        public uint SoundTurnIn { get; set; }
        public uint AreaGroupID;
        public uint LimitTime;
        public long AllowableRaces { get; set; }
        public uint QuestRewardID;
        public int Expansion;
        public List<QuestObjective> Objectives = new List<QuestObjective>();
        public string LogTitle = "";
        public string LogDescription = "";
        public string QuestDescription = "";
        public string AreaDescription = "";
        public string PortraitGiverText = "";
        public string PortraitGiverName = "";
        public string PortraitTurnInText = "";
        public string PortraitTurnInName = "";
        public string QuestCompletionLog = "";

        // quest_detais table
        public uint[] DetailsEmote = new uint[SharedConst.QuestEmoteCount];
        public uint[] DetailsEmoteDelay = new uint[SharedConst.QuestEmoteCount];

        // quest_request_items table
        public uint EmoteOnComplete;
        public uint EmoteOnIncomplete;
        public uint EmoteOnCompleteDelay;
        public uint EmoteOnIncompleteDelay;
        public string RequestItemsText = "";

        // quest_offer_reward table
        public uint[] OfferRewardEmote = new uint[SharedConst.QuestEmoteCount];
        public uint[] OfferRewardEmoteDelay = new uint[SharedConst.QuestEmoteCount];
        public string OfferRewardText = "";

        // quest_template_addon table (custom data)
        public uint MaxLevel;
        public uint AllowableClasses { get; set; }
        public uint SourceSpellID { get; set; }
        public int PrevQuestId;
        public int NextQuestId;
        public int ExclusiveGroup;
        public uint RewardMailTemplateId { get; set; }
        public uint RewardMailDelay { get; set; }
        public uint RequiredSkillId;
        public uint RequiredSkillPoints;
        public uint RequiredMinRepFaction;
        public int RequiredMinRepValue;
        public uint RequiredMaxRepFaction;
        public int RequiredMaxRepValue;
        public uint SourceItemIdCount;
        public uint RewardMailSenderEntry;
        public QuestSpecialFlags SpecialFlags; // custom flags, not sniffed/WDB
        public uint ScriptId;

        public List<int> prevQuests = new List<int>();
        public List<uint> prevChainQuests = new List<uint>();

        uint _rewChoiceItemsCount;
        uint _rewItemsCount;
        uint _rewCurrencyCount;
        #endregion
    }

    public class QuestStatusData
    {
        public QuestStatus Status;
        public uint Timer;
        public int[] ObjectiveData;
    }

    public class QuestGreeting
    {
        public QuestGreeting()
        {
            Text = "";
        }
        public QuestGreeting(ushort emoteType, uint emoteDelay, string text)
        {
            EmoteType = emoteType;
            EmoteDelay = emoteDelay;
            Text = text;
        }

        public ushort EmoteType;
        public uint EmoteDelay;
        public string Text;
    }

    public class QuestGreetingLocale
    {
        public StringArray Greeting = new StringArray((int)LocaleConstant.Total);
    }

    public class QuestTemplateLocale
    {
        public StringArray LogTitle = new StringArray((int)LocaleConstant.Total);
        public StringArray LogDescription = new StringArray((int)LocaleConstant.Total);
        public StringArray QuestDescription = new StringArray((int)LocaleConstant.Total);
        public StringArray AreaDescription = new StringArray((int)LocaleConstant.Total);
        public StringArray PortraitGiverText = new StringArray((int)LocaleConstant.Total);
        public StringArray PortraitGiverName = new StringArray((int)LocaleConstant.Total);
        public StringArray PortraitTurnInText = new StringArray((int)LocaleConstant.Total);
        public StringArray PortraitTurnInName = new StringArray((int)LocaleConstant.Total);
        public StringArray QuestCompletionLog = new StringArray((int)LocaleConstant.Total);
    }

    public class QuestRequestItemsLocale
    {
        public StringArray CompletionText = new StringArray((int)LocaleConstant.Total);
    }

    public class QuestObjectivesLocale
    {
        public StringArray Description = new StringArray((int)LocaleConstant.Total);
    }

    public class QuestOfferRewardLocale
    {
        public StringArray RewardText = new StringArray((int)LocaleConstant.Total);
    }

    public class QuestObjective
    {
        public uint ID;
        public uint QuestID;
        public QuestObjectiveType Type;
        public sbyte StorageIndex;
        public int ObjectID;
        public int Amount;
        public QuestObjectiveFlags Flags;
        public uint Flags2;
        public float ProgressBarWeight;
        public string Description;
        public int[] VisualEffects = new int[0];

        public bool IsStoringFlag()
        {
            switch (Type)
            {
                case QuestObjectiveType.AreaTrigger:
                case QuestObjectiveType.WinPetBattleAgainstNpc:
                case QuestObjectiveType.DefeatBattlePet:
                case QuestObjectiveType.CriteriaTree:
                    return true;
                default:
                    break;
            }
            return false;
        }
    }
}
