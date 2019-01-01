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

using Framework.Constants;
using Framework.Database;
using Framework.Dynamic;
using Framework.GameMath;
using Framework.IO;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Game.DataStorage
{
    public interface IDB2Storage
    {
        bool HasRecord(uint id);

        void WriteRecord(uint id, LocaleConstant locale, ByteBuffer buffer);

        void EraseRecord(uint id);
    }

    [Serializable]
    public class DB6Storage<T> : Dictionary<uint, T>, IDB2Storage where T : new()
    {
        public void LoadData(int indexField, HotfixStatements preparedStatement, HotfixStatements preparedStatementLocale)
        {
            SQLResult result = DB.Hotfix.Query(DB.Hotfix.GetPreparedStatement(preparedStatement));
            if (!result.IsEmpty())
            {
                do
                {
                    var id = result.Read<uint>(indexField == -1 ? 0 : indexField);

                    var obj = new T();

                    int dbIndex = 0;
                    foreach (var f in typeof(T).GetFields())
                    {
                        Type type = f.FieldType;

                        if (type.IsArray)
                        {
                            Type arrayElementType = type.GetElementType();
                            if (arrayElementType.IsEnum)
                                arrayElementType = arrayElementType.GetEnumUnderlyingType();

                            Array array = (Array)f.GetValue(obj);
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
                                case TypeCode.Single:
                                    f.SetValue(obj, ReadArray<float>(result, dbIndex, array.Length));
                                    break;
                                case TypeCode.String:
                                    f.SetValue(obj, ReadArray<string>(result, dbIndex, array.Length));
                                    break;
                                case TypeCode.Object:
                                    if (arrayElementType == typeof(Vector3))
                                        f.SetValue(obj, new Vector3(ReadArray<float>(result, dbIndex, 3)));
                                    break;
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
                                case TypeCode.Single:
                                    f.SetValue(obj, result.Read<float>(dbIndex++));
                                    break;
                                case TypeCode.String:
                                    string str = result.Read<string>(dbIndex++);
                                    f.SetValue(obj, str);
                                    break;
                                case TypeCode.Object:
                                    if (type == typeof(LocalizedString))
                                    {
                                        LocalizedString locString = new LocalizedString();
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
                                        f.SetValue(obj, new FlagArray128(ReadArray<uint>(result, dbIndex, 4)));
                                        dbIndex += 4;
                                    }
                                    break;
                                default:
                                    Log.outError(LogFilter.ServerLoading, "Wrong Type: {0}", type.Name);
                                    break;
                            }
                        }
                    }

                    base[id] = obj;
                }
                while (result.NextRow());
            }

            if (preparedStatementLocale == 0)
                return;

            for (LocaleConstant locale = 0; locale < LocaleConstant.Total; ++locale)
            {
                if (Global.WorldMgr.GetDefaultDbcLocale() == locale || locale == LocaleConstant.None)
                    continue;

                PreparedStatement stmt = DB.Hotfix.GetPreparedStatement(preparedStatementLocale);
                stmt.AddValue(0, locale.ToString());
                SQLResult localeResult = DB.Hotfix.Query(stmt);
                if (localeResult.IsEmpty())
                    continue;

                do
                {
                    int index = 0;
                    var obj = this.LookupByKey(localeResult.Read<uint>(index++));
                    if (obj == null)
                        continue;

                    foreach (var f in typeof(T).GetFields())
                    {
                        if (f.FieldType != typeof(LocalizedString))
                            continue;

                        LocalizedString locString = (LocalizedString)f.GetValue(obj);
                        locString[locale] = localeResult.Read<string>(index++);
                    }
                } while (localeResult.NextRow());
            }
        }

        TValue[] ReadArray<TValue>(SQLResult result, int dbIndex, int arrayLength)
        {
            TValue[] values = new TValue[arrayLength];
            for (int i = 0; i < arrayLength; ++i)
                values[i] = result.Read<TValue>(dbIndex + i);

            return values;
        }

        public bool HasRecord(uint id)
        {
            return ContainsKey(id);
        }

        public void WriteRecord(uint id, LocaleConstant locale, ByteBuffer buffer)
        {
            T entry = this.LookupByKey(id);

            foreach (var fieldInfo in entry.GetType().GetFields())
            {
                if (fieldInfo.Name == "Id")
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
                        buffer.WriteUInt8(fieldInfo.GetValue(entry));
                        break;
                    case TypeCode.SByte:
                        buffer.WriteInt8(fieldInfo.GetValue(entry));
                        break;
                    case TypeCode.Byte:
                        buffer.WriteUInt8(fieldInfo.GetValue(entry));
                        break;
                    case TypeCode.Int16:
                        buffer.WriteInt16(fieldInfo.GetValue(entry));
                        break;
                    case TypeCode.UInt16:
                        buffer.WriteUInt16(fieldInfo.GetValue(entry));
                        break;
                    case TypeCode.Int32:
                        buffer.WriteInt32(fieldInfo.GetValue(entry));
                        break;
                    case TypeCode.UInt32:
                        buffer.WriteUInt32(fieldInfo.GetValue(entry));
                        break;
                    case TypeCode.Int64:
                        buffer.WriteInt64(fieldInfo.GetValue(entry));
                        break;
                    case TypeCode.UInt64:
                        buffer.WriteUInt64(fieldInfo.GetValue(entry));
                        break;
                    case TypeCode.Single:
                        buffer.WriteFloat(fieldInfo.GetValue(entry));
                        break;
                    case TypeCode.Object:
                        switch (type.Name)
                        {
                            case "LocalizedString":
                                LocalizedString locStr = (LocalizedString)fieldInfo.GetValue(entry);
                                if (!locStr.HasString(locale))
                                {
                                    locale = 0;
                                    if (!locStr.HasString(locale))
                                    {
                                        buffer.WriteUInt16(0);
                                        break;
                                    }
                                }

                                string str = locStr[locale];
                                buffer.WriteCString(str);
                                break;
                        }
                        break;
                }
            }
        }

        void WriteArrayValues(object entry, FieldInfo fieldInfo, ByteBuffer buffer)
        {
            var type = fieldInfo.FieldType.GetElementType();
            var array = (Array)fieldInfo.GetValue(entry);
            for (var i = 0; i < array.Length; ++i)
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Boolean:
                        buffer.WriteUInt8(array.GetValue(i));
                        break;
                    case TypeCode.SByte:
                        buffer.WriteInt8(array.GetValue(i));
                        break;
                    case TypeCode.Byte:
                        buffer.WriteUInt8(array.GetValue(i));
                        break;
                    case TypeCode.Int16:
                        buffer.WriteInt16(array.GetValue(i));
                        break;
                    case TypeCode.UInt16:
                        buffer.WriteUInt16(array.GetValue(i));
                        break;
                    case TypeCode.Int32:
                        buffer.WriteInt32(array.GetValue(i));
                        break;
                    case TypeCode.UInt32:
                        buffer.WriteUInt32(array.GetValue(i));
                        break;
                    case TypeCode.Int64:
                        buffer.WriteInt64(array.GetValue(i));
                        break;
                    case TypeCode.UInt64:
                        buffer.WriteUInt64(array.GetValue(i));
                        break;
                    case TypeCode.Single:
                        buffer.WriteFloat(array.GetValue(i));
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
    }
}
