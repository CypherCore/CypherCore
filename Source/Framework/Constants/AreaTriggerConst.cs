// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Framework.Constants
{
    public enum AreaTriggerFlag
    {
        None = 0x00,
        IsServerSide = 0x01
    }

    public enum AreaTriggerActionTypes
    {
        Cast = 0,
        AddAura = 1,
        Teleport = 2,
        Tavern = 3,

        Max
    }

    public enum AreaTriggerActionUserTypes
    {
        Any = 0,
        Friend = 1,
        Enemy = 2,
        Raid = 3,
        Party = 4,
        Caster = 5,
        Max = 6
    }

    public enum AreaTriggerCreatePropertiesFlag
    {
        None = 0x00,
        HasAbsoluteOrientation = 0x01,
        HasDynamicShape = 0x02,
        HasAttached = 0x04,
        HasFaceMovementDir = 0x08,
        HasFollowsTerrain = 0x10, // NYI
        AlwaysExterior = 0x20,
        HasTargetRollPitchYaw = 0x40, // NYI
        HasAnimId = 0x80, // DEPRECATED
        VisualAnimIsDecay = 0x100,
        HasAnimKitId = 0x200, // DEPRECATED
        HasCircularMovement = 0x400, // DEPRECATED
        Unk5 = 0x800,
    }

    public enum AreaTriggerFieldFlags
    {
        None = 0x0000,
        HeightIgnoresScale = 0x0001,
        WowLabsCircle = 0x0002,
        CanLoop = 0x0004,
        AbsoluteOrientation = 0x0008,
        DynamicShape = 0x0010,
        Attached = 0x0020,
        FaceMovementDir = 0x0040,
        FollowsTerrain = 0x0080,
        Unknown1025 = 0x0100,
        AlwaysExterior = 0x0200,
        HasPlayers = 0x0400,
    }

    public enum AreaTriggerPathType
    {
        Spline = 0,
        Orbit = 1,
        None = 2,
        MovementScript = 3
    }
}
