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
using System;

namespace Scripts.EasternKingdoms.Karazhan.Curator
{
    struct TextIds
    {
        public const uint SayAggro = 0;
        public const uint SaySummon = 1;
        public const uint SayEvocate = 2;
        public const uint SayEnrage = 3;
        public const uint SayKill = 4;
        public const uint SayDeath = 5;
    }

    struct SpellIds
    {
        //Flare spell info
        public const uint AstralFlarePassive = 30234;               //Visual effect + Flare damage

        //Curator spell info
        public const uint HatefulBolt = 30383;
        public const uint Evocation = 30254;
        public const uint Enrage = 30403;               //Arcane Infusion: Transforms Curator and adds damage.
        public const uint Berserk = 26662;
    }

    [Script]
    class boss_curator : ScriptedAI
    {
        public boss_curator(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(10), task =>
            {
                //Summon Astral Flare
                Creature AstralFlare = DoSpawnCreature(17096, RandomHelper.Rand32() % 37, RandomHelper.Rand32() % 37, 0, 0, TempSummonType.TimedDespawnOOC, 5000);
                Unit target = SelectTarget(SelectAggroTarget.Random, 0);

                if (AstralFlare && target)
                {
                    AstralFlare.CastSpell(AstralFlare, SpellIds.AstralFlarePassive, false);
                    AstralFlare.GetAI().AttackStart(target);
                }

                //Reduce Mana by 10% of max health
                int mana = me.GetMaxPower(PowerType.Mana);
                if (mana != 0)
                {
                    mana /= 10;
                    me.ModifyPower(PowerType.Mana, -mana);

                    //if this get's us below 10%, then we evocate (the 10th should be summoned now)
                    if (me.GetPower(PowerType.Mana) * 100 / me.GetMaxPower(PowerType.Mana) < 10)
                    {
                        Talk(TextIds.SayEvocate);
                        me.InterruptNonMeleeSpells(false);
                        DoCast(me, SpellIds.Evocation);
                        _scheduler.DelayAll(TimeSpan.FromSeconds(20));
                        //Evocating = true;
                        //no AddTimer cooldown, this will make first flare appear instantly after evocate end, like expected
                        return;
                    }
                    else
                    {
                        if (RandomHelper.URand(0, 1) == 0)
                        {
                            Talk(TextIds.SaySummon);
                        }
                    }
                }

                task.Repeat();
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(15), task =>
            {
                if (Enraged)
                    task.Repeat(TimeSpan.FromSeconds(7));
                else
                    task.Repeat();

                Unit target = SelectTarget(SelectAggroTarget.TopAggro, 1);
                if (target)
                    DoCast(target, SpellIds.HatefulBolt);
            });

            Enraged = false;
        }

        public override void Reset()
        {
            Initialize();

            me.ApplySpellImmune(0, SpellImmunity.Damage, SpellSchoolMask.Arcane, true);
        }

        public override void KilledUnit(Unit victim)
        {
            Talk(TextIds.SayKill);
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.SayDeath);
        }

        public override void EnterCombat(Unit victim)
        {
            Talk(TextIds.SayAggro);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            if (!Enraged)
            {
                if (!HealthAbovePct(15))
                {
                    Enraged = true;
                    DoCast(me, SpellIds.Enrage);
                    Talk(TextIds.SayEnrage);
                }
            }

        }

        bool Enraged;
    }
}
