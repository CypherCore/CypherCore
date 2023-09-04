// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore
{
    struct SpellIds
    {
        public const uint HandOfRagnaros = 19780;
        public const uint WrathOfRagnaros = 20566;
        public const uint LavaBurst = 21158;
        public const uint MagmaBlast = 20565;                   // Ranged attack
        public const uint SonsOfFlameDummy = 21108;                   // Server side effect
        public const uint Ragsubmerge = 21107;                   // Stealth aura
        public const uint Ragemerge = 20568;
        public const uint MeltWeapon = 21388;
        public const uint ElementalFire = 20564;
        public const uint Erruption = 17731;
    }

    struct TextIds
    {
        public const uint SaySummonMaj = 0;
        public const uint SayArrival1Rag = 1;
        public const uint SayArrival2Maj = 2;
        public const uint SayArrival3Rag = 3;
        public const uint SayArrival5Rag = 4;
        public const uint SayReinforcements1 = 5;
        public const uint SayReinforcements2 = 6;
        public const uint SayHand = 7;
        public const uint SayWrath = 8;
        public const uint SayKill = 9;
        public const uint SayMagmaburst = 10;
    }

    struct EventIds
    {
        public const uint Eruption = 1;
        public const uint WrathOfRagnaros = 2;
        public const uint HandOfRagnaros = 3;
        public const uint LavaBurst = 4;
        public const uint ElementalFire = 5;
        public const uint MagmaBlast = 6;
        public const uint Submerge = 7;

        public const uint Intro1 = 8;
        public const uint Intro2 = 9;
        public const uint Intro3 = 10;
        public const uint Intro4 = 11;
        public const uint Intro5 = 12;
    }

    [Script]
    class boss_ragnaros : BossAI
    {
        uint _emergeTimer;
        byte _introState;
        bool _hasYelledMagmaBurst;
        bool _hasSubmergedOnce;
        bool _isBanished;

        public boss_ragnaros(Creature creature) : base(creature, DataTypes.Ragnaros)
        {
            Initialize();
            _introState = 0;
            me.SetReactState(ReactStates.Passive);
            me.SetUnitFlag(UnitFlags.NonAttackable);
            SetCombatMovement(false);
        }

        void Initialize()
        {
            _emergeTimer = 90000;
            _hasYelledMagmaBurst = false;
            _hasSubmergedOnce = false;
            _isBanished = false;
        }

        public override void Reset()
        {
            base.Reset();
            Initialize();
            me.SetEmoteState(Emote.OneshotNone);
        }

        public override void JustEngagedWith(Unit victim)
        {
            base.JustEngagedWith(victim);
            _events.ScheduleEvent(EventIds.Eruption, TimeSpan.FromSeconds(15));
            _events.ScheduleEvent(EventIds.WrathOfRagnaros, TimeSpan.FromSeconds(30));
            _events.ScheduleEvent(EventIds.HandOfRagnaros, TimeSpan.FromSeconds(25));
            _events.ScheduleEvent(EventIds.LavaBurst, TimeSpan.FromSeconds(10));
            _events.ScheduleEvent(EventIds.ElementalFire, TimeSpan.FromSeconds(3));
            _events.ScheduleEvent(EventIds.MagmaBlast, TimeSpan.FromSeconds(2));
            _events.ScheduleEvent(EventIds.Submerge, TimeSpan.FromMinutes(3));
        }

        public override void KilledUnit(Unit victim)
        {
            if (RandomHelper.URand(0, 99) < 25)
                Talk(TextIds.SayKill);
        }

        public override void UpdateAI(uint diff)
        {
            if (_introState != 2)
            {
                if (_introState == 0)
                {
                    me.HandleEmoteCommand(Emote.OneshotEmerge);
                    _events.ScheduleEvent(EventIds.Intro1, TimeSpan.FromSeconds(4));
                    _events.ScheduleEvent(EventIds.Intro2, TimeSpan.FromSeconds(23));
                    _events.ScheduleEvent(EventIds.Intro3, TimeSpan.FromSeconds(42));
                    _events.ScheduleEvent(EventIds.Intro4, TimeSpan.FromSeconds(43));
                    _events.ScheduleEvent(EventIds.Intro5, TimeSpan.FromSeconds(53));
                    _introState = 1;
                }

                _events.Update(diff);

                _events.ExecuteEvents(eventId =>
                {
                    switch (eventId)
                    {
                        case EventIds.Intro1:
                            Talk(TextIds.SayArrival1Rag);
                            break;
                        case EventIds.Intro2:
                            Talk(TextIds.SayArrival3Rag);
                            break;
                        case EventIds.Intro3:
                            me.HandleEmoteCommand(Emote.OneshotAttack1h);
                            break;
                        case EventIds.Intro4:
                            Talk(TextIds.SayArrival5Rag);
                            Creature executus = ObjectAccessor.GetCreature(me, instance.GetGuidData(DataTypes.MajordomoExecutus));
                            if (executus)
                                Unit.Kill(me, executus);
                            break;
                        case EventIds.Intro5:
                            me.SetReactState(ReactStates.Aggressive);
                            me.RemoveUnitFlag(UnitFlags.NonAttackable);
                            me.SetImmuneToPC(false);
                            _introState = 2;
                            break;
                        default:
                            break;
                    }
                });
            }
            else
            {
                if (_isBanished && ((_emergeTimer <= diff) || (instance.GetData(MCMiscConst.DataRagnarosAdds)) > 8))
                {
                    //Become unbanished again
                    me.SetReactState(ReactStates.Aggressive);
                    me.SetFaction((uint)FactionTemplates.Monster);
                    me.SetUninteractible(false);
                    me.SetEmoteState(Emote.OneshotNone);
                    me.HandleEmoteCommand(Emote.OneshotEmerge);
                    Unit target = SelectTarget(SelectTargetMethod.Random, 0);
                    if (target)
                        AttackStart(target);
                    instance.SetData(MCMiscConst.DataRagnarosAdds, 0);

                    //DoCast(me, SpellRagemerge); //"phase spells" didnt worked correctly so Ive commented them and wrote solution witch doesnt need core support
                    _isBanished = false;
                }
                else if (_isBanished)
                {
                    _emergeTimer -= diff;
                    //Do nothing while banished
                    return;
                }

                //Return since we have no target
                if (!UpdateVictim())
                    return;

                _events.Update(diff);

                _events.ExecuteEvents(eventId =>
                {
                    switch (eventId)
                    {
                        case EventIds.Eruption:
                            DoCastVictim(SpellIds.Erruption);
                            _events.ScheduleEvent(EventIds.Eruption, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(45));
                            break;
                        case EventIds.WrathOfRagnaros:
                            DoCastVictim(SpellIds.WrathOfRagnaros);
                            if (RandomHelper.URand(0, 1) != 0)
                                Talk(TextIds.SayWrath);
                            _events.ScheduleEvent(EventIds.WrathOfRagnaros, TimeSpan.FromSeconds(25));
                            break;
                        case EventIds.HandOfRagnaros:
                            DoCast(me, SpellIds.HandOfRagnaros);
                            if (RandomHelper.URand(0, 1) != 0)
                                Talk(TextIds.SayHand);
                            _events.ScheduleEvent(EventIds.HandOfRagnaros, TimeSpan.FromSeconds(20));
                            break;
                        case EventIds.LavaBurst:
                            DoCastVictim(SpellIds.LavaBurst);
                            _events.ScheduleEvent(EventIds.LavaBurst, TimeSpan.FromSeconds(10));
                            break;
                        case EventIds.ElementalFire:
                            DoCastVictim(SpellIds.ElementalFire);
                            _events.ScheduleEvent(EventIds.ElementalFire, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(14));
                            break;
                        case EventIds.MagmaBlast:
                            if (!me.IsWithinMeleeRange(me.GetVictim()))
                            {
                                DoCastVictim(SpellIds.MagmaBlast);
                                if (!_hasYelledMagmaBurst)
                                {
                                    //Say our dialog
                                    Talk(TextIds.SayMagmaburst);
                                    _hasYelledMagmaBurst = true;
                                }
                            }
                            _events.ScheduleEvent(EventIds.MagmaBlast, TimeSpan.FromMilliseconds(2500));
                            break;
                        case EventIds.Submerge:
                        {
                            if (!_isBanished)
                            {
                                //Creature spawning and ragnaros becomming unattackable
                                //is not very well supported in the core //no it really isnt
                                //so added normaly spawning and banish workaround and attack again after 90 secs.
                                me.AttackStop();
                                ResetThreatList();
                                me.SetReactState(ReactStates.Passive);
                                me.InterruptNonMeleeSpells(false);
                                //Root self
                                //DoCast(me, 23973);
                                me.SetFaction((uint)FactionTemplates.Friendly);
                                me.SetUninteractible(true);
                                me.SetEmoteState(Emote.StateSubmerged);
                                me.HandleEmoteCommand(Emote.OneshotSubmerge);
                                instance.SetData(MCMiscConst.DataRagnarosAdds, 0);

                                if (!_hasSubmergedOnce)
                                {
                                    Talk(TextIds.SayReinforcements1);

                                    // summon 8 elementals
                                    for (byte i = 0; i < 8; ++i)
                                    {
                                        Unit target = SelectTarget(SelectTargetMethod.Random, 0);
                                        if (target != null)
                                        {
                                            Creature summoned = me.SummonCreature(12143, target.GetPositionX(), target.GetPositionY(), target.GetPositionZ(), 0.0f, TempSummonType.TimedOrCorpseDespawn, TimeSpan.FromMinutes(15));
                                            if (summoned != null)
                                                summoned.GetAI().AttackStart(target);
                                        }
                                    }

                                    _hasSubmergedOnce = true;
                                    _isBanished = true;
                                    //DoCast(me, SpellRagsubmerge);
                                    _emergeTimer = 90000;

                                }
                                else
                                {
                                    Talk(TextIds.SayReinforcements2);

                                    for (byte i = 0; i < 8; ++i)
                                    {
                                        Unit target = SelectTarget(SelectTargetMethod.Random, 0);
                                        if (target != null)
                                        {
                                            Creature summoned = me.SummonCreature(12143, target.GetPositionX(), target.GetPositionY(), target.GetPositionZ(), 0.0f, TempSummonType.TimedOrCorpseDespawn, TimeSpan.FromMinutes(15));
                                            if (summoned != null)
                                                summoned.GetAI().AttackStart(target);
                                        }
                                    }

                                    _isBanished = true;
                                    //DoCast(me, SpellRagsubmerge);
                                    _emergeTimer = 90000;
                                }
                            }
                            _events.ScheduleEvent(EventIds.Submerge, TimeSpan.FromMinutes(3));
                            break;
                        }
                        default:
                            break;
                    }
                });


                DoMeleeAttackIfReady();
            }
        }
    }

    [Script]
    class npc_son_of_flame : ScriptedAI //didnt work correctly in Eai for me...
    {
        InstanceScript instance;

        public npc_son_of_flame(Creature creature) : base(creature)
        {
            instance = me.GetInstanceScript();
        }

        public override void JustDied(Unit killer)
        {
            instance.SetData(MCMiscConst.DataRagnarosAdds, 1);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            DoMeleeAttackIfReady();
        }
    }
}

