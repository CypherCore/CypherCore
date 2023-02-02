﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;
using System.Collections.Generic;

namespace Game
{
    public partial class WorldSession
    {
        public void SendVoidStorageTransferResult(VoidTransferError result)
        {
            SendPacket(new VoidTransferResult(result));
        }

        [WorldPacketHandler(ClientOpcodes.UnlockVoidStorage, Processing = PacketProcessing.Inplace)]
        void HandleVoidStorageUnlock(UnlockVoidStorage unlockVoidStorage)
        {
            Creature unit = GetPlayer().GetNPCIfCanInteractWith(unlockVoidStorage.Npc, NPCFlags.VaultKeeper, NPCFlags2.None);
            if (!unit)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleVoidStorageUnlock - {0} not found or player can't interact with it.", unlockVoidStorage.Npc.ToString());
                return;
            }

            if (GetPlayer().IsVoidStorageUnlocked())
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleVoidStorageUnlock - Player({0}, name: {1}) tried to unlock void storage a 2nd time.", GetPlayer().GetGUID().ToString(), GetPlayer().GetName());
                return;
            }

            GetPlayer().ModifyMoney(-SharedConst.VoidStorageUnlockCost);
            GetPlayer().UnlockVoidStorage();
        }

        [WorldPacketHandler(ClientOpcodes.QueryVoidStorage, Processing = PacketProcessing.Inplace)]
        void HandleVoidStorageQuery(QueryVoidStorage queryVoidStorage)
        {
            Player player = GetPlayer();

            Creature unit = player.GetNPCIfCanInteractWith(queryVoidStorage.Npc, NPCFlags.Transmogrifier | NPCFlags.VaultKeeper, NPCFlags2.None);
            if (!unit)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleVoidStorageQuery - {0} not found or player can't interact with it.", queryVoidStorage.Npc.ToString());
                SendPacket(new VoidStorageFailed());
                return;
            }

            if (!GetPlayer().IsVoidStorageUnlocked())
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleVoidStorageQuery - {0} name: {1} queried void storage without unlocking it.", player.GetGUID().ToString(), player.GetName());
                SendPacket(new VoidStorageFailed());
                return;
            }

            VoidStorageContents voidStorageContents = new();
            for (byte i = 0; i < SharedConst.VoidStorageMaxSlot; ++i)
            {
                VoidStorageItem item = player.GetVoidStorageItem(i);
                if (item == null)
                    continue;

                VoidItem voidItem = new();
                voidItem.Guid = ObjectGuid.Create(HighGuid.Item, item.ItemId);
                voidItem.Creator = item.CreatorGuid;
                voidItem.Slot = i;
                voidItem.Item = new ItemInstance(item);

                voidStorageContents.Items.Add(voidItem);
            }

            SendPacket(voidStorageContents);
        }

        [WorldPacketHandler(ClientOpcodes.VoidStorageTransfer)]
        void HandleVoidStorageTransfer(VoidStorageTransfer voidStorageTransfer)
        {
            Player player = GetPlayer();

            Creature unit = player.GetNPCIfCanInteractWith(voidStorageTransfer.Npc, NPCFlags.VaultKeeper, NPCFlags2.None);
            if (!unit)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleVoidStorageTransfer - {0} not found or player can't interact with it.", voidStorageTransfer.Npc.ToString());
                return;
            }

            if (!player.IsVoidStorageUnlocked())
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleVoidStorageTransfer - Player ({0}, name: {1}) queried void storage without unlocking it.", player.GetGUID().ToString(), player.GetName());
                return;
            }

            if (voidStorageTransfer.Deposits.Length > player.GetNumOfVoidStorageFreeSlots())
            {
                SendVoidStorageTransferResult(VoidTransferError.Full);
                return;
            }

            uint freeBagSlots = 0;
            if (!voidStorageTransfer.Withdrawals.Empty())
            {
                // make this a Player function
                for (byte i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
                {
                    Bag bag = player.GetBagByPos(i);
                    if (bag)
                        freeBagSlots += bag.GetFreeSlots();
                }
                int inventoryEnd = InventorySlots.ItemStart + _player.GetInventorySlotCount();
                for (byte i = InventorySlots.ItemStart; i < inventoryEnd; i++)
                {
                    if (!player.GetItemByPos(InventorySlots.Bag0, i))
                        ++freeBagSlots;
                }
            }

            if (voidStorageTransfer.Withdrawals.Length > freeBagSlots)
            {
                SendVoidStorageTransferResult(VoidTransferError.InventoryFull);
                return;
            }

            if (!player.HasEnoughMoney((voidStorageTransfer.Deposits.Length * SharedConst.VoidStorageStoreItemCost)))
            {
                SendVoidStorageTransferResult(VoidTransferError.NotEnoughMoney);
                return;
            }

            VoidStorageTransferChanges voidStorageTransferChanges = new();

            byte depositCount = 0;
            for (int i = 0; i < voidStorageTransfer.Deposits.Length; ++i)
            {
                Item item = player.GetItemByGuid(voidStorageTransfer.Deposits[i]);
                if (!item)
                {
                    Log.outDebug(LogFilter.Network, "WORLD: HandleVoidStorageTransfer - {0} {1} wants to deposit an invalid item ({2}).", player.GetGUID().ToString(), player.GetName(), voidStorageTransfer.Deposits[i].ToString());
                    continue;
                }

                VoidStorageItem itemVS = new(Global.ObjectMgr.GenerateVoidStorageItemId(), item.GetEntry(), item.GetCreator(), 
                    item.GetItemRandomBonusListId(), item.GetModifier(ItemModifier.TimewalkerLevel), item.GetModifier(ItemModifier.ArtifactKnowledgeLevel), 
                    item.GetContext(), item.GetBonusListIDs());

                VoidItem voidItem;
                voidItem.Guid = ObjectGuid.Create(HighGuid.Item, itemVS.ItemId);
                voidItem.Creator = item.GetCreator();
                voidItem.Item = new ItemInstance(itemVS);
                voidItem.Slot = _player.AddVoidStorageItem(itemVS);

                voidStorageTransferChanges.AddedItems.Add(voidItem);

                player.DestroyItem(item.GetBagSlot(), item.GetSlot(), true);
                ++depositCount;
            }

            long cost = depositCount * SharedConst.VoidStorageStoreItemCost;

            player.ModifyMoney(-cost);

            for (int i = 0; i < voidStorageTransfer.Withdrawals.Length; ++i)
            {
                byte slot;
                VoidStorageItem itemVS = player.GetVoidStorageItem(voidStorageTransfer.Withdrawals[i].GetCounter(), out slot);
                if (itemVS == null)
                {
                    Log.outDebug(LogFilter.Network, "WORLD: HandleVoidStorageTransfer - {0} {1} tried to withdraw an invalid item ({2})", player.GetGUID().ToString(), player.GetName(), voidStorageTransfer.Withdrawals[i].ToString());
                    continue;
                }

                List<ItemPosCount> dest = new();
                InventoryResult msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, itemVS.ItemEntry, 1);
                if (msg != InventoryResult.Ok)
                {
                    SendVoidStorageTransferResult(VoidTransferError.InventoryFull);
                    Log.outDebug(LogFilter.Network, "WORLD: HandleVoidStorageTransfer - {0} {1} couldn't withdraw {2} because inventory was full.", player.GetGUID().ToString(), player.GetName(), voidStorageTransfer.Withdrawals[i].ToString());
                    return;
                }

                Item item = player.StoreNewItem(dest, itemVS.ItemEntry, true, itemVS.RandomBonusListId, null, itemVS.Context, itemVS.BonusListIDs);
                item.SetCreator(itemVS.CreatorGuid);
                item.SetBinding(true);
                GetCollectionMgr().AddItemAppearance(item);

                voidStorageTransferChanges.RemovedItems.Add(ObjectGuid.Create(HighGuid.Item, itemVS.ItemId));

                player.DeleteVoidStorageItem(slot);
            }

            SendPacket(voidStorageTransferChanges);
            SendVoidStorageTransferResult(VoidTransferError.Ok);
        }

        [WorldPacketHandler(ClientOpcodes.SwapVoidItem, Processing = PacketProcessing.Inplace)]
        void HandleVoidSwapItem(SwapVoidItem swapVoidItem)
        { 
            Player player = GetPlayer();

            Creature unit = player.GetNPCIfCanInteractWith(swapVoidItem.Npc, NPCFlags.VaultKeeper, NPCFlags2.None);
            if (!unit)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleVoidSwapItem - {0} not found or player can't interact with it.", swapVoidItem.Npc.ToString());
                return;
            }

            if (!player.IsVoidStorageUnlocked())
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleVoidSwapItem - Player ({0}, name: {1}) queried void storage without unlocking it.", player.GetGUID().ToString(), player.GetName());
                return;
            }

            byte oldSlot;
            if (player.GetVoidStorageItem(swapVoidItem.VoidItemGuid.GetCounter(), out oldSlot) == null)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleVoidSwapItem - Player (GUID: {0}, name: {1}) requested swapping an invalid item (slot: {2}, itemid: {3}).", player.GetGUID().ToString(), player.GetName(), swapVoidItem.DstSlot, swapVoidItem.VoidItemGuid.ToString());
                return;
            }

            bool usedDestSlot = player.GetVoidStorageItem((byte)swapVoidItem.DstSlot) != null;
            ObjectGuid itemIdDest = ObjectGuid.Empty;
            if (usedDestSlot)
                itemIdDest = ObjectGuid.Create(HighGuid.Item, player.GetVoidStorageItem((byte)swapVoidItem.DstSlot).ItemId);

            if (!player.SwapVoidStorageItem(oldSlot, (byte)swapVoidItem.DstSlot))
            {
                SendVoidStorageTransferResult(VoidTransferError.InternalError1);
                return;
            }

            VoidItemSwapResponse voidItemSwapResponse = new();
            voidItemSwapResponse.VoidItemA = swapVoidItem.VoidItemGuid;
            voidItemSwapResponse.VoidItemSlotA = swapVoidItem.DstSlot;
            if (usedDestSlot)
            {
                voidItemSwapResponse.VoidItemB = itemIdDest;
                voidItemSwapResponse.VoidItemSlotB = oldSlot;
            }

            SendPacket(voidItemSwapResponse);
        }
    }
}
