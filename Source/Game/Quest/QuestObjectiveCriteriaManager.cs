﻿/*
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
using Framework.Database;
using Game.Achievements;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;
using System.Collections.Generic;

namespace Game
{
    class QuestObjectiveCriteriaManager : CriteriaHandler
    {
        public QuestObjectiveCriteriaManager(Player owner)
        {
            _owner = owner;
        }

        public void CheckAllQuestObjectiveCriteria(Player referencePlayer)
        {
            // suppress sending packets
            for (CriteriaTypes i = 0; i < CriteriaTypes.TotalTypes; ++i)
                UpdateCriteria(i, 0, 0, 0, null, referencePlayer);
        }

        public override void Reset()
        {
            foreach (var pair in _criteriaProgress)
                SendCriteriaProgressRemoved(pair.Key);

            _criteriaProgress.Clear();

            DeleteFromDB(_owner.GetGUID());

            // re-fill data
            CheckAllQuestObjectiveCriteria(_owner);
        }

        public static void DeleteFromDB(ObjectGuid guid)
        {
            var trans = new SQLTransaction();

            var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA);
            stmt.AddValue(0, guid.GetCounter());
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA_PROGRESS);
            stmt.AddValue(0, guid.GetCounter());
            trans.Append(stmt);

            DB.Characters.CommitTransaction(trans);
        }

        public void LoadFromDB(SQLResult objectiveResult, SQLResult criteriaResult)
        {
            if (!objectiveResult.IsEmpty())
            {
                do
                {
                    var objectiveId = objectiveResult.Read<uint>(0);

                    QuestObjective objective = Global.ObjectMgr.GetQuestObjective(objectiveId);
                    if (objective == null)
                        continue;

                    _completedObjectives.Add(objectiveId);

                } while (objectiveResult.NextRow());
            }

            if (!criteriaResult.IsEmpty())
            {
                var now = Time.UnixTime;
                do
                {
                    var criteriaId = criteriaResult.Read<uint>(0);
                    var counter = criteriaResult.Read<ulong>(1);
                    long date = criteriaResult.Read<uint>(2);

                    var criteria = Global.CriteriaMgr.GetCriteria(criteriaId);
                    if (criteria == null)
                    {
                        // Removing non-existing criteria data for all characters
                        Log.outError(LogFilter.Player, $"Non-existing quest objective criteria {criteriaId} data has been removed from the table `character_queststatus_objectives_criteria_progress`.");

                        var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_INVALID_QUEST_PROGRESS_CRITERIA);
                        stmt.AddValue(0, criteriaId);
                        DB.Characters.Execute(stmt);

                        continue;
                    }

                    if (criteria.Entry.StartTimer != 0 && date + criteria.Entry.StartTimer < now)
                        continue;

                    var progress = new CriteriaProgress();
                    progress.Counter = counter;
                    progress.Date = date;
                    progress.Changed = false;

                    _criteriaProgress[criteriaId] = progress;
                } while (criteriaResult.NextRow());
            }
        }

        public void SaveToDB(SQLTransaction trans)
        {
            var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA);
            stmt.AddValue(0, _owner.GetGUID().GetCounter());
            trans.Append(stmt);

            if (!_completedObjectives.Empty())
            {
                foreach (var completedObjectiveId in _completedObjectives)
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA);
                    stmt.AddValue(0, _owner.GetGUID().GetCounter());
                    stmt.AddValue(1, completedObjectiveId);
                    trans.Append(stmt);
                }
            }

            if (!_criteriaProgress.Empty())
            {
                foreach (var pair in _criteriaProgress)
                {
                    if (!pair.Value.Changed)
                        continue;

                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA_PROGRESS_BY_CRITERIA);
                    stmt.AddValue(0, _owner.GetGUID().GetCounter());
                    stmt.AddValue(1, pair.Key);
                    trans.Append(stmt);

                    if (pair.Value.Counter != 0)
                    {
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA_PROGRESS);
                        stmt.AddValue(0, _owner.GetGUID().GetCounter());
                        stmt.AddValue(1, pair.Key);
                        stmt.AddValue(2, pair.Value.Counter);
                        stmt.AddValue(3, (uint)pair.Value.Date);
                        trans.Append(stmt);
                    }

                    pair.Value.Changed = false;
                }
            }
        }

        public void ResetCriteria(CriteriaCondition condition, uint failAsset, bool evenIfCriteriaComplete)
        {
            Log.outDebug(LogFilter.Player, $"QuestObjectiveCriteriaMgr.ResetCriteria({condition}, {failAsset}, {evenIfCriteriaComplete})");

            // disable for gamemasters with GM-mode enabled
            if (_owner.IsGameMaster())
                return;

            var playerCriteriaList = Global.CriteriaMgr.GetCriteriaByFailEvent(condition, (int)failAsset);
            foreach (var playerCriteria in playerCriteriaList)
            {
                var trees = Global.CriteriaMgr.GetCriteriaTreesByCriteria(playerCriteria.Id);
                var allComplete = true;
                foreach (var tree in trees)
                {
                    // don't update already completed criteria if not forced
                    if (!(IsCompletedCriteriaTree(tree) && !evenIfCriteriaComplete))
                    {
                        allComplete = false;
                        break;
                    }
                }

                if (allComplete)
                    continue;

                RemoveCriteriaProgress(playerCriteria);
            }
        }

        public void ResetCriteriaTree(uint criteriaTreeId)
        {
            var tree = Global.CriteriaMgr.GetCriteriaTree(criteriaTreeId);
            if (tree == null)
                return;

            CriteriaManager.WalkCriteriaTree(tree, criteriaTree =>
            {
                RemoveCriteriaProgress(criteriaTree.Criteria);
            });
        }

        public override void SendAllData(Player receiver)
        {
            foreach (var pair in _criteriaProgress)
            {
                var criteriaUpdate = new CriteriaUpdate();

                criteriaUpdate.CriteriaID = pair.Key;
                criteriaUpdate.Quantity = pair.Value.Counter;
                criteriaUpdate.PlayerGUID = _owner.GetGUID();
                criteriaUpdate.Flags = 0;

                criteriaUpdate.CurrentTime = pair.Value.Date;
                criteriaUpdate.CreationTime = 0;

                SendPacket(criteriaUpdate);
            }
        }

        void CompletedObjective(QuestObjective questObjective, Player referencePlayer)
        {
            if (HasCompletedObjective(questObjective))
                return;

            _owner.KillCreditCriteriaTreeObjective(questObjective);

            Log.outInfo(LogFilter.Player, $"QuestObjectiveCriteriaMgr.CompletedObjective({questObjective.Id}). {GetOwnerInfo()}");

            _completedObjectives.Add(questObjective.Id);
        }

        public bool HasCompletedObjective(QuestObjective questObjective)
        {
            return _completedObjectives.Contains(questObjective.Id);
        }

        public override void SendCriteriaUpdate(Criteria criteria, CriteriaProgress progress, uint timeElapsed, bool timedCompleted)
        {
            var criteriaUpdate = new CriteriaUpdate();

            criteriaUpdate.CriteriaID = criteria.Id;
            criteriaUpdate.Quantity = progress.Counter;
            criteriaUpdate.PlayerGUID = _owner.GetGUID();
            criteriaUpdate.Flags = 0;
            if (criteria.Entry.StartTimer != 0)
                criteriaUpdate.Flags = timedCompleted ? 1 : 0u; // 1 is for keeping the counter at 0 in client

            criteriaUpdate.CurrentTime = progress.Date;
            criteriaUpdate.ElapsedTime = timeElapsed;
            criteriaUpdate.CreationTime = 0;

            SendPacket(criteriaUpdate);
        }

        public override void SendCriteriaProgressRemoved(uint criteriaId)
        {
            var criteriaDeleted = new CriteriaDeleted();
            criteriaDeleted.CriteriaID = criteriaId;
            SendPacket(criteriaDeleted);
        }

        public override bool CanUpdateCriteriaTree(Criteria criteria, CriteriaTree tree, Player referencePlayer)
        {
            var objective = tree.QuestObjective;
            if (objective == null)
                return false;

            if (HasCompletedObjective(objective))
            {
                Log.outTrace(LogFilter.Player, $"QuestObjectiveCriteriaMgr.CanUpdateCriteriaTree: (Id: {criteria.Id} Type {criteria.Entry.Type} Quest Objective {objective.Id}) Objective already completed");
                return false;
            }

            if (_owner.GetQuestStatus(objective.QuestID) != QuestStatus.Incomplete)
            {
                Log.outTrace(LogFilter.Achievement, $"QuestObjectiveCriteriaMgr.CanUpdateCriteriaTree: (Id: {criteria.Id} Type {criteria.Entry.Type} Quest Objective {objective.Id}) Not on quest");
                return false;
            }

            Quest quest = Global.ObjectMgr.GetQuestTemplate(objective.QuestID);
            if (_owner.GetGroup() && _owner.GetGroup().IsRaidGroup() && !quest.IsAllowedInRaid(referencePlayer.GetMap().GetDifficultyID()))
            {
                Log.outTrace(LogFilter.Achievement, $"QuestObjectiveCriteriaMgr.CanUpdateCriteriaTree: (Id: {criteria.Id} Type {criteria.Entry.Type} Quest Objective {objective.Id}) Quest cannot be completed in raid group");
                return false;
            }

            return base.CanUpdateCriteriaTree(criteria, tree, referencePlayer);
        }

        public override bool CanCompleteCriteriaTree(CriteriaTree tree)
        {
            var objective = tree.QuestObjective;
            if (objective == null)
                return false;

            return base.CanCompleteCriteriaTree(tree);
        }

        public override void CompletedCriteriaTree(CriteriaTree tree, Player referencePlayer)
        {
            var objective = tree.QuestObjective;
            if (objective == null)
                return;

            CompletedObjective(objective, referencePlayer);
        }

        public override void SendPacket(ServerPacket data)
        {
            _owner.SendPacket(data);
        }

        public override string GetOwnerInfo()
        {
            return $"{_owner.GetGUID()} {_owner.GetName()}";
        }

        public override List<Criteria> GetCriteriaByType(CriteriaTypes type, uint asset)
        {
            return Global.CriteriaMgr.GetQuestObjectiveCriteriaByType(type);
        }


        Player _owner;
        List<uint> _completedObjectives = new List<uint>();
    }
}
