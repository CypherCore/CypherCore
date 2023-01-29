// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game
{
    public class ReputationOnKillEntry
    {
        public bool IsTeamAward1 { get; set; }
        public bool IsTeamAward2 { get; set; }
        public uint RepFaction1 { get; set; }
        public uint RepFaction2 { get; set; }
        public uint ReputationMaxCap1 { get; set; }
        public uint ReputationMaxCap2 { get; set; }
        public int RepValue1 { get; set; }
        public int RepValue2 { get; set; }
        public bool TeamDependent { get; set; }
    }
}