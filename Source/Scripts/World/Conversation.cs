// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.AI;
using Game.Entities;

namespace Scripts.World.Conversations
{
    class conversation_allied_race_dk_defender_of_azeroth : ConversationAI
    {
        const uint NpcTalkToYourCommanderCredit = 161709;
        const uint NpcListenToYourCommanderCredit = 163027;

        const uint ConversationLinePlayer = 32926;

        public conversation_allied_race_dk_defender_of_azeroth(Conversation conversation) : base(conversation) { }

        public override void OnCreate(Unit creator)
        {
            Player player = creator.ToPlayer();
            if (player != null)
                player.KilledMonsterCredit(NpcTalkToYourCommanderCredit);
        }

        public override void OnLineStarted(uint lineId, Player sender)
        {
            if (lineId != ConversationLinePlayer)
                return;

            sender.KilledMonsterCredit(NpcListenToYourCommanderCredit);
        }
    }
}