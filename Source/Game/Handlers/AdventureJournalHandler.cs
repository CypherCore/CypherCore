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
using Game.DataStorage;
using Game.Networking;
using Game.Networking.Packets;
using System.Collections.Generic;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.AdventureJournalOpenQuest)]
        void HandleAdventureJournalOpenQuest(AdventureJournalOpenQuest openQuest)
        {
            var uiDisplay = Global.DB2Mgr.GetUiDisplayForClass(_player.GetClass());
            if (uiDisplay != null)
                if (!_player.MeetPlayerCondition(uiDisplay.AdvGuidePlayerConditionID))
                    return;

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

        [WorldPacketHandler(ClientOpcodes.AdventureJournalUpdateSuggestions)]
        void HandleAdventureJournalUpdateSuggestions(AdventureJournalUpdateSuggestions updateSuggestions)
        {
            var uiDisplay = Global.DB2Mgr.GetUiDisplayForClass(_player.GetClass());
            if (uiDisplay != null)
                if (!_player.MeetPlayerCondition(uiDisplay.AdvGuidePlayerConditionID))
                    return;

            AdventureJournalDataResponse response = new();
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
