/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockDepths.GeneralAngerforge
{
    struct SpellIds
    {
        public const uint Mightyblow = 14099;
        public const uint Hamstring = 9080;
        public const uint Cleave = 20691;
    }

    enum Phases
    {
        One = 1,
        Two = 2
    }

    [Script]
    class boss_general_angerforge : ScriptedAI
    {
        Phases phase;

        public boss_general_angerforge(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void JustEngagedWith(Unit who)
        {
            phase = Phases.One;
            _scheduler.Schedule(TimeSpan.FromSeconds(8), task =>
            {
                DoCastVictim(SpellIds.Mightyblow);
                task.Repeat(TimeSpan.FromSeconds(18));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(12), task =>
            {
                DoCastVictim(SpellIds.Hamstring);
                task.Repeat(TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(16), task =>
            {
                DoCastVictim(SpellIds.Cleave);
                task.Repeat(TimeSpan.FromSeconds(9));
            });
        }

        public override void DamageTaken(Unit attacker, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            if (me.HealthBelowPctDamaged(20, damage) && phase == Phases.One)
            {
                phase = Phases.Two;
                _scheduler.Schedule(TimeSpan.FromSeconds(0), task =>
                {
                        for (byte i = 0; i < 2; ++i)
                            SummonMedic(me.GetVictim());
                });
                _scheduler.Schedule(TimeSpan.FromSeconds(0), task =>
                {
                        for (byte i = 0; i < 3; ++i)
                            SummonAdd(me.GetVictim());
                        task.Repeat(TimeSpan.FromSeconds(25));
                });
            }
        }

        void SummonAdd(Unit victim)
        {
            Creature SummonedAdd = DoSpawnCreature(8901, RandomHelper.IRand(-14, 14), RandomHelper.IRand(-14, 14), 0, 0, TempSummonType.TimedOrCorpseDespawn, TimeSpan.FromSeconds(120));
            if (SummonedAdd)
                SummonedAdd.GetAI().AttackStart(victim);
        }

        void SummonMedic(Unit victim)
        {
            Creature SummonedMedic = DoSpawnCreature(8894, RandomHelper.IRand(-9, 9), RandomHelper.IRand(-9, 9), 0, 0, TempSummonType.TimedOrCorpseDespawn, TimeSpan.FromSeconds(120));
            if (SummonedMedic)
                SummonedMedic.GetAI().AttackStart(victim);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }
}

