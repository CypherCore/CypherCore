// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
