// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using Game.Miscellaneous;
using Game.Networking.Packets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
            RewardMoneyDifficulty = fields.Read<uint>(10);
            RewardMoneyMultiplier = fields.Read<float>(11);
            RewardBonusMoney = fields.Read<uint>(12);
            RewardSpell = fields.Read<uint>(13);
            RewardHonor = fields.Read<uint>(14);
            RewardKillHonor = fields.Read<uint>(15);
            SourceItemId = fields.Read<uint>(16);
            RewardArtifactXPDifficulty = fields.Read<uint>(17);
            RewardArtifactXPMultiplier = fields.Read<float>(18);
            RewardArtifactCategoryID = fields.Read<uint>(19);
            Flags = (QuestFlags)fields.Read<uint>(20);
            FlagsEx = (QuestFlagsEx)fields.Read<uint>(21);
            FlagsEx2 = (QuestFlagsEx2)fields.Read<uint>(22);
            FlagsEx3 = fields.Read<uint>(23);

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
            QuestGiverPortraitModelSceneId = fields.Read<int>(68);
            QuestTurnInPortrait = fields.Read<uint>(69);

            for (int i = 0; i < SharedConst.QuestRewardReputationsCount; ++i)
            {
                RewardFactionId[i] = fields.Read<uint>(70 + i * 4);
                RewardFactionValue[i] = fields.Read<int>(71 + i * 4);
                RewardFactionOverride[i] = fields.Read<int>(72 + i * 4);
                RewardFactionCapIn[i] = fields.Read<int>(73 + i * 4);
            }

            RewardReputationMask = fields.Read<uint>(90);

            for (int i = 0; i < SharedConst.QuestRewardCurrencyCount; ++i)
            {
                RewardCurrencyId[i] = fields.Read<uint>(91 + i * 2);
                RewardCurrencyCount[i] = fields.Read<uint>(92 + i * 2);
            }

            SoundAccept = fields.Read<uint>(99);
            SoundTurnIn = fields.Read<uint>(100);
            AreaGroupID = fields.Read<uint>(101);
            LimitTime = fields.Read<uint>(102);
            AllowableRaces = new(fields.Read<ulong>(103));
            ResetByScheduler = fields.Read<bool>(104);
            Expansion = fields.Read<int>(105);
            ManagedWorldStateID = fields.Read<int>(106);
            QuestSessionBonus = fields.Read<int>(107);

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

        public void LoadRewardDisplaySpell(SQLFields fields)
        {
            uint spellId = fields.Read<uint>(1);
            uint playerConditionId = fields.Read<uint>(2);
            QuestCompleteSpellType type = (QuestCompleteSpellType)fields.Read<uint>(3);

            if (!Global.SpellMgr.HasSpellInfo(spellId, Difficulty.None))
            {
                Log.outError(LogFilter.Sql, $"Table `quest_reward_display_spell` has non-existing Spell ({spellId}) set for quest {Id}. Skipped.");
                return;
            }

            if (playerConditionId != 0 && !CliDB.PlayerConditionStorage.ContainsKey(playerConditionId))
            {
                if (!Global.ConditionMgr.HasConditionsForNotGroupedEntry(ConditionSourceType.PlayerCondition, playerConditionId))
                {
                    Log.outError(LogFilter.Sql, $"Table `quest_reward_display_spell` has serverside PlayerCondition ({playerConditionId}) set for quest {Id}. and spell {spellId} without conditions. Set to 0.");
                    playerConditionId = 0;
                }
            }

            if (type >= QuestCompleteSpellType.Max)
            {
                Log.outError(LogFilter.Sql, $"Table `quest_reward_display_spell` invalid type value ({type}) set for quest {Id} and spell {spellId}. Set to 0.");
                type = QuestCompleteSpellType.LegacyBehavior;
            }

            RewardDisplaySpell.Add(new QuestRewardDisplaySpell(spellId, playerConditionId, type));
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
                short emoteId = fields.Read<short>(1 + i);
                if (emoteId < 0 || !CliDB.EmotesStorage.ContainsKey(emoteId))
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
            BreadcrumbForQuestId = fields.Read<int>(7);
            RewardMailTemplateId = fields.Read<uint>(8);
            RewardMailDelay = fields.Read<uint>(9);
            RequiredSkillId = fields.Read<ushort>(10);
            RequiredSkillPoints = fields.Read<ushort>(11);
            RequiredMinRepFaction = fields.Read<ushort>(12);
            RequiredMaxRepFaction = fields.Read<ushort>(13);
            RequiredMinRepValue = fields.Read<int>(14);
            RequiredMaxRepValue = fields.Read<int>(15);
            SourceItemIdCount = fields.Read<byte>(16);
            SpecialFlags = (QuestSpecialFlags)fields.Read<byte>(17);
            ScriptId = Global.ObjectMgr.GetScriptId(fields.Read<string>(18));

            if (SpecialFlags.HasAnyFlag(QuestSpecialFlags.AutoAccept))
                Flags |= QuestFlags.AutoAccept;
        }

        public void LoadQuestMailSender(SQLFields fields)
        {
            RewardMailSenderEntry = fields.Read<uint>(1);
        }

        public void LoadQuestObjective(SQLFields fields)
        {
            QuestObjective obj = new();
            obj.QuestID = fields.Read<uint>(0);
            obj.Id = fields.Read<uint>(1);
            obj.Type = (QuestObjectiveType)fields.Read<byte>(2);
            obj.StorageIndex = fields.Read<sbyte>(3);
            obj.ObjectID = fields.Read<int>(4);
            obj.Amount = fields.Read<int>(5);
            obj.Flags = (QuestObjectiveFlags)fields.Read<uint>(6);
            obj.Flags2 = (QuestObjectiveFlags2)fields.Read<uint>(7);
            obj.ProgressBarWeight = fields.Read<float>(8);
            obj.Description = fields.Read<string>(9);

            bool hasCompletionEffect = false;
            for (var i = 10; i < 15; ++i)
            {
                if (!fields.IsNull(i))
                {
                    hasCompletionEffect = true;
                    break;
                }
            }

            if (hasCompletionEffect)
            {
                obj.CompletionEffect = new QuestObjectiveAction();
                if (!fields.IsNull(10))
                    obj.CompletionEffect.GameEventId = fields.Read<uint>(10);
                if (!fields.IsNull(11))
                    obj.CompletionEffect.SpellId = fields.Read<uint>(11);
                if (!fields.IsNull(12))
                    obj.CompletionEffect.ConversationId = fields.Read<uint>(12);
                if (!fields.IsNull(13))
                    obj.CompletionEffect.UpdatePhaseShift = fields.Read<bool>(13);
                if (!fields.IsNull(14))
                    obj.CompletionEffect.UpdateZoneAuras = fields.Read<bool>(14);
            }

            Objectives.Add(obj);
            _usedQuestObjectiveTypes[(int)obj.Type] = true;
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

        public void LoadConditionalConditionalQuestDescription(SQLFields fields)
        {
            Locale locale = fields.Read<string>(4).ToEnum<Locale>();
            if (!WorldConfig.GetBoolValue(WorldCfg.LoadLocales) && locale != SharedConst.DefaultLocale)
                return;

            if (locale >= Locale.Total)
            {
                Log.outError(LogFilter.Sql, $"Table `quest_description_conditional` has invalid locale {fields.Read<string>(4)} set for quest {fields.Read<uint>(0)}. Skipped.");
                return;
            }

            QuestConditionalText text = ConditionalQuestDescription.Find(text => text.PlayerConditionId == fields.Read<int>(1) && text.QuestgiverCreatureId == fields.Read<int>(2));
            if (text == null)
            {
                text = new();
                ConditionalQuestDescription.Add(text);
            }

            text.PlayerConditionId = fields.Read<int>(1);
            text.QuestgiverCreatureId = fields.Read<int>(2);
            ObjectManager.AddLocaleString(fields.Read<string>(3), locale, text.Text);
        }

        public void LoadConditionalConditionalRequestItemsText(SQLFields fields)
        {
            Locale locale = fields.Read<string>(4).ToEnum<Locale>();
            if (!WorldConfig.GetBoolValue(WorldCfg.LoadLocales) && locale != SharedConst.DefaultLocale)
                return;

            if (locale >= Locale.Total)
            {
                Log.outError(LogFilter.Sql, $"Table `quest_request_items_conditional` has invalid locale {fields.Read<string>(4)} set for quest {fields.Read<uint>(0)}. Skipped.");
                return;
            }

            QuestConditionalText text = ConditionalRequestItemsText.Find(text => text.PlayerConditionId == fields.Read<int>(1) && text.QuestgiverCreatureId == fields.Read<uint>(2));

            if (text == null)
            {
                text = new();
                ConditionalRequestItemsText.Add(text);
            }

            text.PlayerConditionId = fields.Read<int>(1);
            text.QuestgiverCreatureId = fields.Read<int>(2);
            ObjectManager.AddLocaleString(fields.Read<string>(3), locale, text.Text);
        }

        public void LoadConditionalConditionalOfferRewardText(SQLFields fields)
        {
            Locale locale = fields.Read<string>(4).ToEnum<Locale>();
            if (!WorldConfig.GetBoolValue(WorldCfg.LoadLocales) && locale != SharedConst.DefaultLocale)
                return;

            if (locale >= Locale.Total)
            {
                Log.outError(LogFilter.Sql, $"Table `quest_offer_reward_conditional` has invalid locale {fields.Read<string>(4)} set for quest {fields.Read<uint>(0)}. Skipped.");
                return;
            }

            QuestConditionalText text = ConditionalOfferRewardText.Find(text => text.PlayerConditionId == fields.Read<int>(1) && text.QuestgiverCreatureId == fields.Read<uint>(2));

            if (text == null)
            {
                text = new();
                ConditionalOfferRewardText.Add(text);
            }

            text.PlayerConditionId = fields.Read<int>(1);
            text.QuestgiverCreatureId = fields.Read<int>(2);
            ObjectManager.AddLocaleString(fields.Read<string>(3), locale, text.Text);
        }

        public void LoadConditionalConditionalQuestCompletionLog(SQLFields fields)
        {
            Locale locale = fields.Read<string>(4).ToEnum<Locale>();
            if (!WorldConfig.GetBoolValue(WorldCfg.LoadLocales) && locale != SharedConst.DefaultLocale)
                return;

            if (locale >= Locale.Total)
            {
                Log.outError(LogFilter.Sql, $"Table `quest_completion_log_conditional` has invalid locale {fields.Read<string>(4)} set for quest {fields.Read<uint>(0)}. Skipped.");
                return;
            }

            QuestConditionalText text = ConditionalQuestCompletionLog.Find(text => text.PlayerConditionId == fields.Read<int>(1) && text.QuestgiverCreatureId == fields.Read<uint>(2));

            if (text == null)
            {
                text = new();
                ConditionalQuestCompletionLog.Add(text);
            }

            text.PlayerConditionId = fields.Read<int>(1);
            text.QuestgiverCreatureId = fields.Read<int>(2);
            ObjectManager.AddLocaleString(fields.Read<string>(3), locale, text.Text);
        }

        public void LoadTreasurePickers(SQLFields fields)
        {
            TreasurePickerID.Add(fields.Read<int>(1));
        }

        public uint XPValue(Player player)
        {
            return XPValue(player, ContentTuningId, RewardXPDifficulty, RewardXPMultiplier, Expansion);
        }

        public static uint XPValue(Player player, uint contentTuningId, uint xpDifficulty, float xpMultiplier = 1.0f, int expansion = -1)
        {
            if (player != null)
            {
                uint questLevel = (uint)player.GetQuestLevel(contentTuningId);
                QuestXPRecord questXp = CliDB.QuestXPStorage.LookupByKey(questLevel);
                if (questXp == null || xpDifficulty >= 10)
                    return 0;

                uint xp = questXp.Difficulty[xpDifficulty];
                var contentTuning = CliDB.ContentTuningStorage.LookupByKey(contentTuningId);
                if (contentTuning != null)
                    xp = (uint)(xp * contentTuning.QuestXpMultiplier);

                int diffFactor = (int)(2 * (questLevel - player.GetLevel()) + 12);
                if (diffFactor < 1)
                    diffFactor = 1;
                else if (diffFactor > 10)
                    diffFactor = 10;

                xp = (uint)(diffFactor * xp * xpMultiplier / 10);
                if (player.GetLevel() >= Global.ObjectMgr.GetMaxLevelForExpansion(PlayerConst.CurrentExpansion - 1) && player.GetSession().GetExpansion() == PlayerConst.CurrentExpansion && expansion >= 0 && expansion < (int)PlayerConst.CurrentExpansion)
                    xp = (uint)(xp / 9.0f);

                xp = RoundXPValue(xp);

                if (WorldConfig.GetUIntValue(WorldCfg.MinQuestScaledXpRatio) != 0)
                {
                    uint minScaledXP = RoundXPValue((uint)(questXp.Difficulty[xpDifficulty] * xpMultiplier)) * WorldConfig.GetUIntValue(WorldCfg.MinQuestScaledXpRatio) / 100;
                    xp = Math.Max(minScaledXP, xp);
                }

                return xp;
            }

            return 0;
        }

        public static bool IsTakingQuestEnabled(uint questId)
        {
            if (!Global.QuestPoolMgr.IsQuestActive(questId))
                return false;

            return true;
        }

        public uint MoneyValue(Player player)
        {
            QuestMoneyRewardRecord money = CliDB.QuestMoneyRewardStorage.LookupByKey(player.GetQuestLevel(this));
            if (money != null)
                return (uint)(money.Difficulty[RewardMoneyDifficulty] * RewardMoneyMultiplier);
            else
                return 0;
        }

        public uint MaxMoneyValue()
        {
            uint value = 0;
            var questLevels = Global.DB2Mgr.GetContentTuningData(ContentTuningId, 0);
            if (questLevels.HasValue)
            {
                var money = CliDB.QuestMoneyRewardStorage.LookupByKey(questLevels.Value.MaxLevel);
                if (money != null)
                    value = (uint)(money.Difficulty[RewardMoneyDifficulty] * RewardMoneyMultiplier);
            }

            return value;
        }

        public uint GetMaxMoneyReward()
        {
            return (uint)(MaxMoneyValue() * WorldConfig.GetFloatValue(WorldCfg.RateMoneyQuest));
        }

        public QuestTagType? GetQuestTag()
        {
            QuestInfoRecord questInfo = CliDB.QuestInfoStorage.LookupByKey(QuestInfoID);
            if (questInfo != null)
                return (QuestTagType)questInfo.Type;

            return null;
        }

        public bool IsImportant()
        {
            var questInfo = CliDB.QuestInfoStorage.LookupByKey(QuestInfoID);
            if (questInfo != null)
                return (questInfo.Modifiers & 0x400) != 0;

            return false;
        }

        public bool IsMeta()
        {
            var questInfo = CliDB.QuestInfoStorage.LookupByKey(QuestInfoID);
            if (questInfo != null)
                return (questInfo.Modifiers & 0x800) != 0;

            return false;
        }

        public bool IsResetByScheduler() { return ResetByScheduler; }

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
                if (!ConditionManager.IsPlayerMeetingCondition(player, displaySpell.PlayerConditionId))
                    continue;

                rewards.SpellCompletionDisplayID[displaySpellIndex] = (int)displaySpell.SpellId;
                if (++displaySpellIndex >= rewards.SpellCompletionDisplayID.Length)
                    break;
            }

            rewards.SpellCompletionID = RewardSpell;
            rewards.SkillLineID = RewardSkillId;
            rewards.NumSkillUps = RewardSkillPoints;
            rewards.TreasurePickerID = TreasurePickerID;

            for (int i = 0; i < SharedConst.QuestRewardChoicesCount; ++i)
            {
                rewards.ChoiceItems[i].LootItemType = RewardChoiceItemType[i];
                rewards.ChoiceItems[i].Item = new ItemInstance();
                rewards.ChoiceItems[i].Item.ItemID = RewardChoiceItemId[i];
                rewards.ChoiceItems[i].Quantity = RewardChoiceItemCount[i];
            }

            for (int i = 0; i < SharedConst.QuestRewardItemCount; ++i)
            {
                rewards.Items[i].ItemID = RewardItemId[i];
                rewards.Items[i].ItemQty = RewardItemCount[i];
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
                rewards.Currencies[i].CurrencyID = RewardCurrencyId[i];
                rewards.Currencies[i].CurrencyQty = RewardCurrencyCount[i];
            }
        }

        public uint GetRewMoneyMaxLevel()
        {
            // If Quest has flag to not give money on max level, it's 0
            if (HasFlag(QuestFlags.NoMoneyForXp))
                return 0;

            // Else, return the rewarded copper sum modified by the rate
            return (uint)(RewardBonusMoney * WorldConfig.GetFloatValue(WorldCfg.RateMoneyMaxLevelQuest));
        }

        public bool IsAutoAccept()
        {
            return !WorldConfig.GetBoolValue(WorldCfg.QuestIgnoreAutoAccept) && HasFlag(QuestFlags.AutoAccept);
        }

        public bool IsTurnIn()
        {
            return !WorldConfig.GetBoolValue(WorldCfg.QuestIgnoreAutoComplete) && Type == QuestType.TurnIn;
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

            if (Flags.HasAnyFlag(QuestFlags.RaidGroupOk))
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
            for (var loc = Locale.enUS; loc < Locale.Total; ++loc)
            {
                if (!WorldConfig.GetBoolValue(WorldCfg.LoadLocales) && loc != SharedConst.DefaultLocale)
                    continue;

                response[(int)loc] = BuildQueryData(loc, null);
            }
        }

        public QueryQuestInfoResponse BuildQueryData(Locale loc, Player player)
        {
            QueryQuestInfoResponse response = new();

            response.Allow = true;
            response.QuestID = Id;

            response.Info.LogTitle = LogTitle;
            response.Info.LogDescription = LogDescription;
            response.Info.QuestDescription = QuestDescription;
            response.Info.AreaDescription = AreaDescription;
            response.Info.QuestCompletionLog = QuestCompletionLog;
            response.Info.PortraitGiverText = PortraitGiverText;
            response.Info.PortraitGiverName = PortraitGiverName;
            response.Info.PortraitTurnInText = PortraitTurnInText;
            response.Info.PortraitTurnInName = PortraitTurnInName;

            response.Info.ConditionalQuestDescription = ConditionalQuestDescription.Select(text =>
            {
                string content = text.Text[(int)Locale.enUS];
                ObjectManager.GetLocaleString(text.Text, loc, ref content);
                return new ConditionalQuestText(text.PlayerConditionId, text.QuestgiverCreatureId, content);
            }).ToList();

            response.Info.ConditionalQuestCompletionLog = ConditionalQuestCompletionLog.Select(text =>
            {
                string content = text.Text[(int)Locale.enUS];
                ObjectManager.GetLocaleString(text.Text, loc, ref content);
                return new ConditionalQuestText(text.PlayerConditionId, text.QuestgiverCreatureId, content);
            }).ToList();

            if (loc != Locale.enUS)
            {
                var questTemplateLocale = Global.ObjectMgr.GetQuestLocale(Id);
                if (questTemplateLocale != null)
                {
                    ObjectManager.GetLocaleString(questTemplateLocale.LogTitle, loc, ref response.Info.LogTitle);
                    ObjectManager.GetLocaleString(questTemplateLocale.LogDescription, loc, ref response.Info.LogDescription);
                    ObjectManager.GetLocaleString(questTemplateLocale.QuestDescription, loc, ref response.Info.QuestDescription);
                    ObjectManager.GetLocaleString(questTemplateLocale.AreaDescription, loc, ref response.Info.AreaDescription);
                    ObjectManager.GetLocaleString(questTemplateLocale.QuestCompletionLog, loc, ref response.Info.QuestCompletionLog);
                    ObjectManager.GetLocaleString(questTemplateLocale.PortraitGiverText, loc, ref response.Info.PortraitGiverText);
                    ObjectManager.GetLocaleString(questTemplateLocale.PortraitGiverName, loc, ref response.Info.PortraitGiverName);
                    ObjectManager.GetLocaleString(questTemplateLocale.PortraitTurnInText, loc, ref response.Info.PortraitTurnInText);
                    ObjectManager.GetLocaleString(questTemplateLocale.PortraitTurnInName, loc, ref response.Info.PortraitTurnInName);
                }
            }

            response.Info.QuestID = Id;
            response.Info.QuestType = (int)Type;
            response.Info.ContentTuningID = ContentTuningId;
            response.Info.QuestPackageID = PackageID;
            response.Info.QuestSortID = QuestSortID;
            response.Info.QuestInfoID = QuestInfoID;
            response.Info.SuggestedGroupNum = SuggestedPlayers;
            response.Info.RewardNextQuest = NextQuestInChain;
            response.Info.RewardXPDifficulty = RewardXPDifficulty;
            response.Info.RewardXPMultiplier = RewardXPMultiplier;

            if (!HasFlag(QuestFlags.HideReward))
                response.Info.RewardMoney = (int)(player != null ? player.GetQuestMoneyReward(this) : GetMaxMoneyReward());

            response.Info.RewardMoneyDifficulty = RewardMoneyDifficulty;
            response.Info.RewardMoneyMultiplier = RewardMoneyMultiplier;
            response.Info.RewardBonusMoney = RewardBonusMoney;
            foreach (QuestRewardDisplaySpell displaySpell in RewardDisplaySpell)
            {
                QuestCompleteDisplaySpell rewardDisplaySpell = new();
                rewardDisplaySpell.SpellID = displaySpell.SpellId;
                rewardDisplaySpell.PlayerConditionID = displaySpell.PlayerConditionId;
                rewardDisplaySpell.Type = (int)displaySpell.Type;
                response.Info.RewardDisplaySpell.Add(rewardDisplaySpell);
            }

            response.Info.RewardSpell = RewardSpell;

            response.Info.RewardHonor = RewardHonor;
            response.Info.RewardKillHonor = RewardKillHonor;

            response.Info.RewardArtifactXPDifficulty = (int)RewardArtifactXPDifficulty;
            response.Info.RewardArtifactXPMultiplier = RewardArtifactXPMultiplier;
            response.Info.RewardArtifactCategoryID = (int)RewardArtifactCategoryID;

            response.Info.StartItem = SourceItemId;
            response.Info.Flags = (uint)Flags;
            response.Info.FlagsEx = (uint)FlagsEx;
            response.Info.FlagsEx2 = (uint)FlagsEx2;
            response.Info.FlagsEx3 = FlagsEx3;
            response.Info.RewardTitle = RewardTitleId;
            response.Info.RewardArenaPoints = RewardArenaPoints;
            response.Info.RewardSkillLineID = RewardSkillId;
            response.Info.RewardNumSkillUps = RewardSkillPoints;
            response.Info.RewardFactionFlags = RewardReputationMask;
            response.Info.PortraitGiver = QuestGiverPortrait;
            response.Info.PortraitGiverMount = QuestGiverPortraitMount;
            response.Info.PortraitGiverModelSceneID = QuestGiverPortraitModelSceneId;
            response.Info.PortraitTurnIn = QuestTurnInPortrait;

            for (byte i = 0; i < SharedConst.QuestItemDropCount; ++i)
            {
                response.Info.ItemDrop[i] = (int)ItemDrop[i];
                response.Info.ItemDropQuantity[i] = (int)ItemDropQuantity[i];
            }

            if (!HasFlag(QuestFlags.HideReward))
            {
                for (byte i = 0; i < SharedConst.QuestRewardItemCount; ++i)
                {
                    response.Info.RewardItems[i] = RewardItemId[i];
                    response.Info.RewardAmount[i] = RewardItemCount[i];
                }
                for (byte i = 0; i < SharedConst.QuestRewardChoicesCount; ++i)
                {
                    response.Info.UnfilteredChoiceItems[i].ItemID = RewardChoiceItemId[i];
                    response.Info.UnfilteredChoiceItems[i].Quantity = RewardChoiceItemCount[i];
                }
            }

            for (byte i = 0; i < SharedConst.QuestRewardReputationsCount; ++i)
            {
                response.Info.RewardFactionID[i] = RewardFactionId[i];
                response.Info.RewardFactionValue[i] = RewardFactionValue[i];
                response.Info.RewardFactionOverride[i] = RewardFactionOverride[i];
                response.Info.RewardFactionCapIn[i] = RewardFactionCapIn[i];
            }

            response.Info.POIContinent = POIContinent;
            response.Info.POIx = POIx;
            response.Info.POIy = POIy;
            response.Info.POIPriority = POIPriority;

            response.Info.AllowableRaces = AllowableRaces;
            response.Info.TreasurePickerID = TreasurePickerID;
            response.Info.Expansion = Expansion;
            response.Info.ManagedWorldStateID = ManagedWorldStateID;
            response.Info.QuestSessionBonus = 0; //GetQuestSessionBonus(); // this is only sent while quest session is active
            response.Info.QuestGiverCreatureID = 0; // only sent during npc interaction

            foreach (QuestObjective questObjective in Objectives)
            {
                response.Info.Objectives.Add(questObjective);

                if (loc != Locale.enUS)
                {
                    var questObjectivesLocale = Global.ObjectMgr.GetQuestObjectivesLocale(questObjective.Id);
                    if (questObjectivesLocale != null)
                        ObjectManager.GetLocaleString(questObjectivesLocale.Description, loc, ref response.Info.Objectives.Last().Description);
                }
            }

            for (int i = 0; i < SharedConst.QuestRewardCurrencyCount; ++i)
            {
                response.Info.RewardCurrencyID[i] = RewardCurrencyId[i];
                response.Info.RewardCurrencyQty[i] = RewardCurrencyCount[i];
            }

            response.Info.AcceptedSoundKitID = SoundAccept;
            response.Info.CompleteSoundKitID = SoundTurnIn;
            response.Info.AreaGroupID = AreaGroupID;
            response.Info.TimeAllowed = LimitTime;
            response.Info.ResetByScheduler = IsResetByScheduler();

            response.Write();
            return response;
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

        public bool HasQuestObjectiveType(QuestObjectiveType type) { return _usedQuestObjectiveTypes[(int)type]; }

        public bool IsAutoPush() { return HasFlagEx(QuestFlagsEx.AutoPush); }
        public bool IsWorldQuest() { return HasFlagEx(QuestFlagsEx.IsWorldQuest); }

        // Possibly deprecated flag
        public bool IsUnavailable() { return HasFlag(QuestFlags.Deprecated); }

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
        public bool IsPushedToPartyOnAccept() { return HasSpecialFlag(QuestSpecialFlags.AutoPushToParty); }

        public uint GetRewChoiceItemsCount() { return _rewChoiceItemsCount; }
        public uint GetRewItemsCount() { return _rewItemsCount; }

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
        public uint RewardMoneyDifficulty;
        public float RewardMoneyMultiplier;
        public uint RewardBonusMoney;
        public List<QuestRewardDisplaySpell> RewardDisplaySpell = new();
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
        public uint FlagsEx3;
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
        public int QuestGiverPortraitModelSceneId;
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
        public long LimitTime;
        public RaceMask<ulong> AllowableRaces { get; set; }
        public List<int> TreasurePickerID = new();
        public int Expansion;
        public int ManagedWorldStateID;
        public int QuestSessionBonus;
        public List<QuestObjective> Objectives = new();
        public string LogTitle = "";
        public string LogDescription = "";
        public string QuestDescription = "";
        public string AreaDescription = "";
        public string PortraitGiverText = "";
        public string PortraitGiverName = "";
        public string PortraitTurnInText = "";
        public string PortraitTurnInName = "";
        public string QuestCompletionLog = "";
        public bool ResetByScheduler;

        // quest_description_conditional
        public List<QuestConditionalText> ConditionalQuestDescription = new();

        // quest_completion_log_conditional
        public List<QuestConditionalText> ConditionalQuestCompletionLog = new();

        // quest_detais table
        public uint[] DetailsEmote = new uint[SharedConst.QuestEmoteCount];
        public uint[] DetailsEmoteDelay = new uint[SharedConst.QuestEmoteCount];

        // quest_request_items table
        public uint EmoteOnComplete;
        public uint EmoteOnIncomplete;
        public uint EmoteOnCompleteDelay;
        public uint EmoteOnIncompleteDelay;
        public string RequestItemsText = "";

        // quest_request_items_conditional
        public List<QuestConditionalText> ConditionalRequestItemsText = new();

        // quest_offer_reward table
        public int[] OfferRewardEmote = new int[SharedConst.QuestEmoteCount];
        public uint[] OfferRewardEmoteDelay = new uint[SharedConst.QuestEmoteCount];
        public string OfferRewardText = "";

        // quest_offer_reward_conditional
        public List<QuestConditionalText> ConditionalOfferRewardText = new();

        // quest_template_addon table (custom data)
        public uint MaxLevel { get; set; }
        public uint AllowableClasses { get; set; }
        public uint SourceSpellID { get; set; }
        public int PrevQuestId { get; set; }
        public uint NextQuestId { get; set; }
        public int ExclusiveGroup { get; set; }
        public int BreadcrumbForQuestId { get; set; }
        public uint RewardMailTemplateId { get; set; }
        public uint RewardMailDelay { get; set; }
        public uint RequiredSkillId { get; set; }
        public uint RequiredSkillPoints { get; set; }
        public uint RequiredMinRepFaction { get; set; }
        public int RequiredMinRepValue { get; set; }
        public uint RequiredMaxRepFaction { get; set; }
        public int RequiredMaxRepValue { get; set; }
        public uint SourceItemIdCount { get; set; }
        public uint RewardMailSenderEntry { get; set; }
        public QuestSpecialFlags SpecialFlags { get; set; } // custom flags, not sniffed/WDB
        public BitArray _usedQuestObjectiveTypes = new((int)QuestObjectiveType.Max);
        public uint ScriptId { get; set; }

        public List<uint> DependentPreviousQuests = new();
        public List<uint> DependentBreadcrumbQuests = new();
        public QueryQuestInfoResponse[] response = new QueryQuestInfoResponse[(int)Locale.Total];

        uint _rewChoiceItemsCount;
        uint _rewItemsCount;
        ushort _eventIdForQuest;
        #endregion
    }

    public class QuestStatusData
    {
        public ushort Slot = SharedConst.MaxQuestLogSize;
        public QuestStatus Status;
        public long AcceptTime;
        public uint Timer;
        public bool Explored;
        public List<(sbyte, uint)> SpawnTrackingList = new();
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
        public StringArray Greeting = new((int)Locale.Total);
    }

    public class QuestTemplateLocale
    {
        public StringArray LogTitle = new((int)Locale.Total);
        public StringArray LogDescription = new((int)Locale.Total);
        public StringArray QuestDescription = new((int)Locale.Total);
        public StringArray AreaDescription = new((int)Locale.Total);
        public StringArray PortraitGiverText = new((int)Locale.Total);
        public StringArray PortraitGiverName = new((int)Locale.Total);
        public StringArray PortraitTurnInText = new((int)Locale.Total);
        public StringArray PortraitTurnInName = new((int)Locale.Total);
        public StringArray QuestCompletionLog = new((int)Locale.Total);
    }

    public class QuestRequestItemsLocale
    {
        public StringArray CompletionText = new((int)Locale.Total);
    }

    public class QuestObjectivesLocale
    {
        public StringArray Description = new((int)Locale.Total);
    }

    public class QuestOfferRewardLocale
    {
        public StringArray RewardText = new((int)Locale.Total);
    }

    public struct QuestRewardDisplaySpell
    {
        public uint SpellId;
        public uint PlayerConditionId;
        public QuestCompleteSpellType Type;

        public QuestRewardDisplaySpell(uint spellId, uint playerConditionId, QuestCompleteSpellType type)
        {
            SpellId = spellId;
            PlayerConditionId = playerConditionId;
            Type = type;
        }
    }

    public class QuestConditionalText
    {
        public int PlayerConditionId;
        public int QuestgiverCreatureId;
        public StringArray Text = new((int)Locale.Total);
    }

    public class QuestObjectiveAction
    {
        public uint? GameEventId;
        public uint? SpellId;
        public uint? ConversationId;
        public bool UpdatePhaseShift;
        public bool UpdateZoneAuras;
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
        public QuestObjectiveFlags2 Flags2;
        public float ProgressBarWeight;
        public string Description;
        public int[] VisualEffects = Array.Empty<int>();
        public QuestObjectiveAction CompletionEffect;

        public bool IsStoringValue()
        {
            switch (Type)
            {
                case QuestObjectiveType.Monster:
                case QuestObjectiveType.Item:
                case QuestObjectiveType.GameObject:
                case QuestObjectiveType.TalkTo:
                case QuestObjectiveType.PlayerKills:
                case QuestObjectiveType.WinPvpPetBattles:
                case QuestObjectiveType.HaveCurrency:
                case QuestObjectiveType.ObtainCurrency:
                case QuestObjectiveType.IncreaseReputation:
                case QuestObjectiveType.KillWithLabel:
                    return true;
                default:
                    break;
            }
            return false;
        }

        public bool IsStoringFlag()
        {
            switch (Type)
            {
                case QuestObjectiveType.AreaTrigger:
                case QuestObjectiveType.WinPetBattleAgainstNpc:
                case QuestObjectiveType.DefeatBattlePet:
                case QuestObjectiveType.CriteriaTree:
                case QuestObjectiveType.AreaTriggerEnter:
                case QuestObjectiveType.AreaTriggerExit:
                    return true;
                default:
                    break;
            }
            return false;
        }

        public static bool CanAlwaysBeProgressedInRaid(QuestObjectiveType type)
        {
            switch (type)
            {
                case QuestObjectiveType.Item:
                case QuestObjectiveType.Currency:
                case QuestObjectiveType.LearnSpell:
                case QuestObjectiveType.MinReputation:
                case QuestObjectiveType.MaxReputation:
                case QuestObjectiveType.Money:
                case QuestObjectiveType.HaveCurrency:
                case QuestObjectiveType.IncreaseReputation:
                    return true;
                default:
                    break;
            }
            return false;
        }
    }
}
