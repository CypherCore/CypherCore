// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Game.Networking;
using Game.Networking.Packets;

namespace Game.Entities
{
    public class Bag : Item
    {
        private Item[] _bagslot = new Item[36];

        private readonly ContainerData _containerData;

        public Bag()
        {
            ObjectTypeMask |= TypeMask.Container;
            ObjectTypeId = TypeId.Container;

            _containerData = new ContainerData();
        }

        public override void Dispose()
        {
            for (byte i = 0; i < ItemConst.MaxBagSize; ++i)
            {
                Item item = _bagslot[i];

                if (item)
                {
                    if (item.IsInWorld)
                    {
                        Log.outFatal(LogFilter.PlayerItems,
                                     "Item {0} (Slot {1}, bag Slot {2}) in bag {3} (Slot {4}, bag Slot {5}, _bagslot {6}) is to be deleted but is still in world.",
                                     item.GetEntry(),
                                     item.GetSlot(),
                                     item.GetBagSlot(),
                                     GetEntry(),
                                     GetSlot(),
                                     GetBagSlot(),
                                     i);

                        item.RemoveFromWorld();
                    }

                    _bagslot[i].Dispose();
                }
            }

            base.Dispose();
        }

        public override void AddToWorld()
        {
            base.AddToWorld();

            for (uint i = 0; i < GetBagSize(); ++i)
                _bagslot[i]?.AddToWorld();
        }

        public override void RemoveFromWorld()
        {
            for (uint i = 0; i < GetBagSize(); ++i)
                _bagslot[i]?.RemoveFromWorld();

            base.RemoveFromWorld();
        }

        public override bool Create(ulong guidlow, uint itemid, ItemContext context, Player owner)
        {
            var itemProto = Global.ObjectMgr.GetItemTemplate(itemid);

            if (itemProto == null ||
                itemProto.GetContainerSlots() > ItemConst.MaxBagSize)
                return false;

            _Create(ObjectGuid.Create(HighGuid.Item, guidlow));

            _bonusData = new BonusData(itemProto);

            SetEntry(itemid);
            SetObjectScale(1.0f);

            if (owner)
            {
                SetOwnerGUID(owner.GetGUID());
                SetContainedIn(owner.GetGUID());
            }

            SetUpdateFieldValue(Values.ModifyValue(_itemData).ModifyValue(_itemData.MaxDurability), itemProto.MaxDurability);
            SetDurability(itemProto.MaxDurability);
            SetCount(1);
            SetContext(context);

            // Setting the number of Slots the Container has
            SetBagSize(itemProto.GetContainerSlots());

            // Cleaning 20 slots
            for (byte i = 0; i < ItemConst.MaxBagSize; ++i)
                SetSlot(i, ObjectGuid.Empty);

            _bagslot = new Item[ItemConst.MaxBagSize];

            return true;
        }

        public override bool LoadFromDB(ulong guid, ObjectGuid owner_guid, SQLFields fields, uint entry)
        {
            if (!base.LoadFromDB(guid, owner_guid, fields, entry))
                return false;

            ItemTemplate itemProto = GetTemplate(); // checked in Item.LoadFromDB
            SetBagSize(itemProto.GetContainerSlots());

            // cleanup bag content related Item value fields (its will be filled correctly from `character_inventory`)
            for (byte i = 0; i < ItemConst.MaxBagSize; ++i)
            {
                SetSlot(i, ObjectGuid.Empty);
                _bagslot[i] = null;
            }

            return true;
        }

        public override void DeleteFromDB(SQLTransaction trans)
        {
            for (byte i = 0; i < ItemConst.MaxBagSize; ++i)
                _bagslot[i]?.DeleteFromDB(trans);

            base.DeleteFromDB(trans);
        }

        public uint GetFreeSlots()
        {
            uint slots = 0;

            for (uint i = 0; i < GetBagSize(); ++i)
                if (_bagslot[i] == null)
                    ++slots;

            return slots;
        }

        public void RemoveItem(byte slot, bool update)
        {
            _bagslot[slot]?.SetContainer(null);

            _bagslot[slot] = null;
            SetSlot(slot, ObjectGuid.Empty);
        }

        public void StoreItem(byte slot, Item pItem, bool update)
        {
            if (pItem != null &&
                pItem.GetGUID() != GetGUID())
            {
                _bagslot[slot] = pItem;
                SetSlot(slot, pItem.GetGUID());
                pItem.SetContainedIn(GetGUID());
                pItem.SetOwnerGUID(GetOwnerGUID());
                pItem.SetContainer(this);
                pItem.SetSlot(slot);
            }
        }

        public override void BuildCreateUpdateBlockForPlayer(UpdateData data, Player target)
        {
            base.BuildCreateUpdateBlockForPlayer(data, target);

            for (int i = 0; i < GetBagSize(); ++i)
                _bagslot[i]?.BuildCreateUpdateBlockForPlayer(data, target);
        }

        public override void BuildValuesCreate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new();

            buffer.WriteUInt8((byte)flags);
            ObjectData.WriteCreate(buffer, flags, this, target);
            _itemData.WriteCreate(buffer, flags, this, target);
            _containerData.WriteCreate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        public override void BuildValuesUpdate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new();

            buffer.WriteUInt32(Values.GetChangedObjectTypeMask());

            if (Values.HasChanged(TypeId.Object))
                ObjectData.WriteUpdate(buffer, flags, this, target);

            if (Values.HasChanged(TypeId.Item))
                _itemData.WriteUpdate(buffer, flags, this, target);

            if (Values.HasChanged(TypeId.Container))
                _containerData.WriteUpdate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        private void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedItemMask, UpdateMask requestedContainerMask, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            UpdateMask valuesMask = new((int)TypeId.Max);

            if (requestedObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.Object);

            _itemData.FilterDisallowedFieldsMaskForFlag(requestedItemMask, flags);

            if (requestedItemMask.IsAnySet())
                valuesMask.Set((int)TypeId.Item);

            if (requestedContainerMask.IsAnySet())
                valuesMask.Set((int)TypeId.Container);

            WorldPacket buffer = new();
            buffer.WriteUInt32(valuesMask.GetBlock(0));

            if (valuesMask[(int)TypeId.Object])
                ObjectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

            if (valuesMask[(int)TypeId.Item])
                _itemData.WriteUpdate(buffer, requestedItemMask, true, this, target);

            if (valuesMask[(int)TypeId.Container])
                _containerData.WriteUpdate(buffer, requestedContainerMask, true, this, target);

            WorldPacket buffer1 = new();
            buffer1.WriteUInt8((byte)UpdateType.Values);
            buffer1.WritePackedGuid(GetGUID());
            buffer1.WriteUInt32(buffer.GetSize());
            buffer1.WriteBytes(buffer.GetData());

            data.AddUpdateBlock(buffer1);
        }

        public override void ClearUpdateMask(bool remove)
        {
            Values.ClearChangesMask(_containerData);
            base.ClearUpdateMask(remove);
        }

        public bool IsEmpty()
        {
            for (var i = 0; i < GetBagSize(); ++i)
                if (_bagslot[i] != null)
                    return false;

            return true;
        }

        private byte GetSlotByItemGUID(ObjectGuid guid)
        {
            for (byte i = 0; i < GetBagSize(); ++i)
                if (_bagslot[i] != null)
                    if (_bagslot[i].GetGUID() == guid)
                        return i;

            return ItemConst.NullSlot;
        }

        public Item GetItemByPos(byte slot)
        {
            if (slot < GetBagSize())
                return _bagslot[slot];

            return null;
        }

        public uint GetBagSize()
        {
            return _containerData.NumSlots;
        }

        private void SetBagSize(uint numSlots)
        {
            SetUpdateFieldValue(Values.ModifyValue(_containerData).ModifyValue(_containerData.NumSlots), numSlots);
        }

        private void SetSlot(int slot, ObjectGuid guid)
        {
            SetUpdateFieldValue(ref Values.ModifyValue(_containerData).ModifyValue(_containerData.Slots, slot), guid);
        }

        private class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
        {
            private readonly ContainerData ContainerMask = new();
            private readonly ItemData ItemMask = new();
            private readonly ObjectFieldData ObjectMask = new();
            private readonly Bag Owner;

            public ValuesUpdateForPlayerWithMaskSender(Bag owner)
            {
                Owner = owner;
            }

            public void Invoke(Player player)
            {
                UpdateData udata = new(Owner.GetMapId());

                Owner.BuildValuesUpdateForPlayerWithMask(udata, ObjectMask.GetUpdateMask(), ItemMask.GetUpdateMask(), ContainerMask.GetUpdateMask(), player);

                udata.BuildPacket(out UpdateObject packet);
                player.SendPacket(packet);
            }
        }
    }
}