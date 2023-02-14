// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
