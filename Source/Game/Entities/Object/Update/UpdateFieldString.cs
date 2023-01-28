// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Entities
{
    public class UpdateFieldString : IUpdateField<string>
	{
		public string Value { get; set; }
        public int Bit { get; set; }
        public int BlockBit { get; set; }

        public UpdateFieldString(int blockBit, int bit)
		{
			BlockBit = blockBit;
			Bit      = bit;
			Value   = "";
		}

		public void SetValue(string value)
		{
			Value = value;
		}

		public string GetValue()
		{
			return Value;
		}

		public static implicit operator string(UpdateFieldString updateField)
		{
			return updateField.Value;
		}
	}
}