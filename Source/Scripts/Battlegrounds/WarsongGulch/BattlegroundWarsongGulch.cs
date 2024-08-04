// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.BattleGrounds;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.Battlegrounds.WarsongGulch
{
    enum SpellIds
    {
        FocusedAssault = 46392,
        BrutalAssault = 46393,
        CapturedAllianceCosmeticFx = 262508,
        CapturedHordeCosmeticFx = 262512,
        WarsongFlag = 23333,
        WarsongFlagDropped = 23334,
        SilverwingFlag = 23335,
        SilverwingFlagDropped = 23336,
        QuickCapTimer = 183317,
    }

    enum AreaTriggerIds
    {
        CapturePointAlliance = 30,
        CapturePointHorde = 31
    }

    enum PvpStats
    {
        FlagCaptures = 928,
        FlagReturns = 929
    }

    enum WorldStateIds
    {
        FlagStateAlliance = 1545,
        FlagStateHorde = 1546,
        FlagStateNeutral = 1547,
        HordeFlagCountPickedUp = 17712,
        AllianceFlagCountPickedUp = 17713,
        FlagCapturesAlliance = 1581,
        FlagCapturesHorde = 1582,
        FlagCapturesMax = 1601,
        FlagCapturesMaxNew = 17303,
        FlagControlHorde = 2338,
        FlagControlAlliance = 2339,
        StateTimer = 4248,
        StateTimerActive = 4247
    }

    enum TextIds
    {
        StartOneMinute = 10015,
        StartHalfMinute = 10016,
        BattleHasBegun = 10014,
        CapturedHordeFlag = 9801,
        CapturedAllianceFlag = 9802,
        FlagsPlaced = 9803,
        AllianceFlagPickedUp = 9804,
        AllianceFlagDropped = 9805,
        HordeFlagPickedUp = 9807,
        HordeFlagDropped = 9806,
        AllianceFlagReturned = 9808,
        HordeFlagReturned = 9809
    }

    enum SoundIds
    {
        FlagCapturedAlliance = 8173,
        FlagCapturedHorde = 8213,
        FlagPlaced = 8232,
        FlagReturned = 8192,
        HordeFlagPickedUp = 8212,
        AllianceFlagPickedUp = 8174,
        FlagsRespawned = 8232
    }

    enum GameObjectIds
    {
        AllianceDoor = 309704,
        Portcullis009 = 309705,
        Portcullis002 = 309883,
        CollisionPcSize = 242273,
        HordeGate1 = 352709,
        HordeGate2 = 352710,
        AllianceFlagInBase = 227741,
        HordeFlagInBase = 227740
    }

    struct Misc
    {
        public const uint MaxTeamScore = 3;
        public const uint FlagBrutalAssaultStackCount = 5;

        public const uint EventStartBattle = 35912;

        public static TimeSpan FlagAssaultTimer = TimeSpan.FromSeconds(30);

        public static uint[][] HonorRewards = [[20, 40, 40], [60, 40, 80]];
    }

    [Script(nameof(battleground_warsong_gulch), 2106)]
    class battleground_warsong_gulch : BattlegroundScript
    {
        Team _lastFlagCaptureTeam;
        bool _bothFlagsKept;
        List<ObjectGuid> _doors = new();
        ObjectGuid[] _flags = new ObjectGuid[SharedConst.PvpTeamsCount];

        TimeTracker _flagAssaultTimer;
        byte _assaultStackCount;
        ObjectGuid[] _capturePointAreaTriggers = new ObjectGuid[SharedConst.PvpTeamsCount];

        uint _honorWinKills;
        uint _honorEndKills;
        uint _reputationCapture;

        public battleground_warsong_gulch(BattlegroundMap map) : base(map)
        {
            _lastFlagCaptureTeam = Team.Other;

            _flagAssaultTimer.Reset(Misc.FlagAssaultTimer);

            if (Global.BattlegroundMgr.IsBGWeekend(battleground.GetTypeID()))
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
        }

        public override void OnUpdate(uint diff)
        {
            base.OnUpdate(diff);

            if (battleground.GetStatus() == BattlegroundStatus.InProgress)
            {
                if (battleground.GetElapsedTime() >= 17 * Time.Minute * Time.InMilliseconds)
                {
                    if (battleground.GetTeamScore(BattleGroundTeamId.Alliance) == 0)
                    {
                        if (battleground.GetTeamScore(BattleGroundTeamId.Horde) == 0) // No one scored - result is tie
                            battleground.EndBattleground(Team.Other);
                        else // Horde has more points and thus wins
                            battleground.EndBattleground(Team.Horde);
                    }
                    else if (battleground.GetTeamScore(BattleGroundTeamId.Horde) == 0) // Alliance has > 0, Horde has 0, alliance wins
                        battleground.EndBattleground(Team.Alliance);
                    else if (battleground.GetTeamScore(BattleGroundTeamId.Horde) == battleground.GetTeamScore(BattleGroundTeamId.Alliance)) // Team score equal, winner is team that scored the last flag
                        battleground.EndBattleground(_lastFlagCaptureTeam);
                    else if (battleground.GetTeamScore(BattleGroundTeamId.Horde) > battleground.GetTeamScore(BattleGroundTeamId.Alliance))  // Last but not least, check who has the higher score
                        battleground.EndBattleground(Team.Horde);
                    else
                        battleground.EndBattleground(Team.Alliance);
                }
            }

            if (_bothFlagsKept)
            {
                _flagAssaultTimer.Update(diff);
                if (_flagAssaultTimer.Passed())
                {
                    _flagAssaultTimer.Reset(Misc.FlagAssaultTimer);
                    if (_assaultStackCount < byte.MaxValue)
                    {
                        _assaultStackCount++;

                        // update assault debuff stacks
                        DoForFlagKeepers(ApplyAssaultDebuffToPlayer);
                    }
                }
            }
        }

        public override void OnStart()
        {
            base.OnStart();
            foreach (ObjectGuid door in _doors)
            {
                GameObject gameObject = battlegroundMap.GetGameObject(door);
                if (gameObject != null)
                {
                    gameObject.UseDoorOrButton();
                    gameObject.DespawnOrUnsummon(TimeSpan.FromSeconds(3));
                }
            }

            UpdateWorldState((int)WorldStateIds.StateTimerActive, 1);
            UpdateWorldState((int)WorldStateIds.StateTimer, (int)Time.DateTimeToUnixTime(GameTime.GetSystemTime() + TimeSpan.FromMinutes(15)));

            // players joining later are not eligibles
            TriggerGameEvent(Misc.EventStartBattle);
        }

        public override void OnEnd(Team winner)
        {
            base.OnEnd(winner);
            // Win reward
            if (winner == Team.Alliance)
                battleground.RewardHonorToTeam(battleground.GetBonusHonorFromKill(_honorWinKills), Team.Alliance);
            if (winner == Team.Horde)
                battleground.RewardHonorToTeam(battleground.GetBonusHonorFromKill(_honorWinKills), Team.Horde);

            // Complete map_end rewards (even if no team wins)
            battleground.RewardHonorToTeam(battleground.GetBonusHonorFromKill(_honorEndKills), Team.Alliance);
            battleground.RewardHonorToTeam(battleground.GetBonusHonorFromKill(_honorEndKills), Team.Horde);
        }

        void DoForFlagKeepers(Action<Player> action)
        {
            foreach (ObjectGuid flagGUID in _flags)
            {
                GameObject flag = battlegroundMap.GetGameObject(flagGUID);
                if (flag != null)
                {
                    Player carrier = Global.ObjAccessor.FindPlayer(flag.GetFlagCarrierGUID());
                    if (carrier != null)
                        action(carrier);
                }
            }
        }

        void ResetAssaultDebuff()
        {
            _bothFlagsKept = false;
            _assaultStackCount = 0;
            _flagAssaultTimer.Reset(Misc.FlagAssaultTimer);
            DoForFlagKeepers(RemoveAssaultDebuffFromPlayer);
        }

        void ApplyAssaultDebuffToPlayer(Player player)
        {
            if (_assaultStackCount == 0)
                return;

            uint spellId = (uint)SpellIds.FocusedAssault;
            if (_assaultStackCount >= Misc.FlagBrutalAssaultStackCount)
            {
                player.RemoveAurasDueToSpell((uint)SpellIds.FocusedAssault);
                spellId = (uint)SpellIds.BrutalAssault;
            }

            Aura aura = player.GetAura(spellId);
            if (aura == null)
            {
                player.CastSpell(player, spellId, true);
                aura = player.GetAura(spellId);
            }

            if (aura != null)
                aura.SetStackAmount(_assaultStackCount);
        }

        void RemoveAssaultDebuffFromPlayer(Player player)
        {
            player.RemoveAurasDueToSpell((uint)SpellIds.FocusedAssault);
            player.RemoveAurasDueToSpell((uint)SpellIds.BrutalAssault);
        }

        FlagState GetFlagState(int team)
        {
            GameObject flag = battlegroundMap.GetGameObject(_flags[team]);
            if (flag != null)
                return flag.GetFlagState();

            return 0;
        }

        ObjectGuid GetFlagCarrierGUID(int team)
        {
            GameObject flag = battlegroundMap.GetGameObject(_flags[team]);
            if (flag != null)
                return flag.GetFlagCarrierGUID();

            return ObjectGuid.Empty;
        }

        void HandleFlagRoomCapturePoint()
        {
            DoForFlagKeepers(player =>
            {
                int team = Battleground.GetTeamIndexByTeamId(battleground.GetPlayerTeam(player.GetGUID()));
                AreaTrigger trigger = battlegroundMap.GetAreaTrigger(_capturePointAreaTriggers[team]);
                if (trigger != null && trigger.GetInsideUnits().Contains(player.GetGUID()))
                    if (CanCaptureFlag(trigger, player))
                        OnCaptureFlag(trigger, player);
            });
        }

        void UpdateFlagState(Team team, FlagState value)
        {
            static int transformValueToOtherTeamControlWorldState(FlagState state) => state switch
            {

                FlagState.InBase or FlagState.Dropped or FlagState.Respawning => 1,
                FlagState.Taken => 2,
                _ => 0
            };

            if (team == Team.Alliance)
            {
                UpdateWorldState((int)WorldStateIds.FlagStateAlliance, (int)value);
                UpdateWorldState((int)WorldStateIds.FlagControlHorde, transformValueToOtherTeamControlWorldState(value));
            }
            else
            {
                UpdateWorldState((int)WorldStateIds.FlagStateHorde, (int)value);
                UpdateWorldState((int)WorldStateIds.FlagControlAlliance, transformValueToOtherTeamControlWorldState(value));
            }
        }

        void UpdateTeamScore(int team)
        {
            if (team == BattleGroundTeamId.Alliance)
                UpdateWorldState((int)WorldStateIds.FlagCapturesAlliance, (int)battleground.GetTeamScore(team));
            else
                UpdateWorldState((int)WorldStateIds.FlagCapturesHorde, (int)battleground.GetTeamScore(team));
        }

        public override void OnGameObjectCreate(GameObject gameObject)
        {
            base.OnGameObjectCreate(gameObject);
            switch ((GameObjectIds)gameObject.GetEntry())
            {
                case GameObjectIds.AllianceDoor:
                case GameObjectIds.Portcullis009:
                case GameObjectIds.Portcullis002:
                case GameObjectIds.CollisionPcSize:
                case GameObjectIds.HordeGate1:
                case GameObjectIds.HordeGate2:
                    _doors.Add(gameObject.GetGUID());
                    break;
                case GameObjectIds.AllianceFlagInBase:
                    _flags[BattleGroundTeamId.Alliance] = gameObject.GetGUID();
                    break;
                case GameObjectIds.HordeFlagInBase:
                    _flags[BattleGroundTeamId.Horde] = gameObject.GetGUID();
                    break;
                default:
                    break;
            }
        }

        public override void OnAreaTriggerCreate(AreaTrigger areaTrigger)
        {
            base.OnAreaTriggerCreate(areaTrigger);
            if (!areaTrigger.IsStaticSpawn())
                return;

            switch ((AreaTriggerIds)areaTrigger.GetEntry())
            {
                case AreaTriggerIds.CapturePointAlliance:
                    _capturePointAreaTriggers[BattleGroundTeamId.Alliance] = areaTrigger.GetGUID();
                    break;
                case AreaTriggerIds.CapturePointHorde:
                    _capturePointAreaTriggers[BattleGroundTeamId.Horde] = areaTrigger.GetGUID();
                    break;
                default:
                    break;
            }
        }

        public override void OnFlagStateChange(GameObject flagInBase, FlagState oldValue, FlagState newValue, Player player)
        {
            base.OnFlagStateChange(flagInBase, oldValue, newValue, player);

            Team team = flagInBase.GetEntry() == (uint)GameObjectIds.HordeFlagInBase ? Team.Horde : Team.Alliance;
            int otherTeamId = Battleground.GetTeamIndexByTeamId(SharedConst.GetOtherTeam(team));

            UpdateFlagState(team, newValue);

            switch (newValue)
            {
                case FlagState.InBase:
                {
                    if (battleground.GetStatus() == BattlegroundStatus.InProgress)
                    {
                        ResetAssaultDebuff();
                        if (player != null)
                        {
                            // flag got returned to base by player interaction
                            battleground.UpdatePvpStat(player, (uint)PvpStats.FlagReturns, 1);      // +1 flag returns

                            if (team == Team.Alliance)
                            {
                                battleground.SendBroadcastText((uint)TextIds.AllianceFlagReturned, ChatMsg.BgSystemAlliance, player);
                                battleground.PlaySoundToAll((uint)SoundIds.FlagReturned);
                            }
                            else
                            {
                                battleground.SendBroadcastText((uint)TextIds.HordeFlagReturned, ChatMsg.BgSystemHorde, player);
                                battleground.PlaySoundToAll((uint)SoundIds.FlagReturned);
                            }
                        }
                        // Flag respawned due to timeout/capture
                        else if (GetFlagState(otherTeamId) != FlagState.Respawning)
                        {
                            // if other flag is respawning, we will let that one handle the message and sound to prevent double message/sound.
                            battleground.SendBroadcastText((uint)TextIds.FlagsPlaced, ChatMsg.BgSystemNeutral);
                            battleground.PlaySoundToAll((uint)SoundIds.FlagsRespawned);
                        }

                        HandleFlagRoomCapturePoint();
                    }
                    break;
                }
                case FlagState.Dropped:
                {
                    player.RemoveAurasDueToSpell((uint)SpellIds.QuickCapTimer);
                    RemoveAssaultDebuffFromPlayer(player);

                    uint recentlyDroppedSpellId = BattlegroundConst.SpellRecentlyDroppedHordeFlag;
                    if (team == Team.Alliance)
                    {
                        recentlyDroppedSpellId = BattlegroundConst.SpellRecentlyDroppedAllianceFlag;
                        battleground.SendBroadcastText((uint)TextIds.AllianceFlagDropped, ChatMsg.BgSystemAlliance, player);
                    }
                    else
                        battleground.SendBroadcastText((uint)TextIds.HordeFlagDropped, ChatMsg.BgSystemHorde, player);

                    player.CastSpell(player, recentlyDroppedSpellId, true);
                    break;
                }
                case FlagState.Taken:
                {
                    if (team == Team.Horde)
                    {
                        battleground.SendBroadcastText((uint)TextIds.HordeFlagPickedUp, ChatMsg.BgSystemHorde, player);
                        battleground.PlaySoundToAll((uint)SoundIds.HordeFlagPickedUp);
                    }
                    else
                    {
                        battleground.SendBroadcastText((uint)TextIds.AllianceFlagPickedUp, ChatMsg.BgSystemAlliance, player);
                        battleground.PlaySoundToAll((uint)SoundIds.AllianceFlagPickedUp);
                    }

                    if (GetFlagState(otherTeamId) == FlagState.Taken)
                        _bothFlagsKept = true;

                    ApplyAssaultDebuffToPlayer(player);

                    flagInBase.CastSpell(player, (uint)SpellIds.QuickCapTimer, true);
                    player.StartCriteria(CriteriaStartEvent.BeSpellTarget, (uint)SpellIds.QuickCapTimer, TimeSpan.FromSeconds(GameTime.GetGameTime() - flagInBase.GetFlagTakenFromBaseTime()));
                    break;
                }
                case FlagState.Respawning:
                    ResetAssaultDebuff();
                    break;
                default:
                    break;
            }
        }

        public override bool CanCaptureFlag(AreaTrigger areaTrigger, Player player)
        {
            if (battleground.GetStatus() != BattlegroundStatus.InProgress)
                return false;

            Team team = battleground.GetPlayerTeam(player.GetGUID());
            int teamId = Battleground.GetTeamIndexByTeamId(team);
            int otherTeamId = Battleground.GetTeamIndexByTeamId(SharedConst.GetOtherTeam(team));

            if (areaTrigger.GetGUID() != _capturePointAreaTriggers[teamId])
                return false;

            // check if enemy flag's carrier is this player
            if (GetFlagCarrierGUID(otherTeamId) != player.GetGUID())
                return false;

            // check that team's flag is in base
            return GetFlagState(teamId) == FlagState.InBase;
        }

        public override void OnCaptureFlag(AreaTrigger areaTrigger, Player player)
        {
            base.OnCaptureFlag(areaTrigger, player);

            Team winner = Team.Other;

            Team team = battleground.GetPlayerTeam(player.GetGUID());
            int teamId = Battleground.GetTeamIndexByTeamId(team);
            int otherTeamId = Battleground.GetTeamIndexByTeamId(SharedConst.GetOtherTeam(team));

            /*
                1. Update flag states & score world states
                2. update points
                3. chat message & sound
                4. update criterias & achievements
                5. remove all related auras
                ?. Reward honor & reputation
            */

            // 1. update the flag states
            foreach (ObjectGuid flagGuid in _flags)
            {
                GameObject flag1 = battlegroundMap.GetGameObject(flagGuid);
                if (flag1 != null)
                    flag1.HandleCustomTypeCommand(new Game.Entities.GameObjectType.SetNewFlagState(FlagState.Respawning, player));
            }

            // 2. update points
            if (battleground.GetTeamScore(teamId) < Misc.MaxTeamScore)
                battleground.AddPoint(team, 1);

            UpdateTeamScore(teamId);

            // 3. chat message & sound
            if (team == Team.Alliance)
            {
                battleground.SendBroadcastText((uint)TextIds.CapturedHordeFlag, ChatMsg.BgSystemHorde, player);
                battleground.PlaySoundToAll((uint)SoundIds.FlagCapturedAlliance);
                battleground.RewardReputationToTeam(890, _reputationCapture, Team.Alliance);
                player.CastSpell(player, (uint)SpellIds.CapturedAllianceCosmeticFx);
            }
            else
            {
                battleground.SendBroadcastText((uint)TextIds.CapturedAllianceFlag, ChatMsg.BgSystemAlliance, player);
                battleground.PlaySoundToAll((uint)SoundIds.FlagCapturedHorde);
                battleground.RewardReputationToTeam(889, _reputationCapture, Team.Horde);
                player.CastSpell(player, (uint)SpellIds.CapturedHordeCosmeticFx);
            }

            // 4. update criteria's for achievement, player score etc.
            battleground.UpdatePvpStat(player, (uint)PvpStats.FlagCaptures, 1);      // +1 flag captures

            // 5. Remove all related auras
            RemoveAssaultDebuffFromPlayer(player);

            GameObject flag = battlegroundMap.GetGameObject(_flags[otherTeamId]);
            if (flag != null)
                player.RemoveAurasDueToSpell(flag.GetGoInfo().NewFlag.pickupSpell, flag.GetGUID());

            player.RemoveAurasDueToSpell((uint)SpellIds.QuickCapTimer);

            player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.PvPActive);

            battleground.RewardHonorToTeam(battleground.GetBonusHonorFromKill(2), team);

            // update last flag capture to be used if teamscore is equal
            SetLastFlagCapture(team);

            if (battleground.GetTeamScore(teamId) == Misc.MaxTeamScore)
                winner = team;

            if (winner != 0)
            {
                UpdateWorldState((int)WorldStateIds.FlagStateAlliance, 1);
                UpdateWorldState((int)WorldStateIds.FlagStateHorde, 1);
                UpdateWorldState((int)WorldStateIds.StateTimerActive, 0);

                battleground.RewardHonorToTeam(Misc.HonorRewards[Global.BattlegroundMgr.IsBGWeekend(battleground.GetTypeID()) ? 1 : 0][0], winner);
                battleground.EndBattleground(winner);
            }
        }

        public override Team GetPrematureWinner()
        {
            if (battleground.GetTeamScore(BattleGroundTeamId.Alliance) > battleground.GetTeamScore(BattleGroundTeamId.Horde))
                return Team.Alliance;
            if (battleground.GetTeamScore(BattleGroundTeamId.Horde) > battleground.GetTeamScore(BattleGroundTeamId.Alliance))
                return Team.Horde;

            return base.GetPrematureWinner();
        }

        void SetLastFlagCapture(Team team)
        {
            _lastFlagCaptureTeam = team;
        }
    }
}