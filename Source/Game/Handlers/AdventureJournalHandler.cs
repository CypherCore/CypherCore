// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Networking;
using Game.Networking.Packets;

namespace Game
{
	public partial class WorldSession
	{
		[WorldPacketHandler(ClientOpcodes.AdventureJournalOpenQuest)]
		private void HandleAdventureJournalOpenQuest(AdventureJournalOpenQuest openQuest)
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
		private void HandleAdventureJournalUpdateSuggestions(AdventureJournalUpdateSuggestions updateSuggestions)
		{
			var uiDisplay = Global.DB2Mgr.GetUiDisplayForClass(_player.GetClass());

			if (uiDisplay != null)
				if (!_player.MeetPlayerCondition(uiDisplay.AdvGuidePlayerConditionID))
					return;

			AdventureJournalDataResponse response = new();
			response.OnLevelUp = updateSuggestions.OnLevelUp;

			foreach (var adventureJournal in CliDB.AdventureJournalStorage.Values)
				if (_player.MeetPlayerCondition(adventureJournal.PlayerConditionID))
				{
					AdventureJournalEntry adventureJournalData;
					adventureJournalData.AdventureJournalID = (int)adventureJournal.Id;
					adventureJournalData.Priority           = adventureJournal.PriorityMax;
					response.AdventureJournalDatas.Add(adventureJournalData);
				}

			SendPacket(response);
		}
	}
}