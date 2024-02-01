// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Framework.Constants
{
    [Flags]
    public enum RealmFlags
    {
        None = 0x00,
        VersionMismatch = 0x01,
        Offline = 0x02,
        SpecifyBuild = 0x04,
        Unk1 = 0x08,
        Unk2 = 0x10,
        Recommended = 0x20,
        New = 0x40,
        Full = 0x80
    }

    public enum RealmType
    {
        Normal = 0,
        PVP = 1,
        Normal2 = 4,
        RP = 6,
        RPPVP = 8,

        MaxType = 14,

        FFAPVP = 16                            // custom, free for all pvp mode like arena PvP in all zones except rest activated places and sanctuaries
        // replaced by REALM_PVP in realm list
    }
}
