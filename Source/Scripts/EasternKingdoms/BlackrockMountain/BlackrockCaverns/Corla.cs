// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockCaverns.Corla
{
    struct SpellIds
    {
        public const uint Evolution = 75610;
        public const uint DrainEssense = 75645;
        public const uint ShadowPower = 35322;
        public const uint HShadowPower = 39193;
    }

    struct TextIds
    {
        public const uint YellAggro = 0;
        public const uint YellKill = 1;
        public const uint YellEvolvedZealot = 2;
        public const uint YellDeath = 3;

        public const uint EmoteEvolvedZealot = 4;
    }

    [Script]
    class boss_corla : BossAI
    {
        bool combatPhase;

        public boss_corla(Creature creature) : base(creature, DataTypes.Corla) { }

        public override void Reset()
        {
            _Reset();
            combatPhase = false;

            _scheduler.SetValidator(() => !combatPhase);
            _scheduler.Schedule(TimeSpan.FromSeconds(2), drainTask =>
            {
                DoCast(me, SpellIds.DrainEssense);
                drainTask.Schedule(TimeSpan.FromSeconds(15), stopDrainTask =>
                {
                    me.InterruptSpell(CurrentSpellTypes.Channeled);
                    stopDrainTask.Schedule(TimeSpan.FromSeconds(2), evolutionTask =>
                    {
                        DoCast(me, SpellIds.Evolution);
                        drainTask.Repeat(TimeSpan.FromSeconds(2));
                    });
                });
            });
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);
            Talk(TextIds.YellAggro);
            _scheduler.CancelAll();
            combatPhase = true;
        }

        public override void KilledUnit(Unit who)
        {
            if (who.IsPlayer())
                Talk(TextIds.YellKill);
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
            Talk(TextIds.YellDeath);
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);

            DoMeleeAttackIfReady();
        }
    }
}

