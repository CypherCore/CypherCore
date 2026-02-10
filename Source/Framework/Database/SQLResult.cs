// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Framework.Database
{
    public sealed class SQLResult
    {
        private readonly object[][] _rows;
        private readonly Type[] _fieldTypes;
        private int _rowIndex = 0;

        public SQLResult()
        {
            _rows = Array.Empty<object[]>();
            _fieldTypes = Array.Empty<Type>();
        }

        public SQLResult(MySqlDataReader reader)
        {
            int fieldCount = reader.FieldCount;
            _fieldTypes = new Type[fieldCount];

            for (int i = 0; i < fieldCount; i++)
                _fieldTypes[i] = reader.GetFieldType(i);

            var rows = new List<object[]>();

            while (reader.Read())
            {
                var values = new object[fieldCount];
                reader.GetValues(values);
                rows.Add(values);
            }

            _rows = rows.ToArray();
        }

        public bool NextRow()
        {
            if (_rowIndex + 1 >= _rows.Length)
                return false;

            _rowIndex++;
            return true;
        }

        public bool IsEmpty() => _rows.Length == 0;

        public int GetFieldCount() => _fieldTypes.Length;

        public bool IsNull(int column) => _rows[_rowIndex][column] is DBNull;

        public T Read<T>(int column)
        {
            var value = _rows[_rowIndex][column];
            if (value is DBNull || value == null)
                return default;

            if (value is T t)
                return t;

            var columnType = value.GetType();
            switch (Type.GetTypeCode(columnType))
            {
                case TypeCode.SByte:
                    {
                        var val = (sbyte)value;
                        return Unsafe.As<sbyte, T>(ref val);
                    }
                case TypeCode.Byte:
                    {
                        var val = (byte)value;
                        return Unsafe.As<byte, T>(ref val);
                    }
                case TypeCode.Int16:
                    {
                        var val = (short)value;
                        return Unsafe.As<short, T>(ref val);
                    }
                case TypeCode.UInt16:
                    {
                        var val = (ushort)value;
                        return Unsafe.As<ushort, T>(ref val);
                    }
                case TypeCode.Int32:
                    {
                        var val = (int)value;
                        return Unsafe.As<int, T>(ref val);
                    }
                case TypeCode.UInt32:
                    {
                        var val = (uint)value;
                        return Unsafe.As<uint, T>(ref val);
                    }
                case TypeCode.Int64:
                    {
                        var val = (long)value;
                        return Unsafe.As<long, T>(ref val);
                    }
                case TypeCode.UInt64:
                    {
                        var val = (ulong)value;
                        return Unsafe.As<ulong, T>(ref val);
                    }
                case TypeCode.Single:
                    {
                        var val = (float)value;
                        return Unsafe.As<float, T>(ref val);
                    }
                case TypeCode.Double:
                    {
                        var val = (double)value;
                        return Unsafe.As<double, T>(ref val);
                    }
            }

            return default;
        }

        public T[] ReadValues<T>(int startIndex, int numColumns)
        {
            var values = new T[numColumns];
            for (int i = 0; i < numColumns; i++)
                values[i] = Read<T>(startIndex + i);

            return values;
        }

        public SQLFields GetFields() => new SQLFields(_rows[_rowIndex]);
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
