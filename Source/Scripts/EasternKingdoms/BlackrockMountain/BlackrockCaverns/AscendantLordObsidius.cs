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
using Game.Spells;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockCaverns.AscendantLordObsidius
{
    struct SpellIds
    {
        public const uint ManaTap = 36021;
        public const uint ArcaneTorrent = 36022;
        public const uint Domination = 35280;
    }

    struct TextIds
    {
        public const uint YellAggro = 0;
        public const uint YellKill = 1;
        public const uint YellSwitchingShadows = 2;
        public const uint YellDeath = 3;

        public const uint EmoteSwitchingShadows = 4;
    }

    [Script]
    class boss_ascendant_lord_obsidius : BossAI
    {
        public boss_ascendant_lord_obsidius(Creature creature) : base(creature, DataTypes.AscendantLordObsidius) { }

        public override void Reset()
        {
            _Reset();
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);

            _scheduler.Schedule(TimeSpan.FromSeconds(30), ScheduleTasks =>
            {
                DoCastVictim(SpellIds.ManaTap, new CastSpellExtraArgs(true));
                ScheduleTasks.Repeat(TimeSpan.FromSeconds(14), TimeSpan.FromSeconds(22));
            });

            Talk(TextIds.YellAggro);
        }

        public override void KilledUnit(Unit who)
        {
            if (who.IsPlayer())
                Talk(TextIds.YellKill);
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
            Talk(TextIds.YellDeath);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }
}

