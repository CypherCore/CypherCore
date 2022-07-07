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

using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.Thebeast
{
    struct SpellIds
    {
        public const uint Flamebreak = 16785;
        public const uint Immolate = 20294;
        public const uint Terrifyingroar = 14100;
    }

    [Script]
    class boss_thebeast : BossAI
    {
        public boss_thebeast(Creature creature) : base(creature, DataTypes.TheBeast) { }

        public override void Reset()
        {
            _Reset();
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);

            _scheduler.Schedule(TimeSpan.FromSeconds(12), task =>
            {
                DoCastVictim(SpellIds.Flamebreak);
                task.Repeat(TimeSpan.FromSeconds(10));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(3), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);
                if (target)
                    DoCast(target, SpellIds.Immolate);
                task.Repeat(TimeSpan.FromSeconds(8));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(23), task =>
            {
                DoCastVictim(SpellIds.Terrifyingroar);
                task.Repeat(TimeSpan.FromSeconds(20));
            });
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }
}

