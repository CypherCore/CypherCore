// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.QuartermasterZigris
{
    struct SpellIds
    {
        public const uint Shoot = 16496;
        public const uint Stunbomb = 16497;
        public const uint HealingPotion = 15504;
        public const uint Hookednet = 15609;
    }

    [Script]
    class quartermaster_zigris : BossAI
    {
        public quartermaster_zigris(Creature creature) : base(creature, DataTypes.QuartermasterZigris) { }

        public override void Reset()
        {
            _Reset();
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);

            _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
            {
                DoCastVictim(SpellIds.Shoot);
                task.Repeat(TimeSpan.FromMilliseconds(500));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(16), task =>
            {
                DoCastVictim(SpellIds.Stunbomb);
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

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }
}
