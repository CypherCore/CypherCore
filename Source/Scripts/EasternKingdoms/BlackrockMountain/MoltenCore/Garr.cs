/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore.Garr
{
    struct SpellIds
    {
        // Garr
        public const uint AntimagicPulse = 19492;
        public const uint MagmaShackles = 19496;
        public const uint Enrage = 19516;
        public const uint SeparationAnxiety = 23492;

        // Adds
        public const uint Eruption = 19497;
        public const uint Immolate = 15732;
    }

    [Script]
    class boss_garr : BossAI
    {
        public boss_garr(Creature creature) : base(creature, DataTypes.Garr) { }

        public override void JustEngagedWith(Unit victim)
        {
            base.JustEngagedWith(victim);

            _scheduler.Schedule(TimeSpan.FromSeconds(25), task =>
            {
                DoCast(me, SpellIds.AntimagicPulse);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(15), task =>
            {
                DoCast(me, SpellIds.MagmaShackles);
                task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(12));
            });
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }

    [Script]
    class npc_firesworn : ScriptedAI
    {
        public npc_firesworn(Creature creature) : base(creature) { }

        void ScheduleTasks()
        {
            // Timers for this are probably wrong
            _scheduler.Schedule(TimeSpan.FromSeconds(4), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0);
                if (target)
                    DoCast(target, SpellIds.Immolate);

                task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));
            });

            // Separation Anxiety - Periodically check if Garr is nearby
            // ...and enrage if he is not.
            _scheduler.Schedule(TimeSpan.FromSeconds(3), task =>
            {
                if (!me.FindNearestCreature(MCCreatureIds.Garr, 20.0f))
                    DoCastSelf(SpellIds.SeparationAnxiety);
                else if (me.HasAura(SpellIds.SeparationAnxiety))
                    me.RemoveAurasDueToSpell(SpellIds.SeparationAnxiety);

                task.Repeat();
            });
        }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void JustEngagedWith(Unit who)
        {
            ScheduleTasks();
        }

        public override void DamageTaken(Unit attacker, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            ulong health10pct = me.CountPctFromMaxHealth(10);
            ulong health = me.GetHealth();
            if (health - damage < health10pct)
            {
                damage = 0;
                DoCastVictim(SpellIds.Eruption);
                me.DespawnOrUnsummon();
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }
}

