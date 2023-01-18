// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.UrokDoomhowl
{
    struct SpellIds
    {
        public const uint Rend = 16509;
        public const uint Strike = 15580;
        public const uint IntimidatingRoar = 16508;
    }

    struct TextIds
    {
        public const uint SaySummon = 0;
        public const uint SayAggro = 1;
    }

    [Script]
    class boss_urok_doomhowl : BossAI
    {
        public boss_urok_doomhowl(Creature creature) : base(creature, DataTypes.UrokDoomhowl) { }

        public override void Reset()
        {
            _Reset();
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);

            _scheduler.Schedule(TimeSpan.FromSeconds(17), TimeSpan.FromSeconds(20), task =>
            {
                DoCastVictim(SpellIds.Rend);
                task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), task =>
            {
                DoCastVictim(SpellIds.Strike);
                task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10));
            });

            Talk(TextIds.SayAggro);
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
