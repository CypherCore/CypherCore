// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Networking;

namespace Game.Entities
{
    public abstract class BaseUpdateData<T> : IHasChangesMask
    {
        public BaseUpdateData(int blockBit, TypeId bit, int changeMask)
        {
            BlockBit = blockBit;
            Bit = (int)bit;
            ChangesMask = new UpdateMask(changeMask);
        }

        public BaseUpdateData(int changeMask)
        {
            ChangesMask = new UpdateMask(changeMask);
        }

        public int BlockBit { get; set; }
        public UpdateMask ChangesMask { get; set; }
        public int Bit { get; set; }

        public abstract void ClearChangesMask();

        public UpdateMask GetUpdateMask()
        {
            return ChangesMask;
        }

        public void ClearChanged<U>(UpdateField<U> updateField) where U : new()
        {
            ChangesMask.Reset(updateField.Bit);
        }

        public void ClearChanged<U>(UpdateFieldArray<U> updateField, int index) where U : new()
        {
            ChangesMask.Reset(updateField.FirstElementBit + index);
        }

        public void ClearChanged<U>(DynamicUpdateField<U> updateField, int index) where U : new()
        {
            ChangesMask.Reset(Bit);
            updateField.ClearChanged(index);
        }

        public void ClearChangesMask<U>(UpdateField<U> updateField) where U : new()
        {
            if (typeof(IHasChangesMask).IsAssignableFrom(typeof(U)))
                ((IHasChangesMask)updateField.Value).ClearChangesMask();
        }

        public void ClearChangesMask(UpdateFieldString updateField)
        {
        }

        public void ClearChangesMask<U>(OptionalUpdateField<U> updateField) where U : new()
        {
            if (typeof(IHasChangesMask).IsAssignableFrom(typeof(U)) &&
                updateField.HasValue())
                ((IHasChangesMask)updateField.Value).ClearChangesMask();
        }

        public void ClearChangesMask<U>(UpdateFieldArray<U> updateField) where U : new()
        {
            if (typeof(IHasChangesMask).IsAssignableFrom(typeof(U)))
                for (int i = 0; i < updateField.GetSize(); ++i)
                    ((IHasChangesMask)updateField[i]).ClearChangesMask();
        }

        public void ClearChangesMask<U>(DynamicUpdateField<U> updateField) where U : new()
        {
            if (typeof(IHasChangesMask).IsAssignableFrom(typeof(U)))
            {
                for (int i = 0; i < updateField.Size(); ++i)
                    ((IHasChangesMask)updateField[i]).ClearChangesMask();

                updateField.ClearChangesMask();
            }
        }

        public UpdateField<U> ModifyValue<U>(UpdateField<U> updateField) where U : new()
        {
            MarkChanged(updateField);

            return updateField;
        }

        public UpdateFieldString ModifyValue(UpdateFieldString updateField)
        {
            MarkChanged(updateField);

            return updateField;
        }

        public ref U ModifyValue<U>(UpdateFieldArray<U> updateField, int index) where U : new()
        {
            MarkChanged(updateField, index);

            return ref updateField.Values[index];
        }

        public DynamicUpdateField<U> ModifyValue<U>(DynamicUpdateField<U> updateField) where U : new()
        {
            ChangesMask.Set(updateField.BlockBit);
            ChangesMask.Set(updateField.Bit);

            return updateField;
        }

        public DynamicUpdateFieldSetter<U> ModifyValue<U>(DynamicUpdateField<U> updateField, int index) where U : new()
        {
            if (index >= updateField.Values.Count)
            {
                // fill with zeros until reaching desired Slot
                updateField.Values.Resize((uint)index + 1);
                updateField.UpdateMask.Resize((uint)(updateField.Values.Count + 31) / 32);
            }

            ChangesMask.Set(updateField.BlockBit);
            ChangesMask.Set(updateField.Bit);
            updateField.MarkChanged(index);

            return new DynamicUpdateFieldSetter<U>(updateField, index);
        }

        public void MarkChanged<U>(UpdateField<U> updateField) where U : new()
        {
            ChangesMask.Set(updateField.BlockBit);
            ChangesMask.Set(updateField.Bit);
        }

        public void MarkChanged(UpdateFieldString updateField)
        {
            ChangesMask.Set(updateField.BlockBit);
            ChangesMask.Set(updateField.Bit);
        }

        public void MarkChanged<U>(UpdateFieldArray<U> updateField, int index) where U : new()
        {
            ChangesMask.Set(updateField.Bit);
            ChangesMask.Set(updateField.FirstElementBit + index);
        }

        public void WriteCompleteDynamicFieldUpdateMask(int size, WorldPacket data, int bitsForSize = 32)
        {
            data.WriteBits(size, bitsForSize);

            if (size > 32)
            {
                if (data.HasUnfinishedBitPack())
                    for (int block = 0; block < size / 32; ++block)
                        data.WriteBits(0xFFFFFFFFu, 32);
                else
                    for (int block = 0; block < size / 32; ++block)
                        data.WriteUInt32(0xFFFFFFFFu);
            }
            else if (size == 32)
            {
                data.WriteBits(0xFFFFFFFFu, 32);

                return;
            }

            if ((size % 32) != 0)
                data.WriteBits(0xFFFFFFFFu, size % 32);
        }
    }
}