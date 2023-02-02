// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using System;
using System.Collections.Generic;

namespace Game.AI
{
    public class CombatAI : CreatureAI
    {
        protected List<uint> _spells = new();

        public CombatAI(Creature c) : base(c) { }

        public override void InitializeAI()
        {
            for (var i = 0; i < SharedConst.MaxCreatureSpells; ++i)
                if (me.m_spells[i] != 0 && Global.SpellMgr.HasSpellInfo(me.m_spells[i], me.GetMap().GetDifficultyID()))
                    _spells.Add(me.m_spells[i]);

            base.InitializeAI();
        }

        public override void Reset()
        {
            _events.Reset();
        }

        public override void JustDied(Unit killer)
        {
            foreach (var id in _spells)
            {
                AISpellInfoType info = GetAISpellInfo(id, me.GetMap().GetDifficultyID());
                if (info != null && info.condition == AICondition.Die)
                    me.CastSpell(killer, id, true);
            }
        }

        public override void JustEngagedWith(Unit victim)
        {
            foreach (var id in _spells)
            {
                AISpellInfoType info = GetAISpellInfo(id, me.GetMap().GetDifficultyID());
                if (info != null)
                {
                    if (info.condition == AICondition.Aggro)
                        me.CastSpell(victim, id, false);
                    else if (info.condition == AICondition.Combat)
                        _events.ScheduleEvent(id, info.cooldown, info.cooldown * 2);
                }
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _events.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            uint spellId = _events.ExecuteEvent();
            if (spellId != 0)
            {
                DoCast(spellId);
                AISpellInfoType info = GetAISpellInfo(spellId, me.GetMap().GetDifficultyID());
                if (info != null)
                    _events.ScheduleEvent(spellId, info.cooldown, info.cooldown * 2);
            }
            else
                DoMeleeAttackIfReady();
        }

        public override void SpellInterrupted(uint spellId, uint unTimeMs)
        {
            _events.RescheduleEvent(spellId, TimeSpan.FromMilliseconds(unTimeMs));
        }
    }

    public class AggressorAI : CreatureAI
    {
        public AggressorAI(Creature c) : base(c) { }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            DoMeleeAttackIfReady();
        }
    }

    public class CasterAI : CombatAI
    {
        float _attackDistance;

        public CasterAI(Creature creature) : base(creature)
        {
            _attackDistance = SharedConst.MeleeRange;
        }

        public override void InitializeAI()
        {
            base.InitializeAI();

            _attackDistance = 30.0f;
            foreach (var id in _spells)
            {
                AISpellInfoType info = GetAISpellInfo(id, me.GetMap().GetDifficultyID());
                if (info != null && info.condition == AICondition.Combat && _attackDistance > info.maxRange)
                    _attackDistance = info.maxRange;
            }

            if (_attackDistance == 30.0f)
                _attackDistance = SharedConst.MeleeRange;
        }

        public override void AttackStart(Unit victim)
        {
            AttackStartCaster(victim, _attackDistance);
        }

        public override void JustEngagedWith(Unit victim)
        {
            if (_spells.Empty())
                return;

            int spell = (int)(RandomHelper.Rand32() % _spells.Count);
            uint count = 0;
            foreach (var id in _spells)
            {
                AISpellInfoType info = GetAISpellInfo(id, me.GetMap().GetDifficultyID());
                if (info != null)
                {
                    if (info.condition == AICondition.Aggro)
                        me.CastSpell(victim, id, false);
                    else if (info.condition == AICondition.Combat)
                    {
                        TimeSpan cooldown = info.realCooldown;
                        if (count == spell)
                        {
                            DoCast(_spells[spell]);
                            cooldown += TimeSpan.FromMilliseconds(me.GetCurrentSpellCastTime(id));
                        }
                        _events.ScheduleEvent(id, cooldown);
                    }
                }
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _events.Update(diff);

            if (me.GetVictim().HasBreakableByDamageCrowdControlAura(me))
            {
                me.InterruptNonMeleeSpells(false);
                return;
            }

            if (me.HasUnitState(UnitState.Casting))
                return;

            uint spellId = _events.ExecuteEvent();
            if (spellId != 0)
            {
                DoCast(spellId);
                uint casttime = (uint)me.GetCurrentSpellCastTime(spellId);
                AISpellInfoType info = GetAISpellInfo(spellId, me.GetMap().GetDifficultyID());
                if (info != null)
                    _events.ScheduleEvent(spellId, TimeSpan.FromMilliseconds(casttime != 0 ? casttime : 500) + info.realCooldown);
            }
        }
    }

    public class ArcherAI : CreatureAI
    {
        float _minRange;

        public ArcherAI(Creature creature) : base(creature)
        {
            if (creature.m_spells[0] == 0)
                Log.outError(LogFilter.ScriptsAi, $"ArcherAI set for creature with spell1=0. AI will do nothing ({me.GetGUID()})");

            var spellInfo = Global.SpellMgr.GetSpellInfo(creature.m_spells[0], creature.GetMap().GetDifficultyID());
            _minRange = spellInfo != null ? spellInfo.GetMinRange(false) : 0;

            if (_minRange == 0)
                _minRange = SharedConst.MeleeRange;

            creature.m_CombatDistance = spellInfo != null ? spellInfo.GetMaxRange(false) : 0;
            creature.m_SightDistance = creature.m_CombatDistance;
        }

        public override void AttackStart(Unit who)
        {
            if (who == null)
                return;

            if (me.IsWithinCombatRange(who, _minRange))
            {
                if (me.Attack(who, true) && !who.IsFlying())
                    me.GetMotionMaster().MoveChase(who);
            }
            else
            {
                if (me.Attack(who, false) && !who.IsFlying())
                    me.GetMotionMaster().MoveChase(who, me.m_CombatDistance);
            }

            if (who.IsFlying())
                me.GetMotionMaster().MoveIdle();
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            if (!me.IsWithinCombatRange(me.GetVictim(), _minRange))
                DoSpellAttackIfReady(me.m_spells[0]);
            else
                DoMeleeAttackIfReady();
        }
    }

    public class TurretAI : CreatureAI
    {
        float _minRange;

        public TurretAI(Creature creature) : base(creature)
        {
            if (creature.m_spells[0] == 0)
                Log.outError(LogFilter.Server, $"TurretAI set for creature with spell1=0. AI will do nothing ({creature.GetGUID()})");

            var spellInfo = Global.SpellMgr.GetSpellInfo(creature.m_spells[0], creature.GetMap().GetDifficultyID());
            _minRange = spellInfo != null ? spellInfo.GetMinRange(false) : 0;
            creature.m_CombatDistance = spellInfo != null ? spellInfo.GetMaxRange(false) : 0;
            creature.m_SightDistance = creature.m_CombatDistance;
        }

        public override bool CanAIAttack(Unit victim)
        {
            // todo use one function to replace it
            if (!me.IsWithinCombatRange(victim, me.m_CombatDistance) || (_minRange != 0 && me.IsWithinCombatRange(victim, _minRange)))
                return false;
            return true;
        }

        public override void AttackStart(Unit victim)
        {
            if (victim != null)
                me.Attack(victim, false);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            DoSpellAttackIfReady(me.m_spells[0]);
        }
    }

    public class VehicleAI : CreatureAI
    {
        const int VEHICLE_CONDITION_CHECK_TIME = 1000;
        const int VEHICLE_DISMISS_TIME = 5000;

        bool _hasConditions;
        uint _conditionsTimer;
        bool _doDismiss;
        uint _dismissTimer;

        public VehicleAI(Creature creature) : base(creature)
        {
            _conditionsTimer = VEHICLE_CONDITION_CHECK_TIME;
            LoadConditions();
            _doDismiss = false;
            _dismissTimer = VEHICLE_DISMISS_TIME;
        }

        public override void UpdateAI(uint diff)
        {
            CheckConditions(diff);

            if (_doDismiss)
            {
                if (_dismissTimer < diff)
                {
                    _doDismiss = false;
                    me.DespawnOrUnsummon();
                }
                else
                    _dismissTimer -= diff;
            }
        }

        public override void MoveInLineOfSight(Unit who) { }

        public override void AttackStart(Unit victim) { }

        public override void OnCharmed(bool isNew)
        {
            bool charmed = me.IsCharmed();
            if (!me.GetVehicleKit().IsVehicleInUse() && !charmed && _hasConditions)//was used and has conditions
                _doDismiss = true;//needs reset
            else if (charmed)
                _doDismiss = false;//in use again

            _dismissTimer = VEHICLE_DISMISS_TIME;//reset timer
        }

        void LoadConditions()
        {
            _hasConditions = Global.ConditionMgr.HasConditionsForNotGroupedEntry(ConditionSourceType.CreatureTemplateVehicle, me.GetEntry());
        }

        void CheckConditions(uint diff)
        {
            if (!_hasConditions)
                return;

            if (_conditionsTimer <= diff)
            {
                Vehicle vehicleKit = me.GetVehicleKit();
                if (vehicleKit)
                {
                    foreach (var pair in vehicleKit.Seats)
                    {
                        Unit passenger = Global.ObjAccessor.GetUnit(me, pair.Value.Passenger.Guid);
                        if (passenger)
                        {
                            Player player = passenger.ToPlayer();
                            if (player)
                            {
                                if (!Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.CreatureTemplateVehicle, me.GetEntry(), player, me))
                                {
                                    player.ExitVehicle();
                                    return;//check other pessanger in next tick
                                }
                            }
                        }
                    }
                }

                _conditionsTimer = VEHICLE_CONDITION_CHECK_TIME;
            }
            else
                _conditionsTimer -= diff;
        }
    }

    public class ReactorAI : CreatureAI
    {
        public ReactorAI(Creature c) : base(c) { }

        public override void MoveInLineOfSight(Unit who) { }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            DoMeleeAttackIfReady();
        }
    }
}
