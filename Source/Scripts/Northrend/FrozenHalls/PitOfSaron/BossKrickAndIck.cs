using System;
using Game.Entities;
using Game.Scripting;
using Game.AI;
using Framework.Constants;
using Game.Maps;
using Game.Spells;

namespace Scripts.Northrend.FrozenHalls.PitOfSaron.BossKrickAndIck
{
    struct SpellIds
    {
        public const uint MightyKick = 69021; //Ick'S Spell
        public const uint ShadowBolt = 69028; //Krick'S Spell
        public const uint ToxicWaste = 69024; //Krick'S Spell
        public const uint ExplosiveBarrageKrick = 69012; //Special Spell 1
        public const uint ExplosiveBarrageIck = 69263; //Special Spell 1
        public const uint PoisonNova = 68989; //Special Spell 2
        public const uint Pursuit = 68987; //Special Spell 3

        public const uint ExplosiveBarrageSummon = 69015;
        public const uint ExplodingOrb = 69017; //Visual On Exploding Orb
        public const uint AutoGrow = 69020; //Grow Effect On Exploding Orb
        public const uint HastyGrow = 44851; //Need To Check Growing Stacks
        public const uint ExplosiveBarrageDamage = 69019; //Damage Done By Orb While Exploding

        public const uint Strangulating = 69413; //Krick'S Selfcast In Intro
        public const uint Suicide = 7;
        public const uint KrickKillCredit = 71308;
        public const uint NecromanticPower = 69753;
    }

    struct TextIds
    {
        // Krick
        public const uint SayKrickAggro = 0;
        public const uint SayKrickSlay = 1;
        public const uint SayKrickBarrage1 = 2;
        public const uint SayKrickBarrage2 = 3;
        public const uint SayKrickPoisonNova = 4;
        public const uint SayKrickChase = 5;
        public const uint SayKrickOutro1 = 6;
        public const uint SayKrickOutro3 = 7;
        public const uint SayKrickOutro5 = 8;
        public const uint SayKrickOutro8 = 9;

        // Ick
        public const uint SayIckPoisonNova = 0;
        public const uint SayIckChase1 = 1;

        // Outro
        public const uint SayJaynaOutro2 = 0;
        public const uint SayJaynaOutro4 = 1;
        public const uint SayJaynaOutro10 = 2;
        public const uint SaySylvanasOutro2 = 0;
        public const uint SaySylvanasOutro4 = 1;
        public const uint SaySylvanasOutro10 = 2;
        public const uint SayTyrannusOutro7 = 1;
        public const uint SayTyrannusOutro9 = 2;
    }

    struct Events
    {
        public const uint MightyKick = 1;
        public const uint ShadowBolt = 2;
        public const uint ToxicWaste = 3;
        public const uint Special = 4; //Special Spell Selection (One Of Event 5; 6 Or 7)
        public const uint Pursuit = 5;
        public const uint PoisonNova = 6;
        public const uint ExplosiveBarrage = 7;

        // Krick Outro
        public const uint Outro1 = 8;
        public const uint Outro2 = 9;
        public const uint Outro3 = 10;
        public const uint Outro4 = 11;
        public const uint Outro5 = 12;
        public const uint Outro6 = 13;
        public const uint Outro7 = 14;
        public const uint Outro8 = 15;
        public const uint Outro9 = 16;
        public const uint Outro10 = 17;
        public const uint Outro11 = 18;
        public const uint Outro12 = 19;
        public const uint Outro13 = 20;
        public const uint OutroEnd = 21;
    }

    enum KrickPhase
    {
        Combat = 1,
        Outro = 2
    }

    struct Misc
    {
        public const int ActionOutro = 1;

        public const uint PointKrickIntro = 364770;
        public const uint PointKrickDeath = 364771;

        public static Position[] outroPos =
        {
            new Position(828.9342f, 118.6247f, 509.5190f, 0.0000000f),  // Krick's Outro Position
            new Position( 841.0100f, 196.2450f, 573.9640f, 0.2046099f),  // Scourgelord Tyrannus Outro Position (Tele to...)
            new Position( 777.2274f, 119.5521f, 510.0363f, 6.0562930f),  // Sylvanas / Jaine Outro Spawn Position (NPC_SYLVANAS_PART1)
            new Position(823.3984f, 114.4907f, 509.4899f, 0.0000000f),  // Sylvanas / Jaine Outro Move Position (1)
            new Position( 835.5887f, 139.4345f, 530.9526f, 0.0000000f),  // Tyrannus fly down Position (not sniffed)
            new Position( 828.9342f, 118.6247f, 514.5190f, 0.0000000f),  // Krick's Choke Position
            new Position(828.9342f, 118.6247f, 509.4958f, 0.0000000f),  // Kirck's Death Position
            new Position(914.4820f, 143.1602f, 633.3624f, 0.0000000f)   // Tyrannus fly up (not sniffed)
        };
    }


    [Script]
    class boss_ick : BossAI
    {
        public boss_ick(Creature creature) : base(creature, DataTypes.Ick)
        {
            _tempThreat = 0;
        }

        public override void Reset()
        {
            _events.Reset();
            instance.SetBossState(DataTypes.Ick, EncounterState.NotStarted);
        }

        Creature GetKrick()
        {
            return ObjectAccessor.GetCreature(me, instance.GetGuidData(DataTypes.Krick));
        }

        public override void EnterCombat(Unit who)
        {
            _EnterCombat();

            Creature krick = GetKrick();
            if (krick)
                    krick.GetAI().Talk(TextIds.SayKrickAggro);

            _events.ScheduleEvent(Events.MightyKick, 20000);
            _events.ScheduleEvent(Events.ToxicWaste, 5000);
            _events.ScheduleEvent(Events.ShadowBolt, 10000);
            _events.ScheduleEvent(Events.Special, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35));
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            me.GetMotionMaster().Clear();
            base.EnterEvadeMode(why);
        }

        public override void JustDied(Unit killer)
        {
            Creature krick = GetKrick();
            if (krick)
            {
                Vehicle vehicle = me.GetVehicleKit();
                if (vehicle)
                    vehicle.RemoveAllPassengers();
                if (krick.IsAIEnabled)
                    krick.GetAI().DoAction(Misc.ActionOutro);
            }

            instance.SetBossState(DataTypes.Ick, EncounterState.Done);
        }

        public void SetTempThreat(float threat)
        {
            _tempThreat = threat;
        }

        public void _ResetThreat(Unit target)
        {
            ModifyThreatByPercent(target, -100);
            AddThreat(target, _tempThreat);
        }

        public override void UpdateAI(uint diff)
        {
            if (!me.IsInCombat())
                return;

            if (!me.GetVictim() && me.GetThreatManager().IsThreatListEmpty())
            {
                EnterEvadeMode(EvadeReason.NoHostiles);
                return;
            }

            _events.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case Events.ToxicWaste:
                        {
                            Creature krick = GetKrick();
                            if (krick)
                            {
                                Unit target = SelectTarget(SelectAggroTarget.Random, 0);
                                if (target)
                                    krick.CastSpell(target, SpellIds.ToxicWaste);
                                _events.ScheduleEvent(Events.ToxicWaste, TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(10));
                            }
                        }
                        break;
                    case Events.ShadowBolt:
                        {
                            Creature krick = GetKrick();
                            if (krick)
                            {
                                Unit target = SelectTarget(SelectAggroTarget.Random, 1);
                                if (target)
                                    krick.CastSpell(target, SpellIds.ShadowBolt);
                                _events.ScheduleEvent(Events.ShadowBolt, 15000);
                            }
                        }
                        return;
                    case Events.MightyKick:
                        DoCastVictim(SpellIds.MightyKick);
                        _events.ScheduleEvent(Events.MightyKick, 25000);
                        return;
                    case Events.Special:
                        //select one of these three special _events
                        _events.ScheduleEvent(RandomHelper.RAND(Events.ExplosiveBarrage, Events.PoisonNova, Events.Pursuit), 1000);
                        _events.ScheduleEvent(Events.Special, TimeSpan.FromSeconds(23), TimeSpan.FromSeconds(28));
                        break;
                    case Events.ExplosiveBarrage:
                        {
                            Creature krick = GetKrick();
                            if (krick)
                            {
                                krick.GetAI().Talk(TextIds.SayKrickBarrage1);
                                krick.GetAI().Talk(TextIds.SayKrickBarrage2);
                                krick.CastSpell(krick, SpellIds.ExplosiveBarrageKrick, true);
                                DoCast(me, SpellIds.ExplosiveBarrageIck);
                            }
                            _events.DelayEvents(20000);
                        }
                        break;
                    case Events.PoisonNova:
                        {
                            Creature krick = GetKrick();
                            if (krick)
                                krick.GetAI().Talk(TextIds.SayKrickPoisonNova);

                            Talk(TextIds.SayIckPoisonNova);
                            DoCast(me, SpellIds.PoisonNova);
                        }
                        break;
                    case Events.Pursuit:
                        {
                            Creature krick = GetKrick();
                            if (krick)
                                krick.GetAI().Talk(TextIds.SayKrickChase);
                            DoCast(me, SpellIds.Pursuit);
                        }
                        break;
                    default:
                        break;
                }

                if (me.HasUnitState(UnitState.Casting))
                    return;
            });

            DoMeleeAttackIfReady();
        }

        float _tempThreat;
    }

    [Script]
    class boss_krick : ScriptedAI
    {
        public boss_krick(Creature creature) : base(creature)
        {
            _instanceScript = creature.GetInstanceScript();
            _summons = new SummonList(creature);
            Initialize();
        }

        void Initialize()
        {
            _phase = KrickPhase.Combat;
            _outroNpcGUID.Clear();
            _tyrannusGUID.Clear();
        }

        public override void Reset()
        {
            _events.Reset();
            Initialize();

            me.SetReactState(ReactStates.Passive);
            me.AddUnitFlag(UnitFlags.NonAttackable);
        }

        Creature GetIck()
        {
            return ObjectAccessor.GetCreature(me, _instanceScript.GetGuidData(DataTypes.Ick));
        }

        public override void KilledUnit(Unit victim)
        {
            if (victim.IsPlayer())
                Talk(TextIds.SayKrickSlay);
        }

        public override void JustSummoned(Creature summon)
        {
            _summons.Summon(summon);
            if (summon.GetEntry() == CreatureIds.ExplodingOrb)
            {
                summon.CastSpell(summon, SpellIds.ExplodingOrb, true);
                summon.CastSpell(summon, SpellIds.AutoGrow, true);
            }
        }

        public override void DoAction(int actionId)
        {
            if (actionId == Misc.ActionOutro)
            {
                Creature tyrannusPtr = ObjectAccessor.GetCreature(me, _instanceScript.GetGuidData(DataTypes.TyrannusEvent));
                if (tyrannusPtr)
                    tyrannusPtr.NearTeleportTo(Misc.outroPos[1].GetPositionX(), Misc.outroPos[1].GetPositionY(), Misc.outroPos[1].GetPositionZ(), Misc.outroPos[1].GetOrientation());
                else
                    tyrannusPtr = me.SummonCreature(CreatureIds.TyrannusEvents, Misc.outroPos[1], TempSummonType.ManualDespawn);

                tyrannusPtr.SetCanFly(true);
                me.GetMotionMaster().MovePoint(Misc.PointKrickIntro, Misc.outroPos[0].GetPositionX(), Misc.outroPos[0].GetPositionY(), Misc.outroPos[0].GetPositionZ());
                tyrannusPtr.SetFacingToObject(me);
            }
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (type != MovementGeneratorType.Point || id != Misc.PointKrickIntro)
                return;

            Talk(TextIds.SayKrickOutro1);
            _phase = KrickPhase.Outro;
            _events.Reset();
            _events.ScheduleEvent(Events.Outro1, 1000);
        }

        public override void UpdateAI(uint diff)
        {
            if (_phase != KrickPhase.Outro)
                return;

            _events.Update(diff);

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case Events.Outro1:
                        {
                            Creature temp = ObjectAccessor.GetCreature(me, _instanceScript.GetGuidData(DataTypes.JainaSylvanas1));
                            if (temp)
                                temp.DespawnOrUnsummon();

                            Creature jainaOrSylvanas = null;
                            if (_instanceScript.GetData(DataTypes.TeamInInstance) == (uint)Team.Alliance)
                                jainaOrSylvanas = me.SummonCreature(CreatureIds.JainaPart1, Misc.outroPos[2], TempSummonType.ManualDespawn);
                            else
                                jainaOrSylvanas = me.SummonCreature(CreatureIds.SylvanasPart1, Misc.outroPos[2], TempSummonType.ManualDespawn);

                            if (jainaOrSylvanas)
                            {
                                jainaOrSylvanas.GetMotionMaster().MovePoint(0, Misc.outroPos[3]);
                                _outroNpcGUID = jainaOrSylvanas.GetGUID();
                            }
                            _events.ScheduleEvent(Events.Outro2, 6000);
                            break;
                        }
                    case Events.Outro2:
                        {
                            Creature jainaOrSylvanas = ObjectAccessor.GetCreature(me, _outroNpcGUID);
                            if (jainaOrSylvanas)
                            {
                                jainaOrSylvanas.SetFacingToObject(me);
                                me.SetFacingToObject(jainaOrSylvanas);
                                if (_instanceScript.GetData(DataTypes.TeamInInstance) == (uint)Team.Alliance)
                                    jainaOrSylvanas.GetAI().Talk(TextIds.SayJaynaOutro2);
                                else
                                    jainaOrSylvanas.GetAI().Talk(TextIds.SaySylvanasOutro2);
                            }
                            _events.ScheduleEvent(Events.Outro3, 5000);
                        }
                        break;
                    case Events.Outro3:
                        Talk(TextIds.SayKrickOutro3);
                        _events.ScheduleEvent(Events.Outro4, 18000);
                        break;
                    case Events.Outro4:
                        {
                            Creature jainaOrSylvanas = ObjectAccessor.GetCreature(me, _outroNpcGUID);
                            if (jainaOrSylvanas)
                            {
                                if (_instanceScript.GetData(DataTypes.TeamInInstance) == (uint)Team.Alliance)
                                    jainaOrSylvanas.GetAI().Talk(TextIds.SayJaynaOutro4);
                                else
                                    jainaOrSylvanas.GetAI().Talk(TextIds.SaySylvanasOutro4);
                            }
                            _events.ScheduleEvent(Events.Outro5, 5000);
                        }
                        break;
                    case Events.Outro5:
                        Talk(TextIds.SayKrickOutro5);
                        _events.ScheduleEvent(Events.Outro6, 1000);
                        break;
                    case Events.Outro6:
                        {
                            Creature tyrannus = ObjectAccessor.GetCreature(me, _instanceScript.GetGuidData(DataTypes.TyrannusEvent));
                            if (tyrannus)
                            {
                                tyrannus.SetSpeedRate(UnitMoveType.Flight, 3.5f);
                                tyrannus.GetMotionMaster().MovePoint(1, Misc.outroPos[4]);
                                _tyrannusGUID = tyrannus.GetGUID();
                            }
                            _events.ScheduleEvent(Events.Outro7, 5000);
                        }
                        break;
                    case Events.Outro7:
                        {
                            Creature tyrannus = ObjectAccessor.GetCreature(me, _tyrannusGUID);
                            if (tyrannus)
                                tyrannus.GetAI().Talk(TextIds.SayTyrannusOutro7);
                            _events.ScheduleEvent(Events.Outro8, 5000);
                        }
                        break;
                    case Events.Outro8:
                        //! HACK: Creature's can't have MOVEMENTFLAG_FLYING
                        me.AddUnitMovementFlag(MovementFlag.Flying);
                        me.GetMotionMaster().MovePoint(0, Misc.outroPos[5]);
                        DoCast(me, SpellIds.Strangulating);
                        _events.ScheduleEvent(Events.Outro9, 2000);
                        break;
                    case Events.Outro9:
                        {
                            Talk(TextIds.SayKrickOutro8);
                            // @todo Tyrannus starts killing Krick.
                            // there shall be some visual spell effect
                            Creature tyrannus = ObjectAccessor.GetCreature(me, _tyrannusGUID);
                            if (tyrannus)
                                tyrannus.CastSpell(me, SpellIds.NecromanticPower, true);  //not sure if it's the right spell :/
                            _events.ScheduleEvent(Events.Outro10, 1000);
                        }
                        break;
                    case Events.Outro10:
                        //! HACK: Creature's can't have MOVEMENTFLAG_FLYING
                        me.RemoveUnitMovementFlag(MovementFlag.Flying);
                        me.AddUnitMovementFlag(MovementFlag.FallingFar);
                        me.GetMotionMaster().MovePoint(0, Misc.outroPos[6]);
                        _events.ScheduleEvent(Events.Outro11, 2000);
                        break;
                    case Events.Outro11:
                        DoCast(me, SpellIds.KrickKillCredit); // don't really know if we need it
                        me.SetStandState(UnitStandStateType.Dead);
                        me.SetHealth(0);
                        _events.ScheduleEvent(Events.Outro12, 3000);
                        break;
                    case Events.Outro12:
                        {
                            Creature tyrannus = ObjectAccessor.GetCreature(me, _tyrannusGUID);
                            if (tyrannus)
                                tyrannus.GetAI().Talk(TextIds.SayTyrannusOutro9);
                            _events.ScheduleEvent(Events.Outro13, 2000);
                        }
                        break;
                    case Events.Outro13:
                        {
                            Creature jainaOrSylvanas = ObjectAccessor.GetCreature(me, _outroNpcGUID);
                            if (jainaOrSylvanas)
                            {
                                if (_instanceScript.GetData(DataTypes.TeamInInstance) == (uint)Team.Alliance)
                                    jainaOrSylvanas.GetAI().Talk(TextIds.SayJaynaOutro10);
                                else
                                    jainaOrSylvanas.GetAI().Talk(TextIds.SaySylvanasOutro10);
                            }
                            // End of OUTRO. for now...
                            _events.ScheduleEvent(Events.OutroEnd, 3000);

                            Creature tyrannus = ObjectAccessor.GetCreature(me, _tyrannusGUID);
                            if (tyrannus)
                                tyrannus.GetMotionMaster().MovePoint(0, Misc.outroPos[7]);
                        }
                        break;
                    case Events.OutroEnd:
                        {
                            Creature tyrannus = ObjectAccessor.GetCreature(me, _tyrannusGUID);
                            if (tyrannus)
                                tyrannus.DespawnOrUnsummon();

                            me.DisappearAndDie();
                        }
                        break;
                    default:
                        break;
                }
            });
        }

        InstanceScript _instanceScript;
        SummonList _summons;

        KrickPhase _phase;
        ObjectGuid _outroNpcGUID;
        ObjectGuid _tyrannusGUID;
    }

    [Script]
    class spell_krick_explosive_barrage : AuraScript
    {
        void HandlePeriodicTick(AuraEffect aurEff)
        {
            PreventDefaultAction();
            Unit caster = GetCaster();
            if (caster)
            {
                if (caster.IsCreature())
                {
                    var players = caster.GetMap().GetPlayers();
                    foreach (var player in players)
                    {
                        if (player)
                            if (player.IsWithinDist(caster, 60.0f))    // don't know correct range
                                caster.CastSpell(player, SpellIds.ExplosiveBarrageSummon, true);
                    }
                }
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandlePeriodicTick, 0, AuraType.PeriodicTriggerSpell));
        }
    }

    [Script]
    class spell_ick_explosive_barrage : AuraScript
    {
        void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster)
                if (caster.IsCreature())
                    caster.GetMotionMaster().MoveIdle();
        }

        void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster)
            {
                if (caster.IsCreature())
                {
                    caster.GetMotionMaster().Clear();
                    caster.GetMotionMaster().MoveChase(caster.GetVictim());
                }
            }
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new EffectApplyHandler(HandleEffectRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_exploding_orb_hasty_grow : AuraScript
    {
        void OnStackChange(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetStackAmount() == 15)
            {
                Unit target = GetTarget(); // store target because aura gets removed
                target.CastSpell(target, SpellIds.ExplosiveBarrageDamage, false);
                target.RemoveAurasDueToSpell(SpellIds.HastyGrow);
                target.RemoveAurasDueToSpell(SpellIds.AutoGrow);
                target.RemoveAurasDueToSpell(SpellIds.ExplodingOrb);

                Creature creature = target.ToCreature();
                if (creature)
                    creature.DespawnOrUnsummon();
            }
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(OnStackChange, 0, AuraType.ModScale, AuraEffectHandleModes.Reapply));
        }
    }

    [Script]
    class spell_krick_pursuit : SpellScript
    {
        void HandleScriptEffect(uint effIndex)
        {
            if (GetCaster())
            {
                Creature ick = GetCaster().ToCreature();
                if (ick)
                {
                    Unit target = ick.GetAI().SelectTarget(SelectAggroTarget.Random, 0, 200.0f, true);
                    if (target)
                    {
                        ick.GetAI().Talk(TextIds.SayIckChase1, target);
                        ick.AddAura(GetSpellInfo().Id, target);
                        ick.GetAI<boss_ick>().SetTempThreat(ick.GetThreatManager().GetThreat(target));
                        ick.GetThreatManager().AddThreat(target, GetEffectValue(), GetSpellInfo(), true, true);
                        target.GetThreatManager().AddThreat(ick, GetEffectValue(), GetSpellInfo(), true, true);
                    }
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 1, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_krick_pursuit_AuraScript : AuraScript
    {
        void HandleExtraEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster)
            {
                Creature creCaster = caster.ToCreature();
                if (creCaster)
                    creCaster.GetAI<boss_ick>()._ResetThreat(GetTarget());
            }
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(HandleExtraEffect, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_krick_pursuit_confusion : AuraScript
    {
        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().ApplySpellImmune(0, SpellImmunity.State, AuraType.ModTaunt, true);
            GetTarget().ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.AttackMe, true);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().ApplySpellImmune(0, SpellImmunity.State, AuraType.ModTaunt, false);
            GetTarget().ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.AttackMe, false);
        }

        public override void Register()
        {
            OnEffectApply.Add(new EffectApplyHandler(OnApply, 2, AuraType.Linked, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new EffectApplyHandler(OnRemove, 2, AuraType.Linked, AuraEffectHandleModes.Real));
        }
    }
}
