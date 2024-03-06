// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Entities.GameObjectType;
using Game.Maps;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Game.BattleGrounds.Zones.EyeofStorm
{
    class BgEyeofStorm : Battleground
    {
        uint[] m_HonorScoreTics = new uint[SharedConst.PvpTeamsCount];
        uint m_FlagCapturedBgObjectType;                  // type that should be despawned when flag is captured

        TimeTracker _pointsTimer;
        uint m_HonorTics;

        Dictionary<uint, BgEyeOfStormControlZoneHandler> _controlZoneHandlers = new();
        List<ObjectGuid> _doorGUIDs = new();
        ObjectGuid _flagGUID;

        // Focused/Brutal Assault
        bool _assaultEnabled;
        TimeTracker _flagAssaultTimer;
        byte _assaultStackCount;

        public BgEyeofStorm(BattlegroundTemplate battlegroundTemplate) : base(battlegroundTemplate)
        {
            m_HonorScoreTics = [0, 0];
            m_FlagCapturedBgObjectType = 0;
            m_HonorTics = 0;
            _pointsTimer = new(MiscConst.PointsTickTime);
            _assaultEnabled = false;
            _assaultStackCount = 0;
            _flagAssaultTimer = new(MiscConst.FlagAssaultTimer);
        }

        public override void PostUpdateImpl(uint diff)
        {
            if (GetStatus() == BattlegroundStatus.InProgress)
            {
                _pointsTimer.Update(diff);
                if (_pointsTimer.Passed())
                {
                    _pointsTimer.Reset(MiscConst.PointsTickTime);

                    byte baseCountAlliance = GetControlledBaseCount(BattleGroundTeamId.Alliance);
                    byte baseCountHorde = GetControlledBaseCount(BattleGroundTeamId.Horde);
                    if (baseCountAlliance > 0)
                        AddPoints(Team.Alliance, MiscConst.TickPoints[baseCountAlliance - 1]);
                    if (baseCountHorde > 0)
                        AddPoints(Team.Horde, MiscConst.TickPoints[baseCountHorde - 1]);
                }

                if (_assaultEnabled)
                {
                    _flagAssaultTimer.Update(diff);
                    if (_flagAssaultTimer.Passed())
                    {
                        _flagAssaultTimer.Reset(MiscConst.FlagAssaultTimer);
                        _assaultStackCount++;

                        // update assault debuff stacks
                        DoForFlagKeepers(player => ApplyAssaultDebuffToPlayer(player));
                    }
                }
            }
        }

        public override void StartingEventOpenDoors()
        {
            foreach (ObjectGuid door in _doorGUIDs)
            {
                GameObject gameObject = GetBgMap().GetGameObject(door);
                if (gameObject != null)
                {
                    gameObject.UseDoorOrButton();
                    gameObject.DespawnOrUnsummon(TimeSpan.FromSeconds(3));
                }
            }

            // Achievement: Flurry
            TriggerGameEvent(MiscConst.EventStartBattle);
        }

        void AddPoints(Team Team, uint Points)
        {
            int team_index = GetTeamIndexByTeamId(Team);
            m_TeamScores[team_index] += Points;
            m_HonorScoreTics[team_index] += Points;
            if (m_HonorScoreTics[team_index] >= m_HonorTics)
            {
                RewardHonorToTeam(GetBonusHonorFromKill(1), Team);
                m_HonorScoreTics[team_index] -= m_HonorTics;
            }
            UpdateTeamScore(team_index);
        }

        byte GetControlledBaseCount(int teamId)
        {
            byte baseCount = 0;
            foreach (var controlZoneHandler in _controlZoneHandlers)
            {
                uint point = controlZoneHandler.Value.GetPoint();
                switch (teamId)
                {
                    case BattleGroundTeamId.Alliance:
                        if (GetBgMap().GetWorldStateValue(MiscConst.m_PointsIconStruct[point].WorldStateAllianceControlledIndex) == 1)
                            baseCount++;
                        break;
                    case BattleGroundTeamId.Horde:
                        if (GetBgMap().GetWorldStateValue(MiscConst.m_PointsIconStruct[point].WorldStateHordeControlledIndex) == 1)
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
            GameObject flag = GetBgMap().GetGameObject(_flagGUID);
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
            _flagAssaultTimer.Reset(MiscConst.FlagAssaultTimer);
            DoForFlagKeepers(RemoveAssaultDebuffFromPlayer);
        }

        void ApplyAssaultDebuffToPlayer(Player player)
        {
            if (_assaultStackCount == 0)
                return;

            uint spellId = MiscConst.SpellFocusedAssault;
            if (_assaultStackCount >= MiscConst.FlagBrutalAssaultStackCount)
            {
                player.RemoveAurasDueToSpell(MiscConst.SpellFocusedAssault);
                spellId = MiscConst.SpellBrutalAssault;
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
            player.RemoveAurasDueToSpell(MiscConst.SpellFocusedAssault);
            player.RemoveAurasDueToSpell(MiscConst.SpellBrutalAssault);
        }

        void UpdateTeamScore(int team)
        {
            uint score = GetTeamScore(team);
            if (score >= ScoreIds.MaxTeamScore)
            {
                score = ScoreIds.MaxTeamScore;
                if (team == BattleGroundTeamId.Alliance)
                    EndBattleground(Team.Alliance);
                else
                    EndBattleground(Team.Horde);
            }

            if (team == BattleGroundTeamId.Alliance)
                UpdateWorldState(WorldStateIds.AllianceResources, (int)score);
            else
                UpdateWorldState(WorldStateIds.HordeResources, (int)score);
        }

        public override void EndBattleground(Team winner)
        {
            // Win reward
            if (winner == Team.Alliance)
                RewardHonorToTeam(GetBonusHonorFromKill(1), Team.Alliance);
            if (winner == Team.Horde)
                RewardHonorToTeam(GetBonusHonorFromKill(1), Team.Horde);

            // Complete map reward
            RewardHonorToTeam(GetBonusHonorFromKill(1), Team.Alliance);
            RewardHonorToTeam(GetBonusHonorFromKill(1), Team.Horde);

            base.EndBattleground(winner);
        }

        void UpdatePointsCount(uint teamId)
        {
            if (teamId == BattleGroundTeamId.Alliance)
                UpdateWorldState(WorldStateIds.AllianceBase, GetControlledBaseCount(BattleGroundTeamId.Alliance));
            else
                UpdateWorldState(WorldStateIds.HordeBase, GetControlledBaseCount(BattleGroundTeamId.Horde));
        }

        public override void OnGameObjectCreate(GameObject gameobject)
        {
            switch (gameobject.GetEntry())
            {
                case GameobjectIds.ADoorEyEntry:
                case GameobjectIds.HDoorEyEntry:
                    _doorGUIDs.Add(gameobject.GetGUID());
                    break;
                case GameobjectIds.Flag2EyEntry:
                    _flagGUID = gameobject.GetGUID();
                    break;
                default:
                    break;
            }
        }

        public override bool CanCaptureFlag(AreaTrigger areaTrigger, Player player)
        {
            if (areaTrigger.GetEntry() != MiscConst.AreatriggerCaptureFlag)
                return false;

            GameObject flag = GetBgMap().GetGameObject(_flagGUID);
            if (flag != null)
            {
                if (flag.GetFlagCarrierGUID() != player.GetGUID())
                    return false;
            }

            GameObject controlzone = player.FindNearestGameObjectWithOptions(40.0f, new FindGameObjectOptions() { StringId = "bg_eye_of_the_storm_control_zone" });
            if (controlzone != null)
            {
                uint point = _controlZoneHandlers[controlzone.GetEntry()].GetPoint();
                switch (GetPlayerTeam(player.GetGUID()))
                {
                    case Team.Alliance:
                        return GetBgMap().GetWorldStateValue(MiscConst.m_PointsIconStruct[point].WorldStateAllianceControlledIndex) == 1;
                    case Team.Horde:
                        return GetBgMap().GetWorldStateValue(MiscConst.m_PointsIconStruct[point].WorldStateHordeControlledIndex) == 1;
                    default:
                        return false;
                }
            }

            return false;
        }

        public override void OnCaptureFlag(AreaTrigger areaTrigger, Player player)
        {
            if (areaTrigger.GetEntry() != MiscConst.AreatriggerCaptureFlag)
                return;

            uint baseCount = GetControlledBaseCount(GetTeamIndexByTeamId(GetPlayerTeam(player.GetGUID())));

            GameObject gameObject = GetBgMap().GetGameObject(_flagGUID);
            if (gameObject != null)
                gameObject.HandleCustomTypeCommand(new SetNewFlagState(FlagState.Respawning, player));

            Team team = GetPlayerTeam(player.GetGUID());
            if (team == Team.Alliance)
            {
                SendBroadcastText(BroadcastTextIds.AllianceCapturedFlag, ChatMsg.BgSystemAlliance, player);
                PlaySoundToAll(SoundIds.FlagCapturedAlliance);
            }
            else
            {
                SendBroadcastText(BroadcastTextIds.HordeCapturedFlag, ChatMsg.BgSystemHorde, player);
                PlaySoundToAll(SoundIds.FlagCapturedHorde);
            }

            if (baseCount > 0)
                AddPoints(team, MiscConst.FlagPoints[baseCount - 1]);

            UpdateWorldState(WorldStateIds.NetherstormFlagStateHorde, (int)EYFlagState.OnBase);
            UpdateWorldState(WorldStateIds.NetherstormFlagStateAlliance, (int)EYFlagState.OnBase);

            UpdatePvpStat(player, MiscConst.PvpStatFlagCaptures, 1);

            player.RemoveAurasDueToSpell(MiscConst.SpellNetherstormFlag);
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

                    UpdateWorldState(WorldStateIds.NetherstormFlagStateHorde, (int)EYFlagState.WaitRespawn);
                    UpdateWorldState(WorldStateIds.NetherstormFlagStateAlliance, (int)EYFlagState.WaitRespawn);

                    if (GetPlayerTeam(player.GetGUID()) == Team.Alliance)
                        SendBroadcastText(BroadcastTextIds.FlagDropped, ChatMsg.BgSystemAlliance);
                    else
                        SendBroadcastText(BroadcastTextIds.FlagDropped, ChatMsg.BgSystemHorde);
                    break;
                case FlagState.Taken:
                    if (GetPlayerTeam(player.GetGUID()) == Team.Alliance)
                    {
                        UpdateWorldState(WorldStateIds.NetherstormFlagStateAlliance, (int)EYFlagState.OnPlayer);
                        PlaySoundToAll(SoundIds.FlagPickedUpAlliance);
                        SendBroadcastText(BroadcastTextIds.TakenFlag, ChatMsg.BgSystemAlliance, player);
                    }
                    else
                    {
                        UpdateWorldState(WorldStateIds.NetherstormFlagStateHorde, (int)EYFlagState.OnPlayer);
                        PlaySoundToAll(SoundIds.FlagPickedUpHorde);
                        SendBroadcastText(BroadcastTextIds.TakenFlag, ChatMsg.BgSystemHorde, player);
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

            UpdateWorldState(WorldStateIds.NetherstormFlag, (int)newValue);
        }

        public override bool SetupBattleground()
        {
            UpdateWorldState(WorldStateIds.MaxResources, (int)ScoreIds.MaxTeamScore);

            _controlZoneHandlers[GameobjectIds.FrTowerCapEyEntry] = new BgEyeOfStormControlZoneHandler(this, Points.FelReaver);
            _controlZoneHandlers[GameobjectIds.BeTowerCapEyEntry] = new BgEyeOfStormControlZoneHandler(this, Points.BloodElf);
            _controlZoneHandlers[GameobjectIds.DrTowerCapEyEntry] = new BgEyeOfStormControlZoneHandler(this, Points.DraeneiRuins);
            _controlZoneHandlers[GameobjectIds.HuTowerCapEyEntry] = new BgEyeOfStormControlZoneHandler(this, Points.MageTower);

            return true;
        }

        public override void Reset()
        {
            //call parent's class reset
            base.Reset();

            m_TeamScores[BattleGroundTeamId.Alliance] = 0;
            m_TeamScores[BattleGroundTeamId.Horde] = 0;
            m_HonorScoreTics = [0, 0];
            m_FlagCapturedBgObjectType = 0;
            bool isBGWeekend = Global.BattlegroundMgr.IsBGWeekend(GetTypeID());
            m_HonorTics = isBGWeekend ? MiscConst.EYWeekendHonorTicks : MiscConst.NotEYWeekendHonorTicks;
        }

        public override void HandleKillPlayer(Player player, Player killer)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            base.HandleKillPlayer(player, killer);
            EventPlayerDroppedFlag(player);
        }

        public void EventTeamLostPoint(uint teamId, uint point, WorldObject controlZone)
        {
            if (teamId == BattleGroundTeamId.Alliance)
            {
                SendBroadcastText(MiscConst.m_LosingPointTypes[point].MessageIdAlliance, ChatMsg.BgSystemAlliance, controlZone);
                UpdateWorldState(MiscConst.m_PointsIconStruct[point].WorldStateAllianceControlledIndex, 0);
            }
            else if (teamId == BattleGroundTeamId.Horde)
            {
                SendBroadcastText(MiscConst.m_LosingPointTypes[point].MessageIdHorde, ChatMsg.BgSystemHorde, controlZone);
                UpdateWorldState(MiscConst.m_PointsIconStruct[point].WorldStateHordeControlledIndex, 0);
            }

            UpdateWorldState(MiscConst.m_PointsIconStruct[point].WorldStateControlIndex, 1);
            UpdatePointsCount(teamId);
        }

        public void EventTeamCapturedPoint(uint teamId, uint point, WorldObject controlZone)
        {
            if (teamId == BattleGroundTeamId.Alliance)
            {
                SendBroadcastText(MiscConst.m_CapturingPointTypes[point].MessageIdAlliance, ChatMsg.BgSystemAlliance, controlZone);
                UpdateWorldState(MiscConst.m_PointsIconStruct[point].WorldStateAllianceControlledIndex, 1);
            }
            else if (teamId == BattleGroundTeamId.Horde)
            {
                SendBroadcastText(MiscConst.m_CapturingPointTypes[point].MessageIdHorde, ChatMsg.BgSystemHorde, controlZone);
                UpdateWorldState(MiscConst.m_PointsIconStruct[point].WorldStateHordeControlledIndex, 1);
            }

            UpdateWorldState(MiscConst.m_PointsIconStruct[point].WorldStateControlIndex, 0);
            UpdatePointsCount(teamId);
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

        public override void ProcessEvent(WorldObject target, uint eventId, WorldObject invoker)
        {
            base.ProcessEvent(target, eventId, invoker);

            if (invoker != null)
            {
                GameObject gameobject = invoker.ToGameObject();
                if (gameobject != null)
                {
                    if (gameobject.GetGoType() == GameObjectTypes.ControlZone)
                    {
                        if (!_controlZoneHandlers.TryGetValue(gameobject.GetEntry(), out BgEyeOfStormControlZoneHandler handler))
                            return;

                        var controlzone = gameobject.GetGoInfo().ControlZone;
                        if (eventId == controlzone.NeutralEventAlliance)
                            handler.HandleNeutralEventAlliance(gameobject);
                        else if (eventId == controlzone.NeutralEventHorde)
                            handler.HandleNeutralEventHorde(gameobject);
                        else if (eventId == controlzone.ProgressEventAlliance)
                            handler.HandleProgressEventAlliance(gameobject);
                        else if (eventId == controlzone.ProgressEventHorde)
                            handler.HandleProgressEventHorde(gameobject);
                    }
                }
            }
        }

        void RemovePoint(Team team, uint Points = 1) { m_TeamScores[GetTeamIndexByTeamId(team)] -= Points; }

        void SetTeamPoint(Team team, uint Points = 0) { m_TeamScores[GetTeamIndexByTeamId(team)] = Points; }
    }

    struct BgEyeOfStormPointIconsStruct
    {
        public BgEyeOfStormPointIconsStruct(uint worldStateControlIndex, int worldStateAllianceControlledIndex, int worldStateHordeControlledIndex, uint worldStateAllianceStatusBarIcon, uint worldStateHordeStatusBarIcon)
        {
            WorldStateControlIndex = worldStateControlIndex;
            WorldStateAllianceControlledIndex = worldStateAllianceControlledIndex;
            WorldStateHordeControlledIndex = worldStateHordeControlledIndex;
            WorldStateAllianceStatusBarIcon = worldStateAllianceStatusBarIcon;
            WorldStateHordeStatusBarIcon = worldStateHordeStatusBarIcon;
        }

        public uint WorldStateControlIndex;
        public int WorldStateAllianceControlledIndex;
        public int WorldStateHordeControlledIndex;
        public uint WorldStateAllianceStatusBarIcon;
        public uint WorldStateHordeStatusBarIcon;
    }

    struct BgEyeOfStormLosingPointStruct
    {
        public BgEyeOfStormLosingPointStruct(int _SpawnNeutralObjectType, int _DespawnObjectTypeAlliance, uint _MessageIdAlliance, int _DespawnObjectTypeHorde, uint _MessageIdHorde)
        {
            SpawnNeutralObjectType = _SpawnNeutralObjectType;
            DespawnObjectTypeAlliance = _DespawnObjectTypeAlliance;
            MessageIdAlliance = _MessageIdAlliance;
            DespawnObjectTypeHorde = _DespawnObjectTypeHorde;
            MessageIdHorde = _MessageIdHorde;
        }

        public int SpawnNeutralObjectType;
        public int DespawnObjectTypeAlliance;
        public uint MessageIdAlliance;
        public int DespawnObjectTypeHorde;
        public uint MessageIdHorde;
    }

    struct BgEyeOfStormCapturingPointStruct
    {
        public BgEyeOfStormCapturingPointStruct(int _DespawnNeutralObjectType, int _SpawnObjectTypeAlliance, uint _MessageIdAlliance, int _SpawnObjectTypeHorde, uint _MessageIdHorde, uint _GraveyardId)
        {
            DespawnNeutralObjectType = _DespawnNeutralObjectType;
            SpawnObjectTypeAlliance = _SpawnObjectTypeAlliance;
            MessageIdAlliance = _MessageIdAlliance;
            SpawnObjectTypeHorde = _SpawnObjectTypeHorde;
            MessageIdHorde = _MessageIdHorde;
            GraveyardId = _GraveyardId;
        }

        public int DespawnNeutralObjectType;
        public int SpawnObjectTypeAlliance;
        public uint MessageIdAlliance;
        public int SpawnObjectTypeHorde;
        public uint MessageIdHorde;
        public uint GraveyardId;
    }

    class BgEyeOfStormControlZoneHandler : ControlZoneHandler
    {
        BgEyeofStorm _battleground;
        uint _point;

        public BgEyeOfStormControlZoneHandler(BgEyeofStorm bg, uint point)
        {
            _battleground = bg;
            _point = point;
        }

        public override void HandleProgressEventHorde(GameObject controlZone)
        {
            _battleground.EventTeamCapturedPoint(BattleGroundTeamId.Horde, _point, controlZone);
        }

        public override void HandleProgressEventAlliance(GameObject controlZone)
        {
            _battleground.EventTeamCapturedPoint(BattleGroundTeamId.Alliance, _point, controlZone);
        }

        public override void HandleNeutralEventHorde(GameObject controlZone)
        {
            _battleground.EventTeamLostPoint(BattleGroundTeamId.Horde, _point, controlZone);
        }

        public override void HandleNeutralEventAlliance(GameObject controlZone)
        {
            _battleground.EventTeamLostPoint(BattleGroundTeamId.Alliance, _point, controlZone);
        }

        public uint GetPoint() { return _point; }
    }

    #region Constants
    struct MiscConst
    {
        public static TimeSpan PointsTickTime = TimeSpan.FromSeconds(2);
        public static TimeSpan FlagAssaultTimer = TimeSpan.FromSeconds(30);
        public static ushort FlagBrutalAssaultStackCount = 5;

        public const uint EventStartBattle = 13180; // Achievement: Flurry

        public const uint NotEYWeekendHonorTicks = 260;
        public const uint EYWeekendHonorTicks = 160;

        public const uint SpellNetherstormFlag = 34976;
        // Focused/Brutal Assault
        public const uint SpellFocusedAssault = 46392;
        public const uint SpellBrutalAssault = 46393;

        public const uint ExploitTeleportLocationAlliance = 3773;
        public const uint ExploitTeleportLocationHorde = 3772;

        public static byte[] TickPoints = { 1, 2, 5, 10 };
        public static uint[] FlagPoints = { 75, 85, 100, 500 };

        public const uint AreatriggerCaptureFlag = 33;

        public const uint PvpStatFlagCaptures = 183;

        public static BgEyeOfStormPointIconsStruct[] m_PointsIconStruct =
        {
            new BgEyeOfStormPointIconsStruct(WorldStateIds.FelReaverUncontrol, WorldStateIds.FelReaverAllianceControl, WorldStateIds.FelReaverHordeControl, WorldStateIds.FelReaverAllianceControlState, WorldStateIds.FelReaverHordeControlState),
            new BgEyeOfStormPointIconsStruct(WorldStateIds.BloodElfUncontrol, WorldStateIds.BloodElfAllianceControl, WorldStateIds.BloodElfHordeControl, WorldStateIds.BloodElfAllianceControlState, WorldStateIds.BloodElfHordeControlState),
            new BgEyeOfStormPointIconsStruct(WorldStateIds.DraeneiRuinsUncontrol, WorldStateIds.DraeneiRuinsAllianceControl, WorldStateIds.DraeneiRuinsHordeControl, WorldStateIds.DraeneiRuinsAllianceControlState, WorldStateIds.DraeneiRuinsHordeControlState),
            new BgEyeOfStormPointIconsStruct(WorldStateIds.MageTowerUncontrol, WorldStateIds.MageTowerAllianceControl, WorldStateIds.MageTowerHordeControl, WorldStateIds.MageTowerAllianceControlState, WorldStateIds.MageTowerHordeControlState)
        };
        public static BgEyeOfStormLosingPointStruct[] m_LosingPointTypes =
        {
            new BgEyeOfStormLosingPointStruct(ObjectTypes.NBannerFelReaverCenter, ObjectTypes.ABannerFelReaverCenter, BroadcastTextIds.AllianceLostFelReaverRuins, ObjectTypes.HBannerFelReaverCenter, BroadcastTextIds.HordeLostFelReaverRuins),
            new BgEyeOfStormLosingPointStruct(ObjectTypes.NBannerBloodElfCenter, ObjectTypes.ABannerBloodElfCenter, BroadcastTextIds.AllianceLostBloodElfTower, ObjectTypes.HBannerBloodElfCenter, BroadcastTextIds.HordeLostBloodElfTower),
            new BgEyeOfStormLosingPointStruct(ObjectTypes.NBannerDraeneiRuinsCenter, ObjectTypes.ABannerDraeneiRuinsCenter, BroadcastTextIds.AllianceLostDraeneiRuins, ObjectTypes.HBannerDraeneiRuinsCenter, BroadcastTextIds.HordeLostDraeneiRuins),
            new BgEyeOfStormLosingPointStruct(ObjectTypes.NBannerMageTowerCenter, ObjectTypes.ABannerMageTowerCenter, BroadcastTextIds.AllianceLostMageTower, ObjectTypes.HBannerMageTowerCenter, BroadcastTextIds.HordeLostMageTower)
        };
        public static BgEyeOfStormCapturingPointStruct[] m_CapturingPointTypes =
        {
            new BgEyeOfStormCapturingPointStruct(ObjectTypes.NBannerFelReaverCenter, ObjectTypes.ABannerFelReaverCenter, BroadcastTextIds.AllianceTakenFelReaverRuins, ObjectTypes.HBannerFelReaverCenter, BroadcastTextIds.HordeTakenFelReaverRuins, GaveyardIds.FelReaver),
            new BgEyeOfStormCapturingPointStruct(ObjectTypes.NBannerBloodElfCenter, ObjectTypes.ABannerBloodElfCenter, BroadcastTextIds.AllianceTakenBloodElfTower, ObjectTypes.HBannerBloodElfCenter, BroadcastTextIds.HordeTakenBloodElfTower, GaveyardIds.BloodElf),
            new BgEyeOfStormCapturingPointStruct(ObjectTypes.NBannerDraeneiRuinsCenter, ObjectTypes.ABannerDraeneiRuinsCenter, BroadcastTextIds.AllianceTakenDraeneiRuins, ObjectTypes.HBannerDraeneiRuinsCenter, BroadcastTextIds.HordeTakenDraeneiRuins, GaveyardIds.DraeneiRuins),
            new BgEyeOfStormCapturingPointStruct(ObjectTypes.NBannerMageTowerCenter, ObjectTypes.ABannerMageTowerCenter, BroadcastTextIds.AllianceTakenMageTower, ObjectTypes.HBannerMageTowerCenter, BroadcastTextIds.HordeTakenMageTower, GaveyardIds.MageTower)
        };
    }

    struct BroadcastTextIds
    {
        public const uint AllianceTakenFelReaverRuins = 17828;
        public const uint HordeTakenFelReaverRuins = 17829;
        public const uint AllianceLostFelReaverRuins = 17835;
        public const uint HordeLostFelReaverRuins = 17836;

        public const uint AllianceTakenBloodElfTower = 17819;
        public const uint HordeTakenBloodElfTower = 17823;
        public const uint AllianceLostBloodElfTower = 17831;
        public const uint HordeLostBloodElfTower = 17832;

        public const uint AllianceTakenDraeneiRuins = 17827;
        public const uint HordeTakenDraeneiRuins = 17826;
        public const uint AllianceLostDraeneiRuins = 17833;
        public const uint HordeLostDraeneiRuins = 17834;

        public const uint AllianceTakenMageTower = 17824;
        public const uint HordeTakenMageTower = 17825;
        public const uint AllianceLostMageTower = 17837;
        public const uint HordeLostMageTower = 17838;

        public const uint TakenFlag = 18359;
        public const uint FlagDropped = 18361;
        public const uint FlagReset = 18364;
        public const uint AllianceCapturedFlag = 18375;
        public const uint HordeCapturedFlag = 18384;
    }

    struct WorldStateIds
    {
        public const int AllianceResources = 1776;
        public const int HordeResources = 1777;
        public const int MaxResources = 1780;
        public const int AllianceBase = 2752;
        public const int HordeBase = 2753;
        public const int DraeneiRuinsHordeControl = 2733;
        public const int DraeneiRuinsAllianceControl = 2732;
        public const int DraeneiRuinsUncontrol = 2731;
        public const int MageTowerAllianceControl = 2730;
        public const int MageTowerHordeControl = 2729;
        public const int MageTowerUncontrol = 2728;
        public const int FelReaverHordeControl = 2727;
        public const int FelReaverAllianceControl = 2726;
        public const int FelReaverUncontrol = 2725;
        public const int BloodElfHordeControl = 2724;
        public const int BloodElfAllianceControl = 2723;
        public const int BloodElfUncontrol = 2722;
        public const int ProgressBarPercentGrey = 2720;                 //100 = Empty (Only Grey); 0 = Blue|Red (No Grey)
        public const int ProgressBarStatus = 2719;                 //50 Init!; 48 ... Hordak Bere .. 33 .. 0 = Full 100% Hordacky; 100 = Full Alliance
        public const int ProgressBarShow = 2718;                 //1 Init; 0 Druhy Send - Bez Messagu; 1 = Controlled Aliance
        public const int NetherstormFlag = 8863;
        //Set To 2 When Flag Is Picked Up; And To 1 If It Is Dropped
        public const int NetherstormFlagStateAlliance = 9808;
        public const int NetherstormFlagStateHorde = 9809;

        public const int DraeneiRuinsHordeControlState = 17362;
        public const int DraeneiRuinsAllianceControlState = 17366;
        public const int MageTowerHordeControlState = 17361;
        public const int MageTowerAllianceControlState = 17368;
        public const int FelReaverHordeControlState = 17364;
        public const int FelReaverAllianceControlState = 17367;
        public const int BloodElfHordeControlState = 17363;
        public const int BloodElfAllianceControlState = 17365;
    }

    struct SoundIds
    {
        //strange ids, but sure about them
        public const uint FlagPickedUpAlliance = 8212;
        public const uint FlagCapturedHorde = 8213;
        public const uint FlagPickedUpHorde = 8174;
        public const uint FlagCapturedAlliance = 8173;
        public const uint FlagReset = 8192;
    }

    struct GameobjectIds
    {
        public const uint ADoorEyEntry = 184719;           //Alliance Door
        public const uint HDoorEyEntry = 184720;           //Horde Door
        public const uint Flag1EyEntry = 184493;           //Netherstorm Flag (Generic)
        public const uint Flag2EyEntry = 208977;           //Netherstorm Flag (Flagstand)
        public const uint ABannerEyEntry = 184381;           //Visual Banner (Alliance)
        public const uint HBannerEyEntry = 184380;           //Visual Banner (Horde)
        public const uint NBannerEyEntry = 184382;           //Visual Banner (Neutral)
        public const uint BeTowerCapEyEntry = 184080;           //Be Tower Cap Pt
        public const uint FrTowerCapEyEntry = 184081;           //Fel Reaver Cap Pt
        public const uint HuTowerCapEyEntry = 184082;           //Human Tower Cap Pt
        public const uint DrTowerCapEyEntry = 184083;           //Draenei Tower Cap Pt
        public const uint SpeedBuffFelReaverEyEntry = 184970;
        public const uint RestorationBuffFelReaverEyEntry = 184971;
        public const uint BerserkBuffFelReaverEyEntry = 184972;
        public const uint SpeedBuffBloodElfEyEntry = 184964;
        public const uint RestorationBuffBloodElfEyEntry = 184965;
        public const uint BerserkBuffBloodElfEyEntry = 184966;
        public const uint SpeedBuffDraeneiRuinsEyEntry = 184976;
        public const uint RestorationBuffDraeneiRuinsEyEntry = 184977;
        public const uint BerserkBuffDraeneiRuinsEyEntry = 184978;
        public const uint SpeedBuffMageTowerEyEntry = 184973;
        public const uint RestorationBuffMageTowerEyEntry = 184974;
        public const uint BerserkBuffMageTowerEyEntry = 184975;
    }

    struct GaveyardIds
    {
        public const int MainAlliance = 1103;
        public const uint MainHorde = 1104;
        public const uint FelReaver = 1105;
        public const uint BloodElf = 1106;
        public const uint DraeneiRuins = 1107;
        public const uint MageTower = 1108;
    }

    struct Points
    {
        public const int FelReaver = 0;
        public const int BloodElf = 1;
        public const int DraeneiRuins = 2;
        public const int MageTower = 3;

        public const int PlayersOutOfPoints = 4;
        public const int PointsMax = 4;
    }

    struct ObjectTypes
    {
        public const int DoorA = 0;
        public const int DoorH = 1;
        public const int ABannerFelReaverCenter = 2;
        public const int ABannerFelReaverLeft = 3;
        public const int ABannerFelReaverRight = 4;
        public const int ABannerBloodElfCenter = 5;
        public const int ABannerBloodElfLeft = 6;
        public const int ABannerBloodElfRight = 7;
        public const int ABannerDraeneiRuinsCenter = 8;
        public const int ABannerDraeneiRuinsLeft = 9;
        public const int ABannerDraeneiRuinsRight = 10;
        public const int ABannerMageTowerCenter = 11;
        public const int ABannerMageTowerLeft = 12;
        public const int ABannerMageTowerRight = 13;
        public const int HBannerFelReaverCenter = 14;
        public const int HBannerFelReaverLeft = 15;
        public const int HBannerFelReaverRight = 16;
        public const int HBannerBloodElfCenter = 17;
        public const int HBannerBloodElfLeft = 18;
        public const int HBannerBloodElfRight = 19;
        public const int HBannerDraeneiRuinsCenter = 20;
        public const int HBannerDraeneiRuinsLeft = 21;
        public const int HBannerDraeneiRuinsRight = 22;
        public const int HBannerMageTowerCenter = 23;
        public const int HBannerMageTowerLeft = 24;
        public const int HBannerMageTowerRight = 25;
        public const int NBannerFelReaverCenter = 26;
        public const int NBannerFelReaverLeft = 27;
        public const int NBannerFelReaverRight = 28;
        public const int NBannerBloodElfCenter = 29;
        public const int NBannerBloodElfLeft = 30;
        public const int NBannerBloodElfRight = 31;
        public const int NBannerDraeneiRuinsCenter = 32;
        public const int NBannerDraeneiRuinsLeft = 33;
        public const int NBannerDraeneiRuinsRight = 34;
        public const int NBannerMageTowerCenter = 35;
        public const int NBannerMageTowerLeft = 36;
        public const int NBannerMageTowerRight = 37;
        public const int TowerCapFelReaver = 38;
        public const int TowerCapBloodElf = 39;
        public const int TowerCapDraeneiRuins = 40;
        public const int TowerCapMageTower = 41;
        public const int FlagNetherstorm = 42;
        public const int FlagFelReaver = 43;
        public const int FlagBloodElf = 44;
        public const int FlagDraeneiRuins = 45;
        public const int FlagMageTower = 46;
        //Buffs
        public const int SpeedbuffFelReaver = 47;
        public const int RegenbuffFelReaver = 48;
        public const int BerserkbuffFelReaver = 49;
        public const int SpeedbuffBloodElf = 50;
        public const int RegenbuffBloodElf = 51;
        public const int BerserkbuffBloodElf = 52;
        public const int SpeedbuffDraeneiRuins = 53;
        public const int RegenbuffDraeneiRuins = 54;
        public const int BerserkbuffDraeneiRuins = 55;
        public const int SpeedbuffMageTower = 56;
        public const int RegenbuffMageTower = 57;
        public const int BerserkbuffMageTower = 58;
        public const int Max = 59;
    }

    struct ScoreIds
    {
        public const uint WarningNearVictoryScore = 1400;
        public const uint MaxTeamScore = 1500;
    }

    enum EYFlagState
    {
        OnBase = 0,
        WaitRespawn = 1,
        OnPlayer = 2,
        OnGround = 3
    }
    #endregion
}
