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

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockCaverns.Corla
{
    struct SpellIds
    {
        public const uint Evolution = 75610;
        public const uint DrainEssense = 75645;
        public const uint ShadowPower = 35322;
        public const uint HShadowPower = 39193;
    }

    struct TextIds
    {
        public const uint YellAggro = 0;
        public const uint YellKill = 1;
        public const uint YellEvolvedZealot = 2;
        public const uint YellDeath = 3;

        public const uint EmoteEvolvedZealot = 4;
    }

    [Script]
    class boss_corla : BossAI
    {
        bool combatPhase;

        public boss_corla(Creature creature) : base(creature, DataTypes.Corla) { }

        public override void Reset()
        {
            _Reset();
            combatPhase = false;

            _scheduler.SetValidator(() => !combatPhase);
            _scheduler.Schedule(TimeSpan.FromSeconds(2), drainTask =>
            {
                DoCast(me, SpellIds.DrainEssense);
                drainTask.Schedule(TimeSpan.FromSeconds(15), stopDrainTask =>
                {
                    me.InterruptSpell(CurrentSpellTypes.Channeled);
                    stopDrainTask.Schedule(TimeSpan.FromSeconds(2), evolutionTask =>
                    {
                        DoCast(me, SpellIds.Evolution);
                        drainTask.Repeat(TimeSpan.FromSeconds(2));
                    });
                });
            });
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);
            Talk(TextIds.YellAggro);
            _scheduler.CancelAll();
            combatPhase = true;
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
            _scheduler.Update(diff);

            DoMeleeAttackIfReady();
        }
    }
}

