// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockDepths.HighInterrogatorGerstahn
{
    struct SpellIds
    {
        public const uint Shadowwordpain = 10894;
        public const uint Manaburn = 10876;
        public const uint Psychicscream = 8122;
        public const uint Shadowshield = 22417;
    }

    [Script]
    class boss_high_interrogator_gerstahn : ScriptedAI
    {
        public boss_high_interrogator_gerstahn(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void JustEngagedWith(Unit who)
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(4), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100.0f, true);
                if (target != null)
                    DoCast(target, SpellIds.Shadowwordpain);
                task.Repeat(TimeSpan.FromSeconds(7));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(14), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100.0f, true);
                if (target != null)
                    DoCast(target, SpellIds.Manaburn);
                task.Repeat(TimeSpan.FromSeconds(10));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(32), task =>
            {
                DoCastVictim(SpellIds.Psychicscream);
                task.Repeat(TimeSpan.FromSeconds(30));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(8), task =>
            {
                DoCast(me, SpellIds.Shadowshield);
                task.Repeat(TimeSpan.FromSeconds(25));
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

