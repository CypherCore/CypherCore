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

namespace Scripts.Battlegrounds.EyeOfTheStorm
{
    enum WorldStateIds
    {
        AllianceResources = 1776,
        HordeResources = 1777,
        MaxResources = 1780,
        AllianceBase = 2752,
        HordeBase = 2753,
        DraeneiRuinsHordeControl = 2733,
        DraeneiRuinsAllianceControl = 2732,
        DraeneiRuinsUncontrol = 2731,
        MageTowerAllianceControl = 2730,
        MageTowerHordeControl = 2729,
        MageTowerUncontrol = 2728,
        FelReaverHordeControl = 2727,
        FelReaverAllianceControl = 2726,
        FelReaverUncontrol = 2725,
        BloodElfHordeControl = 2724,
        BloodElfAllianceControl = 2723,
        BloodElfUncontrol = 2722,
        ProgressBarPercentGrey = 2720,                 //100 = Empty (Only Grey), 0 = Blue|Red (No Grey)
        ProgressBarStatus = 2719,                 //50 Init!, 48 ... Hordak Bere .. 33 .. 0 = Full 100% Hordacky, 100 = Full Alliance
        ProgressBarShow = 2718,                 //1 Init, 0 Druhy Send - Bez Messagu, 1 = Controlled Aliance
        NetherstormFlag = 8863,
        //Set To 2 When Flag Is Picked Up, And To 1 If It Is Dropped
        NetherstormFlagStateAlliance = 9808,
        NetherstormFlagStateHorde = 9809,

        DraeneiRuinsHordeControlState = 17362,
        DraeneiRuinsAllianceControlState = 17366,
        MageTowerHordeControlState = 17361,
        MageTowerAllianceControlState = 17368,
        FelReaverHordeControlState = 17364,
        FelReaverAllianceControlState = 17367,
        BloodElfHordeControlState = 17363,
        BloodElfAllianceControlState = 17365,
    }

    enum SoundIds
    {
        //strange ids, but sure about them
        FlagPickedUpAlliance = 8212,
        FlagCapturedHorde = 8213,
        FlagPickedUpHorde = 8174,
        FlagCapturedAlliance = 8173,
        FlagReset = 8192
    }

    enum SpellIds
    {
        NetherstormFlag = 34976,
        PlayerDroppedFlag = 34991,

        // Focused/Brutal Assault
        FocusedAssault = 46392,
        BrutalAssault = 46393
    }

    enum GameObjectIds
    {
        ADoorEyEntry = 184719,           //Alliance Door
        HDoorEyEntry = 184720,           //Horde Door
        Flag2EyEntry = 208977,           //Netherstorm Flag (Flagstand)
        BeTowerCapEyEntry = 184080,           //Be Tower Cap Pt
        FrTowerCapEyEntry = 184081,           //Fel Reaver Cap Pt
        HuTowerCapEyEntry = 184082,           //Human Tower Cap Pt
        DrTowerCapEyEntry = 184083,           //Draenei Tower Cap Pt
    }

    enum Points
    {
        FelReaver = 0,
        BloodElf = 1,
        DraeneiRuins = 2,
        MageTower = 3,

        PlayersOutOfPoints = 4,
        Max = 4
    }

    enum EOSFlagState
    {
        OnBase = 0,
        WaitRespawn = 1,
        OnPlayer = 2,
        OnGround = 3
    }

    enum PointState
    {
        NoOwner = 0,
        StateUncontrolled = 0,
        UnderControl = 3
    }

    enum BroadcastTextIds
    {
        AllianceTakenFelReaverRuins = 17828,
        HordeTakenFelReaverRuins = 17829,
        AllianceLostFelReaverRuins = 91961,
        HordeLostFelReaverRuins = 91962,

        AllianceTakenBloodElfTower = 17819,
        HordeTakenBloodElfTower = 17823,
        AllianceLostBloodElfTower = 91957,
        HordeLostBloodElfTower = 91958,

        AllianceTakenDraeneiRuins = 17827,
        HordeTakenDraeneiRuins = 91917,
        AllianceLostDraeneiRuins = 91959,
        HordeLostDraeneiRuins = 91960,

        AllianceTakenMageTower = 17824,
        HordeTakenMageTower = 17825,
        AllianceLostMageTower = 91963,
        HordeLostMageTower = 91964,

        TakenFlag = 18359,
        FlagDropped = 18361,
        FlagReset = 18364,
        AllianceCapturedFlag = 18375,
        HordeCapturedFlag = 18384,
    }

    struct Misc
    {
        public const uint AreatriggerCaptureFlag = 33;

        public const uint WarningNearVictoryScore = 1200;
        public const uint MaxTeamScore = 1500;

        public const uint NotEYWeekendHonorTicks = 260;
        public const uint EYWeekendHonorTicks = 160;

        public const uint PvpStaFlagCaptures = 183;

        public static TimeSpan PointsTickTime = TimeSpan.FromSeconds(2);
        public static TimeSpan FlagAssaultTimer = TimeSpan.FromSeconds(30);
        public const ushort FlagBrutalAssaultStackCount = 5;
        public const uint EventStartBattle = 13180;

        public static byte[] TickPoints = { 1, 2, 5, 10 };
        public static uint[] FlagPoints = { 75, 85, 100, 500 };

        public const uint ExploitTeleportLocationAlliance = 3773;
        public const uint ExploitTeleportLocationHorde = 3772;

        public static EyeOfTheStormPointIconsStruct[] m_PointsIconStruct =
        {
            new(WorldStateIds.FelReaverUncontrol, WorldStateIds.FelReaverAllianceControl, WorldStateIds.FelReaverHordeControl, WorldStateIds.FelReaverAllianceControlState, WorldStateIds.FelReaverHordeControlState),
            new(WorldStateIds.BloodElfUncontrol, WorldStateIds.BloodElfAllianceControl, WorldStateIds.BloodElfHordeControl, WorldStateIds.BloodElfAllianceControlState, WorldStateIds.BloodElfHordeControlState),
            new(WorldStateIds.DraeneiRuinsUncontrol, WorldStateIds.DraeneiRuinsAllianceControl, WorldStateIds.DraeneiRuinsHordeControl, WorldStateIds.DraeneiRuinsAllianceControlState, WorldStateIds.DraeneiRuinsHordeControlState),
            new(WorldStateIds.MageTowerUncontrol, WorldStateIds.MageTowerAllianceControl, WorldStateIds.MageTowerHordeControl, WorldStateIds.MageTowerAllianceControlState, WorldStateIds.MageTowerHordeControlState)
        };

        public static EyeOfTheStormTextIdStruct[] m_LosingPointTypes =
        {
            new(BroadcastTextIds.AllianceLostFelReaverRuins, BroadcastTextIds.HordeLostFelReaverRuins),
            new(BroadcastTextIds.AllianceLostBloodElfTower, BroadcastTextIds.HordeLostBloodElfTower),
            new(BroadcastTextIds.AllianceLostDraeneiRuins, BroadcastTextIds.HordeLostDraeneiRuins),
            new(BroadcastTextIds.AllianceLostMageTower, BroadcastTextIds.HordeLostMageTower)
        };

        public static EyeOfTheStormTextIdStruct[] m_CapturingPointTypes =
        {
            new(BroadcastTextIds.AllianceTakenFelReaverRuins, BroadcastTextIds.HordeTakenFelReaverRuins),
            new(BroadcastTextIds.AllianceTakenBloodElfTower, BroadcastTextIds.HordeTakenBloodElfTower),
            new(BroadcastTextIds.AllianceTakenDraeneiRuins, BroadcastTextIds.HordeTakenDraeneiRuins),
            new(BroadcastTextIds.AllianceTakenMageTower, BroadcastTextIds.HordeTakenMageTower)
        };
    }

    struct EyeOfTheStormPointIconsStruct
    {
        public int WorldStateControlIndex;
        public int WorldStateAllianceControlledIndex;
        public int WorldStateHordeControlledIndex;
        public int WorldStateAllianceStatusBarIcon;
        public int WorldStateHordeStatusBarIcon;

        public EyeOfTheStormPointIconsStruct(WorldStateIds worldStateControlIndex, WorldStateIds worldStateAllianceControlledIndex, WorldStateIds worldStateHordeControlledIndex, WorldStateIds worldStateAllianceStatusBarIcon, WorldStateIds worldStateHordeStatusBarIcon)
        {
            WorldStateControlIndex = (int)worldStateControlIndex;
            WorldStateAllianceControlledIndex = (int)worldStateAllianceControlledIndex;
            WorldStateHordeControlledIndex = (int)worldStateHordeControlledIndex;
            WorldStateAllianceStatusBarIcon = (int)worldStateAllianceStatusBarIcon;
            WorldStateHordeStatusBarIcon = (int)worldStateHordeStatusBarIcon;
        }
    }

    struct EyeOfTheStormTextIdStruct
    {
        public uint MessageIdAlliance;
        public uint MessageIdHorde;

        public EyeOfTheStormTextIdStruct(BroadcastTextIds messageIdAlliance, BroadcastTextIds messageIdHorde)
        {
            MessageIdAlliance = (uint)messageIdAlliance;
            MessageIdHorde = (uint)messageIdHorde;
        }
    }

    class BattlegroundEYControlZoneHandler : ControlZoneHandler
    {
        uint _point;

        public BattlegroundEYControlZoneHandler(Points point)
        {
            _point = (uint)point;
        }

        public uint GetPoint() { return _point; }
    }

    [Script(nameof(battleground_eye_of_the_storm), 566)]
    class battleground_eye_of_the_storm : BattlegroundScript
    {
        uint[] _honorScoreTics = new uint[SharedConst.PvpTeamsCount];

        TimeTracker _pointsTimer = new();
        uint _honorTics;

        Dictionary<uint, BattlegroundEYControlZoneHandler> _controlZoneHandlers = new();
        List<ObjectGuid> _doorGUIDs = new();
        ObjectGuid _flagGUID;

        // Focused/Brutal Assault
        bool _assaultEnabled;
        TimeTracker _flagAssaultTimer;
        byte _assaultStackCount;

        public battleground_eye_of_the_storm(BattlegroundMap map) : base(map)
        {
            _honorTics = 0;
            _pointsTimer = new(Misc.PointsTickTime);
            _assaultEnabled = false;
            _assaultStackCount = 0;
            _flagAssaultTimer = new(Misc.FlagAssaultTimer);

            _controlZoneHandlers[(int)GameObjectIds.FrTowerCapEyEntry] = new(Points.FelReaver);
            _controlZoneHandlers[(int)GameObjectIds.BeTowerCapEyEntry] = new(Points.BloodElf);
            _controlZoneHandlers[(int)GameObjectIds.DrTowerCapEyEntry] = new(Points.DraeneiRuins);
            _controlZoneHandlers[(int)GameObjectIds.HuTowerCapEyEntry] = new(Points.MageTower);

            bool isBGWeekend = Global.BattlegroundMgr.IsBGWeekend(battleground.GetTypeID());
            _honorTics = (isBGWeekend) ? Misc.EYWeekendHonorTicks : Misc.NotEYWeekendHonorTicks;
        }

        public override void OnInit()
        {
            UpdateWorldState((int)WorldStateIds.MaxResources, (int)Misc.MaxTeamScore);
        }

        public override void OnUpdate(uint diff)
        {
            if (battleground.GetStatus() != BattlegroundStatus.InProgress)
                return;

            _pointsTimer.Update(diff);
            if (_pointsTimer.Passed())
            {
                _pointsTimer.Reset(Misc.PointsTickTime);

                byte baseCountAlliance = GetControlledBaseCount(BattleGroundTeamId.Alliance);
                byte baseCountHorde = GetControlledBaseCount(BattleGroundTeamId.Horde);
                if (baseCountAlliance > 0)
                    AddPoint(Team.Alliance, Misc.TickPoints[baseCountAlliance - 1]);
                if (baseCountHorde > 0)
                    AddPoint(Team.Horde, Misc.TickPoints[baseCountHorde - 1]);
            }

            if (_assaultEnabled)
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
            foreach (ObjectGuid door in _doorGUIDs)
            {
                GameObject gameObject = battlegroundMap.GetGameObject(door);
                if (gameObject != null)
                {
                    gameObject.UseDoorOrButton();
                    gameObject.DespawnOrUnsummon(TimeSpan.FromSeconds(3));
                }
            }

            // Achievement: Flurry
            TriggerGameEvent(Misc.EventStartBattle);
        }

        void AddPoint(Team team, uint points)
        {
            battleground.AddPoint(team, points);
            int team_index = Battleground.GetTeamIndexByTeamId(team);
            _honorScoreTics[team_index] += points;
            if (_honorScoreTics[team_index] >= _honorTics)
            {
                battleground.RewardHonorToTeam(battleground.GetBonusHonorFromKill(1), team);
                _honorScoreTics[team_index] -= _honorTics;
            }

            UpdateTeamScore(team_index);
        }

        byte GetControlledBaseCount(int teamId)
        {
            byte baseCount = 0;
            foreach (var (_, controlZoneHandler) in _controlZoneHandlers)
            {
                uint point = controlZoneHandler.GetPoint();
                switch (teamId)
                {
                    case BattleGroundTeamId.Alliance:
                        if (battlegroundMap.GetWorldStateValue(Misc.m_PointsIconStruct[point].WorldStateAllianceControlledIndex) == 1)
                            baseCount++;
                        break;
                    case BattleGroundTeamId.Horde:
                        if (battlegroundMap.GetWorldStateValue(Misc.m_PointsIconStruct[point].WorldStateHordeControlledIndex) == 1)
                            baseCount++;
                        break;
                    default:
                        break;
                }
            }
            return baseCount;
        }

        void DoForFlagKeepers(Action<Player> action)
        {
            GameObject flag = battlegroundMap.GetGameObject(_flagGUID);
            if (flag != null)
            {
                Player carrier = Global.ObjAccessor.FindPlayer(flag.GetFlagCarrierGUID());
                if (carrier != null)
                    action(carrier);
            }
        }

        void ResetAssaultDebuff()
        {
            _assaultEnabled = false;
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

        void UpdateTeamScore(int team)
        {
            uint score = battleground.GetTeamScore(team);

            if (score >= Misc.MaxTeamScore)
            {
                score = Misc.MaxTeamScore;
                if (team == BattleGroundTeamId.Alliance)
                    battleground.EndBattleground(Team.Alliance);
                else
                    battleground.EndBattleground(Team.Horde);
            }

            if (team == BattleGroundTeamId.Alliance)
                UpdateWorldState((int)WorldStateIds.AllianceResources, (int)score);
            else
                UpdateWorldState((int)WorldStateIds.HordeResources, (int)score);
        }

        public override void OnEnd(Team winner)
        {
            base.OnEnd(winner);
            // Win reward
            if (winner == Team.Alliance)
                battleground.RewardHonorToTeam(battleground.GetBonusHonorFromKill(1), Team.Alliance);
            if (winner == Team.Horde)
                battleground.RewardHonorToTeam(battleground.GetBonusHonorFromKill(1), Team.Horde);

            // Complete map reward
            battleground.RewardHonorToTeam(battleground.GetBonusHonorFromKill(1), Team.Alliance);
            battleground.RewardHonorToTeam(battleground.GetBonusHonorFromKill(1), Team.Horde);
        }

        void UpdatePointsCount(int teamId)
        {
            if (teamId == BattleGroundTeamId.Alliance)
                UpdateWorldState((int)WorldStateIds.AllianceBase, GetControlledBaseCount(BattleGroundTeamId.Alliance));
            else
                UpdateWorldState((int)WorldStateIds.HordeBase, GetControlledBaseCount(BattleGroundTeamId.Horde));
        }

        public override void OnGameObjectCreate(GameObject gameObject)
        {
            switch ((GameObjectIds)gameObject.GetEntry())
            {
                case GameObjectIds.ADoorEyEntry:
                case GameObjectIds.HDoorEyEntry:
                    _doorGUIDs.Add(gameObject.GetGUID());
                    break;
                case GameObjectIds.Flag2EyEntry:
                    _flagGUID = gameObject.GetGUID();
                    break;
                default:
                    break;
            }
        }

        public override bool CanCaptureFlag(AreaTrigger areaTrigger, Player player)
        {
            if (areaTrigger.GetEntry() != Misc.AreatriggerCaptureFlag)
                return false;

            GameObject flag = battlegroundMap.GetGameObject(_flagGUID);
            if (flag != null)
            {
                if (flag.GetFlagCarrierGUID() != player.GetGUID())
                    return false;
            }
            GameObject controlzone = player.FindNearestGameObjectWithOptions(40.0f, new() { StringId = "bg_eye_of_the_storm_control_zone" });
            if (controlzone != null)
            {
                uint point = _controlZoneHandlers[controlzone.GetEntry()].GetPoint();
                switch (battleground.GetPlayerTeam(player.GetGUID()))
                {
                    case Team.Alliance:
                        return battlegroundMap.GetWorldStateValue(Misc.m_PointsIconStruct[point].WorldStateAllianceControlledIndex) == 1;
                    case Team.Horde:
                        return battlegroundMap.GetWorldStateValue(Misc.m_PointsIconStruct[point].WorldStateHordeControlledIndex) == 1;
                    default:
                        return false;
                }
            }

            return false;
        }

        public override void OnCaptureFlag(AreaTrigger areaTrigger, Player player)
        {
            if (areaTrigger.GetEntry() != Misc.AreatriggerCaptureFlag)
                return;

            uint baseCount = GetControlledBaseCount(Battleground.GetTeamIndexByTeamId(battleground.GetPlayerTeam(player.GetGUID())));

            GameObject gameObject = battlegroundMap.GetGameObject(_flagGUID);
            if (gameObject != null)
                gameObject.HandleCustomTypeCommand(new Game.Entities.GameObjectType.SetNewFlagState(FlagState.Respawning, player));

            Team team = battleground.GetPlayerTeam(player.GetGUID());
            if (team == Team.Alliance)
            {
                battleground.SendBroadcastText((uint)BroadcastTextIds.AllianceCapturedFlag, ChatMsg.BgSystemAlliance, player);
                battleground.PlaySoundToAll((uint)SoundIds.FlagCapturedAlliance);
            }
            else
            {
                battleground.SendBroadcastText((uint)BroadcastTextIds.HordeCapturedFlag, ChatMsg.BgSystemHorde, player);
                battleground.PlaySoundToAll((uint)SoundIds.FlagCapturedHorde);
            }

            if (baseCount > 0)
                AddPoint(team, Misc.FlagPoints[baseCount - 1]);

            UpdateWorldState((int)WorldStateIds.NetherstormFlagStateHorde, (int)EOSFlagState.OnBase);
            UpdateWorldState((int)WorldStateIds.NetherstormFlagStateAlliance, (int)EOSFlagState.OnBase);

            battleground.UpdatePvpStat(player, Misc.PvpStaFlagCaptures, 1);

            ResetAssaultDebuff();
            player.RemoveAurasDueToSpell((uint)SpellIds.NetherstormFlag);
            player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.PvPActive);
        }

        public override void OnFlagStateChange(GameObject flagInBase, FlagState oldValue, FlagState newValue, Player player)
        {
            switch (newValue)
            {
                case FlagState.InBase:
                    ResetAssaultDebuff();
                    break;
                case FlagState.Dropped:
                    player.CastSpell(player, BattlegroundConst.SpellRecentlyDroppedNeutralFlag, true);
                    RemoveAssaultDebuffFromPlayer(player);

                    UpdateWorldState((int)WorldStateIds.NetherstormFlagStateHorde, (int)EOSFlagState.WaitRespawn);
                    UpdateWorldState((int)WorldStateIds.NetherstormFlagStateAlliance, (int)EOSFlagState.WaitRespawn);

                    if (battleground.GetPlayerTeam(player.GetGUID()) == Team.Alliance)
                        battleground.SendBroadcastText((uint)BroadcastTextIds.FlagDropped, ChatMsg.BgSystemAlliance);
                    else
                        battleground.SendBroadcastText((uint)BroadcastTextIds.FlagDropped, ChatMsg.BgSystemHorde);
                    break;
                case FlagState.Taken:
                    if (battleground.GetPlayerTeam(player.GetGUID()) == Team.Alliance)
                    {
                        UpdateWorldState((int)WorldStateIds.NetherstormFlagStateAlliance, (int)EOSFlagState.OnPlayer);
                        battleground.PlaySoundToAll((uint)SoundIds.FlagPickedUpAlliance);
                        battleground.SendBroadcastText((uint)BroadcastTextIds.TakenFlag, ChatMsg.BgSystemAlliance, player);
                    }
                    else
                    {
                        UpdateWorldState((int)WorldStateIds.NetherstormFlagStateHorde, (int)EOSFlagState.OnPlayer);
                        battleground.PlaySoundToAll((uint)SoundIds.FlagPickedUpHorde);
                        battleground.SendBroadcastText((uint)BroadcastTextIds.TakenFlag, ChatMsg.BgSystemHorde, player);
                    }

                    ApplyAssaultDebuffToPlayer(player);
                    _assaultEnabled = true;

                    player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.PvPActive);
                    break;
                case FlagState.Respawning:
                    ResetAssaultDebuff();
                    break;
                default:
                    break;
            }

            UpdateWorldState((int)WorldStateIds.NetherstormFlag, (int)newValue);
        }

        void EventTeamLostPoint(int teamId, uint point, GameObject controlZone)
        {
            if (teamId == BattleGroundTeamId.Alliance)
            {
                battleground.SendBroadcastText(Misc.m_LosingPointTypes[point].MessageIdAlliance, ChatMsg.BgSystemAlliance, controlZone);
                UpdateWorldState(Misc.m_PointsIconStruct[point].WorldStateAllianceControlledIndex, 0);
            }
            else if (teamId == BattleGroundTeamId.Horde)
            {
                battleground.SendBroadcastText(Misc.m_LosingPointTypes[point].MessageIdHorde, ChatMsg.BgSystemHorde, controlZone);
                UpdateWorldState(Misc.m_PointsIconStruct[point].WorldStateHordeControlledIndex, 0);
            }

            UpdateWorldState(Misc.m_PointsIconStruct[point].WorldStateControlIndex, 1);
            UpdatePointsCount(teamId);
        }

        void EventTeamCapturedPoint(int teamId, uint point, GameObject controlZone)
        {
            if (teamId == BattleGroundTeamId.Alliance)
            {
                battleground.SendBroadcastText(Misc.m_CapturingPointTypes[point].MessageIdAlliance, ChatMsg.BgSystemAlliance, controlZone);
                UpdateWorldState(Misc.m_PointsIconStruct[point].WorldStateAllianceControlledIndex, 1);
            }
            else if (teamId == BattleGroundTeamId.Horde)
            {
                battleground.SendBroadcastText(Misc.m_CapturingPointTypes[point].MessageIdHorde, ChatMsg.BgSystemHorde, controlZone);
                UpdateWorldState(Misc.m_PointsIconStruct[point].WorldStateHordeControlledIndex, 1);
            }

            UpdateWorldState(Misc.m_PointsIconStruct[point].WorldStateControlIndex, 0);
            UpdatePointsCount(teamId);
        }

        public override Team GetPrematureWinner()
        {
            if (battleground.GetTeamScore(BattleGroundTeamId.Alliance) > battleground.GetTeamScore(BattleGroundTeamId.Horde))
                return Team.Alliance;

            if (battleground.GetTeamScore(BattleGroundTeamId.Horde) > battleground.GetTeamScore(BattleGroundTeamId.Alliance))
                return Team.Horde;

            return base.GetPrematureWinner();
        }

        public override void ProcessEvent(WorldObject obj, uint eventId, WorldObject invoker)
        {
            base.ProcessEvent(obj, eventId, invoker);

            if (invoker != null)
            {
                GameObject gameobject = invoker.ToGameObject();
                if (gameobject != null)
                {
                    if (gameobject.GetGoType() == GameObjectTypes.ControlZone)
                    {
                        if (!_controlZoneHandlers.ContainsKey(gameobject.GetEntry()))
                            return;

                        var controlzone = gameobject.GetGoInfo().ControlZone;
                        BattlegroundEYControlZoneHandler handler = _controlZoneHandlers[invoker.GetEntry()];
                        if (eventId == controlzone.NeutralEventAlliance)
                            EventTeamLostPoint(BattleGroundTeamId.Alliance, handler.GetPoint(), gameobject);
                        else if (eventId == controlzone.NeutralEventHorde)
                            EventTeamLostPoint(BattleGroundTeamId.Horde, handler.GetPoint(), gameobject);
                        else if (eventId == controlzone.ProgressEventAlliance)
                            EventTeamCapturedPoint(BattleGroundTeamId.Alliance, handler.GetPoint(), gameobject);
                        else if (eventId == controlzone.ProgressEventHorde)
                            EventTeamCapturedPoint(BattleGroundTeamId.Horde, handler.GetPoint(), gameobject);
                    }
                }
            }
        }
    }
}
