// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Entities
{
    public class DynamicUpdateFieldSetter<T> : IUpdateField<T> where T : new()
	{
		private DynamicUpdateField<T> _dynamicUpdateField;
		private int _index;

		public DynamicUpdateFieldSetter(DynamicUpdateField<T> dynamicUpdateField, int index)
		{
			_dynamicUpdateField = dynamicUpdateField;
			_index              = index;
		}

		public void SetValue(T value)
		{
			_dynamicUpdateField[_index] = value;
		}

		public T GetValue()
		{
			return _dynamicUpdateField[_index];
		}

		public static implicit operator T(DynamicUpdateFieldSetter<T> dynamicUpdateFieldSetter)
		{
			return dynamicUpdateFieldSetter.GetValue();
		}
	}
}