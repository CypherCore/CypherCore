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

namespace Scripts.Northrend.CrusadersColiseum.TrialOfTheCrusader
{
    struct TextIds
    {
        // Gormok
        public const uint EmoteSnobolled = 0;

        // Acidmaw & Dreadscale
        public const uint EmoteEnrage = 0;
        public const uint SaySpecial = 1;

        // Icehowl
        public const uint EmoteTrampleStart = 0;
        public const uint EmoteTrampleCrash = 1;
        public const uint EmoteTrampleFail = 2;
    }

    struct SpellIds
    {
        //Gormok
        public const uint Impale = 66331;
        public const uint StaggeringStomp = 67648;

        //Snobold
        public const uint RisingAnger = 66636;
        public const uint Snobolled = 66406;
        public const uint Batter = 66408;
        public const uint FireBomb = 66313;
        public const uint FireBomb1 = 66317;
        public const uint FireBombDot = 66318;
        public const uint HeadCrack = 66407;
        public const uint JumpToHand = 66342;
        public const uint RidePlayer = 66245;

        //Acidmaw & Dreadscale
        public const uint Sweep = 66794;
        public const uint SummonSlimepool = 66883;
        public const uint Emerge = 66947;
        public const uint Submerge = 66948;
        public const uint Enrage = 68335;
        public const uint SlimePoolEffect = 66882; //In 60s It Diameter Grows From 10y To 40y (R=R+0.25 Per Second)
        public const uint GroundVisual0 = 66969;
        public const uint GroundVisual1 = 68302;
        public const uint HateToZero = 63984;
        //Acidmaw
        public const uint AcidSpit = 66880;
        public const uint ParalyticSpray = 66901;
        public const uint ParalyticBite = 66824;
        public const uint AcidSpew = 66819;
        public const uint Paralysis = 66830;
        public const uint ParalyticToxin = 66823;
        //Dreadscale
        public const uint BurningBite = 66879;
        public const uint MoltenSpew = 66821;
        public const uint FireSpit = 66796;
        public const uint BurningSpray = 66902;
        public const uint BurningBile = 66869;

        //Icehowl
        public const uint FerociousButt = 66770;
        public const uint MassiveCrash = 66683;
        public const uint Whirl = 67345;
        public const uint ArcticBreath = 66689;
        public const uint Trample = 66734;
        public const uint FrothingRage = 66759;
        public const uint StaggeredDaze = 66758;
    }

    struct Actions
    {
        public const int EnableFireBomb = 1;
        public const int DisableFireBomb = 2;
        public const int ActiveSnobold = 3;
    }

    struct Events
    {
        // Snobold
        public const uint FireBomb = 1;
        public const uint Batter = 2;
        public const uint HeadCrack = 3;
        public const uint Snobolled = 4;
        public const uint CheckMount = 5;

        // Acidmaw & Dreadscale
        public const uint Bite = 6;
        public const uint Spew = 7;
        public const uint SlimePool = 8;
        public const uint Spit = 9;
        public const uint Spray = 10;
        public const uint Sweep = 11;
        public const uint Submerge = 12;
        public const uint Emerge = 13;
        public const uint SummonAcidmaw = 14;

        // Icehowl
        public const uint FerociousButt = 15;
        public const uint MassiveCrash = 16;
        public const uint Whirl = 17;
        public const uint ArcticBreath = 18;
        public const uint Trample = 19;
    }

    public struct Misc
    {
        public const uint EquipMain = 50760;
        public const uint EquipOffhand = 48040;
        public const uint EquipRanged = 47267;
        public const int EquipDone = -1;

        public const uint ModelAcidmawStationary = 29815;
        public const uint ModelAcidmawMobile = 29816;
        public const uint ModelDreadscaleStationary = 26935;
        public const uint ModelDreadscaleMobile = 24564;

        public const uint MaxSnobolds = 4;

        public const byte PhaseMobile = 1;
        public const byte PhaseStationary = 2;
        public const byte PhaseSubmerged = 3;

        public const int DataNewTarget = 1;
        public const uint GormokHandSeat = 4;
        public const uint PlayerVehicleId = 444;

        public const uint NpcSnoboldVassal = 34800;
        public const uint NpcFireBomb = 34854;
        public const uint NpcSlimePool = 35176;
    }

    [Script]
    class boss_gormok : BossAI
    {
        public boss_gormok(Creature creature) : base(creature, DataTypes.BossBeasts) { }

        public override void Reset()
        {
            _scheduler.SetValidator(() => !me.HasUnitState(UnitState.Casting));

            _scheduler.Schedule(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10), task =>
            {
                DoCastVictim(SpellIds.Impale);
                task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(15), task =>
            {
                DoCastVictim(SpellIds.StaggeringStomp);
                task.Repeat(TimeSpan.FromSeconds(15));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30), task =>
            {
                for (sbyte i = 0; i < Misc.MaxSnobolds; ++i)
                {
                    Unit snobold = me.GetVehicleKit().GetPassenger(i);
                    if (snobold)
                    {
                        snobold.ExitVehicle();
                        snobold.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
                        snobold.ToCreature().GetAI().DoAction(Actions.DisableFireBomb);
                        snobold.CastSpell(me, SpellIds.JumpToHand, true);
                        break;
                    }
                }
                task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30));
            });

            summons.DespawnAll();
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            instance.DoUseDoorOrButton(instance.GetGuidData(GameObjectIds.MainGateDoor));
            base.EnterEvadeMode(why);
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (type != MovementGeneratorType.Point)
                return;

            switch (id)
            {
                case 0:
                    instance.DoUseDoorOrButton(instance.GetGuidData(GameObjectIds.MainGateDoor));
                    me.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
                    me.SetReactState(ReactStates.Aggressive);
                    me.SetInCombatWithZone();
                    break;
                default:
                    break;
            }
        }

        public override void JustDied(Unit killer)
        {
            instance.SetData(DataTypes.NorthrendBeasts, NorthrendBeasts.GormokDone);
        }

        public override void JustReachedHome()
        {
            instance.DoUseDoorOrButton(instance.GetGuidData(GameObjectIds.MainGateDoor));
            instance.SetData(DataTypes.NorthrendBeasts, (uint)EncounterState.Fail);

            me.DespawnOrUnsummon();
        }

        public override void EnterCombat(Unit who)
        {
            _EnterCombat();
            instance.SetData(DataTypes.NorthrendBeasts, NorthrendBeasts.GormokInProgress);
        }

        public override void DamageTaken(Unit who, ref uint damage)
        {
            // despawn the remaining passengers on death
            if (damage >= me.GetHealth())
            {
                for (sbyte i = 0; i < Misc.MaxSnobolds; ++i)
                {
                    Unit snobold = me.GetVehicleKit().GetPassenger(i);
                    if (snobold)
                        snobold.ToCreature().DespawnOrUnsummon();
                }
            }
        }

        public override void PassengerBoarded(Unit passenger, sbyte seatId, bool apply)
        {
            if (apply && seatId == Misc.GormokHandSeat)
                passenger.CastSpell(me, SpellIds.RisingAnger, true);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            DoMeleeAttackIfReady();
        }
    }

    class SnobolledTargetSelector : ISelector
    {
        public SnobolledTargetSelector(Unit unit) { }

        public bool Check(Unit unit)
        {
            if (unit.GetTypeId() != TypeId.Player)
                return false;

            if (unit.HasAura(SpellIds.RidePlayer) || unit.HasAura(SpellIds.Snobolled))
                return false;

            return true;
        }
    }

    [Script]
    class npc_snobold_vassal : ScriptedAI
    {
        public npc_snobold_vassal(Creature creature) : base(creature)
        {
            _instance = creature.GetInstanceScript();
            _isActive = false;
            _instance.SetData(DataTypes.SnoboldCount, DataTypes.Increase);
            SetCombatMovement(false);
        }

        public override void Reset()
        {
            me.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
            me.SetInCombatWithZone();
            _events.ScheduleEvent(Events.CheckMount, TimeSpan.FromSeconds(1));
            _events.ScheduleEvent(Events.FireBomb, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));
        }

        public override void JustDied(Unit killer)
        {
            Unit target = Global.ObjAccessor.GetPlayer(me, _targetGUID);
            if (target)
                target.RemoveAurasDueToSpell(SpellIds.Snobolled);
            _instance.SetData(DataTypes.SnoboldCount, DataTypes.Decrease);
        }

        public override void DoAction(int action)
        {
            switch (action)
            {
                case Actions.EnableFireBomb:
                    _events.ScheduleEvent(Events.FireBomb, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));
                    break;
                case Actions.DisableFireBomb:
                    _events.CancelEvent(Events.FireBomb);
                    break;
                case Actions.ActiveSnobold:
                    _isActive = true;
                    break;
                default:
                    break;
            }
        }

        public override void SetGUID(ObjectGuid guid, int id = 0)
        {
            if (id == Misc.DataNewTarget)
            {
                Unit target = Global.ObjAccessor.GetPlayer(me, guid);
                if (target)
                {
                    _targetGUID = guid;
                    AttackStart(target);
                    _events.ScheduleEvent(Events.Batter, TimeSpan.FromSeconds(5));
                    _events.ScheduleEvent(Events.HeadCrack, TimeSpan.FromSeconds(25));
                    _events.ScheduleEvent(Events.Snobolled, TimeSpan.FromMilliseconds(500));
                }
            }
        }

        public override void AttackStart(Unit target)
        {
            //Snobold only melee attack players that is your vehicle
            if (!_isActive || target.GetGUID() != _targetGUID)
                return;

            base.AttackStart(target);
        }

        void MountOnBoss()
        {
            Unit gormok = ObjectAccessor.GetCreature(me, _instance.GetGuidData(CreatureIds.Gormok));
            if (gormok && gormok.IsAlive())
            {
                me.AttackStop();
                _targetGUID.Clear();
                _isActive = false;
                _events.CancelEvent(Events.Batter);
                _events.CancelEvent(Events.HeadCrack);

                for (sbyte i = 0; i < Misc.MaxSnobolds; i++)
                {
                    if (!gormok.GetVehicleKit().GetPassenger(i))
                    {
                        me.EnterVehicle(gormok, i);
                        DoAction(Actions.EnableFireBomb);
                        break;
                    }
                }
            }
            //Without Boss, snobolds should jump in another players
            else
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0, new SnobolledTargetSelector(me));
                if (target)
                    me.CastSpell(target, SpellIds.RidePlayer, true);
            }
        }

        public override void UpdateAI(uint diff)
        {
            _events.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case Events.FireBomb:
                        if (me.GetVehicleBase())
                        {
                            Unit target = SelectTarget(SelectAggroTarget.Random, 0, -me.GetVehicleBase().GetCombatReach(), true);
                            if (target)
                                me.CastSpell(target, SpellIds.FireBomb);
                        }
                        _events.Repeat(TimeSpan.FromSeconds(20));
                        break;
                    case Events.HeadCrack:
                        DoCast(me.GetVehicleBase(), SpellIds.HeadCrack);
                        _events.Repeat(TimeSpan.FromSeconds(30));
                        break;
                    case Events.Batter:
                        DoCast(me.GetVehicleBase(), SpellIds.Batter);
                        _events.Repeat(TimeSpan.FromSeconds(10));
                        break;
                    case Events.Snobolled:
                        DoCastAOE(SpellIds.Snobolled);
                        break;
                    case Events.CheckMount:
                        if (!me.GetVehicleBase())
                            MountOnBoss();
                        _events.Repeat(TimeSpan.FromSeconds(1));
                        break;
                    default:
                        break;
                }

                if (me.HasUnitState(UnitState.Casting))
                    return;
            });


            if (!UpdateVictim())
                return;

            // do melee attack only when not on Gormoks back
            if (_isActive)
                DoMeleeAttackIfReady();
        }

        InstanceScript _instance;
        ObjectGuid _targetGUID;
        bool _isActive;
    }

    [Script]
    class npc_firebomb : ScriptedAI
    {
        public npc_firebomb(Creature creature) : base(creature)
        {
            _instance = creature.GetInstanceScript();
        }

        public override void Reset()
        {
            DoCast(me, SpellIds.FireBombDot, true);
            SetCombatMovement(false);
            me.SetReactState(ReactStates.Passive);
            me.SetDisplayFromModel(1);
        }

        public override void UpdateAI(uint diff)
        {
            if (_instance.GetData(DataTypes.NorthrendBeasts) != NorthrendBeasts.GormokInProgress)
                me.DespawnOrUnsummon();
        }

        InstanceScript _instance;
    }

    class boss_jormungarAI : BossAI
    {
        public boss_jormungarAI(Creature creature) : base(creature, DataTypes.BossBeasts)
        {
            Phase = Misc.PhaseMobile;
        }

        public override void Reset()
        {
            Enraged = false;

            _events.ScheduleEvent(Events.Spit, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30), 0, Misc.PhaseStationary);
            _events.ScheduleEvent(Events.Spray, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30), 0, Misc.PhaseStationary);
            _events.ScheduleEvent(Events.Sweep, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30), 0, Misc.PhaseStationary);
            _events.ScheduleEvent(Events.Bite, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30), 0, Misc.PhaseMobile);
            _events.ScheduleEvent(Events.Spew, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30), 0, Misc.PhaseMobile);
            _events.ScheduleEvent(Events.SlimePool, TimeSpan.FromSeconds(15), 0, Misc.PhaseMobile);
        }

        public override void JustDied(Unit killer)
        {
            Creature otherWorm = ObjectAccessor.GetCreature(me, instance.GetGuidData(OtherWormEntry));
            if (otherWorm)
            {
                if (!otherWorm.IsAlive())
                {
                    instance.SetData(DataTypes.NorthrendBeasts, NorthrendBeasts.SnakesDone);

                    me.DespawnOrUnsummon();
                    otherWorm.DespawnOrUnsummon();
                }
                else
                    instance.SetData(DataTypes.NorthrendBeasts, NorthrendBeasts.SnakesSpecial);
            }
        }

        public override void JustReachedHome()
        {
            // prevent losing 2 attempts at once on heroics
            if (instance.GetData(DataTypes.NorthrendBeasts) != (uint)EncounterState.Fail)
                instance.SetData(DataTypes.NorthrendBeasts, (uint)EncounterState.Fail);

            me.DespawnOrUnsummon();
        }

        public override void EnterCombat(Unit who)
        {
            _EnterCombat();
            me.SetInCombatWithZone();
            instance.SetData(DataTypes.NorthrendBeasts, NorthrendBeasts.SnakesInProgress);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            if (!Enraged && instance.GetData(DataTypes.NorthrendBeasts) == NorthrendBeasts.SnakesSpecial)
            {
                me.RemoveAurasDueToSpell(SpellIds.Submerge);
                me.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
                DoCast(SpellIds.Enrage);
                Enraged = true;
                Talk(TextIds.EmoteEnrage);
            }

            _events.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case Events.Emerge:
                        Emerge();
                        return;
                    case Events.Submerge:
                        Submerge();
                        return;
                    case Events.Bite:
                        DoCastVictim(BiteSpell);
                        _events.ScheduleEvent(Events.Bite, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30), 0, Misc.PhaseMobile);
                        return;
                    case Events.Spew:
                        DoCastAOE(SpewSpell);
                        _events.ScheduleEvent(Events.Spew, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30), 0, Misc.PhaseMobile);
                        return;
                    case Events.SlimePool:
                        DoCast(me, SpellIds.SummonSlimepool);
                        _events.ScheduleEvent(Events.SlimePool, TimeSpan.FromSeconds(30), 0, Misc.PhaseMobile);
                        return;
                    case Events.SummonAcidmaw:
                        Creature acidmaw = me.SummonCreature(CreatureIds.Acidmaw, MiscData.ToCCommonLoc[9].GetPositionX(), MiscData.ToCCommonLoc[9].GetPositionY(), MiscData.ToCCommonLoc[9].GetPositionZ(), 5, TempSummonType.ManualDespawn);
                        if (acidmaw)
                        {
                            acidmaw.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
                            acidmaw.SetReactState(ReactStates.Aggressive);
                            acidmaw.SetInCombatWithZone();
                            acidmaw.CastSpell(acidmaw, SpellIds.Emerge);
                            acidmaw.CastSpell(acidmaw, SpellIds.GroundVisual1, true);
                        }
                        return;
                    case Events.Spray:
                        Unit target = SelectTarget(SelectAggroTarget.Random, 0, 0.0f, true);
                        if (target)
                            DoCast(target, SpraySpell);
                        _events.ScheduleEvent(Events.Spray, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30), 0, Misc.PhaseStationary);
                        return;
                    case Events.Sweep:
                        DoCastAOE(SpellIds.Sweep);
                        _events.ScheduleEvent(Events.Sweep, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30), 0, Misc.PhaseStationary);
                        return;
                    default:
                        return;
                }
            });

            if (_events.IsInPhase(Misc.PhaseMobile))
                DoMeleeAttackIfReady();
            if (_events.IsInPhase(Misc.PhaseStationary))
                DoCastVictim(SpitSpell);
        }

        void Submerge()
        {
            DoCast(me, SpellIds.Submerge);
            DoCast(me, SpellIds.GroundVisual0, true);
            me.RemoveAurasDueToSpell(SpellIds.Emerge);
            me.SetInCombatWithZone();
            _events.SetPhase(Misc.PhaseSubmerged);
            _events.ScheduleEvent(Events.Emerge, TimeSpan.FromSeconds(5), 0, Misc.PhaseSubmerged);
            me.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
            me.GetMotionMaster().MovePoint(0, MiscData.ToCCommonLoc[1].GetPositionX() + RandomHelper.FRand(-40.0f, 40.0f), MiscData.ToCCommonLoc[1].GetPositionY() + RandomHelper.FRand(-40.0f, 40.0f), MiscData.ToCCommonLoc[1].GetPositionZ());
            WasMobile = !WasMobile;
        }

        public void Emerge()
        {
            DoCast(me, SpellIds.Emerge);
            DoCastAOE(SpellIds.HateToZero, true);
            me.SetDisplayId(ModelMobile);
            me.RemoveAurasDueToSpell(SpellIds.Submerge);
            me.RemoveAurasDueToSpell(SpellIds.GroundVisual0);
            me.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);

            // if the worm was mobile before submerging, make him stationary now
            if (WasMobile)
            {
                me.SetControlled(true, UnitState.Root);
                SetCombatMovement(false);
                me.SetDisplayId(ModelStationary);
                me.CastSpell(me, SpellIds.GroundVisual1, true);
                _events.SetPhase(Misc.PhaseStationary);
                _events.ScheduleEvent(Events.Submerge, TimeSpan.FromSeconds(45), 0, Misc.PhaseStationary);
                _events.ScheduleEvent(Events.Spit, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30), 0, Misc.PhaseStationary);
                _events.ScheduleEvent(Events.Spray, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30), 0, Misc.PhaseStationary);
                _events.ScheduleEvent(Events.Sweep, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30), 0, Misc.PhaseStationary);
            }
            else
            {
                me.SetControlled(false, UnitState.Root);
                SetCombatMovement(true);
                me.GetMotionMaster().MoveChase(me.GetVictim());
                me.SetDisplayId(ModelMobile);
                _events.SetPhase(Misc.PhaseMobile);
                _events.ScheduleEvent(Events.Submerge, TimeSpan.FromSeconds(45), 0, Misc.PhaseMobile);
                _events.ScheduleEvent(Events.Bite, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30), 0, Misc.PhaseMobile);
                _events.ScheduleEvent(Events.Spew, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30), 0, Misc.PhaseMobile);
                _events.ScheduleEvent(Events.SlimePool, TimeSpan.FromSeconds(15), 0, Misc.PhaseMobile);
            }
        }

        protected uint OtherWormEntry;
        protected uint ModelStationary;
        protected uint ModelMobile;

        protected uint BiteSpell;
        protected uint SpewSpell;
        protected uint SpitSpell;
        protected uint SpraySpell;

        protected uint Phase;
        protected bool Enraged;
        protected bool WasMobile;
    }

    [Script]
    class boss_acidmaw : boss_jormungarAI
    {
        public boss_acidmaw(Creature creature) : base(creature) { }

        public override void Reset()
        {
            base.Reset();
            BiteSpell = SpellIds.ParalyticBite;
            SpewSpell = SpellIds.AcidSpew;
            SpitSpell = SpellIds.AcidSpit;
            SpraySpell = SpellIds.ParalyticSpray;
            ModelStationary = Misc.ModelAcidmawStationary;
            ModelMobile = Misc.ModelAcidmawMobile;
            OtherWormEntry = CreatureIds.Dreadscale;

            WasMobile = true;
            Emerge();
        }
    }

    [Script]
    class boss_dreadscale : boss_jormungarAI
    {
        public boss_dreadscale(Creature creature) : base(creature) { }

        public override void Reset()
        {
            base.Reset();
            BiteSpell = SpellIds.BurningBite;
            SpewSpell = SpellIds.MoltenSpew;
            SpitSpell = SpellIds.FireSpit;
            SpraySpell = SpellIds.BurningSpray;
            ModelStationary = Misc.ModelDreadscaleStationary;
            ModelMobile = Misc.ModelDreadscaleMobile;
            OtherWormEntry = CreatureIds.Acidmaw;

            _events.SetPhase(Misc.PhaseMobile);
            _events.ScheduleEvent(Events.SummonAcidmaw, TimeSpan.FromSeconds(3));
            _events.ScheduleEvent(Events.Submerge, TimeSpan.FromSeconds(45), 0, Misc.PhaseMobile);
            WasMobile = false;
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (type != MovementGeneratorType.Point)
                return;

            switch (id)
            {
                case 0:
                    instance.DoUseDoorOrButton(instance.GetGuidData(GameObjectIds.MainGateDoor));
                    me.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
                    me.SetReactState(ReactStates.Aggressive);
                    me.SetInCombatWithZone();
                    break;
                default:
                    break;
            }
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            instance.DoUseDoorOrButton(instance.GetGuidData(GameObjectIds.MainGateDoor));
            base.EnterEvadeMode(why);
        }

        public override void JustReachedHome()
        {
            instance.DoUseDoorOrButton(instance.GetGuidData(GameObjectIds.MainGateDoor));

            base.JustReachedHome();
        }
    }

    [Script]
    class npc_slime_pool : ScriptedAI
    {
        public npc_slime_pool(Creature creature) : base(creature)
        {
            _instance = creature.GetInstanceScript();
        }

        public override void Reset()
        {
            _cast = false;
            me.SetReactState(ReactStates.Passive);
        }

        public override void UpdateAI(uint diff)
        {
            if (!_cast)
            {
                _cast = true;
                DoCast(me, SpellIds.SlimePoolEffect);
            }

            if (_instance.GetData(DataTypes.NorthrendBeasts) != NorthrendBeasts.SnakesInProgress && _instance.GetData(DataTypes.NorthrendBeasts) != NorthrendBeasts.SnakesSpecial)
                me.DespawnOrUnsummon();
        }

        InstanceScript _instance;
        bool _cast;

    }

    [Script]
    class spell_gormok_fire_bomb : SpellScript
    {
        void TriggerFireBomb(uint effIndex)
        {
            Position pos = GetExplTargetDest();
            if (pos != null)
            {
                Unit caster = GetCaster();
                if (caster)
                    caster.SummonCreature(Misc.NpcFireBomb, pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), 0, TempSummonType.TimedDespawn, 30 * Time.InMilliseconds);
            }
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(TriggerFireBomb, 0, SpellEffectName.TriggerMissile));
        }
    }

    [Script]
    class boss_icehowl : BossAI
    {
        public boss_icehowl(Creature creature) : base(creature, DataTypes.BossBeasts) { }

        public override void Reset()
        {
            _movementStarted = false;
            _movementFinish = false;
            _trampleCast = false;
            _trampleTargetGUID.Clear();
            _trampleTargetX = 0;
            _trampleTargetY = 0;
            _trampleTargetZ = 0;
            _stage = 0;

            _events.ScheduleEvent(Events.FerociousButt, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30));
            _events.ScheduleEvent(Events.ArcticBreath, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(25));
            _events.ScheduleEvent(Events.Whirl, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30));
            _events.ScheduleEvent(Events.MassiveCrash, TimeSpan.FromSeconds(30));
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
            instance.SetData(DataTypes.NorthrendBeasts, NorthrendBeasts.IcehowlDone);
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (type != MovementGeneratorType.Point && type != MovementGeneratorType.Effect)
                return;

            switch (id)
            {
                case 0:
                    if (_stage != 0)
                    {
                        if (me.GetDistance2d(MiscData.ToCCommonLoc[1].GetPositionX(), MiscData.ToCCommonLoc[1].GetPositionY()) < 6.0f)
                            // Middle of the room
                            _stage = 1;
                        else
                        {
                            // Landed from Hop backwards (start trample)
                            if (Global.ObjAccessor.GetPlayer(me, _trampleTargetGUID))
                                _stage = 4;
                            else
                                _stage = 6;
                        }
                    }
                    break;
                case 1: // Finish trample
                    _movementFinish = true;
                    break;
                case 2:
                    instance.DoUseDoorOrButton(instance.GetGuidData(GameObjectIds.MainGateDoor));
                    me.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
                    me.SetReactState(ReactStates.Aggressive);
                    me.SetInCombatWithZone();
                    break;
                default:
                    break;
            }
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            instance.DoUseDoorOrButton(instance.GetGuidData(GameObjectIds.MainGateDoor));
            base.EnterEvadeMode(why);
        }

        public override void JustReachedHome()
        {
            instance.DoUseDoorOrButton(instance.GetGuidData(GameObjectIds.MainGateDoor));
            instance.SetData(DataTypes.NorthrendBeasts, (uint)EncounterState.Fail);
            me.DespawnOrUnsummon();
        }

        public override void EnterCombat(Unit who)
        {
            _EnterCombat();
            instance.SetData(DataTypes.NorthrendBeasts, NorthrendBeasts.IcehowlInProgress);
        }

        public override void SpellHitTarget(Unit target, SpellInfo spell)
        {
            if (spell.Id == SpellIds.Trample && target.IsTypeId(TypeId.Player))
            {
                if (!_trampleCast)
                {
                    DoCast(me, SpellIds.FrothingRage, true);
                    _trampleCast = true;
                }
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _events.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            switch (_stage)
            {
                case 0:
                    {
                        _events.ExecuteEvents(eventId =>
                        {
                            switch (eventId)
                            {
                                case Events.FerociousButt:
                                    DoCastVictim(SpellIds.FerociousButt);
                                    _events.ScheduleEvent(Events.FerociousButt, RandomHelper.URand(15 * Time.InMilliseconds, 30 * Time.InMilliseconds));
                                    return;
                                case Events.ArcticBreath:
                                    Unit target = SelectTarget(SelectAggroTarget.Random, 0, 0.0f, true);
                                    if (target)
                                        DoCast(target, SpellIds.ArcticBreath);
                                    return;
                                case Events.Whirl:
                                    DoCastAOE(SpellIds.Whirl);
                                    _events.ScheduleEvent(Events.Whirl, RandomHelper.URand(15 * Time.InMilliseconds, 30 * Time.InMilliseconds));
                                    return;
                                case Events.MassiveCrash:
                                    me.GetMotionMaster().MoveJump(MiscData.ToCCommonLoc[1], 20.0f, 20.0f, 0); // 1: Middle of the room
                                    SetCombatMovement(false);
                                    me.AttackStop();
                                    _stage = 7; //Invalid (Do nothing more than move)
                                    return;
                                default:
                                    break;
                            }

                            if (me.HasUnitState(UnitState.Casting))
                                return;
                        });
                        DoMeleeAttackIfReady();
                        break;
                    }
                case 1:
                    DoCastAOE(SpellIds.MassiveCrash);
                    me.StopMoving();
                    me.AttackStop();
                    _stage = 2;
                    break;
                case 2:
                    {
                        Unit target = SelectTarget(SelectAggroTarget.Random, 0, 0.0f, true);
                        if (target)
                        {
                            me.StopMoving();
                            me.AttackStop();
                            _trampleTargetGUID = target.GetGUID();
                            me.SetTarget(_trampleTargetGUID);
                            _trampleCast = false;
                            SetCombatMovement(false);
                            me.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable);
                            me.SetControlled(true, UnitState.Root);
                            me.GetMotionMaster().Clear();
                            me.GetMotionMaster().MoveIdle();
                            _events.ScheduleEvent(Events.Trample, TimeSpan.FromSeconds(4));
                            _stage = 3;
                        }
                        else
                            _stage = 6;
                        break;
                    }
                case 3:
                    _events.ExecuteEvents(eventId =>
                    {
                        switch (eventId)
                        {
                            case Events.Trample:
                                {
                                    Unit target = Global.ObjAccessor.GetPlayer(me, _trampleTargetGUID);
                                    if (target)
                                    {
                                        me.StopMoving();
                                        me.AttackStop();
                                        _trampleCast = false;
                                        _trampleTargetX = target.GetPositionX();
                                        _trampleTargetY = target.GetPositionY();
                                        _trampleTargetZ = target.GetPositionZ();
                                        // 2: Hop Backwards
                                        me.GetMotionMaster().MoveJump(2 * me.GetPositionX() - _trampleTargetX, 2 * me.GetPositionY() - _trampleTargetY, me.GetPositionZ(), me.GetOrientation(), 30.0f, 20.0f, 0);
                                        me.SetControlled(false, UnitState.Root);
                                        _stage = 7; //Invalid (Do nothing more than move)
                                    }
                                    else
                                        _stage = 6;
                                    break;
                                }
                            default:
                                break;
                        }
                    });
                    break;
                case 4:
                    {
                        me.StopMoving();
                        me.AttackStop();

                        Player target = Global.ObjAccessor.GetPlayer(me, _trampleTargetGUID);
                        if (target)
                            Talk(TextIds.EmoteTrampleStart, target);

                        me.GetMotionMaster().MoveCharge(_trampleTargetX, _trampleTargetY, _trampleTargetZ, 42, 1);
                        me.SetTarget(ObjectGuid.Empty);
                        _stage = 5;
                        break;
                    }
                case 5:
                    if (_movementFinish)
                    {
                        DoCastAOE(SpellIds.Trample);
                        _movementFinish = false;
                        _stage = 6;
                        return;
                    }
                    if (_events.ExecuteEvent() == Events.Trample)
                    {
                        var lPlayers = me.GetMap().GetPlayers();
                        foreach (var player in lPlayers)
                        {
                            if (player.IsAlive() && player.IsWithinDistInMap(me, 6.0f))
                            {
                                DoCastAOE(SpellIds.Trample);
                                _events.ScheduleEvent(Events.Trample, TimeSpan.FromSeconds(4));
                                break;
                            }
                        }
                    }
                    break;
                case 6:
                    if (!_trampleCast)
                    {
                        DoCast(me, SpellIds.StaggeredDaze);
                        Talk(TextIds.EmoteTrampleCrash);
                    }
                    else
                    {
                        DoCast(me, SpellIds.FrothingRage, true);
                        Talk(TextIds.EmoteTrampleFail);
                    }
                    _movementStarted = false;
                    me.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable);
                    SetCombatMovement(true);
                    me.GetMotionMaster().MovementExpired();
                    me.GetMotionMaster().Clear();
                    me.GetMotionMaster().MoveChase(me.GetVictim());
                    AttackStart(me.GetVictim());
                    _events.ScheduleEvent(Events.MassiveCrash, TimeSpan.FromSeconds(40));
                    _events.ScheduleEvent(Events.ArcticBreath, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(25));
                    _stage = 0;
                    break;
                default:
                    break;
            }
        }

        float _trampleTargetX, _trampleTargetY, _trampleTargetZ;
        ObjectGuid _trampleTargetGUID;
        bool _movementStarted;
        bool _movementFinish;
        bool _trampleCast;
        byte _stage;
    }

    [Script]
    class spell_gormok_jump_to_hand : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RidePlayer);
        }

        public override bool Load()
        {
            if (GetCaster() && GetCaster().GetEntry() == Misc.NpcSnoboldVassal)
                return true;
            return false;
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster)
            {
                CreatureAI gormokAI = GetTarget().ToCreature().GetAI();
                if (gormokAI != null)
                {
                    Unit target = gormokAI.SelectTarget(SelectAggroTarget.Random, 0, new SnobolledTargetSelector(GetTarget()));
                    if (target)
                    {
                        gormokAI.Talk(TextIds.EmoteSnobolled);
                        caster.GetAI().DoAction(Actions.ActiveSnobold);
                        caster.CastSpell(target, SpellIds.RidePlayer, true);
                    }
                }
            }
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.ControlVehicle, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_gormok_ride_player : AuraScript
    {
        public override bool Load()
        {
            if (GetCaster() && GetCaster().GetEntry() == Misc.NpcSnoboldVassal)
                return true;
            return false;
        }

        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            if (target.GetTypeId() != TypeId.Player || !target.IsInWorld)
                return;

            if (!target.CreateVehicleKit(Misc.PlayerVehicleId, 0))
                return;

            Unit caster = GetCaster();
            if (caster)
                caster.GetAI().SetGUID(target.GetGUID(), Misc.DataNewTarget);
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveVehicleKit();
        }

        public override void Register()
        {
            OnEffectApply.Add(new EffectApplyHandler(OnApply, 0, AuraType.ControlVehicle, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.ControlVehicle, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_gormok_snobolled : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RidePlayer);
        }

        void OnPeriodic(AuraEffect aurEff)
        {
            if (!GetTarget().HasAura(SpellIds.RidePlayer))
                Remove();
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    [Script]
    class spell_jormungars_paralytic_toxin : AuraScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.Paralysis);
        }

        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster && caster.GetEntry() == CreatureIds.Acidmaw)
            {
                Creature acidMaw = caster.ToCreature();
                if (acidMaw)
                    acidMaw.GetAI().Talk(TextIds.SaySpecial, GetTarget());
            }
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(SpellIds.Paralysis);
        }

        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            if (!canBeRecalculated)
                amount = aurEff.GetAmount();

            canBeRecalculated = false;
        }

        void HandleDummy(AuraEffect aurEff)
        {
            AuraEffect slowEff = GetEffect(0);
            if (slowEff != null)
            {
                int newAmount = slowEff.GetAmount() - 10;
                if (newAmount < -100)
                    newAmount = -100;
                slowEff.ChangeAmount(newAmount);

                if (newAmount <= -100 && !GetTarget().HasAura(SpellIds.Paralysis))
                    GetTarget().CastSpell(GetTarget(), SpellIds.Paralysis, true, null, slowEff, GetCasterGUID());
            }
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(OnApply, 0, AuraType.ModDecreaseSpeed, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.ModDecreaseSpeed, AuraEffectHandleModes.Real));
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.ModDecreaseSpeed));
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleDummy, 2, AuraType.PeriodicDummy));
        }
    }

    [Script("spell_jormungars_burning_spray", SpellIds.BurningBile)]
    [Script("spell_jormungars_paralytic_spray", SpellIds.ParalyticToxin)]
    class spell_jormungars_snakes_spray : SpellScript
    {
        public spell_jormungars_snakes_spray(uint spellId)
        {
            _spellId = spellId;
        }

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(_spellId);
        }

        void HandleScript(uint effIndex)
        {
            Player target = GetHitPlayer();
            if (target)
                GetCaster().CastSpell(target, _spellId, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.SchoolDamage));
        }

        uint _spellId;
    }

    [Script]
    class spell_jormungars_paralysis : AuraScript
    {
        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster)
            {
                InstanceScript instance = caster.GetInstanceScript();
                if (instance != null)
                    if (instance.GetData(DataTypes.NorthrendBeasts) == NorthrendBeasts.SnakesInProgress || instance.GetData(DataTypes.NorthrendBeasts) == NorthrendBeasts.SnakesSpecial)
                        return;
            }

            Remove();
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(OnApply, 0, AuraType.ModStun, AuraEffectHandleModes.Real));
        }
    }
}
