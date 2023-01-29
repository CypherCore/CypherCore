// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;

namespace Game.Maps
{
    public class InstanceLock
    {
        private readonly InstanceLockData _data = new();
        private readonly Difficulty _difficultyId;
        private readonly uint _mapId;
        private DateTime _expiryTime;
        private bool _extended;
        private uint _instanceId;
        private bool _isInUse;

        public InstanceLock(uint mapId, Difficulty difficultyId, DateTime expiryTime, uint instanceId)
        {
            _mapId = mapId;
            _difficultyId = difficultyId;
            _instanceId = instanceId;
            _expiryTime = expiryTime;
            _extended = false;
        }

        public bool IsExpired()
        {
            return _expiryTime < GameTime.GetSystemTime();
        }

        public DateTime GetEffectiveExpiryTime()
        {
            if (!IsExtended())
                return GetExpiryTime();

            MapDb2Entries entries = new(_mapId, _difficultyId);

            // return next reset Time
            if (IsExpired())
                return Global.InstanceLockMgr.GetNextResetTime(entries);

            // if not expired, return expiration Time + 1 reset period
            return GetExpiryTime() + TimeSpan.FromSeconds(entries.MapDifficulty.GetRaidDuration());
        }

        public uint GetMapId()
        {
            return _mapId;
        }

        public Difficulty GetDifficultyId()
        {
            return _difficultyId;
        }

        public uint GetInstanceId()
        {
            return _instanceId;
        }

        public void SetInstanceId(uint instanceId)
        {
            _instanceId = instanceId;
        }

        public DateTime GetExpiryTime()
        {
            return _expiryTime;
        }

        public void SetExpiryTime(DateTime expiryTime)
        {
            _expiryTime = expiryTime;
        }

        public bool IsExtended()
        {
            return _extended;
        }

        public void SetExtended(bool extended)
        {
            _extended = extended;
        }

        public InstanceLockData GetData()
        {
            return _data;
        }

        public virtual InstanceLockData GetInstanceInitializationData()
        {
            return _data;
        }

        public bool IsInUse()
        {
            return _isInUse;
        }

        public void SetInUse(bool inUse)
        {
            _isInUse = inUse;
        }
    }
}