// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Game
{
    [Flags]
    public enum ShutdownMask
    {
        Restart = 1,
        Idle = 2,
        Force = 4
    }
}