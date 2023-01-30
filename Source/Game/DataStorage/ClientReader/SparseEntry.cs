// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Game.DataStorage
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SparseEntry
    {
        public int Offset;
        public ushort Size;
    }
}