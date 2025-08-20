// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.Arenas;
using Game.DataStorage;
using Game.Entities;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

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
                while (!data.m_Battlegrounds.Empty())
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
                List<ScheduledQueueUpdate> scheduled = new();
                Extensions.Swap(ref scheduled, ref m_QueueUpdateScheduler);

                foreach (var scheduledQueueUpdate in scheduled)
                    GetBattlegroundQueue(scheduledQueueUpdate.QueueId).BattlegroundQueueUpdate(diff, scheduledQueueUpdate.BracketId, scheduledQueueUpdate.ArenaMatchmakerRating);
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

        void BuildBattlegroundStatusHeader(BattlefieldStatusHeader header, Player player, uint ticketId, uint joinTime, BattlegroundQueueTypeId queueId)
        {
            header.Ticket = new RideTicket();
            header.Ticket.RequesterGuid = player.GetGUID();
            header.Ticket.Id = ticketId;
            header.Ticket.Type = RideType.Battlegrounds;
            header.Ticket.Time = (int)joinTime;
            header.QueueID.Add(queueId.GetPacked());
            header.RangeMin = 0; // seems to always be 0
            header.RangeMax = SharedConst.DefaultMaxLevel; // alwyas max level of current expansion. Might be limited to account
            header.TeamSize = queueId.TeamSize;
            header.InstanceID = 0; // seems to always be 0
            header.RegisteredMatch = queueId.Rated;
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

        public void BuildBattlegroundStatusNeedConfirmation(out BattlefieldStatusNeedConfirmation battlefieldStatus, Battleground bg, Player player, uint ticketId, uint joinTime, uint timeout, BattlegroundQueueTypeId queueId)
        {
            battlefieldStatus = new BattlefieldStatusNeedConfirmation();
            BuildBattlegroundStatusHeader(battlefieldStatus.Hdr, player, ticketId, joinTime, queueId);
            battlefieldStatus.Mapid = bg.GetMapId();
            battlefieldStatus.Timeout = timeout;
            battlefieldStatus.Role = 0;
        }

        public void BuildBattlegroundStatusActive(out BattlefieldStatusActive battlefieldStatus, Battleground bg, Player player, uint ticketId, uint joinTime, BattlegroundQueueTypeId queueId)
        {
            battlefieldStatus = new BattlefieldStatusActive();
            BuildBattlegroundStatusHeader(battlefieldStatus.Hdr, player, ticketId, joinTime, queueId);
            battlefieldStatus.ShutdownTimer = bg.GetRemainingTime();
            battlefieldStatus.ArenaFaction = (sbyte)(player.GetBGTeam() == Team.Horde ? BattleGroundTeamId.Horde : BattleGroundTeamId.Alliance);
            battlefieldStatus.LeftEarly = false;
            battlefieldStatus.StartTimer = bg.GetElapsedTime();
            battlefieldStatus.Mapid = bg.GetMapId();
        }

        public void BuildBattlegroundStatusQueued(out BattlefieldStatusQueued battlefieldStatus, Player player, uint ticketId, uint joinTime, BattlegroundQueueTypeId queueId, uint avgWaitTime, bool asGroup)
        {
            battlefieldStatus = new BattlefieldStatusQueued();
            BuildBattlegroundStatusHeader(battlefieldStatus.Hdr, player, ticketId, joinTime, queueId);
            battlefieldStatus.AverageWaitTime = avgWaitTime;
            battlefieldStatus.AsGroup = asGroup;
            battlefieldStatus.SuspendedQueue = false;
            battlefieldStatus.EligibleForMatchmaking = true;
            battlefieldStatus.WaitTime = Time.GetMSTimeDiffToNow(joinTime);
        }

        public void BuildBattlegroundStatusFailed(out BattlefieldStatusFailed battlefieldStatus, BattlegroundQueueTypeId queueId, Player pPlayer, uint ticketId, GroupJoinBattlegroundResult result, ObjectGuid errorGuid = default)
        {
            battlefieldStatus = new BattlefieldStatusFailed();
            battlefieldStatus.Ticket.RequesterGuid = pPlayer.GetGUID();
            battlefieldStatus.Ticket.Id = ticketId;
            battlefieldStatus.Ticket.Type = RideType.Battlegrounds;
            battlefieldStatus.Ticket.Time = (int)pPlayer.GetBattlegroundQueueJoinTime(queueId);
            battlefieldStatus.QueueID = queueId.GetPacked();
            battlefieldStatus.Reason = (int)result;
            if (!errorGuid.IsEmpty() && (result == GroupJoinBattlegroundResult.NotInBattleground || result == GroupJoinBattlegroundResult.JoinTimedOut))
                battlefieldStatus.ClientID = errorGuid;
        }

        public Battleground GetBattleground(uint instanceId, BattlegroundTypeId bgTypeId)
        {
            if (instanceId == 0)
                return null;

            if (bgTypeId != BattlegroundTypeId.None || IsRandomBattleground(bgTypeId))
            {
                var data = bgDataStore.LookupByKey(bgTypeId);
                return data.m_Battlegrounds.LookupByKey(instanceId);
            }

            foreach (var it in bgDataStore)
            {
                var bgs = it.Value.m_Battlegrounds;
                var bg = bgs.LookupByKey(instanceId);
                if (bg != null)
                    return bg;
            }

            return null;
        }

        public void LoadBattlegroundScriptTemplate()
        {
            uint oldMSTime = Time.GetMSTime();

            //                                         0      1                   2
            SQLResult result = DB.World.Query("SELECT MapId, BattlemasterListId, ScriptName FROM battleground_scripts");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 battleground scripts. DB table `battleground_scripts` is empty!");
                return;
            }

            uint count = 0;
            do
            {
                uint mapID = result.Read<uint>(0);

                var mapEntry = CliDB.MapStorage.LookupByKey(mapID);
                if (mapEntry == null || !mapEntry.IsBattlegroundOrArena())
                {
                    Log.outError(LogFilter.Sql, $"BattlegroundMgr::LoadBattlegroundScriptTemplate: bad mapid {mapID}! Map doesn't exist or is not a battleground/arena!");
                    continue;
                }

                BattlegroundTypeId bgTypeId = (BattlegroundTypeId)result.Read<uint>(1);
                if (bgTypeId != BattlegroundTypeId.None && !_battlegroundTemplates.ContainsKey(bgTypeId))
                {
                    Log.outError(LogFilter.Sql, $"BattlegroundMgr::LoadBattlegroundScriptTemplate: bad battlemasterlist id {bgTypeId}! Battleground doesn't exist or is not supported in battleground_template!");
                    continue;
                }

                BattlegroundScriptTemplate scriptTemplate = new();
                scriptTemplate.MapId = mapID;
                scriptTemplate.Id = bgTypeId;
                scriptTemplate.ScriptId = Global.ObjectMgr.GetScriptId(result.Read<string>(2));

                _battlegroundScriptTemplates[(mapID, bgTypeId)] = scriptTemplate;

                ++count;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} battleground scripts in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }

        public BattlegroundScriptTemplate FindBattlegroundScriptTemplate(uint mapId, BattlegroundTypeId bgTypeId)
        {
            BattlegroundScriptTemplate scriptTemplate = _battlegroundScriptTemplates.LookupByKey((mapId, bgTypeId));
            if (scriptTemplate != null)
                return scriptTemplate;

            // fall back to 0 for no specific battleground type id
            return _battlegroundScriptTemplates.LookupByKey((mapId, BattlegroundTypeId.None));
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
        public Battleground CreateNewBattleground(BattlegroundQueueTypeId queueId, BattlegroundBracketId bracketId)
        {
            BattlegroundTypeId bgTypeId = GetRandomBG((BattlegroundTypeId)queueId.BattlemasterListId);

            // get the template BG
            BattlegroundTemplate bg_template = GetBattlegroundTemplateByTypeId(bgTypeId);
            if (bg_template == null)
            {
                Log.outError(LogFilter.Battleground, $"Battleground: CreateNewBattleground - bg template not found for {bgTypeId}");
                return null;
            }

            if (bgTypeId == BattlegroundTypeId.RB || bgTypeId == BattlegroundTypeId.AA || bgTypeId == BattlegroundTypeId.RandomEpic)
                return null;

            PvpDifficultyRecord bracketEntry = Global.DB2Mgr.GetBattlegroundBracketById((uint)bg_template.MapIDs[0], bracketId);
            if (bracketEntry == null)
            {
                Log.outError(LogFilter.Battleground, $"Battleground: CreateNewBattleground: bg bracket entry not found for map {bg_template.MapIDs[0]} bracket id {bracketId}");
                return null;
            }

            Battleground bg = null;
            if (bg_template.IsArena())
                bg = new Arena(bg_template);
            else
                bg = new Battleground(bg_template);

            bg.SetBracket(bracketEntry);
            bg.SetInstanceID(Global.MapMgr.GenerateInstanceId());
            bg.SetClientInstanceID(CreateClientVisibleInstanceId((BattlegroundTypeId)queueId.BattlemasterListId, bracketEntry.GetBracketId()));
            // reset the new bg (set status to status_wait_queue from status_none)
            // this shouldn't be needed anymore as a new Battleground instance is created each time. But some bg sub classes still depend on it.
            bg.Reset();
            bg.SetStatus(BattlegroundStatus.WaitJoin); // start the joining of the bg
            bg.SetArenaType((ArenaTypes)queueId.TeamSize);
            bg.SetRated(queueId.Rated);

            return bg;
        }

        public void LoadBattlegroundTemplates()
        {
            uint oldMSTime = Time.GetMSTime();

            //                                         0   1                 2              3             4       5
            SQLResult result = DB.World.Query("SELECT ID, AllianceStartLoc, HordeStartLoc, StartMaxDist, Weight, ScriptName FROM battleground_template");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 Battlegrounds. DB table `Battleground_template` is empty.");
                return;
            }

            MultiMap<BattlegroundTypeId, int> mapsByBattleground = new();
            foreach (var (_, battlemasterListXMap) in CliDB.BattlemasterListXMapStorage)
                if (CliDB.BattlemasterListStorage.HasRecord(battlemasterListXMap.BattlemasterListID) && CliDB.MapStorage.HasRecord((uint)battlemasterListXMap.MapID))
                    mapsByBattleground.Add((BattlegroundTypeId)battlemasterListXMap.BattlemasterListID, battlemasterListXMap.MapID);

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

                BattlegroundTemplate bgTemplate = new();
                bgTemplate.Id = bgTypeId;
                float dist = result.Read<float>(3);
                bgTemplate.MaxStartDistSq = dist * dist;
                bgTemplate.Weight = result.Read<byte>(4);

                bgTemplate.ScriptId = Global.ObjectMgr.GetScriptId(result.Read<string>(5));
                bgTemplate.BattlemasterEntry = bl;
                bgTemplate.MapIDs = mapsByBattleground[bgTypeId];

                if (bgTemplate.Id != BattlegroundTypeId.AA && !IsRandomBattleground(bgTemplate.Id))
                {
                    uint startId = result.Read<uint>(1);
                    WorldSafeLocsEntry start = Global.ObjectMgr.GetWorldSafeLoc(startId);
                    if (start != null)
                        bgTemplate.StartLocation[BattleGroundTeamId.Alliance] = start;
                    else if (bgTemplate.StartLocation[BattleGroundTeamId.Alliance] != null) // reload case
                        Log.outError(LogFilter.Sql, $"Table `battleground_template` for id {bgTemplate.Id} contains a non-existing WorldSafeLocs.dbc id {startId} in field `AllianceStartLoc`. Ignoring.");
                    else
                    {
                        Log.outError(LogFilter.Sql, $"Table `Battleground_template` for Id {bgTemplate.Id} has a non-existed WorldSafeLocs.dbc id {startId} in field `AllianceStartLoc`. BG not created.");
                        continue;
                    }

                    startId = result.Read<uint>(2);
                    start = Global.ObjectMgr.GetWorldSafeLoc(startId);
                    if (start != null)
                        bgTemplate.StartLocation[BattleGroundTeamId.Horde] = start;
                    else if (bgTemplate.StartLocation[BattleGroundTeamId.Horde] != null) // reload case
                        Log.outError(LogFilter.Sql, $"Table `battleground_template` for id {bgTemplate.Id} contains a non-existing WorldSafeLocs.dbc id {startId} in field `HordeStartLoc`. Ignoring.");
                    else
                    {
                        Log.outError(LogFilter.Sql, $"Table `Battleground_template` for Id {bgTemplate.Id} has a non-existed WorldSafeLocs.dbc id {startId} in field `HordeStartLoc`. BG not created.");
                        continue;
                    }
                }

                _battlegroundTemplates[bgTypeId] = bgTemplate;

                if (bgTemplate.MapIDs.Count == 1)
                    _battlegroundMapTemplates[(uint)bgTemplate.MapIDs[0]] = _battlegroundTemplates[bgTypeId];

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

            player.PlayerTalkClass.GetInteractionData().StartInteraction(guid, PlayerInteractionType.BattleMaster);

            BattlefieldList battlefieldList = new();
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
            if (bg != null)
            {
                uint mapid = bg.GetMapId();
                Team team = player.GetBGTeam();

                WorldSafeLocsEntry pos = bg.GetTeamStartPosition(Battleground.GetTeamIndexByTeamId(team));
                Log.outDebug(LogFilter.Battleground, $"BattlegroundMgr.SendToBattleground: Sending {player.GetName()} to map {mapid}, {pos.Loc} (bgType {bgTypeId})");
                player.TeleportTo(new Entities.TeleportLocation() { Location = pos.Loc, TransportGuid = pos.TransportSpawnId.HasValue ? ObjectGuid.Create(HighGuid.Transport, pos.TransportSpawnId.Value) : ObjectGuid.Empty });
            }
            else
                Log.outError(LogFilter.Battleground, $"BattlegroundMgr.SendToBattleground: Instance {instanceId} (bgType {bgTypeId}) not found while trying to teleport player {player.GetName()}");
        }

        bool IsArenaType(BattlegroundTypeId bgTypeId)
        {
            return bgTypeId == BattlegroundTypeId.AA || bgTypeId == BattlegroundTypeId.BE || bgTypeId == BattlegroundTypeId.NA
                || bgTypeId == BattlegroundTypeId.DS || bgTypeId == BattlegroundTypeId.RV || bgTypeId == BattlegroundTypeId.RL;
        }

        public bool IsRandomBattleground(BattlegroundTypeId battlemasterListId)
        {
            return battlemasterListId == BattlegroundTypeId.RB || battlemasterListId == BattlegroundTypeId.RandomEpic;
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

        public bool IsValidQueueId(BattlegroundQueueTypeId bgQueueTypeId)
        {
            BattlemasterListRecord battlemasterList = CliDB.BattlemasterListStorage.LookupByKey(bgQueueTypeId.BattlemasterListId);
            if (battlemasterList == null)
                return false;

            switch ((BattlegroundQueueIdType)bgQueueTypeId.BgType)
            {
                case BattlegroundQueueIdType.Battleground:
                    if (battlemasterList.GetPvpType() != BattlemasterType.Battleground)
                        return false;
                    if (bgQueueTypeId.TeamSize != 0)
                        return false;
                    break;
                case BattlegroundQueueIdType.Arena:
                    if (battlemasterList.GetPvpType() != BattlemasterType.Arena)
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
                    if (battlemasterList.GetPvpType() != BattlemasterType.Arena)
                        return false;
                    if (!bgQueueTypeId.Rated)
                        return false;
                    if (bgQueueTypeId.TeamSize != (int)ArenaTypes.Team3v3)
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
            ScheduledQueueUpdate scheduleId = new(arenaMatchmakerRating, bgQueueTypeId, bracket_id);
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
                    return HolidayIds.CallToArmsEs;
                case BattlegroundTypeId.WS:
                    return HolidayIds.CallToArmsWg;
                case BattlegroundTypeId.SA:
                    return HolidayIds.CallToArmsSa;
                case BattlegroundTypeId.AB:
                    return HolidayIds.CallToArmsAb;
                case BattlegroundTypeId.IC:
                    return HolidayIds.CallToArmsIc;
                case BattlegroundTypeId.TP:
                    return HolidayIds.CallToArmsTp;
                case BattlegroundTypeId.BFG:
                    return HolidayIds.CallToArmsBg;
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
                case HolidayIds.CallToArmsEs:
                    return BattlegroundTypeId.EY;
                case HolidayIds.CallToArmsWg:
                    return BattlegroundTypeId.WS;
                case HolidayIds.CallToArmsSa:
                    return BattlegroundTypeId.SA;
                case HolidayIds.CallToArmsAb:
                    return BattlegroundTypeId.AB;
                case HolidayIds.CallToArmsIc:
                    return BattlegroundTypeId.IC;
                case HolidayIds.CallToArmsTp:
                    return BattlegroundTypeId.TP;
                case HolidayIds.CallToArmsBg:
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
                List<BattlegroundTemplate> ids = new();
                foreach (var mapId in bgTemplate.MapIDs)
                {
                    BattlegroundTemplate bg = GetBattlegroundTemplateByMapId((uint)mapId);
                    if (bg != null)
                        ids.Add(bg);
                }

                return ids.SelectRandomElementByWeight(i => i.Weight).Id;
            }

            return BattlegroundTypeId.None;
        }

        public List<Battleground> GetBGFreeSlotQueueStore(uint mapId)
        {
            return m_BGFreeSlotQueue[mapId];
        }

        public void AddToBGFreeSlotQueue(Battleground bg)
        {
            m_BGFreeSlotQueue.Add(bg.GetMapId(), bg);
        }

        public void RemoveFromBGFreeSlotQueue(uint mapId, uint instanceId)
        {
            var queues = m_BGFreeSlotQueue[mapId];
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
            if (bg != null)
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

        public BattlegroundTemplate GetBattlegroundTemplateByTypeId(BattlegroundTypeId id)
        {
            return _battlegroundTemplates.LookupByKey(id);
        }

        BattlegroundTemplate GetBattlegroundTemplateByMapId(uint mapId)
        {
            return _battlegroundMapTemplates.LookupByKey(mapId);
        }

        Dictionary<BattlegroundTypeId, BattlegroundData> bgDataStore = new();
        Dictionary<BattlegroundQueueTypeId, BattlegroundQueue> m_BattlegroundQueues = new();
        MultiMap<uint, Battleground> m_BGFreeSlotQueue = new();
        Dictionary<uint, BattlegroundTypeId> mBattleMastersMap = new();
        Dictionary<BattlegroundTypeId, BattlegroundTemplate> _battlegroundTemplates = new();
        Dictionary<uint, BattlegroundTemplate> _battlegroundMapTemplates = new();
        Dictionary<(uint, BattlegroundTypeId), BattlegroundScriptTemplate> _battlegroundScriptTemplates = new();

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
        List<ScheduledQueueUpdate> m_QueueUpdateScheduler = new();
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

        public Dictionary<uint, Battleground> m_Battlegrounds = new();
        public List<uint>[] m_ClientBattlegroundIds = new List<uint>[(int)BattlegroundBracketId.Max];
        public Battleground Template;
    }

    public class BattlegroundTemplate
    {
        public BattlegroundTypeId Id;
        public WorldSafeLocsEntry[] StartLocation = new WorldSafeLocsEntry[SharedConst.PvpTeamsCount];
        public float MaxStartDistSq;
        public byte Weight;
        public uint ScriptId;
        public BattlemasterListRecord BattlemasterEntry;
        public List<int> MapIDs = new();

        public bool IsArena() { return BattlemasterEntry.GetPvpType() == BattlemasterType.Arena; }

        public ushort GetMinPlayersPerTeam()
        {
            return (ushort)BattlemasterEntry.MinPlayers;
        }

        public ushort GetMaxPlayersPerTeam()
        {
            return (ushort)BattlemasterEntry.MaxPlayers;
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

    public class BattlegroundScriptTemplate
    {
        public uint MapId;
        public BattlegroundTypeId Id;
        public uint ScriptId;
    }
}
