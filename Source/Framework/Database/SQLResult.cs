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

using MySql.Data.MySqlClient;
using System;

namespace Framework.Database
{
    public class SQLResult
    {
        MySqlDataReader _reader;

        public SQLResult() { }

        public SQLResult(MySqlDataReader reader)
        {
            _reader = reader;
            NextRow();
        }

        ~SQLResult()
        {
            _reader = null;
        }

        public T Read<T>(int column)
        {
            var value = _reader[column];

            if (value == DBNull.Value)
                return default(T);

            if (value.GetType() != typeof(T))
                return (T)Convert.ChangeType(value, typeof(T));//todo remove me when all fields are the right type  this is super slow

            return (T)value;
        }

        public bool IsNull(int column)
        {
            return _reader.IsDBNull(column);
        }

        public int GetFieldCount() { return _reader.FieldCount; }

        public bool IsEmpty()
        {
            if (_reader == null)
                return true;
            
            return _reader.IsClosed || !_reader.HasRows;
        }

        public SQLFields GetFields()
        {
            object[] values = new object[_reader.FieldCount];
            _reader.GetValues(values);
            return new SQLFields(values);
        }

        public bool NextRow()
        {
            if (_reader == null)
                return false;

            if (_reader.Read())
                return true;

            _reader.Close();
            return false;
        }
    }

    public class SQLFields
    {
        object[] _currentRow;

        public SQLFields(object[] row) { _currentRow = row; }

        public T Read<T>(int column)
        {
            var value = _currentRow[column];

            if (value == DBNull.Value)
                return default(T);

            if (value.GetType() != typeof(T))
                return (T)Convert.ChangeType(value, typeof(T));//todo remove me when all fields are the right type  this is super slow

            return (T)value;
        }

        public T[] ReadValues<T>(int startIndex, int numColumns)
        {
            T[] values = new T[numColumns];
            for (var c = 0; c < numColumns; ++c)
                values[c] = Read<T>(startIndex + c);

            return values;
        }
    }
}
