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
using Game.DataStorage;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;
using System.Collections.Generic;

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

            Item item = GetPlayer().GetItemByPos(packet.Bag, packet.Slot);
            if (!item)
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
        void HandleBankerActivate(Hello packet)
        {
            Creature unit = GetPlayer().GetNPCIfCanInteractWith(packet.Unit, NPCFlags.Banker, NPCFlags2.None);
            if (!unit)
            {
                Log.outError(LogFilter.Network, "HandleBankerActivate: {0} not found or you can not interact with him.", packet.Unit.ToString());
                return;
            }

            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            SendShowBank(packet.Unit);
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
            if (!item)
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
                if (storedItem)
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

        [WorldPacketHandler(ClientOpcodes.BuyBankSlot, Processing = PacketProcessing.Inplace)]
        void HandleBuyBankSlot(BuyBankSlot packet)
        {
            if (!CanUseBank(packet.Guid))
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleBuyBankSlot - {0} not found or you can't interact with him.", packet.Guid.ToString());
                return;
            }

            uint slot = GetPlayer().GetBankBagSlotCount();
            // next slot
            ++slot;

            BankBagSlotPricesRecord slotEntry = CliDB.BankBagSlotPricesStorage.LookupByKey(slot);
            if (slotEntry == null)
                return;

            uint price = slotEntry.Cost;
            if (!GetPlayer().HasEnoughMoney(price))
                return;

            GetPlayer().SetBankBagSlotCount((byte)slot);
            GetPlayer().ModifyMoney(-price);
            GetPlayer().UpdateCriteria(CriteriaType.BankSlotsPurchased);
        }

        [WorldPacketHandler(ClientOpcodes.BuyReagentBank)]
        void HandleBuyReagentBank(ReagentBank reagentBank)
        {
            if (!CanUseBank(reagentBank.Banker))
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleBuyReagentBankOpcode - {reagentBank.Banker} not found or you can't interact with him.");
                return;
            }

            if (_player.IsReagentBankUnlocked())
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleBuyReagentBankOpcode - Player ({_player.GetGUID()}, name: {_player.GetName()}) tried to unlock reagent bank a 2nd time.");
                return;
            }

            long price = 100 * MoneyConstants.Gold;

            if (!_player.HasEnoughMoney(price))
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleBuyReagentBankOpcode - Player ({_player.GetGUID()}, name: {_player.GetName()}) without enough gold.");
                return;
            }

            _player.ModifyMoney(-price);
            _player.UnlockReagentBank();
        }

        [WorldPacketHandler(ClientOpcodes.DepositReagentBank)]
        void HandleReagentBankDeposit(ReagentBank reagentBank)
        {
            if (!CanUseBank(reagentBank.Banker))
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleReagentBankDepositOpcode - {reagentBank.Banker} not found or you can't interact with him.");
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
                        _player.SendEquipError(msg, item);
                    break;
                }

                if (dest.Count == 1 && dest[0].pos == item.GetPos())
                {
                    _player.SendEquipError(InventoryResult.CantSwap, item);
                    continue;
                }

                // store reagent
                _player.RemoveItem(item.GetBagSlot(), item.GetSlot(), true);
                _player.BankItem(dest, item, true);
                anyDeposited = true;
            }
        }

        [WorldPacketHandler(ClientOpcodes.AutobankReagent)]
        void HandleAutoBankReagent(AutoBankReagent autoBankReagent)
        {
            if (!CanUseBank())
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleAutoBankReagentOpcode - {_player.PlayerTalkClass.GetInteractionData().SourceGuid} not found or you can't interact with him.");
                return;
            }

            if (!_player.IsReagentBankUnlocked())
            {
                _player.SendEquipError(InventoryResult.ReagentBankLocked);
                return;
            }

            Item item = _player.GetItemByPos(autoBankReagent.PackSlot, autoBankReagent.Slot);
            if (!item)
                return;

            List<ItemPosCount> dest = new();
            InventoryResult msg = _player.CanBankItem(ItemConst.NullBag, ItemConst.NullSlot, dest, item, false, true, true);
            if (msg != InventoryResult.Ok)
            {
                _player.SendEquipError(msg, item);
                return;
            }

            if (dest.Count == 1 && dest[0].pos == item.GetPos())
            {
                _player.SendEquipError(InventoryResult.CantSwap, item);
                return;
            }

            _player.RemoveItem(autoBankReagent.PackSlot, autoBankReagent.Slot, true);
            _player.BankItem(dest, item, true);
        }

        [WorldPacketHandler(ClientOpcodes.AutostoreBankReagent)]
        void HandleAutoStoreBankReagent(AutoStoreBankReagent autoStoreBankReagent)
        {
            if (!CanUseBank())
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleAutoBankReagentOpcode - {_player.PlayerTalkClass.GetInteractionData().SourceGuid} not found or you can't interact with him.");
                return;
            }

            if (!_player.IsReagentBankUnlocked())
            {
                _player.SendEquipError(InventoryResult.ReagentBankLocked);
                return;
            }

            Item pItem = _player.GetItemByPos(autoStoreBankReagent.Slot, autoStoreBankReagent.PackSlot);
            if (!pItem)
                return;

            if (Player.IsReagentBankPos(autoStoreBankReagent.Slot, autoStoreBankReagent.PackSlot))
            {
                List<ItemPosCount> dest = new();
                InventoryResult msg = _player.CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, dest, pItem, false);
                if (msg != InventoryResult.Ok)
                {
                    _player.SendEquipError(msg, pItem);
                    return;
                }

                _player.RemoveItem(autoStoreBankReagent.Slot, autoStoreBankReagent.PackSlot, true);
                _player.StoreItem(dest, pItem, true);
            }
            else
            {
                List<ItemPosCount> dest = new();
                InventoryResult msg = _player.CanBankItem(ItemConst.NullBag, ItemConst.NullSlot, dest, pItem, false, true, true);
                if (msg != InventoryResult.Ok)
                {
                    _player.SendEquipError(msg, pItem);
                    return;
                }

                _player.RemoveItem(autoStoreBankReagent.Slot, autoStoreBankReagent.PackSlot, true);
                _player.BankItem(dest, pItem, true);
            }
        }

        public void SendShowBank(ObjectGuid guid)
        {
            _player.PlayerTalkClass.GetInteractionData().Reset();
            _player.PlayerTalkClass.GetInteractionData().SourceGuid = guid;
            NPCInteractionOpenResult npcInteraction = new();
            npcInteraction.Npc = guid;
            npcInteraction.InteractionType = PlayerInteractionType.Banker;
            npcInteraction.Success = true;
            SendPacket(npcInteraction);
        }
    }
}
