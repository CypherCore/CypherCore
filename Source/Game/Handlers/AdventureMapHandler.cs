// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
