// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Framework.Database
{
    public class SQLResult
    {
        readonly SQLFields[] _rows = [];
        readonly int _fieldCount;
        int _rowIndex;

        public SQLResult() { }

        public SQLResult(MySqlDataReader reader)
        {
            _fieldCount = reader.FieldCount;

            var rows = new List<SQLFields>();

            while (reader.Read())
            {
                var values = new object[_fieldCount];
                reader.GetValues(values);
                rows.Add(new SQLFields(values));
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

        public SQLFields GetFields() => _rows[_rowIndex];

        public bool IsEmpty() => _rows.Length == 0;

        public int GetFieldCount() { return _fieldCount; }

        public int GetRowCount() { return _rows.Length; }

        public T Read<T>(int column)
        {
            return _rows[_rowIndex].Read<T>(column);
        }

        public bool IsNull(int column)
        {
            return _rows[_rowIndex].IsNull(column);
        }
    }

    public class SQLFields
    {
        object[] _currentRow;

        public SQLFields(object[] row)
        {
            _currentRow = row;
        }

        public T Read<T>(int column)
        {
            var value = _currentRow[column];
            if (IsNull(column) || value == null)
                return default;

            if (value is T t)
                return t;

            switch (Type.GetTypeCode(value.GetType()))
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

        public bool IsNull(int column)
        {
            return _currentRow[column] == DBNull.Value;
        }
    }
}
