// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.Drakkisath
{
    struct SpellIds
    {
        public const uint Firenova = 23462;
        public const uint Cleave = 20691;
        public const uint Confliguration = 16805;
        public const uint Thunderclap = 15548; //Not sure if right Id. 23931 would be a harder possibility.
    }

    [Script]
    class boss_drakkisath : BossAI
    {
        public boss_drakkisath(Creature creature) : base(creature, DataTypes.GeneralDrakkisath) { }

        public override void Reset()
        {
            _Reset();
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);
            _scheduler.Schedule(TimeSpan.FromSeconds(6), task =>
            {
                DoCastVictim(SpellIds.Firenova);
                task.Repeat(TimeSpan.FromSeconds(10));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(8), task =>
            {
                DoCastVictim(SpellIds.Cleave);
                task.Repeat(TimeSpan.FromSeconds(8));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(15), task =>
            {
                DoCastVictim(SpellIds.Confliguration);
                task.Repeat(TimeSpan.FromSeconds(18));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(17), task =>
            {
                DoCastVictim(SpellIds.Thunderclap);
                task.Repeat(TimeSpan.FromSeconds(20));
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

            _scheduler.Update(diff);
        }
    }
}
