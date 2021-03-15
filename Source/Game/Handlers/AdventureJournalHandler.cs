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

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.AdventureJournalOpenQuest)]
        void HandleAdventureJournalOpenQuest(AdventureJournalOpenQuest openQuest)
        {
            var adventureJournal = CliDB.AdventureJournalStorage.LookupByKey(openQuest.AdventureJournalID);
            if (adventureJournal == null)
                return;

            if (!_player.MeetPlayerCondition(adventureJournal.PlayerConditionID))
                return;

            Quest quest = Global.ObjectMgr.GetQuestTemplate(adventureJournal.QuestID);
            if (quest == null)
                return;

            if (_player.CanTakeQuest(quest, true))
                _player.PlayerTalkClass.SendQuestGiverQuestDetails(quest, _player.GetGUID(), true, false);
        }

        [WorldPacketHandler(ClientOpcodes.AdventureJournalStartQuest)]
        void HandleAdventureJournalStartQuest(AdventureJournalStartQuest startQuest)
        {
            Quest quest = Global.ObjectMgr.GetQuestTemplate(startQuest.QuestID);
            if (quest == null)
                return;

            AdventureJournalRecord adventureJournalEntry = null;
            foreach (var adventureJournal in CliDB.AdventureJournalStorage.Values)
            {
                if (quest.Id == adventureJournal.QuestID)
                {
                    adventureJournalEntry = adventureJournal;
                    break;
                }
            }

            if (adventureJournalEntry == null)
                return;

            if (_player.MeetPlayerCondition(adventureJournalEntry.PlayerConditionID) && _player.CanTakeQuest(quest, true))
                _player.AddQuestAndCheckCompletion(quest, null);
        }

        [WorldPacketHandler(ClientOpcodes.AdventureJournalUpdateSuggestions)]
        void HandleAdventureJournalUpdateSuggestions(AdventureJournalUpdateSuggestions updateSuggestions)
        {
            AdventureJournalDataResponse response = new AdventureJournalDataResponse();
            response.OnLevelUp = updateSuggestions.OnLevelUp;

            foreach (var adventureJournal in CliDB.AdventureJournalStorage.Values)
            {
                if (_player.MeetPlayerCondition(adventureJournal.PlayerConditionID))
                {
                    AdventureJournalEntry adventureJournalData;
                    adventureJournalData.AdventureJournalID = (int)adventureJournal.Id;
                    adventureJournalData.Priority = adventureJournal.PriorityMax;
                    response.AdventureJournalDatas.Add(adventureJournalData);
                }
            }

            SendPacket(response);
        }
    }
}
