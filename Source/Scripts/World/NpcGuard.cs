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
using Framework.Dynamic;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;

namespace Scripts.World.NpcGuard
{
    struct SpellIds
    {
        public const uint BanishedShattrathA = 36642;
        public const uint BanishedShattrathS = 36671;
        public const uint BanishTeleport = 36643;
        public const uint Exile = 39533;
    }

    struct TextIds
    {
        public const uint SayGuardSilAggro = 0;
    }


    struct CreatureIds
    {
        public const uint CenarionHoldInfantry = 15184;
        public const uint StormwindCityGuard = 68;
        public const uint StormwindCityPatroller = 1976;
        public const uint OrgrimmarGrunt = 3296;
        public const uint AldorVindicator = 18549;
    }

    [Script]
    class npc_guard_generic : GuardAI
    {
        TaskScheduler _combatScheduler;

        public npc_guard_generic(Creature creature) : base(creature)
        {
            _scheduler.SetValidator(() => !me.HasUnitState(UnitState.Casting) && !me.IsInEvadeMode() && me.IsAlive());
            _combatScheduler = new TaskScheduler();
            _combatScheduler.SetValidator(() => !me.HasUnitState(UnitState.Casting));
        }

        public override void Reset()
        {
            _scheduler.CancelAll();
            _combatScheduler.CancelAll();
            _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
            {
                // Find a spell that targets friendly and applies an aura (these are generally buffs)
                SpellInfo spellInfo = SelectSpell(me, 0, 0, SelectTargetType.AnyFriend, 0, 0, SelectEffect.Aura);
                if (spellInfo != null)
                    DoCast(me, spellInfo.Id);

                task.Repeat(TimeSpan.FromMinutes(10));
            });
        }

        void DoReplyToTextEmote(TextEmotes emote)
        {
            switch (emote)
            {
                case TextEmotes.Kiss:
                    me.HandleEmoteCommand(Emote.OneshotBow);
                    break;
                case TextEmotes.Wave:
                    me.HandleEmoteCommand(Emote.OneshotWave);
                    break;
                case TextEmotes.Salute:
                    me.HandleEmoteCommand(Emote.OneshotSalute);
                    break;
                case TextEmotes.Shy:
                    me.HandleEmoteCommand(Emote.OneshotFlex);
                    break;
                case TextEmotes.Rude:
                case TextEmotes.Chicken:
                    me.HandleEmoteCommand(Emote.OneshotPoint);
                    break;
                default:
                    break;
            }
        }

        public override void ReceiveEmote(Player player, TextEmotes textEmote)
        {
            switch (me.GetEntry())
            {
                case CreatureIds.StormwindCityGuard:
                case CreatureIds.StormwindCityPatroller:
                case CreatureIds.OrgrimmarGrunt:
                    break;
                default:
                    return;
            }

            if (!me.IsFriendlyTo(player))
                return;

            DoReplyToTextEmote(textEmote);
        }

        public override void EnterCombat(Unit who)
        {
            if (me.GetEntry() == CreatureIds.CenarionHoldInfantry)
                Talk(TextIds.SayGuardSilAggro, who);

            _combatScheduler.Schedule(TimeSpan.FromSeconds(1), task =>
            {
                Unit victim = me.GetVictim();
                if (!me.IsAttackReady() || !me.IsWithinMeleeRange(victim))
                {
                    task.Repeat();
                    return;
                }
                if (RandomHelper.randChance(20))
                {
                    SpellInfo spellInfo = SelectSpell(me.GetVictim(), 0, 0, SelectTargetType.AnyEnemy, 0, SharedConst.NominalMeleeRange, SelectEffect.DontCare);
                    if (spellInfo != null)
                    {
                        me.ResetAttackTimer();
                        DoCastVictim(spellInfo.Id);
                        task.Repeat();
                        return;
                    }
                }
                if (ShouldSparWith(victim))
                    me.FakeAttackerStateUpdate(victim);
                else
                    me.AttackerStateUpdate(victim);
                me.ResetAttackTimer();
                task.Repeat();
            });
            _combatScheduler.Schedule(TimeSpan.FromSeconds(5), task =>
            {
                bool healing = false;
                SpellInfo spellInfo = null;

                // Select a healing spell if less than 30% hp and Only 33% of the time
                if (me.HealthBelowPct(30) && RandomHelper.randChance(33))
                    spellInfo = SelectSpell(me, 0, 0, SelectTargetType.AnyFriend, 0, 0, SelectEffect.Healing);

                // No healing spell available, check if we can cast a ranged spell
                if (spellInfo != null)
                    healing = true;
                else
                    spellInfo = SelectSpell(me.GetVictim(), 0, 0, SelectTargetType.AnyEnemy, SharedConst.NominalMeleeRange, 0, SelectEffect.DontCare);

                // Found a spell
                if (spellInfo != null)
                {
                    if (healing)
                        DoCast(me, spellInfo.Id);
                    else
                        DoCastVictim(spellInfo.Id);
                    task.Repeat(TimeSpan.FromSeconds(5));
                }
                else
                    task.Repeat(TimeSpan.FromSeconds(1));
            });
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);

            if (!UpdateVictim())
                return;

            _combatScheduler.Update(diff);
        }
    }

    [Script]
    class npc_guard_shattrath_faction : GuardAI
    {
        public npc_guard_shattrath_faction(Creature creature) : base(creature)
        {
            _scheduler.SetValidator(() => !me.HasUnitState(UnitState.Casting));
        }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void EnterCombat(Unit who)
        {
            ScheduleVanish();
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, base.DoMeleeAttackIfReady);
        }

        void ScheduleVanish()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(5), task =>
            {
                Unit temp = me.GetVictim();
                if (temp && temp.IsTypeId(TypeId.Player))
                {
                    DoCast(temp, me.GetEntry() == CreatureIds.AldorVindicator ? SpellIds.BanishedShattrathS : SpellIds.BanishedShattrathA);
                    ObjectGuid playerGUID = temp.GetGUID();
                    task.Schedule(TimeSpan.FromSeconds(9), task =>
                    {
                        Unit temp = Global.ObjAccessor.GetUnit(me, playerGUID);
                        if (temp)
                        {
                            temp.CastSpell(temp, SpellIds.Exile, true);
                            temp.CastSpell(temp, SpellIds.BanishTeleport, true);
                        }
                        ScheduleVanish();
                    });
                }
                else
                    task.Repeat();
            });
        }
    }
}