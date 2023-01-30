// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using Game.Spells.Auras.EffectHandlers;

namespace Scripts.EasternKingdoms.BaradinHold.Occuthar
{
    internal struct SpellIds
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

    internal struct EventIds
    {
        public const uint SearingShadows = 1;
        public const uint FocusedFire = 2;
        public const uint EyesOfOccuthar = 3;
        public const uint Berserk = 4;

        public const uint FocusedFireFirstDamage = 1;
    }

    internal struct MiscConst
    {
        public const uint MaxOccutharVehicleSeats = 7;
    }

    [Script]
    internal class boss_occuthar : BossAI
    {
        private readonly Vehicle _vehicle;

        public boss_occuthar(Creature creature) : base(creature, DataTypes.Occuthar)
        {
            _vehicle = me.GetVehicleKit();
            Cypher.Assert(_vehicle != null);
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);
            Instance.SendEncounterUnit(EncounterFrameType.Engage, me);
            Events.ScheduleEvent(EventIds.SearingShadows, TimeSpan.FromSeconds(8));
            Events.ScheduleEvent(EventIds.FocusedFire, TimeSpan.FromSeconds(15));
            Events.ScheduleEvent(EventIds.EyesOfOccuthar, TimeSpan.FromSeconds(30));
            Events.ScheduleEvent(EventIds.Berserk, TimeSpan.FromMinutes(5));
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            base.EnterEvadeMode(why);
            Instance.SendEncounterUnit(EncounterFrameType.Disengage, me);
            _DespawnAtEvade();
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
            Instance.SendEncounterUnit(EncounterFrameType.Disengage, me);
        }

        public override void JustSummoned(Creature summon)
        {
            Summons.Summon(summon);

            if (summon.GetEntry() == CreatureIds.FocusFireDummy)
            {
                DoCast(summon, SpellIds.FocusedFire);

                for (sbyte i = 0; i < MiscConst.MaxOccutharVehicleSeats; ++i)
                {
                    Unit vehicle = _vehicle.GetPassenger(i);

                    if (vehicle)
                        vehicle.CastSpell(summon, SpellIds.FocusedFireVisual);
                }
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            Events.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            Events.ExecuteEvents(eventId =>
                                  {
                                      switch (eventId)
                                      {
                                          case EventIds.SearingShadows:
                                              DoCastAOE(SpellIds.SearingShadows);
                                              Events.ScheduleEvent(EventIds.SearingShadows, TimeSpan.FromSeconds(25));

                                              break;
                                          case EventIds.FocusedFire:
                                              DoCastAOE(SpellIds.FocusedFireTrigger, new CastSpellExtraArgs(true));
                                              Events.ScheduleEvent(EventIds.FocusedFire, TimeSpan.FromSeconds(15));

                                              break;
                                          case EventIds.EyesOfOccuthar:
                                              DoCastAOE(SpellIds.EyesOfOccuthar);
                                              Events.RescheduleEvent(EventIds.FocusedFire, TimeSpan.FromSeconds(15));
                                              Events.ScheduleEvent(EventIds.EyesOfOccuthar, TimeSpan.FromSeconds(60));

                                              break;
                                          case EventIds.Berserk:
                                              DoCast(me, SpellIds.Berserk, new CastSpellExtraArgs(true));

                                              break;
                                          default:
                                              break;
                                      }
                                  });

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    internal class npc_eyestalk : ScriptedAI
    {
        private readonly InstanceScript _instance;
        private byte _damageCount;

        public npc_eyestalk(Creature creature) : base(creature)
        {
            _instance = creature.GetInstanceScript();
        }

        public override void IsSummonedBy(WorldObject summoner)
        {
            // player is the spellcaster so register summon manually
            Creature occuthar = ObjectAccessor.GetCreature(me, _instance.GetGuidData(DataTypes.Occuthar));

            occuthar?.GetAI().JustSummoned(me);
        }

        public override void Reset()
        {
            Events.Reset();
            Events.ScheduleEvent(EventIds.FocusedFireFirstDamage, TimeSpan.FromSeconds(0));
        }

        public override void UpdateAI(uint diff)
        {
            Events.Update(diff);

            if (Events.ExecuteEvent() == EventIds.FocusedFireFirstDamage)
            {
                DoCastAOE(SpellIds.FocusedFireFirstDamage);

                if (++_damageCount < 2)
                    Events.ScheduleEvent(EventIds.FocusedFireFirstDamage, TimeSpan.FromSeconds(1));
            }
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
        } // Never evade
    }

    [Script] // 96872 - Focused Fire
    internal class spell_occuthar_focused_fire_SpellScript : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
        }

        private void FilterTargets(List<WorldObject> targets)
        {
            if (targets.Count < 2)
                return;

            targets.RemoveAll(target => GetCaster().GetVictim() == target);

            if (targets.Count >= 2)
                targets.RandomResize(1);
        }
    }

    [Script] // Id - 96931 Eyes of Occu'thar
    internal class spell_occuthar_eyes_of_occuthar_SpellScript : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return !spellInfo.GetEffects().Empty() && ValidateSpellInfo((uint)spellInfo.GetEffect(0).CalcValue());
        }

        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEntry));
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void FilterTargets(List<WorldObject> targets)
        {
            if (targets.Empty())
                return;

            targets.RandomResize(1);
        }

        private void HandleScript(uint effIndex)
        {
            GetHitUnit().CastSpell(GetCaster(), (uint)GetEffectValue(), true);
        }
    }

    [Script] // Id - 96932 Eyes of Occu'thar
    internal class spell_occuthar_eyes_of_occuthar_vehicle_SpellScript : SpellScript, IAfterHit
    {
        public override bool Load()
        {
            InstanceMap instance = GetCaster().GetMap().ToInstanceMap();

            if (instance != null)
                return instance.GetScriptName() == nameof(instance_baradin_hold);

            return false;
        }

        public void AfterHit()
        {
            Position pos = GetHitUnit().GetPosition();

            Creature occuthar = ObjectAccessor.GetCreature(GetCaster(), GetCaster().GetInstanceScript().GetGuidData(DataTypes.Occuthar));

            if (occuthar != null)
            {
                Creature creature = occuthar.SummonCreature(CreatureIds.EyeOfOccuthar, pos);

                creature?.CastSpell(GetHitUnit(), SpellIds.GazeOfOccuthar, false);
            }
        }
    }

    [Script] // 96942 / 101009 - Gaze of Occu'thar
    internal class spell_occuthar_occuthars_destruction_AuraScript : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Load()
        {
            return GetCaster() && GetCaster().GetTypeId() == TypeId.Unit;
        }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(OnRemove, 2, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }

        private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();

            if (caster)
            {
                if (IsExpired())
                    caster.CastSpell((WorldObject)null, SpellIds.OccutharsDestuction, new CastSpellExtraArgs(aurEff));

                caster.ToCreature().DespawnOrUnsummon(TimeSpan.FromMilliseconds(500));
            }
        }
    }
}