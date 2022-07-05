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

namespace Scripts.EasternKingdoms.AlteracValley.Vanndar
{
    struct SpellIds
    {
        public const uint Avatar = 19135;
        public const uint Thunderclap = 15588;
        public const uint Stormbolt = 20685; // not sure
    }

    struct TextIds
    {
        public const uint YellAggro = 0;
        public const uint YellEvade = 1;
        //public const uint YellRespawn1                                 = -1810010; // Missing in database
        //public const uint YellRespawn2                                 = -1810011; // Missing in database
        public const uint YellRandom = 2;
        public const uint YellSpell = 3;
    }

    [Script]
    class boss_vanndar : ScriptedAI
    {
        public boss_vanndar(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void JustEngagedWith(Unit who)
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(3), task =>
            {
                DoCastVictim(SpellIds.Avatar);
                task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(20));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(4), task =>
            {
                DoCastVictim(SpellIds.Thunderclap);
                task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(6), task =>
            {
                DoCastVictim(SpellIds.Stormbolt);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(25));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(30), task =>
            {
                Talk(TextIds.YellRandom);
                task.Repeat(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(30));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(5), task =>
            {
                if (me.GetDistance2d(me.GetHomePosition().GetPositionX(), me.GetHomePosition().GetPositionY()) > 50)
                {
                    EnterEvadeMode();
                    Talk(TextIds.YellEvade);
                }
                task.Repeat();
            });

            Talk(TextIds.YellAggro);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }
}

