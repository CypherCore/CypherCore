// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets
{
    public class AcceptTrade : ClientPacket
    {
        public uint StateIndex;

        public AcceptTrade(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            StateIndex = _worldPacket.ReadUInt32();
        }
    }

    public class BeginTrade : ClientPacket
    {
        public BeginTrade(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
        }
    }

    public class BusyTrade : ClientPacket
    {
        public BusyTrade(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
        }
    }

    public class CancelTrade : ClientPacket
    {
        public CancelTrade(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
        }
    }

    public class ClearTradeItem : ClientPacket
    {
        public byte TradeSlot;

        public ClearTradeItem(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            TradeSlot = _worldPacket.ReadUInt8();
        }
    }

    public class IgnoreTrade : ClientPacket
    {
        public IgnoreTrade(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
        }
    }

    public class InitiateTrade : ClientPacket
    {
        public ObjectGuid Guid;

        public InitiateTrade(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
        }
    }

    public class SetTradeCurrency : ClientPacket
    {
        public uint Quantity;

        public uint Type;

        public SetTradeCurrency(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Type = _worldPacket.ReadUInt32();
            Quantity = _worldPacket.ReadUInt32();
        }
    }

    public class SetTradeGold : ClientPacket
    {
        public ulong Coinage;

        public SetTradeGold(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Coinage = _worldPacket.ReadUInt64();
        }
    }

    public class SetTradeItem : ClientPacket
    {
        public byte ItemSlotInPack;
        public byte PackSlot;

        public byte TradeSlot;

        public SetTradeItem(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            TradeSlot = _worldPacket.ReadUInt8();
            PackSlot = _worldPacket.ReadUInt8();
            ItemSlotInPack = _worldPacket.ReadUInt8();
        }
    }

    public class UnacceptTrade : ClientPacket
    {
        public UnacceptTrade(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
        }
    }

    public class TradeStatusPkt : ServerPacket
    {
        public InventoryResult BagResult;
        public int CurrencyQuantity;
        public int CurrencyType;
        public bool FailureForYou;
        public uint Id;
        public uint ItemID;
        public ObjectGuid Partner;
        public ObjectGuid PartnerAccount;
        public bool PartnerIsSameBnetAccount;

        public TradeStatus Status = TradeStatus.Initiated;
        public byte TradeSlot;

        public TradeStatusPkt() : base(ServerOpcodes.TradeStatus, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteBit(PartnerIsSameBnetAccount);
            _worldPacket.WriteBits(Status, 5);

            switch (Status)
            {
                case TradeStatus.Failed:
                    _worldPacket.WriteBit(FailureForYou);
                    _worldPacket.WriteInt32((int)BagResult);
                    _worldPacket.WriteUInt32(ItemID);

                    break;
                case TradeStatus.Initiated:
                    _worldPacket.WriteUInt32(Id);

                    break;
                case TradeStatus.Proposed:
                    _worldPacket.WritePackedGuid(Partner);
                    _worldPacket.WritePackedGuid(PartnerAccount);

                    break;
                case TradeStatus.WrongRealm:
                case TradeStatus.NotOnTaplist:
                    _worldPacket.WriteUInt8(TradeSlot);

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
    }

    public class TradeUpdated : ServerPacket
    {
        public class UnwrappedTradeItem
        {
            public int Charges;
            public ObjectGuid Creator;
            public uint Durability;
            public int EnchantID;
            public List<ItemGemData> Gems = new();

            public ItemInstance Item;
            public bool Lock;
            public uint MaxDurability;
            public int OnUseEnchantmentID;

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
        }

        public class TradeItem
        {
            public ObjectGuid GiftCreator;
            public ItemInstance Item = new();

            public byte Slot;
            public int StackCount;
            public UnwrappedTradeItem Unwrapped;

            public void Write(WorldPacket data)
            {
                data.WriteUInt8(Slot);
                data.WriteInt32(StackCount);
                data.WritePackedGuid(GiftCreator);
                Item.Write(data);
                data.WriteBit(Unwrapped != null);
                data.FlushBits();

                Unwrapped?.Write(data);
            }
        }

        public uint ClientStateIndex;
        public int CurrencyQuantity;
        public int CurrencyType;
        public uint CurrentStateIndex;

        public ulong Gold;
        public uint Id;
        public List<TradeItem> Items = new();
        public int ProposedEnchantment;
        public byte WhichPlayer;

        public TradeUpdated() : base(ServerOpcodes.TradeUpdated, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt8(WhichPlayer);
            _worldPacket.WriteUInt32(Id);
            _worldPacket.WriteUInt32(ClientStateIndex);
            _worldPacket.WriteUInt32(CurrentStateIndex);
            _worldPacket.WriteUInt64(Gold);
            _worldPacket.WriteInt32(CurrencyType);
            _worldPacket.WriteInt32(CurrencyQuantity);
            _worldPacket.WriteInt32(ProposedEnchantment);
            _worldPacket.WriteInt32(Items.Count);

            Items.ForEach(item => item.Write(_worldPacket));
        }
    }
}