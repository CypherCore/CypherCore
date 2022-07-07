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

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockDepths.HighInterrogatorGerstahn
{
    struct SpellIds
    {
        public const uint Shadowwordpain = 10894;
        public const uint Manaburn = 10876;
        public const uint Psychicscream = 8122;
        public const uint Shadowshield = 22417;
    }

    [Script]
    class boss_high_interrogator_gerstahn : ScriptedAI
    {
        public boss_high_interrogator_gerstahn(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void JustEngagedWith(Unit who)
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(4), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100.0f, true);
                if (target)
                    DoCast(target, SpellIds.Shadowwordpain);
                task.Repeat(TimeSpan.FromSeconds(7));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(14), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100.0f, true);
                if (target)
                    DoCast(target, SpellIds.Manaburn);
                task.Repeat(TimeSpan.FromSeconds(10));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(32), task =>
            {
                DoCastVictim(SpellIds.Psychicscream);
                task.Repeat(TimeSpan.FromSeconds(30));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(8), task =>
            {
                DoCast(me, SpellIds.Shadowshield);
                task.Repeat(TimeSpan.FromSeconds(25));
            });
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }
}

