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
        public const uint HatefulBolt = 30383;
        public const uint Evocation = 30254;
        public const uint ArcaneInfusion = 30403;
        public const uint Berserk = 26662;
        public const uint SummonAstralFlareNE = 30236;
        public const uint SummonAstralFlareNW = 30239;
        public const uint SummonAstralFlareSE = 30240;
        public const uint SummonAstralFlareSW = 30241;
    }

    struct EventIds
    {
        public const uint HatefulBolt = 1;
        public const uint SummonAstralFlare = 2;
        public const uint ArcaneInfusion = 3;
        public const uint Berserk = 4;
    }

    [Script]
    class boss_curator : BossAI
    {
        public boss_curator(Creature creature) : base(creature, DataTypes.Curator) { }

        public override void Reset()
        {
            _Reset();
            _infused = false;
            me.ApplySpellImmune(0, SpellImmunity.Damage, SpellSchoolMask.Arcane, true);
        }

        public override void KilledUnit(Unit victim)
        {
            if (victim.GetTypeId() == TypeId.Player)
                Talk(TextIds.SayKill);
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
            Talk(TextIds.SayDeath);
        }

        public override void EnterCombat(Unit victim)
        {
            _EnterCombat();
            Talk(TextIds.SayAggro);

            _events.ScheduleEvent(EventIds.HatefulBolt, TimeSpan.FromSeconds(12));
            _events.ScheduleEvent(EventIds.SummonAstralFlare, TimeSpan.FromSeconds(10));
            _events.ScheduleEvent(EventIds.Berserk, TimeSpan.FromMinutes(12));
        }

        public override void DamageTaken(Unit attacker, ref uint damage)
        {
            if (!HealthAbovePct(15) && !_infused)
            {
                _infused = true;
                _events.ScheduleEvent(EventIds.ArcaneInfusion, TimeSpan.FromMilliseconds(1));
                _events.CancelEvent(EventIds.SummonAstralFlare);
            }
        }

        public override void ExecuteEvent(uint eventId)
        {
            switch (eventId)
            {
                case EventIds.HatefulBolt:
                    Unit target = SelectTarget(SelectAggroTarget.MaxThreat, 1);
                    if (target != null)
                        DoCast(target, SpellIds.HatefulBolt);
                    _events.Repeat(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(15));
                    break;
                case EventIds.ArcaneInfusion:
                    DoCastSelf(SpellIds.ArcaneInfusion, true);
                    break;
                case EventIds.SummonAstralFlare:
                    if (RandomHelper.randChance(50))
                        Talk(TextIds.SaySummon);


                    DoCastSelf(RandomHelper.RAND(SpellIds.SummonAstralFlareNE, SpellIds.SummonAstralFlareNW, SpellIds.SummonAstralFlareSE, SpellIds.SummonAstralFlareSW), true);

                    int mana = me.GetMaxPower(PowerType.Mana) / 10;
                    if (mana != 0)
                    {
                        me.ModifyPower(PowerType.Mana, -mana);

                        if (me.GetPower(PowerType.Mana) * 100 / me.GetMaxPower(PowerType.Mana) < 10)
                        {
                            Talk(TextIds.SayEvocate);
                            me.InterruptNonMeleeSpells(false);
                            DoCastSelf(SpellIds.Evocation);
                        }
                    }
                    _events.Repeat(TimeSpan.FromSeconds(10));
                    break;
                case EventIds.Berserk:
                    Talk(TextIds.SayEnrage);
                    DoCastSelf(SpellIds.Berserk, true);
                    break;
                default:
                    break;
            }
        }

        bool _infused;
    }

    [Script]
    class npc_curator_astral_flareAI : ScriptedAI
    {
        public npc_curator_astral_flareAI(Creature creature) : base(creature)
        {
            me.SetReactState(ReactStates.Passive);
        }

        public override void Reset()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(2), task =>
            {
                me.SetReactState(ReactStates.Aggressive);
                me.RemoveUnitFlag(UnitFlags.NotSelectable);
                DoZoneInCombat();
            });
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }
}
