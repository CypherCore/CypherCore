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
using Game.Network.Packets;

namespace Game.Entities
{
    public class TradeData
    {
        public TradeData(Player player, Player trader)
        {
            m_player = player;
            m_trader = trader;
            m_clientStateIndex = 1;
            m_serverStateIndex = 1;
        }

        public TradeData GetTraderData()
        {
            return m_trader.GetTradeData();
        }

        public Item GetItem(TradeSlots slot)
        {
            return !m_items[(int)slot].IsEmpty() ? m_player.GetItemByGuid(m_items[(int)slot]) : null;
        }

        public bool HasItem(ObjectGuid itemGuid)
        {
            for (byte i = 0; i < (byte)TradeSlots.Count; ++i)
                if (m_items[i] == itemGuid)
                    return true;

            return false;
        }

        public TradeSlots GetTradeSlotForItem(ObjectGuid itemGuid)
        {
            for (TradeSlots i = 0; i < TradeSlots.Count; ++i)
                if (m_items[(int)i] == itemGuid)
                    return i;

            return TradeSlots.Invalid;
        }

        public Item GetSpellCastItem()
        {
            return !m_spellCastItem.IsEmpty() ? m_player.GetItemByGuid(m_spellCastItem) : null;
        }

        public void SetItem(TradeSlots slot, Item item, bool update = false)
        {
            ObjectGuid itemGuid = item ? item.GetGUID() : ObjectGuid.Empty;

            if (m_items[(int)slot] == itemGuid && !update)
                return;

            m_items[(int)slot] = itemGuid;

            SetAccepted(false);
            GetTraderData().SetAccepted(false);

            UpdateServerStateIndex();

            Update();

            // need remove possible trader spell applied to changed item
            if (slot == TradeSlots.NonTraded)
                GetTraderData().SetSpell(0);

            // need remove possible player spell applied (possible move reagent)
            SetSpell(0);
        }

        public uint GetSpell() { return m_spell; }

        public void SetSpell(uint spell_id, Item castItem = null)
        {
            ObjectGuid itemGuid = castItem ? castItem.GetGUID() : ObjectGuid.Empty;

            if (m_spell == spell_id && m_spellCastItem == itemGuid)
                return;

            m_spell = spell_id;
            m_spellCastItem = itemGuid;

            SetAccepted(false);
            GetTraderData().SetAccepted(false);

            UpdateServerStateIndex();

            Update(true);                                           // send spell info to item owner
            Update(false);                                          // send spell info to caster self
        }

        public void SetMoney(ulong money)
        {
            if (m_money == money)
                return;

            if (!m_player.HasEnoughMoney(money))
            {
                TradeStatusPkt info = new TradeStatusPkt();
                info.Status = TradeStatus.Failed;
                info.BagResult = InventoryResult.NotEnoughMoney;
                m_player.GetSession().SendTradeStatus(info);
                return;
            }
            m_money = money;

            SetAccepted(false);
            GetTraderData().SetAccepted(false);

            UpdateServerStateIndex();

            Update(true);
        }

        void Update(bool forTarget = true)
        {
            if (forTarget)
                m_trader.GetSession().SendUpdateTrade(true);      // player state for trader
            else
                m_player.GetSession().SendUpdateTrade(false);     // player state for player
        }

        public void SetAccepted(bool state, bool crosssend = false)
        {
            m_accepted = state;

            if (!state)
            {
                TradeStatusPkt info = new TradeStatusPkt();
                info.Status = TradeStatus.Unaccepted;
                if (crosssend)
                    m_trader.GetSession().SendTradeStatus(info);
                else
                    m_player.GetSession().SendTradeStatus(info);
            }
        }

        public Player GetTrader() { return m_trader; }

        public bool HasSpellCastItem() { return !m_spellCastItem.IsEmpty(); }

        public ulong GetMoney() { return m_money; }

        public bool IsAccepted() { return m_accepted; }

        public bool IsInAcceptProcess() { return m_acceptProccess; }

        public void SetInAcceptProcess(bool state) { m_acceptProccess = state; }

        public uint GetClientStateIndex() { return m_clientStateIndex; }
        public void UpdateClientStateIndex() { ++m_clientStateIndex; }

        public uint GetServerStateIndex() { return m_serverStateIndex; }
        public void UpdateServerStateIndex() { m_serverStateIndex = RandomHelper.Rand32(); }

        Player m_player;
        Player m_trader;
        bool m_accepted;
        bool m_acceptProccess;
        ulong m_money;
        uint m_spell;
        ObjectGuid m_spellCastItem;
        ObjectGuid[] m_items = new ObjectGuid[(int)TradeSlots.Count];
        uint m_clientStateIndex;
        uint m_serverStateIndex;
    }
}
