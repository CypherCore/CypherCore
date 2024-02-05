// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackwingLair.Vaelastrasz
{
    struct SpellIds
    {
        public const uint Essenceofthered = 23513;
        public const uint Flamebreath = 23461;
        public const uint Firenova = 23462;
        public const uint Tailswipe = 15847;
        public const uint Burningadrenaline = 18173;  //Cast this one. It's what 3.3.5 Dbm expects.
        public const uint BurningadrenalineExplosion = 23478;
        public const uint Cleave = 19983;  //Chain cleave is most likely named something different and contains a dummy effect
    }

    struct TextIds
    {
        public const uint SayLine1 = 0;
        public const uint SayLine2 = 1;
        public const uint SayLine3 = 2;
        public const uint SayHalflife = 3;
        public const uint SayKilltarget = 4;

        public const uint GossipId = 6101;
    }

    [Script]
    class boss_vaelastrasz : BossAI
    {
        ObjectGuid PlayerGUID;
        bool HasYelled;

        public boss_vaelastrasz(Creature creature) : base(creature, DataTypes.VaelastrazTheCorrupt)
        {
            Initialize();
            creature.SetNpcFlag(NPCFlags.Gossip);
            creature.SetFaction((uint)FactionTemplates.Friendly);
            creature.SetUninteractible(false);
        }

        void Initialize()
        {
            PlayerGUID.Clear();
            HasYelled = false;
        }

        public override void Reset()
        {
            _Reset();

            me.SetStandState(UnitStandStateType.Dead);
            Initialize();
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);

            DoCast(me, SpellIds.Essenceofthered);
            me.SetHealth(me.CountPctFromMaxHealth(30));
            // now drop damage requirement to be able to take loot
            me.ResetPlayerDamageReq();

            _scheduler.Schedule(TimeSpan.FromSeconds(10), task =>
            {
                DoCastVictim(SpellIds.Cleave);
                task.Repeat(TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(15), task =>
            {
                DoCastVictim(SpellIds.Flamebreath);
                task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(14));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(20), task =>
            {
                DoCastVictim(SpellIds.Firenova);
                task.Repeat(TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(11), task =>
            {
                //Only cast if we are behind
                if (!me.HasInArc(MathF.PI, me.GetVictim()))
                {
                    DoCast(me.GetVictim(), SpellIds.Tailswipe);
                }
                task.Repeat(TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(15), task =>
            {
                //selects a random target that isn't the current victim and is a mana user (selects mana users) but not pets
                //it also ignores targets who have the aura. We don't want to place the debuff on the same target twice.
                Unit target = SelectTarget(SelectTargetMethod.Random, 1, u => { return u != null && !u.IsPet() && u.GetPowerType() == PowerType.Mana && !u.HasAura(SpellIds.Burningadrenaline); });
                if (target != null)
                    me.CastSpell(target, SpellIds.Burningadrenaline, true);

                task.Repeat(TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(45), task =>
            {
                //Vael has to cast it himself; contrary to the previous commit's comment. Nothing happens otherwise.
                me.CastSpell(me.GetVictim(), SpellIds.Burningadrenaline, true);
                task.Repeat(TimeSpan.FromSeconds(45));
            });
        }

        void BeginSpeech(Unit target)
        {
            PlayerGUID = target.GetGUID();
            me.RemoveNpcFlag(NPCFlags.Gossip);
            _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
            {
                Talk(TextIds.SayLine1);
                me.SetStandState(UnitStandStateType.Stand);
                me.HandleEmoteCommand(Emote.OneshotTalk);
                task.Schedule(TimeSpan.FromSeconds(12), speechTask2 =>
                {
                    Talk(TextIds.SayLine2);
                    me.HandleEmoteCommand(Emote.OneshotTalk);
                    speechTask2.Schedule(TimeSpan.FromSeconds(12), speechTask3 =>
                    {
                        Talk(TextIds.SayLine3);
                        me.HandleEmoteCommand(Emote.OneshotTalk);
                        speechTask3.Schedule(TimeSpan.FromSeconds(16), speechTask4 =>
                        {
                            me.SetFaction((uint)FactionTemplates.DragonflightBlack);
                            Player player = Global.ObjAccessor.GetPlayer(me, PlayerGUID);
                            if (player != null)
                                AttackStart(player);
                        });
                    });
                });
            });
        }

        public override void KilledUnit(Unit victim)
        {
            if ((RandomHelper.Rand32() % 5) != 0)
                return;

            Talk(TextIds.SayKilltarget, victim);
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);

            if (!UpdateVictim())
                return;

            if (me.HasUnitState(UnitState.Casting))
                return;

            // Yell if hp lower than 15%
            if (HealthBelowPct(15) && !HasYelled)
            {
                Talk(TextIds.SayHalflife);
                HasYelled = true;
            }
        }

        public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            if (menuId == TextIds.GossipId && gossipListId == 0)
            {
                player.CloseGossipMenu();
                BeginSpeech(player);
            }
            return false;
        }
    }

    //Need to define an aurascript for EventBurningadrenaline's death effect.
    [Script] // 18173 - Burning Adrenaline
    class spell_vael_burning_adrenaline : AuraScript
    {
        void OnAuraRemoveHandler(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            //The tooltip says the on death the AoE occurs. According to information: http://qaliaresponse.stage.lithium.com/t5/WoW-Mayhem/Surviving-Burning-Adrenaline-For-tanks/td-p/48609
            //Burning Adrenaline can be survived therefore Blizzard's implementation was an AoE bomb that went off if you were still alive and dealt
            //damage to the target. You don't have to die for it to go off. It can go off whether you live or die.
            GetTarget().CastSpell(GetTarget(), SpellIds.BurningadrenalineExplosion, true);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(OnAuraRemoveHandler, 2, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
        }
    }
}

