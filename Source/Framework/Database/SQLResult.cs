// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using MySqlConnector;
using System;
using System.Runtime.CompilerServices;

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
            if (_reader.IsDBNull(column))
                return default;

            var columnType = _reader.GetFieldType(column);
            if (columnType == typeof(T))
                return _reader.GetFieldValue<T>(column);

            switch (Type.GetTypeCode(columnType))
            {
                case TypeCode.SByte:
                {
                    var value = _reader.GetSByte(column);
                    return Unsafe.As<sbyte, T>(ref value);
                }
                case TypeCode.Byte:
                {
                    var value = _reader.GetByte(column);
                    return Unsafe.As<byte, T>(ref value);
                }
                case TypeCode.Int16:
                {
                    var value = _reader.GetInt16(column);
                    return Unsafe.As<short, T>(ref value);
                }
                case TypeCode.UInt16:
                {
                    var value = _reader.GetUInt16(column);
                    return Unsafe.As<ushort, T>(ref value);
                }
                case TypeCode.Int32:
                {
                    var value = _reader.GetInt32(column);
                    return Unsafe.As<int, T>(ref value);
                }
                case TypeCode.UInt32:
                {
                    var value = _reader.GetUInt32(column);
                    return Unsafe.As<uint, T>(ref value);
                }
                case TypeCode.Int64:
                {
                    var value = _reader.GetInt64(column);
                    return Unsafe.As<long, T>(ref value);
                }
                case TypeCode.UInt64:
                {
                    var value = _reader.GetUInt64(column);
                    return Unsafe.As<ulong, T>(ref value);
                }
                case TypeCode.Single:
                {
                    var value = _reader.GetFloat(column);
                    return Unsafe.As<float, T>(ref value);
                }
                case TypeCode.Double:
                {
                    var value = _reader.GetDouble(column);
                    return Unsafe.As<double, T>(ref value);
                }
            }

            return default;
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
                return default;

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

        public bool IsNull(int column)
        {
            return _currentRow[column] == DBNull.Value;
        }
    }
}
