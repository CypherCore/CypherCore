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
    class LootUnit : ClientPacket
    {
        public LootUnit(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Unit = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Unit { get; set; }
    }

    public class LootResponse : ServerPacket
    {
        public LootResponse() : base(ServerOpcodes.LootResponse, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Owner);
            _worldPacket.WritePackedGuid(LootObj);
            _worldPacket.WriteUInt8(FailureReason);
            _worldPacket.WriteUInt8(AcquireReason);
            _worldPacket.WriteUInt8(LootMethod);
            _worldPacket.WriteUInt8(Threshold);
            _worldPacket.WriteUInt32(Coins);
            _worldPacket.WriteUInt32(Items.Count);
            _worldPacket.WriteUInt32(Currencies.Count);
            _worldPacket.WriteBit(Acquired);
            _worldPacket.WriteBit(AELooting);
            _worldPacket.FlushBits();

            foreach (LootItemData item in Items)
                item.Write(_worldPacket);

            foreach (LootCurrency currency in Currencies)
            {
                _worldPacket.WriteUInt32(currency.CurrencyID);
                _worldPacket.WriteUInt32(currency.Quantity);
                _worldPacket.WriteUInt8(currency.LootListID);
                _worldPacket.WriteBits(currency.UIType, 3);
                _worldPacket.FlushBits();
            }
        }

        public ObjectGuid LootObj { get; set; }
        public ObjectGuid Owner { get; set; }
        public byte Threshold { get; set; } = 2; // Most common value, 2 = Uncommon
        public LootMethod LootMethod { get; set; }
        public byte AcquireReason { get; set; }
        public LootError FailureReason { get; set; } = LootError.NoLoot; // Most common value
        public uint Coins { get; set; }
        public List<LootItemData> Items { get; set; } = new List<LootItemData>();
        public List<LootCurrency> Currencies { get; set; } = new List<LootCurrency>();
        public bool Acquired { get; set; }
        public bool AELooting { get; set; }
    }

    class LootItemPkt : ClientPacket
    {
        public LootItemPkt(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint Count = _worldPacket.ReadUInt32();

            for (uint i = 0; i < Count; ++i)
            {
                var loot = new LootRequest()
                {
                    Object = _worldPacket.ReadPackedGuid(),
                    LootListID = _worldPacket.ReadUInt8()
                };

                Loot.Add(loot);
            }
        }

        public List<LootRequest> Loot { get; set; } = new List<LootRequest>();
    }

    class LootRemoved : ServerPacket
    {
        public LootRemoved() : base(ServerOpcodes.LootRemoved, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Owner);
            _worldPacket.WritePackedGuid(LootObj);
            _worldPacket.WriteUInt8(LootListID);
        }

        public ObjectGuid LootObj { get; set; }
        public ObjectGuid Owner { get; set; }
        public byte LootListID { get; set; }
    }

    class LootRelease : ClientPacket
    {
        public LootRelease(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Unit = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Unit { get; set; }
    }

    class LootMoney : ClientPacket
    {
        public LootMoney(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class LootMoneyNotify : ServerPacket
    {
        public LootMoneyNotify() : base(ServerOpcodes.LootMoneyNotify) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Money);
            _worldPacket.WriteBit(SoleLooter);
            _worldPacket.FlushBits();
        }

        public uint Money { get; set; }
        public bool SoleLooter { get; set; }
    }

    class CoinRemoved : ServerPacket
    {
        public CoinRemoved() : base(ServerOpcodes.CoinRemoved) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(LootObj);
        }

        public ObjectGuid LootObj { get; set; }
    }

    class LootRoll : ClientPacket
    {
        public LootRoll(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            LootObj = _worldPacket.ReadPackedGuid();
            LootListID = _worldPacket.ReadUInt8();
            RollType = (RollType)_worldPacket.ReadUInt8();
        }

        public ObjectGuid LootObj { get; set; }
        public byte LootListID { get; set; }
        public RollType RollType { get; set; }
    }

    class LootReleaseResponse : ServerPacket
    {
        public LootReleaseResponse() : base(ServerOpcodes.LootRelease) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(LootObj);
            _worldPacket.WritePackedGuid(Owner);
        }

        public ObjectGuid LootObj { get; set; }
        public ObjectGuid Owner { get; set; }
    }

    class LootReleaseAll : ServerPacket
    {
        public LootReleaseAll() : base(ServerOpcodes.LootReleaseAll, ConnectionType.Instance) { }

        public override void Write() { }
    }

    class LootList : ServerPacket
    {
        public LootList() : base(ServerOpcodes.LootList) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Owner);
            _worldPacket.WritePackedGuid(LootObj);

            _worldPacket.WriteBit(Master.HasValue);
            _worldPacket.WriteBit(RoundRobinWinner.HasValue);
            _worldPacket.FlushBits();

            if (Master.HasValue)
                _worldPacket.WritePackedGuid(Master.Value);

            if (RoundRobinWinner.HasValue)
                _worldPacket.WritePackedGuid(RoundRobinWinner.Value);
        }

        public ObjectGuid Owner { get; set; }
        public ObjectGuid LootObj { get; set; }
        public Optional<ObjectGuid> Master { get; set; }
        public Optional<ObjectGuid> RoundRobinWinner { get; set; }
    }

    class SetLootSpecialization : ClientPacket
    {
        public SetLootSpecialization(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            SpecID = _worldPacket.ReadUInt32();
        }

        public uint SpecID { get; set; }
    }

    class StartLootRoll : ServerPacket
    {
        public StartLootRoll() : base(ServerOpcodes.StartLootRoll) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(LootObj);
            _worldPacket.WriteInt32(MapID);
            _worldPacket.WriteUInt32(RollTime);
            _worldPacket.WriteUInt8(ValidRolls);
            _worldPacket.WriteUInt8(Method);
            Item.Write(_worldPacket);
        }

        public ObjectGuid LootObj { get; set; }
        public int MapID { get; set; }
        public uint RollTime { get; set; }
        public LootMethod Method { get; set; }
        public RollMask ValidRolls { get; set; }
        public LootItemData Item { get; set; } = new LootItemData();
    }

    class LootRollBroadcast : ServerPacket
    {
        public LootRollBroadcast() : base(ServerOpcodes.LootRoll) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(LootObj);
            _worldPacket.WritePackedGuid(Player);
            _worldPacket.WriteInt32(Roll);
            _worldPacket.WriteUInt8(RollType);
            Item.Write(_worldPacket);
            _worldPacket.WriteBit(Autopassed);
            _worldPacket.FlushBits();
        }

        public ObjectGuid LootObj { get; set; }
        public ObjectGuid Player { get; set; }
        public int Roll;             // Roll value can be negative, it means that it is an "offspec" roll but only during roll selection broadcast (not when sending the result)
        public RollType RollType { get; set; }
        public LootItemData Item { get; set; } = new LootItemData();
        public bool Autopassed { get; set; }   // Triggers message |HlootHistory:%d|h[Loot]|h: You automatically passed on: %s because you cannot loot that item.
    }

    class LootRollWon : ServerPacket
    {
        public LootRollWon() : base(ServerOpcodes.LootRollWon) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(LootObj);
            _worldPacket.WritePackedGuid(Winner);
            _worldPacket.WriteInt32(Roll);
            _worldPacket.WriteUInt8(RollType);
            Item.Write(_worldPacket);
            _worldPacket.WriteBit(MainSpec);
            _worldPacket.FlushBits();
        }

        public ObjectGuid LootObj { get; set; }
        public ObjectGuid Winner { get; set; }
        public int Roll { get; set; }
        public RollType RollType { get; set; }
        public LootItemData Item { get; set; } = new LootItemData();
        public bool MainSpec { get; set; }
    }

    class LootAllPassed : ServerPacket
    {
        public LootAllPassed() : base(ServerOpcodes.LootAllPassed) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(LootObj);
            Item.Write(_worldPacket);
        }

        public ObjectGuid LootObj { get; set; }
        public LootItemData Item { get; set; } = new LootItemData();
    }

    class LootRollsComplete : ServerPacket
    {
        public LootRollsComplete() : base(ServerOpcodes.LootRollsComplete) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(LootObj);
            _worldPacket.WriteUInt8(LootListID);
        }

        public ObjectGuid LootObj { get; set; }
        public byte LootListID { get; set; }
    }

    class AELootTargets : ServerPacket
    {
        public AELootTargets(uint count) : base(ServerOpcodes.AeLootTargets, ConnectionType.Instance)
        {
            Count = count;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Count);
        }

        uint Count;
    }

    class AELootTargetsAck : ServerPacket
    {
        public AELootTargetsAck() : base(ServerOpcodes.AeLootTargetAck, ConnectionType.Instance) { }

        public override void Write() { }
    }

    class MasterLootCandidateList : ServerPacket
    {
        public MasterLootCandidateList() : base(ServerOpcodes.MasterLootCandidateList) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(LootObj);
            _worldPacket.WriteUInt32(Players.Count);
            Players.ForEach(guid => _worldPacket.WritePackedGuid(guid));
        }

        public List<ObjectGuid> Players { get; set; } = new List<ObjectGuid>();
        public ObjectGuid LootObj { get; set; }
    }

    //Structs
    public class LootItemData
    {
        public void Write(WorldPacket data)
        {
            data.WriteBits(Type, 2);
            data.WriteBits(UIType, 3);
            data.WriteBit(CanTradeToTapList);
            data.FlushBits();
            Loot.Write(data); // WorldPackets::Item::ItemInstance
            data.WriteUInt32(Quantity);
            data.WriteUInt8(LootItemType);
            data.WriteUInt8(LootListID);
        }

        public byte Type { get; set; }
        public LootSlotType UIType { get; set; }
        public uint Quantity { get; set; }
        public byte LootItemType { get; set; }
        public byte LootListID { get; set; }
        public bool CanTradeToTapList { get; set; }
        public ItemInstance Loot { get; set; }
    }

    public struct LootCurrency
    {
        public uint CurrencyID { get; set; }
        public uint Quantity { get; set; }
        public byte LootListID { get; set; }
        public byte UIType { get; set; }
    }

    public struct LootRequest
    {
        public ObjectGuid Object { get; set; }
        public byte LootListID { get; set; }
    }
}
