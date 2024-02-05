// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;

namespace Scripts.EasternKingdoms.AlteracValley.Drekthar
{
    struct SpellIds
    {
        public const uint Whirlwind = 15589;
        public const uint Whirlwind2 = 13736;
        public const uint Knockdown = 19128;
        public const uint Frenzy = 8269;
        public const uint SweepingStrikes = 18765; // not sure
        public const uint Cleave = 20677; // not sure
        public const uint Windfury = 35886; // not sure
        public const uint Stormpike = 51876;  // not sure
    }

    struct TextIds
    {
        public const uint SayAggro = 0;
        public const uint SayEvade = 1;
        public const uint SayRespawn = 2;
        public const uint SayRandom = 3;
    }

    [Script]
    class boss_drekthar : ScriptedAI
    {
        public boss_drekthar(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void JustEngagedWith(Unit who)
        {
            Talk(TextIds.SayAggro);
            _scheduler.Schedule(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(20), task =>
            {
                DoCastVictim(SpellIds.Whirlwind);
                task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(18));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(20), task =>
            {
                DoCastVictim(SpellIds.Whirlwind2);
                task.Repeat(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(25));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(12), task =>
            {
                DoCastVictim(SpellIds.Knockdown);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(6), task =>
            {
                DoCastVictim(SpellIds.Frenzy);
                task.Repeat(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(30));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(30), task =>
            {
                Talk(TextIds.SayRandom);
                task.Repeat();
            });
        }

        public override void JustAppeared()
        {
            Reset();
            Talk(TextIds.SayRespawn);
        }

        public override bool CheckInRoom()
        {
            if (me.GetDistance2d(me.GetHomePosition().GetPositionX(), me.GetHomePosition().GetPositionY()) > 50)
            {
                EnterEvadeMode();
                Talk(TextIds.SayEvade);
                return false;
            }

            return true;
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim() || !CheckInRoom())
                return;

            _scheduler.Update(diff);
        }
    }
}