// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Game
{
    [Flags]
    public enum PhaseFlags : ushort
    {
        None = 0x0,
        Cosmetic = 0x1,
        Personal = 0x2
    }
}