// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IConversation;

namespace Scripts.World
{
    [Script]
    internal class conversation_allied_race_dk_defender_of_azeroth : ScriptObjectAutoAddDBBound, IConversationOnConversationCreate, IConversationOnConversationLineStarted
    {
        private const uint NpcTalkToYourCommanderCredit = 161709;
        private const uint NpcListenToYourCommanderCredit = 163027;
        private const uint ConversationLinePlayer = 32926;

        public conversation_allied_race_dk_defender_of_azeroth() : base("conversation_allied_race_dk_defender_of_azeroth")
        {
        }

        public void OnConversationCreate(Conversation conversation, Unit creator)
        {
            Player player = creator.ToPlayer();

            player?.KilledMonsterCredit(NpcTalkToYourCommanderCredit);
        }

        public void OnConversationLineStarted(Conversation conversation, uint lineId, Player sender)
        {
            if (lineId != ConversationLinePlayer)
                return;

            sender.KilledMonsterCredit(NpcListenToYourCommanderCredit);
        }
    }
}