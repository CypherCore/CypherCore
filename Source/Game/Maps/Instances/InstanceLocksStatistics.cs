// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Maps
{
    public struct InstanceLocksStatistics
    {
        public int InstanceCount; // Number of existing ID-based locks
        public int PlayerCount;   // Number of players that have any lock
    }
}