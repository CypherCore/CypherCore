// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.DungeonFinding
{
    public class LfgQueueStatusData
    {
        public LfgQueueStatusData(byte _queueId = 0, uint _dungeonId = 0, int _waitTime = -1, int _waitTimeAvg = -1, int _waitTimeTank = -1, int _waitTimeHealer = -1,
                                  int _waitTimeDps = -1, uint _queuedTime = 0, byte _tanks = 0, byte _healers = 0, byte _dps = 0)
        {
            QueueId = _queueId;
            DungeonId = _dungeonId;
            WaitTime = _waitTime;
            WaitTimeAvg = _waitTimeAvg;
            WaitTimeTank = _waitTimeTank;
            WaitTimeHealer = _waitTimeHealer;
            WaitTimeDps = _waitTimeDps;
            QueuedTime = _queuedTime;
            Tanks = _tanks;
            Healers = _healers;
            Dps = _dps;
        }

        public byte Dps { get; set; }
        public uint DungeonId { get; set; }
        public byte Healers { get; set; }
        public uint QueuedTime { get; set; }

        public byte QueueId { get; set; }
        public byte Tanks { get; set; }
        public int WaitTime { get; set; }
        public int WaitTimeAvg { get; set; }
        public int WaitTimeDps { get; set; }
        public int WaitTimeHealer { get; set; }
        public int WaitTimeTank { get; set; }
    }
}