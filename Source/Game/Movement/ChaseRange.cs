// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;

namespace Game.Movement
{
    public struct ChaseRange
    {
        // this contains info that informs how we should path!
        public float MinRange;     // we have to move if we are within this range...    (min. attack range)
        public float MinTolerance; // ...and if we are, we will move this far away
        public float MaxRange;     // we have to move if we are outside this range...   (max. attack range)
        public float MaxTolerance; // ...and if we are, we will move into this range

        public ChaseRange(float range)
        {
            MinRange = range > SharedConst.ContactDistance ? 0 : range - SharedConst.ContactDistance;
            MinTolerance = range;
            MaxRange = range + SharedConst.ContactDistance;
            MaxTolerance = range;
        }

        public ChaseRange(float min, float max)
        {
            MinRange = min;
            MinTolerance = Math.Min(min + SharedConst.ContactDistance, (min + max) / 2);
            MaxRange = max;
            MaxTolerance = Math.Max(max - SharedConst.ContactDistance, MinTolerance);
        }

        public ChaseRange(float min, float tMin, float tMax, float max)
        {
            MinRange = min;
            MinTolerance = tMin;
            MaxRange = max;
            MaxTolerance = tMax;
        }
    }
}