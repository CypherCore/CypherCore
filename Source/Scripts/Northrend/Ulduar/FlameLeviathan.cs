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
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.Northrend.Ulduar.FlameLeviathan
{
    struct Spells
    {
        public const uint Pursued = 62374;
        public const uint GatheringSpeed = 62375;
        public const uint BatteringRam = 62376;
        public const uint FlameVents = 62396;
        public const uint MissileBarrage = 62400;
        public const uint SystemsShutdown = 62475;
        public const uint OverloadCircuit = 62399;
        public const uint StartTheEngine = 62472;
        public const uint SearingFlame = 62402;
        public const uint Blaze = 62292;
        public const uint TarPassive = 62288;
        public const uint SmokeTrail = 63575;
        public const uint Electroshock = 62522;
        public const uint Napalm = 63666;
        public const uint InvisAndStealthDetect = 18950; // Passive
                                                         //Tower Additional Spells
        public const uint ThorimSHammer = 62911; // Tower Of Storms
        public const uint MimironSInferno = 62909; // Tower Of Flames
        public const uint HodirSFury = 62533; // Tower Of Frost
        public const uint FreyaSWard = 62906; // Tower Of Nature
        public const uint FreyaSummons = 62947; // Tower Of Nature
                                                //Tower Ap & Health Spells
        public const uint BuffTowerOfStorms = 65076;
        public const uint BuffTowerOfFlames = 65075;
        public const uint BuffTowerOfFrost = 65077;
        public const uint BuffTowerOfLife = 64482;
        //Additional Spells
        public const uint Lash = 65062;
        public const uint FreyaSWardEffect1 = 62947;
        public const uint FreyaSWardEffect2 = 62907;
        public const uint AutoRepair = 62705;
        public const uint DummyBlue = 63294;
        public const uint DummyGreen = 63295;
        public const uint DummyYellow = 63292;
        public const uint LiquidPyrite = 62494;
        public const uint DustyExplosion = 63360;
        public const uint DustCloudImpact = 54740;
        public const uint StealthDetection = 18950;
        public const uint RideVehicle = 46598;
    }

    struct CreatureIds
    {
        public const uint Seat = 33114;
        public const uint Mechanolift = 33214;
        public const uint Liquid = 33189;
        public const uint Container = 33218;
        public const uint ThorimBeacon = 33365;
        public const uint MimironBeacon = 33370;
        public const uint HodirBeacon = 33212;
        public const uint FreyaBeacon = 33367;
        public const uint ThorimTargetBeacon = 33364;
        public const uint MimironTargetBeacon = 33369;
        public const uint HodirTargetBeacon = 33108;
        public const uint FreyaTargetBeacon = 33366;
        public const uint Lorekeeper = 33686; // Hard Mode Starter
        public const uint BranzBronzbeard = 33579;
        public const uint Delorah = 33701;
        public const uint UlduarGauntletGenerator = 33571; // Trigger Tied To Towers
    }

    struct Towers
    {
        public const uint ofStorms = 194377;
        public const uint ofFlames = 194371;
        public const uint ofFrost = 194370;
        public const uint ofLife = 194375;
    }

    struct Events
    {
        public const int Pursue = 1;
        public const int Missile = 2;
        public const int Vent = 3;
        public const int Speed = 4;
        public const int Summon = 5;
        public const int Shutdown = 6;
        public const int Repair = 7;
        public const int ThorimSHammer = 8;    // Tower Of Storms
        public const int MimironSInferno = 9;    // Tower Of Flames
        public const int HodirSFury = 10;   // Tower Of Frost
        public const int FreyaSWard = 11;   // Tower Of Nature
    }

    struct Seats
    {
        public const int Player = 0;
        public const int Turret = 1;
        public const int Device = 2;
        public const int Cannon = 7;
    }

    struct Vehicles
    {
        public const uint Siege = 33060;
        public const uint Chopper = 33062;
        public const uint Demolisher = 33109;
    }

    struct Leviathan
    {
        public const int DataShutout = 29112912; // 2911, 2912 are achievement IDs
        public const int DataOrbitAchievements = 1;
        public const int VehicleSpawns = 5;
        public const int FreyaSpawns = 4;

        public const int ActionStartHardMode = 5;
        public const int ActionSpawnVehicles = 6;
        // Amount of seats depending on Raid mode
        public const int TwoSeats = 2;
        public const int FourSeats = 4;

        //Postions
        public static Position Center = new Position(354.8771f, -12.90240f, 409.803650f);
        public static Position InfernoStart = new Position(390.93f, -13.91f, 409.81f);

        public static Position[] PosSiege =
        {
                new Position(-814.59f, -64.54f, 429.92f, 5.969f),
                new Position(-784.37f, -33.31f, 429.92f, 5.096f),
                new Position(-808.99f, -52.10f, 429.92f, 5.668f),
                new Position(-798.59f, -44.00f, 429.92f, 5.663f),
                new Position(-812.83f, -77.71f, 429.92f, 0.046f),
            };

        public static Position[] PosChopper =
        {
                new Position(-717.83f, -106.56f, 430.02f, 0.122f),
                new Position(-717.83f, -114.23f, 430.44f, 0.122f),
                new Position(-717.83f, -109.70f, 430.22f, 0.122f),
                new Position(-718.45f, -118.24f, 430.26f, 0.052f),
                new Position(-718.45f, -123.58f, 430.41f, 0.085f),
            };

        public static Position[] PosDemolisher =
        {
                new Position(-724.12f, -176.64f, 430.03f, 2.543f),
                new Position(-766.70f, -225.03f, 430.50f, 1.710f),
                new Position(-729.54f, -186.26f, 430.12f, 1.902f),
                new Position(-756.01f, -219.23f, 430.50f, 2.369f),
                new Position(-798.01f, -227.24f, 429.84f, 1.446f),
            };

        public static Position[] FreyaBeacons =
        {
                new Position(377.02f, -119.10f, 409.81f),
                new Position(185.62f, -119.10f, 409.81f),
                new Position(377.02f, 54.78f, 409.81f),
                new Position(185.62f, 54.78f, 409.81f),
            };
    }

    struct TextIds
    {
        public const uint Aggro = 0;
        public const uint Slay = 1;
        public const uint Death = 2;
        public const uint Target = 3;
        public const uint Hardmode = 4;
        public const uint TowerNone = 5;
        public const uint TowerFrost = 6;
        public const uint TowerFlame = 7;
        public const uint TowerNature = 8;
        public const uint TowerStorm = 9;
        public const uint PlayerRiding = 10;
        public const uint Overload = 11;
        public const uint EmotePursue = 12;
        public const uint EmoteOverload = 13;
        public const int EmoteRepair = 14;
    }

    struct GossipIds
    {
        //LoreKeeperGossips
        public const int MenuLoreKeeper = 10477;
        public const int OptionLoreKeeper = 0;
    }

    [Script]
    class boss_flame_leviathan : BossAI
    {
        public boss_flame_leviathan(Creature creature) : base(creature, BossIds.Leviathan)
        {
            vehicle = creature.GetVehicleKit();
        }

        public override void InitializeAI()
        {
            Cypher.Assert(vehicle);
            if (!me.IsDead())
                Reset();

            ActiveTowersCount = 4;
            Shutdown = 0;
            ActiveTowers = false;
            towerOfStorms = false;
            towerOfLife = false;
            towerOfFlames = false;
            towerOfFrost = false;
            Shutout = true;
            Unbroken = true;

            DoCast(Spells.InvisAndStealthDetect);

            me.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable | UnitFlags.Stunned);
            me.SetReactState(ReactStates.Passive);
        }

        public override void Reset()
        {
            _Reset();
            //resets shutdown counter to 0.  2 or 4 depending on raid mode
            Shutdown = 0;
            _pursueTarget.Clear();

            me.SetReactState(ReactStates.Defensive);
        }

        public override void EnterCombat(Unit who)
        {
            _EnterCombat();
            me.SetReactState(ReactStates.Passive);
            _events.ScheduleEvent(Events.Pursue, 1);
            _events.ScheduleEvent(Events.Missile, RandomHelper.URand(1500, 4 * Time.InMilliseconds));
            _events.ScheduleEvent(Events.Vent, 20 * Time.InMilliseconds);
            _events.ScheduleEvent(Events.Shutdown, 150 * Time.InMilliseconds);
            _events.ScheduleEvent(Events.Speed, 15 * Time.InMilliseconds);
            _events.ScheduleEvent(Events.Summon, 1 * Time.InMilliseconds);
            ActiveTower(); //void ActiveTower
        }

        void ActiveTower()
        {
            if (ActiveTowers)
            {
                if (towerOfStorms)
                {
                    me.AddAura(Spells.BuffTowerOfStorms, me);
                    _events.ScheduleEvent(Events.ThorimSHammer, 35 * Time.InMilliseconds);
                }

                if (towerOfFlames)
                {
                    me.AddAura(Spells.BuffTowerOfFlames, me);
                    _events.ScheduleEvent(Events.MimironSInferno, 70 * Time.InMilliseconds);
                }

                if (towerOfFrost)
                {
                    me.AddAura(Spells.BuffTowerOfFrost, me);
                    _events.ScheduleEvent(Events.HodirSFury, 105 * Time.InMilliseconds);
                }

                if (towerOfLife)
                {
                    me.AddAura(Spells.BuffTowerOfLife, me);
                    _events.ScheduleEvent(Events.FreyaSWard, 140 * Time.InMilliseconds);
                }

                if (!towerOfLife && !towerOfFrost && !towerOfFlames && !towerOfStorms)
                    Talk(TextIds.TowerNone);
                else
                    Talk(TextIds.Hardmode);
            }
            else
                Talk(TextIds.Aggro);
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
            // Set Field Flags 67108928 = 64 | 67108864 = UNIT_FLAG_UNK_6 | UNIT_FLAG_SKINNABLE
            // Set DynFlags 12
            // Set NPCFlags 0
            Talk(TextIds.Death);
        }

        public override void SpellHit(Unit caster, SpellInfo spell)
        {
            if (spell.Id == Spells.StartTheEngine)
                vehicle.InstallAllAccessories(false);

            if (spell.Id == Spells.Electroshock)
                me.InterruptSpell(CurrentSpellTypes.Channeled);

            if (spell.Id == Spells.OverloadCircuit)
                ++Shutdown;
        }

        public override uint GetData(uint type)
        {
            switch (type)
            {
                case Leviathan.DataShutout:
                    return (uint)(Shutout ? 1 : 0);
                case InstanceAchievementData.DataUnbroken:
                    return (uint)(Unbroken ? 1 : 0);
                case Leviathan.DataOrbitAchievements:
                    if (ActiveTowers) // Only on HardMode
                        return ActiveTowersCount;
                    break;
                default:
                    break;
            }

            return 0;
        }

        public override void SetData(uint id, uint data)
        {
            if (id == InstanceAchievementData.DataUnbroken)
                Unbroken = data != 0 ? true : false;
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _events.Update(diff);

            if (Shutdown == RaidMode(Leviathan.TwoSeats, Leviathan.FourSeats))
            {
                Shutdown = 0;
                _events.ScheduleEvent(Events.Shutdown, 4000);
                me.RemoveAurasDueToSpell(Spells.OverloadCircuit);
                me.InterruptNonMeleeSpells(true);
                return;
            }

            if (me.HasUnitState(UnitState.Casting))
                return;
            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case Events.Pursue:
                        Talk(TextIds.Target);
                        DoCast(Spells.Pursued);  // Will select target in spellscript
                        _events.ScheduleEvent(Events.Pursue, 35 * Time.InMilliseconds);
                        break;
                    case Events.Missile:
                        DoCast(me, Spells.MissileBarrage, true);
                        _events.ScheduleEvent(Events.Missile, 2 * Time.InMilliseconds);
                        break;
                    case Events.Vent:
                        DoCastAOE(Spells.FlameVents);
                        _events.ScheduleEvent(Events.Vent, 20 * Time.InMilliseconds);
                        break;
                    case Events.Speed:
                        DoCastAOE(Spells.GatheringSpeed);
                        _events.ScheduleEvent(Events.Speed, 15 * Time.InMilliseconds);
                        break;
                    case Events.Summon:
                        if (summons.Count < 15)
                        {
                            Creature lift = DoSummonFlyer(CreatureIds.Mechanolift, me, 30.0f, 50.0f, 0);
                            if (lift)
                                lift.GetMotionMaster().MoveRandom(100);
                        }
                        _events.ScheduleEvent(Events.Summon, 2 * Time.InMilliseconds);
                        break;
                    case Events.Shutdown:
                        Talk(TextIds.Overload);
                        Talk(TextIds.EmoteOverload);
                        me.CastSpell(me, Spells.SystemsShutdown, true);
                        if (Shutout)
                            Shutout = false;
                        _events.ScheduleEvent(Events.Repair, 4000);
                        _events.DelayEvents(20 * Time.InMilliseconds, 0);
                        break;
                    case Events.Repair:
                        Talk(TextIds.EmoteRepair);
                        me.ClearUnitState(UnitState.Stunned | UnitState.Root);
                        _events.ScheduleEvent(Events.Shutdown, 150 * Time.InMilliseconds);
                        _events.CancelEvent(Events.Repair);
                        break;
                    case Events.ThorimSHammer: // Tower of Storms
                        for (byte i = 0; i < 7; ++i)
                        {
                            Creature thorim = DoSummon(CreatureIds.ThorimBeacon, me, RandomHelper.URand(20, 60), 20000, TempSummonType.TimedDespawn);
                            if (thorim)
                                thorim.GetMotionMaster().MoveRandom(100);
                        }
                        Talk(TextIds.TowerStorm);
                        _events.CancelEvent(Events.ThorimSHammer);
                        break;
                    case Events.MimironSInferno: // Tower of Flames
                        me.SummonCreature(CreatureIds.MimironBeacon, Leviathan.InfernoStart);
                        Talk(TextIds.TowerFlame);
                        _events.CancelEvent(Events.MimironSInferno);
                        break;
                    case Events.HodirSFury:      // Tower of Frost
                        for (byte i = 0; i < 7; ++i)
                        {
                            Creature hodir = DoSummon(CreatureIds.HodirBeacon, me, 50f, 0);
                            if (hodir)
                                hodir.GetMotionMaster().MoveRandom(100);
                        }
                        Talk(TextIds.TowerFrost);
                        _events.CancelEvent(Events.HodirSFury);
                        break;
                    case Events.FreyaSWard:    // Tower of Nature
                        Talk(TextIds.TowerNature);
                        for (int i = 0; i < 4; ++i)
                            me.SummonCreature(CreatureIds.FreyaBeacon, Leviathan.FreyaBeacons[i]);

                        Unit target = SelectTarget(SelectAggroTarget.Random);
                        if (target)
                            DoCast(target, Spells.FreyaSWard);
                        _events.CancelEvent(Events.FreyaSWard);
                        break;
                }
            });

            DoBatteringRamIfReady();
        }

        public override void SpellHitTarget(Unit target, SpellInfo spell)
        {
            if (spell.Id == Spells.Pursued)
                _pursueTarget = target.GetGUID();
        }

        public override void DoAction(int action)
        {
            if (action != 0 && action <= 4) // Tower destruction, debuff leviathan loot and reduce active tower count
            {
                if (me.HasLootMode(LootModes.Default | LootModes.HardMode1 | LootModes.HardMode2 | LootModes.HardMode3 | LootModes.HardMode4) && ActiveTowersCount == 4)
                    me.RemoveLootMode(LootModes.HardMode4);

                if (me.HasLootMode(LootModes.Default | LootModes.HardMode1 | LootModes.HardMode2 | LootModes.HardMode3) && ActiveTowersCount == 3)
                    me.RemoveLootMode(LootModes.HardMode3);

                if (me.HasLootMode(LootModes.Default | LootModes.HardMode1 | LootModes.HardMode2) && ActiveTowersCount == 2)
                    me.RemoveLootMode(LootModes.HardMode2);

                if (me.HasLootMode(LootModes.Default | LootModes.HardMode1) && ActiveTowersCount == 1)
                    me.RemoveLootMode(LootModes.HardMode1);
            }

            switch (action)
            {
                case LeviathanActions.TowerOfStormDestroyed:
                    if (towerOfStorms)
                    {
                        towerOfStorms = false;
                        --ActiveTowersCount;
                    }
                    break;
                case LeviathanActions.TowerOfFrostDestroyed:
                    if (towerOfFrost)
                    {
                        towerOfFrost = false;
                        --ActiveTowersCount;
                    }
                    break;
                case LeviathanActions.TowerOfFlamesDestroyed:
                    if (towerOfFlames)
                    {
                        towerOfFlames = false;
                        --ActiveTowersCount;
                    }
                    break;
                case LeviathanActions.TowerOfLifeDestroyed:
                    if (towerOfLife)
                    {
                        towerOfLife = false;
                        --ActiveTowersCount;
                    }
                    break;
                case Leviathan.ActionStartHardMode:  // Activate hard-mode enable all towers, apply buffs on leviathan
                    ActiveTowers = true;
                    towerOfStorms = true;
                    towerOfLife = true;
                    towerOfFlames = true;
                    towerOfFrost = true;
                    me.SetLootMode(LootModes.Default | LootModes.HardMode1 | LootModes.HardMode2 | LootModes.HardMode3 | LootModes.HardMode4);
                    break;
                case LeviathanActions.MoveToCenterPosition: // Triggered by 2 Collossus near door
                    if (!me.IsDead())
                    {
                        me.SetHomePosition(Leviathan.Center);
                        me.GetMotionMaster().MoveCharge(Leviathan.Center.GetPositionX(), Leviathan.Center.GetPositionY(), Leviathan.Center.GetPositionZ()); // position center
                        me.SetReactState(ReactStates.Aggressive);
                        me.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable | UnitFlags.Stunned);
                        return;
                    }
                    break;
                default:
                    break;
            }
        }

        //! Copypasta from DoSpellAttackIfReady, only difference is the target - it cannot be selected trough GetVictim this way -
        //! I also removed the spellInfo check
        void DoBatteringRamIfReady()
        {
            if (me.isAttackReady())
            {
                Unit target = Global.ObjAccessor.GetUnit(me, _pursueTarget);
                if (me.IsWithinCombatRange(target, 30.0f))
                {
                    DoCast(target, Spells.BatteringRam);
                    me.resetAttackTimer();
                }
            }
        }

        Vehicle vehicle;
        byte ActiveTowersCount;
        byte Shutdown;
        bool ActiveTowers;
        bool towerOfStorms;
        bool towerOfLife;
        bool towerOfFlames;
        bool towerOfFrost;
        bool Shutout;
        bool Unbroken;
        ObjectGuid _pursueTarget;
    }

    [Script]
    class boss_flame_leviathan_seat : ScriptedAI
    {
        public boss_flame_leviathan_seat(Creature creature)
            : base(creature)
        {
            vehicle = creature.GetVehicleKit();
            Cypher.Assert(vehicle);
            me.SetReactState(ReactStates.Passive);
            me.SetDisplayFromModel(1);
            instance = creature.GetInstanceScript();
        }

        InstanceScript instance;
        Vehicle vehicle;

        public override void PassengerBoarded(Unit who, sbyte seatId, bool apply)
        {
            if (!me.GetVehicle())
                return;

            if (seatId == Seats.Player)
            {
                Creature leviathan = me.GetVehicleCreatureBase();
                if (!apply)
                    return;
                else if (leviathan)
                    leviathan.GetAI().Talk(TextIds.PlayerRiding);

                Unit turretPassenger = me.GetVehicleKit().GetPassenger(Seats.Turret);
                if (turretPassenger)
                {
                    Creature turret = turretPassenger.ToCreature();
                    if (turret)
                    {
                        turret.SetFaction(me.GetVehicleBase().getFaction());
                        turret.SetUInt32Value(UnitFields.Flags, 0); // unselectable
                        turret.GetAI().AttackStart(who);
                    }
                }
                Unit devicePassenger = me.GetVehicleKit().GetPassenger(Seats.Device);
                if (devicePassenger)
                {
                    Creature device = devicePassenger.ToCreature();
                    if (device)
                    {
                        device.SetFlag64(UnitFields.NpcFlags, NPCFlags.SpellClick);
                        device.RemoveFlag(UnitFields.Flags, UnitFlags.NotSelectable);
                    }
                }

                me.SetFlag(UnitFields.Flags, UnitFlags.NotSelectable);
            }
            else if (seatId == Seats.Turret)
            {
                if (apply)
                    return;

                Unit device = vehicle.GetPassenger(Seats.Device);
                if (device)
                {
                    device.SetFlag64(UnitFields.NpcFlags, NPCFlags.SpellClick);
                    device.SetUInt32Value(UnitFields.Flags, 0); // unselectable
                }
            }
        }
    }

    [Script]
    class boss_flame_leviathan_defense_cannon : ScriptedAI
    {
        public boss_flame_leviathan_defense_cannon(Creature creature)
            : base(creature)
        {
        }

        uint NapalmTimer;

        public override void Reset()
        {
            NapalmTimer = 5 * Time.InMilliseconds;
            DoCast(me, Spells.StealthDetection);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            if (NapalmTimer <= diff)
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0);
                if (target)
                    if (CanAIAttack(target))
                        DoCast(target, Spells.Napalm, true);

                NapalmTimer = 5000;
            }
            else
                NapalmTimer -= diff;
        }

        public override bool CanAIAttack(Unit who)
        {
            if (!who.IsTypeId(TypeId.Player) || !who.GetVehicle() || who.GetVehicleBase().GetEntry() == CreatureIds.Seat)
                return false;
            return true;
        }
    }

    [Script]
    class boss_flame_leviathan_defense_turret : TurretAI
    {
        public boss_flame_leviathan_defense_turret(Creature creature) : base(creature) { }

        public override void DamageTaken(Unit who, ref uint damage)
        {
            if (!CanAIAttack(who))
                damage = 0;
        }

        public override bool CanAIAttack(Unit who)
        {
            if (!who.IsTypeId(TypeId.Player) || !who.GetVehicle() || who.GetVehicleBase().GetEntry() != CreatureIds.Seat)
                return false;
            return true;
        }
    }

    [Script]
    class boss_flame_leviathan_overload_device : PassiveAI
    {
        public boss_flame_leviathan_overload_device(Creature creature)
            : base(creature)
        {
        }

        public override void OnSpellClick(Unit clicker, ref bool result)
        {
            if (!result)
                return;

            if (me.GetVehicle())
            {
                me.RemoveFlag64(UnitFields.NpcFlags, NPCFlags.SpellClick);
                me.SetFlag(UnitFields.Flags, UnitFlags.NotSelectable);

                Unit player = me.GetVehicle().GetPassenger(Seats.Player);
                if (player)
                {
                    me.GetVehicleBase().CastSpell(player, Spells.SmokeTrail, true);
                    player.GetMotionMaster().MoveKnockbackFrom(me.GetVehicleBase().GetPositionX(), me.GetVehicleBase().GetPositionY(), 30, 30);
                    player.ExitVehicle();
                }
            }
        }
    }

    [Script]
    class boss_flame_leviathan_safety_container : PassiveAI
    {
        public boss_flame_leviathan_safety_container(Creature creature)
            : base(creature)
        {
        }

        public override void JustDied(Unit killer)
        {
            float x, y, z;
            me.GetPosition(out x, out y, out z);
            z = me.GetMap().GetHeight(me.GetPhaseShift(), x, y, z);
            me.GetMotionMaster().MovePoint(0, x, y, z);
            me.SetPosition(x, y, z, 0);
        }

        public override void UpdateAI(uint diff)
        {
        }
    }

    [Script]
    class npc_mechanolift : PassiveAI
    {
        public npc_mechanolift(Creature creature) : base(creature)
        {
            Cypher.Assert(me.GetVehicleKit());
        }

        uint MoveTimer;

        public override void Reset()
        {
            MoveTimer = 0;
            me.GetMotionMaster().MoveRandom(50);
        }

        public override void JustDied(Unit killer)
        {
            me.GetMotionMaster().MoveTargetedHome();
            DoCast(Spells.DustyExplosion);
            Creature liquid = DoSummon(CreatureIds.Liquid, me, 0f);
            if (liquid)
            {
                liquid.CastSpell(liquid, Spells.LiquidPyrite, true);
                liquid.CastSpell(liquid, Spells.DustCloudImpact, true);
            }
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (type == MovementGeneratorType.Point && id == 1)
            {
                Creature container = me.FindNearestCreature(CreatureIds.Container, 5, true);
                if (container)
                    container.EnterVehicle(me);
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (MoveTimer <= diff)
            {
                if (me.GetVehicleKit().HasEmptySeat(-1))
                {
                    Creature container = me.FindNearestCreature(CreatureIds.Container, 50, true);
                    if (container && !container.GetVehicle())
                        me.GetMotionMaster().MovePoint(1, container.GetPositionX(), container.GetPositionY(), container.GetPositionZ());
                }

                MoveTimer = 30000; //check next 30 seconds
            }
            else
                MoveTimer -= diff;
        }
    }

    [Script]
    class npc_pool_of_tar : ScriptedAI
    {
        public npc_pool_of_tar(Creature creature)
            : base(creature)
        {
            me.RemoveFlag(UnitFields.Flags, UnitFlags.NotSelectable);
            me.SetReactState(ReactStates.Passive);
            me.CastSpell(me, Spells.TarPassive, true);
        }

        public override void DamageTaken(Unit who, ref uint damage)
        {
            damage = 0;
        }

        public override void SpellHit(Unit caster, SpellInfo spell)
        {
            if (spell.SchoolMask.HasAnyFlag(SpellSchoolMask.Fire) && !me.HasAura(Spells.Blaze))
                me.CastSpell(me, Spells.Blaze, true);
        }

        public override void UpdateAI(uint diff) { }
    }

    [Script]
    class npc_colossus : ScriptedAI
    {
        public npc_colossus(Creature creature)
            : base(creature)
        {
            instance = creature.GetInstanceScript();
        }

        InstanceScript instance;

        public override void JustDied(Unit killer)
        {
            if (me.GetHomePosition().IsInDist(Leviathan.Center, 50.0f))
                instance.SetData(InstanceData.Colossus, instance.GetData(InstanceData.Colossus) + 1);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class npc_thorims_hammer : ScriptedAI
    {
        public npc_thorims_hammer(Creature creature)
            : base(creature)
        {
            me.SetFlag(UnitFields.Flags, UnitFlags.NotSelectable);
            me.CastSpell(me, Spells.DummyBlue, true);
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (who.IsTypeId(TypeId.Player) && who.IsVehicle() && me.IsInRange(who, 0, 10, false))
            {
                Creature trigger = DoSummonFlyer(CreatureIds.ThorimTargetBeacon, me, 20, 0, 1000, TempSummonType.TimedDespawn);
                if (trigger)
                    trigger.CastSpell(who, Spells.ThorimSHammer, true);
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!me.HasAura(Spells.DummyBlue))
                me.CastSpell(me, Spells.DummyBlue, true);

            UpdateVictim();
        }
    }

    [Script]
    class npc_mimirons_inferno : npc_escortAI
    {
        public npc_mimirons_inferno(Creature creature)
            : base(creature)
        {
            me.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
            me.CastSpell(me, Spells.DummyYellow, true);
            me.SetReactState(ReactStates.Passive);
        }

        public override void WaypointReached(uint waypointId)
        {

        }

        public override void Reset()
        {
            infernoTimer = 2000;
        }

        uint infernoTimer;

        public override void UpdateAI(uint diff)
        {
            base.UpdateAI(diff);

            if (!HasEscortState(eEscortState.Escorting))
                Start(false, true, ObjectGuid.Empty, null, false, true);
            else
            {
                if (infernoTimer <= diff)
                {
                    Creature trigger = DoSummonFlyer(CreatureIds.MimironTargetBeacon, me, 20, 0, 1000, TempSummonType.TimedDespawn);
                    if (trigger)
                    {
                        trigger.CastSpell(me.GetPositionX(), me.GetPositionY(), me.GetPositionZ(), Spells.MimironSInferno, true);
                        infernoTimer = 2000;
                    }
                }
                else
                    infernoTimer -= diff;

                if (!me.HasAura(Spells.DummyYellow))
                    me.CastSpell(me, Spells.DummyYellow, true);
            }
        }
    }

    [Script]
    class npc_hodirs_fury : ScriptedAI
    {
        public npc_hodirs_fury(Creature creature)
            : base(creature)
        {
            me.SetFlag(UnitFields.Flags, UnitFlags.NotSelectable);
            me.CastSpell(me, Spells.DummyGreen, true);
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (who.IsTypeId(TypeId.Player) && who.IsVehicle() && me.IsInRange(who, 0, 5, false))
            {
                Creature trigger = DoSummonFlyer(CreatureIds.HodirTargetBeacon, me, 20, 0, 1000, TempSummonType.TimedDespawn);
                if (trigger)
                    trigger.CastSpell(who, Spells.HodirSFury, true);
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!me.HasAura(Spells.DummyGreen))
                me.CastSpell(me, Spells.DummyGreen, true);

            UpdateVictim();
        }
    }

    [Script]
    class npc_freyas_ward : ScriptedAI
    {
        public npc_freyas_ward(Creature creature)
            : base(creature)
        {
            me.CastSpell(me, Spells.DummyGreen, true);
        }

        uint summonTimer;

        public override void Reset()
        {
            summonTimer = 5000;
        }

        public override void UpdateAI(uint diff)
        {
            if (summonTimer <= diff)
            {
                DoCast(Spells.FreyaSWardEffect1);
                DoCast(Spells.FreyaSWardEffect2);
                summonTimer = 20000;
            }
            else
                summonTimer -= diff;

            if (!me.HasAura(Spells.DummyGreen))
                me.CastSpell(me, Spells.DummyGreen, true);

            UpdateVictim();
        }
    }

    [Script]
    class npc_freya_ward_summon : ScriptedAI
    {
        public npc_freya_ward_summon(Creature creature)
            : base(creature)
        {
            creature.GetMotionMaster().MoveRandom(100);
        }

        uint lashTimer;

        public override void Reset()
        {
            lashTimer = 5000;
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            if (lashTimer <= diff)
            {
                DoCast(Spells.Lash);
                lashTimer = 20000;
            }
            else
                lashTimer -= diff;

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class npc_lorekeeper : ScriptedAI
    {
        public npc_lorekeeper(Creature creature) : base(creature)
        {
            _instance = creature.GetInstanceScript();
        }

        public override void DoAction(int action)
        {
            // Start encounter
            if (action == Leviathan.ActionSpawnVehicles)
            {
                for (int i = 0; i < RaidMode(2, 5); ++i)
                    DoSummon(Vehicles.Siege, Leviathan.PosSiege[i], 3000, TempSummonType.CorpseTimedDespawn);
                for (int i = 0; i < RaidMode(2, 5); ++i)
                    DoSummon(Vehicles.Chopper, Leviathan.PosChopper[i], 3000, TempSummonType.CorpseTimedDespawn);
                for (int i = 0; i < RaidMode(2, 5); ++i)
                    DoSummon(Vehicles.Demolisher, Leviathan.PosDemolisher[i], 3000, TempSummonType.CorpseTimedDespawn);
                return;
            }
        }

        public override void sGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            if (menuId == GossipIds.MenuLoreKeeper && gossipListId == GossipIds.OptionLoreKeeper)
            {
                me.RemoveFlag(UnitFields.NpcFlags, NPCFlags.Gossip);
                player.PlayerTalkClass.SendCloseGossip();
                me.GetMap().LoadGrid(364, -16); // make sure leviathan is loaded

                Creature leviathan = ObjectAccessor.GetCreature(me, _instance.GetGuidData(BossIds.Leviathan));
                if (leviathan)
                {
                    leviathan.GetAI().DoAction(Leviathan.ActionStartHardMode);
                    me.SetVisible(false);
                    DoAction(Leviathan.ActionSpawnVehicles); // spawn the vehicles

                    Creature delorah = _instance.GetCreature(InstanceData.Dellorah);
                    if (delorah)
                    {
                        Creature brann = _instance.GetCreature(InstanceData.BrannBronzebeardIntro);
                        if (brann)
                        {
                            brann.RemoveFlag(UnitFields.NpcFlags, NPCFlags.Gossip);
                            delorah.GetMotionMaster().MovePoint(0, brann.GetPositionX() - 4, brann.GetPositionY(), brann.GetPositionZ());
                            // @todo delorah->AI()->Talk(xxxx, brann->GetGUID()); when reached at branz
                        }
                    }
                }
            }
        }

        InstanceScript _instance;
    }

    [Script]
    class go_ulduar_tower : GameObjectScript
    {
        public go_ulduar_tower() : base("go_ulduar_tower") { }

        public override void OnDestroyed(GameObject go, Player player)
        {
            InstanceScript instance = go.GetInstanceScript();
            if (instance == null)
                return;

            switch (go.GetEntry())
            {
                case Towers.ofStorms:
                    instance.ProcessEvent(go, InstanceEventIds.TowerOfStormDestroyed);
                    break;
                case Towers.ofFlames:
                    instance.ProcessEvent(go, InstanceEventIds.TowerOfFlamesDestroyed);
                    break;
                case Towers.ofFrost:
                    instance.ProcessEvent(go, InstanceEventIds.TowerOfFrostDestroyed);
                    break;
                case Towers.ofLife:
                    instance.ProcessEvent(go, InstanceEventIds.TowerOfLifeDestroyed);
                    break;
            }

            Creature trigger = go.FindNearestCreature(CreatureIds.UlduarGauntletGenerator, 15.0f, true);
            if (trigger)
                trigger.DisappearAndDie();
        }
    }

    [Script]
    class achievement_three_car_garage_demolisher : AchievementCriteriaScript
    {
        public achievement_three_car_garage_demolisher() : base("achievement_three_car_garage_demolisher") { }

        public override bool OnCheck(Player source, Unit target)
        {
            Creature vehicle = source.GetVehicleCreatureBase();
            if (vehicle)
            {
                if (vehicle.GetEntry() == Vehicles.Demolisher)
                    return true;
            }

            return false;
        }
    }

    [Script]
    class achievement_three_car_garage_chopper : AchievementCriteriaScript
    {
        public achievement_three_car_garage_chopper() : base("achievement_three_car_garage_chopper") { }

        public override bool OnCheck(Player source, Unit target)
        {
            Creature vehicle = source.GetVehicleCreatureBase();
            if (vehicle)
            {
                if (vehicle.GetEntry() == Vehicles.Chopper)
                    return true;
            }

            return false;
        }
    }

    [Script]
    class achievement_three_car_garage_siege : AchievementCriteriaScript
    {
        public achievement_three_car_garage_siege() : base("achievement_three_car_garage_siege") { }

        public override bool OnCheck(Player source, Unit target)
        {
            Creature vehicle = source.GetVehicleCreatureBase();
            if (vehicle)
            {
                if (vehicle.GetEntry() == Vehicles.Siege)
                    return true;
            }

            return false;
        }
    }

    [Script]
    class achievement_shutout : AchievementCriteriaScript
    {
        public achievement_shutout() : base("achievement_shutout") { }

        public override bool OnCheck(Player source, Unit target)
        {
            if (target)
            {
                Creature leviathan = target.ToCreature();
                if (leviathan)
                    if (leviathan.GetAI().GetData(Leviathan.DataShutout) != 0)
                        return true;
            }

            return false;
        }
    }

    [Script]
    class achievement_unbroken : AchievementCriteriaScript
    {
        public achievement_unbroken() : base("achievement_unbroken") { }

        public override bool OnCheck(Player source, Unit target)
        {
            if (target)
            {
                InstanceScript instance = target.GetInstanceScript();
                if (instance != null)
                    return instance.GetData(InstanceAchievementData.DataUnbroken) != 0;
            }

            return false;
        }
    }

    [Script]
    class achievement_orbital_bombardment : AchievementCriteriaScript
    {
        public achievement_orbital_bombardment() : base("achievement_orbital_bombardment") { }

        public override bool OnCheck(Player source, Unit target)
        {
            if (!target)
                return false;

            Creature leviathan = target.ToCreature();
            if (leviathan)
                if (leviathan.GetAI().GetData(Leviathan.DataOrbitAchievements) >= 1)
                    return true;

            return false;
        }
    }

    [Script]
    class achievement_orbital_devastation : AchievementCriteriaScript
    {
        public achievement_orbital_devastation() : base("achievement_orbital_devastation") { }

        public override bool OnCheck(Player source, Unit target)
        {
            if (!target)
                return false;
            Creature leviathan = target.ToCreature();
            if (leviathan)
                if (leviathan.GetAI().GetData(Leviathan.DataOrbitAchievements) >= 2)
                    return true;

            return false;
        }
    }

    [Script]
    class achievement_nuked_from_orbit : AchievementCriteriaScript
    {
        public achievement_nuked_from_orbit() : base("achievement_nuked_from_orbit") { }

        public override bool OnCheck(Player source, Unit target)
        {
            if (!target)
                return false;

            Creature leviathan = target.ToCreature();
            if (leviathan)
                if (leviathan.GetAI().GetData(Leviathan.DataOrbitAchievements) >= 3)
                    return true;

            return false;
        }
    }

    [Script]
    class achievement_orbit_uary : AchievementCriteriaScript
    {
        public achievement_orbit_uary() : base("achievement_orbit_uary") { }

        public override bool OnCheck(Player source, Unit target)
        {
            if (!target)
                return false;

            Creature leviathan = target.ToCreature();
            if (leviathan)
                if (leviathan.GetAI().GetData(Leviathan.DataOrbitAchievements) == 4)
                    return true;

            return false;
        }
    }

    [Script]
    class spell_load_into_catapult : AuraScript
    {
        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit owner = GetOwner().ToUnit();
            if (!owner)
                return;

            owner.CastSpell(owner, 62340, true);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit owner = GetOwner().ToUnit();
            if (!owner)
                return;

            owner.RemoveAurasDueToSpell(62340);
        }

        public override void Register()
        {
            OnEffectApply.Add(new EffectApplyHandler(OnApply, 0, AuraType.ControlVehicle, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.ControlVehicle, AuraEffectHandleModes.RealOrReapplyMask));
        }
    }

    [Script]
    class spell_auto_repair : SpellScript
    {
        void CheckCooldownForTarget(SpellMissInfo missInfo)
        {
            if (missInfo != SpellMissInfo.None)
                return;

            if (GetHitUnit().HasAuraEffect(62705, 2))   // Check presence of dummy aura indicating cooldown
            {
                PreventHitEffect(0);
                PreventHitDefaultEffect(1);
                PreventHitDefaultEffect(2);
                //! Currently this doesn't work: if we call PreventHitAura(), the existing aura will be removed
                //! because of recent aura refreshing changes. Since removing the existing aura negates the idea
                //! of a cooldown marker, we just let the dummy aura refresh itself without executing the other SpellEffectName.
                //! The spelleffects can be executed by letting the dummy aura expire naturally.
                //! This is a temporary solution only.
            }
        }

        void HandleScript(uint eff)
        {
            Vehicle vehicle = GetHitUnit().GetVehicleKit();
            if (!vehicle)
                return;

            Player driver = vehicle.GetPassenger(0) ? vehicle.GetPassenger(0).ToPlayer() : null;
            if (!driver)
                return;

            driver.TextEmote(TextIds.EmoteRepair, driver, true);

            InstanceScript instance = driver.GetInstanceScript();
            if (instance == null)
                return;

            // Actually should/could use basepoints (100) for this spell effect as percentage of health, but oh well.
            vehicle.GetBase().SetFullHealth();

            // For achievement
            instance.SetData(InstanceAchievementData.DataUnbroken, 0);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
            BeforeHit.Add(new BeforeHitHandler(CheckCooldownForTarget));
        }
    }

    [Script]
    class spell_systems_shutdown : AuraScript
    {
        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Creature owner = GetOwner().ToCreature();
            if (!owner)
                return;

            //! This could probably in the SPELL_EFFECT_SEND_EVENT handler too:
            owner.AddUnitState(UnitState.Stunned | UnitState.Root);
            owner.SetFlag(UnitFields.Flags, UnitFlags.Stunned);
            owner.RemoveAurasDueToSpell(Spells.GatheringSpeed);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Creature owner = GetOwner().ToCreature();
            if (!owner)
                return;

            owner.RemoveFlag(UnitFields.Flags, UnitFlags.Stunned);
        }

        public override void Register()
        {
            OnEffectApply.Add(new EffectApplyHandler(OnApply, 0, AuraType.ModDamagePercentTaken, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.ModDamagePercentTaken, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_pursue : SpellScript
    {
        public override bool Load()
        {
            _target = null;
            return true;
        }

        void FilterTargets(List<WorldObject> targets)
        {
            targets.RemoveAll(target =>
            {
                //! No players, only vehicles (@todo check if blizzlike)
                Creature creatureTarget = target.ToCreature();
                if (!creatureTarget)
                    return true;

                //! NPC entries must match
                if (creatureTarget.GetEntry() != Vehicles.Demolisher && creatureTarget.GetEntry() != Vehicles.Siege)
                    return true;

                //! NPC must be a valid vehicle installation
                Vehicle vehicle = creatureTarget.GetVehicleKit();
                if (!vehicle)
                    return true;

                //! Entity needs to be in appropriate area
                if (target.GetAreaId() != AREA_FORMATION_GROUNDS)
                    return true;

                //! Vehicle must be in use by player
                bool playerFound = false;
                foreach (var seat in vehicle.Seats.Values)
                {
                    if (seat.Passenger.Guid.IsPlayer())
                    {
                        playerFound = true;
                        break;
                    }
                }

                return !playerFound;
            });

            if (targets.Empty())
            {
                Creature caster = GetCaster().ToCreature();
                if (caster)
                    caster.GetAI().EnterEvadeMode();
            }
            else
            {
                //! In the end, only one target should be selected
                _target = targets.SelectRandom();
                FilterTargetsSubsequently(targets);
            }
        }

        void FilterTargetsSubsequently(List<WorldObject> targets)
        {
            targets.Clear();
            if (_target)
                targets.Add(_target);
        }

        void HandleScript(uint eff)
        {
            Creature caster = GetCaster().ToCreature();
            if (!caster)
                return;

            caster.GetAI().AttackStart(GetHitUnit());    // Chase target

            foreach (var seat in caster.GetVehicleKit().Seats.Values)
            {
                Player passenger = Global.ObjAccessor.GetPlayer(caster, seat.Passenger.Guid);
                if (passenger)
                {
                    caster.GetAI().Talk(TextIds.EmotePursue, passenger);
                    return;
                }
            }
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargetsSubsequently, 1, Targets.UnitSrcAreaEnemy));
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ApplyAura));
        }

        const uint AREA_FORMATION_GROUNDS = 4652;

        WorldObject _target;
    }

    [Script]
    class spell_vehicle_throw_passenger : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            Spell baseSpell = GetSpell();
            SpellCastTargets targets = baseSpell.m_targets;
            int damage = GetEffectValue();
            if (targets.HasTraj())
            {
                Vehicle vehicle = GetCaster().GetVehicleKit();
                if (vehicle)
                {
                    Unit passenger = vehicle.GetPassenger((sbyte)(damage - 1));
                    if (passenger)
                    {
                        // use 99 because it is 3d search
                        List<WorldObject> targetList = new List<WorldObject>();
                        var check = new WorldObjectSpellAreaTargetCheck(99, GetExplTargetDest(), GetCaster(), GetCaster(), GetSpellInfo(), SpellTargetCheckTypes.Default, null);
                        var searcher = new WorldObjectListSearcher(GetCaster(), targetList, check);
                        Cell.VisitAllObjects(GetCaster(), searcher, 99.0f);
                        float minDist = 99 * 99;
                        Unit target = null;
                        foreach (var obj in targetList)
                        {
                            Unit unit = obj.ToUnit();
                            if (unit)
                            {
                                if (unit.GetEntry() == CreatureIds.Seat)
                                {
                                    Vehicle seat = unit.GetVehicleKit();
                                    if (seat)
                                    {
                                        if (!seat.GetPassenger(0))
                                        {
                                            Unit device = seat.GetPassenger(2);
                                            if (device)
                                                if (!device.GetCurrentSpell(CurrentSpellTypes.Channeled))
                                                {
                                                    float dist = unit.GetExactDistSq(targets.GetDstPos());
                                                    if (dist < minDist)
                                                    {
                                                        minDist = dist;
                                                        target = unit;
                                                    }
                                                }
                                        }
                                    }
                                }
                            }
                        }
                        if (target && target.IsWithinDist2d(targets.GetDstPos(), GetSpellInfo().GetEffect(effIndex).CalcRadius() * 2)) // now we use *2 because the location of the seat is not correct
                            passenger.EnterVehicle(target, 0);
                        else
                        {
                            passenger.ExitVehicle();
                            passenger.GetMotionMaster().MoveJump(targets.GetDstPos(), targets.GetSpeedXY(), targets.GetSpeedZ());
                        }
                    }
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Dummy));
        }
    }
}
