// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;

namespace Game.Maps
{
    internal class SharedInstanceLock : InstanceLock
    {
        /// <summary>
        ///  Instance Id based locks have two states
        ///  One shared by everyone, which is the real State used by instance
        ///  and one for each player that shows in UI that might have less encounters completed
        /// </summary>
        private readonly SharedInstanceLockData _sharedData;

        public SharedInstanceLock(uint mapId, Difficulty difficultyId, DateTime expiryTime, uint instanceId, SharedInstanceLockData sharedData) : base(mapId, difficultyId, expiryTime, instanceId)
        {
            _sharedData = sharedData;
        }

        public override InstanceLockData GetInstanceInitializationData()
        {
            return _sharedData;
        }

        public SharedInstanceLockData GetSharedData()
        {
            return _sharedData;
        }
    }
}