// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.AI
{
    public enum SmartEscortState
    {
        None = 0x00,      //nothing in progress
        Escorting = 0x01, //escort is in progress
        Returning = 0x02, //escort is returning after being in combat
        Paused = 0x04     //will not proceed with waypoints before State is removed
    }
}