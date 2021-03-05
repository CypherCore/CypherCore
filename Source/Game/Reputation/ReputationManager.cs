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
using Game.DataStorage;
using Game.Entities;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;

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

        private ReputationRank ReputationToRank(int standing)
        {
            var limit = Reputation_Cap + 1;
            for (var rank = ReputationRank.Max - 1; rank >= ReputationRank.Min; --rank)
            {
                limit -= PointsInRank[(int)rank];
                if (standing >= limit)
                    return rank;
            }
            return ReputationRank.Min;
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

            var factionState = GetState(factionEntry);
            if (factionState != null)
                return (factionState.Flags.HasAnyFlag(FactionFlags.AtWar));
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
            if (factionEntry == null)
                return 0;

            var raceMask = SharedConst.GetMaskForRace(_player.GetRace());
            var classMask = _player.GetClassMask();
            for (var i = 0; i < 4; i++)
            {
                if ((Convert.ToBoolean(factionEntry.ReputationRaceMask[i] & raceMask) ||
                    (factionEntry.ReputationRaceMask[i] == 0 && factionEntry.ReputationClassMask[i] != 0)) &&
                    (Convert.ToBoolean(factionEntry.ReputationClassMask[i] & classMask) ||
                    factionEntry.ReputationClassMask[i] == 0))
                    return factionEntry.ReputationBase[i];
            }

            // in faction.dbc exist factions with (RepListId >=0, listed in character reputation list) with all BaseRepRaceMask[i] == 0
            return 0;
        }

        public int GetReputation(FactionRecord factionEntry)
        {
            // Faction without recorded reputation. Just ignore.
            if (factionEntry == null)
                return 0;

            var state = GetState(factionEntry);
            if (state != null)
                return GetBaseReputation(factionEntry) + state.Standing;

            return 0;
        }

        public ReputationRank GetRank(FactionRecord factionEntry)
        {
            var reputation = GetReputation(factionEntry);
            return ReputationToRank(reputation);
        }

        private ReputationRank GetBaseRank(FactionRecord factionEntry)
        {
            var reputation = GetBaseReputation(factionEntry);
            return ReputationToRank(reputation);
        }

        public ReputationRank GetForcedRankIfAny(FactionTemplateRecord factionTemplateEntry)
        {
            return GetForcedRankIfAny(factionTemplateEntry.Faction);
        }

        public void ApplyForceReaction(uint faction_id, ReputationRank rank, bool apply)
        {
            if (apply)
                _forcedReactions[faction_id] = rank;
            else
                _forcedReactions.Remove(faction_id);
        }

        private uint GetDefaultStateFlags(FactionRecord factionEntry)
        {
            if (factionEntry == null)
                return 0;

            var raceMask = SharedConst.GetMaskForRace(_player.GetRace());
            var classMask = _player.GetClassMask();
            for (var i = 0; i < 4; i++)
            {
                if ((Convert.ToBoolean(factionEntry.ReputationRaceMask[i] & raceMask) ||
                    (factionEntry.ReputationRaceMask[i] == 0 &&
                     factionEntry.ReputationClassMask[i] != 0)) &&
                    (Convert.ToBoolean(factionEntry.ReputationClassMask[i] & classMask) ||
                     factionEntry.ReputationClassMask[i] == 0))
                    return factionEntry.ReputationFlags[i];
            }
            return 0;
        }

        public void SendForceReactions()
        {
            var setForcedReactions = new SetForcedReactions();

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
            var setFactionStanding = new SetFactionStanding();
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
            var initFactions = new InitializeFactions();

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
            var packet = new SetFactionVisible(visible);
            packet.FactionIndex = faction.ReputationListID;
            _player.SendPacket(packet);
        }

        private void Initialize()
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
                    var newFaction = new FactionState();
                    newFaction.Id = factionEntry.Id;
                    newFaction.ReputationListID = (uint)factionEntry.ReputationIndex;
                    newFaction.Standing = 0;
                    newFaction.Flags = (FactionFlags)(GetDefaultStateFlags(factionEntry) & 0xFF);//todo fixme for higher value then byte?????
                    newFaction.needSend = true;
                    newFaction.needSave = true;

                    if (Convert.ToBoolean(newFaction.Flags & FactionFlags.Visible))
                        ++_visibleFactionCount;

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
            var res = false;
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
                                var spilloverRep = (int)(standing * repTemplate.faction_rate[i]);
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
                        var parent = CliDB.FactionStorage.LookupByKey(factionEntry.ParentFactionID);
                        if (parent != null)
                        {
                            var parentState = _factions.LookupByKey(parent.ReputationIndex);
                            // some team factions have own reputation standing, in this case do not spill to other sub-factions
                            if (parentState != null && parentState.Flags.HasAnyFlag(FactionFlags.Special))
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
                            var factionEntryCalc = CliDB.FactionStorage.LookupByKey(id);
                            if (factionEntryCalc != null)
                            {
                                if (factionEntryCalc == factionEntry || GetRank(factionEntryCalc) > (ReputationRank)factionEntryCalc.ParentFactionMod[0])
                                    continue;
                                var spilloverRep = (int)(spillOverRepOut * factionEntryCalc.ParentFactionMod[0]);
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
                // if we update spillover only, do not update main reputation (rank exceeds creature reward rate)
                if (!spillOverOnly)
                    res = SetOneFactionReputation(factionEntry, standing, incremental);

                // only this faction gets reported to client, even if it has no own visible standing
                SendState(faction);
            }
            return res;
        }

        public bool SetOneFactionReputation(FactionRecord factionEntry, int standing, bool incremental)
        {
            var factionState = _factions.LookupByKey((uint)factionEntry.ReputationIndex);
            if (factionState != null)
            {
                var BaseRep = GetBaseReputation(factionEntry);

                if (incremental)
                {
                    // int32 *= float cause one point loss?
                    standing = (int)(Math.Floor(standing * WorldConfig.GetFloatValue(WorldCfg.RateReputationGain) + 0.5f));
                    standing += factionState.Standing + BaseRep;
                }

                if (standing > Reputation_Cap)
                    standing = Reputation_Cap;
                else if (standing < Reputation_Bottom)
                    standing = Reputation_Bottom;

                var old_rank = ReputationToRank(factionState.Standing + BaseRep);
                var new_rank = ReputationToRank(standing);

                factionState.Standing = standing - BaseRep;
                factionState.needSend = true;
                factionState.needSave = true;

                SetVisible(factionState);

                if (new_rank <= ReputationRank.Hostile)
                    SetAtWar(factionState, true);

                if (new_rank > old_rank)
                    _sendFactionIncreased = true;

                UpdateRankCounters(old_rank, new_rank);

                _player.ReputationChanged(factionEntry);
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
                if (!Convert.ToBoolean(factionEntry.ReputationRaceMask[1] & SharedConst.GetMaskForRace(_player.GetRace())) && factionEntry.ReputationBase[1] == Reputation_Bottom)
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

        private void SetVisible(FactionState faction)
        {
            // always invisible or hidden faction can't be make visible
            // except if faction has FACTION_FLAG_SPECIAL
            if (Convert.ToBoolean(faction.Flags & (FactionFlags.InvisibleForced | FactionFlags.Hidden)) && !Convert.ToBoolean(faction.Flags & FactionFlags.Special))
                return;

            // already set
            if (Convert.ToBoolean(faction.Flags & FactionFlags.Visible))
                return;

            faction.Flags |= FactionFlags.Visible;
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
            if (Convert.ToBoolean(factionState.Flags & (FactionFlags.InvisibleForced | FactionFlags.Hidden)))
                return;

            SetAtWar(factionState, on);
        }

        private void SetAtWar(FactionState faction, bool atWar)
        {
            // Do not allow to declare war to our own faction. But allow for rival factions (eg Aldor vs Scryer).
            if (atWar && faction.Flags.HasAnyFlag(FactionFlags.PeaceForced) && !faction.Flags.HasAnyFlag(FactionFlags.Rival))
                return;

            // already set
            if (((faction.Flags & FactionFlags.AtWar) != 0) == atWar)
                return;

            if (atWar)
                faction.Flags |= FactionFlags.AtWar;
            else
                faction.Flags &= ~FactionFlags.AtWar;

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

        private void SetInactive(FactionState faction, bool inactive)
        {
            // always invisible or hidden faction can't be inactive
            if (inactive && Convert.ToBoolean(faction.Flags & (FactionFlags.InvisibleForced | FactionFlags.Hidden)) || !Convert.ToBoolean(faction.Flags & FactionFlags.Visible))
                return;

            // already set
            if (((faction.Flags & FactionFlags.Inactive) != 0) == inactive)
                return;

            if (inactive)
                faction.Flags |= FactionFlags.Inactive;
            else
                faction.Flags &= ~FactionFlags.Inactive;

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
                        var BaseRep = GetBaseReputation(factionEntry);
                        var old_rank = ReputationToRank(BaseRep);
                        var new_rank = ReputationToRank(BaseRep + faction.Standing);
                        UpdateRankCounters(old_rank, new_rank);

                        var dbFactionFlags = (FactionFlags)result.Read<uint>(2);

                        if (Convert.ToBoolean(dbFactionFlags & FactionFlags.Visible))
                            SetVisible(faction);                    // have internal checks for forced invisibility

                        if (Convert.ToBoolean(dbFactionFlags & FactionFlags.Inactive))
                            SetInactive(faction, true);              // have internal checks for visibility requirement

                        if (Convert.ToBoolean(dbFactionFlags & FactionFlags.AtWar))  // DB at war
                            SetAtWar(faction, true);                 // have internal checks for FACTION_FLAG_PEACE_FORCED
                        else                                        // DB not at war
                        {
                            // allow remove if visible (and then not FACTION_FLAG_INVISIBLE_FORCED or FACTION_FLAG_HIDDEN)
                            if (Convert.ToBoolean(faction.Flags & FactionFlags.Visible))
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
                    var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_REPUTATION_BY_FACTION);
                    stmt.AddValue(0, _player.GetGUID().GetCounter());
                    stmt.AddValue(1, factionState.Id);
                    trans.Append(stmt);

                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_REPUTATION_BY_FACTION);
                    stmt.AddValue(0, _player.GetGUID().GetCounter());
                    stmt.AddValue(1, factionState.Id);
                    stmt.AddValue(2, factionState.Standing);
                    stmt.AddValue(3, factionState.Flags);
                    trans.Append(stmt);

                    factionState.needSave = false;
                }
            }
        }

        private void UpdateRankCounters(ReputationRank old_rank, ReputationRank new_rank)
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

            var raceMask = (ulong)SharedConst.GetMaskForRace(race);
            var classMask = (1u << ((int)playerClass - 1));

            for (var i = 0; i < 4; i++)
            {
                if ((factionEntry.ReputationClassMask[i] == 0 || factionEntry.ReputationClassMask[i].HasAnyFlag((short)classMask))
                    && (factionEntry.ReputationRaceMask[i] == 0 || factionEntry.ReputationRaceMask[i].HasAnyFlag((uint)raceMask)))
                    return factionEntry.ReputationBase[i];
            }

            return 0;
        }

        #region Fields

        private Player _player;
        private byte _visibleFactionCount;
        private byte _honoredFactionCount;
        private byte _reveredFactionCount;
        private byte _exaltedFactionCount;
        private bool _sendFactionIncreased; //! Play visual effect on next SMSG_SET_FACTION_STANDING sent
        #endregion

        public static int[] PointsInRank = { 36000, 3000, 3000, 3000, 6000, 12000, 21000, 1000 };
        public static CypherStrings[] ReputationRankStrIndex =
        {
            CypherStrings.RepHated, CypherStrings.RepHostile, CypherStrings.RepUnfriendly, CypherStrings.RepNeutral,
            CypherStrings.RepFriendly, CypherStrings.RepHonored, CypherStrings.RepRevered, CypherStrings.RepExalted
        };

        private const int Reputation_Cap = 42999;
        private const int Reputation_Bottom = -42000;
        private SortedDictionary<uint, FactionState> _factions = new SortedDictionary<uint, FactionState>();
        private Dictionary<uint, ReputationRank> _forcedReactions = new Dictionary<uint, ReputationRank>();


    }
    public class FactionState
    {
        public uint Id;
        public uint ReputationListID;
        public int Standing;
        public FactionFlags Flags;
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
