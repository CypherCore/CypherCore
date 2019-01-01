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
using Framework.IO;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Network.Packets;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scripts.Northrend.Ulduar
{
    [Script]
    class instance_ulduar : InstanceMapScript
    {
        public instance_ulduar() : base("instance_ulduar", 603) { }

        class instance_ulduar_InstanceMapScript : InstanceScript
        {
            public instance_ulduar_InstanceMapScript(InstanceMap map) : base(map)
            {
                SetHeaders("UU");
                SetBossNumber(BossIds.MaxEncounter);

                LoadDoorData(doorData);
                LoadMinionData(minionData);

                _algalonTimer = 61;

                Unbroken = true;
                IsDriveMeCrazyEligible = true;
            }

            public override void FillInitialWorldStates(InitWorldStates packet)
            {
                packet.AddState(InstanceWorldStates.AlgalonTimerEnabled, (_algalonTimer != 0 && _algalonTimer <= 60 ? 1 : 0));
                packet.AddState(InstanceWorldStates.AlgalonDespawnTimer, (int)Math.Min(_algalonTimer, 60));
            }

            public override void OnPlayerEnter(Player player)
            {
                if (TeamInInstance == 0)
                    TeamInInstance = player.GetTeam();

                if (_summonAlgalon)
                {
                    _summonAlgalon = false;
                    TempSummon algalon = instance.SummonCreature(InstanceCreatureIds.Algalon, Algalon.AlgalonTheObserver.AlgalonLandPos);
                    if (_algalonTimer != 0 && _algalonTimer <= 60)
                        algalon.GetAI().DoAction((int)InstanceEvents.ActionInitAlgalon);
                    else
                        algalon.RemoveFlag(UnitFields.Flags, UnitFlags.ImmuneToPc);
                }

                // Keepers at Observation Ring
                if (GetBossState(BossIds.Freya) == EncounterState.Done && _summonObservationRingKeeper[0] && KeeperGUIDs[0].IsEmpty())
                {
                    _summonObservationRingKeeper[0] = false;
                    instance.SummonCreature(InstanceCreatureIds.FreyaObservationRing, YoggSaron.ObservationRingKeepersPos[0]);
                }
                if (GetBossState(BossIds.Hodir) == EncounterState.Done && _summonObservationRingKeeper[1] && KeeperGUIDs[1].IsEmpty())
                {
                    _summonObservationRingKeeper[1] = false;
                    instance.SummonCreature(InstanceCreatureIds.HodirObservationRing, YoggSaron.ObservationRingKeepersPos[1]);
                }
                if (GetBossState(BossIds.Thorim) == EncounterState.Done && _summonObservationRingKeeper[2] && KeeperGUIDs[2].IsEmpty())
                {
                    _summonObservationRingKeeper[2] = false;
                    instance.SummonCreature(InstanceCreatureIds.ThorimObservationRing, YoggSaron.ObservationRingKeepersPos[2]);
                }
                if (GetBossState(BossIds.Mimiron) == EncounterState.Done && _summonObservationRingKeeper[3] && KeeperGUIDs[3].IsEmpty())
                {
                    _summonObservationRingKeeper[3] = false;
                    instance.SummonCreature(InstanceCreatureIds.MimironObservationRing, YoggSaron.ObservationRingKeepersPos[3]);
                }

                // Keepers in Yogg-Saron's room
                if (_summonYSKeeper[0])
                    instance.SummonCreature(InstanceCreatureIds.FreyaYs, YoggSaron.YSKeepersPos[0]);
                if (_summonYSKeeper[1])
                    instance.SummonCreature(InstanceCreatureIds.HodirYs, YoggSaron.YSKeepersPos[1]);
                if (_summonYSKeeper[2])
                    instance.SummonCreature(InstanceCreatureIds.ThorimYs, YoggSaron.YSKeepersPos[2]);
                if (_summonYSKeeper[3])
                    instance.SummonCreature(InstanceCreatureIds.MimironYs, YoggSaron.YSKeepersPos[3]);
            }

            public override void OnCreatureCreate(Creature creature)
            {
                if (TeamInInstance == 0)
                {
                    var Players = instance.GetPlayers();
                    if (!Players.Empty())
                    {
                        Player player = Players.First();
                        if (player)
                            TeamInInstance = player.GetTeam();
                    }
                }

                switch (creature.GetEntry())
                {
                    case InstanceCreatureIds.Leviathan:
                        LeviathanGUID = creature.GetGUID();
                        break;
                    case InstanceCreatureIds.SalvagedDemolisher:
                    case InstanceCreatureIds.SalvagedSiegeEngine:
                    case InstanceCreatureIds.SalvagedChopper:
                        LeviathanVehicleGUIDs.Add(creature.GetGUID());
                        break;
                    case InstanceCreatureIds.Ignis:
                        IgnisGUID = creature.GetGUID();
                        break;

                    // Razorscale
                    case InstanceCreatureIds.Razorscale:
                        RazorscaleGUID = creature.GetGUID();
                        break;
                    case InstanceCreatureIds.RazorscaleController:
                        RazorscaleController = creature.GetGUID();
                        break;
                    case InstanceCreatureIds.ExpeditionCommander:
                        ExpeditionCommanderGUID = creature.GetGUID();
                        break;

                    // XT-002 Deconstructor
                    case InstanceCreatureIds.Xt002:
                        XT002GUID = creature.GetGUID();
                        break;
                    case InstanceCreatureIds.XtToyPile:
                        for (byte i = 0; i < 4; ++i)
                        {
                            if (XTToyPileGUIDs[i].IsEmpty())
                            {
                                XTToyPileGUIDs[i] = creature.GetGUID();
                                break;
                            }
                        }
                        break;

                    // Assembly of Iron
                    case InstanceCreatureIds.Steelbreaker:
                        AssemblyGUIDs[0] = creature.GetGUID();
                        AddMinion(creature, true);
                        break;
                    case InstanceCreatureIds.Molgeim:
                        AssemblyGUIDs[1] = creature.GetGUID();
                        AddMinion(creature, true);
                        break;
                    case InstanceCreatureIds.Brundir:
                        AssemblyGUIDs[2] = creature.GetGUID();
                        AddMinion(creature, true);
                        break;

                    case InstanceCreatureIds.Kologarn:
                        KologarnGUID = creature.GetGUID();
                        break;
                    case InstanceCreatureIds.Auriaya:
                        AuriayaGUID = creature.GetGUID();
                        break;

                    // Hodir
                    case InstanceCreatureIds.Hodir:
                        HodirGUID = creature.GetGUID();
                        break;
                    case InstanceCreatureIds.EiviNightfeather:
                        if (TeamInInstance == Team.Horde)
                            creature.UpdateEntry(InstanceCreatureIds.TorGreycloud);
                        break;
                    case InstanceCreatureIds.EllieNightfeather:
                        if (TeamInInstance == Team.Horde)
                            creature.UpdateEntry(InstanceCreatureIds.KarGreycloud);
                        break;
                    case InstanceCreatureIds.ElementalistMahfuun:
                        if (TeamInInstance == Team.Horde)
                            creature.UpdateEntry(InstanceCreatureIds.SpiritwalkerTara);
                        break;
                    case InstanceCreatureIds.ElementalistAvuun:
                        if (TeamInInstance == Team.Horde)
                            creature.UpdateEntry(InstanceCreatureIds.SpiritwalkerYona);
                        break;
                    case InstanceCreatureIds.MissyFlamecuffs:
                        if (TeamInInstance == Team.Horde)
                            creature.UpdateEntry(InstanceCreatureIds.AmiraBlazeweaver);
                        break;
                    case InstanceCreatureIds.SissyFlamecuffs:
                        if (TeamInInstance == Team.Horde)
                            creature.UpdateEntry(InstanceCreatureIds.VeeshaBlazeweaver);
                        break;
                    case InstanceCreatureIds.FieldMedicPenny:
                        if (TeamInInstance == Team.Horde)
                            creature.UpdateEntry(InstanceCreatureIds.BattlePriestEliza);
                        break;
                    case InstanceCreatureIds.FieldMedicJessi:
                        if (TeamInInstance == Team.Horde)
                            creature.UpdateEntry(InstanceCreatureIds.BattlePriestGina);
                        break;

                    case InstanceCreatureIds.Thorim:
                        ThorimGUID = creature.GetGUID();
                        break;

                    // Freya
                    case InstanceCreatureIds.Freya:
                        FreyaGUID = creature.GetGUID();
                        break;
                    case InstanceCreatureIds.Ironbranch:
                        ElderGUIDs[0] = creature.GetGUID();
                        if (GetBossState(BossIds.Freya) == EncounterState.Done)
                            creature.DespawnOrUnsummon();
                        break;
                    case InstanceCreatureIds.Brightleaf:
                        ElderGUIDs[1] = creature.GetGUID();
                        if (GetBossState(BossIds.Freya) == EncounterState.Done)
                            creature.DespawnOrUnsummon();
                        break;
                    case InstanceCreatureIds.Stonebark:
                        ElderGUIDs[2] = creature.GetGUID();
                        if (GetBossState(BossIds.Freya) == EncounterState.Done)
                            creature.DespawnOrUnsummon();
                        break;
                    case InstanceCreatureIds.FreyaAchieveTrigger:
                        FreyaAchieveTriggerGUID = creature.GetGUID();
                        break;

                    // Mimiron
                    case InstanceCreatureIds.Mimiron:
                        MimironGUID = creature.GetGUID();
                        break;
                    case InstanceCreatureIds.LeviathanMkII:
                        MimironVehicleGUIDs[0] = creature.GetGUID();
                        break;
                    case InstanceCreatureIds.Vx001:
                        MimironVehicleGUIDs[1] = creature.GetGUID();
                        break;
                    case InstanceCreatureIds.AerialCommandUnit:
                        MimironVehicleGUIDs[2] = creature.GetGUID();
                        break;
                    case InstanceCreatureIds.Computer:
                        MimironComputerGUID = creature.GetGUID();
                        break;
                    case InstanceCreatureIds.WorldTriggerMimiron:
                        MimironWorldTriggerGUID = creature.GetGUID();
                        break;

                    case InstanceCreatureIds.Vezax:
                        VezaxGUID = creature.GetGUID();
                        break;

                    // Yogg-Saron
                    case InstanceCreatureIds.YoggSaron:
                        YoggSaronGUID = creature.GetGUID();
                        break;
                    case InstanceCreatureIds.VoiceOfYoggSaron:
                        VoiceOfYoggSaronGUID = creature.GetGUID();
                        break;
                    case InstanceCreatureIds.BrainOfYoggSaron:
                        BrainOfYoggSaronGUID = creature.GetGUID();
                        break;
                    case InstanceCreatureIds.Sara:
                        SaraGUID = creature.GetGUID();
                        break;
                    case InstanceCreatureIds.FreyaYs:
                        KeeperGUIDs[0] = creature.GetGUID();
                        _summonYSKeeper[0] = false;
                        SaveToDB();
                        ++keepersCount;
                        break;
                    case InstanceCreatureIds.HodirYs:
                        KeeperGUIDs[1] = creature.GetGUID();
                        _summonYSKeeper[1] = false;
                        SaveToDB();
                        ++keepersCount;
                        break;
                    case InstanceCreatureIds.ThorimYs:
                        KeeperGUIDs[2] = creature.GetGUID();
                        _summonYSKeeper[2] = false;
                        SaveToDB();
                        ++keepersCount;
                        break;
                    case InstanceCreatureIds.MimironYs:
                        KeeperGUIDs[3] = creature.GetGUID();
                        _summonYSKeeper[3] = false;
                        SaveToDB();
                        ++keepersCount;
                        break;
                    case InstanceCreatureIds.SanityWell:
                        creature.SetReactState(ReactStates.Passive);
                        break;

                    // Algalon
                    case InstanceCreatureIds.Algalon:
                        AlgalonGUID = creature.GetGUID();
                        break;
                    case InstanceCreatureIds.BrannBronzbeardAlg:
                        BrannBronzebeardAlgGUID = creature.GetGUID();
                        break;
                    //! These creatures are summoned by something else than Algalon
                    //! but need to be controlled/despawned by him - so they need to be
                    //! registered in his summon list
                    case InstanceCreatureIds.AlgalonVoidZoneVisualStalker:
                    case InstanceCreatureIds.AlgalonStalkerAsteroidTarget01:
                    case InstanceCreatureIds.AlgalonStalkerAsteroidTarget02:
                    case InstanceCreatureIds.UnleashedDarkMatter:
                        Creature algalon = instance.GetCreature(AlgalonGUID);
                        if (algalon)
                            algalon.GetAI().JustSummoned(creature);
                        break;
                }
            }

            public override void OnCreatureRemove(Creature creature)
            {
                switch (creature.GetEntry())
                {
                    case InstanceCreatureIds.XtToyPile:
                        for (byte i = 0; i < 4; ++i)
                            if (XTToyPileGUIDs[i] == creature.GetGUID())
                            {
                                XTToyPileGUIDs[i].Clear();
                                break;
                            }
                        break;
                    case InstanceCreatureIds.Steelbreaker:
                    case InstanceCreatureIds.Molgeim:
                    case InstanceCreatureIds.Brundir:
                        AddMinion(creature, false);
                        break;
                    case InstanceCreatureIds.BrannBronzbeardAlg:
                        if (BrannBronzebeardAlgGUID == creature.GetGUID())
                            BrannBronzebeardAlgGUID.Clear();
                        break;
                    default:
                        break;
                }
            }

            public override void OnGameObjectCreate(GameObject gameObject)
            {
                switch (gameObject.GetEntry())
                {
                    case InstanceGameObjectIds.KologarnChestHero:
                    case InstanceGameObjectIds.KologarnChest:
                        KologarnChestGUID = gameObject.GetGUID();
                        break;
                    case InstanceGameObjectIds.KologarnBridge:
                        KologarnBridgeGUID = gameObject.GetGUID();
                        if (GetBossState(BossIds.Kologarn) == EncounterState.Done)
                            HandleGameObject(ObjectGuid.Empty, false, gameObject);
                        break;
                    case InstanceGameObjectIds.ThorimChestHero:
                    case InstanceGameObjectIds.ThorimChest:
                        ThorimChestGUID = gameObject.GetGUID();
                        break;
                    case InstanceGameObjectIds.HodirRareCacheOfWinterHero:
                    case InstanceGameObjectIds.HodirRareCacheOfWinter:
                        HodirRareCacheGUID = gameObject.GetGUID();
                        break;
                    case InstanceGameObjectIds.HodirChestHero:
                    case InstanceGameObjectIds.HodirChest:
                        HodirChestGUID = gameObject.GetGUID();
                        break;
                    case InstanceGameObjectIds.MimironTram:
                        MimironTramGUID = gameObject.GetGUID();
                        break;
                    case InstanceGameObjectIds.MimironElevator:
                        MimironElevatorGUID = gameObject.GetGUID();
                        break;
                    case InstanceGameObjectIds.MimironButton:
                        MimironButtonGUID = gameObject.GetGUID();
                        break;
                    case InstanceGameObjectIds.LeviathanGate:
                        LeviathanGateGUID = gameObject.GetGUID();
                        if (GetBossState(BossIds.Leviathan) == EncounterState.Done)
                            gameObject.SetGoState(GameObjectState.ActiveAlternative);
                        break;
                    case InstanceGameObjectIds.LeviathanDoor:
                    case InstanceGameObjectIds.Xt002Door:
                    case InstanceGameObjectIds.IronCouncilDoor:
                    case InstanceGameObjectIds.ArchivumDoor:
                    case InstanceGameObjectIds.HodirEntrance:
                    case InstanceGameObjectIds.HodirDoor:
                    case InstanceGameObjectIds.HodirIceDoor:
                    case InstanceGameObjectIds.MimironDoor1:
                    case InstanceGameObjectIds.MimironDoor2:
                    case InstanceGameObjectIds.MimironDoor3:
                    case InstanceGameObjectIds.VezaxDoor:
                    case InstanceGameObjectIds.YoggSaronDoor:
                        AddDoor(gameObject, true);
                        break;
                    case InstanceGameObjectIds.RazorHarpoon1:
                        RazorHarpoonGUIDs[0] = gameObject.GetGUID();
                        break;
                    case InstanceGameObjectIds.RazorHarpoon2:
                        RazorHarpoonGUIDs[1] = gameObject.GetGUID();
                        break;
                    case InstanceGameObjectIds.RazorHarpoon3:
                        RazorHarpoonGUIDs[2] = gameObject.GetGUID();
                        break;
                    case InstanceGameObjectIds.RazorHarpoon4:
                        RazorHarpoonGUIDs[3] = gameObject.GetGUID();
                        break;
                    case InstanceGameObjectIds.MoleMachine:
                        if (GetBossState(BossIds.Razorscale) == EncounterState.InProgress)
                            gameObject.SetGoState(GameObjectState.Active);
                        break;
                    case InstanceGameObjectIds.BrainRoomDoor1:
                        BrainRoomDoorGUIDs[0] = gameObject.GetGUID();
                        break;
                    case InstanceGameObjectIds.BrainRoomDoor2:
                        BrainRoomDoorGUIDs[1] = gameObject.GetGUID();
                        break;
                    case InstanceGameObjectIds.BrainRoomDoor3:
                        BrainRoomDoorGUIDs[2] = gameObject.GetGUID();
                        break;
                    case InstanceGameObjectIds.CelestialPlanetariumAccess10:
                    case InstanceGameObjectIds.CelestialPlanetariumAccess25:
                        if (_algalonSummoned)
                            gameObject.SetFlag(GameObjectFields.Flags, GameObjectFlags.InUse);
                        break;
                    case InstanceGameObjectIds.DoodadUlSigildoor01:
                        AlgalonSigilDoorGUID[0] = gameObject.GetGUID();
                        if (_algalonSummoned)
                            gameObject.SetGoState(GameObjectState.Active);
                        break;
                    case InstanceGameObjectIds.DoodadUlSigildoor02:
                        AlgalonSigilDoorGUID[1] = gameObject.GetGUID();
                        if (_algalonSummoned)
                            gameObject.SetGoState(GameObjectState.Active);
                        break;
                    case InstanceGameObjectIds.DoodadUlSigildoor03:
                        AlgalonSigilDoorGUID[2] = gameObject.GetGUID();
                        AddDoor(gameObject, true);
                        break;
                    case InstanceGameObjectIds.DoodadUlUniversefloor01:
                        AlgalonFloorGUID[0] = gameObject.GetGUID();
                        AddDoor(gameObject, true);
                        break;
                    case InstanceGameObjectIds.DoodadUlUniversefloor02:
                        AlgalonFloorGUID[1] = gameObject.GetGUID();
                        AddDoor(gameObject, true);
                        break;
                    case InstanceGameObjectIds.DoodadUlUniverseglobe01:
                        AlgalonUniverseGUID = gameObject.GetGUID();
                        AddDoor(gameObject, true);
                        break;
                    case InstanceGameObjectIds.DoodadUlUlduarTrapdoor03:
                        AlgalonTrapdoorGUID = gameObject.GetGUID();
                        AddDoor(gameObject, true);
                        break;
                    case InstanceGameObjectIds.GiftOfTheObserver10:
                    case InstanceGameObjectIds.GiftOfTheObserver25:
                        GiftOfTheObserverGUID = gameObject.GetGUID();
                        break;
                    default:
                        break;
                }
            }

            public override void OnGameObjectRemove(GameObject gameObject)
            {
                switch (gameObject.GetEntry())
                {
                    case InstanceGameObjectIds.LeviathanDoor:
                    case InstanceGameObjectIds.Xt002Door:
                    case InstanceGameObjectIds.IronCouncilDoor:
                    case InstanceGameObjectIds.ArchivumDoor:
                    case InstanceGameObjectIds.HodirEntrance:
                    case InstanceGameObjectIds.HodirDoor:
                    case InstanceGameObjectIds.HodirIceDoor:
                    case InstanceGameObjectIds.MimironDoor1:
                    case InstanceGameObjectIds.MimironDoor2:
                    case InstanceGameObjectIds.MimironDoor3:
                    case InstanceGameObjectIds.VezaxDoor:
                    case InstanceGameObjectIds.YoggSaronDoor:
                    case InstanceGameObjectIds.DoodadUlSigildoor01:
                    case InstanceGameObjectIds.DoodadUlUniversefloor01:
                    case InstanceGameObjectIds.DoodadUlUniversefloor02:
                    case InstanceGameObjectIds.DoodadUlUniverseglobe01:
                    case InstanceGameObjectIds.DoodadUlUlduarTrapdoor03:
                        AddDoor(gameObject, false);
                        break;
                    default:
                        break;
                }
            }

            public override void OnUnitDeath(Unit unit)
            {
                // Champion/Conqueror of Ulduar
                if (unit.IsTypeId(TypeId.Player))
                {
                    for (byte i = 0; i < BossIds.Algalon; i++)
                    {
                        if (GetBossState(i) == EncounterState.InProgress)
                        {
                            _CoUAchivePlayerDeathMask |= (1u << i);
                            SaveToDB();
                        }
                    }
                }

                Creature creature = unit.ToCreature();
                if (!creature)
                    return;

                switch (creature.GetEntry())
                {
                    case InstanceCreatureIds.CorruptedServitor:
                    case InstanceCreatureIds.MisguidedNymph:
                    case InstanceCreatureIds.GuardianLasher:
                    case InstanceCreatureIds.ForestSwarmer:
                    case InstanceCreatureIds.MangroveEnt:
                    case InstanceCreatureIds.IronrootLasher:
                    case InstanceCreatureIds.NaturesBlade:
                    case InstanceCreatureIds.GuardianOfLife:
                        if (!conSpeedAtory)
                        {
                            DoStartCriteriaTimer(CriteriaTimedTypes.Event, InstanceCriteriaIds.ConSpeedAtory);
                            conSpeedAtory = true;
                        }
                        break;
                    case InstanceCreatureIds.Ironbranch:
                    case InstanceCreatureIds.Stonebark:
                    case InstanceCreatureIds.Brightleaf:
                        if (!lumberjacked)
                        {
                            DoStartCriteriaTimer(CriteriaTimedTypes.Event, InstanceCriteriaIds.Lumberjacked);
                            lumberjacked = true;
                        }
                        break;
                    default:
                        break;
                }
            }

            public override void ProcessEvent(WorldObject gameObject, uint eventId)
            {
                // Flame Leviathan's Tower Event triggers
                Creature FlameLeviathan = instance.GetCreature(LeviathanGUID);

                switch (eventId)
                {
                    case LeviathanActions.TowerOfStormDestroyed:
                        if (FlameLeviathan && FlameLeviathan.IsAlive())
                            FlameLeviathan.GetAI().DoAction(LeviathanActions.TowerOfStormDestroyed);
                        break;
                    case LeviathanActions.TowerOfFrostDestroyed:
                        if (FlameLeviathan && FlameLeviathan.IsAlive())
                            FlameLeviathan.GetAI().DoAction(LeviathanActions.TowerOfFrostDestroyed);
                        break;
                    case LeviathanActions.TowerOfFlamesDestroyed:
                        if (FlameLeviathan && FlameLeviathan.IsAlive())
                            FlameLeviathan.GetAI().DoAction(LeviathanActions.TowerOfFlamesDestroyed);
                        break;
                    case LeviathanActions.TowerOfLifeDestroyed:
                        if (FlameLeviathan && FlameLeviathan.IsAlive())
                            FlameLeviathan.GetAI().DoAction(LeviathanActions.TowerOfLifeDestroyed);
                        break;
                    case InstanceEventIds.ActivateSanityWell:
                        Creature freya = instance.GetCreature(KeeperGUIDs[0]);
                        if (freya)
                            freya.GetAI().DoAction(4/*ACTION_SANITY_WELLS*/);
                        break;
                    case InstanceEventIds.HodirsProtectiveGazeProc:
                        Creature hodir = instance.GetCreature(KeeperGUIDs[1]);
                        if (hodir)
                            hodir.GetAI().DoAction(5/*ACTION_FLASH_FREEZE*/);
                        break;
                }
            }

            public override bool SetBossState(uint type, EncounterState state)
            {
                if (!base.SetBossState(type, state))
                    return false;

                switch (type)
                {
                    case BossIds.Leviathan:
                        if (state == EncounterState.Done)
                        {
                            // Eject all players from vehicles and make them untargetable.
                            // They will be despawned after a while
                            foreach (var vehicleGuid in LeviathanVehicleGUIDs)
                            {
                                Creature vehicleCreature = instance.GetCreature(vehicleGuid);
                                if (vehicleCreature != null)
                                {
                                    Vehicle vehicle = vehicleCreature.GetVehicleKit();
                                    if (vehicle != null)
                                    {
                                        vehicle.RemoveAllPassengers();
                                        vehicleCreature.SetFlag(UnitFields.Flags, UnitFlags.NotSelectable);
                                        vehicleCreature.DespawnOrUnsummon(5 * Time.Minute * Time.InMilliseconds);
                                    }
                                }
                            }
                        }
                        break;
                    case BossIds.Ignis:
                    case BossIds.Razorscale:
                    case BossIds.Xt002:
                    case BossIds.AssemblyOfIron:
                    case BossIds.Auriaya:
                    case BossIds.Vezax:
                    case BossIds.YoggSaron:
                        break;
                    case BossIds.Mimiron:
                        if (state == EncounterState.Done)
                            instance.SummonCreature(InstanceCreatureIds.MimironObservationRing, YoggSaron.ObservationRingKeepersPos[3]);
                        break;
                    case BossIds.Freya:
                        if (state == EncounterState.Done)
                            instance.SummonCreature(InstanceCreatureIds.FreyaObservationRing, YoggSaron.ObservationRingKeepersPos[0]);
                        break;
                    case BossIds.Kologarn:
                        if (state == EncounterState.Done)
                        {
                            GameObject gameObject = instance.GetGameObject(KologarnChestGUID);
                            if (gameObject)
                            {
                                gameObject.SetRespawnTime((int)gameObject.GetRespawnDelay());
                                gameObject.RemoveFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                            }
                            HandleGameObject(KologarnBridgeGUID, false);
                        }
                        break;
                    case BossIds.Hodir:
                        if (state == EncounterState.Done)
                        {
                            GameObject HodirRareCache = instance.GetGameObject(HodirRareCacheGUID);
                            if (HodirRareCache)
                                if (GetData(InstanceData.HodirRareCache) != 0)
                                    HodirRareCache.RemoveFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                            GameObject HodirChest = instance.GetGameObject(HodirChestGUID);
                            if (HodirChest)
                                HodirChest.SetRespawnTime((int)HodirChest.GetRespawnDelay());

                            instance.SummonCreature(InstanceCreatureIds.HodirObservationRing, YoggSaron.ObservationRingKeepersPos[1]);
                        }
                        break;
                    case BossIds.Thorim:
                        if (state == EncounterState.Done)
                        {
                            GameObject gameObject = instance.GetGameObject(ThorimChestGUID);
                            if (gameObject)
                                gameObject.SetRespawnTime((int)gameObject.GetRespawnDelay());

                            instance.SummonCreature(InstanceCreatureIds.ThorimObservationRing, YoggSaron.ObservationRingKeepersPos[2]);
                        }
                        break;
                    case BossIds.Algalon:
                        if (state == EncounterState.Done)
                        {
                            _events.CancelEvent(InstanceEvents.EventUpdateAlgalonTimer);
                            _events.CancelEvent(InstanceEvents.EventDespawnAlgalon);
                            DoUpdateWorldState(InstanceWorldStates.AlgalonTimerEnabled, 0);
                            _algalonTimer = 61;
                            GameObject gameObject = instance.GetGameObject(GiftOfTheObserverGUID);
                            if (gameObject)
                                gameObject.SetRespawnTime((int)gameObject.GetRespawnDelay());
                            // get item level (recheck weapons)
                            var players = instance.GetPlayers();
                            foreach (var player in players)
                            {
                                for (byte slot = EquipmentSlot.MainHand; slot <= EquipmentSlot.OffHand; ++slot)
                                {
                                    Item item = player.GetItemByPos(InventorySlots.Bag0, slot);
                                    if (item)
                                        if (item.GetTemplate().GetBaseItemLevel() > _maxWeaponItemLevel)
                                            _maxWeaponItemLevel = item.GetTemplate().GetBaseItemLevel();
                                }
                            }
                        }
                        else if (state == EncounterState.InProgress)
                        {
                            // get item level (armor cannot be swapped in combat)
                            var players = instance.GetPlayers();
                            foreach (var player in players)
                            {
                                for (byte slot = EquipmentSlot.Start; slot < EquipmentSlot.End; ++slot)
                                {
                                    if (slot == EquipmentSlot.Tabard || slot == EquipmentSlot.Cloak)
                                        continue;

                                    Item item = player.GetItemByPos(InventorySlots.Bag0, slot);
                                    if (item)
                                    {
                                        if (slot >= EquipmentSlot.MainHand && slot <= EquipmentSlot.OffHand)
                                        {
                                            if (item.GetTemplate().GetBaseItemLevel() > _maxWeaponItemLevel)
                                                _maxWeaponItemLevel = item.GetTemplate().GetBaseItemLevel();
                                        }
                                        else if (item.GetTemplate().GetBaseItemLevel() > _maxArmorItemLevel)
                                            _maxArmorItemLevel = item.GetTemplate().GetBaseItemLevel();
                                    }
                                }

                            }
                        }
                        break;
                }

                return true;
            }

            public override void SetData(uint type, uint data)
            {
                switch (type)
                {
                    case InstanceData.Colossus:
                        ColossusData = data;
                        if (data == 2)
                        {
                            Creature Leviathan = instance.GetCreature(LeviathanGUID);
                            if (Leviathan)
                                Leviathan.GetAI().DoAction(LeviathanActions.MoveToCenterPosition);
                            GameObject gameObject = instance.GetGameObject(LeviathanGateGUID);
                            if (gameObject)
                                gameObject.SetGoState(GameObjectState.ActiveAlternative);
                            SaveToDB();
                        }
                        break;
                    case InstanceData.HodirRareCache:
                        HodirRareCacheData = data;
                        if (HodirRareCacheData == 0)
                        {
                            Creature Hodir = instance.GetCreature(HodirGUID);
                            if (Hodir)
                            {
                                GameObject gameObject = instance.GetGameObject(HodirRareCacheGUID);
                                if (gameObject)
                                    Hodir.RemoveGameObject(gameObject, false);
                            }
                        }
                        break;
                    case InstanceAchievementData.DataUnbroken:
                        Unbroken = data != 0;
                        break;
                    case InstanceData.Illusion:
                        illusion = (byte)data;
                        break;
                    case InstanceData.DriveMeCrazy:
                        IsDriveMeCrazyEligible = data != 0 ? true : false;
                        break;
                    case InstanceEvents.EventDespawnAlgalon:
                        DoUpdateWorldState(InstanceWorldStates.AlgalonTimerEnabled, 1);
                        DoUpdateWorldState(InstanceWorldStates.AlgalonDespawnTimer, 60);
                        _algalonTimer = 60;
                        _events.ScheduleEvent(InstanceEvents.EventDespawnAlgalon, 3600000);
                        _events.ScheduleEvent(InstanceEvents.EventUpdateAlgalonTimer, 60000);
                        break;
                    case InstanceData.AlgalonSummonState:
                        _algalonSummoned = true;
                        break;
                    default:
                        break;
                }
            }

            public override void SetGuidData(uint type, ObjectGuid data) { }

            public override ObjectGuid GetGuidData(uint data)
            {
                switch (data)
                {
                    case BossIds.Leviathan:
                        return LeviathanGUID;
                    case BossIds.Ignis:
                        return IgnisGUID;

                    // Razorscale
                    case BossIds.Razorscale:
                        return RazorscaleGUID;
                    case InstanceData.RazorscaleControl:
                        return RazorscaleController;
                    case InstanceData.ExpeditionCommander:
                        return ExpeditionCommanderGUID;
                    case InstanceGameObjectIds.RazorHarpoon1:
                        return RazorHarpoonGUIDs[0];
                    case InstanceGameObjectIds.RazorHarpoon2:
                        return RazorHarpoonGUIDs[1];
                    case InstanceGameObjectIds.RazorHarpoon3:
                        return RazorHarpoonGUIDs[2];
                    case InstanceGameObjectIds.RazorHarpoon4:
                        return RazorHarpoonGUIDs[3];

                    // XT-002 Deconstructor
                    case BossIds.Xt002:
                        return XT002GUID;
                    case InstanceData.ToyPile0:
                    case InstanceData.ToyPile1:
                    case InstanceData.ToyPile2:
                    case InstanceData.ToyPile3:
                        return XTToyPileGUIDs[data - InstanceData.ToyPile0];

                    // Assembly of Iron
                    case InstanceData.Steelbreaker:
                        return AssemblyGUIDs[0];
                    case InstanceData.Molgeim:
                        return AssemblyGUIDs[1];
                    case InstanceData.Brundir:
                        return AssemblyGUIDs[2];

                    case BossIds.Kologarn:
                        return KologarnGUID;
                    case BossIds.Auriaya:
                        return AuriayaGUID;
                    case BossIds.Hodir:
                        return HodirGUID;
                    case BossIds.Thorim:
                        return ThorimGUID;

                    // Freya
                    case BossIds.Freya:
                        return FreyaGUID;
                    case BossIds.Brightleaf:
                        return ElderGUIDs[0];
                    case BossIds.Ironbranch:
                        return ElderGUIDs[1];
                    case BossIds.Stonebark:
                        return ElderGUIDs[2];

                    // Mimiron
                    case BossIds.Mimiron:
                        return MimironGUID;
                    case InstanceData.LeviathanMKII:
                        return MimironVehicleGUIDs[0];
                    case InstanceData.VX001:
                        return MimironVehicleGUIDs[1];
                    case InstanceData.AerialCommandUnit:
                        return MimironVehicleGUIDs[2];
                    case InstanceData.Computer:
                        return MimironComputerGUID;
                    case InstanceData.MimironWorldTrigger:
                        return MimironWorldTriggerGUID;
                    case InstanceData.MimironElevator:
                        return MimironElevatorGUID;
                    case InstanceData.MimironButton:
                        return MimironButtonGUID;

                    case BossIds.Vezax:
                        return VezaxGUID;

                    // Yogg-Saron
                    case BossIds.YoggSaron:
                        return YoggSaronGUID;
                    case InstanceData.VoiceOfYoggSaron:
                        return VoiceOfYoggSaronGUID;
                    case InstanceData.BrainOfYoggSaron:
                        return BrainOfYoggSaronGUID;
                    case InstanceData.Sara:
                        return SaraGUID;
                    case InstanceGameObjectIds.BrainRoomDoor1:
                        return BrainRoomDoorGUIDs[0];
                    case InstanceGameObjectIds.BrainRoomDoor2:
                        return BrainRoomDoorGUIDs[1];
                    case InstanceGameObjectIds.BrainRoomDoor3:
                        return BrainRoomDoorGUIDs[2];
                    case InstanceData.FreyaYs:
                        return KeeperGUIDs[0];
                    case InstanceData.HodirYs:
                        return KeeperGUIDs[1];
                    case InstanceData.ThorimYs:
                        return KeeperGUIDs[2];
                    case InstanceData.MimironYs:
                        return KeeperGUIDs[3];

                    // Algalon
                    case BossIds.Algalon:
                        return AlgalonGUID;
                    case InstanceData.Sigildoor01:
                        return AlgalonSigilDoorGUID[0];
                    case InstanceData.Sigildoor02:
                        return AlgalonSigilDoorGUID[1];
                    case InstanceData.Sigildoor03:
                        return AlgalonSigilDoorGUID[2];
                    case InstanceData.UniverseFloor01:
                        return AlgalonFloorGUID[0];
                    case InstanceData.UniverseFloor02:
                        return AlgalonFloorGUID[1];
                    case InstanceData.UniverseGlobe:
                        return AlgalonUniverseGUID;
                    case InstanceData.AlgalonTrapdoor:
                        return AlgalonTrapdoorGUID;
                    case InstanceData.BrannBronzebeardAlg:
                        return BrannBronzebeardAlgGUID;
                }

                return ObjectGuid.Empty;
            }

            public override uint GetData(uint type)
            {
                switch (type)
                {
                    case InstanceData.Colossus:
                        return ColossusData;
                    case InstanceData.HodirRareCache:
                        return HodirRareCacheData;
                    case InstanceAchievementData.DataUnbroken:
                        return (uint)(Unbroken ? 1 : 0);
                    case InstanceData.Illusion:
                        return illusion;
                    case InstanceData.KeepersCount:
                        return keepersCount;
                    default:
                        break;
                }

                return 0;
            }

            public override bool CheckAchievementCriteriaMeet(uint criteriaId, Player source, Unit target = null, uint miscvalue1 = 0)
            {
                switch (criteriaId)
                {
                    case InstanceCriteriaIds.HeraldOfTitans:
                        return _maxArmorItemLevel <= InstanceAchievementData.MaxHeraldArmorItemlevel && _maxWeaponItemLevel <= InstanceAchievementData.MaxHeraldWeaponItemlevel;
                    case InstanceCriteriaIds.WaitsDreamingStormwind10:
                    case InstanceCriteriaIds.WaitsDreamingStormwind25:
                        return illusion == YoggSaronIllusions.StormwindIllusion;
                    case InstanceCriteriaIds.WaitsDreamingChamber10:
                    case InstanceCriteriaIds.WaitsDreamingChamber25:
                        return illusion == YoggSaronIllusions.ChamberIllusion;
                    case InstanceCriteriaIds.WaitsDreamingIcecrown10:
                    case InstanceCriteriaIds.WaitsDreamingIcecrown25:
                        return illusion == YoggSaronIllusions.IcecrownIllusion;
                    case InstanceCriteriaIds.DriveMeCrazy10:
                    case InstanceCriteriaIds.DriveMeCrazy25:
                        return IsDriveMeCrazyEligible;
                    case InstanceCriteriaIds.ThreeLightsInTheDarkness10:
                    case InstanceCriteriaIds.ThreeLightsInTheDarkness25:
                        return keepersCount <= 3;
                    case InstanceCriteriaIds.TwoLightsInTheDarkness10:
                    case InstanceCriteriaIds.TwoLightsInTheDarkness25:
                        return keepersCount <= 2;
                    case InstanceCriteriaIds.OneLightInTheDarkness10:
                    case InstanceCriteriaIds.OneLightInTheDarkness25:
                        return keepersCount <= 1;
                    case InstanceCriteriaIds.AloneInTheDarkness10:
                    case InstanceCriteriaIds.AloneInTheDarkness25:
                        return keepersCount == 0;
                    case InstanceCriteriaIds.ChampionLeviathan10:
                    case InstanceCriteriaIds.ChampionLeviathan25:
                        return (_CoUAchivePlayerDeathMask & (1 << (int)BossIds.Leviathan)) == 0;
                    case InstanceCriteriaIds.ChampionIgnis10:
                    case InstanceCriteriaIds.ChampionIgnis25:
                        return (_CoUAchivePlayerDeathMask & (1 << (int)BossIds.Ignis)) == 0;
                    case InstanceCriteriaIds.ChampionRazorscale10:
                    case InstanceCriteriaIds.ChampionRazorscale25:
                        return (_CoUAchivePlayerDeathMask & (1 << (int)BossIds.Razorscale)) == 0;
                    case InstanceCriteriaIds.ChampionXt002_10:
                    case InstanceCriteriaIds.ChampionXt002_25:
                        return (_CoUAchivePlayerDeathMask & (1 << (int)BossIds.Xt002)) == 0;
                    case InstanceCriteriaIds.ChampionIronCouncil10:
                    case InstanceCriteriaIds.ChampionIronCouncil25:
                        return (_CoUAchivePlayerDeathMask & (1 << (int)BossIds.AssemblyOfIron)) == 0;
                    case InstanceCriteriaIds.ChampionKologarn10:
                    case InstanceCriteriaIds.ChampionKologarn25:
                        return (_CoUAchivePlayerDeathMask & (1 << (int)BossIds.Kologarn)) == 0;
                    case InstanceCriteriaIds.ChampionAuriaya10:
                    case InstanceCriteriaIds.ChampionAuriaya25:
                        return (_CoUAchivePlayerDeathMask & (1 << (int)BossIds.Auriaya)) == 0;
                    case InstanceCriteriaIds.ChampionHodir10:
                    case InstanceCriteriaIds.ChampionHodir25:
                        return (_CoUAchivePlayerDeathMask & (1 << (int)BossIds.Hodir)) == 0;
                    case InstanceCriteriaIds.ChampionThorim10:
                    case InstanceCriteriaIds.ChampionThorim25:
                        return (_CoUAchivePlayerDeathMask & (1 << (int)BossIds.Thorim)) == 0;
                    case InstanceCriteriaIds.ChampionFreya10:
                    case InstanceCriteriaIds.ChampionFreya25:
                        return (_CoUAchivePlayerDeathMask & (1 << (int)BossIds.Freya)) == 0;
                    case InstanceCriteriaIds.ChampionMimiron10:
                    case InstanceCriteriaIds.ChampionMimiron25:
                        return (_CoUAchivePlayerDeathMask & (1 << (int)BossIds.Mimiron)) == 0;
                    case InstanceCriteriaIds.ChampionVezax10:
                    case InstanceCriteriaIds.ChampionVezax25:
                        return (_CoUAchivePlayerDeathMask & (1 << (int)BossIds.Vezax)) == 0;
                    case InstanceCriteriaIds.ChampionYoggSaron10:
                    case InstanceCriteriaIds.ChampionYoggSaron25:
                        return (_CoUAchivePlayerDeathMask & (1 << (int)BossIds.YoggSaron)) == 0;
                }

                return false;
            }

            public override void WriteSaveDataMore(StringBuilder data)
            {
                data.AppendFormat("{0} {1} {2}", GetData(InstanceData.Colossus), _algalonTimer, (_algalonSummoned ? 1 : 0));

                for (byte i = 0; i < 4; ++i)
                    data.AppendFormat(" {0}", (KeeperGUIDs[i].IsEmpty() ? 0 : 1));

                data.AppendFormat(" {0}", _CoUAchivePlayerDeathMask);
            }

            public override void ReadSaveDataMore(StringArguments data)
            {
                EncounterState tempState = (EncounterState)data.NextUInt32();
                if (tempState == EncounterState.InProgress || tempState > EncounterState.Special)
                    tempState = EncounterState.NotStarted;
                SetData(InstanceData.Colossus, (uint)tempState);

                _algalonTimer = data.NextUInt32();
                tempState = (EncounterState)data.NextUInt32();
                _algalonSummoned = tempState != 0;
                if (_algalonSummoned && GetBossState(BossIds.Algalon) != EncounterState.Done)
                {
                    _summonAlgalon = true;
                    if (_algalonTimer != 0 && _algalonTimer <= 60)
                    {
                        _events.ScheduleEvent(InstanceEvents.EventUpdateAlgalonTimer, 60000);
                        DoUpdateWorldState(InstanceWorldStates.AlgalonTimerEnabled, 1);
                        DoUpdateWorldState(InstanceWorldStates.AlgalonDespawnTimer, _algalonTimer);
                    }
                }

                for (byte i = 0; i < 4; ++i)
                {
                    tempState = (EncounterState)data.NextUInt32();
                    _summonYSKeeper[i] = tempState != 0;
                }

                if (GetBossState(BossIds.Freya) == EncounterState.Done && !_summonYSKeeper[0])
                    _summonObservationRingKeeper[0] = true;
                if (GetBossState(BossIds.Hodir) == EncounterState.Done && !_summonYSKeeper[1])
                    _summonObservationRingKeeper[1] = true;
                if (GetBossState(BossIds.Thorim) == EncounterState.Done && !_summonYSKeeper[2])
                    _summonObservationRingKeeper[2] = true;
                if (GetBossState(BossIds.Mimiron) == EncounterState.Done && !_summonYSKeeper[3])
                    _summonObservationRingKeeper[3] = true;

                _CoUAchivePlayerDeathMask = data.NextUInt32();
            }

            public override void Update(uint diff)
            {
                if (_events.Empty())
                    return;

                _events.Update(diff);

                _events.ExecuteEvents(eventId =>
                {
                    switch (eventId)
                    {
                        case InstanceEvents.EventUpdateAlgalonTimer:
                            SaveToDB();
                            DoUpdateWorldState(InstanceWorldStates.AlgalonDespawnTimer, --_algalonTimer);
                            if (_algalonTimer != 0)
                                _events.ScheduleEvent(InstanceEvents.EventUpdateAlgalonTimer, 60000);
                            else
                            {
                                DoUpdateWorldState(InstanceWorldStates.AlgalonTimerEnabled, 0);
                                _events.CancelEvent(InstanceEvents.EventUpdateAlgalonTimer);
                                Creature algalon = instance.GetCreature(AlgalonGUID);
                                if (algalon)
                                    algalon.GetAI().DoAction((int)InstanceEvents.EventDespawnAlgalon);
                            }
                            break;
                    }
                });
            }

            public override void UpdateDoorState(GameObject door)
            {
                // Leviathan doors are set to DOOR_TYPE_ROOM except the one it uses to enter the room
                // which has to be set to DOOR_TYPE_PASSAGE
                if (door.GetEntry() == InstanceGameObjectIds.LeviathanDoor && door.GetPositionX() > 400.0f)
                    door.SetGoState(GetBossState(BossIds.Leviathan) == EncounterState.Done ? GameObjectState.Active : GameObjectState.Ready);
                else
                    base.UpdateDoorState(door);
            }

            public override void AddDoor(GameObject door, bool add)
            {
                // Leviathan doors are South except the one it uses to enter the room
                // which is North and should not be used for boundary checks in BossAI.CheckBoundary()
                if (door.GetEntry() == InstanceGameObjectIds.LeviathanDoor && door.GetPositionX() > 400.0f)
                {
                    if (add)
                        GetBossInfo(BossIds.Leviathan).door[(int)DoorType.Passage].Add(door.GetGUID());
                    else
                        GetBossInfo(BossIds.Leviathan).door[(int)DoorType.Passage].Remove(door.GetGUID());

                    if (add)
                        UpdateDoorState(door);
                }
                else
                    base.AddDoor(door, add);
            }

            BossBoundaryEntry[] boundaries =
            {
                new BossBoundaryEntry(BossIds.Leviathan, new RectangleBoundary(148.0f, 401.3f, -155.0f, 90.0f) ),
                new BossBoundaryEntry(BossIds.Ignis, new RectangleBoundary(495.0f, 680.0f, 90.0f, 400.0f) ),
                new BossBoundaryEntry(BossIds.Razorscale, new RectangleBoundary(370.0f, 810.0f, -542.0f, -55.0f)),
                new BossBoundaryEntry(BossIds.Xt002, new RectangleBoundary(755.0f, 940.0f, -125.0f, 95.0f)),
                new BossBoundaryEntry(BossIds.AssemblyOfIron, new CircleBoundary(new Position(1587.2f, 121.0f), 90.0)),
                new BossBoundaryEntry(BossIds.Algalon, new CircleBoundary(new Position(1632.668f, -307.7656f), 45.0)),
                new BossBoundaryEntry(BossIds.Algalon, new ZRangeBoundary(410.0f, 440.0f)),
                new BossBoundaryEntry(BossIds.Hodir, new EllipseBoundary(new Position(2001.5f, -240.0f), 50.0, 75.0)),
                new BossBoundaryEntry(BossIds.Thorim, new CircleBoundary(new Position(2134.73f, -263.2f), 50.0)),
                new BossBoundaryEntry(BossIds.Freya, new RectangleBoundary(2094.6f, 2520.0f, -250.0f, 200.0f)),
                new BossBoundaryEntry(BossIds.Mimiron, new CircleBoundary(new Position(2744.0f, 2569.0f), 70.0)),
                new BossBoundaryEntry(BossIds.Vezax, new RectangleBoundary(1740.0f, 1930.0f, 31.0f, 228.0f)),
                new BossBoundaryEntry(BossIds.YoggSaron, new CircleBoundary(new Position(1980.42f, -27.68f), 105.0))
            };

            DoorData[] doorData =
            {
                new DoorData(InstanceGameObjectIds.LeviathanDoor,             BossIds.Leviathan,       DoorType.Room),
                new DoorData(InstanceGameObjectIds.Xt002Door,                 BossIds.Xt002,           DoorType.Room),
                new DoorData(InstanceGameObjectIds.IronCouncilDoor,           BossIds.AssemblyOfIron,  DoorType.Room),
                new DoorData(InstanceGameObjectIds.ArchivumDoor,              BossIds.AssemblyOfIron,  DoorType.Passage),
                new DoorData(InstanceGameObjectIds.HodirEntrance,             BossIds.Hodir,           DoorType.Room),
                new DoorData(InstanceGameObjectIds.HodirDoor,                 BossIds.Hodir,           DoorType.Passage),
                new DoorData(InstanceGameObjectIds.HodirIceDoor,              BossIds.Hodir,           DoorType.Passage),
                new DoorData(InstanceGameObjectIds.MimironDoor1,              BossIds.Mimiron,         DoorType.Room),
                new DoorData(InstanceGameObjectIds.MimironDoor2,              BossIds.Mimiron,         DoorType.Room),
                new DoorData(InstanceGameObjectIds.MimironDoor3,              BossIds.Mimiron,         DoorType.Room),
                new DoorData(InstanceGameObjectIds.VezaxDoor,                 BossIds.Vezax,           DoorType.Passage),
                new DoorData(InstanceGameObjectIds.YoggSaronDoor,             BossIds.YoggSaron,       DoorType.Room),
                new DoorData(InstanceGameObjectIds.DoodadUlSigildoor03,       BossIds.Algalon,         DoorType.Room),
                new DoorData(InstanceGameObjectIds.DoodadUlUniversefloor01,   BossIds.Algalon,         DoorType.Room),
                new DoorData(InstanceGameObjectIds.DoodadUlUniversefloor02,   BossIds.Algalon,         DoorType.SpawnHole),
                new DoorData(InstanceGameObjectIds.DoodadUlUniverseglobe01,   BossIds.Algalon,         DoorType.SpawnHole),
                new DoorData(InstanceGameObjectIds.DoodadUlUlduarTrapdoor03,  BossIds.Algalon,         DoorType.SpawnHole),
                new DoorData(0,                                           0,                            DoorType.Room)
            };

            MinionData[] minionData =
            {
                new MinionData(InstanceCreatureIds.Steelbreaker,  BossIds.AssemblyOfIron),
                new MinionData(InstanceCreatureIds.Molgeim,       BossIds.AssemblyOfIron),
                new MinionData(InstanceCreatureIds.Brundir,       BossIds.AssemblyOfIron),
                new MinionData(0,                        0                          )
            };

            // Creatures
            ObjectGuid LeviathanGUID;
            List<ObjectGuid> LeviathanVehicleGUIDs = new List<ObjectGuid>();
            ObjectGuid IgnisGUID;
            ObjectGuid RazorscaleGUID;
            ObjectGuid RazorscaleController;
            ObjectGuid ExpeditionCommanderGUID;
            ObjectGuid XT002GUID;
            ObjectGuid[] XTToyPileGUIDs = new ObjectGuid[4];
            ObjectGuid[] AssemblyGUIDs = new ObjectGuid[3];
            ObjectGuid KologarnGUID;
            ObjectGuid AuriayaGUID;
            ObjectGuid HodirGUID;
            ObjectGuid ThorimGUID;
            ObjectGuid FreyaGUID;
            ObjectGuid[] ElderGUIDs = new ObjectGuid[3];
            ObjectGuid FreyaAchieveTriggerGUID;
            ObjectGuid MimironGUID;
            ObjectGuid[] MimironVehicleGUIDs = new ObjectGuid[3];
            ObjectGuid MimironComputerGUID;
            ObjectGuid MimironWorldTriggerGUID;
            ObjectGuid VezaxGUID;
            ObjectGuid YoggSaronGUID;
            ObjectGuid VoiceOfYoggSaronGUID;
            ObjectGuid SaraGUID;
            ObjectGuid BrainOfYoggSaronGUID;
            ObjectGuid[] KeeperGUIDs = new ObjectGuid[4];
            ObjectGuid AlgalonGUID;
            ObjectGuid BrannBronzebeardAlgGUID;

            // GameObjects
            ObjectGuid LeviathanGateGUID;
            ObjectGuid[] RazorHarpoonGUIDs = new ObjectGuid[4];
            ObjectGuid KologarnChestGUID;
            ObjectGuid KologarnBridgeGUID;
            ObjectGuid ThorimChestGUID;
            ObjectGuid HodirRareCacheGUID;
            ObjectGuid HodirChestGUID;
            ObjectGuid MimironTramGUID;
            ObjectGuid MimironElevatorGUID;
            ObjectGuid MimironButtonGUID;
            ObjectGuid[] BrainRoomDoorGUIDs = new ObjectGuid[3];
            ObjectGuid[] AlgalonSigilDoorGUID = new ObjectGuid[3];
            ObjectGuid[] AlgalonFloorGUID = new ObjectGuid[2];
            ObjectGuid AlgalonUniverseGUID;
            ObjectGuid AlgalonTrapdoorGUID;
            ObjectGuid GiftOfTheObserverGUID;

            // Miscellaneous
            Team TeamInInstance;
            uint HodirRareCacheData;
            uint ColossusData;
            //byte elderCount;
            byte illusion;
            byte keepersCount;
            bool conSpeedAtory;
            bool lumberjacked;
            bool Unbroken;
            bool IsDriveMeCrazyEligible;

            uint _algalonTimer;
            bool _summonAlgalon;
            bool _algalonSummoned;
            bool[] _summonObservationRingKeeper = new bool[4];
            bool[] _summonYSKeeper = new bool[4];
            uint _maxArmorItemLevel;
            uint _maxWeaponItemLevel;
            uint _CoUAchivePlayerDeathMask;
        }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_ulduar_InstanceMapScript(map);
        }
    }
}
