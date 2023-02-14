// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.DataStorage;
using Game.Networking;
using Game.Networking.Packets;
using System.Linq;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.AdventureMapStartQuest)]
        void HandleAdventureMapStartQuest(AdventureMapStartQuest startQuest)
        {
            Quest quest = Global.ObjectMgr.GetQuestTemplate(startQuest.QuestID);
            if (quest == null)
                return;

            var adventureMapPOI = CliDB.AdventureMapPOIStorage.Values.FirstOrDefault(adventureMap =>
            {
                return adventureMap.QuestID == startQuest.QuestID && _player.MeetPlayerCondition(adventureMap.PlayerConditionID);
            });

            if (adventureMapPOI == null)
                return;

            if (_player.CanTakeQuest(quest, true))
                _player.AddQuestAndCheckCompletion(quest, _player);
        }
    }
}
