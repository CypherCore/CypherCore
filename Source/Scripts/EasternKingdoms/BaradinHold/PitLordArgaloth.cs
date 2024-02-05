// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.EasternKingdoms.BaradinHold.PitLordArgaloth
{
    struct SpellIds
    {
        public const uint MeteorSlash = 88942;
        public const uint ConsumingDarkness = 88954;
        public const uint FelFirestorm = 88972;
        public const uint Berserk = 47008;
    }

    [Script]
    class boss_pit_lord_argaloth : BossAI
    {
        boss_pit_lord_argaloth(Creature creature) : base(creature, DataTypes.Argaloth) { }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);
            instance.SendEncounterUnit(EncounterFrameType.Engage, me);
            _scheduler.Schedule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20), task =>
            {
                DoCastAOE(SpellIds.MeteorSlash);
                task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(20));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(25), task =>
            {
                DoCastAOE(SpellIds.ConsumingDarkness, new CastSpellExtraArgs(true));
                task.Repeat(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(25));
            });
            _scheduler.Schedule(TimeSpan.FromMinutes(5), task =>
            {
                DoCast(me, SpellIds.Berserk, new CastSpellExtraArgs(true));
            });
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            instance.SendEncounterUnit(EncounterFrameType.Disengage, me);
            _DespawnAtEvade();
        }

        public override void DamageTaken(Unit attacker, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            if (me.HealthBelowPctDamaged(33, damage) ||
                me.HealthBelowPctDamaged(66, damage))
            {
                DoCastAOE(SpellIds.FelFirestorm);
            }
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
            instance.SendEncounterUnit(EncounterFrameType.Disengage, me);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);
        }
    }

    [Script] // 88954 / 95173 - Consuming Darkness
    class spell_argaloth_consuming_darkness_SpellScript : SpellScript
    {
        void FilterTargets(List<WorldObject> targets)
        {
            targets.RandomResize(GetCaster().GetMap().Is25ManRaid() ? 8 : 3u);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
        }
    }

    [Script] // 88942 / 95172 - Meteor Slash
    class spell_argaloth_meteor_slash_SpellScript : SpellScript
    {
        int _targetCount;

        void CountTargets(List<WorldObject> targets)
        {
            _targetCount = targets.Count;
        }

        void SplitDamage()
        {
            if (_targetCount == 0)
                return;

            SetHitDamage((GetHitDamage() / _targetCount));
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(CountTargets, 0, Targets.UnitConeCasterToDestEnemy));
            OnHit.Add(new HitHandler(SplitDamage));
        }
    }
}

