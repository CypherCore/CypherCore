// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;
using System.Collections.Generic;

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore.Sulfuron
{
    struct SpellIds
    {
        // Sulfuron Harbringer
        public const uint DarkStrike = 19777;
        public const uint DemoralizingShout = 19778;
        public const uint Inspire = 19779;
        public const uint Knockdown = 19780;
        public const uint Flamespear = 19781;

        // Adds
        public const uint Heal = 19775;
        public const uint Shadowwordpain = 19776;
        public const uint Immolate = 20294;
    }

    [Script]
    class boss_sulfuron : BossAI
    {
        public boss_sulfuron(Creature creature) : base(creature, DataTypes.SulfuronHarbinger) { }

        public override void JustEngagedWith(Unit victim)
        {
            base.JustEngagedWith(victim);

            _scheduler.Schedule(TimeSpan.FromSeconds(10), task =>
            {
                DoCast(me, SpellIds.DarkStrike);
                task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(18));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(15), task =>
            {
                DoCastVictim(SpellIds.DemoralizingShout);
                task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(20));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(13), task =>
            {
                List<Creature> healers = DoFindFriendlyMissingBuff(45.0f, SpellIds.Inspire);
                if (!healers.Empty())
                    DoCast(healers.SelectRandom(), SpellIds.Inspire);

                DoCast(me, SpellIds.Inspire);
                task.Repeat(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(26));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(6), task =>
            {
                DoCastVictim(SpellIds.Knockdown);
                task.Repeat(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(2), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);
                if (target)
                    DoCast(target, SpellIds.Flamespear);
                task.Repeat(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(16));
            });
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }

    [Script]
    class npc_flamewaker_priest : ScriptedAI
    {
        public npc_flamewaker_priest(Creature creature) : base(creature)
        {
        }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void JustDied(Unit killer)
        {
            _scheduler.CancelAll();
        }

        public override void JustEngagedWith(Unit victim)
        {
            base.JustEngagedWith(victim);

            _scheduler.Schedule(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30), task =>
            {
                Unit target = DoSelectLowestHpFriendly(60.0f, 1);
                if (target)
                    DoCast(target, SpellIds.Heal);
                task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(20));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(2), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true, true, -(int)SpellIds.Shadowwordpain);
                if (target)
                    DoCast(target, SpellIds.Shadowwordpain);
                task.Repeat(TimeSpan.FromSeconds(18), TimeSpan.FromSeconds(26));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(8), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true, true, -(int)SpellIds.Immolate);
                if (target)
                    DoCast(target, SpellIds.Immolate);
                task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(25));
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

