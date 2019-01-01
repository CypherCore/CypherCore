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

namespace Scripts.Northrend.AzjolNerub.Ahnkahet.JedogaShadowseeker
{
    struct SpellIds
    {
        public const uint SphereVisual = 56075;
        public const uint GiftOfTheHerald = 56219;
        public const uint CycloneStrike = 56855; // Self
        public const uint LightningBolt = 56891; // 40y
        public const uint Thundershock = 56926;  // 30y

        public const uint BeamVisualJedogasAufseher1 = 60342;
        public const uint BeamVisualJedogasAufseher2 = 56312;
    }

    struct TextIds
    {
        public const uint SayAggro = 0;
        public const uint SaySacrifice1 = 1;
        public const uint SaySacrifice2 = 2;
        public const uint SaySlay = 3;
        public const uint SayDeath = 4;
        public const uint SayPreaching = 5;
    }
    struct Misc
    {
        public const int ActionInitiateKilled = 1;
        public const uint DataVolunteerWork = 2;

        public static Position[] JedogaPosition =
        {
            new Position(372.330994f, -705.278015f, -0.624178f,  5.427970f),
            new Position(372.330994f, -705.278015f, -16.179716f, 5.427970f)
        };
    }

    [Script]
    public class boss_jedoga_shadowseeker : ScriptedAI
    {
        public boss_jedoga_shadowseeker(Creature creature) : base(creature)
        {
            Initialize();
            instance = creature.GetInstanceScript();
            bFirstTime = true;
            bPreDone = false;
        }

        void Initialize()
        {
            uiOpFerTimer = RandomHelper.URand(15 * Time.InMilliseconds, 20 * Time.InMilliseconds);

            uiCycloneTimer = 3 * Time.InMilliseconds;
            uiBoltTimer = 7 * Time.InMilliseconds;
            uiThunderTimer = 12 * Time.InMilliseconds;

            bOpFerok = false;
            bOpFerokFail = false;
            bOnGround = false;
            bCanDown = false;
            volunteerWork = true;
        }

        public override void Reset()
        {
            Initialize();

            if (!bFirstTime)
                instance.SetBossState(DataTypes.JedogaShadowseeker, EncounterState.Fail);

            instance.SetGuidData(DataTypes.PlJedogaTarget, ObjectGuid.Empty);
            instance.SetGuidData(DataTypes.AddJedogaVictim, ObjectGuid.Empty);
            instance.SetData(DataTypes.JedogaResetInitiands, 0);
            MoveUp();

            bFirstTime = false;
        }

        public override void EnterCombat(Unit who)
        {
            if (instance == null || (who.GetTypeId() == TypeId.Unit && who.GetEntry() == AKCreatureIds.JedogaController))
                return;

            Talk(TextIds.SayAggro);
            me.SetInCombatWithZone();
            instance.SetBossState(DataTypes.JedogaShadowseeker, EncounterState.InProgress);
        }

        public override void AttackStart(Unit who)
        {
            if (!who || (who.GetTypeId() == TypeId.Unit && who.GetEntry() == AKCreatureIds.JedogaController))
                return;

            base.AttackStart(who);
        }

        public override void KilledUnit(Unit Victim)
        {
            if (!Victim || !Victim.IsTypeId(TypeId.Player))
                return;

            Talk(TextIds.SaySlay);
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.SayDeath);
            instance.SetBossState(DataTypes.JedogaShadowseeker, EncounterState.Done);
        }

        public override void DoAction(int action)
        {
            if (action == Misc.ActionInitiateKilled)
                volunteerWork = false;
        }

        public override uint GetData(uint type)
        {
            if (type == Misc.DataVolunteerWork)
                return volunteerWork ? 1 : 0u;

            return 0;
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (instance == null || !who || (who.IsTypeId(TypeId.Unit) && who.GetEntry() == AKCreatureIds.JedogaController))
                return;

            if (!bPreDone && who.IsTypeId(TypeId.Player) && me.GetDistance(who) < 100.0f)
            {
                Talk(TextIds.SayPreaching);
                bPreDone = true;
            }

            if (instance.GetBossState(DataTypes.JedogaShadowseeker) != EncounterState.InProgress || !bOnGround)
                return;

            if (!me.GetVictim() && me.CanCreatureAttack(who))
            {
                float attackRadius = me.GetAttackDistance(who);
                if (me.IsWithinDistInMap(who, attackRadius) && me.IsWithinLOSInMap(who))
                {
                    if (!me.GetVictim())
                    {
                        who.RemoveAurasByType(AuraType.ModStealth);
                        AttackStart(who);
                    }
                    else
                    {
                        who.SetInCombatWith(me);
                        me.AddThreat(who, 0.0f);
                    }
                }
            }
        }

        void MoveDown()
        {
            bOpFerokFail = false;

            instance.SetData(DataTypes.JedogaTriggerSwitch, 0);
            me.GetMotionMaster().MovePoint(1, Misc.JedogaPosition[1]);
            me.ApplySpellImmune(0, SpellImmunity.Damage, SpellSchoolMask.Normal, false);
            me.ApplySpellImmune(0, SpellImmunity.Damage, SpellSchoolMask.Magic, false);
            me.RemoveFlag(UnitFields.Flags, UnitFlags.NotSelectable | UnitFlags.NonAttackable);

            me.RemoveAurasDueToSpell(SpellIds.SphereVisual);

            bOnGround = true;

            if (UpdateVictim())
            {
                AttackStart(me.GetVictim());
                me.GetMotionMaster().MoveChase(me.GetVictim());
            }
            else
            {
                Unit target = Global.ObjAccessor.GetUnit(me, instance.GetGuidData(DataTypes.PlJedogaTarget));
                if (target)
                {
                    AttackStart(target);
                    instance.SetData(DataTypes.JedogaResetInitiands, 0);
                    if (instance.GetBossState(DataTypes.JedogaShadowseeker) != EncounterState.InProgress)
                        EnterCombat(target);
                }
                else if (!me.IsInCombat())
                    EnterEvadeMode();
            }
        }

        void MoveUp()
        {
            me.ApplySpellImmune(0, SpellImmunity.Damage, SpellSchoolMask.Normal, true);
            me.ApplySpellImmune(0, SpellImmunity.Damage, SpellSchoolMask.Magic, true);
            me.SetFlag(UnitFields.Flags, UnitFlags.NotSelectable | UnitFlags.NonAttackable);

            me.AttackStop();
            me.RemoveAllAuras();
            me.LoadCreaturesAddon();
            me.GetMotionMaster().MovePoint(0, Misc.JedogaPosition[0]);

            instance.SetData(DataTypes.JedogaTriggerSwitch, 1);
            if (instance.GetBossState(DataTypes.JedogaShadowseeker) == EncounterState.InProgress)
                GetVictimForSacrifice();

            bOnGround = false;
            uiOpFerTimer = RandomHelper.URand(15 * Time.InMilliseconds, 30 * Time.InMilliseconds);
        }

        void GetVictimForSacrifice()
        {
            ObjectGuid victim = instance.GetGuidData(DataTypes.AddJedogaInitiand);
            if (!victim.IsEmpty())
            {
                Talk(TextIds.SaySacrifice1);
                instance.SetGuidData(DataTypes.AddJedogaVictim, victim);
            }
            else
                bCanDown = true;
        }

        void Sacrifice()
        {
            Talk(TextIds.SaySacrifice2);

            me.InterruptNonMeleeSpells(false);
            DoCast(me, SpellIds.GiftOfTheHerald, false);

            bOpFerok = false;
            bCanDown = true;
        }

        public override void UpdateAI(uint diff)
        {
            if (instance.GetBossState(DataTypes.JedogaShadowseeker) != EncounterState.InProgress && instance.GetData(DataTypes.AllInitiandDead) != 0)
                MoveDown();

            if (bOpFerok && !bOnGround && !bCanDown)
                Sacrifice();

            if (bOpFerokFail && !bOnGround && !bCanDown)
                bCanDown = true;

            if (bCanDown)
            {
                MoveDown();
                bCanDown = false;
            }

            if (bOnGround)
            {
                if (!UpdateVictim())
                    return;

                if (uiCycloneTimer <= diff)
                {
                    DoCast(me, SpellIds.CycloneStrike, false);
                    uiCycloneTimer = RandomHelper.URand(15 * Time.InMilliseconds, 30 * Time.InMilliseconds);
                }
                else uiCycloneTimer -= diff;

                if (uiBoltTimer <= diff)
                {
                    Unit target = SelectTarget(SelectAggroTarget.Random, 0, 100, true);
                    if (target)
                        me.CastSpell(target, SpellIds.LightningBolt, false);

                    uiBoltTimer = RandomHelper.URand(15 * Time.InMilliseconds, 30 * Time.InMilliseconds);
                }
                else uiBoltTimer -= diff;

                if (uiThunderTimer <= diff)
                {
                    Unit target = SelectTarget(SelectAggroTarget.Random, 0, 100, true);
                    if (target)
                        me.CastSpell(target, SpellIds.Thundershock, false);

                    uiThunderTimer = RandomHelper.URand(15 * Time.InMilliseconds, 30 * Time.InMilliseconds);
                }
                else uiThunderTimer -= diff;

                if (uiOpFerTimer <= diff)
                    MoveUp();
                else
                    uiOpFerTimer -= diff;

                DoMeleeAttackIfReady();
            }
        }

        InstanceScript instance;

        uint uiOpFerTimer;
        uint uiCycloneTimer;
        uint uiBoltTimer;
        uint uiThunderTimer;

        bool bPreDone;
        public bool bOpFerok;
        bool bOnGround;
        public bool bOpFerokFail;
        bool bCanDown;
        bool volunteerWork;
        bool bFirstTime;
    }

    [Script]
    class npc_jedoga_initiand : ScriptedAI
    {
        public npc_jedoga_initiand(Creature creature) : base(creature)
        {
            Initialize();
            instance = creature.GetInstanceScript();
        }

        void Initialize()
        {
            bWalking = false;
            bCheckTimer = 2 * Time.InMilliseconds;
        }

        public override void Reset()
        {
            Initialize();

            if (instance.GetBossState(DataTypes.JedogaShadowseeker) != EncounterState.InProgress)
            {
                me.RemoveAurasDueToSpell(SpellIds.SphereVisual);
                me.ApplySpellImmune(0, SpellImmunity.Damage, SpellSchoolMask.Normal, false);
                me.ApplySpellImmune(0, SpellImmunity.Damage, SpellSchoolMask.Magic, false);
                me.RemoveFlag(UnitFields.Flags, UnitFlags.NotSelectable | UnitFlags.NonAttackable);
            }
            else
            {
                DoCast(me, SpellIds.SphereVisual, false);
                me.ApplySpellImmune(0, SpellImmunity.Damage, SpellSchoolMask.Normal, true);
                me.ApplySpellImmune(0, SpellImmunity.Damage, SpellSchoolMask.Magic, true);
                me.SetFlag(UnitFields.Flags, UnitFlags.NotSelectable | UnitFlags.NonAttackable);
            }
        }

        public override void JustDied(Unit killer)
        {
            if (!killer || instance == null)
                return;

            if (bWalking)
            {
                Creature boss = ObjectAccessor.GetCreature(me, instance.GetGuidData(DataTypes.JedogaShadowseeker));
                if (boss)
                {
                    if (!boss.GetAI<boss_jedoga_shadowseeker>().bOpFerok)
                        boss.GetAI<boss_jedoga_shadowseeker>().bOpFerokFail = true;

                    if (killer.IsTypeId(TypeId.Player))
                        boss.GetAI().DoAction(Misc.ActionInitiateKilled);
                }

                instance.SetGuidData(DataTypes.AddJedogaVictim, ObjectGuid.Empty);

                bWalking = false;
            }
            if (killer.IsTypeId(TypeId.Player))
                instance.SetGuidData(DataTypes.PlJedogaTarget, killer.GetGUID());
        }

        public override void EnterCombat(Unit who) { }

        public override void AttackStart(Unit victim)
        {
            if ((instance.GetBossState(DataTypes.JedogaShadowseeker) == EncounterState.InProgress) || !victim)
                return;

            base.AttackStart(victim);
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if ((instance.GetBossState(DataTypes.JedogaShadowseeker) == EncounterState.InProgress) || !who)
                return;

            base.MoveInLineOfSight(who);
        }

        public override void MovementInform(MovementGeneratorType uiType, uint uiPointId)
        {
            if (uiType != MovementGeneratorType.Point || instance == null)
                return;

            switch (uiPointId)
            {
                case 1:
                    {
                        Creature boss = me.GetMap().GetCreature(instance.GetGuidData(DataTypes.JedogaShadowseeker));
                        if (boss)
                        {
                            boss.GetAI<boss_jedoga_shadowseeker>().bOpFerok = true;
                            boss.GetAI<boss_jedoga_shadowseeker>().bOpFerokFail = false;
                            me.KillSelf();
                        }
                    }
                    break;
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (bCheckTimer <= diff)
            {
                if (me.GetGUID() == instance.GetGuidData(DataTypes.AddJedogaVictim) && !bWalking)
                {
                    me.RemoveAurasDueToSpell(SpellIds.SphereVisual);
                    me.ApplySpellImmune(0, SpellImmunity.Damage, SpellSchoolMask.Normal, false);
                    me.ApplySpellImmune(0, SpellImmunity.Damage, SpellSchoolMask.Magic, false);
                    me.RemoveFlag(UnitFields.Flags, UnitFlags.NotSelectable | UnitFlags.NonAttackable);

                    float distance = me.GetDistance(Misc.JedogaPosition[1]);

                    if (distance < 9.0f)
                        me.SetSpeedRate(UnitMoveType.Walk, 0.5f);
                    else if (distance < 15.0f)
                        me.SetSpeedRate(UnitMoveType.Walk, 0.75f);
                    else if (distance < 20.0f)
                        me.SetSpeedRate(UnitMoveType.Walk, 1.0f);

                    me.GetMotionMaster().Clear(false);
                    me.GetMotionMaster().MovePoint(1, Misc.JedogaPosition[1]);
                    bWalking = true;
                }
                if (!bWalking)
                {
                    if (instance.GetBossState(DataTypes.JedogaShadowseeker) != EncounterState.InProgress && me.HasAura(SpellIds.SphereVisual))
                    {
                        me.RemoveAurasDueToSpell(SpellIds.SphereVisual);
                        me.ApplySpellImmune(0, SpellImmunity.Damage, SpellSchoolMask.Normal, false);
                        me.ApplySpellImmune(0, SpellImmunity.Damage, SpellSchoolMask.Magic, false);
                        me.RemoveFlag(UnitFields.Flags, UnitFlags.NotSelectable | UnitFlags.NonAttackable);
                    }
                    if (instance.GetBossState(DataTypes.JedogaShadowseeker) == EncounterState.InProgress && !me.HasAura(SpellIds.SphereVisual))
                    {
                        DoCast(me, SpellIds.SphereVisual, false);
                        me.ApplySpellImmune(0, SpellImmunity.Damage, SpellSchoolMask.Normal, true);
                        me.ApplySpellImmune(0, SpellImmunity.Damage, SpellSchoolMask.Magic, true);
                        me.SetFlag(UnitFields.Flags, UnitFlags.NotSelectable | UnitFlags.NonAttackable);
                    }
                }
                bCheckTimer = 2 * Time.InMilliseconds;
            }
            else bCheckTimer -= diff;

            //Return since we have no target
            if (!UpdateVictim())
                return;

            DoMeleeAttackIfReady();
        }

        InstanceScript instance;

        uint bCheckTimer;
        bool bWalking;
    }


    [Script]
    class npc_jedogas_aufseher_trigger : ScriptedAI
    {
        public npc_jedogas_aufseher_trigger(Creature creature) : base(creature)
        {
            instance = creature.GetInstanceScript();
            bRemoved = false;
            bRemoved2 = false;
            bCast = false;
            bCast2 = false;

            SetCombatMovement(false);
        }

        public override void Reset() { }
        public override void EnterCombat(Unit who) { }
        public override void AttackStart(Unit victim) { }
        public override void MoveInLineOfSight(Unit who) { }


        public override void UpdateAI(uint diff)
        {
            if (!bRemoved && me.GetPositionX() > 440.0f)
            {
                if (instance.GetBossState(DataTypes.PrinceTaldaram) == EncounterState.Done)
                {
                    me.InterruptNonMeleeSpells(true);
                    bRemoved = true;
                    return;
                }
                if (!bCast)
                {
                    DoCast(me, SpellIds.BeamVisualJedogasAufseher1, false);
                    bCast = true;
                }
            }
            if (!bRemoved2 && me.GetPositionX() < 440.0f)
            {
                if (!bCast2 && instance.GetData(DataTypes.JedogaTriggerSwitch) != 0)
                {
                    DoCast(me, SpellIds.BeamVisualJedogasAufseher2, false);
                    bCast2 = true;
                }
                if (bCast2 && instance.GetData(DataTypes.JedogaTriggerSwitch) == 0)
                {
                    me.InterruptNonMeleeSpells(true);
                    bCast2 = false;
                }
                if (!bRemoved2 && instance.GetBossState(DataTypes.JedogaShadowseeker) == EncounterState.Done)
                {
                    me.InterruptNonMeleeSpells(true);
                    bRemoved2 = true;
                }
            }
        }

        InstanceScript instance;

        bool bRemoved;
        bool bRemoved2;
        bool bCast;
        bool bCast2;
    }

    [Script]
    class achievement_volunteer_work : AchievementCriteriaScript
    {
        public achievement_volunteer_work() : base("achievement_volunteer_work") { }

        public override bool OnCheck(Player player, Unit target)
        {
            if (!target)
                return false;

            Creature Jedoga = target.ToCreature();
            if (Jedoga)
                if (Jedoga.GetAI().GetData(Misc.DataVolunteerWork) != 0)
                    return true;

            return false;
        }
    }
}
