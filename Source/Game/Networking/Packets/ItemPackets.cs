﻿/*
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
using Framework.Dynamic;
using Game.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Game.Networking.Packets
{
    public class BuyBackItem : ClientPacket
    {
        public BuyBackItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            VendorGUID = _worldPacket.ReadPackedGuid();
            Slot = _worldPacket.ReadUInt32();
        }

        public ObjectGuid VendorGUID;
        public uint Slot;
    }

    public class BuyItem : ClientPacket
    {
        public BuyItem(WorldPacket packet) : base(packet)
        {
            Item = new ItemInstance();
        }

        public override void Read()
        {
            VendorGUID = _worldPacket.ReadPackedGuid();
            ContainerGUID = _worldPacket.ReadPackedGuid();
            Quantity = _worldPacket.ReadInt32();
            Muid = _worldPacket.ReadUInt32();
            Slot = _worldPacket.ReadUInt32();
            Item.Read(_worldPacket);
            ItemType = (ItemVendorType)_worldPacket.ReadBits<int>(3);
        }

        public ObjectGuid VendorGUID;
        public ItemInstance Item;
        public uint Muid;
        public uint Slot;
        public ItemVendorType ItemType;
        public int Quantity;
        public ObjectGuid ContainerGUID;
    }

    public class BuySucceeded : ServerPacket
    {
        public BuySucceeded() : base(ServerOpcodes.BuySucceeded) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(VendorGUID);
            _worldPacket.WriteUInt32(Muid);
            _worldPacket.WriteUInt32(NewQuantity);
            _worldPacket.WriteUInt32(QuantityBought);
        }

        public ObjectGuid VendorGUID;
        public uint Muid;
        public uint QuantityBought;
        public uint NewQuantity;
    }

    public class BuyFailed : ServerPacket
    {
        public BuyFailed() : base(ServerOpcodes.BuyFailed) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(VendorGUID);
            _worldPacket.WriteUInt32(Muid);
            _worldPacket.WriteUInt8((byte)Reason);
        }

        public ObjectGuid VendorGUID;
        public uint Muid;
        public BuyResult Reason = BuyResult.CantFindItem;
    }

    public class GetItemPurchaseData : ClientPacket
    {
        public GetItemPurchaseData(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ItemGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid ItemGUID;
    }

    internal class SetItemPurchaseData : ServerPacket
    {
        public SetItemPurchaseData() : base(ServerOpcodes.SetItemPurchaseData, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ItemGUID);
            Contents.Write(_worldPacket);
            _worldPacket.WriteUInt32(Flags);
            _worldPacket.WriteUInt32(PurchaseTime);
        }

        public uint PurchaseTime;
        public uint Flags;
        public ItemPurchaseContents Contents = new ItemPurchaseContents();
        public ObjectGuid ItemGUID;
    }

    internal class ItemPurchaseRefund : ClientPacket
    {
        public ItemPurchaseRefund(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ItemGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid ItemGUID;
    }

    internal class ItemPurchaseRefundResult : ServerPacket
    {
        public ItemPurchaseRefundResult() : base(ServerOpcodes.ItemPurchaseRefundResult, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ItemGUID);
            _worldPacket.WriteUInt8(Result);
            _worldPacket.WriteBit(Contents.HasValue);
            _worldPacket.FlushBits();
            if (Contents.HasValue)
                Contents.Value.Write(_worldPacket);
        }

        public byte Result;
        public ObjectGuid ItemGUID;
        public Optional<ItemPurchaseContents> Contents;
    }

    internal class ItemExpirePurchaseRefund : ServerPacket
    {
        public ItemExpirePurchaseRefund() : base(ServerOpcodes.ItemExpirePurchaseRefund, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ItemGUID);
        }

        public ObjectGuid ItemGUID;
    }

    public class RepairItem : ClientPacket
    {
        public RepairItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            NpcGUID = _worldPacket.ReadPackedGuid();
            ItemGUID = _worldPacket.ReadPackedGuid();
            UseGuildBank = _worldPacket.HasBit();
        }

        public ObjectGuid NpcGUID;
        public ObjectGuid ItemGUID;
        public bool UseGuildBank;
    }

    public class SellItem : ClientPacket
    {
        public SellItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            VendorGUID = _worldPacket.ReadPackedGuid();
            ItemGUID = _worldPacket.ReadPackedGuid();
            Amount = _worldPacket.ReadUInt32();
        }

        public ObjectGuid VendorGUID;
        public ObjectGuid ItemGUID;
        public uint Amount;
    }

    public class ItemTimeUpdate : ServerPacket
    {
        public ItemTimeUpdate() : base(ServerOpcodes.ItemTimeUpdate) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ItemGuid);
            _worldPacket.WriteUInt32(DurationLeft);
        }

        public ObjectGuid ItemGuid;
        public uint DurationLeft;
    }

    public class SetProficiency : ServerPacket
    {
        public SetProficiency() : base(ServerOpcodes.SetProficiency, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(ProficiencyMask);
            _worldPacket.WriteUInt8(ProficiencyClass);
        }

        public uint ProficiencyMask;
        public byte ProficiencyClass;
    }

    public class InventoryChangeFailure : ServerPacket
    {
        public InventoryChangeFailure() : base(ServerOpcodes.InventoryChangeFailure) { }

        public override void Write()
        {
            _worldPacket.WriteInt8((sbyte)BagResult);
            _worldPacket.WritePackedGuid(Item[0]);
            _worldPacket.WritePackedGuid(Item[1]);
            _worldPacket.WriteUInt8(ContainerBSlot); // bag type subclass, used with EQUIP_ERR_EVENT_AUTOEQUIP_BIND_CONFIRM and EQUIP_ERR_WRONG_BAG_TYPE_2

            switch (BagResult)
            {
                case InventoryResult.CantEquipLevelI:
                case InventoryResult.PurchaseLevelTooLow:
                    _worldPacket.WriteInt32(Level);
                    break;
                case InventoryResult.EventAutoequipBindConfirm:
                    _worldPacket.WritePackedGuid(SrcContainer);
                    _worldPacket.WriteInt32(SrcSlot);
                    _worldPacket.WritePackedGuid(DstContainer);
                    break;
                case InventoryResult.ItemMaxLimitCategoryCountExceededIs:
                case InventoryResult.ItemMaxLimitCategorySocketedExceededIs:
                case InventoryResult.ItemMaxLimitCategoryEquippedExceededIs:
                    _worldPacket.WriteInt32(LimitCategory);
                    break;
            }
        }

        public InventoryResult BagResult;
        public byte ContainerBSlot;
        public ObjectGuid SrcContainer;
        public ObjectGuid DstContainer;
        public int SrcSlot;
        public int LimitCategory;
        public int Level;
        public ObjectGuid[] Item = new ObjectGuid[2];
    }

    public class SplitItem : ClientPacket
    {
        public SplitItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Inv = new InvUpdate(_worldPacket);
            FromPackSlot = _worldPacket.ReadUInt8();
            FromSlot = _worldPacket.ReadUInt8();
            ToPackSlot = _worldPacket.ReadUInt8();
            ToSlot = _worldPacket.ReadUInt8();
            Quantity = _worldPacket.ReadInt32();
        }

        public byte ToSlot;
        public byte ToPackSlot;
        public byte FromPackSlot;
        public int Quantity;
        public InvUpdate Inv;
        public byte FromSlot;
    }

    public class SwapInvItem : ClientPacket
    {
        public SwapInvItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Inv = new InvUpdate(_worldPacket);
            Slot2 = _worldPacket.ReadUInt8();
            Slot1 = _worldPacket.ReadUInt8();
        }

        public InvUpdate Inv;
        public byte Slot1; // Source Slot
        public byte Slot2; // Destination Slot
    }

    public class SwapItem : ClientPacket
    {
        public SwapItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Inv = new InvUpdate(_worldPacket);
            ContainerSlotB = _worldPacket.ReadUInt8();
            ContainerSlotA = _worldPacket.ReadUInt8();
            SlotB = _worldPacket.ReadUInt8();
            SlotA = _worldPacket.ReadUInt8();
        }

        public InvUpdate Inv;
        public byte SlotA;
        public byte ContainerSlotB;
        public byte SlotB;
        public byte ContainerSlotA;
    }

    public class AutoEquipItem : ClientPacket
    {
        public AutoEquipItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Inv = new InvUpdate(_worldPacket);
            PackSlot = _worldPacket.ReadUInt8();
            Slot = _worldPacket.ReadUInt8();
        }

        public byte Slot;
        public InvUpdate Inv;
        public byte PackSlot;
    }

    internal class AutoEquipItemSlot : ClientPacket
    {
        public AutoEquipItemSlot(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Inv = new InvUpdate(_worldPacket);
            Item = _worldPacket.ReadPackedGuid();
            ItemDstSlot = _worldPacket.ReadUInt8();
        }

        public ObjectGuid Item;
        public byte ItemDstSlot;
        public InvUpdate Inv;
    }

    public class AutoStoreBagItem : ClientPacket
    {
        public AutoStoreBagItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Inv = new InvUpdate(_worldPacket);
            ContainerSlotB = _worldPacket.ReadUInt8();
            ContainerSlotA = _worldPacket.ReadUInt8();
            SlotA = _worldPacket.ReadUInt8();
        }

        public byte ContainerSlotB;
        public InvUpdate Inv;
        public byte ContainerSlotA;
        public byte SlotA;
    }

    public class DestroyItem : ClientPacket
    {
        public DestroyItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Count = _worldPacket.ReadUInt32();
            ContainerId = _worldPacket.ReadUInt8();
            SlotNum = _worldPacket.ReadUInt8();
        }

        public uint Count;
        public byte SlotNum;
        public byte ContainerId;
    }

    public class SellResponse : ServerPacket
    {
        public SellResponse() : base(ServerOpcodes.SellResponse) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(VendorGUID);
            _worldPacket.WritePackedGuid(ItemGUID);
            _worldPacket.WriteUInt8((byte)Reason);
        }

        public ObjectGuid VendorGUID;
        public ObjectGuid ItemGUID;
        public SellResult Reason = SellResult.Unk;
    }

    internal class ItemPushResult : ServerPacket
    {
        public ItemPushResult() : base(ServerOpcodes.ItemPushResult) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(PlayerGUID);
            _worldPacket.WriteUInt8(Slot);
            _worldPacket.WriteInt32(SlotInBag);
            _worldPacket.WriteInt32(QuestLogItemID);
            _worldPacket.WriteUInt32(Quantity);
            _worldPacket.WriteUInt32(QuantityInInventory);
            _worldPacket.WriteInt32(DungeonEncounterID);
            _worldPacket.WriteInt32(BattlePetSpeciesID);
            _worldPacket.WriteInt32(BattlePetBreedID);
            _worldPacket.WriteUInt32(BattlePetBreedQuality);
            _worldPacket.WriteInt32(BattlePetLevel);
            _worldPacket.WritePackedGuid(ItemGUID);
            _worldPacket.WriteBit(Pushed);
            _worldPacket.WriteBit(Created);
            _worldPacket.WriteBits((uint)DisplayText, 3);
            _worldPacket.WriteBit(IsBonusRoll);
            _worldPacket.WriteBit(IsEncounterLoot);
            _worldPacket.FlushBits();

            Item.Write(_worldPacket);            
        }

        public ObjectGuid PlayerGUID;
        public byte Slot;
        public int SlotInBag;
        public ItemInstance Item;
        public int QuestLogItemID;// Item ID used for updating quest progress
                                   // only set if different than real ID (similar to CreatureTemplate.KillCredit)
        public uint Quantity;
        public uint QuantityInInventory;
        public int DungeonEncounterID;
        public int BattlePetSpeciesID;
        public int BattlePetBreedID;
        public uint BattlePetBreedQuality;
        public int BattlePetLevel;
        public ObjectGuid ItemGUID;
        public bool Pushed;
        public DisplayType DisplayText;
        public bool Created;
        public bool IsBonusRoll;
        public bool IsEncounterLoot;


        public enum DisplayType
        {
            Hidden = 0,
            Normal = 1,
            EncounterLoot = 2
        }
    }

    internal class ReadItem : ClientPacket
    {
        public ReadItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PackSlot = _worldPacket.ReadUInt8();
            Slot = _worldPacket.ReadUInt8();
        }

        public byte PackSlot;
        public byte Slot;
    }

    internal class ReadItemResultFailed : ServerPacket
    {
        public ReadItemResultFailed() : base(ServerOpcodes.ReadItemResultFailed) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Item);
            _worldPacket.WriteUInt32(Delay);
            _worldPacket.WriteBits(Subcode, 2);
            _worldPacket.FlushBits();
        }

        public ObjectGuid Item;
        public byte Subcode;
        public uint Delay;
    }

    internal class ReadItemResultOK : ServerPacket
    {
        public ReadItemResultOK() : base(ServerOpcodes.ReadItemResultOk) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Item);
        }

        public ObjectGuid Item;
    }

    internal class WrapItem : ClientPacket
    {
        public WrapItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Inv = new InvUpdate(_worldPacket);
        }

        public InvUpdate Inv;
    }

    internal class EnchantmentLog : ServerPacket
    {
        public EnchantmentLog() : base(ServerOpcodes.EnchantmentLog, ConnectionType.Instance) { }

        public override void Write()
        {    
            _worldPacket.WritePackedGuid(Owner);
            _worldPacket.WritePackedGuid(Caster);
            _worldPacket.WritePackedGuid(ItemGUID);
            _worldPacket.WriteUInt32(ItemID);
            _worldPacket.WriteUInt32(Enchantment);
            _worldPacket.WriteUInt32(EnchantSlot);
        }

        public ObjectGuid Owner;
        public ObjectGuid Caster;
        public ObjectGuid ItemGUID;
        public uint ItemID;
        public uint Enchantment;
        public uint EnchantSlot;
    }

    internal class CancelTempEnchantment : ClientPacket
    {
        public CancelTempEnchantment(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Slot = _worldPacket.ReadInt32();
        }

        public int Slot;
    }

    internal class ItemCooldown : ServerPacket
    {
        public ItemCooldown() : base(ServerOpcodes.ItemCooldown) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ItemGuid);
            _worldPacket.WriteUInt32(SpellID);
            _worldPacket.WriteUInt32(Cooldown);
        }

        public ObjectGuid ItemGuid;
        public uint SpellID;
        public uint Cooldown;
    }

    internal class ItemEnchantTimeUpdate : ServerPacket
    {
        public ItemEnchantTimeUpdate() : base(ServerOpcodes.ItemEnchantTimeUpdate, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ItemGuid);
            _worldPacket.WriteUInt32(DurationLeft);
            _worldPacket.WriteUInt32(Slot);
            _worldPacket.WritePackedGuid(OwnerGuid);
        }

        public ObjectGuid OwnerGuid;
        public ObjectGuid ItemGuid;
        public uint DurationLeft;
        public uint Slot;
    }

    internal class UseCritterItem : ClientPacket
    {
        public UseCritterItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ItemGuid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid ItemGuid;
    }

    internal class SocketGems : ClientPacket
    {
        public SocketGems(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ItemGuid = _worldPacket.ReadPackedGuid();
            for (var i = 0; i < ItemConst.MaxGemSockets; ++i)
                GemItem[i] = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid ItemGuid;
        public ObjectGuid[] GemItem = new ObjectGuid[ItemConst.MaxGemSockets];
    }

    internal class SocketGemsSuccess : ServerPacket
    {
        public SocketGemsSuccess() : base(ServerOpcodes.SocketGemsSuccess, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Item);
        }

        public ObjectGuid Item;
    }

    internal class SortBags : ClientPacket
    {
        public SortBags(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    internal class SortBankBags : ClientPacket
    {
        public SortBankBags(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    internal class SortReagentBankBags : ClientPacket
    {
        public SortReagentBankBags(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    internal class BagCleanupFinished : ServerPacket
    {
        public BagCleanupFinished() : base(ServerOpcodes.BagCleanupFinished, ConnectionType.Instance) { }

        public override void Write() { }
    }

    internal class RemoveNewItem : ClientPacket
    {
        public RemoveNewItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ItemGuid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid ItemGuid { get; set; }
    }

    internal class InventoryFullOverflow : ServerPacket
    {
        public InventoryFullOverflow() : base(ServerOpcodes.InventoryFullOverflow) { }

        public override void Write() { }
    }

    //Structs
    public class ItemBonuses
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt8((byte)Context);
            data.WriteInt32(BonusListIDs.Count);
            foreach (var bonusID in BonusListIDs)
                data.WriteUInt32(bonusID);
        }

        public void Read(WorldPacket data)
        {
            Context = (ItemContext)data.ReadUInt8();
            var bonusListIdSize = data.ReadUInt32();

            BonusListIDs = new List<uint>();
            for (var i = 0u; i < bonusListIdSize; ++i)
            {
                var bonusId = data.ReadUInt32();
                BonusListIDs.Add(bonusId);
            }
        }

        public override int GetHashCode()
        {
            return Context.GetHashCode() ^ BonusListIDs.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is ItemBonuses)
                return (ItemBonuses)obj == this;

            return false;
        }

        public static bool operator ==(ItemBonuses left, ItemBonuses right)
        {
            if (left.Context != right.Context)
                return false;

            if (left.BonusListIDs.Count != right.BonusListIDs.Count)
                return false;

            return left.BonusListIDs.SequenceEqual(right.BonusListIDs);
        }

        public static bool operator !=(ItemBonuses left, ItemBonuses right)
        {
            return !(left == right);
        }

        public ItemContext Context;
        public List<uint> BonusListIDs = new List<uint>();
    }

    public class ItemMod
    {  
        public uint Value;
        public ItemModifier Type;

        public ItemMod()
        {
            Type = ItemModifier.Max;
        }
        public ItemMod(uint value, ItemModifier type)
        {
            Value = value;
            Type = type;
        }

        public void Read(WorldPacket data)
        {
            Value = data.ReadUInt32();
            Type = (ItemModifier)data.ReadUInt8();
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(Value);
            data.WriteUInt8((byte)Type);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode() ^ Type.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is ItemMod)
                return (ItemMod)obj == this;

            return false;
        }

        public static bool operator ==(ItemMod left, ItemMod right)
        {
            if (left.Value != right.Value)
                return false;

            return left.Type != right.Type;
        }

        public static bool operator !=(ItemMod left, ItemMod right)
        {
            return !(left == right);
        }
    }

    public class ItemModList
    {
        public Array<ItemMod> Values = new Array<ItemMod>((int)ItemModifier.Max);

        public void Read(WorldPacket data)
        {
            var itemModListCount = data.ReadBits<uint>(6);
            data.ResetBitPos();

            for (var i = 0; i < itemModListCount; ++i)
            {
                var itemMod = new ItemMod();
                itemMod.Read(data);
                Values[i] = itemMod;
            }
        }

        public void Write(WorldPacket data)
        {
            data.WriteBits(Values.Count, 6);
            data.FlushBits();

            foreach (var itemMod in Values)
                itemMod.Write(data);
        }

        public override int GetHashCode()
        {
            return Values.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is ItemModList)
                return (ItemModList)obj == this;

            return false;
        }

        public static bool operator ==(ItemModList left, ItemModList right)
        {
            if (left.Values.Count != right.Values.Count)
                return false;

            return !left.Values.Except(right.Values).Any();
        }

        public static bool operator !=(ItemModList left, ItemModList right)
        {
            return !(left == right);
        }
    }

    public class ItemInstance
    {
        public uint ItemID;
        public Optional<ItemBonuses> ItemBonus;
        public ItemModList Modifications = new ItemModList();

        public ItemInstance() { }

        public ItemInstance(Item item)
        {
            ItemID = item.GetEntry();
            List<uint> bonusListIds = item.m_itemData.BonusListIDs;
            if (!bonusListIds.Empty())
            {
                ItemBonus.HasValue = true;
                ItemBonus.Value.BonusListIDs.AddRange(bonusListIds);
                ItemBonus.Value.Context = item.GetContext();
            }

            foreach (var mod in item.m_itemData.Modifiers.GetValue().Values)
                Modifications.Values.Add(new ItemMod(mod.Value, (ItemModifier)mod.Type));
        }

        public ItemInstance(Loots.LootItem lootItem)
        {
            ItemID = lootItem.itemid;

            if (!lootItem.BonusListIDs.Empty() || lootItem.randomBonusListId != 0)
            {
                ItemBonus.HasValue = true;
                ItemBonus.Value.BonusListIDs = lootItem.BonusListIDs;
                ItemBonus.Value.Context = lootItem.context;
                if (lootItem.randomBonusListId != 0)
                    ItemBonus.Value.BonusListIDs.Add(lootItem.randomBonusListId);
            }
        }

        public ItemInstance(VoidStorageItem voidItem)
        {
            ItemID = voidItem.ItemEntry;

            if (voidItem.FixedScalingLevel != 0)
                Modifications.Values.Add(new ItemMod(voidItem.FixedScalingLevel, ItemModifier.TimewalkerLevel));

            if (voidItem.ArtifactKnowledgeLevel != 0)
                Modifications.Values.Add(new ItemMod(voidItem.ArtifactKnowledgeLevel, ItemModifier.ArtifactKnowledgeLevel));

            if (!voidItem.BonusListIDs.Empty())
            {
                ItemBonus.HasValue = true;
                ItemBonus.Value.Context = voidItem.Context;
                ItemBonus.Value.BonusListIDs = voidItem.BonusListIDs;
            }
        }

        public ItemInstance(SocketedGem gem)
        {
            ItemID = gem.ItemId;

            var bonus = new ItemBonuses();
            bonus.Context = (ItemContext)(byte)gem.Context;
            foreach (var bonusListId in gem.BonusListIDs)
                if (bonusListId != 0)
                    bonus.BonusListIDs.Add(bonusListId);

            if (bonus.Context != 0 || !bonus.BonusListIDs.Empty())
                ItemBonus.Set(bonus);
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(ItemID);

            data.WriteBit(ItemBonus.HasValue);
            data.FlushBits();

            Modifications.Write(data);

            if (ItemBonus.HasValue)
                ItemBonus.Value.Write(data);
        }

        public void Read(WorldPacket data)
        {
            ItemID = data.ReadUInt32();

            ItemBonus.HasValue = data.HasBit();
            data.ResetBitPos();

            Modifications.Read(data);

            if (ItemBonus.HasValue)
                ItemBonus.Value.Read(data);
        }

        public override int GetHashCode()
        {
            return ItemID.GetHashCode() ^ ItemBonus.GetHashCode() ^ Modifications.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is ItemInstance)
                return (ItemInstance)obj == this;

            return false;
        }

        public static bool operator ==(ItemInstance left, ItemInstance right)
        {
            if (left.ItemID != right.ItemID)
                return false;

            if (left.ItemBonus.HasValue != right.ItemBonus.HasValue)
                return false;

            if (left.Modifications != right.Modifications)
                return false;

            if (left.ItemBonus.HasValue && left.ItemBonus.Value != right.ItemBonus.Value)
                return false;

            return true;
        }

        public static bool operator !=(ItemInstance left, ItemInstance right)
        {
            return !(left == right);
        }
    }

    public class ItemEnchantData
    {
        public ItemEnchantData(uint id, uint expiration, int charges, byte slot)
        {
            ID = id;
            Expiration = expiration;
            Charges = charges;
            Slot = slot;
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(ID);
            data.WriteUInt32(Expiration);
            data.WriteInt32(Charges);
            data.WriteUInt8(Slot);
        }

        public uint ID;
        public uint Expiration;
        public int Charges;
        public byte Slot;
    }

    public class ItemGemData
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt8(Slot);
            Item.Write(data);
        }

        public void Read(WorldPacket data)
        {
            Slot = data.ReadUInt8();
            Item.Read(data);
        }

        public byte Slot;
        public ItemInstance Item = new ItemInstance();
    }

    public struct InvUpdate
    {
        public InvUpdate(WorldPacket data)
        {
            Items = new List<InvItem>();
            var size = data.ReadBits<int>(2);
            data.ResetBitPos();
            for (var i = 0; i < size; ++i)
            {
                var item = new InvItem
                {
                    ContainerSlot = data.ReadUInt8(),
                    Slot = data.ReadUInt8()
                };
                Items.Add(item);
            }
        }

        public List<InvItem> Items;

        public struct InvItem
        {
            public byte ContainerSlot;
            public byte Slot;
        }
    }

    internal struct ItemPurchaseRefundItem
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(ItemID);
            data.WriteUInt32(ItemCount);
        }

        public uint ItemID;
        public uint ItemCount;
    }

    internal struct ItemPurchaseRefundCurrency
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(CurrencyID);
            data.WriteUInt32(CurrencyCount);
        }

        public uint CurrencyID;
        public uint CurrencyCount;
    }

    internal class ItemPurchaseContents
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt64(Money);
            for (var i = 0; i < 5; ++i)
                Items[i].Write(data);

            for (var i = 0; i < 5; ++i)
                Currencies[i].Write(data);
        }

        public ulong Money;
        public ItemPurchaseRefundItem[] Items = new ItemPurchaseRefundItem[5];
        public ItemPurchaseRefundCurrency[] Currencies = new ItemPurchaseRefundCurrency[5];
    }
}
