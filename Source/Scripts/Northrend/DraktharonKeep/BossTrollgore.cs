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

namespace Scripts.Northrend.DraktharonKeep.Trollgore
{
    struct SpellIds
    {
        public const uint InfectedWound = 49637;
        public const uint Crush = 49639;
        public const uint CorpseExplode = 49555;
        public const uint CorpseExplodeDamage = 49618;
        public const uint Consume = 49380;
        public const uint ConsumeBuff = 49381;
        public const uint ConsumeBuffH = 59805;

        public const uint SummonInvaderA = 49456;
        public const uint SummonInvaderB = 49457;
        public const uint SummonInvaderC = 49458; // Can'T Find Any Sniffs

        public const uint InvaderTaunt = 49405;
    }

    struct TextIds
    {
        public const uint SayAggro = 0;
        public const uint SayKill = 1;
        public const uint SayConsume = 2;
        public const uint SayExplode = 3;
        public const uint SayDeath = 4;
    }

    struct Misc
    {
        public const uint DataConsumptionJunction = 1;
        public const uint PointLanding = 1;

        public static Position Landing = new Position(-263.0534f, -660.8658f, 26.50903f, 0.0f);
    }

    [Script]
    class boss_trollgore : BossAI
    {
        public boss_trollgore(Creature creature) : base(creature, DTKDataTypes.Trollgore)
        {
            Initialize();
        }

        void Initialize()
        {
            _consumptionJunction = true;
        }

        public override void Reset()
        {
            _Reset();
            Initialize();
        }

        public override void EnterCombat(Unit who)
        {
            _EnterCombat();
            Talk(TextIds.SayAggro);

            _scheduler.SetValidator(() => !me.HasUnitState(UnitState.Casting));

            _scheduler.Schedule(TimeSpan.FromSeconds(15), task =>
             {
                 Talk(TextIds.SayConsume);
                 DoCastAOE(SpellIds.Consume);
                 task.Repeat();
             });

            _scheduler.Schedule(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), task =>
            {
                DoCastVictim(SpellIds.Crush);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60), task =>
            {
                DoCastVictim(SpellIds.InfectedWound);
                task.Repeat(TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(35));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(3), task =>
            {
                Talk(TextIds.SayExplode);
                DoCastAOE(SpellIds.CorpseExplode);
                task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(19));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(40), task =>
            {
                for (byte i = 0; i < 3; ++i)
                {
                    Creature trigger = ObjectAccessor.GetCreature(me, instance.GetGuidData(DTKDataTypes.TrollgoreInvaderSummoner1 + i));
                    if (trigger)
                        trigger.CastSpell(trigger, RandomHelper.RAND(SpellIds.SummonInvaderA, SpellIds.SummonInvaderB, SpellIds.SummonInvaderC), true, null, null, me.GetGUID());
                }

                task.Repeat();
            });
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            if (_consumptionJunction)
            {
                Aura ConsumeAura = me.GetAura(DungeonMode(SpellIds.ConsumeBuff, SpellIds.ConsumeBuffH));
                if (ConsumeAura != null && ConsumeAura.GetStackAmount() > 9)
                    _consumptionJunction = false;
            }

            DoMeleeAttackIfReady();
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
            Talk(TextIds.SayDeath);
        }

        public override uint GetData(uint type)
        {
            if (type == Misc.DataConsumptionJunction)
                return _consumptionJunction ? 1 : 0u;

            return 0;
        }

        public override void KilledUnit(Unit victim)
        {
            if (victim.IsTypeId(TypeId.Player))
                Talk(TextIds.SayKill);
        }

        public override void JustSummoned(Creature summon)
        {
            summon.GetMotionMaster().MovePoint(Misc.PointLanding, Misc.Landing);
            summons.Summon(summon);
        }

        bool _consumptionJunction;
    }

    [Script]
    class npc_drakkari_invader : ScriptedAI
    {
        public npc_drakkari_invader(Creature creature) : base(creature) { }

        public override void MovementInform(MovementGeneratorType type, uint pointId)
        {
            if (type == MovementGeneratorType.Point && pointId == Misc.PointLanding)
            {
                me.Dismount();
                me.RemoveFlag(UnitFields.Flags, UnitFlags.ImmuneToPc | UnitFlags.ImmuneToNpc);
                DoCastAOE(SpellIds.InvaderTaunt);
            }
        }
    }

    [Script] // 49380, 59803 - Consume
    class spell_trollgore_consume : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ConsumeBuff);
        }

        void HandleConsume(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (target)
                target.CastSpell(GetCaster(), SpellIds.ConsumeBuff, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleConsume, 1, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 49555, 59807 - Corpse Explode
    class spell_trollgore_corpse_explode : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CorpseExplodeDamage);
        }

        void PeriodicTick(AuraEffect aurEff)
        {
            if (aurEff.GetTickNumber() == 2)
            {
                Unit caster = GetCaster();
                if (caster)
                    caster.CastSpell(GetTarget(), SpellIds.CorpseExplodeDamage, true, null, aurEff);
            }
        }

        void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Creature target = GetTarget().ToCreature();
            if (target)
                target.DespawnOrUnsummon();
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(PeriodicTick, 0, AuraType.Dummy));
            AfterEffectRemove.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 49405 - Invader Taunt Trigger
    class spell_trollgore_invader_taunt : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo((uint)spellInfo.GetEffect(0).CalcValue());
        }

        void HandleTaunt(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (target)
                target.CastSpell(GetCaster(), (uint)GetEffectValue(), true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleTaunt, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class achievement_consumption_junction : AchievementCriteriaScript
    {
        public achievement_consumption_junction() : base("achievement_consumption_junction") { }

        public override bool OnCheck(Player source, Unit target)
        {
            if (!target)
                return false;

            Creature Trollgore = target.ToCreature();
            if (Trollgore)
                if (Trollgore.GetAI().GetData(Misc.DataConsumptionJunction) != 0)
                    return true;

            return false;
        }
    }
}
