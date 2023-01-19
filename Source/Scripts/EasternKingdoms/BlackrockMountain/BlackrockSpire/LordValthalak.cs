// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire
{
    struct SpellIds
    {
        public const uint Frenzy = 8269;
        public const uint SummonSpectralAssassin = 27249;
        public const uint ShadowBoltVolley = 27382;
        public const uint ShadowWrath = 27286;
    }

    [Script]
    class boss_lord_valthalak : BossAI
    {
        bool frenzy40;
        bool frenzy15;

        public boss_lord_valthalak(Creature creature) : base(creature, DataTypes.LordValthalak)
        {
            Initialize();
        }

        void Initialize()
        {
            frenzy40 = false;
            frenzy15 = false;
        }

        public override void Reset()
        {
            _Reset();
            Initialize();
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);

            _scheduler.Schedule(TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(8), 1, task =>
            {
                DoCast(me, SpellIds.SummonSpectralAssassin);
                task.Repeat(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(9), TimeSpan.FromSeconds(18), task =>
            {
                DoCastVictim(SpellIds.ShadowWrath);
                task.Repeat(TimeSpan.FromSeconds(19), TimeSpan.FromSeconds(24));
            });
        }

        public override void JustDied(Unit killer)
        {
            instance.SetBossState(DataTypes.LordValthalak, EncounterState.Done);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            if (!frenzy40)
            {
                if (HealthBelowPct(40))
                {
                    DoCast(me, SpellIds.Frenzy);
                    _scheduler.CancelGroup(1);
                    frenzy40 = true;
                }
            }

            if (!frenzy15)
            {
                if (HealthBelowPct(15))
                {
                    DoCast(me, SpellIds.Frenzy);
                    _scheduler.Schedule(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(14), task =>
                    {
                        DoCastVictim(SpellIds.ShadowBoltVolley);
                        task.Repeat(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(6));
                    });
                    frenzy15 = true;
                }
            }

            DoMeleeAttackIfReady();
        }
    }
}

