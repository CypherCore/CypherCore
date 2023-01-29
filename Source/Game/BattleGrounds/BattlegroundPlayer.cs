// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.BattleGrounds
{
    public class BattlegroundPlayer
    {
        public int ActiveSpec { get; set; } // Player's active spec
        public bool Mercenary { get; set; }
        public long OfflineRemoveTime { get; set; } // for tracking and removing offline players from queue after 5 Time.Minutes
        public Team Team { get; set; }              // Player's team
    }
}