/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;

namespace Scripts.Northrend.DraktharonKeep.TharonJa
{
    struct SpellIds
    {
        // Skeletal Spells (Phase 1)
        public const uint CurseOfLife = 49527;
        public const uint RainOfFire = 49518;
        public const uint ShadowVolley = 49528;
        public const uint DecayFlesh = 49356; // Cast At End Of Phase 1; Starts Phase 2
                                              // Flesh Spells (Phase 2)
        public const uint GiftOfTharonJa = 52509;
        public const uint ClearGiftOfTharonJa = 53242;
        public const uint EyeBeam = 49544;
        public const uint LightningBreath = 49537;
        public const uint PoisonCloud = 49548;
        public const uint ReturnFlesh = 53463; // Channeled Spell Ending Phase Two And Returning To Phase 1. This Ability Will Stun The Party For 6 Seconds.
        public const uint AchievementCheck = 61863;
        public const uint FleshVisual = 52582;
        public const uint Dummy = 49551;
    }

    struct EventIds
    {
        public const uint CurseOfLife = 1;
        public const uint RainOfFire = 2;
        public const uint ShadowVolley = 3;

        public const uint EyeBeam = 4;
        public const uint LightningBreath = 5;
        public const uint PoisonCloud = 6;

        public const uint DecayFlesh = 7;
        public const uint GoingFlesh = 8;
        public const uint ReturnFlesh = 9;
        public const uint GoingSkeletal = 10;
    }

    struct TextIds
    {
        public const uint SayAggro = 0;
        public const uint SayKill = 1;
        public const uint SayFlesh = 2;
        public const uint SaySkeleton = 3;
        public const uint SayDeath = 4;
    }

    struct Misc
    {
        public const uint ModelFlesh = 27073;
    }

    [Script]
    class boss_tharon_ja : BossAI
    {
        public boss_tharon_ja(Creature creature) : base(creature, DTKDataTypes.TharonJa) { }

        public override void Reset()
        {
            _Reset();
            me.RestoreDisplayId();
        }

        public override void EnterCombat(Unit who)
        {
            Talk(TextIds.SayAggro);
            _EnterCombat();

            _events.ScheduleEvent(EventIds.DecayFlesh, TimeSpan.FromSeconds(20));
            _events.ScheduleEvent(EventIds.CurseOfLife, TimeSpan.FromSeconds(1));
            _events.ScheduleEvent(EventIds.RainOfFire, TimeSpan.FromSeconds(14), TimeSpan.FromSeconds(18));
            _events.ScheduleEvent(EventIds.ShadowVolley, TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10));
        }

        public override void KilledUnit(Unit who)
        {
            if (who.IsTypeId(TypeId.Player))
                Talk(TextIds.SayKill);
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();

            Talk(TextIds.SayDeath);
            DoCastAOE(SpellIds.ClearGiftOfTharonJa, true);
            DoCastAOE(SpellIds.AchievementCheck, true);
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
                    case EventIds.CurseOfLife:
                        {
                            Unit target = SelectTarget(SelectAggroTarget.Random, 0, 100.0f, true);
                            if (target)
                                DoCast(target, SpellIds.CurseOfLife);
                            _events.ScheduleEvent(EventIds.CurseOfLife, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15));
                        }
                        return;
                    case EventIds.ShadowVolley:
                        DoCastVictim(SpellIds.ShadowVolley);
                        _events.ScheduleEvent(EventIds.ShadowVolley, TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10));
                        return;
                    case EventIds.RainOfFire:
                        {
                            Unit target = SelectTarget(SelectAggroTarget.Random, 0, 100.0f, true);
                            if (target)
                                DoCast(target, SpellIds.RainOfFire);
                            _events.ScheduleEvent(EventIds.RainOfFire, TimeSpan.FromSeconds(14), TimeSpan.FromSeconds(18));
                        }
                        return;
                    case EventIds.LightningBreath:
                        {
                            Unit target = SelectTarget(SelectAggroTarget.Random, 0, 100.0f, true);
                            if (target)
                                DoCast(target, SpellIds.LightningBreath);
                            _events.ScheduleEvent(EventIds.LightningBreath, TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(7));
                        }
                        return;
                    case EventIds.EyeBeam:
                        {
                            Unit target = SelectTarget(SelectAggroTarget.Random, 0, 100.0f, true);
                            if (target)
                                DoCast(target, SpellIds.EyeBeam);
                            _events.ScheduleEvent(EventIds.EyeBeam, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(6));
                        }
                        return;
                    case EventIds.PoisonCloud:
                        DoCastAOE(SpellIds.PoisonCloud);
                        _events.ScheduleEvent(EventIds.PoisonCloud, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12));
                        return;
                    case EventIds.DecayFlesh:
                        DoCastAOE(SpellIds.DecayFlesh);
                        _events.ScheduleEvent(EventIds.GoingFlesh, TimeSpan.FromSeconds(6));
                        return;
                    case EventIds.GoingFlesh:
                        Talk(TextIds.SayFlesh);
                        me.SetDisplayId(Misc.ModelFlesh);
                        DoCastAOE(SpellIds.GiftOfTharonJa, true);
                        DoCast(me, SpellIds.FleshVisual, true);
                        DoCast(me, SpellIds.Dummy, true);

                        _events.Reset();
                        _events.ScheduleEvent(EventIds.ReturnFlesh, TimeSpan.FromSeconds(20));
                        _events.ScheduleEvent(EventIds.LightningBreath, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(4));
                        _events.ScheduleEvent(EventIds.EyeBeam, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8));
                        _events.ScheduleEvent(EventIds.PoisonCloud, TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(7));
                        break;
                    case EventIds.ReturnFlesh:
                        DoCastAOE(SpellIds.ReturnFlesh);
                        _events.ScheduleEvent(EventIds.GoingSkeletal, 6000);
                        return;
                    case EventIds.GoingSkeletal:
                        Talk(TextIds.SaySkeleton);
                        me.RestoreDisplayId();
                        DoCastAOE(SpellIds.ClearGiftOfTharonJa, true);

                        _events.Reset();
                        _events.ScheduleEvent(EventIds.DecayFlesh, TimeSpan.FromSeconds(20));
                        _events.ScheduleEvent(EventIds.CurseOfLife, TimeSpan.FromSeconds(1));
                        _events.ScheduleEvent(EventIds.RainOfFire, TimeSpan.FromSeconds(14), TimeSpan.FromSeconds(18));
                        _events.ScheduleEvent(EventIds.ShadowVolley, TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10));
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

    [Script]
    class spell_tharon_ja_clear_gift_of_tharon_ja : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GiftOfTharonJa);
        }

        void HandleScript(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (target)
                target.RemoveAura(SpellIds.GiftOfTharonJa);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }
}
