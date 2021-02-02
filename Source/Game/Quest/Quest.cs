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

using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using Game.Networking.Packets;
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
            PackageID = fields.Read<uint>(2);
            ContentTuningId = fields.Read<uint>(3);
            QuestSortID = fields.Read<short>(4);
            QuestInfoID = fields.Read<ushort>(5);
            SuggestedPlayers = fields.Read<uint>(6);
            NextQuestInChain = fields.Read<uint>(7);
            RewardXPDifficulty = fields.Read<uint>(8);
            RewardXPMultiplier = fields.Read<float>(9);
            RewardMoney = fields.Read<int>(10);
            RewardMoneyDifficulty = fields.Read<uint>(11);
            RewardMoneyMultiplier = fields.Read<float>(12);
            RewardBonusMoney = fields.Read<uint>(13);
            RewardSpell = fields.Read<uint>(14);
            RewardHonor = fields.Read<uint>(15);
            RewardKillHonor = fields.Read<uint>(16);
            SourceItemId = fields.Read<uint>(17);
            RewardArtifactXPDifficulty = fields.Read<uint>(18);
            RewardArtifactXPMultiplier = fields.Read<float>(19);
            RewardArtifactCategoryID = fields.Read<uint>(20);
            Flags = (QuestFlags)fields.Read<uint>(21);
            FlagsEx = (QuestFlagsEx)fields.Read<uint>(22);
            FlagsEx2 = (QuestFlagsEx2)fields.Read<uint>(23);

            for (int i = 0; i < SharedConst.QuestItemDropCount; ++i)
            {
                RewardItemId[i] = fields.Read<uint>(24 + i * 4);
                RewardItemCount[i] = fields.Read<uint>(25 + i * 4);
                ItemDrop[i] = fields.Read<uint>(26 + i * 4);
                ItemDropQuantity[i] = fields.Read<uint>(27 + i * 4);

                if (RewardItemId[i] != 0)
                    ++_rewItemsCount;
            }

            for (int i = 0; i < SharedConst.QuestRewardChoicesCount; ++i)
            {
                RewardChoiceItemId[i] = fields.Read<uint>(40 + i * 3);
                RewardChoiceItemCount[i] = fields.Read<uint>(41 + i * 3);
                RewardChoiceItemDisplayId[i] = fields.Read<uint>(42 + i * 3);

                if (RewardChoiceItemId[i] != 0)
                    ++_rewChoiceItemsCount;
            }

            POIContinent = fields.Read<uint>(58);
            POIx = fields.Read<float>(59);
            POIy = fields.Read<float>(60);
            POIPriority = fields.Read<uint>(61);

            RewardTitleId = fields.Read<uint>(62);
            RewardArenaPoints = fields.Read<int>(63);
            RewardSkillId = fields.Read<uint>(64);
            RewardSkillPoints = fields.Read<uint>(65);

            QuestGiverPortrait = fields.Read<uint>(66);
            QuestGiverPortraitMount = fields.Read<uint>(67);
            QuestTurnInPortrait = fields.Read<uint>(68);

            for (int i = 0; i < SharedConst.QuestRewardReputationsCount; ++i)
            {
                RewardFactionId[i] = fields.Read<uint>(69 + i * 4);
                RewardFactionValue[i] = fields.Read<int>(70 + i * 4);
                RewardFactionOverride[i] = fields.Read<int>(71 + i * 4);
                RewardFactionCapIn[i] = fields.Read<int>(72 + i * 4);
            }

            RewardReputationMask = fields.Read<uint>(89);

            for (int i = 0; i < SharedConst.QuestRewardCurrencyCount; ++i)
            {
                RewardCurrencyId[i] = fields.Read<uint>(90 + i * 2);
                RewardCurrencyCount[i] = fields.Read<uint>(91 + i * 2);

                if (RewardCurrencyId[i] != 0)
                    ++_rewCurrencyCount;
            }

            SoundAccept = fields.Read<uint>(98);
            SoundTurnIn = fields.Read<uint>(99);
            AreaGroupID = fields.Read<uint>(100);
            LimitTime = fields.Read<uint>(101);
            AllowableRaces = (long)fields.Read<ulong>(102);
            TreasurePickerID = fields.Read<int>(103);
            Expansion = fields.Read<int>(104);
            ManagedWorldStateID = fields.Read<int>(105);
            QuestSessionBonus = fields.Read<int>(106);

            LogTitle = fields.Read<string>(107);
            LogDescription = fields.Read<string>(108);
            QuestDescription = fields.Read<string>(109);
            AreaDescription = fields.Read<string>(110);
            PortraitGiverText = fields.Read<string>(111);
            PortraitGiverName = fields.Read<string>(112);
            PortraitTurnInText = fields.Read<string>(113);
            PortraitTurnInName = fields.Read<string>(114);
            QuestCompletionLog = fields.Read<string>(115);
        }

        public void LoadRewardDisplaySpell(SQLFields fields)
        {
            uint spellId = fields.Read<uint>(1);
            uint playerConditionId = fields.Read<uint>(2);

            if (!Global.SpellMgr.HasSpellInfo(spellId, Difficulty.None))
            {
                Log.outError(LogFilter.Sql, $"Table `quest_reward_display_spell` has non-existing Spell ({spellId}) set for quest {Id}. Skipped.");
                return;
            }

            if (playerConditionId != 0 && !CliDB.PlayerConditionStorage.ContainsKey(playerConditionId))
            {
                Log.outError(LogFilter.Sql, $"Table `quest_reward_display_spell` has non-existing PlayerCondition ({spellId}) set for quest {Id}. Set to 0.");
                playerConditionId = 0;
            }

            RewardDisplaySpell.Add(new QuestRewardDisplaySpell(spellId, playerConditionId));
        }

        public void LoadRewardChoiceItems(SQLFields fields)
        {
            for (int i = 0; i < SharedConst.QuestRewardChoicesCount; ++i)
                RewardChoiceItemType[i] = (LootItemType)fields.Read<byte>(1 + i);
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
            NextQuestId = fields.Read<uint>(5);
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
            SpecialFlags = (QuestSpecialFlags)fields.Read<byte>(16);
            ScriptId = Global.ObjectMgr.GetScriptId(fields.Read<string>(17));

            if (SpecialFlags.HasAnyFlag(QuestSpecialFlags.AutoAccept))
                Flags |= QuestFlags.AutoAccept;
        }

        public void LoadQuestMailSender(SQLFields fields)
        {
            RewardMailSenderEntry = fields.Read<uint>(1);
        }

        public void LoadQuestObjective(SQLFields fields)
        {
            QuestObjective obj = new QuestObjective();  
            obj.QuestID = fields.Read<uint>(0);
            obj.Id = fields.Read<uint>(1);
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
                if (obj.Id == objID)
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

        public uint XPValue(Player player)
        {
            if (player)
            {
                uint questLevel = (uint)player.GetQuestLevel(this);
                QuestXPRecord questXp = CliDB.QuestXPStorage.LookupByKey(questLevel);
                if (questXp == null || RewardXPDifficulty >= 10)
                    return 0;

                int diffFactor = (int)(2 * (questLevel - player.GetLevel()) + 12);
                if (diffFactor < 1)
                    diffFactor = 1;
                else if (diffFactor > 10)
                    diffFactor = 10;

                uint xp = (uint)(diffFactor * questXp.Difficulty[RewardXPDifficulty] * RewardXPMultiplier / 10);
                if (player.GetLevel() >= Global.ObjectMgr.GetMaxLevelForExpansion(PlayerConst.CurrentExpansion - 1) && player.GetSession().GetExpansion() == PlayerConst.CurrentExpansion && Expansion < (int)PlayerConst.CurrentExpansion)
                    xp = (uint)(xp / 9.0f);

                xp = RoundXPValue(xp);

                if (WorldConfig.GetUIntValue(WorldCfg.MinQuestScaledXpRatio) != 0)
                {
                    uint minScaledXP = RoundXPValue((uint)(questXp.Difficulty[RewardXPDifficulty] * RewardXPMultiplier)) * WorldConfig.GetUIntValue(WorldCfg.MinQuestScaledXpRatio) / 100;
                    xp = Math.Max(minScaledXP, xp);
                }

                return xp;
            }

            return 0;
        }

        public uint MoneyValue(Player player)
        {
            QuestMoneyRewardRecord money = CliDB.QuestMoneyRewardStorage.LookupByKey(player.GetQuestLevel(this));
            if (money != null)
                return (uint)(money.Difficulty[RewardMoneyDifficulty] * RewardMoneyMultiplier);
            else
                return 0;
        }

        public QuestTagType? GetQuestTag()
        {
            QuestInfoRecord questInfo = CliDB.QuestInfoStorage.LookupByKey(QuestInfoID);
            if (questInfo != null)
                return (QuestTagType)questInfo.Type;

            return null;
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

            var displaySpellIndex = 0;
            foreach (QuestRewardDisplaySpell displaySpell in RewardDisplaySpell)
            {
                PlayerConditionRecord playerCondition = CliDB.PlayerConditionStorage.LookupByKey(displaySpell.PlayerConditionId);
                if (playerCondition != null)
                    if (!ConditionManager.IsPlayerMeetingCondition(player, playerCondition))
                        continue;

                rewards.SpellCompletionDisplayID[displaySpellIndex] = (int)displaySpell.SpellId;
                if (++displaySpellIndex >= rewards.SpellCompletionDisplayID.Length)
                    break;
            }
            
            rewards.SpellCompletionID = RewardSpell;
            rewards.SkillLineID = RewardSkillId;
            rewards.NumSkillUps = RewardSkillPoints;
            rewards.TreasurePickerID = (uint)TreasurePickerID;

            for (int i = 0; i < SharedConst.QuestRewardChoicesCount; ++i)
            {
                rewards.ChoiceItems[i].LootItemType = RewardChoiceItemType[i];
                rewards.ChoiceItems[i].Item = new ItemInstance();
                rewards.ChoiceItems[i].Item.ItemID = RewardChoiceItemId[i];
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
                rewards.FactionCapIn[i] = RewardFactionCapIn[i];
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

        public void InitializeQueryData()
        {
            QueryData = new QueryQuestInfoResponse();

            QueryData.Allow = true;
            QueryData.QuestID = Id;

            QueryData.Info.LogTitle = LogTitle;
            QueryData.Info.LogDescription = LogDescription;
            QueryData.Info.QuestDescription = QuestDescription;
            QueryData.Info.AreaDescription = AreaDescription;
            QueryData.Info.QuestCompletionLog = QuestCompletionLog;
            QueryData.Info.PortraitGiverText = PortraitGiverText;
            QueryData.Info.PortraitGiverName = PortraitGiverName;
            QueryData.Info.PortraitTurnInText = PortraitTurnInText;
            QueryData.Info.PortraitTurnInName = PortraitTurnInName;

            QueryData.Info.QuestID = Id;
            QueryData.Info.QuestType = (int)Type;
            QueryData.Info.ContentTuningID = ContentTuningId;
            QueryData.Info.QuestPackageID = PackageID;
            QueryData.Info.QuestSortID = QuestSortID;
            QueryData.Info.QuestInfoID = QuestInfoID;
            QueryData.Info.SuggestedGroupNum = SuggestedPlayers;
            QueryData.Info.RewardNextQuest = NextQuestInChain;
            QueryData.Info.RewardXPDifficulty = RewardXPDifficulty;
            QueryData.Info.RewardXPMultiplier = RewardXPMultiplier;

            if (!HasFlag(QuestFlags.HiddenRewards))
                QueryData.Info.RewardMoney = RewardMoney;

            QueryData.Info.RewardMoneyDifficulty = RewardMoneyDifficulty;
            QueryData.Info.RewardMoneyMultiplier = RewardMoneyMultiplier;
            QueryData.Info.RewardBonusMoney = RewardBonusMoney;
            foreach (QuestRewardDisplaySpell displaySpell in RewardDisplaySpell)
                QueryData.Info.RewardDisplaySpell.Add(new QuestCompleteDisplaySpell(displaySpell.SpellId, displaySpell.PlayerConditionId));

            QueryData.Info.RewardSpell = RewardSpell;

            QueryData.Info.RewardHonor = RewardHonor;
            QueryData.Info.RewardKillHonor = RewardKillHonor;

            QueryData.Info.RewardArtifactXPDifficulty = (int)RewardArtifactXPDifficulty;
            QueryData.Info.RewardArtifactXPMultiplier = RewardArtifactXPMultiplier;
            QueryData.Info.RewardArtifactCategoryID = (int)RewardArtifactCategoryID;

            QueryData.Info.StartItem = SourceItemId;
            QueryData.Info.Flags = (uint)Flags;
            QueryData.Info.FlagsEx = (uint)FlagsEx;
            QueryData.Info.FlagsEx2 = (uint)FlagsEx2;
            QueryData.Info.RewardTitle = RewardTitleId;
            QueryData.Info.RewardArenaPoints = RewardArenaPoints;
            QueryData.Info.RewardSkillLineID = RewardSkillId;
            QueryData.Info.RewardNumSkillUps = RewardSkillPoints;
            QueryData.Info.RewardFactionFlags = RewardReputationMask;
            QueryData.Info.PortraitGiver = QuestGiverPortrait;
            QueryData.Info.PortraitGiverMount = QuestGiverPortraitMount;
            QueryData.Info.PortraitTurnIn = QuestTurnInPortrait;

            for (byte i = 0; i < SharedConst.QuestItemDropCount; ++i)
            {
                QueryData.Info.ItemDrop[i] = (int)ItemDrop[i];
                QueryData.Info.ItemDropQuantity[i] = (int)ItemDropQuantity[i];
            }

            if (!HasFlag(QuestFlags.HiddenRewards))
            {
                for (byte i = 0; i < SharedConst.QuestRewardItemCount; ++i)
                {
                    QueryData.Info.RewardItems[i] = RewardItemId[i];
                    QueryData.Info.RewardAmount[i] = RewardItemCount[i];
                }
                for (byte i = 0; i < SharedConst.QuestRewardChoicesCount; ++i)
                {
                    QueryData.Info.UnfilteredChoiceItems[i].ItemID = RewardChoiceItemId[i];
                    QueryData.Info.UnfilteredChoiceItems[i].Quantity = RewardChoiceItemCount[i];
                }
            }

            for (byte i = 0; i < SharedConst.QuestRewardReputationsCount; ++i)
            {
                QueryData.Info.RewardFactionID[i] = RewardFactionId[i];
                QueryData.Info.RewardFactionValue[i] = RewardFactionValue[i];
                QueryData.Info.RewardFactionOverride[i] = RewardFactionOverride[i];
                QueryData.Info.RewardFactionCapIn[i] = RewardFactionCapIn[i];
            }

            QueryData.Info.POIContinent = POIContinent;
            QueryData.Info.POIx = POIx;
            QueryData.Info.POIy = POIy;
            QueryData.Info.POIPriority = POIPriority;

            QueryData.Info.AllowableRaces = AllowableRaces;
            QueryData.Info.TreasurePickerID = TreasurePickerID;
            QueryData.Info.Expansion = Expansion;
            QueryData.Info.ManagedWorldStateID = ManagedWorldStateID;
            QueryData.Info.QuestSessionBonus = 0; //GetQuestSessionBonus(); // this is only sent while quest session is active

            foreach (QuestObjective questObjective in Objectives)
                QueryData.Info.Objectives.Add(questObjective);

            for (int i = 0; i < SharedConst.QuestRewardCurrencyCount; ++i)
            {
                QueryData.Info.RewardCurrencyID[i] = RewardCurrencyId[i];
                QueryData.Info.RewardCurrencyQty[i] = RewardCurrencyCount[i];
            }

            QueryData.Info.AcceptedSoundKitID = SoundAccept;
            QueryData.Info.CompleteSoundKitID = SoundTurnIn;
            QueryData.Info.AreaGroupID = AreaGroupID;
            QueryData.Info.TimeAllowed = LimitTime;
        }

        public static uint RoundXPValue(uint xp)
        {
            if (xp <= 100)
                return 5 * ((xp + 2) / 5);
            else if (xp <= 500)
                return 10 * ((xp + 5) / 10);
            else if (xp <= 1000)
                return 25 * ((xp + 12) / 25);
            else
                return 50 * ((xp + 25) / 50);
        }

        public bool HasFlag(QuestFlags flag) { return (Flags & flag) != 0; }
        public bool HasFlagEx(QuestFlagsEx flag) { return (FlagsEx & flag) != 0; }
        public bool HasFlagEx(QuestFlagsEx2 flag) { return (FlagsEx2 & flag) != 0; }

        public bool HasSpecialFlag(QuestSpecialFlags flag) { return (SpecialFlags & flag) != 0; }
        public void SetSpecialFlag(QuestSpecialFlags flag) { SpecialFlags |= flag; }
        
        public bool IsAutoPush() { return HasFlagEx(QuestFlagsEx.AutoPush);    }
        public bool IsWorldQuest() { return HasFlagEx(QuestFlagsEx.IsWorldQuest);}

        // Possibly deprecated flag
        public bool IsUnavailable() { return HasFlag(QuestFlags.Unavailable); }

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

        public void SetEventIdForQuest(ushort eventId) { _eventIdForQuest = eventId; }
        public ushort GetEventIdForQuest() { return _eventIdForQuest; }

        #region Fields
        public uint Id;
        public QuestType Type;
        public uint PackageID;
        public uint ContentTuningId;
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
        public List<QuestRewardDisplaySpell> RewardDisplaySpell = new List<QuestRewardDisplaySpell>();
        public uint RewardSpell { get; set; }
        public uint RewardHonor;
        public uint RewardKillHonor;
        public uint RewardArtifactXPDifficulty;
        public float RewardArtifactXPMultiplier;
        public uint RewardArtifactCategoryID;
        public uint SourceItemId { get; set; }
        public QuestFlags Flags { get; set; }
        public QuestFlagsEx FlagsEx;
        public QuestFlagsEx2 FlagsEx2;
        public uint[] RewardItemId = new uint[SharedConst.QuestRewardItemCount];
        public uint[] RewardItemCount = new uint[SharedConst.QuestRewardItemCount];
        public uint[] ItemDrop = new uint[SharedConst.QuestItemDropCount];
        public uint[] ItemDropQuantity = new uint[SharedConst.QuestItemDropCount];
        public LootItemType[] RewardChoiceItemType = new LootItemType[SharedConst.QuestRewardChoicesCount];
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
        public uint QuestGiverPortraitMount;
        public uint QuestTurnInPortrait;
        public uint[] RewardFactionId = new uint[SharedConst.QuestRewardReputationsCount];
        public int[] RewardFactionValue = new int[SharedConst.QuestRewardReputationsCount];
        public int[] RewardFactionOverride = new int[SharedConst.QuestRewardReputationsCount];
        public int[] RewardFactionCapIn = new int[SharedConst.QuestRewardReputationsCount];
        public uint RewardReputationMask;
        public uint[] RewardCurrencyId = new uint[SharedConst.QuestRewardCurrencyCount];
        public uint[] RewardCurrencyCount = new uint[SharedConst.QuestRewardCurrencyCount];
        public uint SoundAccept { get; set; }
        public uint SoundTurnIn { get; set; }
        public uint AreaGroupID;
        public uint LimitTime;
        public long AllowableRaces { get; set; }
        public int TreasurePickerID;
        public int Expansion;
        public int ManagedWorldStateID;
        public int QuestSessionBonus;
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
        public uint NextQuestId;
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

        public List<uint> DependentPreviousQuests = new List<uint>();
        public QueryQuestInfoResponse QueryData;

        uint _rewChoiceItemsCount;
        uint _rewItemsCount;
        uint _rewCurrencyCount;
        ushort _eventIdForQuest;
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
        public StringArray Greeting = new StringArray((int)Locale.Total);
    }

    public class QuestTemplateLocale
    {
        public StringArray LogTitle = new StringArray((int)Locale.Total);
        public StringArray LogDescription = new StringArray((int)Locale.Total);
        public StringArray QuestDescription = new StringArray((int)Locale.Total);
        public StringArray AreaDescription = new StringArray((int)Locale.Total);
        public StringArray PortraitGiverText = new StringArray((int)Locale.Total);
        public StringArray PortraitGiverName = new StringArray((int)Locale.Total);
        public StringArray PortraitTurnInText = new StringArray((int)Locale.Total);
        public StringArray PortraitTurnInName = new StringArray((int)Locale.Total);
        public StringArray QuestCompletionLog = new StringArray((int)Locale.Total);
    }

    public class QuestRequestItemsLocale
    {
        public StringArray CompletionText = new StringArray((int)Locale.Total);
    }

    public class QuestObjectivesLocale
    {
        public StringArray Description = new StringArray((int)Locale.Total);
    }

    public class QuestOfferRewardLocale
    {
        public StringArray RewardText = new StringArray((int)Locale.Total);
    }

    public struct QuestRewardDisplaySpell
    {    
        public uint SpellId;
        public uint PlayerConditionId;

        public QuestRewardDisplaySpell(uint spellId, uint playerConditionId)
        {
            SpellId = spellId;
            PlayerConditionId = playerConditionId;
        }
    }

    public class QuestObjective
    {
        public uint Id;
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
