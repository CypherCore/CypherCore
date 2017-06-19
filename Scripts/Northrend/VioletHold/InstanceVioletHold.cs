using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting;
using Game.Maps;
using Game.WorldEntities;
using Framework.Util;
using Framework.Constants;
using Game.Spells;
using Game;

namespace Scripts.Northrend.VioletHold
{
    class instance_violet_hold : InstanceMapScript
    {
        public instance_violet_hold() : base("instance_violet_hold", 608) { }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_violet_hold_InstanceMapScript(map);
        }

        class instance_violet_hold_InstanceMapScript : InstanceScript
        {
            public instance_violet_hold_InstanceMapScript(Map map) : base(map) { }

            public override void Initialize()
            {
                uiMoragg = 0;
                uiErekem = 0;
                uiIchoron = 0;
                uiLavanthor = 0;
                uiXevozz = 0;
                uiZuramat = 0;
                uiCyanigosa = 0;
                uiSinclari = 0;

                uiMoraggCell = 0;
                uiErekemCell = 0;
                uiErekemGuard[0] = 0;
                uiErekemGuard[1] = 0;
                uiIchoronCell = 0;
                uiLavanthorCell = 0;
                uiXevozzCell = 0;
                uiZuramatCell = 0;
                uiMainDoor = 0;
                uiTeleportationPortal = 0;
                uiSaboteurPortal = 0;

                trashMobs.Clear();

                uiRemoveNpc = 0;

                uiDoorIntegrity = 100;

                uiWaveCount = 0;
                uiLocation = (byte)RandomHelper.URand(0, 5);
                uiFirstBoss = 0;
                uiSecondBoss = 0;
                uiCountErekemGuards = 0;
                uiCountActivationCrystals = 0;
                uiCyanigosaEventPhase = 1;

                uiActivationTimer = 5000;
                uiDoorSpellTimer = 2000;
                uiCyanigosaEventTimer = 3 * Time.InMilliseconds;

                bActive = false;
                bIsDoorSpellCast = false;
                bCrystalActivated = false;
                defenseless = true;
                uiMainEventPhase = EncounterState.NotStarted;
            }

            public override bool IsEncounterInProgress()
            {
                for (byte i = 0; i < MAX_ENCOUNTER; ++i)
                    if ((EncounterState)m_auiEncounter[i] == EncounterState.InProgress)
                        return true;

                return false;
            }

            public override void OnCreatureCreate(Creature creature)
            {
                switch (creature.GetEntry())
                {
                    case CREATURE_XEVOZZ:
                        uiXevozz = creature.GetGUID();
                        break;
                    case CREATURE_LAVANTHOR:
                        uiLavanthor = creature.GetGUID();
                        break;
                    case CREATURE_ICHORON:
                        uiIchoron = creature.GetGUID();
                        break;
                    case CREATURE_ZURAMAT:
                        uiZuramat = creature.GetGUID();
                        break;
                    case CREATURE_EREKEM:
                        uiErekem = creature.GetGUID();
                        break;
                    case CREATURE_EREKEM_GUARD:
                        if (uiCountErekemGuards < 2)
                        {
                            uiErekemGuard[uiCountErekemGuards++] = creature.GetGUID();
                            creature.SetFlag(UnitFields.Flags, UnitFlags.ImmuneToPc | UnitFlags.NonAttackable);
                        }
                        break;
                    case CREATURE_MORAGG:
                        uiMoragg = creature.GetGUID();
                        break;
                    case CREATURE_CYANIGOSA:
                        uiCyanigosa = creature.GetGUID();
                        creature.SetFlag(UnitFields.Flags, UnitFlags.ImmuneToPc | UnitFlags.NonAttackable);
                        break;
                    case CREATURE_SINCLARI:
                        uiSinclari = creature.GetGUID();
                        break;
                }

                if (creature.GetGUID() == uiFirstBoss || creature.GetGUID() == uiSecondBoss)
                {
                    creature.AllLootRemovedFromCorpse();
                    creature.RemoveLootMode(1);
                }
            }

            public override void OnGameObjectCreate(GameObject go)
            {
                switch (go.GetEntry())
                {
                    case GO_EREKEM_GUARD_1_DOOR:
                        uiErekemLeftGuardCell = go.GetGUID();
                        break;
                    case GO_EREKEM_GUARD_2_DOOR:
                        uiErekemRightGuardCell = go.GetGUID();
                        break;
                    case GO_EREKEM_DOOR:
                        uiErekemCell = go.GetGUID();
                        break;
                    case GO_ZURAMAT_DOOR:
                        uiZuramatCell = go.GetGUID();
                        break;
                    case GO_LAVANTHOR_DOOR:
                        uiLavanthorCell = go.GetGUID();
                        break;
                    case GO_MORAGG_DOOR:
                        uiMoraggCell = go.GetGUID();
                        break;
                    case GO_ICHORON_DOOR:
                        uiIchoronCell = go.GetGUID();
                        break;
                    case GO_XEVOZZ_DOOR:
                        uiXevozzCell = go.GetGUID();
                        break;
                    case GO_MAIN_DOOR:
                        uiMainDoor = go.GetGUID();
                        break;
                    case GO_ACTIVATION_CRYSTAL:
                        if (uiCountActivationCrystals < 4)
                            uiActivationCrystal[uiCountActivationCrystals++] = go.GetGUID();
                        break;
                }
            }

            public override void SetData(uint type, uint data)
            {
                switch (type)
                {
                    case DATA_1ST_BOSS_EVENT:
                        UpdateEncounterState(EncounterCreditType.KillCreature, CREATURE_EREKEM, null);
                        m_auiEncounter[0] = (byte)data;
                        if ((EncounterState)data == EncounterState.Done)
                            SaveToDB();
                        break;
                    case DATA_2ND_BOSS_EVENT:
                        UpdateEncounterState(EncounterCreditType.KillCreature, CREATURE_MORAGG, null);
                        m_auiEncounter[1] = (byte)data;
                        if ((EncounterState)data == EncounterState.Done)
                            SaveToDB();
                        break;
                    case DATA_CYANIGOSA_EVENT:
                        m_auiEncounter[2] = (byte)data;
                        if ((EncounterState)data == EncounterState.Done)
                        {
                            SaveToDB();
                            uiMainEventPhase = EncounterState.EncounterState.Done;
                            if (GameObject pMainDoor = instance.GetGameObject(uiMainDoor))
                                pMainDoor.SetGoState(GameObjectState.Active);
                        }
                        break;
                    case DATA_WAVE_COUNT:
                        uiWaveCount = (byte)data;
                        bActive = true;
                        break;
                    case DATA_REMOVE_NPC:
                        uiRemoveNpc = (byte)data;
                        break;
                    case DATA_PORTAL_LOCATION:
                        uiLocation = (byte)data;
                        break;
                    case DATA_DOOR_INTEGRITY:
                        uiDoorIntegrity = (byte)data;
                        defenseless = false;
                        DoUpdateWorldState(WORLD_STATE_VH_PRISON_STATE, uiDoorIntegrity);
                        break;
                    case DATA_NPC_PRESENCE_AT_DOOR_ADD:
                        NpcAtDoorCastingList.Add((byte)data);
                        break;
                    case DATA_NPC_PRESENCE_AT_DOOR_REMOVE:
                        if (!NpcAtDoorCastingList.Empty())
                            NpcAtDoorCastingList.RemoveAt(0);//.pop_back();
                        break;
                    case DATA_MAIN_DOOR:
                        if (GameObject pMainDoor = instance.GetGameObject(uiMainDoor))
                        {
                            switch ((GameObjectState)data)
                            {
                                case GameObjectState.Active:
                                case GameObjectState.Ready:
                                case GameObjectState.ActiveAlternative:
                                    pMainDoor.SetGoState((GameObjectState)data);
                                    break;
                            }
                        }
                        break;
                    case DATA_START_BOSS_ENCOUNTER:
                        switch (uiWaveCount)
                        {
                            case 6:
                                StartBossEncounter(uiFirstBoss);
                                break;
                            case 12:
                                StartBossEncounter(uiSecondBoss);
                                break;
                        }
                        break;
                    case DATA_ACTIVATE_CRYSTAL:
                        ActivateCrystal();
                        break;
                    case DATA_MAIN_EVENT_PHASE:
                        uiMainEventPhase = (EncounterState)data;
                        if ((EncounterState)data == EncounterState.InProgress) // Start event
                        {
                            if (GameObject mainDoor = instance.GetGameObject(uiMainDoor))
                                mainDoor.SetGoState(GameObjectState.Ready);
                            uiWaveCount = 1;
                            bActive = true;
                            for (int i = 0; i < 4; ++i)
                            {
                                if (GameObject crystal = instance.GetGameObject(uiActivationCrystal[i]))
                                    crystal.RemoveFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                            }
                            uiRemoveNpc = 0; // might not have been reset after a wipe on a boss.
                        }
                        break;
                }
            }

            public override void SetData64(uint type, ulong data)
            {
                switch (type)
                {
                    case DATA_ADD_TRASH_MOB:
                        trashMobs.Add(data);
                        break;
                    case DATA_DEL_TRASH_MOB:
                        trashMobs.Remove(data);
                        break;
                }
            }

            public override uint GetData(uint type)
            {
                switch (type)
                {
                    case DATA_1ST_BOSS_EVENT:
                        return m_auiEncounter[0];
                    case DATA_2ND_BOSS_EVENT:
                        return m_auiEncounter[1];
                    case DATA_CYANIGOSA_EVENT:
                        return m_auiEncounter[2];
                    case DATA_WAVE_COUNT:
                        return uiWaveCount;
                    case DATA_REMOVE_NPC:
                        return uiRemoveNpc;
                    case DATA_PORTAL_LOCATION:
                        return uiLocation;
                    case DATA_DOOR_INTEGRITY:
                        return uiDoorIntegrity;
                    case DATA_NPC_PRESENCE_AT_DOOR:
                        return (uint)NpcAtDoorCastingList.Count;
                    case DATA_FIRST_BOSS:
                        return uiFirstBoss;
                    case DATA_SECOND_BOSS:
                        return uiSecondBoss;
                    case DATA_MAIN_EVENT_PHASE:
                        return (uint)uiMainEventPhase;
                    case DATA_DEFENSELESS:
                        return (uint)(defenseless ? 1 : 0);
                }

                return 0;
            }

            public override ulong GetData64(uint identifier)
            {
                switch (identifier)
                {
                    case DATA_MORAGG: return uiMoragg;
                    case DATA_EREKEM: return uiErekem;
                    case DATA_EREKEM_GUARD_1: return uiErekemGuard[0];
                    case DATA_EREKEM_GUARD_2: return uiErekemGuard[1];
                    case DATA_ICHORON: return uiIchoron;
                    case DATA_LAVANTHOR: return uiLavanthor;
                    case DATA_XEVOZZ: return uiXevozz;
                    case DATA_ZURAMAT: return uiZuramat;
                    case DATA_CYANIGOSA: return uiCyanigosa;
                    case DATA_MORAGG_CELL: return uiMoraggCell;
                    case DATA_EREKEM_CELL: return uiErekemCell;
                    case DATA_EREKEM_RIGHT_GUARD_CELL: return uiErekemRightGuardCell;
                    case DATA_EREKEM_LEFT_GUARD_CELL: return uiErekemLeftGuardCell;
                    case DATA_ICHORON_CELL: return uiIchoronCell;
                    case DATA_LAVANTHOR_CELL: return uiLavanthorCell;
                    case DATA_XEVOZZ_CELL: return uiXevozzCell;
                    case DATA_ZURAMAT_CELL: return uiZuramatCell;
                    case DATA_MAIN_DOOR: return uiMainDoor;
                    case DATA_SINCLARI: return uiSinclari;
                    case DATA_TELEPORTATION_PORTAL: return uiTeleportationPortal;
                    case DATA_SABOTEUR_PORTAL: return uiSaboteurPortal;
                }

                return 0;
            }

            void SpawnPortal()
            {
                SetData(DATA_PORTAL_LOCATION, (GetData(DATA_PORTAL_LOCATION) + RandomHelper.URand(1, 5)) % 6);
                if (Creature pSinclari = instance.GetCreature(uiSinclari))
                    if (Creature portal = pSinclari.SummonCreature(CREATURE_TELEPORTATION_PORTAL, PortalLocation[GetData(DATA_PORTAL_LOCATION)], TempSummonType.CorpseDespawn))
                        uiTeleportationPortal = portal.GetGUID();
            }

            void StartBossEncounter(byte uiBoss, bool bForceRespawn = true)
            {
                Creature pBoss = null;

                switch (uiBoss)
                {
                    case BOSS_MORAGG:
                        HandleGameObject(uiMoraggCell, bForceRespawn);
                        pBoss = instance.GetCreature(uiMoragg);
                        if (pBoss)
                            pBoss.GetMotionMaster().MovePoint(0, BossStartMove1);
                        break;
                    case BOSS_EREKEM:
                        HandleGameObject(uiErekemCell, bForceRespawn);
                        HandleGameObject(uiErekemRightGuardCell, bForceRespawn);
                        HandleGameObject(uiErekemLeftGuardCell, bForceRespawn);

                        pBoss = instance.GetCreature(uiErekem);

                        if (pBoss)
                            pBoss.GetMotionMaster().MovePoint(0, BossStartMove2);

                        if (Creature pGuard1 = instance.GetCreature(uiErekemGuard[0]))
                        {
                            if (bForceRespawn)
                                pGuard1.SetFlag(UnitFields.Flags, UnitFlags.ImmuneToPc | UnitFlags.NonAttackable);
                            else
                                pGuard1.RemoveFlag(UnitFields.Flags, UnitFlags.ImmuneToPc | UnitFlags.NonAttackable);
                            pGuard1.GetMotionMaster().MovePoint(0, BossStartMove21);
                        }

                        if (Creature pGuard2 = instance.GetCreature(uiErekemGuard[1]))
                        {
                            if (bForceRespawn)
                                pGuard2.SetFlag(UnitFields.Flags, UnitFlags.ImmuneToPc | UnitFlags.NonAttackable);
                            else
                                pGuard2.RemoveFlag(UnitFields.Flags, UnitFlags.ImmuneToPc | UnitFlags.NonAttackable);
                            pGuard2.GetMotionMaster().MovePoint(0, BossStartMove22);
                        }
                        break;
                    case BOSS_ICHORON:
                        HandleGameObject(uiIchoronCell, bForceRespawn);
                        pBoss = instance.GetCreature(uiIchoron);
                        if (pBoss)
                            pBoss.GetMotionMaster().MovePoint(0, BossStartMove3);
                        break;
                    case BOSS_LAVANTHOR:
                        HandleGameObject(uiLavanthorCell, bForceRespawn);
                        pBoss = instance.GetCreature(uiLavanthor);
                        if (pBoss)
                            pBoss.GetMotionMaster().MovePoint(0, BossStartMove4);
                        break;
                    case BOSS_XEVOZZ:
                        HandleGameObject(uiXevozzCell, bForceRespawn);
                        pBoss = instance.GetCreature(uiXevozz);
                        if (pBoss)
                            pBoss.GetMotionMaster().MovePoint(0, BossStartMove5);
                        break;
                    case BOSS_ZURAMAT:
                        HandleGameObject(uiZuramatCell, bForceRespawn);
                        pBoss = instance.GetCreature(uiZuramat);
                        if (pBoss)
                            pBoss.GetMotionMaster().MovePoint(0, BossStartMove6);
                        break;
                }

                // generic boss state changes
                if (pBoss)
                {
                    pBoss.RemoveFlag(UnitFields.Flags, UnitFlags.ImmuneToPc | UnitFlags.NonAttackable);
                    pBoss.SetReactState(ReactStates.Aggressive);

                    if (!bForceRespawn)
                    {
                        if (pBoss.IsDead())
                        {
                            // respawn but avoid to be looted again
                            pBoss.Respawn();
                            pBoss.RemoveLootMode(1);
                        }
                        pBoss.SetFlag(UnitFields.Flags, UnitFlags.ImmuneToPc | UnitFlags.NonAttackable);
                        uiWaveCount = 0;
                    }
                }
            }

            void AddWave()
            {
                DoUpdateWorldState(WORLD_STATE_VH, 1);
                DoUpdateWorldState(WORLD_STATE_VH_WAVE_COUNT, uiWaveCount);

                switch (uiWaveCount)
                {
                    case 6:
                        if (uiFirstBoss == 0)
                            uiFirstBoss = (byte)RandomHelper.URand(1, 6);
                        if (Creature pSinclari = instance.GetCreature(uiSinclari))
                        {
                            if (Creature pPortal = pSinclari.SummonCreature(CREATURE_TELEPORTATION_PORTAL, MiddleRoomPortalSaboLocation, TempSummonType.CorpseDespawn))
                                uiSaboteurPortal = pPortal.GetGUID();
                            if (Creature pAzureSaboteur = pSinclari.SummonCreature(CREATURE_SABOTEOUR, MiddleRoomLocation, TempSummonType.DeadDespawn))
                                pAzureSaboteur.CastSpell(pAzureSaboteur, SABOTEUR_SHIELD_EFFECT, false);
                        }
                        break;
                    case 12:
                        if (uiSecondBoss == 0)
                            do
                            {
                                uiSecondBoss = (byte)RandomHelper.URand(1, 6);
                            } while (uiSecondBoss == uiFirstBoss);
                        if (Creature pSinclari = instance.GetCreature(uiSinclari))
                        {
                            if (Creature pPortal = pSinclari.SummonCreature(CREATURE_TELEPORTATION_PORTAL, MiddleRoomPortalSaboLocation, TempSummonType.CorpseDespawn))
                                uiSaboteurPortal = pPortal.GetGUID();
                            if (Creature pAzureSaboteur = pSinclari.SummonCreature(CREATURE_SABOTEOUR, MiddleRoomLocation, TempSummonType.DeadDespawn))
                                pAzureSaboteur.CastSpell(pAzureSaboteur, SABOTEUR_SHIELD_EFFECT, false);
                        }
                        break;
                    case 18:
                        {
                            Creature pSinclari = instance.GetCreature(uiSinclari);
                            if (pSinclari)
                                pSinclari.SummonCreature(CREATURE_CYANIGOSA, CyanigosasSpawnLocation, TempSummonType.DeadDespawn);
                            break;
                        }
                    case 1:
                        {
                            if (GameObject pMainDoor = instance.GetGameObject(uiMainDoor))
                                pMainDoor.SetGoState(GameObjectState.Ready);
                            DoUpdateWorldState(WORLD_STATE_VH_PRISON_STATE, 100);
                            // no break
                        }
                        goto default;
                    default:
                        SpawnPortal();
                        break;
                }
            }

            public override string GetSaveData()
            {
                OUT_SAVE_INST_DATA();                

                str_data = string.Format("V H {0} {1} {2} {3} {4}", m_auiEncounter[0], m_auiEncounter[1], m_auiEncounter[2], uiFirstBoss, uiSecondBoss);

                OUT_SAVE_INST_DATA_COMPLETE();
                return str_data;
            }

            public override void Load(string str)
            {
                if (!string.IsNullOrEmpty(str))
                {
                    OUT_LOAD_INST_DATA_FAIL();
                    return;
                }

                OUT_LOAD_INST_DATA(str);

                StringArguments args = new StringArguments(str);
                char dataHead1 = args.NextChar();
                char dataHead2 = args.NextChar();
                ushort data0 = args.NextUInt16();
                ushort data1 = args.NextUInt16();
                ushort data2 = args.NextUInt16();
                ushort data3 = args.NextUInt16();
                ushort data4 = args.NextUInt16();

                if (dataHead1 == 'V' && dataHead2 == 'H')
                {
                    m_auiEncounter[0] = data0;
                    m_auiEncounter[1] = data1;
                    m_auiEncounter[2] = data2;

                    for (byte i = 0; i < MAX_ENCOUNTER; ++i)
                        if ((EncounterState)m_auiEncounter[i] == EncounterState.InProgress)
                            m_auiEncounter[i] = (ushort)EncounterState.NotStarted;

                    uiFirstBoss = (byte)data3;
                    uiSecondBoss = (byte)data4;
                }
                else
                    OUT_LOAD_INST_DATA_FAIL();

                OUT_LOAD_INST_DATA_COMPLETE();
            }

            bool CheckWipe()
            {
                var players = instance.GetPlayers();
                foreach (var player in players)
                {
                    if (player.IsGameMaster())
                        continue;

                    if (player.IsAlive())
                        return false;
                }

                return true;
            }

            public override void Update(uint diff)
            {
                if (!instance.HavePlayers())
                    return;

                // portals should spawn if other portal is dead and doors are closed
                if (bActive && uiMainEventPhase == EncounterState.InProgress)
                {
                    if (uiActivationTimer < diff)
                    {
                        AddWave();
                        bActive = false;
                        // 1 minute waiting time after each boss fight
                        uiActivationTimer = (uint)((uiWaveCount == 6 || uiWaveCount == 12) ? 60000 : 5000);
                    } else uiActivationTimer -= diff;
                }

                // if main event is in progress and players have wiped then reset instance
                if (uiMainEventPhase == EncounterState.InProgress && CheckWipe())
                {
                    SetData(DATA_REMOVE_NPC, 1);
                    StartBossEncounter(uiFirstBoss, false);
                    StartBossEncounter(uiSecondBoss, false);

                    SetData(DATA_MAIN_DOOR, GameObjectState.Active);
                    SetData(DATA_WAVE_COUNT, 0);
                    uiMainEventPhase = EncounterState.NotStarted;

                    for (int i = 0; i < 4; ++i)
                    {
                        if (GameObject crystal = instance.GetGameObject(uiActivationCrystal[i]))
                            crystal.SetFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                    }

                    if (Creature pSinclari = instance.GetCreature(uiSinclari))
                    {
                        pSinclari.SetVisible(true);

                        List<Creature> GuardList = new List<Creature>();
                        pSinclari.GetCreatureListWithEntryInGrid(GuardList, NPC_VIOLET_HOLD_GUARD, 40.0f);
                        if (!GuardList.Empty())
                        {
                            foreach (var pGuard in GuardList)
                            {
                                pGuard.SetVisible(true);
                                pGuard.SetReactState(ReactStates.Aggressive);
                                pGuard.GetMotionMaster().MovePoint(1, pGuard.GetHomePosition());
                            }
                        }
                        pSinclari.GetMotionMaster().MovePoint(1, pSinclari.GetHomePosition());
                        pSinclari.RemoveFlag(UnitFields.Flags, UnitFlags.NotSelectable);
                    }
                }

                // Cyanigosa is spawned but not tranformed, prefight event
                Creature pCyanigosa = instance.GetCreature(uiCyanigosa);
                if (pCyanigosa && !pCyanigosa.HasAura(CYANIGOSA_SPELL_TRANSFORM))
                {
                    if (uiCyanigosaEventTimer <= diff)
                    {
                        switch (uiCyanigosaEventPhase)
                        {
                            case 1:
                                pCyanigosa.CastSpell(pCyanigosa, CYANIGOSA_BLUE_AURA, false);
                                pCyanigosa.GetAI().Talk(CYANIGOSA_SAY_SPAWN);
                                uiCyanigosaEventTimer = 7 * Time.InMilliseconds;
                                ++uiCyanigosaEventPhase;
                                break;
                            case 2:
                                pCyanigosa.GetMotionMaster().MoveJump(MiddleRoomLocation.GetPositionX(), MiddleRoomLocation.GetPositionY(), MiddleRoomLocation.GetPositionZ(), 10.0f, 20.0f);
                                pCyanigosa.CastSpell(pCyanigosa, CYANIGOSA_BLUE_AURA, false);
                                uiCyanigosaEventTimer = 7 * Time.InMilliseconds;
                                ++uiCyanigosaEventPhase;
                                break;
                            case 3:
                                pCyanigosa.RemoveAurasDueToSpell(CYANIGOSA_BLUE_AURA);
                                pCyanigosa.CastSpell(pCyanigosa, CYANIGOSA_SPELL_TRANSFORM, 0);
                                pCyanigosa.RemoveFlag(UnitFields.Flags, UnitFlags.ImmuneToPc | UnitFlags.NonAttackable);
                                pCyanigosa.SetReactState(ReactStates.Aggressive);
                                uiCyanigosaEventTimer = 2 * Time.InMilliseconds;
                                ++uiCyanigosaEventPhase;
                                break;
                            case 4:
                                uiCyanigosaEventPhase = 0;
                                break;
                        }
                    } else uiCyanigosaEventTimer -= diff;
                }

                // if there are NPCs in front of the prison door, which are casting the door seal spell and doors are active
                if (GetData(DATA_NPC_PRESENCE_AT_DOOR) && uiMainEventPhase == EncounterState.InProgress)
                {
                    // if door integrity is > 0 then decrase it's integrity state
                    if (GetData(DATA_DOOR_INTEGRITY))
                    {
                        if (uiDoorSpellTimer < diff)
                        {
                            SetData(DATA_DOOR_INTEGRITY, GetData(DATA_DOOR_INTEGRITY) - 1);
                            uiDoorSpellTimer = 2000;
                        } else uiDoorSpellTimer -= diff;
                    }
                    // else set door state to active (means door will open and group have failed to sustain mob invasion on the door)
                    else
                    {
                        SetData(DATA_MAIN_DOOR, GameObjectState.Active);
                        uiMainEventPhase = EncounterState.Fail;
                    }
                }
            }

            void ActivateCrystal()
            {
                // just to make things easier we'll get the gameobject from the map
                GameObject invoker = instance.GetGameObject(uiActivationCrystal[0]);
                if (!invoker)
                    return;

                SpellInfo spellInfoLightning = Global.SpellMgr.GetSpellInfo(SPELL_ARCANE_LIGHTNING);
                if (spellInfoLightning == null)
                    return;

                // the orb
                TempSummon trigger = invoker.SummonCreature(NPC_DEFENSE_SYSTEM, ArcaneSphere, TempSummonType.ManualDespawn, 0);
                if (!trigger)
                    return;

                // visuals
                trigger.CastSpell(trigger, spellInfoLightning, true, 0, 0, trigger.GetGUID());

                // Kill all mobs registered with SetData64(ADD_TRASH_MOB)
                foreach (var guid in trashMobs)
                {
                    Creature creature = instance.GetCreature(guid);
                    if (creature && creature.IsAlive())
                        trigger.Kill(creature);
                }
            }

            public override void ProcessEvent(WorldObject go, uint uiEventId)
            {
                switch (uiEventId)
                {
                    case EVENT_ACTIVATE_CRYSTAL:
                        bCrystalActivated = true; // Activation by player's will throw event signal
                        ActivateCrystal();
                        break;
                }
            }

            #region Fields

            ulong uiMoragg;
            ulong uiErekem;
            ulong[] uiErekemGuard = new ulong[2];
            ulong uiIchoron;
            ulong uiLavanthor;
            ulong uiXevozz;
            ulong uiZuramat;
            ulong uiCyanigosa;
            ulong uiSinclari;

            ulong uiMoraggCell;
            ulong uiErekemCell;
            ulong uiErekemLeftGuardCell;
            ulong uiErekemRightGuardCell;
            ulong uiIchoronCell;
            ulong uiLavanthorCell;
            ulong uiXevozzCell;
            ulong uiZuramatCell;
            ulong uiMainDoor;
            ulong uiTeleportationPortal;
            ulong uiSaboteurPortal;

            ulong[] uiActivationCrystal = new ulong[4];

            uint uiActivationTimer;
            uint uiCyanigosaEventTimer;
            uint uiDoorSpellTimer;

            List<ulong> trashMobs = new List<ulong>(); // to kill with crystal

            byte uiWaveCount;
            byte uiLocation;
            byte uiFirstBoss;
            byte uiSecondBoss;
            byte uiRemoveNpc;

            byte uiDoorIntegrity;

            ushort[] m_auiEncounter = new ushort[MAX_ENCOUNTER];
            byte uiCountErekemGuards;
            byte uiCountActivationCrystals;
            byte uiCyanigosaEventPhase;
            EncounterState uiMainEventPhase; // SPECIAL: pre event animations, InProgress: event itself

            bool bActive;
            bool bWiped;
            bool bIsDoorSpellCast;
            bool bCrystalActivated;
            bool defenseless;

            List<byte> NpcAtDoorCastingList = new List<byte>();

            string str_data;
            #endregion
        }
    }
}
