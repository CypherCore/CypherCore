// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Framework.Realm
{
    public struct RealmId : IEquatable<RealmId>
    {   
        public uint Index { get; set; }
        public byte Region { get; set; }
        public byte Site { get; set; }

        public RealmId(byte region, byte battlegroup, uint index)
        {
            Region = region;
            Site = battlegroup;
            Index = index;
        }

        public RealmId(uint realmAddress)
        {
            Region = (byte)((realmAddress >> 24) & 0xFF);
            Site = (byte)((realmAddress >> 16) & 0xFF);
            Index = realmAddress & 0xFFFF;
        }

        public uint GetAddress()
        {
            return (uint)((Region << 24) | (Site << 16) | (ushort)Index);
        }

        public string GetAddressString()
        {
            return $"{Region}-{Site}-{Index}";
        }

        public string GetSubRegionAddress()
        {
            return $"{Region}-{Site}-0";
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is RealmId && Equals((RealmId)obj);
        }

        public bool Equals(RealmId other)
        {
            return other.Index == Index;
        }

        public override int GetHashCode()
        {
            return new { Site, Region, Index }.GetHashCode();
        }
    }
}
