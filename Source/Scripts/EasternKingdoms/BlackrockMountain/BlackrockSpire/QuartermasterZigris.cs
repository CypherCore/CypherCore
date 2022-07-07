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

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.QuartermasterZigris
{
    struct SpellIds
    {
        public const uint Shoot = 16496;
        public const uint Stunbomb = 16497;
        public const uint HealingPotion = 15504;
        public const uint Hookednet = 15609;
    }

    [Script]
    class quartermaster_zigris : BossAI
    {
        public quartermaster_zigris(Creature creature) : base(creature, DataTypes.QuartermasterZigris) { }

        public override void Reset()
        {
            _Reset();
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);

            _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
            {
                DoCastVictim(SpellIds.Shoot);
                task.Repeat(TimeSpan.FromMilliseconds(500));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(16), task =>
            {
                DoCastVictim(SpellIds.Stunbomb);
                task.Repeat(TimeSpan.FromSeconds(14));
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
