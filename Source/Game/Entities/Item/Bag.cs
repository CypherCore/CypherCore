// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.Networking;
using Game.Networking.Packets;
using System.Collections.Generic;

namespace Game.Entities
{
    public class Bag : Item
    {
        public Bag()
        {
            ObjectTypeMask |= TypeMask.Container;
            ObjectTypeId = TypeId.Container;

            m_containerData = new ContainerData();
        }

        public override void Dispose()
        {
            for (byte i = 0; i < ItemConst.MaxBagSize; ++i)
            {
                Item item = m_bagslot[i];
                if (item != null)
                {
                    if (item.IsInWorld)
                    {
                        Log.outFatal(LogFilter.PlayerItems, "Item {0} (slot {1}, bag slot {2}) in bag {3} (slot {4}, bag slot {5}, m_bagslot {6}) is to be deleted but is still in world.",
                            item.GetEntry(), item.GetSlot(), item.GetBagSlot(),
                            GetEntry(), GetSlot(), GetBagSlot(), i);
                        item.RemoveFromWorld();
                    }
                    m_bagslot[i].Dispose();
                }
            }

            base.Dispose();
        }

        public override void AddToWorld()
        {
            base.AddToWorld();

            for (uint i = 0; i < GetBagSize(); ++i)
                if (m_bagslot[i] != null)
                    m_bagslot[i].AddToWorld();
        }

        public override void RemoveFromWorld()
        {
            for (uint i = 0; i < GetBagSize(); ++i)
                if (m_bagslot[i] != null)
                    m_bagslot[i].RemoveFromWorld();

            base.RemoveFromWorld();
        }

        public override bool Create(ulong guidlow, uint itemid, ItemContext context, Player owner)
        {
            var itemProto = Global.ObjectMgr.GetItemTemplate(itemid);

            if (itemProto == null || itemProto.GetContainerSlots() > ItemConst.MaxBagSize)
                return false;

            _Create(ObjectGuid.Create(HighGuid.Item, guidlow));

            _bonusData = new BonusData(itemProto);

            SetEntry(itemid);
            SetObjectScale(1.0f);

            if (owner != null)
            {
                SetOwnerGUID(owner.GetGUID());
                SetContainedIn(owner.GetGUID());
            }

            SetUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.MaxDurability), itemProto.MaxDurability);
            SetDurability(itemProto.MaxDurability);
            SetCount(1);
            SetContext(context);

            // Setting the number of Slots the Container has
            SetBagSize(itemProto.GetContainerSlots());

            // Cleaning 20 slots
            for (byte i = 0; i < ItemConst.MaxBagSize; ++i)
                SetSlot(i, ObjectGuid.Empty);

            m_bagslot = new Item[ItemConst.MaxBagSize];
            return true;
        }

        public override bool LoadFromDB(ulong guid, ObjectGuid owner_guid, SQLFields fields, uint entry)
        {
            if (!base.LoadFromDB(guid, owner_guid, fields, entry))
                return false;

            ItemTemplate itemProto = GetTemplate(); // checked in Item.LoadFromDB
            SetBagSize(itemProto.GetContainerSlots());
            // cleanup bag content related item value fields (its will be filled correctly from `character_inventory`)
            for (byte i = 0; i < ItemConst.MaxBagSize; ++i)
            {
                SetSlot(i, ObjectGuid.Empty);
                m_bagslot[i] = null;
            }
            return true;
        }

        public override void DeleteFromDB(SQLTransaction trans)
        {
            for (byte i = 0; i < ItemConst.MaxBagSize; ++i)
                if (m_bagslot[i] != null)
                    m_bagslot[i].DeleteFromDB(trans);

            base.DeleteFromDB(trans);
        }

        public uint GetFreeSlots()
        {
            uint slots = 0;
            for (uint i = 0; i < GetBagSize(); ++i)
                if (m_bagslot[i] == null)
                    ++slots;

            return slots;
        }

        public void RemoveItem(byte slot, bool update)
        {
            if (m_bagslot[slot] != null)
                m_bagslot[slot].SetContainer(null);

            m_bagslot[slot] = null;
            SetSlot(slot, ObjectGuid.Empty);
        }

        public void StoreItem(byte slot, Item pItem, bool update)
        {
            if (pItem != null && pItem.GetGUID() != GetGUID())
            {
                m_bagslot[slot] = pItem;
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
                if (m_bagslot[i] != null)
                    m_bagslot[i].BuildCreateUpdateBlockForPlayer(data, target);
        }

        public override void BuildValuesCreate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);

            data.WriteUInt8((byte)flags);
            m_objectData.WriteCreate(data, flags, this, target);
            m_itemData.WriteCreate(data, flags, this, target);
            m_containerData.WriteCreate(data, flags, this, target);
        }

        public override void BuildValuesUpdate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);

            data.WriteUInt32(m_values.GetChangedObjectTypeMask());
            if (m_values.HasChanged(TypeId.Object))
                m_objectData.WriteUpdate(data, flags, this, target);

            if (m_values.HasChanged(TypeId.Item))
                m_itemData.WriteUpdate(data, flags, this, target);

            if (m_values.HasChanged(TypeId.Container))
                m_containerData.WriteUpdate(data, flags, this, target);
        }

        void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedItemMask, UpdateMask requestedContainerMask, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            UpdateMask valuesMask = new((int)TypeId.Max);
            if (requestedObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.Object);

            m_itemData.FilterDisallowedFieldsMaskForFlag(requestedItemMask, flags);
            if (requestedItemMask.IsAnySet())
                valuesMask.Set((int)TypeId.Item);

            if (requestedContainerMask.IsAnySet())
                valuesMask.Set((int)TypeId.Container);

            WorldPacket buffer = new();
            buffer.WriteUInt32(valuesMask.GetBlock(0));

            if (valuesMask[(int)TypeId.Object])
                m_objectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

            if (valuesMask[(int)TypeId.Item])
                m_itemData.WriteUpdate(buffer, requestedItemMask, true, this, target);

            if (valuesMask[(int)TypeId.Container])
                m_containerData.WriteUpdate(buffer, requestedContainerMask, true, this, target);

            WorldPacket buffer1 = new();
            buffer1.WriteUInt8((byte)UpdateType.Values);
            buffer1.WritePackedGuid(GetGUID());
            buffer1.WriteUInt32(buffer.GetSize());
            buffer1.WriteBytes(buffer.GetData());

            data.AddUpdateBlock(buffer1);
        }

        public override void ClearUpdateMask(bool remove)
        {
            m_values.ClearChangesMask(m_containerData);
            base.ClearUpdateMask(remove);
        }

        public bool IsEmpty()
        {
            for (var i = 0; i < GetBagSize(); ++i)
                if (m_bagslot[i] != null)
                    return false;

            return true;
        }

        byte GetSlotByItemGUID(ObjectGuid guid)
        {
            for (byte i = 0; i < GetBagSize(); ++i)
                if (m_bagslot[i] != null)
                    if (m_bagslot[i].GetGUID() == guid)
                        return i;

            return ItemConst.NullSlot;
        }

        public Item GetItemByPos(byte slot)
        {
            if (slot < GetBagSize())
                return m_bagslot[slot];

            return null;
        }

        public uint GetBagSize() { return m_containerData.NumSlots; }
        void SetBagSize(uint numSlots) { SetUpdateFieldValue(m_values.ModifyValue(m_containerData).ModifyValue(m_containerData.NumSlots), numSlots); }

        void SetSlot(int slot, ObjectGuid guid) { SetUpdateFieldValue(ref m_values.ModifyValue(m_containerData).ModifyValue(m_containerData.Slots, slot), guid); }

        ContainerData m_containerData;
        Item[] m_bagslot = new Item[36];

        class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
        {
            Bag Owner;
            ObjectFieldData ObjectMask = new();
            ItemData ItemMask = new();
            ContainerData ContainerMask = new();

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
