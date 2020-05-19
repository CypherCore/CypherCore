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
using Framework.Database;
using Game.Arenas;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Garrisons;
using Game.Maps;
using Game.Network;
using Game.Network.Packets;
using Game.Scenarios;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Game.Achievements
{
    public class CriteriaHandler
    {
        public virtual void Reset()
        {
            foreach (var iter in _criteriaProgress)
                SendCriteriaProgressRemoved(iter.Key);

            _criteriaProgress.Clear();
        }

        /// <summary>
        /// this function will be called whenever the user might have done a criteria relevant action
        /// </summary>
        /// <param name="type"></param>
        /// <param name="miscValue1"></param>
        /// <param name="miscValue2"></param>
        /// <param name="miscValue3"></param>
        /// <param name="unit"></param>
        /// <param name="referencePlayer"></param>
        public void UpdateCriteria(CriteriaTypes type, ulong miscValue1 = 0, ulong miscValue2 = 0, ulong miscValue3 = 0, Unit unit = null, Player referencePlayer = null)
        {
            if (type >= CriteriaTypes.TotalTypes)
            {
                Log.outDebug(LogFilter.Achievement, "UpdateCriteria: Wrong criteria type {0}", type);
                return;
            }

            if (!referencePlayer)
            {
                Log.outDebug(LogFilter.Achievement, "UpdateCriteria: Player is NULL! Cant update criteria");
                return;
            }

            // disable for gamemasters with GM-mode enabled
            if (referencePlayer.IsGameMaster())
            {
                Log.outDebug(LogFilter.Achievement, "UpdateCriteria: [Player {0} GM mode on] {1}, {2} ({3}), {4}, {5}, {6}", referencePlayer.GetName(), GetOwnerInfo(), type, type, miscValue1, miscValue2, miscValue3);
                return;
            }

            Log.outDebug(LogFilter.Achievement, "UpdateCriteria({0}, {1}, {2}, {3}) {4}", type, type, miscValue1, miscValue2, miscValue3, GetOwnerInfo());

            List<Criteria> criteriaList = GetCriteriaByType(type, (uint)miscValue1);
            foreach (Criteria criteria in criteriaList)
            {
                List<CriteriaTree> trees = Global.CriteriaMgr.GetCriteriaTreesByCriteria(criteria.Id);
                if (!CanUpdateCriteria(criteria, trees, miscValue1, miscValue2, miscValue3, unit, referencePlayer))
                    continue;

                // requirements not found in the dbc
                CriteriaDataSet data = Global.CriteriaMgr.GetCriteriaDataSet(criteria);
                if (data != null)
                    if (!data.Meets(referencePlayer, unit, (uint)miscValue1))
                        continue;

                switch (type)
                {
                    // std. case: increment at 1
                    case CriteriaTypes.WinBg:
                    case CriteriaTypes.NumberOfTalentResets:
                    case CriteriaTypes.LoseDuel:
                    case CriteriaTypes.CreateAuction:
                    case CriteriaTypes.WonAuctions:    //FIXME: for online player only currently
                    case CriteriaTypes.RollNeed:
                    case CriteriaTypes.RollGreed:
                    case CriteriaTypes.QuestAbandoned:
                    case CriteriaTypes.FlightPathsTaken:
                    case CriteriaTypes.AcceptedSummonings:
                    case CriteriaTypes.LootEpicItem:
                    case CriteriaTypes.ReceiveEpicItem:
                    case CriteriaTypes.Death:
                    case CriteriaTypes.CompleteDailyQuest:
                    case CriteriaTypes.CompleteBattleground:
                    case CriteriaTypes.DeathAtMap:
                    case CriteriaTypes.DeathInDungeon:
                    case CriteriaTypes.KilledByCreature:
                    case CriteriaTypes.KilledByPlayer:
                    case CriteriaTypes.DeathsFrom:
                    case CriteriaTypes.BeSpellTarget:
                    case CriteriaTypes.BeSpellTarget2:
                    case CriteriaTypes.CastSpell:
                    case CriteriaTypes.CastSpell2:
                    case CriteriaTypes.WinRatedArena:
                    case CriteriaTypes.UseItem:
                    case CriteriaTypes.RollNeedOnLoot:
                    case CriteriaTypes.RollGreedOnLoot:
                    case CriteriaTypes.DoEmote:
                    case CriteriaTypes.UseGameobject:
                    case CriteriaTypes.FishInGameobject:
                    case CriteriaTypes.WinDuel:
                    case CriteriaTypes.HkClass:
                    case CriteriaTypes.HkRace:
                    case CriteriaTypes.BgObjectiveCapture:
                    case CriteriaTypes.HonorableKill:
                    case CriteriaTypes.SpecialPvpKill:
                    case CriteriaTypes.GetKillingBlows:
                    case CriteriaTypes.HonorableKillAtArea:
                    case CriteriaTypes.WinArena: // This also behaves like ACHIEVEMENT_CRITERIA_TYPE_WIN_RATED_ARENA
                    case CriteriaTypes.OnLogin:
                    case CriteriaTypes.PlaceGarrisonBuilding:
                    case CriteriaTypes.OwnBattlePetCount:
                    case CriteriaTypes.HonorLevelReached:
                    case CriteriaTypes.PrestigeReached:
                        SetCriteriaProgress(criteria, 1, referencePlayer, ProgressType.Accumulate);
                        break;
                    // std case: increment at miscValue1
                    case CriteriaTypes.MoneyFromVendors:
                    case CriteriaTypes.GoldSpentForTalents:
                    case CriteriaTypes.MoneyFromQuestReward:
                    case CriteriaTypes.GoldSpentForTravelling:
                    case CriteriaTypes.GoldSpentAtBarber:
                    case CriteriaTypes.GoldSpentForMail:
                    case CriteriaTypes.LootMoney:
                    case CriteriaTypes.GoldEarnedByAuctions: //FIXME: for online player only currently
                    case CriteriaTypes.TotalDamageReceived:
                    case CriteriaTypes.TotalHealingReceived:
                    case CriteriaTypes.UseLfdToGroupWithPlayers:
                    case CriteriaTypes.DamageDone:
                    case CriteriaTypes.HealingDone:
                    case CriteriaTypes.HeartOfAzerothArtifactPowerEarned:
                        SetCriteriaProgress(criteria, miscValue1, referencePlayer, ProgressType.Accumulate);
                        break;
                    case CriteriaTypes.KillCreature:
                    case CriteriaTypes.KillCreatureType:
                    case CriteriaTypes.LootType:
                    case CriteriaTypes.OwnItem:
                    case CriteriaTypes.LootItem:
                    case CriteriaTypes.Currency:
                        SetCriteriaProgress(criteria, miscValue2, referencePlayer, ProgressType.Accumulate);
                        break;
                    // std case: high value at miscValue1
                    case CriteriaTypes.HighestAuctionBid:
                    case CriteriaTypes.HighestAuctionSold: //FIXME: for online player only currently
                    case CriteriaTypes.HighestHitDealt:
                    case CriteriaTypes.HighestHitReceived:
                    case CriteriaTypes.HighestHealCasted:
                    case CriteriaTypes.HighestHealingReceived:
                    case CriteriaTypes.HeartOfAzerothLevelReached:
                        SetCriteriaProgress(criteria, miscValue1, referencePlayer, ProgressType.Highest);
                        break;
                    case CriteriaTypes.ReachLevel:
                        SetCriteriaProgress(criteria, referencePlayer.GetLevel(), referencePlayer);
                        break;
                    case CriteriaTypes.ReachSkillLevel:
                        uint skillvalue = referencePlayer.GetBaseSkillValue((SkillType)criteria.Entry.Asset);
                        if (skillvalue != 0)
                            SetCriteriaProgress(criteria, skillvalue, referencePlayer);
                        break;
                    case CriteriaTypes.LearnSkillLevel:
                        uint maxSkillvalue = referencePlayer.GetPureMaxSkillValue((SkillType)criteria.Entry.Asset);
                        if (maxSkillvalue != 0)
                            SetCriteriaProgress(criteria, maxSkillvalue, referencePlayer);
                        break;
                    case CriteriaTypes.CompleteQuestCount:
                        SetCriteriaProgress(criteria, (uint)referencePlayer.GetRewardedQuestCount(), referencePlayer);
                        break;
                    case CriteriaTypes.CompleteDailyQuestDaily:
                        {
                            long nextDailyResetTime = Global.WorldMgr.GetNextDailyQuestsResetTime();
                            CriteriaProgress progress = GetCriteriaProgress(criteria);

                            if (miscValue1 == 0) // Login case.
                            {
                                // reset if player missed one day.
                                if (progress != null && progress.Date < (nextDailyResetTime - 2 * Time.Day))
                                    SetCriteriaProgress(criteria, 0, referencePlayer);
                                continue;
                            }

                            ProgressType progressType;
                            if (progress == null)
                                // 1st time. Start count.
                                progressType = ProgressType.Set;
                            else if (progress.Date < (nextDailyResetTime - 2 * Time.Day))
                                // last progress is older than 2 days. Player missed 1 day => Restart count.
                                progressType = ProgressType.Set;
                            else if (progress.Date < (nextDailyResetTime - Time.Day))
                                // last progress is between 1 and 2 days. => 1st time of the day.
                                progressType = ProgressType.Accumulate;
                            else
                                // last progress is within the day before the reset => Already counted today.
                                continue;

                            SetCriteriaProgress(criteria, 1, referencePlayer, progressType);
                            break;
                        }
                    case CriteriaTypes.CompleteQuestsInZone:
                        {
                            if (miscValue1 != 0)
                            {
                                SetCriteriaProgress(criteria, 1, referencePlayer, ProgressType.Accumulate);
                            }
                            else // login case
                            {
                                uint counter = 0;

                                var rewQuests = referencePlayer.GetRewardedQuests();
                                foreach (var id in rewQuests)
                                {
                                    Quest quest = Global.ObjectMgr.GetQuestTemplate(id);
                                    if (quest != null && quest.QuestSortID >= 0 && quest.QuestSortID == criteria.Entry.Asset)
                                        ++counter;
                                }
                                SetCriteriaProgress(criteria, counter, referencePlayer);
                            }
                            break;
                        }
                    case CriteriaTypes.FallWithoutDying:
                        // miscValue1 is the ingame fallheight*100 as stored in dbc
                        SetCriteriaProgress(criteria, miscValue1, referencePlayer);
                        break;
                    case CriteriaTypes.CompleteQuest:
                    case CriteriaTypes.LearnSpell:
                    case CriteriaTypes.ExploreArea:
                    case CriteriaTypes.VisitBarberShop:
                    case CriteriaTypes.EquipEpicItem:
                    case CriteriaTypes.EquipItem:
                    case CriteriaTypes.CompleteAchievement:
                    case CriteriaTypes.RecruitGarrisonFollower:
                    case CriteriaTypes.OwnBattlePet:
                        SetCriteriaProgress(criteria, 1, referencePlayer);
                        break;
                    case CriteriaTypes.BuyBankSlot:
                        SetCriteriaProgress(criteria, referencePlayer.GetBankBagSlotCount(), referencePlayer);
                        break;
                    case CriteriaTypes.GainReputation:
                        {
                            int reputation = referencePlayer.GetReputationMgr().GetReputation(criteria.Entry.Asset);
                            if (reputation > 0)
                                SetCriteriaProgress(criteria, (uint)reputation, referencePlayer);
                            break;
                        }
                    case CriteriaTypes.GainExaltedReputation:
                        SetCriteriaProgress(criteria, referencePlayer.GetReputationMgr().GetExaltedFactionCount(), referencePlayer);
                        break;
                    case CriteriaTypes.LearnSkilllineSpells:
                    case CriteriaTypes.LearnSkillLine:
                        {
                            uint spellCount = 0;
                            foreach (var spell in referencePlayer.GetSpellMap())
                            {
                                var bounds = Global.SpellMgr.GetSkillLineAbilityMapBounds(spell.Key);
                                foreach (var skill in bounds)
                                {
                                    if (skill.SkillLine == criteria.Entry.Asset)
                                    {
                                        // do not add couter twice if by any chance skill is listed twice in dbc (eg. skill 777 and spell 22717)
                                        ++spellCount;
                                        break;
                                    }
                                }
                            }
                            SetCriteriaProgress(criteria, spellCount, referencePlayer);
                            break;
                        }
                    case CriteriaTypes.GainReveredReputation:
                        SetCriteriaProgress(criteria, referencePlayer.GetReputationMgr().GetReveredFactionCount(), referencePlayer);
                        break;
                    case CriteriaTypes.GainHonoredReputation:
                        SetCriteriaProgress(criteria, referencePlayer.GetReputationMgr().GetHonoredFactionCount(), referencePlayer);
                        break;
                    case CriteriaTypes.KnownFactions:
                        SetCriteriaProgress(criteria, referencePlayer.GetReputationMgr().GetVisibleFactionCount(), referencePlayer);
                        break;
                    case CriteriaTypes.EarnHonorableKill:
                        SetCriteriaProgress(criteria, referencePlayer.m_activePlayerData.LifetimeHonorableKills, referencePlayer);
                        break;
                    case CriteriaTypes.HighestGoldValueOwned:
                        SetCriteriaProgress(criteria, referencePlayer.GetMoney(), referencePlayer, ProgressType.Highest);
                        break;
                    case CriteriaTypes.EarnAchievementPoints:
                        if (miscValue1 == 0)
                            continue;
                        SetCriteriaProgress(criteria, miscValue1, referencePlayer, ProgressType.Accumulate);
                        break;
                    case CriteriaTypes.HighestPersonalRating:
                        {
                            uint reqTeamType = criteria.Entry.Asset;

                            if (miscValue1 != 0)
                            {
                                if (miscValue2 != reqTeamType)
                                    continue;

                                SetCriteriaProgress(criteria, miscValue1, referencePlayer, ProgressType.Highest);
                            }
                            else // login case
                            {

                                for (byte arena_slot = 0; arena_slot < SharedConst.MaxArenaSlot; ++arena_slot)
                                {
                                    uint teamId = referencePlayer.GetArenaTeamId(arena_slot);
                                    if (teamId == 0)
                                        continue;

                                    ArenaTeam team = Global.ArenaTeamMgr.GetArenaTeamById(teamId);
                                    if (team == null || team.GetArenaType() != reqTeamType)
                                        continue;

                                    ArenaTeamMember member = team.GetMember(referencePlayer.GetGUID());
                                    if (member != null)
                                    {
                                        SetCriteriaProgress(criteria, member.PersonalRating, referencePlayer, ProgressType.Highest);
                                        break;
                                    }
                                }
                            }
                            break;
                        }
                    case CriteriaTypes.ReachGuildLevel:
                        SetCriteriaProgress(criteria, miscValue1, referencePlayer);
                        break;
                    case CriteriaTypes.TransmogSetUnlocked:
                        if (miscValue1 != criteria.Entry.Asset)
                            continue;
                        SetCriteriaProgress(criteria, 1, referencePlayer, ProgressType.Accumulate);
                        break;
                    case CriteriaTypes.AppearanceUnlockedBySlot:
                        if (miscValue2 == 0 /*login case*/ || miscValue1 != criteria.Entry.Asset)
                            continue;
                        SetCriteriaProgress(criteria, 1, referencePlayer, ProgressType.Accumulate);
                        break;
                    // FIXME: not triggered in code as result, need to implement
                    case CriteriaTypes.CompleteRaid:
                    case CriteriaTypes.PlayArena:
                    case CriteriaTypes.HighestTeamRating:
                    case CriteriaTypes.OwnRank:
                    case CriteriaTypes.SpentGoldGuildRepairs:
                    case CriteriaTypes.CraftItemsGuild:
                    case CriteriaTypes.CatchFromPool:
                    case CriteriaTypes.BuyGuildBankSlots:
                    case CriteriaTypes.EarnGuildAchievementPoints:
                    case CriteriaTypes.WinRatedBattleground:
                    case CriteriaTypes.ReachBgRating:
                    case CriteriaTypes.BuyGuildTabard:
                    case CriteriaTypes.CompleteQuestsGuild:
                    case CriteriaTypes.HonorableKillsGuild:
                    case CriteriaTypes.KillCreatureTypeGuild:
                    case CriteriaTypes.CompleteArchaeologyProjects:
                    case CriteriaTypes.CompleteGuildChallengeType:
                    case CriteriaTypes.CompleteGuildChallenge:
                    case CriteriaTypes.LfrDungeonsCompleted:
                    case CriteriaTypes.LfrLeaves:
                    case CriteriaTypes.LfrVoteKicksInitiatedByPlayer:
                    case CriteriaTypes.LfrVoteKicksNotInitByPlayer:
                    case CriteriaTypes.BeKickedFromLfr:
                    case CriteriaTypes.CountOfLfrQueueBoostsByTank:
                    case CriteriaTypes.CompleteScenarioCount:
                    case CriteriaTypes.CompleteScenario:
                    case CriteriaTypes.CaptureBattlePet:
                    case CriteriaTypes.WinPetBattle:
                    case CriteriaTypes.LevelBattlePet:
                    case CriteriaTypes.CaptureBattlePetCredit:
                    case CriteriaTypes.LevelBattlePetCredit:
                    case CriteriaTypes.EnterArea:
                    case CriteriaTypes.LeaveArea:
                    case CriteriaTypes.CompleteDungeonEncounter:
                    case CriteriaTypes.UpgradeGarrisonBuilding:
                    case CriteriaTypes.ConstructGarrisonBuilding:
                    case CriteriaTypes.UpgradeGarrison:
                    case CriteriaTypes.StartGarrisonMission:
                    case CriteriaTypes.CompleteGarrisonMissionCount:
                    case CriteriaTypes.CompleteGarrisonMission:
                    case CriteriaTypes.RecruitGarrisonFollowerCount:
                    case CriteriaTypes.LearnGarrisonBlueprintCount:
                    case CriteriaTypes.CompleteGarrisonShipment:
                    case CriteriaTypes.RaiseGarrisonFollowerItemLevel:
                    case CriteriaTypes.RaiseGarrisonFollowerLevel:
                    case CriteriaTypes.OwnToy:
                    case CriteriaTypes.OwnToyCount:
                    case CriteriaTypes.OwnHeirlooms:
                    case CriteriaTypes.SurveyGameobject:
                    case CriteriaTypes.ClearDigsite:
                    case CriteriaTypes.ManualCompleteCriteria:
                    case CriteriaTypes.CompleteChallengeModeGuild:
                    case CriteriaTypes.DefeatCreatureGroup:
                    case CriteriaTypes.CompleteChallengeMode:
                    case CriteriaTypes.SendEvent:
                    case CriteriaTypes.CookRecipesGuild:
                    case CriteriaTypes.EarnPetBattleAchievementPoints:
                    case CriteriaTypes.SendEventScenario:
                    case CriteriaTypes.ReleaseSpirit:
                    case CriteriaTypes.OwnPet:
                    case CriteriaTypes.GarrisonCompleteDungeonEncounter:
                    case CriteriaTypes.CompleteLfgDungeon:
                    case CriteriaTypes.LfgVoteKicksInitiatedByPlayer:
                    case CriteriaTypes.LfgVoteKicksNotInitByPlayer:
                    case CriteriaTypes.BeKickedFromLfg:
                    case CriteriaTypes.LfgLeaves:
                    case CriteriaTypes.CountOfLfgQueueBoostsByTank:
                    case CriteriaTypes.ReachAreatriggerWithActionset:
                    case CriteriaTypes.StartOrderHallMission:
                    case CriteriaTypes.RecruitGarrisonFollowerWithQuality:
                    case CriteriaTypes.ArtifactPowerEarned:
                    case CriteriaTypes.ArtifactTraitsUnlocked:
                    case CriteriaTypes.OrderHallTalentLearned:
                    case CriteriaTypes.OrderHallRecruitTroop:
                    case CriteriaTypes.CompleteWorldQuest:
                    case CriteriaTypes.GainParagonReputation:
                    case CriteriaTypes.EarnHonorXp:
                    case CriteriaTypes.RelicTalentUnlocked:
                    case CriteriaTypes.ReachAccountHonorLevel:
                        break;                                   // Not implemented yet :(
                }

                foreach (CriteriaTree tree in trees)
                {
                    if (IsCompletedCriteriaTree(tree))
                        CompletedCriteriaTree(tree, referencePlayer);

                    AfterCriteriaTreeUpdate(tree, referencePlayer);
                }
            }
        }

        public void UpdateTimedCriteria(uint timeDiff)
        {
            if (!_timeCriteriaTrees.Empty())
            {
                foreach (var key in _timeCriteriaTrees.Keys.ToList())
                {
                    var value = _timeCriteriaTrees[key];
                    // Time is up, remove timer and reset progress
                    if (value <= timeDiff)
                    {
                        CriteriaTree criteriaTree = Global.CriteriaMgr.GetCriteriaTree(key);
                        if (criteriaTree.Criteria != null)
                            RemoveCriteriaProgress(criteriaTree.Criteria);

                        _timeCriteriaTrees.Remove(key);
                    }
                    else
                    {
                        _timeCriteriaTrees[key] -= timeDiff;
                    }
                }
            }
        }

        public void StartCriteriaTimer(CriteriaTimedTypes type, uint entry, uint timeLost = 0)
        {
            List<Criteria> criteriaList = Global.CriteriaMgr.GetTimedCriteriaByType(type);
            foreach (Criteria criteria in criteriaList)
            {
                if (criteria.Entry.StartAsset != entry)
                    continue;

                List<CriteriaTree> trees = Global.CriteriaMgr.GetCriteriaTreesByCriteria(criteria.Id);
                bool canStart = false;
                foreach (CriteriaTree tree in trees)
                {
                    if (!_timeCriteriaTrees.ContainsKey(tree.Id) && !IsCompletedCriteriaTree(tree))
                    {
                        // Start the timer
                        if (criteria.Entry.StartTimer * Time.InMilliseconds > timeLost)
                        {
                            _timeCriteriaTrees[tree.Id] = (uint)(criteria.Entry.StartTimer * Time.InMilliseconds - timeLost);
                            canStart = true;
                        }
                    }
                }

                if (!canStart)
                    continue;

                // and at client too
                SetCriteriaProgress(criteria, 0, null, ProgressType.Set);
            }
        }

        public void RemoveCriteriaTimer(CriteriaTimedTypes type, uint entry)
        {
            List<Criteria> criteriaList = Global.CriteriaMgr.GetTimedCriteriaByType(type);
            foreach (Criteria criteria in criteriaList)
            {
                if (criteria.Entry.StartAsset != entry)
                    continue;

                List<CriteriaTree> trees = Global.CriteriaMgr.GetCriteriaTreesByCriteria(criteria.Id);
                // Remove the timer from all trees
                foreach (CriteriaTree tree in trees)
                    _timeCriteriaTrees.Remove(tree.Id);

                // remove progress
                RemoveCriteriaProgress(criteria);
            }
        }

        public CriteriaProgress GetCriteriaProgress(Criteria entry)
        {
            return _criteriaProgress.LookupByKey(entry.Id);
        }

        public void SetCriteriaProgress(Criteria criteria, ulong changeValue, Player referencePlayer, ProgressType progressType = ProgressType.Set)
        {
            // Don't allow to cheat - doing timed criteria without timer active
            List<CriteriaTree> trees = null;
            if (criteria.Entry.StartTimer != 0)
            {
                trees = Global.CriteriaMgr.GetCriteriaTreesByCriteria(criteria.Id);
                if (trees.Empty())
                    return;

                bool hasTreeForTimed = false;
                foreach (CriteriaTree tree in trees)
                {
                    var timedIter = _timeCriteriaTrees.LookupByKey(tree.Id);
                    if (timedIter != 0)
                    {
                        hasTreeForTimed = true;
                        break;
                    }
                }

                if (!hasTreeForTimed)
                    return;
            }

            Log.outDebug(LogFilter.Achievement, "SetCriteriaProgress({0}, {1}) for {2}", criteria.Id, changeValue, GetOwnerInfo());

            CriteriaProgress progress = GetCriteriaProgress(criteria);
            if (progress == null)
            {
                // not create record for 0 counter but allow it for timed criteria
                // we will need to send 0 progress to client to start the timer
                if (changeValue == 0 && criteria.Entry.StartTimer == 0)
                    return;

                progress = new CriteriaProgress();
                progress.Counter = changeValue;

            }
            else
            {
                ulong newValue = 0;
                switch (progressType)
                {
                    case ProgressType.Set:
                        newValue = changeValue;
                        break;
                    case ProgressType.Accumulate:
                        {
                            // avoid overflow
                            ulong max_value = ulong.MaxValue;
                            newValue = max_value - progress.Counter > changeValue ? progress.Counter + changeValue : max_value;
                            break;
                        }
                    case ProgressType.Highest:
                        newValue = progress.Counter < changeValue ? changeValue : progress.Counter;
                        break;
                }

                // not update (not mark as changed) if counter will have same value
                if (progress.Counter == newValue && criteria.Entry.StartTimer == 0)
                    return;

                progress.Counter = newValue;
            }

            progress.Changed = true;
            progress.Date = Time.UnixTime; // set the date to the latest update.
            progress.PlayerGUID = referencePlayer ? referencePlayer.GetGUID() : ObjectGuid.Empty;
            _criteriaProgress[criteria.Id] = progress;

            uint timeElapsed = 0;
            if (criteria.Entry.StartTimer != 0)
            {
                Cypher.Assert(trees != null);

                foreach (CriteriaTree tree in trees)
                {
                    var timedIter = _timeCriteriaTrees.LookupByKey(tree.Id);
                    if (timedIter != 0)
                    {
                        // Client expects this in packet
                        timeElapsed = criteria.Entry.StartTimer - (timedIter / Time.InMilliseconds);

                        // Remove the timer, we wont need it anymore
                        if (IsCompletedCriteriaTree(tree))
                            _timeCriteriaTrees.Remove(tree.Id);
                    }
                }
            }

            SendCriteriaUpdate(criteria, progress, timeElapsed, true);
        }

        public void RemoveCriteriaProgress(Criteria criteria)
        {
            if (criteria == null)
                return;

            if (!_criteriaProgress.ContainsKey(criteria.Id))
                return;

            SendCriteriaProgressRemoved(criteria.Id);

            _criteriaProgress.Remove(criteria.Id);
        }

        public bool IsCompletedCriteriaTree(CriteriaTree tree)
        {
            if (!CanCompleteCriteriaTree(tree))
                return false;

            ulong requiredCount = tree.Entry.Amount;
            switch ((CriteriaTreeOperator)tree.Entry.Operator)
            {
                case CriteriaTreeOperator.Single:
                    return tree.Criteria != null && IsCompletedCriteria(tree.Criteria, requiredCount);
                case CriteriaTreeOperator.SinglerNotCompleted:
                    return tree.Criteria == null || !IsCompletedCriteria(tree.Criteria, requiredCount);
                case CriteriaTreeOperator.All:
                    foreach (CriteriaTree node in tree.Children)
                        if (!IsCompletedCriteriaTree(node))
                            return false;
                    return true;
                case CriteriaTreeOperator.SumChildren:
                    {
                        ulong progress = 0;
                        CriteriaManager.WalkCriteriaTree(tree, criteriaTree =>
                        {
                            if (criteriaTree.Criteria != null)
                            {
                                CriteriaProgress criteriaProgress = GetCriteriaProgress(criteriaTree.Criteria);
                                if (criteriaProgress != null)
                                    progress += criteriaProgress.Counter;
                            }
                        });
                        return progress >= requiredCount;
                    }
                case CriteriaTreeOperator.MaxChild:
                    {
                        ulong progress = 0;
                        CriteriaManager.WalkCriteriaTree(tree, criteriaTree =>
                        {
                            if (criteriaTree.Criteria != null)
                            {
                                CriteriaProgress criteriaProgress = GetCriteriaProgress(criteriaTree.Criteria);
                                if (criteriaProgress != null)
                                    if (criteriaProgress.Counter > progress)
                                        progress = criteriaProgress.Counter;
                            }
                        });
                        return progress >= requiredCount;
                    }
                case CriteriaTreeOperator.CountDirectChildren:
                    {
                        ulong progress = 0;
                        foreach (CriteriaTree node in tree.Children)
                        {
                            if (node.Criteria != null)
                            {
                                CriteriaProgress criteriaProgress = GetCriteriaProgress(node.Criteria);
                                if (criteriaProgress != null)
                                    if (criteriaProgress.Counter >= 1)
                                        if (++progress >= requiredCount)
                                            return true;
                            }
                        }

                        return false;
                    }
                case CriteriaTreeOperator.Any:
                    {
                        ulong progress = 0;
                        foreach (CriteriaTree node in tree.Children)
                            if (IsCompletedCriteriaTree(node))
                                if (++progress >= requiredCount)
                                    return true;

                        return false;
                    }
                case CriteriaTreeOperator.SumChildrenWeight:
                    {
                        ulong progress = 0;
                        CriteriaManager.WalkCriteriaTree(tree, criteriaTree =>
                        {
                            if (criteriaTree.Criteria != null)
                            {
                                CriteriaProgress criteriaProgress = GetCriteriaProgress(criteriaTree.Criteria);
                                if (criteriaProgress != null)
                                    progress += criteriaProgress.Counter * criteriaTree.Entry.Amount;
                            }
                        });
                        return progress >= requiredCount;
                    }
                default:
                    break;
            }

            return false;
        }

        public virtual bool CanUpdateCriteriaTree(Criteria criteria, CriteriaTree tree, Player referencePlayer)
        {
            if ((tree.Entry.Flags.HasAnyFlag(CriteriaTreeFlags.HordeOnly) && referencePlayer.GetTeam() != Team.Horde) ||
                (tree.Entry.Flags.HasAnyFlag(CriteriaTreeFlags.AllianceOnly) && referencePlayer.GetTeam() != Team.Alliance))
            {
                Log.outTrace(LogFilter.Achievement, "CriteriaHandler.CanUpdateCriteriaTree: (Id: {0} Type {1} CriteriaTree {2}) Wrong faction",
                    criteria.Id, criteria.Entry.Type, tree.Entry.Id);
                return false;
            }

            return true;
        }

        public virtual bool CanCompleteCriteriaTree(CriteriaTree tree)
        {
            return true;
        }

        bool IsCompletedCriteria(Criteria criteria, ulong requiredAmount)
        {
            CriteriaProgress progress = GetCriteriaProgress(criteria);
            if (progress == null)
                return false;

            switch (criteria.Entry.Type)
            {
                case CriteriaTypes.WinBg:
                case CriteriaTypes.KillCreature:
                case CriteriaTypes.ReachLevel:
                case CriteriaTypes.ReachGuildLevel:
                case CriteriaTypes.ReachSkillLevel:
                case CriteriaTypes.CompleteQuestCount:
                case CriteriaTypes.CompleteDailyQuestDaily:
                case CriteriaTypes.CompleteQuestsInZone:
                case CriteriaTypes.DamageDone:
                case CriteriaTypes.HealingDone:
                case CriteriaTypes.CompleteDailyQuest:
                case CriteriaTypes.FallWithoutDying:
                case CriteriaTypes.BeSpellTarget:
                case CriteriaTypes.BeSpellTarget2:
                case CriteriaTypes.CastSpell:
                case CriteriaTypes.CastSpell2:
                case CriteriaTypes.BgObjectiveCapture:
                case CriteriaTypes.HonorableKillAtArea:
                case CriteriaTypes.HonorableKill:
                case CriteriaTypes.EarnHonorableKill:
                case CriteriaTypes.OwnItem:
                case CriteriaTypes.WinRatedArena:
                case CriteriaTypes.HighestPersonalRating:
                case CriteriaTypes.UseItem:
                case CriteriaTypes.LootItem:
                case CriteriaTypes.BuyBankSlot:
                case CriteriaTypes.GainReputation:
                case CriteriaTypes.GainExaltedReputation:
                case CriteriaTypes.VisitBarberShop:
                case CriteriaTypes.EquipEpicItem:
                case CriteriaTypes.RollNeedOnLoot:
                case CriteriaTypes.RollGreedOnLoot:
                case CriteriaTypes.HkClass:
                case CriteriaTypes.HkRace:
                case CriteriaTypes.DoEmote:
                case CriteriaTypes.EquipItem:
                case CriteriaTypes.MoneyFromQuestReward:
                case CriteriaTypes.LootMoney:
                case CriteriaTypes.UseGameobject:
                case CriteriaTypes.SpecialPvpKill:
                case CriteriaTypes.FishInGameobject:
                case CriteriaTypes.LearnSkilllineSpells:
                case CriteriaTypes.LearnSkillLine:
                case CriteriaTypes.WinDuel:
                case CriteriaTypes.LootType:
                case CriteriaTypes.UseLfdToGroupWithPlayers:
                case CriteriaTypes.GetKillingBlows:
                case CriteriaTypes.Currency:
                case CriteriaTypes.PlaceGarrisonBuilding:
                case CriteriaTypes.OwnBattlePetCount:
                case CriteriaTypes.AppearanceUnlockedBySlot:
                case CriteriaTypes.GainParagonReputation:
                case CriteriaTypes.EarnHonorXp:
                case CriteriaTypes.RelicTalentUnlocked:
                case CriteriaTypes.ReachAccountHonorLevel:
                case CriteriaTypes.HeartOfAzerothArtifactPowerEarned:
                case CriteriaTypes.HeartOfAzerothLevelReached:
                    return progress.Counter >= requiredAmount;
                case CriteriaTypes.CompleteAchievement:
                case CriteriaTypes.CompleteQuest:
                case CriteriaTypes.LearnSpell:
                case CriteriaTypes.ExploreArea:
                case CriteriaTypes.RecruitGarrisonFollower:
                case CriteriaTypes.OwnBattlePet:
                case CriteriaTypes.HonorLevelReached:
                case CriteriaTypes.PrestigeReached:
                case CriteriaTypes.TransmogSetUnlocked:
                    return progress.Counter >= 1;
                case CriteriaTypes.LearnSkillLevel:
                    return progress.Counter >= (requiredAmount * 75);
                case CriteriaTypes.EarnAchievementPoints:
                    return progress.Counter >= 9000;
                case CriteriaTypes.WinArena:
                    return requiredAmount != 0 && progress.Counter >= requiredAmount;
                case CriteriaTypes.OnLogin:
                    return true;
                // handle all statistic-only criteria here
                default:
                    break;
            }

            return false;
        }

        bool CanUpdateCriteria(Criteria criteria, List<CriteriaTree> trees, ulong miscValue1, ulong miscValue2, ulong miscValue3, Unit unit, Player referencePlayer)
        {
            if (Global.DisableMgr.IsDisabledFor(DisableType.Criteria, criteria.Id, null))
            {
                Log.outError(LogFilter.Achievement, "CanUpdateCriteria: (Id: {0} Type {1}) Disabled", criteria.Id, criteria.Entry.Type);
                return false;
            }

            bool treeRequirementPassed = false;
            foreach (CriteriaTree tree in trees)
            {
                if (!CanUpdateCriteriaTree(criteria, tree, referencePlayer))
                    continue;

                treeRequirementPassed = true;
                break;
            }

            if (!treeRequirementPassed)
                return false;

            if (!RequirementsSatisfied(criteria, miscValue1, miscValue2, miscValue3, unit, referencePlayer))
            {
                Log.outTrace(LogFilter.Achievement, "CanUpdateCriteria: (Id: {0} Type {1}) Requirements not satisfied", criteria.Id, criteria.Entry.Type);
                return false;
            }

            if (criteria.Modifier != null && !ModifierTreeSatisfied(criteria.Modifier, miscValue1, miscValue2, unit, referencePlayer))
            {
                Log.outTrace(LogFilter.Achievement, "CanUpdateCriteria: (Id: {0} Type {1}) Requirements have not been satisfied", criteria.Id, criteria.Entry.Type);
                return false;
            }

            if (!ConditionsSatisfied(criteria, referencePlayer))
            {
                Log.outTrace(LogFilter.Achievement, "CanUpdateCriteria: (Id: {0} Type {1}) Conditions have not been satisfied", criteria.Id, criteria.Entry.Type);
                return false;
            }

            return true;
        }

        bool ConditionsSatisfied(Criteria criteria, Player referencePlayer)
        {
            if (criteria.Entry.FailEvent == 0)
                return true;

            switch ((CriteriaCondition)criteria.Entry.FailEvent)
            {
                case CriteriaCondition.BgMap:
                    if (!referencePlayer.InBattleground())
                        return false;
                    break;
                case CriteriaCondition.NotInGroup:
                    if (referencePlayer.GetGroup())
                        return false;
                    break;
                default:
                    break;
            }

            return true;
        }

        bool RequirementsSatisfied(Criteria criteria, ulong miscValue1, ulong miscValue2, ulong miscValue3, Unit unit, Player referencePlayer)
        {
            switch (criteria.Entry.Type)
            {
                case CriteriaTypes.AcceptedSummonings:
                case CriteriaTypes.CompleteDailyQuest:
                case CriteriaTypes.CreateAuction:
                case CriteriaTypes.FallWithoutDying:
                case CriteriaTypes.FlightPathsTaken:
                case CriteriaTypes.GetKillingBlows:
                case CriteriaTypes.GoldEarnedByAuctions:
                case CriteriaTypes.GoldSpentAtBarber:
                case CriteriaTypes.GoldSpentForMail:
                case CriteriaTypes.GoldSpentForTalents:
                case CriteriaTypes.GoldSpentForTravelling:
                case CriteriaTypes.HighestAuctionBid:
                case CriteriaTypes.HighestAuctionSold:
                case CriteriaTypes.HighestHealingReceived:
                case CriteriaTypes.HighestHealCasted:
                case CriteriaTypes.HighestHitDealt:
                case CriteriaTypes.HighestHitReceived:
                case CriteriaTypes.HonorableKill:
                case CriteriaTypes.LootMoney:
                case CriteriaTypes.LoseDuel:
                case CriteriaTypes.MoneyFromQuestReward:
                case CriteriaTypes.MoneyFromVendors:
                case CriteriaTypes.NumberOfTalentResets:
                case CriteriaTypes.QuestAbandoned:
                case CriteriaTypes.ReachGuildLevel:
                case CriteriaTypes.RollGreed:
                case CriteriaTypes.RollNeed:
                case CriteriaTypes.SpecialPvpKill:
                case CriteriaTypes.TotalDamageReceived:
                case CriteriaTypes.TotalHealingReceived:
                case CriteriaTypes.UseLfdToGroupWithPlayers:
                case CriteriaTypes.VisitBarberShop:
                case CriteriaTypes.WinDuel:
                case CriteriaTypes.WinRatedArena:
                case CriteriaTypes.WonAuctions:
                    if (miscValue1 == 0)
                        return false;
                    break;
                case CriteriaTypes.BuyBankSlot:
                case CriteriaTypes.CompleteDailyQuestDaily:
                case CriteriaTypes.CompleteQuestCount:
                case CriteriaTypes.EarnAchievementPoints:
                case CriteriaTypes.GainExaltedReputation:
                case CriteriaTypes.GainHonoredReputation:
                case CriteriaTypes.GainReveredReputation:
                case CriteriaTypes.HighestGoldValueOwned:
                case CriteriaTypes.HighestPersonalRating:
                case CriteriaTypes.KnownFactions:
                case CriteriaTypes.ReachLevel:
                case CriteriaTypes.OnLogin:
                    break;
                case CriteriaTypes.CompleteAchievement:
                    if (!RequiredAchievementSatisfied(criteria.Entry.Asset))
                        return false;
                    break;
                case CriteriaTypes.WinBg:
                case CriteriaTypes.CompleteBattleground:
                case CriteriaTypes.DeathAtMap:
                    if (miscValue1 == 0 || criteria.Entry.Asset != referencePlayer.GetMapId())
                        return false;
                    break;
                case CriteriaTypes.KillCreature:
                case CriteriaTypes.KilledByCreature:
                    if (miscValue1 == 0 || criteria.Entry.Asset != miscValue1)
                        return false;
                    break;
                case CriteriaTypes.ReachSkillLevel:
                case CriteriaTypes.LearnSkillLevel:
                    // update at loading or specific skill update
                    if (miscValue1 != 0 && miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaTypes.CompleteQuestsInZone:
                    if (miscValue1 != 0)
                    {
                        Quest quest = Global.ObjectMgr.GetQuestTemplate((uint)miscValue1);
                        if (quest == null || quest.QuestSortID != criteria.Entry.Asset)
                            return false;
                    }
                    break;
                case CriteriaTypes.Death:
                    {
                        if (miscValue1 == 0)
                            return false;
                        break;
                    }
                case CriteriaTypes.DeathInDungeon:
                    {
                        if (miscValue1 == 0)
                            return false;

                        Map map = referencePlayer.IsInWorld ? referencePlayer.GetMap() : Global.MapMgr.FindMap(referencePlayer.GetMapId(), referencePlayer.GetInstanceId());
                        if (!map || !map.IsDungeon())
                            return false;

                        //FIXME: work only for instances where max == min for players
                        if (map.ToInstanceMap().GetMaxPlayers() != criteria.Entry.Asset)
                            return false;
                        break;
                    }
                case CriteriaTypes.KilledByPlayer:
                    if (miscValue1 == 0 || !unit || !unit.IsTypeId(TypeId.Player))
                        return false;
                    break;
                case CriteriaTypes.DeathsFrom:
                    if (miscValue1 == 0 || miscValue2 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaTypes.CompleteQuest:
                    {
                        // if miscValues != 0, it contains the questID.
                        if (miscValue1 != 0)
                        {
                            if (miscValue1 != criteria.Entry.Asset)
                                return false;
                        }
                        else
                        {
                            // login case.
                            if (!referencePlayer.GetQuestRewardStatus(criteria.Entry.Asset))
                                return false;
                        }
                        CriteriaDataSet data = Global.CriteriaMgr.GetCriteriaDataSet(criteria);
                        if (data != null)
                            if (!data.Meets(referencePlayer, unit))
                                return false;
                        break;
                    }
                case CriteriaTypes.BeSpellTarget:
                case CriteriaTypes.BeSpellTarget2:
                case CriteriaTypes.CastSpell:
                case CriteriaTypes.CastSpell2:
                    if (miscValue1 == 0 || miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaTypes.LearnSpell:
                    if (miscValue1 != 0 && miscValue1 != criteria.Entry.Asset)
                        return false;

                    if (!referencePlayer.HasSpell(criteria.Entry.Asset))
                        return false;
                    break;
                case CriteriaTypes.LootType:
                    // miscValue1 = itemId - miscValue2 = count of item loot
                    // miscValue3 = loot_type (note: 0 = LOOT_CORPSE and then it ignored)
                    if (miscValue1 == 0 || miscValue2 == 0 || miscValue3 == 0 || miscValue3 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaTypes.OwnItem:
                    if (miscValue1 != 0 && criteria.Entry.Asset != miscValue1)
                        return false;
                    break;
                case CriteriaTypes.UseItem:
                case CriteriaTypes.LootItem:
                case CriteriaTypes.EquipItem:
                    if (miscValue1 == 0 || criteria.Entry.Asset != miscValue1)
                        return false;
                    break;
                case CriteriaTypes.ExploreArea:
                    {
                        WorldMapOverlayRecord worldOverlayEntry = CliDB.WorldMapOverlayStorage.LookupByKey(criteria.Entry.Asset);
                        if (worldOverlayEntry == null)
                            break;

                        bool matchFound = false;
                        for (int j = 0; j < SharedConst.MaxWorldMapOverlayArea; ++j)
                        {
                            AreaTableRecord area = CliDB.AreaTableStorage.LookupByKey(worldOverlayEntry.AreaID[j]);
                            if (area == null)
                                break;

                            if (area.AreaBit < 0)
                                continue;

                            int playerIndexOffset = (int)((uint)area.AreaBit / 64);
                            if (playerIndexOffset >= PlayerConst.ExploredZonesSize)
                                continue;

                            ulong mask = 1ul << (int)((uint)area.AreaBit % 64);
                            if (Convert.ToBoolean(referencePlayer.m_activePlayerData.ExploredZones[playerIndexOffset] & mask))
                            {
                                matchFound = true;
                                break;
                            }
                        }

                        if (!matchFound)
                            return false;
                        break;
                    }
                case CriteriaTypes.GainReputation:
                    if (miscValue1 != 0 && miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaTypes.EquipEpicItem:
                    // miscValue1 = itemSlot miscValue2 = itemid
                    if (miscValue2 == 0 || miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaTypes.RollNeedOnLoot:
                case CriteriaTypes.RollGreedOnLoot:
                    {
                        // miscValue1 = itemid miscValue2 = diced value
                        if (miscValue1 == 0 || miscValue2 != criteria.Entry.Asset)
                            return false;

                        ItemTemplate proto = Global.ObjectMgr.GetItemTemplate((uint)miscValue1);
                        if (proto == null)
                            return false;
                        break;
                    }
                case CriteriaTypes.DoEmote:
                    if (miscValue1 == 0 || miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaTypes.DamageDone:
                case CriteriaTypes.HealingDone:
                    if (miscValue1 == 0)
                        return false;

                    if (criteria.Entry.FailEvent == (uint)CriteriaCondition.BgMap)
                    {
                        if (!referencePlayer.InBattleground())
                            return false;

                        // map specific case (BG in fact) expected player targeted damage/heal
                        if (!unit || !unit.IsTypeId(TypeId.Player))
                            return false;
                    }
                    break;
                case CriteriaTypes.UseGameobject:
                case CriteriaTypes.FishInGameobject:
                    if (miscValue1 == 0 || miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaTypes.LearnSkilllineSpells:
                case CriteriaTypes.LearnSkillLine:
                    if (miscValue1 != 0 && miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaTypes.LootEpicItem:
                case CriteriaTypes.ReceiveEpicItem:
                    {
                        if (miscValue1 == 0)
                            return false;
                        ItemTemplate proto = Global.ObjectMgr.GetItemTemplate((uint)miscValue1);
                        if (proto == null || proto.GetQuality() < ItemQuality.Epic)
                            return false;
                        break;
                    }
                case CriteriaTypes.HkClass:
                    if (miscValue1 == 0 || miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaTypes.HkRace:
                    if (miscValue1 == 0 || miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaTypes.BgObjectiveCapture:
                    if (miscValue1 == 0 || miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaTypes.HonorableKillAtArea:
                    if (miscValue1 == 0 || miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaTypes.Currency:
                    if (miscValue1 == 0 || miscValue2 == 0 || (long)miscValue2 < 0
                        || miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaTypes.WinArena:
                    if (miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaTypes.HighestTeamRating:
                    return false;
                case CriteriaTypes.PlaceGarrisonBuilding:
                    if (miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                default:
                    break;
            }
            return true;
        }

        public bool ModifierTreeSatisfied(ModifierTreeNode tree, ulong miscValue1, ulong miscValue2, Unit unit, Player referencePlayer)
        {
            switch ((ModifierTreeOperator)tree.Entry.Operator)
            {
                case ModifierTreeOperator.SingleTrue:
                    return tree.Entry.Type != 0 && ModifierSatisfied(tree.Entry, miscValue1, miscValue2, unit, referencePlayer);
                case ModifierTreeOperator.SingleFalse:
                    return tree.Entry.Type != 0 && !ModifierSatisfied(tree.Entry, miscValue1, miscValue2, unit, referencePlayer);
                case ModifierTreeOperator.All:
                    foreach (ModifierTreeNode node in tree.Children)
                        if (!ModifierTreeSatisfied(node, miscValue1, miscValue2, unit, referencePlayer))
                            return false;
                    return true;
                case ModifierTreeOperator.Some:
                    {
                        sbyte requiredAmount = Math.Max(tree.Entry.Amount, (sbyte)1);
                        foreach (ModifierTreeNode node in tree.Children)
                            if (ModifierTreeSatisfied(node, miscValue1, miscValue2, unit, referencePlayer))
                                if (--requiredAmount == 0)
                                    return true;

                        return false;
                    }
                default:
                    break;
            }

            return false;
        }

        bool ModifierSatisfied(ModifierTreeRecord modifier, ulong miscValue1, ulong miscValue2, Unit unit, Player referencePlayer)
        {
            uint reqValue = modifier.Asset;
        int secondaryAsset = modifier.SecondaryAsset;
        int tertiaryAsset = modifier.TertiaryAsset;

            switch ((CriteriaAdditionalCondition)modifier.Type)
            {
                case CriteriaAdditionalCondition.SourceDrunkValue: // 1
                    {
                        uint inebriation = (uint)Math.Min(Math.Max(referencePlayer.GetDrunkValue(), referencePlayer.m_playerData.FakeInebriation), 100);
                        if (inebriation < reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.SourcePlayerCondition: // 2
                    {
                        PlayerConditionRecord playerCondition = CliDB.PlayerConditionStorage.LookupByKey(reqValue);
                        if (playerCondition == null || !ConditionManager.IsPlayerMeetingCondition(referencePlayer, playerCondition))
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.ItemLevel: // 3
                    {
                        // miscValue1 is itemid
                        ItemTemplate item = Global.ObjectMgr.GetItemTemplate((uint)miscValue1);
                        if (item == null || item.GetBaseItemLevel() < reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.TargetCreatureEntry: // 4
                    if (unit == null || unit.GetEntry() != reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.TargetMustBePlayer: // 5
                    if (unit == null || !unit.IsTypeId(TypeId.Player))
                        return false;
                    break;
                case CriteriaAdditionalCondition.TargetMustBeDead: // 6
                    if (unit == null || unit.IsAlive())
                        return false;
                    break;
                case CriteriaAdditionalCondition.TargetMustBeEnemy: // 7
                    if (unit == null || !referencePlayer.IsHostileTo(unit))
                        return false;
                    break;
                case CriteriaAdditionalCondition.SourceHasAura: // 8
                    if (!referencePlayer.HasAura(reqValue))
                        return false;
                    break;
                case CriteriaAdditionalCondition.SourceHasAuraType: // 9
                    if (!referencePlayer.HasAuraType((AuraType)reqValue))
                        return false;
                    break;
                case CriteriaAdditionalCondition.TargetHasAura: // 10
                    if (unit == null || !unit.HasAura(reqValue))
                        return false;
                    break;
                case CriteriaAdditionalCondition.TargetHasAuraType: // 11
                    if (unit == null || !unit.HasAuraType((AuraType)reqValue))
                        return false;
                    break;
                case CriteriaAdditionalCondition.SourceAuraState: // 12
                    if (!referencePlayer.HasAuraState((AuraStateType)reqValue))
                        return false;
                    break;
                case CriteriaAdditionalCondition.TargetAuraState: // 13
                    if (!unit || !unit.HasAuraState((AuraStateType)reqValue))
                        return false;
                    break;
                case CriteriaAdditionalCondition.ItemQualityMin: // 14
                    {
                        // miscValue1 is itemid
                        ItemTemplate item = Global.ObjectMgr.GetItemTemplate((uint)miscValue1);
                        if (item == null || (uint)item.GetQuality() < reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.ItemQualityEquals: // 15
                    {
                        // miscValue1 is itemid
                        ItemTemplate item = Global.ObjectMgr.GetItemTemplate((uint)miscValue1);
                        if (item == null || (uint)item.GetQuality() != reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.SourceIsAlive: // 16
                    if (referencePlayer.IsDead())
                        return false;
                    break;
                case CriteriaAdditionalCondition.SourceAreaOrZone: // 17
                    {
                        uint zoneId, areaId;
                        referencePlayer.GetZoneAndAreaId(out zoneId, out areaId);
                        if (zoneId != reqValue && areaId != reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.TargetAreaOrZone: // 18
                    {
                        if (unit == null)
                            return false;
                        uint zoneId, areaId;
                        unit.GetZoneAndAreaId(out zoneId, out areaId);
                        if (zoneId != reqValue && areaId != reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.MapDifficultyOld: // 20
                    {
                        DifficultyRecord difficulty = CliDB.DifficultyStorage.LookupByKey(referencePlayer.GetMap().GetDifficultyID());
                        if (difficulty == null || difficulty.OldEnumValue == -1 || difficulty.OldEnumValue != reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.SourceLevelAboveTarget: // 22
                    if (!unit || referencePlayer.GetLevel() + reqValue < unit.GetLevel())
                        return false;
                    break;
                case CriteriaAdditionalCondition.SourceLevelEqualTarget: // 23
                    if (!unit || referencePlayer.GetLevel() != unit.GetLevel())
                        return false;
                    break;
                case CriteriaAdditionalCondition.ArenaType: // 24
                    {
                        Battleground bg = referencePlayer.GetBattleground();
                        if (!bg || !bg.IsArena() || bg.GetArenaType() != (ArenaTypes)reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.SourceRace: // 25
                    if ((uint)referencePlayer.GetRace() != reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.SourceClass: // 26
                    if ((uint)referencePlayer.GetClass() != reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.TargetRace: // 27
                    if (unit == null || !unit.IsTypeId(TypeId.Player) || (uint)unit.GetRace() != reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.TargetClass: // 28
                    if (unit == null || !unit.IsTypeId(TypeId.Player) || (uint)unit.GetClass() != reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.MaxGroupMembers: // 29
                    if (referencePlayer.GetGroup() && referencePlayer.GetGroup().GetMembersCount() >= reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.TargetCreatureType: // 30
                    {
                        if (unit == null)
                            return false;

                        if (!unit.IsTypeId(TypeId.Unit) || (uint)unit.GetCreatureType() != reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.TargetCreatureFamily: // 31
                    {
                        if (!unit)
                            return false;
                        if (unit.GetTypeId() != TypeId.Unit || unit.ToCreature().GetCreatureTemplate().Family != (CreatureFamily)reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.SourceMap: // 32
                    if (referencePlayer.GetMapId() != reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.ClientVersion: // 33
                    if (reqValue < Global.RealmMgr.GetMinorMajorBugfixVersionForBuild(Global.WorldMgr.GetRealm().Build))
                        return false;
                    break;
                case CriteriaAdditionalCondition.BattlePetTeamLevel: // 34
                    foreach (BattlePetSlot slot in referencePlayer.GetSession().GetBattlePetMgr().GetSlots())
                        if (slot.Pet.Level != reqValue)
                            return false;
                    break;
                case CriteriaAdditionalCondition.NotInGroup: // 35
                    if (referencePlayer.GetGroup())
                        return false;
                    break;
                case CriteriaAdditionalCondition.InGroup: // 36
                    if (!referencePlayer.GetGroup())
                        return false;
                    break;
                case CriteriaAdditionalCondition.TitleBitIndex: // 38          // miscValue1 is title's bit index
                    if (miscValue1 != reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.SourceLevel: // 39
                    if (referencePlayer.GetLevel() != reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.TargetLevel: // 40
                    if (unit == null || unit.GetLevelForTarget(referencePlayer) != reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.SourceZone: // 41
                    {
                        uint zoneId = referencePlayer.GetAreaId();
                        AreaTableRecord areaEntry = CliDB.AreaTableStorage.LookupByKey(zoneId);
                        if (areaEntry != null)
                            if (areaEntry.Flags[0].HasAnyFlag(AreaFlags.Unk9))
                                zoneId = areaEntry.ParentAreaID;
                        if (zoneId != reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.TargetZone: // 42
                    {
                        if (!unit)
                            return false;
                        uint zoneId = unit.GetAreaId();
                        AreaTableRecord areaEntry = CliDB.AreaTableStorage.LookupByKey(zoneId);
                        if (areaEntry != null)
                            if (areaEntry.Flags[0].HasAnyFlag(AreaFlags.Unk9))
                                zoneId = areaEntry.ParentAreaID;
                        if (zoneId != reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.SourceHealthPctLower: // 43
                    if (referencePlayer.GetHealthPct() > (float)reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.SourceHealthPctGreater: // 44
                    if (referencePlayer.GetHealthPct() < (float)reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.SourceHealthPctEqual: // 45
                    if (referencePlayer.GetHealthPct() != (float)reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.TargetHealthPctLower: // 46
                    if (unit == null || unit.GetHealthPct() >= (float)reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.TargetHealthPctGreater: // 47
                    if (!unit || unit.GetHealthPct() < (float)reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.TargetHealthPctEqual: // 48
                    if (!unit || unit.GetHealthPct() != (float)reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.SourceHealthLower: // 49
                    if (referencePlayer.GetHealth() > reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.SourceHealthGreater: // 50
                    if (referencePlayer.GetHealth() < reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.SourceHealthEqual: // 51
                    if (referencePlayer.GetHealth() != reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.TargetHealthLower: // 52
                    if (!unit || unit.GetHealth() > reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.TargetHealthGreater: // 53
                    if (!unit || unit.GetHealth() < reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.TargetHealthEqual: // 54
                    if (!unit || unit.GetHealth() != reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.TargetPlayerCondition: // 55
                    {
                        if (unit == null || !unit.IsPlayer())
                            return false;

                        PlayerConditionRecord playerCondition = CliDB.PlayerConditionStorage.LookupByKey(reqValue);
                        if (playerCondition == null || !ConditionManager.IsPlayerMeetingCondition(unit.ToPlayer(), playerCondition))
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.MinAchievementPoints: // 56
                    if (referencePlayer.GetAchievementPoints() <= reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.InLfgDungeon: // 57
                    if (ConditionManager.GetPlayerConditionLfgValue(referencePlayer, PlayerConditionLfgStatus.InLFGDungeon) == 0)
                        return false;
                    break;
                case CriteriaAdditionalCondition.InLfgRandomDungeon: // 58
                    if (ConditionManager.GetPlayerConditionLfgValue(referencePlayer, PlayerConditionLfgStatus.InLFGRandomDungeon) == 0)
                        return false;
                    break;
                case CriteriaAdditionalCondition.InLfgFirstRandomDungeon: // 59
                    if (ConditionManager.GetPlayerConditionLfgValue(referencePlayer, PlayerConditionLfgStatus.InLFGFirstRandomDungeon) == 0)
                        return false;
                    break;
                case CriteriaAdditionalCondition.GuildReputation: // 62
                    if (referencePlayer.GetReputationMgr().GetReputation(1168) < reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.RatedBattlegroundRating: // 64
                    if (referencePlayer.GetRBGPersonalRating() < reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.WorldStateExpression: // 67
                    WorldStateExpressionRecord worldStateExpression = CliDB.WorldStateExpressionStorage.LookupByKey(reqValue);
                    if (worldStateExpression != null)
                        return ConditionManager.IsPlayerMeetingExpression(referencePlayer, worldStateExpression);
                    return false;
                case CriteriaAdditionalCondition.MapDifficulty: // 68
                    {
                        DifficultyRecord difficulty = CliDB.DifficultyStorage.LookupByKey(referencePlayer.GetMap().GetDifficultyID());
                        if (difficulty == null || difficulty.Id != reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.SourceLevelGreater: // 69
                    if (referencePlayer.GetLevel() < reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.TargetLevelGreater: // 70
                    if (!unit || unit.GetLevel() < reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.SourceLevelLower: // 71
                    if (referencePlayer.GetLevel() > reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.TargetLevelLower: // 72
                    if (!unit || unit.GetLevel() > reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.ModifierTree: // 73
                    ModifierTreeNode nextModifierTree = Global.CriteriaMgr.GetModifierTree(reqValue);
                    if (nextModifierTree != null)
                        return ModifierTreeSatisfied(nextModifierTree, miscValue1, miscValue2, unit, referencePlayer);
                    return false;
                case CriteriaAdditionalCondition.ScenarioId: // 74
                    {
                        Scenario scenario = referencePlayer.GetScenario();
                        if (scenario == null)
                            return false;

                        if (scenario.GetEntry().Id != reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.TheTillersReputation: // 75
                    if (referencePlayer.GetReputationMgr().GetReputation(1272) < reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.ScenarioStepIndex: // 82
                    {
                        Scenario scenario = referencePlayer.GetScenario();
                        if (scenario == null)
                            return false;

                        if (scenario.GetStep().OrderIndex != (reqValue - 1))
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.IsOnQuest: // 84
                    if (referencePlayer.FindQuestSlot(reqValue) == SharedConst.MaxQuestLogSize)
                        return false;
                    break;
                case CriteriaAdditionalCondition.ExaltedWithFaction: // 85
                    if (referencePlayer.GetReputationMgr().GetReputation(reqValue) < 42000)
                        return false;
                    break;
                case CriteriaAdditionalCondition.HasAchievement: // 86
                    if (!referencePlayer.HasAchieved(reqValue))
                        return false;
                    break;
                case CriteriaAdditionalCondition.CloudSerpentReputation: // 88
                    if (referencePlayer.GetReputationMgr().GetReputation(1271) < reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.BattlePetSpecies: // 91
                    if (miscValue1 != reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.ActiveExpansion: // 92
                    if ((int)referencePlayer.GetSession().GetExpansion() < reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.FactionStanding: // 95
                    if (referencePlayer.GetReputationMgr().GetReputation(reqValue) < reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.SourceSex: // 97
                    if ((int)referencePlayer.GetGender() != reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.SourceNativeSex: // 98
                    if (referencePlayer.m_playerData.NativeSex != reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.Skill: // 99
                    if (referencePlayer.GetPureSkillValue((SkillType)reqValue) < secondaryAsset)
                        return false;
                    break;
                case CriteriaAdditionalCondition.NormalPhaseShift: // 101
                    if (!PhasingHandler.InDbPhaseShift(referencePlayer, 0, 0, 0))
                        return false;
                    break;
                case CriteriaAdditionalCondition.InPhase: // 102
                    if (!PhasingHandler.InDbPhaseShift(referencePlayer, 0, (ushort)reqValue, 0))
                        return false;
                    break;
                case CriteriaAdditionalCondition.NotInPhase: // 103
                    if (PhasingHandler.InDbPhaseShift(referencePlayer, 0, (ushort)reqValue, 0))
                        return false;
                    break;
                case CriteriaAdditionalCondition.HasSpell: // 104
                    if (!referencePlayer.HasSpell(reqValue))
                        return false;
                    break;
                case CriteriaAdditionalCondition.ItemCount: // 105
                    if (referencePlayer.GetItemCount(reqValue, false) < secondaryAsset)
                        return false;
                    break;
                case CriteriaAdditionalCondition.AccountExpansion: // 106
                    if ((int)referencePlayer.GetSession().GetAccountExpansion() < reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.RewardedQuest: // 110
                    uint questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(reqValue);
                    if (questBit != 0)
                        if ((referencePlayer.m_activePlayerData.QuestCompleted[((int)questBit - 1) >> 6] & (1ul << (((int)questBit - 1) & 63))) == 0)
                        return false;
                    break;
                case CriteriaAdditionalCondition.CompletedQuest: // 111
                    if (referencePlayer.GetQuestStatus(reqValue) != QuestStatus.Complete)
                        return false;
                    break;
                case CriteriaAdditionalCondition.ExploredArea: // 113
                    {
                        AreaTableRecord areaTable = CliDB.AreaTableStorage.LookupByKey(reqValue);
                        if (areaTable == null)
                            return false;

                        if (areaTable.AreaBit <= 0)
                            break; // success

                        int playerIndexOffset = areaTable.AreaBit / 64;
                        if (playerIndexOffset >= PlayerConst.ExploredZonesSize)
                            break;

                        if ((referencePlayer.m_activePlayerData.ExploredZones[playerIndexOffset] & (1ul << (areaTable.AreaBit % 64))) == 0)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.ItemCountIncludingBank: // 114
                    if (referencePlayer.GetItemCount(reqValue, true) < secondaryAsset)
                        return false;
                    break;
                case CriteriaAdditionalCondition.SourcePvpFactionIndex: // 116
                    {
                        ChrRacesRecord race = CliDB.ChrRacesStorage.LookupByKey(referencePlayer.GetRace());
                        if (race == null)
                            return false;

                        FactionTemplateRecord faction = CliDB.FactionTemplateStorage.LookupByKey(race.FactionID);
                        if (faction == null)
                            return false;

                        int factionIndex = -1;
                        if (faction.FactionGroup.HasAnyFlag((byte)FactionMasks.Horde))
                            factionIndex = 0;
                        else if (faction.FactionGroup.HasAnyFlag((byte)FactionMasks.Alliance))
                            factionIndex = 1;
                        else if (faction.FactionGroup.HasAnyFlag((byte)FactionMasks.Player))
                            factionIndex = 0;
                        if (factionIndex != reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.LfgValueEqual: // 117
                    if (ConditionManager.GetPlayerConditionLfgValue(referencePlayer, (PlayerConditionLfgStatus)reqValue) != secondaryAsset)
                        return false;
                    break;
                case CriteriaAdditionalCondition.LfgValueGreater: // 118
                    if (ConditionManager.GetPlayerConditionLfgValue(referencePlayer, (PlayerConditionLfgStatus)reqValue) < secondaryAsset)
                        return false;
                    break;
                case CriteriaAdditionalCondition.CurrencyAmount: // 119
                    if (!referencePlayer.HasCurrency(reqValue, (uint)secondaryAsset))
                        return false;
                    break;
                case CriteriaAdditionalCondition.CurrencyTrackedAmount: // 121
                    if (referencePlayer.GetTrackedCurrencyCount(reqValue) < secondaryAsset)
                        return false;
                    break;
                case CriteriaAdditionalCondition.MapInstanceType: // 122
                    if ((uint)referencePlayer.GetMap().GetEntry().InstanceType != reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.Mentor: // 123
                    if (!referencePlayer.HasPlayerFlag(PlayerFlags.Mentor))
                        return false;
                    break;
                case CriteriaAdditionalCondition.GarrisonLevelAbove: // 126
                    {
                        Garrison garrison = referencePlayer.GetGarrison();
                        if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)secondaryAsset || garrison.GetSiteLevel().GarrLevel < reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.GarrisonFollowersAboveLevel: // 127
                    {
                        Garrison garrison = referencePlayer.GetGarrison();
                        if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                            return false;

                        uint followerCount = garrison.CountFollowers(follower =>
                        {
                            return follower.PacketInfo.FollowerLevel >= secondaryAsset;
                        });

                        if (followerCount < reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.GarrisonFollowersAboveQuality: // 128
                    {
                        Garrison garrison = referencePlayer.GetGarrison();
                        if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                            return false;

                        uint followerCount = garrison.CountFollowers(follower =>
                        {
                            return follower.PacketInfo.Quality >= secondaryAsset;
                        });

                        if (followerCount < reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.GarrisonFollowerAboveLevelWithAbility: // 129
                    {
                        Garrison garrison = referencePlayer.GetGarrison();
                        if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                            return false;

                        uint followerCount = garrison.CountFollowers(follower =>
                        {
                            return follower.PacketInfo.FollowerLevel >= reqValue && follower.HasAbility((uint)secondaryAsset);
                        });

                        if (followerCount < 1)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.GarrisonFollowerAboveLevelWithTrait: // 130
                    {
                        Garrison garrison = referencePlayer.GetGarrison();
                        if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                            return false;

                        GarrAbilityRecord traitEntry = CliDB.GarrAbilityStorage.LookupByKey(secondaryAsset);
                        if (traitEntry == null || !traitEntry.Flags.HasAnyFlag(GarrisonAbilityFlags.Trait))
                            return false;

                        uint followerCount = garrison.CountFollowers(follower =>
                        {
                            return follower.PacketInfo.FollowerLevel >= reqValue && follower.HasAbility((uint)secondaryAsset);
                        });

                        if (followerCount < 1)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.GarrisonFollowerWithAbilityInBuilding: // 131
                    {
                        Garrison garrison = referencePlayer.GetGarrison();
                        if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                            return false;

                        uint followerCount = garrison.CountFollowers(follower =>
                        {
                            GarrBuildingRecord followerBuilding = CliDB.GarrBuildingStorage.LookupByKey(follower.PacketInfo.CurrentBuildingID);
                            if (followerBuilding == null)
                                return false;

                            return followerBuilding.BuildingType == secondaryAsset && follower.HasAbility(reqValue); ;
                        });

                        if (followerCount < 1)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.GarrisonFollowerWithTraitInBuilding: // 132
                    {
                        Garrison garrison = referencePlayer.GetGarrison();
                        if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                            return false;

                        GarrAbilityRecord traitEntry = CliDB.GarrAbilityStorage.LookupByKey(reqValue);
                        if (traitEntry == null || !traitEntry.Flags.HasAnyFlag(GarrisonAbilityFlags.Trait))
                            return false;

                        uint followerCount = garrison.CountFollowers(follower =>
                        {
                            GarrBuildingRecord followerBuilding = CliDB.GarrBuildingStorage.LookupByKey(follower.PacketInfo.CurrentBuildingID);
                            if (followerBuilding == null)
                                return false;

                            return followerBuilding.BuildingType == secondaryAsset && follower.HasAbility(reqValue); ;
                        });

                        if (followerCount < 1)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.GarrisonFollowerAboveLevelInBuilding: // 133
                    {
                        Garrison garrison = referencePlayer.GetGarrison();
                        if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                            return false;

                        uint followerCount = garrison.CountFollowers(follower =>
                        {
                            if (follower.PacketInfo.FollowerLevel < reqValue)
                                return false;

                            GarrBuildingRecord followerBuilding = CliDB.GarrBuildingStorage.LookupByKey(follower.PacketInfo.CurrentBuildingID);
                            if (followerBuilding == null)
                                return false;

                            return followerBuilding.BuildingType == secondaryAsset;
                        });
                        if (followerCount < 1)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.GarrisonBuildingAboveLevel: // 134
                    {
                        Garrison garrison = referencePlayer.GetGarrison();
                        if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                            return false;

                        foreach (Garrison.Plot plot in garrison.GetPlots())
                        {
                            if (!plot.BuildingInfo.PacketInfo.HasValue)
                                continue;

                            GarrBuildingRecord building = CliDB.GarrBuildingStorage.LookupByKey(plot.BuildingInfo.PacketInfo.Value.GarrBuildingID);
                            if (building == null || building.UpgradeLevel < reqValue || building.BuildingType != secondaryAsset)
                                continue;

                            return true;
                        }
                        return false;
                    }
                case CriteriaAdditionalCondition.GarrisonBlueprint: // 135
                    {
                        GarrBuildingRecord blueprintBuilding = CliDB.GarrBuildingStorage.LookupByKey(reqValue);
                        if (blueprintBuilding == null)
                            return false;

                        Garrison garrison = referencePlayer.GetGarrison();
                        if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)blueprintBuilding.GarrTypeID)
                            return false;

                        if (!garrison.HasBlueprint(reqValue))
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.GarrisonBuildingInactive: // 140
                    {
                        GarrBuildingRecord building = CliDB.GarrBuildingStorage.LookupByKey(reqValue);
                        if (building == null)
                            return false;

                        Garrison garrison = referencePlayer.GetGarrison();
                        if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                            return false;

                        foreach (Garrison.Plot plot in garrison.GetPlots())
                        {
                            if (!plot.BuildingInfo.PacketInfo.HasValue || plot.BuildingInfo.PacketInfo.Value.GarrBuildingID != reqValue)
                                continue;

                            return !plot.BuildingInfo.PacketInfo.Value.Active;
                        }
                        return false;
                    }
                case CriteriaAdditionalCondition.GarrisonBuildingEqualLevel: // 142
                    {
                        Garrison garrison = referencePlayer.GetGarrison();
                        if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                            return false;

                        foreach (Garrison.Plot plot in garrison.GetPlots())
                        {
                            if (!plot.BuildingInfo.PacketInfo.HasValue)
                                continue;

                            GarrBuildingRecord building = CliDB.GarrBuildingStorage.LookupByKey(plot.BuildingInfo.PacketInfo.Value.GarrBuildingID);
                            if (building == null || building.UpgradeLevel != reqValue || building.BuildingType != secondaryAsset)
                                continue;

                            return true;
                        }
                        return false;
                    }
                case CriteriaAdditionalCondition.GarrisonFollowerWithAbility: // 143
                    {
                        Garrison garrison = referencePlayer.GetGarrison();
                        if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)secondaryAsset)
                            return false;

                        if (miscValue1 != 0)
                        {
                            Garrison.Follower follower = garrison.GetFollower(miscValue1);
                            if (follower == null)
                                return false;

                            if (!follower.HasAbility(reqValue))
                                return false;
                        }
                        else
                        {
                            uint followerCount = garrison.CountFollowers(follower =>
                            {
                                return follower.HasAbility(reqValue);
                            });

                            if (followerCount < 1)
                                return false;
                        }
                        break;
                    }
                case CriteriaAdditionalCondition.GarrisonFollowerWithTrait: // 144
                    {
                        GarrAbilityRecord traitEntry = CliDB.GarrAbilityStorage.LookupByKey(reqValue);
                        if (traitEntry == null || !traitEntry.Flags.HasAnyFlag(GarrisonAbilityFlags.Trait))
                            return false;

                        Garrison garrison = referencePlayer.GetGarrison();
                        if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)secondaryAsset)
                            return false;

                        if (miscValue1 != 0)
                        {
                            Garrison.Follower follower = garrison.GetFollower(miscValue1);
                            if (follower == null || !follower.HasAbility(reqValue))
                                return false;
                        }
                        else
                        {
                            uint followerCount = garrison.CountFollowers(follower =>
                            {
                                return follower.HasAbility(reqValue);
                            });

                            if (followerCount < 1)
                                return false;
                        }
                        break;
                    }
                case CriteriaAdditionalCondition.GarrisonFollowerAboveQualityWod: // 145
                    {
                        Garrison garrison = referencePlayer.GetGarrison();
                        if (garrison == null || garrison.GetGarrisonType() != GarrisonType.Garrison)
                            return false;

                        if (miscValue1 != 0)
                        {
                            Garrison.Follower follower = garrison.GetFollower(miscValue1);
                            if (follower == null || follower.PacketInfo.Quality < reqValue)
                                return false;
                        }
                        else
                        {
                            uint followerCount = garrison.CountFollowers(follower =>
                            {
                                return follower.PacketInfo.Quality >= reqValue;
                            });

                            if (followerCount < 1)
                                return false;
                        }
                        break;
                    }
                case CriteriaAdditionalCondition.GarrisonFollowerEqualLevel: // 146
                    {
                        Garrison garrison = referencePlayer.GetGarrison();
                        if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)secondaryAsset)
                            return false;

                        if (miscValue1 != 0)
                        {
                            Garrison.Follower follower = garrison.GetFollower(miscValue1);
                            if (follower == null || follower.PacketInfo.FollowerLevel < reqValue)
                                return false;
                        }
                        else
                        {
                            uint followerCount = garrison.CountFollowers(follower =>
                            {
                                return follower.PacketInfo.FollowerLevel >= reqValue;
                            });

                            if (followerCount < 1)
                                return false;
                        }
                        break;
                    }
                case CriteriaAdditionalCondition.BattlePetSpeciesInTeam: // 151
                    {
                        uint count = 0;
                        foreach (BattlePetSlot slot in referencePlayer.GetSession().GetBattlePetMgr().GetSlots())
                            if (slot.Pet.Species == secondaryAsset)
                                ++count;

                        if (count < reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.BattlePetFamilyInTeam: // 152
                    {
                        uint count = 0;
                        foreach (BattlePetSlot slot in referencePlayer.GetSession().GetBattlePetMgr().GetSlots())
                        {
                            BattlePetSpeciesRecord species = CliDB.BattlePetSpeciesStorage.LookupByKey(slot.Pet.Species);
                            if (species != null)
                                if (species.PetTypeEnum == secondaryAsset)
                                    ++count;
                        }

                        if (count < reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.GarrisonFollowerId: // 157
                    {
                        Garrison garrison = referencePlayer.GetGarrison();
                        if (garrison == null)
                            return false;

                        if (miscValue1 != 0)
                        {
                            Garrison.Follower follower = garrison.GetFollower(miscValue1);
                            if (follower == null || follower.PacketInfo.GarrFollowerID != reqValue)
                                return false;
                        }
                        else
                        {
                            uint followerCount = garrison.CountFollowers(follower =>
                            {
                                return follower.PacketInfo.GarrFollowerID == reqValue;
                            });

                            if (followerCount < 1)
                                return false;
                        }
                        break;
                    }
                case CriteriaAdditionalCondition.GarrisonFollowerAboveItemLevel: // 168
                    {
                        Garrison garrison = referencePlayer.GetGarrison();
                        if (garrison == null)
                            return false;

                        if (miscValue1 != 0)
                        {
                            Garrison.Follower follower = garrison.GetFollower(miscValue1);
                            if (follower == null || follower.PacketInfo.GarrFollowerID != reqValue)
                                return false;
                        }
                        else
                        {
                            uint followerCount = garrison.CountFollowers(follower =>
                            {
                                return follower.GetItemLevel() >= reqValue;
                            });

                            if (followerCount < 1)
                                return false;
                        }
                        break;
                    }
                case CriteriaAdditionalCondition.GarrisonFollowersAboveItemLevel: // 169
                    {
                        Garrison garrison = referencePlayer.GetGarrison();
                        if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                            return false;

                        uint followerCount = garrison.CountFollowers(follower =>
                        {
                            return follower.GetItemLevel() >= secondaryAsset;
                        });

                        if (followerCount < reqValue)
                            return false;

                        break;
                    }


                case CriteriaAdditionalCondition.GarrisonLevelEqual: // 170
                    {
                        Garrison garrison = referencePlayer.GetGarrison();
                        if (garrison == null || garrison.GetGarrisonType() != GarrisonType.Garrison || garrison.GetSiteLevel().GarrLevel != reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.TargetingCorpse: // 173
                    if (referencePlayer.GetTarget().GetHigh() != HighGuid.Corpse)
                        return false;
                    break;
                case CriteriaAdditionalCondition.GarrisonFollowersLevelEqual: // 175
                    {
                        Garrison garrison = referencePlayer.GetGarrison();
                        if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                            return false;

                        uint followerCount = garrison.CountFollowers(follower =>
                        {
                            return follower.PacketInfo.FollowerLevel >= secondaryAsset;
                        });

                        if (followerCount < reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.GarrisonFollowerIdInBuilding: // 176
                    {
                        Garrison garrison = referencePlayer.GetGarrison();
                        if (garrison == null || garrison.GetGarrisonType() != GarrisonType.Garrison)
                            return false;

                        uint followerCount = garrison.CountFollowers(follower =>
                        {
                            if (follower.PacketInfo.GarrFollowerID != reqValue)
                                return false;

                            GarrBuildingRecord followerBuilding = CliDB.GarrBuildingStorage.LookupByKey(follower.PacketInfo.CurrentBuildingID);
                            if (followerBuilding == null)
                                return false;

                            return followerBuilding.BuildingType == secondaryAsset;
                        });

                        if (followerCount < 1)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.WorldPvpArea: // 179
                    {
                        BattleField bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(referencePlayer.GetZoneId());
                        if (bf == null || bf.GetBattleId() != reqValue)
                            return false;
                        break;
                    }

                case CriteriaAdditionalCondition.GarrisonFollowersItemLevelAbove: // 184
                    {
                        Garrison garrison = referencePlayer.GetGarrison();
                        if (garrison == null)
                            return false;

                        uint followerCount = garrison.CountFollowers(follower =>
                        {
                            GarrFollowerRecord garrFollower = CliDB.GarrFollowerStorage.LookupByKey(follower.PacketInfo.GarrFollowerID);
                            if (garrFollower == null)
                                return false;

                            return follower.GetItemLevel() >= secondaryAsset && garrFollower.GarrFollowerTypeID == tertiaryAsset;
                        });

                        if (followerCount < reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.HonorLevel: // 193
                    if (referencePlayer.GetHonorLevel() != reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.PrestigeLevel: // 194
                    return false;
                case CriteriaAdditionalCondition.ItemModifiedAppearance: // 200
                    {
                        var (PermAppearance, TempAppearance) = referencePlayer.GetSession().GetCollectionMgr().HasItemAppearance(reqValue);
                        if (!PermAppearance || TempAppearance)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.HasCharacterRestrictions: // 203
                    {
                        if (referencePlayer.m_activePlayerData.CharacterRestrictions.Empty())
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.QuestInfoId: // 206
                    {
                        Quest quest = Global.ObjectMgr.GetQuestTemplate((uint)miscValue1);
                        if (quest == null || quest.Id != reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.ArtifactAppearanceSetUsed: // 208
                    {
                        for (byte slot = EquipmentSlot.MainHand; slot <= EquipmentSlot.Ranged; ++slot)
                        {
                            Item artifact = referencePlayer.GetItemByPos(InventorySlots.Bag0, slot);
                            if (artifact != null)
                            {
                                ArtifactAppearanceRecord artifactAppearance = CliDB.ArtifactAppearanceStorage.LookupByKey(artifact.GetModifier(ItemModifier.ArtifactAppearanceId));
                                if (artifactAppearance != null)
                                    if (artifactAppearance.ArtifactAppearanceSetID == reqValue)
                                        return true;
                            }
                        }
                        return false;
                    }
                case CriteriaAdditionalCondition.CurrencyAmountEqual: // 209
                    if (referencePlayer.GetCurrency(reqValue) != secondaryAsset)
                        return false;
                    break;
                case CriteriaAdditionalCondition.ScenarioType: // 211
                    {
                        Scenario scenario = referencePlayer.GetScenario();
                        if (scenario == null)
                            return false;

                        if (scenario.GetEntry().Type != reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.AccountExpansionEqual: // 212
                    if ((uint)referencePlayer.GetSession().GetAccountExpansion() != reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.AchievementGloballyIncompleted: // 231
                    {
                        AchievementRecord achievement = CliDB.AchievementStorage.LookupByKey(secondaryAsset);
                        if (achievement == null)
                            return false;

                        if (Global.AchievementMgr.IsRealmCompleted(achievement))
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.MainHandVisibleSubclass: // 232
                    {
                        uint itemSubclass = (uint)ItemSubClassWeapon.Fist;
                        ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(referencePlayer.m_playerData.VisibleItems[EquipmentSlot.MainHand].ItemID);
                        if (itemTemplate != null)
                            itemSubclass = itemTemplate.GetSubClass();
                        if (itemSubclass != reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.OffHandVisibleSubclass: // 233
                    {
                        uint itemSubclass = (uint)ItemSubClassWeapon.Fist;
                        ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(referencePlayer.m_playerData.VisibleItems[EquipmentSlot.OffHand].ItemID);
                        if (itemTemplate != null)
                            itemSubclass = itemTemplate.GetSubClass();
                        if (itemSubclass != reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.AzeriteItemLevel: // 235
                    {
                        Item heartOfAzeroth = referencePlayer.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);
                        if (!heartOfAzeroth || heartOfAzeroth.ToAzeriteItem().GetLevel() < reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.SourceDisplayRace: // 252
                    {
                        CreatureDisplayInfoRecord creatureDisplayInfo = CliDB.CreatureDisplayInfoStorage.LookupByKey(referencePlayer.GetDisplayId());
                        if (creatureDisplayInfo == null)
                            return false;

                        CreatureDisplayInfoExtraRecord creatureDisplayInfoExtra = CliDB.CreatureDisplayInfoExtraStorage.LookupByKey(creatureDisplayInfo.ExtendedDisplayInfoID);
                        if (creatureDisplayInfoExtra == null)
                            return false;

                        if (creatureDisplayInfoExtra.DisplayRaceID != reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.TargetDisplayRace: // 253
                    {
                        if (!unit)
                            return false;
                        CreatureDisplayInfoRecord creatureDisplayInfo = CliDB.CreatureDisplayInfoStorage.LookupByKey(unit.GetDisplayId());
                        if (creatureDisplayInfo == null)
                            return false;

                        CreatureDisplayInfoExtraRecord creatureDisplayInfoExtra = CliDB.CreatureDisplayInfoExtraStorage.LookupByKey(creatureDisplayInfo.ExtendedDisplayInfoID);
                        if (creatureDisplayInfoExtra == null)
                            return false;

                        if (creatureDisplayInfoExtra.DisplayRaceID != reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.SourceAuraCountEqual: // 255
                    if (referencePlayer.GetAuraCount((uint)secondaryAsset) != reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.TargetAuraCountEqual: // 256
                    if (!unit || unit.GetAuraCount((uint)secondaryAsset) != reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.SourceAuraCountGreater: // 257
                    if (referencePlayer.GetAuraCount((uint)secondaryAsset) < reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.TargetAuraCountGreater: // 258
                    if (!unit || unit.GetAuraCount((uint)secondaryAsset) < reqValue)
                        return false;
                    break;
                case CriteriaAdditionalCondition.UnlockedAzeriteEssenceRankLower: // 259
                    {
                        Item heartOfAzeroth = referencePlayer.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);
                        if (heartOfAzeroth != null)
                        {
                            AzeriteItem azeriteItem = heartOfAzeroth.ToAzeriteItem();
                            if (azeriteItem != null)
                            {
                                foreach (UnlockedAzeriteEssence essence in azeriteItem.m_azeriteItemData.UnlockedEssences)
                                    if (essence.AzeriteEssenceID == reqValue && essence.Rank < secondaryAsset)
                                        return true;
                            }
                        }
                        return false;
                    }
                case CriteriaAdditionalCondition.UnlockedAzeriteEssenceRankEqual: // 260
                    {
                        Item heartOfAzeroth = referencePlayer.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);
                        if (heartOfAzeroth != null)
                        {
                            AzeriteItem azeriteItem = heartOfAzeroth.ToAzeriteItem();
                            if (azeriteItem != null)
                            {
                                foreach (UnlockedAzeriteEssence essence in azeriteItem.m_azeriteItemData.UnlockedEssences)
                                    if (essence.AzeriteEssenceID == reqValue && essence.Rank == secondaryAsset)
                                        return true;
                            }
                        }
                        return false;
                    }
                case CriteriaAdditionalCondition.UnlockedAzeriteEssenceRankGreater: // 261
                    {
                        Item heartOfAzeroth = referencePlayer.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);
                        if (heartOfAzeroth != null)
                        {
                            AzeriteItem azeriteItem = heartOfAzeroth.ToAzeriteItem();
                            if (azeriteItem != null)
                            {
                                foreach (UnlockedAzeriteEssence essence in azeriteItem.m_azeriteItemData.UnlockedEssences)
                                    if (essence.AzeriteEssenceID == reqValue && essence.Rank > secondaryAsset)
                                        return true;
                            }
                        }
                        return false;
                    }
                case CriteriaAdditionalCondition.SourceHasAuraEffectIndex: // 262
                    if (referencePlayer.GetAuraEffect(reqValue, (uint)secondaryAsset) == null)
                        return false;
                    break;
                case CriteriaAdditionalCondition.SourceSpecializationRole: // 263
                    {
                        ChrSpecializationRecord spec = CliDB.ChrSpecializationStorage.LookupByKey(referencePlayer.GetPrimarySpecialization());
                        if (spec == null || spec.Role != reqValue)
                            return false;
                        break;
                    }
                case CriteriaAdditionalCondition.SourceLevel120: // 264
                    if (referencePlayer.GetLevel() != 120)
                        return false;
                    break;
                case CriteriaAdditionalCondition.SelectedAzeriteEssenceRankLower: // 266
                    {
                        Item heartOfAzeroth = referencePlayer.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);
                        if (heartOfAzeroth != null)
                        {
                            AzeriteItem azeriteItem = heartOfAzeroth.ToAzeriteItem();
                            if (azeriteItem != null)
                            {
                                SelectedAzeriteEssences selectedEssences = azeriteItem.GetSelectedAzeriteEssences();
                                if (selectedEssences != null)
                                {
                                    foreach (UnlockedAzeriteEssence essence in azeriteItem.m_azeriteItemData.UnlockedEssences)
                                        if (essence.AzeriteEssenceID == selectedEssences.AzeriteEssenceID[(int)reqValue] && essence.Rank < secondaryAsset)
                                            return true;
                                }
                            }
                        }
                        return false;
                    }
                case CriteriaAdditionalCondition.SelectedAzeriteEssenceRankGreater: // 267
                    {
                        Item heartOfAzeroth = referencePlayer.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);
                        if (heartOfAzeroth != null)
                        {
                            AzeriteItem azeriteItem = heartOfAzeroth.ToAzeriteItem();
                            if (azeriteItem != null)
                            {
                                SelectedAzeriteEssences selectedEssences = azeriteItem.GetSelectedAzeriteEssences();
                                if (selectedEssences != null)
                                {
                                    foreach (UnlockedAzeriteEssence essence in azeriteItem.m_azeriteItemData.UnlockedEssences)
                                        if (essence.AzeriteEssenceID == selectedEssences.AzeriteEssenceID[(int)reqValue] && essence.Rank > secondaryAsset)
                                            return true;
                                }
                            }
                        }
                    }
                    return false;
                case CriteriaAdditionalCondition.MapOrCosmeticMap: // 280
                    {
                        MapRecord map = referencePlayer.GetMap().GetEntry();
                        if (map.Id != reqValue && map.CosmeticParentMapID != reqValue)
                            return false;
                        break;
                    }
                default:
                    break;
            }
            return true;
        }

        public virtual void SendAllData(Player receiver) { }
        public virtual void SendCriteriaUpdate(Criteria criteria, CriteriaProgress progress, uint timeElapsed, bool timedCompleted) { }
        public virtual void SendCriteriaProgressRemoved(uint criteriaId) { }

        public virtual void CompletedCriteriaTree(CriteriaTree tree, Player referencePlayer) { }
        public virtual void AfterCriteriaTreeUpdate(CriteriaTree tree, Player referencePlayer) { }

        public virtual void SendPacket(ServerPacket data) { }

        public virtual bool RequiredAchievementSatisfied(uint achievementId) { return false; }

        public virtual string GetOwnerInfo() { return ""; }
        public virtual List<Criteria> GetCriteriaByType(CriteriaTypes type, uint asset) { return null; }

        protected Dictionary<uint, CriteriaProgress> _criteriaProgress = new Dictionary<uint, CriteriaProgress>();
        Dictionary<uint, uint /*ms time left*/> _timeCriteriaTrees = new Dictionary<uint, uint>();
    }

    public class CriteriaManager : Singleton<CriteriaManager>
    {
        CriteriaManager() { }

        public void LoadCriteriaModifiersTree()
        {
            uint oldMSTime = Time.GetMSTime();

            if (CliDB.ModifierTreeStorage.Empty())
            {
                Log.outError(LogFilter.ServerLoading, "Loaded 0 criteria modifiers.");
                return;
            }

            // Load modifier tree nodes
            foreach (var tree in CliDB.ModifierTreeStorage.Values)
            {
                ModifierTreeNode node = new ModifierTreeNode();
                node.Entry = tree;
                _criteriaModifiers[node.Entry.Id] = node;
            }

            // Build tree
            foreach (var treeNode in _criteriaModifiers.Values)
            {
                if (treeNode.Entry.Parent == 0)
                    continue;

                var parent = _criteriaModifiers.LookupByKey(treeNode.Entry.Parent);
                if (parent != null)
                    parent.Children.Add(treeNode);
            }

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} criteria modifiers in {1} ms", _criteriaModifiers.Count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        T GetEntry<T>(Dictionary<uint, T> map, CriteriaTreeRecord tree) where T : new()
        {
            CriteriaTreeRecord cur = tree;
            var obj = map.LookupByKey(tree.Id);
            while (obj == null)
            {
                if (cur.Parent == 0)
                    break;

                cur = CliDB.CriteriaTreeStorage.LookupByKey(cur.Parent);
                if (cur == null)
                    break;

                obj = map.LookupByKey(cur.Id);
            }

            if (obj == null)
                return default;

            return obj;
        }

        public void LoadCriteriaList()
        {
            uint oldMSTime = Time.GetMSTime();

            if (CliDB.CriteriaTreeStorage.Empty())
            {
                Log.outError(LogFilter.ServerLoading, "Loaded 0 criteria.");
                return;
            }

            Dictionary<uint /*criteriaTreeID*/, AchievementRecord> achievementCriteriaTreeIds = new Dictionary<uint, AchievementRecord>();
            foreach (AchievementRecord achievement in CliDB.AchievementStorage.Values)
                if (achievement.CriteriaTree != 0)
                    achievementCriteriaTreeIds[achievement.CriteriaTree] = achievement;

            Dictionary<uint, ScenarioStepRecord> scenarioCriteriaTreeIds = new Dictionary<uint, ScenarioStepRecord>();
            foreach (ScenarioStepRecord scenarioStep in CliDB.ScenarioStepStorage.Values)
            {
                if (scenarioStep.CriteriaTreeId != 0)
                    scenarioCriteriaTreeIds[scenarioStep.CriteriaTreeId] = scenarioStep;
            }

            Dictionary<uint /*criteriaTreeID*/, QuestObjective> questObjectiveCriteriaTreeIds = new Dictionary<uint, QuestObjective>();
            foreach (var pair in Global.ObjectMgr.GetQuestTemplates())
            {
                foreach (QuestObjective objective in pair.Value.Objectives)
                {
                    if (objective.Type != QuestObjectiveType.CriteriaTree)
                        continue;

                    if (objective.ObjectID != 0)
                        questObjectiveCriteriaTreeIds[(uint)objective.ObjectID] = objective;
                }
            }

            // Load criteria tree nodes
            foreach (CriteriaTreeRecord tree in CliDB.CriteriaTreeStorage.Values)
            {
                // Find linked achievement
                AchievementRecord achievement = GetEntry(achievementCriteriaTreeIds, tree);
                ScenarioStepRecord scenarioStep = GetEntry(scenarioCriteriaTreeIds, tree);
                QuestObjective questObjective = GetEntry(questObjectiveCriteriaTreeIds, tree);
                if (achievement == null && scenarioStep == null && questObjective == null)
                    continue;

                CriteriaTree criteriaTree = new CriteriaTree();
                criteriaTree.Id = tree.Id;
                criteriaTree.Achievement = achievement;
                criteriaTree.ScenarioStep = scenarioStep;
                criteriaTree.QuestObjective = questObjective;
                criteriaTree.Entry = tree;

                _criteriaTrees[criteriaTree.Entry.Id] = criteriaTree;
            }

            // Build tree
            foreach (var pair in _criteriaTrees)
            {
                if (pair.Value.Entry.Parent == 0)
                    continue;

                var parent = _criteriaTrees.LookupByKey(pair.Value.Entry.Parent);
                if (parent != null)
                {
                    parent.Children.Add(pair.Value);
                    while (parent != null)
                    {
                        var cur = parent;
                        parent = _criteriaTrees.LookupByKey(parent.Entry.Parent);
                        if (parent == null)
                        {
                            if (CliDB.CriteriaStorage.ContainsKey(pair.Value.Entry.CriteriaID))
                                _criteriaTreeByCriteria.Add(pair.Value.Entry.CriteriaID, cur);
                        }
                    }
                }
                else if (CliDB.CriteriaStorage.ContainsKey(pair.Value.Entry.CriteriaID))
                    _criteriaTreeByCriteria.Add(pair.Value.Entry.CriteriaID, pair.Value);
            }

            for (var i = 0; i < (int)CriteriaCondition.Max; ++i)
                _criteriasByFailEvent[i] = new MultiMap<int, Criteria>();

            // Load criteria
            uint criterias = 0;
            uint guildCriterias = 0;
            uint scenarioCriterias = 0;
            uint questObjectiveCriterias = 0;
            foreach (CriteriaRecord criteriaEntry in CliDB.CriteriaStorage.Values)
            {
                Cypher.Assert(criteriaEntry.Type < CriteriaTypes.TotalTypes,
                    $"CRITERIA_TYPE_TOTAL must be greater than or equal to {criteriaEntry.Type + 1} but is currently equal to {CriteriaTypes.TotalTypes}");
                Cypher.Assert(criteriaEntry.StartEvent < CriteriaTimedTypes.Max, $"CRITERIA_TYPE_TOTAL must be greater than or equal to {criteriaEntry.StartEvent + 1} but is currently equal to {CriteriaTimedTypes.Max}");
                Cypher.Assert(criteriaEntry.FailEvent < (byte)CriteriaCondition.Max, $"CRITERIA_CONDITION_MAX must be greater than or equal to {criteriaEntry.FailEvent + 1} but is currently equal to {CriteriaCondition.Max}");

                var treeList = _criteriaTreeByCriteria.LookupByKey(criteriaEntry.Id);
                if (treeList.Empty())
                    continue;

                Criteria criteria = new Criteria();
                criteria.Id = criteriaEntry.Id;
                criteria.Entry = criteriaEntry;
                var mod = _criteriaModifiers.LookupByKey(criteriaEntry.ModifierTreeId);
                if (mod != null)
                    criteria.Modifier = mod;

                _criteria[criteria.Id] = criteria;

                foreach (CriteriaTree tree in treeList)
                {
                    AchievementRecord achievement = tree.Achievement;
                    if (achievement != null)
                    {
                        if (achievement.Flags.HasAnyFlag(AchievementFlags.Guild))
                            criteria.FlagsCu |= CriteriaFlagsCu.Guild;
                        else if (achievement.Flags.HasAnyFlag(AchievementFlags.Account))
                            criteria.FlagsCu |= CriteriaFlagsCu.Account;
                        else
                            criteria.FlagsCu |= CriteriaFlagsCu.Player;
                    }
                    else if (tree.ScenarioStep != null)
                        criteria.FlagsCu |= CriteriaFlagsCu.Scenario;
                    else if (tree.QuestObjective != null)
                        criteria.FlagsCu |= CriteriaFlagsCu.QuestObjective;
                }

                if (criteria.FlagsCu.HasAnyFlag(CriteriaFlagsCu.Player | CriteriaFlagsCu.Account))
                {
                    ++criterias;
                    _criteriasByType.Add(criteriaEntry.Type, criteria);
                }

                if (criteria.FlagsCu.HasAnyFlag(CriteriaFlagsCu.Guild))
                {
                    ++guildCriterias;
                    _guildCriteriasByType.Add(criteriaEntry.Type, criteria);
                }

                if (criteria.FlagsCu.HasAnyFlag(CriteriaFlagsCu.Scenario))
                {
                    ++scenarioCriterias;
                    _scenarioCriteriasByType.Add(criteriaEntry.Type, criteria);
                }

                if (criteria.FlagsCu.HasAnyFlag(CriteriaFlagsCu.QuestObjective))
                {
                    ++questObjectiveCriterias;
                    _questObjectiveCriteriasByType.Add(criteriaEntry.Type, criteria);
                }

                if (criteriaEntry.StartTimer != 0)
                    _criteriasByTimedType.Add(criteriaEntry.StartEvent, criteria);

                if (criteriaEntry.FailEvent != 0)
                    _criteriasByFailEvent[criteriaEntry.FailEvent].Add((int)criteriaEntry.FailAsset, criteria);
            }

            foreach (var p in _criteriaTrees)
                p.Value.Criteria = GetCriteria(p.Value.Entry.CriteriaID);

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {criterias} criteria, {guildCriterias} guild criteria, {scenarioCriterias} scenario criteria and {questObjectiveCriterias} quest objective criteria in {Time.GetMSTimeDiffToNow(oldMSTime)} ms.");
        }

        public void LoadCriteriaData()
        {
            uint oldMSTime = Time.GetMSTime();

            _criteriaDataMap.Clear();                              // need for reload case

            SQLResult result = DB.World.Query("SELECT criteria_id, type, value1, value2, ScriptName FROM criteria_data");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 additional criteria data. DB table `criteria_data` is empty.");
                return;
            }

            uint count = 0;
            do
            {
                uint criteria_id = result.Read<uint>(0);

                Criteria criteria = GetCriteria(criteria_id);
                if (criteria == null)
                {
                    Log.outError(LogFilter.Sql, "Table `criteria_data` contains data for non-existing criteria (Entry: {0}). Ignored.", criteria_id);
                    continue;
                }

                CriteriaDataType dataType = (CriteriaDataType)result.Read<byte>(1);
                string scriptName = result.Read<string>(4);
                uint scriptId = 0;
                if (!scriptName.IsEmpty())
                {
                    if (dataType != CriteriaDataType.Script)
                        Log.outError(LogFilter.Sql, "Table `criteria_data` contains a ScriptName for non-scripted data type (Entry: {0}, type {1}), useless data.", criteria_id, dataType);
                    else
                        scriptId = Global.ObjectMgr.GetScriptId(scriptName);
                }

                CriteriaData data = new CriteriaData(dataType, result.Read<uint>(2), result.Read<uint>(3), scriptId);

                if (!data.IsValid(criteria))
                    continue;

                // this will allocate empty data set storage
                CriteriaDataSet dataSet = new CriteriaDataSet();
                dataSet.SetCriteriaId(criteria_id);

                // add real data only for not NONE data types
                if (data.DataType != CriteriaDataType.None)
                    dataSet.Add(data);

                _criteriaDataMap[criteria_id] = dataSet;
                // counting data by and data types
                ++count;
            }
            while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} additional criteria data in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public CriteriaTree GetCriteriaTree(uint criteriaTreeId)
        {
            return _criteriaTrees.LookupByKey(criteriaTreeId);
        }

        public Criteria GetCriteria(uint criteriaId)
        {
            return _criteria.LookupByKey(criteriaId);
        }

        public ModifierTreeNode GetModifierTree(uint modifierTreeId)
        {
            return _criteriaModifiers.LookupByKey(modifierTreeId);
        }

        bool IsCriteriaTypeStoredByAsset(CriteriaTypes type)
        {
            switch (type)
            {
                case CriteriaTypes.KillCreature:
                case CriteriaTypes.WinBg:
                case CriteriaTypes.ReachSkillLevel:
                case CriteriaTypes.CompleteAchievement:
                case CriteriaTypes.CompleteQuestsInZone:
                case CriteriaTypes.CompleteBattleground:
                case CriteriaTypes.KilledByCreature:
                case CriteriaTypes.CompleteQuest:
                case CriteriaTypes.BeSpellTarget:
                case CriteriaTypes.CastSpell:
                case CriteriaTypes.BgObjectiveCapture:
                case CriteriaTypes.HonorableKillAtArea:
                case CriteriaTypes.LearnSpell:
                case CriteriaTypes.OwnItem:
                case CriteriaTypes.LearnSkillLevel:
                case CriteriaTypes.UseItem:
                case CriteriaTypes.LootItem:
                case CriteriaTypes.ExploreArea:
                case CriteriaTypes.GainReputation:
                case CriteriaTypes.EquipEpicItem:
                case CriteriaTypes.HkClass:
                case CriteriaTypes.HkRace:
                case CriteriaTypes.DoEmote:
                case CriteriaTypes.EquipItem:
                case CriteriaTypes.UseGameobject:
                case CriteriaTypes.BeSpellTarget2:
                case CriteriaTypes.FishInGameobject:
                case CriteriaTypes.LearnSkilllineSpells:
                case CriteriaTypes.LootType:
                case CriteriaTypes.CastSpell2:
                case CriteriaTypes.LearnSkillLine:
                    return true;
                default:
                    return false;
            }
        }

        public List<Criteria> GetPlayerCriteriaByType(CriteriaTypes type, uint asset)
        {
            if (asset != 0 && IsCriteriaTypeStoredByAsset(type))
            {
                if (_criteriasByAsset[(int)type].ContainsKey(asset))
                    return _criteriasByAsset[(int)type][asset];
            }

            return _criteriasByType.LookupByKey(type);
        }

        public List<Criteria> GetGuildCriteriaByType(CriteriaTypes type)
        {
            return _guildCriteriasByType.LookupByKey(type);
        }

        public List<Criteria> GetScenarioCriteriaByType(CriteriaTypes type)
        {
            return _scenarioCriteriasByType.LookupByKey(type);
        }

        public List<Criteria> GetQuestObjectiveCriteriaByType(CriteriaTypes type)
        {
            return _questObjectiveCriteriasByType[type];
        }

        public List<CriteriaTree> GetCriteriaTreesByCriteria(uint criteriaId)
        {
            return _criteriaTreeByCriteria.LookupByKey(criteriaId);
        }

        public List<Criteria> GetTimedCriteriaByType(CriteriaTimedTypes type)
        {
            return _criteriasByTimedType.LookupByKey(type);
        }

        public List<Criteria> GetCriteriaByFailEvent(CriteriaCondition condition, int asset)
        {
            return _criteriasByFailEvent[(int)condition].LookupByKey(asset);
        }
        
        public CriteriaDataSet GetCriteriaDataSet(Criteria criteria)
        {
            return _criteriaDataMap.LookupByKey(criteria.Id);
        }

        public static bool IsGroupCriteriaType(CriteriaTypes type)
        {
            switch (type)
            {
                case CriteriaTypes.KillCreature:
                case CriteriaTypes.WinBg:
                case CriteriaTypes.BeSpellTarget:         // NYI
                case CriteriaTypes.WinRatedArena:
                case CriteriaTypes.BeSpellTarget2:        // NYI
                case CriteriaTypes.WinRatedBattleground:  // NYI
                    return true;
                default:
                    break;
            }

            return false;
        }

        public static void WalkCriteriaTree(CriteriaTree tree, Action<CriteriaTree> func)
        {
            foreach (CriteriaTree node in tree.Children)
                WalkCriteriaTree(node, func);

            func(tree);
        }

        Dictionary<uint, CriteriaDataSet> _criteriaDataMap = new Dictionary<uint, CriteriaDataSet>();

        Dictionary<uint, CriteriaTree> _criteriaTrees = new Dictionary<uint, CriteriaTree>();
        Dictionary<uint, Criteria> _criteria = new Dictionary<uint, Criteria>();
        Dictionary<uint, ModifierTreeNode> _criteriaModifiers = new Dictionary<uint, ModifierTreeNode>();

        MultiMap<uint, CriteriaTree> _criteriaTreeByCriteria = new MultiMap<uint, CriteriaTree>();

        // store criterias by type to speed up lookup
        MultiMap<CriteriaTypes, Criteria> _criteriasByType = new MultiMap<CriteriaTypes, Criteria>();
        MultiMap<uint, Criteria>[] _criteriasByAsset = new MultiMap<uint, Criteria>[(int)CriteriaTypes.TotalTypes];
        MultiMap<CriteriaTypes, Criteria> _guildCriteriasByType = new MultiMap<CriteriaTypes, Criteria>();
        MultiMap<CriteriaTypes, Criteria> _scenarioCriteriasByType = new MultiMap<CriteriaTypes, Criteria>();
        MultiMap<CriteriaTypes, Criteria> _questObjectiveCriteriasByType = new MultiMap<CriteriaTypes, Criteria>();

        MultiMap<CriteriaTimedTypes, Criteria> _criteriasByTimedType = new MultiMap<CriteriaTimedTypes, Criteria>();
        MultiMap<int, Criteria>[] _criteriasByFailEvent = new MultiMap<int, Criteria>[(int)CriteriaCondition.Max];
    }

    public class ModifierTreeNode
    {
        public ModifierTreeRecord Entry;
        public List<ModifierTreeNode> Children = new List<ModifierTreeNode>();
    }

    public class Criteria
    {
        public uint Id;
        public CriteriaRecord Entry;
        public ModifierTreeNode Modifier;
        public CriteriaFlagsCu FlagsCu;
    }

    public class CriteriaTree
    {    
        public uint Id;
        public CriteriaTreeRecord Entry;
        public AchievementRecord Achievement;
        public ScenarioStepRecord ScenarioStep;
        public QuestObjective QuestObjective;
        public Criteria Criteria;
        public List<CriteriaTree> Children = new List<CriteriaTree>();
    }

    public class CriteriaProgress
    {
        public ulong Counter;
        public long Date;                                            // latest update time.
        public ObjectGuid PlayerGUID;                               // GUID of the player that completed this criteria (guild achievements)
        public bool Changed;
    }

    [StructLayout(LayoutKind.Explicit)]
    public class CriteriaData
    {
        public CriteriaData()
        {
            DataType = CriteriaDataType.None;

            Raw.Value1 = 0;
            Raw.Value2 = 0;
            ScriptId = 0;
        }

        public CriteriaData(CriteriaDataType _dataType, uint _value1, uint _value2, uint _scriptId)
        {
            DataType = _dataType;

            Raw.Value1 = _value1;
            Raw.Value2 = _value2;
            ScriptId = _scriptId;
        }

        public bool IsValid(Criteria criteria)
        {
            if (DataType >= CriteriaDataType.Max)
            {
                Log.outError(LogFilter.Sql, "Table `criteria_data` for criteria (Entry: {0}) has wrong data type ({1}), ignored.", criteria.Id, DataType);
                return false;
            }

            switch (criteria.Entry.Type)
            {
                case CriteriaTypes.KillCreature:
                case CriteriaTypes.KillCreatureType:
                case CriteriaTypes.WinBg:
                case CriteriaTypes.FallWithoutDying:
                case CriteriaTypes.CompleteQuest:          // only hardcoded list
                case CriteriaTypes.CastSpell:
                case CriteriaTypes.WinRatedArena:
                case CriteriaTypes.DoEmote:
                case CriteriaTypes.SpecialPvpKill:
                case CriteriaTypes.WinDuel:
                case CriteriaTypes.LootType:
                case CriteriaTypes.CastSpell2:
                case CriteriaTypes.BeSpellTarget:
                case CriteriaTypes.BeSpellTarget2:
                case CriteriaTypes.EquipEpicItem:
                case CriteriaTypes.RollNeedOnLoot:
                case CriteriaTypes.RollGreedOnLoot:
                case CriteriaTypes.BgObjectiveCapture:
                case CriteriaTypes.HonorableKill:
                case CriteriaTypes.CompleteDailyQuest:    // only Children's Week achievements
                case CriteriaTypes.UseItem:                // only Children's Week achievements
                case CriteriaTypes.GetKillingBlows:
                case CriteriaTypes.ReachLevel:
                case CriteriaTypes.OnLogin:
                case CriteriaTypes.LootEpicItem:
                case CriteriaTypes.ReceiveEpicItem:
                    break;
                default:
                    if (DataType != CriteriaDataType.Script)
                    {
                        Log.outError(LogFilter.Sql, "Table `criteria_data` has data for non-supported criteria type (Entry: {0} Type: {1}), ignored.", criteria.Id, (CriteriaTypes)criteria.Entry.Type);
                        return false;
                    }
                    break;
            }

            switch (DataType)
            {
                case CriteriaDataType.None:
                case CriteriaDataType.InstanceScript:
                    return true;
                case CriteriaDataType.TCreature:
                    if (Creature.Id == 0 || Global.ObjectMgr.GetCreatureTemplate(Creature.Id) == null)
                    {
                        Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_CREATURE ({2}) has non-existing creature id in value1 ({3}), ignored.",
                            criteria.Id, criteria.Entry.Type, DataType, Creature.Id);
                        return false;
                    }
                    return true;
                case CriteriaDataType.TPlayerClassRace:
                    if (ClassRace.ClassId == 0 && ClassRace.RaceId == 0)
                    {
                        Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_T_PLAYER_CLASS_RACE ({2}) must not have 0 in either value field, ignored.",
                            criteria.Id, criteria.Entry.Type, DataType);
                        return false;
                    }
                    if (ClassRace.ClassId != 0 && ((1 << (int)(ClassRace.ClassId - 1)) & (int)Class.ClassMaskAllPlayable) == 0)
                    {
                        Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_T_PLAYER_CLASS_RACE ({2}) has non-existing class in value1 ({3}), ignored.",
                            criteria.Id, criteria.Entry.Type, DataType, ClassRace.ClassId);
                        return false;
                    }
                    if (ClassRace.RaceId != 0 && ((1ul << (int)(ClassRace.RaceId - 1)) & (ulong)Race.RaceMaskAllPlayable) == 0)
                    {
                        Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_T_PLAYER_CLASS_RACE ({2}) has non-existing race in value2 ({3}), ignored.",
                            criteria.Id, criteria.Entry.Type, DataType, ClassRace.RaceId);
                        return false;
                    }
                    return true;
                case CriteriaDataType.TPlayerLessHealth:
                    if (Health.Percent < 1 || Health.Percent > 100)
                    {
                        Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_PLAYER_LESS_HEALTH ({2}) has wrong percent value in value1 ({3}), ignored.",
                            criteria.Id, criteria.Entry.Type, DataType, Health.Percent);
                        return false;
                    }
                    return true;
                case CriteriaDataType.SAura:
                case CriteriaDataType.TAura:
                    {
                        SpellInfo spellEntry = Global.SpellMgr.GetSpellInfo(Aura.SpellId);
                        if (spellEntry == null)
                        {
                            Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type {2} has wrong spell id in value1 ({3}), ignored.",
                                criteria.Id, criteria.Entry.Type, DataType, Aura.SpellId);
                            return false;
                        }
                        SpellEffectInfo effect = spellEntry.GetEffect(Difficulty.None, Aura.EffectIndex);
                        if (effect == null)
                        {
                            Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type {2} has wrong spell effect index in value2 ({3}), ignored.",
                                criteria.Id, criteria.Entry.Type, DataType, Aura.EffectIndex);
                            return false;
                        }
                        if (effect.ApplyAuraName == 0)
                        {
                            Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type {2} has non-aura spell effect (ID: {3} Effect: {4}), ignores.",
                                criteria.Id, criteria.Entry.Type, DataType, Aura.SpellId, Aura.EffectIndex);
                            return false;
                        }
                        return true;
                    }
                case CriteriaDataType.Value:
                    if (Value.ComparisonType >= (int)ComparisionType.Max)
                    {
                        Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_VALUE ({2}) has wrong ComparisionType in value2 ({3}), ignored.",
                            criteria.Id, criteria.Entry.Type, DataType, Value.ComparisonType);
                        return false;
                    }
                    return true;
                case CriteriaDataType.TLevel:
                    if (Level.Min > SharedConst.GTMaxLevel)
                    {
                        Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_T_LEVEL ({2}) has wrong minlevel in value1 ({3}), ignored.",
                            criteria.Id, criteria.Entry.Type, DataType, Level.Min);
                        return false;
                    }
                    return true;
                case CriteriaDataType.TGender:
                    if (Gender.Gender > (int)Framework.Constants.Gender.None)
                    {
                        Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_T_GENDER ({2}) has wrong gender in value1 ({3}), ignored.",
                            criteria.Id, criteria.Entry.Type, DataType, Gender.Gender);
                        return false;
                    }
                    return true;
                case CriteriaDataType.Script:
                    if (ScriptId == 0)
                    {
                        Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_SCRIPT ({2}) does not have ScriptName set, ignored.",
                            criteria.Id, criteria.Entry.Type, DataType);
                        return false;
                    }
                    return true;
                case CriteriaDataType.MapPlayerCount:
                    if (MapPlayers.MaxCount <= 0)
                    {
                        Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_MAP_PLAYER_COUNT ({2}) has wrong max players count in value1 ({3}), ignored.",
                            criteria.Id, criteria.Entry.Type, DataType, MapPlayers.MaxCount);
                        return false;
                    }
                    return true;
                case CriteriaDataType.TTeam:
                    if (TeamId.Team != (int)Team.Alliance && TeamId.Team != (int)Team.Horde)
                    {
                        Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_T_TEAM ({2}) has unknown team in value1 ({3}), ignored.",
                            criteria.Id, criteria.Entry.Type, DataType, TeamId.Team);
                        return false;
                    }
                    return true;
                case CriteriaDataType.SDrunk:
                    if (Drunk.State >= 4)
                    {
                        Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_S_DRUNK ({2}) has unknown drunken state in value1 ({3}), ignored.",
                            criteria.Id, criteria.Entry.Type, DataType, Drunk.State);
                        return false;
                    }
                    return true;
                case CriteriaDataType.Holiday:
                    if (!CliDB.HolidaysStorage.ContainsKey(Holiday.Id))
                    {
                        Log.outError(LogFilter.Sql, "Table `criteria_data`(Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_HOLIDAY ({2}) has unknown holiday in value1 ({3}), ignored.",
                            criteria.Id, criteria.Entry.Type, DataType, Holiday.Id);
                        return false;
                    }
                    return true;
                case CriteriaDataType.GameEvent:
                    {
                        var events = Global.GameEventMgr.GetEventMap();
                        if (GameEvent.Id < 1 || GameEvent.Id >= events.Length)
                        {
                            Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_GAME_EVENT ({2}) has unknown game_event in value1 ({3}), ignored.",
                                criteria.Id, criteria.Entry.Type, DataType, GameEvent.Id);
                            return false;
                        }
                        return true;
                    }
                case CriteriaDataType.BgLossTeamScore:
                    return true;                                    // not check correctness node indexes
                case CriteriaDataType.SEquippedItem:
                    if (EquippedItem.ItemQuality >= (uint)ItemQuality.Max)
                    {
                        Log.outError(LogFilter.Sql, "Table `achievement_criteria_requirement` (Entry: {0} Type: {1}) for requirement ACHIEVEMENT_CRITERIA_REQUIRE_S_EQUIPED_ITEM ({2}) has unknown quality state in value1 ({3}), ignored.",
                            criteria.Id, criteria.Entry.Type, DataType, EquippedItem.ItemQuality);
                        return false;
                    }
                    return true;
                case CriteriaDataType.MapId:
                    if (!CliDB.MapStorage.ContainsKey(MapId.Id))
                    {
                        Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_MAP_ID ({2}) contains an unknown map entry in value1 ({3}), ignored.",
                            criteria.Id, criteria.Entry.Type, DataType, MapId.Id);
                    }
                    return true;
                case CriteriaDataType.SPlayerClassRace:
                    if (ClassRace.ClassId == 0 && ClassRace.RaceId == 0)
                    {
                        Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_S_PLAYER_CLASS_RACE ({2}) must not have 0 in either value field, ignored.",
                            criteria.Id, criteria.Entry.Type, DataType);
                        return false;
                    }
                    if (ClassRace.ClassId != 0 && ((1 << (int)(ClassRace.ClassId - 1)) & (int)Class.ClassMaskAllPlayable) == 0)
                    {
                        Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_S_PLAYER_CLASS_RACE ({2}) has non-existing class in value1 ({3}), ignored.",
                            criteria.Id, criteria.Entry.Type, DataType, ClassRace.ClassId);
                        return false;
                    }
                    if (ClassRace.RaceId != 0 && ((1ul << (int)(ClassRace.RaceId - 1)) & (ulong)Race.RaceMaskAllPlayable) == 0)
                    {
                        Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_S_PLAYER_CLASS_RACE ({2}) has non-existing race in value2 ({3}), ignored.",
                            criteria.Id, criteria.Entry.Type, DataType, ClassRace.RaceId);
                        return false;
                    }
                    return true;
                case CriteriaDataType.SKnownTitle:
                    if (!CliDB.CharTitlesStorage.ContainsKey(KnownTitle.Id))
                    {
                        Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_S_KNOWN_TITLE ({2}) contains an unknown title_id in value1 ({3}), ignore.",
                            criteria.Id, criteria.Entry.Type, DataType, KnownTitle.Id);
                        return false;
                    }
                    return true;
                case CriteriaDataType.SItemQuality:
                    if (itemQuality.Quality >= (uint)ItemQuality.Max)
                    {
                        Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_S_ITEM_QUALITY ({2}) contains an unknown quality state value in value1 ({3}), ignored.",
                            criteria.Id, criteria.Entry.Type, DataType, itemQuality.Quality);
                        return false;
                    }
                    return true;
                default:
                    Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) contains data of a non-supported data type ({2}), ignored.", criteria.Id, criteria.Entry.Type, DataType);
                    return false;
            }
        }

        public bool Meets(uint criteria_id, Player source, Unit target, uint miscValue1 = 0)
        {
            switch (DataType)
            {
                case CriteriaDataType.None:
                    return true;
                case CriteriaDataType.TCreature:
                    if (target == null || !target.IsTypeId(TypeId.Unit))
                        return false;
                    return target.GetEntry() == Creature.Id;
                case CriteriaDataType.TPlayerClassRace:
                    if (target == null || !target.IsTypeId(TypeId.Player))
                        return false;
                    if (ClassRace.ClassId != 0 && ClassRace.ClassId != (uint)target.ToPlayer().GetClass())
                        return false;
                    if (ClassRace.RaceId != 0 && ClassRace.RaceId != (uint)target.ToPlayer().GetRace())
                        return false;
                    return true;
                case CriteriaDataType.SPlayerClassRace:
                    if (source == null || !source.IsTypeId(TypeId.Player))
                        return false;
                    if (ClassRace.ClassId != 0 && ClassRace.ClassId != (uint)source.ToPlayer().GetClass())
                        return false;
                    if (ClassRace.RaceId != 0 && ClassRace.RaceId != (uint)source.ToPlayer().GetRace())
                        return false;
                    return true;
                case CriteriaDataType.TPlayerLessHealth:
                    if (target == null || !target.IsTypeId(TypeId.Player))
                        return false;
                    return !target.HealthAbovePct((int)Health.Percent);
                case CriteriaDataType.SAura:
                    return source.HasAuraEffect(Aura.SpellId, (byte)Aura.EffectIndex);
                case CriteriaDataType.TAura:
                    return target != null && target.HasAuraEffect(Aura.SpellId, (byte)Aura.EffectIndex);
                case CriteriaDataType.Value:
                    return MathFunctions.CompareValues((ComparisionType)Value.ComparisonType, miscValue1, Value.Value);
                case CriteriaDataType.TLevel:
                    if (target == null)
                        return false;
                    return target.GetLevelForTarget(source) >= Level.Min;
                case CriteriaDataType.TGender:
                    if (target == null)
                        return false;
                    return (uint)target.GetGender() == Gender.Gender;
                case CriteriaDataType.Script:
                    return Global.ScriptMgr.OnCriteriaCheck(ScriptId, source, target);
                case CriteriaDataType.MapPlayerCount:
                    return source.GetMap().GetPlayersCountExceptGMs() <= MapPlayers.MaxCount;
                case CriteriaDataType.TTeam:
                    if (target == null || !target.IsTypeId(TypeId.Player))
                        return false;
                    return (uint)target.ToPlayer().GetTeam() == TeamId.Team;
                case CriteriaDataType.SDrunk:
                    return Player.GetDrunkenstateByValue(source.GetDrunkValue()) >= (DrunkenState)Drunk.State;
                case CriteriaDataType.Holiday:
                    return Global.GameEventMgr.IsHolidayActive((HolidayIds)Holiday.Id);
                case CriteriaDataType.GameEvent:
                    return Global.GameEventMgr.IsEventActive((ushort)GameEvent.Id);
                case CriteriaDataType.BgLossTeamScore:
                    {
                        Battleground bg = source.GetBattleground();
                        if (!bg)
                            return false;

                        int score = (int)bg.GetTeamScore(source.GetTeam() == Team.Alliance ? Framework.Constants.TeamId.Horde : Framework.Constants.TeamId.Alliance);
                        return score >= BattlegroundScore.Min && score <= BattlegroundScore.Max;
                    }
                case CriteriaDataType.InstanceScript:
                    {
                        if (!source.IsInWorld)
                            return false;
                        Map map = source.GetMap();
                        if (!map.IsDungeon())
                        {
                            Log.outError(LogFilter.Achievement, "Achievement system call AchievementCriteriaDataType.InstanceScript ({0}) for achievement criteria {1} for non-dungeon/non-raid map {2}",
                                CriteriaDataType.InstanceScript, criteria_id, map.GetId());
                            return false;
                        }
                        InstanceScript instance = ((InstanceMap)map).GetInstanceScript();
                        if (instance == null)
                        {
                            Log.outError(LogFilter.Achievement, "Achievement system call criteria_data_INSTANCE_SCRIPT ({0}) for achievement criteria {1} for map {2} but map does not have a instance script",
                                CriteriaDataType.InstanceScript, criteria_id, map.GetId());
                            return false;
                        }
                        return instance.CheckAchievementCriteriaMeet(criteria_id, source, target, miscValue1);
                    }
                case CriteriaDataType.SEquippedItem:
                    {
                        ItemTemplate pProto = Global.ObjectMgr.GetItemTemplate(miscValue1);
                        if (pProto == null)
                            return false;
                        return pProto.GetBaseItemLevel() >= EquippedItem.ItemLevel && (int)pProto.GetQuality() >= EquippedItem.ItemQuality;
                    }
                                case CriteriaDataType.MapId:
                    return source.GetMapId() == MapId.Id;
                case CriteriaDataType.SKnownTitle:
                    {
                        CharTitlesRecord titleInfo = CliDB.CharTitlesStorage.LookupByKey(KnownTitle.Id);
                        if (titleInfo != null)
                            return source && source.HasTitle(titleInfo.MaskID);

                        return false;
                    }
                case CriteriaDataType.SItemQuality:
                    {
                        ItemTemplate pProto = Global.ObjectMgr.GetItemTemplate(miscValue1);
                        if (pProto == null)
                            return false;
                        return (uint)pProto.GetQuality() == itemQuality.Quality;
                    }
                default:
                    break;
            }
            return false;
        }

        [FieldOffset(0)]
        public CriteriaDataType DataType;

        [FieldOffset(4)]
        public CreatureStruct Creature;

        [FieldOffset(4)]
        public ClassRaceStruct ClassRace;

        [FieldOffset(4)]
        public HealthStruct Health;

        [FieldOffset(4)]
        public AuraStruct Aura;

        [FieldOffset(4)]
        public ValueStruct Value;

        [FieldOffset(4)]
        public LevelStruct Level;

        [FieldOffset(4)]
        public GenderStruct Gender;

        [FieldOffset(4)]
        public MapPlayersStruct MapPlayers;

        [FieldOffset(4)]
        public TeamStruct TeamId;

        [FieldOffset(4)]
        public DrunkStruct Drunk;

        [FieldOffset(4)]
        public HolidayStruct Holiday;

        [FieldOffset(4)]
        public BgLossTeamScoreStruct BattlegroundScore;

        [FieldOffset(4)]
        public EquippedItemStruct EquippedItem;

        [FieldOffset(4)]
        public MapIdStruct MapId;

        [FieldOffset(4)]
        public KnownTitleStruct KnownTitle;

        [FieldOffset(4)]
        public GameEventStruct GameEvent;

        [FieldOffset(4)]
        public ItemQualityStruct itemQuality;

        [FieldOffset(4)]
        public RawStruct Raw;

        [FieldOffset(12)]
        public uint ScriptId;

        #region Structs
        // criteria_data_TYPE_NONE              = 0 (no data)
        // criteria_data_TYPE_T_CREATURE        = 1
        public struct CreatureStruct
        {
            public uint Id;
        }
        // criteria_data_TYPE_T_PLAYER_CLASS_RACE = 2
        // criteria_data_TYPE_S_PLAYER_CLASS_RACE = 21
        public struct ClassRaceStruct
        {
            public uint ClassId;
            public uint RaceId;
        }
        // criteria_data_TYPE_T_PLAYER_LESS_HEALTH = 3
        public struct HealthStruct
        {
            public uint Percent;
        }
        // criteria_data_TYPE_S_AURA            = 5
        // criteria_data_TYPE_T_AURA            = 7
        public struct AuraStruct
        {
            public uint SpellId;
            public uint EffectIndex;
        }
        // criteria_data_TYPE_VALUE             = 8
        public struct ValueStruct
        {
            public uint Value;
            public uint ComparisonType;
        }
        // criteria_data_TYPE_T_LEVEL           = 9
        public struct LevelStruct
        {
            public uint Min;
        }
        // criteria_data_TYPE_T_GENDER          = 10
        public struct GenderStruct
        {
            public uint Gender;
        }
        // criteria_data_TYPE_SCRIPT            = 11 (no data)
        // criteria_data_TYPE_MAP_PLAYER_COUNT  = 13
        public struct MapPlayersStruct
        {
            public uint MaxCount;
        }
        // criteria_data_TYPE_T_TEAM            = 14
        public struct TeamStruct
        {
            public uint Team;
        }
        // criteria_data_TYPE_S_DRUNK           = 15
        public struct DrunkStruct
        {
            public uint State;
        }
        // criteria_data_TYPE_HOLIDAY           = 16
        public struct HolidayStruct
        {
            public uint Id;
        }
        // criteria_data_TYPE_BG_LOSS_TEAM_SCORE= 17
        public struct BgLossTeamScoreStruct
        {
            public uint Min;
            public uint Max;
        }
        // criteria_data_INSTANCE_SCRIPT        = 18 (no data)
        // criteria_data_TYPE_S_EQUIPED_ITEM    = 19
        public struct EquippedItemStruct
        {
            public uint ItemLevel;
            public uint ItemQuality;
        }
        // criteria_data_TYPE_MAP_ID            = 20
        public struct MapIdStruct
        {
            public uint Id;
        }
        // criteria_data_TYPE_KNOWN_TITLE       = 23
        public struct KnownTitleStruct
        {
            public uint Id;
        }
        // CRITERIA_DATA_TYPE_S_ITEM_QUALITY    = 24
        public struct ItemQualityStruct
        {
            public uint Quality;
        }
        // criteria_data_TYPE_GAME_EVENT           = 25
        public struct GameEventStruct
        {
            public uint Id;
        }
        // raw
        public struct RawStruct
        {
            public uint Value1;
            public uint Value2;
        }
        #endregion
    }

    public class CriteriaDataSet
    {
        public void Add(CriteriaData data) { storage.Add(data); }

        public bool Meets(Player source, Unit target, uint miscValue = 0)
        {
            foreach (var data in storage)
                if (!data.Meets(criteria_id, source, target, miscValue))
                    return false;

            return true;
        }

        public void SetCriteriaId(uint id) { criteria_id = id; }

        uint criteria_id;
        List<CriteriaData> storage = new List<CriteriaData>();
    }
}
