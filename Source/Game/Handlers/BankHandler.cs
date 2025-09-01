// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Chat;
using Game.DataStorage;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.AutobankItem, Processing = PacketProcessing.Inplace)]
        void HandleAutoBankItem(AutoBankItem packet)
        {
            if (!CanUseBank())
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleAutoBankItemOpcode - {_player.PlayerTalkClass.GetInteractionData().SourceGuid} not found or you can't interact with him.");
                return;
            }

            if (packet.BankType != BankType.Character)
                return;

            Item item = GetPlayer().GetItemByPos(packet.Bag, packet.Slot);
            if (item == null)
                return;

            List<ItemPosCount> dest = new();
            InventoryResult msg = GetPlayer().CanBankItem(ItemConst.NullBag, ItemConst.NullSlot, dest, item, false);
            if (msg != InventoryResult.Ok)
            {
                GetPlayer().SendEquipError(msg, item);
                return;
            }

            if (dest.Count == 1 && dest[0].pos == item.GetPos())
            {
                GetPlayer().SendEquipError(InventoryResult.CantSwap, item);
                return;
            }

            GetPlayer().RemoveItem(packet.Bag, packet.Slot, true);
            GetPlayer().ItemRemovedQuestCheck(item.GetEntry(), item.GetCount());
            GetPlayer().BankItem(dest, item, true);
        }

        [WorldPacketHandler(ClientOpcodes.BankerActivate, Processing = PacketProcessing.Inplace)]
        void HandleBankerActivate(BankerActivate bankerActivate)
        {
            if (bankerActivate.InteractionType != PlayerInteractionType.Banker && bankerActivate.InteractionType != PlayerInteractionType.CharacterBanker)
                return;

            Creature unit = GetPlayer().GetNPCIfCanInteractWith(bankerActivate.Banker, NPCFlags.AccountBanker | NPCFlags.Banker, NPCFlags2.None);
            if (unit == null)
            {
                Log.outError(LogFilter.Network, $"HandleBankerActivate: {bankerActivate.Banker} not found or you can not interact with him.");
                return;
            }

            switch (bankerActivate.InteractionType)
            {
                case PlayerInteractionType.Banker:
                    if (!unit.HasNpcFlag(NPCFlags.AccountBanker) || !unit.HasNpcFlag(NPCFlags.Banker))
                        return;
                    break;
                case PlayerInteractionType.CharacterBanker:
                    if (!unit.HasNpcFlag(NPCFlags.Banker))
                        return;
                    break;
                case PlayerInteractionType.AccountBanker:
                    if (!unit.HasNpcFlag(NPCFlags.AccountBanker))
                        return;
                    break;
                default:
                    break;
            }

            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            SendShowBank(bankerActivate.Banker, bankerActivate.InteractionType);
        }

        [WorldPacketHandler(ClientOpcodes.AutostoreBankItem, Processing = PacketProcessing.Inplace)]
        void HandleAutoStoreBankItem(AutoStoreBankItem packet)
        {
            if (!CanUseBank())
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleAutoBankItemOpcode - {_player.PlayerTalkClass.GetInteractionData().SourceGuid} not found or you can't interact with him.");
                return;
            }

            Item item = GetPlayer().GetItemByPos(packet.Bag, packet.Slot);
            if (item == null)
                return;

            if (Player.IsBankPos(packet.Bag, packet.Slot))                 // moving from bank to inventory
            {
                List<ItemPosCount> dest = new();
                InventoryResult msg = GetPlayer().CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, dest, item, false);
                if (msg != InventoryResult.Ok)
                {
                    GetPlayer().SendEquipError(msg, item);
                    return;
                }

                GetPlayer().RemoveItem(packet.Bag, packet.Slot, true);
                Item storedItem = GetPlayer().StoreItem(dest, item, true);
                if (storedItem != null)
                    GetPlayer().ItemAddedQuestCheck(storedItem.GetEntry(), storedItem.GetCount());
            }
            else                                                    // moving from inventory to bank
            {
                List<ItemPosCount> dest = new();
                InventoryResult msg = GetPlayer().CanBankItem(ItemConst.NullBag, ItemConst.NullSlot, dest, item, false);
                if (msg != InventoryResult.Ok)
                {
                    GetPlayer().SendEquipError(msg, item);
                    return;
                }

                GetPlayer().RemoveItem(packet.Bag, packet.Slot, true);
                GetPlayer().BankItem(dest, item, true);
            }
        }

        [WorldPacketHandler(ClientOpcodes.BuyAccountBankTab)]
        void HandleBuyBankTab(BuyBankTab buyBankTab)
        {
            if (!CanUseBank(buyBankTab.Banker))
            {
                Log.outDebug(LogFilter.Network, $"WorldSession::HandleBuyBankTab {_player.GetGUID()} - Banker {buyBankTab.Banker} not found or can't interact with him.");
                return;
            }

            if (buyBankTab.BankType != BankType.Character)
            {
                Log.outDebug(LogFilter.Network, $"WorldSession::HandleBuyBankTab {_player.GetGUID()} - Bank type {buyBankTab.BankType} is not supported.");
                return;
            }

            uint itemId = 0;
            byte slot = 0;
            byte inventorySlot = 0;

            switch (buyBankTab.BankType)
            {
                case BankType.Character:
                    itemId = 242709;
                    slot = _player.GetCharacterBankTabCount();
                    inventorySlot = (byte)(InventorySlots.BankBagStart + slot);
                    break;
                case BankType.Account:
                    itemId = 208392;
                    slot = _player.GetAccountBankTabCount();
                    inventorySlot = (byte)(InventorySlots.AccountBankBagStart + slot);
                    break;
                default:
                    Log.outDebug(LogFilter.Network, $"WorldSession::HandleBuyBankTab {_player.GetGUID()} - Bank type {buyBankTab.BankType} is not supported.");
                    return;
            }

            var bankTab = CliDB.BankTabStorage.FirstOrDefault(record => record.Value.BankType == (byte)buyBankTab.BankType && record.Value.OrderIndex == slot).Value;
            if (bankTab == null)
                return;

            ulong price = bankTab.Cost;
            if (!_player.HasEnoughMoney(price))
                return;

            InventoryResult msg = _player.CanEquipNewItem(inventorySlot, out ushort inventoryPos, itemId, false);
            if (msg != InventoryResult.Ok)
            {
                _player.SendEquipError(msg, null, null, itemId);
                return;
            }

            Item bag = _player.EquipNewItem(inventoryPos, itemId, ItemContext.None, true);
            if (bag == null)
                return;

            switch (buyBankTab.BankType)
            {
                case BankType.Character:
                    _player.SetCharacterBankTabCount((byte)(slot + 1));
                    _player.SetCharacterBankTabSettings(slot, new CommandHandler(this).GetParsedString(CypherStrings.BankTabName, slot + 1), "", "", BagSlotFlags.None);
                    break;
                case BankType.Account:
                    _player.SetAccountBankTabCount((byte)(slot + 1));
                    _player.SetAccountBankTabSettings(slot, new CommandHandler(this).GetParsedString(CypherStrings.BankTabName, slot + 1), "", "", BagSlotFlags.None);
                    break;
                default:
                    break;
            }

            _player.ModifyMoney(-(long)price);

            _player.UpdateCriteria(CriteriaType.BankTabPurchased, (ulong)buyBankTab.BankType);
        }

        [WorldPacketHandler(ClientOpcodes.UpdateAccountBankTabSettings)]
        void HandleUpdateBankTabSettings(UpdateBankTabSettings updateBankTabSettings)
        {
            if (!CanUseBank(updateBankTabSettings.Banker))
            {
                Log.outDebug(LogFilter.Network, $"WorldSession::HandleUpdateBankTabSettings {_player.GetGUID()} - Banker {updateBankTabSettings.Banker} not found or can't interact with him.");
                return;
            }

            switch (updateBankTabSettings.BankType)
            {
                case BankType.Character:
                    if (updateBankTabSettings.Tab >= _player.m_activePlayerData.CharacterBankTabSettings.Size())
                    {
                        Log.outDebug(LogFilter.Network, $"WorldSession::HandleUpdateBankTabSettings {_player.GetGUID()} doesn't have bank tab {updateBankTabSettings.Tab} in bank type {updateBankTabSettings.BankType}.");
                        return;
                    }
                    _player.SetCharacterBankTabSettings(updateBankTabSettings.Tab, updateBankTabSettings.Settings.Name,
                        updateBankTabSettings.Settings.Icon, updateBankTabSettings.Settings.Description, updateBankTabSettings.Settings.DepositFlags);
                    break;
                case BankType.Account:
                    if (updateBankTabSettings.Tab >= _player.m_activePlayerData.AccountBankTabSettings.Size())
                    {
                        Log.outDebug(LogFilter.Network, $"WorldSession::HandleUpdateBankTabSettings {_player.GetGUID()} doesn't have bank tab {updateBankTabSettings.Tab} in bank type {updateBankTabSettings.BankType}.");
                        return;
                    }
                    _player.SetAccountBankTabSettings(updateBankTabSettings.Tab, updateBankTabSettings.Settings.Name,
                        updateBankTabSettings.Settings.Icon, updateBankTabSettings.Settings.Description, updateBankTabSettings.Settings.DepositFlags);
                    break;
                default:
                    Log.outDebug(LogFilter.Network, $"WorldSession::HandleUpdateBankTabSettings {_player.GetGUID()} - Bank type {updateBankTabSettings.BankType} is not supported.");
                    break;
            }
        }

        [WorldPacketHandler(ClientOpcodes.AutoDepositCharacterBank)]
        void HandleAutoDepositCharacterBank(AutoDepositCharacterBank autoDepositCharacterBank)
        {
            if (!CanUseBank(autoDepositCharacterBank.Banker))
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleAutoDepositCharacterBank - {autoDepositCharacterBank.Banker} not found or you can't interact with him.");
                return;
            }

            if (!_player.IsReagentBankUnlocked())
            {
                _player.SendEquipError(InventoryResult.ReagentBankLocked);
                return;
            }

            // query all reagents from player's inventory
            bool anyDeposited = false;
            foreach (Item item in _player.GetCraftingReagentItemsToDeposit())
            {
                List<ItemPosCount> dest = new();
                InventoryResult msg = _player.CanBankItem(ItemConst.NullBag, ItemConst.NullSlot, dest, item, false, true, true);
                if (msg != InventoryResult.Ok)
                {
                    if (msg != InventoryResult.ReagentBankFull || !anyDeposited)
                        _player.SendEquipError(msg, item, null);
                    break;
                }

                if (dest.Count == 1 && dest[0].pos == item.GetPos())
                {
                    _player.SendEquipError(InventoryResult.CantSwap, item, null);
                    continue;
                }

                // store reagent
                _player.RemoveItem(item.GetBagSlot(), item.GetSlot(), true);
                _player.BankItem(dest, item, true);
                anyDeposited = true;
            }

        }

        public void SendShowBank(ObjectGuid guid, PlayerInteractionType interactionType)
        {
            _player.PlayerTalkClass.GetInteractionData().StartInteraction(guid, interactionType);

            NPCInteractionOpenResult npcInteraction = new();
            npcInteraction.Npc = guid;
            npcInteraction.InteractionType = interactionType;
            npcInteraction.Success = true;
            SendPacket(npcInteraction);
        }
    }
}
