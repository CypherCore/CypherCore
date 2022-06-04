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
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Chat
{
    [CommandGroup("debug", RBACPermissions.CommandDebug, true)]
    class DebugCommands
    {
        [Command("anim", RBACPermissions.CommandDebugAnim)]
        static bool HandleDebugAnimCommand(CommandHandler handler, Emote emote)
        {
            Unit unit = handler.GetSelectedUnit();
            if (unit)
                unit.HandleEmoteCommand(emote);

            handler.SendSysMessage($"Playing emote {emote}");
            return true;
        }

        [Command("areatriggers", RBACPermissions.CommandDebugAreatriggers)]
        static bool HandleDebugAreaTriggersCommand(CommandHandler handler)
        {
            Player player = handler.GetSession().GetPlayer();
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
        static bool HandleDebugArenaCommand(CommandHandler handler)
        {
            Global.BattlegroundMgr.ToggleArenaTesting();
            return true;
        }

        [Command("bg", RBACPermissions.CommandDebugBg, true)]
        static bool HandleDebugBattlegroundCommand(CommandHandler handler)
        {
            Global.BattlegroundMgr.ToggleTesting();
            return true;
        }

        [Command("boundary", RBACPermissions.CommandDebugBoundary)]
        static bool HandleDebugBoundaryCommand(CommandHandler handler, string fill, uint durationArg)
        {
            Player player = handler.GetSession().GetPlayer();
            if (!player)
                return false;

            Creature target = handler.GetSelectedCreature();
            if (!target || !target.IsAIEnabled())
                return false;

            TimeSpan duration = durationArg != 0 ? TimeSpan.FromSeconds(durationArg) : TimeSpan.Zero;
            if (duration <= TimeSpan.Zero || duration >= TimeSpan.FromMinutes(30)) // arbitrary upper limit
                duration = TimeSpan.FromMinutes(3);

            CypherStrings errMsg = target.GetAI().VisualizeBoundary(duration, player, fill == "fill");
            if (errMsg > 0)
                handler.SendSysMessage(errMsg);

            return true;
        }

        [Command("combat", RBACPermissions.CommandDebugCombat)]
        static bool HandleDebugCombatListCommand(CommandHandler handler)
        {
            Unit target = handler.GetSelectedUnit();
            if (!target)
                target = handler.GetSession().GetPlayer();

            handler.SendSysMessage($"Combat refs: (Combat state: {target.IsInCombat()} | Manager state: {target.GetCombatManager().HasCombat()})");
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

        [Command("conversation", RBACPermissions.CommandDebugConversation)]
        static bool HandleDebugConversationCommand(CommandHandler handler, StringArguments args)
        {
            if (args.Empty())
                return false;

            uint conversationEntry = args.NextUInt32();
            if (conversationEntry == 0)
                return false;

            Player target = handler.GetSelectedPlayerOrSelf();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            return Conversation.CreateConversation(conversationEntry, target, target, target.GetGUID()) != null;
        }

        [Command("dummy", RBACPermissions.CommandDebugDummy)]
        static bool HandleDebugDummyCommand(CommandHandler handler)
        {
            handler.SendSysMessage("This command does nothing right now. Edit your local core (DebugCommands.cs) to make it do whatever you need for testing.");
            return true;
        }

        [Command("entervehicle", RBACPermissions.CommandDebugEntervehicle)]
        static bool HandleDebugEnterVehicleCommand(CommandHandler handler, uint entry, sbyte seatId = -1)
        {
            Unit target = handler.GetSelectedUnit();
            if (!target || !target.IsVehicle())
                return false;

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
        static bool HandleDebugGetItemStateCommand(CommandHandler handler, string itemState)
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

        [Command("guidlimits", RBACPermissions.CommandDebug, true)]
        static bool HandleDebugGuidLimitsCommand(CommandHandler handler, uint mapId)
        {
            if (mapId != 0)
                Global.MapMgr.DoForAllMapsWithMapId(mapId, map => HandleDebugGuidLimitsMap(handler, map));
            else
                Global.MapMgr.DoForAllMaps(map => HandleDebugGuidLimitsMap(handler, map));

            handler.SendSysMessage($"Guid Warn Level: {WorldConfig.GetIntValue(WorldCfg.RespawnGuidWarnLevel)}");
            handler.SendSysMessage($"Guid Alert Level: {WorldConfig.GetIntValue(WorldCfg.RespawnGuidAlertLevel)}");
            return true;
        }

        [Command("instancespawn", RBACPermissions.CommandDebugInstancespawn)]
        static bool HandleDebugInstanceSpawns(CommandHandler handler, StringArguments args)
        {
            Player player = handler.GetSession().GetPlayer();
            if (player == null)
                return false;

            string explainOrGroupId = args.NextString();

            bool explain = false;
            uint groupID = 0;
            if (explainOrGroupId.Equals("explain"))
                explain = true;
            else
                groupID = uint.Parse(explainOrGroupId);

            if (groupID != 0 && Global.ObjectMgr.GetSpawnGroupData(groupID) == null)
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
                if (groupID != 0 && info.SpawnGroupId != groupID)
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

            if (groupID != 0 && !store.ContainsKey(groupID))
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
                                    handler.SendSysMessage($" | |-- '{groupData.name}' would be allowed to spawn by boss state {bossStateId} being {(EncounterState)actualState}, but this is overruled");
                                else
                                    handler.SendSysMessage($" | |-- '{groupData.name}' is allowed to spawn because boss state {bossStateId} is {(EncounterState)bossStateId}.");
                            }
                            else
                            {
                                isBlocked = true;
                                handler.SendSysMessage($" | |-- '{groupData.name}' is blocked from spawning because boss state {bossStateId} is {(EncounterState)bossStateId}.");
                            }
                        }
                        else
                            handler.SendSysMessage($" | |-- '{groupData.name}' could've been {(isSpawn ? "allowed to spawn" : "blocked from spawning")} if boss state {bossStateId} matched mask 0x{tuple.Item3:X2}; but it is {(EncounterState)actualState} . 0x{(1 << (int)actualState):X2}, which does not match.");
                    }
                    if (isBlocked)
                        handler.SendSysMessage($" | |=> '{groupData.name}' is not active due to a blocking rule being matched");
                    else if (isSpawned)
                        handler.SendSysMessage($" | |=> '{groupData.name}' is active due to a spawn rule being matched");
                    else
                        handler.SendSysMessage($" | |=> '{groupData.name}' is not active due to none of its rules being matched");
                }
                else
                    handler.SendSysMessage($" - '{groupData.name}' ({key}) is {(map.IsSpawnGroupActive(key) ? "" : "not ")}active");
            }
            return true;
        }

        [Command("itemexpire", RBACPermissions.CommandDebugItemexpire)]
        static bool HandleDebugItemExpireCommand(CommandHandler handler, ulong guid)
        {
            Item item = handler.GetSession().GetPlayer().GetItemByGuid(ObjectGuid.Create(HighGuid.Item, guid));
            if (!item)
                return false;

            handler.GetSession().GetPlayer().DestroyItem(item.GetBagSlot(), item.GetSlot(), true);
            Global.ScriptMgr.OnItemExpire(handler.GetSession().GetPlayer(), item.GetTemplate());

            return true;
        }

        [Command("loadcells", RBACPermissions.CommandDebugLoadcells)]
        static bool HandleDebugLoadCellsCommand(CommandHandler handler, uint mapId = 0xFFFFFFFF)
        {
            Player player = handler.GetSession().GetPlayer();
            if (!player)
                return false;

            Map map = null;
            if (mapId != 0xFFFFFFFF)
                map = Global.MapMgr.FindBaseNonInstanceMap(mapId);

            if (!map)
                map = player.GetMap();

            map.LoadAllCells();

            handler.SendSysMessage("Cells loaded (mapId: {0})", map.GetId());
            return true;
        }

        [Command("lootrecipient", RBACPermissions.CommandDebugLootrecipient)]
        static bool HandleDebugGetLootRecipientCommand(CommandHandler handler)
        {
            Creature target = handler.GetSelectedCreature();
            if (!target)
                return false;

            handler.SendSysMessage("Loot recipient for creature {0} (GUID {1}, SpawnID {2}) is {3}", target.GetName(), target.GetGUID().ToString(), target.GetSpawnId(),
                target.HasLootRecipient() ? (target.GetLootRecipient() ? target.GetLootRecipient().GetName() : "offline") : "no loot recipient");
            return true;
        }

        [Command("los", RBACPermissions.CommandDebugLos)]
        static bool HandleDebugLoSCommand(CommandHandler handler)
        {
            Unit unit = handler.GetSelectedUnit();
            if (unit)
            {
                Player player = handler.GetSession().GetPlayer();
                handler.SendSysMessage($"Checking LoS {player.GetName()} . {unit.GetName()}:");
                handler.SendSysMessage($"    VMAP LoS: {(player.IsWithinLOSInMap(unit, LineOfSightChecks.Vmap) ? "clear" : "obstructed")}");
                handler.SendSysMessage($"    GObj LoS: {(player.IsWithinLOSInMap(unit, LineOfSightChecks.Gobject) ? "clear" : "obstructed")}");
                handler.SendSysMessage($"{unit.GetName()} is {(player.IsWithinLOSInMap(unit) ? "" : "not ")}in line of sight of {player.GetName()}.");
                return true;
            }
            return false;
        }

        [Command("moveflags", RBACPermissions.CommandDebugMoveflags)]
        static bool HandleDebugMoveflagsCommand(CommandHandler handler, StringArguments args)
        {
            Unit target = handler.GetSelectedUnit();
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
                    MoveUpdate moveUpdate = new();
                    moveUpdate.Status = target.m_movementInfo;
                    target.SendMessageToSet(moveUpdate, true);
                }

                handler.SendSysMessage(CypherStrings.MoveflagsSet, target.GetUnitMovementFlags(), target.GetUnitMovementFlags2());
            }

            return true;
        }

        [Command("neargraveyard", RBACPermissions.CommandNearGraveyard)]
        static bool HandleDebugNearGraveyard(CommandHandler handler, string linked)
        {
            Player player = handler.GetSession().GetPlayer();
            WorldSafeLocsEntry nearestLoc = null;

            if (linked == "linked")
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
        static bool HandleDebugObjectCountCommand(CommandHandler handler, uint? mapId)
        {
            void HandleDebugObjectCountMap(Map map)
            {
                handler.SendSysMessage($"Map Id: {map.GetId()} Name: '{map.GetMapName()}' Instance Id: {map.GetInstanceId()} Creatures: {map.GetObjectsStore().OfType<Creature>().Count()} GameObjects: {map.GetObjectsStore().OfType<GameObject>().Count()}");

                Dictionary<uint, uint> creatureIds = new();
                foreach (var p in map.GetObjectsStore())
                {
                    if (p.Value.IsCreature())
                    {
                        if (!creatureIds.ContainsKey(p.Value.GetEntry()))
                            creatureIds[p.Value.GetEntry()] = 0;

                        creatureIds[p.Value.GetEntry()]++;
                    }
                }

                var orderedCreatures = creatureIds.OrderBy(p => p.Value).Where(p => p.Value > 5);

                handler.SendSysMessage("Top Creatures count:");
                foreach (var p in orderedCreatures)
                    handler.SendSysMessage($"Entry: {p.Key} Count: {p.Value}");
            }

            if (mapId.HasValue)
                Global.MapMgr.DoForAllMapsWithMapId(mapId.Value, map => HandleDebugObjectCountMap(map));
            else
                Global.MapMgr.DoForAllMaps(map => HandleDebugObjectCountMap(map));

            return true;
        }

        [Command("phase", RBACPermissions.CommandDebugPhase)]
        static bool HandleDebugPhaseCommand(CommandHandler handler)
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

        [Command("questreset", RBACPermissions.CommandDebugQuestreset)]
        static bool HandleDebugQuestResetCommand(CommandHandler handler, string arg)
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
                Global.WorldMgr.SetNextDailyQuestsResetTime(now);
                handler.SendSysMessage("Daily quest reset scheduled for next tick.");
            }
            if (weekly)
            {
                Global.WorldMgr.SetNextWeeklyQuestsResetTime(now);
                handler.SendSysMessage("Weekly quest reset scheduled for next tick.");
            }
            if (monthly)
            {
                Global.WorldMgr.SetNextMonthlyQuestsResetTime(now);
                handler.SendSysMessage("Monthly quest reset scheduled for next tick.");
            }

            return true;
        }

        [Command("raidreset", RBACPermissions.CommandInstanceUnbind)]
        static bool HandleDebugRaidResetCommand(CommandHandler handler, uint mapId, uint difficulty)
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

            if (difficulty != 0 && CliDB.DifficultyStorage.HasRecord(difficulty))
            {
                handler.SendSysMessage($"Invalid difficulty {difficulty}.");
                return false;
            }

            if (difficulty != 0 && Global.DB2Mgr.GetMapDifficultyData(mEntry.Id, (Difficulty)difficulty) == null)
            {
                handler.SendSysMessage($"Difficulty {(Difficulty)difficulty} is not valid for '{mEntry.MapName[handler.GetSessionDbcLocale()]}'.");
                return true;
            }

            if (difficulty == 0)
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
        static bool HandleDebugSetAuraStateCommand(CommandHandler handler, StringArguments args)
        {
            if (args.Empty())
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            Unit unit = handler.GetSelectedUnit();
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

        [Command("setvid", RBACPermissions.CommandDebugSetvid)]
        static bool HandleDebugSetVehicleIdCommand(CommandHandler handler, StringArguments args)
        {
            Unit target = handler.GetSelectedUnit();
            if (!target || target.IsVehicle())
                return false;

            if (args.Empty())
                return false;

            uint id = args.NextUInt32();
            handler.SendSysMessage("Vehicle id set to {0}", id);
            return true;
        }

        [Command("spawnvehicle", RBACPermissions.CommandDebugSpawnvehicle)]
        static bool HandleDebugSpawnVehicleCommand(CommandHandler handler, uint entry, uint id)
        {
            float x, y, z, o = handler.GetSession().GetPlayer().GetOrientation();
            handler.GetSession().GetPlayer().GetClosePoint(out x, out y, out z, handler.GetSession().GetPlayer().GetCombatReach());

            if (id == 0)
                return handler.GetSession().GetPlayer().SummonCreature(entry, x, y, z, o);

            CreatureTemplate creatureTemplate = Global.ObjectMgr.GetCreatureTemplate(entry);
            if (creatureTemplate == null)
                return false;

            VehicleRecord vehicleRecord = CliDB.VehicleStorage.LookupByKey(id);
            if (vehicleRecord == null)
                return false;

            Map map = handler.GetSession().GetPlayer().GetMap();
            Position pos = new(x, y, z, o);

            Creature creature = Creature.CreateCreature(entry, map, pos, id);
            if (!creature)
                return false;

            map.AddToMap(creature);
            return true;
        }

        [Command("threat", RBACPermissions.CommandDebugThreat)]
        static bool HandleDebugThreatListCommand(CommandHandler handler)
        {
            Unit target = handler.GetSelectedUnit();
            if (target == null)
                target = handler.GetSession().GetPlayer();

            ThreatManager mgr = target.GetThreatManager();
            if (!target.IsAlive())
            {
                handler.SendSysMessage($"{target.GetName()} ({target.GetGUID()}) is not alive.");
                return true;
            }

            uint count = 0;
            var threatenedByMe = target.GetThreatManager().GetThreatenedByMeList();
            if (threatenedByMe.Empty())
                handler.SendSysMessage($"{target.GetName()} ({target.GetGUID()}) does not threaten any units.");
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
                    handler.SendSysMessage($"{target.GetName()} ({target.GetGUID()}, SpawnID {(target.IsCreature() ? target.ToCreature().GetSpawnId() : 0)}) is not currently engaged.");
                else
                    handler.SendSysMessage($"{target.GetName()} ({target.GetGUID()}, SpawnID {(target.IsCreature() ? target.ToCreature().GetSpawnId() : 0)}) seems to be engaged, but does not have a threat list??");
            }
            else if (target.IsEngaged())
                handler.SendSysMessage($"{target.GetName()} ({target.GetGUID()}) is currently engaged. (This unit cannot have a threat list.)");
            else
                handler.SendSysMessage($"{target.GetName()} ({target.GetGUID()}) is not currently engaged. (This unit cannot have a threat list.)");

            return true;
        }

        [Command("threatinfo", RBACPermissions.CommandDebugThreatinfo)]
        static bool HandleDebugThreatInfoCommand(CommandHandler handler)
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
                var mods = mgr._singleSchoolModifiers;
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
                var mods = mgr._multiSchoolModifiers;
                handler.SendSysMessage($"- Multi-school threat modifiers ({mods.Count} entries):");

                foreach (var pair in mods)
                    handler.SendSysMessage($" |-- Mask {pair.Key:X}: {pair.Value:0.XX}");
            }

            // _redirectInfo
            {
                var redirectInfo = mgr._redirectInfo;
                if (redirectInfo.Empty())
                    handler.SendSysMessage(" - No redirects being applied");
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
                var redirectRegistry = mgr._redirectRegistry;
                if (redirectRegistry.Empty())
                    handler.SendSysMessage(" - No redirects are registered");
                else
                {
                    handler.SendSysMessage($" - {redirectRegistry.Count} spells may have redirects registered");
                    foreach (var outerPair in redirectRegistry) // (spellId, (guid, pct))
                    {
                        SpellInfo spell = Global.SpellMgr.GetSpellInfo(outerPair.Key, Difficulty.None);
                        handler.SendSysMessage($" |-- #{outerPair.Key} {(spell != null ? spell.SpellName[Global.WorldMgr.GetDefaultDbcLocale()] : "<unknown>")} ({outerPair.Value.Count} entries):");
                        foreach (var innerPair in outerPair.Value) // (guid, pct)
                        {
                            Unit unit = Global.ObjAccessor.GetUnit(target, innerPair.Key);
                            handler.SendSysMessage($"   |-- {innerPair.Value} to {(unit != null ? unit.GetName() : innerPair.Key)}");
                        }
                    }
                }
            }

            return true;
        }

        [Command("transport", RBACPermissions.CommandDebugTransport)]
        static bool HandleDebugTransportCommand(CommandHandler handler, string operation)
        {
            Transport transport = handler.GetSession().GetPlayer().GetTransport<Transport>();
            if (!transport)
                return false;

            bool start = false;
            if (operation == "stop")
                transport.EnableMovement(false);
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

        [Command("worldstate", RBACPermissions.CommandDebugWorldState)]
        static bool HandleDebugUpdateWorldStateCommand(CommandHandler handler, uint variable, uint value)
        {
            handler.GetSession().GetPlayer().SendUpdateWorldState(variable, value);
            return true;
        }

        [Command("worldstate", RBACPermissions.CommandDebug)]
        static bool HandleDebugWorldStateCommand(CommandHandler handler, StringArguments args)
        {
            if (args.Empty())
                return false;

            string worldStateIdStr = args.NextString();
            string valueStr = args.NextString();

            if (worldStateIdStr.IsEmpty())
                return false;

            Player target = handler.GetSelectedPlayerOrSelf();
            if (target == null)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            uint worldStateId = uint.Parse(worldStateIdStr);
            uint value = valueStr.IsEmpty() ? 0 : uint.Parse(valueStr);

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
        static bool HandleWPGPSCommand(CommandHandler handler)
        {
            Player player = handler.GetSession().GetPlayer();

            Log.outInfo(LogFilter.SqlDev, $"(@PATH, XX, {player.GetPositionX():3F}, {player.GetPositionY():3F}, {player.GetPositionZ():5F}, {player.GetOrientation():5F}, 0, 0, 0, 100, 0)");

            handler.SendSysMessage("Waypoint SQL written to SQL Developer log");
            return true;
        }

        [Command("wsexpression", RBACPermissions.CommandDebug)]
        static bool HandleDebugWSExpressionCommand(CommandHandler handler, StringArguments args)
        {
            if (args.Empty())
                return false;

            string expressionIdStr = args.NextString();

            if (expressionIdStr.IsEmpty())
                return false;

            uint expressionId = uint.Parse(expressionIdStr);

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

        [CommandGroup("play", RBACPermissions.CommandDebugPlay)]
        class PlayCommands
        {
            [Command("cinematic", RBACPermissions.CommandDebugPlayCinematic)]
            static bool HandleDebugPlayCinematicCommand(CommandHandler handler, uint cinematicId)
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

                handler.GetSession().GetPlayer().SendCinematicStart(cinematicId);
                return true;
            }

            [Command("movie", RBACPermissions.CommandDebugPlayMovie)]
            static bool HandleDebugPlayMovieCommand(CommandHandler handler, uint movieId)
            {
                if (!CliDB.MovieStorage.ContainsKey(movieId))
                {
                    handler.SendSysMessage(CypherStrings.MovieNotExist, movieId);
                    return false;
                }

                handler.GetSession().GetPlayer().SendMovieStart(movieId);
                return true;
            }

            [Command("music", RBACPermissions.CommandDebugPlayMusic)]
            static bool HandleDebugPlayMusicCommand(CommandHandler handler, uint musicId)
            {
                if (!CliDB.SoundKitStorage.ContainsKey(musicId))
                {
                    handler.SendSysMessage(CypherStrings.SoundNotExist, musicId);
                    return false;
                }

                Player player = handler.GetSession().GetPlayer();

                player.PlayDirectMusic(musicId, player);

                handler.SendSysMessage(CypherStrings.YouHearSound, musicId);
                return true;
            }

            [Command("sound", RBACPermissions.CommandDebugPlaySound)]
            static bool HandleDebugPlaySoundCommand(CommandHandler handler, uint soundId, uint broadcastTextId)
            {
                if (!CliDB.SoundKitStorage.ContainsKey(soundId))
                {
                    handler.SendSysMessage(CypherStrings.SoundNotExist, soundId);
                    return false;
                }

                Player player = handler.GetSession().GetPlayer();

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

        [CommandGroup("pvp", RBACPermissions.CommandDebug)]
        class PvpCommands
        {
            [Command("warmode", RBACPermissions.CommandDebug)]
            static bool HandleDebugWarModeFactionBalanceCommand(CommandHandler handler, string command, int rewardValue = 0)
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


        [CommandGroup("send", RBACPermissions.CommandDebugSend)]
        class SendCommands
        {
            [Command("buyerror", RBACPermissions.CommandDebugSendBuyerror)]
            static bool HandleDebugSendBuyErrorCommand(CommandHandler handler, StringArguments args)
            {
                if (args.Empty())
                    return false;

                BuyResult msg = (BuyResult)args.NextUInt32();
                handler.GetSession().GetPlayer().SendBuyError(msg, null, 0);
                return true;
            }

            [Command("channelnotify", RBACPermissions.CommandDebugSendChannelnotify)]
            static bool HandleDebugSendChannelNotifyCommand(CommandHandler handler, StringArguments args)
            {
                if (args.Empty())
                    return false;

                string name = "test";
                byte code = args.NextByte();
                ChannelNotify packet = new();
                packet.Type = (ChatNotify)code;
                packet.Channel = name;
                handler.GetSession().SendPacket(packet);
                return true;
            }

            [Command("chatmessage", RBACPermissions.CommandDebugSendChatmessage)]
            static bool HandleDebugSendChatMsgCommand(CommandHandler handler, StringArguments args)
            {
                if (args.Empty())
                    return false;

                string msg = "testtest";
                byte type = args.NextByte();
                ChatPkt data = new();
                data.Initialize((ChatMsg)type, Language.Universal, handler.GetSession().GetPlayer(), handler.GetSession().GetPlayer(), msg, 0, "chan");
                handler.GetSession().SendPacket(data);
                return true;
            }

            [Command("equiperror", RBACPermissions.CommandDebugSendEquiperror)]
            static bool HandleDebugSendEquipErrorCommand(CommandHandler handler, StringArguments args)
            {
                if (args.Empty())
                    return false;

                InventoryResult msg = (InventoryResult)args.NextUInt32();
                handler.GetSession().GetPlayer().SendEquipError(msg);
                return true;
            }

            [Command("largepacket", RBACPermissions.CommandDebugSendLargepacket)]
            static bool HandleDebugSendLargePacketCommand(CommandHandler handler)
            {
                const string stuffingString = "This is a dummy string to push the packet's size beyond 128000 bytes. ";
                StringBuilder ss = new();
                while (ss.Length < 128000)
                    ss.Append(stuffingString);
                handler.SendSysMessage(ss.ToString());
                return true;
            }

            [Command("opcode", RBACPermissions.CommandDebugSendOpcode)]
            static bool HandleDebugSendOpcodeCommand(CommandHandler handler)
            {
                handler.SendSysMessage(CypherStrings.NoCmd);
                return true;
            }

            [Command("playerchoice", RBACPermissions.CommandDebugSendPlayerChoice)]
            static bool HandleDebugSendPlayerChoiceCommand(CommandHandler handler, StringArguments args)
            {
                if (args.Empty())
                    return false;

                int choiceId = args.NextInt32();
                Player player = handler.GetSession().GetPlayer();

                player.SendPlayerChoice(player.GetGUID(), choiceId);
                return true;
            }

            [Command("qpartymsg", RBACPermissions.CommandDebugSendQpartymsg)]
            static bool HandleDebugSendQuestPartyMsgCommand(CommandHandler handler, StringArguments args)
            {
                uint msg = args.NextUInt32();
                handler.GetSession().GetPlayer().SendPushToPartyResponse(handler.GetSession().GetPlayer(), (QuestPushReason)msg);
                return true;
            }

            [Command("qinvalidmsg", RBACPermissions.CommandDebugSendQinvalidmsg)]
            static bool HandleDebugSendQuestInvalidMsgCommand(CommandHandler handler, StringArguments args)
            {
                QuestFailedReasons msg = (QuestFailedReasons)args.NextUInt32();
                handler.GetSession().GetPlayer().SendCanTakeQuestResponse(msg);
                return true;
            }

            [Command("sellerror", RBACPermissions.CommandDebugSendSellerror)]
            static bool HandleDebugSendSellErrorCommand(CommandHandler handler, StringArguments args)
            {
                if (args.Empty())
                    return false;

                SellResult msg = (SellResult)args.NextUInt32();
                handler.GetSession().GetPlayer().SendSellError(msg, null, ObjectGuid.Empty);
                return true;
            }

            [Command("setphaseshift", RBACPermissions.CommandDebugSendSetphaseshift)]
            static bool HandleDebugSendSetPhaseShiftCommand(CommandHandler handler, uint phaseId, uint visibleMapId, uint uiMapPhaseId)
            {
                PhaseShift phaseShift = new();

                if (phaseId != 0)
                    phaseShift.AddPhase(phaseId, PhaseFlags.None, null);

                if (visibleMapId != 0)
                    phaseShift.AddVisibleMapId(visibleMapId, null);

                if (uiMapPhaseId != 0)
                    phaseShift.AddUiMapPhaseId(uiMapPhaseId);

                PhasingHandler.SendToPlayer(handler.GetSession().GetPlayer(), phaseShift);
                return true;
            }

            [Command("spellfail", RBACPermissions.CommandDebugSendSpellfail)]
            static bool HandleDebugSendSpellFailCommand(CommandHandler handler, StringArguments args)
            {
                if (args.Empty())
                    return false;

                if (!byte.TryParse(args.NextString(), out byte failNum) || failNum == 0)
                    return false;

                int failArg1 = args.NextInt32();
                int failArg2 = args.NextInt32();

                CastFailed castFailed = new();
                castFailed.CastID = ObjectGuid.Empty;
                castFailed.SpellID = 133;
                castFailed.Reason = (SpellCastResult)failNum;
                castFailed.FailedArg1 = failArg1;
                castFailed.FailedArg2 = failArg2;
                handler.GetSession().SendPacket(castFailed);

                return true;
            }
        }

        static void HandleDebugGuidLimitsMap(CommandHandler handler, Map map)
        {
            handler.SendSysMessage($"Map Id: {map.GetId()} Name: '{map.GetMapName()}' Instance Id: {map.GetInstanceId()} Highest Guid Creature: {map.GenerateLowGuid(HighGuid.Creature)} GameObject: {map.GetMaxLowGuid(HighGuid.GameObject)}");
        }
    }
}
