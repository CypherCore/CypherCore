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
using System.Collections.Generic;


namespace Scripts.Northrend.AzjolNerub.Ahnkahet.ElderNadox
{
    struct SpellIds
    {
        public const uint BroodPlague = 56130;
        public const uint HBroodRage = 59465;
        public const uint Enrage = 26662; // Enraged If Too Far Away From Home
        public const uint SummonSwarmers = 56119; // 2x 30178  -- 2x Every 10secs
        public const uint SummonSwarmGuard = 56120; // 1x 30176

        // Adds
        public const uint SwarmBuff = 56281;
        public const uint Sprint = 56354;
    }

    struct TextIds
    {
        public const uint SayAggro = 0;
        public const uint SaySlay = 1;
        public const uint SayDeath = 2;
        public const uint SayEggSac = 3;
        public const uint EmoteHatches = 4;
    }

    struct Misc
    {
        public const uint DataRespectYourElders = 6;
    }

    [Script]
    class boss_elder_nadox : BossAI
    {
        public boss_elder_nadox(Creature creature) : base(creature, DataTypes.ElderNadox)
        {
            Initialize();
        }

        void Initialize()
        {
            GuardianSummoned = false;
            GuardianDied = false;
        }

        public override void Reset()
        {
            _Reset();
            Initialize();
        }

        public override void EnterCombat(Unit who)
        {
            _EnterCombat();
            Talk(TextIds.SayAggro);

            _scheduler.Schedule(TimeSpan.FromSeconds(13), task =>
            {
                DoCast(SelectTarget(SelectAggroTarget.Random, 0, 100, true), SpellIds.BroodPlague, true);
                task.Repeat(TimeSpan.FromSeconds(15));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(10), task =>
            {
                // @todo: summoned by egg
                DoCast(me, SpellIds.SummonSwarmers);
                if (RandomHelper.URand(1, 3) == 3) // 33% chance of dialog
                    Talk(TextIds.SayEggSac);
                task.Repeat();
            });

            if (IsHeroic())
            {
                _scheduler.Schedule(TimeSpan.FromSeconds(12), task =>
                {
                    DoCast(SpellIds.HBroodRage);
                    task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(50));
                });

                _scheduler.Schedule(TimeSpan.FromSeconds(5), task =>
                {
                    if (me.HasAura(SpellIds.Enrage))
                        return;
                    if (me.GetPositionZ() < 24.0f)
                        DoCast(me, SpellIds.Enrage, true);
                    task.Repeat();
                });
            }
        }

        public override void SummonedCreatureDies(Creature summon, Unit killer)
        {
            if (summon.GetEntry() == AKCreatureIds.AhnkaharGuardian)
                GuardianDied = true;
        }

        public override uint GetData(uint type)
        {
            if (type == Misc.DataRespectYourElders)
                return !GuardianDied ? 1 : 0u;

            return 0;
        }

        public override void KilledUnit(Unit who)
        {
            if (who.IsTypeId(TypeId.Player))
                Talk(TextIds.SaySlay);
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
            Talk(TextIds.SayDeath);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            if (!GuardianSummoned && me.HealthBelowPct(50))
            {
                // @todo: summoned by egg
                Talk(TextIds.EmoteHatches, me);
                DoCast(me, SpellIds.SummonSwarmGuard);
                GuardianSummoned = true;
            }

            DoMeleeAttackIfReady();
        }

        bool GuardianSummoned;
        bool GuardianDied;
    }

    [Script]
    class npc_ahnkahar_nerubian : ScriptedAI
    {
        public npc_ahnkahar_nerubian(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _scheduler.CancelAll();
            _scheduler.Schedule(TimeSpan.FromSeconds(13), task =>
            {
                DoCast(me, SpellIds.Sprint);
                task.Repeat(TimeSpan.FromSeconds(20));
            });
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            if (me.HasUnitState(UnitState.Casting))
                return;

            _scheduler.Update(diff);

            DoMeleeAttackIfReady();
        }
    }

    // 56159 - Swarm
    [Script]
    class spell_ahn_kahet_swarm : SpellScript
    {
        public spell_ahn_kahet_swarm()
        {
            _targetCount = 0;
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SwarmBuff);
        }

        void CountTargets(List<WorldObject> targets)
        {
            _targetCount = targets.Count;
        }

        void HandleDummy(uint effIndex)
        {
            if (_targetCount != 0)
            {
                Aura aura = GetCaster().GetAura(SpellIds.SwarmBuff);
                if (aura != null)
                {
                    aura.SetStackAmount((byte)_targetCount);
                    aura.RefreshDuration();
                }
                else
                    GetCaster().CastCustomSpell(SpellIds.SwarmBuff, SpellValueMod.AuraStack, _targetCount, GetCaster(), TriggerCastFlags.FullMask);
            }
            else
                GetCaster().RemoveAurasDueToSpell(SpellIds.SwarmBuff);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(CountTargets, 0, Targets.UnitSrcAreaAlly));
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }

        int _targetCount;
    }

    [Script]
    class achievement_respect_your_elders : AchievementCriteriaScript
    {
        public achievement_respect_your_elders() : base("achievement_respect_your_elders") { }

        public override bool OnCheck(Player player, Unit target)
        {
            return target && target.GetAI().GetData(Misc.DataRespectYourElders) != 0;
        }
    }
}
