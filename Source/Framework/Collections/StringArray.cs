/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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

using System;
using System.Collections;

namespace Framework.Collections
{
    public class StringArray
    {
        public StringArray(int size)
        {
            _str = new string[size];

            for (var i = 0; i < size; ++i)
                _str[i] = string.Empty;
        }

        public StringArray(string str, params string[] separator)
        {
            if (str.IsEmpty())
                return;

            _str = str.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        }

        public StringArray(string str, params char[] separator)
        {
            if (str.IsEmpty())
                return;

            _str = str.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        }

        public string this[int index]
        {
            get { return _str[index]; }
            set { _str[index] = value; }
        }

        public IEnumerator GetEnumerator()
        {
            return _str.GetEnumerator();
        }

        public bool IsEmpty()
        {
            return _str == null || _str.Length == 0;
        }

        public int Length => _str != null ? _str.Length : 0;

        string[] _str;
    }
}
