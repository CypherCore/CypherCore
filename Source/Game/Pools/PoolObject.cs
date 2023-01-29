// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Game
{
    public class PoolObject
    {
        public float Chance { get; set; }

        public ulong Guid { get; set; }

        public PoolObject(ulong _guid, float _chance)
        {
            Guid = _guid;
            Chance = Math.Abs(_chance);
        }
    }
}