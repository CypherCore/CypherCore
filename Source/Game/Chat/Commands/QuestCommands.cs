// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Game.Chat
{
    [CommandGroup("quest")]
    class QuestCommands
    {
        [Command("add", RBACPermissions.CommandQuestAdd)]
        static bool HandleQuestAdd(CommandHandler handler, Quest quest)
        {
            Player player = handler.GetSelectedPlayer();
            if (player == null)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            if (Global.DisableMgr.IsDisabledFor(DisableType.Quest, quest.Id, null))
            {
                handler.SendSysMessage(CypherStrings.CommandQuestNotfound, quest.Id);
                return false;
            }

            // check item starting quest (it can work incorrectly if added without item in inventory)
            var itc = Global.ObjectMgr.GetItemTemplates();
            var result = itc.Values.FirstOrDefault(p => p.GetStartQuest() == quest.Id);

            if (result != null)
            {
                handler.SendSysMessage(CypherStrings.CommandQuestStartfromitem, quest.Id, result.GetId());
                return false;
            }

            if (player.IsActiveQuest(quest.Id))
                return false;

            // ok, normal (creature/GO starting) quest
            if (player.CanAddQuest(quest, true))
                player.AddQuestAndCheckCompletion(quest, null);

            return true;
        }

        [Command("complete", RBACPermissions.CommandQuestComplete)]
        static bool HandleQuestComplete(CommandHandler handler, Quest quest)
        {
            Player player = handler.GetSelectedPlayer();
            if (player == null)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            // If player doesn't have the quest
            if ((player.GetQuestStatus(quest.Id) == QuestStatus.None && !quest.HasFlag(QuestFlags.TrackingEvent)) || Global.DisableMgr.IsDisabledFor(DisableType.Quest, quest.Id, null))
            {
                handler.SendSysMessage(CypherStrings.CommandQuestNotfound, quest.Id);
                return false;
            }

            foreach (var obj in quest.Objectives)
                CompleteObjective(player, obj);

            player.CompleteQuest(quest.Id);
            return true;
        }

        [Command("remove", RBACPermissions.CommandQuestRemove)]
        static bool HandleQuestRemove(CommandHandler handler, Quest quest)
        {
            Player player = handler.GetSelectedPlayer();
            if (player == null)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            QuestStatus oldStatus = player.GetQuestStatus(quest.Id);

            if (oldStatus != QuestStatus.None)
            {
                // remove all quest entries for 'entry' from quest log
                for (byte slot = 0; slot < SharedConst.MaxQuestLogSize; ++slot)
                {
                    uint logQuest = player.GetQuestSlotQuestId(slot);
                    if (logQuest == quest.Id)
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

                player.RemoveActiveQuest(quest.Id, false);
                player.RemoveRewardedQuest(quest.Id);
                player.DespawnPersonalSummonsForQuest(quest.Id);

                Global.ScriptMgr.OnQuestStatusChange(player, quest.Id);
                Global.ScriptMgr.OnQuestStatusChange(player, quest, oldStatus, QuestStatus.None);

                handler.SendSysMessage(CypherStrings.CommandQuestRemoved);
                return true;
            }
            else
            {
                handler.SendSysMessage(CypherStrings.CommandQuestNotfound, quest.Id);
                return false;
            }
        }

        [Command("reward", RBACPermissions.CommandQuestReward)]
        static bool HandleQuestReward(CommandHandler handler, Quest quest)
        {
            Player player = handler.GetSelectedPlayer();
            if (player == null)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            // If player doesn't have the quest
            if (player.GetQuestStatus(quest.Id) != QuestStatus.Complete || Global.DisableMgr.IsDisabledFor(DisableType.Quest, quest.Id, null))
            {
                handler.SendSysMessage(CypherStrings.CommandQuestNotfound, quest.Id);
                return false;
            }

            player.RewardQuest(quest, LootItemType.Item, 0, player);
            return true;
        }

        [CommandGroup("objective")]
        class ObjectiveCommands
        {
            [Command("complete", RBACPermissions.CommandQuestObjectiveComplete)]
            static bool HandleQuestObjectiveComplete(CommandHandler handler, uint objectiveId)
            {
                Player player = handler.GetSelectedPlayerOrSelf();
                if (player == null)
                {
                    handler.SendSysMessage(CypherStrings.NoCharSelected);
                    return false;
                }

                QuestObjective obj = Global.ObjectMgr.GetQuestObjective(objectiveId);
                if (obj == null)
                {
                    handler.SendSysMessage(CypherStrings.QuestObjectiveNotfound);
                    return false;
                }

                CompleteObjective(player, obj);
                return true;
            }
        }

        static void CompleteObjective(Player player, QuestObjective obj)
        {
            switch (obj.Type)
            {
                case QuestObjectiveType.Item:
                {
                    uint curItemCount = player.GetItemCount((uint)obj.ObjectID, true);
                    List<ItemPosCount> dest = new();
                    var msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, (uint)obj.ObjectID, (uint)(obj.Amount - curItemCount));
                    if (msg == InventoryResult.Ok)
                    {
                        Item item = player.StoreNewItem(dest, (uint)obj.ObjectID, true);
                        player.SendNewItem(item, (uint)(obj.Amount - curItemCount), true, false);
                    }
                    break;
                }
                case QuestObjectiveType.Currency:
                {
                    player.ModifyCurrency((uint)obj.ObjectID, obj.Amount, CurrencyGainSource.Cheat);
                    break;
                }
                case QuestObjectiveType.MinReputation:
                {
                    int curRep = player.GetReputationMgr().GetReputation((uint)obj.ObjectID);
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
                    int curRep = player.GetReputationMgr().GetReputation((uint)obj.ObjectID);
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
                case QuestObjectiveType.ProgressBar:
                    // do nothing
                    break;
                default:
                    player.UpdateQuestObjectiveProgress(obj.Type, obj.ObjectID, obj.Amount);
                    break;
            }
        }
    }
}
