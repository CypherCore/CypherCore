// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Networking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Entities
{
    public interface IUpdateField<T>
    {
        void SetValue(T value);
        T GetValue();
    }

    public interface IHasChangesMask
    {
        void ClearChangesMask();
        UpdateMask GetUpdateMask();
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

        public int Size() { return _value.GetByteCount(); }

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

    public class UpdateFieldArrayString
    {
        public string[] _values;
        public int FirstElementBit;
        public int Bit;

        public UpdateFieldArrayString(uint size, int bit, int firstElementBit)
        {
            _values = new string[size];
            for (var i = 0; i < size; ++i)
                _values[i] = "";

            Bit = bit;
            FirstElementBit = firstElementBit;
        }

        public string this[int index]
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

        public IEnumerator<string> GetEnumerator()
        {
            foreach (var obj in _values)
                yield return obj;
        }
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

        public T First(Func<T, bool> predicate)
        {
            return _values.First(predicate);
        }

        public int IndexOf(T value)
        {
            return Array.IndexOf(_values, value);
        }

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
            return (_updateMask[UpdateMask.GetBlockIndex(index)] & UpdateMask.GetBlockFlag(index)) != 0;
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

    public class VariantUpdateField(int blockBit, int bit, params Type[] types)
    {
        public object _value;
        Type[] _types = types;

        public int BlockBit = blockBit;
        public int Bit = bit;

        public bool Is<T>() { return _types.Contains(typeof(T)); }

        public T Get<T>() where T : class, new()
        {
            if (Is<T>())
                return _value as T;

            return null;
        }

        public void Visit(Action<dynamic> visitor)
        {
            visitor(_value);
        }

        public void ConstructValue<T>() where T : class, new()
        {
            _value = new T();
        }
    }

    public abstract class HasChangesMask : IHasChangesMask
    {
        public int ChangeMaskLength;

        public UpdateMask _changesMask;
        public int _blockBit;
        public int Bit;

        public HasChangesMask(int blockBit, TypeId bit, int changeMask)
        {
            _blockBit = blockBit;
            Bit = (int)bit;
            ChangeMaskLength = changeMask;
            _changesMask = new UpdateMask(changeMask);
        }

        public HasChangesMask(int changeMask)
        {
            ChangeMaskLength = changeMask;
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
            if (updateField.FirstElementBit >= 0)
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

        public void ClearChangesMask(UpdateFieldArrayString updateField) { }

        public void ClearChangesMask<U>(DynamicUpdateField<U> updateField) where U : new()
        {
            if (typeof(IHasChangesMask).IsAssignableFrom(typeof(U)))
            {
                for (int i = 0; i < updateField.Size(); ++i)
                    ((IHasChangesMask)updateField[i]).ClearChangesMask();

                updateField.ClearChangesMask();
            }
        }

        public void ClearChangesMask(VariantUpdateField field)
        {
            if (typeof(IHasChangesMask).IsAssignableFrom(field._value.GetType()))
                ((IHasChangesMask)field._value).ClearChangesMask();
        }

        public dynamic ModifyValueOld(dynamic value, int bit, int blockBit)
        {
            _changesMask.Set(blockBit);
            _changesMask.Set(bit);
            return value;
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

        public OptionalUpdateField<U> ModifyValue<U>(OptionalUpdateField<U> updateField) where U : new()
        {
            MarkChanged(updateField);
            return updateField;
        }

        public ref U ModifyValue<U>(UpdateFieldArray<U> updateField, int index) where U : new()
        {
            MarkChanged(updateField, index);
            return ref updateField._values[index];
        }

        public ref string ModifyValue(UpdateFieldArrayString updateField, int index)
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

        public DynamicUpdateFieldSetter<U> ModifyValue<U>(UpdateFieldArray<DynamicUpdateField<U>> updateField, int index, int dynamicIndex) where U : new()
        {
            if (dynamicIndex >= updateField[index].Size())
            {
                // fill with zeros until reaching desired slot
                updateField[index]._values.Resize((uint)dynamicIndex + 1);
                updateField[index]._updateMask.Resize((uint)(updateField[index]._values.Count + 31) / 32);
            }

            _changesMask.Set(updateField.Bit);
            if (updateField.FirstElementBit >= 0)
                _changesMask.Set(updateField.FirstElementBit);

            updateField[index].MarkChanged(dynamicIndex);

            return new DynamicUpdateFieldSetter<U>(updateField[index], dynamicIndex);
        }

        public V ModifyValue<V>(VariantUpdateField field) where V : class, new()
        {
            if (!field.Is<V>())
                field.ConstructValue<V>();

            if (field.BlockBit >= 0)
                _changesMask.Set(field.BlockBit);

            _changesMask.Set(Bit);
            return field.Get<V>();
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
            if (updateField.FirstElementBit >= 0)
                _changesMask.Set(updateField.FirstElementBit + index);
        }

        public void MarkChanged(UpdateFieldArrayString updateField, int index)
        {
            _changesMask.Set(updateField.Bit);
            if (updateField.FirstElementBit >= 0)
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

        public UpdateMask GetStaticUpdateMask()
        {
            return new UpdateMask(ChangeMaskLength);
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
