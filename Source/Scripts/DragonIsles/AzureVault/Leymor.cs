// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;

namespace Scripts.DragonIsles.AzureVault.Leymor
{
    struct SpellIds
    {
        // Leymor
        public const uint Stasis = 375729;
        public const uint ArcaneEruption = 375749;
        public const uint LeyLineSprouts = 374364;
        public const uint LeyLineSproutsMissile = 374362;
        public const uint ConsumingStomp = 374720;
        public const uint ConsumingStompDamage = 374731;
        public const uint EruptingFissure = 386660;
        public const uint EruptingFissureSproutSelector = 394154;
        public const uint ExplosiveBrand = 374567;
        public const uint ExplosiveBrandDamage = 374570;
        public const uint ExplosiveBrandKnockback = 374582;
        public const uint InfusedStrike = 374789;

        // Ley-Line Sprout
        public const uint VolatileSapling = 388654;
        public const uint LeyLineSproutAt = 374161;
        public const uint ArcanePower = 374736;

        // Volatile Sapling
        public const uint SappyBurst = 375591;

        // Arcane Tender
        public const uint StasisRitual = 375732;
        public const uint StasisRitualMissile = 375738;
        public const uint ErraticGrowthChannel = 375596;
        public const uint WildEruption = 375652;
        public const uint WildEruptionMissile = 375650;
    }

    struct MiscConst
    {
        public const uint SayAnnounceAwaken = 0;

        public const uint SpellVisualKitSproutDeath = 159239;

        public const uint NpcLeylineSprouts = 190509;

        public const int ActionArcaneTenderDeath = 1;
    }

    [Script] // 186644 - Leymor
    class boss_leymor : BossAI
    {
        int _killedArcaneTender;

        public boss_leymor(Creature creature) : base(creature, DataTypes.Leymor) { }

        public override void JustAppeared()
        {
            if (instance.GetData(DataTypes.LeymorIntroDone) != 0)
                return;

            me.SetUnitFlag(UnitFlags.ImmuneToPc | UnitFlags.ImmuneToNpc);
            DoCastSelf(SpellIds.Stasis);
        }

        public override void DoAction(int action)
        {
            if (action == MiscConst.ActionArcaneTenderDeath)
            {
                _killedArcaneTender++;
                if (_killedArcaneTender >= 3)
                {
                    instance.SetData(DataTypes.LeymorIntroDone, 1);

                    _scheduler.Schedule(TimeSpan.FromSeconds(1), _ =>
                    {
                        me.RemoveAurasDueToSpell(SpellIds.Stasis);
                        me.RemoveUnitFlag(UnitFlags.ImmuneToPc | UnitFlags.ImmuneToNpc);
                        DoCastSelf(SpellIds.ArcaneEruption);
                        Talk(MiscConst.SayAnnounceAwaken);
                    });
                }
            }
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
            instance.SendEncounterUnit(EncounterFrameType.Disengage, me);
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            instance.SendEncounterUnit(EncounterFrameType.Disengage, me);

            summons.DespawnAll();
            _EnterEvadeMode();
            _DespawnAtEvade();
        }

        public override void OnChannelFinished(SpellInfo spell)
        {
            if (spell.Id == SpellIds.ConsumingStomp)
                DoCastAOE(SpellIds.ConsumingStompDamage, true);
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);
            _scheduler.CancelAll();
            _scheduler.Schedule(TimeSpan.FromSeconds(3), task =>
            {
                DoCastSelf(SpellIds.LeyLineSprouts);
                task.Repeat(TimeSpan.FromSeconds(48));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(45), task =>
            {
                DoCastSelf(SpellIds.ConsumingStomp);
                task.Repeat(TimeSpan.FromSeconds(48));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(20), task =>
            {
                DoCastVictim(SpellIds.EruptingFissure);
                task.Repeat(TimeSpan.FromSeconds(48));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(31), task =>
            {
                DoCastSelf(SpellIds.ExplosiveBrand);
                task.Repeat(TimeSpan.FromSeconds(48));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(10), task =>
            {
                DoCastVictim(SpellIds.InfusedStrike);
                task.Repeat(TimeSpan.FromSeconds(48));
            });
            instance.SendEncounterUnit(EncounterFrameType.Engage, me, 1);
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff, DoMeleeAttackIfReady);
        }
    }

    [Script] // 191164 - Arcane Tender
    class npc_arcane_tender : ScriptedAI
    {
        public npc_arcane_tender(Creature creature) : base(creature) { }

        public override void JustDied(Unit killer)
        {
            Creature leymor = me.GetInstanceScript().GetCreature(DataTypes.Leymor);
            if (leymor == null)
                return;

            if (!leymor.IsAIEnabled())
                return;

            leymor.GetAI().DoAction(MiscConst.ActionArcaneTenderDeath);
        }

        public override void JustAppeared()
        {
            Creature leymor = me.GetInstanceScript().GetCreature(DataTypes.Leymor);
            if (leymor == null)
                return;

            DoCast(leymor, SpellIds.StasisRitual);
        }

        public override void JustReachedHome()
        {
            JustAppeared();
        }

        public override void Reset()
        {
            _events.Reset();
        }

        public override void JustEngagedWith(Unit who)
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(22), task =>
            {
                DoCastAOE(SpellIds.ErraticGrowthChannel);
                task.Repeat();
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(12), task =>
            {
                DoCastAOE(SpellIds.WildEruption);
                task.Repeat();
            });
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, DoMeleeAttackIfReady);
        }
    }

    [Script] // 190509 - Ley-Line Sprout
    class npc_ley_line_sprouts : ScriptedAI
    {
        public npc_ley_line_sprouts(Creature creature) : base(creature) { }

        public override void JustAppeared()
        {
            DoCastSelf(SpellIds.LeyLineSproutAt);

            DoCastAOE(SpellIds.ArcanePower, true);
        }

        public override void JustSummoned(Creature summon)
        {
            Creature leymor = me.GetInstanceScript().GetCreature(DataTypes.Leymor);
            if (leymor == null)
                return;

            if (!leymor.IsAIEnabled())
                return;

            leymor.GetAI().JustSummoned(summon);
        }

        public override void JustDied(Unit killer)
        {
            if (GetDifficulty() == Difficulty.Mythic || GetDifficulty() == Difficulty.MythicKeystone)
                DoCastAOE(SpellIds.VolatileSapling, true);

            TempSummon tempSummon = me.ToTempSummon();
            if (tempSummon != null)
            {
                Unit summoner = tempSummon.GetSummonerUnit();
                if (summoner != null)
                {
                    Aura aura = summoner.GetAura(SpellIds.ArcanePower);
                    if (aura != null)
                        aura.ModStackAmount(-1);
                }
            }
        }

        public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
        {
            if (spellInfo.Id != SpellIds.ExplosiveBrandDamage && spellInfo.Id != SpellIds.EruptingFissureSproutSelector)
                return;

            me.SendPlaySpellVisualKit(MiscConst.SpellVisualKitSproutDeath, 0, 0);
            me.KillSelf();
        }
    }

    [Script] // 196559 - Volatile Sapling
    class npc_volatile_sapling : ScriptedAI
    {
        public npc_volatile_sapling(Creature creature) : base(creature) { }

        public override void DamageTaken(Unit attacker, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            if (me.GetHealth() <= damage)
            {
                damage = (uint)(me.GetHealth() - 1);

                if (!_isSappyBurstCast)
                {
                    me.CastSpell(null, SpellIds.SappyBurst, false);
                    _isSappyBurstCast = true;
                }
            }
        }

        public override void OnSpellCast(SpellInfo spell)
        {
            if (spell.Id != SpellIds.SappyBurst)
                return;

            me.KillSelf();
        }


        bool _isSappyBurstCast;
    }

    [Script] // 374364 - Ley-Line Sprouts
    class spell_ley_line_sprouts : SpellScript
    {
        Position[] LeyLineSproutGroupOrigin =
        {
            new(-5129.39f, 1253.30f, 555.58f),
            new(-5101.68f, 1253.71f, 555.90f),
            new(-5114.70f, 1230.28f, 555.89f),
            new(-5141.62f, 1230.33f, 555.83f),
            new(-5155.62f, 1253.60f, 555.87f),
            new(-5141.42f, 1276.70f, 555.89f),
            new(-5114.78f, 1277.42f, 555.87f)
        };

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LeyLineSproutsMissile);
        }

        void HandleHit(uint effIndex)
        {
            foreach (Position pos in LeyLineSproutGroupOrigin)
            {
                for (int i = 0; i < 2; i++)
                    GetCaster().CastSpell(pos, SpellIds.LeyLineSproutsMissile, true);
            }
        }

        public override void Register()
        {
            OnEffectHit.Add(new(HandleHit, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 375732 - Stasis Ritual
    class spell_stasis_ritual : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.StasisRitualMissile);
        }

        void HandlePeriodic(AuraEffect aurEff)
        {
            Unit caster = GetCaster();
            if (caster != null)
                caster.CastSpell(null, SpellIds.StasisRitualMissile, true);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(HandlePeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    [Script] // 375652 - Wild Eruption
    class spell_wild_eruption : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.WildEruptionMissile);
        }

        void HandleHitTarget(uint effIndex)
        {
            GetCaster().CastSpell(GetHitDest(), SpellIds.WildEruptionMissile, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleHitTarget, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 375749 - Arcane Eruption
    class at_leymor_arcane_eruption : AreaTriggerAI
    {
        public at_leymor_arcane_eruption(AreaTrigger areatrigger) : base(areatrigger) { }

        public override void OnUnitEnter(Unit unit)
        {
            if (!unit.IsPlayer())
                return;

            unit.ApplyMovementForce(at.GetGUID(), at.GetPosition(), -20.0f, MovementForceType.Gravity);
        }

        public override void OnUnitExit(Unit unit)
        {
            if (!unit.IsPlayer())
                return;

            unit.RemoveMovementForce(at.GetGUID());
        }
    }

    [Script] // 374567 - Explosive Brand
    class spell_explosive_brand : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ExplosiveBrandKnockback);
        }

        void HandleHit(uint effIndex)
        {
            GetCaster().CastSpell(null, SpellIds.ExplosiveBrandKnockback, true);
        }

        public override void Register()
        {
            OnEffectHit.Add(new(HandleHit, 0, SpellEffectName.ApplyAura));
        }
    }

    [Script] // 374567 - Explosive Brand
    class spell_explosive_brand_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ExplosiveBrandDamage);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
                return;

            Unit caster = GetCaster();
            if (caster != null)
                caster.CastSpell(GetTarget(), SpellIds.ExplosiveBrandDamage, true);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 374720 - Consuming Stomp
    class spell_consuming_stomp : AuraScript
    {
        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().KillSelf();
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(OnRemove, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 386660 - Erupting Fissure
    class spell_erupting_fissure : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EruptingFissureSproutSelector);
        }

        void HandleHit(uint effIndex)
        {
            GetCaster().CastSpell(GetHitDest(), SpellIds.EruptingFissureSproutSelector, true);
        }

        public override void Register()
        {
            OnEffectHit.Add(new(HandleHit, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 375591 - Sappy Burst
    class spell_sappy_burst : SpellScript
    {
        void HandleHitTarget(uint effIndex)
        {
            GetCaster().KillSelf();
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleHitTarget, 2, SpellEffectName.ScriptEffect));
        }
    }
}