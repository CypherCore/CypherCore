// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Game.Entities
{
    [StructLayout(LayoutKind.Sequential)]
    public class ItemDynamicFieldGems
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public ushort[] BonusListIDs = new ushort[16];

        public byte Context;
        public uint ItemId;
    }
}