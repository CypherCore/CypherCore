// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.EasternKingdoms.BaradinHold.Occuthar
{
    struct SpellIds
    {
        public const uint SearingShadows = 96913;
        public const uint FocusedFireFirstDamage = 97212;
        public const uint FocusedFireTrigger = 96872;
        public const uint FocusedFireVisual = 96886;
        public const uint FocusedFire = 96884;
        public const uint EyesOfOccuthar = 96920;
        public const uint GazeOfOccuthar = 96942;
        public const uint OccutharsDestuction = 96968;
        public const uint Berserk = 47008;
    }

    struct EventIds
    {
        public const uint SearingShadows = 1;
        public const uint FocusedFire = 2;
        public const uint EyesOfOccuthar = 3;
        public const uint Berserk = 4;

        public const uint FocusedFireFirstDamage = 1;
    }

    struct MiscConst
    {
        public const uint MaxOccutharVehicleSeats = 7;
    }

    [Script]
    class boss_occuthar : BossAI
    {
        Vehicle _vehicle;

        public boss_occuthar(Creature creature) : base(creature, DataTypes.Occuthar)
        {
            _vehicle = me.GetVehicleKit();
            Cypher.Assert(_vehicle != null);
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);
            instance.SendEncounterUnit(EncounterFrameType.Engage, me);
            _events.ScheduleEvent(EventIds.SearingShadows, TimeSpan.FromSeconds(8));
            _events.ScheduleEvent(EventIds.FocusedFire, TimeSpan.FromSeconds(15));
            _events.ScheduleEvent(EventIds.EyesOfOccuthar, TimeSpan.FromSeconds(30));
            _events.ScheduleEvent(EventIds.Berserk, TimeSpan.FromMinutes(5));
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            base.EnterEvadeMode(why);
            instance.SendEncounterUnit(EncounterFrameType.Disengage, me);
            _DespawnAtEvade();
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
            instance.SendEncounterUnit(EncounterFrameType.Disengage, me);
        }

        public override void JustSummoned(Creature summon)
        {
            summons.Summon(summon);

            if (summon.GetEntry() == CreatureIds.FocusFireDummy)
            {
                DoCast(summon, SpellIds.FocusedFire);

                for (sbyte i = 0; i < MiscConst.MaxOccutharVehicleSeats; ++i)
                {
                    Unit vehicle = _vehicle.GetPassenger(i);
                    if (vehicle != null)
                        vehicle.CastSpell(summon, SpellIds.FocusedFireVisual);
                }
            }
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
                    case EventIds.SearingShadows:
                        DoCastAOE(SpellIds.SearingShadows);
                        _events.ScheduleEvent(EventIds.SearingShadows, TimeSpan.FromSeconds(25));
                        break;
                    case EventIds.FocusedFire:
                        DoCastAOE(SpellIds.FocusedFireTrigger, new CastSpellExtraArgs(true));
                        _events.ScheduleEvent(EventIds.FocusedFire, TimeSpan.FromSeconds(15));
                        break;
                    case EventIds.EyesOfOccuthar:
                        DoCastAOE(SpellIds.EyesOfOccuthar);
                        _events.RescheduleEvent(EventIds.FocusedFire, TimeSpan.FromSeconds(15));
                        _events.ScheduleEvent(EventIds.EyesOfOccuthar, TimeSpan.FromSeconds(60));
                        break;
                    case EventIds.Berserk:
                        DoCast(me, SpellIds.Berserk, new CastSpellExtraArgs(true));
                        break;
                    default:
                        break;
                }
            });
        }
    }

    [Script]
    class npc_eyestalk : ScriptedAI
    {
        InstanceScript _instance;
        byte _damageCount;

        public npc_eyestalk(Creature creature) : base(creature)
        {
            _instance = creature.GetInstanceScript();
        }

        public override void IsSummonedBy(WorldObject summoner)
        {
            // player is the spellcaster so register summon manually
            Creature occuthar = ObjectAccessor.GetCreature(me, _instance.GetGuidData(DataTypes.Occuthar));
            if (occuthar != null)
                occuthar.GetAI().JustSummoned(me);
        }

        public override void Reset()
        {
            _events.Reset();
            _events.ScheduleEvent(EventIds.FocusedFireFirstDamage, TimeSpan.FromSeconds(0));
        }

        public override void UpdateAI(uint diff)
        {
            _events.Update(diff);

            if (_events.ExecuteEvent() == EventIds.FocusedFireFirstDamage)
            {
                DoCastAOE(SpellIds.FocusedFireFirstDamage);
                if (++_damageCount < 2)
                    _events.ScheduleEvent(EventIds.FocusedFireFirstDamage, TimeSpan.FromSeconds(1));
            }
        }

        public override void EnterEvadeMode(EvadeReason why) { } // Never evade
    }

    [Script] // 96872 - Focused Fire
    class spell_occuthar_focused_fire_SpellScript : SpellScript
    {
        void FilterTargets(List<WorldObject> targets)
        {
            if (targets.Count < 2)
                return;

            targets.RemoveAll(target => GetCaster().GetVictim() == target);

            if (targets.Count >= 2)
                targets.RandomResize(1);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
        }
    }

    [Script] // Id - 96931 Eyes of Occu'thar
    class spell_occuthar_eyes_of_occuthar_SpellScript : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect(spellInfo.Id, 0) && ValidateSpellInfo((uint)spellInfo.GetEffect(0).CalcValue());
        }

        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        void FilterTargets(List<WorldObject> targets)
        {
            if (targets.Empty())
                return;

            targets.RandomResize(1);
        }

        void HandleScript(uint effIndex)
        {
            GetHitUnit().CastSpell(GetCaster(), (uint)GetEffectValue(), true);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEntry));
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // Id - 96932 Eyes of Occu'thar
    class spell_occuthar_eyes_of_occuthar_vehicle_SpellScript : SpellScript
    {
        public override bool Load()
        {
            InstanceMap instance = GetCaster().GetMap().ToInstanceMap();
            if (instance != null)
                return instance.GetScriptName() == nameof(instance_baradin_hold);

            return false;
        }

        void HandleScript()
        {
            Position pos = GetHitUnit().GetPosition();

            Creature occuthar = ObjectAccessor.GetCreature(GetCaster(), GetCaster().GetInstanceScript().GetGuidData(DataTypes.Occuthar));
            if (occuthar != null)
            {
                Creature creature = occuthar.SummonCreature(CreatureIds.EyeOfOccuthar, pos);
                if (creature != null)
                    creature.CastSpell(GetHitUnit(), SpellIds.GazeOfOccuthar, false);
            }
        }

        public override void Register()
        {
            AfterHit.Add(new HitHandler(HandleScript));
        }
    }

    [Script] // 96942 / 101009 - Gaze of Occu'thar
    class spell_occuthar_occuthars_destruction_AuraScript : AuraScript
    {
        public override bool Load()
        {
            return GetCaster() != null && GetCaster().GetTypeId() == TypeId.Unit;
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                if (IsExpired())
                    caster.CastSpell((WorldObject)null, SpellIds.OccutharsDestuction, new CastSpellExtraArgs(aurEff));

                caster.ToCreature().DespawnOrUnsummon(TimeSpan.FromMilliseconds(500));
            }
        }

        public override void Register()
        {
            OnEffectRemove.Add(new EffectApplyHandler(OnRemove, 2, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
        }
    }
}

