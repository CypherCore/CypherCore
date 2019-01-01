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
using System.Linq;

namespace Scripts.Northrend.AzjolNerub.AzjolNerub.Anubarak
{
    struct SpellIds
    {
        public const uint Emerge = 53500;
        public const uint Submerge = 53421;
        public const uint ImpaleAura = 53456;
        public const uint ImpaleVisual = 53455;
        public const uint ImpaleDamage = 53454;
        public const uint LeechingSwarm = 53467;
        public const uint Pound = 59433;
        public const uint PoundDamage = 59432;
        public const uint CarrionBeetles = 53520;
        public const uint CarrionBeetle = 53521;

        public const uint SummonDarter = 53599;
        public const uint SummonAssassin = 53609;
        public const uint SummonGuardian = 53614;
        public const uint SummonVenomancer = 53615;

        public const uint Dart = 59349;
        public const uint Backstab = 52540;
        public const uint AssassinVisual = 53611;
        public const uint SunderArmor = 53618;
        public const uint PoisonBolt = 53617;
    }

    struct CreatureIds
    {
        public const uint WorldTrigger = 22515;
    }

    struct TextIds
    {
        public const uint SayAggro = 0;
        public const uint SaySlay = 1;
        public const uint SayDeath = 2;
        public const uint SayLocust = 3;
        public const uint SaySubmerge = 4;
        public const uint SayIntro = 5;
    }

    struct EventIds
    {
        public const uint Pound = 1;
        public const uint Impale = 2;
        public const uint LeechingSwarm = 3;
        public const uint CarrionBeetles = 4;
        public const uint Submerge = 5; // Use Event For This So We Don'T Submerge Mid-Cast
        public const uint Darter = 6;
        public const uint Assassin = 7;
        public const uint Guardian = 8;
        public const uint Venomancer = 9;
        public const uint CloseDoor = 10;
    }

    struct Misc
    {
        public const uint AchievGottaGoStartEvent = 20381;

        public const byte PhaseEmerge = 1;
        public const byte PhaseSubmerge = 2;

        public const int GuidTypePet = 0;
        public const int GuidTypeImpale = 1;

        public const byte SummonGroupWorldTriggerGuardian = 1;

        public const int ActionPetDied = 1;
        public const int ActionPetEvade = 2;
    }

    [Script]
    class boss_anub_arak : BossAI
    {
        public boss_anub_arak(Creature creature) : base(creature, ANDataTypes.Anubarak) { }

        public override void Reset()
        {
            base.Reset();
            me.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
            instance.DoStopCriteriaTimer(CriteriaTimedTypes.Event, Misc.AchievGottaGoStartEvent);
            _nextSubmerge = 75;
            _petCount = 0;
        }

        public override bool CanAIAttack(Unit victim) { return true; } // do not check boundary here

        public override void EnterCombat(Unit who)
        {
            base.EnterCombat(who);

            GameObject door = instance.GetGameObject(ANDataTypes.AnubarakWall);
            if (door)
                door.SetGoState(GameObjectState.Active); // open door for now
            GameObject door2 = instance.GetGameObject(ANDataTypes.AnubarakWall2);
            if (door2)
                door2.SetGoState(GameObjectState.Active);

            Talk(TextIds.SayAggro);
            instance.DoStartCriteriaTimer(CriteriaTimedTypes.Event, Misc.AchievGottaGoStartEvent);

            _events.SetPhase(Misc.PhaseEmerge);
            _events.ScheduleEvent(EventIds.CloseDoor, TimeSpan.FromSeconds(5));
            _events.ScheduleEvent(EventIds.Pound, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), 0, Misc.PhaseEmerge);
            _events.ScheduleEvent(EventIds.LeechingSwarm, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(7), 0, Misc.PhaseEmerge);
            _events.ScheduleEvent(EventIds.CarrionBeetles, TimeSpan.FromSeconds(14), TimeSpan.FromSeconds(17), 0, Misc.PhaseEmerge);

            // set up world triggers
            List<TempSummon> summoned;
            me.SummonCreatureGroup(Misc.SummonGroupWorldTriggerGuardian, out summoned);
            if (summoned.Empty()) // something went wrong
            {
                EnterEvadeMode(EvadeReason.Other);
                return;
            }
            _guardianTrigger = summoned.First().GetGUID();

            Creature trigger = DoSummon(CreatureIds.WorldTrigger, me.GetPosition(), 0u, TempSummonType.ManualDespawn);
            if (trigger)
                _assassinTrigger = trigger.GetGUID();
            else
            {
                EnterEvadeMode(EvadeReason.Other);
                return;
            }
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            summons.DespawnAll();
            _DespawnAtEvade();
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
                    case EventIds.CloseDoor:
                        GameObject door = instance.GetGameObject(ANDataTypes.AnubarakWall);
                        if (door)
                            door.SetGoState(GameObjectState.Ready);
                        GameObject door2 = instance.GetGameObject(ANDataTypes.AnubarakWall2);
                        if (door2)
                            door2.SetGoState(GameObjectState.Ready);
                        break;
                    case EventIds.Pound:
                        DoCastVictim(SpellIds.Pound);
                        _events.Repeat(TimeSpan.FromSeconds(26), TimeSpan.FromSeconds(32));
                        break;
                    case EventIds.LeechingSwarm:
                        Talk(TextIds.SayLocust);
                        DoCastAOE(SpellIds.LeechingSwarm);
                        _events.Repeat(TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(28));
                        break;
                    case EventIds.CarrionBeetles:
                        DoCastAOE(SpellIds.CarrionBeetles);
                        _events.Repeat(TimeSpan.FromSeconds(24), TimeSpan.FromSeconds(27));
                        break;
                    case EventIds.Impale:
                        Creature impaleTarget = ObjectAccessor.GetCreature(me, _impaleTarget);
                        if (impaleTarget)
                            DoCast(impaleTarget, SpellIds.ImpaleDamage, true);
                        break;
                    case EventIds.Submerge:
                        Talk(TextIds.SaySubmerge);
                        DoCastSelf(SpellIds.Submerge);
                        break;
                    case EventIds.Darter:
                        {
                            List<Creature> triggers = new List<Creature>();
                            me.GetCreatureListWithEntryInGrid(triggers, CreatureIds.WorldTrigger);
                            if (!triggers.Empty())
                            {
                                var it = triggers.SelectRandom();
                                it.CastSpell(it, SpellIds.SummonDarter, true);
                                _events.Repeat(TimeSpan.FromSeconds(11));
                            }
                            else
                                EnterEvadeMode(EvadeReason.Other);
                            break;
                        }
                    case EventIds.Assassin:
                        {
                            Creature trigger = ObjectAccessor.GetCreature(me, _assassinTrigger);
                            if (trigger)
                            {
                                trigger.CastSpell(trigger, SpellIds.SummonAssassin, true);
                                trigger.CastSpell(trigger, SpellIds.SummonAssassin, true);
                                if (_assassinCount > 2)
                                {
                                    _assassinCount -= 2;
                                    _events.Repeat(TimeSpan.FromSeconds(20));
                                }
                                else
                                    _assassinCount = 0;
                            }
                            else // something went wrong
                                EnterEvadeMode(EvadeReason.Other);
                            break;
                        }
                    case EventIds.Guardian:
                        {
                            Creature trigger = ObjectAccessor.GetCreature(me, _guardianTrigger);
                            if (trigger)
                            {
                                trigger.CastSpell(trigger, SpellIds.SummonGuardian, true);
                                trigger.CastSpell(trigger, SpellIds.SummonGuardian, true);
                                if (_guardianCount > 2)
                                {
                                    _guardianCount -= 2;
                                    _events.Repeat(TimeSpan.FromSeconds(20));
                                }
                                else
                                    _guardianCount = 0;
                            }
                            else
                                EnterEvadeMode(EvadeReason.Other);
                        }
                        break;
                    case EventIds.Venomancer:
                        {
                            Creature trigger = ObjectAccessor.GetCreature(me, _guardianTrigger);
                            if (trigger)
                            {
                                trigger.CastSpell(trigger, SpellIds.SummonVenomancer, true);
                                trigger.CastSpell(trigger, SpellIds.SummonVenomancer, true);
                                if (_venomancerCount > 2)
                                {
                                    _venomancerCount -= 2;
                                    _events.Repeat(TimeSpan.FromSeconds(20));
                                }
                                else
                                    _venomancerCount = 0;
                            }
                            else
                                EnterEvadeMode(EvadeReason.Other);
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

        public override void JustDied(Unit killer)
        {
            _JustDied();
            Talk(TextIds.SayDeath);
        }

        public override void KilledUnit(Unit victim)
        {
            if (victim.IsTypeId(TypeId.Player))
                Talk(TextIds.SaySlay);
        }

        public override void SetGUID(ObjectGuid guid, int type)
        {
            switch (type)
            {
                case Misc.GuidTypePet:
                    {
                        Creature creature = ObjectAccessor.GetCreature(me, guid);
                        if (creature)
                            JustSummoned(creature);
                        else // something has gone horribly wrong
                            EnterEvadeMode(EvadeReason.Other);
                        break;
                    }
                case Misc.GuidTypeImpale:
                    _impaleTarget = guid;
                    _events.ScheduleEvent(EventIds.Impale, TimeSpan.FromSeconds(4));
                    break;
            }
        }

        public override void DoAction(int action)
        {
            switch (action)
            {
                case Misc.ActionPetDied:
                    if (_petCount == 0) // underflow check - something has gone horribly wrong
                    {
                        EnterEvadeMode(EvadeReason.Other);
                        return;
                    }
                    if (--_petCount == 0) // last pet died, emerge
                    {
                        me.RemoveAurasDueToSpell(SpellIds.Submerge);
                        me.RemoveAurasDueToSpell(SpellIds.ImpaleAura);
                        me.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
                        DoCastSelf(SpellIds.Emerge);
                        _events.SetPhase(Misc.PhaseEmerge);
                        _events.ScheduleEvent(EventIds.Pound, TimeSpan.FromSeconds(13), TimeSpan.FromSeconds(18), 0, Misc.PhaseEmerge);
                        _events.ScheduleEvent(EventIds.LeechingSwarm, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(7), 0, Misc.PhaseEmerge);
                        _events.ScheduleEvent(EventIds.CarrionBeetles, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), 0, Misc.PhaseEmerge);
                    }
                    break;
                case Misc.ActionPetEvade:
                    EnterEvadeMode(EvadeReason.Other);
                    break;
            }
        }

        public override void DamageTaken(Unit source, ref uint damage)
        {
            if (me.HasAura(SpellIds.Submerge))
                damage = 0;
            else
                if (_nextSubmerge != 0 && me.HealthBelowPctDamaged((int)_nextSubmerge, damage))
            {
                _events.CancelEvent(EventIds.Submerge);
                _events.ScheduleEvent(EventIds.Submerge, 0, 0, Misc.PhaseEmerge);
                _nextSubmerge = _nextSubmerge - 25;
            }
        }

        public override void SpellHit(Unit whose, SpellInfo spell)
        {
            if (spell.Id == SpellIds.Submerge)
            {
                me.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
                me.RemoveAurasDueToSpell(SpellIds.LeechingSwarm);
                DoCastSelf(SpellIds.ImpaleAura, true);

                _events.SetPhase(Misc.PhaseSubmerge);
                switch (_nextSubmerge)
                {
                    case 50: // first submerge phase
                        _assassinCount = 4;
                        _guardianCount = 2;
                        _venomancerCount = 0;
                        break;
                    case 25: // second submerge phase
                        _assassinCount = 6;
                        _guardianCount = 2;
                        _venomancerCount = 2;
                        break;
                    case 0:  // third submerge phase
                        _assassinCount = 6;
                        _guardianCount = 2;
                        _venomancerCount = 2;
                        _events.ScheduleEvent(EventIds.Darter, TimeSpan.FromSeconds(0), 0, Misc.PhaseSubmerge);
                        break;
                }
                _petCount = (uint)(_guardianCount + _venomancerCount);
                if (_assassinCount != 0)
                    _events.ScheduleEvent(EventIds.Assassin, TimeSpan.FromSeconds(0), 0, Misc.PhaseSubmerge);
                if (_guardianCount != 0)
                    _events.ScheduleEvent(EventIds.Guardian, TimeSpan.FromSeconds(4), 0, Misc.PhaseSubmerge);
                if (_venomancerCount != 0)
                    _events.ScheduleEvent(EventIds.Venomancer, TimeSpan.FromSeconds(20), 0, Misc.PhaseSubmerge);
            }
        }

        ObjectGuid _impaleTarget;
        uint _nextSubmerge;
        uint _petCount;
        ObjectGuid _guardianTrigger;
        ObjectGuid _assassinTrigger;
        byte _assassinCount;
        byte _guardianCount;
        byte _venomancerCount;
    }

    class npc_anubarak_pet_template : ScriptedAI
    {
        public npc_anubarak_pet_template(Creature creature, bool isLarge) : base(creature)
        {
            _instance = creature.GetInstanceScript();
            _isLarge = isLarge;
        }

        public override void InitializeAI()
        {
            base.InitializeAI();

            Creature anubarak = _instance.GetCreature(ANDataTypes.Anubarak);
            if (anubarak)
                anubarak.GetAI().SetGUID(me.GetGUID(), Misc.GuidTypePet);
            else
                me.DespawnOrUnsummon();
        }

        public override void JustDied(Unit killer)
        {
            base.JustDied(killer);
            if (_isLarge)
            {
                Creature anubarak = _instance.GetCreature(ANDataTypes.Anubarak);
                if (anubarak)
                    anubarak.GetAI().DoAction(Misc.ActionPetDied);
            }
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            Creature anubarak = _instance.GetCreature(ANDataTypes.Anubarak);
            if (anubarak)
                anubarak.GetAI().DoAction(Misc.ActionPetEvade);
            else
                me.DespawnOrUnsummon();
        }

        protected InstanceScript _instance;
        bool _isLarge;
    }

    [Script]
    class npc_anubarak_anub_ar_darter : npc_anubarak_pet_template
    {
        public npc_anubarak_anub_ar_darter(Creature creature) : base(creature, false) { }

        public override void InitializeAI()
        {
            base.InitializeAI();
            DoCastAOE(SpellIds.Dart);
        }
    }

    [Script]
    class npc_anubarak_anub_ar_assassin : npc_anubarak_pet_template
    {
        public npc_anubarak_anub_ar_assassin(Creature creature) : base(creature, false)
        {
            _backstabTimer = 6 * Time.InMilliseconds;
        }

        bool IsInBounds(Position jumpTo, List<AreaBoundary> boundary)
        {
            if (boundary == null)
                return true;
            foreach (var it in boundary)
                if (!it.IsWithinBoundary(jumpTo))
                    return false;
            return true;
        }

        Position GetRandomPositionAround(Creature anubarak)
        {
            float DISTANCE_MIN = 10.0f;
            float DISTANCE_MAX = 30.0f;
            double angle = RandomHelper.NextDouble() * 2.0 * Math.PI;
            return new Position(anubarak.GetPositionX() + (float)(RandomHelper.FRand(DISTANCE_MIN, DISTANCE_MAX) * Math.Sin(angle)), anubarak.GetPositionY() + (float)(RandomHelper.FRand(DISTANCE_MIN, DISTANCE_MAX) * Math.Cos(angle)), anubarak.GetPositionZ());
        }

        public override void InitializeAI()
        {
            base.InitializeAI();
            var boundary = _instance.GetBossBoundary(ANDataTypes.Anubarak);
            Creature anubarak = _instance.GetCreature(ANDataTypes.Anubarak);
            if (anubarak)
            {
                Position jumpTo;
                do
                    jumpTo = GetRandomPositionAround(anubarak);
                while (!IsInBounds(jumpTo, boundary));
                me.GetMotionMaster().MoveJump(jumpTo, 40.0f, 40.0f);
                DoCastSelf(SpellIds.AssassinVisual, true);
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            if (diff >= _backstabTimer)
            {
                if (me.GetVictim() && me.GetVictim().isInBack(me))
                    DoCastVictim(SpellIds.Backstab);
                _backstabTimer = 6 * Time.InMilliseconds;
            }
            else
                _backstabTimer -= diff;

            DoMeleeAttackIfReady();
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (id == EventId.Jump)
            {
                me.RemoveAurasDueToSpell(SpellIds.AssassinVisual);
                DoZoneInCombat();
            }
        }

        uint _backstabTimer;
    }

    [Script]
    class npc_anubarak_anub_ar_guardian : npc_anubarak_pet_template
    {
        public npc_anubarak_anub_ar_guardian(Creature creature) : base(creature, true)
        {
            _sunderTimer = 6 * Time.InMilliseconds;
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            if (diff >= _sunderTimer)
            {
                DoCastVictim(SpellIds.SunderArmor);
                _sunderTimer = 12 * Time.InMilliseconds;
            }
            else
                _sunderTimer -= diff;

            DoMeleeAttackIfReady();
        }

        uint _sunderTimer;
    }

    [Script]
    class npc_anubarak_anub_ar_venomancer : npc_anubarak_pet_template
    {
        public npc_anubarak_anub_ar_venomancer(Creature creature) : base(creature, true)
        {
            _boltTimer = 5 * Time.InMilliseconds;
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            if (diff >= _boltTimer)
            {
                DoCastVictim(SpellIds.PoisonBolt);
                _boltTimer = RandomHelper.URand(2, 3) * Time.InMilliseconds;
            }
            else
                _boltTimer -= diff;

            DoMeleeAttackIfReady();
        }

        uint _boltTimer;
    }

    [Script]
    class npc_anubarak_impale_target : NullCreatureAI
    {
        public npc_anubarak_impale_target(Creature creature) : base(creature) { }

        public override void InitializeAI()
        {
            Creature anubarak = me.GetInstanceScript().GetCreature(ANDataTypes.Anubarak);
            if (anubarak)
            {
                DoCastSelf(SpellIds.ImpaleVisual);
                me.DespawnOrUnsummon(TimeSpan.FromSeconds(6));
                anubarak.GetAI().SetGUID(me.GetGUID(), Misc.GuidTypeImpale);
            }
            else
                me.DespawnOrUnsummon();
        }
    }

    [Script]
    class spell_anubarak_pound : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.PoundDamage);
        }

        void HandleDummy(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (target)
                GetCaster().CastSpell(target, SpellIds.PoundDamage, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.ApplyAura));
        }
    }

    [Script]
    class spell_anubarak_carrion_beetles : AuraScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.CarrionBeetle);
        }

        void HandlePeriodic(AuraEffect eff)
        {
            GetCaster().CastSpell(GetCaster(), SpellIds.CarrionBeetle, true);
            GetCaster().CastSpell(GetCaster(), SpellIds.CarrionBeetle, true);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDummy));
        }
    }
}
