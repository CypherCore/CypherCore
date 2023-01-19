// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;

namespace Scripts.EasternKingdoms.Karazhan.MaidenOfVirtue
{
    struct SpellIds
    {
        public const uint Repentance = 29511;
        public const uint Holyfire = 29522;
        public const uint Holywrath = 32445;
        public const uint Holyground = 29523;
        public const uint Berserk = 26662;
    }

    struct TextIds
    {
        public const uint SayAggro = 0;
        public const uint SaySlay = 1;
        public const uint SayRepentance = 2;
        public const uint SayDeath = 3;
    }

    [Script]
    class boss_maiden_of_virtue : BossAI
    {
        public boss_maiden_of_virtue(Creature creature) : base(creature, DataTypes.MaidenOfVirtue) { }

        public override void KilledUnit(Unit Victim)
        {
            if (RandomHelper.randChance(50))
                Talk(TextIds.SaySlay);
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.SayDeath);
            _JustDied();
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);
            Talk(TextIds.SayAggro);

            DoCastSelf(SpellIds.Holyground, new CastSpellExtraArgs(true));
            _scheduler.Schedule(TimeSpan.FromSeconds(33), TimeSpan.FromSeconds(45), task =>
            {
                DoCastVictim(SpellIds.Repentance);
                Talk(TextIds.SayRepentance);
                task.Repeat(TimeSpan.FromSeconds(35));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(8), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 50, true);
                if (target)
                    DoCast(target, SpellIds.Holyfire);
                task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(19));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(25), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 80, true);
                if (target)
                    DoCast(target, SpellIds.Holywrath);
                task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(25));
            });
            _scheduler.Schedule(TimeSpan.FromMinutes(10), task =>
            {
                DoCastSelf(SpellIds.Berserk, new CastSpellExtraArgs(true));
            });
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }
}