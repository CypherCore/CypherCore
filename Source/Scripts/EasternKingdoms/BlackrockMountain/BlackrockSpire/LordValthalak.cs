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

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire
{
    struct SpellIds
    {
        public const uint Frenzy = 8269;
        public const uint SummonSpectralAssassin = 27249;
        public const uint ShadowBoltVolley = 27382;
        public const uint ShadowWrath = 27286;
    }

    [Script]
    class boss_lord_valthalak : BossAI
    {
        bool frenzy40;
        bool frenzy15;

        public boss_lord_valthalak(Creature creature) : base(creature, DataTypes.LordValthalak)
        {
            Initialize();
        }

        void Initialize()
        {
            frenzy40 = false;
            frenzy15 = false;
        }

        public override void Reset()
        {
            _Reset();
            Initialize();
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);

            _scheduler.Schedule(TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(8), 1, task =>
            {
                DoCast(me, SpellIds.SummonSpectralAssassin);
                task.Repeat(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(9), TimeSpan.FromSeconds(18), task =>
            {
                DoCastVictim(SpellIds.ShadowWrath);
                task.Repeat(TimeSpan.FromSeconds(19), TimeSpan.FromSeconds(24));
            });
        }

        public override void JustDied(Unit killer)
        {
            instance.SetBossState(DataTypes.LordValthalak, EncounterState.Done);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            if (!frenzy40)
            {
                if (HealthBelowPct(40))
                {
                    DoCast(me, SpellIds.Frenzy);
                    _scheduler.CancelGroup(1);
                    frenzy40 = true;
                }
            }

            if (!frenzy15)
            {
                if (HealthBelowPct(15))
                {
                    DoCast(me, SpellIds.Frenzy);
                    _scheduler.Schedule(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(14), task =>
                    {
                        DoCastVictim(SpellIds.ShadowBoltVolley);
                        task.Repeat(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(6));
                    });
                    frenzy15 = true;
                }
            }

            DoMeleeAttackIfReady();
        }
    }
}

