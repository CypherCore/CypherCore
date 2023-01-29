// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Game.Entities;

namespace Game.Movement
{
    public struct ChaseAngle
    {
        public float RelativeAngle; // we want to be at this angle relative to the Target (0 = front, _PI = back)
        public float Tolerance;     // but we'll tolerate anything within +- this much

        public ChaseAngle(float angle, float tol = MathFunctions.PiOver4)
        {
            RelativeAngle = Position.NormalizeOrientation(angle);
            Tolerance = tol;
        }

        public float UpperBound()
        {
            return Position.NormalizeOrientation(RelativeAngle + Tolerance);
        }

        public float LowerBound()
        {
            return Position.NormalizeOrientation(RelativeAngle - Tolerance);
        }

        public bool IsAngleOkay(float relAngle)
        {
            float diff = Math.Abs(relAngle - RelativeAngle);

            return (Math.Min(diff, (2 * MathF.PI) - diff) <= Tolerance);
        }
    }
}