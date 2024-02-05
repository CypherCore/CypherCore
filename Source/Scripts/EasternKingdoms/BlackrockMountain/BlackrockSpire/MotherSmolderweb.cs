// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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

            _scheduler.Update(diff);
        }
    }
}
