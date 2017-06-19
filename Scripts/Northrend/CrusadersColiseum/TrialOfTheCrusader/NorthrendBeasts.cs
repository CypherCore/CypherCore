/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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

namespace Scripts.Northrend.CrusadersColiseum.TrialOfTheCrusader
{
    public struct Beasts
    {
        // Gormok
        public const uint EmoteSnobolled = 0;

        // Acidmaw & Dreadscale
        public const uint EmoteEnrage = 0;

        // Icehowl
        public const uint EmoteTrampleStart = 0;
        public const uint EmoteTrampleCrash = 1;
        public const uint EmoteTrampleFail = 2;

        public const uint EquipMain = 50760;
        public const uint EquipOffhand = 48040;
        public const uint EquipRanged = 47267;
        public const int EquipDone = -1;

        public const uint ModelAcidmawStationary = 29815;
        public const uint ModelAcidmawMobile = 29816;
        public const uint ModelDreadscaleStationary = 26935;
        public const uint ModelDreadscaleMobile = 24564;

        public const uint NpcSnoboldVassal = 34800;
        public const uint NpcFireBomb = 34854;
        public const uint NpcSlimePool = 35176;
        public const uint MaxSnobolds = 4;

        //Gormok
        public const uint SpellImpale = 66331;
        public const uint SpellStaggeringStomp = 67648;
        public const uint SpellRisingAnger = 66636;
        //Snobold
        public const uint SpellSnobolled = 66406;
        public const uint SpellBatter = 66408;
        public const uint SpellFireBomb = 66313;
        public const uint SpellFireBomb1 = 66317;
        public const uint SpellFireBombDot = 66318;
        public const uint SpellHeadCrack = 66407;

        //Acidmaw & Dreadscale
        public const uint SpellAcidSpit = 66880;
        public const uint SpellParalyticSpray = 66901;
        public const uint SpellAcidSpew = 66819;
        public const uint SpellParalyticBite = 66824;
        public const uint SpellSweep0 = 66794;
        public const uint SpellSummonSlimepool = 66883;
        public const uint SpellFireSpit = 66796;
        public const uint SpellMoltenSpew = 66821;
        public const uint SpellBurningBite = 66879;
        public const uint SpellBurningSpray = 66902;
        public const uint SpellSweep1 = 67646;
        public const uint SpellEmerge0 = 66947;
        public const uint SpellSubmerge0 = 66948;
        public const uint SpellEnrage = 68335;
        public const uint SpellSlimePoolEffect = 66882; //In 60s It Diameter Grows From 10y To 40y (R=R+0.25 Per Second)

        //Icehowl
        public const uint SpellFerociousButt = 66770;
        public const uint SpellMassiveCrash = 66683;
        public const uint SpellWhirl = 67345;
        public const uint SpellArcticBreath = 66689;
        public const uint SpellTrample = 66734;
        public const uint SpellFrothingRage = 66759;
        public const uint SpellStaggeredDaze = 66758;

        public const int ActionEnableFireBomb = 1;
        public const int ActionDisableFireBomb = 2;

        // Gormok
        public const uint EventImpale = 1;
        public const uint EventStaggeringStomp = 2;
        public const uint EventThrow = 3;

        // Snobold
        public const uint EventFireBomb = 4;
        public const uint EventBatter = 5;
        public const uint EventHeadCrack = 6;

        // Acidmaw & Dreadscale
        public const uint EventBite = 7;
        public const uint EventSpew = 8;
        public const uint EventSlimePool = 9;
        public const uint EventSpit = 10;
        public const uint EventSpray = 11;
        public const uint EventSweep = 12;
        public const uint EventSubmerge = 13;
        public const uint EventEmerge = 14;
        public const uint EventSummonAcidmaw = 15;

        // Icehowl
        public const uint EventFerociousButt = 16;
        public const uint EventMassiveCrash = 17;
        public const uint EventWhirl = 18;
        public const uint EventArcticBreath = 19;
        public const uint EventTrample = 20;

        public const byte PhaseMobile = 1;
        public const byte PhaseStationary = 2;
        public const byte PhaseSubmerged = 3;
    }

    [Script]
    class boss_gormok : CreatureScript
    {
        public boss_gormok() : base("boss_gormok") { }

        class boss_gormokAI : BossAI
        {
            public boss_gormokAI(Creature creature) : base(creature, DataTypes.BossBeasts) { }

            public override void Reset()
            {
                _events.ScheduleEvent(Beasts.EventImpale, RandomHelper.URand(8 * Time.InMilliseconds, 10 * Time.InMilliseconds));
                _events.ScheduleEvent(Beasts.EventStaggeringStomp, 15 * Time.InMilliseconds);
                _events.ScheduleEvent(Beasts.EventThrow, RandomHelper.URand(15 * Time.InMilliseconds, 30 * Time.InMilliseconds));

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
                instance.SetData(DataTypes.TypeNorthrendBeasts, NorthrendBeasts.GormokDone);
            }

            public override void JustReachedHome()
            {
                instance.DoUseDoorOrButton(instance.GetGuidData(GameObjectIds.MainGateDoor));
                instance.SetData(DataTypes.TypeNorthrendBeasts, (uint)EncounterState.Fail);

                me.DespawnOrUnsummon();
            }

            public override void EnterCombat(Unit who)
            {
                _EnterCombat();
                me.SetInCombatWithZone();
                instance.SetData(DataTypes.TypeNorthrendBeasts, NorthrendBeasts.GormokInProgress);

                for (sbyte i = 0; i < Beasts.MaxSnobolds; i++)
                {
                    Creature pSnobold = DoSpawnCreature(Beasts.NpcSnoboldVassal, 0, 0, 0, 0, TempSummonType.CorpseDespawn, 0);
                    if (pSnobold)
                    {
                        pSnobold.EnterVehicle(me, i);
                        pSnobold.SetInCombatWithZone();
                        pSnobold.GetAI().DoAction(Beasts.ActionEnableFireBomb);
                    }
                }
            }

            public override void DamageTaken(Unit who, ref uint damage)
            {
                // despawn the remaining passengers on death
                if (damage >= me.GetHealth())
                {
                    for (sbyte i = 0; i < Beasts.MaxSnobolds; ++i)
                    {
                        Unit pSnobold = me.GetVehicleKit().GetPassenger(i);
                        if (pSnobold)
                            pSnobold.ToCreature().DespawnOrUnsummon();
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

                _events.ExecuteEvents(eventId =>
                {
                    switch (eventId)
                    {
                        case Beasts.EventImpale:
                            DoCastVictim(Beasts.SpellImpale);
                            _events.ScheduleEvent(Beasts.EventImpale, RandomHelper.URand(8 * Time.InMilliseconds, 10 * Time.InMilliseconds));
                            return;
                        case Beasts.EventStaggeringStomp:
                            DoCastVictim(Beasts.SpellStaggeringStomp);
                            _events.ScheduleEvent(Beasts.EventStaggeringStomp, 15 * Time.InMilliseconds);
                            return;
                        case Beasts.EventThrow:
                            for (sbyte i = 0; i < Beasts.MaxSnobolds; ++i)
                            {
                                Unit pSnobold = me.GetVehicleKit().GetPassenger(i);
                                if (pSnobold)
                                {
                                    pSnobold.ExitVehicle();
                                    pSnobold.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
                                    pSnobold.ToCreature().SetReactState(ReactStates.Aggressive);
                                    pSnobold.ToCreature().GetAI().DoAction(Beasts.ActionDisableFireBomb);
                                    pSnobold.CastSpell(me, Beasts.SpellRisingAnger, true);
                                    Talk(Beasts.EmoteSnobolled);
                                    break;
                                }
                            }
                            _events.ScheduleEvent(Beasts.EventThrow, RandomHelper.URand(15 * Time.InMilliseconds, 30 * Time.InMilliseconds));
                            return;
                        default:
                            return;
                    }
                });

                DoMeleeAttackIfReady();
            }
        }

        public override CreatureAI GetAI(Creature creature)
        {
            return GetInstanceAI<boss_gormokAI>(creature);
        }
    }

    [Script]
    class npc_snobold_vassal : CreatureScript
    {
        public npc_snobold_vassal() : base("npc_snobold_vassal") { }

        class npc_snobold_vassalAI : ScriptedAI
        {
            public npc_snobold_vassalAI(Creature creature) : base(creature)
            {
                _targetDied = false;
                _instance = creature.GetInstanceScript();
                _instance.SetData(DataTypes.SnoboldCount, DataTypes.Increase);
            }

            public override void Reset()
            {
                _events.ScheduleEvent(Beasts.EventBatter, 5 * Time.InMilliseconds);
                _events.ScheduleEvent(Beasts.EventHeadCrack, 25 * Time.InMilliseconds);

                _targetGUID.Clear();
                _targetDied = false;

                //Workaround for Snobold
                me.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
            }

            public override void EnterCombat(Unit who)
            {
                _targetGUID = who.GetGUID();
                me.TauntApply(who);
                DoCast(who, Beasts.SpellSnobolled);
            }

            public override void DamageTaken(Unit pDoneBy, ref uint uiDamage)
            {
                if (pDoneBy.GetGUID() == _targetGUID)
                    uiDamage = 0;
            }

            public override void MovementInform(MovementGeneratorType type, uint id)
            {
                if (type != MovementGeneratorType.Point)
                    return;

                switch (id)
                {
                    case 0:
                        if (_targetDied)
                            me.DespawnOrUnsummon();
                        break;
                    default:
                        break;
                }
            }

            public override void JustDied(Unit killer)
            {
                Unit target = Global.ObjAccessor.GetPlayer(me, _targetGUID);
                if (target)
                    if (target.IsAlive())
                        target.RemoveAurasDueToSpell(Beasts.SpellSnobolled);
                _instance.SetData(DataTypes.SnoboldCount, DataTypes.Decrease);
            }

            public override void DoAction(int action)
            {
                switch (action)
                {
                    case Beasts.ActionEnableFireBomb:
                        _events.ScheduleEvent(Beasts.EventFireBomb, RandomHelper.URand(5 * Time.InMilliseconds, 30 * Time.InMilliseconds));
                        break;
                    case Beasts.ActionDisableFireBomb:
                        _events.CancelEvent(Beasts.EventFireBomb);
                        break;
                    default:
                        break;
                }
            }

            public override void UpdateAI(uint diff)
            {
                if (!UpdateVictim() || _targetDied)
                    return;

                Unit target = Global.ObjAccessor.GetPlayer(me, _targetGUID);
                if (target)
                {
                    if (!target.IsAlive())
                    {
                        Unit gormok = ObjectAccessor.GetCreature(me, _instance.GetGuidData(CreatureIds.Gormok));
                        if (gormok && gormok.IsAlive())
                        {
                            SetCombatMovement(false);
                            _targetDied = true;

                            // looping through Gormoks seats
                            for (sbyte i = 0; i < Beasts.MaxSnobolds; i++)
                            {
                                if (!gormok.GetVehicleKit().GetPassenger(i))
                                {
                                    me.EnterVehicle(gormok, i);
                                    DoAction(Beasts.ActionEnableFireBomb);
                                    break;
                                }
                            }
                        }
                        else if (target = SelectTarget(SelectAggroTarget.Random, 0, 0.0f, true))
                        {
                            _targetGUID = target.GetGUID();
                            me.GetMotionMaster().MoveJump(target, 15.0f, 15.0f);
                        }
                    }
                }

                _events.Update(diff);

                if (me.HasUnitState(UnitState.Casting))
                    return;

                _events.ExecuteEvents(eventId =>
                {
                    switch (eventId)
                    {
                        case Beasts.EventFireBomb:
                            {
                                if (me.GetVehicleBase())
                                {
                                    Unit fireTarget = SelectTarget(SelectAggroTarget.Random, 0, -me.GetVehicleBase().GetCombatReach(), true);
                                    if (fireTarget)
                                        me.CastSpell(fireTarget.GetPositionX(), fireTarget.GetPositionY(), fireTarget.GetPositionZ(), Beasts.SpellFireBomb, true);
                                }
                                _events.ScheduleEvent(Beasts.EventFireBomb, 20 * Time.InMilliseconds);
                                return;
                            }
                        case Beasts.EventHeadCrack:
                            // commented out while SPELL_SNOBOLLED gets fixed
                            //if (Unit target = Global.ObjAccessor.GetPlayer(me, m_uiTargetGUID))
                            DoCastVictim(Beasts.SpellHeadCrack);
                            _events.ScheduleEvent(Beasts.EventHeadCrack, 30 * Time.InMilliseconds);
                            return;
                        case Beasts.EventBatter:
                            // commented out while SPELL_SNOBOLLED gets fixed
                            //if (Unit target = Global.ObjAccessor.GetPlayer(me, m_uiTargetGUID))
                            DoCastVictim(Beasts.SpellBatter);
                            _events.ScheduleEvent(Beasts.EventBatter, 10 * Time.InMilliseconds);
                            return;
                        default:
                            return;
                    }
                });

                // do melee attack only when not on Gormoks back
                if (!me.GetVehicleBase())
                    DoMeleeAttackIfReady();
            }

            InstanceScript _instance;
            ObjectGuid _targetGUID;
            bool _targetDied;
        }

        public override CreatureAI GetAI(Creature creature)
        {
            return GetInstanceAI<npc_snobold_vassalAI>(creature);
        }
    }

    [Script]
    class npc_firebomb : CreatureScript
    {
        public npc_firebomb() : base("npc_firebomb") { }

        class npc_firebombAI : ScriptedAI
        {
            public npc_firebombAI(Creature creature) : base(creature)
            {
                _instance = creature.GetInstanceScript();
            }

            public override void Reset()
            {
                DoCast(me, Beasts.SpellFireBombDot, true);
                SetCombatMovement(false);
                me.SetReactState(ReactStates.Passive);
                me.SetDisplayId(me.GetCreatureTemplate().ModelId2);
            }

            public override void UpdateAI(uint diff)
            {
                if (_instance.GetData(DataTypes.TypeNorthrendBeasts) != NorthrendBeasts.GormokInProgress)
                    me.DespawnOrUnsummon();
            }

            InstanceScript _instance;
        }

        public override CreatureAI GetAI(Creature creature)
        {
            return GetInstanceAI<npc_firebombAI>(creature);
        }
    }

    [Script]
    class boss_acidmaw : CreatureScript
    {
        public boss_acidmaw() : base("boss_acidmaw") { }

        public class boss_acidmawAI : boss_jormungarAI
        {
            public boss_acidmawAI(Creature creature) : base(creature) { }

            public override void Reset()
            {
                base.Reset();
                BiteSpell = Beasts.SpellParalyticBite;
                SpewSpell = Beasts.SpellAcidSpew;
                SpitSpell = Beasts.SpellAcidSpit;
                SpraySpell = Beasts.SpellParalyticSpray;
                ModelStationary = Beasts.ModelAcidmawStationary;
                ModelMobile = Beasts.ModelAcidmawMobile;
                OtherWormEntry = CreatureIds.Dreadscale;

                WasMobile = true;
                Emerge();
            }
        }

        public override CreatureAI GetAI(Creature creature)
        {
            return GetInstanceAI<boss_acidmawAI>(creature);
        }
    }

    [Script]
    class boss_dreadscale : CreatureScript
    {
        public boss_dreadscale() : base("boss_dreadscale") { }

        public class boss_dreadscaleAI : boss_jormungarAI
        {
            public boss_dreadscaleAI(Creature creature) : base(creature)
            {
            }

            public override void Reset()
            {
                base.Reset();
                BiteSpell = Beasts.SpellBurningBite;
                SpewSpell = Beasts.SpellMoltenSpew;
                SpitSpell = Beasts.SpellFireSpit;
                SpraySpell = Beasts.SpellBurningSpray;
                ModelStationary = Beasts.ModelDreadscaleStationary;
                ModelMobile = Beasts.ModelDreadscaleMobile;
                OtherWormEntry = CreatureIds.Acidmaw;

                _events.SetPhase(Beasts.PhaseMobile);
                _events.ScheduleEvent(Beasts.EventSummonAcidmaw, 3 * Time.InMilliseconds);
                _events.ScheduleEvent(Beasts.EventSubmerge, 45 * Time.InMilliseconds, 0, Beasts.PhaseMobile);
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

        public override CreatureAI GetAI(Creature creature)
        {
            return GetInstanceAI<boss_dreadscaleAI>(creature);
        }
    }

    [Script]
    class npc_slime_pool : CreatureScript
    {
        public npc_slime_pool() : base("npc_slime_pool") { }

        class npc_slime_poolAI : ScriptedAI
        {
            public npc_slime_poolAI(Creature creature) : base(creature)
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
                    DoCast(me, Beasts.SpellSlimePoolEffect);
                }

                if (_instance.GetData(DataTypes.TypeNorthrendBeasts) != NorthrendBeasts.SnakesInProgress && _instance.GetData(DataTypes.TypeNorthrendBeasts) != NorthrendBeasts.SnakesSpecial)
                    me.DespawnOrUnsummon();
            }

            InstanceScript _instance;
            bool _cast;

        }

        public override CreatureAI GetAI(Creature creature)
        {
            return GetInstanceAI<npc_slime_poolAI>(creature);
        }
    }

    [Script]
    class spell_gormok_fire_bomb : SpellScriptLoader
    {
        public spell_gormok_fire_bomb() : base("spell_gormok_fire_bomb") { }

        class spell_gormok_fire_bomb_SpellScript : SpellScript
        {
            void TriggerFireBomb(uint effIndex)
            {
                Position pos = GetExplTargetDest();
                if (pos != null)
                {
                    Unit caster = GetCaster();
                    if (caster)
                        caster.SummonCreature(Beasts.NpcFireBomb, pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), 0, TempSummonType.TimedDespawn, 30 * Time.InMilliseconds);
                }
            }

            public override void Register()
            {
                OnEffectHit.Add(new EffectHandler(TriggerFireBomb, 0, SpellEffectName.TriggerMissile));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_gormok_fire_bomb_SpellScript();
        }
    }

    [Script]
    class boss_icehowl : CreatureScript
    {
        public boss_icehowl() : base("boss_icehowl") { }

        class boss_icehowlAI : BossAI
        {
            public boss_icehowlAI(Creature creature) : base(creature, DataTypes.BossBeasts) { }

            public override void Reset()
            {
                _events.ScheduleEvent(Beasts.EventFerociousButt, RandomHelper.URand(15 * Time.InMilliseconds, 30 * Time.InMilliseconds));
                _events.ScheduleEvent(Beasts.EventArcticBreath, RandomHelper.URand(15 * Time.InMilliseconds, 25 * Time.InMilliseconds));
                _events.ScheduleEvent(Beasts.EventWhirl, RandomHelper.URand(15 * Time.InMilliseconds, 30 * Time.InMilliseconds));
                _events.ScheduleEvent(Beasts.EventMassiveCrash, 30 * Time.InMilliseconds);
                _movementFinish = false;
                _trampleCast = false;
                _trampleTargetGUID.Clear();
                _trampleTargetX = 0;
                _trampleTargetY = 0;
                _trampleTargetZ = 0;
                _stage = 0;
            }

            public override void JustDied(Unit killer)
            {
                _JustDied();
                instance.SetData(DataTypes.TypeNorthrendBeasts, NorthrendBeasts.IcehowlDone);
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
                instance.SetData(DataTypes.TypeNorthrendBeasts, (uint)EncounterState.Fail);
                me.DespawnOrUnsummon();
            }

            public override void KilledUnit(Unit who)
            {
                if (who.IsTypeId(TypeId.Player))
                    instance.SetData(DataTypes.TributeToImmortalityEligible, 0);
            }

            public override void EnterCombat(Unit who)
            {
                _EnterCombat();
                instance.SetData(DataTypes.TypeNorthrendBeasts, NorthrendBeasts.IcehowlInProgress);
            }

            public override void SpellHitTarget(Unit target, SpellInfo spell)
            {
                if (spell.Id == Beasts.SpellTrample && target.IsTypeId(TypeId.Player))
                {
                    if (!_trampleCast)
                    {
                        DoCast(me, Beasts.SpellFrothingRage, true);
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
                                    case Beasts.EventFerociousButt:
                                        DoCastVictim(Beasts.SpellFerociousButt);
                                        _events.ScheduleEvent(Beasts.EventFerociousButt, RandomHelper.URand(15 * Time.InMilliseconds, 30 * Time.InMilliseconds));
                                        return;
                                    case Beasts.EventArcticBreath:
                                        Unit target = SelectTarget(SelectAggroTarget.Random, 0, 0.0f, true);
                                        if (target)
                                            DoCast(target, Beasts.SpellArcticBreath);
                                        return;
                                    case Beasts.EventWhirl:
                                        DoCastAOE(Beasts.SpellWhirl);
                                        _events.ScheduleEvent(Beasts.EventWhirl, RandomHelper.URand(15 * Time.InMilliseconds, 30 * Time.InMilliseconds));
                                        return;
                                    case Beasts.EventMassiveCrash:
                                        me.GetMotionMaster().MoveJump(MiscData.ToCCommonLoc[1], 20.0f, 20.0f, 0); // 1: Middle of the room
                                        SetCombatMovement(false);
                                        me.AttackStop();
                                        _stage = 7; //Invalid (Do nothing more than move)
                                        return;
                                    default:
                                        break;
                                }
                            });
                            DoMeleeAttackIfReady();
                            break;
                        }
                    case 1:
                        DoCastAOE(Beasts.SpellMassiveCrash);
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
                                _events.ScheduleEvent(Beasts.EventTrample, 4 * Time.InMilliseconds);
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
                                case Beasts.EventTrample:
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
                                Talk(Beasts.EmoteTrampleStart, target);

                            me.GetMotionMaster().MoveCharge(_trampleTargetX, _trampleTargetY, _trampleTargetZ, 42, 1);
                            me.SetTarget(ObjectGuid.Empty);
                            _stage = 5;
                            break;
                        }
                    case 5:
                        if (_movementFinish)
                        {
                            DoCastAOE(Beasts.SpellTrample);
                            _movementFinish = false;
                            _stage = 6;
                            return;
                        }
                        if (_events.ExecuteEvent() == Beasts.EventTrample)
                        {
                            var lPlayers = me.GetMap().GetPlayers();
                            foreach (var player in lPlayers)
                            {
                                if (player.IsAlive() && player.IsWithinDistInMap(me, 6.0f))
                                {
                                    DoCastAOE(Beasts.SpellTrample);
                                    _events.ScheduleEvent(Beasts.EventTrample, 4 * Time.InMilliseconds);
                                    break;
                                }
                            }
                        }
                        break;
                    case 6:
                        if (!_trampleCast)
                        {
                            DoCast(me, Beasts.SpellStaggeredDaze);
                            Talk(Beasts.EmoteTrampleCrash);
                        }
                        else
                        {
                            DoCast(me, Beasts.SpellFrothingRage, true);
                            Talk(Beasts.EmoteTrampleFail);
                        }
                        me.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable);
                        SetCombatMovement(true);
                        me.GetMotionMaster().MovementExpired();
                        me.GetMotionMaster().Clear();
                        me.GetMotionMaster().MoveChase(me.GetVictim());
                        AttackStart(me.GetVictim());
                        _events.ScheduleEvent(Beasts.EventMassiveCrash, 40 * Time.InMilliseconds);
                        _events.ScheduleEvent(Beasts.EventArcticBreath, RandomHelper.URand(15 * Time.InMilliseconds, 25 * Time.InMilliseconds));
                        _stage = 0;
                        break;
                    default:
                        break;
                }
            }

            float _trampleTargetX, _trampleTargetY, _trampleTargetZ;
            ObjectGuid _trampleTargetGUID;
            bool _movementFinish;
            bool _trampleCast;
            byte _stage;
        }

        public override CreatureAI GetAI(Creature creature)
        {
            return GetInstanceAI<boss_icehowlAI>(creature);
        }
    }

    class boss_jormungarAI : BossAI
    {
        public boss_jormungarAI(Creature creature) : base(creature, DataTypes.BossBeasts) { }

        public override void Reset()
        {
            Enraged = false;

            _events.ScheduleEvent(Beasts.EventSpit, RandomHelper.URand(15 * Time.InMilliseconds, 30 * Time.InMilliseconds), 0, Beasts.PhaseStationary);
            _events.ScheduleEvent(Beasts.EventSpray, RandomHelper.URand(15 * Time.InMilliseconds, 30 * Time.InMilliseconds), 0, Beasts.PhaseStationary);
            _events.ScheduleEvent(Beasts.EventSweep, RandomHelper.URand(15 * Time.InMilliseconds, 30 * Time.InMilliseconds), 0, Beasts.PhaseStationary);
            _events.ScheduleEvent(Beasts.EventBite, RandomHelper.URand(15 * Time.InMilliseconds, 30 * Time.InMilliseconds), 0, Beasts.PhaseMobile);
            _events.ScheduleEvent(Beasts.EventSpew, RandomHelper.URand(15 * Time.InMilliseconds, 30 * Time.InMilliseconds), 0, Beasts.PhaseMobile);
            _events.ScheduleEvent(Beasts.EventSlimePool, 15 * Time.InMilliseconds, 0, Beasts.PhaseMobile);
        }

        public override void JustDied(Unit killer)
        {
            Creature otherWorm = ObjectAccessor.GetCreature(me, instance.GetGuidData(OtherWormEntry));
            if (otherWorm)
            {
                if (!otherWorm.IsAlive())
                {
                    instance.SetData(DataTypes.TypeNorthrendBeasts, NorthrendBeasts.SnakesDone);

                    me.DespawnOrUnsummon();
                    otherWorm.DespawnOrUnsummon();
                }
                else
                    instance.SetData(DataTypes.TypeNorthrendBeasts, NorthrendBeasts.SnakesSpecial);
            }
        }

        public override void JustReachedHome()
        {
            // prevent losing 2 attempts at once on heroics
            if (instance.GetData(DataTypes.TypeNorthrendBeasts) != (uint)EncounterState.Fail)
                instance.SetData(DataTypes.TypeNorthrendBeasts, (uint)EncounterState.Fail);

            me.DespawnOrUnsummon();
        }

        public override void KilledUnit(Unit who)
        {
            if (who.IsTypeId(TypeId.Player))
                instance.SetData(DataTypes.TributeToImmortalityEligible, 0);
        }

        public override void EnterCombat(Unit who)
        {
            _EnterCombat();
            me.SetInCombatWithZone();
            instance.SetData(DataTypes.TypeNorthrendBeasts, NorthrendBeasts.SnakesInProgress);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            if (!Enraged && instance.GetData(DataTypes.TypeNorthrendBeasts) == NorthrendBeasts.SnakesSpecial)
            {
                me.RemoveAurasDueToSpell(Beasts.SpellSubmerge0);
                me.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
                DoCast(Beasts.SpellEnrage);
                Enraged = true;
                Talk(Beasts.EmoteEnrage);
            }

            _events.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case Beasts.EventEmerge:
                        Emerge();
                        return;
                    case Beasts.EventSubmerge:
                        Submerge();
                        return;
                    case Beasts.EventBite:
                        DoCastVictim(BiteSpell);
                        _events.ScheduleEvent(Beasts.EventBite, RandomHelper.URand(15 * Time.InMilliseconds, 30 * Time.InMilliseconds), 0, Beasts.PhaseMobile);
                        return;
                    case Beasts.EventSpew:
                        DoCastAOE(SpewSpell);
                        _events.ScheduleEvent(Beasts.EventSpew, RandomHelper.URand(15 * Time.InMilliseconds, 30 * Time.InMilliseconds), 0, Beasts.PhaseMobile);
                        return;
                    case Beasts.EventSlimePool:
                        DoCast(me, Beasts.SpellSummonSlimepool);
                        _events.ScheduleEvent(Beasts.EventSlimePool, 30 * Time.InMilliseconds, 0, Beasts.PhaseMobile);
                        return;
                    case Beasts.EventSummonAcidmaw:
                        Creature acidmaw = me.SummonCreature(CreatureIds.Acidmaw, MiscData.ToCCommonLoc[9].GetPositionX(), MiscData.ToCCommonLoc[9].GetPositionY(), MiscData.ToCCommonLoc[9].GetPositionZ(), 5, TempSummonType.ManualDespawn);
                        if (acidmaw)
                        {
                            acidmaw.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
                            acidmaw.SetReactState(ReactStates.Aggressive);
                            acidmaw.SetInCombatWithZone();
                            acidmaw.CastSpell(acidmaw, Beasts.SpellEmerge0);
                        }
                        return;
                    case Beasts.EventSpray:
                        Unit target = SelectTarget(SelectAggroTarget.Random, 0, 0.0f, true);
                        if (target)
                            DoCast(target, SpraySpell);
                        _events.ScheduleEvent(Beasts.EventSpray, RandomHelper.URand(15 * Time.InMilliseconds, 30 * Time.InMilliseconds), 0, Beasts.PhaseStationary);
                        return;
                    case Beasts.EventSweep:
                        DoCastAOE(Beasts.SpellSweep0);
                        _events.ScheduleEvent(Beasts.EventSweep, RandomHelper.URand(15 * Time.InMilliseconds, 30 * Time.InMilliseconds), 0, Beasts.PhaseStationary);
                        return;
                    default:
                        return;
                }
            });
            if (_events.IsInPhase(Beasts.PhaseMobile))
                DoMeleeAttackIfReady();
            if (_events.IsInPhase(Beasts.PhaseStationary))
                DoSpellAttackIfReady(SpitSpell);
        }

        void Submerge()
        {
            DoCast(me, Beasts.SpellSubmerge0);
            me.RemoveAurasDueToSpell(Beasts.SpellEmerge0);
            me.SetInCombatWithZone();
            _events.SetPhase(Beasts.PhaseSubmerged);
            _events.ScheduleEvent(Beasts.EventEmerge, 5 * Time.InMilliseconds, 0, Beasts.PhaseSubmerged);
            me.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
            me.GetMotionMaster().MovePoint(0, MiscData.ToCCommonLoc[1].GetPositionX() + RandomHelper.FRand(-40.0f, 40.0f), MiscData.ToCCommonLoc[1].GetPositionY() + RandomHelper.FRand(-40.0f, 40.0f), MiscData.ToCCommonLoc[1].GetPositionZ());
            WasMobile = !WasMobile;
        }

        public void Emerge()
        {
            DoCast(me, Beasts.SpellEmerge0);
            me.SetDisplayId(ModelMobile);
            me.RemoveAurasDueToSpell(Beasts.SpellSubmerge0);
            me.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
            me.GetMotionMaster().Clear();

            // if the worm was mobile before submerging, make him stationary now
            if (WasMobile)
            {
                me.SetControlled(true, UnitState.Root);
                SetCombatMovement(false);
                me.SetDisplayId(ModelStationary);
                _events.SetPhase(Beasts.PhaseStationary);
                _events.ScheduleEvent(Beasts.EventSubmerge, 45 * Time.InMilliseconds, 0, Beasts.PhaseStationary);
                _events.ScheduleEvent(Beasts.EventSpit, RandomHelper.URand(15 * Time.InMilliseconds, 30 * Time.InMilliseconds), 0, Beasts.PhaseStationary);
                _events.ScheduleEvent(Beasts.EventSpray, RandomHelper.URand(15 * Time.InMilliseconds, 30 * Time.InMilliseconds), 0, Beasts.PhaseStationary);
                _events.ScheduleEvent(Beasts.EventSweep, RandomHelper.URand(15 * Time.InMilliseconds, 30 * Time.InMilliseconds), 0, Beasts.PhaseStationary);
            }
            else
            {
                me.SetControlled(false, UnitState.Root);
                SetCombatMovement(true);
                me.GetMotionMaster().MoveChase(me.GetVictim());
                me.SetDisplayId(ModelMobile);
                _events.SetPhase(Beasts.PhaseMobile);
                _events.ScheduleEvent(Beasts.EventSubmerge, 45 * Time.InMilliseconds, 0, Beasts.PhaseMobile);
                _events.ScheduleEvent(Beasts.EventBite, RandomHelper.URand(15 * Time.InMilliseconds, 30 * Time.InMilliseconds), 0, Beasts.PhaseMobile);
                _events.ScheduleEvent(Beasts.EventSpew, RandomHelper.URand(15 * Time.InMilliseconds, 30 * Time.InMilliseconds), 0, Beasts.PhaseMobile);
                _events.ScheduleEvent(Beasts.EventSlimePool, 15 * Time.InMilliseconds, 0, Beasts.PhaseMobile);
            }
        }

        protected uint OtherWormEntry;
        protected uint ModelStationary;
        protected uint ModelMobile;

        protected uint BiteSpell;
        protected uint SpewSpell;
        protected uint SpitSpell;
        protected uint SpraySpell;

        //protected uint Phase;
        protected bool Enraged;
        protected bool WasMobile;
    }
}
