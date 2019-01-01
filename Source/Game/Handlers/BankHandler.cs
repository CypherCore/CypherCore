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
using System.Collections.Generic;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.AutobankItem)]
        void HandleAutoBankItem(AutoBankItem packet)
        {
            if (!CanUseBank())
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleAutoBankItemOpcode - {0} not found or you can't interact with him.", m_currentBankerGUID.ToString());
                return;
            }

            Item item = GetPlayer().GetItemByPos(packet.Bag, packet.Slot);
            if (!item)
                return;

            List<ItemPosCount> dest = new List<ItemPosCount>();
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

        [WorldPacketHandler(ClientOpcodes.BankerActivate)]
        void HandleBankerActivate(Hello packet)
        {
            Creature unit = GetPlayer().GetNPCIfCanInteractWith(packet.Unit, NPCFlags.Banker);
            if (!unit)
            {
                Log.outError(LogFilter.Network, "HandleBankerActivate: {0} not found or you can not interact with him.", packet.Unit.ToString());
                return;
            }

            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            SendShowBank(packet.Unit);
        }

        [WorldPacketHandler(ClientOpcodes.AutostoreBankItem)]
        void HandleAutoStoreBankItem(AutoStoreBankItem packet)
        {
            if (!CanUseBank())
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleAutoBankItemOpcode - {0} not found or you can't interact with him.", m_currentBankerGUID.ToString());
                return;
            }

            Item item = GetPlayer().GetItemByPos(packet.Bag, packet.Slot);
            if (!item)
                return;

            if (Player.IsBankPos(packet.Bag, packet.Slot))                 // moving from bank to inventory
            {
                List<ItemPosCount> dest = new List<ItemPosCount>();
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
                List<ItemPosCount> dest = new List<ItemPosCount>();
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

        [WorldPacketHandler(ClientOpcodes.BuyBankSlot)]
        void HandleBuyBankSlot(BuyBankSlot packet)
        {
            if (!CanUseBank(packet.Guid))
                Log.outDebug(LogFilter.Network, "WORLD: HandleBuyBankSlot - {0} not found or you can't interact with him.", packet.Guid.ToString());

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
            GetPlayer().UpdateCriteria(CriteriaTypes.BuyBankSlot);
        }

        public void SendShowBank(ObjectGuid guid)
        {
            m_currentBankerGUID = guid;
            ShowBank packet = new ShowBank();
            packet.Guid = guid;
            SendPacket(packet);
        }
    }
}
