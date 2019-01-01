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
using Framework.GameMath;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Northrend.Ulduar
{
    namespace Mimiron
    {
        struct Yells
        {
            public const uint Aggro = 0;
            public const uint HardmodeOn = 1;
            public const uint MkiiActivate = 2;
            public const uint MkiiSlay = 3;
            public const uint MkiiDeath = 4;
            public const uint Vx001Activate = 5;
            public const uint Vx001Slay = 6;
            public const uint Vx001Death = 7;
            public const uint AerialActivate = 8;
            public const uint AerialSlay = 9;
            public const uint AerialDeath = 10;
            public const uint V07tronActivate = 11;
            public const uint V07tronSlay = 12;
            public const uint V07tronDeath = 13;
            public const uint Berserk = 14;
        }

        struct ComputerYells
        {
            public const uint SelfDestructInitiated = 0;
            public const uint SelfDestructTerminated = 1;
            public const uint SelfDestruct10 = 2;
            public const uint SelfDestruct9 = 3;
            public const uint SelfDestruct8 = 4;
            public const uint SelfDestruct7 = 5;
            public const uint SelfDestruct6 = 6;
            public const uint SelfDestruct5 = 7;
            public const uint SelfDestruct4 = 8;
            public const uint SelfDestruct3 = 9;
            public const uint SelfDestruct2 = 10;
            public const uint SelfDestruct1 = 11;
            public const uint SelfDestructFinalized = 12;
        }

        struct Spells
        {
            // Mimiron
            public const uint Weld = 63339; // Idle Aura.
            public const uint Seat1 = 52391; // Cast On All Vehicles; Cycled On Mkii
            public const uint Seat2 = 63313; // Cast On Mkii And Vx-001; Cycled On Mkii
            public const uint Seat3 = 63314; // Cast On Mkii; Cycled On Mkii
            public const uint Seat5 = 63316; // Cast On Mkii And Vx-001; Cycled On Mkii
            public const uint Seat6 = 63344; // Cast On Mkii
            public const uint Seat7 = 63345; // Cast On Mkii
            public const uint Jetpack = 63341;
            public const uint DespawnAssaultBots = 64463; // Only Despawns Assault Bots... No Equivalent Spell For The Other Adds...
            public const uint TeleportVisual = 41232;
            public const uint SleepVisual1 = 64393;
            public const uint SleepVisual2 = 64394;

            // Leviathan Mk Ii
            public const uint FlameSuppressantMk = 64570;
            public const uint NapalmShell = 63666;
            public const uint ForceCastNapalmShell = 64539;
            public const uint PlasmaBlast = 62997;
            public const uint ScriptEffectPlasmaBlast = 64542;
            public const uint ShockBlast = 63631;
            public const uint ShockBlastAura = 63632; // Deprecated? It Is Never Cast.

            // Vx-001
            public const uint FlameSuppressantVx = 65192;
            public const uint SpinningUp = 63414;
            public const uint HeatWaveAura = 63679;
            public const uint HandPulseLeft = 64348;
            public const uint HandPulseRight = 64352;
            public const uint MountMkii = 64387;
            public const uint TorsoDisabled = 64120;

            // Aerial Command Unit
            public const uint PlasmaBallP1 = 63689;
            public const uint PlasmaBallP2 = 65647;
            public const uint MountVx001 = 64388;

            // Proximity Mines
            public const uint ProximityMines = 63027; // Cast By Leviathan Mk Ii
            public const uint ProximityMineExplosion = 66351;
            public const uint ProximityMineTrigger = 65346;
            public const uint ProximityMinePeriodicTrigger = 65345;
            public const uint PeriodicProximityAura = 65345;
            public const uint SummonProximityMine = 65347;

            // Rapid Burst
            public const uint RapidBurstLeft = 63387;
            public const uint RapidBurstRight = 64019;
            public const uint RapidBurst = 63382; // Cast By Vx-001
            public const uint RapidBurstTargetMe = 64841; // Cast By Burst Target
            public const uint SummonBurstTarget = 64840; // Cast By Vx-001

            // Rocket Strike
            public const uint SummonRocketStrike = 63036;
            public const uint ScriptEffectRocketStrike = 63681; // Cast By Rocket (Mimiron Visual)
            public const uint RocketStrike = 64064; // Added In CreatureTemplateAddon
            public const uint RocketStrikeSingle = 64402; // Cast By Vx-001
            public const uint RocketStrikeBoth = 65034; // Cast By Vx-001

            // Flames
            public const uint FlamesPeriodicTrigger = 64561; // Added In CreatureTemplateAddon
            public const uint SummonFlamesSpreadTrigger = 64562;
            public const uint SummonFlamesInitial = 64563;
            public const uint SummonFlamesSpread = 64564;
            public const uint Flames = 64566;
            public const uint ScriptEffectSummonFlamesInitial = 64567;

            // Frost Bomb
            public const uint ScriptEffectFrostBomb = 64623; // Cast By Vx-001
            public const uint FrostBombLinked = 64624; // Added In CreatureTemplateAddon
            public const uint FrostBombDummy = 64625;
            public const uint SummonFrostBomb = 64627; // Cast By Vx-001
            public const uint FrostBombExplosion = 64626;
            public const uint ClearFires = 65354;

            // Bots
            public const uint SummonFireBot = 64622;
            public const uint SummonFireBotDummy = 64621;
            public const uint SummonFireBotTrigger = 64620; // Cast By Areal Command Unit
            public const uint DeafeningSiren = 64616; // Added In CreatureTemplateAddon
            public const uint FireSearchAura = 64617; // Added In CreatureTemplateAddon
            public const uint FireSearch = 64618;
            public const uint WaterSpray = 64619;

            public const uint SummonJunkBot = 63819;
            public const uint SummonJunkBotTrigger = 63820; // Cast By Areal Command Unit
            public const uint SummonJunkBotDummy = 64398;

            public const uint SummonAssaultBotTrigger = 64425; // Cast By Areal Command Unit
            public const uint SummonAssaultBotDummy = 64426;
            public const uint SummonAssaultBot = 64427;
            public const uint MagneticField = 64668;

            public const uint SummonBombBot = 63811; // Cast By Areal Command Unit
            public const uint BombBotAura = 63767; // Added In CreatureTemplateAddon

            // Miscellaneous
            public const uint SelfDestructionAura = 64610;
            public const uint SelfDestructionVisual = 64613;
            public const uint NotSoFriendlyFire = 65040;
            public const uint ElevatorKnockback = 65096; // Cast By Worldtrigger.
            public const uint VehicleDamaged = 63415;
            public const uint EmergencyMode = 64582; // Mkii; Vx001; Aerial; Assault; Junk
            public const uint EmergencyModeTurret = 65101; // Cast By Leviathan Mk Ii; Only Hits Leviathan Mk Ii Turret
            public const uint SelfRepair = 64383;
            public const uint MagneticCore = 64436;
            public const uint MagneticCoreVisual = 64438;
            public const uint HalfHeal = 64188;
            public const uint ClearAllDebuffs = 34098; // @Todo: Make Use Of This Spell...
            public const uint FreezeAnimStun = 63354; // Used To Prevent Mkii From Doing Stuff?..
            public const uint FreezeAnim = 16245;  // Idle Aura. Freezes Animation.
        }

        struct Data
        {
            public const uint SetupMine = 0;
            public const uint SetupBomb = 1;
            public const uint SetupRocket = 2;
            public const uint NotSoFriendlyFire = 3;
            public const uint Firefighter = 4;
            public const uint Waterspray = 5;
            public const uint MoveNew = 6;
        }

        struct Events
        {
            // Leviathan Mk Ii
            public const uint ProximityMine = 1;
            public const uint NapalmShell = 2;
            public const uint PlasmaBlast = 3;
            public const uint ShockBlast = 4;
            public const uint FlameSuppressantMk = 5;
            public const uint MovePoint2 = 6;
            public const uint MovePoint3 = 7;
            public const uint MovePoint5 = 8;

            // Vx-001
            public const uint RapidBurst = 1;
            public const uint SpinningUp = 2;
            public const uint RocketStrike = 3;
            public const uint HandPulse = 4;
            public const uint FrostBomb = 5;
            public const uint FlameSuppressantVx = 6;
            public const uint Reload = 7;

            // Aerial Command Unit
            public const uint SummonFireBots = 1;
            public const uint SummonJunkBot = 2;
            public const uint SummonAssaultBot = 3;
            public const uint SummonBombBot = 4;

            // Mimiron
            public const uint SummonFlames = 1;
            public const uint Intro1 = 2;
            public const uint Intro2 = 3;
            public const uint Intro3 = 4;

            public const uint Vx001Activation1 = 5;
            public const uint Vx001Activation2 = 6;
            public const uint Vx001Activation3 = 7;
            public const uint Vx001Activation4 = 8;
            public const uint Vx001Activation5 = 9;
            public const uint Vx001Activation6 = 10;
            public const uint Vx001Activation7 = 11;
            public const uint Vx001Activation8 = 12;

            public const uint AerialActivation1 = 13;
            public const uint AerialActivation2 = 14;
            public const uint AerialActivation3 = 15;
            public const uint AerialActivation4 = 16;
            public const uint AerialActivation5 = 17;
            public const uint AerialActivation6 = 18;

            public const uint Vol7ronActivation1 = 19;
            public const uint Vol7ronActivation2 = 20;
            public const uint Vol7ronActivation3 = 21;
            public const uint Vol7ronActivation4 = 22;
            public const uint Vol7ronActivation5 = 23;
            public const uint Vol7ronActivation6 = 24;
            public const uint Vol7ronActivation7 = 25;

            public const uint Outtro1 = 26;
            public const uint Outtro2 = 27;
            public const uint Outtro3 = 28;

            // Computer
            public const uint SelfDestruct10 = 1;
            public const uint SelfDestruct9 = 2;
            public const uint SelfDestruct8 = 3;
            public const uint SelfDestruct7 = 4;
            public const uint SelfDestruct6 = 5;
            public const uint SelfDestruct5 = 6;
            public const uint SelfDestruct4 = 7;
            public const uint SelfDestruct3 = 8;
            public const uint SelfDestruct2 = 9;
            public const uint SelfDestruct1 = 10;
            public const uint SelfDestructFinalized = 11;
        }

        struct Actions
        {
            public const int StartMkii = 1;
            public const int HardmodeMkii = 2;

            public const int ActivateVx001 = 3;
            public const int StartVx001 = 4;
            public const int HardmodeVx001 = 5;

            public const int ActivateAerial = 6;
            public const int StartAerial = 7;
            public const int HardmodeAerial = 8;
            public const int DisableAerial = 9;
            public const int EnableAerial = 10;

            public const int ActivateV0l7r0n1 = 11;
            public const int ActivateV0l7r0n2 = 12;
            public const int AssembledCombat = 13; // All 3 Parts Use This Action = 1; Its Done On Purpose.

            public const int ActivateHardMode = 14;
            public const int ActivateComputer = 15;
            public const int DeactivateComputer = 16;
            public const int ActivateSelfDestruct = 17;

            public const int EncounterDone = 18;
        }

        struct Phases
        {
            public const byte LeviathanMkII = 1;
            public const byte Vx001 = 2;
            public const byte AerialCommandUnit = 3;
            public const byte Vol7ron = 4;
        }

        struct Waypoints
        {
            public const uint MkiiP1Idle = 1;
            public const uint MkiiP4Pos1 = 2;
            public const uint MkiiP4Pos2 = 3;
            public const uint MkiiP4Pos3 = 4;
            public const uint MkiiP4Pos4 = 5;
            public const uint MkiiP4Pos5 = 6;
            public const uint AerialP4Pos = 7;
        }

        struct SeatIds
        {
            public const sbyte RocketLeft = 5;
            public const sbyte RocketRight = 6;
        }

        struct MimironConst
        {
            public static uint[] RepairSpells =
            {
                Spells.Seat1,
                Spells.Seat2,
                Spells.Seat3,
                Spells.Seat5
            };

            public static Position[] VehicleRelocation =
            {
                new Position(0.0f, 0.0f, 0.0f),
                new Position(2792.07f, 2596.32f, 364.3136f, 3.560472f), // WP_MKII_P1_IDLE
                new Position(2765.945f, 2571.095f, 364.0636f), // WP_MKII_P4_POS_1
                new Position(2768.195f, 2573.095f, 364.0636f), // WP_MKII_P4_POS_2
                new Position(2763.820f, 2568.870f, 364.3136f), // WP_MKII_P4_POS_3
                new Position(2761.215f, 2568.875f, 364.0636f), // WP_MKII_P4_POS_4
                new Position(2744.610f, 2569.380f, 364.3136f), // WP_MKII_P4_POS_5
                new Position(2748.513f, 2569.051f, 364.3136f)  // WP_AERIAL_P4_POS
            };

            public static Position VX001SummonPos = new Position(2744.431f, 2569.385f, 364.3968f, 3.141593f);
            public static Position ACUSummonPos = new Position(2744.650f, 2569.460f, 380.0000f, 0.0f);

            public static bool IsEncounterFinished(Unit who)
            {
                InstanceScript instance = who.GetInstanceScript();

                Creature mkii = ObjectAccessor.GetCreature(who, instance.GetGuidData(InstanceData.LeviathanMKII));
                Creature vx001 = ObjectAccessor.GetCreature(who, instance.GetGuidData(InstanceData.VX001));
                Creature aerial = ObjectAccessor.GetCreature(who, instance.GetGuidData(InstanceData.AerialCommandUnit));
                if (!mkii || !vx001 || !aerial)
                    return false;

                if (mkii.GetStandState() == UnitStandStateType.Dead && vx001.GetStandState() == UnitStandStateType.Dead && aerial.GetStandState() == UnitStandStateType.Dead)
                {
                    who.Kill(mkii);
                    who.Kill(vx001);
                    who.Kill(aerial);
                    mkii.DespawnOrUnsummon(120000);
                    vx001.DespawnOrUnsummon(120000);
                    aerial.DespawnOrUnsummon(120000);
                    Creature mimiron = ObjectAccessor.GetCreature(who, instance.GetGuidData(BossIds.Mimiron));
                    if (mimiron)
                        mimiron.GetAI().JustDied(who);
                    return true;
                }
                return false;
            }
        }

        [Script]
        class boss_mimiron : BossAI
        {
            public boss_mimiron(Creature creature) : base(creature, BossIds.Mimiron)
            {
                me.SetReactState(ReactStates.Passive);
                _fireFighter = false;
            }

            public override void InitializeAI()
            {
                SetupEncounter();
            }

            void SetupEncounter()
            {
                _Reset();
                me.SetReactState(ReactStates.Passive);
                me.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable);

                GameObject elevator = ObjectAccessor.GetGameObject(me, instance.GetGuidData(InstanceData.MimironElevator));
                if (elevator)
                    elevator.SetGoState(GameObjectState.Active);

                if (_fireFighter)
                {
                    Creature computer = ObjectAccessor.GetCreature(me, instance.GetGuidData(InstanceData.Computer));
                    if (computer)
                        computer.GetAI().DoAction(Actions.DeactivateComputer);
                }

                GameObject button = ObjectAccessor.GetGameObject(me, instance.GetGuidData(InstanceData.MimironButton));
                if (button)
                {
                    button.SetGoState(GameObjectState.Ready);
                    button.RemoveFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                }

                _fireFighter = false;
                DoCast(me, Spells.Weld);
                Unit mkii = Global.ObjAccessor.GetUnit(me, instance.GetGuidData(InstanceData.LeviathanMKII));
                if (mkii)
                    DoCast(mkii, Spells.Seat3);

                if (!_events.Empty())
                {

                }
            }

            public override void DoAction(int action)
            {
                switch (action)
                {
                    case Actions.ActivateVx001:
                        _events.ScheduleEvent(Events.Vx001Activation1, 1000);
                        break;
                    case Actions.ActivateAerial:
                        _events.ScheduleEvent(Events.AerialActivation1, 5000);
                        break;
                    case Actions.ActivateV0l7r0n1:
                        Talk(Yells.AerialDeath);
                        Creature mkii = ObjectAccessor.GetCreature(me, instance.GetGuidData(InstanceData.LeviathanMKII));
                        if (mkii)
                            mkii.GetMotionMaster().MovePoint(Waypoints.MkiiP4Pos1, MimironConst.VehicleRelocation[Waypoints.MkiiP4Pos1]);
                        break;
                    case Actions.ActivateV0l7r0n2:
                        _events.ScheduleEvent(Events.Vol7ronActivation1, 1000);
                        break;
                    case Actions.ActivateHardMode:
                        _fireFighter = true;
                        DoZoneInCombat(me);
                        break;
                    default:
                        break;
                }
            }

            public override void EnterCombat(Unit who)
            {
                if (!me.GetVehicleBase())
                    return;

                //PLay Sound number 15612

                _EnterCombat();
                me.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable);
                me.RemoveAurasDueToSpell(Spells.Weld);
                DoCast(me.GetVehicleBase(), Spells.Seat6);

                GameObject button = ObjectAccessor.GetGameObject(me, instance.GetGuidData(InstanceData.MimironButton));
                if (button)
                    button.SetFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);

                if (_fireFighter)
                    _events.ScheduleEvent(Events.SummonFlames, 3000);

                _events.ScheduleEvent(Events.Intro1, 1500);
            }

            public override void JustDied(Unit who)
            {
                instance.SetBossState(BossIds.Mimiron, EncounterState.Done);
                _events.Reset();
                me.CombatStop(true);
                me.SetDisableGravity(false);
                DoCast(me, Spells.SleepVisual1);
                DoCastAOE(Spells.DespawnAssaultBots);
                me.ExitVehicle();
                // ExitVehicle() offset position is not implemented, so we make up for that with MoveJump()...
                me.GetMotionMaster().MoveJump(me.GetPositionX() + (float)(10.0f * Math.Cos(me.GetOrientation())), me.GetPositionY() + (float)(10.0f * Math.Sin(me.GetOrientation())), me.GetPositionZ(), me.GetOrientation(), 10.0f, 5.0f);
                _events.ScheduleEvent(Events.Outtro1, 7000);
            }

            public override void JustRespawned()
            {
                //SetupEncounter();
            }

            public override void EnterEvadeMode(EvadeReason why = EvadeReason.Other)
            {
                _DespawnAtEvade();
            }

            public override void UpdateAI(uint diff)
            {
                if (!UpdateVictim() && instance.GetBossState(BossIds.Mimiron) != EncounterState.Done)
                    return;

                _events.Update(diff);

                if (me.HasUnitState(UnitState.Casting))
                    return;

                _events.ExecuteEvents(eventId =>
                {
                    switch (eventId)
                    {
                        case Events.SummonFlames:
                            {
                                Unit worldtrigger = Global.ObjAccessor.GetUnit(me, instance.GetGuidData(InstanceData.MimironWorldTrigger));
                                if (worldtrigger)
                                    worldtrigger.CastCustomSpell(Spells.ScriptEffectSummonFlamesInitial, SpellValueMod.MaxTargets, 3, null, true, null, null, me.GetGUID());
                                _events.RescheduleEvent(Events.SummonFlames, 28000);
                            }
                            break;
                        case Events.Intro1:
                            Talk(_fireFighter ? Yells.HardmodeOn : Yells.MkiiActivate);
                            _events.ScheduleEvent(Events.Intro2, 5000);
                            break;
                        case Events.Intro2:
                            {
                                Unit mkii = me.GetVehicleBase();
                                if (mkii)
                                {
                                    DoCast(mkii, Spells.Seat7);
                                    mkii.RemoveAurasDueToSpell(Spells.FreezeAnim);
                                    mkii.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
                                }
                                _events.ScheduleEvent(Events.Intro3, 2000);
                            }
                            break;
                        case Events.Intro3:
                            {
                                Creature mkii = me.GetVehicleCreatureBase();
                                if (mkii)
                                    mkii.GetAI().DoAction(_fireFighter ? Actions.HardmodeMkii : Actions.StartMkii);
                            }
                            break;
                        case Events.Vx001Activation1:
                            {
                                Unit mkii = me.GetVehicleBase();
                                if (mkii)
                                    DoCast(mkii, Spells.Seat6);
                                _events.ScheduleEvent(Events.Vx001Activation2, 1000);
                            }
                            break;
                        case Events.Vx001Activation2:
                            {
                                Talk(Yells.MkiiDeath);
                                _events.ScheduleEvent(Events.Vx001Activation3, 5000);
                            }
                            break;
                        case Events.Vx001Activation3:
                            {
                                GameObject elevator = ObjectAccessor.GetGameObject(me, instance.GetGuidData(InstanceData.MimironElevator));
                                if (elevator)
                                    elevator.SetGoState(GameObjectState.Ready);
                                Unit worldtrigger = Global.ObjAccessor.GetUnit(me, instance.GetGuidData(InstanceData.MimironWorldTrigger));
                                if (worldtrigger)
                                    worldtrigger.CastSpell(worldtrigger, Spells.ElevatorKnockback);
                                _events.ScheduleEvent(Events.Vx001Activation4, 6000);
                            }
                            break;
                        case Events.Vx001Activation4:
                            {
                                GameObject elevator = ObjectAccessor.GetGameObject(me, instance.GetGuidData(InstanceData.MimironElevator));
                                if (elevator)
                                    elevator.SetGoState(GameObjectState.ActiveAlternative);
                                Creature vx001 = me.SummonCreature(InstanceCreatureIds.Vx001, MimironConst.VX001SummonPos, TempSummonType.CorpseTimedDespawn, 120000);
                                if (vx001)
                                    vx001.CastSpell(vx001, Spells.FreezeAnim);
                                _events.ScheduleEvent(Events.Vx001Activation5, 19000);
                            }
                            break;
                        case Events.Vx001Activation5:
                            {
                                Unit vx001 = Global.ObjAccessor.GetUnit(me, instance.GetGuidData(InstanceData.VX001));
                                if (vx001)
                                    DoCast(vx001, Spells.Seat1);
                                _events.ScheduleEvent(Events.Vx001Activation6, 3500);
                            }
                            break;
                        case Events.Vx001Activation6:
                            Talk(Yells.Vx001Activate);
                            _events.ScheduleEvent(Events.Vx001Activation7, 4000);
                            break;
                        case Events.Vx001Activation7:
                            {
                                Unit vx001 = me.GetVehicleBase();
                                if (vx001)
                                    DoCast(vx001, Spells.Seat2);
                                _events.ScheduleEvent(Events.Vx001Activation8, 3000);
                            }
                            break;
                        case Events.Vx001Activation8:
                            {
                                Creature vx001 = me.GetVehicleCreatureBase();
                                if (vx001)
                                    vx001.GetAI().DoAction(_fireFighter ? Actions.HardmodeVx001 : Actions.StartVx001);
                            }
                            break;
                        case Events.AerialActivation1:
                            {
                                Unit mkii = me.GetVehicleBase();
                                if (mkii)
                                    DoCast(mkii, Spells.Seat5);
                                _events.ScheduleEvent(Events.AerialActivation2, 2500);
                            }
                            break;
                        case Events.AerialActivation2:
                            Talk(Yells.Vx001Death);
                            _events.ScheduleEvent(Events.AerialActivation3, 5000);
                            break;
                        case Events.AerialActivation3:
                            me.SummonCreature(InstanceCreatureIds.AerialCommandUnit, MimironConst.ACUSummonPos, TempSummonType.ManualDespawn);
                            _events.ScheduleEvent(Events.AerialActivation4, 5000);
                            break;
                        case Events.AerialActivation4:
                            {
                                Unit aerial = Global.ObjAccessor.GetUnit(me, instance.GetGuidData(InstanceData.AerialCommandUnit));
                                if (aerial)
                                    me.CastSpell(aerial, Spells.Seat1);
                                _events.ScheduleEvent(Events.AerialActivation5, 2000);
                            }
                            break;
                        case Events.AerialActivation5:
                            Talk(Yells.AerialActivate);
                            _events.ScheduleEvent(Events.AerialActivation6, 8000);
                            break;
                        case Events.AerialActivation6:
                            Creature acu = me.GetVehicleCreatureBase();
                            if (acu)
                                acu.GetAI().DoAction(_fireFighter ? Actions.HardmodeAerial : Actions.StartAerial);
                            break;
                        case Events.Vol7ronActivation1:
                            {
                                Creature mkii = ObjectAccessor.GetCreature(me, instance.GetGuidData(InstanceData.LeviathanMKII));
                                if (mkii)
                                    mkii.SetFacingTo((float)Math.PI);
                                _events.ScheduleEvent(Events.Vol7ronActivation2, 1000);
                            }
                            break;
                        case Events.Vol7ronActivation2:
                            {
                                Creature mkii = ObjectAccessor.GetCreature(me, instance.GetGuidData(InstanceData.LeviathanMKII));
                                if (mkii)
                                {
                                    Creature vx001 = ObjectAccessor.GetCreature(me, instance.GetGuidData(InstanceData.VX001));
                                    if (vx001)
                                    {
                                        vx001.RemoveAurasDueToSpell(Spells.TorsoDisabled);
                                        vx001.CastSpell(mkii, Spells.MountMkii);
                                    }
                                }
                                _events.ScheduleEvent(Events.Vol7ronActivation3, 4500);
                            }
                            break;
                        case Events.Vol7ronActivation3:
                            {
                                Creature mkii = ObjectAccessor.GetCreature(me, instance.GetGuidData(InstanceData.LeviathanMKII));
                                if (mkii)
                                    mkii.GetMotionMaster().MovePoint(Waypoints.MkiiP4Pos4, MimironConst.VehicleRelocation[Waypoints.MkiiP4Pos4]);
                                _events.ScheduleEvent(Events.Vol7ronActivation4, 5000);
                            }
                            break;
                        case Events.Vol7ronActivation4:
                            {
                                Creature vx001 = ObjectAccessor.GetCreature(me, instance.GetGuidData(InstanceData.VX001));
                                if (vx001)
                                {
                                    Creature aerial = ObjectAccessor.GetCreature(me, instance.GetGuidData(InstanceData.AerialCommandUnit));
                                    if (aerial)
                                    {
                                        aerial.GetMotionMaster().MoveLand(0, new Position(aerial.GetPositionX(), aerial.GetPositionY(), aerial.GetPositionZMinusOffset()));
                                        aerial.SetByteValue(UnitFields.Bytes1, UnitBytes1Offsets.AnimTier, 0);
                                        aerial.CastSpell(vx001, Spells.MountVx001);
                                        aerial.CastSpell(aerial, Spells.HalfHeal);
                                    }
                                }
                                _events.ScheduleEvent(Events.Vol7ronActivation5, 4000);
                            }
                            break;
                        case Events.Vol7ronActivation5:
                            Talk(Yells.V07tronActivate);
                            _events.ScheduleEvent(Events.Vol7ronActivation6, 3000);
                            break;
                        case Events.Vol7ronActivation6:
                            {
                                Creature vx001 = ObjectAccessor.GetCreature(me, instance.GetGuidData(InstanceData.VX001));
                                if (vx001)
                                    DoCast(vx001, Spells.Seat2);
                                _events.ScheduleEvent(Events.Vol7ronActivation7, 5000);
                            }
                            break;
                        case Events.Vol7ronActivation7:
                            for (uint data = InstanceData.LeviathanMKII; data <= InstanceData.AerialCommandUnit; ++data)
                            {
                                Creature mimironVehicle = ObjectAccessor.GetCreature(me, instance.GetGuidData(data));
                                if (mimironVehicle)
                                    mimironVehicle.GetAI().DoAction(Actions.AssembledCombat);
                            }
                            break;
                        case Events.Outtro1:
                            me.RemoveAurasDueToSpell(Spells.SleepVisual1);
                            DoCast(me, Spells.SleepVisual2);
                            me.SetFaction(35);
                            _events.ScheduleEvent(Events.Outtro2, 3000);
                            break;
                        case Events.Outtro2:
                            Talk(Yells.V07tronDeath);
                            if (_fireFighter)
                            {
                                Creature computer = ObjectAccessor.GetCreature(me, instance.GetGuidData(InstanceData.Computer));
                                if (computer)
                                    computer.GetAI().DoAction(Actions.DeactivateComputer);
                                me.SummonGameObject(RaidMode(InstanceGameObjectIds.CacheOfInnovationFirefighter, InstanceGameObjectIds.CacheOfInnovationFirefighterHero), 2744.040f, 2569.352f, 364.3135f, 3.124123f, new Quaternion(0.0f, 0.0f, 0.9999619f, 0.008734641f), 604800);
                            }
                            else
                                me.SummonGameObject(RaidMode(InstanceGameObjectIds.CacheOfInnovation, InstanceGameObjectIds.CacheOfInnovationHero), 2744.040f, 2569.352f, 364.3135f, 3.124123f, new Quaternion(0.0f, 0.0f, 0.9999619f, 0.008734641f), 604800);
                            _events.ScheduleEvent(Events.Outtro3, 11000);
                            break;
                        case Events.Outtro3:
                            DoCast(me, Spells.TeleportVisual);
                            me.SetFlag(UnitFields.Flags, UnitFlags.NotSelectable);
                            me.DespawnOrUnsummon(1000); // sniffs say 6 sec after, but it doesnt matter.
                            break;
                        default:
                            break;
                    }
                });
            }

            bool _fireFighter;
        }

        [Script]
        class boss_leviathan_mk_ii : BossAI
        {
            public boss_leviathan_mk_ii(Creature creature) : base(creature, BossIds.Mimiron)
            {
                _fireFighter = false;
                _setupMine = true;
                _setupBomb = true;
                _setupRocket = true;
            }

            public override void InitializeAI()
            {
                SetupEncounter();
            }

            void SetupEncounter()
            {
                _Reset();
                me.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
                me.SetReactState(ReactStates.Passive);
                _fireFighter = false;
                _setupMine = true;
                _setupBomb = true;
                _setupRocket = true;
                DoCast(me, Spells.FreezeAnim);
            }

            public override void DamageTaken(Unit who, ref uint damage)
            {
                if (damage >= me.GetHealth())
                {
                    damage = (uint)(me.GetHealth() - 1); // Let creature fall to 1 hp, but do not let it die or damage itself with SetHealth().
                    me.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable);
                    DoCast(me, Spells.VehicleDamaged, true);
                    me.AttackStop();
                    me.SetReactState(ReactStates.Passive);
                    me.RemoveAllAurasExceptType(AuraType.ControlVehicle, AuraType.ModIncreaseHealthPercent);

                    if (_events.IsInPhase(Phases.LeviathanMkII))
                    {
                        me.CastStop();
                        Unit turret = me.GetVehicleKit().GetPassenger(3);
                        if (turret)
                            turret.KillSelf();

                        me.SetSpeedRate(UnitMoveType.Run, 1.5f);
                        me.GetMotionMaster().MovePoint(Waypoints.MkiiP1Idle, MimironConst.VehicleRelocation[Waypoints.MkiiP1Idle]);
                    }
                    else if (_events.IsInPhase(Phases.Vol7ron))
                    {
                        me.SetStandState(UnitStandStateType.Dead);

                        if (MimironConst.IsEncounterFinished(who))
                            return;

                        me.CastStop();
                        DoCast(me, Spells.SelfRepair);
                    }
                    _events.Reset();
                }
            }

            public override void DoAction(int action)
            {
                switch (action)
                {
                    case Actions.HardmodeMkii:
                        _fireFighter = true;
                        DoCast(me, Spells.EmergencyMode);
                        DoCastAOE(Spells.EmergencyModeTurret);
                        _events.ScheduleEvent(Events.FlameSuppressantVx, 60000, 0, Phases.LeviathanMkII);
                        goto case Actions.StartMkii;
                    case Actions.StartMkii:
                        me.SetReactState(ReactStates.Aggressive);
                        _events.SetPhase(Phases.LeviathanMkII);

                        _events.ScheduleEvent(Events.NapalmShell, 3000, 0, Phases.LeviathanMkII);
                        _events.ScheduleEvent(Events.PlasmaBlast, 15000, 0, Phases.LeviathanMkII);
                        _events.ScheduleEvent(Events.ProximityMine, 5000);
                        _events.ScheduleEvent(Events.ShockBlast, 18000);
                        break;
                    case Actions.AssembledCombat:
                        me.SetStandState(UnitStandStateType.Stand);
                        me.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
                        me.SetReactState(ReactStates.Aggressive);

                        _events.SetPhase(Phases.Vol7ron);
                        _events.ScheduleEvent(Events.ProximityMine, 15000);
                        _events.ScheduleEvent(Events.ShockBlast, 45000);
                        break;
                    default:
                        break;
                }
            }

            public override uint GetData(uint type)
            {
                switch (type)
                {
                    case Data.SetupMine:
                        return _setupMine ? 1 : 0u;
                    case Data.SetupBomb:
                        return _setupBomb ? 1 : 0u;
                    case Data.SetupRocket:
                        return _setupRocket ? 1 : 0u;
                    case Data.Firefighter:
                        return _fireFighter ? 1 : 0u;
                    default:
                        return 0;
                }
            }

            public override void JustSummoned(Creature summon)
            {
                summons.Summon(summon);
            }

            public override void KilledUnit(Unit victim)
            {
                if (victim.IsTypeId(TypeId.Player))
                {
                    Creature mimiron = ObjectAccessor.GetCreature(me, instance.GetGuidData(BossIds.Mimiron));
                    if (mimiron)
                        mimiron.GetAI().Talk(_events.IsInPhase(Phases.LeviathanMkII) ? Yells.MkiiSlay : Yells.V07tronSlay);
                }
            }

            public override void MovementInform(MovementGeneratorType type, uint point)
            {
                if (type != MovementGeneratorType.Point)
                    return;

                switch (point)
                {
                    case Waypoints.MkiiP1Idle:
                        {
                            me.SetFlag(UnitFields.Flags, UnitFlags.NotSelectable);
                            DoCast(me, Spells.HalfHeal);

                            Creature mimiron = ObjectAccessor.GetCreature(me, instance.GetGuidData(BossIds.Mimiron));
                            if (mimiron)
                                mimiron.GetAI().DoAction(Actions.ActivateVx001);
                        }
                        break;
                    case Waypoints.MkiiP4Pos1:
                        _events.ScheduleEvent(Events.MovePoint2, 1);
                        break;
                    case Waypoints.MkiiP4Pos2:
                        _events.ScheduleEvent(Events.MovePoint3, 1);
                        break;
                    case Waypoints.MkiiP4Pos3:
                        {
                            Creature mimiron = ObjectAccessor.GetCreature(me, instance.GetGuidData(BossIds.Mimiron));
                            if (mimiron)
                                mimiron.GetAI().DoAction(Actions.ActivateV0l7r0n2);
                        }
                        break;
                    case Waypoints.MkiiP4Pos4:
                        _events.ScheduleEvent(Events.MovePoint5, 1);
                        break;
                    default:
                        break;
                }
            }

            public override void SetData(uint id, uint data)
            {
                switch (id)
                {
                    case Data.SetupMine:
                        _setupMine = data != 0;
                        break;
                    case Data.SetupBomb:
                        _setupBomb = data != 0;
                        break;
                    case Data.SetupRocket:
                        _setupRocket = data != 0;
                        break;
                    default:
                        break;
                }
            }

            public override void JustRespawned()
            {
                SetupEncounter();
            }

            public override void EnterEvadeMode(EvadeReason why = EvadeReason.Other)
            {
                _DespawnAtEvade();
            }

            public override void UpdateAI(uint diff)
            {
                if (!UpdateVictim())
                    return;

                _events.Update(diff);

                if (me.HasUnitState(UnitState.Casting))
                    return;

                _events.ExecuteEvents(eventId =>
                {
                    switch (eventId)
                    {
                        case Events.ProximityMine:
                            DoCastAOE(Spells.ProximityMines);
                            _events.RescheduleEvent(Events.ProximityMine, 35000);
                            break;
                        case Events.PlasmaBlast:
                            DoCastVictim(Spells.ScriptEffectPlasmaBlast);
                            _events.RescheduleEvent(Events.PlasmaBlast, RandomHelper.URand(30000, 45000), 0, Phases.LeviathanMkII);

                            if (_events.GetTimeUntilEvent(Events.NapalmShell) < 9000)
                                _events.RescheduleEvent(Events.NapalmShell, 9000, 0, Phases.LeviathanMkII); // The actual spell is cast by the turret, we should not let it interrupt itself.
                            break;
                        case Events.ShockBlast:
                            DoCastAOE(Spells.ShockBlast);
                            _events.RescheduleEvent(Events.ShockBlast, RandomHelper.URand(34000, 36000));
                            break;
                        case Events.FlameSuppressantMk:
                            DoCastAOE(Spells.FlameSuppressantMk);
                            _events.RescheduleEvent(Events.FlameSuppressantMk, 60000, 0, Phases.LeviathanMkII);
                            break;
                        case Events.NapalmShell:
                            DoCastAOE(Spells.ForceCastNapalmShell);
                            _events.RescheduleEvent(Events.NapalmShell, RandomHelper.URand(6000, 15000), 0, Phases.LeviathanMkII);

                            if (_events.GetTimeUntilEvent(Events.PlasmaBlast) < 2000)
                                _events.RescheduleEvent(Events.PlasmaBlast, 2000, 0, Phases.LeviathanMkII);  // The actual spell is cast by the turret, we should not let it interrupt itself.
                            break;
                        case Events.MovePoint2:
                            me.GetMotionMaster().MovePoint(Waypoints.MkiiP4Pos2, MimironConst.VehicleRelocation[Waypoints.MkiiP4Pos2]);
                            break;
                        case Events.MovePoint3:
                            me.GetMotionMaster().MovePoint(Waypoints.MkiiP4Pos3, MimironConst.VehicleRelocation[Waypoints.MkiiP4Pos3]);
                            break;
                        case Events.MovePoint5:
                            me.GetMotionMaster().MovePoint(Waypoints.MkiiP4Pos5, MimironConst.VehicleRelocation[Waypoints.MkiiP4Pos5]);
                            break;
                        default:
                            break;
                    }
                });
                DoMeleeAttackIfReady();
            }

            bool _fireFighter;
            bool _setupMine;
            bool _setupBomb;
            bool _setupRocket;
        }

        [Script] //todo check for both rockets
        class boss_vx_001 : BossAI
        {
            public boss_vx_001(Creature creature) : base(creature, BossIds.Mimiron)
            {
                me.SetDisableGravity(true); // This is the unfold visual state of VX-001, it has to be set on create as it requires an objectupdate if set later.
                me.SetUInt32Value(UnitFields.NpcEmotestate, (uint)Emote.StateSpecialUnarmed); // This is a hack to force the yet to be unfolded visual state.
                me.SetReactState(ReactStates.Passive);
                _fireFighter = false;
            }

            public override void DamageTaken(Unit who, ref uint damage)
            {
                if (damage >= me.GetHealth())
                {
                    //play sound 15615
                    damage = (uint)(me.GetHealth() - 1); // Let creature fall to 1 hp, but do not let it die or damage itself with SetHealth().
                    me.AttackStop();
                    DoCast(me, Spells.VehicleDamaged, true);
                    me.RemoveAllAurasExceptType(AuraType.ControlVehicle, AuraType.ModIncreaseHealthPercent);

                    if (_events.IsInPhase(Phases.Vx001))
                    {
                        me.CastStop();
                        me.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
                        DoCast(me, Spells.HalfHeal); // has no effect, wat
                        DoCast(me, Spells.TorsoDisabled);
                        Creature mimiron = ObjectAccessor.GetCreature(me, instance.GetGuidData(BossIds.Mimiron));
                        if (mimiron)
                            mimiron.GetAI().DoAction(Actions.ActivateAerial);
                    }
                    else if (_events.IsInPhase(Phases.Vol7ron))
                    {
                        me.SetStandState(UnitStandStateType.Dead);
                        me.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable);

                        if (MimironConst.IsEncounterFinished(who))
                            return;

                        me.CastStop();
                        DoCast(me, Spells.SelfRepair);
                    }
                    _events.Reset();
                }
            }

            public override void DoAction(int action)
            {
                switch (action)
                {
                    case Actions.HardmodeVx001:
                        _fireFighter = true;
                        DoCast(me, Spells.EmergencyMode);
                        _events.ScheduleEvent(Events.FrostBomb, 1000);
                        _events.ScheduleEvent(Events.FlameSuppressantVx, 6000);
                        goto case Actions.StartVx001;
                    case Actions.StartVx001:
                        me.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
                        me.RemoveAurasDueToSpell(Spells.FreezeAnim);
                        me.SetUInt32Value(UnitFields.NpcEmotestate, (uint)Emote.OneshotNone); // Remove emotestate.
                                                                                              //me.SetuintValue(UnitFields.Bytes1, UnitBytes1Offsets.AnimTier, UnitBytes1Flags.AlwaysStand | UnitBytes1Flags.Hover); Blizzard handles hover animation like this it seems.
                        DoCast(me, Spells.HeatWaveAura);

                        _events.SetPhase(Phases.Vx001);
                        _events.ScheduleEvent(Events.RocketStrike, 20000);
                        _events.ScheduleEvent(Events.SpinningUp, RandomHelper.URand(30000, 35000));
                        _events.ScheduleEvent(Events.RapidBurst, 500, 0, Phases.Vx001);
                        break;
                    case Actions.AssembledCombat:
                        me.SetStandState(UnitStandStateType.Stand);
                        me.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);

                        _events.SetPhase(Phases.Vol7ron);
                        _events.ScheduleEvent(Events.RocketStrike, 20000);
                        _events.ScheduleEvent(Events.SpinningUp, RandomHelper.URand(30000, 35000));
                        _events.ScheduleEvent(Events.HandPulse, 500, 0, Phases.Vol7ron);
                        if (_fireFighter)
                            _events.ScheduleEvent(Events.FrostBomb, 1000);
                        break;
                    default:
                        break;
                }
            }

            public override void EnterEvadeMode(EvadeReason why)
            {
                summons.DespawnAll();
            }

            public override void JustSummoned(Creature summon)
            {
                summons.Summon(summon);
                if (summon.GetEntry() == InstanceCreatureIds.BurstTarget)
                    summon.CastSpell(me, Spells.RapidBurstTargetMe);
            }

            public override void KilledUnit(Unit victim)
            {
                if (victim.IsTypeId(TypeId.Player))
                {
                    Creature mimiron = ObjectAccessor.GetCreature(me, instance.GetGuidData(BossIds.Mimiron));
                    if (mimiron)
                        mimiron.GetAI().Talk(_events.IsInPhase(Phases.Vx001) ? Yells.Vx001Slay : Yells.V07tronSlay);
                }
            }

            public override void SpellHit(Unit caster, SpellInfo spellProto)
            {
                if (caster.GetEntry() == InstanceCreatureIds.BurstTarget && !me.HasUnitState(UnitState.Casting))
                    DoCast(caster, Spells.RapidBurst);
            }

            public override void UpdateAI(uint diff)
            {
                if (!UpdateVictim())
                    return;

                _events.Update(diff);

                // Handle rotation during SPELL_SPINNING_UP, SPELL_P3WX2_LASER_BARRAGE, SPELL_RAPID_BURST, and SPELL_HAND_PULSE_LEFT/RIGHT
                if (me.HasUnitState(UnitState.Casting))
                {
                    List<ObjectGuid> channelObjects = me.GetChannelObjects();
                    Unit channelTarget = (channelObjects.Count == 1 ? Global.ObjAccessor.GetUnit(me, channelObjects[0]) : null);
                    if (channelTarget)
                        me.SetFacingToObject(channelTarget);
                    return;
                }

                _events.ExecuteEvents(eventId =>
                {
                    switch (eventId)
                    {
                        case Events.RapidBurst:
                            {
                                Unit target = SelectTarget(SelectAggroTarget.Random, 0, 120, true);
                                if (target)
                                    DoCast(target, Spells.SummonBurstTarget);
                                _events.RescheduleEvent(Events.RapidBurst, 3000, 0, Phases.Vx001);
                            }
                            break;
                        case Events.RocketStrike:
                            DoCastAOE(_events.IsInPhase(Phases.Vx001) ? Spells.RocketStrikeSingle : Spells.RocketStrikeBoth);
                            _events.ScheduleEvent(Events.Reload, 10000);
                            _events.RescheduleEvent(Events.RocketStrike, RandomHelper.URand(20000, 25000));
                            break;
                        case Events.Reload:
                            for (sbyte seat = (sbyte)SeatIds.RocketLeft; seat <= SeatIds.RocketRight; ++seat)
                            {
                                Unit rocket = me.GetVehicleKit().GetPassenger(seat);
                                if (rocket)
                                    rocket.SetDisplayId(rocket.GetNativeDisplayId());
                            }
                            break;
                        case Events.HandPulse:
                            {
                                Unit target = SelectTarget(SelectAggroTarget.Random, 0, 120, true);
                                if (target)
                                    DoCast(target, RandomHelper.RAND(Spells.HandPulseLeft, Spells.HandPulseRight));
                                _events.RescheduleEvent(Events.HandPulse, RandomHelper.URand(1500, 3000), 0, Phases.Vol7ron);
                            }
                            break;
                        case Events.FrostBomb:
                            DoCastAOE(Spells.ScriptEffectFrostBomb);
                            _events.RescheduleEvent(Events.FrostBomb, 45000);
                            break;
                        case Events.SpinningUp:
                            DoCastAOE(Spells.SpinningUp);
                            _events.DelayEvents(14000);
                            _events.RescheduleEvent(Events.SpinningUp, RandomHelper.URand(55000, 65000));
                            break;
                        case Events.FlameSuppressantVx:
                            DoCastAOE(Spells.FlameSuppressantVx);
                            _events.RescheduleEvent(Events.FlameSuppressantVx, RandomHelper.URand(10000, 12000), 0, Phases.Vx001);
                            break;
                        default:
                            break;
                    }
                });
            }

            bool _fireFighter;
        }

        [Script]
        class boss_aerial_command_unit : BossAI
        {
            public boss_aerial_command_unit(Creature creature) : base(creature, BossIds.Mimiron)
            {
                me.SetReactState(ReactStates.Passive);
                fireFigther = false;
            }

            public override void DamageTaken(Unit who, ref uint damage)
            {
                if (damage >= me.GetHealth())
                {
                    damage = (uint)(me.GetHealth() - 1); // Let creature fall to 1 hp, but do not let it die or damage itself with SetHealth().
                    me.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable);
                    me.AttackStop();
                    me.SetReactState(ReactStates.Passive);
                    DoCast(me, Spells.VehicleDamaged, true);
                    me.RemoveAllAurasExceptType(AuraType.ControlVehicle, AuraType.ModIncreaseHealthPercent);

                    if (_events.IsInPhase(Phases.AerialCommandUnit))
                    {
                        me.GetMotionMaster().Clear(true);
                        me.GetMotionMaster().MovePoint(Waypoints.AerialP4Pos, MimironConst.VehicleRelocation[Waypoints.AerialP4Pos]);
                    }
                    else if (_events.IsInPhase(Phases.Vol7ron))
                    {
                        me.SetStandState(UnitStandStateType.Dead);

                        if (MimironConst.IsEncounterFinished(who))
                            return;

                        me.CastStop();
                        DoCast(me, Spells.SelfRepair);
                    }
                    _events.Reset();
                }
            }

            public override void DoAction(int action)
            {
                switch (action)
                {
                    case Actions.HardmodeAerial:
                        fireFigther = true;
                        DoCast(me, Spells.EmergencyMode);
                        _events.ScheduleEvent(Events.SummonFireBots, 1000, 0, Phases.AerialCommandUnit);
                        goto case Actions.StartAerial;
                    case Actions.StartAerial:
                        me.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
                        me.SetReactState(ReactStates.Aggressive);

                        _events.SetPhase(Phases.AerialCommandUnit);
                        _events.ScheduleEvent(Events.SummonJunkBot, 5000, 0, Phases.AerialCommandUnit);
                        _events.ScheduleEvent(Events.SummonAssaultBot, 9000, 0, Phases.AerialCommandUnit);
                        _events.ScheduleEvent(Events.SummonBombBot, 9000, 0, Phases.AerialCommandUnit);
                        break;
                    case Actions.DisableAerial:
                        me.CastStop();
                        me.AttackStop();
                        me.SetReactState(ReactStates.Passive);
                        me.GetMotionMaster().MoveFall();
                        _events.DelayEvents(23000);
                        break;
                    case Actions.EnableAerial:
                        me.SetReactState(ReactStates.Aggressive);
                        break;
                    case Actions.AssembledCombat:
                        me.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
                        me.SetReactState(ReactStates.Aggressive);
                        me.SetStandState(UnitStandStateType.Stand);
                        _events.SetPhase(Phases.Vol7ron);
                        break;
                    default:
                        break;
                }
            }

            public override void EnterEvadeMode(EvadeReason why)
            {
                summons.DespawnAll();
            }

            public override void JustSummoned(Creature summon)
            {
                if (fireFigther && (summon.GetEntry() == InstanceCreatureIds.AssaultBot || summon.GetEntry() == InstanceCreatureIds.JunkBot))
                    summon.CastSpell(summon, Spells.EmergencyMode);
                base.JustSummoned(summon);
            }

            public override void KilledUnit(Unit victim)
            {
                if (victim.IsTypeId(TypeId.Player))
                {
                    Creature mimiron = ObjectAccessor.GetCreature(me, instance.GetGuidData(BossIds.Mimiron));
                    if (mimiron)
                        mimiron.GetAI().Talk(_events.IsInPhase(Phases.AerialCommandUnit) ? Yells.AerialSlay : Yells.V07tronSlay);
                }
            }

            public override void MovementInform(MovementGeneratorType type, uint point)
            {
                if (type == MovementGeneratorType.Point && point == Waypoints.AerialP4Pos)
                {
                    me.SetFlag(UnitFields.Flags, UnitFlags.NotSelectable);

                    Creature mimiron = ObjectAccessor.GetCreature(me, instance.GetGuidData(BossIds.Mimiron));
                    if (mimiron)
                        mimiron.GetAI().DoAction(Actions.ActivateV0l7r0n1);
                }
            }

            public override void UpdateAI(uint diff)
            {
                if (!UpdateVictim())
                    return;

                _events.Update(diff);

                if (me.HasUnitState(UnitState.Casting))
                    return;

                _events.ExecuteEvents(eventId =>
                {
                    switch (eventId)
                    {
                        case Events.SummonFireBots:
                            me.CastCustomSpell(Spells.SummonFireBotTrigger, SpellValueMod.MaxTargets, 3, null, true);
                            _events.RescheduleEvent(Events.SummonFireBots, 45000, 0, Phases.AerialCommandUnit);
                            break;
                        case Events.SummonJunkBot:
                            me.CastCustomSpell(Spells.SummonJunkBotTrigger, SpellValueMod.MaxTargets, 1, null, true);
                            _events.RescheduleEvent(Events.SummonJunkBot, RandomHelper.URand(11000, 12000), 0, Phases.AerialCommandUnit);
                            break;
                        case Events.SummonAssaultBot:
                            me.CastCustomSpell(Spells.SummonAssaultBotTrigger, SpellValueMod.MaxTargets, 1, null, true);
                            _events.RescheduleEvent(Events.SummonAssaultBot, 30000, 0, Phases.AerialCommandUnit);
                            break;
                        case Events.SummonBombBot:
                            DoCast(me, Spells.SummonBombBot);
                            _events.RescheduleEvent(Events.SummonBombBot, RandomHelper.URand(15000, 20000), 0, Phases.AerialCommandUnit);
                            break;
                        default:
                            break;
                    }
                });
                DoSpellAttackIfReady(_events.IsInPhase(Phases.AerialCommandUnit) ? Spells.PlasmaBallP1 : Spells.PlasmaBallP2);
            }

            bool fireFigther;
        }

        [Script]
        class npc_mimiron_assault_bot : ScriptedAI
        {
            public npc_mimiron_assault_bot(Creature creature) : base(creature) { }

            public override void EnterCombat(Unit who)
            {
                _scheduler.Schedule(TimeSpan.FromSeconds(14), task =>
                {
                    DoCastVictim(Spells.MagneticField);
                    me.ClearUnitState(UnitState.Casting);
                    task.Repeat(TimeSpan.FromSeconds(30));
                });
            }

            public override void UpdateAI(uint diff)
            {
                if (!UpdateVictim())
                    return;

                if (me.HasUnitState(UnitState.Root))
                {
                    Unit newTarget = SelectTarget(SelectAggroTarget.Nearest, 0, 30.0f, true);
                    if (newTarget)
                    {
                        me.DeleteThreatList();
                        AttackStart(newTarget);
                    }
                }

                _scheduler.Update(diff, DoMeleeAttackIfReady);
            }
        }

        [Script]
        class npc_mimiron_emergency_fire_bot : ScriptedAI
        {
            public npc_mimiron_emergency_fire_bot(Creature creature) : base(creature)
            {
                me.SetReactState(ReactStates.Passive);
                isWaterSprayReady = true;
                moveNew = true;
            }

            public override uint GetData(uint id)
            {
                if (id == Data.Waterspray)
                    return isWaterSprayReady ? 1 : 0u;
                if (id == Data.MoveNew)
                    return moveNew ? 1 : 0u;
                return 0;
            }

            public override void SetData(uint id, uint data)
            {
                if (id == Data.Waterspray)
                    isWaterSprayReady = false;
                else if (id == Data.MoveNew)
                    moveNew = data == 1;
            }

            public override void Reset()
            {
                _scheduler.Schedule(TimeSpan.FromSeconds(7), task =>
                {
                    isWaterSprayReady = true;
                    task.Repeat(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(9));
                });

                isWaterSprayReady = true;
                moveNew = true;
            }

            public override void UpdateAI(uint diff)
            {
                if (!isWaterSprayReady)
                    _scheduler.Update(diff);
            }

            bool isWaterSprayReady;
            bool moveNew;
        }

        [Script]
        class npc_mimiron_computer : ScriptedAI
        {
            public npc_mimiron_computer(Creature creature) : base(creature)
            {
                instance = me.GetInstanceScript();
            }

            public override void DoAction(int action)
            {
                switch (action)
                {
                    case Actions.ActivateComputer:
                        Talk(ComputerYells.SelfDestructInitiated);
                        _events.ScheduleEvent(Events.SelfDestruct10, 3000);
                        break;
                    case Actions.DeactivateComputer:
                        Talk(ComputerYells.SelfDestructTerminated);
                        me.RemoveAurasDueToSpell(Spells.SelfDestructionAura);
                        me.RemoveAurasDueToSpell(Spells.SelfDestructionVisual);
                        _events.Reset();
                        break;
                    default:
                        break;
                }
            }

            public override void UpdateAI(uint diff)
            {
                _events.Update(diff);
                _events.ExecuteEvents(eventId =>
                {
                    switch (eventId)
                    {
                        case Events.SelfDestruct10:
                            {
                                Talk(ComputerYells.SelfDestruct10);
                                Creature mimiron = ObjectAccessor.GetCreature(me, instance.GetGuidData(BossIds.Mimiron));
                                if (mimiron)
                                    mimiron.GetAI().DoAction(Actions.ActivateHardMode);
                                _events.ScheduleEvent(Events.SelfDestruct9, 60000);
                            }
                            break;
                        case Events.SelfDestruct9:
                            Talk(ComputerYells.SelfDestruct9);
                            _events.ScheduleEvent(Events.SelfDestruct8, 60000);
                            break;
                        case Events.SelfDestruct8:
                            Talk(ComputerYells.SelfDestruct8);
                            _events.ScheduleEvent(Events.SelfDestruct7, 60000);
                            break;
                        case Events.SelfDestruct7:
                            Talk(ComputerYells.SelfDestruct7);
                            _events.ScheduleEvent(Events.SelfDestruct6, 60000);
                            break;
                        case Events.SelfDestruct6:
                            Talk(ComputerYells.SelfDestruct6);
                            _events.ScheduleEvent(Events.SelfDestruct5, 60000);
                            break;
                        case Events.SelfDestruct5:
                            Talk(ComputerYells.SelfDestruct5);
                            _events.ScheduleEvent(Events.SelfDestruct4, 60000);
                            break;
                        case Events.SelfDestruct4:
                            Talk(ComputerYells.SelfDestruct4);
                            _events.ScheduleEvent(Events.SelfDestruct3, 60000);
                            break;
                        case Events.SelfDestruct3:
                            Talk(ComputerYells.SelfDestruct3);
                            _events.ScheduleEvent(Events.SelfDestruct2, 60000);
                            break;
                        case Events.SelfDestruct2:
                            Talk(ComputerYells.SelfDestruct2);
                            _events.ScheduleEvent(Events.SelfDestruct1, 60000);
                            break;
                        case Events.SelfDestruct1:
                            Talk(ComputerYells.SelfDestruct1);
                            _events.ScheduleEvent(Events.SelfDestructFinalized, 60000);
                            break;
                        case Events.SelfDestructFinalized:
                            {
                                Talk(ComputerYells.SelfDestructFinalized);
                                Creature mimiron = ObjectAccessor.GetCreature(me, instance.GetGuidData(BossIds.Mimiron));
                                if (mimiron)
                                    mimiron.GetAI().DoAction(Actions.ActivateSelfDestruct);
                                DoCast(me, Spells.SelfDestructionAura);
                                DoCast(me, Spells.SelfDestructionVisual);
                            }
                            break;
                        default:
                            break;
                    }
                });
            }

            InstanceScript instance;
        }

        [Script]
        class npc_mimiron_flames : ScriptedAI
        {
            public npc_mimiron_flames(Creature creature) : base(creature)
            {
                instance = me.GetInstanceScript();
            }

            public override void Reset() // Reset is possibly more suitable for this case.
            {
                _scheduler.Schedule(TimeSpan.FromSeconds(4), task =>
                {
                    DoCastAOE(Spells.SummonFlamesSpreadTrigger);
                });
            }

            public override void UpdateAI(uint diff)
            {
                if (instance.GetBossState(BossIds.Mimiron) != EncounterState.InProgress)
                    me.DespawnOrUnsummon();

                _scheduler.Update(diff);
            }

            InstanceScript instance;
        }

        [Script]
        class npc_mimiron_frost_bomb : ScriptedAI
        {
            public npc_mimiron_frost_bomb(Creature creature) : base(creature) { }

            public override void Reset()
            {
                _scheduler.Schedule(TimeSpan.FromSeconds(10), task =>
                {
                    DoCastAOE(Spells.FrostBombExplosion);

                    task.Schedule(TimeSpan.FromSeconds(3), () =>
                    {
                        DoCastAOE(Spells.ClearFires);
                        me.DespawnOrUnsummon(3000);
                    });
                });
            }

            public override void UpdateAI(uint diff)
            {
                _scheduler.Update(diff);
            }
        }

        [Script]
        class npc_mimiron_proximity_mine : ScriptedAI
        {
            public npc_mimiron_proximity_mine(Creature creature) : base(creature) { }

            public override void Reset()
            {
                _scheduler.Schedule(TimeSpan.FromSeconds(1.5), task =>
                {
                    DoCast(me, Spells.ProximityMinePeriodicTrigger);

                    task.Schedule(TimeSpan.FromSeconds(33.5), () =>
                    {
                        if (me.HasAura(Spells.ProximityMinePeriodicTrigger))
                            DoCastAOE(Spells.ProximityMineExplosion);
                        me.DespawnOrUnsummon(1000);
                    });
                });

            }

            public override void UpdateAI(uint diff)
            {
                _scheduler.Update(diff);
            }
        }

        [Script]
        class go_mimiron_hardmode_button : GameObjectScript
        {
            public go_mimiron_hardmode_button() : base("go_mimiron_hardmode_button") { }

            public override bool OnGossipHello(Player player, GameObject go)
            {
                if (go.HasFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable))
                    return true;

                InstanceScript instance = go.GetInstanceScript();
                if (instance == null)
                    return false;

                Creature computer = ObjectAccessor.GetCreature(go, instance.GetGuidData(InstanceData.Computer));
                if (computer)
                    computer.GetAI().DoAction(Actions.ActivateComputer);
                go.SetGoState(GameObjectState.Active);
                go.SetFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                return true;
            }
        }

        [Script] // 63801 - Bomb Bot
        class spell_mimiron_bomb_bot : SpellScript
        {
            void HandleScript(uint effIndex)
            {
                if (GetHitPlayer())
                {
                    InstanceScript instance = GetCaster().GetInstanceScript();
                    if (instance != null)
                    {
                        Creature mkii = ObjectAccessor.GetCreature(GetCaster(), instance.GetGuidData(InstanceData.LeviathanMKII));
                        if (mkii)
                            mkii.GetAI().SetData(Data.SetupBomb, 0);
                    }
                }
            }

            void HandleDespawn(uint effIndex)
            {
                Creature target = GetHitCreature();
                if (target)
                {
                    target.SetFlag(UnitFields.Flags, UnitFlags.NotSelectable | UnitFlags.Pacified);
                    target.DespawnOrUnsummon(1000);
                }
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.SchoolDamage));
                OnEffectHitTarget.Add(new EffectHandler(HandleDespawn, 1, SpellEffectName.ApplyAura));
            }
        }

        [Script] // 65192 - Flame Suppressant, 65224 - Clear Fires, 65354 - Clear Fires, 64619 - Water Spray
        class spell_mimiron_clear_fires : SpellScript
        {
            void HandleDummy(uint effIndex)
            {
                if (GetHitCreature())
                    GetHitCreature().DespawnOrUnsummon();
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
            }
        }

        [Script] // 64463 - Despawn Assault Bots
        class spell_mimiron_despawn_assault_bots : SpellScript
        {
            void HandleScript(uint effIndex)
            {
                if (GetHitCreature())
                    GetHitCreature().DespawnOrUnsummon();
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
            }
        }

        [Script] // 64618 - Fire Search
        class spell_mimiron_fire_search : SpellScript
        {
            public spell_mimiron_fire_search()
            {
                _noTarget = false;
            }

            public override bool Validate(SpellInfo spell)
            {
                return ValidateSpellInfo(Spells.WaterSpray);
            }

            void FilterTargets(List<WorldObject> targets)
            {
                _noTarget = targets.Empty();
                if (_noTarget)
                    return;

                WorldObject target = targets.SelectRandom();
                targets.Clear();
                targets.Add(target);
            }

            void HandleAftercast()
            {
                if (_noTarget)
                    GetCaster().GetMotionMaster().MoveRandom(15.0f);
            }

            void HandleScript(uint effIndex)
            {
                Unit caster = GetCaster();
                UnitAI ai = caster.GetAI();
                if (ai != null)
                {
                    if (caster.GetDistance2d(GetHitUnit()) <= 15.0f && ai.GetData(Data.Waterspray) != 0)
                    {
                        caster.CastSpell(GetHitUnit(), Spells.WaterSpray, true);
                        ai.SetData(Data.Waterspray, 0);
                        ai.SetData(Data.MoveNew, 1);
                    }
                    else if (caster.GetAI().GetData(Data.MoveNew) != 0)
                    {
                        caster.GetMotionMaster().MoveChase(GetHitUnit());
                        ai.SetData(Data.MoveNew, 0);
                    }
                }
            }

            public override void Register()
            {
                AfterCast.Add(new CastHandler(HandleAftercast));
                OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEntry));
                OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
            }

            bool _noTarget;
        }

        [Script] // 64436 - Magnetic Core
        class spell_mimiron_magnetic_core : SpellScript
        {
            void FilterTargets(List<WorldObject> targets)
            {
                targets.RemoveAll(obj => obj.ToUnit() && (obj.ToUnit().GetVehicleBase() || obj.HasFlag(UnitFields.Flags, UnitFlags.NonAttackable)));
            }

            public override void Register()
            {
                OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitSrcAreaEntry));
            }
        }

        [Script]
        class spell_mimiron_magnetic_core_AuraScript : AuraScript
        {
            public override bool Validate(SpellInfo spell)
            {
                return ValidateSpellInfo(Spells.MagneticCoreVisual);
            }

            void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
            {
                Creature target = GetTarget().ToCreature();
                if (target)
                {
                    target.GetAI().DoAction(Actions.DisableAerial);
                    target.CastSpell(target, Spells.MagneticCoreVisual, true);
                }
            }

            void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
            {
                Creature target = GetTarget().ToCreature();
                if (target)
                {
                    target.GetAI().DoAction(Actions.EnableAerial);
                    target.RemoveAurasDueToSpell(Spells.MagneticCoreVisual);
                }
            }

            void OnRemoveSelf(AuraEffect aurEff, AuraEffectHandleModes mode)
            {
                TempSummon summ = GetTarget().ToTempSummon();
                if (summ)
                    summ.DespawnOrUnsummon();
            }

            public override void Register()
            {
                AfterEffectApply.Add(new EffectApplyHandler(OnApply, 1, AuraType.ModDamagePercentTaken, AuraEffectHandleModes.Real));
                AfterEffectRemove.Add(new EffectApplyHandler(OnRemove, 1, AuraType.ModDamagePercentTaken, AuraEffectHandleModes.Real));
                AfterEffectRemove.Add(new EffectApplyHandler(OnRemoveSelf, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            }
        }

        [Script] // 63667 - Napalm Shell
        class spell_mimiron_napalm_shell : SpellScript
        {
            public override bool Validate(SpellInfo spell)
            {
                return ValidateSpellInfo(Spells.NapalmShell);
            }

            void FilterTargets(List<WorldObject> targets)
            {
                if (targets.Empty())
                    return;

                WorldObject target = targets.SelectRandom();

                targets.RemoveAll(new AllWorldObjectsInRange(GetCaster(), 15.0f).Invoke);

                if (!targets.Empty())
                    target = targets.SelectRandom();

                targets.Clear();
                targets.Add(target);
            }

            void HandleScript(uint effIndex)
            {
                GetCaster().CastSpell(GetHitUnit(), Spells.NapalmShell);
            }

            public override void Register()
            {
                OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
                OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
            }
        }

        [Script] // 64542 - Plasma Blast
        class spell_mimiron_plasma_blast : SpellScript
        {
            public override bool Validate(SpellInfo spell)
            {
                return ValidateSpellInfo(Spells.PlasmaBlast);
            }

            public override bool Load()
            {
                return GetCaster().GetVehicleKit() != null;
            }

            void HandleScript(uint effIndex)
            {
                Unit caster = GetCaster().GetVehicleKit().GetPassenger(3);
                if (caster)
                    caster.CastSpell(GetHitUnit(), Spells.PlasmaBlast);
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
            }
        }

        [Script] // 66351 - Explosion
        class spell_mimiron_proximity_explosion : SpellScript
        {
            public void onHit(uint effIndex)
            {
                if (GetHitPlayer())
                {
                    InstanceScript instance = GetCaster().GetInstanceScript();
                    if (instance != null)
                    {
                        Creature mkII = ObjectAccessor.GetCreature(GetCaster(), instance.GetGuidData(InstanceData.LeviathanMKII));
                        if (mkII)
                            mkII.GetAI().SetData(Data.SetupMine, 0);
                    }
                }
            }

            void HandleAura(uint effIndex)
            {
                GetCaster().RemoveAurasDueToSpell(Spells.ProximityMinePeriodicTrigger);
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(onHit, 0, SpellEffectName.SchoolDamage));
                OnEffectHitTarget.Add(new EffectHandler(HandleAura, 1, SpellEffectName.ApplyAura));
            }
        }

        [Script] // 63027 - Proximity Mines
        class spell_mimiron_proximity_mines : SpellScript
        {
            public override bool Validate(SpellInfo spell)
            {
                return ValidateSpellInfo(Spells.SummonProximityMine);
            }

            void HandleScript(uint effIndex)
            {
                for (byte i = 0; i < 10; ++i)
                    GetCaster().CastSpell(GetCaster(), Spells.SummonProximityMine, true);
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
            }
        }

        [Script] // 65346 - Proximity Mine
        class spell_mimiron_proximity_trigger : SpellScript
        {
            public override bool Validate(SpellInfo spell)
            {
                return ValidateSpellInfo(Spells.ProximityMineExplosion);
            }

            void FilterTargets(List<WorldObject> targets)
            {
                targets.Remove(GetExplTargetWorldObject());

                if (targets.Empty())
                    FinishCast(SpellCastResult.NoValidTargets);
            }

            void HandleDummy(uint effIndex)
            {
                GetCaster().CastSpell((Unit)null, Spells.ProximityMineExplosion, true);
            }

            public override void Register()
            {
                OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEntry));
                OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
            }
        }

        [Script] // 63382 - Rapid Burst
        class spell_mimiron_rapid_burst : AuraScript
        {
            public override bool Validate(SpellInfo spell)
            {
                return ValidateSpellInfo(Spells.RapidBurstLeft, Spells.RapidBurstRight);
            }

            void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
            {
                TempSummon summ = GetTarget().ToTempSummon();
                if (summ)
                    summ.DespawnOrUnsummon();
            }

            void HandleDummyTick(AuraEffect aurEff)
            {
                if (GetCaster())
                    GetCaster().CastSpell(GetTarget(), aurEff.GetTickNumber() % 2 == 0 ? Spells.RapidBurstRight : Spells.RapidBurstLeft, true, null, aurEff);
            }

            public override void Register()
            {
                AfterEffectRemove.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real));
                OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleDummyTick, 1, AuraType.PeriodicDummy));
            }
        }

        [Script] // 64402 - Rocket Strike, 65034 - Rocket Strike
        class spell_mimiron_rocket_strike : SpellScript
        {
            public override bool Validate(SpellInfo spell)
            {
                return ValidateSpellInfo(Spells.ScriptEffectRocketStrike);
            }

            void FilterTargets(List<WorldObject> targets)
            {
                if (targets.Empty())
                    return;

                if (m_scriptSpellId == Spells.RocketStrikeSingle && GetCaster().IsVehicle())
                {
                    WorldObject target = GetCaster().GetVehicleKit().GetPassenger(RandomHelper.RAND(SeatIds.RocketLeft, SeatIds.RocketRight));
                    if (target)
                    {
                        targets.Clear();
                        targets.Add(target);
                    }
                }
            }

            void HandleDummy(uint effIndex)
            {
                GetHitUnit().CastSpell((Unit)null, Spells.ScriptEffectRocketStrike, true, null, null, GetCaster().GetGUID());
            }

            public override void Register()
            {
                OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEntry));
                OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
            }
        }

        [Script] // 63041 - Rocket Strike
        class spell_mimiron_rocket_strike_damage : SpellScript
        {
            public override bool Validate(SpellInfo spell)
            {
                return ValidateSpellInfo(Spells.NotSoFriendlyFire);
            }

            void HandleAfterCast()
            {
                TempSummon summ = GetCaster().ToTempSummon();
                if (summ)
                    summ.DespawnOrUnsummon();
            }

            void HandleScript(uint effIndex)
            {
                if (GetHitPlayer())
                {
                    InstanceScript instance = GetCaster().GetInstanceScript();
                    if (instance != null)
                    {
                        Creature mkii = ObjectAccessor.GetCreature(GetCaster(), instance.GetGuidData(InstanceData.LeviathanMKII));
                        if (mkii)
                            mkii.GetAI().SetData(Data.SetupRocket, 0);
                    }
                }
            }

            void HandleFriendlyFire(uint effIndex)
            {
                GetHitUnit().CastSpell((Unit)null, Spells.NotSoFriendlyFire, true);
            }

            public override void Register()
            {
                AfterCast.Add(new CastHandler(HandleAfterCast));
                OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.SchoolDamage));
                OnEffectHitTarget.Add(new EffectHandler(HandleFriendlyFire, 1, SpellEffectName.SchoolDamage));
            }
        }

        [Script] // 63681 - Rocket Strike
        class spell_mimiron_rocket_strike_target_select : SpellScript
        {
            public override bool Validate(SpellInfo spell)
            {
                return ValidateSpellInfo(Spells.SummonRocketStrike);
            }

            void FilterTargets(List<WorldObject> targets)
            {
                if (targets.Empty())
                    return;

                WorldObject target = targets.SelectRandom();

                targets.RemoveAll(new AllWorldObjectsInRange(GetCaster(), 15.0f).Invoke);

                if (!targets.Empty())
                    target = targets.SelectRandom();

                targets.Clear();
                targets.Add(target);
            }

            void HandleScript(uint effIndex)
            {
                InstanceScript instance = GetCaster().GetInstanceScript();
                if (instance != null)
                    GetCaster().CastSpell(GetHitUnit(), Spells.SummonRocketStrike, true, null, null, instance.GetGuidData(InstanceData.VX001));
                GetCaster().SetDisplayId(11686);
            }

            public override void Register()
            {
                OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
                OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
            }
        }

        [Script] // 64383 - Self Repair
        class spell_mimiron_self_repair : SpellScript
        {
            void HandleScript()
            {
                if (GetCaster().GetAI() != null)
                    GetCaster().GetAI().DoAction(Actions.AssembledCombat);
            }

            public override void Register()
            {
                AfterHit.Add(new HitHandler(HandleScript));
            }
        }

        [Script] // 64426 - Summon Scrap Bot
        class spell_mimiron_summon_assault_bot : AuraScript
        {
            public override bool Validate(SpellInfo spell)
            {
                return ValidateSpellInfo(Spells.SummonAssaultBot);
            }

            void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
            {
                Unit caster = GetCaster();
                if (caster)
                {
                    InstanceScript instance = caster.GetInstanceScript();
                    if (instance != null)
                        if (instance.GetBossState(BossIds.Mimiron) == EncounterState.InProgress)
                            caster.CastSpell(caster, Spells.SummonAssaultBot, false, null, aurEff, instance.GetGuidData(InstanceData.AerialCommandUnit));
                }
            }

            public override void Register()
            {
                OnEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            }
        }

        [Script] // 64425 - Summon Scrap Bot Trigger
        class spell_mimiron_summon_assault_bot_target : SpellScript
        {
            public override bool Validate(SpellInfo spell)
            {
                return ValidateSpellInfo(Spells.SummonAssaultBotDummy);
            }

            void HandleDummy(uint effIndex)
            {
                GetHitUnit().CastSpell(GetHitUnit(), Spells.SummonAssaultBotDummy, true);
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
            }
        }

        [Script] // 64621 - Summon Fire Bot
        class spell_mimiron_summon_fire_bot : AuraScript
        {
            public override bool Validate(SpellInfo spell)
            {
                return ValidateSpellInfo(Spells.SummonFireBot);
            }

            void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
            {
                Unit caster = GetCaster();
                if (caster)
                {
                    InstanceScript instance = caster.GetInstanceScript();
                    if (instance != null)
                        if (instance.GetBossState(BossIds.Mimiron) == EncounterState.InProgress)
                            caster.CastSpell(caster, Spells.SummonFireBot, false, null, aurEff, instance.GetGuidData(InstanceData.AerialCommandUnit));
                }
            }

            public override void Register()
            {
                OnEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real))
                    ;
            }
        }

        [Script] // 64620 - Summon Fire Bot Trigger
        class spell_mimiron_summon_fire_bot_target : SpellScript
        {
            public override bool Validate(SpellInfo spell)
            {
                return ValidateSpellInfo(Spells.SummonFireBotDummy);
            }

            void HandleDummy(uint effIndex)
            {
                GetHitUnit().CastSpell(GetHitUnit(), Spells.SummonFireBotDummy, true);
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
            }
        }

        [Script] // 64562 - Summon Flames Spread Trigger
        class spell_mimiron_summon_flames_spread : SpellScript
        {
            void FilterTargets(List<WorldObject> targets)
            {
                if (targets.Empty())
                    return;

                // Flames must chase the closest player
                WorldObject target = targets.First();

                foreach (var iter in targets)
                    if (GetCaster().GetDistance2d(iter) < GetCaster().GetDistance2d(target))
                        target = iter;

                targets.Clear();
                targets.Add(target);
            }

            public void onHit(uint effIndex)
            {
                GetCaster().SetInFront(GetHitUnit());
            }

            public override void Register()
            {
                OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
                OnEffectHitTarget.Add(new EffectHandler(onHit, 0, SpellEffectName.ApplyAura));
            }
        }

        class spell_mimiron_summon_flames_spread_AuraScript : AuraScript
        {
            public override bool Validate(SpellInfo spell)
            {
                return ValidateSpellInfo(Spells.SummonFlamesSpread);
            }

            void HandleTick(AuraEffect aurEff)
            {
                PreventDefaultAction();
                Unit caster = GetCaster();
                if (caster)
                    if (caster.HasAura(Spells.FlamesPeriodicTrigger))
                        caster.CastSpell(GetTarget(), Spells.SummonFlamesSpread, true);
            }

            public override void Register()
            {
                OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleTick, 0, AuraType.PeriodicTriggerSpell));
            }
        }

        [Script] // 64623 - Frost Bomb
        class spell_mimiron_summon_frost_bomb_target : SpellScript
        {
            public override bool Validate(SpellInfo spell)
            {
                return ValidateSpellInfo(Spells.SummonFrostBomb);
            }

            void FilterTargets(List<WorldObject> targets)
            {
                if (targets.Empty())
                    return;

                targets.RemoveAll(new AllWorldObjectsInRange(GetCaster(), 15.0f).Invoke);

                if (targets.Empty())
                    return;

                WorldObject target = targets.SelectRandom();

                targets.Clear();
                targets.Add(target);
            }

            void HandleScript(uint effIndex)
            {
                GetCaster().CastSpell(GetHitUnit(), Spells.SummonFrostBomb, true);
            }

            public override void Register()
            {
                OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEntry));
                OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
            }
        }

        [Script] // 64398 - Summon Scrap Bot
        class spell_mimiron_summon_junk_bot : AuraScript
        {
            public override bool Validate(SpellInfo spell)
            {
                return ValidateSpellInfo(Spells.SummonJunkBot);
            }

            void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
            {
                Unit caster = GetCaster();
                if (caster)
                {
                    InstanceScript instance = caster.GetInstanceScript();
                    if (instance != null)
                        if (instance.GetBossState(BossIds.Mimiron) == EncounterState.InProgress)
                            caster.CastSpell(caster, Spells.SummonJunkBot, false, null, aurEff, instance.GetGuidData(InstanceData.AerialCommandUnit));
                }
            }

            public override void Register()
            {
                OnEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            }
        }

        [Script] // 63820 - Summon Scrap Bot Trigger
        class spell_mimiron_summon_junk_bot_target : SpellScript
        {
            public override bool Validate(SpellInfo spell)
            {
                return ValidateSpellInfo(Spells.SummonJunkBotDummy);
            }

            void HandleDummy(uint effIndex)
            {
                GetHitUnit().CastSpell(GetHitUnit(), Spells.SummonJunkBotDummy, true);
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
            }
        }

        [Script] // 63339 - Weld
        class spell_mimiron_weld : AuraScript
        {
            void HandleTick(AuraEffect aurEff)
            {
                Unit caster = GetTarget();
                Unit vehicle = caster.GetVehicleBase();
                if (vehicle)
                {
                    if (aurEff.GetTickNumber() % 5 == 0)
                        caster.CastSpell(vehicle, MimironConst.RepairSpells[RandomHelper.IRand(0, 3)]);
                    //caster.SetFacingToObject(vehicle);
                }
            }

            public override void Register()
            {
                OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleTick, 0, AuraType.PeriodicTriggerSpell));
            }
        }

        [Script]
        class achievement_setup_boom : AchievementCriteriaScript
        {
            public achievement_setup_boom() : base("achievement_setup_boom") { }

            public override bool OnCheck(Player source, Unit target)
            {
                return target && target.GetAI().GetData(Data.SetupBomb) != 0;
            }
        }

        [Script]
        class achievement_setup_mine : AchievementCriteriaScript
        {
            public achievement_setup_mine() : base("achievement_setup_mine") { }

            public override bool OnCheck(Player source, Unit target)
            {
                return target && target.GetAI().GetData(Data.SetupMine) != 0;
            }
        }

        [Script]
        class achievement_setup_rocket : AchievementCriteriaScript
        {
            public achievement_setup_rocket() : base("achievement_setup_rocket") { }

            public override bool OnCheck(Player source, Unit target)
            {
                return target && target.GetAI().GetData(Data.SetupRocket) != 0;
            }
        }

        [Script]
        class achievement_firefighter : AchievementCriteriaScript
        {
            public achievement_firefighter() : base("achievement_firefighter") { }

            public override bool OnCheck(Player source, Unit target)
            {
                return target && target.GetAI().GetData(Data.Firefighter) != 0;
            }
        }
    }
}
