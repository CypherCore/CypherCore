// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Game.Networking;

namespace Game.Entities
{
    public class DynamicUpdateField<T> where T : new()
    {
        public List<uint> UpdateMask { get; set; }
        public List<T> Values { get; set; }
        public int Bit { get; set; }
        public int BlockBit { get; set; }

        public DynamicUpdateField()
        {
            Values = new List<T>();
            UpdateMask = new List<uint>();

            BlockBit = -1;
            Bit = -1;
        }

        public DynamicUpdateField(int blockBit, int bit)
        {
            Values = new List<T>();
            UpdateMask = new List<uint>();

            BlockBit = blockBit;
            Bit = bit;
        }

        public T this[int index]
        {
            get
            {
                if (Values.Count <= index)
                    Values.Add(new T());

                return Values[index];
            }
            set => Values[index] = value;
        }

        public int FindIndex(T value)
        {
            return Values.IndexOf(value);
        }

        public int FindIndexIf(Predicate<T> predicate)
        {
            return Values.FindIndex(predicate);
        }

        public bool HasChanged(int index)
        {
            return (UpdateMask[index / 32] & (1 << (index % 32))) != 0;
        }

        public void WriteUpdateMask(WorldPacket data, int bitsForSize = 32)
        {
            data.WriteBits(Values.Count, bitsForSize);

            if (Values.Count > 32)
            {
                if (data.HasUnfinishedBitPack())
                    for (int block = 0; block < Values.Count / 32; ++block)
                        data.WriteBits(UpdateMask[block], 32);
                else
                    for (int block = 0; block < Values.Count / 32; ++block)
                        data.WriteUInt32(UpdateMask[block]);
            }

            else if (Values.Count == 32)
            {
                data.WriteBits(UpdateMask.Last(), 32);

                return;
            }

            if ((Values.Count % 32) != 0)
                data.WriteBits(UpdateMask.Last(), Values.Count % 32);
        }

        public void ClearChangesMask()
        {
            for (var i = 0; i < UpdateMask.Count; ++i)
                UpdateMask[i] = 0;
        }

        public void AddValue(T value)
        {
            MarkChanged(Values.Count);
            MarkAllUpdateMaskFields(value);

            Values.Add(value);
        }

        public void InsertValue(int index, T value)
        {
            Values.Insert(index, value);

            for (int i = index; i < Values.Count; ++i)
            {
                MarkChanged(i);
                // also mark all fields of value as changed
                MarkAllUpdateMaskFields(Values[i]);
            }
        }

        public void RemoveValue(int index)
        {
            // remove by shifting entire container - client might rely on values being sorted for certain fields
            Values.RemoveAt(index);

            for (int i = index; i < Values.Count; ++i)
            {
                MarkChanged(i);
                // also mark all fields of value as changed
                MarkAllUpdateMaskFields(Values[i]);
            }

            if ((Values.Count % 32) != 0)
                UpdateMask[Entities.UpdateMask.GetBlockIndex(Values.Count)] &= (uint)~Entities.UpdateMask.GetBlockFlag(Values.Count);
            else
                UpdateMask.RemoveAt(UpdateMask.Count - 1);
        }

        private void MarkAllUpdateMaskFields(T value)
        {
            if (value is IHasChangesMask)
                ((IHasChangesMask)value).GetUpdateMask().SetAll();
        }

        public void Clear()
        {
            Values.Clear();
            UpdateMask.Clear();
        }

        public void MarkChanged(int index)
        {
            int block = Entities.UpdateMask.GetBlockIndex(index);

            if (block >= UpdateMask.Count)
                UpdateMask.Add(0);

            UpdateMask[block] |= (uint)Entities.UpdateMask.GetBlockFlag(index);
        }

        public void ClearChanged(int index)
        {
            int block = Entities.UpdateMask.GetBlockIndex(index);

            if (block >= UpdateMask.Count)
                UpdateMask.Add(0);

            UpdateMask[block] &= ~(uint)Entities.UpdateMask.GetBlockFlag(index);
        }

        public bool Empty()
        {
            return Values.Empty();
        }

        public int Size()
        {
            return Values.Count;
        }

        public static implicit operator List<T>(DynamicUpdateField<T> updateField)
        {
            return updateField.Values;
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var obj in Values)
                yield return obj;
        }
    }
}