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
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Guilds;
using Game.Loots;
using Game.Maps;
using Game.Network;
using Game.Network.Packets;
using System;
using System.Collections.Generic;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.LootItem)]
        void HandleAutostoreLootItem(LootItemPkt packet)
        {
            Player player = GetPlayer();
            AELootResult aeResult = player.GetAELootView().Count > 1 ? new AELootResult() : null;

            // @todo Implement looting by LootObject guid
            foreach (LootRequest req in packet.Loot)
            {
                Loot loot = null;
                ObjectGuid lguid = player.GetLootWorldObjectGUID(req.Object);

                if (lguid.IsGameObject())
                {
                    GameObject go = player.GetMap().GetGameObject(lguid);

                    // not check distance for GO in case owned GO (fishing bobber case, for example) or Fishing hole GO
                    if (!go || ((go.GetOwnerGUID() != player.GetGUID() && go.GetGoType() != GameObjectTypes.FishingHole) && !go.IsWithinDistInMap(player, SharedConst.InteractionDistance)))
                    {
                        player.SendLootRelease(lguid);
                        continue;
                    }

                    loot = go.loot;
                }
                else if (lguid.IsItem())
                {
                    Item pItem = player.GetItemByGuid(lguid);

                    if (!pItem)
                    {
                        player.SendLootRelease(lguid);
                        continue;
                    }

                    loot = pItem.loot;
                }
                else if (lguid.IsCorpse())
                {
                    Corpse bones = ObjectAccessor.GetCorpse(player, lguid);
                    if (!bones)
                    {
                        player.SendLootRelease(lguid);
                        continue;
                    }

                    loot = bones.loot;
                }
                else
                {
                    Creature creature = player.GetMap().GetCreature(lguid);

                    bool lootAllowed = creature && creature.IsAlive() == (player.GetClass() == Class.Rogue && creature.loot.loot_type == LootType.Pickpocketing);
                    if (!lootAllowed || !creature.IsWithinDistInMap(player, AELootCreatureCheck.LootDistance))
                    {
                        player.SendLootError(req.Object, lguid, lootAllowed ? LootError.TooFar : LootError.DidntKill);
                        continue;
                    }

                    loot = creature.loot;
                }

                player.StoreLootItem((byte)(req.LootListID - 1), loot, aeResult);

                // If player is removing the last LootItem, delete the empty container.
                if (loot.isLooted() && lguid.IsItem())
                    player.GetSession().DoLootRelease(lguid);
            }

            if (aeResult != null)
            {
                foreach (var resultValue in aeResult.GetByOrder())
                {
                    player.SendNewItem(resultValue.item, resultValue.count, false, false, true);
                    player.UpdateCriteria(CriteriaTypes.LootItem, resultValue.item.GetEntry(), resultValue.count);
                    player.UpdateCriteria(CriteriaTypes.LootType, resultValue.item.GetEntry(), resultValue.count, (ulong)resultValue.lootType);
                    player.UpdateCriteria(CriteriaTypes.LootEpicItem, resultValue.item.GetEntry(), resultValue.count);
                }
            }
        }

        [WorldPacketHandler(ClientOpcodes.LootMoney)]
        void HandleLootMoney(LootMoney lootMoney)
        {
            Player player = GetPlayer();

            foreach (var lootView in player.GetAELootView())
            {
                ObjectGuid guid = lootView.Value;
                Loot loot = null;
                bool shareMoney = true;

                switch (guid.GetHigh())
                {
                    case HighGuid.GameObject:
                        {
                            GameObject go = player.GetMap().GetGameObject(guid);

                            // do not check distance for GO if player is the owner of it (ex. fishing bobber)
                            if (go && ((go.GetOwnerGUID() == player.GetGUID() || go.IsWithinDistInMap(player, SharedConst.InteractionDistance))))
                                loot = go.loot;

                            break;
                        }
                    case HighGuid.Corpse:                               // remove insignia ONLY in BG
                        {
                            Corpse bones = ObjectAccessor.GetCorpse(player, guid);

                            if (bones && bones.IsWithinDistInMap(player, SharedConst.InteractionDistance))
                            {
                                loot = bones.loot;
                                shareMoney = false;
                            }

                            break;
                        }
                    case HighGuid.Item:
                        {
                            Item item = player.GetItemByGuid(guid);
                            if (item)
                            {
                                loot = item.loot;
                                shareMoney = false;
                            }
                            break;
                        }
                    case HighGuid.Creature:
                    case HighGuid.Vehicle:
                        {
                            Creature creature = player.GetMap().GetCreature(guid);
                            bool lootAllowed = creature && creature.IsAlive() == (player.GetClass() == Class.Rogue && creature.loot.loot_type == LootType.Pickpocketing);
                            if (lootAllowed && creature.IsWithinDistInMap(player, AELootCreatureCheck.LootDistance))
                            {
                                loot = creature.loot;
                                if (creature.IsAlive())
                                    shareMoney = false;
                            }
                            else
                                player.SendLootError(lootView.Key, guid, lootAllowed ? LootError.TooFar : LootError.DidntKill);
                            break;
                        }
                    default:
                        continue;                                         // unlootable type
                }

                if (loot == null)
                    continue;

                loot.NotifyMoneyRemoved();
                if (shareMoney && player.GetGroup() != null)      //item, pickpocket and players can be looted only single player
                {

                    Group group = player.GetGroup();

                    List<Player> playersNear = new List<Player>();
                    for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.next())
                    {
                        Player member = refe.GetSource();
                        if (!member)
                            continue;

                        if (player.IsAtGroupRewardDistance(member))
                            playersNear.Add(member);
                    }

                    uint goldPerPlayer = (uint)(loot.gold / playersNear.Count);

                    foreach (var pl in playersNear)
                    {
                        pl.ModifyMoney(goldPerPlayer);
                        pl.UpdateCriteria(CriteriaTypes.LootMoney, goldPerPlayer);

                        Guild guild = Global.GuildMgr.GetGuildById(pl.GetGuildId());
                        if (guild)
                        {
                            uint guildGold = MathFunctions.CalculatePct(goldPerPlayer, pl.GetTotalAuraModifier(AuraType.DepositBonusMoneyInGuildBankOnLoot));
                            if (guildGold != 0)
                                guild.HandleMemberDepositMoney(this, guildGold, true);
                        }

                        LootMoneyNotify packet = new LootMoneyNotify();
                        packet.Money = goldPerPlayer;
                        packet.SoleLooter = playersNear.Count <= 1 ? true : false;
                        pl.SendPacket(packet);
                    }
                }
                else
                {
                    player.ModifyMoney(loot.gold);
                    player.UpdateCriteria(CriteriaTypes.LootMoney, loot.gold);

                    Guild guild = Global.GuildMgr.GetGuildById(player.GetGuildId());
                    if (guild)
                    {
                        uint guildGold = MathFunctions.CalculatePct(loot.gold, player.GetTotalAuraModifier(AuraType.DepositBonusMoneyInGuildBankOnLoot));
                        if (guildGold != 0)
                            guild.HandleMemberDepositMoney(this, guildGold, true);
                    }

                    LootMoneyNotify packet = new LootMoneyNotify();
                    packet.Money = loot.gold;
                    packet.SoleLooter = true; // "You loot..."
                    SendPacket(packet);
                }

                loot.gold = 0;

                // Delete the money loot record from the DB
                if (!loot.containerID.IsEmpty())
                    loot.DeleteLootMoneyFromContainerItemDB();

                // Delete container if empty
                if (loot.isLooted() && guid.IsItem())
                    player.GetSession().DoLootRelease(guid);
            }
        }

        class AELootCreatureCheck : ICheck<Creature>
        {
            public static float LootDistance = 30.0f;

            public AELootCreatureCheck(Player looter, ObjectGuid mainLootTarget)
            {
                _looter = looter;
                _mainLootTarget = mainLootTarget;
            }

            public bool Invoke(Creature creature)
            {
                if (creature.IsAlive())
                    return false;

                if (creature.GetGUID() == _mainLootTarget)
                    return false;

                if (!_looter.IsWithinDist(creature, LootDistance))
                    return false;

                return _looter.isAllowedToLoot(creature);
            }

            Player _looter;
            ObjectGuid _mainLootTarget;
        }

        [WorldPacketHandler(ClientOpcodes.LootUnit)]
        void HandleLoot(LootUnit packet)
        {
            // Check possible cheat
            if (!GetPlayer().IsAlive() || !packet.Unit.IsCreatureOrVehicle())
                return;

            List<Creature> corpses = new List<Creature>();
            AELootCreatureCheck check = new AELootCreatureCheck(_player, packet.Unit);
            CreatureListSearcher searcher = new CreatureListSearcher(_player, corpses, check);
            Cell.VisitGridObjects(_player, searcher, AELootCreatureCheck.LootDistance);

            if (!corpses.Empty())
                SendPacket(new AELootTargets((uint)corpses.Count + 1));

            GetPlayer().SendLoot(packet.Unit, LootType.Corpse);

            if (!corpses.Empty())
            {
                // main target
                SendPacket(new AELootTargetsAck());

                foreach (Creature creature in corpses)
                {
                    GetPlayer().SendLoot(creature.GetGUID(), LootType.Corpse, true);
                    SendPacket(new AELootTargetsAck());
                }
            }

            // interrupt cast
            if (GetPlayer().IsNonMeleeSpellCast(false))
                GetPlayer().InterruptNonMeleeSpells(false);
        }

        [WorldPacketHandler(ClientOpcodes.LootRelease)]
        void HandleLootRelease(LootRelease packet)
        {
            // cheaters can modify lguid to prevent correct apply loot release code and re-loot
            // use internal stored guid
            if (GetPlayer().HasLootWorldObjectGUID(packet.Unit))
                DoLootRelease(packet.Unit);
        }

        public void DoLootRelease(ObjectGuid lguid)
        {
            Player player = GetPlayer();
            Loot loot;

            if (player.GetLootGUID() == lguid)
                player.SetLootGUID(ObjectGuid.Empty);
            player.SendLootRelease(lguid);

            player.RemoveFlag(UnitFields.Flags, UnitFlags.Looting);

            if (!player.IsInWorld)
                return;

            if (lguid.IsGameObject())
            {
                GameObject go = player.GetMap().GetGameObject(lguid);

                // not check distance for GO in case owned GO (fishing bobber case, for example) or Fishing hole GO
                if (!go || ((go.GetOwnerGUID() != player.GetGUID() && go.GetGoType() != GameObjectTypes.FishingHole) && !go.IsWithinDistInMap(player, SharedConst.InteractionDistance)))
                    return;

                loot = go.loot;

                if (go.GetGoType() == GameObjectTypes.Door)
                {
                    // locked doors are opened with spelleffect openlock, prevent remove its as looted
                    go.UseDoorOrButton();
                }
                else if (loot.isLooted() || go.GetGoType() == GameObjectTypes.FishingNode)
                {
                    if (go.GetGoType() == GameObjectTypes.FishingHole)
                    {                                               // The fishing hole used once more
                        go.AddUse();                               // if the max usage is reached, will be despawned in next tick
                        if (go.GetUseCount() >= go.m_goValue.FishingHole.MaxOpens)
                            go.SetLootState(LootState.JustDeactivated);
                        else
                            go.SetLootState(LootState.Ready);
                    }
                    else
                        go.SetLootState(LootState.JustDeactivated);

                    loot.clear();
                }
                else
                {
                    // not fully looted object
                    go.SetLootState(LootState.Activated, player);

                    // if the round robin player release, reset it.
                    if (player.GetGUID() == loot.roundRobinPlayer)
                        loot.roundRobinPlayer.Clear();
                }
            }
            else if (lguid.IsCorpse())        // ONLY remove insignia at BG
            {
                Corpse corpse = ObjectAccessor.GetCorpse(player, lguid);
                if (!corpse || !corpse.IsWithinDistInMap(player, SharedConst.InteractionDistance))
                    return;

                loot = corpse.loot;

                if (loot.isLooted())
                {
                    loot.clear();
                    corpse.RemoveFlag(CorpseFields.DynamicFlags, 0x0001);
                }
            }
            else if (lguid.IsItem())
            {
                Item pItem = player.GetItemByGuid(lguid);
                if (!pItem)
                    return;

                ItemTemplate proto = pItem.GetTemplate();

                // destroy only 5 items from stack in case prospecting and milling
                if (proto.GetFlags().HasAnyFlag(ItemFlags.IsProspectable | ItemFlags.IsMillable))
                {
                    pItem.m_lootGenerated = false;
                    pItem.loot.clear();

                    uint count = pItem.GetCount();

                    // >=5 checked in spell code, but will work for cheating cases also with removing from another stacks.
                    if (count > 5)
                        count = 5;

                    player.DestroyItemCount(pItem, ref count, true);
                }
                else
                {
                    if (pItem.loot.isLooted() || !proto.GetFlags().HasAnyFlag(ItemFlags.HasLoot)) // Only delete item if no loot or money (unlooted loot is saved to db)
                        player.DestroyItem(pItem.GetBagSlot(), pItem.GetSlot(), true);
                }
                return;                                             // item can be looted only single player
            }
            else
            {
                Creature creature = player.GetMap().GetCreature(lguid);

                bool lootAllowed = creature && creature.IsAlive() == (player.GetClass() == Class.Rogue && creature.loot.loot_type == LootType.Pickpocketing);
                if (!lootAllowed || !creature.IsWithinDistInMap(player, AELootCreatureCheck.LootDistance))
                    return;

                loot = creature.loot;
                if (loot.isLooted())
                {
                    creature.RemoveFlag(ObjectFields.DynamicFlags, UnitDynFlags.Lootable);

                    // skip pickpocketing loot for speed, skinning timer reduction is no-op in fact
                    if (!creature.IsAlive())
                        creature.AllLootRemovedFromCorpse();

                    loot.clear();
                }
                else
                {
                    // if the round robin player release, reset it.
                    if (player.GetGUID() == loot.roundRobinPlayer)
                    {
                        loot.roundRobinPlayer.Clear();

                        Group group = player.GetGroup();
                        if (group)
                        {
                            if (group.GetLootMethod() != LootMethod.MasterLoot)
                                group.SendLooter(creature, null);


                        }
                        // force dynflag update to update looter and lootable info
                        creature.ForceValuesUpdateAtIndex(ObjectFields.DynamicFlags);
                    }
                }
            }

            //Player is not looking at loot list, he doesn't need to see updates on the loot list
            loot.RemoveLooter(player.GetGUID());
            player.RemoveAELootedObject(loot.GetGUID());
        }

        void DoLootReleaseAll()
        {
            Dictionary<ObjectGuid, ObjectGuid> lootView = _player.GetAELootView();
            foreach (var lootPair in lootView)
                DoLootRelease(lootPair.Value);
        }

        //[WorldPacketHandler(ClientOpcodes.MasterLootItem)]
        void HandleLootMasterGive(WorldPacket packet)
        {
            ObjectGuid lootguid = packet.ReadPackedGuid();
            byte slotid = packet.ReadUInt8();
            ObjectGuid target_playerguid = packet.ReadPackedGuid();

            if (GetPlayer().GetGroup() == null || GetPlayer().GetGroup().GetLooterGuid() != GetPlayer().GetGUID() || GetPlayer().GetGroup().GetLootMethod() != LootMethod.MasterLoot)
            {
                GetPlayer().SendLootError(lootguid, ObjectGuid.Empty, LootError.DidntKill);
                return;
            }

            Player target = Global.ObjAccessor.FindPlayer(target_playerguid);
            if (!target)
            {
                GetPlayer().SendLootError(lootguid, ObjectGuid.Empty, LootError.PlayerNotFound);
                return;
            }

            Log.outDebug(LogFilter.Network, "HandleLootMasterGive (CMSG_LOOT_MASTER_GIVE, 0x02A3) Target = [{0}].", target.GetName());

            if (GetPlayer().GetLootGUID() != lootguid)
            {
                GetPlayer().SendLootError(lootguid, ObjectGuid.Empty, LootError.DidntKill);
                return;
            }

            if (!GetPlayer().IsInRaidWith(target) || !GetPlayer().IsInMap(target))
            {
                GetPlayer().SendLootError(lootguid, ObjectGuid.Empty, LootError.MasterOther);
                Log.outInfo(LogFilter.Loot, "MasterLootItem: Player {0} tried to give an item to ineligible player {1}!", GetPlayer().GetName(), target.GetName());
                return;
            }

            Loot loot = null;

            if (GetPlayer().GetLootGUID().IsCreatureOrVehicle())
            {
                Creature creature = GetPlayer().GetMap().GetCreature(lootguid);
                if (creature == null)
                    return;

                loot = creature.loot;
            }
            else if (GetPlayer().GetLootGUID().IsGameObject())
            {
                GameObject pGO = GetPlayer().GetMap().GetGameObject(lootguid);
                if (pGO == null)
                    return;

                loot = pGO.loot;
            }

            if (loot == null)
                return;

            if (slotid >= loot.items.Count + loot.quest_items.Count)
            {
                Log.outDebug(LogFilter.Loot, "MasterLootItem: Player {0} might be using a hack! (slot {1}, size {2})",
                    GetPlayer().GetName(), slotid, loot.items.Count);
                return;
            }

            LootItem item = slotid >= loot.items.Count ? loot.quest_items[slotid - loot.items.Count] : loot.items[slotid];

            List<ItemPosCount> dest = new List<ItemPosCount>();
            InventoryResult msg = target.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, item.itemid, item.count);
            if (item.follow_loot_rules && !item.AllowedForPlayer(target))
                msg = InventoryResult.CantEquipEver;
            if (msg != InventoryResult.Ok)
            {
                if (msg == InventoryResult.ItemMaxCount)
                    GetPlayer().SendLootError(lootguid, ObjectGuid.Empty, LootError.MasterUniqueItem);
                else if (msg == InventoryResult.InvFull)
                    GetPlayer().SendLootError(lootguid, ObjectGuid.Empty, LootError.MasterInvFull);
                else
                    GetPlayer().SendLootError(lootguid, ObjectGuid.Empty, LootError.MasterOther);

                target.SendEquipError(msg, null, null, item.itemid);
                return;
            }

            // not move item from loot to target inventory
            Item newitem = target.StoreNewItem(dest, item.itemid, true, item.randomPropertyId, item.GetAllowedLooters(), item.context, item.BonusListIDs);
            target.SendNewItem(newitem, item.count, false, false, true);
            target.UpdateCriteria(CriteriaTypes.LootItem, item.itemid, item.count);
            target.UpdateCriteria(CriteriaTypes.LootType, item.itemid, item.count, (ulong)loot.loot_type);
            target.UpdateCriteria(CriteriaTypes.LootEpicItem, item.itemid, item.count);

            // mark as looted
            item.count = 0;
            item.is_looted = true;

            loot.NotifyItemRemoved(slotid);
            --loot.unlootedCount;
        }

        [WorldPacketHandler(ClientOpcodes.SetLootSpecialization)]
        void HandleSetLootSpecialization(SetLootSpecialization packet)
        {
            if (packet.SpecID != 0)
            {
                ChrSpecializationRecord chrSpec = CliDB.ChrSpecializationStorage.LookupByKey(packet.SpecID);
                if (chrSpec != null)
                {
                    if (chrSpec.ClassID == (uint)GetPlayer().GetClass())
                        GetPlayer().SetLootSpecId(packet.SpecID);
                }
            }
            else
                GetPlayer().SetLootSpecId(0);
        }
    }
}
