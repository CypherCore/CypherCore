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

namespace Framework.Constants
{
    public enum AreaTriggerFlags
    {
        HasAbsoluteOrientation = 0x01, // Nyi
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
        Max = 5
    }

    public enum AreaTriggerActionTypes
    {
        Cast = 0,
        AddAura = 1,
        Max = 2
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
