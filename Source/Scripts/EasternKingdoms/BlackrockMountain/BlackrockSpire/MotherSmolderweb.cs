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
using Game.Spells;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.MotherSmolderweb
{
    struct SpellIds
    {
        public const uint Crystalize = 16104;
        public const uint Mothersmilk = 16468;
        public const uint SummonSpireSpiderling = 16103;
    }

    [Script]
    class boss_mother_smolderweb : BossAI
    {
        public boss_mother_smolderweb(Creature creature) : base(creature, DataTypes.MotherSmolderweb) { }

        public override void Reset()
        {
            _Reset();
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);
            _scheduler.Schedule(TimeSpan.FromSeconds(20), task =>
            {
                DoCast(me, SpellIds.Crystalize);
                task.Repeat(TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(10), task =>
            {
                DoCast(me, SpellIds.Mothersmilk);
                task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(12500));
            });
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
        }

        public override void DamageTaken(Unit done_by, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            if (me.GetHealth() <= damage)
                DoCast(me, SpellIds.SummonSpireSpiderling, new CastSpellExtraArgs(true));
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }
}

