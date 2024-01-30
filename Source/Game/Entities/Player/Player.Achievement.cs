// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Achievements;
using Game.DataStorage;
using Game.Guilds;
using Game.Scenarios;
using System;
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

        public void StartCriteria(CriteriaStartEvent startEvent, uint entry, TimeSpan timeLost = default)
        {
            m_achievementSys.StartCriteria(startEvent, entry, timeLost);
        }

        public void FailCriteria(CriteriaFailEvent failEvent, uint failAsset)
        {
            m_achievementSys.FailCriteria(failEvent, failAsset);
            m_questObjectiveCriteriaMgr.FailCriteria(failEvent, failAsset);
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
            if (guild != null)
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
