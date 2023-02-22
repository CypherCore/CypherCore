// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.MotherSmolderweb
{
    internal struct SpellIds
    {
        public const uint Crystalize = 16104;
        public const uint Mothersmilk = 16468;
        public const uint SummonSpireSpiderling = 16103;
    }

    [Script]
    internal class boss_mother_smolderweb : BossAI
    {
        public boss_mother_smolderweb(Creature creature) : base(creature, DataTypes.MotherSmolderweb)
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
                                    DoCast(me, SpellIds.Crystalize);
                                    task.Repeat(TimeSpan.FromSeconds(15));
                                });

            _scheduler.Schedule(TimeSpan.FromSeconds(10),
                                task =>
                                {
                                    DoCast(me, SpellIds.Mothersmilk);
                                    task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(12500));
                                });
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
        }

        public override void DamageTaken(Unit done_by, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            if (me.GetHealth() <= damage)
                DoCast(me, SpellIds.SummonSpireSpiderling, new CastSpellExtraArgs(true));
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }
}