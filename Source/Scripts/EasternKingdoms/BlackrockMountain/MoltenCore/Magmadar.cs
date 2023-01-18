// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore.Magmadar
{
    struct SpellIds
    {
        public const uint Frenzy = 19451;
        public const uint MagmaSpit = 19449;
        public const uint Panic = 19408;
        public const uint LavaBomb = 19428;
    }

    struct TextIds
    {
        public const uint EmoteFrenzy = 0;
    }

    [Script]
    class boss_magmadar : BossAI
    {
        public boss_magmadar(Creature creature) : base(creature, DataTypes.Magmadar) { }

        public override void Reset()
        {
            base.Reset();
            DoCast(me, SpellIds.MagmaSpit, new CastSpellExtraArgs(true));
        }

        public override void JustEngagedWith(Unit victim)
        {
            base.JustEngagedWith(victim);

            _scheduler.Schedule(TimeSpan.FromSeconds(30), task =>
            {
                Talk(TextIds.EmoteFrenzy);
                DoCast(me, SpellIds.Frenzy);
                task.Repeat(TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(20), task =>
            {
                DoCastVictim(SpellIds.Panic);
                task.Repeat(TimeSpan.FromSeconds(35));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(12), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true, true, -(int)SpellIds.LavaBomb);
                if (target)
                    DoCast(target, SpellIds.LavaBomb);
                task.Repeat(TimeSpan.FromSeconds(12));
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

