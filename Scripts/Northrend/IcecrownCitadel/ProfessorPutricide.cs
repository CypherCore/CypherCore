using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Entities;
using Game.AI;
using Game.Scripting;
using Game.Spells;
using Framework.Constants;
using Game.Maps;
using Game.DataStorage;

namespace Scripts.Northrend.IcecrownCitadel
{
    using static IccConst;
    using static ProfessorPutricideConst;

    class ProfessorPutricideConst
    {
        // Festergut
        public const uint EventFestergutDies = 1;
        public const uint EventFestergutGoo = 2;

        public const uint SayFestergutGaseousBlight = 0;
        public const uint SayFestergutDeath = 1;

        public const uint SpellReleaseGasVisual = 69125;
        public const uint SpellGaseousBlightLarge = 69157;
        public const uint SpellGaseousBlightMedium = 69162;
        public const uint SpellGaseousBlightSmall = 69164;
        public const uint SpellMalleableGooH = 72296;
        public const uint SpellMalleableGooSummon = 72299;


        // Rotface
        public const uint EventRotfaceDies = 3;
        public const uint EventRotfaceOozeFlood = 5;

        public const uint SayRotfaceOozeFlood = 2;
        public const uint SayRotfaceDeath = 3;

        // Professor Putricide
        public const uint EventBerserk = 6;    // All Phases
        public const uint EventSlimePuddle = 7;    // All Phases
        public const uint EventUnstableExperiment = 8;    // P1 && P2
        public const uint EventTearGas = 9;    // Phase Transition Not Heroic
        public const uint EventResumeAttack = 10;
        public const uint EventMalleableGoo = 11;
        public const uint EventChokingGasBomb = 12;
        public const uint EventUnboundPlague = 13;
        public const uint EventMutatedPlague = 14;
        public const uint EventPhaseTransition = 15;

        public const uint SayAggro = 4;
        public const uint EmoteUnstableExperiment = 5;
        public const uint SayPhaseTransitionHeroic = 6;
        public const uint SayTransform1 = 7;
        public const uint SayTransform2 = 8;    // Always Used For Phase2 Change; Do Not Group With public const uint SayTransform1
        public const uint EmoteMalleableGoo = 9;
        public const uint EmoteChokingGasBomb = 10;
        public const uint SayKill = 11;
        public const uint SayBerserk = 12;
        public const uint SayDeath = 13;

        public const uint SpellSlimePuddleTrigger = 70341;
        public const uint SpellMalleableGoo = 70852;
        public const uint SpellUnstableExperiment = 70351;
        public const uint SpellTearGas = 71617;    // Phase Transition
        public const uint SpellTearGasCreature = 71618;
        public const uint SpellTearGasCancel = 71620;
        public const uint SpellTearGasPeriodicTrigger = 73170;
        public const uint SpellCreateConcoction = 71621;
        public const uint SpellGuzzlePotions = 71893;
        public const uint SpellOozeTankProtection = 71770;    // Protects The Tank
        public const uint SpellChokingGasBomb = 71255;
        public const uint SpellOozeVariable = 74118;
        public const uint SpellGasVariable = 74119;
        public const uint SpellUnboundPlague = 70911;
        public const uint SpellUnboundPlagueSearcher = 70917;
        public const uint SpellPlagueSickness = 70953;
        public const uint SpellUnboundPlagueProtection = 70955;
        public const uint SpellMutatedPlague = 72451;
        public const uint SpellMutatedPlagueClear = 72618;

        // Slime Puddle
        public const uint SpellGrowStacker = 70345;
        public const uint SpellGrow = 70347;
        public const uint SpellSlimePuddleAura = 70343;

        // Gas Cloud
        public const uint SpellGaseousBloatProc = 70215;
        public const uint SpellGaseousBloat = 70672;
        public const uint SpellGaseousBloatProtection = 70812;
        public const uint SpellExpungedGas = 70701;

        // Volatile Ooze
        public const uint SpellOozeEruption = 70492;
        public const uint SpellVolatileOozeAdhesive = 70447;
        public const uint SpellOozeEruptionSearchPeriodic = 70457;
        public const uint SpellVolatileOozeProtection = 70530;

        // Choking Gas Bomb
        public const uint SpellChokingGasBombPeriodic = 71259;
        public const uint SpellChokingGasExplosionTrigger = 71280;

        // Mutated Abomination Vehicle
        public const uint SpellAbominationVehiclePowerDrain = 70385;
        public const uint SpellMutatedTransformation = 70311;
        public const uint SpellMutatedTransformationDamage = 70405;
        public const uint SpellMutatedTransformationName = 72401;

        // Unholy Infusion
        public const uint SpellUnholyInfusionCredit = 71518;

        public static Position festergutWatchPos = new Position(4324.820f, 3166.03f, 389.3831f, 3.316126f); //emote 432 (release gas)
        public static Position rotfaceWatchPos = new Position(4390.371f, 3164.50f, 389.3890f, 5.497787f); //emote 432 (release ooze)
        public static Position tablePos = new Position(4356.190f, 3262.90f, 389.4820f, 1.483530f);

        //Points
        public const uint PointFestergut = 366260;
        public const uint PointRotface = 366270;
        public const uint PointTable = 366780;

        //Data
        public const uint DataExperimentStage = 1;
        public const uint DataPhase = 2;
        public const uint DataAbomination = 3;
    }

    enum Phases
    {
        None = 0,
        Festergut = 1,
        Rotface = 2,
        Combat1 = 4,
        Combat2 = 5,
        Combat3 = 6
    }

    class AbominationDespawner
    {
        public AbominationDespawner(Unit owner)
        {
            _owner = owner;
        }

        public bool Invoke(ObjectGuid guid)
        {
            Unit summon = Global.ObjAccessor.GetUnit(_owner, guid);
            if (summon)
            {
                if (summon.GetEntry() == NPC_MUTATED_ABOMINATION_10 || summon.GetEntry() == NPC_MUTATED_ABOMINATION_25)
                {
                    Vehicle veh = summon.GetVehicleKit();
                    if (veh)
                        veh.RemoveAllPassengers(); // also despawns the vehicle

                    // Found unit is Mutated Abomination, remove it
                    return true;
                }

                // Found unit is not Mutated Abomintaion, leave it
                return false;
            }

            // No unit found, remove from SummonList
            return true;
        }

        Unit _owner;
    }

    struct RotfaceHeightCheck
    {
        public RotfaceHeightCheck(Creature rotface)
        {
            _rotface = rotface;
        }

        public bool Invoke(Creature stalker)
        {
            return stalker.GetPositionZ() < _rotface.GetPositionZ() + 5.0f;
        }

        Creature _rotface;
    }

    class boss_professor_putricide : CreatureScript
    {
        public boss_professor_putricide() : base("boss_professor_putricide") { }

        class boss_professor_putricideAI : BossAI
        {
            public boss_professor_putricideAI(Creature creature) : base(creature, DataTypes.ProfessorPutricide)
            {
                _baseSpeed = creature.GetSpeedRate(UnitMoveType.Run);
                _experimentState = EXPERIMENT_STATE_OOZE;

                _phase = Phases.None;
                _oozeFloodStage = 0;
            }

            public override void Reset()
            {
                if (!(_events.IsInPhase((byte)Phases.Rotface) || _events.IsInPhase((byte)Phases.Festergut)))
                    instance.SetBossState(DataTypes.ProfessorPutricide, EncounterState.NotStarted);
                instance.SetData(DataTypes.NauseaAchievement, 1);

                _events.Reset();
                summons.DespawnAll();
                SetPhase(Phases.Combat1);
                _experimentState = EXPERIMENT_STATE_OOZE;
                me.SetReactState(ReactStates.Defensive);
                me.SetWalk(false);
                if (me.GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Point)
                    me.GetMotionMaster().MovementExpired();

                if (instance.GetBossState(DATA_ROTFACE) == EncounterState.Done && instance.GetBossState(DataTypes.Festergut) == EncounterState.Done)
                    me.RemoveFlag(UnitFields.Flags, UnitFlags.ImmuneToPc | UnitFlags.NotSelectable);
            }

            public override void EnterCombat(Unit who)
            {
                if (_events.IsInPhase((byte)Phases.Rotface) || _events.IsInPhase((byte)Phases.Festergut))
                    return;

                if (!instance.CheckRequiredBosses(DataTypes.ProfessorPutricide, who.ToPlayer()))
                {
                    EnterEvadeMode();
                    instance.DoCastSpellOnPlayers(LIGHT_S_HAMMER_TELEPORT);
                    return;
                }

                me.setActive(true);
                _events.Reset();
                _events.ScheduleEvent(EVENT_BERSERK, 600000);
                _events.ScheduleEvent(EVENT_SLIME_PUDDLE, 10000);
                _events.ScheduleEvent(EVENT_UNSTABLE_EXPERIMENT, RandomHelper.URand(30000, 35000));
                if (IsHeroic())
                    _events.ScheduleEvent(EVENT_UNBOUND_PLAGUE, 20000);

                SetPhase(Phases.Combat1);
                Talk(SAY_AGGRO);
                DoCast(me, SPELL_OOZE_TANK_PROTECTION, true);
                DoZoneInCombat(me);

                instance.SetBossState(DataTypes.ProfessorPutricide, EncounterState.InProgress);
            }

            public override void JustReachedHome()
            {
                _JustReachedHome();
                me.SetWalk(false);
                if (_events.IsInPhase((byte)Phases.Combat1) || _events.IsInPhase((byte)Phases.Combat2) || _events.IsInPhase((byte)Phases.Combat3))
                    instance.SetBossState(DataTypes.ProfessorPutricide, EncounterState.Fail);
            }

            public override void KilledUnit(Unit victim)
            {
                if (victim.IsPlayer())
                    Talk(SAY_KILL);
            }

            public override void JustDied(Unit killer)
            {
                _JustDied();
                Talk(SAY_DEATH);

                if (Is25ManRaid() && me.HasAura(SPELL_SHADOWS_FATE))
                    DoCastAOE(SPELL_UNHOLY_INFUSION_CREDIT, true);

                DoCast(SPELL_MUTATED_PLAGUE_CLEAR);
            }

            public override void JustSummoned(Creature summon)
            {
                summons.Summon(summon);
                switch (summon.GetEntry())
                {
                    case NPC_MALLEABLE_OOZE_STALKER:
                        DoCast(summon, SPELL_MALLEABLE_GOO_H);
                        return;
                    case NPC_GROWING_OOZE_PUDDLE:
                        summon.CastSpell(summon, SPELL_GROW_STACKER, true);
                        summon.CastSpell(summon, SPELL_SLIME_PUDDLE_AURA, true);
                        // blizzard casts this spell 7 times initially (confirmed in sniff)
                        for (byte i = 0; i < 7; ++i)
                            summon.CastSpell(summon, SPELL_GROW, true);
                        break;
                    case NPC_GAS_CLOUD:
                        // no possible aura seen in sniff adding the aurastate
                        summon.ModifyAuraState(AuraStateType.Unk22, true);
                        summon.CastSpell(summon, SPELL_GASEOUS_BLOAT_PROC, true);
                        summon.ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.KnockBack, true);
                        summon.SetReactState(ReactStates.Passive);
                        break;
                    case NPC_VOLATILE_OOZE:
                        // no possible aura seen in sniff adding the aurastate
                        summon.ModifyAuraState(AuraStateType.Unk19, true);
                        summon.CastSpell(summon, SPELL_OOZE_ERUPTION_SEARCH_PERIODIC, true);
                        summon.ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.KnockBack, true);
                        summon.SetReactState(ReactStates.Passive);
                        break;
                    case NPC_CHOKING_GAS_BOMB:
                        summon.CastSpell(summon, SPELL_CHOKING_GAS_BOMB_PERIODIC, true);
                        summon.CastSpell(summon, SPELL_CHOKING_GAS_EXPLOSION_TRIGGER, true);
                        return;
                    case NPC_MUTATED_ABOMINATION_10:
                    case NPC_MUTATED_ABOMINATION_25:
                        return;
                    default:
                        break;
                }

                if (me.IsInCombat())
                    DoZoneInCombat(summon);
            }

            public override void DamageTaken(Unit attacker, ref uint damage)
            {
                switch (_phase)
                {
                    case Phases.Combat1:
                        if (HealthAbovePct(80))
                            return;
                        me.SetReactState(ReactStates.Passive);
                        DoAction(SharedActions.ChangePhase);
                        break;
                    case Phases.Combat2:
                        if (HealthAbovePct(35))
                            return;
                        me.SetReactState(ReactStates.Passive);
                        DoAction(SharedActions.ChangePhase);
                        break;
                    default:
                        break;
                }
            }

            public override void MovementInform(MovementGeneratorType type, uint id)
            {
                if (type != MovementGeneratorType.Point)
                    return;
                switch (id)
                {
                    case POINT_FESTERGUT:
                        instance.SetBossState(DataTypes.Festergut, EncounterState.InProgress); // needed here for delayed gate close
                        me.SetSpeed(UnitMoveType.Run, _baseSpeed, true);
                        DoAction(SharedActions.FestergutGas);
                        Creature festergut = ObjectAccessor.GetCreature(me, instance.GetGuidData(DataTypes.Festergut));
                        if (festergut)
                            festergut.CastSpell(festergut, SPELL_GASEOUS_BLIGHT_LARGE, false, null, null, festergut.GetGUID());
                        break;
                    case POINT_ROTFACE:
                        instance.SetBossState(DATA_ROTFACE, EncounterState.InProgress);   // needed here for delayed gate close
                        me.SetSpeed(UnitMoveType.Run, _baseSpeed, true);
                        DoAction(SharedActions.RotfaceOoze);
                        _events.ScheduleEvent(EVENT_ROTFACE_OOZE_FLOOD, 25000, 0, Phases.Rotface);
                        break;
                    case POINT_TABLE:
                        // stop attack
                        me.GetMotionMaster().MoveIdle();
                        me.SetSpeed(UnitMoveType.Run, _baseSpeed, true);
                        GameObject table = ObjectAccessor.GetGameObject(me, instance.GetGuidData(DataTypes.PutricideTable));
                        if (table)
                            me.SetFacingToObject(table);
                        // operating on new phase already
                        switch (_phase)
                        {
                            case Phases.Combat2:
                                {
                                    SpellInfo spell = Global.SpellMgr.GetSpellInfo(SPELL_CREATE_CONCOCTION);
                                    DoCast(me, SPELL_CREATE_CONCOCTION);
                                    _events.ScheduleEvent(EVENT_PHASE_TRANSITION, spell.CalcCastTime() + 100);
                                    break;
                                }
                            case Phases.Combat3:
                                {
                                    SpellInfo spell = Global.SpellMgr.GetSpellInfo(SPELL_GUZZLE_POTIONS);
                                    DoCast(me, SPELL_GUZZLE_POTIONS);
                                    _events.ScheduleEvent(EVENT_PHASE_TRANSITION, spell.CalcCastTime() + 100);
                                    break;
                                }
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }
            }

            public override void DoAction(int action)
            {
                switch (action)
                {
                    case SharedActions.FestergutCombat:
                        SetPhase(Phases.Festergut);
                        me.SetSpeed(UnitMoveType.Run, _baseSpeed * 2.0f, true);
                        me.GetMotionMaster().MovePoint(POINT_FESTERGUT, ProfessorPutricideConst.festergutWatchPos);
                        me.SetReactState(ReactStates.Passive);
                        DoZoneInCombat(me);
                        if (IsHeroic())
                            _events.ScheduleEvent(EVENT_FESTERGUT_GOO, RandomHelper.URand(13000, 18000), 0, Phases.Festergut);
                        break;
                    case SharedActions.FestergutGas:
                        Talk(SAY_FESTERGUT_GASEOUS_BLIGHT);
                        DoCast(me, SPELL_RELEASE_GAS_VISUAL, true);
                        break;
                    case SharedActions.FestergutDeath:
                        _events.ScheduleEvent(EVENT_FESTERGUT_DIES, 4000, 0, Phases.Festergut);
                        break;
                    case SharedActions.RotfaceCombat:
                        {
                            SetPhase(Phases.Rotface);
                            me.SetSpeed(UnitMoveType.Run, _baseSpeed * 2.0f, true);
                            me.GetMotionMaster().MovePoint(POINT_ROTFACE, rotfaceWatchPos);
                            me.SetReactState(ReactStates.Passive);
                            _oozeFloodStage = 0;
                            DoZoneInCombat(me);
                            // init random sequence of floods
                            Creature rotface = ObjectAccessor.GetCreature(me, instance.GetGuidData(DATA_ROTFACE));
                            if (rotface)
                            {
                                List<Creature> list = new List<Creature>();
                                rotface.GetCreatureListWithEntryInGrid(list, NPC_PUDDLE_STALKER, 50.0f);
                                list.RemoveAll(new RotfaceHeightCheck(rotface).Invoke);
                                if (list.Count() > 4)
                                {
                                    list.Sort(new ObjectDistanceOrderPred(rotface));
                                    list.RemoveRange(4, list.Count - 1);
                                }

                                byte i = 0;
                                while (!list.Empty())
                                {
                                    var itr = list[RandomHelper.IRand(0, list.Count - 1)];
                                    _oozeFloodDummyGUIDs[i++] = itr.GetGUID();
                                    list.Remove(itr);
                                }
                            }
                            break;
                        }
                    case SharedActions.RotfaceOoze:
                        Talk(SAY_ROTFACE_OOZE_FLOOD);
                        Creature dummy = ObjectAccessor.GetCreature(me, _oozeFloodDummyGUIDs[_oozeFloodStage]);
                        if (dummy)
                            dummy.CastSpell(dummy, oozeFloodSpells[_oozeFloodStage], true, null, null, me.GetGUID()); // cast from self for LoS (with prof's GUID for logs)
                        if (++_oozeFloodStage == 4)
                            _oozeFloodStage = 0;
                        break;
                    case SharedActions.RotfaceDeath:
                        _events.ScheduleEvent(EVENT_ROTFACE_DIES, 4500, 0, Phases.Rotface);
                        break;
                    case SharedActions.ChangePhase:
                        me.SetSpeed(UnitMoveType.Run, _baseSpeed * 2.0f, true);
                        _events.DelayEvents(30000);
                        me.AttackStop();
                        if (!IsHeroic())
                        {
                            DoCast(me, SPELL_TEAR_GAS);
                            _events.ScheduleEvent(EVENT_TEAR_GAS, 2500);
                        }
                        else
                        {
                            Talk(SAY_PHASE_TRANSITION_HEROIC);
                            DoCast(me, SPELL_UNSTABLE_EXPERIMENT, true);
                            DoCast(me, SPELL_UNSTABLE_EXPERIMENT, true);
                            // cast variables
                            if (Is25ManRaid())
                            {
                                List<Unit> targetList = new List<Unit>();
                                {
                                    var threatlist = me.GetThreatManager().getThreatList();
                                    foreach (var itr in threatlist)
                                        if (itr.getTarget().IsPlayer())
                                            targetList.Add(itr.getTarget());
                                }

                                int half = targetList.Count / 2;
                                // half gets ooze variable
                                while (half < targetList.Count)
                                {
                                    var itr = targetList[RandomHelper.IRand(0, targetList.Count - 1)];
                                    itr.CastSpell(itr, SPELL_OOZE_VARIABLE, true);
                                    targetList.Remove(itr);
                                }
                                // and half gets gas
                                foreach (var itr in targetList)
                                    itr.CastSpell(itr, SPELL_GAS_VARIABLE, true);
                            }
                            me.GetMotionMaster().MovePoint(POINT_TABLE, ProfessorPutricideConst.tablePos);
                        }
                        switch (_phase)
                        {
                            case Phases.Combat1:
                                SetPhase(Phases.Combat2);
                                _events.ScheduleEvent(EVENT_MALLEABLE_GOO, RandomHelper.URand(21000, 26000));
                                _events.ScheduleEvent(EVENT_CHOKING_GAS_BOMB, RandomHelper.URand(35000, 40000));
                                break;
                            case Phases.Combat2:
                                SetPhase(Phases.Combat3);
                                _events.ScheduleEvent(EVENT_MUTATED_PLAGUE, 25000);
                                _events.CancelEvent(EVENT_UNSTABLE_EXPERIMENT);
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }
            }

            public override uint GetData(uint type)
            {
                switch (type)
                {
                    case DataExperimentStage:
                        return _experimentState ? 1 : 0u;
                    case DataPhase:
                        return (uint)_phase;
                    case DataAbomination:
                        return (summons.HasEntry(NPC_MUTATED_ABOMINATION_10) || summons.HasEntry(NPC_MUTATED_ABOMINATION_25)) ? 1 : 0u;
                    default:
                        break;
                }

                return 0;
            }

            public override void SetData(uint id, uint data)
            {
                if (id == DataExperimentStage)
                    _experimentState = data != 0;
            }

            public override void UpdateAI(uint diff)
            {
                if ((!(_events.IsInPhase((byte)Phases.Rotface) || _events.IsInPhase((byte)Phases.Festergut)) && !UpdateVictim()) || !CheckInRoom())
                    return;

                _events.Update(diff);

                if (me.HasUnitState(UnitState.Casting))
                    return;
                _events.ExecuteEvents(eventId =>
                {
                    switch (eventId)
                    {
                        case EVENT_FESTERGUT_DIES:
                            Talk(SAY_FESTERGUT_DEATH);
                            EnterEvadeMode();
                            break;
                        case EVENT_FESTERGUT_GOO:
                            me.CastCustomSpell(SPELL_MALLEABLE_GOO_SUMMON, SpellValueMod.MaxTargets, 1, null, true);
                            _events.ScheduleEvent(EVENT_FESTERGUT_GOO, (Is25ManRaid() ? 10000 : 30000) + RandomHelper.URand(0, 5000), 0, Phases.Festergut);
                            break;
                        case EVENT_ROTFACE_DIES:
                            Talk(SAY_ROTFACE_DEATH);
                            EnterEvadeMode();
                            break;
                        case EVENT_ROTFACE_OOZE_FLOOD:
                            DoAction(SharedActions.RotfaceOoze);
                            _events.ScheduleEvent(EVENT_ROTFACE_OOZE_FLOOD, 25000, 0, Phases.Rotface);
                            break;
                        case EVENT_BERSERK:
                            Talk(SAY_BERSERK);
                            DoCast(me, SPELL_BERSERK2);
                            break;
                        case EVENT_SLIME_PUDDLE:
                            {
                                List<Unit> targets = SelectTargetList(2, SelectAggroTarget.Random, 0.0f, true);
                                if (!targets.Empty())
                                {
                                    foreach (var itr in targets)
                                        DoCast(itr, SPELL_SLIME_PUDDLE_TRIGGER);
                                }
                                _events.ScheduleEvent(EVENT_SLIME_PUDDLE, 35000);
                                break;
                            }
                        case EVENT_UNSTABLE_EXPERIMENT:
                            Talk(EMOTE_UNSTABLE_EXPERIMENT);
                            DoCast(me, SPELL_UNSTABLE_EXPERIMENT);
                            _events.ScheduleEvent(EVENT_UNSTABLE_EXPERIMENT, RandomHelper.URand(35000, 40000));
                            break;
                        case EVENT_TEAR_GAS:
                            me.GetMotionMaster().MovePoint(POINT_TABLE, ProfessorPutricideConst.tablePos);
                            DoCast(me, SPELL_TEAR_GAS_PERIODIC_TRIGGER, true);
                            break;
                        case EVENT_RESUME_ATTACK:
                            me.SetReactState(ReactStates.Defensive);
                            AttackStart(me.GetVictim());
                            // remove Tear Gas
                            me.RemoveAurasDueToSpell(SPELL_TEAR_GAS_PERIODIC_TRIGGER);
                            instance.DoRemoveAurasDueToSpellOnPlayers(71615);
                            DoCastAOE(SPELL_TEAR_GAS_CANCEL);
                            instance.DoRemoveAurasDueToSpellOnPlayers(SPELL_GAS_VARIABLE);
                            instance.DoRemoveAurasDueToSpellOnPlayers(SPELL_OOZE_VARIABLE);
                            break;
                        case EVENT_MALLEABLE_GOO:
                            if (Is25ManRaid())
                            {
                                List<Unit> targets = SelectTargetList(2, SelectAggroTarget.Random, -7.0f, true);
                                if (!targets.Empty())
                                {
                                    Talk(EMOTE_MALLEABLE_GOO);
                                    foreach (var itr in targets)
                                        DoCast(itr, SPELL_MALLEABLE_GOO);
                                }
                            }
                            else
                            {
                                Unit target = SelectTarget(SelectAggroTarget.Random, 1, -7.0f, true);
                                if (target)
                                {
                                    Talk(EMOTE_MALLEABLE_GOO);
                                    DoCast(target, SPELL_MALLEABLE_GOO);
                                }
                            }
                            _events.ScheduleEvent(EVENT_MALLEABLE_GOO, RandomHelper.URand(25000, 30000));
                            break;
                        case EVENT_CHOKING_GAS_BOMB:
                            Talk(EMOTE_CHOKING_GAS_BOMB);
                            DoCast(me, SPELL_CHOKING_GAS_BOMB);
                            _events.ScheduleEvent(EVENT_CHOKING_GAS_BOMB, RandomHelper.URand(35000, 40000));
                            break;
                        case EVENT_UNBOUND_PLAGUE:
                            {
                                Unit target = SelectTarget(SelectAggroTarget.Random, 0, new NonTankTargetSelector(me));
                                if (target)
                                {
                                    DoCast(target, SPELL_UNBOUND_PLAGUE);
                                    DoCast(target, SPELL_UNBOUND_PLAGUE_SEARCHER);
                                }
                                _events.ScheduleEvent(EVENT_UNBOUND_PLAGUE, 90000);
                            }
                            break;
                        case EVENT_MUTATED_PLAGUE:
                            DoCastVictim(SPELL_MUTATED_PLAGUE);
                            _events.ScheduleEvent(EVENT_MUTATED_PLAGUE, 10000);
                            break;
                        case EVENT_PHASE_TRANSITION:
                            {
                                switch (_phase)
                                {
                                    case Phases.Combat2:
                                        {
                                            Creature face = me.FindNearestCreature(NPC_TEAR_GAS_TARGET_STALKER, 50.0f);
                                            if (face)
                                                me.SetFacingToObject(face);
                                            me.HandleEmoteCommand(EMOTE_ONESHOT_KNEEL);
                                            Talk(SAY_TRANSFORM_1);
                                            _events.ScheduleEvent(EVENT_RESUME_ATTACK, 5500, 0, Phases.Combat2);
                                        }
                                        break;
                                    case Phases.Combat3:
                                        { Creature face = me.FindNearestCreature(NPC_TEAR_GAS_TARGET_STALKER, 50.0f);
                                            if (face)
                                                me.SetFacingToObject(face);
                                            me.HandleEmoteCommand(EMOTE_ONESHOT_KNEEL);
                                            Talk(SAY_TRANSFORM_2);
                                            summons.DespawnIf(new AbominationDespawner(me).Invoke);
                                            _events.ScheduleEvent(EVENT_RESUME_ATTACK, 8500, 0, Phases.Combat3);
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            }
                            break;
                        default:
                            break;
                    }
                });

                DoMeleeAttackIfReady();
            }

            void SetPhase(Phases newPhase)
            {
                _phase = newPhase;
                _events.SetPhase((byte)newPhase);
            }

            ObjectGuid[] _oozeFloodDummyGUIDs = new ObjectGuid[4];
            Phases _phase;          // external of EventMap because event phase gets reset on evade
            float _baseSpeed;
            byte _oozeFloodStage;
            bool _experimentState;
        }

        public override CreatureAI GetAI(Creature creature)
        {
            return GetIcecrownCitadelAI<boss_professor_putricideAI>(creature);
        }
    }

    class npc_putricide_oozeAI : ScriptedAI
    {
        public npc_putricide_oozeAI(Creature creature, uint hitTargetSpellId) : base(creature)
        {
            _hitTargetSpellId = hitTargetSpellId;
            _newTargetSelectTimer = 0;

        }

        public override void SpellHitTarget(Unit target, SpellInfo spell)
        {
            if (_newTargetSelectTimer == 0 && spell.Id == _hitTargetSpellId)
                _newTargetSelectTimer = 1000;
        }

        public override void SpellHit(Unit caster, SpellInfo spell)
        {
            if (spell.Id == SPELL_TEAR_GAS_CREATURE)
                _newTargetSelectTimer = 1000;
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim() && _newTargetSelectTimer == 0)
                return;

            if (_newTargetSelectTimer == 0 && !me.IsNonMeleeSpellCast(false, false, true, false, true))
                _newTargetSelectTimer = 1000;

            DoMeleeAttackIfReady();

            if (_newTargetSelectTimer == 0)
                return;

            if (me.HasAura(SPELL_TEAR_GAS_CREATURE))
                return;

            if (_newTargetSelectTimer <= diff)
            {
                _newTargetSelectTimer = 0;
                CastMainSpell();
            }
            else
                _newTargetSelectTimer -= diff;
        }

        public virtual void CastMainSpell() { }

        uint _hitTargetSpellId;
        uint _newTargetSelectTimer;
    }

    class npc_volatile_ooze : CreatureScript
    {
        public npc_volatile_ooze() : base("npc_volatile_ooze") { }

        class npc_volatile_oozeAI : npc_putricide_oozeAI
        {
            public npc_volatile_oozeAI(Creature creature) : base(creature, SPELL_OOZE_ERUPTION)
            {
            }

            public override void CastMainSpell()
            {
                me.CastSpell(me, ProfessorPutricideConst.SpellVolatileOozeAdhesive, false);
            }
        }

        public override CreatureAI GetAI(Creature creature)
        {
            return GetIcecrownCitadelAI<npc_volatile_oozeAI>(creature);
        }
    }

    class npc_gas_cloud : CreatureScript
    {
        public npc_gas_cloud() : base("npc_gas_cloud") { }

        class npc_gas_cloudAI : npc_putricide_oozeAI
        {
            public npc_gas_cloudAI(Creature creature) : base(creature, SPELL_EXPUNGED_GAS)
            {
                _newTargetSelectTimer = 0;
            }

            public override void CastMainSpell()
            {
                me.CastCustomSpell(SPELL_GASEOUS_BLOAT, SpellValueMod.AuraStack, 10, me, false);
            }

            uint _newTargetSelectTimer;
        }

        public override CreatureAI GetAI(Creature creature)
        {
            return GetIcecrownCitadelAI<npc_gas_cloudAI>(creature);
        }
    }

    class spell_putricide_gaseous_bloat : SpellScriptLoader
    {
        public spell_putricide_gaseous_bloat() : base("spell_putricide_gaseous_bloat") { }

        class spell_putricide_gaseous_bloat_AuraScript : AuraScript
        {
            void HandleExtraEffect(AuraEffect aurEff)
            {
                Unit target = GetTarget();
                Unit caster = GetCaster();
                if (caster)
                {
                    target.RemoveAuraFromStack(GetSpellInfo().Id, GetCasterGUID());
                    if (!target.HasAura(GetId()))
                        caster.CastCustomSpell(SPELL_GASEOUS_BLOAT, SpellValueMod.AuraStack, 10, caster, false);
                }
            }

            public override void Register()
            {
                OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleExtraEffect, 0, SPELL_AURA_PERIODIC_DAMAGE));
            }
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_putricide_gaseous_bloat_AuraScript();
        }
    }

    class spell_putricide_ooze_channel : SpellScriptLoader
    {
        public spell_putricide_ooze_channel() : base("spell_putricide_ooze_channel") { }

        class spell_putricide_ooze_channel_SpellScript : SpellScript
        {
            public spell_putricide_ooze_channel_SpellScript()
            {
                _target = null;
            }

            public override bool Validate(SpellInfo spell)
            {
                if (spell.ExcludeTargetAuraSpell == 0)
                    return false;
                if (!Global.SpellMgr.HasSpellInfo(spell.ExcludeTargetAuraSpell))
                    return false;
                return true;
            }

            // set up initial variables and check if caster is creature
            // this will let use safely use ToCreature() casts in entire script
            public override bool Load()
            {
                return GetCaster().IsTypeId(TypeId.Unit);
            }

            void SelectTarget(List<WorldObject> targets)
            {
                if (targets.Empty())
                {
                    FinishCast(SpellCastResult.NoValidTargets);
                    GetCaster().ToCreature().DespawnOrUnsummon(1);    // despawn next update
                    return;
                }

                WorldObject target = targets.PickRandom();
                targets.Clear();
                targets.Add(target);
                _target = target;
            }

            void SetTarget(List<WorldObject> targets)
            {
                targets.Clear();
                if (_target)
                    targets.Add(_target);
            }

            void StartAttack()
            {
                GetCaster().ClearUnitState(UnitState.Casting);
                GetCaster().DeleteThreatList();
                GetCaster().ToCreature().GetAI().AttackStart(GetHitUnit());
                GetCaster().AddThreat(GetHitUnit(), 500000000.0f);    // value seen in sniff
            }

            public override void Register()
            {
                OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(SelectTarget, 0, Targets.UnitSrcAreaEnemy));
                OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(SetTarget, 1, Targets.UnitSrcAreaEnemy));
                OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(SetTarget, 2, Targets.UnitSrcAreaEnemy));
                AfterHit.Add(new HitHandler(StartAttack));
            }

            WorldObject _target;
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_putricide_ooze_channel_SpellScript();
        }
    }

    class ExactDistanceCheck
    {
        public ExactDistanceCheck(Unit source, float dist)
        {
            _source = source;
            _dist = dist;
        }

        public bool Invoke(WorldObject unit)
        {
            return _source.GetExactDist2d(unit) > _dist;
        }

        Unit _source;
        float _dist;
    }

    class spell_putricide_slime_puddle : SpellScriptLoader
    {
        public spell_putricide_slime_puddle() : base("spell_putricide_slime_puddle") { }

        class spell_putricide_slime_puddle_SpellScript : SpellScript
        {
            void ScaleRange(List<WorldObject> targets)
            {
                targets.RemoveAll(new ExactDistanceCheck(GetCaster(), 2.5f * GetCaster().GetObjectScale()).Invoke);
            }

            public override void Register()
            {
                OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(ScaleRange, 0, Targets.UnitDestAreaEntry));
                OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(ScaleRange, 1, Targets.UnitDestAreaEntry));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_putricide_slime_puddle_SpellScript();
        }
    }

    // this is here only because on retail you dont actually enter HEROIC mode for ICC
    class spell_putricide_slime_puddle_aura : SpellScriptLoader
    {
        public spell_putricide_slime_puddle_aura() : base("spell_putricide_slime_puddle_aura") { }

        class spell_putricide_slime_puddle_aura_SpellScript : SpellScript
        {
            void ReplaceAura()
            {
                Unit target = GetHitUnit();
                if (target)
                    GetCaster().AddAura(Convert.ToBoolean((int)GetCaster().GetMap().GetSpawnMode() & 1) ? 72456 : 70346u, target);
            }

            public override void Register()
            {
                OnHit.Add(new HitHandler(ReplaceAura));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_putricide_slime_puddle_aura_SpellScript();
        }
    }

    class spell_putricide_unstable_experiment : SpellScriptLoader
    {
        public spell_putricide_unstable_experiment() : base("spell_putricide_unstable_experiment") { }

        class spell_putricide_unstable_experiment_SpellScript : SpellScript
        {
            void HandleScript(uint effIndex)
            {
                PreventHitDefaultEffect(effIndex);
                if (!GetCaster().IsTypeId(TypeId.Unit))
                    return;

                Creature creature = GetCaster().ToCreature();

                uint stage = creature.GetAI().GetData(DataExperimentStage);
                creature.GetAI().SetData(DataExperimentStage, stage ^ 1);

                Creature target = null;
                List<Creature> creList = new List<Creature>();
                GetCaster().GetCreatureListWithEntryInGrid(creList, NPC_ABOMINATION_WING_MAD_SCIENTIST_STALKER, 200.0f);
                // 2 of them are spawned at green place - weird trick blizz
                foreach (var itr in creList)
                {
                    target = itr;
                    List<Creature> tmp = new List<Creature>();
                    target.GetCreatureListWithEntryInGrid(tmp, NPC_ABOMINATION_WING_MAD_SCIENTIST_STALKER, 10.0f);
                    if ((stage == 0 && tmp.Count > 1) || (stage != 0 && tmp.Count == 1))
                        break;
                }

                GetCaster().CastSpell(target, (uint)GetSpellInfo().GetEffect(stage).CalcValue(), true, null, null, GetCaster().GetGUID());
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_putricide_unstable_experiment_SpellScript();
        }
    }

    class spell_putricide_ooze_eruption_searcher : SpellScriptLoader
    {
        public spell_putricide_ooze_eruption_searcher() : base("spell_putricide_ooze_eruption_searcher") { }

        class spell_putricide_ooze_eruption_searcher_SpellScript : SpellScript
        {
            void HandleDummy(uint effIndex)
            {
                if (GetHitUnit().HasAura(SPELL_VOLATILE_OOZE_ADHESIVE))
                {
                    GetHitUnit().RemoveAurasDueToSpell(SPELL_VOLATILE_OOZE_ADHESIVE, GetCaster().GetGUID(), 0, AuraRemoveMode.EnemySpell);
                    GetCaster().CastSpell(GetHitUnit(), SPELL_OOZE_ERUPTION, true);
                }
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_putricide_ooze_eruption_searcher_SpellScript();
        }
    }

    class spell_putricide_choking_gas_bomb : SpellScriptLoader
    {
        public spell_putricide_choking_gas_bomb() : base("spell_putricide_choking_gas_bomb") { }

        class spell_putricide_choking_gas_bomb_SpellScript : SpellScript
        {
            void HandleScript(uint effIndex)
            {
                uint skipIndex = RandomHelper.URand(0, 2);
                foreach (SpellEffectInfo effect in GetSpellInfo().GetEffectsForDifficulty(GetCaster().GetMap().GetDifficultyID()))
                {
                    if (effect == null || effect.EffectIndex == skipIndex)
                        continue;

                    uint spellId = (uint)effect.CalcValue();
                    GetCaster().CastSpell(GetCaster(), spellId, true, null, null, GetCaster().GetGUID());
                }
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_putricide_choking_gas_bomb_SpellScript();
        }
    }

    class spell_putricide_unbound_plague : SpellScriptLoader
    {
        public spell_putricide_unbound_plague() : base("spell_putricide_unbound_plague") { }

        class spell_putricide_unbound_plague_SpellScript : SpellScript
        {
            public override bool Validate(SpellInfo spell)
            {
                if (!Global.SpellMgr.GetSpellInfo(SPELL_UNBOUND_PLAGUE))
                    return false;
                if (!Global.SpellMgr.GetSpellInfo(SPELL_UNBOUND_PLAGUE_SEARCHER))
                    return false;
                return true;
            }

            void FilterTargets(List<WorldObject> targets)
            {
                AuraEffect eff = GetCaster().GetAuraEffect(SPELL_UNBOUND_PLAGUE_SEARCHER, 0);
                if (eff != null)
                {
                    if (eff.GetTickNumber() < 2)
                    {
                        targets.Clear();
                        return;
                    }
                }


                targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, SPELL_UNBOUND_PLAGUE));
                targets.RandomResize(1);
            }

            void HandleScript(uint effIndex)
            {
                if (!GetHitUnit())
                    return;

                InstanceScript instance = GetCaster().GetInstanceScript();
                if (instance == null)
                    return;

                if (!GetHitUnit().HasAura(SPELL_UNBOUND_PLAGUE))
                {
                    Creature professor = ObjectAccessor.GetCreature(GetCaster(), instance.GetGuidData(DataTypes.ProfessorPutricide));
                    if (professor)
                    {
                        Aura oldPlague = GetCaster().GetAura(SPELL_UNBOUND_PLAGUE, professor.GetGUID());
                        if (oldPlague != null)
                        {
                            Aura newPlague = professor.AddAura(SPELL_UNBOUND_PLAGUE, GetHitUnit());
                            if (newPlague != null)
                            {
                                newPlague.SetMaxDuration(oldPlague.GetMaxDuration());
                                newPlague.SetDuration(oldPlague.GetDuration());
                                oldPlague.Remove();
                                GetCaster().RemoveAurasDueToSpell(SPELL_UNBOUND_PLAGUE_SEARCHER);
                                GetCaster().CastSpell(GetCaster(), SPELL_PLAGUE_SICKNESS, true);
                                GetCaster().CastSpell(GetCaster(), SPELL_UNBOUND_PLAGUE_PROTECTION, true);
                                professor.CastSpell(GetHitUnit(), SPELL_UNBOUND_PLAGUE_SEARCHER, true);
                            }
                        }
                    }
                }
            }

            public override void Register()
            {
                OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaAlly));
                OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_putricide_unbound_plague_SpellScript();
        }
    }

    class spell_putricide_eat_ooze : SpellScriptLoader
    {
        public spell_putricide_eat_ooze() : base("spell_putricide_eat_ooze") { }

        class spell_putricide_eat_ooze_SpellScript : SpellScript
        {
            void SelectTarget(List<WorldObject> targets)
            {
                if (targets.Empty())
                    return;

                targets.Sort(new ObjectDistanceOrderPred(GetCaster()));
                WorldObject target = targets[0];
                targets.Clear();
                targets.Add(target);
            }

            void HandleScript(uint effIndex)
            {
                Creature target = GetHitCreature();
                if (!target)
                    return;

                Aura grow = target.GetAura((uint)GetEffectValue());
                if (grow != null)
                {
                    if (grow.GetStackAmount() < 3)
                    {
                        target.RemoveAurasDueToSpell(SPELL_GROW_STACKER);
                        target.RemoveAura(grow);
                        target.DespawnOrUnsummon(1);
                    }
                    else
                        grow.ModStackAmount(-3);
                }
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
                OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(SelectTarget, 0, Targets.UnitDestAreaEntry));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_putricide_eat_ooze_SpellScript();
        }
    }

    class spell_putricide_mutated_plague : SpellScriptLoader
    {
        public spell_putricide_mutated_plague() : base("spell_putricide_mutated_plague") { }

        class spell_putricide_mutated_plague_AuraScript : AuraScript
        {
            void HandleTriggerSpell(AuraEffect aurEff)
            {
                PreventDefaultAction();
                Unit caster = GetCaster();
                if (!caster)
                    return;

                uint triggerSpell = GetSpellInfo().GetEffect(aurEff.GetEffIndex()).TriggerSpell;
                SpellInfo spell = Global.SpellMgr.GetSpellInfo(triggerSpell);

                int damage = spell.GetEffect(0).CalcValue(caster);
                float multiplier = 2.0f;
                if (Convert.ToBoolean((int)GetTarget().GetMap().GetSpawnMode() & 1))
                    multiplier = 3.0f;

                damage *= (int)Math.Pow(multiplier, GetStackAmount());
                damage = (int)(damage * 1.5f);

                GetTarget().CastCustomSpell(triggerSpell, SpellValueMod.BasePoint0, damage, GetTarget(), true, null, aurEff, GetCasterGUID());
            }

            void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
            {
                uint healSpell = (uint)GetSpellInfo().GetEffect(0).CalcValue();
                SpellInfo healSpellInfo = Global.SpellMgr.GetSpellInfo(healSpell);

                if (healSpellInfo == null)
                    return;

                int heal = healSpellInfo.GetEffect(0).CalcValue() * GetStackAmount();
                GetTarget().CastCustomSpell(healSpell, SpellValueMod.BasePoint0, heal, GetTarget(), true, null, null, GetCasterGUID());
            }

            public override void Register()
            {
                OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleTriggerSpell, 0, SPELL_AURA_PERIODIC_TRIGGER_SPELL));
                AfterEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, SPELL_AURA_PERIODIC_TRIGGER_SPELL, AuraEffectHandleModes.Real));
            }
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_putricide_mutated_plague_AuraScript();
        }
    }

    class spell_putricide_mutation_init : SpellScriptLoader
    {
        public spell_putricide_mutation_init() : base("spell_putricide_mutation_init") { }

        class spell_putricide_mutation_init_SpellScript : SpellScript
        {
            SpellCastResult CheckRequirementInternal(SpellCustomErrors extendedError)
            {
                InstanceScript instance = GetExplTargetUnit().GetInstanceScript();
                if (instance == null)
                    return SpellCastResult.CantDoThatRightNow;

                Creature professor = ObjectAccessor.GetCreature(GetExplTargetUnit(), instance.GetGuidData(DataTypes.ProfessorPutricide));
                if (!professor)
                    return SpellCastResult.CantDoThatRightNow;

                if (professor.GetAI().GetData(DataPhase) == (uint)Phases.Combat3 || !professor.IsAlive())
                {
                    extendedError = SpellCustomErrors.AllPotionsUsed;
                    return SpellCastResult.CustomError;
                }

                if (professor.GetAI().GetData(DataAbomination) != 0)
                {
                    extendedError = SpellCustomErrors.TooManyAbominations;
                    return SpellCastResult.CustomError;
                }

                return SpellCastResult.SpellCastOk;
            }

            SpellCastResult CheckRequirement()
            {
                if (!GetExplTargetUnit())
                    return SpellCastResult.BadTargets;

                if (!GetExplTargetUnit().IsPlayer())
                    return SpellCastResult.TargetNotPlayer;

                SpellCustomErrors extension = SpellCustomErrors.None;
                SpellCastResult result = CheckRequirementInternal(extension);
                if (result != SpellCastResult.SpellCastOk)
                {
                    Spell.SendCastResult(GetExplTargetUnit().ToPlayer(), GetSpellInfo(), 0, result, extension);
                    return result;
                }

                return SpellCastResult.SpellCastOk;
            }

            public override void Register()
            {
                OnCheckCast.Add(new CheckCastHandler(CheckRequirement));
            }
        }

        class spell_putricide_mutation_init_AuraScript : AuraScript
        {
            void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
            {
                uint spellId = 70311;
                if (Convert.ToBoolean((int)GetTarget().GetMap().GetSpawnMode() & 1))
                    spellId = 71503;

                GetTarget().CastSpell(GetTarget(), spellId, true);
            }

            public override void Register()
            {
                AfterEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, SPELL_AURA_DUMMY, AuraEffectHandleModes.Real));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_putricide_mutation_init_SpellScript();
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_putricide_mutation_init_AuraScript();
        }
    }

    class spell_putricide_mutated_transformation_dismiss : SpellScriptLoader
    {
        public spell_putricide_mutated_transformation_dismiss() : base("spell_putricide_mutated_transformation_dismiss") { }

        class spell_putricide_mutated_transformation_dismiss_AuraScript : AuraScript
        {
            void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
            {
                Vehicle veh = GetTarget().GetVehicleKit();
                if (veh)
                    veh.RemoveAllPassengers();
            }

            public override void Register()
            {
                AfterEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, SPELL_AURA_PERIODIC_TRIGGER_SPELL, AuraEffectHandleModes.Real));
            }
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_putricide_mutated_transformation_dismiss_AuraScript();
        }
    }

    class spell_putricide_mutated_transformation : SpellScriptLoader
    {
        public spell_putricide_mutated_transformation() : base("spell_putricide_mutated_transformation") { }

        class spell_putricide_mutated_transformation_SpellScript : SpellScript
        {
            void HandleSummon(uint effIndex)
            {
                PreventHitDefaultEffect(effIndex);
                Unit caster = GetOriginalCaster();
                if (!caster)
                    return;

                InstanceScript instance = caster.GetInstanceScript();
                if (instance == null)
                    return;

                Creature putricide = ObjectAccessor.GetCreature(caster, instance.GetGuidData(DataTypes.ProfessorPutricide));
                if (!putricide)
                    return;

                if (putricide.GetAI().GetData(DataAbomination) != 0)
                {
                    Player player = caster.ToPlayer();
                    if (player)
                        Spell.SendCastResult(player, GetSpellInfo(), 0, SpellCastResult.CustomError, SpellCustomErrors.TooManyAbominations);
                    return;
                }

                uint entry = (uint)GetSpellInfo().GetEffect(effIndex).MiscValue;
                SummonPropertiesRecord properties = CliDB.SummonPropertiesStorage.LookupByKey(GetSpellInfo().GetEffect(effIndex).MiscValueB);
                uint duration = (uint)GetSpellInfo().GetDuration();

                Position pos = caster.GetPosition();
                TempSummon summon = caster.GetMap().SummonCreature(entry, pos, properties, duration, caster, GetSpellInfo().Id);
                if (!summon || !summon.IsVehicle())
                    return;

                summon.CastSpell(summon, SPELL_ABOMINATION_VEHICLE_POWER_DRAIN, true);
                summon.CastSpell(summon, SPELL_MUTATED_TRANSFORMATION_DAMAGE, true);
                caster.CastSpell(summon, SPELL_MUTATED_TRANSFORMATION_NAME, true);

                caster.EnterVehicle(summon, 0);    // VEHICLE_SPELL_RIDE_HARDCODED is used according to sniff, this is ok
                summon.SetCreatorGUID(caster.GetGUID());
                putricide.GetAI().JustSummoned(summon);
            }

            public override void Register()
            {
                OnEffectHit.Add(new EffectHandler(HandleSummon, 0, SpellEffectName.Summon));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_putricide_mutated_transformation_SpellScript();
        }
    }

    class spell_putricide_mutated_transformation_dmg : SpellScriptLoader
    {
        public spell_putricide_mutated_transformation_dmg() : base("spell_putricide_mutated_transformation_dmg") { }

        class spell_putricide_mutated_transformation_dmg_SpellScript : SpellScript
        {
            void FilterTargetsInitial(List<WorldObject> targets)
            {
                Unit owner = Global.ObjAccessor.GetUnit(GetCaster(), GetCaster().GetCreatorGUID());
                if (owner)
                    targets.Remove(owner);
            }

            public override void Register()
            {
                OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargetsInitial, 0, Targets.UnitSrcAreaAlly));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_putricide_mutated_transformation_dmg_SpellScript();
        }
    }

    class spell_putricide_regurgitated_ooze : SpellScriptLoader
    {
        public spell_putricide_regurgitated_ooze() : base("spell_putricide_regurgitated_ooze") { }

        class spell_putricide_regurgitated_ooze_SpellScript : SpellScript
        {
            // the only purpose of this hook is to fail the achievement
            void ExtraEffect(uint effIndex)
            {
                InstanceScript instance = GetCaster().GetInstanceScript();
                if (instance != null)
                    instance.SetData(DataTypes.NauseaAchievement, 0);
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(ExtraEffect, 0, SpellEffectName.ApplyAura));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_putricide_regurgitated_ooze_SpellScript();
        }
    }

    // Removes aura with id stored in effect value
    class spell_putricide_clear_aura_effect_value : SpellScriptLoader
    {
        public spell_putricide_clear_aura_effect_value() : base("spell_putricide_clear_aura_effect_value") { }

        class spell_putricide_clear_aura_effect_value_SpellScript : SpellScript
        {
            void HandleScript(uint effIndex)
            {
                PreventHitDefaultEffect(effIndex);
                GetHitUnit().RemoveAurasDueToSpell((uint)GetEffectValue());
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_putricide_clear_aura_effect_value_SpellScript();
        }
    }

    // Stinky and Precious spell, it's here because its used for both (Festergut and Rotface "pets")
    class spell_stinky_precious_decimate : SpellScriptLoader
    {
        public spell_stinky_precious_decimate() : base("spell_stinky_precious_decimate") { }

        class spell_stinky_precious_decimate_SpellScript : SpellScript
        {
            void HandleScript(uint effIndex)
            {
                if (GetHitUnit().GetHealthPct() > GetEffectValue())
                {
                    uint newHealth = GetHitUnit().GetMaxHealth() * (uint)GetEffectValue() / 100;
                    GetHitUnit().SetHealth(newHealth);
                }
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_stinky_precious_decimate_SpellScript();
        }
    }
}
