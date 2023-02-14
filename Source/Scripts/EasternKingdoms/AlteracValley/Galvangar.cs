// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.AlteracValley.Galvangar
{
    internal struct SpellIds
    {
        public const uint Cleave = 15284;
        public const uint FrighteningShout = 19134;
        public const uint Whirlwind1 = 15589;
        public const uint Whirlwind2 = 13736;
        public const uint MortalStrike = 16856;
    }

    internal struct TextIds
    {
        public const uint SayAggro = 0;
        public const uint SayEvade = 1;
        public const uint SayBuff = 2;
    }

    internal struct ActionIds
    {
        public const int BuffYell = -30001; // shared from Battleground
    }

    [Script]
    internal class boss_galvangar : ScriptedAI
    {
        public boss_galvangar(Creature creature) : base(creature)
        {
        }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void JustEngagedWith(Unit who)
        {
            Talk(TextIds.SayAggro);

            _scheduler.Schedule(TimeSpan.FromSeconds(1),
                                TimeSpan.FromSeconds(9),
                                task =>
                                {
                                    DoCastVictim(SpellIds.Cleave);
                                    task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(16));
                                });

            _scheduler.Schedule(TimeSpan.FromSeconds(2),
                                TimeSpan.FromSeconds(19),
                                task =>
                                {
                                    DoCastVictim(SpellIds.FrighteningShout);
                                    task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15));
                                });

            _scheduler.Schedule(TimeSpan.FromSeconds(1),
                                TimeSpan.FromSeconds(13),
                                task =>
                                {
                                    DoCastVictim(SpellIds.Whirlwind1);
                                    task.Repeat(TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(10));
                                });

            _scheduler.Schedule(TimeSpan.FromSeconds(5),
                                TimeSpan.FromSeconds(20),
                                task =>
                                {
                                    DoCastVictim(SpellIds.Whirlwind2);
                                    task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(25));
                                });

            _scheduler.Schedule(TimeSpan.FromSeconds(5),
                                TimeSpan.FromSeconds(20),
                                task =>
                                {
                                    DoCastVictim(SpellIds.MortalStrike);
                                    task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
                                });
        }

        public override void DoAction(int actionId)
        {
            if (actionId == ActionIds.BuffYell)
                Talk(TextIds.SayBuff);
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
            if (!UpdateVictim() ||
                !CheckInRoom())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }
}