/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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
using Framework.Dynamic;
using Game.Entities;
using System.Collections.Generic;

namespace Game.Network.Packets
{
    public class AcceptTrade : ClientPacket
    {
        public AcceptTrade(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            StateIndex = _worldPacket.ReadUInt32();
        }

        public uint StateIndex { get; set; }
    }

    public class BeginTrade : ClientPacket
    {
        public BeginTrade(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class BusyTrade : ClientPacket
    {
        public BusyTrade(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class CancelTrade : ClientPacket
    {
        public CancelTrade(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class ClearTradeItem : ClientPacket
    {
        public ClearTradeItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            TradeSlot = _worldPacket.ReadUInt8();
        }

        public byte TradeSlot { get; set; }
    }

    public class IgnoreTrade : ClientPacket
    {
        public IgnoreTrade(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class InitiateTrade : ClientPacket
    {
        public InitiateTrade(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Guid { get; set; }
    }

    public class SetTradeCurrency : ClientPacket
    {
        public SetTradeCurrency(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Type = _worldPacket.ReadUInt32();
            Quantity = _worldPacket.ReadUInt32();
        }

        public uint Type { get; set; }
        public uint Quantity { get; set; }
    }

    public class SetTradeGold : ClientPacket
    {
        public SetTradeGold(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Coinage = _worldPacket.ReadUInt64();
        }

        public ulong Coinage { get; set; }
    }

    public class SetTradeItem : ClientPacket
    {
        public SetTradeItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            TradeSlot = _worldPacket.ReadUInt8();
            PackSlot = _worldPacket.ReadUInt8();
            ItemSlotInPack = _worldPacket.ReadUInt8();
        }

        public byte TradeSlot { get; set; }
        public byte PackSlot { get; set; }
        public byte ItemSlotInPack { get; set; }
    }

    public class UnacceptTrade : ClientPacket
    {
        public UnacceptTrade(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class TradeStatusPkt : ServerPacket
    {
        public TradeStatusPkt() : base(ServerOpcodes.TradeStatus, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteBit(PartnerIsSameBnetAccount);
            _worldPacket.WriteBits(Status, 5);
            switch (Status)
            {
                case TradeStatus.Failed:
                    _worldPacket.WriteBit(FailureForYou);
                    _worldPacket.WriteInt32(BagResult);
                    _worldPacket.WriteInt32(ItemID);
                    break;
                case TradeStatus.Initiated:
                    _worldPacket.WriteUInt32(ID);
                    break;
                case TradeStatus.Proposed:
                    _worldPacket.WritePackedGuid(Partner);
                    _worldPacket.WritePackedGuid(PartnerAccount);
                    break;
                case TradeStatus.WrongRealm:
                case TradeStatus.NotOnTaplist:
                    _worldPacket.WriteInt8(TradeSlot);
                    break;
                case TradeStatus.NotEnoughCurrency:
                case TradeStatus.CurrencyNotTradable:
                    _worldPacket.WriteInt32(CurrencyType);
                    _worldPacket.WriteInt32(CurrencyQuantity);
                    break;
                default:
                    _worldPacket.FlushBits();
                    break;
            }
        }

        public TradeStatus Status { get; set; } = TradeStatus.Initiated;
        public byte TradeSlot { get; set; }
        public ObjectGuid PartnerAccount { get; set; }
        public ObjectGuid Partner { get; set; }
        public int CurrencyType { get; set; }
        public int CurrencyQuantity { get; set; }
        public bool FailureForYou { get; set; }
        public InventoryResult BagResult { get; set; }
        public uint ItemID;
        public uint ID { get; set; }
        public bool PartnerIsSameBnetAccount { get; set; }
    }

    public class TradeUpdated : ServerPacket
    {
        public TradeUpdated() : base(ServerOpcodes.TradeUpdated, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt8(WhichPlayer);
            _worldPacket.WriteUInt32(ID);
            _worldPacket.WriteUInt32(ClientStateIndex);
            _worldPacket.WriteUInt32(CurrentStateIndex);
            _worldPacket.WriteUInt64(Gold);
            _worldPacket.WriteInt32(CurrencyType);
            _worldPacket.WriteInt32(CurrencyQuantity);
            _worldPacket.WriteInt32(ProposedEnchantment);
            _worldPacket.WriteUInt32(Items.Count);

            Items.ForEach(item => item.Write(_worldPacket));
        }

        public class UnwrappedTradeItem
        {
            public void Write(WorldPacket data)
            {
                data.WriteInt32(EnchantID);
                data.WriteInt32(OnUseEnchantmentID);
                data.WritePackedGuid(Creator);
                data.WriteInt32(Charges);
                data.WriteUInt32(MaxDurability);
                data.WriteUInt32(Durability);
                data.WriteBits(Gems.Count, 2);
                data.WriteBit(Lock);
                data.FlushBits();

                foreach (var gem in Gems)
                    gem.Write(data);
            }

            public ItemInstance Item { get; set; }
            public int EnchantID { get; set; }
            public int OnUseEnchantmentID { get; set; }
            public ObjectGuid Creator { get; set; }
            public int Charges { get; set; }
            public bool Lock { get; set; }
            public uint MaxDurability { get; set; }
            public uint Durability { get; set; }
            public List<ItemGemData> Gems { get; set; } = new List<ItemGemData>();
        }

        public class TradeItem
        {
            public void Write(WorldPacket data)
            {
                data.WriteUInt8(Slot);
                data.WriteUInt32(StackCount);
                data.WritePackedGuid(GiftCreator);
                Item.Write(data);
                data.WriteBit(Unwrapped.HasValue);
                data.FlushBits();

                if (Unwrapped.HasValue)
                    Unwrapped.Value.Write(data);
            }

            public byte Slot { get; set; }
            public ItemInstance Item { get; set; } = new ItemInstance();
            public int StackCount { get; set; }
            public ObjectGuid GiftCreator { get; set; }
            public Optional<UnwrappedTradeItem> Unwrapped;
        }

        public ulong Gold { get; set; }
        public uint CurrentStateIndex { get; set; }
        public byte WhichPlayer { get; set; }
        public uint ClientStateIndex { get; set; }
        public List<TradeItem> Items { get; set; } = new List<TradeItem>();
        public int CurrencyType { get; set; }
        public uint ID { get; set; }
        public int ProposedEnchantment { get; set; }
        public int CurrencyQuantity { get; set; }
    }
}
