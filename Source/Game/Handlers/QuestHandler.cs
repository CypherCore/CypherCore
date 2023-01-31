// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Networking;
using Game.Networking.Packets;
using Game.Scripting.Interfaces.IPlayer;
using Game.Scripting.Interfaces.IQuest;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.QuestGiverStatusQuery, Processing = PacketProcessing.Inplace)]
        private void HandleQuestgiverStatusQuery(QuestGiverStatusQuery packet)
        {
            QuestGiverStatus questStatus = QuestGiverStatus.None;

            var questgiver = Global.ObjAccessor.GetObjectByTypeMask(_player, packet.QuestGiverGUID, TypeMask.Unit | TypeMask.GameObject);

            if (!questgiver)
            {
                Log.outInfo(LogFilter.Network, "Error in CMSG_QUESTGIVER_STATUS_QUERY, called for non-existing questgiver {0}", packet.QuestGiverGUID.ToString());

                return;
            }

            switch (questgiver.GetTypeId())
            {
                case TypeId.Unit:
                    if (!questgiver.ToCreature().IsHostileTo(GetPlayer())) // do not show quest status to enemies
                        questStatus = _player.GetQuestDialogStatus(questgiver);

                    break;
                case TypeId.GameObject:
                    questStatus = _player.GetQuestDialogStatus(questgiver);

                    break;
                default:
                    Log.outError(LogFilter.Network, "QuestGiver called for unexpected Type {0}", questgiver.GetTypeId());

                    break;
            }

            //inform client about status of quest
            _player.PlayerTalkClass.SendQuestGiverStatus(questStatus, packet.QuestGiverGUID);
        }

        [WorldPacketHandler(ClientOpcodes.QuestGiverHello, Processing = PacketProcessing.Inplace)]
        private void HandleQuestgiverHello(QuestGiverHello packet)
        {
            Creature creature = _player.GetNPCIfCanInteractWith(packet.QuestGiverGUID, NPCFlags.QuestGiver, NPCFlags2.None);

            if (creature == null)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleQuestgiverHello - {0} not found or you can't interact with him.", packet.QuestGiverGUID.ToString());

                return;
            }

            // remove fake death
            if (_player.HasUnitState(UnitState.Died))
                _player.RemoveAurasByType(AuraType.FeignDeath);

            // Stop the npc if moving
            uint pause = creature.GetMovementTemplate().GetInteractionPauseTimer();

            if (pause != 0)
                creature.PauseMovement(pause);

            creature.SetHomePosition(creature.GetPosition());

            _player.PlayerTalkClass.ClearMenus();

            if (creature.GetAI().OnGossipHello(_player))
                return;

            _player.PrepareGossipMenu(creature, creature.GetCreatureTemplate().GossipMenuId, true);
            _player.SendPreparedGossip(creature);
        }

        [WorldPacketHandler(ClientOpcodes.QuestGiverAcceptQuest, Processing = PacketProcessing.Inplace)]
        private void HandleQuestgiverAcceptQuest(QuestGiverAcceptQuest packet)
        {
            WorldObject obj;

            if (!packet.QuestGiverGUID.IsPlayer())
                obj = Global.ObjAccessor.GetObjectByTypeMask(_player, packet.QuestGiverGUID, TypeMask.Unit | TypeMask.GameObject | TypeMask.Item);
            else
                obj = Global.ObjAccessor.FindPlayer(packet.QuestGiverGUID);

            var CLOSE_GOSSIP_CLEAR_SHARING_INFO = new Action(() =>
                                                             {
                                                                 _player.PlayerTalkClass.SendCloseGossip();
                                                                 _player.ClearQuestSharingInfo();
                                                             });

            // no or incorrect quest giver
            if (obj == null)
            {
                CLOSE_GOSSIP_CLEAR_SHARING_INFO();

                return;
            }

            Player playerQuestObject = obj.ToPlayer();

            if (playerQuestObject)
            {
                if ((_player.GetPlayerSharingQuest().IsEmpty() && _player.GetPlayerSharingQuest() != packet.QuestGiverGUID) ||
                    !playerQuestObject.CanShareQuest(packet.QuestID))
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
                if (!_player.CanTakeQuest(quest, true))
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
                            for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.Next())
                            {
                                Player player = refe.GetSource();

                                if (!player ||
                                    player == _player ||
                                    !player.IsInMap(_player)) // not self and in same map
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

                    _player.PlayerTalkClass.SendCloseGossip();

                    return;
                }
            }

            CLOSE_GOSSIP_CLEAR_SHARING_INFO();
        }

        [WorldPacketHandler(ClientOpcodes.QuestGiverQueryQuest, Processing = PacketProcessing.Inplace)]
        private void HandleQuestgiverQueryQuest(QuestGiverQueryQuest packet)
        {
            // Verify that the Guid is valid and is a questgiver or involved in the requested quest
            var obj = Global.ObjAccessor.GetObjectByTypeMask(_player, packet.QuestGiverGUID, (TypeMask.Unit | TypeMask.GameObject | TypeMask.Item));

            if (!obj ||
                (!obj.HasQuest(packet.QuestID) && !obj.HasInvolvedQuest(packet.QuestID)))
            {
                _player.PlayerTalkClass.SendCloseGossip();

                return;
            }

            Quest quest = Global.ObjectMgr.GetQuestTemplate(packet.QuestID);

            if (quest != null)
            {
                if (!_player.CanTakeQuest(quest, true))
                    return;

                if (quest.IsAutoAccept() &&
                    _player.CanAddQuest(quest, true))
                    _player.AddQuestAndCheckCompletion(quest, obj);

                if (quest.IsAutoComplete())
                    _player.PlayerTalkClass.SendQuestGiverRequestItems(quest, obj.GetGUID(), _player.CanCompleteQuest(quest.Id), true);
                else
                    _player.PlayerTalkClass.SendQuestGiverQuestDetails(quest, obj.GetGUID(), true, false);
            }
        }

        [WorldPacketHandler(ClientOpcodes.QueryQuestInfo, Processing = PacketProcessing.Inplace)]
        private void HandleQuestQuery(QueryQuestInfo packet)
        {
            Quest quest = Global.ObjectMgr.GetQuestTemplate(packet.QuestID);

            if (quest != null)
            {
                _player.PlayerTalkClass.SendQuestQueryResponse(quest);
            }
            else
            {
                QueryQuestInfoResponse response = new();
                response.QuestID = packet.QuestID;
                SendPacket(response);
            }
        }

        [WorldPacketHandler(ClientOpcodes.QuestGiverChooseReward, Processing = PacketProcessing.Inplace)]
        private void HandleQuestgiverChooseReward(QuestGiverChooseReward packet)
        {
            Quest quest = Global.ObjectMgr.GetQuestTemplate(packet.QuestID);

            if (quest == null)
                return;

            if (packet.Choice.Item.ItemID != 0)
                switch (packet.Choice.LootItemType)
                {
                    case LootItemType.Item:
                        ItemTemplate rewardProto = Global.ObjectMgr.GetItemTemplate(packet.Choice.Item.ItemID);

                        if (rewardProto == null)
                        {
                            Log.outError(LogFilter.Network, "Error in CMSG_QUESTGIVER_CHOOSE_REWARD: player {0} ({1}) tried to get invalid reward Item (Item Entry: {2}) for quest {3} (possible packet-hacking detected)", _player.GetName(), _player.GetGUID().ToString(), packet.Choice.Item.ItemID, packet.QuestID);

                            return;
                        }

                        bool itemValid = false;

                        for (uint i = 0; i < quest.GetRewChoiceItemsCount(); ++i)
                            if (quest.RewardChoiceItemId[i] != 0 &&
                                quest.RewardChoiceItemType[i] == LootItemType.Item &&
                                quest.RewardChoiceItemId[i] == packet.Choice.Item.ItemID)
                            {
                                itemValid = true;

                                break;
                            }

                        if (!itemValid &&
                            quest.PackageID != 0)
                        {
                            var questPackageItems = Global.DB2Mgr.GetQuestPackageItems(quest.PackageID);

                            if (questPackageItems != null)
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

                            if (!itemValid)
                            {
                                var questPackageItems1 = Global.DB2Mgr.GetQuestPackageItemsFallback(quest.PackageID);

                                if (questPackageItems1 != null)
                                    foreach (var questPackageItem in questPackageItems1)
                                    {
                                        if (questPackageItem.ItemID != packet.Choice.Item.ItemID)
                                            continue;

                                        itemValid = true;

                                        break;
                                    }
                            }
                        }

                        if (!itemValid)
                        {
                            Log.outError(LogFilter.Network, "Error in CMSG_QUESTGIVER_CHOOSE_REWARD: player {0} ({1}) tried to get reward Item (Item Entry: {2}) wich is not a reward for quest {3} (possible packet-hacking detected)", _player.GetName(), _player.GetGUID().ToString(), packet.Choice.Item.ItemID, packet.QuestID);

                            return;
                        }

                        break;
                    case LootItemType.Currency:
                        if (!CliDB.CurrencyTypesStorage.HasRecord(packet.Choice.Item.ItemID))
                        {
                            Log.outError(LogFilter.Player, $"Error in CMSG_QUESTGIVER_CHOOSE_REWARD: player {_player.GetName()} ({_player.GetGUID()}) tried to get invalid reward currency (Currency ID: {packet.Choice.Item.ItemID}) for quest {packet.QuestID} (possible packet-hacking detected)");

                            return;
                        }

                        bool currencyValid = false;

                        for (uint i = 0; i < quest.GetRewChoiceItemsCount(); ++i)
                            if (quest.RewardChoiceItemId[i] != 0 &&
                                quest.RewardChoiceItemType[i] == LootItemType.Currency &&
                                quest.RewardChoiceItemId[i] == packet.Choice.Item.ItemID)
                            {
                                currencyValid = true;

                                break;
                            }

                        if (!currencyValid)
                        {
                            Log.outError(LogFilter.Player, $"Error in CMSG_QUESTGIVER_CHOOSE_REWARD: player {_player.GetName()} ({_player.GetGUID()}) tried to get reward currency (Currency ID: {packet.Choice.Item.ItemID}) wich is not a reward for quest {packet.QuestID} (possible packet-hacking detected)");

                            return;
                        }

                        break;
                }

            WorldObject obj = GetPlayer();

            if (!quest.HasFlag(QuestFlags.AutoComplete))
            {
                obj = Global.ObjAccessor.GetObjectByTypeMask(_player, packet.QuestGiverGUID, TypeMask.Unit | TypeMask.GameObject);

                if (!obj ||
                    !obj.HasInvolvedQuest(packet.QuestID))
                    return;

                // some kind of WPE protection
                if (!_player.CanInteractWithQuestGiver(obj))
                    return;
            }

            if ((!_player.CanSeeStartQuest(quest) && _player.GetQuestStatus(packet.QuestID) == QuestStatus.None) ||
                (_player.GetQuestStatus(packet.QuestID) != QuestStatus.Complete && !quest.IsAutoComplete()))
            {
                Log.outError(LogFilter.Network,
                             "Error in QuestStatus.Complete: player {0} ({1}) tried to complete quest {2}, but is not allowed to do so (possible packet-hacking or high latency)",
                             _player.GetName(),
                             _player.GetGUID().ToString(),
                             packet.QuestID);

                return;
            }

            if (_player.CanRewardQuest(quest, true)) // First, check if player is allowed to turn the quest in (all objectives completed). If not, we send players to the offer reward screen
            {
                if (_player.CanRewardQuest(quest, packet.Choice.LootItemType, packet.Choice.Item.ItemID, true)) // Then check if player can receive the reward Item (if inventory is not full, if player doesn't have too many unique items, and so on). If not, the client will close the gossip window
                {
                    Battleground bg = _player.GetBattleground();

                    GetPlayer().RewardQuest(quest, packet.Choice.LootItemType, packet.Choice.Item.ItemID, obj);
                }
            }
            else
            {
                _player.PlayerTalkClass.SendQuestGiverOfferReward(quest, packet.QuestGiverGUID, true);
            }
        }

        [WorldPacketHandler(ClientOpcodes.QuestGiverRequestReward, Processing = PacketProcessing.Inplace)]
        private void HandleQuestgiverRequestReward(QuestGiverRequestReward packet)
        {
            WorldObject obj = Global.ObjAccessor.GetObjectByTypeMask(_player, packet.QuestGiverGUID, TypeMask.Unit | TypeMask.GameObject);

            if (obj == null ||
                !obj.HasInvolvedQuest(packet.QuestID))
                return;

            // some kind of WPE protection
            if (!_player.CanInteractWithQuestGiver(obj))
                return;

            if (_player.CanCompleteQuest(packet.QuestID))
                _player.CompleteQuest(packet.QuestID);

            if (_player.GetQuestStatus(packet.QuestID) != QuestStatus.Complete)
                return;

            Quest quest = Global.ObjectMgr.GetQuestTemplate(packet.QuestID);

            if (quest != null)
                _player.PlayerTalkClass.SendQuestGiverOfferReward(quest, packet.QuestGiverGUID, true);
        }

        [WorldPacketHandler(ClientOpcodes.QuestLogRemoveQuest, Processing = PacketProcessing.Inplace)]
        private void HandleQuestLogRemoveQuest(QuestLogRemoveQuest packet)
        {
            if (packet.Entry < SharedConst.MaxQuestLogSize)
            {
                uint questId = _player.GetQuestSlotQuestId(packet.Entry);

                if (questId != 0)
                {
                    if (!_player.TakeQuestSourceItem(questId, true))
                        return; // can't un-equip some items, reject quest cancel

                    Quest quest = Global.ObjectMgr.GetQuestTemplate(questId);
                    QuestStatus oldStatus = _player.GetQuestStatus(questId);

                    if (quest != null)
                    {
                        if (quest.LimitTime != 0)
                            _player.RemoveTimedQuest(questId);

                        if (quest.HasFlag(QuestFlags.Pvp))
                        {
                            _player.PvpInfo.IsHostile = _player.PvpInfo.IsInHostileArea || _player.HasPvPForcingQuest();
                            _player.UpdatePvPState();
                        }
                    }

                    _player.SetQuestSlot(packet.Entry, 0);
                    _player.TakeQuestSourceItem(questId, true); // remove quest src Item from player
                    _player.AbandonQuest(questId);              // remove all quest items player received before abandoning quest. Note, this does not remove normal drop items that happen to be quest requirements. 
                    _player.RemoveActiveQuest(questId);
                    _player.RemoveCriteriaTimer(CriteriaStartEvent.AcceptQuest, questId);

                    Log.outInfo(LogFilter.Network, "Player {0} abandoned quest {1}", _player.GetGUID().ToString(), questId);

                    Global.ScriptMgr.ForEach<IPlayerOnQuestStatusChange>(p => p.OnQuestStatusChange(_player, questId));

                    if (quest != null)
                        Global.ScriptMgr.RunScript<IQuestOnQuestStatusChange>(script => script.OnQuestStatusChange(_player, quest, oldStatus, QuestStatus.None), quest.ScriptId);
                }

                _player.UpdateCriteria(CriteriaType.AbandonAnyQuest, 1);
            }
        }

        [WorldPacketHandler(ClientOpcodes.QuestConfirmAccept)]
        private void HandleQuestConfirmAccept(QuestConfirmAccept packet)
        {
            Quest quest = Global.ObjectMgr.GetQuestTemplate(packet.QuestID);

            if (quest != null)
            {
                if (!quest.HasFlag(QuestFlags.PartyAccept))
                    return;

                Player originalPlayer = Global.ObjAccessor.FindPlayer(_player.GetPlayerSharingQuest());

                if (originalPlayer == null)
                    return;

                if (!_player.IsInSameRaidWith(originalPlayer))
                    return;

                if (!originalPlayer.IsActiveQuest(packet.QuestID))
                    return;

                if (!_player.CanTakeQuest(quest, true))
                    return;

                if (_player.CanAddQuest(quest, true))
                {
                    _player.AddQuestAndCheckCompletion(quest, null); // NULL, this prevent DB script from duplicate running

                    if (quest.SourceSpellID > 0)
                        _player.CastSpell(_player, quest.SourceSpellID, true);
                }
            }

            _player.ClearQuestSharingInfo();
        }

        [WorldPacketHandler(ClientOpcodes.QuestGiverCompleteQuest, Processing = PacketProcessing.Inplace)]
        private void HandleQuestgiverCompleteQuest(QuestGiverCompleteQuest packet)
        {
            bool autoCompleteMode = packet.FromScript; // 0 - standart complete quest mode with npc, 1 - auto-complete mode

            Quest quest = Global.ObjectMgr.GetQuestTemplate(packet.QuestID);

            if (quest == null)
                return;

            if (autoCompleteMode && !quest.HasFlag(QuestFlags.AutoComplete))
                return;

            WorldObject obj;

            if (autoCompleteMode)
                obj = GetPlayer();
            else
                obj = Global.ObjAccessor.GetObjectByTypeMask(_player, packet.QuestGiverGUID, TypeMask.Unit | TypeMask.GameObject);

            if (!obj)
                return;

            if (!autoCompleteMode)
            {
                if (!obj.HasInvolvedQuest(packet.QuestID))
                    return;

                // some kind of WPE protection
                if (!_player.CanInteractWithQuestGiver(obj))
                    return;
            }
            else
            {
                // Do not allow completing quests on other players.
                if (packet.QuestGiverGUID != _player.GetGUID())
                    return;
            }

            if (!_player.CanSeeStartQuest(quest) &&
                _player.GetQuestStatus(packet.QuestID) == QuestStatus.None)
            {
                Log.outError(LogFilter.Network,
                             "Possible hacking attempt: Player {0} ({1}) tried to complete quest [entry: {2}] without being in possession of the quest!",
                             _player.GetName(),
                             _player.GetGUID().ToString(),
                             packet.QuestID);

                return;
            }

            if (_player.GetQuestStatus(packet.QuestID) != QuestStatus.Complete)
            {
                if (quest.IsRepeatable())
                    _player.PlayerTalkClass.SendQuestGiverRequestItems(quest, packet.QuestGiverGUID, _player.CanCompleteRepeatableQuest(quest), false);
                else
                    _player.PlayerTalkClass.SendQuestGiverRequestItems(quest, packet.QuestGiverGUID, _player.CanRewardQuest(quest, false), false);
            }
            else
            {
                if (quest.HasQuestObjectiveType(QuestObjectiveType.Item)) // some items required
                    _player.PlayerTalkClass.SendQuestGiverRequestItems(quest, packet.QuestGiverGUID, _player.CanRewardQuest(quest, false), false);
                else // no items required
                    _player.PlayerTalkClass.SendQuestGiverOfferReward(quest, packet.QuestGiverGUID, true);
            }
        }

        [WorldPacketHandler(ClientOpcodes.QuestGiverCloseQuest, Processing = PacketProcessing.Inplace)]
        private void HandleQuestgiverCloseQuest(QuestGiverCloseQuest questGiverCloseQuest)
        {
            if (_player.FindQuestSlot(questGiverCloseQuest.QuestID) >= SharedConst.MaxQuestLogSize)
                return;

            Quest quest = Global.ObjectMgr.GetQuestTemplate(questGiverCloseQuest.QuestID);

            if (quest == null)
                return;

            Global.ScriptMgr.RunScript<IQuestOnAckAutoAccept>(script => script.OnAcknowledgeAutoAccept(_player, quest), quest.ScriptId);
        }

        [WorldPacketHandler(ClientOpcodes.PushQuestToParty)]
        private void HandlePushQuestToParty(PushQuestToParty packet)
        {
            Quest quest = Global.ObjectMgr.GetQuestTemplate(packet.QuestID);

            if (quest == null)
                return;

            Player sender = GetPlayer();

            if (!_player.CanShareQuest(packet.QuestID))
            {
                sender.SendPushToPartyResponse(sender, QuestPushReason.NotAllowed);

                return;
            }

            // in pool and not currently available (wintergrasp weekly, dalaran weekly) - can't share
            if (Global.QuestPoolMgr.IsQuestActive(packet.QuestID))
            {
                sender.SendPushToPartyResponse(sender, QuestPushReason.NotDaily);

                return;
            }

            Group group = sender.GetGroup();

            if (!group)
            {
                sender.SendPushToPartyResponse(sender, QuestPushReason.NotInParty);

                return;
            }

            for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.Next())
            {
                Player receiver = refe.GetSource();

                if (!receiver ||
                    receiver == sender)
                    continue;

                if (!receiver.GetPlayerSharingQuest().IsEmpty())
                {
                    sender.SendPushToPartyResponse(receiver, QuestPushReason.Busy);

                    continue;
                }

                if (!receiver.IsAlive())
                {
                    sender.SendPushToPartyResponse(receiver, QuestPushReason.Dead);
                    receiver.SendPushToPartyResponse(sender, QuestPushReason.DeadToRecipient, quest);

                    continue;
                }

                switch (receiver.GetQuestStatus(packet.QuestID))
                {
                    case QuestStatus.Rewarded:
                        {
                            sender.SendPushToPartyResponse(receiver, QuestPushReason.AlreadyDone);
                            receiver.SendPushToPartyResponse(sender, QuestPushReason.AlreadyDoneToRecipient, quest);

                            continue;
                        }
                    case QuestStatus.Incomplete:
                    case QuestStatus.Complete:
                        {
                            sender.SendPushToPartyResponse(receiver, QuestPushReason.OnQuest);
                            receiver.SendPushToPartyResponse(sender, QuestPushReason.OnQuestToRecipient, quest);

                            continue;
                        }
                    default:
                        break;
                }

                if (!receiver.SatisfyQuestLog(false))
                {
                    sender.SendPushToPartyResponse(receiver, QuestPushReason.LogFull);
                    receiver.SendPushToPartyResponse(sender, QuestPushReason.LogFullToRecipient, quest);

                    continue;
                }

                if (!receiver.SatisfyQuestDay(quest, false))
                {
                    sender.SendPushToPartyResponse(receiver, QuestPushReason.AlreadyDone);
                    receiver.SendPushToPartyResponse(sender, QuestPushReason.AlreadyDoneToRecipient, quest);

                    continue;
                }

                if (!receiver.SatisfyQuestMinLevel(quest, false))
                {
                    sender.SendPushToPartyResponse(receiver, QuestPushReason.LowLevel);
                    receiver.SendPushToPartyResponse(sender, QuestPushReason.LowLevelToRecipient, quest);

                    continue;
                }

                if (!receiver.SatisfyQuestMaxLevel(quest, false))
                {
                    sender.SendPushToPartyResponse(receiver, QuestPushReason.HighLevel);
                    receiver.SendPushToPartyResponse(sender, QuestPushReason.HighLevelToRecipient, quest);

                    continue;
                }

                if (!receiver.SatisfyQuestClass(quest, false))
                {
                    sender.SendPushToPartyResponse(receiver, QuestPushReason.Class);
                    receiver.SendPushToPartyResponse(sender, QuestPushReason.ClassToRecipient, quest);

                    continue;
                }

                if (!receiver.SatisfyQuestRace(quest, false))
                {
                    sender.SendPushToPartyResponse(receiver, QuestPushReason.Race);
                    receiver.SendPushToPartyResponse(sender, QuestPushReason.RaceToRecipient, quest);

                    continue;
                }

                if (!receiver.SatisfyQuestReputation(quest, false))
                {
                    sender.SendPushToPartyResponse(receiver, QuestPushReason.LowFaction);
                    receiver.SendPushToPartyResponse(sender, QuestPushReason.LowFactionToRecipient, quest);

                    continue;
                }

                if (!receiver.SatisfyQuestDependentQuests(quest, false))
                {
                    sender.SendPushToPartyResponse(receiver, QuestPushReason.Prerequisite);
                    receiver.SendPushToPartyResponse(sender, QuestPushReason.PrerequisiteToRecipient, quest);

                    continue;
                }

                if (!receiver.SatisfyQuestExpansion(quest, false))
                {
                    sender.SendPushToPartyResponse(receiver, QuestPushReason.Expansion);
                    receiver.SendPushToPartyResponse(sender, QuestPushReason.ExpansionToRecipient, quest);

                    continue;
                }

                if (!receiver.CanTakeQuest(quest, false))
                {
                    sender.SendPushToPartyResponse(receiver, QuestPushReason.Invalid);
                    receiver.SendPushToPartyResponse(sender, QuestPushReason.InvalidToRecipient, quest);

                    continue;
                }

                sender.SendPushToPartyResponse(receiver, QuestPushReason.Success);

                if ((quest.IsAutoComplete() && quest.IsRepeatable() && !quest.IsDailyOrWeekly()) ||
                    quest.HasFlag(QuestFlags.AutoComplete))
                {
                    receiver.PlayerTalkClass.SendQuestGiverRequestItems(quest, sender.GetGUID(), receiver.CanCompleteRepeatableQuest(quest), true);
                }
                else
                {
                    receiver.SetQuestSharingInfo(sender.GetGUID(), quest.Id);
                    receiver.PlayerTalkClass.SendQuestGiverQuestDetails(quest, receiver.GetGUID(), true, false);

                    if (quest.IsAutoAccept() &&
                        receiver.CanAddQuest(quest, true) &&
                        receiver.CanTakeQuest(quest, true))
                    {
                        receiver.AddQuestAndCheckCompletion(quest, sender);
                        sender.SendPushToPartyResponse(receiver, QuestPushReason.Accepted);
                        receiver.ClearQuestSharingInfo();
                    }
                }
            }
        }

        [WorldPacketHandler(ClientOpcodes.QuestPushResult)]
        private void HandleQuestPushResult(QuestPushResult packet)
        {
            if (!_player.GetPlayerSharingQuest().IsEmpty())
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
        private void HandleQuestgiverStatusMultipleQuery(QuestGiverStatusMultipleQuery packet)
        {
            _player.SendQuestGiverStatusMultiple();
        }

        [WorldPacketHandler(ClientOpcodes.QuestGiverStatusTrackedQuery)]
        void HandleQuestgiverStatusTrackedQueryOpcode(QuestGiverStatusTrackedQuery questGiverStatusTrackedQuery)
        {
            _player.SendQuestGiverStatusMultiple(questGiverStatusTrackedQuery.QuestGiverGUIDs);
        }

        [WorldPacketHandler(ClientOpcodes.RequestWorldQuestUpdate)]
        private void HandleRequestWorldQuestUpdate(RequestWorldQuestUpdate packet)
        {
            WorldQuestUpdateResponse response = new();

            // @todo: 7.x Has to be implemented
            //response.WorldQuestUpdates.push_back(WorldPackets::Quest::WorldQuestUpdateInfo(lastUpdate, questID, timer, variableID, value));

            SendPacket(response);
        }

        [WorldPacketHandler(ClientOpcodes.ChoiceResponse)]
        private void HandlePlayerChoiceResponse(ChoiceResponse choiceResponse)
        {
            if (_player.PlayerTalkClass.GetInteractionData().PlayerChoiceId != choiceResponse.ChoiceID)
            {
                Log.outError(LogFilter.Player, $"Error in CMSG_CHOICE_RESPONSE: {GetPlayerInfo()} tried to respond to invalid player choice {choiceResponse.ChoiceID} (allowed {_player.PlayerTalkClass.GetInteractionData().PlayerChoiceId}) (possible packet-hacking detected)");

                return;
            }

            PlayerChoice playerChoice = Global.ObjectMgr.GetPlayerChoice(choiceResponse.ChoiceID);

            if (playerChoice == null)
                return;

            PlayerChoiceResponse playerChoiceResponse = playerChoice.GetResponseByIdentifier(choiceResponse.ResponseIdentifier);

            if (playerChoiceResponse == null)
            {
                Log.outError(LogFilter.Player, $"Error in CMSG_CHOICE_RESPONSE: {GetPlayerInfo()} tried to select invalid player choice response {choiceResponse.ResponseIdentifier} (possible packet-hacking detected)");

                return;
            }

            Global.ScriptMgr.ForEach<IPlayerOnPlayerChoiceResponse>(p => p.OnPlayerChoiceResponse(_player, (uint)choiceResponse.ChoiceID, (uint)choiceResponse.ResponseIdentifier));

            if (playerChoiceResponse.Reward != null)
            {
                var reward = playerChoiceResponse.Reward;

                if (reward.TitleId != 0)
                    _player.SetTitle(CliDB.CharTitlesStorage.LookupByKey(reward.TitleId), false);

                if (reward.PackageId != 0)
                    _player.RewardQuestPackage((uint)reward.PackageId);

                if (reward.SkillLineId != 0 &&
                    _player.HasSkill((SkillType)reward.SkillLineId))
                    _player.UpdateSkillPro((uint)reward.SkillLineId, 1000, reward.SkillPointCount);

                if (reward.HonorPointCount != 0)
                    _player.AddHonorXP(reward.HonorPointCount);

                if (reward.Money != 0)
                    _player.ModifyMoney((long)reward.Money, false);

                if (reward.Xp != 0)
                    _player.GiveXP(reward.Xp, null, 0.0f);

                foreach (PlayerChoiceResponseRewardItem item in reward.Items)
                {
                    List<ItemPosCount> dest = new();

                    if (_player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, item.Id, (uint)item.Quantity) == InventoryResult.Ok)
                    {
                        Item newItem = _player.StoreNewItem(dest, item.Id, true, ItemEnchantmentManager.GenerateItemRandomBonusListId(item.Id), null, ItemContext.QuestReward, item.BonusListIDs);
                        _player.SendNewItem(newItem, (uint)item.Quantity, true, false);
                    }
                }

                foreach (PlayerChoiceResponseRewardEntry currency in reward.Currency)
                    _player.ModifyCurrency(currency.Id, currency.Quantity);

                foreach (PlayerChoiceResponseRewardEntry faction in reward.Faction)
                    _player.GetReputationMgr().ModifyReputation(CliDB.FactionStorage.LookupByKey(faction.Id), faction.Quantity);
            }
        }
    }
}