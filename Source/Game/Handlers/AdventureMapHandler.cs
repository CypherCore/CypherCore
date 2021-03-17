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
using Framework.IO;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Guilds;
using Game.Maps;
using Game.Misc;
using Game.Networking;
using Game.Networking.Packets;
using Game.PvP;
using System;
using System.Collections.Generic;
using System.Text;
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
