// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Entities
{
    public struct PvPInfo
	{
		public bool IsHostile { get; set; }
        public bool IsInHostileArea { get; set; } //> Marks if player is in an area which forces PvP flag
        public bool IsInNoPvPArea { get; set; }   //> Marks if player is in a sanctuary or friendly capital city
        public bool IsInFFAPvPArea { get; set; }  //> Marks if player is in an FFAPvP area (such as Gurubashi Arena)
        public long EndTimer { get; set; }        //> Time when player unflags himself for PvP (flag removed after 5 minutes)
    }
}