// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Networking.Packets;

namespace Game.Entities
{
	public class TradeData
	{
		private bool _accepted;
		private bool _acceptProccess;
		private uint _clientStateIndex;
		private ObjectGuid[] _items = new ObjectGuid[(int)TradeSlots.Count];
		private ulong _money;

		private Player _player;
		private uint _serverStateIndex;
		private uint _spell;
		private ObjectGuid _spellCastItem;
		private Player _trader;

		public TradeData(Player player, Player trader)
		{
			_player           = player;
			_trader           = trader;
			_clientStateIndex = 1;
			_serverStateIndex = 1;
		}

		public TradeData GetTraderData()
		{
			return _trader.GetTradeData();
		}

		public Item GetItem(TradeSlots slot)
		{
			return !_items[(int)slot].IsEmpty() ? _player.GetItemByGuid(_items[(int)slot]) : null;
		}

		public bool HasItem(ObjectGuid itemGuid)
		{
			for (byte i = 0; i < (byte)TradeSlots.Count; ++i)
				if (_items[i] == itemGuid)
					return true;

			return false;
		}

		public TradeSlots GetTradeSlotForItem(ObjectGuid itemGuid)
		{
			for (TradeSlots i = 0; i < TradeSlots.Count; ++i)
				if (_items[(int)i] == itemGuid)
					return i;

			return TradeSlots.Invalid;
		}

		public Item GetSpellCastItem()
		{
			return !_spellCastItem.IsEmpty() ? _player.GetItemByGuid(_spellCastItem) : null;
		}

		public void SetItem(TradeSlots slot, Item item, bool update = false)
		{
			ObjectGuid itemGuid = item ? item.GetGUID() : ObjectGuid.Empty;

			if (_items[(int)slot] == itemGuid &&
			    !update)
				return;

			_items[(int)slot] = itemGuid;

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

		public uint GetSpell()
		{
			return _spell;
		}

		public void SetSpell(uint spell_id, Item castItem = null)
		{
			ObjectGuid itemGuid = castItem ? castItem.GetGUID() : ObjectGuid.Empty;

			if (_spell == spell_id &&
			    _spellCastItem == itemGuid)
				return;

			_spell         = spell_id;
			_spellCastItem = itemGuid;

			SetAccepted(false);
			GetTraderData().SetAccepted(false);

			UpdateServerStateIndex();

			Update(true);  // send spell info to item owner
			Update(false); // send spell info to caster self
		}

		public void SetMoney(ulong money)
		{
			if (_money == money)
				return;

			if (!_player.HasEnoughMoney(money))
			{
				TradeStatusPkt info = new();
				info.Status    = TradeStatus.Failed;
				info.BagResult = InventoryResult.NotEnoughMoney;
				_player.GetSession().SendTradeStatus(info);

				return;
			}

			_money = money;

			SetAccepted(false);
			GetTraderData().SetAccepted(false);

			UpdateServerStateIndex();

			Update(true);
		}

		private void Update(bool forTarget = true)
		{
			if (forTarget)
				_trader.GetSession().SendUpdateTrade(true); // player state for trader
			else
				_player.GetSession().SendUpdateTrade(false); // player state for player
		}

		public void SetAccepted(bool state, bool crosssend = false)
		{
			_accepted = state;

			if (!state)
			{
				TradeStatusPkt info = new();
				info.Status = TradeStatus.Unaccepted;

				if (crosssend)
					_trader.GetSession().SendTradeStatus(info);
				else
					_player.GetSession().SendTradeStatus(info);
			}
		}

		public Player GetTrader()
		{
			return _trader;
		}

		public bool HasSpellCastItem()
		{
			return !_spellCastItem.IsEmpty();
		}

		public ulong GetMoney()
		{
			return _money;
		}

		public bool IsAccepted()
		{
			return _accepted;
		}

		public bool IsInAcceptProcess()
		{
			return _acceptProccess;
		}

		public void SetInAcceptProcess(bool state)
		{
			_acceptProccess = state;
		}

		public uint GetClientStateIndex()
		{
			return _clientStateIndex;
		}

		public void UpdateClientStateIndex()
		{
			++_clientStateIndex;
		}

		public uint GetServerStateIndex()
		{
			return _serverStateIndex;
		}

		public void UpdateServerStateIndex()
		{
			_serverStateIndex = RandomHelper.Rand32();
		}
	}
}