/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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
using Game.DataStorage;
using Game.Entities;
using Game.Network.Packets;
using System.Collections.Generic;

namespace Game.BattleGrounds.Zones
{
    class BgWarsongGluch : Battleground
    {
        public BgWarsongGluch()
        {
            BgObjects = new ObjectGuid[WSGObjectTypes.Max];
            BgCreatures = new ObjectGuid[WSGCreatureTypes.Max];

            StartMessageIds[BattlegroundConst.EventIdSecond] = WSGBroadcastTexts.StartOneMinute;
            StartMessageIds[BattlegroundConst.EventIdThird] = WSGBroadcastTexts.StartHalfMinute;
            StartMessageIds[BattlegroundConst.EventIdFourth] = WSGBroadcastTexts.BattleHasBegun;
        }

        public override void PostUpdateImpl(uint diff)
        {
            if (GetStatus() == BattlegroundStatus.InProgress)
            {
                if (GetElapsedTime() >= 27 * Time.Minute * Time.InMilliseconds)
                {
                    if (GetTeamScore(TeamId.Alliance) == 0)
                    {
                        if (GetTeamScore(TeamId.Horde) == 0)        // No one scored - result is tie
                            EndBattleground(Team.Other);
                        else                                 // Horde has more points and thus wins
                            EndBattleground(Team.Horde);
                    }
                    else if (GetTeamScore(TeamId.Horde) == 0)
                        EndBattleground(Team.Alliance);           // Alliance has > 0, Horde has 0, alliance wins
                    else if (GetTeamScore(TeamId.Horde) == GetTeamScore(TeamId.Alliance)) // Team score equal, winner is team that scored the last flag
                        EndBattleground((Team)_lastFlagCaptureTeam);
                    else if (GetTeamScore(TeamId.Horde) > GetTeamScore(TeamId.Alliance))  // Last but not least, check who has the higher score
                        EndBattleground(Team.Horde);
                    else
                        EndBattleground(Team.Alliance);
                }
                // first update needed after 1 minute of game already in progress
                else if (GetElapsedTime() > (_minutesElapsed * Time.Minute * Time.InMilliseconds) + 3 * Time.Minute * Time.InMilliseconds)
                {
                    ++_minutesElapsed;
                    UpdateWorldState(WSGWorldStates.StateTimer, (uint)(25 - _minutesElapsed));
                }

                if (_flagState[TeamId.Alliance] == WSGFlagState.WaitRespawn)
                {
                    _flagsTimer[TeamId.Alliance] -= (int)diff;

                    if (_flagsTimer[TeamId.Alliance] < 0)
                    {
                        _flagsTimer[TeamId.Alliance] = 0;
                        RespawnFlag(Team.Alliance, true);
                    }
                }

                if (_flagState[TeamId.Alliance] == WSGFlagState.OnGround)
                {
                    _flagsDropTimer[TeamId.Alliance] -= (int)diff;

                    if (_flagsDropTimer[TeamId.Alliance] < 0)
                    {
                        _flagsDropTimer[TeamId.Alliance] = 0;
                        RespawnFlagAfterDrop(Team.Alliance);
                        _bothFlagsKept = false;
                    }
                }

                if (_flagState[TeamId.Horde] == WSGFlagState.WaitRespawn)
                {
                    _flagsTimer[TeamId.Horde] -= (int)diff;

                    if (_flagsTimer[TeamId.Horde] < 0)
                    {
                        _flagsTimer[TeamId.Horde] = 0;
                        RespawnFlag(Team.Horde, true);
                    }
                }

                if (_flagState[TeamId.Horde] == WSGFlagState.OnGround)
                {
                    _flagsDropTimer[TeamId.Horde] -= (int)diff;

                    if (_flagsDropTimer[TeamId.Horde] < 0)
                    {
                        _flagsDropTimer[TeamId.Horde] = 0;
                        RespawnFlagAfterDrop(Team.Horde);
                        _bothFlagsKept = false;
                    }
                }

                if (_bothFlagsKept)
                {
                    _flagSpellForceTimer += (int)diff;
                    if (_flagDebuffState == 0 && _flagSpellForceTimer >= 10 * Time.Minute * Time.InMilliseconds)  //10 minutes
                    {
                        Player player = Global.ObjAccessor.FindPlayer(m_FlagKeepers[0]);
                        if (player)
                            player.CastSpell(player, WSGSpellId.FocusedAssault, true);

                        player = Global.ObjAccessor.FindPlayer(m_FlagKeepers[1]);
                        if (player)
                            player.CastSpell(player, WSGSpellId.FocusedAssault, true);

                        _flagDebuffState = 1;
                    }
                    else if (_flagDebuffState == 1 && _flagSpellForceTimer >= 900000) //15 minutes
                    {
                        Player player = Global.ObjAccessor.FindPlayer(m_FlagKeepers[0]);
                        if (player)
                        {
                            player.RemoveAurasDueToSpell(WSGSpellId.FocusedAssault);
                            player.CastSpell(player, WSGSpellId.BrutalAssault, true);
                        }

                        player = Global.ObjAccessor.FindPlayer(m_FlagKeepers[1]);
                        if (player)
                        {
                            player.RemoveAurasDueToSpell(WSGSpellId.FocusedAssault);
                            player.CastSpell(player, WSGSpellId.BrutalAssault, true);
                        }
                        _flagDebuffState = 2;
                    }
                }
                else
                {
                    Player player = Global.ObjAccessor.FindPlayer(m_FlagKeepers[0]);
                    if (player)
                    {
                        player.RemoveAurasDueToSpell(WSGSpellId.FocusedAssault);
                        player.RemoveAurasDueToSpell(WSGSpellId.BrutalAssault);
                    }

                    player = Global.ObjAccessor.FindPlayer(m_FlagKeepers[1]);
                    if (player)
                    {
                        player.RemoveAurasDueToSpell(WSGSpellId.FocusedAssault);
                        player.RemoveAurasDueToSpell(WSGSpellId.BrutalAssault);
                    }

                    _flagSpellForceTimer = 0; //reset timer.
                    _flagDebuffState = 0;
                }
            }
        }

        public override void GetPlayerPositionData(List<BattlegroundPlayerPosition> positions)
        {
            Player player = Global.ObjAccessor.GetPlayer(GetBgMap(), m_FlagKeepers[TeamId.Alliance]);
            if (player)
            {
                BattlegroundPlayerPosition position = new BattlegroundPlayerPosition();
                position.Guid = player.GetGUID();
                position.Pos.X = player.GetPositionX();
                position.Pos.Y = player.GetPositionY();
                position.IconID = BattlegroundConst.PlayerPositionIconAllianceFlag;
                position.ArenaSlot = BattlegroundConst.PlayerPositionArenaSlotNone;
                positions.Add(position);
            }

            player = Global.ObjAccessor.GetPlayer(GetBgMap(), m_FlagKeepers[TeamId.Horde]);
            if (player)
            {
                BattlegroundPlayerPosition position = new BattlegroundPlayerPosition();
                position.Guid = player.GetGUID();
                position.Pos.X = player.GetPositionX();
                position.Pos.Y = player.GetPositionY();
                position.IconID = BattlegroundConst.PlayerPositionIconHordeFlag;
                position.ArenaSlot = BattlegroundConst.PlayerPositionArenaSlotNone;
                positions.Add(position);
            }
        }

        public override void StartingEventCloseDoors()
        {
            for (int i = WSGObjectTypes.DoorA1; i <= WSGObjectTypes.DoorH4; ++i)
            {
                DoorClose(i);
                SpawnBGObject(i, BattlegroundConst.RespawnImmediately);
            }
            for (int i = WSGObjectTypes.AFlag; i <= WSGObjectTypes.Berserkbuff2; ++i)
                SpawnBGObject(i, BattlegroundConst.RespawnOneDay);

            UpdateWorldState(WSGWorldStates.StateTimerActive, 1);
            UpdateWorldState(WSGWorldStates.StateTimer, 25);
        }

        public override void StartingEventOpenDoors()
        {
            for (int i = WSGObjectTypes.DoorA1; i <= WSGObjectTypes.DoorA6; ++i)
                DoorOpen(i);
            for (int i = WSGObjectTypes.DoorH1; i <= WSGObjectTypes.DoorH4; ++i)
                DoorOpen(i);

            for (int i = WSGObjectTypes.AFlag; i <= WSGObjectTypes.Berserkbuff2; ++i)
                SpawnBGObject(i, BattlegroundConst.RespawnImmediately);

            SpawnBGObject(WSGObjectTypes.DoorA5, BattlegroundConst.RespawnOneDay);
            SpawnBGObject(WSGObjectTypes.DoorA6, BattlegroundConst.RespawnOneDay);
            SpawnBGObject(WSGObjectTypes.DoorH3, BattlegroundConst.RespawnOneDay);
            SpawnBGObject(WSGObjectTypes.DoorH4, BattlegroundConst.RespawnOneDay);

            // players joining later are not eligibles
            StartCriteriaTimer(CriteriaTimedTypes.Event, 8563);
        }

        public override void AddPlayer(Player player)
        {
            base.AddPlayer(player);
            PlayerScores[player.GetGUID()] = new BattlegroundWGScore(player.GetGUID(), player.GetBGTeam());
        }

        void RespawnFlag(Team Team, bool captured)
        {
            if (Team == Team.Alliance)
            {
                Log.outDebug(LogFilter.Battleground, "Respawn Alliance flag");
                _flagState[TeamId.Alliance] = WSGFlagState.OnBase;
            }
            else
            {
                Log.outDebug(LogFilter.Battleground, "Respawn Horde flag");
                _flagState[TeamId.Horde] = WSGFlagState.OnBase;
            }

            if (captured)
            {
                //when map_update will be allowed for Battlegrounds this code will be useless
                SpawnBGObject(WSGObjectTypes.HFlag, BattlegroundConst.RespawnImmediately);
                SpawnBGObject(WSGObjectTypes.AFlag, BattlegroundConst.RespawnImmediately);
                SendBroadcastText(WSGBroadcastTexts.FlagsPlaced, ChatMsg.BgSystemNeutral);
                PlaySoundToAll(WSGSound.FlagsRespawned);        // flag respawned sound...
            }
            _bothFlagsKept = false;
        }

        void RespawnFlagAfterDrop(Team team)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            RespawnFlag(team, false);
            if (team == Team.Alliance)
                SpawnBGObject(WSGObjectTypes.AFlag, BattlegroundConst.RespawnImmediately);
            else
                SpawnBGObject(WSGObjectTypes.HFlag, BattlegroundConst.RespawnImmediately);

            SendBroadcastText(WSGBroadcastTexts.FlagsPlaced, ChatMsg.BgSystemNeutral);
            PlaySoundToAll(WSGSound.FlagsRespawned);

            GameObject obj = GetBgMap().GetGameObject(GetDroppedFlagGUID(team));
            if (obj)
                obj.Delete();
            else
                Log.outError(LogFilter.Battleground, "unknown droped flag ({0})", GetDroppedFlagGUID(team).ToString());

            SetDroppedFlagGUID(ObjectGuid.Empty, GetTeamIndexByTeamId(team));
            _bothFlagsKept = false;
        }

        void EventPlayerCapturedFlag(Player player)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            Team winner = 0;

            player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.EnterPvpCombat);
            if (player.GetTeam() == Team.Alliance)
            {
                if (!IsHordeFlagPickedup())
                    return;
                SetHordeFlagPicker(ObjectGuid.Empty);                              // must be before aura remove to prevent 2 events (drop+capture) at the same time
                                                                                   // horde flag in base (but not respawned yet)
                _flagState[TeamId.Horde] = WSGFlagState.WaitRespawn;
                // Drop Horde Flag from Player
                player.RemoveAurasDueToSpell(WSGSpellId.WarsongFlag);
                if (_flagDebuffState == 1)
                    player.RemoveAurasDueToSpell(WSGSpellId.FocusedAssault);
                else if (_flagDebuffState == 2)
                    player.RemoveAurasDueToSpell(WSGSpellId.BrutalAssault);

                if (GetTeamScore(TeamId.Alliance) < WSGTimerOrScore.MaxTeamScore)
                    AddPoint(Team.Alliance, 1);
                PlaySoundToAll(WSGSound.FlagCapturedAlliance);
                RewardReputationToTeam(890, m_ReputationCapture, Team.Alliance);
            }
            else
            {
                if (!IsAllianceFlagPickedup())
                    return;
                SetAllianceFlagPicker(ObjectGuid.Empty);                           // must be before aura remove to prevent 2 events (drop+capture) at the same time
                                                                                   // alliance flag in base (but not respawned yet)
                _flagState[TeamId.Alliance] = WSGFlagState.WaitRespawn;
                // Drop Alliance Flag from Player
                player.RemoveAurasDueToSpell(WSGSpellId.SilverwingFlag);
                if (_flagDebuffState == 1)
                    player.RemoveAurasDueToSpell(WSGSpellId.FocusedAssault);
                else if (_flagDebuffState == 2)
                    player.RemoveAurasDueToSpell(WSGSpellId.BrutalAssault);

                if (GetTeamScore(TeamId.Horde) < WSGTimerOrScore.MaxTeamScore)
                    AddPoint(Team.Horde, 1);
                PlaySoundToAll(WSGSound.FlagCapturedHorde);
                RewardReputationToTeam(889, m_ReputationCapture, Team.Horde);
            }
            //for flag capture is reward 2 honorable kills
            RewardHonorToTeam(GetBonusHonorFromKill(2), player.GetTeam());

            SpawnBGObject(WSGObjectTypes.HFlag, WSGTimerOrScore.FlagRespawnTime);
            SpawnBGObject(WSGObjectTypes.AFlag, WSGTimerOrScore.FlagRespawnTime);

            if (player.GetTeam() == Team.Alliance)
                SendBroadcastText(WSGBroadcastTexts.CapturedHordeFlag, ChatMsg.BgSystemAlliance, player);
            else
                SendBroadcastText(WSGBroadcastTexts.CapturedAllianceFlag, ChatMsg.BgSystemHorde, player);

            UpdateFlagState(player.GetTeam(), WSGFlagState.WaitRespawn);                  // flag state none
            UpdateTeamScore(player.GetTeamId());
            // only flag capture should be updated
            UpdatePlayerScore(player, ScoreType.FlagCaptures, 1);      // +1 flag captures

            // update last flag capture to be used if teamscore is equal
            SetLastFlagCapture(player.GetTeam());

            if (GetTeamScore(TeamId.Alliance) == WSGTimerOrScore.MaxTeamScore)
                winner = Team.Alliance;

            if (GetTeamScore(TeamId.Horde) == WSGTimerOrScore.MaxTeamScore)
                winner = Team.Horde;

            if (winner != 0)
            {
                UpdateWorldState(WSGWorldStates.FlagUnkAlliance, 0);
                UpdateWorldState(WSGWorldStates.FlagUnkHorde, 0);
                UpdateWorldState(WSGWorldStates.FlagStateAlliance, 1);
                UpdateWorldState(WSGWorldStates.FlagStateHorde, 1);
                UpdateWorldState(WSGWorldStates.StateTimerActive, 0);

                RewardHonorToTeam(Honor[(int)m_HonorMode][(int)WSGRewards.Win], winner);
                EndBattleground(winner);
            }
            else
            {
                _flagsTimer[GetTeamIndexByTeamId(player.GetTeam())] = WSGTimerOrScore.FlagRespawnTime;
            }
        }

        public override void EventPlayerDroppedFlag(Player player)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
            {
                // if not running, do not cast things at the dropper player (prevent spawning the "dropped" flag), neither send unnecessary messages
                // just take off the aura
                if (player.GetTeam() == Team.Alliance)
                {
                    if (!IsHordeFlagPickedup())
                        return;

                    if (GetFlagPickerGUID(TeamId.Horde) == player.GetGUID())
                    {
                        SetHordeFlagPicker(ObjectGuid.Empty);
                        player.RemoveAurasDueToSpell(WSGSpellId.WarsongFlag);
                    }
                }
                else
                {
                    if (!IsAllianceFlagPickedup())
                        return;

                    if (GetFlagPickerGUID(TeamId.Alliance) == player.GetGUID())
                    {
                        SetAllianceFlagPicker(ObjectGuid.Empty);
                        player.RemoveAurasDueToSpell(WSGSpellId.SilverwingFlag);
                    }
                }
                return;
            }

            bool set = false;

            if (player.GetTeam() == Team.Alliance)
            {
                if (!IsHordeFlagPickedup())
                    return;
                if (GetFlagPickerGUID(TeamId.Horde) == player.GetGUID())
                {
                    SetHordeFlagPicker(ObjectGuid.Empty);
                    player.RemoveAurasDueToSpell(WSGSpellId.WarsongFlag);
                    if (_flagDebuffState == 1)
                        player.RemoveAurasDueToSpell(WSGSpellId.FocusedAssault);
                    else if (_flagDebuffState == 2)
                        player.RemoveAurasDueToSpell(WSGSpellId.BrutalAssault);
                    _flagState[TeamId.Horde] = WSGFlagState.OnGround;
                    player.CastSpell(player, WSGSpellId.WarsongFlagDropped, true);
                    set = true;
                }
            }
            else
            {
                if (!IsAllianceFlagPickedup())
                    return;
                if (GetFlagPickerGUID(TeamId.Alliance) == player.GetGUID())
                {
                    SetAllianceFlagPicker(ObjectGuid.Empty);
                    player.RemoveAurasDueToSpell(WSGSpellId.SilverwingFlag);
                    if (_flagDebuffState == 1)
                        player.RemoveAurasDueToSpell(WSGSpellId.FocusedAssault);
                    else if (_flagDebuffState == 2)
                        player.RemoveAurasDueToSpell(WSGSpellId.BrutalAssault);
                    _flagState[TeamId.Alliance] = WSGFlagState.OnGround;
                    player.CastSpell(player, WSGSpellId.SilverwingFlagDropped, true);
                    set = true;
                }
            }

            if (set)
            {
                player.CastSpell(player, BattlegroundConst.SpellRecentlyDroppedFlag, true);
                UpdateFlagState(player.GetTeam(), WSGFlagState.WaitRespawn);

                if (player.GetTeam() == Team.Alliance)
                {
                    SendBroadcastText(WSGBroadcastTexts.HordeFlagDropped, ChatMsg.BgSystemHorde, player);
                    UpdateWorldState(WSGWorldStates.FlagUnkHorde, 0xFFFFFFFF);
                }
                else
                {
                    SendBroadcastText(WSGBroadcastTexts.AllianceFlagDropped, ChatMsg.BgSystemAlliance, player);
                    UpdateWorldState(WSGWorldStates.FlagUnkAlliance, 0xFFFFFFFF);
                }

                _flagsDropTimer[GetTeamIndexByTeamId(GetOtherTeam(player.GetTeam()))] = WSGTimerOrScore.FlagDropTime;
            }
        }

        public override void EventPlayerClickedOnFlag(Player player, GameObject target_obj)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            //alliance flag picked up from base
            if (player.GetTeam() == Team.Horde && GetFlagState(Team.Alliance) == WSGFlagState.OnBase
                && BgObjects[WSGObjectTypes.AFlag] == target_obj.GetGUID())
            {
                SendBroadcastText(WSGBroadcastTexts.AllianceFlagPickedUp, ChatMsg.BgSystemHorde, player);
                PlaySoundToAll(WSGSound.AllianceFlagPickedUp);
                SpawnBGObject(WSGObjectTypes.AFlag, BattlegroundConst.RespawnOneDay);
                SetAllianceFlagPicker(player.GetGUID());
                _flagState[TeamId.Alliance] = WSGFlagState.OnPlayer;
                //update world state to show correct flag carrier
                UpdateFlagState(Team.Horde, WSGFlagState.OnPlayer);
                UpdateWorldState(WSGWorldStates.FlagUnkAlliance, 1);
                player.CastSpell(player, WSGSpellId.SilverwingFlag, true);
                player.StartCriteriaTimer(CriteriaTimedTypes.SpellTarget, WSGSpellId.SilverwingFlagPicked);
                if (_flagState[1] == WSGFlagState.OnPlayer)
                    _bothFlagsKept = true;
            }

            //horde flag picked up from base
            if (player.GetTeam() == Team.Alliance && GetFlagState(Team.Horde) == WSGFlagState.OnBase
                && BgObjects[WSGObjectTypes.HFlag] == target_obj.GetGUID())
            {
                SendBroadcastText(WSGBroadcastTexts.HordeFlagPickedUp, ChatMsg.BgSystemAlliance, player);
                PlaySoundToAll(WSGSound.HordeFlagPickedUp);
                SpawnBGObject(WSGObjectTypes.HFlag, BattlegroundConst.RespawnOneDay);
                SetHordeFlagPicker(player.GetGUID());
                _flagState[TeamId.Horde] = WSGFlagState.OnPlayer;
                //update world state to show correct flag carrier
                UpdateFlagState(Team.Alliance, WSGFlagState.OnPlayer);
                UpdateWorldState(WSGWorldStates.FlagUnkHorde, 1);
                player.CastSpell(player, WSGSpellId.WarsongFlag, true);
                player.StartCriteriaTimer(CriteriaTimedTypes.SpellTarget, WSGSpellId.WarsongFlagPicked);
                if (_flagState[0] == WSGFlagState.OnPlayer)
                    _bothFlagsKept = true;
            }

            //Alliance flag on ground(not in base) (returned or picked up again from ground!)
            if (GetFlagState(Team.Alliance) == WSGFlagState.OnGround && player.IsWithinDistInMap(target_obj, 10)
                && target_obj.GetGoInfo().entry == WSGObjectEntry.AFlagGround)
            {
                if (player.GetTeam() == Team.Alliance)
                {
                    SendBroadcastText(WSGBroadcastTexts.AllianceFlagReturned, ChatMsg.BgSystemAlliance, player);
                    UpdateFlagState(Team.Horde, WSGFlagState.WaitRespawn);
                    RespawnFlag(Team.Alliance, false);
                    SpawnBGObject(WSGObjectTypes.AFlag, BattlegroundConst.RespawnImmediately);
                    PlaySoundToAll(WSGSound.FlagReturned);
                    UpdatePlayerScore(player, ScoreType.FlagReturns, 1);
                    _bothFlagsKept = false;
                }
                else
                {
                    SendBroadcastText(WSGBroadcastTexts.AllianceFlagPickedUp, ChatMsg.BgSystemHorde, player);
                    PlaySoundToAll(WSGSound.AllianceFlagPickedUp);
                    SpawnBGObject(WSGObjectTypes.AFlag, BattlegroundConst.RespawnOneDay);
                    SetAllianceFlagPicker(player.GetGUID());
                    player.CastSpell(player, WSGSpellId.SilverwingFlag, true);
                    _flagState[TeamId.Alliance] = WSGFlagState.OnPlayer;
                    UpdateFlagState(Team.Horde, WSGFlagState.OnPlayer);
                    if (_flagDebuffState == 1)
                        player.CastSpell(player, WSGSpellId.FocusedAssault, true);
                    else if (_flagDebuffState == 2)
                        player.CastSpell(player, WSGSpellId.BrutalAssault, true);
                    UpdateWorldState(WSGWorldStates.FlagUnkAlliance, 1);
                }
                //called in HandleGameObjectUseOpcode:
                //target_obj.Delete();
            }

            //Horde flag on ground(not in base) (returned or picked up again)
            if (GetFlagState(Team.Horde) == WSGFlagState.OnGround && player.IsWithinDistInMap(target_obj, 10)
                && target_obj.GetGoInfo().entry == WSGObjectEntry.HFlagGround)
            {
                if (player.GetTeam() == Team.Horde)
                {
                    SendBroadcastText(WSGBroadcastTexts.HordeFlagReturned, ChatMsg.BgSystemHorde, player);
                    UpdateFlagState(Team.Alliance, WSGFlagState.WaitRespawn);
                    RespawnFlag(Team.Horde, false);
                    SpawnBGObject(WSGObjectTypes.HFlag, BattlegroundConst.RespawnImmediately);
                    PlaySoundToAll(WSGSound.FlagReturned);
                    UpdatePlayerScore(player, ScoreType.FlagReturns, 1);
                    _bothFlagsKept = false;
                }
                else
                {
                    SendBroadcastText(WSGBroadcastTexts.HordeFlagPickedUp, ChatMsg.BgSystemAlliance, player);
                    PlaySoundToAll(WSGSound.HordeFlagPickedUp);
                    SpawnBGObject(WSGObjectTypes.HFlag, BattlegroundConst.RespawnOneDay);
                    SetHordeFlagPicker(player.GetGUID());
                    player.CastSpell(player, WSGSpellId.WarsongFlag, true);
                    _flagState[TeamId.Horde] = WSGFlagState.OnPlayer;
                    UpdateFlagState(Team.Alliance, WSGFlagState.OnPlayer);
                    if (_flagDebuffState == 1)
                        player.CastSpell(player, WSGSpellId.FocusedAssault, true);
                    else if (_flagDebuffState == 2)
                        player.CastSpell(player, WSGSpellId.BrutalAssault, true);
                    UpdateWorldState(WSGWorldStates.FlagUnkHorde, 1);
                }
                //called in HandleGameObjectUseOpcode:
                //target_obj.Delete();
            }

            player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.EnterPvpCombat);
        }

        public override void RemovePlayer(Player player, ObjectGuid guid, Team team)
        {
            // sometimes flag aura not removed :(
            if (IsAllianceFlagPickedup() && m_FlagKeepers[TeamId.Alliance] == guid)
            {
                if (!player)
                {
                    Log.outError(LogFilter.Battleground, "BattlegroundWS: Removing offline player who has the FLAG!!");
                    SetAllianceFlagPicker(ObjectGuid.Empty);
                    RespawnFlag(Team.Alliance, false);
                }
                else
                    EventPlayerDroppedFlag(player);
            }
            if (IsHordeFlagPickedup() && m_FlagKeepers[TeamId.Horde] == guid)
            {
                if (!player)
                {
                    Log.outError(LogFilter.Battleground, "BattlegroundWS: Removing offline player who has the FLAG!!");
                    SetHordeFlagPicker(ObjectGuid.Empty);
                    RespawnFlag(Team.Horde, false);
                }
                else
                    EventPlayerDroppedFlag(player);
            }
        }

        void UpdateFlagState(Team team, WSGFlagState value)
        {
            if (team == Team.Alliance)
                UpdateWorldState(WSGWorldStates.FlagStateAlliance, (uint)value);
            else
                UpdateWorldState(WSGWorldStates.FlagStateHorde, (uint)value);
        }

        void UpdateTeamScore(int team)
        {
            if (team == TeamId.Alliance)
                UpdateWorldState(WSGWorldStates.FlagCapturesAlliance, GetTeamScore(team));
            else
                UpdateWorldState(WSGWorldStates.FlagCapturesHorde, GetTeamScore(team));
        }

        public override void HandleAreaTrigger(Player player, uint trigger, bool entered)
        {
            //uint SpellId = 0;
            //uint64 buff_guid = 0;
            switch (trigger)
            {
                case 8965: // Horde Start
                case 8966: // Alliance Start
                    if (GetStatus() == BattlegroundStatus.WaitJoin && !entered)
                        TeleportPlayerToExploitLocation(player);
                    break;
                case 3686:                                          // Alliance elixir of speed spawn. Trigger not working, because located inside other areatrigger, can be replaced by IsWithinDist(object, dist) in Battleground.Update().
                    //buff_guid = BgObjects[BG_WS_OBJECT_SPEEDBUFF_1];
                    break;
                case 3687:                                          // Horde elixir of speed spawn. Trigger not working, because located inside other areatrigger, can be replaced by IsWithinDist(object, dist) in Battleground.Update().
                    //buff_guid = BgObjects[BG_WS_OBJECT_SPEEDBUFF_2];
                    break;
                case 3706:                                          // Alliance elixir of regeneration spawn
                    //buff_guid = BgObjects[BG_WS_OBJECT_REGENBUFF_1];
                    break;
                case 3708:                                          // Horde elixir of regeneration spawn
                    //buff_guid = BgObjects[BG_WS_OBJECT_REGENBUFF_2];
                    break;
                case 3707:                                          // Alliance elixir of berserk spawn
                    //buff_guid = BgObjects[BG_WS_OBJECT_BERSERKBUFF_1];
                    break;
                case 3709:                                          // Horde elixir of berserk spawn
                    //buff_guid = BgObjects[BG_WS_OBJECT_BERSERKBUFF_2];
                    break;
                case 3646:                                          // Alliance Flag spawn
                    if (_flagState[TeamId.Horde] != 0 && _flagState[TeamId.Alliance] == 0)
                        if (GetFlagPickerGUID(TeamId.Horde) == player.GetGUID())
                            EventPlayerCapturedFlag(player);
                    break;
                case 3647:                                          // Horde Flag spawn
                    if (_flagState[TeamId.Alliance] != 0 && _flagState[TeamId.Horde] == 0)
                        if (GetFlagPickerGUID(TeamId.Alliance) == player.GetGUID())
                            EventPlayerCapturedFlag(player);
                    break;
                case 3649:                                          // unk1
                case 3688:                                          // unk2
                case 4628:                                          // unk3
                case 4629:                                          // unk4
                    break;
                default:
                    base.HandleAreaTrigger(player, trigger, entered);
                    break;
            }

            //if (buff_guid)
            //    HandleTriggerBuff(buff_guid, player);
        }

        public override bool SetupBattleground()
        {
            bool result = true;
            result &= AddObject(WSGObjectTypes.AFlag, WSGObjectEntry.AFlag, 1540.423f, 1481.325f, 351.8284f, 3.089233f, 0, 0, 0.9996573f, 0.02617699f, WSGTimerOrScore.FlagRespawnTime / 1000);
            result &= AddObject(WSGObjectTypes.HFlag, WSGObjectEntry.HFlag, 916.0226f, 1434.405f, 345.413f, 0.01745329f, 0, 0, 0.008726535f, 0.9999619f, WSGTimerOrScore.FlagRespawnTime / 1000);
            if (!result)
            {
                Log.outError(LogFilter.Sql, "BgWarsongGluch: Failed to spawn flag object!");
                return false;
            }

            // buffs
            result &= AddObject(WSGObjectTypes.Speedbuff1, Buff_Entries[0], 1449.93f, 1470.71f, 342.6346f, -1.64061f, 0, 0, 0.7313537f, -0.6819983f, BattlegroundConst.BuffRespawnTime);
            result &= AddObject(WSGObjectTypes.Speedbuff2, Buff_Entries[0], 1005.171f, 1447.946f, 335.9032f, 1.64061f, 0, 0, 0.7313537f, 0.6819984f, BattlegroundConst.BuffRespawnTime);
            result &= AddObject(WSGObjectTypes.Regenbuff1, Buff_Entries[1], 1317.506f, 1550.851f, 313.2344f, -0.2617996f, 0, 0, 0.1305263f, -0.9914448f, BattlegroundConst.BuffRespawnTime);
            result &= AddObject(WSGObjectTypes.Regenbuff2, Buff_Entries[1], 1110.451f, 1353.656f, 316.5181f, -0.6806787f, 0, 0, 0.333807f, -0.9426414f, BattlegroundConst.BuffRespawnTime);
            result &= AddObject(WSGObjectTypes.Berserkbuff1, Buff_Entries[2], 1320.09f, 1378.79f, 314.7532f, 1.186824f, 0, 0, 0.5591929f, 0.8290376f, BattlegroundConst.BuffRespawnTime);
            result &= AddObject(WSGObjectTypes.Berserkbuff2, Buff_Entries[2], 1139.688f, 1560.288f, 306.8432f, -2.443461f, 0, 0, 0.9396926f, -0.3420201f, BattlegroundConst.BuffRespawnTime);
            if (!result)
            {
                Log.outError(LogFilter.Sql, "BgWarsongGluch: Failed to spawn buff object!");
                return false;
            }

            // alliance gates
            result &= AddObject(WSGObjectTypes.DoorA1, WSGObjectEntry.DoorA1, 1503.335f, 1493.466f, 352.1888f, 3.115414f, 0, 0, 0.9999143f, 0.01308903f, BattlegroundConst.RespawnImmediately);
            result &= AddObject(WSGObjectTypes.DoorA2, WSGObjectEntry.DoorA2, 1492.478f, 1457.912f, 342.9689f, 3.115414f, 0, 0, 0.9999143f, 0.01308903f, BattlegroundConst.RespawnImmediately);
            result &= AddObject(WSGObjectTypes.DoorA3, WSGObjectEntry.DoorA3, 1468.503f, 1494.357f, 351.8618f, 3.115414f, 0, 0, 0.9999143f, 0.01308903f, BattlegroundConst.RespawnImmediately);
            result &= AddObject(WSGObjectTypes.DoorA4, WSGObjectEntry.DoorA4, 1471.555f, 1458.778f, 362.6332f, 3.115414f, 0, 0, 0.9999143f, 0.01308903f, BattlegroundConst.RespawnImmediately);
            result &= AddObject(WSGObjectTypes.DoorA5, WSGObjectEntry.DoorA5, 1492.347f, 1458.34f, 342.3712f, -0.03490669f, 0, 0, 0.01745246f, -0.9998477f, BattlegroundConst.RespawnImmediately);
            result &= AddObject(WSGObjectTypes.DoorA6, WSGObjectEntry.DoorA6, 1503.466f, 1493.367f, 351.7352f, -0.03490669f, 0, 0, 0.01745246f, -0.9998477f, BattlegroundConst.RespawnImmediately);
            // horde gates
            result &= AddObject(WSGObjectTypes.DoorH1, WSGObjectEntry.DoorH1, 949.1663f, 1423.772f, 345.6241f, -0.5756807f, -0.01673368f, -0.004956111f, -0.2839723f, 0.9586737f, BattlegroundConst.RespawnImmediately);
            result &= AddObject(WSGObjectTypes.DoorH2, WSGObjectEntry.DoorH2, 953.0507f, 1459.842f, 340.6526f, -1.99662f, -0.1971825f, 0.1575096f, -0.8239487f, 0.5073641f, BattlegroundConst.RespawnImmediately);
            result &= AddObject(WSGObjectTypes.DoorH3, WSGObjectEntry.DoorH3, 949.9523f, 1422.751f, 344.9273f, 0.0f, 0, 0, 0, 1, BattlegroundConst.RespawnImmediately);
            result &= AddObject(WSGObjectTypes.DoorH4, WSGObjectEntry.DoorH4, 950.7952f, 1459.583f, 342.1523f, 0.05235988f, 0, 0, 0.02617695f, 0.9996573f, BattlegroundConst.RespawnImmediately);
            if (!result)
            {
                Log.outError(LogFilter.Sql, "BgWarsongGluch: Failed to spawn door object Battleground not created!");
                return false;
            }

            WorldSafeLocsRecord sg = CliDB.WorldSafeLocsStorage.LookupByKey(WSGGraveyards.MainAlliance);
            if (sg == null || !AddSpiritGuide(WSGCreatureTypes.SpiritMainAlliance, sg.Loc.X, sg.Loc.Y, sg.Loc.Z, 3.124139f, TeamId.Alliance))
            {
                Log.outError(LogFilter.Sql, "BgWarsongGluch: Failed to spawn Alliance spirit guide! Battleground not created!");
                return false;
            }

            sg = CliDB.WorldSafeLocsStorage.LookupByKey(WSGGraveyards.MainHorde);
            if (sg == null || !AddSpiritGuide(WSGCreatureTypes.SpiritMainHorde, sg.Loc.X, sg.Loc.Y, sg.Loc.Z, 3.193953f, TeamId.Horde))
            {
                Log.outError(LogFilter.Sql, "BgWarsongGluch: Failed to spawn Horde spirit guide! Battleground not created!");
                return false;
            }

            return true;
        }

        public override void Reset()
        {
            //call parent's class reset
            base.Reset();

            m_FlagKeepers[TeamId.Alliance].Clear();
            m_FlagKeepers[TeamId.Horde].Clear();
            m_DroppedFlagGUID[TeamId.Alliance] = ObjectGuid.Empty;
            m_DroppedFlagGUID[TeamId.Horde] = ObjectGuid.Empty;
            _flagState[TeamId.Alliance] = WSGFlagState.OnBase;
            _flagState[TeamId.Horde] = WSGFlagState.OnBase;
            m_TeamScores[TeamId.Alliance] = 0;
            m_TeamScores[TeamId.Horde] = 0;

            if (Global.BattlegroundMgr.IsBGWeekend(GetTypeID()))
            {
                m_ReputationCapture = 45;
                m_HonorWinKills = 3;
                m_HonorEndKills = 4;
            }
            else
            {
                m_ReputationCapture = 35;
                m_HonorWinKills = 1;
                m_HonorEndKills = 2;
            }
            _minutesElapsed = 0;
            _lastFlagCaptureTeam = 0;
            _bothFlagsKept = false;
            _flagDebuffState = 0;
            _flagSpellForceTimer = 0;
            _flagsDropTimer[TeamId.Alliance] = 0;
            _flagsDropTimer[TeamId.Horde] = 0;
            _flagsTimer[TeamId.Alliance] = 0;
            _flagsTimer[TeamId.Horde] = 0;
        }

        public override void EndBattleground(Team winner)
        {
            // Win reward
            if (winner == Team.Alliance)
                RewardHonorToTeam(GetBonusHonorFromKill(m_HonorWinKills), Team.Alliance);
            if (winner == Team.Horde)
                RewardHonorToTeam(GetBonusHonorFromKill(m_HonorWinKills), Team.Horde);
            // Complete map_end rewards (even if no team wins)
            RewardHonorToTeam(GetBonusHonorFromKill(m_HonorEndKills), Team.Alliance);
            RewardHonorToTeam(GetBonusHonorFromKill(m_HonorEndKills), Team.Horde);

            base.EndBattleground(winner);
        }

        public override void HandleKillPlayer(Player victim, Player killer)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            EventPlayerDroppedFlag(victim);

            base.HandleKillPlayer(victim, killer);
        }

        public override bool UpdatePlayerScore(Player player, ScoreType type, uint value, bool doAddHonor = true)
        {
            if (!base.UpdatePlayerScore(player, type, value, doAddHonor))
                return false;

            switch (type)
            {
                case ScoreType.FlagCaptures:                           // flags captured
                    player.UpdateCriteria(CriteriaTypes.BgObjectiveCapture, 42);
                    break;
                case ScoreType.FlagReturns:                            // flags returned
                    player.UpdateCriteria(CriteriaTypes.BgObjectiveCapture, 44);
                    break;
                default:
                    break;
            }
            return true;
        }

        public override WorldSafeLocsRecord GetClosestGraveYard(Player player)
        {
            //if status in progress, it returns main graveyards with spiritguides
            //else it will return the graveyard in the flagroom - this is especially good
            //if a player dies in preparation phase - then the player can't cheat
            //and teleport to the graveyard outside the flagroom
            //and start running around, while the doors are still closed
            if (player.GetTeam() == Team.Alliance)
            {
                if (GetStatus() == BattlegroundStatus.InProgress)
                    return CliDB.WorldSafeLocsStorage.LookupByKey(WSGGraveyards.MainAlliance);
                else
                    return CliDB.WorldSafeLocsStorage.LookupByKey(WSGGraveyards.FlagRoomAlliance);
            }
            else
            {
                if (GetStatus() == BattlegroundStatus.InProgress)
                    return CliDB.WorldSafeLocsStorage.LookupByKey(WSGGraveyards.MainHorde);
                else
                    return CliDB.WorldSafeLocsStorage.LookupByKey(WSGGraveyards.FlagRoomHorde);
            }
        }

        public override WorldSafeLocsRecord GetExploitTeleportLocation(Team team)
        {
            return CliDB.WorldSafeLocsStorage.LookupByKey(team == Team.Alliance ? ExploitTeleportLocationAlliance : ExploitTeleportLocationHorde);
        }

        public override void FillInitialWorldStates(InitWorldStates packet)
        {
            packet.AddState(WSGWorldStates.FlagCapturesAlliance, (int)GetTeamScore(TeamId.Alliance));
            packet.AddState(WSGWorldStates.FlagCapturesHorde, (int)GetTeamScore(TeamId.Horde));

            if (_flagState[TeamId.Alliance] == WSGFlagState.OnGround)
                packet.AddState(WSGWorldStates.FlagUnkAlliance, -1);
            else if (_flagState[TeamId.Alliance] == WSGFlagState.OnPlayer)
                packet.AddState(WSGWorldStates.FlagUnkAlliance, 1);
            else
                packet.AddState(WSGWorldStates.FlagUnkAlliance, 0);

            if (_flagState[TeamId.Horde] == WSGFlagState.OnGround)
                packet.AddState(WSGWorldStates.FlagUnkHorde, -1);
            else if (_flagState[TeamId.Horde] == WSGFlagState.OnPlayer)
                packet.AddState(WSGWorldStates.FlagUnkHorde, 1);
            else
                packet.AddState(WSGWorldStates.FlagUnkHorde, 0);

            packet.AddState(WSGWorldStates.FlagCapturesMax, (int)WSGTimerOrScore.MaxTeamScore);

            if (GetStatus() == BattlegroundStatus.InProgress)
            {
                packet.AddState(WSGWorldStates.StateTimerActive, 1);
                packet.AddState(WSGWorldStates.StateTimer, 25 - _minutesElapsed);
            }
            else
                packet.AddState(WSGWorldStates.StateTimerActive, 0);

            if (_flagState[TeamId.Horde] == WSGFlagState.OnPlayer)
                packet.AddState(WSGWorldStates.FlagStateHorde, 2);
            else
                packet.AddState(WSGWorldStates.FlagStateHorde, 1);

            if (_flagState[TeamId.Alliance] == WSGFlagState.OnPlayer)
                packet.AddState(WSGWorldStates.FlagStateAlliance, 2);
            else
                packet.AddState(WSGWorldStates.FlagStateAlliance, 1);
        }

        public override Team GetPrematureWinner()
        {
            if (GetTeamScore(TeamId.Alliance) > GetTeamScore(TeamId.Horde))
                return Team.Alliance;
            else if (GetTeamScore(TeamId.Horde) > GetTeamScore(TeamId.Alliance))
                return Team.Horde;

            return base.GetPrematureWinner();
        }

        public override bool CheckAchievementCriteriaMeet(uint criteriaId, Player player, Unit target, uint miscValue)
        {
            switch (criteriaId)
            {
                case (uint)BattlegroundCriteriaId.SaveTheDay:
                    if (target)
                    {
                        Player playerTarget = target.ToPlayer();
                        if (playerTarget)
                            return GetFlagState(playerTarget.GetTeam()) == WSGFlagState.OnBase;
                    }
                    return false;
            }

            return base.CheckAchievementCriteriaMeet(criteriaId, player, target, miscValue);
        }

        public override ObjectGuid GetFlagPickerGUID(int team = -1)
        {
            if (team == TeamId.Alliance || team == TeamId.Horde)
                return m_FlagKeepers[team];

            return ObjectGuid.Empty;
        }

        void SetAllianceFlagPicker(ObjectGuid guid) { m_FlagKeepers[TeamId.Alliance] = guid; }
        void SetHordeFlagPicker(ObjectGuid guid) { m_FlagKeepers[TeamId.Horde] = guid; }
        bool IsAllianceFlagPickedup() { return !m_FlagKeepers[TeamId.Alliance].IsEmpty(); }
        bool IsHordeFlagPickedup() { return !m_FlagKeepers[TeamId.Horde].IsEmpty(); }
        WSGFlagState GetFlagState(Team team) { return _flagState[GetTeamIndexByTeamId(team)]; }

        void SetLastFlagCapture(Team team) { _lastFlagCaptureTeam = (uint)team; }
        public override void SetDroppedFlagGUID(ObjectGuid guid, int team = -1)
        {
            if (team == TeamId.Alliance || team == TeamId.Horde)
                m_DroppedFlagGUID[team] = guid;

        }
        ObjectGuid GetDroppedFlagGUID(Team team) { return m_DroppedFlagGUID[GetTeamIndexByTeamId(team)]; }

        void AddPoint(Team team, uint Points = 1) { m_TeamScores[GetTeamIndexByTeamId(team)] += Points; }
        void SetTeamPoint(Team team, uint Points = 0) { m_TeamScores[GetTeamIndexByTeamId(team)] = Points; }
        void RemovePoint(Team team, uint Points = 1) { m_TeamScores[GetTeamIndexByTeamId(team)] -= Points; }

        ObjectGuid[] m_FlagKeepers = new ObjectGuid[2];                            // 0 - alliance, 1 - horde
        ObjectGuid[] m_DroppedFlagGUID = new ObjectGuid[2];
        WSGFlagState[] _flagState = new WSGFlagState[2];                               // for checking flag state
        int[] _flagsTimer = new int[2];
        int[] _flagsDropTimer = new int[2];
        uint _lastFlagCaptureTeam;                       // Winner is based on this if score is equal

        uint m_ReputationCapture;
        uint m_HonorWinKills;
        uint m_HonorEndKills;
        int _flagSpellForceTimer;
        bool _bothFlagsKept;
        byte _flagDebuffState;                            // 0 - no debuffs, 1 - focused assault, 2 - brutal assault
        byte _minutesElapsed;

        uint[][] Honor =
        {
            new uint[] {20, 40, 40 }, // normal honor
            new uint[] { 60, 40, 80}  // holiday
        };
        const uint ExploitTeleportLocationAlliance = 3784;
        const uint ExploitTeleportLocationHorde = 3785;
    }

    class BattlegroundWGScore : BattlegroundScore
    {
        public BattlegroundWGScore(ObjectGuid playerGuid, Team team) : base(playerGuid, team) { }

        public override void UpdateScore(ScoreType type, uint value)
        {
            switch (type)
            {
                case ScoreType.FlagCaptures:   // Flags captured
                    FlagCaptures += value;
                    break;
                case ScoreType.FlagReturns:    // Flags returned
                    FlagReturns += value;
                    break;
                default:
                    base.UpdateScore(type, value);
                    break;
            }
        }

        public override void BuildPvPLogPlayerDataPacket(out PVPLogData.PlayerData playerData)
        {
            base.BuildPvPLogPlayerDataPacket(out playerData);

            playerData.Stats.Add(FlagCaptures);
            playerData.Stats.Add(FlagReturns);
        }

        public override uint GetAttr1() { return FlagCaptures; }
        public override uint GetAttr2() { return FlagReturns; }

        uint FlagCaptures;
        uint FlagReturns;
    }

    #region Constants
    enum WSGRewards
    {
        Win = 0,
        FlapCap,
        MapComplete,
        RewardNum
    }
    enum WSGFlagState
    {
        OnBase = 0,
        WaitRespawn = 1,
        OnPlayer = 2,
        OnGround = 3
    }

    struct WSGObjectTypes
    {
        public const int DoorA1 = 0;
        public const int DoorA2 = 1;
        public const int DoorA3 = 2;
        public const int DoorA4 = 3;
        public const int DoorA5 = 4;
        public const int DoorA6 = 5;
        public const int DoorH1 = 6;
        public const int DoorH2 = 7;
        public const int DoorH3 = 8;
        public const int DoorH4 = 9;
        public const int AFlag = 10;
        public const int HFlag = 11;
        public const int Speedbuff1 = 12;
        public const int Speedbuff2 = 13;
        public const int Regenbuff1 = 14;
        public const int Regenbuff2 = 15;
        public const int Berserkbuff1 = 16;
        public const int Berserkbuff2 = 17;
        public const int Max = 18;
    }
    public sealed class WSGObjectEntry
    {
        public const uint DoorA1 = 179918;
        public const uint DoorA2 = 179919;
        public const uint DoorA3 = 179920;
        public const uint DoorA4 = 179921;
        public const uint DoorA5 = 180322;
        public const uint DoorA6 = 180322;
        public const uint DoorH1 = 179916;
        public const uint DoorH2 = 179917;
        public const uint DoorH3 = 180322;
        public const uint DoorH4 = 180322;
        public const uint AFlag = 179830;
        public const uint HFlag = 179831;
        public const uint AFlagGround = 179785;
        public const uint HFlagGround = 179786;
    }

    struct WSGCreatureTypes
    {
        public const int SpiritMainAlliance = 0;
        public const int SpiritMainHorde = 1;

        public const int Max = 2;
    }

    struct WSGWorldStates
    {
        public const uint FlagUnkAlliance = 1545;
        public const uint FlagUnkHorde = 1546;
        //    FlagUnk                      = 1547;
        public const uint FlagCapturesAlliance = 1581;
        public const uint FlagCapturesHorde = 1582;
        public const uint FlagCapturesMax = 1601;
        public const uint FlagStateHorde = 2338;
        public const uint FlagStateAlliance = 2339;
        public const uint StateTimer = 4248;
        public const uint StateTimerActive = 4247;
    }

    struct WSGSpellId
    {
        public const uint WarsongFlag = 23333;
        public const uint WarsongFlagDropped = 23334;
        public const uint WarsongFlagPicked = 61266;    // Fake Spell; Does Not Exist But Used As Timer Start Event
        public const uint SilverwingFlag = 23335;
        public const uint SilverwingFlagDropped = 23336;
        public const uint SilverwingFlagPicked = 61265;    // Fake Spell; Does Not Exist But Used As Timer Start Event
        public const uint FocusedAssault = 46392;
        public const uint BrutalAssault = 46393;
    }

    struct WSGTimerOrScore
    {
        public const uint MaxTeamScore = 3;
        public const int FlagRespawnTime = 23000;
        public const int FlagDropTime = 10000;
        public const uint SpellForceTime = 600000;
        public const uint SpellBrutalTime = 900000;
    }

    struct WSGGraveyards
    {
        public const uint FlagRoomAlliance = 769;
        public const uint FlagRoomHorde = 770;
        public const uint MainAlliance = 771;
        public const uint MainHorde = 772;
    }

    struct WSGSound
    {
        public const uint FlagCapturedAlliance = 8173;
        public const uint FlagCapturedHorde = 8213;
        public const uint FlagPlaced = 8232;
        public const uint FlagReturned = 8192;
        public const uint HordeFlagPickedUp = 8212;
        public const uint AllianceFlagPickedUp = 8174;
        public const uint FlagsRespawned = 8232;
    }

    struct WSGBroadcastTexts
    {
        public const uint StartOneMinute = 10015;
        public const uint StartHalfMinute = 10016;
        public const uint BattleHasBegun = 10014;

        public const uint CapturedHordeFlag = 9801;
        public const uint CapturedAllianceFlag = 9802;
        public const uint FlagsPlaced = 9803;
        public const uint AllianceFlagPickedUp = 9804;
        public const uint AllianceFlagDropped = 9805;
        public const uint HordeFlagPickedUp = 9807;
        public const uint HordeFlagDropped = 9806;
        public const uint AllianceFlagReturned = 9808;
        public const uint HordeFlagReturned = 9809;
    }
    #endregion
}
