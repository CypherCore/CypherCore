// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Framework.Constants;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.Combat;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Maps.Checks;
using Game.Maps.Notifiers;
using Game.Networking.Packets;
using Game.Scripting.Interfaces.IItem;
using Game.Spells;

namespace Game.Chat
{
    [CommandGroup("debug")]
    internal class DebugCommands
    {
        [CommandGroup("asan")]
        private class DebugAsanCommands
        {
            [Command("memoryleak", RBACPermissions.CommandDebug, true)]
            private static bool HandleDebugMemoryLeak(CommandHandler handler)
            {
                return true;
            }

            [Command("outofbounds", RBACPermissions.CommandDebug, true)]
            private static bool HandleDebugOutOfBounds(CommandHandler handler)
            {
                return true;
            }
        }

        [CommandGroup("play")]
        private class DebugPlayCommands
        {
            [Command("cinematic", RBACPermissions.CommandDebug)]
            private static bool HandleDebugPlayCinematicCommand(CommandHandler handler, uint cinematicId)
            {
                CinematicSequencesRecord cineSeq = CliDB.CinematicSequencesStorage.LookupByKey(cinematicId);

                if (cineSeq == null)
                {
                    handler.SendSysMessage(CypherStrings.CinematicNotExist, cinematicId);

                    return false;
                }

                // Dump camera locations
                var list = M2Storage.GetFlyByCameras(cineSeq.Camera[0]);

                if (list != null)
                {
                    handler.SendSysMessage("Waypoints for sequence {0}, camera {1}", cinematicId, cineSeq.Camera[0]);
                    uint count = 1;

                    foreach (FlyByCamera cam in list)
                    {
                        handler.SendSysMessage("{0} - {1}ms [{2}, {3}, {4}] Facing {5} ({6} degrees)", count, cam.timeStamp, cam.locations.X, cam.locations.Y, cam.locations.Z, cam.locations.W, cam.locations.W * (180 / Math.PI));
                        count++;
                    }

                    handler.SendSysMessage("{0} waypoints dumped", list.Count);
                }

                handler.GetPlayer().SendCinematicStart(cinematicId);

                return true;
            }

            [Command("movie", RBACPermissions.CommandDebug)]
            private static bool HandleDebugPlayMovieCommand(CommandHandler handler, uint movieId)
            {
                if (!CliDB.MovieStorage.ContainsKey(movieId))
                {
                    handler.SendSysMessage(CypherStrings.MovieNotExist, movieId);

                    return false;
                }

                handler.GetPlayer().SendMovieStart(movieId);

                return true;
            }

            [Command("music", RBACPermissions.CommandDebug)]
            private static bool HandleDebugPlayMusicCommand(CommandHandler handler, uint musicId)
            {
                if (!CliDB.SoundKitStorage.ContainsKey(musicId))
                {
                    handler.SendSysMessage(CypherStrings.SoundNotExist, musicId);

                    return false;
                }

                Player player = handler.GetPlayer();

                player.PlayDirectMusic(musicId, player);

                handler.SendSysMessage(CypherStrings.YouHearSound, musicId);

                return true;
            }

            [Command("sound", RBACPermissions.CommandDebug)]
            private static bool HandleDebugPlaySoundCommand(CommandHandler handler, uint soundId, uint broadcastTextId)
            {
                if (!CliDB.SoundKitStorage.ContainsKey(soundId))
                {
                    handler.SendSysMessage(CypherStrings.SoundNotExist, soundId);

                    return false;
                }

                Player player = handler.GetPlayer();

                Unit unit = handler.GetSelectedUnit();

                if (!unit)
                {
                    handler.SendSysMessage(CypherStrings.SelectCharOrCreature);

                    return false;
                }

                if (!player.GetTarget().IsEmpty())
                    unit.PlayDistanceSound(soundId, player);
                else
                    unit.PlayDirectSound(soundId, player, broadcastTextId);

                handler.SendSysMessage(CypherStrings.YouHearSound, soundId);

                return true;
            }
        }

        [CommandGroup("pvp")]
        private class DebugPvpCommands
        {
            [Command("warmode", RBACPermissions.CommandDebug)]
            private static bool HandleDebugWarModeFactionBalanceCommand(CommandHandler handler, string command, int rewardValue = 0)
            {
                // USAGE: .debug pvp fb <alliance|horde|neutral|off> [pct]
                // neutral     Sets faction balance off.
                // alliance    Set faction balance to alliance.
                // horde       Set faction balance to horde.
                // off         Reset the faction balance and use the calculated value of it
                switch (command.ToLower())
                {
                    default: // workaround for Variant of only ExactSequences not being supported
                        handler.SendSysMessage(CypherStrings.BadValue);

                        return false;
                    case "alliance":
                        Global.WorldMgr.SetForcedWarModeFactionBalanceState(TeamId.Alliance, rewardValue);

                        break;
                    case "horde":
                        Global.WorldMgr.SetForcedWarModeFactionBalanceState(TeamId.Horde, rewardValue);

                        break;
                    case "neutral":
                        Global.WorldMgr.SetForcedWarModeFactionBalanceState(TeamId.Neutral);

                        break;
                    case "off":
                        Global.WorldMgr.DisableForcedWarModeFactionBalanceState();

                        break;
                }

                return true;
            }
        }

        [CommandGroup("send")]
        private class DebugSendCommands
        {
            [Command("buyerror", RBACPermissions.CommandDebug)]
            private static bool HandleDebugSendBuyErrorCommand(CommandHandler handler, BuyResult error)
            {
                handler.GetPlayer().SendBuyError(error, null, 0);

                return true;
            }

            [Command("channelnotify", RBACPermissions.CommandDebug)]
            private static bool HandleDebugSendChannelNotifyCommand(CommandHandler handler, ChatNotify type)
            {
                ChannelNotify packet = new();
                packet.Type = type;
                packet.Channel = "test";
                handler.GetSession().SendPacket(packet);

                return true;
            }

            [Command("chatmessage", RBACPermissions.CommandDebug)]
            private static bool HandleDebugSendChatMsgCommand(CommandHandler handler, ChatMsg type)
            {
                ChatPkt data = new();
                data.Initialize(type, Language.Universal, handler.GetPlayer(), handler.GetPlayer(), "testtest", 0, "chan");
                handler.GetSession().SendPacket(data);

                return true;
            }

            [Command("equiperror", RBACPermissions.CommandDebug)]
            private static bool HandleDebugSendEquipErrorCommand(CommandHandler handler, InventoryResult error)
            {
                handler.GetPlayer().SendEquipError(error);

                return true;
            }

            [Command("largepacket", RBACPermissions.CommandDebug)]
            private static bool HandleDebugSendLargePacketCommand(CommandHandler handler)
            {
                StringBuilder ss = new();

                while (ss.Length < 128000)
                    ss.Append("This is a dummy string to push the packet's size beyond 128000 bytes. ");

                handler.SendSysMessage(ss.ToString());

                return true;
            }

            [Command("opcode", RBACPermissions.CommandDebug)]
            private static bool HandleDebugSendOpcodeCommand(CommandHandler handler)
            {
                handler.SendSysMessage(CypherStrings.CmdInvalid);

                return true;
            }

            [Command("playerchoice", RBACPermissions.CommandDebug)]
            private static bool HandleDebugSendPlayerChoiceCommand(CommandHandler handler, int choiceId)
            {
                Player player = handler.GetPlayer();
                player.SendPlayerChoice(player.GetGUID(), choiceId);

                return true;
            }

            [Command("qpartymsg", RBACPermissions.CommandDebug)]
            private static bool HandleDebugSendQuestPartyMsgCommand(CommandHandler handler, QuestPushReason msg)
            {
                handler.GetPlayer().SendPushToPartyResponse(handler.GetPlayer(), msg);

                return true;
            }

            [Command("qinvalidmsg", RBACPermissions.CommandDebug)]
            private static bool HandleDebugSendQuestInvalidMsgCommand(CommandHandler handler, QuestFailedReasons msg)
            {
                handler.GetPlayer().SendCanTakeQuestResponse(msg);

                return true;
            }

            [Command("sellerror", RBACPermissions.CommandDebug)]
            private static bool HandleDebugSendSellErrorCommand(CommandHandler handler, SellResult error)
            {
                handler.GetPlayer().SendSellError(error, null, ObjectGuid.Empty);

                return true;
            }

            [Command("setphaseshift", RBACPermissions.CommandDebug)]
            private static bool HandleDebugSendSetPhaseShiftCommand(CommandHandler handler, uint phaseId, uint visibleMapId, uint uiMapPhaseId)
            {
                PhaseShift phaseShift = new();

                if (phaseId != 0)
                    phaseShift.AddPhase(phaseId, PhaseFlags.None, null);

                if (visibleMapId != 0)
                    phaseShift.AddVisibleMapId(visibleMapId, null);

                if (uiMapPhaseId != 0)
                    phaseShift.AddUiMapPhaseId(uiMapPhaseId);

                PhasingHandler.SendToPlayer(handler.GetPlayer(), phaseShift);

                return true;
            }

            [Command("spellfail", RBACPermissions.CommandDebug)]
            private static bool HandleDebugSendSpellFailCommand(CommandHandler handler, SpellCastResult result, int? failArg1, int? failArg2)
            {
                CastFailed castFailed = new();
                castFailed.CastID = ObjectGuid.Empty;
                castFailed.SpellID = 133;
                castFailed.Reason = result;
                castFailed.FailedArg1 = failArg1.GetValueOrDefault(-1);
                castFailed.FailedArg2 = failArg2.GetValueOrDefault(-1);
                handler.GetSession().SendPacket(castFailed);

                return true;
            }
        }

        [CommandGroup("warden")]
        private class DebugWardenCommands
        {
            [Command("Force", RBACPermissions.CommandDebug, true)]
            private static bool HandleDebugWardenForce(CommandHandler handler, ushort[] checkIds)
            {
                /*if (checkIds.Empty())
				    return false;

				Warden  warden = handler.GetSession().GetWarden();
				if (warden == null)
				{
				    handler.SendSysMessage("Warden system is not enabled");
				    return true;
				}

				size_t const nQueued = warden->DEBUG_ForceSpecificChecks(checkIds);
				handler->PSendSysMessage("%zu/%zu checks queued for your Warden, they should be sent over the next few minutes (depending on settings)", nQueued, checkIds.size());*/
                return true;
            }
        }

        [Command("anim", RBACPermissions.CommandDebug)]
        private static bool HandleDebugAnimCommand(CommandHandler handler, Emote emote)
        {
            Unit unit = handler.GetSelectedUnit();

            if (unit)
                unit.HandleEmoteCommand(emote);

            handler.SendSysMessage($"Playing Emote {emote}");

            return true;
        }

        [Command("areatriggers", RBACPermissions.CommandDebug)]
        private static bool HandleDebugAreaTriggersCommand(CommandHandler handler)
        {
            Player player = handler.GetPlayer();

            if (!player.IsDebugAreaTriggers)
            {
                handler.SendSysMessage(CypherStrings.DebugAreatriggerOn);
                player.IsDebugAreaTriggers = true;
            }
            else
            {
                handler.SendSysMessage(CypherStrings.DebugAreatriggerOff);
                player.IsDebugAreaTriggers = false;
            }

            return true;
        }

        [Command("arena", RBACPermissions.CommandDebug, true)]
        private static bool HandleDebugArenaCommand(CommandHandler handler)
        {
            Global.BattlegroundMgr.ToggleArenaTesting();

            return true;
        }

        [Command("bg", RBACPermissions.CommandDebug, true)]
        private static bool HandleDebugBattlegroundCommand(CommandHandler handler)
        {
            Global.BattlegroundMgr.ToggleTesting();

            return true;
        }

        [Command("boundary", RBACPermissions.CommandDebug)]
        private static bool HandleDebugBoundaryCommand(CommandHandler handler, string fill, uint durationArg)
        {
            Player player = handler.GetPlayer();

            if (!player)
                return false;

            Creature target = handler.GetSelectedCreature();

            if (!target ||
                !target.IsAIEnabled())
                return false;

            TimeSpan duration = durationArg != 0 ? TimeSpan.FromSeconds(durationArg) : TimeSpan.Zero;

            if (duration <= TimeSpan.Zero ||
                duration >= TimeSpan.FromMinutes(30)) // arbitrary upper limit
                duration = TimeSpan.FromMinutes(3);

            CypherStrings errMsg = target.GetAI().VisualizeBoundary(duration, player, fill == "fill");

            if (errMsg > 0)
                handler.SendSysMessage(errMsg);

            return true;
        }

        [Command("combat", RBACPermissions.CommandDebug)]
        private static bool HandleDebugCombatListCommand(CommandHandler handler)
        {
            Unit target = handler.GetSelectedUnit();

            if (!target)
                target = handler.GetPlayer();

            handler.SendSysMessage($"Combat refs: (Combat State: {target.IsInCombat()} | Manager State: {target.GetCombatManager().HasCombat()})");

            foreach (var refe in target.GetCombatManager().GetPvPCombatRefs())
            {
                Unit unit = refe.Value.GetOther(target);
                handler.SendSysMessage($"[PvP] {unit.GetName()} (SpawnID {(unit.IsCreature() ? unit.ToCreature().GetSpawnId() : 0)})");
            }

            foreach (var refe in target.GetCombatManager().GetPvECombatRefs())
            {
                Unit unit = refe.Value.GetOther(target);
                handler.SendSysMessage($"[PvE] {unit.GetName()} (SpawnID {(unit.IsCreature() ? unit.ToCreature().GetSpawnId() : 0)})");
            }

            return true;
        }

        [Command("conversation", RBACPermissions.CommandDebug)]
        private static bool HandleDebugConversationCommand(CommandHandler handler, uint conversationEntry)
        {
            Player target = handler.GetSelectedPlayerOrSelf();

            if (!target)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);

                return false;
            }

            return Conversation.CreateConversation(conversationEntry, target, target, target.GetGUID()) != null;
        }

        [Command("dummy", RBACPermissions.CommandDebug)]
        private static bool HandleDebugDummyCommand(CommandHandler handler)
        {
            handler.SendSysMessage("This command does nothing right now. Edit your local core (DebugCommands.cs) to make it do whatever you need for testing.");

            return true;
        }

        [Command("entervehicle", RBACPermissions.CommandDebug)]
        private static bool HandleDebugEnterVehicleCommand(CommandHandler handler, uint entry, sbyte seatId = -1)
        {
            Unit target = handler.GetSelectedUnit();

            if (!target ||
                !target.IsVehicle())
                return false;

            if (entry == 0)
            {
                handler.GetPlayer().EnterVehicle(target, seatId);
            }
            else
            {
                var check = new AllCreaturesOfEntryInRange(handler.GetPlayer(), entry, 20.0f);
                var searcher = new CreatureSearcher(handler.GetPlayer(), check);
                Cell.VisitAllObjects(handler.GetPlayer(), searcher, 30.0f);
                var passenger = searcher.GetTarget();

                if (!passenger ||
                    passenger == target)
                    return false;

                passenger.EnterVehicle(target, seatId);
            }

            handler.SendSysMessage("Unit {0} entered vehicle {1}", entry, seatId);

            return true;
        }

        [Command("getitemstate", RBACPermissions.CommandDebug)]
        private static bool HandleDebugGetItemStateCommand(CommandHandler handler, string itemState)
        {
            ItemUpdateState state = ItemUpdateState.Unchanged;
            bool listQueue = false;
            bool checkAll = false;

            if (itemState == "unchanged")
                state = ItemUpdateState.Unchanged;
            else if (itemState == "changed")
                state = ItemUpdateState.Changed;
            else if (itemState == "new")
                state = ItemUpdateState.New;
            else if (itemState == "removed")
                state = ItemUpdateState.Removed;
            else if (itemState == "queue")
                listQueue = true;
            else if (itemState == "check_all")
                checkAll = true;
            else
                return false;

            Player player = handler.GetSelectedPlayer();

            if (!player)
                player = handler.GetPlayer();

            if (!listQueue &&
                !checkAll)
            {
                itemState = "The player has the following " + itemState + " items: ";
                handler.SendSysMessage(itemState);

                for (byte i = (int)PlayerSlots.Start; i < (int)PlayerSlots.End; ++i)
                {
                    if (i >= InventorySlots.BuyBackStart &&
                        i < InventorySlots.BuyBackEnd)
                        continue;

                    Item item = player.GetItemByPos(InventorySlots.Bag0, i);

                    if (item)
                    {
                        Bag bag = item.ToBag();

                        if (bag)
                            for (byte j = 0; j < bag.GetBagSize(); ++j)
                            {
                                Item item2 = bag.GetItemByPos(j);

                                if (item2)
                                    if (item2.GetState() == state)
                                        handler.SendSysMessage("bag: 255 Slot: {0} Guid: {1} owner: {2}", item2.GetSlot(), item2.GetGUID().ToString(), item2.GetOwnerGUID().ToString());
                            }
                        else if (item.GetState() == state)
                            handler.SendSysMessage("bag: 255 Slot: {0} Guid: {1} owner: {2}", item.GetSlot(), item.GetGUID().ToString(), item.GetOwnerGUID().ToString());
                    }
                }
            }

            if (listQueue)
            {
                List<Item> updateQueue = player.ItemUpdateQueue;

                for (int i = 0; i < updateQueue.Count; ++i)
                {
                    Item item = updateQueue[i];

                    if (!item)
                        continue;

                    Bag container = item.GetContainer();
                    byte bagSlot = container ? container.GetSlot() : InventorySlots.Bag0;

                    string st = "";

                    switch (item.GetState())
                    {
                        case ItemUpdateState.Unchanged:
                            st = "unchanged";

                            break;
                        case ItemUpdateState.Changed:
                            st = "changed";

                            break;
                        case ItemUpdateState.New:
                            st = "new";

                            break;
                        case ItemUpdateState.Removed:
                            st = "removed";

                            break;
                    }

                    handler.SendSysMessage("bag: {0} Slot: {1} Guid: {2} - State: {3}", bagSlot, item.GetSlot(), item.GetGUID().ToString(), st);
                }

                if (updateQueue.Empty())
                    handler.SendSysMessage("The player's updatequeue is empty");
            }

            if (checkAll)
            {
                bool error = false;
                List<Item> updateQueue = player.ItemUpdateQueue;

                for (byte i = (int)PlayerSlots.Start; i < (int)PlayerSlots.End; ++i)
                {
                    if (i >= InventorySlots.BuyBackStart &&
                        i < InventorySlots.BuyBackEnd)
                        continue;

                    Item item = player.GetItemByPos(InventorySlots.Bag0, i);

                    if (!item)
                        continue;

                    if (item.GetSlot() != i)
                    {
                        handler.SendSysMessage("Item with Slot {0} and Guid {1} has an incorrect Slot value: {2}", i, item.GetGUID().ToString(), item.GetSlot());
                        error = true;

                        continue;
                    }

                    if (item.GetOwnerGUID() != player.GetGUID())
                    {
                        handler.SendSysMessage("The Item with Slot {0} and itemguid {1} does have non-matching owner Guid ({2}) and player Guid ({3}) !", item.GetSlot(), item.GetGUID().ToString(), item.GetOwnerGUID().ToString(), player.GetGUID().ToString());
                        error = true;

                        continue;
                    }

                    Bag container = item.GetContainer();

                    if (container)
                    {
                        handler.SendSysMessage("The Item with Slot {0} and Guid {1} has a container (Slot: {2}, Guid: {3}) but shouldn't!", item.GetSlot(), item.GetGUID().ToString(), container.GetSlot(), container.GetGUID().ToString());
                        error = true;

                        continue;
                    }

                    if (item.IsInUpdateQueue())
                    {
                        ushort qp = (ushort)item.GetQueuePos();

                        if (qp > updateQueue.Count)
                        {
                            handler.SendSysMessage("The Item with Slot {0} and Guid {1} has its queuepos ({2}) larger than the update queue size! ", item.GetSlot(), item.GetGUID().ToString(), qp);
                            error = true;

                            continue;
                        }

                        if (updateQueue[qp] == null)
                        {
                            handler.SendSysMessage("The Item with Slot {0} and Guid {1} has its queuepos ({2}) pointing to NULL in the queue!", item.GetSlot(), item.GetGUID().ToString(), qp);
                            error = true;

                            continue;
                        }

                        if (updateQueue[qp] != item)
                        {
                            handler.SendSysMessage("The Item with Slot {0} and Guid {1} has a queuepos ({2}) that points to another Item in the queue (bag: {3}, Slot: {4}, Guid: {5})", item.GetSlot(), item.GetGUID().ToString(), qp, updateQueue[qp].GetBagSlot(), updateQueue[qp].GetSlot(), updateQueue[qp].GetGUID().ToString());
                            error = true;

                            continue;
                        }
                    }
                    else if (item.GetState() != ItemUpdateState.Unchanged)
                    {
                        handler.SendSysMessage("The Item with Slot {0} and Guid {1} is not in queue but should be (State: {2})!", item.GetSlot(), item.GetGUID().ToString(), item.GetState());
                        error = true;

                        continue;
                    }

                    Bag bag = item.ToBag();

                    if (bag)
                        for (byte j = 0; j < bag.GetBagSize(); ++j)
                        {
                            Item item2 = bag.GetItemByPos(j);

                            if (!item2)
                                continue;

                            if (item2.GetSlot() != j)
                            {
                                handler.SendSysMessage("The Item in bag {0} and Slot {1} (Guid: {2}) has an incorrect Slot value: {3}", bag.GetSlot(), j, item2.GetGUID().ToString(), item2.GetSlot());
                                error = true;

                                continue;
                            }

                            if (item2.GetOwnerGUID() != player.GetGUID())
                            {
                                handler.SendSysMessage("The Item in bag {0} at Slot {1} and with itemguid {2}, the owner's Guid ({3}) and the player's Guid ({4}) don't match!", bag.GetSlot(), item2.GetSlot(), item2.GetGUID().ToString(), item2.GetOwnerGUID().ToString(), player.GetGUID().ToString());
                                error = true;

                                continue;
                            }

                            Bag container1 = item2.GetContainer();

                            if (!container1)
                            {
                                handler.SendSysMessage("The Item in bag {0} at Slot {1} with Guid {2} has no container!", bag.GetSlot(), item2.GetSlot(), item2.GetGUID().ToString());
                                error = true;

                                continue;
                            }

                            if (container1 != bag)
                            {
                                handler.SendSysMessage("The Item in bag {0} at Slot {1} with Guid {2} has a different container(Slot {3} Guid {4})!", bag.GetSlot(), item2.GetSlot(), item2.GetGUID().ToString(), container1.GetSlot(), container1.GetGUID().ToString());
                                error = true;

                                continue;
                            }

                            if (item2.IsInUpdateQueue())
                            {
                                ushort qp = (ushort)item2.GetQueuePos();

                                if (qp > updateQueue.Count)
                                {
                                    handler.SendSysMessage("The Item in bag {0} at Slot {1} having Guid {2} has a queuepos ({3}) larger than the update queue size! ", bag.GetSlot(), item2.GetSlot(), item2.GetGUID().ToString(), qp);
                                    error = true;

                                    continue;
                                }

                                if (updateQueue[qp] == null)
                                {
                                    handler.SendSysMessage("The Item in bag {0} at Slot {1} having Guid {2} has a queuepos ({3}) that points to NULL in the queue!", bag.GetSlot(), item2.GetSlot(), item2.GetGUID().ToString(), qp);
                                    error = true;

                                    continue;
                                }

                                if (updateQueue[qp] != item2)
                                {
                                    handler.SendSysMessage("The Item in bag {0} at Slot {1} having Guid {2} has a queuepos ({3}) that points to another Item in the queue (bag: {4}, Slot: {5}, Guid: {6})", bag.GetSlot(), item2.GetSlot(), item2.GetGUID().ToString(), qp, updateQueue[qp].GetBagSlot(), updateQueue[qp].GetSlot(), updateQueue[qp].GetGUID().ToString());
                                    error = true;

                                    continue;
                                }
                            }
                            else if (item2.GetState() != ItemUpdateState.Unchanged)
                            {
                                handler.SendSysMessage("The Item in bag {0} at Slot {1} having Guid {2} is not in queue but should be (State: {3})!", bag.GetSlot(), item2.GetSlot(), item2.GetGUID().ToString(), item2.GetState());
                                error = true;

                                continue;
                            }
                        }
                }

                for (int i = 0; i < updateQueue.Count; ++i)
                {
                    Item item = updateQueue[i];

                    if (!item)
                        continue;

                    if (item.GetOwnerGUID() != player.GetGUID())
                    {
                        handler.SendSysMessage("queue({0}): For the Item with Guid {0}, the owner's Guid ({1}) and the player's Guid ({2}) don't match!", i, item.GetGUID().ToString(), item.GetOwnerGUID().ToString(), player.GetGUID().ToString());
                        error = true;

                        continue;
                    }

                    if (item.GetQueuePos() != i)
                    {
                        handler.SendSysMessage("queue({0}): For the Item with Guid {1}, the queuepos doesn't match it's position in the queue!", i, item.GetGUID().ToString());
                        error = true;

                        continue;
                    }

                    if (item.GetState() == ItemUpdateState.Removed)
                        continue;

                    Item test = player.GetItemByPos(item.GetBagSlot(), item.GetSlot());

                    if (test == null)
                    {
                        handler.SendSysMessage("queue({0}): The bag({1}) and Slot({2}) values for the Item with Guid {3} are incorrect, the player doesn't have any Item at that position!", i, item.GetBagSlot(), item.GetSlot(), item.GetGUID().ToString());
                        error = true;

                        continue;
                    }

                    if (test != item)
                    {
                        handler.SendSysMessage("queue({0}): The bag({1}) and Slot({2}) values for the Item with Guid {3} are incorrect, an Item which Guid is {4} is there instead!", i, item.GetBagSlot(), item.GetSlot(), item.GetGUID().ToString(), test.GetGUID().ToString());
                        error = true;

                        continue;
                    }
                }

                if (!error)
                    handler.SendSysMessage("All OK!");
            }

            return true;
        }

        [Command("guidlimits", RBACPermissions.CommandDebug, true)]
        private static bool HandleDebugGuidLimitsCommand(CommandHandler handler, uint mapId)
        {
            if (mapId != 0)
                Global.MapMgr.DoForAllMapsWithMapId(mapId, map => HandleDebugGuidLimitsMap(handler, map));
            else
                Global.MapMgr.DoForAllMaps(map => HandleDebugGuidLimitsMap(handler, map));

            handler.SendSysMessage($"Guid Warn Level: {WorldConfig.GetIntValue(WorldCfg.RespawnGuidWarnLevel)}");
            handler.SendSysMessage($"Guid Alert Level: {WorldConfig.GetIntValue(WorldCfg.RespawnGuidAlertLevel)}");

            return true;
        }

        [Command("instancespawn", RBACPermissions.CommandDebug)]
        private static bool HandleDebugInstanceSpawns(CommandHandler handler, [VariantArg(typeof(uint), typeof(string))] object optArg)
        {
            Player player = handler.GetPlayer();

            if (player == null)
                return false;

            bool explain = false;
            uint groupID = 0;

            if (optArg is string &&
                (optArg as string).Equals("explain", StringComparison.OrdinalIgnoreCase))
                explain = true;
            else
                groupID = (uint)optArg;

            if (groupID != 0 &&
                Global.ObjectMgr.GetSpawnGroupData(groupID) == null)
            {
                handler.SendSysMessage($"There is no spawn group with ID {groupID}.");

                return false;
            }

            Map map = player.GetMap();
            string mapName = map.GetMapName();
            InstanceScript instance = player.GetInstanceScript();

            if (instance == null)
            {
                handler.SendSysMessage($"{mapName} has no instance script.");

                return false;
            }

            var spawnGroups = instance.GetInstanceSpawnGroups();

            if (spawnGroups.Empty())
            {
                handler.SendSysMessage($"{mapName}'s instance script does not manage any spawn groups.");

                return false;
            }

            MultiMap<uint, Tuple<bool, byte, byte>> store = new();

            foreach (InstanceSpawnGroupInfo info in spawnGroups)
            {
                if (groupID != 0 &&
                    info.SpawnGroupId != groupID)
                    continue;

                bool isSpawn;

                if (info.Flags.HasFlag(InstanceSpawnGroupFlags.BlockSpawn))
                    isSpawn = false;
                else if (info.Flags.HasFlag(InstanceSpawnGroupFlags.ActivateSpawn))
                    isSpawn = true;
                else
                    continue;

                store.Add(info.SpawnGroupId, Tuple.Create(isSpawn, info.BossStateId, info.BossStates));
            }

            if (groupID != 0 &&
                !store.ContainsKey(groupID))
            {
                handler.SendSysMessage($"{mapName}'s instance script does not manage group '{Global.ObjectMgr.GetSpawnGroupData(groupID).name}'.");

                return false;
            }

            if (groupID == 0)
                handler.SendSysMessage($"Spawn groups managed by {mapName} ({map.GetId()}):");

            foreach (var key in store.Keys)
            {
                SpawnGroupTemplateData groupData = Global.ObjectMgr.GetSpawnGroupData(key);
                Cypher.Assert(groupData != null); // checked by objectmgr on load

                if (explain)
                {
                    handler.SendSysMessage(" |-- '{}' ({})", groupData.name, key);
                    bool isBlocked = false, isSpawned = false;

                    foreach (var tuple in store[key])
                    {
                        bool isSpawn = tuple.Item1;
                        byte bossStateId = tuple.Item2;
                        EncounterState actualState = instance.GetBossState(bossStateId);

                        if ((tuple.Item3 & (1 << (int)actualState)) != 0)
                        {
                            if (isSpawn)
                            {
                                isSpawned = true;

                                if (isBlocked)
                                    handler.SendSysMessage($" | |-- '{groupData.name}' would be allowed to spawn by boss State {bossStateId} being {(EncounterState)actualState}, but this is overruled");
                                else
                                    handler.SendSysMessage($" | |-- '{groupData.name}' is allowed to spawn because boss State {bossStateId} is {(EncounterState)bossStateId}.");
                            }
                            else
                            {
                                isBlocked = true;
                                handler.SendSysMessage($" | |-- '{groupData.name}' is Blocked from spawning because boss State {bossStateId} is {(EncounterState)bossStateId}.");
                            }
                        }
                        else
                        {
                            handler.SendSysMessage($" | |-- '{groupData.name}' could've been {(isSpawn ? "allowed to spawn" : "Blocked from spawning")} if boss State {bossStateId} matched mask 0x{tuple.Item3:X2}; but it is {(EncounterState)actualState} . 0x{(1 << (int)actualState):X2}, which does not match.");
                        }
                    }

                    if (isBlocked)
                        handler.SendSysMessage($" | |=> '{groupData.name}' is not active due to a blocking rule being matched");
                    else if (isSpawned)
                        handler.SendSysMessage($" | |=> '{groupData.name}' is active due to a spawn rule being matched");
                    else
                        handler.SendSysMessage($" | |=> '{groupData.name}' is not active due to none of its rules being matched");
                }
                else
                {
                    handler.SendSysMessage($" - '{groupData.name}' ({key}) is {(map.IsSpawnGroupActive(key) ? "" : "not ")}active");
                }
            }

            return true;
        }

        [Command("itemexpire", RBACPermissions.CommandDebug)]
        private static bool HandleDebugItemExpireCommand(CommandHandler handler, ulong guid)
        {
            Item item = handler.GetPlayer().GetItemByGuid(ObjectGuid.Create(HighGuid.Item, guid));

            if (!item)
                return false;

            handler.GetPlayer().DestroyItem(item.GetBagSlot(), item.GetSlot(), true);
            var itemTemplate = item.GetTemplate();
            Global.ScriptMgr.RunScriptRet<IItemOnExpire>(p => p.OnExpire(handler.GetPlayer(), itemTemplate), itemTemplate.ScriptId);

            return true;
        }

        [Command("loadcells", RBACPermissions.CommandDebug, true)]
        private static bool HandleDebugLoadCellsCommand(CommandHandler handler, uint? mapId, uint? tileX, uint? tileY)
        {
            if (mapId.HasValue)
            {
                Global.MapMgr.DoForAllMapsWithMapId(mapId.Value, map => HandleDebugLoadCellsCommandHelper(handler, map, tileX, tileY));

                return true;
            }

            Player player = handler.GetPlayer();

            if (player != null)
                // Fallback to player's map if no map has been specified
                return HandleDebugLoadCellsCommandHelper(handler, player.GetMap(), tileX, tileY);

            return false;
        }

        private static bool HandleDebugLoadCellsCommandHelper(CommandHandler handler, Map map, uint? tileX, uint? tileY)
        {
            if (!map)
                return false;

            // Load 1 single tile if specified, otherwise load the whole map
            if (tileX.HasValue &&
                tileY.HasValue)
            {
                handler.SendSysMessage($"Loading cell (mapId: {map.GetId()} tile: {tileX}, {tileY}). Current GameObjects {map.GetObjectsStore().Count(p => p.Value is GameObject)}, Creatures {map.GetObjectsStore().Count(p => p.Value is Creature)}");

                // Some unit convertions to go from TileXY to GridXY to WorldXY
                float x = (((float)(64 - 1 - tileX.Value) - 0.5f - MapConst.CenterGridId) * MapConst.SizeofGrids) + (MapConst.CenterGridOffset * 2);
                float y = (((float)(64 - 1 - tileY.Value) - 0.5f - MapConst.CenterGridId) * MapConst.SizeofGrids) + (MapConst.CenterGridOffset * 2);
                map.LoadGrid(x, y);

                handler.SendSysMessage($"Cell loaded (mapId: {map.GetId()} tile: {tileX}, {tileY}) After load - GameObject {map.GetObjectsStore().Count(p => p.Value is GameObject)}, Creatures {map.GetObjectsStore().Count(p => p.Value is Creature)}");
            }
            else
            {
                handler.SendSysMessage($"Loading all cells (mapId: {map.GetId()}). Current GameObjects {map.GetObjectsStore().Count(p => p.Value is GameObject)}, Creatures {map.GetObjectsStore().Count(p => p.Value is Creature)}");

                map.LoadAllCells();

                handler.SendSysMessage($"Cells loaded (mapId: {map.GetId()}) After load - GameObject {map.GetObjectsStore().Count(p => p.Value is GameObject)}, Creatures {map.GetObjectsStore().Count(p => p.Value is Creature)}");
            }

            return true;
        }

        [Command("lootrecipient", RBACPermissions.CommandDebug)]
        private static bool HandleDebugGetLootRecipientCommand(CommandHandler handler)
        {
            Creature target = handler.GetSelectedCreature();

            if (!target)
                return false;

            handler.SendSysMessage($"Loot recipients for creature {target.GetName()} ({target.GetGUID()}, SpawnID {target.GetSpawnId()}) are:");

            foreach (ObjectGuid tapperGuid in target.GetTapList())
            {
                Player tapper = Global.ObjAccessor.GetPlayer(target, tapperGuid);
                handler.SendSysMessage($"* {(tapper != null ? tapper.GetName() : "offline")}");
            }

            return true;
        }

        [Command("los", RBACPermissions.CommandDebug)]
        private static bool HandleDebugLoSCommand(CommandHandler handler)
        {
            Unit unit = handler.GetSelectedUnit();

            if (unit)
            {
                Player player = handler.GetPlayer();
                handler.SendSysMessage($"Checking LoS {player.GetName()} . {unit.GetName()}:");
                handler.SendSysMessage($"    VMAP LoS: {(player.IsWithinLOSInMap(unit, LineOfSightChecks.Vmap) ? "clear" : "obstructed")}");
                handler.SendSysMessage($"    GObj LoS: {(player.IsWithinLOSInMap(unit, LineOfSightChecks.Gobject) ? "clear" : "obstructed")}");
                handler.SendSysMessage($"{unit.GetName()} is {(player.IsWithinLOSInMap(unit) ? "" : "not ")}in line of sight of {player.GetName()}.");

                return true;
            }

            return false;
        }

        [Command("moveflags", RBACPermissions.CommandDebug)]
        private static bool HandleDebugMoveflagsCommand(CommandHandler handler, uint? moveFlags, uint? moveFlagsExtra)
        {
            Unit target = handler.GetSelectedUnit();

            if (!target)
                target = handler.GetPlayer();

            if (!moveFlags.HasValue)
            {
                //! Display case
                handler.SendSysMessage(CypherStrings.MoveflagsGet, target.GetUnitMovementFlags(), target.GetUnitMovementFlags2());
            }
            else
            {
                // @fixme: port master's HandleDebugMoveflagsCommand; Flags need different handling

                target.SetUnitMovementFlags((MovementFlag)moveFlags);

                if (moveFlagsExtra.HasValue)
                    target.SetUnitMovementFlags2((MovementFlag2)moveFlagsExtra);

                if (!target.IsTypeId(TypeId.Player))
                {
                    target.DestroyForNearbyPlayers(); // Force new SMSG_UPDATE_OBJECT:CreateObject
                }
                else
                {
                    MoveUpdate moveUpdate = new();
                    moveUpdate.Status = target.MovementInfo;
                    target.SendMessageToSet(moveUpdate, true);
                }

                handler.SendSysMessage(CypherStrings.MoveflagsSet, target.GetUnitMovementFlags(), target.GetUnitMovementFlags2());
            }

            return true;
        }

        [Command("neargraveyard", RBACPermissions.CommandDebug)]
        private static bool HandleDebugNearGraveyard(CommandHandler handler, string linked)
        {
            Player player = handler.GetPlayer();
            WorldSafeLocsEntry nearestLoc = null;

            if (linked == "linked")
            {
                Battleground bg = player.GetBattleground();

                if (bg)
                {
                    nearestLoc = bg.GetClosestGraveYard(player);
                }
                else
                {
                    BattleField bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(player.GetMap(), player.GetZoneId());

                    if (bf != null)
                        nearestLoc = bf.GetClosestGraveYard(player);
                    else
                        nearestLoc = Global.ObjectMgr.GetClosestGraveYard(player, player.GetTeam(), player);
                }
            }
            else
            {
                float x = player.GetPositionX();
                float y = player.GetPositionY();
                float z = player.GetPositionZ();
                float distNearest = float.MaxValue;

                foreach (var pair in Global.ObjectMgr.GetWorldSafeLocs())
                {
                    var worldSafe = pair.Value;

                    if (worldSafe.Loc.GetMapId() == player.GetMapId())
                    {
                        float dist = (worldSafe.Loc.GetPositionX() - x) * (worldSafe.Loc.GetPositionX() - x) + (worldSafe.Loc.GetPositionY() - y) * (worldSafe.Loc.GetPositionY() - y) + (worldSafe.Loc.GetPositionZ() - z) * (worldSafe.Loc.GetPositionZ() - z);

                        if (dist < distNearest)
                        {
                            distNearest = dist;
                            nearestLoc = worldSafe;
                        }
                    }
                }
            }

            if (nearestLoc != null)
                handler.SendSysMessage(CypherStrings.CommandNearGraveyard, nearestLoc.Id, nearestLoc.Loc.GetPositionX(), nearestLoc.Loc.GetPositionY(), nearestLoc.Loc.GetPositionZ());
            else
                handler.SendSysMessage(CypherStrings.CommandNearGraveyardNotfound);

            return true;
        }

        [Command("objectcount", RBACPermissions.CommandDebug, true)]
        private static bool HandleDebugObjectCountCommand(CommandHandler handler, uint? mapId)
        {
            void HandleDebugObjectCountMap(Map map)
            {
                handler.SendSysMessage($"Map Id: {map.GetId()} Name: '{map.GetMapName()}' Instance Id: {map.GetInstanceId()} Creatures: {map.GetObjectsStore().OfType<Creature>().Count()} GameObjects: {map.GetObjectsStore().OfType<GameObject>().Count()} SetActive Objects: {map.GetActiveNonPlayersCount()}");

                Dictionary<uint, uint> creatureIds = new();

                foreach (var p in map.GetObjectsStore())
                    if (p.Value.IsCreature())
                    {
                        if (!creatureIds.ContainsKey(p.Value.GetEntry()))
                            creatureIds[p.Value.GetEntry()] = 0;

                        creatureIds[p.Value.GetEntry()]++;
                    }

                var orderedCreatures = creatureIds.OrderBy(p => p.Value).Where(p => p.Value > 5);

                handler.SendSysMessage("Top Creatures Count:");

                foreach (var p in orderedCreatures)
                    handler.SendSysMessage($"Entry: {p.Key} Count: {p.Value}");
            }

            if (mapId.HasValue)
                Global.MapMgr.DoForAllMapsWithMapId(mapId.Value, map => HandleDebugObjectCountMap(map));
            else
                Global.MapMgr.DoForAllMaps(map => HandleDebugObjectCountMap(map));

            return true;
        }

        [Command("phase", RBACPermissions.CommandDebug)]
        private static bool HandleDebugPhaseCommand(CommandHandler handler)
        {
            Unit target = handler.GetSelectedUnit();

            if (!target)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);

                return false;
            }

            if (target.GetDBPhase() > 0)
                handler.SendSysMessage($"Target creature's PhaseId in DB: {target.GetDBPhase()}");
            else if (target.GetDBPhase() < 0)
                handler.SendSysMessage($"Target creature's PhaseGroup in DB: {Math.Abs(target.GetDBPhase())}");

            PhasingHandler.PrintToChat(handler, target);

            return true;
        }

        [Command("pvp warmode", RBACPermissions.CommandDebug, true)]
        private static bool HandleDebugWarModeBalanceCommand(CommandHandler handler, string command, int? rewardValue)
        {
            // USAGE: .debug pvp fb <alliance|horde|neutral|off> [pct]
            // neutral     Sets faction balance off.
            // alliance    Set faction balance to alliance.
            // horde       Set faction balance to horde.
            // off         Reset the faction balance and use the calculated value of it
            switch (command)
            {
                case "alliance":
                    Global.WorldMgr.SetForcedWarModeFactionBalanceState(TeamId.Alliance, rewardValue.GetValueOrDefault(0));

                    break;
                case "horde":
                    Global.WorldMgr.SetForcedWarModeFactionBalanceState(TeamId.Horde, rewardValue.GetValueOrDefault(0));

                    break;
                case "neutral":
                    Global.WorldMgr.SetForcedWarModeFactionBalanceState(TeamId.Neutral);

                    break;
                case "off":
                    Global.WorldMgr.DisableForcedWarModeFactionBalanceState();

                    break;
                default:
                    handler.SendSysMessage(CypherStrings.BadValue);

                    return false;
            }

            return true;
        }

        [Command("questreset", RBACPermissions.CommandDebug)]
        private static bool HandleDebugQuestResetCommand(CommandHandler handler, string arg)
        {
            bool daily = false, weekly = false, monthly = false;

            if (arg == "ALL")
                daily = weekly = monthly = true;
            else if (arg == "DAILY")
                daily = true;
            else if (arg == "WEEKLY")
                weekly = true;
            else if (arg == "MONTHLY")
                monthly = true;
            else
                return false;

            long now = GameTime.GetGameTime();

            if (daily)
            {
                Global.WorldMgr.DailyReset();
                handler.SendSysMessage($"Daily quests have been reset. Next scheduled reset: {Time.UnixTimeToDateTime(Global.WorldMgr.GetPersistentWorldVariable(WorldManager.NEXT_DAILY_QUEST_RESET_TIME_VAR_ID)).ToShortTimeString()}");
            }

            if (weekly)
            {
                Global.WorldMgr.ResetWeeklyQuests();
                handler.SendSysMessage($"Weekly quests have been reset. Next scheduled reset: {Time.UnixTimeToDateTime(Global.WorldMgr.GetPersistentWorldVariable(WorldManager.NEXT_WEEKLY_QUEST_RESET_TIME_VAR_ID)).ToShortTimeString()}");
            }

            if (monthly)
            {
                Global.WorldMgr.ResetMonthlyQuests();
                handler.SendSysMessage($"Monthly quests have been reset. Next scheduled reset: {Time.UnixTimeToDateTime(Global.WorldMgr.GetPersistentWorldVariable(WorldManager.NEXT_MONTHLY_QUEST_RESET_TIME_VAR_ID)).ToShortTimeString()}");
            }

            return true;
        }

        [Command("raidreset", RBACPermissions.CommandDebug)]
        private static bool HandleDebugRaidResetCommand(CommandHandler handler, uint mapId, uint difficulty)
        {
            MapRecord mEntry = CliDB.MapStorage.LookupByKey(mapId);

            if (mEntry == null)
            {
                handler.SendSysMessage("Invalid map specified.");

                return true;
            }

            if (!mEntry.IsDungeon())
            {
                handler.SendSysMessage($"'{mEntry.MapName[handler.GetSessionDbcLocale()]}' is not a dungeon map.");

                return true;
            }

            if (difficulty != 0 &&
                CliDB.DifficultyStorage.HasRecord(difficulty))
            {
                handler.SendSysMessage($"Invalid difficulty {difficulty}.");

                return false;
            }

            if (difficulty != 0 &&
                Global.DB2Mgr.GetMapDifficultyData(mEntry.Id, (Difficulty)difficulty) == null)
            {
                handler.SendSysMessage($"Difficulty {(Difficulty)difficulty} is not valid for '{mEntry.MapName[handler.GetSessionDbcLocale()]}'.");

                return true;
            }

            return true;
        }

        [Command("setaurastate", RBACPermissions.CommandDebug)]
        private static bool HandleDebugSetAuraStateCommand(CommandHandler handler, AuraStateType? state, bool apply)
        {
            Unit unit = handler.GetSelectedUnit();

            if (!unit)
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);

                return false;
            }

            if (!state.HasValue)
            {
                // reset all states
                for (AuraStateType s = 0; s < AuraStateType.Max; ++s)
                    unit.ModifyAuraState(s, false);

                return true;
            }

            unit.ModifyAuraState(state.GetValueOrDefault(0), apply);

            return true;
        }

        [Command("spawnvehicle", RBACPermissions.CommandDebug)]
        private static bool HandleDebugSpawnVehicleCommand(CommandHandler handler, uint entry, uint id)
        {
            float x, y, z, o = handler.GetPlayer().GetOrientation();
            handler.GetPlayer().GetClosePoint(out x, out y, out z, handler.GetPlayer().GetCombatReach());

            if (id == 0)
                return handler.GetPlayer().SummonCreature(entry, x, y, z, o);

            CreatureTemplate creatureTemplate = Global.ObjectMgr.GetCreatureTemplate(entry);

            if (creatureTemplate == null)
                return false;

            VehicleRecord vehicleRecord = CliDB.VehicleStorage.LookupByKey(id);

            if (vehicleRecord == null)
                return false;

            Map map = handler.GetPlayer().GetMap();
            Position pos = new(x, y, z, o);

            Creature creature = Creature.CreateCreature(entry, map, pos, id);

            if (!creature)
                return false;

            map.AddToMap(creature);

            return true;
        }

        [Command("threat", RBACPermissions.CommandDebug)]
        private static bool HandleDebugThreatListCommand(CommandHandler handler)
        {
            Unit target = handler.GetSelectedUnit();

            if (target == null)
                target = handler.GetPlayer();

            ThreatManager mgr = target.GetThreatManager();

            if (!target.IsAlive())
            {
                handler.SendSysMessage($"{target.GetName()} ({target.GetGUID()}) is not alive.");

                return true;
            }

            uint count = 0;
            var threatenedByMe = target.GetThreatManager().GetThreatenedByMeList();

            if (threatenedByMe.Empty())
            {
                handler.SendSysMessage($"{target.GetName()} ({target.GetGUID()}) does not threaten any units.");
            }
            else
            {
                handler.SendSysMessage($"List of units threatened by {target.GetName()} ({target.GetGUID()})");

                foreach (var pair in threatenedByMe)
                {
                    Unit unit = pair.Value.GetOwner();
                    handler.SendSysMessage($"   {++count}.   {unit.GetName()}   ({unit.GetGUID()}, SpawnID {(unit.IsCreature() ? unit.ToCreature().GetSpawnId() : 0)})  - threat {pair.Value.GetThreat()}");
                }

                handler.SendSysMessage("End of threatened-by-me list.");
            }

            if (mgr.CanHaveThreatList())
            {
                if (!mgr.IsThreatListEmpty(true))
                {
                    if (target.IsEngaged())
                        handler.SendSysMessage($"Threat list of {target.GetName()} ({target.GetGUID()}, SpawnID {(target.IsCreature() ? target.ToCreature().GetSpawnId() : 0)}):");
                    else
                        handler.SendSysMessage($"{target.GetName()} ({target.GetGUID()}, SpawnID {(target.IsCreature() ? target.ToCreature().GetSpawnId() : 0)}) is not engaged, but still has a threat list? Well, here it is:");

                    count = 0;
                    Unit fixateVictim = mgr.GetFixateTarget();

                    foreach (ThreatReference refe in mgr.GetSortedThreatList())
                    {
                        Unit unit = refe.GetVictim();
                        handler.SendSysMessage($"   {++count}.   {unit.GetName()}   ({unit.GetGUID()})  - threat {refe.GetThreat()}[{(unit == fixateVictim ? "FIXATE" : refe.GetTauntState())}][{refe.GetOnlineState()}]");
                    }

                    handler.SendSysMessage("End of threat list.");
                }
                else if (!target.IsEngaged())
                {
                    handler.SendSysMessage($"{target.GetName()} ({target.GetGUID()}, SpawnID {(target.IsCreature() ? target.ToCreature().GetSpawnId() : 0)}) is not currently engaged.");
                }
                else
                {
                    handler.SendSysMessage($"{target.GetName()} ({target.GetGUID()}, SpawnID {(target.IsCreature() ? target.ToCreature().GetSpawnId() : 0)}) seems to be engaged, but does not have a threat list??");
                }
            }
            else if (target.IsEngaged())
            {
                handler.SendSysMessage($"{target.GetName()} ({target.GetGUID()}) is currently engaged. (This unit cannot have a threat list.)");
            }
            else
            {
                handler.SendSysMessage($"{target.GetName()} ({target.GetGUID()}) is not currently engaged. (This unit cannot have a threat list.)");
            }

            return true;
        }

        [Command("threatinfo", RBACPermissions.CommandDebug)]
        private static bool HandleDebugThreatInfoCommand(CommandHandler handler)
        {
            Unit target = handler.GetSelectedUnit();

            if (target == null)
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);

                return false;
            }

            handler.SendSysMessage($"Threat info for {target.GetName()} ({target.GetGUID()}):");

            ThreatManager mgr = target.GetThreatManager();

            // _singleSchoolModifiers
            {
                var mods = mgr.SingleSchoolModifiers;
                handler.SendSysMessage(" - Single-school threat modifiers:");
                handler.SendSysMessage($" |-- Physical: {mods[(int)SpellSchools.Normal] * 100.0f:0.##}");
                handler.SendSysMessage($" |-- Holy    : {mods[(int)SpellSchools.Holy] * 100.0f:0.##}");
                handler.SendSysMessage($" |-- Fire    : {mods[(int)SpellSchools.Fire] * 100.0f:0.##}");
                handler.SendSysMessage($" |-- Nature  : {mods[(int)SpellSchools.Nature] * 100.0f:0.##}");
                handler.SendSysMessage($" |-- Frost   : {mods[(int)SpellSchools.Frost] * 100.0f:0.##}");
                handler.SendSysMessage($" |-- Shadow  : {mods[(int)SpellSchools.Shadow] * 100.0f:0.##}");
                handler.SendSysMessage($" |-- Arcane  : {mods[(int)SpellSchools.Arcane] * 100.0f:0.##}");
            }

            // _multiSchoolModifiers
            {
                handler.SendSysMessage($"- Multi-school threat modifiers ({mgr.MultiSchoolModifiers.Count} entries):");

                lock (mgr.MultiSchoolModifiers)
                    foreach (var pair in mgr.MultiSchoolModifiers)
                        handler.SendSysMessage($" |-- Mask {pair.Key:X}: {pair.Value:0.XX}");
            }

            // _redirectInfo
            {
                var redirectInfo = mgr.RedirectInfo;

                if (redirectInfo.Empty())
                {
                    handler.SendSysMessage(" - No redirects being applied");
                }
                else
                {
                    handler.SendSysMessage($" - {redirectInfo.Count} redirects being applied:");

                    foreach (var pair in redirectInfo)
                    {
                        Unit unit = Global.ObjAccessor.GetUnit(target, pair.Item1);
                        handler.SendSysMessage($" |-- {pair.Item2:D2} to {(unit != null ? unit.GetName() : pair.Item1)}");
                    }
                }
            }

            // _redirectRegistry
            {
                var redirectRegistry = mgr.RedirectRegistry;

                if (redirectRegistry.Empty())
                {
                    handler.SendSysMessage(" - No redirects are registered");
                }
                else
                {
                    handler.SendSysMessage($" - {redirectRegistry.Count} spells may have redirects registered");

                    foreach (var outerPair in redirectRegistry) // (spellId, (Guid, pct))
                    {
                        SpellInfo spell = Global.SpellMgr.GetSpellInfo(outerPair.Key, Difficulty.None);
                        handler.SendSysMessage($" |-- #{outerPair.Key} {(spell != null ? spell.SpellName[Global.WorldMgr.GetDefaultDbcLocale()] : "<unknown>")} ({outerPair.Value.Count} entries):");

                        foreach (var innerPair in outerPair.Value) // (Guid, pct)
                        {
                            Unit unit = Global.ObjAccessor.GetUnit(target, innerPair.Key);
                            handler.SendSysMessage($"   |-- {innerPair.Value} to {(unit != null ? unit.GetName() : innerPair.Key)}");
                        }
                    }
                }
            }

            return true;
        }

        [Command("Transport", RBACPermissions.CommandDebug)]
        private static bool HandleDebugTransportCommand(CommandHandler handler, string operation)
        {
            Transport transport = handler.GetPlayer().GetTransport<Transport>();

            if (!transport)
                return false;

            bool start = false;

            if (operation == "stop")
            {
                transport.EnableMovement(false);
            }
            else if (operation == "start")
            {
                transport.EnableMovement(true);
                start = true;
            }
            else
            {
                Position pos = transport.GetPosition();
                handler.SendSysMessage("Transport {0} is {1}", transport.GetName(), transport.GetGoState() == GameObjectState.Ready ? "stopped" : "moving");
                handler.SendSysMessage("Transport position: {0}", pos.ToString());

                return true;
            }

            handler.SendSysMessage("Transport {0} {1}", transport.GetName(), start ? "started" : "stopped");

            return true;
        }

        [Command("warden Force", RBACPermissions.CommandDebug, true)]
        private static bool HandleDebugWardenForce(CommandHandler handler, ushort[] checkIds)
        {
            /*if (checkIds.Empty())
			    return false;

			Warden  warden = handler.GetSession().GetWarden();
			if (warden == null)
			{
			    handler.SendSysMessage("Warden system is not enabled");
			    return true;
			}

			size_t const nQueued = warden->DEBUG_ForceSpecificChecks(checkIds);
			handler->PSendSysMessage("%zu/%zu checks queued for your Warden, they should be sent over the next few minutes (depending on settings)", nQueued, checkIds.size());*/
            return true;
        }

        [Command("worldstate", RBACPermissions.CommandDebug)]
        private static bool HandleDebugUpdateWorldStateCommand(CommandHandler handler, uint variable, uint value)
        {
            handler.GetPlayer().SendUpdateWorldState(variable, value);

            return true;
        }

        [CommandNonGroup("wpgps", RBACPermissions.CommandDebug)]
        private static bool HandleWPGPSCommand(CommandHandler handler)
        {
            Player player = handler.GetPlayer();

            Log.outInfo(LogFilter.SqlDev, $"(@PATH, XX, {player.GetPositionX():3F}, {player.GetPositionY():3F}, {player.GetPositionZ():5F}, {player.GetOrientation():5F}, 0, 0, 0, 100, 0)");

            handler.SendSysMessage("Waypoint SQL written to SQL Developer log");

            return true;
        }

        [Command("wsexpression", RBACPermissions.CommandDebug)]
        private static bool HandleDebugWSExpressionCommand(CommandHandler handler, uint expressionId)
        {
            Player target = handler.GetSelectedPlayerOrSelf();

            if (target == null)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);

                return false;
            }

            WorldStateExpressionRecord wsExpressionEntry = CliDB.WorldStateExpressionStorage.LookupByKey(expressionId);

            if (wsExpressionEntry == null)
                return false;

            if (ConditionManager.IsPlayerMeetingExpression(target, wsExpressionEntry))
                handler.SendSysMessage($"Expression {expressionId} meet");
            else
                handler.SendSysMessage($"Expression {expressionId} not meet");

            return true;
        }

        private static void HandleDebugGuidLimitsMap(CommandHandler handler, Map map)
        {
            handler.SendSysMessage($"Map Id: {map.GetId()} Name: '{map.GetMapName()}' Instance Id: {map.GetInstanceId()} Highest Guid Creature: {map.GenerateLowGuid(HighGuid.Creature)} GameObject: {map.GetMaxLowGuid(HighGuid.GameObject)}");
        }
    }
}