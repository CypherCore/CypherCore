// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Game
{
    public class GuildReward
    {
        public List<uint> AchievementsRequired { get; set; } = new();
        public ulong Cost { get; set; }
        public uint ItemID { get; set; }
        public byte MinGuildRep { get; set; }
        public ulong RaceMask { get; set; }
    }
}