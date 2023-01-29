// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;

namespace Game.Achievements
{
    public class AchievementManager : CriteriaHandler
    {
        protected uint _achievementPoints;
        protected Dictionary<uint, CompletedAchievementData> _completedAchievements = new();

        public Func<KeyValuePair<uint, CompletedAchievementData>, AchievementRecord> VisibleAchievementCheck = value =>
                                                                                                               {
                                                                                                                   AchievementRecord achievement = CliDB.AchievementStorage.LookupByKey(value.Key);

                                                                                                                   if (achievement != null &&
                                                                                                                       !achievement.Flags.HasAnyFlag(AchievementFlags.Hidden))
                                                                                                                       return achievement;

                                                                                                                   return null;
                                                                                                               };

        /// <summary>
        ///  called at player login. The player might have fulfilled some achievements when the Achievement system wasn't working yet
        /// </summary>
        /// <param Name="referencePlayer"></param>
        public void CheckAllAchievementCriteria(Player referencePlayer)
        {
            // suppress sending packets
            for (CriteriaType i = 0; i < CriteriaType.Count; ++i)
                UpdateCriteria(i, 0, 0, 0, null, referencePlayer);
        }

        public bool HasAchieved(uint achievementId)
        {
            return _completedAchievements.ContainsKey(achievementId);
        }

        public uint GetAchievementPoints()
        {
            return _achievementPoints;
        }

        public ICollection<uint> GetCompletedAchievementIds()
        {
            return _completedAchievements.Keys;
        }

        public override bool CanUpdateCriteriaTree(Criteria criteria, CriteriaTree tree, Player referencePlayer)
        {
            AchievementRecord achievement = tree.Achievement;

            if (achievement == null)
                return false;

            if (HasAchieved(achievement.Id))
            {
                Log.outTrace(LogFilter.Achievement,
                             "CanUpdateCriteriaTree: (Id: {0} Type {1} Achievement {2}) Achievement already earned",
                             criteria.Id,
                             criteria.Entry.Type,
                             achievement.Id);

                return false;
            }

            if (achievement.InstanceID != -1 &&
                referencePlayer.GetMapId() != achievement.InstanceID)
            {
                Log.outTrace(LogFilter.Achievement,
                             "CanUpdateCriteriaTree: (Id: {0} Type {1} Achievement {2}) Wrong map",
                             criteria.Id,
                             criteria.Entry.Type,
                             achievement.Id);

                return false;
            }

            if ((achievement.Faction == AchievementFaction.Horde && referencePlayer.GetTeam() != Team.Horde) ||
                (achievement.Faction == AchievementFaction.Alliance && referencePlayer.GetTeam() != Team.Alliance))
            {
                Log.outTrace(LogFilter.Achievement,
                             "CanUpdateCriteriaTree: (Id: {0} Type {1} Achievement {2}) Wrong faction",
                             criteria.Id,
                             criteria.Entry.Type,
                             achievement.Id);

                return false;
            }

            // Don't update realm first achievements if the player's account isn't allowed to do so
            if (achievement.Flags.HasAnyFlag(AchievementFlags.RealmFirstReach | AchievementFlags.RealmFirstKill))
                if (referencePlayer.GetSession().HasPermission(RBACPermissions.CannotEarnRealmFirstAchievements))
                    return false;

            if (achievement.CovenantID != 0 &&
                referencePlayer.PlayerData.CovenantID != achievement.CovenantID)
            {
                Log.outTrace(LogFilter.Achievement, $"CanUpdateCriteriaTree: (Id: {criteria.Id} Type {criteria.Entry.Type} Achievement {achievement.Id}) Wrong covenant");

                return false;
            }

            return base.CanUpdateCriteriaTree(criteria, tree, referencePlayer);
        }

        public override bool CanCompleteCriteriaTree(CriteriaTree tree)
        {
            AchievementRecord achievement = tree.Achievement;

            if (achievement == null)
                return false;

            // counter can never complete
            if (achievement.Flags.HasAnyFlag(AchievementFlags.Counter))
                return false;

            if (achievement.Flags.HasAnyFlag(AchievementFlags.RealmFirstReach | AchievementFlags.RealmFirstKill))
                // someone on this realm has already completed that Achievement
                if (Global.AchievementMgr.IsRealmCompleted(achievement))
                    return false;

            return true;
        }

        public override void CompletedCriteriaTree(CriteriaTree tree, Player referencePlayer)
        {
            AchievementRecord achievement = tree.Achievement;

            if (achievement == null)
                return;

            // counter can never complete
            if (achievement.Flags.HasAnyFlag(AchievementFlags.Counter))
                return;

            // already completed and stored
            if (HasAchieved(achievement.Id))
                return;

            if (IsCompletedAchievement(achievement))
                CompletedAchievement(achievement, referencePlayer);
        }

        public override void AfterCriteriaTreeUpdate(CriteriaTree tree, Player referencePlayer)
        {
            AchievementRecord achievement = tree.Achievement;

            if (achievement == null)
                return;

            // check again the completeness for SUMM and REQ COUNT achievements,
            // as they don't depend on the completed criteria but on the sum of the progress of each individual criteria
            if (achievement.Flags.HasAnyFlag(AchievementFlags.Summ))
                if (IsCompletedAchievement(achievement))
                    CompletedAchievement(achievement, referencePlayer);

            var achRefList = Global.AchievementMgr.GetAchievementByReferencedId(achievement.Id);

            foreach (AchievementRecord refAchievement in achRefList)
                if (IsCompletedAchievement(refAchievement))
                    CompletedAchievement(refAchievement, referencePlayer);
        }

        private bool IsCompletedAchievement(AchievementRecord entry)
        {
            // counter can never complete
            if (entry.Flags.HasAnyFlag(AchievementFlags.Counter))
                return false;

            CriteriaTree tree = Global.CriteriaMgr.GetCriteriaTree(entry.CriteriaTree);

            if (tree == null)
                return false;

            // For SUMM achievements, we have to Count the progress of each criteria of the Achievement.
            // Oddly, the Target Count is NOT contained in the Achievement, but in each individual criteria
            if (entry.Flags.HasAnyFlag(AchievementFlags.Summ))
            {
                long progress = 0;

                CriteriaManager.WalkCriteriaTree(tree,
                                                 criteriaTree =>
                                                 {
                                                     if (criteriaTree.Criteria != null)
                                                     {
                                                         CriteriaProgress criteriaProgress = GetCriteriaProgress(criteriaTree.Criteria);

                                                         if (criteriaProgress != null)
                                                             progress += (long)criteriaProgress.Counter;
                                                     }
                                                 });

                return progress >= tree.Entry.Amount;
            }

            return IsCompletedCriteriaTree(tree);
        }

        public override bool RequiredAchievementSatisfied(uint achievementId)
        {
            return HasAchieved(achievementId);
        }

        public virtual void CompletedAchievement(AchievementRecord entry, Player referencePlayer)
        {
        }
    }
}