// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Framework.Dynamic;
using Framework.IO;
using Game.Extendability;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace Game.DataStorage
{
    public interface IDB2Storage
    {
        bool HasRecord(uint id);

        void WriteRecord(uint id, Locale locale, ByteBuffer buffer);

        void EraseRecord(uint id);

        string GetName();
    }

    [Serializable]
    public class DB6Storage<T> : Dictionary<uint, T>, IDB2Storage where T : new()
    {
        WDCHeader _header;
        string _tableName = typeof(T).Name;

        public void LoadData(string fullFileName)
        {
            if (!File.Exists(fullFileName))
            {
                Log.outError(LogFilter.ServerLoading, $"File {fullFileName} not found.");
                return;
            }

            DBReader reader = new();
            using (var stream = new FileStream(fullFileName, FileMode.Open))
            {
                if (!reader.Load(stream))
                {
                    Log.outError(LogFilter.ServerLoading, $"Error loading {fullFileName}.");
                    return;
                }
            }

            _header = reader.Header;

            foreach (var b in reader.Records)
                Add((uint)b.Key, b.Value.As<T>());
        }

        public void LoadHotfixData(BitSet availableDb2Locales, HotfixStatements preparedStatement, HotfixStatements preparedStatementLocale)
        {
            LoadFromDB(false, preparedStatement);
            LoadFromDB(true, preparedStatement);

            if (preparedStatementLocale == 0)
                return;

            for (Locale locale = 0; locale < Locale.Total; ++locale)
            {
                if (!availableDb2Locales[(int)locale])
                    continue;

                LoadStringsFromDB(false, locale, preparedStatementLocale);
                LoadStringsFromDB(true, locale, preparedStatementLocale);
            }
        }

        void LoadFromDB(bool custom, HotfixStatements preparedStatement)
        {
            // Even though this query is executed only once, prepared statement is used to send data from mysql server in binary format
            PreparedStatement stmt = HotfixDatabase.GetPreparedStatement(preparedStatement);
            stmt.AddValue(0, !custom);
            SQLResult result = DB.Hotfix.Query(stmt);
            if (result.IsEmpty())
                return;

            do
            {
                var obj = new T();

                int dbIndex = 0;
                var fields = typeof(T).GetFields();
                foreach (var f in fields)
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
                                    float[] values = ReadArray<float>(result, dbIndex, array.Length * 3);

                                    Vector3[] vectors = new Vector3[array.Length];
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
                                string str = result.Read<string>(dbIndex++);
                                f.SetValue(obj, str);
                                break;
                            case TypeCode.Object:
                                if (type == typeof(LocalizedString))
                                {
                                    LocalizedString locString = new();
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

                    if (fields.Length != 0)
                    {
                        var id = (uint)fields[_header.IdIndex == -1 ? 0 : _header.IdIndex].GetValue(obj);

                        if (WorldConfig.GetDefaultValue<bool>("LoadAllHotfix", false))
                            if (base.TryGetValue(id, out var value) && !IOHelpers.AreObjectsEqual(value, obj))
                                DB2Manager.Instance.AddHotfixRecord(_header.TableHash, id);

                        base[id] = obj;
                    }
                }
                while (result.NextRow());
            }

        void LoadStringsFromDB(bool custom, Locale locale, HotfixStatements preparedStatement)
        {
            PreparedStatement stmt = HotfixDatabase.GetPreparedStatement(preparedStatement);
            // stmt.AddValue(0, !custom); // This is from cypher. I always load all of them.
            stmt.AddValue(0, locale.ToString());
            SQLResult result = DB.Hotfix.Query(stmt);
            if (result.IsEmpty())
                return;

            do
            {
                int index = 0;
                var obj = this.LookupByKey(result.Read<uint>(index++));
                if (obj == null)
                    continue;

                foreach (var f in typeof(T).GetFields())
                {
                    if (f.FieldType != typeof(LocalizedString))
                        continue;

                    LocalizedString locString = (LocalizedString)f.GetValue(obj);
                    locString[locale] = result.Read<string>(index++);
                }
            } while (result.NextRow());
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

        public void WriteRecord(uint id, Locale locale, ByteBuffer buffer)
        {
            T entry = this.LookupByKey(id);

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
                                LocalizedString locStr = (LocalizedString)fieldInfo.GetValue(entry);
                                if (!locStr.HasString(locale))
                                {
                                    locale = 0;
                                    if (!locStr.HasString(locale))
                                    {
                                        buffer.WriteUInt8(0);
                                        break;
                                    }
                                }

                                string str = locStr[locale];
                                buffer.WriteCString(str);
                                break;
                            case "Vector2":
                                Vector2 vector2 = (Vector2)fieldInfo.GetValue(entry);
                                buffer.WriteVector2(vector2);
                                break;
                            case "Vector3":
                                Vector3 vector3 = (Vector3)fieldInfo.GetValue(entry);
                                buffer.WriteVector3(vector3);
                                break;
                            case "FlagArray128":
                                FlagArray128 flagArray128 = (FlagArray128)fieldInfo.GetValue(entry);
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

        void WriteArrayValues(object entry, FieldInfo fieldInfo, ByteBuffer buffer)
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

        public uint GetNumRows() { return Keys.Max() + 1; }

        public string GetName()
        {
            return _tableName;
        }
    }
}
