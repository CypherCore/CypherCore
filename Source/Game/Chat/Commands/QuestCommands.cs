﻿/*
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

using Framework.Constants;
using Framework.IO;
using Game.DataStorage;
using Game.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Game.Chat
{
    [CommandGroup("quest", RBACPermissions.CommandQuest)]
    internal class QuestCommands
    {
        [Command("add", RBACPermissions.CommandQuestAdd)]
        private static bool Add(StringArguments args, CommandHandler handler)
        {
            var player = handler.GetSelectedPlayer();
            if (!player)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            // .addquest #entry'
            // number or [name] Shift-click form |color|Hquest:quest_id:quest_level:min_level:max_level:scaling_faction|h[name]|h|r
            var cId = handler.ExtractKeyFromLink(args, "Hquest");
            if (!uint.TryParse(cId, out var entry))
                return false;

            var quest = Global.ObjectMgr.GetQuestTemplate(entry);
            if (quest == null)
            {
                handler.SendSysMessage(CypherStrings.CommandQuestNotfound, entry);
                return false;
            }

            // check item starting quest (it can work incorrectly if added without item in inventory)
            var itc = Global.ObjectMgr.GetItemTemplates();
            var result = itc.Values.FirstOrDefault(p => p.GetStartQuest() == entry);

            if (result != null)
            {
                handler.SendSysMessage(CypherStrings.CommandQuestStartfromitem, entry, result.GetId());
                return false;
            }

            // ok, normal (creature/GO starting) quest
            if (player.CanAddQuest(quest, true))
                player.AddQuestAndCheckCompletion(quest, null);

            return true;
        }

        [Command("complete", RBACPermissions.CommandQuestComplete)]
        private static bool Complete(StringArguments args, CommandHandler handler)
        {
            var player = handler.GetSelectedPlayer();
            if (!player)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            // .quest complete #entry
            // number or [name] Shift-click form |color|Hquest:quest_id:quest_level:min_level:max_level:scaling_faction|h[name]|h|r
            var cId = handler.ExtractKeyFromLink(args, "Hquest");
            if (!uint.TryParse(cId, out var entry))
                return false;

            var quest = Global.ObjectMgr.GetQuestTemplate(entry);

            // If player doesn't have the quest
            if (quest == null || player.GetQuestStatus(entry) == QuestStatus.None)
            {
                handler.SendSysMessage(CypherStrings.CommandQuestNotfound, entry);
                return false;
            }

            for (var i = 0; i < quest.Objectives.Count; ++i)
            {
                var obj = quest.Objectives[i];

                switch (obj.Type)
                {
                    case QuestObjectiveType.Item:
                        {
                            var curItemCount = player.GetItemCount((uint)obj.ObjectID, true);
                            var dest = new List<ItemPosCount>();
                            var msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, (uint)obj.ObjectID, (uint)(obj.Amount - curItemCount));
                            if (msg == InventoryResult.Ok)
                            {
                                var item = player.StoreNewItem(dest, (uint)obj.ObjectID, true);
                                player.SendNewItem(item, (uint)(obj.Amount - curItemCount), true, false);
                            }
                            break;
                        }
                    case QuestObjectiveType.Monster:
                        {
                            var creatureInfo = Global.ObjectMgr.GetCreatureTemplate((uint)obj.ObjectID);
                            if (creatureInfo != null)
                                for (var z = 0; z < obj.Amount; ++z)
                                    player.KilledMonster(creatureInfo, ObjectGuid.Empty);
                            break;
                        }
                    case QuestObjectiveType.GameObject:
                        {
                            for (var z = 0; z < obj.Amount; ++z)
                                player.KillCreditGO((uint)obj.ObjectID);
                            break;
                        }
                    case QuestObjectiveType.MinReputation:
                        {
                            var curRep = player.GetReputationMgr().GetReputation((uint)obj.ObjectID);
                            if (curRep < obj.Amount)
                            {
                                var factionEntry = CliDB.FactionStorage.LookupByKey(obj.ObjectID);
                                if (factionEntry != null)
                                    player.GetReputationMgr().SetReputation(factionEntry, obj.Amount);
                            }
                            break;
                        }
                    case QuestObjectiveType.MaxReputation:
                        {
                            var curRep = player.GetReputationMgr().GetReputation((uint)obj.ObjectID);
                            if (curRep > obj.Amount)
                            {
                                var factionEntry = CliDB.FactionStorage.LookupByKey(obj.ObjectID);
                                if (factionEntry != null)
                                    player.GetReputationMgr().SetReputation(factionEntry, obj.Amount);
                            }
                            break;
                        }
                    case QuestObjectiveType.Money:
                        {
                            player.ModifyMoney(obj.Amount);
                            break;
                        }
                }

            }

            player.CompleteQuest(entry);
            return true;
        }

        [Command("remove", RBACPermissions.CommandQuestRemove)]
        private static bool Remove(StringArguments args, CommandHandler handler)
        {
            var player = handler.GetSelectedPlayer();
            if (!player)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            // .removequest #entry'
            // number or [name] Shift-click form |color|Hquest:quest_id:quest_level:min_level:max_level:scaling_faction|h[name]|h|r
            var cId = handler.ExtractKeyFromLink(args, "Hquest");
            if (!uint.TryParse(cId, out var entry))
                return false;

            var quest = Global.ObjectMgr.GetQuestTemplate(entry);
            if (quest == null)
            {
                handler.SendSysMessage(CypherStrings.CommandQuestNotfound, entry);
                return false;
            }

            var oldStatus = player.GetQuestStatus(entry);

            // remove all quest entries for 'entry' from quest log
            for (byte slot = 0; slot < SharedConst.MaxQuestLogSize; ++slot)
            {
                var logQuest = player.GetQuestSlotQuestId(slot);
                if (logQuest == entry)
                {
                    player.SetQuestSlot(slot, 0);

                    // we ignore unequippable quest items in this case, its' still be equipped
                    player.TakeQuestSourceItem(logQuest, false);

                    if (quest.HasFlag(QuestFlags.Pvp))
                    {
                        player.pvpInfo.IsHostile = player.pvpInfo.IsInHostileArea || player.HasPvPForcingQuest();
                        player.UpdatePvPState();
                    }
                }
            }

            player.RemoveActiveQuest(entry, false);
            player.RemoveRewardedQuest(entry);

            Global.ScriptMgr.OnQuestStatusChange(player, entry);
            Global.ScriptMgr.OnQuestStatusChange(player, quest, oldStatus, QuestStatus.None);

            handler.SendSysMessage(CypherStrings.CommandQuestRemoved);
            return true;
        }

        [Command("reward", RBACPermissions.CommandQuestReward)]
        private static bool Reward(StringArguments args, CommandHandler handler)
        {
            var player = handler.GetSelectedPlayer();
            if (!player)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            // .quest reward #entry
            // number or [name] Shift-click form |color|Hquest:quest_id:quest_level:min_level:max_level:scaling_faction|h[name]|h|r
            var cId = handler.ExtractKeyFromLink(args, "Hquest");
            if (!uint.TryParse(cId, out var entry))
                return false;

            var quest = Global.ObjectMgr.GetQuestTemplate(entry);

            // If player doesn't have the quest
            if (quest == null || player.GetQuestStatus(entry) != QuestStatus.Complete)
            {
                handler.SendSysMessage(CypherStrings.CommandQuestNotfound, entry);
                return false;
            }

            player.RewardQuest(quest, LootItemType.Item, 0, player);
            return true;
        }
    }
}
