/*
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
using Game.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Game.Entities
{
    public class UpdateFieldHolder
    {
        UpdateMask _changesMask = new UpdateMask((int)TypeId.Max);
        WorldObject _owner;

        public UpdateFieldHolder(WorldObject owner) { _owner = owner; }

        public BaseUpdateData<T> ModifyValue<T>(BaseUpdateData<T> updateData)
        {
            _changesMask.Set(updateData.Bit);
            return updateData;
        }

        public void ClearChangesMask<T>(BaseUpdateData<T> updateData)
        {
            _changesMask.Reset(updateData.Bit);
            updateData.ClearChangesMask();
        }

        public void ClearChangesMask<T, U>(BaseUpdateData<T> updateData, ref UpdateField<U> updateField) where T : new() where U : new()
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

        public int FindIndexIf(Predicate<T> blah)
        {
            return _values.FindIndex(blah);
        }

        public bool HasChanged(int index)
        {
            return (_updateMask[index / 32] & (1 << (index % 32))) != 0;
        }

        public void WriteUpdateMask(WorldPacket data)
        {
            data.WriteBits(_values.Count, 32);
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

            if (value is IHasChangesMask)
            {
                IHasChangesMask hasChanges = (IHasChangesMask)value;
                if (hasChanges != null)
                    hasChanges.GetUpdateMask().SetAll();
            }

            _values.Add(value);
        }

        public void InsertValue(int index, T value)
        {
            _values.Insert(index, value);
            for (int i = index; i < _values.Count; ++i)
            {
                MarkChanged(i);
                // also mark all fields of value as changed
                IHasChangesMask hasChangesMask = (IHasChangesMask)_values[i];
                if (hasChangesMask != null)
                    hasChangesMask.GetUpdateMask().SetAll();
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
                IHasChangesMask hasChanges = (IHasChangesMask)_values[i];
                if (hasChanges != null)
                    hasChanges.GetUpdateMask().SetAll();
            }
            if ((_values.Count % 32) != 0)
                _updateMask[UpdateMask.GetBlockIndex(_values.Count)] &= (uint)~UpdateMask.GetBlockFlag(_values.Count);
            else
                _updateMask.RemoveAt(_updateMask.Count - 1);
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

    public abstract class BaseUpdateData<T> : IHasChangesMask
    {
        public UpdateMask _changesMask;
        public int _blockBit;
        public int Bit;

        public BaseUpdateData(int blockBit, TypeId bit, int changeMask)
        {
            _blockBit = blockBit;
            Bit = (int)bit;
            _changesMask = new UpdateMask(changeMask);
        }

        public BaseUpdateData(int changeMask)
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
            _changesMask.Reset(updateField.Bit);
            _changesMask.Reset(updateField.FirstElementBit + index);
        }

        public void ClearChanged<U>(DynamicUpdateField<U> updateField, int index) where U : new()
        {
            _changesMask.Reset(Bit);
            updateField.ClearChanged(index);
        }

        public void ClearChangesMask<U>(UpdateField<U> updateField) where U : new()
        {
            if (typeof(U).GetInterfaces().Any(x => typeof(IHasChangesMask) == x))
                ((IHasChangesMask)updateField._value).ClearChangesMask();
        }

        public void ClearChangesMask<U>(UpdateFieldArray<U> updateField) where U : new()
        {
            if (typeof(U).GetInterfaces().Any(x => typeof(IHasChangesMask) == x))
            {
                for (int i = 0; i < updateField.GetSize(); ++i)
                    ((IHasChangesMask)updateField[i]).ClearChangesMask();
            }
        }

        public void ClearChangesMask<U>(DynamicUpdateField<U> updateField) where U : new()
        {
            if (typeof(U).GetInterfaces().Any(x => typeof(IHasChangesMask) == x))
            {
                for (int i = 0; i < updateField.Size(); ++i)
                    ((IHasChangesMask)updateField[i]).ClearChangesMask();

                updateField.ClearChangesMask();
            }
        }

        public UpdateField<UU> ModifyValue<U, UU>(Expression<Func<U, UpdateField<UU>>> expression) where UU : new()
        {
            var fieldInfo = ((MemberExpression)expression.Body).Member as FieldInfo;
            if (fieldInfo == null)
                throw new ArgumentException("The lambda expression 'property' should point to a valid Property");

            var updateField = (UpdateField<UU>)fieldInfo.GetValue(this);
            _changesMask.Set(updateField.BlockBit);
            _changesMask.Set(updateField.Bit);
            return updateField;
        }

        public ref UU ModifyValue<U, UU>(Expression<Func<U, UpdateFieldArray<UU>>> expression, int index) where UU : new()
        {
            var fieldInfo = ((MemberExpression)expression.Body).Member as FieldInfo;
            if (fieldInfo == null)
                throw new ArgumentException("The lambda expression should point to a valid Field");

            var updateFieldArray = (UpdateFieldArray<UU>)fieldInfo.GetValue(this);
            _changesMask.Set(updateFieldArray.Bit);
            _changesMask.Set(updateFieldArray.FirstElementBit + index);
            return ref updateFieldArray._values[index];
        }

        public UpdateField<U> ModifyValue<U>(UpdateField<U> updateField) where U : new()
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

        public void MarkChanged<U>(UpdateFieldArray<U> updateField, int index) where U : new()
        {
            _changesMask.Set(updateField.Bit);
            _changesMask.Set(updateField.FirstElementBit + index);
        }

        public void WriteCompleteDynamicFieldUpdateMask(int size, WorldPacket data)
        {
            data.WriteBits(size, 32);
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
