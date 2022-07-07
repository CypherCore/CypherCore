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
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.Halycon
{

    struct SpellIds
    {
        public const uint Rend = 13738;
        public const uint Thrash = 3391;
    }

    struct TextIds
    {
        public const uint EmoteDeath = 0;
    }

    [Script]
    class boss_halycon : BossAI
    {
        static Position SummonLocation = new Position(-167.9561f, -411.7844f, 76.23057f, 1.53589f);

        bool Summoned;

        public boss_halycon(Creature creature) : base(creature, DataTypes.Halycon)
        {
            Initialize();
        }

        void Initialize()
        {
            Summoned = false;
        }

        public override void Reset()
        {
            _Reset();
            Initialize();
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);

            _scheduler.Schedule(TimeSpan.FromSeconds(17), TimeSpan.FromSeconds(20), task =>
            {
                DoCastVictim(SpellIds.Rend);
                task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), task =>
            {
                DoCast(me, SpellIds.Thrash);
            });
        }

        public override void JustDied(Unit killer)
        {
            me.SummonCreature(CreaturesIds.GizrulTheSlavener, SummonLocation, TempSummonType.TimedDespawn, TimeSpan.FromMinutes(5));
            Talk(TextIds.EmoteDeath);

            Summoned = true;
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }
}

