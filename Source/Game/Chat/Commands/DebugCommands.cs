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

using Framework.Constants;
using Framework.IO;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.Combat;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Chat
{
    [CommandGroup("debug", RBACPermissions.CommandDebug, true)]
    internal class DebugCommands
    {
        [Command("anim", RBACPermissions.CommandDebugAnim)]
        private static bool HandleDebugAnimCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            var animId = args.NextUInt32();
            var unit = handler.GetSelectedUnit();
            if (unit)
                unit.HandleEmoteCommand((Emote)animId);
            return true;
        }

        [Command("areatriggers", RBACPermissions.CommandDebugAreatriggers)]
        private static bool HandleDebugAreaTriggersCommand(StringArguments args, CommandHandler handler)
        {
            var player = handler.GetSession().GetPlayer();
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

        [Command("arena", RBACPermissions.CommandDebugArena, true)]
        private static bool HandleDebugArenaCommand(StringArguments args, CommandHandler handler)
        {
            Global.BattlegroundMgr.ToggleArenaTesting();
            return true;
        }

        [Command("bg", RBACPermissions.CommandDebugBg, true)]
        private static bool HandleDebugBattlegroundCommand(StringArguments args, CommandHandler handler)
        {
            Global.BattlegroundMgr.ToggleTesting();
            return true;
        }

        [Command("boundary", RBACPermissions.CommandDebugBoundary)]
        private static bool HandleDebugBoundaryCommand(StringArguments args, CommandHandler handler)
        {
            var player = handler.GetSession().GetPlayer();
            if (!player)
                return false;

            var target = handler.GetSelectedCreature();
            if (!target || !target.IsAIEnabled || target.GetAI() == null)
                return false;

            var fill_str = args.NextString();
            var duration_str = args.NextString();

            if (!int.TryParse(duration_str, out var duration))
                duration = -1;
            if (duration <= 0 || duration >= 30 * Time.Minute) // arbitary upper limit
                duration = 3 * Time.Minute;

            var doFill = fill_str.ToLower().Equals("fill");

            var errMsg = target.GetAI().VisualizeBoundary(duration, player, doFill);
            if (errMsg > 0)
                handler.SendSysMessage(errMsg);

            return true;
        }

        [Command("conversation", RBACPermissions.CommandDebugConversation)]
        private static bool HandleDebugConversationCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            var conversationEntry = args.NextUInt32();
            if (conversationEntry == 0)
                return false;

            var target = handler.GetSelectedPlayerOrSelf();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            return Conversation.CreateConversation(conversationEntry, target, target, new List<ObjectGuid>() { target.GetGUID() }) != null;
        }

        [Command("entervehicle", RBACPermissions.CommandDebugEntervehicle)]
        private static bool HandleDebugEnterVehicleCommand(StringArguments args, CommandHandler handler)
        {
            var target = handler.GetSelectedUnit();
            if (!target || !target.IsVehicle())
                return false;

            if (args.Empty())
                return false;

            var entry = args.NextUInt32();
            if (!sbyte.TryParse(args.NextString(), out var seatId))
                seatId = -1;

            if (entry == 0)
                handler.GetSession().GetPlayer().EnterVehicle(target, seatId);
            else
            {
                var check = new AllCreaturesOfEntryInRange(handler.GetSession().GetPlayer(), entry, 20.0f);
                var searcher = new CreatureSearcher(handler.GetSession().GetPlayer(), check);
                Cell.VisitAllObjects(handler.GetSession().GetPlayer(), searcher, 30.0f);
                var passenger = searcher.GetTarget();
                if (!passenger || passenger == target)
                    return false;
                passenger.EnterVehicle(target, seatId);
            }

            handler.SendSysMessage("Unit {0} entered vehicle {1}", entry, seatId);
            return true;
        }

        [Command("getitemstate", RBACPermissions.CommandDebugGetitemstate)]
        private static bool HandleDebugGetItemStateCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            var itemState = args.NextString();

            var state = ItemUpdateState.Unchanged;
            var listQueue = false;
            var checkAll = false;

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

            var player = handler.GetSelectedPlayer();
            if (!player)
                player = handler.GetSession().GetPlayer();

            if (!listQueue && !checkAll)
            {
                itemState = "The player has the following " + itemState + " items: ";
                handler.SendSysMessage(itemState);
                for (byte i = (int)PlayerSlots.Start; i < (int)PlayerSlots.End; ++i)
                {
                    if (i >= InventorySlots.BuyBackStart && i < InventorySlots.BuyBackEnd)
                        continue;

                    var item = player.GetItemByPos(InventorySlots.Bag0, i);
                    if (item)
                    {
                        var bag = item.ToBag();
                        if (bag)
                        {
                            for (byte j = 0; j < bag.GetBagSize(); ++j)
                            {
                                var item2 = bag.GetItemByPos(j);
                                if (item2)
                                    if (item2.GetState() == state)
                                        handler.SendSysMessage("bag: 255 slot: {0} guid: {1} owner: {2}", item2.GetSlot(), item2.GetGUID().ToString(), item2.GetOwnerGUID().ToString());
                            }
                        }
                        else if (item.GetState() == state)
                            handler.SendSysMessage("bag: 255 slot: {0} guid: {1} owner: {2}", item.GetSlot(), item.GetGUID().ToString(), item.GetOwnerGUID().ToString());
                    }
                }
            }

            if (listQueue)
            {
                var updateQueue = player.ItemUpdateQueue;
                for (var i = 0; i < updateQueue.Count; ++i)
                {
                    var item = updateQueue[i];
                    if (!item)
                        continue;

                    var container = item.GetContainer();
                    var bagSlot = container ? container.GetSlot() : InventorySlots.Bag0;

                    var st = "";
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

                    handler.SendSysMessage("bag: {0} slot: {1} guid: {2} - state: {3}", bagSlot, item.GetSlot(), item.GetGUID().ToString(), st);
                }
                if (updateQueue.Empty())
                    handler.SendSysMessage("The player's updatequeue is empty");
            }

            if (checkAll)
            {
                var error = false;
                var updateQueue = player.ItemUpdateQueue;
                for (byte i = (int)PlayerSlots.Start; i < (int)PlayerSlots.End; ++i)
                {
                    if (i >= InventorySlots.BuyBackStart && i < InventorySlots.BuyBackEnd)
                        continue;

                    var item = player.GetItemByPos(InventorySlots.Bag0, i);
                    if (!item)
                        continue;

                    if (item.GetSlot() != i)
                    {
                        handler.SendSysMessage("Item with slot {0} and guid {1} has an incorrect slot value: {2}", i, item.GetGUID().ToString(), item.GetSlot());
                        error = true;
                        continue;
                    }

                    if (item.GetOwnerGUID() != player.GetGUID())
                    {
                        handler.SendSysMessage("The item with slot {0} and itemguid {1} does have non-matching owner guid ({2}) and player guid ({3}) !", item.GetSlot(), item.GetGUID().ToString(), item.GetOwnerGUID().ToString(), player.GetGUID().ToString());
                        error = true;
                        continue;
                    }

                    var container = item.GetContainer();
                    if (container)
                    {
                        handler.SendSysMessage("The item with slot {0} and guid {1} has a container (slot: {2}, guid: {3}) but shouldn't!", item.GetSlot(), item.GetGUID().ToString(), container.GetSlot(), container.GetGUID().ToString());
                        error = true;
                        continue;
                    }

                    if (item.IsInUpdateQueue())
                    {
                        var qp = (ushort)item.GetQueuePos();
                        if (qp > updateQueue.Count)
                        {
                            handler.SendSysMessage("The item with slot {0} and guid {1} has its queuepos ({2}) larger than the update queue size! ", item.GetSlot(), item.GetGUID().ToString(), qp);
                            error = true;
                            continue;
                        }

                        if (updateQueue[qp] == null)
                        {
                            handler.SendSysMessage("The item with slot {0} and guid {1} has its queuepos ({2}) pointing to NULL in the queue!", item.GetSlot(), item.GetGUID().ToString(), qp);
                            error = true;
                            continue;
                        }

                        if (updateQueue[qp] != item)
                        {
                            handler.SendSysMessage("The item with slot {0} and guid {1} has a queuepos ({2}) that points to another item in the queue (bag: {3}, slot: {4}, guid: {5})", item.GetSlot(), item.GetGUID().ToString(), qp, updateQueue[qp].GetBagSlot(), updateQueue[qp].GetSlot(), updateQueue[qp].GetGUID().ToString());
                            error = true;
                            continue;
                        }
                    }
                    else if (item.GetState() != ItemUpdateState.Unchanged)
                    {
                        handler.SendSysMessage("The item with slot {0} and guid {1} is not in queue but should be (state: {2})!", item.GetSlot(), item.GetGUID().ToString(), item.GetState());
                        error = true;
                        continue;
                    }

                    var bag = item.ToBag();
                    if (bag)
                    {
                        for (byte j = 0; j < bag.GetBagSize(); ++j)
                        {
                            var item2 = bag.GetItemByPos(j);
                            if (!item2)
                                continue;

                            if (item2.GetSlot() != j)
                            {
                                handler.SendSysMessage("The item in bag {0} and slot {1} (guid: {2}) has an incorrect slot value: {3}", bag.GetSlot(), j, item2.GetGUID().ToString(), item2.GetSlot());
                                error = true;
                                continue;
                            }

                            if (item2.GetOwnerGUID() != player.GetGUID())
                            {
                                handler.SendSysMessage("The item in bag {0} at slot {1} and with itemguid {2}, the owner's guid ({3}) and the player's guid ({4}) don't match!", bag.GetSlot(), item2.GetSlot(), item2.GetGUID().ToString(), item2.GetOwnerGUID().ToString(), player.GetGUID().ToString());
                                error = true;
                                continue;
                            }

                            var container1 = item2.GetContainer();
                            if (!container1)
                            {
                                handler.SendSysMessage("The item in bag {0} at slot {1} with guid {2} has no container!", bag.GetSlot(), item2.GetSlot(), item2.GetGUID().ToString());
                                error = true;
                                continue;
                            }

                            if (container1 != bag)
                            {
                                handler.SendSysMessage("The item in bag {0} at slot {1} with guid {2} has a different container(slot {3} guid {4})!", bag.GetSlot(), item2.GetSlot(), item2.GetGUID().ToString(), container1.GetSlot(), container1.GetGUID().ToString());
                                error = true;
                                continue;
                            }

                            if (item2.IsInUpdateQueue())
                            {
                                var qp = (ushort)item2.GetQueuePos();
                                if (qp > updateQueue.Count)
                                {
                                    handler.SendSysMessage("The item in bag {0} at slot {1} having guid {2} has a queuepos ({3}) larger than the update queue size! ", bag.GetSlot(), item2.GetSlot(), item2.GetGUID().ToString(), qp);
                                    error = true;
                                    continue;
                                }

                                if (updateQueue[qp] == null)
                                {
                                    handler.SendSysMessage("The item in bag {0} at slot {1} having guid {2} has a queuepos ({3}) that points to NULL in the queue!", bag.GetSlot(), item2.GetSlot(), item2.GetGUID().ToString(), qp);
                                    error = true;
                                    continue;
                                }

                                if (updateQueue[qp] != item2)
                                {
                                    handler.SendSysMessage("The item in bag {0} at slot {1} having guid {2} has a queuepos ({3}) that points to another item in the queue (bag: {4}, slot: {5}, guid: {6})", bag.GetSlot(), item2.GetSlot(), item2.GetGUID().ToString(), qp, updateQueue[qp].GetBagSlot(), updateQueue[qp].GetSlot(), updateQueue[qp].GetGUID().ToString());
                                    error = true;
                                    continue;
                                }
                            }
                            else if (item2.GetState() != ItemUpdateState.Unchanged)
                            {
                                handler.SendSysMessage("The item in bag {0} at slot {1} having guid {2} is not in queue but should be (state: {3})!", bag.GetSlot(), item2.GetSlot(), item2.GetGUID().ToString(), item2.GetState());
                                error = true;
                                continue;
                            }
                        }
                    }
                }

                for (var i = 0; i < updateQueue.Count; ++i)
                {
                    var item = updateQueue[i];
                    if (!item)
                        continue;

                    if (item.GetOwnerGUID() != player.GetGUID())
                    {
                        handler.SendSysMessage("queue({0}): For the item with guid {0}, the owner's guid ({1}) and the player's guid ({2}) don't match!", i, item.GetGUID().ToString(), item.GetOwnerGUID().ToString(), player.GetGUID().ToString());
                        error = true;
                        continue;
                    }

                    if (item.GetQueuePos() != i)
                    {
                        handler.SendSysMessage("queue({0}): For the item with guid {1}, the queuepos doesn't match it's position in the queue!", i, item.GetGUID().ToString());
                        error = true;
                        continue;
                    }

                    if (item.GetState() == ItemUpdateState.Removed)
                        continue;

                    var test = player.GetItemByPos(item.GetBagSlot(), item.GetSlot());

                    if (test == null)
                    {
                        handler.SendSysMessage("queue({0}): The bag({1}) and slot({2}) values for the item with guid {3} are incorrect, the player doesn't have any item at that position!", i, item.GetBagSlot(), item.GetSlot(), item.GetGUID().ToString());
                        error = true;
                        continue;
                    }

                    if (test != item)
                    {
                        handler.SendSysMessage("queue({0}): The bag({1}) and slot({2}) values for the item with guid {3} are incorrect, an item which guid is {4} is there instead!", i, item.GetBagSlot(), item.GetSlot(), item.GetGUID().ToString(), test.GetGUID().ToString());
                        error = true;
                        continue;
                    }
                }
                if (!error)
                    handler.SendSysMessage("All OK!");
            }

            return true;
        }

        [Command("combat", RBACPermissions.CommandDebugCombat)]
        private static bool HandleDebugCombatListCommand(StringArguments args, CommandHandler handler)
        {
            var target = handler.GetSelectedUnit();
            if (!target)
                target = handler.GetSession().GetPlayer();
            var refe = target.GetHostileRefManager().GetFirst();
            uint count = 0;
            handler.SendSysMessage("Hostil reference list of {0} (guid {1})", target.GetName(), target.GetGUID().ToString());
            while (refe != null)
            {
                var unit = refe.GetSource().GetOwner();
                if (unit)
                {
                    ++count;
                    handler.SendSysMessage("   {0}.   {1}   ({2}, SpawnId: {3})  - threat {4}", count, unit.GetName(), unit.GetGUID().ToString(), unit.IsTypeId(TypeId.Unit) ? unit.ToCreature().GetSpawnId() : 0, refe.GetThreat());
                }
                refe = refe.Next();
            }
            handler.SendSysMessage("End of hostil reference list.");
            return true;
        }

        [Command("itemexpire", RBACPermissions.CommandDebugItemexpire)]
        private static bool HandleDebugItemExpireCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            if (!ulong.TryParse(args.NextString(), out var guid))
                return false;

            var item = handler.GetSession().GetPlayer().GetItemByGuid(ObjectGuid.Create(HighGuid.Item, guid));
            if (!item)
                return false;

            handler.GetSession().GetPlayer().DestroyItem(item.GetBagSlot(), item.GetSlot(), true);
            Global.ScriptMgr.OnItemExpire(handler.GetSession().GetPlayer(), item.GetTemplate());

            return true;
        }

        [Command("lootrecipient", RBACPermissions.CommandDebugLootrecipient)]
        private static bool HandleDebugGetLootRecipientCommand(StringArguments args, CommandHandler handler)
        {
            var target = handler.GetSelectedCreature();
            if (!target)
                return false;

            handler.SendSysMessage("Loot recipient for creature {0} (GUID {1}, DB GUID {2}) is {3}", target.GetName(), target.GetGUID().ToString(), target.GetSpawnId(),
                target.HasLootRecipient() ? (target.GetLootRecipient() ? target.GetLootRecipient().GetName() : "offline") : "no loot recipient");
            return true;
        }

        [Command("los", RBACPermissions.CommandDebugLos)]
        private static bool HandleDebugLoSCommand(StringArguments args, CommandHandler handler)
        {
            var unit = handler.GetSelectedUnit();
            if (unit)
            {
                var player = handler.GetSession().GetPlayer();
                handler.SendSysMessage($"Checking LoS {player.GetName()} -> {unit.GetName()}:");
                handler.SendSysMessage($"    VMAP LoS: {(player.IsWithinLOSInMap(unit, LineOfSightChecks.Vmap) ? "clear" : "obstructed")}");
                handler.SendSysMessage($"    GObj LoS: {(player.IsWithinLOSInMap(unit, LineOfSightChecks.Gobject) ? "clear" : "obstructed")}");
                handler.SendSysMessage($"{unit.GetName()} is {(player.IsWithinLOSInMap(unit) ? "" : "not ")}in line of sight of {player.GetName()}.");
                return true;
            }
            return false;
        }

        [Command("moveflags", RBACPermissions.CommandDebugMoveflags)]
        private static bool HandleDebugMoveflagsCommand(StringArguments args, CommandHandler handler)
        {
            var target = handler.GetSelectedUnit();
            if (!target)
                target = handler.GetSession().GetPlayer();

            if (args.Empty())
            {
                //! Display case
                handler.SendSysMessage(CypherStrings.MoveflagsGet, target.GetUnitMovementFlags(), target.GetUnitMovementFlags2());
            }
            else
            {
                var mask1 = args.NextString();
                if (string.IsNullOrEmpty(mask1))
                    return false;

                var mask2 = args.NextString(" \n");

                if (!uint.TryParse(mask1, out var moveFlags))
                    return false;
                target.SetUnitMovementFlags((MovementFlag)moveFlags);

                // @fixme: port master's HandleDebugMoveflagsCommand; flags need different handling

                if (!string.IsNullOrEmpty(mask2))
                {
                    if (!uint.TryParse(mask2, out var moveFlagsExtra))
                        return false;
                    target.SetUnitMovementFlags2((MovementFlag2)moveFlagsExtra);
                }

                if (!target.IsTypeId(TypeId.Player))
                    target.DestroyForNearbyPlayers();  // Force new SMSG_UPDATE_OBJECT:CreateObject
                else
                {
                    var moveUpdate = new MoveUpdate();
                    moveUpdate.Status = target.m_movementInfo;
                    target.SendMessageToSet(moveUpdate, true);
                }

                handler.SendSysMessage(CypherStrings.MoveflagsSet, target.GetUnitMovementFlags(), target.GetUnitMovementFlags2());
            }

            return true;
        }

        [Command("neargraveyard", RBACPermissions.CommandNearGraveyard)]
        private static bool HandleDebugNearGraveyard(StringArguments args, CommandHandler handler)
        {
            var player = handler.GetSession().GetPlayer();
            WorldSafeLocsEntry nearestLoc = null;

            if (args.NextString().Equals("linked"))
            {
                var bg = player.GetBattleground();
                if (bg)
                    nearestLoc = bg.GetClosestGraveYard(player);
                else
                {
                    var bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(player.GetZoneId());
                    if (bf != null)
                        nearestLoc = bf.GetClosestGraveYard(player);
                    else
                        nearestLoc = Global.ObjectMgr.GetClosestGraveYard(player, player.GetTeam(), player);
                }
            }
            else
            {
                var x = player.GetPositionX();
                var y = player.GetPositionY();
                var z = player.GetPositionZ();
                var distNearest = float.MaxValue;

                foreach (var pair in Global.ObjectMgr.GetWorldSafeLocs())
                {
                    var worldSafe = pair.Value;
                    if (worldSafe.Loc.GetMapId() == player.GetMapId())
                    {
                        var dist = (worldSafe.Loc.GetPositionX() - x) * (worldSafe.Loc.GetPositionX() - x) + (worldSafe.Loc.GetPositionY() - y) * (worldSafe.Loc.GetPositionY() - y) + (worldSafe.Loc.GetPositionZ() - z) * (worldSafe.Loc.GetPositionZ() - z);
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

        [Command("loadcells", RBACPermissions.CommandDebugLoadcells)]
        private static bool HandleDebugLoadCellsCommand(StringArguments args, CommandHandler handler)
        {
            var player = handler.GetSession().GetPlayer();
            if (!player)
                return false;

            Map map = null;

            if (!args.Empty())
            {
                var mapId = args.NextUInt32();
                map = Global.MapMgr.FindBaseNonInstanceMap(mapId);
            }
            if (!map)
                map = player.GetMap();

            map.LoadAllCells();

            handler.SendSysMessage("Cells loaded (mapId: {0})", map.GetId());
            return true;
        }

        [Command("phase", RBACPermissions.CommandDebugPhase)]
        private static bool HandleDebugPhaseCommand(StringArguments args, CommandHandler handler)
        {
            var target = handler.GetSelectedUnit();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);
                return false;
            }

            if (target.GetDBPhase() > 0)
                handler.SendSysMessage($"Target creature's PhaseId in DB: {target.GetDBPhase()}");
            else if (target.GetDBPhase() < 0)
                handler.SendSysMessage($"Target creature's PhaseGroup in DB: {Math.Abs(target.GetDBPhase())}");

            PhasingHandler.PrintToChat(handler, target.GetPhaseShift());
            return true;
        }

        [Command("raidreset", RBACPermissions.CommandInstanceUnbind)]
        private static bool HandleDebugRaidResetCommand(StringArguments args, CommandHandler handler)
        {
            if (!int.TryParse(args.NextString(), out var map) || map <= 0)
                return false;

            var mapId = (uint)map;
            var mEntry = CliDB.MapStorage.LookupByKey(mapId);
            if (mEntry == null)
            {
                handler.SendSysMessage("Invalid map specified.");
                return false;
            }

            if (!mEntry.IsDungeon())
            {
                handler.SendSysMessage($"'{mEntry.MapName[handler.GetSessionDbcLocale()]}' is not a dungeon map.");
                return true;
            }

            if (!int.TryParse(args.NextString(), out var difficulty) || difficulty < -1)
                return false;

            if (CliDB.DifficultyStorage.HasRecord((uint)difficulty))
            {
                handler.SendSysMessage($"Invalid difficulty {difficulty}.");
                return false;
            }

            if (difficulty >= 0 && Global.DB2Mgr.GetMapDifficultyData(mEntry.Id, (Difficulty)difficulty) == null)
            {
                handler.SendSysMessage($"Difficulty {(Difficulty)difficulty} is not valid for '{mEntry.MapName[handler.GetSessionDbcLocale()]}'.");
                return true;
            }

            if (difficulty == -1)
            {
                handler.SendSysMessage($"Resetting all difficulties for '{mEntry.MapName[handler.GetSessionDbcLocale()]}'.");
                foreach (var diff in CliDB.DifficultyStorage.Values)
                {
                    if (Global.DB2Mgr.GetMapDifficultyData(mapId, (Difficulty)diff.Id) != null)
                    {
                        handler.SendSysMessage($"Resetting difficulty {diff.Id} for '{mEntry.MapName[handler.GetSessionDbcLocale()]}'.");
                        Global.InstanceSaveMgr.ForceGlobalReset(mapId, (Difficulty)diff.Id);
                    }
                }
            }
            else if (mEntry.IsNonRaidDungeon() && difficulty == (int)Difficulty.Normal)
            {
                handler.SendSysMessage($"'{mEntry.MapName[handler.GetSessionDbcLocale()]}' does not have any permanent saves for difficulty {(Difficulty)difficulty}.");
            }
            else
            {
                handler.SendSysMessage($"Resetting difficulty {(Difficulty)difficulty} for '{mEntry.MapName[handler.GetSessionDbcLocale()]}'.");
                Global.InstanceSaveMgr.ForceGlobalReset(mapId, (Difficulty)difficulty);
            }

            return true;
        }

        [Command("setaurastate", RBACPermissions.CommandDebugSetaurastate)]
        private static bool HandleDebugSetAuraStateCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            var unit = handler.GetSelectedUnit();
            if (!unit)
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);
                return false;
            }

            var state = args.NextInt32();
            if (state == 0)
            {
                // reset all states
                for (var i = 1; i <= 32; ++i)
                    unit.ModifyAuraState((AuraStateType)i, false);
                return true;
            }

            unit.ModifyAuraState((AuraStateType)Math.Abs(state), state > 0);
            return true;
        }

        [Command("setvid", RBACPermissions.CommandDebugSetvid)]
        private static bool HandleDebugSetVehicleIdCommand(StringArguments args, CommandHandler handler)
        {
            var target = handler.GetSelectedUnit();
            if (!target || target.IsVehicle())
                return false;

            if (args.Empty())
                return false;

            var id = args.NextUInt32();
            handler.SendSysMessage("Vehicle id set to {0}", id);
            return true;
        }

        [Command("spawnvehicle", RBACPermissions.CommandDebugSpawnvehicle)]
        private static bool HandleDebugSpawnVehicleCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            var entry = args.NextUInt32();
            if (entry == 0)
                return false;

            float x, y, z, o = handler.GetSession().GetPlayer().GetOrientation();
            handler.GetSession().GetPlayer().GetClosePoint(out x, out y, out z, handler.GetSession().GetPlayer().GetCombatReach());

            var id = args.NextUInt32();
            if (id == 0)
                return handler.GetSession().GetPlayer().SummonCreature(entry, x, y, z, o);

            var creatureTemplate = Global.ObjectMgr.GetCreatureTemplate(entry);
            if (creatureTemplate == null)
                return false;

            var vehicleRecord = CliDB.VehicleStorage.LookupByKey(id);
            if (vehicleRecord == null)
                return false;

            var map = handler.GetSession().GetPlayer().GetMap();
            var pos = new Position(x, y, z, o);

            var creature = Creature.CreateCreature(entry, map, pos, id);
            if (!creature)
                return false;

            map.AddToMap(creature);
            return true;
        }

        [Command("threat", RBACPermissions.CommandDebugThreat)]
        private static bool HandleDebugThreatListCommand(StringArguments args, CommandHandler handler)
        {
            var target = handler.GetSelectedCreature();
            if (!target || target.IsTotem() || target.IsPet())
                return false;

            var threatList = target.GetThreatManager().GetThreatList();
            uint count = 0;
            handler.SendSysMessage("Threat list of {0} (guid {1})", target.GetName(), target.GetGUID().ToString());
            foreach (var refe in threatList)
            {
                var unit = refe.GetTarget();
                if (!unit)
                    continue;
                ++count;
                handler.SendSysMessage("   {0}.   {1}   (guid {2})  - threat {3}", count, unit.GetName(), unit.GetGUID().ToString(), refe.GetThreat());
            }
            handler.SendSysMessage("End of threat list.");
            return true;
        }

        [Command("transport", RBACPermissions.CommandDebugTransport)]
        private static bool HandleDebugTransportCommand(StringArguments args, CommandHandler handler)
        {
            var transport = handler.GetSession().GetPlayer().GetTransport();
            if (!transport)
                return false;

            var start = false;
            var arg1 = args.NextString();
            if (arg1 == "stop")
                transport.EnableMovement(false);
            else if (arg1 == "start")
            {
                transport.EnableMovement(true);
                start = true;
            }
            else
            {
                var pos = transport.GetPosition();
                handler.SendSysMessage("Transport {0} is {1}", transport.GetName(), transport.GetGoState() == GameObjectState.Ready ? "stopped" : "moving");
                handler.SendSysMessage("Transport position: {0}", pos.ToString());
                return true;
            }

            handler.SendSysMessage("Transport {0} {1}", transport.GetName(), start ? "started" : "stopped");
            return true;
        }

        [Command("worldstate", RBACPermissions.CommandDebugWorldState)]
        private static bool HandleDebugUpdateWorldStateCommand(StringArguments args, CommandHandler handler)
        {
            if (!uint.TryParse(args.NextString(), out var variable) || variable == 0)
                return false;

            var state = args.NextUInt32();
            handler.GetSession().GetPlayer().SendUpdateWorldState(variable, state);
            return true;
        }

        [Command("worldstate", RBACPermissions.CommandDebug)]
        private static bool HandleDebugWorldStateCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            var worldStateIdStr = args.NextString();
            var valueStr = args.NextString();

            if (worldStateIdStr.IsEmpty())
                return false;

            var target = handler.GetSelectedPlayerOrSelf();
            if (target == null)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            var worldStateId = uint.Parse(worldStateIdStr);
            var value = valueStr.IsEmpty() ? 0 : uint.Parse(valueStr);

            if (value != 0)
            {
                Global.WorldMgr.SetWorldState(worldStateId, value);
                target.SendUpdateWorldState(worldStateId, value);
            }
            else
                handler.SendSysMessage($"Worldstate {worldStateId} actual value : {Global.WorldMgr.GetWorldState(worldStateId)}");

            return true;
        }

        [CommandNonGroup("wpgps", RBACPermissions.CommandWpgps)]
        private static bool HandleWPGPSCommand(StringArguments args, CommandHandler handler)
        {
            var player = handler.GetSession().GetPlayer();

            Log.outInfo(LogFilter.SqlDev, "(@PATH, XX, {0}, {1}, {2}, 0, 0, 0, 100, 0),", player.GetPositionX(), player.GetPositionY(), player.GetPositionZ());

            handler.SendSysMessage("Waypoint SQL written to SQL Developer log");
            return true;
        }

        [Command("wsexpression", RBACPermissions.CommandDebug)]
        private static bool HandleDebugWSExpressionCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            var expressionIdStr = args.NextString();

            if (expressionIdStr.IsEmpty())
                return false;

            var expressionId = uint.Parse(expressionIdStr);

            var target = handler.GetSelectedPlayerOrSelf();
            if (target == null)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            var wsExpressionEntry = CliDB.WorldStateExpressionStorage.LookupByKey(expressionId);
            if (wsExpressionEntry == null)
                return false;

            if (ConditionManager.IsPlayerMeetingExpression(target, wsExpressionEntry))
                handler.SendSysMessage($"Expression {expressionId} meet");
            else
                handler.SendSysMessage($"Expression {expressionId} not meet");

            return true;
        }

        [CommandGroup("play", RBACPermissions.CommandDebugPlay)]
        private class PlayCommands
        {
            [Command("cinematic", RBACPermissions.CommandDebugPlayCinematic)]
            private static bool HandleDebugPlayCinematicCommand(StringArguments args, CommandHandler handler)
            {
                // USAGE: .debug play cinematic #cinematicid
                // #cinematicid - ID decimal number from CinemaicSequences.dbc (1st column)
                if (args.Empty())
                {
                    handler.SendSysMessage(CypherStrings.BadValue);
                    return false;
                }

                var cinematicId = args.NextUInt32();

                var cineSeq = CliDB.CinematicSequencesStorage.LookupByKey(cinematicId);
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
                    foreach (var cam in list)
                    {
                        handler.SendSysMessage("{0} - {1}ms [{2}, {3}, {4}] Facing {5} ({6} degrees)", count, cam.timeStamp, cam.locations.X, cam.locations.Y, cam.locations.Z, cam.locations.W, cam.locations.W * (180 / Math.PI));
                        count++;
                    }
                    handler.SendSysMessage("{0} waypoints dumped", list.Count);
                }

                handler.GetSession().GetPlayer().SendCinematicStart(cinematicId);
                return true;
            }

            [Command("movie", RBACPermissions.CommandDebugPlayMovie)]
            private static bool HandleDebugPlayMovieCommand(StringArguments args, CommandHandler handler)
            {
                // USAGE: .debug play movie #movieid
                // #movieid - ID decimal number from Movie.dbc (1st column)
                if (args.Empty())
                {
                    handler.SendSysMessage(CypherStrings.BadValue);
                    return false;
                }

                var movieId = args.NextUInt32();

                if (!CliDB.MovieStorage.ContainsKey(movieId))
                {
                    handler.SendSysMessage(CypherStrings.MovieNotExist, movieId);
                    return false;
                }

                handler.GetSession().GetPlayer().SendMovieStart(movieId);
                return true;
            }

            [Command("music", RBACPermissions.CommandDebugPlayMusic)]
            private static bool HandleDebugPlayMusicCommand(StringArguments args, CommandHandler handler)
            {
                // USAGE: .debug play music #musicId
                // #musicId - ID decimal number from SoundEntries.dbc (1st column)
                if (args.Empty())
                {
                    handler.SendSysMessage(CypherStrings.BadValue);
                    return false;
                }

                var musicId = args.NextUInt32();
                if (!CliDB.SoundKitStorage.ContainsKey(musicId))
                {
                    handler.SendSysMessage(CypherStrings.SoundNotExist, musicId);
                    return false;
                }

                var player = handler.GetSession().GetPlayer();

                player.PlayDirectMusic(musicId, player);

                handler.SendSysMessage(CypherStrings.YouHearSound, musicId);
                return true;
            }

            [Command("sound", RBACPermissions.CommandDebugPlaySound)]
            private static bool HandleDebugPlaySoundCommand(StringArguments args, CommandHandler handler)
            {
                // USAGE: .debug playsound #soundid
                // #soundid - ID decimal number from SoundEntries.dbc (1st column)
                if (args.Empty())
                {
                    handler.SendSysMessage(CypherStrings.BadValue);

                    return false;
                }

                var soundId = args.NextUInt32();

                if (!CliDB.SoundKitStorage.ContainsKey(soundId))
                {
                    handler.SendSysMessage(CypherStrings.SoundNotExist, soundId);
                    return false;
                }

                var player = handler.GetSession().GetPlayer();

                var unit = handler.GetSelectedUnit();
                if (!unit)
                {
                    handler.SendSysMessage(CypherStrings.SelectCharOrCreature);
                    return false;
                }

                var broadcastTextId = args.NextUInt32();

                if (!player.GetTarget().IsEmpty())
                    unit.PlayDistanceSound(soundId, player);
                else
                    unit.PlayDirectSound(soundId, player, broadcastTextId);

                handler.SendSysMessage(CypherStrings.YouHearSound, soundId);
                return true;
            }
        }

        [CommandGroup("send", RBACPermissions.CommandDebugSend)]
        private class SendCommands
        {
            [Command("buyerror", RBACPermissions.CommandDebugSendBuyerror)]
            private static bool HandleDebugSendBuyErrorCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                var msg = (BuyResult)args.NextUInt32();
                handler.GetSession().GetPlayer().SendBuyError(msg, null, 0);
                return true;
            }

            [Command("channelnotify", RBACPermissions.CommandDebugSendChannelnotify)]
            private static bool HandleDebugSendChannelNotifyCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                var name = "test";
                var code = args.NextByte();
                var packet = new ChannelNotify();
                packet.Type = (ChatNotify)code;
                packet.Channel = name;
                handler.GetSession().SendPacket(packet);
                return true;
            }

            [Command("chatmessage", RBACPermissions.CommandDebugSendChatmessage)]
            private static bool HandleDebugSendChatMsgCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                var msg = "testtest";
                var type = args.NextByte();
                var data = new ChatPkt();
                data.Initialize((ChatMsg)type, Language.Universal, handler.GetSession().GetPlayer(), handler.GetSession().GetPlayer(), msg, 0, "chan");
                handler.GetSession().SendPacket(data);
                return true;
            }

            [Command("equiperror", RBACPermissions.CommandDebugSendEquiperror)]
            private static bool HandleDebugSendEquipErrorCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                var msg = (InventoryResult)args.NextUInt32();
                handler.GetSession().GetPlayer().SendEquipError(msg);
                return true;
            }

            [Command("largepacket", RBACPermissions.CommandDebugSendLargepacket)]
            private static bool HandleDebugSendLargePacketCommand(StringArguments args, CommandHandler handler)
            {
                const string stuffingString = "This is a dummy string to push the packet's size beyond 128000 bytes. ";
                var ss = new StringBuilder();
                while (ss.Length < 128000)
                    ss.Append(stuffingString);
                handler.SendSysMessage(ss.ToString());
                return true;
            }

            [Command("opcode", RBACPermissions.CommandDebugSendOpcode)]
            private static bool HandleDebugSendOpcodeCommand(StringArguments args, CommandHandler handler)
            {
                handler.SendSysMessage(CypherStrings.NoCmd);
                return true;
            }

            [Command("playerchoice", RBACPermissions.CommandDebugSendPlayerChoice)]
            private static bool HandleDebugSendPlayerChoiceCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                var choiceId = args.NextInt32();
                var player = handler.GetSession().GetPlayer();

                player.SendPlayerChoice(player.GetGUID(), choiceId);
                return true;
            }

            [Command("qpartymsg", RBACPermissions.CommandDebugSendQpartymsg)]
            private static bool HandleDebugSendQuestPartyMsgCommand(StringArguments args, CommandHandler handler)
            {
                var msg = args.NextUInt32();
                handler.GetSession().GetPlayer().SendPushToPartyResponse(handler.GetSession().GetPlayer(), (QuestPushReason)msg);
                return true;
            }

            [Command("qinvalidmsg", RBACPermissions.CommandDebugSendQinvalidmsg)]
            private static bool HandleDebugSendQuestInvalidMsgCommand(StringArguments args, CommandHandler handler)
            {
                var msg = (QuestFailedReasons)args.NextUInt32();
                handler.GetSession().GetPlayer().SendCanTakeQuestResponse(msg);
                return true;
            }

            [Command("sellerror", RBACPermissions.CommandDebugSendSellerror)]
            private static bool HandleDebugSendSellErrorCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                var msg = (SellResult)args.NextUInt32();
                handler.GetSession().GetPlayer().SendSellError(msg, null, ObjectGuid.Empty);
                return true;
            }

            [Command("setphaseshift", RBACPermissions.CommandDebugSendSetphaseshift)]
            private static bool HandleDebugSendSetPhaseShiftCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                var phaseShift = new PhaseShift();

                if (uint.TryParse(args.NextString(), out var terrain))
                    phaseShift.AddVisibleMapId(terrain, null);

                if (uint.TryParse(args.NextString(), out var phase))
                    phaseShift.AddPhase(phase, PhaseFlags.None, null);

                if (uint.TryParse(args.NextString(), out var map))
                    phaseShift.AddUiMapPhaseId(map);

                PhasingHandler.SendToPlayer(handler.GetSession().GetPlayer(), phaseShift);
                return true;
            }

            [Command("spellfail", RBACPermissions.CommandDebugSendSpellfail)]
            private static bool HandleDebugSendSpellFailCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                if (!byte.TryParse(args.NextString(), out var failNum) || failNum == 0)
                    return false;

                var failArg1 = args.NextInt32();
                var failArg2 = args.NextInt32();

                var castFailed = new CastFailed();
                castFailed.CastID = ObjectGuid.Empty;
                castFailed.SpellID = 133;
                castFailed.Reason = (SpellCastResult)failNum;
                castFailed.FailedArg1 = failArg1;
                castFailed.FailedArg2 = failArg2;
                handler.GetSession().SendPacket(castFailed);

                return true;
            }
        }
    }
}
