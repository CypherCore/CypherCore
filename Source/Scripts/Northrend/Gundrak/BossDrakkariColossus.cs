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
using System.Collections.Generic;

namespace Scripts.Northrend.Gundrak.DrakkariColossus
{
    struct TextIds
    {
        // Drakkari Elemental
        public const uint EmoteMojo = 0;
        public const uint EmoteActivateAltar = 1;
    }

    struct SpellIds
    {
        public const uint Emerge = 54850;
        public const uint ElementalSpawnEffect = 54888;
        public const uint MojoVolley = 54849;
        public const uint SurgeVisual = 54827;
        public const uint Merge = 54878;
        public const uint MightyBlow = 54719;
        public const uint Surge = 54801;
        public const uint FreezeAnim = 16245;
        public const uint MojoPuddle = 55627;
        public const uint MojoWave = 55626;
    }

    struct Misc
    {
        public const uint EventMightyBlow = 1;
        public const uint EventSurge = 1;

        public const int ActionSummonElemental = 1;
        public const int ActionFreezeColossus = 2;
        public const int ActionUnfreezeColossus = 3;
        public const int ActionReturnToColossus = 1;

        public const byte ColossusPhaseNormal = 1;
        public const byte ColossusPhaseFirstElementalSummon = 2;
        public const byte ColossusPhaseSecondElementalSummon = 3;

        public const uint DataColossusPhase = 1;
        public const uint DataIntroDone = 2;
    }

    [Script]
    class boss_drakkari_colossus : BossAI
    {
        public boss_drakkari_colossus(Creature creature) : base(creature, GDDataTypes.DrakkariColossus)
        {
            Initialize();
            me.SetReactState(ReactStates.Passive);
            introDone = false;
        }

        void Initialize()
        {
            phase = Misc.ColossusPhaseNormal;
        }

        public override void Reset()
        {
            _Reset();

            if (GetData(Misc.DataIntroDone) != 0)
            {
                me.SetReactState(ReactStates.Aggressive);
                me.RemoveFlag(UnitFields.Flags, UnitFlags.ImmuneToPc);
                me.RemoveAura(SpellIds.FreezeAnim);
            }

            _scheduler.SetValidator(() => !me.HasUnitState(UnitState.Casting));

            _scheduler.Schedule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30), task =>
            {
                DoCastVictim(SpellIds.MightyBlow);
                task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
            });

            Initialize();
        }

        public override void EnterCombat(Unit who)
        {
            _EnterCombat();
            me.RemoveAura(SpellIds.FreezeAnim);
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
        }

        public override void DoAction(int action)
        {
            switch (action)
            {
                case Misc.ActionSummonElemental:
                    DoCast(SpellIds.Emerge);
                    break;
                case Misc.ActionFreezeColossus:
                    me.GetMotionMaster().Clear();
                    me.GetMotionMaster().MoveIdle();

                    me.SetReactState(ReactStates.Passive);
                    me.SetFlag(UnitFields.Flags, UnitFlags.ImmuneToPc);
                    DoCast(me, SpellIds.FreezeAnim);
                    break;
                case Misc.ActionUnfreezeColossus:
                    if (me.GetReactState() == ReactStates.Aggressive)
                        return;

                    me.SetReactState(ReactStates.Aggressive);
                    me.RemoveFlag(UnitFields.Flags, UnitFlags.ImmuneToPc);
                    me.RemoveAura(SpellIds.FreezeAnim);

                    me.SetInCombatWithZone();

                    if (me.GetVictim())
                        me.GetMotionMaster().MoveChase(me.GetVictim(), 0, 0);
                    break;
            }
        }

        public override void DamageTaken(Unit attacker, ref uint damage)
        {
            if (me.HasFlag(UnitFields.Flags, UnitFlags.ImmuneToPc))
                damage = 0;

            if (phase == Misc.ColossusPhaseNormal ||
                phase == Misc.ColossusPhaseFirstElementalSummon)
            {
                if (HealthBelowPct(phase == Misc.ColossusPhaseNormal ? 50 : 5))
                {
                    damage = 0;
                    phase = (phase == Misc.ColossusPhaseNormal ? Misc.ColossusPhaseFirstElementalSummon : Misc.ColossusPhaseSecondElementalSummon);
                    DoAction(Misc.ActionFreezeColossus);
                    DoAction(Misc.ActionSummonElemental);
                }
            }
        }

        public override uint GetData(uint data)
        {
            if (data == Misc.DataColossusPhase)
                return phase;
            else if (data == Misc.DataIntroDone)
                return introDone ? 1 : 0u;

            return 0;
        }

        public override void SetData(uint type, uint data)
        {
            if (type == Misc.DataIntroDone)
                introDone = data != 0;
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            if (me.GetReactState() == ReactStates.Aggressive)
                DoMeleeAttackIfReady();
        }

        public override void JustSummoned(Creature summon)
        {
            summon.SetInCombatWithZone();

            if (phase == Misc.ColossusPhaseSecondElementalSummon)
                summon.SetHealth(summon.GetMaxHealth() / 2);
        }

        byte phase;
        bool introDone;
    }

    [Script]
    class boss_drakkari_elemental : ScriptedAI
    {
        public boss_drakkari_elemental(Creature creature) : base(creature)
        {
            DoCast(me, SpellIds.ElementalSpawnEffect);
            instance = creature.GetInstanceScript();
        }

        public override void Reset()
        {
            _scheduler.CancelAll();
            _scheduler.SetValidator(() => !me.HasUnitState(UnitState.Casting));

            _scheduler.Schedule(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), task =>
            {
                DoCast(SpellIds.SurgeVisual);
                Unit target = SelectTarget(SelectAggroTarget.Random, 1, 0.0f, true);
                if (target)
                    DoCast(target, SpellIds.Surge);
                task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
            });

            me.AddAura(SpellIds.MojoVolley, me);
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.EmoteActivateAltar);

            Creature colossus = instance.GetCreature(GDDataTypes.DrakkariColossus);
            if (colossus)
                killer.Kill(colossus);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            DoMeleeAttackIfReady();
        }

        public override void DoAction(int action)
        {
            switch (action)
            {
                case Misc.ActionReturnToColossus:
                    Talk(TextIds.EmoteMojo);
                    DoCast(SpellIds.SurgeVisual);
                    Creature colossus = instance.GetCreature(GDDataTypes.DrakkariColossus);
                    if (colossus)
                        // what if the elemental is more than 80 yards from drakkari colossus ?
                        DoCast(colossus, SpellIds.Merge, true);
                    break;
            }
        }

        public override void DamageTaken(Unit attacker, ref uint damage)
        {
            if (HealthBelowPct(50))
            {
                Creature colossus = instance.GetCreature(GDDataTypes.DrakkariColossus);
                if (colossus)
                {
                    if (colossus.GetAI().GetData(Misc.DataColossusPhase) == Misc.ColossusPhaseFirstElementalSummon)
                    {
                        damage = 0;

                        // to prevent spell spaming
                        if (me.HasUnitState(UnitState.Charging))
                            return;

                        // not sure about this, the idea of this code is to prevent bug the elemental
                        // if it is not in a acceptable distance to cast the charge spell.
                        if (me.GetDistance(colossus) > 80.0f)
                        {
                            if (me.GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Point)
                                return;

                            me.GetMotionMaster().MovePoint(0, colossus.GetPositionX(), colossus.GetPositionY(), colossus.GetPositionZ());
                            return;
                        }
                        DoAction(Misc.ActionReturnToColossus);
                    }
                }
            }
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            me.DespawnOrUnsummon();
        }

        public override void SpellHitTarget(Unit target, SpellInfo spell)
        {
            if (spell.Id == SpellIds.Merge)
            {
                Creature colossus = target.ToCreature();
                if (colossus)
                {
                    colossus.GetAI().DoAction(Misc.ActionUnfreezeColossus);
                    me.DespawnOrUnsummon();
                }
            }
        }

        InstanceScript instance;
    }

    [Script]
    class npc_living_mojo : ScriptedAI
    {
        public npc_living_mojo(Creature creature) : base(creature)
        {
            instance = creature.GetInstanceScript();
        }

        public override void Reset()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(2), task =>
            {
                DoCastVictim(SpellIds.MojoWave);
                task.Repeat(TimeSpan.FromSeconds(15));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(7), task =>
            {
                DoCastVictim(SpellIds.MojoPuddle);
                task.Repeat(TimeSpan.FromSeconds(18));
            });
        }

        void MoveMojos(Creature boss)
        {
            List<Creature> mojosList = new List<Creature>();
            boss.GetCreatureListWithEntryInGrid(mojosList, me.GetEntry(), 12.0f);
            if (!mojosList.Empty())
            {
                foreach (var mojo in mojosList)
                {
                    if (mojo)
                        mojo.GetMotionMaster().MovePoint(1, boss.GetHomePosition());
                }
            }
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (type != MovementGeneratorType.Point)
                return;

            if (id == 1)
            {
                Creature colossus = instance.GetCreature(GDDataTypes.DrakkariColossus);
                if (colossus)
                {
                    colossus.GetAI().DoAction(Misc.ActionUnfreezeColossus);
                    if (colossus.GetAI().GetData(Misc.DataIntroDone) == 0)
                        colossus.GetAI().SetData(Misc.DataIntroDone, 1);
                    colossus.SetInCombatWithZone();
                    me.DespawnOrUnsummon();
                }
            }
        }

        public override void AttackStart(Unit attacker)
        {
            if (me.GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Point)
                return;

            // we do this checks to see if the creature is one of the creatures that sorround the boss
            Creature colossus = instance.GetCreature(GDDataTypes.DrakkariColossus);
            if (colossus)
            {
                Position homePosition = me.GetHomePosition();

                float distance = homePosition.GetExactDist(colossus.GetHomePosition());

                if (distance < 12.0f)
                {
                    MoveMojos(colossus);
                    me.SetReactState(ReactStates.Passive);
                }
                else
                    base.AttackStart(attacker);
            }
        }

        public override void UpdateAI(uint diff)
        {
            //Return since we have no target
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            DoMeleeAttackIfReady();
        }

        InstanceScript instance;
    }
}
