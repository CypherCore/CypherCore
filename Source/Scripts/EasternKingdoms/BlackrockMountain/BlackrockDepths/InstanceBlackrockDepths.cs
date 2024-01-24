// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.IO;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockDepths
{
    struct CreatureIds
    {
        public const uint Emperor = 9019;
        public const uint Phalanx = 9502;
        public const uint Angerrel = 9035;
        public const uint Doperel = 9040;
        public const uint Haterel = 9034;
        public const uint Vilerel = 9036;
        public const uint Seethrel = 9038;
        public const uint Gloomrel = 9037;
        public const uint Doomrel = 9039;
        public const uint Magmus = 9938;
        public const uint Moira = 8929;
        public const uint Coren = 23872;
    }

    struct GameObjectIds
    {
        public const uint Arena1 = 161525;
        public const uint Arena2 = 161522;
        public const uint Arena3 = 161524;
        public const uint Arena4 = 161523;
        public const uint ShadowLock = 161460;
        public const uint ShadowMechanism = 161461;
        public const uint ShadowGiantDoor = 157923;
        public const uint ShadowDummy = 161516;
        public const uint BarKegShot = 170607;
        public const uint BarKegTrap = 171941;
        public const uint BarDoor = 170571;
        public const uint TombEnter = 170576;
        public const uint TombExit = 170577;
        public const uint Lyceum = 170558;
        public const uint SfN = 174745; // Shadowforge Brazier North
        public const uint SfS = 174744; // Shadowforge Brazier South
        public const uint GolemRoomN = 170573; // Magmus door North
        public const uint GolemRoomS = 170574; // Magmus door Soutsh
        public const uint ThroneRoom = 170575; // Throne door
        public const uint SpectralChalice = 164869;
        public const uint ChestSeven = 169243;
    }

    struct DataTypes
    {
        public const uint TypeRingOfLaw = 1;
        public const uint TypeVault = 2;
        public const uint TypeBar = 3;
        public const uint TypeTombOfSeven = 4;
        public const uint TypeLyceum = 5;
        public const uint TypeIronHall = 6;

        public const uint DataEmperor = 10;
        public const uint DataPhalanx = 11;

        public const uint DataArena1 = 12;
        public const uint DataArena2 = 13;
        public const uint DataArena3 = 14;
        public const uint DataArena4 = 15;

        public const uint DataGoBarKeg = 16;
        public const uint DataGoBarKegTrap = 17;
        public const uint DataGoBarDoor = 18;
        public const uint DataGoChalice = 19;

        public const uint DataGhostkill = 20;
        public const uint DataEvenstarter = 21;

        public const uint DataGolemDoorN = 22;
        public const uint DataGolemDoorS = 23;

        public const uint DataThroneDoor = 24;

        public const uint DataSfBrazierN = 25;
        public const uint DataSfBrazierS = 26;
        public const uint DataMoira = 27;
        public const uint DataCoren = 28;
    }

    struct MiscConst
    {
        public const uint TimerTombOfTheSeven = 15000;
        public const uint MaxEncounter = 6;
        public const uint TombOfSevenBossNum = 7;
    }

    class instance_blackrock_depths : InstanceMapScript
    {
        public instance_blackrock_depths() : base(nameof(instance_blackrock_depths), 230) { }

        class instance_blackrock_depths_InstanceMapScript : InstanceScript
        {
            ObjectGuid EmperorGUID;
            ObjectGuid PhalanxGUID;
            ObjectGuid MagmusGUID;
            ObjectGuid MoiraGUID;
            ObjectGuid CorenGUID;

            ObjectGuid GoArena1GUID;
            ObjectGuid GoArena2GUID;
            ObjectGuid GoArena3GUID;
            ObjectGuid GoArena4GUID;
            ObjectGuid GoShadowLockGUID;
            ObjectGuid GoShadowMechGUID;
            ObjectGuid GoShadowGiantGUID;
            ObjectGuid GoShadowDummyGUID;
            ObjectGuid GoBarKegGUID;
            ObjectGuid GoBarKegTrapGUID;
            ObjectGuid GoBarDoorGUID;
            ObjectGuid GoTombEnterGUID;
            ObjectGuid GoTombExitGUID;
            ObjectGuid GoLyceumGUID;
            ObjectGuid GoSFSGUID;
            ObjectGuid GoSFNGUID;
            ObjectGuid GoGolemNGUID;
            ObjectGuid GoGolemSGUID;
            ObjectGuid GoThroneGUID;
            ObjectGuid GoChestGUID;
            ObjectGuid GoSpectralChaliceGUID;

            uint BarAleCount;
            uint GhostKillCount;
            ObjectGuid[] TombBossGUIDs = new ObjectGuid[MiscConst.TombOfSevenBossNum];
            ObjectGuid TombEventStarterGUID;
            uint TombTimer;
            uint TombEventCounter;

            public instance_blackrock_depths_InstanceMapScript(InstanceMap map) : base(map)
            {
                SetHeaders("BRD");
                SetBossNumber(MiscConst.MaxEncounter);

                BarAleCount = 0;
                GhostKillCount = 0;
                TombTimer = MiscConst.TimerTombOfTheSeven;
                TombEventCounter = 0;
            }

            public override void OnCreatureCreate(Creature creature)
            {
                switch (creature.GetEntry())
                {
                    case CreatureIds.Emperor: EmperorGUID = creature.GetGUID(); break;
                    case CreatureIds.Phalanx: PhalanxGUID = creature.GetGUID(); break;
                    case CreatureIds.Moira: MoiraGUID = creature.GetGUID(); break;
                    case CreatureIds.Coren: CorenGUID = creature.GetGUID(); break;
                    case CreatureIds.Doomrel: TombBossGUIDs[0] = creature.GetGUID(); break;
                    case CreatureIds.Doperel: TombBossGUIDs[1] = creature.GetGUID(); break;
                    case CreatureIds.Haterel: TombBossGUIDs[2] = creature.GetGUID(); break;
                    case CreatureIds.Vilerel: TombBossGUIDs[3] = creature.GetGUID(); break;
                    case CreatureIds.Seethrel: TombBossGUIDs[4] = creature.GetGUID(); break;
                    case CreatureIds.Gloomrel: TombBossGUIDs[5] = creature.GetGUID(); break;
                    case CreatureIds.Angerrel: TombBossGUIDs[6] = creature.GetGUID(); break;
                    case CreatureIds.Magmus:
                        MagmusGUID = creature.GetGUID();
                        if (!creature.IsAlive())
                            HandleGameObject(GetGuidData(DataTypes.DataThroneDoor), true); // if Magmus is dead open door to last boss
                        break;
                }
            }

            public override void OnGameObjectCreate(GameObject go)
            {
                switch (go.GetEntry())
                {
                    case GameObjectIds.Arena1: GoArena1GUID = go.GetGUID(); break;
                    case GameObjectIds.Arena2: GoArena2GUID = go.GetGUID(); break;
                    case GameObjectIds.Arena3: GoArena3GUID = go.GetGUID(); break;
                    case GameObjectIds.Arena4: GoArena4GUID = go.GetGUID(); break;
                    case GameObjectIds.ShadowLock: GoShadowLockGUID = go.GetGUID(); break;
                    case GameObjectIds.ShadowMechanism: GoShadowMechGUID = go.GetGUID(); break;
                    case GameObjectIds.ShadowGiantDoor: GoShadowGiantGUID = go.GetGUID(); break;
                    case GameObjectIds.ShadowDummy: GoShadowDummyGUID = go.GetGUID(); break;
                    case GameObjectIds.BarKegShot: GoBarKegGUID = go.GetGUID(); break;
                    case GameObjectIds.BarKegTrap: GoBarKegTrapGUID = go.GetGUID(); break;
                    case GameObjectIds.BarDoor: GoBarDoorGUID = go.GetGUID(); break;
                    case GameObjectIds.TombEnter: GoTombEnterGUID = go.GetGUID(); break;
                    case GameObjectIds.TombExit:
                        GoTombExitGUID = go.GetGUID();
                        if (GhostKillCount >= MiscConst.TombOfSevenBossNum)
                            HandleGameObject(ObjectGuid.Empty, true, go);
                        else
                            HandleGameObject(ObjectGuid.Empty, false, go);
                        break;
                    case GameObjectIds.Lyceum: GoLyceumGUID = go.GetGUID(); break;
                    case GameObjectIds.SfS: GoSFSGUID = go.GetGUID(); break;
                    case GameObjectIds.SfN: GoSFNGUID = go.GetGUID(); break;
                    case GameObjectIds.GolemRoomN: GoGolemNGUID = go.GetGUID(); break;
                    case GameObjectIds.GolemRoomS: GoGolemSGUID = go.GetGUID(); break;
                    case GameObjectIds.ThroneRoom: GoThroneGUID = go.GetGUID(); break;
                    case GameObjectIds.ChestSeven: GoChestGUID = go.GetGUID(); break;
                    case GameObjectIds.SpectralChalice: GoSpectralChaliceGUID = go.GetGUID(); break;
                }
            }

            public override void SetGuidData(uint type, ObjectGuid data)
            {
                switch (type)
                {
                    case DataTypes.DataEvenstarter:
                        TombEventStarterGUID = data;
                        if (TombEventStarterGUID.IsEmpty())
                            TombOfSevenReset();//reset
                        else
                            TombOfSevenStart();//start
                        break;
                }
            }

            public override void SetData(uint type, uint data)
            {
                switch (type)
                {
                    case DataTypes.TypeRingOfLaw:
                        SetBossState(0, (EncounterState)data);
                        break;
                    case DataTypes.TypeVault:
                        SetBossState(1, (EncounterState)data);
                        break;
                    case DataTypes.TypeBar:
                        if (data == (uint)EncounterState.Special)
                            ++BarAleCount;
                        else
                            SetBossState(2, (EncounterState)data);
                        break;
                    case DataTypes.TypeTombOfSeven:
                        SetBossState(3, (EncounterState)data);
                        break;
                    case DataTypes.TypeLyceum:
                        SetBossState(4, (EncounterState)data);
                        break;
                    case DataTypes.TypeIronHall:
                        SetBossState(5, (EncounterState)data);
                        break;
                    case DataTypes.DataGhostkill:
                        GhostKillCount += data;
                        break;
                }
            }

            public override uint GetData(uint type)
            {
                switch (type)
                {
                    case DataTypes.TypeRingOfLaw:
                        return (uint)GetBossState(0);
                    case DataTypes.TypeVault:
                        return (uint)GetBossState(1);
                    case DataTypes.TypeBar:
                        if (GetBossState(2) == EncounterState.InProgress && BarAleCount == 3)
                            return (uint)EncounterState.Special;
                        else
                            return (uint)GetBossState(2);
                    case DataTypes.TypeTombOfSeven:
                        return (uint)GetBossState(3);
                    case DataTypes.TypeLyceum:
                        return (uint)GetBossState(4);
                    case DataTypes.TypeIronHall:
                        return (uint)GetBossState(5);
                    case DataTypes.DataGhostkill:
                        return GhostKillCount;
                }
                return 0;
            }

            public override ObjectGuid GetGuidData(uint data)
            {
                switch (data)
                {
                    case DataTypes.DataEmperor:
                        return EmperorGUID;
                    case DataTypes.DataPhalanx:
                        return PhalanxGUID;
                    case DataTypes.DataMoira:
                        return MoiraGUID;
                    case DataTypes.DataCoren:
                        return CorenGUID;
                    case DataTypes.DataArena1:
                        return GoArena1GUID;
                    case DataTypes.DataArena2:
                        return GoArena2GUID;
                    case DataTypes.DataArena3:
                        return GoArena3GUID;
                    case DataTypes.DataArena4:
                        return GoArena4GUID;
                    case DataTypes.DataGoBarKeg:
                        return GoBarKegGUID;
                    case DataTypes.DataGoBarKegTrap:
                        return GoBarKegTrapGUID;
                    case DataTypes.DataGoBarDoor:
                        return GoBarDoorGUID;
                    case DataTypes.DataEvenstarter:
                        return TombEventStarterGUID;
                    case DataTypes.DataSfBrazierN:
                        return GoSFNGUID;
                    case DataTypes.DataSfBrazierS:
                        return GoSFSGUID;
                    case DataTypes.DataThroneDoor:
                        return GoThroneGUID;
                    case DataTypes.DataGolemDoorN:
                        return GoGolemNGUID;
                    case DataTypes.DataGolemDoorS:
                        return GoGolemSGUID;
                    case DataTypes.DataGoChalice:
                        return GoSpectralChaliceGUID;
                }
                return ObjectGuid.Empty;
            }

            void TombOfSevenEvent()
            {
                if (GhostKillCount < MiscConst.TombOfSevenBossNum && !TombBossGUIDs[TombEventCounter].IsEmpty())
                {
                    Creature boss = instance.GetCreature(TombBossGUIDs[TombEventCounter]);
                    if (boss != null)
                    {
                        boss.SetFaction((uint)FactionTemplates.DarkIronDwarves);
                        boss.SetImmuneToPC(false);
                        Unit target = boss.SelectNearestTarget(500);
                        if (target != null)
                            boss.GetAI().AttackStart(target);
                    }
                }
            }

            void TombOfSevenReset()
            {
                HandleGameObject(GoTombExitGUID, false);//event reseted, close exit door
                HandleGameObject(GoTombEnterGUID, true);//event reseted, open entrance door
                for (byte i = 0; i < MiscConst.TombOfSevenBossNum; ++i)
                {
                    Creature boss = instance.GetCreature(TombBossGUIDs[i]);
                    if (boss != null)
                    {
                        if (!boss.IsAlive())
                            boss.Respawn();
                        else
                            boss.SetFaction((uint)FactionTemplates.Friendly);
                    }
                }
                GhostKillCount = 0;
                TombEventStarterGUID.Clear();
                TombEventCounter = 0;
                TombTimer = MiscConst.TimerTombOfTheSeven;
                SetData(DataTypes.TypeTombOfSeven, (uint)EncounterState.NotStarted);
            }

            void TombOfSevenStart()
            {
                HandleGameObject(GoTombExitGUID, false);//event started, close exit door
                HandleGameObject(GoTombEnterGUID, false);//event started, close entrance door
                SetData(DataTypes.TypeTombOfSeven, (uint)EncounterState.InProgress);
            }

            void TombOfSevenEnd()
            {
                DoRespawnGameObject(GoChestGUID, TimeSpan.FromHours(24));
                HandleGameObject(GoTombExitGUID, true);//event done, open exit door
                HandleGameObject(GoTombEnterGUID, true);//event done, open entrance door
                TombEventStarterGUID.Clear();
                SetData(DataTypes.TypeTombOfSeven, (uint)EncounterState.Done);
            }

            public override void Update(uint diff)
            {
                if (!TombEventStarterGUID.IsEmpty() && GhostKillCount < MiscConst.TombOfSevenBossNum)
                {
                    if (TombTimer <= diff)
                    {
                        TombTimer = MiscConst.TimerTombOfTheSeven;
                        if (TombEventCounter < MiscConst.TombOfSevenBossNum)
                        {
                            TombOfSevenEvent();
                            ++TombEventCounter;
                        }

                        // Check Killed bosses
                        for (byte i = 0; i < MiscConst.TombOfSevenBossNum; ++i)
                        {
                            Creature boss = instance.GetCreature(TombBossGUIDs[i]);
                            if (boss != null)
                            {
                                if (!boss.IsAlive())
                                {
                                    GhostKillCount = i + 1u;
                                }
                            }
                        }
                    }
                    else TombTimer -= diff;
                }
                if (GhostKillCount >= MiscConst.TombOfSevenBossNum && !TombEventStarterGUID.IsEmpty())
                    TombOfSevenEnd();
            }
        }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_blackrock_depths_InstanceMapScript(map);
        }
    }
}

