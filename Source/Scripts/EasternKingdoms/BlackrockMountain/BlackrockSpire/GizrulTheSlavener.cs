// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.GizrulTheSlavener
{
    struct SpellIds
    {
        public const uint FatalBite = 16495;
        public const uint InfectedBite = 16128;
        public const uint Frenzy = 8269;
    }

    struct PathIds
    {
        public const uint Gizrul = 402450;
    }

    [Script]
    class boss_gizrul_the_slavener : BossAI
    {
        public boss_gizrul_the_slavener(Creature creature) : base(creature, DataTypes.GizrulTheSlavener) { }

        public override void Reset()
        {
            _Reset();
        }

        public override void IsSummonedBy(WorldObject summoner)
        {
            me.GetMotionMaster().MovePath(PathIds.Gizrul, false);
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);

            _scheduler.Schedule(TimeSpan.FromSeconds(17), TimeSpan.FromSeconds(20), task =>
            {
                DoCastVictim(SpellIds.FatalBite);
                task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), task =>
            {
                DoCast(me, SpellIds.InfectedBite);
                task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10));
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

