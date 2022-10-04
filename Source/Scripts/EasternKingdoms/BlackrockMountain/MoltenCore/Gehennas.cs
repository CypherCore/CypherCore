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

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore.Gehennas
{
    struct SpellIds
    {
        public const uint GehennasCurse = 19716;
        public const uint RainOfFire = 19717;
        public const uint ShadowBolt = 19728;
    }

    [Script]
    class boss_gehennas : BossAI
    {
        public boss_gehennas(Creature creature) : base(creature, DataTypes.Gehennas) { }

        public override void JustEngagedWith(Unit victim)
        {
            base.JustEngagedWith(victim);

            _scheduler.Schedule(TimeSpan.FromSeconds(12), task =>
            {
                DoCastVictim(SpellIds.GehennasCurse);
                task.Repeat(TimeSpan.FromSeconds(22), TimeSpan.FromSeconds(30));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(10), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0);
                if (target)
                    DoCast(target, SpellIds.RainOfFire);
                task.Repeat(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(12));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(6), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 1);
                if (target)
                    DoCast(target, SpellIds.ShadowBolt);
                task.Repeat(TimeSpan.FromSeconds(7));
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

