// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;
using System.Collections.Generic;

namespace Game.Networking.Packets
{
    class LootUnit : ClientPacket
    {
        public LootUnit(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Unit = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Unit;
    }

    public class LootResponse : ServerPacket
    {
        public LootResponse() : base(ServerOpcodes.LootResponse, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Owner);
            _worldPacket.WritePackedGuid(LootObj);
            _worldPacket.WriteUInt8((byte)FailureReason);
            _worldPacket.WriteUInt8(AcquireReason);
            _worldPacket.WriteUInt8((byte)LootMethod);
            _worldPacket.WriteUInt8(Threshold);
            _worldPacket.WriteUInt32(Coins);
            _worldPacket.WriteInt32(Items.Count);
            _worldPacket.WriteInt32(Currencies.Count);
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

        public ObjectGuid LootObj;
        public ObjectGuid Owner;
        public byte Threshold = 2; // Most common value, 2 = Uncommon
        public LootMethod LootMethod;
        public byte AcquireReason;
        public LootError FailureReason = LootError.NoLoot; // Most common value
        public uint Coins;
        public List<LootItemData> Items = new();
        public List<LootCurrency> Currencies = new();
        public bool Acquired;
        public bool AELooting;
    }

    class LootItemPkt : ClientPacket
    {
        public List<LootRequest> Loot = new();
        public bool IsSoftInteract;

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

            IsSoftInteract = _worldPacket.HasBit();
        }
    }

    class MasterLootItem : ClientPacket
    {
        public MasterLootItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint Count = _worldPacket.ReadUInt32();
            Target = _worldPacket.ReadPackedGuid();

            for (int i = 0; i < Count; ++i)
            {
                LootRequest lootRequest = new();
                lootRequest.Object = _worldPacket.ReadPackedGuid();
                lootRequest.LootListID = _worldPacket.ReadUInt8();
                Loot[i] = lootRequest;
            }
        }

        public Array<LootRequest> Loot = new(1000);
        public ObjectGuid Target;
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

        public ObjectGuid LootObj;
        public ObjectGuid Owner;
        public byte LootListID;
    }

    class LootRelease : ClientPacket
    {
        public LootRelease(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Unit = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Unit;
    }

    class LootMoney : ClientPacket
    {
        public bool IsSoftInteract;

        public LootMoney(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            IsSoftInteract = _worldPacket.HasBit();
        }
    }

    class LootMoneyNotify : ServerPacket
    {
        public LootMoneyNotify() : base(ServerOpcodes.LootMoneyNotify) { }

        public override void Write()
        {
            _worldPacket.WriteUInt64(Money);
            _worldPacket.WriteUInt64(MoneyMod);
            _worldPacket.WriteBit(SoleLooter);
            _worldPacket.FlushBits();
        }

        public ulong Money;
        public ulong MoneyMod;
        public bool SoleLooter;
    }

    class CoinRemoved : ServerPacket
    {
        public CoinRemoved() : base(ServerOpcodes.CoinRemoved) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(LootObj);
        }

        public ObjectGuid LootObj;
    }

    class LootRollPacket : ClientPacket
    {
        public LootRollPacket(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            LootObj = _worldPacket.ReadPackedGuid();
            LootListID = _worldPacket.ReadUInt8();
            RollType = (RollVote)_worldPacket.ReadUInt8();
        }

        public ObjectGuid LootObj;
        public byte LootListID;
        public RollVote RollType;
    }

    class LootReleaseResponse : ServerPacket
    {
        public LootReleaseResponse() : base(ServerOpcodes.LootRelease) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(LootObj);
            _worldPacket.WritePackedGuid(Owner);
        }

        public ObjectGuid LootObj;
        public ObjectGuid Owner;
    }

    class LootReleaseAll : ServerPacket
    {
        public LootReleaseAll() : base(ServerOpcodes.LootReleaseAll, ConnectionType.Instance) { }

        public override void Write() { }
    }

    class LootList : ServerPacket
    {
        public LootList() : base(ServerOpcodes.LootList, ConnectionType.Instance) { }

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

        public ObjectGuid Owner;
        public ObjectGuid LootObj;
        public ObjectGuid? Master;
        public ObjectGuid? RoundRobinWinner;
    }

    class SetLootSpecialization : ClientPacket
    {
        public SetLootSpecialization(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            SpecID = _worldPacket.ReadUInt32();
        }

        public uint SpecID;
    }

    class StartLootRoll : ServerPacket
    {
        public ObjectGuid LootObj;
        public int MapID;
        public uint RollTime;
        public LootMethod Method;
        public RollMask ValidRolls;
        public Array<LootRollIneligibilityReason> LootRollIneligibleReason = new Array<LootRollIneligibilityReason>(4);
        public LootItemData Item = new();
        public uint DungeonEncounterID;

        public StartLootRoll() : base(ServerOpcodes.StartLootRoll) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(LootObj);
            _worldPacket.WriteInt32(MapID);
            _worldPacket.WriteUInt32(RollTime);
            _worldPacket.WriteUInt8((byte)ValidRolls);
            foreach (var reason in LootRollIneligibleReason)
                _worldPacket.WriteUInt32((uint)reason);

            _worldPacket.WriteUInt8((byte)Method);
            _worldPacket.WriteUInt32(DungeonEncounterID);
            Item.Write(_worldPacket);
        }
    }

    class LootRollBroadcast : ServerPacket
    {
        public ObjectGuid LootObj;
        public ObjectGuid Player;
        public int Roll;             // Roll value can be negative, it means that it is an "offspec" roll but only during roll selection broadcast (not when sending the result)
        public RollVote RollType;
        public LootItemData Item = new();
        public bool Autopassed;    // Triggers message |HlootHistory:%d|h[Loot]|h: You automatically passed on: %s because you cannot loot that item.
        public bool OffSpec;
        public uint DungeonEncounterID;

        public LootRollBroadcast() : base(ServerOpcodes.LootRoll) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(LootObj);
            _worldPacket.WritePackedGuid(Player);
            _worldPacket.WriteInt32(Roll);
            _worldPacket.WriteUInt8((byte)RollType);
            _worldPacket.WriteUInt32(DungeonEncounterID);
            Item.Write(_worldPacket);
            _worldPacket.WriteBit(Autopassed);
            _worldPacket.WriteBit(OffSpec);
            _worldPacket.FlushBits();
        }
    }

    class LootRollWon : ServerPacket
    {
        public ObjectGuid LootObj;
        public ObjectGuid Winner;
        public int Roll;
        public RollVote RollType;
        public LootItemData Item = new();
        public bool MainSpec;
        public uint DungeonEncounterID;

        public LootRollWon() : base(ServerOpcodes.LootRollWon) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(LootObj);
            _worldPacket.WritePackedGuid(Winner);
            _worldPacket.WriteInt32(Roll);
            _worldPacket.WriteUInt8((byte)RollType);
            _worldPacket.WriteUInt32(DungeonEncounterID);
            Item.Write(_worldPacket);
            _worldPacket.WriteBit(MainSpec);
            _worldPacket.FlushBits();
        }
    }

    class LootAllPassed : ServerPacket
    {
        public ObjectGuid LootObj;
        public LootItemData Item = new();
        public uint DungeonEncounterID;

        public LootAllPassed() : base(ServerOpcodes.LootAllPassed) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(LootObj);
            _worldPacket.WriteUInt32(DungeonEncounterID);
            Item.Write(_worldPacket);
        }
    }

    class LootRollsComplete : ServerPacket
    {
        public ObjectGuid LootObj;
        public byte LootListID;
        public int DungeonEncounterID;

        public LootRollsComplete() : base(ServerOpcodes.LootRollsComplete) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(LootObj);
            _worldPacket.WriteUInt8(LootListID);
            _worldPacket.WriteInt32(DungeonEncounterID);
        }
    }

    class MasterLootCandidateList : ServerPacket
    {
        public MasterLootCandidateList() : base(ServerOpcodes.MasterLootCandidateList, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(LootObj);
            _worldPacket.WriteInt32(Players.Count);
            Players.ForEach(guid => _worldPacket.WritePackedGuid(guid));
        }

        public List<ObjectGuid> Players = new();
        public ObjectGuid LootObj;
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

        public byte Type;
        public LootSlotType UIType;
        public uint Quantity;
        public byte LootItemType;
        public byte LootListID;
        public bool CanTradeToTapList;
        public ItemInstance Loot;
    }

    public struct LootCurrency
    {
        public uint CurrencyID;
        public uint Quantity;
        public byte LootListID;
        public byte UIType;
    }

    public struct LootRequest
    {
        public ObjectGuid Object;
        public byte LootListID;
    }
}
