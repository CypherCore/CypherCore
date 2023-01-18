// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;

namespace Scripts.EasternKingdoms.MagistersTerrace.FelbloodKaelthas
{
    struct TextIds
    {
        // Kael'thas Sunstrider
        public const uint SayIntro1 = 0;
        public const uint SayIntro2 = 1;
        public const uint SayGravityLapse1 = 2;
        public const uint SayGravityLapse2 = 3;
        public const uint SayPowerFeedback = 4;
        public const uint SaySummonPhoenix = 5;
        public const uint SayAnnouncePyroblast = 6;
        public const uint SayFlameStrike = 7;
        public const uint SayDeath = 8;
    }

    struct SpellIds
    {
        // Kael'thas Sunstrider
        public const uint Fireball = 44189;
        public const uint GravityLapse = 49887;
        public const uint HGravityLapse = 44226;
        public const uint GravityLapseCenterTeleport = 44218;
        public const uint GravityLapseLeftTeleport = 44219;
        public const uint GravityLapseFrontLeftTeleport = 44220;
        public const uint GravityLapseFrontTeleport = 44221;
        public const uint GravityLapseFrontRightTeleport = 44222;
        public const uint GravityLapseRightTeleport = 44223;
        public const uint GravityLapseInitial = 44224;
        public const uint GravityLapseFly = 44227;
        public const uint GravityLapseBeamVisualPeriodic = 44251;
        public const uint SummonArcaneSphere = 44265;
        public const uint FlameStrike = 46162;
        public const uint ShockBarrier = 46165;
        public const uint PowerFeedback = 44233;
        public const uint HPowerFeedback = 47109;
        public const uint Pyroblast = 36819;
        public const uint Phoenix = 44194;
        public const uint EmoteTalkExclamation = 48348;
        public const uint EmotePoint = 48349;
        public const uint EmoteRoar = 48350;
        public const uint ClearFlight = 44232;
        public const uint QuiteSuicide = 3617; // Serverside public const uint 

        // Flame Strike
        public const uint FlameStrikeDummy = 44191;
        public const uint FlameStrikeDamage = 44190;

        // Phoenix
        public const uint Rebirth = 44196;
        public const uint Burn = 44197;
        public const uint EmberBlast = 44199;
        public const uint SummonPhoenixEgg = 44195; // Serverside public const uint 
        public const uint FullHeal = 17683;
    }

    enum Phase
    {
        Intro = 0,
        One = 1,
        Two = 2,
        Outro = 3
    }

    struct MiscConst
    {
        public static uint[] GravityLapseTeleportSpells =
        {
            SpellIds.GravityLapseLeftTeleport,
            SpellIds.GravityLapseFrontLeftTeleport,
            SpellIds.GravityLapseFrontTeleport,
            SpellIds.GravityLapseFrontRightTeleport,
            SpellIds.GravityLapseRightTeleport
        };
    }

    [Script]
    class boss_felblood_kaelthas : BossAI
    {
        byte _gravityLapseTargetCount;
        bool _firstGravityLapse;

        Phase _phase;

        static uint groupFireBall = 1;

        public boss_felblood_kaelthas(Creature creature) : base(creature, DataTypes.KaelthasSunstrider)
        {
            Initialize();
        }

        void Initialize()
        {
            _gravityLapseTargetCount = 0;
            _firstGravityLapse = true;
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);
            _phase = Phase.One;

            _scheduler.Schedule(TimeSpan.FromMilliseconds(1), groupFireBall, task =>
            {
                DoCastVictim(SpellIds.Fireball);
                task.Repeat(TimeSpan.FromSeconds(2.5));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(44), task =>
            {
                Talk(TextIds.SayFlameStrike);
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 40.0f, true);
                if (target)
                    DoCast(target, SpellIds.FlameStrike);
                task.Repeat();
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(12), task =>
            {
                Talk(TextIds.SaySummonPhoenix);
                DoCastSelf(SpellIds.Phoenix);
                task.Repeat(TimeSpan.FromSeconds(45));
            });

            if (IsHeroic())
            {
                _scheduler.Schedule(TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(1), task =>
                {
                    Talk(TextIds.SayAnnouncePyroblast);
                    DoCastSelf(SpellIds.ShockBarrier);
                    task.RescheduleGroup(groupFireBall, TimeSpan.FromSeconds(2.5));
                    task.Schedule(TimeSpan.FromSeconds(2), pyroBlastTask =>
                    {
                        Unit target = SelectTarget(SelectTargetMethod.Random, 0, 40.0f, true);
                        if (target != null)
                            DoCast(target, SpellIds.Pyroblast);
                    });
                    task.Repeat(TimeSpan.FromMinutes(1));
                });
            }
        }

        public override void Reset()
        {
            _Reset();
            Initialize();
            _phase = Phase.Intro;
        }

        public override void JustDied(Unit killer)
        {
            // No _JustDied() here because otherwise we would reset the events which will trigger the death sequence twice.
            instance.SetBossState(DataTypes.KaelthasSunstrider, EncounterState.Done);
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            DoCastAOE(SpellIds.ClearFlight, new CastSpellExtraArgs(true));
            _EnterEvadeMode();
            summons.DespawnAll();
            _DespawnAtEvade();
        }

        public override void DamageTaken(Unit attacker, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            // Checking for lethal damage first so we trigger the outro phase without triggering phase two in case of oneshot attacks
            if (damage >= me.GetHealth() && _phase != Phase.Outro)
            {
                me.AttackStop();
                me.SetReactState(ReactStates.Passive);
                me.InterruptNonMeleeSpells(true);
                me.RemoveAurasDueToSpell(DungeonMode(SpellIds.PowerFeedback, SpellIds.HPowerFeedback));
                summons.DespawnAll();
                DoCastAOE(SpellIds.ClearFlight);
                Talk(TextIds.SayDeath);

                _phase = Phase.Outro;
                _scheduler.CancelAll();

                _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
                {
                    DoCastSelf(SpellIds.EmoteTalkExclamation);
                });
                _scheduler.Schedule(TimeSpan.FromSeconds(3.8), task =>
                {
                    DoCastSelf(SpellIds.EmotePoint);
                });
                _scheduler.Schedule(TimeSpan.FromSeconds(7.4), task =>
                {
                    DoCastSelf(SpellIds.EmoteRoar);
                });
                _scheduler.Schedule(TimeSpan.FromSeconds(10), task =>
                {
                    DoCastSelf(SpellIds.EmoteRoar);
                });
                _scheduler.Schedule(TimeSpan.FromSeconds(11), task =>
                {
                    DoCastSelf(SpellIds.QuiteSuicide);
                });
            }

            // Phase two checks. Skip phase two if we are in the outro already
            if (me.HealthBelowPctDamaged(50, damage) && _phase != Phase.Two && _phase != Phase.Outro)
            {
                _phase = Phase.Two;
                _scheduler.CancelAll();
                _scheduler.Schedule(TimeSpan.FromMilliseconds(1), task =>
                {
                    Talk(_firstGravityLapse ? TextIds.SayGravityLapse1 : TextIds.SayGravityLapse2);
                    _firstGravityLapse = false;
                    me.SetReactState(ReactStates.Passive);
                    me.AttackStop();
                    me.GetMotionMaster().Clear();
                    task.Schedule(TimeSpan.FromSeconds(1), _ =>
                    {
                        DoCastSelf(SpellIds.GravityLapseCenterTeleport);
                        task.Schedule(TimeSpan.FromSeconds(1), _ =>
                        {
                            _gravityLapseTargetCount = 0;
                            DoCastAOE(SpellIds.GravityLapseInitial);
                            _scheduler.Schedule(TimeSpan.FromSeconds(4), _ =>
                            {
                                for (byte i = 0; i < 3; i++)
                                    DoCastSelf(SpellIds.SummonArcaneSphere, new CastSpellExtraArgs(true));
                            });
                            _scheduler.Schedule(TimeSpan.FromSeconds(5), _ =>
                            {
                                DoCastAOE(SpellIds.GravityLapseBeamVisualPeriodic);
                            });
                            _scheduler.Schedule(TimeSpan.FromSeconds(35), _ =>
                            {
                                Talk(TextIds.SayPowerFeedback);
                                DoCastAOE(SpellIds.ClearFlight);
                                DoCastSelf(DungeonMode(SpellIds.PowerFeedback, SpellIds.HPowerFeedback));
                                summons.DespawnEntry(CreatureIds.ArcaneSphere);
                                task.Repeat(TimeSpan.FromSeconds(11));
                            });
                        });
                    });
                });
            }

            // Kael'thas may only kill himself via Quite Suicide
            if (damage >= me.GetHealth() && attacker != me)
                damage = (uint)(me.GetHealth() - 1);
        }

        public override void SetData(uint type, uint data)
        {
            if (type == DataTypes.KaelthasIntro)
            {
                // skip the intro if Kael'thas is engaged already
                if (_phase != Phase.Intro)
                    return;

                me.SetImmuneToPC(true);
                _scheduler.Schedule(TimeSpan.FromSeconds(6), task =>
                {
                    Talk(TextIds.SayIntro1);
                    me.SetEmoteState(Emote.StateTalk);
                    _scheduler.Schedule(TimeSpan.FromSeconds(20.6), _ =>
                    {
                        Talk(TextIds.SayIntro2);
                        _scheduler.Schedule(TimeSpan.FromSeconds(15) + TimeSpan.FromMilliseconds(500), _ =>
                        {
                            me.SetEmoteState(Emote.OneshotNone);
                            me.SetImmuneToPC(false);
                        });
                    });
                    _scheduler.Schedule(TimeSpan.FromSeconds(15.6), _ => me.HandleEmoteCommand(Emote.OneshotLaughNoSheathe));
                });
            }
        }

        public override void SpellHitTarget(WorldObject target, SpellInfo spellInfo)
        {
            Unit unitTarget = target.ToUnit();
            if (!unitTarget)
                return;

            switch (spellInfo.Id)
            {
                case SpellIds.GravityLapseInitial:
                {
                    DoCast(unitTarget, MiscConst.GravityLapseTeleportSpells[_gravityLapseTargetCount], new CastSpellExtraArgs(true));
                    target.m_Events.AddEventAtOffset(() =>
                    {
                        target.CastSpell(target, DungeonMode(SpellIds.GravityLapse, SpellIds.HGravityLapse));
                        target.CastSpell(target, SpellIds.GravityLapseFly);

                    }, TimeSpan.FromMilliseconds(400));
                    _gravityLapseTargetCount++;
                    break;
                }
                case SpellIds.ClearFlight:
                    unitTarget.RemoveAurasDueToSpell(SpellIds.GravityLapseFly);
                    unitTarget.RemoveAurasDueToSpell(DungeonMode(SpellIds.GravityLapse, SpellIds.HGravityLapse));
                    break;
                default:
                    break;
            }
        }

        public override void JustSummoned(Creature summon)
        {
            summons.Summon(summon);

            switch (summon.GetEntry())
            {
                case CreatureIds.ArcaneSphere:
                    Unit target = SelectTarget(SelectTargetMethod.Random, 0, 70.0f, true);
                    if (target)
                        summon.GetMotionMaster().MoveFollow(target, 0.0f, 0.0f);
                    break;
                case CreatureIds.FlameStrike:
                    summon.CastSpell(summon, SpellIds.FlameStrikeDummy);
                    summon.DespawnOrUnsummon(TimeSpan.FromSeconds(15));
                    break;
                default:
                    break;
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim() && _phase != Phase.Intro)
                return;

            _scheduler.Update(diff);
        }
    }

    [Script]
    class npc_felblood_kaelthas_phoenix : ScriptedAI
    {
        InstanceScript _instance;

        bool _isInEgg;
        ObjectGuid _eggGUID;

        public npc_felblood_kaelthas_phoenix(Creature creature) : base(creature)
        {
            _instance = creature.GetInstanceScript();
            Initialize();
        }

        void Initialize()
        {
            me.SetReactState(ReactStates.Passive);
            _isInEgg = false;
        }

        public override void IsSummonedBy(WorldObject summoner)
        {
            DoZoneInCombat();
            DoCastSelf(SpellIds.Burn);
            DoCastSelf(SpellIds.Rebirth);
            _scheduler.Schedule(TimeSpan.FromSeconds(2), task => me.SetReactState(ReactStates.Aggressive));
        }

        public override void JustEngagedWith(Unit who) { }

        public override void DamageTaken(Unit attacker, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            if (damage >= me.GetHealth())
            {
                if (!_isInEgg)
                {
                    me.AttackStop();
                    me.SetReactState(ReactStates.Passive);
                    me.RemoveAllAuras();
                    me.SetUnitFlag(UnitFlags.Uninteractible);
                    DoCastSelf(SpellIds.EmberBlast);
                    // DoCastSelf(SpellSummonPhoenixEgg); -- We do a manual summon for now. Feel free to move it to spelleffect_dbc
                    Creature egg = DoSummon(CreatureIds.PhoenixEgg, me.GetPosition(), TimeSpan.FromSeconds(0));
                    if (egg)
                    {
                        Creature kaelthas = _instance.GetCreature(DataTypes.KaelthasSunstrider);
                        if (kaelthas)
                        {
                            kaelthas.GetAI().JustSummoned(egg);
                            _eggGUID = egg.GetGUID();
                        }
                    }

                    _scheduler.Schedule(TimeSpan.FromSeconds(15), task =>
                    {
                        Creature egg = ObjectAccessor.GetCreature(me, _eggGUID);
                        if (egg)
                            egg.DespawnOrUnsummon();

                        me.RemoveAllAuras();
                        task.Schedule(TimeSpan.FromSeconds(2), rebirthTask =>
                        {
                            DoCastSelf(SpellIds.Rebirth);
                            rebirthTask.Schedule(TimeSpan.FromSeconds(2), engageTask =>
                            {
                                _isInEgg = false;
                                DoCastSelf(SpellIds.FullHeal);
                                DoCastSelf(SpellIds.Burn);
                                me.RemoveUnitFlag(UnitFlags.Uninteractible);
                                engageTask.Schedule(TimeSpan.FromSeconds(2), task => me.SetReactState(ReactStates.Aggressive));
                            });
                        });
                    });
                    _isInEgg = true;
                }
                damage = (uint)(me.GetHealth() - 1);
            }

        }

        public override void SummonedCreatureDies(Creature summon, Unit killer)
        {
            // Egg has been destroyed within 15 seconds so we lose the phoenix.
            me.DespawnOrUnsummon();
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }

    [Script] // 44191 - Flame Strike
    class spell_felblood_kaelthas_flame_strike : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FlameStrikeDamage);
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            if (target)
                target.CastSpell(target, SpellIds.FlameStrikeDamage);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }
}

