// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Achievements;
using Game.DataStorage;
using Game.Guilds;
using Game.Scenarios;

namespace Game.Entities
{
	public partial class Player
	{
		public void ResetAchievements()
		{
			_achievementSys.Reset();
		}

		public void SendRespondInspectAchievements(Player player)
		{
			_achievementSys.SendAchievementInfo(player);
		}

		public uint GetAchievementPoints()
		{
			return _achievementSys.GetAchievementPoints();
		}

		public ICollection<uint> GetCompletedAchievementIds()
		{
			return _achievementSys.GetCompletedAchievementIds();
		}

		public bool HasAchieved(uint achievementId)
		{
			return _achievementSys.HasAchieved(achievementId);
		}

		public void StartCriteriaTimer(CriteriaStartEvent startEvent, uint entry, uint timeLost = 0)
		{
			_achievementSys.StartCriteriaTimer(startEvent, entry, timeLost);
		}

		public void RemoveCriteriaTimer(CriteriaStartEvent startEvent, uint entry)
		{
			_achievementSys.RemoveCriteriaTimer(startEvent, entry);
		}

		public void ResetCriteria(CriteriaFailEvent failEvent, uint failAsset, bool evenIfCriteriaComplete = false)
		{
			_achievementSys.ResetCriteria(failEvent, failAsset, evenIfCriteriaComplete);
			_questObjectiveCriteriaMgr.ResetCriteria(failEvent, failAsset, evenIfCriteriaComplete);
		}

		public void UpdateCriteria(CriteriaType type, ulong miscValue1 = 0, ulong miscValue2 = 0, ulong miscValue3 = 0, WorldObject refe = null)
		{
			_achievementSys.UpdateCriteria(type, miscValue1, miscValue2, miscValue3, refe, this);
			_questObjectiveCriteriaMgr.UpdateCriteria(type, miscValue1, miscValue2, miscValue3, refe, this);

			// Update only individual achievement criteria here, otherwise we may get multiple updates
			// from a single boss kill
			if (CriteriaManager.IsGroupCriteriaType(type))
				return;

			Scenario scenario = GetScenario();

			if (scenario != null)
				scenario.UpdateCriteria(type, miscValue1, miscValue2, miscValue3, refe, this);

			Guild guild = Global.GuildMgr.GetGuildById(GetGuildId());

			if (guild)
				guild.UpdateCriteria(type, miscValue1, miscValue2, miscValue3, refe, this);
		}

		public void CompletedAchievement(AchievementRecord entry)
		{
			_achievementSys.CompletedAchievement(entry, this);
		}

		public bool ModifierTreeSatisfied(uint modifierTreeId)
		{
			return _achievementSys.ModifierTreeSatisfied(modifierTreeId);
		}
	}
}