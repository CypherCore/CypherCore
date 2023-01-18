// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockCaverns.KarshSteelbender
{
    struct SpellIds
    {
        public const uint Cleave = 15284;
        public const uint QuicksilverArmor = 75842;
        public const uint SuperheatedQuicksilverArmor = 75846;
    }

    struct TextIds
    {
        public const uint YellAggro = 0;
        public const uint YellKill = 1;
        public const uint YellQuicksilverArmor = 2;
        public const uint YellDeath = 3;

        public const uint EmoteQuicksilverArmor = 4;
    }

    [Script]
    class boss_karsh_steelbender : BossAI
    {
        public boss_karsh_steelbender(Creature creature) : base(creature, DataTypes.KarshSteelbender) { }

        public override void Reset()
        {
            _Reset();
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);
            Talk(TextIds.YellAggro);

            _scheduler.Schedule(TimeSpan.FromSeconds(10), task =>
            {
                DoCastVictim(SpellIds.Cleave);
                task.Repeat(TimeSpan.FromSeconds(10));
            });
        }

        public override void KilledUnit(Unit who)
        {
            if (who.IsPlayer())
                Talk(TextIds.YellKill);
        }

        public override void JustDied(Unit victim)
        {
            _JustDied();
            Talk(TextIds.YellDeath);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }
}

