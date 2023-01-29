// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Numerics;

namespace Game.Movement
{
    public class SplineChainLink
    {
        public uint ExpectedDuration { get; set; }
        public List<Vector3> Points { get; set; } = new();
        public uint TimeToNext { get; set; }
        public float Velocity { get; set; }

        public SplineChainLink(Vector3[] points, uint expectedDuration, uint msToNext, float velocity)
        {
            Points.AddRange(points);
            ExpectedDuration = expectedDuration;
            TimeToNext = msToNext;
            Velocity = velocity;
        }

        public SplineChainLink(uint expectedDuration, uint msToNext, float velocity)
        {
            ExpectedDuration = expectedDuration;
            TimeToNext = msToNext;
            Velocity = velocity;
        }
    }
}