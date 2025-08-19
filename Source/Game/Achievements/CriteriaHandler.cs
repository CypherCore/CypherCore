// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.ClientBuild;
using Framework.Constants;
using Framework.Database;
using Game.Arenas;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Garrisons;
using Game.Maps;
using Game.Miscellaneous;
using Game.Networking;
using Game.Networking.Packets;
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
        protected Dictionary<uint, CriteriaProgress> _criteriaProgress = new();
        Dictionary<uint /*criteriaID*/, TimeSpan /*time left*/> _startedCriteria = new();

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
        /// <param name="refe"></param>
        /// <param name="referencePlayer"></param>
        public void UpdateCriteria(CriteriaType type, ulong miscValue1 = 0, ulong miscValue2 = 0, ulong miscValue3 = 0, WorldObject refe = null, Player referencePlayer = null)
        {
            if (type >= CriteriaType.Count)
            {
                Log.outDebug(LogFilter.Achievement, "UpdateCriteria: Wrong criteria type {0}", type);
                return;
            }

            if (referencePlayer == null)
            {
                Log.outDebug(LogFilter.Achievement, "UpdateCriteria: Player is NULL! Cant update criteria");
                return;
            }

            // Disable for GameMasters with GM-mode enabled or for players that don't have the related RBAC permission
            if (referencePlayer.IsGameMaster() || referencePlayer.GetSession().HasPermission(RBACPermissions.CannotEarnAchievements))
            {
                Log.outDebug(LogFilter.Achievement, $"CriteriaHandler::UpdateCriteria: [Player {referencePlayer.GetName()} {(referencePlayer.IsGameMaster() ? "GM mode on" : "disallowed by RBAC")}]" +
                    $" {GetOwnerInfo()}, {type} ({(uint)type}), {miscValue1}, {miscValue2}, {miscValue3}");
                return;
            }

            Log.outDebug(LogFilter.Achievement, "UpdateCriteria({0}, {1}, {2}, {3}) {4}", type, type, miscValue1, miscValue2, miscValue3, GetOwnerInfo());

            List<Criteria> criteriaList = GetCriteriaByType(type, (uint)miscValue1);
            foreach (Criteria criteria in criteriaList)
                UpdateCriteria(criteria, miscValue1, miscValue2, miscValue3, refe, referencePlayer);
        }

        public void UpdateCriteria(Criteria criteria, ulong miscValue1 = 0, ulong miscValue2 = 0, ulong miscValue3 = 0, WorldObject refe = null, Player referencePlayer = null)
        {
            var trees = Global.CriteriaMgr.GetCriteriaTreesByCriteria(criteria.Id);
            if (!CanUpdateCriteria(criteria, trees, miscValue1, miscValue2, miscValue3, refe, referencePlayer))
                return;

            // requirements not found in the dbc
            CriteriaDataSet data = Global.CriteriaMgr.GetCriteriaDataSet(criteria);
            if (data != null && !data.Meets(referencePlayer, refe, (uint)miscValue1, (uint)miscValue2))
                return;

            switch (criteria.Entry.Type)
            {
                // std. case: increment at 1
                case CriteriaType.WinBattleground:
                case CriteriaType.TotalRespecs:
                case CriteriaType.LoseDuel:
                case CriteriaType.ItemsPostedAtAuction:
                case CriteriaType.AuctionsWon:    /* FIXME: for online player only currently */
                case CriteriaType.RollAnyNeed:
                case CriteriaType.RollAnyGreed:
                case CriteriaType.AbandonAnyQuest:
                case CriteriaType.BuyTaxi:
                case CriteriaType.AcceptSummon:
                case CriteriaType.LootAnyItem:
                case CriteriaType.ObtainAnyItem:
                case CriteriaType.DieAnywhere:
                case CriteriaType.CompleteDailyQuest:
                case CriteriaType.ParticipateInBattleground:
                case CriteriaType.DieOnMap:
                case CriteriaType.DieInInstance:
                case CriteriaType.KilledByCreature:
                case CriteriaType.KilledByPlayer:
                case CriteriaType.DieFromEnviromentalDamage:
                case CriteriaType.BeSpellTarget:
                case CriteriaType.GainAura:
                case CriteriaType.CastSpell:
                case CriteriaType.LandTargetedSpellOnTarget:
                case CriteriaType.WinAnyRankedArena:
                case CriteriaType.UseItem:
                case CriteriaType.RollNeed:
                case CriteriaType.RollGreed:
                case CriteriaType.DoEmote:
                case CriteriaType.UseGameobject:
                case CriteriaType.CatchFishInFishingHole:
                case CriteriaType.WinDuel:
                case CriteriaType.DeliverKillingBlowToClass:
                case CriteriaType.DeliverKillingBlowToRace:
                case CriteriaType.TrackedWorldStateUIModified:
                case CriteriaType.EarnHonorableKill:
                case CriteriaType.KillPlayer:
                case CriteriaType.DeliveredKillingBlow:
                case CriteriaType.PVPKillInArea:
                case CriteriaType.WinArena: // This also behaves like CriteriaType::WinAnyRankedArena
                case CriteriaType.ParticipateInArena:
                case CriteriaType.PlayerTriggerGameEvent:
                case CriteriaType.Login:
                case CriteriaType.AnyoneTriggerGameEventScenario:
                case CriteriaType.DefeatDungeonEncounterWhileElegibleForLoot:
                case CriteriaType.CompleteAnyScenario:
                case CriteriaType.CompleteScenario:
                case CriteriaType.BattlePetReachLevel:
                case CriteriaType.ActivelyEarnPetLevel:
                case CriteriaType.DefeatDungeonEncounter:
                case CriteriaType.PlaceGarrisonBuilding:
                case CriteriaType.ActivateAnyGarrisonBuilding:
                case CriteriaType.LearnAnyHeirloom:
                case CriteriaType.LearnAnyTransmog:
                case CriteriaType.HonorLevelIncrease:
                case CriteriaType.PrestigeLevelIncrease:
                case CriteriaType.LearnAnyTransmogInSlot:
                case CriteriaType.CompleteAnyReplayQuest:
                case CriteriaType.BuyItemsFromVendors:
                case CriteriaType.SellItemsToVendors:
                case CriteriaType.ReachMaxLevel:
                case CriteriaType.LearnTaxiNode:
                    SetCriteriaProgress(criteria, 1, referencePlayer, ProgressType.Accumulate);
                    break;
                // std case: increment at miscValue1
                case CriteriaType.MoneyEarnedFromSales:
                case CriteriaType.MoneySpentOnRespecs:
                case CriteriaType.MoneyEarnedFromQuesting:
                case CriteriaType.MoneySpentOnTaxis:
                case CriteriaType.MoneySpentAtBarberShop:
                case CriteriaType.MoneySpentOnPostage:
                case CriteriaType.MoneyLootedFromCreatures:
                case CriteriaType.MoneyEarnedFromAuctions:/* FIXME: for online player only currently */
                case CriteriaType.TotalDamageTaken:
                case CriteriaType.TotalHealReceived:
                case CriteriaType.CompletedLFGDungeon:
                case CriteriaType.CompletedLFGDungeonWithStrangers:
                case CriteriaType.DamageDealt:
                case CriteriaType.HealingDone:
                case CriteriaType.EarnArtifactXPForAzeriteItem:
                case CriteriaType.GainLevels:
                case CriteriaType.EarnArtifactXP:
                    SetCriteriaProgress(criteria, miscValue1, referencePlayer, ProgressType.Accumulate);
                    break;
                case CriteriaType.KillCreature:
                case CriteriaType.KillAnyCreature:
                case CriteriaType.GetLootByType:
                case CriteriaType.AcquireItem:
                case CriteriaType.LootItem:
                case CriteriaType.CurrencyGained:
                    SetCriteriaProgress(criteria, miscValue2, referencePlayer, ProgressType.Accumulate);
                    break;
                // std case: high value at miscValue1
                case CriteriaType.HighestAuctionBid:
                case CriteriaType.HighestAuctionSale: /* FIXME: for online player only currently */
                case CriteriaType.HighestDamageDone:
                case CriteriaType.HighestDamageTaken:
                case CriteriaType.HighestHealCast:
                case CriteriaType.HighestHealReceived:
                case CriteriaType.AnyArtifactPowerRankPurchased:
                case CriteriaType.AzeriteLevelReached:
                case CriteriaType.ReachRenownLevel:
                    SetCriteriaProgress(criteria, miscValue1, referencePlayer, ProgressType.Highest);
                    break;
                case CriteriaType.ReachLevel:
                    SetCriteriaProgress(criteria, referencePlayer.GetLevel(), referencePlayer);
                    break;
                case CriteriaType.SkillRaised:
                    uint skillvalue = referencePlayer.GetBaseSkillValue((SkillType)criteria.Entry.Asset);
                    if (skillvalue != 0)
                SetCriteriaProgress(criteria, skillvalue, referencePlayer);
                    break;
                case CriteriaType.AchieveSkillStep:
                    uint maxSkillvalue = referencePlayer.GetPureMaxSkillValue((SkillType)criteria.Entry.Asset);
                    if (maxSkillvalue != 0)
                SetCriteriaProgress(criteria, maxSkillvalue, referencePlayer);
                    break;
                case CriteriaType.CompleteQuestsCount:
                    SetCriteriaProgress(criteria, (ulong)referencePlayer.GetRewardedQuestCount(), referencePlayer);
                    break;
                case CriteriaType.CompleteAnyDailyQuestPerDay:
                {
                    long nextDailyResetTime = Global.WorldMgr.GetNextDailyQuestsResetTime();
                    CriteriaProgress progress = GetCriteriaProgress(criteria);

                    if (miscValue1 == 0) // Login case.
                    {
                        // reset if player missed one day.
                        if (progress != null && progress.Date < (nextDailyResetTime - 2 * Time.Day))
                            SetCriteriaProgress(criteria, 0, referencePlayer, ProgressType.Set);
                        return;
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
                        return;

                    SetCriteriaProgress(criteria, 1, referencePlayer, progressType);
                    break;
                }
                case CriteriaType.CompleteQuestsInZone:
                {
                    if (miscValue1 != 0)
                    {
                        SetCriteriaProgress(criteria, 1, referencePlayer, ProgressType.Accumulate);
                    }
                    else // login case
                    {
                        uint counter = 0;

                        var rewQuests = referencePlayer.GetRewardedQuests();
                        foreach (uint rewQuest in rewQuests)
                        {
                            Quest  quest = Global.ObjectMgr.GetQuestTemplate(rewQuest);
                            if (quest != null && quest.QuestSortID >= 0 && quest.QuestSortID == criteria.Entry.Asset)
                                ++counter;
                        }

                        SetCriteriaProgress(criteria, counter, referencePlayer, ProgressType.Highest);
                    }
                    break;
                }
                case CriteriaType.MaxDistFallenWithoutDying:
                    // miscValue1 is the ingame fallheight*100 as stored in dbc
                    SetCriteriaProgress(criteria, miscValue1, referencePlayer);
                    break;
                case CriteriaType.EarnAchievement:
                case CriteriaType.CompleteQuest:
                case CriteriaType.LearnOrKnowSpell:
                case CriteriaType.RevealWorldMapOverlay:
                case CriteriaType.GotHaircut:
                case CriteriaType.EquipItemInSlot:
                case CriteriaType.EquipItem:
                case CriteriaType.EnterAreaTriggerWithActionSet:
                case CriteriaType.LeaveAreaTriggerWithActionSet:
                case CriteriaType.LearnedNewPet:
                case CriteriaType.EnterArea:
                case CriteriaType.LeaveArea:
                case CriteriaType.RecruitGarrisonFollower:
                case CriteriaType.LearnHeirloom:
                case CriteriaType.ActivelyReachLevel:
                case CriteriaType.CollectTransmogSetFromGroup:
                case CriteriaType.EnterTopLevelArea:
                case CriteriaType.LeaveTopLevelArea:
                    SetCriteriaProgress(criteria, 1, referencePlayer);
                    break;
                case CriteriaType.BankSlotsPurchased:
                    SetCriteriaProgress(criteria, referencePlayer.GetBankBagSlotCount(), referencePlayer);
                    break;
                case CriteriaType.ReputationGained:
                {
                    int reputation = referencePlayer.GetReputationMgr().GetReputation(criteria.Entry.Asset);
                    if (reputation > 0)
                        SetCriteriaProgress(criteria, (ulong)reputation, referencePlayer);
                    break;
                }
                case CriteriaType.TotalExaltedFactions:
                    SetCriteriaProgress(criteria, referencePlayer.GetReputationMgr().GetExaltedFactionCount(), referencePlayer);
                    break;
                case CriteriaType.LearnSpellFromSkillLine:
                case CriteriaType.LearnTradeskillSkillLine:
                {
                    uint spellCount = 0;
                    foreach (var (spellId, _) in referencePlayer.GetSpellMap())
                    {
                        var bounds = Global.SpellMgr.GetSkillLineAbilityMapBounds(spellId);
                        foreach (var skillLineRecord in bounds)
                        {
                            if (skillLineRecord.SkillLine == criteria.Entry.Asset)
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
                case CriteriaType.TotalReveredFactions:
                    SetCriteriaProgress(criteria, referencePlayer.GetReputationMgr().GetReveredFactionCount(), referencePlayer);
                    break;
                case CriteriaType.TotalHonoredFactions:
                    SetCriteriaProgress(criteria, referencePlayer.GetReputationMgr().GetHonoredFactionCount(), referencePlayer);
                    break;
                case CriteriaType.TotalFactionsEncountered:
                    SetCriteriaProgress(criteria, referencePlayer.GetReputationMgr().GetVisibleFactionCount(), referencePlayer);
                    break;
                case CriteriaType.HonorableKills:
                    SetCriteriaProgress(criteria, referencePlayer.m_activePlayerData.LifetimeHonorableKills, referencePlayer);
                    break;
                case CriteriaType.MostMoneyOwned:
                    SetCriteriaProgress(criteria, referencePlayer.GetMoney(), referencePlayer, ProgressType.Highest);
                    break;
                case CriteriaType.EarnAchievementPoints:
                    if (miscValue1 == 0)
                        return;
                    SetCriteriaProgress(criteria, miscValue1, referencePlayer, ProgressType.Accumulate);
                    break;
                case CriteriaType.EarnPersonalArenaRating:
                {
                    uint reqTeamType = criteria.Entry.Asset;

                    if (miscValue1 != 0)
                    {
                        if (miscValue2 != reqTeamType)
                            return;

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
                case CriteriaType.UniquePetsOwned:
                    SetCriteriaProgress(criteria, referencePlayer.GetSession().GetBattlePetMgr().GetPetUniqueSpeciesCount(), referencePlayer);
                    break;
                case CriteriaType.GuildAttainedLevel:
                    SetCriteriaProgress(criteria, miscValue1, referencePlayer);
                    break;
                // FIXME: not triggered in code as result, need to implement
                case CriteriaType.RunInstance:
                case CriteriaType.EarnTeamArenaRating:
                case CriteriaType.EarnTitle:
                case CriteriaType.MoneySpentOnGuildRepair:
                case CriteriaType.CreatedItemsByCastingSpell:
                case CriteriaType.FishInAnyPool:
                case CriteriaType.GuildBankTabsPurchased:
                case CriteriaType.EarnGuildAchievementPoints:
                case CriteriaType.WinAnyBattleground:
                case CriteriaType.EarnBattlegroundRating:
                case CriteriaType.GuildTabardCreated:
                case CriteriaType.CompleteQuestsCountForGuild:
                case CriteriaType.HonorableKillsForGuild:
                case CriteriaType.KillAnyCreatureForGuild:
                case CriteriaType.CompleteAnyResearchProject:
                case CriteriaType.CompleteGuildChallenge:
                case CriteriaType.CompleteAnyGuildChallenge:
                case CriteriaType.CompletedLFRDungeon:
                case CriteriaType.AbandonedLFRDungeon:
                case CriteriaType.KickInitiatorInLFRDungeon:
                case CriteriaType.KickVoterInLFRDungeon:
                case CriteriaType.KickTargetInLFRDungeon:
                case CriteriaType.GroupedTankLeftEarlyInLFRDungeon:
                case CriteriaType.AccountObtainPetThroughBattle:
                case CriteriaType.WinPetBattle:
                case CriteriaType.PlayerObtainPetThroughBattle:
                case CriteriaType.ActivateGarrisonBuilding:
                case CriteriaType.UpgradeGarrison:
                case CriteriaType.StartAnyGarrisonMissionWithFollowerType:
                case CriteriaType.SucceedAnyGarrisonMissionWithFollowerType:
                case CriteriaType.SucceedGarrisonMission:
                case CriteriaType.RecruitAnyGarrisonFollower:
                case CriteriaType.LearnAnyGarrisonBlueprint:
                case CriteriaType.CollectGarrisonShipment:
                case CriteriaType.ItemLevelChangedForGarrisonFollower:
                case CriteriaType.LevelChangedForGarrisonFollower:
                case CriteriaType.LearnToy:
                case CriteriaType.LearnAnyToy:
                case CriteriaType.FindResearchObject:
                case CriteriaType.ExhaustAnyResearchSite:
                case CriteriaType.CompleteInternalCriteria:
                case CriteriaType.CompleteAnyChallengeMode:
                case CriteriaType.KilledAllUnitsInSpawnRegion:
                case CriteriaType.CompleteChallengeMode:
                case CriteriaType.CreatedItemsByCastingSpellWithLimit:
                case CriteriaType.BattlePetAchievementPointsEarned:
                case CriteriaType.ReleasedSpirit:
                case CriteriaType.AccountKnownPet:
                case CriteriaType.KickInitiatorInLFGDungeon:
                case CriteriaType.KickVoterInLFGDungeon:
                case CriteriaType.KickTargetInLFGDungeon:
                case CriteriaType.AbandonedLFGDungeon:
                case CriteriaType.GroupedTankLeftEarlyInLFGDungeon:
                case CriteriaType.StartGarrisonMission:
                case CriteriaType.QualityUpgradedForGarrisonFollower:
                case CriteriaType.CompleteResearchGarrisonTalent:
                case CriteriaType.RecruitAnyGarrisonTroop:
                case CriteriaType.CompleteAnyWorldQuest:
                case CriteriaType.ParagonLevelIncreaseWithFaction:
                case CriteriaType.PlayerHasEarnedHonor:
                case CriteriaType.ChooseRelicTalent:
                case CriteriaType.AccountHonorLevelReached:
                case CriteriaType.MythicPlusCompleted:
                case CriteriaType.SocketAnySoulbindConduit:
                case CriteriaType.ObtainAnyItemWithCurrencyValue:
                case CriteriaType.EarnExpansionLevel:
                case CriteriaType.LearnTransmog:
                default:
                    break;                          // Not implemented yet :(
            }

            foreach (CriteriaTree tree in trees)
            {
                if (IsCompletedCriteriaTree(tree))
                    CompletedCriteriaTree(tree, referencePlayer);

                AfterCriteriaTreeUpdate(tree, referencePlayer);
            }
        }

        public void UpdateTimedCriteria(TimeSpan timeDiff)
        {
            foreach (var key in _startedCriteria.Keys.ToList())
            {
                // Time is up, remove timer and reset progress
                if (_startedCriteria[key] <= timeDiff)
                {
                    RemoveCriteriaProgress(Global.CriteriaMgr.GetCriteria(key));

                    _startedCriteria.Remove(key);
                }
                else
                    _startedCriteria[key] -= timeDiff;
            }
        }

        public void StartCriteria(CriteriaStartEvent startEvent, uint entry, TimeSpan timeLost = default)
        {
            List<Criteria> criteriaList = Global.CriteriaMgr.GetCriteriaByStartEvent(startEvent, (int)entry);
            if (criteriaList.Empty())
                return;

            foreach (Criteria criteria in criteriaList)
            {
                TimeSpan timeLimit = TimeSpan.MaxValue; // this value is for criteria that have a start event requirement but no time limit
                if (criteria.Entry.StartTimer != 0)
                    timeLimit = TimeSpan.FromSeconds(criteria.Entry.StartTimer);

                timeLimit -= timeLost;

                if (timeLimit <= TimeSpan.Zero)
                    continue;

                List<CriteriaTree> trees = Global.CriteriaMgr.GetCriteriaTreesByCriteria(criteria.Id);
                bool canStart = trees.Any(tree => !IsCompletedCriteriaTree(tree));

                if (!canStart)
                    continue;

                bool isNew = _startedCriteria.TryAdd(criteria.Id, timeLimit);
                if (!isNew)
                {
                    if (!criteria.Entry.HasFlag(CriteriaFlags.ResetOnStart))
                        continue;

                    _startedCriteria[criteria.Id] = timeLimit;
                }

                // and at client too
                SetCriteriaProgress(criteria, 0, null, ProgressType.Set);
            }
        }

        public void FailCriteria(CriteriaFailEvent failEvent, uint asset)
        {
            List<Criteria> criteriaList = Global.CriteriaMgr.GetCriteriaByFailEvent(failEvent, (int)asset);
            if (criteriaList.Empty())
                return;

            foreach (Criteria criteria in criteriaList)
            {
                _startedCriteria.Remove(criteria.Id);

                List<CriteriaTree> trees = Global.CriteriaMgr.GetCriteriaTreesByCriteria(criteria.Id);
                bool allTreesFullyComplete = trees.All(tree =>
                {
                    CriteriaTree root = tree;
                    CriteriaTree parent = Global.CriteriaMgr.GetCriteriaTree(root.Entry.Parent);
                    if (parent != null)
                    {
                        do
                        {
                            root = parent;
                            parent = Global.CriteriaMgr.GetCriteriaTree(root.Entry.Parent);
                        } while (parent != null);
                    }

                    return IsCompletedCriteriaTree(root);
                });

                if (allTreesFullyComplete)
                    continue;

                RemoveCriteriaProgress(criteria);
            }
        }

        public CriteriaProgress GetCriteriaProgress(Criteria entry)
        {
            return _criteriaProgress.LookupByKey(entry.Id);
        }

        public void SetCriteriaProgress(Criteria criteria, ulong changeValue, Player referencePlayer, ProgressType progressType = ProgressType.Set)
        {
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
            progress.Date = GameTime.GetGameTime(); // set the date to the latest update.
            progress.PlayerGUID = referencePlayer != null ? referencePlayer.GetGUID() : ObjectGuid.Empty;
            _criteriaProgress[criteria.Id] = progress;

            TimeSpan timeElapsed = TimeSpan.Zero;
            if (criteria.Entry.StartTimer != 0)
            {
                if (_startedCriteria.TryGetValue(criteria.Id, out TimeSpan startedTime))
                {
                    // Client expects this in packet
                    timeElapsed = TimeSpan.FromSeconds(criteria.Entry.StartTimer) - startedTime;

                    // Remove the timer, we wont need it anymore
                    var trees = Global.CriteriaMgr.GetCriteriaTreesByCriteria(criteria.Id);

                    bool allTreesCompleted = trees.All(IsCompletedCriteriaTree);

                    if (allTreesCompleted)
                        _startedCriteria.Remove(criteria.Id);
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

            _criteriaProgress[criteria.Id].Counter = 0;
            _criteriaProgress[criteria.Id].Changed = true;
        }

        public bool IsCompletedCriteriaTree(CriteriaTree tree)
        {
            if (!CanCompleteCriteriaTree(tree))
                return false;

            ulong requiredCount = tree.Entry.Amount;
            switch ((CriteriaTreeOperator)tree.Entry.Operator)
            {
                case CriteriaTreeOperator.Complete:
                    return tree.Criteria != null && IsCompletedCriteria(tree.Criteria, requiredCount);
                case CriteriaTreeOperator.NotComplete:
                    return tree.Criteria == null || !IsCompletedCriteria(tree.Criteria, requiredCount);
                case CriteriaTreeOperator.CompleteAll:
                    foreach (CriteriaTree node in tree.Children)
                        if (!IsCompletedCriteriaTree(node))
                            return false;
                    return true;
                case CriteriaTreeOperator.Sum:
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
                case CriteriaTreeOperator.Highest:
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
                case CriteriaTreeOperator.StartedAtLeast:
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
                case CriteriaTreeOperator.CompleteAtLeast:
                {
                    ulong progress = 0;
                    foreach (CriteriaTree node in tree.Children)
                        if (IsCompletedCriteriaTree(node))
                            if (++progress >= requiredCount)
                                return true;

                    return false;
                }
                case CriteriaTreeOperator.ProgressBar:
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
            if ((tree.Entry.HasFlag(CriteriaTreeFlags.HordeOnly) && referencePlayer.GetTeam() != Team.Horde) ||
                (tree.Entry.HasFlag(CriteriaTreeFlags.AllianceOnly) && referencePlayer.GetTeam() != Team.Alliance))
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
                case CriteriaType.WinBattleground:
                case CriteriaType.KillCreature:
                case CriteriaType.ReachLevel:
                case CriteriaType.GuildAttainedLevel:
                case CriteriaType.SkillRaised:
                case CriteriaType.CompleteQuestsCount:
                case CriteriaType.CompleteAnyDailyQuestPerDay:
                case CriteriaType.CompleteQuestsInZone:
                case CriteriaType.DamageDealt:
                case CriteriaType.HealingDone:
                case CriteriaType.CompleteDailyQuest:
                case CriteriaType.MaxDistFallenWithoutDying:
                case CriteriaType.BeSpellTarget:
                case CriteriaType.GainAura:
                case CriteriaType.CastSpell:
                case CriteriaType.LandTargetedSpellOnTarget:
                case CriteriaType.TrackedWorldStateUIModified:
                case CriteriaType.PVPKillInArea:
                case CriteriaType.EarnHonorableKill:
                case CriteriaType.HonorableKills:
                case CriteriaType.AcquireItem:
                case CriteriaType.WinAnyRankedArena:
                case CriteriaType.EarnPersonalArenaRating:
                case CriteriaType.UseItem:
                case CriteriaType.LootItem:
                case CriteriaType.BankSlotsPurchased:
                case CriteriaType.ReputationGained:
                case CriteriaType.TotalExaltedFactions:
                case CriteriaType.RollNeed:
                case CriteriaType.RollGreed:
                case CriteriaType.DeliverKillingBlowToClass:
                case CriteriaType.DeliverKillingBlowToRace:
                case CriteriaType.DoEmote:
                case CriteriaType.MoneyEarnedFromQuesting:
                case CriteriaType.MoneyLootedFromCreatures:
                case CriteriaType.UseGameobject:
                case CriteriaType.KillPlayer:
                case CriteriaType.CatchFishInFishingHole:
                case CriteriaType.LearnSpellFromSkillLine:
                case CriteriaType.WinDuel:
                case CriteriaType.DefeatDungeonEncounterWhileElegibleForLoot:
                case CriteriaType.GetLootByType:
                case CriteriaType.LearnTradeskillSkillLine:
                case CriteriaType.CompletedLFGDungeon:
                case CriteriaType.CompletedLFGDungeonWithStrangers:
                case CriteriaType.DeliveredKillingBlow:
                case CriteriaType.CurrencyGained:
                case CriteriaType.CompleteAnyScenario:
                case CriteriaType.CompleteScenario:
                case CriteriaType.UniquePetsOwned:
                case CriteriaType.BattlePetReachLevel:
                case CriteriaType.ActivelyEarnPetLevel:
                case CriteriaType.DefeatDungeonEncounter:
                case CriteriaType.PlaceGarrisonBuilding:
                case CriteriaType.LearnHeirloom:
                case CriteriaType.LearnAnyHeirloom:
                case CriteriaType.EarnArtifactXP:
                case CriteriaType.AnyArtifactPowerRankPurchased:
                case CriteriaType.LearnAnyTransmogInSlot:
                case CriteriaType.ParagonLevelIncreaseWithFaction:
                case CriteriaType.PlayerHasEarnedHonor:
                case CriteriaType.ChooseRelicTalent:
                case CriteriaType.AccountHonorLevelReached:
                case CriteriaType.EarnArtifactXPForAzeriteItem:
                case CriteriaType.AzeriteLevelReached:
                case CriteriaType.CompleteAnyReplayQuest:
                case CriteriaType.BuyItemsFromVendors:
                case CriteriaType.SellItemsToVendors:
                case CriteriaType.GainLevels:
                case CriteriaType.ReachRenownLevel:
                case CriteriaType.LearnTaxiNode:
                    return progress.Counter >= requiredAmount;
                case CriteriaType.EarnAchievement:
                case CriteriaType.CompleteQuest:
                case CriteriaType.LearnOrKnowSpell:
                case CriteriaType.RevealWorldMapOverlay:
                case CriteriaType.GotHaircut:
                case CriteriaType.EquipItemInSlot:
                case CriteriaType.EquipItem:
                case CriteriaType.EnterAreaTriggerWithActionSet:
                case CriteriaType.LeaveAreaTriggerWithActionSet:
                case CriteriaType.LearnedNewPet:
                case CriteriaType.EnterArea:
                case CriteriaType.LeaveArea:
                case CriteriaType.RecruitGarrisonFollower:
                case CriteriaType.HonorLevelIncrease:
                case CriteriaType.PrestigeLevelIncrease:
                case CriteriaType.ActivelyReachLevel:
                case CriteriaType.CollectTransmogSetFromGroup:
                case CriteriaType.ReachMaxLevel:
                case CriteriaType.EnterTopLevelArea:
                case CriteriaType.LeaveTopLevelArea:
                    return progress.Counter >= 1;
                case CriteriaType.AchieveSkillStep:
                    return progress.Counter >= (requiredAmount * 75);
                case CriteriaType.EarnAchievementPoints:
                    return progress.Counter >= 9000;
                case CriteriaType.WinArena:
                case CriteriaType.ParticipateInArena:
                    return requiredAmount != 0 && progress.Counter >= requiredAmount;
                case CriteriaType.Login:
                    return true;
                // handle all statistic-only criteria here
                default:
                    break;
            }

            return false;
        }

        bool CanUpdateCriteria(Criteria criteria, List<CriteriaTree> trees, ulong miscValue1, ulong miscValue2, ulong miscValue3, WorldObject refe, Player referencePlayer)
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

            if (!RequirementsSatisfied(criteria, miscValue1, miscValue2, miscValue3, refe, referencePlayer))
            {
                Log.outTrace(LogFilter.Achievement, "CanUpdateCriteria: (Id: {0} Type {1}) Requirements not satisfied", criteria.Id, criteria.Entry.Type);
                return false;
            }

            if (criteria.Modifier != null && !ModifierTreeSatisfied(criteria.Modifier, miscValue1, miscValue2, refe, referencePlayer))
            {
                Log.outTrace(LogFilter.Achievement, "CanUpdateCriteria: (Id: {0} Type {1}) Requirements have not been satisfied", criteria.Id, criteria.Entry.Type);
                return false;
            }

            if (!ConditionsSatisfied(criteria, referencePlayer))
            {
                Log.outTrace(LogFilter.Achievement, "CanUpdateCriteria: (Id: {0} Type {1}) Conditions have not been satisfied", criteria.Id, criteria.Entry.Type);
                return false;
            }

            if (criteria.Entry.EligibilityWorldStateID != 0)
                if (Global.WorldStateMgr.GetValue(criteria.Entry.EligibilityWorldStateID, referencePlayer.GetMap()) != criteria.Entry.EligibilityWorldStateValue)
                    return false;

            return true;
        }

        bool ConditionsSatisfied(Criteria criteria, Player referencePlayer)
        {
            if (criteria.Entry.StartEvent != 0 && !_startedCriteria.ContainsKey(criteria.Id))
                return false;

            return true;
        }

        bool RequirementsSatisfied(Criteria criteria, ulong miscValue1, ulong miscValue2, ulong miscValue3, WorldObject refe, Player referencePlayer)
        {
            switch (criteria.Entry.Type)
            {
                case CriteriaType.AcceptSummon:
                case CriteriaType.CompleteDailyQuest:
                case CriteriaType.ItemsPostedAtAuction:
                case CriteriaType.MaxDistFallenWithoutDying:
                case CriteriaType.BuyTaxi:
                case CriteriaType.DeliveredKillingBlow:
                case CriteriaType.MoneyEarnedFromAuctions:
                case CriteriaType.MoneySpentAtBarberShop:
                case CriteriaType.MoneySpentOnPostage:
                case CriteriaType.MoneySpentOnRespecs:
                case CriteriaType.MoneySpentOnTaxis:
                case CriteriaType.HighestAuctionBid:
                case CriteriaType.HighestAuctionSale:
                case CriteriaType.HighestHealReceived:
                case CriteriaType.HighestHealCast:
                case CriteriaType.HighestDamageDone:
                case CriteriaType.HighestDamageTaken:
                case CriteriaType.EarnHonorableKill:
                case CriteriaType.LootAnyItem:
                case CriteriaType.MoneyLootedFromCreatures:
                case CriteriaType.LoseDuel:
                case CriteriaType.MoneyEarnedFromQuesting:
                case CriteriaType.MoneyEarnedFromSales:
                case CriteriaType.TotalRespecs:
                case CriteriaType.ObtainAnyItem:
                case CriteriaType.AbandonAnyQuest:
                case CriteriaType.GuildAttainedLevel:
                case CriteriaType.RollAnyGreed:
                case CriteriaType.RollAnyNeed:
                case CriteriaType.KillPlayer:
                case CriteriaType.TotalDamageTaken:
                case CriteriaType.TotalHealReceived:
                case CriteriaType.CompletedLFGDungeon:
                case CriteriaType.CompletedLFGDungeonWithStrangers:
                case CriteriaType.GotHaircut:
                case CriteriaType.WinDuel:
                case CriteriaType.WinAnyRankedArena:
                case CriteriaType.AuctionsWon:
                case CriteriaType.CompleteAnyReplayQuest:
                case CriteriaType.BuyItemsFromVendors:
                case CriteriaType.SellItemsToVendors:
                case CriteriaType.GainLevels:
                case CriteriaType.LearnAnyTransmog:
                case CriteriaType.CompleteAnyScenario:
                    if (miscValue1 == 0)
                        return false;
                    break;
                case CriteriaType.BankSlotsPurchased:
                case CriteriaType.CompleteAnyDailyQuestPerDay:
                case CriteriaType.CompleteQuestsCount:
                case CriteriaType.EarnAchievementPoints:
                case CriteriaType.TotalExaltedFactions:
                case CriteriaType.TotalHonoredFactions:
                case CriteriaType.TotalReveredFactions:
                case CriteriaType.MostMoneyOwned:
                case CriteriaType.EarnPersonalArenaRating:
                case CriteriaType.TotalFactionsEncountered:
                case CriteriaType.ReachLevel:
                case CriteriaType.Login:
                case CriteriaType.UniquePetsOwned:
                    break;
                case CriteriaType.EarnAchievement:
                    if (!RequiredAchievementSatisfied(criteria.Entry.Asset))
                        return false;
                    break;
                case CriteriaType.WinBattleground:
                case CriteriaType.ParticipateInBattleground:
                case CriteriaType.DieOnMap:
                    if (miscValue1 == 0 || criteria.Entry.Asset != referencePlayer.GetMapId())
                        return false;
                    break;
                case CriteriaType.KillCreature:
                case CriteriaType.KilledByCreature:
                    if (miscValue1 == 0 || criteria.Entry.Asset != miscValue1)
                        return false;
                    break;
                case CriteriaType.SkillRaised:
                case CriteriaType.AchieveSkillStep:
                    // update at loading or specific skill update
                    if (miscValue1 != 0 && miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaType.CompleteQuestsInZone:
                    if (miscValue1 != 0)
                    {
                        Quest quest = Global.ObjectMgr.GetQuestTemplate((uint)miscValue1);
                        if (quest == null || quest.QuestSortID != criteria.Entry.Asset)
                            return false;
                    }
                    break;
                case CriteriaType.DieAnywhere:
                {
                    if (miscValue1 == 0)
                        return false;
                    break;
                }
                case CriteriaType.DieInInstance:
                {
                    if (miscValue1 == 0)
                        return false;

                    Map map = referencePlayer.IsInWorld ? referencePlayer.GetMap() : Global.MapMgr.FindMap(referencePlayer.GetMapId(), referencePlayer.GetInstanceId());
                    if (map == null || !map.IsDungeon())
                        return false;

                    //FIXME: work only for instances where max == min for players
                    if (map.ToInstanceMap().GetMaxPlayers() != criteria.Entry.Asset)
                        return false;
                    break;
                }
                case CriteriaType.KilledByPlayer:
                    if (miscValue1 == 0 || refe == null || !refe.IsTypeId(TypeId.Player))
                        return false;
                    break;
                case CriteriaType.DieFromEnviromentalDamage:
                    if (miscValue1 == 0 || miscValue2 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaType.CompleteQuest:
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
                        if (!data.Meets(referencePlayer, refe))
                            return false;
                    break;
                }
                case CriteriaType.BeSpellTarget:
                case CriteriaType.GainAura:
                case CriteriaType.CastSpell:
                case CriteriaType.LandTargetedSpellOnTarget:
                    if (miscValue1 == 0 || miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaType.LearnOrKnowSpell:
                    if (miscValue1 != 0 && miscValue1 != criteria.Entry.Asset)
                        return false;

                    if (!referencePlayer.HasSpell(criteria.Entry.Asset))
                        return false;
                    break;
                case CriteriaType.GetLootByType:
                    // miscValue1 = itemId - miscValue2 = count of item loot
                    // miscValue3 = loot_type (note: 0 = LOOT_CORPSE and then it ignored)
                    if (miscValue1 == 0 || miscValue2 == 0 || miscValue3 == 0 || miscValue3 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaType.AcquireItem:
                    if (miscValue1 != 0 && criteria.Entry.Asset != miscValue1)
                        return false;
                    break;
                case CriteriaType.UseItem:
                case CriteriaType.LootItem:
                case CriteriaType.EquipItem:
                case CriteriaType.LearnHeirloom:
                    if (miscValue1 == 0 || criteria.Entry.Asset != miscValue1)
                        return false;
                    break;
                case CriteriaType.RevealWorldMapOverlay:
                {
                    WorldMapOverlayRecord worldOverlayEntry = CliDB.WorldMapOverlayStorage.LookupByKey(criteria.Entry.Asset);
                    if (worldOverlayEntry == null)
                        break;

                    bool matchFound = false;
                    for (uint j = 0; j < SharedConst.MaxWorldMapOverlayArea; ++j)
                    {
                        if (referencePlayer.HasExploredZone(j))
                        {
                            matchFound = true;
                            break;
                        }
                    }

                    if (!matchFound)
                        return false;
                    break;
                }
                case CriteriaType.ReputationGained:
                    if (miscValue1 != 0 && miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaType.EquipItemInSlot:
                case CriteriaType.LearnAnyTransmogInSlot:
                    // miscValue1 = EquipmentSlot miscValue2 = itemid | itemModifiedAppearanceId
                    if (miscValue2 == 0 || miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaType.RollNeed:
                case CriteriaType.RollGreed:
                {
                    // miscValue1 = itemid miscValue2 = diced value
                    if (miscValue1 == 0 || miscValue2 != criteria.Entry.Asset)
                        return false;

                    ItemTemplate proto = Global.ObjectMgr.GetItemTemplate((uint)miscValue1);
                    if (proto == null)
                        return false;
                    break;
                }
                case CriteriaType.DoEmote:
                    if (miscValue1 == 0 || miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaType.DamageDealt:
                case CriteriaType.HealingDone:
                    if (miscValue1 == 0)
                        return false;

                    if ((CriteriaFailEvent)criteria.Entry.FailEvent == CriteriaFailEvent.LeaveBattleground)
                    {
                        if (!referencePlayer.InBattleground())
                            return false;

                        // map specific case (BG in fact) expected player targeted damage/heal
                        if (refe == null || !refe.IsTypeId(TypeId.Player))
                            return false;
                    }
                    break;
                case CriteriaType.UseGameobject:
                case CriteriaType.CatchFishInFishingHole:
                    if (miscValue1 == 0 || miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaType.LearnSpellFromSkillLine:
                case CriteriaType.LearnTradeskillSkillLine:
                    if (miscValue1 != 0 && miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaType.DeliverKillingBlowToClass:
                    if (miscValue1 == 0 || miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaType.DeliverKillingBlowToRace:
                    if (miscValue1 == 0 || miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaType.TrackedWorldStateUIModified:
                    if (miscValue1 == 0 || miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaType.PVPKillInArea:
                case CriteriaType.EnterArea:
                    if (miscValue1 == 0 || !Global.DB2Mgr.IsInArea((uint)miscValue1, criteria.Entry.Asset))
                        return false;
                    break;
                case CriteriaType.LeaveArea:
                    if (miscValue1 == 0 || Global.DB2Mgr.IsInArea((uint)miscValue1, criteria.Entry.Asset))
                        return false;
                    break;
                case CriteriaType.CurrencyGained:
                case CriteriaType.ReachRenownLevel:
                    if (miscValue1 == 0 || miscValue2 == 0 || (long)miscValue2 < 0
                        || miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaType.WinArena:
                    if (miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaType.EarnTeamArenaRating:
                    return false;
                case CriteriaType.DefeatDungeonEncounterWhileElegibleForLoot:
                case CriteriaType.DefeatDungeonEncounter:
                    if (miscValue1 == 0 || miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaType.PlaceGarrisonBuilding:
                case CriteriaType.ActivateGarrisonBuilding:
                    if (miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaType.RecruitGarrisonFollower:
                    if (miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaType.CollectTransmogSetFromGroup:
                    if (miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaType.BattlePetReachLevel:
                case CriteriaType.ActivelyEarnPetLevel:
                    if (miscValue1 == 0 || miscValue2 == 0 || miscValue2 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaType.ActivelyReachLevel:
                    if (miscValue1 == 0 || miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaType.EnterTopLevelArea:
                case CriteriaType.LeaveTopLevelArea:
                    if (miscValue1 == 0 || miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaType.PlayerTriggerGameEvent:
                case CriteriaType.AnyoneTriggerGameEventScenario:
                    if (miscValue1 == 0 || miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaType.CompleteScenario:
                    if (miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaType.EnterAreaTriggerWithActionSet:
                case CriteriaType.LeaveAreaTriggerWithActionSet:
                    if (miscValue1 == 0 || miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                case CriteriaType.ReachMaxLevel:
                    if (!referencePlayer.IsMaxLevel())
                        return false;
                    break;
                case CriteriaType.LearnTaxiNode:
                    if (miscValue1 != criteria.Entry.Asset)
                        return false;
                    break;
                default:
                    break;
            }
            return true;
        }

        public bool ModifierTreeSatisfied(ModifierTreeNode tree, ulong miscValue1, ulong miscValue2, WorldObject refe, Player referencePlayer)
        {
            switch ((ModifierTreeOperator)tree.Entry.Operator)
            {
                case ModifierTreeOperator.SingleTrue:
                    return tree.Entry.Type != 0 && ModifierSatisfied(tree.Entry, miscValue1, miscValue2, refe, referencePlayer);
                case ModifierTreeOperator.SingleFalse:
                    return tree.Entry.Type != 0 && !ModifierSatisfied(tree.Entry, miscValue1, miscValue2, refe, referencePlayer);
                case ModifierTreeOperator.All:
                    foreach (ModifierTreeNode node in tree.Children)
                        if (!ModifierTreeSatisfied(node, miscValue1, miscValue2, refe, referencePlayer))
                            return false;
                    return true;
                case ModifierTreeOperator.Some:
                {
                    sbyte requiredAmount = Math.Max(tree.Entry.Amount, (sbyte)1);
                    foreach (ModifierTreeNode node in tree.Children)
                        if (ModifierTreeSatisfied(node, miscValue1, miscValue2, refe, referencePlayer))
                            if (--requiredAmount == 0)
                                return true;

                    return false;
                }
                default:
                    break;
            }

            return false;
        }

        bool ModifierSatisfied(ModifierTreeRecord modifier, ulong miscValue1, ulong miscValue2, WorldObject refe, Player referencePlayer)
        {
            uint reqValue = modifier.Asset;
            int secondaryAsset = modifier.SecondaryAsset;
            int tertiaryAsset = modifier.TertiaryAsset;

            switch ((ModifierTreeType)modifier.Type)
            {
                case ModifierTreeType.PlayerInebriationLevelEqualOrGreaterThan: // 1
                {
                    uint inebriation = (uint)Math.Min(Math.Max(referencePlayer.GetDrunkValue(), referencePlayer.m_playerData.FakeInebriation), 100);
                    if (inebriation < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerMeetsCondition: // 2
                {
                    if (!ConditionManager.IsPlayerMeetingCondition(referencePlayer, reqValue))
                        return false;
                    break;
                }
                case ModifierTreeType.MinimumItemLevel: // 3
                {
                    // miscValue1 is itemid
                    ItemTemplate item = Global.ObjectMgr.GetItemTemplate((uint)miscValue1);
                    if (item == null || item.GetBaseItemLevel() < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.TargetCreatureId: // 4
                    if (refe == null || refe.GetEntry() != reqValue)
                        return false;
                    break;
                case ModifierTreeType.TargetIsPlayer: // 5
                    if (refe == null || !refe.IsTypeId(TypeId.Player))
                        return false;
                    break;
                case ModifierTreeType.TargetIsDead: // 6
                    if (refe == null || !refe.IsUnit() || refe.ToUnit().IsAlive())
                        return false;
                    break;
                case ModifierTreeType.TargetIsOppositeFaction: // 7
                    if (refe == null || !referencePlayer.IsHostileTo(refe))
                        return false;
                    break;
                case ModifierTreeType.PlayerHasAura: // 8
                    if (!referencePlayer.HasAura(reqValue))
                        return false;
                    break;
                case ModifierTreeType.PlayerHasAuraEffect: // 9
                    if (!referencePlayer.HasAuraType((AuraType)reqValue))
                        return false;
                    break;
                case ModifierTreeType.TargetHasAura: // 10
                    if (refe == null || !refe.IsUnit() || !refe.ToUnit().HasAura(reqValue))
                        return false;
                    break;
                case ModifierTreeType.TargetHasAuraEffect: // 11
                    if (refe == null || !refe.IsUnit() || !refe.ToUnit().HasAuraType((AuraType)reqValue))
                        return false;
                    break;
                case ModifierTreeType.TargetHasAuraState: // 12
                    if (refe == null || !refe.IsUnit() || !refe.ToUnit().HasAuraState((AuraStateType)reqValue))
                        return false;
                    break;
                case ModifierTreeType.PlayerHasAuraState: // 13
                    if (!referencePlayer.HasAuraState((AuraStateType)reqValue))
                        return false;
                    break;
                case ModifierTreeType.ItemQualityIsAtLeast: // 14
                {
                    // miscValue1 is itemid
                    ItemTemplate item = Global.ObjectMgr.GetItemTemplate((uint)miscValue1);
                    if (item == null || (uint)item.GetQuality() < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.ItemQualityIsExactly: // 15
                {
                    // miscValue1 is itemid
                    ItemTemplate item = Global.ObjectMgr.GetItemTemplate((uint)miscValue1);
                    if (item == null || (uint)item.GetQuality() != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerIsAlive: // 16
                    if (referencePlayer.IsDead())
                        return false;
                    break;
                case ModifierTreeType.PlayerIsInArea: // 17
                {
                    if (!Global.DB2Mgr.IsInArea(referencePlayer.GetAreaId(), reqValue))
                        return false;
                    break;
                }
                case ModifierTreeType.TargetIsInArea: // 18
                {
                    if (refe == null)
                        return false;
                    if (!Global.DB2Mgr.IsInArea(refe.GetAreaId(), reqValue))
                        return false;
                    break;
                }
                case ModifierTreeType.ItemId: // 19
                    if (miscValue1 != reqValue)
                        return false;
                    break;
                case ModifierTreeType.LegacyDungeonDifficulty: // 20
                {
                    DifficultyRecord difficulty = CliDB.DifficultyStorage.LookupByKey(referencePlayer.GetMap().GetDifficultyID());
                    if (difficulty == null || difficulty.OldEnumValue == -1 || difficulty.OldEnumValue != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerToTargetLevelDeltaGreaterThan: // 21
                    if (refe == null || !refe.IsUnit() || referencePlayer.GetLevel() < refe.ToUnit().GetLevel() + reqValue)
                        return false;
                    break;
                case ModifierTreeType.TargetToPlayerLevelDeltaGreaterThan: // 22
                    if (refe == null || !refe.IsUnit() || referencePlayer.GetLevel() + reqValue < refe.ToUnit().GetLevel())
                        return false;
                    break;
                case ModifierTreeType.PlayerLevelEqualTargetLevel: // 23
                    if (refe == null || !refe.IsUnit() || referencePlayer.GetLevel() != refe.ToUnit().GetLevel())
                        return false;
                    break;
                case ModifierTreeType.PlayerInArenaWithTeamSize: // 24
                {
                    Battleground bg = referencePlayer.GetBattleground();
                    if (bg == null || !bg.IsArena() || bg.GetArenaType() != (ArenaTypes)reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerRace: // 25
                    if ((uint)referencePlayer.GetRace() != reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerClass: // 26
                    if ((uint)referencePlayer.GetClass() != reqValue)
                        return false;
                    break;
                case ModifierTreeType.TargetRace: // 27
                    if (refe == null || !refe.IsUnit() || refe.ToUnit().GetRace() != (Race)reqValue)
                        return false;
                    break;
                case ModifierTreeType.TargetClass: // 28
                    if (refe == null || !refe.IsUnit() || refe.ToUnit().GetClass() != (Class)reqValue)
                        return false;
                    break;
                case ModifierTreeType.LessThanTappers: // 29
                    if (referencePlayer.GetGroup() != null && referencePlayer.GetGroup().GetMembersCount() >= reqValue)
                        return false;
                    break;
                case ModifierTreeType.CreatureType: // 30
                {
                    if (refe == null)
                        return false;

                    if (!refe.IsUnit() || refe.ToUnit().GetCreatureType() != (CreatureType)reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.CreatureFamily: // 31
                {
                    if (refe == null)
                        return false;
                    if (!refe.IsCreature() || refe.ToCreature().GetCreatureTemplate().Family != (CreatureFamily)reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerMap: // 32
                    if (referencePlayer.GetMapId() != reqValue)
                        return false;
                    break;
                case ModifierTreeType.ClientVersionEqualOrLessThan: // 33
                    var currentRealm = Global.RealmMgr.GetCurrentRealm();
                    if (currentRealm == null)
                        return false;
                    if (reqValue < ClientBuildHelper.GetMinorMajorBugfixVersionForBuild(currentRealm.Build))
                        return false;
                    break;
                case ModifierTreeType.BattlePetTeamLevel: // 34
                    foreach (BattlePetSlot slot in referencePlayer.GetSession().GetBattlePetMgr().GetSlots())
                        if (slot.Pet.Level < reqValue)
                            return false;
                    break;
                case ModifierTreeType.PlayerIsNotInParty: // 35
                    if (referencePlayer.GetGroup() != null)
                        return false;
                    break;
                case ModifierTreeType.PlayerIsInParty: // 36
                    if (referencePlayer.GetGroup() == null)
                        return false;
                    break;
                case ModifierTreeType.HasPersonalRatingEqualOrGreaterThan: // 37
                    if (referencePlayer.GetMaxPersonalArenaRatingRequirement(0) < reqValue)
                        return false;
                    break;
                case ModifierTreeType.HasTitle: // 38
                    if (!referencePlayer.HasTitle(reqValue))
                        return false;
                    break;
                case ModifierTreeType.PlayerLevelEqual: // 39
                    if (referencePlayer.GetLevel() != reqValue)
                        return false;
                    break;
                case ModifierTreeType.TargetLevelEqual: // 40
                    if (refe == null || refe.GetLevelForTarget(referencePlayer) != reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerIsInZone: // 41
                {
                    uint zoneId = referencePlayer.GetAreaId();
                    AreaTableRecord areaEntry = CliDB.AreaTableStorage.LookupByKey(zoneId);
                    if (areaEntry != null && areaEntry.HasFlag(AreaFlags.IsSubzone))
                        zoneId = areaEntry.ParentAreaID;
                    if (zoneId != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.TargetIsInZone: // 42
                {
                    if (refe == null)
                        return false;
                    uint zoneId = refe.GetAreaId();
                    AreaTableRecord areaEntry = CliDB.AreaTableStorage.LookupByKey(zoneId);
                    if (areaEntry != null && areaEntry.HasFlag(AreaFlags.IsSubzone))
                        zoneId = areaEntry.ParentAreaID;
                    if (zoneId != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHealthBelowPercent: // 43
                    if (referencePlayer.GetHealthPct() > (float)reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerHealthAbovePercent: // 44
                    if (referencePlayer.GetHealthPct() < (float)reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerHealthEqualsPercent: // 45
                    if (referencePlayer.GetHealthPct() != (float)reqValue)
                        return false;
                    break;
                case ModifierTreeType.TargetHealthBelowPercent: // 46
                    if (refe == null || !refe.IsUnit() || refe.ToUnit().GetHealthPct() > reqValue)
                        return false;
                    break;
                case ModifierTreeType.TargetHealthAbovePercent: // 47
                    if (refe == null || !refe.IsUnit() || refe.ToUnit().GetHealthPct() < reqValue)
                        return false;
                    break;
                case ModifierTreeType.TargetHealthEqualsPercent: // 48
                    if (refe == null || !refe.IsUnit() || refe.ToUnit().GetHealthPct() != reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerHealthBelowValue: // 49
                    if (referencePlayer.GetHealth() > reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerHealthAboveValue: // 50
                    if (referencePlayer.GetHealth() < reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerHealthEqualsValue: // 51
                    if (referencePlayer.GetHealth() != reqValue)
                        return false;
                    break;
                case ModifierTreeType.TargetHealthBelowValue: // 52
                    if (refe == null || !refe.IsUnit() || refe.ToUnit().GetHealth() > reqValue)
                        return false;
                    break;
                case ModifierTreeType.TargetHealthAboveValue: // 53
                    if (refe == null || !refe.IsUnit() || refe.ToUnit().GetHealth() < reqValue)
                        return false;
                    break;
                case ModifierTreeType.TargetHealthEqualsValue: // 54
                    if (refe == null || !refe.IsUnit() || refe.ToUnit().GetHealth() != reqValue)
                        return false;
                    break;
                case ModifierTreeType.TargetIsPlayerAndMeetsCondition: // 55
                {
                    if (refe == null || !refe.IsPlayer())
                        return false;

                    if (!ConditionManager.IsPlayerMeetingCondition(refe.ToPlayer(), reqValue))
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHasMoreThanAchievementPoints: // 56
                    if (referencePlayer.GetAchievementPoints() <= reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerInLfgDungeon: // 57
                    if (ConditionManager.GetPlayerConditionLfgValue(referencePlayer, PlayerConditionLfgStatus.InLFGDungeon) == 0)
                        return false;
                    break;
                case ModifierTreeType.PlayerInRandomLfgDungeon: // 58
                    if (ConditionManager.GetPlayerConditionLfgValue(referencePlayer, PlayerConditionLfgStatus.InLFGRandomDungeon) == 0)
                        return false;
                    break;
                case ModifierTreeType.PlayerInFirstRandomLfgDungeon: // 59
                    if (ConditionManager.GetPlayerConditionLfgValue(referencePlayer, PlayerConditionLfgStatus.InLFGFirstRandomDungeon) == 0)
                        return false;
                    break;
                case ModifierTreeType.PlayerInRankedArenaMatch: // 60
                {
                    Battleground bg = referencePlayer.GetBattleground();
                    if (bg == null || !bg.IsArena() || !bg.IsRated())
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerInGuildParty: // 61 NYI
                    return false;
                case ModifierTreeType.PlayerGuildReputationEqualOrGreaterThan: // 62
                    if (referencePlayer.GetReputationMgr().GetReputation(1168) < reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerInRatedBattleground: // 63
                {
                    Battleground bg = referencePlayer.GetBattleground();
                    if (bg == null || !bg.IsBattleground() || !bg.IsRated())
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerBattlegroundRatingEqualOrGreaterThan: // 64
                    if (referencePlayer.GetRBGPersonalRating() < reqValue)
                        return false;
                    break;
                case ModifierTreeType.ResearchProjectRarity: // 65 NYI
                case ModifierTreeType.ResearchProjectBranch: // 66 NYI
                    return false;
                case ModifierTreeType.WorldStateExpression: // 67
                    WorldStateExpressionRecord worldStateExpression = CliDB.WorldStateExpressionStorage.LookupByKey(reqValue);
                    if (worldStateExpression != null)
                        return ConditionManager.IsMeetingWorldStateExpression(referencePlayer.GetMap(), worldStateExpression);
                    return false;
                case ModifierTreeType.DungeonDifficulty: // 68
                    if (referencePlayer.GetMap().GetDifficultyID() != (Difficulty)reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerLevelEqualOrGreaterThan: // 69
                    if (referencePlayer.GetLevel() < reqValue)
                        return false;
                    break;
                case ModifierTreeType.TargetLevelEqualOrGreaterThan: // 70
                    if (refe == null || !refe.IsUnit() || refe.ToUnit().GetLevel() < reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerLevelEqualOrLessThan: // 71
                    if (referencePlayer.GetLevel() > reqValue)
                        return false;
                    break;
                case ModifierTreeType.TargetLevelEqualOrLessThan: // 72
                    if (refe == null || !refe.IsUnit() || refe.ToUnit().GetLevel() > reqValue)
                        return false;
                    break;
                case ModifierTreeType.ModifierTree: // 73
                    ModifierTreeNode nextModifierTree = Global.CriteriaMgr.GetModifierTree(reqValue);
                    if (nextModifierTree != null)
                        return ModifierTreeSatisfied(nextModifierTree, miscValue1, miscValue2, refe, referencePlayer);
                    return false;
                case ModifierTreeType.PlayerScenario: // 74
                {
                    Scenario scenario = referencePlayer.GetScenario();
                    if (scenario == null || scenario.GetEntry().Id != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.TillersReputationGreaterThan: // 75
                    if (referencePlayer.GetReputationMgr().GetReputation(1272) < reqValue)
                        return false;
                    break;
                case ModifierTreeType.BattlePetAchievementPointsEqualOrGreaterThan: // 76
                {
                    static short getRootAchievementCategory(AchievementRecord achievement)
                    {
                        short category = (short)achievement.Category;
                        do
                        {
                            var categoryEntry = CliDB.AchievementCategoryStorage.LookupByKey(category);
                            if (categoryEntry?.Parent == -1)
                                break;

                            category = categoryEntry.Parent;
                        } while (true);

                        return category;
                    }

                    uint petAchievementPoints = 0;
                    foreach (uint achievementId in referencePlayer.GetCompletedAchievementIds())
                    {
                        var achievement = CliDB.AchievementStorage.LookupByKey(achievementId);
                        if (getRootAchievementCategory(achievement) == SharedConst.AchivementCategoryPetBattles)
                            petAchievementPoints += achievement.Points;
                    }

                    if (petAchievementPoints < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.UniqueBattlePetsEqualOrGreaterThan: // 77
                    if (referencePlayer.GetSession().GetBattlePetMgr().GetPetUniqueSpeciesCount() < reqValue)
                        return false;
                    break;
                case ModifierTreeType.BattlePetType: // 78
                {
                    var speciesEntry = CliDB.BattlePetSpeciesStorage.LookupByKey(miscValue1);
                    if (speciesEntry?.PetTypeEnum != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.BattlePetHealthPercentLessThan: // 79 NYI - use target battle pet here, the one we were just battling
                    return false;
                case ModifierTreeType.GuildGroupMemberCountEqualOrGreaterThan: // 80
                {
                    uint guildMemberCount = 0;
                    var group = referencePlayer.GetGroup();
                    if (group != null)
                    {
                        for (var itr = group.GetFirstMember(); itr != null; itr = itr.Next())
                            if (itr.GetSource().GetGuildId() == referencePlayer.GetGuildId())
                                ++guildMemberCount;
                    }

                    if (guildMemberCount < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.BattlePetOpponentCreatureId: // 81 NYI
                    return false;
                case ModifierTreeType.PlayerScenarioStep: // 82
                {
                    Scenario scenario = referencePlayer.GetScenario();
                    if (scenario == null)
                        return false;

                    if (scenario.GetStep().OrderIndex != (reqValue - 1))
                        return false;
                    break;
                }
                case ModifierTreeType.ChallengeModeMedal: // 83
                    return false; // OBSOLETE
                case ModifierTreeType.PlayerOnQuest: // 84
                    if (referencePlayer.FindQuestSlot(reqValue) == SharedConst.MaxQuestLogSize)
                        return false;
                    break;
                case ModifierTreeType.ExaltedWithFaction: // 85
                    if (referencePlayer.GetReputationMgr().GetReputation(reqValue) < 42000)
                        return false;
                    break;
                case ModifierTreeType.EarnedAchievementOnAccount: // 86
                case ModifierTreeType.EarnedAchievementOnPlayer: // 87
                    if (!referencePlayer.HasAchieved(reqValue))
                        return false;
                    break;
                case ModifierTreeType.OrderOfTheCloudSerpentReputationGreaterThan: // 88
                    if (referencePlayer.GetReputationMgr().GetReputation(1271) < reqValue)
                        return false;
                    break;
                case ModifierTreeType.BattlePetQuality: // 89 NYI
                case ModifierTreeType.BattlePetFightWasPVP: // 90 NYI
                    return false;
                case ModifierTreeType.BattlePetSpecies: // 91
                    if (miscValue1 != reqValue)
                        return false;
                    break;
                case ModifierTreeType.ServerExpansionEqualOrGreaterThan: // 92
                    if (WorldConfig.GetIntValue(WorldCfg.Expansion) < reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerHasBattlePetJournalLock: // 93
                    if (!referencePlayer.GetSession().GetBattlePetMgr().HasJournalLock())
                        return false;
                    break;
                case ModifierTreeType.FriendshipRepReactionIsMet: // 94
                {
                    var friendshipRepReaction = CliDB.FriendshipRepReactionStorage.LookupByKey(reqValue);
                    if (friendshipRepReaction == null)
                        return false;

                    var friendshipReputation = CliDB.FriendshipReputationStorage.LookupByKey(friendshipRepReaction.FriendshipRepID);
                    if (friendshipReputation == null)
                        return false;

                    if (referencePlayer.GetReputation((uint)friendshipReputation.FactionID) < friendshipRepReaction.ReactionThreshold)
                        return false;
                    break;
                }
                case ModifierTreeType.ReputationWithFactionIsEqualOrGreaterThan: // 95
                    if (referencePlayer.GetReputationMgr().GetReputation(reqValue) < reqValue)
                        return false;
                    break;
                case ModifierTreeType.ItemClassAndSubclass: // 96
                {
                    ItemTemplate item = Global.ObjectMgr.GetItemTemplate((uint)miscValue1);
                    if (item == null || item.GetClass() != (ItemClass)reqValue || item.GetSubClass() != secondaryAsset)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerGender: // 97
                    if ((int)referencePlayer.GetGender() != reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerNativeGender: // 98
                    if (referencePlayer.GetNativeGender() != (Gender)reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerSkillEqualOrGreaterThan: // 99
                    if (referencePlayer.GetPureSkillValue((SkillType)reqValue) < secondaryAsset)
                        return false;
                    break;
                case ModifierTreeType.PlayerLanguageSkillEqualOrGreaterThan: // 100
                {
                    var languageDescs = Global.LanguageMgr.GetLanguageDescById((Language)reqValue);
                    if (!languageDescs.Any(desc => referencePlayer.GetSkillValue((SkillType)desc.SkillId) >= secondaryAsset))
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerIsInNormalPhase: // 101
                    if (!PhasingHandler.InDbPhaseShift(referencePlayer, 0, 0, 0))
                        return false;
                    break;
                case ModifierTreeType.PlayerIsInPhase: // 102
                    if (!PhasingHandler.InDbPhaseShift(referencePlayer, 0, (ushort)reqValue, 0))
                        return false;
                    break;
                case ModifierTreeType.PlayerIsInPhaseGroup: // 103
                    if (!PhasingHandler.InDbPhaseShift(referencePlayer, 0, 0, reqValue))
                        return false;
                    break;
                case ModifierTreeType.PlayerKnowsSpell: // 104
                    if (!referencePlayer.HasSpell(reqValue))
                        return false;
                    break;
                case ModifierTreeType.PlayerHasItemQuantity: // 105
                    if (referencePlayer.GetItemCount(reqValue, false) < secondaryAsset)
                        return false;
                    break;
                case ModifierTreeType.PlayerExpansionLevelEqualOrGreaterThan: // 106
                    if (referencePlayer.GetSession().GetExpansion() < (Expansion)reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerHasAuraWithLabel: // 107
                    if (!referencePlayer.HasAura(aura => aura.GetSpellInfo().HasLabel(reqValue)))
                        return false;
                    break;
                case ModifierTreeType.PlayersRealmWorldState: // 108
                    if (Global.WorldStateMgr.GetValue((int)reqValue, referencePlayer.GetMap()) != secondaryAsset)
                        return false;
                    break;
                case ModifierTreeType.TimeBetween: // 109
                {
                    WowTime from = new();
                    from.SetPackedTime(reqValue);
                    WowTime to = new();
                    to.SetPackedTime((uint)secondaryAsset);

                    if (!GameTime.GetWowTime().IsInRange(from, to))
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHasCompletedQuest: // 110
                    if (!referencePlayer.IsQuestCompletedBitSet(reqValue))
                        return false;
                    break;
                case ModifierTreeType.PlayerIsReadyToTurnInQuest: // 111
                    if (referencePlayer.GetQuestStatus(reqValue) != QuestStatus.Complete)
                        return false;
                    break;
                case ModifierTreeType.PlayerHasCompletedQuestObjective: // 112
                {
                    QuestObjective objective = Global.ObjectMgr.GetQuestObjective(reqValue);
                    if (objective == null)
                        return false;

                    Quest quest = Global.ObjectMgr.GetQuestTemplate(objective.QuestID);
                    if (quest == null)
                        return false;

                    ushort slot = referencePlayer.FindQuestSlot(objective.QuestID);
                    if (slot >= SharedConst.MaxQuestLogSize || referencePlayer.GetQuestRewardStatus(objective.QuestID) || !referencePlayer.IsQuestObjectiveComplete(slot, quest, objective))
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHasExploredArea: // 113
                {
                    if (!referencePlayer.HasExploredZone(reqValue))
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHasItemQuantityIncludingBank: // 114
                    if (referencePlayer.GetItemCount(reqValue, true) < secondaryAsset)
                        return false;
                    break;
                case ModifierTreeType.Weather: // 115
                    if (referencePlayer.GetMap().GetZoneWeather(referencePlayer.GetZoneId()) != (WeatherState)reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerFaction: // 116
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
                case ModifierTreeType.LfgStatusEqual: // 117
                    if (ConditionManager.GetPlayerConditionLfgValue(referencePlayer, (PlayerConditionLfgStatus)reqValue) != secondaryAsset)
                        return false;
                    break;
                case ModifierTreeType.LFgStatusEqualOrGreaterThan: // 118
                    if (ConditionManager.GetPlayerConditionLfgValue(referencePlayer, (PlayerConditionLfgStatus)reqValue) < secondaryAsset)
                        return false;
                    break;
                case ModifierTreeType.PlayerHasCurrencyEqualOrGreaterThan: // 119
                    if (!referencePlayer.HasCurrency(reqValue, (uint)secondaryAsset))
                        return false;
                    break;
                case ModifierTreeType.TargetThreatListSizeLessThan: // 120
                {
                    if (refe == null)
                        return false;
                    Unit unitRef = refe.ToUnit();
                    if (unitRef == null || !unitRef.CanHaveThreatList())
                        return false;
                    if (unitRef.GetThreatManager().GetThreatListSize() >= reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHasTrackedCurrencyEqualOrGreaterThan: // 121
                    if (referencePlayer.GetCurrencyTrackedQuantity(reqValue) < secondaryAsset)
                        return false;
                    break;
                case ModifierTreeType.PlayerMapInstanceType: // 122
                    if ((uint)referencePlayer.GetMap().GetEntry().InstanceType != reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerInTimeWalkerInstance: // 123
                    if (!referencePlayer.HasPlayerFlag(PlayerFlags.Timewalking))
                        return false;
                    break;
                case ModifierTreeType.PvpSeasonIsActive: // 124
                    if (!WorldConfig.GetBoolValue(WorldCfg.ArenaSeasonInProgress))
                        return false;
                    break;
                case ModifierTreeType.PvpSeason: // 125
                    if (WorldConfig.GetIntValue(WorldCfg.ArenaSeasonId) != reqValue)
                        return false;
                    break;
                case ModifierTreeType.GarrisonTierEqualOrGreaterThan: // 126
                {
                    Garrison garrison = referencePlayer.GetGarrison();
                    if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)secondaryAsset || garrison.GetSiteLevel().GarrLevel < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.GarrisonFollowersWithLevelEqualOrGreaterThan: // 127
                {
                    Garrison garrison = referencePlayer.GetGarrison();
                    if (garrison == null)
                        return false;

                    uint followerCount = garrison.CountFollowers(follower =>
                    {
                        var garrFollower = CliDB.GarrFollowerStorage.LookupByKey(follower.PacketInfo.GarrFollowerID);
                        return garrFollower?.GarrFollowerTypeID == tertiaryAsset && follower.PacketInfo.FollowerLevel >= secondaryAsset;
                    });

                    if (followerCount < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.GarrisonFollowersWithQualityEqualOrGreaterThan: // 128
                {
                    Garrison garrison = referencePlayer.GetGarrison();
                    if (garrison == null)
                        return false;

                    uint followerCount = garrison.CountFollowers(follower =>
                    {
                        var garrFollower = CliDB.GarrFollowerStorage.LookupByKey(follower.PacketInfo.GarrFollowerID);
                        return garrFollower?.GarrFollowerTypeID == tertiaryAsset && follower.PacketInfo.Quality >= secondaryAsset;
                    });

                    if (followerCount < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.GarrisonFollowerWithAbilityAtLevelEqualOrGreaterThan: // 129
                {
                    Garrison garrison = referencePlayer.GetGarrison();
                    if (garrison == null)
                        return false;

                    uint followerCount = garrison.CountFollowers(follower =>
                    {
                        var garrFollower = CliDB.GarrFollowerStorage.LookupByKey(follower.PacketInfo.GarrFollowerID);
                        return garrFollower?.GarrFollowerTypeID == tertiaryAsset && follower.PacketInfo.FollowerLevel >= reqValue && follower.HasAbility((uint)secondaryAsset);
                    });

                    if (followerCount < 1)
                        return false;
                    break;
                }
                case ModifierTreeType.GarrisonFollowerWithTraitAtLevelEqualOrGreaterThan: // 130
                {
                    Garrison garrison = referencePlayer.GetGarrison();
                    if (garrison == null)
                        return false;

                    GarrAbilityRecord traitEntry = CliDB.GarrAbilityStorage.LookupByKey(secondaryAsset);
                    if (traitEntry == null || !traitEntry.HasFlag(GarrisonAbilityFlags.Trait))
                        return false;

                    uint followerCount = garrison.CountFollowers(follower =>
                    {
                        var garrFollower = CliDB.GarrFollowerStorage.LookupByKey(follower.PacketInfo.GarrFollowerID);
                        return garrFollower?.GarrFollowerTypeID == tertiaryAsset && follower.PacketInfo.FollowerLevel >= reqValue && follower.HasAbility((uint)secondaryAsset);
                    });

                    if (followerCount < 1)
                        return false;
                    break;
                }
                case ModifierTreeType.GarrisonFollowerWithAbilityAssignedToBuilding: // 131
                {
                    Garrison garrison = referencePlayer.GetGarrison();
                    if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                        return false;

                    uint followerCount = garrison.CountFollowers(follower =>
                    {
                        GarrBuildingRecord followerBuilding = CliDB.GarrBuildingStorage.LookupByKey(follower.PacketInfo.CurrentBuildingID);
                        if (followerBuilding == null)
                            return false;

                        return followerBuilding.BuildingType == secondaryAsset && follower.HasAbility(reqValue);
                    });

                    if (followerCount < 1)
                        return false;
                    break;
                }
                case ModifierTreeType.GarrisonFollowerWithTraitAssignedToBuilding: // 132
                {
                    Garrison garrison = referencePlayer.GetGarrison();
                    if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                        return false;

                    GarrAbilityRecord traitEntry = CliDB.GarrAbilityStorage.LookupByKey(reqValue);
                    if (traitEntry == null || !traitEntry.HasFlag(GarrisonAbilityFlags.Trait))
                        return false;

                    uint followerCount = garrison.CountFollowers(follower =>
                    {
                        GarrBuildingRecord followerBuilding = CliDB.GarrBuildingStorage.LookupByKey(follower.PacketInfo.CurrentBuildingID);
                        if (followerBuilding == null)
                            return false;

                        return followerBuilding.BuildingType == secondaryAsset && follower.HasAbility(reqValue);
                    });

                    if (followerCount < 1)
                        return false;
                    break;
                }
                case ModifierTreeType.GarrisonFollowerWithLevelAssignedToBuilding: // 133
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
                case ModifierTreeType.GarrisonBuildingWithLevelEqualOrGreaterThan: // 134
                {
                    Garrison garrison = referencePlayer.GetGarrison();
                    if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                        return false;

                    foreach (Garrison.Plot plot in garrison.GetPlots())
                    {
                        if (plot.BuildingInfo.PacketInfo == null)
                            continue;

                        GarrBuildingRecord building = CliDB.GarrBuildingStorage.LookupByKey(plot.BuildingInfo.PacketInfo.GarrBuildingID);
                        if (building == null || building.UpgradeLevel < reqValue || building.BuildingType != secondaryAsset)
                            continue;

                        return true;
                    }
                    return false;
                }
                case ModifierTreeType.HasBlueprintForGarrisonBuilding: // 135
                {
                    Garrison garrison = referencePlayer.GetGarrison();
                    if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)secondaryAsset)
                        return false;

                    if (!garrison.HasBlueprint(reqValue))
                        return false;
                    break;
                }
                case ModifierTreeType.HasGarrisonBuildingSpecialization: // 136
                    return false; // OBSOLETE
                case ModifierTreeType.AllGarrisonPlotsAreFull: // 137
                {
                    Garrison garrison = referencePlayer.GetGarrison();
                    if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)reqValue)
                        return false;

                    foreach (var plot in garrison.GetPlots())
                        if (plot.BuildingInfo.PacketInfo == null)
                            return false;
                    break;
                }
                case ModifierTreeType.PlayerIsInOwnGarrison: // 138
                    if (!referencePlayer.GetMap().IsGarrison() || referencePlayer.GetMap().GetInstanceId() != referencePlayer.GetGUID().GetCounter())
                        return false;
                    break;
                case ModifierTreeType.GarrisonShipmentOfTypeIsPending: // 139 NYI
                    return false;
                case ModifierTreeType.GarrisonBuildingIsUnderConstruction: // 140
                {
                    GarrBuildingRecord building = CliDB.GarrBuildingStorage.LookupByKey(reqValue);
                    if (building == null)
                        return false;

                    Garrison garrison = referencePlayer.GetGarrison();
                    if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                        return false;

                    foreach (Garrison.Plot plot in garrison.GetPlots())
                    {
                        if (plot.BuildingInfo.PacketInfo == null || plot.BuildingInfo.PacketInfo.GarrBuildingID != reqValue)
                            continue;

                        return !plot.BuildingInfo.PacketInfo.Active;
                    }
                    return false;
                }
                case ModifierTreeType.GarrisonMissionHasBeenCompleted: // 141 NYI
                    return false;
                case ModifierTreeType.GarrisonBuildingLevelEqual: // 142
                {
                    Garrison garrison = referencePlayer.GetGarrison();
                    if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                        return false;

                    foreach (Garrison.Plot plot in garrison.GetPlots())
                    {
                        if (plot.BuildingInfo.PacketInfo == null)
                            continue;

                        GarrBuildingRecord building = CliDB.GarrBuildingStorage.LookupByKey(plot.BuildingInfo.PacketInfo.GarrBuildingID);
                        if (building == null || building.UpgradeLevel != secondaryAsset || building.BuildingType != reqValue)
                            continue;

                        return true;
                    }
                    return false;
                }
                case ModifierTreeType.GarrisonFollowerHasAbility: // 143
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
                case ModifierTreeType.GarrisonFollowerHasTrait: // 144
                {
                    GarrAbilityRecord traitEntry = CliDB.GarrAbilityStorage.LookupByKey(reqValue);
                    if (traitEntry == null || !traitEntry.HasFlag(GarrisonAbilityFlags.Trait))
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
                case ModifierTreeType.GarrisonFollowerQualityEqual: // 145
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
                case ModifierTreeType.GarrisonFollowerLevelEqual: // 146
                {
                    Garrison garrison = referencePlayer.GetGarrison();
                    if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)secondaryAsset)
                        return false;

                    if (miscValue1 != 0)
                    {
                        Garrison.Follower follower = garrison.GetFollower(miscValue1);
                        if (follower == null || follower.PacketInfo.FollowerLevel != reqValue)
                            return false;
                    }
                    else
                    {
                        uint followerCount = garrison.CountFollowers(follower =>
                        {
                            return follower.PacketInfo.FollowerLevel == reqValue;
                        });

                        if (followerCount < 1)
                            return false;
                    }
                    break;
                }
                case ModifierTreeType.GarrisonMissionIsRare: // 147 NYI
                case ModifierTreeType.GarrisonMissionIsElite: // 148 NYI
                    return false;
                case ModifierTreeType.CurrentGarrisonBuildingLevelEqual: // 149
                {
                    if (miscValue1 == 0)
                        return false;

                    Garrison garrison = referencePlayer.GetGarrison();
                    if (garrison == null)
                        return false;

                    foreach (var plot in garrison.GetPlots())
                    {
                        if (plot.BuildingInfo.PacketInfo == null || plot.BuildingInfo.PacketInfo.GarrBuildingID != miscValue1)
                            continue;

                        var building = CliDB.GarrBuildingStorage.LookupByKey(plot.BuildingInfo.PacketInfo.GarrBuildingID);
                        if (building == null || building.UpgradeLevel != reqValue)
                            continue;

                        return true;
                    }
                    break;
                }
                case ModifierTreeType.GarrisonPlotInstanceHasBuildingThatIsReadyToActivate: // 150
                {
                    Garrison garrison = referencePlayer.GetGarrison();
                    if (garrison == null)
                        return false;

                    var plot = garrison.GetPlot(reqValue);
                    if (plot == null)
                        return false;

                    if (!plot.BuildingInfo.CanActivate() || plot.BuildingInfo.PacketInfo == null || plot.BuildingInfo.PacketInfo.Active)
                        return false;
                    break;
                }
                case ModifierTreeType.BattlePetTeamWithSpeciesEqualOrGreaterThan: // 151
                {
                    uint count = 0;
                    foreach (BattlePetSlot slot in referencePlayer.GetSession().GetBattlePetMgr().GetSlots())
                        if (slot.Pet.Species == secondaryAsset)
                            ++count;

                    if (count < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.BattlePetTeamWithTypeEqualOrGreaterThan: // 152
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
                case ModifierTreeType.PetBattleLastAbility: // 153 NYI
                case ModifierTreeType.PetBattleLastAbilityType: // 154 NYI
                    return false;
                case ModifierTreeType.BattlePetTeamWithAliveEqualOrGreaterThan: // 155
                {
                    uint count = 0;
                    foreach (var slot in referencePlayer.GetSession().GetBattlePetMgr().GetSlots())
                        if (slot.Pet.Health > 0)
                            ++count;

                    if (count < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.HasGarrisonBuildingActiveSpecialization: // 156
                    return false; // OBSOLETE
                case ModifierTreeType.HasGarrisonFollower: // 157
                {
                    Garrison garrison = referencePlayer.GetGarrison();
                    if (garrison == null)
                        return false;

                    uint followerCount = garrison.CountFollowers(follower =>
                    {
                        return follower.PacketInfo.GarrFollowerID == reqValue;
                    });

                    if (followerCount < 1)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerQuestObjectiveProgressEqual: // 158
                {
                    QuestObjective objective = Global.ObjectMgr.GetQuestObjective(reqValue);
                    if (objective == null)
                        return false;

                    if (referencePlayer.GetQuestObjectiveData(objective) != secondaryAsset)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerQuestObjectiveProgressEqualOrGreaterThan: // 159
                {
                    QuestObjective objective = Global.ObjectMgr.GetQuestObjective(reqValue);
                    if (objective == null)
                        return false;

                    if (referencePlayer.GetQuestObjectiveData(objective) < secondaryAsset)
                        return false;
                    break;
                }
                case ModifierTreeType.IsPTRRealm: // 160
                case ModifierTreeType.IsBetaRealm: // 161
                case ModifierTreeType.IsQARealm: // 162
                    return false; // always false
                case ModifierTreeType.GarrisonShipmentContainerIsFull: // 163
                    return false;
                case ModifierTreeType.PlayerCountIsValidToStartGarrisonInvasion: // 164
                    return true; // Only 1 player is required and referencePlayer.GetMap() will ALWAYS have at least the referencePlayer on it
                case ModifierTreeType.InstancePlayerCountEqualOrLessThan: // 165
                    if (referencePlayer.GetMap().GetPlayersCountExceptGMs() > reqValue)
                        return false;
                    break;
                case ModifierTreeType.AllGarrisonPlotsFilledWithBuildingsWithLevelEqualOrGreater: // 166
                {
                    Garrison garrison = referencePlayer.GetGarrison();
                    if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)reqValue)
                        return false;

                    foreach (var plot in garrison.GetPlots())
                    {
                        if (plot.BuildingInfo.PacketInfo == null)
                            return false;

                        var building = CliDB.GarrBuildingStorage.LookupByKey(plot.BuildingInfo.PacketInfo.GarrBuildingID);
                        if (building == null || building.UpgradeLevel != reqValue)
                            return false;
                    }
                    break;
                }
                case ModifierTreeType.GarrisonMissionType: // 167 NYI
                    return false;
                case ModifierTreeType.GarrisonFollowerItemLevelEqualOrGreaterThan: // 168
                {
                    if (miscValue1 == 0)
                        return false;

                    Garrison garrison = referencePlayer.GetGarrison();
                    if (garrison == null)
                        return false;

                    uint followerCount = garrison.CountFollowers(follower =>
                    {
                        return follower.PacketInfo.GarrFollowerID == miscValue1 && follower.GetItemLevel() >= reqValue;
                    });

                    if (followerCount < 1)
                        return false;
                    break;
                }
                case ModifierTreeType.GarrisonFollowerCountWithItemLevelEqualOrGreaterThan: // 169
                {
                    Garrison garrison = referencePlayer.GetGarrison();
                    if (garrison == null)
                        return false;

                    uint followerCount = garrison.CountFollowers(follower =>
                    {
                        var garrFollower = CliDB.GarrFollowerStorage.LookupByKey(follower.PacketInfo.GarrFollowerID);
                        return garrFollower?.GarrFollowerTypeID == tertiaryAsset && follower.GetItemLevel() >= secondaryAsset;
                    });

                    if (followerCount < reqValue)
                        return false;

                    break;
                }
                case ModifierTreeType.GarrisonTierEqual: // 170
                {
                    Garrison garrison = referencePlayer.GetGarrison();
                    if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)secondaryAsset || garrison.GetSiteLevel().GarrLevel != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.InstancePlayerCountEqual: // 171
                    if (referencePlayer.GetMap().GetPlayers().Count != reqValue)
                        return false;
                    break;
                case ModifierTreeType.CurrencyId: // 172
                    if (miscValue1 != reqValue)
                        return false;
                    break;
                case ModifierTreeType.SelectionIsPlayerCorpse: // 173
                    if (referencePlayer.GetTarget().GetHigh() != HighGuid.Corpse)
                        return false;
                    break;
                case ModifierTreeType.PlayerCanAcceptQuest: // 174
                {
                    Quest quest = Global.ObjectMgr.GetQuestTemplate(reqValue);
                    if (quest == null)
                        return false;

                    if (!referencePlayer.CanTakeQuest(quest, false))
                        return false;
                    break;
                }
                case ModifierTreeType.GarrisonFollowerCountWithLevelEqualOrGreaterThan: // 175
                {
                    Garrison garrison = referencePlayer.GetGarrison();
                    if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)tertiaryAsset)
                        return false;

                    uint followerCount = garrison.CountFollowers(follower =>
                    {
                        var garrFollower = CliDB.GarrFollowerStorage.LookupByKey(follower.PacketInfo.GarrFollowerID);
                        return garrFollower?.GarrFollowerTypeID == tertiaryAsset && follower.PacketInfo.FollowerLevel == secondaryAsset;
                    });

                    if (followerCount < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.GarrisonFollowerIsInBuilding: // 176
                {
                    Garrison garrison = referencePlayer.GetGarrison();
                    if (garrison == null)
                        return false;

                    uint followerCount = garrison.CountFollowers(follower =>
                    {
                        return follower.PacketInfo.GarrFollowerID == reqValue && follower.PacketInfo.CurrentBuildingID == secondaryAsset;
                    });

                    if (followerCount < 1)
                        return false;
                    break;
                }
                case ModifierTreeType.GarrisonMissionCountLessThan: // 177 NYI
                    return false;
                case ModifierTreeType.GarrisonPlotInstanceCountEqualOrGreaterThan: // 178
                {
                    Garrison garrison = referencePlayer.GetGarrison();
                    if (garrison == null || garrison.GetGarrisonType() != (GarrisonType)reqValue)
                        return false;

                    uint plotCount = 0;
                    foreach (var plot in garrison.GetPlots())
                    {
                        var garrPlotInstance = CliDB.GarrPlotInstanceStorage.LookupByKey(plot.PacketInfo.GarrPlotInstanceID);
                        if (garrPlotInstance == null || garrPlotInstance.GarrPlotID != secondaryAsset)
                            continue;

                        ++plotCount;
                    }

                    if (plotCount < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.CurrencySource: // 179 NYI
                    return false;
                case ModifierTreeType.PlayerIsInNotOwnGarrison: // 180
                    if (!referencePlayer.GetMap().IsGarrison() || referencePlayer.GetMap().GetInstanceId() == referencePlayer.GetGUID().GetCounter())
                        return false;
                    break;
                case ModifierTreeType.HasActiveGarrisonFollower: // 181
                {
                    Garrison garrison = referencePlayer.GetGarrison();
                    if (garrison == null)
                        return false;

                    uint followerCount = garrison.CountFollowers(follower => follower.PacketInfo.GarrFollowerID == reqValue && (follower.PacketInfo.FollowerStatus & (byte)GarrisonFollowerStatus.Inactive) == 0);
                    if (followerCount < 1)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerDailyRandomValueMod_X_Equals: // 182 NYI
                    return false;
                case ModifierTreeType.PlayerHasMount: // 183
                {
                    foreach (var pair in referencePlayer.GetSession().GetCollectionMgr().GetAccountMounts())
                    {
                        var mount = Global.DB2Mgr.GetMount(pair.Key);
                        if (mount == null)
                            continue;

                        if (mount.Id == reqValue)
                            return true;
                    }
                    return false;
                }
                case ModifierTreeType.GarrisonFollowerCountWithInactiveWithItemLevelEqualOrGreaterThan: // 184
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
                case ModifierTreeType.GarrisonFollowerIsOnAMission: // 185
                {
                    Garrison garrison = referencePlayer.GetGarrison();
                    if (garrison == null)
                        return false;

                    uint followerCount = garrison.CountFollowers(follower => follower.PacketInfo.GarrFollowerID == reqValue && follower.PacketInfo.CurrentMissionID != 0);
                    if (followerCount < 1)
                        return false;
                    break;
                }
                case ModifierTreeType.GarrisonMissionCountInSetLessThan: // 186 NYI
                    return false;
                case ModifierTreeType.GarrisonFollowerType: // 187
                {
                    var garrFollower = CliDB.GarrFollowerStorage.LookupByKey(miscValue1);
                    if (garrFollower == null || garrFollower.GarrFollowerTypeID != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerUsedBoostLessThanHoursAgoRealTime: // 188 NYI
                case ModifierTreeType.PlayerUsedBoostLessThanHoursAgoGameTime: // 189 NYI
                    return false;
                case ModifierTreeType.PlayerIsMercenary: // 190
                    if (!referencePlayer.HasPlayerFlagEx(PlayerFlagsEx.MercenaryMode))
                        return false;
                    break;
                case ModifierTreeType.PlayerEffectiveRace: // 191 NYI
                case ModifierTreeType.TargetEffectiveRace: // 192 NYI
                    return false;
                case ModifierTreeType.HonorLevelEqualOrGreaterThan: // 193
                    if (referencePlayer.GetHonorLevel() < reqValue)
                        return false;
                    break;
                case ModifierTreeType.PrestigeLevelEqualOrGreaterThan: // 194
                    return false; // OBSOLOTE
                case ModifierTreeType.GarrisonMissionIsReadyToCollect: // 195 NYI
                case ModifierTreeType.PlayerIsInstanceOwner: // 196 NYI
                    return false;
                case ModifierTreeType.PlayerHasHeirloom: // 197
                    if (!referencePlayer.GetSession().GetCollectionMgr().GetAccountHeirlooms().ContainsKey(reqValue))
                        return false;
                    break;
                case ModifierTreeType.TeamPoints: // 198 NYI
                    return false;
                case ModifierTreeType.PlayerHasToy: // 199
                    if (!referencePlayer.GetSession().GetCollectionMgr().HasToy(reqValue))
                        return false;
                    break;
                case ModifierTreeType.PlayerHasTransmog: // 200
                {
                    var (PermAppearance, TempAppearance) = referencePlayer.GetSession().GetCollectionMgr().HasItemAppearance(reqValue);
                    if (!PermAppearance || TempAppearance)
                        return false;
                    break;
                }
                case ModifierTreeType.GarrisonTalentSelected: // 201 NYI
                case ModifierTreeType.GarrisonTalentResearched: // 202 NYI
                    return false;
                case ModifierTreeType.PlayerHasRestriction: // 203
                {
                    int restrictionIndex = referencePlayer.m_activePlayerData.CharacterRestrictions.FindIndexIf(restriction => restriction.Type == reqValue);
                    if (restrictionIndex < 0)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerCreatedCharacterLessThanHoursAgoRealTime: // 204 NYI
                    return false;
                case ModifierTreeType.PlayerCreatedCharacterLessThanHoursAgoGameTime: // 205
                    if (TimeSpan.FromHours(reqValue) >= TimeSpan.FromSeconds(referencePlayer.GetTotalPlayedTime()))
                        return false;
                    break;
                case ModifierTreeType.QuestHasQuestInfoId: // 206
                {
                    Quest quest = Global.ObjectMgr.GetQuestTemplate((uint)miscValue1);
                    if (quest == null || quest.Id != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.GarrisonTalentResearchInProgress: // 207 NYI
                    return false;
                case ModifierTreeType.PlayerEquippedArtifactAppearanceSet: // 208
                {
                    Aura artifactAura = referencePlayer.GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);
                    if (artifactAura != null)
                    {
                        Item artifact = referencePlayer.GetItemByGuid(artifactAura.GetCastItemGUID());
                        if (artifact != null)
                        {
                            ArtifactAppearanceRecord artifactAppearance = CliDB.ArtifactAppearanceStorage.LookupByKey(artifact.GetModifier(ItemModifier.ArtifactAppearanceId));
                            if (artifactAppearance != null)
                                if (artifactAppearance.ArtifactAppearanceSetID == reqValue)
                                    break;
                        }
                    }
                    return false;
                }
                case ModifierTreeType.PlayerHasCurrencyEqual: // 209
                    if (referencePlayer.GetCurrencyQuantity(reqValue) != secondaryAsset)
                        return false;
                    break;
                case ModifierTreeType.MinimumAverageItemHighWaterMarkForSpec: // 210 NYI
                    return false;
                case ModifierTreeType.PlayerScenarioType: // 211
                {
                    Scenario scenario = referencePlayer.GetScenario();
                    if (scenario == null)
                        return false;

                    if (scenario.GetEntry().Type != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayersAuthExpansionLevelEqualOrGreaterThan: // 212
                    if (referencePlayer.GetSession().GetAccountExpansion() < (Expansion)reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerLastWeek2v2Rating: // 213 NYI
                case ModifierTreeType.PlayerLastWeek3v3Rating: // 214 NYI
                case ModifierTreeType.PlayerLastWeekRBGRating: // 215 NYI
                    return false;
                case ModifierTreeType.GroupMemberCountFromConnectedRealmEqualOrGreaterThan: // 216
                {
                    uint memberCount = 0;
                    var group = referencePlayer.GetGroup();
                    if (group != null)
                    {
                        for (var itr = group.GetFirstMember(); itr != null; itr = itr.Next())
                            if (itr.GetSource() != referencePlayer && referencePlayer.m_playerData.VirtualPlayerRealm == itr.GetSource().m_playerData.VirtualPlayerRealm)
                                ++memberCount;
                    }

                    if (memberCount < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.ArtifactTraitUnlockedCountEqualOrGreaterThan: // 217
                {
                    Item artifact = referencePlayer.GetItemByEntry((uint)secondaryAsset, ItemSearchLocation.Everywhere);
                    if (artifact == null)
                        return false;

                    if (artifact.GetTotalUnlockedArtifactPowers() < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.ParagonReputationLevelEqualOrGreaterThan: // 218
                    if (referencePlayer.GetReputationMgr().GetParagonLevel((uint)miscValue1) < reqValue)
                        return false;
                    return false;
                case ModifierTreeType.GarrisonShipmentIsReady: // 219 NYI
                    return false;
                case ModifierTreeType.PlayerIsInPvpBrawl: // 220
                {
                    var bg = CliDB.BattlemasterListStorage.LookupByKey(referencePlayer.GetBattlegroundTypeId());
                    if (bg == null || !bg.HasFlag(BattlemasterListFlags.Brawl))
                        return false;
                    break;
                }
                case ModifierTreeType.ParagonReputationLevelWithFactionEqualOrGreaterThan: // 221
                {
                    var faction = CliDB.FactionStorage.LookupByKey(secondaryAsset);
                    if (faction == null)
                        return false;

                    if (referencePlayer.GetReputationMgr().GetParagonLevel(faction.ParagonFactionID) < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHasItemWithBonusListFromTreeAndQuality: // 222
                {
                    var bonusListIDs = ItemBonusMgr.GetAllBonusListsForTree(reqValue);
                    if (bonusListIDs.Empty())
                        return false;

                    bool bagScanReachedEnd = referencePlayer.ForEachItem(ItemSearchLocation.Everywhere, item =>
                    {
                        bool hasBonus = item.GetBonusListIDs().Any(bonusListID => bonusListIDs.Contains(bonusListID));
                        return !hasBonus;
                    });

                    if (bagScanReachedEnd)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHasEmptyInventorySlotCountEqualOrGreaterThan: // 223
                    if (referencePlayer.GetFreeInventorySlotCount(ItemSearchLocation.Inventory) < reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerHasItemInHistoryOfProgressiveEvent: // 224 NYI
                    return false;
                case ModifierTreeType.PlayerHasArtifactPowerRankCountPurchasedEqualOrGreaterThan: // 225
                {
                    Aura artifactAura = referencePlayer.GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);
                    if (artifactAura == null)
                        return false;

                    Item artifact = referencePlayer.GetItemByGuid(artifactAura.GetCastItemGUID());
                    if (artifact == null)
                        return false;

                    var artifactPower = artifact.GetArtifactPower((uint)secondaryAsset);
                    if (artifactPower == null)
                        return false;

                    if (artifactPower.PurchasedRank < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHasBoosted: // 226
                    if (referencePlayer.HasLevelBoosted())
                        return false;
                    break;
                case ModifierTreeType.PlayerHasRaceChanged: // 227
                    if (referencePlayer.HasRaceChanged())
                        return false;
                    break;
                case ModifierTreeType.PlayerHasBeenGrantedLevelsFromRaF: // 228
                    if (referencePlayer.HasBeenGrantedLevelsFromRaF())
                        return false;
                    break;
                case ModifierTreeType.IsTournamentRealm: // 229
                    return false;
                case ModifierTreeType.PlayerCanAccessAlliedRaces: // 230
                    if (!referencePlayer.GetSession().CanAccessAlliedRaces())
                        return false;
                    break;
                case ModifierTreeType.GroupMemberCountWithAchievementEqualOrLessThan: // 231
                {
                    var group = referencePlayer.GetGroup();
                    if (group != null)
                    {
                        uint membersWithAchievement = 0;
                        for (var itr = group.GetFirstMember(); itr != null; itr = itr.Next())
                            if (itr.GetSource().HasAchieved((uint)secondaryAsset))
                                ++membersWithAchievement;

                        if (membersWithAchievement > reqValue)
                            return false;
                    }
                    // true if no group
                    break;
                }
                case ModifierTreeType.PlayerMainhandWeaponType: // 232
                {
                    var visibleItem = referencePlayer.m_playerData.VisibleItems[EquipmentSlot.MainHand];
                    uint itemSubclass = (uint)ItemSubClassWeapon.Fist;
                    ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(visibleItem.ItemID);
                    if (itemTemplate != null)
                    {
                        if (itemTemplate.GetClass() == ItemClass.Weapon)
                        {
                            itemSubclass = itemTemplate.GetSubClass();

                            var itemModifiedAppearance = Global.DB2Mgr.GetItemModifiedAppearance(visibleItem.ItemID, visibleItem.ItemAppearanceModID);
                            if (itemModifiedAppearance != null)
                            {
                                var itemModifiedAppearaceExtra = CliDB.ItemModifiedAppearanceExtraStorage.LookupByKey(itemModifiedAppearance.Id);
                                if (itemModifiedAppearaceExtra != null)
                                    if (itemModifiedAppearaceExtra.DisplayWeaponSubclassID > 0)
                                        itemSubclass = (uint)itemModifiedAppearaceExtra.DisplayWeaponSubclassID;
                            }
                        }
                    }
                    if (itemSubclass != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerOffhandWeaponType: // 233
                {
                    var visibleItem = referencePlayer.m_playerData.VisibleItems[EquipmentSlot.OffHand];
                    uint itemSubclass = (uint)ItemSubClassWeapon.Fist;
                    ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(visibleItem.ItemID);
                    if (itemTemplate != null)
                    {
                        if (itemTemplate.GetClass() == ItemClass.Weapon)
                        {
                            itemSubclass = itemTemplate.GetSubClass();

                            var itemModifiedAppearance = Global.DB2Mgr.GetItemModifiedAppearance(visibleItem.ItemID, visibleItem.ItemAppearanceModID);
                            if (itemModifiedAppearance != null)
                            {
                                var itemModifiedAppearaceExtra = CliDB.ItemModifiedAppearanceExtraStorage.LookupByKey(itemModifiedAppearance.Id);
                                if (itemModifiedAppearaceExtra != null)
                                    if (itemModifiedAppearaceExtra.DisplayWeaponSubclassID > 0)
                                        itemSubclass = (uint)itemModifiedAppearaceExtra.DisplayWeaponSubclassID;
                            }
                        }
                    }
                    if (itemSubclass != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerPvpTier: // 234
                {
                    var pvpTier = CliDB.PvpTierStorage.LookupByKey(reqValue);
                    if (pvpTier == null)
                        return false;

                    PVPInfo pvpInfo = referencePlayer.GetPvpInfoForBracket((byte)pvpTier.BracketID);
                    if (pvpInfo == null)
                        return false;

                    if (pvpTier.Id != pvpInfo.PvpTierID)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerAzeriteLevelEqualOrGreaterThan: // 235
                {
                    Item heartOfAzeroth = referencePlayer.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);
                    if (heartOfAzeroth == null || heartOfAzeroth.ToAzeriteItem().GetLevel() < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerIsOnQuestInQuestline: // 236
                {
                    bool isOnQuest = false;
                    var questLineQuests = Global.DB2Mgr.GetQuestsForQuestLine(reqValue);
                    if (!questLineQuests.Empty())
                        isOnQuest = questLineQuests.Any(questLineQuest => referencePlayer.FindQuestSlot(questLineQuest.QuestID) < SharedConst.MaxQuestLogSize);

                    if (!isOnQuest)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerIsQnQuestLinkedToScheduledWorldStateGroup: // 237
                    return false; // OBSOLETE (db2 removed)
                case ModifierTreeType.PlayerIsInRaidGroup: // 238
                {
                    var group = referencePlayer.GetGroup();
                    if (group == null || !group.IsRaidGroup())
                        return false;

                    break;
                }
                case ModifierTreeType.PlayerPvpTierInBracketEqualOrGreaterThan: // 239
                {
                    PVPInfo pvpInfo = referencePlayer.GetPvpInfoForBracket((byte)secondaryAsset);
                    if (pvpInfo == null)
                        return false;

                    var pvpTier = CliDB.PvpTierStorage.LookupByKey(pvpInfo.PvpTierID);
                    if (pvpTier == null)
                        return false;

                    if (pvpTier.Rank < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerCanAcceptQuestInQuestline: // 240
                {
                    var questLineQuests = Global.DB2Mgr.GetQuestsForQuestLine(reqValue);
                    if (questLineQuests.Empty())
                        return false;

                    bool canTakeQuest = questLineQuests.Any(questLineQuest =>
                    {
                        Quest quest = Global.ObjectMgr.GetQuestTemplate(questLineQuest.QuestID);
                        if (quest != null)
                            return referencePlayer.CanTakeQuest(quest, false);

                        return false;
                    });

                    if (!canTakeQuest)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHasCompletedQuestline: // 241
                {
                    var questLineQuests = Global.DB2Mgr.GetQuestsForQuestLine(reqValue);
                    if (questLineQuests.Empty())
                        return false;

                    foreach (var questLineQuest in questLineQuests)
                        if (!referencePlayer.GetQuestRewardStatus(questLineQuest.QuestID))
                            return false;
                    break;
                }
                case ModifierTreeType.PlayerHasCompletedQuestlineQuestCount: // 242
                {
                    var questLineQuests = Global.DB2Mgr.GetQuestsForQuestLine(reqValue);
                    if (questLineQuests.Empty())
                        return false;

                    uint completedQuests = 0;
                    foreach (var questLineQuest in questLineQuests)
                        if (referencePlayer.GetQuestRewardStatus(questLineQuest.QuestID))
                            ++completedQuests;

                    if (completedQuests < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHasCompletedPercentageOfQuestline: // 243
                {
                    var questLineQuests = Global.DB2Mgr.GetQuestsForQuestLine(reqValue);
                    if (questLineQuests.Empty())
                        return false;

                    int completedQuests = 0;
                    foreach (var questLineQuest in questLineQuests)
                        if (referencePlayer.GetQuestRewardStatus(questLineQuest.QuestID))
                            ++completedQuests;

                    if (MathFunctions.GetPctOf(completedQuests, questLineQuests.Count) < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHasWarModeEnabled: // 244
                    if (!referencePlayer.HasPlayerLocalFlag(PlayerLocalFlags.WarMode))
                        return false;
                    break;
                case ModifierTreeType.PlayerIsOnWarModeShard: // 245
                    if (!referencePlayer.HasPlayerFlag(PlayerFlags.WarModeActive))
                        return false;
                    break;
                case ModifierTreeType.PlayerIsAllowedToToggleWarModeInArea: // 246
                    if (!referencePlayer.CanEnableWarModeInArea())
                        return false;
                    break;
                case ModifierTreeType.MythicPlusKeystoneLevelEqualOrGreaterThan: // 247 NYI
                case ModifierTreeType.MythicPlusCompletedInTime: // 248 NYI
                case ModifierTreeType.MythicPlusMapChallengeMode: // 249 NYI
                case ModifierTreeType.MythicPlusDisplaySeason: // 250 NYI
                case ModifierTreeType.MythicPlusMilestoneSeason: // 251 NYI
                    return false;
                case ModifierTreeType.PlayerVisibleRace: // 252
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
                case ModifierTreeType.TargetVisibleRace: // 253
                {
                    if (refe == null || !refe.IsUnit())
                        return false;
                    CreatureDisplayInfoRecord creatureDisplayInfo = CliDB.CreatureDisplayInfoStorage.LookupByKey(refe.ToUnit().GetDisplayId());
                    if (creatureDisplayInfo == null)
                        return false;

                    CreatureDisplayInfoExtraRecord creatureDisplayInfoExtra = CliDB.CreatureDisplayInfoExtraStorage.LookupByKey(creatureDisplayInfo.ExtendedDisplayInfoID);
                    if (creatureDisplayInfoExtra == null)
                        return false;

                    if (creatureDisplayInfoExtra.DisplayRaceID != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.FriendshipRepReactionEqual: // 254
                {
                    var friendshipRepReaction = CliDB.FriendshipRepReactionStorage.LookupByKey(reqValue);
                    if (friendshipRepReaction == null)
                        return false;

                    var friendshipReputation = CliDB.FriendshipReputationStorage.LookupByKey(friendshipRepReaction.FriendshipRepID);
                    if (friendshipReputation == null)
                        return false;

                    var friendshipReactions = Global.DB2Mgr.GetFriendshipRepReactions(reqValue);
                    if (friendshipReactions == null)
                        return false;

                    int rank = (int)referencePlayer.GetReputationRank((uint)friendshipReputation.FactionID);
                    if (rank >= friendshipReactions.Count)
                        return false;

                    if (friendshipReactions[rank].Id != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerAuraStackCountEqual: // 255
                    if (referencePlayer.GetAuraCount((uint)secondaryAsset) != reqValue)
                        return false;
                    break;
                case ModifierTreeType.TargetAuraStackCountEqual: // 256
                    if (refe == null || !refe.IsUnit() || refe.ToUnit().GetAuraCount((uint)secondaryAsset) != reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerAuraStackCountEqualOrGreaterThan: // 257
                    if (referencePlayer.GetAuraCount((uint)secondaryAsset) < reqValue)
                        return false;
                    break;
                case ModifierTreeType.TargetAuraStackCountEqualOrGreaterThan: // 258
                    if (refe == null || !refe.IsUnit() || refe.ToUnit().GetAuraCount((uint)secondaryAsset) < reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerHasAzeriteEssenceRankLessThan: // 259
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
                case ModifierTreeType.PlayerHasAzeriteEssenceRankEqual: // 260
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
                case ModifierTreeType.PlayerHasAzeriteEssenceRankGreaterThan: // 261
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
                case ModifierTreeType.PlayerHasAuraWithEffectIndex: // 262
                    if (referencePlayer.GetAuraEffect(reqValue, (uint)secondaryAsset) == null)
                        return false;
                    break;
                case ModifierTreeType.PlayerLootSpecializationMatchesRole: // 263
                {
                    ChrSpecializationRecord spec = referencePlayer.GetPrimarySpecializationEntry();
                    if (spec == null || spec.Role != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerIsAtMaxExpansionLevel: // 264
                    if (!referencePlayer.IsMaxLevel())
                        return false;
                    break;
                case ModifierTreeType.TransmogSource: // 265
                {
                    var itemModifiedAppearance = CliDB.ItemModifiedAppearanceStorage.LookupByKey(miscValue2);
                    if (itemModifiedAppearance == null)
                        return false;

                    if (itemModifiedAppearance.TransmogSourceTypeEnum != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHasAzeriteEssenceInSlotAtRankLessThan: // 266
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
                case ModifierTreeType.PlayerHasAzeriteEssenceInSlotAtRankGreaterThan: // 267
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
                    return false;
                }
                case ModifierTreeType.PlayerLevelWithinContentTuning: // 268
                {
                    uint level = referencePlayer.GetLevel();
                    var levels = Global.DB2Mgr.GetContentTuningData(reqValue, 0);
                    if (levels.HasValue)
                    {
                        if (secondaryAsset != 0)
                            return level >= levels.Value.MinLevelWithDelta && level <= levels.Value.MaxLevelWithDelta;
                        return level >= levels.Value.MinLevel && level <= levels.Value.MaxLevel;
                    }
                    return false;
                }
                case ModifierTreeType.TargetLevelWithinContentTuning: // 269
                {
                    if (refe == null || !refe.IsUnit())
                        return false;

                    uint level = refe.ToUnit().GetLevel();
                    var levels = Global.DB2Mgr.GetContentTuningData(reqValue, 0);
                    if (levels.HasValue)
                    {
                        if (secondaryAsset != 0)
                            return level >= levels.Value.MinLevelWithDelta && level <= levels.Value.MaxLevelWithDelta;
                        return level >= levels.Value.MinLevel && level <= levels.Value.MaxLevel;
                    }
                    return false;
                }
                case ModifierTreeType.PlayerIsScenarioInitiator: // 270 NYI
                    return false;
                case ModifierTreeType.PlayerHasCompletedQuestOrIsOnQuest: // 271
                {
                    QuestStatus status = referencePlayer.GetQuestStatus(reqValue);
                    if (status == QuestStatus.None || status == QuestStatus.Failed)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerLevelWithinOrAboveContentTuning: // 272
                {
                    uint level = referencePlayer.GetLevel();
                    var levels = Global.DB2Mgr.GetContentTuningData(reqValue, 0);
                    if (levels.HasValue)
                        return secondaryAsset != 0 ? level >= levels.Value.MinLevelWithDelta : level >= levels.Value.MinLevel;
                    return false;
                }
                case ModifierTreeType.TargetLevelWithinOrAboveContentTuning: // 273
                {
                    if (refe == null || !refe.IsUnit())
                        return false;

                    uint level = refe.ToUnit().GetLevel();
                    var levels = Global.DB2Mgr.GetContentTuningData(reqValue, 0);
                    if (levels.HasValue)
                        return secondaryAsset != 0 ? level >= levels.Value.MinLevelWithDelta : level >= levels.Value.MinLevel;
                    return false;
                }
                case ModifierTreeType.PlayerLevelWithinOrAboveLevelRange: // 274 NYI
                case ModifierTreeType.TargetLevelWithinOrAboveLevelRange: // 275 NYI
                    return false;
                case ModifierTreeType.MaxJailersTowerLevelEqualOrGreaterThan: // 276
                    if (referencePlayer.m_activePlayerData.JailersTowerLevelMax < reqValue)
                        return false;
                    break;
                case ModifierTreeType.GroupedWithRaFRecruit: // 277
                {
                    var group = referencePlayer.GetGroup();
                    if (group == null)
                        return false;

                    for (var itr = group.GetFirstMember(); itr != null; itr = itr.Next())
                        if (itr.GetSource().GetSession().GetRecruiterId() == referencePlayer.GetSession().GetAccountId())
                            return true;

                    return false;
                }
                case ModifierTreeType.GroupedWithRaFRecruiter: // 278
                {
                    var group = referencePlayer.GetGroup();
                    if (group == null)
                        return false;

                    for (var itr = group.GetFirstMember(); itr != null; itr = itr.Next())
                        if (itr.GetSource().GetSession().GetAccountId() == referencePlayer.GetSession().GetRecruiterId())
                            return true;

                    return false;
                }
                case ModifierTreeType.PlayerSpecialization: // 279
                    if (referencePlayer.GetPrimarySpecialization() != (ChrSpecialization)reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerMapOrCosmeticChildMap: // 280
                {
                    MapRecord map = referencePlayer.GetMap().GetEntry();
                    if (map.Id != reqValue && map.CosmeticParentMapID != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerCanAccessShadowlandsPrepurchaseContent: // 281
                    if (referencePlayer.GetSession().GetAccountExpansion() < Expansion.ShadowLands)
                        return false;
                    break;
                case ModifierTreeType.PlayerHasEntitlement: // 282 NYI
                case ModifierTreeType.PlayerIsInPartySyncGroup: // 283 NYI
                case ModifierTreeType.QuestHasPartySyncRewards: // 284 NYI
                case ModifierTreeType.HonorGainSource: // 285 NYI
                case ModifierTreeType.JailersTowerActiveFloorIndexEqualOrGreaterThan: // 286 NYI
                case ModifierTreeType.JailersTowerActiveFloorDifficultyEqualOrGreaterThan: // 287 NYI
                    return false;
                case ModifierTreeType.PlayerCovenant: // 288
                    if (referencePlayer.m_playerData.CovenantID != reqValue)
                        return false;
                    break;
                case ModifierTreeType.HasTimeEventPassed: // 289
                {
                    long eventTimestamp = GameTime.GetGameTime();
                    switch (reqValue)
                    {
                        case 111: // Battle for Azeroth Season 4 Start
                            eventTimestamp = 1579618800L; // January 21, 2020 8:00
                            break;
                        case 120: // Patch 9.0.1
                            eventTimestamp = 1602601200L; // October 13, 2020 8:00
                            break;
                        case 121: // Shadowlands Season 1 Start
                            eventTimestamp = 1607439600L; // December 8, 2020 8:00
                            break;
                        case 123: // Shadowlands Season 1 End
                                  // timestamp = unknown
                            break;
                        case 149: // Shadowlands Season 2 End
                                  // timestamp = unknown
                            break;
                        case 349: // Dragonflight Season 3 Start (pre-season)
                            eventTimestamp = 1699340400L; // November 7, 2023 8:00
                            break;
                        case 350: // Dragonflight Season 3 Start
                            eventTimestamp = 1699945200L; // November 14, 2023 8:00
                            break;
                        case 352: // Dragonflight Season 3 End
                                  // eventTimestamp = time_t(); unknown
                            break;
                        default:
                            break;
                    }
                    if (GameTime.GetGameTime() < eventTimestamp)
                        return false;
                    break;
                }
                case ModifierTreeType.GarrisonHasPermanentTalent: // 290 NYI
                    return false;
                case ModifierTreeType.HasActiveSoulbind: // 291
                    if (referencePlayer.m_playerData.SoulbindID != reqValue)
                        return false;
                    break;
                case ModifierTreeType.HasMemorizedSpell: // 292 NYI
                    return false;
                case ModifierTreeType.PlayerHasAPACSubscriptionReward_2020: // 293
                case ModifierTreeType.PlayerHasTBCCDEWarpStalker_Mount: // 294
                case ModifierTreeType.PlayerHasTBCCDEDarkPortal_Toy: // 295
                case ModifierTreeType.PlayerHasTBCCDEPathOfIllidan_Toy: // 296
                case ModifierTreeType.PlayerHasImpInABallToySubscriptionReward: // 297
                    return false;
                case ModifierTreeType.PlayerIsInAreaGroup: // 298
                {
                    var areas = Global.DB2Mgr.GetAreasForGroup(reqValue);
                    foreach (uint areaInGroup in areas)
                        if (Global.DB2Mgr.IsInArea(referencePlayer.GetAreaId(), areaInGroup))
                            return true;
                    return false;
                }
                case ModifierTreeType.TargetIsInAreaGroup: // 299
                {
                    if (refe == null)
                        return false;

                    var areas = Global.DB2Mgr.GetAreasForGroup(reqValue);
                    foreach (uint areaInGroup in areas)
                        if (Global.DB2Mgr.IsInArea(refe.GetAreaId(), areaInGroup))
                            return true;
                    return false;
                }
                case ModifierTreeType.PlayerIsInChromieTime: // 300
                    if (referencePlayer.m_activePlayerData.UiChromieTimeExpansionID != reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerIsInAnyChromieTime: // 301
                    if (referencePlayer.m_activePlayerData.UiChromieTimeExpansionID == 0)
                        return false;
                    break;
                case ModifierTreeType.ItemIsAzeriteArmor: // 302
                    if (Global.DB2Mgr.GetAzeriteEmpoweredItem((uint)miscValue1) == null)
                        return false;
                    break;
                case ModifierTreeType.PlayerHasRuneforgePower: // 303
                {
                    int block = (int)reqValue / 32;
                    if (block >= referencePlayer.m_activePlayerData.RuneforgePowers.Size())
                        return false;

                    uint bit = reqValue % 32;
                    return (referencePlayer.m_activePlayerData.RuneforgePowers[block] & (1u << (int)bit)) != 0;
                }
                case ModifierTreeType.PlayerInChromieTimeForScaling: // 304
                    if ((referencePlayer.m_playerData.CtrOptions._value.ConditionalFlags & 1) == 0)
                        return false;
                    break;
                case ModifierTreeType.IsRaFRecruit: // 305
                    if (referencePlayer.GetSession().GetRecruiterId() == 0)
                        return false;
                    break;
                case ModifierTreeType.AllPlayersInGroupHaveAchievement: // 306
                {
                    var group = referencePlayer.GetGroup();
                    if (group != null)
                    {
                        for (var itr = group.GetFirstMember(); itr != null; itr = itr.Next())
                            if (!itr.GetSource().HasAchieved(reqValue))
                                return false;
                    }
                    else if (!referencePlayer.HasAchieved(reqValue))
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHasSoulbindConduitRankEqualOrGreaterThan: // 307 NYI
                    return false;
                case ModifierTreeType.PlayerSpellShapeshiftFormCreatureDisplayInfoSelection: // 308
                {
                    ShapeshiftFormModelData formModelData = Global.DB2Mgr.GetShapeshiftFormModelData(referencePlayer.GetRace(), referencePlayer.GetNativeGender(), (ShapeShiftForm)secondaryAsset);
                    if (formModelData == null)
                        return false;

                    uint formChoice = referencePlayer.GetCustomizationChoice(formModelData.OptionID);
                    var choiceIndex = formModelData.Choices.FindIndex(choice => { return choice.Id == formChoice; });
                    if (choiceIndex == -1)
                        return false;

                    if (reqValue != formModelData.Displays[choiceIndex].DisplayID)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerSoulbindConduitCountAtRankEqualOrGreaterThan: // 309 NYI
                    return false;
                case ModifierTreeType.PlayerIsRestrictedAccount: // 310
                    return false;
                case ModifierTreeType.PlayerIsFlying: // 311
                    if (!referencePlayer.IsFlying())
                        return false;
                    break;
                case ModifierTreeType.PlayerScenarioIsLastStep: // 312
                {
                    Scenario scenario = referencePlayer.GetScenario();
                    if (scenario == null)
                        return false;

                    if (scenario.GetStep() != scenario.GetLastStep())
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHasWeeklyRewardsAvailable: // 313
                    if (referencePlayer.m_activePlayerData.WeeklyRewardsPeriodSinceOrigin == 0)
                        return false;
                    break;
                case ModifierTreeType.TargetCovenant: // 314
                    if (refe == null || !refe.IsPlayer())
                        return false;
                    if (refe.ToPlayer().m_playerData.CovenantID != reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerHasTBCCollectorsEdition: // 315
                case ModifierTreeType.PlayerHasWrathCollectorsEdition: // 316
                    return false;
                case ModifierTreeType.GarrisonTalentResearchedAndAtRankEqualOrGreaterThan: // 317 NYI
                case ModifierTreeType.CurrencySpentOnGarrisonTalentResearchEqualOrGreaterThan: // 318 NYI
                case ModifierTreeType.RenownCatchupActive: // 319 NYI
                case ModifierTreeType.RapidRenownCatchupActive: // 320 NYI
                case ModifierTreeType.PlayerMythicPlusRatingEqualOrGreaterThan: // 321 NYI
                case ModifierTreeType.PlayerMythicPlusRunCountInCurrentExpansionEqualOrGreaterThan: // 322 NYI
                    return false;
                case ModifierTreeType.PlayerHasCustomizationChoice: // 323
                {
                    int customizationChoiceIndex = referencePlayer.m_playerData.Customizations.FindIndexIf(choice =>
                    {
                        return choice.ChrCustomizationChoiceID == reqValue;
                    });

                    if (customizationChoiceIndex < 0)
                        return false;

                    break;
                }
                case ModifierTreeType.PlayerBestWeeklyWinPvpTier: // 324
                {
                    var pvpTier = CliDB.PvpTierStorage.LookupByKey(reqValue);
                    if (pvpTier == null)
                        return false;

                    PVPInfo pvpInfo = referencePlayer.GetPvpInfoForBracket((byte)pvpTier.BracketID);
                    if (pvpInfo == null)
                        return false;

                    if (pvpTier.Id != pvpInfo.WeeklyBestWinPvpTierID)
                        return false;

                    break;
                }
                case ModifierTreeType.PlayerBestWeeklyWinPvpTierInBracketEqualOrGreaterThan: // 325
                {
                    PVPInfo pvpInfo = referencePlayer.GetPvpInfoForBracket((byte)secondaryAsset);
                    if (pvpInfo == null)
                        return false;

                    var pvpTier = CliDB.PvpTierStorage.LookupByKey(pvpInfo.WeeklyBestWinPvpTierID);
                    if (pvpTier == null)
                        return false;

                    if (pvpTier.Rank < reqValue)
                        return false;

                    break;
                }
                case ModifierTreeType.PlayerHasVanillaCollectorsEdition: // 326
                    return false;
                case ModifierTreeType.PlayerHasItemWithKeystoneLevelModifierEqualOrGreaterThan: // 327
                {
                    bool bagScanReachedEnd = referencePlayer.ForEachItem(ItemSearchLocation.Inventory, item =>
                    {
                        if (item.GetEntry() != reqValue)
                            return true;

                        if (item.GetModifier(ItemModifier.ChallengeKeystoneLevel) < secondaryAsset)
                            return true;

                        return false;
                    });
                    if (bagScanReachedEnd)
                        return false;

                    break;
                }
                case ModifierTreeType.PlayerAuraWithLabelStackCountEqualOrGreaterThan: // 335
                {
                    uint count = 0;
                    referencePlayer.HasAura(aura =>
                    {
                        if (aura.GetSpellInfo().HasLabel((uint)secondaryAsset))
                            count += aura.GetStackAmount();
                        return false;
                    });
                    if (count < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerAuraWithLabelStackCountEqual: // 336
                {
                    uint count = 0;
                    referencePlayer.HasAura(aura =>
                    {
                        if (aura.GetSpellInfo().HasLabel((uint)secondaryAsset))
                            count += aura.GetStackAmount();
                        return false;
                    });
                    if (count != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerAuraWithLabelStackCountEqualOrLessThan: // 337
                {
                    uint count = 0;
                    referencePlayer.HasAura(aura =>
                    {
                        if (aura.GetSpellInfo().HasLabel((uint)secondaryAsset))
                            count += aura.GetStackAmount();
                        return false;
                    });
                    if (count > reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerIsInCrossFactionGroup: // 338
                {
                    var group = referencePlayer.GetGroup();
                    if (!group.GetGroupFlags().HasFlag(GroupFlags.CrossFaction))
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHasTraitNodeEntryInActiveConfig: // 340
                {
                    bool hasTraitNodeEntry()
                    {
                        foreach (var traitConfig in referencePlayer.m_activePlayerData.TraitConfigs)
                        {
                            if ((TraitConfigType)(int)traitConfig.Type == TraitConfigType.Combat)
                            {
                                if (referencePlayer.m_activePlayerData.ActiveCombatTraitConfigID != traitConfig.ID
                                    || !((TraitCombatConfigFlags)(int)traitConfig.CombatConfigFlags).HasFlag(TraitCombatConfigFlags.ActiveForSpec))
                                    continue;
                            }

                            foreach (var traitEntry in traitConfig.Entries)
                                if (traitEntry.TraitNodeEntryID == reqValue)
                                    return true;
                        }
                        return false;
                    }
                    if (!hasTraitNodeEntry())
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHasTraitNodeEntryInActiveConfigRankGreaterOrEqualThan: // 341
                {
                    var traitNodeEntryRank = new Func<short?>(() =>
                    {
                        foreach (var traitConfig in referencePlayer.m_activePlayerData.TraitConfigs)
                        {
                            if ((TraitConfigType)(int)traitConfig.Type == TraitConfigType.Combat)
                            {
                                if (referencePlayer.m_activePlayerData.ActiveCombatTraitConfigID != traitConfig.ID
                                    || !((TraitCombatConfigFlags)(int)traitConfig.CombatConfigFlags).HasFlag(TraitCombatConfigFlags.ActiveForSpec))
                                    continue;
                            }

                            foreach (var traitEntry in traitConfig.Entries)
                                if (traitEntry.TraitNodeEntryID == secondaryAsset)
                                    return (short)traitEntry.Rank;
                        }
                        return null;
                    })();
                    if (!traitNodeEntryRank.HasValue || traitNodeEntryRank < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerDaysSinceLogout: // 344
                    if (GameTime.GetGameTime() - referencePlayer.m_playerData.LogoutTime < reqValue * Time.Day)
                        return false;
                    break;
                case ModifierTreeType.PlayerHasPerksProgramPendingReward: // 350
                    if (!referencePlayer.m_activePlayerData.HasPerksProgramPendingReward)
                        return false;
                    break;
                case ModifierTreeType.PlayerCanUseItem: // 351
                {
                    ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(reqValue);
                    if (itemTemplate == null || referencePlayer.CanUseItem(itemTemplate) != InventoryResult.Ok)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerSummonedBattlePetSpecies: // 352
                    if (referencePlayer.m_playerData.CurrentBattlePetSpeciesID != reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerSummonedBattlePetIsMaxLevel: // 353
                    if (referencePlayer.m_unitData.WildBattlePetLevel != SharedConst.MaxBattlePetLevel)
                        return false;
                    break;
                case ModifierTreeType.PlayerHasAtLeastProfPathRanks: // 355
                {
                    uint ranks = 0;
                    foreach (TraitConfig traitConfig in referencePlayer.m_activePlayerData.TraitConfigs)
                    {
                        if ((TraitConfigType)(int)traitConfig.Type != TraitConfigType.Profession)
                            continue;

                        if (traitConfig.SkillLineID != secondaryAsset)
                            continue;

                        foreach (TraitEntry traitEntry in traitConfig.Entries)
                            if (CliDB.TraitNodeEntryStorage.LookupByKey(traitEntry.TraitNodeEntryID)?.GetNodeEntryType() == TraitNodeEntryType.ProfPath)
                                ranks += (uint)(traitEntry.Rank + traitEntry.GrantedRanks);
                    }

                    if (ranks < reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHasItemTransmogrifiedToItemModifiedAppearance: // 358
                {
                    var itemModifiedAppearance = CliDB.ItemModifiedAppearanceStorage.LookupByKey(reqValue);

                    bool bagScanReachedEnd = referencePlayer.ForEachItem(ItemSearchLocation.Inventory, item =>
                    {
                        if (item.GetVisibleAppearanceModId(referencePlayer) == itemModifiedAppearance.Id)
                            return false;

                        if (item.GetEntry() == itemModifiedAppearance.ItemID)
                            return false;

                        return true;
                    });
                    if (bagScanReachedEnd)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHasCompletedDungeonEncounterInDifficulty: // 366
                    if (!referencePlayer.IsLockedToDungeonEncounter(reqValue, (Difficulty)secondaryAsset))
                        return false;
                    break;
                case ModifierTreeType.PlayerIsBetweenQuests: // 369
                {
                    QuestStatus status = referencePlayer.GetQuestStatus(reqValue);
                    if (status == QuestStatus.None || status == QuestStatus.Failed)
                        return false;
                    if (referencePlayer.IsQuestRewarded((uint)secondaryAsset))
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerScenarioStepID: // 371
                {
                    Scenario scenario = referencePlayer.GetScenario();
                    if (scenario == null)
                        return false;
                    if (scenario.GetStep().Id != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerZPositionBelow: // 374
                    if (referencePlayer.GetPositionZ() >= reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerDataFlagAccountIsSet: // 378
                    if (!referencePlayer.HasDataFlagAccount(reqValue))
                        return false;
                    break;
                case ModifierTreeType.PlayerDataFlagCharacterIsSet: // 379
                    if (!referencePlayer.HasDataFlagCharacter(reqValue))
                        return false;
                    break;
                case ModifierTreeType.PlayerIsOnMapWithExpansion: // 380
                {
                    var mapEntry = referencePlayer.GetMap().GetEntry();
                    if (mapEntry.ExpansionID != reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerHasActiveTraitSubTree: // 385
                {
                    int traitConfigWithSubtree = referencePlayer.m_activePlayerData.TraitConfigs.FindIndexIf(traitConfig =>
                    {
                        if (traitConfig.Type == (int)TraitConfigType.Combat
                            && (referencePlayer.m_activePlayerData.ActiveCombatTraitConfigID != traitConfig.ID
                                || !traitConfig.CombatConfigFlags.GetValue().HasAnyFlag((int)TraitCombatConfigFlags.ActiveForSpec)))
                            return false;

                        return traitConfig.SubTrees.FindIndexIf(traitSubTree =>
                        {
                            return traitSubTree.TraitSubTreeID == reqValue && traitSubTree.Active != 0;
                        }) >= 0;
                    });
                    if (traitConfigWithSubtree < 0)
                        return false;
                    break;
                }
                case ModifierTreeType.TargetCreatureClassificationEqual: // 389
                {
                    Creature targetCreature = refe?.ToCreature();
                    if (targetCreature == null)
                        return false;
                    if (targetCreature.GetCreatureClassification() != (CreatureClassifications)reqValue)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerDataElementCharacterBetween: // 390
                {
                    var value = referencePlayer.GetDataElementCharacter(reqValue);
                    return value >= secondaryAsset && value <= tertiaryAsset;
                }
                case ModifierTreeType.PlayerDataElementAccountBetween: // 391
                {
                    var value = referencePlayer.GetDataElementAccount(reqValue);
                    return value >= secondaryAsset && value <= tertiaryAsset;             
                }
                case ModifierTreeType.PlayerHasCompletedQuestOrIsReadyToTurnIn: // 392
                {
                    QuestStatus status = referencePlayer.GetQuestStatus(reqValue);
                    if (status != QuestStatus.Complete && status != QuestStatus.Rewarded)
                        return false;
                    break;
                }
                case ModifierTreeType.PlayerTitle: // 393
                    if (referencePlayer.m_playerData.PlayerTitle != reqValue)
                        return false;
                    break;
                case ModifierTreeType.PlayerIsInGuild: // 404
                    if (referencePlayer.GetGuildId() == 0)
                        return false;
                    break;
                default:
                    return false;
            }
            return true;
        }

        public virtual void SendAllData(Player receiver) { }
        public virtual void SendCriteriaUpdate(Criteria criteria, CriteriaProgress progress, TimeSpan timeElapsed, bool timedCompleted) { }
        public virtual void SendCriteriaProgressRemoved(uint criteriaId) { }

        public virtual void CompletedCriteriaTree(CriteriaTree tree, Player referencePlayer) { }
        public virtual void AfterCriteriaTreeUpdate(CriteriaTree tree, Player referencePlayer) { }

        public virtual void SendPacket(ServerPacket data) { }

        public virtual bool RequiredAchievementSatisfied(uint achievementId) { return false; }

        public virtual string GetOwnerInfo() { return ""; }
        public virtual List<Criteria> GetCriteriaByType(CriteriaType type, uint asset) { return null; }
    }

    public class CriteriaManager : Singleton<CriteriaManager>
{
    Dictionary<uint, CriteriaDataSet> _criteriaDataMap = new();

    Dictionary<uint, CriteriaTree> _criteriaTrees = new();
    Dictionary<uint, Criteria> _criteria = new();
    Dictionary<uint, ModifierTreeNode> _criteriaModifiers = new();

    MultiMap<uint, CriteriaTree> _criteriaTreeByCriteria = new();

    // store criterias by type to speed up lookup
    MultiMap<CriteriaType, Criteria> _criteriasByType = new();
    MultiMap<uint, Criteria>[] _criteriasByAsset = new MultiMap<uint, Criteria>[(int)CriteriaType.Count];
    MultiMap<CriteriaType, Criteria> _guildCriteriasByType = new();
    MultiMap<uint, Criteria>[] _scenarioCriteriasByTypeAndScenarioId = new MultiMap<uint, Criteria>[(int)CriteriaType.Count];
    MultiMap<CriteriaType, Criteria> _questObjectiveCriteriasByType = new();

    MultiMap<int, Criteria>[] _criteriasByStartEvent = new MultiMap<int, Criteria>[(int)CriteriaStartEvent.Count];
    MultiMap<int, Criteria>[] _criteriasByFailEvent = new MultiMap<int, Criteria>[(int)CriteriaFailEvent.Count];

    CriteriaManager()
    {
        for (var i = 0; i < (int)CriteriaType.Count; ++i)
        {
            _criteriasByAsset[i] = new MultiMap<uint, Criteria>();
            _scenarioCriteriasByTypeAndScenarioId[i] = new MultiMap<uint, Criteria>();
        }

        for (var i = 0; i < (int)CriteriaStartEvent.Count; ++i)
            _criteriasByStartEvent[i] = new();

        for (var i = 0; i < (int)CriteriaFailEvent.Count; ++i)
            _criteriasByFailEvent[i] = new();

    }

    public void LoadCriteriaModifiersTree()
    {
        uint oldMSTime = Time.GetMSTime();

        if (CliDB.ModifierTreeStorage.Empty())
        {
            Log.outInfo(LogFilter.ServerLoading, "Loaded 0 criteria modifiers.");
            return;
        }

        // Load modifier tree nodes
        foreach (var tree in CliDB.ModifierTreeStorage.Values)
        {
            ModifierTreeNode node = new();
            node.Entry = tree;
            _criteriaModifiers[node.Entry.Id] = node;
        }

        // Build tree
        foreach (var treeNode in _criteriaModifiers.Values)
        {
            ModifierTreeNode parentNode = _criteriaModifiers.LookupByKey(treeNode.Entry.Parent);
            if (parentNode != null)
                parentNode.Children.Add(treeNode);
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

        Dictionary<uint /*criteriaTreeID*/, AchievementRecord> achievementCriteriaTreeIds = new();
        foreach (AchievementRecord achievement in CliDB.AchievementStorage.Values)
            if (achievement.CriteriaTree != 0)
                achievementCriteriaTreeIds[achievement.CriteriaTree] = achievement;

        Dictionary<uint, ScenarioStepRecord> scenarioCriteriaTreeIds = new();
        foreach (ScenarioStepRecord scenarioStep in CliDB.ScenarioStepStorage.Values)
        {
            if (scenarioStep.CriteriaTreeId != 0)
                scenarioCriteriaTreeIds[scenarioStep.CriteriaTreeId] = scenarioStep;
        }

        Dictionary<uint /*criteriaTreeID*/, QuestObjective> questObjectiveCriteriaTreeIds = new();
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

            CriteriaTree criteriaTree = new();
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
            CriteriaTree parent = _criteriaTrees.LookupByKey(pair.Value.Entry.Parent);
            if (parent != null)
                parent.Children.Add(pair.Value);

            if (CliDB.CriteriaStorage.HasRecord(pair.Value.Entry.CriteriaID))
                _criteriaTreeByCriteria.Add(pair.Value.Entry.CriteriaID, pair.Value);
        }

        for (var i = 0; i < (int)CriteriaFailEvent.Count; ++i)
            _criteriasByFailEvent[i] = new MultiMap<int, Criteria>();

        // Load criteria
        uint criterias = 0;
        uint guildCriterias = 0;
        uint scenarioCriterias = 0;
        uint questObjectiveCriterias = 0;
        foreach (CriteriaRecord criteriaEntry in CliDB.CriteriaStorage.Values)
        {
            Cypher.Assert(criteriaEntry.Type < CriteriaType.Count, $"CriteriaType.Count must be greater than or equal to {criteriaEntry.Type + 1} but is currently equal to {CriteriaType.Count}");
            Cypher.Assert(criteriaEntry.StartEvent < (byte)CriteriaStartEvent.Count, $"CriteriaStartEvent.Count must be greater than or equal to {criteriaEntry.StartEvent + 1} but is currently equal to {CriteriaStartEvent.Count}");
            Cypher.Assert(criteriaEntry.FailEvent < (byte)CriteriaFailEvent.Count, $"CriteriaFailEvent.Count must be greater than or equal to {criteriaEntry.FailEvent + 1} but is currently equal to {CriteriaFailEvent.Count}");

            var treeList = _criteriaTreeByCriteria.LookupByKey(criteriaEntry.Id);
            if (treeList.Empty())
                continue;

            Criteria criteria = new();
            criteria.Id = criteriaEntry.Id;
            criteria.Entry = criteriaEntry;
            criteria.Modifier = _criteriaModifiers.LookupByKey(criteriaEntry.ModifierTreeId);

            _criteria[criteria.Id] = criteria;

            List<uint> scenarioIds = new();
            foreach (CriteriaTree tree in treeList)
            {
                tree.Criteria = criteria;

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
                {
                    criteria.FlagsCu |= CriteriaFlagsCu.Scenario;
                    scenarioIds.Add(tree.ScenarioStep.ScenarioID);
                }
                else if (tree.QuestObjective != null)
                    criteria.FlagsCu |= CriteriaFlagsCu.QuestObjective;
            }

            if (criteria.FlagsCu.HasAnyFlag(CriteriaFlagsCu.Player | CriteriaFlagsCu.Account))
            {
                ++criterias;
                _criteriasByType.Add(criteriaEntry.Type, criteria);
                if (IsCriteriaTypeStoredByAsset(criteriaEntry.Type))
                {
                    if (criteriaEntry.Type != CriteriaType.RevealWorldMapOverlay)
                        _criteriasByAsset[(int)criteriaEntry.Type].Add(criteriaEntry.Asset, criteria);
                    else
                    {
                        var worldOverlayEntry = CliDB.WorldMapOverlayStorage.LookupByKey(criteriaEntry.Asset);
                        if (worldOverlayEntry == null)
                            break;

                        for (byte j = 0; j < SharedConst.MaxWorldMapOverlayArea; ++j)
                        {
                            if (worldOverlayEntry.AreaID[j] != 0)
                            {
                                bool valid = true;
                                for (byte i = 0; i < j; ++i)
                                    if (worldOverlayEntry.AreaID[j] == worldOverlayEntry.AreaID[i])
                                        valid = false;
                                if (valid)
                                    _criteriasByAsset[(int)criteriaEntry.Type].Add(worldOverlayEntry.AreaID[j], criteria);
                            }
                        }
                    }
                }
            }

            if (criteria.FlagsCu.HasAnyFlag(CriteriaFlagsCu.Guild))
            {
                ++guildCriterias;
                _guildCriteriasByType.Add(criteriaEntry.Type, criteria);
            }

            if (criteria.FlagsCu.HasAnyFlag(CriteriaFlagsCu.Scenario))
            {
                ++scenarioCriterias;
                foreach (uint scenarioId in scenarioIds)
                    _scenarioCriteriasByTypeAndScenarioId[(int)criteriaEntry.Type].Add(scenarioId, criteria);
            }

            if (criteria.FlagsCu.HasAnyFlag(CriteriaFlagsCu.QuestObjective))
            {
                ++questObjectiveCriterias;
                _questObjectiveCriteriasByType.Add(criteriaEntry.Type, criteria);
            }

            if (criteriaEntry.StartEvent != 0)
                _criteriasByStartEvent[criteriaEntry.StartEvent].Add((int)criteriaEntry.StartAsset, criteria);

            if (criteriaEntry.FailEvent != 0)
                _criteriasByFailEvent[criteriaEntry.FailEvent].Add((int)criteriaEntry.FailAsset, criteria);
        }

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

            CriteriaData data = new(dataType, result.Read<uint>(2), result.Read<uint>(3), scriptId);

            if (!data.IsValid(criteria))
                continue;

            // this will allocate empty data set storage
            CriteriaDataSet dataSet = new();
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

    public static CriteriaType[] GetRetroactivelyUpdateableCriteriaTypes() =>
    [
        CriteriaType.CompleteResearchProject,
            CriteriaType.CompleteAnyResearchProject,
            CriteriaType.ReachLevel,
            CriteriaType.SkillRaised,
            CriteriaType.CompleteQuestsCount,
            CriteriaType.CompleteAnyDailyQuestPerDay,
            CriteriaType.CompleteQuestsInZone,
            CriteriaType.CompleteQuest,
            CriteriaType.LearnOrKnowSpell,
            CriteriaType.AcquireItem,
            CriteriaType.EarnPersonalArenaRating,
            CriteriaType.AchieveSkillStep,
            CriteriaType.RevealWorldMapOverlay,
            //CriteriaType.EarnTitle,  /*NYI*/
            CriteriaType.BankSlotsPurchased,
            CriteriaType.ReputationGained,
            CriteriaType.TotalExaltedFactions,
            //CriteriaType.CompleteQuestsInSort,  /*NYI*/
            CriteriaType.LearnSpellFromSkillLine,
            CriteriaType.MostMoneyOwned,
            CriteriaType.TotalReveredFactions,
            CriteriaType.TotalHonoredFactions,
            CriteriaType.TotalFactionsEncountered,
            //CriteriaType.AccountKnownPet,  /*NYI*/
            CriteriaType.LearnTradeskillSkillLine,
            CriteriaType.HonorableKills,
            //CriteriaType.GuildBankTabsPurchased, /*NYI*/
            //CriteriaType.EarnGuildAchievementPoints, /*NYI*/
            //CriteriaType.EarnBattlegroundRating, /*NYI*/
            //CriteriaType.GuildTabardCreated, /*NYI*/
            CriteriaType.LearnedNewPet,
            CriteriaType.UniquePetsOwned,
            //CriteriaType.UpgradeGarrison, /*NYI*/
            //CriteriaType.AcquireGarrison, /*NYI*/
            //CriteriaType.LearnGarrisonBlueprint, /*NYI*/
            //CriteriaType.LearnGarrisonSpecialization, /*NYI*/
            //CriteriaType.LearnToy, /*NYI*/ // Learn Toy "{Item}"
            //CriteriaType.LearnAnyToy, /*NYI*/ // Learn Any Toy
            //CriteriaType.LearnTransmog, /*NYI*/
            CriteriaType.HonorLevelIncrease,
            //CriteriaType.AccountHonorLevelReached, /*NYI*/
            CriteriaType.ReachMaxLevel,
            //CriteriaType.MemorizeSpell, /*NYI*/
            //CriteriaType.LearnTransmogIllusion, /*NYI*/
            //CriteriaType.MythicPlusRatingAttained, /*NYI*/
            //CriteriaType.MythicPlusDisplaySeasonEnded, /*NYI*/
            //CriteriaType.CompleteTrackingQuest, /*NYI*/
            //CriteriaType.WarbandBankTabPurchased, /*NYI*/
            CriteriaType.LearnTaxiNode,
            CriteriaType.EarnAchievementPoints,
            CriteriaType.BattlePetAchievementPointsEarned,
            CriteriaType.EarnAchievement // criteria possibly completed by retroactive scan, must be last
    ];

    bool IsCriteriaTypeStoredByAsset(CriteriaType type)
    {
        switch (type)
        {
            case CriteriaType.KillCreature:
            case CriteriaType.WinBattleground:
            case CriteriaType.SkillRaised:
            case CriteriaType.EarnAchievement:
            case CriteriaType.CompleteQuestsInZone:
            case CriteriaType.ParticipateInBattleground:
            case CriteriaType.KilledByCreature:
            case CriteriaType.CompleteQuest:
            case CriteriaType.BeSpellTarget:
            case CriteriaType.CastSpell:
            case CriteriaType.TrackedWorldStateUIModified:
            case CriteriaType.PVPKillInArea:
            case CriteriaType.LearnOrKnowSpell:
            case CriteriaType.AcquireItem:
            case CriteriaType.AchieveSkillStep:
            case CriteriaType.UseItem:
            case CriteriaType.LootItem:
            case CriteriaType.RevealWorldMapOverlay:
            case CriteriaType.ReputationGained:
            case CriteriaType.EquipItemInSlot:
            case CriteriaType.DeliverKillingBlowToClass:
            case CriteriaType.DeliverKillingBlowToRace:
            case CriteriaType.DoEmote:
            case CriteriaType.EquipItem:
            case CriteriaType.UseGameobject:
            case CriteriaType.GainAura:
            case CriteriaType.CatchFishInFishingHole:
            case CriteriaType.LearnSpellFromSkillLine:
            case CriteriaType.DefeatDungeonEncounterWhileElegibleForLoot:
            case CriteriaType.GetLootByType:
            case CriteriaType.LandTargetedSpellOnTarget:
            case CriteriaType.LearnTradeskillSkillLine:
            case CriteriaType.DefeatDungeonEncounter:
            case CriteriaType.LearnToy:
            case CriteriaType.LearnAnyTransmog:
                return true;
            default:
                return false;
        }
    }

    public List<Criteria> GetPlayerCriteriaByType(CriteriaType type, uint asset)
    {
        if (asset != 0 && IsCriteriaTypeStoredByAsset(type))
        {
            if (_criteriasByAsset[(int)type].ContainsKey(asset))
                return _criteriasByAsset[(int)type][asset];

            return new List<Criteria>();
        }

        return _criteriasByType.LookupByKey(type);
    }

    public List<Criteria> GetScenarioCriteriaByTypeAndScenario(CriteriaType type, uint scenarioId)
    {
        return _scenarioCriteriasByTypeAndScenarioId[(int)type].LookupByKey(scenarioId);
    }

    public MultiMap<int, Criteria> GetCriteriaByStartEvent(CriteriaStartEvent startEvent)
    {
        return _criteriasByStartEvent[(int)startEvent];
    }

    public List<Criteria> GetCriteriaByStartEvent(CriteriaStartEvent startEvent, int asset)
    {
        return _criteriasByStartEvent[(int)startEvent].LookupByKey(asset);
    }

    public MultiMap<int, Criteria> GetCriteriaByFailEvent(CriteriaFailEvent failEvent)
    {
        return _criteriasByFailEvent[(int)failEvent];
    }

    public List<Criteria> GetCriteriaByFailEvent(CriteriaFailEvent failEvent, int asset)
    {
        return _criteriasByFailEvent[(int)failEvent].LookupByKey(asset);
    }

    public List<Criteria> GetGuildCriteriaByType(CriteriaType type)
    {
        return _guildCriteriasByType.LookupByKey(type);
    }

    public List<Criteria> GetQuestObjectiveCriteriaByType(CriteriaType type)
    {
        return _questObjectiveCriteriasByType[type];
    }

    public List<CriteriaTree> GetCriteriaTreesByCriteria(uint criteriaId)
    {
        return _criteriaTreeByCriteria.LookupByKey(criteriaId);
    }

    public CriteriaDataSet GetCriteriaDataSet(Criteria criteria)
    {
        return _criteriaDataMap.LookupByKey(criteria.Id);
    }

    public static bool IsGroupCriteriaType(CriteriaType type)
    {
        switch (type)
        {
            case CriteriaType.KillCreature:
            case CriteriaType.WinBattleground:
            case CriteriaType.BeSpellTarget:       // NYI
            case CriteriaType.WinAnyRankedArena:
            case CriteriaType.GainAura:            // NYI
            case CriteriaType.WinAnyBattleground:  // NYI
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
}

public class ModifierTreeNode
{
    public ModifierTreeRecord Entry;
    public List<ModifierTreeNode> Children = new();
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
    public List<CriteriaTree> Children = new();
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
            case CriteriaType.KillCreature:
            case CriteriaType.KillAnyCreature:
            case CriteriaType.WinBattleground:
            case CriteriaType.MaxDistFallenWithoutDying:
            case CriteriaType.CompleteQuest:          // only hardcoded list
            case CriteriaType.CastSpell:
            case CriteriaType.WinAnyRankedArena:
            case CriteriaType.DoEmote:
            case CriteriaType.KillPlayer:
            case CriteriaType.WinDuel:
            case CriteriaType.GetLootByType:
            case CriteriaType.LandTargetedSpellOnTarget:
            case CriteriaType.BeSpellTarget:
            case CriteriaType.GainAura:
            case CriteriaType.EquipItemInSlot:
            case CriteriaType.RollNeed:
            case CriteriaType.RollGreed:
            case CriteriaType.TrackedWorldStateUIModified:
            case CriteriaType.EarnHonorableKill:
            case CriteriaType.CompleteDailyQuest:    // only Children's Week achievements
            case CriteriaType.UseItem:                // only Children's Week achievements
            case CriteriaType.DeliveredKillingBlow:
            case CriteriaType.ReachLevel:
            case CriteriaType.Login:
            case CriteriaType.LootAnyItem:
            case CriteriaType.ObtainAnyItem:
                break;
            default:
                if (DataType != CriteriaDataType.Script)
                {
                    Log.outError(LogFilter.Sql, "Table `criteria_data` has data for non-supported criteria type (Entry: {0} Type: {1}), ignored.", criteria.Id, (CriteriaType)criteria.Entry.Type);
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
                if (!RaceMask.AllPlayable.HasRace((Race)ClassRace.RaceId))
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
                SpellInfo spellEntry = Global.SpellMgr.GetSpellInfo(Aura.SpellId, Difficulty.None);
                if (spellEntry == null)
                {
                    Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type {2} has wrong spell id in value1 ({3}), ignored.",
                        criteria.Id, criteria.Entry.Type, DataType, Aura.SpellId);
                    return false;
                }
                if (spellEntry.GetEffects().Count <= Aura.EffectIndex)
                {
                    Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type {2} has wrong spell effect index in value2 ({3}), ignored.",
                        criteria.Id, criteria.Entry.Type, DataType, Aura.EffectIndex);
                    return false;
                }
                if (spellEntry.GetEffect(Aura.EffectIndex).ApplyAuraName == 0)
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
                    return false;
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
                if (ClassRace.RaceId != 0 && !RaceMask.AllPlayable.HasRace((Race)ClassRace.RaceId))
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

    public bool Meets(uint criteriaId, Player source, WorldObject target, uint miscValue1 = 0, uint miscValue2 = 0)
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
                return !target.ToPlayer().HealthAbovePct((int)Health.Percent);
            case CriteriaDataType.SAura:
                return source.HasAuraEffect(Aura.SpellId, (byte)Aura.EffectIndex);
            case CriteriaDataType.TAura:
            {
                if (target == null)
                    return false;
                Unit unitTarget = target.ToUnit();
                if (unitTarget == null)
                    return false;
                return unitTarget.HasAuraEffect(Aura.SpellId, Aura.EffectIndex);
            }
            case CriteriaDataType.Value:
                return MathFunctions.CompareValues((ComparisionType)Value.ComparisonType, miscValue1, Value.Value);
            case CriteriaDataType.TLevel:
                if (target == null)
                    return false;
                return target.GetLevelForTarget(source) >= Level.Min;
            case CriteriaDataType.TGender:
            {
                if (target == null)
                    return false;
                Unit unitTarget = target.ToUnit();
                if (unitTarget == null)
                    return false;
                return unitTarget.GetGender() == (Gender)Gender.Gender;
            }
            case CriteriaDataType.Script:
            {
                Unit unitTarget = null;
                if (target != null)
                    unitTarget = target.ToUnit();
                return Global.ScriptMgr.OnCriteriaCheck(ScriptId, source.ToPlayer(), unitTarget.ToUnit());
            }
            case CriteriaDataType.MapPlayerCount:
                return source.GetMap().GetPlayersCountExceptGMs() <= MapPlayers.MaxCount;
            case CriteriaDataType.TTeam:
                if (target == null || !target.IsTypeId(TypeId.Player))
                    return false;
                return target.ToPlayer().GetTeam() == (Team)TeamId.Team;
            case CriteriaDataType.SDrunk:
                return Player.GetDrunkenstateByValue(source.GetDrunkValue()) >= (DrunkenState)Drunk.State;
            case CriteriaDataType.Holiday:
                return Global.GameEventMgr.IsHolidayActive((HolidayIds)Holiday.Id);
            case CriteriaDataType.GameEvent:
                return Global.GameEventMgr.IsEventActive((ushort)GameEvent.Id);
            case CriteriaDataType.BgLossTeamScore:
            {
                Battleground bg = source.GetBattleground();
                if (bg == null)
                    return false;

                int score = (int)bg.GetTeamScore(bg.GetPlayerTeam(source.GetGUID()) == Team.Alliance ? BattleGroundTeamId.Horde : BattleGroundTeamId.Alliance);
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
                        CriteriaDataType.InstanceScript, criteriaId, map.GetId());
                    return false;
                }
                InstanceScript instance = ((InstanceMap)map).GetInstanceScript();
                if (instance == null)
                {
                    Log.outError(LogFilter.Achievement, "Achievement system call criteria_data_INSTANCE_SCRIPT ({0}) for achievement criteria {1} for map {2} but map does not have a instance script",
                        CriteriaDataType.InstanceScript, criteriaId, map.GetId());
                    return false;
                }

                Unit unitTarget = null;
                if (target != null)
                    unitTarget = target.ToUnit();
                return instance.CheckAchievementCriteriaMeet(criteriaId, source, unitTarget, miscValue1);
            }
            case CriteriaDataType.SEquippedItem:
            {
                Criteria entry = Global.CriteriaMgr.GetCriteria(criteriaId);

                uint itemId = entry.Entry.Type == CriteriaType.EquipItemInSlot ? miscValue2 : miscValue1;
                ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(itemId);
                if (itemTemplate == null)
                    return false;
                return itemTemplate.GetBaseItemLevel() >= EquippedItem.ItemLevel && (uint)itemTemplate.GetQuality() >= EquippedItem.ItemQuality;
            }
            case CriteriaDataType.MapId:
                return source.GetMapId() == MapId.Id;
            case CriteriaDataType.SKnownTitle:
            {
                CharTitlesRecord titleInfo = CliDB.CharTitlesStorage.LookupByKey(KnownTitle.Id);
                if (titleInfo != null)
                    return source != null && source.HasTitle(titleInfo.MaskID);

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
    uint _criteriaId;
    List<CriteriaData> _storage = new();

    public void Add(CriteriaData data) { _storage.Add(data); }

    public bool Meets(Player source, WorldObject target, uint miscValue = 0, uint miscValue2 = 0)
    {
        foreach (var data in _storage)
            if (!data.Meets(_criteriaId, source, target, miscValue, miscValue2))
                return false;

        return true;
    }

    public void SetCriteriaId(uint id) { _criteriaId = id; }
}
}
