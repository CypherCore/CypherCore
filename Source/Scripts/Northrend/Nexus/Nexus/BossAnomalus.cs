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
using System;

namespace Scripts.Northrend.Nexus.Nexus
{
    struct AnomalusConst
    {
        //Spells
        public const uint SpellSpark = 47751;
        public const uint SpellSparkHeroic = 57062;
        public const uint SpellRiftShield = 47748;
        public const uint SpellChargeRift = 47747; // Works Wrong (Affect Players; Not Rifts)
        public const uint SpellCreateRift = 47743; // Don'T Work; Using Wa
        public const uint SpellArcaneAttraction = 57063; // No Idea; When It'S Used 

        public const uint SpellChaoticEnergyBurst = 47688;
        public const uint SpellChargedChaoticEnergyBurst = 47737;
        public const uint SpellArcaneform = 48019; // Chaotic Rift Visual

        //Texts
        public const uint SayAggro = 0;
        public const uint SayDeath = 1;
        public const uint SayRift = 2;
        public const uint SayShield = 3;
        public const uint SayRiftEmote = 4; // Needs To Be Added To Script
        public const uint SayShieldEmote = 5;  // Needs To Be Added To Script

        //Misc
        public const uint NpcCrazedManaWraith = 26746;
        public const uint NpcChaoticRift = 26918;

        public const uint DataChaosTheory = 1;

        public static Position[] RiftLocation =
        {
            new Position(652.64f, -273.70f, -8.75f, 0.0f),
            new Position(634.45f, -265.94f, -8.44f, 0.0f),
            new Position(620.73f, -281.17f, -9.02f, 0.0f),
            new Position(626.10f, -304.67f, -9.44f, 0.0f),
            new Position(639.87f, -314.11f, -9.49f, 0.0f),
            new Position(651.72f, -297.44f, -9.37f, 0.0f)
        };
    }

    [Script]
    class boss_anomalus : ScriptedAI
    {
        public boss_anomalus(Creature creature) : base(creature)
        {
            instance = me.GetInstanceScript();
        }

        void Initialize()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(5), task =>
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0);
                if (target)
                    DoCast(target, AnomalusConst.SpellSpark);

                task.Repeat(TimeSpan.FromSeconds(5));
            });

            Phase = 0;
            uiChaoticRiftGUID.Clear();
            chaosTheory = true;
        }

        public override void Reset()
        {
            Initialize();

            instance.SetBossState(DataTypes.Anomalus, EncounterState.NotStarted);
        }

        public override void EnterCombat(Unit who)
        {
            Talk(AnomalusConst.SayAggro);

            instance.SetBossState(DataTypes.Anomalus, EncounterState.InProgress);
        }

        public override void JustDied(Unit killer)
        {
            Talk(AnomalusConst.SayDeath);

            instance.SetBossState(DataTypes.Anomalus, EncounterState.Done);
        }

        public override uint GetData(uint type)
        {
            if (type == AnomalusConst.DataChaosTheory)
                return chaosTheory ? 1 : 0u;

            return 0;
        }

        public override void SummonedCreatureDies(Creature summoned, Unit who)
        {
            if (summoned.GetEntry() == AnomalusConst.NpcChaoticRift)
                chaosTheory = false;
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            if (me.GetDistance(me.GetHomePosition()) > 60.0f)
            {
                // Not blizzlike, hack to avoid an exploit
                EnterEvadeMode();
                return;
            }

            if (me.HasAura(AnomalusConst.SpellRiftShield))
            {
                if (!uiChaoticRiftGUID.IsEmpty())
                {
                    Creature Rift = ObjectAccessor.GetCreature(me, uiChaoticRiftGUID);
                    if (Rift && Rift.IsDead())
                    {
                        me.RemoveAurasDueToSpell(AnomalusConst.SpellRiftShield);
                        uiChaoticRiftGUID.Clear();
                    }
                    return;
                }
            }
            else
                uiChaoticRiftGUID.Clear();

            if ((Phase == 0) && HealthBelowPct(50))
            {
                Phase = 1;
                Talk(AnomalusConst.SayShield);
                DoCast(me, AnomalusConst.SpellRiftShield);
                Creature Rift = me.SummonCreature(AnomalusConst.NpcChaoticRift, AnomalusConst.RiftLocation[RandomHelper.IRand(0, 5)], TempSummonType.TimedDespawnOOC, 1000);
                if (Rift)
                {
                    //DoCast(Rift, SPELL_CHARGE_RIFT);
                    Unit target = SelectTarget(SelectAggroTarget.Random, 0);
                    if (target)
                        Rift.GetAI().AttackStart(target);
                    uiChaoticRiftGUID = Rift.GetGUID();
                    Talk(AnomalusConst.SayRift);
                }
            }

            _scheduler.Update(diff);

            DoMeleeAttackIfReady();
        }

        InstanceScript instance;

        byte Phase;
        ObjectGuid uiChaoticRiftGUID;
        bool chaosTheory;
    }

    [Script]
    class npc_chaotic_rift : ScriptedAI
    {
        public npc_chaotic_rift(Creature creature) : base(creature)
        {
            Initialize();
            instance = me.GetInstanceScript();
            SetCombatMovement(false);
        }

        void Initialize()
        {
            uiChaoticEnergyBurstTimer = 1000;
            uiSummonCrazedManaWraithTimer = 5000;
        }

        public override void Reset()
        {
            Initialize();
            me.SetDisplayFromModel(1);
            DoCast(me, AnomalusConst.SpellArcaneform, false);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            if (uiChaoticEnergyBurstTimer <= diff)
            {
                Creature Anomalus = ObjectAccessor.GetCreature(me, instance.GetGuidData(DataTypes.Anomalus));
                Unit target = SelectTarget(SelectAggroTarget.Random, 0);
                if (target)
                {
                    if (Anomalus && Anomalus.HasAura(AnomalusConst.SpellRiftShield))
                        DoCast(target, AnomalusConst.SpellChargedChaoticEnergyBurst);
                    else
                        DoCast(target, AnomalusConst.SpellChaoticEnergyBurst);
                }
                uiChaoticEnergyBurstTimer = 1000;
            }
            else
                uiChaoticEnergyBurstTimer -= diff;

            if (uiSummonCrazedManaWraithTimer <= diff)
            {
                Creature Wraith = me.SummonCreature(AnomalusConst.NpcCrazedManaWraith, me.GetPositionX() + 1, me.GetPositionY() + 1, me.GetPositionZ(), 0, TempSummonType.TimedDespawnOOC, 1000);
                if (Wraith)
                {
                    Unit target = SelectTarget(SelectAggroTarget.Random, 0);
                    if (target)
                        Wraith.GetAI().AttackStart(target);
                }
                Creature Anomalus = ObjectAccessor.GetCreature(me, instance.GetGuidData(DataTypes.Anomalus));
                if (Anomalus && Anomalus.HasAura(AnomalusConst.SpellRiftShield))
                    uiSummonCrazedManaWraithTimer = 5000;
                else
                    uiSummonCrazedManaWraithTimer = 10000;
            }
            else
                uiSummonCrazedManaWraithTimer -= diff;
        }

        InstanceScript instance;

        uint uiChaoticEnergyBurstTimer;
        uint uiSummonCrazedManaWraithTimer;
    }

    [Script]
    class achievement_chaos_theory : AchievementCriteriaScript
    {
        public achievement_chaos_theory() : base("achievement_chaos_theory")
        {
        }

        public override bool OnCheck(Player player, Unit target)
        {
            if (!target)
                return false;

            Creature Anomalus = target.ToCreature();
            if (Anomalus)
                if (Anomalus.GetAI().GetData(AnomalusConst.DataChaosTheory) != 0)
                    return true;

            return false;
        }
    }
}
