// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using System;

namespace Game.Movement
{
    public class MoveSplineFlag
    {
        public MoveSplineFlag() { }
        public MoveSplineFlag(SplineFlag f) { Flags = f; }
        public MoveSplineFlag(MoveSplineFlag f) { Flags = f.Flags; }

        public bool IsSmooth() { return Flags.HasAnyFlag(SplineFlag.Catmullrom); }
        public bool IsLinear() { return !IsSmooth(); }

        public bool HasAllFlags(SplineFlag f) { return (Flags & f) == f; }
        public bool HasFlag(SplineFlag f) { return (Flags & f) != 0; }

        public void SetUnsetFlag(SplineFlag f, bool Set = true)
        {
            if (Set)
                Flags |= f;
            else
                Flags &= ~f;
        }

        public void EnableAnimation() { Flags = (Flags & ~(SplineFlag.Falling | SplineFlag.Parabolic | SplineFlag.FallingSlow | SplineFlag.FadeObject)) | SplineFlag.Animation; }
        public void EnableParabolic() { Flags = (Flags & ~(SplineFlag.Falling | SplineFlag.Animation | SplineFlag.FallingSlow | SplineFlag.FadeObject)) | SplineFlag.Parabolic; }
        public void EnableFlying() { Flags = (Flags & ~SplineFlag.Falling) | SplineFlag.Flying; }
        public void EnableFalling() { Flags = (Flags & ~(SplineFlag.Parabolic | SplineFlag.Animation | SplineFlag.Flying)) | SplineFlag.Falling; }
        public void EnableCatmullRom() { Flags = (Flags & ~SplineFlag.SmoothGroundPath) | SplineFlag.Catmullrom; }
        public void EnableTransportEnter() { Flags = (Flags & ~SplineFlag.TransportExit) | SplineFlag.TransportEnter; }
        public void EnableTransportExit() { Flags = (Flags & ~SplineFlag.TransportEnter) | SplineFlag.TransportExit; }

        public SplineFlag Flags;
        public byte animTier;
    }
}
