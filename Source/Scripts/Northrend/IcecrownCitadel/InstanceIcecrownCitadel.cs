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
using Game;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Network.Packets;
using Game.Scripting;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scripts.Northrend.IcecrownCitadel
{
    [Script]
    public class InstanceIcecrownCitadel : InstanceMapScript
    {
        public struct EventIds
        {
            public const uint PlayersGunshipSpawn = 22663;
            public const uint PlayersGunshipCombat = 22664;
            public const uint PlayersGunshipSaurfang = 22665;
            public const uint EnemyGunshipCombat = 22860;
            public const uint EnemyGunshipDespawn = 22861;
            public const uint Quake = 23437;
            public const uint SeconsRemorselessWinter = 23507;
            public const uint TeleportToFrostmourne = 23617;
        }

        public struct TimedEvents
        {
            public const uint UpdateExecutionTime = 1;
            public const uint QuakeShatter = 2;
            public const uint RebuildPlatform = 3;
            public const uint RespawnGunship = 4;
        }

        public static BossBoundaryEntry[] boundaries =
        {
            new BossBoundaryEntry(Bosses.LordMarrowgar, new CircleBoundary(new Position(-428.0f, 2211.0f), 95.0)),
            new BossBoundaryEntry(Bosses.LordMarrowgar, new RectangleBoundary(-430.0f, -330.0f, 2110.0f, 2310.0f) ),
            new BossBoundaryEntry(Bosses.LadyDeathwhisper, new RectangleBoundary(-670.0f, -520.0f, 2145.0f, 2280.0f) ),
            new BossBoundaryEntry(Bosses.DeathbringerSaurfang, new RectangleBoundary(-565.0f, -465.0f, 2160.0f, 2260.0f) ),

            new BossBoundaryEntry(Bosses.Rotface, new RectangleBoundary(4385.0f, 4505.0f, 3082.0f, 3195.0f) ),
            new BossBoundaryEntry(Bosses.Festergut, new RectangleBoundary(4205.0f, 4325.0f, 3082.0f, 3195.0f) ),
            new BossBoundaryEntry(Bosses.ProfessorPutricide, new ParallelogramBoundary(new Position(4356.0f, 3290.0f), new Position(4435.0f, 3194.0f), new Position(4280.0f, 3194.0f)) ),
            new BossBoundaryEntry(Bosses.ProfessorPutricide, new RectangleBoundary(4280.0f, 4435.0f, 3150.0f, 4360.0f) ),

            new BossBoundaryEntry(Bosses.BloodPrinceCouncil, new EllipseBoundary(new Position(4660.95f, 2769.194f), 85.0, 60.0) ),
            new BossBoundaryEntry(Bosses.BloodQueenLanaThel, new CircleBoundary(new Position(4595.93f, 2769.365f), 64.0) ),

            new BossBoundaryEntry(Bosses.SisterSvalna, new RectangleBoundary(4291.0f, 4423.0f, 2438.0f, 2653.0f) ),
            new BossBoundaryEntry(Bosses.ValithriaDreamwalker, new RectangleBoundary(4112.5f, 4293.5f, 2385.0f, 2585.0f) ),
            new BossBoundaryEntry(Bosses.Sindragosa, new EllipseBoundary(new Position(4408.6f, 2484.0f), 100.0, 75.0) )
        };

        public static DoorData[] doorData =
        {
            new DoorData(GameObjectIds.LordMarrowgarSEntrance,          Bosses.LordMarrowgar,        DoorType.Room     ),
            new DoorData(GameObjectIds.LordMarrowgarIcewall,            Bosses.LordMarrowgar,        DoorType.Passage  ),
            new DoorData(GameObjectIds.DoodadIcecrownIcewall02,         Bosses.LordMarrowgar,        DoorType.Passage  ),
            new DoorData(GameObjectIds.OratoryOfTheDamnedEntrance,      Bosses.LadyDeathwhisper,     DoorType.Room     ),
            new DoorData(GameObjectIds.SaurfangSDoor,                   Bosses.DeathbringerSaurfang, DoorType.Passage  ),
            new DoorData(GameObjectIds.OrangePlagueMonsterEntrance,     Bosses.Festergut,            DoorType.Room     ),
            new DoorData(GameObjectIds.GreenPlagueMonsterEntrance,      Bosses.Rotface,              DoorType.Room     ),
            new DoorData(GameObjectIds.ScientistEntrance,               Bosses.ProfessorPutricide,   DoorType.Room     ),
            new DoorData(GameObjectIds.CrimsonHallDoor,                 Bosses.BloodPrinceCouncil,   DoorType.Room     ),
            new DoorData(GameObjectIds.BloodElfCouncilDoor,             Bosses.BloodPrinceCouncil,   DoorType.Passage  ),
            new DoorData(GameObjectIds.BloodElfCouncilDoorRight,        Bosses.BloodPrinceCouncil,   DoorType.Passage  ),
            new DoorData(GameObjectIds.DoodadIcecrownBloodprinceDoor01, Bosses.BloodQueenLanaThel,   DoorType.Room     ),
            new DoorData(GameObjectIds.DoodadIcecrownGrate01,           Bosses.BloodQueenLanaThel,   DoorType.Passage  ),
            new DoorData(GameObjectIds.GreenDragonBossEntrance,         Bosses.SisterSvalna,         DoorType.Passage  ),
            new DoorData(GameObjectIds.GreenDragonBossEntrance,         Bosses.ValithriaDreamwalker, DoorType.Room     ),
            new DoorData(GameObjectIds.GreenDragonBossExit,             Bosses.ValithriaDreamwalker, DoorType.Passage  ),
            new DoorData(GameObjectIds.DoodadIcecrownRoostportcullis01, Bosses.ValithriaDreamwalker, DoorType.SpawnHole),
            new DoorData(GameObjectIds.DoodadIcecrownRoostportcullis02, Bosses.ValithriaDreamwalker, DoorType.SpawnHole),
            new DoorData(GameObjectIds.DoodadIcecrownRoostportcullis03, Bosses.ValithriaDreamwalker, DoorType.SpawnHole),
            new DoorData(GameObjectIds.DoodadIcecrownRoostportcullis04, Bosses.ValithriaDreamwalker, DoorType.SpawnHole),
            new DoorData(GameObjectIds.SindragosaEntranceDoor,          Bosses.Sindragosa,           DoorType.Room     ),
            new DoorData(GameObjectIds.SindragosaShortcutEntranceDoor,  Bosses.Sindragosa,           DoorType.Passage  ),
            new DoorData(GameObjectIds.SindragosaShortcutExitDoor,      Bosses.Sindragosa,           DoorType.Passage  ),
            new DoorData(GameObjectIds.IceWall ,                        Bosses.Sindragosa,           DoorType.Room     ),
            new DoorData(GameObjectIds.IceWall,                         Bosses.Sindragosa,           DoorType.Room     ),
            new DoorData(0,                                             0,                           DoorType.Room     ), // END
        };

        public class WeeklyQuest
        {
            public WeeklyQuest(uint entry, uint id1, uint id2)
            {
                creatureEntry = entry;
                questId[0] = id1;
                questId[1] = id2;
            }

            public uint creatureEntry;
            public uint[] questId = new uint[2];  // 10 and 25 man versions
        }

        public static WeeklyQuest[] WeeklyQuestData =
        {
            new WeeklyQuest(CreatureIds.InfiltratorMinchar,        WeeklyQuestIds.Deprogramming10,             WeeklyQuestIds.Deprogramming25            ), // Deprogramming
            new WeeklyQuest(CreatureIds.KorKronLieutenant,         WeeklyQuestIds.SecuringTheRamparts10,       WeeklyQuestIds.SecuringTheRamparts25      ), // Securing the Ramparts
            new WeeklyQuest(CreatureIds.RottingFrostGiant10,       WeeklyQuestIds.SecuringTheRamparts10,       WeeklyQuestIds.SecuringTheRamparts25      ), // Securing the Ramparts
            new WeeklyQuest(CreatureIds.RottingFrostGiant25,       WeeklyQuestIds.SecuringTheRamparts10,       WeeklyQuestIds.SecuringTheRamparts25      ), // Securing the Ramparts
            new WeeklyQuest(CreatureIds.AlchemistAdrianna,         WeeklyQuestIds.ResidueRendezvous10,         WeeklyQuestIds.ResidueRendezvous25        ), // Residue Rendezvous
            new WeeklyQuest(CreatureIds.AlrinTheAgile,             WeeklyQuestIds.BloodQuickening10,           WeeklyQuestIds.BloodQuickening25          ), // Blood Quickening
            new WeeklyQuest(CreatureIds.InfiltratorMincharBq,      WeeklyQuestIds.BloodQuickening10,           WeeklyQuestIds.BloodQuickening25          ), // Blood Quickening
            new WeeklyQuest(CreatureIds.MincharBeamStalker,        WeeklyQuestIds.BloodQuickening10,           WeeklyQuestIds.BloodQuickening25          ), // Blood Quickening
            new WeeklyQuest(CreatureIds.ValithriaDreamwalkerQuest, WeeklyQuestIds.RespiteForATornmentedSoul10, WeeklyQuestIds.RespiteForATornmentedSoul25), // Respite for a Tormented Soul
        };

        // NPCs spawned at Light's Hammer on Lich King dead
        static Position JainaSpawnPos = new Position(-48.65278f, 2211.026f, 27.98586f, 3.124139f);
        static Position MuradinSpawnPos = new Position(-47.34549f, 2208.087f, 27.98586f, 3.106686f);
        static Position UtherSpawnPos = new Position(-26.58507f, 2211.524f, 30.19898f, 3.124139f);
        static Position SylvanasSpawnPos = new Position(-41.45833f, 2222.891f, 27.98586f, 3.647738f);

        public InstanceIcecrownCitadel() : base("instance_icecrown_citadel", 631) { }

        class instance_icecrown_citadel_InstanceMapScript : InstanceScript
        {
            public instance_icecrown_citadel_InstanceMapScript(InstanceMap map) : base(map)
            {
                SetHeaders("IC");
                SetBossNumber(Bosses.MaxEncounters);
                LoadBossBoundaries(boundaries);
                LoadDoorData(doorData);
                //HeroicAttempts = IccConst.MaxHeroicAttempts;
                IsBonedEligible = true;
                IsOozeDanceEligible = true;
                IsNauseaEligible = true;
                IsOrbWhispererEligible = true;
                ColdflameJetsState = EncounterState.NotStarted;
                UpperSpireTeleporterActiveState = EncounterState.NotStarted;
                BloodQuickeningState = EncounterState.NotStarted;
            }

            // A function to help reduce the number of lines for teleporter management.
            void SetTeleporterState(GameObject go, bool usable)
            {
                if (usable)
                {
                    go.RemoveFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                    go.SetGoState(GameObjectState.Active);
                }
                else
                {
                    go.SetFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                    go.SetGoState(GameObjectState.Ready);
                }
            }

            public override void FillInitialWorldStates(InitWorldStates packet)
            {
                packet.AddState(WorldStates.ShowTimer, BloodQuickeningState == EncounterState.InProgress ? 1 : 0);
                packet.AddState(WorldStates.ExecutionTime, BloodQuickeningMinutes);
                //packet.AddState(WorldStates.ShowAttempts, instance.IsHeroic() ? 1 : 0);
                //packet.AddState(WorldStates.AttemptsRemaining, (int)HeroicAttempts);
                //packet.AddState(WorldStates.AttemptsMax, (int)IccConst.MaxHeroicAttempts);
            }

            public override void OnPlayerEnter(Player player)
            {
                if (TeamInInstance == 0)
                    TeamInInstance = player.GetTeam();

                if (GetBossState(Bosses.LadyDeathwhisper) == EncounterState.Done && GetBossState(Bosses.GunshipBattle) != EncounterState.Done)
                    SpawnGunship();
            }

            public override void OnCreatureCreate(Creature creature)
            {
                if (TeamInInstance == 0)
                {
                    var players = instance.GetPlayers();
                    if (!players.Empty())
                    {
                        Player player = players.FirstOrDefault();
                        if (player)
                            TeamInInstance = player.GetTeam();
                    }
                }

                switch (creature.GetEntry())
                {
                    case CreatureIds.KorKronGeneral:
                        if (TeamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.AllianceCommander);
                        break;
                    case CreatureIds.KorKronLieutenant:
                        if (TeamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.SkybreakerLieutenant);
                        break;
                    case CreatureIds.Tortunok:
                        if (TeamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.AlanaMoonstrike);
                        break;
                    case CreatureIds.GerardoTheSuave:
                        if (TeamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.TalanMoonstrike);
                        break;
                    case CreatureIds.UvlusBanefire:
                        if (TeamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.MalfusGrimfrost);
                        break;
                    case CreatureIds.IkfirusTheVile:
                        if (TeamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.Yili);
                        break;
                    case CreatureIds.VolGuk:
                        if (TeamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.Jedebia);
                        break;
                    case CreatureIds.HaraggTheUnseen:
                        if (TeamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.NibyTheAlmighty);
                        break;
                    case CreatureIds.GarroshHellscream:
                        if (TeamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.KingVarianWrynn);
                        break;
                    case CreatureIds.DeathbringerSaurfang:
                        DeathbringerSaurfangGUID = creature.GetGUID();
                        break;
                    case CreatureIds.AllianceGunshipCannon:
                    case CreatureIds.HordeGunshipCannon:
                        creature.SetControlled(true, UnitState.Root);
                        break;
                    case CreatureIds.SeHighOverlordSaurfang:
                        if (TeamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.SeMuradinBronzebeard, creature.GetCreatureData());
                        goto case CreatureIds.SeMuradinBronzebeard;
                    // no break;
                    case CreatureIds.SeMuradinBronzebeard:
                        DeathbringerSaurfangEventGUID = creature.GetGUID();
                        creature.LastUsedScriptID = creature.GetScriptId();
                        break;
                    case CreatureIds.SeKorKronReaver:
                        if (TeamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.SeSkybreakerMarine);
                        break;
                    case CreatureIds.Festergut:
                        FestergutGUID = creature.GetGUID();
                        break;
                    case CreatureIds.Rotface:
                        RotfaceGUID = creature.GetGUID();
                        break;
                    case CreatureIds.ProfessorPutricide:
                        ProfessorPutricideGUID = creature.GetGUID();
                        break;
                    case CreatureIds.PrinceKeleseth:
                        BloodCouncilGUIDs[0] = creature.GetGUID();
                        break;
                    case CreatureIds.PrinceTaldaram:
                        BloodCouncilGUIDs[1] = creature.GetGUID();
                        break;
                    case CreatureIds.PrinceValanar:
                        BloodCouncilGUIDs[2] = creature.GetGUID();
                        break;
                    case CreatureIds.BloodOrbController:
                        BloodCouncilControllerGUID = creature.GetGUID();
                        break;
                    case CreatureIds.BloodQueenLanaThel:
                        BloodQueenLanaThelGUID = creature.GetGUID();
                        break;
                    case CreatureIds.CrokScourgebane:
                        CrokScourgebaneGUID = creature.GetGUID();
                        break;
                    // we can only do this because there are no gaps in their entries
                    case CreatureIds.CaptainArnath:
                    case CreatureIds.CaptainBrandon:
                    case CreatureIds.CaptainGrondel:
                    case CreatureIds.CaptainRupert:
                        CrokCaptainGUIDs[creature.GetEntry() - CreatureIds.CaptainArnath] = creature.GetGUID();
                        break;
                    case CreatureIds.SisterSvalna:
                        SisterSvalnaGUID = creature.GetGUID();
                        break;
                    case CreatureIds.ValithriaDreamwalker:
                        ValithriaDreamwalkerGUID = creature.GetGUID();
                        break;
                    case CreatureIds.TheLichKingValithria:
                        ValithriaLichKingGUID = creature.GetGUID();
                        break;
                    case CreatureIds.GreenDragonCombatTrigger:
                        ValithriaTriggerGUID = creature.GetGUID();
                        break;
                    case CreatureIds.Sindragosa:
                        SindragosaGUID = creature.GetGUID();
                        break;
                    case CreatureIds.Spinestalker:
                        SpinestalkerGUID = creature.GetGUID();
                        break;
                    case CreatureIds.Rimefang:
                        RimefangGUID = creature.GetGUID();
                        break;
                    case CreatureIds.InvisibleStalker:
                        // Teleporter visual at center
                        if (creature.GetExactDist2d(4357.052f, 2769.421f) < 10.0f)
                            creature.CastSpell(creature, InstanceSpells.ArthasTeleporterCeremony, false);
                        break;
                    case CreatureIds.TheLichKing:
                        TheLichKingGUID = creature.GetGUID();
                        break;
                    case CreatureIds.HighlordTirionFordringLk:
                        HighlordTirionFordringGUID = creature.GetGUID();
                        break;
                    case CreatureIds.TerenasMenethilFrostmourne:
                    case CreatureIds.TerenasMenethilFrostmourneH:
                        TerenasMenethilGUID = creature.GetGUID();
                        break;
                    case CreatureIds.WickedSpirit:
                        // Remove corpse as soon as it dies (and respawn 10 seconds later)
                        creature.SetCorpseDelay(0);
                        creature.SetReactState(ReactStates.Passive);
                        break;
                    default:
                        break;
                }
            }

            public override void OnCreatureRemove(Creature creature)
            {
                if (creature.GetEntry() == CreatureIds.Sindragosa)
                    SindragosaGUID.Clear();
            }

            // Weekly quest spawn prevention
            public override uint GetCreatureEntry(ulong guidLow, CreatureData data)
            {
                uint entry = data.id;
                switch (entry)
                {
                    case CreatureIds.InfiltratorMinchar:
                    case CreatureIds.KorKronLieutenant:
                    case CreatureIds.AlchemistAdrianna:
                    case CreatureIds.AlrinTheAgile:
                    case CreatureIds.InfiltratorMincharBq:
                    case CreatureIds.MincharBeamStalker:
                    case CreatureIds.ValithriaDreamwalkerQuest:
                        {
                            for (byte questIndex = 0; questIndex < IccConst.WeeklyNPCs; ++questIndex)
                            {
                                if (WeeklyQuestData[questIndex].creatureEntry == entry)
                                {
                                    byte diffIndex = (byte)((int)instance.GetDifficultyID() & 1);
                                    if (!Global.PoolMgr.IsSpawnedObject<Quest>(WeeklyQuestData[questIndex].questId[diffIndex]))
                                        entry = 0;
                                    break;
                                }
                            }
                            break;
                        }
                    case CreatureIds.HordeGunshipCannon:
                    case CreatureIds.OrgrimsHammerCrew:
                    case CreatureIds.SkyReaverKormBlackscar:
                        if (TeamInInstance == Team.Alliance)
                            return 0;
                        break;
                    case CreatureIds.AllianceGunshipCannon:
                    case CreatureIds.SkybreakerDeckhand:
                    case CreatureIds.HighCaptainJustinBartlett:
                        if (TeamInInstance == Team.Horde)
                            return 0;
                        break;
                    case CreatureIds.ZafodBoombox:
                        GameObjectTemplate go = Global.ObjectMgr.GetGameObjectTemplate(GameObjectIds.TheSkybreaker_A);
                        if (go != null)
                            if ((TeamInInstance == Team.Alliance && data.mapid == go.MoTransport.SpawnMap) ||
                                (TeamInInstance == Team.Horde && data.mapid != go.MoTransport.SpawnMap))
                                return entry;
                        return 0;
                    case CreatureIds.IGBMuradinBrozebeard:
                        if ((TeamInInstance == Team.Alliance && data.posX > 10.0f) ||
                            (TeamInInstance == Team.Horde && data.posX < 10.0f))
                            return entry;
                        return 0;
                    default:
                        break;
                }

                return entry;
            }

            public override uint GetGameObjectEntry(ulong spawnId, uint entry)
            {
                switch (entry)
                {
                    case GameObjectIds.GunshipArmory_H_10N:
                    case GameObjectIds.GunshipArmory_H_25N:
                    case GameObjectIds.GunshipArmory_H_10H:
                    case GameObjectIds.GunshipArmory_H_25H:
                        if (TeamInInstance == Team.Alliance)
                            return 0;
                        break;
                    case GameObjectIds.GunshipArmory_A_10N:
                    case GameObjectIds.GunshipArmory_A_25N:
                    case GameObjectIds.GunshipArmory_A_10H:
                    case GameObjectIds.GunshipArmory_A_25H:
                        if (TeamInInstance == Team.Horde)
                            return 0;
                        break;
                    default:
                        break;
                }

                return entry;
            }

            public override void OnUnitDeath(Unit unit)
            {
                Creature creature = unit.ToCreature();
                if (!creature)
                    return;

                switch (creature.GetEntry())
                {
                    case CreatureIds.YmirjarBattleMaiden:
                    case CreatureIds.YmirjarDeathbringer:
                    case CreatureIds.YmirjarFrostbinder:
                    case CreatureIds.YmirjarHuntress:
                    case CreatureIds.YmirjarWarlord:
                        Creature crok = instance.GetCreature(CrokScourgebaneGUID);
                        if (crok)
                            crok.GetAI().SetGUID(creature.GetGUID(), Actions.VrykulDeath);
                        break;
                    case CreatureIds.FrostwingWhelp:
                        if (FrostwyrmGUIDs.Empty())
                            return;

                        if (creature.GetAI().GetData(1/*DATA_FROSTWYRM_OWNER*/) == DataTypes.Spinestalker)
                        {
                            SpinestalkerTrash.Remove(creature.GetSpawnId());
                            if (SpinestalkerTrash.Empty())
                            {
                                Creature spinestalk = instance.GetCreature(SpinestalkerGUID);
                                if (spinestalk)
                                    spinestalk.GetAI().DoAction(Actions.StartFrostwyrm);
                            }
                        }
                        else
                        {
                            RimefangTrash.Remove(creature.GetSpawnId());
                            if (RimefangTrash.Empty())
                            {
                                Creature spinestalk = instance.GetCreature(RimefangGUID);
                                if (spinestalk)
                                    spinestalk.GetAI().DoAction(Actions.StartFrostwyrm);
                            }
                        }
                        break;
                    case CreatureIds.Rimefang:
                    case CreatureIds.Spinestalker:
                        {
                            if (instance.IsHeroic() && HeroicAttempts == 0)
                                return;

                            if (GetBossState(Bosses.Sindragosa) == EncounterState.Done)
                                return;

                            FrostwyrmGUIDs.Remove(creature.GetSpawnId());
                            if (FrostwyrmGUIDs.Empty())
                            {
                                instance.LoadGrid(Sindragosa.SindragosaSpawnPos.GetPositionX(), Sindragosa.SindragosaSpawnPos.GetPositionY());
                                Creature boss = instance.SummonCreature(CreatureIds.Sindragosa, Sindragosa.SindragosaSpawnPos);
                                if (boss)
                                    boss.GetAI().DoAction(Actions.StartFrostwyrm);
                            }
                            break;
                        }
                    default:
                        break;
                }
            }

            public override void OnGameObjectCreate(GameObject go)
            {
                switch (go.GetEntry())
                {
                    case GameObjectIds.DoodadIcecrownIcewall02:
                    case GameObjectIds.LordMarrowgarIcewall:
                    case GameObjectIds.LordMarrowgarSEntrance:
                    case GameObjectIds.OratoryOfTheDamnedEntrance:
                    case GameObjectIds.OrangePlagueMonsterEntrance:
                    case GameObjectIds.GreenPlagueMonsterEntrance:
                    case GameObjectIds.ScientistEntrance:
                    case GameObjectIds.CrimsonHallDoor:
                    case GameObjectIds.BloodElfCouncilDoor:
                    case GameObjectIds.BloodElfCouncilDoorRight:
                    case GameObjectIds.DoodadIcecrownBloodprinceDoor01:
                    case GameObjectIds.DoodadIcecrownGrate01:
                    case GameObjectIds.GreenDragonBossEntrance:
                    case GameObjectIds.GreenDragonBossExit:
                    case GameObjectIds.DoodadIcecrownRoostportcullis02:
                    case GameObjectIds.DoodadIcecrownRoostportcullis03:
                    case GameObjectIds.SindragosaEntranceDoor:
                    case GameObjectIds.SindragosaShortcutEntranceDoor:
                    case GameObjectIds.SindragosaShortcutExitDoor:
                    case GameObjectIds.IceWall:
                        AddDoor(go, true);
                        break;
                    // these 2 gates are functional only on 25man modes
                    case GameObjectIds.DoodadIcecrownRoostportcullis01:
                    case GameObjectIds.DoodadIcecrownRoostportcullis04:
                        if (instance.Is25ManRaid())
                            AddDoor(go, true);
                        break;
                    case GameObjectIds.LadyDeathwhisperElevator:
                        LadyDeathwisperElevatorGUID = go.GetGUID();
                        if (GetBossState(Bosses.LadyDeathwhisper) == EncounterState.Done)
                            go.SetTransportState(GameObjectState.TransportActive);
                        break;
                    case GameObjectIds.TheSkybreaker_H:
                    case GameObjectIds.OrgrimsHammer_A:
                        EnemyGunshipGUID = go.GetGUID();
                        break;
                    case GameObjectIds.GunshipArmory_H_10N:
                    case GameObjectIds.GunshipArmory_H_25N:
                    case GameObjectIds.GunshipArmory_H_10H:
                    case GameObjectIds.GunshipArmory_H_25H:
                    case GameObjectIds.GunshipArmory_A_10N:
                    case GameObjectIds.GunshipArmory_A_25N:
                    case GameObjectIds.GunshipArmory_A_10H:
                    case GameObjectIds.GunshipArmory_A_25H:
                        GunshipArmoryGUID = go.GetGUID();
                        break;
                    case GameObjectIds.SaurfangSDoor:
                        DeathbringerSaurfangDoorGUID = go.GetGUID();
                        AddDoor(go, true);
                        break;
                    case GameObjectIds.DeathbringerSCache10n:
                    case GameObjectIds.DeathbringerSCache25n:
                    case GameObjectIds.DeathbringerSCache10h:
                    case GameObjectIds.DeathbringerSCache25h:
                        DeathbringersCacheGUID = go.GetGUID();
                        break;
                    case GameObjectIds.TransporterLichKing:
                        TeleporterLichKingGUID = go.GetGUID();
                        if (GetBossState(Bosses.ProfessorPutricide) == EncounterState.Done && GetBossState(Bosses.BloodQueenLanaThel) == EncounterState.Done && GetBossState(Bosses.Sindragosa) == EncounterState.Done)
                            go.SetGoState(GameObjectState.Active);
                        break;
                    case GameObjectIds.TransporterUpperSpire:
                        TeleporterUpperSpireGUID = go.GetGUID();
                        if (GetBossState(Bosses.DeathbringerSaurfang) != EncounterState.Done || GetData(DataTypes.UpperSpireTeleAct) != (uint)EncounterState.Done)
                            SetTeleporterState(go, false);
                        else
                            SetTeleporterState(go, true);
                        break;
                    case GameObjectIds.TransporterLightsHammer:
                        TeleporterLightsHammerGUID = go.GetGUID();
                        SetTeleporterState(go, GetBossState(Bosses.LordMarrowgar) == EncounterState.Done);
                        break;
                    case GameObjectIds.TransporterRampart:
                        TeleporterRampartsGUID = go.GetGUID();
                        SetTeleporterState(go, GetBossState(Bosses.LadyDeathwhisper) == EncounterState.Done);
                        break;
                    case GameObjectIds.TransporterDeathBringer:
                        TeleporterDeathBringerGUID = go.GetGUID();
                        SetTeleporterState(go, GetBossState(Bosses.GunshipBattle) == EncounterState.Done);
                        break;
                    case GameObjectIds.TransporterOratory:
                        TeleporterOratoryGUID = go.GetGUID();
                        SetTeleporterState(go, GetBossState(Bosses.LordMarrowgar) == EncounterState.Done);
                        break;
                    case GameObjectIds.TransporterSindragosa:
                        TeleporterSindragosaGUID = go.GetGUID();
                        SetTeleporterState(go, GetBossState(Bosses.ValithriaDreamwalker) == EncounterState.Done);
                        break;
                    case GameObjectIds.PlagueSigil:
                        PlagueSigilGUID = go.GetGUID();
                        if (GetBossState(Bosses.ProfessorPutricide) == EncounterState.Done)
                            HandleGameObject(PlagueSigilGUID, false, go);
                        break;
                    case GameObjectIds.BloodwingSigil:
                        BloodwingSigilGUID = go.GetGUID();
                        if (GetBossState(Bosses.BloodQueenLanaThel) == EncounterState.Done)
                            HandleGameObject(BloodwingSigilGUID, false, go);
                        break;
                    case GameObjectIds.SigilOfTheFrostwing:
                        FrostwingSigilGUID = go.GetGUID();
                        if (GetBossState(Bosses.Sindragosa) == EncounterState.Done)
                            HandleGameObject(FrostwingSigilGUID, false, go);
                        break;
                    case GameObjectIds.ScientistAirlockDoorCollision:
                        PutricideCollisionGUID = go.GetGUID();
                        if (GetBossState(Bosses.Festergut) == EncounterState.Done && GetBossState(Bosses.Rotface) == EncounterState.Done)
                            HandleGameObject(PutricideCollisionGUID, true, go);
                        break;
                    case GameObjectIds.ScientistAirlockDoorOrange:
                        PutricideGateGUIDs[0] = go.GetGUID();
                        if (GetBossState(Bosses.Festergut) == EncounterState.Done && GetBossState(Bosses.Rotface) == EncounterState.Done)
                            go.SetGoState(GameObjectState.ActiveAlternative);
                        else if (GetBossState(Bosses.Festergut) == EncounterState.Done)
                            HandleGameObject(PutricideGateGUIDs[1], false, go);
                        break;
                    case GameObjectIds.ScientistAirlockDoorGreen:
                        PutricideGateGUIDs[1] = go.GetGUID();
                        if (GetBossState(Bosses.Rotface) == EncounterState.Done && GetBossState(Bosses.Festergut) == EncounterState.Done)
                            go.SetGoState(GameObjectState.ActiveAlternative);
                        else if (GetBossState(Bosses.Rotface) == EncounterState.Done)
                            HandleGameObject(PutricideGateGUIDs[1], false, go);
                        break;
                    case GameObjectIds.DoodadIcecrownOrangetubes02:
                        PutricidePipeGUIDs[0] = go.GetGUID();
                        if (GetBossState(Bosses.Festergut) == EncounterState.Done)
                            HandleGameObject(PutricidePipeGUIDs[0], true, go);
                        break;
                    case GameObjectIds.DoodadIcecrownGreentubes02:
                        PutricidePipeGUIDs[1] = go.GetGUID();
                        if (GetBossState(Bosses.Rotface) == EncounterState.Done)
                            HandleGameObject(PutricidePipeGUIDs[1], true, go);
                        break;
                    case GameObjectIds.DrinkMe:
                        PutricideTableGUID = go.GetGUID();
                        break;
                    case GameObjectIds.CacheOfTheDreamwalker10n:
                    case GameObjectIds.CacheOfTheDreamwalker25n:
                    case GameObjectIds.CacheOfTheDreamwalker10h:
                    case GameObjectIds.CacheOfTheDreamwalker25h:
                        Creature valithria = instance.GetCreature(ValithriaDreamwalkerGUID);
                        if (valithria)
                            go.SetLootRecipient(valithria.GetLootRecipient(), valithria.GetLootRecipientGroup());
                        go.RemoveFlag(GameObjectFields.Flags, GameObjectFlags.Locked | GameObjectFlags.NotSelectable | GameObjectFlags.NoDespawn);
                        break;
                    case GameObjectIds.ArthasPlatform:
                        ArthasPlatformGUID = go.GetGUID();
                        break;
                    case GameObjectIds.ArthasPrecipice:
                        ArthasPrecipiceGUID = go.GetGUID();
                        break;
                    case GameObjectIds.DoodadIcecrownThronefrostyedge01:
                        FrozenThroneEdgeGUID = go.GetGUID();
                        break;
                    case GameObjectIds.DoodadIcecrownThronefrostywind01:
                        FrozenThroneWindGUID = go.GetGUID();
                        break;
                    case GameObjectIds.DoodadIcecrownSnowedgewarning01:
                        FrozenThroneWarningGUID = go.GetGUID();
                        break;
                    case GameObjectIds.FrozenLavaman:
                        FrozenBolvarGUID = go.GetGUID();
                        if (GetBossState(Bosses.TheLichKing) == EncounterState.Done)
                            go.SetRespawnTime(7 * Time.Day);
                        break;
                    case GameObjectIds.LavamanPillarsChained:
                        PillarsChainedGUID = go.GetGUID();
                        if (GetBossState(Bosses.TheLichKing) == EncounterState.Done)
                            go.SetRespawnTime(7 * Time.Day);
                        break;
                    case GameObjectIds.LavamanPillarsUnchained:
                        PillarsUnchainedGUID = go.GetGUID();
                        if (GetBossState(Bosses.TheLichKing) == EncounterState.Done)
                            go.SetRespawnTime(7 * Time.Day);
                        break;
                    default:
                        break;
                }
            }

            public override void OnGameObjectRemove(GameObject go)
            {
                switch (go.GetEntry())
                {
                    case GameObjectIds.DoodadIcecrownIcewall02:
                    case GameObjectIds.LordMarrowgarIcewall:
                    case GameObjectIds.LordMarrowgarSEntrance:
                    case GameObjectIds.OratoryOfTheDamnedEntrance:
                    case GameObjectIds.SaurfangSDoor:
                    case GameObjectIds.OrangePlagueMonsterEntrance:
                    case GameObjectIds.GreenPlagueMonsterEntrance:
                    case GameObjectIds.ScientistEntrance:
                    case GameObjectIds.CrimsonHallDoor:
                    case GameObjectIds.BloodElfCouncilDoor:
                    case GameObjectIds.BloodElfCouncilDoorRight:
                    case GameObjectIds.DoodadIcecrownBloodprinceDoor01:
                    case GameObjectIds.DoodadIcecrownGrate01:
                    case GameObjectIds.GreenDragonBossEntrance:
                    case GameObjectIds.GreenDragonBossExit:
                    case GameObjectIds.DoodadIcecrownRoostportcullis01:
                    case GameObjectIds.DoodadIcecrownRoostportcullis02:
                    case GameObjectIds.DoodadIcecrownRoostportcullis03:
                    case GameObjectIds.DoodadIcecrownRoostportcullis04:
                    case GameObjectIds.SindragosaEntranceDoor:
                    case GameObjectIds.SindragosaShortcutEntranceDoor:
                    case GameObjectIds.SindragosaShortcutExitDoor:
                    case GameObjectIds.IceWall:
                        AddDoor(go, false);
                        break;
                    case GameObjectIds.TheSkybreaker_A:
                    case GameObjectIds.OrgrimsHammer_H:
                        GunshipGUID.Clear();
                        break;
                    default:
                        break;
                }
            }

            public override uint GetData(uint type)
            {
                switch (type)
                {
                    case DataTypes.SindragosaFrostwyrms:
                        return (uint)FrostwyrmGUIDs.Count;
                    case DataTypes.Spinestalker:
                        return (uint)SpinestalkerTrash.Count;
                    case DataTypes.Rimefang:
                        return (uint)RimefangTrash.Count;
                    case DataTypes.ColdflameJets:
                        return (uint)ColdflameJetsState;
                    case DataTypes.UpperSpireTeleAct:
                        return (uint)UpperSpireTeleporterActiveState;
                    case DataTypes.TeamInInstance:
                        return (uint)TeamInInstance;
                    case DataTypes.BloodQuickeningState:
                        return (uint)BloodQuickeningState;
                    case DataTypes.HeroicAttempts:
                        return HeroicAttempts;
                    default:
                        break;
                }

                return 0;
            }

            public override ObjectGuid GetGuidData(uint type)
            {
                switch (type)
                {
                    case Bosses.GunshipBattle:
                        return GunshipGUID;
                    case DataTypes.EnemyGunship:
                        return EnemyGunshipGUID;
                    case Bosses.DeathbringerSaurfang:
                        return DeathbringerSaurfangGUID;
                    case DataTypes.SaurfangEventNpc:
                        return DeathbringerSaurfangEventGUID;
                    case GameObjectIds.SaurfangSDoor:
                        return DeathbringerSaurfangDoorGUID;
                    case Bosses.Festergut:
                        return FestergutGUID;
                    case Bosses.Rotface:
                        return RotfaceGUID;
                    case Bosses.ProfessorPutricide:
                        return ProfessorPutricideGUID;
                    case DataTypes.PutricideTable:
                        return PutricideTableGUID;
                    case DataTypes.PrinceKelesethGuid:
                        return BloodCouncilGUIDs[0];
                    case DataTypes.PrinceTaldaramGuid:
                        return BloodCouncilGUIDs[1];
                    case DataTypes.PrinceValanarGuid:
                        return BloodCouncilGUIDs[2];
                    case DataTypes.BloodPrincesControl:
                        return BloodCouncilControllerGUID;
                    case Bosses.BloodQueenLanaThel:
                        return BloodQueenLanaThelGUID;
                    case DataTypes.CrokScourgebane:
                        return CrokScourgebaneGUID;
                    case DataTypes.CaptainArnath:
                    case DataTypes.CaptainBrandon:
                    case DataTypes.CaptainGrondel:
                    case DataTypes.CaptainRupert:
                        return CrokCaptainGUIDs[type - DataTypes.CaptainArnath];
                    case Bosses.SisterSvalna:
                        return SisterSvalnaGUID;
                    case Bosses.ValithriaDreamwalker:
                        return ValithriaDreamwalkerGUID;
                    case DataTypes.ValithriaLichKing:
                        return ValithriaLichKingGUID;
                    case DataTypes.ValithriaTrigger:
                        return ValithriaTriggerGUID;
                    case Bosses.Sindragosa:
                        return SindragosaGUID;
                    case DataTypes.Spinestalker:
                        return SpinestalkerGUID;
                    case DataTypes.Rimefang:
                        return RimefangGUID;
                    case Bosses.TheLichKing:
                        return TheLichKingGUID;
                    case DataTypes.HighlordTirionFordring:
                        return HighlordTirionFordringGUID;
                    case DataTypes.ArthasPlatform:
                        return ArthasPlatformGUID;
                    case DataTypes.TerenasMenethil:
                        return TerenasMenethilGUID;
                    default:
                        break;
                }

                return ObjectGuid.Empty;
            }

            public override bool SetBossState(uint type, EncounterState state)
            {
                if (!base.SetBossState(type, state))
                    return false;

                switch (type)
                {
                    case Bosses.LordMarrowgar:
                        if (state == EncounterState.Done)
                        {
                            GameObject teleporter = instance.GetGameObject(TeleporterLightsHammerGUID);
                            if (teleporter)
                                SetTeleporterState(teleporter, true);

                            teleporter = instance.GetGameObject(TeleporterOratoryGUID);
                            if (teleporter)
                                SetTeleporterState(teleporter, true);
                        }
                        break;
                    case Bosses.LadyDeathwhisper:
                        if (state == EncounterState.Done)
                        {
                            GameObject teleporter = instance.GetGameObject(TeleporterRampartsGUID);
                            if (teleporter)
                                SetTeleporterState(teleporter, true);

                            GameObject elevator = instance.GetGameObject(LadyDeathwisperElevatorGUID);
                            if (elevator)
                                elevator.SetTransportState(GameObjectState.TransportActive);

                            SpawnGunship();
                        }
                        break;
                    case Bosses.GunshipBattle:
                        if (state == EncounterState.Done)
                        {
                            GameObject teleporter = instance.GetGameObject(TeleporterDeathBringerGUID);
                            if (teleporter)
                                SetTeleporterState(teleporter, true);

                            GameObject loot = instance.GetGameObject(GunshipArmoryGUID);
                            if (loot)
                                loot.RemoveFlag(GameObjectFields.Flags, GameObjectFlags.Locked | GameObjectFlags.NotSelectable | GameObjectFlags.NoDespawn);
                        }
                        else if (state == EncounterState.Fail)
                            _events.ScheduleEvent(TimedEvents.RespawnGunship, 30000);
                        break;
                    case Bosses.DeathbringerSaurfang:
                        switch (state)
                        {
                            case EncounterState.Done:
                                {
                                    GameObject loot = instance.GetGameObject(DeathbringersCacheGUID);
                                    if (loot)
                                    {
                                        Creature deathbringer = instance.GetCreature(DeathbringerSaurfangGUID);
                                        if (deathbringer)
                                            loot.SetLootRecipient(deathbringer.GetLootRecipient(), deathbringer.GetLootRecipientGroup());
                                        loot.RemoveFlag(GameObjectFields.Flags, GameObjectFlags.Locked | GameObjectFlags.NotSelectable | GameObjectFlags.NoDespawn);
                                    }

                                    GameObject teleporter = instance.GetGameObject(TeleporterUpperSpireGUID);
                                    if (teleporter)
                                        SetTeleporterState(teleporter, true);

                                    teleporter = instance.GetGameObject(TeleporterDeathBringerGUID);
                                    if (teleporter)
                                        SetTeleporterState(teleporter, true);
                                    break;
                                }
                            case EncounterState.NotStarted:
                                {
                                    GameObject teleporter = instance.GetGameObject(TeleporterDeathBringerGUID);
                                    if (teleporter)
                                        SetTeleporterState(teleporter, true);
                                    break;
                                }
                            case EncounterState.InProgress:
                                {
                                    GameObject teleporter = instance.GetGameObject(TeleporterDeathBringerGUID);
                                    if (teleporter)
                                        SetTeleporterState(teleporter, false);
                                    break;
                                }
                            default:
                                break;
                        }
                        break;
                    case Bosses.Festergut:
                        if (state == EncounterState.Done)
                        {
                            if (GetBossState(Bosses.Rotface) == EncounterState.Done)
                            {
                                HandleGameObject(PutricideCollisionGUID, true);
                                GameObject go = instance.GetGameObject(PutricideGateGUIDs[0]);
                                if (go)
                                    go.SetGoState(GameObjectState.ActiveAlternative);

                                go = instance.GetGameObject(PutricideGateGUIDs[1]);
                                if (go)
                                    go.SetGoState(GameObjectState.ActiveAlternative);
                            }
                            else
                                HandleGameObject(PutricideGateGUIDs[0], false);
                            HandleGameObject(PutricidePipeGUIDs[0], true);
                        }
                        break;
                    case Bosses.Rotface:
                        if (state == EncounterState.Done)
                        {
                            if (GetBossState(Bosses.Festergut) == EncounterState.Done)
                            {
                                HandleGameObject(PutricideCollisionGUID, true);
                                GameObject go = instance.GetGameObject(PutricideGateGUIDs[0]);
                                if (go)
                                    go.SetGoState(GameObjectState.ActiveAlternative);

                                go = instance.GetGameObject(PutricideGateGUIDs[1]);
                                if (go)
                                    go.SetGoState(GameObjectState.ActiveAlternative);
                            }
                            else
                                HandleGameObject(PutricideGateGUIDs[1], false);
                            HandleGameObject(PutricidePipeGUIDs[1], true);
                        }
                        break;
                    case Bosses.ProfessorPutricide:
                        HandleGameObject(PlagueSigilGUID, state != EncounterState.Done);
                        if (state == EncounterState.Done)
                            CheckLichKingAvailability();
                        if (instance.IsHeroic())
                        {
                            if (state == EncounterState.Fail && HeroicAttempts != 0)
                            {
                                --HeroicAttempts;
                                DoUpdateWorldState(WorldStates.AttemptsRemaining, HeroicAttempts);
                                if (HeroicAttempts == 0)
                                {
                                    Creature putricide = instance.GetCreature(ProfessorPutricideGUID);
                                    if (putricide)
                                        putricide.DespawnOrUnsummon();
                                }
                            }
                        }
                        break;
                    case Bosses.BloodQueenLanaThel:
                        HandleGameObject(BloodwingSigilGUID, state != EncounterState.Done);
                        if (state == EncounterState.Done)
                            CheckLichKingAvailability();
                        if (instance.IsHeroic())
                        {
                            if (state == EncounterState.Fail && HeroicAttempts != 0)
                            {
                                --HeroicAttempts;
                                DoUpdateWorldState(WorldStates.AttemptsRemaining, HeroicAttempts);
                                if (HeroicAttempts == 0)
                                {
                                    Creature bq = instance.GetCreature(BloodQueenLanaThelGUID);
                                    if (bq)
                                        bq.DespawnOrUnsummon();
                                }
                            }
                        }
                        break;
                    case Bosses.ValithriaDreamwalker:
                        if (state == EncounterState.Done)
                        {
                            if (Global.PoolMgr.IsSpawnedObject<Quest>(WeeklyQuestData[8].questId[(int)instance.GetDifficultyID() & 1]))
                                instance.SummonCreature(CreatureIds.ValithriaDreamwalkerQuest, ValithriaDreamwalker.ValithriaSpawnPos);

                            GameObject teleporter = instance.GetGameObject(TeleporterSindragosaGUID);
                            if (teleporter)
                                SetTeleporterState(teleporter, true);
                        }
                        break;
                    case Bosses.Sindragosa:
                        HandleGameObject(FrostwingSigilGUID, state != EncounterState.Done);
                        if (state == EncounterState.Done)
                            CheckLichKingAvailability();
                        if (instance.IsHeroic())
                        {
                            if (state == EncounterState.Fail && HeroicAttempts != 0)
                            {
                                --HeroicAttempts;
                                DoUpdateWorldState(WorldStates.AttemptsRemaining, HeroicAttempts);
                                if (HeroicAttempts == 0)
                                {
                                    Creature sindra = instance.GetCreature(SindragosaGUID);
                                    if (sindra)
                                        sindra.DespawnOrUnsummon();
                                }
                            }
                        }
                        break;
                    case Bosses.TheLichKing:
                        {
                            // set the platform as active object to dramatically increase visibility range
                            // note: "active" gameobjects do not block grid unloading
                            GameObject precipice = instance.GetGameObject(ArthasPrecipiceGUID);
                            if (precipice)
                                precipice.setActive(state == EncounterState.InProgress);

                            GameObject platform = instance.GetGameObject(ArthasPlatformGUID);
                            if (platform)
                                platform.setActive(state == EncounterState.InProgress);

                            if (instance.IsHeroic())
                            {
                                if (state == EncounterState.Fail && HeroicAttempts != 0)
                                {
                                    --HeroicAttempts;
                                    DoUpdateWorldState(WorldStates.AttemptsRemaining, HeroicAttempts);
                                    if (HeroicAttempts == 0)
                                    {
                                        Creature theLichKing = instance.GetCreature(TheLichKingGUID);
                                        if (theLichKing)
                                            theLichKing.DespawnOrUnsummon();
                                    }
                                }
                            }

                            if (state == EncounterState.Done)
                            {
                                GameObject bolvar = instance.GetGameObject(FrozenBolvarGUID);
                                if (bolvar)
                                    bolvar.SetRespawnTime(7 * Time.Day);

                                GameObject pillars = instance.GetGameObject(PillarsChainedGUID);
                                if (pillars)
                                    pillars.SetRespawnTime(7 * Time.Day);

                                pillars = instance.GetGameObject(PillarsUnchainedGUID);
                                if (pillars)
                                    pillars.SetRespawnTime(7 * Time.Day);

                                instance.SummonCreature(CreatureIds.LadyJainaProudmooreQuest, JainaSpawnPos);
                                instance.SummonCreature(CreatureIds.MuradinBronzaBeardQuest, MuradinSpawnPos);
                                instance.SummonCreature(CreatureIds.UtherTheLightBringerQuest, UtherSpawnPos);
                                instance.SummonCreature(CreatureIds.LadySylvanasWindrunnerQuest, SylvanasSpawnPos);
                            }
                            break;
                        }
                    default:
                        break;
                }
                return true;
            }

            void SpawnGunship()
            {
                if (GunshipGUID.IsEmpty())
                {
                    SetBossState(Bosses.GunshipBattle, EncounterState.NotStarted);
                    uint gunshipEntry = TeamInInstance == Team.Horde ? GameObjectIds.OrgrimsHammer_H : GameObjectIds.TheSkybreaker_A;
                    Transport gunship = Global.TransportMgr.CreateTransport(gunshipEntry, 0, instance);
                    if (gunship)
                        GunshipGUID = gunship.GetGUID();
                }
            }

            public override void SetData(uint type, uint data)
            {
                switch (type)
                {
                    case DataTypes.BonedAchievement:
                        IsBonedEligible = data != 0 ? true : false;
                        break;
                    case DataTypes.OozeDanceAchievement:
                        IsOozeDanceEligible = data != 0 ? true : false;
                        break;
                    case DataTypes.NauseaAchievement:
                        IsNauseaEligible = data != 0 ? true : false;
                        break;
                    case DataTypes.OrbWhispererAchievement:
                        IsOrbWhispererEligible = data != 0 ? true : false;
                        break;
                    case DataTypes.SindragosaFrostwyrms:
                        FrostwyrmGUIDs.Add(data);
                        break;
                    case DataTypes.Spinestalker:
                        SpinestalkerTrash.Add(data);
                        break;
                    case DataTypes.Rimefang:
                        RimefangTrash.Add(data);
                        break;
                    case DataTypes.ColdflameJets:
                        ColdflameJetsState = (EncounterState)data;
                        if (ColdflameJetsState == EncounterState.Done)
                            SaveToDB();
                        break;
                    case DataTypes.BloodQuickeningState:
                        {
                            // skip if nothing changes
                            if (BloodQuickeningState == (EncounterState)data)
                                break;

                            // 5 is the index of Blood Quickening
                            if (!Global.PoolMgr.IsSpawnedObject<Quest>(WeeklyQuestData[5].questId[(int)instance.GetDifficultyID() & 1]))
                                break;

                            switch ((EncounterState)data)
                            {
                                case EncounterState.InProgress:
                                    _events.ScheduleEvent(TimedEvents.UpdateExecutionTime, 60000);
                                    BloodQuickeningMinutes = 30;
                                    DoUpdateWorldState(WorldStates.ShowTimer, 1);
                                    DoUpdateWorldState(WorldStates.ExecutionTime, BloodQuickeningMinutes);
                                    break;
                                case EncounterState.Done:
                                    _events.CancelEvent(TimedEvents.UpdateExecutionTime);
                                    BloodQuickeningMinutes = 0;
                                    DoUpdateWorldState(WorldStates.ShowTimer, 0);
                                    break;
                                default:
                                    break;
                            }

                            BloodQuickeningState = (EncounterState)data;
                            SaveToDB();
                            break;
                        }
                    case DataTypes.UpperSpireTeleAct:
                        UpperSpireTeleporterActiveState = (EncounterState)data;
                        if (UpperSpireTeleporterActiveState == EncounterState.Done)
                        {
                            GameObject go = instance.GetGameObject(TeleporterUpperSpireGUID);
                            if (go)
                                SetTeleporterState(go, true);
                            SaveToDB();
                        }
                        break;
                    default:
                        break;
                }
            }

            public override bool CheckAchievementCriteriaMeet(uint criteria_id, Player source, Unit target, uint miscvalue1)
            {
                switch (criteria_id)
                {
                    case AchievementCriteriaIds.Boned10n:
                    case AchievementCriteriaIds.Boned25n:
                    case AchievementCriteriaIds.Boned10h:
                    case AchievementCriteriaIds.Boned25h:
                        return IsBonedEligible;
                    case AchievementCriteriaIds.DancesWithOozes10:
                    case AchievementCriteriaIds.DancesWithOozes25:
                    case AchievementCriteriaIds.DancesWithOozes10H:
                    case AchievementCriteriaIds.DancesWithOozes25H:
                        return IsOozeDanceEligible;
                    case AchievementCriteriaIds.Nausea10:
                    case AchievementCriteriaIds.Nausea25:
                    case AchievementCriteriaIds.Nausea10H:
                    case AchievementCriteriaIds.Nausea25H:
                        return IsNauseaEligible;
                    case AchievementCriteriaIds.OrbWhisperer10:
                    case AchievementCriteriaIds.OrbWhisperer25:
                    case AchievementCriteriaIds.OrbWhisperer10H:
                    case AchievementCriteriaIds.OrbWhisperer25H:
                        return IsOrbWhispererEligible;
                    // Only one criteria for both modes, need to do it like this
                    case AchievementCriteriaIds.KillLanaThel10m:
                    case AchievementCriteriaIds.OnceBittenTwiceShy10:
                    case AchievementCriteriaIds.OnceBittenTwiceShy10V:
                        return instance.ToInstanceMap().GetMaxPlayers() == 10;
                    case AchievementCriteriaIds.KillLanaThel25m:
                    case AchievementCriteriaIds.OnceBittenTwiceShy25:
                    case AchievementCriteriaIds.OnceBittenTwiceShy25V:
                        return instance.ToInstanceMap().GetMaxPlayers() == 25;
                    default:
                        break;
                }

                return false;
            }

            public override bool CheckRequiredBosses(uint bossId, Player player = null)
            {
                if (player && player.GetSession().HasPermission(RBACPermissions.SkipCheckInstanceRequiredBosses))
                    return true;

                switch (bossId)
                {
                    case Bosses.TheLichKing:
                        if (!CheckPlagueworks(bossId))
                            return false;
                        if (!CheckCrimsonHalls(bossId))
                            return false;
                        if (!CheckFrostwingHalls(bossId))
                            return false;
                        break;
                    case Bosses.Sindragosa:
                    case Bosses.ValithriaDreamwalker:
                        if (!CheckFrostwingHalls(bossId))
                            return false;
                        break;
                    case Bosses.BloodQueenLanaThel:
                    case Bosses.BloodPrinceCouncil:
                        if (!CheckCrimsonHalls(bossId))
                            return false;
                        break;
                    case Bosses.Festergut:
                    case Bosses.Rotface:
                    case Bosses.ProfessorPutricide:
                        if (!CheckPlagueworks(bossId))
                            return false;
                        break;
                    default:
                        break;
                }

                if (!CheckLowerSpire(bossId))
                    return false;

                return true;
            }

            bool CheckPlagueworks(uint bossId)
            {
                switch (bossId)
                {
                    case Bosses.TheLichKing:
                        if (GetBossState(Bosses.ProfessorPutricide) != EncounterState.Done)
                            return false;
                        goto case Bosses.ProfessorPutricide;
                    // no break
                    case Bosses.ProfessorPutricide:
                        if (GetBossState(Bosses.Festergut) != EncounterState.Done || GetBossState(Bosses.Rotface) != EncounterState.Done)
                            return false;
                        break;
                    default:
                        break;
                }

                return true;
            }

            bool CheckCrimsonHalls(uint bossId)
            {
                switch (bossId)
                {
                    case Bosses.TheLichKing:
                        if (GetBossState(Bosses.BloodQueenLanaThel) != EncounterState.Done)
                            return false;
                        goto case Bosses.BloodQueenLanaThel;
                    // no break
                    case Bosses.BloodQueenLanaThel:
                        if (GetBossState(Bosses.BloodPrinceCouncil) != EncounterState.Done)
                            return false;
                        break;
                    default:
                        break;
                }

                return true;
            }

            bool CheckFrostwingHalls(uint bossId)
            {
                switch (bossId)
                {
                    case Bosses.TheLichKing:
                        if (GetBossState(Bosses.Sindragosa) != EncounterState.Done)
                            return false;
                        goto case Bosses.Sindragosa;
                    // no break
                    case Bosses.Sindragosa:
                        if (GetBossState(Bosses.ValithriaDreamwalker) != EncounterState.Done)
                            return false;
                        break;
                    default:
                        break;
                }

                return true;
            }

            bool CheckLowerSpire(uint bossId)
            {
                switch (bossId)
                {
                    case Bosses.TheLichKing:
                    case Bosses.Sindragosa:
                    case Bosses.BloodQueenLanaThel:
                    case Bosses.ProfessorPutricide:
                    case Bosses.ValithriaDreamwalker:
                    case Bosses.BloodPrinceCouncil:
                    case Bosses.Rotface:
                    case Bosses.Festergut:
                        if (GetBossState(Bosses.DeathbringerSaurfang) != EncounterState.Done)
                            return false;
                        goto case Bosses.DeathbringerSaurfang;
                    // no break
                    case Bosses.DeathbringerSaurfang:
                        if (GetBossState(Bosses.GunshipBattle) != EncounterState.Done)
                            return false;
                        goto case Bosses.GunshipBattle;
                    // no break
                    case Bosses.GunshipBattle:
                        if (GetBossState(Bosses.LadyDeathwhisper) != EncounterState.Done)
                            return false;
                        goto case Bosses.LadyDeathwhisper;
                    // no break
                    case Bosses.LadyDeathwhisper:
                        if (GetBossState(Bosses.LordMarrowgar) != EncounterState.Done)
                            return false;
                        break;
                }

                return true;
            }

            void CheckLichKingAvailability()
            {
                if (GetBossState(Bosses.ProfessorPutricide) == EncounterState.Done && GetBossState(Bosses.BloodQueenLanaThel) == EncounterState.Done && GetBossState(Bosses.Sindragosa) == EncounterState.Done)
                {
                    GameObject teleporter = instance.GetGameObject(TeleporterLichKingGUID);
                    if (teleporter)
                    {
                        teleporter.SetGoState(GameObjectState.Active);

                        List<Creature> stalkers = new List<Creature>();
                        teleporter.GetCreatureListWithEntryInGrid(stalkers, CreatureIds.InvisibleStalker, 100.0f);
                        if (stalkers.Empty())
                            return;

                        stalkers.Sort(new ObjectDistanceOrderPred(teleporter));
                        stalkers.FirstOrDefault().CastSpell((Unit)null, InstanceSpells.ArthasTeleporterCeremony, false);
                        stalkers.RemoveAt(0);
                        foreach (var creature in stalkers)
                            creature.GetAI().Reset();
                    }
                }
            }

            public override void WriteSaveDataMore(StringBuilder data)
            {
                data.AppendFormat("{0} {1} {2} {3} {4}", HeroicAttempts, ColdflameJetsState, BloodQuickeningState, BloodQuickeningMinutes, UpperSpireTeleporterActiveState);
            }

            public override void ReadSaveDataMore(StringArguments data)
            {
                HeroicAttempts = data.NextUInt32();

                EncounterState temp = (EncounterState)data.NextUInt32();
                if (temp == EncounterState.InProgress)
                    ColdflameJetsState = EncounterState.NotStarted;
                else
                    ColdflameJetsState = temp != 0 ? EncounterState.Done : EncounterState.NotStarted;

                temp = (EncounterState)data.NextUInt32();
                BloodQuickeningState = temp != 0 ? EncounterState.Done : EncounterState.NotStarted;   // DONE means finished (not success/fail)
                BloodQuickeningMinutes = data.NextUInt16();

                temp = (EncounterState)data.NextUInt32();
                UpperSpireTeleporterActiveState = temp != 0 ? EncounterState.Done : EncounterState.NotStarted;
            }

            public override void Update(uint diff)
            {
                if (BloodQuickeningState != EncounterState.InProgress && GetBossState(Bosses.TheLichKing) != EncounterState.InProgress && GetBossState(Bosses.GunshipBattle) != EncounterState.Fail)
                    return;

                _events.Update(diff);

                _events.ExecuteEvents(eventId =>
                {
                    switch (eventId)
                    {
                        case TimedEvents.UpdateExecutionTime:
                            {
                                --BloodQuickeningMinutes;
                                if (BloodQuickeningMinutes != 0)
                                {
                                    _events.ScheduleEvent(TimedEvents.UpdateExecutionTime, 60000);
                                    DoUpdateWorldState(WorldStates.ShowTimer, 1);
                                    DoUpdateWorldState(WorldStates.ExecutionTime, BloodQuickeningMinutes);
                                }
                                else
                                {
                                    BloodQuickeningState = EncounterState.Done;
                                    DoUpdateWorldState(WorldStates.ShowTimer, 0);
                                    Creature bq = instance.GetCreature(BloodQueenLanaThelGUID);
                                    if (bq)
                                        bq.GetAI().DoAction(Actions.KillMinchar);
                                }
                                SaveToDB();
                                break;
                            }
                        case TimedEvents.QuakeShatter:
                            {
                                GameObject platform = instance.GetGameObject(ArthasPlatformGUID);
                                if (platform)
                                    platform.SetDestructibleState(GameObjectDestructibleState.Damaged);

                                GameObject edge = instance.GetGameObject(FrozenThroneEdgeGUID);
                                if (edge)
                                    edge.SetGoState(GameObjectState.Active);

                                GameObject wind = instance.GetGameObject(FrozenThroneWindGUID);
                                if (wind)
                                    wind.SetGoState(GameObjectState.Ready);

                                GameObject warning = instance.GetGameObject(FrozenThroneWarningGUID);
                                if (warning)
                                    warning.SetGoState(GameObjectState.Ready);

                                Creature theLichKing = instance.GetCreature(TheLichKingGUID);
                                if (theLichKing)
                                    theLichKing.GetAI().DoAction(Actions.RestoreLight);
                                break;
                            }
                        case TimedEvents.RebuildPlatform:
                            GameObject platform1 = instance.GetGameObject(ArthasPlatformGUID);
                            if (platform1)
                                platform1.SetDestructibleState(GameObjectDestructibleState.Rebuilding);

                            GameObject edge1 = instance.GetGameObject(FrozenThroneEdgeGUID);
                            if (edge1)
                                edge1.SetGoState(GameObjectState.Ready);

                            GameObject wind1 = instance.GetGameObject(FrozenThroneWindGUID);
                            if (wind1)
                                wind1.SetGoState(GameObjectState.Active);
                            break;
                        case TimedEvents.RespawnGunship:
                            SpawnGunship();
                            break;
                        default:
                            break;
                    }
                });
            }

            public override void ProcessEvent(WorldObject source, uint eventId)
            {
                switch (eventId)
                {
                    case EventIds.EnemyGunshipDespawn:
                        if (GetBossState(Bosses.GunshipBattle) == EncounterState.Done)
                            source.AddObjectToRemoveList();
                        break;
                    case EventIds.EnemyGunshipCombat:
                        Creature captain = source.FindNearestCreature(TeamInInstance == Team.Horde ? CreatureIds.IGBHighOverlordSaurfang : CreatureIds.IGBMuradinBrozebeard, 100.0f);
                        if (captain)
                            captain.GetAI().DoAction(Actions.EnemyGunshipTalk);
                        goto case EventIds.PlayersGunshipSpawn;
                    // no break;
                    case EventIds.PlayersGunshipSpawn:
                    case EventIds.PlayersGunshipCombat:
                        GameObject go = source.ToGameObject();
                        if (go)
                        {
                            Transport transport = go.ToTransport();
                            if (transport)
                                transport.EnableMovement(false);
                        }
                        break;
                    case EventIds.PlayersGunshipSaurfang:
                        Creature _captain = source.FindNearestCreature(TeamInInstance == Team.Horde ? CreatureIds.IGBHighOverlordSaurfang : CreatureIds.IGBMuradinBrozebeard, 100.0f);
                        if (_captain)
                            _captain.GetAI().DoAction(Actions.ExitShip);
                        GameObject _go = source.ToGameObject();
                        if (_go)
                        {
                            Transport transport = _go.ToTransport();
                            if (transport)
                                transport.EnableMovement(false);
                        }
                        break;
                    case EventIds.Quake:
                        GameObject warning = instance.GetGameObject(FrozenThroneWarningGUID);
                        if (warning)
                            warning.SetGoState(GameObjectState.Active);
                        _events.ScheduleEvent(TimedEvents.QuakeShatter, 5000);
                        break;
                    case EventIds.SeconsRemorselessWinter:
                        GameObject platform = instance.GetGameObject(ArthasPlatformGUID);
                        if (platform)
                        {
                            platform.SetDestructibleState(GameObjectDestructibleState.Destroyed);
                            _events.ScheduleEvent(TimedEvents.RebuildPlatform, 1500);
                        }
                        break;
                    case EventIds.TeleportToFrostmourne: // Harvest Soul (normal mode)
                        Creature terenas = instance.SummonCreature(CreatureIds.TerenasMenethilFrostmourne, TheLichKing.TerenasSpawn, null, 63000);
                        if (terenas)
                        {
                            terenas.GetAI().DoAction(Actions.FrostmourneIntro);
                            List<Creature> triggers = new List<Creature>();
                            terenas.GetCreatureListWithEntryInGrid(triggers, CreatureIds.WorldTriggerInfiniteAoi, 100.0f);
                            if (!triggers.Empty())
                            {
                                triggers.Sort(new ObjectDistanceOrderPred(terenas, false));
                                Unit visual = triggers.FirstOrDefault();
                                visual.CastSpell(visual, InstanceSpells.FrostmourneTeleportVisual, true);
                            }
                            Creature warden = instance.SummonCreature(CreatureIds.SpiritWarden, TheLichKing.SpiritWardenSpawn, null, 63000);
                            if (warden)
                            {
                                terenas.GetAI().AttackStart(warden);
                                warden.AddThreat(terenas, 300000.0f);
                            }
                        }
                        break;
                }
            }

            ObjectGuid LadyDeathwisperElevatorGUID;
            ObjectGuid GunshipGUID;
            ObjectGuid EnemyGunshipGUID;
            ObjectGuid GunshipArmoryGUID;
            ObjectGuid DeathbringerSaurfangGUID;
            ObjectGuid DeathbringerSaurfangDoorGUID;
            ObjectGuid DeathbringerSaurfangEventGUID;   // Muradin Bronzebeard or High Overlord Saurfang
            ObjectGuid DeathbringersCacheGUID;
            ObjectGuid TeleporterLichKingGUID;
            ObjectGuid TeleporterUpperSpireGUID;
            ObjectGuid TeleporterLightsHammerGUID;
            ObjectGuid TeleporterRampartsGUID;
            ObjectGuid TeleporterDeathBringerGUID;
            ObjectGuid TeleporterOratoryGUID;
            ObjectGuid TeleporterSindragosaGUID;
            ObjectGuid PlagueSigilGUID;
            ObjectGuid BloodwingSigilGUID;
            ObjectGuid FrostwingSigilGUID;
            ObjectGuid[] PutricidePipeGUIDs = new ObjectGuid[2];
            ObjectGuid[] PutricideGateGUIDs = new ObjectGuid[2];
            ObjectGuid PutricideCollisionGUID;
            ObjectGuid FestergutGUID;
            ObjectGuid RotfaceGUID;
            ObjectGuid ProfessorPutricideGUID;
            ObjectGuid PutricideTableGUID;
            ObjectGuid[] BloodCouncilGUIDs = new ObjectGuid[3];
            ObjectGuid BloodCouncilControllerGUID;
            ObjectGuid BloodQueenLanaThelGUID;
            ObjectGuid CrokScourgebaneGUID;
            ObjectGuid[] CrokCaptainGUIDs = new ObjectGuid[4];
            ObjectGuid SisterSvalnaGUID;
            ObjectGuid ValithriaDreamwalkerGUID;
            ObjectGuid ValithriaLichKingGUID;
            ObjectGuid ValithriaTriggerGUID;
            ObjectGuid SindragosaGUID;
            ObjectGuid SpinestalkerGUID;
            ObjectGuid RimefangGUID;
            ObjectGuid TheLichKingGUID;
            ObjectGuid HighlordTirionFordringGUID;
            ObjectGuid TerenasMenethilGUID;
            ObjectGuid ArthasPlatformGUID;
            ObjectGuid ArthasPrecipiceGUID;
            ObjectGuid FrozenThroneEdgeGUID;
            ObjectGuid FrozenThroneWindGUID;
            ObjectGuid FrozenThroneWarningGUID;
            ObjectGuid FrozenBolvarGUID;
            ObjectGuid PillarsChainedGUID;
            ObjectGuid PillarsUnchainedGUID;
            Team TeamInInstance;
            EncounterState ColdflameJetsState;
            EncounterState UpperSpireTeleporterActiveState;
            List<ulong> FrostwyrmGUIDs = new List<ulong>();
            List<ulong> SpinestalkerTrash = new List<ulong>();
            List<ulong> RimefangTrash = new List<ulong>();
            EncounterState BloodQuickeningState;
            uint HeroicAttempts;
            ushort BloodQuickeningMinutes;
            bool IsBonedEligible;
            bool IsOozeDanceEligible;
            bool IsNauseaEligible;
            bool IsOrbWhispererEligible;
        }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_icecrown_citadel_InstanceMapScript(map);
        }
    }
}
