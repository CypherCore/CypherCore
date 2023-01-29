// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Entities
{

    public class UpdateField<T> : IUpdateField<T> where T : new()
    {
        public T Value { get; set; }
        public int Bit { get; set; }
        public int BlockBit { get; set; }

        public UpdateField(int blockBit, int bit)
        {
            BlockBit = blockBit;
            Bit = bit;
            Value = new T();
        }

        public void SetValue(T value)
        {
            Value = value;
        }

        public T GetValue()
        {
            return Value;
        }

        public static implicit operator T(UpdateField<T> updateField)
        {
            return updateField.Value;
        }
    }
}