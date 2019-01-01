/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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

namespace Scripts.Northrend.Nexus.Nexus
{
    struct NexusCommandersConst
    {
        //Spells
        public const uint SpellBattleShout = 31403;
        public const uint SpellCharge = 60067;
        public const uint SpellFrighteningShout = 19134;
        public const uint SpellWhirlwind = 38618;
        public const uint SpellFrozenPrison = 47543;

        //Texts
        public const uint SayAggro = 0;
        public const uint SayKill = 1;
        public const uint SayDeath = 2;
    }

    [Script]
    class boss_nexus_commanders : BossAI
    {
        boss_nexus_commanders(Creature creature) : base(creature, DataTypes.Commander) { }

        public override void EnterCombat(Unit who)
        {
            _EnterCombat();
            Talk(NexusCommandersConst.SayAggro);
            me.RemoveAurasDueToSpell(NexusCommandersConst.SpellFrozenPrison);
            DoCast(me, NexusCommandersConst.SpellBattleShout);

            //Charge
            _scheduler.Schedule(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(4), task =>
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0, 100.0f, true);
                if (target)
                    DoCast(target, NexusCommandersConst.SpellCharge);

                task.Repeat(TimeSpan.FromSeconds(11), TimeSpan.FromSeconds(15));
            });

            //Whirlwind
            _scheduler.Schedule(TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(8), task =>
            {
                DoCast(me, NexusCommandersConst.SpellWhirlwind);
                task.Repeat(TimeSpan.FromSeconds(19.5), TimeSpan.FromSeconds(25));
            });

            //Frightening Shout
            _scheduler.Schedule(TimeSpan.FromSeconds(13), TimeSpan.FromSeconds(15), task =>
            {
                DoCastAOE(NexusCommandersConst.SpellFrighteningShout);
                task.Repeat(TimeSpan.FromSeconds(45), TimeSpan.FromSeconds(55));
            });
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
            Talk(NexusCommandersConst.SayDeath);
        }

        public override void KilledUnit(Unit who)
        {
            if (who.IsTypeId(TypeId.Player))
                Talk(NexusCommandersConst.SayKill);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            DoMeleeAttackIfReady();
        }
    }
}
