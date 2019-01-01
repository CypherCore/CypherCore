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
using Game.Entities;
using System.Collections.Generic;

namespace Game.AI
{
    public class CombatAI : CreatureAI
    {
        public CombatAI(Creature c) : base(c) { }

        public override void InitializeAI()
        {
            for (var i = 0; i < SharedConst.MaxCreatureSpells; ++i)
                if (me.m_spells[i] != 0 && Global.SpellMgr.GetSpellInfo(me.m_spells[i]) != null)
                    spells.Add(me.m_spells[i]);

            base.InitializeAI();
        }

        public override void Reset()
        {
            _events.Reset();
        }

        public override void JustDied(Unit killer)
        {
            foreach (var id in spells)
                if (AISpellInfo[id].condition == AICondition.Die)
                    me.CastSpell(killer, id, true);
        }

        public override void EnterCombat(Unit victim)
        {
            foreach (var id in spells)
            {
                if (AISpellInfo[id].condition == AICondition.Aggro)
                    me.CastSpell(victim, id, false);
                else if (AISpellInfo[id].condition == AICondition.Combat)
                    _events.ScheduleEvent(id, AISpellInfo[id].cooldown + RandomHelper.Rand32() % AISpellInfo[id].cooldown);
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
                _events.ScheduleEvent(spellId, AISpellInfo[spellId].cooldown + RandomHelper.Rand32() % AISpellInfo[spellId].cooldown);
            }

            DoMeleeAttackIfReady();
        }

        public override void SpellInterrupted(uint spellId, uint unTimeMs)
        {
            _events.RescheduleEvent(spellId, unTimeMs);
        }

        public List<uint> spells = new List<uint>();
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
        public CasterAI(Creature c)
            : base(c)
        {
            m_attackDist = SharedConst.MeleeRange;
        }

        public override void InitializeAI()
        {
            base.InitializeAI();

            m_attackDist = 30.0f;
            foreach (var id in spells)
                if (AISpellInfo[id].condition == AICondition.Combat && m_attackDist > AISpellInfo[id].maxRange)
                    m_attackDist = AISpellInfo[id].maxRange;
            if (m_attackDist == 30.0f)
                m_attackDist = SharedConst.MeleeRange;
        }

        public override void AttackStart(Unit victim)
        {
            AttackStartCaster(victim, m_attackDist);
        }

        public override void EnterCombat(Unit victim)
        {
            if (spells.Empty())
                return;

            int spell = (int)(RandomHelper.Rand32() % spells.Count);
            uint count = 0;
            foreach (var id in spells)
            {

                if (AISpellInfo[id].condition == AICondition.Aggro)
                    me.CastSpell(victim, id, false);
                else if (AISpellInfo[id].condition == AICondition.Combat)
                {
                    uint cooldown = AISpellInfo[id].realCooldown;
                    if (count == spell)
                    {
                        DoCast(spells[spell]);
                        cooldown += (uint)me.GetCurrentSpellCastTime(id);
                    }
                    _events.ScheduleEvent(id, cooldown);
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
                _events.ScheduleEvent(spellId, (casttime != 0 ? casttime : 500) + AISpellInfo[spellId].realCooldown);
            }
        }

        float m_attackDist;
    }

    public class ArcherAI : CreatureAI
    {
        public ArcherAI(Creature c)
            : base(c)
        {
            if (me.m_spells[0] == 0)
                Log.outError(LogFilter.ScriptsAi, "ArcherAI set for creature (entry = {0}) with spell1=0. AI will do nothing", me.GetEntry());

            var spellInfo = Global.SpellMgr.GetSpellInfo(me.m_spells[0]);
            m_minRange = spellInfo != null ? spellInfo.GetMinRange(false) : 0;

            if (m_minRange == 0)
                m_minRange = SharedConst.MeleeRange;
            me.m_CombatDistance = spellInfo != null ? spellInfo.GetMaxRange(false) : 0;
            me.m_SightDistance = me.m_CombatDistance;
        }

        public override void AttackStart(Unit who)
        {
            if (who == null)
                return;

            if (me.IsWithinCombatRange(who, m_minRange))
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

            if (!me.IsWithinCombatRange(me.GetVictim(), m_minRange))
                DoSpellAttackIfReady(me.m_spells[0]);
            else
                DoMeleeAttackIfReady();
        }

        float m_minRange;
    }

    public class TurretAI : CreatureAI
    {
        public TurretAI(Creature c)
            : base(c)
        {
            if (me.m_spells[0] == 0)
                Log.outError(LogFilter.Server, "TurretAI set for creature (entry = {0}) with spell1=0. AI will do nothing", me.GetEntry());

            var spellInfo = Global.SpellMgr.GetSpellInfo(me.m_spells[0]);
            m_minRange = spellInfo != null ? spellInfo.GetMinRange(false) : 0;
            me.m_CombatDistance = spellInfo != null ? spellInfo.GetMaxRange(false) : 0;
            me.m_SightDistance = me.m_CombatDistance;
        }

        public override bool CanAIAttack(Unit victim)
        {
            // todo use one function to replace it
            if (!me.IsWithinCombatRange(me.GetVictim(), me.m_CombatDistance)
                || (m_minRange != 0 && me.IsWithinCombatRange(me.GetVictim(), m_minRange)))
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

        float m_minRange;
    }

    public class VehicleAI : CreatureAI
    {
        const int VEHICLE_CONDITION_CHECK_TIME = 1000;
        const int VEHICLE_DISMISS_TIME = 5000;

        public VehicleAI(Creature creature) : base(creature)
        {
            m_ConditionsTimer = VEHICLE_CONDITION_CHECK_TIME;
            LoadConditions();
            m_DoDismiss = false;
            m_DismissTimer = VEHICLE_DISMISS_TIME;
        }

        public override void UpdateAI(uint diff)
        {
            CheckConditions(diff);

            if (m_DoDismiss)
            {
                if (m_DismissTimer < diff)
                {
                    m_DoDismiss = false;
                    me.DespawnOrUnsummon();
                }
                else
                    m_DismissTimer -= diff;
            }
        }

        public override void MoveInLineOfSight(Unit who) { }

        public override void AttackStart(Unit victim) { }

        public override void OnCharmed(bool apply)
        {
            if (!me.GetVehicleKit().IsVehicleInUse() && !apply && m_HasConditions)//was used and has conditions
                m_DoDismiss = true;//needs reset
            else if (apply)
                m_DoDismiss = false;//in use again

            m_DismissTimer = VEHICLE_DISMISS_TIME;//reset timer
        }

        void LoadConditions()
        {
            m_HasConditions = Global.ConditionMgr.HasConditionsForNotGroupedEntry(ConditionSourceType.CreatureTemplateVehicle, me.GetEntry());
        }

        void CheckConditions(uint diff)
        {
            if (m_ConditionsTimer < diff)
            {
                if (m_HasConditions)
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
                }
                m_ConditionsTimer = VEHICLE_CONDITION_CHECK_TIME;
            }
            else 
                m_ConditionsTimer -= diff;
        }

        bool m_HasConditions;
        uint m_ConditionsTimer;
        bool m_DoDismiss;
        uint m_DismissTimer;
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
