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
using Game.Spells;
using System.Collections.Generic;

namespace Game.BattleFields
{
    class BattlefieldWG : BattleField
    {
        public override bool SetupBattlefield()
        {
            InitStalker(WGNpcs.Stalker, WGConst.WintergraspStalkerPos);

            m_TypeId = (uint)BattleFieldTypes.WinterGrasp;                              // See enum BattlefieldTypes
            m_BattleId = BattlefieldIds.WG;
            m_ZoneId = WGConst.ZoneId;
            m_MapId = WGConst.MapId;
            m_Map = Global.MapMgr.FindMap(m_MapId, 0);

            m_MaxPlayer = WorldConfig.GetUIntValue(WorldCfg.WintergraspPlrMax);
            m_IsEnabled = WorldConfig.GetBoolValue(WorldCfg.WintergraspEnable);
            m_MinPlayer = WorldConfig.GetUIntValue(WorldCfg.WintergraspPlrMin);
            m_MinLevel = WorldConfig.GetUIntValue(WorldCfg.WintergraspPlrMinLvl);
            m_BattleTime = WorldConfig.GetUIntValue(WorldCfg.WintergraspBattletime) * Time.Minute * Time.InMilliseconds;
            m_NoWarBattleTime = WorldConfig.GetUIntValue(WorldCfg.WintergraspNobattletime) * Time.Minute * Time.InMilliseconds;
            m_RestartAfterCrash = WorldConfig.GetUIntValue(WorldCfg.WintergraspRestartAfterCrash) * Time.Minute * Time.InMilliseconds;

            m_TimeForAcceptInvite = 20;
            m_StartGroupingTimer = 15 * Time.Minute * Time.InMilliseconds;
            m_tenacityTeam = TeamId.Neutral;

            KickPosition = new WorldLocation(m_MapId, 5728.117f, 2714.346f, 697.733f, 0);

            RegisterZone(m_ZoneId);

            for (var team = 0; team < SharedConst.BGTeamsCount; ++team)
            {
                DefenderPortalList[team] = new List<ObjectGuid>();
                m_vehicles[team] = new List<ObjectGuid>();
            }

            m_saveTimer = 60000;

            // Load from db
            if ((Global.WorldMgr.getWorldState(WGWorldStates.Active) == 0) && (Global.WorldMgr.getWorldState(WGWorldStates.Defender) == 0) && (Global.WorldMgr.getWorldState(WGConst.ClockWorldState[0]) == 0))
            {
                Global.WorldMgr.setWorldState(WGWorldStates.Active, 0);
                Global.WorldMgr.setWorldState(WGWorldStates.Defender, RandomHelper.URand(0, 1));
                Global.WorldMgr.setWorldState(WGConst.ClockWorldState[0], m_NoWarBattleTime);
            }

            m_isActive = Global.WorldMgr.getWorldState(WGWorldStates.Active) != 0;
            m_DefenderTeam = Global.WorldMgr.getWorldState(WGWorldStates.Defender);

            m_Timer = Global.WorldMgr.getWorldState(WGConst.ClockWorldState[0]);
            if (m_isActive)
            {
                m_isActive = false;
                m_Timer = m_RestartAfterCrash;
            }

            SetData(WGData.WonA, Global.WorldMgr.getWorldState(WGWorldStates.AttackedA));
            SetData(WGData.DefA, Global.WorldMgr.getWorldState(WGWorldStates.DefendedA));
            SetData(WGData.WonH, Global.WorldMgr.getWorldState(WGWorldStates.AttackedH));
            SetData(WGData.DefH, Global.WorldMgr.getWorldState(WGWorldStates.DefendedH));

            foreach (var gy in WGConst.WGGraveYard)
            {
                BfGraveyardWG graveyard = new BfGraveyardWG(this);

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
                WGWorkshop workshop = new WGWorkshop(this, i);
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
                    BfWGGameObjectBuilding b = new BfWGGameObjectBuilding(this, build.BuildingType, build.WorldState);
                    b.Init(go);
                    if (!IsEnabled() && go.GetEntry() == WGGameObjects.VaultGate)
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

        public override bool Update(uint diff)
        {
            bool m_return = base.Update(diff);
            if (m_saveTimer <= diff)
            {
                Global.WorldMgr.setWorldState(WGWorldStates.Active, m_isActive);
                Global.WorldMgr.setWorldState(WGWorldStates.Defender, m_DefenderTeam);
                Global.WorldMgr.setWorldState(WGConst.ClockWorldState[0], m_Timer);
                Global.WorldMgr.setWorldState(WGWorldStates.AttackedA, GetData(WGData.WonA));
                Global.WorldMgr.setWorldState(WGWorldStates.DefendedA, GetData(WGData.DefA));
                Global.WorldMgr.setWorldState(WGWorldStates.AttackedH, GetData(WGData.WonH));
                Global.WorldMgr.setWorldState(WGWorldStates.DefendedH, GetData(WGData.DefH));
                m_saveTimer = 60 * Time.InMilliseconds;
            }
            else
                m_saveTimer -= diff;

            return m_return;
        }

        public override void OnBattleStart()
        {
            // Spawn titan relic
            GameObject relic = SpawnGameObject(WGGameObjects.TitanSRelic, WGConst.RelicPos, WGConst.RelicRot);
            if (relic)
            {
                // Update faction of relic, only attacker can click on
                relic.SetFaction(WGConst.WintergraspFaction[GetAttackerTeam()]);
                // Set in use (not allow to click on before last door is broken)
                relic.SetFlag(GameObjectFields.Flags, GameObjectFlags.InUse | GameObjectFlags.NotSelectable);
                m_titansRelicGUID = relic.GetGUID();
            }
            else
                Log.outError(LogFilter.Battlefield, "WG: Failed to spawn titan relic.");


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
            {
                if (wall != null)
                {
                    wall.Rebuild();
                    wall.UpdateTurretAttack(false);
                }
            }

            SetData(WGData.BrokenTowerAtt, 0);
            SetData(WGData.BrokenTowerDef, 0);
            SetData(WGData.DamagedTowerAtt, 0);
            SetData(WGData.DamagedTowerDef, 0);

            // Update graveyard (in no war time all graveyard is to deffender, in war time, depend of base)
            foreach (var workShop in Workshops)
            {
                if (workShop != null)
                    workShop.UpdateGraveyardAndWorkshop();
            }

            for (byte team = 0; team < SharedConst.BGTeamsCount; ++team)
            {
                foreach (var guid in m_players[team])
                {
                    // Kick player in orb room, TODO: offline player ?
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    if (player)
                    {
                        float x, y, z;
                        player.GetPosition(out x, out y, out z);
                        if (5500 > x && x > 5392 && y < 2880 && y > 2800 && z < 480)
                            player.TeleportTo(571, 5349.8686f, 2838.481f, 409.240f, 0.046328f);
                        SendInitWorldStatesTo(player);
                    }
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
            {
                if (workshop.GetTeamControl() == TeamId.Alliance)
                    UpdateData(WGData.MaxVehicleA, 4);
                else if (workshop.GetTeamControl() == TeamId.Horde)
                    UpdateData(WGData.MaxVehicleH, 4);
            }

            UpdateVehicleCountWG();
        }

        public override void OnBattleEnd(bool endByTimer)
        {
            // Remove relic
            if (!m_titansRelicGUID.IsEmpty())
            {
                GameObject relic = GetGameObject(m_titansRelicGUID);
                if (relic)
                    relic.RemoveFromWorld();
            }
            m_titansRelicGUID.Clear();

            // successful defense
            if (endByTimer)
                UpdateData(GetDefenderTeam() == TeamId.Horde ? WGData.DefH : WGData.DefA, 1);
            // successful attack (note that teams have already been swapped, so defender team is the one who won)
            else
                UpdateData(GetDefenderTeam() == TeamId.Horde ? WGData.WonH : WGData.WonA, 1);

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
                if (graveyard != null)
                    graveyard.GiveControlTo(GetDefenderTeam());
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

            // Saving data
            foreach (var obj in BuildingsInZone)
                obj.Save();
            foreach (var workShop in Workshops)
                workShop.Save();

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
                    // Send Wintergrasp victory achievement
                    DoCompleteOrIncrementAchievement(WGAchievements.WinWg, player);
                    // Award achievement for succeeding in Wintergrasp in 10 minutes or less
                    if (!endByTimer && GetTimer() <= 10000)
                        DoCompleteOrIncrementAchievement(WGAchievements.WinWgTimer10, player);
                }
            }

            foreach (var guid in m_PlayersInWar[GetAttackerTeam()])
            {
                Player player = Global.ObjAccessor.FindPlayer(guid);
                if (player)
                    player.CastSpell(player, WGSpells.DefeatReward, true);
            }

            for (byte team = 0; team < SharedConst.BGTeamsCount; ++team)
            {
                foreach (var guid in m_PlayersInWar[team])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    if (player)
                        RemoveAurasFromPlayer(player);
                }

                m_PlayersInWar[team].Clear();

                foreach (var guid in m_vehicles[team])
                {
                    Creature creature = GetCreature(guid);
                    if (creature)
                        if (creature.IsVehicle())
                            creature.DespawnOrUnsummon();
                }

                m_vehicles[team].Clear();
            }

            if (!endByTimer)
            {
                for (byte team = 0; team < SharedConst.BGTeamsCount; ++team)
                {
                    foreach (var guid in m_players[team])
                    {
                        Player player = Global.ObjAccessor.FindPlayer(guid);
                        if (player)
                        {
                            player.RemoveAurasDueToSpell(m_DefenderTeam == TeamId.Alliance ? WGSpells.HordeControlPhaseShift : WGSpells.AllianceControlPhaseShift, player.GetGUID());
                            player.AddAura(m_DefenderTeam == TeamId.Horde ? WGSpells.HordeControlPhaseShift : WGSpells.AllianceControlPhaseShift, player);
                        }
                    }
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

        uint GetSpiritGraveyardId(uint areaId)
        {
            switch (areaId)
            {
                case WintergraspAreaIds.WintergraspFortress:
                    return WGGraveyardId.Keep;
                case WintergraspAreaIds.TheSunkenRing:
                    return WGGraveyardId.WorkshopNE;
                case WintergraspAreaIds.TheBrokenTemplate:
                    return WGGraveyardId.WorkshopNW;
                case WintergraspAreaIds.WestparkWorkshop:
                    return WGGraveyardId.WorkshopSW;
                case WintergraspAreaIds.EastparkWorkshop:
                    return WGGraveyardId.WorkshopSE;
                case WintergraspAreaIds.Wintergrasp:
                    return WGGraveyardId.Alliance;
                case WintergraspAreaIds.TheChilledQuagmire:
                    return WGGraveyardId.Horde;
                default:
                    Log.outError(LogFilter.Battlefield, "BattlefieldWG.GetSpiritGraveyardId: Unexpected Area Id {0}", areaId);
                    break;
            }

            return 0;
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
                        if (m_GraveyardList[graveyardId] != null)
                            m_GraveyardList[graveyardId].SetSpirit(creature, teamIndex);
                        break;
                    }
            }

            // untested code - not sure if it is valid.
            if (IsWarTime())
            {
                switch (creature.GetEntry())
                {
                    case WGNpcs.SiegeEngineAlliance:
                    case WGNpcs.SiegeEngineHorde:
                    case WGNpcs.Catapult:
                    case WGNpcs.Demolisher:
                        {
                            if (!creature.ToTempSummon() || creature.ToTempSummon().GetSummonerGUID().IsEmpty() || !Global.ObjAccessor.FindPlayer(creature.ToTempSummon().GetSummonerGUID()))
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
                                    m_vehicles[teamIndex].Add(creature.GetGUID());
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
                                    m_vehicles[teamIndex].Add(creature.GetGUID());
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
        }

        public override void OnCreatureRemove(Creature c) { }

        public override void OnGameObjectCreate(GameObject go)
        {
            uint workshopId = 0;

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
            {
                if (workshop.GetId() == workshopId)
                {
                    WintergraspCapturePoint capturePoint = new WintergraspCapturePoint(this, GetAttackerTeam());

                    capturePoint.SetCapturePointData(go);
                    capturePoint.LinkToWorkshop(workshop);
                    AddCapturePoint(capturePoint);
                    break;
                }
            }
        }

        public override void HandleKill(Player killer, Unit victim)
        {
            if (killer == victim)
                return;

            if (victim.IsTypeId(TypeId.Player))
                HandlePromotion(killer, victim);

            // @todo Recent PvP activity worldstate
        }

        bool FindAndRemoveVehicleFromList(Unit vehicle)
        {
            for (byte i = 0; i < SharedConst.BGTeamsCount; ++i)
            {
                if (m_vehicles[i].Contains(vehicle.GetGUID()))
                {
                    m_vehicles[i].Remove(vehicle.GetGUID());
                    if (i == TeamId.Horde)
                        UpdateData(WGData.VehicleH, -1);
                    else
                        UpdateData(WGData.VehicleA, -1);
                    return true;
                }
            }
            return false;
        }

        public override void OnUnitDeath(Unit unit)
        {
            if (IsWarTime())
                if (unit.IsVehicle())
                    if (FindAndRemoveVehicleFromList(unit))
                        UpdateVehicleCountWG();
        }

        void HandlePromotion(Player playerKiller, Unit unitKilled)
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

        // Update rank for player
        void PromotePlayer(Player killer)
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
                        Global.CreatureTextMgr.SendChat(stalker, WintergraspText.RankCorporal, killer, ChatMsg.Addon, Language.Addon, CreatureTextRange.Normal, 0, Team.Other, false, killer);
                }
                else
                    killer.CastSpell(killer, WGSpells.Recruit, true);
            }
            else if ((aur = killer.GetAura(WGSpells.Corporal)) != null)
            {
                if (aur.GetStackAmount() >= 5)
                {
                    killer.RemoveAura(WGSpells.Corporal);
                    killer.CastSpell(killer, WGSpells.Lieutenant, true);
                    Creature stalker = GetCreature(StalkerGuid);
                    if (stalker)
                        Global.CreatureTextMgr.SendChat(stalker, WintergraspText.RankFirstLieutenant, killer, ChatMsg.Addon, Language.Addon, CreatureTextRange.Normal, 0, Team.Other, false, killer);
                }
                else
                    killer.CastSpell(killer, WGSpells.Corporal, true);
            }
        }

        void RemoveAurasFromPlayer(Player player)
        {
            player.RemoveAurasDueToSpell(WGSpells.Recruit);
            player.RemoveAurasDueToSpell(WGSpells.Corporal);
            player.RemoveAurasDueToSpell(WGSpells.Lieutenant);
            player.RemoveAurasDueToSpell(WGSpells.TowerControl);
            player.RemoveAurasDueToSpell(WGSpells.SpiritualImmunity);
            player.RemoveAurasDueToSpell(WGSpells.Tenacity);
            player.RemoveAurasDueToSpell(WGSpells.EssenceOfWintergrasp);
            player.RemoveAurasDueToSpell(WGSpells.WintergraspRestrictedFlightArea);
        }

        public override void OnPlayerJoinWar(Player player)
        {
            RemoveAurasFromPlayer(player);

            player.CastSpell(player, WGSpells.Recruit, true);

            if (player.GetZoneId() != m_ZoneId)
            {
                if (player.GetTeamId() == GetDefenderTeam())
                    player.TeleportTo(571, 5345, 2842, 410, 3.14f);
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
            SendInitWorldStatesTo(player);
        }

        public override void OnPlayerLeaveWar(Player player)
        {
            // Remove all aura from WG // @todo false we can go out of this zone on retail and keep Rank buff, remove on end of WG
            if (!player.GetSession().PlayerLogout())
            {
                Creature vehicle = player.GetVehicleCreatureBase();
                if (vehicle)   // Remove vehicle of player if he go out.
                    vehicle.DespawnOrUnsummon();

                RemoveAurasFromPlayer(player);
            }

            player.RemoveAurasDueToSpell(WGSpells.HordeControlsFactoryPhaseShift);
            player.RemoveAurasDueToSpell(WGSpells.AllianceControlsFactoryPhaseShift);
            player.RemoveAurasDueToSpell(WGSpells.HordeControlPhaseShift);
            player.RemoveAurasDueToSpell(WGSpells.AllianceControlPhaseShift);
            UpdateTenacity();
        }

        public override void OnPlayerLeaveZone(Player player)
        {
            if (!m_isActive)
                RemoveAurasFromPlayer(player);

            player.RemoveAurasDueToSpell(WGSpells.HordeControlsFactoryPhaseShift);
            player.RemoveAurasDueToSpell(WGSpells.AllianceControlsFactoryPhaseShift);
            player.RemoveAurasDueToSpell(WGSpells.HordeControlPhaseShift);
            player.RemoveAurasDueToSpell(WGSpells.AllianceControlPhaseShift);
        }

        public override void OnPlayerEnterZone(Player player)
        {
            if (!m_isActive)
                RemoveAurasFromPlayer(player);

            player.AddAura(m_DefenderTeam == TeamId.Horde ? WGSpells.HordeControlPhaseShift : WGSpells.AllianceControlPhaseShift, player);
            // Send worldstate to player
            SendInitWorldStatesTo(player);
        }

        public override uint GetData(uint data)
        {
            switch (data)
            {
                // Used to determine when the phasing spells must be cast
                // See: SpellArea.IsFitToRequirements
                case WintergraspAreaIds.TheSunkenRing:
                case WintergraspAreaIds.TheBrokenTemplate:
                case WintergraspAreaIds.WestparkWorkshop:
                case WintergraspAreaIds.EastparkWorkshop:
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

        public override void FillInitialWorldStates(InitWorldStates packet)
        {
            packet.AddState(WGWorldStates.Attacker, (int)GetAttackerTeam());
            packet.AddState(WGWorldStates.Defender, (int)GetDefenderTeam());
            // Note: cleanup these two, their names look awkward
            packet.AddState(WGWorldStates.Active, IsWarTime());
            packet.AddState(WGWorldStates.ShowWorldstate, IsWarTime());

            for (uint i = 0; i < 2; ++i)
                packet.AddState(WGConst.ClockWorldState[i], (int)(Time.UnixTime + (m_Timer / 1000)));

            packet.AddState(WGWorldStates.VehicleH, (int)GetData(WGData.VehicleH));
            packet.AddState(WGWorldStates.MaxVehicleH, (int)GetData(WGData.MaxVehicleH));
            packet.AddState(WGWorldStates.VehicleA, (int)GetData(WGData.VehicleA));
            packet.AddState(WGWorldStates.MaxVehicleA, (int)GetData(WGData.MaxVehicleA));

            foreach (BfWGGameObjectBuilding building in BuildingsInZone)
                building.FillInitialWorldStates(packet);

            foreach (WGWorkshop workshop in Workshops)
                workshop.FillInitialWorldStates(packet);
        }

        void SendInitWorldStatesTo(Player player)
        {
            InitWorldStates packet = new InitWorldStates();
            packet.AreaID = m_ZoneId;
            packet.MapID = m_MapId;
            packet.SubareaID = 0;

            FillInitialWorldStates(packet);

            player.SendPacket(packet);
        }

        public override void SendInitWorldStatesToAll()
        {
            for (byte team = 0; team < SharedConst.BGTeamsCount; team++)
            {
                foreach (var guid in m_players[team])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    if (player)
                        SendInitWorldStatesTo(player);
                }
            }
        }

        public void BrokenWallOrTower(uint team, BfWGGameObjectBuilding building)
        {
            if (team == GetDefenderTeam())
            {
                foreach (var guid in m_PlayersInWar[GetAttackerTeam()])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    if (player)
                        if (player.GetDistance2d(GetGameObject(building.GetGUID())) < 50.0f)
                            player.KilledMonsterCredit(WintergraspQuests.CreditDefendSiege);
                }
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

                // Add buff stack to defenders and give achievement/quest credit
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

                // If all three south towers are destroyed (ie. all attack towers), remove ten minutes from battle time
                if (GetData(WGData.BrokenTowerAtt) == 3)
                {
                    if ((int)(m_Timer - 600000) < 0)
                        m_Timer = 0;
                    else
                        m_Timer -= 600000;
                    SendInitWorldStatesToAll();
                }
            }
            else // Keep tower
            {
                UpdateData(WGData.DamagedTowerDef, -1);
                UpdateData(WGData.BrokenTowerDef, 1);
            }
        }

        public override void ProcessEvent(WorldObject obj, uint eventId)
        {
            if (!obj || !IsWarTime())
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

            // if destroy or damage event, search the wall/tower and update worldstate/send warning message
            foreach (var building in BuildingsInZone)
            {
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
        }

        // Called when a tower is damaged, used for honor reward calcul
        public void UpdateDamagedTowerCount(uint team)
        {
            if (team == GetAttackerTeam())
                UpdateData(WGData.DamagedTowerAtt, 1);
            else
                UpdateData(WGData.DamagedTowerDef, 1);
        }

        // Update vehicle count WorldState to player
        void UpdateVehicleCountWG()
        {
            SendUpdateWorldState(WGWorldStates.VehicleH, GetData(WGData.VehicleH));
            SendUpdateWorldState(WGWorldStates.MaxVehicleH, GetData(WGData.MaxVehicleH));
            SendUpdateWorldState(WGWorldStates.VehicleA, GetData(WGData.VehicleA));
            SendUpdateWorldState(WGWorldStates.MaxVehicleA, GetData(WGData.MaxVehicleA));
        }

        void UpdateTenacity()
        {
            int alliancePlayers = m_PlayersInWar[TeamId.Alliance].Count;
            int hordePlayers = m_PlayersInWar[TeamId.Horde].Count;
            int newStack = 0;

            if (alliancePlayers != 0 && hordePlayers != 0)
            {
                if (alliancePlayers < hordePlayers)
                    newStack = (int)((((float)hordePlayers / alliancePlayers) - 1) * 4);  // positive, should cast on alliance
                else if (alliancePlayers > hordePlayers)
                    newStack = (int)((1 - ((float)alliancePlayers / hordePlayers)) * 4);  // negative, should cast on horde
            }

            if (newStack == m_tenacityStack)
                return;

            m_tenacityStack = (uint)newStack;
            // Remove old buff
            if (m_tenacityTeam != TeamId.Neutral)
            {
                foreach (var guid in m_players[m_tenacityTeam])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    if (player)
                        if (player.getLevel() >= m_MinLevel)
                            player.RemoveAurasDueToSpell(WGSpells.Tenacity);
                }

                foreach (var guid in m_vehicles[m_tenacityTeam])
                {
                    Creature creature = GetCreature(guid);
                    if (creature)
                        creature.RemoveAurasDueToSpell(WGSpells.TenacityVehicle);
                }
            }

            // Apply new buff
            if (newStack != 0)
            {
                m_tenacityTeam = newStack > 0 ? TeamId.Alliance : TeamId.Horde;

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

                foreach (var guid in m_PlayersInWar[m_tenacityTeam])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    if (player)
                        player.SetAuraStack(WGSpells.Tenacity, player, (uint)newStack);
                }

                foreach (var guid in m_vehicles[m_tenacityTeam])
                {
                    Creature creature = GetCreature(guid);
                    if (creature)
                        creature.SetAuraStack(WGSpells.TenacityVehicle, creature, (uint)newStack);
                }

                if (buff_honor != 0)
                {
                    foreach (var guid in m_PlayersInWar[m_tenacityTeam])
                    {
                        Player player = Global.ObjAccessor.FindPlayer(guid);
                        if (player)
                            player.CastSpell(player, buff_honor, true);
                    }

                    foreach (var guid in m_vehicles[m_tenacityTeam])
                    {
                        Creature creature = GetCreature(guid);
                        if (creature)
                            creature.CastSpell(creature, buff_honor, true);
                    }
                }
            }
            else
                m_tenacityTeam = TeamId.Neutral;
        }

        public GameObject GetRelic() { return GetGameObject(m_titansRelicGUID); }

        // Define relic object
        void SetRelic(ObjectGuid relicGUID) { m_titansRelicGUID = relicGUID; }

        // Check if players can interact with the relic (Only if the last door has been broken)
        bool CanInteractWithRelic() { return m_isRelicInteractible; }

        // Define if player can interact with the relic
        public void SetRelicInteractible(bool allow) { m_isRelicInteractible = allow; }


        bool m_isRelicInteractible;

        List<WGWorkshop> Workshops = new List<WGWorkshop>();

        List<ObjectGuid>[] DefenderPortalList = new List<ObjectGuid>[SharedConst.BGTeamsCount];
        List<BfWGGameObjectBuilding> BuildingsInZone = new List<BfWGGameObjectBuilding>();

        List<ObjectGuid>[] m_vehicles = new List<ObjectGuid>[SharedConst.BGTeamsCount];
        List<ObjectGuid> CanonList = new List<ObjectGuid>();

        int m_tenacityTeam;
        uint m_tenacityStack;
        uint m_saveTimer;

        ObjectGuid m_titansRelicGUID;
    }

    class BfWGGameObjectBuilding
    {
        public BfWGGameObjectBuilding(BattlefieldWG WG, WGGameObjectBuildingType type, uint worldState)
        {
            _wg = WG;
            _teamControl = TeamId.Neutral;
            _type = type;
            _worldState = worldState;
            _state = WGGameObjectState.None;

            for (var i = 0; i < 2; ++i)
            {
                m_GameObjectList[i] = new List<ObjectGuid>();
                m_CreatureBottomList[i] = new List<ObjectGuid>();
                m_CreatureTopList[i] = new List<ObjectGuid>();
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
                    _teamControl = _wg.GetDefenderTeam();           // Objects that are part of the keep should be the defender's
                    break;
                case WGGameObjectBuildingType.Tower:
                    _teamControl = _wg.GetAttackerTeam();           // The towers in the south should be the attacker's
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
                            go.SetGoState(GameObjectState.Ready);
                    }

                    // Update worldstate
                    _state = WGGameObjectState.AllianceIntact - ((int)_teamControl * 3);
                    _wg.SendUpdateWorldState(_worldState, (uint)_state);
                }
                UpdateCreatureAndGo();
                build.SetFaction(WGConst.WintergraspFaction[_teamControl]);
            }
        }

        // Called when associated gameobject is damaged
        public void Damaged()
        {
            // Update worldstate
            _state = WGGameObjectState.AllianceDamage - ((int)_teamControl * 3);
            _wg.SendUpdateWorldState(_worldState, (uint)_state);

            // Send warning message
            if (_staticTowerInfo != null)                                       // tower damage + name
                _wg.SendWarning(_staticTowerInfo.DamagedTextId);

            foreach (var guid in m_CreatureTopList[_wg.GetAttackerTeam()])
            {
                Creature creature = _wg.GetCreature(guid);
                if (creature)
                    _wg.HideNpc(creature);
            }

            foreach (var guid in m_TurretTopList)
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
            _wg.SendUpdateWorldState(_worldState, (uint)_state);

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
                        _wg.GetRelic().RemoveFlag(GameObjectFields.Flags, GameObjectFlags.InUse | GameObjectFlags.NotSelectable);
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
                    _teamControl = _wg.GetDefenderTeam();           // Objects that are part of the keep should be the defender's
                    break;
                case WGGameObjectBuildingType.Tower:
                    _teamControl = _wg.GetAttackerTeam();           // The towers in the south should be the attacker's
                    break;
                default:
                    _teamControl = TeamId.Neutral;
                    break;
            }

            _state = (WGGameObjectState)Global.WorldMgr.getWorldState(_worldState);
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

            if (towerId >  3) // Attacker towers
            {
                // Spawn associate gameobjects
                foreach (var gobData in WGConst.AttackTowers[towerId - 4].GameObject)
                {
                    GameObject goHorde = _wg.SpawnGameObject(gobData.HordeEntry, gobData.Pos, gobData.Rot);
                    if (goHorde)
                        m_GameObjectList[TeamId.Horde].Add(goHorde.GetGUID());

                    GameObject goAlliance = _wg.SpawnGameObject(gobData.AllianceEntry, gobData.Pos, gobData.Rot);
                    if (goAlliance)
                        m_GameObjectList[TeamId.Alliance].Add(goAlliance.GetGUID());
                }

                // Spawn associate npc bottom
                foreach (var creatureData in WGConst.AttackTowers[towerId - 4].CreatureBottom)
                {
                    Creature creature = _wg.SpawnCreature(creatureData.HordeEntry, creatureData.Pos);
                    if (creature)
                        m_CreatureBottomList[TeamId.Horde].Add(creature.GetGUID());

                    creature = _wg.SpawnCreature(creatureData.AllianceEntry, creatureData.Pos);
                    if (creature)
                        m_CreatureBottomList[TeamId.Alliance].Add(creature.GetGUID());
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
                        m_TowerCannonBottomList.Add(turret.GetGUID());
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
                        m_TurretTopList.Add(turret.GetGUID());
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

        void UpdateCreatureAndGo()
        {
            foreach (var guid in m_CreatureTopList[_wg.GetDefenderTeam()])
            {
                Creature creature = _wg.GetCreature(guid);
                if (creature)
                    _wg.HideNpc(creature);
            }

            foreach (var guid in m_CreatureTopList[_wg.GetAttackerTeam()])
            {
                Creature creature = _wg.GetCreature(guid);
                if (creature)
                    _wg.ShowNpc(creature, true);
            }

            foreach (var guid in m_CreatureBottomList[_wg.GetDefenderTeam()])
            {
                Creature creature = _wg.GetCreature(guid);
                if (creature)
                    _wg.HideNpc(creature);
            }

            foreach (var guid in m_CreatureBottomList[_wg.GetAttackerTeam()])
            {
                Creature creature = _wg.GetCreature(guid);
                if (creature)
                    _wg.ShowNpc(creature, true);
            }

            foreach (var guid in m_GameObjectList[_wg.GetDefenderTeam()])
            {
                GameObject obj = _wg.GetGameObject(guid);
                if (obj)
                    obj.SetRespawnTime(Time.Day);
            }

            foreach (var guid in m_GameObjectList[_wg.GetAttackerTeam()])
            {
                GameObject obj = _wg.GetGameObject(guid);
                if (obj)
                    obj.SetRespawnTime(0);
            }
        }

        public void UpdateTurretAttack(bool disable)
        {
            foreach (var guid in m_TowerCannonBottomList)
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

            foreach (var guid in m_TurretTopList)
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

        public void FillInitialWorldStates(InitWorldStates packet)
        {
            packet.AddState(_worldState, (int)_state);
        }

        public void Save()
        {
            Global.WorldMgr.setWorldState(_worldState, (ulong)_state);
        }

        public ObjectGuid GetGUID() { return _buildGUID; }

        // WG object
        BattlefieldWG _wg;

        // Linked gameobject
        ObjectGuid _buildGUID;

        // the team that controls this point
        uint _teamControl;

        WGGameObjectBuildingType _type;
        uint _worldState;

        WGGameObjectState _state;

        StaticWintergraspTowerInfo _staticTowerInfo;

        // GameObject associations
        List<ObjectGuid>[] m_GameObjectList = new List<ObjectGuid>[SharedConst.BGTeamsCount];

        // Creature associations
        List<ObjectGuid>[] m_CreatureBottomList = new List<ObjectGuid>[SharedConst.BGTeamsCount];
        List<ObjectGuid>[] m_CreatureTopList = new List<ObjectGuid>[SharedConst.BGTeamsCount];
        List<ObjectGuid> m_TowerCannonBottomList = new List<ObjectGuid>();
        List<ObjectGuid> m_TurretTopList = new List<ObjectGuid>();
    }

    class WGWorkshop
    {
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
                        _wg.SendUpdateWorldState(_staticInfo.WorldStateId, (uint)_state);

                        // Warning message
                        if (!init)
                            _wg.SendWarning(_staticInfo.AllianceCaptureTextId); // workshop taken - alliance

                        // Found associate graveyard and update it
                        if (_staticInfo.WorkshopId < WGWorkshopIds.KeepWest)
                        {
                            BfGraveyard gy = _wg.GetGraveyardById(_staticInfo.WorkshopId);
                            if (gy != null)
                                gy.GiveControlTo(TeamId.Alliance);
                        }
                        _teamControl = teamId;
                        break;
                    }
                case TeamId.Horde:
                    {
                        // Update worldstate
                        _state = WGGameObjectState.HordeIntact;
                        _wg.SendUpdateWorldState(_staticInfo.WorldStateId, (uint)_state);

                        // Warning message
                        if (!init)
                            _wg.SendWarning(_staticInfo.HordeCaptureTextId); // workshop taken - horde

                        // Update graveyard control
                        if (_staticInfo.WorkshopId < WGWorkshopIds.KeepWest)
                        {
                            BfGraveyard gy = _wg.GetGraveyardById(_staticInfo.WorkshopId);
                            if (gy != null)
                                gy.GiveControlTo(TeamId.Horde);
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

        public void FillInitialWorldStates(InitWorldStates packet)
        {
            packet.AddState(_staticInfo.WorldStateId, (int)_state);
        }

        public void Save()
        {
            Global.WorldMgr.setWorldState(_staticInfo.WorldStateId, (uint)_state);
        }

        public uint GetTeamControl()  { return _teamControl; }

        BattlefieldWG _wg;                             // Pointer to wintergrasp
        //ObjectGuid _buildGUID;
        WGGameObjectState _state;              // For worldstate
        uint _teamControl;                            // Team witch control the workshop

        StaticWintergraspWorkshopInfo _staticInfo;
    }

    class WintergraspCapturePoint : BfCapturePoint
    {
        public WintergraspCapturePoint(BattlefieldWG battlefield, uint teamInControl)
            : base(battlefield)
        {
            m_Bf = battlefield;
            m_team = teamInControl;
        }

        public void LinkToWorkshop(WGWorkshop workshop) { m_Workshop = workshop; }

        public override void ChangeTeam(uint oldteam)
        {
            Cypher.Assert(m_Workshop != null);
            m_Workshop.GiveControlTo(m_team, false);
        }
        uint GetTeam() { return m_team; }

        protected WGWorkshop m_Workshop;
    }

    class BfGraveyardWG : BfGraveyard
    {
        public BfGraveyardWG(BattlefieldWG battlefield)
            : base(battlefield)
        {
            m_Bf = battlefield;
            m_GossipTextId = 0;
        }

        public void SetTextId(int textid) { m_GossipTextId = textid; }
        int GetTextId() { return m_GossipTextId; }

        protected int m_GossipTextId;
    }
}
