// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.AI;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces.IBattlefield;
using Game.Spells;

namespace Game.BattleFields
{
    internal class BattlefieldWG : BattleField
    {
        private readonly List<ObjectGuid>[] _vehicles = new List<ObjectGuid>[SharedConst.PvpTeamsCount];
        private readonly List<BfWGGameObjectBuilding> BuildingsInZone = new();
        private readonly List<ObjectGuid> CanonList = new();

        private readonly List<ObjectGuid>[] DefenderPortalList = new List<ObjectGuid>[SharedConst.PvpTeamsCount];

        private readonly List<WGWorkshop> Workshops = new();
        private bool _isRelicInteractible;
        private uint _tenacityStack;

        private int _tenacityTeam;

        private ObjectGuid _titansRelicGUID;

        public BattlefieldWG(Map map) : base(map)
        {
        }

        public override bool SetupBattlefield()
        {
            m_TypeId = (uint)BattleFieldTypes.WinterGrasp; // See enum BattlefieldTypes
            m_BattleId = BattlefieldIds.WG;
            m_ZoneId = (uint)AreaId.Wintergrasp;

            InitStalker(WGNpcs.Stalker, WGConst.WintergraspStalkerPos);

            m_MaxPlayer = WorldConfig.GetUIntValue(WorldCfg.WintergraspPlrMax);
            m_IsEnabled = WorldConfig.GetBoolValue(WorldCfg.WintergraspEnable);
            m_MinPlayer = WorldConfig.GetUIntValue(WorldCfg.WintergraspPlrMin);
            m_MinLevel = WorldConfig.GetUIntValue(WorldCfg.WintergraspPlrMinLvl);
            m_BattleTime = WorldConfig.GetUIntValue(WorldCfg.WintergraspBattletime) * Time.Minute * Time.InMilliseconds;
            m_NoWarBattleTime = WorldConfig.GetUIntValue(WorldCfg.WintergraspNobattletime) * Time.Minute * Time.InMilliseconds;
            m_RestartAfterCrash = WorldConfig.GetUIntValue(WorldCfg.WintergraspRestartAfterCrash) * Time.Minute * Time.InMilliseconds;

            m_TimeForAcceptInvite = 20;
            m_StartGroupingTimer = 15 * Time.Minute * Time.InMilliseconds;
            _tenacityTeam = TeamId.Neutral;

            KickPosition = new WorldLocation(m_MapId, 5728.117f, 2714.346f, 697.733f, 0);

            RegisterZone(m_ZoneId);

            for (var team = 0; team < SharedConst.PvpTeamsCount; ++team)
            {
                DefenderPortalList[team] = new List<ObjectGuid>();
                _vehicles[team] = new List<ObjectGuid>();
            }

            // Load from db
            if (Global.WorldStateMgr.GetValue(WorldStates.BattlefieldWgShowTimeNextBattle, m_Map) == 0 &&
                Global.WorldStateMgr.GetValue(WGConst.ClockWorldState[0], m_Map) == 0)
            {
                Global.WorldStateMgr.SetValueAndSaveInDb(WorldStates.BattlefieldWgShowTimeNextBattle, 0, false, m_Map);
                Global.WorldStateMgr.SetValueAndSaveInDb(WorldStates.BattlefieldWgDefender, RandomHelper.IRand(0, 1), false, m_Map);
                Global.WorldStateMgr.SetValueAndSaveInDb(WGConst.ClockWorldState[0], (int)(GameTime.GetGameTime() + m_NoWarBattleTime / Time.InMilliseconds), false, m_Map);
            }

            m_isActive = Global.WorldStateMgr.GetValue(WorldStates.BattlefieldWgShowTimeNextBattle, m_Map) == 0;
            m_DefenderTeam = (uint)Global.WorldStateMgr.GetValue(WorldStates.BattlefieldWgDefender, m_Map);

            m_Timer = (uint)(Global.WorldStateMgr.GetValue(WGConst.ClockWorldState[0], m_Map) - GameTime.GetGameTime());

            if (m_isActive)
            {
                m_isActive = false;
                m_Timer = m_RestartAfterCrash;
            }

            Global.WorldStateMgr.SetValue(WorldStates.BattlefieldWgAttacker, (int)GetAttackerTeam(), false, m_Map);
            Global.WorldStateMgr.SetValue(WGConst.ClockWorldState[1], (int)(GameTime.GetGameTime() + m_Timer / Time.InMilliseconds), false, m_Map);

            foreach (var gy in WGConst.WGGraveYard)
            {
                BfGraveyardWG graveyard = new(this);

                // When between games, the graveyard is controlled by the defending team
                if (gy.StartControl == TeamId.Neutral)
                    graveyard.Initialize(m_DefenderTeam, gy.GraveyardID);
                else
                    graveyard.Initialize(gy.StartControl, gy.GraveyardID);

                graveyard.SetTextId(gy.TextId);
                m_GraveyardList.Add(graveyard);
            }


            // Spawn workshop creatures and gameobjects
            for (byte i = 0; i < WGConst.MaxWorkshops; i++)
            {
                WGWorkshop workshop = new(this, i);

                if (i < WGWorkshopIds.Ne)
                    workshop.GiveControlTo(GetAttackerTeam(), true);
                else
                    workshop.GiveControlTo(GetDefenderTeam(), true);

                // Note: Capture point is added once the gameobject is created.
                Workshops.Add(workshop);
            }

            // Spawn turrets and hide them per default
            foreach (var turret in WGConst.WGTurret)
            {
                Position towerCannonPos = turret.GetPosition();
                Creature creature = SpawnCreature(WGNpcs.TowerCannon, towerCannonPos);

                if (creature)
                {
                    CanonList.Add(creature.GetGUID());
                    HideNpc(creature);
                }
            }

            // Spawn all gameobjects
            foreach (var build in WGConst.WGGameObjectBuilding)
            {
                GameObject go = SpawnGameObject(build.Entry, build.Pos, build.Rot);

                if (go)
                {
                    BfWGGameObjectBuilding b = new(this, build.BuildingType, build.WorldState);
                    b.Init(go);

                    if (!m_IsEnabled &&
                        go.GetEntry() == WGGameObjects.VaultGate)
                        go.SetDestructibleState(GameObjectDestructibleState.Destroyed);

                    BuildingsInZone.Add(b);
                }
            }

            // Spawning portal defender
            foreach (var teleporter in WGConst.WGPortalDefenderData)
            {
                GameObject go = SpawnGameObject(teleporter.AllianceEntry, teleporter.Pos, teleporter.Rot);

                if (go)
                {
                    DefenderPortalList[TeamId.Alliance].Add(go.GetGUID());
                    go.SetRespawnTime((int)(GetDefenderTeam() == TeamId.Alliance ? BattlegroundConst.RespawnImmediately : BattlegroundConst.RespawnOneDay));
                }

                go = SpawnGameObject(teleporter.HordeEntry, teleporter.Pos, teleporter.Rot);

                if (go)
                {
                    DefenderPortalList[TeamId.Horde].Add(go.GetGUID());
                    go.SetRespawnTime((int)(GetDefenderTeam() == TeamId.Horde ? BattlegroundConst.RespawnImmediately : BattlegroundConst.RespawnOneDay));
                }
            }

            UpdateCounterVehicle(true);

            return true;
        }

        public override void OnBattleStart()
        {
            // Spawn titan relic
            GameObject relic = SpawnGameObject(WGGameObjects.TitanSRelic, WGConst.RelicPos, WGConst.RelicRot);

            if (relic)
            {
                // Update faction of relic, only Attacker can click on
                relic.SetFaction(WGConst.WintergraspFaction[GetAttackerTeam()]);
                // Set in use (not allow to click on before last door is broken)
                relic.SetFlag(GameObjectFlags.InUse | GameObjectFlags.NotSelectable);
                _titansRelicGUID = relic.GetGUID();
            }
            else
            {
                Log.outError(LogFilter.Battlefield, "WG: Failed to spawn titan relic.");
            }

            Global.WorldStateMgr.SetValue(WorldStates.BattlefieldWgAttacker, (int)GetAttackerTeam(), false, m_Map);
            Global.WorldStateMgr.SetValueAndSaveInDb(WorldStates.BattlefieldWgDefender, (int)GetDefenderTeam(), false, m_Map);
            Global.WorldStateMgr.SetValueAndSaveInDb(WorldStates.BattlefieldWgShowTimeNextBattle, 0, false, m_Map);
            Global.WorldStateMgr.SetValue(WorldStates.BattlefieldWgShowTimeBattleEnd, 1, false, m_Map);
            Global.WorldStateMgr.SetValueAndSaveInDb(WGConst.ClockWorldState[0], (int)(GameTime.GetGameTime() + m_Timer / Time.InMilliseconds), false, m_Map);

            // Update tower visibility and update faction
            foreach (var guid in CanonList)
            {
                Creature creature = GetCreature(guid);

                if (creature)
                {
                    ShowNpc(creature, true);
                    creature.SetFaction(WGConst.WintergraspFaction[GetDefenderTeam()]);
                }
            }

            // Rebuild all wall
            foreach (var wall in BuildingsInZone)
                if (wall != null)
                {
                    wall.Rebuild();
                    wall.UpdateTurretAttack(false);
                }

            SetData(WGData.BrokenTowerAtt, 0);
            SetData(WGData.BrokenTowerDef, 0);
            SetData(WGData.DamagedTowerAtt, 0);
            SetData(WGData.DamagedTowerDef, 0);

            // Update graveyard (in no war Time all graveyard is to deffender, in war Time, depend of base)
            foreach (var workShop in Workshops)
                workShop?.UpdateGraveyardAndWorkshop();

            for (byte team = 0; team < SharedConst.PvpTeamsCount; ++team)
                foreach (var guid in m_players[team])
                {
                    // Kick player in orb room, TODO: offline player ?
                    Player player = Global.ObjAccessor.FindPlayer(guid);

                    if (player)
                    {
                        float x, y, z;
                        player.GetPosition(out x, out y, out z);

                        if (5500 > x &&
                            x > 5392 &&
                            y < 2880 &&
                            y > 2800 &&
                            z < 480)
                            player.TeleportTo(571, 5349.8686f, 2838.481f, 409.240f, 0.046328f);
                    }
                }

            // Initialize vehicle counter
            UpdateCounterVehicle(true);
            // Send start warning to all players
            SendWarning(WintergraspText.StartBattle);
        }

        public void UpdateCounterVehicle(bool init)
        {
            if (init)
            {
                SetData(WGData.VehicleH, 0);
                SetData(WGData.VehicleA, 0);
            }

            SetData(WGData.MaxVehicleH, 0);
            SetData(WGData.MaxVehicleA, 0);

            foreach (var workshop in Workshops)
                if (workshop.GetTeamControl() == TeamId.Alliance)
                    UpdateData(WGData.MaxVehicleA, 4);
                else if (workshop.GetTeamControl() == TeamId.Horde)
                    UpdateData(WGData.MaxVehicleH, 4);

            UpdateVehicleCountWG();
        }

        public override void OnBattleEnd(bool endByTimer)
        {
            // Remove relic
            if (!_titansRelicGUID.IsEmpty())
            {
                GameObject relic = GetGameObject(_titansRelicGUID);

                if (relic)
                    relic.RemoveFromWorld();
            }

            _titansRelicGUID.Clear();

            // change collision wall State closed
            foreach (BfWGGameObjectBuilding building in BuildingsInZone)
                building.RebuildGate();

            // update win statistics
            {
                WorldStates worldStateId;

                // successful defense
                if (endByTimer)
                    worldStateId = GetDefenderTeam() == TeamId.Horde ? WorldStates.BattlefieldWgDefendedH : WorldStates.BattlefieldWgDefendedA;
                // successful attack (note that teams have already been swapped, so defender team is the one who won)
                else
                    worldStateId = GetDefenderTeam() == TeamId.Horde ? WorldStates.BattlefieldWgAttackedH : WorldStates.BattlefieldWgAttackedA;

                Global.WorldStateMgr.SetValueAndSaveInDb(worldStateId, Global.WorldStateMgr.GetValue((int)worldStateId, m_Map) + 1, false, m_Map);
            }

            Global.WorldStateMgr.SetValueAndSaveInDb(WorldStates.BattlefieldWgDefender, (int)GetDefenderTeam(), false, m_Map);
            Global.WorldStateMgr.SetValueAndSaveInDb(WorldStates.BattlefieldWgShowTimeNextBattle, 1, false, m_Map);
            Global.WorldStateMgr.SetValue(WorldStates.BattlefieldWgShowTimeBattleEnd, 0, false, m_Map);
            Global.WorldStateMgr.SetValue(WGConst.ClockWorldState[1], (int)(GameTime.GetGameTime() + m_Timer / Time.InMilliseconds), false, m_Map);

            // Remove turret
            foreach (var guid in CanonList)
            {
                Creature creature = GetCreature(guid);

                if (creature)
                {
                    if (!endByTimer)
                        creature.SetFaction(WGConst.WintergraspFaction[GetDefenderTeam()]);

                    HideNpc(creature);
                }
            }

            // Update all graveyard, control is to defender when no wartime
            for (byte i = 0; i < WGGraveyardId.Horde; i++)
            {
                BfGraveyard graveyard = GetGraveyardById(i);

                graveyard?.GiveControlTo(GetDefenderTeam());
            }

            // Update portals
            foreach (var guid in DefenderPortalList[GetDefenderTeam()])
            {
                GameObject portal = GetGameObject(guid);

                if (portal)
                    portal.SetRespawnTime((int)BattlegroundConst.RespawnImmediately);
            }

            foreach (var guid in DefenderPortalList[GetAttackerTeam()])
            {
                GameObject portal = GetGameObject(guid);

                if (portal)
                    portal.SetRespawnTime((int)BattlegroundConst.RespawnOneDay);
            }

            foreach (var guid in m_PlayersInWar[GetDefenderTeam()])
            {
                Player player = Global.ObjAccessor.FindPlayer(guid);

                if (player)
                {
                    player.CastSpell(player, WGSpells.EssenceOfWintergrasp, true);
                    player.CastSpell(player, WGSpells.VictoryReward, true);
                    // Complete victory quests
                    player.AreaExploredOrEventHappens(WintergraspQuests.VictoryAlliance);
                    player.AreaExploredOrEventHappens(WintergraspQuests.VictoryHorde);
                    // Send Wintergrasp victory Achievement
                    DoCompleteOrIncrementAchievement(WGAchievements.WinWg, player);

                    // Award Achievement for succeeding in Wintergrasp in 10 minutes or less
                    if (!endByTimer &&
                        GetTimer() <= 10000)
                        DoCompleteOrIncrementAchievement(WGAchievements.WinWgTimer10, player);
                }
            }

            foreach (var guid in m_PlayersInWar[GetAttackerTeam()])
            {
                Player player = Global.ObjAccessor.FindPlayer(guid);

                if (player)
                    player.CastSpell(player, WGSpells.DefeatReward, true);
            }

            for (byte team = 0; team < SharedConst.PvpTeamsCount; ++team)
            {
                foreach (var guid in m_PlayersInWar[team])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);

                    if (player)
                        RemoveAurasFromPlayer(player);
                }

                m_PlayersInWar[team].Clear();

                foreach (var guid in _vehicles[team])
                {
                    Creature creature = GetCreature(guid);

                    if (creature)
                        if (creature.IsVehicle())
                            creature.DespawnOrUnsummon();
                }

                _vehicles[team].Clear();
            }

            if (!endByTimer)
                for (byte team = 0; team < SharedConst.PvpTeamsCount; ++team)
                    foreach (var guid in m_players[team])
                    {
                        Player player = Global.ObjAccessor.FindPlayer(guid);

                        if (player)
                        {
                            player.RemoveAurasDueToSpell(m_DefenderTeam == TeamId.Alliance ? WGSpells.HordeControlPhaseShift : WGSpells.AllianceControlPhaseShift, player.GetGUID());
                            player.AddAura(m_DefenderTeam == TeamId.Horde ? WGSpells.HordeControlPhaseShift : WGSpells.AllianceControlPhaseShift, player);
                        }
                    }

            if (!endByTimer) // win alli/horde
                SendWarning((GetDefenderTeam() == TeamId.Alliance) ? WintergraspText.FortressCaptureAlliance : WintergraspText.FortressCaptureHorde);
            else // defend alli/horde
                SendWarning((GetDefenderTeam() == TeamId.Alliance) ? WintergraspText.FortressDefendAlliance : WintergraspText.FortressDefendHorde);
        }

        public override void DoCompleteOrIncrementAchievement(uint achievement, Player player, byte incrementNumber = 1)
        {
            AchievementRecord achievementEntry = CliDB.AchievementStorage.LookupByKey(achievement);

            if (achievementEntry == null)
                return;

            switch (achievement)
            {
                //removed by TC
                //case ACHIEVEMENTS_WIN_WG_100:
                //{
                // player.UpdateAchievementCriteria();
                //}
                default:
                    {
                        if (player)
                            player.CompletedAchievement(achievementEntry);

                        break;
                    }
            }
        }

        public override void OnStartGrouping()
        {
            SendWarning(WintergraspText.StartGrouping);
        }

        public override void OnCreatureCreate(Creature creature)
        {
            // Accessing to db spawned creatures
            switch (creature.GetEntry())
            {
                case WGNpcs.DwarvenSpiritGuide:
                case WGNpcs.TaunkaSpiritGuide:
                    {
                        int teamIndex = (creature.GetEntry() == WGNpcs.DwarvenSpiritGuide ? TeamId.Alliance : TeamId.Horde);
                        byte graveyardId = (byte)GetSpiritGraveyardId(creature.GetAreaId());

                        m_GraveyardList[graveyardId]?.SetSpirit(creature, teamIndex);

                        break;
                    }
            }

            // untested code - not sure if it is valid.
            if (IsWarTime())
                switch (creature.GetEntry())
                {
                    case WGNpcs.SiegeEngineAlliance:
                    case WGNpcs.SiegeEngineHorde:
                    case WGNpcs.Catapult:
                    case WGNpcs.Demolisher:
                        {
                            if (!creature.ToTempSummon() ||
                                creature.ToTempSummon().GetSummonerGUID().IsEmpty() ||
                                !Global.ObjAccessor.FindPlayer(creature.ToTempSummon().GetSummonerGUID()))
                            {
                                creature.DespawnOrUnsummon();

                                return;
                            }

                            Player creator = Global.ObjAccessor.FindPlayer(creature.ToTempSummon().GetSummonerGUID());
                            int teamIndex = creator.GetTeamId();

                            if (teamIndex == TeamId.Horde)
                            {
                                if (GetData(WGData.VehicleH) < GetData(WGData.MaxVehicleH))
                                {
                                    UpdateData(WGData.VehicleH, 1);
                                    creature.AddAura(WGSpells.HordeFlag, creature);
                                    _vehicles[teamIndex].Add(creature.GetGUID());
                                    UpdateVehicleCountWG();
                                }
                                else
                                {
                                    creature.DespawnOrUnsummon();

                                    return;
                                }
                            }
                            else
                            {
                                if (GetData(WGData.VehicleA) < GetData(WGData.MaxVehicleA))
                                {
                                    UpdateData(WGData.VehicleA, 1);
                                    creature.AddAura(WGSpells.AllianceFlag, creature);
                                    _vehicles[teamIndex].Add(creature.GetGUID());
                                    UpdateVehicleCountWG();
                                }
                                else
                                {
                                    creature.DespawnOrUnsummon();

                                    return;
                                }
                            }

                            creature.CastSpell(creator, WGSpells.GrabPassenger, true);

                            break;
                        }
                }
        }

        public override void OnCreatureRemove(Creature c)
        {
        }

        public override void OnGameObjectCreate(GameObject go)
        {
            uint workshopId;

            switch (go.GetEntry())
            {
                case WGGameObjects.FactoryBannerNe:
                    workshopId = WGWorkshopIds.Ne;

                    break;
                case WGGameObjects.FactoryBannerNw:
                    workshopId = WGWorkshopIds.Nw;

                    break;
                case WGGameObjects.FactoryBannerSe:
                    workshopId = WGWorkshopIds.Se;

                    break;
                case WGGameObjects.FactoryBannerSw:
                    workshopId = WGWorkshopIds.Sw;

                    break;
                default:
                    return;
            }

            foreach (var workshop in Workshops)
                if (workshop.GetId() == workshopId)
                {
                    WintergraspCapturePoint capturePoint = new(this, GetAttackerTeam());

                    capturePoint.SetCapturePointData(go);
                    capturePoint.LinkToWorkshop(workshop);
                    AddCapturePoint(capturePoint);

                    break;
                }
        }

        public override void HandleKill(Player killer, Unit victim)
        {
            if (killer == victim)
                return;

            if (victim.IsTypeId(Framework.Constants.TypeId.Player))
            {
                HandlePromotion(killer, victim);
                // Allow to Skin non-released corpse
                victim.SetUnitFlag(UnitFlags.Skinnable);
            }

            // @todo Recent PvP activity worldstate
        }

        public override void OnUnitDeath(Unit unit)
        {
            if (IsWarTime())
                if (unit.IsVehicle())
                    if (FindAndRemoveVehicleFromList(unit))
                        UpdateVehicleCountWG();
        }

        public void HandlePromotion(Player playerKiller, Unit unitKilled)
        {
            int teamId = playerKiller.GetTeamId();

            foreach (var guid in m_PlayersInWar[teamId])
            {
                Player player = Global.ObjAccessor.FindPlayer(guid);

                if (player)
                    if (player.GetDistance2d(unitKilled) < 40.0f)
                        PromotePlayer(player);
            }
        }

        public override void OnPlayerJoinWar(Player player)
        {
            RemoveAurasFromPlayer(player);

            player.CastSpell(player, WGSpells.Recruit, true);

            if (player.GetZoneId() != m_ZoneId)
            {
                if (player.GetTeamId() == GetDefenderTeam())
                {
                    player.TeleportTo(571, 5345, 2842, 410, 3.14f);
                }
                else
                {
                    if (player.GetTeamId() == TeamId.Horde)
                        player.TeleportTo(571, 5025.857422f, 3674.628906f, 362.737122f, 4.135169f);
                    else
                        player.TeleportTo(571, 5101.284f, 2186.564f, 373.549f, 3.812f);
                }
            }

            UpdateTenacity();

            if (player.GetTeamId() == GetAttackerTeam())
            {
                if (GetData(WGData.BrokenTowerAtt) < 3)
                    player.SetAuraStack(WGSpells.TowerControl, player, 3 - GetData(WGData.BrokenTowerAtt));
            }
            else
            {
                if (GetData(WGData.BrokenTowerAtt) > 0)
                    player.SetAuraStack(WGSpells.TowerControl, player, GetData(WGData.BrokenTowerAtt));
            }
        }

        public override void OnPlayerLeaveWar(Player player)
        {
            // Remove all aura from WG // @todo false we can go out of this zone on retail and keep Rank buff, remove on end of WG
            if (!player.GetSession().PlayerLogout())
            {
                Creature vehicle = player.GetVehicleCreatureBase();

                if (vehicle) // Remove vehicle of player if he go out.
                    vehicle.DespawnOrUnsummon();

                RemoveAurasFromPlayer(player);
            }

            player.RemoveAura(WGSpells.HordeControlsFactoryPhaseShift);
            player.RemoveAura(WGSpells.AllianceControlsFactoryPhaseShift);
            player.RemoveAura(WGSpells.HordeControlPhaseShift);
            player.RemoveAura(WGSpells.AllianceControlPhaseShift);
            UpdateTenacity();
        }

        public override void OnPlayerLeaveZone(Player player)
        {
            if (!m_isActive)
                RemoveAurasFromPlayer(player);

            player.RemoveAura(WGSpells.HordeControlsFactoryPhaseShift);
            player.RemoveAura(WGSpells.AllianceControlsFactoryPhaseShift);
            player.RemoveAura(WGSpells.HordeControlPhaseShift);
            player.RemoveAura(WGSpells.AllianceControlPhaseShift);
        }

        public override void OnPlayerEnterZone(Player player)
        {
            if (!m_isActive)
                RemoveAurasFromPlayer(player);

            player.AddAura(m_DefenderTeam == TeamId.Horde ? WGSpells.HordeControlPhaseShift : WGSpells.AllianceControlPhaseShift, player);
        }

        public override uint GetData(uint data)
        {
            switch ((AreaId)data)
            {
                // Used to determine when the phasing spells must be cast
                // See: SpellArea.IsFitToRequirements
                case AreaId.TheSunkenRing:
                case AreaId.TheBrokenTemplate:
                case AreaId.WestparkWorkshop:
                case AreaId.EastparkWorkshop:
                    // Graveyards and Workshops are controlled by the same team.
                    BfGraveyard graveyard = GetGraveyardById((int)GetSpiritGraveyardId(data));

                    if (graveyard != null)
                        return graveyard.GetControlTeamId();

                    break;
                default:
                    break;
            }

            return base.GetData(data);
        }

        public void BrokenWallOrTower(uint team, BfWGGameObjectBuilding building)
        {
            if (team == GetDefenderTeam())
                foreach (var guid in m_PlayersInWar[GetAttackerTeam()])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);

                    if (player)
                        if (player.GetDistance2d(GetGameObject(building.GetGUID())) < 50.0f)
                            player.KilledMonsterCredit(WintergraspQuests.CreditDefendSiege);
                }
        }

        // Called when a tower is broke
        public void UpdatedDestroyedTowerCount(uint team)
        {
            // Southern tower
            if (team == GetAttackerTeam())
            {
                // Update counter
                UpdateData(WGData.DamagedTowerAtt, -1);
                UpdateData(WGData.BrokenTowerAtt, 1);

                // Remove buff stack on attackers
                foreach (var guid in m_PlayersInWar[GetAttackerTeam()])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);

                    if (player)
                        player.RemoveAuraFromStack(WGSpells.TowerControl);
                }

                // Add buff stack to defenders and give Achievement/quest credit
                foreach (var guid in m_PlayersInWar[GetDefenderTeam()])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);

                    if (player)
                    {
                        player.CastSpell(player, WGSpells.TowerControl, true);
                        player.KilledMonsterCredit(WintergraspQuests.CreditTowersDestroyed);
                        DoCompleteOrIncrementAchievement(WGAchievements.WgTowerDestroy, player);
                    }
                }

                // If all three south towers are destroyed (ie. all attack towers), remove ten minutes from battle Time
                if (GetData(WGData.BrokenTowerAtt) == 3)
                {
                    if ((int)(m_Timer - 600000) < 0)
                        m_Timer = 0;
                    else
                        m_Timer -= 600000;

                    Global.WorldStateMgr.SetValue(WGConst.ClockWorldState[0], (int)(GameTime.GetGameTime() + m_Timer / Time.InMilliseconds), false, m_Map);
                }
            }
            else // Keep tower
            {
                UpdateData(WGData.DamagedTowerDef, -1);
                UpdateData(WGData.BrokenTowerDef, 1);
            }
        }

        public override void ProcessEvent(WorldObject obj, uint eventId, WorldObject invoker)
        {
            if (!obj ||
                !IsWarTime())
                return;

            // We handle only gameobjects here
            GameObject go = obj.ToGameObject();

            if (!go)
                return;

            // On click on titan relic
            if (go.GetEntry() == WGGameObjects.TitanSRelic)
            {
                GameObject relic = GetRelic();

                if (CanInteractWithRelic())
                    EndBattle(false);
                else if (relic)
                    relic.SetRespawnTime(0);
            }

            // if destroy or Damage event, search the wall/tower and update worldstate/send warning message
            foreach (var building in BuildingsInZone)
                if (go.GetGUID() == building.GetGUID())
                {
                    GameObject buildingGo = GetGameObject(building.GetGUID());

                    if (buildingGo)
                    {
                        if (buildingGo.GetGoInfo().DestructibleBuilding.DamagedEvent == eventId)
                            building.Damaged();

                        if (buildingGo.GetGoInfo().DestructibleBuilding.DestroyedEvent == eventId)
                            building.Destroyed();

                        break;
                    }
                }
        }

        // Called when a tower is damaged, used for honor reward calcul
        public void UpdateDamagedTowerCount(uint team)
        {
            if (team == GetAttackerTeam())
                UpdateData(WGData.DamagedTowerAtt, 1);
            else
                UpdateData(WGData.DamagedTowerDef, 1);
        }

        public GameObject GetRelic()
        {
            return GetGameObject(_titansRelicGUID);
        }

        // Define if player can interact with the relic
        public void SetRelicInteractible(bool allow)
        {
            _isRelicInteractible = allow;
        }

        private uint GetSpiritGraveyardId(uint areaId)
        {
            switch ((AreaId)areaId)
            {
                case AreaId.WintergraspFortress:
                    return WGGraveyardId.Keep;
                case AreaId.TheSunkenRing:
                    return WGGraveyardId.WorkshopNE;
                case AreaId.TheBrokenTemplate:
                    return WGGraveyardId.WorkshopNW;
                case AreaId.WestparkWorkshop:
                    return WGGraveyardId.WorkshopSW;
                case AreaId.EastparkWorkshop:
                    return WGGraveyardId.WorkshopSE;
                case AreaId.Wintergrasp:
                    return WGGraveyardId.Alliance;
                case AreaId.TheChilledQuagmire:
                    return WGGraveyardId.Horde;
                default:
                    Log.outError(LogFilter.Battlefield, "BattlefieldWG.GetSpiritGraveyardId: Unexpected Area Id {0}", areaId);

                    break;
            }

            return 0;
        }

        private bool FindAndRemoveVehicleFromList(Unit vehicle)
        {
            for (byte i = 0; i < SharedConst.PvpTeamsCount; ++i)
                if (_vehicles[i].Contains(vehicle.GetGUID()))
                {
                    _vehicles[i].Remove(vehicle.GetGUID());

                    if (i == TeamId.Horde)
                        UpdateData(WGData.VehicleH, -1);
                    else
                        UpdateData(WGData.VehicleA, -1);

                    return true;
                }

            return false;
        }

        // Update rank for player
        private void PromotePlayer(Player killer)
        {
            if (!m_isActive)
                return;

            // Updating rank of player
            Aura aur = killer.GetAura(WGSpells.Recruit);

            if (aur != null)
            {
                if (aur.GetStackAmount() >= 5)
                {
                    killer.RemoveAura(WGSpells.Recruit);
                    killer.CastSpell(killer, WGSpells.Corporal, true);
                    Creature stalker = GetCreature(StalkerGuid);

                    if (stalker)
                        Global.CreatureTextMgr.SendChat(stalker, WintergraspText.RankCorporal, killer, ChatMsg.Addon, Language.Addon, CreatureTextRange.Normal, 0, SoundKitPlayType.Normal, Team.Other, false, killer);
                }
                else
                {
                    killer.CastSpell(killer, WGSpells.Recruit, true);
                }
            }
            else if ((aur = killer.GetAura(WGSpells.Corporal)) != null)
            {
                if (aur.GetStackAmount() >= 5)
                {
                    killer.RemoveAura(WGSpells.Corporal);
                    killer.CastSpell(killer, WGSpells.Lieutenant, true);
                    Creature stalker = GetCreature(StalkerGuid);

                    if (stalker)
                        Global.CreatureTextMgr.SendChat(stalker, WintergraspText.RankFirstLieutenant, killer, ChatMsg.Addon, Language.Addon, CreatureTextRange.Normal, 0, SoundKitPlayType.Normal, Team.Other, false, killer);
                }
                else
                {
                    killer.CastSpell(killer, WGSpells.Corporal, true);
                }
            }
        }

        private void RemoveAurasFromPlayer(Player player)
        {
            player.RemoveAura(WGSpells.Recruit);
            player.RemoveAura(WGSpells.Corporal);
            player.RemoveAura(WGSpells.Lieutenant);
            player.RemoveAura(WGSpells.TowerControl);
            player.RemoveAura(WGSpells.SpiritualImmunity);
            player.RemoveAura(WGSpells.Tenacity);
            player.RemoveAura(WGSpells.EssenceOfWintergrasp);
            player.RemoveAura(WGSpells.WintergraspRestrictedFlightArea);
        }

        // Update vehicle Count WorldState to player
        private void UpdateVehicleCountWG()
        {
            Global.WorldStateMgr.SetValue(WorldStates.BattlefieldWgVehicleH, (int)GetData(WGData.VehicleH), false, m_Map);
            Global.WorldStateMgr.SetValue(WorldStates.BattlefieldWgMaxVehicleH, (int)GetData(WGData.MaxVehicleH), false, m_Map);
            Global.WorldStateMgr.SetValue(WorldStates.BattlefieldWgVehicleA, (int)GetData(WGData.VehicleA), false, m_Map);
            Global.WorldStateMgr.SetValue(WorldStates.BattlefieldWgMaxVehicleA, (int)GetData(WGData.MaxVehicleA), false, m_Map);
        }

        private void UpdateTenacity()
        {
            int alliancePlayers = m_PlayersInWar[TeamId.Alliance].Count;
            int hordePlayers = m_PlayersInWar[TeamId.Horde].Count;
            int newStack = 0;

            if (alliancePlayers != 0 &&
                hordePlayers != 0)
            {
                if (alliancePlayers < hordePlayers)
                    newStack = (int)((((double)hordePlayers / alliancePlayers) - 1) * 4); // positive, should cast on alliance
                else if (alliancePlayers > hordePlayers)
                    newStack = (int)((1 - ((double)alliancePlayers / hordePlayers)) * 4); // negative, should cast on horde
            }

            if (newStack == _tenacityStack)
                return;

            _tenacityStack = (uint)newStack;

            // Remove old buff
            if (_tenacityTeam != TeamId.Neutral)
            {
                foreach (var guid in m_players[_tenacityTeam])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);

                    if (player)
                        if (player.GetLevel() >= m_MinLevel)
                            player.RemoveAura(WGSpells.Tenacity);
                }

                foreach (var guid in _vehicles[_tenacityTeam])
                {
                    Creature creature = GetCreature(guid);

                    if (creature)
                        creature.RemoveAura(WGSpells.TenacityVehicle);
                }
            }

            // Apply new buff
            if (newStack != 0)
            {
                _tenacityTeam = newStack > 0 ? TeamId.Alliance : TeamId.Horde;

                if (newStack < 0)
                    newStack = -newStack;

                if (newStack > 20)
                    newStack = 20;

                uint buff_honor = WGSpells.GreatestHonor;

                if (newStack < 15)
                    buff_honor = WGSpells.GreaterHonor;

                if (newStack < 10)
                    buff_honor = WGSpells.GreatHonor;

                if (newStack < 5)
                    buff_honor = 0;

                foreach (var guid in m_PlayersInWar[_tenacityTeam])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);

                    if (player)
                        player.SetAuraStack(WGSpells.Tenacity, player, (uint)newStack);
                }

                foreach (var guid in _vehicles[_tenacityTeam])
                {
                    Creature creature = GetCreature(guid);

                    if (creature)
                        creature.SetAuraStack(WGSpells.TenacityVehicle, creature, (uint)newStack);
                }

                if (buff_honor != 0)
                {
                    foreach (var guid in m_PlayersInWar[_tenacityTeam])
                    {
                        Player player = Global.ObjAccessor.FindPlayer(guid);

                        if (player)
                            player.CastSpell(player, buff_honor, true);
                    }

                    foreach (var guid in _vehicles[_tenacityTeam])
                    {
                        Creature creature = GetCreature(guid);

                        if (creature)
                            creature.CastSpell(creature, buff_honor, true);
                    }
                }
            }
            else
            {
                _tenacityTeam = TeamId.Neutral;
            }
        }

        // Define relic object
        private void SetRelic(ObjectGuid relicGUID)
        {
            _titansRelicGUID = relicGUID;
        }

        // Check if players can interact with the relic (Only if the last door has been broken)
        private bool CanInteractWithRelic()
        {
            return _isRelicInteractible;
        }
    }

    internal class BfWGGameObjectBuilding
    {
        // Creature associations
        private readonly List<ObjectGuid>[] _CreatureBottomList = new List<ObjectGuid>[SharedConst.PvpTeamsCount];
        private readonly List<ObjectGuid>[] _CreatureTopList = new List<ObjectGuid>[SharedConst.PvpTeamsCount];

        // GameObject associations
        private readonly List<ObjectGuid>[] _GameObjectList = new List<ObjectGuid>[SharedConst.PvpTeamsCount];
        private readonly List<ObjectGuid> _TowerCannonBottomList = new();
        private readonly List<ObjectGuid> _TurretTopList = new();

        private readonly WGGameObjectBuildingType _type;

        // WG object
        private readonly BattlefieldWG _wg;

        private readonly uint _worldState;

        // Linked gameobject
        private ObjectGuid _buildGUID;

        private WGGameObjectState _state;

        private StaticWintergraspTowerInfo _staticTowerInfo;

        // the team that controls this point
        private uint _teamControl;

        public BfWGGameObjectBuilding(BattlefieldWG WG, WGGameObjectBuildingType type, uint worldState)
        {
            _wg = WG;
            _teamControl = TeamId.Neutral;
            _type = type;
            _worldState = worldState;
            _state = WGGameObjectState.None;

            for (var i = 0; i < 2; ++i)
            {
                _GameObjectList[i] = new List<ObjectGuid>();
                _CreatureBottomList[i] = new List<ObjectGuid>();
                _CreatureTopList[i] = new List<ObjectGuid>();
            }
        }

        public void Rebuild()
        {
            switch (_type)
            {
                case WGGameObjectBuildingType.KeepTower:
                case WGGameObjectBuildingType.DoorLast:
                case WGGameObjectBuildingType.Door:
                case WGGameObjectBuildingType.Wall:
                    _teamControl = _wg.GetDefenderTeam(); // Objects that are part of the keep should be the defender's

                    break;
                case WGGameObjectBuildingType.Tower:
                    _teamControl = _wg.GetAttackerTeam(); // The towers in the south should be the Attacker's

                    break;
                default:
                    _teamControl = TeamId.Neutral;

                    break;
            }

            GameObject build = _wg.GetGameObject(_buildGUID);

            if (build)
            {
                // Rebuild gameobject
                if (build.IsDestructibleBuilding())
                {
                    build.SetDestructibleState(GameObjectDestructibleState.Rebuilding, null, true);

                    if (build.GetEntry() == WGGameObjects.VaultGate)
                    {
                        GameObject go = build.FindNearestGameObject(WGGameObjects.KeepCollisionWall, 50.0f);

                        if (go)
                            go.SetGoState(GameObjectState.Active);
                    }

                    // Update worldstate
                    _state = WGGameObjectState.AllianceIntact - ((int)_teamControl * 3);
                    Global.WorldStateMgr.SetValueAndSaveInDb((int)_worldState, (int)_state, false, _wg.GetMap());
                }

                UpdateCreatureAndGo();
                build.SetFaction(WGConst.WintergraspFaction[_teamControl]);
            }
        }

        public void RebuildGate()
        {
            GameObject build = _wg.GetGameObject(_buildGUID);

            if (build != null)
                if (build.IsDestructibleBuilding() &&
                    build.GetEntry() == WGGameObjects.VaultGate)
                {
                    GameObject go = build.FindNearestGameObject(WGGameObjects.KeepCollisionWall, 50.0f);

                    go?.SetGoState(GameObjectState.Ready); //not GO_STATE_ACTIVE
                }
        }

        // Called when associated gameobject is damaged
        public void Damaged()
        {
            // Update worldstate
            _state = WGGameObjectState.AllianceDamage - ((int)_teamControl * 3);
            Global.WorldStateMgr.SetValueAndSaveInDb((int)_worldState, (int)_state, false, _wg.GetMap());

            // Send warning message
            if (_staticTowerInfo != null) // tower Damage + Name
                _wg.SendWarning(_staticTowerInfo.DamagedTextId);

            foreach (var guid in _CreatureTopList[_wg.GetAttackerTeam()])
            {
                Creature creature = _wg.GetCreature(guid);

                if (creature)
                    _wg.HideNpc(creature);
            }

            foreach (var guid in _TurretTopList)
            {
                Creature creature = _wg.GetCreature(guid);

                if (creature)
                    _wg.HideNpc(creature);
            }

            if (_type == WGGameObjectBuildingType.KeepTower)
                _wg.UpdateDamagedTowerCount(_wg.GetDefenderTeam());
            else if (_type == WGGameObjectBuildingType.Tower)
                _wg.UpdateDamagedTowerCount(_wg.GetAttackerTeam());
        }

        // Called when associated gameobject is destroyed
        public void Destroyed()
        {
            // Update worldstate
            _state = WGGameObjectState.AllianceDestroy - ((int)_teamControl * 3);
            Global.WorldStateMgr.SetValueAndSaveInDb((int)_worldState, (int)_state, false, _wg.GetMap());

            // Warn players
            if (_staticTowerInfo != null)
                _wg.SendWarning(_staticTowerInfo.DestroyedTextId);

            switch (_type)
            {
                // Inform the global wintergrasp script of the destruction of this object
                case WGGameObjectBuildingType.Tower:
                case WGGameObjectBuildingType.KeepTower:
                    _wg.UpdatedDestroyedTowerCount(_teamControl);

                    break;
                case WGGameObjectBuildingType.DoorLast:
                    GameObject build = _wg.GetGameObject(_buildGUID);

                    if (build)
                    {
                        GameObject go = build.FindNearestGameObject(WGGameObjects.KeepCollisionWall, 50.0f);

                        if (go)
                            go.SetGoState(GameObjectState.Active);
                    }

                    _wg.SetRelicInteractible(true);

                    if (_wg.GetRelic())
                        _wg.GetRelic().RemoveFlag(GameObjectFlags.InUse | GameObjectFlags.NotSelectable);
                    else
                        Log.outError(LogFilter.Server, "BattlefieldWG: Titan Relic not found.");

                    break;
            }

            _wg.BrokenWallOrTower(_teamControl, this);
        }

        public void Init(GameObject go)
        {
            if (!go)
                return;

            // GameObject associated to object
            _buildGUID = go.GetGUID();

            switch (_type)
            {
                case WGGameObjectBuildingType.KeepTower:
                case WGGameObjectBuildingType.DoorLast:
                case WGGameObjectBuildingType.Door:
                case WGGameObjectBuildingType.Wall:
                    _teamControl = _wg.GetDefenderTeam(); // Objects that are part of the keep should be the defender's

                    break;
                case WGGameObjectBuildingType.Tower:
                    _teamControl = _wg.GetAttackerTeam(); // The towers in the south should be the Attacker's

                    break;
                default:
                    _teamControl = TeamId.Neutral;

                    break;
            }

            _state = (WGGameObjectState)Global.WorldStateMgr.GetValue((int)_worldState, _wg.GetMap());

            if (_state == WGGameObjectState.None)
            {
                // set to default State based on Type
                switch (_teamControl)
                {
                    case TeamId.Alliance:
                        _state = WGGameObjectState.AllianceIntact;

                        break;
                    case TeamId.Horde:
                        _state = WGGameObjectState.HordeIntact;

                        break;
                    case TeamId.Neutral:
                        _state = WGGameObjectState.NeutralIntact;

                        break;
                    default:
                        break;
                }

                Global.WorldStateMgr.SetValueAndSaveInDb((int)_worldState, (int)_state, false, _wg.GetMap());
            }

            switch (_state)
            {
                case WGGameObjectState.NeutralIntact:
                case WGGameObjectState.AllianceIntact:
                case WGGameObjectState.HordeIntact:
                    go.SetDestructibleState(GameObjectDestructibleState.Rebuilding, null, true);

                    break;
                case WGGameObjectState.NeutralDestroy:
                case WGGameObjectState.AllianceDestroy:
                case WGGameObjectState.HordeDestroy:
                    go.SetDestructibleState(GameObjectDestructibleState.Destroyed);

                    break;
                case WGGameObjectState.NeutralDamage:
                case WGGameObjectState.AllianceDamage:
                case WGGameObjectState.HordeDamage:
                    go.SetDestructibleState(GameObjectDestructibleState.Damaged);

                    break;
            }

            int towerId = -1;

            switch (go.GetEntry())
            {
                case WGGameObjects.FortressTower1:
                    towerId = 0;

                    break;
                case WGGameObjects.FortressTower2:
                    towerId = 1;

                    break;
                case WGGameObjects.FortressTower3:
                    towerId = 2;

                    break;
                case WGGameObjects.FortressTower4:
                    towerId = 3;

                    break;
                case WGGameObjects.ShadowsightTower:
                    towerId = 4;

                    break;
                case WGGameObjects.WinterSEdgeTower:
                    towerId = 5;

                    break;
                case WGGameObjects.FlamewatchTower:
                    towerId = 6;

                    break;
            }

            if (towerId > 3) // Attacker towers
            {
                // Spawn associate gameobjects
                foreach (var gobData in WGConst.AttackTowers[towerId - 4].GameObject)
                {
                    GameObject goHorde = _wg.SpawnGameObject(gobData.HordeEntry, gobData.Pos, gobData.Rot);

                    if (goHorde)
                        _GameObjectList[TeamId.Horde].Add(goHorde.GetGUID());

                    GameObject goAlliance = _wg.SpawnGameObject(gobData.AllianceEntry, gobData.Pos, gobData.Rot);

                    if (goAlliance)
                        _GameObjectList[TeamId.Alliance].Add(goAlliance.GetGUID());
                }

                // Spawn associate npc bottom
                foreach (var creatureData in WGConst.AttackTowers[towerId - 4].CreatureBottom)
                {
                    Creature creature = _wg.SpawnCreature(creatureData.HordeEntry, creatureData.Pos);

                    if (creature)
                        _CreatureBottomList[TeamId.Horde].Add(creature.GetGUID());

                    creature = _wg.SpawnCreature(creatureData.AllianceEntry, creatureData.Pos);

                    if (creature)
                        _CreatureBottomList[TeamId.Alliance].Add(creature.GetGUID());
                }
            }

            if (towerId >= 0)
            {
                _staticTowerInfo = WGConst.TowerData[towerId];

                // Spawn Turret bottom
                foreach (var turretPos in WGConst.TowerCannon[towerId].TowerCannonBottom)
                {
                    Creature turret = _wg.SpawnCreature(WGNpcs.TowerCannon, turretPos);

                    if (turret)
                    {
                        _TowerCannonBottomList.Add(turret.GetGUID());

                        switch (go.GetEntry())
                        {
                            case WGGameObjects.FortressTower1:
                            case WGGameObjects.FortressTower2:
                            case WGGameObjects.FortressTower3:
                            case WGGameObjects.FortressTower4:
                                turret.SetFaction(WGConst.WintergraspFaction[_wg.GetDefenderTeam()]);

                                break;
                            case WGGameObjects.ShadowsightTower:
                            case WGGameObjects.WinterSEdgeTower:
                            case WGGameObjects.FlamewatchTower:
                                turret.SetFaction(WGConst.WintergraspFaction[_wg.GetAttackerTeam()]);

                                break;
                        }

                        _wg.HideNpc(turret);
                    }
                }

                // Spawn Turret top
                foreach (var towerCannonPos in WGConst.TowerCannon[towerId].TurretTop)
                {
                    Creature turret = _wg.SpawnCreature(WGNpcs.TowerCannon, towerCannonPos);

                    if (turret)
                    {
                        _TurretTopList.Add(turret.GetGUID());

                        switch (go.GetEntry())
                        {
                            case WGGameObjects.FortressTower1:
                            case WGGameObjects.FortressTower2:
                            case WGGameObjects.FortressTower3:
                            case WGGameObjects.FortressTower4:
                                turret.SetFaction(WGConst.WintergraspFaction[_wg.GetDefenderTeam()]);

                                break;
                            case WGGameObjects.ShadowsightTower:
                            case WGGameObjects.WinterSEdgeTower:
                            case WGGameObjects.FlamewatchTower:
                                turret.SetFaction(WGConst.WintergraspFaction[_wg.GetAttackerTeam()]);

                                break;
                        }

                        _wg.HideNpc(turret);
                    }
                }

                UpdateCreatureAndGo();
            }
        }

        public void UpdateTurretAttack(bool disable)
        {
            foreach (var guid in _TowerCannonBottomList)
            {
                Creature creature = _wg.GetCreature(guid);

                if (creature)
                {
                    GameObject build = _wg.GetGameObject(_buildGUID);

                    if (build)
                    {
                        if (disable)
                            _wg.HideNpc(creature);
                        else
                            _wg.ShowNpc(creature, true);

                        switch (build.GetEntry())
                        {
                            case WGGameObjects.FortressTower1:
                            case WGGameObjects.FortressTower2:
                            case WGGameObjects.FortressTower3:
                            case WGGameObjects.FortressTower4:
                                {
                                    creature.SetFaction(WGConst.WintergraspFaction[_wg.GetDefenderTeam()]);

                                    break;
                                }
                            case WGGameObjects.ShadowsightTower:
                            case WGGameObjects.WinterSEdgeTower:
                            case WGGameObjects.FlamewatchTower:
                                {
                                    creature.SetFaction(WGConst.WintergraspFaction[_wg.GetAttackerTeam()]);

                                    break;
                                }
                        }
                    }
                }
            }

            foreach (var guid in _TurretTopList)
            {
                Creature creature = _wg.GetCreature(guid);

                if (creature)
                {
                    GameObject build = _wg.GetGameObject(_buildGUID);

                    if (build)
                    {
                        if (disable)
                            _wg.HideNpc(creature);
                        else
                            _wg.ShowNpc(creature, true);

                        switch (build.GetEntry())
                        {
                            case WGGameObjects.FortressTower1:
                            case WGGameObjects.FortressTower2:
                            case WGGameObjects.FortressTower3:
                            case WGGameObjects.FortressTower4:
                                {
                                    creature.SetFaction(WGConst.WintergraspFaction[_wg.GetDefenderTeam()]);

                                    break;
                                }
                            case WGGameObjects.ShadowsightTower:
                            case WGGameObjects.WinterSEdgeTower:
                            case WGGameObjects.FlamewatchTower:
                                {
                                    creature.SetFaction(WGConst.WintergraspFaction[_wg.GetAttackerTeam()]);

                                    break;
                                }
                        }
                    }
                }
            }
        }

        public ObjectGuid GetGUID()
        {
            return _buildGUID;
        }

        private void UpdateCreatureAndGo()
        {
            foreach (var guid in _CreatureTopList[_wg.GetDefenderTeam()])
            {
                Creature creature = _wg.GetCreature(guid);

                if (creature)
                    _wg.HideNpc(creature);
            }

            foreach (var guid in _CreatureTopList[_wg.GetAttackerTeam()])
            {
                Creature creature = _wg.GetCreature(guid);

                if (creature)
                    _wg.ShowNpc(creature, true);
            }

            foreach (var guid in _CreatureBottomList[_wg.GetDefenderTeam()])
            {
                Creature creature = _wg.GetCreature(guid);

                if (creature)
                    _wg.HideNpc(creature);
            }

            foreach (var guid in _CreatureBottomList[_wg.GetAttackerTeam()])
            {
                Creature creature = _wg.GetCreature(guid);

                if (creature)
                    _wg.ShowNpc(creature, true);
            }

            foreach (var guid in _GameObjectList[_wg.GetDefenderTeam()])
            {
                GameObject obj = _wg.GetGameObject(guid);

                if (obj)
                    obj.SetRespawnTime(Time.Day);
            }

            foreach (var guid in _GameObjectList[_wg.GetAttackerTeam()])
            {
                GameObject obj = _wg.GetGameObject(guid);

                if (obj)
                    obj.SetRespawnTime(0);
            }
        }
    }

    internal class WGWorkshop
    {
        private readonly StaticWintergraspWorkshopInfo _staticInfo;

        private readonly BattlefieldWG _wg; // Pointer to wintergrasp

        //ObjectGuid _buildGUID;
        private WGGameObjectState _state; // For worldstate
        private uint _teamControl;        // Team witch control the workshop

        public WGWorkshop(BattlefieldWG wg, byte type)
        {
            _wg = wg;
            _state = WGGameObjectState.None;
            _teamControl = TeamId.Neutral;
            _staticInfo = WGConst.WorkshopData[type];
        }

        public byte GetId()
        {
            return _staticInfo.WorkshopId;
        }

        public void GiveControlTo(uint teamId, bool init)
        {
            switch (teamId)
            {
                case TeamId.Neutral:
                    {
                        // Send warning message to all player to inform a faction attack to a workshop
                        // alliance / horde attacking a workshop
                        _wg.SendWarning(_teamControl != 0 ? _staticInfo.HordeAttackTextId : _staticInfo.AllianceAttackTextId);

                        break;
                    }
                case TeamId.Alliance:
                    {
                        // Updating worldstate
                        _state = WGGameObjectState.AllianceIntact;
                        Global.WorldStateMgr.SetValueAndSaveInDb(_staticInfo.WorldStateId, (int)_state, false, _wg.GetMap());

                        // Warning message
                        if (!init)
                            _wg.SendWarning(_staticInfo.AllianceCaptureTextId); // workshop taken - alliance

                        // Found associate graveyard and update it
                        if (_staticInfo.WorkshopId < WGWorkshopIds.KeepWest)
                        {
                            BfGraveyard gy = _wg.GetGraveyardById(_staticInfo.WorkshopId);

                            gy?.GiveControlTo(TeamId.Alliance);
                        }

                        _teamControl = teamId;

                        break;
                    }
                case TeamId.Horde:
                    {
                        // Update worldstate
                        _state = WGGameObjectState.HordeIntact;
                        Global.WorldStateMgr.SetValueAndSaveInDb(_staticInfo.WorldStateId, (int)_state, false, _wg.GetMap());

                        // Warning message
                        if (!init)
                            _wg.SendWarning(_staticInfo.HordeCaptureTextId); // workshop taken - horde

                        // Update graveyard control
                        if (_staticInfo.WorkshopId < WGWorkshopIds.KeepWest)
                        {
                            BfGraveyard gy = _wg.GetGraveyardById(_staticInfo.WorkshopId);

                            gy?.GiveControlTo(TeamId.Horde);
                        }

                        _teamControl = teamId;

                        break;
                    }
            }

            if (!init)
                _wg.UpdateCounterVehicle(false);
        }

        public void UpdateGraveyardAndWorkshop()
        {
            if (_staticInfo.WorkshopId < WGWorkshopIds.Ne)
                GiveControlTo(_wg.GetAttackerTeam(), true);
            else
                GiveControlTo(_wg.GetDefenderTeam(), true);
        }

        public uint GetTeamControl()
        {
            return _teamControl;
        }
    }

    internal class WintergraspCapturePoint : BfCapturePoint
    {
        protected WGWorkshop _Workshop;

        public WintergraspCapturePoint(BattlefieldWG battlefield, uint teamInControl)
            : base(battlefield)
        {
            m_Bf = battlefield;
            m_team = teamInControl;
        }

        public void LinkToWorkshop(WGWorkshop workshop)
        {
            _Workshop = workshop;
        }

        public override void ChangeTeam(uint oldteam)
        {
            Cypher.Assert(_Workshop != null);
            _Workshop.GiveControlTo(m_team, false);
        }

        private uint GetTeam()
        {
            return m_team;
        }
    }

    internal class BfGraveyardWG : BfGraveyard
    {
        protected int _GossipTextId;

        public BfGraveyardWG(BattlefieldWG battlefield)
            : base(battlefield)
        {
            m_Bf = battlefield;
            _GossipTextId = 0;
        }

        public void SetTextId(int textid)
        {
            _GossipTextId = textid;
        }

        private int GetTextId()
        {
            return _GossipTextId;
        }
    }

    [Script]
    internal class Battlefield_wintergrasp : ScriptObjectAutoAddDBBound, IBattlefieldGetBattlefield
    {
        public Battlefield_wintergrasp() : base("battlefield_wg")
        {
        }

        public BattleField GetBattlefield(Map map)
        {
            return new BattlefieldWG(map);
        }
    }

    [Script]
    internal class npc_wg_give_promotion_credit : ScriptedAI
    {
        public npc_wg_give_promotion_credit(Creature creature) : base(creature)
        {
        }

        public override void JustDied(Unit killer)
        {
            if (!killer ||
                !killer.IsPlayer())
                return;

            BattlefieldWG wintergrasp = (BattlefieldWG)Global.BattleFieldMgr.GetBattlefieldByBattleId(killer.GetMap(), BattlefieldIds.WG);

            if (wintergrasp == null)
                return;

            wintergrasp.HandlePromotion(killer.ToPlayer(), me);
        }
    }
}