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

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.OverlordWyrmthalak
{
    struct SpellIds
    {
        public const uint Blastwave = 11130;
        public const uint Shout = 23511;
        public const uint Cleave = 20691;
        public const uint Knockaway = 20686;
    }

    struct MiscConst
    {
        public const uint NpcSpirestoneWarlord = 9216;
        public const uint NpcSmolderthornBerserker = 9268;

        public static Position SummonLocation1 = new Position(-39.355f, -513.456f, 88.472f, 4.679f);
        public static Position SummonLocation2 = new Position(-49.875f, -511.896f, 88.195f, 4.613f);
    }

    [Script]
    class boss_overlord_wyrmthalak : BossAI
    {
        bool Summoned;

        public boss_overlord_wyrmthalak(Creature creature) : base(creature, DataTypes.OverlordWyrmthalak)
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

            _scheduler.Schedule(TimeSpan.FromSeconds(20), task =>
            {
                DoCastVictim(SpellIds.Blastwave);
                task.Repeat(TimeSpan.FromSeconds(20));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(2), task =>
            {
                DoCastVictim(SpellIds.Shout);
                task.Repeat(TimeSpan.FromSeconds(10));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(6), task =>
            {
                DoCastVictim(SpellIds.Cleave);
                task.Repeat(TimeSpan.FromSeconds(7));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(12), task =>
            {
                DoCastVictim(SpellIds.Knockaway);
                task.Repeat(TimeSpan.FromSeconds(14));
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

            if (!Summoned && HealthBelowPct(51))
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);
                if (target)
                {
                    Creature warlord = me.SummonCreature(MiscConst.NpcSpirestoneWarlord, MiscConst.SummonLocation1, TempSummonType.TimedDespawn, TimeSpan.FromMinutes(5));
                    if (warlord)
                        warlord.GetAI().AttackStart(target);
                    Creature berserker = me.SummonCreature(MiscConst.NpcSmolderthornBerserker, MiscConst.SummonLocation2, TempSummonType.TimedDespawn, TimeSpan.FromMinutes(5));
                    if (berserker)
                        berserker.GetAI().AttackStart(target);
                    Summoned = true;
                }
            }

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }
}

