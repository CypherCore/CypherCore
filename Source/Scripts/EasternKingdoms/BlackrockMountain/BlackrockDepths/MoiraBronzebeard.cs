// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockDepths.MoiraBronzebeard
{
    struct SpellIds
    {
        public const uint Heal = 10917;
        public const uint Renew = 10929;
        public const uint Shield = 10901;
        public const uint Mindblast = 10947;
        public const uint Shadowwordpain = 10894;
        public const uint Smite = 10934;
    }

    [Script]
    class boss_moira_bronzebeard : ScriptedAI
    {
        public boss_moira_bronzebeard(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void JustEngagedWith(Unit who)
        {
            //_scheduler.Schedule(EventHeal, TimeSpan.FromSeconds(12s)); // not used atm // These times are probably wrong
            _scheduler.Schedule(TimeSpan.FromSeconds(16), task =>
            {
                DoCastVictim(SpellIds.Mindblast);
                task.Repeat(TimeSpan.FromSeconds(14));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(2), task =>
            {
                DoCastVictim(SpellIds.Shadowwordpain);
                task.Repeat(TimeSpan.FromSeconds(18));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(8), task =>
            {
                DoCastVictim(SpellIds.Smite);
                task.Repeat(TimeSpan.FromSeconds(10));
            });
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);
        }
    }
}

