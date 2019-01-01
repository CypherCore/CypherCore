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
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;

namespace Scripts.Northrend.AzjolNerub.Ahnkahet.PrinceTaldaram
{
    struct SpellIds
    {
        public const uint Bloodthirst = 55968; // Trigger Spell + Add Aura
        public const uint ConjureFlameSphere = 55931;
        public const uint FlameSphereSummon1 = 55895; // 1x 30106
        public const uint FlameSphereSummon2 = 59511; // 1x 31686
        public const uint FlameSphereSummon3 = 59512; // 1x 31687
        public const uint FlameSphereSpawnEffect = 55891;
        public const uint FlameSphereVisual = 55928;
        public const uint FlameSpherePeriodic = 55926;
        public const uint FlameSphereDeathEffect = 55947;
        public const uint EmbraceOfTheVampyr = 55959;
        public const uint Vanish = 55964;

        public const uint BeamVisual = 60342;
        public const uint HoverFall = 60425;
    }

    struct CreatureIds
    {
        public const uint FlameSphere1 = 30106;
        public const uint FlameSphere2 = 31686;
        public const uint FlameSphere3 = 31687;
    }

    struct TextIds
    {
        public const uint Say1 = 0;
        public const uint SayWarning = 1;
        public const uint SayAggro = 2;
        public const uint SaySlay = 3;
        public const uint SayDeath = 4;
        public const uint SayFeed = 5;
        public const uint SayVanish = 6;
    }

    struct Misc
    {
        public const uint EventConjureFlameSpheres = 1;
        public const uint EventBloodthirst = 2;
        public const uint EventVanish = 3;
        public const uint EventJustVanished = 4;
        public const uint EventVanished = 5;
        public const uint EventFeeding = 6;

        // Flame Sphere
        public const uint EventStartMove = 7;
        public const uint EventDespawn = 8;

        public const uint DataEmbraceDmg = 20000;
        public const uint DataEmbraceDmgH = 40000;
        public const float DataSphereDistance = 25.0f;
        public const float DataSphereAngleOffset = MathFunctions.PI / 2;
        public const float DataGroundPositionZ = 11.30809f;
    }

    [Script]
    public class boss_prince_taldaram : BossAI
    {
        public boss_prince_taldaram(Creature creature) : base(creature, DataTypes.PrinceTaldaram)
        {
            me.SetDisableGravity(true);
            _embraceTakenDamage = 0;
        }

        public override void Reset()
        {
            _Reset();
            _flameSphereTargetGUID.Clear();
            _embraceTargetGUID.Clear();
            _embraceTakenDamage = 0;
        }

        public override void EnterCombat(Unit who)
        {
            _EnterCombat();
            Talk(TextIds.SayAggro);
            _events.ScheduleEvent(Misc.EventBloodthirst, 10000);
            _events.ScheduleEvent(Misc.EventVanish, RandomHelper.URand(25000, 35000));
            _events.ScheduleEvent(Misc.EventConjureFlameSpheres, 5000);
        }

        public override void JustSummoned(Creature summon)
        {
            base.JustSummoned(summon);

            switch (summon.GetEntry())
            {
                case CreatureIds.FlameSphere1:
                case CreatureIds.FlameSphere2:
                case CreatureIds.FlameSphere3:
                    summon.GetAI().SetGUID(_flameSphereTargetGUID);
                    break;
            }
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
                    case Misc.EventBloodthirst:
                        DoCast(me, SpellIds.Bloodthirst);
                        _events.ScheduleEvent(Misc.EventBloodthirst, 10000);
                        break;
                    case Misc.EventConjureFlameSpheres:
                        // random target?
                        Unit victim = me.GetVictim();
                        if (victim)
                        {
                            _flameSphereTargetGUID = victim.GetGUID();
                            DoCast(victim, SpellIds.ConjureFlameSphere);
                        }
                        _events.ScheduleEvent(Misc.EventConjureFlameSpheres, 15000);
                        break;
                    case Misc.EventVanish:
                        {
                            var players = me.GetMap().GetPlayers();
                            uint targets = 0;
                            foreach (var player in players)
                            {
                                if (player && player.IsAlive())
                                    ++targets;
                            }

                            if (targets > 2)
                            {
                                Talk(TextIds.SayVanish);
                                DoCast(me, SpellIds.Vanish);
                                me.SetInCombatState(true); // Prevents the boss from resetting
                                _events.DelayEvents(500);
                                _events.ScheduleEvent(Misc.EventJustVanished, 500);
                                Unit embraceTarget = SelectTarget(SelectAggroTarget.Random, 0, 100.0f, true);
                                if (embraceTarget)
                                    _embraceTargetGUID = embraceTarget.GetGUID();
                            }
                            _events.ScheduleEvent(Misc.EventVanish, RandomHelper.URand(25000, 35000));
                            break;
                        }
                    case Misc.EventJustVanished:
                        {
                            Unit embraceTarget = GetEmbraceTarget();
                            if (embraceTarget)
                            {
                                me.GetMotionMaster().Clear();
                                me.SetSpeedRate(UnitMoveType.Walk, 2.0f);
                                me.GetMotionMaster().MoveChase(embraceTarget);
                            }
                            _events.ScheduleEvent(Misc.EventVanished, 1300);
                        }
                        break;
                    case Misc.EventVanished:
                        {
                            Unit embraceTarget = GetEmbraceTarget();
                            if (embraceTarget)
                                DoCast(embraceTarget, SpellIds.EmbraceOfTheVampyr);
                            Talk(TextIds.SayFeed);
                            me.GetMotionMaster().Clear();
                            me.SetSpeedRate(UnitMoveType.Walk, 1.0f);
                            me.GetMotionMaster().MoveChase(me.GetVictim());
                            _events.ScheduleEvent(Misc.EventFeeding, 20000);
                        }
                        break;
                    case Misc.EventFeeding:
                        _embraceTargetGUID.Clear();
                        break;
                    default:
                        break;
                }
            });

            DoMeleeAttackIfReady();
        }

        public override void DamageTaken(Unit doneBy, ref uint damage)
        {
            Unit embraceTarget = GetEmbraceTarget();

            if (embraceTarget && embraceTarget.IsAlive())
            {
                _embraceTakenDamage += damage;
                if (_embraceTakenDamage > DungeonMode<uint>(Misc.DataEmbraceDmg, Misc.DataEmbraceDmgH))
                {
                    _embraceTargetGUID.Clear();
                    me.CastStop();
                }
            }
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.SayDeath);
            _JustDied();
        }

        public override void KilledUnit(Unit victim)
        {
            if (!victim.IsTypeId(TypeId.Player))
                return;

            if (victim.GetGUID() == _embraceTargetGUID)
                _embraceTargetGUID.Clear();

            Talk(TextIds.SaySlay);
        }

        public bool CheckSpheres()
        {
            for (byte i = 0; i < 2; ++i)
            {
                if (instance.GetData(DataTypes.Sphere1 + i) == 0)
                    return false;
            }

            RemovePrison();
            return true;
        }

        Unit GetEmbraceTarget()
        {
            if (!_embraceTargetGUID.IsEmpty())
                return Global.ObjAccessor.GetUnit(me, _embraceTargetGUID);

            return null;
        }

        void RemovePrison()
        {
            me.RemoveFlag(UnitFields.Flags, UnitFlags.NotSelectable);
            me.RemoveAurasDueToSpell(SpellIds.BeamVisual);
            me.SetHomePosition(me.GetPositionX(), me.GetPositionY(), Misc.DataGroundPositionZ, me.GetOrientation());
            DoCast(SpellIds.HoverFall);
            me.SetDisableGravity(false);
            me.GetMotionMaster().MoveLand(0, me.GetHomePosition());
            Talk(TextIds.SayWarning);
            instance.HandleGameObject(instance.GetGuidData(DataTypes.PrinceTaldaramPlatform), true);
        }

        ObjectGuid _flameSphereTargetGUID;
        ObjectGuid _embraceTargetGUID;
        uint _embraceTakenDamage;
    }

    [Script] // 30106, 31686, 31687 - Flame Sphere
    class npc_prince_taldaram_flame_sphere : ScriptedAI
    {
        public npc_prince_taldaram_flame_sphere(Creature creature) : base(creature)
        {
        }

        public override void Reset()
        {
            DoCast(me, SpellIds.FlameSphereSpawnEffect, true);
            DoCast(me, SpellIds.FlameSphereVisual, true);

            _flameSphereTargetGUID.Clear();
            _events.Reset();
            _events.ScheduleEvent(Misc.EventStartMove, 3 * Time.InMilliseconds);
            _events.ScheduleEvent(Misc.EventDespawn, 13 * Time.InMilliseconds);
        }

        public override void SetGUID(ObjectGuid guid, int id = 0)
        {
            _flameSphereTargetGUID = guid;
        }

        public override void EnterCombat(Unit who) { }

        public override void MoveInLineOfSight(Unit who) { }

        public override void UpdateAI(uint diff)
        {
            _events.Update(diff);

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case Misc.EventStartMove:
                        {
                            DoCast(me, SpellIds.FlameSpherePeriodic, true);

                            // @todo: find correct values
                            float angleOffset = 0.0f;
                            float distOffset = Misc.DataSphereDistance;

                            switch (me.GetEntry())
                            {
                                case CreatureIds.FlameSphere1:
                                    break;
                                case CreatureIds.FlameSphere2:
                                    angleOffset = Misc.DataSphereAngleOffset;
                                    break;
                                case CreatureIds.FlameSphere3:
                                    angleOffset = -Misc.DataSphereAngleOffset;
                                    break;
                                default:
                                    return;
                            }

                            Unit sphereTarget = Global.ObjAccessor.GetUnit(me, _flameSphereTargetGUID);
                            if (!sphereTarget)
                                return;

                            float angle = me.GetAngle(sphereTarget) + angleOffset;
                            float x = me.GetPositionX() + distOffset * (float)Math.Cos(angle);
                            float y = me.GetPositionY() + distOffset * (float)Math.Sin(angle);

                            // @todo: correct speed
                            me.GetMotionMaster().MovePoint(0, x, y, me.GetPositionZ());
                            break;
                        }
                    case Misc.EventDespawn:
                        DoCast(me, SpellIds.FlameSphereDeathEffect, true);
                        me.DespawnOrUnsummon(1000);
                        break;
                    default:
                        break;
                }
            });
        }

        ObjectGuid _flameSphereTargetGUID;
    }

    [Script] // 193093, 193094 - Ancient Nerubian Device
    class go_prince_taldaram_sphere : GameObjectScript
    {
        public go_prince_taldaram_sphere() : base("go_prince_taldaram_sphere") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            InstanceScript instance = go.GetInstanceScript();
            if (instance == null)
                return false;

            Creature PrinceTaldaram = ObjectAccessor.GetCreature(go, instance.GetGuidData(DataTypes.PrinceTaldaram));
            if (PrinceTaldaram && PrinceTaldaram.IsAlive())
            {
                go.SetFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                go.SetGoState(GameObjectState.Active);

                switch (go.GetEntry())
                {
                    case GameObjectIds.Sphere1:
                        instance.SetData(DataTypes.Sphere1, (uint)EncounterState.InProgress);
                        PrinceTaldaram.GetAI().Talk(TextIds.Say1);
                        break;
                    case GameObjectIds.Sphere2:
                        instance.SetData(DataTypes.Sphere2, (uint)EncounterState.InProgress);
                        PrinceTaldaram.GetAI().Talk(TextIds.Say1);
                        break;
                }

                PrinceTaldaram.GetAI<boss_prince_taldaram>().CheckSpheres();
            }
            return true;
        }
    }

    [Script] // 55931 - Conjure Flame Sphere
    class spell_prince_taldaram_conjure_flame_sphere : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FlameSphereSummon1, SpellIds.FlameSphereSummon2, SpellIds.FlameSphereSummon3);
        }

        void HandleScript(uint effIndex)
        {
            Unit caster = GetCaster();
            caster.CastSpell(caster, SpellIds.FlameSphereSummon1, true);

            if (caster.GetMap().IsHeroic())
            {
                caster.CastSpell(caster, SpellIds.FlameSphereSummon2, true);
                caster.CastSpell(caster, SpellIds.FlameSphereSummon3, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 55895, 59511, 59512 - Flame Sphere Summon
    class spell_prince_taldaram_flame_sphere_summon : SpellScript
    {
        void SetDest(ref SpellDestination dest)
        {
            dest.RelocateOffset(new Position(0.0f, 0.0f, 5.5f, 0.0f));
        }

        public override void Register()
        {
            OnDestinationTargetSelect.Add(new DestinationTargetSelectHandler(SetDest, 0, Targets.DestCaster));
        }
    }
}
