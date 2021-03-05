﻿/*
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

using Framework.Constants;
using Framework.Database;
using Framework.Dynamic;
using Framework.GameMath;
using Framework.IO;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;

namespace Game.DataStorage
{
    public interface IDB2Storage
    {
        bool HasRecord(uint id);

        void WriteRecord(uint id, Locale locale, ByteBuffer buffer);

        void EraseRecord(uint id);
    }

    [Serializable]
    public class DB6Storage<T> : Dictionary<uint, T>, IDB2Storage where T : new()
    {
        private WDCHeader _header;

        public void LoadData(WDCHeader header, BitSet availableDb2Locales, HotfixStatements preparedStatement, HotfixStatements preparedStatementLocale)
        {
            _header = header;

            var result = DB.Hotfix.Query(DB.Hotfix.GetPreparedStatement(preparedStatement));
            if (!result.IsEmpty())
            {
                do
                {
                    var obj = new T();

                    var dbIndex = 0;
                    var fields = typeof(T).GetFields();
                    foreach (var f in typeof(T).GetFields())
                    {
                        var type = f.FieldType;

                        if (type.IsArray)
                        {
                            var arrayElementType = type.GetElementType();
                            if (arrayElementType.IsEnum)
                                arrayElementType = arrayElementType.GetEnumUnderlyingType();

                            var array = (Array)f.GetValue(obj);
                            switch (Type.GetTypeCode(arrayElementType))
                            {
                                case TypeCode.SByte:
                                    f.SetValue(obj, ReadArray<sbyte>(result, dbIndex, array.Length));
                                    break;
                                case TypeCode.Byte:
                                    f.SetValue(obj, ReadArray<byte>(result, dbIndex, array.Length));
                                    break;
                                case TypeCode.Int16:
                                    f.SetValue(obj, ReadArray<short>(result, dbIndex, array.Length));
                                    break;
                                case TypeCode.UInt16:
                                    f.SetValue(obj, ReadArray<ushort>(result, dbIndex, array.Length));
                                    break;
                                case TypeCode.Int32:
                                    f.SetValue(obj, ReadArray<int>(result, dbIndex, array.Length));
                                    break;
                                case TypeCode.UInt32:
                                    f.SetValue(obj, ReadArray<uint>(result, dbIndex, array.Length));
                                    break;
                                case TypeCode.Int64:
                                    f.SetValue(obj, ReadArray<long>(result, dbIndex, array.Length));
                                    break;
                                case TypeCode.UInt64:
                                    f.SetValue(obj, ReadArray<ulong>(result, dbIndex, array.Length));
                                    break;
                                case TypeCode.Single:
                                    f.SetValue(obj, ReadArray<float>(result, dbIndex, array.Length));
                                    break;
                                case TypeCode.String:
                                    f.SetValue(obj, ReadArray<string>(result, dbIndex, array.Length));
                                    break;
                                case TypeCode.Object:
                                    if (arrayElementType == typeof(Vector3))
                                    {
                                        var values = ReadArray<float>(result, dbIndex, array.Length * 3);

                                        var vectors = new Vector3[array.Length];
                                        for (var i = 0; i < array.Length; ++i)
                                            vectors[i] = new Vector3(values[(i * 3)..(3 + (i * 3))]);

                                        f.SetValue(obj, vectors);

                                        dbIndex += array.Length * 3;
                                    }
                                    continue;
                                default:
                                    Log.outError(LogFilter.ServerLoading, "Wrong Array Type: {0}", arrayElementType.Name);
                                    break;
                            }

                            dbIndex += array.Length;
                        }
                        else
                        {
                            if (type.IsEnum)
                                type = type.GetEnumUnderlyingType();

                            switch (Type.GetTypeCode(type))
                            {
                                case TypeCode.SByte:
                                    f.SetValue(obj, result.Read<sbyte>(dbIndex++));
                                    break;
                                case TypeCode.Byte:
                                    f.SetValue(obj, result.Read<byte>(dbIndex++));
                                    break;
                                case TypeCode.Int16:
                                    f.SetValue(obj, result.Read<short>(dbIndex++));
                                    break;
                                case TypeCode.UInt16:
                                    f.SetValue(obj, result.Read<ushort>(dbIndex++));
                                    break;
                                case TypeCode.Int32:
                                    f.SetValue(obj, result.Read<int>(dbIndex++));
                                    break;
                                case TypeCode.UInt32:
                                    f.SetValue(obj, result.Read<uint>(dbIndex++));
                                    break;
                                case TypeCode.Int64:
                                    f.SetValue(obj, result.Read<long>(dbIndex++));
                                    break;
                                case TypeCode.UInt64:
                                    f.SetValue(obj, result.Read<ulong>(dbIndex++));
                                    break;
                                case TypeCode.Single:
                                    f.SetValue(obj, result.Read<float>(dbIndex++));
                                    break;
                                case TypeCode.String:
                                    var str = result.Read<string>(dbIndex++);
                                    f.SetValue(obj, str);
                                    break;
                                case TypeCode.Object:
                                    if (type == typeof(LocalizedString))
                                    {
                                        var locString = new LocalizedString();
                                        locString[Global.WorldMgr.GetDefaultDbcLocale()] = result.Read<string>(dbIndex++);

                                        f.SetValue(obj, locString);
                                    }
                                    else if (type == typeof(Vector2))
                                    {
                                        f.SetValue(obj, new Vector2(ReadArray<float>(result, dbIndex, 2)));
                                        dbIndex += 2;
                                    }
                                    else if (type == typeof(Vector3))
                                    {
                                        f.SetValue(obj, new Vector3(ReadArray<float>(result, dbIndex, 3)));
                                        dbIndex += 3;
                                    }
                                    else if (type == typeof(FlagArray128))
                                    {
                                        f.SetValue(obj, new FlagArray128(ReadArray<int>(result, dbIndex, 4)));
                                        dbIndex += 4;
                                    }
                                    break;
                                default:
                                    Log.outError(LogFilter.ServerLoading, "Wrong Type: {0}", type.Name);
                                    break;
                            }
                        }
                    }

                    var id = (uint)fields[header.IdIndex == -1 ? 0 : header.IdIndex].GetValue(obj);
                    base[id] = obj;
                }
                while (result.NextRow());
            }

            if (preparedStatementLocale == 0)
                return;

            for (Locale locale = 0; locale < Locale.Total; ++locale)
            {
                if (Global.WorldMgr.GetDefaultDbcLocale() == locale || !availableDb2Locales[(int)locale])
                    continue;

                var stmt = DB.Hotfix.GetPreparedStatement(preparedStatementLocale);
                stmt.AddValue(0, locale.ToString());
                var localeResult = DB.Hotfix.Query(stmt);
                if (localeResult.IsEmpty())
                    continue;

                do
                {
                    var index = 0;
                    var obj = this.LookupByKey(localeResult.Read<uint>(index++));
                    if (obj == null)
                        continue;

                    foreach (var f in typeof(T).GetFields())
                    {
                        if (f.FieldType != typeof(LocalizedString))
                            continue;

                        var locString = (LocalizedString)f.GetValue(obj);
                        locString[locale] = localeResult.Read<string>(index++);
                    }
                } while (localeResult.NextRow());
            }
        }

        private TValue[] ReadArray<TValue>(SQLResult result, int dbIndex, int arrayLength)
        {
            var values = new TValue[arrayLength];
            for (var i = 0; i < arrayLength; ++i)
                values[i] = result.Read<TValue>(dbIndex + i);

            return values;
        }

        public bool HasRecord(uint id)
        {
            return ContainsKey(id);
        }

        public void WriteRecord(uint id, Locale locale, ByteBuffer buffer)
        {
            var entry = this.LookupByKey(id);

            foreach (var fieldInfo in entry.GetType().GetFields())
            {
                if (fieldInfo.Name == "Id" && _header.HasIndexTable())
                    continue;

                var type = fieldInfo.FieldType;
                if (type.IsArray)
                {
                    WriteArrayValues(entry, fieldInfo, buffer);
                    continue;
                }

                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Boolean:
                        buffer.WriteUInt8((byte)((bool)fieldInfo.GetValue(entry) ? 1 : 0));
                        break;
                    case TypeCode.SByte:
                        buffer.WriteInt8((sbyte)fieldInfo.GetValue(entry));
                        break;
                    case TypeCode.Byte:
                        buffer.WriteUInt8((byte)fieldInfo.GetValue(entry));
                        break;
                    case TypeCode.Int16:
                        buffer.WriteInt16((short)fieldInfo.GetValue(entry));
                        break;
                    case TypeCode.UInt16:
                        buffer.WriteUInt16((ushort)fieldInfo.GetValue(entry));
                        break;
                    case TypeCode.Int32:
                        buffer.WriteInt32((int)fieldInfo.GetValue(entry));
                        break;
                    case TypeCode.UInt32:
                        buffer.WriteUInt32((uint)fieldInfo.GetValue(entry));
                        break;
                    case TypeCode.Int64:
                        buffer.WriteInt64((long)fieldInfo.GetValue(entry));
                        break;
                    case TypeCode.UInt64:
                        buffer.WriteUInt64((ulong)fieldInfo.GetValue(entry));
                        break;
                    case TypeCode.Single:
                        buffer.WriteFloat((float)fieldInfo.GetValue(entry));
                        break;
                    case TypeCode.String:
                        buffer.WriteCString((string)fieldInfo.GetValue(entry));
                        break;
                    case TypeCode.Object:
                        switch (type.Name)
                        {
                            case "LocalizedString":
                                var locStr = (LocalizedString)fieldInfo.GetValue(entry);
                                if (!locStr.HasString(locale))
                                {
                                    locale = 0;
                                    if (!locStr.HasString(locale))
                                    {
                                        buffer.WriteUInt16(0);
                                        break;
                                    }
                                }

                                var str = locStr[locale];
                                buffer.WriteCString(str);
                                break;
                            case "Vector2":
                                var vector2 = (Vector2)fieldInfo.GetValue(entry);
                                buffer.WriteVector2(vector2);
                                break;
                            case "Vector3":
                                var vector3 = (Vector3)fieldInfo.GetValue(entry);
                                buffer.WriteVector3(vector3);
                                break;
                            case "FlagArray128":
                                var flagArray128 = (FlagArray128)fieldInfo.GetValue(entry);
                                buffer.WriteUInt32(flagArray128[0]);
                                buffer.WriteUInt32(flagArray128[1]);
                                buffer.WriteUInt32(flagArray128[2]);
                                buffer.WriteUInt32(flagArray128[3]);
                                break;
                            default:
                                throw new Exception($"Unhandled Custom type: {type.Name}");
                        }
                        break;
                }
            }
        }

        private void WriteArrayValues(object entry, FieldInfo fieldInfo, ByteBuffer buffer)
        {
            var type = fieldInfo.FieldType.GetElementType();
            var array = (Array)fieldInfo.GetValue(entry);
            for (var i = 0; i < array.Length; ++i)
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Boolean:
                        buffer.WriteUInt8((byte)((bool)array.GetValue(i) ? 1 : 0));
                        break;
                    case TypeCode.SByte:
                        buffer.WriteInt8((sbyte)array.GetValue(i));
                        break;
                    case TypeCode.Byte:
                        buffer.WriteUInt8((byte)array.GetValue(i));
                        break;
                    case TypeCode.Int16:
                        buffer.WriteInt16((short)array.GetValue(i));
                        break;
                    case TypeCode.UInt16:
                        buffer.WriteUInt16((ushort)array.GetValue(i));
                        break;
                    case TypeCode.Int32:
                        buffer.WriteInt32((int)array.GetValue(i));
                        break;
                    case TypeCode.UInt32:
                        buffer.WriteUInt32((uint)array.GetValue(i));
                        break;
                    case TypeCode.Int64:
                        buffer.WriteInt64((long)array.GetValue(i));
                        break;
                    case TypeCode.UInt64:
                        buffer.WriteUInt64((ulong)array.GetValue(i));
                        break;
                    case TypeCode.Single:
                        buffer.WriteFloat((float)array.GetValue(i));
                        break;
                    case TypeCode.String:
                        var str = (string)array.GetValue(i);
                        buffer.WriteCString(str);
                        break;
                }
            }
        }

        public void EraseRecord(uint id)
        {
            Remove(id);
        }

        public uint GetTableHash() { return _header.TableHash; }
    }
}
