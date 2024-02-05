// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore.Golemagg
{
    struct SpellIds
    {
        // Golemagg
        public const uint Magmasplash = 13879;
        public const uint Pyroblast = 20228;
        public const uint Earthquake = 19798;
        public const uint Enrage = 19953;
        public const uint GolemaggTrust = 20553;

        // Core Rager
        public const uint Mangle = 19820;
    }

    struct TextIds
    {
        public const uint EmoteLowhp = 0;
    }

    [Script]
    class boss_golemagg : BossAI
    {
        public boss_golemagg(Creature creature) : base(creature, DataTypes.GolemaggTheIncinerator) { }

        public override void Reset()
        {
            base.Reset();
            DoCast(me, SpellIds.Magmasplash, new CastSpellExtraArgs(true));
        }

        public override void JustEngagedWith(Unit victim)
        {
            base.JustEngagedWith(victim);
            _scheduler.Schedule(TimeSpan.FromSeconds(7), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0);
                if (target != null)
                    DoCast(target, SpellIds.Pyroblast);
                task.Repeat(TimeSpan.FromSeconds(7));
            });
        }

        public override void DamageTaken(Unit attacker, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            if (!HealthBelowPct(10) || me.HasAura(SpellIds.Enrage))
                return;

            DoCast(me, SpellIds.Enrage, new CastSpellExtraArgs(true));
            _scheduler.Schedule(TimeSpan.FromSeconds(3), task =>
            {
                DoCastVictim(SpellIds.Earthquake);
                task.Repeat(TimeSpan.FromSeconds(3));
            });
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);
        }
    }

    [Script]
    class npc_core_rager : ScriptedAI
    {
        InstanceScript _instance;

        public npc_core_rager(Creature creature) : base(creature)
        {
            _instance = creature.GetInstanceScript();
        }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void JustEngagedWith(Unit who)
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(7), task => // These times are probably wrong
        {
            DoCastVictim(SpellIds.Mangle);
            task.Repeat(TimeSpan.FromSeconds(10));
        });
        }

        public override void DamageTaken(Unit attacker, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            if (HealthAbovePct(50) || _instance == null)
                return;

            Creature pGolemagg = ObjectAccessor.GetCreature(me, _instance.GetGuidData(DataTypes.GolemaggTheIncinerator));
            if (pGolemagg != null)
            {
                if (pGolemagg.IsAlive())
                {
                    me.AddAura(SpellIds.GolemaggTrust, me);
                    Talk(TextIds.EmoteLowhp);
                    me.SetFullHealth();
                }
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);
        }
    }
}

