// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    internal class QuestManager
    {
        static MultiMap<uint, CampaignRecord> CampaignsByQuestLine = [];
        static MultiMap<uint, QuestLineData> QuestLineDataByQuest = [];
        static List<CampaignQuestLine> CampaignQuestLines = [];
        static MultiMap<uint, QuestLineXQuestRecord> QuestsByQuestLine = [];

        public static void Load()
        {
            foreach (CampaignXQuestLineRecord campaignQuestLine in CliDB.CampaignXQuestLineStorage.Values)
            {
                CampaignRecord campaign = CliDB.CampaignStorage.LookupByKey(campaignQuestLine.CampaignID);
                if (campaign != null)
                {
                    CampaignsByQuestLine.Add(campaignQuestLine.QuestLineID, campaign);
                    CampaignQuestLines.Add(new CampaignQuestLine() { CampaignId = campaignQuestLine.CampaignID, QuestLineId = campaignQuestLine.QuestLineID });
                }
            }

            foreach (QuestLineXQuestRecord questLineQuest in CliDB.QuestLineXQuestStorage.Values)
            {
                QuestsByQuestLine.Add(questLineQuest.QuestLineID, questLineQuest);
                QuestLineData questLineData = new()
                {
                    QuestLineQuest = questLineQuest,
                    Campaigns = CampaignsByQuestLine.LookupByKey(questLineQuest.QuestLineID)
                };
                QuestLineDataByQuest.Add(questLineQuest.QuestID, questLineData);
            }

            foreach (var key in QuestsByQuestLine.Keys)
                QuestsByQuestLine[key] = QuestsByQuestLine[key].OrderBy(p => p.OrderIndex).ToList();
        }

        public static List<QuestLineXQuestRecord> GetQuestsForQuestLine(uint questLineId)
        {
            return QuestsByQuestLine.LookupByKey(questLineId);
        }

        public static bool IsQuestLineQuestAvailableForPlayer(uint questLineId, Player player)
        {
            foreach (QuestLineXQuestRecord questLineQuest in GetQuestsForQuestLine(questLineId))
            {
                Quest quest = Global.ObjectMgr.GetQuestTemplate(questLineQuest.QuestID);
                if (quest != null && player.CanTakeQuest(quest, false))
                    return true;
            }

            return false;
        }

        public static bool IsQuestLineQuestActiveForPlayer(uint questLineId, Player player)
        {
            foreach (QuestLineXQuestRecord questLineQuest in GetQuestsForQuestLine(questLineId))
                if (player.IsActiveQuest(questLineQuest.QuestID))
                    return true;

            return false;
        }

        public static bool IsQuestLineCompletedByPlayer(uint questLineId, Player player)
        {
            foreach (QuestLineXQuestRecord questLineQuest in GetQuestsForQuestLine(questLineId))
            {
                if (questLineQuest.HasFlag(QuestLineXQuestFlags.IgnoreForCompletion))
                    continue;

                if (!player.IsQuestCompletedBitSet(questLineQuest.QuestID))
                    return false;
            }

            return true;
        }

        public static (uint Completed, uint Total) GetQuestLineStatsForPlayer(uint questLineId, Player player)
        {
            uint completed = 0;
            uint total = 0;
            foreach (QuestLineXQuestRecord questLineQuest in GetQuestsForQuestLine(questLineId))
            {
                if (questLineQuest.HasFlag(QuestLineXQuestFlags.IgnoreForCompletion))
                    continue;

                completed += player.IsQuestCompletedBitSet(questLineQuest.QuestID) ? 1 : 0u;
                ++total;
            }

            return (completed, total);
        }

        public static void SkipQuestLineForPlayer(uint questLineId, Player player)
        {
            List<QuestLineXQuestRecord> questLineQuests = GetQuestsForQuestLine(questLineId);
            player.SkipQuests(questLineQuests.Select(p => p.QuestID));
        }

        static IEnumerable<CampaignQuestLine> GetQuestLinesForCampaign(uint campaignId)
        {
            return CampaignQuestLines.Where(p => p.CampaignId == campaignId);
        }

        public static bool IsCampaignCompletedByPlayer(uint campaignId, Player player)
        {
            var questLines = GetQuestLinesForCampaign(campaignId);
            if (questLines.Count() == 0)
                return false;

            foreach (CampaignQuestLine campaignQuestLine in questLines)
                if (!IsQuestLineCompletedByPlayer(campaignQuestLine.QuestLineId, player))
                    return false;

            // all questlines completed
            return true;
        }

        public static bool IsCampaignQuestStatusVisibleForPlayer(uint questId, Player player)
        {
            var QuestLineDataList = QuestLineDataByQuest.LookupByKey(questId);
            if (QuestLineDataList == null)
                return false;

            foreach (QuestLineData questLineData in QuestLineDataList)
            {
                if (questLineData.Campaigns == null)
                    continue;

                foreach (CampaignRecord campaign in questLineData.Campaigns)
                {
                    if (campaign.HasFlag(CampaignFlags.DontUseJourneyQuestBang))
                        continue;

                    if (!ConditionManager.IsPlayerMeetingCondition(player, (uint)campaign.Prerequisite))
                        continue;

                    if (!ConditionManager.IsPlayerMeetingCondition(player, (uint)campaign.Stalled))
                        continue;

                    if (campaign.Completed != 0 && ConditionManager.IsPlayerMeetingCondition(player, (uint)campaign.Completed))
                        continue;

                    if (!ConditionManager.IsPlayerMeetingCondition(player, (uint)campaign.OnlyStallIf))
                        continue;

                    return true;
                }
            }

            return false;
        }

        public static void SkipCampaignForPlayer(uint campaignId, Player player)
        {
            List<uint> questIds = [];

            foreach (CampaignQuestLine campaignQuestLine in GetQuestLinesForCampaign(campaignId))
            {
                List<QuestLineXQuestRecord> questLineQuests = GetQuestsForQuestLine(campaignQuestLine.QuestLineId);
                questIds.AddRange(questLineQuests.Select(p => p.QuestID));
            }

            player.SkipQuests(questIds);
        }
    }

    public struct QuestLineData
    {
        public QuestLineXQuestRecord QuestLineQuest;
        public List<CampaignRecord> Campaigns;
    }

    public struct CampaignQuestLine
    {
        public uint CampaignId;
        public uint QuestLineId;
    }
}
