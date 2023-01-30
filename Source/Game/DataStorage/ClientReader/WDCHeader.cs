// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;

namespace Game.DataStorage
{
    public class WDCHeader
    {
        public uint BitpackedDataOffset { get; set; }
        public uint ColumnMetaSize { get; set; }
        public uint CommonDataSize { get; set; }
        public uint FieldCount { get; set; }
        public HeaderFlags Flags { get; set; }
        public int IdIndex { get; set; }
        public uint LayoutHash { get; set; }
        public int Locale { get; set; }
        public uint LookupColumnCount { get; set; }
        public int MaxId { get; set; }
        public int MinId { get; set; }
        public uint PalletDataSize { get; set; }
        public uint RecordCount { get; set; }
        public uint RecordSize { get; set; }
        public uint SectionsCount { get; set; }

        public uint Signature { get; set; }
        public uint StringTableSize { get; set; }

        public uint TableHash { get; set; }
        public uint TotalFieldCount { get; set; }

        public bool HasIndexTable()
        {
            return Convert.ToBoolean(Flags & HeaderFlags.IndexMap);
        }

        public bool HasOffsetTable()
        {
            return Convert.ToBoolean(Flags & HeaderFlags.OffsetMap);
        }
    }
}