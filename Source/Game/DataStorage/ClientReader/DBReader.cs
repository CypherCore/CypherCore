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

using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Framework.Dynamic;
using Framework.GameMath;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Game.DataStorage
{
    class DBReader
    {
        public static DB6Storage<T> Read<T>(string fileName, HotfixStatements preparedStatement, HotfixStatements preparedStatementLocale = 0) where T : new()
        {
            ClearData();

            DB6Storage<T> storage = new DB6Storage<T>();

            if (!File.Exists(CliDB.DataPath + fileName))
            {
                Log.outError(LogFilter.ServerLoading, "File {0} not found.", fileName);
                return storage;
            }

            //First lets load field Info
            var fields = typeof(T).GetFields();
            DB6FieldInfo[] fieldsInfo = new DB6FieldInfo[fields.Length];
            for (var i = 0; i < fields.Length; ++i)
                fieldsInfo[i] = new DB6FieldInfo(fields[i]);

            using (var fileReader = new BinaryReader(new MemoryStream(File.ReadAllBytes(CliDB.DataPath + fileName))))
            {
                ReadHeader(fileReader);

                var records = ReadData(fileReader);
                foreach (var pair in records)
                {
                    using (BinaryReader dataReader = new BinaryReader(new MemoryStream(pair.Value), System.Text.Encoding.UTF8))
                    {
                        var obj = new T();

                        int objectFieldIndex = 0;
                        //First check if index is in data
                        if (Header.HasIndexTable())
                            fieldsInfo[objectFieldIndex++].SetValue(obj, (uint)pair.Key);

                        for (var dataFieldIndex = 0; dataFieldIndex < Header.FieldCount; ++dataFieldIndex)
                        {
                            int arrayLength = ColumnMeta[dataFieldIndex].ArraySize;
                            if (arrayLength > 1)
                            {
                                while (arrayLength > 0)
                                {
                                    var fieldInfo = fieldsInfo[objectFieldIndex++];
                                    if (fieldInfo.IsArray)
                                    {
                                        Array array = (Array)fieldInfo.Getter(obj);
                                        SetArrayValue(obj, (uint)array.Length, fieldInfo, dataReader);

                                        arrayLength -= array.Length;
                                    }
                                    else
                                    {
                                        //Only Data is Array
                                        if (Type.GetTypeCode(fieldInfo.FieldType) == TypeCode.Object)
                                        {
                                            switch (fieldInfo.FieldType.Name)
                                            {
                                                case "Vector2":
                                                    fieldInfo.SetValue(obj, dataReader.Read<Vector2>());
                                                    arrayLength -= 2;
                                                    break;
                                                case "Vector3":
                                                    fieldInfo.SetValue(obj, dataReader.Read<Vector3>());
                                                    arrayLength -= 3;
                                                    break;
                                                case "LocalizedString":
                                                    LocalizedString locString = new LocalizedString();
                                                    locString[Global.WorldMgr.GetDefaultDbcLocale()] = GetString(dataReader);
                                                    fieldInfo.SetValue(obj, locString);
                                                    arrayLength -= 1;
                                                    break;
                                                case "FlagArray128":
                                                    fieldInfo.SetValue(obj, new FlagArray128(dataReader.ReadUInt32(), dataReader.ReadUInt32(), dataReader.ReadUInt32(), dataReader.ReadUInt32()));
                                                    arrayLength -= 4;
                                                    break;
                                                default:
                                                    Log.outError(LogFilter.ServerLoading, "Unknown Array Type {0} in DBClient File", fieldInfo.FieldType.Name, nameof(T));
                                                    break;
                                            }
                                        }
                                        else
                                            SetValue(obj, fieldInfo, dataReader);
                                    }

                                    dataReader.BaseStream.Position += GetPadding(fieldInfo.FieldType, dataFieldIndex);
                                }
                            }
                            else
                            {
                                var fieldInfo = fieldsInfo[objectFieldIndex++];
                                if (fieldInfo.IsArray)
                                {
                                    Array array = (Array)fieldInfo.Getter(obj);
                                    SetArrayValue(obj, (uint)array.Length, fieldInfo, dataReader);

                                    dataFieldIndex += array.Length - 1;
                                }
                                else
                                    SetValue(obj, fieldInfo, dataReader);

                                dataReader.BaseStream.Position += GetPadding(fieldInfo.FieldType, dataFieldIndex);
                            }
                        }

                        //Check if there is parent ids and fill them
                        if (objectFieldIndex < fieldsInfo.Length && Header.LookupColumnCount > 0)
                            fieldsInfo[objectFieldIndex].SetValue(obj, dataReader.ReadUInt32());

                        storage.Add((uint)pair.Key, obj);
                    }
                }

                storage.LoadData(Header.IdIndex, fieldsInfo, preparedStatement, preparedStatementLocale);
            }

            Global.DB2Mgr.AddDB2(Header.TableHash, storage);
            CliDB.LoadedFileCount++;
            return storage;
        }

        static void ClearData()
        {
            Header = null;
            StringTable = null;
            FieldStructure = null;
            ColumnMeta = null;
        }

        static void ReadHeader(BinaryReader reader)
        {
            Header = new DB6Header();
            Header.Signature = reader.ReadUInt32();
            Header.RecordCount = reader.ReadUInt32();
            Header.FieldCount = reader.ReadUInt32();
            Header.RecordSize = reader.ReadUInt32();
            Header.StringTableSize = reader.ReadUInt32(); // also offset for sparse table

            Header.TableHash = reader.ReadUInt32();
            Header.LayoutHash = reader.ReadUInt32();
            Header.MinId = reader.ReadInt32();
            Header.MaxId = reader.ReadInt32();
            Header.Locale = reader.ReadInt32();
            Header.Flags = (HeaderFlags)reader.ReadUInt16();
            Header.IdIndex = reader.ReadUInt16();
            Header.TotalFieldCount = reader.ReadUInt32();

            Header.BitpackedDataOffset = reader.ReadUInt32();
            Header.LookupColumnCount = reader.ReadUInt32();
            Header.ColumnMetaSize = reader.ReadUInt32();
            Header.CommonDataSize = reader.ReadUInt32();
            Header.PalletDataSize = reader.ReadUInt32();
            Header.SectionsCount = reader.ReadUInt32();
        }

        static Dictionary<int, byte[]> ReadData(BinaryReader reader)
        {
            Dictionary<int, byte[]> records = new Dictionary<int, byte[]>();

            var sections = reader.ReadArray<SectionHeader>(Header.SectionsCount);

            //Gather field structures
            FieldStructure = reader.ReadArray<FieldMetaData>(Header.FieldCount);

            // column meta data
            ColumnMeta = new List<ColumnStructureEntry>();
            for (int i = 0; i < Header.FieldCount; i++)
            {
                var column = new ColumnStructureEntry()
                {
                    RecordOffset = reader.ReadUInt16(),
                    Size = reader.ReadUInt16(),
                    AdditionalDataSize = reader.ReadUInt32(), // size of pallet / sparse values
                    CompressionType = (DB2ColumnCompression)reader.ReadUInt32(),
                    BitOffset = reader.ReadInt32(),
                    BitWidth = reader.ReadInt32(),
                    Cardinality = reader.ReadInt32()
                };

                // preload arraysizes
                if (column.CompressionType == DB2ColumnCompression.None)
                    column.ArraySize = Math.Max(column.Size / FieldStructure[i].BitCount, 1);
                else if (column.CompressionType == DB2ColumnCompression.PalletArray)
                    column.ArraySize = Math.Max(column.Cardinality, 1);

                ColumnMeta.Add(column);
            }

            // Pallet values
            for (int i = 0; i < ColumnMeta.Count; i++)
            {
                if (ColumnMeta[i].CompressionType == DB2ColumnCompression.Pallet || ColumnMeta[i].CompressionType == DB2ColumnCompression.PalletArray)
                {
                    int elements = (int)ColumnMeta[i].AdditionalDataSize / 4;
                    int cardinality = Math.Max(ColumnMeta[i].Cardinality, 1);

                    ColumnMeta[i].PalletValues = new List<byte[]>();
                    for (int j = 0; j < elements / cardinality; j++)
                        ColumnMeta[i].PalletValues.Add(reader.ReadBytes(cardinality * 4));
                }
            }

            // Sparse values
            for (int i = 0; i < ColumnMeta.Count; i++)
            {
                if (ColumnMeta[i].CompressionType == DB2ColumnCompression.CommonData)
                {
                    ColumnMeta[i].SparseValues = new Dictionary<int, byte[]>();
                    for (int j = 0; j < ColumnMeta[i].AdditionalDataSize / 8; j++)
                        ColumnMeta[i].SparseValues[reader.ReadInt32()] = reader.ReadBytes(4);
                }
            }

            List<Tuple<int, int>> offsetmap = new List<Tuple<int, int>>();

            bool hasIndexTable = Header.HasIndexTable();

            byte[] recordData;
            for (int sectionIndex = 0; sectionIndex < Header.SectionsCount; sectionIndex++)
            {
                reader.BaseStream.Position = sections[sectionIndex].FileOffset;

                if (Header.HasOffsetTable())
                {   
                    // sparse data with inlined strings
                    recordData = reader.ReadBytes(sections[sectionIndex].SparseTableOffset - sections[sectionIndex].FileOffset);

                    // OffsetTable
                    for (int i = 0; i < (Header.MaxId - Header.MinId + 1); i++)
                    {
                        int offset = reader.ReadInt32();
                        int length = reader.ReadInt32();

                        offsetmap.Add(new Tuple<int, int>(offset, length));
                    }                    
                }
                else
                {
                    // records data
                    recordData = reader.ReadBytes((int)(sections[sectionIndex].NumRecords * Header.RecordSize));

                    Array.Resize(ref recordData, recordData.Length + 8); // pad with extra zeros so we don't crash when reading

                    // string data
                    StringTable = new Dictionary<int, string>();

                    for (int i = 0; i < sections[sectionIndex].StringTableSize;)
                    {
                        int oldPos = (int)reader.BaseStream.Position;

                        StringTable[i] = reader.ReadCString();

                        i += (int)(reader.BaseStream.Position - oldPos);
                    }
                }

                // IndexTable
                int[] m_indexes = reader.ReadArray<int>((uint)(sections[sectionIndex].IndexDataSize / 4));

                // duplicate rows data
                Dictionary<int, int> copyData = new Dictionary<int, int>();

                for (int i = 0; i < sections[sectionIndex].CopyTableSize / 8; i++)
                    copyData[reader.ReadInt32()] = reader.ReadInt32();

                // Relationships
                RelationShipData relationShipData = null;
                if (sections[sectionIndex].ParentLookupDataSize > 0)
                {
                    relationShipData = new RelationShipData()
                    {
                        Records = reader.ReadUInt32(),
                        MinId = reader.ReadUInt32(),
                        MaxId = reader.ReadUInt32(),
                        Entries = new Dictionary<uint, byte[]>()
                    };

                    for (int i = 0; i < relationShipData.Records; i++)
                    {
                        byte[] foreignKey = reader.ReadBytes(4);
                        uint index = reader.ReadUInt32();
                        // has duplicates just like the copy table does... why?
                        if (!relationShipData.Entries.ContainsKey(index))
                            relationShipData.Entries.Add(index, foreignKey);
                    }
                }

                // Record Data
                if (Header.HasOffsetTable() && hasIndexTable)
                {
                    for (int i = 0; i < Header.RecordCount; ++i)
                    {
                        int id = m_indexes[records.Count];
                        var map = offsetmap[i];

                        reader.BaseStream.Position = map.Item1;

                        byte[] data = reader.ReadBytes(map.Item2);

                        IEnumerable<byte> recordbytes = data;

                        // append relationship id
                        if (relationShipData != null)
                        {
                            // seen cases of missing indicies 
                            if (relationShipData.Entries.TryGetValue((uint)i, out byte[] foreignData))
                                recordbytes = recordbytes.Concat(foreignData);
                            else
                                recordbytes = recordbytes.Concat(new byte[4]);
                        }

                        records.Add(id, recordbytes.ToArray());
                    }
                }
                else
                {
                    BitReader bitReader = new BitReader(recordData);
                    for (int i = 0; i < Header.RecordCount; ++i)
                    {
                        bitReader.Position = 0;
                        bitReader.Offset = i * (int)Header.RecordSize;

                        MemoryStream stream = new MemoryStream();
                        BinaryWriter binaryWriter = new BinaryWriter(stream);

                        int id = 0;
                        if (sections[sectionIndex].IndexDataSize != 0)
                            id = m_indexes[i];

                        for (int f = 0; f < Header.FieldCount; f++)
                        {
                            int bitWidth = ColumnMeta[f].BitWidth;

                            switch (ColumnMeta[f].CompressionType)
                            {
                                case DB2ColumnCompression.None:
                                    int bitSize = FieldStructure[f].BitCount;
                                    if (!hasIndexTable && f == Header.IdIndex)
                                    {
                                        id = (int)bitReader.ReadUInt32(bitSize);// always read Ids as ints
                                        binaryWriter.Write(id);
                                    }
                                    else
                                    {
                                        for (int x = 0; x < ColumnMeta[f].ArraySize; x++)
                                            binaryWriter.Write(bitReader.ReadValue(bitSize));
                                    }
                                    break;

                                case DB2ColumnCompression.Immediate:
                                case DB2ColumnCompression.SignedImmediate:
                                    if (!hasIndexTable && f == Header.IdIndex)
                                    {
                                        id = (int)bitReader.ReadUInt32(bitWidth);// always read Ids as ints
                                        binaryWriter.Write(id);
                                        continue;
                                    }
                                    else
                                        binaryWriter.Write(bitReader.ReadValue(bitWidth, ColumnMeta[f].BitOffset, ColumnMeta[f].CompressionType == DB2ColumnCompression.SignedImmediate));
                                    break;

                                case DB2ColumnCompression.CommonData:
                                    if (ColumnMeta[f].SparseValues.TryGetValue(id, out byte[] valBytes))
                                        binaryWriter.Write(valBytes);
                                    else
                                        binaryWriter.Write(ColumnMeta[f].BitOffset);
                                    break;

                                case DB2ColumnCompression.Pallet:
                                case DB2ColumnCompression.PalletArray:
                                    uint palletIndex = bitReader.ReadUInt32(bitWidth);
                                    binaryWriter.Write(ColumnMeta[f].PalletValues[(int)palletIndex]);
                                    break;

                                default:
                                    throw new Exception($"Unknown compression {ColumnMeta[f].CompressionType}");
                            }
                        }

                        // append relationship id
                        if (relationShipData != null)
                        {
                            // seen cases of missing indicies 
                            if (relationShipData.Entries.TryGetValue((uint)i, out byte[] foreignData))
                                binaryWriter.Write(foreignData);
                            else
                                binaryWriter.Write(0);
                        }

                        records.Add(id, stream.ToArray());
                    }
                }

                foreach (var copyRow in copyData)
                {
                    byte[] newrecord = records[copyRow.Value].ToArray();
                    records.Add(copyRow.Key, newrecord);
                }
            }

            return records;
        }

        static Dictionary<Type, int> bytecounts = new Dictionary<Type, int>()
        {
            { typeof(byte), 1 },
            { typeof(sbyte), 1 },
            { typeof(short), 2 },
            { typeof(ushort), 2 },
        };

        static int GetPadding(Type type, int fieldIndex)
        {
            if (ColumnMeta[fieldIndex].CompressionType < DB2ColumnCompression.CommonData)
                return 0;

            if (!bytecounts.ContainsKey(type))
                return 0;

            return 4 - bytecounts[type];
        }

        static FieldMetaData[] GetBits()
        {
            List<FieldMetaData> bits = new List<FieldMetaData>();
            for (int i = 0; i < ColumnMeta.Count; i++)
            {
                short bitcount = (short)(FieldStructure[i].BitCount == 64 ? FieldStructure[i].BitCount : 0); // force bitcounts
                bits.Add(new FieldMetaData(bitcount, 0));
            }

            return bits.ToArray();
        }

        static void SetArrayValue(object obj, uint arraySize, DB6FieldInfo fieldInfo, BinaryReader reader)
        {
            switch (Type.GetTypeCode(fieldInfo.FieldType))
            {
                case TypeCode.SByte:
                    fieldInfo.SetValue(obj, reader.ReadArray<sbyte>(arraySize));
                    break;
                case TypeCode.Byte:
                    fieldInfo.SetValue(obj, reader.ReadArray<byte>(arraySize));
                    break;
                case TypeCode.Int16:
                    fieldInfo.SetValue(obj, reader.ReadArray<short>(arraySize));
                    break;
                case TypeCode.UInt16:
                    fieldInfo.SetValue(obj, reader.ReadArray<ushort>(arraySize));
                    break;
                case TypeCode.Int32:
                    fieldInfo.SetValue(obj, reader.ReadArray<int>(arraySize));
                    break;
                case TypeCode.UInt32:
                    fieldInfo.SetValue(obj, reader.ReadArray<uint>(arraySize));
                    break;
                case TypeCode.Int64:
                    fieldInfo.SetValue(obj, reader.ReadArray<long>(arraySize));
                    break;
                case TypeCode.UInt64:
                    fieldInfo.SetValue(obj, reader.ReadArray<ulong>(arraySize));
                    break;
                case TypeCode.Single:
                    fieldInfo.SetValue(obj, reader.ReadArray<float>(arraySize));
                    break;
                case TypeCode.String:
                    string[] str = new string[arraySize];
                    for (var i = 0; i < arraySize; ++i)
                        str[i] = GetString(reader);
                    fieldInfo.SetValue(obj, str);
                    break;
                default:
                    Log.outError(LogFilter.ServerLoading, "Wrong Array Type: {0}", fieldInfo.FieldType.Name);
                    break;
            }
        }

        static void SetValue(object obj, DB6FieldInfo fieldInfo, BinaryReader reader)
        {
            switch (Type.GetTypeCode(fieldInfo.FieldType))
            {
                case TypeCode.SByte:
                    fieldInfo.SetValue(obj, reader.ReadSByte());
                    break;
                case TypeCode.Byte:
                    fieldInfo.SetValue(obj, reader.ReadByte());
                    break;
                case TypeCode.Int16:
                    fieldInfo.SetValue(obj, reader.ReadInt16());
                    break;
                case TypeCode.UInt16:
                    fieldInfo.SetValue(obj, reader.ReadUInt16());
                    break;
                case TypeCode.Int32:
                    fieldInfo.SetValue(obj, reader.ReadInt32());
                    break;
                case TypeCode.UInt32:
                    fieldInfo.SetValue(obj, reader.ReadUInt32());
                    break;
                case TypeCode.Int64:
                    fieldInfo.SetValue(obj, reader.ReadInt64());
                    break;
                case TypeCode.UInt64:
                    fieldInfo.SetValue(obj, reader.ReadUInt64());
                    break;
                case TypeCode.Single:
                    fieldInfo.SetValue(obj, reader.ReadSingle());
                    break;
                case TypeCode.String:
                    string str = GetString(reader);
                    fieldInfo.SetValue(obj, str);
                    break;
                case TypeCode.Object:
                    switch (fieldInfo.FieldType.Name)
                    {
                        case "Vector2":
                            fieldInfo.SetValue(obj, reader.Read<Vector2>());
                            break;
                        case "Vector3":
                            fieldInfo.SetValue(obj, reader.Read<Vector3>());
                            break;
                        case "LocalizedString":
                            LocalizedString locString = new LocalizedString();
                            locString[Global.WorldMgr.GetDefaultDbcLocale()] = GetString(reader);
                            fieldInfo.SetValue(obj, locString);
                            break;
                        default:
                            Log.outError(LogFilter.ServerLoading, "Wrong Array Type: {0}", fieldInfo.FieldType.Name);
                            break;
                    }
                    break;
                default:
                    Log.outError(LogFilter.ServerLoading, "Wrong Array Type: {0}", fieldInfo.FieldType.Name);
                    break;
            }
        }

        static string GetString(BinaryReader reader)
        {
            if (StringTable != null)
                return StringTable.LookupByKey(reader.ReadUInt32());

            return reader.ReadCString();
        }

        static DB6Header Header;
        static Dictionary<int, string> StringTable;

        static FieldMetaData[] FieldStructure;
        static List<ColumnStructureEntry> ColumnMeta;
    }

    public struct DB6FieldInfo
    {
        public DB6FieldInfo(FieldInfo fieldInfo)
        {
            IsArray = false;
            FieldType = fieldInfo.FieldType;

            if (fieldInfo.FieldType.IsArray)
            {
                FieldType = fieldInfo.FieldType.GetElementType();
                IsArray = true;
            }

            Setter = fieldInfo.CompileSetter();
            Getter = fieldInfo.CompileGetter();
        }

        public void SetValue(Array array, object value, int arrayIndex)
        {
            array.SetValue(value, arrayIndex % array.Length);
        }

        public void SetValue(object obj, object value)
        {
            Setter(obj, value);
        }

        public Type FieldType;
        public bool IsArray;
        Action<object, object> Setter;
        public Func<object, object> Getter;
    }

    public class DB6Header
    {
        public bool HasIndexTable()
        {
            return Convert.ToBoolean(Flags & HeaderFlags.IndexMap);
        }

        public bool HasOffsetTable()
        {
            return Convert.ToBoolean(Flags & HeaderFlags.OffsetMap);
        }

        public uint Signature;
        public uint RecordCount;
        public uint FieldCount;
        public uint RecordSize;
        public uint StringTableSize;

        public uint TableHash;
        public uint LayoutHash;
        public int MinId;
        public int MaxId;
        public int Locale;
        public HeaderFlags Flags;
        public int IdIndex;
        public uint TotalFieldCount;
        public uint BitpackedDataOffset;
        public uint LookupColumnCount;
        public uint ColumnMetaSize;
        public uint CommonDataSize;
        public uint PalletDataSize;
        public uint SectionsCount;
    }

    public struct FieldMetaData
    {
        public short Bits;
        public ushort Offset;

        public int ByteCount
        {
            get
            {
                int value = (32 - Bits) >> 3;
                return (value < 0 ? Math.Abs(value) + 4 : value);
            }
        }

        public int BitCount
        {
            get
            {
                int bitSize = 32 - Bits;
                if (bitSize < 0)
                    bitSize = (bitSize * -1) + 32;
                return bitSize;
            }
        }

        public FieldMetaData(short bits, ushort offset)
        {
            Bits = bits;
            Offset = offset;
        }
    }

    public class ColumnStructureEntry
    {
        public ushort RecordOffset { get; set; }
        public ushort Size { get; set; }
        public uint AdditionalDataSize { get; set; }
        public DB2ColumnCompression CompressionType { get; set; }
        public int BitOffset { get; set; }  // used as common data column for Sparse
        public int BitWidth { get; set; }
        public int Cardinality { get; set; } // flags for Immediate, &1: Signed

        public List<byte[]> PalletValues { get; set; }
        public Dictionary<int, byte[]> SparseValues { get; set; }
        public int ArraySize { get; set; } = 1;
    }

    public struct SectionHeader
    {
        public int unk1;
        public int unk2;
        public int FileOffset;
        public int NumRecords;
        public int StringTableSize;
        public int CopyTableSize;
        public int SparseTableOffset; // CatalogDataOffset, absolute value, {uint offset, ushort size}[MaxId - MinId + 1]
        public int IndexDataSize; // int indexData[IndexDataSize / 4]
        public int ParentLookupDataSize; // uint NumRecords, uint minId, uint maxId, {uint id, uint index}[NumRecords], questionable usefulness...
    }

    public class RelationShipData
    {
        public uint Records;
        public uint MinId;
        public uint MaxId;
        public Dictionary<uint, byte[]> Entries; // index, id
    }

    struct OffsetDuplicate
    {
        public int HiddenIndex { get; }
        public int VisibleIndex { get; }

        public OffsetDuplicate(int hidden, int visible)
        {
            HiddenIndex = hidden;
            VisibleIndex = visible;
        }
    }

    public class LocalizedString
    {
        public bool HasString(LocaleConstant locale = SharedConst.DefaultLocale)
        {
            return !string.IsNullOrEmpty(stringStorage[(int)locale]);
        }

        public string this[LocaleConstant locale]
        {
            get
            {
                return stringStorage[(int)locale] ?? "";
            }
            set
            {
                stringStorage[(int)locale] = value;
            }
        }

        StringArray stringStorage = new StringArray((int)LocaleConstant.Total);
    }

    public enum DB2ColumnCompression : uint
    {
        None,
        Immediate,
        CommonData,
        Pallet,
        PalletArray,
        SignedImmediate
    }

    [Flags]
    public enum HeaderFlags : short
    {
        None = 0x0,
        OffsetMap = 0x1,
        SecondIndex = 0x2,
        IndexMap = 0x4,
        Unknown = 0x8,
        Compressed = 0x10,
    }
}
