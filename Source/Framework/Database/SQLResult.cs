/*
 * Copyright (C) 2012-2018 CypherCore <http://github.com/CypherCore>
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
using System.Collections.Generic;

namespace Framework.Database
{
    public class SQLResult
    {
        public SQLResult()
        {
            _rows = new List<object[]>();
        }
        public SQLResult(List<object[]> values)
        {
            _rows = values;
        }

        public T Read<T>(int column)
        {
            var value = _rows[_rowIndex][column];

            if (value.GetType() == typeof(T))
                return (T)value;

            if (value != DBNull.Value)
                return (T)Convert.ChangeType(value, typeof(T));

            if (typeof(T).Name == "String")
                return (T)Convert.ChangeType("", typeof(T));

            return default(T);
        }

        public T[] ReadValues<T>(int startIndex, int numColumns)
        {
            T[] values = new T[numColumns];
            for (var c = 0; c < numColumns; ++c)
                values[c] = Read<T>(startIndex + c);

            return values;
        }

        public bool IsNull(int column)
        {
            return _rows[_rowIndex][column] == DBNull.Value;
        }

        public int GetRowCount() { return _rows.Count; }

        public bool IsEmpty() { return GetRowCount() == 0; }

        public SQLFields GetFields() { return new SQLFields(_rows[_rowIndex]); }

        public bool NextRow()
        {
            if (_rowIndex >= GetRowCount() - 1)
            {
                _rowIndex = 0;
                return false;
            }

            _rowIndex++;
            return true;
        }

        public void ResetRowIndex()
        {
            _rowIndex = 0;
        }

        private int _rowIndex;
        List<object[]> _rows;
    }

    public class SQLFields
    {
        public SQLFields(object[] row) { _currentRow = row; }

        public T Read<T>(int column)
        {
            var value = _currentRow[column];

            if (value.GetType() == typeof(T))
                return (T)value;

            if (value != DBNull.Value)
                return (T)Convert.ChangeType(value, typeof(T));

            if (typeof(T).Name == "String")
                return (T)Convert.ChangeType("", typeof(T));

            return default(T);
        }

        public T[] ReadValues<T>(int startIndex, int numColumns)
        {
            T[] values = new T[numColumns];
            for (var c = 0; c < numColumns; ++c)
                values[c] = Read<T>(startIndex + c);

            return values;
        }

        object[] _currentRow;
    }
}
