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

namespace Scripts.EasternKingdoms.AlteracValley.Galvangar
{
    struct SpellIds
    {
        public const uint Cleave = 15284;
        public const uint FrighteningShout = 19134;
        public const uint Whirlwind1 = 15589;
        public const uint Whirlwind2 = 13736;
        public const uint MortalStrike = 16856;
    }

    struct TextIds
    {
        public const uint SayAggro = 0;
        public const uint SayEvade = 1;
        public const uint SayBuff = 2;
    }

    struct ActionIds
    {
        public const int BuffYell = -30001; // shared from Battleground
    }

    [Script]
    class boss_galvangar : ScriptedAI
    {
        public boss_galvangar(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void JustEngagedWith(Unit who)
        {
            Talk(TextIds.SayAggro);
            _scheduler.Schedule(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(9), task =>
            {
                DoCastVictim(SpellIds.Cleave);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(16));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(19), task =>
            {
                DoCastVictim(SpellIds.FrighteningShout);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(13), task =>
            {
                DoCastVictim(SpellIds.Whirlwind1);
                task.Repeat(TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(10));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20), task =>
            {
                DoCastVictim(SpellIds.Whirlwind2);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(25));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20), task =>
            {
                DoCastVictim(SpellIds.MortalStrike);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
            });
        }

        public override void DoAction(int actionId)
        {
            if (actionId == ActionIds.BuffYell)
                Talk(TextIds.SayBuff);
        }

        public override bool CheckInRoom()
        {
            if (me.GetDistance2d(me.GetHomePosition().GetPositionX(), me.GetHomePosition().GetPositionY()) > 50)
            {
                EnterEvadeMode();
                Talk(TextIds.SayEvade);
                return false;
            }

            return true;
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim() || !CheckInRoom())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }
}

