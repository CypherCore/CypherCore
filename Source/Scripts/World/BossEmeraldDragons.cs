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
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.World.BossEmeraldDragons
{
    struct CreatureIds
    {
        public const uint DragonYsondre = 14887;
        public const uint DragonLethon = 14888;
        public const uint DragonEmeriss = 14889;
        public const uint DragonTaerar = 14890;
        public const uint DreamFog = 15224;

        //Ysondre
        public const uint DementedDruid = 15260;

        //Lethon
        public const uint SpiritShade = 15261;
    }

    struct Spells
    {
        public const uint TailSweep = 15847;    // Tail Sweep - Slap Everything Behind Dragon (2 Seconds Interval)
        public const uint SummonPlayer = 24776;    // Teleport Highest Threat Player In Front Of Dragon If Wandering Off
        public const uint DreamFog = 24777;    // Auraspell For Dream Fog Npc (15224)
        public const uint Sleep = 24778;    // Sleep Triggerspell (Used For Dream Fog)
        public const uint SeepingFogLeft = 24813;    // Dream Fog - Summon Left
        public const uint SeepingFogRight = 24814;    // Dream Fog - Summon Right
        public const uint NoxiousBreath = 24818;
        public const uint MarkOfNature = 25040;    // Mark Of Nature Trigger (Applied On Target Death - 15 Minutes Of Being Suspectible To Aura Of Nature)
        public const uint MarkOfNatureAura = 25041;    // Mark Of Nature (Passive Marker-Test; Ticks Every 10 Seconds From Boss; Triggers Spellid 25042 (Scripted)
        public const uint AuraOfNature = 25043;    // Stun For 2 Minutes (Used When public const uint MarkOfNature Exists On The Target)

        //Ysondre
        public const uint LightningWave = 24819;
        public const uint SummonDruidSpirits = 24795;

        //Lethon
        public const uint DrawSpirit = 24811;
        public const uint ShadowBoltWhirl = 24834;
        public const uint DarkOffering = 24804;

        //Emeriss
        public const uint PutridMushroom = 24904;
        public const uint CorruptionOfEarth = 24910;
        public const uint VolatileInfection = 24928;

        //Taerar
        public const uint BellowingRoar = 22686;
        public const uint Shade = 24313;
        public const uint ArcaneBlast = 24857;

        public static uint[] TaerarShadeSpells = { 24841, 24842, 24843 };
    }

    struct Texts
    {
        //Ysondre
        public const uint YsondreAggro = 0;
        public const uint YsondreSummonDruids = 1;

        //Lethon
        public const uint LethonAggro = 0;
        public const uint LethonDrawSpirit = 1;

        //Emeriss
        public const uint EmerissAggro = 0;
        public const uint EmerissCastCorruption = 1;

        //Taerar
        public const uint TaerarAggro = 0;
        public const uint TaerarSummonShades = 1;
    }

    class emerald_dragonAI : WorldBossAI
    {
        public emerald_dragonAI(Creature creature) : base(creature) { }

        public override void Reset()
        {
            base.Reset();
            me.RemoveFlag(UnitFields.Flags, UnitFlags.NotSelectable | UnitFlags.NonAttackable);
            me.SetReactState(ReactStates.Aggressive);
            DoCast(me, Spells.MarkOfNatureAura, true);

            _scheduler.Schedule(TimeSpan.FromSeconds(4), task =>
            {
                // Tail Sweep is cast every two seconds, no matter what goes on in front of the dragon
                DoCast(me, Spells.TailSweep);
                task.Repeat(TimeSpan.FromSeconds(2));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(7.5), TimeSpan.FromSeconds(15), task =>
            {
                // Noxious Breath is cast on random intervals, no less than 7.5 seconds between
                DoCast(me, Spells.NoxiousBreath);
                task.Repeat();
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(12.5), TimeSpan.FromSeconds(20), task =>
            {
                // Seeping Fog appears only as "pairs", and only ONE pair at any given time!
                // Despawntime is 2 minutes, so reschedule it for new cast after 2 minutes + a minor "random time" (30 seconds at max)
                DoCast(me, Spells.SeepingFogLeft, true);
                DoCast(me, Spells.SeepingFogRight, true);
                task.Repeat(TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2.5));
            });
        }

        // Target killed during encounter, mark them as suspectible for Aura Of Nature
        public override void KilledUnit(Unit who)
        {
            if (who.IsTypeId(TypeId.Player))
                who.CastSpell(who, Spells.MarkOfNature, true);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            if (me.HasUnitState(UnitState.Casting))
                return;

            _scheduler.Update(diff);

            Unit target = SelectTarget(SelectAggroTarget.TopAggro, 0, -50.0f, true);
            if (target)
                DoCast(target, Spells.SummonPlayer);

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class npc_dream_fog : ScriptedAI
    {
        public npc_dream_fog(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            _roamTimer = 0;
        }

        public override void Reset()
        {
            Initialize();
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            if (_roamTimer == 0)
            {
                // Chase target, but don't attack - otherwise just roam around
                Unit target = SelectTarget(SelectAggroTarget.Random, 0, 0.0f, true);
                if (target)
                {
                    _roamTimer = RandomHelper.URand(15000, 30000);
                    me.GetMotionMaster().Clear(false);
                    me.GetMotionMaster().MoveChase(target, 0.2f);
                }
                else
                {
                    _roamTimer = 2500;
                    me.GetMotionMaster().Clear(false);
                    me.GetMotionMaster().MoveRandom(25.0f);
                }
                // Seeping fog movement is slow enough for a player to be able to walk backwards and still outpace it
                me.SetWalk(true);
                me.SetSpeedRate(UnitMoveType.Walk, 0.75f);
            }
            else
                _roamTimer -= diff;
        }

        uint _roamTimer;
    }

    [Script]
    class boss_ysondre : CreatureScript
    {
        public boss_ysondre() : base("boss_ysondre") { }

        class boss_ysondreAI : emerald_dragonAI
        {
            public boss_ysondreAI(Creature creature) : base(creature)
            {
                Initialize();
            }

            void Initialize()
            {
                _stage = 1;
            }

            public override void Reset()
            {
                Initialize();
                base.Reset();
                _scheduler.Schedule(TimeSpan.FromSeconds(12), task =>
                {
                    DoCastVictim(Spells.LightningWave);
                    task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20));
                });
            }

            public override void EnterCombat(Unit who)
            {
                Talk(Texts.YsondreAggro);
                base.EnterCombat(who);
            }

            // Summon druid spirits on 75%, 50% and 25% health
            public override void DamageTaken(Unit attacker, ref uint damage)
            {
                if (!HealthAbovePct(100 - 25 * _stage))
                {
                    Talk(Texts.YsondreSummonDruids);

                    for (byte i = 0; i < 10; ++i)
                        DoCast(me, Spells.SummonDruidSpirits, true);
                    ++_stage;
                }
            }

            byte _stage;
        }

        public override CreatureAI GetAI(Creature creature)
        {
            return new boss_ysondreAI(creature);
        }
    }

    [Script]
    class boss_lethon : CreatureScript
    {
        public boss_lethon() : base("boss_lethon") { }

        class boss_lethonAI : emerald_dragonAI
        {
            public boss_lethonAI(Creature creature) : base(creature)
            {
                Initialize();
            }

            void Initialize()
            {
                _stage = 1;
            }

            public override void Reset()
            {
                Initialize();
                base.Reset();

                _scheduler.Schedule(TimeSpan.FromSeconds(10), task =>
                {
                    me.CastSpell((Unit)null, Spells.ShadowBoltWhirl, false);
                    task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30));
                });
            }

            public override void EnterCombat(Unit who)
            {
                Talk(Texts.LethonAggro);
                base.EnterCombat(who);
            }

            public override void DamageTaken(Unit attacker, ref uint damage)
            {
                if (!HealthAbovePct(100 - 25 * _stage))
                {
                    Talk(Texts.LethonDrawSpirit);
                    DoCast(me, Spells.DrawSpirit);
                    ++_stage;
                }
            }

            public override void SpellHitTarget(Unit target, SpellInfo spell)
            {
                if (spell.Id == Spells.DrawSpirit && target.IsTypeId(TypeId.Player))
                {
                    Position targetPos = target.GetPosition();
                    me.SummonCreature(CreatureIds.SpiritShade, targetPos, TempSummonType.TimedDespawnOOC, 50000);
                }
            }

            byte _stage;
        }

        public override CreatureAI GetAI(Creature creature)
        {
            return new boss_lethonAI(creature);
        }
    }

    [Script]
    class npc_spirit_shade : PassiveAI
    {
        public npc_spirit_shade(Creature creature) : base(creature) { }

        public override void IsSummonedBy(Unit summoner)
        {
            _summonerGuid = summoner.GetGUID();
            me.GetMotionMaster().MoveFollow(summoner, 0.0f, 0.0f);
        }

        public override void MovementInform(MovementGeneratorType moveType, uint data)
        {
            if (moveType == MovementGeneratorType.Follow && data == _summonerGuid.GetCounter())
            {
                me.CastSpell((Unit)null, Spells.DarkOffering, false);
                me.DespawnOrUnsummon(1000);
            }
        }

        ObjectGuid _summonerGuid;
    }

    [Script]
    class boss_emeriss : CreatureScript
    {
        public boss_emeriss() : base("boss_emeriss") { }

        class boss_emerissAI : emerald_dragonAI
        {
            public boss_emerissAI(Creature creature) : base(creature)
            {
                Initialize();
            }

            void Initialize()
            {
                _stage = 1;
            }

            public override void Reset()
            {
                Initialize();
                base.Reset();

                _scheduler.Schedule(TimeSpan.FromSeconds(12), task =>
                {
                    DoCastVictim(Spells.VolatileInfection);
                    task.Repeat(TimeSpan.FromSeconds(120));
                });
            }

            public override void KilledUnit(Unit who)
            {
                if (who.IsTypeId(TypeId.Player))
                    DoCast(who, Spells.PutridMushroom, true);
                base.KilledUnit(who);
            }

            public override void EnterCombat(Unit who)
            {
                Talk(Texts.EmerissAggro);
                base.EnterCombat(who);
            }

            public override void DamageTaken(Unit attacker, ref uint damage)
            {
                if (!HealthAbovePct(100 - 25 * _stage))
                {
                    Talk(Texts.EmerissCastCorruption);
                    DoCast(me, Spells.CorruptionOfEarth, true);
                    ++_stage;
                }
            }

            byte _stage;
        }

        public override CreatureAI GetAI(Creature creature)
        {
            return new boss_emerissAI(creature);
        }
    }

    [Script]
    class boss_taerar : CreatureScript
    {
        public boss_taerar() : base("boss_taerar") { }

        class boss_taerarAI : emerald_dragonAI
        {
            public boss_taerarAI(Creature creature) : base(creature)
            {
                Initialize();
            }

            void Initialize()
            {
                _stage = 1;
                _shades = 0;
                _banished = false;
                _banishedTimer = 0;
            }

            public override void Reset()
            {
                me.RemoveAurasDueToSpell(Spells.Shade);

                Initialize();

                base.Reset();

                _scheduler.Schedule(TimeSpan.FromSeconds(12), task =>
                {
                    DoCast(Spells.ArcaneBlast);
                    task.Repeat(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(12));
                });

                _scheduler.Schedule(TimeSpan.FromSeconds(30), task =>
                {
                    DoCast(Spells.BellowingRoar);
                    task.Repeat(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(30));
                });
            }

            public override void EnterCombat(Unit who)
            {
                Talk(Texts.TaerarAggro);
                base.EnterCombat(who);
            }

            public override void SummonedCreatureDies(Creature summon, Unit killer)
            {
                --_shades;
            }

            public override void DamageTaken(Unit attacker, ref uint damage)
            {
                // At 75, 50 or 25 percent health, we need to activate the shades and go "banished"
                // Note: _stage holds the amount of times they have been summoned
                if (!_banished && !HealthAbovePct(100 - 25 * _stage))
                {
                    _banished = true;
                    _banishedTimer = 60000;

                    me.InterruptNonMeleeSpells(false);
                    DoStopAttack();

                    Talk(Texts.TaerarSummonShades);

                    foreach (var spell in Spells.TaerarShadeSpells)
                        DoCastVictim(spell, true);
                    _shades += (byte)Spells.TaerarShadeSpells.Length;

                    DoCast(Spells.Shade);
                    me.SetFlag(UnitFields.Flags, UnitFlags.NotSelectable | UnitFlags.NonAttackable);
                    me.SetReactState(ReactStates.Passive);

                    ++_stage;
                }
            }

            public override void UpdateAI(uint diff)
            {
                if (!me.IsInCombat())
                    return;

                if (_banished)
                {
                    // If all three shades are dead, OR it has taken too long, end the current event and get Taerar back into business
                    if (_banishedTimer <= diff || _shades == 0)
                    {
                        _banished = false;

                        me.RemoveFlag(UnitFields.Flags, UnitFlags.NotSelectable | UnitFlags.NonAttackable);
                        me.RemoveAurasDueToSpell(Spells.Shade);
                        me.SetReactState(ReactStates.Aggressive);
                    }
                    // _banishtimer has not expired, and we still have active shades:
                    else
                        _banishedTimer -= diff;

                    // Update the scheduler before we return (handled under emerald_dragonAI.UpdateAI(diff); if we're not inside this check)
                    _scheduler.Update(diff);

                    return;
                }

                base.UpdateAI(diff);
            }

            bool _banished;                              // used for shades activation testing
            uint _banishedTimer;                         // counter for banishment timeout
            byte _shades;                                // keep track of how many shades are dead
            byte _stage;                                 // check which "shade phase" we're at (75-50-25 percentage counters)
        }

        public override CreatureAI GetAI(Creature creature)
        {
            return new boss_taerarAI(creature);
        }
    }

    [Script]
    class spell_dream_fog_sleep : SpellScript
    {
        void FilterTargets(List<WorldObject> targets)
        {
            targets.RemoveAll(obj =>
            {
                Unit unit = obj.ToUnit();
                if (unit)
                    return unit.HasAura(Spells.Sleep);
                return true;
            });
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaEnemy));
        }
    }

    [Script]
    class spell_mark_of_nature : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(Spells.MarkOfNature, Spells.AuraOfNature);
        }

        void FilterTargets(List<WorldObject> targets)
        {
            targets.RemoveAll(obj =>
            {
                // return those not tagged or already under the influence of Aura of Nature
                Unit unit = obj.ToUnit();
                if (unit)
                    return !(unit.HasAura(Spells.MarkOfNature) && !unit.HasAura(Spells.AuraOfNature));
                return true;
            });
        }

        void HandleEffect(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            GetHitUnit().CastSpell(GetHitUnit(), Spells.AuraOfNature, true);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
            OnEffectHitTarget.Add(new EffectHandler(HandleEffect, 0, SpellEffectName.ApplyAura));
        }
    }
}
