// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Maps
{
    public class MinionData
    {
        public uint BossId { get; set; }
        public uint Entry { get; set; }

        public MinionData(uint _entry, uint _bossid)
        {
            Entry = _entry;
            BossId = _bossid;
        }
    }
}