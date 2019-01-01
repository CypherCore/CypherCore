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
using Game.Network;
using Game.Network.Packets;
using System;
using System.Collections.Generic;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.TransmogrifyItems)]
        void HandleTransmogrifyItems(TransmogrifyItems transmogrifyItems)
        {
            Player player = GetPlayer();

            // Validate
            if (!player.GetNPCIfCanInteractWith(transmogrifyItems.Npc, NPCFlags.Transmogrifier))
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleTransmogrifyItems - Unit (GUID: {0}) not found or player can't interact with it.", transmogrifyItems.ToString());
                return;
            }

            long cost = 0;
            Dictionary<Item, uint> transmogItems = new Dictionary<Item, uint>();
            Dictionary<Item, uint> illusionItems = new Dictionary<Item, uint>();

            List<Item> resetAppearanceItems = new List<Item>();
            List<Item> resetIllusionItems = new List<Item>();
            List<uint> bindAppearances = new List<uint>();

            foreach (TransmogrifyItem transmogItem in transmogrifyItems.Items)
            {
                // slot of the transmogrified item
                if (transmogItem.Slot >= EquipmentSlot.End)
                {
                    Log.outDebug(LogFilter.Network, "WORLD: HandleTransmogrifyItems - Player ({0}, name: {1}) tried to transmogrify wrong slot {2} when transmogrifying items.", player.GetGUID().ToString(), player.GetName(), transmogItem.Slot);
                    return;
                }

                // transmogrified item
                Item itemTransmogrified = player.GetItemByPos(InventorySlots.Bag0, (byte)transmogItem.Slot);
                if (!itemTransmogrified)
                {
                    Log.outDebug(LogFilter.Network, "WORLD: HandleTransmogrifyItems - Player (GUID: {0}, name: {1}) tried to transmogrify an invalid item in a valid slot (slot: {2}).", player.GetGUID().ToString(), player.GetName(), transmogItem.Slot);
                    return;
                }

                if (transmogItem.ItemModifiedAppearanceID != 0)
                {
                    ItemModifiedAppearanceRecord itemModifiedAppearance = CliDB.ItemModifiedAppearanceStorage.LookupByKey(transmogItem.ItemModifiedAppearanceID);
                    if (itemModifiedAppearance == null)
                    {
                        Log.outDebug(LogFilter.Network, "WORLD: HandleTransmogrifyItems - {0}, Name: {1} tried to transmogrify using invalid appearance ({2}).", player.GetGUID().ToString(), player.GetName(), transmogItem.ItemModifiedAppearanceID);
                        return;
                    }

                    var pairValue = GetCollectionMgr().HasItemAppearance((uint)transmogItem.ItemModifiedAppearanceID);
                    if (!pairValue.Item1)
                    {
                        Log.outDebug(LogFilter.Network, "WORLD: HandleTransmogrifyItems - {0}, Name: {1} tried to transmogrify using appearance he has not collected ({2}).", player.GetGUID().ToString(), player.GetName(), transmogItem.ItemModifiedAppearanceID);
                        return;
                    }
                    ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(itemModifiedAppearance.ItemID);
                    if (player.CanUseItem(itemTemplate) != InventoryResult.Ok)
                    {
                        Log.outDebug(LogFilter.Network, "WORLD: HandleTransmogrifyItems - {0}, Name: {1} tried to transmogrify using appearance he can never use ({2}).", player.GetGUID().ToString(), player.GetName(), transmogItem.ItemModifiedAppearanceID);
                        return;
                    }

                    // validity of the transmogrification items
                    if (!Item.CanTransmogrifyItemWithItem(itemTransmogrified, itemModifiedAppearance))
                    {
                        Log.outDebug(LogFilter.Network, "WORLD: HandleTransmogrifyItems - {0}, Name: {1} failed CanTransmogrifyItemWithItem ({2} with appearance {3}).", player.GetGUID().ToString(), player.GetName(), itemTransmogrified.GetEntry(), transmogItem.ItemModifiedAppearanceID);
                        return;
                    }

                    transmogItems[itemTransmogrified] = (uint)transmogItem.ItemModifiedAppearanceID;
                    if (pairValue.Item2)
                        bindAppearances.Add((uint)transmogItem.ItemModifiedAppearanceID);

                    // add cost
                    cost += itemTransmogrified.GetSellPrice(_player);
                }
                else
                    resetAppearanceItems.Add(itemTransmogrified);

                if (transmogItem.SpellItemEnchantmentID != 0)
                {
                    if (transmogItem.Slot != EquipmentSlot.MainHand && transmogItem.Slot != EquipmentSlot.OffHand)
                    {
                        Log.outDebug(LogFilter.Network, "WORLD: HandleTransmogrifyItems - {0}, Name: {1} tried to transmogrify illusion into non-weapon slot ({2}).", player.GetGUID().ToString(), player.GetName(), transmogItem.Slot);
                        return;
                    }

                    SpellItemEnchantmentRecord illusion = CliDB.SpellItemEnchantmentStorage.LookupByKey(transmogItem.SpellItemEnchantmentID);
                    if (illusion == null)
                    {
                        Log.outDebug(LogFilter.Network, "WORLD: HandleTransmogrifyItems - {0}, Name: {1} tried to transmogrify illusion using invalid enchant ({2}).", player.GetGUID().ToString(), player.GetName(), transmogItem.SpellItemEnchantmentID);
                        return;
                    }

                    if (illusion.ItemVisual == 0 || !illusion.Flags.HasAnyFlag(EnchantmentSlotMask.Collectable))
                    {
                        Log.outDebug(LogFilter.Network, "WORLD: HandleTransmogrifyItems - {0}, Name: {1} tried to transmogrify illusion using not allowed enchant ({2}).", player.GetGUID().ToString(), player.GetName(), transmogItem.SpellItemEnchantmentID);
                        return;
                    }

                    PlayerConditionRecord condition = CliDB.PlayerConditionStorage.LookupByKey(illusion.TransmogPlayerConditionID);
                    if (condition != null)
                    {
                        if (!ConditionManager.IsPlayerMeetingCondition(player, condition))
                        {
                            Log.outDebug(LogFilter.Network, "WORLD: HandleTransmogrifyItems - {0}, Name: {1} tried to transmogrify illusion using not collected enchant ({2}).", player.GetGUID().ToString(), player.GetName(), transmogItem.SpellItemEnchantmentID);
                            return;
                        }
                    }

                    if (illusion.ScalingClassRestricted > 0 && illusion.ScalingClassRestricted != (byte)player.GetClass())
                    {
                        Log.outDebug(LogFilter.Network, "WORLD: HandleTransmogrifyItems - {0}, Name: {1} tried to transmogrify illusion using not allowed class enchant ({2}).", player.GetGUID().ToString(), player.GetName(), transmogItem.SpellItemEnchantmentID);
                        return;
                    }

                    illusionItems[itemTransmogrified] = (uint)transmogItem.SpellItemEnchantmentID;
                    cost += illusion.TransmogCost;
                }
                else
                    resetIllusionItems.Add(itemTransmogrified);
            }

            if (cost != 0) // 0 cost if reverting look
            {
                if (!player.HasEnoughMoney(cost))
                    return;

                player.ModifyMoney(-cost);
            }

            // Everything is fine, proceed
            foreach (var transmogPair in transmogItems)
            {
                Item transmogrified = transmogPair.Key;

                if (!transmogrifyItems.CurrentSpecOnly)
                {
                    transmogrified.SetModifier(ItemModifier.TransmogAppearanceAllSpecs, transmogPair.Value);
                    transmogrified.SetModifier(ItemModifier.TransmogAppearanceSpec1, 0);
                    transmogrified.SetModifier(ItemModifier.TransmogAppearanceSpec2, 0);
                    transmogrified.SetModifier(ItemModifier.TransmogAppearanceSpec3, 0);
                    transmogrified.SetModifier(ItemModifier.TransmogAppearanceSpec4, 0);
                }
                else
                {
                    if (transmogrified.GetModifier(ItemModifier.TransmogAppearanceSpec1) == 0)
                        transmogrified.SetModifier(ItemModifier.TransmogAppearanceSpec1, transmogrified.GetModifier(ItemModifier.TransmogAppearanceAllSpecs));
                    if (transmogrified.GetModifier(ItemModifier.TransmogAppearanceSpec2) == 0)
                        transmogrified.SetModifier(ItemModifier.TransmogAppearanceSpec2, transmogrified.GetModifier(ItemModifier.TransmogAppearanceAllSpecs));
                    if (transmogrified.GetModifier(ItemModifier.TransmogAppearanceSpec3) == 0)
                        transmogrified.SetModifier(ItemModifier.TransmogAppearanceSpec3, transmogrified.GetModifier(ItemModifier.TransmogAppearanceAllSpecs));
                    if (transmogrified.GetModifier(ItemModifier.TransmogAppearanceSpec4) == 0)
                        transmogrified.SetModifier(ItemModifier.TransmogAppearanceSpec4, transmogrified.GetModifier(ItemModifier.TransmogAppearanceAllSpecs));
                    transmogrified.SetModifier(ItemConst.AppearanceModifierSlotBySpec[player.GetActiveTalentGroup()], transmogPair.Value);
                }

                player.SetVisibleItemSlot(transmogrified.GetSlot(), transmogrified);

                transmogrified.SetNotRefundable(player);
                transmogrified.ClearSoulboundTradeable(player);
                transmogrified.SetState(ItemUpdateState.Changed, player);
            }

            foreach (var illusionPair in illusionItems)
            {
                Item transmogrified = illusionPair.Key;

                if (!transmogrifyItems.CurrentSpecOnly)
                {
                    transmogrified.SetModifier(ItemModifier.EnchantIllusionAllSpecs, illusionPair.Value);
                    transmogrified.SetModifier(ItemModifier.EnchantIllusionSpec1, 0);
                    transmogrified.SetModifier(ItemModifier.EnchantIllusionSpec2, 0);
                    transmogrified.SetModifier(ItemModifier.EnchantIllusionSpec3, 0);
                    transmogrified.SetModifier(ItemModifier.EnchantIllusionSpec4, 0);
                }
                else
                {
                    if (transmogrified.GetModifier(ItemModifier.EnchantIllusionSpec1) == 0)
                        transmogrified.SetModifier(ItemModifier.EnchantIllusionSpec1, transmogrified.GetModifier(ItemModifier.EnchantIllusionAllSpecs));
                    if (transmogrified.GetModifier(ItemModifier.EnchantIllusionSpec2) == 0)
                        transmogrified.SetModifier(ItemModifier.EnchantIllusionSpec2, transmogrified.GetModifier(ItemModifier.EnchantIllusionAllSpecs));
                    if (transmogrified.GetModifier(ItemModifier.EnchantIllusionSpec3) == 0)
                        transmogrified.SetModifier(ItemModifier.EnchantIllusionSpec3, transmogrified.GetModifier(ItemModifier.EnchantIllusionAllSpecs));
                    if (transmogrified.GetModifier(ItemModifier.EnchantIllusionSpec4) == 0)
                        transmogrified.SetModifier(ItemModifier.EnchantIllusionSpec4, transmogrified.GetModifier(ItemModifier.EnchantIllusionAllSpecs));
                    transmogrified.SetModifier(ItemConst.IllusionModifierSlotBySpec[player.GetActiveTalentGroup()], illusionPair.Value);
                }

                player.SetVisibleItemSlot(transmogrified.GetSlot(), transmogrified);

                transmogrified.SetNotRefundable(player);
                transmogrified.ClearSoulboundTradeable(player);
                transmogrified.SetState(ItemUpdateState.Changed, player);
            }

            foreach (Item item in resetAppearanceItems)
            {
                if (!transmogrifyItems.CurrentSpecOnly)
                {
                    item.SetModifier(ItemModifier.TransmogAppearanceAllSpecs, 0);
                    item.SetModifier(ItemModifier.TransmogAppearanceSpec1, 0);
                    item.SetModifier(ItemModifier.TransmogAppearanceSpec2, 0);
                    item.SetModifier(ItemModifier.TransmogAppearanceSpec3, 0);
                    item.SetModifier(ItemModifier.TransmogAppearanceSpec4, 0);
                }
                else
                {
                    if (item.GetModifier(ItemModifier.TransmogAppearanceSpec1) == 0)
                        item.SetModifier(ItemModifier.TransmogAppearanceSpec1, item.GetModifier(ItemModifier.TransmogAppearanceAllSpecs));
                    if (item.GetModifier(ItemModifier.TransmogAppearanceSpec2) == 0)
                        item.SetModifier(ItemModifier.TransmogAppearanceSpec2, item.GetModifier(ItemModifier.TransmogAppearanceAllSpecs));
                    if (item.GetModifier(ItemModifier.TransmogAppearanceSpec2) == 0)
                        item.SetModifier(ItemModifier.TransmogAppearanceSpec3, item.GetModifier(ItemModifier.TransmogAppearanceAllSpecs));
                    if (item.GetModifier(ItemModifier.TransmogAppearanceSpec4) == 0)
                        item.SetModifier(ItemModifier.TransmogAppearanceSpec4, item.GetModifier(ItemModifier.TransmogAppearanceAllSpecs));
                    item.SetModifier(ItemConst.AppearanceModifierSlotBySpec[player.GetActiveTalentGroup()], 0);
                    item.SetModifier(ItemModifier.EnchantIllusionAllSpecs, 0);
                }

                item.SetState(ItemUpdateState.Changed, player);
                player.SetVisibleItemSlot(item.GetSlot(), item);
            }

            foreach (Item item in resetIllusionItems)
            {
                if (!transmogrifyItems.CurrentSpecOnly)
                {
                    item.SetModifier(ItemModifier.EnchantIllusionAllSpecs, 0);
                    item.SetModifier(ItemModifier.EnchantIllusionSpec1, 0);
                    item.SetModifier(ItemModifier.EnchantIllusionSpec2, 0);
                    item.SetModifier(ItemModifier.EnchantIllusionSpec3, 0);
                    item.SetModifier(ItemModifier.EnchantIllusionSpec4, 0);
                }
                else
                {
                    if (item.GetModifier(ItemModifier.EnchantIllusionSpec1) == 0)
                        item.SetModifier(ItemModifier.EnchantIllusionSpec1, item.GetModifier(ItemModifier.EnchantIllusionAllSpecs));
                    if (item.GetModifier(ItemModifier.EnchantIllusionSpec2) == 0)
                        item.SetModifier(ItemModifier.EnchantIllusionSpec2, item.GetModifier(ItemModifier.EnchantIllusionAllSpecs));
                    if (item.GetModifier(ItemModifier.EnchantIllusionSpec3) == 0)
                        item.SetModifier(ItemModifier.EnchantIllusionSpec3, item.GetModifier(ItemModifier.EnchantIllusionAllSpecs));
                    if (item.GetModifier(ItemModifier.EnchantIllusionSpec4) == 0)
                        item.SetModifier(ItemModifier.EnchantIllusionSpec4, item.GetModifier(ItemModifier.EnchantIllusionAllSpecs));
                    item.SetModifier(ItemConst.IllusionModifierSlotBySpec[player.GetActiveTalentGroup()], 0);
                    item.SetModifier(ItemModifier.TransmogAppearanceAllSpecs, 0);
                }

                item.SetState(ItemUpdateState.Changed, player);
                player.SetVisibleItemSlot(item.GetSlot(), item);
            }

            foreach (uint itemModifedAppearanceId in bindAppearances)
            {
                var itemsProvidingAppearance = GetCollectionMgr().GetItemsProvidingTemporaryAppearance(itemModifedAppearanceId);
                foreach (ObjectGuid itemGuid in itemsProvidingAppearance)
                {
                    Item item = player.GetItemByGuid(itemGuid);
                    if (item)
                    {
                        item.SetNotRefundable(player);
                        item.ClearSoulboundTradeable(player);
                        GetCollectionMgr().AddItemAppearance(item);
                    }
                }
            }
        }

        public void SendOpenTransmogrifier(ObjectGuid guid)
        {
            SendPacket(new OpenTransmogrifier(guid));
        }
    }
}
