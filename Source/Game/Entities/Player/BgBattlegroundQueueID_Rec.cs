// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.BattleGrounds;

namespace Game.Entities
{
    public class BgBattlegroundQueueID_Rec
    {
        public BattlegroundQueueTypeId BGQueueTypeId;
        public uint InvitedToInstance { get; set; }
        public uint JoinTime { get; set; }
        public bool Mercenary { get; set; }
    }
}