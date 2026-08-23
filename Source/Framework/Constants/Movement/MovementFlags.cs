// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Framework.Constants
{
    [Flags]
    public enum MovementFlag : ulong
    {
        None = 0x00,   // Skip
        Forward = 0x01,
        Backward = 0x02,
        StrafeLeft = 0x04,
        StrafeRight = 0x08,
        Left = 0x10,
        Right = 0x20,
        PitchUp = 0x40,
        PitchDown = 0x80,
        Walking = 0x100,   // Walking
        DisableGravity = 0x200,   // Former Levitating. This Is Used When Walking Is Not Possible.
        Root = 0x400,   // Must Not Be Set Along With MaskMoving
        Falling = 0x800,   // Damage Dealt On That Type Of Falling
        FallingFar = 0x1000,
        PendingStop = 0x2000,
        PendingStrafeStop = 0x4000,
        PendingForward = 0x8000,
        PendingBackward = 0x10000,
        PendingStrafeLeft = 0x20000,
        PendingStrafeRight = 0x40000,
        PendingRoot = 0x80000,
        Swimming = 0x100000,   // Appears With Fly Flag Also
        Ascending = 0x200000,   // Press "Space" When Flying
        Descending = 0x400000,
        CanFly = 0x800000,   // Appears When Unit Can Fly. For Example, Appears When A Player Sits On A Mount.
        Flying = 0x1000000,   // Unit Is Actually Flying. Pretty Sure This Is Only Used For Players. Creatures Use DisableGravity
        SplineElevation = 0x2000000,   // Used For Flight Paths
        Waterwalking = 0x4000000,   // Prevent Unit From Falling Through Water
        FallingSlow = 0x8000000,   // Active Rogue Safe Fall Spell (Passive)
        CannotSwim = 0x10000000,
        DisableCollision = 0x20000000,
        Knockback = 0x40000000,
        TouchingGround = 0x80000000,   // Terrain Normal Calculation Is Disabled If This Flag Is Not Present, Client Automatically Handles Setting This Flag
        NoStrafe = 0x100000000,
        NoJumping = 0x200000000,
        FullSpeedTurning = 0x400000000,
        FullSpeedPitching = 0x800000000,
        AlwaysAllowPitching = 0x1000000000,
        WaterwalkingFullPitch = 0x2000000000,
        CanSwimToFlyTrans = 0x4000000000,
        CanTurnWhileFalling = 0x8000000000,
        IgnoreMovementForces = 0x10000000000,
        CanDoubleJump = 0x20000000000,
        DoubleJump = 0x40000000000,
        Unk43 = 0x80000000000,   // Old Movementflags2 0x8000
        DisableInertia = 0x100000000000,
        CanAdvFly = 0x200000000000,
        AdvFlying = 0x400000000000,
        Unk47 = 0x800000000000,   // Old Movementflags3 0x8
        Unk48 = 0x1000000000000,   // Old Movementflags3 0x10
        FallingAdvFlyDismount = 0x2000000000000,   // Falling After Dismounting While Adv Flying
        Unk50 = 0x4000000000000,   // Old Movementflags3 0x200
        WalkingOnWater = 0x8000000000000,   // Currently On Water Surface
        CanDrive = 0x10000000000000,
        DrivingForward = 0x20000000000000,
        DrivingBackward = 0x40000000000000,
        Unk55 = 0x80000000000000,   // Old Movementflags3 0x20000
        Unk56 = 0x100000000000000,   // Old Movementflags3 0x40000
        Unk57 = 0x200000000000000,   // Old Movementflags3 0x80000
        Unk58 = 0x400000000000000,
        Hover = 0x800000000000000,

        MaskMoving =
            Forward | Backward | StrafeLeft | StrafeRight |
            Falling | Ascending | Descending,// Skip

        MaskTurning =
            Left | Right | PitchUp | PitchDown, // Skip

        MaskMovingFly =
            Flying | Ascending | Descending, // Skip

        // Movement Flags Allowed For Creature In Createobject - We Need To Keep All Other Enabled Serverside
        // To Properly Calculate All Movement
        MaskCreatureAllowed =
            Forward | DisableGravity | Root | Swimming |
            CanFly | Waterwalking | FallingSlow | Hover | DisableCollision, // Skip

        /// @Todo If Needed: Add More Flags To This Masks That Are Exclusive To Players
        MaskPlayerOnly =
            Flying, // Skip

        /// Movement Flags That Have Change Status Opcodes Associated For Players
        MaskHasPlayerStatusOpcode = DisableGravity | Root |
            CanFly | Waterwalking | FallingSlow | Hover | DisableCollision // Skip
    }
}
