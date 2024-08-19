// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using System;

namespace Game.Movement
{
    public class MoveSplineFlag
    {
        public MoveSplineFlag() { }
        public MoveSplineFlag(MoveSplineFlagEnum f) { Flags = f; }
        public MoveSplineFlag(MoveSplineFlag f) { Flags = f.Flags; }

        public bool IsSmooth() { return Flags.HasAnyFlag(MoveSplineFlagEnum.Catmullrom); }
        public bool IsLinear() { return !IsSmooth(); }

        public bool HasAllFlags(MoveSplineFlagEnum f) { return (Flags & f) == f; }
        public bool HasFlag(MoveSplineFlagEnum f) { return (Flags & f) != 0; }

        public void SetUnsetFlag(MoveSplineFlagEnum f, bool Set = true)
        {
            if (Set)
                Flags |= f;
            else
                Flags &= ~f;
        }

        public void EnableAnimation() { Flags = (Flags & ~(MoveSplineFlagEnum.Falling | MoveSplineFlagEnum.Parabolic | MoveSplineFlagEnum.FallingSlow | MoveSplineFlagEnum.FadeObject)) | MoveSplineFlagEnum.Animation; }
        public void EnableParabolic() { Flags = (Flags & ~(MoveSplineFlagEnum.Falling | MoveSplineFlagEnum.Animation | MoveSplineFlagEnum.FallingSlow | MoveSplineFlagEnum.FadeObject)) | MoveSplineFlagEnum.Parabolic; }
        public void EnableFlying() { Flags = (Flags & ~MoveSplineFlagEnum.Falling) | MoveSplineFlagEnum.Flying; }
        public void EnableFalling() { Flags = (Flags & ~(MoveSplineFlagEnum.Parabolic | MoveSplineFlagEnum.Animation | MoveSplineFlagEnum.Flying)) | MoveSplineFlagEnum.Falling; }
        public void EnableCatmullRom() { Flags = (Flags & ~MoveSplineFlagEnum.SmoothGroundPath) | MoveSplineFlagEnum.Catmullrom; }
        public void EnableTransportEnter() { Flags = (Flags & ~MoveSplineFlagEnum.TransportExit) | MoveSplineFlagEnum.TransportEnter; }
        public void EnableTransportExit() { Flags = (Flags & ~MoveSplineFlagEnum.TransportEnter) | MoveSplineFlagEnum.TransportExit; }
        public void EnableSteering() { Flags = (Flags & ~MoveSplineFlagEnum.SmoothGroundPath) | MoveSplineFlagEnum.Steering; }

        public MoveSplineFlagEnum Flags;
        public byte animTier;
    }
}
