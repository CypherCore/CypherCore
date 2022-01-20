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

namespace Framework.Dynamic
{
    public struct Optional<T>
    {
        private bool _hasValue;
        public T Value;

        public Optional(T value)
        {
            Value = value;
            _hasValue = true;
        }

        public bool HasValue
        {
            get { return _hasValue; }
        }

        public void Set(T value)
        {
            Value = value;
            _hasValue = true;
        }

        public void Clear()
        {
            _hasValue = false;
            Value = default;
        }

        public T ValueOr(T otherValue)
        {
            return HasValue ? Value : otherValue;
        }

        public static explicit operator T(Optional<T> optional)
        {
            return optional.Value;
        }

        public static implicit operator Optional<T>(T value)
        {
            return new Optional<T>(value);
        }
    }
}
