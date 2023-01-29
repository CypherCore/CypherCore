// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Maps
{
    public class RespawnInfo
    {
        public uint Entry { get; set; }
        public uint GridId { get; set; }
        public long RespawnTime { get; set; }
        public ulong SpawnId { get; set; }
        public SpawnObjectType Type { get; set; }

        public RespawnInfo(ulong spawnId)
        {
            this.SpawnId = spawnId;
        }

        public RespawnInfo()
        {
        }

        public RespawnInfo(RespawnInfo info)
        {
            Type = info.Type;
            SpawnId = info.SpawnId;
            Entry = info.Entry;
            RespawnTime = info.RespawnTime;
            GridId = info.GridId;
        }
    }
}