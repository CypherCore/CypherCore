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
using Game.DataStorage;
using Game.Entities;
using Game.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using Game.BattleGrounds.Zones;

namespace Game.BattleGrounds
{
    public class BattlegroundManager : Singleton<BattlegroundManager>
    {
        BattlegroundManager()
        {
            m_NextRatedArenaUpdate = WorldConfig.GetUIntValue(WorldCfg.ArenaRatedUpdateTimer);
        }

        public void DeleteAllBattlegrounds()
        {
            foreach (var data in bgDataStore.Values.ToList())
                while(data.m_Battlegrounds.Empty())
                    data.m_Battlegrounds.First().Value.Dispose();

            bgDataStore.Clear();

            foreach (var bg in m_BGFreeSlotQueue.Values.ToList())
                bg.Dispose();

            m_BGFreeSlotQueue.Clear();
        }

        public void Update(uint diff)
        {
            m_UpdateTimer += diff;
            if (m_UpdateTimer > 1000)
            {
                foreach (var data in bgDataStore.Values)
                {
                    var bgs = data.m_Battlegrounds;

                    // first one is template and should not be deleted
                    foreach (var pair in bgs.ToList())
                    {
                        Battleground bg = pair.Value;
                        bg.Update(m_UpdateTimer);

                        if (bg.ToBeDeleted())
                        {
                            bgs.Remove(pair.Key);
                            var clients = data.m_ClientBattlegroundIds[(int)bg.GetBracketId()];
                            if (!clients.Empty())
                                clients.Remove(bg.GetClientInstanceID());

                            bg.Dispose();
                        }
                    }
                }

                m_UpdateTimer = 0;
            }
            // update events timer
            foreach (var pair in m_BattlegroundQueues)
                pair.Value.UpdateEvents(diff);

            // update scheduled queues
            if (!m_QueueUpdateScheduler.Empty())
            {
                List<ScheduledQueueUpdate> scheduled = new List<ScheduledQueueUpdate>();
                Extensions.Swap(ref scheduled, ref m_QueueUpdateScheduler);

                for (byte i = 0; i < scheduled.Count; i++)
                {
                    uint arenaMMRating = scheduled[i].ArenaMatchmakerRating;
                    BattlegroundQueueTypeId bgQueueTypeId = scheduled[i].QueueId;
                    BattlegroundBracketId bracket_id = scheduled[i].BracketId;
                    GetBattlegroundQueue(bgQueueTypeId).BattlegroundQueueUpdate(diff, bracket_id, arenaMMRating);
                }
            }

            // if rating difference counts, maybe force-update queues
            if (WorldConfig.GetIntValue(WorldCfg.ArenaMaxRatingDifference) != 0 && WorldConfig.GetIntValue(WorldCfg.ArenaRatedUpdateTimer) != 0)
            {
                // it's time to force update
                if (m_NextRatedArenaUpdate < diff)
                {
                    // forced update for rated arenas (scan all, but skipped non rated)
                    Log.outDebug(LogFilter.Arena, "BattlegroundMgr: UPDATING ARENA QUEUES");
                    foreach (ArenaTypes teamSize in new[] { ArenaTypes.Team2v2, ArenaTypes.Team3v3, ArenaTypes.Team5v5 })
                    {
                        BattlegroundQueueTypeId ratedArenaQueueId = BGQueueTypeId((ushort)BattlegroundTypeId.AA, BattlegroundQueueIdType.Arena, true, teamSize);
                        for (var bracket = BattlegroundBracketId.First; bracket < BattlegroundBracketId.Max; ++bracket)
                            GetBattlegroundQueue(ratedArenaQueueId).BattlegroundQueueUpdate(diff, bracket, 0);
                    }

                    m_NextRatedArenaUpdate = WorldConfig.GetUIntValue(WorldCfg.ArenaRatedUpdateTimer);
                }
                else
                    m_NextRatedArenaUpdate -= diff;
            }
        }

        void BuildBattlegroundStatusHeader(BattlefieldStatusHeader header, Battleground bg, Player player, uint ticketId, uint joinTime, BattlegroundQueueTypeId queueId, ArenaTypes arenaType)
        {
            header.Ticket = new RideTicket();
            header.Ticket.RequesterGuid = player.GetGUID();
            header.Ticket.Id = ticketId;
            header.Ticket.Type = RideType.Battlegrounds;
            header.Ticket.Time = (int)joinTime;
            header.QueueID.Add(queueId.GetPacked());
            header.RangeMin = (byte)bg.GetMinLevel();
            header.RangeMax = (byte)bg.GetMaxLevel();
            header.TeamSize = (byte)(bg.IsArena() ? arenaType : 0);
            header.InstanceID = bg.GetClientInstanceID();
            header.RegisteredMatch = bg.IsRated();
            header.TournamentRules = false;
        }

        public void BuildBattlegroundStatusNone(out BattlefieldStatusNone battlefieldStatus, Player player, uint ticketId, uint joinTime)
        {
            battlefieldStatus = new BattlefieldStatusNone();
            battlefieldStatus.Ticket.RequesterGuid = player.GetGUID();
            battlefieldStatus.Ticket.Id = ticketId;
            battlefieldStatus.Ticket.Type = RideType.Battlegrounds;
            battlefieldStatus.Ticket.Time = (int)joinTime;
        }

        public void BuildBattlegroundStatusNeedConfirmation(out BattlefieldStatusNeedConfirmation battlefieldStatus, Battleground bg, Player player, uint ticketId, uint joinTime, uint timeout, ArenaTypes arenaType)
        {
            battlefieldStatus = new BattlefieldStatusNeedConfirmation();
            BuildBattlegroundStatusHeader(battlefieldStatus.Hdr, bg, player, ticketId, joinTime, bg.GetQueueId(), arenaType);
            battlefieldStatus.Mapid = bg.GetMapId();
            battlefieldStatus.Timeout = timeout;
            battlefieldStatus.Role = 0;
        }

        public void BuildBattlegroundStatusActive(out BattlefieldStatusActive battlefieldStatus, Battleground bg, Player player, uint ticketId, uint joinTime, ArenaTypes arenaType)
        {
            battlefieldStatus = new BattlefieldStatusActive();
            BuildBattlegroundStatusHeader(battlefieldStatus.Hdr, bg, player, ticketId, joinTime, bg.GetQueueId(), arenaType);
            battlefieldStatus.ShutdownTimer = bg.GetRemainingTime();
            battlefieldStatus.ArenaFaction = (byte)(player.GetBGTeam() == Team.Horde ? TeamId.Horde : TeamId.Alliance);
            battlefieldStatus.LeftEarly = false;
            battlefieldStatus.StartTimer = bg.GetElapsedTime();
            battlefieldStatus.Mapid = bg.GetMapId();
        }

        public void BuildBattlegroundStatusQueued(out BattlefieldStatusQueued battlefieldStatus, Battleground bg, Player player, uint ticketId, uint joinTime, BattlegroundQueueTypeId queueId, uint avgWaitTime, ArenaTypes arenaType, bool asGroup)
        {
            battlefieldStatus = new BattlefieldStatusQueued();
            BuildBattlegroundStatusHeader(battlefieldStatus.Hdr, bg, player, ticketId, joinTime, queueId, arenaType);
            battlefieldStatus.AverageWaitTime = avgWaitTime;
            battlefieldStatus.AsGroup = asGroup;
            battlefieldStatus.SuspendedQueue = false;
            battlefieldStatus.EligibleForMatchmaking = true;
            battlefieldStatus.WaitTime = Time.GetMSTimeDiffToNow(joinTime);
        }

        public void BuildBattlegroundStatusFailed(out BattlefieldStatusFailed battlefieldStatus, Battleground bg, Player pPlayer, uint ticketId, GroupJoinBattlegroundResult result, ObjectGuid errorGuid = default)
        {
            battlefieldStatus = new BattlefieldStatusFailed();
            battlefieldStatus.Ticket.RequesterGuid = pPlayer.GetGUID();
            battlefieldStatus.Ticket.Id = ticketId;
            battlefieldStatus.Ticket.Type = RideType.Battlegrounds;
            battlefieldStatus.Ticket.Time = (int)pPlayer.GetBattlegroundQueueJoinTime(bg.GetQueueId());
            battlefieldStatus.QueueID = bg.GetQueueId().GetPacked();
            battlefieldStatus.Reason = (int)result;
            if (!errorGuid.IsEmpty() && (result == GroupJoinBattlegroundResult.NotInBattleground || result == GroupJoinBattlegroundResult.JoinTimedOut))
                battlefieldStatus.ClientID = errorGuid;
        }

        public Battleground GetBattleground(uint instanceId, BattlegroundTypeId bgTypeId)
        {
            if (instanceId == 0)
                return null;

            if (bgTypeId != BattlegroundTypeId.None || bgTypeId == BattlegroundTypeId.RB || bgTypeId == BattlegroundTypeId.RandomEpic)
            {
                var data = bgDataStore.LookupByKey(bgTypeId);
                return data.m_Battlegrounds.LookupByKey(instanceId);
            }

            foreach (var it in bgDataStore)
            {
                var bgs = it.Value.m_Battlegrounds;
                var bg = bgs.LookupByKey(instanceId);
                if (bg)
                    return bg;
            }

            return null;
        }

        public Battleground GetBattlegroundTemplate(BattlegroundTypeId bgTypeId)
        {
            if (bgDataStore.ContainsKey(bgTypeId))
                return bgDataStore[bgTypeId].Template;

            return null;
        }

        uint CreateClientVisibleInstanceId(BattlegroundTypeId bgTypeId, BattlegroundBracketId bracket_id)
        {
            if (IsArenaType(bgTypeId))
                return 0;                                           //arenas don't have client-instanceids

            // we create here an instanceid, which is just for
            // displaying this to the client and without any other use..
            // the client-instanceIds are unique for each Battleground-type
            // the instance-id just needs to be as low as possible, beginning with 1
            // the following works, because std.set is default ordered with "<"
            // the optimalization would be to use as bitmask std.vector<uint32> - but that would only make code unreadable

            var clientIds = bgDataStore[bgTypeId].m_ClientBattlegroundIds[(int)bracket_id];
            uint lastId = 0;
            foreach (var id in clientIds)
            {
                if (++lastId != id)                             //if there is a gap between the ids, we will break..
                    break;
                lastId = id;
            }

            clientIds.Add(++lastId);
            return lastId;
        }

        // create a new Battleground that will really be used to play
        public Battleground CreateNewBattleground(BattlegroundQueueTypeId queueId, PvpDifficultyRecord bracketEntry)
        {
            BattlegroundTypeId bgTypeId = GetRandomBG((BattlegroundTypeId)queueId.BattlemasterListId);

            // get the template BG
            Battleground bg_template = GetBattlegroundTemplate(bgTypeId);
            if (bg_template == null)
            {
                Log.outError(LogFilter.Battleground, "Battleground: CreateNewBattleground - bg template not found for {0}", bgTypeId);
                return null;
            }

            if (bgTypeId == BattlegroundTypeId.RB || bgTypeId == BattlegroundTypeId.AA || bgTypeId == BattlegroundTypeId.RandomEpic)
                return null;

            // create a copy of the BG template
            Battleground bg = bg_template.GetCopy();

            bool isRandom = bgTypeId != (BattlegroundTypeId)queueId.BattlemasterListId && !bg.IsArena();

            bg.SetQueueId(queueId);
            bg.SetBracket(bracketEntry);
            bg.SetInstanceID(Global.MapMgr.GenerateInstanceId());
            bg.SetClientInstanceID(CreateClientVisibleInstanceId((BattlegroundTypeId)queueId.BattlemasterListId, bracketEntry.GetBracketId()));
            bg.Reset();                     // reset the new bg (set status to status_wait_queue from status_none)
            bg.SetStatus(BattlegroundStatus.WaitJoin); // start the joining of the bg
            bg.SetArenaType((ArenaTypes)queueId.TeamSize);
            bg.SetRandomTypeID(bgTypeId);
            bg.SetRated(queueId.Rated);
            bg.SetRandom(isRandom);

            return bg;
        }

        // used to create the BG templates
        bool CreateBattleground(BattlegroundTemplate bgTemplate)
        {
            Battleground bg = GetBattlegroundTemplate(bgTemplate.Id);
            if (!bg)
            {
                // Create the BG
                switch (bgTemplate.Id)
                {
                    //case BattlegroundTypeId.AV:
                    // bg = new BattlegroundAV(bgTemplate);
                    //break;
                    case BattlegroundTypeId.WS:
                        bg = new BgWarsongGluch(bgTemplate);
                        break;
                    case BattlegroundTypeId.AB:
                        bg = new BgArathiBasin(bgTemplate);
                        break;                        
                    case BattlegroundTypeId.NA:
                        bg = new NagrandArena(bgTemplate);
                        break;
                    case BattlegroundTypeId.BE:
                        bg = new BladesEdgeArena(bgTemplate);
                        break;
                    case BattlegroundTypeId.EY:
                        bg = new BgEyeofStorm(bgTemplate);
                        break;
                    case BattlegroundTypeId.RL:
                        bg = new RuinsofLordaeronArena(bgTemplate);
                        break;
                    case BattlegroundTypeId.SA:
                        bg = new BgStrandOfAncients(bgTemplate);
                        break;
                    case BattlegroundTypeId.DS:
                        bg = new DalaranSewersArena(bgTemplate);
                        break;                        
                    case BattlegroundTypeId.RV:
                        bg = new RingofValorArena(bgTemplate);
                        break;
                    //case BattlegroundTypeId.IC:
                    //bg = new BattlegroundIC(bgTemplate);
                    //break;
                    case BattlegroundTypeId.AA:
                        bg = new Battleground(bgTemplate);
                        break;
                    case BattlegroundTypeId.RB:
                        bg = new Battleground(bgTemplate);
                        bg.SetRandom(true);
                        break;
                    /*
                case BattlegroundTypeId.TP:
                    bg = new BattlegroundTP(bgTemplate);
                    break;
                case BattlegroundTypeId.BFG:
                    bg = new BattlegroundBFG(bgTemplate);
                    break;
                    */
                    case BattlegroundTypeId.RandomEpic:
                        bg = new Battleground(bgTemplate);
                        bg.SetRandom(true);
                        break;
                    default:
                        return false;
                }
            }

            if (!bgDataStore.ContainsKey(bg.GetTypeID()))
                bgDataStore[bg.GetTypeID()] = new BattlegroundData();

            bgDataStore[bg.GetTypeID()].Template = bg;

            return true;
        }

        public void LoadBattlegroundTemplates()
        {
            uint oldMSTime = Time.GetMSTime();

            //                                         0   1                 2              3             4       5
            SQLResult result = DB.World.Query("SELECT ID, AllianceStartLoc, HordeStartLoc, StartMaxDist, Weight, ScriptName FROM battleground_template");
            if (result.IsEmpty())
            {
                Log.outError(LogFilter.ServerLoading, "Loaded 0 Battlegrounds. DB table `Battleground_template` is empty.");
                return;
            }

            uint count = 0;
            do
            {
                BattlegroundTypeId bgTypeId = (BattlegroundTypeId)result.Read<uint>(0);
                if (Global.DisableMgr.IsDisabledFor(DisableType.Battleground, (uint)bgTypeId, null))
                    continue;

                // can be overwrite by values from DB
                BattlemasterListRecord bl = CliDB.BattlemasterListStorage.LookupByKey(bgTypeId);
                if (bl == null)
                {
                    Log.outError(LogFilter.Battleground, "Battleground ID {0} not found in BattlemasterList.dbc. Battleground not created.", bgTypeId);
                    continue;
                }

                BattlegroundTemplate bgTemplate = new BattlegroundTemplate();
                bgTemplate.Id = bgTypeId;
                float dist = result.Read<float>(3);
                bgTemplate.MaxStartDistSq = dist * dist;
                bgTemplate.Weight = result.Read<byte>(4);

                bgTemplate.ScriptId = Global.ObjectMgr.GetScriptId(result.Read<string>(5));
                bgTemplate.BattlemasterEntry = bl;

                if (bgTemplate.Id != BattlegroundTypeId.AA && bgTemplate.Id != BattlegroundTypeId.RB && bgTemplate.Id != BattlegroundTypeId.RandomEpic)
                {
                    uint startId = result.Read<uint>(1);
                    WorldSafeLocsEntry start = Global.ObjectMgr.GetWorldSafeLoc(startId);
                    if (start != null)
                        bgTemplate.StartLocation[TeamId.Alliance] = start;
                    else if (bgTemplate.StartLocation[TeamId.Alliance] != null) // reload case
                        Log.outError(LogFilter.Sql, $"Table `battleground_template` for id {bgTemplate.Id} contains a non-existing WorldSafeLocs.dbc id {startId} in field `AllianceStartLoc`. Ignoring.");
                    else
                    {
                        Log.outError(LogFilter.Sql, $"Table `Battleground_template` for Id {bgTemplate.Id} has a non-existed WorldSafeLocs.dbc id {startId} in field `AllianceStartLoc`. BG not created.");
                        continue;
                    }

                    startId = result.Read<uint>(2);
                    start = Global.ObjectMgr.GetWorldSafeLoc(startId);
                    if (start != null)
                        bgTemplate.StartLocation[TeamId.Horde] = start;
                    else if (bgTemplate.StartLocation[TeamId.Horde] != null) // reload case
                        Log.outError(LogFilter.Sql, $"Table `battleground_template` for id {bgTemplate.Id} contains a non-existing WorldSafeLocs.dbc id {startId} in field `HordeStartLoc`. Ignoring.");
                    else
                    {
                        Log.outError(LogFilter.Sql, $"Table `Battleground_template` for Id {bgTemplate.Id} has a non-existed WorldSafeLocs.dbc id {startId} in field `HordeStartLoc`. BG not created.");
                        continue;
                    }
                }

                if (!CreateBattleground(bgTemplate))
                {
                    Log.outError(LogFilter.Battleground, $"Could not create battleground template class ({bgTemplate.Id})!");
                    continue;
                }

                _battlegroundTemplates[bgTypeId] = bgTemplate;

                if (bgTemplate.BattlemasterEntry.MapId[1] == -1) // in this case we have only one mapId
                    _battlegroundMapTemplates[(uint)bgTemplate.BattlemasterEntry.MapId[0]] = _battlegroundTemplates[bgTypeId];

                ++count;
            }
            while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} Battlegrounds in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void SendBattlegroundList(Player player, ObjectGuid guid, BattlegroundTypeId bgTypeId)
        {
            BattlegroundTemplate bgTemplate = GetBattlegroundTemplateByTypeId(bgTypeId);
            if (bgTemplate == null)
                return;

            BattlefieldList battlefieldList = new BattlefieldList();
            battlefieldList.BattlemasterGuid = guid;
            battlefieldList.BattlemasterListID = (int)bgTypeId;
            battlefieldList.MinLevel = bgTemplate.GetMinLevel();
            battlefieldList.MaxLevel = bgTemplate.GetMaxLevel();
            battlefieldList.PvpAnywhere = guid.IsEmpty();
            battlefieldList.HasRandomWinToday = player.GetRandomWinner();
            player.SendPacket(battlefieldList);
        }

        public void SendToBattleground(Player player, uint instanceId, BattlegroundTypeId bgTypeId)
        {
            Battleground bg = GetBattleground(instanceId, bgTypeId);
            if (bg)
            {
                uint mapid = bg.GetMapId();
                Team team = player.GetBGTeam();

                WorldSafeLocsEntry pos = bg.GetTeamStartPosition(Battleground.GetTeamIndexByTeamId(team));
                Log.outDebug(LogFilter.Battleground, $"BattlegroundMgr.SendToBattleground: Sending {player.GetName()} to map {mapid}, {pos.Loc} (bgType {bgTypeId})");
                player.TeleportTo(pos.Loc);
            }
            else
                Log.outError(LogFilter.Battleground, $"BattlegroundMgr.SendToBattleground: Instance {instanceId} (bgType {bgTypeId}) not found while trying to teleport player {player.GetName()}");
        }

        public void SendAreaSpiritHealerQuery(Player player, Battleground bg, ObjectGuid guid)
        {
            uint time = 30000 - bg.GetLastResurrectTime();      // resurrect every 30 seconds
            if (time == 0xFFFFFFFF)
                time = 0;

            AreaSpiritHealerTime areaSpiritHealerTime = new AreaSpiritHealerTime();
            areaSpiritHealerTime.HealerGuid = guid;
            areaSpiritHealerTime.TimeLeft = time;

            player.SendPacket(areaSpiritHealerTime);
        }

        bool IsArenaType(BattlegroundTypeId bgTypeId)
        {
            return bgTypeId == BattlegroundTypeId.AA || bgTypeId == BattlegroundTypeId.BE || bgTypeId == BattlegroundTypeId.NA
                || bgTypeId == BattlegroundTypeId.DS || bgTypeId == BattlegroundTypeId.RV || bgTypeId == BattlegroundTypeId.RL;
        }

        public BattlegroundQueueTypeId BGQueueTypeId(ushort battlemasterListId, BattlegroundQueueIdType type, bool rated, ArenaTypes teamSize)
        {
            return new BattlegroundQueueTypeId(battlemasterListId, (byte)type, rated, (byte)teamSize);
        }

        public void ToggleTesting()
        {
            m_Testing = !m_Testing;
            Global.WorldMgr.SendWorldText(m_Testing ? CypherStrings.DebugBgOn : CypherStrings.DebugBgOff);
        }

        public void ToggleArenaTesting()
        {
            m_ArenaTesting = !m_ArenaTesting;
            Global.WorldMgr.SendWorldText(m_ArenaTesting ? CypherStrings.DebugArenaOn : CypherStrings.DebugArenaOff);
        }

        public void SetHolidayWeekends(uint mask)
        {
            // The current code supports battlegrounds up to BattlegroundTypeId(31)
            for (var bgtype = 1; bgtype < (int)BattlegroundTypeId.Max && bgtype < 32; ++bgtype)
            {
                Battleground bg = GetBattlegroundTemplate((BattlegroundTypeId)bgtype);
                if (bg)
                    bg.SetHoliday(Convert.ToBoolean(mask & (1 << bgtype)));
            }
        }

        public bool IsValidQueueId(BattlegroundQueueTypeId bgQueueTypeId)
        {
            BattlemasterListRecord battlemasterList = CliDB.BattlemasterListStorage.LookupByKey(bgQueueTypeId.BattlemasterListId);
            if (battlemasterList == null)
                return false;

            switch ((BattlegroundQueueIdType)bgQueueTypeId.BgType)
            {
                case BattlegroundQueueIdType.Battleground:
                    if (battlemasterList.InstanceType != (int)MapTypes.Battleground)
                        return false;
                    if (bgQueueTypeId.TeamSize != 0)
                        return false;
                    break;
                case BattlegroundQueueIdType.Arena:
                    if (battlemasterList.InstanceType != (int)MapTypes.Arena)
                        return false;
                    if (!bgQueueTypeId.Rated)
                        return false;
                    if (bgQueueTypeId.TeamSize == 0)
                        return false;
                    break;
                case BattlegroundQueueIdType.Wargame:
                    if (bgQueueTypeId.Rated)
                        return false;
                    break;
                case BattlegroundQueueIdType.ArenaSkirmish:
                    if (battlemasterList.InstanceType != (int)MapTypes.Arena)
                        return false;
                    if (bgQueueTypeId.Rated)
                        return false;
                    if (bgQueueTypeId.TeamSize != 0)
                        return false;
                    break;
                default:
                    return false;
            }

            return true;
        }

        public void ScheduleQueueUpdate(uint arenaMatchmakerRating, BattlegroundQueueTypeId bgQueueTypeId, BattlegroundBracketId bracket_id)
        {
            //we will use only 1 number created of bgTypeId and bracket_id
            ScheduledQueueUpdate scheduleId = new ScheduledQueueUpdate(arenaMatchmakerRating, bgQueueTypeId, bracket_id);
            if (!m_QueueUpdateScheduler.Contains(scheduleId))
                m_QueueUpdateScheduler.Add(scheduleId);
        }

        public uint GetMaxRatingDifference()
        {
            // this is for stupid people who can't use brain and set max rating difference to 0
            uint diff = WorldConfig.GetUIntValue(WorldCfg.ArenaMaxRatingDifference);
            if (diff == 0)
                diff = 5000;
            return diff;
        }

        public uint GetRatingDiscardTimer()
        {
            return WorldConfig.GetUIntValue(WorldCfg.ArenaRatingDiscardTimer);
        }

        public uint GetPrematureFinishTime()
        {
            return WorldConfig.GetUIntValue(WorldCfg.BattlegroundPrematureFinishTimer);
        }

        public void LoadBattleMastersEntry()
        {
            uint oldMSTime = Time.GetMSTime();

            mBattleMastersMap.Clear();                                  // need for reload case

            SQLResult result = DB.World.Query("SELECT entry, bg_template FROM battlemaster_entry");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 battlemaster entries. DB table `battlemaster_entry` is empty!");
                return;
            }

            uint count = 0;

            do
            {
                uint entry = result.Read<uint>(0);
                CreatureTemplate cInfo = Global.ObjectMgr.GetCreatureTemplate(entry);
                if (cInfo != null)
                {
                    if (!cInfo.Npcflag.HasAnyFlag((uint)NPCFlags.BattleMaster))
                        Log.outError(LogFilter.Sql, "Creature (Entry: {0}) listed in `battlemaster_entry` is not a battlemaster.", entry);
                }
                else
                {
                    Log.outError(LogFilter.Sql, "Creature (Entry: {0}) listed in `battlemaster_entry` does not exist.", entry);
                    continue;
                }

                uint bgTypeId = result.Read<uint>(1);
                if (!CliDB.BattlemasterListStorage.ContainsKey(bgTypeId))
                {
                    Log.outError(LogFilter.Sql, "Table `battlemaster_entry` contain entry {0} for not existed Battleground type {1}, ignored.", entry, bgTypeId);
                    continue;
                }

                ++count;
                mBattleMastersMap[entry] = (BattlegroundTypeId)bgTypeId;
            }
            while (result.NextRow());

            CheckBattleMasters();

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} battlemaster entries in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        void CheckBattleMasters()
        {
            var templates = Global.ObjectMgr.GetCreatureTemplates();
            foreach (var creature in templates)
            {
                if (creature.Value.Npcflag.HasAnyFlag((uint)NPCFlags.BattleMaster) && !mBattleMastersMap.ContainsKey(creature.Value.Entry))
                {
                    Log.outError(LogFilter.Sql, "CreatureTemplate (Entry: {0}) has UNIT_NPC_FLAG_BATTLEMASTER but no data in `battlemaster_entry` table. Removing flag!", creature.Value.Entry);
                    templates[creature.Key].Npcflag &= ~(uint)NPCFlags.BattleMaster;
                }
            }
        }

        HolidayIds BGTypeToWeekendHolidayId(BattlegroundTypeId bgTypeId)
        {
            switch (bgTypeId)
            {
                case BattlegroundTypeId.AV:
                    return HolidayIds.CallToArmsAv;
                case BattlegroundTypeId.EY:
                    return HolidayIds.CallToArmsEy;
                case BattlegroundTypeId.WS:
                    return HolidayIds.CallToArmsWs;
                case BattlegroundTypeId.SA:
                    return HolidayIds.CallToArmsSa;
                case BattlegroundTypeId.AB:
                    return HolidayIds.CallToArmsAb;
                case BattlegroundTypeId.IC:
                    return HolidayIds.CallToArmsIc;
                case BattlegroundTypeId.TP:
                    return HolidayIds.CallToArmsTp;
                case BattlegroundTypeId.BFG:
                    return HolidayIds.CallToArmsBfg;
                default:
                    return HolidayIds.None;
            }
        }

        public BattlegroundTypeId WeekendHolidayIdToBGType(HolidayIds holiday)
        {
            switch (holiday)
            {
                case HolidayIds.CallToArmsAv:
                    return BattlegroundTypeId.AV;
                case HolidayIds.CallToArmsEy:
                    return BattlegroundTypeId.EY;
                case HolidayIds.CallToArmsWs:
                    return BattlegroundTypeId.WS;
                case HolidayIds.CallToArmsSa:
                    return BattlegroundTypeId.SA;
                case HolidayIds.CallToArmsAb:
                    return BattlegroundTypeId.AB;
                case HolidayIds.CallToArmsIc:
                    return BattlegroundTypeId.IC;
                case HolidayIds.CallToArmsTp:
                    return BattlegroundTypeId.TP;
                case HolidayIds.CallToArmsBfg:
                    return BattlegroundTypeId.BFG;
                default:
                    return BattlegroundTypeId.None;
            }
        }

        public bool IsBGWeekend(BattlegroundTypeId bgTypeId)
        {
            return Global.GameEventMgr.IsHolidayActive(BGTypeToWeekendHolidayId(bgTypeId));
        }

        BattlegroundTypeId GetRandomBG(BattlegroundTypeId bgTypeId)
        {
            BattlegroundTemplate bgTemplate = GetBattlegroundTemplateByTypeId(bgTypeId);
            if (bgTemplate != null)
            {
                Dictionary<BattlegroundTypeId, float> selectionWeights = new Dictionary<BattlegroundTypeId, float>();

                foreach (var mapId in bgTemplate.BattlemasterEntry.MapId)
                {
                    if (mapId == -1)
                        break;

                    BattlegroundTemplate bg = GetBattlegroundTemplateByMapId((uint)mapId);
                    if (bg != null)
                    {
                        selectionWeights.Add(bg.Id, bg.Weight);
                    }
                }

                return selectionWeights.SelectRandomElementByWeight(i => i.Value).Key;
            }

            return BattlegroundTypeId.None;
        }

        public List<Battleground> GetBGFreeSlotQueueStore(BattlegroundQueueTypeId bgTypeId)
        {
            return m_BGFreeSlotQueue[bgTypeId];
        }

        public void AddToBGFreeSlotQueue(BattlegroundQueueTypeId bgTypeId, Battleground bg)
        {
            m_BGFreeSlotQueue.Add(bgTypeId, bg);
        }

        public void RemoveFromBGFreeSlotQueue(BattlegroundQueueTypeId bgTypeId, uint instanceId)
        {
            var queues = m_BGFreeSlotQueue[bgTypeId];
            foreach (var bg in queues)
            {
                if (bg.GetInstanceID() == instanceId)
                {
                    queues.Remove(bg);
                    return;
                }
            }
        }

        public void AddBattleground(Battleground bg)
        {
            if (bg)
                bgDataStore[bg.GetTypeID()].m_Battlegrounds[bg.GetInstanceID()] = bg;
        }

        public void RemoveBattleground(BattlegroundTypeId bgTypeId, uint instanceId)
        {
            bgDataStore[bgTypeId].m_Battlegrounds.Remove(instanceId);
        }

        public BattlegroundQueue GetBattlegroundQueue(BattlegroundQueueTypeId bgQueueTypeId) 
        {
            if (!m_BattlegroundQueues.ContainsKey(bgQueueTypeId))
                m_BattlegroundQueues[bgQueueTypeId] = new BattlegroundQueue(bgQueueTypeId);

            return m_BattlegroundQueues[bgQueueTypeId];
        }

        public bool IsArenaTesting() { return m_ArenaTesting; }
        public bool IsTesting() { return m_Testing; }

        public BattlegroundTypeId GetBattleMasterBG(uint entry)
        {
            return mBattleMastersMap.LookupByKey(entry);
        }

        BattlegroundTemplate GetBattlegroundTemplateByTypeId(BattlegroundTypeId id)
        {
            return _battlegroundTemplates.LookupByKey(id);
        }

        BattlegroundTemplate GetBattlegroundTemplateByMapId(uint mapId)
        {
            return _battlegroundMapTemplates.LookupByKey(mapId);
        }

        Dictionary<BattlegroundTypeId, BattlegroundData> bgDataStore = new Dictionary<BattlegroundTypeId, BattlegroundData>();
        Dictionary<BattlegroundQueueTypeId, BattlegroundQueue> m_BattlegroundQueues = new Dictionary<BattlegroundQueueTypeId, BattlegroundQueue>();
        MultiMap<BattlegroundQueueTypeId, Battleground> m_BGFreeSlotQueue = new MultiMap<BattlegroundQueueTypeId, Battleground>();
        Dictionary<uint, BattlegroundTypeId> mBattleMastersMap = new Dictionary<uint, BattlegroundTypeId>();
        Dictionary<BattlegroundTypeId, BattlegroundTemplate> _battlegroundTemplates = new Dictionary<BattlegroundTypeId, BattlegroundTemplate>();
        Dictionary<uint, BattlegroundTemplate> _battlegroundMapTemplates = new Dictionary<uint, BattlegroundTemplate>();

        struct ScheduledQueueUpdate
        {
            public ScheduledQueueUpdate(uint arenaMatchmakerRating, BattlegroundQueueTypeId queueId, BattlegroundBracketId bracketId)
            {
                ArenaMatchmakerRating = arenaMatchmakerRating;
                QueueId = queueId;
                BracketId = bracketId;
            }

            public uint ArenaMatchmakerRating;
            public BattlegroundQueueTypeId QueueId;
            public BattlegroundBracketId BracketId;

            public static bool operator ==(ScheduledQueueUpdate right, ScheduledQueueUpdate left)
            {
                return left.ArenaMatchmakerRating == right.ArenaMatchmakerRating && left.QueueId == right.QueueId && left.BracketId == right.BracketId;
            }

            public static bool operator !=(ScheduledQueueUpdate right, ScheduledQueueUpdate left)
            {
                return !(right == left);
            }

            public override bool Equals(object obj)
            {
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return ArenaMatchmakerRating.GetHashCode() ^ QueueId.GetHashCode() ^ BracketId.GetHashCode();
            }
        }
        List<ScheduledQueueUpdate> m_QueueUpdateScheduler = new List<ScheduledQueueUpdate>();
        uint m_NextRatedArenaUpdate;
        uint m_UpdateTimer;
        bool m_ArenaTesting;
        bool m_Testing;
    }

    public class BattlegroundData
    {
        public BattlegroundData()
        {
            for (var i = 0; i < (int)BattlegroundBracketId.Max; ++i)
                m_ClientBattlegroundIds[i] = new List<uint>();
        }

        public Dictionary<uint, Battleground> m_Battlegrounds = new Dictionary<uint, Battleground>();
        public List<uint>[] m_ClientBattlegroundIds = new List<uint>[(int)BattlegroundBracketId.Max];
        public Battleground Template;
    }

    public class BattlegroundTemplate
    {
        public BattlegroundTypeId Id;
        public WorldSafeLocsEntry[] StartLocation = new WorldSafeLocsEntry[SharedConst.BGTeamsCount];
        public float MaxStartDistSq;
        public byte Weight;
        public uint ScriptId;
        public BattlemasterListRecord BattlemasterEntry;

        public bool IsArena() { return BattlemasterEntry.InstanceType == (uint)MapTypes.Arena; }

        public ushort GetMinPlayersPerTeam()
        {
            return BattlemasterEntry.MinPlayers;
        }

        public ushort GetMaxPlayersPerTeam()
        {
            return BattlemasterEntry.MaxPlayers;
        }

        public byte GetMinLevel()
        {
            return BattlemasterEntry.MinLevel;
        }

        public byte GetMaxLevel()
        {
            return BattlemasterEntry.MaxLevel;
        }
    }
}
