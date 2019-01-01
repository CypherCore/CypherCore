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
    public enum UpdateFlag
    {
        None                = 0x0,
        Self                = 0x1,
        Transport           = 0x2,
        HasTarget           = 0x4,
        Living              = 0x8,
        StationaryPosition  = 0x10,
        Vehicle             = 0x20,
        TransportPosition   = 0x40,
        Rotation            = 0x80,
        AnimKits            = 0x100,
        Areatrigger         = 0x0200,
        Gameobject          = 0x0400,
        //UPDATEFLAG_REPLACE_ACTIVE        = 0x0800,
        //UPDATEFLAG_NO_BIRTH_ANIM         = 0x1000,
        //UPDATEFLAG_ENABLE_PORTALS        = 0x2000,
        //UPDATEFLAG_PLAY_HOVER_ANIM       = 0x4000,
        //UPDATEFLAG_IS_SUPPRESSING_GREETINGS = 0x8000
        //UPDATEFLAG_SCENEOBJECT           = 0x10000,
        //UPDATEFLAG_SCENE_PENDING_INSTANCE = 0x20000
    }
}
