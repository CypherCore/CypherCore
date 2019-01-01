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
using System.Collections.Generic;

namespace Scripts.Northrend.FrozenHalls.ForgeOfSouls.Bronjahm
{
    struct TextIds
    {
        public const uint SayAggro = 0;
        public const uint SaySlay = 1;
        public const uint SayDeath = 2;
        public const uint SaySoulStorm = 3;
        public const uint SayCorruptSoul = 4;
    }

    struct SpellIds
    {
        public const uint MagicSBane = 68793;
        public const uint ShadowBolt = 70043;
        public const uint CorruptSoul = 68839;
        public const uint ConsumeSoul = 68861;
        public const uint Teleport = 68988;
        public const uint Fear = 68950;
        public const uint Soulstorm = 68872;
        public const uint SoulstormChannel = 69008; // Pre-Fight
        public const uint SoulstormVisual = 68870; // Pre-Cast Soulstorm
        public const uint PurpleBanishVisual = 68862;  // Used By Soul Fragment (Aura)
    }

    struct EventIds
    {
        public const uint MagicBane = 1;
        public const uint ShadowBolt = 2;
        public const uint CorruptSoul = 3;
        public const uint Soulstorm = 4;
        public const uint Fear = 5;
    }

    struct Misc
    {
        public const uint DataSoulPower = 1;

        public const byte Phase1 = 1;
        public const byte Phase2 = 2;

        public static uint[] SoulstormVisualSpells =
        {
            68904,
            68886,
            68905,
            68896,
            68906,
            68897,
            68907,
            68898
        };
    }

    [Script]
    class boss_bronjahm : BossAI
    {
        public boss_bronjahm(Creature creature) : base(creature, DataType.Bronjahm)
        {
            DoCast(me, SpellIds.SoulstormChannel, true);
        }

        public override void Reset()
        {
            _Reset();
            _events.SetPhase(Misc.Phase1);
            _events.ScheduleEvent(EventIds.ShadowBolt, 2000);
            _events.ScheduleEvent(EventIds.MagicBane, TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(20));
            _events.ScheduleEvent(EventIds.CorruptSoul, TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(35), 0, Misc.Phase1);
        }

        public override void JustReachedHome()
        {
            _JustReachedHome();
            DoCast(me, SpellIds.SoulstormChannel, true);
        }

        public override void EnterCombat(Unit who)
        {
            _EnterCombat();
            Talk(TextIds.SayAggro);
            me.RemoveAurasDueToSpell(SpellIds.SoulstormChannel);
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
            Talk(TextIds.SayDeath);
        }

        public override void KilledUnit(Unit who)
        {
            if (who.GetTypeId() == TypeId.Player)
                Talk(TextIds.SaySlay);
        }

        public override void DamageTaken(Unit attacker, ref uint damage)
        {
            if (_events.IsInPhase(Misc.Phase1) && !HealthAbovePct(30))
            {
                _events.SetPhase(Misc.Phase2);
                DoCast(me, SpellIds.Teleport);
                _events.ScheduleEvent(EventIds.Fear, TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(16), 0, Misc.Phase2);
                _events.ScheduleEvent(EventIds.Soulstorm, 100, 0, Misc.Phase2);
            }
        }

        public override void JustSummoned(Creature summon)
        {
            if (summon.GetEntry() == CreatureIds.CorruptedSoulFragment)
            {
                summons.Summon(summon);
                summon.SetReactState(ReactStates.Passive);
                summon.GetMotionMaster().MoveFollow(me, me.GetObjectSize(), 0.0f);
                summon.CastSpell(summon, SpellIds.PurpleBanishVisual, true);
            }
        }

        public override uint GetData(uint type)
        {
            if (type == Misc.DataSoulPower)
            {
                uint count = 0;
                foreach (ObjectGuid guid in summons)
                {
                    Creature summon = ObjectAccessor.GetCreature(me, guid);
                    if (summon)
                            if (summon.GetEntry() == CreatureIds.CorruptedSoulFragment && summon.IsAlive())
                        ++count;
                }

                return count;
            }

            return 0;
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
                    case EventIds.MagicBane:
                        DoCastAOE(SpellIds.MagicSBane);
                        _events.ScheduleEvent(EventIds.MagicBane, TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(20));
                        break;
                    case EventIds.ShadowBolt:
                        if (_events.IsInPhase(Misc.Phase2))
                        {
                            DoCastVictim(SpellIds.ShadowBolt);
                            _events.ScheduleEvent(EventIds.ShadowBolt, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(2));
                        }
                        else
                        {
                            if (!me.IsWithinMeleeRange(me.GetVictim()))
                                DoCastVictim(SpellIds.ShadowBolt);
                            _events.ScheduleEvent(EventIds.ShadowBolt, TimeSpan.FromMilliseconds(2));
                        }
                        break;
                    case EventIds.CorruptSoul:
                        Unit target = SelectTarget(SelectAggroTarget.Random, 1, 0.0f, true);
                        if (target)
                        {
                            Talk(TextIds.SayCorruptSoul);
                            DoCast(target, SpellIds.CorruptSoul);
                        }
                        _events.ScheduleEvent(EventIds.CorruptSoul, TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(35), 0, Misc.Phase1);
                        break;
                    case EventIds.Soulstorm:
                        Talk(TextIds.SaySoulStorm);
                        me.CastSpell(me, SpellIds.SoulstormVisual, true);
                        me.CastSpell(me, SpellIds.Soulstorm, false);
                        break;
                    case EventIds.Fear:
                        me.CastCustomSpell(SpellIds.Fear, SpellValueMod.MaxTargets, 1, null, false);
                        _events.ScheduleEvent(EventIds.Fear, TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(12), 0, Misc.Phase2);
                        break;
                    default:
                        break;
                }

                if (me.HasUnitState(UnitState.Casting))
                    return;
            });

            if (!_events.IsInPhase(Misc.Phase2))
                DoMeleeAttackIfReady();
        }
    }

    [Script]
    class npc_corrupted_soul_fragment : ScriptedAI
    {
        public npc_corrupted_soul_fragment(Creature creature) : base(creature)
        {
            instance = me.GetInstanceScript();
        }

        public override void IsSummonedBy(Unit summoner)
        {
            Creature bronjahm = ObjectAccessor.GetCreature(me, instance.GetGuidData(DataType.Bronjahm));
            if (bronjahm)
                    bronjahm.GetAI().JustSummoned(me);
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (type != MovementGeneratorType.Follow)
                return;

            if (instance.GetGuidData(DataType.Bronjahm).GetCounter() != id)
                return;

            me.CastSpell((Unit)null, SpellIds.ConsumeSoul, true);
            me.DespawnOrUnsummon();
        }

        InstanceScript instance;
    }

    [Script]
    class spell_bronjahm_magic_bane : SpellScript
    {
        void RecalculateDamage()
        {
            if (GetHitUnit().GetPowerType() != PowerType.Mana)
                return;

            int maxDamage = GetCaster().GetMap().IsHeroic() ? 15000 : 10000;
            int newDamage = GetHitDamage() + (GetHitUnit().GetMaxPower(PowerType.Mana) / 2);

            SetHitDamage(Math.Min(maxDamage, newDamage));
        }

        public override void Register()
        {
            OnHit.Add(new HitHandler(RecalculateDamage));
        }
    }

    [Script]
    class spell_bronjahm_consume_soul : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            GetHitUnit().CastSpell(GetHitUnit(), (uint)GetEffectValue(), true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_bronjahm_soulstorm_visual : AuraScript
    {
        void HandlePeriodicTick(AuraEffect aurEff)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(GetTarget(), Misc.SoulstormVisualSpells[aurEff.GetTickNumber() % 8], true);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandlePeriodicTick, 0, AuraType.PeriodicDummy));
        }
    }

    [Script]
    class spell_bronjahm_soulstorm_targeting : SpellScript
    {
        void FilterTargets(List<WorldObject> targets)
        {
            Unit caster = GetCaster();
            targets.RemoveAll(target => caster.GetExactDist2d(target) <= 10.0f);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, SpellConst.EffectAll, Targets.UnitDestAreaEnemy));
        }
    }

    [Script]
    class achievement_bronjahm_soul_power : AchievementCriteriaScript
    {
        public achievement_bronjahm_soul_power() : base("achievement_bronjahm_soul_power") { }

        public override bool OnCheck(Player source, Unit target)
        {
            return target && target.GetAI().GetData(Misc.DataSoulPower) >= 4;
        }
    }
}
