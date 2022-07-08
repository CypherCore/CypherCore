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

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackwingLair.Firemaw
{
    struct SpellIds
    {
        public const uint Shadowflame = 22539;
        public const uint Wingbuffet = 23339;
        public const uint Flamebuffet = 23341;
    }

    [Script]
    class boss_firemaw : BossAI
    {
        public boss_firemaw(Creature creature) : base(creature, DataTypes.Firemaw) { }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);

            _scheduler.Schedule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20), task =>
            {
                DoCastVictim(SpellIds.Shadowflame);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(30), task =>
            {
                DoCastVictim(SpellIds.Wingbuffet);
                if (GetThreat(me.GetVictim()) != 0)
                    ModifyThreatByPercent(me.GetVictim(), -75);
                task.Repeat(TimeSpan.FromSeconds(30));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(5), task =>
            {
                DoCastVictim(SpellIds.Flamebuffet);
                task.Repeat(TimeSpan.FromSeconds(5));
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

