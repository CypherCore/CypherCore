// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
