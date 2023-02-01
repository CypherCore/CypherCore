﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Numerics;

namespace Game.Collision
{
    public class AreaInfoCallback : WorkerCallback
    {
        private readonly ModelInstance[] _prims;

        public AreaInfoCallback(ModelInstance[] val)
        {
            _prims = val;
        }

        public AreaInfo AInfo { get; set; } = new();

        public override void Invoke(Vector3 point, uint entry)
        {
            if (_prims[entry] == null)
                return;

            _prims[entry].IntersectPoint(point, AInfo);
        }
    }
}