// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using Game.AI;
using Game.DataStorage;
using Game.Maps;
using System;

namespace Scripts.EasternKingdoms.BaradinHold.Alizabal
{
    struct SpellIds
    {
        public const uint BladeDance = 105784;
        public const uint BladeDanceDummy = 105828;
        public const uint SeethingHate = 105067;
        public const uint Skewer = 104936;
        public const uint Berserk = 47008;
    }

    struct TextIds
    {
        public const uint SayIntro = 1;
        public const uint SayAggro = 2;
        public const uint SayHate = 3;
        public const uint SaySkewer = 4;
        public const uint SaySkewerAnnounce = 5;
        public const uint SayBladeStorm = 6;
        public const uint SaySlay = 10;
        public const uint SayDeath = 12;
    }

    struct ActionIds
    {
        public const int Intro = 1;
    }

    struct PointIds
    {
        public const uint Storm = 1;
    }

    struct EventIds
    {
        public const uint RandomCast = 1;
        public const uint StopStorm = 2;
        public const uint MoveStorm = 3;
        public const uint CastStorm = 4;
    }

    [Script]
    class at_alizabal_intro : AreaTriggerScript
    {
        public at_alizabal_intro() : base("at_alizabal_intro") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger)
        {
            InstanceScript instance = player.GetInstanceScript();
            if (instance != null)
            {
                Creature alizabal = ObjectAccessor.GetCreature(player, instance.GetGuidData(DataTypes.Alizabal));
                if (alizabal != null)
                    alizabal.GetAI().DoAction(ActionIds.Intro);
            }
            return true;
        }
    }

    [Script]
    class boss_alizabal : BossAI
    {
        bool _intro;
        bool _hate;
        bool _skewer;

        public boss_alizabal(Creature creature) : base(creature, DataTypes.Alizabal) { }

        public override void Reset()
        {
            _Reset();
            _hate = false;
            _skewer = false;
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);
            Talk(TextIds.SayAggro);
            instance.SendEncounterUnit(EncounterFrameType.Engage, me);
            _events.ScheduleEvent(EventIds.RandomCast, TimeSpan.FromSeconds(10));
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
            Talk(TextIds.SayDeath);
            instance.SendEncounterUnit(EncounterFrameType.Disengage, me);
        }

        public override void KilledUnit(Unit who)
        {
            if (who.IsPlayer())
                Talk(TextIds.SaySlay);
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            instance.SendEncounterUnit(EncounterFrameType.Disengage, me);
            me.GetMotionMaster().MoveTargetedHome();
            _DespawnAtEvade();
        }

        public override void DoAction(int action)
        {
            switch (action)
            {
                case ActionIds.Intro:
                    if (!_intro)
                    {
                        Talk(TextIds.SayIntro);
                        _intro = true;
                    }
                    break;
            }
        }

        public override void MovementInform(MovementGeneratorType type, uint pointId)
        {
            switch (pointId)
            {
                case PointIds.Storm:
                    _events.ScheduleEvent(EventIds.CastStorm, TimeSpan.FromMilliseconds(1));
                    break;
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _events.Update(diff);

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case EventIds.RandomCast:
                    {
                        switch (RandomHelper.URand(0, 1))
                        {
                            case 0:
                                if (!_skewer)
                                {
                                    Unit target = SelectTarget(SelectTargetMethod.MaxThreat, 0);
                                    if (target != null)
                                    {
                                        DoCast(target, SpellIds.Skewer, new CastSpellExtraArgs(true));
                                        Talk(TextIds.SaySkewer);
                                        Talk(TextIds.SaySkewerAnnounce, target);
                                    }
                                    _skewer = true;
                                    _events.ScheduleEvent(EventIds.RandomCast, TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(10));
                                }
                                else if (!_hate)
                                {
                                    Unit target = SelectTarget(SelectTargetMethod.Random, 0, new NonTankTargetSelector(me));
                                    if (target != null)
                                    {
                                        DoCast(target, SpellIds.SeethingHate, new CastSpellExtraArgs(true));
                                        Talk(TextIds.SayHate);
                                    }
                                    _hate = true;
                                    _events.ScheduleEvent(EventIds.RandomCast, TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(10));
                                }
                                else if (_hate && _skewer)
                                {
                                    Talk(TextIds.SayBladeStorm);
                                    DoCastAOE(SpellIds.BladeDanceDummy);
                                    DoCastAOE(SpellIds.BladeDance);
                                    _events.ScheduleEvent(EventIds.RandomCast, TimeSpan.FromSeconds(21));
                                    _events.ScheduleEvent(EventIds.MoveStorm, TimeSpan.FromMilliseconds(4050));
                                    _events.ScheduleEvent(EventIds.StopStorm, TimeSpan.FromSeconds(13));
                                }
                                break;
                            case 1:
                                if (!_hate)
                                {
                                    Unit target = SelectTarget(SelectTargetMethod.Random, 0, new NonTankTargetSelector(me));
                                    if (target != null)
                                    {
                                        DoCast(target, SpellIds.SeethingHate, new CastSpellExtraArgs(true));
                                        Talk(TextIds.SayHate);
                                    }
                                    _hate = true;
                                    _events.ScheduleEvent(EventIds.RandomCast, TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(10));
                                }
                                else if (!_skewer)
                                {
                                    Unit target = SelectTarget(SelectTargetMethod.MaxThreat, 0);
                                    if (target != null)
                                    {
                                        DoCast(target, SpellIds.Skewer, new CastSpellExtraArgs(true));
                                        Talk(TextIds.SaySkewer);
                                        Talk(TextIds.SaySkewerAnnounce, target);
                                    }
                                    _skewer = true;
                                    _events.ScheduleEvent(EventIds.RandomCast, TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(10));
                                }
                                else if (_hate && _skewer)
                                {
                                    Talk(TextIds.SayBladeStorm);
                                    DoCastAOE(SpellIds.BladeDanceDummy);
                                    DoCastAOE(SpellIds.BladeDance);
                                    _events.ScheduleEvent(EventIds.RandomCast, TimeSpan.FromSeconds(21));
                                    _events.ScheduleEvent(EventIds.MoveStorm, TimeSpan.FromMilliseconds(4050));
                                    _events.ScheduleEvent(EventIds.StopStorm, TimeSpan.FromSeconds(13));
                                }
                                break;
                        }
                        break;
                    }
                    case EventIds.MoveStorm:
                    {
                        me.SetSpeedRate(UnitMoveType.Run, 4.0f);
                        me.SetSpeedRate(UnitMoveType.Walk, 4.0f);
                        Unit target = SelectTarget(SelectTargetMethod.Random, 0, new NonTankTargetSelector(me));
                        if (target != null)
                            me.GetMotionMaster().MovePoint(PointIds.Storm, target.GetPositionX(), target.GetPositionY(), target.GetPositionZ());
                        _events.ScheduleEvent(EventIds.MoveStorm, TimeSpan.FromMilliseconds(4050));
                        break;
                    }
                    case EventIds.StopStorm:
                        me.RemoveAura(SpellIds.BladeDance);
                        me.RemoveAura(SpellIds.BladeDanceDummy);
                        me.SetSpeedRate(UnitMoveType.Walk, 1.0f);
                        me.SetSpeedRate(UnitMoveType.Run, 1.14f);
                        me.GetMotionMaster().MoveChase(me.GetVictim());
                        _hate = false;
                        _skewer = false;
                        break;
                    case EventIds.CastStorm:
                        DoCastAOE(SpellIds.BladeDance);
                        break;
                }
            });

            DoMeleeAttackIfReady();
        }
    }
}

