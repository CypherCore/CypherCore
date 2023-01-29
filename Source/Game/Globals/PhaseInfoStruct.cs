// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public class PhaseInfoStruct
    {
        public PhaseInfoStruct(uint id)
        {
            Id = id;
        }

        public List<uint> Areas { get; set; } = new();

        public uint Id { get; set; }

        public bool IsAllowedInArea(uint areaId)
        {
            return Areas.Any(areaToCheck => Global.DB2Mgr.IsInArea(areaId, areaToCheck));
        }
    }
}