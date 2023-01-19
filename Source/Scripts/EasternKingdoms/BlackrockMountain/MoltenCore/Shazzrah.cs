// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore.Shazzrah
{
    struct SpellIds
    {
        public const uint ArcaneExplosion = 19712;
        public const uint ShazzrahCurse = 19713;
        public const uint MagicGrounding = 19714;
        public const uint Counterspell = 19715;
        public const uint ShazzrahGateDummy = 23138; // Teleports to and attacks a random target.
        public const uint ShazzrahGate = 23139;
    }

    struct EventIds
    {
        public const uint ArcaneExplosion = 1;
        public const uint ArcaneExplosionTriggered = 2;
        public const uint ShazzrahCurse = 3;
        public const uint MagicGrounding = 4;
        public const uint Counterspell = 5;
        public const uint ShazzrahGate = 6;
    }

    [Script]
    class boss_shazzrah : BossAI
    {
        public boss_shazzrah(Creature creature) : base(creature, DataTypes.Shazzrah) { }

        public override void JustEngagedWith(Unit target)
        {
            base.JustEngagedWith(target);
            _events.ScheduleEvent(EventIds.ArcaneExplosion, TimeSpan.FromSeconds(6));
            _events.ScheduleEvent(EventIds.ShazzrahCurse, TimeSpan.FromSeconds(10));
            _events.ScheduleEvent(EventIds.MagicGrounding, TimeSpan.FromSeconds(24));
            _events.ScheduleEvent(EventIds.Counterspell, TimeSpan.FromSeconds(15));
            _events.ScheduleEvent(EventIds.ShazzrahGate, TimeSpan.FromSeconds(45));
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _events.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case EventIds.ArcaneExplosion:
                        DoCastVictim(SpellIds.ArcaneExplosion);
                        _events.ScheduleEvent(EventIds.ArcaneExplosion, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(7));
                        break;
                    // Triggered subsequent to using "Gate of Shazzrah".
                    case EventIds.ArcaneExplosionTriggered:
                        DoCastVictim(SpellIds.ArcaneExplosion);
                        break;
                    case EventIds.ShazzrahCurse:
                        Unit target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true, true, -(int)SpellIds.ShazzrahCurse);
                        if (target)
                            DoCast(target, SpellIds.ShazzrahCurse);
                        _events.ScheduleEvent(EventIds.ShazzrahCurse, TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(30));
                        break;
                    case EventIds.MagicGrounding:
                        DoCast(me, SpellIds.MagicGrounding);
                        _events.ScheduleEvent(EventIds.MagicGrounding, TimeSpan.FromSeconds(35));
                        break;
                    case EventIds.Counterspell:
                        DoCastVictim(SpellIds.Counterspell);
                        _events.ScheduleEvent(EventIds.Counterspell, TimeSpan.FromSeconds(16), TimeSpan.FromSeconds(20));
                        break;
                    case EventIds.ShazzrahGate:
                        ResetThreatList();
                        DoCastAOE(SpellIds.ShazzrahGateDummy);
                        _events.ScheduleEvent(EventIds.ArcaneExplosionTriggered, TimeSpan.FromSeconds(2));
                        _events.RescheduleEvent(EventIds.ArcaneExplosion, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(6));
                        _events.ScheduleEvent(EventIds.ShazzrahGate, TimeSpan.FromSeconds(45));
                        break;
                    default:
                        break;
                }

                if (me.HasUnitState(UnitState.Casting))
                    return;
            });


            DoMeleeAttackIfReady();
        }
    }

    [Script] // 23138 - Gate of Shazzrah
    class spell_shazzrah_gate_dummy : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ShazzrahGate);
        }

        void FilterTargets(List<WorldObject> targets)
        {
            if (targets.Empty())
                return;

            WorldObject target = targets.SelectRandom();
            targets.Clear();
            targets.Add(target);
        }

        void HandleScript(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (target)
            {
                target.CastSpell(GetCaster(), SpellIds.ShazzrahGate, true);
                Creature creature = GetCaster().ToCreature();
                if (creature)
                    creature.GetAI().AttackStart(target); // Attack the target which caster will teleport to.
            }
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Dummy));
        }
    }
}

