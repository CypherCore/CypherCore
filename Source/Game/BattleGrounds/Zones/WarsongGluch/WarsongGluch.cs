// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.BattleGrounds.Zones.WarsongGluch
{
    internal class BgWarsongGluch : Battleground
    {
        private const uint EXPLOIT_TELEPORT_LOCATION_ALLIANCE = 3784;
        private const uint EXPLOIT_TELEPORT_LOCATION_HORDE = 3785;
        private readonly ObjectGuid[] _droppedFlagGUID = new ObjectGuid[2];

        private readonly ObjectGuid[] _flagKeepers = new ObjectGuid[2]; // 0 - alliance, 1 - horde
        private readonly int[] _flagsDropTimer = new int[2];
        private readonly WSGFlagState[] _flagState = new WSGFlagState[2]; // for checking flag State
        private readonly int[] _flagsTimer = new int[2];

        private readonly uint[][] _honor =
        {
            new uint[]
            {
                20, 40, 40
            }, // normal honor
			new uint[]
            {
                60, 40, 80
            } // holiday
		};

        private bool _bothFlagsKept;
        private byte _flagDebuffState; // 0 - no debuffs, 1 - focused assault, 2 - brutal assault
        private int _flagSpellForceTimer;
        private uint _honorEndKills;
        private uint _honorWinKills;
        private uint _lastFlagCaptureTeam; // Winner is based on this if score is equal

        private uint _reputationCapture;

        public BgWarsongGluch(BattlegroundTemplate battlegroundTemplate) : base(battlegroundTemplate)
        {
            BgObjects = new ObjectGuid[WSGObjectTypes.MAX];
            BgCreatures = new ObjectGuid[WSGCreatureTypes.MAX];

            StartMessageIds[BattlegroundConst.EventIdSecond] = WSGBroadcastTexts.START_ONE_MINUTE;
            StartMessageIds[BattlegroundConst.EventIdThird] = WSGBroadcastTexts.START_HALF_MINUTE;
            StartMessageIds[BattlegroundConst.EventIdFourth] = WSGBroadcastTexts.BATTLE_HAS_BEGUN;
        }

        public override void PostUpdateImpl(uint diff)
        {
            if (GetStatus() == BattlegroundStatus.InProgress)
            {
                if (GetElapsedTime() >= 17 * Time.Minute * Time.InMilliseconds)
                {
                    if (GetTeamScore(TeamId.Alliance) == 0)
                    {
                        if (GetTeamScore(TeamId.Horde) == 0) // No one scored - result is tie
                            EndBattleground(Team.Other);
                        else // Horde has more points and thus wins
                            EndBattleground(Team.Horde);
                    }
                    else if (GetTeamScore(TeamId.Horde) == 0)
                    {
                        EndBattleground(Team.Alliance); // Alliance has > 0, Horde has 0, alliance wins
                    }
                    else if (GetTeamScore(TeamId.Horde) == GetTeamScore(TeamId.Alliance)) // Team score equal, winner is team that scored the last flag
                    {
                        EndBattleground((Team)_lastFlagCaptureTeam);
                    }
                    else if (GetTeamScore(TeamId.Horde) > GetTeamScore(TeamId.Alliance)) // Last but not least, check who has the higher score
                    {
                        EndBattleground(Team.Horde);
                    }
                    else
                    {
                        EndBattleground(Team.Alliance);
                    }
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

                    if (_flagDebuffState == 0 &&
                        _flagSpellForceTimer >= 10 * Time.Minute * Time.InMilliseconds) //10 minutes
                    {
                        // Apply Stage 1 (Focused Assault)
                        Player player = Global.ObjAccessor.FindPlayer(_flagKeepers[0]);

                        if (player)
                            player.CastSpell(player, WSGSpellId.FOCUSED_ASSAULT, true);

                        player = Global.ObjAccessor.FindPlayer(_flagKeepers[1]);

                        if (player)
                            player.CastSpell(player, WSGSpellId.FOCUSED_ASSAULT, true);

                        _flagDebuffState = 1;
                    }
                    else if (_flagDebuffState == 1 &&
                             _flagSpellForceTimer >= 900000) //15 minutes
                    {
                        // Apply Stage 2 (Brutal Assault)
                        Player player = Global.ObjAccessor.FindPlayer(_flagKeepers[0]);

                        if (player)
                        {
                            player.RemoveAurasDueToSpell(WSGSpellId.FOCUSED_ASSAULT);
                            player.CastSpell(player, WSGSpellId.BRUTAL_ASSAULT, true);
                        }

                        player = Global.ObjAccessor.FindPlayer(_flagKeepers[1]);

                        if (player)
                        {
                            player.RemoveAurasDueToSpell(WSGSpellId.FOCUSED_ASSAULT);
                            player.CastSpell(player, WSGSpellId.BRUTAL_ASSAULT, true);
                        }

                        _flagDebuffState = 2;
                    }
                }
                else if ((_flagState[TeamId.Alliance] == WSGFlagState.OnBase || _flagState[TeamId.Alliance] == WSGFlagState.WaitRespawn) &&
                         (_flagState[TeamId.Horde] == WSGFlagState.OnBase || _flagState[TeamId.Horde] == WSGFlagState.WaitRespawn))
                {
                    // Both Flags are in base or awaiting respawn.
                    // Remove assault debuffs, reset timers

                    Player player = Global.ObjAccessor.FindPlayer(_flagKeepers[0]);

                    if (player)
                    {
                        player.RemoveAurasDueToSpell(WSGSpellId.FOCUSED_ASSAULT);
                        player.RemoveAurasDueToSpell(WSGSpellId.BRUTAL_ASSAULT);
                    }

                    player = Global.ObjAccessor.FindPlayer(_flagKeepers[1]);

                    if (player)
                    {
                        player.RemoveAurasDueToSpell(WSGSpellId.FOCUSED_ASSAULT);
                        player.RemoveAurasDueToSpell(WSGSpellId.BRUTAL_ASSAULT);
                    }

                    _flagSpellForceTimer = 0; //reset timer.
                    _flagDebuffState = 0;
                }
            }
        }

        public override void StartingEventCloseDoors()
        {
            for (int i = WSGObjectTypes.DOOR_A_1; i <= WSGObjectTypes.DOOR_H_4; ++i)
            {
                DoorClose(i);
                SpawnBGObject(i, BattlegroundConst.RespawnImmediately);
            }

            for (int i = WSGObjectTypes.A_FLAG; i <= WSGObjectTypes.BERSERKBUFF_2; ++i)
                SpawnBGObject(i, BattlegroundConst.RespawnOneDay);
        }

        public override void StartingEventOpenDoors()
        {
            for (int i = WSGObjectTypes.DOOR_A_1; i <= WSGObjectTypes.DOOR_A_6; ++i)
                DoorOpen(i);

            for (int i = WSGObjectTypes.DOOR_H_1; i <= WSGObjectTypes.DOOR_H_4; ++i)
                DoorOpen(i);

            for (int i = WSGObjectTypes.A_FLAG; i <= WSGObjectTypes.BERSERKBUFF_2; ++i)
                SpawnBGObject(i, BattlegroundConst.RespawnImmediately);

            SpawnBGObject(WSGObjectTypes.DOOR_A_5, BattlegroundConst.RespawnOneDay);
            SpawnBGObject(WSGObjectTypes.DOOR_A_6, BattlegroundConst.RespawnOneDay);
            SpawnBGObject(WSGObjectTypes.DOOR_H_3, BattlegroundConst.RespawnOneDay);
            SpawnBGObject(WSGObjectTypes.DOOR_H_4, BattlegroundConst.RespawnOneDay);

            UpdateWorldState(WSGWorldStates.STATE_TIMER_ACTIVE, 1);
            UpdateWorldState(WSGWorldStates.STATE_TIMER, (int)(GameTime.GetGameTime() + 15 * Time.Minute));

            // players joining later are not eligibles
            TriggerGameEvent(8563);
        }

        public override void AddPlayer(Player player)
        {
            bool isInBattleground = IsPlayerInBattleground(player.GetGUID());
            base.AddPlayer(player);

            if (!isInBattleground)
                PlayerScores[player.GetGUID()] = new BattlegroundWGScore(player.GetGUID(), player.GetBGTeam());
        }

        public override void EventPlayerDroppedFlag(Player player)
        {
            Team team = GetPlayerTeam(player.GetGUID());

            if (GetStatus() != BattlegroundStatus.InProgress)
            {
                // if not running, do not cast things at the dropper player (prevent spawning the "dropped" flag), neither send unnecessary messages
                // just take off the aura
                if (team == Team.Alliance)
                {
                    if (!IsHordeFlagPickedup())
                        return;

                    if (GetFlagPickerGUID(TeamId.Horde) == player.GetGUID())
                    {
                        SetHordeFlagPicker(ObjectGuid.Empty);
                        player.RemoveAurasDueToSpell(WSGSpellId.WARSONG_FLAG);
                    }
                }
                else
                {
                    if (!IsAllianceFlagPickedup())
                        return;

                    if (GetFlagPickerGUID(TeamId.Alliance) == player.GetGUID())
                    {
                        SetAllianceFlagPicker(ObjectGuid.Empty);
                        player.RemoveAurasDueToSpell(WSGSpellId.SILVERWING_FLAG);
                    }
                }

                return;
            }

            bool set = false;

            if (team == Team.Alliance)
            {
                if (!IsHordeFlagPickedup())
                    return;

                if (GetFlagPickerGUID(TeamId.Horde) == player.GetGUID())
                {
                    SetHordeFlagPicker(ObjectGuid.Empty);
                    player.RemoveAurasDueToSpell(WSGSpellId.WARSONG_FLAG);

                    if (_flagDebuffState == 1)
                        player.RemoveAurasDueToSpell(WSGSpellId.FOCUSED_ASSAULT);
                    else if (_flagDebuffState == 2)
                        player.RemoveAurasDueToSpell(WSGSpellId.BRUTAL_ASSAULT);

                    _flagState[TeamId.Horde] = WSGFlagState.OnGround;
                    player.CastSpell(player, WSGSpellId.WARSONG_FLAG_DROPPED, true);
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
                    player.RemoveAurasDueToSpell(WSGSpellId.SILVERWING_FLAG);

                    if (_flagDebuffState == 1)
                        player.RemoveAurasDueToSpell(WSGSpellId.FOCUSED_ASSAULT);
                    else if (_flagDebuffState == 2)
                        player.RemoveAurasDueToSpell(WSGSpellId.BRUTAL_ASSAULT);

                    _flagState[TeamId.Alliance] = WSGFlagState.OnGround;
                    player.CastSpell(player, WSGSpellId.SILVERWING_FLAG_DROPPED, true);
                    set = true;
                }
            }

            if (set)
            {
                player.CastSpell(player, BattlegroundConst.SpellRecentlyDroppedFlag, true);
                UpdateFlagState(team, WSGFlagState.OnGround);

                if (team == Team.Alliance)
                    SendBroadcastText(WSGBroadcastTexts.HORDE_FLAG_DROPPED, ChatMsg.BgSystemHorde, player);
                else
                    SendBroadcastText(WSGBroadcastTexts.ALLIANCE_FLAG_DROPPED, ChatMsg.BgSystemAlliance, player);

                _flagsDropTimer[GetTeamIndexByTeamId(GetOtherTeam(team))] = WSGTimerOrScore.FLAG_DROP_TIME;
            }
        }

        public override void EventPlayerClickedOnFlag(Player player, GameObject target_obj)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            Team team = GetPlayerTeam(player.GetGUID());

            //alliance flag picked up from base
            if (team == Team.Horde &&
                GetFlagState(Team.Alliance) == WSGFlagState.OnBase &&
                BgObjects[WSGObjectTypes.A_FLAG] == target_obj.GetGUID())
            {
                SendBroadcastText(WSGBroadcastTexts.ALLIANCE_FLAG_PICKED_UP, ChatMsg.BgSystemHorde, player);
                PlaySoundToAll(WSGSound.ALLIANCE_FLAG_PICKED_UP);
                SpawnBGObject(WSGObjectTypes.A_FLAG, BattlegroundConst.RespawnOneDay);
                SetAllianceFlagPicker(player.GetGUID());
                _flagState[TeamId.Alliance] = WSGFlagState.OnPlayer;
                //update world State to show correct flag carrier
                UpdateFlagState(Team.Horde, WSGFlagState.OnPlayer);
                player.CastSpell(player, WSGSpellId.SILVERWING_FLAG, true);
                player.StartCriteriaTimer(CriteriaStartEvent.BeSpellTarget, WSGSpellId.SILVERWING_FLAG_PICKED);

                if (_flagState[1] == WSGFlagState.OnPlayer)
                    _bothFlagsKept = true;

                if (_flagDebuffState == 1)
                    player.CastSpell(player, WSGSpellId.FOCUSED_ASSAULT, true);
                else if (_flagDebuffState == 2)
                    player.CastSpell(player, WSGSpellId.BRUTAL_ASSAULT, true);
            }

            //horde flag picked up from base
            if (team == Team.Alliance &&
                GetFlagState(Team.Horde) == WSGFlagState.OnBase &&
                BgObjects[WSGObjectTypes.H_FLAG] == target_obj.GetGUID())
            {
                SendBroadcastText(WSGBroadcastTexts.HORDE_FLAG_PICKED_UP, ChatMsg.BgSystemAlliance, player);
                PlaySoundToAll(WSGSound.HORDE_FLAG_PICKED_UP);
                SpawnBGObject(WSGObjectTypes.H_FLAG, BattlegroundConst.RespawnOneDay);
                SetHordeFlagPicker(player.GetGUID());
                _flagState[TeamId.Horde] = WSGFlagState.OnPlayer;
                //update world State to show correct flag carrier
                UpdateFlagState(Team.Alliance, WSGFlagState.OnPlayer);
                player.CastSpell(player, WSGSpellId.WARSONG_FLAG, true);
                player.StartCriteriaTimer(CriteriaStartEvent.BeSpellTarget, WSGSpellId.WARSONG_FLAG_PICKED);

                if (_flagState[0] == WSGFlagState.OnPlayer)
                    _bothFlagsKept = true;

                if (_flagDebuffState == 1)
                    player.CastSpell(player, WSGSpellId.FOCUSED_ASSAULT, true);
                else if (_flagDebuffState == 2)
                    player.CastSpell(player, WSGSpellId.BRUTAL_ASSAULT, true);
            }

            //Alliance flag on ground(not in base) (returned or picked up again from ground!)
            if (GetFlagState(Team.Alliance) == WSGFlagState.OnGround &&
                player.IsWithinDistInMap(target_obj, 10) &&
                target_obj.GetGoInfo().entry == WSGObjectEntry.A_FLAG_GROUND)
            {
                if (team == Team.Alliance)
                {
                    SendBroadcastText(WSGBroadcastTexts.ALLIANCE_FLAG_RETURNED, ChatMsg.BgSystemAlliance, player);
                    UpdateFlagState(Team.Horde, WSGFlagState.WaitRespawn);
                    RespawnFlag(Team.Alliance, false);
                    SpawnBGObject(WSGObjectTypes.A_FLAG, BattlegroundConst.RespawnImmediately);
                    PlaySoundToAll(WSGSound.FLAG_RETURNED);
                    UpdatePlayerScore(player, ScoreType.FlagReturns, 1);
                    _bothFlagsKept = false;

                    HandleFlagRoomCapturePoint(TeamId.Horde); // Check Horde flag if it is in capture zone; if so, capture it
                }
                else
                {
                    SendBroadcastText(WSGBroadcastTexts.ALLIANCE_FLAG_PICKED_UP, ChatMsg.BgSystemHorde, player);
                    PlaySoundToAll(WSGSound.ALLIANCE_FLAG_PICKED_UP);
                    SpawnBGObject(WSGObjectTypes.A_FLAG, BattlegroundConst.RespawnOneDay);
                    SetAllianceFlagPicker(player.GetGUID());
                    player.CastSpell(player, WSGSpellId.SILVERWING_FLAG, true);
                    _flagState[TeamId.Alliance] = WSGFlagState.OnPlayer;
                    UpdateFlagState(Team.Horde, WSGFlagState.OnPlayer);

                    if (_flagDebuffState == 1)
                        player.CastSpell(player, WSGSpellId.FOCUSED_ASSAULT, true);
                    else if (_flagDebuffState == 2)
                        player.CastSpell(player, WSGSpellId.BRUTAL_ASSAULT, true);
                }
                //called in HandleGameObjectUseOpcode:
                //target_obj.Delete();
            }

            //Horde flag on ground(not in base) (returned or picked up again)
            if (GetFlagState(Team.Horde) == WSGFlagState.OnGround &&
                player.IsWithinDistInMap(target_obj, 10) &&
                target_obj.GetGoInfo().entry == WSGObjectEntry.H_FLAG_GROUND)
            {
                if (team == Team.Horde)
                {
                    SendBroadcastText(WSGBroadcastTexts.HORDE_FLAG_RETURNED, ChatMsg.BgSystemHorde, player);
                    UpdateFlagState(Team.Alliance, WSGFlagState.WaitRespawn);
                    RespawnFlag(Team.Horde, false);
                    SpawnBGObject(WSGObjectTypes.H_FLAG, BattlegroundConst.RespawnImmediately);
                    PlaySoundToAll(WSGSound.FLAG_RETURNED);
                    UpdatePlayerScore(player, ScoreType.FlagReturns, 1);
                    _bothFlagsKept = false;

                    HandleFlagRoomCapturePoint(TeamId.Alliance); // Check Alliance flag if it is in capture zone; if so, capture it
                }
                else
                {
                    SendBroadcastText(WSGBroadcastTexts.HORDE_FLAG_PICKED_UP, ChatMsg.BgSystemAlliance, player);
                    PlaySoundToAll(WSGSound.HORDE_FLAG_PICKED_UP);
                    SpawnBGObject(WSGObjectTypes.H_FLAG, BattlegroundConst.RespawnOneDay);
                    SetHordeFlagPicker(player.GetGUID());
                    player.CastSpell(player, WSGSpellId.WARSONG_FLAG, true);
                    _flagState[TeamId.Horde] = WSGFlagState.OnPlayer;
                    UpdateFlagState(Team.Alliance, WSGFlagState.OnPlayer);

                    if (_flagDebuffState == 1)
                        player.CastSpell(player, WSGSpellId.FOCUSED_ASSAULT, true);
                    else if (_flagDebuffState == 2)
                        player.CastSpell(player, WSGSpellId.BRUTAL_ASSAULT, true);
                }
                //called in HandleGameObjectUseOpcode:
                //target_obj.Delete();
            }

            player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.PvPActive);
        }

        public override void RemovePlayer(Player player, ObjectGuid guid, Team team)
        {
            // sometimes flag aura not removed :(
            if (IsAllianceFlagPickedup() &&
                _flagKeepers[TeamId.Alliance] == guid)
            {
                if (!player)
                {
                    Log.outError(LogFilter.Battleground, "BattlegroundWS: Removing offline player who has the FLAG!!");
                    SetAllianceFlagPicker(ObjectGuid.Empty);
                    RespawnFlag(Team.Alliance, false);
                }
                else
                {
                    EventPlayerDroppedFlag(player);
                }
            }

            if (IsHordeFlagPickedup() &&
                _flagKeepers[TeamId.Horde] == guid)
            {
                if (!player)
                {
                    Log.outError(LogFilter.Battleground, "BattlegroundWS: Removing offline player who has the FLAG!!");
                    SetHordeFlagPicker(ObjectGuid.Empty);
                    RespawnFlag(Team.Horde, false);
                }
                else
                {
                    EventPlayerDroppedFlag(player);
                }
            }
        }

        public override void HandleAreaTrigger(Player player, uint trigger, bool entered)
        {
            //uint SpellId = 0;
            //uint64 buff_guid = 0;
            switch (trigger)
            {
                case 8965: // Horde Start
                case 8966: // Alliance Start
                    if (GetStatus() == BattlegroundStatus.WaitJoin &&
                        !entered)
                        TeleportPlayerToExploitLocation(player);

                    break;
                case 3686: // Alliance elixir of speed spawn. Trigger not working, because located inside other areatrigger, can be replaced by IsWithinDist(object, dist) in Battleground.Update().
                           //buff_guid = BgObjects[BG_WS_OBJECT_SPEEDBUFF_1];
                    break;
                case 3687: // Horde elixir of speed spawn. Trigger not working, because located inside other areatrigger, can be replaced by IsWithinDist(object, dist) in Battleground.Update().
                           //buff_guid = BgObjects[BG_WS_OBJECT_SPEEDBUFF_2];
                    break;
                case 3706: // Alliance elixir of regeneration spawn
                           //buff_guid = BgObjects[BG_WS_OBJECT_REGENBUFF_1];
                    break;
                case 3708: // Horde elixir of regeneration spawn
                           //buff_guid = BgObjects[BG_WS_OBJECT_REGENBUFF_2];
                    break;
                case 3707: // Alliance elixir of berserk spawn
                           //buff_guid = BgObjects[BG_WS_OBJECT_BERSERKBUFF_1];
                    break;
                case 3709: // Horde elixir of berserk spawn
                           //buff_guid = BgObjects[BG_WS_OBJECT_BERSERKBUFF_2];
                    break;
                case 3646: // Alliance Flag spawn
                    if (_flagState[TeamId.Horde] != 0 &&
                        _flagState[TeamId.Alliance] == 0)
                        if (GetFlagPickerGUID(TeamId.Horde) == player.GetGUID())
                            EventPlayerCapturedFlag(player);

                    break;
                case 3647: // Horde Flag spawn
                    if (_flagState[TeamId.Alliance] != 0 &&
                        _flagState[TeamId.Horde] == 0)
                        if (GetFlagPickerGUID(TeamId.Alliance) == player.GetGUID())
                            EventPlayerCapturedFlag(player);

                    break;
                case 3649: // unk1
                case 3688: // unk2
                case 4628: // unk3
                case 4629: // unk4
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
            result &= AddObject(WSGObjectTypes.A_FLAG, WSGObjectEntry.A_FLAG, 1540.423f, 1481.325f, 351.8284f, 3.089233f, 0, 0, 0.9996573f, 0.02617699f, WSGTimerOrScore.FLAG_RESPAWN_TIME / 1000);
            result &= AddObject(WSGObjectTypes.H_FLAG, WSGObjectEntry.H_FLAG, 916.0226f, 1434.405f, 345.413f, 0.01745329f, 0, 0, 0.008726535f, 0.9999619f, WSGTimerOrScore.FLAG_RESPAWN_TIME / 1000);

            if (!result)
            {
                Log.outError(LogFilter.Sql, "BgWarsongGluch: Failed to spawn flag object!");

                return false;
            }

            // buffs
            result &= AddObject(WSGObjectTypes.SPEEDBUFF_1, Buff_Entries[0], 1449.93f, 1470.71f, 342.6346f, -1.64061f, 0, 0, 0.7313537f, -0.6819983f, BattlegroundConst.BuffRespawnTime);
            result &= AddObject(WSGObjectTypes.SPEEDBUFF_2, Buff_Entries[0], 1005.171f, 1447.946f, 335.9032f, 1.64061f, 0, 0, 0.7313537f, 0.6819984f, BattlegroundConst.BuffRespawnTime);
            result &= AddObject(WSGObjectTypes.REGENBUFF_1, Buff_Entries[1], 1317.506f, 1550.851f, 313.2344f, -0.2617996f, 0, 0, 0.1305263f, -0.9914448f, BattlegroundConst.BuffRespawnTime);
            result &= AddObject(WSGObjectTypes.REGENBUFF_2, Buff_Entries[1], 1110.451f, 1353.656f, 316.5181f, -0.6806787f, 0, 0, 0.333807f, -0.9426414f, BattlegroundConst.BuffRespawnTime);
            result &= AddObject(WSGObjectTypes.BERSERKBUFF_1, Buff_Entries[2], 1320.09f, 1378.79f, 314.7532f, 1.186824f, 0, 0, 0.5591929f, 0.8290376f, BattlegroundConst.BuffRespawnTime);
            result &= AddObject(WSGObjectTypes.BERSERKBUFF_2, Buff_Entries[2], 1139.688f, 1560.288f, 306.8432f, -2.443461f, 0, 0, 0.9396926f, -0.3420201f, BattlegroundConst.BuffRespawnTime);

            if (!result)
            {
                Log.outError(LogFilter.Sql, "BgWarsongGluch: Failed to spawn buff object!");

                return false;
            }

            // alliance gates
            result &= AddObject(WSGObjectTypes.DOOR_A_1, WSGObjectEntry.DOOR_A_1, 1503.335f, 1493.466f, 352.1888f, 3.115414f, 0, 0, 0.9999143f, 0.01308903f, BattlegroundConst.RespawnImmediately);
            result &= AddObject(WSGObjectTypes.DOOR_A_2, WSGObjectEntry.DOOR_A_2, 1492.478f, 1457.912f, 342.9689f, 3.115414f, 0, 0, 0.9999143f, 0.01308903f, BattlegroundConst.RespawnImmediately);
            result &= AddObject(WSGObjectTypes.DOOR_A_3, WSGObjectEntry.DOOR_A_3, 1468.503f, 1494.357f, 351.8618f, 3.115414f, 0, 0, 0.9999143f, 0.01308903f, BattlegroundConst.RespawnImmediately);
            result &= AddObject(WSGObjectTypes.DOOR_A_4, WSGObjectEntry.DOOR_A_4, 1471.555f, 1458.778f, 362.6332f, 3.115414f, 0, 0, 0.9999143f, 0.01308903f, BattlegroundConst.RespawnImmediately);
            result &= AddObject(WSGObjectTypes.DOOR_A_5, WSGObjectEntry.DOOR_A_5, 1492.347f, 1458.34f, 342.3712f, -0.03490669f, 0, 0, 0.01745246f, -0.9998477f, BattlegroundConst.RespawnImmediately);
            result &= AddObject(WSGObjectTypes.DOOR_A_6, WSGObjectEntry.DOOR_A_6, 1503.466f, 1493.367f, 351.7352f, -0.03490669f, 0, 0, 0.01745246f, -0.9998477f, BattlegroundConst.RespawnImmediately);
            // horde gates
            result &= AddObject(WSGObjectTypes.DOOR_H_1, WSGObjectEntry.DOOR_H_1, 949.1663f, 1423.772f, 345.6241f, -0.5756807f, -0.01673368f, -0.004956111f, -0.2839723f, 0.9586737f, BattlegroundConst.RespawnImmediately);
            result &= AddObject(WSGObjectTypes.DOOR_H_2, WSGObjectEntry.DOOR_H_2, 953.0507f, 1459.842f, 340.6526f, -1.99662f, -0.1971825f, 0.1575096f, -0.8239487f, 0.5073641f, BattlegroundConst.RespawnImmediately);
            result &= AddObject(WSGObjectTypes.DOOR_H_3, WSGObjectEntry.DOOR_H_3, 949.9523f, 1422.751f, 344.9273f, 0.0f, 0, 0, 0, 1, BattlegroundConst.RespawnImmediately);
            result &= AddObject(WSGObjectTypes.DOOR_H_4, WSGObjectEntry.DOOR_H_4, 950.7952f, 1459.583f, 342.1523f, 0.05235988f, 0, 0, 0.02617695f, 0.9996573f, BattlegroundConst.RespawnImmediately);

            if (!result)
            {
                Log.outError(LogFilter.Sql, "BgWarsongGluch: Failed to spawn door object Battleground not created!");

                return false;
            }

            WorldSafeLocsEntry sg = Global.ObjectMgr.GetWorldSafeLoc(WSGGraveyards.MAIN_ALLIANCE);

            if (sg == null ||
                !AddSpiritGuide(WSGCreatureTypes.SPIRIT_MAIN_ALLIANCE, sg.Loc.GetPositionX(), sg.Loc.GetPositionY(), sg.Loc.GetPositionZ(), 3.124139f, TeamId.Alliance))
            {
                Log.outError(LogFilter.Sql, "BgWarsongGluch: Failed to spawn Alliance spirit guide! Battleground not created!");

                return false;
            }

            sg = Global.ObjectMgr.GetWorldSafeLoc(WSGGraveyards.MAIN_HORDE);

            if (sg == null ||
                !AddSpiritGuide(WSGCreatureTypes.SPIRIT_MAIN_HORDE, sg.Loc.GetPositionX(), sg.Loc.GetPositionY(), sg.Loc.GetPositionZ(), 3.193953f, TeamId.Horde))
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

            _flagKeepers[TeamId.Alliance].Clear();
            _flagKeepers[TeamId.Horde].Clear();
            _droppedFlagGUID[TeamId.Alliance] = ObjectGuid.Empty;
            _droppedFlagGUID[TeamId.Horde] = ObjectGuid.Empty;
            _flagState[TeamId.Alliance] = WSGFlagState.OnBase;
            _flagState[TeamId.Horde] = WSGFlagState.OnBase;
            TeamScores[TeamId.Alliance] = 0;
            TeamScores[TeamId.Horde] = 0;

            if (Global.BattlegroundMgr.IsBGWeekend(GetTypeID()))
            {
                _reputationCapture = 45;
                _honorWinKills = 3;
                _honorEndKills = 4;
            }
            else
            {
                _reputationCapture = 35;
                _honorWinKills = 1;
                _honorEndKills = 2;
            }

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
                RewardHonorToTeam(GetBonusHonorFromKill(_honorWinKills), Team.Alliance);

            if (winner == Team.Horde)
                RewardHonorToTeam(GetBonusHonorFromKill(_honorWinKills), Team.Horde);

            // Complete map_end rewards (even if no team wins)
            RewardHonorToTeam(GetBonusHonorFromKill(_honorEndKills), Team.Alliance);
            RewardHonorToTeam(GetBonusHonorFromKill(_honorEndKills), Team.Horde);

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
                case ScoreType.FlagCaptures: // Flags captured
                    player.UpdateCriteria(CriteriaType.TrackedWorldStateUIModified, WSObjectives.CAPTURE_FLAG);

                    break;
                case ScoreType.FlagReturns: // Flags returned
                    player.UpdateCriteria(CriteriaType.TrackedWorldStateUIModified, WSObjectives.RETURN_FLAG);

                    break;
                default:
                    break;
            }

            return true;
        }

        public override WorldSafeLocsEntry GetClosestGraveYard(Player player)
        {
            //if status in progress, it returns main graveyards with spiritguides
            //else it will return the graveyard in the flagroom - this is especially good
            //if a player dies in preparation phase - then the player can't cheat
            //and teleport to the graveyard outside the flagroom
            //and start running around, while the doors are still closed
            if (GetPlayerTeam(player.GetGUID()) == Team.Alliance)
            {
                if (GetStatus() == BattlegroundStatus.InProgress)
                    return Global.ObjectMgr.GetWorldSafeLoc(WSGGraveyards.MAIN_ALLIANCE);
                else
                    return Global.ObjectMgr.GetWorldSafeLoc(WSGGraveyards.FLAG_ROOM_ALLIANCE);
            }
            else
            {
                if (GetStatus() == BattlegroundStatus.InProgress)
                    return Global.ObjectMgr.GetWorldSafeLoc(WSGGraveyards.MAIN_HORDE);
                else
                    return Global.ObjectMgr.GetWorldSafeLoc(WSGGraveyards.FLAG_ROOM_HORDE);
            }
        }

        public override WorldSafeLocsEntry GetExploitTeleportLocation(Team team)
        {
            return Global.ObjectMgr.GetWorldSafeLoc(team == Team.Alliance ? EXPLOIT_TELEPORT_LOCATION_ALLIANCE : EXPLOIT_TELEPORT_LOCATION_HORDE);
        }

        public override Team GetPrematureWinner()
        {
            if (GetTeamScore(TeamId.Alliance) > GetTeamScore(TeamId.Horde))
                return Team.Alliance;
            else if (GetTeamScore(TeamId.Horde) > GetTeamScore(TeamId.Alliance))
                return Team.Horde;

            return base.GetPrematureWinner();
        }

        public override ObjectGuid GetFlagPickerGUID(int team = -1)
        {
            if (team == TeamId.Alliance ||
                team == TeamId.Horde)
                return _flagKeepers[team];

            return ObjectGuid.Empty;
        }

        public override void SetDroppedFlagGUID(ObjectGuid guid, int team = -1)
        {
            if (team == TeamId.Alliance ||
                team == TeamId.Horde)
                _droppedFlagGUID[team] = guid;
        }

        private void RespawnFlag(Team Team, bool captured)
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
                SpawnBGObject(WSGObjectTypes.H_FLAG, BattlegroundConst.RespawnImmediately);
                SpawnBGObject(WSGObjectTypes.A_FLAG, BattlegroundConst.RespawnImmediately);
                SendBroadcastText(WSGBroadcastTexts.FLAGS_PLACED, ChatMsg.BgSystemNeutral);
                PlaySoundToAll(WSGSound.FLAGS_RESPAWNED); // flag respawned sound...
            }

            _bothFlagsKept = false;
        }

        private void RespawnFlagAfterDrop(Team team)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            RespawnFlag(team, false);

            if (team == Team.Alliance)
                SpawnBGObject(WSGObjectTypes.A_FLAG, BattlegroundConst.RespawnImmediately);
            else
                SpawnBGObject(WSGObjectTypes.H_FLAG, BattlegroundConst.RespawnImmediately);

            SendBroadcastText(WSGBroadcastTexts.FLAGS_PLACED, ChatMsg.BgSystemNeutral);
            PlaySoundToAll(WSGSound.FLAGS_RESPAWNED);

            GameObject obj = GetBgMap().GetGameObject(GetDroppedFlagGUID(team));

            if (obj)
                obj.Delete();
            else
                Log.outError(LogFilter.Battleground, "unknown droped flag ({0})", GetDroppedFlagGUID(team).ToString());

            SetDroppedFlagGUID(ObjectGuid.Empty, GetTeamIndexByTeamId(team));
            _bothFlagsKept = false;
            // Check opposing flag if it is in capture zone; if so, capture it
            HandleFlagRoomCapturePoint(team == Team.Alliance ? TeamId.Horde : TeamId.Alliance);
        }

        private void EventPlayerCapturedFlag(Player player)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            Team winner = 0;

            player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.PvPActive);
            Team team = GetPlayerTeam(player.GetGUID());

            if (team == Team.Alliance)
            {
                if (!IsHordeFlagPickedup())
                    return;

                SetHordeFlagPicker(ObjectGuid.Empty); // must be before aura remove to prevent 2 events (drop+capture) at the same Time
                                                      // horde flag in base (but not respawned yet)
                _flagState[TeamId.Horde] = WSGFlagState.WaitRespawn;
                // Drop Horde Flag from Player
                player.RemoveAurasDueToSpell(WSGSpellId.WARSONG_FLAG);

                if (_flagDebuffState == 1)
                    player.RemoveAurasDueToSpell(WSGSpellId.FOCUSED_ASSAULT);
                else if (_flagDebuffState == 2)
                    player.RemoveAurasDueToSpell(WSGSpellId.BRUTAL_ASSAULT);

                if (GetTeamScore(TeamId.Alliance) < WSGTimerOrScore.MAX_TEAM_SCORE)
                    AddPoint(Team.Alliance, 1);

                PlaySoundToAll(WSGSound.FLAG_CAPTURED_ALLIANCE);
                RewardReputationToTeam(890, _reputationCapture, Team.Alliance);
            }
            else
            {
                if (!IsAllianceFlagPickedup())
                    return;

                SetAllianceFlagPicker(ObjectGuid.Empty); // must be before aura remove to prevent 2 events (drop+capture) at the same Time
                                                         // alliance flag in base (but not respawned yet)
                _flagState[TeamId.Alliance] = WSGFlagState.WaitRespawn;
                // Drop Alliance Flag from Player
                player.RemoveAurasDueToSpell(WSGSpellId.SILVERWING_FLAG);

                if (_flagDebuffState == 1)
                    player.RemoveAurasDueToSpell(WSGSpellId.FOCUSED_ASSAULT);
                else if (_flagDebuffState == 2)
                    player.RemoveAurasDueToSpell(WSGSpellId.BRUTAL_ASSAULT);

                if (GetTeamScore(TeamId.Horde) < WSGTimerOrScore.MAX_TEAM_SCORE)
                    AddPoint(Team.Horde, 1);

                PlaySoundToAll(WSGSound.FLAG_CAPTURED_HORDE);
                RewardReputationToTeam(889, _reputationCapture, Team.Horde);
            }

            //for flag capture is reward 2 honorable kills
            RewardHonorToTeam(GetBonusHonorFromKill(2), team);

            SpawnBGObject(WSGObjectTypes.H_FLAG, WSGTimerOrScore.FLAG_RESPAWN_TIME);
            SpawnBGObject(WSGObjectTypes.A_FLAG, WSGTimerOrScore.FLAG_RESPAWN_TIME);

            if (team == Team.Alliance)
                SendBroadcastText(WSGBroadcastTexts.CAPTURED_HORDE_FLAG, ChatMsg.BgSystemAlliance, player);
            else
                SendBroadcastText(WSGBroadcastTexts.CAPTURED_ALLIANCE_FLAG, ChatMsg.BgSystemHorde, player);

            UpdateFlagState(team, WSGFlagState.WaitRespawn); // flag State none
            UpdateTeamScore(GetTeamIndexByTeamId(team));
            // only flag capture should be updated
            UpdatePlayerScore(player, ScoreType.FlagCaptures, 1); // +1 flag captures

            // update last flag capture to be used if teamscore is equal
            SetLastFlagCapture(team);

            if (GetTeamScore(TeamId.Alliance) == WSGTimerOrScore.MAX_TEAM_SCORE)
                winner = Team.Alliance;

            if (GetTeamScore(TeamId.Horde) == WSGTimerOrScore.MAX_TEAM_SCORE)
                winner = Team.Horde;

            if (winner != 0)
            {
                UpdateWorldState(WSGWorldStates.FLAG_STATE_ALLIANCE, 1);
                UpdateWorldState(WSGWorldStates.FLAG_STATE_HORDE, 1);
                UpdateWorldState(WSGWorldStates.STATE_TIMER_ACTIVE, 0);

                RewardHonorToTeam(_honor[(int)HonorMode][(int)WSGRewards.Win], winner);
                EndBattleground(winner);
            }
            else
            {
                _flagsTimer[GetTeamIndexByTeamId(team)] = WSGTimerOrScore.FLAG_RESPAWN_TIME;
            }
        }

        private void HandleFlagRoomCapturePoint(int team)
        {
            Player flagCarrier = Global.ObjAccessor.GetPlayer(GetBgMap(), GetFlagPickerGUID(team));
            uint areaTrigger = team == TeamId.Alliance ? 3647 : 3646u;

            if (flagCarrier != null &&
                flagCarrier.IsInAreaTriggerRadius(CliDB.AreaTriggerStorage.LookupByKey(areaTrigger)))
                EventPlayerCapturedFlag(flagCarrier);
        }

        private void UpdateFlagState(Team team, WSGFlagState value)
        {
            static int transformValueToOtherTeamControlWorldState(WSGFlagState value)
            {
                switch (value)
                {
                    case WSGFlagState.OnBase:
                    case WSGFlagState.OnGround:
                    case WSGFlagState.WaitRespawn:
                        return 1;
                    case WSGFlagState.OnPlayer:
                        return 2;
                    default:
                        return 0;
                }
            }

            ;

            if (team == Team.Horde)
            {
                UpdateWorldState(WSGWorldStates.FLAG_STATE_ALLIANCE, (int)value);
                UpdateWorldState(WSGWorldStates.FLAG_CONTROL_HORDE, transformValueToOtherTeamControlWorldState(value));
            }
            else
            {
                UpdateWorldState(WSGWorldStates.FLAG_STATE_HORDE, (int)value);
                UpdateWorldState(WSGWorldStates.FLAG_CONTROL_ALLIANCE, transformValueToOtherTeamControlWorldState(value));
            }
        }

        private void UpdateTeamScore(int team)
        {
            if (team == TeamId.Alliance)
                UpdateWorldState(WSGWorldStates.FLAG_CAPTURES_ALLIANCE, (int)GetTeamScore(team));
            else
                UpdateWorldState(WSGWorldStates.FLAG_CAPTURES_HORDE, (int)GetTeamScore(team));
        }

        private void SetAllianceFlagPicker(ObjectGuid guid)
        {
            _flagKeepers[TeamId.Alliance] = guid;
        }

        private void SetHordeFlagPicker(ObjectGuid guid)
        {
            _flagKeepers[TeamId.Horde] = guid;
        }

        private bool IsAllianceFlagPickedup()
        {
            return !_flagKeepers[TeamId.Alliance].IsEmpty();
        }

        private bool IsHordeFlagPickedup()
        {
            return !_flagKeepers[TeamId.Horde].IsEmpty();
        }

        private WSGFlagState GetFlagState(Team team)
        {
            return _flagState[GetTeamIndexByTeamId(team)];
        }

        private void SetLastFlagCapture(Team team)
        {
            _lastFlagCaptureTeam = (uint)team;
        }

        private ObjectGuid GetDroppedFlagGUID(Team team)
        {
            return _droppedFlagGUID[GetTeamIndexByTeamId(team)];
        }

        private void AddPoint(Team team, uint Points = 1)
        {
            TeamScores[GetTeamIndexByTeamId(team)] += Points;
        }
    }

    internal class BattlegroundWGScore : BattlegroundScore
    {
        private uint FlagCaptures;
        private uint FlagReturns;

        public BattlegroundWGScore(ObjectGuid playerGuid, Team team) : base(playerGuid, team)
        {
        }

        public override void UpdateScore(ScoreType type, uint value)
        {
            switch (type)
            {
                case ScoreType.FlagCaptures: // Flags captured
                    FlagCaptures += value;

                    break;
                case ScoreType.FlagReturns: // Flags returned
                    FlagReturns += value;

                    break;
                default:
                    base.UpdateScore(type, value);

                    break;
            }
        }

        public override void BuildPvPLogPlayerDataPacket(out PVPMatchStatistics.PVPMatchPlayerStatistics playerData)
        {
            base.BuildPvPLogPlayerDataPacket(out playerData);

            playerData.Stats.Add(new PVPMatchStatistics.PVPMatchPlayerPVPStat(WSObjectives.CAPTURE_FLAG, FlagCaptures));
            playerData.Stats.Add(new PVPMatchStatistics.PVPMatchPlayerPVPStat(WSObjectives.RETURN_FLAG, FlagReturns));
        }

        public override uint GetAttr1()
        {
            return FlagCaptures;
        }

        public override uint GetAttr2()
        {
            return FlagReturns;
        }
    }
}