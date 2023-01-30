// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore.Gehennas
{
    struct SpellIds
    {
        public const uint GehennasCurse = 19716;
        public const uint RainOfFire = 19717;
        public const uint ShadowBolt = 19728;
    }

    [Script]
    class boss_gehennas : BossAI
    {
        public boss_gehennas(Creature creature) : base(creature, DataTypes.Gehennas) { }

        public override void JustEngagedWith(Unit victim)
        {
            base.JustEngagedWith(victim);

            _scheduler.Schedule(TimeSpan.FromSeconds(12), task =>
            {
                DoCastVictim(SpellIds.GehennasCurse);
                task.Repeat(TimeSpan.FromSeconds(22), TimeSpan.FromSeconds(30));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(10), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0);
                if (target)
                    DoCast(target, SpellIds.RainOfFire);
                task.Repeat(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(12));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(6), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 1);
                if (target)
                    DoCast(target, SpellIds.ShadowBolt);
                task.Repeat(TimeSpan.FromSeconds(7));
            });
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }
}

