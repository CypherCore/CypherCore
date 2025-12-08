// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.Chat;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Guilds;
using Game.Maps;
using Game.Networking;
using Game.Networking.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Game.BattleGrounds
{
    public class Battleground : ZoneScript, IDisposable
    {
        public Battleground(BattlegroundTemplate battlegroundTemplate)
        {
            _battlegroundTemplate = battlegroundTemplate;
            m_Status = BattlegroundStatus.None;
            _winnerTeamId = PvPTeamId.Neutral;

            StartDelayTimes[BattlegroundConst.EventIdFirst] = BattlegroundStartTimeIntervals.Delay2m;
            StartDelayTimes[BattlegroundConst.EventIdSecond] = BattlegroundStartTimeIntervals.Delay1m;
            StartDelayTimes[BattlegroundConst.EventIdThird] = BattlegroundStartTimeIntervals.Delay30s;
            StartDelayTimes[BattlegroundConst.EventIdFourth] = BattlegroundStartTimeIntervals.None;

            StartMessageIds[BattlegroundConst.EventIdFirst] = BattlegroundBroadcastTexts.StartTwoMinutes;
            StartMessageIds[BattlegroundConst.EventIdSecond] = BattlegroundBroadcastTexts.StartOneMinute;
            StartMessageIds[BattlegroundConst.EventIdThird] = BattlegroundBroadcastTexts.StartHalfMinute;
            StartMessageIds[BattlegroundConst.EventIdFourth] = BattlegroundBroadcastTexts.HasBegun;
        }

        public virtual void Dispose()
        {
            Global.BattlegroundMgr.RemoveBattleground(GetTypeID(), GetInstanceID());

            // unload map
            if (m_Map != null)
            {
                m_Map.UnloadAll(); // unload all objects (they may hold a reference to bg in their ZoneScript pointer)
                m_Map.SetUnload(); // mark for deletion by MapManager

                //unlink to prevent crash, always unlink all pointer reference before destruction
                m_Map.SetBG(null);
                m_Map = null;
            }

            // remove from bg free slot queue
            RemoveFromBGFreeSlotQueue();
        }

        public Battleground GetCopy()
        {
            return (Battleground)MemberwiseClone();
        }

        public void Update(uint diff)
        {
            if (!PreUpdateImpl(diff))
                return;

            if (GetPlayersSize() == 0)
            {
                //BG is empty
                // if there are no players invited, delete BG
                // this will delete arena or bg object, where any player entered
                // [[   but if you use Battleground object again (more battles possible to be played on 1 instance)
                //      then this condition should be removed and code:
                //      if (!GetInvitedCount(Team.Horde) && !GetInvitedCount(Team.Alliance))
                //          this.AddToFreeBGObjectsQueue(); // not yet implemented
                //      should be used instead of current
                // ]]
                // Battleground Template instance cannot be updated, because it would be deleted
                if (GetInvitedCount(Team.Horde) == 0 && GetInvitedCount(Team.Alliance) == 0)
                    m_SetDeleteThis = true;
                return;
            }

            switch (GetStatus())
            {
                case BattlegroundStatus.WaitJoin:
                    if (GetPlayersSize() != 0)
                    {
                        _ProcessJoin(diff);
                        _CheckSafePositions(diff);
                    }
                    break;
                case BattlegroundStatus.InProgress:
                    _ProcessOfflineQueue();
                    _ProcessPlayerPositionBroadcast(diff);
                    // after 47 Time.Minutes without one team losing, the arena closes with no winner and no rating change
                    if (IsArena())
                    {
                        if (GetElapsedTime() >= 47 * Time.Minute * Time.InMilliseconds)
                        {
                            EndBattleground(Team.Other);
                            return;
                        }
                    }
                    else
                    {
                        if (Global.BattlegroundMgr.GetPrematureFinishTime() != 0 && (GetPlayersCountByTeam(Team.Alliance) < GetMinPlayersPerTeam() || GetPlayersCountByTeam(Team.Horde) < GetMinPlayersPerTeam()))
                            _ProcessProgress(diff);
                        else if (m_PrematureCountDown)
                            m_PrematureCountDown = false;
                    }
                    break;
                case BattlegroundStatus.WaitLeave:
                    _ProcessLeave(diff);
                    break;
                default:
                    break;
            }

            // Update start time and reset stats timer
            SetElapsedTime(GetElapsedTime() + diff);
            if (GetStatus() == BattlegroundStatus.WaitJoin)
                m_ResetStatTimer += diff;

            PostUpdateImpl(diff);
        }

        void _CheckSafePositions(uint diff)
        {
            float maxDist = GetStartMaxDist();
            if (maxDist == 0.0f)
                return;

            m_ValidStartPositionTimer += diff;
            if (m_ValidStartPositionTimer >= BattlegroundConst.CheckPlayerPositionInverval)
            {
                m_ValidStartPositionTimer = 0;

                foreach (var guid in GetPlayers().Keys)
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    if (player != null)
                    {
                        if (player.IsGameMaster())
                            continue;

                        Position pos = player.GetPosition();
                        WorldSafeLocsEntry startPos = GetTeamStartPosition(GetTeamIndexByTeamId(player.GetBGTeam()));
                        if (pos.GetExactDistSq(startPos.Loc) > maxDist)
                        {
                            Log.outDebug(LogFilter.Battleground, $"Battleground: Sending {player.GetName()} back to start location (map: {GetMapId()}) (possible exploit)");
                            player.TeleportTo(startPos.Loc);
                        }
                    }
                }
            }
        }

        void _ProcessPlayerPositionBroadcast(uint diff)
        {
            m_LastPlayerPositionBroadcast += diff;
            if (m_LastPlayerPositionBroadcast >= BattlegroundConst.PlayerPositionUpdateInterval)
            {
                m_LastPlayerPositionBroadcast = 0;

                BattlegroundPlayerPositions playerPositions = new();
                for (var i = 0; i < _playerPositions.Count; ++i)
                {
                    var playerPosition = _playerPositions[i];
                    // Update position data if we found player.
                    Player player = Global.ObjAccessor.GetPlayer(GetBgMap(), playerPosition.Guid);
                    if (player != null)
                        playerPosition.Pos = player.GetPosition();

                    playerPositions.FlagCarriers.Add(playerPosition);
                }

                SendPacketToAll(playerPositions);
            }
        }

        void _ProcessOfflineQueue()
        {
            // remove offline players from bg after 5 Time.Minutes
            if (!m_OfflineQueue.Empty())
            {
                var guid = m_OfflineQueue.FirstOrDefault();
                var bgPlayer = m_Players.LookupByKey(guid);
                if (bgPlayer != null)
                {
                    if (bgPlayer.OfflineRemoveTime <= GameTime.GetGameTime())
                    {
                        RemovePlayerAtLeave(guid, true, true);// remove player from BG
                        m_OfflineQueue.RemoveAt(0);                 // remove from offline queue
                    }
                }
            }
        }

        public Team GetPrematureWinner()
        {
            Team winner = Team.Other;
            if (GetPlayersCountByTeam(Team.Alliance) >= GetMinPlayersPerTeam())
                winner = Team.Alliance;
            else if (GetPlayersCountByTeam(Team.Horde) >= GetMinPlayersPerTeam())
                winner = Team.Horde;

            return winner;
        }

        void _ProcessProgress(uint diff)
        {
            // *********************************************************
            // ***           Battleground BALLANCE SYSTEM            ***
            // *********************************************************
            // if less then minimum players are in on one side, then start premature finish timer
            if (!m_PrematureCountDown)
            {
                m_PrematureCountDown = true;
                m_PrematureCountDownTimer = Global.BattlegroundMgr.GetPrematureFinishTime();
            }
            else if (m_PrematureCountDownTimer < diff)
            {
                // time's up!
                EndBattleground(GetPrematureWinner());
                m_PrematureCountDown = false;
            }
            else if (!Global.BattlegroundMgr.IsTesting())
            {
                uint newtime = m_PrematureCountDownTimer - diff;
                // announce every Time.Minute
                if (newtime > (Time.Minute * Time.InMilliseconds))
                {
                    if (newtime / (Time.Minute * Time.InMilliseconds) != m_PrematureCountDownTimer / (Time.Minute * Time.InMilliseconds))
                        SendMessageToAll(CypherStrings.BattlegroundPrematureFinishWarning, ChatMsg.System, null, m_PrematureCountDownTimer / (Time.Minute * Time.InMilliseconds));
                }
                else
                {
                    //announce every 15 seconds
                    if (newtime / (15 * Time.InMilliseconds) != m_PrematureCountDownTimer / (15 * Time.InMilliseconds))
                        SendMessageToAll(CypherStrings.BattlegroundPrematureFinishWarningSecs, ChatMsg.System, null, m_PrematureCountDownTimer / Time.InMilliseconds);
                }
                m_PrematureCountDownTimer = newtime;
            }
        }

        void _ProcessJoin(uint diff)
        {
            // *********************************************************
            // ***           Battleground STARTING SYSTEM            ***
            // *********************************************************
            ModifyStartDelayTime((int)diff);

            if (!IsArena())
                SetRemainingTime(300000);

            if (m_ResetStatTimer > 5000)
            {
                m_ResetStatTimer = 0;
                foreach (var guid in GetPlayers().Keys)
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    if (player != null)
                        player.ResetAllPowers();
                }
            }

            if (!m_Events.HasAnyFlag(BattlegroundEventFlags.Event1))
            {
                m_Events |= BattlegroundEventFlags.Event1;

                if (FindBgMap() == null)
                {
                    Log.outError(LogFilter.Battleground, $"Battleground._ProcessJoin: map (map id: {GetMapId()}, instance id: {m_InstanceID}) is not created!");
                    EndNow();
                    return;
                }

                _preparationStartTime = GameTime.GetGameTime();
                foreach (Group group in m_BgRaids)
                    if (group != null)
                        group.StartCountdown(CountdownTimerType.Pvp, TimeSpan.FromSeconds((int)StartDelayTimes[BattlegroundConst.EventIdFirst] / 1000), _preparationStartTime);

                GetBgMap().GetBattlegroundScript().OnPrepareStage1();
                SetStartDelayTime(StartDelayTimes[BattlegroundConst.EventIdFirst]);
                // First start warning - 2 or 1 Minute
                if (StartMessageIds[BattlegroundConst.EventIdFirst] != 0)
                    SendBroadcastText(StartMessageIds[BattlegroundConst.EventIdFirst], ChatMsg.BgSystemNeutral);
            }
            // After 1 Time.Minute or 30 seconds, warning is signaled
            else if (GetStartDelayTime() <= (int)StartDelayTimes[BattlegroundConst.EventIdSecond] && !m_Events.HasAnyFlag(BattlegroundEventFlags.Event2))
            {
                m_Events |= BattlegroundEventFlags.Event2;
                GetBgMap().GetBattlegroundScript().OnPrepareStage2();
                if (StartMessageIds[BattlegroundConst.EventIdSecond] != 0)
                    SendBroadcastText(StartMessageIds[BattlegroundConst.EventIdSecond], ChatMsg.BgSystemNeutral);
            }
            // After 30 or 15 seconds, warning is signaled
            else if (GetStartDelayTime() <= (int)StartDelayTimes[BattlegroundConst.EventIdThird] && !m_Events.HasAnyFlag(BattlegroundEventFlags.Event3))
            {
                m_Events |= BattlegroundEventFlags.Event3;
                GetBgMap().GetBattlegroundScript().OnPrepareStage3();
                if (StartMessageIds[BattlegroundConst.EventIdThird] != 0)
                    SendBroadcastText(StartMessageIds[BattlegroundConst.EventIdThird], ChatMsg.BgSystemNeutral);
            }
            // Delay expired (after 2 or 1 Time.Minute)
            else if (GetStartDelayTime() <= 0 && !m_Events.HasAnyFlag(BattlegroundEventFlags.Event4))
            {
                m_Events |= BattlegroundEventFlags.Event4;

                GetBgMap().GetBattlegroundScript().OnStart();

                if (StartMessageIds[BattlegroundConst.EventIdFourth] != 0)
                    SendBroadcastText(StartMessageIds[BattlegroundConst.EventIdFourth], ChatMsg.RaidBossEmote);
                SetStatus(BattlegroundStatus.InProgress);
                SetStartDelayTime(StartDelayTimes[BattlegroundConst.EventIdFourth]);

                SendPacketToAll(new PVPMatchSetState(PVPMatchState.Engaged));

                foreach (var (guid, _) in GetPlayers())
                {
                    Player player = Global.ObjAccessor.GetPlayer(GetBgMap(), guid);
                    if (player != null)
                    {
                        player.StartCriteria(CriteriaStartEvent.StartBattleground, GetBgMap().GetId());
                        player.AtStartOfEncounter(EncounterType.Battleground);
                    }
                }

                // Remove preparation
                if (IsArena())
                {
                    //todo add arena sound PlaySoundToAll(SOUND_ARENA_START);
                    foreach (var guid in GetPlayers().Keys)
                    {
                        Player player = Global.ObjAccessor.FindPlayer(guid);
                        if (player != null)
                        {
                            // Correctly display EnemyUnitFrame
                            player.SetArenaFaction((byte)player.GetBGTeam());

                            player.RemoveAurasDueToSpell(BattlegroundConst.SpellArenaPreparation);
                            player.ResetAllPowers();
                            if (!player.IsGameMaster())
                            {
                                // remove auras with duration lower than 30s
                                player.RemoveAppliedAuras(aurApp =>
                                {
                                    Aura aura = aurApp.GetBase();
                                    return !aura.IsPermanent() && aura.GetDuration() <= 30 * Time.InMilliseconds && aurApp.IsPositive()
                                    && !aura.GetSpellInfo().HasAttribute(SpellAttr0.NoImmunities) && !aura.HasEffectType(AuraType.ModInvisibility);
                                });
                            }
                        }
                    }

                    CheckWinConditions();
                }
                else
                {
                    PlaySoundToAll((uint)BattlegroundSounds.BgStart);

                    foreach (var guid in GetPlayers().Keys)
                    {
                        Player player = Global.ObjAccessor.FindPlayer(guid);
                        if (player != null)
                        {
                            player.RemoveAurasDueToSpell(BattlegroundConst.SpellPreparation);
                            player.ResetAllPowers();
                        }
                    }
                    // Announce BG starting
                    if (WorldConfig.GetBoolValue(WorldCfg.BattlegroundQueueAnnouncerEnable))
                        Global.WorldMgr.SendWorldText(CypherStrings.BgStartedAnnounceWorld, GetName(), GetMinLevel(), GetMaxLevel());
                }
            }

            if (GetRemainingTime() > 0 && (m_EndTime -= (int)diff) > 0)
                SetRemainingTime(GetRemainingTime() - diff);
        }

        void _ProcessLeave(uint diff)
        {
            // *********************************************************
            // ***           Battleground ENDING SYSTEM              ***
            // *********************************************************
            // remove all players from Battleground after 2 Time.Minutes
            SetRemainingTime(GetRemainingTime() - diff);
            if (GetRemainingTime() <= 0)
            {
                SetRemainingTime(0);
                foreach (var guid in m_Players.Keys)
                {
                    RemovePlayerAtLeave(guid, true, true);// remove player from BG
                    // do not change any Battleground's private variables
                }
            }
        }

        public Player _GetPlayer(ObjectGuid guid, bool offlineRemove, string context)
        {
            Player player = null;
            if (!offlineRemove)
            {
                player = Global.ObjAccessor.FindPlayer(guid);
                if (player == null)
                    Log.outError(LogFilter.Battleground, $"Battleground.{context}: player ({guid}) not found for BG (map: {GetMapId()}, instance id: {m_InstanceID})!");
            }
            return player;
        }

        public Player _GetPlayer(KeyValuePair<ObjectGuid, BattlegroundPlayer> pair, string context)
        {
            return _GetPlayer(pair.Key, pair.Value.OfflineRemoveTime != 0, context);
        }

        Player _GetPlayerForTeam(Team team, KeyValuePair<ObjectGuid, BattlegroundPlayer> pair, string context)
        {
            Player player = _GetPlayer(pair, context);
            if (player != null)
            {
                Team playerTeam = pair.Value.Team;
                if (playerTeam == 0)
                    playerTeam = player.GetEffectiveTeam();
                if (playerTeam != team)
                    player = null;
            }
            return player;
        }

        public BattlegroundMap GetBgMap()
        {
            Cypher.Assert(m_Map != null);
            return m_Map;
        }

        public WorldSafeLocsEntry GetTeamStartPosition(int teamId)
        {
            Cypher.Assert(teamId < BattleGroundTeamId.Neutral);
            return _battlegroundTemplate.StartLocation[teamId];
        }

        float GetStartMaxDist()
        {
            return _battlegroundTemplate.MaxStartDistSq;
        }

        public void SendPacketToAll(ServerPacket packet)
        {
            foreach (var pair in m_Players)
            {
                Player player = _GetPlayer(pair, "SendPacketToAll");
                if (player != null)
                    player.SendPacket(packet);
            }
        }

        void SendPacketToTeam(Team team, ServerPacket packet, Player except = null)
        {
            foreach (var pair in m_Players)
            {
                Player player = _GetPlayerForTeam(team, pair, "SendPacketToTeam");
                if (player != null)
                {
                    if (player != except)
                        player.SendPacket(packet);
                }
            }
        }

        public void SendChatMessage(Creature source, byte textId, WorldObject target = null)
        {
            Global.CreatureTextMgr.SendChat(source, textId, target);
        }

        public void SendBroadcastText(uint id, ChatMsg msgType, WorldObject target = null)
        {
            if (!CliDB.BroadcastTextStorage.ContainsKey(id))
            {
                Log.outError(LogFilter.Battleground, $"Battleground.SendBroadcastText: `broadcast_text` (ID: {id}) was not found");
                return;
            }

            BroadcastTextBuilder builder = new(null, msgType, id, Gender.Male, target);
            LocalizedDo localizer = new(builder);
            BroadcastWorker(localizer);
        }

        public void PlaySoundToAll(uint soundID)
        {
            SendPacketToAll(new PlaySound(ObjectGuid.Empty, soundID, 0));
        }

        void PlaySoundToTeam(uint soundID, Team team)
        {
            SendPacketToTeam(team, new PlaySound(ObjectGuid.Empty, soundID, 0));
        }

        public void CastSpellOnTeam(uint SpellID, Team team)
        {
            foreach (var pair in m_Players)
            {
                Player player = _GetPlayerForTeam(team, pair, "CastSpellOnTeam");
                if (player != null)
                    player.CastSpell(player, SpellID, true);
            }
        }

        public void RemoveAuraOnTeam(uint SpellID, Team team)
        {
            foreach (var pair in m_Players)
            {
                Player player = _GetPlayerForTeam(team, pair, "RemoveAuraOnTeam");
                if (player != null)
                    player.RemoveAura(SpellID);
            }
        }

        public void RewardHonorToTeam(uint Honor, Team team)
        {
            foreach (var pair in m_Players)
            {
                Player player = _GetPlayerForTeam(team, pair, "RewardHonorToTeam");
                if (player != null)
                    UpdatePlayerScore(player, ScoreType.BonusHonor, Honor, true, HonorGainSource.TeamContribution);
            }
        }

        public void RewardReputationToTeam(uint faction_id, uint Reputation, Team team)
        {
            FactionRecord factionEntry = CliDB.FactionStorage.LookupByKey(faction_id);
            if (factionEntry == null)
                return;

            foreach (var pair in m_Players)
            {
                Player player = _GetPlayerForTeam(team, pair, "RewardReputationToTeam");
                if (player == null)
                    continue;

                if (player.HasPlayerFlagEx(PlayerFlagsEx.MercenaryMode))
                    continue;

                uint repGain = Reputation;
                MathFunctions.AddPct(ref repGain, player.GetTotalAuraModifier(AuraType.ModReputationGain));
                MathFunctions.AddPct(ref repGain, player.GetTotalAuraModifierByMiscValue(AuraType.ModFactionReputationGain, (int)faction_id));
                player.GetReputationMgr().ModifyReputation(factionEntry, (int)repGain);
            }
        }

        public void UpdateWorldState(int worldStateId, int value, bool hidden = false)
        {
            Global.WorldStateMgr.SetValue(worldStateId, value, hidden, GetBgMap());
        }

        public void UpdateWorldState(uint worldStateId, int value, bool hidden = false)
        {
            Global.WorldStateMgr.SetValue((int)worldStateId, value, hidden, GetBgMap());
        }

        public void UpdateWorldState(int worldStateId, bool value, bool hidden = false)
        {
            Global.WorldStateMgr.SetValue(worldStateId, value ? 1 : 0, hidden, GetBgMap());
        }

        public virtual void EndBattleground(Team winner)
        {
            RemoveFromBGFreeSlotQueue();

            bool guildAwarded = false;

            if (winner == Team.Alliance)
            {
                if (IsBattleground())
                    SendBroadcastText(BattlegroundBroadcastTexts.AllianceWins, ChatMsg.BgSystemNeutral);

                PlaySoundToAll((uint)BattlegroundSounds.AllianceWins);
                SetWinner(PvPTeamId.Alliance);
            }
            else if (winner == Team.Horde)
            {
                if (IsBattleground())
                    SendBroadcastText(BattlegroundBroadcastTexts.HordeWins, ChatMsg.BgSystemNeutral);

                PlaySoundToAll((uint)BattlegroundSounds.HordeWins);
                SetWinner(PvPTeamId.Horde);
            }
            else
            {
                SetWinner(PvPTeamId.Neutral);
            }

            PreparedStatement stmt;
            ulong battlegroundId = 1;
            if (IsBattleground() && WorldConfig.GetBoolValue(WorldCfg.BattlegroundStoreStatisticsEnable))
            {
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_PVPSTATS_MAXID);
                SQLResult result = DB.Characters.Query(stmt);

                if (!result.IsEmpty())
                    battlegroundId = result.Read<ulong>(0) + 1;

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_PVPSTATS_BATTLEGROUND);
                stmt.AddValue(0, battlegroundId);
                stmt.AddValue(1, (byte)GetWinner());
                stmt.AddValue(2, GetUniqueBracketId());
                stmt.AddValue(3, (uint)GetTypeID());
                DB.Characters.Execute(stmt);
            }

            SetStatus(BattlegroundStatus.WaitLeave);
            //we must set it this way, because end time is sent in packet!
            SetRemainingTime(BattlegroundConst.AutocloseBattleground);

            PVPMatchComplete pvpMatchComplete = new();
            pvpMatchComplete.Winner = (byte)GetWinner();
            pvpMatchComplete.Duration = (int)Math.Max(0, (GetElapsedTime() - (int)BattlegroundStartTimeIntervals.Delay2m) / Time.InMilliseconds);
            BuildPvPLogDataPacket(out pvpMatchComplete.LogData);
            pvpMatchComplete.Write();

            foreach (var pair in m_Players)
            {
                Team team = pair.Value.Team;

                Player player = _GetPlayer(pair, "EndBattleground");
                if (player == null)
                    continue;

                // should remove spirit of redemption
                if (player.HasAuraType(AuraType.SpiritOfRedemption))
                    player.RemoveAurasByType(AuraType.ModShapeshift);

                if (!player.IsAlive())
                {
                    player.ResurrectPlayer(1.0f);
                    player.SpawnCorpseBones();
                }
                else
                {
                    //needed cause else in av some creatures will kill the players at the end
                    player.CombatStop();
                }

                // remove temporary currency bonus auras before rewarding player
                player.RemoveAura(BattlegroundConst.SpellHonorableDefender25y);
                player.RemoveAura(BattlegroundConst.SpellHonorableDefender60y);

                uint winnerKills = player.GetRandomWinner() ? WorldConfig.GetUIntValue(WorldCfg.BgRewardWinnerHonorLast) : WorldConfig.GetUIntValue(WorldCfg.BgRewardWinnerHonorFirst);
                uint loserKills = player.GetRandomWinner() ? WorldConfig.GetUIntValue(WorldCfg.BgRewardLoserHonorLast) : WorldConfig.GetUIntValue(WorldCfg.BgRewardLoserHonorFirst);

                if (IsBattleground() && WorldConfig.GetBoolValue(WorldCfg.BattlegroundStoreStatisticsEnable))
                {
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_PVPSTATS_PLAYER);
                    var score = PlayerScores.LookupByKey(player.GetGUID());

                    stmt.AddValue(0, battlegroundId);
                    stmt.AddValue(1, player.GetGUID().GetCounter());
                    stmt.AddValue(2, team == winner);
                    stmt.AddValue(3, score.KillingBlows);
                    stmt.AddValue(4, score.Deaths);
                    stmt.AddValue(5, score.HonorableKills);
                    stmt.AddValue(6, score.BonusHonor);
                    stmt.AddValue(7, score.DamageDone);
                    stmt.AddValue(8, score.HealingDone);
                    stmt.AddValue(9, score.GetAttr(1));
                    stmt.AddValue(10, score.GetAttr(2));
                    stmt.AddValue(11, score.GetAttr(3));
                    stmt.AddValue(12, score.GetAttr(4));
                    stmt.AddValue(13, score.GetAttr(5));

                    DB.Characters.Execute(stmt);
                }

                // Reward winner team
                if (team == winner)
                {
                    BattlegroundPlayer bgPlayer = GetBattlegroundPlayerData(player.GetGUID());
                    if (bgPlayer != null)
                    {
                        if (Global.BattlegroundMgr.IsRandomBattleground((BattlegroundTypeId)bgPlayer.queueTypeId.BattlemasterListId)
                            || Global.BattlegroundMgr.IsBGWeekend((BattlegroundTypeId)bgPlayer.queueTypeId.BattlemasterListId))
                        {
                            HonorGainSource source = HonorGainSource.BGCompletion;
                            if (!player.GetRandomWinner())
                                source = Global.BattlegroundMgr.IsRandomBattleground((BattlegroundTypeId)bgPlayer.queueTypeId.BattlemasterListId) ? HonorGainSource.RandomBGCompletion : HonorGainSource.HolidayBGCompletion;

                            UpdatePlayerScore(player, ScoreType.BonusHonor, GetBonusHonorFromKill(winnerKills), true, source);
                            if (!player.GetRandomWinner())
                            {
                                player.SetRandomWinner(true);
                                // TODO: win honor xp
                            }
                        }
                        else
                        {
                            // TODO: loss honor xp
                        }
                    }

                    player.UpdateCriteria(CriteriaType.WinBattleground, player.GetMapId());
                    if (!guildAwarded)
                    {
                        guildAwarded = true;
                        uint guildId = GetBgMap().GetOwnerGuildId(player.GetBGTeam());
                        if (guildId != 0)
                        {
                            Guild guild = Global.GuildMgr.GetGuildById(guildId);
                            if (guild != null)
                                guild.UpdateCriteria(CriteriaType.WinBattleground, player.GetMapId(), 0, 0, null, player);
                        }
                    }
                }
                else
                {
                    BattlegroundPlayer bgPlayer = GetBattlegroundPlayerData(player.GetGUID());
                    if (bgPlayer != null)
                    {
                        if (Global.BattlegroundMgr.IsRandomBattleground((BattlegroundTypeId)bgPlayer.queueTypeId.BattlemasterListId)
                            || Global.BattlegroundMgr.IsBGWeekend((BattlegroundTypeId)bgPlayer.queueTypeId.BattlemasterListId))
                            UpdatePlayerScore(player, ScoreType.BonusHonor, GetBonusHonorFromKill(loserKills), true, HonorGainSource.BGCompletion);
                    }
                }

                player.ResetAllPowers();
                player.CombatStopWithPets(true);

                BlockMovement(player);

                player.SendPacket(pvpMatchComplete);

                player.UpdateCriteria(CriteriaType.ParticipateInBattleground, player.GetMapId());

                GetBgMap().GetBattlegroundScript().OnEnd(winner);
            }
        }

        uint GetScriptId()
        {
            return _battlegroundTemplate.ScriptId;
        }

        public uint GetBonusHonorFromKill(uint kills)
        {
            //variable kills means how many honorable kills you scored (so we need kills * honor_for_one_kill)
            uint maxLevel = Math.Min(GetMaxLevel(), 80U);
            return Formulas.HKHonorAtLevel(maxLevel, kills);
        }

        void BlockMovement(Player player)
        {
            // movement disabled NOTE: the effect will be automatically removed by client when the player is teleported from the battleground, so no need to send with uint8(1) in RemovePlayerAtLeave()
            player.SetClientControl(player, false);
        }

        public virtual void RemovePlayerAtLeave(ObjectGuid guid, bool Transport, bool SendPacket)
        {
            Team team = GetPlayerTeam(guid);
            bool participant = false;
            // Remove from lists/maps
            var bgPlayer = m_Players.LookupByKey(guid);
            BattlegroundQueueTypeId? bgQueueTypeId = null;
            if (bgPlayer != null)
            {
                bgQueueTypeId = bgPlayer.queueTypeId;
                UpdatePlayersCountByTeam(team, true);               // -1 player
                m_Players.Remove(guid);
                // check if the player was a participant of the match, or only entered through gm command (goname)
                participant = true;
            }

            if (PlayerScores.ContainsKey(guid))
                PlayerScores.Remove(guid);

            Player player = Global.ObjAccessor.FindPlayer(guid);
            if (player != null)
            {
                // should remove spirit of redemption
                if (player.HasAuraType(AuraType.SpiritOfRedemption))
                    player.RemoveAurasByType(AuraType.ModShapeshift);

                player.RemoveAurasByType(AuraType.Mounted);
                player.RemoveAura(BattlegroundConst.SpellMercenaryHorde1);
                player.RemoveAura(BattlegroundConst.SpellMercenaryHordeReactions);
                player.RemoveAura(BattlegroundConst.SpellMercenaryAlliance1);
                player.RemoveAura(BattlegroundConst.SpellMercenaryAllianceReactions);
                player.RemoveAura(BattlegroundConst.SpellMercenaryShapeshift);
                player.RemovePlayerFlagEx(PlayerFlagsEx.MercenaryMode);

                player.AtEndOfEncounter(EncounterType.Battleground);

                player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags2.LeaveArenaOrBattleground);

                if (!player.IsAlive())                              // resurrect on exit
                {
                    player.ResurrectPlayer(1.0f);
                    player.SpawnCorpseBones();
                }
            }
            else
                Player.OfflineResurrect(guid, null);

            RemovePlayer(player, guid, team);                           // BG subclass specific code

            if (participant) // if the player was a match participant, remove auras, calc rating, update queue
            {
                if (player != null)
                {
                    player.ClearAfkReports();

                    // if arena, remove the specific arena auras
                    if (IsArena())
                    {
                        // unsummon current and summon old pet if there was one and there isn't a current pet
                        player.RemovePet(null, PetSaveMode.NotInSlot);
                        player.ResummonPetTemporaryUnSummonedIfAny();
                    }
                    if (SendPacket && bgQueueTypeId.HasValue)
                    {
                        BattlefieldStatusNone battlefieldStatus;
                        Global.BattlegroundMgr.BuildBattlegroundStatusNone(out battlefieldStatus, player, player.GetBattlegroundQueueIndex(bgQueueTypeId.Value), player.GetBattlegroundQueueJoinTime(bgQueueTypeId.Value));
                        player.SendPacket(battlefieldStatus);
                    }

                    // this call is important, because player, when joins to Battleground, this method is not called, so it must be called when leaving bg
                    if (bgQueueTypeId.HasValue)
                        player.RemoveBattlegroundQueueId(bgQueueTypeId.Value);
                }

                // remove from raid group if player is member
                Group group = GetBgRaid(team);
                if (group != null)
                {
                    if (!group.RemoveMember(guid))                // group was disbanded
                        SetBgRaid(team, null);
                }
                DecreaseInvitedCount(team);
                //we should update Battleground queue, but only if bg isn't ending
                if (IsBattleground() && GetStatus() < BattlegroundStatus.WaitLeave && bgQueueTypeId.HasValue)
                {
                    // a player has left the Battleground, so there are free slots . add to queue
                    AddToBGFreeSlotQueue();
                    Global.BattlegroundMgr.ScheduleQueueUpdate(0, bgQueueTypeId.Value, GetBracketId());
                }
                // Let others know
                BattlegroundPlayerLeft playerLeft = new();
                playerLeft.Guid = guid;
                SendPacketToTeam(team, playerLeft, player);
            }

            if (player != null)
            {
                // Do next only if found in Battleground
                player.SetBattlegroundId(0, BattlegroundTypeId.None);  // We're not in BG.
                // reset destination bg team
                player.SetBGTeam(Team.Other);

                // remove all criterias on bg leave
                player.FailCriteria(CriteriaFailEvent.LeaveBattleground, 0);

                if (Transport)
                    player.TeleportToBGEntryPoint();

                Log.outDebug(LogFilter.Battleground, "Removed player {0} from Battleground.", player.GetName());
            }

            //Battleground object will be deleted next Battleground.Update() call
        }

        // this method is called when no players remains in Battleground
        public virtual void Reset()
        {
            SetWinner(PvPTeamId.Neutral);
            SetStatus(BattlegroundStatus.WaitQueue);
            SetElapsedTime(0);
            SetRemainingTime(0);
            m_Events = 0;

            if (m_InvitedAlliance > 0 || m_InvitedHorde > 0)
                Log.outError(LogFilter.Battleground, $"Battleground.Reset: one of the counters is not 0 (Team.Alliance: {m_InvitedAlliance}, Team.Horde: {m_InvitedHorde}) for BG (map: {GetMapId()}, instance id: {m_InstanceID})!");

            m_InvitedAlliance = 0;
            m_InvitedHorde = 0;
            m_InBGFreeSlotQueue = false;

            m_Players.Clear();

            PlayerScores.Clear();

            _playerPositions.Clear();
        }

        public void StartBattleground()
        {
            SetElapsedTime(0);
            // add BG to free slot queue
            AddToBGFreeSlotQueue();

            // add bg to update list
            // This must be done here, because we need to have already invited some players when first BG.Update() method is executed
            // and it doesn't matter if we call StartBattleground() more times, because m_Battlegrounds is a map and instance id never changes
            Global.BattlegroundMgr.AddBattleground(this);

            if (m_IsRated)
                Log.outDebug(LogFilter.Arena, "Arena match type: {0} for Team1Id: {1} - Team2Id: {2} started.", m_ArenaType, m_ArenaTeamIds[BattleGroundTeamId.Alliance], m_ArenaTeamIds[BattleGroundTeamId.Horde]);
        }

        public void TeleportPlayerToExploitLocation(Player player)
        {
            WorldSafeLocsEntry loc = GetExploitTeleportLocation(player.GetBGTeam());
            if (loc != null)
                player.TeleportTo(loc.Loc);
        }

        public virtual void AddPlayer(Player player, BattlegroundQueueTypeId queueId)
        {
            // remove afk from player
            if (player.IsAFK())
                player.ToggleAFK();

            // score struct must be created in inherited class

            ObjectGuid guid = player.GetGUID();
            Team team = player.GetBGTeam();

            BattlegroundPlayer bp = new();
            bp.OfflineRemoveTime = 0;
            bp.Team = team;
            bp.Mercenary = player.IsMercenaryForBattlegroundQueueType(queueId);
            bp.queueTypeId = queueId;

            bool isInBattleground = IsPlayerInBattleground(player.GetGUID());
            // Add to list/maps
            m_Players[guid] = bp;

            if (!isInBattleground)
            {
                UpdatePlayersCountByTeam(team, false);                  // +1 player
                PlayerScores[player.GetGUID()] = new BattlegroundScore(player.GetGUID(), player.GetBGTeam(), _pvpStatIds);
            }

            BattlegroundPlayerJoined playerJoined = new();
            playerJoined.Guid = player.GetGUID();
            SendPacketToTeam(team, playerJoined, player);

            PVPMatchInitialize pvpMatchInitialize = new();
            pvpMatchInitialize.MapID = GetMapId();
            switch (GetStatus())
            {
                case BattlegroundStatus.None:
                case BattlegroundStatus.WaitQueue:
                    pvpMatchInitialize.State = PVPMatchState.Inactive;
                    break;
                case BattlegroundStatus.WaitJoin:
                    pvpMatchInitialize.State = PVPMatchState.StartUp;
                    break;
                case BattlegroundStatus.InProgress:
                    pvpMatchInitialize.State = PVPMatchState.Engaged;
                    break;
                case BattlegroundStatus.WaitLeave:
                    pvpMatchInitialize.State = PVPMatchState.Complete;
                    break;
                default:
                    break;
            }

            if (GetElapsedTime() >= (int)BattlegroundStartTimeIntervals.Delay2m)
            {
                pvpMatchInitialize.Duration = (int)(GetElapsedTime() - (int)BattlegroundStartTimeIntervals.Delay2m) / Time.InMilliseconds;
                pvpMatchInitialize.StartTime = GameTime.GetGameTime() - pvpMatchInitialize.Duration;
            }

            pvpMatchInitialize.ArenaFaction = (byte)(player.GetBGTeam() == Team.Horde ? PvPTeamId.Horde : PvPTeamId.Alliance);
            pvpMatchInitialize.BattlemasterListID = queueId.BattlemasterListId;
            pvpMatchInitialize.Registered = false;
            pvpMatchInitialize.AffectsRating = IsRated();

            player.SendPacket(pvpMatchInitialize);

            player.RemoveAurasByType(AuraType.Mounted);

            // add arena specific auras
            if (IsArena())
            {
                player.RemoveArenaEnchantments(EnchantmentSlot.Temp);

                player.DestroyConjuredItems(true);
                player.UnsummonPetTemporaryIfAny();

                if (GetStatus() == BattlegroundStatus.WaitJoin)                 // not started yet
                {
                    player.CastSpell(player, BattlegroundConst.SpellArenaPreparation, true);
                    player.ResetAllPowers();
                }
            }
            else
            {
                if (GetStatus() == BattlegroundStatus.WaitJoin)                 // not started yet
                    player.CastSpell(player, BattlegroundConst.SpellPreparation, true);   // reduces all mana cost of spells.

                if (bp.Mercenary)
                {
                    if (bp.Team == Team.Horde)
                    {
                        player.CastSpell(player, BattlegroundConst.SpellMercenaryHorde1, true);
                        player.CastSpell(player, BattlegroundConst.SpellMercenaryHordeReactions, true);
                    }
                    else if (bp.Team == Team.Alliance)
                    {
                        player.CastSpell(player, BattlegroundConst.SpellMercenaryAlliance1, true);
                        player.CastSpell(player, BattlegroundConst.SpellMercenaryAllianceReactions, true);
                    }

                    player.CastSpell(player, BattlegroundConst.SpellMercenaryShapeshift);
                    player.SetPlayerFlagEx(PlayerFlagsEx.MercenaryMode);
                }
            }

            // setup BG group membership
            PlayerAddedToBGCheckIfBGIsRunning(player);
            AddOrSetPlayerToCorrectBgGroup(player, team);

            GetBgMap().GetBattlegroundScript().OnPlayerJoined(player, isInBattleground);
        }

        // this method adds player to his team's bg group, or sets his correct group if player is already in bg group
        public void AddOrSetPlayerToCorrectBgGroup(Player player, Team team)
        {
            ObjectGuid playerGuid = player.GetGUID();
            Group group = GetBgRaid(team);
            if (group == null)                                      // first player joined
            {
                group = new Group();
                SetBgRaid(team, group);
                group.Create(player);
                TimeSpan countdownMaxForBGType = TimeSpan.FromSeconds((int)StartDelayTimes[BattlegroundConst.EventIdFirst] / 1000);
                if (_preparationStartTime != 0)
                    group.StartCountdown(CountdownTimerType.Pvp, countdownMaxForBGType, _preparationStartTime);
                else
                    group.StartCountdown(CountdownTimerType.Pvp, countdownMaxForBGType);
            }
            else                                            // raid already exist
            {
                if (group.IsMember(playerGuid))
                {
                    byte subgroup = group.GetMemberGroup(playerGuid);
                    player.SetBattlegroundOrBattlefieldRaid(group, subgroup);
                }
                else
                {
                    group.AddMember(player);
                    Group originalGroup = player.GetOriginalGroup();
                    if (originalGroup != null)
                    {
                        if (originalGroup.IsLeader(playerGuid))
                        {
                            group.ChangeLeader(playerGuid);
                            group.SendUpdate();
                        }
                    }
                }
            }
        }

        // This method should be called when player logs into running Battleground
        public void EventPlayerLoggedIn(Player player)
        {
            ObjectGuid guid = player.GetGUID();
            // player is correct pointer
            foreach (var id in m_OfflineQueue)
            {
                if (id == guid)
                {
                    m_OfflineQueue.Remove(id);
                    break;
                }
            }
            m_Players[guid].OfflineRemoveTime = 0;
            PlayerAddedToBGCheckIfBGIsRunning(player);
            // if Battleground is starting, then add preparation aura
            // we don't have to do that, because preparation aura isn't removed when player logs out
        }

        // This method should be called when player logs out from running Battleground
        public void EventPlayerLoggedOut(Player player)
        {
            ObjectGuid guid = player.GetGUID();
            if (!IsPlayerInBattleground(guid))  // Check if this player really is in Battleground (might be a GM who teleported inside)
                return;

            // player is correct pointer, it is checked in WorldSession.LogoutPlayer()
            m_OfflineQueue.Add(player.GetGUID());
            m_Players[guid].OfflineRemoveTime = GameTime.GetGameTime() + BattlegroundConst.MaxOfflineTime;
            if (GetStatus() == BattlegroundStatus.InProgress)
            {
                // drop flag and handle other cleanups
                RemovePlayer(player, guid, GetPlayerTeam(guid));

                // 1 player is logging out, if it is the last alive, then end arena!
                if (IsArena() && player.IsAlive())
                    if (GetAlivePlayersCountByTeam(player.GetBGTeam()) <= 1 && GetPlayersCountByTeam(SharedConst.GetOtherTeam(player.GetBGTeam())) != 0)
                        EndBattleground(SharedConst.GetOtherTeam(player.GetBGTeam()));
            }
        }

        // This method should be called only once ... it adds pointer to queue
        void AddToBGFreeSlotQueue()
        {
            if (!m_InBGFreeSlotQueue && IsBattleground())
            {
                Global.BattlegroundMgr.AddToBGFreeSlotQueue(this);
                m_InBGFreeSlotQueue = true;
            }
        }

        // This method removes this Battleground from free queue - it must be called when deleting Battleground
        public void RemoveFromBGFreeSlotQueue()
        {
            if (m_InBGFreeSlotQueue)
            {
                Global.BattlegroundMgr.RemoveFromBGFreeSlotQueue(GetMapId(), m_InstanceID);
                m_InBGFreeSlotQueue = false;
            }
        }

        // get the number of free slots for team
        // returns the number how many players can join Battleground to MaxPlayersPerTeam
        public uint GetFreeSlotsForTeam(Team team)
        {
            // if BG is starting and WorldCfg.BattlegroundInvitationType == BattlegroundQueueInvitationTypeB.NoBalance, invite anyone
            if (GetStatus() == BattlegroundStatus.WaitJoin && WorldConfig.GetIntValue(WorldCfg.BattlegroundInvitationType) == (int)BattlegroundQueueInvitationType.NoBalance)
                return (GetInvitedCount(team) < GetMaxPlayersPerTeam()) ? GetMaxPlayersPerTeam() - GetInvitedCount(team) : 0;

            // if BG is already started or WorldCfg.BattlegroundInvitationType != BattlegroundQueueInvitationType.NoBalance, do not allow to join too much players of one faction
            uint otherTeamInvitedCount;
            uint thisTeamInvitedCount;
            uint otherTeamPlayersCount;
            uint thisTeamPlayersCount;

            if (team == Team.Alliance)
            {
                thisTeamInvitedCount = GetInvitedCount(Team.Alliance);
                otherTeamInvitedCount = GetInvitedCount(Team.Horde);
                thisTeamPlayersCount = GetPlayersCountByTeam(Team.Alliance);
                otherTeamPlayersCount = GetPlayersCountByTeam(Team.Horde);
            }
            else
            {
                thisTeamInvitedCount = GetInvitedCount(Team.Horde);
                otherTeamInvitedCount = GetInvitedCount(Team.Alliance);
                thisTeamPlayersCount = GetPlayersCountByTeam(Team.Horde);
                otherTeamPlayersCount = GetPlayersCountByTeam(Team.Alliance);
            }
            if (GetStatus() == BattlegroundStatus.InProgress || GetStatus() == BattlegroundStatus.WaitJoin)
            {
                // difference based on ppl invited (not necessarily entered battle)
                // default: allow 0
                uint diff = 0;

                // allow join one person if the sides are equal (to fill up bg to minPlayerPerTeam)
                if (otherTeamInvitedCount == thisTeamInvitedCount)
                    diff = 1;
                // allow join more ppl if the other side has more players
                else if (otherTeamInvitedCount > thisTeamInvitedCount)
                    diff = otherTeamInvitedCount - thisTeamInvitedCount;

                // difference based on max players per team (don't allow inviting more)
                uint diff2 = (thisTeamInvitedCount < GetMaxPlayersPerTeam()) ? GetMaxPlayersPerTeam() - thisTeamInvitedCount : 0;
                // difference based on players who already entered
                // default: allow 0
                uint diff3 = 0;
                // allow join one person if the sides are equal (to fill up bg minPlayerPerTeam)
                if (otherTeamPlayersCount == thisTeamPlayersCount)
                    diff3 = 1;
                // allow join more ppl if the other side has more players
                else if (otherTeamPlayersCount > thisTeamPlayersCount)
                    diff3 = otherTeamPlayersCount - thisTeamPlayersCount;
                // or other side has less than minPlayersPerTeam
                else if (thisTeamInvitedCount <= GetMinPlayersPerTeam())
                    diff3 = GetMinPlayersPerTeam() - thisTeamInvitedCount + 1;

                // return the minimum of the 3 differences

                // min of diff and diff 2
                diff = Math.Min(diff, diff2);
                // min of diff, diff2 and diff3
                return Math.Min(diff, diff3);
            }
            return 0;
        }

        public bool IsArena()
        {
            return _battlegroundTemplate.IsArena();
        }

        public bool IsBattleground()
        {
            return !IsArena();
        }

        public bool HasFreeSlots()
        {
            return GetPlayersSize() < GetMaxPlayers();
        }

        public virtual void BuildPvPLogDataPacket(out PVPMatchStatistics pvpLogData)
        {
            pvpLogData = new PVPMatchStatistics();

            foreach (var score in PlayerScores)
            {
                Player player = Global.ObjAccessor.GetPlayer(GetBgMap(), score.Key);
                if (player != null)
                {
                    score.Value.BuildPvPLogPlayerDataPacket(out PVPMatchStatistics.PVPMatchPlayerStatistics playerData);

                    playerData.IsInWorld = true;
                    playerData.PrimaryTalentTree = (int)player.GetPrimarySpecialization();
                    playerData.Sex = (sbyte)player.GetGender();
                    playerData.PlayerRace = (sbyte)player.GetRace();
                    playerData.PlayerClass = (sbyte)player.GetClass();
                    playerData.HonorLevel = (int)player.GetHonorLevel();

                    pvpLogData.Statistics.Add(playerData);
                }
            }

            pvpLogData.PlayerCount[(int)PvPTeamId.Horde] = (sbyte)GetPlayersCountByTeam(Team.Horde);
            pvpLogData.PlayerCount[(int)PvPTeamId.Alliance] = (sbyte)GetPlayersCountByTeam(Team.Alliance);
        }

        public BattlegroundScore GetBattlegroundScore(Player player)
        {
            return PlayerScores.LookupByKey(player.GetGUID());
        }

        public bool UpdatePlayerScore(Player player, ScoreType type, uint value, bool doAddHonor = true, HonorGainSource? source = null)
        {
            var bgScore = PlayerScores.LookupByKey(player.GetGUID());
            if (bgScore == null)  // player not found...
                return false;

            if (type == ScoreType.BonusHonor && doAddHonor && IsBattleground())
                player.RewardHonor(null, 1, (int)value, source.GetValueOrDefault(HonorGainSource.Kill));
            else
                bgScore.UpdateScore(type, value);

            return true;
        }

        public void UpdatePvpStat(Player player, uint pvpStatId, uint value)
        {
            BattlegroundScore score = PlayerScores.LookupByKey(player.GetGUID());
            if (score != null)
                score.UpdatePvpStat(pvpStatId, value);
        }

        public uint GetMapId()
        {
            return (uint)_battlegroundTemplate.MapIDs[0];
        }

        public void SetBgMap(BattlegroundMap map)
        {
            m_Map = map;
            if (map != null)
                _pvpStatIds = Global.DB2Mgr.GetPVPStatIDsForMap(map.GetId());
            else
                _pvpStatIds = null;
        }

        public void SendMessageToAll(CypherStrings entry, ChatMsg msgType, Player source = null)
        {
            if (entry == 0)
                return;

            CypherStringChatBuilder builder = new(null, msgType, entry, source);
            LocalizedDo localizer = new(builder);
            BroadcastWorker(localizer);
        }

        public void SendMessageToAll(CypherStrings entry, ChatMsg msgType, Player source, params object[] args)
        {
            if (entry == 0)
                return;

            CypherStringChatBuilder builder = new(null, msgType, entry, source, args);
            LocalizedDo localizer = new(builder);
            BroadcastWorker(localizer);
        }

        public void AddPlayerPosition(BattlegroundPlayerPosition position)
        {
            _playerPositions.Add(position);
        }

        public void RemovePlayerPosition(ObjectGuid guid)
        {
            _playerPositions.RemoveAll(playerPosition => playerPosition.Guid == guid);
        }

        void EndNow()
        {
            RemoveFromBGFreeSlotQueue();
            SetStatus(BattlegroundStatus.WaitLeave);
            SetRemainingTime(0);
        }

        public virtual void HandleKillPlayer(Player victim, Player killer)
        {
            // Keep in mind that for arena this will have to be changed a bit

            // Add +1 deaths
            UpdatePlayerScore(victim, ScoreType.Deaths, 1);
            // Add +1 kills to group and +1 killing_blows to killer
            if (killer != null)
            {
                // Don't reward credit for killing ourselves, like fall damage of hellfire (warlock)
                if (killer == victim)
                    return;

                Team killerTeam = GetPlayerTeam(killer.GetGUID());

                UpdatePlayerScore(killer, ScoreType.HonorableKills, 1);
                UpdatePlayerScore(killer, ScoreType.KillingBlows, 1);

                foreach (var (guid, player) in m_Players)
                {
                    Player creditedPlayer = Global.ObjAccessor.FindPlayer(guid);
                    if (creditedPlayer == null || creditedPlayer == killer)
                        continue;

                    if (player.Team == killerTeam && creditedPlayer.IsAtGroupRewardDistance(victim))
                        UpdatePlayerScore(creditedPlayer, ScoreType.HonorableKills, 1);
                }
            }

            if (!IsArena())
            {
                // To be able to remove insignia -- ONLY IN Battlegrounds
                victim.SetUnitFlag(UnitFlags.Skinnable);
                RewardXPAtKill(killer, victim);
            }

            BattlegroundScript script = GetBgMap().GetBattlegroundScript();
            if (script != null)
                script.OnPlayerKilled(victim, killer);
        }

        public virtual void HandleKillUnit(Creature victim, Unit killer) 
        {
            BattlegroundScript script = GetBgMap().GetBattlegroundScript();
            if (script != null)
                script.OnUnitKilled(victim, killer);
        }

        // Return the player's team based on Battlegroundplayer info
        // Used in same faction arena matches mainly
        public Team GetPlayerTeam(ObjectGuid guid)
        {
            var player = m_Players.LookupByKey(guid);
            if (player != null)
                return player.Team;
            return Team.Other;
        }

        public bool IsPlayerInBattleground(ObjectGuid guid)
        {
            return m_Players.ContainsKey(guid);
        }

        public bool IsPlayerMercenaryInBattleground(ObjectGuid guid)
        {
            var player = m_Players.LookupByKey(guid);
            if (player != null)
                return player.Mercenary;

            return false;
        }

        void PlayerAddedToBGCheckIfBGIsRunning(Player player)
        {
            if (GetStatus() != BattlegroundStatus.WaitLeave)
                return;

            BlockMovement(player);

            PVPMatchStatisticsMessage pvpMatchStatistics = new();
            BuildPvPLogDataPacket(out pvpMatchStatistics.Data);
            player.SendPacket(pvpMatchStatistics);
        }

        public uint GetAlivePlayersCountByTeam(Team team)
        {
            uint count = 0;
            foreach (var pair in m_Players)
            {
                if (pair.Value.Team == team)
                {
                    Player player = Global.ObjAccessor.FindPlayer(pair.Key);
                    if (player != null && player.IsAlive())
                        ++count;
                }
            }
            return count;
        }

        public void SetBgRaid(Team team, Group bg_raid)
        {
            Group old_raid = m_BgRaids[GetTeamIndexByTeamId(team)];
            if (old_raid != null)
                old_raid.SetBattlegroundGroup(null);
            if (bg_raid != null)
                bg_raid.SetBattlegroundGroup(this);
            m_BgRaids[GetTeamIndexByTeamId(team)] = bg_raid;
        }

        public void SetBracket(PvpDifficultyRecord bracketEntry)
        {
            _pvpDifficultyEntry = bracketEntry;
        }

        void RewardXPAtKill(Player killer, Player victim)
        {
            if (WorldConfig.GetBoolValue(WorldCfg.BgXpForKill) && killer != null && victim != null)
                new KillRewarder(new[] { killer }, victim, true).Reward();
        }

        public uint GetTeamScore(int teamIndex)
        {
            if (teamIndex == BattleGroundTeamId.Alliance || teamIndex == BattleGroundTeamId.Horde)
                return m_TeamScores[teamIndex];

            Log.outError(LogFilter.Battleground, "GetTeamScore with wrong Team {0} for BG {1}", teamIndex, GetTypeID());
            return 0;
        }

        public string GetName()
        {
            return _battlegroundTemplate.BattlemasterEntry.Name[Global.WorldMgr.GetDefaultDbcLocale()];
        }

        public BattlegroundTypeId GetTypeID()
        {
            return _battlegroundTemplate.Id;
        }

        public BattlegroundBracketId GetBracketId()
        {
            return _pvpDifficultyEntry.GetBracketId();
        }

        byte GetUniqueBracketId()
        {
            return (byte)((GetMinLevel() / 5) - 1); // 10 - 1, 15 - 2, 20 - 3, etc.
        }

        uint GetMaxPlayers()
        {
            return GetMaxPlayersPerTeam() * 2;
        }

        uint GetMinPlayers()
        {
            return GetMinPlayersPerTeam() * 2;
        }

        public uint GetMinLevel()
        {
            if (_pvpDifficultyEntry != null)
                return _pvpDifficultyEntry.MinLevel;

            return _battlegroundTemplate.GetMinLevel();
        }

        public uint GetMaxLevel()
        {
            if (_pvpDifficultyEntry != null)
                return _pvpDifficultyEntry.MaxLevel;

            return _battlegroundTemplate.GetMaxLevel();
        }

        public uint GetMaxPlayersPerTeam()
        {
            if (IsArena())
            {
                switch (GetArenaType())
                {
                    case ArenaTypes.Team2v2:
                        return 2;
                    case ArenaTypes.Team3v3:
                        return 3;
                    case ArenaTypes.Team5v5: // removed
                        return 5;
                    default:
                        break;
                }
            }

            return _battlegroundTemplate.GetMaxPlayersPerTeam();
        }

        public uint GetMinPlayersPerTeam()
        {
            return _battlegroundTemplate.GetMinPlayersPerTeam();
        }

        public BattlegroundPlayer GetBattlegroundPlayerData(ObjectGuid playerGuid)
        {
            return m_Players.LookupByKey(playerGuid);
        }

        public void AddPoint(Team team, uint points = 1) { m_TeamScores[GetTeamIndexByTeamId(team)] += points; }
        public void SetTeamPoint(Team team, uint points = 0) { m_TeamScores[GetTeamIndexByTeamId(team)] = points; }
        void RemovePoint(Team team, uint points = 1) { m_TeamScores[GetTeamIndexByTeamId(team)] -= points; }

        public uint GetInstanceID() { return m_InstanceID; }
        public BattlegroundStatus GetStatus() { return m_Status; }
        public uint GetClientInstanceID() { return m_ClientInstanceID; }
        public uint GetElapsedTime() { return m_StartTime; }
        public uint GetRemainingTime() { return (uint)m_EndTime; }

        int GetStartDelayTime() { return m_StartDelayTime; }
        public ArenaTypes GetArenaType() { return m_ArenaType; }
        PvPTeamId GetWinner() { return _winnerTeamId; }

        //here we can count minlevel and maxlevel for players
        public void SetInstanceID(uint InstanceID) { m_InstanceID = InstanceID; }
        public void SetStatus(BattlegroundStatus Status) { m_Status = Status; }
        public void SetClientInstanceID(uint InstanceID) { m_ClientInstanceID = InstanceID; }
        public void SetElapsedTime(uint Time) { m_StartTime = Time; }
        public void SetRemainingTime(uint Time) { m_EndTime = (int)Time; }
        public void SetRated(bool state) { m_IsRated = state; }
        public void SetArenaType(ArenaTypes type) { m_ArenaType = type; }
        public void SetWinner(PvPTeamId winnerTeamId) { _winnerTeamId = winnerTeamId; }
        public List<uint> GetPvpStatIds() { return _pvpStatIds; }

        void ModifyStartDelayTime(int diff) { m_StartDelayTime -= diff; }
        void SetStartDelayTime(BattlegroundStartTimeIntervals Time) { m_StartDelayTime = (int)Time; }

        public void DecreaseInvitedCount(Team team)
        {
            if (team == Team.Alliance)
                --m_InvitedAlliance;
            else
                --m_InvitedHorde;
        }
        public void IncreaseInvitedCount(Team team)
        {
            if (team == Team.Alliance)
                ++m_InvitedAlliance;
            else
                ++m_InvitedHorde;
        }

        uint GetInvitedCount(Team team) { return (team == Team.Alliance) ? m_InvitedAlliance : m_InvitedHorde; }

        public bool IsRated() { return m_IsRated; }

        public Dictionary<ObjectGuid, BattlegroundPlayer> GetPlayers() { return m_Players; }
        uint GetPlayersSize() { return (uint)m_Players.Count; }
        uint GetPlayerScoresSize() { return (uint)PlayerScores.Count; }

        public BattlegroundMap FindBgMap() { return m_Map; }

        Group GetBgRaid(Team team) { return m_BgRaids[GetTeamIndexByTeamId(team)]; }

        public static int GetTeamIndexByTeamId(Team team) { return team == Team.Alliance ? BattleGroundTeamId.Alliance : BattleGroundTeamId.Horde; }
        public uint GetPlayersCountByTeam(Team team) { return m_PlayersCount[GetTeamIndexByTeamId(team)]; }
        void UpdatePlayersCountByTeam(Team team, bool remove)
        {
            if (remove)
                --m_PlayersCount[GetTeamIndexByTeamId(team)];
            else
                ++m_PlayersCount[GetTeamIndexByTeamId(team)];
        }

        public virtual void CheckWinConditions() { }

        public void SetArenaTeamIdForTeam(Team team, uint ArenaTeamId) { m_ArenaTeamIds[GetTeamIndexByTeamId(team)] = ArenaTeamId; }
        public uint GetArenaTeamIdForTeam(Team team) { return m_ArenaTeamIds[GetTeamIndexByTeamId(team)]; }
        public uint GetArenaTeamIdByIndex(uint index) { return m_ArenaTeamIds[index]; }
        public void SetArenaMatchmakerRating(Team team, uint MMR) { m_ArenaTeamMMR[GetTeamIndexByTeamId(team)] = MMR; }
        public uint GetArenaMatchmakerRating(Team team) { return m_ArenaTeamMMR[GetTeamIndexByTeamId(team)]; }

        public virtual WorldSafeLocsEntry GetExploitTeleportLocation(Team team) { return null; }

        public virtual bool HandlePlayerUnderMap(Player player) { return false; }

        public bool ToBeDeleted() { return m_SetDeleteThis; }
        void SetDeleteThis() { m_SetDeleteThis = true; }

        bool CanAwardArenaPoints() { return GetMinLevel() >= 71; }

        public virtual void RemovePlayer(Player player, ObjectGuid guid, Team team) { }

        public virtual bool PreUpdateImpl(uint diff) { return true; }

        public virtual void PostUpdateImpl(uint diff) { }

        void BroadcastWorker(IDoWork<Player> _do)
        {
            foreach (var pair in m_Players)
            {
                Player player = _GetPlayer(pair, "BroadcastWorker");
                if (player != null)
                    _do.Invoke(player);
            }
        }

        #region Fields
        protected Dictionary<ObjectGuid, BattlegroundScore> PlayerScores = new();                // Player scores
        // Player lists, those need to be accessible by inherited classes
        Dictionary<ObjectGuid, BattlegroundPlayer> m_Players = new();

        // these are important variables used for starting messages
        BattlegroundEventFlags m_Events;
        public BattlegroundStartTimeIntervals[] StartDelayTimes = new BattlegroundStartTimeIntervals[4];
        // this must be filled inructors!
        public uint[] StartMessageIds = new uint[4];

        public uint[] m_TeamScores = new uint[SharedConst.PvpTeamsCount];

        public uint[] Buff_Entries = { BattlegroundConst.SpeedBuff, BattlegroundConst.RegenBuff, BattlegroundConst.BerserkerBuff };

        // Battleground
        uint m_InstanceID;                                // Battleground Instance's GUID!
        BattlegroundStatus m_Status;
        uint m_ClientInstanceID;                          // the instance-id which is sent to the client and without any other internal use
        uint m_StartTime;
        uint m_ResetStatTimer;
        uint m_ValidStartPositionTimer;
        int m_EndTime;                                    // it is set to 120000 when bg is ending and it decreases itself
        ArenaTypes m_ArenaType;                                 // 2=2v2, 3=3v3, 5=5v5
        bool m_InBGFreeSlotQueue;                         // used to make sure that BG is only once inserted into the BattlegroundMgr.BGFreeSlotQueue[bgTypeId] deque
        bool m_SetDeleteThis;                             // used for safe deletion of the bg after end / all players leave
        PvPTeamId _winnerTeamId;
        int m_StartDelayTime;
        bool m_IsRated;                                   // is this battle rated?
        bool m_PrematureCountDown;
        uint m_PrematureCountDownTimer;
        uint m_LastPlayerPositionBroadcast;

        // Player lists
        List<ObjectGuid> m_OfflineQueue = new();                  // Player GUID

        // Invited counters are useful for player invitation to BG - do not allow, if BG is started to one faction to have 2 more players than another faction
        // Invited counters will be changed only when removing already invited player from queue, removing player from Battleground and inviting player to BG
        // Invited players counters
        uint m_InvitedAlliance;
        uint m_InvitedHorde;

        // Raid Group
        Group[] m_BgRaids = new Group[SharedConst.PvpTeamsCount];                   // 0 - Team.Alliance, 1 - Team.Horde

        // Players count by team
        uint[] m_PlayersCount = new uint[SharedConst.PvpTeamsCount];

        // Arena team ids by team
        uint[] m_ArenaTeamIds = new uint[SharedConst.PvpTeamsCount];

        uint[] m_ArenaTeamMMR = new uint[SharedConst.PvpTeamsCount];

        // Start location
        BattlegroundMap m_Map;

        BattlegroundTemplate _battlegroundTemplate;
        PvpDifficultyRecord _pvpDifficultyEntry;
        List<uint> _pvpStatIds = new();

        List<BattlegroundPlayerPosition> _playerPositions = new();

        // Time when the first message "the battle will begin in 2minutes" is send (or 1m for arenas)
        long _preparationStartTime;
        #endregion
    }

    public class BattlegroundPlayer
    {
        public long OfflineRemoveTime;  // for tracking and removing offline players from queue after 5 Time.Minutes
        public Team Team;               // Player's team
        public bool Mercenary;
        public BattlegroundQueueTypeId queueTypeId;
    }
}
