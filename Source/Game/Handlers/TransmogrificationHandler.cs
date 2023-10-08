// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;
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
            if (player.GetNPCIfCanInteractWith(transmogrifyItems.Npc, NPCFlags.Transmogrifier, NPCFlags2.None) == null)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleTransmogrifyItems - Unit (GUID: {0}) not found or player can't interact with it.", transmogrifyItems.ToString());
                return;
            }

            long cost = 0;
            Dictionary<Item, uint[]> transmogItems = new();// new Dictionary<Item, Tuple<uint, uint>>();
            Dictionary<Item, uint> illusionItems = new();

            List<Item> resetAppearanceItems = new();
            List<Item> resetIllusionItems = new();
            List<uint> bindAppearances = new();

            bool validateAndStoreTransmogItem(Item itemTransmogrified, uint itemModifiedAppearanceId, bool isSecondary)
            {
                var itemModifiedAppearance = CliDB.ItemModifiedAppearanceStorage.LookupByKey(itemModifiedAppearanceId);
                if (itemModifiedAppearance == null)
                {
                    Log.outDebug(LogFilter.Network, $"WORLD: HandleTransmogrifyItems - {player.GetGUID()}, Name: {player.GetName()} tried to transmogrify using invalid appearance ({itemModifiedAppearanceId}).");
                    return false;
                }

                if (isSecondary && itemTransmogrified.GetTemplate().GetInventoryType() != InventoryType.Shoulders)
                {
                    Log.outDebug(LogFilter.Network, $"WORLD: HandleTransmogrifyItems - {player.GetGUID()}, Name: {player.GetName()} tried to transmogrify secondary appearance to non-shoulder item.");
                    return false;
                }

                bool hasAppearance, isTemporary;
                (hasAppearance, isTemporary) = GetCollectionMgr().HasItemAppearance(itemModifiedAppearanceId);
                if (!hasAppearance)
                {
                    Log.outDebug(LogFilter.Network, $"WORLD: HandleTransmogrifyItems - {player.GetGUID()}, Name: {player.GetName()} tried to transmogrify using appearance he has not collected ({itemModifiedAppearanceId}).");
                    return false;
                }
                ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(itemModifiedAppearance.ItemID);
                if (player.CanUseItem(itemTemplate) != InventoryResult.Ok)
                {
                    Log.outDebug(LogFilter.Network, $"WORLD: HandleTransmogrifyItems - {player.GetGUID()}, Name: {player.GetName()} tried to transmogrify using appearance he can never use ({itemModifiedAppearanceId}).");
                    return false;
                }

                // validity of the transmogrification items
                if (!Item.CanTransmogrifyItemWithItem(itemTransmogrified, itemModifiedAppearance))
                {
                    Log.outDebug(LogFilter.Network, $"WORLD: HandleTransmogrifyItems - {player.GetGUID()}, Name: {player.GetName()} failed CanTransmogrifyItemWithItem ({itemTransmogrified.GetEntry()} with appearance {itemModifiedAppearanceId}).");
                    return false;
                }

                if (!transmogItems.ContainsKey(itemTransmogrified))
                    transmogItems[itemTransmogrified] = new uint[2];

                if (!isSecondary)
                    transmogItems[itemTransmogrified][0] = itemModifiedAppearanceId;
                else
                    transmogItems[itemTransmogrified][1] = itemModifiedAppearanceId;

                if (isTemporary)
                    bindAppearances.Add(itemModifiedAppearanceId);

                return true;
            };

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
                if (itemTransmogrified == null)
                {
                    Log.outDebug(LogFilter.Network, "WORLD: HandleTransmogrifyItems - Player (GUID: {0}, name: {1}) tried to transmogrify an invalid item in a valid slot (slot: {2}).", player.GetGUID().ToString(), player.GetName(), transmogItem.Slot);
                    return;
                }

                if (transmogItem.ItemModifiedAppearanceID != 0 || transmogItem.SecondaryItemModifiedAppearanceID > 0)
                {
                    if (transmogItem.ItemModifiedAppearanceID != 0 && !validateAndStoreTransmogItem(itemTransmogrified, (uint)transmogItem.ItemModifiedAppearanceID, false))
                        return;

                    if (transmogItem.SecondaryItemModifiedAppearanceID > 0 && !validateAndStoreTransmogItem(itemTransmogrified, (uint)transmogItem.SecondaryItemModifiedAppearanceID, true))
                        return;

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

                    TransmogIllusionRecord illusion = Global.DB2Mgr.GetTransmogIllusionForEnchantment((uint)transmogItem.SpellItemEnchantmentID);
                    if (illusion == null)
                    {
                        Log.outDebug(LogFilter.Network, "WORLD: HandleTransmogrifyItems - {0}, Name: {1} tried to transmogrify illusion using invalid enchant ({2}).", player.GetGUID().ToString(), player.GetName(), transmogItem.SpellItemEnchantmentID);
                        return;
                    }

                    var condition = CliDB.PlayerConditionStorage.LookupByKey(illusion.UnlockConditionID);
                    if (condition != null)
                    {
                        if (!ConditionManager.IsPlayerMeetingCondition(player, condition))
                        {
                            Log.outDebug(LogFilter.Network, "WORLD: HandleTransmogrifyItems - {0}, Name: {1} tried to transmogrify illusion using not allowed enchant ({2}).", player.GetGUID().ToString(), player.GetName(), transmogItem.SpellItemEnchantmentID);
                            return;
                        }
                    }

                    illusionItems[itemTransmogrified] = (uint)transmogItem.SpellItemEnchantmentID;
                    cost += illusion.TransmogCost;
                }
                else
                    resetIllusionItems.Add(itemTransmogrified);
            }

            if (!player.HasAuraType(AuraType.RemoveTransmogCost) && cost != 0) // 0 cost if reverting look
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
                    transmogrified.SetModifier(ItemModifier.TransmogAppearanceAllSpecs, transmogPair.Value[0]);
                    transmogrified.SetModifier(ItemModifier.TransmogAppearanceSpec1, 0);
                    transmogrified.SetModifier(ItemModifier.TransmogAppearanceSpec2, 0);
                    transmogrified.SetModifier(ItemModifier.TransmogAppearanceSpec3, 0);
                    transmogrified.SetModifier(ItemModifier.TransmogAppearanceSpec4, 0);

                    transmogrified.SetModifier(ItemModifier.TransmogSecondaryAppearanceAllSpecs, transmogPair.Value[1]);
                    transmogrified.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec1, 0);
                    transmogrified.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec2, 0);
                    transmogrified.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec3, 0);
                    transmogrified.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec4, 0);
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

                    if (transmogrified.GetModifier(ItemModifier.TransmogSecondaryAppearanceSpec1) == 0)
                        transmogrified.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec1, transmogrified.GetModifier(ItemModifier.TransmogSecondaryAppearanceAllSpecs));
                    if (transmogrified.GetModifier(ItemModifier.TransmogSecondaryAppearanceSpec2) == 0)
                        transmogrified.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec2, transmogrified.GetModifier(ItemModifier.TransmogSecondaryAppearanceAllSpecs));
                    if (transmogrified.GetModifier(ItemModifier.TransmogSecondaryAppearanceSpec3) == 0)
                        transmogrified.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec3, transmogrified.GetModifier(ItemModifier.TransmogSecondaryAppearanceAllSpecs));
                    if (transmogrified.GetModifier(ItemModifier.TransmogSecondaryAppearanceSpec4) == 0)
                        transmogrified.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec4, transmogrified.GetModifier(ItemModifier.TransmogSecondaryAppearanceAllSpecs));

                    transmogrified.SetModifier(ItemConst.AppearanceModifierSlotBySpec[player.GetActiveTalentGroup()], transmogPair.Value[0]);
                    transmogrified.SetModifier(ItemConst.SecondaryAppearanceModifierSlotBySpec[player.GetActiveTalentGroup()], transmogPair.Value[1]);
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

                    item.SetModifier(ItemModifier.TransmogSecondaryAppearanceAllSpecs, 0);
                    item.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec1, 0);
                    item.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec2, 0);
                    item.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec3, 0);
                    item.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec4, 0);
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

                    if (item.GetModifier(ItemModifier.TransmogSecondaryAppearanceSpec1) == 0)
                        item.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec1, item.GetModifier(ItemModifier.TransmogSecondaryAppearanceAllSpecs));
                    if (item.GetModifier(ItemModifier.TransmogSecondaryAppearanceSpec2) == 0)
                        item.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec2, item.GetModifier(ItemModifier.TransmogSecondaryAppearanceAllSpecs));
                    if (item.GetModifier(ItemModifier.TransmogSecondaryAppearanceSpec3) == 0)
                        item.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec3, item.GetModifier(ItemModifier.TransmogSecondaryAppearanceAllSpecs));
                    if (item.GetModifier(ItemModifier.TransmogSecondaryAppearanceSpec4) == 0)
                        item.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec4, item.GetModifier(ItemModifier.TransmogSecondaryAppearanceAllSpecs));

                    item.SetModifier(ItemConst.AppearanceModifierSlotBySpec[player.GetActiveTalentGroup()], 0);
                    item.SetModifier(ItemConst.SecondaryAppearanceModifierSlotBySpec[player.GetActiveTalentGroup()], 0);
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
                    if (item != null)
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
            NPCInteractionOpenResult npcInteraction = new();
            npcInteraction.Npc = guid;
            npcInteraction.InteractionType = PlayerInteractionType.Transmogrifier;
            npcInteraction.Success = true;
            SendPacket(npcInteraction);
        }
    }
}
