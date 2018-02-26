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

using Framework.Constants;
using Framework.Database;
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
        public void LoadData(int indexField, DB6FieldInfo[] helpers, HotfixStatements preparedStatement, HotfixStatements preparedStatementLocale)
        {
            SQLResult result = DB.Hotfix.Query(DB.Hotfix.GetPreparedStatement(preparedStatement));
            if (!result.IsEmpty())
            {
                do
                {
                    var idValue = result.Read<uint>(indexField == -1 ? 0 : indexField);

                    var obj = new T();
                    int index = 0;
                    for (var fieldIndex = 0; fieldIndex < helpers.Length; fieldIndex++)
                    {
                        var helper = helpers[fieldIndex];
                        if (helper.IsArray)
                        {
                            Array array = (Array)helper.Getter(obj);
                            for (var i = 0; i < array.Length; ++i)
                            {
                                switch (Type.GetTypeCode(helper.FieldType))
                                {
                                    case TypeCode.SByte:
                                        helper.SetValue(array, result.Read<sbyte>(index++), i);
                                        break;
                                    case TypeCode.Byte:
                                        helper.SetValue(array, result.Read<byte>(index++), i);
                                        break;
                                    case TypeCode.Int16:
                                        helper.SetValue(array, result.Read<short>(index++), i);
                                        break;
                                    case TypeCode.UInt16:
                                        helper.SetValue(array, result.Read<ushort>(index++), i);
                                        break;
                                    case TypeCode.Int32:
                                        helper.SetValue(array, result.Read<int>(index++), i);
                                        break;
                                    case TypeCode.UInt32:
                                        helper.SetValue(array, result.Read<uint>(index++), i);
                                        break;
                                    case TypeCode.Single:
                                        helper.SetValue(array, result.Read<float>(index++), i);
                                        break;
                                    case TypeCode.String:
                                        helper.SetValue(array, result.Read<string>(index++), i);
                                        break;
                                    case TypeCode.Object:
                                        switch (helper.FieldType.Name)
                                        {
                                            case "Vector2":
                                                var vector2 = new Vector2();
                                                vector2.X = result.Read<float>(index++);
                                                vector2.Y = result.Read<float>(index++);
                                                helper.SetValue(array, vector2, i);
                                                break;
                                            case "Vector3":
                                                var vector3 = new Vector3();
                                                vector3.X = result.Read<float>(index++);
                                                vector3.Y = result.Read<float>(index++);
                                                vector3.Z = result.Read<float>(index++);
                                                helper.SetValue(array, vector3, i);
                                                break;
                                            case "LocalizedString":
                                                LocalizedString locString = new LocalizedString();
                                                locString[Global.WorldMgr.GetDefaultDbcLocale()] = result.Read<string>(index++);
                                                helper.SetValue(array, locString, i);
                                                break;
                                            default:
                                                Log.outError(LogFilter.ServerLoading, "Wrong Array Type: {0}", helper.FieldType.Name);
                                                break;
                                        }
                                        break;
                                    default:
                                        Log.outError(LogFilter.ServerLoading, "Wrong Array Type: {0}", helper.FieldType.Name);
                                        break;
                                }
                            }
                        }
                        else
                        {
                            switch (Type.GetTypeCode(helper.FieldType))
                            {
                                case TypeCode.SByte:
                                    helper.SetValue(obj, result.Read<sbyte>(index++));
                                    break;
                                case TypeCode.Byte:
                                    helper.SetValue(obj, result.Read<byte>(index++));
                                    break;
                                case TypeCode.Int16:
                                    helper.SetValue(obj, result.Read<short>(index++));
                                    break;
                                case TypeCode.UInt16:
                                    helper.SetValue(obj, result.Read<ushort>(index++));
                                    break;
                                case TypeCode.Int32:
                                    helper.SetValue(obj, result.Read<int>(index++));
                                    break;
                                case TypeCode.UInt32:
                                    helper.SetValue(obj, result.Read<uint>(index++));
                                    break;
                                case TypeCode.Single:
                                    helper.SetValue(obj, result.Read<float>(index++));
                                    break;
                                case TypeCode.String:
                                    string str = result.Read<string>(index++);
                                    helper.SetValue(obj, str);
                                    break;
                                case TypeCode.Object:
                                    switch (helper.FieldType.Name)
                                    {
                                        case "Vector2":
                                            var vector2 = new Vector2();
                                            vector2.X = result.Read<float>(index++);
                                            vector2.Y = result.Read<float>(index++);
                                            helper.SetValue(obj, vector2);
                                            break;
                                        case "Vector3":
                                            var vector3 = new Vector3();
                                            vector3.X = result.Read<float>(index++);
                                            vector3.Y = result.Read<float>(index++);
                                            vector3.Z = result.Read<float>(index++);
                                            helper.SetValue(obj, vector3);
                                            break;
                                        case "LocalizedString":
                                            LocalizedString locString = new LocalizedString();
                                            locString[Global.WorldMgr.GetDefaultDbcLocale()] = result.Read<string>(index++);
                                            helper.SetValue(obj, locString);
                                            break;
                                        default:
                                            Log.outError(LogFilter.ServerLoading, "Wrong Array Type: {0}", helper.FieldType.Name);
                                            break;
                                    }
                                    break;
                                default:
                                    Log.outError(LogFilter.ServerLoading, "Wrong Array Type: {0}", helper.FieldType.Name);
                                    break;
                            }
                        }
                    }

                    base[idValue] = obj;
                }
                while (result.NextRow());
            }

            if (preparedStatementLocale == 0)
                return;

            for (LocaleConstant locale = 0; locale < LocaleConstant.OldTotal; ++locale)
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

                    for (var i = 0; i < helpers.Length; i++)
                    {
                        var fieldInfo = helpers[i];
                        if (fieldInfo.FieldType != typeof(LocalizedString))
                            continue;

                        LocalizedString locString = (LocalizedString)fieldInfo.Getter(obj);
                        locString[locale] = localeResult.Read<string>(index++);
                    }
                } while (localeResult.NextRow());
            }
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
