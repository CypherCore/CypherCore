// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Entities.GameObjectType;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Game.BattleGrounds.Zones.WarsongGluch
{
    class BgWarsongGluch : Battleground
    {
        Team _lastFlagCaptureTeam;                       // Winner is based on this if score is equal

        uint m_ReputationCapture;
        uint m_HonorWinKills;
        uint m_HonorEndKills;
        bool _bothFlagsKept;

        List<ObjectGuid> _doors = new();
        ObjectGuid[] _flags = new ObjectGuid[2];

        TimeTracker _flagAssaultTimer;
        byte _assaultStackCount;
        ObjectGuid[] _capturePointAreaTriggers = new ObjectGuid[2];

        public BgWarsongGluch(BattlegroundTemplate battlegroundTemplate) : base(battlegroundTemplate)
        {
            StartMessageIds[BattlegroundConst.EventIdSecond] = (uint)BroadcastTextIds.StartOneMinute;
            StartMessageIds[BattlegroundConst.EventIdThird] = (uint)BroadcastTextIds.StartHalfMinute;
            StartMessageIds[BattlegroundConst.EventIdFourth] = (uint)BroadcastTextIds.BattleHasBegun;

            _flagAssaultTimer = new(MiscConst.FlagAssaultTimer);
        }

        public override void PostUpdateImpl(uint diff)
        {
            if (GetStatus() == BattlegroundStatus.InProgress)
            {
                if (GetElapsedTime() >= 17 * Time.Minute * Time.InMilliseconds)
                {
                    if (GetTeamScore(BattleGroundTeamId.Alliance) == 0)
                    {
                        if (GetTeamScore(BattleGroundTeamId.Horde) == 0)        // No one scored - result is tie
                            EndBattleground(Team.Other);
                        else                                 // Horde has more points and thus wins
                            EndBattleground(Team.Horde);
                    }
                    else if (GetTeamScore(BattleGroundTeamId.Horde) == 0)
                        EndBattleground(Team.Alliance);           // Alliance has > 0, Horde has 0, alliance wins
                    else if (GetTeamScore(BattleGroundTeamId.Horde) == GetTeamScore(BattleGroundTeamId.Alliance)) // Team score equal, winner is team that scored the last flag
                        EndBattleground((Team)_lastFlagCaptureTeam);
                    else if (GetTeamScore(BattleGroundTeamId.Horde) > GetTeamScore(BattleGroundTeamId.Alliance))  // Last but not least, check who has the higher score
                        EndBattleground(Team.Horde);
                    else
                        EndBattleground(Team.Alliance);
                }

            }

            if (_bothFlagsKept)
            {
                _flagAssaultTimer.Update(diff);
                if (_flagAssaultTimer.Passed())
                {
                    _flagAssaultTimer.Reset(MiscConst.FlagAssaultTimer);
                    _assaultStackCount++;

                    // update assault debuff stacks
                    DoForFlagKeepers(ApplyAssaultDebuffToPlayer);
                }
            }
        }

        void DoForFlagKeepers(Action<Player> action)
        {
            foreach (ObjectGuid flagGUID in _flags)
            {
                GameObject flag = GetBgMap().GetGameObject(flagGUID);
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
            _flagAssaultTimer.Reset(MiscConst.FlagAssaultTimer);
            DoForFlagKeepers(RemoveAssaultDebuffFromPlayer);
        }

        void ApplyAssaultDebuffToPlayer(Player player)
        {
            if (_assaultStackCount == 0)
                return;

            uint spellId = (uint)SpellIds.FocusedAssault;
            if (_assaultStackCount >= MiscConst.FlagBrutalAssaultStackCount)
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

        public override void StartingEventOpenDoors()
        {
            foreach (ObjectGuid door in _doors)
            {
                GameObject gameObject = GetBgMap().GetGameObject(door);
                if (gameObject != null)
                {
                    gameObject.UseDoorOrButton();
                    gameObject.DespawnOrUnsummon(TimeSpan.FromSeconds(3));
                }
            }

            UpdateWorldState((int)WorldStateIds.StateTimerActive, 1);
            UpdateWorldState((int)WorldStateIds.StateTimer, (int)(GameTime.GetGameTime() + 15 * Time.Minute));

            // players joining later are not eligibles
            TriggerGameEvent(8563);
        }

        FlagState GetFlagState(int team)
        {
            GameObject gameObject = FindBgMap().GetGameObject(_flags[team]);
            if (gameObject != null)
                return gameObject.GetFlagState();

            return 0;
        }

        ObjectGuid GetFlagCarrierGUID(int team)
        {
            GameObject gameObject = FindBgMap().GetGameObject(_flags[team]);
            if (gameObject != null)
                return gameObject.GetFlagCarrierGUID();

            return ObjectGuid.Empty;
        }

        void HandleFlagRoomCapturePoint()
        {
            DoForFlagKeepers(player =>
            {
                int team = GetTeamIndexByTeamId(GetPlayerTeam(player.GetGUID()));
                AreaTrigger trigger = GetBgMap().GetAreaTrigger(_capturePointAreaTriggers[team]);
                if (trigger != null && trigger.GetInsideUnits().Contains(player.GetGUID()))
                    if (CanCaptureFlag(trigger, player))
                        OnCaptureFlag(trigger, player);
            });
        }

        void UpdateFlagState(Team team, FlagState value)
        {
            int transformValueToOtherTeamControlWorldState(FlagState value)
            {
                switch (value)
                {
                    case FlagState.InBase:
                    case FlagState.Dropped:
                    case FlagState.Respawning:
                        return 1;
                    case FlagState.Taken:
                        return 2;
                    default:
                        return 0;
                }
            };

            if (team == Team.Horde)
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
                UpdateWorldState((int)WorldStateIds.FlagCapturesAlliance, (int)GetTeamScore(team));
            else
                UpdateWorldState((int)WorldStateIds.FlagCapturesHorde, (int)GetTeamScore(team));
        }

        public override bool SetupBattleground()
        {
            return true;
        }

        public override void Reset()
        {
            //call parent's class reset
            base.Reset();

            m_TeamScores[BattleGroundTeamId.Alliance] = 0;
            m_TeamScores[BattleGroundTeamId.Horde] = 0;

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

            _lastFlagCaptureTeam = Team.Other;
            _bothFlagsKept = false;

            _doors.Clear();
            _flags.Clear();
            _assaultStackCount = 0;
            _flagAssaultTimer.Reset(MiscConst.FlagAssaultTimer);
            _capturePointAreaTriggers.Clear();
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

        public override WorldSafeLocsEntry GetClosestGraveyard(Player player)
        {
            return Global.ObjectMgr.GetClosestGraveyard(player, player.GetBGTeam(), player);
        }

        public override WorldSafeLocsEntry GetExploitTeleportLocation(Team team)
        {
            return Global.ObjectMgr.GetWorldSafeLoc(team == Team.Alliance ? MiscConst.ExploitTeleportLocationAlliance : MiscConst.ExploitTeleportLocationHorde);
        }

        public override Team GetPrematureWinner()
        {
            if (GetTeamScore(BattleGroundTeamId.Alliance) > GetTeamScore(BattleGroundTeamId.Horde))
                return Team.Alliance;
            else if (GetTeamScore(BattleGroundTeamId.Horde) > GetTeamScore(BattleGroundTeamId.Alliance))
                return Team.Horde;

            return base.GetPrematureWinner();
        }

        public override void OnGameObjectCreate(GameObject gameObject)
        {
            switch ((GameobjectIds)gameObject.GetEntry())
            {
                case GameobjectIds.AllianceDoor:
                case GameobjectIds.Portcullis009:
                case GameobjectIds.Portcullis002:
                case GameobjectIds.CollisionPcSize:
                case GameobjectIds.HordeGate1:
                case GameobjectIds.HordeGate2:
                    _doors.Add(gameObject.GetGUID());
                    break;
                case GameobjectIds.AllianceFlagInBase:
                    _flags[BattleGroundTeamId.Alliance] = gameObject.GetGUID();
                    break;
                case GameobjectIds.HordeFlagInBase:
                    _flags[BattleGroundTeamId.Horde] = gameObject.GetGUID();
                    break;
                default:
                    break;
            }
        }

        public override void OnAreaTriggerCreate(AreaTrigger areaTrigger)
        {
            if (!areaTrigger.IsStaticSpawn())
                return;

            switch (areaTrigger.GetEntry())
            {
                case MiscConst.AtCapturePointAlliance:
                    _capturePointAreaTriggers[BattleGroundTeamId.Alliance] = areaTrigger.GetGUID();
                    break;
                case MiscConst.AtCapturePointHorde:
                    _capturePointAreaTriggers[BattleGroundTeamId.Horde] = areaTrigger.GetGUID();
                    break;
                default:
                    break;
            }
        }

        public override void OnFlagStateChange(GameObject flagInBase, FlagState oldValue, FlagState newValue, Player player)
        {
            Team team = flagInBase.GetEntry() == (uint)GameobjectIds.HordeFlagInBase ? Team.Horde : Team.Alliance;
            int otherTeamId = GetTeamIndexByTeamId(GetOtherTeam(team));

            UpdateFlagState(team, newValue);

            switch (newValue)
            {
                case FlagState.InBase:
                {
                    if (GetStatus() == BattlegroundStatus.InProgress)
                    {
                        ResetAssaultDebuff();
                        if (player != null)
                        {
                            // flag got returned to base by player interaction
                            UpdatePvpStat(player, MiscConst.PvpStatFlagReturns, 1);      // +1 flag returns

                            if (team == Team.Alliance)
                            {
                                SendBroadcastText((uint)BroadcastTextIds.AllianceFlagReturned, ChatMsg.BgSystemAlliance, player);
                                PlaySoundToAll((uint)SoundIds.FlagReturned);
                            }
                            else
                            {
                                SendBroadcastText((uint)BroadcastTextIds.HordeFlagReturned, ChatMsg.BgSystemHorde, player);
                                PlaySoundToAll((uint)SoundIds.FlagReturned);
                            }
                        }
                        // Flag respawned due to timeout/capture
                        else if (GetFlagState(otherTeamId) != FlagState.Respawning)
                        {
                            // if other flag is respawning, we will let that one handle the message and sound to prevent double message/sound.
                            SendBroadcastText((uint)BroadcastTextIds.FlagsPlaced, ChatMsg.BgSystemNeutral);
                            PlaySoundToAll((uint)SoundIds.FlagsRespawned);
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
                        SendBroadcastText((uint)BroadcastTextIds.AllianceFlagDropped, ChatMsg.BgSystemAlliance, player);
                    }
                    else
                        SendBroadcastText((uint)BroadcastTextIds.HordeFlagDropped, ChatMsg.BgSystemHorde, player);

                    player.CastSpell(player, recentlyDroppedSpellId, true);
                    break;
                }
                case FlagState.Taken:
                {
                    if (team == Team.Horde)
                    {
                        SendBroadcastText((uint)BroadcastTextIds.HordeFlagPickedUp, ChatMsg.BgSystemHorde, player);
                        PlaySoundToAll((uint)SoundIds.HordeFlagPickedUp);
                    }
                    else
                    {
                        SendBroadcastText((uint)BroadcastTextIds.AllianceFlagPickedUp, ChatMsg.BgSystemAlliance, player);
                        PlaySoundToAll((uint)SoundIds.AllianceFlagPickedUp);
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
            if (GetStatus() != BattlegroundStatus.InProgress)
                return false;

            Team team = GetPlayerTeam(player.GetGUID());
            int teamId = GetTeamIndexByTeamId(team);
            int otherTeamId = GetTeamIndexByTeamId(GetOtherTeam(team));

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
            Team winner = Team.Other;

            Team team = GetPlayerTeam(player.GetGUID());
            int teamId = GetTeamIndexByTeamId(team);
            int otherTeamId = GetTeamIndexByTeamId(GetOtherTeam(team));

            /*
                1. Update flag states & score world states
                2. udpate points
                3. chat message & sound
                4. update criterias & achievements
                5. remove all related auras
                ?. Reward honor & reputation
            */

            // 1. update the flag states
            for (byte i = 0; i < _flags.Length; i++)
            {
                GameObject gameObject1 = GetBgMap().GetGameObject(_flags[i]);
                if (gameObject1 != null)
                    gameObject1.HandleCustomTypeCommand(new SetNewFlagState(FlagState.Respawning, player));
            }

            // 2. update points
            if (GetTeamScore(teamId) < MiscConst.MaxTeamScore)
                AddPoint(team, 1);

            UpdateTeamScore(teamId);

            // 3. chat message & sound
            if (team == Team.Alliance)
            {
                SendBroadcastText((uint)BroadcastTextIds.CapturedHordeFlag, ChatMsg.BgSystemHorde, player);
                PlaySoundToAll((uint)SoundIds.FlagCapturedAlliance);
                RewardReputationToTeam(890, m_ReputationCapture, Team.Alliance);
                player.CastSpell(player, (uint)SpellIds.CapturedAllianceCosmeticFx);
            }
            else
            {
                SendBroadcastText((uint)BroadcastTextIds.CapturedAllianceFlag, ChatMsg.BgSystemAlliance, player);
                PlaySoundToAll((uint)SoundIds.FlagCapturedHorde);
                RewardReputationToTeam(889, m_ReputationCapture, Team.Horde);
                player.CastSpell(player, (uint)SpellIds.CapturedHordeCosmeticFx);
            }

            // 4. update criteria's for achievement, player score etc.
            UpdatePvpStat(player, MiscConst.PvpStatFlagCaptures, 1);      // +1 flag captures

            // 5. Remove all related auras
            RemoveAssaultDebuffFromPlayer(player);

            GameObject gameObject = GetBgMap().GetGameObject(_flags[otherTeamId]);
            if (gameObject != null)
                player.RemoveAurasDueToSpell(gameObject.GetGoInfo().NewFlag.pickupSpell, gameObject.GetGUID());

            player.RemoveAurasDueToSpell((uint)SpellIds.QuickCapTimer);

            player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.PvPActive);

            RewardHonorToTeam(GetBonusHonorFromKill(2), team);

            // update last flag capture to be used if teamscore is equal
            SetLastFlagCapture(team);

            if (GetTeamScore(teamId) == MiscConst.MaxTeamScore)
                winner = team;

            if (winner != Team.Other)
            {
                UpdateWorldState((int)WorldStateIds.FlagStateAlliance, 1);
                UpdateWorldState((int)WorldStateIds.FlagStateHorde, 1);
                UpdateWorldState((int)WorldStateIds.StateTimerActive, 0);

                RewardHonorToTeam(MiscConst.Honor[Global.BattlegroundMgr.IsBGWeekend(GetTypeID()) ? 1 : 0][(int)Rewards.Win], winner);
                EndBattleground(winner);
            }
        }

        void SetLastFlagCapture(Team team) { _lastFlagCaptureTeam = team; }

        void AddPoint(Team team, uint Points = 1) { m_TeamScores[GetTeamIndexByTeamId(team)] += Points; }
        void SetTeamPoint(Team team, uint Points = 0) { m_TeamScores[GetTeamIndexByTeamId(team)] = Points; }
        void RemovePoint(Team team, uint Points = 1) { m_TeamScores[GetTeamIndexByTeamId(team)] -= Points; }
    }

    #region Constants
    struct MiscConst
    {
        public const uint MaxTeamScore = 3;
        public const uint FlagRespawnTime = 23000;
        public const uint FlagDropTime = 10000;
        public const uint SpellForceTime = 600000;
        public const uint SpellBrutalTime = 900000;

        public const uint ExploitTeleportLocationAlliance = 7051;
        public const uint ExploitTeleportLocationHorde = 7050;

        public const uint AtCapturePointAlliance = 30;
        public const uint AtCapturePointHorde = 31;

        public const uint WsEventStartBattle = 35912;

        public static TimeSpan FlagAssaultTimer = TimeSpan.FromSeconds(30);
        public const ushort FlagBrutalAssaultStackCount = 5;

        public static uint[][] Honor =
        {
            [20, 40, 40], // Normal Honor
            [60, 40, 80]  // Holiday
        };

        public const uint PvpStatFlagCaptures = 928;
        public const uint PvpStatFlagReturns = 929;
    }


    enum BroadcastTextIds
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
        HordeFlagReturned = 9809,
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

    enum SpellIds
    {
        WarsongFlag = 23333,
        WarsongFlagDropped = 23334,
        //WarsongFlagPicked     = 61266,
        SilverwingFlag = 23335,
        SilverwingFlagDropped = 23336,
        //SilverwingFlagPicked  = 61265,
        FocusedAssault = 46392,
        BrutalAssault = 46393,
        QuickCapTimer = 183317,   // Serverside

        //Carrierdebuffs
        CapturedAllianceCosmeticFx = 262508,
        CapturedHordeCosmeticFx = 262512,
    }

    enum WorldStateIds
    {
        FlagStateAlliance = 1545,
        FlagStateHorde = 1546,
        FlagStateNeutral = 1547,     // Unused
        HordeFlagCountPickedUp = 17712,    // Brawl
        AllianceFlagCountPickedUp = 17713,    // Brawl
        FlagCapturesAlliance = 1581,
        FlagCapturesHorde = 1582,
        FlagCapturesMax = 1601,
        FlagCapturesMaxNew = 17303,
        FlagControlHorde = 2338,
        FlagControlAlliance = 2339,
        StateTimer = 4248,
        StateTimerActive = 4247
    }

    enum GameobjectIds
    {
        // Doors
        AllianceDoor = 309704,
        Portcullis009 = 309705, // Doodad7neBlackrookPortcullis009
        Portcullis002 = 309883, // Doodad7neBlackrookPortcullis002
        CollisionPcSize = 242273,
        HordeGate1 = 352709,
        HordeGate2 = 352710,

        // Flags
        AllianceFlagInBase = 227741,
        HordeFlagInBase = 227740
    }

    enum Rewards
    {
        Win = 0,
        FlagCap,
        MapComplete,
        RewardNum
    }
    #endregion
}
