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

using System;

namespace Framework.Constants
{
    [Flags]
    public enum MovementFlag
    {
        None                 = 0x0,
        Forward              = 0x1,
        Backward             = 0x2,
        StrafeLeft           = 0x4,
        StrafeRight          = 0x8,
        Left                 = 0x10,
        Right                = 0x20,
        PitchUp              = 0x40,
        PitchDown            = 0x80,
        Walking              = 0x100,
        DisableGravity       = 0x200,
        Root                 = 0x400,
        Falling              = 0x800,
        FallingFar           = 0x1000,
        PendingStop          = 0x2000,
        PendingStrafeStop    = 0x4000,
        PendingForward       = 0x8000,
        PendingBackward      = 0x10000,
        PendingStrafeLeft    = 0x20000,
        PendingStrafeRight   = 0x40000,
        PendingRoot          = 0x80000,
        Swimming             = 0x100000,
        Ascending            = 0x200000,
        Descending           = 0x400000,
        CanFly               = 0x800000,
        Flying               = 0x1000000,
        SplineElevation      = 0x2000000,
        WaterWalk            = 0x4000000,
        FallingSlow          = 0x8000000,
        Hover                = 0x10000000,
        DisableCollision     = 0x20000000,

        MaskMoving = Forward | Backward | StrafeLeft | StrafeRight | Falling | Ascending | Descending,

        MaskTurning = Left | Right | PitchUp | PitchDown,

        MaskMovingFly = Flying | Ascending | Descending,

        MaskCreatureAllowed = Forward | DisableGravity | Root | Swimming | 
            CanFly | WaterWalk | FallingSlow | Hover | DisableCollision,

        MaskPlayerOnly = Flying,

        MaskHasPlayerStatusOpcode = DisableGravity | Root | CanFly | WaterWalk |
            FallingSlow | Hover | DisableCollision
    }

    [Flags]
    public enum MovementFlag2
    {
        None                                = 0x0,
        NoStrafe                            = 0x1,
        NoJumping                           = 0x2,
        FullSpeedTurning                    = 0x4,
        FullSpeedPitching                   = 0x8,
        AlwaysAllowPitching                 = 0x10,
        IsVehicleExitVoluntary              = 0x20,
        JumpSplineInAir                     = 0x40,
        AnimTierInTrans                     = 0x80,
        WaterwalkingFullPitch               = 0x100, // will always waterwalk, even if facing the camera directly down
        VehiclePassengerIsTransitionAllowed = 0x200,
        CanSwimToFlyTrans                   = 0x400,
        Unk11                               = 0x800, // terrain normal calculation is disabled if this flag is not present, client automatically handles setting this flag
        CanTurnWhileFalling                 = 0x1000,
        Unk13                               = 0x2000, // will always waterwalk, even if facing the camera directly down
        IgnoreMovementForces                = 0x4000,
        Unk15                               = 0x8000,
        CanDoubleJump                       = 0x10000,
        DoubleJump                          = 0x20000,
        // these flags cannot be sent (18 bits in packet)
        Unk18                               = 0x40000,
        Unk19                               = 0x80000,
        InterpolatedMovement                = 0x100000,
        InterpolatedTurning                 = 0x200000,
        InterpolatedPitching                = 0x400000
    }
}
