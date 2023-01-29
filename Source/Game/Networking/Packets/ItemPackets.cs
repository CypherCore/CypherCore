// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Entities;
using Game.Loots;

namespace Game.Networking.Packets
{
    public class BuyBackItem : ClientPacket
    {
        public uint Slot;

        public ObjectGuid VendorGUID;

        public BuyBackItem(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            VendorGUID = _worldPacket.ReadPackedGuid();
            Slot = _worldPacket.ReadUInt32();
        }
    }

    public class BuyItem : ClientPacket
    {
        public ObjectGuid ContainerGUID;
        public ItemInstance Item;
        public ItemVendorType ItemType;
        public uint Muid;
        public int Quantity;
        public uint Slot;

        public ObjectGuid VendorGUID;

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
    }

    public class BuySucceeded : ServerPacket
    {
        public uint Muid;
        public uint NewQuantity;
        public uint QuantityBought;

        public ObjectGuid VendorGUID;

        public BuySucceeded() : base(ServerOpcodes.BuySucceeded)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(VendorGUID);
            _worldPacket.WriteUInt32(Muid);
            _worldPacket.WriteUInt32(NewQuantity);
            _worldPacket.WriteUInt32(QuantityBought);
        }
    }

    public class BuyFailed : ServerPacket
    {
        public uint Muid;
        public BuyResult Reason = BuyResult.CantFindItem;

        public ObjectGuid VendorGUID;

        public BuyFailed() : base(ServerOpcodes.BuyFailed)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(VendorGUID);
            _worldPacket.WriteUInt32(Muid);
            _worldPacket.WriteUInt8((byte)Reason);
        }
    }

    public class GetItemPurchaseData : ClientPacket
    {
        public ObjectGuid ItemGUID;

        public GetItemPurchaseData(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            ItemGUID = _worldPacket.ReadPackedGuid();
        }
    }

    internal class SetItemPurchaseData : ServerPacket
    {
        public ItemPurchaseContents Contents = new();
        public uint Flags;
        public ObjectGuid ItemGUID;

        public uint PurchaseTime;

        public SetItemPurchaseData() : base(ServerOpcodes.SetItemPurchaseData, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ItemGUID);
            Contents.Write(_worldPacket);
            _worldPacket.WriteUInt32(Flags);
            _worldPacket.WriteUInt32(PurchaseTime);
        }
    }

    internal class ItemPurchaseRefund : ClientPacket
    {
        public ObjectGuid ItemGUID;

        public ItemPurchaseRefund(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            ItemGUID = _worldPacket.ReadPackedGuid();
        }
    }

    internal class ItemPurchaseRefundResult : ServerPacket
    {
        public ItemPurchaseContents Contents;
        public ObjectGuid ItemGUID;

        public byte Result;

        public ItemPurchaseRefundResult() : base(ServerOpcodes.ItemPurchaseRefundResult, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ItemGUID);
            _worldPacket.WriteUInt8(Result);
            _worldPacket.WriteBit(Contents != null);
            _worldPacket.FlushBits();

            Contents?.Write(_worldPacket);
        }
    }

    internal class ItemExpirePurchaseRefund : ServerPacket
    {
        public ObjectGuid ItemGUID;

        public ItemExpirePurchaseRefund() : base(ServerOpcodes.ItemExpirePurchaseRefund, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ItemGUID);
        }
    }

    public class RepairItem : ClientPacket
    {
        public ObjectGuid ItemGUID;

        public ObjectGuid NpcGUID;
        public bool UseGuildBank;

        public RepairItem(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            NpcGUID = _worldPacket.ReadPackedGuid();
            ItemGUID = _worldPacket.ReadPackedGuid();
            UseGuildBank = _worldPacket.HasBit();
        }
    }

    public class SellItem : ClientPacket
    {
        public uint Amount;
        public ObjectGuid ItemGUID;

        public ObjectGuid VendorGUID;

        public SellItem(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            VendorGUID = _worldPacket.ReadPackedGuid();
            ItemGUID = _worldPacket.ReadPackedGuid();
            Amount = _worldPacket.ReadUInt32();
        }
    }

    public class ItemTimeUpdate : ServerPacket
    {
        public uint DurationLeft;

        public ObjectGuid ItemGuid;

        public ItemTimeUpdate() : base(ServerOpcodes.ItemTimeUpdate)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ItemGuid);
            _worldPacket.WriteUInt32(DurationLeft);
        }
    }

    public class SetProficiency : ServerPacket
    {
        public byte ProficiencyClass;

        public uint ProficiencyMask;

        public SetProficiency() : base(ServerOpcodes.SetProficiency, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(ProficiencyMask);
            _worldPacket.WriteUInt8(ProficiencyClass);
        }
    }

    public class InventoryChangeFailure : ServerPacket
    {
        public InventoryResult BagResult;
        public byte ContainerBSlot;
        public ObjectGuid DstContainer;
        public ObjectGuid[] Item = new ObjectGuid[2];
        public int Level;
        public int LimitCategory;
        public ObjectGuid SrcContainer;
        public int SrcSlot;

        public InventoryChangeFailure() : base(ServerOpcodes.InventoryChangeFailure)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteInt8((sbyte)BagResult);
            _worldPacket.WritePackedGuid(Item[0]);
            _worldPacket.WritePackedGuid(Item[1]);
            _worldPacket.WriteUInt8(ContainerBSlot); // bag Type subclass, used with EQUIP_ERR_EVENT_AUTOEQUIP_BIND_CONFIRM and EQUIP_ERR_WRONG_BAG_TYPE_2

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
    }

    public class SplitItem : ClientPacket
    {
        public byte FromPackSlot;
        public byte FromSlot;
        public InvUpdate Inv;
        public int Quantity;
        public byte ToPackSlot;

        public byte ToSlot;

        public SplitItem(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Inv = new InvUpdate(_worldPacket);
            FromPackSlot = _worldPacket.ReadUInt8();
            FromSlot = _worldPacket.ReadUInt8();
            ToPackSlot = _worldPacket.ReadUInt8();
            ToSlot = _worldPacket.ReadUInt8();
            Quantity = _worldPacket.ReadInt32();
        }
    }

    public class SwapInvItem : ClientPacket
    {
        public InvUpdate Inv;
        public byte Slot1; // Source Slot
        public byte Slot2; // Destination Slot

        public SwapInvItem(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Inv = new InvUpdate(_worldPacket);
            Slot2 = _worldPacket.ReadUInt8();
            Slot1 = _worldPacket.ReadUInt8();
        }
    }

    public class SwapItem : ClientPacket
    {
        public byte ContainerSlotA;
        public byte ContainerSlotB;

        public InvUpdate Inv;
        public byte SlotA;
        public byte SlotB;

        public SwapItem(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Inv = new InvUpdate(_worldPacket);
            ContainerSlotB = _worldPacket.ReadUInt8();
            ContainerSlotA = _worldPacket.ReadUInt8();
            SlotB = _worldPacket.ReadUInt8();
            SlotA = _worldPacket.ReadUInt8();
        }
    }

    public class AutoEquipItem : ClientPacket
    {
        public InvUpdate Inv;
        public byte PackSlot;

        public byte Slot;

        public AutoEquipItem(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Inv = new InvUpdate(_worldPacket);
            PackSlot = _worldPacket.ReadUInt8();
            Slot = _worldPacket.ReadUInt8();
        }
    }

    internal class AutoEquipItemSlot : ClientPacket
    {
        public InvUpdate Inv;

        public ObjectGuid Item;
        public byte ItemDstSlot;

        public AutoEquipItemSlot(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Inv = new InvUpdate(_worldPacket);
            Item = _worldPacket.ReadPackedGuid();
            ItemDstSlot = _worldPacket.ReadUInt8();
        }
    }

    public class AutoStoreBagItem : ClientPacket
    {
        public byte ContainerSlotA;

        public byte ContainerSlotB;
        public InvUpdate Inv;
        public byte SlotA;

        public AutoStoreBagItem(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Inv = new InvUpdate(_worldPacket);
            ContainerSlotB = _worldPacket.ReadUInt8();
            ContainerSlotA = _worldPacket.ReadUInt8();
            SlotA = _worldPacket.ReadUInt8();
        }
    }

    public class DestroyItem : ClientPacket
    {
        public byte ContainerId;

        public uint Count;
        public byte SlotNum;

        public DestroyItem(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Count = _worldPacket.ReadUInt32();
            ContainerId = _worldPacket.ReadUInt8();
            SlotNum = _worldPacket.ReadUInt8();
        }
    }

    public class SellResponse : ServerPacket
    {
        public ObjectGuid ItemGUID;
        public SellResult Reason = SellResult.Unk;

        public ObjectGuid VendorGUID;

        public SellResponse() : base(ServerOpcodes.SellResponse)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(VendorGUID);
            _worldPacket.WritePackedGuid(ItemGUID);
            _worldPacket.WriteUInt8((byte)Reason);
        }
    }

    internal class ItemPushResult : ServerPacket
    {
        public enum DisplayType
        {
            Hidden = 0,
            Normal = 1,
            EncounterLoot = 2
        }

        public int BattlePetBreedID;
        public uint BattlePetBreedQuality;
        public int BattlePetLevel;
        public int BattlePetSpeciesID;
        public CraftingData CraftingData;
        public bool Created;
        public DisplayType DisplayText;
        public int DungeonEncounterID;
        public uint? FirstCraftOperationID;
        public bool IsBonusRoll;
        public bool IsEncounterLoot;
        public ItemInstance Item;
        public ObjectGuid ItemGUID;

        public ObjectGuid PlayerGUID;

        public bool Pushed;

        // only set if different than real ID (similar to CreatureTemplate.KillCredit)
        public uint Quantity;
        public uint QuantityInInventory;
        public int QuestLogItemID; // Item ID used for updating quest progress
        public byte Slot;
        public int SlotInBag;
        public List<UiEventToast> Toasts = new();

        public ItemPushResult() : base(ServerOpcodes.ItemPushResult)
        {
        }

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
            _worldPacket.WriteInt32(Toasts.Count);

            foreach (UiEventToast uiEventToast in Toasts)
                uiEventToast.Write(_worldPacket);

            _worldPacket.WriteBit(Pushed);
            _worldPacket.WriteBit(Created);
            _worldPacket.WriteBits((uint)DisplayText, 3);
            _worldPacket.WriteBit(IsBonusRoll);
            _worldPacket.WriteBit(IsEncounterLoot);
            _worldPacket.WriteBit(CraftingData != null);
            _worldPacket.WriteBit(FirstCraftOperationID.HasValue);
            _worldPacket.FlushBits();

            Item.Write(_worldPacket);

            if (FirstCraftOperationID.HasValue)
                _worldPacket.WriteUInt32(FirstCraftOperationID.Value);

            CraftingData?.Write(_worldPacket);
        }
    }

    internal class ReadItem : ClientPacket
    {
        public byte PackSlot;
        public byte Slot;

        public ReadItem(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            PackSlot = _worldPacket.ReadUInt8();
            Slot = _worldPacket.ReadUInt8();
        }
    }

    internal class ReadItemResultFailed : ServerPacket
    {
        public uint Delay;

        public ObjectGuid Item;
        public byte Subcode;

        public ReadItemResultFailed() : base(ServerOpcodes.ReadItemResultFailed)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Item);
            _worldPacket.WriteUInt32(Delay);
            _worldPacket.WriteBits(Subcode, 2);
            _worldPacket.FlushBits();
        }
    }

    internal class ReadItemResultOK : ServerPacket
    {
        public ObjectGuid Item;

        public ReadItemResultOK() : base(ServerOpcodes.ReadItemResultOk)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Item);
        }
    }

    internal class WrapItem : ClientPacket
    {
        public InvUpdate Inv;

        public WrapItem(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Inv = new InvUpdate(_worldPacket);
        }
    }

    internal class EnchantmentLog : ServerPacket
    {
        public ObjectGuid Caster;
        public uint Enchantment;
        public uint EnchantSlot;
        public ObjectGuid ItemGUID;
        public uint ItemID;

        public ObjectGuid Owner;

        public EnchantmentLog() : base(ServerOpcodes.EnchantmentLog, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Owner);
            _worldPacket.WritePackedGuid(Caster);
            _worldPacket.WritePackedGuid(ItemGUID);
            _worldPacket.WriteUInt32(ItemID);
            _worldPacket.WriteUInt32(Enchantment);
            _worldPacket.WriteUInt32(EnchantSlot);
        }
    }

    internal class CancelTempEnchantment : ClientPacket
    {
        public int Slot;

        public CancelTempEnchantment(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Slot = _worldPacket.ReadInt32();
        }
    }

    internal class ItemCooldown : ServerPacket
    {
        public uint Cooldown;

        public ObjectGuid ItemGuid;
        public uint SpellID;

        public ItemCooldown() : base(ServerOpcodes.ItemCooldown)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ItemGuid);
            _worldPacket.WriteUInt32(SpellID);
            _worldPacket.WriteUInt32(Cooldown);
        }
    }

    internal class ItemEnchantTimeUpdate : ServerPacket
    {
        public uint DurationLeft;
        public ObjectGuid ItemGuid;

        public ObjectGuid OwnerGuid;
        public uint Slot;

        public ItemEnchantTimeUpdate() : base(ServerOpcodes.ItemEnchantTimeUpdate, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ItemGuid);
            _worldPacket.WriteUInt32(DurationLeft);
            _worldPacket.WriteUInt32(Slot);
            _worldPacket.WritePackedGuid(OwnerGuid);
        }
    }

    internal class UseCritterItem : ClientPacket
    {
        public ObjectGuid ItemGuid;

        public UseCritterItem(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            ItemGuid = _worldPacket.ReadPackedGuid();
        }
    }

    internal class SocketGems : ClientPacket
    {
        public ObjectGuid[] GemItem = new ObjectGuid[ItemConst.MaxGemSockets];

        public ObjectGuid ItemGuid;

        public SocketGems(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            ItemGuid = _worldPacket.ReadPackedGuid();

            for (int i = 0; i < ItemConst.MaxGemSockets; ++i)
                GemItem[i] = _worldPacket.ReadPackedGuid();
        }
    }

    internal class SocketGemsSuccess : ServerPacket
    {
        public ObjectGuid Item;

        public SocketGemsSuccess() : base(ServerOpcodes.SocketGemsSuccess, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Item);
        }
    }

    internal class SortBags : ClientPacket
    {
        public SortBags(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
        }
    }

    internal class SortBankBags : ClientPacket
    {
        public SortBankBags(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
        }
    }

    internal class SortReagentBankBags : ClientPacket
    {
        public SortReagentBankBags(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
        }
    }

    internal class BagCleanupFinished : ServerPacket
    {
        public BagCleanupFinished() : base(ServerOpcodes.BagCleanupFinished, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
        }
    }

    internal class RemoveNewItem : ClientPacket
    {
        public RemoveNewItem(WorldPacket packet) : base(packet)
        {
        }

        public ObjectGuid ItemGuid { get; set; }

        public override void Read()
        {
            ItemGuid = _worldPacket.ReadPackedGuid();
        }
    }

    internal class InventoryFullOverflow : ServerPacket
    {
        public InventoryFullOverflow() : base(ServerOpcodes.InventoryFullOverflow)
        {
        }

        public override void Write()
        {
        }
    }

    //Structs
    public class ItemBonuses
    {
        public List<uint> BonusListIDs = new();

        public ItemContext Context;

        public void Write(WorldPacket data)
        {
            data.WriteUInt8((byte)Context);
            data.WriteInt32(BonusListIDs.Count);

            foreach (uint bonusID in BonusListIDs)
                data.WriteUInt32(bonusID);
        }

        public void Read(WorldPacket data)
        {
            Context = (ItemContext)data.ReadUInt8();
            uint bonusListIdSize = data.ReadUInt32();

            BonusListIDs = new List<uint>();

            for (uint i = 0u; i < bonusListIdSize; ++i)
            {
                uint bonusId = data.ReadUInt32();
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
            if (ReferenceEquals(left, right))
                return true;

            if (left is null ||
                right is null)
                return false;

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
    }

    public class ItemMod
    {
        public ItemModifier Type;
        public uint Value;

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
        public Array<ItemMod> Values = new((int)ItemModifier.Max);

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

            foreach (ItemMod itemMod in Values)
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
        public ItemBonuses ItemBonus;
        public uint ItemID;
        public ItemModList Modifications = new();

        public ItemInstance()
        {
        }

        public ItemInstance(Item item)
        {
            ItemID = item.GetEntry();
            List<uint> bonusListIds = item.GetBonusListIDs();

            if (!bonusListIds.Empty())
            {
                ItemBonus = new ItemBonuses();
                ItemBonus.BonusListIDs.AddRange(bonusListIds);
                ItemBonus.Context = item.GetContext();
            }

            foreach (var mod in item._itemData.Modifiers.GetValue().Values)
                Modifications.Values.Add(new ItemMod(mod.Value, (ItemModifier)mod.Type));
        }

        public ItemInstance(LootItem lootItem)
        {
            ItemID = lootItem.Itemid;

            if (!lootItem.BonusListIDs.Empty() ||
                lootItem.RandomBonusListId != 0)
            {
                ItemBonus = new ItemBonuses();
                ItemBonus.BonusListIDs = lootItem.BonusListIDs;
                ItemBonus.Context = lootItem.Context;

                if (lootItem.RandomBonusListId != 0)
                    ItemBonus.BonusListIDs.Add(lootItem.RandomBonusListId);
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
                ItemBonus = new ItemBonuses();
                ItemBonus.Context = voidItem.Context;
                ItemBonus.BonusListIDs = voidItem.BonusListIDs;
            }
        }

        public ItemInstance(SocketedGem gem)
        {
            ItemID = gem.ItemId;

            ItemBonuses bonus = new();
            bonus.Context = (ItemContext)(byte)gem.Context;

            foreach (ushort bonusListId in gem.BonusListIDs)
                if (bonusListId != 0)
                    bonus.BonusListIDs.Add(bonusListId);

            if (bonus.Context != 0 ||
                !bonus.BonusListIDs.Empty())
                ItemBonus = bonus;
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(ItemID);

            data.WriteBit(ItemBonus != null);
            data.FlushBits();

            Modifications.Write(data);

            ItemBonus?.Write(data);
        }

        public void Read(WorldPacket data)
        {
            ItemID = data.ReadUInt32();

            if (data.HasBit())
                ItemBonus = new ItemBonuses();

            data.ResetBitPos();

            Modifications.Read(data);

            ItemBonus?.Read(data);
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
            if (ReferenceEquals(left, right))
                return true;

            if (left is null)
                return false;

            if (right is null)
                return false;

            if (left.ItemID != right.ItemID)
                return false;

            if (left.ItemBonus != null &&
                right.ItemBonus != null &&
                left.ItemBonus != right.ItemBonus)
                return false;

            if (left.Modifications != right.Modifications)
                return false;

            return true;
        }

        public static bool operator !=(ItemInstance left, ItemInstance right)
        {
            return !(left == right);
        }
    }

    public class ItemBonusKey : IEquatable<ItemBonusKey>
    {
        public List<uint> BonusListIDs = new();
        public uint ItemID;
        public List<ItemMod> Modifications = new();

        public bool Equals(ItemBonusKey right)
        {
            if (ItemID != right.ItemID)
                return false;

            if (BonusListIDs != right.BonusListIDs)
                return false;

            if (Modifications != right.Modifications)
                return false;

            return true;
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(ItemID);
            data.WriteInt32(BonusListIDs.Count);
            data.WriteInt32(Modifications.Count);

            if (!BonusListIDs.Empty())
                foreach (var id in BonusListIDs)
                    data.WriteUInt32(id);

            foreach (ItemMod modification in Modifications)
                modification.Write(data);
        }
    }

    public class ItemEnchantData
    {
        public int Charges;
        public uint Expiration;

        public uint ID;
        public byte Slot;

        public ItemEnchantData()
        {
        }

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
    }

    public class ItemGemData
    {
        public ItemInstance Item = new();

        public byte Slot;

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
    }

    public struct InvUpdate
    {
        public InvUpdate(WorldPacket data)
        {
            Items = new List<InvItem>();
            int size = data.ReadBits<int>(2);
            data.ResetBitPos();

            for (int i = 0; i < size; ++i)
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
        public ItemPurchaseRefundCurrency[] Currencies = new ItemPurchaseRefundCurrency[5];
        public ItemPurchaseRefundItem[] Items = new ItemPurchaseRefundItem[5];

        public ulong Money;

        public void Write(WorldPacket data)
        {
            data.WriteUInt64(Money);

            for (int i = 0; i < 5; ++i)
                Items[i].Write(data);

            for (int i = 0; i < 5; ++i)
                Currencies[i].Write(data);
        }
    }

    public struct UiEventToast
    {
        public int UiEventToastID;
        public int Asset;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(UiEventToastID);
            data.WriteInt32(Asset);
        }
    }
}