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
using Game.Achievements;
using Game.DataStorage;
using Game.Guilds;
using Game.Scenarios;
using System.Collections.Generic;

namespace Game.Entities
{
    public partial class Player
    {
        public void ResetAchievements()
        {
            m_achievementSys.Reset();
        }

        public void SendRespondInspectAchievements(Player player)
        {
            m_achievementSys.SendAchievementInfo(player);
        }

        public uint GetAchievementPoints()
        {
            return m_achievementSys.GetAchievementPoints();
        }

        public ICollection<uint> GetCompletedAchievementIds()
        {
            return m_achievementSys.GetCompletedAchievementIds();
        }
        
        public bool HasAchieved(uint achievementId)
        {
            return m_achievementSys.HasAchieved(achievementId);
        }
        public void StartCriteriaTimer(CriteriaStartEvent startEvent, uint entry, uint timeLost = 0)
        {
            m_achievementSys.StartCriteriaTimer(startEvent, entry, timeLost);
        }

        public void RemoveCriteriaTimer(CriteriaStartEvent startEvent, uint entry)
        {
            m_achievementSys.RemoveCriteriaTimer(startEvent, entry);
        }

        public void ResetCriteria(CriteriaFailEvent failEvent, uint failAsset, bool evenIfCriteriaComplete = false)
        {
            m_achievementSys.ResetCriteria(failEvent, failAsset, evenIfCriteriaComplete);
            m_questObjectiveCriteriaMgr.ResetCriteria(failEvent, failAsset, evenIfCriteriaComplete);
        }

        public void UpdateCriteria(CriteriaType type, ulong miscValue1 = 0, ulong miscValue2 = 0, ulong miscValue3 = 0, WorldObject refe = null)
        {
            m_achievementSys.UpdateCriteria(type, miscValue1, miscValue2, miscValue3, refe, this);
            m_questObjectiveCriteriaMgr.UpdateCriteria(type, miscValue1, miscValue2, miscValue3, refe, this);

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
            m_achievementSys.CompletedAchievement(entry, this);
        }

        public bool ModifierTreeSatisfied(uint modifierTreeId)
        {
            return m_achievementSys.ModifierTreeSatisfied(modifierTreeId);
        }
    }
}
