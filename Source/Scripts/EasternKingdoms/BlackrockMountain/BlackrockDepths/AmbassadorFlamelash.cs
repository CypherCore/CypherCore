// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockDepths.AmbassadorFlamelash
{
    internal struct SpellIds
    {
        public const uint Fireblast = 15573;
    }

    [Script]
    internal class boss_ambassador_flamelash : ScriptedAI
    {
        public boss_ambassador_flamelash(Creature creature) : base(creature)
        {
        }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void JustEngagedWith(Unit who)
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(2),
                                task =>
                                {
                                    DoCastVictim(SpellIds.Fireblast);
                                    task.Repeat(TimeSpan.FromSeconds(7));
                                });

            _scheduler.Schedule(TimeSpan.FromSeconds(24),
                                task =>
                                {
                                    for (uint i = 0; i < 4; ++i)
                                        SummonSpirit(me.GetVictim());

                                    task.Repeat(TimeSpan.FromSeconds(30));
                                });
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }

        private void SummonSpirit(Unit victim)
        {
            Creature spirit = DoSpawnCreature(9178, RandomHelper.FRand(-9, 9), RandomHelper.FRand(-9, 9), 0, 0, TempSummonType.TimedOrCorpseDespawn, TimeSpan.FromSeconds(60));

            if (spirit)
                spirit.GetAI().AttackStart(victim);
        }
    }
}