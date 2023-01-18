// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockDepths.AmbassadorFlamelash
{
    struct SpellIds
    {
        public const uint Fireblast = 15573;
    }

    [Script]
    class boss_ambassador_flamelash : ScriptedAI
    {
        public boss_ambassador_flamelash(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void JustEngagedWith(Unit who)
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(2), task =>
            {
                DoCastVictim(SpellIds.Fireblast);
                task.Repeat(TimeSpan.FromSeconds(7));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(24), task =>
            {
                for (uint i = 0; i < 4; ++i)
                    SummonSpirit(me.GetVictim());
                task.Repeat(TimeSpan.FromSeconds(30));
            });
        }

        void SummonSpirit(Unit victim)
        {
            Creature spirit = DoSpawnCreature(9178, RandomHelper.FRand(-9, 9), RandomHelper.FRand(-9, 9), 0, 0, Framework.Constants.TempSummonType.TimedOrCorpseDespawn, TimeSpan.FromSeconds(60));
            if (spirit)
                spirit.GetAI().AttackStart(victim);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }
}

