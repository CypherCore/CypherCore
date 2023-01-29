// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.DungeonFinding
{
    public class LfgJoinResultData
    {
        public Dictionary<ObjectGuid, Dictionary<uint, LfgLockInfoData>> Lockmap { get; set; } = new();
        public List<string> PlayersMissingRequirement { get; set; } = new();

        public LfgJoinResult Result { get; set; }
        public LfgRoleCheckState State { get; set; }

        public LfgJoinResultData(LfgJoinResult _result = LfgJoinResult.Ok, LfgRoleCheckState _state = LfgRoleCheckState.Default)
        {
            Result = _result;
            State = _state;
        }
    }
}