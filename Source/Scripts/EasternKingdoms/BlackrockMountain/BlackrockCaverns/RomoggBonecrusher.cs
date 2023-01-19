// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockCaverns.RomoggBonecrusher
{
    struct SpellIds
    {
        public const uint CallForHelp = 82137; // Needs Scripting
        public const uint ChainsOfWoe = 75539;
        public const uint Quake = 75272;
        public const uint Skullcracker = 75543;
        public const uint WoundingStrike = 75571;
    }

    struct TextIds
    {
        public const uint YellAggro = 0;
        public const uint YellKill = 1;
        public const uint YellSkullcracker = 2;
        public const uint YellDeath = 3;
        
        public const uint EmoteCallForHelp = 4;
        public const uint EmoteSkullcracker = 5;
    }

    struct MiscConst
    {
        public const uint TypeRaz = 1;
        public const uint DataRomoggDead = 1;
        public static Position SummonPos = new Position(249.2639f, 949.1614f, 191.7866f, 3.141593f);
    }

    [Script]
    class boss_romogg_bonecrusher : BossAI
    {
        public boss_romogg_bonecrusher(Creature creature) : base(creature, DataTypes.RomoggBonecrusher)
        {
            me.SummonCreature(CreatureIds.RazTheCrazed, MiscConst.SummonPos, TempSummonType.ManualDespawn, TimeSpan.FromSeconds(200));
        }

        public override void Reset()
        {
            _Reset();
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
            Talk(TextIds.YellDeath);

            Creature raz = instance.GetCreature(DataTypes.RazTheCrazed);
            if (raz)
                raz.GetAI().SetData(MiscConst.TypeRaz, MiscConst.DataRomoggDead);
        }

        public override void KilledUnit(Unit who)
        {
            if (who.IsPlayer())
                Talk(TextIds.YellKill);
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);

            _scheduler.Schedule(TimeSpan.FromSeconds(22), TimeSpan.FromSeconds(32), task =>
            {
                Talk(TextIds.YellSkullcracker);
                DoCast(me, SpellIds.ChainsOfWoe);
                task.Repeat(TimeSpan.FromSeconds(22), TimeSpan.FromSeconds(32));
                _scheduler.Schedule(TimeSpan.FromSeconds(3), skullCrackerTask =>
                {
                    Talk(TextIds.EmoteSkullcracker);
                    DoCast(me, SpellIds.Skullcracker);
                });
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(26), TimeSpan.FromSeconds(32), task =>
            {
                DoCastVictim(SpellIds.WoundingStrike, new CastSpellExtraArgs(true));
                task.Repeat(TimeSpan.FromSeconds(26), TimeSpan.FromSeconds(32));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(45), task =>
            {
                DoCast(me, SpellIds.Quake);
                task.Repeat(TimeSpan.FromSeconds(32), TimeSpan.FromSeconds(40));
            });

            Talk(TextIds.YellAggro);
            Talk(TextIds.EmoteCallForHelp);
            DoCast(me, SpellIds.CallForHelp);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }
}

