// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.Thebeast
{
    struct SpellIds
    {
        public const uint Flamebreak = 16785;
        public const uint Immolate = 20294;
        public const uint Terrifyingroar = 14100;
    }

    [Script]
    class boss_thebeast : BossAI
    {
        public boss_thebeast(Creature creature) : base(creature, DataTypes.TheBeast) { }

        public override void Reset()
        {
            _Reset();
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);

            _scheduler.Schedule(TimeSpan.FromSeconds(12), task =>
            {
                DoCastVictim(SpellIds.Flamebreak);
                task.Repeat(TimeSpan.FromSeconds(10));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(3), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);
                if (target)
                    DoCast(target, SpellIds.Immolate);
                task.Repeat(TimeSpan.FromSeconds(8));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(23), task =>
            {
                DoCastVictim(SpellIds.Terrifyingroar);
                task.Repeat(TimeSpan.FromSeconds(20));
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

