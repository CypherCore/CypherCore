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

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore.Magmadar
{
    struct SpellIds
    {
        public const uint Frenzy = 19451;
        public const uint MagmaSpit = 19449;
        public const uint Panic = 19408;
        public const uint LavaBomb = 19428;
    }

    struct TextIds
    {
        public const uint EmoteFrenzy = 0;
    }

    [Script]
    class boss_magmadar : BossAI
    {
        public boss_magmadar(Creature creature) : base(creature, BossIds.Magmadar) { }

        public override void Reset()
        {
            base.Reset();
            DoCast(me, SpellIds.MagmaSpit, new CastSpellExtraArgs(true));
        }

        public override void JustEngagedWith(Unit victim)
        {
            base.JustEngagedWith(victim);

            _scheduler.Schedule(TimeSpan.FromSeconds(30), task =>
            {
                Talk(TextIds.EmoteFrenzy);
                DoCast(me, SpellIds.Frenzy);
                task.Repeat(TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(20), task =>
            {
                DoCastVictim(SpellIds.Panic);
                task.Repeat(TimeSpan.FromSeconds(35));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(12), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true, true, -(int)SpellIds.LavaBomb);
                if (target)
                    DoCast(target, SpellIds.LavaBomb);
                task.Repeat(TimeSpan.FromSeconds(12));
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

