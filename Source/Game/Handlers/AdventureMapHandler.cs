// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;
using Game.Networking;
using Game.Networking.Packets;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.CheckIsAdventureMapPoiValid)]
        void HandleCheckIsAdventureMapPoiValid(CheckIsAdventureMapPoiValid checkIsAdventureMapPoiValid)
        {
            AdventureMapPOIRecord entry = CliDB.AdventureMapPOIStorage.LookupByKey(checkIsAdventureMapPoiValid.AdventureMapPoiID);
            if (entry == null)
                return;

            void sendIsPoiValid(uint adventureMapPoiId, bool isVisible)
            {
                PlayerIsAdventureMapPoiValid isMapPoiValid = new();
                isMapPoiValid.AdventureMapPoiID = adventureMapPoiId;
                isMapPoiValid.IsVisible = isVisible;
                SendPacket(isMapPoiValid);
            };

            Quest quest = Global.ObjectMgr.GetQuestTemplate(entry.QuestID);
            if (quest == null)
            {
                sendIsPoiValid(entry.Id, false);
                return;
            }

            if (!_player.MeetPlayerCondition(entry.PlayerConditionID))
            {
                sendIsPoiValid(entry.Id, false);
                return;
            }

            sendIsPoiValid(entry.Id, true);
        }

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
