// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Entities
{
    public class OptionalUpdateField<T> : IUpdateField<T> where T : new()
	{
		private bool _hasValue;
		public T Value { get; set; }
		public int Bit { get; set; }
        public int BlockBit { get; set; }

        public OptionalUpdateField(int blockBit, int bit)
		{
			BlockBit = blockBit;
			Bit      = bit;
		}

		public void SetValue(T value)
		{
			_hasValue = true;
			Value    = value;
		}

		public T GetValue()
		{
			return Value;
		}

		public static implicit operator T(OptionalUpdateField<T> updateField)
		{
			return updateField.Value;
		}

		public bool HasValue()
		{
			return _hasValue;
		}
	}
}