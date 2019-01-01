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
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Northrend.AzjolNerub.AzjolNerub.KrikthirTheGatewatcher
{
    struct SpellIds
    {
        // Krik'Thir The Gatewatcher
        public const uint SubbossAggroTrigger = 52343;
        public const uint Swarm = 52440;
        public const uint MindFlay = 52586;
        public const uint CurseOfFatigue = 52592;
        public const uint Frenzy = 28747;

        // Watchers - Shared
        public const uint WebWrap = 52086;
        public const uint WebWrapWrapped = 52087;
        public const uint InfectedBite = 52469;

        // Watcher Gashra
        public const uint Enrage = 52470;
        // Watcher Narjil
        public const uint BlindingWebs = 52524;
        // Watcher Silthik
        public const uint PoisonSpray = 52493;

        // Anub'Ar Warrior
        public const uint Cleave = 49806;
        public const uint Strike = 52532;

        // Anub'Ar Skirmisher
        public const uint Charge = 52538;
        public const uint Backstab = 52540;
        public const uint FixtateTrigger = 52536;
        public const uint FixtateTriggered = 52537;

        // Anub'Ar Shadowcaster
        public const uint ShadowBolt = 52534;
        public const uint ShadowNova = 52535;

        // Skittering Infector
        public const uint AcidSplash = 52446;
    }

    struct Misc
    {
        public const uint DataPetGroup = 0;

        // Krik'thir the Gatewatcher
        public const uint EventSendGroup = 1;
        public const uint EventSwarm = 2;
        public const uint EventMindFlay = 3;
        public const uint EventFrenzy = 4;
    }

    struct ActionIds
    {
        public const int GashraDied = 0;
        public const int NarjilDied = 1;
        public const int SilthikDied = 2;
        public const int WatcherEngaged = 3;
        public const int PetEngaged = 4;
        public const int PetEvade = 5;
    }

    struct TextIds
    {
        public const uint SayAggro = 0;
        public const uint SaySlay = 1;
        public const uint SayDeath = 2;
        public const uint SaySwarm = 3;
        public const uint SayPrefight = 4;
        public const uint SaySendGroup = 5;
    }

    [Script]
    class boss_krik_thir : BossAI
    {
        public boss_krik_thir(Creature creature) : base(creature, ANDataTypes.KrikthirTheGatewatcher) { }

        void SummonAdds()
        {
            if (instance.GetBossState(ANDataTypes.KrikthirTheGatewatcher) == EncounterState.Done)
                return;

            for (byte i = 1; i <= 3; ++i)
            {
                List<TempSummon> summons;
                me.SummonCreatureGroup(i, out summons);

                foreach (TempSummon summon in summons)
                    summon.GetAI().SetData(Misc.DataPetGroup, i);
            }
        }

        public override void Reset()
        {
            base.Reset();
            _hadFrenzy = false;
            _petsInCombat = false;
            _watchersActive = 0;
            me.SetReactState(ReactStates.Passive);
        }

        public override void InitializeAI()
        {
            base.InitializeAI();
            SummonAdds();
        }

        public override void JustRespawned()
        {
            base.JustRespawned();
            SummonAdds();
        }

        public override void KilledUnit(Unit victim)
        {
            if (victim.IsTypeId(TypeId.Player))
                Talk(TextIds.SaySlay);
        }

        public override void JustDied(Unit killer)
        {
            summons.Clear();
            base.JustDied(killer);
            Talk(TextIds.SayDeath);
        }

        public override void EnterCombat(Unit who)
        {
            _petsInCombat = false;
            me.SetReactState(ReactStates.Aggressive);
            summons.DoZoneInCombat();

            _events.CancelEvent(Misc.EventSendGroup);
            _events.ScheduleEvent(Misc.EventSwarm, TimeSpan.FromSeconds(5));
            _events.ScheduleEvent(Misc.EventMindFlay, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3));

            base.EnterCombat(who);
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (!me.HasReactState(ReactStates.Passive))
            {
                base.MoveInLineOfSight(who);
                return;
            }

            if (me.CanStartAttack(who, false) && me.IsWithinDistInMap(who, me.GetAttackDistance(who) + me.m_CombatDistance))
                EnterCombat(who);
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            summons.DespawnAll();
            _DespawnAtEvade();
        }

        public override void DoAction(int action)
        {
            switch (action)
            {
                case -ANInstanceMisc.ActionGatewatcherGreet:
                    if (!_hadGreet && me.IsAlive() && !me.IsInCombat() && !_petsInCombat)
                    {
                        _hadGreet = true;
                        Talk(TextIds.SayPrefight);
                    }
                    break;
                case ActionIds.GashraDied:
                case ActionIds.NarjilDied:
                case ActionIds.SilthikDied:
                    if (_watchersActive == 0) // something is wrong
                    {
                        EnterEvadeMode(EvadeReason.Other);
                        return;
                    }
                    if ((--_watchersActive) == 0) // if there are no watchers currently in combat...
                        _events.RescheduleEvent(Misc.EventSendGroup, TimeSpan.FromSeconds(5)); // ...send the next watcher after the targets sooner
                    break;
                case ActionIds.WatcherEngaged:
                    ++_watchersActive;
                    break;
                case ActionIds.PetEngaged:
                    if (_petsInCombat || me.IsInCombat())
                        break;
                    _petsInCombat = true;
                    Talk(TextIds.SayAggro);
                    _events.ScheduleEvent(Misc.EventSendGroup, TimeSpan.FromSeconds(70));
                    break;
                case ActionIds.PetEvade:
                    EnterEvadeMode(EvadeReason.Other);
                    break;
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim() && !_petsInCombat)
                return;

            _events.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            if (me.HealthBelowPct(10) && !_hadFrenzy)
            {
                _hadFrenzy = true;
                _events.ScheduleEvent(Misc.EventFrenzy, TimeSpan.FromSeconds(1));
            }

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case Misc.EventSendGroup:
                        DoCastAOE(SpellIds.SubbossAggroTrigger, true);
                        _events.Repeat(TimeSpan.FromSeconds(70));
                        break;
                    case Misc.EventSwarm:
                        DoCastAOE(SpellIds.Swarm);
                        Talk(TextIds.SaySwarm);
                        break;
                    case Misc.EventMindFlay:
                        DoCastVictim(SpellIds.MindFlay);
                        _events.Repeat(TimeSpan.FromSeconds(9), TimeSpan.FromSeconds(11));
                        break;
                    case Misc.EventFrenzy:
                        DoCastSelf(SpellIds.Frenzy);
                        DoCastAOE(SpellIds.CurseOfFatigue);
                        _events.Repeat(TimeSpan.FromSeconds(15));
                        break;
                }

                if (me.HasUnitState(UnitState.Casting))
                    return;
            });

            DoMeleeAttackIfReady();
        }

        public override void SpellHit(Unit whose, SpellInfo spell)
        {
            if (spell.Id == SpellIds.SubbossAggroTrigger)
                DoZoneInCombat();
        }

        public override void SpellHitTarget(Unit who, SpellInfo spell)
        {
            if (spell.Id == SpellIds.SubbossAggroTrigger)
                Talk(TextIds.SaySendGroup);
        }

        bool _hadGreet;
        bool _hadFrenzy;
        bool _petsInCombat;
        byte _watchersActive;
    }

    class npc_gatewatcher_petAI : ScriptedAI
    {
        public npc_gatewatcher_petAI(Creature creature, bool isWatcher) : base(creature)
        {
            _instance = creature.GetInstanceScript();
            _isWatcher = isWatcher;
        }

        public virtual void _EnterCombat() { }

        public override void EnterCombat(Unit who)
        {
            if (_isWatcher)
            {
                _isWatcher = false;

                TempSummon meSummon = me.ToTempSummon();
                if (meSummon)
                {
                    Creature summoner = meSummon.GetSummonerCreatureBase();
                    if (summoner)
                        summoner.GetAI().DoAction(ActionIds.WatcherEngaged);
                }
            }

            if (me.HasReactState(ReactStates.Passive))
            {
                List<Creature> others = new List<Creature>();
                me.GetCreatureListWithEntryInGrid(others, 0, 40.0f);
                foreach (Creature other in others)
                {
                    if (other.GetAI().GetData(Misc.DataPetGroup) == _petGroup)
                    {
                        other.SetReactState(ReactStates.Aggressive);
                        other.GetAI().AttackStart(who);
                    }
                }

                TempSummon meSummon = me.ToTempSummon();
                if (meSummon)
                {
                    Creature summoner = meSummon.GetSummonerCreatureBase();
                    if (summoner)
                        summoner.GetAI().DoAction(ActionIds.PetEngaged);
                }
            }
            _EnterCombat();
            base.EnterCombat(who);
        }

        public override void SetData(uint data, uint value)
        {
            if (data == Misc.DataPetGroup)
            {
                _petGroup = value;
                me.SetReactState(_petGroup != 0 ? ReactStates.Passive : ReactStates.Aggressive);
            }
        }

        public override uint GetData(uint data)
        {
            if (data == Misc.DataPetGroup)
                return _petGroup;
            return 0;
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (!me.HasReactState(ReactStates.Passive))
            {
                base.MoveInLineOfSight(who);
                return;
            }

            if (me.CanStartAttack(who, false) && me.IsWithinDistInMap(who, me.GetAttackDistance(who) + me.m_CombatDistance))
                EnterCombat(who);
        }

        public override void SpellHit(Unit whose, SpellInfo spell)
        {
            if (spell.Id == SpellIds.SubbossAggroTrigger)
                DoZoneInCombat();
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            TempSummon meSummon = me.ToTempSummon();
            if (meSummon)
            {
                Creature summoner = meSummon.GetSummonerCreatureBase();
                if (summoner)
                    summoner.GetAI().DoAction(ActionIds.PetEvade);
                else
                    me.DespawnOrUnsummon();
                return;
            }
            base.EnterEvadeMode(why);
        }

        protected InstanceScript _instance;
        uint _petGroup;
        bool _isWatcher;
    }

    [Script]
    class npc_watcher_gashra : npc_gatewatcher_petAI
    {
        public npc_watcher_gashra(Creature creature) : base(creature, true)
        {
            me.SetReactState(ReactStates.Passive);
        }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void _EnterCombat()
        {
            _scheduler.SetValidator(() => !me.HasUnitState(UnitState.Casting));

            _scheduler.Schedule(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5), task =>
            {
                DoCastSelf(SpellIds.Enrage);
                task.Repeat(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(20));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(16), TimeSpan.FromSeconds(19), task =>
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0, 100.0f);
                if (target)
                    DoCast(target, SpellIds.WebWrap);
                task.Repeat(TimeSpan.FromSeconds(13), TimeSpan.FromSeconds(19));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(11), task =>
            {
                DoCastVictim(SpellIds.InfectedBite);
                task.Repeat(TimeSpan.FromSeconds(23), TimeSpan.FromSeconds(27));
            });
        }

        public override void JustDied(Unit killer)
        {
            Creature krikthir = _instance.GetCreature(ANDataTypes.KrikthirTheGatewatcher);
            if (krikthir && krikthir.IsAlive())
                krikthir.GetAI().DoAction(ActionIds.GashraDied);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class npc_watcher_narjil : npc_gatewatcher_petAI
    {
        public npc_watcher_narjil(Creature creature) : base(creature, true) { }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void _EnterCombat()
        {
            _scheduler.SetValidator(() => !me.HasUnitState(UnitState.Casting));

            _scheduler.Schedule(TimeSpan.FromSeconds(13), TimeSpan.FromSeconds(18), task =>
            {
                DoCastVictim(SpellIds.BlindingWebs);
                task.Repeat(TimeSpan.FromSeconds(23), TimeSpan.FromSeconds(27));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5), task =>
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0, 100, true);
                if (target)
                    DoCast(target, SpellIds.WebWrap);
                task.Repeat(TimeSpan.FromSeconds(13), TimeSpan.FromSeconds(19));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(11), task =>
            {
                DoCastVictim(SpellIds.InfectedBite);
                task.Repeat(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(25));
            });
        }

        public override void JustDied(Unit killer)
        {
            Creature krikthir = _instance.GetCreature(ANDataTypes.KrikthirTheGatewatcher);
            if (krikthir && krikthir.IsAlive())
                krikthir.GetAI().DoAction(ActionIds.NarjilDied);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class npc_watcher_silthik : npc_gatewatcher_petAI
    {
        public npc_watcher_silthik(Creature creature) : base(creature, true) { }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void _EnterCombat()
        {
            _scheduler.SetValidator(() => !me.HasUnitState(UnitState.Casting));

            _scheduler.Schedule(TimeSpan.FromSeconds(16), TimeSpan.FromSeconds(19), task =>
             {
                 DoCastVictim(SpellIds.PoisonSpray);
                 task.Repeat(TimeSpan.FromSeconds(13), TimeSpan.FromSeconds(19));
             });

            _scheduler.Schedule(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(11), task =>
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0, 100, true);
                if (target)
                    DoCast(target, SpellIds.WebWrap);
                task.Repeat(TimeSpan.FromSeconds(13), TimeSpan.FromSeconds(17));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5), task =>
            {
                DoCastVictim(SpellIds.InfectedBite);
                task.Repeat(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(24));
            });
        }

        public override void JustDied(Unit killer)
        {
            Creature krikthir = _instance.GetCreature(ANDataTypes.KrikthirTheGatewatcher);
            if (krikthir && krikthir.IsAlive())
                krikthir.GetAI().DoAction(ActionIds.SilthikDied);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class npc_anub_ar_warrior : npc_gatewatcher_petAI
    {
        public npc_anub_ar_warrior(Creature creature) : base(creature, false) { }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void _EnterCombat()
        {
            _scheduler.SetValidator(() => !me.HasUnitState(UnitState.Casting));

            _scheduler.Schedule(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(9), task =>
            {
                DoCastVictim(SpellIds.Cleave);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(16));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), task =>
            {
                DoCastVictim(SpellIds.Strike);
                task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(19));
            });
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class npc_anub_ar_skirmisher : npc_gatewatcher_petAI
    {
        public npc_anub_ar_skirmisher(Creature creature) : base(creature, false) { }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void _EnterCombat()
        {
            _scheduler.SetValidator(() => !me.HasUnitState(UnitState.Casting));

            _scheduler.Schedule(TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(8), task =>
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0, 100.0f, true);
                if (target)
                    DoCast(target, SpellIds.Charge);
                task.Repeat(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(25));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(9), task =>
            {
                if (me.GetVictim() && me.GetVictim().isInBack(me))
                    DoCastVictim(SpellIds.Backstab);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(13));
            });
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            DoMeleeAttackIfReady();
        }

        public override void SpellHitTarget(Unit target, SpellInfo spell)
        {
            if (spell.Id == SpellIds.Charge && target)
                DoCast(target, SpellIds.FixtateTrigger);
        }
    }

    [Script]
    class npc_anub_ar_shadowcaster : npc_gatewatcher_petAI
    {
        public npc_anub_ar_shadowcaster(Creature creature) : base(creature, false) { }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void _EnterCombat()
        {
            _scheduler.SetValidator(() => !me.HasUnitState(UnitState.Casting));

            _scheduler.Schedule(TimeSpan.FromSeconds(4), task =>
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0, 100.0f, true);
                if (target)
                    DoCast(target, SpellIds.ShadowBolt);
                task.Repeat(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(14), task =>
            {
                DoCastVictim(SpellIds.ShadowNova);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(16));
            });
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class npc_skittering_swarmer : ScriptedAI
    {
        public npc_skittering_swarmer(Creature creature) : base(creature) { }

        public override void InitializeAI()
        {
            base.InitializeAI();
            Creature gatewatcher = me.GetInstanceScript().GetCreature(ANDataTypes.KrikthirTheGatewatcher);
            if (gatewatcher)
            {
                Unit target = gatewatcher.getAttackerForHelper();
                if (target)
                    AttackStart(target);
                gatewatcher.GetAI().JustSummoned(me);
            }
        }
    }

    [Script]
    class npc_skittering_infector : ScriptedAI
    {
        public npc_skittering_infector(Creature creature) : base(creature) { }

        public override void InitializeAI()
        {
            base.InitializeAI();
            Creature gatewatcher = me.GetInstanceScript().GetCreature(ANDataTypes.KrikthirTheGatewatcher);
            if (gatewatcher)
            {
                Unit target = gatewatcher.getAttackerForHelper();
                if (target)
                    AttackStart(target);
                gatewatcher.GetAI().JustSummoned(me);
            }
        }

        public override void JustDied(Unit killer)
        {
            DoCastAOE(SpellIds.AcidSplash);
            base.JustDied(killer);
        }
    }

    [Script]
    class npc_gatewatcher_web_wrap : NullCreatureAI
    {
        public npc_gatewatcher_web_wrap(Creature creature) : base(creature) { }

        public override void JustDied(Unit killer)
        {
            TempSummon meSummon = me.ToTempSummon();
            if (meSummon)
            {
                Unit summoner = meSummon.GetSummoner();
                if (summoner)
                    summoner.RemoveAurasDueToSpell(SpellIds.WebWrapWrapped);
            }
        }
    }

    [Script]
    class spell_gatewatcher_subboss_trigger : SpellScript
    {
        void HandleTargets(List<WorldObject> targetList)
        {
            // Remove any Watchers that are already in combat
            for (var i = 0; i < targetList.Count; ++i)
            {
                Creature creature = targetList[i].ToCreature();
                if (creature)
                    if (creature.IsAlive() && !creature.IsInCombat())
                        continue;

                targetList.RemoveAt(i);
            }

            // Default to Krik'thir himself if he isn't engaged
            WorldObject target = null;
            if (GetCaster() && !GetCaster().IsInCombat())
                target = GetCaster();
            // Unless there are Watchers that aren't engaged yet
            if (!targetList.Empty())
            {
                // If there are, pick one of them at random
                target = targetList.SelectRandom();
            }
            // And hit only that one
            targetList.Clear();
            if (target)
                targetList.Add(target);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(HandleTargets, 0, Targets.UnitSrcAreaEntry));
        }
    }

    [Script]
    class spell_anub_ar_skirmisher_fixtate : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.FixtateTriggered);
        }

        void HandleScript(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (target)
                target.CastSpell(GetCaster(), SpellIds.FixtateTriggered, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_gatewatcher_web_wrap : AuraScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.WebWrapWrapped);
        }

        void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
                return;

            Unit target = GetTarget();
            if (target)
                target.CastSpell(target, SpellIds.WebWrapWrapped, true);
        }

        public override void Register()
        {
            OnEffectRemove.Add(new EffectApplyHandler(HandleEffectRemove, 0, AuraType.ModRoot, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class achievement_watch_him_die : AchievementCriteriaScript
    {
        public achievement_watch_him_die() : base("achievement_watch_him_die") { }

        public override bool OnCheck(Player player, Unit target)
        {
            if (!target)
                return false;

            InstanceScript instance = target.GetInstanceScript();
            if (instance == null)
                return false;

            foreach (uint watcherData in new[] { ANDataTypes.WatcherGashra, ANDataTypes.WatcherNarjil, ANDataTypes.WatcherSilthik })
            {
                Creature watcher = instance.GetCreature(watcherData);
                if (watcher)
                    if (watcher.IsAlive())
                        continue;
                return false;
            }

            return true;
        }
    }
}
