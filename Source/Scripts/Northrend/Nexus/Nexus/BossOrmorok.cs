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

namespace Scripts.Northrend.Nexus.Nexus
{
    struct OrmorokConst
    {
        //Spells
        public const uint SpellReflection = 47981;
        public const uint SpellTrample = 48016;
        public const uint SpellFrenzy = 48017;
        public const uint SpellSummonCrystallineTangler = 61564;
        public const uint SpellCrystalSpikes = 47958;

        //Texts
        public const uint SayAggro = 1;
        public const uint SayDeath = 2;
        public const uint SayReflect = 3;
        public const uint SayCrystalSpikes = 4;
        public const uint SayKill = 5;
        public const uint SayFrenzy = 6;
    }

    [Script]
    class boss_ormorok : BossAI
    {
        public boss_ormorok(Creature creature) : base(creature, DataTypes.Ormorok)
        {
            Initialize();
        }

        void Initialize()
        {
            frenzy = false;
        }

        public override void Reset()
        {
            base.Reset();
            Initialize();
        }

        public override void EnterCombat(Unit who)
        {
            _EnterCombat();

            //Crystal Spikes
            _scheduler.Schedule(TimeSpan.FromSeconds(12), task =>
            {
                Talk(OrmorokConst.SayCrystalSpikes);
                DoCast(OrmorokConst.SpellCrystalSpikes);
                task.Repeat(TimeSpan.FromSeconds(12));
            });

            //Trample
            _scheduler.Schedule(TimeSpan.FromSeconds(10), task =>
            {
                DoCast(me, OrmorokConst.SpellTrample);
                task.Repeat(TimeSpan.FromSeconds(10));
            });

            //Spell Reflection
            _scheduler.Schedule(TimeSpan.FromSeconds(30), task =>
            {
                Talk(OrmorokConst.SayReflect);
                DoCast(me, OrmorokConst.SpellReflection);
                task.Repeat(TimeSpan.FromSeconds(30));
            });

            //Heroic Crystalline Tangler
            if (IsHeroic())
            {
                _scheduler.Schedule(TimeSpan.FromSeconds(17), task =>
                {
                    Unit target = SelectTarget(SelectAggroTarget.Random, 0, new OrmorokTanglerPredicate(me));
                    if (target)
                        DoCast(target, OrmorokConst.SpellSummonCrystallineTangler);

                    task.Repeat(TimeSpan.FromSeconds(17));
                });
            }

            Talk(OrmorokConst.SayAggro);
        }

        public override void DamageTaken(Unit attacker, ref uint damage)
        {
            if (!frenzy && HealthBelowPct(25))
            {
                Talk(OrmorokConst.SayFrenzy);
                DoCast(me, OrmorokConst.SpellFrenzy);
                frenzy = true;
            }
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
            Talk(OrmorokConst.SayDeath);
        }

        public override void KilledUnit(Unit who)
        {
            if (who.IsTypeId(TypeId.Player))
                Talk(OrmorokConst.SayKill);
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

        bool frenzy;
    }

    class OrmorokTanglerPredicate : ISelector
    {
        public OrmorokTanglerPredicate(Unit unit)
        {
            me = unit;
        }

        public bool Check(Unit target)
        {
            return target.GetDistance2d(me) >= 5.0f;
        }

        Unit me;
    }

    struct CrystalSpikesConst
    {
        public const uint NpcCrystalSpikeInitial = 27101;
        public const uint NpcCrystalSpikeTrigger = 27079;

        public const uint DataCount = 1;
        public const uint MaxCount = 5;

        public const uint SpellCrystalSpikeDamage = 47944;

        public const uint GoCrystalSpikeTrap = 188537;


        public static uint[] CrystalSpikeSummon =
        {
            47936,
            47942,
            7943
        };
    }

    [Script]
    class npc_crystal_spike_trigger : ScriptedAI
    {
        public npc_crystal_spike_trigger(Creature creature) : base(creature) { }

        public override void IsSummonedBy(Unit owner)
        {
            switch (me.GetEntry())
            {
                case CrystalSpikesConst.NpcCrystalSpikeInitial:
                    _count = 0;
                    me.SetFacingToObject(owner);
                    break;
                case CrystalSpikesConst.NpcCrystalSpikeTrigger:
                    Creature trigger = owner.ToCreature();
                    if (trigger)
                        _count = trigger.GetAI().GetData(CrystalSpikesConst.DataCount) + 1;
                    break;
                default:
                    _count = CrystalSpikesConst.MaxCount;
                    break;
            }

            if (me.GetEntry() == CrystalSpikesConst.NpcCrystalSpikeTrigger)
            {
                GameObject trap = me.FindNearestGameObject(CrystalSpikesConst.GoCrystalSpikeTrap, 1.0f);
                if (trap)
                    trap.Use(me);
            }

            //Despawn
            _scheduler.Schedule(TimeSpan.FromSeconds(2), task =>
            {
                if (me.GetEntry() == CrystalSpikesConst.NpcCrystalSpikeTrigger)
                {
                    GameObject trap = me.FindNearestGameObject(CrystalSpikesConst.GoCrystalSpikeTrap, 1.0f);
                    if (trap)
                        trap.Delete();
                }

                me.DespawnOrUnsummon();
            });
        }

        public override uint GetData(uint type)
        {
            return type == CrystalSpikesConst.DataCount ? _count : 0;
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }

        uint _count;
    }

    [Script]
    class spell_crystal_spike : AuraScript
    {
        void HandlePeriodic(AuraEffect aurEff)
        {
            Unit target = GetTarget();
            if (target.GetEntry() == CrystalSpikesConst.NpcCrystalSpikeInitial || target.GetEntry() == CrystalSpikesConst.NpcCrystalSpikeTrigger)
            {
                Creature trigger = target.ToCreature();
                if (trigger)
                {
                    uint spell = target.GetEntry() == CrystalSpikesConst.NpcCrystalSpikeInitial ? CrystalSpikesConst.CrystalSpikeSummon[0] : CrystalSpikesConst.CrystalSpikeSummon[RandomHelper.IRand(0, 2)];
                    if (trigger.GetAI().GetData(CrystalSpikesConst.DataCount) < CrystalSpikesConst.MaxCount)
                        trigger.CastSpell(trigger, spell, true);
                }
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDummy));
        }
    }
}
