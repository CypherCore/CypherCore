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

namespace Scripts.World
{
    public struct GuardsConst
    {
        public const int CreatureCooldown = 5000;
        public const int SaySilAggro = 0;
    }

    struct CreatureIds
    {
        public const int CenarionHoldIndantry = 15184;
        public const int StormwindCityGuard = 68;
        public const int StormwindCityPatroller = 1976;
        public const int OrgimmarGrunt = 3296;
    }

    struct Spells
    {
        public const uint BanishedA = 36642;
        public const uint BanishedS = 36671;
        public const uint BanishTeleport = 36643;
        public const uint Exile = 39533;
    }

    [Script]
    class guard_generic : GuardAI
    {
        public guard_generic(Creature creature) : base(creature) { }

        public override void Reset()
        {
            globalCooldown = 0;
            buffTimer = 0;
        }

        public override void EnterCombat(Unit who)
        {
            if (me.GetEntry() == CreatureIds.CenarionHoldIndantry)
                Talk(GuardsConst.SaySilAggro, who);
            SpellInfo spell = me.reachWithSpellAttack(who);
            if (spell != null)
                DoCast(who, spell.Id);
        }

        public override void UpdateAI(uint diff)
        {
            //Always decrease our global cooldown first
            if (globalCooldown > diff)
                globalCooldown -= diff;
            else
                globalCooldown = 0;

            //Buff timer (only buff when we are alive and not in combat
            if (me.IsAlive() && !me.IsInCombat())
            {
                if (buffTimer <= diff)
                {
                    //Find a spell that targets friendly and applies an aura (these are generally buffs)
                    SpellInfo info = SelectSpell(me, 0, 0, SelectTargetType.AnyFriend, 0, 0, SelectEffect.Aura);

                    if (info != null && globalCooldown == 0)
                    {
                        //Cast the buff spell
                        DoCast(me, info.Id);

                        //Set our global cooldown
                        globalCooldown = GuardsConst.CreatureCooldown;

                        //Set our timer to 10 minutes before rebuff
                        buffTimer = 600000;
                    }                                                   //Try again in 30 seconds
                    else buffTimer = 30000;
                }
                else buffTimer -= diff;
            }

            //Return since we have no target
            if (!UpdateVictim())
                return;

            // Make sure our attack is ready and we arn't currently casting
            if (me.isAttackReady() && !me.IsNonMeleeSpellCast(false))
            {
                //If we are within range melee the target
                if (me.IsWithinMeleeRange(me.GetVictim()))
                {
                    bool healing = false;
                    SpellInfo info = null;

                    //Select a healing spell if less than 30% hp
                    if (me.HealthBelowPct(30))
                        info = SelectSpell(me, 0, 0, SelectTargetType.AnyFriend, 0, 0, SelectEffect.Healing);

                    //No healing spell available, select a hostile spell
                    if (info != null)
                        healing = true;
                    else
                        info = SelectSpell(me.GetVictim(), 0, 0, SelectTargetType.AnyEnemy, 0, 0, SelectEffect.DontCare);

                    //20% chance to replace our white hit with a spell
                    if (info != null && RandomHelper.IRand(0, 99) < 20 && globalCooldown == 0)
                    {
                        //Cast the spell
                        if (healing)
                            DoCast(me, info.Id);
                        else
                            DoCastVictim(info.Id);

                        //Set our global cooldown
                        globalCooldown = GuardsConst.CreatureCooldown;
                    }
                    else
                        me.AttackerStateUpdate(me.GetVictim());

                    me.resetAttackTimer();
                }
            }
            else
            {
                //Only run this code if we arn't already casting
                if (!me.IsNonMeleeSpellCast(false))
                {
                    bool healing = false;
                    SpellInfo info = null;

                    //Select a healing spell if less than 30% hp ONLY 33% of the time
                    if (me.HealthBelowPct(30) && 33 > RandomHelper.IRand(0, 99))
                        info = SelectSpell(me, 0, 0, SelectTargetType.AnyFriend, 0, 0, SelectEffect.Healing);

                    //No healing spell available, See if we can cast a ranged spell (Range must be greater than ATTACK_DISTANCE)
                    if (info != null)
                        healing = true;
                    else
                        info = SelectSpell(me.GetVictim(), 0, 0, SelectTargetType.AnyEnemy, SharedConst.NominalMeleeRange, 0, SelectEffect.DontCare);

                    //Found a spell, check if we arn't on cooldown
                    if (info != null && globalCooldown == 0)
                    {
                        //If we are currently moving stop us and set the movement generator
                        if (me.GetMotionMaster().GetCurrentMovementGeneratorType() != MovementGeneratorType.Idle)
                        {
                            me.GetMotionMaster().Clear(false);
                            me.GetMotionMaster().MoveIdle();
                        }

                        //Cast spell
                        if (healing)
                            DoCast(me, info.Id);
                        else
                            DoCastVictim(info.Id);

                        //Set our global cooldown
                        globalCooldown = GuardsConst.CreatureCooldown;
                    }                                               //If no spells available and we arn't moving run to target
                    else if (me.GetMotionMaster().GetCurrentMovementGeneratorType() != MovementGeneratorType.Chase)
                    {
                        //Cancel our current spell and then mutate new movement generator
                        me.InterruptNonMeleeSpells(false);
                        me.GetMotionMaster().Clear(false);
                        me.GetMotionMaster().MoveChase(me.GetVictim());
                    }
                }
            }

            DoMeleeAttackIfReady();
        }

        public void DoReplyToTextEmote(TextEmotes emote)
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
            }
        }

        public override void ReceiveEmote(Player player, TextEmotes textEmote)
        {
            switch (me.GetEntry())
            {
                case CreatureIds.StormwindCityGuard:
                case CreatureIds.StormwindCityPatroller:
                case CreatureIds.OrgimmarGrunt:
                    break;
                default:
                    return;
            }

            if (!me.IsFriendlyTo(player))
                return;

            DoReplyToTextEmote(textEmote);
        }

        uint globalCooldown;
        uint buffTimer;
    }

    [Script]
    class guard_shattrath_scryer : GuardAI
    {
        public guard_shattrath_scryer(Creature creature) : base(creature) { }

        public override void Reset()
        {
            playerGUID.Clear();
            canTeleport = false;

            _scheduler.Schedule(TimeSpan.FromSeconds(5), task =>
            {
                Unit temp = me.GetVictim();
                if (temp && temp.IsTypeId(TypeId.Player))
                {
                    DoCast(temp, Spells.BanishedA);
                    playerGUID = temp.GetGUID();
                    if (!playerGUID.IsEmpty())
                        canTeleport = true;

                    task.Repeat(TimeSpan.FromSeconds(9));
                }
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(8.5), task =>
            {
                if (canTeleport)
                {
                    Unit temp = Global.ObjAccessor.GetUnit(me, playerGUID);
                    if (temp)
                    {
                        temp.CastSpell(temp, Spells.Exile, true);
                        temp.CastSpell(temp, Spells.BanishTeleport, true);
                    }
                    playerGUID.Clear();
                    canTeleport = false;

                    task.Repeat();
                }
            });
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            DoMeleeAttackIfReady();
        }

        ObjectGuid playerGUID;
        bool canTeleport;
    }

    [Script]
    class guard_shattrath_aldor : GuardAI
    {
        public guard_shattrath_aldor(Creature creature) : base(creature) { }

        public override void Reset()
        {
            playerGUID.Clear();
            canTeleport = false;

            _scheduler.Schedule(TimeSpan.FromSeconds(5), task =>
            {
                Unit temp = me.GetVictim();
                if (temp && temp.IsTypeId(TypeId.Player))
                {
                    DoCast(temp, Spells.BanishedA);
                    playerGUID = temp.GetGUID();
                    if (!playerGUID.IsEmpty())
                        canTeleport = true;

                    task.Repeat(TimeSpan.FromSeconds(9));
                }
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(8.5), task =>
            {
                if (canTeleport)
                {
                    Unit temp = Global.ObjAccessor.GetUnit(me, playerGUID);
                    if (temp)
                    {
                        temp.CastSpell(temp, Spells.Exile, true);
                        temp.CastSpell(temp, Spells.BanishTeleport, true);
                    }
                    playerGUID.Clear();
                    canTeleport = false;

                    task.Repeat();
                }
            });
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            DoMeleeAttackIfReady();
        }

        ObjectGuid playerGUID;
        bool canTeleport;
    }
}
