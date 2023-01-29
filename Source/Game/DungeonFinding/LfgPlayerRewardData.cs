// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.DungeonFinding
{
    public class LfgPlayerRewardData
    {
        public bool Done { get; set; }
        public Quest Quest { get; set; }

        public uint RdungeonEntry { get; set; }
        public uint SdungeonEntry { get; set; }

        public LfgPlayerRewardData(uint random, uint current, bool _done, Quest _quest)
        {
            RdungeonEntry = random;
            SdungeonEntry = current;
            Done = _done;
            Quest = _quest;
        }
    }
}