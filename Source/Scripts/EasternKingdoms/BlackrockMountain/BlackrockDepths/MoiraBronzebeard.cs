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

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockDepths.MoiraBronzebeard
{
    struct SpellIds
    {
        public const uint Heal = 10917;
        public const uint Renew = 10929;
        public const uint Shield = 10901;
        public const uint Mindblast = 10947;
        public const uint Shadowwordpain = 10894;
        public const uint Smite = 10934;
    }

    [Script]
    class boss_moira_bronzebeard : ScriptedAI
    {
        public boss_moira_bronzebeard(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void JustEngagedWith(Unit who)
        {
            //_scheduler.Schedule(EventHeal, TimeSpan.FromSeconds(12s)); // not used atm // These times are probably wrong
            _scheduler.Schedule(TimeSpan.FromSeconds(16), task =>
            {
                DoCastVictim(SpellIds.Mindblast);
                task.Repeat(TimeSpan.FromSeconds(14));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(2), task =>
            {
                DoCastVictim(SpellIds.Shadowwordpain);
                task.Repeat(TimeSpan.FromSeconds(18));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(8), task =>
            {
                DoCastVictim(SpellIds.Smite);
                task.Repeat(TimeSpan.FromSeconds(10));
            });
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);
        }
    }
}

