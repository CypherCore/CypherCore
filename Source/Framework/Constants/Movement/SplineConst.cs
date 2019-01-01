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
 */﻿

using System;

namespace Framework.Constants
{
    public enum MonsterMoveType
    {
        Normal = 0,
        FacingSpot = 1,
        FacingTarget = 2,
        FacingAngle = 3
    }

    [Flags]
    public enum SplineFlag : uint
    {
        None = 0x00,
        // x00-x07 used as animation Ids storage in pair with Animation flag
        Unknown0 = 0x00000008,           // NOT VERIFIED - does someting related to falling/fixed orientation
        FallingSlow = 0x00000010,
        Done = 0x00000020,
        Falling = 0x00000040,           // Affects elevation computation, can't be combined with Parabolic flag
        NoSpline = 0x00000080,
        Unknown1 = 0x00000100,           // NOT VERIFIED
        Flying = 0x00000200,           // Smooth movement(Catmullrom interpolation mode), flying animation
        OrientationFixed = 0x00000400,           // Model orientation fixed
        Catmullrom = 0x00000800,           // Used Catmullrom interpolation mode
        Cyclic = 0x00001000,           // Movement by cycled spline
        EnterCycle = 0x00002000,           // Everytimes appears with cyclic flag in monster move packet, erases first spline vertex after first cycle done
        Frozen = 0x00004000,           // Will never arrive
        TransportEnter = 0x00008000,
        TransportExit = 0x00010000,
        Unknown2 = 0x00020000,           // NOT VERIFIED
        Unknown3 = 0x00040000,           // NOT VERIFIED
        Backward = 0x00080000,
        SmoothGroundPath = 0x00100000,
        CanSwim = 0x00200000,
        UncompressedPath = 0x00400000,
        Unknown4 = 0x00800000,           // NOT VERIFIED
        Unknown5 = 0x01000000,           // NOT VERIFIED
        Animation = 0x02000000,           // Plays animation after some time passed
        Parabolic = 0x04000000,           // Affects elevation computation, can't be combined with Falling flag
        FadeObject = 0x08000000,
        Steering = 0x10000000,
        Unknown8 = 0x20000000,           // NOT VERIFIED
        Unknown9 = 0x40000000,           // NOT VERIFIED
        Unknown10 = 0x80000000,           // NOT VERIFIED

        // animation ids stored here, see AnimType enum, used with Animation flag
        MaskAnimations = 0x7,
        // flags that shouldn't be appended into SMSG_MONSTER_MOVE\SMSG_MONSTER_MOVE_TRANSPORT packet, should be more probably
        MaskNoMonsterMove = MaskAnimations | Done,
        // Unused, not suported flags
        MaskUnused = NoSpline | EnterCycle | Frozen | Unknown0 | Unknown1 | Unknown2 | Unknown3 | Unknown4 | FadeObject | Steering | Unknown8 | Unknown9 | Unknown10
    }
}
