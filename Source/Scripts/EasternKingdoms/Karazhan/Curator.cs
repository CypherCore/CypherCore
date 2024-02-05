// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;

namespace Scripts.EasternKingdoms.Karazhan.Curator
{
    struct SpellIds
    {
        public const uint HatefulBolt = 30383;
        public const uint Evocation = 30254;
        public const uint ArcaneInfusion = 30403;
        public const uint Berserk = 26662;
        public const uint SummonAstralFlareNe = 30236;
        public const uint SummonAstralFlareNw = 30239;
        public const uint SummonAstralFlareSe = 30240;
        public const uint SummonAstralFlareSw = 30241;
    }

    struct TextIds
    {
        public const uint SayAggro = 0;
        public const uint SaySummon = 1;
        public const uint SayEvocate = 2;
        public const uint SayEnrage = 3;
        public const uint SayKill = 4;
        public const uint SayDeath = 5;
    }

    struct MiscConst
    {
        public const uint GroupAstralFlare = 1;
    }

    [Script]
    class boss_curator : BossAI
    {
        public boss_curator(Creature creature) : base(creature, DataTypes.Curator) { }

        public override void Reset()
        {
            _Reset();
            _infused = false;
        }

        public override void KilledUnit(Unit victim)
        {
            if (victim.IsPlayer())
                Talk(TextIds.SayKill);
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
            Talk(TextIds.SayDeath);
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);
            Talk(TextIds.SayAggro);

            _scheduler.Schedule(TimeSpan.FromSeconds(12), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.MaxThreat, 1);
                if (target != null)
                    DoCast(target, SpellIds.HatefulBolt);
                task.Repeat(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(10), MiscConst.GroupAstralFlare, task =>
            {
                if (RandomHelper.randChance(50))
                    Talk(TextIds.SaySummon);

                DoCastSelf(RandomHelper.RAND(SpellIds.SummonAstralFlareNe, SpellIds.SummonAstralFlareNw, SpellIds.SummonAstralFlareSe, SpellIds.SummonAstralFlareSw), new CastSpellExtraArgs(true));

                int mana = (me.GetMaxPower(PowerType.Mana) / 10);
                if (mana != 0)
                {
                    me.ModifyPower(PowerType.Mana, -mana);

                    if (me.GetPower(PowerType.Mana) * 100 / me.GetMaxPower(PowerType.Mana) < 10)
                    {
                        Talk(TextIds.SayEvocate);
                        me.InterruptNonMeleeSpells(false);
                        DoCastSelf(SpellIds.Evocation);
                    }
                }
                task.Repeat(TimeSpan.FromSeconds(10));
            });
            _scheduler.Schedule(TimeSpan.FromMinutes(12), ScheduleTasks =>
            {
                Talk(TextIds.SayEnrage);
                DoCastSelf(SpellIds.Berserk, new CastSpellExtraArgs(true));
            });
        }

        public override void DamageTaken(Unit attacker, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            if (!HealthAbovePct(15) && !_infused)
            {
                _infused = true;
                _scheduler.Schedule(TimeSpan.FromMilliseconds(1), task => DoCastSelf(SpellIds.ArcaneInfusion, new CastSpellExtraArgs(true)));
                _scheduler.CancelGroup(MiscConst.GroupAstralFlare);
            }
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }

        bool _infused;
    }

    [Script]
    class npc_curator_astral_flare : ScriptedAI
    {
        public npc_curator_astral_flare(Creature creature) : base(creature)
        {
            me.SetReactState(ReactStates.Passive);
        }

        public override void Reset()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(2), task =>
            {
                me.SetReactState(ReactStates.Aggressive);
                me.SetUninteractible(false);
                DoZoneInCombat();
            });
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }
}