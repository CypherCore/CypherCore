// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Framework.Constants
{
    public enum UpdateType
    {
        Values            = 0,
        CreateObject      = 1,
        CreateObject2     = 2,
        OutOfRangeObjects    = 3,
    }

    [Flags]
    public enum UpdateFieldFlag
    {
        None = 0,
        Owner = 0x01,
        PartyMember = 0x02,
        UnitAll = 0x04,
        Empath = 0x08
    }
}
