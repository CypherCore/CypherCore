/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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
using Game.Network.Packets;
using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Chat
{
    [CommandGroup("debug", RBACPermissions.CommandDebug, true)]
    class DebugCommands
    {
        [Command("anim", RBACPermissions.CommandDebugAnim)]
        static bool HandleDebugAnimCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            uint animId = args.NextUInt32();
            Unit unit = handler.getSelectedUnit();
            if (unit)
                unit.HandleEmoteCommand((Emote)animId);
            return true;
        }

        [Command("areatriggers", RBACPermissions.CommandDebugAreatriggers)]
        static bool HandleDebugAreaTriggersCommand(StringArguments args, CommandHandler handler)
        {
            Player player = handler.GetSession().GetPlayer();
            if (!player.isDebugAreaTriggers)
            {
                handler.SendSysMessage(CypherStrings.DebugAreatriggerOn);
                player.isDebugAreaTriggers = true;
            }
            else
            {
                handler.SendSysMessage(CypherStrings.DebugAreatriggerOff);
                player.isDebugAreaTriggers = false;
            }
            return true;
        }

        [Command("arena", RBACPermissions.CommandDebugArena, true)]
        static bool HandleDebugArenaCommand(StringArguments args, CommandHandler handler)
        {
            Global.BattlegroundMgr.ToggleArenaTesting();
            return true;
        }

        [Command("bg", RBACPermissions.CommandDebugBg, true)]
        static bool HandleDebugBattlegroundCommand(StringArguments args, CommandHandler handler)
        {
            Global.BattlegroundMgr.ToggleTesting();
            return true;
        }

        [Command("boundary", RBACPermissions.CommandDebugBoundary)]
        static bool HandleDebugBoundaryCommand(StringArguments args, CommandHandler handler)
        {
            Player player = handler.GetSession().GetPlayer();
            if (!player)
                return false;

            Creature target = handler.getSelectedCreature();
            if (!target || !target.IsAIEnabled || target.GetAI() == null)
                return false;

            string fill_str = args.NextString();
            string duration_str = args.NextString();

            if (!int.TryParse(duration_str, out int duration))
                duration = -1;
            if (duration <= 0 || duration >= 30 * Time.Minute) // arbitary upper limit
                duration = 3 * Time.Minute;

            bool doFill = fill_str.ToLower().Equals("fill");

            CypherStrings errMsg = target.GetAI().VisualizeBoundary(duration, player, doFill);
            if (errMsg > 0)
                handler.SendSysMessage(errMsg);

            return true;
        }

        [Command("conversation", RBACPermissions.CommandDebugConversation)]
        static bool HandleDebugConversationCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            uint conversationEntry = args.NextUInt32();
            if (conversationEntry == 0)
                return false;

            Player target = handler.getSelectedPlayerOrSelf();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            return Conversation.CreateConversation(conversationEntry, target, target, new List<ObjectGuid>() { target.GetGUID() }) != null;
        }

        [Command("entervehicle", RBACPermissions.CommandDebugEntervehicle)]
        static bool HandleDebugEnterVehicleCommand(StringArguments args, CommandHandler handler)
        {
            Unit target = handler.getSelectedUnit();
            if (!target || !target.IsVehicle())
                return false;

            if (args.Empty())
                return false;

            uint entry = args.NextUInt32();
            if (!sbyte.TryParse(args.NextString(), out sbyte seatId))
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
        static bool HandleDebugGetItemStateCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            string itemState = args.NextString();

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

            Player player = handler.getSelectedPlayer();
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

                    Item item = player.GetItemByPos(InventorySlots.Bag0, i);
                    if (item)
                    {
                        Bag bag = item.ToBag();
                        if (bag)
                        {
                            for (byte j = 0; j < bag.GetBagSize(); ++j)
                            {
                                Item item2 = bag.GetItemByPos(j);
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

                    handler.SendSysMessage("bag: {0} slot: {1} guid: {2} - state: {3}", bagSlot, item.GetSlot(), item.GetGUID().ToString(), st);
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
                    if (i >= InventorySlots.BuyBackStart && i < InventorySlots.BuyBackEnd)
                        continue;

                    Item item = player.GetItemByPos(InventorySlots.Bag0, i);
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

                    Bag container = item.GetContainer();
                    if (container)
                    {
                        handler.SendSysMessage("The item with slot {0} and guid {1} has a container (slot: {2}, guid: {3}) but shouldn't!", item.GetSlot(), item.GetGUID().ToString(), container.GetSlot(), container.GetGUID().ToString());
                        error = true;
                        continue;
                    }

                    if (item.IsInUpdateQueue())
                    {
                        ushort qp = (ushort)item.GetQueuePos();
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

                    Bag bag = item.ToBag();
                    if (bag)
                    {
                        for (byte j = 0; j < bag.GetBagSize(); ++j)
                        {
                            Item item2 = bag.GetItemByPos(j);
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

                            Bag container1 = item2.GetContainer();
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
                                ushort qp = (ushort)item2.GetQueuePos();
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

                for (int i = 0; i < updateQueue.Count; ++i)
                {
                    Item item = updateQueue[i];
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

                    Item test = player.GetItemByPos(item.GetBagSlot(), item.GetSlot());

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

        [Command("getitemvalue", RBACPermissions.CommandDebugGetitemvalue)]
        static bool HandleDebugGetItemValueCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            if (!ulong.TryParse(args.NextString(), out ulong guid))
                return false;

            if (!uint.TryParse(args.NextString(), out uint index))
                return false;

            Item item = handler.GetSession().GetPlayer().GetItemByGuid(ObjectGuid.Create(HighGuid.Item, guid));
            if (!item)
                return false;

            if (index >= item.valuesCount)
                return false;

            uint value = item.GetUInt32Value(index);

            handler.SendSysMessage("Item {0}: value at {1} is {2}", guid, index, value);

            return true;
        }

        [Command("getvalue", RBACPermissions.CommandDebugGetvalue)]
        static bool HandleDebugGetValueCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            string fieldStr = args.NextString();
            string valueStr = args.NextString();

            if (string.IsNullOrEmpty(fieldStr))
                return false;

            Unit target = handler.getSelectedUnit();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);
                return false;
            }

            if (!uint.TryParse(fieldStr, out uint field))
                return false;

            ObjectGuid guid = target.GetGUID();
            if (field >= target.valuesCount)
            {
                handler.SendSysMessage(CypherStrings.TooBigIndex, field, guid.ToString(), target.valuesCount);
                return false;
            }

            if (!bool.TryParse(valueStr, out bool isInt32))
                isInt32 = true;

            if (isInt32)
            {
                uint value = target.GetUInt32Value(field);
                handler.SendSysMessage(CypherStrings.GetUintField, guid.ToString(), field, value);
            }
            else
            {
                float value = target.GetFloatValue(field);
                handler.SendSysMessage(CypherStrings.GetFloatField, guid.ToString(), field, value);
            }

            return true;
        }

        [Command("hostil", RBACPermissions.CommandDebugHostil)]
        static bool HandleDebugHostileRefListCommand(StringArguments args, CommandHandler handler)
        {
            Unit target = handler.getSelectedUnit();
            if (!target)
                target = handler.GetSession().GetPlayer();
            HostileReference refe = target.getHostileRefManager().getFirst();
            uint count = 0;
            handler.SendSysMessage("Hostil reference list of {0} (guid {1})", target.GetName(), target.GetGUID().ToString());
            while (refe != null)
            {
                Unit unit = refe.GetSource().GetOwner();
                if (unit)
                {
                    ++count;
                    handler.SendSysMessage("   {0}.   {1}   ({2}, SpawnId: {3})  - threat {4}", count, unit.GetName(), unit.GetGUID().ToString(), unit.IsTypeId(TypeId.Unit) ? unit.ToCreature().GetSpawnId() : 0, refe.getThreat());
                }
                refe = refe.next();
            }
            handler.SendSysMessage("End of hostil reference list.");
            return true;
        }

        [Command("itemexpire", RBACPermissions.CommandDebugItemexpire)]
        static bool HandleDebugItemExpireCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            if (!ulong.TryParse(args.NextString(), out ulong guid))
                return false;

            Item item = handler.GetSession().GetPlayer().GetItemByGuid(ObjectGuid.Create(HighGuid.Item, guid));
            if (!item)
                return false;

            handler.GetSession().GetPlayer().DestroyItem(item.GetBagSlot(), item.GetSlot(), true);
            Global.ScriptMgr.OnItemExpire(handler.GetSession().GetPlayer(), item.GetTemplate());

            return true;
        }

        [Command("lootrecipient", RBACPermissions.CommandDebugLootrecipient)]
        static bool HandleDebugGetLootRecipientCommand(StringArguments args, CommandHandler handler)
        {
            Creature target = handler.getSelectedCreature();
            if (!target)
                return false;

            handler.SendSysMessage("Loot recipient for creature {0} (GUID {1}, DB GUID {2}) is {3}", target.GetName(), target.GetGUID().ToString(), target.GetSpawnId(),
                target.hasLootRecipient() ? (target.GetLootRecipient() ? target.GetLootRecipient().GetName() : "offline") : "no loot recipient");
            return true;
        }

        [Command("los", RBACPermissions.CommandDebugLos)]
        static bool HandleDebugLoSCommand(StringArguments args, CommandHandler handler)
        {
            Unit unit = handler.getSelectedUnit();
            if (unit)
                handler.SendSysMessage("Unit {0} (GuidLow: {1}) is {2}in LoS", unit.GetName(), unit.GetGUID().ToString(), handler.GetSession().GetPlayer().IsWithinLOSInMap(unit) ? "" : "not ");
            return true;
        }

        [Command("Mod32Value", RBACPermissions.CommandDebugMod32value)]
        static bool HandleDebugMod32ValueCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            uint field = args.NextUInt32();
            int value = args.NextInt32();

            if (field >= handler.GetSession().GetPlayer().valuesCount)
            {
                handler.SendSysMessage(CypherStrings.TooBigIndex, field, handler.GetSession().GetPlayer().GetGUID().ToString(), handler.GetSession().GetPlayer().valuesCount);
                return false;
            }

            uint currentValue = handler.GetSession().GetPlayer().GetUInt32Value(field);

            currentValue += (uint)value;
            handler.GetSession().GetPlayer().SetUInt32Value(field, currentValue);

            handler.SendSysMessage(CypherStrings.Change32bitField, field, currentValue);

            return true;
        }

        [Command("moveflags", RBACPermissions.CommandDebugMoveflags)]
        static bool HandleDebugMoveflagsCommand(StringArguments args, CommandHandler handler)
        {
            Unit target = handler.getSelectedUnit();
            if (!target)
                target = handler.GetSession().GetPlayer();

            if (args.Empty())
            {
                //! Display case
                handler.SendSysMessage(CypherStrings.MoveflagsGet, target.GetUnitMovementFlags(), target.GetUnitMovementFlags2());
            }
            else
            {
                string mask1 = args.NextString();
                if (string.IsNullOrEmpty(mask1))
                    return false;

                string mask2 = args.NextString(" \n");

                if (!uint.TryParse(mask1, out uint moveFlags))
                    return false;
                target.SetUnitMovementFlags((MovementFlag)moveFlags);

                // @fixme: port master's HandleDebugMoveflagsCommand; flags need different handling

                if (!string.IsNullOrEmpty(mask2))
                {
                    if (!uint.TryParse(mask2, out uint moveFlagsExtra))
                        return false;
                    target.SetUnitMovementFlags2((MovementFlag2)moveFlagsExtra);
                }

                if (!target.IsTypeId(TypeId.Player))
                    target.DestroyForNearbyPlayers();  // Force new SMSG_UPDATE_OBJECT:CreateObject
                else
                {
                    MoveUpdate moveUpdate = new MoveUpdate();
                    moveUpdate.Status = target.m_movementInfo;
                    target.SendMessageToSet(moveUpdate, true);
                }

                handler.SendSysMessage(CypherStrings.MoveflagsSet, target.GetUnitMovementFlags(), target.GetUnitMovementFlags2());
            }

            return true;
        }

        [Command("neargraveyard", RBACPermissions.CommandNearGraveyard)]
        static bool HandleDebugNearGraveyard(StringArguments args, CommandHandler handler)
        {
            Player player = handler.GetSession().GetPlayer();
            WorldSafeLocsRecord nearestLoc = null;

            if (args.NextString().Equals("linked"))
            {
                Battleground bg = player.GetBattleground();
                if (bg)
                    nearestLoc = bg.GetClosestGraveYard(player);
                else
                {
                    BattleField bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(player.GetZoneId());
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

                foreach (var loc in CliDB.WorldSafeLocsStorage.Values)
                {
                    if (loc.MapID == player.GetMapId())
                    {
                        float dist = (loc.Loc.X - x) * (loc.Loc.X - x) + (loc.Loc.Y - y) * (loc.Loc.Y - y) + (loc.Loc.Z - z) * (loc.Loc.Z - z);
                        if (dist < distNearest)
                        {
                            distNearest = dist;
                            nearestLoc = loc;
                        }
                    }
                }
            }

            if (nearestLoc != null)
                handler.SendSysMessage(CypherStrings.CommandNearGraveyard, nearestLoc.Id, nearestLoc.Loc.X, nearestLoc.Loc.Y, nearestLoc.Loc.Z);
            else
                handler.SendSysMessage(CypherStrings.CommandNearGraveyardNotfound);

            return true;
        }

        [Command("loadcells", RBACPermissions.CommandDebugLoadcells)]
        static bool HandleDebugLoadCellsCommand(StringArguments args, CommandHandler handler)
        {
            Player player = handler.GetSession().GetPlayer();
            if (!player)
                return false;

            Map map = null;

            if (!args.Empty())
            {
                uint mapId = args.NextUInt32();
                map = Global.MapMgr.FindBaseNonInstanceMap(mapId);
            }
            if (!map)
                map = player.GetMap();

            map.LoadAllCells();

            handler.SendSysMessage("Cells loaded (mapId: {0})", map.GetId());
            return true;
        }

        [Command("phase", RBACPermissions.CommandDebugPhase)]
        static bool HandleDebugPhaseCommand(StringArguments args, CommandHandler handler)
        {
            Unit target = handler.getSelectedUnit();
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
        static bool HandleDebugRaidResetCommand(StringArguments args, CommandHandler handler)
        {
           string map_str = args.NextString();
            string difficulty_str = args.NextString();

            if (!int.TryParse(map_str, out int map) || map <= 0)
                return false;

            MapRecord mEntry = CliDB.MapStorage.LookupByKey(map);
            if (mEntry == null || !mEntry.IsRaid())
                return false;

            if (!int.TryParse(difficulty_str, out int difficulty))
                difficulty = -1;
            if (difficulty >= (int)Difficulty.Max || difficulty < -1)
                return false;

            if (difficulty == -1)
            {
                for (byte diff = 0; diff < (int)Difficulty.Max; ++diff)
                {
                    if (Global.DB2Mgr.GetMapDifficultyData((uint)map, (Difficulty)diff) != null)
                        Global.InstanceSaveMgr.ForceGlobalReset((uint)map, (Difficulty)diff);
                }
            }
            else
                Global.InstanceSaveMgr.ForceGlobalReset((uint)map, (Difficulty)difficulty);
            return true;
        }

        [Command("setaurastate", RBACPermissions.CommandDebugSetaurastate)]
        static bool HandleDebugSetAuraStateCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            Unit unit = handler.getSelectedUnit();
            if (!unit)
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);
                return false;
            }

            int state = args.NextInt32();
            if (state == 0)
            {
                // reset all states
                for (int i = 1; i <= 32; ++i)
                    unit.ModifyAuraState((AuraStateType)i, false);
                return true;
            }

            unit.ModifyAuraState((AuraStateType)Math.Abs(state), state > 0);
            return true;
        }

        [Command("setbit", RBACPermissions.CommandDebugSetbit)]
        static bool HandleDebugSet32BitCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            WorldObject target = handler.GetSelectedObject();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);
                return false;
            }

            uint field = args.NextUInt32();
            uint val = args.NextUInt32();
            if (val > 32)                                         //uint = 32 bits
                return false;

            uint value = (uint)(val != 0 ? 1 << ((int)val - 1) : 0);
            target.SetUInt32Value(field, value);

            handler.SendSysMessage(CypherStrings.Set32bitField, field, value);
            return true;
        }

        [Command("setitemvalue", RBACPermissions.CommandDebugSetitemvalue)]
        static bool HandleDebugSetItemValueCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            if (!ulong.TryParse(args.NextString(), out ulong guid))
                return false;

            if (!uint.TryParse(args.NextString(), out uint index))
                return false;

            if (!uint.TryParse(args.NextString(), out uint value))
             return false;

            Item item = handler.GetSession().GetPlayer().GetItemByGuid(ObjectGuid.Create(HighGuid.Item, guid));
            if (!item)
                return false;

            if (index >= item.valuesCount)
                return false;

            item.SetUInt32Value(index, value);
            return true;
        }

        [Command("setvalue", RBACPermissions.CommandDebugSetvalue)]
        static bool HandleDebugSetValueCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            WorldObject target = handler.GetSelectedObject();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);
                return false;
            }

            ObjectGuid guid = target.GetGUID();

            uint field = args.NextUInt32();
            if (field >= target.valuesCount)
            {
                handler.SendSysMessage(CypherStrings.TooBigIndex, field, guid.ToString(), target.valuesCount);
                return false;
            }

            string valueStr = args.NextString();
            if (!bool.TryParse(args.NextString(), out bool isInt32))
                isInt32 = true;

            if (isInt32)
            {
                if (!uint.TryParse(valueStr, out uint value))
                    return false;

                target.SetUInt32Value(field, value);
                handler.SendSysMessage(CypherStrings.SetUintField, guid.ToString(), field, value);
            }
            else
            {
                if (!float.TryParse(valueStr, out float value))
                    return false;

                target.SetFloatValue(field, value);
                handler.SendSysMessage(CypherStrings.SetFloatField, guid.ToString(), field, value);
            }

            return true;
        }

        [Command("setvid", RBACPermissions.CommandDebugSetvid)]
        static bool HandleDebugSetVehicleIdCommand(StringArguments args, CommandHandler handler)
        {
            Unit target = handler.getSelectedUnit();
            if (!target || target.IsVehicle())
                return false;

            if (args.Empty())
                return false;

            uint id = args.NextUInt32();
            handler.SendSysMessage("Vehicle id set to {0}", id);
            return true;
        }

        [Command("spawnvehicle", RBACPermissions.CommandDebugSpawnvehicle)]
        static bool HandleDebugSpawnVehicleCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            uint entry = args.NextUInt32();
            if (entry == 0)
                return false;

            float x, y, z, o = handler.GetSession().GetPlayer().GetOrientation();
            handler.GetSession().GetPlayer().GetClosePoint(out x, out y, out z, handler.GetSession().GetPlayer().GetObjectSize());

            uint id = args.NextUInt32();
            if (id == 0)
                return handler.GetSession().GetPlayer().SummonCreature(entry, x, y, z, o);

            CreatureTemplate creatureTemplate = Global.ObjectMgr.GetCreatureTemplate(entry);
            if (creatureTemplate == null)
                return false;

            VehicleRecord vehicleRecord = CliDB.VehicleStorage.LookupByKey(id);
            if (vehicleRecord == null)
                return false;

            Map map = handler.GetSession().GetPlayer().GetMap();
            Position pos = new Position(x, y, z, o);

            Creature creature = Creature.CreateCreature(entry, map, pos, id);
            if (!creature)
                return false;

            map.AddToMap(creature);
            return true;
        }

        [Command("threat", RBACPermissions.CommandDebugThreat)]
        static bool HandleDebugThreatListCommand(StringArguments args, CommandHandler handler)
        {
            Creature target = handler.getSelectedCreature();
            if (!target || target.IsTotem() || target.IsPet())
                return false;

            var threatList = target.GetThreatManager().getThreatList();
            uint count = 0;
            handler.SendSysMessage("Threat list of {0} (guid {1})", target.GetName(), target.GetGUID().ToString());
            foreach (var refe in threatList)
            {
                Unit unit = refe.getTarget();
                if (!unit)
                    continue;
                ++count;
                handler.SendSysMessage("   {0}.   {1}   (guid {2})  - threat {3}", count, unit.GetName(), unit.GetGUID().ToString(), refe.getThreat());
            }
            handler.SendSysMessage("End of threat list.");
            return true;
        }

        [Command("transport", RBACPermissions.CommandDebugTransport)]
        static bool HandleDebugTransportCommand(StringArguments args, CommandHandler handler)
        {
            Transport transport = handler.GetSession().GetPlayer().GetTransport();
            if (!transport)
                return false;

            bool start = false;
            string arg1 = args.NextString();
            if (arg1 == "stop")
                transport.EnableMovement(false);
            else if (arg1 == "start")
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

        [Command("update", RBACPermissions.CommandDebugUpdate)]
        static bool HandleDebugUpdateCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Unit unit = handler.getSelectedUnit();
            if (!unit)
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);
                return false;
            }

            uint updateIndex = args.NextUInt32();

            //check updateIndex
            if (unit.IsTypeId(TypeId.Player))
            {
                if (updateIndex >= (int)PlayerFields.End)
                    return true;
            }
            else if (updateIndex >= (int)UnitFields.End)
                return true;

            uint value;
            string valueStr = args.NextString();
            if (string.IsNullOrEmpty(valueStr))
            {
                value = unit.GetUInt32Value(updateIndex);

                handler.SendSysMessage(CypherStrings.Update, unit.GetGUID().ToString(), updateIndex, value);
                return true;
            }

            if (!uint.TryParse(valueStr, out value))
                return false;

            handler.SendSysMessage(CypherStrings.UpdateChange, unit.GetGUID().ToString(), updateIndex, value);
            unit.SetUInt32Value(updateIndex, value);

            return true;
        }

        [Command("uws", RBACPermissions.CommandDebugUws)]
        static bool HandleDebugUpdateWorldStateCommand(StringArguments args, CommandHandler handler)
        {
            if (!uint.TryParse(args.NextString(), out uint variable) || variable == 0)
                return false;

            uint state = args.NextUInt32();
            handler.GetSession().GetPlayer().SendUpdateWorldState(variable, state);
            return true;
        }

        [CommandGroup("send", RBACPermissions.CommandDebugSend)]
        class SendCommands
        {
            [Command("buyerror", RBACPermissions.CommandDebugSendBuyerror)]
            static bool HandleDebugSendBuyErrorCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                BuyResult msg = (BuyResult)args.NextUInt32();
                handler.GetSession().GetPlayer().SendBuyError(msg, null, 0);
                return true;
            }

            [Command("channelnotify", RBACPermissions.CommandDebugSendChannelnotify)]
            static bool HandleDebugSendChannelNotifyCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                string name = "test";
                byte code = args.NextByte();
                ChannelNotify packet = new ChannelNotify();
                packet.Type = (ChatNotify)code;
                packet.Channel = name;
                handler.GetSession().SendPacket(packet);
                return true;
            }

            [Command("chatmessage", RBACPermissions.CommandDebugSendChatmessage)]
            static bool HandleDebugSendChatMsgCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                string msg = "testtest";
                byte type = args.NextByte();
                ChatPkt data = new ChatPkt();
                data.Initialize((ChatMsg)type, Language.Universal, handler.GetSession().GetPlayer(), handler.GetSession().GetPlayer(), msg, 0, "chan");
                handler.GetSession().SendPacket(data);
                return true;
            }

            [Command("equiperror", RBACPermissions.CommandDebugSendEquiperror)]
            static bool HandleDebugSendEquipErrorCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                InventoryResult msg = (InventoryResult)args.NextUInt32();
                handler.GetSession().GetPlayer().SendEquipError(msg);
                return true;
            }

            [Command("largepacket", RBACPermissions.CommandDebugSendLargepacket)]
            static bool HandleDebugSendLargePacketCommand(StringArguments args, CommandHandler handler)
            {
                const string stuffingString = "This is a dummy string to push the packet's size beyond 128000 bytes. ";
                StringBuilder ss = new StringBuilder();
                while (ss.Length < 128000)
                    ss.Append(stuffingString);
                handler.SendSysMessage(ss.ToString());
                return true;
            }

            [Command("opcode", RBACPermissions.CommandDebugSendOpcode)]
            static bool HandleDebugSendOpcodeCommand(StringArguments args, CommandHandler handler)
            {
                handler.SendSysMessage(CypherStrings.NoCmd);
                return true;
            }

            [Command("playerchoice", RBACPermissions.CommandDebugSendPlayerChoice)]
            static bool HandleDebugSendPlayerChoiceCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                int choiceId = args.NextInt32();
                Player player = handler.GetSession().GetPlayer();

                player.SendPlayerChoice(player.GetGUID(), choiceId);
                return true;
            }

            [Command("qpartymsg", RBACPermissions.CommandDebugSendQpartymsg)]
            static bool HandleDebugSendQuestPartyMsgCommand(StringArguments args, CommandHandler handler)
            {
                uint msg = args.NextUInt32();
                handler.GetSession().GetPlayer().SendPushToPartyResponse(handler.GetSession().GetPlayer(), (QuestPushReason)msg);
                return true;
            }

            [Command("qinvalidmsg", RBACPermissions.CommandDebugSendQinvalidmsg)]
            static bool HandleDebugSendQuestInvalidMsgCommand(StringArguments args, CommandHandler handler)
            {
                QuestFailedReasons msg = (QuestFailedReasons)args.NextUInt32();
                handler.GetSession().GetPlayer().SendCanTakeQuestResponse(msg);
                return true;
            }

            [Command("sellerror", RBACPermissions.CommandDebugSendSellerror)]
            static bool HandleDebugSendSellErrorCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                SellResult msg = (SellResult)args.NextUInt32();
                handler.GetSession().GetPlayer().SendSellError(msg, null, ObjectGuid.Empty);
                return true;
            }

            [Command("setphaseshift", RBACPermissions.CommandDebugSendSetphaseshift)]
            static bool HandleDebugSendSetPhaseShiftCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                PhaseShift phaseShift = new PhaseShift();

                if (uint.TryParse(args.NextString(), out uint terrain))
                    phaseShift.AddVisibleMapId(terrain, null);

                if (uint.TryParse(args.NextString(), out uint phase))
                    phaseShift.AddPhase(phase, PhaseFlags.None, null);

                if (uint.TryParse(args.NextString(), out uint map))
                    phaseShift.AddUiMapPhaseId(map);

                PhasingHandler.SendToPlayer(handler.GetSession().GetPlayer(), phaseShift);
                return true;
            }

            [Command("spellfail", RBACPermissions.CommandDebugSendSpellfail)]
            static bool HandleDebugSendSpellFailCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                if (!byte.TryParse(args.NextString(), out byte failNum) ||failNum == 0)
                    return false;

                int failArg1 = args.NextInt32();
                int failArg2 = args.NextInt32();

                CastFailed castFailed = new CastFailed();
                castFailed.CastID = ObjectGuid.Empty;
                castFailed.SpellID = 133;
                castFailed.Reason = (SpellCastResult)failNum;
                castFailed.FailedArg1 = failArg1;
                castFailed.FailedArg2 = failArg2;
                handler.GetSession().SendPacket(castFailed);

                return true;
            }
        }

        [CommandGroup("play", RBACPermissions.CommandDebugPlay)]
        class PlayCommands
        {
            [Command("cinematic", RBACPermissions.CommandDebugPlayCinematic)]
            static bool HandleDebugPlayCinematicCommand(StringArguments args, CommandHandler handler)
            {
                // USAGE: .debug play cinematic #cinematicid
                // #cinematicid - ID decimal number from CinemaicSequences.dbc (1st column)
                if (args.Empty())
                {
                    handler.SendSysMessage(CypherStrings.BadValue);
                    return false;
                }

                uint id = args.NextUInt32();

                CinematicSequencesRecord cineSeq = CliDB.CinematicSequencesStorage.LookupByKey(id);
                if (cineSeq == null)
                {
                    handler.SendSysMessage(CypherStrings.CinematicNotExist, id);
                    return false;
                }

                // Dump camera locations
                var list = M2Storage.GetFlyByCameras(cineSeq.Camera[0]);
                if (list != null)
                {
                    handler.SendSysMessage("Waypoints for sequence {0}, camera {1}", id, cineSeq.Camera[0]);
                    uint count = 1;
                    foreach (FlyByCamera cam in list)
                    {
                        handler.SendSysMessage("{0} - {1}ms [{2}, {3}, {4}] Facing {5} ({6} degrees)", count, cam.timeStamp, cam.locations.X, cam.locations.Y, cam.locations.Z, cam.locations.W, cam.locations.W * (180 / Math.PI));
                        count++;
                    }
                    handler.SendSysMessage("{0} waypoints dumped", list.Count);
                }

                handler.GetSession().GetPlayer().SendCinematicStart(id);
                return true;
            }

            [Command("movie", RBACPermissions.CommandDebugPlayMovie)]
            static bool HandleDebugPlayMovieCommand(StringArguments args, CommandHandler handler)
            {
                // USAGE: .debug play movie #movieid
                // #movieid - ID decimal number from Movie.dbc (1st column)
                if (args.Empty())
                {
                    handler.SendSysMessage(CypherStrings.BadValue);
                    return false;
                }

                uint id = args.NextUInt32();

                if (!CliDB.MovieStorage.ContainsKey(id))
                {
                    handler.SendSysMessage(CypherStrings.MovieNotExist, id);
                    return false;
                }

                handler.GetSession().GetPlayer().SendMovieStart(id);
                return true;
            }

            [Command("sound", RBACPermissions.CommandDebugPlaySound)]
            static bool HandleDebugPlaySoundCommand(StringArguments args, CommandHandler handler)
            {
                // USAGE: .debug playsound #soundid
                // #soundid - ID decimal number from SoundEntries.dbc (1st column)
                if (args.Empty())
                {
                    handler.SendSysMessage(CypherStrings.BadValue);

                    return false;
                }

                uint soundId = args.NextUInt32();

                if (!CliDB.SoundKitStorage.ContainsKey(soundId))
                {
                    handler.SendSysMessage(CypherStrings.SoundNotExist, soundId);
                    return false;
                }

                Unit unit = handler.getSelectedUnit();
                if (!unit)
                {
                    handler.SendSysMessage(CypherStrings.SelectCharOrCreature);
                    return false;
                }

                if (!handler.GetSession().GetPlayer().GetTarget().IsEmpty())
                    unit.PlayDistanceSound(soundId, handler.GetSession().GetPlayer());
                else
                    unit.PlayDirectSound(soundId, handler.GetSession().GetPlayer());

                handler.SendSysMessage(CypherStrings.YouHearSound, soundId);
                return true;
            }
        }

        [CommandNonGroup("wpgps", RBACPermissions.CommandWpgps)]
        static bool HandleWPGPSCommand(StringArguments args, CommandHandler handler)
        {
            Player player = handler.GetSession().GetPlayer();

            Log.outInfo(LogFilter.SqlDev, "(@PATH, XX, {0}, {1}, {2}, 0, 0, 0, 100, 0),", player.GetPositionX(), player.GetPositionY(), player.GetPositionZ());

            handler.SendSysMessage("Waypoint SQL written to SQL Developer log");
            return true;
        }
    }
}
