/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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
using Framework.Database;
using Game.Network;

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
                if (item)
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

            if (owner)
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
            WorldPacket buffer = new WorldPacket();

            buffer.WriteUInt8((byte)flags);
            m_objectData.WriteCreate(buffer, flags, this, target);
            m_itemData.WriteCreate(buffer, flags, this, target);
            m_containerData.WriteCreate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        public override void BuildValuesUpdate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new WorldPacket();

            buffer.WriteUInt32(m_values.GetChangedObjectTypeMask());
            if (m_values.HasChanged(TypeId.Object))
                m_objectData.WriteUpdate(buffer, flags, this, target);

            if (m_values.HasChanged(TypeId.Item))
                m_itemData.WriteUpdate(buffer, flags, this, target);

            if (m_values.HasChanged(TypeId.Container))
                m_containerData.WriteUpdate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
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

        public uint GetItemCount(uint item, Item eItem)
        {
            Item pItem;
            uint count = 0;
            for (var i = 0; i < GetBagSize(); ++i)
            {
                pItem = m_bagslot[i];
                if (pItem != null && pItem != eItem && pItem.GetEntry() == item)
                    count += pItem.GetCount();
            }

            if (eItem != null && eItem.GetTemplate().GetGemProperties() != 0)
            {
                for (var i = 0; i < GetBagSize(); ++i)
                {
                    pItem = m_bagslot[i];
                    if (pItem != null && pItem != eItem && pItem.GetSocketColor(0) != 0)
                        count += pItem.GetGemCountWithID(item);
                }
            }

            return count;
        }

        public uint GetItemCountWithLimitCategory(uint limitCategory, Item skipItem)
        {
            uint count = 0;
            for (uint i = 0; i < GetBagSize(); ++i)
            {
                Item pItem = m_bagslot[i];
                if (pItem != null)
                {
                    if (pItem != skipItem)
                    {
                        ItemTemplate pProto = pItem.GetTemplate();
                        if (pProto != null)
                            if (pProto.GetItemLimitCategory() == limitCategory)
                                count += m_bagslot[i].GetCount();
                    }
                }
            }

            return count;
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
    }
}
