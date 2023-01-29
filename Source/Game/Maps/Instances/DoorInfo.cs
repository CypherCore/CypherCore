// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Maps
{
    internal class DoorInfo
    {
        public BossInfo BossInfo;
        public DoorType Type;

        public DoorInfo(BossInfo _bossInfo, DoorType _type)
        {
            BossInfo = _bossInfo;
            Type = _type;
        }
    }
}