// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Framework.Constants;

namespace Game.DataStorage
{
    [StructLayout(LayoutKind.Explicit)]
    public struct ColumnMetaData
    {
        [FieldOffset(0)] public ushort RecordOffset;
        [FieldOffset(2)] public ushort Size;
        [FieldOffset(4)] public uint AdditionalDataSize;
        [FieldOffset(8)] public DB2ColumnCompression CompressionType;
        [FieldOffset(12)] public ColumnCompressionData_Immediate Immediate;
        [FieldOffset(12)] public ColumnCompressionData_Pallet Pallet;
        [FieldOffset(12)] public ColumnCompressionData_Common Common;
    }
}