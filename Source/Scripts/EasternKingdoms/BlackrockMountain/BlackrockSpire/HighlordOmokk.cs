// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.HighlordOmokk
{
    internal struct SpellIds
    {
        public const uint Frenzy = 8269;
        public const uint KnockAway = 10101;
    }

    [Script]
    internal class boss_highlord_omokk : BossAI
    {
        public boss_highlord_omokk(Creature creature) : base(creature, DataTypes.HighlordOmokk)
        {
        }

        public override void Reset()
        {
            _Reset();
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);

            _scheduler.Schedule(TimeSpan.FromSeconds(20),
                                task =>
                                {
                                    DoCastVictim(SpellIds.Frenzy);
                                    task.Repeat(TimeSpan.FromMinutes(1));
                                });

            _scheduler.Schedule(TimeSpan.FromSeconds(18),
                                task =>
                                {
                                    DoCastVictim(SpellIds.KnockAway);
                                    task.Repeat(TimeSpan.FromSeconds(12));
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