// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Game.Maps
{
    internal struct CompareRespawnInfo : IComparer<RespawnInfo>
    {
        public int Compare(RespawnInfo a, RespawnInfo b)
        {
            if (a == b)
                return 0;

            if (a.RespawnTime != b.RespawnTime)
                return a.RespawnTime.CompareTo(b.RespawnTime);

            if (a.SpawnId != b.SpawnId)
                return a.SpawnId.CompareTo(b.SpawnId);

            Cypher.Assert(a.Type != b.Type, $"Duplicate respawn entry for spawnId ({a.Type},{a.SpawnId}) found!");

            return a.Type.CompareTo(b.Type);
        }
    }
}