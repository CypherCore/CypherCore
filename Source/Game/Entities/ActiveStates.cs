// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Entities
{
    public enum ActiveStates
    {
        Passive = 0x01,  // 0x01 - passive
        Disabled = 0x81, // 0x80 - castable
        Enabled = 0xC1,  // 0x40 | 0x80 - auto cast + castable
        Command = 0x07,  // 0x01 | 0x02 | 0x04
        Reaction = 0x06, // 0x02 | 0x04
        Decide = 0x00    // custom
    }
}