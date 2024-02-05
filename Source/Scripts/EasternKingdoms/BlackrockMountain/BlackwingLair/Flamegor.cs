// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackwingLair.Flamegor
{
    struct SpellIds
    {
        public const uint Shadowflame = 22539;
        public const uint Wingbuffet = 23339;
        public const uint Frenzy = 23342;  //This spell periodically triggers fire nova
    }

    struct TextIds
    {
        public const uint EmoteFrenzy = 0;
    }

    [Script]
    class boss_flamegor : BossAI
    {
        public boss_flamegor(Creature creature) : base(creature, DataTypes.Flamegor) { }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);

            _scheduler.Schedule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20), task =>
            {
                DoCastVictim(SpellIds.Shadowflame);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(30), task =>
            {
                DoCastVictim(SpellIds.Wingbuffet);
                if (GetThreat(me.GetVictim()) != 0)
                    ModifyThreatByPercent(me.GetVictim(), -75);
                task.Repeat(TimeSpan.FromSeconds(30));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(10), task =>
            {
                Talk(TextIds.EmoteFrenzy);
                DoCast(me, SpellIds.Frenzy);
                task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10));
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

