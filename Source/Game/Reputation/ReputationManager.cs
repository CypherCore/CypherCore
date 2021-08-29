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
using Game.DataStorage;
using Game.Entities;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public class ReputationMgr
    {
        public ReputationMgr(Player owner)
        {
            _player = owner;
            _visibleFactionCount = 0;
            _honoredFactionCount = 0;
            _reveredFactionCount = 0;
            _exaltedFactionCount = 0;
            _sendFactionIncreased = false;
        }

        ReputationRank ReputationToRankHelper<T>(IList<T> thresholds, int standing, Func<T, int> thresholdExtractor)
        {
            int i = 0;
            int rank = -1;
            while (i != thresholds.Count - 1 && standing >= thresholdExtractor(thresholds[i]))
            {
                ++rank;
                ++i;
            }

            return (ReputationRank)rank;
        }

        ReputationRank ReputationToRank(FactionRecord factionEntry, int standing)
        {
            ReputationRank rank = ReputationRank.Min;

            var friendshipReactions = Global.DB2Mgr.GetFriendshipRepReactions(factionEntry.FriendshipRepID);
            if (!friendshipReactions.Empty())
                rank = ReputationToRankHelper(friendshipReactions, standing, (FriendshipRepReactionRecord frr) => { return frr.ReactionThreshold; });
            else
                rank = ReputationToRankHelper(ReputationRankThresholds, standing, (int threshold) => { return threshold; });

            return rank;
        }
        
        public FactionState GetState(FactionRecord factionEntry)
        {
            return factionEntry.CanHaveReputation() ? GetState(factionEntry.ReputationIndex) : null;
        }

        public bool IsAtWar(uint factionId)
        {
            var factionEntry = CliDB.FactionStorage.LookupByKey(factionId);
            if (factionEntry == null)
                return false;

            return IsAtWar(factionEntry);
        }

        public bool IsAtWar(FactionRecord factionEntry)
        {
            if (factionEntry == null)
                return false;

            FactionState factionState = GetState(factionEntry);
            if (factionState != null)
                return factionState.Flags.HasFlag(ReputationFlags.AtWar);
            return false;
        }

        public int GetReputation(uint faction_id)
        {
            var factionEntry = CliDB.FactionStorage.LookupByKey(faction_id);
            if (factionEntry == null)
            {
                Log.outError(LogFilter.Player, "ReputationMgr.GetReputation: Can't get reputation of {0} for unknown faction (faction id) #{1}.", _player.GetName(), faction_id);
                return 0;
            }

            return GetReputation(factionEntry);
        }

        public int GetBaseReputation(FactionRecord factionEntry)
        {
            int dataIndex = GetFactionDataIndexForRaceAndClass(factionEntry);
            if (dataIndex < 0)
                return 0;

            return factionEntry.ReputationBase[dataIndex];
        }

        int GetMinReputation(FactionRecord factionEntry)
        {
            var friendshipReactions = Global.DB2Mgr.GetFriendshipRepReactions(factionEntry.FriendshipRepID);
            if (!friendshipReactions.Empty())
                return friendshipReactions[0].ReactionThreshold;

            return ReputationRankThresholds[0];
        }

        int GetMaxReputation(FactionRecord factionEntry)
        {
            ParagonReputationRecord paragonReputation = Global.DB2Mgr.GetParagonReputation(factionEntry.Id);
            if (paragonReputation != null)
            {
                // has reward quest, cap is just before threshold for another quest reward
                // for example: if current reputation is 12345 and questa are given every 10000 and player has unclaimed reward
                // then cap will be 19999

                // otherwise cap is one theshold level larger
                // if current reputation is 12345 and questa are given every 10000 and player does NOT have unclaimed reward
                // then cap will be 29999

                int reputation = GetReputation(factionEntry);
                int cap = reputation + paragonReputation.LevelThreshold - reputation % paragonReputation.LevelThreshold - 1;

                if (_player.GetQuestStatus((uint)paragonReputation.QuestID) == QuestStatus.None)
                    cap += paragonReputation.LevelThreshold;

                return cap;
            }

            var friendshipReactions = Global.DB2Mgr.GetFriendshipRepReactions(factionEntry.FriendshipRepID);
            if (!friendshipReactions.Empty())
                return friendshipReactions.LastOrDefault().ReactionThreshold;

            int dataIndex = GetFactionDataIndexForRaceAndClass(factionEntry);
            if (dataIndex >= 0)
                return factionEntry.ReputationMax[dataIndex];

            return ReputationRankThresholds.LastOrDefault();
        }
        
        public int GetReputation(FactionRecord factionEntry)
        {
            // Faction without recorded reputation. Just ignore.
            if (factionEntry == null)
                return 0;

            FactionState state = GetState(factionEntry);
            if (state != null)
                return GetBaseReputation(factionEntry) + state.Standing;

            return 0;
        }

        public ReputationRank GetRank(FactionRecord factionEntry)
        {
            int reputation = GetReputation(factionEntry);
            return ReputationToRank(factionEntry, reputation);
        }

        ReputationRank GetBaseRank(FactionRecord factionEntry)
        {
            int reputation = GetBaseReputation(factionEntry);
            return ReputationToRank(factionEntry, reputation);
        }

        public ReputationRank GetForcedRankIfAny(FactionTemplateRecord factionTemplateEntry)
        {
            return GetForcedRankIfAny(factionTemplateEntry.Faction);
        }

        public int GetParagonLevel(uint paragonFactionId)
        {
            return GetParagonLevel(CliDB.FactionStorage.LookupByKey(paragonFactionId));
        }

        int GetParagonLevel(FactionRecord paragonFactionEntry)
        {
            if (paragonFactionEntry == null)
                return 0;

            ParagonReputationRecord paragonReputation = Global.DB2Mgr.GetParagonReputation(paragonFactionEntry.Id);
            if (paragonReputation != null)
                return GetReputation(paragonFactionEntry) / paragonReputation.LevelThreshold;

            return 0;
        }
        
        public void ApplyForceReaction(uint faction_id, ReputationRank rank, bool apply)
        {
            if (apply)
                _forcedReactions[faction_id] = rank;
            else
                _forcedReactions.Remove(faction_id);
        }

        ReputationFlags GetDefaultStateFlags(FactionRecord factionEntry)
        {
            ReputationFlags flags = ReputationFlags.None;

            int dataIndex = GetFactionDataIndexForRaceAndClass(factionEntry);
            if (dataIndex > 0)
                flags = (ReputationFlags)factionEntry.ReputationFlags[dataIndex];

            if (Global.DB2Mgr.GetParagonReputation(factionEntry.Id) != null)
                flags |= ReputationFlags.ShowPropagated;

            return flags;
        }

        public void SendForceReactions()
        {
            SetForcedReactions setForcedReactions = new();

            foreach (var pair in _forcedReactions)
            {
                ForcedReaction forcedReaction;
                forcedReaction.Faction = (int)pair.Key;
                forcedReaction.Reaction = (int)pair.Value;

                setForcedReactions.Reactions.Add(forcedReaction);
            }
            _player.SendPacket(setForcedReactions);
        }

        public void SendState(FactionState faction)
        {
            SetFactionStanding setFactionStanding = new();
            setFactionStanding.ReferAFriendBonus = 0.0f;
            setFactionStanding.BonusFromAchievementSystem = 0.0f;
            if (faction != null)
                setFactionStanding.Faction.Add(new FactionStandingData((int)faction.ReputationListID, faction.Standing));

            foreach (var state in _factions.Values)
            {
                if (state.needSend)
                {
                    state.needSend = false;
                    if (faction == null || state.ReputationListID != faction.ReputationListID)
                        setFactionStanding.Faction.Add(new FactionStandingData((int)state.ReputationListID, state.Standing));
                }
            }

            setFactionStanding.ShowVisual = _sendFactionIncreased;
            _player.SendPacket(setFactionStanding);

            _sendFactionIncreased = false; // Reset
        }

        public void SendInitialReputations()
        {
            InitializeFactions initFactions = new();

            foreach (var pair in _factions)
            {
                initFactions.FactionFlags[pair.Key] = pair.Value.Flags;
                initFactions.FactionStandings[pair.Key] = pair.Value.Standing;
                // @todo faction bonus
                pair.Value.needSend = false;
            }

            _player.SendPacket(initFactions);
        }

        public void SendVisible(FactionState faction, bool visible = true)
        {
            if (_player.GetSession().PlayerLoading())
                return;

            //make faction visible / not visible in reputation list at client
            SetFactionVisible packet = new(visible);
            packet.FactionIndex = faction.ReputationListID;
            _player.SendPacket(packet);
        }

        void Initialize()
        {
            _factions.Clear();
            _visibleFactionCount = 0;
            _honoredFactionCount = 0;
            _reveredFactionCount = 0;
            _exaltedFactionCount = 0;
            _sendFactionIncreased = false;

            foreach (var factionEntry in CliDB.FactionStorage.Values)
            {
                if (factionEntry.CanHaveReputation())
                {
                    FactionState newFaction = new();
                    newFaction.Id = factionEntry.Id;
                    newFaction.ReputationListID = (uint)factionEntry.ReputationIndex;
                    newFaction.Standing = 0;
                    newFaction.Flags = GetDefaultStateFlags(factionEntry);
                    newFaction.needSend = true;
                    newFaction.needSave = true;

                    if (newFaction.Flags.HasFlag(ReputationFlags.Visible))
                        ++_visibleFactionCount;

                    if (factionEntry.FriendshipRepID == 0)
                        UpdateRankCounters(ReputationRank.Hostile, GetBaseRank(factionEntry));

                    _factions[newFaction.ReputationListID] = newFaction;
                }
            }
        }

        public bool ModifyReputation(FactionRecord factionEntry, int standing, bool spillOverOnly = false, bool noSpillover = false)
        {
            return SetReputation(factionEntry, standing, true, spillOverOnly, noSpillover);
        }

        public bool SetReputation(FactionRecord factionEntry, int standing)
        {
            return SetReputation(factionEntry, standing, false, false, false);
        }
        
        public bool SetReputation(FactionRecord factionEntry, int standing, bool incremental, bool spillOverOnly, bool noSpillover)
        {
            Global.ScriptMgr.OnPlayerReputationChange(_player, factionEntry.Id, standing, incremental);
            bool res = false;
            if (!noSpillover)
            {
                // if spillover definition exists in DB, override DBC
                RepSpilloverTemplate repTemplate = Global.ObjectMgr.GetRepSpillover(factionEntry.Id);
                if (repTemplate != null)
                {
                    for (uint i = 0; i < 5; ++i)
                    {
                        if (repTemplate.faction[i] != 0)
                        {
                            if (_player.GetReputationRank(repTemplate.faction[i]) <= (ReputationRank)repTemplate.faction_rank[i])
                            {
                                // bonuses are already given, so just modify standing by rate
                                int spilloverRep = (int)(standing * repTemplate.faction_rate[i]);
                                SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(repTemplate.faction[i]), spilloverRep, incremental);
                            }
                        }
                    }
                }
                else
                {
                    float spillOverRepOut = standing;
                    // check for sub-factions that receive spillover
                    var flist = Global.DB2Mgr.GetFactionTeamList(factionEntry.Id);
                    // if has no sub-factions, check for factions with same parent
                    if (flist == null && factionEntry.ParentFactionID != 0 && factionEntry.ParentFactionMod[1] != 0.0f)
                    {
                        spillOverRepOut *= factionEntry.ParentFactionMod[1];
                        FactionRecord parent = CliDB.FactionStorage.LookupByKey(factionEntry.ParentFactionID);
                        if (parent != null)
                        {
                            var parentState = _factions.LookupByKey(parent.ReputationIndex);
                            // some team factions have own reputation standing, in this case do not spill to other sub-factions
                            if (parentState != null && parentState.Flags.HasFlag(ReputationFlags.HeaderShowsBar))
                            {
                                SetOneFactionReputation(parent, (int)spillOverRepOut, incremental);
                            }
                            else    // spill to "sister" factions
                            {
                                flist = Global.DB2Mgr.GetFactionTeamList(factionEntry.ParentFactionID);
                            }
                        }
                    }
                    if (flist != null)
                    {
                        // Spillover to affiliated factions
                        foreach (var id in flist)
                        {
                            FactionRecord factionEntryCalc = CliDB.FactionStorage.LookupByKey(id);
                            if (factionEntryCalc != null)
                            {
                                if (factionEntryCalc == factionEntry || GetRank(factionEntryCalc) > (ReputationRank)factionEntryCalc.ParentFactionMod[0])
                                    continue;
                                int spilloverRep = (int)(spillOverRepOut * factionEntryCalc.ParentFactionMod[0]);
                                if (spilloverRep != 0 || !incremental)
                                    res = SetOneFactionReputation(factionEntryCalc, spilloverRep, incremental);
                            }
                        }
                    }
                }
            }

            // spillover done, update faction itself
            var faction = _factions.LookupByKey(factionEntry.ReputationIndex);
            if (faction != null)
            {
                FactionRecord primaryFactionToModify = factionEntry;
                if (incremental && standing > 0 && CanGainParagonReputationForFaction(factionEntry))
                {
                    primaryFactionToModify = CliDB.FactionStorage.LookupByKey(factionEntry.ParagonFactionID);
                    faction = _factions.LookupByKey(primaryFactionToModify.ReputationIndex);
                }

                if (faction != null)
                {
                    // if we update spillover only, do not update main reputation (rank exceeds creature reward rate)
                    if (!spillOverOnly)
                        res = SetOneFactionReputation(primaryFactionToModify, standing, incremental);

                    // only this faction gets reported to client, even if it has no own visible standing
                    SendState(faction);
                }
            }
            return res;
        }

        public bool SetOneFactionReputation(FactionRecord factionEntry, int standing, bool incremental)
        {
            var factionState = _factions.LookupByKey((uint)factionEntry.ReputationIndex);
            if (factionState != null)
            {
                int BaseRep = GetBaseReputation(factionEntry);

                if (incremental)
                {
                    // int32 *= float cause one point loss?
                    standing = (int)(Math.Floor(standing * WorldConfig.GetFloatValue(WorldCfg.RateReputationGain) + 0.5f));
                    standing += factionState.Standing + BaseRep;
                }

                if (standing > GetMaxReputation(factionEntry))
                    standing = GetMaxReputation(factionEntry);
                else if (standing < GetMinReputation(factionEntry))
                    standing = GetMinReputation(factionEntry);

                ReputationRank old_rank = ReputationToRank(factionEntry, factionState.Standing + BaseRep);
                ReputationRank new_rank = ReputationToRank(factionEntry, standing);

                int oldStanding = factionState.Standing + BaseRep;
                int newStanding = standing - BaseRep;

                _player.ReputationChanged(factionEntry, newStanding - factionState.Standing);

                factionState.Standing = newStanding;
                factionState.needSend = true;
                factionState.needSave = true;

                SetVisible(factionState);

                if (new_rank <= ReputationRank.Hostile)
                    SetAtWar(factionState, true);

                if (new_rank > old_rank)
                    _sendFactionIncreased = true;

                ParagonReputationRecord paragonReputation = Global.DB2Mgr.GetParagonReputation(factionEntry.Id);
                if (paragonReputation != null)
                {
                    int oldParagonLevel = oldStanding / paragonReputation.LevelThreshold;
                    int newParagonLevel = standing / paragonReputation.LevelThreshold;
                    if (oldParagonLevel != newParagonLevel)
                    {
                        Quest paragonRewardQuest = Global.ObjectMgr.GetQuestTemplate((uint)paragonReputation.QuestID);
                        if (paragonRewardQuest != null)
                            _player.AddQuestAndCheckCompletion(paragonRewardQuest, null);
                    }
                }

                if (factionEntry.FriendshipRepID == 0 && paragonReputation == null)
                    UpdateRankCounters(old_rank, new_rank);

                _player.UpdateCriteria(CriteriaTypes.KnownFactions, factionEntry.Id);
                _player.UpdateCriteria(CriteriaTypes.GainReputation, factionEntry.Id);
                _player.UpdateCriteria(CriteriaTypes.GainExaltedReputation, factionEntry.Id);
                _player.UpdateCriteria(CriteriaTypes.GainReveredReputation, factionEntry.Id);
                _player.UpdateCriteria(CriteriaTypes.GainHonoredReputation, factionEntry.Id);

                return true;
            }
            return false;
        }

        public void SetVisible(FactionTemplateRecord factionTemplateEntry)
        {
            if (factionTemplateEntry.Faction == 0)
                return;

            var factionEntry = CliDB.FactionStorage.LookupByKey(factionTemplateEntry.Faction);
            if (factionEntry.Id != 0)
                // Never show factions of the opposing team
                if (!Convert.ToBoolean(factionEntry.ReputationRaceMask[1] & SharedConst.GetMaskForRace(_player.GetRace())) && factionEntry.ReputationBase[1] == SharedConst.ReputationBottom)
                    SetVisible(factionEntry);
        }

        public void SetVisible(FactionRecord factionEntry)
        {
            if (!factionEntry.CanHaveReputation())
                return;

            var factionState = _factions.LookupByKey((uint)factionEntry.ReputationIndex);
            if (factionState == null)
                return;

            SetVisible(factionState);
        }

        void SetVisible(FactionState faction)
        {
            // always invisible or hidden faction can't be make visible
            if (faction.Flags.HasFlag(ReputationFlags.Hidden))
                return;

            if (faction.Flags.HasFlag(ReputationFlags.Header) && !faction.Flags.HasFlag(ReputationFlags.HeaderShowsBar))
                return;

            if (Global.DB2Mgr.GetParagonReputation(faction.Id) != null)
                return;

            // already set
            if (faction.Flags.HasFlag(ReputationFlags.Visible))
                return;

            faction.Flags |= ReputationFlags.Visible;
            faction.needSend = true;
            faction.needSave = true;

            _visibleFactionCount++;

            SendVisible(faction);
        }

        public void SetAtWar(uint repListID, bool on)
        {
            var factionState = _factions.LookupByKey(repListID);
            if (factionState == null)
                return;

            // always invisible or hidden faction can't change war state
            if (factionState.Flags.HasAnyFlag(ReputationFlags.Hidden | ReputationFlags.Header))
                return;

            SetAtWar(factionState, on);
        }

        void SetAtWar(FactionState faction, bool atWar)
        {
            // Do not allow to declare war to our own faction. But allow for rival factions (eg Aldor vs Scryer).
            if (atWar && faction.Flags.HasFlag(ReputationFlags.Peaceful))
                return;

            // already set
            if (faction.Flags.HasFlag(ReputationFlags.AtWar) == atWar)
                return;

            if (atWar)
                faction.Flags |= ReputationFlags.AtWar;
            else
                faction.Flags &= ~ReputationFlags.AtWar;

            faction.needSend = true;
            faction.needSave = true;
        }

        public void SetInactive(uint repListID, bool on)
        {
            var factionState = _factions.LookupByKey(repListID);
            if (factionState == null)
                return;

            SetInactive(factionState, on);
        }

        void SetInactive(FactionState faction, bool inactive)
        {
            // always invisible or hidden faction can't be inactive
            if (faction.Flags.HasAnyFlag(ReputationFlags.Hidden | ReputationFlags.Header) || !faction.Flags.HasFlag(ReputationFlags.Visible))
                return;

            // already set
            if (faction.Flags.HasFlag(ReputationFlags.Inactive) == inactive)
                return;

            if (inactive)
                faction.Flags |= ReputationFlags.Inactive;
            else
                faction.Flags &= ~ReputationFlags.Inactive;

            faction.needSend = true;
            faction.needSave = true;
        }

        public void LoadFromDB(SQLResult result)
        {
            // Set initial reputations (so everything is nifty before DB data load)
            Initialize();

            if (!result.IsEmpty())
            {
                do
                {
                    var factionEntry = CliDB.FactionStorage.LookupByKey(result.Read<uint>(0));
                    if (factionEntry != null && factionEntry.CanHaveReputation())
                    {
                        var faction = _factions.LookupByKey((uint)factionEntry.ReputationIndex);
                        if (faction == null)
                            continue;
                        // update standing to current
                        faction.Standing = result.Read<int>(1);

                        // update counters
                        if (factionEntry.FriendshipRepID == 0)
                        {
                            int BaseRep = GetBaseReputation(factionEntry);
                            ReputationRank old_rank = ReputationToRank(factionEntry, BaseRep);
                            ReputationRank new_rank = ReputationToRank(factionEntry, BaseRep + faction.Standing);
                            UpdateRankCounters(old_rank, new_rank);
                        }

                        ReputationFlags dbFactionFlags = (ReputationFlags)result.Read<uint>(2);

                        if (dbFactionFlags.HasFlag(ReputationFlags.Visible))
                            SetVisible(faction);                    // have internal checks for forced invisibility

                        if (dbFactionFlags.HasFlag(ReputationFlags.Inactive))
                            SetInactive(faction, true);              // have internal checks for visibility requirement

                        if (dbFactionFlags.HasFlag(ReputationFlags.AtWar))  // DB at war
                            SetAtWar(faction, true);                 // have internal checks for FACTION_FLAG_PEACE_FORCED
                        else                                        // DB not at war
                        {
                            // allow remove if visible (and then not FACTION_FLAG_INVISIBLE_FORCED or FACTION_FLAG_HIDDEN)
                            if (faction.Flags.HasFlag(ReputationFlags.Visible))
                                SetAtWar(faction, false);            // have internal checks for FACTION_FLAG_PEACE_FORCED
                        }

                        // set atWar for hostile
                        if (GetRank(factionEntry) <= ReputationRank.Hostile)
                            SetAtWar(faction, true);

                        // reset changed flag if values similar to saved in DB
                        if (faction.Flags == dbFactionFlags)
                        {
                            faction.needSend = false;
                            faction.needSave = false;
                        }
                    }
                } while (result.NextRow());
            }
        }

        public void SaveToDB(SQLTransaction trans)
        {
            foreach (var factionState in _factions.Values)
            {
                if (factionState.needSave)
                {
                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_REPUTATION_BY_FACTION);
                    stmt.AddValue(0, _player.GetGUID().GetCounter());
                    stmt.AddValue(1, factionState.Id);
                    trans.Append(stmt);

                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_REPUTATION_BY_FACTION);
                    stmt.AddValue(0, _player.GetGUID().GetCounter());
                    stmt.AddValue(1, factionState.Id);
                    stmt.AddValue(2, factionState.Standing);
                    stmt.AddValue(3, (ushort)factionState.Flags);
                    trans.Append(stmt);

                    factionState.needSave = false;
                }
            }
        }

        void UpdateRankCounters(ReputationRank old_rank, ReputationRank new_rank)
        {
            if (old_rank >= ReputationRank.Exalted)
                --_exaltedFactionCount;
            if (old_rank >= ReputationRank.Revered)
                --_reveredFactionCount;
            if (old_rank >= ReputationRank.Honored)
                --_honoredFactionCount;

            if (new_rank >= ReputationRank.Exalted)
                ++_exaltedFactionCount;
            if (new_rank >= ReputationRank.Revered)
                ++_reveredFactionCount;
            if (new_rank >= ReputationRank.Honored)
                ++_honoredFactionCount;
        }

        int GetFactionDataIndexForRaceAndClass(FactionRecord factionEntry)
        {
            if (factionEntry == null)
                return -1;

            long raceMask = SharedConst.GetMaskForRace(_player.GetRace());
            short classMask = (short)_player.GetClassMask();
            for (int i = 0; i < 4; i++)
            {
                if ((factionEntry.ReputationRaceMask[i].HasAnyFlag(raceMask) || (factionEntry.ReputationRaceMask[i] == 0 && factionEntry.ReputationClassMask[i] != 0))
                    && (factionEntry.ReputationClassMask[i].HasAnyFlag(classMask) || factionEntry.ReputationClassMask[i] == 0))

                    return i;
            }

            return -1;
        }

        bool CanGainParagonReputationForFaction(FactionRecord factionEntry)
        {
            if (!CliDB.FactionStorage.ContainsKey(factionEntry.ParagonFactionID))
                return false;

            if (GetRank(factionEntry) != ReputationRank.Exalted)
                return false;

            ParagonReputationRecord paragonReputation = Global.DB2Mgr.GetParagonReputation(factionEntry.ParagonFactionID);
            if (paragonReputation == null)
                return false;

            Quest quest = Global.ObjectMgr.GetQuestTemplate((uint)paragonReputation.QuestID);
            if (quest == null)
                return false;

            return _player.GetLevel() >= _player.GetQuestMinLevel(quest);
        }
        
        public byte GetVisibleFactionCount() { return _visibleFactionCount; }

        public byte GetHonoredFactionCount() { return _honoredFactionCount; }

        public byte GetReveredFactionCount() { return _reveredFactionCount; }

        public byte GetExaltedFactionCount() { return _exaltedFactionCount; }

        public SortedDictionary<uint, FactionState> GetStateList() { return _factions; }

        public FactionState GetState(int id)
        {
            return _factions.LookupByKey((uint)id);
        }

        public uint GetReputationRankStrIndex(FactionRecord factionEntry)
        {
            return (uint)ReputationRankStrIndex[(int)GetRank(factionEntry)];
        }

        public ReputationRank GetForcedRankIfAny(uint factionId)
        {
            var forced = _forcedReactions.ContainsKey(factionId);
            return forced ? _forcedReactions[factionId] : ReputationRank.None;
        }

        // this allows calculating base reputations to offline players, just by race and class
        public static int GetBaseReputationOf(FactionRecord factionEntry, Race race, Class playerClass)
        {
            if (factionEntry == null)
                return 0;

            long raceMask = SharedConst.GetMaskForRace(race);
            uint classMask = (1u << ((int)playerClass - 1));

            for (int i = 0; i < 4; i++)
            {
                if ((factionEntry.ReputationClassMask[i] == 0 || factionEntry.ReputationClassMask[i].HasAnyFlag((short)classMask))
                    && (factionEntry.ReputationRaceMask[i] == 0 || factionEntry.ReputationRaceMask[i].HasAnyFlag(raceMask)))
                    return factionEntry.ReputationBase[i];
            }

            return 0;
        }

        #region Fields
        Player _player;
        byte _visibleFactionCount;
        byte _honoredFactionCount;
        byte _reveredFactionCount;
        byte _exaltedFactionCount;
        bool _sendFactionIncreased; //! Play visual effect on next SMSG_SET_FACTION_STANDING sent
        #endregion

        public static int[] ReputationRankThresholds =
        {
            -42000,
            // Hated
            -6000,
            // Hostile
            -3000,
            // Unfriendly
            0,
            // Neutral
            3000,
            // Friendly
            9000,
            // Honored
            21000,
            // Revered
            42000
            // Exalted
        };

        public static CypherStrings[] ReputationRankStrIndex =
        {
            CypherStrings.RepHated, CypherStrings.RepHostile, CypherStrings.RepUnfriendly, CypherStrings.RepNeutral,
            CypherStrings.RepFriendly, CypherStrings.RepHonored, CypherStrings.RepRevered, CypherStrings.RepExalted
        };

        SortedDictionary<uint, FactionState> _factions = new();
        Dictionary<uint, ReputationRank> _forcedReactions = new();
    }

    public class FactionState
    {
        public uint Id;
        public uint ReputationListID;
        public int Standing;
        public ReputationFlags Flags;
        public bool needSend;
        public bool needSave;
    }
    public class RepRewardRate
    {
        public float questRate;            // We allow rate = 0.0 in database. For this case, it means that
        public float questDailyRate;
        public float questWeeklyRate;
        public float questMonthlyRate;
        public float questRepeatableRate;
        public float creatureRate;         // no reputation are given at all for this faction/rate type.
        public float spellRate;
    }
    public class ReputationOnKillEntry
    {
        public uint RepFaction1;
        public uint RepFaction2;
        public uint ReputationMaxCap1;
        public int RepValue1;
        public uint ReputationMaxCap2;
        public int RepValue2;
        public bool IsTeamAward1;
        public bool IsTeamAward2;
        public bool TeamDependent;
    }
    public class RepSpilloverTemplate
    {
        public uint[] faction = new uint[5];
        public float[] faction_rate = new float[5];
        public uint[] faction_rank = new uint[5];
    }
}
