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

using Framework.Constants;
using System;

namespace Game.Movement
{
    public class MoveSplineFlag
    {
        public MoveSplineFlag() { }
        public MoveSplineFlag(SplineFlag f) { Flags = f; }
        public MoveSplineFlag(MoveSplineFlag f) { Flags = f.Flags; }

        public bool isSmooth() { return Flags.HasAnyFlag(SplineFlag.Catmullrom); }
        public bool isLinear() { return !isSmooth(); }

        public byte getAnimationId() { return animId; }
        public bool hasAllFlags(SplineFlag f) { return (Flags & f) == f; }
        public bool hasFlag(SplineFlag f) { return (Flags & f) != 0; }

        public void SetUnsetFlag(SplineFlag f, bool Set = true)
        {
            if (Set)
                Flags |= f;
            else
                Flags &= ~f;
        }

        public void EnableAnimation(uint anim) { Flags = (Flags & ~(SplineFlag.MaskAnimations | SplineFlag.Falling | SplineFlag.Parabolic | SplineFlag.FallingSlow | SplineFlag.FadeObject)) | SplineFlag.Animation | ((SplineFlag)anim & SplineFlag.MaskAnimations); }
        public void EnableParabolic() { Flags = (Flags & ~(SplineFlag.MaskAnimations | SplineFlag.Falling | SplineFlag.Animation | SplineFlag.FallingSlow | SplineFlag.FadeObject)) | SplineFlag.Parabolic; }
        public void EnableFlying() { Flags = (Flags & ~SplineFlag.Falling) | SplineFlag.Flying; }
        public void EnableFalling() { Flags = (Flags & ~(SplineFlag.MaskAnimations | SplineFlag.Parabolic | SplineFlag.Animation | SplineFlag.Flying)) | SplineFlag.Falling; }
        public void EnableCatmullRom() { Flags = (Flags & ~SplineFlag.SmoothGroundPath) | SplineFlag.Catmullrom; }
        public void EnableTransportEnter() { Flags = (Flags & ~SplineFlag.TransportExit) | SplineFlag.TransportEnter; }
        public void EnableTransportExit() { Flags = (Flags & ~SplineFlag.TransportEnter) | SplineFlag.TransportExit; }

        public SplineFlag Flags;
        public byte animId;
    }
}
