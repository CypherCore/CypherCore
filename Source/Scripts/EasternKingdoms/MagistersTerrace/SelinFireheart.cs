// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using System;
using System.Collections.Generic;

namespace Scripts.EasternKingdoms.MagistersTerrace.SelinFireheart
{
    struct TextIds
    {
        public const uint SayAggro = 0;
        public const uint SayEnergy = 1;
        public const uint SayEmpowered = 2;
        public const uint SayKill = 3;
        public const uint SayDeath = 4;
        public const uint EmoteCrystal = 5;
    }

    struct SpellIds
    {
        // Crystal effect spells
        public const uint FelCrystalDummy = 44329;
        public const uint ManaRage = 44320;               // This spell triggers 44321, which changes scale and regens mana Requires an entry in spell_script_target

        // Selin's spells
        public const uint DrainLife = 44294;
        public const uint FelExplosion = 44314;

        public const uint DrainMana = 46153;               // Heroic only
    }

    struct PhaseIds
    {
        public const byte Normal = 1;
        public const byte Drain = 2;
    }

    struct EventIds
    {
        public const uint FelExplosion = 1;
        public const uint DrainCrystal = 2;
        public const uint DrainMana = 3;
        public const uint DrainLife = 4;
        public const uint Empower = 5;
    }

    struct MiscConst
    {
        public const int ActionSwitchPhase = 1;
    }

    [Script] // @todo crystals should really be a Db creature summon group, having them in `creature` like this will cause tons of despawn/respawn bugs
    class boss_selin_fireheart : BossAI
    {
        ObjectGuid CrystalGUID;
        bool _scheduledEvents;

        public boss_selin_fireheart(Creature creature) : base(creature, DataTypes.SelinFireheart) { }

        public override void Reset()
        {
            List<Creature> crystals = me.GetCreatureListWithEntryInGrid(CreatureIds.FelCrystal, 250.0f);

            foreach (Creature creature in crystals)
                creature.Respawn(true);

            _Reset();
            CrystalGUID.Clear();
            _scheduledEvents = false;
        }

        public override void DoAction(int action)
        {
            switch (action)
            {
                case MiscConst.ActionSwitchPhase:
                    _events.SetPhase(PhaseIds.Normal);
                    _events.ScheduleEvent(EventIds.FelExplosion, TimeSpan.FromSeconds(2), 0, PhaseIds.Normal);
                    AttackStart(me.GetVictim());
                    me.GetMotionMaster().MoveChase(me.GetVictim());
                    break;
                default:
                    break;
            }
        }

        void SelectNearestCrystal()
        {
            Creature crystal = me.FindNearestCreature(CreatureIds.FelCrystal, 250.0f);
            if (crystal != null)
            {
                Talk(TextIds.SayEnergy);
                Talk(TextIds.EmoteCrystal);

                DoCast(crystal, SpellIds.FelCrystalDummy);
                CrystalGUID = crystal.GetGUID();

                float x, y, z;
                crystal.GetClosePoint(out x, out y, out z, me.GetCombatReach(), SharedConst.ContactDistance);

                _events.SetPhase(PhaseIds.Drain);
                me.SetWalk(false);
                me.GetMotionMaster().MovePoint(1, x, y, z);
            }
        }

        void ShatterRemainingCrystals()
        {
            List<Creature> crystals = me.GetCreatureListWithEntryInGrid(CreatureIds.FelCrystal, 250.0f);

            foreach (Creature crystal in crystals)
                crystal.KillSelf();
        }

        public override void JustEngagedWith(Unit who)
        {
            Talk(TextIds.SayAggro);
            base.JustEngagedWith(who);

            _events.SetPhase(PhaseIds.Normal);
            _events.ScheduleEvent(EventIds.FelExplosion, TimeSpan.FromMilliseconds(2100), 0, PhaseIds.Normal);
        }

        public override void KilledUnit(Unit victim)
        {
            if (victim.IsPlayer())
                Talk(TextIds.SayKill);
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (type == MovementGeneratorType.Point && id == 1)
            {
                Unit CrystalChosen = Global.ObjAccessor.GetUnit(me, CrystalGUID);
                if (CrystalChosen != null && CrystalChosen.IsAlive())
                {
                    CrystalChosen.SetUninteractible(false);
                    CrystalChosen.CastSpell(me, SpellIds.ManaRage, true);
                    _events.ScheduleEvent(EventIds.Empower, TimeSpan.FromSeconds(10), PhaseIds.Drain);
                }
            }
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.SayDeath);
            _JustDied();

            ShatterRemainingCrystals();
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _events.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case EventIds.FelExplosion:
                        DoCastAOE(SpellIds.FelExplosion);
                        _events.ScheduleEvent(EventIds.FelExplosion, TimeSpan.FromSeconds(2), 0, PhaseIds.Normal);
                        break;
                    case EventIds.DrainCrystal:
                        SelectNearestCrystal();
                        _scheduledEvents = false;
                        break;
                    case EventIds.DrainMana:
                    {
                        Unit target = SelectTarget(SelectTargetMethod.Random, 0, 45.0f, true);
                        if (target != null)
                            DoCast(target, SpellIds.DrainMana);
                        _events.ScheduleEvent(EventIds.DrainMana, TimeSpan.FromSeconds(10), 0, PhaseIds.Normal);
                        break;
                    }
                    case EventIds.DrainLife:
                    {
                        Unit target = SelectTarget(SelectTargetMethod.Random, 0, 20.0f, true);
                        if (target != null)
                            DoCast(target, SpellIds.DrainLife);
                        _events.ScheduleEvent(EventIds.DrainLife, TimeSpan.FromSeconds(10), 0, PhaseIds.Normal);
                        break;
                    }
                    case EventIds.Empower:
                    {
                        Talk(TextIds.SayEmpowered);

                        Creature CrystalChosen = ObjectAccessor.GetCreature(me, CrystalGUID);
                        if (CrystalChosen != null && CrystalChosen.IsAlive())
                            CrystalChosen.KillSelf();

                        CrystalGUID.Clear();

                        me.GetMotionMaster().Clear();
                        me.GetMotionMaster().MoveChase(me.GetVictim());
                        break;
                    }
                    default:
                        break;
                }

                if (me.HasUnitState(UnitState.Casting))
                    return;
            });

            if (me.GetPowerPct(PowerType.Mana) < 10.0f)
            {
                if (_events.IsInPhase(PhaseIds.Normal) && !_scheduledEvents)
                {
                    _scheduledEvents = true;
                    TimeSpan timer = RandomHelper.RandTime(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(7));
                    _events.ScheduleEvent(EventIds.DrainLife, timer, 0, PhaseIds.Normal);

                    if (IsHeroic())
                    {
                        _events.ScheduleEvent(EventIds.DrainCrystal, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), 0, PhaseIds.Normal);
                        _events.ScheduleEvent(EventIds.DrainMana, timer + TimeSpan.FromSeconds(5), 0, PhaseIds.Normal);
                    }
                    else
                        _events.ScheduleEvent(EventIds.DrainCrystal, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(25), 0, PhaseIds.Normal);
                }
            }
        }
    }

    [Script]
    class npc_fel_crystal : ScriptedAI
    {
        public npc_fel_crystal(Creature creature) : base(creature) { }

        public override void JustDied(Unit killer)
        {
            InstanceScript instance = me.GetInstanceScript();
            if (instance != null)
            {
                Creature selin = instance.GetCreature(DataTypes.SelinFireheart);
                if (selin != null && selin.IsAlive())
                    selin.GetAI().DoAction(MiscConst.ActionSwitchPhase);
            }
        }
    }
}