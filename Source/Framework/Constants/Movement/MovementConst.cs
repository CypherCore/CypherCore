// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Framework.Constants
{
    public enum MovementGeneratorMode
    {
        Default = 0,
        Override
    }

    public enum MovementGeneratorPriority
    {
        None = 0,
        Normal,
        Highest
    }

    public enum MovementSlot
    {
        Default = 0,
        Active,
        Max
    }

    public enum MovementGeneratorType
    {
        Idle = 0, // IdleMovement
        Random = 1, // RandomMovement
        Waypoint = 2, // WaypointMovement

        MaxDB = 3, // *** this and below motion types can't be set in DB.
        Confused = 4, // ConfusedMovementGenerator
        Chase = 5, // TargetedMovementGenerator
        Home = 6, // HomeMovementGenerator
        Flight = 7, // WaypointMovementGenerator
        Point = 8, // PointMovementGenerator
        Fleeing = 9, // FleeingMovementGenerator
        Distract = 10, // IdleMovementGenerator
        Assistance = 11, // PointMovementGenerator
        AssistanceDistract = 12, // IdleMovementGenerator
        TimedFleeing = 13, // FleeingMovementGenerator
        Follow = 14,
        Rotate = 15,
        Effect = 16,
        SplineChain = 17, // SplineChainMovementGenerator
        Formation = 18, // FormationMovementGenerator
        Max
    }

    public enum MotionMasterFlags
    {
        None = 0x0,
        Update = 0x1, // Update in progress
        StaticInitializationPending = 0x2, // Static movement (MOTION_SLOT_DEFAULT) hasn't been initialized
        InitializationPending = 0x4, // MotionMaster is stalled until signaled
        Initializing = 0x8, // MotionMaster is initializing

        Delayed = Update | InitializationPending
    }

    public enum MotionMasterDelayedActionType
    {
        Clear = 0,
        ClearSlot,
        ClearMode,
        ClearPriority,
        Add,
        Remove,
        RemoveType,
        Initialize
    }

    public enum MovementGeneratorFlags
    {
        None = 0x000,
        InitializationPending = 0x001,
        Initialized = 0x002,
        SpeedUpdatePending = 0x004,
        Interrupted = 0x008,
        Paused = 0x010,
        TimedPaused = 0x020,
        Deactivated = 0x040,
        InformEnabled = 0x080,
        Finalized = 0x100,
        PersistOnDeath = 0x200,

        Transitory = SpeedUpdatePending | Interrupted
    }

    public struct EventId
    {
        public const uint Charge = 1003;
        public const uint Jump = 1004;

        /// Special charge event which is used for charge spells that have explicit targets
        /// and had a path already generated - using it in PointMovementGenerator will not
        /// create a new spline and launch it
        public const uint ChargePrepath = 1005;

        public const uint Face = 1006;
        public const uint VehicleBoard = 1007;
        public const uint VehicleExit = 1008;
        public const uint AssistMove = 1009;

        public const uint SmartRandomPoint = 0xFFFFFE;
        public const uint SmartEscortLastOCCPoint = 0xFFFFFF;
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

    public enum MovementForceType
    {
        SingleDirectional = 0, // always in a single direction
        Gravity = 1  // pushes/pulls away from a single point
    }
}
