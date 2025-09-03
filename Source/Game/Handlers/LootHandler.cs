// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Loots;
using Game.Maps;
using Game.Networking;
using Game.Networking.Packets;
using Game.Spells;
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

            foreach (LootRequest req in packet.Loot)
            {
                Loot loot = player.GetAELootView().LookupByKey(req.Object);
                if (loot == null)
                {
                    player.SendLootRelease(ObjectGuid.Empty);
                    continue;
                }

                ObjectGuid lguid = loot.GetOwnerGUID();

                if (lguid.IsGameObject())
                {
                    GameObject go = player.GetMap().GetGameObject(lguid);

                    // not check distance for GO in case owned GO (fishing bobber case, for example) or Fishing hole GO
                    if (go == null || ((go.GetOwnerGUID() != player.GetGUID() && go.GetGoType() != GameObjectTypes.FishingHole) && !go.IsWithinDistInMap(player)))
                    {
                        player.SendLootRelease(lguid);
                        continue;
                    }
                }
                else if (lguid.IsCreatureOrVehicle())
                {
                    Creature creature = player.GetMap().GetCreature(lguid);
                    if (creature == null)
                    {
                        player.SendLootError(req.Object, lguid, LootError.NoLoot);
                        continue;
                    }

                    if (!creature.IsWithinDistInMap(player, AELootCreatureCheck.LootDistance))
                    {
                        player.SendLootError(req.Object, lguid, LootError.TooFar);
                        continue;
                    }
                }

                player.StoreLootItem(lguid, req.LootListID, loot, aeResult);

                // If player is removing the last LootItem, delete the empty container.
                if (loot.IsLooted() && lguid.IsItem())
                    player.GetSession().DoLootRelease(loot);
            }

            if (aeResult != null)
            {
                foreach (var resultValue in aeResult.GetByOrder())
                {
                    player.SendNewItem(resultValue.item, resultValue.count, false, false, true, resultValue.dungeonEncounterId);
                    player.UpdateCriteria(CriteriaType.LootItem, resultValue.item.GetEntry(), resultValue.count);
                    player.UpdateCriteria(CriteriaType.GetLootByType, resultValue.item.GetEntry(), resultValue.count, (ulong)resultValue.lootType);
                    player.UpdateCriteria(CriteriaType.LootAnyItem, resultValue.item.GetEntry(), resultValue.count);
                }
            }

            Unit.ProcSkillsAndAuras(player, null, new ProcFlagsInit(ProcFlags.Looted), new ProcFlagsInit(), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, null, null, null);
        }

        [WorldPacketHandler(ClientOpcodes.LootMoney)]
        void HandleLootMoney(LootMoney lootMoney)
        {
            Player player = GetPlayer();
            List<Loot> forceLootRelease = new();

            foreach (var lootView in player.GetAELootView())
            {
                Loot loot = lootView.Value;
                ObjectGuid guid = loot.GetOwnerGUID();
                bool shareMoney = loot.loot_type == LootType.Corpse;

                loot.NotifyMoneyRemoved(player.GetMap());
                if (shareMoney && player.GetGroup() != null)      //item, pickpocket and players can be looted only single player
                {
                    Group group = player.GetGroup();

                    List<Player> playersNear = new();
                    foreach (GroupReference groupRef in group.GetMembers())
                    {
                        Player member = groupRef.GetSource();
                        if (!loot.HasAllowedLooter(member.GetGUID()))
                            continue;

                        if (player.IsAtGroupRewardDistance(member))
                            playersNear.Add(member);
                    }

                    ulong goldPerPlayer = (ulong)(loot.gold / playersNear.Count);

                    foreach (var pl in playersNear)
                    {
                        ulong goldMod = MathFunctions.CalculatePct(goldPerPlayer, pl.GetTotalAuraModifierByMiscValue(AuraType.ModMoneyGain, 1));

                        pl.ModifyMoney((long)(goldPerPlayer + goldMod));
                        pl.UpdateCriteria(CriteriaType.MoneyLootedFromCreatures, goldPerPlayer);

                        LootMoneyNotify packet = new();
                        packet.Money = goldPerPlayer;
                        packet.MoneyMod = goldMod;
                        packet.SoleLooter = playersNear.Count <= 1 ? true : false;
                        pl.SendPacket(packet);
                    }
                }
                else
                {
                    ulong goldMod = MathFunctions.CalculatePct(loot.gold, player.GetTotalAuraModifierByMiscValue(AuraType.ModMoneyGain, 1));

                    player.ModifyMoney((long)(loot.gold + goldMod));
                    player.UpdateCriteria(CriteriaType.MoneyLootedFromCreatures, loot.gold);

                    LootMoneyNotify packet = new();
                    packet.Money = loot.gold;
                    packet.MoneyMod = goldMod;
                    packet.SoleLooter = true; // "You loot..."
                    SendPacket(packet);
                }

                loot.LootMoney();

                // Delete the money loot record from the DB
                if (loot.loot_type == LootType.Item)
                    Global.LootItemStorage.RemoveStoredMoneyForContainer(guid.GetCounter());

                // Delete container if empty
                if (loot.IsLooted() && guid.IsItem())
                    forceLootRelease.Add(loot);
            }

            foreach (Loot loot in forceLootRelease)
                player.GetSession().DoLootRelease(loot);
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
                return IsValidAELootTarget(creature);
            }

            public bool IsValidLootTarget(Creature creature)
            {
                if (creature.IsAlive())
                    return false;

                if (!_looter.IsWithinDist(creature, LootDistance))
                    return false;

                return _looter.IsAllowedToLoot(creature);
            }

            bool IsValidAELootTarget(Creature creature)
            {
                if (creature.GetGUID() == _mainLootTarget)
                    return false;

                return IsValidLootTarget(creature);
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

            Creature lootTarget = ObjectAccessor.GetCreature(GetPlayer(), packet.Unit);
            if (lootTarget == null)
                return;

            AELootCreatureCheck check = new(_player, packet.Unit);
            if (!check.IsValidLootTarget(lootTarget))
                return;

            // interrupt cast
            if (GetPlayer().IsNonMeleeSpellCast(false))
                GetPlayer().InterruptNonMeleeSpells(false);

            GetPlayer().RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Looting);

            List<Creature> corpses = new();
            bool aeLootEnabled = WorldConfig.GetBoolValue(WorldCfg.EnableAeLoot);
            if (aeLootEnabled)
            {
                CreatureListSearcher searcher = new(_player, corpses, check);
                Cell.VisitGridObjects(_player, searcher, AELootCreatureCheck.LootDistance);
                if (corpses.Count > 49)
                    corpses.Resize(49); // lootTarget is 50th, not in corpses vector
            }

            if (!corpses.Empty())
                SendPacket(new AELootTargets((uint)corpses.Count + 1));

            GetPlayer().SendLoot(lootTarget.GetLootForPlayer(GetPlayer()));

            if (!corpses.Empty())
            {
                // main target
                SendPacket(new AELootTargetsAck());

                foreach (Creature creature in corpses)
                {
                    GetPlayer().SendLoot(creature.GetLootForPlayer(GetPlayer()), true);
                    SendPacket(new AELootTargetsAck());
                }
            }
        }

        [WorldPacketHandler(ClientOpcodes.LootRelease)]
        void HandleLootRelease(LootRelease packet)
        {
            // cheaters can modify lguid to prevent correct apply loot release code and re-loot
            // use internal stored guid
            Loot loot = GetPlayer().GetLootByWorldObjectGUID(packet.Unit);
            if (loot != null)
                DoLootRelease(loot);
        }

        public void DoLootRelease(Loot loot)
        {
            ObjectGuid lguid = loot.GetOwnerGUID();
            Player player = GetPlayer();

            if (player.GetLootGUID() == lguid)
                player.SetLootGUID(ObjectGuid.Empty);

            //Player is not looking at loot list, he doesn't need to see updates on the loot list
            loot.RemoveLooter(player.GetGUID());
            player.SendLootRelease(lguid);
            player.GetAELootView().Remove(loot.GetGUID());

            if (player.GetAELootView().Empty())
                player.RemoveUnitFlag(UnitFlags.Looting);

            if (!player.IsInWorld)
                return;

            if (lguid.IsGameObject())
            {
                GameObject go = player.GetMap().GetGameObject(lguid);

                // not check distance for GO in case owned GO (fishing bobber case, for example) or Fishing hole GO
                if (go == null || ((go.GetOwnerGUID() != player.GetGUID() && go.GetGoType() != GameObjectTypes.FishingHole) && !go.IsWithinDistInMap(player)))
                    return;

                if (loot.IsLooted() || go.GetGoType() == GameObjectTypes.FishingNode || go.GetGoType() == GameObjectTypes.FishingHole)
                {
                    if (go.GetGoType() == GameObjectTypes.FishingNode)
                    {
                        go.SetLootState(LootState.JustDeactivated);
                    }
                    else if (go.GetGoType() == GameObjectTypes.FishingHole)
                    {                                               // The fishing hole used once more
                        go.AddUse();                               // if the max usage is reached, will be despawned in next tick
                        if (go.GetUseCount() >= go.GetGoValue().FishingHole.MaxOpens)
                            go.SetLootState(LootState.JustDeactivated);
                        else
                            go.SetLootState(LootState.Ready);
                    }
                    else if (go.GetGoType() != GameObjectTypes.GatheringNode && go.IsFullyLooted())
                        go.SetLootState(LootState.JustDeactivated);

                    go.OnLootRelease(player);
                }
                else
                {
                    // not fully looted object
                    go.SetLootState(LootState.Activated, player);
                }
            }
            else if (lguid.IsCorpse())        // ONLY remove insignia at BG
            {
                Corpse corpse = ObjectAccessor.GetCorpse(player, lguid);
                if (corpse == null || !corpse.IsWithinDistInMap(player, SharedConst.InteractionDistance))
                    return;

                if (loot.IsLooted())
                {
                    corpse.loot = null;
                    corpse.RemoveCorpseDynamicFlag(CorpseDynFlags.Lootable);
                }
            }
            else if (lguid.IsItem())
            {
                Item pItem = player.GetItemByGuid(lguid);
                if (pItem == null)
                    return;

                ItemTemplate proto = pItem.GetTemplate();

                // destroy only 5 items from stack in case prospecting and milling
                if (loot.loot_type == LootType.Prospecting || loot.loot_type == LootType.Milling)
                {
                    pItem.m_lootGenerated = false;
                    pItem.loot = null;

                    uint count = pItem.GetCount();

                    // >=5 checked in spell code, but will work for cheating cases also with removing from another stacks.
                    if (count > 5)
                        count = 5;

                    player.DestroyItemCount(pItem, ref count, true);
                }
                else
                {
                    // Only delete item if no loot or money (unlooted loot is saved to db) or if it isn't an openable item
                    if (loot.IsLooted() || !proto.HasFlag(ItemFlags.HasLoot))
                        player.DestroyItem(pItem.GetBagSlot(), pItem.GetSlot(), true);
                }
                return;                                             // item can be looted only single player
            }
            else
            {
                Creature creature = player.GetMap().GetCreature(lguid);
                if (creature == null)
                    return;

                if (loot.IsLooted())
                {
                    if (creature.IsFullyLooted())
                    {
                        creature.RemoveDynamicFlag(UnitDynFlags.Lootable);

                        // skip pickpocketing loot for speed, skinning timer reduction is no-op in fact
                        if (!creature.IsAlive())
                            creature.AllLootRemovedFromCorpse();
                    }
                }
                else
                {
                    // if the round robin player release, reset it.
                    if (player.GetGUID() == loot.roundRobinPlayer)
                    {
                        loot.roundRobinPlayer.Clear();
                        loot.NotifyLootList(creature.GetMap());
                    }
                }

                // force dynflag update to update looter and lootable info
                creature.m_values.ModifyValue(creature.m_objectData).ModifyValue(creature.m_objectData.DynamicFlags);
                creature.ForceUpdateFieldChange();
            }
        }

        public void DoLootReleaseAll()
        {
            Dictionary<ObjectGuid, Loot> lootView = _player.GetAELootView();
            foreach (var (_, loot) in lootView)
                DoLootRelease(loot);
        }

        [WorldPacketHandler(ClientOpcodes.MasterLootItem)]
        void HandleLootMasterGive(MasterLootItem masterLootItem)
        {
            AELootResult aeResult = new();

            if (GetPlayer().GetGroup() == null || GetPlayer().GetGroup().GetLooterGuid() != GetPlayer().GetGUID())
            {
                GetPlayer().SendLootError(ObjectGuid.Empty, ObjectGuid.Empty, LootError.DidntKill);
                return;
            }

            // player on other map
            Player target = Global.ObjAccessor.GetPlayer(_player, masterLootItem.Target);
            if (target == null)
            {
                GetPlayer().SendLootError(ObjectGuid.Empty, ObjectGuid.Empty, LootError.PlayerNotFound);
                return;
            }

            foreach (LootRequest req in masterLootItem.Loot)
            {
                Loot loot = _player.GetAELootView().LookupByKey(req.Object);

                if (loot == null || loot.GetLootMethod() != LootMethod.MasterLoot)
                    return;

                if (!_player.IsInRaidWith(target) || !_player.IsInMap(target))
                {
                    _player.SendLootError(req.Object, loot.GetOwnerGUID(), LootError.MasterOther);
                    Log.outInfo(LogFilter.Cheat, $"MasterLootItem: Player {GetPlayer().GetName()} tried to give an item to ineligible player {target.GetName()} !");
                    return;
                }

                if (!loot.HasAllowedLooter(masterLootItem.Target))
                {
                    _player.SendLootError(req.Object, loot.GetOwnerGUID(), LootError.MasterOther);
                    return;
                }

                if (req.LootListID >= loot.items.Count)
                {
                    _player.SendLootError(req.Object, loot.GetOwnerGUID(), LootError.MasterOther);
                    Log.outDebug(LogFilter.Loot, $"MasterLootItem: Player {GetPlayer().GetName()} might be using a hack! (slot {req.LootListID}, size {loot.items.Count})");
                    return;
                }

                LootItem item = loot.items[req.LootListID];
                if (item.type != LootItemType.Item)
                {
                    _player.SendLootError(req.Object, loot.GetOwnerGUID(), LootError.MasterOther);
                    return;
                }

                List<ItemPosCount> dest = new();
                InventoryResult msg = target.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, item.itemid, item.count);
                if (!item.HasAllowedLooter(target.GetGUID()))
                    msg = InventoryResult.CantEquipEver;
                if (msg != InventoryResult.Ok)
                {
                    if (msg == InventoryResult.ItemMaxCount)
                        _player.SendLootError(req.Object, loot.GetOwnerGUID(), LootError.MasterUniqueItem);
                    else if (msg == InventoryResult.InvFull)
                        _player.SendLootError(req.Object, loot.GetOwnerGUID(), LootError.MasterInvFull);
                    else
                        _player.SendLootError(req.Object, loot.GetOwnerGUID(), LootError.MasterOther);

                    return;
                }

                // now move item from loot to target inventory
                Item newitem = target.StoreNewItem(dest, item.itemid, true, item.randomBonusListId, item.GetAllowedLooters(), item.context, item.BonusListIDs);
                if (newitem != null)
                    aeResult.Add(newitem, (byte)item.count, loot.loot_type, loot.GetDungeonEncounterId());
                else
                    target.ApplyItemLootedSpell(Global.ObjectMgr.GetItemTemplate(item.itemid));

                // mark as looted
                item.count = 0;
                item.is_looted = true;

                loot.NotifyItemRemoved(req.LootListID, GetPlayer().GetMap());
                --loot.unlootedCount;
            }

            foreach (var resultValue in aeResult.GetByOrder())
            {
                target.SendNewItem(resultValue.item, resultValue.count, false, false, true);
                target.UpdateCriteria(CriteriaType.LootItem, resultValue.item.GetEntry(), resultValue.count);
                target.UpdateCriteria(CriteriaType.GetLootByType, resultValue.item.GetEntry(), resultValue.count, (ulong)resultValue.lootType);
                target.UpdateCriteria(CriteriaType.LootAnyItem, resultValue.item.GetEntry(), resultValue.count);
            }
        }

        [WorldPacketHandler(ClientOpcodes.LootRoll)]
        void HandleLootRoll(LootRollPacket packet)
        {
            LootRoll lootRoll = GetPlayer().GetLootRoll(packet.LootObj, packet.LootListID);
            if (lootRoll == null)
                return;

            lootRoll.PlayerVote(GetPlayer(), packet.RollType);
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
