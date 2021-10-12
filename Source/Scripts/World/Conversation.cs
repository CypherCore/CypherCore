/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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

using Framework.Constants;
using Game.Entities;
using Game.Scripting;

namespace Scripts.World
{
    [Script]
    class conversation_allied_race_dk_defender_of_azeroth : ConversationScript
    {
        const uint NpcTalkToYourCommanderCredit = 161709;
        const uint NpcListenToYourCommanderCredit = 163027;
        const uint ConversationLinePlayer = 32926;

        public conversation_allied_race_dk_defender_of_azeroth() : base("conversation_allied_race_dk_defender_of_azeroth") { }

        public override void OnConversationCreate(Conversation conversation, Unit creator)
        {
            conversation.AddActor(ObjectGuid.Create(HighGuid.Player, 0xFFFFFFFFFFFFFFFF), 1);
            Player player = creator.ToPlayer();
            if (player != null)
                player.KilledMonsterCredit(NpcTalkToYourCommanderCredit);
        }

        public override void OnConversationLineStarted(Conversation conversation, uint lineId, Player sender)
        {
            if (lineId != ConversationLinePlayer)
                return;

            sender.KilledMonsterCredit(NpcListenToYourCommanderCredit);
        }
    }
}
