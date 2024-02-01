// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Framework.Constants
{
    public enum AreaTriggerFlags
    {
        HasAbsoluteOrientation = 0x01,
        HasDynamicShape = 0x02, // Implemented For Spheres
        HasAttached = 0x04,
        HasFaceMovementDir = 0x08,
        HasFollowsTerrain = 0x010, // Nyi
        Unk1 = 0x020,
        HasTargetRollPitchYaw = 0x040, // Nyi
        HasAnimID = 0x080,
        Unk3 = 0x100,
        HasAnimKitID = 0x200,
        HasCircularMovement = 0x400,
        Unk5 = 0x800
    }

    public enum AreaTriggerTypes
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
}
