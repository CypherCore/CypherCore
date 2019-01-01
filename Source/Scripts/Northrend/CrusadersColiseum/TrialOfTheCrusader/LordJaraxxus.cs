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
using System.Collections.Generic;

namespace Scripts.Northrend.CrusadersColiseum.TrialOfTheCrusader
{
    struct Jaraxxus
    {
        public const uint SayIntro = 0;
        public const uint SayAggro = 1;
        public const uint EmoteLegionFlame = 2;
        public const uint EmoteNetherPortal = 3;
        public const uint SayMistressOfPain = 4;
        public const uint EmoteIncinerate = 5;
        public const uint SayIncinerate = 6;
        public const uint EmoteInfernalEruption = 7;
        public const uint SayInfernalEruption = 8;
        public const uint SayKillPlayer = 9;
        public const uint SayDeath = 10;
        public const uint SayBerserk = 11;

        public const uint NpcLegionFlame = 34784;
        public const uint NpcInfernalVolcano = 34813;
        public const uint NpcFelInfernal = 34815; // Immune To All Cc On Heroic (Stuns; Banish; Interrupt; Etc)
        public const uint NpcNetherPortal = 34825;
        public const uint NpcMistressOfPain = 34826;

        public const uint SpellLegionFlame = 66197; // Player Should Run Away From Raid Because He Triggers Legion Flame
        public const uint SpellLegionFlameEffect = 66201; // Used By Trigger Npc
        public const uint SpellNetherPower = 66228; // +20% Of Spell Damage Per Stack; Stackable Up To 5/10 Times; Must Be Dispelled/Stealed
        public const uint SpellFelLighting = 66528; // Jumps To Nearby Targets
        public const uint SpellFelFireball = 66532; // Does Heavy Damage To The Tank; Interruptable
        public const uint SpellIncinerateFlesh = 66237; // Target Must Be Healed Or Will Trigger Burning Inferno
        public const uint SpellBurningInferno = 66242; // Triggered By Incinerate Flesh
        public const uint SpellInfernalEruption = 66258; // Summons Infernal Volcano
        public const uint SpellInfernalEruptionEffect = 66252; // Summons Felflame Infernal (3 At Normal And Inifinity At Heroic)
        public const uint SpellNetherPortal = 66269; // Summons Nether Portal
        public const uint SpellNetherPortalEffect = 66263; // Summons Mistress Of Pain (1 At Normal And Infinity At Heroic)

        public const uint SpellBerserk = 64238; // Unused

        // Mistress Of Pain Spells
        public const uint SpellShivanSlash = 67098;
        public const uint SpellSpinningStrike = 66283;
        public const uint SpellMistressKiss = 66336;
        public const uint SpellFelInferno = 67047;
        public const uint SpellFelStreak = 66494;
        public const int SpellLordHittin = 66326;   // Special Effect Preventing More Specific Spells Be Cast On The Same Player Within 10 Seconds
        public const uint SpellMistressKissDamageSilence = 66359;

        // Lord Jaraxxus
        public const uint EventFelFireball = 1;
        public const uint EventFelLightning = 2;
        public const uint EventIncinerateFlesh = 3;
        public const uint EventNetherPower = 4;
        public const uint EventLegionFlame = 5;
        public const uint EventSummonoNetherPortal = 6;
        public const uint EventSummonInfernalEruption = 7;

        // Mistress Of Pain
        public const uint EventShivanSlash = 8;
        public const uint EventSpinningStrike = 9;
        public const uint EventMistressKiss = 10;
    }

    [Script]
    class boss_jaraxxus : BossAI
    {
        public boss_jaraxxus(Creature creature) : base(creature, DataTypes.BossJaraxxus) { }

        public override void Reset()
        {
            _Reset();
            _events.ScheduleEvent(Jaraxxus.EventFelFireball, 5 * Time.InMilliseconds);
            _events.ScheduleEvent(Jaraxxus.EventFelLightning, RandomHelper.URand(10 * Time.InMilliseconds, 15 * Time.InMilliseconds));
            _events.ScheduleEvent(Jaraxxus.EventIncinerateFlesh, RandomHelper.URand(20 * Time.InMilliseconds, 25 * Time.InMilliseconds));
            _events.ScheduleEvent(Jaraxxus.EventNetherPower, 40 * Time.InMilliseconds);
            _events.ScheduleEvent(Jaraxxus.EventLegionFlame, 30 * Time.InMilliseconds);
            _events.ScheduleEvent(Jaraxxus.EventSummonoNetherPortal, 20 * Time.InMilliseconds);
            _events.ScheduleEvent(Jaraxxus.EventSummonInfernalEruption, 80 * Time.InMilliseconds);
        }

        public override void JustReachedHome()
        {
            _JustReachedHome();
            instance.SetBossState(DataTypes.BossJaraxxus, EncounterState.Fail);
            DoCast(me, Spells.JaraxxusChains);
            me.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable);
        }

        public override void KilledUnit(Unit who)
        {
            if (who.IsTypeId(TypeId.Player))
            {
                Talk(Jaraxxus.SayKillPlayer);
                instance.SetData(DataTypes.TributeToImmortalityEligible, 0);
            }
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
            Talk(Jaraxxus.SayDeath);
        }

        public override void JustSummoned(Creature summoned)
        {
            summons.Summon(summoned);
        }

        public override void EnterCombat(Unit who)
        {
            _EnterCombat();
            Talk(Jaraxxus.SayAggro);
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
                    case Jaraxxus.EventFelFireball:
                        DoCastVictim(Jaraxxus.SpellFelFireball);
                        _events.ScheduleEvent(Jaraxxus.EventFelFireball, RandomHelper.URand(10 * Time.InMilliseconds, 15 * Time.InMilliseconds));
                        return;
                    case Jaraxxus.EventFelLightning:
                        {
                            Unit target = SelectTarget(SelectAggroTarget.Random, 0, 0.0f, true, -Jaraxxus.SpellLordHittin);
                            if (target)
                                DoCast(target, Jaraxxus.SpellFelLighting);
                            _events.ScheduleEvent(Jaraxxus.EventFelLightning, RandomHelper.URand(10 * Time.InMilliseconds, 15 * Time.InMilliseconds));
                            return;
                        }
                    case Jaraxxus.EventIncinerateFlesh:
                        {
                            Unit target = SelectTarget(SelectAggroTarget.Random, 1, 0.0f, true, -Jaraxxus.SpellLordHittin);
                            if (target)
                            {
                                Talk(Jaraxxus.EmoteIncinerate, target);
                                Talk(Jaraxxus.SayIncinerate);
                                DoCast(target, Jaraxxus.SpellInfernalEruption);
                            }
                            _events.ScheduleEvent(Jaraxxus.EventIncinerateFlesh, RandomHelper.URand(20 * Time.InMilliseconds, 25 * Time.InMilliseconds));
                            return;
                        }
                    case Jaraxxus.EventNetherPower:
                        me.CastCustomSpell(Jaraxxus.SpellNetherPower, SpellValueMod.AuraStack, RaidMode(5, 10, 5, 10), me, true);
                        _events.ScheduleEvent(Jaraxxus.EventNetherPower, 40 * Time.InMilliseconds);
                        return;
                    case Jaraxxus.EventLegionFlame:
                        {
                            Unit target = SelectTarget(SelectAggroTarget.Random, 1, 0.0f, true, -Jaraxxus.SpellLordHittin);
                            if (target)
                            {
                                Talk(Jaraxxus.EmoteLegionFlame, target);
                                DoCast(target, Jaraxxus.SpellLegionFlame);
                            }
                            _events.ScheduleEvent(Jaraxxus.EventLegionFlame, 30 * Time.InMilliseconds);
                            return;
                        }
                    case Jaraxxus.EventSummonoNetherPortal:
                        Talk(Jaraxxus.EmoteNetherPortal);
                        Talk(Jaraxxus.SayMistressOfPain);
                        DoCast(Jaraxxus.SpellNetherPortal);
                        _events.ScheduleEvent(Jaraxxus.EventSummonoNetherPortal, 2 * Time.Minute * Time.InMilliseconds);
                        return;
                    case Jaraxxus.EventSummonInfernalEruption:
                        Talk(Jaraxxus.EmoteInfernalEruption);
                        Talk(Jaraxxus.SayInfernalEruption);
                        DoCast(Jaraxxus.SpellInfernalEruption);
                        _events.ScheduleEvent(Jaraxxus.EventSummonInfernalEruption, 2 * Time.Minute * Time.InMilliseconds);
                        return;
                }
            });

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class npc_legion_flame : ScriptedAI
    {
        public npc_legion_flame(Creature creature) : base(creature)
        {
            SetCombatMovement(false);
            _instance = creature.GetInstanceScript();
        }

        public override void Reset()
        {
            me.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
            me.SetInCombatWithZone();
            DoCast(Jaraxxus.SpellLegionFlameEffect);
        }

        public override void UpdateAI(uint diff)
        {
            UpdateVictim();
            if (_instance.GetBossState(DataTypes.BossJaraxxus) != EncounterState.InProgress)
                me.DespawnOrUnsummon();
        }

        InstanceScript _instance;
    }

    [Script]
    class npc_infernal_volcano : ScriptedAI
    {
        public npc_infernal_volcano(Creature creature) : base(creature)
        {
            _summons = new SummonList(me);
            SetCombatMovement(false);
        }

        public override void Reset()
        {
            me.SetReactState(ReactStates.Passive);

            if (!IsHeroic())
                me.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable | UnitFlags.Pacified);
            else
                me.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable | UnitFlags.Pacified);

            _summons.DespawnAll();
        }

        public override void IsSummonedBy(Unit summoner)
        {
            DoCast(Jaraxxus.SpellInfernalEruptionEffect);
        }

        public override void JustSummoned(Creature summoned)
        {
            _summons.Summon(summoned);
            // makes immediate corpse despawn of summoned Felflame Infernals
            summoned.SetCorpseDelay(0);
        }

        public override void JustDied(Unit killer)
        {
            // used to despawn corpse immediately
            me.DespawnOrUnsummon();
        }

        public override void UpdateAI(uint diff) { }

        SummonList _summons;
    }

    [Script]
    class npc_fel_infernal : ScriptedAI
    {
        public npc_fel_infernal(Creature creature) : base(creature)
        {
            _instance = creature.GetInstanceScript();
        }

        public override void Reset()
        {
            _felStreakTimer = 30 * Time.InMilliseconds;
            me.SetInCombatWithZone();
        }

        public override void UpdateAI(uint diff)
        {
            if (_instance.GetBossState(DataTypes.BossJaraxxus) != EncounterState.InProgress)
            {
                me.DespawnOrUnsummon();
                return;
            }

            if (!UpdateVictim())
                return;

            if (_felStreakTimer <= diff)
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0, 0.0f, true);
                if (target)
                    DoCast(target, Jaraxxus.SpellFelStreak);
                _felStreakTimer = 30 * Time.InMilliseconds;
            }
            else
                _felStreakTimer -= diff;

            DoMeleeAttackIfReady();
        }

        uint _felStreakTimer;
        InstanceScript _instance;
    }

    [Script]
    class npc_nether_portal : ScriptedAI
    {
        public npc_nether_portal(Creature creature) : base(creature)
        {
            _summons = new SummonList(me);
        }

        public override void Reset()
        {
            me.SetReactState(ReactStates.Passive);

            if (!IsHeroic())
                me.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable | UnitFlags.Pacified);
            else
                me.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable | UnitFlags.Pacified);

            _summons.DespawnAll();
        }

        public override void IsSummonedBy(Unit summoner)
        {
            DoCast(Jaraxxus.SpellNetherPortalEffect);
        }

        public override void JustSummoned(Creature summoned)
        {
            _summons.Summon(summoned);
            // makes immediate corpse despawn of summoned Mistress of Pain
            summoned.SetCorpseDelay(0);
        }

        public override void JustDied(Unit killer)
        {
            // used to despawn corpse immediately
            me.DespawnOrUnsummon();
        }

        public override void UpdateAI(uint diff) { }

        SummonList _summons;
    }

    [Script]
    class npc_mistress_of_pain : ScriptedAI
    {
        public npc_mistress_of_pain(Creature creature) : base(creature)
        {
            _instance = creature.GetInstanceScript();
            _instance.SetData(DataTypes.MistressOfPainCount, DataTypes.Increase);
        }

        public override void Reset()
        {
            _events.ScheduleEvent(Jaraxxus.EventShivanSlash, 30 * Time.InMilliseconds);
            _events.ScheduleEvent(Jaraxxus.EventSpinningStrike, 30 * Time.InMilliseconds);
            if (IsHeroic())
                _events.ScheduleEvent(Jaraxxus.EventMistressKiss, 15 * Time.InMilliseconds);
            me.SetInCombatWithZone();
        }

        public override void JustDied(Unit killer)
        {
            _instance.SetData(DataTypes.MistressOfPainCount, DataTypes.Decrease);
        }

        public override void UpdateAI(uint diff)
        {
            if (_instance.GetBossState(DataTypes.BossJaraxxus) != EncounterState.InProgress)
            {
                me.DespawnOrUnsummon();
                return;
            }

            if (!UpdateVictim())
                return;

            _events.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case Jaraxxus.EventShivanSlash:
                        DoCastVictim(Jaraxxus.SpellShivanSlash);
                        _events.ScheduleEvent(Jaraxxus.EventShivanSlash, 30 * Time.InMilliseconds);
                        return;
                    case Jaraxxus.EventSpinningStrike:
                        Unit target = SelectTarget(SelectAggroTarget.Random, 0, 0.0f, true);
                        if (target)
                            DoCast(target, Jaraxxus.SpellSpinningStrike);
                        _events.ScheduleEvent(Jaraxxus.EventSpinningStrike, 30 * Time.InMilliseconds);
                        return;
                    case Jaraxxus.EventMistressKiss:
                        DoCast(me, Jaraxxus.SpellMistressKiss);
                        _events.ScheduleEvent(Jaraxxus.EventMistressKiss, 30 * Time.InMilliseconds);
                        return;
                    default:
                        break;
                }
            });

            DoMeleeAttackIfReady();
        }

        InstanceScript _instance;
    }

    [Script]
    class spell_mistress_kiss : AuraScript
    {
        public override bool Load()
        {
            return ValidateSpellInfo(Jaraxxus.SpellMistressKissDamageSilence);
        }

        void HandleDummyTick(AuraEffect aurEff)
        {
            Unit caster = GetCaster();
            Unit target = GetTarget();
            if (caster && target)
            {
                if (target.HasUnitState(UnitState.Casting))
                {
                    caster.CastSpell(target, Jaraxxus.SpellMistressKissDamageSilence, true);
                    target.RemoveAurasDueToSpell(GetSpellInfo().Id);
                }
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleDummyTick, 0, AuraType.PeriodicDummy));
        }
    }

    [Script]
    class spell_mistress_kiss_area : SpellScript
    {
        void FilterTargets(List<WorldObject> targets)
        {
            // get a list of players with mana
            targets.RemoveAll(unit => unit.IsTypeId(TypeId.Player) && unit.ToPlayer().GetPowerType() == PowerType.Mana);
            if (targets.Empty())
                return;

            WorldObject target = targets.SelectRandom();
            targets.Clear();
            targets.Add(target);
        }

        void HandleScript(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), (uint)GetEffectValue(), true);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }
}