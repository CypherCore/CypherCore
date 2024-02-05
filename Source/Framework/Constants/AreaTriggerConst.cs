// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Framework.Constants
{
    public enum AreaTriggerFlag
    {
        None = 0x00,
        IsServerSide = 0x01
    }

    public enum AreaTriggerShapeType
    {
        Sphere = 0,
        Box = 1,
        Unk = 2,
        Polygon = 3,
        Cylinder = 4,
        Disk = 5,
        BoundedPlane = 6,
        Max
    }

    public enum AreaTriggerActionTypes
    {
        Cast = 0,
        AddAura = 1,
        Teleport = 2,
        Max = 3
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
        Unk1 = 0x20,
        HasTargetRollPitchYaw = 0x40, // NYI
        HasAnimId = 0x80, // DEPRECATED
        Unk3 = 0x100,
        HasAnimKitId = 0x200, // DEPRECATED
        HasCircularMovement = 0x400, // DEPRECATED
        Unk5 = 0x800,
    }
}
