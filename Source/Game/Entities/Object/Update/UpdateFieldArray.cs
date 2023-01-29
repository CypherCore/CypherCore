// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Game.Entities
{
    public class UpdateFieldArray<T> where T : new()
    {
        public T[] Values { get; set; }
        public int Bit { get; set; }
        public int FirstElementBit { get; set; }

        public UpdateFieldArray(uint size, int bit, int firstElementBit)
        {
            Values = new T[size];

            for (var i = 0; i < size; ++i)
                Values[i] = new T();

            Bit = bit;
            FirstElementBit = firstElementBit;
        }

        public T this[int index]
        {
            get => Values[index];
            set => Values[index] = value;
        }

        public int GetSize()
        {
            return Values.Length;
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var obj in Values)
                yield return obj;
        }
    }
}