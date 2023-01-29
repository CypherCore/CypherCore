// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Maps
{
    public class DoorData
    {
        public uint BossId { get; set; }
        public uint Entry { get; set; }
        public DoorType Type { get; set; }

        public DoorData(uint _entry, uint _bossid, DoorType _type)
        {
            Entry = _entry;
            BossId = _bossid;
            Type = _type;
        }
    }
}