// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.ShadowHunterVoshgajin
{
    struct SpellIds
    {
        public const uint Curseofblood = 24673;
        public const uint Hex = 16708;
        public const uint Cleave = 20691;
    }

    [Script]
    class boss_shadow_hunter_voshgajin : BossAI
    {
        public boss_shadow_hunter_voshgajin(Creature creature) : base(creature, DataTypes.ShadowHunterVoshgajin) { }

        public override void Reset()
        {
            _Reset();
            //DoCast(me, SpellIcearmor, true);
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);

            _scheduler.Schedule(TimeSpan.FromSeconds(2), task =>
            {
                DoCastVictim(SpellIds.Curseofblood);
                task.Repeat(TimeSpan.FromSeconds(45));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(8), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);
                if (target)
                    DoCast(target, SpellIds.Hex);
                task.Repeat(TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(14), task =>
            {
                DoCastVictim(SpellIds.Cleave);
                task.Repeat(TimeSpan.FromSeconds(7));
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

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }
}

