// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Framework.Constants;
using Framework.Database;

namespace Game.DataStorage
{
    internal class DBReader
    {
        private const uint WDC3FmtSig = 0x33434457; // WDC3
        internal ColumnMetaData[] ColumnMeta;
        internal Dictionary<int, Value32>[] CommonData;
        internal FieldMetaData[] FieldMeta;

        internal WDCHeader Header;
        internal Value32[][] PalletData;

        private readonly Dictionary<int, WDC3Row> _records = new();

        public static DB6Storage<T> Read<T>(BitSet availableDb2Locales, string db2Path, string fileName, HotfixStatements preparedStatement, HotfixStatements preparedStatementLocale, ref uint loadedFileCount) where T : new()
        {
            DB6Storage<T> storage = new();

            if (!File.Exists(db2Path + fileName))
            {
                Log.outError(LogFilter.ServerLoading, $"File {fileName} not found.");

                return storage;
            }

            DBReader reader = new();

            using (var stream = new FileStream(db2Path + fileName, FileMode.Open))
            {
                if (!reader.Load(stream))
                {
                    Log.outError(LogFilter.ServerLoading, $"Error loading {fileName}.");

                    return storage;
                }
            }

            foreach (var b in reader._records)
                storage.Add((uint)b.Key, b.Value.As<T>());

            storage.LoadData(reader.Header, availableDb2Locales, preparedStatement, preparedStatementLocale);

            Global.DB2Mgr.AddDB2(reader.Header.TableHash, storage);
            loadedFileCount++;

            return storage;
        }

        private bool Load(Stream stream)
        {
            using (var reader = new BinaryReader(stream, Encoding.UTF8))
            {
                Header = new WDCHeader();
                Header.Signature = reader.ReadUInt32();

                if (Header.Signature != WDC3FmtSig)
                    return false;

                Header.RecordCount = reader.ReadUInt32();
                Header.FieldCount = reader.ReadUInt32();
                Header.RecordSize = reader.ReadUInt32();
                Header.StringTableSize = reader.ReadUInt32();
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

                var sections = reader.ReadArray<SectionHeader>(Header.SectionsCount);

                // field meta _data
                FieldMeta = reader.ReadArray<FieldMetaData>(Header.FieldCount);

                // column meta _data 
                ColumnMeta = reader.ReadArray<ColumnMetaData>(Header.FieldCount);

                // pallet _data
                PalletData = new Value32[ColumnMeta.Length][];

                for (int i = 0; i < ColumnMeta.Length; i++)
                    if (ColumnMeta[i].CompressionType == DB2ColumnCompression.Pallet ||
                        ColumnMeta[i].CompressionType == DB2ColumnCompression.PalletArray)
                        PalletData[i] = reader.ReadArray<Value32>(ColumnMeta[i].AdditionalDataSize / 4);

                // common _data
                CommonData = new Dictionary<int, Value32>[ColumnMeta.Length];

                for (int i = 0; i < ColumnMeta.Length; i++)
                    if (ColumnMeta[i].CompressionType == DB2ColumnCompression.Common)
                    {
                        Dictionary<int, Value32> commonValues = new();
                        CommonData[i] = commonValues;

                        for (int j = 0; j < ColumnMeta[i].AdditionalDataSize / 8; j++)
                            commonValues[reader.ReadInt32()] = reader.Read<Value32>();
                    }

                long previousStringTableSize = 0;
                long previousRecordCount = 0;

                for (int sectionIndex = 0; sectionIndex < Header.SectionsCount; sectionIndex++)
                {
                    if (sections[sectionIndex].TactKeyLookup != 0) // && !hasTactKeyFunc(sections[sectionIndex].TactKeyLookup))
                    {
                        previousStringTableSize += sections[sectionIndex].StringTableSize;
                        previousRecordCount += sections[sectionIndex].NumRecords;

                        //Console.WriteLine("Detected db2 with encrypted section! HasKey {0}", CASC.HasKey(Sections[sectionIndex].TactKeyLookup));
                        continue;
                    }

                    reader.BaseStream.Position = sections[sectionIndex].FileOffset;

                    byte[] recordsData;
                    Dictionary<long, string> stringsTable = null;
                    SparseEntry[] sparseEntries = null;

                    if (!Header.HasOffsetTable())
                    {
                        // records _data
                        recordsData = reader.ReadBytes((int)(sections[sectionIndex].NumRecords * Header.RecordSize));

                        // string _data
                        stringsTable = new Dictionary<long, string>();

                        long stringDataOffset = 0;

                        if (sectionIndex == 0)
                            stringDataOffset = (Header.RecordCount - sections[sectionIndex].NumRecords) * Header.RecordSize;
                        else
                            stringDataOffset = previousStringTableSize;

                        for (int i = 0; i < sections[sectionIndex].StringTableSize;)
                        {
                            long oldPos = reader.BaseStream.Position;

                            stringsTable[i + stringDataOffset] = reader.ReadCString();

                            i += (int)(reader.BaseStream.Position - oldPos);
                        }
                    }
                    else
                    {
                        // sparse _data with inlined strings
                        recordsData = reader.ReadBytes(sections[sectionIndex].SparseTableOffset - sections[sectionIndex].FileOffset);

                        if (reader.BaseStream.Position != sections[sectionIndex].SparseTableOffset)
                            throw new Exception("reader.BaseStream.Position != sections[sectionIndex].SparseTableOffset");
                    }

                    Array.Resize(ref recordsData, recordsData.Length + 8); // pad with extra zeros so we don't crash when reading

                    // index _data
                    int[] indexData = reader.ReadArray<int>((uint)(sections[sectionIndex].IndexDataSize / 4));
                    bool isIndexEmpty = Header.HasIndexTable() && indexData.Count(i => i == 0) == sections[sectionIndex].NumRecords;

                    // duplicate rows _data
                    Dictionary<int, int> copyData = new();

                    for (int i = 0; i < sections[sectionIndex].NumCopyRecords; i++)
                        copyData[reader.ReadInt32()] = reader.ReadInt32();

                    if (sections[sectionIndex].NumSparseRecords > 0)
                        sparseEntries = reader.ReadArray<SparseEntry>((uint)sections[sectionIndex].NumSparseRecords);

                    // reference _data
                    ReferenceData refData = null;

                    if (sections[sectionIndex].ParentLookupDataSize > 0)
                    {
                        refData = new ReferenceData
                        {
                            NumRecords = reader.ReadInt32(),
                            MinId = reader.ReadInt32(),
                            MaxId = reader.ReadInt32()
                        };

                        refData.Entries = new Dictionary<int, int>();
                        ReferenceEntry[] entries = reader.ReadArray<ReferenceEntry>((uint)refData.NumRecords);

                        foreach (var entry in entries)
                            refData.Entries[entry.Index] = entry.Id;
                    }
                    else
                    {
                        refData = new ReferenceData
                        {
                            Entries = new Dictionary<int, int>()
                        };
                    }

                    if (sections[sectionIndex].NumSparseRecords > 0)
                    {
                        // TODO: use this shit
                        int[] sparseIndexData = reader.ReadArray<int>((uint)sections[sectionIndex].NumSparseRecords);

                        if (Header.HasIndexTable() &&
                            indexData.Length != sparseIndexData.Length)
                            throw new Exception("indexData.Length != sparseIndexData.Length");

                        indexData = sparseIndexData;
                    }

                    BitReader bitReader = new(recordsData);

                    for (int i = 0; i < sections[sectionIndex].NumRecords; ++i)
                    {
                        bitReader.Position = 0;

                        if (Header.HasOffsetTable())
                            bitReader.Offset = sparseEntries[i].Offset - sections[sectionIndex].FileOffset;
                        else
                            bitReader.Offset = i * (int)Header.RecordSize;

                        bool hasRef = refData.Entries.TryGetValue(i, out int refId);

                        long recordIndex = i + previousRecordCount;
                        long recordOffset = (recordIndex * Header.RecordSize) - (Header.RecordCount * Header.RecordSize);

                        var rec = new WDC3Row(this, bitReader, (int)recordOffset, Header.HasIndexTable() ? (isIndexEmpty ? i : indexData[i]) : -1, hasRef ? refId : -1, stringsTable);
                        _records.Add(rec.Id, rec);
                    }

                    foreach (var copyRow in copyData)
                        if (copyRow.Key != 0)
                        {
                            var rec = _records[copyRow.Value].Clone();
                            rec.Id = copyRow.Key;
                            _records.Add(copyRow.Key, rec);
                        }

                    previousStringTableSize += sections[sectionIndex].StringTableSize;
                    previousRecordCount += sections[sectionIndex].NumRecords;
                }
            }

            return true;
        }
    }
}