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
using System;

namespace Scripts.Northrend.Gundrak.EckTheFerocious
{
    struct TextIds
    {
        public const uint EmoteSpawn = 0;
    }

    struct SpellIds
    {
        public const uint Berserk = 55816; // Eck goes berserk, increasing his attack speed by 150% and all damage he deals by 500%.
        public const uint Bite = 55813; // Eck bites down hard, inflicting 150% of his normal damage to an enemy.
        public const uint Spit = 55814; // Eck spits toxic bile at enemies in a cone in front of him, inflicting 2970 Nature damage and draining 220 mana every 1 sec for 3 sec.
        public const uint Spring1 = 55815; // Eck leaps at a distant target.  --> Drops aggro and charges a random player. Tank can simply taunt him back.
        public const uint Spring2 = 55837;  // Eck leaps at a distant target.
    }

    [Script]
    class boss_eck : BossAI
    {
        public boss_eck(Creature creature) : base(creature, GDDataTypes.EckTheFerocious)
        {
            Initialize();
            Talk(TextIds.EmoteSpawn);
        }

        void Initialize()
        {
            _berserk = false;
        }

        public override void Reset()
        {
            _Reset();
            Initialize();
        }

        public override void EnterCombat(Unit who)
        {
            _EnterCombat();

            _scheduler.SetValidator(() => !me.HasUnitState(UnitState.Casting));

            _scheduler.Schedule(TimeSpan.FromSeconds(5), task =>
            {
                DoCastVictim(SpellIds.Bite);
                task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(12));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(10), task =>
            {
                DoCastVictim(SpellIds.Spit);
                task.Repeat(TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(14));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(8), task =>
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 1, 35.0f, true);
                if (target)
                    DoCast(target, RandomHelper.RAND(SpellIds.Spring1, SpellIds.Spring2));
                task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));
            });

            // 60-90 secs according to wowwiki
            _scheduler.Schedule(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(90), 1, task =>
            {
                DoCast(me, SpellIds.Berserk);
                _berserk = true;
            });
        }

        public override void DamageTaken(Unit attacker, ref uint damage)
        {
            if (!_berserk && me.HealthBelowPctDamaged(20, damage))
            {
                _scheduler.RescheduleGroup(1, TimeSpan.FromSeconds(1));
                _berserk = true;
            }
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

        bool _berserk;
    }
}