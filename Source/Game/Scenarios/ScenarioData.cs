// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.DataStorage;

namespace Game.Scenarios
{
    public class ScenarioData
    {
        public ScenarioRecord Entry { get; set; }
        public Dictionary<byte, ScenarioStepRecord> Steps { get; set; } = new();
    }
}