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

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockCaverns.Beauty
{
    struct SpellIds
    {
        public const uint TerrifyingRoar = 76028; // Not yet Implemented
        public const uint BerserkerCharge = 76030;
        public const uint MagmaSpit = 76031;
        public const uint Flamebreak = 76032;
        public const uint Berserk = 82395; // Not yet Implemented
    }

    struct SoundIds
    {
        public const uint Aggro = 18559;
        public const uint Death = 18563;
    }

    [Script]
    class boss_beauty : BossAI
    {
        public boss_beauty(Creature creature) : base(creature, DataTypes.Beauty) { }

        public override void Reset()
        {
            _Reset();
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);

            _scheduler.Schedule(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(10), task =>
            {
                DoCast(SelectTarget(SelectTargetMethod.Random, 0, 100, true), SpellIds.MagmaSpit, new CastSpellExtraArgs(true));
                task.Repeat();
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(16), TimeSpan.FromSeconds(19), task =>
            {
                DoCast(SelectTarget(SelectTargetMethod.Random, 0, 100, true), SpellIds.BerserkerCharge, new CastSpellExtraArgs(true));
                task.Repeat();
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(18), TimeSpan.FromSeconds(22), task =>
            {
                DoCast(me, SpellIds.Flamebreak);
                task.Repeat();
            });

            DoPlaySoundToSet(me, SoundIds.Aggro);
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
            DoPlaySoundToSet(me, SoundIds.Death);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }
}

