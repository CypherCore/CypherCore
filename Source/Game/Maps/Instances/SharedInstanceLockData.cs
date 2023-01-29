// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Maps
{
    internal class SharedInstanceLockData : InstanceLockData
    {
        public uint InstanceId;

        ~SharedInstanceLockData()
        {
            // Cleanup database
            if (InstanceId != 0)
                Global.InstanceLockMgr.OnSharedInstanceLockDataDelete(InstanceId);
        }
    }
}