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

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore.BaronGeddon
{
    struct SpellIds
    {
        public const uint Inferno = 19695;
        public const uint InfernoDmg = 19698;
        public const uint IgniteMana = 19659;
        public const uint LivingBomb = 20475;
        public const uint Armageddon = 20478;
    }

    struct TextIds
    {
        public const uint EmoteService = 0;
    }

    [Script]
    class boss_baron_geddon : BossAI
    {
        public boss_baron_geddon(Creature creature) : base(creature, DataTypes.BaronGeddon) { }

        public override void JustEngagedWith(Unit victim)
        {
            base.JustEngagedWith(victim);

            _scheduler.Schedule(TimeSpan.FromSeconds(45), task =>
            {
                DoCast(me, SpellIds.Inferno);
                task.Repeat(TimeSpan.FromSeconds(45));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(30), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true, true, -(int)SpellIds.IgniteMana);
                if (target)
                    DoCast(target, SpellIds.IgniteMana);
                task.Repeat(TimeSpan.FromSeconds(30));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(35), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);
                if (target)
                    DoCast(target, SpellIds.LivingBomb);
                task.Repeat(TimeSpan.FromSeconds(35));
            });
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            // If we are <2% hp cast Armageddon
            if (!HealthAbovePct(2))
            {
                me.InterruptNonMeleeSpells(true);
                DoCast(me, SpellIds.Armageddon);
                Talk(TextIds.EmoteService);
                return;
            }

            if (me.HasUnitState(UnitState.Casting))
                return;

            DoMeleeAttackIfReady();
        }
    }

    [Script] // 19695 - Inferno
    class spell_baron_geddon_inferno : AuraScript
    {
        void OnPeriodic(AuraEffect aurEff)
        {
            PreventDefaultAction();
            int[] damageForTick = { 500, 500, 1000, 1000, 2000, 2000, 3000, 5000 };
            CastSpellExtraArgs args = new CastSpellExtraArgs(TriggerCastFlags.FullMask);
            args.TriggeringAura = aurEff;
            args.AddSpellMod(SpellValueMod.BasePoint0, damageForTick[aurEff.GetTickNumber() - 1]);
            GetTarget().CastSpell((WorldObject)null, SpellIds.InfernoDmg, args);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicTriggerSpell));
        }
    }
}

