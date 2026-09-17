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
        None = 0x0000,
        HeightIgnoresScale = 0x0001,
        VisualAnimIsDecay = 0x0002,
        AbsoluteOrientation = 0x0004,
        FaceMovementDir = 0x0008, // NYI
        FollowsTerrain = 0x0010, // NYI
        AlwaysExterior = 0x0020,
        UsesUnitRawFacing = 0x0040  // NYI
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
        FaceMovementDir = 0x0040, // applies when attached to unit (refers to movement direction of the unit)
        FollowsTerrain = 0x0080,
        UsesUnitRawFacing = 0x0100, // uses GetTransOffsetO instead of GetOrientation when attached to unit on a transport/vehicle
        AlwaysExterior = 0x0200,
        HasPlayers = 0x0400,
    }

    public enum AreaTriggerPathType
    {
        Spline = 0,
        Orbit = 1,
        Stationary = 2,
        MovementScript = 3
    }

    public enum AreaTriggerExitReason
    {
        NotInside = 0, // Unit leave areatrigger
        ByExpire = 1  // On areatrigger despawn
    }
}
