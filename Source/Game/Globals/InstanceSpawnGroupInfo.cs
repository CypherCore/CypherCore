// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game
{
    public struct InstanceSpawnGroupInfo
    {
        public byte BossStateId { get; set; }
        public byte BossStates { get; set; }
        public uint SpawnGroupId { get; set; }
        public InstanceSpawnGroupFlags Flags { get; set; }
    }
}