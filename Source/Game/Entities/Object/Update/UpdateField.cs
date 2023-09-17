// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Networking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Entities
{
    public class UpdateFieldHolder
    {
        UpdateMask _changesMask = new((int)TypeId.Max);

        public HasChangesMask ModifyValue(HasChangesMask updateData)
        {
            _changesMask.Set(updateData.Bit);
            return updateData;
        }

        public void ClearChangesMask(HasChangesMask updateData)
        {
            _changesMask.Reset(updateData.Bit);
            updateData.ClearChangesMask();
        }

        public void ClearChangesMask<U>(HasChangesMask updateData, ref UpdateField<U> updateField) where U : new()
        {
            _changesMask.Reset(updateData.Bit);

            IHasChangesMask hasChangesMask = (IHasChangesMask)updateField._value;
            if (hasChangesMask != null)
                hasChangesMask.ClearChangesMask();
        }

        public uint GetChangedObjectTypeMask()
        {
            return _changesMask.GetBlock(0);
        }

        public bool HasChanged(TypeId index)
        {
            return _changesMask[(int)index];
        }
    }

    public interface IUpdateField<T>
    {
        void SetValue(T value);
        T GetValue();
    }

    public class UpdateFieldString : IUpdateField<string>
    {
        public string _value;
        public int BlockBit;
        public int Bit;

        public UpdateFieldString(int blockBit, int bit)
        {
            BlockBit = blockBit;
            Bit = bit;
            _value = "";
        }

        public static implicit operator string(UpdateFieldString updateField)
        {
            return updateField._value;
        }

        public void SetValue(string value) { _value = value; }

        public string GetValue() { return _value; }
    }

    public class UpdateField<T> : IUpdateField<T> where T : new()
    {
        public T _value;
        public int BlockBit;
        public int Bit;

        public UpdateField(int blockBit, int bit)
        {
            BlockBit = blockBit;
            Bit = bit;
            _value = new T();
        }

        public static implicit operator T(UpdateField<T> updateField)
        {
            return updateField._value;
        }

        public void SetValue(T value) { _value = value; }

        public T GetValue() { return _value; }
    }

    public class OptionalUpdateField<T> : IUpdateField<T> where T : new()
    {
        bool _hasValue;
        public T _value;
        public int BlockBit;
        public int Bit;

        public OptionalUpdateField(int blockBit, int bit)
        {
            BlockBit = blockBit;
            Bit = bit;
        }

        public static implicit operator T(OptionalUpdateField<T> updateField)
        {
            return updateField._value;
        }

        public void SetValue(T value)
        {
            _hasValue = true;
            _value = value;
        }

        public T GetValue() { return _value; }

        public bool HasValue() { return _hasValue; }
    }

    public class UpdateFieldArray<T> where T : new()
    {
        public T[] _values;
        public int FirstElementBit;
        public int Bit;

        public UpdateFieldArray(uint size, int bit, int firstElementBit)
        {
            _values = new T[size];
            for (var i = 0; i < size; ++i)
                _values[i] = new T();

            Bit = bit;
            FirstElementBit = firstElementBit;
        }

        public T this[int index]
        {
            get
            {
                return _values[index];
            }
            set
            {
                _values[index] = value;
            }
        }

        public int GetSize() { return _values.Length; }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var obj in _values)
                yield return obj;
        }
    }

    public class DynamicUpdateField<T> where T : new()
    {
        public List<T> _values;
        public List<uint> _updateMask;
        public int BlockBit;
        public int Bit;

        public DynamicUpdateField()
        {
            _values = new List<T>();
            _updateMask = new List<uint>();

            BlockBit = -1;
            Bit = -1;
        }

        public DynamicUpdateField(int blockBit, int bit)
        {
            _values = new List<T>();
            _updateMask = new List<uint>();

            BlockBit = blockBit;
            Bit = bit;
        }

        public int FindIndex(T value)
        {
            return _values.IndexOf(value);
        }

        public int FindIndexIf(Predicate<T> predicate)
        {
            return _values.FindIndex(predicate);
        }

        public bool HasChanged(int index)
        {
            return (_updateMask[index / 32] & (1 << (index % 32))) != 0;
        }

        public void WriteUpdateMask(WorldPacket data, int bitsForSize = 32)
        {
            data.WriteBits(_values.Count, bitsForSize);
            if (_values.Count > 32)
            {
                if (data.HasUnfinishedBitPack())
                    for (int block = 0; block < _values.Count / 32; ++block)
                        data.WriteBits(_updateMask[block], 32);
                else
                    for (int block = 0; block < _values.Count / 32; ++block)
                        data.WriteUInt32(_updateMask[block]);
            }

            else if (_values.Count == 32)
            {
                data.WriteBits(_updateMask.Last(), 32);
                return;
            }

            if ((_values.Count % 32) != 0)
                data.WriteBits(_updateMask.Last(), _values.Count % 32);
        }

        public void ClearChangesMask()
        {
            for (var i = 0; i < _updateMask.Count; ++i)
                _updateMask[i] = 0;
        }

        public void AddValue(T value)
        {
            MarkChanged(_values.Count);
            MarkAllUpdateMaskFields(value);

            _values.Add(value);
        }

        public void InsertValue(int index, T value)
        {
            _values.Insert(index, value);
            for (int i = index; i < _values.Count; ++i)
            {
                MarkChanged(i);
                // also mark all fields of value as changed
                MarkAllUpdateMaskFields(_values[i]);
            }
        }

        public void RemoveValue(int index)
        {
            // remove by shifting entire container - client might rely on values being sorted for certain fields
            _values.RemoveAt(index);
            for (int i = index; i < _values.Count; ++i)
            {
                MarkChanged(i);
                // also mark all fields of value as changed
                MarkAllUpdateMaskFields(_values[i]);
            }
            if ((_values.Count % 32) != 0)
                _updateMask[UpdateMask.GetBlockIndex(_values.Count)] &= (uint)~UpdateMask.GetBlockFlag(_values.Count);
            else
                _updateMask.RemoveAt(_updateMask.Count - 1);
        }

        void MarkAllUpdateMaskFields(T value)
        {
            if (value is IHasChangesMask)
                ((IHasChangesMask)value).GetUpdateMask().SetAll();
        }

        public void Clear()
        {
            _values.Clear();
            _updateMask.Clear();
        }

        public void MarkChanged(int index)
        {
            int block = UpdateMask.GetBlockIndex(index);
            if (block >= _updateMask.Count)
                _updateMask.Add(0);

            _updateMask[block] |= (uint)UpdateMask.GetBlockFlag(index);
        }

        public void ClearChanged(int index)
        {
            int block = UpdateMask.GetBlockIndex(index);
            if (block >= _updateMask.Count)
                _updateMask.Add(0);

            _updateMask[block] &= ~(uint)UpdateMask.GetBlockFlag(index);
        }

        public bool Empty()
        {
            return _values.Empty();
        }

        public int Size()
        {
            return _values.Count;
        }

        public T this[int index]
        {
            get
            {
                if (_values.Count <= index)
                    _values.Add(new T());

                return _values[index];
            }
            set
            {
                _values[index] = value;
            }
        }

        public static implicit operator List<T>(DynamicUpdateField<T> updateField)
        {
            return updateField._values;
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var obj in _values)
                yield return obj;
        }
    }

    public interface IHasChangesMask
    {
        void ClearChangesMask();
        UpdateMask GetUpdateMask();
    }

    public abstract class HasChangesMask : IHasChangesMask
    {
        public UpdateMask _changesMask;
        public int _blockBit;
        public int Bit;

        public HasChangesMask(int blockBit, TypeId bit, int changeMask)
        {
            _blockBit = blockBit;
            Bit = (int)bit;
            _changesMask = new UpdateMask(changeMask);
        }

        public HasChangesMask(int changeMask)
        {
            _changesMask = new UpdateMask(changeMask);
        }

        public abstract void ClearChangesMask();

        public UpdateMask GetUpdateMask()
        {
            return _changesMask;
        }

        public void ClearChanged<U>(UpdateField<U> updateField) where U : new()
        {
            _changesMask.Reset(updateField.Bit);
        }

        public void ClearChanged<U>(UpdateFieldArray<U> updateField, int index) where U : new()
        {
            _changesMask.Reset(updateField.FirstElementBit + index);
        }

        public void ClearChanged<U>(DynamicUpdateField<U> updateField, int index) where U : new()
        {
            _changesMask.Reset(Bit);
            updateField.ClearChanged(index);
        }

        public void ClearChangesMask<U>(UpdateField<U> updateField) where U : new()
        {
            if (typeof(IHasChangesMask).IsAssignableFrom(typeof(U)))
                ((IHasChangesMask)updateField._value).ClearChangesMask();
        }

        public void ClearChangesMask(UpdateFieldString updateField) { }

        public void ClearChangesMask<U>(OptionalUpdateField<U> updateField) where U : new()
        {
            if (typeof(IHasChangesMask).IsAssignableFrom(typeof(U)) && updateField.HasValue())
                ((IHasChangesMask)updateField._value).ClearChangesMask();
        }

        public void ClearChangesMask<U>(UpdateFieldArray<U> updateField) where U : new()
        {
            if (typeof(IHasChangesMask).IsAssignableFrom(typeof(U)))
            {
                for (int i = 0; i < updateField.GetSize(); ++i)
                    ((IHasChangesMask)updateField[i]).ClearChangesMask();
            }
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

        public OptionalUpdateField<U> ModifyValue<U>(OptionalUpdateField<U> updateField) where U : new()
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
            return ref updateField._values[index];
        }

        public DynamicUpdateField<U> ModifyValue<U>(DynamicUpdateField<U> updateField) where U : new()
        {
            _changesMask.Set(updateField.BlockBit);
            _changesMask.Set(updateField.Bit);
            return updateField;
        }

        public DynamicUpdateFieldSetter<U> ModifyValue<U>(DynamicUpdateField<U> updateField, int index) where U : new()
        {
            if (index >= updateField._values.Count)
            {
                // fill with zeros until reaching desired slot
                updateField._values.Resize((uint)index + 1);
                updateField._updateMask.Resize((uint)(updateField._values.Count + 31) / 32);
            }

            _changesMask.Set(updateField.BlockBit);
            _changesMask.Set(updateField.Bit);
            updateField.MarkChanged(index);

            return new DynamicUpdateFieldSetter<U>(updateField, index);
        }

        public void MarkChanged<U>(UpdateField<U> updateField) where U : new()
        {
            _changesMask.Set(updateField.BlockBit);
            _changesMask.Set(updateField.Bit);
        }

        public void MarkChanged<U>(OptionalUpdateField<U> updateField) where U : new()
        {
            _changesMask.Set(updateField.BlockBit);
            _changesMask.Set(updateField.Bit);
        }

        public void MarkChanged(UpdateFieldString updateField)
        {
            _changesMask.Set(updateField.BlockBit);
            _changesMask.Set(updateField.Bit);
        }

        public void MarkChanged<U>(UpdateFieldArray<U> updateField, int index) where U : new()
        {
            _changesMask.Set(updateField.Bit);
            _changesMask.Set(updateField.FirstElementBit + index);
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

    public class DynamicUpdateFieldSetter<T> : IUpdateField<T> where T : new()
    {
        DynamicUpdateField<T> _dynamicUpdateField;
        int _index;

        public DynamicUpdateFieldSetter(DynamicUpdateField<T> dynamicUpdateField, int index)
        {
            _dynamicUpdateField = dynamicUpdateField;
            _index = index;
        }

        public void SetValue(T value)
        {
            _dynamicUpdateField[_index] = value;
        }

        public T GetValue() { return _dynamicUpdateField[_index]; }

        public static implicit operator T(DynamicUpdateFieldSetter<T> dynamicUpdateFieldSetter)
        {
            return dynamicUpdateFieldSetter.GetValue();
        }
    }
}
