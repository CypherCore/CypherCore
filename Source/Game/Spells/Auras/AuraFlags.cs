// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Game.Spells
{
    [Flags]
    public enum AuraFlags
    {
        None = 0x00,
        NoCaster = 0x01,
        Positive = 0x02,
        Duration = 0x04,
        Scalable = 0x08,
        Negative = 0x10,
        Unk20 = 0x20,
        Unk40 = 0x40,
        Unk80 = 0x80,
        MawPower = 0x100
    }
}