// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;

namespace Scripts.EasternKingdoms.AlteracValley.Vanndar
{
    struct SpellIds
    {
        public const uint Avatar = 19135;
        public const uint Thunderclap = 15588;
        public const uint Stormbolt = 20685; // not sure
    }

    struct TextIds
    {
        public const uint YellAggro = 0;
        public const uint YellEvade = 1;
        //public const uint YellRespawn1                                 = -1810010; // Missing in database
        //public const uint YellRespawn2                                 = -1810011; // Missing in database
        public const uint YellRandom = 2;
        public const uint YellSpell = 3;
    }

    [Script]
    class boss_vanndar : ScriptedAI
    {
        public boss_vanndar(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void JustEngagedWith(Unit who)
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(3), task =>
            {
                DoCastVictim(SpellIds.Avatar);
                task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(20));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(4), task =>
            {
                DoCastVictim(SpellIds.Thunderclap);
                task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(6), task =>
            {
                DoCastVictim(SpellIds.Stormbolt);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(25));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(30), task =>
            {
                Talk(TextIds.YellRandom);
                task.Repeat(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(30));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(5), task =>
            {
                if (me.GetDistance2d(me.GetHomePosition().GetPositionX(), me.GetHomePosition().GetPositionY()) > 50)
                {
                    EnterEvadeMode();
                    Talk(TextIds.YellEvade);
                }
                task.Repeat();
            });

            Talk(TextIds.YellAggro);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }
}

