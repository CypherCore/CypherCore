// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.WarmasterVoone
{
    struct SpellIds
    {
        public const uint Snapkick = 15618;
        public const uint Cleave = 15284;
        public const uint Uppercut = 10966;
        public const uint Mortalstrike = 16856;
        public const uint Pummel = 15615;
        public const uint Throwaxe = 16075;
    }

    [Script]
    class boss_warmaster_voone : BossAI
    {
        public boss_warmaster_voone(Creature creature) : base(creature, DataTypes.WarmasterVoone) { }

        public override void Reset()
        {
            _Reset();
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);

            _scheduler.Schedule(TimeSpan.FromSeconds(8), task =>
            {
                DoCastVictim(SpellIds.Snapkick);
                task.Repeat(TimeSpan.FromSeconds(6));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(14), task =>
            {
                DoCastVictim(SpellIds.Cleave);
                task.Repeat(TimeSpan.FromSeconds(12));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(20), task =>
            {
                DoCastVictim(SpellIds.Uppercut);
                task.Repeat(TimeSpan.FromSeconds(14));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(12), task =>
            {
                DoCastVictim(SpellIds.Mortalstrike);
                task.Repeat(TimeSpan.FromSeconds(10));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(32), task =>
            {
                DoCastVictim(SpellIds.Pummel);
                task.Repeat(TimeSpan.FromSeconds(16));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
            {
                DoCastVictim(SpellIds.Throwaxe);
                task.Repeat(TimeSpan.FromSeconds(8));
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

            _scheduler.Update(diff);
        }
    }
}

