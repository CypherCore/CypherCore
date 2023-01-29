// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.DungeonFinding
{
    public class LfgRoleCheck
    {
        public long CancelTime { get; set; }
        public List<uint> Dungeons { get; set; } = new();
        public ObjectGuid Leader;
        public uint DungeonId { get; set; }
        public Dictionary<ObjectGuid, LfgRoles> Roles { get; set; } = new();
        public LfgRoleCheckState State { get; set; }
    }
}