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
using Game.BattleGrounds;
using Game.Entities;
using Game.Groups;
using Game.Networking;
using Game.Networking.Packets;
using Game.DataStorage;
using System.Collections.Generic;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.QuestGiverStatusQuery, Processing = PacketProcessing.Inplace)]
        void HandleQuestgiverStatusQuery(QuestGiverStatusQuery packet)
        {
            var questStatus = QuestGiverStatus.None;

            var questgiver = Global.ObjAccessor.GetObjectByTypeMask(GetPlayer(), packet.QuestGiverGUID, TypeMask.Unit | TypeMask.GameObject);
            if (!questgiver)
            {
                Log.outInfo(LogFilter.Network, "Error in CMSG_QUESTGIVER_STATUS_QUERY, called for non-existing questgiver {0}", packet.QuestGiverGUID.ToString());
                return;
            }

            switch (questgiver.GetTypeId())
            {
                case TypeId.Unit:
                    if (!questgiver.ToCreature().IsHostileTo(GetPlayer()))// do not show quest status to enemies
                        questStatus = GetPlayer().GetQuestDialogStatus(questgiver);
                    break;
                case TypeId.GameObject:
                    questStatus = GetPlayer().GetQuestDialogStatus(questgiver);
                    break;
                default:
                    Log.outError(LogFilter.Network, "QuestGiver called for unexpected type {0}", questgiver.GetTypeId());
                    break;
            }

            //inform client about status of quest
            GetPlayer().PlayerTalkClass.SendQuestGiverStatus(questStatus, packet.QuestGiverGUID);
        }

        [WorldPacketHandler(ClientOpcodes.QuestGiverHello)]
        void HandleQuestgiverHello(QuestGiverHello packet)
        {
            var creature = GetPlayer().GetNPCIfCanInteractWith(packet.QuestGiverGUID, NPCFlags.QuestGiver, NPCFlags2.None);
            if (creature == null)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleQuestgiverHello - {0} not found or you can't interact with him.", packet.QuestGiverGUID.ToString());
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            // Stop the npc if moving
            creature.PauseMovement(WorldConfig.GetUIntValue(WorldCfg.CreatureStopForPlayer));
            creature.SetHomePosition(creature.GetPosition());

            _player.PlayerTalkClass.ClearMenus();
            if (creature.GetAI().GossipHello(_player))
                return;

            GetPlayer().PrepareGossipMenu(creature, creature.GetCreatureTemplate().GossipMenuId, true);
            GetPlayer().SendPreparedGossip(creature);
        }

        [WorldPacketHandler(ClientOpcodes.QuestGiverAcceptQuest)]
        void HandleQuestgiverAcceptQuest(QuestGiverAcceptQuest packet)
        {
            WorldObject obj;
            if (!packet.QuestGiverGUID.IsPlayer())
                obj = Global.ObjAccessor.GetObjectByTypeMask(_player, packet.QuestGiverGUID, TypeMask.Unit | TypeMask.GameObject | TypeMask.Item);
            else
                obj = Global.ObjAccessor.FindPlayer(packet.QuestGiverGUID);

            var CLOSE_GOSSIP_CLEAR_SHARING_INFO = new System.Action(() =>
            {
                GetPlayer().PlayerTalkClass.SendCloseGossip();
                GetPlayer().ClearQuestSharingInfo();
            });

            // no or incorrect quest giver
            if (obj == null)
            {
                CLOSE_GOSSIP_CLEAR_SHARING_INFO();
                return;
            }

            var playerQuestObject = obj.ToPlayer();
            if (playerQuestObject)
            {
                if ((_player.GetPlayerSharingQuest().IsEmpty() && _player.GetPlayerSharingQuest() != packet.QuestGiverGUID) || !playerQuestObject.CanShareQuest(packet.QuestID))
                {
                    CLOSE_GOSSIP_CLEAR_SHARING_INFO();
                    return;
                }
                if (!_player.IsInSameRaidWith(playerQuestObject))
                {
                    CLOSE_GOSSIP_CLEAR_SHARING_INFO();
                    return;
                }
            }
            else
            {
                if (!obj.HasQuest(packet.QuestID))
                {
                    CLOSE_GOSSIP_CLEAR_SHARING_INFO();
                    return;
                }
            }

            // some kind of WPE protection
            if (!_player.CanInteractWithQuestGiver(obj))
            {
                CLOSE_GOSSIP_CLEAR_SHARING_INFO();
                return;
            }

            Quest quest = Global.ObjectMgr.GetQuestTemplate(packet.QuestID);
            if (quest != null)
            {
                // prevent cheating
                if (!GetPlayer().CanTakeQuest(quest, true))
                {
                    CLOSE_GOSSIP_CLEAR_SHARING_INFO();
                    return;
                }

                if (!_player.GetPlayerSharingQuest().IsEmpty())
                {
                    Player player = Global.ObjAccessor.FindPlayer(_player.GetPlayerSharingQuest());
                    if (player != null)
                    {
                        player.SendPushToPartyResponse(_player, QuestPushReason.Accepted);
                        _player.ClearQuestSharingInfo();
                    }
                }

                if (_player.CanAddQuest(quest, true))
                {
                    _player.AddQuestAndCheckCompletion(quest, obj);

                    if (quest.HasFlag(QuestFlags.PartyAccept))
                    {
                        var group = _player.GetGroup();
                        if (group)
                        {
                            for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.Next())
                            {
                                var player = refe.GetSource();

                                if (!player || player == _player)     // not self
                                    continue;

                                if (player.CanTakeQuest(quest, true))
                                {
                                    player.SetQuestSharingInfo(_player.GetGUID(), quest.Id);

                                    //need confirmation that any gossip window will close
                                    player.PlayerTalkClass.SendCloseGossip();

                                    _player.SendQuestConfirmAccept(quest, player);
                                }
                            }
                        }
                    }

                    _player.PlayerTalkClass.SendCloseGossip();

                    return;
                }
            }

            CLOSE_GOSSIP_CLEAR_SHARING_INFO();
        }

        [WorldPacketHandler(ClientOpcodes.QuestGiverQueryQuest)]
        void HandleQuestgiverQueryQuest(QuestGiverQueryQuest packet)
        {
            // Verify that the guid is valid and is a questgiver or involved in the requested quest
            var obj = Global.ObjAccessor.GetObjectByTypeMask(GetPlayer(), packet.QuestGiverGUID, (TypeMask.Unit | TypeMask.GameObject | TypeMask.Item));
            if (!obj || (!obj.HasQuest(packet.QuestID) && !obj.HasInvolvedQuest(packet.QuestID)))
            {
                GetPlayer().PlayerTalkClass.SendCloseGossip();
                return;
            }

            Quest quest = Global.ObjectMgr.GetQuestTemplate(packet.QuestID);
            if (quest != null)
            {
                if (!GetPlayer().CanTakeQuest(quest, true))
                    return;

                if (quest.IsAutoAccept() && GetPlayer().CanAddQuest(quest, true))
                    GetPlayer().AddQuestAndCheckCompletion(quest, obj);

                if (quest.IsAutoComplete())
                    GetPlayer().PlayerTalkClass.SendQuestGiverRequestItems(quest, obj.GetGUID(), GetPlayer().CanCompleteQuest(quest.Id), true);
                else
                    GetPlayer().PlayerTalkClass.SendQuestGiverQuestDetails(quest, obj.GetGUID(), true, false);
            }
        }

        [WorldPacketHandler(ClientOpcodes.QueryQuestInfo)]
        void HandleQuestQuery(QueryQuestInfo packet)
        {
            Quest quest = Global.ObjectMgr.GetQuestTemplate(packet.QuestID);
            if (quest != null)
                _player.PlayerTalkClass.SendQuestQueryResponse(quest);
            else
            {
                var response = new QueryQuestInfoResponse();
                response.QuestID = packet.QuestID;
                SendPacket(response);
            }
        }

        [WorldPacketHandler(ClientOpcodes.QuestGiverChooseReward)]
        void HandleQuestgiverChooseReward(QuestGiverChooseReward packet)
        {
            Quest quest = Global.ObjectMgr.GetQuestTemplate(packet.QuestID);
            if (quest == null)
                return;

            if (packet.Choice.Item.ItemID != 0)
            {
                switch (packet.Choice.LootItemType)
                {
                    case LootItemType.Item:
                        ItemTemplate rewardProto = Global.ObjectMgr.GetItemTemplate(packet.Choice.Item.ItemID);
                        if (rewardProto == null)
                        {
                            Log.outError(LogFilter.Network, "Error in CMSG_QUESTGIVER_CHOOSE_REWARD: player {0} ({1}) tried to get invalid reward item (Item Entry: {2}) for quest {3} (possible packet-hacking detected)", GetPlayer().GetName(), GetPlayer().GetGUID().ToString(), packet.Choice.Item.ItemID, packet.QuestID);
                            return;
                        }

                        var itemValid = false;
                        for (uint i = 0; i < quest.GetRewChoiceItemsCount(); ++i)
                        {
                            if (quest.RewardChoiceItemId[i] != 0 && quest.RewardChoiceItemType[i] == LootItemType.Item && quest.RewardChoiceItemId[i] == packet.Choice.Item.ItemID)
                            {
                                itemValid = true;
                                break;
                            }
                        }

                        if (!itemValid && quest.PackageID != 0)
                        {
                            var questPackageItems = Global.DB2Mgr.GetQuestPackageItems(quest.PackageID);
                            if (questPackageItems != null)
                            {
                                foreach (var questPackageItem in questPackageItems)
                                {
                                    if (questPackageItem.ItemID != packet.Choice.Item.ItemID)
                                        continue;

                                    if (_player.CanSelectQuestPackageItem(questPackageItem))
                                    {
                                        itemValid = true;
                                        break;
                                    }
                                }
                            }

                            if (!itemValid)
                            {
                                var questPackageItems1 = Global.DB2Mgr.GetQuestPackageItemsFallback(quest.PackageID);
                                if (questPackageItems1 != null)
                                {
                                    foreach (var questPackageItem in questPackageItems1)
                                    {
                                        if (questPackageItem.ItemID != packet.Choice.Item.ItemID)
                                            continue;

                                        itemValid = true;
                                        break;
                                    }
                                }
                            }
                        }

                        if (!itemValid)
                        {
                            Log.outError(LogFilter.Network, "Error in CMSG_QUESTGIVER_CHOOSE_REWARD: player {0} ({1}) tried to get reward item (Item Entry: {2}) wich is not a reward for quest {3} (possible packet-hacking detected)", GetPlayer().GetName(), GetPlayer().GetGUID().ToString(), packet.Choice.Item.ItemID, packet.QuestID);
                            return;
                        }
                        break;
                    case LootItemType.Currency:
                        if (!CliDB.CurrencyTypesStorage.HasRecord(packet.Choice.Item.ItemID))
                        {
                            Log.outError(LogFilter.Player, $"Error in CMSG_QUESTGIVER_CHOOSE_REWARD: player {_player.GetName()} ({_player.GetGUID()}) tried to get invalid reward currency (Currency ID: {packet.Choice.Item.ItemID}) for quest {packet.QuestID} (possible packet-hacking detected)");
                            return;
                        }

                        var currencyValid = false;
                        for (uint i = 0; i < quest.GetRewChoiceItemsCount(); ++i)
                        {
                            if (quest.RewardChoiceItemId[i] != 0 && quest.RewardChoiceItemType[i] == LootItemType.Currency && quest.RewardChoiceItemId[i] == packet.Choice.Item.ItemID)
                            {
                                currencyValid = true;
                                break;
                            }
                        }
                        if (!currencyValid)
                        {
                            Log.outError(LogFilter.Player, $"Error in CMSG_QUESTGIVER_CHOOSE_REWARD: player {_player.GetName()} ({_player.GetGUID()}) tried to get reward currency (Currency ID: {packet.Choice.Item.ItemID}) wich is not a reward for quest {packet.QuestID} (possible packet-hacking detected)");
                            return;
                        }
                        break;
                }
            }

            WorldObject obj = GetPlayer();
            if (!quest.HasFlag(QuestFlags.AutoComplete))
            {
                obj = Global.ObjAccessor.GetObjectByTypeMask(GetPlayer(), packet.QuestGiverGUID, TypeMask.Unit | TypeMask.GameObject);
                if (!obj || !obj.HasInvolvedQuest(packet.QuestID))
                    return;

                // some kind of WPE protection
                if (!GetPlayer().CanInteractWithQuestGiver(obj))
                    return;
            }

            if ((!GetPlayer().CanSeeStartQuest(quest) && GetPlayer().GetQuestStatus(packet.QuestID) == QuestStatus.None) ||
                (GetPlayer().GetQuestStatus(packet.QuestID) != QuestStatus.Complete && !quest.IsAutoComplete()))
            {
                Log.outError(LogFilter.Network, "Error in QuestStatus.Complete: player {0} ({1}) tried to complete quest {2}, but is not allowed to do so (possible packet-hacking or high latency)",
                    GetPlayer().GetName(), GetPlayer().GetGUID().ToString(), packet.QuestID);
                return;
            }

            if (GetPlayer().CanRewardQuest(quest, packet.Choice.LootItemType, packet.Choice.Item.ItemID, true))
            {
                GetPlayer().RewardQuest(quest, packet.Choice.LootItemType, packet.Choice.Item.ItemID, obj);

                switch (obj.GetTypeId())
                {
                    case TypeId.Unit:
                    case TypeId.Player:
                        {
                            //For AutoSubmition was added plr case there as it almost same exclute AI script cases.
                            var unitQGiver = obj.ToUnit();
                            // Send next quest
                            Quest nextQuest = _player.GetNextQuest(packet.QuestGiverGUID, quest);
                            if (nextQuest != null)
                            {
                                // Only send the quest to the player if the conditions are met
                                if (_player.CanTakeQuest(nextQuest, false))
                                {
                                    if (nextQuest.IsAutoAccept() && _player.CanAddQuest(nextQuest, true))
                                        _player.AddQuestAndCheckCompletion(nextQuest, obj);

                                    _player.PlayerTalkClass.SendQuestGiverQuestDetails(nextQuest, packet.QuestGiverGUID, true, false);
                                }
                            }

                            _player.PlayerTalkClass.ClearMenus();
                            var qGiverAI = unitQGiver.GetAI();
                            if (qGiverAI != null)
                                qGiverAI.QuestReward(_player, quest, packet.Choice.LootItemType, packet.Choice.Item.ItemID);
                            break;
                        }
                    case TypeId.GameObject:
                        {
                            var questGiver = obj.ToGameObject();
                            // Send next quest
                            Quest nextQuest = _player.GetNextQuest(packet.QuestGiverGUID, quest);
                            if (nextQuest != null)
                            {
                                // Only send the quest to the player if the conditions are met
                                if (_player.CanTakeQuest(nextQuest, false))
                                {
                                    if (nextQuest.IsAutoAccept() && _player.CanAddQuest(nextQuest, true))
                                        _player.AddQuestAndCheckCompletion(nextQuest, obj);

                                    _player.PlayerTalkClass.SendQuestGiverQuestDetails(nextQuest, packet.QuestGiverGUID, true, false);
                                }
                            }

                            _player.PlayerTalkClass.ClearMenus();
                            questGiver.GetAI().QuestReward(_player, quest, packet.Choice.LootItemType, packet.Choice.Item.ItemID);
                            break;
                        }
                    default:
                        break;
                }
            }
            else
                GetPlayer().PlayerTalkClass.SendQuestGiverOfferReward(quest, packet.QuestGiverGUID, true);
        }

        [WorldPacketHandler(ClientOpcodes.QuestGiverRequestReward)]
        void HandleQuestgiverRequestReward(QuestGiverRequestReward packet)
        {
            var obj = Global.ObjAccessor.GetObjectByTypeMask(GetPlayer(), packet.QuestGiverGUID, TypeMask.Unit | TypeMask.GameObject);
            if (obj == null || !obj.HasInvolvedQuest(packet.QuestID))
                return;

            // some kind of WPE protection
            if (!GetPlayer().CanInteractWithQuestGiver(obj))
                return;

            if (GetPlayer().CanCompleteQuest(packet.QuestID))
                GetPlayer().CompleteQuest(packet.QuestID);

            if (GetPlayer().GetQuestStatus(packet.QuestID) != QuestStatus.Complete)
                return;

            Quest quest = Global.ObjectMgr.GetQuestTemplate(packet.QuestID);
            if (quest != null)
                GetPlayer().PlayerTalkClass.SendQuestGiverOfferReward(quest, packet.QuestGiverGUID, true);
        }

        [WorldPacketHandler(ClientOpcodes.QuestLogRemoveQuest)]
        void HandleQuestLogRemoveQuest(QuestLogRemoveQuest packet)
        {
            if (packet.Entry < SharedConst.MaxQuestLogSize)
            {
                uint questId = GetPlayer().GetQuestSlotQuestId(packet.Entry);
                if (questId != 0)
                {
                    if (!GetPlayer().TakeQuestSourceItem(questId, true))
                        return;                                     // can't un-equip some items, reject quest cancel

                    Quest quest = Global.ObjectMgr.GetQuestTemplate(questId);
                    QuestStatus oldStatus = _player.GetQuestStatus(questId);

                    if (quest != null)
                    {
                        if (quest.HasSpecialFlag(QuestSpecialFlags.Timed))
                            GetPlayer().RemoveTimedQuest(questId);

                        if (quest.HasFlag(QuestFlags.Pvp))
                        {
                            GetPlayer().pvpInfo.IsHostile = GetPlayer().pvpInfo.IsInHostileArea || GetPlayer().HasPvPForcingQuest();
                            GetPlayer().UpdatePvPState();
                        }
                    }

                    GetPlayer().TakeQuestSourceItem(questId, true); // remove quest src item from player
                    GetPlayer().AbandonQuest(questId); // remove all quest items player received before abandoning quest. Note, this does not remove normal drop items that happen to be quest requirements. 
                    GetPlayer().RemoveActiveQuest(questId);
                    GetPlayer().RemoveCriteriaTimer(CriteriaTimedTypes.Quest, questId);

                    Log.outInfo(LogFilter.Network, "Player {0} abandoned quest {1}", GetPlayer().GetGUID().ToString(), questId);

                    Global.ScriptMgr.OnQuestStatusChange(_player, questId);

                    if (quest != null)
                        Global.ScriptMgr.OnQuestStatusChange(_player, quest, oldStatus, QuestStatus.None);
                }

                GetPlayer().SetQuestSlot(packet.Entry, 0);

                GetPlayer().UpdateCriteria(CriteriaTypes.QuestAbandoned, 1);
            }
        }

        [WorldPacketHandler(ClientOpcodes.QuestConfirmAccept)]
        void HandleQuestConfirmAccept(QuestConfirmAccept packet)
        {
            Quest quest = Global.ObjectMgr.GetQuestTemplate(packet.QuestID);
            if (quest != null)
            {
                if (!quest.HasFlag(QuestFlags.PartyAccept))
                    return;

                Player originalPlayer = Global.ObjAccessor.FindPlayer(GetPlayer().GetPlayerSharingQuest());
                if (originalPlayer == null)
                    return;

                if (!GetPlayer().IsInSameRaidWith(originalPlayer))
                    return;

                if (!originalPlayer.IsActiveQuest(packet.QuestID))
                    return;

                if (!GetPlayer().CanTakeQuest(quest, true))
                    return;

                if (GetPlayer().CanAddQuest(quest, true))
                {
                    GetPlayer().AddQuestAndCheckCompletion(quest, null);                // NULL, this prevent DB script from duplicate running

                    if (quest.SourceSpellID > 0)
                        _player.CastSpell(_player, quest.SourceSpellID, true);
                }
            }

            GetPlayer().ClearQuestSharingInfo();
        }

        [WorldPacketHandler(ClientOpcodes.QuestGiverCompleteQuest)]
        void HandleQuestgiverCompleteQuest(QuestGiverCompleteQuest packet)
        {
            var autoCompleteMode = packet.FromScript; // 0 - standart complete quest mode with npc, 1 - auto-complete mode

            Quest quest = Global.ObjectMgr.GetQuestTemplate(packet.QuestID);
            if (quest == null)
                return;

            if (autoCompleteMode && !quest.HasFlag(QuestFlags.AutoComplete))
                return;

            WorldObject obj;
            if (autoCompleteMode)
                obj = GetPlayer();
            else
                obj = Global.ObjAccessor.GetObjectByTypeMask(GetPlayer(), packet.QuestGiverGUID, TypeMask.Unit | TypeMask.GameObject);

            if (!obj)
                return;

            if (!autoCompleteMode)
            {
                if (!obj.HasInvolvedQuest(packet.QuestID))
                    return;

                // some kind of WPE protection
                if (!GetPlayer().CanInteractWithQuestGiver(obj))
                    return;
            }
            else
            {
                // Do not allow completing quests on other players.
                if (packet.QuestGiverGUID != GetPlayer().GetGUID())
                    return;
            }

            if (!GetPlayer().CanSeeStartQuest(quest) && GetPlayer().GetQuestStatus(packet.QuestID) == QuestStatus.None)
            {
                Log.outError(LogFilter.Network, "Possible hacking attempt: Player {0} ({1}) tried to complete quest [entry: {2}] without being in possession of the quest!",
                    GetPlayer().GetName(), GetPlayer().GetGUID().ToString(), packet.QuestID);
                return;
            }
            Battleground bg = GetPlayer().GetBattleground();
            if (bg)
                bg.HandleQuestComplete(packet.QuestID, GetPlayer());

            if (GetPlayer().GetQuestStatus(packet.QuestID) != QuestStatus.Complete)
            {
                if (quest.IsRepeatable())
                    GetPlayer().PlayerTalkClass.SendQuestGiverRequestItems(quest, packet.QuestGiverGUID, GetPlayer().CanCompleteRepeatableQuest(quest), false);
                else
                    GetPlayer().PlayerTalkClass.SendQuestGiverRequestItems(quest, packet.QuestGiverGUID, GetPlayer().CanRewardQuest(quest, false), false);
            }
            else
            {
                if (quest.HasSpecialFlag(QuestSpecialFlags.Deliver))                  // some items required
                    GetPlayer().PlayerTalkClass.SendQuestGiverRequestItems(quest, packet.QuestGiverGUID, GetPlayer().CanRewardQuest(quest, false), false);
                else                                            // no items required
                    GetPlayer().PlayerTalkClass.SendQuestGiverOfferReward(quest, packet.QuestGiverGUID, true);
            }
        }

        [WorldPacketHandler(ClientOpcodes.QuestGiverCloseQuest)]
        void HandleQuestgiverCloseQuest(QuestGiverCloseQuest questGiverCloseQuest)
        {
            if (_player.FindQuestSlot(questGiverCloseQuest.QuestID) >= SharedConst.MaxQuestLogSize)
                return;

            Quest quest = Global.ObjectMgr.GetQuestTemplate(questGiverCloseQuest.QuestID);
            if (quest == null)
                return;

            Global.ScriptMgr.OnQuestAcknowledgeAutoAccept(_player, quest);
        }

        [WorldPacketHandler(ClientOpcodes.PushQuestToParty)]
        void HandlePushQuestToParty(PushQuestToParty packet)
        {
            if (!GetPlayer().CanShareQuest(packet.QuestID))
                return;

            Quest quest = Global.ObjectMgr.GetQuestTemplate(packet.QuestID);
            if (quest == null)
                return;

            var sender = GetPlayer();

            var group = sender.GetGroup();
            if (!group)
            {
                sender.SendPushToPartyResponse(sender, QuestPushReason.NotInParty);
                return;
            }

            for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.Next())
            {
                var receiver = refe.GetSource();

                if (!receiver || receiver == sender)
                    continue;

                if (!receiver.SatisfyQuestStatus(quest, false))
                {
                    sender.SendPushToPartyResponse(receiver, QuestPushReason.OnQuest);
                    continue;
                }

                if (receiver.GetQuestStatus(packet.QuestID) == QuestStatus.Complete)
                {
                    sender.SendPushToPartyResponse(receiver, QuestPushReason.AlreadyDone);
                    continue;
                }

                if (!receiver.SatisfyQuestDay(quest, false))
                {
                    sender.SendPushToPartyResponse(receiver, QuestPushReason.DifferentServerDaily);
                    continue;
                }

                if (!receiver.CanTakeQuest(quest, false))
                {
                    sender.SendPushToPartyResponse(receiver, QuestPushReason.Invalid);
                    continue;
                }

                if (!receiver.SatisfyQuestLog(false))
                {
                    sender.SendPushToPartyResponse(receiver, QuestPushReason.LogFull);
                    continue;
                }

                if (!receiver.GetPlayerSharingQuest().IsEmpty())
                {
                    sender.SendPushToPartyResponse(receiver, QuestPushReason.Busy);
                    continue;
                }

                sender.SendPushToPartyResponse(receiver, QuestPushReason.Success);

                if (quest.IsAutoAccept() && receiver.CanAddQuest(quest, true) && receiver.CanTakeQuest(quest, true))
                    receiver.AddQuestAndCheckCompletion(quest, sender);

                if ((quest.IsAutoComplete() && quest.IsRepeatable() && !quest.IsDailyOrWeekly()) || quest.HasFlag(QuestFlags.AutoComplete))
                    receiver.PlayerTalkClass.SendQuestGiverRequestItems(quest, sender.GetGUID(), receiver.CanCompleteRepeatableQuest(quest), true);
                else
                {
                    receiver.SetQuestSharingInfo(sender.GetGUID(), quest.Id);
                    receiver.PlayerTalkClass.SendQuestGiverQuestDetails(quest, receiver.GetGUID(), true, false);
                }
            }
        }

        [WorldPacketHandler(ClientOpcodes.QuestPushResult)]
        void HandleQuestPushResult(QuestPushResult packet)
        {
            if (!GetPlayer().GetPlayerSharingQuest().IsEmpty())
            {
                if (_player.GetPlayerSharingQuest() == packet.SenderGUID)
                {
                    Player player = Global.ObjAccessor.FindPlayer(_player.GetPlayerSharingQuest());
                    if (player)
                        player.SendPushToPartyResponse(_player, packet.Result);
                }

                _player.ClearQuestSharingInfo();
            }
        }

        [WorldPacketHandler(ClientOpcodes.QuestGiverStatusMultipleQuery)]
        void HandleQuestgiverStatusMultipleQuery(QuestGiverStatusMultipleQuery packet)
        {
            _player.SendQuestGiverStatusMultiple();
        }

        [WorldPacketHandler(ClientOpcodes.RequestWorldQuestUpdate)]
        void HandleRequestWorldQuestUpdate(RequestWorldQuestUpdate packet)
        {
            var response = new WorldQuestUpdateResponse();

            // @todo: 7.x Has to be implemented
            //response.WorldQuestUpdates.push_back(WorldPackets::Quest::WorldQuestUpdateInfo(lastUpdate, questID, timer, variableID, value));

            SendPacket(response);
        }

        [WorldPacketHandler(ClientOpcodes.ChoiceResponse)]
        void HandlePlayerChoiceResponse(ChoiceResponse choiceResponse)
        {
            if (_player.PlayerTalkClass.GetInteractionData().PlayerChoiceId != choiceResponse.ChoiceID)
            {
                Log.outError(LogFilter.Player, $"Error in CMSG_CHOICE_RESPONSE: {GetPlayerInfo()} tried to respond to invalid player choice {choiceResponse.ChoiceID} (allowed {_player.PlayerTalkClass.GetInteractionData().PlayerChoiceId}) (possible packet-hacking detected)");
                return;
            }

            PlayerChoice playerChoice = Global.ObjectMgr.GetPlayerChoice(choiceResponse.ChoiceID);
            if (playerChoice == null)
                return;

            PlayerChoiceResponse playerChoiceResponse = playerChoice.GetResponse(choiceResponse.ResponseID);
            if (playerChoiceResponse == null)
            {
                Log.outError(LogFilter.Player, $"Error in CMSG_CHOICE_RESPONSE: {GetPlayerInfo()} tried to select invalid player choice response {choiceResponse.ResponseID} (possible packet-hacking detected)");
                return;
            }

            Global.ScriptMgr.OnPlayerChoiceResponse(GetPlayer(), (uint)choiceResponse.ChoiceID, (uint)choiceResponse.ResponseID);

            if (playerChoiceResponse.Reward.HasValue)
            {
                var reward = playerChoiceResponse.Reward.Value;
                if (reward.TitleId != 0)
                    _player.SetTitle(CliDB.CharTitlesStorage.LookupByKey(reward.TitleId), false);

                if (reward.PackageId != 0)
                    _player.RewardQuestPackage((uint)reward.PackageId);

                if (reward.SkillLineId != 0 && _player.HasSkill((SkillType)reward.SkillLineId))
                    _player.UpdateSkillPro((uint)reward.SkillLineId, 1000, reward.SkillPointCount);

                if (reward.HonorPointCount != 0)
                    _player.AddHonorXP(reward.HonorPointCount);

                if (reward.Money != 0)
                    _player.ModifyMoney((long)reward.Money, false);

                if (reward.Xp != 0)
                    _player.GiveXP(reward.Xp, null, 0.0f);

                foreach (PlayerChoiceResponseRewardItem item in reward.Items)
                {
                    var dest = new List<ItemPosCount>();
                    if (_player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, item.Id, (uint)item.Quantity) == InventoryResult.Ok)
                    {
                        Item newItem = _player.StoreNewItem(dest, item.Id, true, ItemEnchantmentManager.GenerateItemRandomBonusListId(item.Id), null, ItemContext.QuestReward, item.BonusListIDs);
                        _player.SendNewItem(newItem, (uint)item.Quantity, true, false);
                    }
                }

                foreach (PlayerChoiceResponseRewardEntry currency in reward.Currency)
                    _player.ModifyCurrency((CurrencyTypes)currency.Id, currency.Quantity);

                foreach (PlayerChoiceResponseRewardEntry faction in reward.Faction)
                    _player.GetReputationMgr().ModifyReputation(CliDB.FactionStorage.LookupByKey(faction.Id), faction.Quantity);
            }
        }
    }
}
