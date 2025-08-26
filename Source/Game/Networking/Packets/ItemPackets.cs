// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using System;
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
            ItemType = (ItemVendorType)_worldPacket.ReadInt32();
            Item.Read(_worldPacket);
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
            _worldPacket.WriteUInt32((uint)Reason);
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

    class SetItemPurchaseData : ServerPacket
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
        public ItemPurchaseContents Contents = new();
        public ObjectGuid ItemGUID;
    }

    class ItemPurchaseRefund : ClientPacket
    {
        public ItemPurchaseRefund(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ItemGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid ItemGUID;
    }

    class ItemPurchaseRefundResult : ServerPacket
    {
        public ItemPurchaseRefundResult() : base(ServerOpcodes.ItemPurchaseRefundResult, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ItemGUID);
            _worldPacket.WriteUInt8(Result);
            _worldPacket.WriteBit(Contents != null);
            _worldPacket.FlushBits();
            if (Contents != null)
                Contents.Write(_worldPacket);
        }

        public byte Result;
        public ObjectGuid ItemGUID;
        public ItemPurchaseContents Contents;
    }

    class ItemExpirePurchaseRefund : ServerPacket
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
            _worldPacket.WriteInt32((int)BagResult);
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

    class AutoEquipItemSlot : ClientPacket
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
            _worldPacket.WriteInt32(ItemGUIDs.Count);
            _worldPacket.WriteInt32((int)Reason);
            foreach (ObjectGuid itemGuid in ItemGUIDs)
                _worldPacket.WritePackedGuid(itemGuid);
        }

        public ObjectGuid VendorGUID;
        public List<ObjectGuid> ItemGUIDs = new();
        public SellResult Reason = SellResult.Unk;
    }

    class ItemPushResult : ServerPacket
    {
        public ItemPushResult() : base(ServerOpcodes.ItemPushResult) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(PlayerGUID);
            _worldPacket.WriteUInt8(Slot);
            _worldPacket.WriteInt32(SlotInBag);
            _worldPacket.WriteInt32(ProxyItemID);
            _worldPacket.WriteUInt32(Quantity);
            _worldPacket.WriteUInt32(QuantityInInventory);
            _worldPacket.WriteInt32(QuantityInQuestLog);
            _worldPacket.WriteInt32(EncounterID);
            _worldPacket.WriteInt32(BattlePetSpeciesID);
            _worldPacket.WriteInt32(BattlePetBreedID);
            _worldPacket.WriteUInt8(BattlePetBreedQuality);
            _worldPacket.WriteInt32(BattlePetLevel);
            _worldPacket.WritePackedGuid(ItemGUID);
            _worldPacket.WriteInt32(Toasts.Count);
            foreach (UiEventToast uiEventToast in Toasts)
                uiEventToast.Write(_worldPacket);

            _worldPacket.WriteBit(Pushed);
            _worldPacket.WriteBit(Created);
            _worldPacket.WriteBit(FakeQuestItem);
            _worldPacket.WriteBits((uint)ChatNotifyType, 3);
            _worldPacket.WriteBit(IsBonusRoll);
            _worldPacket.WriteBit(IsPersonalLoot);
            _worldPacket.WriteBit(CraftingData != null);
            _worldPacket.WriteBit(FirstCraftOperationID.HasValue);
            _worldPacket.FlushBits();

            Item.Write(_worldPacket);

            if (FirstCraftOperationID.HasValue)
                _worldPacket.WriteUInt32(FirstCraftOperationID.Value);

            if (CraftingData != null)
                CraftingData.Write(_worldPacket);
        }

        public ObjectGuid PlayerGUID;
        public byte Slot;
        public int SlotInBag;
        public ItemInstance Item = new();
        public int ProxyItemID;// Item ID used for updating quest progress
                                  // only set if different than real ID (similar to CreatureTemplate.KillCredit)
        public uint Quantity;
        public uint QuantityInInventory;
        public int QuantityInQuestLog;
        public int EncounterID;
        public int BattlePetSpeciesID;
        public int BattlePetBreedID;
        public byte BattlePetBreedQuality;
        public int BattlePetLevel;
        public ObjectGuid ItemGUID;
        public List<UiEventToast> Toasts = new();
        public CraftingData CraftingData;
        public uint? FirstCraftOperationID;
        public bool Pushed;
        public DisplayType ChatNotifyType;
        public bool Created;
        public bool FakeQuestItem;
        public bool IsBonusRoll;
        public bool IsPersonalLoot;

        public enum DisplayType
        {
            Hidden = 0,
            Normal = 1,
            EncounterLoot = 2
        }
    }

    class ReadItem : ClientPacket
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

    class ReadItemResultFailed : ServerPacket
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

    class ReadItemResultOK : ServerPacket
    {
        public ReadItemResultOK() : base(ServerOpcodes.ReadItemResultOk) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Item);
        }

        public ObjectGuid Item;
    }

    class WrapItem : ClientPacket
    {
        public WrapItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Inv = new InvUpdate(_worldPacket);
        }

        public InvUpdate Inv;
    }

    class EnchantmentLog : ServerPacket
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

    class CancelTempEnchantment : ClientPacket
    {
        public CancelTempEnchantment(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Slot = _worldPacket.ReadInt32();
        }

        public int Slot;
    }

    class ItemCooldown : ServerPacket
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

    class ItemEnchantTimeUpdate : ServerPacket
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

    class UseCritterItem : ClientPacket
    {
        public UseCritterItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ItemGuid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid ItemGuid;
    }

    class SocketGems : ClientPacket
    {
        public SocketGems(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ItemGuid = _worldPacket.ReadPackedGuid();
            for (int i = 0; i < ItemConst.MaxGemSockets; ++i)
                GemItem[i] = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid ItemGuid;
        public ObjectGuid[] GemItem = new ObjectGuid[ItemConst.MaxGemSockets];
    }

    class SocketGemsSuccess : ServerPacket
    {
        public SocketGemsSuccess() : base(ServerOpcodes.SocketGemsSuccess, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Item);
        }

        public ObjectGuid Item;
    }

    class SortAccountBankBags : ClientPacket
    {
        public SortAccountBankBags(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class SortBags : ClientPacket
    {
        public SortBags(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class SortBankBags : ClientPacket
    {
        public SortBankBags(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class BagCleanupFinished : ServerPacket
    {
        public BagCleanupFinished() : base(ServerOpcodes.BagCleanupFinished, ConnectionType.Instance) { }

        public override void Write() { }
    }

    class RemoveNewItem : ClientPacket
    {
        public RemoveNewItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ItemGuid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid ItemGuid { get; set; }
    }

    class InventoryFullOverflow : ServerPacket
    {
        public InventoryFullOverflow() : base(ServerOpcodes.InventoryFullOverflow) { }

        public override void Write() { }
    }

    class ChangeBagSlotFlag : ClientPacket
    {
        public byte BagIndex;
        public BagSlotFlags FlagToChange;
        public bool On;

        public ChangeBagSlotFlag(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            BagIndex = _worldPacket.ReadUInt8();
            FlagToChange = (BagSlotFlags)_worldPacket.ReadUInt32();
            On = _worldPacket.HasBit();
        }
    }

    class ChangeBankBagSlotFlag : ClientPacket
    {
        public byte BagIndex;
        public BagSlotFlags FlagToChange;
        public bool On;

        public ChangeBankBagSlotFlag(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            BagIndex = _worldPacket.ReadUInt8();
            FlagToChange = (BagSlotFlags)_worldPacket.ReadUInt32();
            On = _worldPacket.HasBit();
        }
    }

    class SetBackpackAutosortDisabled : ClientPacket
    {
        public bool Disable;

        public SetBackpackAutosortDisabled(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Disable = _worldPacket.HasBit();
        }

    }

    class SetBackpackSellJunkDisabled : ClientPacket
    {
        public bool Disable;

        public SetBackpackSellJunkDisabled(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Disable = _worldPacket.HasBit();
        }

    }

    class SetBankAutosortDisabled : ClientPacket
    {
        public bool Disable;

        public SetBankAutosortDisabled(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Disable = _worldPacket.HasBit();
        }
    }

    class AddItemPassive : ServerPacket
    {
        public uint SpellID;

        public AddItemPassive() : base(ServerOpcodes.AddItemPassive, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SpellID);
        }
    }

    class RemoveItemPassive : ServerPacket
    {
        public uint SpellID;

        public RemoveItemPassive() : base(ServerOpcodes.RemoveItemPassive, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SpellID);
        }
    }

    class SendItemPassives : ServerPacket
    {
        public List<uint> SpellID = new();

        public SendItemPassives() : base(ServerOpcodes.SendItemPassives, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(SpellID.Count);
            if (!SpellID.Empty())
                foreach (uint id in SpellID)
                    _worldPacket.WriteUInt32(id);
        }
    }

    //Structs
    public class ItemBonuses
    {
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

            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
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

        public ItemContext Context;
        public List<uint> BonusListIDs = new();
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
            Type = (ItemModifier)data.ReadUInt8();
            Value = data.ReadUInt32();
        }

        public void Write(WorldPacket data)
        {     
            data.WriteUInt8((byte)Type);
            data.WriteUInt32(Value);
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
        public uint ItemID;
        public ItemBonuses ItemBonus;
        public ItemModList Modifications = new();

        public ItemInstance() { }

        public ItemInstance(Item item)
        {
            ItemID = item.GetEntry();
            List<uint> bonusListIds = item.GetBonusListIDs();
            if (!bonusListIds.Empty())
            {
                ItemBonus = new();
                ItemBonus.BonusListIDs.AddRange(bonusListIds);
                ItemBonus.Context = item.GetContext();
            }

            foreach (var mod in item.m_itemData.Modifiers.GetValue().Values)
                Modifications.Values.Add(new ItemMod(mod.Value, (ItemModifier)mod.Type));
        }

        public ItemInstance(Loots.LootItem lootItem)
        {
            ItemID = lootItem.itemid;

            if (!lootItem.BonusListIDs.Empty() || lootItem.randomBonusListId != 0)
            {
                ItemBonus = new();
                ItemBonus.BonusListIDs = lootItem.BonusListIDs;
                ItemBonus.Context = lootItem.context;
                if (lootItem.randomBonusListId != 0)
                    ItemBonus.BonusListIDs.Add(lootItem.randomBonusListId);
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

            if (bonus.Context != 0 || !bonus.BonusListIDs.Empty())
                ItemBonus = bonus;
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(ItemID);

            data.WriteBit(ItemBonus != null);
            data.FlushBits();

            Modifications.Write(data);

            if (ItemBonus != null)
                ItemBonus.Write(data);
        }

        public void Read(WorldPacket data)
        {
            ItemID = data.ReadUInt32();

            if (data.HasBit())
                ItemBonus = new();

            data.ResetBitPos();

            Modifications.Read(data);

            if (ItemBonus != null)
                ItemBonus.Read(data);
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
            if (ReferenceEquals(left, null))
                return false;
            if (ReferenceEquals(right, null))
                return false;

            if (left.ItemID != right.ItemID)
                return false;

            if (left.ItemBonus != null && right.ItemBonus != null && left.ItemBonus != right.ItemBonus)
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
        public uint ItemID;
        public List<uint> BonusListIDs = new();
        public List<ItemMod> Modifications = new();

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
    }

    public class ItemEnchantData
    {
        public ItemEnchantData() { }
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
        public ItemInstance Item = new();
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

    struct ItemPurchaseRefundItem
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(ItemID);
            data.WriteUInt32(ItemCount);
        }

        public uint ItemID;
        public uint ItemCount;
    }

    struct ItemPurchaseRefundCurrency
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(CurrencyID);
            data.WriteUInt32(CurrencyCount);
        }

        public uint CurrencyID;
        public uint CurrencyCount;
    }

    class ItemPurchaseContents
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt64(Money);
            for (int i = 0; i < 5; ++i)
                Items[i].Write(data);

            for (int i = 0; i < 5; ++i)
                Currencies[i].Write(data);
        }

        public ulong Money;
        public ItemPurchaseRefundItem[] Items = new ItemPurchaseRefundItem[5];
        public ItemPurchaseRefundCurrency[] Currencies = new ItemPurchaseRefundCurrency[5];
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
