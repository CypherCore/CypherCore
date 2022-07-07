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

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.GizrulTheSlavener
{
    struct SpellIds
    {
        public const uint FatalBite = 16495;
        public const uint InfectedBite = 16128;
        public const uint Frenzy = 8269;
    }

    struct PathIds
    {
        public const uint Gizrul = 402450;
    }

    [Script]
    class boss_gizrul_the_slavener : BossAI
    {
        public boss_gizrul_the_slavener(Creature creature) : base(creature, DataTypes.GizrulTheSlavener) { }

        public override void Reset()
        {
            _Reset();
        }

        public override void IsSummonedBy(WorldObject summoner)
        {
            me.GetMotionMaster().MovePath(PathIds.Gizrul, false);
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);

            _scheduler.Schedule(TimeSpan.FromSeconds(17), TimeSpan.FromSeconds(20), task =>
            {
                DoCastVictim(SpellIds.FatalBite);
                task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), task =>
            {
                DoCast(me, SpellIds.InfectedBite);
                task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10));
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

