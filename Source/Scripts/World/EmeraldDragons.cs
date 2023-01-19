// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.World.EmeraldDragons
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

    struct SpellIds
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

        public static uint[] TaerarShadeSpells = new uint[] { 24841, 24842, 24843 };
    }

    struct TextIds
    {
        //Ysondre
        public const uint SayYsondreAggro = 0;
        public const uint SayYsondreSummonDruids = 1;

        //Lethon
        public const uint SayLethonAggro = 0;
        public const uint SayLethonDrawSpirit = 1;

        //Emeriss
        public const uint SayEmerissAggro = 0;
        public const uint SayEmerissCastCorruption = 1;

        //Taerar
        public const uint SayTaerarAggro = 0;
        public const uint SayTaerarSummonShades = 1;
    }

    class emerald_dragonAI : WorldBossAI
    {
        public emerald_dragonAI(Creature creature) : base(creature) { }

        public override void Reset()
        {
            base.Reset();
            me.RemoveUnitFlag(UnitFlags.Uninteractible | UnitFlags.NonAttackable);
            me.SetReactState(ReactStates.Aggressive);
            DoCast(me, SpellIds.MarkOfNatureAura, new CastSpellExtraArgs(true));

            _scheduler.Schedule(TimeSpan.FromSeconds(4), task =>
            {
                // Tail Sweep is cast every two seconds, no matter what goes on in front of the dragon
                DoCast(me, SpellIds.TailSweep);
                task.Repeat(TimeSpan.FromSeconds(2));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(7.5), TimeSpan.FromSeconds(15), task =>
            {
                // Noxious Breath is cast on random intervals, no less than 7.5 seconds between
                DoCast(me, SpellIds.NoxiousBreath);
                task.Repeat();
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(12.5), TimeSpan.FromSeconds(20), task =>
            {
                // Seeping Fog appears only as "pairs", and only ONE pair at any given time!
                // Despawntime is 2 minutes, so reschedule it for new cast after 2 minutes + a minor "random time" (30 seconds at max)
                DoCast(me, SpellIds.SeepingFogLeft, new CastSpellExtraArgs(true));
                DoCast(me, SpellIds.SeepingFogRight, new CastSpellExtraArgs(true));
                task.Repeat(TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2.5));
            });
        }

        // Target killed during encounter, mark them as suspectible for Aura Of Nature
        public override void KilledUnit(Unit who)
        {
            if (who.IsTypeId(TypeId.Player))
                who.CastSpell(who, SpellIds.MarkOfNature, true);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            if (me.HasUnitState(UnitState.Casting))
                return;

            _scheduler.Update(diff);

            Unit target = SelectTarget(SelectTargetMethod.MaxThreat, 0, -50.0f, true);
            if (target)
                DoCast(target, SpellIds.SummonPlayer);

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class npc_dream_fog : ScriptedAI
    {
        uint _roamTimer;

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
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);
                if (target)
                {
                    _roamTimer = RandomHelper.URand(15000, 30000);
                    me.GetMotionMaster().Clear();
                    me.GetMotionMaster().MoveChase(target, 0.2f);
                }
                else
                {
                    _roamTimer = 2500;
                    me.GetMotionMaster().Clear();
                    me.GetMotionMaster().MoveRandom(25.0f);
                }
                // Seeping fog movement is slow enough for a player to be able to walk backwards and still outpace it
                me.SetWalk(true);
                me.SetSpeedRate(UnitMoveType.Walk, 0.75f);
            }
            else
                _roamTimer -= diff;
        }
    }

    [Script]
    class boss_ysondre : emerald_dragonAI
    {
        byte _stage;

        public boss_ysondre(Creature creature) : base(creature)
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
                DoCastVictim(SpellIds.LightningWave);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20));
            });
        }

        public override void JustEngagedWith(Unit who)
        {
            Talk(TextIds.SayYsondreAggro);
            base.JustEngagedWith(who);
        }

        // Summon druid spirits on 75%, 50% and 25% health
        public override void DamageTaken(Unit attacker, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            if (!HealthAbovePct(100 - 25 * _stage))
            {
                Talk(TextIds.SayYsondreSummonDruids);

                for (byte i = 0; i < 10; ++i)
                    DoCast(me, SpellIds.SummonDruidSpirits, new CastSpellExtraArgs(true));
                ++_stage;
            }
        }
    }

    [Script]
    class boss_lethon : emerald_dragonAI
    {
        byte _stage;

        public boss_lethon(Creature creature) : base(creature)
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
                me.CastSpell((Unit)null, SpellIds.ShadowBoltWhirl, false);
                task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30));
            });
        }

        public override void JustEngagedWith(Unit who)
        {
            Talk(TextIds.SayLethonAggro);
            base.JustEngagedWith(who);
        }

        public override void DamageTaken(Unit attacker, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            if (!HealthAbovePct(100 - 25 * _stage))
            {
                Talk(TextIds.SayLethonDrawSpirit);
                DoCast(me, SpellIds.DrawSpirit);
                ++_stage;
            }
        }

        public override void SpellHitTarget(WorldObject target, SpellInfo spellInfo)
        {
            if (spellInfo.Id == SpellIds.DrawSpirit && target.IsPlayer())
            {
                Position targetPos = target.GetPosition();
                me.SummonCreature(CreatureIds.SpiritShade, targetPos, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(50));
            }
        }
    }

    [Script]
    class npc_spirit_shade : PassiveAI
    {
        ObjectGuid _summonerGuid;

        public npc_spirit_shade(Creature creature) : base(creature) { }

        public override void IsSummonedBy(WorldObject summoner)
        {
            Unit unitSummoner = summoner.ToUnit();
            if (unitSummoner == null)
                return;

            _summonerGuid = summoner.GetGUID();
            me.GetMotionMaster().MoveFollow(unitSummoner, 0.0f, 0.0f);
        }

        public override void MovementInform(MovementGeneratorType moveType, uint data)
        {
            if (moveType == MovementGeneratorType.Follow && data == _summonerGuid.GetCounter())
            {
                me.CastSpell((Unit)null, SpellIds.DarkOffering, false);
                me.DespawnOrUnsummon(TimeSpan.FromSeconds(1));
            }
        }
    }

    [Script]
    class boss_emeriss : emerald_dragonAI
    {
        byte _stage;

        public boss_emeriss(Creature creature) : base(creature)
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
                DoCastVictim(SpellIds.VolatileInfection);
                task.Repeat(TimeSpan.FromSeconds(120));
            });
        }

        public override void KilledUnit(Unit who)
        {
            if (who.IsTypeId(TypeId.Player))
                DoCast(who, SpellIds.PutridMushroom, new CastSpellExtraArgs(true));
            base.KilledUnit(who);
        }

        public override void JustEngagedWith(Unit who)
        {
            Talk(TextIds.SayEmerissAggro);
            base.JustEngagedWith(who);
        }

        public override void DamageTaken(Unit attacker, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null) 
        {
            if (!HealthAbovePct(100 - 25 * _stage))
            {
                Talk(TextIds.SayEmerissCastCorruption);
                DoCast(me, SpellIds.CorruptionOfEarth, new CastSpellExtraArgs(true));
                ++_stage;
            }
        }
    }

    [Script]
    class boss_taerar : emerald_dragonAI
    {
        bool _banished;                              // used for shades activation testing
        uint _banishedTimer;                         // counter for banishment timeout
        byte _shades;                                // keep track of how many shades are dead
        byte _stage;                                 // check which "shade phase" we're at (75-50-25 percentage counters)

        public boss_taerar(Creature creature) : base(creature)
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
            me.RemoveAurasDueToSpell(SpellIds.Shade);

            Initialize();
            base.Reset();

            _scheduler.Schedule(TimeSpan.FromSeconds(12), task =>
            {
                DoCast(SpellIds.ArcaneBlast);
                task.Repeat(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(12));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(30), task =>
            {
                DoCast(SpellIds.BellowingRoar);
                task.Repeat(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(30));
            });
        }

        public override void JustEngagedWith(Unit who)
        {
            Talk(TextIds.SayTaerarAggro);
            base.JustEngagedWith(who);
        }

        public override void SummonedCreatureDies(Creature summon, Unit killer)
        {
            --_shades;
        }

        public override void DamageTaken(Unit attacker, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            // At 75, 50 or 25 percent health, we need to activate the shades and go "banished"
            // Note: _stage holds the amount of times they have been summoned
            if (!_banished && !HealthAbovePct(100 - 25 * _stage))
            {
                _banished = true;
                _banishedTimer = 60000;

                me.InterruptNonMeleeSpells(false);
                DoStopAttack();

                Talk(TextIds.SayTaerarSummonShades);

                foreach (var spell in SpellIds.TaerarShadeSpells)
                    DoCastVictim(spell, new CastSpellExtraArgs(true));
                _shades += (byte)SpellIds.TaerarShadeSpells.Length;

                DoCast(SpellIds.Shade);
                me.SetUnitFlag(UnitFlags.Uninteractible | UnitFlags.NonAttackable);
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
                // If all three shades are dead, Or it has taken too long, end the current event and get Taerar back into business
                if (_banishedTimer <= diff || _shades == 0)
                {
                    _banished = false;

                    me.RemoveUnitFlag(UnitFlags.Uninteractible | UnitFlags.NonAttackable);
                    me.RemoveAurasDueToSpell(SpellIds.Shade);
                    me.SetReactState(ReactStates.Aggressive);
                }
                // _banishtimer has not expired, and we still have active shades:
                else
                    _banishedTimer -= diff;

                // Update the _scheduler before we return (handled under emerald_dragonAI.UpdateAI(diff); if we're not inside this check)
                _scheduler.Update(diff);

                return;
            }

            base.UpdateAI(diff);
        }
    }

    [Script] // 24778 - Sleep
    class spell_dream_fog_sleep_SpellScript : SpellScript
    {
        void FilterTargets(List<WorldObject> targets)
        {
            targets.RemoveAll(obj =>
            {
                Unit unit = obj.ToUnit();
                if (unit)
                    return unit.HasAura(SpellIds.Sleep);
                return true;
            });
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaEnemy));
        }
    }

    [Script] // 25042 - Triggerspell - Mark of Nature
    class spell_mark_of_nature_SpellScript : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.MarkOfNature, SpellIds.AuraOfNature);
        }

        void FilterTargets(List<WorldObject> targets)
        {
            targets.RemoveAll(obj =>
            {
                // return those not tagged or already under the influence of Aura of Nature
                Unit unit = obj.ToUnit();
                if (unit)
                    return !(unit.HasAura(SpellIds.MarkOfNature) && !unit.HasAura(SpellIds.AuraOfNature));

                return true;
            });
        }

        void HandleEffect(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            GetHitUnit().CastSpell(GetHitUnit(), SpellIds.AuraOfNature, true);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
            OnEffectHitTarget.Add(new EffectHandler(HandleEffect, 0, SpellEffectName.ApplyAura));
        }
    }
}