// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.DungeonFinding
{
    public class LfgLockInfoData
    {
        public float CurrentItemLevel { get; set; }

        public LfgLockStatusType LockStatus { get; set; }
        public ushort RequiredItemLevel { get; set; }

        public LfgLockInfoData(LfgLockStatusType _lockStatus = 0, ushort _requiredItemLevel = 0, float _currentItemLevel = 0)
        {
            LockStatus = _lockStatus;
            RequiredItemLevel = _requiredItemLevel;
            CurrentItemLevel = _currentItemLevel;
        }
    }
}