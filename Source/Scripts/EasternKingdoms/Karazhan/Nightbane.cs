// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;

namespace Scripts.EasternKingdoms.Karazhan.Nightbane
{
    struct SpellIds
    {
        public const uint BellowingRoar = 36922;
        public const uint CharredEarth = 30129;
        public const uint Cleave = 30131;
        public const uint DistractingAsh = 30130;
        public const uint RainOfBones = 37098;
        public const uint SmokingBlast = 30128;
        public const uint SmokingBlastT = 37057;
        public const uint SmolderingBreath = 30210;
        public const uint SummonSkeleton = 30170;
        public const uint TailSweep = 25653;
    }

    struct TextIds
    {
        public const uint EmoteSummon = 0;
        public const uint YellAggro = 1;
        public const uint YellFlyPhase = 2;
        public const uint YellLandPhase = 3;
        public const uint EmoteBreath = 4;
    }

    struct PointIds
    {
        public const uint IntroStart = 0;
        public const uint IntroEnd = 1;
        public const uint IntroLanding = 2;
        public const uint PhaseTwoFly = 3;
        public const uint PhaseTwoPreFly = 4;
        public const uint PhaseTwoLanding = 5;
        public const uint PhaseTwoEnd = 6;
    }

    struct SplineChainIds
    {
        public const uint IntroStart = 1;
        public const uint IntroEnd = 2;
        public const uint IntroLanding = 3;
        public const uint SecondLanding = 4;
        public const uint PhaseTwo = 5;
    }

    enum NightbanePhases
    {
        Intro = 0,
        Ground,
        Fly
    }

    struct MiscConst
    {
        public const int ActionSummon = 0;
        public const uint PathPhaseTwo = 13547500;

        public const uint GroupGround = 1;
        public const uint GroupFly = 2;

        public static Position FlyPosition = new Position(-11160.13f, -1870.683f, 97.73876f, 0.0f);
        public static Position FlyPositionLeft = new Position(-11094.42f, -1866.992f, 107.8375f, 0.0f);
        public static Position FlyPositionRight = new Position(-11193.77f, -1921.983f, 107.9845f, 0.0f);
    }

    [Script]
    class boss_nightbane : BossAI
    {
        byte _flyCount;
        NightbanePhases phase;

        public boss_nightbane(Creature creature) : base(creature, DataTypes.Nightbane) { }

        public override void Reset()
        {
            _Reset();
            _flyCount = 0;
            me.SetDisableGravity(true);
            HandleTerraceDoors(true);
            GameObject urn = ObjectAccessor.GetGameObject(me, instance.GetGuidData(DataTypes.GoBlackenedUrn));
            if (urn != null)
                urn.RemoveFlag(GameObjectFlags.InUse);
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            me.SetDisableGravity(true);
            base.EnterEvadeMode(why);
        }

        public override void JustReachedHome()
        {
            _DespawnAtEvade();
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
            HandleTerraceDoors(true);
        }

        public override void DoAction(int action)
        {
            if (action == MiscConst.ActionSummon)
            {
                Talk(TextIds.EmoteSummon);
                phase = NightbanePhases.Intro;
                me.SetActive(true);
                me.SetFarVisible(true);
                me.SetUninteractible(false);
                me.GetMotionMaster().MoveAlongSplineChain(PointIds.IntroStart, SplineChainIds.IntroStart, false);
                HandleTerraceDoors(false);
            }
        }

        void SetupGroundPhase()
        {
            phase = NightbanePhases.Ground;
            _scheduler.Schedule(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(15), MiscConst.GroupGround, task =>
            {
                DoCastVictim(SpellIds.Cleave);
                task.Repeat(TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(23), MiscConst.GroupGround, task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);
                if (target != null)
                    if (!me.HasInArc(MathF.PI, target))
                        DoCast(target, SpellIds.TailSweep);
                task.Repeat(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(30));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(48), MiscConst.GroupGround, task =>
            {
                DoCastAOE(SpellIds.BellowingRoar);
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(18), MiscConst.GroupGround, task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);
                if (target != null)
                    DoCast(target, SpellIds.CharredEarth);
                task.Repeat(TimeSpan.FromSeconds(18), TimeSpan.FromSeconds(21));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(26), TimeSpan.FromSeconds(30), MiscConst.GroupGround, task =>
            {
                DoCastVictim(SpellIds.SmolderingBreath);
                task.Repeat(TimeSpan.FromSeconds(28), TimeSpan.FromSeconds(40));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(82), MiscConst.GroupGround, task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);
                if (target != null)
                    DoCast(target, SpellIds.DistractingAsh);
            });
        }

        void HandleTerraceDoors(bool open)
        {
            instance.HandleGameObject(instance.GetGuidData(DataTypes.MastersTerraceDoor1), open);
            instance.HandleGameObject(instance.GetGuidData(DataTypes.MastersTerraceDoor2), open);
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);
            Talk(TextIds.YellAggro);
            SetupGroundPhase();
        }

        public override void DamageTaken(Unit attacker, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            if (phase == NightbanePhases.Fly)
            {
                if (damage >= me.GetHealth())
                    damage = (uint)(me.GetHealth() - 1);
                return;
            }

            if ((_flyCount == 0 && HealthBelowPct(75)) || (_flyCount == 1 && HealthBelowPct(50)) || (_flyCount == 2 && HealthBelowPct(25)))
            {
                phase = NightbanePhases.Fly;
                StartPhaseFly();
            }
        }

        public override void MovementInform(MovementGeneratorType type, uint pointId)
        {
            if (type == MovementGeneratorType.SplineChain)
            {
                switch (pointId)
                {
                    case PointIds.IntroStart:
                        me.SetStandState(UnitStandStateType.Stand);
                        _scheduler.Schedule(TimeSpan.FromMilliseconds(1), task =>
                        {
                            me.GetMotionMaster().MoveAlongSplineChain(PointIds.IntroEnd, SplineChainIds.IntroEnd, false);
                        });
                        break;
                    case PointIds.IntroEnd:
                        _scheduler.Schedule(TimeSpan.FromSeconds(2), task =>
                        {
                            me.GetMotionMaster().MoveAlongSplineChain(PointIds.IntroLanding, SplineChainIds.IntroLanding, false);
                        });
                        break;
                    case PointIds.IntroLanding:
                        me.SetDisableGravity(false);
                        me.HandleEmoteCommand(Emote.OneshotLand);
                        _scheduler.Schedule(TimeSpan.FromSeconds(3), task =>
                        {
                            me.SetImmuneToPC(false);
                            DoZoneInCombat();
                        });
                        break;
                    case PointIds.PhaseTwoLanding:
                        phase = NightbanePhases.Ground;
                        me.SetDisableGravity(false);
                        me.HandleEmoteCommand(Emote.OneshotLand);
                        _scheduler.Schedule(TimeSpan.FromSeconds(3), task =>
                        {
                            SetupGroundPhase();
                            me.SetReactState(ReactStates.Aggressive);
                        });
                        break;
                    case PointIds.PhaseTwoEnd:
                        _scheduler.Schedule(TimeSpan.FromMilliseconds(1), task =>
                        {
                            me.GetMotionMaster().MoveAlongSplineChain(PointIds.PhaseTwoLanding, SplineChainIds.SecondLanding, false);
                        });
                        break;
                    default:
                        break;
                }
            }
            else if (type == MovementGeneratorType.Point)
            {
                if (pointId == PointIds.PhaseTwoFly)
                {
                    _scheduler.Schedule(TimeSpan.FromSeconds(33), MiscConst.GroupFly, task =>
                    {
                        _scheduler.CancelGroup(MiscConst.GroupFly);
                        _scheduler.Schedule(TimeSpan.FromSeconds(2), MiscConst.GroupGround, landTask =>
                        {
                            Talk(TextIds.YellLandPhase);
                            me.SetDisableGravity(true);
                            me.GetMotionMaster().MoveAlongSplineChain(PointIds.PhaseTwoEnd, SplineChainIds.PhaseTwo, false);
                        });
                    });
                    _scheduler.Schedule(TimeSpan.FromSeconds(2), MiscConst.GroupFly, task =>
                    {
                        Talk(TextIds.EmoteBreath);
                        task.Schedule(TimeSpan.FromSeconds(3), MiscConst.GroupFly, somethingTask =>
                        {
                            ResetThreatList();
                            Unit target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);
                            if (target != null)
                            {
                                me.SetFacingToObject(target);
                                DoCast(target, SpellIds.RainOfBones);
                            }
                        });
                    });
                    _scheduler.Schedule(TimeSpan.FromSeconds(21), MiscConst.GroupFly, task =>
                    {
                        Unit target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);
                        if (target != null)
                            DoCast(target, SpellIds.SmokingBlastT);
                        task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(7));
                    });
                    _scheduler.Schedule(TimeSpan.FromSeconds(17), MiscConst.GroupFly, task =>
                    {
                        Unit target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);
                        if (target != null)
                            DoCast(target, SpellIds.SmokingBlast);
                        task.Repeat(TimeSpan.FromMilliseconds(1400));
                    });
                }
                else if (pointId == PointIds.PhaseTwoPreFly)
                {
                    _scheduler.Schedule(TimeSpan.FromMilliseconds(1), task =>
                    {
                        me.GetMotionMaster().MovePoint(PointIds.PhaseTwoFly, MiscConst.FlyPosition, true);
                    });
                }
            }
        }

        void StartPhaseFly()
        {
            ++_flyCount;
            Talk(TextIds.YellFlyPhase);
            _scheduler.CancelGroup(MiscConst.GroupGround);
            me.InterruptNonMeleeSpells(false);
            me.HandleEmoteCommand(Emote.OneshotLiftoff);
            me.SetDisableGravity(true);
            me.SetReactState(ReactStates.Passive);
            me.AttackStop();

            if (me.GetDistance(MiscConst.FlyPositionLeft) < me.GetDistance(MiscConst.FlyPosition))
                me.GetMotionMaster().MovePoint(PointIds.PhaseTwoPreFly, MiscConst.FlyPositionLeft, true);
            else if (me.GetDistance(MiscConst.FlyPositionRight) < me.GetDistance(MiscConst.FlyPosition))
                me.GetMotionMaster().MovePoint(PointIds.PhaseTwoPreFly, MiscConst.FlyPositionRight, true);
            else
                me.GetMotionMaster().MovePoint(PointIds.PhaseTwoFly, MiscConst.FlyPosition, true);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim() && phase != NightbanePhases.Intro)
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }

    [Script] // 37098 - Rain of Bones
    class spell_rain_of_bones_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SummonSkeleton);
        }

        void OnTrigger(AuraEffect aurEff)
        {
            if (aurEff.GetTickNumber() % 5 == 0)
                GetTarget().CastSpell(GetTarget(), SpellIds.SummonSkeleton, true);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(OnTrigger, 1, AuraType.PeriodicTriggerSpell));
        }
    }

    [Script]
    class go_blackened_urn : GameObjectAI
    {
        InstanceScript instance;

        public go_blackened_urn(GameObject go) : base(go)
        {
            instance = go.GetInstanceScript();
        }

        public override bool OnGossipHello(Player player)
        {
            if (me.HasFlag(GameObjectFlags.InUse))
                return false;

            if (instance.GetBossState(DataTypes.Nightbane) == EncounterState.Done || instance.GetBossState(DataTypes.Nightbane) == EncounterState.InProgress)
                return false;

            Creature nightbane = ObjectAccessor.GetCreature(me, instance.GetGuidData(DataTypes.Nightbane));
            if (nightbane != null)
            {
                me.SetFlag(GameObjectFlags.InUse);
                nightbane.GetAI().DoAction(MiscConst.ActionSummon);
            }
            return false;
        }
    }
}