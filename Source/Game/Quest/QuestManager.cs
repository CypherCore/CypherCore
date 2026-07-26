// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    internal class QuestManager
    {
        static MultiMap<uint, QuestLineXQuestRecord> QuestsByQuestLine = [];

        public static void Load()
        {
            foreach (QuestLineXQuestRecord questLineQuest in CliDB.QuestLineXQuestStorage.Values)
                QuestsByQuestLine.Add(questLineQuest.QuestLineID, questLineQuest);

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
    }
}
