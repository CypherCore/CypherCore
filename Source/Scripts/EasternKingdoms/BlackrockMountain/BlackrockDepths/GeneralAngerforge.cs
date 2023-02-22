// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockDepths.GeneralAngerforge
{
    internal struct SpellIds
    {
        public const uint Mightyblow = 14099;
        public const uint Hamstring = 9080;
        public const uint Cleave = 20691;
    }

    internal enum Phases
    {
        One = 1,
        Two = 2
    }

    [Script]
    internal class boss_general_angerforge : ScriptedAI
    {
        private Phases phase;

        public boss_general_angerforge(Creature creature) : base(creature)
        {
        }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void JustEngagedWith(Unit who)
        {
            phase = Phases.One;

            _scheduler.Schedule(TimeSpan.FromSeconds(8),
                                task =>
                                {
                                    DoCastVictim(SpellIds.Mightyblow);
                                    task.Repeat(TimeSpan.FromSeconds(18));
                                });

            _scheduler.Schedule(TimeSpan.FromSeconds(12),
                                task =>
                                {
                                    DoCastVictim(SpellIds.Hamstring);
                                    task.Repeat(TimeSpan.FromSeconds(15));
                                });

            _scheduler.Schedule(TimeSpan.FromSeconds(16),
                                task =>
                                {
                                    DoCastVictim(SpellIds.Cleave);
                                    task.Repeat(TimeSpan.FromSeconds(9));
                                });
        }

        public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            if (me.HealthBelowPctDamaged(20, damage) &&
                phase == Phases.One)
            {
                phase = Phases.Two;

                _scheduler.Schedule(TimeSpan.FromSeconds(0),
                                    task =>
                                    {
                                        for (byte i = 0; i < 2; ++i)
                                            SummonMedic(me.GetVictim());
                                    });

                _scheduler.Schedule(TimeSpan.FromSeconds(0),
                                    task =>
                                    {
                                        for (byte i = 0; i < 3; ++i)
                                            SummonAdd(me.GetVictim());

                                        task.Repeat(TimeSpan.FromSeconds(25));
                                    });
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }

        private void SummonAdd(Unit victim)
        {
            Creature SummonedAdd = DoSpawnCreature(8901, RandomHelper.IRand(-14, 14), RandomHelper.IRand(-14, 14), 0, 0, TempSummonType.TimedOrCorpseDespawn, TimeSpan.FromSeconds(120));

            if (SummonedAdd)
                SummonedAdd.GetAI().AttackStart(victim);
        }

        private void SummonMedic(Unit victim)
        {
            Creature SummonedMedic = DoSpawnCreature(8894, RandomHelper.IRand(-9, 9), RandomHelper.IRand(-9, 9), 0, 0, TempSummonType.TimedOrCorpseDespawn, TimeSpan.FromSeconds(120));

            if (SummonedMedic)
                SummonedMedic.GetAI().AttackStart(victim);
        }
    }
}