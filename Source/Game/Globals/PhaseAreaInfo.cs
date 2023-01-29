// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Conditions;

namespace Game
{
    public class PhaseAreaInfo
    {
        public List<Condition> Conditions { get; set; } = new();

        public PhaseInfoStruct PhaseInfo { get; set; }
        public List<uint> SubAreaExclusions { get; set; } = new();

        public PhaseAreaInfo(PhaseInfoStruct phaseInfo)
        {
            PhaseInfo = phaseInfo;
        }
    }
}