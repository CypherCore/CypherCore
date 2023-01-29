// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.DungeonFinding
{
    public class LfgReward
    {
        public uint FirstQuest { get; set; }

        public uint MaxLevel { get; set; }
        public uint OtherQuest { get; set; }

        public LfgReward(uint _maxLevel = 0, uint _firstQuest = 0, uint _otherQuest = 0)
        {
            MaxLevel = _maxLevel;
            FirstQuest = _firstQuest;
            OtherQuest = _otherQuest;
        }
    }
}