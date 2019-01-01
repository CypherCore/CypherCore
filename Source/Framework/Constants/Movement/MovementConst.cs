/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

namespace Framework.Constants
{
    public enum MovementSlot
    {
        Idle,
        Active,
        Controlled,
        Max
    }

    public enum MovementGeneratorType
    {
        Idle = 0,                              // IdleMovement
        Random = 1,                              // RandomMovement
        Waypoint = 2,                              // WaypointMovement

        MaxDB = 3,                              // *** this and below motion types can't be set in DB.
        AnimalRandom = MaxDB,         // AnimalRandomMovementGenerator.h
        Confused = 4,                              // ConfusedMovementGenerator.h
        Chase = 5,                              // TargetedMovementGenerator.h
        Home = 6,                              // HomeMovementGenerator.h
        Flight = 7,                              // WaypointMovementGenerator.h
        Point = 8,                              // PointMovementGenerator.h
        Fleeing = 9,                              // FleeingMovementGenerator.h
        Distract = 10,                             // IdleMovementGenerator.h
        Assistance = 11,                             // PointMovementGenerator.h (first part of flee for assistance)
        AssistanceDistract = 12,                   // IdleMovementGenerator.h (second part of flee for assistance)
        TimedFleeing = 13,                         // FleeingMovementGenerator.h (alt.second part of flee for assistance)
        Follow = 14,
        Rotate = 15,
        Effect = 16,
        Null = 17,
        SplineChain = 18,
        Max
    }

    public struct EventId
    {
        public const uint Charge = 1003;
        public const uint Jump = 1004;

        /// Special charge event which is used for charge spells that have explicit targets
        /// and had a path already generated - using it in PointMovementGenerator will not
        /// create a new spline and launch it
        public const uint ChargePrepath = 1005;
        public const uint SmartRandomPoint = 0xFFFFFE;
        public const uint SmartEscortLastOCCPoint = 0xFFFFFF;
    }

    public enum AnimType
    {
        ToGround = 0, // 460 = ToGround, index of AnimationData.dbc
        FlyToFly = 1, // 461 = FlyToFly?
        ToFly = 2, // 458 = ToFly
        FlyToGround = 3  // 463 = FlyToGround
    }

    public enum RotateDirection
    {
        Left,
        Right
    }

    public enum UpdateCollisionHeightReason
    {
        Scale = 0,
        Mount = 1,
        Force = 2
    }
}
